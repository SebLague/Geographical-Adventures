// -----------------------------------------------------------------------
// <copyright file="TriangleQuadTree.cs" company="">
// Original code by Frank Dockhorn, [not available anymore: http://sourceforge.net/projects/quadtreesim/]
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Tools
{
    using System.Collections.Generic;
    using System.Linq;
    using TriangleNet.Geometry;

    /// <summary>
    /// A Quadtree implementation optimized for triangles.
    /// </summary>
    public class TriangleQuadTree
    {
        QuadNode root;

        internal ITriangle[] triangles;

        internal int sizeBound;
        internal int maxDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleQuadTree" /> class.
        /// </summary>
        /// <param name="mesh">Mesh containing triangles.</param>
        /// <param name="maxDepth">The maximum depth of the tree.</param>
        /// <param name="sizeBound">The maximum number of triangles contained in a leaf.</param>
        /// <remarks>
        /// The quadtree does not track changes of the mesh. If a mesh is refined or
        /// changed in any other way, a new quadtree has to be built to make the point
        /// location work.
        /// 
        /// A node of the tree will be split, if its level if less than the max depth parameter
        /// AND the number of triangles in the node is greater than the size bound.
        /// </remarks>
        public TriangleQuadTree(Mesh mesh, int maxDepth = 10, int sizeBound = 10)
        {
            this.maxDepth = maxDepth;
            this.sizeBound = sizeBound;

            triangles = mesh.Triangles.ToArray();

            int currentDepth = 0;

            root = new QuadNode(mesh.Bounds, this, true);
            root.CreateSubRegion(++currentDepth);
        }

        public ITriangle Query(double x, double y)
        {
            var point = new Point(x, y);
            var indices = root.FindTriangles(point);

            foreach (var i in indices)
            {
                var tri = this.triangles[i];

                if (IsPointInTriangle(point, tri.GetVertex(0), tri.GetVertex(1), tri.GetVertex(2)))
                {
                    return tri;
                }
            }

            return null;
        }

        /// <summary>
        /// Test, if a given point lies inside a triangle.
        /// </summary>
        /// <param name="p">Point to locate.</param>
        /// <param name="t0">Corner point of triangle.</param>
        /// <param name="t1">Corner point of triangle.</param>
        /// <param name="t2">Corner point of triangle.</param>
        /// <returns>True, if point is inside or on the edge of this triangle.</returns>
        internal static bool IsPointInTriangle(Point p, Point t0, Point t1, Point t2)
        {
            // TODO: no need to create new Point instances here
            Point d0 = new Point(t1.x - t0.x, t1.y - t0.y);
            Point d1 = new Point(t2.x - t0.x, t2.y - t0.y);
            Point d2 = new Point(p.x - t0.x, p.y - t0.y);

            // crossproduct of (0, 0, 1) and d0
            Point c0 = new Point(-d0.y, d0.x);

            // crossproduct of (0, 0, 1) and d1
            Point c1 = new Point(-d1.y, d1.x);

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

        internal static double DotProduct(Point p, Point q)
        {
            return p.x * q.x + p.y * q.y;
        }

        /// <summary>
        /// A node of the quadtree.
        /// </summary>
        class QuadNode
        {
            const int SW = 0;
            const int SE = 1;
            const int NW = 2;
            const int NE = 3;

            const double EPS = 1e-6;

            static readonly byte[] BITVECTOR = { 0x1, 0x2, 0x4, 0x8 };

            Rectangle bounds;
            Point pivot;
            TriangleQuadTree tree;
            QuadNode[] regions;
            List<int> triangles;

            byte bitRegions;

            public QuadNode(Rectangle box, TriangleQuadTree tree)
                : this(box, tree, false)
            {
            }

            public QuadNode(Rectangle box, TriangleQuadTree tree, bool init)
            {
                this.tree = tree;

                this.bounds = new Rectangle(box.Left, box.Bottom, box.Width, box.Height);
                this.pivot = new Point((box.Left + box.Right) / 2, (box.Bottom + box.Top) / 2);

                this.bitRegions = 0;

                this.regions = new QuadNode[4];
                this.triangles = new List<int>();

                if (init)
                {
                    int count = tree.triangles.Length;

                    // Allocate memory upfront
                    triangles.Capacity = count;

                    for (int i = 0; i < count; i++)
                    {
                        triangles.Add(i);
                    }
                }
            }

            public List<int> FindTriangles(Point searchPoint)
            {
                int region = FindRegion(searchPoint);
                if (regions[region] == null)
                {
                    return triangles;
                }
                return regions[region].FindTriangles(searchPoint);
            }

            public void CreateSubRegion(int currentDepth)
            {
                // The four sub regions of the quad tree
                //   +--------------+
                //   | nw 2 | ne 3  |
                //   |------+pivot--|
                //   | sw 0 | se 1  |
                //   +--------------+
                Rectangle box;

                var width = bounds.Right - pivot.x;
                var height = bounds.Top - pivot.y;

                // 1. region south west
                box = new Rectangle(bounds.Left, bounds.Bottom, width, height);
                regions[0] = new QuadNode(box, tree);

                // 2. region south east
                box = new Rectangle(pivot.x, bounds.Bottom, width, height);
                regions[1] = new QuadNode(box, tree);

                // 3. region north west
                box = new Rectangle(bounds.Left, pivot.y, width, height);
                regions[2] = new QuadNode(box, tree);

                // 4. region north east
                box = new Rectangle(pivot.x, pivot.y, width, height);
                regions[3] = new QuadNode(box, tree);

                Point[] triangle = new Point[3];

                // Find region for every triangle vertex
                foreach (var index in triangles)
                {
                    ITriangle tri = tree.triangles[index];

                    triangle[0] = tri.GetVertex(0);
                    triangle[1] = tri.GetVertex(1);
                    triangle[2] = tri.GetVertex(2);

                    AddTriangleToRegion(triangle, index);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (regions[i].triangles.Count > tree.sizeBound && currentDepth < tree.maxDepth)
                    {
                        regions[i].CreateSubRegion(currentDepth + 1);
                    }
                }
            }

            void AddTriangleToRegion(Point[] triangle, int index)
            {
                bitRegions = 0;
                if (TriangleQuadTree.IsPointInTriangle(pivot, triangle[0], triangle[1], triangle[2]))
                {
                    AddToRegion(index, SW);
                    AddToRegion(index, SE);
                    AddToRegion(index, NW);
                    AddToRegion(index, NE);
                    return;
                }

                FindTriangleIntersections(triangle, index);

                if (bitRegions == 0)
                {
                    // we didn't find any intersection so we add this triangle to a point's region		
                    int region = FindRegion(triangle[0]);
                    regions[region].triangles.Add(index);
                }
            }

            void FindTriangleIntersections(Point[] triangle, int index)
            {
                // PLEASE NOTE:
                // Handling of component comparison is tightly associated with the implementation 
                // of the findRegion() function. That means when the point to be compared equals 
                // the pivot point the triangle must be put at least into region 2.
                //
                // Linear equations are in parametric form.
                //    pivot.x = triangle[0].x + t * (triangle[1].x - triangle[0].x)
                //    pivot.y = triangle[0].y + t * (triangle[1].y - triangle[0].y)

                int k = 2;

                double dx, dy;
                // Iterate through all triangle laterals and find bounding box intersections
                for (int i = 0; i < 3; k = i++)
                {
                    dx = triangle[i].x - triangle[k].x;
                    dy = triangle[i].y - triangle[k].y;

                    if (dx != 0.0)
                    {
                        FindIntersectionsWithX(dx, dy, triangle, index, k);
                    }
                    if (dy != 0.0)
                    {
                        FindIntersectionsWithY(dx, dy, triangle, index, k);
                    }
                }
            }

            void FindIntersectionsWithX(double dx, double dy, Point[] triangle, int index, int k)
            {
                double t;

                // find intersection with plane x = m_pivot.dX
                t = (pivot.x - triangle[k].x) / dx;
                if (t < (1 + EPS) && t > -EPS)
                {
                    // we have an intersection
                    double yComponent = triangle[k].y + t * dy;

                    if (yComponent < pivot.y && yComponent >= bounds.Bottom)
                    {
                        AddToRegion(index, SW);
                        AddToRegion(index, SE);
                    }
                    else if (yComponent <= bounds.Top)
                    {
                        AddToRegion(index, NW);
                        AddToRegion(index, NE);
                    }
                }

                // find intersection with plane x = m_boundingBox[0].dX
                t = (bounds.Left - triangle[k].x) / dx;
                if (t < (1 + EPS) && t > -EPS)
                {
                    // we have an intersection
                    double yComponent = triangle[k].y + t * dy;

                    if (yComponent < pivot.y && yComponent >= bounds.Bottom)
                    {
                        AddToRegion(index, SW);
                    }
                    else if (yComponent <= bounds.Top) // TODO: check && yComponent >= pivot.Y
                    {
                        AddToRegion(index, NW);
                    }
                }

                // find intersection with plane x = m_boundingBox[1].dX
                t = (bounds.Right - triangle[k].x) / dx;
                if (t < (1 + EPS) && t > -EPS)
                {
                    // we have an intersection
                    double yComponent = triangle[k].y + t * dy;

                    if (yComponent < pivot.y && yComponent >= bounds.Bottom)
                    {
                        AddToRegion(index, SE);
                    }
                    else if (yComponent <= bounds.Top)
                    {
                        AddToRegion(index, NE);
                    }
                }
            }

            void FindIntersectionsWithY(double dx, double dy, Point[] triangle, int index, int k)
            {
                double t, xComponent;

                // find intersection with plane y = m_pivot.dY
                t = (pivot.y - triangle[k].y) / dy;
                if (t < (1 + EPS) && t > -EPS)
                {
                    // we have an intersection
                    xComponent = triangle[k].x + t * dx;

                    if (xComponent > pivot.x && xComponent <= bounds.Right)
                    {
                        AddToRegion(index, SE);
                        AddToRegion(index, NE);
                    }
                    else if (xComponent >= bounds.Left)
                    {
                        AddToRegion(index, SW);
                        AddToRegion(index, NW);
                    }
                }

                // find intersection with plane y = m_boundingBox[0].dY
                t = (bounds.Bottom - triangle[k].y) / dy;
                if (t < (1 + EPS) && t > -EPS)
                {
                    // we have an intersection
                    xComponent = triangle[k].x + t * dx;

                    if (xComponent > pivot.x && xComponent <= bounds.Right)
                    {
                        AddToRegion(index, SE);
                    }
                    else if (xComponent >= bounds.Left)
                    {
                        AddToRegion(index, SW);
                    }
                }

                // find intersection with plane y = m_boundingBox[1].dY
                t = (bounds.Top - triangle[k].y) / dy;
                if (t < (1 + EPS) && t > -EPS)
                {
                    // we have an intersection
                    xComponent = triangle[k].x + t * dx;

                    if (xComponent > pivot.x && xComponent <= bounds.Right)
                    {
                        AddToRegion(index, NE);
                    }
                    else if (xComponent >= bounds.Left)
                    {
                        AddToRegion(index, NW);
                    }
                }
            }

            int FindRegion(Point point)
            {
                int b = 2;
                if (point.y < pivot.y)
                {
                    b = 0;
                }
                if (point.x > pivot.x)
                {
                    b++;
                }
                return b;
            }

            void AddToRegion(int index, int region)
            {
                //if (!(m_bitRegions & BITVECTOR[region]))
                if ((bitRegions & BITVECTOR[region]) == 0)
                {
                    regions[region].triangles.Add(index);
                    bitRegions |= BITVECTOR[region];
                }
            }
        }
    }
}
