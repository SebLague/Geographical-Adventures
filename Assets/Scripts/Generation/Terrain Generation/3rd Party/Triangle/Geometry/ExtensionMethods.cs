
namespace TriangleNet.Geometry
{
    using System;
    using TriangleNet.Meshing;

    public static class ExtensionMethods
    {
        #region IPolygon extensions

        /// <summary>
        /// Triangulates a polygon.
        /// </summary>
        public static IMesh Triangulate(this IPolygon polygon)
        {
            return (new GenericMesher()).Triangulate(polygon, null, null);
        }

        /// <summary>
        /// Triangulates a polygon, applying constraint options.
        /// </summary>
        /// <param name="options">Constraint options.</param>
        public static IMesh Triangulate(this IPolygon polygon, ConstraintOptions options)
        {
            return (new GenericMesher()).Triangulate(polygon, options, null);
        }

        /// <summary>
        /// Triangulates a polygon, applying quality options.
        /// </summary>
        /// <param name="quality">Quality options.</param>
        public static IMesh Triangulate(this IPolygon polygon, QualityOptions quality)
        {
            return (new GenericMesher()).Triangulate(polygon, null, quality);
        }

        /// <summary>
        /// Triangulates a polygon, applying quality and constraint options.
        /// </summary>
        /// <param name="options">Constraint options.</param>
        /// <param name="quality">Quality options.</param>
        public static IMesh Triangulate(this IPolygon polygon, ConstraintOptions options, QualityOptions quality)
        {
            return (new GenericMesher()).Triangulate(polygon, options, quality);
        }

        /// <summary>
        /// Triangulates a polygon, applying quality and constraint options.
        /// </summary>
        /// <param name="options">Constraint options.</param>
        /// <param name="quality">Quality options.</param>
        /// <param name="triangulator">The triangulation algorithm.</param>
        public static IMesh Triangulate(this IPolygon polygon, ConstraintOptions options, QualityOptions quality,
            ITriangulator triangulator)
        {
            return (new GenericMesher(triangulator)).Triangulate(polygon, options, quality);
        }

        #endregion

        #region Rectangle extensions

        #endregion

        #region ITriangle extensions

        /// <summary>
        /// Test whether a given point lies inside a triangle or not.
        /// </summary>
        /// <param name="p">Point to locate.</param>
        /// <returns>True, if point is inside or on the edge of this triangle.</returns>
        public static bool Contains(this ITriangle triangle, Point p)
        {
            return Contains(triangle, p.X, p.Y);
        }

        /// <summary>
        /// Test whether a given point lies inside a triangle or not.
        /// </summary>
        /// <param name="x">Point to locate.</param>
        /// <param name="y">Point to locate.</param>
        /// <returns>True, if point is inside or on the edge of this triangle.</returns>
        public static bool Contains(this ITriangle triangle, double x, double y)
        {
            var t0 = triangle.GetVertex(0);
            var t1 = triangle.GetVertex(1);
            var t2 = triangle.GetVertex(2);

            // TODO: no need to create new Point instances here
            Point d0 = new Point(t1.X - t0.X, t1.Y - t0.Y);
            Point d1 = new Point(t2.X - t0.X, t2.Y - t0.Y);
            Point d2 = new Point(x - t0.X, y - t0.Y);

            // crossproduct of (0, 0, 1) and d0
            Point c0 = new Point(-d0.Y, d0.X);

            // crossproduct of (0, 0, 1) and d1
            Point c1 = new Point(-d1.Y, d1.X);

            // Linear combination d2 = s * d0 + v * d1.
            //
            // Multiply both sides of the equation with c0 and c1
            // and solve for s and v respectively
            //
            // s = d2 * c1 / d0 * c1
            // v = d2 * c0 / d1 * c0

            double s = DotProduct(d2, c1) / DotProduct(d0, c1);
            double v = DotProduct(d2, c0) / DotProduct(d1, c0);

            if (s >= 0 && v >= 0 && ((s + v) <= 1))
            {
                // Point is inside or on the edge of this triangle.
                return true;
            }

            return false;
        }

        public static Rectangle Bounds(this ITriangle triangle)
        {
            var bounds = new Rectangle();

            for (int i = 0; i < 3; i++)
            {
                bounds.Expand(triangle.GetVertex(i));
            }

            return bounds;
        }

        #endregion

        #region Helper methods

        internal static double DotProduct(Point p, Point q)
        {
            return p.X * q.X + p.Y * q.Y;
        }

        #endregion
    }
}
