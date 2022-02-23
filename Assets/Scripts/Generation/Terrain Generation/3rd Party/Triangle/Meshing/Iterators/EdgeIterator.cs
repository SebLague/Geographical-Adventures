// -----------------------------------------------------------------------
// <copyright file="EdgeEnumerator.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing.Iterators
{
    using System.Collections.Generic;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;

    /// <summary>
    /// Enumerates the edges of a triangulation.
    /// </summary>
    public class EdgeIterator : IEnumerator<Edge>
    {
        IEnumerator<Triangle> triangles;
        Otri tri = default(Otri);
        Otri neighbor = default(Otri);
        Osub sub = default(Osub);
        Edge current;
        Vertex p1, p2;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeIterator" /> class.
        /// </summary>
        public EdgeIterator(Mesh mesh)
        {
            triangles = mesh.triangles.GetEnumerator();
            triangles.MoveNext();

            tri.tri = triangles.Current;
            tri.orient = 0;
        }

        public Edge Current
        {
            get { return current; }
        }

        public void Dispose()
        {
            this.triangles.Dispose();
        }

        object System.Collections.IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            if (tri.tri == null)
            {
                return false;
            }

            current = null;

            while (current == null)
            {
                if (tri.orient == 3)
                {
                    if (triangles.MoveNext())
                    {
                        tri.tri = triangles.Current;
                        tri.orient = 0;
                    }
                    else
                    {
                        // Finally no more triangles
                        return false;
                    }
                }

                tri.Sym(ref neighbor);

                if ((tri.tri.id < neighbor.tri.id) || (neighbor.tri.id == Mesh.DUMMY))
                {
                    p1 = tri.Org();
                    p2 = tri.Dest();

                    tri.Pivot(ref sub);

                    // Boundary mark of dummysub is 0, so we don't need to worry about that.
                    current = new Edge(p1.id, p2.id, sub.seg.boundary);
                }

                tri.orient++;
            }

            return true;
        }

        public void Reset()
        {
            this.triangles.Reset();
        }
    }
}
