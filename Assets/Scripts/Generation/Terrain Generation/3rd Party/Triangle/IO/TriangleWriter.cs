// -----------------------------------------------------------------------
// <copyright file="TriangleWriter.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using TriangleNet.Geometry;
    using TriangleNet.Topology;

    /// <summary>
    /// Helper methods for writing Triangle file formats.
    /// </summary>
    public class TriangleWriter
    {
        static NumberFormatInfo nfi = NumberFormatInfo.InvariantInfo;

        /// <summary>
        /// Number the vertices and write them to a .node file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public void Write(Mesh mesh, string filename)
        {
            WritePoly(mesh, Path.ChangeExtension(filename, ".poly"));
            WriteElements(mesh, Path.ChangeExtension(filename, ".ele"));
        }

        /// <summary>
        /// Number the vertices and write them to a .node file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public void WriteNodes(Mesh mesh, string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                WriteNodes(writer, mesh);
            }
        }

        /// <summary>
        /// Number the vertices and write them to a .node file.
        /// </summary>
        private void WriteNodes(StreamWriter writer, Mesh mesh)
        {
            int outvertices = mesh.vertices.Count;
            int nextras = mesh.nextras;

            Behavior behavior = mesh.behavior;

            if (behavior.Jettison)
            {
                outvertices = mesh.vertices.Count - mesh.undeads;
            }

            if (writer != null)
            {
                // Number of vertices, number of dimensions, number of vertex attributes,
                // and number of boundary markers (zero or one).
                writer.WriteLine("{0} {1} {2} {3}", outvertices, mesh.mesh_dim, nextras,
                    behavior.UseBoundaryMarkers ? "1" : "0");

                if (mesh.numbering == NodeNumbering.None)
                {
                    // If the mesh isn't numbered yet, use linear node numbering.
                    mesh.Renumber();
                }

                if (mesh.numbering == NodeNumbering.Linear)
                {
                    // If numbering is linear, just use the dictionary values.
                    WriteNodes(writer, mesh.vertices.Values, behavior.UseBoundaryMarkers,
                        nextras, behavior.Jettison);
                }
                else
                {
                    // If numbering is not linear, a simple 'foreach' traversal of the dictionary
                    // values doesn't reflect the actual numbering. Use an array instead.

                    // TODO: Could use a custom sorting function on dictionary values instead.
                    Vertex[] nodes = new Vertex[mesh.vertices.Count];

                    foreach (var node in mesh.vertices.Values)
                    {
                        nodes[node.id] = node;
                    }

                    WriteNodes(writer, nodes, behavior.UseBoundaryMarkers,
                        nextras, behavior.Jettison);
                }
            }
        }

        /// <summary>
        /// Write the vertices to a stream.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="writer"></param>
        private void WriteNodes(StreamWriter writer, IEnumerable<Vertex> nodes, bool markers,
            int attribs, bool jettison)
        {
            int index = 0;

            foreach (var vertex in nodes)
            {
                if (!jettison || vertex.type != VertexType.UndeadVertex)
                {
                    // Vertex number, x and y coordinates.
                    writer.Write("{0} {1} {2}", index, vertex.x.ToString(nfi), vertex.y.ToString(nfi));

#if USE_ATTRIBS
                    // Write attributes.
                    for (int j = 0; j < attribs; j++)
                    {
                        writer.Write(" {0}", vertex.attributes[j].ToString(nfi));
                    }
#endif

                    if (markers)
                    {
                        // Write the boundary marker.
                        writer.Write(" {0}", vertex.label);
                    }

                    writer.WriteLine();

                    index++;
                }
            }
        }

        /// <summary>
        /// Write the triangles to an .ele file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public void WriteElements(Mesh mesh, string filename)
        {
            Otri tri = default(Otri);
            Vertex p1, p2, p3;
            bool regions = mesh.behavior.useRegions;

            int j = 0;

            tri.orient = 0;

            using (var writer = new StreamWriter(filename))
            {
                // Number of triangles, vertices per triangle, attributes per triangle.
                writer.WriteLine("{0} 3 {1}", mesh.triangles.Count, regions ? 1 : 0);

                foreach (var item in mesh.triangles)
                {
                    tri.tri = item;

                    p1 = tri.Org();
                    p2 = tri.Dest();
                    p3 = tri.Apex();

                    // Triangle number, indices for three vertices.
                    writer.Write("{0} {1} {2} {3}", j, p1.id, p2.id, p3.id);

                    if (regions)
                    {
                        writer.Write(" {0}", tri.tri.label);
                    }

                    writer.WriteLine();

                    // Number elements
                    item.id = j++;
                }
            }
        }

        /// <summary>
        /// Write the segments and holes to a .poly file.
        /// </summary>
        /// <param name="polygon">Data source.</param>
        /// <param name="filename">File name.</param>
        /// <param name="writeNodes">Write nodes into this file.</param>
        /// <remarks>If the nodes should not be written into this file, 
        /// make sure a .node file was written before, so that the nodes 
        /// are numbered right.</remarks>
        public void WritePoly(IPolygon polygon, string filename)
        {
            bool hasMarkers = polygon.HasSegmentMarkers;

            using (var writer = new StreamWriter(filename))
            {
                // TODO: write vertex attributes

                writer.WriteLine("{0} 2 0 {1}", polygon.Points.Count, polygon.HasPointMarkers ? "1" : "0");

                // Write nodes to this file.
                WriteNodes(writer, polygon.Points, polygon.HasPointMarkers, 0, false);

                // Number of segments, number of boundary markers (zero or one).
                writer.WriteLine("{0} {1}", polygon.Segments.Count, hasMarkers ? "1" : "0");

                Vertex p, q;

                int j = 0;
                foreach (var seg in polygon.Segments)
                {
                    p = seg.GetVertex(0);
                    q = seg.GetVertex(1);

                    // Segment number, indices of its two endpoints, and possibly a marker.
                    if (hasMarkers)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", j, p.ID, q.ID, seg.Label);
                    }
                    else
                    {
                        writer.WriteLine("{0} {1} {2}", j, p.ID, q.ID);
                    }

                    j++;
                }

                // Holes
                j = 0;
                writer.WriteLine("{0}", polygon.Holes.Count);
                foreach (var hole in polygon.Holes)
                {
                    writer.WriteLine("{0} {1} {2}", j++, hole.X.ToString(nfi), hole.Y.ToString(nfi));
                }

                // Regions
                if (polygon.Regions.Count > 0)
                {
                    j = 0;
                    writer.WriteLine("{0}", polygon.Regions.Count);
                    foreach (var region in polygon.Regions)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", j, region.point.X.ToString(nfi),
                            region.point.Y.ToString(nfi), region.id);

                        j++;
                    }
                }
            }
        }

        /// <summary>
        /// Write the segments and holes to a .poly file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public void WritePoly(Mesh mesh, string filename)
        {
            WritePoly(mesh, filename, true);
        }

        /// <summary>
        /// Write the segments and holes to a .poly file.
        /// </summary>
        /// <param name="mesh">Data source.</param>
        /// <param name="filename">File name.</param>
        /// <param name="writeNodes">Write nodes into this file.</param>
        /// <remarks>If the nodes should not be written into this file, 
        /// make sure a .node file was written before, so that the nodes 
        /// are numbered right.</remarks>
        public void WritePoly(Mesh mesh, string filename, bool writeNodes)
        {
            Osub subseg = default(Osub);
            Vertex pt1, pt2;

            bool useBoundaryMarkers = mesh.behavior.UseBoundaryMarkers;

            using (var writer = new StreamWriter(filename))
            {
                if (writeNodes)
                {
                    // Write nodes to this file.
                    WriteNodes(writer, mesh);
                }
                else
                {
                    // The zero indicates that the vertices are in a separate .node file.
                    // Followed by number of dimensions, number of vertex attributes,
                    // and number of boundary markers (zero or one).
                    writer.WriteLine("0 {0} {1} {2}", mesh.mesh_dim, mesh.nextras,
                        useBoundaryMarkers ? "1" : "0");
                }

                // Number of segments, number of boundary markers (zero or one).
                writer.WriteLine("{0} {1}", mesh.subsegs.Count,
                    useBoundaryMarkers ? "1" : "0");

                subseg.orient = 0;

                int j = 0;
                foreach (var item in mesh.subsegs.Values)
                {
                    subseg.seg = item;

                    pt1 = subseg.Org();
                    pt2 = subseg.Dest();

                    // Segment number, indices of its two endpoints, and possibly a marker.
                    if (useBoundaryMarkers)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", j, pt1.id, pt2.id, subseg.seg.boundary);
                    }
                    else
                    {
                        writer.WriteLine("{0} {1} {2}", j, pt1.id, pt2.id);
                    }

                    j++;
                }

                // Holes
                j = 0;
                writer.WriteLine("{0}", mesh.holes.Count);
                foreach (var hole in mesh.holes)
                {
                    writer.WriteLine("{0} {1} {2}", j++, hole.X.ToString(nfi), hole.Y.ToString(nfi));
                }

                // Regions
                if (mesh.regions.Count > 0)
                {
                    j = 0;
                    writer.WriteLine("{0}", mesh.regions.Count);
                    foreach (var region in mesh.regions)
                    {
                        writer.WriteLine("{0} {1} {2} {3}", j, region.point.X.ToString(nfi),
                            region.point.Y.ToString(nfi), region.id);

                        j++;
                    }
                }
            }
        }

        /// <summary>
        /// Write the edges to an .edge file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        public void WriteEdges(Mesh mesh, string filename)
        {
            Otri tri = default(Otri), trisym = default(Otri);
            Osub checkmark = default(Osub);
            Vertex p1, p2;

            Behavior behavior = mesh.behavior;

            using (var writer = new StreamWriter(filename))
            {
                // Number of edges, number of boundary markers (zero or one).
                writer.WriteLine("{0} {1}", mesh.NumberOfEdges, behavior.UseBoundaryMarkers ? "1" : "0");

                long index = 0;
                // To loop over the set of edges, loop over all triangles, and look at
                // the three edges of each triangle.  If there isn't another triangle
                // adjacent to the edge, operate on the edge.  If there is another
                // adjacent triangle, operate on the edge only if the current triangle
                // has a smaller pointer than its neighbor.  This way, each edge is
                // considered only once.
                foreach (var item in mesh.triangles)
                {
                    tri.tri = item;

                    for (tri.orient = 0; tri.orient < 3; tri.orient++)
                    {
                        tri.Sym(ref trisym);
                        if ((tri.tri.id < trisym.tri.id) || (trisym.tri.id == Mesh.DUMMY))
                        {
                            p1 = tri.Org();
                            p2 = tri.Dest();

                            if (behavior.UseBoundaryMarkers)
                            {
                                // Edge number, indices of two endpoints, and a boundary marker.
                                // If there's no subsegment, the boundary marker is zero.
                                if (behavior.useSegments)
                                {
                                    tri.Pivot(ref checkmark);

                                    if (checkmark.seg.hash == Mesh.DUMMY)
                                    {
                                        writer.WriteLine("{0} {1} {2} {3}", index, p1.id, p2.id, 0);
                                    }
                                    else
                                    {
                                        writer.WriteLine("{0} {1} {2} {3}", index, p1.id, p2.id,
                                                checkmark.seg.boundary);
                                    }
                                }
                                else
                                {
                                    writer.WriteLine("{0} {1} {2} {3}", index, p1.id, p2.id,
                                            trisym.tri.id == Mesh.DUMMY ? "1" : "0");
                                }
                            }
                            else
                            {
                                // Edge number, indices of two endpoints.
                                writer.WriteLine("{0} {1} {2}", index, p1.id, p2.id);
                            }

                            index++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write the triangle neighbors to a .neigh file.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="filename"></param>
        /// <remarks>WARNING: Be sure WriteElements has been called before, 
        /// so the elements are numbered right!</remarks>
        public void WriteNeighbors(Mesh mesh, string filename)
        {
            Otri tri = default(Otri), trisym = default(Otri);
            int n1, n2, n3;
            int i = 0;

            using (StreamWriter writer = new StreamWriter(filename))
            {
                // Number of triangles, three neighbors per triangle.
                writer.WriteLine("{0} 3", mesh.triangles.Count);

                foreach (var item in mesh.triangles)
                {
                    tri.tri = item;

                    tri.orient = 1;
                    tri.Sym(ref trisym);
                    n1 = trisym.tri.id;

                    tri.orient = 2;
                    tri.Sym(ref trisym);
                    n2 = trisym.tri.id;

                    tri.orient = 0;
                    tri.Sym(ref trisym);
                    n3 = trisym.tri.id;

                    // Triangle number, neighboring triangle numbers.
                    writer.WriteLine("{0} {1} {2} {3}", i++, n1, n2, n3);
                }
            }
        }
    }
}
