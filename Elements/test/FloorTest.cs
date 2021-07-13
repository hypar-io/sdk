using Elements.Geometry;
using System.Collections.Generic;
using Xunit;

namespace Elements.Tests
{
    public class FloorTest : ModelTest
    {
        [Fact]
        public void Floor()
        {
            this.Name = "Elements_Floor";

            var p = Polygon.L(10, 20, 5);
            var openings = new List<Opening>(){
                new Opening(Polygon.Rectangle(1, 1), Vector3.ZAxis, transform: new Transform(1, 1, 0)),
                new Opening(Polygon.Rectangle(3, 3), Vector3.ZAxis, transform: new Transform(1, 3, 0)),
            };
            var floor = new Floor(p, 0.1, new Transform(0, 0, 0.5), new Material("green", Colors.Green, 0.0f, 0.0f));

            this.Model.AddElement(floor);
            this.Model.AddElements(openings);
        }
    }
}