// -----------------------------------------------------------------------
// <copyright file="Edge.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    /// <summary>
    /// Represents a straight line segment in 2D space.
    /// </summary>
    public class Edge : IEdge
    {
        /// <summary>
        /// Gets the first endpoints index.
        /// </summary>
        public int P0
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the second endpoints index.
        /// </summary>
        public int P1
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the segments boundary mark.
        /// </summary>
        public int Label
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge" /> class.
        /// </summary>
        public Edge(int p0, int p1)
            : this(p0, p1, 0)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge" /> class.
        /// </summary>
        public Edge(int p0, int p1, int label)
        {
            this.P0 = p0;
            this.P1 = p1;
            this.Label = label;
        }
    }
}
