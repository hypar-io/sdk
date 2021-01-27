using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Abstractions;

namespace Elements.Tests
{
    public class TopographyTests : ModelTest
    {
        private ITestOutputHelper _output;

        public TopographyTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact, Trait("Category", "Examples")]
        public void Example()
        {
            this.Name = "Elements_Topography";

            // <example>
            // Read topo elevations from a file.
            var data = JsonConvert.DeserializeObject<Dictionary<string, double[]>>(File.ReadAllText("./elevations.json"));
            var elevations = data["points"];
            var tileSize = WebMercatorProjection.GetTileSizeMeters(15);

            // Create a topography.
            var topo = new Topography(Vector3.Origin, tileSize, elevations);
            // </example>

            this.Model.AddElement(topo);
        }

        [Fact]
        public void Simple()
        {
            this.Name = "TopographySimple";
            var elevations = new double[] { 0.2, 1.0, 0.5, 0.25, 0.1, 0.2, 2.0, 0.05, 0.05 };
            var colorizer = new Func<Triangle, Elements.Geometry.Color>((t) =>
            {
                return Colors.Green;
            });
            var topo = new Topography(Vector3.Origin, 3, elevations);
            this.Model.AddElement(topo);
        }

        [Fact]
        public void ConstructTopography()
        {
            this.Name = "Topography";
            var topo = CreateTopoFromMapboxElevations();
            this.Model.AddElement(topo);
        }

        [Fact]
        public void TopographyHasTextureApplied()
        {
            this.Name = "TexturedTopography";
            var m = new Material("texture", Colors.Gray, 0.0f, 0.0f, "./Textures/UV.jpg");
            var topo = CreateTopoFromMapboxElevations(material: m);
            this.Model.AddElement(topo);
        }

        [Fact]
        public void TopographySerializesQuickly()
        {
            this.Name = "TopographySerializationPerfomance";
            var sw = new Stopwatch();
            var topo = CreateTopoFromMapboxElevations();
            sw.Start();
            this.Model.AddElement(topo);
            sw.Stop();
            _output.WriteLine($"Serialization of topography: {sw.ElapsedMilliseconds.ToString()}ms");
            this.Model.Elements.Clear();
            sw.Reset();
            sw.Start();
            this.Model.AddElement(BuiltInMaterials.Topography);
            topo.Material = BuiltInMaterials.Topography;
            this.Model.AddElement(topo);
            sw.Stop();
            _output.WriteLine($"Serialization of topography w/out recursive gather: {sw.ElapsedMilliseconds.ToString()}ms");
        }

        [Fact]
        public void TopographyReserializationGetsTopography()
        {
            var topo = CreateTopoFromMapboxElevations();
            this.Model.AddElement(topo);
            var json = this.Model.ToJson();
            var newModel = Model.FromJson(json);
            var newTopo = newModel.AllElementsOfType<Topography>().First();
            Assert.Equal(topo.Mesh.Triangles.Count, newTopo.Mesh.Triangles.Count);
            Assert.Equal(topo.Mesh.Vertices.Count, newTopo.Mesh.Vertices.Count);
            Assert.Equal(topo.CellWidth, newTopo.CellWidth);
            Assert.Equal(topo.CellWidth, newTopo.CellWidth);
            Assert.Equal(topo.RowWidth, newTopo.RowWidth);
        }

        [Fact]
        public void RaysIntersectTopography()
        {
            this.Name = "RayTopographyIntersection";
            var topo = CreateTopoFromMapboxElevations(new Vector3(10000000, 10000000));
            var mp = new ModelPoints(new List<Vector3>(), new Material("xsect", Colors.Black));
            foreach (var t in topo.Mesh.Triangles)
            {
                var o = t.Vertices[0].Position;
                var c = new[] { t.Vertices[0].Position, t.Vertices[1].Position, t.Vertices[2].Position }.Average();
                var r = new Ray(new Vector3(c.X, c.Y), Vector3.ZAxis);
                if (r.Intersects(t, out Vector3 result))
                {
                    mp.Locations.Add(result);
                }
            }
            this.Model.AddElement(topo);
            if (mp.Locations.Count > 0)
            {
                this.Model.AddElement(mp);
            }
        }

