
namespace TriangleNet.Tools
{
    using TriangleNet.Geometry;

    public static class Interpolation
    {
#if USE_ATTRIBS
        /// <summary>
        /// Linear interpolation of vertex attributes.
        /// </summary>
        /// <param name="vertex">The interpolation vertex.</param>
        /// <param name="triangle">The triangle containing the vertex.</param>
        /// <param name="n">The number of vertex attributes.</param>
        /// <remarks>
        /// The vertex is expected to lie inside the triangle.
        /// </remarks>
        public static void InterpolateAttributes(Vertex vertex, ITriangle triangle, int n)
        {
            Vertex org = triangle.GetVertex(0);
            Vertex dest = triangle.GetVertex(1);
            Vertex apex = triangle.GetVertex(2);

            double xdo, ydo, xao, yao;
            double denominator;
            double dx, dy;
            double xi, eta;

            // Compute the circumcenter of the triangle.
            xdo = dest.x - org.x;
            ydo = dest.y - org.y;
            xao = apex.x - org.x;
            yao = apex.y - org.y;

            denominator = 0.5 / (xdo * yao - xao * ydo);

            //dx = (yao * dodist - ydo * aodist) * denominator;
            //dy = (xdo * aodist - xao * dodist) * denominator;

            dx = vertex.x - org.x;
            dy = vertex.y - org.y;

            // To interpolate vertex attributes for the new vertex, define a
            // coordinate system with a xi-axis directed from the triangle's
            // origin to its destination, and an eta-axis, directed from its
            // origin to its apex.
            xi = (yao * dx - xao * dy) * (2.0 * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0 * denominator);
        
            for (int i = 0; i < n; i++)
            {
                // Interpolate the vertex attributes.
                vertex.attributes[i] = org.attributes[i]
                    + xi * (dest.attributes[i] - org.attributes[i])
                    + eta * (apex.attributes[i] - org.attributes[i]);
            }
        }
#endif

#if USE_Z
        /// <summary>
        /// Linear interpolation of a scalar value.
        /// </summary>
        /// <param name="p">The interpolation point.</param>
        /// <param name="triangle">The triangle containing the point.</param>
        /// <remarks>
        /// The point is expected to lie inside the triangle.
        /// </remarks>
        public static void InterpolateZ(Point p, ITriangle triangle)
        {
            Vertex org = triangle.GetVertex(0);
            Vertex dest = triangle.GetVertex(1);
            Vertex apex = triangle.GetVertex(2);

            double xdo, ydo, xao, yao;
            double denominator;
            double dx, dy;
            double xi, eta;

            // Compute the circumcenter of the triangle.
            xdo = dest.x - org.x;
            ydo = dest.y - org.y;
            xao = apex.x - org.x;
            yao = apex.y - org.y;

            denominator = 0.5 / (xdo * yao - xao * ydo);

            //dx = (yao * dodist - ydo * aodist) * denominator;
            //dy = (xdo * aodist - xao * dodist) * denominator;

            dx = p.x - org.x;
            dy = p.y - org.y;

            // To interpolate z value for the given point inserted, define a
            // coordinate system with a xi-axis, directed from the triangle's
            // origin to its destination, and an eta-axis, directed from its
            // origin to its apex.
            xi = (yao * dx - xao * dy) * (2.0 * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

            p.z = org.z + xi * (dest.z - org.z) + eta * (apex.z - org.z);
        }
#endif
    }
}
