// -----------------------------------------------------------------------
// <copyright file="GenericMesher.cs">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing
{
    using System;
    using System.Collections.Generic;
    using TriangleNet.Geometry;
    using TriangleNet.IO;
    using TriangleNet.Meshing.Algorithm;

    /// <summary>
    /// Create meshes of point sets or polygons.
    /// </summary>
    public class GenericMesher
    {
        Configuration config;
        ITriangulator triangulator;

        public GenericMesher()
            : this(new Dwyer(), new Configuration())
        {
        }

        public GenericMesher(ITriangulator triangulator)
            : this(triangulator, new Configuration())
        {
        }

        public GenericMesher(Configuration config)
            : this(new Dwyer(), config)
        {
        }

        public GenericMesher(ITriangulator triangulator, Configuration config)
        {
            this.config = config;
            this.triangulator = triangulator;
        }

        /// <inheritdoc />
        public IMesh Triangulate(IList<Vertex> points)
        {
            return triangulator.Triangulate(points, config);
        }

        /// <inheritdoc />
        public IMesh Triangulate(IPolygon polygon)
        {
            return Triangulate(polygon, null, null);
        }

        /// <inheritdoc />
        public IMesh Triangulate(IPolygon polygon, ConstraintOptions options)
        {
            return Triangulate(polygon, options, null);
        }

        /// <inheritdoc />
        public IMesh Triangulate(IPolygon polygon, QualityOptions quality)
        {
            return Triangulate(polygon, null, quality);
        }

        /// <inheritdoc />
        public IMesh Triangulate(IPolygon polygon, ConstraintOptions options, QualityOptions quality)
        {
            var mesh = (Mesh)triangulator.Triangulate(polygon.Points, config);

            var cmesher = new ConstraintMesher(mesh, config);
            var qmesher = new QualityMesher(mesh, config);

            mesh.SetQualityMesher(qmesher);

            // Insert segments.
            cmesher.Apply(polygon, options);

            // Refine mesh.
            qmesher.Apply(quality);

            return mesh;
        }

        /// <summary>
        /// Generates a structured mesh with bounds [0, 0, width, height].
        /// </summary>
        /// <param name="width">Width of the mesh (must be > 0).</param>
        /// <param name="height">Height of the mesh (must be > 0).</param>
        /// <param name="nx">Number of segments in x direction.</param>
        /// <param name="ny">Number of segments in y direction.</param>
        /// <returns>Mesh</returns>
        public static IMesh StructuredMesh(double width, double height, int nx, int ny)
        {
            if (width <= 0.0)
            {
                throw new ArgumentException("width");
            }

            if (height <= 0.0)
            {
                throw new ArgumentException("height");
            }

            return StructuredMesh(new Rectangle(0.0, 0.0, width, height), nx, ny);
        }

        /// <summary>
        /// Generates a structured mesh.
        /// </summary>
        /// <param name="bounds">Bounds of the mesh.</param>
        /// <param name="nx">Number of segments in x direction.</param>
        /// <param name="ny">Number of segments in y direction.</param>
        /// <returns>Mesh</returns>
        public static IMesh StructuredMesh(Rectangle bounds, int nx, int ny)
        {
            var polygon = new Polygon((nx + 1) * (ny + 1));

            double x, y, dx, dy, left, bottom;

            dx = bounds.Width / nx;
            dy = bounds.Height / ny;

            left = bounds.Left;
            bottom = bounds.Bottom;

            int i, j, k, l, n = 0;

            // Add vertices.
            var points = new Vertex[(nx + 1) * (ny + 1)];

            for (i = 0; i <= nx; i++)
            {
                x = left + i * dx;

                for (j = 0; j <= ny; j++)
                {
                    y = bottom + j * dy;

                    points[n++] = new Vertex(x, y);
                }
            }

            polygon.Points.AddRange(points);

            n = 0;

            // Set vertex hash and id.
            foreach (var v in points)
            {
                v.hash = v.id = n++;
            }

            // Add boundary segments.
            var segments = polygon.Segments;

            segments.Capacity = 2 * (nx + ny);

            Vertex a, b;

            for (j = 0; j < ny; j++)
            {
                // Left
                a = points[j];
                b = points[j + 1];

                segments.Add(new Segment(a, b, 1));

                a.Label = b.Label = 1;

                // Right
                a = points[nx * (ny + 1) + j];
                b = points[nx * (ny + 1) + (j + 1)];

                segments.Add(new Segment(a, b, 1));

                a.Label = b.Label = 1;
            }

            for (i = 0; i < nx; i++)
            {
                // Bottom
                a = points[(ny + 1) * i];
                b = points[(ny + 1) * (i + 1)];

                segments.Add(new Segment(a, b, 1));

                a.Label = b.Label = 1;

                // Top
                a = points[ny + (ny + 1) * i];
                b = points[ny + (ny + 1) * (i + 1)];

                segments.Add(new Segment(a, b, 1));

                a.Label = b.Label = 1;
            }

            // Add triangles.
            var triangles = new InputTriangle[2 * nx * ny];

            n = 0;

            for (i = 0; i < nx; i++)
            {
                for (j = 0; j < ny; j++)
                {
                    k = j + (ny + 1) * i;
                    l = j + (ny + 1) * (i + 1);

                    // Create 2 triangles in rectangle [k, l, l + 1, k + 1].

                    if ((i + j) % 2 == 0)
                    {
                        // Diagonal from bottom left to top right.
                        triangles[n++] = new InputTriangle(k, l, l + 1);
                        triangles[n++] = new InputTriangle(k, l + 1, k + 1);
                    }
                    else
                    {
                        // Diagonal from top left to bottom right.
                        triangles[n++] = new InputTriangle(k, l, k + 1);
                        triangles[n++] = new InputTriangle(l, l + 1, k + 1);
                    }
                }
            }

            return Converter.ToMesh(polygon, triangles);
        }
    }
}
