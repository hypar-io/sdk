using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Elements.Geometry;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A unique edge in a cell complex, regardless of directionality when it comes to face traversal.
    /// Directional edges for this purpose are in associated DirectedEdges.
    /// There is a maximum of two DirectedEdges per Edge.
    /// </summary>
    public class Edge : EdgeBase<Edge>, Interfaces.IHasNeighbors<Edge, Line>
    {
        /// <summary>
        /// DirectedEdges that reference this Edge.
        /// </summary>
        [JsonIgnore]
        internal HashSet<DirectedEdge> DirectedEdges = new HashSet<DirectedEdge>();

        /// <summary>
        /// Represents a unique Edge within a FaceComplex.
        /// Is not intended to be created or modified outside of the FaceComplex class code.
        /// </summary>
        /// <param name="faceComplex">FaceComplex that this belongs to.</param>
        /// <param name="id">Edge ID.</param>
        /// <param name="vertexId1">One of the vertex IDs for this edge.</param>
        /// <param name="vertexId2">The other vertex ID for this edge.</param>
        internal Edge(FaceComplex faceComplex, ulong id, ulong vertexId1, ulong vertexId2) : base(id, faceComplex)
        {
            this.SetVerticesFromIds(vertexId1, vertexId2);
        }

        /// <summary>
        /// Used for deserialization only!
        /// Simply omits the associated FaceComplex, which we manually add in the FaceComplex deserializer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="startVertexId"></param>
        /// <param name="endVertexId"></param>
        /// <returns></returns>
        [JsonConstructor]
        internal Edge(ulong id, ulong startVertexId, ulong endVertexId) : base(id, null)
        {
            this.SetVerticesFromIds(startVertexId, endVertexId);
        }

        /// <summary>
        /// Sets the StartVertexId and EndVertexId so that start vertex always has a smaller ID than end vertex.
        /// </summary>
        /// <param name="id1">One of the two applicable vertex IDs.</param>
        /// <param name="id2">The other applicable vertex IDs.</param>
        private void SetVerticesFromIds(ulong id1, ulong id2)
        {
            if (id1 < id2)
            {
                this.StartVertexId = id1;
                this.EndVertexId = id2;
            }
            else
            {
                this.EndVertexId = id1;
                this.StartVertexId = id2;
            }
        }

        /// <summary>
        /// Get the unique hash for an Edge with list (of length 2) of its unordered vertex IDs.
        /// </summary>
        /// <param name="vertexIds"></param>
        /// <returns></returns>
        internal static string GetHash(List<ulong> vertexIds)
        {
            var sortedIds = vertexIds.ToList();
            sortedIds.Sort();
            var hash = String.Join(",", sortedIds);
            return hash;
        }

        /// <summary>
        /// Get associated Vertices.
        /// </summary>
        /// <returns></returns>
        public List<Vertex> GetVertices()
        {
            return new List<Vertex>() { this.FaceComplex.GetVertex(this.StartVertexId), this.FaceComplex.GetVertex(this.EndVertexId) };
        }

        /// <summary>
        /// Get associated Faces.
        /// </summary>
        /// <returns></returns>
        public List<Face> GetFaces()
        {
            return this.GetDirectedEdges().Select(ds => ds.GetFaces()).SelectMany(x => x).Distinct().ToList();
        }

        /// <summary>
        /// Get associated Cells
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetCells()
        {
            return this.GetFaces().Select(face => face.GetCells()).SelectMany(x => x).Distinct().ToList();
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
        /// Get the associated Face that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Face GetClosestFace(Vector3 point)
        {
            return Face.GetClosest<Face>(this.GetFaces(), point);
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
        /// Get a list of all neighboring Edges that share a Vertex.
        /// </summary>
        /// <returns></returns>
        public List<Edge> GetNeighbors()
        {
            return this.GetVertices().Select(vertex => vertex.GetEdges()).SelectMany(x => x).Where(e => e.Id != this.Id).Distinct().ToList();
        }

        /// <summary>
        /// Get the closest neighboring Edge to a point.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Edge GetClosestNeighbor(Vector3 target)
        {
            return Edge.GetClosest<Edge>(this.GetNeighbors().Where(e => e.DistanceTo(target) < this.DistanceTo(target)).ToList(), target);
        }

        /// <summary>
        /// Get associated DirectedEdges.
        /// </summary>
        /// <returns></returns>
        internal List<DirectedEdge> GetDirectedEdges()
        {
            return this.DirectedEdges.ToList();
        }

        /// <summary>
        /// Traverse the neighbors of this Edge toward the target point.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="completedRadius">If provided, ends the traversal when the neighbor is within this distance to the target point.</param>
        /// <returns>A collection of traversed Edges, including the starting Edge.</returns>
        public List<Edge> TraverseNeighbors(Vector3 target, double completedRadius = 0)
        {
            var maxCount = this.FaceComplex.GetEdges().Count;
            Func<Edge, Edge> getNextNeighbor = (Edge curNeighbor) => (curNeighbor.GetClosestNeighbor(target));
            return Edge.TraverseNeighbors(this, maxCount, target, completedRadius, getNextNeighbor);
        }
    }
}