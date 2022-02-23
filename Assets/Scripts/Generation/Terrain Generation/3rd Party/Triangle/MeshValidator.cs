// -----------------------------------------------------------------------
// <copyright file="MeshValidator.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    using System;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;

    public static class MeshValidator
    {
        private static RobustPredicates predicates = RobustPredicates.Default;

        /// <summary>
        /// Test the mesh for topological consistency.
        /// </summary>
        public static bool IsConsistent(Mesh mesh)
        {
            Otri tri = default(Otri);
            Otri oppotri = default(Otri), oppooppotri = default(Otri);
            Vertex org, dest, apex;
            Vertex oppoorg, oppodest;

            var logger = Log.Instance;

            // Temporarily turn on exact arithmetic if it's off.
            bool saveexact = Behavior.NoExact;
            Behavior.NoExact = false;

            int horrors = 0;

            // Run through the list of triangles, checking each one.
            foreach (var t in mesh.triangles)
            {
                tri.tri = t;

                // Check all three edges of the triangle.
                for (tri.orient = 0; tri.orient < 3; tri.orient++)
                {
                    org = tri.Org();
                    dest = tri.Dest();
                    if (tri.orient == 0)
                    {
                        // Only test for inversion once.
                        // Test if the triangle is flat or inverted.
                        apex = tri.Apex();
                        if (predicates.CounterClockwise(org, dest, apex) <= 0.0)
                        {
                            if (Log.Verbose)
                            {
                                logger.Warning(String.Format("Triangle is flat or inverted (ID {0}).", t.id),
                                    "MeshValidator.IsConsistent()");
                            }

                            horrors++;
                        }
                    }

                    // Find the neighboring triangle on this edge.
                    tri.Sym(ref oppotri);
                    if (oppotri.tri.id != Mesh.DUMMY)
                    {
                        // Check that the triangle's neighbor knows it's a neighbor.
                        oppotri.Sym(ref oppooppotri);
                        if ((tri.tri != oppooppotri.tri) || (tri.orient != oppooppotri.orient))
                        {
                            if (tri.tri == oppooppotri.tri && Log.Verbose)
                            {
                                logger.Warning("Asymmetric triangle-triangle bond: (Right triangle, wrong orientation)",
                                    "MeshValidator.IsConsistent()");
                            }

                            horrors++;
                        }
                        // Check that both triangles agree on the identities
                        // of their shared vertices.
                        oppoorg = oppotri.Org();
                        oppodest = oppotri.Dest();
                        if ((org != oppodest) || (dest != oppoorg))
                        {
                            if (Log.Verbose)
                            {
                                logger.Warning("Mismatched edge coordinates between two triangles.",
                                    "MeshValidator.IsConsistent()");
                            }

                            horrors++;
                        }
                    }
                }
            }

            // Check for unconnected vertices
            mesh.MakeVertexMap();
            foreach (var v in mesh.vertices.Values)
            {
                if (v.tri.tri == null && Log.Verbose)
                {
                    logger.Warning("Vertex (ID " + v.id + ") not connected to mesh (duplicate input vertex?)",
                                "MeshValidator.IsConsistent()");
                }
            }

            // Restore the status of exact arithmetic.
            Behavior.NoExact = saveexact;

            return (horrors == 0);
        }

        /// <summary>
        /// Check if the mesh is (conforming) Delaunay.
        /// </summary>
        public static bool IsDelaunay(Mesh mesh)
        {
            return IsDelaunay(mesh, false);
        }

        /// <summary>
        /// Check if that the mesh is (constrained) Delaunay.
        /// </summary>
        public static bool IsConstrainedDelaunay(Mesh mesh)
        {
            return IsDelaunay(mesh, true);
        }

        /// <summary>
        /// Ensure that the mesh is (constrained) Delaunay.
        /// </summary>
        private static bool IsDelaunay(Mesh mesh, bool constrained)
        {
            Otri loop = default(Otri);
            Otri oppotri = default(Otri);
            Osub opposubseg = default(Osub);
            Vertex org, dest, apex;
            Vertex oppoapex;

            bool shouldbedelaunay;

            var logger = Log.Instance;

            // Temporarily turn on exact arithmetic if it's off.
            bool saveexact = Behavior.NoExact;
            Behavior.NoExact = false;

            int horrors = 0;

            var inf1 = mesh.infvertex1;
            var inf2 = mesh.infvertex2;
            var inf3 = mesh.infvertex3;

            // Run through the list of triangles, checking each one.
            foreach (var tri in mesh.triangles)
            {
                loop.tri = tri;

                // Check all three edges of the triangle.
                for (loop.orient = 0; loop.orient < 3; loop.orient++)
                {
                    org = loop.Org();
                    dest = loop.Dest();
                    apex = loop.Apex();

                    loop.Sym(ref oppotri);
                    oppoapex = oppotri.Apex();

                    // Only test that the edge is locally Delaunay if there is an
                    // adjoining triangle whose pointer is larger (to ensure that
                    // each pair isn't tested twice).
                    shouldbedelaunay = (loop.tri.id < oppotri.tri.id) &&
                           !Otri.IsDead(oppotri.tri) && (oppotri.tri.id != Mesh.DUMMY) &&
                          (org != inf1) && (org != inf2) && (org != inf3) &&
                          (dest != inf1) && (dest != inf2) && (dest != inf3) &&
                          (apex != inf1) && (apex != inf2) && (apex != inf3) &&
                          (oppoapex != inf1) && (oppoapex != inf2) && (oppoapex != inf3);

                    if (constrained && mesh.checksegments && shouldbedelaunay)
                    {
                        // If a subsegment separates the triangles, then the edge is
                        // constrained, so no local Delaunay test should be done.
                        loop.Pivot(ref opposubseg);

                        if (opposubseg.seg.hash != Mesh.DUMMY)
                        {
                            shouldbedelaunay = false;
                        }
                    }

                    if (shouldbedelaunay)
                    {
                        if (predicates.NonRegular(org, dest, apex, oppoapex) > 0.0)
                        {
                            if (Log.Verbose)
                            {
                                logger.Warning(String.Format("Non-regular pair of triangles found (IDs {0}/{1}).",
                                    loop.tri.id, oppotri.tri.id), "MeshValidator.IsDelaunay()");
                            }

                            horrors++;
                        }
                    }
                }

            }

            // Restore the status of exact arithmetic.
            Behavior.NoExact = saveexact;

            return (horrors == 0);
        }
    }
}
