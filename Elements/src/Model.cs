#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Elements.Serialization.JSON;
using Elements.Geometry;
using Elements.Validators;
using Newtonsoft.Json.Linq;

namespace Elements
{
    /// <summary>
    /// A container for elements.
    /// </summary>
    public partial class Model
    {
        /// <summary>
        /// The version of the Elements library used to create this model.
        /// </summary>
        public string ElementsVersion => this.GetType().Assembly.GetName().Version.ToString();

        /// <summary>
        /// Construct an empty model.
        /// </summary>
        public Model()
        {
            this.Transform = new Transform();
        }

        /// <summary>
        /// Construct a model.
        /// </summary>
        /// <param name="transform">The model's transform.</param>
        /// <param name="elements">The model's elements.</param>
        public Model(Transform @transform, System.Collections.Generic.IDictionary<Guid, Element> @elements)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Model>();
            if (validator != null)
            {
                validator.PreConstruct(new object[] { @transform, @elements });
            }

            this.Transform = @transform;
            this.Elements = @elements;
        }

        /// <summary>
        /// Add an element to the model.
        /// This operation recursively searches the element's properties
        /// for element sub-properties and adds those elements to the elements
        /// dictionary before adding the element itself.
        /// </summary>
        /// <param name="element">The element to add to the model.
        /// added to the model's elements collection?</param>
        /// <param name="updateElementChildren">A flag indicating whether the element's children should be parsed during addition.</param>
        public void AddElement(Element element, bool updateElementChildren = true)
        {
            if (element == null)
            {
                return;
            }

            element.UpdateChildren();
            var subElements = element.AllChildren();
            foreach (var subElement in subElements)
            {
                if (!this.Elements.ContainsKey(subElement.Key))
                {
                    this.Elements.Add(subElement.Key, subElement.Value);
                }
            }

            if (!this.Elements.ContainsKey(element.Id))
            {
                this.Elements.Add(element.Id, element);
            }
        }

        /// <summary>
        /// Add a collection of elements to the model.
        /// </summary>
        /// <param name="elements">The elements to add to the model.</param>
        public void AddElements(IEnumerable<Element> elements)
        {
            foreach (var e in elements)
            {
                AddElement(e);
            }
        }

        /// <summary>
        /// Add elements to the model.
        /// </summary>
        /// <param name="elements">The elements to add to the model.</param>
        public void AddElements(params Element[] elements)
        {
            foreach (var e in elements)
            {
                AddElement(e);
            }
        }

        /// <summary>
        /// Get an entity by id from the Model.
        /// </summary>
        /// <param name="id">The identifier of the element.</param>
        /// <returns>An entity or null if no entity can be found
        /// with the provided id.</returns>
        public T GetElementOfType<T>(Guid id) where T : Element
        {
            if (this.Elements.ContainsKey(id))
            {
                return (T)this.Elements[id];
            }
            return null;
        }

        /// <summary>
        /// Get the first entity with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>An entity or null if no entity can be found
        /// with the provided name.</returns>
        public T GetElementByName<T>(string name) where T : Element
        {
            var found = this.Elements.FirstOrDefault(e => e.Value.Name == name);
            if (found.Equals(new KeyValuePair<long, Element>()))
            {
                return null;
            }
            return (T)found.Value;
        }

        /// <summary>
        /// Get all entities of the specified Type.
        /// </summary>
        /// <typeparam name="T">The Type of element to return.</typeparam>
        /// <returns>A collection of elements of the specified type.</returns>
        public IEnumerable<T> AllElementsOfType<T>()
        {
            return this.Elements.Values.OfType<T>();
        }

        /// <summary>
        /// Serialize the model to JSON.
        /// </summary>
        public string ToJson(bool indent = false)
        {
            var exportModel = new Model();
            foreach (var kvp in this.Elements)
            {
                // TODO: Remove this when classes are updated to use
                // INotifyPropertyChanged

                // Some elements compute profiles and transforms
                // during UpdateRepresentation. Call UpdateRepresentation
                // here to ensure these values are correct in the JSON.
                if (kvp.Value is GeometricElement)
                {
                    ((GeometricElement)kvp.Value).UpdateRepresentations();
                }
                // Don't parse the children during this addition because
                // the previous call to UpdateRepresentation will already
                // have forced this.
                exportModel.AddElement(kvp.Value, false);
            }

            // Sort the elements in priority order.
            // So that they are deserialized with dependent
            // elements first.
            var elements = exportModel.Elements.Values.ToList();
            elements.Sort((a, b) =>
            {
                if (a.SortPriority < b.SortPriority) return 1;
                if (a.SortPriority > b.SortPriority) return -1;
                return 0;
            });
            exportModel.Transform = this.Transform;
            exportModel.Elements = elements.ToDictionary(e => e.Id, e => e);
            return Newtonsoft.Json.JsonConvert.SerializeObject(exportModel,
                                                               indent ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Deserialize a model from JSON.
        /// </summary>
        /// <param name="json">The JSON representing the model.</param>
        /// <param name="serializationErrors">A collection of deserialization errors.</param>
        /// <param name="forceTypeReload">Option to force reloading the inernal type cache. Use if you add types dynamically in your code.</param>
        public static Model FromJson(string json,
                                     out List<string> serializationErrors,
                                     bool forceTypeReload = false)
        {
            serializationErrors = new List<string>();

            var obj = JObject.Parse(json);
            Migrator.Instance.Migrate(obj, out var migrationErrors);

            var model = DeserializeModel(obj, out var deserializationErrors, forceTypeReload);

            serializationErrors.AddRange(migrationErrors);
            serializationErrors.AddRange(deserializationErrors);

            return model;
        }

        /// <summary>
        /// Deserialize a model from JSON using custom migrations.
        /// </summary>
        /// <param name="json">The JSON representing the model.</param>
        /// <param name="migrator">A migrator instance. Use this parameter to supply a custom migrator.</param>
        /// <param name="serializationErrors">A collection of deserialization and migration errors.</param>
        /// <param name="forceTypeReload">Option to force reloading the inernal type cache. Use if you add types dynamically in your code.</param>
        public static Model FromJson(string json,
                                     Migrator migrator,
                                     out List<string> serializationErrors,
                                     bool forceTypeReload = false)
        {
            serializationErrors = new List<string>();

            var obj = JObject.Parse(json);
            migrator.Migrate(obj, out var migrationErrors);

            var model = DeserializeModel(obj, out var deserialiationErrors, forceTypeReload);

            serializationErrors.AddRange(migrationErrors);
            serializationErrors.AddRange(deserialiationErrors);

            return model;
        }

        private static Model DeserializeModel(JObject obj, out List<string> serializationErrors, bool forceTypeReload)
        {
            serializationErrors = new List<string>();

            if (forceTypeReload)
            {
                JsonInheritanceConverter.RefreshAppDomainTypeCache(out var typeLoadErrors);
                serializationErrors.AddRange(typeLoadErrors);
            }

            var errors = new List<string>();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings()
            {
                Error = (sender, args) =>
                {
                    errors.Add(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            });
            var model = obj.ToObject<Model>(serializer);
            serializationErrors.AddRange(errors);

            return model;
        }

        public static Model FromJson(string json, bool forceTypeReload = false)
        {
            return FromJson(json, out _, forceTypeReload);
        }
    }
}