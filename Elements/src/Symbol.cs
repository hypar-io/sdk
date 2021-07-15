
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elements.Serialization.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elements
{
    public partial class Symbol
    {
        /// <summary>
        /// Get the geometry from this symbol.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<object>> GetGeometryAsync()
        {
            if (this.Geometry.InternalGeometry != null)
            {
                return this.Geometry.InternalGeometry;
            }
            else if (this.Geometry.GeometryUrl != null)
            {
                try
                {
                    var request = HttpWebRequest.Create(this.Geometry.GeometryUrl);
                    var response = await request.GetResponseAsync();
                    var stream = response.GetResponseStream();
                    var streamReader = new StreamReader(stream);
                    var json = streamReader.ReadToEnd();
                    var objects = JsonConvert.DeserializeObject<List<JObject>>(json);
                    var _loadedTypes = typeof(Element).Assembly.GetTypes().ToList();
                    return objects.Select(obj =>
                    {
                        var discriminator = obj.Value<string>("discriminator");
                        var matchingType = _loadedTypes.FirstOrDefault(t => t.FullName.Equals(discriminator));
                        return obj.ToObject(matchingType);
                    });
                }
                catch
                {
                    return new List<object>();
                }
            }
            return new List<object>();
        }
    }
}