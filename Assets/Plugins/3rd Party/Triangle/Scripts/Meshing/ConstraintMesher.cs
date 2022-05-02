// -----------------------------------------------------------------------
// <copyright file="ConstraintMesher.cs">
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
    using TriangleNet.Meshing.Iterators;
    using TriangleNet.Topology;

    internal class ConstraintMesher
    {
        IPredicates predicates;

        Mesh mesh;
        Behavior behavior;
        TriangleLocator locator;

        List<Triangle> viri;

        ILog<LogItem> logger;

        public ConstraintMesher(Mesh mesh, Configuration config)
        {
            this.mesh = mesh;
            this.predicates = config.Predicates();

            this.behavior = mesh.behavior;
            this.locator = mesh.locator;

            this.viri = new List<Triangle>();

            logger = Log.Instance;
        }


        /// <summary>
        /// Insert segments into the mesh.
        /// </summary>
        /// <param name="input">The polygon.</param>
        /// <param name="options">Constraint options.</param>
        public void Apply(IPolygon input, ConstraintOptions options)
        {
            behavior.Poly = input.Segments.Count > 0;

            // Copy constraint options
            if (options != null)
            {
                behavior.ConformingDelaunay = options.ConformingDelaunay;
                behavior.Convex = options.Convex;
                behavior.NoBisect = options.SegmentSplitting;

                if (behavior.ConformingDelaunay)
                {
                    behavior.Quality = true;
                }
            }

            //if (input.EdgeMarkers != null)
            //{
            //    behavior.UseBoundaryMarkers = true;
            //}

            behavior.useRegions = input.Regions.Count > 0;

            // Ensure that no vertex can be mistaken for a triangular bounding
            // box vertex in insertvertex().
            mesh.infvertex1 = null;
            mesh.infvertex2 = null;
            mesh.infvertex3 = null;

            if (behavior.useSegments)
            {
                // Segments will be introduced next.
                mesh.checksegments = true;

                // Insert PSLG segments and/or convex hull segments.
                FormSkeleton(input);
            }

            if (behavior.Poly && (mesh.triangles.Count > 0))
            {
                // Copy holes and regions
                mesh.holes.AddRange(input.Holes);
                mesh.regions.AddRange(input.Regions);

                // Carve out holes and concavities.
                CarveHoles();
            }
        }

        /// <summary>
        /// Find the holes and infect them. Find the area constraints and infect 
        /// them. Infect the convex hull. Spread the infection and kill triangles. 
        /// Spread the area constraints.
        /// </summary>
        private void CarveHoles()
        {
            Otri searchtri = default(Otri);
            Vertex searchorg, searchdest;
            LocateResult intersect;

            Triangle[] regionTris = null;

            var dummytri = mesh.dummytri;

            if (!mesh.behavior.Convex)
            {
                // Mark as infected any unprotected triangles on the boundary.
                // This is one way by which concavities are created.
                InfectHull();
            }

            if (!mesh.behavior.NoHoles)
            {
                // Infect each triangle in which a hole lies.
                foreach (var hole in mesh.holes)
                {
                    // Ignore holes that aren't within the bounds of the mesh.
                    if (mesh.bounds.Contains(hole))
                    {
                        // Start searching from some triangle on the outer boundary.
                        searchtri.tri = dummytri;
                        searchtri.orient = 0;
                        searchtri.Sym();
                        // Ensure that the hole is to the left of this boundary edge;
                        // otherwise, locate() will falsely report that the hole
                        // falls within the starting triangle.
                        searchorg = searchtri.Org();
                        searchdest = searchtri.Dest();
                        if (predicates.CounterClockwise(searchorg, searchdest, hole) > 0.0)
                        {
                            // Find a triangle that contains the hole.
                            intersect = mesh.locator.Locate(hole, ref searchtri);
                            if ((intersect != LocateResult.Outside) && (!searchtri.IsInfected()))
                            {
                                // Infect the triangle. This is done by marking the triangle
                                // as infected and including the triangle in the virus pool.
                                searchtri.Infect();
                                viri.Add(searchtri.tri);
                            }
                        }
                    }
                }
            }

            // Now, we have to find all the regions BEFORE we carve the holes, because locate() won't
            // work when the triangulation is no longer convex. (Incidentally, this is the reason why
            // regional attributes and area constraints can't be used when refining a preexisting mesh,
            // which might not be convex; they can only be used with a freshly triangulated PSLG.)
            if (mesh.regions.Count > 0)
            {
                int i = 0;

                regionTris = new Triangle[mesh.regions.Count];

                // Find the starting triangle for each region.
                foreach (var region in mesh.regions)
                {
                    regionTris[i] = dummytri;
                    // Ignore region points that aren't within the bounds of the mesh.
                    if (mesh.bounds.Contains(region.point))
                    {
                        // Start searching from some triangle on the outer boundary.
                        searchtri.tri = dummytri;
                        searchtri.orient = 0;
                        searchtri.Sym();
                        // Ensure that the region point is to the left of this boundary
                        // edge; otherwise, locate() will falsely report that the
                        // region point falls within the starting triangle.
                        searchorg = searchtri.Org();
                        searchdest = searchtri.Dest();
                        if (predicates.CounterClockwise(searchorg, searchdest, region.point) > 0.0)
                        {
                            // Find a triangle that contains the region point.
                            intersect = mesh.locator.Locate(region.point, ref searchtri);
                            if ((intersect != LocateResult.Outside) && (!searchtri.IsInfected()))
                            {
                                // Record the triangle for processing after the
                                // holes have been carved.
                                regionTris[i] = searchtri.tri;
                                regionTris[i].label = region.id;
                                regionTris[i].area = region.area;
                            }
                        }
                    }

                    i++;
                }
            }

            if (viri.Count > 0)
            {
                // Carve the holes and concavities.
                Plague();
            }

            if (regionTris != null)
            {
                var iterator = new RegionIterator(mesh);

                for (int i = 0; i < regionTris.Length; i++)
                {
                    if (regionTris[i].id != Mesh.DUMMY)
                    {
                        // Make sure the triangle under consideration still exists.
                        // It may have been eaten by the virus.
                        if (!Otri.IsDead(regionTris[i]))
                        {
                            // Apply one region's attribute and/or area constraint.
                            iterator.Process(regionTris[i]);
                        }
                    }
                }
            }

            // Free up memory (virus pool should be empty anyway).
            viri.Clear();
        }

        /// <summary>
        /// Create the segments of a triangulation, including PSLG segments and edges 
        /// on the convex hull.
        /// </summary>
        private void FormSkeleton(IPolygon input)
        {
            // The segment endpoints.
            Vertex p, q;

            mesh.insegments = 0;

            if (behavior.Poly)
            {
                // If the input vertices are collinear, there is no triangulation,
                // so don't try to insert segments.
                if (mesh.triangles.Count == 0)
                {
                    return;
                }

                // If segments are to be inserted, compute a mapping
                // from vertices to triangles.
                if (input.Segments.Count > 0)
                {
                    mesh.MakeVertexMap();
                }

                // Read and insert the segments.
                foreach (var seg in input.Segments)
                {
                    mesh.insegments++;

                    p = seg.GetVertex(0);
                    q = seg.GetVertex(1);

                    if ((p.x == q.x) && (p.y == q.y))
                    {
                        if (Log.Verbose)
                        {
                            logger.Warning("Endpoints of segment (IDs " + p.id + "/" + q.id + ") are coincident.",
                                "Mesh.FormSkeleton()");
                        }
                    }
                    else
                    {
                        InsertSegment(p, q, seg.Label);
                    }
                }
            }

            if (behavior.Convex || !behavior.Poly)
            {
                // Enclose the convex hull with subsegments.
                MarkHull();
            }
        }

        #region Carving holes

        /// <summary>
        /// Virally infect all of the triangles of the convex hull that are not 
        /// protected by subsegments. Where there are subsegments, set boundary 
        /// markers as appropriate.
        /// </summary>
        private void InfectHull()
        {
            Otri hulltri = default(Otri);
            Otri nexttri = default(Otri);
            Otri starttri = default(Otri);
            Osub hullsubseg = default(Osub);
            Vertex horg, hdest;

            var dummytri = mesh.dummytri;

            // Find a triangle handle on the hull.
            hulltri.tri = dummytri;
            hulltri.orient = 0;
            hulltri.Sym();

            // Remember where we started so we know when to stop.
            hulltri.Copy(ref starttri);
            // Go once counterclockwise around the convex hull.
            do
            {
                // Ignore triangles that are already infected.
                if (!hulltri.IsInfected())
                {
                    // Is the triangle protected by a subsegment?
                    hulltri.Pivot(ref hullsubseg);
                    if (hullsubseg.seg.hash == Mesh.DUMMY)
                    {
                        // The triangle is not protected; infect it.
                        if (!hulltri.IsInfected())
                        {
                            hulltri.Infect();
                            viri.Add(hulltri.tri);
                        }
                    }
                    else
                    {
                        // The triangle is protected; set boundary markers if appropriate.
                        if (hullsubseg.seg.boundary == 0)
                        {
                            hullsubseg.seg.boundary = 1;
                            horg = hulltri.Org();
                            hdest = hulltri.Dest();
                            if (horg.label == 0)
                            {
                                horg.label = 1;
                            }
                            if (hdest.label == 0)
                            {
                                hdest.label = 1;
                            }
                        }
                    }
                }
                // To find the next hull edge, go clockwise around the next vertex.
                hulltri.Lnext();
                hulltri.Oprev(ref nexttri);
                while (nexttri.tri.id != Mesh.DUMMY)
                {
                    nexttri.Copy(ref hulltri);
                    hulltri.Oprev(ref nexttri);
                }

            } while (!hulltri.Equals(starttri));
        }

        /// <summary>
        /// Spread the virus from all infected triangles to any neighbors not 
        /// protected by subsegments. Delete all infected triangles.
        /// </summary>
        /// <remarks>
        /// This is the procedure that actually creates holes and concavities.
        ///
        /// This procedure operates in two phases. The first phase identifies all
        /// the triangles that will die, and marks them as infected. They are
        /// marked to ensure that each triangle is added to the virus pool only
        /// once, so the procedure will terminate.
        ///
        /// The second phase actually eliminates the infected triangles. It also
        /// eliminates orphaned vertices.
        /// </remarks>
        void Plague()
        {
            Otri testtri = default(Otri);
            Otri neighbor = default(Otri);
            Osub neighborsubseg = default(Osub);
            Vertex testvertex;
            Vertex norg, ndest;

            var dummysub = mesh.dummysub;
            var dummytri = mesh.dummytri;

            bool killorg;

            // Loop through all the infected triangles, spreading the virus to
            // their neighbors, then to their neighbors' neighbors.
            for (int i = 0; i < viri.Count; i++)
            {
                // WARNING: Don't use foreach, mesh.viri list may get modified.

                testtri.tri = viri[i];
                // A triangle is marked as infected by messing with one of its pointers
                // to subsegments, setting it to an illegal value.  Hence, we have to
                // temporarily uninfect this triangle so that we can examine its
                // adjacent subsegments.
                // TODO: Not true in the C# version (so we could skip this).
                testtri.Uninfect();

                // Check each of the triangle's three neighbors.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    // Find the neighbor.
                    testtri.Sym(ref neighbor);
                    // Check for a subsegment between the triangle and its neighbor.
                    testtri.Pivot(ref neighborsubseg);
                    // Check if the neighbor is nonexistent or already infected.
                    if ((neighbor.tri.id == Mesh.DUMMY) || neighbor.IsInfected())
                    {
                        if (neighborsubseg.seg.hash != Mesh.DUMMY)
                        {
                            // There is a subsegment separating the triangle from its
                            // neighbor, but both triangles are dying, so the subsegment
                            // dies too.
                            mesh.SubsegDealloc(neighborsubseg.seg);
                            if (neighbor.tri.id != Mesh.DUMMY)
                            {
                                // Make sure the subsegment doesn't get deallocated again
                                // later when the infected neighbor is visited.
                                neighbor.Uninfect();
                                neighbor.SegDissolve(dummysub);
                                neighbor.Infect();
                            }
                        }
                    }
                    else
                    {   // The neighbor exists and is not infected.
                        if (neighborsubseg.seg.hash == Mesh.DUMMY)
                        {
                            // There is no subsegment protecting the neighbor, so
                            // the neighbor becomes infected.
                            neighbor.Infect();
                            // Ensure that the neighbor's neighbors will be infected.
                            viri.Add(neighbor.tri);
                        }
                        else
                        {
                            // The neighbor is protected by a subsegment.
                            // Remove this triangle from the subsegment.
                            neighborsubseg.TriDissolve(dummytri);
                            // The subsegment becomes a boundary.  Set markers accordingly.
                            if (neighborsubseg.seg.boundary == 0)
                            {
                                neighborsubseg.seg.boundary = 1;
                            }
                            norg = neighbor.Org();
                            ndest = neighbor.Dest();
                            if (norg.label == 0)
                            {
                                norg.label = 1;
                            }
                            if (ndest.label == 0)
                            {
                                ndest.label = 1;
                            }
                        }
                    }
                }
                // Remark the triangle as infected, so it doesn't get added to the
                // virus pool again.
                testtri.Infect();
            }

            foreach (var virus in viri)
            {
                testtri.tri = virus;

                // Check each of the three corners of the triangle for elimination.
                // This is done by walking around each vertex, checking if it is
                // still connected to at least one live triangle.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    testvertex = testtri.Org();
                    // Check if the vertex has already been tested.
                    if (testvertex != null)
                    {
                        killorg = true;
                        // Mark the corner of the triangle as having been tested.
                        testtri.SetOrg(null);
                        // Walk counterclockwise about the vertex.
                        testtri.Onext(ref neighbor);
                        // Stop upon reaching a boundary or the starting triangle.
                        while ((neighbor.tri.id != Mesh.DUMMY) &&
                               (!neighbor.Equals(testtri)))
                        {
                            if (neighbor.IsInfected())
                            {
                                // Mark the corner of this triangle as having been tested.
                                neighbor.SetOrg(null);
                            }
                            else
                            {
                                // A live triangle.  The vertex survives.
                                killorg = false;
                            }
                            // Walk counterclockwise about the vertex.
                            neighbor.Onext();
                        }
                        // If we reached a boundary, we must walk clockwise as well.
                        if (neighbor.tri.id == Mesh.DUMMY)
                        {
                            // Walk clockwise about the vertex.
                            testtri.Oprev(ref neighbor);
                            // Stop upon reaching a boundary.
                            while (neighbor.tri.id != Mesh.DUMMY)
                            {
                                if (neighbor.IsInfected())
                                {
                                    // Mark the corner of this triangle as having been tested.
                                    neighbor.SetOrg(null);
                                }
                                else
                                {
                                    // A live triangle.  The vertex survives.
                                    killorg = false;
                                }
                                // Walk clockwise about the vertex.
                                neighbor.Oprev();
                            }
                        }
                        if (killorg)
                        {
                            // Deleting vertex
                            testvertex.type = VertexType.UndeadVertex;
                            mesh.undeads++;
                        }
                    }
                }

                // Record changes in the number of boundary edges, and disconnect
                // dead triangles from their neighbors.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    testtri.Sym(ref neighbor);
                    if (neighbor.tri.id == Mesh.DUMMY)
                    {
                        // There is no neighboring triangle on this edge, so this edge
                        // is a boundary edge. This triangle is being deleted, so this
                        // boundary edge is deleted.
                        mesh.hullsize--;
                    }
                    else
                    {
                        // Disconnect the triangle from its neighbor.
                        neighbor.Dissolve(dummytri);
                        // There is a neighboring triangle on this edge, so this edge
                        // becomes a boundary edge when this triangle is deleted.
                        mesh.hullsize++;
                    }
                }
                // Return the dead triangle to the pool of triangles.
                mesh.TriangleDealloc(testtri.tri);
            }

            // Empty the virus pool.
            viri.Clear();
        }

        #endregion

        #region Segment insertion

        /// <summary>
        /// Find the first triangle on the path from one point to another.
        /// </summary>
        /// <param name="searchtri"></param>
        /// <param name="searchpoint"></param>
        /// <returns>
        /// The return value notes whether the destination or apex of the found
        /// triangle is collinear with the two points in question.</returns>
        /// <remarks>
        /// Finds the triangle that intersects a line segment drawn from the
        /// origin of 'searchtri' to the point 'searchpoint', and returns the result
        /// in 'searchtri'. The origin of 'searchtri' does not change, even though
        /// the triangle returned may differ from the one passed in. This routine
        /// is used to find the direction to move in to get from one point to
        /// another.
        /// </remarks>
        private FindDirectionResult FindDirection(ref Otri searchtri, Vertex searchpoint)
        {
            Otri checktri = default(Otri);
            Vertex startvertex;
            Vertex leftvertex, rightvertex;
            double leftccw, rightccw;
            bool leftflag, rightflag;

            startvertex = searchtri.Org();
            rightvertex = searchtri.Dest();
            leftvertex = searchtri.Apex();
            // Is 'searchpoint' to the left?
            leftccw = predicates.CounterClockwise(searchpoint, startvertex, leftvertex);
            leftflag = leftccw > 0.0;
            // Is 'searchpoint' to the right?
            rightccw = predicates.CounterClockwise(startvertex, searchpoint, rightvertex);
            rightflag = rightccw > 0.0;
            if (leftflag && rightflag)
            {
                // 'searchtri' faces directly away from 'searchpoint'. We could go left
                // or right. Ask whether it's a triangle or a boundary on the left.
                searchtri.Onext(ref checktri);
                if (checktri.tri.id == Mesh.DUMMY)
                {
                    leftflag = false;
                }
                else
                {
                    rightflag = false;
                }
            }
            while (leftflag)
            {
                // Turn left until satisfied.
                searchtri.Onext();
                if (searchtri.tri.id == Mesh.DUMMY)
                {
                    logger.Error("Unable to find a triangle on path.", "Mesh.FindDirection().1");
                    throw new Exception("Unable to find a triangle on path.");
                }
                leftvertex = searchtri.Apex();
                rightccw = leftccw;
                leftccw = predicates.CounterClockwise(searchpoint, startvertex, leftvertex);
                leftflag = leftccw > 0.0;
            }
            while (rightflag)
            {
                // Turn right until satisfied.
                searchtri.Oprev();
                if (searchtri.tri.id == Mesh.DUMMY)
                {
                    logger.Error("Unable to find a triangle on path.", "Mesh.FindDirection().2");
                    throw new Exception("Unable to find a triangle on path.");
                }
                rightvertex = searchtri.Dest();
                leftccw = rightccw;
                rightccw = predicates.CounterClockwise(startvertex, searchpoint, rightvertex);
                rightflag = rightccw > 0.0;
            }
            if (leftccw == 0.0)
            {
                return FindDirectionResult.Leftcollinear;
            }
            else if (rightccw == 0.0)
            {
                return FindDirectionResult.Rightcollinear;
            }
            else
            {
                return FindDirectionResult.Within;
            }
        }

        /// <summary>
        /// Find the intersection of an existing segment and a segment that is being 
        /// inserted. Insert a vertex at the intersection, splitting an existing subsegment.
        /// </summary>
        /// <param name="splittri"></param>
        /// <param name="splitsubseg"></param>
        /// <param name="endpoint2"></param>
        /// <remarks>
        /// The segment being inserted connects the apex of splittri to endpoint2.
        /// splitsubseg is the subsegment being split, and MUST adjoin splittri.
        /// Hence, endpoints of the subsegment being split are the origin and
        /// destination of splittri.
        ///
        /// On completion, splittri is a handle having the newly inserted
        /// intersection point as its origin, and endpoint1 as its destination.
        /// </remarks>
        private void SegmentIntersection(ref Otri splittri, ref Osub splitsubseg, Vertex endpoint2)
        {
            Osub opposubseg = default(Osub);
            Vertex endpoint1;
            Vertex torg, tdest;
            Vertex leftvertex, rightvertex;
            Vertex newvertex;
            InsertVertexResult success;

            var dummysub = mesh.dummysub;

            double ex, ey;
            double tx, ty;
            double etx, ety;
            double split, denom;

            // Find the other three segment endpoints.
            endpoint1 = splittri.Apex();
            torg = splittri.Org();
            tdest = splittri.Dest();
            // Segment intersection formulae; see the Antonio reference.
            tx = tdest.x - torg.x;
            ty = tdest.y - torg.y;
            ex = endpoint2.x - endpoint1.x;
            ey = endpoint2.y - endpoint1.y;
            etx = torg.x - endpoint2.x;
            ety = torg.y - endpoint2.y;
            denom = ty * ex - tx * ey;
            if (denom == 0.0)
            {
                logger.Error("Attempt to find intersection of parallel segments.",
                    "Mesh.SegmentIntersection()");
                throw new Exception("Attempt to find intersection of parallel segments.");
            }
            split = (ey * etx - ex * ety) / denom;

            // Create the new vertex.
            newvertex = new Vertex(
                torg.x + split * (tdest.x - torg.x),
                torg.y + split * (tdest.y - torg.y),
                splitsubseg.seg.boundary
#if USE_ATTRIBS
                , mesh.nextras
#endif
                );

            newvertex.hash = mesh.hash_vtx++;
            newvertex.id = newvertex.hash;

#if USE_ATTRIBS
            // Interpolate its attributes.
            for (int i = 0; i < mesh.nextras; i++)
            {
                newvertex.attributes[i] = torg.attributes[i] + split * (tdest.attributes[i] - torg.attributes[i]);
            }
#endif
#if USE_Z
            newvertex.z = torg.z + split * (tdest.z - torg.z);
#endif

            mesh.vertices.Add(newvertex.hash, newvertex);

            // Insert the intersection vertex.  This should always succeed.
            success = mesh.InsertVertex(newvertex, ref splittri, ref splitsubseg, false, false);
            if (success != InsertVertexResult.Successful)
            {
                logger.Error("Failure to split a segment.", "Mesh.SegmentIntersection()");
                throw new Exception("Failure to split a segment.");
            }
            // Record a triangle whose origin is the new vertex.
            newvertex.tri = splittri;
            if (mesh.steinerleft > 0)
            {
                mesh.steinerleft--;
            }

            // Divide the segment into two, and correct the segment endpoints.
            splitsubseg.Sym();
            splitsubseg.Pivot(ref opposubseg);
            splitsubseg.Dissolve(dummysub);
            opposubseg.Dissolve(dummysub);
            do
            {
                splitsubseg.SetSegOrg(newvertex);
                splitsubseg.Next();
            } while (splitsubseg.seg.hash != Mesh.DUMMY);
            do
            {
                opposubseg.SetSegOrg(newvertex);
                opposubseg.Next();
            } while (opposubseg.seg.hash != Mesh.DUMMY);

            // Inserting the vertex may have caused edge flips.  We wish to rediscover
            // the edge connecting endpoint1 to the new intersection vertex.
            FindDirection(ref splittri, endpoint1);

            rightvertex = splittri.Dest();
            leftvertex = splittri.Apex();
            if ((leftvertex.x == endpoint1.x) && (leftvertex.y == endpoint1.y))
            {
                splittri.Onext();
            }
            else if ((rightvertex.x != endpoint1.x) || (rightvertex.y != endpoint1.y))
            {
                logger.Error("Topological inconsistency after splitting a segment.", "Mesh.SegmentIntersection()");
                throw new Exception("Topological inconsistency after splitting a segment.");
            }
            // 'splittri' should have destination endpoint1.
        }

        /// <summary>
        /// Scout the first triangle on the path from one endpoint to another, and check 
        /// for completion (reaching the second endpoint), a collinear vertex, or the 
        /// intersection of two segments.
        /// </summary>
        /// <param name="searchtri"></param>
        /// <param name="endpoint2"></param>
        /// <param name="newmark"></param>
        /// <returns>Returns true if the entire segment is successfully inserted, and false 
        /// if the job must be finished by ConstrainedEdge().</returns>
        /// <remarks>
        /// If the first triangle on the path has the second endpoint as its
        /// destination or apex, a subsegment is inserted and the job is done.
        ///
        /// If the first triangle on the path has a destination or apex that lies on
        /// the segment, a subsegment is inserted connecting the first endpoint to
        /// the collinear vertex, and the search is continued from the collinear
        /// vertex.
        ///
        /// If the first triangle on the path has a subsegment opposite its origin,
        /// then there is a segment that intersects the segment being inserted.
        /// Their intersection vertex is inserted, splitting the subsegment.
        /// </remarks>
        private bool ScoutSegment(ref Otri searchtri, Vertex endpoint2, int newmark)
        {
            Otri crosstri = default(Otri);
            Osub crosssubseg = default(Osub);
            Vertex leftvertex, rightvertex;
            FindDirectionResult collinear;

            collinear = FindDirection(ref searchtri, endpoint2);
            rightvertex = searchtri.Dest();
            leftvertex = searchtri.Apex();
            if (((leftvertex.x == endpoint2.x) && (leftvertex.y == endpoint2.y)) ||
                ((rightvertex.x == endpoint2.x) && (rightvertex.y == endpoint2.y)))
            {
                // The segment is already an edge in the mesh.
                if ((leftvertex.x == endpoint2.x) && (leftvertex.y == endpoint2.y))
                {
                    searchtri.Lprev();
                }
                // Insert a subsegment, if there isn't already one there.
                mesh.InsertSubseg(ref searchtri, newmark);
                return true;
            }
            else if (collinear == FindDirectionResult.Leftcollinear)
            {
                // We've collided with a vertex between the segment's endpoints.
                // Make the collinear vertex be the triangle's origin.
                searchtri.Lprev();
                mesh.InsertSubseg(ref searchtri, newmark);
                // Insert the remainder of the segment.
                return ScoutSegment(ref searchtri, endpoint2, newmark);
            }
            else if (collinear == FindDirectionResult.Rightcollinear)
            {
                // We've collided with a vertex between the segment's endpoints.
                mesh.InsertSubseg(ref searchtri, newmark);
                // Make the collinear vertex be the triangle's origin.
                searchtri.Lnext();
                // Insert the remainder of the segment.
                return ScoutSegment(ref searchtri, endpoint2, newmark);
            }
            else
            {
                searchtri.Lnext(ref crosstri);
                crosstri.Pivot(ref crosssubseg);
                // Check for a crossing segment.
                if (crosssubseg.seg.hash == Mesh.DUMMY)
                {
                    return false;
                }
                else
                {
                    // Insert a vertex at the intersection.
                    SegmentIntersection(ref crosstri, ref crosssubseg, endpoint2);
                    crosstri.Copy(ref searchtri);
                    mesh.InsertSubseg(ref searchtri, newmark);
                    // Insert the remainder of the segment.
                    return ScoutSegment(ref searchtri, endpoint2, newmark);
                }
            }
        }

        /// <summary>
        /// Enforce the Delaunay condition at an edge, fanning out recursively from 
        /// an existing vertex. Pay special attention to stacking inverted triangles.
        /// </summary>
        /// <param name="fixuptri"></param>
        /// <param name="leftside">Indicates whether or not fixuptri is to the left of 
        /// the segment being inserted. (Imagine that the segment is pointing up from
        /// endpoint1 to endpoint2.)</param>
        /// <remarks>
        /// This is a support routine for inserting segments into a constrained
        /// Delaunay triangulation.
        ///
        /// The origin of fixuptri is treated as if it has just been inserted, and
        /// the local Delaunay condition needs to be enforced. It is only enforced
        /// in one sector, however, that being the angular range defined by
        /// fixuptri.
        ///
        /// This routine also needs to make decisions regarding the "stacking" of
        /// triangles. (Read the description of ConstrainedEdge() below before
        /// reading on here, so you understand the algorithm.) If the position of
        /// the new vertex (the origin of fixuptri) indicates that the vertex before
        /// it on the polygon is a reflex vertex, then "stack" the triangle by
        /// doing nothing.  (fixuptri is an inverted triangle, which is how stacked
        /// triangles are identified.)
        ///
        /// Otherwise, check whether the vertex before that was a reflex vertex.
        /// If so, perform an edge flip, thereby eliminating an inverted triangle
        /// (popping it off the stack). The edge flip may result in the creation
        /// of a new inverted triangle, depending on whether or not the new vertex
        /// is visible to the vertex three edges behind on the polygon.
        ///
        /// If neither of the two vertices behind the new vertex are reflex
        /// vertices, fixuptri and fartri, the triangle opposite it, are not
        /// inverted; hence, ensure that the edge between them is locally Delaunay.
        /// </remarks>
        private void DelaunayFixup(ref Otri fixuptri, bool leftside)
        {
            Otri neartri = default(Otri);
            Otri fartri = default(Otri);
            Osub faredge = default(Osub);
            Vertex nearvertex, leftvertex, rightvertex, farvertex;

            fixuptri.Lnext(ref neartri);
            neartri.Sym(ref fartri);
            // Check if the edge opposite the origin of fixuptri can be flipped.
            if (fartri.tri.id == Mesh.DUMMY)
            {
                return;
            }
            neartri.Pivot(ref faredge);
            if (faredge.seg.hash != Mesh.DUMMY)
            {
                return;
            }
            // Find all the relevant vertices.
            nearvertex = neartri.Apex();
            leftvertex = neartri.Org();
            rightvertex = neartri.Dest();
            farvertex = fartri.Apex();
            // Check whether the previous polygon vertex is a reflex vertex.
            if (leftside)
            {
                if (predicates.CounterClockwise(nearvertex, leftvertex, farvertex) <= 0.0)
                {
                    // leftvertex is a reflex vertex too. Nothing can
                    // be done until a convex section is found.
                    return;
                }
            }
            else
            {
                if (predicates.CounterClockwise(farvertex, rightvertex, nearvertex) <= 0.0)
                {
                    // rightvertex is a reflex vertex too.  Nothing can
                    // be done until a convex section is found.
                    return;
                }
            }
            if (predicates.CounterClockwise(rightvertex, leftvertex, farvertex) > 0.0)
            {
                // fartri is not an inverted triangle, and farvertex is not a reflex
                // vertex.  As there are no reflex vertices, fixuptri isn't an
                // inverted triangle, either.  Hence, test the edge between the
                // triangles to ensure it is locally Delaunay.
                if (predicates.InCircle(leftvertex, farvertex, rightvertex, nearvertex) <= 0.0)
                {
                    return;
                }
                // Not locally Delaunay; go on to an edge flip.
            }
            // else fartri is inverted; remove it from the stack by flipping.
            mesh.Flip(ref neartri);
            fixuptri.Lprev();    // Restore the origin of fixuptri after the flip.
            // Recursively process the two triangles that result from the flip.
            DelaunayFixup(ref fixuptri, leftside);
            DelaunayFixup(ref fartri, leftside);
        }

        /// <summary>
        /// Force a segment into a constrained Delaunay triangulation by deleting the 
        /// triangles it intersects, and triangulating the polygons that form on each 
        /// side of it.
        /// </summary>
        /// <param name="starttri"></param>
        /// <param name="endpoint2"></param>
        /// <param name="newmark"></param>
        /// <remarks>
        /// Generates a single subsegment connecting 'endpoint1' to 'endpoint2'.
        /// The triangle 'starttri' has 'endpoint1' as its origin.  'newmark' is the
        /// boundary marker of the segment.
        ///
        /// To insert a segment, every triangle whose interior intersects the
        /// segment is deleted. The union of these deleted triangles is a polygon
        /// (which is not necessarily monotone, but is close enough), which is
        /// divided into two polygons by the new segment. This routine's task is
        /// to generate the Delaunay triangulation of these two polygons.
        ///
        /// You might think of this routine's behavior as a two-step process.  The
        /// first step is to walk from endpoint1 to endpoint2, flipping each edge
        /// encountered.  This step creates a fan of edges connected to endpoint1,
        /// including the desired edge to endpoint2. The second step enforces the
        /// Delaunay condition on each side of the segment in an incremental manner:
        /// proceeding along the polygon from endpoint1 to endpoint2 (this is done
        /// independently on each side of the segment), each vertex is "enforced"
        /// as if it had just been inserted, but affecting only the previous
        /// vertices. The result is the same as if the vertices had been inserted
        /// in the order they appear on the polygon, so the result is Delaunay.
        ///
        /// In truth, ConstrainedEdge() interleaves these two steps. The procedure
        /// walks from endpoint1 to endpoint2, and each time an edge is encountered
        /// and flipped, the newly exposed vertex (at the far end of the flipped
        /// edge) is "enforced" upon the previously flipped edges, usually affecting
        /// only one side of the polygon (depending upon which side of the segment
        /// the vertex falls on).
        ///
        /// The algorithm is complicated by the need to handle polygons that are not
        /// convex.  Although the polygon is not necessarily monotone, it can be
        /// triangulated in a manner similar to the stack-based algorithms for
        /// monotone polygons. For each reflex vertex (local concavity) of the
        /// polygon, there will be an inverted triangle formed by one of the edge
        /// flips. (An inverted triangle is one with negative area - that is, its
        /// vertices are arranged in clockwise order - and is best thought of as a
        /// wrinkle in the fabric of the mesh.)  Each inverted triangle can be
        /// thought of as a reflex vertex pushed on the stack, waiting to be fixed
        /// later.
        ///
        /// A reflex vertex is popped from the stack when a vertex is inserted that
        /// is visible to the reflex vertex. (However, if the vertex behind the
        /// reflex vertex is not visible to the reflex vertex, a new inverted
        /// triangle will take its place on the stack.) These details are handled
        /// by the DelaunayFixup() routine above.
        /// </remarks>
        private void ConstrainedEdge(ref Otri starttri, Vertex endpoint2, int newmark)
        {
            Otri fixuptri = default(Otri), fixuptri2 = default(Otri);
            Osub crosssubseg = default(Osub);
            Vertex endpoint1;
            Vertex farvertex;
            double area;
            bool collision;
            bool done;

            endpoint1 = starttri.Org();
            starttri.Lnext(ref fixuptri);
            mesh.Flip(ref fixuptri);
            // 'collision' indicates whether we have found a vertex directly
            // between endpoint1 and endpoint2.
            collision = false;
            done = false;
            do
            {
                farvertex = fixuptri.Org();
                // 'farvertex' is the extreme point of the polygon we are "digging"
                //  to get from endpoint1 to endpoint2.
                if ((farvertex.x == endpoint2.x) && (farvertex.y == endpoint2.y))
                {
                    fixuptri.Oprev(ref fixuptri2);
                    // Enforce the Delaunay condition around endpoint2.
                    DelaunayFixup(ref fixuptri, false);
                    DelaunayFixup(ref fixuptri2, true);
                    done = true;
                }
                else
                {
                    // Check whether farvertex is to the left or right of the segment being
                    // inserted, to decide which edge of fixuptri to dig through next.
                    area = predicates.CounterClockwise(endpoint1, endpoint2, farvertex);
                    if (area == 0.0)
                    {
                        // We've collided with a vertex between endpoint1 and endpoint2.
                        collision = true;
                        fixuptri.Oprev(ref fixuptri2);
                        // Enforce the Delaunay condition around farvertex.
                        DelaunayFixup(ref fixuptri, false);
                        DelaunayFixup(ref fixuptri2, true);
                        done = true;
                    }
                    else
                    {
                        if (area > 0.0)
                        {
                            // farvertex is to the left of the segment.
                            fixuptri.Oprev(ref fixuptri2);
                            // Enforce the Delaunay condition around farvertex, on the
                            // left side of the segment only.
                            DelaunayFixup(ref fixuptri2, true);
                            // Flip the edge that crosses the segment. After the edge is
                            // flipped, one of its endpoints is the fan vertex, and the
                            // destination of fixuptri is the fan vertex.
                            fixuptri.Lprev();
                        }
                        else
                        {
                            // farvertex is to the right of the segment.
                            DelaunayFixup(ref fixuptri, false);
                            // Flip the edge that crosses the segment. After the edge is
                            // flipped, one of its endpoints is the fan vertex, and the
                            // destination of fixuptri is the fan vertex.
                            fixuptri.Oprev();
                        }
                        // Check for two intersecting segments.
                        fixuptri.Pivot(ref crosssubseg);
                        if (crosssubseg.seg.hash == Mesh.DUMMY)
                        {
                            mesh.Flip(ref fixuptri);    // May create inverted triangle at left.
                        }
                        else
                        {
                            // We've collided with a segment between endpoint1 and endpoint2.
                            collision = true;
                            // Insert a vertex at the intersection.
                            SegmentIntersection(ref fixuptri, ref crosssubseg, endpoint2);
                            done = true;
                        }
                    }
                }
            } while (!done);
            // Insert a subsegment to make the segment permanent.
            mesh.InsertSubseg(ref fixuptri, newmark);
            // If there was a collision with an interceding vertex, install another
            // segment connecting that vertex with endpoint2.
            if (collision)
            {
                // Insert the remainder of the segment.
                if (!ScoutSegment(ref fixuptri, endpoint2, newmark))
                {
                    ConstrainedEdge(ref fixuptri, endpoint2, newmark);
                }
            }
        }

        /// <summary>
        /// Insert a PSLG segment into a triangulation.
        /// </summary>
        /// <param name="endpoint1"></param>
        /// <param name="endpoint2"></param>
        /// <param name="newmark"></param>
        private void InsertSegment(Vertex endpoint1, Vertex endpoint2, int newmark)
        {
            Otri searchtri1 = default(Otri), searchtri2 = default(Otri);
            Vertex checkvertex = null;

            var dummytri = mesh.dummytri;

            // Find a triangle whose origin is the segment's first endpoint.
            searchtri1 = endpoint1.tri;
            if (searchtri1.tri != null)
            {
                checkvertex = searchtri1.Org();
            }

            if (checkvertex != endpoint1)
            {
                // Find a boundary triangle to search from.
                searchtri1.tri = dummytri;
                searchtri1.orient = 0;
                searchtri1.Sym();
                // Search for the segment's first endpoint by point location.
                if (locator.Locate(endpoint1, ref searchtri1) != LocateResult.OnVertex)
                {
                    logger.Error("Unable to locate PSLG vertex in triangulation.", "Mesh.InsertSegment().1");
                    throw new Exception("Unable to locate PSLG vertex in triangulation.");
                }
            }
            // Remember this triangle to improve subsequent point location.
            locator.Update(ref searchtri1);

            // Scout the beginnings of a path from the first endpoint
            // toward the second.
            if (ScoutSegment(ref searchtri1, endpoint2, newmark))
            {
                // The segment was easily inserted.
                return;
            }
            // The first endpoint may have changed if a collision with an intervening
            // vertex on the segment occurred.
            endpoint1 = searchtri1.Org();

            // Find a triangle whose origin is the segment's second endpoint.
            checkvertex = null;
            searchtri2 = endpoint2.tri;
            if (searchtri2.tri != null)
            {
                checkvertex = searchtri2.Org();
            }
            if (checkvertex != endpoint2)
            {
                // Find a boundary triangle to search from.
                searchtri2.tri = dummytri;
                searchtri2.orient = 0;
                searchtri2.Sym();
                // Search for the segment's second endpoint by point location.
                if (locator.Locate(endpoint2, ref searchtri2) != LocateResult.OnVertex)
                {
                    logger.Error("Unable to locate PSLG vertex in triangulation.", "Mesh.InsertSegment().2");
                    throw new Exception("Unable to locate PSLG vertex in triangulation.");
                }
            }
            // Remember this triangle to improve subsequent point location.
            locator.Update(ref searchtri2);
            // Scout the beginnings of a path from the second endpoint
            // toward the first.
            if (ScoutSegment(ref searchtri2, endpoint1, newmark))
            {
                // The segment was easily inserted.
                return;
            }
            // The second endpoint may have changed if a collision with an intervening
            // vertex on the segment occurred.
            endpoint2 = searchtri2.Org();

            // Insert the segment directly into the triangulation.
            ConstrainedEdge(ref searchtri1, endpoint2, newmark);
        }

        /// <summary>
        /// Cover the convex hull of a triangulation with subsegments.
        /// </summary>
        private void MarkHull()
        {
            Otri hulltri = default(Otri);
            Otri nexttri = default(Otri);
            Otri starttri = default(Otri);

            // Find a triangle handle on the hull.
            hulltri.tri = mesh.dummytri;
            hulltri.orient = 0;
            hulltri.Sym();
            // Remember where we started so we know when to stop.
            hulltri.Copy(ref starttri);
            // Go once counterclockwise around the convex hull.
            do
            {
                // Create a subsegment if there isn't already one here.
                mesh.InsertSubseg(ref hulltri, 1);
                // To find the next hull edge, go clockwise around the next vertex.
                hulltri.Lnext();
                hulltri.Oprev(ref nexttri);
                while (nexttri.tri.id != Mesh.DUMMY)
                {
                    nexttri.Copy(ref hulltri);
                    hulltri.Oprev(ref nexttri);
                }
            } while (!hulltri.Equals(starttri));
        }

        #endregion
    }
}
