using System.IO;
using System.Net;
using Elements;
using DotLiquid;
using System;
using System.Linq;
using System.Collections.Generic;
using Elements.Geometry;
using System.Reflection;
using Elements.Generate.StringUtils;

namespace Elements.Generate
{
    /// <summary>
    /// Generate code for a ContentCatalog.  The basic format will be a static class with properties
    /// for each of the types in the catalog.
    /// </summary>
    public static class CatalogGenerator
    {
        private static string _catalogTemplatePath;
        /// <summary>
        /// The directory in which to find code templates. Some execution contexts may require this to be overridden as the
        /// Executing Assembly is not necessarily in the same place as the templates (e.g. Headless Grasshopper Execution)
        /// </summary>
        public static string CatalogTemplatePath
        {
            get
            {
                if (_catalogTemplatePath == null)
                {
                    _catalogTemplatePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "./Templates/Catalog.liquid"));
                }
                if (!File.Exists(_catalogTemplatePath))
                {
                    Console.WriteLine("Templates path attempted: " + _catalogTemplatePath);
                    throw new InvalidDataException("The templates folder cannot be found and is necessary for successful code generation. Be sure that the Hypar.Elements.CodeGeneration nuget package is referenced in all of your projects.");
                }
                return _catalogTemplatePath;
            }
            set => _catalogTemplatePath = value;
        }

        /// <summary>
        /// Generate a catalog from either file path or public URL.
        /// </summary>
        /// <param name="uri">The location of the catalog json file.</param>
        /// <param name="saveDirectory">The folder of where to save the resulting generated code.</param>
        /// <param name="useReferenceOrientation">Should ContentElements be reoriented to match the rotation they had when they were exported?</param>
        public static void FromUri(string uri, string saveDirectory, bool useReferenceOrientation = true)
        {
            Template.RegisterSafeType(typeof(ContentCatalog), new[] { "Name", "Content", "ReferenceConfiguration" });
            Template.RegisterSafeType(typeof(ContentElement), GetContentElementToRender);
            Template.RegisterSafeType(typeof(ElementInstance), GetElementInstanceToRender);
            DotLiquid.Template.RegisterFilter(typeof(HyparFilters));

            var json = GetContentsOfUri(uri);
            ContentCatalog catalog = ContentCatalog.FromJson(json);
            if (useReferenceOrientation)
            {
                catalog.UseReferenceOrientation();
            }
            var templateText = File.ReadAllText(CatalogTemplatePath);
            var template = DotLiquid.Template.Parse(templateText);
            var result = template.Render(Hash.FromAnonymousObject(new
            {
                catalog = catalog
            }));

            var path = Path.Combine(saveDirectory, catalog.Name.ToSafeIdentifier() + ".g.cs");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllText(path, result);
        }

        private static Func<object, object> GetElementInstanceToRender = (element) =>
        {
            var inst = element as ElementInstance;
            return new
            {
                name = inst.Name,
                baseName = inst.BaseDefinition.Name,
                transform = TransformToCode(inst.Transform).Replace("\n", "\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t")
            };
        };

        private static Func<object, object> GetContentElementToRender = (element) =>
        {
            var constructor = element.GetType().GetConstructors().OrderByDescending(c => c.GetParameters().Length);
            List<object> constructorParams = new List<object>();
            var parameters = constructor.FirstOrDefault().GetParameters();
            foreach (var param in parameters)
            {
                var property = element.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == param.Name.ToLower());
                if (property != null)
                {
                    var value = property.GetValue(element);
                    var codeToAdd = value;
                    switch (value)
                    {
                        case string str:
                            codeToAdd = $"@\"{str}\"";
                            break;
                        case Guid guid:
                            codeToAdd = $"new Guid(\"{guid}\")";
                            break;
                        case bool tf:
                            codeToAdd = tf ? "true" : "false";
                            break;
                        case double num:
                            codeToAdd = $"{num}";
                            break;
                        case int i:
                            codeToAdd = $"{i}";
                            break;
                        case BBox3 bBox:
                            codeToAdd = $"new BBox3(new Vector3({bBox.Min.X},{bBox.Min.Y},{bBox.Min.Z}), new Vector3({bBox.Max.X},{bBox.Max.Y},{bBox.Max.Z}))";
                            break;
                        case Transform tr:
                            codeToAdd = TransformToCode(tr);
                            break;
                        case Vector3 v:
                            codeToAdd = $"new Vector3({v.X},{v.Y},{v.Z})";
                            break;
                        case IList<Symbol> symbols:
                            codeToAdd = $"JsonConvert.DeserializeObject<List<Symbol>>(\"{System.Web.HttpUtility.JavaScriptStringEncode(Newtonsoft.Json.JsonConvert.SerializeObject(symbols))}\")";
                            break;
                        case Dictionary<string, object> dict:
                            codeToAdd = "@\"" + Newtonsoft.Json.JsonConvert.SerializeObject(dict).Replace("\"", "\"\"") + "\"";
                            break;
                        case Material material:
                            // new Material(material.Color,
                            //              material.SpecularFactor,
                            //              material.GlossinessFactor,
                            //              material.Unlit,
                            //              material.Texture,
                            //              material.DoubleSided,
                            //              material.Id,
                            //              material.Name);
                            codeToAdd = "BuiltInMaterials.Default";
                            break;
                        default:
                            codeToAdd = "null";
                            break;
                    }

                    constructorParams.Add(codeToAdd);
                }
                else
                {
                    constructorParams.Add(null);
                }
            }

            ContentElement content = element as ContentElement;
            return new
            {
                name = content.Name,
                constructorArgs = string.Join(",\n", constructorParams).Replace("\n", "\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t")
            };
        };

        private static string TransformToCode(Transform tr)
        {
            return $"new Transform(new Vector3({tr.Origin.X},{tr.Origin.Y},{tr.Origin.Z})," +
                        $"\n\t\tnew Vector3({tr.XAxis.X},{tr.XAxis.Y},{tr.XAxis.Z})," +
                        $"\n\t\tnew Vector3({tr.YAxis.X},{tr.YAxis.Y},{tr.YAxis.Z})," +
                        $"\n\t\tnew Vector3({tr.ZAxis.X},{tr.ZAxis.Y},{tr.ZAxis.Z}))";
        }

        private static string GetContentsOfUri(string uri)
        {
            if (uri.StartsWith("http://") || uri.StartsWith("https://"))
            {
                using (WebClient client = new WebClient())
                {
                    string s = client.DownloadString(uri);
                    return s;
                }
            }
            else if (File.Exists(uri))
            {
                return File.ReadAllText(uri);
            }
            else
            {
                throw new InvalidDataException("Could not read the contents of the file at that uri");
            }
        }
    }
}