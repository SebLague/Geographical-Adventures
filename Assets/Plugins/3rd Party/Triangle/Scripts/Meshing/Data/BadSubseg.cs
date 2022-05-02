// -----------------------------------------------------------------------
// <copyright file="BadSubseg.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing.Data
{
    using System;
    using TriangleNet.Geometry;
    using TriangleNet.Topology;

    /// <summary>
    /// A queue used to store encroached subsegments.
    /// </summary>
    /// <remarks>
    /// Each subsegment's vertices are stored so that we can check whether a 
    /// subsegment is still the same.
    /// </remarks>
    class BadSubseg
    {
        public Osub subseg; // An encroached subsegment.
        public Vertex org, dest; // Its two vertices.

        public override int GetHashCode()
        {
            return subseg.seg.hash;
        }

        public override string ToString()
        {
            return String.Format("B-SID {0}", subseg.seg.hash);
        }
    }
}
