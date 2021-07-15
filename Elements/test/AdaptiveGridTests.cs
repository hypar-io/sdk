using Elements.Geometry;
using Elements.Spatial.AdaptiveGrid;
using Elements.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Elements.Tests
{
    public class AdaptiveGridTests : ModelTest
    {
        [Fact]
        public void AdaptiveGridWithFixedStepSizeExample()
        {
            this.Name = "Elements_Spatial_AdaptiveGrid_AdaptiveGridWithFixedStepSize";
            var random = new Random();

            var adaptiveGrid = new AdaptiveGrid(0, new Transform());
            var transform = new Transform(new Vector3(10, 5, 3));
            adaptiveGrid.AddFromPolygon(Polygon.Rectangle(10, 5).TransformedPolygon(transform), 5);
            foreach (var edge in adaptiveGrid.GetEdges())
            {
                Model.AddElement(new ModelCurve(edge.GetGeometry(), material: random.NextMaterial()));
            }
        }
        
        [Fact]
        public void AdaptiveGridPolygonKeyPointsExample()
        {
            this.Name = "Elements_Spatial_AdaptiveGrid_AdaptiveGridPolygonKeyPoints";
            var random = new Random();

            var adaptiveGrid = new AdaptiveGrid(0, new Transform());
            var points = new List<Vector3>()
            {
                new Vector3(-6, -4),
                new Vector3(-2, -4),
                new Vector3(3, -4),
                new Vector3(1, 4.5),
                new Vector3(6, 3),
            };
            adaptiveGrid.AddFromPolygon(Polygon.Rectangle(15, 10).TransformedPolygon(new Transform(new Vector3(), new Vector3(10, 0, 10))), points);

            foreach (var edge in adaptiveGrid.GetEdges())
            {
                Model.AddElement(new ModelCurve(edge.GetGeometry(), material: random.NextMaterial()));
            }
        }


        [Fact]
        public void AdaptiveGridBboxKeyPointsExample()
        {
            this.Name = "Elements_Spatial_AdaptiveGrid_AdaptiveGridBboxKeyPoints";
            var random = new Random();

            var adaptiveGrid = new AdaptiveGrid(0, new Transform());
            var points = new List<Vector3>()
            {
                new Vector3(-6, -4),
                new Vector3(-2, -4),
                new Vector3(3, -4),
                new Vector3(1, 4.5, 3),
                new Vector3(6, 3, -2),
            };
            adaptiveGrid.AddFromBbox(new BBox3(new Vector3(-7.5, -5, -3), new Vector3(10, 10, 3)), points);
            adaptiveGrid.AddFromPolygon(Polygon.Rectangle(new Vector3(-10, -5), new Vector3(15, 10)).TransformedPolygon(new Transform(new Vector3(0, 0, 3))), 2);
            adaptiveGrid.AddFromPolygon(Polygon.Rectangle(new Vector3(-10, -5), new Vector3(15, 10)).TransformedPolygon(new Transform(new Vector3(0, 0, 2))), 2);

            foreach (var edge in adaptiveGrid.GetEdges())
            {
                Model.AddElement(new ModelCurve(edge.GetGeometry(), material: random.NextMaterial()));
            }
        }
    }
}
