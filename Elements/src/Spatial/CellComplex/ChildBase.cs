using System.Collections.Generic;
using System.Linq;
using Elements.Geometry;
using Newtonsoft.Json;
using System;

namespace Elements.Spatial.CellComplex
{
    /// <summary>
    /// An abstract base for the children of FaceComplex.
    /// </summary>
    public abstract class ChildBase<ChildClass, GeometryType> : Interfaces.IDistanceTo where ChildClass : ChildBase<ChildClass, GeometryType>
    {
        /// <summary>
        /// ID of this child.
        /// </summary>
        public ulong Id { get; internal set; }

        /// <summary>
        /// The FaceComplex that this child belongs to.
        /// </summary>
        [JsonIgnore]
        public FaceComplex FaceComplex { get; internal set; }

        /// <summary>
        /// Used to return a unique identifier for when we make HashSets of children of this type.
        /// </summary>
        public override int GetHashCode()
        {
            return (int)this.Id;
        }

        /// <summary>
        /// Base constructor for a FaceComplex child.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="faceComplex"></param>
        internal ChildBase(ulong id, FaceComplex faceComplex = null)
        {
            this.Id = id;
            this.FaceComplex = faceComplex;
        }

        /// <summary>
        /// Get the shortest distance from a point to the geometry representing this child.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public abstract double DistanceTo(Vector3 point);

        /// <summary>
        /// Used to handle comparisons for when we make HashSets of children of this type.
        /// </summary>
        public override bool Equals(object obj)
        {
            ChildClass other = obj as ChildClass;
            if (other == null) return false;
            return this.Id == other.Id;
        }

        /// <summary>
        /// Get the closest candidate from a list of candidates.
        /// </summary>
        /// <param name="candidates">List of available candidates.</param>
        /// <param name="point">Our target point to determine closest distance from.</param>
        /// <typeparam name="T">Return object type.</typeparam>
        /// <returns></returns>
        internal static T GetClosest<T>(List<T> candidates, Vector3 point) where T : class, Interfaces.IDistanceTo
        {
            if (candidates.Count == 0)
            {
                return null;
            }
            return candidates.OrderBy(c => c.DistanceTo(point)).ToList()[0];
        }

        /// <summary>
        /// A utility to traverse the neighbors of a traversable child.
        /// </summary>
        /// <param name="startChild">Starting child.</param>
        /// <param name="maxCount">The number of traversals after which this will abort. This is a safety measure against infinite loops.</param>
        /// <param name="target">Target to traverse toward.</param>
        /// <param name="completedRadius">If provided, ends the traversal when the neighbor is within this distance to the target point.</param>
        /// <param name="getNextNeighbor">Provide the method by which we will grab the next neighbor in the traversal series.</param>
        /// <returns>A collection of traversed children, including the starting child.</returns>
        internal static List<ChildClass> TraverseNeighbors(ChildClass startChild, int maxCount, Vector3 target, double completedRadius, Func<ChildClass, ChildClass> getNextNeighbor)
        {
            var count = 0;
            var neighbors = new List<ChildClass>();
            var curNeighbor = startChild;
            while (curNeighbor != null && count <= maxCount)
            {
                neighbors.Add(curNeighbor);
                if (curNeighbor.DistanceTo(target) <= completedRadius)
                {
                    break;
                }
                curNeighbor = getNextNeighbor(curNeighbor);
                count += 1;
            }
            return neighbors;
        }

        /// <summary>
        /// Get the associated geometry for this child.
        /// </summary>
        /// <returns></returns>
        public abstract GeometryType GetGeometry();
    }
}