// -----------------------------------------------------------------------
// <copyright file="HalfEdge.cs">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Topology.DCEL
{
    public class HalfEdge
    {
        internal int id;
        internal int mark;

        internal Vertex origin;
        internal Face face;
        internal HalfEdge twin;
        internal HalfEdge next;

        /// <summary>
        /// Gets or sets the half-edge id.
        /// </summary>
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public int Boundary
        {
            get { return mark; }
            set { mark = value; }
        }

        /// <summary>
        /// Gets or sets the origin of the half-edge.
        /// </summary>
        public Vertex Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        /// <summary>
        /// Gets or sets the face connected to the half-edge.
        /// </summary>
        public Face Face
        {
            get { return face; }
            set { face = value; }
        }

        /// <summary>
        /// Gets or sets the twin of the half-edge.
        /// </summary>
        public HalfEdge Twin
        {
            get { return twin; }
            set { twin = value; }
        }

        /// <summary>
        /// Gets or sets the next pointer of the half-edge.
        /// </summary>
        public HalfEdge Next
        {
            get { return next; }
            set { next = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HalfEdge" /> class.
        /// </summary>
        /// <param name="origin">The origin of this half-edge.</param>
        public HalfEdge(Vertex origin)
        {
            this.origin = origin;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HalfEdge" /> class.
        /// </summary>
        /// <param name="origin">The origin of this half-edge.</param>
        /// <param name="face">The face connected to this half-edge.</param>
        public HalfEdge(Vertex origin, Face face)
        {
            this.origin = origin;
            this.face = face;

            // IMPORTANT: do not remove the (face.edge == null) check!
            if (face != null && face.edge == null)
            {
                face.edge = this;
            }
        }

        public override string ToString()
        {
            return string.Format("HE-ID {0} (Origin = VID-{1})", id, origin.id);
        }
    }
}
