# Changelog

## 0.9.3

### Changed

- Added `Symbols` property to `ContentElement`.

## 0.9.2

### Added

- `Polyline.Split(List<Vector3> point)`
- `Polygon.Split(List<Vector3> point)`
- `Polygon.TrimmedTo(List<Polygon> polygons)`
- `Vector3.>=`
- `Vector3.<=`
- `Plane.Intersects(Plane a, Plane b)`
- `GeometricElement.Openings`

### Removed
- `IHasOpenings`
- A handful of convenience operators and conversions:
  - implicit `(double X, double Y, double Z)` => `Vector3`
  - implicit `(double X, double Y)` => `Vector3`
  - implicit `(int X, int Y, int Z)` => `Vector3`
  - implicit `(int X, int Y)` => `Vector3`
  - implicit `(double R, double G, double B, double A)` => `Color`
  - implicit `(double R, double G, double B)` => `Color`
  - `new Polygon(params Vector3[] vertices)`
  - `new Polyline(params Vector3[] vertices)`
  - implicit `SolidOperation` => `Representation`
  - `new Representation(params SolidOperation[] solidOperations)`
  - `Polygon.Split(params Polyline[] polylines)`
  - `Polygon.UnionAll(params Polygon[] polygons)`
  - `Polygon.Difference(params Polygon[] polygons)`
  - `Polygon.Union(params Polygon[] polygons)`
- `Profile.Offset()`

### Changed

- Some changes to `ContentElement` instance glTF serialization to allow selectability and transformability in the Hypar UI.

### Fixed

