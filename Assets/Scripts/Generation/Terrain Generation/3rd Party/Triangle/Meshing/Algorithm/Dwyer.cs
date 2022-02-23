// -----------------------------------------------------------------------
// <copyright file="Dwyer.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing.Algorithm
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;
    using TriangleNet.Topology;

    /// <summary>
    /// Builds a delaunay triangulation using the divide-and-conquer algorithm.
    /// </summary>
    /// <remarks>
    /// The divide-and-conquer bounding box
    ///
    /// I originally implemented the divide-and-conquer and incremental Delaunay
    /// triangulations using the edge-based data structure presented by Guibas
    /// and Stolfi. Switching to a triangle-based data structure doubled the
    /// speed. However, I had to think of a few extra tricks to maintain the
    /// elegance of the original algorithms.
    ///
    /// The "bounding box" used by my variant of the divide-and-conquer
    /// algorithm uses one triangle for each edge of the convex hull of the
    /// triangulation. These bounding triangles all share a common apical
    /// vertex, which is represented by NULL and which represents nothing.
    /// The bounding triangles are linked in a circular fan about this NULL
    /// vertex, and the edges on the convex hull of the triangulation appear
    /// opposite the NULL vertex. You might find it easiest to imagine that
    /// the NULL vertex is a point in 3D space behind the center of the
    /// triangulation, and that the bounding triangles form a sort of cone.
    ///
    /// This bounding box makes it easy to represent degenerate cases. For
    /// instance, the triangulation of two vertices is a single edge. This edge
    /// is represented by two bounding box triangles, one on each "side" of the
    /// edge. These triangles are also linked together in a fan about the NULL
    /// vertex.
    ///
    /// The bounding box also makes it easy to traverse the convex hull, as the
    /// divide-and-conquer algorithm needs to do.
    /// </remarks>
    public class Dwyer : ITriangulator
    {
        // Random is not threadsafe, so don't make this static.
        Random rand = new Random(DateTime.Now.Millisecond);

        IPredicates predicates;

        public bool UseDwyer = true;

        Vertex[] sortarray;
        Mesh mesh;

        /// <summary>
        /// Form a Delaunay triangulation by the divide-and-conquer method.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Sorts the vertices, calls a recursive procedure to triangulate them, and
        /// removes the bounding box, setting boundary markers as appropriate.
        /// </remarks>
        public IMesh Triangulate(IList<Vertex> points, Configuration config)
        {
            this.predicates = config.Predicates();

            this.mesh = new Mesh(config);
            this.mesh.TransferNodes(points);

            Otri hullleft = default(Otri), hullright = default(Otri);
            int i, j, n = points.Count;

            // Allocate an array of pointers to vertices for sorting.
            this.sortarray = new Vertex[n];
            i = 0;
            foreach (var v in points)
            {
                sortarray[i++] = v;
            }

            // Sort the vertices.
            VertexSorter.Sort(sortarray);

            // Discard duplicate vertices, which can really mess up the algorithm.
            i = 0;
            for (j = 1; j < n; j++)
            {
                if ((sortarray[i].x == sortarray[j].x) && (sortarray[i].y == sortarray[j].y))
                {
                    if (Log.Verbose)
                    {
                        Log.Instance.Warning(
                            String.Format("A duplicate vertex appeared and was ignored (ID {0}).", sortarray[j].id),
                            "Dwyer.Triangulate()");
                    }
                    sortarray[j].type = VertexType.UndeadVertex;
                    mesh.undeads++;
                }
                else
                {
                    i++;
                    sortarray[i] = sortarray[j];
                }
            }
            i++;
            if (UseDwyer)
            {
                // Re-sort the array of vertices to accommodate alternating cuts.
                VertexSorter.Alternate(sortarray, i);
            }

            // Form the Delaunay triangulation.
            DivconqRecurse(0, i - 1, 0, ref hullleft, ref hullright);

            this.mesh.hullsize = RemoveGhosts(ref hullleft);

            return this.mesh;
        }

        /// <summary>
        /// Merge two adjacent Delaunay triangulations into a single Delaunay triangulation.
        /// </summary>
        /// <param name="farleft">Bounding triangles of the left triangulation.</param>
        /// <param name="innerleft">Bounding triangles of the left triangulation.</param>
        /// <param name="innerright">Bounding triangles of the right triangulation.</param>
        /// <param name="farright">Bounding triangles of the right triangulation.</param>
        /// <param name="axis"></param>
        /// <remarks>
        /// This is similar to the algorithm given by Guibas and Stolfi, but uses
        /// a triangle-based, rather than edge-based, data structure.
        ///
        /// The algorithm walks up the gap between the two triangulations, knitting
        /// them together.  As they are merged, some of their bounding triangles
        /// are converted into real triangles of the triangulation.  The procedure
        /// pulls each hull's bounding triangles apart, then knits them together
        /// like the teeth of two gears.  The Delaunay property determines, at each
        /// step, whether the next "tooth" is a bounding triangle of the left hull
        /// or the right.  When a bounding triangle becomes real, its apex is
        /// changed from NULL to a real vertex.
        ///
        /// Only two new triangles need to be allocated.  These become new bounding
        /// triangles at the top and bottom of the seam.  They are used to connect
        /// the remaining bounding triangles (those that have not been converted
        /// into real triangles) into a single fan.
        ///
        /// On entry, 'farleft' and 'innerleft' are bounding triangles of the left
        /// triangulation.  The origin of 'farleft' is the leftmost vertex, and
        /// the destination of 'innerleft' is the rightmost vertex of the
        /// triangulation.  Similarly, 'innerright' and 'farright' are bounding
        /// triangles of the right triangulation.  The origin of 'innerright' and
        /// destination of 'farright' are the leftmost and rightmost vertices.
        ///
        /// On completion, the origin of 'farleft' is the leftmost vertex of the
        /// merged triangulation, and the destination of 'farright' is the rightmost
        /// vertex.
        /// </remarks>
        void MergeHulls(ref Otri farleft, ref Otri innerleft, ref Otri innerright,
                        ref Otri farright, int axis)
        {
            Otri leftcand = default(Otri), rightcand = default(Otri);
            Otri nextedge = default(Otri);
            Otri sidecasing = default(Otri), topcasing = default(Otri), outercasing = default(Otri);
            Otri checkedge = default(Otri);
            Otri baseedge = default(Otri);
            Vertex innerleftdest;
            Vertex innerrightorg;
            Vertex innerleftapex, innerrightapex;
            Vertex farleftpt, farrightpt;
            Vertex farleftapex, farrightapex;
            Vertex lowerleft, lowerright;
            Vertex upperleft, upperright;
            Vertex nextapex;
            Vertex checkvertex;
            bool changemade;
            bool badedge;
            bool leftfinished, rightfinished;

            innerleftdest = innerleft.Dest();
            innerleftapex = innerleft.Apex();
            innerrightorg = innerright.Org();
            innerrightapex = innerright.Apex();
            // Special treatment for horizontal cuts.
            if (UseDwyer && (axis == 1))
            {
                farleftpt = farleft.Org();
                farleftapex = farleft.Apex();
                farrightpt = farright.Dest();
                farrightapex = farright.Apex();
                // The pointers to the extremal vertices are shifted to point to the
                // topmost and bottommost vertex of each hull, rather than the
                // leftmost and rightmost vertices.
                while (farleftapex.y < farleftpt.y)
                {
                    farleft.Lnext();
                    farleft.Sym();
                    farleftpt = farleftapex;
                    farleftapex = farleft.Apex();
                }
                innerleft.Sym(ref checkedge);
                checkvertex = checkedge.Apex();
                while (checkvertex.y > innerleftdest.y)
                {
                    checkedge.Lnext(ref innerleft);
                    innerleftapex = innerleftdest;
                    innerleftdest = checkvertex;
                    innerleft.Sym(ref checkedge);
                    checkvertex = checkedge.Apex();
                }
                while (innerrightapex.y < innerrightorg.y)
                {
                    innerright.Lnext();
                    innerright.Sym();
                    innerrightorg = innerrightapex;
                    innerrightapex = innerright.Apex();
                }
                farright.Sym(ref checkedge);
                checkvertex = checkedge.Apex();
                while (checkvertex.y > farrightpt.y)
                {
                    checkedge.Lnext(ref farright);
                    farrightapex = farrightpt;
                    farrightpt = checkvertex;
                    farright.Sym(ref checkedge);
                    checkvertex = checkedge.Apex();
                }
            }
            // Find a line tangent to and below both hulls.
            do
            {
                changemade = false;
                // Make innerleftdest the "bottommost" vertex of the left hull.
                if (predicates.CounterClockwise(innerleftdest, innerleftapex, innerrightorg) > 0.0)
                {
                    innerleft.Lprev();
                    innerleft.Sym();
                    innerleftdest = innerleftapex;
                    innerleftapex = innerleft.Apex();
                    changemade = true;
                }
                // Make innerrightorg the "bottommost" vertex of the right hull.
                if (predicates.CounterClockwise(innerrightapex, innerrightorg, innerleftdest) > 0.0)
                {
                    innerright.Lnext();
                    innerright.Sym();
                    innerrightorg = innerrightapex;
                    innerrightapex = innerright.Apex();
                    changemade = true;
                }
            } while (changemade);

            // Find the two candidates to be the next "gear tooth."
            innerleft.Sym(ref leftcand);
            innerright.Sym(ref rightcand);
            // Create the bottom new bounding triangle.
            mesh.MakeTriangle(ref baseedge);
            // Connect it to the bounding boxes of the left and right triangulations.
            baseedge.Bond(ref innerleft);
            baseedge.Lnext();
            baseedge.Bond(ref innerright);
            baseedge.Lnext();
            baseedge.SetOrg(innerrightorg);
            baseedge.SetDest(innerleftdest);
            // Apex is intentionally left NULL.

            // Fix the extreme triangles if necessary.
            farleftpt = farleft.Org();
            if (innerleftdest == farleftpt)
            {
                baseedge.Lnext(ref farleft);
            }
            farrightpt = farright.Dest();
            if (innerrightorg == farrightpt)
            {
                baseedge.Lprev(ref farright);
            }
            // The vertices of the current knitting edge.
            lowerleft = innerleftdest;
            lowerright = innerrightorg;
            // The candidate vertices for knitting.
            upperleft = leftcand.Apex();
            upperright = rightcand.Apex();
            // Walk up the gap between the two triangulations, knitting them together.
            while (true)
            {
                // Have we reached the top? (This isn't quite the right question,
                // because even though the left triangulation might seem finished now,
                // moving up on the right triangulation might reveal a new vertex of
                // the left triangulation. And vice-versa.)
                leftfinished = predicates.CounterClockwise(upperleft, lowerleft, lowerright) <= 0.0;
                rightfinished = predicates.CounterClockwise(upperright, lowerleft, lowerright) <= 0.0;
                if (leftfinished && rightfinished)
                {
                    // Create the top new bounding triangle.
                    mesh.MakeTriangle(ref nextedge);
                    nextedge.SetOrg(lowerleft);
                    nextedge.SetDest(lowerright);
                    // Apex is intentionally left NULL.
                    // Connect it to the bounding boxes of the two triangulations.
                    nextedge.Bond(ref baseedge);
                    nextedge.Lnext();
                    nextedge.Bond(ref rightcand);
                    nextedge.Lnext();
                    nextedge.Bond(ref leftcand);

                    // Special treatment for horizontal cuts.
                    if (UseDwyer && (axis == 1))
                    {
                        farleftpt = farleft.Org();
                        farleftapex = farleft.Apex();
                        farrightpt = farright.Dest();
                        farrightapex = farright.Apex();
                        farleft.Sym(ref checkedge);
                        checkvertex = checkedge.Apex();
                        // The pointers to the extremal vertices are restored to the
                        // leftmost and rightmost vertices (rather than topmost and
                        // bottommost).
                        while (checkvertex.x < farleftpt.x)
                        {
                            checkedge.Lprev(ref farleft);
                            farleftapex = farleftpt;
                            farleftpt = checkvertex;
                            farleft.Sym(ref checkedge);
                            checkvertex = checkedge.Apex();
                        }
                        while (farrightapex.x > farrightpt.x)
                        {
                            farright.Lprev();
                            farright.Sym();
                            farrightpt = farrightapex;
                            farrightapex = farright.Apex();
                        }
                    }
                    return;
                }
                // Consider eliminating edges from the left triangulation.
                if (!leftfinished)
                {
                    // What vertex would be exposed if an edge were deleted?
                    leftcand.Lprev(ref nextedge);
                    nextedge.Sym();
                    nextapex = nextedge.Apex();
                    // If nextapex is NULL, then no vertex would be exposed; the
                    // triangulation would have been eaten right through.
                    if (nextapex != null)
                    {
                        // Check whether the edge is Delaunay.
                        badedge = predicates.InCircle(lowerleft, lowerright, upperleft, nextapex) > 0.0;
                        while (badedge)
                        {
                            // Eliminate the edge with an edge flip.  As a result, the
                            // left triangulation will have one more boundary triangle.
                            nextedge.Lnext();
                            nextedge.Sym(ref topcasing);
                            nextedge.Lnext();
                            nextedge.Sym(ref sidecasing);
                            nextedge.Bond(ref topcasing);
                            leftcand.Bond(ref sidecasing);
                            leftcand.Lnext();
                            leftcand.Sym(ref outercasing);
                            nextedge.Lprev();
                            nextedge.Bond(ref outercasing);
                            // Correct the vertices to reflect the edge flip.
                            leftcand.SetOrg(lowerleft);
                            leftcand.SetDest(null);
                            leftcand.SetApex(nextapex);
                            nextedge.SetOrg(null);
                            nextedge.SetDest(upperleft);
                            nextedge.SetApex(nextapex);
                            // Consider the newly exposed vertex.
                            upperleft = nextapex;
                            // What vertex would be exposed if another edge were deleted?
                            sidecasing.Copy(ref nextedge);
                            nextapex = nextedge.Apex();
                            if (nextapex != null)
                            {
                                // Check whether the edge is Delaunay.
                                badedge = predicates.InCircle(lowerleft, lowerright, upperleft, nextapex) > 0.0;
                            }
                            else
                            {
                                // Avoid eating right through the triangulation.
                                badedge = false;
                            }
                        }
                    }
                }
                // Consider eliminating edges from the right triangulation.
                if (!rightfinished)
                {
                    // What vertex would be exposed if an edge were deleted?
                    rightcand.Lnext(ref nextedge);
                    nextedge.Sym();
                    nextapex = nextedge.Apex();
                    // If nextapex is NULL, then no vertex would be exposed; the
                    // triangulation would have been eaten right through.
                    if (nextapex != null)
                    {
                        // Check whether the edge is Delaunay.
                        badedge = predicates.InCircle(lowerleft, lowerright, upperright, nextapex) > 0.0;
                        while (badedge)
                        {
                            // Eliminate the edge with an edge flip.  As a result, the
                            // right triangulation will have one more boundary triangle.
                            nextedge.Lprev();
                            nextedge.Sym(ref topcasing);
                            nextedge.Lprev();
                            nextedge.Sym(ref sidecasing);
                            nextedge.Bond(ref topcasing);
                            rightcand.Bond(ref sidecasing);
                            rightcand.Lprev();
                            rightcand.Sym(ref outercasing);
                            nextedge.Lnext();
                            nextedge.Bond(ref outercasing);
                            // Correct the vertices to reflect the edge flip.
                            rightcand.SetOrg(null);
                            rightcand.SetDest(lowerright);
                            rightcand.SetApex(nextapex);
                            nextedge.SetOrg(upperright);
                            nextedge.SetDest(null);
                            nextedge.SetApex(nextapex);
                            // Consider the newly exposed vertex.
                            upperright = nextapex;
                            // What vertex would be exposed if another edge were deleted?
                            sidecasing.Copy(ref nextedge);
                            nextapex = nextedge.Apex();
                            if (nextapex != null)
                            {
                                // Check whether the edge is Delaunay.
                                badedge = predicates.InCircle(lowerleft, lowerright, upperright, nextapex) > 0.0;
                            }
                            else
                            {
                                // Avoid eating right through the triangulation.
                                badedge = false;
                            }
                        }
                    }
                }
                if (leftfinished || (!rightfinished &&
                       (predicates.InCircle(upperleft, lowerleft, lowerright, upperright) > 0.0)))
                {
                    // Knit the triangulations, adding an edge from 'lowerleft'
                    // to 'upperright'.
                    baseedge.Bond(ref rightcand);
                    rightcand.Lprev(ref baseedge);
                    baseedge.SetDest(lowerleft);
                    lowerright = upperright;
                    baseedge.Sym(ref rightcand);
                    upperright = rightcand.Apex();
                }
                else
                {
                    // Knit the triangulations, adding an edge from 'upperleft'
                    // to 'lowerright'.
                    baseedge.Bond(ref leftcand);
                    leftcand.Lnext(ref baseedge);
                    baseedge.SetOrg(lowerright);
                    lowerleft = upperleft;
                    baseedge.Sym(ref leftcand);
                    upperleft = leftcand.Apex();
                }
            }
        }

        /// <summary>
        /// Recursively form a Delaunay triangulation by the divide-and-conquer method.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="axis"></param>
        /// <param name="farleft"></param>
        /// <param name="farright"></param>
        /// <remarks>
        /// Recursively breaks down the problem into smaller pieces, which are
        /// knitted together by mergehulls(). The base cases (problems of two or
        /// three vertices) are handled specially here.
        ///
        /// On completion, 'farleft' and 'farright' are bounding triangles such that
        /// the origin of 'farleft' is the leftmost vertex (breaking ties by
        /// choosing the highest leftmost vertex), and the destination of
        /// 'farright' is the rightmost vertex (breaking ties by choosing the
        /// lowest rightmost vertex).
        /// </remarks>
        void DivconqRecurse(int left, int right, int axis,
                            ref Otri farleft, ref Otri farright)
        {
            Otri midtri = default(Otri);
            Otri tri1 = default(Otri);
            Otri tri2 = default(Otri);
            Otri tri3 = default(Otri);
            Otri innerleft = default(Otri), innerright = default(Otri);
            double area;
            int vertices = right - left + 1;
            int divider;

            if (vertices == 2)
            {
                // The triangulation of two vertices is an edge.  An edge is
                // represented by two bounding triangles.
                mesh.MakeTriangle(ref farleft);
                farleft.SetOrg(sortarray[left]);
                farleft.SetDest(sortarray[left + 1]);
                // The apex is intentionally left NULL.
                mesh.MakeTriangle(ref farright);
                farright.SetOrg(sortarray[left + 1]);
                farright.SetDest(sortarray[left]);
                // The apex is intentionally left NULL.
                farleft.Bond(ref farright);
                farleft.Lprev();
                farright.Lnext();
                farleft.Bond(ref farright);
                farleft.Lprev();
                farright.Lnext();
                farleft.Bond(ref farright);

                // Ensure that the origin of 'farleft' is sortarray[0].
                farright.Lprev(ref farleft);
                return;
            }
            else if (vertices == 3)
            {
                // The triangulation of three vertices is either a triangle (with
                // three bounding triangles) or two edges (with four bounding
                // triangles).  In either case, four triangles are created.
                mesh.MakeTriangle(ref midtri);
                mesh.MakeTriangle(ref tri1);
                mesh.MakeTriangle(ref tri2);
                mesh.MakeTriangle(ref tri3);
                area = predicates.CounterClockwise(sortarray[left], sortarray[left + 1], sortarray[left + 2]);
                if (area == 0.0)
                {
                    // Three collinear vertices; the triangulation is two edges.
                    midtri.SetOrg(sortarray[left]);
                    midtri.SetDest(sortarray[left + 1]);
                    tri1.SetOrg(sortarray[left + 1]);
                    tri1.SetDest(sortarray[left]);
                    tri2.SetOrg(sortarray[left + 2]);
                    tri2.SetDest(sortarray[left + 1]);
                    tri3.SetOrg(sortarray[left + 1]);
                    tri3.SetDest(sortarray[left + 2]);
                    // All apices are intentionally left NULL.
                    midtri.Bond(ref tri1);
                    tri2.Bond(ref tri3);
                    midtri.Lnext();
                    tri1.Lprev();
                    tri2.Lnext();
                    tri3.Lprev();
                    midtri.Bond(ref tri3);
                    tri1.Bond(ref tri2);
                    midtri.Lnext();
                    tri1.Lprev();
                    tri2.Lnext();
                    tri3.Lprev();
                    midtri.Bond(ref tri1);
                    tri2.Bond(ref tri3);
                    // Ensure that the origin of 'farleft' is sortarray[0].
                    tri1.Copy(ref farleft);
                    // Ensure that the destination of 'farright' is sortarray[2].
                    tri2.Copy(ref farright);
                }
                else
                {
                    // The three vertices are not collinear; the triangulation is one
                    // triangle, namely 'midtri'.
                    midtri.SetOrg(sortarray[left]);
                    tri1.SetDest(sortarray[left]);
                    tri3.SetOrg(sortarray[left]);
                    // Apices of tri1, tri2, and tri3 are left NULL.
                    if (area > 0.0)
                    {
                        // The vertices are in counterclockwise order.
                        midtri.SetDest(sortarray[left + 1]);
                        tri1.SetOrg(sortarray[left + 1]);
                        tri2.SetDest(sortarray[left + 1]);
                        midtri.SetApex(sortarray[left + 2]);
                        tri2.SetOrg(sortarray[left + 2]);
                        tri3.SetDest(sortarray[left + 2]);
                    }
                    else
                    {
                        // The vertices are in clockwise order.
                        midtri.SetDest(sortarray[left + 2]);
                        tri1.SetOrg(sortarray[left + 2]);
                        tri2.SetDest(sortarray[left + 2]);
                        midtri.SetApex(sortarray[left + 1]);
                        tri2.SetOrg(sortarray[left + 1]);
                        tri3.SetDest(sortarray[left + 1]);
                    }
                    // The topology does not depend on how the vertices are ordered.
                    midtri.Bond(ref tri1);
                    midtri.Lnext();
                    midtri.Bond(ref tri2);
                    midtri.Lnext();
                    midtri.Bond(ref tri3);
                    tri1.Lprev();
                    tri2.Lnext();
                    tri1.Bond(ref tri2);
                    tri1.Lprev();
                    tri3.Lprev();
                    tri1.Bond(ref tri3);
                    tri2.Lnext();
                    tri3.Lprev();
                    tri2.Bond(ref tri3);
                    // Ensure that the origin of 'farleft' is sortarray[0].
                    tri1.Copy(ref farleft);
                    // Ensure that the destination of 'farright' is sortarray[2].
                    if (area > 0.0)
                    {
                        tri2.Copy(ref farright);
                    }
                    else
                    {
                        farleft.Lnext(ref farright);
                    }
                }

                return;
            }
            else
            {
                // Split the vertices in half.
                divider = vertices >> 1;

                // Recursively triangulate each half.
                DivconqRecurse(left, left + divider - 1, 1 - axis, ref farleft, ref innerleft);
                DivconqRecurse(left + divider, right, 1 - axis, ref innerright, ref farright);

                // Merge the two triangulations into one.
                MergeHulls(ref farleft, ref innerleft, ref innerright, ref farright, axis);
            }
        }

        /// <summary>
        /// Removes ghost triangles.
        /// </summary>
        /// <param name="startghost"></param>
        /// <returns>Number of vertices on the hull.</returns>
        int RemoveGhosts(ref Otri startghost)
        {
            Otri searchedge = default(Otri);
            Otri dissolveedge = default(Otri);
            Otri deadtriangle = default(Otri);
            Vertex markorg;

            int hullsize;

            bool noPoly = !mesh.behavior.Poly;

            // Find an edge on the convex hull to start point location from.
            startghost.Lprev(ref searchedge);
            searchedge.Sym();
            mesh.dummytri.neighbors[0] = searchedge;

            // Remove the bounding box and count the convex hull edges.
            startghost.Copy(ref dissolveedge);
            hullsize = 0;
            do
            {
                hullsize++;
                dissolveedge.Lnext(ref deadtriangle);
                dissolveedge.Lprev();
                dissolveedge.Sym();

                // If no PSLG is involved, set the boundary markers of all the vertices
                // on the convex hull.  If a PSLG is used, this step is done later.
                if (noPoly)
                {
                    // Watch out for the case where all the input vertices are collinear.
                    if (dissolveedge.tri.id != Mesh.DUMMY)
                    {
                        markorg = dissolveedge.Org();
                        if (markorg.label == 0)
                        {
                            markorg.label = 1;
                        }
                    }
                }
                // Remove a bounding triangle from a convex hull triangle.
                dissolveedge.Dissolve(mesh.dummytri);
                // Find the next bounding triangle.
                deadtriangle.Sym(ref dissolveedge);

                // Delete the bounding triangle.
                mesh.TriangleDealloc(deadtriangle.tri);
            } while (!dissolveedge.Equals(startghost));

            return hullsize;
        }
    }
}
