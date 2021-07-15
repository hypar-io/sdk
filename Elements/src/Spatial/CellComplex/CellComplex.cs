using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A non-manifold cellular structure.
    /// </summary>
    /// <example>
    /// [!code-csharp[Main](../../Elements/test/CellComplexTests.cs?name=example)]
    /// </example>
    public class CellComplex : FaceComplex
    {
        private ulong _cellId = 1; // we start at 1 because 0 is returned as default value from dicts

        /// <summary>
        /// Cells by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Cell> _cells = new Dictionary<ulong, Cell>();

        /// <summary>
        /// Create a CellComplex.
        /// </summary>
        /// <param name="id">Optional ID: If blank, a new Guid will be created.</param>
        /// <param name="name">Optional name of your CellComplex.</param>
        /// <returns></returns>
        public CellComplex(Guid id = default(Guid), string name = null) : base(id != default(Guid) ? id : Guid.NewGuid(), name) { }

        /// <summary>
        /// This constructor is intended for serialization and deserialization only.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="_vertices"></param>
        /// <param name="_orientations"></param>
        /// <param name="_edges"></param>
        /// <param name="_directedEdges"></param>
        /// <param name="_faces"></param>
        /// <param name="_cells"></param>
        /// <returns></returns>
        [JsonConstructor]
        internal CellComplex(
            Guid id,
            string name,
            Dictionary<ulong, Vertex> _vertices,
            Dictionary<ulong, Vertex> _orientations,
            Dictionary<ulong, Edge> _edges,
            Dictionary<ulong, DirectedEdge> _directedEdges,
            Dictionary<ulong, Face> _faces,
            Dictionary<ulong, Cell> _cells
        ) : base(id, name)
        {
            foreach (var vertex in _vertices.Values)
            {
                var added = this.AddVertexOrOrientation<Vertex>(vertex.Value, vertex.Id);
                added.Name = vertex.Name;
            }

            foreach (var orientation in _orientations.Values)
            {
                this.AddVertexOrOrientation<Orientation>(orientation.Value, orientation.Id);
            }

            foreach (var edge in _edges.Values)
            {
                if (!this.AddEdge(new List<ulong>() { edge.StartVertexId, edge.EndVertexId }, edge.Id, out var addedEdge))
                {
                    throw new Exception("Duplicate edge ID found");
                }
            }

            foreach (var directedEdge in _directedEdges.Values)
            {
                var edge = this.GetEdge(directedEdge.EdgeId);
                if (!this.AddDirectedEdge(edge, edge.StartVertexId == directedEdge.StartVertexId, directedEdge.Id, out var addedDirectedEdge))
                {
                    throw new Exception("Duplicate directed edge ID found");
                }
            }

            foreach (var face in _faces.Values)
            {
                face.FaceComplex = this; // CellComplex not included on deserialization, add it back for processing even though we will discard this and create a new one
                var polygon = face.GetGeometry();
                var orientation = face.GetOrientation();
                if (!this.AddFace(polygon, face.Id, orientation.U, orientation.V, out var addedFace))
                {
                    throw new Exception("Duplicate face ID found");
                }
            }

            foreach (var cell in _cells.Values)
            {
                var cellFaces = cell.FaceIds.Select(fId => this.GetFace(fId)).ToList();
                var bottomFace = this.GetFace(cell.BottomFaceId);
                var topFace = this.GetFace(cell.TopFaceId);
                if (!this.AddCell(cell.Id, cellFaces, bottomFace, topFace, out var addedCell))
                {
                    throw new Exception("Duplicate cell found");
                }
            }
        }

        #region add content

        /// <summary>
        /// Add a cell to the CellComplex.
        /// </summary>
        /// <param name="polygon">The polygon that forms the base of this cell.</param>
        /// <param name="height">The height of the cell.</param>
        /// <param name="elevation">The elevation of the bottom of this cell.</param>
        /// <param name="uGrid">An optional but highly recommended U grid that allows the cell's top and bottom faces to store intended directionality.</param>
        /// <param name="vGrid">An optional but highly recommended V grid that allows the cell's top and bottom faces to store intended directionality.</param>
        /// <returns>The created Cell.</returns>
        public Cell AddCell(Polygon polygon, double height, double elevation, Grid1d uGrid = null, Grid1d vGrid = null)
        {
            var elevationVector = new Vector3(0, 0, elevation);
            var heightVector = new Vector3(0, 0, height);

            var transformedPolygonBottom = (Polygon)polygon.Transformed(new Transform(elevationVector));
            var transformedPolygonTop = (Polygon)polygon.Transformed(new Transform(elevationVector + heightVector));

            Orientation u = null;
            Orientation v = null;

            if (uGrid != null)
            {
                u = this.AddVertexOrOrientation<Orientation>(uGrid.Direction().Unitized());
            }
            if (vGrid != null)
            {
                v = this.AddVertexOrOrientation<Orientation>(vGrid.Direction().Unitized());
            }

            var bottomFace = this.AddFace(transformedPolygonBottom, u, v);
            var topFace = this.AddFace(transformedPolygonTop, u, v);

            var faces = new List<Face>() { bottomFace, topFace };

            var up = this.AddVertexOrOrientation<Orientation>(new Vector3(0, 0, 1));

            foreach (var faceEdge in transformedPolygonBottom.Segments())
            {
                var vertices = new List<Vector3>() { faceEdge.Start, faceEdge.End };
                var horizontalU = this.AddVertexOrOrientation<Orientation>((faceEdge.End - faceEdge.Start).Unitized());
                vertices.Add(faceEdge.End + heightVector);
                vertices.Add(faceEdge.Start + heightVector);
                var facePoly = new Polygon(vertices);
                faces.Add(this.AddFace(facePoly, horizontalU, up));
            }

            this.AddCell(this._cellId, faces, bottomFace, topFace, out var cell);
            return cell;
        }

        /// <summary>
        /// The lowest-level method to add a Cell: all other AddCell methods will eventually call this one.
        /// </summary>
        /// <param name="cellId"></param>
        /// <param name="faces"></param>
        /// <param name="bottomFace"></param>
        /// <param name="topFace"></param>
        /// <param name="cell"></param>
        /// <returns>Whether the cell was successfully added. Will be false if cellId already exists.</returns>
        private Boolean AddCell(ulong cellId, List<Face> faces, Face bottomFace, Face topFace, out Cell cell)
        {
            if (this._cells.ContainsKey(cellId))
            {
                cell = null;
                return false;
            }

            cell = new Cell(this, cellId, faces, bottomFace, topFace);
            this._cells.Add(cell.Id, cell);

            foreach (var face in faces)
            {
                face.Cells.Add(cell);
            }

            this._cellId = Math.Max(cellId + 1, this._cellId + 1);
            return true;
        }

        #endregion add content

        /// <summary>
        /// Get a Cell by its ID.
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public Cell GetCell(ulong cellId)
        {
            this._cells.TryGetValue(cellId, out var cell);
            return cell;
        }

        /// <summary>
        /// Get all Cells.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetCells()
        {
            return this._cells.Values.ToList();
        }

        /// <summary>
        /// Get the associated Cell that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Cell GetClosestCell(Vector3 point)
        {
            return Cell.GetClosest<Cell>(this.GetCells(), point);
        }
    }
}