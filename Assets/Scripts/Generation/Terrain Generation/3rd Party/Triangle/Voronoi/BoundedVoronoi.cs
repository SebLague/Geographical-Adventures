// -----------------------------------------------------------------------
// <copyright file="BoundedVoronoi.cs">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Voronoi
{
    using System.Collections.Generic;
    using TriangleNet.Geometry;
    using TriangleNet.Tools;
    using TriangleNet.Topology.DCEL;

    using HVertex = TriangleNet.Topology.DCEL.Vertex;
    using TVertex = TriangleNet.Geometry.Vertex;

    public class BoundedVoronoi : VoronoiBase
    {
        int offset;

        public BoundedVoronoi(Mesh mesh)
            : this(mesh, new DefaultVoronoiFactory(), RobustPredicates.Default)
        {
        }

        public BoundedVoronoi(Mesh mesh, IVoronoiFactory factory, IPredicates predicates)
            : base(mesh, factory, predicates, true)
        {
            // We explicitly told the base constructor to call the Generate method, so
            // at this point the basic Voronoi diagram is already created.
            offset = base.vertices.Count;

            // Each vertex of the hull will be part of a Voronoi cell.
            base.vertices.Capacity = offset + mesh.hullsize;

            // Create bounded Voronoi diagram.
            PostProcess();

            ResolveBoundaryEdges();
        }

        /// <summary>
        /// Computes edge intersections with mesh boundary edges.
        /// </summary>
        private void PostProcess()
        {
            foreach (var edge in rays)
            {
                var twin = edge.twin;

                var v1 = (TVertex)edge.face.generator;
                var v2 = (TVertex)twin.face.generator;

                double dir = predicates.CounterClockwise(v1, v2, edge.origin);

                if (dir <= 0)
                {
                    HandleCase1(edge, v1, v2);
                }
                else
                {
                    HandleCase2(edge, v1, v2);
                }
            }
        }

        /// <summary>
        /// Case 1: edge origin lies inside the domain.
        /// </summary>
        private void HandleCase1(HalfEdge edge, TVertex v1, TVertex v2)
        {
            //int mark = GetBoundaryMark(v1);

            // The infinite vertex.
            var v = (Point)edge.twin.origin;

            // The half-edge is the bisector of v1 and v2, so the projection onto the
            // boundary segment is actually its midpoint.
            v.x = (v1.x + v2.x) / 2.0;
            v.y = (v1.y + v2.y) / 2.0;

            // Close the cell connected to edge.
            var gen = factory.CreateVertex(v1.x, v1.y);

            var h1 = factory.CreateHalfEdge(edge.twin.origin, edge.face);
            var h2 = factory.CreateHalfEdge(gen, edge.face);

            edge.next = h1;
            h1.next = h2;
            h2.next = edge.face.edge;

            gen.leaving = h2;

            // Let the face edge point to the edge leaving at generator.
            edge.face.edge = h2;

            base.edges.Add(h1);
            base.edges.Add(h2);

            int count = base.edges.Count;

            h1.id = count;
            h2.id = count + 1;

            gen.id = offset++;
            base.vertices.Add(gen);
        }

        /// <summary>
        /// Case 2: edge origin lies outside the domain.
        /// </summary>
        private void HandleCase2(HalfEdge edge, TVertex v1, TVertex v2)
        {
            // The vertices of the infinite edge.
            var p1 = (Point)edge.origin;
            var p2 = (Point)edge.twin.origin;

            // The two edges leaving p1, pointing into the mesh.
            var e1 = edge.twin.next;
            var e2 = e1.twin.next;

            // Find the two intersections with boundary edge.
            IntersectionHelper.IntersectSegments(v1, v2, e1.origin, e1.twin.origin, ref p2);
            IntersectionHelper.IntersectSegments(v1, v2, e2.origin, e2.twin.origin, ref p1);

            // The infinite edge will now lie on the boundary. Update pointers:
            e1.twin.next = edge.twin;
            edge.twin.next = e2;
            edge.twin.face = e2.face;

            e1.origin = edge.twin.origin;

            edge.twin.twin = null;
            edge.twin = null;

            // Close the cell.
            var gen = factory.CreateVertex(v1.x, v1.y);
            var he = factory.CreateHalfEdge(gen, edge.face);

            edge.next = he;
            he.next = edge.face.edge;

            // Let the face edge point to the edge leaving at generator.
            edge.face.edge = he;

            base.edges.Add(he);

            he.id = base.edges.Count;

            gen.id = offset++;
            base.vertices.Add(gen);
        }

        /*
        private int GetBoundaryMark(Vertex v)
        {
            Otri tri = default(Otri);
            Otri next = default(Otri);
            Osub seg = default(Osub);

            // Get triangle connected to generator.
            v.tri.Copy(ref tri);
            v.tri.Copy(ref next);

            // Find boundary triangle.
            while (next.triangle.id != -1)
            {
                next.Copy(ref tri);
                next.OnextSelf();
            }

            // Find edge dual to current half-edge.
            tri.LnextSelf();
            tri.LnextSelf();

            tri.SegPivot(ref seg);

            return seg.seg.boundary;
        }
        //*/
    }
}
