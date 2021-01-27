using System;
using System.Collections;
using Elements.Geometry;

namespace Elements
{
    /// <summary>
    /// A material with red, green, blue, alpha, and metallic factor components.
    /// </summary>
    public partial class Material : Element
    {
        internal override int SortPriority => 3;

        /// <summary>
        /// Construct a material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <param name="id">The id of the material.</param>
        public Material(string name, Guid id = default(Guid)) : base(id, name)
        {
            this.Color = Colors.Gray;
        }

        /// <summary>
        /// Construct a material.
        /// </summary>
        /// <param name="name">The identifier of the material. Identifiers should be unique within a model.</param>
        /// <param name="color">The RGBA color of the material.</param>
        /// <param name="specularFactor">The specular component of the color. Between 0.0 and 1.0.</param>
        /// <param name="glossinessFactor">The glossiness component of the color. Between 0.0 and 1.0.</param>
        /// <param name="texture">A relative path to a jpg or png image file to be used as a texture.</param>
        /// <param name="unlit">Is this material affected by lights?</param>
        /// <param name="doubleSided">Is this material to be rendered from both sides?</param>
        /// <param name="id">The id of the material.</param>
        /// <exception>Thrown when the specular or glossiness value is less than 0.0.</exception>
        /// <exception>Thrown when the specular or glossiness value is greater than 1.0.</exception>
        public Material(string name,
                        Color color,
                        double specularFactor = 0.1,
                        double glossinessFactor = 0.1,
                        string texture = null,
                        bool unlit = false,
                        bool doubleSided = false,
                        Guid id = default(Guid)) :
            this(color,
                 specularFactor,
                 glossinessFactor,
                 unlit,
                 texture,
                 doubleSided,
                 id != default(Guid) ? id : Guid.NewGuid(),
                 name)
        { }

        /// <summary>
        /// Is this material equal to the provided material?
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            var m = obj as Material;
            if (m == null)
            {
                return false;
            }
            return this.Color.Equals(m.Color) && this.SpecularFactor == m.SpecularFactor && this.GlossinessFactor == m.GlossinessFactor && this.Name == m.Name;
        }

        /// <summary>
        /// Get the hash code for the material.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return new ArrayList() { this.Name, this.Color, this.SpecularFactor, this.GlossinessFactor }.GetHashCode();
        }
    }
}