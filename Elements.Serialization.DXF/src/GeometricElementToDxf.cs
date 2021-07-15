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
    /// A concrete implementation of IRenderDxf for any GeometricElement. This
    /// just uses the base implementation of GeometricDxfConverter.
    /// </summary>
    public class GeometricElementToDxf : GeometricDxfConverter<GeometricElement>
    {

    }
}