// -----------------------------------------------------------------------
// <copyright file="BadTriangle.cs" company="">
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
    /// A queue used to store bad triangles.
    /// </summary>
    /// <remarks>
    /// The key is the square of the cosine of the smallest angle of the triangle.
    /// Each triangle's vertices are stored so that one can check whether a
    /// triangle is still the same.
    /// </remarks>
    class BadTriangle
    {
        public Otri poortri; // A skinny or too-large triangle.
        public double key; // cos^2 of smallest (apical) angle.
        public Vertex org, dest, apex; // Its three vertices.
        public BadTriangle next; // Pointer to next bad triangle.

        public override string ToString()
        {
            return String.Format("B-TID {0}", poortri.tri.hash);
        }
    }
}
