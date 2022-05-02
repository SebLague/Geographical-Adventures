// -----------------------------------------------------------------------
// <copyright file="Incremental.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Meshing.Algorithm
{
    using System.Collections.Generic;
    using TriangleNet.Topology;
    using TriangleNet.Geometry;

    /// <summary>
    /// Builds a delaunay triangulation using the incremental algorithm.
    /// </summary>
    public class Incremental : ITriangulator
    {
        Mesh mesh;

        /// <summary>
        /// Form a Delaunay triangulation by incrementally inserting vertices.
        /// </summary>
        /// <returns>Returns the number of edges on the convex hull of the 
        /// triangulation.</returns>
        public IMesh Triangulate(IList<Vertex> points, Configuration config)
        {
            this.mesh = new Mesh(config);
            this.mesh.TransferNodes(points);

            Otri starttri = new Otri();

            // Create a triangular bounding box.
            GetBoundingBox();

            foreach (var v in mesh.vertices.Values)
            {
                starttri.tri = mesh.dummytri;
                Osub tmp = default(Osub);
                if (mesh.InsertVertex(v, ref starttri, ref tmp, false, false) == InsertVertexResult.Duplicate)
                {
                    if (Log.Verbose)
                    {
                        Log.Instance.Warning("A duplicate vertex appeared and was ignored.",
                            "Incremental.Triangulate()");
                    }
                    v.type = VertexType.UndeadVertex;
                    mesh.undeads++;
                }
            }

            // Remove the bounding box.
            this.mesh.hullsize = RemoveBox();

            return this.mesh;
        }

        /// <summary>
        /// Form an "infinite" bounding triangle to insert vertices into.
        /// </summary>
        /// <remarks>
        /// The vertices at "infinity" are assigned finite coordinates, which are
        /// used by the point location routines, but (mostly) ignored by the
        /// Delaunay edge flip routines.
        /// </remarks>
        void GetBoundingBox()
        {
            Otri inftri = default(Otri); // Handle for the triangular bounding box.
            Rectangle box = mesh.bounds;

            // Find the width (or height, whichever is larger) of the triangulation.
            double width = box.Width;
            if (box.Height > width)
            {
                width = box.Height;
            }
            if (width == 0.0)
            {
                width = 1.0;
            }
            // Create the vertices of the bounding box.
            mesh.infvertex1 = new Vertex(box.Left - 50.0 * width, box.Bottom - 40.0 * width);
            mesh.infvertex2 = new Vertex(box.Right + 50.0 * width, box.Bottom - 40.0 * width);
            mesh.infvertex3 = new Vertex(0.5 * (box.Left + box.Right), box.Top + 60.0 * width);

            // Create the bounding box.
            mesh.MakeTriangle(ref inftri);

            inftri.SetOrg(mesh.infvertex1);
            inftri.SetDest(mesh.infvertex2);
            inftri.SetApex(mesh.infvertex3);

            // Link dummytri to the bounding box so we can always find an
            // edge to begin searching (point location) from.
            mesh.dummytri.neighbors[0] = inftri;
        }

        /// <summary>
        /// Remove the "infinite" bounding triangle, setting boundary markers as appropriate.
        /// </summary>
        /// <returns>Returns the number of edges on the convex hull of the triangulation.</returns>
        /// <remarks>
        /// The triangular bounding box has three boundary triangles (one for each
        /// side of the bounding box), and a bunch of triangles fanning out from
        /// the three bounding box vertices (one triangle for each edge of the
        /// convex hull of the inner mesh).  This routine removes these triangles.
        /// </remarks>
        int RemoveBox()
        {
            Otri deadtriangle = default(Otri);
            Otri searchedge = default(Otri);
            Otri checkedge = default(Otri);
            Otri nextedge = default(Otri), finaledge = default(Otri), dissolveedge = default(Otri);
            Vertex markorg;
            int hullsize;

            bool noPoly = !mesh.behavior.Poly;

            // Find a boundary triangle.
            nextedge.tri = mesh.dummytri;
            nextedge.orient = 0;
            nextedge.Sym();

            // Mark a place to stop.
            nextedge.Lprev(ref finaledge);
            nextedge.Lnext();
            nextedge.Sym();
            // Find a triangle (on the boundary of the vertex set) that isn't
            // a bounding box triangle.
            nextedge.Lprev(ref searchedge);
            searchedge.Sym();
            // Check whether nextedge is another boundary triangle
            // adjacent to the first one.
            nextedge.Lnext(ref checkedge);
            checkedge.Sym();
            if (checkedge.tri.id == Mesh.DUMMY)
            {
                // Go on to the next triangle.  There are only three boundary
                // triangles, and this next triangle cannot be the third one,
                // so it's safe to stop here.
                searchedge.Lprev();
                searchedge.Sym();
            }

            // Find a new boundary edge to search from, as the current search
            // edge lies on a bounding box triangle and will be deleted.
            mesh.dummytri.neighbors[0] = searchedge;

            hullsize = -2;
            while (!nextedge.Equals(finaledge))
            {
                hullsize++;
                nextedge.Lprev(ref dissolveedge);
                dissolveedge.Sym();
                // If not using a PSLG, the vertices should be marked now.
                // (If using a PSLG, markhull() will do the job.)
                if (noPoly)
                {
                    // Be careful!  One must check for the case where all the input
                    // vertices are collinear, and thus all the triangles are part of
                    // the bounding box.  Otherwise, the setvertexmark() call below
                    // will cause a bad pointer reference.
                    if (dissolveedge.tri.id != Mesh.DUMMY)
                    {
                        markorg = dissolveedge.Org();
                        if (markorg.label == 0)
                        {
                            markorg.label = 1;
                        }
                    }
                }
                // Disconnect the bounding box triangle from the mesh triangle.
                dissolveedge.Dissolve(mesh.dummytri);
                nextedge.Lnext(ref deadtriangle);
                deadtriangle.Sym(ref nextedge);
                // Get rid of the bounding box triangle.
                mesh.TriangleDealloc(deadtriangle.tri);
                // Do we need to turn the corner?
                if (nextedge.tri.id == Mesh.DUMMY)
                {
                    // Turn the corner.
                    dissolveedge.Copy(ref nextedge);
                }
            }

            mesh.TriangleDealloc(finaledge.tri);

            return hullsize;
        }
    }
}
