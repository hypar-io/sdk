using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A unique Vertex or Orientation in a face complex.
    /// </summary>
    public abstract class VertexBase<ChildClass> : ChildBase<ChildClass, Vector3> where ChildClass : ChildBase<ChildClass, Vector3>
    {
        /// <summary>
        /// Location in space if this is a Vertex, or a vector direction if this is an Orientation.
        /// </summary>
        public Vector3 Value;

        /// <summary>
        /// An optional user-supplied name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Represents one Vertex or Orientation in a FaceComplex.
        /// Is not intended to be created or modified outside of the FaceComplex class code.
        /// </summary>
        /// <param name="faceComplex">FaceComplex that this belongs to</param>
        /// <param name="id"></param>
        /// <param name="point">Location of the vertex</param>
        /// <param name="name">Optional name</param>
        internal VertexBase(FaceComplex faceComplex, ulong id, Vector3 point, string name = null) : base(id, faceComplex)
        {
            this.Value = point;
            this.Name = name;
        }

        /// <summary>
        /// For deserialization only!
        /// </summary>
        /// <param name="id"></param>
        /// <param name="point"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [JsonConstructor]
        internal VertexBase(ulong id, Vector3 point, string name = null) : base(id, null)
        {
            this.Value = point;
            this.Name = name;
        }

        /// <summary>
        /// Get the Vector3 that represents this Vertex or Orientation.
        /// </summary>
        /// <returns></returns>
        public override Vector3 GetGeometry()
        {
            return this.Value;
        }

        /// <summary>
        ///  Get the shortest distance from a point to the geometry representing this vertex.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override double DistanceTo(Vector3 point)
        {
            return point.DistanceTo(this.GetGeometry());
        }
    }
}