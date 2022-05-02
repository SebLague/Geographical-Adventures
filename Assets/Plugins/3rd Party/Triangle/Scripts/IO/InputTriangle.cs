// -----------------------------------------------------------------------
// <copyright file="InputTriangle.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using TriangleNet.Topology;
    using TriangleNet.Geometry;

    /// <summary>
    /// Simple triangle class for input.
    /// </summary>
    public class InputTriangle : ITriangle
    {
        internal int[] vertices;
        internal int label;
        internal double area;

        public InputTriangle(int p0, int p1, int p2)
        {
            this.vertices = new int[] { p0, p1, p2 };
        }

        #region Public properties

        /// <summary>
        /// Gets the triangle id.
        /// </summary>
        public int ID
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        /// Region ID the triangle belongs to.
        /// </summary>
        public int Label
        {
            get { return label; }
            set { label = value; }
        }

        /// <summary>
        /// Gets the triangle area constraint.
        /// </summary>
        public double Area
        {
            get { return area; }
            set { area = value; }
        }

        /// <summary>
        /// Gets the specified corners vertex.
        /// </summary>
        public Vertex GetVertex(int index)
        {
            return null; // TODO: throw NotSupportedException?
        }

        public int GetVertexID(int index)
        {
            return vertices[index];
        }

        public ITriangle GetNeighbor(int index)
        {
            return null;
        }

        public int GetNeighborID(int index)
        {
            return -1;
        }

        public ISegment GetSegment(int index)
        {
            return null;
        }

        #endregion
    }
}
