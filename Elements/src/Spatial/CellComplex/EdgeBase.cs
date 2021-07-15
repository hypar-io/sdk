using Elements.Geometry;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A unique edge or directed edge in a face complex.
    /// </summary>
    public abstract class EdgeBase<ChildClass> : ChildBase<ChildClass, Line> where ChildClass : ChildBase<ChildClass, Line>
    {
        /// <summary>
        /// ID of start Vertex.
        /// </summary>
        public ulong StartVertexId;

        /// <summary>
        /// ID of end Vertex.
        /// </summary>
        public ulong EndVertexId;

        /// <summary>
        /// Create an EdgeBase (just calls CellChild constructor).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="faceComplex"></param>
        /// <returns></returns>
        protected EdgeBase(ulong id, FaceComplex faceComplex) : base(id, faceComplex) { }

        /// <summary>
        /// Get the geometry that represents this Edge or DirectedEdge.
        /// </summary>
        /// <returns></returns>
        public override Line GetGeometry()
        {
            return new Line(
                this.FaceComplex.GetVertex(this.StartVertexId).Value,
                this.FaceComplex.GetVertex(this.EndVertexId).Value
            );
        }

        /// <summary>
        /// Get the shortest distance from a point to the geometry representing this edge.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override double DistanceTo(Vector3 point)
        {
            return point.DistanceTo(this.GetGeometry());
        }
    }
}