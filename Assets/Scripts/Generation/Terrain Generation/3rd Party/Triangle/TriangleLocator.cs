// -----------------------------------------------------------------------
// <copyright file="TriangleLocator.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using TriangleNet.Geometry;
    using TriangleNet.Topology;

    /// <summary>
    /// Locate triangles in a mesh.
    /// </summary>
    /// <remarks>
    /// WARNING: This routine is designed for convex triangulations, and will
    /// not generally work after the holes and concavities have been carved.
    /// 
    /// Based on a paper by Ernst P. Mucke, Isaac Saias, and Binhai Zhu, "Fast
    /// Randomized Point Location Without Preprocessing in Two- and Three-Dimensional
    /// Delaunay Triangulations," Proceedings of the Twelfth Annual Symposium on
    /// Computational Geometry, ACM, May 1996.
    /// </remarks>
    public class TriangleLocator
    {
        TriangleSampler sampler;
        Mesh mesh;

        IPredicates predicates;

        // Pointer to a recently visited triangle. Improves point location if
        // proximate vertices are inserted sequentially.
        internal Otri recenttri;

        public TriangleLocator(Mesh mesh)
            : this(mesh, RobustPredicates.Default)
        {
        }

        public TriangleLocator(Mesh mesh, IPredicates predicates)
        {
            this.mesh = mesh;
            this.predicates = predicates;

            sampler = new TriangleSampler(mesh);
        }

        /// <summary>
        /// Suggest the given triangle as a starting triangle for point location.
        /// </summary>
        /// <param name="otri"></param>
        public void Update(ref Otri otri)
        {
            otri.Copy(ref recenttri);
        }

        public void Reset()
        {
            sampler.Reset();
            recenttri.tri = null; // No triangle has been visited yet.
        }

        /// <summary>
        /// Find a triangle or edge containing a given point.
        /// </summary>
        /// <param name="searchpoint">The point to locate.</param>
        /// <param name="searchtri">The triangle to start the search at.</param>
        /// <param name="stopatsubsegment"> If 'stopatsubsegment' is set, the search 
        /// will stop if it tries to walk through a subsegment, and will return OUTSIDE.</param>
        /// <returns>Location information.</returns>
        /// <remarks>
        /// Begins its search from 'searchtri'. It is important that 'searchtri'
        /// be a handle with the property that 'searchpoint' is strictly to the left
        /// of the edge denoted by 'searchtri', or is collinear with that edge and
        /// does not intersect that edge. (In particular, 'searchpoint' should not
        /// be the origin or destination of that edge.)
        ///
        /// These conditions are imposed because preciselocate() is normally used in
        /// one of two situations:
        ///
        /// (1)  To try to find the location to insert a new point.  Normally, we
        ///      know an edge that the point is strictly to the left of. In the
        ///      incremental Delaunay algorithm, that edge is a bounding box edge.
        ///      In Ruppert's Delaunay refinement algorithm for quality meshing,
        ///      that edge is the shortest edge of the triangle whose circumcenter
        ///      is being inserted.
        ///
        /// (2)  To try to find an existing point.  In this case, any edge on the
        ///      convex hull is a good starting edge. You must screen out the
        ///      possibility that the vertex sought is an endpoint of the starting
        ///      edge before you call preciselocate().
        ///
        /// On completion, 'searchtri' is a triangle that contains 'searchpoint'.
        ///
        /// This implementation differs from that given by Guibas and Stolfi.  It
        /// walks from triangle to triangle, crossing an edge only if 'searchpoint'
        /// is on the other side of the line containing that edge. After entering
        /// a triangle, there are two edges by which one can leave that triangle.
        /// If both edges are valid ('searchpoint' is on the other side of both
        /// edges), one of the two is chosen by drawing a line perpendicular to
        /// the entry edge (whose endpoints are 'forg' and 'fdest') passing through
        /// 'fapex'. Depending on which side of this perpendicular 'searchpoint'
        /// falls on, an exit edge is chosen.
        ///
        /// This implementation is empirically faster than the Guibas and Stolfi
        /// point location routine (which I originally used), which tends to spiral
        /// in toward its target.
        ///
        /// Returns ONVERTEX if the point lies on an existing vertex. 'searchtri'
        /// is a handle whose origin is the existing vertex.
        ///
        /// Returns ONEDGE if the point lies on a mesh edge. 'searchtri' is a
        /// handle whose primary edge is the edge on which the point lies.
        ///
        /// Returns INTRIANGLE if the point lies strictly within a triangle.
        /// 'searchtri' is a handle on the triangle that contains the point.
        ///
        /// Returns OUTSIDE if the point lies outside the mesh. 'searchtri' is a
        /// handle whose primary edge the point is to the right of.  This might
        /// occur when the circumcenter of a triangle falls just slightly outside
        /// the mesh due to floating-point roundoff error. It also occurs when
        /// seeking a hole or region point that a foolish user has placed outside
        /// the mesh.
        ///
        /// WARNING:  This routine is designed for convex triangulations, and will
        /// not generally work after the holes and concavities have been carved.
        /// However, it can still be used to find the circumcenter of a triangle, as
        /// long as the search is begun from the triangle in question.</remarks>
        public LocateResult PreciseLocate(Point searchpoint, ref Otri searchtri,
            bool stopatsubsegment)
        {
            Otri backtracktri = default(Otri);
            Osub checkedge = default(Osub);
            Vertex forg, fdest, fapex;
            double orgorient, destorient;
            bool moveleft;

            // Where are we?
            forg = searchtri.Org();
            fdest = searchtri.Dest();
            fapex = searchtri.Apex();
            while (true)
            {
                // Check whether the apex is the point we seek.
                if ((fapex.x == searchpoint.x) && (fapex.y == searchpoint.y))
                {
                    searchtri.Lprev();
                    return LocateResult.OnVertex;
                }
                // Does the point lie on the other side of the line defined by the
                // triangle edge opposite the triangle's destination?
                destorient = predicates.CounterClockwise(forg, fapex, searchpoint);
                // Does the point lie on the other side of the line defined by the
                // triangle edge opposite the triangle's origin?
                orgorient = predicates.CounterClockwise(fapex, fdest, searchpoint);
                if (destorient > 0.0)
                {
                    if (orgorient > 0.0)
                    {
                        // Move left if the inner product of (fapex - searchpoint) and
                        // (fdest - forg) is positive.  This is equivalent to drawing
                        // a line perpendicular to the line (forg, fdest) and passing
                        // through 'fapex', and determining which side of this line
                        // 'searchpoint' falls on.
                        moveleft = (fapex.x - searchpoint.x) * (fdest.x - forg.x) +
                                   (fapex.y - searchpoint.y) * (fdest.y - forg.y) > 0.0;
                    }
                    else
                    {
                        moveleft = true;
                    }
                }
                else
                {
                    if (orgorient > 0.0)
                    {
                        moveleft = false;
                    }
                    else
                    {
                        // The point we seek must be on the boundary of or inside this
                        // triangle.
                        if (destorient == 0.0)
                        {
                            searchtri.Lprev();
                            return LocateResult.OnEdge;
                        }
                        if (orgorient == 0.0)
                        {
                            searchtri.Lnext();
                            return LocateResult.OnEdge;
                        }
                        return LocateResult.InTriangle;
                    }
                }

                // Move to another triangle. Leave a trace 'backtracktri' in case
                // floating-point roundoff or some such bogey causes us to walk
                // off a boundary of the triangulation.
                if (moveleft)
                {
                    searchtri.Lprev(ref backtracktri);
                    fdest = fapex;
                }
                else
                {
                    searchtri.Lnext(ref backtracktri);
                    forg = fapex;
                }
                backtracktri.Sym(ref searchtri);

                if (mesh.checksegments && stopatsubsegment)
                {
                    // Check for walking through a subsegment.
                    backtracktri.Pivot(ref checkedge);
                    if (checkedge.seg.hash != Mesh.DUMMY)
                    {
                        // Go back to the last triangle.
                        backtracktri.Copy(ref searchtri);
                        return LocateResult.Outside;
                    }
                }
                // Check for walking right out of the triangulation.
                if (searchtri.tri.id == Mesh.DUMMY)
                {
                    // Go back to the last triangle.
                    backtracktri.Copy(ref searchtri);
                    return LocateResult.Outside;
                }

                fapex = searchtri.Apex();
            }
        }

        /// <summary>
        /// Find a triangle or edge containing a given point.
        /// </summary>
        /// <param name="searchpoint">The point to locate.</param>
        /// <param name="searchtri">The triangle to start the search at.</param>
        /// <returns>Location information.</returns>
        /// <remarks>
        /// Searching begins from one of:  the input 'searchtri', a recently
        /// encountered triangle 'recenttri', or from a triangle chosen from a
        /// random sample. The choice is made by determining which triangle's
        /// origin is closest to the point we are searching for. Normally,
        /// 'searchtri' should be a handle on the convex hull of the triangulation.
        ///
        /// Details on the random sampling method can be found in the Mucke, Saias,
        /// and Zhu paper cited in the header of this code.
        ///
        /// On completion, 'searchtri' is a triangle that contains 'searchpoint'.
        ///
        /// Returns ONVERTEX if the point lies on an existing vertex. 'searchtri'
        /// is a handle whose origin is the existing vertex.
        ///
        /// Returns ONEDGE if the point lies on a mesh edge. 'searchtri' is a
        /// handle whose primary edge is the edge on which the point lies.
        ///
        /// Returns INTRIANGLE if the point lies strictly within a triangle.
        /// 'searchtri' is a handle on the triangle that contains the point.
        ///
        /// Returns OUTSIDE if the point lies outside the mesh. 'searchtri' is a
        /// handle whose primary edge the point is to the right of.  This might
        /// occur when the circumcenter of a triangle falls just slightly outside
        /// the mesh due to floating-point roundoff error. It also occurs when
        /// seeking a hole or region point that a foolish user has placed outside
        /// the mesh.
        ///
        /// WARNING:  This routine is designed for convex triangulations, and will
        /// not generally work after the holes and concavities have been carved.
        /// </remarks>
        public LocateResult Locate(Point searchpoint, ref Otri searchtri)
        {
            Otri sampletri = default(Otri);
            Vertex torg, tdest;
            double searchdist, dist;
            double ahead;

            // Record the distance from the suggested starting triangle to the
            // point we seek.
            torg = searchtri.Org();
            searchdist = (searchpoint.x - torg.x) * (searchpoint.x - torg.x) +
                         (searchpoint.y - torg.y) * (searchpoint.y - torg.y);

            // If a recently encountered triangle has been recorded and has not been
            // deallocated, test it as a good starting point.
            if (recenttri.tri != null)
            {
                if (!Otri.IsDead(recenttri.tri))
                {
                    torg = recenttri.Org();
                    if ((torg.x == searchpoint.x) && (torg.y == searchpoint.y))
                    {
                        recenttri.Copy(ref searchtri);
                        return LocateResult.OnVertex;
                    }
                    dist = (searchpoint.x - torg.x) * (searchpoint.x - torg.x) +
                           (searchpoint.y - torg.y) * (searchpoint.y - torg.y);
                    if (dist < searchdist)
                    {
                        recenttri.Copy(ref searchtri);
                        searchdist = dist;
                    }
                }
            }

            // TODO: Improve sampling.
            sampler.Update();

            foreach (var t in sampler)
            {
                sampletri.tri = t;
                if (!Otri.IsDead(sampletri.tri))
                {
                    torg = sampletri.Org();
                    dist = (searchpoint.x - torg.x) * (searchpoint.x - torg.x) +
                           (searchpoint.y - torg.y) * (searchpoint.y - torg.y);
                    if (dist < searchdist)
                    {
                        sampletri.Copy(ref searchtri);
                        searchdist = dist;
                    }
                }
            }

            // Where are we?
            torg = searchtri.Org();
            tdest = searchtri.Dest();

            // Check the starting triangle's vertices.
            if ((torg.x == searchpoint.x) && (torg.y == searchpoint.y))
            {
                return LocateResult.OnVertex;
            }
            if ((tdest.x == searchpoint.x) && (tdest.y == searchpoint.y))
            {
                searchtri.Lnext();
                return LocateResult.OnVertex;
            }

            // Orient 'searchtri' to fit the preconditions of calling preciselocate().
            ahead = predicates.CounterClockwise(torg, tdest, searchpoint);
            if (ahead < 0.0)
            {
                // Turn around so that 'searchpoint' is to the left of the
                // edge specified by 'searchtri'.
                searchtri.Sym();
            }
            else if (ahead == 0.0)
            {
                // Check if 'searchpoint' is between 'torg' and 'tdest'.
                if (((torg.x < searchpoint.x) == (searchpoint.x < tdest.x)) &&
                    ((torg.y < searchpoint.y) == (searchpoint.y < tdest.y)))
                {
                    return LocateResult.OnEdge;
                }
            }

            return PreciseLocate(searchpoint, ref searchtri, false);
        }
    }
}
