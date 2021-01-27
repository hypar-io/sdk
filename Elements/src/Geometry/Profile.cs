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
        internal override int SortPriority => 3;

        /// <summary>
        /// Construct a profile.
        /// </summary>
        /// <param name="perimeter">The perimeter of the profile.</param>
        public Profile(Polygon perimeter) : base(Guid.NewGuid(), null)
        {
            this.Perimeter = perimeter;
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
            if (solution.ChildCount == 0)
            {
                return null;
            }
            var joinedProfiles = new List<Profile>();
            foreach (var result in solution.Childs)
            {
                var perimeter = PolygonExtensions.ToPolygon(result.Contour, tolerance);
                List<Polygon> voidCrvs = new List<Polygon>();
                if (result.ChildCount > 0)
                {
                    foreach (var child in result.Childs)
                    {
                        var voidCrv = PolygonExtensions.ToPolygon(child.Contour, tolerance);
                        voidCrvs.Add(voidCrv);
                    }

                }
                joinedProfiles.Add(new Profile(perimeter, voidCrvs, Guid.NewGuid(), null));
            }
            return joinedProfiles;
        }
    }
}