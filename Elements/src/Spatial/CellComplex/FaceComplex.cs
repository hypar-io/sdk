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
    public class FaceComplex : Elements.Element
    {
        /// <summary>
        /// Tolerance for points being considered the same.
        /// Applies individually to X, Y, and Z coordinates, not the cumulative difference!
        /// </summary>
        public double Tolerance = Vector3.EPSILON;

        private ulong _edgeId = 1; // we start at 1 because 0 is returned as default value from dicts

        private ulong _directedEdgeId = 1; // we start at 1 because 0 is returned as default value from dicts

        private ulong _vertexId = 1; // we start at 1 because 0 is returned as default value from dicts

        private ulong _faceId = 1; // we start at 1 because 0 is returned as default value from dicts

        private ulong _orientationId = 1; // we start at 1 because 0 is returned as default value from dicts

        /// <summary>
        /// Vertices by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Vertex> _vertices = new Dictionary<ulong, Vertex>();

        /// <summary>
        /// U or V directions by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Orientation> _orientations = new Dictionary<ulong, Orientation>();

        /// <summary>
        /// Edges by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Edge> _edges = new Dictionary<ulong, Edge>();

        /// <summary>
        /// DirectedEdges by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, DirectedEdge> _directedEdges = new Dictionary<ulong, DirectedEdge>();

        /// <summary>
        /// Faces by ID.
        /// </summary>
        [JsonProperty]
        private Dictionary<ulong, Face> _faces = new Dictionary<ulong, Face>();

        // Vertex lookup by x, y, z coordinate.
        private Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> _verticesLookup = new Dictionary<double, Dictionary<double, Dictionary<double, ulong>>>();

        // Orientation lookup by x, y, z coordinate.
        private Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> _orientationsLookup = new Dictionary<double, Dictionary<double, Dictionary<double, ulong>>>();

        // See Edge.GetHash for how faces are identified as unique.
        private Dictionary<string, ulong> _edgesLookup = new Dictionary<string, ulong>();

        // Same as edgesLookup, with an addition level of dictionary for whether lesserVertexId is the start point or not
        private Dictionary<(ulong, ulong), Dictionary<Boolean, ulong>> _directedEdgesLookup = new Dictionary<(ulong, ulong), Dictionary<Boolean, ulong>>();

        // See Face.GetHash for how faces are identified as unique.
        /// <summary>
        /// See <see cref="Face.GetHash(List{DirectedEdge})"/>.
        /// </summary>
        private Dictionary<string, ulong> _facesLookup = new Dictionary<string, ulong>();

        /// <summary>
        /// Create a FaceComplex.
        /// </summary>
        /// <param name="id">Optional ID: If blank, a new Guid will be created.</param>
        /// <param name="name">Optional name of your FaceComplex.</param>
        /// <returns></returns>
        public FaceComplex(Guid id = default(Guid), string name = null) : base(id != default(Guid) ? id : Guid.NewGuid(), name) { }

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
        /// <returns></returns>
        [JsonConstructor]
        internal FaceComplex(
            Guid id,
            string name,
            Dictionary<ulong, Vertex> _vertices,
            Dictionary<ulong, Vertex> _orientations,
            Dictionary<ulong, Edge> _edges,
            Dictionary<ulong, DirectedEdge> _directedEdges,
            Dictionary<ulong, Face> _faces
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
                var polygon = face.GetGeometry();
                var orientation = face.GetOrientation();
                if (!this.AddFace(polygon, face.Id, orientation.U, orientation.V, out var addedFace))
                {
                    throw new Exception("Duplicate face ID found");
                }
            }
        }

        /// <summary>
        /// Create a FaceComplex.
        /// </summary>
        /// <param name="bBox">The bounding box of FaceComplex.</param>
        /// <param name="xSpacing">Faces length along the x-axis.</param>
        /// <param name="ySpacing">Faces length along the y-axis</param>
        /// <param name="zSpacing">Faces length along the z-axis</param>
        /// <param name="id">Optional ID: If blank, a new Guid will be created.</param>
        /// <param name="name">Optional name of your FaceComplex.</param>
        public FaceComplex(BBox3 bBox, double xSpacing, double ySpacing, double zSpacing, Guid id = default(Guid), string name = null) : this(id, name)
        {
            AddFaceComplex(bBox, xSpacing, ySpacing, zSpacing, true);
        }

        #region add content

        /// <summary>
        /// Add face complex.
        /// </summary>
        /// <param name="bBox">The bounding box of FaceComplex.</param>
        /// <param name="xSpacing">Faces length along the x-axis.</param>
        /// <param name="ySpacing">Faces length along the y-axis</param>
        /// <param name="zSpacing">Faces length along the z-axis</param>
        public void AddFaceComplex(BBox3 bBox, double xSpacing, double ySpacing, double zSpacing)
        {
            AddFaceComplex(bBox, xSpacing, ySpacing, zSpacing, false);
        }

        private void AddFaceComplex(BBox3 bBox, double xSpacing, double ySpacing, double zSpacing, bool addFacesWithoutCheck)
        {
            var boundary = new Polygon(new List<Vector3>
            {
                new Vector3(bBox.Min.X, bBox.Min.Y),
                new Vector3(bBox.Min.X, bBox.Max.Y),
                new Vector3(bBox.Max.X, bBox.Max.Y),
                new Vector3(bBox.Max.X, bBox.Min.Y)
            });
            var elevation = bBox.Min.Z;

            var grid = new Grid2d(boundary);
            var height = bBox.Max.Z - bBox.Min.Z;
            var gridZ = new Grid1d(height);
            grid.U.DivideByFixedLength(xSpacing);
            grid.V.DivideByFixedLength(ySpacing);
            gridZ.DivideByFixedLength(zSpacing);
            var zCells = gridZ.GetCells();
            var up = this.AddVertexOrOrientation<Orientation>(new Vector3(0, 0, 1));
            for (var i = 0; i < zCells.Count; i++)
            {
                var elevationVector = new Vector3(0, 0, elevation + zCells[i].Domain.Min);
                var edges = new HashSet<Line>(new LineComparer());
                foreach (var cell in grid.GetCells())
                {
                    foreach (var crv in cell.GetTrimmedCellGeometry())
                    {
                        var polygon = (Polygon)crv;
                        var transformedPolygonBottom = (Polygon)polygon.Transformed(new Transform(elevationVector));

                        var uGrid = grid.U;
                        var vGrid = grid.V;

                        AddOrSplitFace(transformedPolygonBottom, uGrid, vGrid, addFacesWithoutCheck);

                        if (i == zCells.Count - 1)
                        {
                            var transformedPolygonTop = (Polygon)polygon.Transformed(new Transform(new Vector3(0, 0, elevation + zCells[i].Domain.Max)));
                            AddOrSplitFace(transformedPolygonTop, uGrid, vGrid, addFacesWithoutCheck);
                        }

                        foreach (var faceEdge in transformedPolygonBottom.Segments())
                        {
                            if (!edges.Contains(faceEdge))
                            {
                                edges.Add(faceEdge);
                                var vertices = new List<Vector3>() { faceEdge.Start, faceEdge.End };
                                var horizontalU = this.AddVertexOrOrientation<Orientation>((faceEdge.End - faceEdge.Start).Unitized());
                                var heightVector = new Vector3(0, 0, zCells[i].Domain.Length);
                                vertices.Add(faceEdge.End + heightVector);
                                vertices.Add(faceEdge.Start + heightVector);
                                var facePoly = new Polygon(vertices);
                                AddOrSplitFace(facePoly, horizontalU, up, addFacesWithoutCheck);
                            }
                        }
                    }
                }
            }
        }

        private void AddOrSplitFace(Polygon polygon, Orientation u = null, Orientation v = null, bool addWithoutCheck = false)
        {
            if (addWithoutCheck)
            {
                AddFace(polygon, u, v);
                return;
            }

            var polygonTransform = polygon.ToTransform();
            var inverseTransform = new Transform(polygonTransform);
            inverseTransform.Invert();
            var polygonInXY = polygon.TransformedPolygon(inverseTransform);
            var intersectedFaces = GetFaces()
                .Select(face => new { face, geometry = face.GetGeometry().TransformedPolygon(inverseTransform) })
                .Where(face => polygonInXY.Vertices.Union(face.geometry.Vertices).ToList().AreCoplanar()
                               && polygonInXY.Intersects(face.geometry));

            if (!intersectedFaces.Any())
            {
                AddFace(polygon, u, v);
                return;
            }

            foreach (var face in intersectedFaces)
            {
                var hash = Face.GetHash(face.face.GetDirectedEdges());
                _facesLookup.TryGetValue(hash, out var id);
                _faces.Remove(id);
                _facesLookup.Remove(hash);
                var intersections = polygonInXY.Intersection(face.geometry);
                if (intersections != null)
                {
                    foreach (var intersection in intersections)
                    {
                        AddFace(intersection.TransformedPolygon(polygonTransform), u, v);
                    }
                }
                var faceDiffs = face.geometry.Difference(polygonInXY);
                if (faceDiffs != null)
                {
                    foreach (var diff in faceDiffs)
                    {
                        AddFace(diff.TransformedPolygon(polygonTransform), u, v);
                    }
                }
            }
            var polygonDiffs = polygonInXY.Difference(intersectedFaces.Select(face => face.geometry).ToList());
            if (polygonDiffs != null)
            {
                foreach (var diff in polygonDiffs)
                {
                    AddFace(diff.TransformedPolygon(polygonTransform), u, v);
                }
            }

            // TODO If new faces are added to the side of existing faces, such that they must have a common edge,
            // edges of faces must be replaced with new edges.
            // For example, if existing face has edge AB and new face has edge AC and point C lies between A and B,
            // then edge AB in existing face should be replaced with edges AC, CB.
        }

        private void AddOrSplitFace(Polygon polygon, Grid1d uGrid = null, Grid1d vGrid = null, bool addWithoutCheck = false)
        {
            Orientation u = null;
            Orientation v = null;
            if (uGrid != null)
            {
                u = AddVertexOrOrientation<Orientation>(uGrid.Direction().Unitized());
            }
            if (vGrid != null)
            {
                v = AddVertexOrOrientation<Orientation>(vGrid.Direction().Unitized());
            }

            AddVertexOrOrientation<Orientation>(new Vector3(0, 0, 1));

            AddOrSplitFace(polygon, u, v, addWithoutCheck);
        }

        /// <summary>
        /// Add a Face to the FaceComplex.
        /// </summary>
        /// <param name="polygon">A polygon representing a unique Face.</param>
        /// <param name="u">Orientation of U axis.</param>
        /// <param name="v">Orientation of V axis.</param>
        /// <returns>The created Face.</returns>
        protected Face AddFace(Polygon polygon, Orientation u = null, Orientation v = null)
        {
            this.AddFace(polygon, this._faceId, u, v, out var face);
            return face;
        }

        /// <summary>
        /// Add a DirectedEdge to the FaceComplex.
        /// </summary>
        /// <param name="line">Line with Start and End in the expected direction.</param>
        /// <returns>The created DirectedEdge.</returns>
        private protected DirectedEdge AddDirectedEdge(Line line)
        {
            var points = new List<Vector3>() { line.Start, line.End };
            var vertices = points.Select(vertex => this.AddVertexOrOrientation<Vertex>(vertex)).ToList();
            this.AddEdge(vertices.Select(v => v.Id).ToList(), this._edgeId, out var edge);
            var dirMatchesEdge = vertices[0].Id == edge.StartVertexId;
            this.AddDirectedEdge(edge, dirMatchesEdge, this._directedEdgeId, out var directedEdge);
            return directedEdge;
        }

        /// <summary>
        /// The lowest-level method to add a Face: all other AddFace methods will eventually call this one.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="idIfNew"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="face"></param>
        /// <returns>Whether the face was successfully added. Will be false if idIfNew already exists</returns>
        protected Boolean AddFace(Polygon polygon, ulong idIfNew, Orientation u, Orientation v, out Face face)
        {
            var lines = polygon.Segments();
            var directedEdges = new List<DirectedEdge>();
            foreach (var line in lines)
            {
                directedEdges.Add(this.AddDirectedEdge(line));
            }

            var hash = Face.GetHash(directedEdges);

            if (!this._facesLookup.TryGetValue(hash, out var faceId))
            {
                face = new Face(this, idIfNew, directedEdges, u, v);
                faceId = face.Id;
                this._facesLookup.Add(hash, faceId);
                this._faces.Add(faceId, face);

                foreach (var directedEdge in directedEdges)
                {
                    directedEdge.Faces.Add(face);
                }

                this._faceId = Math.Max(idIfNew + 1, this._faceId + 1);
                return true;
            }
            else
            {
                this._faces.TryGetValue(faceId, out face);
                return false;
            }
        }

        protected Face AddFace(Polygon polygon, Grid1d u = null, Grid1d v = null)
        {
            this.AddFace(polygon, this._faceId, u, v, out var face);
            return face;
        }

        protected Boolean AddFace(Polygon polygon, ulong idIfNew, Grid1d uGrid, Grid1d vGrid, out Face face)
        {
            Orientation u = null;
            Orientation v = null;
            if (uGrid != null)
            {
                u = AddVertexOrOrientation<Orientation>(uGrid.Direction().Unitized());
            }
            if (vGrid != null)
            {
                v = AddVertexOrOrientation<Orientation>(vGrid.Direction().Unitized());
            }

            this.AddVertexOrOrientation<Orientation>(new Vector3(0, 0, 1));

            return this.AddFace(polygon, idIfNew, u, v, out face);
        }

        /// <summary>
        /// The lowest-level method to add a DirectedEdge: all other AddDirectedEdge methods will eventually call this one.
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="edgeTupleIsInOrder"></param>
        /// <param name="idIfNew"></param>
        /// <param name="directedEdge"></param>
        /// <returns>Whether the directedEdge was successfully added. Will be false if idIfNew already exists.</returns>
        private protected Boolean AddDirectedEdge(Edge edge, Boolean edgeTupleIsInOrder, ulong idIfNew, out DirectedEdge directedEdge)
        {
            var edgeTuple = (edge.StartVertexId, edge.EndVertexId);

            if (!this._directedEdgesLookup.TryGetValue(edgeTuple, out var directedEdgeDict))
            {
                directedEdgeDict = new Dictionary<bool, ulong>();
                this._directedEdgesLookup.Add(edgeTuple, directedEdgeDict);
            }

            if (!directedEdgeDict.TryGetValue(edgeTupleIsInOrder, out var directedEdgeId))
            {
                directedEdge = new DirectedEdge(this, idIfNew, edge, edgeTupleIsInOrder);
                directedEdgeId = directedEdge.Id;

                directedEdgeDict.Add(edgeTupleIsInOrder, directedEdgeId);
                this._directedEdges.Add(directedEdgeId, directedEdge);

                edge.DirectedEdges.Add(directedEdge);

                this._directedEdgeId = Math.Max(directedEdgeId + 1, this._directedEdgeId + 1);

                return true;
            }
            else
            {
                this._directedEdges.TryGetValue(directedEdgeId, out directedEdge);

                return false;
            }
        }

        /// <summary>
        /// The lowest-level method to add an Edge: all other AddEdge methods will eventually call this one.
        /// </summary>
        /// <param name="vertexIds"></param>
        /// <param name="idIfNew"></param>
        /// <param name="edge"></param>
        /// <returns>Whether the edge was successfully added. Will be false if idIfNew already exists.</returns>
        protected Boolean AddEdge(List<ulong> vertexIds, ulong idIfNew, out Edge edge)
        {
            var hash = Edge.GetHash(vertexIds);

            if (!this._edgesLookup.TryGetValue(hash, out var edgeId))
            {
                edge = new Edge(this, idIfNew, vertexIds[0], vertexIds[1]);
                edgeId = edge.Id;

                this._edgesLookup[hash] = edgeId;
                this._edges.Add(edgeId, edge);

                this.GetVertex(edge.StartVertexId).Edges.Add(edge);
                this.GetVertex(edge.EndVertexId).Edges.Add(edge);

                this._edgeId = Math.Max(edgeId + 1, this._edgeId + 1);

                return true;
            }
            else
            {
                this._edges.TryGetValue(edgeId, out edge);
                return false;
            }
        }

        /// <summary>
        /// Utility to get the address dictionary of Vertices or Orientations
        /// </summary>
        /// <typeparam name="T">Vertex or Orientation.</typeparam>
        /// <returns></returns>
        protected Dictionary<ulong, T> GetVertexOrOrientationDictionary<T>() where T : VertexBase<T>
        {
            if (typeof(T) != typeof(Orientation) && typeof(T) != typeof(Vertex))
            {
                throw new Exception("Unsupported type provided, expected Vertex or Orientation");
            }
            return typeof(T) == typeof(Orientation) ? this._orientations as Dictionary<ulong, T> : this._vertices as Dictionary<ulong, T>;
        }

        /// <summary>
        /// Utility to get the lookup dictionary of Vertices or Orientations.
        /// </summary>
        /// <typeparam name="T">Vertex or Orientation.</typeparam>
        /// <returns></returns>
        protected Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> GetVertexOrOrientationLookup<T>() where T : VertexBase<T>
        {
            if (typeof(T) != typeof(Orientation) && typeof(T) != typeof(Vertex))
            {
                throw new Exception("Unsupported type provided, expected Vertex or Orientation");
            }
            return typeof(T) == typeof(Orientation) ? this._orientationsLookup : this._verticesLookup;
        }

        /// <summary>
        /// Add a Vertex or an Orientation by its location or vector direction.
        /// </summary>
        /// <param name="point">This represents the point in space if this is a Vertex, or the orientation vector if it is an Orientation.</param>
        /// <typeparam name="T">Vertex or Orientation.</typeparam>
        /// <returns>The created or existing Vertex or Orientation.</returns>
        protected T AddVertexOrOrientation<T>(Vector3 point) where T : VertexBase<T>
        {
            var newId = typeof(T) == typeof(Orientation) ? this._orientationId : this._vertexId;
            var dict = GetVertexOrOrientationDictionary<T>();
            this.AddVertexOrOrientation<T>(point, newId, out var addedId);
            dict.TryGetValue(addedId, out var vertexOrOrientation);
            return vertexOrOrientation as T;
        }

        /// <summary>
        /// Add a Vertex or an Orientation by its location or vector direction, and its ID.
        /// This should only be used during deserialization, when we know exactly what our IDs are ahead of time.
        /// </summary>
        /// <param name="point">This represents the point in space if this is a Vertex, or the orientation vector if it is an Orientation.</param>
        /// <param name="id">ID of the item to create.</param>
        /// <typeparam name="T">Vertex or Orientation.</typeparam>
        /// <returns>The created Vertex or Orientation.</returns>
        protected T AddVertexOrOrientation<T>(Vector3 point, ulong id) where T : VertexBase<T>
        {
            var dict = GetVertexOrOrientationDictionary<T>();

            this.AddVertexOrOrientation<T>(point, id, out var addedId);
            if (addedId != id)
            {
                throw new Exception("This ID already exists");
            }
            dict.TryGetValue(addedId, out var vertexOrOrientation);
            return vertexOrOrientation as T;
        }

        /// <summary>
        /// The lowest-level method to add a Vertex or Orientation: all other AddVertexOrOrientation methods will eventually call this one.
        /// </summary>
        /// <param name="point">This represents the point in space if this is a Vertex, or the orientation vector if it is an Orientation.</param>
        /// <param name="idIfNew">ID to use if we want to create a new item.</param>
        /// <param name="id">ID of created or existing item.</param>
        /// <typeparam name="T">Vertex or Orientation.</typeparam>
        /// <returns>Whether the item was successfully added. Will be false if idIfNew already exists.</returns>
        protected Boolean AddVertexOrOrientation<T>(Vector3 point, ulong idIfNew, out ulong id) where T : VertexBase<T>
        {
            var lookups = this.GetVertexOrOrientationLookup<T>();
            var dict = this.GetVertexOrOrientationDictionary<T>();
            var isOrientation = typeof(T) == typeof(Orientation);
            if (ValueExists(lookups, point, out id, Tolerance))
            {
                return false;
            }
            var zDict = GetAddressParent(lookups, point, true);
            var vertexOrOrientation = isOrientation ? new Orientation(this, idIfNew, point) as T : new Vertex(this, idIfNew, point) as T;
            id = vertexOrOrientation.Id;
            zDict.Add(point.Z, id);
            dict.Add(id, vertexOrOrientation);
            if (isOrientation)
            {
                this._orientationId = Math.Max(id + 1, this._orientationId + 1);
            }
            else
            {
                this._vertexId = Math.Max(id + 1, this._vertexId + 1);
            }
            return true;
        }

        #endregion add content

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
        /// Get a U or V direction by its ID.
        /// </summary>
        /// <param name="orientationId"></param>
        /// <returns></returns>
        public Orientation GetOrientation(ulong? orientationId)
        {
            if (orientationId == null)
            {
                return null;
            }
            this._orientations.TryGetValue((ulong)orientationId, out var orientation);
            return orientation;
        }

        /// <summary>
        /// Get all Orientations.
        /// </summary>
        /// <returns></returns>
        internal List<Orientation> GetOrientations()
        {
            return this._orientations.Values.ToList();
        }

        /// <summary>
        /// Get a Edge by its ID.
        /// </summary>
        /// <param name="edgeId"></param>
        /// <returns></returns>
        public Edge GetEdge(ulong edgeId)
        {
            this._edges.TryGetValue(edgeId, out var edge);
            return edge;
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
        /// Get a Face by its ID.
        /// </summary>
        /// <param name="faceId"></param>
        /// <returns></returns>
        public Face GetFace(ulong? faceId)
        {
            if (faceId == null)
            {
                return null;
            }
            this._faces.TryGetValue((ulong)faceId, out var face);
            return face;
        }

        /// <summary>
        /// Get all Faces.
        /// </summary>
        /// <returns></returns>
        public List<Face> GetFaces()
        {
            return this._faces.Values.ToList();
        }

        /// <summary>
        /// Get the associated Vertex that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vertex GetClosestVertex(Vector3 point)
        {
            return Vertex.GetClosest<Vertex>(this.GetVertices(), point);
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
        /// Get the associated Face that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Face GetClosestFace(Vector3 point)
        {
            return Face.GetClosest<Face>(this.GetFaces(), point);
        }

        /// <summary>
        /// Get a DirectedEdge by its ID.
        /// </summary>
        /// <param name="directedEdgeId"></param>
        /// <returns></returns>
        internal DirectedEdge GetDirectedEdge(ulong directedEdgeId)
        {
            this._directedEdges.TryGetValue(directedEdgeId, out var directedEdge);
            return directedEdge;
        }

        /// <summary>
        /// Get all DirectedEdges.
        /// </summary>
        /// <returns></returns>
        internal List<DirectedEdge> GetDirectedEdges()
        {
            return this._directedEdges.Values.ToList();
        }

        /// <summary>
        /// Get all vertices matching an X/Y coordinate, regardless of Z.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search against each component of the coordinate.</param>
        /// <returns></returns>
        public List<Vertex> GetVerticesMatchingXY(double x, double y, Nullable<double> fuzzyFactor = null)
        {
            var vertices = new List<Vertex>();
            var zDict = GetAddressParent(this._verticesLookup, new Vector3(x, y), fuzzyFactor: fuzzyFactor);
            if (zDict == null)
            {
                return vertices;
            }
            return zDict.Values.Select(id => this.GetVertex(id)).ToList();
        }

        /// <summary>
        /// Whether a vertex location already exists in the FaceComplex.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="id">The ID of the Vertex, if a match is found.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search against each component of the coordinate.</param>
        /// <returns></returns>
        public Boolean VertexExists(Vector3 point, out ulong id, Nullable<double> fuzzyFactor = null)
        {
            return ValueExists(this._verticesLookup, point, out id, fuzzyFactor);
        }

        /// <summary>
        /// Add a value to a Dictionary of HashSets of longs. Used as a utility for internal lookups.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static HashSet<ulong> AddValue(Dictionary<ulong, HashSet<ulong>> dict, ulong key, ulong value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, new HashSet<ulong>());
            }
            dict.TryGetValue(key, out var set);
            set.Add(value);
            return set;
        }

        /// <summary>
        /// In a dictionary of x, y, and z coordinates, gets last level dictionary of z values.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="point"></param>
        /// <param name="addAddressIfNonExistent">Whether to create the dictionary address if it didn't previously exist.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search against each component of the coordinate.</param>
        /// <returns>The created or existing last level of values. This can be null if the dictionary address didn't exist previously, and we chose not to add it.</returns>
        protected static Dictionary<double, ulong> GetAddressParent(Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> dict, Vector3 point, Boolean addAddressIfNonExistent = false, Nullable<double> fuzzyFactor = null)
        {
            if (!TryGetValue<Dictionary<double, Dictionary<double, ulong>>>(dict, point.X, out var yzDict, fuzzyFactor))
            {
                yzDict = new Dictionary<double, Dictionary<double, ulong>>();
                if (addAddressIfNonExistent)
                {
                    dict.Add(point.X, yzDict);
                }
                else
                {
                    return null;
                }
            }

            if (!TryGetValue<Dictionary<double, ulong>>(yzDict, point.Y, out var zDict, fuzzyFactor))
            {
                zDict = new Dictionary<double, ulong>();
                if (addAddressIfNonExistent)
                {
                    yzDict.Add(point.Y, zDict);
                }
                else
                {
                    return null;
                }
            }

            return zDict;
        }

        /// <summary>
        /// In a dictionary of x, y, and z coordinates, whether a point value is represented.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="point"></param>
        /// <param name="id">ID of the found vertex or orientation, if found.</param>
        /// <param name="fuzzyFactor">Amount of tolerance in the search against each component of the coordinate.</param>
        /// <returns></returns>
        protected static Boolean ValueExists(Dictionary<double, Dictionary<double, Dictionary<double, ulong>>> dict, Vector3 point, out ulong id, Nullable<double> fuzzyFactor = null)
        {
            var zDict = GetAddressParent(dict, point, fuzzyFactor: fuzzyFactor);
            if (zDict == null)
            {
                id = 0;
                return false;
            }
            return TryGetValue<ulong>(zDict, point.Z, out id, fuzzyFactor);
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
        protected static Boolean TryGetValue<T>(Dictionary<double, T> dict, double key, out T value, Nullable<double> fuzzyFactor = null)
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

        private class LineComparer : IEqualityComparer<Line>
        {
            public bool Equals(Line x, Line y)
            {
                if (x == y)
                {
                    return true;
                }

                if (x.Start.Equals(y.Start) && x.End.Equals(y.End)
                    || x.Start.Equals(y.End) && x.End.Equals(y.Start))
                {
                    return true;
                }

                return false;
            }

            public int GetHashCode(Line obj)
            {
                return obj.Start.GetHashCode() + obj.End.GetHashCode();
            }
        }
    }
}