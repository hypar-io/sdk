using Elements.Geometry;
using System;
using Xunit;

namespace Elements.Tests
{
    public class FloorTests : ModelTest
    {
        [Fact, Trait("Category", "Examples")]
        public void Floor()
        {
            this.Name = "Elements_Floor";

            // <example>            
            // Create a floor with no elevation.
            var p = Polygon.L(10, 20, 5);
            var floor1 = new Floor(p, 0.1);

            // Create a floor with an elevation.
            var floor2 = new Floor(p, 0.1, new Transform(0, 0, 3));

            // Create some openings.
            floor1.AddOpening(1, 1, 1, 1);
            floor1.AddOpening(3, 3, 1, 3);
            // </example>

            Assert.Equal(0.0, floor1.Elevation);
            Assert.Equal(0.1, floor1.Thickness);
            Assert.Equal(0.0, floor1.Transform.Origin.Z);

            this.Model.AddElement(floor1);
            this.Model.AddElement(floor2);
        }

        [Fact]
        public void FloorWithAddedOpenings()
        {
            this.Name = "FloorWithAddedOpenings";

            var p = Polygon.L(10, 20, 5);
            var floor1 = new Floor(p, 0.1, new Transform(0, 0, 0.5), material: new Material("green", Colors.Green, 0.0f, 0.0f));

            var transRotate = new Transform();
            transRotate.Rotate(Vector3.ZAxis, 20.0);
            transRotate.Move(new Vector3(0, 0, 2));
            var floor2 = new Floor(p, 0.1, transRotate, material: new Material("blue", Colors.Blue, 0.0f, 0.0f));

            floor1.AddOpening(1, 1, 1, 1);
            floor1.AddOpening(3, 3, 1, 3);

            floor2.AddOpening(1, 1, 1, 1);
            floor2.AddOpening(3, 3, 1, 3);

            Assert.Equal(0.5, floor1.Elevation);
            Assert.Equal(0.1, floor1.Thickness);
            Assert.Equal(0.5, floor1.Transform.Origin.Z);

            this.Model.AddElements(new[] { floor1, floor2 });
        }

        [Fact]
        public void ZeroThickness()
        {
            var model = new Model();
            var poly = Polygon.Rectangle(width: 20, height: 20);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Floor(poly, 0.0));
        }

        [Fact]
        public void Area()
        {
            // A floor with two holes punched in it.
            var p1 = Polygon.Rectangle(1, 1);
            var p2 = Polygon.Rectangle(1, 1);
            var o1 = new Opening(p1, Vector3.ZAxis, 1, 1);
            var o2 = new Opening(p2, Vector3.ZAxis, 3, 3);
            var floor = new Floor(Polygon.Rectangle(10, 10), 0.2);
            Assert.Equal(100.0, floor.Area());
        }
    }
}