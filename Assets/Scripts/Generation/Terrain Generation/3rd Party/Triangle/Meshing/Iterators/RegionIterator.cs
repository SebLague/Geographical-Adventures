// -----------------------------------------------------------------------
// <copyright file="RegionIterator.cs" company="">
// Original Matlab code by John Burkardt, Florida State University
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing.Iterators
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Topology;

    /// <summary>
    /// Iterates the region a given triangle belongs to and applies an action
    /// to each connected trianlge in that region. 
    /// </summary>
    /// <remarks>
    /// The default action is to set the region id and area constraint.
    /// </remarks>
    public class RegionIterator
    {
        List<Triangle> region;

        public RegionIterator(Mesh mesh)
        {
            this.region = new List<Triangle>();
        }

        /// <summary>
        /// Set the region attribute of all trianlges connected to given triangle.
        /// </summary>
        /// <param name="triangle">The triangle seed.</param>
        /// <param name="boundary">If non-zero, process all triangles of the
        /// region that is enclosed by segments with given boundary label.</param>
        public void Process(Triangle triangle, int boundary = 0)
        {
            this.Process(triangle, (tri) =>
            {
                // Set the region id and area constraint.
                tri.label = triangle.label;
                tri.area = triangle.area;
            }, boundary);
        }

        /// <summary>
        /// Process all trianlges connected to given triangle and apply given action.
        /// </summary>
        /// <param name="triangle">The seeding triangle.</param>
        /// <param name="action">The action to apply to each triangle.</param>
        /// <param name="boundary">If non-zero, process all triangles of the
        /// region that is enclosed by segments with given boundary label.</param>
        public void Process(Triangle triangle, Action<Triangle> action, int boundary = 0)
        {
            // Make sure the triangle under consideration still exists.
            // It may have been eaten by the virus.
            if (triangle.id == Mesh.DUMMY || Otri.IsDead(triangle))
            {
                return;
            }

            // Add the seeding triangle to the region.
            region.Add(triangle);

            triangle.infected = true;

            if (boundary == 0)
            {
                // Stop at any subsegment.
                ProcessRegion(action, seg => seg.hash == Mesh.DUMMY);
            }
            else
            {
                // Stop at segments that have the given boundary label.
                ProcessRegion(action, seg => seg.boundary != boundary);
            }

            // Free up memory (virus pool should be empty anyway).
            region.Clear();
        }

        /// <summary>
        /// Apply given action to each triangle of selected region.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="protector"></param>
        void ProcessRegion(Action<Triangle> action, Func<SubSegment, bool> protector)
        {
            Otri testtri = default(Otri);
            Otri neighbor = default(Otri);
            Osub neighborsubseg = default(Osub);

            // Loop through all the infected triangles, spreading the attribute
            // and/or area constraint to their neighbors, then to their neighbors'
            // neighbors.
            for (int i = 0; i < region.Count; i++)
            {
                // WARNING: Don't use foreach, viri list gets modified.

                testtri.tri = region[i];

                // Apply function.
                action(testtri.tri);

                // Check each of the triangle's three neighbors.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    // Find the neighbor.
                    testtri.Sym(ref neighbor);
                    // Check for a subsegment between the triangle and its neighbor.
                    testtri.Pivot(ref neighborsubseg);
                    // Make sure the neighbor exists, is not already infected, and
                    // isn't protected by a subsegment.
                    if ((neighbor.tri.id != Mesh.DUMMY) && !neighbor.IsInfected()
                        && protector(neighborsubseg.seg))
                    {
                        // Infect the neighbor.
                        neighbor.Infect();
                        // Ensure that the neighbor's neighbors will be infected.
                        region.Add(neighbor.tri);
                    }
                }
            }

            // Uninfect all triangles.
            foreach (var virus in region)
            {
                virus.infected = false;
            }

            // Empty the virus pool.
            region.Clear();
        }
    }
}
