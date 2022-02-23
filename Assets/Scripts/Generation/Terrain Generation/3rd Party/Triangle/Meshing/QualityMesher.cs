// -----------------------------------------------------------------------
// <copyright file="QualityMesher.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Geometry;
    using TriangleNet.Logging;
    using TriangleNet.Meshing.Data;
    using TriangleNet.Tools;
    using TriangleNet.Topology;

    /// <summary>
    /// Provides methods for mesh quality enforcement and testing.
    /// </summary>
    class QualityMesher
    {
        IPredicates predicates;

        Queue<BadSubseg> badsubsegs;
        BadTriQueue queue;
        Mesh mesh;
        Behavior behavior;

        NewLocation newLocation;

        ILog<LogItem> logger;

        // Stores the vertices of the triangle that contains newvertex
        // in SplitTriangle method.
        Triangle newvertex_tri;

        public QualityMesher(Mesh mesh, Configuration config)
        {
            logger = Log.Instance;

            badsubsegs = new Queue<BadSubseg>();
            queue = new BadTriQueue();

            this.mesh = mesh;
            this.predicates = config.Predicates();

            this.behavior = mesh.behavior;

            newLocation = new NewLocation(mesh, predicates);

            newvertex_tri = new Triangle();
        }

        /// <summary>
        /// Apply quality constraints to a mesh.
        /// </summary>
        /// <param name="quality">The quality constraints.</param>
        /// <param name="delaunay">A value indicating, if the refined mesh should be Conforming Delaunay.</param>
        public void Apply(QualityOptions quality, bool delaunay = false)
        {
            // Copy quality options
            if (quality != null)
            {
                behavior.Quality = true;

                behavior.MinAngle = quality.MinimumAngle;
                behavior.MaxAngle = quality.MaximumAngle;
                behavior.MaxArea = quality.MaximumArea;
                behavior.UserTest = quality.UserTest;
                behavior.VarArea = quality.VariableArea;

                behavior.ConformingDelaunay = behavior.ConformingDelaunay || delaunay;

                mesh.steinerleft = quality.SteinerPoints == 0 ? -1 : quality.SteinerPoints;
            }

            // TODO: remove
            if (!behavior.Poly)
            {
                // Be careful not to allocate space for element area constraints that
                // will never be assigned any value (other than the default -1.0).
                behavior.VarArea = false;
            }

            // Ensure that no vertex can be mistaken for a triangular bounding
            // box vertex in insertvertex().
            mesh.infvertex1 = null;
            mesh.infvertex2 = null;
            mesh.infvertex3 = null;

            if (behavior.useSegments)
            {
                mesh.checksegments = true;
            }

            if (behavior.Quality && mesh.triangles.Count > 0)
            {
                // Enforce angle and area constraints.
                EnforceQuality();
            }
        }

        /// <summary>
        /// Add a bad subsegment to the queue.
        /// </summary>
        /// <param name="badseg">Bad subsegment.</param>
        public void AddBadSubseg(BadSubseg badseg)
        {
            badsubsegs.Enqueue(badseg);
        }

        #region Check

        /// <summary>
        /// Check a subsegment to see if it is encroached; add it to the list if it is.
        /// </summary>
        /// <param name="testsubseg">The subsegment to check.</param>
        /// <returns>Returns a nonzero value if the subsegment is encroached.</returns>
        /// <remarks>
        /// A subsegment is encroached if there is a vertex in its diametral lens.
        /// For Ruppert's algorithm (-D switch), the "diametral lens" is the
        /// diametral circle. For Chew's algorithm (default), the diametral lens is
        /// just big enough to enclose two isosceles triangles whose bases are the
        /// subsegment. Each of the two isosceles triangles has two angles equal
        /// to 'b.minangle'.
        ///
        /// Chew's algorithm does not require diametral lenses at all--but they save
        /// time. Any vertex inside a subsegment's diametral lens implies that the
        /// triangle adjoining the subsegment will be too skinny, so it's only a
        /// matter of time before the encroaching vertex is deleted by Chew's
        /// algorithm. It's faster to simply not insert the doomed vertex in the
        /// first place, which is why I use diametral lenses with Chew's algorithm.
        /// </remarks>
        public int CheckSeg4Encroach(ref Osub testsubseg)
        {
            Otri neighbortri = default(Otri);
            Osub testsym = default(Osub);
            BadSubseg encroachedseg;
            double dotproduct;
            int encroached;
            int sides;
            Vertex eorg, edest, eapex;

            encroached = 0;
            sides = 0;

            eorg = testsubseg.Org();
            edest = testsubseg.Dest();
            // Check one neighbor of the subsegment.
            testsubseg.Pivot(ref neighbortri);
            // Does the neighbor exist, or is this a boundary edge?
            if (neighbortri.tri.id != Mesh.DUMMY)
            {
                sides++;
                // Find a vertex opposite this subsegment.
                eapex = neighbortri.Apex();
                // Check whether the apex is in the diametral lens of the subsegment
                // (the diametral circle if 'conformdel' is set).  A dot product
                // of two sides of the triangle is used to check whether the angle
                // at the apex is greater than (180 - 2 'minangle') degrees (for
                // lenses; 90 degrees for diametral circles).
                dotproduct = (eorg.x - eapex.x) * (edest.x - eapex.x) +
                             (eorg.y - eapex.y) * (edest.y - eapex.y);
                if (dotproduct < 0.0)
                {
                    if (behavior.ConformingDelaunay ||
                        (dotproduct * dotproduct >=
                         (2.0 * behavior.goodAngle - 1.0) * (2.0 * behavior.goodAngle - 1.0) *
                         ((eorg.x - eapex.x) * (eorg.x - eapex.x) +
                          (eorg.y - eapex.y) * (eorg.y - eapex.y)) *
                         ((edest.x - eapex.x) * (edest.x - eapex.x) +
                          (edest.y - eapex.y) * (edest.y - eapex.y))))
                    {
                        encroached = 1;
                    }
                }
            }
            // Check the other neighbor of the subsegment.
            testsubseg.Sym(ref testsym);
            testsym.Pivot(ref neighbortri);
            // Does the neighbor exist, or is this a boundary edge?
            if (neighbortri.tri.id != Mesh.DUMMY)
            {
                sides++;
                // Find the other vertex opposite this subsegment.
                eapex = neighbortri.Apex();
                // Check whether the apex is in the diametral lens of the subsegment
                // (or the diametral circle, if 'conformdel' is set).
                dotproduct = (eorg.x - eapex.x) * (edest.x - eapex.x) +
                             (eorg.y - eapex.y) * (edest.y - eapex.y);
                if (dotproduct < 0.0)
                {
                    if (behavior.ConformingDelaunay ||
                        (dotproduct * dotproduct >=
                         (2.0 * behavior.goodAngle - 1.0) * (2.0 * behavior.goodAngle - 1.0) *
                         ((eorg.x - eapex.x) * (eorg.x - eapex.x) +
                          (eorg.y - eapex.y) * (eorg.y - eapex.y)) *
                         ((edest.x - eapex.x) * (edest.x - eapex.x) +
                          (edest.y - eapex.y) * (edest.y - eapex.y))))
                    {
                        encroached += 2;
                    }
                }
            }

            if (encroached > 0 && (behavior.NoBisect == 0 || ((behavior.NoBisect == 1) && (sides == 2))))
            {
                // Add the subsegment to the list of encroached subsegments.
                // Be sure to get the orientation right.
                encroachedseg = new BadSubseg();
                if (encroached == 1)
                {
                    encroachedseg.subseg = testsubseg;
                    encroachedseg.org = eorg;
                    encroachedseg.dest = edest;
                }
                else
                {
                    encroachedseg.subseg = testsym;
                    encroachedseg.org = edest;
                    encroachedseg.dest = eorg;
                }

                badsubsegs.Enqueue(encroachedseg);
            }

            return encroached;
        }

        /// <summary>
        /// Test a triangle for quality and size.
        /// </summary>
        /// <param name="testtri">Triangle to check.</param>
        /// <remarks>
        /// Tests a triangle to see if it satisfies the minimum angle condition and
        /// the maximum area condition.  Triangles that aren't up to spec are added
        /// to the bad triangle queue.
        /// </remarks>
        public void TestTriangle(ref Otri testtri)
        {
            Otri tri1 = default(Otri), tri2 = default(Otri);
            Osub testsub = default(Osub);
            Vertex torg, tdest, tapex;
            Vertex base1, base2;
            Vertex org1, dest1, org2, dest2;
            Vertex joinvertex;
            double dxod, dyod, dxda, dyda, dxao, dyao;
            double dxod2, dyod2, dxda2, dyda2, dxao2, dyao2;
            double apexlen, orglen, destlen, minedge;
            double angle;
            double area;
            double dist1, dist2;

            double maxangle;

            torg = testtri.Org();
            tdest = testtri.Dest();
            tapex = testtri.Apex();
            dxod = torg.x - tdest.x;
            dyod = torg.y - tdest.y;
            dxda = tdest.x - tapex.x;
            dyda = tdest.y - tapex.y;
            dxao = tapex.x - torg.x;
            dyao = tapex.y - torg.y;
            dxod2 = dxod * dxod;
            dyod2 = dyod * dyod;
            dxda2 = dxda * dxda;
            dyda2 = dyda * dyda;
            dxao2 = dxao * dxao;
            dyao2 = dyao * dyao;
            // Find the lengths of the triangle's three edges.
            apexlen = dxod2 + dyod2;
            orglen = dxda2 + dyda2;
            destlen = dxao2 + dyao2;

            if ((apexlen < orglen) && (apexlen < destlen))
            {
                // The edge opposite the apex is shortest.
                minedge = apexlen;
                // Find the square of the cosine of the angle at the apex.
                angle = dxda * dxao + dyda * dyao;
                angle = angle * angle / (orglen * destlen);
                base1 = torg;
                base2 = tdest;
                testtri.Copy(ref tri1);
            }
            else if (orglen < destlen)
            {
                // The edge opposite the origin is shortest.
                minedge = orglen;
                // Find the square of the cosine of the angle at the origin.
                angle = dxod * dxao + dyod * dyao;
                angle = angle * angle / (apexlen * destlen);
                base1 = tdest;
                base2 = tapex;
                testtri.Lnext(ref tri1);
            }
            else
            {
                // The edge opposite the destination is shortest.
                minedge = destlen;
                // Find the square of the cosine of the angle at the destination.
                angle = dxod * dxda + dyod * dyda;
                angle = angle * angle / (apexlen * orglen);
                base1 = tapex;
                base2 = torg;
                testtri.Lprev(ref tri1);
            }

            if (behavior.VarArea || behavior.fixedArea || (behavior.UserTest != null))
            {
                // Check whether the area is larger than permitted.
                area = 0.5 * (dxod * dyda - dyod * dxda);
                if (behavior.fixedArea && (area > behavior.MaxArea))
                {
                    // Add this triangle to the list of bad triangles.
                    queue.Enqueue(ref testtri, minedge, tapex, torg, tdest);
                    return;
                }

                // Nonpositive area constraints are treated as unconstrained.
                if ((behavior.VarArea) && (area > testtri.tri.area) && (testtri.tri.area > 0.0))
                {
                    // Add this triangle to the list of bad triangles.
                    queue.Enqueue(ref testtri, minedge, tapex, torg, tdest);
                    return;
                }

                // Check whether the user thinks this triangle is too large.
                if ((behavior.UserTest != null) && behavior.UserTest(testtri.tri, area))
                {
                    queue.Enqueue(ref testtri, minedge, tapex, torg, tdest);
                    return;
                }
            }

            // find the maximum edge and accordingly the pqr orientation
            if ((apexlen > orglen) && (apexlen > destlen))
            {
                // The edge opposite the apex is longest.
                // maxedge = apexlen;
                // Find the cosine of the angle at the apex.
                maxangle = (orglen + destlen - apexlen) / (2 * Math.Sqrt(orglen * destlen));
            }
            else if (orglen > destlen)
            {
                // The edge opposite the origin is longest.
                // maxedge = orglen;
                // Find the cosine of the angle at the origin.
                maxangle = (apexlen + destlen - orglen) / (2 * Math.Sqrt(apexlen * destlen));
            }
            else
            {
                // The edge opposite the destination is longest.
                // maxedge = destlen;
                // Find the cosine of the angle at the destination.
                maxangle = (apexlen + orglen - destlen) / (2 * Math.Sqrt(apexlen * orglen));
            }

            // Check whether the angle is smaller than permitted.
            if ((angle > behavior.goodAngle) || (maxangle < behavior.maxGoodAngle && behavior.MaxAngle != 0.0))
            {
                // Use the rules of Miller, Pav, and Walkington to decide that certain
                // triangles should not be split, even if they have bad angles.
                // A skinny triangle is not split if its shortest edge subtends a
                // small input angle, and both endpoints of the edge lie on a
                // concentric circular shell.  For convenience, I make a small
                // adjustment to that rule:  I check if the endpoints of the edge
                // both lie in segment interiors, equidistant from the apex where
                // the two segments meet.
                // First, check if both points lie in segment interiors.
                if ((base1.type == VertexType.SegmentVertex) &&
                    (base2.type == VertexType.SegmentVertex))
                {
                    // Check if both points lie in a common segment. If they do, the
                    // skinny triangle is enqueued to be split as usual.
                    tri1.Pivot(ref testsub);
                    if (testsub.seg.hash == Mesh.DUMMY)
                    {
                        // No common segment.  Find a subsegment that contains 'torg'.
                        tri1.Copy(ref tri2);
                        do
                        {
                            tri1.Oprev();
                            tri1.Pivot(ref testsub);
                        } while (testsub.seg.hash == Mesh.DUMMY);
                        // Find the endpoints of the containing segment.
                        org1 = testsub.SegOrg();
                        dest1 = testsub.SegDest();
                        // Find a subsegment that contains 'tdest'.
                        do
                        {
                            tri2.Dnext();
                            tri2.Pivot(ref testsub);
                        } while (testsub.seg.hash == Mesh.DUMMY);
                        // Find the endpoints of the containing segment.
                        org2 = testsub.SegOrg();
                        dest2 = testsub.SegDest();
                        // Check if the two containing segments have an endpoint in common.
                        joinvertex = null;
                        if ((dest1.x == org2.x) && (dest1.y == org2.y))
                        {
                            joinvertex = dest1;
                        }
                        else if ((org1.x == dest2.x) && (org1.y == dest2.y))
                        {
                            joinvertex = org1;
                        }
                        if (joinvertex != null)
                        {
                            // Compute the distance from the common endpoint (of the two
                            // segments) to each of the endpoints of the shortest edge.
                            dist1 = ((base1.x - joinvertex.x) * (base1.x - joinvertex.x) +
                                     (base1.y - joinvertex.y) * (base1.y - joinvertex.y));
                            dist2 = ((base2.x - joinvertex.x) * (base2.x - joinvertex.x) +
                                     (base2.y - joinvertex.y) * (base2.y - joinvertex.y));
                            // If the two distances are equal, don't split the triangle.
                            if ((dist1 < 1.001 * dist2) && (dist1 > 0.999 * dist2))
                            {
                                // Return now to avoid enqueueing the bad triangle.
                                return;
                            }
                        }
                    }
                }

                // Add this triangle to the list of bad triangles.
                queue.Enqueue(ref testtri, minedge, tapex, torg, tdest);
            }
        }

        #endregion

        #region Maintanance

        /// <summary>
        /// Traverse the entire list of subsegments, and check each to see if it 
        /// is encroached. If so, add it to the list.
        /// </summary>
        private void TallyEncs()
        {
            Osub subsegloop = default(Osub);
            subsegloop.orient = 0;

            foreach (var seg in mesh.subsegs.Values)
            {
                subsegloop.seg = seg;
                // If the segment is encroached, add it to the list.
                CheckSeg4Encroach(ref subsegloop);
            }
        }

        /// <summary>
        /// Split all the encroached subsegments.
        /// </summary>
        /// <param name="triflaws">A flag that specifies whether one should take 
        /// note of new bad triangles that result from inserting vertices to repair 
        /// encroached subsegments.</param>
        /// <remarks>
        /// Each encroached subsegment is repaired by splitting it - inserting a
        /// vertex at or near its midpoint.  Newly inserted vertices may encroach
        /// upon other subsegments; these are also repaired.
        /// </remarks>
        private void SplitEncSegs(bool triflaws)
        {
            Otri enctri = default(Otri);
            Otri testtri = default(Otri);
            Osub testsh = default(Osub);
            Osub currentenc = default(Osub);
            BadSubseg seg;
            Vertex eorg, edest, eapex;
            Vertex newvertex;
            InsertVertexResult success;
            double segmentlength, nearestpoweroftwo;
            double split;
            double multiplier, divisor;
            bool acuteorg, acuteorg2, acutedest, acutedest2;

            // Note that steinerleft == -1 if an unlimited number
            // of Steiner points is allowed.
            while (badsubsegs.Count > 0)
            {
                if (mesh.steinerleft == 0)
                {
                    break;
                }

                seg = badsubsegs.Dequeue();

                currentenc = seg.subseg;
                eorg = currentenc.Org();
                edest = currentenc.Dest();
                // Make sure that this segment is still the same segment it was
                // when it was determined to be encroached.  If the segment was
                // enqueued multiple times (because several newly inserted
                // vertices encroached it), it may have already been split.
                if (!Osub.IsDead(currentenc.seg) && (eorg == seg.org) && (edest == seg.dest))
                {
                    // To decide where to split a segment, we need to know if the
                    // segment shares an endpoint with an adjacent segment.
                    // The concern is that, if we simply split every encroached
                    // segment in its center, two adjacent segments with a small
                    // angle between them might lead to an infinite loop; each
                    // vertex added to split one segment will encroach upon the
                    // other segment, which must then be split with a vertex that
                    // will encroach upon the first segment, and so on forever.
                    // To avoid this, imagine a set of concentric circles, whose
                    // radii are powers of two, about each segment endpoint.
                    // These concentric circles determine where the segment is
                    // split. (If both endpoints are shared with adjacent
                    // segments, split the segment in the middle, and apply the
                    // concentric circles for later splittings.)

                    // Is the origin shared with another segment?
                    currentenc.Pivot(ref enctri);
                    enctri.Lnext(ref testtri);
                    testtri.Pivot(ref testsh);
                    acuteorg = testsh.seg.hash != Mesh.DUMMY;
                    // Is the destination shared with another segment?
                    testtri.Lnext();
                    testtri.Pivot(ref testsh);
                    acutedest = testsh.seg.hash != Mesh.DUMMY;

                    // If we're using Chew's algorithm (rather than Ruppert's)
                    // to define encroachment, delete free vertices from the
                    // subsegment's diametral circle.
                    if (!behavior.ConformingDelaunay && !acuteorg && !acutedest)
                    {
                        eapex = enctri.Apex();
                        while ((eapex.type == VertexType.FreeVertex) &&
                               ((eorg.x - eapex.x) * (edest.x - eapex.x) +
                                (eorg.y - eapex.y) * (edest.y - eapex.y) < 0.0))
                        {
                            mesh.DeleteVertex(ref testtri);
                            currentenc.Pivot(ref enctri);
                            eapex = enctri.Apex();
                            enctri.Lprev(ref testtri);
                        }
                    }

                    // Now, check the other side of the segment, if there's a triangle there.
                    enctri.Sym(ref testtri);
                    if (testtri.tri.id != Mesh.DUMMY)
                    {
                        // Is the destination shared with another segment?
                        testtri.Lnext();
                        testtri.Pivot(ref testsh);
                        acutedest2 = testsh.seg.hash != Mesh.DUMMY;
                        acutedest = acutedest || acutedest2;
                        // Is the origin shared with another segment?
                        testtri.Lnext();
                        testtri.Pivot(ref testsh);
                        acuteorg2 = testsh.seg.hash != Mesh.DUMMY;
                        acuteorg = acuteorg || acuteorg2;

                        // Delete free vertices from the subsegment's diametral circle.
                        if (!behavior.ConformingDelaunay && !acuteorg2 && !acutedest2)
                        {
                            eapex = testtri.Org();
                            while ((eapex.type == VertexType.FreeVertex) &&
                                   ((eorg.x - eapex.x) * (edest.x - eapex.x) +
                                    (eorg.y - eapex.y) * (edest.y - eapex.y) < 0.0))
                            {
                                mesh.DeleteVertex(ref testtri);
                                enctri.Sym(ref testtri);
                                eapex = testtri.Apex();
                                testtri.Lprev();
                            }
                        }
                    }

                    // Use the concentric circles if exactly one endpoint is shared
                    // with another adjacent segment.
                    if (acuteorg || acutedest)
                    {
                        segmentlength = Math.Sqrt((edest.x - eorg.x) * (edest.x - eorg.x) +
                                             (edest.y - eorg.y) * (edest.y - eorg.y));
                        // Find the power of two that most evenly splits the segment.
                        // The worst case is a 2:1 ratio between subsegment lengths.
                        nearestpoweroftwo = 1.0;
                        while (segmentlength > 3.0 * nearestpoweroftwo)
                        {
                            nearestpoweroftwo *= 2.0;
                        }
                        while (segmentlength < 1.5 * nearestpoweroftwo)
                        {
                            nearestpoweroftwo *= 0.5;
                        }
                        // Where do we split the segment?
                        split = nearestpoweroftwo / segmentlength;
                        if (acutedest)
                        {
                            split = 1.0 - split;
                        }
                    }
                    else
                    {
                        // If we're not worried about adjacent segments, split
                        // this segment in the middle.
                        split = 0.5;
                    }

                    // Create the new vertex (interpolate coordinates).
                    newvertex = new Vertex(
                        eorg.x + split * (edest.x - eorg.x),
                        eorg.y + split * (edest.y - eorg.y),
                        currentenc.seg.boundary
#if USE_ATTRIBS
                        , mesh.nextras
#endif
                    );

                    newvertex.type = VertexType.SegmentVertex;

                    newvertex.hash = mesh.hash_vtx++;
                    newvertex.id = newvertex.hash;

                    mesh.vertices.Add(newvertex.hash, newvertex);
#if USE_ATTRIBS
                    // Interpolate attributes.
                    for (int i = 0; i < mesh.nextras; i++)
                    {
                        newvertex.attributes[i] = eorg.attributes[i]
                            + split * (edest.attributes[i] - eorg.attributes[i]);
                    }
#endif
#if USE_Z
                    newvertex.z = eorg.z + split * (edest.z - eorg.z);
#endif
                    if (!Behavior.NoExact)
                    {
                        // Roundoff in the above calculation may yield a 'newvertex'
                        // that is not precisely collinear with 'eorg' and 'edest'.
                        // Improve collinearity by one step of iterative refinement.
                        multiplier = predicates.CounterClockwise(eorg, edest, newvertex);
                        divisor = ((eorg.x - edest.x) * (eorg.x - edest.x) +
                                   (eorg.y - edest.y) * (eorg.y - edest.y));
                        if ((multiplier != 0.0) && (divisor != 0.0))
                        {
                            multiplier = multiplier / divisor;
                            // Watch out for NANs.
                            if (!double.IsNaN(multiplier))
                            {
                                newvertex.x += multiplier * (edest.y - eorg.y);
                                newvertex.y += multiplier * (eorg.x - edest.x);
                            }
                        }
                    }

                    // Check whether the new vertex lies on an endpoint.
                    if (((newvertex.x == eorg.x) && (newvertex.y == eorg.y)) ||
                        ((newvertex.x == edest.x) && (newvertex.y == edest.y)))
                    {

                        logger.Error("Ran out of precision: I attempted to split a"
                            + " segment to a smaller size than can be accommodated by"
                            + " the finite precision of floating point arithmetic.",
                            "Quality.SplitEncSegs()");

                        throw new Exception("Ran out of precision");
                    }
                    // Insert the splitting vertex.  This should always succeed.
                    success = mesh.InsertVertex(newvertex, ref enctri, ref currentenc, true, triflaws);
                    if ((success != InsertVertexResult.Successful) && (success != InsertVertexResult.Encroaching))
                    {
                        logger.Error("Failure to split a segment.", "Quality.SplitEncSegs()");
                        throw new Exception("Failure to split a segment.");
                    }
                    if (mesh.steinerleft > 0)
                    {
                        mesh.steinerleft--;
                    }
                    // Check the two new subsegments to see if they're encroached.
                    CheckSeg4Encroach(ref currentenc);
                    currentenc.Next();
                    CheckSeg4Encroach(ref currentenc);
                }

                // Set subsegment's origin to NULL. This makes it possible to detect dead 
                // badsubsegs when traversing the list of all badsubsegs.
                seg.org = null;
            }
        }

        /// <summary>
        /// Test every triangle in the mesh for quality measures.
        /// </summary>
        private void TallyFaces()
        {
            Otri triangleloop = default(Otri);
            triangleloop.orient = 0;

            foreach (var tri in mesh.triangles)
            {
                triangleloop.tri = tri;

                // If the triangle is bad, enqueue it.
                TestTriangle(ref triangleloop);
            }
        }

        /// <summary>
        /// Inserts a vertex at the circumcenter of a triangle. Deletes 
        /// the newly inserted vertex if it encroaches upon a segment.
        /// </summary>
        /// <param name="badtri"></param>
        private void SplitTriangle(BadTriangle badtri)
        {
            Otri badotri = default(Otri);
            Vertex borg, bdest, bapex;
            Point newloc; // Location of the new vertex
            double xi = 0, eta = 0;
            InsertVertexResult success;
            bool errorflag;

            badotri = badtri.poortri;
            borg = badotri.Org();
            bdest = badotri.Dest();
            bapex = badotri.Apex();

            // Make sure that this triangle is still the same triangle it was
            // when it was tested and determined to be of bad quality.
            // Subsequent transformations may have made it a different triangle.
            if (!Otri.IsDead(badotri.tri) && (borg == badtri.org) &&
                (bdest == badtri.dest) && (bapex == badtri.apex))
            {
                errorflag = false;
                // Create a new vertex at the triangle's circumcenter.

                // Using the original (simpler) Steiner point location method
                // for mesh refinement.
                // TODO: NewLocation doesn't work for refinement. Why? Maybe 
                // reset VertexType?
                if (behavior.fixedArea || behavior.VarArea)
                {
                    newloc = predicates.FindCircumcenter(borg, bdest, bapex, ref xi, ref eta, behavior.offconstant);
                }
                else
                {
                    newloc = newLocation.FindLocation(borg, bdest, bapex, ref xi, ref eta, true, badotri);
                }

                // Check whether the new vertex lies on a triangle vertex.
                if (((newloc.x == borg.x) && (newloc.y == borg.y)) ||
                    ((newloc.x == bdest.x) && (newloc.y == bdest.y)) ||
                    ((newloc.x == bapex.x) && (newloc.y == bapex.y)))
                {
                    if (Log.Verbose)
                    {
                        logger.Warning("New vertex falls on existing vertex.", "Quality.SplitTriangle()");
                        errorflag = true;
                    }
                }
                else
                {
                    // The new vertex must be in the interior, and therefore is a
                    // free vertex with a marker of zero.
                    Vertex newvertex = new Vertex(newloc.x, newloc.y, 0
#if USE_ATTRIBS
                        , mesh.nextras
#endif
                        );

                    newvertex.type = VertexType.FreeVertex;

                    // Ensure that the handle 'badotri' does not represent the longest
                    // edge of the triangle.  This ensures that the circumcenter must
                    // fall to the left of this edge, so point location will work.
                    // (If the angle org-apex-dest exceeds 90 degrees, then the
                    // circumcenter lies outside the org-dest edge, and eta is
                    // negative.  Roundoff error might prevent eta from being
                    // negative when it should be, so I test eta against xi.)
                    if (eta < xi)
                    {
                        badotri.Lprev();
                    }

                    // Assign triangle for attributes interpolation.
                    newvertex.tri.tri = newvertex_tri;

                    // Insert the circumcenter, searching from the edge of the triangle,
                    // and maintain the Delaunay property of the triangulation.
                    Osub tmp = default(Osub);
                    success = mesh.InsertVertex(newvertex, ref badotri, ref tmp, true, true);

                    if (success == InsertVertexResult.Successful)
                    {
                        newvertex.hash = mesh.hash_vtx++;
                        newvertex.id = newvertex.hash;
#if USE_ATTRIBS
                        if (mesh.nextras > 0)
                        {
                            Interpolation.InterpolateAttributes(newvertex, newvertex.tri.tri, mesh.nextras);
                        }
#endif
#if USE_Z
                        Interpolation.InterpolateZ(newvertex, newvertex.tri.tri);
#endif
                        mesh.vertices.Add(newvertex.hash, newvertex);

                        if (mesh.steinerleft > 0)
                        {
                            mesh.steinerleft--;
                        }
                    }
                    else if (success == InsertVertexResult.Encroaching)
                    {
                        // If the newly inserted vertex encroaches upon a subsegment,
                        // delete the new vertex.
                        mesh.UndoVertex();
                    }
                    else if (success == InsertVertexResult.Violating)
                    {
                        // Failed to insert the new vertex, but some subsegment was
                        // marked as being encroached.
                    }
                    else
                    {   // success == DUPLICATEVERTEX
                        // Couldn't insert the new vertex because a vertex is already there.
                        if (Log.Verbose)
                        {
                            logger.Warning("New vertex falls on existing vertex.", "Quality.SplitTriangle()");
                            errorflag = true;
                        }
                    }
                }
                if (errorflag)
                {
                    logger.Error("The new vertex is at the circumcenter of triangle: This probably "
                        + "means that I am trying to refine triangles to a smaller size than can be "
                        + "accommodated by the finite precision of floating point arithmetic.",
                        "Quality.SplitTriangle()");

                    throw new Exception("The new vertex is at the circumcenter of triangle.");
                }
            }
        }

        /// <summary>
        /// Remove all the encroached subsegments and bad triangles from the triangulation.
        /// </summary>
        private void EnforceQuality()
        {
            BadTriangle badtri;

            // Test all segments to see if they're encroached.
            TallyEncs();

            // Fix encroached subsegments without noting bad triangles.
            SplitEncSegs(false);
            // At this point, if we haven't run out of Steiner points, the
            // triangulation should be (conforming) Delaunay.

            // Next, we worry about enforcing triangle quality.
            if ((behavior.MinAngle > 0.0) || behavior.VarArea || behavior.fixedArea || behavior.UserTest != null)
            {
                // TODO: Reset queue? (Or is it always empty at this point)

                // Test all triangles to see if they're bad.
                TallyFaces();

                mesh.checkquality = true;
                while ((queue.Count > 0) && (mesh.steinerleft != 0))
                {
                    // Fix one bad triangle by inserting a vertex at its circumcenter.
                    badtri = queue.Dequeue();
                    SplitTriangle(badtri);

                    if (badsubsegs.Count > 0)
                    {
                        // Put bad triangle back in queue for another try later.
                        queue.Enqueue(badtri);
                        // Fix any encroached subsegments that resulted.
                        // Record any new bad triangles that result.
                        SplitEncSegs(true);
                    }
                }
            }

            // At this point, if the "-D" switch was selected and we haven't run out
            // of Steiner points, the triangulation should be (conforming) Delaunay
            // and have no low-quality triangles.

            // Might we have run out of Steiner points too soon?
            if (Log.Verbose && behavior.ConformingDelaunay && (badsubsegs.Count > 0) && (mesh.steinerleft == 0))
            {

                logger.Warning("I ran out of Steiner points, but the mesh has encroached subsegments, "
                        + "and therefore might not be truly Delaunay. If the Delaunay property is important "
                        + "to you, try increasing the number of Steiner points.",
                        "Quality.EnforceQuality()");
            }
        }

        #endregion
    }
}
