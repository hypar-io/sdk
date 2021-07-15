using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Elements.Geometry;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A Face within a face complex or cell. Multiple cells can share the same Face.
    /// </summary>
    public class Face : ChildBase<Face, Polygon>, Interfaces.IHasNeighbors<Face, Polygon>
    {
        /// <summary>
        /// Directed edge IDs.
        /// </summary>
        public List<ulong> DirectedEdgeIds;

        /// <summary>
        /// ID of U orientation.
        /// </summary>
        [JsonProperty]
        private ulong? _orientationUId;

        /// <summary>
        /// ID of V orientation.
        /// </summary>
        [JsonProperty]
        private ulong? _orientationVId;

        /// <summary>
        /// Cells that reference this Face.
        /// </summary>
        [JsonIgnore]
        internal HashSet<Cell> Cells = new HashSet<Cell>();

        /// <summary>
        /// Represents a unique Face within a FaceComplex.
        /// Is not intended to be created or modified outside of the FaceComplex class code.
        /// </summary>
        /// <param name="faceComplex">FaceComplex that this Face belongs to.</param>
        /// <param name="id">ID of this Face.</param>
        /// <param name="directedEdges">List of the DirectedEdges that make up this Face.</param>
        /// <param name="u">Optional but highly recommended intended U direction for the Face.</param>
        /// <param name="v">Optional but highly recommended intended V direction for the Face.</param>
        internal Face(FaceComplex faceComplex, ulong id, List<DirectedEdge> directedEdges, Orientation u = null, Orientation v = null) : base(id, faceComplex)
        {
            this.DirectedEdgeIds = directedEdges.Select(ds => ds.Id).ToList();
            if (u != null)
            {
                this._orientationUId = u.Id;
            }
            if (v != null)
            {
                this._orientationVId = v.Id;
            }
        }

        /// <summary>
        /// Used for deserialization only!
        /// </summary>
        [JsonConstructor]
        internal Face(ulong id, List<ulong> directedEdgeIds, ulong? _orientationUId = null, ulong? _orientationVId = null) : base(id, null)
        {
            this.Id = id;
            this.DirectedEdgeIds = directedEdgeIds;
            this._orientationUId = _orientationUId;
            this._orientationVId = _orientationVId;
        }

        /// <summary>
        /// Get the geometry for this Face.
        /// </summary>
        /// <returns></returns>
        public override Polygon GetGeometry()
        {
            return new Polygon(this.GetVertices().Select(v => v.Value).ToList());
        }

        /// <summary>
        ///  Get the shortest distance from a point to the geometry representing this face.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override double DistanceTo(Vector3 point)
        {
            return point.DistanceTo(this.GetGeometry());
        }

        /// <summary>
        /// Face lookup hash is EdgeIds in ascending order.
        /// We do not directly use the `DirectedEdgeIds` because they could wind differently on a shared face.
        /// </summary>
        /// <param name="directedEdges"></param>
        /// <returns></returns>
        internal static string GetHash(List<DirectedEdge> directedEdges)
        {
            var sortedIds = directedEdges.Select(ds => ds.EdgeId).ToList();
            sortedIds.Sort();
            var hash = String.Join(",", sortedIds);
            return hash;
        }

        /// <summary>
        /// Get associated DirectedEdges.
        /// </summary>
        /// <returns></returns>
        internal List<DirectedEdge> GetDirectedEdges()
        {
            return this.DirectedEdgeIds.Select(dsId => FaceComplex.GetDirectedEdge(dsId)).ToList();
        }

        /// <summary>
        /// Get the normal vector for this Face.
        /// </summary>
        /// <returns></returns>
        private Vector3 GetNormal()
        {
            return this.GetGeometry().Normal();
        }

        /// <summary>
        /// Whether this Face is parallel to another Face.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private bool IsParallel(Face face)
        {
            return face.GetNormal().Equals(this.GetNormal());
        }

        /// <summary>
        /// Get associated Cells.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetCells()
        {
            return this.Cells.ToList();
        }

        /// <summary>
        /// Get associated Vertices.
        /// </summary>
        /// <returns></returns>
        public List<Vertex> GetVertices()
        {
            return this.GetDirectedEdges().Select(ds => this.FaceComplex.GetVertex(ds.StartVertexId)).ToList();
        }

        /// <summary>
        /// Get associated Edges.
        /// </summary>
        /// <returns></returns>
        public List<Edge> GetEdges()
        {
            return this.GetDirectedEdges().Select(ds => ds.GetEdge()).ToList();
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
        /// Get the associated Cell that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Cell GetClosestCell(Vector3 point)
        {
            return Cell.GetClosest<Cell>(this.GetCells(), point);
        }

        /// <summary>
        /// Get the user-set U and V Orientatinos of this face.
        /// </summary>
        /// <returns></returns>
        public (Orientation U, Orientation V) GetOrientation()
        {
            return (U: this.FaceComplex.GetOrientation(this._orientationUId), V: this.FaceComplex.GetOrientation(this._orientationVId));
        }

        /// <summary>
        /// Get a list of all neighbors of this Face.
        /// A neighbor is defined as a Face which shares any Edge.
        /// </summary>
        /// <returns></returns>
        public List<Face> GetNeighbors() {
            return this.GetNeighbors(false, false);
        }

        /// <summary>
        /// Get a list of all neighbors of this Face.
        /// A neighbor is defined as a Face which shares any Edge.
        /// </summary>
        /// <param name="parallel">If true, only returns Faces that are oriented the same way as this Face.</param>
        /// <param name="includeSharedVertices">If true, includes Faces that share a Vertex as well as Faces that share an Edge.</param>
        /// <returns></returns>
        public List<Face> GetNeighbors(bool parallel = false, bool includeSharedVertices = false)
        {
            var groupedFaces = includeSharedVertices ? this.GetVertices().Select(v => v.GetFaces()).ToList() : this.GetEdges().Select(s => s.GetFaces()).ToList();
            var faces = groupedFaces.SelectMany(x => x).Distinct().Where(f => f.Id != this.Id).ToList();
            if (parallel)
            {
                return faces.Where(f => this.IsParallel(f)).ToList();
            }
            else
            {
                return faces;
            }
        }

        /// <summary>
        /// Get a list of neighboring Faces that share a specific Edge.
        /// </summary>
        /// <param name="edge">Edge that the neighbor should share.</param>
        /// <param name="parallel">Whether to only return Faces that are parallel to this Face.</param>
        /// <returns></returns>
        public List<Face> GetNeighbors(Edge edge, bool parallel = false)
        {
            if (!parallel)
            {
                return edge.GetFaces().Where(face => face.Id != this.Id).ToList();
            }
            else
            {
                return edge.GetFaces().Where(face => face.Id != this.Id && this.IsParallel(face)).ToList();
            }
        }

        /// <summary>
        /// Get the closest associated Face to a given point.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Face GetClosestNeighbor(Vector3 target) {
            return this.GetClosestNeighbor(target, false, false);
        }

        /// <summary>
        /// Get the closest associated Face to a given point.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parallel">If true, only checks faces that are oriented the same way as this Face.</param>
        /// <param name="includeSharedVertices">If true, checks Faces that share a Vertex as well as Faces that share a Edge.</param>
        /// <returns></returns>
        public Face GetClosestNeighbor(Vector3 target, bool parallel = false, bool includeSharedVertices = false)
        {
            return Face.GetClosest<Face>(this.GetNeighbors(parallel, includeSharedVertices).Where(f =>
            {
                var d1 = this.DistanceTo(target);
                var d2 = f.DistanceTo(target);
                if (f.DistanceTo(target) < this.DistanceTo(target))
                {

                }
                return f.DistanceTo(target) < this.DistanceTo(target);
            }).ToList(), target);
        }

        /// <summary>
        /// Traverse the neighbors of this Face toward the target point.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="completedRadius"></param>
        /// <returns></returns>
        public List<Face> TraverseNeighbors(Vector3 target, double completedRadius = 0) {
            return this.TraverseNeighbors(target, false, false, completedRadius);
        }

        /// <summary>
        /// Traverse the neighbors of this Face toward the target point.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parallel">If true, only checks faces that are oriented the same way as this Face.</param>
        /// <param name="includeSharedVertices">If true, checks Faces that share a Vertex as well as Faces that share a Edge.</param>
        /// <param name="completedRadius">If provided, ends the traversal when the neighbor is within this distance to the target point.</param>
        /// <returns>A collection of traversed Faces, including the starting Face.</returns>
        public List<Face> TraverseNeighbors(Vector3 target, bool parallel = false, bool includeSharedVertices = false, double completedRadius = 0)
        {
            var maxCount = this.FaceComplex.GetFaces().Count;
            Func<Face, Face> getNextNeighbor = (Face curNeighbor) => (curNeighbor.GetClosestNeighbor(target, parallel, includeSharedVertices));
            return Face.TraverseNeighbors(this, maxCount, target, completedRadius, getNextNeighbor);
        }
    }
}