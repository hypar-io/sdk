using Elements.Geometry;
using Elements.Serialization.IFC;
using Elements.Serialization.glTF;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using Elements.Geometry.Profiles;

namespace Elements.IFC.Tests
{
    public class IfcTests
    {
        private const string basePath = "models";

        private readonly ITestOutputHelper output;

        public IfcTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        // [InlineData("rac_sample", "../../../models/IFC4/rac_advanced_sample_project.ifc")]
        // [InlineData("rme_sample", "../../../models/IFC4/rme_advanced_sample_project.ifc")]
        // [InlineData("rst_sample", "../../../models/IFC4/rst_advanced_sample_project.ifc")]
        [InlineData("AC-20-Smiley-West-10-Bldg", "../../../models/IFC4/AC-20-Smiley-West-10-Bldg.ifc")]
        [InlineData("AC20-Institute-Var-2", "../../../models/IFC4/AC20-Institute-Var-2.ifc")]
        // [InlineData("20160125WestRiverSide Hospital - IFC4-Autodesk_Hospital_Sprinkle", "../../../models/IFC4/20160125WestRiverSide Hospital - IFC4-Autodesk_Hospital_Sprinkle.ifc")]
        public void IFC4(string name, string ifcPath)
        {
            var ctorErrors = new List<string>();
            var model = IFCModelExtensions.FromIFC(Path.Combine(Environment.CurrentDirectory, ifcPath), out ctorErrors);
            foreach (var e in ctorErrors)
            {
                this.output.WriteLine(e);
            }
            model.ToGlTF(ConstructGlbPath(name));
        }

        [Theory(Skip = "IFC2X3")]
        [InlineData("example_1", "../../../models/IFC2X3/example_1.ifc")]
        // TODO: Reenable when IfcCompositeCurve is supported.
        // [InlineData("example_2", "../../../models/IFC2X3/example_2.ifc")]
        [InlineData("example_3", "../../../models/IFC2X3/example_3.ifc")]// new []{"0bKcgqsaHFN9FTVipKV_Ue","3Lkqsa9JzD0BBXIMnx2zgD"})]
        [InlineData("wall_with_window_vectorworks", "../../../models/IFC2X3/wall_with_window_vectorworks.ifc")]
        public void IFC2X3(string name, string ifcPath, string[] idsToConvert = null)
        {
            var ctorErrors = new List<string>();
            var model = IFCModelExtensions.FromIFC(Path.Combine(Environment.CurrentDirectory, ifcPath), out ctorErrors, idsToConvert);
            foreach (var e in ctorErrors)
            {
                this.output.WriteLine(e);
            }
            model.ToGlTF(ConstructGlbPath(name));
        }

        [Fact]
        public void Wall()
        {
            var line = new Line(Vector3.Origin, new Vector3(10, 10, 0));
            var line1 = new Line(new Vector3(10, 10, 0), new Vector3(10, 15, 0));
            var wall = new StandardWall(line, 0.2, 3);
            var wall1 = new StandardWall(line1, 0.2, 2);
            var model = new Model();
            model.AddElement(wall);
            model.AddElement(wall1);
            model.ToIFC(ConstructIfcPath("IfcWall"));
        }

        [Fact]
        public void PlanWall()
        {
            var planShape = Polygon.L(2, 2, 0.15);
            var wall1 = new Wall(planShape, 3.0);
            var wall2 = new Wall(planShape, 3.0, BuiltInMaterials.Concrete, new Transform(0, 0, 3));
            var model = new Model();
            model.AddElement(wall1);
            model.AddElement(wall2);
            model.ToIFC(ConstructIfcPath("IfcWallPlan"));
        }

        [Fact]
        public void Floor()
        {
            var planShape = Polygon.L(2, 4, 1.5);
            var floor = new Floor(planShape, 0.1);
            var floor1 = new Floor(planShape, 0.1, new Transform(0, 0, 2));
            var o = new Opening(Polygon.Rectangle(0.5, 0.5), transform: new Transform(0.5, 0.5, 0));
            floor.Openings.Add(o);

            var model = new Model();
            model.AddElement(floor);
            model.AddElement(floor1);

            var ifcPath = ConstructIfcPath("IfcFloor");
            model.ToIFC(ifcPath);
            model.ToGlTF(ConstructGlbPath("IfcFloor"));

            var ctorErrors = new List<string>();
            var newModel = IFCModelExtensions.FromIFC(ifcPath, out ctorErrors);
            foreach (var e in ctorErrors)
            {
                this.output.WriteLine(e);
            }

            Assert.Equal(10, newModel.Elements.Values.Count);
            newModel.ToGlTF(ConstructGlbPath("IfcFloor2"));
        }

        [Fact]
        public void Beams()
        {
            var model = new Model();

            var pts = Hypar(5.0, 5.0);
            var m1 = new Material("red", Colors.Red, 0f, 0f);
            var m2 = new Material("green", Colors.Green, 0f, 0f);

            var prof = WideFlangeProfileServer.Instance.GetProfileByType(WideFlangeProfileType.W10x100);
            for (var j = 0; j < pts.Count; j++)
            {
                var colA = pts[j];
                List<Vector3> colB = null;
                if (j + 1 < pts.Count)
                {
                    colB = pts[j + 1];
                }

                for (var i = 0; i < colA.Count; i++)
                {
                    var a = colA[i];
                    Vector3 b = default(Vector3);
                    if (i + 1 < colA.Count)
                    {
                        b = colA[i + 1];
                        var line1 = new Line(a, b);
                        var beam1 = new Beam(line1, prof, m1);
                        model.AddElement(beam1);
                    }

                    if (colB != null)
                    {
                        var c = colB[i];
                        var line2 = new Line(a, c);
                        var beam2 = new Beam(line2, prof, m2);
                        model.AddElement(beam2);
                    }
                }
            }
            model.ToIFC(ConstructIfcPath("IfcBeams"));
        }

        private List<List<Vector3>> Hypar(double a, double b)
        {
            var result = new List<List<Vector3>>();
            for (var x = -5; x <= 5; x++)
            {
                var column = new List<Vector3>();
                for (var y = -5; y <= 5; y++)
                {
                    var z = Math.Pow(y, 2) / Math.Pow(b, 2) - Math.Pow(x, 2) / Math.Pow(a, 2);
                    column.Add(new Vector3(x, y, z));
                }
                result.Add(column);
            }

            return result;
        }

        private string ConstructIfcPath(string modelName)
        {
            var modelsDirectory = Path.Combine(Environment.CurrentDirectory, basePath);
            if (!Directory.Exists(modelsDirectory))
            {
                Directory.CreateDirectory(modelsDirectory);
            }
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, basePath, $"{modelName}.ifc"));
        }

        private string ConstructGlbPath(string modelName)
        {
            var modelsDirectory = Path.Combine(Environment.CurrentDirectory, basePath);
            if (!Directory.Exists(modelsDirectory))
            {
                Directory.CreateDirectory(modelsDirectory);
            }
            return Path.GetFullPath(Path.Combine(modelsDirectory, $"{modelName}.glb"));
        }
    }
}