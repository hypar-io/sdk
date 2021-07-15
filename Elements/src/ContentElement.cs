using System.Collections.Generic;
using System.IO;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace Elements
{
    public partial class ContentElement
    {
         /// <summary>
        /// This constructor adds the ability to include additionalProperties.  The additional properties should be 
        /// a dictionary that has been serialized to a string, they are deserialized during construction.
        /// This is used in Revit Content workflows to store instance parameter data.
        /// </summary>
        /// <param name="gltfLocation">The path to the .glb file.</param>
        /// <param name="boundingBox">The BBox3 of this Content Element.</param>
        /// <param name="gltfScaleToMeters">The number required to scale this contents dimensions to meters.  Used during gltf merging.</param>
        /// <param name="sourceDirection">The direction the element was facing when it was extracted from it's source.</param>
        /// <param name="symbols">Any additional symbol representations of this content element.</param>
        /// <param name="transform">The transform of this ContentElement.</param>
        /// <param name="material">The material, used for the BBox representation of this element.</param>
        /// <param name="representation">The representation which will be updated when needed.</param>
        /// <param name="isElementDefinition">Should the element be used to create instances, or should it be inserted into a 3D scene.</param>
        /// <param name="id">The guid of this element.</param>
        /// <param name="name">The name of this element.</param>
        /// <param name="additionalProperties">The string json serialization of a dictionary of additional parameters.</param>
        public ContentElement(string @gltfLocation, BBox3 @boundingBox, double @gltfScaleToMeters, Vector3 @sourceDirection, IList<Symbol> symbols, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name, string @additionalProperties)
        : this(@gltfLocation, @boundingBox, @gltfScaleToMeters, @sourceDirection, symbols, @transform, @material, @representation, @isElementDefinition, @id, @name)
        {
            this.AdditionalProperties = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(@additionalProperties);
        }

        /// <summary>
        /// This constructor adds the ability to include additionalProperties.  The additional properties should be 
        /// a dictionary that has been serialized to a string, they are deserialized during construction.
        /// This is used in Revit Content workflows to store instance parameter data.
        /// </summary>
        /// <param name="gltfLocation">The path to the .glb file.</param>
        /// <param name="boundingBox">The BBox3 of this Content Element.</param>
        /// <param name="gltfScaleToMeters">The number required to scale this contents dimensions to meters.  Used during gltf merging.</param>
        /// <param name="sourceDirection">The direction the element was facing when it was extracted from it's source.</param>
        /// <param name="transform">The transform of this ContentElement.</param>
        /// <param name="material">The material, used for the BBox representation of this element.</param>
        /// <param name="representation">The representation which will be updated when needed.</param>
        /// <param name="isElementDefinition">Should the element be used to create instances, or should it be inserted into a 3D scene.</param>
        /// <param name="id">The guid of this element.</param>
        /// <param name="name">The name of this element.</param>
        /// <param name="additionalProperties">The string json serialization of a dictionary of additional parameters.</param>
        public ContentElement(string @gltfLocation, BBox3 @boundingBox, double @gltfScaleToMeters, Vector3 @sourceDirection, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name, string @additionalProperties)
        : this(@gltfLocation, @boundingBox, @gltfScaleToMeters, @sourceDirection, null, @transform, @material, @representation, @isElementDefinition, @id, @name)
        {
            this.AdditionalProperties = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(@additionalProperties);
        }

        /// <summary>
        /// This constructor adds backwards compatibility with the old constructor, before Symbols were added.
        /// </summary>
        /// <param name="gltfLocation">The path to the .glb file.</param>
        /// <param name="boundingBox">The BBox3 of this Content Element.</param>
        /// <param name="gltfScaleToMeters">The number required to scale this contents dimensions to meters.  Used during gltf merging.</param>
        /// <param name="sourceDirection">The direction the element was facing when it was extracted from it's source.</param>
        /// <param name="transform">The transform of this ContentElement.</param>
        /// <param name="material">The material, used for the BBox representation of this element.</param>
        /// <param name="representation">The representation which will be updated when needed.</param>
        /// <param name="isElementDefinition">Should the element be used to create instances, or should it be inserted into a 3D scene.</param>
        /// <param name="id">The guid of this element.</param>
        /// <param name="name">The name of this element.</param>
        public ContentElement(string @gltfLocation, BBox3 @boundingBox, double @gltfScaleToMeters, Vector3 @sourceDirection, Transform @transform, Material @material, Representation @representation, bool @isElementDefinition, System.Guid @id, string @name)
        : this(@gltfLocation, @boundingBox, @gltfScaleToMeters, @sourceDirection, null, @transform, @material, @representation, @isElementDefinition, @id, @name)
        {

        }


        /// <summary>
        /// Update the ContentElement representation with a solid of the
        /// Bounding Box. This is used in the absence of finding a
        /// Gltf for import.
        /// </summary>
        public override void UpdateRepresentations()
        {
            if (!BoundingBox.IsValid() || BoundingBox.IsDegenerate())
            {
                return;
            }
            var vertices = new List<Vector3> { BoundingBox.Min, BoundingBox.Max };
            var bottomProfile = new Polygon(new List<Vector3>{
                            new Vector3(BoundingBox.Min.X, BoundingBox.Min.Y, BoundingBox.Min.Z),
                            new Vector3(BoundingBox.Min.X, BoundingBox.Max.Y, BoundingBox.Min.Z),
                            new Vector3(BoundingBox.Max.X, BoundingBox.Max.Y, BoundingBox.Min.Z),
                            new Vector3(BoundingBox.Max.X, BoundingBox.Min.Y, BoundingBox.Min.Z),
                        });

            var height = BoundingBox.Max.Z - BoundingBox.Min.Z;
            var boxSolid = new Extrude(bottomProfile, height, Vector3.ZAxis, false);
            this.Representation = new Representation(new List<SolidOperation> { boxSolid });
        }
    }
}