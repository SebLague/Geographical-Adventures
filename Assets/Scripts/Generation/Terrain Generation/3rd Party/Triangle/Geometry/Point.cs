// -----------------------------------------------------------------------
// <copyright file="Point.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents a 2D point.
    /// </summary>
#if USE_Z
    [DebuggerDisplay("ID {ID} [{X}, {Y}, {Z}]")]
#else
    [DebuggerDisplay("ID {ID} [{X}, {Y}]")]
#endif
    public class Point : IComparable<Point>, IEquatable<Point>
    {
        internal int id;
        internal int label;

        internal double x;
        internal double y;
#if USE_Z
        internal double z;
#endif

        public Point()
            : this(0.0, 0.0, 0)
        {
        }

        public Point(double x, double y)
            : this(x, y, 0)
        {
        }

        public Point(double x, double y, int label)
        {
            this.x = x;
            this.y = y;
            this.label = label;
        }

        #region Public properties

        /// <summary>
        /// Gets or sets the vertex id.
        /// </summary>
        public int ID
        {
            get { return this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the vertex x coordinate.
        /// </summary>
        public double X
        {
            get { return this.x; }
            set { this.x = value; }
        }

        /// <summary>
        /// Gets or sets the vertex y coordinate.
        /// </summary>
        public double Y
        {
            get { return this.y; }
            set { this.y = value; }
        }

#if USE_Z
        /// <summary>
        /// Gets or sets the vertex z coordinate.
        /// </summary>
        public double Z
        {
            get { return this.z; }
            set { this.z = value; }
        }
#endif

        /// <summary>
        /// Gets or sets a general-purpose label.
        /// </summary>
        /// <remarks>
        /// This is used for the vertex boundary mark.
        /// </remarks>
        public int Label
        {
            get { return this.label; }
            set { this.label = value; }
        }

        #endregion

        #region Operator overloading / overriding Equals

        // Compare "Guidelines for Overriding Equals() and Operator =="
        // http://msdn.microsoft.com/en-us/library/ms173147.aspx

        public static bool operator ==(Point a, Point b)
        {
            // If both are null, or both are same instance, return true.
            if (Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            Point p = obj as Point;

            if ((object)p == null)
            {
                return false;
            }

            return (x == p.x) && (y == p.y);
        }

        public bool Equals(Point p)
        {
            // If vertex is null return false.
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (x == p.x) && (y == p.y);
        }

        #endregion

        public int CompareTo(Point other)
        {
            if (x == other.x && y == other.y)
            {
                return 0;
            }

            return (x < other.x || (x == other.x && y < other.y)) ? -1 : 1;
        }

        public override int GetHashCode()
        {
            int hash = 19;
            hash = hash * 31 + x.GetHashCode();
            hash = hash * 31 + y.GetHashCode();

            return hash;
        }
    }
}
