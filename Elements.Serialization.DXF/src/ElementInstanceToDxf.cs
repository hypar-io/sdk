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
    /// Convert an element instance to a dxf entity.
    /// </summary>
    public class ElementInstanceToDXF : DxfConverter<ElementInstance>
    {
        /// <summary>
        /// Add a DXF entity to the document for an element instance.
        /// </summary>
        public override void TryAddDxfEntity(DxfFile document, ElementInstance elementInstance, DxfRenderContext context)
        {
            var insert = new DxfInsert
            {
                Location = elementInstance.Transform.ToDxfPoint(context),
                Rotation = elementInstance.Transform.ToDxfAngle(context),
                Name = elementInstance.BaseDefinition.GetBlockName()
            };
            document.Entities.Add(insert);
        }
    }
}