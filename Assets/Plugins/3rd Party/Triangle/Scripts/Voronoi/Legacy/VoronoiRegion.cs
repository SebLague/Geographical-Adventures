// -----------------------------------------------------------------------
// <copyright file="VoronoiRegion.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Voronoi.Legacy
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;

    /// <summary>
    /// Represents a region in the Voronoi diagram.
    /// </summary>
    public class VoronoiRegion
    {
        int id;
        Point generator;
        List<Point> vertices;
        bool bounded;

        // A map (vertex id) -> (neighbor across adjacent edge)
        Dictionary<int, VoronoiRegion> neighbors;

        /// <summary>
        /// Gets the Voronoi region id (which is the same as the generators vertex id).
        /// </summary>
        public int ID
        {
            get { return id; }
        }

        /// <summary>
        /// Gets the Voronoi regions generator.
        /// </summary>
        public Point Generator
        {
            get { return generator; }
        }

        /// <summary>
        /// Gets the Voronoi vertices on the regions boundary.
        /// </summary>
        public ICollection<Point> Vertices
        {
            get { return vertices; }
        }

        /// <summary>
        /// Gets or sets whether the Voronoi region is bounded.
        /// </summary>
        public bool Bounded
        {
            get { return bounded; }
            set { bounded = value; }
        }

        public VoronoiRegion(Vertex generator)
        {
            this.id = generator.id;
            this.generator = generator;
            this.vertices = new List<Point>();
            this.bounded = true;

            this.neighbors = new Dictionary<int, VoronoiRegion>();
        }

        public void Add(Point point)
        {
            this.vertices.Add(point);
        }

        public void Add(List<Point> points)
        {
            this.vertices.AddRange(points);
        }

        /// <summary>
        /// Returns the neighbouring Voronoi region, that lies across the edge starting at
        /// given vertex.
        /// </summary>
        /// <param name="p">Vertex defining an edge of the region.</param>
        /// <returns>Neighbouring Voronoi region</returns>
        /// <remarks>
        /// The edge starting at p is well defined (vertices are ordered counterclockwise).
        /// </remarks>
        public VoronoiRegion GetNeighbor(Point p)
        {
            VoronoiRegion neighbor;

            if (neighbors.TryGetValue(p.id, out neighbor))
            {
                return neighbor;
            }

            return null;
        }

        internal void AddNeighbor(int id, VoronoiRegion neighbor)
        {
            this.neighbors.Add(id, neighbor);
        }

        public override string ToString()
        {
            return String.Format("R-ID {0}", id);
        }
    }
}