- [#616](https://github.com/hypar-io/Elements/issues/616) Code generation from local files now supplies a directory path to help resolve local references.

## 0.9.1

### Added

- `Transform(Plane plane)`
- `Polygon.Trimmed(Plane plane, bool flip)`
- ~~`GetSolid()` method on GeometricElement that returns the Csg solid.~~
- `ToMesh()` method on GeometricElement that return the mesh of a processed solid.
- `Polygon.ToTransform()`
- `Elements.Anaysis.AnalysisImage`
- `Profile.CreateFromPolygons(IEnumerable<Polygon> polygons)`
- `CellComplex`:
  - `Cell.TraverseNeighbors(Vector3 target, double? completedRadius)`
  - `Edge.TraverseNeighbors(Vector3 target, double? completedRadius)`
  - `Face.TraverseNeighbors(Vector3 target, double? completedRadius)`
  - `Face.TraverseNeighbors(Vector3 target, bool? parallel, bool? includeSharedVertices, double? completedRadius)`
- Dxf creation framework with first Dxf converter.
- `new BBox3(Element element)`
- `Bbox3.Corners()`
- `Vector3.AreCollinear(Vector3 a, Vector3 b, Vector3 c)`
- `Polygon.CollinearPointsRemoved()`

### Changed

- `AnalysisMesh` now handles single valued analysis.
- `Polygon.Split()` can now handle polygons that are not in the XY plane.
- Leave the discriminator property during deserialization. It will go to AdditionalProperties.
- `Lamina` representations can now include voids/holes.

### Fixed

- Guard against missing transforms while generating CSGs.
- Fixed a bug ([#585](https://github.com/hypar-io/Elements/issues/585)) where CSG Booleans for certain representations (especially laminae) would fail.
- Guard against missing transforms while generating CSGs.
- In rare cases a `Line.Intersect(Line)` call would fail if there were near-duplicate vertices, this is fixed.
- `Grid1d.ClosestPosition` now does a better job finding points on polyline axes.
- Code-generated constructors now get default arguments for inherited properties.
- `IsDegenerate()` method was reversed.
- Adding content elements that contain multiple nodes used to only add the first mesh, now it adds all the nodes in the referenced glb hierarchy.
- `Transform.OfVector(Vector)` is not applying translation anymore as vector doesn't have a position by definition.

## 0.9.0

### Added

- `Grid2d.IsOutside()`
- `GraphicsBuffers`

### Removed

- `BuiltInMaterials.Dirt`
- `BuiltInMaterials.Grass`

### Changed

- `Grid2d.IsTrimmed()` now takes an optional boolean parameter `treatFullyOutsideAsTrimmed`
- `ConstructedSolid` serializes and deserializes correctly.
- `Solid.AddFace(Polygon, Polygon[])` can take an optional third `mergeVerticesAndEdges` argument which will automatically reuse existing edges + vertices in the solid.
- Adds optional `tolerance` parameter to `Line.ExtendTo(Polygon)`, `Line.ExtendTo(IEnumerable<Line>)`, `Vector3.IsParallelTo(Vector3)`.
- `Mesh.GetBuffers` now returns a `GraphicsBuffers` object.
- `Solid.Tessellate` now returns a `GraphicsBuffers` object.
- `CsgExtensions.Tessellate` now returns a `GraphicsBuffers` object.

### Fixed

- Fixed a bug in `ConvexHull.FromPoints` when multiple X coordinates are equal.
- Fixed a bug in `Grid2d(Polygon, Vector3, Vector3, Vector3)` where U or V directions skew slightly when they nearly parallel with a boundary edge.

## 0.8.5

### Added

- `Elements.Spatial.CellComplex`
- `Grid2d.ToModelCurves()`
- Alpha release of `Hypar.Elements.Components`
- `Polyline.OffsetOnSide`
- `Ray.Intersects(Polygon)`
- `Vector3.DistanceTo(Polygon)`
- `(double Cut, double Fill) Topography.CutAndFill(Polygon perimeter, double topElevation, out Mesh cutVolume, out Mesh fillVolume, double batterAngle)`
- `void Topography.Trim(Polygon perimeter)`

### Changed

- `ColorScale` no longer bands colors but returns smooth gradient interpolation. It additionally now supports a list of values that correspond with the provided colors, allowing intentionally skewed interpolation.
- `Solids.Import` is now public.
- `Polygon.Contains` was modified to better handle polygons that are not on the XY plane.

### Fixed

## 0.8.4

### Added

- `BBox3.IsValid()`
- `BBox3.IsDegenerate()`
- `Elements.Light`
- `Elements.PointLight`
- `Elements.SpotLight`
- `Identity.AddOverrideIdentity`
- `Material.NormalTexture`
- `Polygon.PointInternal()`
- `Topography.DepthMesh`
- `Topography.DepthBelowMinimumElevation`
- `Topography.AbsoluteMinimumElevation`
- `Material.RepeatTexture`
- `BBox3.IsValid()`
- `BBox3.IsDegenerate()`
- `Polygon.Split(Polyline)`
- `Polygon.Split(IEnumerable<Polyline> polylines)`
- `Profile.Split(IEnumerable<Profile>, IEnumerable<Polyline> p)`
- `Elements.Spatial.HalfEdgeGraph2d`
  - `.Construct()`
  - `.Polygonize()`
- Release helper github action

### Changed

- `Elements.DirectionalLight` now inherits from `Elements.Light`.
- `Elements.ContentCatalog` now has a `ReferenceConfiguration` property.
- `SHSProfile`
- `SHSProfileFactory`
- `RHSProfile`
- `RHSProfileFactory`
- `Spatial.WebMercatorProjection.GetTileSizeMeters` produces a much more accurate result and requires a latitude.
- Adding glb elements to a model uses a cache rather than fetching the stream every time.
- `ProfileServer` is now `ProfileFactory`
- `WideFlangeProfileServer` is now `WideFlangeProfileFactory`
- First alpha after minor release logic was fixed
- `HSSPipeProfileServer` is now `HSSPipeProfileFactory`
- TypeGeneratorTests weren't actually running.
- `Profile.Split(IEnumerable<Profile>, Polyline p)` now uses improved logic

## 0.8.3

### Added

- `Profile.ToModelCurves()`
- `Profile.Difference()`
- `Profile.Intersection()`
- `Profile.Split()`
- `Profile.Segments()`
- `Bbox3.ToModelCurves()`
- `Line.ExtendTo(IEnumerable<Line>)`
- `Line.ExtendTo(Polyline)`
- `Line.ExtendTo(Profile)`
- `Line.ExtendTo(Polygon)`
- `ConvexHull.FromPoints(IEnumerable<Vector3>)`
- `ConvexHull.FromPolyline(Polyline)`
- `ConvexHull.FromPolylines(IEnumerable<Polyline>)`
- `ConvexHull.FromProfile(Profile)`
- `Polygon.FromAlignedBoundingBox2d(IEnumerable<Vector3>)`
- `Grid2d(Polygon, Grid1d, Grid1d)`
- `Grid2d(IEnumerable<Polygon>, Grid1d, Grid1d)`
- `Grid2d(Polygon, Vector3, Vector3, Vector3)`
- `Grid2d(IEnumerable<Polygon>, Vector3, Vector3, Vector3)`
- `Random.NextColor()` and `Random.NextMaterial()`
- `Validator.DisableValidationOnConstruction`
- `Vector3.ComputeDefaultBasisVectors()`

### Changed

- Make MeshConverter deserialization more flexible to accommodate a schema used in function `input_schema`.
- Prevent the Polygon / Polyline constructors from throwing an exception on duplicate vertices, by removing duplicates automatically.
- Make `Grid1d` and `Grid2d` serializable
- `new Transform(Vector3 origin, Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)` did not unitize its axes, this is fixed.
- All solids and csgs will now have planar texture coordinates.
- Triangles are now validated to check for 3 distinct vertex positions.

### Fixed

- Fixed a bug where `Polygon.UnionAll` was sometimes returning null when it shouldn't (Thanks @M-Juliani !)
- Fixed [#517](https://github.com/hypar-io/Elements/issues/517)
- Fixed a bug where Grid2d subcells would not split correctly with `SplitAtPoint`
- Fixed [#528](https://github.com/hypar-io/Elements/issues/528)

## 0.8.2

### Changed

- The errors parameter for Model.FromJson now has the out modifier. It no longer takes a default value.
- Model deserialization only refreshes type cache if the `forceTypeReload` parameter is set to true.

### Fixed

- Fixed #483 `Deserialization of profiles created in UpdateRepresentation`
- Fixed #484 `Failure to deserialize Model if any assembly can't be loaded.`
- Fixed an issue where updates to a `Grid2d`'s component `Grid1d` axes would not propagate to the `Grid2d`.

### Added

- `Profile.UnionAll(...)` - Create a new set of profiles, merging any overlapping profiles and preserving internal voids.
- `Polyline.SharedSegments()` - Enables search for segments shared between two polylines.
- `Polyline.TransformSegment(...)` - Allows transforms for individual polyline segments. May be optionally flagged as polygon and/or planar motion.

## 0.8.1

### Added

- `TypeGenerator.CoreTypeNames`
- `MeshConverter`
- Implicit conversion of `Curve` types to `ModelCurve`.
- `byte[] Model.ToGlb(...)`
- `Grid1d.ClosestPosition()`
- `Grid1d.DivideByFixedLengthFromPoint()`
- `Grid2d.GetCellNodes()`

### Changed

- Removed `JsonInheritanceAttribute` from `Element` base types.
- `Sweep` contstructor now takes a rotation. Transformation of the profile based on rotation happens internal to the sweep's construction.
- `Polyline` are no longer required to be planar.
- Modifies Grid1d.DivideByFixedLengthFromPosition() to be more flexible — supporting a "position" outside the grids domain.
- Modifies Grid2d.GetCellSeparators() to support returning trimmed separators

### Removed

- `TypeGenerator.GetCoreTypeNames()`
- `UserElementAttribute`
- `NumericProperty`

### Fixed

- `Ray.Intersects` now calls `UpdateRepresentations` internally for accurate intersections.
- Fixed #470
- Fixes a bug in `Line.Trim(Polygon p)` where lines that started on the polygon would not be treated as outside the polygon.
- Fixes a bug in `Grid2d.IsTrimmed()` that would ignore cases where a cell was trimmed by an inner hole.

## 0.8.0

### Added

- `Hypar.Elements.Serialization.IFC` - IFC serialization code has been moved to a new project.
- `Hypar.Elements.CodeGeneration` - Code generation has been moved to a new project.
- `Elements.DirectionalLight` - You can now create a directional light in the model which will be written to glTF using the `KHR_lights_punctual` extension.
- `Elements.ContentElement` - This new class represents a piece of content meant to be instanced throughout a model.
  - The ContentElement is also added to the model by first checking for an available gltf, and then using a bounding box representation as a fallback.
- `Transform.Scaled()` - This new method returns a scaled copy of the transform, allowing for a fluent like api.
- `Transform.Moved(...)` - Return a copy of a transform moved by the specified amount.
- `Transform.Concatenated(...)` - Return a copy of a transform with the specified transform concatenated with it.
- `IHasOpenings.AddOpening(...)` - `AddOpening` provides an API which hides the internals of creating openings.
- `Opening.DepthFront` & `Opening.DepthBack` enable the creation of openings which extrude different amounts above and below their XY plane.
- Solid operations which have `IsVoid=true` now use csg operations.

### Changed

- Updated ImageSharp to 1.0.0.
- The source code is now structured with the typical .NET project layout of `/src` and `/test` per folder.
- `Opening` now has two primary constructors. The ability to create an opening with a profile has been removed. All profiles are now defined with a polygon as the perimeter.
- `Opening.Profile` is now deprecated. Please use `Opening.Perimeter`.
- `Polygon.Normal()` has been moved to the base class `Polyline.Normal()`.

### Fixed

- Fixed #313.
- Fixed #322.
- Fixed #342.
- Fixed #392.
- Fixed #407.
- Fixed #408
- Fixed #416.
- Fixed #417.
- Fixed #441

## 0.7.3

### Fixed

- CodeGen was failing intermittently
- Elements schemas with Dictionary types were failing to serialize
- [#355](https://github.com/hypar-io/Elements/issues/355)

### Added

- Elements supports the [Hub beta](https://hypar-io.github.io/Elements/Hub.html)
- CodeGen supports `input_schema`
- Hypar.Revit is completely rewritten as an external application, two external commands, and an IDirectContext3D server.

### Changed

- some Tessellate method signatures are updated to allow assigning colors at the time of tessellation as Revit requires vertex colors.
- Updates are made to the type generator to support compiling multiple types into an assembly on disk.

## 0.7.2

### Fixed

- [#307](https://github.com/hypar-io/Elements/issues/307)
- `Mesh.ComputeNormals()` would fail if there were any unconnected vertices.
- `new Vertex()` would ignore supplied Normals.
- `Vector3.ClosestPointOn(Line)` would return points that were not on the line.
- `Line.Intersects(Line)` in infinite mode would sometimes return erroneous results.
- `Vector3.AreCollinear()` would return the wrong result if the first two vertices were coincident.

### Added

- Added `Plane.ClosestPoint(Vector3 p)`
- New methods for dynamic type generation in `TypeGeneration`, utilized by the Grasshopper plugin.
- `Line.Trim(Polygon)`
- `Line.PointOnLine(Vector3 point)`
- **Grid1d**
  - `Grid1d(Grid1d other)` constructor
  - Adds `IgnoreOutsideDomain` flag to `SplitAtOffset`
  - Adds `SplitAtPoint(point)` and `SplitAtPoints(points)` methods
  - Adds internal `Evaluate(t)` method
  - Adds internal `SpawnSubGrid(domain)` method
- **Grid2d**
  - Adds `Grid2d(Grid2d other)` constructor
  - Adds `Grid2d(Grid2d other, Grid1d u, Grid1d v)` constructor
  - Adds `SplitAtPoint(point)` and `SplitAtPoints(points)` methods to Grid2d
  - Adds `Grid2d(Grid1d u, Grid1d v)` constructor
  - Adds support for curved 1d Grid axes
  - Adds private `SpawnSubGrid(Grid1d uCell, Grid1d vCell)` method
- `Curve.Transformed(transform)` (and related `XXX.TransformedXXX()` methods for child types Arc, Bezier, Line, Polyline, Polygon)

### Changed

- Updates to Elements Docs including Grasshopper + Excel.
- `Line.Intersects(Plane p)` supports infinite lines
- `Line.Intersects(Line l)` supports 3d line intersections
- `Line.Intersects(Line l)` now has an optional flag indicating whether to include the line ends as intersections.
- `Line.PointOnLine(Point)` now has an optional flag indicating whether to include points at the ends as "on" the line.
- **Grid1d / Grid2d**
  - Removes "Parent/child" updating from 1d grids / 2d grids in favor of always recalculating the 2d grid every time its `Cells` are accessed. This may have a bit of a performance hit, but it's worth it to make 2d grid behavior easier to reason about.
  - Allows Grid2ds to support construction from Grid1ds that are not straight lines. Previously Grid1ds could support any sort of curve and Grid2ds were stuck as dumb rectangles.
- **JsonInheritanceConverter**
  - Makes the Type Cache on the JsonInheritanceConverter static, and exposes a public method to refresh it. In the grasshopper context, new types may have been dynamically loaded since the JsonInheritanceConverter was initialized, so it needs to be refreshed before deserializing a model.
- **TypeGenerator**
  - Enables external overriding of the Templates path, as in GH's case the `Templates` folder is not in the same place as the executing assembly.
  - Exposes a public, synchronous method `GetSchema` to get a `JsonSchema` from uri (wrapping `GetSchemaAsync`)
  - Refactors some of the internal processes of `GenerateInMemoryAssemblyFromUrisAndLoadAsync`:
    - `GenerateCodeFromSchema()` produces csharp from a schema, including generating the namespace, typename, and local excludes
    - `GenerateCompilation()` takes the csharp and compiles it, using a new optional flag `frameworkBuild` to designate whether it should load netstandard or net framework reference assemblies.
    - `EmitAndLoad()` generates the assembly in memory and loads it into the app domain.
  - Adds an `EmitAndSave()` method that generates the assembly and writes it to a .dll on disk
  - Adds a public `GenerateAndSaveDllForSchema()` method used by grasshopper that generates code from a schema, generates a compilation, and saves it to disk as a DLL.
  - Adds a public `GetLoadedElementTypes()` method used by grasshopper to list all the currently loaded element types.

### Deprecated

- `Transform.OfXXX(xxx)` curve methods have been deprecated in favor of `XXX.Transformed(Transform)` and `XXX.TransformedXXX(Transform)`.

## 0.7.0

### Fixed

- [#271](https://github.com/hypar-io/Elements/issues/271)
- [#284](https://github.com/hypar-io/Elements/issues/284)
- [#285](https://github.com/hypar-io/Elements/issues/285)
- [#265](https://github.com/hypar-io/Elements/issues/265)
- [#221](https://github.com/hypar-io/Elements/issues/221)
- [#229](https://github.com/hypar-io/Elements/issues/229)
- [#189](https://github.com/hypar-io/Elements/issues/189)

### Added

- `Curve.ToPolyline(int divisions = 10)`
- `Circle.ToPolygon(int divisions = 10)`
- `Transform.Move(double x, double y, double z)`
- `Transform.Rotate(double angle)`
- `TypeGenerator.GenerateUserElementTypesFromUrisAsync(string[] uris, string outputBaseDir, bool isUserElement = false)`

### Changed

- Updated documentation to reflect the use of .NET Core 3.1.

### Deprecated

- `Polygon.Circle(...)`

## 0.6.2

### Added

- `Material.Unlit`
- `Material.DoubleSided`
- `Units.LengthUnit`
- `Elements.MeshImportElement`

## Changed

- `Mesh.AddVertex(...)` is now public.
- `Mesh.AddTriangle(...)` is now public.

### Removed

- `SolidOperation.GetSolid()`.

### Fixed

- #262
- Fixed an error where `Transform.OfPlane(...)` would not solve correctly if the plane was not at the world origin.

### Changed

- `Grid2d` now supports grids that are not parallel to the XY plane

## 0.6.0

### Added

- `ColorScale`
- `AnalysisMesh`
- `Ray.Intersects(...)` for `Plane`, `Face`, `Solid`, and `SolidOperation`

### Fixed

- Fix #253

## 0.5.2

### Fixed

- `Grid2d` constructors accepting a Transform interpreted the transform incorrectly.

## 0.5.1

### Added

- `Grid1d`
- `Grid2d`
- `Domain1d`
- `GeometricElement.IsElementDefinition`
- A `drawEdges` optional parameter to `Model.ToGlTF(...)` to enable edge rendering.
- `Polyline` and `Profile` now implement `IEquatable`.
- `Polygon.Union(IList<Polygon> firstSet, IList<Polygon> secondSet)`
- `Polygon.Difference(IList<Polygon> firstSet, IList<Polygon> secondSet)`
- `Polygon.XOR(IList<Polygon> firstSet, IList<Polygon> secondSet)`
- `Polygon.Intersection(IList<Polygon> firstSet, IList<Polygon> secondSet)`

### Changed

- `Vector.Normalized()` is now `Vector.Unitized()`
- `Color.ToString()` now returns a useful description

### Fixed

- Fixed an error with `Transform.OfVector(...)` where the translation of the transform was not applied.
- Fixed an error where `Mesh.ComputeNormals(...)` was not set to a unitized vector.
- Fixed an error with `BBox3`'s solver for Polygons

## 0.4.4

### Added

- `Contour`
- `Transform.Reflect(Vector3 n)`
- `ElementInstance`
- `Vector3.ClosestPointOn(Line line)`
- `Line.TrimTo(Line line)`
- `Line.ExtendTo(Line line)`
- `Line.Offset(double distance, bool flip = false)`
- `Line.DivideByLengthFromCenter(double l)`
- `Ray.Intersects(Ray ray, out Vector3 result, bool ignoreRayDirection)`
- `Polygon.Fillet(double radius)`
- `Arc.Complement()`
- `Polygons.Star(double outerRadius, double innerRadius, int points)`
- `Units.CardinalDirections`
- `Mesh.ComputeNormals`
- `Topography.AverageEdges(Topography target, Units.CardinalDirection edgeToAverage)`
- `Topography.GetEdgeVertices(Units.CardinalDirection direction)`
- `WebMercatorProjection`

### Fixed

- Fixed [#125](https://github.com/hypar-io/Hypar/issues/125).
- Fixed one Transform constructor whose computed axes were not unit length, causing the transform to scale.
- Topography is now written to IFC.

## 0.4.2

### Changed

- `Vector3` is now a struct.
- `Color` is now a struct.
- `ProfileServer.GetProfileByName(...)` is now deprecated in favor of `ProfileServer.GetProfileByType(...)`

### Added

- `Bezier`
- `WideFlangeProfileType`
- `HSSPipeProfileType`
- `Curve.MinimumChordLength` static property to allow the user to set the minimum chord length for subdivision of all curves for rendering.
- `Circle`
- `FrameType` Bezier curves can have their frames calculated using Frenet frames or "road like" frames.

## 0.4.0

### Changed

- All element types are partial classes with one part of the class generated from its JSON schema.
- `Polygon.Rectangle` constructor no longer takes an origin.
- `Polygon.Clip` now takes an optional additional set of holes.
- `Wall` and `Floor` constructors no longer take collections of `Opening`s.
  - Openings can be added using `wall.Openings.Add(...)`.
- `Polygon` now has more robust checks during construction.
  - All vertices must be coplanar.
  - Zero length segments are not allowed.
  - Self-intersecting segments are not allowed.
- `Solid`, `Face`, `Vertex`, `Edge`, `HalfEdge`, and `Loop` are now marked `internal`.
- `Quaternion` is now marked `internal`.
- `Matrix` is now marked `internal`.
- `SolidConverter` is now marked `internal`.
- `Elements.Serialization.IFC.ModelExtensions` is now marked `internal`.
- All core type property setters are now `public`.
- The `elevation` parameter has been removed from `Floor`. Floor elevation is now set by passing a `Transform` with a Z coordinate.

### Added

- `ModelCurve` - Draw curves in 3D.
- `ModelPoints` - Draw collections of points in 3D.
- `Elements.Generate.TypeGenerator` class.
- `/Schemas` directory.
- Optional `rotation` on `StructuralFraming` constructors.
- `Model` now has Elements property which is `IDictionary<Guid,Element>`.
- `double Vector3.CCW(Vector3 a, Vector3 b, Vector3 c)`
- `bool Line.Intersects(Line l)`
- `Elements.Validators.IValidator` and the `Elements.Validators.Validator` singleton to provide argument validation during construction of user elements.
- `Line.DivideByLength()`
- `Line.DivideByCount()`
- `Ray` class.
- `Vector3.IsZero()`

### Removed

- The empty Dynamo project.
- `ElementType`, `WallType`, `FloorType`, `StructuralFramingType`
- `MaterialLayer`
- `Transform` constructor taking `start` and `end` parameters. The `Transform` constructor which takes an X and a Z axis should now be used.

### Fixed

- Transforms are now consistently right-handed.
- Transforms on curves are now consistently oriented with the +X axis oriented to the "right" and the +Z axis oriented along the inverse of the tangent of the curve.
- Built in materials for displaying transforms are now red, green, and blue. Previously they were all red.
- All classes deriving from `Element` now pass their `id`, `transform`, and `name` to the base constructor.
- Line/plane intersections now return null if the intersection is "behind" the start of the line.
- Beams whose setbacks total more than the length of the beam no longer fail.
- Plane construction no longer fails when the normal vector and the origin vector are "parallel".
- Fixed #209.
- Topography is now serialized to JSON.
- Built in materials now have an assigned Id.

## 0.3.8

### Changed

- Elements representing building components now return positive areas.
- Added Area property to:
  Panel
  Space
  Added Volume property to:
- Floor
- Space
  Added positive area calculation to:
- Floor
- Mass
- Added positive Volume calculation to:
- StructuralFraming
- Beam.Volume() throws an InvalidOperationException for non-linear beams.
- Added TODO to support Volume() for all beam curves.

## 0.3.6

### Changed

- Edges are no longer written to the glTF file.
- Large performance improvements made to glTF writing using `Buffer.BlockCopy` and writing buffers directly from tesselation to glTF buffer.

### Fixed

- Fix #177.

## 0.3.4

### Changed

- Numerous comments were updated for clarity.
- Numerous styling changes were made to the documentation to align with the Hypar brand.

### Fixed

- Fixed an error where vertex colors were not correctly encoded in the glTF.

## 0.3.3

### Fixed

- Fix #173.
- Fix #7.

## 0.3.0

### Changed

- `Element.Id` is now a `Guid`.

### Fixed

- Fix #107.
- Fix #132.
- Fix #137.
- Fix #144.
- Fix #142.

## 0.2.17

### Added

- The `Kernel` singleton has been added to contain all geometry methods for creating solids.

### Fixed

- Fixed an error where, when writing edges to gltf, ushort would be overflowed and wrap back to 0 causing a loop not to terminate.

## 0.2.16

### Added

- Materials are now serialized to IFC using `IfcStyledItem`.

### Fixed

- Fixed an error where we returned directly after processing child Elements of an `IAggregateElements`, before we could process the parent element.
- Fixed writing of gltf files so that the `.bin` file is located adjacent to the `.gltf`.

## 0.2.15

### Added

- `Space` elements are now serialized to IFC as `IfcSpace`.

## 0.2.5

### Changed

- `IHasOpenings.Openings[]` is now `IHasOpenings.List<Opening>[]`.

### Fixed

- `Opening` elements are now serialized to IFC as `IfcOpeningElement`.

## 0.2.4.4

### Changed

- `Solid.Slice()` has been made internal. It's not yet ready for consumers. See [#103](https://github.com/hypar-io/elements/issues/103)

## 0.2.4.3

### Fixed

- Spaces are now correctly colored. See [#134](https://github.com/hypar-io/elements/issues/134).

## 0.2.4.2

### Added

- Added `ToIfcWall()` extension method to save a `Wall` to an `IfcWall`.

### Fixed

- `Space.Profile` is set in the constructor when a `Space` is constructed with a profile. [#132](https://github.com/hypar-io/elements/pull/132)
- Sub-elements of `IAggregateElements` are now added to the `Model`. [#137](https://github.com/hypar-io/elements/pull/137)

## 0.2.4.1

### Added

- Added `StandardWall`, for walls defined along a curve. `Wall` continues to be for walls defined by a planar profile extruded to a height.
- Added `Polygon.L`.

### Changed

- `Floor` constructors no longer have `material` parameter. Materials are now specified through the `FloorType`.
- `IAggregateElement` is now `IAggregateElements`.
- `Panel` now takes `Polygon` instead of `Vector3[]`.

## 0.2.4

### Changed

- `IGeometry3D` is now `ISolid`.
- `ISolid` (formerly `IGeometry3D`) now contains one solid, not an array of solids.

### Removed

- `Solid.Material`. Elements are now expected to implement the `IMaterial` interface or have an `IElementType<T>` which specifies a material.

## 0.2.3

### Added

- `MaterialLayer`
- `StructuralFramingType` - `StructuralFramingType` combines a `Profile` and a `Material` to define a type for framing elements.

### Changed

- `IProfileProvider` is now `IProfile`
- `IElementTypeProvider` is now `IElementType`
- All structural framing type constructors now take a `StructuralFramingType` in place of a `Profile` and a `Material`.
- All properties serialize to JSON using camel case.
- Many expensive properties were converted to methods.
- A constructor has been added to `WallType` that takes a collection of `MaterialLayer`.

## 0.2.2

### Added

- `Matrix.Determinant()`
- `Matrix.Inverse()`
- `Transform.Invert()`
- `Model.ToIFC()`
- `Elements.Serialization.JSON` namespace.
- `Elements.Serialization.IFC` namespace.
- `Elements.Serialization.glTF` namespace.

### Changed

- Wall constructor which uses a center line can now have a Transform specified.
- `Profile.ComputeTransform()` now finds the first 3 non-collinear points for calculating its plane. Previously, this function would break when two of the first three vertices were co-linear.
- Using Hypar.IFC2X3 for interaction with IFC.
- `Line.Thicken()` now throws an exception when the line does not have the same elevation for both end points.
- `Model.SaveGlb()` is now `Model.ToGlTF()`.

## 0.2.1

### Added

- The `Topography` class has been added.
- `Transform.OfPoint(Vector3 vector)` has been added to transform a vector as a point with translation. This was previously `Transform.OfVector(Vector3 vector)`. All sites previously using `OfVector(...)` are now using `OfPoint(...)`.
- `Material.DoubleSided`
- `Loop.InsertEdgeAfter()`
- `Solid.Slice()`
- `Model.Extensions`

### Changed

- `Transform.OfVector(Vector3 vector)` now does proper vector transformation without translation.
- Attempting to construct a `Vector3` with NaN or Infinite arguments will throw an `ArgumentOutOfRangeException`.

## 0.2.0

### Added

- IFC implementation has begun with `Model.FromIFC(...)`. Support includes reading of Walls, Slabs, Spaces, Beams, and Columns. Brep booleans required for Wall and Slab openings are not yet supported and are instead converted to Polygon openings in Wall and Floor profiles.
- The `Elements.Geometry.Profiles` namespace has been added. All profile servers can now be found here.
- The `Elements.Geometry.Solids` namespace has been added.
- The Frame type has been added to represent a continuous extrusion of a profile around a polygonal perimeter.
- The `ModelTest` base class has been added. Inheriting from this test class enables a test to automatically write its `Model` to a `.glb` file and to serialize and deserialize to/from JSON to ensure the stability of serialization.
- The `Solid.ToGlb` extension method has been added to enable serializing one `Solid` to glTF for testing.

### Changed

- Element identifiers are now of type `long`.
- Breps have been re-implemented in the `Solid` class. Currently only planar trimmed faces are supported.
- Many improvements to JSON serialization have been added, including the ability to serialize breps.
- '{element}.AddParameter' has been renamed to '{element}.AddProperty'.
- The `Hypar.Geometry` namespace is now `Elements.Geometry`.
- The `Hypar.Elements` namespace is now `Elements`.

### Removed

- The `IProfile` interface has been removed.
- The `Extrusion` class and `IBrep` have been replaced with the `Solid` class. The IGeometry interface now returns a `Solid[]`.
- Many uses of `System.Linq` have been removed.
- Many uses of `IEnumerable<T>` have been replaced with `T[]`.
