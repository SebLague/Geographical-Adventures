// -----------------------------------------------------------------------
// <copyright file="RobustPredicates.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using System;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;

    /// <summary>
    /// Adaptive exact arithmetic geometric predicates.
    /// </summary>
    /// <remarks>
    /// The adaptive exact arithmetic geometric predicates implemented herein are described in
    /// detail in the paper "Adaptive Precision Floating-Point Arithmetic and Fast Robust
    /// Geometric Predicates." by Jonathan Richard Shewchuk, see
    /// http://www.cs.cmu.edu/~quake/robust.html
    /// 
    /// The macros of the original C code were automatically expanded using the Visual Studio
    /// command prompt with the command "CL /P /C EXACT.C", see
    /// http://msdn.microsoft.com/en-us/library/8z9z0bx6.aspx
    /// </remarks>
    public class RobustPredicates : IPredicates
    {
        #region Default predicates instance (Singleton)

        private static readonly object creationLock = new object();
        private static RobustPredicates _default;

        /// <summary>
        /// Gets the default configuration instance.
        /// </summary>
        public static RobustPredicates Default
        {
            get
            {
                if (_default == null)
                {
                    lock (creationLock)
                    {
                        if (_default == null)
                        {
                            _default = new RobustPredicates();
                        }
                    }
                }

                return _default;
            }
        }

        #endregion

        #region Static initialization

        private static double epsilon, splitter, resulterrbound;
        private static double ccwerrboundA, ccwerrboundB, ccwerrboundC;
        private static double iccerrboundA, iccerrboundB, iccerrboundC;
        //private static double o3derrboundA, o3derrboundB, o3derrboundC;

        /// <summary>
        /// Initialize the variables used for exact arithmetic.  
        /// </summary>
        /// <remarks>
        /// 'epsilon' is the largest power of two such that 1.0 + epsilon = 1.0 in
        /// floating-point arithmetic. 'epsilon' bounds the relative roundoff
        /// error. It is used for floating-point error analysis.
        ///
        /// 'splitter' is used to split floating-point numbers into two half-
        /// length significands for exact multiplication.
        ///
        /// I imagine that a highly optimizing compiler might be too smart for its
        /// own good, and somehow cause this routine to fail, if it pretends that
        /// floating-point arithmetic is too much like double arithmetic.
        ///
        /// Don't change this routine unless you fully understand it.
        /// </remarks>
        static RobustPredicates()
        {
            double half;
            double check, lastcheck;
            bool every_other;

            every_other = true;
            half = 0.5;
            epsilon = 1.0;
            splitter = 1.0;
            check = 1.0;
            // Repeatedly divide 'epsilon' by two until it is too small to add to
            // one without causing roundoff.  (Also check if the sum is equal to
            // the previous sum, for machines that round up instead of using exact
            // rounding.  Not that these routines will work on such machines.)
            do
            {
                lastcheck = check;
                epsilon *= half;
                if (every_other)
                {
                    splitter *= 2.0;
                }
                every_other = !every_other;
                check = 1.0 + epsilon;
            } while ((check != 1.0) && (check != lastcheck));
            splitter += 1.0;
            // Error bounds for orientation and incircle tests. 
            resulterrbound = (3.0 + 8.0 * epsilon) * epsilon;
            ccwerrboundA = (3.0 + 16.0 * epsilon) * epsilon;
            ccwerrboundB = (2.0 + 12.0 * epsilon) * epsilon;
            ccwerrboundC = (9.0 + 64.0 * epsilon) * epsilon * epsilon;
            iccerrboundA = (10.0 + 96.0 * epsilon) * epsilon;
            iccerrboundB = (4.0 + 48.0 * epsilon) * epsilon;
            iccerrboundC = (44.0 + 576.0 * epsilon) * epsilon * epsilon;
            //o3derrboundA = (7.0 + 56.0 * epsilon) * epsilon;
            //o3derrboundB = (3.0 + 28.0 * epsilon) * epsilon;
            //o3derrboundC = (26.0 + 288.0 * epsilon) * epsilon * epsilon;
        }

        #endregion

        public RobustPredicates()
        {
            AllocateWorkspace();
        }

        /// <summary>
        /// Check, if the three points appear in counterclockwise order. The result is 
        /// also a rough approximation of twice the signed area of the triangle defined 
        /// by the three points.
        /// </summary>
        /// <param name="pa">Point a.</param>
        /// <param name="pb">Point b.</param>
        /// <param name="pc">Point c.</param>
        /// <returns>Return a positive value if the points pa, pb, and pc occur in 
        /// counterclockwise order; a negative value if they occur in clockwise order; 
        /// and zero if they are collinear.</returns>
        public double CounterClockwise(Point pa, Point pb, Point pc)
        {
            double detleft, detright, det;
            double detsum, errbound;

            Statistic.CounterClockwiseCount++;

            detleft = (pa.x - pc.x) * (pb.y - pc.y);
            detright = (pa.y - pc.y) * (pb.x - pc.x);
            det = detleft - detright;

            if (Behavior.NoExact)
            {
                return det;
            }

            if (detleft > 0.0)
            {
                if (detright <= 0.0)
                {
                    return det;
                }
                else
                {
                    detsum = detleft + detright;
                }
            }
            else if (detleft < 0.0)
            {
                if (detright >= 0.0)
                {
                    return det;
                }
                else
                {
                    detsum = -detleft - detright;
                }
            }
            else
            {
                return det;
            }

            errbound = ccwerrboundA * detsum;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            Statistic.CounterClockwiseAdaptCount++;
            return CounterClockwiseAdapt(pa, pb, pc, detsum);
        }

        /// <summary>
        /// Check if the point pd lies inside the circle passing through pa, pb, and pc. The 
        /// points pa, pb, and pc must be in counterclockwise order, or the sign of the result 
        /// will be reversed.
        /// </summary>
        /// <param name="pa">Point a.</param>
        /// <param name="pb">Point b.</param>
        /// <param name="pc">Point c.</param>
        /// <param name="pd">Point d.</param>
        /// <returns>Return a positive value if the point pd lies inside the circle passing through 
        /// pa, pb, and pc; a negative value if it lies outside; and zero if the four points 
        /// are cocircular.</returns>
        public double InCircle(Point pa, Point pb, Point pc, Point pd)
        {
            double adx, bdx, cdx, ady, bdy, cdy;
            double bdxcdy, cdxbdy, cdxady, adxcdy, adxbdy, bdxady;
            double alift, blift, clift;
            double det;
            double permanent, errbound;

            Statistic.InCircleCount++;

            adx = pa.x - pd.x;
            bdx = pb.x - pd.x;
            cdx = pc.x - pd.x;
            ady = pa.y - pd.y;
            bdy = pb.y - pd.y;
            cdy = pc.y - pd.y;

            bdxcdy = bdx * cdy;
            cdxbdy = cdx * bdy;
            alift = adx * adx + ady * ady;

            cdxady = cdx * ady;
            adxcdy = adx * cdy;
            blift = bdx * bdx + bdy * bdy;

            adxbdy = adx * bdy;
            bdxady = bdx * ady;
            clift = cdx * cdx + cdy * cdy;

            det = alift * (bdxcdy - cdxbdy)
                + blift * (cdxady - adxcdy)
                + clift * (adxbdy - bdxady);

            if (Behavior.NoExact)
            {
                return det;
            }

            permanent = (Math.Abs(bdxcdy) + Math.Abs(cdxbdy)) * alift
                      + (Math.Abs(cdxady) + Math.Abs(adxcdy)) * blift
                      + (Math.Abs(adxbdy) + Math.Abs(bdxady)) * clift;
            errbound = iccerrboundA * permanent;
            if ((det > errbound) || (-det > errbound))
            {
                return det;
            }

            Statistic.InCircleAdaptCount++;
            return InCircleAdapt(pa, pb, pc, pd, permanent);
        }

        /// <summary>
        /// Return a positive value if the point pd is incompatible with the circle 
        /// or plane passing through pa, pb, and pc (meaning that pd is inside the 
        /// circle or below the plane); a negative value if it is compatible; and 
        /// zero if the four points are cocircular/coplanar. The points pa, pb, and 
        /// pc must be in counterclockwise order, or the sign of the result will be 
        /// reversed.
        /// </summary>
        /// <param name="pa">Point a.</param>
        /// <param name="pb">Point b.</param>
        /// <param name="pc">Point c.</param>
        /// <param name="pd">Point d.</param>
        /// <returns>Return a positive value if the point pd lies inside the circle passing through 
        /// pa, pb, and pc; a negative value if it lies outside; and zero if the four points 
        /// are cocircular.</returns>
        public double NonRegular(Point pa, Point pb, Point pc, Point pd)
        {
            return InCircle(pa, pb, pc, pd);
        }

        /// <summary>
        /// Find the circumcenter of a triangle.
        /// </summary>
        /// <param name="org">Triangle point.</param>
        /// <param name="dest">Triangle point.</param>
        /// <param name="apex">Triangle point.</param>
        /// <param name="xi">Relative coordinate of new location.</param>
        /// <param name="eta">Relative coordinate of new location.</param>
        /// <param name="offconstant">Off-center constant.</param>
        /// <returns>Coordinates of the circumcenter (or off-center)</returns>
        public Point FindCircumcenter(Point org, Point dest, Point apex,
            ref double xi, ref double eta, double offconstant)
        {
            double xdo, ydo, xao, yao;
            double dodist, aodist, dadist;
            double denominator;
            double dx, dy, dxoff, dyoff;

            Statistic.CircumcenterCount++;

            // Compute the circumcenter of the triangle.
            xdo = dest.x - org.x;
            ydo = dest.y - org.y;
            xao = apex.x - org.x;
            yao = apex.y - org.y;
            dodist = xdo * xdo + ydo * ydo;
            aodist = xao * xao + yao * yao;
            dadist = (dest.x - apex.x) * (dest.x - apex.x) +
                     (dest.y - apex.y) * (dest.y - apex.y);

            if (Behavior.NoExact)
            {
                denominator = 0.5 / (xdo * yao - xao * ydo);
            }
            else
            {
                // Use the counterclockwise() routine to ensure a positive (and
                // reasonably accurate) result, avoiding any possibility of
                // division by zero.
                denominator = 0.5 / CounterClockwise(dest, apex, org);
                // Don't count the above as an orientation test.
                Statistic.CounterClockwiseCount--;
            }

            dx = (yao * dodist - ydo * aodist) * denominator;
            dy = (xdo * aodist - xao * dodist) * denominator;

            // Find the (squared) length of the triangle's shortest edge.  This
            // serves as a conservative estimate of the insertion radius of the
            // circumcenter's parent. The estimate is used to ensure that
            // the algorithm terminates even if very small angles appear in
            // the input PSLG.
            if ((dodist < aodist) && (dodist < dadist))
            {
                if (offconstant > 0.0)
                {
                    // Find the position of the off-center, as described by Alper Ungor.
                    dxoff = 0.5 * xdo - offconstant * ydo;
                    dyoff = 0.5 * ydo + offconstant * xdo;
                    // If the off-center is closer to the origin than the
                    // circumcenter, use the off-center instead.
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                }
            }
            else if (aodist < dadist)
            {
                if (offconstant > 0.0)
                {
                    dxoff = 0.5 * xao + offconstant * yao;
                    dyoff = 0.5 * yao - offconstant * xao;
                    // If the off-center is closer to the origin than the
                    // circumcenter, use the off-center instead.
                    if (dxoff * dxoff + dyoff * dyoff < dx * dx + dy * dy)
                    {
                        dx = dxoff;
                        dy = dyoff;
                    }
                }
            }
            else
            {
                if (offconstant > 0.0)
                {
                    dxoff = 0.5 * (apex.x - dest.x) - offconstant * (apex.y - dest.y);
                    dyoff = 0.5 * (apex.y - dest.y) + offconstant * (apex.x - dest.x);
                    // If the off-center is closer to the destination than the
                    // circumcenter, use the off-center instead.
                    if (dxoff * dxoff + dyoff * dyoff <
                        (dx - xdo) * (dx - xdo) + (dy - ydo) * (dy - ydo))
                    {
                        dx = xdo + dxoff;
                        dy = ydo + dyoff;
                    }
                }
            }

            // To interpolate vertex attributes for the new vertex inserted at
            // the circumcenter, define a coordinate system with a xi-axis,
            // directed from the triangle's origin to its destination, and
            // an eta-axis, directed from its origin to its apex.
            // Calculate the xi and eta coordinates of the circumcenter.
            xi = (yao * dx - xao * dy) * (2.0 * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

            return new Point(org.x + dx, org.y + dy);
        }

        /// <summary>
        /// Find the circumcenter of a triangle.
        /// </summary>
        /// <param name="org">Triangle point.</param>
        /// <param name="dest">Triangle point.</param>
        /// <param name="apex">Triangle point.</param>
        /// <param name="xi">Relative coordinate of new location.</param>
        /// <param name="eta">Relative coordinate of new location.</param>
        /// <returns>Coordinates of the circumcenter</returns>
        /// <remarks>
        /// The result is returned both in terms of x-y coordinates and xi-eta
        /// (barycentric) coordinates. The xi-eta coordinate system is defined in
        /// terms of the triangle: the origin of the triangle is the origin of the
        /// coordinate system; the destination of the triangle is one unit along the
        /// xi axis; and the apex of the triangle is one unit along the eta axis.
        /// This procedure also returns the square of the length of the triangle's
        /// shortest edge.
        /// </remarks>
        public Point FindCircumcenter(Point org, Point dest, Point apex,
            ref double xi, ref double eta)
        {
            double xdo, ydo, xao, yao;
            double dodist, aodist;
            double denominator;
            double dx, dy;

            Statistic.CircumcenterCount++;

            // Compute the circumcenter of the triangle.
            xdo = dest.x - org.x;
            ydo = dest.y - org.y;
            xao = apex.x - org.x;
            yao = apex.y - org.y;
            dodist = xdo * xdo + ydo * ydo;
            aodist = xao * xao + yao * yao;

            if (Behavior.NoExact)
            {
                denominator = 0.5 / (xdo * yao - xao * ydo);
            }
            else
            {
                // Use the counterclockwise() routine to ensure a positive (and
                // reasonably accurate) result, avoiding any possibility of
                // division by zero.
                denominator = 0.5 / CounterClockwise(dest, apex, org);
                // Don't count the above as an orientation test.
                Statistic.CounterClockwiseCount--;
            }

            dx = (yao * dodist - ydo * aodist) * denominator;
            dy = (xdo * aodist - xao * dodist) * denominator;

            // To interpolate vertex attributes for the new vertex inserted at
            // the circumcenter, define a coordinate system with a xi-axis,
            // directed from the triangle's origin to its destination, and
            // an eta-axis, directed from its origin to its apex.
            // Calculate the xi and eta coordinates of the circumcenter.
            xi = (yao * dx - xao * dy) * (2.0 * denominator);
            eta = (xdo * dy - ydo * dx) * (2.0 * denominator);

            return new Point(org.x + dx, org.y + dy);
        }

        #region Exact arithmetics

        /// <summary>
        /// Sum two expansions, eliminating zero components from the output expansion.  
        /// </summary>
        /// <param name="elen"></param>
        /// <param name="e"></param>
        /// <param name="flen"></param>
        /// <param name="f"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sets h = e + f.  See the Robust Predicates paper for details.
        /// 
        /// If round-to-even is used (as with IEEE 754), maintains the strongly nonoverlapping
        /// property.  (That is, if e is strongly nonoverlapping, h will be also.) Does NOT
        /// maintain the nonoverlapping or nonadjacent properties. 
        /// </remarks>
        private int FastExpansionSumZeroElim(int elen, double[] e, int flen, double[] f, double[] h)
        {
            double Q;
            double Qnew;
            double hh;
            double bvirt;
            double avirt, bround, around;
            int eindex, findex, hindex;
            double enow, fnow;

            enow = e[0];
            fnow = f[0];
            eindex = findex = 0;
            if ((fnow > enow) == (fnow > -enow))
            {
                Q = enow;
                enow = e[++eindex];
            }
            else
            {
                Q = fnow;
                fnow = f[++findex];
            }
            hindex = 0;
            if ((eindex < elen) && (findex < flen))
            {
                if ((fnow > enow) == (fnow > -enow))
                {
                    Qnew = (double)(enow + Q); bvirt = Qnew - enow; hh = Q - bvirt;
                    enow = e[++eindex];
                }
                else
                {
                    Qnew = (double)(fnow + Q); bvirt = Qnew - fnow; hh = Q - bvirt;
                    fnow = f[++findex];
                }
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
                while ((eindex < elen) && (findex < flen))
                {
                    if ((fnow > enow) == (fnow > -enow))
                    {
                        Qnew = (double)(Q + enow);
                        bvirt = (double)(Qnew - Q);
                        avirt = Qnew - bvirt;
                        bround = enow - bvirt;
                        around = Q - avirt;
                        hh = around + bround;

                        enow = e[++eindex];
                    }
                    else
                    {
                        Qnew = (double)(Q + fnow);
                        bvirt = (double)(Qnew - Q);
                        avirt = Qnew - bvirt;
                        bround = fnow - bvirt;
                        around = Q - avirt;
                        hh = around + bround;

                        fnow = f[++findex];
                    }
                    Q = Qnew;
                    if (hh != 0.0)
                    {
                        h[hindex++] = hh;
                    }
                }
            }
            while (eindex < elen)
            {
                Qnew = (double)(Q + enow);
                bvirt = (double)(Qnew - Q);
                avirt = Qnew - bvirt;
                bround = enow - bvirt;
                around = Q - avirt;
                hh = around + bround;

                enow = e[++eindex];
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            while (findex < flen)
            {
                Qnew = (double)(Q + fnow);
                bvirt = (double)(Qnew - Q);
                avirt = Qnew - bvirt;
                bround = fnow - bvirt;
                around = Q - avirt;
                hh = around + bround;

                fnow = f[++findex];
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            if ((Q != 0.0) || (hindex == 0))
            {
                h[hindex++] = Q;
            }
            return hindex;
        }

        /// <summary>
        /// Multiply an expansion by a scalar, eliminating zero components from the output expansion.  
        /// </summary>
        /// <param name="elen"></param>
        /// <param name="e"></param>
        /// <param name="b"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sets h = be.  See my Robust Predicates paper for details.
        /// 
        /// Maintains the nonoverlapping property.  If round-to-even is used (as with IEEE 754),
        /// maintains the strongly nonoverlapping and nonadjacent properties as well. (That is,
        /// if e has one of these properties, so will h.)
        /// </remarks>
        private int ScaleExpansionZeroElim(int elen, double[] e, double b, double[] h)
        {
            double Q, sum;
            double hh;
            double product1;
            double product0;
            int eindex, hindex;
            double enow;
            double bvirt;
            double avirt, bround, around;
            double c;
            double abig;
            double ahi, alo, bhi, blo;
            double err1, err2, err3;

            c = (double)(splitter * b); abig = (double)(c - b); bhi = c - abig; blo = b - bhi;
            Q = (double)(e[0] * b); c = (double)(splitter * e[0]); abig = (double)(c - e[0]); ahi = c - abig; alo = e[0] - ahi; err1 = Q - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); hh = (alo * blo) - err3;
            hindex = 0;
            if (hh != 0)
            {
                h[hindex++] = hh;
            }
            for (eindex = 1; eindex < elen; eindex++)
            {
                enow = e[eindex];
                product1 = (double)(enow * b); c = (double)(splitter * enow); abig = (double)(c - enow); ahi = c - abig; alo = enow - ahi; err1 = product1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); product0 = (alo * blo) - err3;
                sum = (double)(Q + product0); bvirt = (double)(sum - Q); avirt = sum - bvirt; bround = product0 - bvirt; around = Q - avirt; hh = around + bround;
                if (hh != 0)
                {
                    h[hindex++] = hh;
                }
                Q = (double)(product1 + sum); bvirt = Q - product1; hh = sum - bvirt;
                if (hh != 0)
                {
                    h[hindex++] = hh;
                }
            }
            if ((Q != 0.0) || (hindex == 0))
            {
                h[hindex++] = Q;
            }
            return hindex;
        }

        /// <summary>
        /// Produce a one-word estimate of an expansion's value. 
        /// </summary>
        /// <param name="elen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private double Estimate(int elen, double[] e)
        {
            double Q;
            int eindex;

            Q = e[0];
            for (eindex = 1; eindex < elen; eindex++)
            {
                Q += e[eindex];
            }
            return Q;
        }

        /// <summary>
        /// Return a positive value if the points pa, pb, and pc occur in counterclockwise
        /// order; a negative value if they occur in clockwise order; and zero if they are
        /// collinear. The result is also a rough approximation of twice the signed area of
        /// the triangle defined by the three points. 
        /// </summary>
        /// <param name="pa"></param>
        /// <param name="pb"></param>
        /// <param name="pc"></param>
        /// <param name="detsum"></param>
        /// <returns></returns>
        /// <remarks>
        /// Uses exact arithmetic if necessary to ensure a correct answer. The result returned
        /// is the determinant of a matrix. This determinant is computed adaptively, in the
        /// sense that exact arithmetic is used only to the degree it is needed to ensure that
        /// the returned value has the correct sign.  Hence, this function is usually quite fast,
        /// but will run more slowly when the input points are collinear or nearly so.
        /// </remarks>
        private double CounterClockwiseAdapt(Point pa, Point pb, Point pc, double detsum)
        {
            double acx, acy, bcx, bcy;
            double acxtail, acytail, bcxtail, bcytail;
            double detleft, detright;
            double detlefttail, detrighttail;
            double det, errbound;
            // Edited to work around index out of range exceptions (changed array length from 4 to 5).
            // See unsafe indexing in FastExpansionSumZeroElim.
            double[] B = new double[5], u = new double[5];
            double[] C1 = new double[8], C2 = new double[12], D = new double[16];
            double B3;
            int C1length, C2length, Dlength;

            double u3;
            double s1, t1;
            double s0, t0;

            double bvirt;
            double avirt, bround, around;
            double c;
            double abig;
            double ahi, alo, bhi, blo;
            double err1, err2, err3;
            double _i, _j;
            double _0;

            acx = (double)(pa.x - pc.x);
            bcx = (double)(pb.x - pc.x);
            acy = (double)(pa.y - pc.y);
            bcy = (double)(pb.y - pc.y);

            detleft = (double)(acx * bcy); c = (double)(splitter * acx); abig = (double)(c - acx); ahi = c - abig; alo = acx - ahi; c = (double)(splitter * bcy); abig = (double)(c - bcy); bhi = c - abig; blo = bcy - bhi; err1 = detleft - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); detlefttail = (alo * blo) - err3;
            detright = (double)(acy * bcx); c = (double)(splitter * acy); abig = (double)(c - acy); ahi = c - abig; alo = acy - ahi; c = (double)(splitter * bcx); abig = (double)(c - bcx); bhi = c - abig; blo = bcx - bhi; err1 = detright - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); detrighttail = (alo * blo) - err3;

            _i = (double)(detlefttail - detrighttail); bvirt = (double)(detlefttail - _i); avirt = _i + bvirt; bround = bvirt - detrighttail; around = detlefttail - avirt; B[0] = around + bround; _j = (double)(detleft + _i); bvirt = (double)(_j - detleft); avirt = _j - bvirt; bround = _i - bvirt; around = detleft - avirt; _0 = around + bround; _i = (double)(_0 - detright); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - detright; around = _0 - avirt; B[1] = around + bround; B3 = (double)(_j + _i); bvirt = (double)(B3 - _j); avirt = B3 - bvirt; bround = _i - bvirt; around = _j - avirt; B[2] = around + bround;

            B[3] = B3;

            det = Estimate(4, B);
            errbound = ccwerrboundB * detsum;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            bvirt = (double)(pa.x - acx); avirt = acx + bvirt; bround = bvirt - pc.x; around = pa.x - avirt; acxtail = around + bround;
            bvirt = (double)(pb.x - bcx); avirt = bcx + bvirt; bround = bvirt - pc.x; around = pb.x - avirt; bcxtail = around + bround;
            bvirt = (double)(pa.y - acy); avirt = acy + bvirt; bround = bvirt - pc.y; around = pa.y - avirt; acytail = around + bround;
            bvirt = (double)(pb.y - bcy); avirt = bcy + bvirt; bround = bvirt - pc.y; around = pb.y - avirt; bcytail = around + bround;

            if ((acxtail == 0.0) && (acytail == 0.0)
                && (bcxtail == 0.0) && (bcytail == 0.0))
            {
                return det;
            }

            errbound = ccwerrboundC * detsum + resulterrbound * ((det) >= 0.0 ? (det) : -(det));
            det += (acx * bcytail + bcy * acxtail)
                 - (acy * bcxtail + bcx * acytail);
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            s1 = (double)(acxtail * bcy); c = (double)(splitter * acxtail); abig = (double)(c - acxtail); ahi = c - abig; alo = acxtail - ahi; c = (double)(splitter * bcy); abig = (double)(c - bcy); bhi = c - abig; blo = bcy - bhi; err1 = s1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); s0 = (alo * blo) - err3;
            t1 = (double)(acytail * bcx); c = (double)(splitter * acytail); abig = (double)(c - acytail); ahi = c - abig; alo = acytail - ahi; c = (double)(splitter * bcx); abig = (double)(c - bcx); bhi = c - abig; blo = bcx - bhi; err1 = t1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); t0 = (alo * blo) - err3;
            _i = (double)(s0 - t0); bvirt = (double)(s0 - _i); avirt = _i + bvirt; bround = bvirt - t0; around = s0 - avirt; u[0] = around + bround; _j = (double)(s1 + _i); bvirt = (double)(_j - s1); avirt = _j - bvirt; bround = _i - bvirt; around = s1 - avirt; _0 = around + bround; _i = (double)(_0 - t1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - t1; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
            u[3] = u3;
            C1length = FastExpansionSumZeroElim(4, B, 4, u, C1);

            s1 = (double)(acx * bcytail); c = (double)(splitter * acx); abig = (double)(c - acx); ahi = c - abig; alo = acx - ahi; c = (double)(splitter * bcytail); abig = (double)(c - bcytail); bhi = c - abig; blo = bcytail - bhi; err1 = s1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); s0 = (alo * blo) - err3;
            t1 = (double)(acy * bcxtail); c = (double)(splitter * acy); abig = (double)(c - acy); ahi = c - abig; alo = acy - ahi; c = (double)(splitter * bcxtail); abig = (double)(c - bcxtail); bhi = c - abig; blo = bcxtail - bhi; err1 = t1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); t0 = (alo * blo) - err3;
            _i = (double)(s0 - t0); bvirt = (double)(s0 - _i); avirt = _i + bvirt; bround = bvirt - t0; around = s0 - avirt; u[0] = around + bround; _j = (double)(s1 + _i); bvirt = (double)(_j - s1); avirt = _j - bvirt; bround = _i - bvirt; around = s1 - avirt; _0 = around + bround; _i = (double)(_0 - t1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - t1; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
            u[3] = u3;
            C2length = FastExpansionSumZeroElim(C1length, C1, 4, u, C2);

            s1 = (double)(acxtail * bcytail); c = (double)(splitter * acxtail); abig = (double)(c - acxtail); ahi = c - abig; alo = acxtail - ahi; c = (double)(splitter * bcytail); abig = (double)(c - bcytail); bhi = c - abig; blo = bcytail - bhi; err1 = s1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); s0 = (alo * blo) - err3;
            t1 = (double)(acytail * bcxtail); c = (double)(splitter * acytail); abig = (double)(c - acytail); ahi = c - abig; alo = acytail - ahi; c = (double)(splitter * bcxtail); abig = (double)(c - bcxtail); bhi = c - abig; blo = bcxtail - bhi; err1 = t1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); t0 = (alo * blo) - err3;
            _i = (double)(s0 - t0); bvirt = (double)(s0 - _i); avirt = _i + bvirt; bround = bvirt - t0; around = s0 - avirt; u[0] = around + bround; _j = (double)(s1 + _i); bvirt = (double)(_j - s1); avirt = _j - bvirt; bround = _i - bvirt; around = s1 - avirt; _0 = around + bround; _i = (double)(_0 - t1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - t1; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
            u[3] = u3;
            Dlength = FastExpansionSumZeroElim(C2length, C2, 4, u, D);

            return (D[Dlength - 1]);
        }

        /// <summary>
        /// Return a positive value if the point pd lies inside the circle passing through
        /// pa, pb, and pc; a negative value if it lies outside; and zero if the four points
        /// are cocircular. The points pa, pb, and pc must be in counterclockwise order, or 
        /// the sign of the result will be reversed.
        /// </summary>
        /// <param name="pa"></param>
        /// <param name="pb"></param>
        /// <param name="pc"></param>
        /// <param name="pd"></param>
        /// <param name="permanent"></param>
        /// <returns></returns>
        /// <remarks>
        /// Uses exact arithmetic if necessary to ensure a correct answer. The result returned
        /// is the determinant of a matrix. This determinant is computed adaptively, in the
        /// sense that exact arithmetic is used only to the degree it is needed to ensure that
        /// the returned value has the correct sign. Hence, this function is usually quite fast,
        /// but will run more slowly when the input points are cocircular or nearly so.
        /// </remarks>
        private double InCircleAdapt(Point pa, Point pb, Point pc, Point pd, double permanent)
        {
            double adx, bdx, cdx, ady, bdy, cdy;
            double det, errbound;

            double bdxcdy1, cdxbdy1, cdxady1, adxcdy1, adxbdy1, bdxady1;
            double bdxcdy0, cdxbdy0, cdxady0, adxcdy0, adxbdy0, bdxady0;
            double[] bc = new double[4], ca = new double[4], ab = new double[4];
            double bc3, ca3, ab3;
            int axbclen, axxbclen, aybclen, ayybclen, alen;
            int bxcalen, bxxcalen, bycalen, byycalen, blen;
            int cxablen, cxxablen, cyablen, cyyablen, clen;
            int ablen;
            double[] finnow, finother, finswap;
            int finlength;

            double adxtail, bdxtail, cdxtail, adytail, bdytail, cdytail;
            double adxadx1, adyady1, bdxbdx1, bdybdy1, cdxcdx1, cdycdy1;
            double adxadx0, adyady0, bdxbdx0, bdybdy0, cdxcdx0, cdycdy0;
            double[] aa = new double[4], bb = new double[4], cc = new double[4];
            double aa3, bb3, cc3;
            double ti1, tj1;
            double ti0, tj0;
            // Edited to work around index out of range exceptions (changed array length from 4 to 5).
            // See unsafe indexing in FastExpansionSumZeroElim.
            double[] u = new double[5], v = new double[5];
            double u3, v3;
            int temp8len, temp16alen, temp16blen, temp16clen;
            int temp32alen, temp32blen, temp48len, temp64len;
            double[] axtbb = new double[8], axtcc = new double[8], aytbb = new double[8], aytcc = new double[8];
            int axtbblen, axtcclen, aytbblen, aytcclen;
            double[] bxtaa = new double[8], bxtcc = new double[8], bytaa = new double[8], bytcc = new double[8];
            int bxtaalen, bxtcclen, bytaalen, bytcclen;
            double[] cxtaa = new double[8], cxtbb = new double[8], cytaa = new double[8], cytbb = new double[8];
            int cxtaalen, cxtbblen, cytaalen, cytbblen;
            double[] axtbc = new double[8], aytbc = new double[8], bxtca = new double[8], bytca = new double[8], cxtab = new double[8], cytab = new double[8];
            int axtbclen = 0, aytbclen = 0, bxtcalen = 0, bytcalen = 0, cxtablen = 0, cytablen = 0;
            double[] axtbct = new double[16], aytbct = new double[16], bxtcat = new double[16], bytcat = new double[16], cxtabt = new double[16], cytabt = new double[16];
            int axtbctlen, aytbctlen, bxtcatlen, bytcatlen, cxtabtlen, cytabtlen;
            double[] axtbctt = new double[8], aytbctt = new double[8], bxtcatt = new double[8];
            double[] bytcatt = new double[8], cxtabtt = new double[8], cytabtt = new double[8];
            int axtbcttlen, aytbcttlen, bxtcattlen, bytcattlen, cxtabttlen, cytabttlen;
            double[] abt = new double[8], bct = new double[8], cat = new double[8];
            int abtlen, bctlen, catlen;
            double[] abtt = new double[4], bctt = new double[4], catt = new double[4];
            int abttlen, bcttlen, cattlen;
            double abtt3, bctt3, catt3;
            double negate;

            double bvirt;
            double avirt, bround, around;
            double c;
            double abig;
            double ahi, alo, bhi, blo;
            double err1, err2, err3;
            double _i, _j;
            double _0;

            adx = (double)(pa.x - pd.x);
            bdx = (double)(pb.x - pd.x);
            cdx = (double)(pc.x - pd.x);
            ady = (double)(pa.y - pd.y);
            bdy = (double)(pb.y - pd.y);
            cdy = (double)(pc.y - pd.y);

            adx = (double)(pa.x - pd.x);
            bdx = (double)(pb.x - pd.x);
            cdx = (double)(pc.x - pd.x);
            ady = (double)(pa.y - pd.y);
            bdy = (double)(pb.y - pd.y);
            cdy = (double)(pc.y - pd.y);

            bdxcdy1 = (double)(bdx * cdy); c = (double)(splitter * bdx); abig = (double)(c - bdx); ahi = c - abig; alo = bdx - ahi; c = (double)(splitter * cdy); abig = (double)(c - cdy); bhi = c - abig; blo = cdy - bhi; err1 = bdxcdy1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); bdxcdy0 = (alo * blo) - err3;
            cdxbdy1 = (double)(cdx * bdy); c = (double)(splitter * cdx); abig = (double)(c - cdx); ahi = c - abig; alo = cdx - ahi; c = (double)(splitter * bdy); abig = (double)(c - bdy); bhi = c - abig; blo = bdy - bhi; err1 = cdxbdy1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); cdxbdy0 = (alo * blo) - err3;
            _i = (double)(bdxcdy0 - cdxbdy0); bvirt = (double)(bdxcdy0 - _i); avirt = _i + bvirt; bround = bvirt - cdxbdy0; around = bdxcdy0 - avirt; bc[0] = around + bround; _j = (double)(bdxcdy1 + _i); bvirt = (double)(_j - bdxcdy1); avirt = _j - bvirt; bround = _i - bvirt; around = bdxcdy1 - avirt; _0 = around + bround; _i = (double)(_0 - cdxbdy1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - cdxbdy1; around = _0 - avirt; bc[1] = around + bround; bc3 = (double)(_j + _i); bvirt = (double)(bc3 - _j); avirt = bc3 - bvirt; bround = _i - bvirt; around = _j - avirt; bc[2] = around + bround;
            bc[3] = bc3;
            axbclen = ScaleExpansionZeroElim(4, bc, adx, axbc);
            axxbclen = ScaleExpansionZeroElim(axbclen, axbc, adx, axxbc);
            aybclen = ScaleExpansionZeroElim(4, bc, ady, aybc);
            ayybclen = ScaleExpansionZeroElim(aybclen, aybc, ady, ayybc);
            alen = FastExpansionSumZeroElim(axxbclen, axxbc, ayybclen, ayybc, adet);

            cdxady1 = (double)(cdx * ady); c = (double)(splitter * cdx); abig = (double)(c - cdx); ahi = c - abig; alo = cdx - ahi; c = (double)(splitter * ady); abig = (double)(c - ady); bhi = c - abig; blo = ady - bhi; err1 = cdxady1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); cdxady0 = (alo * blo) - err3;
            adxcdy1 = (double)(adx * cdy); c = (double)(splitter * adx); abig = (double)(c - adx); ahi = c - abig; alo = adx - ahi; c = (double)(splitter * cdy); abig = (double)(c - cdy); bhi = c - abig; blo = cdy - bhi; err1 = adxcdy1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); adxcdy0 = (alo * blo) - err3;
            _i = (double)(cdxady0 - adxcdy0); bvirt = (double)(cdxady0 - _i); avirt = _i + bvirt; bround = bvirt - adxcdy0; around = cdxady0 - avirt; ca[0] = around + bround; _j = (double)(cdxady1 + _i); bvirt = (double)(_j - cdxady1); avirt = _j - bvirt; bround = _i - bvirt; around = cdxady1 - avirt; _0 = around + bround; _i = (double)(_0 - adxcdy1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - adxcdy1; around = _0 - avirt; ca[1] = around + bround; ca3 = (double)(_j + _i); bvirt = (double)(ca3 - _j); avirt = ca3 - bvirt; bround = _i - bvirt; around = _j - avirt; ca[2] = around + bround;
            ca[3] = ca3;
            bxcalen = ScaleExpansionZeroElim(4, ca, bdx, bxca);
            bxxcalen = ScaleExpansionZeroElim(bxcalen, bxca, bdx, bxxca);
            bycalen = ScaleExpansionZeroElim(4, ca, bdy, byca);
            byycalen = ScaleExpansionZeroElim(bycalen, byca, bdy, byyca);
            blen = FastExpansionSumZeroElim(bxxcalen, bxxca, byycalen, byyca, bdet);

            adxbdy1 = (double)(adx * bdy); c = (double)(splitter * adx); abig = (double)(c - adx); ahi = c - abig; alo = adx - ahi; c = (double)(splitter * bdy); abig = (double)(c - bdy); bhi = c - abig; blo = bdy - bhi; err1 = adxbdy1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); adxbdy0 = (alo * blo) - err3;
            bdxady1 = (double)(bdx * ady); c = (double)(splitter * bdx); abig = (double)(c - bdx); ahi = c - abig; alo = bdx - ahi; c = (double)(splitter * ady); abig = (double)(c - ady); bhi = c - abig; blo = ady - bhi; err1 = bdxady1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); bdxady0 = (alo * blo) - err3;
            _i = (double)(adxbdy0 - bdxady0); bvirt = (double)(adxbdy0 - _i); avirt = _i + bvirt; bround = bvirt - bdxady0; around = adxbdy0 - avirt; ab[0] = around + bround; _j = (double)(adxbdy1 + _i); bvirt = (double)(_j - adxbdy1); avirt = _j - bvirt; bround = _i - bvirt; around = adxbdy1 - avirt; _0 = around + bround; _i = (double)(_0 - bdxady1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - bdxady1; around = _0 - avirt; ab[1] = around + bround; ab3 = (double)(_j + _i); bvirt = (double)(ab3 - _j); avirt = ab3 - bvirt; bround = _i - bvirt; around = _j - avirt; ab[2] = around + bround;
            ab[3] = ab3;
            cxablen = ScaleExpansionZeroElim(4, ab, cdx, cxab);
            cxxablen = ScaleExpansionZeroElim(cxablen, cxab, cdx, cxxab);
            cyablen = ScaleExpansionZeroElim(4, ab, cdy, cyab);
            cyyablen = ScaleExpansionZeroElim(cyablen, cyab, cdy, cyyab);
            clen = FastExpansionSumZeroElim(cxxablen, cxxab, cyyablen, cyyab, cdet);

            ablen = FastExpansionSumZeroElim(alen, adet, blen, bdet, abdet);
            finlength = FastExpansionSumZeroElim(ablen, abdet, clen, cdet, fin1);

            det = Estimate(finlength, fin1);
            errbound = iccerrboundB * permanent;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            bvirt = (double)(pa.x - adx); avirt = adx + bvirt; bround = bvirt - pd.x; around = pa.x - avirt; adxtail = around + bround;
            bvirt = (double)(pa.y - ady); avirt = ady + bvirt; bround = bvirt - pd.y; around = pa.y - avirt; adytail = around + bround;
            bvirt = (double)(pb.x - bdx); avirt = bdx + bvirt; bround = bvirt - pd.x; around = pb.x - avirt; bdxtail = around + bround;
            bvirt = (double)(pb.y - bdy); avirt = bdy + bvirt; bround = bvirt - pd.y; around = pb.y - avirt; bdytail = around + bround;
            bvirt = (double)(pc.x - cdx); avirt = cdx + bvirt; bround = bvirt - pd.x; around = pc.x - avirt; cdxtail = around + bround;
            bvirt = (double)(pc.y - cdy); avirt = cdy + bvirt; bround = bvirt - pd.y; around = pc.y - avirt; cdytail = around + bround;
            if ((adxtail == 0.0) && (bdxtail == 0.0) && (cdxtail == 0.0)
                && (adytail == 0.0) && (bdytail == 0.0) && (cdytail == 0.0))
            {
                return det;
            }

            errbound = iccerrboundC * permanent + resulterrbound * ((det) >= 0.0 ? (det) : -(det));
            det += ((adx * adx + ady * ady) * ((bdx * cdytail + cdy * bdxtail) - (bdy * cdxtail + cdx * bdytail))
                    + 2.0 * (adx * adxtail + ady * adytail) * (bdx * cdy - bdy * cdx))
                 + ((bdx * bdx + bdy * bdy) * ((cdx * adytail + ady * cdxtail) - (cdy * adxtail + adx * cdytail))
                    + 2.0 * (bdx * bdxtail + bdy * bdytail) * (cdx * ady - cdy * adx))
                 + ((cdx * cdx + cdy * cdy) * ((adx * bdytail + bdy * adxtail) - (ady * bdxtail + bdx * adytail))
                    + 2.0 * (cdx * cdxtail + cdy * cdytail) * (adx * bdy - ady * bdx));
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            finnow = fin1;
            finother = fin2;

            if ((bdxtail != 0.0) || (bdytail != 0.0) || (cdxtail != 0.0) || (cdytail != 0.0))
            {
                adxadx1 = (double)(adx * adx); c = (double)(splitter * adx); abig = (double)(c - adx); ahi = c - abig; alo = adx - ahi; err1 = adxadx1 - (ahi * ahi); err3 = err1 - ((ahi + ahi) * alo); adxadx0 = (alo * alo) - err3;
                adyady1 = (double)(ady * ady); c = (double)(splitter * ady); abig = (double)(c - ady); ahi = c - abig; alo = ady - ahi; err1 = adyady1 - (ahi * ahi); err3 = err1 - ((ahi + ahi) * alo); adyady0 = (alo * alo) - err3;
                _i = (double)(adxadx0 + adyady0); bvirt = (double)(_i - adxadx0); avirt = _i - bvirt; bround = adyady0 - bvirt; around = adxadx0 - avirt; aa[0] = around + bround; _j = (double)(adxadx1 + _i); bvirt = (double)(_j - adxadx1); avirt = _j - bvirt; bround = _i - bvirt; around = adxadx1 - avirt; _0 = around + bround; _i = (double)(_0 + adyady1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = adyady1 - bvirt; around = _0 - avirt; aa[1] = around + bround; aa3 = (double)(_j + _i); bvirt = (double)(aa3 - _j); avirt = aa3 - bvirt; bround = _i - bvirt; around = _j - avirt; aa[2] = around + bround;
                aa[3] = aa3;
            }
            if ((cdxtail != 0.0) || (cdytail != 0.0) || (adxtail != 0.0) || (adytail != 0.0))
            {
                bdxbdx1 = (double)(bdx * bdx); c = (double)(splitter * bdx); abig = (double)(c - bdx); ahi = c - abig; alo = bdx - ahi; err1 = bdxbdx1 - (ahi * ahi); err3 = err1 - ((ahi + ahi) * alo); bdxbdx0 = (alo * alo) - err3;
                bdybdy1 = (double)(bdy * bdy); c = (double)(splitter * bdy); abig = (double)(c - bdy); ahi = c - abig; alo = bdy - ahi; err1 = bdybdy1 - (ahi * ahi); err3 = err1 - ((ahi + ahi) * alo); bdybdy0 = (alo * alo) - err3;
                _i = (double)(bdxbdx0 + bdybdy0); bvirt = (double)(_i - bdxbdx0); avirt = _i - bvirt; bround = bdybdy0 - bvirt; around = bdxbdx0 - avirt; bb[0] = around + bround; _j = (double)(bdxbdx1 + _i); bvirt = (double)(_j - bdxbdx1); avirt = _j - bvirt; bround = _i - bvirt; around = bdxbdx1 - avirt; _0 = around + bround; _i = (double)(_0 + bdybdy1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = bdybdy1 - bvirt; around = _0 - avirt; bb[1] = around + bround; bb3 = (double)(_j + _i); bvirt = (double)(bb3 - _j); avirt = bb3 - bvirt; bround = _i - bvirt; around = _j - avirt; bb[2] = around + bround;
                bb[3] = bb3;
            }
            if ((adxtail != 0.0) || (adytail != 0.0) || (bdxtail != 0.0) || (bdytail != 0.0))
            {
                cdxcdx1 = (double)(cdx * cdx); c = (double)(splitter * cdx); abig = (double)(c - cdx); ahi = c - abig; alo = cdx - ahi; err1 = cdxcdx1 - (ahi * ahi); err3 = err1 - ((ahi + ahi) * alo); cdxcdx0 = (alo * alo) - err3;
                cdycdy1 = (double)(cdy * cdy); c = (double)(splitter * cdy); abig = (double)(c - cdy); ahi = c - abig; alo = cdy - ahi; err1 = cdycdy1 - (ahi * ahi); err3 = err1 - ((ahi + ahi) * alo); cdycdy0 = (alo * alo) - err3;
                _i = (double)(cdxcdx0 + cdycdy0); bvirt = (double)(_i - cdxcdx0); avirt = _i - bvirt; bround = cdycdy0 - bvirt; around = cdxcdx0 - avirt; cc[0] = around + bround; _j = (double)(cdxcdx1 + _i); bvirt = (double)(_j - cdxcdx1); avirt = _j - bvirt; bround = _i - bvirt; around = cdxcdx1 - avirt; _0 = around + bround; _i = (double)(_0 + cdycdy1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = cdycdy1 - bvirt; around = _0 - avirt; cc[1] = around + bround; cc3 = (double)(_j + _i); bvirt = (double)(cc3 - _j); avirt = cc3 - bvirt; bround = _i - bvirt; around = _j - avirt; cc[2] = around + bround;
                cc[3] = cc3;
            }

            if (adxtail != 0.0)
            {
                axtbclen = ScaleExpansionZeroElim(4, bc, adxtail, axtbc);
                temp16alen = ScaleExpansionZeroElim(axtbclen, axtbc, 2.0 * adx, temp16a);

                axtcclen = ScaleExpansionZeroElim(4, cc, adxtail, axtcc);
                temp16blen = ScaleExpansionZeroElim(axtcclen, axtcc, bdy, temp16b);

                axtbblen = ScaleExpansionZeroElim(4, bb, adxtail, axtbb);
                temp16clen = ScaleExpansionZeroElim(axtbblen, axtbb, -cdy, temp16c);

                temp32alen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (adytail != 0.0)
            {
                aytbclen = ScaleExpansionZeroElim(4, bc, adytail, aytbc);
                temp16alen = ScaleExpansionZeroElim(aytbclen, aytbc, 2.0 * ady, temp16a);

                aytbblen = ScaleExpansionZeroElim(4, bb, adytail, aytbb);
                temp16blen = ScaleExpansionZeroElim(aytbblen, aytbb, cdx, temp16b);

                aytcclen = ScaleExpansionZeroElim(4, cc, adytail, aytcc);
                temp16clen = ScaleExpansionZeroElim(aytcclen, aytcc, -bdx, temp16c);

                temp32alen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (bdxtail != 0.0)
            {
                bxtcalen = ScaleExpansionZeroElim(4, ca, bdxtail, bxtca);
                temp16alen = ScaleExpansionZeroElim(bxtcalen, bxtca, 2.0 * bdx, temp16a);

                bxtaalen = ScaleExpansionZeroElim(4, aa, bdxtail, bxtaa);
                temp16blen = ScaleExpansionZeroElim(bxtaalen, bxtaa, cdy, temp16b);

                bxtcclen = ScaleExpansionZeroElim(4, cc, bdxtail, bxtcc);
                temp16clen = ScaleExpansionZeroElim(bxtcclen, bxtcc, -ady, temp16c);

                temp32alen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (bdytail != 0.0)
            {
                bytcalen = ScaleExpansionZeroElim(4, ca, bdytail, bytca);
                temp16alen = ScaleExpansionZeroElim(bytcalen, bytca, 2.0 * bdy, temp16a);

                bytcclen = ScaleExpansionZeroElim(4, cc, bdytail, bytcc);
                temp16blen = ScaleExpansionZeroElim(bytcclen, bytcc, adx, temp16b);

                bytaalen = ScaleExpansionZeroElim(4, aa, bdytail, bytaa);
                temp16clen = ScaleExpansionZeroElim(bytaalen, bytaa, -cdx, temp16c);

                temp32alen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (cdxtail != 0.0)
            {
                cxtablen = ScaleExpansionZeroElim(4, ab, cdxtail, cxtab);
                temp16alen = ScaleExpansionZeroElim(cxtablen, cxtab, 2.0 * cdx, temp16a);

                cxtbblen = ScaleExpansionZeroElim(4, bb, cdxtail, cxtbb);
                temp16blen = ScaleExpansionZeroElim(cxtbblen, cxtbb, ady, temp16b);

                cxtaalen = ScaleExpansionZeroElim(4, aa, cdxtail, cxtaa);
                temp16clen = ScaleExpansionZeroElim(cxtaalen, cxtaa, -bdy, temp16c);

                temp32alen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }
            if (cdytail != 0.0)
            {
                cytablen = ScaleExpansionZeroElim(4, ab, cdytail, cytab);
                temp16alen = ScaleExpansionZeroElim(cytablen, cytab, 2.0 * cdy, temp16a);

                cytaalen = ScaleExpansionZeroElim(4, aa, cdytail, cytaa);
                temp16blen = ScaleExpansionZeroElim(cytaalen, cytaa, bdx, temp16b);

                cytbblen = ScaleExpansionZeroElim(4, bb, cdytail, cytbb);
                temp16clen = ScaleExpansionZeroElim(cytbblen, cytbb, -adx, temp16c);

                temp32alen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32a);
                temp48len = FastExpansionSumZeroElim(temp16clen, temp16c, temp32alen, temp32a, temp48);
                finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                finswap = finnow; finnow = finother; finother = finswap;
            }

            if ((adxtail != 0.0) || (adytail != 0.0))
            {
                if ((bdxtail != 0.0) || (bdytail != 0.0)
                    || (cdxtail != 0.0) || (cdytail != 0.0))
                {
                    ti1 = (double)(bdxtail * cdy); c = (double)(splitter * bdxtail); abig = (double)(c - bdxtail); ahi = c - abig; alo = bdxtail - ahi; c = (double)(splitter * cdy); abig = (double)(c - cdy); bhi = c - abig; blo = cdy - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    tj1 = (double)(bdx * cdytail); c = (double)(splitter * bdx); abig = (double)(c - bdx); ahi = c - abig; alo = bdx - ahi; c = (double)(splitter * cdytail); abig = (double)(c - cdytail); bhi = c - abig; blo = cdytail - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 + tj0); bvirt = (double)(_i - ti0); avirt = _i - bvirt; bround = tj0 - bvirt; around = ti0 - avirt; u[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 + tj1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = tj1 - bvirt; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
                    u[3] = u3;
                    negate = -bdy;
                    ti1 = (double)(cdxtail * negate); c = (double)(splitter * cdxtail); abig = (double)(c - cdxtail); ahi = c - abig; alo = cdxtail - ahi; c = (double)(splitter * negate); abig = (double)(c - negate); bhi = c - abig; blo = negate - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    negate = -bdytail;
                    tj1 = (double)(cdx * negate); c = (double)(splitter * cdx); abig = (double)(c - cdx); ahi = c - abig; alo = cdx - ahi; c = (double)(splitter * negate); abig = (double)(c - negate); bhi = c - abig; blo = negate - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 + tj0); bvirt = (double)(_i - ti0); avirt = _i - bvirt; bround = tj0 - bvirt; around = ti0 - avirt; v[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 + tj1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = tj1 - bvirt; around = _0 - avirt; v[1] = around + bround; v3 = (double)(_j + _i); bvirt = (double)(v3 - _j); avirt = v3 - bvirt; bround = _i - bvirt; around = _j - avirt; v[2] = around + bround;
                    v[3] = v3;
                    bctlen = FastExpansionSumZeroElim(4, u, 4, v, bct);

                    ti1 = (double)(bdxtail * cdytail); c = (double)(splitter * bdxtail); abig = (double)(c - bdxtail); ahi = c - abig; alo = bdxtail - ahi; c = (double)(splitter * cdytail); abig = (double)(c - cdytail); bhi = c - abig; blo = cdytail - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    tj1 = (double)(cdxtail * bdytail); c = (double)(splitter * cdxtail); abig = (double)(c - cdxtail); ahi = c - abig; alo = cdxtail - ahi; c = (double)(splitter * bdytail); abig = (double)(c - bdytail); bhi = c - abig; blo = bdytail - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 - tj0); bvirt = (double)(ti0 - _i); avirt = _i + bvirt; bround = bvirt - tj0; around = ti0 - avirt; bctt[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 - tj1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - tj1; around = _0 - avirt; bctt[1] = around + bround; bctt3 = (double)(_j + _i); bvirt = (double)(bctt3 - _j); avirt = bctt3 - bvirt; bround = _i - bvirt; around = _j - avirt; bctt[2] = around + bround;
                    bctt[3] = bctt3;
                    bcttlen = 4;
                }
                else
                {
                    bct[0] = 0.0;
                    bctlen = 1;
                    bctt[0] = 0.0;
                    bcttlen = 1;
                }

                if (adxtail != 0.0)
                {
                    temp16alen = ScaleExpansionZeroElim(axtbclen, axtbc, adxtail, temp16a);
                    axtbctlen = ScaleExpansionZeroElim(bctlen, bct, adxtail, axtbct);
                    temp32alen = ScaleExpansionZeroElim(axtbctlen, axtbct, 2.0 * adx, temp32a);
                    temp48len = FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (bdytail != 0.0)
                    {
                        temp8len = ScaleExpansionZeroElim(4, cc, adxtail, temp8);
                        temp16alen = ScaleExpansionZeroElim(temp8len, temp8, bdytail, temp16a);
                        finlength = FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                    if (cdytail != 0.0)
                    {
                        temp8len = ScaleExpansionZeroElim(4, bb, -adxtail, temp8);
                        temp16alen = ScaleExpansionZeroElim(temp8len, temp8, cdytail, temp16a);
                        finlength = FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }

                    temp32alen = ScaleExpansionZeroElim(axtbctlen, axtbct, adxtail, temp32a);
                    axtbcttlen = ScaleExpansionZeroElim(bcttlen, bctt, adxtail, axtbctt);
                    temp16alen = ScaleExpansionZeroElim(axtbcttlen, axtbctt, 2.0 * adx, temp16a);
                    temp16blen = ScaleExpansionZeroElim(axtbcttlen, axtbctt, adxtail, temp16b);
                    temp32blen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
                if (adytail != 0.0)
                {
                    temp16alen = ScaleExpansionZeroElim(aytbclen, aytbc, adytail, temp16a);
                    aytbctlen = ScaleExpansionZeroElim(bctlen, bct, adytail, aytbct);
                    temp32alen = ScaleExpansionZeroElim(aytbctlen, aytbct, 2.0 * ady, temp32a);
                    temp48len = FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;


                    temp32alen = ScaleExpansionZeroElim(aytbctlen, aytbct, adytail, temp32a);
                    aytbcttlen = ScaleExpansionZeroElim(bcttlen, bctt, adytail, aytbctt);
                    temp16alen = ScaleExpansionZeroElim(aytbcttlen, aytbctt, 2.0 * ady, temp16a);
                    temp16blen = ScaleExpansionZeroElim(aytbcttlen, aytbctt, adytail, temp16b);
                    temp32blen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
            }
            if ((bdxtail != 0.0) || (bdytail != 0.0))
            {
                if ((cdxtail != 0.0) || (cdytail != 0.0)
                    || (adxtail != 0.0) || (adytail != 0.0))
                {
                    ti1 = (double)(cdxtail * ady); c = (double)(splitter * cdxtail); abig = (double)(c - cdxtail); ahi = c - abig; alo = cdxtail - ahi; c = (double)(splitter * ady); abig = (double)(c - ady); bhi = c - abig; blo = ady - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    tj1 = (double)(cdx * adytail); c = (double)(splitter * cdx); abig = (double)(c - cdx); ahi = c - abig; alo = cdx - ahi; c = (double)(splitter * adytail); abig = (double)(c - adytail); bhi = c - abig; blo = adytail - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 + tj0); bvirt = (double)(_i - ti0); avirt = _i - bvirt; bround = tj0 - bvirt; around = ti0 - avirt; u[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 + tj1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = tj1 - bvirt; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
                    u[3] = u3;
                    negate = -cdy;
                    ti1 = (double)(adxtail * negate); c = (double)(splitter * adxtail); abig = (double)(c - adxtail); ahi = c - abig; alo = adxtail - ahi; c = (double)(splitter * negate); abig = (double)(c - negate); bhi = c - abig; blo = negate - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    negate = -cdytail;
                    tj1 = (double)(adx * negate); c = (double)(splitter * adx); abig = (double)(c - adx); ahi = c - abig; alo = adx - ahi; c = (double)(splitter * negate); abig = (double)(c - negate); bhi = c - abig; blo = negate - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 + tj0); bvirt = (double)(_i - ti0); avirt = _i - bvirt; bround = tj0 - bvirt; around = ti0 - avirt; v[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 + tj1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = tj1 - bvirt; around = _0 - avirt; v[1] = around + bround; v3 = (double)(_j + _i); bvirt = (double)(v3 - _j); avirt = v3 - bvirt; bround = _i - bvirt; around = _j - avirt; v[2] = around + bround;
                    v[3] = v3;
                    catlen = FastExpansionSumZeroElim(4, u, 4, v, cat);

                    ti1 = (double)(cdxtail * adytail); c = (double)(splitter * cdxtail); abig = (double)(c - cdxtail); ahi = c - abig; alo = cdxtail - ahi; c = (double)(splitter * adytail); abig = (double)(c - adytail); bhi = c - abig; blo = adytail - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    tj1 = (double)(adxtail * cdytail); c = (double)(splitter * adxtail); abig = (double)(c - adxtail); ahi = c - abig; alo = adxtail - ahi; c = (double)(splitter * cdytail); abig = (double)(c - cdytail); bhi = c - abig; blo = cdytail - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 - tj0); bvirt = (double)(ti0 - _i); avirt = _i + bvirt; bround = bvirt - tj0; around = ti0 - avirt; catt[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 - tj1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - tj1; around = _0 - avirt; catt[1] = around + bround; catt3 = (double)(_j + _i); bvirt = (double)(catt3 - _j); avirt = catt3 - bvirt; bround = _i - bvirt; around = _j - avirt; catt[2] = around + bround;
                    catt[3] = catt3;
                    cattlen = 4;
                }
                else
                {
                    cat[0] = 0.0;
                    catlen = 1;
                    catt[0] = 0.0;
                    cattlen = 1;
                }

                if (bdxtail != 0.0)
                {
                    temp16alen = ScaleExpansionZeroElim(bxtcalen, bxtca, bdxtail, temp16a);
                    bxtcatlen = ScaleExpansionZeroElim(catlen, cat, bdxtail, bxtcat);
                    temp32alen = ScaleExpansionZeroElim(bxtcatlen, bxtcat, 2.0 * bdx, temp32a);
                    temp48len = FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (cdytail != 0.0)
                    {
                        temp8len = ScaleExpansionZeroElim(4, aa, bdxtail, temp8);
                        temp16alen = ScaleExpansionZeroElim(temp8len, temp8, cdytail, temp16a);
                        finlength = FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                    if (adytail != 0.0)
                    {
                        temp8len = ScaleExpansionZeroElim(4, cc, -bdxtail, temp8);
                        temp16alen = ScaleExpansionZeroElim(temp8len, temp8, adytail, temp16a);
                        finlength = FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }

                    temp32alen = ScaleExpansionZeroElim(bxtcatlen, bxtcat, bdxtail, temp32a);
                    bxtcattlen = ScaleExpansionZeroElim(cattlen, catt, bdxtail, bxtcatt);
                    temp16alen = ScaleExpansionZeroElim(bxtcattlen, bxtcatt, 2.0 * bdx, temp16a);
                    temp16blen = ScaleExpansionZeroElim(bxtcattlen, bxtcatt, bdxtail, temp16b);
                    temp32blen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
                if (bdytail != 0.0)
                {
                    temp16alen = ScaleExpansionZeroElim(bytcalen, bytca, bdytail, temp16a);
                    bytcatlen = ScaleExpansionZeroElim(catlen, cat, bdytail, bytcat);
                    temp32alen = ScaleExpansionZeroElim(bytcatlen, bytcat, 2.0 * bdy, temp32a);
                    temp48len = FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;

                    temp32alen = ScaleExpansionZeroElim(bytcatlen, bytcat, bdytail, temp32a);
                    bytcattlen = ScaleExpansionZeroElim(cattlen, catt, bdytail, bytcatt);
                    temp16alen = ScaleExpansionZeroElim(bytcattlen, bytcatt, 2.0 * bdy, temp16a);
                    temp16blen = ScaleExpansionZeroElim(bytcattlen, bytcatt, bdytail, temp16b);
                    temp32blen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
            }
            if ((cdxtail != 0.0) || (cdytail != 0.0))
            {
                if ((adxtail != 0.0) || (adytail != 0.0)
                    || (bdxtail != 0.0) || (bdytail != 0.0))
                {
                    ti1 = (double)(adxtail * bdy); c = (double)(splitter * adxtail); abig = (double)(c - adxtail); ahi = c - abig; alo = adxtail - ahi; c = (double)(splitter * bdy); abig = (double)(c - bdy); bhi = c - abig; blo = bdy - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    tj1 = (double)(adx * bdytail); c = (double)(splitter * adx); abig = (double)(c - adx); ahi = c - abig; alo = adx - ahi; c = (double)(splitter * bdytail); abig = (double)(c - bdytail); bhi = c - abig; blo = bdytail - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 + tj0); bvirt = (double)(_i - ti0); avirt = _i - bvirt; bround = tj0 - bvirt; around = ti0 - avirt; u[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 + tj1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = tj1 - bvirt; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
                    u[3] = u3;
                    negate = -ady;
                    ti1 = (double)(bdxtail * negate); c = (double)(splitter * bdxtail); abig = (double)(c - bdxtail); ahi = c - abig; alo = bdxtail - ahi; c = (double)(splitter * negate); abig = (double)(c - negate); bhi = c - abig; blo = negate - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    negate = -adytail;
                    tj1 = (double)(bdx * negate); c = (double)(splitter * bdx); abig = (double)(c - bdx); ahi = c - abig; alo = bdx - ahi; c = (double)(splitter * negate); abig = (double)(c - negate); bhi = c - abig; blo = negate - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 + tj0); bvirt = (double)(_i - ti0); avirt = _i - bvirt; bround = tj0 - bvirt; around = ti0 - avirt; v[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 + tj1); bvirt = (double)(_i - _0); avirt = _i - bvirt; bround = tj1 - bvirt; around = _0 - avirt; v[1] = around + bround; v3 = (double)(_j + _i); bvirt = (double)(v3 - _j); avirt = v3 - bvirt; bround = _i - bvirt; around = _j - avirt; v[2] = around + bround;
                    v[3] = v3;
                    abtlen = FastExpansionSumZeroElim(4, u, 4, v, abt);

                    ti1 = (double)(adxtail * bdytail); c = (double)(splitter * adxtail); abig = (double)(c - adxtail); ahi = c - abig; alo = adxtail - ahi; c = (double)(splitter * bdytail); abig = (double)(c - bdytail); bhi = c - abig; blo = bdytail - bhi; err1 = ti1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); ti0 = (alo * blo) - err3;
                    tj1 = (double)(bdxtail * adytail); c = (double)(splitter * bdxtail); abig = (double)(c - bdxtail); ahi = c - abig; alo = bdxtail - ahi; c = (double)(splitter * adytail); abig = (double)(c - adytail); bhi = c - abig; blo = adytail - bhi; err1 = tj1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); tj0 = (alo * blo) - err3;
                    _i = (double)(ti0 - tj0); bvirt = (double)(ti0 - _i); avirt = _i + bvirt; bround = bvirt - tj0; around = ti0 - avirt; abtt[0] = around + bround; _j = (double)(ti1 + _i); bvirt = (double)(_j - ti1); avirt = _j - bvirt; bround = _i - bvirt; around = ti1 - avirt; _0 = around + bround; _i = (double)(_0 - tj1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - tj1; around = _0 - avirt; abtt[1] = around + bround; abtt3 = (double)(_j + _i); bvirt = (double)(abtt3 - _j); avirt = abtt3 - bvirt; bround = _i - bvirt; around = _j - avirt; abtt[2] = around + bround;
                    abtt[3] = abtt3;
                    abttlen = 4;
                }
                else
                {
                    abt[0] = 0.0;
                    abtlen = 1;
                    abtt[0] = 0.0;
                    abttlen = 1;
                }

                if (cdxtail != 0.0)
                {
                    temp16alen = ScaleExpansionZeroElim(cxtablen, cxtab, cdxtail, temp16a);
                    cxtabtlen = ScaleExpansionZeroElim(abtlen, abt, cdxtail, cxtabt);
                    temp32alen = ScaleExpansionZeroElim(cxtabtlen, cxtabt, 2.0 * cdx, temp32a);
                    temp48len = FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                    if (adytail != 0.0)
                    {
                        temp8len = ScaleExpansionZeroElim(4, bb, cdxtail, temp8);
                        temp16alen = ScaleExpansionZeroElim(temp8len, temp8, adytail, temp16a);
                        finlength = FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }
                    if (bdytail != 0.0)
                    {
                        temp8len = ScaleExpansionZeroElim(4, aa, -cdxtail, temp8);
                        temp16alen = ScaleExpansionZeroElim(temp8len, temp8, bdytail, temp16a);
                        finlength = FastExpansionSumZeroElim(finlength, finnow, temp16alen, temp16a, finother);
                        finswap = finnow; finnow = finother; finother = finswap;
                    }

                    temp32alen = ScaleExpansionZeroElim(cxtabtlen, cxtabt, cdxtail, temp32a);
                    cxtabttlen = ScaleExpansionZeroElim(abttlen, abtt, cdxtail, cxtabtt);
                    temp16alen = ScaleExpansionZeroElim(cxtabttlen, cxtabtt, 2.0 * cdx, temp16a);
                    temp16blen = ScaleExpansionZeroElim(cxtabttlen, cxtabtt, cdxtail, temp16b);
                    temp32blen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
                if (cdytail != 0.0)
                {
                    temp16alen = ScaleExpansionZeroElim(cytablen, cytab, cdytail, temp16a);
                    cytabtlen = ScaleExpansionZeroElim(abtlen, abt, cdytail, cytabt);
                    temp32alen = ScaleExpansionZeroElim(cytabtlen, cytabt, 2.0 * cdy, temp32a);
                    temp48len = FastExpansionSumZeroElim(temp16alen, temp16a, temp32alen, temp32a, temp48);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp48len, temp48, finother);
                    finswap = finnow; finnow = finother; finother = finswap;


                    temp32alen = ScaleExpansionZeroElim(cytabtlen, cytabt, cdytail, temp32a);
                    cytabttlen = ScaleExpansionZeroElim(abttlen, abtt, cdytail, cytabtt);
                    temp16alen = ScaleExpansionZeroElim(cytabttlen, cytabtt, 2.0 * cdy, temp16a);
                    temp16blen = ScaleExpansionZeroElim(cytabttlen, cytabtt, cdytail, temp16b);
                    temp32blen = FastExpansionSumZeroElim(temp16alen, temp16a, temp16blen, temp16b, temp32b);
                    temp64len = FastExpansionSumZeroElim(temp32alen, temp32a, temp32blen, temp32b, temp64);
                    finlength = FastExpansionSumZeroElim(finlength, finnow, temp64len, temp64, finother);
                    finswap = finnow; finnow = finother; finother = finswap;
                }
            }

            return finnow[finlength - 1];
        }

        #region Workspace

        // InCircleAdapt workspace:
        double[] fin1, fin2, abdet;

        double[] axbc, axxbc, aybc, ayybc, adet;
        double[] bxca, bxxca, byca, byyca, bdet;
        double[] cxab, cxxab, cyab, cyyab, cdet;

        double[] temp8, temp16a, temp16b, temp16c;
        double[] temp32a, temp32b, temp48, temp64;

        private void AllocateWorkspace()
        {
            fin1 = new double[1152];
            fin2 = new double[1152];
            abdet = new double[64];

            axbc = new double[8];
            axxbc = new double[16];
            aybc = new double[8];
            ayybc = new double[16];
            adet = new double[32];

            bxca = new double[8];
            bxxca = new double[16];
            byca = new double[8];
            byyca = new double[16];
            bdet = new double[32];

            cxab = new double[8];
            cxxab = new double[16];
            cyab = new double[8];
            cyyab = new double[16];
            cdet = new double[32];

            temp8 = new double[8];
            temp16a = new double[16];
            temp16b = new double[16];
            temp16c = new double[16];

            temp32a = new double[32];
            temp32b = new double[32];
            temp48 = new double[48];
            temp64 = new double[64];
        }

        private void ClearWorkspace()
        {
        }

        #endregion

        #endregion
    }
}
