using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elements.Spatial.AdaptiveGrid
{
    public class Edge
    {
        /// <summary>
        /// ID of start Vertex.
        /// </summary>
        public ulong StartId;

        /// <summary>
        /// ID of end Vertex.
        /// </summary>
        public ulong EndId;

        public AdaptiveGrid AdaptiveGrid { get; private set; }

        /// <summary>
        /// ID of this child.
        /// </summary>
        public ulong Id { get; internal set; }

        internal Edge(AdaptiveGrid adaptiveGrid, ulong id, ulong vertexId1, ulong vertexId2)
        {
            AdaptiveGrid = adaptiveGrid;
            Id = id;

            this.SetVerticesFromIds(vertexId1, vertexId2);
        }

        public override bool Equals(object obj)
        {
            return obj is Edge edge && StartId.Equals(edge.StartId) && EndId.Equals(edge.EndId);
        }

        public override int GetHashCode()
        {
            return GetHash(new List<ulong> { StartId, EndId }).GetHashCode();
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
        /// Sets the StartVertexId and EndVertexId so that start vertex always has a smaller ID than end vertex.
        /// </summary>
        /// <param name="id1">One of the two applicable vertex IDs.</param>
        /// <param name="id2">The other applicable vertex IDs.</param>
        private void SetVerticesFromIds(ulong id1, ulong id2)
        {
            if (id1 < id2)
            {
                this.StartId = id1;
                this.EndId = id2;
            }
            else
            {
                this.EndId = id1;
                this.StartId = id2;
            }
        }

        /// <summary>
        /// Get associated Vertices.
        /// </summary>
        /// <returns></returns>
        public List<Vertex> GetVertices()
        {
            return new List<Vertex>() { this.AdaptiveGrid.GetVertex(this.StartId), this.AdaptiveGrid.GetVertex(this.EndId) };
        }

        /// <summary>
        /// Get the geometry that represents this Edge or DirectedEdge.
        /// </summary>
        /// <returns></returns>
        public Line GetGeometry()
        {
            return new Line(
                this.AdaptiveGrid.GetVertex(this.StartId).Point,
                this.AdaptiveGrid.GetVertex(this.EndId).Point
            );
        }
    }
}
