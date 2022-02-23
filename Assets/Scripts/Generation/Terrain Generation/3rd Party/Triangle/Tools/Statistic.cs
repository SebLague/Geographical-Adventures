// -----------------------------------------------------------------------
// <copyright file="Statistic.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;

    /// <summary>
    /// Gather mesh statistics.
    /// </summary>
    public class Statistic
    {
        #region Static members

        /// <summary>
        /// Number of incircle tests performed.
        /// </summary>
        public static long InCircleCount = 0;
        public static long InCircleAdaptCount = 0;

        /// <summary>
        /// Number of counterclockwise tests performed.
        /// </summary>
        public static long CounterClockwiseCount = 0;
        public static long CounterClockwiseAdaptCount = 0;

        /// <summary>
        /// Number of 3D orientation tests performed.
        /// </summary>
        public static long Orient3dCount = 0;

        /// <summary>
        /// Number of right-of-hyperbola tests performed.
        /// </summary>
        public static long HyperbolaCount = 0;

        /// <summary>
        /// // Number of circumcenter calculations performed.
        /// </summary>
        public static long CircumcenterCount = 0;

        /// <summary>
        /// Number of circle top calculations performed.
        /// </summary>
        public static long CircleTopCount = 0;

        /// <summary>
        /// Number of vertex relocations.
        /// </summary>
        public static long RelocationCount = 0;

        #endregion

        #region Properties

        double minEdge = 0;
        /// <summary>
        /// Gets the shortest edge.
        /// </summary>
        public double ShortestEdge { get { return minEdge; } }

        double maxEdge = 0;
        /// <summary>
        /// Gets the longest edge.
        /// </summary>
        public double LongestEdge { get { return maxEdge; } }

        //
        double minAspect = 0;
        /// <summary>
        /// Gets the shortest altitude.
        /// </summary>
        public double ShortestAltitude { get { return minAspect; } }

        double maxAspect = 0;
        /// <summary>
        /// Gets the largest aspect ratio.
        /// </summary>
        public double LargestAspectRatio { get { return maxAspect; } }

        double minArea = 0;
        /// <summary>
        /// Gets the smallest area.
        /// </summary>
        public double SmallestArea { get { return minArea; } }

        double maxArea = 0;
        /// <summary>
        /// Gets the largest area.
        /// </summary>
        public double LargestArea { get { return maxArea; } }

        double minAngle = 0;
        /// <summary>
        /// Gets the smallest angle.
        /// </summary>
        public double SmallestAngle { get { return minAngle; } }

        double maxAngle = 0;
        /// <summary>
        /// Gets the largest angle.
        /// </summary>
        public double LargestAngle { get { return maxAngle; } }

        int[] angleTable;
        /// <summary>
        /// Gets the angle histogram.
        /// </summary>
        public int[] AngleHistogram { get { return angleTable; } }

        int[] minAngles;
        /// <summary>
        /// Gets the min angles histogram.
        /// </summary>
        public int[] MinAngleHistogram { get { return minAngles; } }

        int[] maxAngles;
        /// <summary>
        /// Gets the max angles histogram.
        /// </summary>
        public int[] MaxAngleHistogram { get { return maxAngles; } }

        #endregion

        #region Private methods

        private void GetAspectHistogram(Mesh mesh)
        {
            int[] aspecttable;
            double[] ratiotable;

            aspecttable = new int[16];
            ratiotable = new double[] {
                1.5, 2.0, 2.5, 3.0, 4.0, 6.0, 10.0, 15.0, 25.0, 50.0,
                100.0, 300.0, 1000.0, 10000.0, 100000.0, 0.0 };


            Otri tri = default(Otri);
            Vertex[] p = new Vertex[3];
            double[] dx = new double[3], dy = new double[3];
            double[] edgelength = new double[3];
            double triarea;
            double trilongest2;
            double triminaltitude2;
            double triaspect2;

            int aspectindex;
            int i, j, k;

            tri.orient = 0;
            foreach (var t in mesh.triangles)
            {
                tri.tri = t;
                p[0] = tri.Org();
                p[1] = tri.Dest();
                p[2] = tri.Apex();
                trilongest2 = 0.0;

                for (i = 0; i < 3; i++)
                {
                    j = plus1Mod3[i];
                    k = minus1Mod3[i];
                    dx[i] = p[j].x - p[k].x;
                    dy[i] = p[j].y - p[k].y;
                    edgelength[i] = dx[i] * dx[i] + dy[i] * dy[i];
                    if (edgelength[i] > trilongest2)
                    {
                        trilongest2 = edgelength[i];
                    }
                }

                //triarea = Primitives.CounterClockwise(p[0], p[1], p[2]);
                triarea = Math.Abs((p[2].x - p[0].x) * (p[1].y - p[0].y) -
                    (p[1].x - p[0].x) * (p[2].y - p[0].y)) / 2.0;

                triminaltitude2 = triarea * triarea / trilongest2;

                triaspect2 = trilongest2 / triminaltitude2;

                aspectindex = 0;
                while ((triaspect2 > ratiotable[aspectindex] * ratiotable[aspectindex]) && (aspectindex < 15))
                {
                    aspectindex++;
                }
                aspecttable[aspectindex]++;
            }
        }

        #endregion

        static readonly int[] plus1Mod3 = { 1, 2, 0 };
        static readonly int[] minus1Mod3 = { 2, 0, 1 };

        /// <summary>
        /// Update statistics about the quality of the mesh.
        /// </summary>
        /// <param name="mesh"></param>
        public void Update(Mesh mesh, int sampleDegrees)
        {
            Point[] p = new Point[3];

            int k1, k2;
            int degreeStep;

            //sampleDegrees = 36; // sample every 5 degrees
            //sampleDegrees = 45; // sample every 4 degrees
            sampleDegrees = 60; // sample every 3 degrees

            double[] cosSquareTable = new double[sampleDegrees / 2 - 1];
            double[] dx = new double[3];
            double[] dy = new double[3];
            double[] edgeLength = new double[3];
            double dotProduct;
            double cosSquare;
            double triArea;
            double triLongest2;
            double triMinAltitude2;
            double triAspect2;

            double radconst = Math.PI / sampleDegrees;
            double degconst = 180.0 / Math.PI;

            // New angle table
            angleTable = new int[sampleDegrees];
            minAngles = new int[sampleDegrees];
            maxAngles = new int[sampleDegrees];

            for (int i = 0; i < sampleDegrees / 2 - 1; i++)
            {
                cosSquareTable[i] = Math.Cos(radconst * (i + 1));
                cosSquareTable[i] = cosSquareTable[i] * cosSquareTable[i];
            }
            for (int i = 0; i < sampleDegrees; i++)
            {
                angleTable[i] = 0;
            }

            minAspect = mesh.bounds.Width + mesh.bounds.Height;
            minAspect = minAspect * minAspect;
            maxAspect = 0.0;
            minEdge = minAspect;
            maxEdge = 0.0;
            minArea = minAspect;
            maxArea = 0.0;
            minAngle = 0.0;
            maxAngle = 2.0;

            bool acuteBiggest = true;
            bool acuteBiggestTri = true;

            double triMinAngle, triMaxAngle = 1;

            foreach (var tri in mesh.triangles)
            {
                triMinAngle = 0; // Min angle:  0 < a <  60 degress
                triMaxAngle = 1; // Max angle: 60 < a < 180 degress

                p[0] = tri.vertices[0];
                p[1] = tri.vertices[1];
                p[2] = tri.vertices[2];

                triLongest2 = 0.0;

                for (int i = 0; i < 3; i++)
                {
                    k1 = plus1Mod3[i];
                    k2 = minus1Mod3[i];

                    dx[i] = p[k1].x - p[k2].x;
                    dy[i] = p[k1].y - p[k2].y;

                    edgeLength[i] = dx[i] * dx[i] + dy[i] * dy[i];

                    if (edgeLength[i] > triLongest2)
                    {
                        triLongest2 = edgeLength[i];
                    }

                    if (edgeLength[i] > maxEdge)
                    {
                        maxEdge = edgeLength[i];
                    }

                    if (edgeLength[i] < minEdge)
                    {
                        minEdge = edgeLength[i];
                    }
                }

                //triarea = Primitives.CounterClockwise(p[0], p[1], p[2]);
                triArea = Math.Abs((p[2].x - p[0].x) * (p[1].y - p[0].y) -
                    (p[1].x - p[0].x) * (p[2].y - p[0].y));

                if (triArea < minArea)
                {
                    minArea = triArea;
                }

                if (triArea > maxArea)
                {
                    maxArea = triArea;
                }

                triMinAltitude2 = triArea * triArea / triLongest2;
                if (triMinAltitude2 < minAspect)
                {
                    minAspect = triMinAltitude2;
                }

                triAspect2 = triLongest2 / triMinAltitude2;
                if (triAspect2 > maxAspect)
                {
                    maxAspect = triAspect2;
                }

                for (int i = 0; i < 3; i++)
                {
                    k1 = plus1Mod3[i];
                    k2 = minus1Mod3[i];

                    dotProduct = dx[k1] * dx[k2] + dy[k1] * dy[k2];
                    cosSquare = dotProduct * dotProduct / (edgeLength[k1] * edgeLength[k2]);
                    degreeStep = sampleDegrees / 2 - 1;

                    for (int j = degreeStep - 1; j >= 0; j--)
                    {
                        if (cosSquare > cosSquareTable[j])
                        {
                            degreeStep = j;
                        }
                    }

                    if (dotProduct <= 0.0)
                    {
                        angleTable[degreeStep]++;
                        if (cosSquare > minAngle)
                        {
                            minAngle = cosSquare;
                        }
                        if (acuteBiggest && (cosSquare < maxAngle))
                        {
                            maxAngle = cosSquare;
                        }

                        // Update min/max angle per triangle
                        if (cosSquare > triMinAngle)
                        {
                            triMinAngle = cosSquare;
                        }
                        if (acuteBiggestTri && (cosSquare < triMaxAngle))
                        {
                            triMaxAngle = cosSquare;
                        }
                    }
                    else
                    {
                        angleTable[sampleDegrees - degreeStep - 1]++;
                        if (acuteBiggest || (cosSquare > maxAngle))
                        {
                            maxAngle = cosSquare;
                            acuteBiggest = false;
                        }

                        // Update max angle for (possibly non-acute) triangle
                        if (acuteBiggestTri || (cosSquare > triMaxAngle))
                        {
                            triMaxAngle = cosSquare;
                            acuteBiggestTri = false;
                        }
                    }
                }

                // Update min angle histogram
                degreeStep = sampleDegrees / 2 - 1;

                for (int j = degreeStep - 1; j >= 0; j--)
                {
                    if (triMinAngle > cosSquareTable[j])
                    {
                        degreeStep = j;
                    }
                }
                minAngles[degreeStep]++;

                // Update max angle histogram
                degreeStep = sampleDegrees / 2 - 1;

                for (int j = degreeStep - 1; j >= 0; j--)
                {
                    if (triMaxAngle > cosSquareTable[j])
                    {
                        degreeStep = j;
                    }
                }

                if (acuteBiggestTri)
                {
                    maxAngles[degreeStep]++;
                }
                else
                {
                    maxAngles[sampleDegrees - degreeStep - 1]++;
                }

                acuteBiggestTri = true;
            }

            minEdge = Math.Sqrt(minEdge);
            maxEdge = Math.Sqrt(maxEdge);
            minAspect = Math.Sqrt(minAspect);
            maxAspect = Math.Sqrt(maxAspect);
            minArea *= 0.5;
            maxArea *= 0.5;
            if (minAngle >= 1.0)
            {
                minAngle = 0.0;
            }
            else
            {
                minAngle = degconst * Math.Acos(Math.Sqrt(minAngle));
            }

            if (maxAngle >= 1.0)
            {
                maxAngle = 180.0;
            }
            else
            {
                if (acuteBiggest)
                {
                    maxAngle = degconst * Math.Acos(Math.Sqrt(maxAngle));
                }
                else
                {
                    maxAngle = 180.0 - degconst * Math.Acos(Math.Sqrt(maxAngle));
                }
            }
        }

        /// <summary>
        /// Compute angle information for given triangle.
        /// </summary>
        /// <param name="triangle">The triangle to check.</param>
        /// <param name="data">Array of doubles (length 6).</param>
        /// <remarks>
        /// On return, the squared cosines of the minimum and maximum angle will
        /// be stored at position data[0] and data[1] respectively.
        /// If the triangle was obtuse, data[2] will be set to -1 and maximum angle
        /// is computed as (pi - acos(sqrt(data[1]))).
        /// </remarks>
        public static void ComputeAngles(ITriangle triangle, double[] data)
        {
            double min = 0.0;
            double max = 1.0;

            var va = triangle.GetVertex(0);
            var vb = triangle.GetVertex(1);
            var vc = triangle.GetVertex(2);

            double dxa = vb.x - vc.x;
            double dya = vb.y - vc.y;
            double lena = dxa * dxa + dya * dya;

            double dxb = vc.x - va.x;
            double dyb = vc.y - va.y;
            double lenb = dxb * dxb + dyb * dyb;

            double dxc = va.x - vb.x;
            double dyc = va.y - vb.y;
            double lenc = dxc * dxc + dyc * dyc;

            // Dot products.
            double dota = data[0] = dxb * dxc + dyb * dyc;
            double dotb = data[1] = dxc * dxa + dyc * dya;
            double dotc = data[2] = dxa * dxb + dya * dyb;

            // Squared cosines.
            data[3] = (dota * dota) / (lenb * lenc);
            data[4] = (dotb * dotb) / (lenc * lena);
            data[5] = (dotc * dotc) / (lena * lenb);

            // The sign of the dot product will tell us, if the angle is
            // acute (value < 0) or obtuse (value > 0).

            bool acute = true;

            double cos, dot;

            for (int i = 0; i < 3; i++)
            {
                dot = data[i];
                cos = data[3 + i];

                if (dot <= 0.0)
                {
                    if (cos > min)
                    {
                        min = cos;
                    }

                    if (acute && (cos < max))
                    {
                        max = cos;
                    }
                }
                else
                {
                    // Update max angle for (possibly non-acute) triangle
                    if (acute || (cos > max))
                    {
                        max = cos;
                        acute = false;
                    }
                }
            }

            data[0] = min;
            data[1] = max;
            data[2] = acute ? 1.0 : -1.0;
        }
    }
}
