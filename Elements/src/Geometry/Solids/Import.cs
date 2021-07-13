namespace Elements.Geometry.Solids
{
    /// <summary>
    /// A solid operation defined by an existing solid.
    /// </summary>
    public class Import : SolidOperation
    {
        /// <summary>
        /// Create an import solid.
        /// </summary>
        /// <param name="solid">The solid which was imported.</param>
        /// <param name="isVoid">Is the operation a void?</param>
        public Import(Solid solid, bool isVoid = false) : base(isVoid)
        {
            this._solid = solid;
            this._csg = solid.ToCsg();
        }
    }
}