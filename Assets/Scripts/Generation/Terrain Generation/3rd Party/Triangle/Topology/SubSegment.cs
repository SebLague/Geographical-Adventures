// -----------------------------------------------------------------------
// <copyright file="Segment.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Topology
{
    using System;
    using TriangleNet.Geometry;

    /// <summary>
    /// The subsegment data structure.
    /// </summary>
    public class SubSegment : ISegment
    {
        // Hash for dictionary. Will be set by mesh instance.
        internal int hash;

        internal Osub[] subsegs;
        internal Vertex[] vertices;
        internal Otri[] triangles;
        internal int boundary;

        public SubSegment()
        {
            // Four NULL vertices.
            vertices = new Vertex[4];

            // Set the boundary marker to zero.
            boundary = 0;

            // Initialize the two adjoining subsegments to be the omnipresent
            // subsegment.
            subsegs = new Osub[2];

            // Initialize the two adjoining triangles to be "outer space."
            triangles = new Otri[2];
        }

        #region Public properties

        /// <summary>
        /// Gets the first endpoints vertex id.
        /// </summary>
        public int P0
        {
            get { return this.vertices[0].id; }
        }

        /// <summary>
        /// Gets the seconds endpoints vertex id.
        /// </summary>
        public int P1
        {
            get { return this.vertices[1].id; }
        }

        /// <summary>
        /// Gets the segment boundary mark.
        /// </summary>
        public int Label
        {
            get { return this.boundary; }
        }

        #endregion

        /// <summary>
        /// Gets the segments endpoint.
        /// </summary>
        public Vertex GetVertex(int index)
        {
            return this.vertices[index]; // TODO: Check range?
        }

        /// <summary>
        /// Gets an adjoining triangle.
        /// </summary>
        public ITriangle GetTriangle(int index)
        {
            return triangles[index].tri.hash == Mesh.DUMMY ? null : triangles[index].tri;
        }

        public override int GetHashCode()
        {
            return this.hash;
        }

        public override string ToString()
        {
            return String.Format("SID {0}", hash);
        }
    }
}
