using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;

namespace Elements.Geometry
{
    /// <summary>
    /// A polygonal perimeter with zero or more polygonal voids.
    /// </summary>
    public partial class Profile : Element, IEquatable<Profile>
    {
        /// <summary>
        /// Construct a profile.
        /// </summary>
        /// <param name="perimeter">The perimeter of the profile.</param>
        public Profile(Polygon perimeter) : base(Guid.NewGuid(), null)
        {
            this.Perimeter = perimeter;
            this.Voids = new List<Polygon>();
        }

        /// <summary>
        /// Construct a profile from a collection of polygons.
        /// If the collection contains more than one polygon, the first polygon
        /// will be used as the perimeter and any remaining polygons will 
        /// be used as voids.
        /// </summary>
        /// <param name="polygons">The polygons bounding this profile.</param>
        public Profile(IList<Polygon> polygons) : base(Guid.NewGuid(), null)
        {
            var polyCount = polygons.Count();
            if (polyCount == 0)
            {
                return;
            }
            if (polyCount == 1)
            {
                this.Perimeter = polygons.First();
                return;
            }
            var polyIsContainedByOther = new bool[polyCount];
            for (int i = 0; i < polyCount; i++)
            {
                var polyToTest = polygons[i];
                polyIsContainedByOther[i] = false;
                for (int j = 0; j < polyCount; j++)
                {
                    if (i == j) continue;
                    var otherPoly = polygons[j];
                    if (otherPoly.Contains(polyToTest))
                    {
                        polyIsContainedByOther[i] = true;
                        break;
                    }
                }
            }

            var indices = Enumerable.Range(0, polyCount);
            var outerMostIndices = indices.Where(i => !polyIsContainedByOther[i]);
            if (outerMostIndices.Count() > 1)
            {
                throw new ArgumentException("Unable to construct a profile. More than one of the polygons supplied are not contained by any other.");
            }
            if (outerMostIndices.Count() == 0)
            {
                throw new ArgumentException("Unable to construct a profile. All the supplied polygons are inside other supplied polygons. Sounds like a geometric paradox!");
            }

            var perimeter = polygons[outerMostIndices.First()];
            var voids = indices.Except(outerMostIndices).Select(i => polygons[i]);
            this.Perimeter = perimeter;
            this.Voids = voids.ToList();
            OrientVoids();
        }

        /// <summary>
        /// A transformed copy of this profile.
        /// </summary>
        /// <param name="transform">The transform.</param>
        public Profile Transformed(Transform transform)
        {
            return new Profile(this.Perimeter.TransformedPolygon(transform), this.Voids?.Select(v => v.TransformedPolygon(transform)).ToList() ?? new List<Polygon>());
        }

        /// <summary>
        /// Construct a profile.
        /// </summary>
        /// <param name="perimeter">The perimeter of the profile.</param>
        /// <param name="void">A void in the profile.</param>
        public Profile(Polygon perimeter,
                       Polygon @void) :
            this(perimeter, new[] { @void }, Guid.NewGuid(), null)
        { }

        /// <summary>
        /// Get a new profile which is the reverse of this profile.
        /// </summary>
        public Profile Reversed()
        {
            Polygon[] voids = null;
            if (this.Voids != null)
            {
                voids = new Polygon[this.Voids.Count];
                for (var i = 0; i < this.Voids.Count; i++)
                {
                    voids[i] = this.Voids[i].Reversed();
                }
            }
            return new Profile(this.Perimeter.Reversed(), voids, Guid.NewGuid(), null);
        }

        /// <summary>
        /// The area of the profile.
        /// </summary>
        public double Area()
        {
            return ClippedArea();
        }

        /// <summary>
        /// Transform this profile in place.
        /// </summary>
        /// <param name="t">The transform.</param>
        public void Transform(Transform t)
        {
            this.Perimeter.Transform(t);
            if (this.Voids == null)
            {
                return;
            }

            for (var i = 0; i < this.Voids.Count; i++)
            {
                this.Voids[i].Transform(t);
            }
        }

        /// <summary>
        /// Return a new profile that is this profile scaled about the origin by the desired amount.
        /// </summary>
        public Elements.Geometry.Profile Scale(double amount)
        {
            var transform = new Elements.Geometry.Transform();
            transform.Scale(amount);

            return transform.OfProfile(this);
        }

