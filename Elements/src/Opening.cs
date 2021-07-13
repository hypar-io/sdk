using System;
using System.Collections.Generic;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Newtonsoft.Json;

namespace Elements
{
    /// <summary>
    /// A polygonal opening.
    /// An opening's placement is defined by the x and y coordinates.
    /// The direction of the opening corresponds to the +Z axis of the transform.
    /// </summary>
    public class Opening : GeometricElement
    {
        /// <summary>
        /// The profile of the opening.
        /// </summary>
        [Obsolete("Use perimeter instead.")]
        public Profile Profile { get; set; }

        /// <summary>
        /// The perimeter of the opening.
        /// </summary>
        public Polygon Perimeter { get; set; }

        /// <summary>
        /// The depth of the opening along the opening's +Z axis.
        /// </summary>
        public double DepthFront { get; set; }

        /// <summary>
        /// The depth of the opening along the opening's -Z axis.
        /// </summary>
        public double DepthBack { get; set; }

        /// <summary>
        /// The direction of the opening's extrusion.
        /// </summary>
        public Vector3 Direction { get; set; }

        /// <summary>
        /// Create an opening.
        /// </summary>
        [JsonConstructor]
        public Opening(Polygon perimeter,
                       Vector3 direction,
                       double depthFront = 1.0,
                       double depthBack = 1.0,
                       Transform transform = null,
                       Representation representation = null,
                       bool isElementDefinition = false,
                       Guid id = default(Guid),
                       string name = null) : base(transform != null ? transform : new Transform(),
                                                  BuiltInMaterials.Void,
                                                  representation != null ? representation : new Representation(new List<SolidOperation>()),
                                                  isElementDefinition,
                                                  id != default(Guid) ? id : Guid.NewGuid(),
                                                  name)
        {
            this.Perimeter = perimeter;
            this.DepthBack = depthBack;
            this.DepthFront = depthFront;
            this.Direction = direction;
        }

        /// <summary>
        /// Update representations
        /// </summary>
        public override void UpdateRepresentations()
        {
            this.Representation.SolidOperations.Clear();
            if (this.DepthFront > 0)
            {
                this.Representation.SolidOperations.Add(new Extrude(this.Perimeter, this.DepthFront, this.Direction, true));
            }
            if (this.DepthBack > 0)
            {
                this.Representation.SolidOperations.Add(new Extrude(this.Perimeter, this.DepthBack, this.Direction.Negate(), true));
            }
        }
    }
}