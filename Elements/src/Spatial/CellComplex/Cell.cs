using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Geometry.Solids;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A cell is a 3-dimensional closed extrusion within a complex.
    /// </summary>
    public class Cell : ChildBase<Cell, Extrude>, Interfaces.IHasNeighbors<Cell, Extrude>
    {
        /// <summary>
        /// The ID of this cell's bottom face. If set, it is also expected to be duplicated in list of faces.
        /// In the current implementation of CellComplex, this should always be set.
        /// This may not be the case in the future, if Cells are no longer constrained to vertical extrusions.
        /// </summary>
        public ulong? BottomFaceId = null;

        /// <summary>
        /// The ID of this cell's top face. If set, it is also expected to be duplicated in list of faces.
        /// In the current implementation of CellComplex, this should always be set.
        /// This may not be the case in the future, if Cells are no longer constrained to vertical extrusions.
        /// </summary>
        public ulong? TopFaceId = null;

        /// <summary>
        /// The IDs of all the faces which belong to this Cell.
        /// </summary>
        public List<ulong> FaceIds;

        /// <summary>
        /// An Extrusion that we generate and store internally, and which can be accessed through GetGeometry()
        /// </summary>
        private Extrude _geometry;

        /// <summary>
        /// The CellComplex that this cell belongs to.
        /// </summary>
        [JsonIgnore]
        public CellComplex CellComplex { get; internal set; }

        /// <summary>
        /// Represents a unique Cell wtihin a CellComplex.
        /// Is not intended to be created or modified outside of the CellComplex class code.
        /// </summary>
        /// <param name="cellComplex">CellComplex that this cell belongs to.</param>
        /// <param name="id">Cell ID.</param>
        /// <param name="faces">List of faces which make up this CellComplex.</param>
        /// <param name="bottomFace">The bottom face for this cell. This should also be included in the list of all faces.</param>
        /// <param name="topFace">The top face for this cell. This should also be included in the list of all faces.</param>
        internal Cell(CellComplex cellComplex, ulong id, List<Face> faces, Face bottomFace, Face topFace) : base(id, cellComplex)
        {
            this.CellComplex = cellComplex;
            if (bottomFace != null)
            {
                this.BottomFaceId = bottomFace.Id;
            }
            if (topFace != null)
            {
                this.TopFaceId = topFace.Id;
            }
            if (bottomFace != null && topFace != null)
            {
                var bottom = this.GetBottomFace().GetGeometry();
                var top = this.GetTopFace().GetGeometry();
                this._geometry = new Extrude(bottom, top.Centroid().Z - bottom.Centroid().Z, new Vector3(0, 0, 1), false);
            }
            this.FaceIds = faces.Select(ds => ds.Id).ToList();
        }

        /// <summary>
        /// Used for deserialization only!
        /// </summary>
        [JsonConstructor]
        internal Cell(ulong id, List<ulong> faceIds, ulong? bottomFaceId, ulong? topFaceId) : base(id, null)
        {
            this.FaceIds = faceIds;
            this.BottomFaceId = bottomFaceId;
            this.TopFaceId = topFaceId;
        }

        /// <summary>
        /// Get a Solid Extrusion representing this Cell.
        /// </summary>
        /// <returns></returns>
        public override Extrude GetGeometry()
        {
            return this._geometry;
        }

        /// <summary>
        /// Get the shortest distance from a point to the geometry representing this cell.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override double DistanceTo(Vector3 point)
        {
            var extrude = this.GetGeometry() as Extrude;
            var bottom = extrude.Profile.Perimeter;
            var bottomZ = bottom.Centroid().Z;
            var topZ = (bottom.Centroid() + extrude.Direction * extrude.Height).Z;
            var isInside = point.Z >= bottomZ && point.Z <= topZ && bottom.Contains(new Vector3(point.X, point.Y, bottomZ));
            if (isInside)
            {
                return 0;
            }
            var minDistance = double.PositiveInfinity;
            foreach (var face in extrude.Solid.Faces.Values)
            {
                minDistance = Math.Min(minDistance, point.DistanceTo(face.Outer.ToPolygon()));
            }
            return minDistance;
        }

        /// <summary>
        /// Get top face, if defined.
        /// </summary>
        /// <returns></returns>
        public Face GetTopFace()
        {
            return this.TopFaceId == null ? null : this.FaceComplex.GetFace(this.TopFaceId);
        }

        /// <summary>
        /// Get bottom face, if defined.
        /// </summary>
        /// <returns></returns>
        public Face GetBottomFace()
        {
            return this.BottomFaceId == null ? null : this.FaceComplex.GetFace(this.BottomFaceId);
        }

        /// <summary>
        /// Get associated Faces.
        /// </summary>
        /// <returns></returns>
        public List<Face> GetFaces()
        {
            return this.FaceIds.Select(fId => this.FaceComplex.GetFace(fId)).ToList();
        }

        /// <summary>
        /// Get the closest associated face to the supplied position.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Face GetClosestFace(Vector3 point)
        {
            return Face.GetClosest<Face>(this.GetFaces(), point);
        }

        /// <summary>
        /// Get associated Edges.
        /// </summary>
        /// <returns></returns>
        public List<Edge> GetEdges()
        {
            return this.GetFaces().Select(face => face.GetEdges()).SelectMany(x => x).Distinct().ToList();
        }

        /// <summary>
        /// Get the associated Edge that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Edge GetClosestEdge(Vector3 point)
        {
            return Edge.GetClosest<Edge>(this.GetEdges(), point);
        }

        /// <summary>
        /// Get associated Vertices.
        /// </summary>
        /// <returns></returns>
        public List<Vertex> GetVertices(Vector3? point = null)
        {
            return this.GetFaces().Select(f => f.GetVertices()).SelectMany(x => x).Distinct().ToList();
        }

        /// <summary>
        /// Get the associated vertex that is closest to a point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vertex GetClosestVertex(Vector3 point)
        {
            return Vertex.GetClosest<Vertex>(this.GetVertices(), point);
        }

        /// <summary>
        /// Get list of Cells that are neighbors.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetNeighbors()
        {
            return this.GetFaces().Select(f => f.GetCells()).SelectMany(x => x).Distinct().Where(c => c.Id != this.Id).ToList();
        }

        /// <summary>
        /// Get the neighbor that shares a specific Face with this cell.
        /// Can be null if the face is not shared.
        /// </summary>
        /// <param name="face">Shared face</param>
        /// <returns></returns>
        public Cell GetNeighbors(Face face)
        {
            if (face == null)
            {
                return null;
            }

            var cells = face.GetCells().Where(cell => cell.Id != this.Id).ToList();

            if (cells.Count == 0)
            {
                return null;
            }
            if (cells.Count > 1)
            {
                throw new Exception("Cells should only have one neighbor which share this face");
            }

            return cells[0];
        }

        /// <summary>
        /// Get the closest associated cell to the supplied point.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Cell GetClosestNeighbor(Vector3 target)
        {
            return Cell.GetClosest<Cell>(this.GetNeighbors().Where(c => c.DistanceTo(target) < this.DistanceTo(target)).ToList(), target);
        }

        /// <summary>
        /// Traverse the neighbors of this Cell toward the target point.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="completedRadius">If provided, ends the traversal when the neighbor is within this distance to the target point.</param>
        /// <returns>A collection of traversed Cells, including the starting Cell.</returns>
        public List<Cell> TraverseNeighbors(Vector3 target, double completedRadius = 0)
        {
            var maxCount = CellComplex.GetCells().Count;
            Func<Cell, Cell> getNextNeighbor = (Cell curNeighbor) => (curNeighbor.GetClosestNeighbor(target));
            return Cell.TraverseNeighbors(this, maxCount, target, completedRadius, getNextNeighbor);
        }
    }
}