        /// <summary>
        /// Perform a union operation, returning a new profile that is the union of the current profile with the other profile
        /// <param name="profile">The profile with which to create a union.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// </summary>
        public Profile Union(Profile profile, double tolerance = Vector3.EPSILON)
        {
            var clipper = new ClipperLib.Clipper();
            clipper.AddPath(this.Perimeter.ToClipperPath(tolerance), PolyType.ptSubject, true);
            clipper.AddPath(profile.Perimeter.ToClipperPath(tolerance), PolyType.ptClip, true);

            if (this.Voids != null && this.Voids.Count > 0)
            {
                clipper.AddPaths(this.Voids.Select(v => v.ToClipperPath(tolerance)).ToList(), PolyType.ptSubject, true);
            }
            if (profile.Voids != null && profile.Voids.Count > 0)
            {
                clipper.AddPaths(profile.Voids.Select(v => v.ToClipperPath(tolerance)).ToList(), PolyType.ptClip, true);
            }
            var solution = new List<List<ClipperLib.IntPoint>>();
            clipper.Execute(ClipType.ctUnion, solution);
            return new Profile(solution.Select(s => s.ToPolygon(tolerance)).ToList());
        }

        /// <summary>
        /// Default constructor for profile.
        /// </summary>
        protected Profile(string name) : base(Guid.NewGuid(), name) { }

        /// <summary>
        ///  Conduct a clip operation on this profile.
        /// </summary>
        internal void Clip(IEnumerable<Profile> additionalHoles = null, double tolerance = Vector3.EPSILON)
        {
            var clipper = new ClipperLib.Clipper();
            clipper.AddPath(this.Perimeter.ToClipperPath(tolerance), ClipperLib.PolyType.ptSubject, true);
            if (this.Voids != null)
            {
                clipper.AddPaths(this.Voids.Select(p => p.ToClipperPath(tolerance)).ToList(), ClipperLib.PolyType.ptClip, true);
            }
            if (additionalHoles != null)
            {
                clipper.AddPaths(additionalHoles.Select(h => h.Perimeter.ToClipperPath(tolerance)).ToList(), ClipperLib.PolyType.ptClip, true);
            }
            var solution = new List<List<ClipperLib.IntPoint>>();
            var result = clipper.Execute(ClipperLib.ClipType.ctDifference, solution, ClipperLib.PolyFillType.pftEvenOdd);

            // Completely disjoint polygons like a circular pipe
            // profile will result in an empty solution.
            if (solution.Count > 0)
            {
                var polys = solution.Select(s => s.ToPolygon(tolerance)).ToArray();
                this.Perimeter = polys[0];
                this.Voids = polys.Skip(1).ToArray();
            }
        }

        /// <summary>
        /// Ensure that voids run in an opposite winding direction to the perimeter of the profile.
        /// Be sure to call this if you modify the Profile's Voids array directly.
        /// </summary>
        public void OrientVoids()
        {
            var correctedVoids = new List<Polygon>();
            var perimeterNormal = Perimeter.Normal();
            foreach (var voidCrv in Voids)
            {
                if (voidCrv.Normal().Dot(perimeterNormal) > 0)
                {
                    correctedVoids.Add(voidCrv.Reversed());
                }
                else
                {
                    correctedVoids.Add(voidCrv);
                }
            }
            this.Voids = correctedVoids;
        }

        private double ClippedArea()
        {
            if (this.Voids == null || this.Voids.Count == 0)
            {
                return this.Perimeter.Area();
            }

            var clipper = new ClipperLib.Clipper();
            clipper.AddPath(this.Perimeter.ToClipperPath(), ClipperLib.PolyType.ptSubject, true);
            clipper.AddPaths(this.Voids.Select(p => p.ToClipperPath()).ToList(), ClipperLib.PolyType.ptClip, true);
            var solution = new List<List<ClipperLib.IntPoint>>();
            clipper.Execute(ClipperLib.ClipType.ctDifference, solution, ClipperLib.PolyFillType.pftEvenOdd);
            return solution.Sum(s => ClipperLib.Clipper.Area(s)) / Math.Pow(1.0 / Vector3.EPSILON, 2);
        }

