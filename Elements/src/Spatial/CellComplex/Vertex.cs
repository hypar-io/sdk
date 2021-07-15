using Elements.Geometry;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A unique Vertex in a FaceComplex.
    /// </summary>
    public class Vertex : VertexBase<Vertex>
    {
        /// <summary>
        /// All Edges connected to this Vertex.
        /// </summary>
        [JsonIgnore]
        internal HashSet<Edge> Edges = new HashSet<Edge>();

        /// <summary>
        /// Represents a unique Vertex within a FaceComplex.
        /// Is not intended to be created or modified outside of the CellComplex class code.
        /// </summary>
        /// <param name="faceComplex">FaceComplex that this belongs to</param>
        /// <param name="id"></param>
        /// <param name="point">Location of the vertex</param>
        /// <param name="name">Optional name</param>
        internal Vertex(FaceComplex faceComplex, ulong id, Vector3 point, string name = null) : base(faceComplex, id, point, name) { }

        /// <summary>
        /// For deserialization only!
        /// </summary>
        /// <param name="id"></param>
        /// <param name="point"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [JsonConstructor]
        internal Vertex(ulong id, Vector3 point, string name = null) : base(id, point, name) { }

        /// <summary>
        /// Get associated Edges.
        /// </summary>
        /// <returns></returns>
        public List<Edge> GetEdges()
        {
            return this.Edges.ToList();
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
        /// Get associated Cells.
        /// </summary>
        /// <returns></returns>
        public List<Cell> GetCells()
        {
            return this.GetFaces().Select(face => face.GetCells()).SelectMany(x => x).Distinct().ToList();
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
        /// Get the associated Cell that is closest to a point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Cell GetClosestCell(Vector3 point)
        {
            return Cell.GetClosest<Cell>(this.GetCells(), point);
        }

        /// <summary>
        /// Get associated DirectedEdges.
        /// </summary>
        /// <returns></returns>
        private List<DirectedEdge> GetDirectedEdges()
        {
            return this.GetEdges().Select(edge => edge.GetDirectedEdges()).SelectMany(x => x).Distinct().ToList();
        }
    }
}