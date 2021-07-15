using System.Collections.Generic;
using System.Linq;
using Elements.Geometry;
using Elements.Serialization.DXF.Extensions;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace Elements.Serialization.DXF
{
    /// <summary>
    /// Convert a floor to a dxf entity.
    /// </summary>
    public class ContentElementToDXF : DxfConverter<ContentElement>
    {
        /// <summary>
        /// Create a DXF Entity Object for a ContentElement.
        /// </summary>        
        public override void TryAddDxfEntity(DxfFile doc, ContentElement contentElement, DxfRenderContext context)
        {
            var chosenSymbol = pickSymbolByContext(contentElement, context);
            if (chosenSymbol == null)
            {
                // TODO: handle 
                return;
            }
            // TODO: make all this handle await?
            var geometry = chosenSymbol.GetGeometryAsync().GetAwaiter().GetResult();
            var polygons = geometry.OfType<Polygon>().Select(p => p.ToDxf());
            var polylines = geometry.OfType<Polyline>().Select(p => p.ToDxf());
            var blockName = contentElement.GetBlockName();
            var block = new DxfBlock
            {
                BasePoint = contentElement.Transform.ToDxfPoint(context),
                Name = blockName
            };
            doc.BlockRecords.Add(new DxfBlockRecord(blockName));
            foreach (var p in polygons.Union(polylines))
            {
                block.Entities.Add(p);
            }
            doc.Blocks.Add(block);
            // if it's not being used as an element definition, 
            // add an instance of it to the drawing.
            if (!contentElement.IsElementDefinition)
            {
                var insert = new DxfInsert
                {
                    Name = blockName,
                    Location = contentElement.Transform.ToDxfPoint(context),
                };
                doc.Entities.Add(insert);
            }
        }

        private Symbol pickSymbolByContext(ContentElement contentElement, DxfRenderContext context)
        {
            var contentTransform = contentElement.Transform;
            var drawingRangeTransform = context.DrawingRange?.Transform ?? new Transform();
            //TODO — pick an appropriate symbol based on the context orientation

            var symbol = contentElement.Symbols.FirstOrDefault(s => s.CameraPosition == SymbolCameraPosition.Top);
            return symbol;
        }
    }
}