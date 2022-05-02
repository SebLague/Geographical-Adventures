// -----------------------------------------------------------------------
// <copyright file="SweepLine.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing.Algorithm
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;

    /// <summary>
    /// Builds a delaunay triangulation using the sweepline algorithm.
    /// </summary>
    public class SweepLine : ITriangulator
    {
        static int randomseed = 1;
        static int SAMPLERATE = 10;

        static int randomnation(int choices)
        {
            randomseed = (randomseed * 1366 + 150889) % 714025;
            return randomseed / (714025 / choices + 1);
        }

        IPredicates predicates;

        Mesh mesh;
        double xminextreme; // Nonexistent x value used as a flag in sweepline.
        List<SplayNode> splaynodes;

        public IMesh Triangulate(IList<Vertex> points, Configuration config)
        {
            this.predicates = config.Predicates();

            this.mesh = new Mesh(config);
            this.mesh.TransferNodes(points);

            // Nonexistent x value used as a flag to mark circle events in sweepline
            // Delaunay algorithm.
            xminextreme = 10 * mesh.bounds.Left - 9 * mesh.bounds.Right;

            SweepEvent[] eventheap;

            SweepEvent nextevent;
            SweepEvent newevent;
            SplayNode splayroot;
            Otri bottommost = default(Otri);
            Otri searchtri = default(Otri);
            Otri fliptri;
            Otri lefttri = default(Otri);
            Otri righttri = default(Otri);
            Otri farlefttri = default(Otri);
            Otri farrighttri = default(Otri);
            Otri inserttri = default(Otri);
            Vertex firstvertex, secondvertex;
            Vertex nextvertex, lastvertex;
            Vertex connectvertex;
            Vertex leftvertex, midvertex, rightvertex;
            double lefttest, righttest;
            int heapsize;
            bool check4events, farrightflag = false;

            splaynodes = new List<SplayNode>();
            splayroot = null;

            heapsize = points.Count;
            CreateHeap(out eventheap, heapsize);//, out events, out freeevents);

            mesh.MakeTriangle(ref lefttri);
            mesh.MakeTriangle(ref righttri);
            lefttri.Bond(ref righttri);
            lefttri.Lnext();
            righttri.Lprev();
            lefttri.Bond(ref righttri);
            lefttri.Lnext();
            righttri.Lprev();
            lefttri.Bond(ref righttri);
            firstvertex = eventheap[0].vertexEvent;

            HeapDelete(eventheap, heapsize, 0);
            heapsize--;
            do
            {
                if (heapsize == 0)
                {
                    Log.Instance.Error("Input vertices are all identical.", "SweepLine.Triangulate()");
                    throw new Exception("Input vertices are all identical.");
                }
                secondvertex = eventheap[0].vertexEvent;
                HeapDelete(eventheap, heapsize, 0);
                heapsize--;
                if ((firstvertex.x == secondvertex.x) &&
                    (firstvertex.y == secondvertex.y))
                {
                    if (Log.Verbose)
                    {
                        Log.Instance.Warning("A duplicate vertex appeared and was ignored (ID " + secondvertex.id + ").",
                            "SweepLine.Triangulate().1");
                    }
                    secondvertex.type = VertexType.UndeadVertex;
                    mesh.undeads++;
                }
            } while ((firstvertex.x == secondvertex.x) &&
                     (firstvertex.y == secondvertex.y));
            lefttri.SetOrg(firstvertex);
            lefttri.SetDest(secondvertex);
            righttri.SetOrg(secondvertex);
            righttri.SetDest(firstvertex);
            lefttri.Lprev(ref bottommost);
            lastvertex = secondvertex;

            while (heapsize > 0)
            {
                nextevent = eventheap[0];
                HeapDelete(eventheap, heapsize, 0);
                heapsize--;
                check4events = true;
                if (nextevent.xkey < mesh.bounds.Left)
                {
                    fliptri = nextevent.otriEvent;
                    fliptri.Oprev(ref farlefttri);
                    Check4DeadEvent(ref farlefttri, eventheap, ref heapsize);
                    fliptri.Onext(ref farrighttri);
                    Check4DeadEvent(ref farrighttri, eventheap, ref heapsize);

                    if (farlefttri.Equals(bottommost))
                    {
                        fliptri.Lprev(ref bottommost);
                    }
                    mesh.Flip(ref fliptri);
                    fliptri.SetApex(null);
                    fliptri.Lprev(ref lefttri);
                    fliptri.Lnext(ref righttri);
                    lefttri.Sym(ref farlefttri);

                    if (randomnation(SAMPLERATE) == 0)
                    {
                        fliptri.Sym();
                        leftvertex = fliptri.Dest();
                        midvertex = fliptri.Apex();
                        rightvertex = fliptri.Org();
                        splayroot = CircleTopInsert(splayroot, lefttri, leftvertex, midvertex, rightvertex, nextevent.ykey);
                    }
                }
                else
                {
                    nextvertex = nextevent.vertexEvent;
                    if ((nextvertex.x == lastvertex.x) &&
                        (nextvertex.y == lastvertex.y))
                    {
                        if (Log.Verbose)
                        {
                            Log.Instance.Warning("A duplicate vertex appeared and was ignored (ID " + nextvertex.id + ").",
                                "SweepLine.Triangulate().2");
                        }
                        nextvertex.type = VertexType.UndeadVertex;
                        mesh.undeads++;
                        check4events = false;
                    }
                    else
                    {
                        lastvertex = nextvertex;

                        splayroot = FrontLocate(splayroot, bottommost, nextvertex, ref searchtri, ref farrightflag);

                        //bottommost.Copy(ref searchtri);
                        //farrightflag = false;
                        //while (!farrightflag && RightOfHyperbola(ref searchtri, nextvertex))
                        //{
                        //    searchtri.OnextSelf();
                        //    farrightflag = searchtri.Equal(bottommost);
                        //}

                        Check4DeadEvent(ref searchtri, eventheap, ref heapsize);

                        searchtri.Copy(ref farrighttri);
                        searchtri.Sym(ref farlefttri);
                        mesh.MakeTriangle(ref lefttri);
                        mesh.MakeTriangle(ref righttri);
                        connectvertex = farrighttri.Dest();
                        lefttri.SetOrg(connectvertex);
                        lefttri.SetDest(nextvertex);
                        righttri.SetOrg(nextvertex);
                        righttri.SetDest(connectvertex);
                        lefttri.Bond(ref righttri);
                        lefttri.Lnext();
                        righttri.Lprev();
                        lefttri.Bond(ref righttri);
                        lefttri.Lnext();
                        righttri.Lprev();
                        lefttri.Bond(ref farlefttri);
                        righttri.Bond(ref farrighttri);
                        if (!farrightflag && farrighttri.Equals(bottommost))
                        {
                            lefttri.Copy(ref bottommost);
                        }

                        if (randomnation(SAMPLERATE) == 0)
                        {
                            splayroot = SplayInsert(splayroot, lefttri, nextvertex);
                        }
                        else if (randomnation(SAMPLERATE) == 0)
                        {
                            righttri.Lnext(ref inserttri);
                            splayroot = SplayInsert(splayroot, inserttri, nextvertex);
                        }
                    }
                }

                if (check4events)
                {
                    leftvertex = farlefttri.Apex();
                    midvertex = lefttri.Dest();
                    rightvertex = lefttri.Apex();
                    lefttest = predicates.CounterClockwise(leftvertex, midvertex, rightvertex);
                    if (lefttest > 0.0)
                    {
                        newevent = new SweepEvent();

                        newevent.xkey = xminextreme;
                        newevent.ykey = CircleTop(leftvertex, midvertex, rightvertex, lefttest);
                        newevent.otriEvent = lefttri;
                        HeapInsert(eventheap, heapsize, newevent);
                        heapsize++;
                        lefttri.SetOrg(new SweepEventVertex(newevent));
                    }
                    leftvertex = righttri.Apex();
                    midvertex = righttri.Org();
                    rightvertex = farrighttri.Apex();
                    righttest = predicates.CounterClockwise(leftvertex, midvertex, rightvertex);
                    if (righttest > 0.0)
                    {
                        newevent = new SweepEvent();

                        newevent.xkey = xminextreme;
                        newevent.ykey = CircleTop(leftvertex, midvertex, rightvertex, righttest);
                        newevent.otriEvent = farrighttri;
                        HeapInsert(eventheap, heapsize, newevent);
                        heapsize++;
                        farrighttri.SetOrg(new SweepEventVertex(newevent));
                    }
                }
            }

            splaynodes.Clear();
            bottommost.Lprev();

            this.mesh.hullsize = RemoveGhosts(ref bottommost);

            return this.mesh;
        }

        #region Heap

        void HeapInsert(SweepEvent[] heap, int heapsize, SweepEvent newevent)
        {
            double eventx, eventy;
            int eventnum;
            int parent;
            bool notdone;

            eventx = newevent.xkey;
            eventy = newevent.ykey;
            eventnum = heapsize;
            notdone = eventnum > 0;
            while (notdone)
            {
                parent = (eventnum - 1) >> 1;
                if ((heap[parent].ykey < eventy) ||
                    ((heap[parent].ykey == eventy)
                     && (heap[parent].xkey <= eventx)))
                {
                    notdone = false;
                }
                else
                {
                    heap[eventnum] = heap[parent];
                    heap[eventnum].heapposition = eventnum;

                    eventnum = parent;
                    notdone = eventnum > 0;
                }
            }
            heap[eventnum] = newevent;
            newevent.heapposition = eventnum;
        }

        void Heapify(SweepEvent[] heap, int heapsize, int eventnum)
        {
            SweepEvent thisevent;
            double eventx, eventy;
            int leftchild, rightchild;
            int smallest;
            bool notdone;

            thisevent = heap[eventnum];
            eventx = thisevent.xkey;
            eventy = thisevent.ykey;
            leftchild = 2 * eventnum + 1;
            notdone = leftchild < heapsize;
            while (notdone)
            {
                if ((heap[leftchild].ykey < eventy) ||
                    ((heap[leftchild].ykey == eventy)
                     && (heap[leftchild].xkey < eventx)))
                {
                    smallest = leftchild;
                }
                else
                {
                    smallest = eventnum;
                }
                rightchild = leftchild + 1;
                if (rightchild < heapsize)
                {
                    if ((heap[rightchild].ykey < heap[smallest].ykey) ||
                        ((heap[rightchild].ykey == heap[smallest].ykey)
                         && (heap[rightchild].xkey < heap[smallest].xkey)))
                    {
                        smallest = rightchild;
                    }
                }
                if (smallest == eventnum)
                {
                    notdone = false;
                }
                else
                {
                    heap[eventnum] = heap[smallest];
                    heap[eventnum].heapposition = eventnum;
                    heap[smallest] = thisevent;
                    thisevent.heapposition = smallest;

                    eventnum = smallest;
                    leftchild = 2 * eventnum + 1;
                    notdone = leftchild < heapsize;
                }
            }
        }

        void HeapDelete(SweepEvent[] heap, int heapsize, int eventnum)
        {
            SweepEvent moveevent;
            double eventx, eventy;
            int parent;
            bool notdone;

            moveevent = heap[heapsize - 1];
            if (eventnum > 0)
            {
                eventx = moveevent.xkey;
                eventy = moveevent.ykey;
                do
                {
                    parent = (eventnum - 1) >> 1;
                    if ((heap[parent].ykey < eventy) ||
                        ((heap[parent].ykey == eventy)
                         && (heap[parent].xkey <= eventx)))
                    {
                        notdone = false;
                    }
                    else
                    {
                        heap[eventnum] = heap[parent];
                        heap[eventnum].heapposition = eventnum;

                        eventnum = parent;
                        notdone = eventnum > 0;
                    }
                } while (notdone);
            }
            heap[eventnum] = moveevent;
            moveevent.heapposition = eventnum;
            Heapify(heap, heapsize - 1, eventnum);
        }

        void CreateHeap(out SweepEvent[] eventheap, int size)
        {
            Vertex thisvertex;
            int maxevents;
            int i;
            SweepEvent evt;

            maxevents = (3 * size) / 2;
            eventheap = new SweepEvent[maxevents];

            i = 0;
            foreach (var v in mesh.vertices.Values)
            {
                thisvertex = v;
                evt = new SweepEvent();
                evt.vertexEvent = thisvertex;
                evt.xkey = thisvertex.x;
                evt.ykey = thisvertex.y;
                HeapInsert(eventheap, i++, evt);
            }
        }

        #endregion

        #region Splaytree

        SplayNode Splay(SplayNode splaytree, Point searchpoint, ref Otri searchtri)
        {
            SplayNode child, grandchild;
            SplayNode lefttree, righttree;
            SplayNode leftright;
            Vertex checkvertex;
            bool rightofroot, rightofchild;

            if (splaytree == null)
            {
                return null;
            }
            checkvertex = splaytree.keyedge.Dest();
            if (checkvertex == splaytree.keydest)
            {
                rightofroot = RightOfHyperbola(ref splaytree.keyedge, searchpoint);
                if (rightofroot)
                {
                    splaytree.keyedge.Copy(ref searchtri);
                    child = splaytree.rchild;
                }
                else
                {
                    child = splaytree.lchild;
                }
                if (child == null)
                {
                    return splaytree;
                }
                checkvertex = child.keyedge.Dest();
                if (checkvertex != child.keydest)
                {
                    child = Splay(child, searchpoint, ref searchtri);
                    if (child == null)
                    {
                        if (rightofroot)
                        {
                            splaytree.rchild = null;
                        }
                        else
                        {
                            splaytree.lchild = null;
                        }
                        return splaytree;
                    }
                }
                rightofchild = RightOfHyperbola(ref child.keyedge, searchpoint);
                if (rightofchild)
                {
                    child.keyedge.Copy(ref searchtri);
                    grandchild = Splay(child.rchild, searchpoint, ref searchtri);
                    child.rchild = grandchild;
                }
                else
                {
                    grandchild = Splay(child.lchild, searchpoint, ref searchtri);
                    child.lchild = grandchild;
                }
                if (grandchild == null)
                {
                    if (rightofroot)
                    {
                        splaytree.rchild = child.lchild;
                        child.lchild = splaytree;
                    }
                    else
                    {
                        splaytree.lchild = child.rchild;
                        child.rchild = splaytree;
                    }
                    return child;
                }
                if (rightofchild)
                {
                    if (rightofroot)
                    {
                        splaytree.rchild = child.lchild;
                        child.lchild = splaytree;
                    }
                    else
                    {
                        splaytree.lchild = grandchild.rchild;
                        grandchild.rchild = splaytree;
                    }
                    child.rchild = grandchild.lchild;
                    grandchild.lchild = child;
                }
                else
                {
                    if (rightofroot)
                    {
                        splaytree.rchild = grandchild.lchild;
                        grandchild.lchild = splaytree;
                    }
                    else
                    {
                        splaytree.lchild = child.rchild;
                        child.rchild = splaytree;
                    }
                    child.lchild = grandchild.rchild;
                    grandchild.rchild = child;
                }
                return grandchild;
            }
            else
            {
                lefttree = Splay(splaytree.lchild, searchpoint, ref searchtri);
                righttree = Splay(splaytree.rchild, searchpoint, ref searchtri);

                splaynodes.Remove(splaytree);
                if (lefttree == null)
                {
                    return righttree;
                }
                else if (righttree == null)
                {
                    return lefttree;
                }
                else if (lefttree.rchild == null)
                {
                    lefttree.rchild = righttree.lchild;
                    righttree.lchild = lefttree;
                    return righttree;
                }
                else if (righttree.lchild == null)
                {
                    righttree.lchild = lefttree.rchild;
                    lefttree.rchild = righttree;
                    return lefttree;
                }
                else
                {
                    //      printf("Holy Toledo!!!\n");
                    leftright = lefttree.rchild;
                    while (leftright.rchild != null)
                    {
                        leftright = leftright.rchild;
                    }
                    leftright.rchild = righttree;
                    return lefttree;
                }
            }
        }

        SplayNode SplayInsert(SplayNode splayroot, Otri newkey, Point searchpoint)
        {
            SplayNode newsplaynode;

            newsplaynode = new SplayNode(); //poolalloc(m.splaynodes);
            splaynodes.Add(newsplaynode);
            newkey.Copy(ref newsplaynode.keyedge);
            newsplaynode.keydest = newkey.Dest();
            if (splayroot == null)
            {
                newsplaynode.lchild = null;
                newsplaynode.rchild = null;
            }
            else if (RightOfHyperbola(ref splayroot.keyedge, searchpoint))
            {
                newsplaynode.lchild = splayroot;
                newsplaynode.rchild = splayroot.rchild;
                splayroot.rchild = null;
            }
            else
            {
                newsplaynode.lchild = splayroot.lchild;
                newsplaynode.rchild = splayroot;
                splayroot.lchild = null;
            }
            return newsplaynode;
        }

        SplayNode FrontLocate(SplayNode splayroot, Otri bottommost, Vertex searchvertex,
                              ref Otri searchtri, ref bool farright)
        {
            bool farrightflag;

            bottommost.Copy(ref searchtri);
            splayroot = Splay(splayroot, searchvertex, ref searchtri);

            farrightflag = false;
            while (!farrightflag && RightOfHyperbola(ref searchtri, searchvertex))
            {
                searchtri.Onext();
                farrightflag = searchtri.Equals(bottommost);
            }
            farright = farrightflag;
            return splayroot;
        }

        SplayNode CircleTopInsert(SplayNode splayroot, Otri newkey,
                                  Vertex pa, Vertex pb, Vertex pc, double topy)
        {
            double ccwabc;
            double xac, yac, xbc, ybc;
            double aclen2, bclen2;
            Point searchpoint = new Point(); // TODO: mesh.nextras
            Otri dummytri = default(Otri);

            ccwabc = predicates.CounterClockwise(pa, pb, pc);
            xac = pa.x - pc.x;
            yac = pa.y - pc.y;
            xbc = pb.x - pc.x;
            ybc = pb.y - pc.y;
            aclen2 = xac * xac + yac * yac;
            bclen2 = xbc * xbc + ybc * ybc;
            searchpoint.x = pc.x - (yac * bclen2 - ybc * aclen2) / (2.0 * ccwabc);
            searchpoint.y = topy;
            return SplayInsert(Splay(splayroot, searchpoint, ref dummytri), newkey, searchpoint);
        }

        #endregion

        bool RightOfHyperbola(ref Otri fronttri, Point newsite)
        {
            Vertex leftvertex, rightvertex;
            double dxa, dya, dxb, dyb;

            Statistic.HyperbolaCount++;

            leftvertex = fronttri.Dest();
            rightvertex = fronttri.Apex();
            if ((leftvertex.y < rightvertex.y) ||
                ((leftvertex.y == rightvertex.y) &&
                 (leftvertex.x < rightvertex.x)))
            {
                if (newsite.x >= rightvertex.x)
                {
                    return true;
                }
            }
            else
            {
                if (newsite.x <= leftvertex.x)
                {
                    return false;
                }
            }
            dxa = leftvertex.x - newsite.x;
            dya = leftvertex.y - newsite.y;
            dxb = rightvertex.x - newsite.x;
            dyb = rightvertex.y - newsite.y;
            return dya * (dxb * dxb + dyb * dyb) > dyb * (dxa * dxa + dya * dya);
        }

        double CircleTop(Vertex pa, Vertex pb, Vertex pc, double ccwabc)
        {
            double xac, yac, xbc, ybc, xab, yab;
            double aclen2, bclen2, ablen2;

            Statistic.CircleTopCount++;

            xac = pa.x - pc.x;
            yac = pa.y - pc.y;
            xbc = pb.x - pc.x;
            ybc = pb.y - pc.y;
            xab = pa.x - pb.x;
            yab = pa.y - pb.y;
            aclen2 = xac * xac + yac * yac;
            bclen2 = xbc * xbc + ybc * ybc;
            ablen2 = xab * xab + yab * yab;
            return pc.y + (xac * bclen2 - xbc * aclen2 + Math.Sqrt(aclen2 * bclen2 * ablen2)) / (2.0 * ccwabc);
        }

        void Check4DeadEvent(ref Otri checktri, SweepEvent[] eventheap, ref int heapsize)
        {
            SweepEvent deadevent;
            SweepEventVertex eventvertex;
            int eventnum = -1;

            eventvertex = checktri.Org() as SweepEventVertex;
            if (eventvertex != null)
            {
                deadevent = eventvertex.evt;
                eventnum = deadevent.heapposition;

                HeapDelete(eventheap, heapsize, eventnum);
                heapsize--;
                checktri.SetOrg(null);
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

            var dummytri = mesh.dummytri;

            // Find an edge on the convex hull to start point location from.
            startghost.Lprev(ref searchedge);
            searchedge.Sym();
            dummytri.neighbors[0] = searchedge;
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
                dissolveedge.Dissolve(dummytri);
                // Find the next bounding triangle.
                deadtriangle.Sym(ref dissolveedge);

                // Delete the bounding triangle.
                mesh.TriangleDealloc(deadtriangle.tri);
            } while (!dissolveedge.Equals(startghost));

            return hullsize;
        }

        #region Internal classes

        /// <summary>
        /// A node in a heap used to store events for the sweepline Delaunay algorithm.
        /// </summary>
        /// <remarks>
        /// Only used in the sweepline algorithm.
        /// 
        /// Nodes do not point directly to their parents or children in the heap. Instead, each
        /// node knows its position in the heap, and can look up its parent and children in a
        /// separate array. To distinguish site events from circle events, all circle events are
        /// given an invalid (smaller than 'xmin') x-coordinate 'xkey'.
        /// </remarks>
        class SweepEvent
        {
            public double xkey, ykey;     // Coordinates of the event.
            public Vertex vertexEvent;    // Vertex event.
            public Otri otriEvent;        // Circle event.
            public int heapposition;      // Marks this event's position in the heap.
        }

        /// <summary>
        /// Introducing a new class which aggregates a sweep event is the easiest way
        /// to handle the pointer magic of the original code (casting a sweep event 
        /// to vertex etc.).
        /// </summary>
        class SweepEventVertex : Vertex
        {
            public SweepEvent evt;

            public SweepEventVertex(SweepEvent e)
            {
                evt = e;
            }
        }

        /// <summary>
        /// A node in the splay tree.
        /// </summary>
        /// <remarks>
        /// Only used in the sweepline algorithm.
        /// 
        /// Each node holds an oriented ghost triangle that represents a boundary edge
        /// of the growing triangulation. When a circle event covers two boundary edges
        /// with a triangle, so that they are no longer boundary edges, those edges are
        /// not immediately deleted from the tree; rather, they are lazily deleted when
        /// they are next encountered. (Since only a random sample of boundary edges are
        /// kept in the tree, lazy deletion is faster.) 'keydest' is used to verify that
        /// a triangle is still the same as when it entered the splay tree; if it has
        /// been rotated (due to a circle event), it no longer represents a boundary
        /// edge and should be deleted.
        /// </remarks>
        class SplayNode
        {
            public Otri keyedge;              // Lprev of an edge on the front.
            public Vertex keydest;            // Used to verify that splay node is still live.
            public SplayNode lchild, rchild;  // Children in splay tree.
        }

        #endregion
    }
}
