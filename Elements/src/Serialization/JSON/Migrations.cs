using System;
using Newtonsoft.Json.Linq;
#pragma warning disable 1591

namespace Elements.Serialization.JSON
{
    /// <summary>
    /// Migrate models serialized to JSON.
    /// </summary>
    public class Migrate_Representation_082_083 : IMigration
    {
        public Version From => Version.Parse("0.8.2");

        public Version To => Version.Parse("0.8.3");

        /// <summary>
        /// A JSON path expression for all elements which have a representation.
        /// </summary>
        public string Path => "$.Elements.*.Representation";

        public void Migrate(JToken token)
        {
            var repObj = (JObject)token;
            var geomObj = (JObject)((JProperty)repObj.Parent).Parent;

            // Create a new SolidRepresentation based
            // on the old representation.
            var solidRepObject = new JObject(repObj);
            var id = Guid.NewGuid().ToString();
            solidRepObject["Id"] = id;
            solidRepObject["Name"] = string.Empty;
            solidRepObject["discriminator"] = "Elements.Geometry.Representation";

            // Replace the representation with an id.
            geomObj.Remove("Representation");
            geomObj["Representation"] = id;

            // Add the solid representation to the model before 
            // the geometric element. Insert a property with the value
            // of the solid representation before the property for 
            // the geometric element.
            geomObj.Parent.AddBeforeSelf(new JProperty(id, solidRepObject));

            return;
        }
    }
}