        [Fact]
        public void MapboxTopography()
        {
            this.Name = "MapboxTopography";

            // 0 1
            // 2 3

            var maps = new[]{
                "./Topography/Texture_f7c3dc2f-c47c-4638-a962-53ae31719cf5_0.jpg",
                "./Topography/Texture_df332cc3-62b0-42ac-9041-942e1fd985aa_1.jpg",
                "./Topography/Texture_12454f24-690a-43e2-826d-e4deae5eb82e_2.jpg",
                "./Topography/Texture_aa1b1148-0563-4b9d-b1c0-7138024dc974_3.jpg"};

            var topos = new[]{
                "./Topography/Topo_52f0cdf5-2d34-4e1c-927d-79fb5f2caf43_0.png",
                "./Topography/Topo_5f21ae5e-eeef-4346-af16-860afdc0e829_1.png",
                "./Topography/Topo_e056d328-5b7d-47d3-b6bc-42d6d245d8ec_2.png",
                "./Topography/Topo_0a91fd8a-d887-40c2-a729-aac47441f9d8_3.png"};

            var tiles = new[]{
                new Tuple<int,int>(20,20),
                new Tuple<int,int>(21,20),
                new Tuple<int,int>(20,21),
                new Tuple<int,int>(21,21),
            };

            // Web Mercator Tiling (x,y)
            // 0,0   1,0
            // 0,1   1,1

            // Image Coordinates (x,y)
            // 0,0   1,0
            // 0,1   1,1

            // Image Texture coordinates (u,v)
            // 0,1   1,1
            // 0,0   1,0

            // Mesh Coordinates
            // 0,1   1,1
            // 0,0   1,0

            var zoom = 16;
            var selectedOrigin = WebMercatorProjection.TileIdToCenterWebMercator(tiles[0].Item1, tiles[0].Item2, zoom);
            var sampleSize = 4;

            var topographies = new Topography[maps.Length];

            for (var i = 0; i < maps.Length; i++)
            {
                using (var topo = Image.Load<Rgba32>(topos[i]))
                {
                    var size = topo.Width / sampleSize;
                    var elevationData = new double[(int)Math.Pow(size, 2)];

                    var idx = 0;
                    // Read the rows in reverse order as images
                    // start in the top left and meshes start
                    // in the lower left.
                    for (var y = topo.Width - 1; y >= 0; y -= sampleSize)
                    {
                        for (var x = 0; x < topo.Width; x += sampleSize)
                        {
                            var c = topo[x, y];
                            var height = -10000 + ((c.R * 256 * 256 + c.G * 256 + c.B) * 0.1);
                            elevationData[idx] = height;
                            idx++;
                        }
                    }

                    var tileSize = WebMercatorProjection.GetTileSizeMeters(zoom);
                    var origin = WebMercatorProjection.TileIdToCenterWebMercator(tiles[i].Item1, tiles[i].Item2, zoom) - new Vector3(tileSize / 2.0, tileSize / 2.0) - selectedOrigin;
                    var material = new Material($"Topo_{i}", Colors.White, 0.0f, 0.0f, maps[i]);
                    var topography = new Topography(origin, tileSize, elevationData, material);
                    this.Model.AddElement(topography);
                    topographies[i] = topography;
                }
            }

            // 1 2
            // 2 3

            topographies[0].AverageEdges(topographies[1], Units.CardinalDirection.East);
            topographies[0].AverageEdges(topographies[2], Units.CardinalDirection.South);
            topographies[1].AverageEdges(topographies[3], Units.CardinalDirection.South);
            topographies[2].AverageEdges(topographies[3], Units.CardinalDirection.East);
        }

        [Fact]
        public void Csg()
        {
            this.Name = "Topography_CSG";
            var topo = CreateTopoFromMapboxElevations();
            var csg = topo.Mesh.ToCsg();

            var box = new Extrude(Polygon.Star(200, 100, 5), 100, Vector3.ZAxis, false);
            csg = csg.Substract(box._csg.Transform(new Transform(topo.Mesh.Vertices[topo.RowWidth * topo.RowWidth / 2 + topo.RowWidth / 2].Position + new Vector3(0, 0, -50)).ToMatrix4x4()));

            var result = new Mesh();
            csg.Tessellate(ref result);
            result.ComputeNormals();
            var material = new Material($"Topo", Colors.White, 0.0f, 0.0f, "./Topography/Texture_12454f24-690a-43e2-826d-e4deae5eb82e_2.jpg");
            this.Model.AddElement(new MeshElement(result, material));
        }

        private static Topography CreateTopoFromMapboxElevations(Vector3 origin = default(Vector3), Material material = null)
        {
            // Read topo elevations
            var data = JsonConvert.DeserializeObject<Dictionary<string, double[]>>(File.ReadAllText("./elevations.json"));
            var elevations = data["points"];

            // Compute the mapbox tile side lenth.
            var tileSize = WebMercatorProjection.GetTileSizeMeters(15);

            return new Topography(origin.Equals(default(Vector3)) ? origin : Vector3.Origin, tileSize, elevations, material);
        }
    }
}