        private Transform ComputeTransform()
        {
            var v = this.Perimeter.Vertices.ToList();
            var x = (v[0] - v[1]).Unitized();
            var i = 2;
            var b = (v[i] - v[1]).Unitized();

            // Solve for parallel vectors
            while (b.IsAlmostEqualTo(x) || b.IsAlmostEqualTo(x.Negate()))
            {
                i++;
                b = (v[i] - v[1]).Unitized();
            }
            var z = x.Cross(b);
            return new Transform(v[0], x, z);
        }

        private bool IsPlanar()
        {
            var t = ComputeTransform();
            var vertices = this.Perimeter.Vertices;
            var p = t.XY();
            foreach (var v in vertices)
            {
                var d = v.DistanceTo(p);
                if (Math.Abs(d) > Vector3.EPSILON)
                {
                    Console.WriteLine($"Out of plane distance: {d}.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Is this profile equal to the provided profile?
        /// </summary>
        /// <param name="other">The other profile.</param>
        public bool Equals(Profile other)
        {
            if ((this.Voids != null && other.Voids != null))
            {
                if (this.Voids.Count != other.Voids.Count)
                {
                    return false;
                }
                for (var i = 0; i < this.Voids.Count; i++)
                {
                    if (!this.Voids[i].Equals(other.Voids[i]))
                    {
                        return false;
                    }
                }
            }
            return this.Perimeter.Equals(other.Perimeter);
        }

        /// <summary>
        /// Tests if a point is contained within this profile. Returns false for points that are outside of the profile, within voids, or coincident at edges or vertices.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(Vector3 point)
        {
            Contains(point, out Containment containment);
            return containment == Containment.Inside;
        }

        /// <summary>
        /// Tests if a point is contained within this profile. Returns false for points that are outside of the profile (or within voids). 
        /// </summary>
        /// <param name="point">The position to test.</param>
        /// <param name="containment">Whether the point is inside, outside, at an edge, or at a vertex.</param>
        /// <returns>True if the point is within the profile.</returns>
        public bool Contains(Vector3 point, out Containment containment)
        {
            IEnumerable<Line> allLines = Perimeter.Segments();
            if (Voids != null)
            {
                allLines = allLines.Union(Voids.SelectMany(v => v.Segments()));
            }
            return Polygon.Contains(allLines, point, out containment);
        }

        /// <summary>
        /// Perform a union operation on a set of multiple profiles.
        /// </summary>
        /// <param name="profiles">The profiles with which to create a union.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// <returns>A new list of profiles comprising the union of all input profiles.</returns>
        public static List<Profile> UnionAll(IEnumerable<Profile> profiles, double tolerance = Vector3.EPSILON)
        {
            Clipper clipper = new Clipper();
            foreach (var profile in profiles)
            {
                var subjectPolygons = new List<Polygon> { profile.Perimeter };
                if (profile.Voids != null && profile.Voids.Count > 0)
                {
                    subjectPolygons.AddRange(profile.Voids);
                }
                var clipperPaths = subjectPolygons.Select(s => s.ToClipperPath(tolerance)).ToList();
                clipper.AddPaths(clipperPaths, PolyType.ptSubject, true);
            }
            PolyTree solution = new PolyTree();
            clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftPositive);
            var joinedProfiles = solution.ToProfiles(tolerance);
            return joinedProfiles;
        }

        /// <summary>
        /// Perform a difference operation on two sets of profiles.
        /// </summary>
        /// <param name="firstSet">The profiles to subtract from.</param>
        /// <param name="secondSet">The profiles to subtract with.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// <returns>A new list of profiles comprising the first set minus the second set.</returns>
        public static List<Profile> Difference(IEnumerable<Profile> firstSet, IEnumerable<Profile> secondSet, double tolerance = Vector3.EPSILON)
        {
            Clipper clipper = new Clipper();
            foreach (var profile in firstSet)
            {
                var clipperPaths = profile.ToClipperPaths(tolerance);
                clipper.AddPaths(clipperPaths, PolyType.ptSubject, true);
            }

            foreach (var profile in secondSet)
            {
                var clipperPaths = profile.ToClipperPaths(tolerance);
                clipper.AddPaths(clipperPaths, PolyType.ptClip, true);
            }
            PolyTree solution = new PolyTree();
            clipper.Execute(ClipType.ctDifference, solution, PolyFillType.pftNonZero);
            var joinedProfiles = solution.ToProfiles(tolerance);
            return joinedProfiles;
        }

        /// <summary>
        /// Constructs the intersections between two sets of profiles.
        /// </summary>
        /// <param name="firstSet">The first set of profiles to intersect with.</param>
        /// <param name="secondSet">The second set of profiles to intersect with.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// <returns>A new list of profiles comprising the overlap between the first set and the second set.</returns>
        public static List<Profile> Intersection(IEnumerable<Profile> firstSet, IEnumerable<Profile> secondSet, double tolerance = Vector3.EPSILON)
        {
            Clipper clipper = new Clipper();
            foreach (var profile in firstSet)
            {
                var clipperPaths = profile.ToClipperPaths(tolerance);
                clipper.AddPaths(clipperPaths, PolyType.ptSubject, true);
            }

            foreach (var profile in secondSet)
            {
                var clipperPaths = profile.ToClipperPaths(tolerance);
                clipper.AddPaths(clipperPaths, PolyType.ptClip, true);
            }
            PolyTree solution = new PolyTree();
            clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftNonZero);
            var joinedProfiles = solution.ToProfiles(tolerance);
            return joinedProfiles;
        }

        /// <summary>
        /// Split a set of profiles with a collection of open polylines.
        /// </summary>
        /// <param name="profiles">The profiles to split</param>
        /// <param name="splitLines">The polylines defining the splits.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        public static List<Profile> Split(IEnumerable<Profile> profiles, IEnumerable<Polyline> splitLines, double tolerance = Vector3.EPSILON)
        {
            List<Profile> resultProfiles = new List<Profile>();
            foreach (var inputProfile in profiles)
            {
                inputProfile.OrientVoids();
                var polygons = new List<Polygon>() {
                    inputProfile.Perimeter
                };
                if (inputProfile.Voids != null)
                {
                    polygons.AddRange(inputProfile.Voids);
                }
                // construct a half-edge graph from all polygons and splitter polylines.
                var graph = Elements.Spatial.HalfEdgeGraph2d.Construct(polygons, splitLines);
                // make sure all polygons are consistently wound - we reverse them if we need to treat them as voids.
                var perimSplits = graph.Polygonize().Select(p => p.IsClockWise() ? p.Reversed() : p).ToList();
                // for every resultant polygon, we can't be sure if it's a void, or should have a void,
                // so we check if it includes any of the others, and subtract the original profile's voids
                // as well. 
                for (int i = 0; i < perimSplits.Count; i++)
                {
                    Polygon perimeterPoly = perimSplits[i];
                    var voidProfiles = inputProfile.Voids.Select(v => new Profile(v.Reversed())).ToList();
                    for (int j = 0; j < perimSplits.Count; j++)
                    {
                        //don't compare a polygon with itself
                        if (i == j)
                        {
                            continue;
                        }
                        if (perimeterPoly.Covers(perimSplits[j]))
                        {
                            voidProfiles.Add(new Profile(perimSplits[j].Reversed()));
                        }
                    }
                    var perimeterProfiles = new[] { new Profile(perimeterPoly) };
                    resultProfiles.AddRange(Profile.Difference(perimeterProfiles, voidProfiles, tolerance));
                }
            };
            return resultProfiles;
        }


        /// <summary>
        /// Split a set of profiles with a collection of open polylines.
        /// </summary>
        /// <param name="profiles">The profiles to split</param>
        /// <param name="splitLine">The polyline defining the splits.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        public static List<Profile> Split(IEnumerable<Profile> profiles, Polyline splitLine, double tolerance = Vector3.EPSILON)
        {
            return Profile.Split(profiles, new[] { splitLine }, tolerance);
        }

        /// <summary>
        /// Offset this profile by a given distance.
        /// </summary>
        /// <param name="distance">The offset distance.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// <returns></returns>
        public List<Profile> Offset(double distance, double tolerance = Vector3.EPSILON)
        {
            return Profile.Offset(new[] { this }, distance, tolerance);
        }

        /// <summary>
        /// Offset profiles by a given distance.
        /// </summary>
        /// <param name="profiles">The profiles to offset.</param>
        /// <param name="distance">The offset distance.</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// <returns>A collection of resulting profiles.</returns>
        public static List<Profile> Offset(IEnumerable<Profile> profiles, double distance, double tolerance = Vector3.EPSILON)
        {
            var clipperScale = 1.0 / tolerance;
            ClipperOffset clipper = new ClipperOffset();
            foreach (var profile in profiles)
            {
                var subjectPolygons = new List<Polygon> { profile.Perimeter };
                if (profile.Voids != null && profile.Voids.Count > 0)
                {
                    subjectPolygons.AddRange(profile.Voids);
                }
                var clipperPaths = subjectPolygons.Select(s => s.ToClipperPath(tolerance)).ToList();
                clipper.AddPaths(clipperPaths, JoinType.jtMiter, ClipperLib.EndType.etClosedPolygon);
            }
            PolyTree solution = new PolyTree();
            clipper.Execute(ref solution, distance * clipperScale);
            var joinedProfiles = solution.ToProfiles(tolerance);
            return joinedProfiles;
        }

        /// <summary>
        /// Create a collection of profiles from a collection of polygons. Inner polygons will be treated as voids in alternating fashion.
        /// </summary>
        /// <param name="polygons">The polygons to sort into profiles</param>
        /// <param name="tolerance">An optional tolerance.</param>
        /// <returns></returns>
        public static List<Profile> CreateFromPolygons(IEnumerable<Polygon> polygons, double tolerance = Vector3.EPSILON)
        {
            Clipper clipper = new Clipper();
            foreach (var polygon in polygons)
            {
                var clipperPath = polygon.ToClipperPath(tolerance);
                clipper.AddPath(clipperPath, PolyType.ptSubject, true);
            }

            PolyTree solution = new PolyTree();
            clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftEvenOdd);
            var joinedProfiles = solution.ToProfiles(tolerance);
            return joinedProfiles;
        }

        /// <summary>
        /// Get all segments from a profile's perimeter and internal voids.
        /// </summary>
        public List<Line> Segments()
        {
            return Perimeter.Segments().Union(Voids?.SelectMany(v => v.Segments()) ?? new Line[0]).ToList();
        }
    }

