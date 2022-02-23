// -----------------------------------------------------------------------
// <copyright file="Rectangle.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A simple rectangle class.
    /// </summary>
    public class Rectangle
    {
        double xmin, ymin, xmax, ymax;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle" /> class.
        /// </summary>
        public Rectangle()
        {
            this.xmin = this.ymin = double.MaxValue;
            this.xmax = this.ymax = -double.MaxValue;
        }

        public Rectangle(Rectangle other)
            : this(other.Left, other.Bottom, other.Right, other.Top)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle" /> class
        /// with predefined bounds.
        /// </summary>
        /// <param name="x">Minimum x value (left).</param>
        /// <param name="y">Minimum y value (bottom).</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        public Rectangle(double x, double y, double width, double height)
        {
            this.xmin = x;
            this.ymin = y;
            this.xmax = x + width;
            this.ymax = y + height;
        }

        /// <summary>
        /// Gets the minimum x value (left boundary).
        /// </summary>
        public double Left
        {
            get { return xmin; }
        }

        /// <summary>
        /// Gets the maximum x value (right boundary).
        /// </summary>
        public double Right
        {
            get { return xmax; }
        }

        /// <summary>
        /// Gets the minimum y value (bottom boundary).
        /// </summary>
        public double Bottom
        {
            get { return ymin; }
        }

        /// <summary>
        /// Gets the maximum y value (top boundary).
        /// </summary>
        public double Top
        {
            get { return ymax; }
        }

        /// <summary>
        /// Gets the width of the rectangle.
        /// </summary>
        public double Width
        {
            get { return xmax - xmin; }
        }

        /// <summary>
        /// Gets the height of the rectangle.
        /// </summary>
        public double Height
        {
            get { return ymax - ymin; }
        }

        /// <summary>
        /// Update bounds.
        /// </summary>
        /// <param name="dx">Add dx to left and right bounds.</param>
        /// <param name="dy">Add dy to top and bottom bounds.</param>
        public void Resize(double dx, double dy)
        {
            xmin -= dx;
            xmax += dx;
            ymin -= dy;
            ymax += dy;
        }

        /// <summary>
        /// Expand rectangle to include given point.
        /// </summary>
        /// <param name="p">Point.</param>
        public void Expand(Point p)
        {
            xmin = Math.Min(xmin, p.x);
            ymin = Math.Min(ymin, p.y);
            xmax = Math.Max(xmax, p.x);
            ymax = Math.Max(ymax, p.y);
        }

        /// <summary>
        /// Expand rectangle to include a list of points.
        /// </summary>
        public void Expand(IEnumerable<Point> points)
        {
            foreach (var p in points)
            {
                Expand(p);
            }
        }

        /// <summary>
        /// Expand rectangle to include given rectangle.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public void Expand(Rectangle other)
        {
            xmin = Math.Min(xmin, other.xmin);
            ymin = Math.Min(ymin, other.ymin);
            xmax = Math.Max(xmax, other.xmax);
            ymax = Math.Max(ymax, other.ymax);
        }

        /// <summary>
        /// Check if given point is inside rectangle.
        /// </summary>
        /// <param name="x">Point to check.</param>
        /// <param name="y">Point to check.</param>
        /// <returns>Return true, if rectangle contains given point.</returns>
        public bool Contains(double x, double y)
        {
            return ((x >= xmin) && (x <= xmax) && (y >= ymin) && (y <= ymax));
        }

        /// <summary>
        /// Check if given point is inside rectangle.
        /// </summary>
        /// <param name="pt">Point to check.</param>
        /// <returns>Return true, if rectangle contains given point.</returns>
        public bool Contains(Point pt)
        {
            return Contains(pt.x, pt.y);
        }

        /// <summary>
        /// Check if this rectangle contains other rectangle.
        /// </summary>
        /// <param name="other">Rectangle to check.</param>
        /// <returns>Return true, if this rectangle contains given rectangle.</returns>
        public bool Contains(Rectangle other)
        {
            return (xmin <= other.Left && other.Right <= xmax
                && ymin <= other.Bottom && other.Top <= ymax);
        }

        /// <summary>
        /// Check if this rectangle intersects other rectangle.
        /// </summary>
        /// <param name="other">Rectangle to check.</param>
        /// <returns>Return true, if given rectangle intersects this rectangle.</returns>
        public bool Intersects(Rectangle other)
        {
            return (other.Left < xmax && xmin < other.Right
                && other.Bottom < ymax && ymin < other.Top);
        }
    }
}
