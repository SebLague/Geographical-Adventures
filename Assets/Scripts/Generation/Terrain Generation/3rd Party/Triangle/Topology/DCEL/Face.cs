// -----------------------------------------------------------------------
// <copyright file="Face.cs">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Topology.DCEL
{
    using System.Collections.Generic;
    using TriangleNet.Geometry;

    /// <summary>
    /// A face of DCEL mesh.
    /// </summary>
    public class Face
    {
        #region Static initialization of "Outer Space" face

        public static readonly Face Empty;

        static Face()
        {
            Empty = new Face(null);
            Empty.id = -1;
        }

        #endregion

        internal int id;
        internal int mark;

        internal Point generator;

        internal HalfEdge edge;
        internal bool bounded;

        /// <summary>
        /// Gets or sets the face id.
        /// </summary>
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Gets or sets a half-edge connected to the face.
        /// </summary>
        public HalfEdge Edge
        {
            get { return edge; }
            set { edge = value; }
        }

        /// <summary>
        /// Gets or sets a value, indicating if the face is bounded (for Voronoi diagram).
        /// </summary>
        public bool Bounded
        {
            get { return bounded; }
            set { bounded = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Face" /> class.
        /// </summary>
        /// <param name="generator">The generator of this face (for Voronoi diagram)</param>
        public Face(Point generator)
            : this(generator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Face" /> class.
        /// </summary>
        /// <param name="generator">The generator of this face (for Voronoi diagram)</param>
        /// <param name="edge">The half-edge connected to this face.</param>
        public Face(Point generator, HalfEdge edge)
        {
            this.generator = generator;
            this.edge = edge;
            this.bounded = true;

            if (generator != null)
            {
                this.id = generator.ID;
            }
        }

        /// <summary>
        /// Enumerates all half-edges of the face boundary.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HalfEdge> EnumerateEdges()
        {
            var edge = this.Edge;
            int first = edge.ID;

            do
            {
                yield return edge;

                edge = edge.Next;
            } while (edge.ID != first);
        }

        public override string ToString()
        {
            return string.Format("F-ID {0}", id);
        }
    }
}
