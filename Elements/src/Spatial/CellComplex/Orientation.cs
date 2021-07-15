using Elements.Geometry;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// A unique orientation direction in a FaceComplex.
    /// </summary>
    public class Orientation : VertexBase<Orientation>
    {
        /// <summary>
        /// Represents a unique orientation direction within a FaceComplex.
        /// Is not intended to be created or modified outside of the CellComplex class code.
        /// </summary>
        /// <param name="faceComplex">FaceComplex that this belongs to</param>
        /// <param name="id"></param>
        /// <param name="orientation">The orientation direction</param>
        /// <param name="name">Optional name</param>
        /// <returns></returns>
        internal Orientation(FaceComplex faceComplex, ulong id, Vector3 orientation, string name = null) : base(faceComplex, id, orientation, name) { }

        /// <summary>
        /// Do not use this method: it just throws an exception.
        /// Orientations are relative directions and do not exist at any point in absolute space. A distance cannot be calculated for orientations.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override double DistanceTo(Vector3 point)
        {
            throw new System.Exception("Orientations are relative directions and do not exist at any point in absolute space. A distance cannot be calculated for orientations.");
        }
    }
}