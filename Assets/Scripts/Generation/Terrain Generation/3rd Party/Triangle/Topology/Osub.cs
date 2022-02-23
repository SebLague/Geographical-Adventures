// -----------------------------------------------------------------------
// <copyright file="Osub.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Topology
{
    using System;
    using TriangleNet.Geometry;

    /// <summary>
    /// An oriented subsegment.
    /// </summary>
    /// <remarks>
    /// Includes a pointer to a subsegment and an orientation. The orientation denotes a
    /// side of the edge. Hence, there are two possible orientations. By convention, the
    /// edge is always directed so that the "side" denoted is the right side of the edge.
    /// </remarks>
    public struct Osub
    {
        internal SubSegment seg;
        internal int orient; // Ranges from 0 to 1.

        public SubSegment Segment
        {
            get { return seg; }
        }

        public override string ToString()
        {
            if (seg == null)
            {
                return "O-TID [null]";
            }
            return String.Format("O-SID {0}", seg.hash);
        }

        #region Osub primitives

        /// <summary>
        /// Reverse the orientation of a subsegment. [sym(ab) -> ba]
        /// </summary>
        public void Sym(ref Osub os)
        {
            os.seg = seg;
            os.orient = 1 - orient;
        }

        /// <summary>
        /// Reverse the orientation of a subsegment. [sym(ab) -> ba]
        /// </summary>
        public void Sym()
        {
            orient = 1 - orient;
        }

        /// <summary>
        /// Find adjoining subsegment with the same origin. [pivot(ab) -> a*]
        /// </summary>
        /// <remarks>spivot() finds the other subsegment (from the same segment) 
        /// that shares the same origin.
        /// </remarks>
        public void Pivot(ref Osub os)
        {
            os = seg.subsegs[orient];
        }

        /// <summary>
        /// Finds a triangle abutting a subsegment.
        /// </summary>
        internal void Pivot(ref Otri ot)
        {
            ot = seg.triangles[orient];
        }

        /// <summary>
        /// Find next subsegment in sequence. [next(ab) -> b*]
        /// </summary>
        public void Next(ref Osub ot)
        {
            ot = seg.subsegs[1 - orient];
        }

        /// <summary>
        /// Find next subsegment in sequence. [next(ab) -> b*]
        /// </summary>
        public void Next()
        {
            this = seg.subsegs[1 - orient];
        }

        /// <summary>
        /// Get the origin of a subsegment
        /// </summary>
        public Vertex Org()
        {
            return seg.vertices[orient];
        }

        /// <summary>
        /// Get the destination of a subsegment
        /// </summary>
        public Vertex Dest()
        {
            return seg.vertices[1 - orient];
        }


        #endregion

        #region Osub primitives (internal)

        /// <summary>
        /// Set the origin or destination of a subsegment.
        /// </summary>
        internal void SetOrg(Vertex vertex)
        {
            seg.vertices[orient] = vertex;
        }

        /// <summary>
        /// Set destination of a subsegment.
        /// </summary>
        internal void SetDest(Vertex vertex)
        {
            seg.vertices[1 - orient] = vertex;
        }

        /// <summary>
        /// Get the origin of the segment that includes the subsegment.
        /// </summary>
        internal Vertex SegOrg()
        {
            return seg.vertices[2 + orient];
        }

        /// <summary>
        /// Get the destination of the segment that includes the subsegment.
        /// </summary>
        internal Vertex SegDest()
        {
            return seg.vertices[3 - orient];
        }

        /// <summary>
        /// Set the origin of the segment that includes the subsegment.
        /// </summary>
        internal void SetSegOrg(Vertex vertex)
        {
            seg.vertices[2 + orient] = vertex;
        }

        /// <summary>
        /// Set the destination of the segment that includes the subsegment.
        /// </summary>
        internal void SetSegDest(Vertex vertex)
        {
            seg.vertices[3 - orient] = vertex;
        }

        /* Unused primitives.

        /// <summary>
        /// Find adjoining subsegment with the same origin. [pivot(ab) -> a*]
        /// </summary>
        public void PivotSelf()
        {
            this = seg.subsegs[orient];
        }

        /// <summary>
        /// Read a boundary marker.
        /// </summary>
        /// <remarks>Boundary markers are used to hold user-defined tags for 
        /// setting boundary conditions in finite element solvers.</remarks>
        public int Mark()
        {
            return seg.boundary;
        }

        /// <summary>
        /// Set a boundary marker.
        /// </summary>
        public void SetMark(int value)
        {
            seg.boundary = value;
        }

        /// <summary>
        /// Copy a subsegment.
        /// </summary>
        public void Copy(ref Osub o2)
        {
            o2.seg = seg;
            o2.orient = orient;
        }

        //*/

        /// <summary>
        /// Bond two subsegments together. [bond(abc, ba)]
        /// </summary>
        internal void Bond(ref Osub os)
        {
            seg.subsegs[orient] = os;
            os.seg.subsegs[os.orient] = this;
        }

        /// <summary>
        /// Dissolve a subsegment bond (from one side).
        /// </summary>
        /// <remarks>Note that the other subsegment will still think it's 
        /// connected to this subsegment.</remarks>
        internal void Dissolve(SubSegment dummy)
        {
            seg.subsegs[orient].seg = dummy;
        }

        /// <summary>
        /// Test for equality of subsegments.
        /// </summary>
        internal bool Equal(Osub os)
        {
            return ((seg == os.seg) && (orient == os.orient));
        }

        /// <summary>
        /// Dissolve a bond (from the subsegment side).
        /// </summary>
        internal void TriDissolve(Triangle dummy)
        {
            seg.triangles[orient].tri = dummy;
        }

        /// <summary>
        /// Check a subsegment's deallocation.
        /// </summary>
        internal static bool IsDead(SubSegment sub)
        {
            return sub.subsegs[0].seg == null;
        }

        /// <summary>
        /// Set a subsegment's deallocation.
        /// </summary>
        internal static void Kill(SubSegment sub)
        {
            sub.subsegs[0].seg = null;
            sub.subsegs[1].seg = null;
        }

        #endregion
    }
}
