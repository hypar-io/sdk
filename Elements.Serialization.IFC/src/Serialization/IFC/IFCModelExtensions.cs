using Elements.Analysis;
using Elements.Geometry;
using IFC;
using STEP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Elements.Serialization.IFC
{
    /// <summary>
    /// Extension methods for writing elements to and from IFC.
    /// </summary>
    public static class IFCModelExtensions
    {
        /// <summary>
        /// Construct a model from an IFC.
        /// </summary>
        /// <param name="path">The path to an IFC Express file.</param>
        /// <param name="constructionErrors">Error messages which ocurred during model construction.</param>
        public static Model FromIFC2(string path, out List<string> constructionErrors)
        {
            List<STEPError> errors;
            var ifcModel = new Document(path, out errors);
            foreach (var error in errors)
            {
                Console.WriteLine("***IFC ERROR***" + error.Message);
            }

            var buildingElements = ifcModel.AllInstancesDerivedFromType<IfcBuildingElement>();

            var relVoids = ifcModel.AllInstancesOfType<IfcRelVoidsElement>();

            var model = new Model();

            constructionErrors = new List<string>();

            Dictionary<Guid, GeometricElement> elementDefinitions = new Dictionary<Guid, GeometricElement>();

            // var types = ifcModel.AllInstancesDerivedFromType<IfcBuildingElementType>();
            // var relTypes = ifcModel.AllInstancesOfType<IfcRelDefinesByType>();

            var materials = new Dictionary<string, Material>();
            var repMaterialMap = new Dictionary<Guid, Material>();

            // Get a unique set of materials that are used for all 
            // styled items. While we're doing that we also build
            // a map of the items that are being styled and their materials.

            // TODO: Some of our test models use IfcMaterialList, which is 
            // deprecated in IFC4. In the AC-Smiley model, for example, the
            // windows use a representation with a material list that has glass
            // for the window and wood for the frame. I think styled items is 
            // the modern way to do this, so we shouldn't support material lists.

            var styledItems = ifcModel.AllInstancesOfType<IfcStyledItem>();
            foreach (var styledItem in styledItems)
            {
                var item = styledItem.Item; // The representation item that is styled.
                if (item == null)
                {
                    continue;
                }
                foreach (var style in styledItem.Styles)
                {
                    if (style.Choice is IfcPresentationStyleAssignment)
                    {
                        var styleAssign = (IfcPresentationStyleAssignment)style.Choice;
                        foreach (var presentationStyle in styleAssign.Styles)
                        {
                            if (presentationStyle.Choice is IfcSurfaceStyle)
                            {
                                var surfaceStyle = (IfcSurfaceStyle)presentationStyle.Choice;
                                foreach (var styleElement in surfaceStyle.Styles)
                                {
                                    if (styleElement.Choice is IfcSurfaceStyleRendering)
                                    {
                                        var rendering = (IfcSurfaceStyleRendering)styleElement.Choice;
                                        var transparency = (IfcRatioMeasure)rendering.Transparency;
                                        var color = rendering.SurfaceColour.ToColor(1.0 - transparency);
                                        var name = surfaceStyle.Name;
                                        Material material = null;
                                        if (!materials.ContainsKey(name))
                                        {
                                            material = new Material(color,
                                                                    0.4,
                                                                    0.4,
                                                                    false,
                                                                    null,
                                                                    false,
                                                                    true,
                                                                    null,
                                                                    true,
                                                                    surfaceStyle.Id,
                                                                    surfaceStyle.Name);
                                            materials.Add(material.Name, material);
                                        }
                                        else
                                        {
                                            material = materials[name];
                                        }
                                        repMaterialMap.Add(item.Id, material);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var sites = ifcModel.AllInstancesOfType<IfcSite>();
            foreach (var site in sites)
            {
                var transform = site.ObjectPlacement.ToTransform();
                var rep = site.GetRepresentationFromProduct(model,
                                                            constructionErrors,
                                                            repMaterialMap,
                                                            out Transform mapTransform,
                                                            out Guid mapId,
                                                            out Material materialHint);
                if (rep == null)
                {
                    continue;
                }
                var geom = new GeometricElement(transform,
                                                        materialHint ?? BuiltInMaterials.Default,
                                                        rep,
                                                        false,
                                                        IfcGuid.FromIfcGUID(site.GlobalId),
                                                        site.Name);
                model.AddElement(geom);
            }

            foreach (var buildingElement in buildingElements)
            {
                try
                {
                    var transform = new Transform();
                    transform.Concatenate(buildingElement.ObjectPlacement.ToTransform());

                    // Check if the building element is contained in a building storey
                    foreach (var cis in buildingElement.ContainedInStructure)
                    {
                        transform.Concatenate(cis.RelatingStructure.ObjectPlacement.ToTransform());
                    }

                    var rep = buildingElement.GetRepresentationFromProduct(model,
                                                                           constructionErrors,
                                                                           repMaterialMap,
                                                                           out Transform mapTransform,
                                                                           out Guid mapId,
                                                                           out Material materialHint);

                    if (mapTransform != null)
                    {
                        GeometricElement definition = null;
                        if (elementDefinitions.ContainsKey(mapId))
                        {
                            definition = elementDefinitions[mapId];
                        }
                        else
                        {
                            definition = new GeometricElement(transform,
                                                        materialHint ?? BuiltInMaterials.Default,
                                                        rep,
                                                        true,
                                                        IfcGuid.FromIfcGUID(buildingElement.GlobalId),
                                                        buildingElement.Name);
                            elementDefinitions.Add(mapId, definition);
                        }

                        // The cartesian transform needs to be applied 
                        // before the element transformation because it
                        // may contain scale and rotation.
                        var instanceTransform = new Transform(mapTransform);
                        instanceTransform.Concatenate(transform);
                        var instance = definition.CreateInstance(instanceTransform, "test");

                        model.AddElement(instance);
                    }
                    else
                    {
                        if (rep.SolidOperations.Count == 0)
                        {
                            constructionErrors.Add($"{buildingElement.GetType().Name} did not have any solid operations in its representation.");
                            continue;
                        }

                        // TODO: Handle IfcMappedItem
                        // - Idea: Make Representations an Element, so that they can be shared.
                        // - Idea: Make PropertySet an Element. PropertySets can store type properties.
                        var geom = new GeometricElement(transform,
                                                        materialHint ?? BuiltInMaterials.Default,
                                                        rep,
                                                        false,
                                                        IfcGuid.FromIfcGUID(buildingElement.GlobalId),
                                                        buildingElement.Name);

                        var voids = relVoids.Where(v => v.RelatingBuildingElement == buildingElement).Select(v => v.RelatedOpeningElement).Cast<IfcOpeningElement>();
                        foreach (var v in voids)
                        {
                            var opening = v.ToOpening();
                            geom.Openings.Add(opening);
                        }

                        model.AddElement(geom);
                    }
                }
                catch (Exception ex)
                {
                    constructionErrors.Add(ex.Message);
                }
            }
            return model;
        }

        /// <summary>
        /// Load a model from IFC.
        /// </summary>
        /// <param name="path">The path to an IFC Express file.</param>
        /// <param name="idsToConvert">An array of element ids to convert.</param>
        /// <param name="constructionErrors">Error messages which ocurred during model construction.</param>
        /// <returns>A model.</returns>
        public static Model FromIFC(string path, out List<string> constructionErrors, IList<string> idsToConvert = null)
        {
            List<STEPError> errors;
            var ifcModel = new Document(path, out errors);
            foreach (var error in errors)
            {
                Console.WriteLine("***IFC ERROR***" + error.Message);
            }

            IEnumerable<IfcSlab> ifcSlabs = null;
            IEnumerable<IfcSpace> ifcSpaces = null;
            IEnumerable<IfcWallStandardCase> ifcWalls = null;
            IEnumerable<IfcBeam> ifcBeams = null;
            IEnumerable<IfcColumn> ifcColumns = null;
            IEnumerable<IfcRelVoidsElement> ifcVoids = null;
            IEnumerable<IfcRelAssociatesMaterial> ifcMaterials = null;

            if (idsToConvert != null && idsToConvert.Count > 0)
            {
                ifcSlabs = ifcModel.AllInstancesOfType<IfcSlab>().Where(i => idsToConvert.Contains(i.GlobalId));
                ifcSpaces = ifcModel.AllInstancesOfType<IfcSpace>().Where(i => idsToConvert.Contains(i.GlobalId));
                ifcWalls = ifcModel.AllInstancesOfType<IfcWallStandardCase>().Where(i => idsToConvert.Contains(i.GlobalId));
                ifcBeams = ifcModel.AllInstancesOfType<IfcBeam>().Where(i => idsToConvert.Contains(i.GlobalId));
                ifcColumns = ifcModel.AllInstancesOfType<IfcColumn>().Where(i => idsToConvert.Contains(i.GlobalId));
                ifcVoids = ifcModel.AllInstancesOfType<IfcRelVoidsElement>().Where(i => idsToConvert.Contains(i.GlobalId));
                ifcMaterials = ifcModel.AllInstancesOfType<IfcRelAssociatesMaterial>().Where(i => idsToConvert.Contains(i.GlobalId));
            }
            else
            {
                ifcSlabs = ifcModel.AllInstancesOfType<IfcSlab>();
                ifcSpaces = ifcModel.AllInstancesOfType<IfcSpace>();
                ifcWalls = ifcModel.AllInstancesOfType<IfcWallStandardCase>();
                ifcBeams = ifcModel.AllInstancesOfType<IfcBeam>();
                ifcColumns = ifcModel.AllInstancesOfType<IfcColumn>();
                ifcVoids = ifcModel.AllInstancesOfType<IfcRelVoidsElement>();
                ifcMaterials = ifcModel.AllInstancesOfType<IfcRelAssociatesMaterial>();
            }

            constructionErrors = new List<string>();

            var slabs = new List<Floor>();
            foreach (var s in ifcSlabs)
            {
                try
                {
                    slabs.Add(s.ToFloor(ifcVoids.Where(v => v.RelatingBuildingElement == s).Select(v => v.RelatedOpeningElement).Cast<IfcOpeningElement>()));
                }
                catch (Exception ex)
                {
                    constructionErrors.Add(ex.Message);
                    continue;
                }

            }

            var spaces = new List<Space>();
            foreach (var sp in ifcSpaces)
            {
                try
                {
                    spaces.Add(sp.ToSpace());
                }
                catch (Exception ex)
                {
                    constructionErrors.Add(ex.Message);
                    continue;
                }
            }

            var walls = new List<Wall>();
            foreach (var w in ifcWalls)
            {
                try
                {
                    walls.Add(w.ToWall(ifcVoids.Where(v => v.RelatingBuildingElement == w).Select(v => v.RelatedOpeningElement).Cast<IfcOpeningElement>()));
                }
                catch (Exception ex)
                {
                    constructionErrors.Add(ex.Message);
                    continue;
                }
            }

            var beams = new List<Beam>();
            foreach (var b in ifcBeams)
            {
                try
                {
                    beams.Add(b.ToBeam());
                }
                catch (Exception ex)
                {
                    constructionErrors.Add(ex.Message);
                    continue;
                }
            }

            var columns = new List<Column>();
            foreach (var c in ifcColumns)
            {
                try
                {
                    columns.Add(c.ToColumn());
                }
                catch (Exception ex)
                {
                    constructionErrors.Add(ex.Message);
                    continue;
                }
            }

            var model = new Model();
            model.AddElements(slabs);
            model.AddElements(spaces);
            model.AddElements(walls);
            model.AddElements(beams);
            model.AddElements(columns);

            return model;
        }

        /// <summary>
        /// Write the model to IFC.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="path">The path to the generated IFC STEP file.</param>
        public static void ToIFC(this Model model, string path)
        {
            var ifc = new Document("Elements", "Elements", Environment.UserName,
                                    null, null, null, "Elements", null, null,
                                    null, null, null, null, null, null
                                    );

            var proj = ifc.AllInstancesOfType<IfcProject>().FirstOrDefault();

            // Add a site
            var site = new IfcSite(IfcGuid.ToIfcGuid(Guid.NewGuid()),
                                   null,
                                   "Hypar Site",
                                   "The default site generated by Hypar",
                                   null,
                                   null,
                                   null,
                                   null,
                                   IfcElementCompositionEnum.ELEMENT,
                                   new IfcCompoundPlaneAngleMeasure(new List<int> { 0, 0, 0 }),
                                   new IfcCompoundPlaneAngleMeasure(new List<int> { 0, 0, 0 }),
                                   0,
                                   null,
                                   null);
            var projAggregate = new IfcRelAggregates(IfcGuid.ToIfcGuid(Guid.NewGuid()), proj, new List<IfcObjectDefinition> { site });

            // Add building and building storey
            var building = new IfcBuilding(IfcGuid.ToIfcGuid(Guid.NewGuid()),
                                           null,
                                           "Default Building",
                                           "The default building generated by Hypar.",
                                           null,
                                           null,
                                           null,
                                           null,
                                           IfcElementCompositionEnum.ELEMENT,
                                           0,
                                           0,
                                           null);
            var storey = new IfcBuildingStorey(IfcGuid.ToIfcGuid(Guid.NewGuid()),
                                               null,
                                               "Default Storey",
                                               "The default storey generated by Hypar",
                                               null,
                                               null,
                                               null,
                                               null,
                                               IfcElementCompositionEnum.ELEMENT,
                                               0);
            var aggregate = new IfcRelAggregates(IfcGuid.ToIfcGuid(Guid.NewGuid()), building, new List<IfcObjectDefinition> { storey });

            // Aggregate the building into the site
            var siteAggregate = new IfcRelAggregates(IfcGuid.ToIfcGuid(Guid.NewGuid()), site, new List<IfcObjectDefinition> { building });

            ifc.AddEntity(site);
            ifc.AddEntity(projAggregate);
            ifc.AddEntity(building);
            ifc.AddEntity(storey);
            ifc.AddEntity(aggregate);
            ifc.AddEntity(siteAggregate);

            var products = new List<IfcProduct>();
            var context = ifc.AllInstancesOfType<IfcGeometricRepresentationContext>().FirstOrDefault();

            // IfcRelAssociatesMaterial
            // IfcMaterialDefinitionRepresentation
            // https://forums.buildingsmart.org/t/where-and-how-will-my-colors-be-saved-in-ifc/1806/12
            var styleAssignments = new Dictionary<Guid, List<IfcStyleAssignmentSelect>>();

            var white = Colors.White.ToIfcColourRgb();
            ifc.AddEntity(white);

            // TODO: Fix color support in all applications.
            // https://forums.buildingsmart.org/t/why-is-it-so-difficult-to-get-colors-to-show-up/2312/12
            foreach (var m in model.AllElementsOfType<Material>())
            {
                var material = new IfcMaterial(m.Name, null, "Hypar");
                ifc.AddEntity(material);

                var color = m.Color.ToIfcColourRgb();
                ifc.AddEntity(color);

                var transparency = new IfcNormalisedRatioMeasure(1.0 - m.Color.Alpha);

                var shading = new IfcSurfaceStyleShading(color, transparency);
                ifc.AddEntity(shading);

                var styles = new List<IfcSurfaceStyleElementSelect>{
                    new IfcSurfaceStyleElementSelect(shading),
                };
                var surfaceStyle = new IfcSurfaceStyle(material.Name, IfcSurfaceSide.BOTH, styles);
                ifc.AddEntity(surfaceStyle);

                var styleAssign = new IfcStyleAssignmentSelect(surfaceStyle);
                var assignments = new List<IfcStyleAssignmentSelect>() { styleAssign };
                styleAssignments.Add(m.Id, assignments);
            }


            foreach (var e in model.Elements.Values.Where(e =>
            {
                var t = e.GetType();
                return ((e is GeometricElement &&
                        !((GeometricElement)e).IsElementDefinition) || e is ElementInstance) &&
                        t != typeof(ModelCurve) &&
                        t != typeof(ModelPoints) &&
                        t != typeof(AnalysisMesh);
            }))
            {
                try
                {
                    products.AddRange(e.ToIfcProducts(context, ifc, styleAssignments));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"There was an error writing an element of type {e.GetType()} to IFC: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    continue;
                }
            }

            var spatialRel = new IfcRelContainedInSpatialStructure(IfcGuid.ToIfcGuid(Guid.NewGuid()), products, storey);
            ifc.AddEntity(spatialRel);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, ifc.ToSTEP(path));
        }
    }
}