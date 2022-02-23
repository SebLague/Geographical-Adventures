// -----------------------------------------------------------------------
// <copyright file="Otri.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Topology
{
    using System;
    using TriangleNet.Geometry;

    /// <summary>
    /// An oriented triangle.
    /// </summary>
    /// <remarks>
    /// Includes a pointer to a triangle and orientation.  The orientation denotes an edge
    /// of the triangle. Hence, there are three possible orientations. By convention, each
    /// edge always points counterclockwise about the corresponding triangle.
    /// </remarks>
    public struct Otri
    {
        internal Triangle tri;
        internal int orient; // Ranges from 0 to 2.

        public Triangle Triangle
        {
            get { return tri; }
            set { tri = value; }
        }

        public override string ToString()
        {
            if (tri == null)
            {
                return "O-TID [null]";
            }
            return String.Format("O-TID {0}", tri.hash);
        }

        #region Otri primitives (public)

        // For fast access
        static readonly int[] plus1Mod3 = { 1, 2, 0 };
        static readonly int[] minus1Mod3 = { 2, 0, 1 };

        // The following primitives are all described by Guibas and Stolfi.
        // However, Guibas and Stolfi use an edge-based data structure,
        // whereas I use a triangle-based data structure.
        //
        // lnext: finds the next edge (counterclockwise) of a triangle.
        //
        // onext: spins counterclockwise around a vertex; that is, it finds 
        // the next edge with the same origin in the counterclockwise direction. This
        // edge is part of a different triangle.
        //
        // oprev: spins clockwise around a vertex; that is, it finds the 
        // next edge with the same origin in the clockwise direction.  This edge is 
        // part of a different triangle.
        //
        // dnext: spins counterclockwise around a vertex; that is, it finds 
        // the next edge with the same destination in the counterclockwise direction.
        // This edge is part of a different triangle.
        //
        // dprev: spins clockwise around a vertex; that is, it finds the 
        // next edge with the same destination in the clockwise direction. This edge 
        // is part of a different triangle.
        //
        // rnext: moves one edge counterclockwise about the adjacent 
        // triangle. (It's best understood by reading Guibas and Stolfi. It 
        // involves changing triangles twice.)
        //
        // rprev: moves one edge clockwise about the adjacent triangle.
        // (It's best understood by reading Guibas and Stolfi.  It involves
        // changing triangles twice.)

        /// <summary>
        /// Find the abutting triangle; same edge. [sym(abc) -> ba*]
        /// </summary>
        /// Note that the edge direction is necessarily reversed, because the handle specified 
        /// by an oriented triangle is directed counterclockwise around the triangle.
        /// </remarks>
        public void Sym(ref Otri ot)
        {
            ot.tri = tri.neighbors[orient].tri;
            ot.orient = tri.neighbors[orient].orient;
        }

        /// <summary>
        /// Find the abutting triangle; same edge. [sym(abc) -> ba*]
        /// </summary>
        public void Sym()
        {
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of a triangle. [lnext(abc) -> bca]
        /// </summary>
        public void Lnext(ref Otri ot)
        {
            ot.tri = tri;
            ot.orient = plus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of a triangle. [lnext(abc) -> bca]
        /// </summary>
        public void Lnext()
        {
            orient = plus1Mod3[orient];
        }

        /// <summary>
        /// Find the previous edge (clockwise) of a triangle. [lprev(abc) -> cab]
        /// </summary>
        public void Lprev(ref Otri ot)
        {
            ot.tri = tri;
            ot.orient = minus1Mod3[orient];
        }

        /// <summary>
        /// Find the previous edge (clockwise) of a triangle. [lprev(abc) -> cab]
        /// </summary>
        public void Lprev()
        {
            orient = minus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same origin. [onext(abc) -> ac*]
        /// </summary>
        public void Onext(ref Otri ot)
        {
            //Lprev(ref ot);
            ot.tri = tri;
            ot.orient = minus1Mod3[orient];

            //ot.SymSelf();
            int tmp = ot.orient;
            ot.orient = ot.tri.neighbors[tmp].orient;
            ot.tri = ot.tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same origin. [onext(abc) -> ac*]
        /// </summary>
        public void Onext()
        {
            //LprevSelf();
            orient = minus1Mod3[orient];

            //SymSelf();
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the next edge clockwise with the same origin. [oprev(abc) -> a*b]
        /// </summary>
        public void Oprev(ref Otri ot)
        {
            //Sym(ref ot);
            ot.tri = tri.neighbors[orient].tri;
            ot.orient = tri.neighbors[orient].orient;

            //ot.LnextSelf();
            ot.orient = plus1Mod3[ot.orient];
        }

        /// <summary>
        /// Find the next edge clockwise with the same origin. [oprev(abc) -> a*b]
        /// </summary>
        public void Oprev()
        {
            //SymSelf();
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;

            //LnextSelf();
            orient = plus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same destination. [dnext(abc) -> *ba]
        /// </summary>
        public void Dnext(ref Otri ot)
        {
            //Sym(ref ot);
            ot.tri = tri.neighbors[orient].tri;
            ot.orient = tri.neighbors[orient].orient;

            //ot.LprevSelf();
            ot.orient = minus1Mod3[ot.orient];
        }

        /// <summary>
        /// Find the next edge counterclockwise with the same destination. [dnext(abc) -> *ba]
        /// </summary>
        public void Dnext()
        {
            //SymSelf();
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;

            //LprevSelf();
            orient = minus1Mod3[orient];
        }

        /// <summary>
        /// Find the next edge clockwise with the same destination. [dprev(abc) -> cb*]
        /// </summary>
        public void Dprev(ref Otri ot)
        {
            //Lnext(ref ot);
            ot.tri = tri;
            ot.orient = plus1Mod3[orient];

            //ot.SymSelf();
            int tmp = ot.orient;
            ot.orient = ot.tri.neighbors[tmp].orient;
            ot.tri = ot.tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the next edge clockwise with the same destination. [dprev(abc) -> cb*]
        /// </summary>
        public void Dprev()
        {
            //LnextSelf();
            orient = plus1Mod3[orient];

            //SymSelf();
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of the adjacent triangle. [rnext(abc) -> *a*]
        /// </summary>
        public void Rnext(ref Otri ot)
        {
            //Sym(ref ot);
            ot.tri = tri.neighbors[orient].tri;
            ot.orient = tri.neighbors[orient].orient;

            //ot.LnextSelf();
            ot.orient = plus1Mod3[ot.orient];

            //ot.SymSelf();
            int tmp = ot.orient;
            ot.orient = ot.tri.neighbors[tmp].orient;
            ot.tri = ot.tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the next edge (counterclockwise) of the adjacent triangle. [rnext(abc) -> *a*]
        /// </summary>
        public void Rnext()
        {
            //SymSelf();
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;

            //LnextSelf();
            orient = plus1Mod3[orient];

            //SymSelf();
            tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the previous edge (clockwise) of the adjacent triangle. [rprev(abc) -> b**]
        /// </summary>
        public void Rprev(ref Otri ot)
        {
            //Sym(ref ot);
            ot.tri = tri.neighbors[orient].tri;
            ot.orient = tri.neighbors[orient].orient;

            //ot.LprevSelf();
            ot.orient = minus1Mod3[ot.orient];

            //ot.SymSelf();
            int tmp = ot.orient;
            ot.orient = ot.tri.neighbors[tmp].orient;
            ot.tri = ot.tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Find the previous edge (clockwise) of the adjacent triangle. [rprev(abc) -> b**]
        /// </summary>
        public void Rprev()
        {
            //SymSelf();
            int tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;

            //LprevSelf();
            orient = minus1Mod3[orient];

            //SymSelf();
            tmp = orient;
            orient = tri.neighbors[tmp].orient;
            tri = tri.neighbors[tmp].tri;
        }

        /// <summary>
        /// Origin [org(abc) -> a]
        /// </summary>
        public Vertex Org()
        {
            return tri.vertices[plus1Mod3[orient]];
        }

        /// <summary>
        /// Destination [dest(abc) -> b]
        /// </summary>
        public Vertex Dest()
        {
            return tri.vertices[minus1Mod3[orient]];
        }

        /// <summary>
        /// Apex [apex(abc) -> c]
        /// </summary>
        public Vertex Apex()
        {
            return tri.vertices[orient];
        }

        /// <summary>
        /// Copy an oriented triangle.
        /// </summary>
        public void Copy(ref Otri ot)
        {
            ot.tri = tri;
            ot.orient = orient;
        }

        /// <summary>
        /// Test for equality of oriented triangles.
        /// </summary>
        public bool Equals(Otri ot)
        {
            return ((tri == ot.tri) && (orient == ot.orient));
        }

        #endregion

        #region Otri primitives (internal)

        /// <summary>
        /// Set Origin
        /// </summary>
        internal void SetOrg(Vertex v)
        {
            tri.vertices[plus1Mod3[orient]] = v;
        }

        /// <summary>
        /// Set Destination
        /// </summary>
        internal void SetDest(Vertex v)
        {
            tri.vertices[minus1Mod3[orient]] = v;
        }

        /// <summary>
        /// Set Apex
        /// </summary>
        internal void SetApex(Vertex v)
        {
            tri.vertices[orient] = v;
        }

        /// <summary>
        /// Bond two triangles together at the resepective handles. [bond(abc, bad)]
        /// </summary>
        internal void Bond(ref Otri ot)
        {
            tri.neighbors[orient].tri = ot.tri;
            tri.neighbors[orient].orient = ot.orient;

            ot.tri.neighbors[ot.orient].tri = this.tri;
            ot.tri.neighbors[ot.orient].orient = this.orient;
        }

        /// <summary>
        /// Dissolve a bond (from one side).  
        /// </summary>
        /// <remarks>Note that the other triangle will still think it's connected to 
        /// this triangle. Usually, however, the other triangle is being deleted 
        /// entirely, or bonded to another triangle, so it doesn't matter.
        /// </remarks>
        internal void Dissolve(Triangle dummy)
        {
            tri.neighbors[orient].tri = dummy;
            tri.neighbors[orient].orient = 0;
        }

        /// <summary>
        /// Infect a triangle with the virus.
        /// </summary>
        internal void Infect()
        {
            tri.infected = true;
        }

        /// <summary>
        /// Cure a triangle from the virus.
        /// </summary>
        internal void Uninfect()
        {
            tri.infected = false;
        }

        /// <summary>
        /// Test a triangle for viral infection.
        /// </summary>
        internal bool IsInfected()
        {
            return tri.infected;
        }

        /// <summary>
        /// Finds a subsegment abutting a triangle.
        /// </summary>
        internal void Pivot(ref Osub os)
        {
            os = tri.subsegs[orient];
        }

        /// <summary>
        /// Bond a triangle to a subsegment.
        /// </summary>
        internal void SegBond(ref Osub os)
        {
            tri.subsegs[orient] = os;
            os.seg.triangles[os.orient] = this;
        }

        /// <summary>
        /// Dissolve a bond (from the triangle side).
        /// </summary>
        internal void SegDissolve(SubSegment dummy)
        {
            tri.subsegs[orient].seg = dummy;
        }

        /// <summary>
        /// Check a triangle's deallocation.
        /// </summary>
        internal static bool IsDead(Triangle tria)
        {
            return tria.neighbors[0].tri == null;
        }

        /// <summary>
        /// Set a triangle's deallocation.
        /// </summary>
        internal static void Kill(Triangle tri)
        {
            tri.neighbors[0].tri = null;
            tri.neighbors[2].tri = null;
        }

        #endregion
    }
}
