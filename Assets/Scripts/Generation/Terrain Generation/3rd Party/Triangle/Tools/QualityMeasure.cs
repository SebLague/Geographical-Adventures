// -----------------------------------------------------------------------
// <copyright file="QualityMeasure.cs" company="">
// Original Matlab code by John Burkardt, Florida State University
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using TriangleNet.Geometry;

    /// <summary>
    /// Provides mesh quality information.
    /// </summary>
    /// <remarks>
    /// Given a triangle abc with points A (ax, ay), B (bx, by), C (cx, cy).
    /// 
    /// The side lengths are given as
    ///   a = sqrt((cx - bx)^2 + (cy - by)^2) -- side BC opposite of A
    ///   b = sqrt((cx - ax)^2 + (cy - ay)^2) -- side CA opposite of B
    ///   c = sqrt((ax - bx)^2 + (ay - by)^2) -- side AB opposite of C
    ///   
    /// The angles are given as
    ///   ang_a = acos((b^2 + c^2 - a^2)  / (2 * b * c)) -- angle at A
    ///   ang_b = acos((c^2 + a^2 - b^2)  / (2 * c * a)) -- angle at B
    ///   ang_c = acos((a^2 + b^2 - c^2)  / (2 * a * b)) -- angle at C
    ///   
    /// The semiperimeter is given as
    ///   s = (a + b + c) / 2
    ///   
    /// The area is given as
    ///   D = abs(ax * (by - cy) + bx * (cy - ay) + cx * (ay - by)) / 2
    ///     = sqrt(s * (s - a) * (s - b) * (s - c))
    ///      
    /// The inradius is given as
    ///   r = D / s
    ///   
    /// The circumradius is given as
    ///   R = a * b * c / (4 * D)
    /// 
    /// The altitudes are given as
    ///   alt_a = 2 * D / a -- altitude above side a
    ///   alt_b = 2 * D / b -- altitude above side b
    ///   alt_c = 2 * D / c -- altitude above side c
    /// 
    /// The aspect ratio may be given as the ratio of the longest to the
    /// shortest edge or, more commonly as the ratio of the circumradius 
    /// to twice the inradius
    ///   ar = R / (2 * r)
    ///      = a * b * c / (8 * (s - a) * (s - b) * (s - c))
    ///      = a * b * c / ((b + c - a) * (c + a - b) * (a + b - c))
    /// </remarks>
    public class QualityMeasure
    {
        AreaMeasure areaMeasure;
        AlphaMeasure alphaMeasure;
        Q_Measure qMeasure;

        Mesh mesh;

        public QualityMeasure()
        {
            areaMeasure = new AreaMeasure();
            alphaMeasure = new AlphaMeasure();
            qMeasure = new Q_Measure();
        }

        #region Public properties

        /// <summary>
        /// Minimum triangle area.
        /// </summary>
        public double AreaMinimum
        {
            get { return areaMeasure.area_min; }
        }

        /// <summary>
        /// Maximum triangle area.
        /// </summary>
        public double AreaMaximum
        {
            get { return areaMeasure.area_max; }
        }

        /// <summary>
        /// Ratio of maximum and minimum triangle area.
        /// </summary>
        public double AreaRatio
        {
            get { return areaMeasure.area_max / areaMeasure.area_min; }
        }

        /// <summary>
        /// Smallest angle.
        /// </summary>
        public double AlphaMinimum
        {
            get { return alphaMeasure.alpha_min; }
        }

        /// <summary>
        /// Maximum smallest angle.
        /// </summary>
        public double AlphaMaximum
        {
            get { return alphaMeasure.alpha_max; }
        }

        /// <summary>
        /// Average angle.
        /// </summary>
        public double AlphaAverage
        {
            get { return alphaMeasure.alpha_ave; }
        }

        /// <summary>
        /// Average angle weighted by area.
        /// </summary>
        public double AlphaArea
        {
            get { return alphaMeasure.alpha_area; }
        }

        /// <summary>
        /// Smallest aspect ratio.
        /// </summary>
        public double Q_Minimum
        {
            get { return qMeasure.q_min; }
        }

        /// <summary>
        /// Largest aspect ratio.
        /// </summary>
        public double Q_Maximum
        {
            get { return qMeasure.q_max; }
        }

        /// <summary>
        /// Average aspect ratio.
        /// </summary>
        public double Q_Average
        {
            get { return qMeasure.q_ave; }
        }

        /// <summary>
        /// Average aspect ratio weighted by area.
        /// </summary>
        public double Q_Area
        {
            get { return qMeasure.q_area; }
        }

        #endregion

        public void Update(Mesh mesh)
        {
            this.mesh = mesh;

            // Reset all measures.
            areaMeasure.Reset();
            alphaMeasure.Reset();
            qMeasure.Reset();

            Compute();
        }

        private void Compute()
        {
            Point a, b, c;
            double ab, bc, ca;
            double lx, ly;
            double area;

            int n = 0;

            foreach (var tri in mesh.triangles)
            {
                n++;

                a = tri.vertices[0];
                b = tri.vertices[1];
                c = tri.vertices[2];

                lx = a.x - b.x;
                ly = a.y - b.y;
                ab = Math.Sqrt(lx * lx + ly * ly);
                lx = b.x - c.x;
                ly = b.y - c.y;
                bc = Math.Sqrt(lx * lx + ly * ly);
                lx = c.x - a.x;
                ly = c.y - a.y;
                ca = Math.Sqrt(lx * lx + ly * ly);

                area = areaMeasure.Measure(a, b, c);
                alphaMeasure.Measure(ab, bc, ca, area);
                qMeasure.Measure(ab, bc, ca, area);
            }

            // Normalize measures
            alphaMeasure.Normalize(n, areaMeasure.area_total);
            qMeasure.Normalize(n, areaMeasure.area_total);
        }

        /// <summary>
        /// Determines the bandwidth of the coefficient matrix.
        /// </summary>
        /// <returns>Bandwidth of the coefficient matrix.</returns>
        /// <remarks>
        /// The quantity computed here is the "geometric" bandwidth determined
        /// by the finite element mesh alone.
        ///
        /// If a single finite element variable is associated with each node
        /// of the mesh, and if the nodes and variables are numbered in the
        /// same way, then the geometric bandwidth is the same as the bandwidth
        /// of a typical finite element matrix.
        ///
        /// The bandwidth M is defined in terms of the lower and upper bandwidths:
        ///
        ///   M = ML + 1 + MU
        ///
        /// where 
        ///
        ///   ML = maximum distance from any diagonal entry to a nonzero
        ///   entry in the same row, but earlier column,
        ///
        ///   MU = maximum distance from any diagonal entry to a nonzero
        ///   entry in the same row, but later column.
        ///
        /// Because the finite element node adjacency relationship is symmetric,
        /// we are guaranteed that ML = MU.
        /// </remarks>
        public int Bandwidth()
        {
            if (mesh == null) return 0;

            // Lower and upper bandwidth of the matrix
            int ml = 0, mu = 0;

            int gi, gj;

            foreach (var tri in mesh.triangles)
            {
                for (int j = 0; j < 3; j++)
                {
                    gi = tri.GetVertex(j).id;

                    for (int k = 0; k < 3; k++)
                    {
                        gj = tri.GetVertex(k).id;

                        mu = Math.Max(mu, gj - gi);
                        ml = Math.Max(ml, gi - gj);
                    }
                }
            }

            return ml + 1 + mu;
        }

        class AreaMeasure
        {
            // Minimum area
            public double area_min = double.MaxValue;
            // Maximum area
            public double area_max = -double.MaxValue;
            // Total area of geometry
            public double area_total = 0;
            // Nmber of triangles with zero area
            public int area_zero = 0;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                area_min = double.MaxValue;
                area_max = -double.MaxValue;
                area_total = 0;
                area_zero = 0;
            }

            /// <summary>
            /// Compute the area of given triangle.
            /// </summary>
            /// <param name="a">Triangle corner a.</param>
            /// <param name="b">Triangle corner b.</param>
            /// <param name="c">Triangle corner c.</param>
            /// <returns>Triangle area.</returns>
            public double Measure(Point a, Point b, Point c)
            {
                double area = 0.5 * Math.Abs(a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));

                area_min = Math.Min(area_min, area);
                area_max = Math.Max(area_max, area);
                area_total += area;

                if (area == 0.0)
                {
                    area_zero = area_zero + 1;
                }

                return area;
            }
        }

        /// <summary>
        /// The alpha measure determines the triangulated pointset quality.
        /// </summary>
        /// <remarks>
        /// The alpha measure evaluates the uniformity of the shapes of the triangles
        /// defined by a triangulated pointset.
        ///
        /// We compute the minimum angle among all the triangles in the triangulated
        /// dataset and divide by the maximum possible value (which, in degrees,
        /// is 60). The best possible value is 1, and the worst 0. A good
        /// triangulation should have an alpha score close to 1.
        /// </remarks>
        class AlphaMeasure
        {
            // Minimum value over all triangles
            public double alpha_min;
            // Maximum value over all triangles
            public double alpha_max;
            // Value averaged over all triangles
            public double alpha_ave;
            // Value averaged over all triangles and weighted by area
            public double alpha_area;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                alpha_min = double.MaxValue;
                alpha_max = -double.MaxValue;
                alpha_ave = 0;
                alpha_area = 0;
            }

            double acos(double c)
            {
                if (c <= -1.0)
                {
                    return Math.PI;
                }
                else if (1.0 <= c)
                {
                    return 0.0;
                }
                else
                {
                    return Math.Acos(c);
                }
            }

            /// <summary>
            /// Compute q value of given triangle.
            /// </summary>
            /// <param name="ab">Side length ab.</param>
            /// <param name="bc">Side length bc.</param>
            /// <param name="ca">Side length ca.</param>
            /// <param name="area">Triangle area.</param>
            /// <returns></returns>
            public double Measure(double ab, double bc, double ca, double area)
            {
                double alpha = double.MaxValue;

                double ab2 = ab * ab;
                double bc2 = bc * bc;
                double ca2 = ca * ca;

                double a_angle;
                double b_angle;
                double c_angle;

                // Take care of a ridiculous special case.
                if (ab == 0.0 && bc == 0.0 && ca == 0.0)
                {
                    a_angle = 2.0 * Math.PI / 3.0;
                    b_angle = 2.0 * Math.PI / 3.0;
                    c_angle = 2.0 * Math.PI / 3.0;
                }
                else
                {
                    if (ca == 0.0 || ab == 0.0)
                    {
                        a_angle = Math.PI;
                    }
                    else
                    {
                        a_angle = acos((ca2 + ab2 - bc2) / (2.0 * ca * ab));
                    }

                    if (ab == 0.0 || bc == 0.0)
                    {
                        b_angle = Math.PI;
                    }
                    else
                    {
                        b_angle = acos((ab2 + bc2 - ca2) / (2.0 * ab * bc));
                    }

                    if (bc == 0.0 || ca == 0.0)
                    {
                        c_angle = Math.PI;
                    }
                    else
                    {
                        c_angle = acos((bc2 + ca2 - ab2) / (2.0 * bc * ca));
                    }
                }

                alpha = Math.Min(alpha, a_angle);
                alpha = Math.Min(alpha, b_angle);
                alpha = Math.Min(alpha, c_angle);

                // Normalize angle from [0,pi/3] radians into qualities in [0,1].
                alpha = alpha * 3.0 / Math.PI;

                alpha_ave += alpha;
                alpha_area += area * alpha;

                alpha_min = Math.Min(alpha, alpha_min);
                alpha_max = Math.Max(alpha, alpha_max);

                return alpha;
            }

            /// <summary>
            /// Normalize values.
            /// </summary>
            public void Normalize(int n, double area_total)
            {
                if (n > 0)
                {
                    alpha_ave /= n;
                }
                else
                {
                    alpha_ave = 0.0;
                }

                if (0.0 < area_total)
                {
                    alpha_area /= area_total;
                }
                else
                {
                    alpha_area = 0.0;
                }
            }
        }

        /// <summary>
        /// The Q measure determines the triangulated pointset quality.
        /// </summary>
        /// <remarks>
        /// The Q measure evaluates the uniformity of the shapes of the triangles
        /// defined by a triangulated pointset. It uses the aspect ratio
        ///
        ///    2 * (incircle radius) / (circumcircle radius)
        ///
        /// In an ideally regular mesh, all triangles would have the same
        /// equilateral shape, for which Q = 1. A good mesh would have
        /// 0.5 &lt; Q.
        /// </remarks>
        class Q_Measure
        {
            // Minimum value over all triangles
            public double q_min;
            // Maximum value over all triangles
            public double q_max;
            // Average value
            public double q_ave;
            // Average value weighted by the area of each triangle
            public double q_area;

            /// <summary>
            /// Reset all values.
            /// </summary>
            public void Reset()
            {
                q_min = double.MaxValue;
                q_max = -double.MaxValue;
                q_ave = 0;
                q_area = 0;
            }

            /// <summary>
            /// Compute q value of given triangle.
            /// </summary>
            /// <param name="ab">Side length ab.</param>
            /// <param name="bc">Side length bc.</param>
            /// <param name="ca">Side length ca.</param>
            /// <param name="area">Triangle area.</param>
            /// <returns></returns>
            public double Measure(double ab, double bc, double ca, double area)
            {
                double q = (bc + ca - ab) * (ca + ab - bc) * (ab + bc - ca) / (ab * bc * ca);

                q_min = Math.Min(q_min, q);
                q_max = Math.Max(q_max, q);

                q_ave += q;
                q_area += q * area;

                return q;
            }

            /// <summary>
            /// Normalize values.
            /// </summary>
            public void Normalize(int n, double area_total)
            {
                if (n > 0)
                {
                    q_ave /= n;
                }
                else
                {
                    q_ave = 0.0;
                }

                if (area_total > 0.0)
                {
                    q_area /= area_total;
                }
                else
                {
                    q_area = 0.0;
                }
            }
        }
    }
}
