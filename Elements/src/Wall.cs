using Elements.Geometry;
using Elements.Geometry.Solids;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Elements
{
    /// <summary>
    /// A wall defined by a planar profile extruded to a height.
    /// </summary>
    public class Wall : GeometricElement
    {
        /// <summary>
        /// The height of the wall.
        /// </summary>
        public double Height { get; protected set; }

        /// <summary>
        /// The profile of the wall.
        /// </summary>
        public Profile Profile { get; protected set; }

        /// <summary>
        /// Construct a wall by extruding a profile.
        /// </summary>
        /// <param name="profile">The plan profile of the wall.</param>
        /// <param name="height">The height of the wall.</param>
        /// <param name="material">The material of the wall.</param>
        /// <param name="transform">An option transform for the wall.</param>
        /// <param name="representation">The wall's representation.</param>
        /// <param name="isElementDefinition">Is this an element definition?</param>
        /// <param name="id">The id of the wall.</param>
        /// <param name="name">The name of the wall.</param>
        [JsonConstructor]
        public Wall(Profile profile,
                      double height,
                      Material material = null,
                      Transform transform = null,
                      Representation representation = null,
                      bool isElementDefinition = false,
                      Guid id = default(Guid),
                      string name = null) : base(transform != null ? transform : new Transform(),
                                                 material != null ? material : BuiltInMaterials.Concrete,
                                                 representation != null ? representation : new Representation(new List<SolidOperation>()),
                                                 isElementDefinition,
                                                 id != default(Guid) ? id : Guid.NewGuid(),
                                                 name)
        {
            if (height <= 0.0)
            {
                throw new ArgumentOutOfRangeException("The wall could not be created. The height of the wall must be greater than 0.0.");
            }

            this.Profile = profile;
            this.Height = height;
        }

        /// <summary>
        /// Update the representations.
        /// </summary>
        public override void UpdateRepresentations()
        {
            this.Representation.SolidOperations.Clear();
            this.Representation.SolidOperations.Add(new Extrude(this.Profile, this.Height, Vector3.ZAxis, false));
        }

        /// <summary>
        /// A pass-through constructor to set the id, name, and transform.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="transform"></param>
        /// <param name="material"></param>
        /// <param name="representation"></param>
        /// <param name="isElementDefinition">Is this an element definition?</param>
        protected Wall(Transform transform,
                       Material material,
                       Representation representation,
                       bool isElementDefinition = false,
                       Guid id = default(Guid),
                       string name = null) : base(transform,
                                           material,
                                           representation,
                                           isElementDefinition,
                                           id == default(Guid) ? Guid.NewGuid() : id,
                                           name)
        { }

        /// <summary>
        /// Construct a wall from geometry.
        /// </summary>
        /// <param name="geometry">The geometry of the wall.</param>
        /// <param name="transform">The wall's Transform.</param>
        /// <param name="isElementDefinition">Is this an element definition?</param>
        internal Wall(Solid geometry,
                      Transform transform = null,
                      bool isElementDefinition = false) : base(transform != null ? transform : new Transform(),
                                                         BuiltInMaterials.Default,
                                                         new Representation(new List<SolidOperation>()),
                                                         isElementDefinition,
                                                         Guid.NewGuid(),
                                                         null)
        {
            if (geometry == null)
            {
                throw new ArgumentOutOfRangeException("You must supply one solid to construct a Wall.");
            }

            if (transform != null)
            {
                this.Transform = transform;
            }
            this.Representation.SolidOperations.Add(new ConstructedSolid(geometry));
        }
    }
}