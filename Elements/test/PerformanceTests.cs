using System;
using System.Diagnostics;
using Elements.Geometry;
using Xunit;

namespace Elements.Tests
{
    public class PerformanceTests : ModelTest
    {
        [Fact(Skip = "Benchmark")]
        public void GlTFWriteTest()
        {
            this.Name = "Performance_Edges";
            this.GenerateIfc = false;

            var sw = new Stopwatch();
            sw.Start();

            var w = 1.0;
            var l = 1.0;
            var dim = 100;

            var profile = new Profile(Polygon.Rectangle(w, l));

            var mass = new Mass(profile, 1, BuiltInMaterials.Mass, isElementDefinition: true);
            this.Model.AddElement(mass);

            for (var i = 0; i < dim; i++)
            {
                for (var j = 0; j < dim; j++)
                {
                    var instance = mass.CreateInstance(new Transform(new Vector3(w * i, l * j)), $"i_j");
                    this.Model.AddElement(instance);
                }
            }

            sw.Stop();
            Console.WriteLine($"{sw.Elapsed} for creating {dim * dim} masses.");
        }
    }
}