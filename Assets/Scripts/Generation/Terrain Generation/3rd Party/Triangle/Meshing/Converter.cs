// -----------------------------------------------------------------------
// <copyright file="Converter.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TriangleNet.Geometry;
    using TriangleNet.Topology;
    using TriangleNet.Topology.DCEL;

    using HVertex = TriangleNet.Topology.DCEL.Vertex;
    using TVertex = TriangleNet.Geometry.Vertex;

    /// <summary>
    /// The Converter class provides methods for mesh reconstruction and conversion.
    /// </summary>
    public static class Converter
    {
        #region Triangle mesh conversion

        /// <summary>
        /// Reconstruct a triangulation from its raw data representation.
        /// </summary>
        public static Mesh ToMesh(Polygon polygon, IList<ITriangle> triangles)
        {
            return ToMesh(polygon, triangles.ToArray());
        }

        /// <summary>
        /// Reconstruct a triangulation from its raw data representation.
        /// </summary>
        public static Mesh ToMesh(Polygon polygon, ITriangle[] triangles)
        {
            Otri tri = default(Otri);
            Osub subseg = default(Osub);
            int i = 0;

            int elements = triangles == null ? 0 : triangles.Length;
            int segments = polygon.Segments.Count;

            // TODO: Configuration should be a function argument.
            var mesh = new Mesh(new Configuration());

            mesh.TransferNodes(polygon.Points);

            mesh.regions.AddRange(polygon.Regions);
            mesh.behavior.useRegions = polygon.Regions.Count > 0;

            if (polygon.Segments.Count > 0)
            {
                mesh.behavior.Poly = true;
                mesh.holes.AddRange(polygon.Holes);
            }

            // Create the triangles.
            for (i = 0; i < elements; i++)
            {
                mesh.MakeTriangle(ref tri);
            }

            if (mesh.behavior.Poly)
            {
                mesh.insegments = segments;

                // Create the subsegments.
                for (i = 0; i < segments; i++)
                {
                    mesh.MakeSegment(ref subseg);
                }
            }

            var vertexarray = SetNeighbors(mesh, triangles);

            SetSegments(mesh, polygon, vertexarray);

            return mesh;
        }

        /// <summary>
        /// Finds the adjacencies between triangles by forming a stack of triangles for
        /// each vertex. Each triangle is on three different stacks simultaneously.
        /// </summary>
        private static List<Otri>[] SetNeighbors(Mesh mesh, ITriangle[] triangles)
        {
            Otri tri = default(Otri);
            Otri triangleleft = default(Otri);
            Otri checktri = default(Otri);
            Otri checkleft = default(Otri);
            Otri nexttri;
            TVertex tdest, tapex;
            TVertex checkdest, checkapex;
            int[] corner = new int[3];
            int aroundvertex;
            int i;

            // Allocate a temporary array that maps each vertex to some adjacent triangle.
            var vertexarray = new List<Otri>[mesh.vertices.Count];

            // Each vertex is initially unrepresented.
            for (i = 0; i < mesh.vertices.Count; i++)
            {
                Otri tmp = default(Otri);
                tmp.tri = mesh.dummytri;
                vertexarray[i] = new List<Otri>(3);
                vertexarray[i].Add(tmp);
            }

            i = 0;

            // Read the triangles from the .ele file, and link
            // together those that share an edge.
            foreach (var item in mesh.triangles)
            {
                tri.tri = item;

                // Copy the triangle's three corners.
                for (int j = 0; j < 3; j++)
                {
                    corner[j] = triangles[i].GetVertexID(j);

                    if ((corner[j] < 0) || (corner[j] >= mesh.invertices))
                    {
                        Log.Instance.Error("Triangle has an invalid vertex index.", "MeshReader.Reconstruct()");
                        throw new Exception("Triangle has an invalid vertex index.");
                    }
                }

                // Read the triangle's attributes.
                tri.tri.label = triangles[i].Label;

                // TODO: VarArea
                if (mesh.behavior.VarArea)
                {
                    tri.tri.area = triangles[i].Area;
                }

                // Set the triangle's vertices.
                tri.orient = 0;
                tri.SetOrg(mesh.vertices[corner[0]]);
                tri.SetDest(mesh.vertices[corner[1]]);
                tri.SetApex(mesh.vertices[corner[2]]);

                // Try linking the triangle to others that share these vertices.
                for (tri.orient = 0; tri.orient < 3; tri.orient++)
                {
                    // Take the number for the origin of triangleloop.
                    aroundvertex = corner[tri.orient];

                    int index = vertexarray[aroundvertex].Count - 1;

                    // Look for other triangles having this vertex.
                    nexttri = vertexarray[aroundvertex][index];

                    // Push the current triangle onto the stack.
                    vertexarray[aroundvertex].Add(tri);

                    checktri = nexttri;

                    if (checktri.tri.id != Mesh.DUMMY)
                    {
                        tdest = tri.Dest();
                        tapex = tri.Apex();

                        // Look for other triangles that share an edge.
                        do
                        {
                            checkdest = checktri.Dest();
                            checkapex = checktri.Apex();

                            if (tapex == checkdest)
                            {
                                // The two triangles share an edge; bond them together.
                                tri.Lprev(ref triangleleft);
                                triangleleft.Bond(ref checktri);
                            }
                            if (tdest == checkapex)
                            {
                                // The two triangles share an edge; bond them together.
                                checktri.Lprev(ref checkleft);
                                tri.Bond(ref checkleft);
                            }
                            // Find the next triangle in the stack.
                            index--;
                            nexttri = vertexarray[aroundvertex][index];

                            checktri = nexttri;
                        } while (checktri.tri.id != Mesh.DUMMY);
                    }
                }

                i++;
            }

            return vertexarray;
        }

        /// <summary>
        /// Finds the adjacencies between triangles and subsegments.
        /// </summary>
        private static void SetSegments(Mesh mesh, Polygon polygon, List<Otri>[] vertexarray)
        {
            Otri checktri = default(Otri);
            Otri nexttri; // Triangle
            TVertex checkdest;
            Otri checkneighbor = default(Otri);
            Osub subseg = default(Osub);
            Otri prevlink; // Triangle

            TVertex tmp;
            TVertex sorg, sdest;

            bool notfound;

            //bool segmentmarkers = false;
            int boundmarker;
            int aroundvertex;
            int i;

            int hullsize = 0;

            // Prepare to count the boundary edges.
            if (mesh.behavior.Poly)
            {
                // Link the segments to their neighboring triangles.
                boundmarker = 0;
                i = 0;
                foreach (var item in mesh.subsegs.Values)
                {
                    subseg.seg = item;

                    sorg = polygon.Segments[i].GetVertex(0);
                    sdest = polygon.Segments[i].GetVertex(1);

                    boundmarker = polygon.Segments[i].Label;

                    if ((sorg.id < 0 || sorg.id >= mesh.invertices) || (sdest.id < 0 || sdest.id >= mesh.invertices))
                    {
                        Log.Instance.Error("Segment has an invalid vertex index.", "MeshReader.Reconstruct()");
                        throw new Exception("Segment has an invalid vertex index.");
                    }

                    // set the subsegment's vertices.
                    subseg.orient = 0;
                    subseg.SetOrg(sorg);
                    subseg.SetDest(sdest);
                    subseg.SetSegOrg(sorg);
                    subseg.SetSegDest(sdest);
                    subseg.seg.boundary = boundmarker;
                    // Try linking the subsegment to triangles that share these vertices.
                    for (subseg.orient = 0; subseg.orient < 2; subseg.orient++)
                    {
                        // Take the number for the destination of subsegloop.
                        aroundvertex = subseg.orient == 1 ? sorg.id : sdest.id;

                        int index = vertexarray[aroundvertex].Count - 1;

                        // Look for triangles having this vertex.
                        prevlink = vertexarray[aroundvertex][index];
                        nexttri = vertexarray[aroundvertex][index];

                        checktri = nexttri;
                        tmp = subseg.Org();
                        notfound = true;
                        // Look for triangles having this edge.  Note that I'm only
                        // comparing each triangle's destination with the subsegment;
                        // each triangle's apex is handled through a different vertex.
                        // Because each triangle appears on three vertices' lists, each
                        // occurrence of a triangle on a list can (and does) represent
                        // an edge.  In this way, most edges are represented twice, and
                        // every triangle-subsegment bond is represented once.
                        while (notfound && (checktri.tri.id != Mesh.DUMMY))
                        {
                            checkdest = checktri.Dest();

                            if (tmp == checkdest)
                            {
                                // We have a match. Remove this triangle from the list.
                                //prevlink = vertexarray[aroundvertex][index];
                                vertexarray[aroundvertex].Remove(prevlink);
                                // Bond the subsegment to the triangle.
                                checktri.SegBond(ref subseg);
                                // Check if this is a boundary edge.
                                checktri.Sym(ref checkneighbor);
                                if (checkneighbor.tri.id == Mesh.DUMMY)
                                {
                                    // The next line doesn't insert a subsegment (because there's
                                    // already one there), but it sets the boundary markers of
                                    // the existing subsegment and its vertices.
                                    mesh.InsertSubseg(ref checktri, 1);
                                    hullsize++;
                                }
                                notfound = false;
                            }
                            index--;
                            // Find the next triangle in the stack.
                            prevlink = vertexarray[aroundvertex][index];
                            nexttri = vertexarray[aroundvertex][index];

                            checktri = nexttri;
                        }
                    }

                    i++;
                }
            }

            // Mark the remaining edges as not being attached to any subsegment.
            // Also, count the (yet uncounted) boundary edges.
            for (i = 0; i < mesh.vertices.Count; i++)
            {
                // Search the stack of triangles adjacent to a vertex.
                int index = vertexarray[i].Count - 1;
                nexttri = vertexarray[i][index];
                checktri = nexttri;

                while (checktri.tri.id != Mesh.DUMMY)
                {
                    // Find the next triangle in the stack before this
                    // information gets overwritten.
                    index--;
                    nexttri = vertexarray[i][index];
                    // No adjacent subsegment.  (This overwrites the stack info.)
                    checktri.SegDissolve(mesh.dummysub);
                    checktri.Sym(ref checkneighbor);
                    if (checkneighbor.tri.id == Mesh.DUMMY)
                    {
                        mesh.InsertSubseg(ref checktri, 1);
                        hullsize++;
                    }

                    checktri = nexttri;
                }
            }

            mesh.hullsize = hullsize;
        }

        #endregion

        #region DCEL conversion

        public static DcelMesh ToDCEL(Mesh mesh)
        {
            var dcel = new DcelMesh();

            var vertices = new HVertex[mesh.vertices.Count];
            var faces = new Face[mesh.triangles.Count];

            dcel.HalfEdges.Capacity = 2 * mesh.NumberOfEdges;

            mesh.Renumber();

            HVertex vertex;

            foreach (var v in mesh.vertices.Values)
            {
                vertex = new HVertex(v.x, v.y);
                vertex.id = v.id;
                vertex.label = v.label;

                vertices[v.id] = vertex;
            }

            // Maps a triangle to its 3 edges (used to set next pointers).
            var map = new List<HalfEdge>[mesh.triangles.Count];

            Face face;

            foreach (var t in mesh.triangles)
            {
                face = new Face(null);
                face.id = t.id;

                faces[t.id] = face;

                map[t.id] = new List<HalfEdge>(3);
            }

            Otri tri = default(Otri), neighbor = default(Otri);
            TriangleNet.Geometry.Vertex org, dest;

            int id, nid, count = mesh.triangles.Count;

            HalfEdge edge, twin, next;

            var edges = dcel.HalfEdges;

            // Count half-edges (edge ids).
            int k = 0;

            // Maps a vertex to its leaving boundary edge.
            var boundary = new Dictionary<int, HalfEdge>();

            foreach (var t in mesh.triangles)
            {
                id = t.id;

                tri.tri = t;

                for (int i = 0; i < 3; i++)
                {
                    tri.orient = i;
                    tri.Sym(ref neighbor);

                    nid = neighbor.tri.id;

                    if (id < nid || nid < 0)
                    {
                        face = faces[id];

                        // Get the endpoints of the current triangle edge.
                        org = tri.Org();
                        dest = tri.Dest();

                        // Create half-edges.
                        edge = new HalfEdge(vertices[org.id], face);
                        twin = new HalfEdge(vertices[dest.id], nid < 0 ? Face.Empty : faces[nid]);

                        map[id].Add(edge);

                        if (nid >= 0)
                        {
                            map[nid].Add(twin);
                        }
                        else
                        {
                            boundary.Add(dest.id, twin);
                        }

                        // Set leaving edges.
                        edge.origin.leaving = edge;
                        twin.origin.leaving = twin;

                        // Set twin edges.
                        edge.twin = twin;
                        twin.twin = edge;

                        edge.id = k++;
                        twin.id = k++;

                        edges.Add(edge);
                        edges.Add(twin);
                    }
                }
            }

            // Set next pointers for each triangle face.
            foreach (var t in map)
            {
                edge = t[0];
                next = t[1];

                if (edge.twin.origin.id == next.origin.id)
                {
                    edge.next = next;
                    next.next = t[2];
                    t[2].next = edge;
                }
                else
                {
                    edge.next = t[2];
                    next.next = edge;
                    t[2].next = next;
                }
            }

            // Resolve boundary edges.
            foreach (var e in boundary.Values)
            {
                e.next = boundary[e.twin.origin.id];
            }

            dcel.Vertices.AddRange(vertices);
            dcel.Faces.AddRange(faces);

            return dcel;
        }

        #endregion
    }
}
