using Elements.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elements.Spatial.AdaptiveGrid
{
    public class AdaptiveGrid
    {
        #region Private fields

        private ulong _edgeId = 1; // we start at 1 because 0 is returned as default value from dicts

        private ulong _vertexId = 1; // we start at 1 because 0 is returned as default value from dicts

        /// <summary>
        /// Vertices by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Vertex> _vertices = new Dictionary<ulong, Vertex>();

        /// <summary>
        /// Edges by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Edge> _edges = new Dictionary<ulong, Edge>();

        // See Edge.GetHash for how faces are identified as unique.
        private Dictionary<string, ulong> _edgesLookup = new Dictionary<string, ulong>();

        // Vertex lookup by x, y, z coordinate.
        private Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> _verticesLookup = new Dictionary<double, Dictionary<double, Dictionary<double, ulong>>>();

        #endregion

        #region Properties

        /// <summary>
        /// Tolerance for points being considered the same.
        /// Applies individually to X, Y, and Z coordinates, not the cumulative difference!
        /// </summary>
        public double Tolerance { get; set; } = Vector3.EPSILON;

        public double MinimumResolution { get; set; }

        public Transform Transform { get; set; }

        #endregion

        #region Constructors

        public AdaptiveGrid(double minimumResolution, Transform transform)
        {
            MinimumResolution = minimumResolution;
            Transform = transform;
        }

        #endregion

        #region Public logic

        public void AddFromBbox(BBox3 bBox, double stepSize)
        {
            var height = bBox.Max.Z - bBox.Min.Z;
            var boundary = new Polygon(new List<Vector3>
            {
                new Vector3(bBox.Min.X, bBox.Min.Y),
                new Vector3(bBox.Min.X, bBox.Max.Y),
                new Vector3(bBox.Max.X, bBox.Max.Y),
                new Vector3(bBox.Max.X, bBox.Min.Y)
            }).TransformedPolygon(new Transform(new Vector3(0, 0, bBox.Min.Z)));
            AddFromExtrude(boundary, Vector3.ZAxis, height, stepSize);
        }

        public void AddFromBbox(BBox3 bBox, List<Vector3> keyPoints)
        {
            var height = bBox.Max.Z - bBox.Min.Z;
            var boundary = new Polygon(new List<Vector3>
            {
                new Vector3(bBox.Min.X, bBox.Min.Y),
                new Vector3(bBox.Min.X, bBox.Max.Y),
                new Vector3(bBox.Max.X, bBox.Max.Y),
                new Vector3(bBox.Max.X, bBox.Min.Y)
            }).TransformedPolygon(new Transform(new Vector3(0, 0, bBox.Min.Z)));
            AddFromExtrude(boundary, Vector3.ZAxis, height, keyPoints);
        }

        public void AddFromExtrude(Polygon boundingPolygon, Vector3 extrusionAxis, double height, double stepSize)
        {
            var gridZ = new Grid1d(height);
            var sacrificialPanels = (gridZ.Domain.Length % stepSize < MinimumResolution) ? 1 : 0;
            gridZ.DivideByFixedLength(stepSize, sacrificialPanels: sacrificialPanels);

            var zCells = gridZ.GetCells();
            for (var i = 0; i < zCells.Count; i++)
            {
                var elevationVector = zCells[i].Domain.Min * extrusionAxis;
                var transformedPolygonBottom = boundingPolygon.TransformedPolygon(new Transform(elevationVector));
                var addedEdges = AddFromPolygon(transformedPolygonBottom, stepSize);
                AddVerticalEdges(extrusionAxis, zCells[i].Domain.Length, addedEdges);
                if (i == zCells.Count - 1)
                {
                    var transformedPolygonTop = boundingPolygon.TransformedPolygon(new Transform(zCells[i].Domain.Max * extrusionAxis));
                    AddFromPolygon(transformedPolygonTop, stepSize);
                }
            }
        }

        public void AddFromExtrude(Polygon boundingPolygon, Vector3 extrusionAxis, double height, List<Vector3> keyPoints)
        {
            var gridZ = new Grid1d(new Line(boundingPolygon.Start, boundingPolygon.Start + height * extrusionAxis));
            gridZ.SplitAtPoints(keyPoints);

            var zCells = gridZ.GetCells();
            for (var i = 0; i < zCells.Count; i++)
            {
                var elevationVector = zCells[i].Domain.Min * extrusionAxis;
                var transformedPolygonBottom = boundingPolygon.TransformedPolygon(new Transform(elevationVector));
                var addedEdges = AddFromPolygon(transformedPolygonBottom, keyPoints);
                AddVerticalEdges(extrusionAxis, zCells[i].Domain.Length, addedEdges);
                if (i == zCells.Count - 1)
                {
                    var transformedPolygonTop = boundingPolygon.TransformedPolygon(new Transform(zCells[i].Domain.Max * extrusionAxis));
                    AddFromPolygon(transformedPolygonTop, keyPoints);
                }
            }
        }

        public HashSet<Edge> AddFromPolygon(Polygon boundingPolygon, double stepSize)
        {
            if (stepSize < MinimumResolution)
            {
                return new HashSet<Edge>();
            }

            var grid = CreateGridFromPolygon(boundingPolygon);
            SplitGridAtIntersectionPoints(boundingPolygon, grid);

            var sacrificialPanelsU = (grid.U.Domain.Length % stepSize < MinimumResolution) ? 1 : 0;
            var sacrificialPanelsV = (grid.V.Domain.Length % stepSize < MinimumResolution) ? 1 : 0;
            grid.U.DivideByFixedLength(stepSize, sacrificialPanels: sacrificialPanelsU);
            grid.V.DivideByFixedLength(stepSize, sacrificialPanels: sacrificialPanelsV);
            return AddFromGrid(grid);
        }

        public HashSet<Edge> AddFromPolygon(Polygon boundingPolygon, List<Vector3> keyPoints)
        {
            var grid = CreateGridFromPolygon(boundingPolygon);
            SplitGridAtIntersectionPoints(boundingPolygon, grid);

            grid.U.SplitAtPoints(keyPoints);
            grid.V.SplitAtPoints(keyPoints);
            return AddFromGrid(grid);
        }

        public HashSet<Edge> AddFromGrid(Grid2d grid)
        {
            var cells = grid.GetCells();
            var addedEdges = new HashSet<Edge>();
            foreach (var cell in cells)
            {
                foreach (var cellGeometry in cell.GetTrimmedCellGeometry())
                {
                    var polygon = (Polygon)cellGeometry;
                    foreach (var segment in polygon.Segments())
                    {
                        var edges = AddEdge(segment);
                        edges.ForEach(e => addedEdges.Add(e));
                    }
                }
            }

            return addedEdges;
        }

        public List<Edge> AddEdge(Line edgeLine)
        {
            var addedEdges = new List<Edge>();
            var startVertex = AddVertex(edgeLine.Start);
            var endVertex = AddVertex(edgeLine.End);

            var newEdgeLine = new Line(startVertex.Point, endVertex.Point);
            var intersectionPoints = new List<Vector3>();
            foreach (var edge in GetEdges())
            {
                var oldEdgeLine = edge.GetGeometry();
                if (!new List<Vector3> { oldEdgeLine.Start, oldEdgeLine.End, newEdgeLine.Start, newEdgeLine.End }.AreCoplanar())
                {
                    continue;
                }
                if (newEdgeLine.Intersects(oldEdgeLine, out var intersectionPoint))
                {
                    intersectionPoints.Add(intersectionPoint);
                    var newVertex = AddVertex(intersectionPoint);
                    if (edge.StartId != newVertex.Id)
                    {
                        AddEdge(edge.StartId, newVertex.Id);
                    }
                    if (edge.EndId != newVertex.Id)
                    {
                        AddEdge(edge.EndId, newVertex.Id);
                    }

                    DeleteEdge(edge);
                }
                else if (oldEdgeLine.Direction().IsParallelTo(newEdgeLine.Direction()))
                {
                    var isNewEdgeStartOnOldEdge = oldEdgeLine.PointOnLine(newEdgeLine.Start);
                    var isNewEdgeEndOnOldEdge = oldEdgeLine.PointOnLine(newEdgeLine.End);
                    var isOldEdgeStartOnNewEdge = newEdgeLine.PointOnLine(oldEdgeLine.Start, true);
                    var isOldEdgeEndOnNewEdge = newEdgeLine.PointOnLine(oldEdgeLine.End, true);
                    // new edge is inside old edge
                    if (isNewEdgeStartOnOldEdge && isNewEdgeEndOnOldEdge)
                    {
                        if (oldEdgeLine.Start.DistanceTo(newEdgeLine.Start) < oldEdgeLine.Start.DistanceTo(newEdgeLine.End))
                        {
                            AddEdge(edge.StartId, startVertex.Id);
                            AddEdge(edge.EndId, endVertex.Id);
                        }
                        else
                        {
                            AddEdge(edge.StartId, endVertex.Id);
                            AddEdge(edge.EndId, startVertex.Id);
                        }
                        DeleteEdge(edge);
                    }
                    // edges overlap
                    else if (isNewEdgeStartOnOldEdge || isNewEdgeEndOnOldEdge)
                    {
                        if (isOldEdgeEndOnNewEdge)
                        {
                            intersectionPoints.Add(oldEdgeLine.End);
                            if (oldEdgeLine.Start.DistanceTo(newEdgeLine.Start) < oldEdgeLine.Start.DistanceTo(newEdgeLine.End))
                            {
                                AddEdge(edge.StartId, startVertex.Id);
                            }
                            else
                            {
                                AddEdge(edge.StartId, endVertex.Id);
                            }
                        }
                        else if (isOldEdgeStartOnNewEdge)
                        {
                            intersectionPoints.Add(oldEdgeLine.Start);
                            if (oldEdgeLine.End.DistanceTo(newEdgeLine.Start) < oldEdgeLine.End.DistanceTo(newEdgeLine.End))
                            {
                                AddEdge(edge.EndId, startVertex.Id);
                            }
                            else
                            {
                                AddEdge(edge.EndId, endVertex.Id);
                            }
                        }
                        DeleteEdge(edge);
                    }
                    // old edge is inside new edge
                    else if (isOldEdgeStartOnNewEdge && isOldEdgeEndOnNewEdge)
                    {
                        intersectionPoints.Add(oldEdgeLine.Start);
                        intersectionPoints.Add(oldEdgeLine.End);
                        DeleteEdge(edge);
                    }
                }
            }
            if (intersectionPoints.Any())
            {
                intersectionPoints = intersectionPoints.OrderBy(p => p.DistanceTo(newEdgeLine.Start)).ToList();
                intersectionPoints.Insert(0, newEdgeLine.Start);
                intersectionPoints.Add(newEdgeLine.End);
                intersectionPoints = intersectionPoints.Distinct().ToList();
                for (var i = 0; i < intersectionPoints.Count - 1; i++)
                {
                    var v1 = AddVertex(intersectionPoints[i]);
                    var v2 = AddVertex(intersectionPoints[i + 1]);
                    addedEdges.Add(AddEdge(v1.Id, v2.Id));
                }
            }
            else
            {
                addedEdges.Add(AddEdge(startVertex.Id, endVertex.Id));
            }

            return addedEdges;
        }

        /// <summary>
        /// Get a Vertex by its ID.
        /// </summary>
        /// <param name="vertexId"></param>
        /// <returns></returns>
        public Vertex GetVertex(ulong vertexId)
        {
            this._vertices.TryGetValue(vertexId, out var vertex);
            return vertex;
        }

        /// <summary>
        /// Get all Vertices.
        /// </summary>
        /// <returns></returns>
        public List<Vertex> GetVertices()
        {
            return this._vertices.Values.ToList();
        }

        /// <summary>
        /// Get all Edges.
        /// </summary>
        /// <returns></returns>
        public List<Edge> GetEdges()
        {
            return this._edges.Values.ToList();
        }

        /// <summary>
        /// Whether a vertex location already exists in the AdaptiveGrid.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="id">The ID of the Vertex, if a match is found.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search against each component of the coordinate.</param>
        /// <returns></returns>
        public bool VertexExists(Vector3 point, out ulong id, double? fuzzyFactor = null)
        {
            var zDict = GetAddressParent(_verticesLookup, point, fuzzyFactor: fuzzyFactor);
            if (zDict == null)
            {
                id = 0;
                return false;
            }
            return TryGetValue(zDict, point.Z, out id, fuzzyFactor);
        }

        #endregion

        #region Private logic

        private Vertex AddVertex(Vector3 point)
        {
            if (!VertexExists(point, out var id, Tolerance))
            {
                var zDict = GetAddressParent(_verticesLookup, point, true, Tolerance);
                id = this._vertexId;
                var vertex = new Vertex(this, id, point);
                zDict[point.Z] = id;
                _vertices[id] = vertex;
                this._vertexId++;
            }

            return GetVertex(id);
        }

        private void DeleteVertex(ulong id)
        {
            var vertex = _vertices[id];
            _vertices.Remove(id);
            var zDict = GetAddressParent(_verticesLookup, vertex.Point, fuzzyFactor: Tolerance);
            if (zDict == null)
            {
                return;
            }
            zDict.Remove(vertex.Point.Z);

            TryGetValue(_verticesLookup, vertex.Point.X, out var yzDict, Tolerance);
            if (zDict.Count == 0)
            {
                yzDict.Remove(vertex.Point.Y);
            }
            if (yzDict.Count == 0)
            {
                _verticesLookup.Remove(vertex.Point.X);
            }
        }

        private Edge AddEdge(ulong vertexId1, ulong vertexId2)
        {
            if (vertexId1 == vertexId2)
            {
                throw new ArgumentException("Can't create edge. The vertices of the edge cannot be the same.", $"{vertexId1}, {vertexId2}");
            }

            var hash = Edge.GetHash(new List<ulong> { vertexId1, vertexId2 });

            if (!this._edgesLookup.TryGetValue(hash, out var edgeId))
            {
                var edge = new Edge(this, this._edgeId, vertexId1, vertexId2);
                edgeId = edge.Id;

                this._edgesLookup[hash] = edgeId;
                this._edges.Add(edgeId, edge);

                this.GetVertex(edge.StartId).Edges.Add(edge);
                this.GetVertex(edge.EndId).Edges.Add(edge);

                this._edgeId++;
                return edge;
            }
            else
            {
                this._edges.TryGetValue(edgeId, out var edge);
                return edge;
            }
        }

        private void DeleteEdge(Edge edge)
        {
            var hash = Edge.GetHash(new List<ulong> { edge.StartId, edge.EndId });
            this._edgesLookup.Remove(hash);
            this._edges.Remove(edge.Id);

            var startVertexEdges = this.GetVertex(edge.StartId).Edges;
            startVertexEdges.Remove(edge);
            if (!startVertexEdges.Any())
            {
                DeleteVertex(edge.StartId);
            }
            var endVertexEdges = this.GetVertex(edge.EndId).Edges;
            endVertexEdges.Remove(edge);
            if (!endVertexEdges.Any())
            {
                DeleteVertex(edge.EndId);
            }
        }

        private Grid2d CreateGridFromPolygon(Polygon boundingPolygon)
        {
            var boundingPolygonPlane = boundingPolygon.Plane();
            var primaryAxisDirection = Transform.XAxis - Transform.XAxis.Dot(boundingPolygonPlane.Normal) * boundingPolygonPlane.Normal;
            if (primaryAxisDirection.IsZero())
            {
                primaryAxisDirection = Transform.ZAxis - Transform.ZAxis.Dot(boundingPolygonPlane.Normal) * boundingPolygonPlane.Normal;
            }
            return new Grid2d(boundingPolygon, new Transform(boundingPolygon.Vertices.FirstOrDefault(), primaryAxisDirection, boundingPolygon.Normal()));
        }

        private void SplitGridAtIntersectionPoints(Polygon boundingPolygon, Grid2d grid)
        {
            var boundingPolygonPlane = boundingPolygon.Plane();
            var intersectionPoints = new List<Vector3>();
            foreach (var edge in GetEdges())
            {
                if (edge.GetGeometry().Intersects(boundingPolygonPlane, out var intersectionPoint)
                    && boundingPolygon.Contains(intersectionPoint))
                {
                    intersectionPoints.Add(intersectionPoint);
                }
            }
            grid.U.SplitAtPoints(intersectionPoints);
            grid.V.SplitAtPoints(intersectionPoints);
        }

        private void AddVerticalEdges(Vector3 extrusionAxis, double height, HashSet<Edge> addedEdges)
        {
            foreach (var bottomVertex in addedEdges.SelectMany(e => e.GetVertices()).Distinct())
            {
                var heightVector = height * extrusionAxis;
                var topPoint = bottomVertex.Point + heightVector;
                AddEdge(new Line(bottomVertex.Point, topPoint));
            }
        }

        /// <summary>
        /// A version of TryGetValue on a dictionary that optionally takes in a tolerance when running the comparison.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key">Number to search for.</param>
        /// <param name="value">Value if match was found.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search for the key.</param>
        /// <typeparam name="T">The type of the dictionary values.</typeparam>
        /// <returns>Whether a match was found.</returns>
        private static bool TryGetValue<T>(Dictionary<double, T> dict, double key, out T value, double? fuzzyFactor = null)
        {
            if (dict.TryGetValue(key, out value))
            {
                return true;
            }
            if (fuzzyFactor != null)
            {
                foreach (var curKey in dict.Keys)
                {
                    if (Math.Abs(curKey - key) <= fuzzyFactor)
                    {
                        value = dict[curKey];
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// In a dictionary of x, y, and z coordinates, gets last level dictionary of z values.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="point"></param>
        /// <param name="addAddressIfNonExistent">Whether to create the dictionary address if it didn't previously exist.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search against each component of the coordinate.</param>
        /// <returns>The created or existing last level of values. This can be null if the dictionary address didn't exist previously, and we chose not to add it.</returns>
        private static Dictionary<double, ulong> GetAddressParent(Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> dict, Vector3 point, bool addAddressIfNonExistent = false, double? fuzzyFactor = null)
        {
            if (!TryGetValue(dict, point.X, out var yzDict, fuzzyFactor))
            {
                if (addAddressIfNonExistent)
                {
                    yzDict = new Dictionary<double, Dictionary<double, ulong>>();
                    dict.Add(point.X, yzDict);
                }
                else
                {
                    return null;
                }
            }

            if (!TryGetValue(yzDict, point.Y, out var zDict, fuzzyFactor))
            {
                if (addAddressIfNonExistent)
                {
                    zDict = new Dictionary<double, ulong>();
                    yzDict.Add(point.Y, zDict);
                }
                else
                {
                    return null;
                }
            }

            return zDict;
        }

        #endregion
    }
}