    /// <summary>
    /// Profile extension methods.
    /// </summary>
    public static class ProfileExtensions
    {
        internal static List<List<IntPoint>> ToClipperPaths(this Profile profile, double tolerance = Vector3.EPSILON)
        {
            var subjectPolygons = new List<Polygon> { profile.Perimeter.IsClockWise() ? profile.Perimeter.Reversed() : profile.Perimeter };
            if (profile.Voids != null && profile.Voids.Count > 0)
            {
                subjectPolygons.AddRange(profile.Voids);
            }
            var clipperPaths = subjectPolygons.Select(s => s.ToClipperPath(tolerance)).ToList();
            return clipperPaths;
        }

        internal static Profile ToProfile(this PolyNode node, double tolerance = Vector3.EPSILON)
        {
            var perimeter = PolygonExtensions.ToPolygon(node.Contour, tolerance);
            if (perimeter == null)
            {
                return null;
            }
            List<Polygon> voidCrvs = new List<Polygon>();
            if (node.ChildCount > 0)
            {
                foreach (var child in node.Childs)
                {
                    var voidCrv = PolygonExtensions.ToPolygon(child.Contour, tolerance);
                    if (voidCrv != null)
                    {
                        voidCrvs.Add(voidCrv);
                    }
                }
            }
            var profile = new Profile(perimeter, voidCrvs, Guid.NewGuid(), null);
            return profile;
        }

        internal static List<Profile> ToProfiles(this PolyNode node, double tolerance = Vector3.EPSILON)
        {
            var joinedProfiles = new List<Profile>();

            if (node.Contour != null && !node.IsHole) // the outermost PolyTree will have a null contour, and skip this.
            {
                var profile = node.ToProfile(tolerance);
                joinedProfiles.Add(profile);
            }
            foreach (var result in node.Childs)
            {
                var profiles = result.ToProfiles(tolerance);
                joinedProfiles.AddRange(profiles.Where(p => p != null));
            }
            return joinedProfiles;
        }

    }
}