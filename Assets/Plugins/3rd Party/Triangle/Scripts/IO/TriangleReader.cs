// -----------------------------------------------------------------------
// <copyright file="TriangleReader.cs" company="">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using TriangleNet.Geometry;

    /// <summary>
    /// Helper methods for reading Triangle file formats.
    /// </summary>
    public class TriangleReader
    {
        static NumberFormatInfo nfi = NumberFormatInfo.InvariantInfo;

        int startIndex = 0;

        #region Helper methods

        private bool TryReadLine(StreamReader reader, out string[] token)
        {
            token = null;

            if (reader.EndOfStream)
            {
                return false;
            }

            string line = reader.ReadLine().Trim();

            while (String.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                if (reader.EndOfStream)
                {
                    return false;
                }

                line = reader.ReadLine().Trim();
            }

            token = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            return true;
        }

        /// <summary>
        /// Read vertex information of the given line.
        /// </summary>
        /// <param name="data">The input geometry.</param>
        /// <param name="index">The current vertex index.</param>
        /// <param name="line">The current line.</param>
        /// <param name="attributes">Number of point attributes</param>
        /// <param name="marks">Number of point markers (0 or 1)</param>
        private void ReadVertex(List<Vertex> data, int index, string[] line, int attributes, int marks)
        {
            double x = double.Parse(line[1], nfi);
            double y = double.Parse(line[2], nfi);

            var v = new Vertex(x, y);

            // Read a vertex marker.
            if (marks > 0 && line.Length > 3 + attributes)
            {
                v.Label = int.Parse(line[3 + attributes]);
            }

            if (attributes > 0)
            {
#if USE_ATTRIBS
                var attribs = new double[attributes];

                // Read the vertex attributes.
                for (int j = 0; j < attributes; j++)
                {
                    if (line.Length > 3 + j)
                    {
                        attribs[j] = double.Parse(line[3 + j], nfi);
                    }
                }

                v.attributes = attribs;
#endif
            }

            data.Add(v);
        }

        #endregion

        #region Main I/O methods

        /// <summary>
        /// Reads geometry information from .node or .poly files.
        /// </summary>
        public void Read(string filename, out Polygon polygon)
        {
            polygon = null;

            string path = Path.ChangeExtension(filename, ".poly");

            if (File.Exists(path))
            {
                polygon = ReadPolyFile(path);
            }
            else
            {
                path = Path.ChangeExtension(filename, ".node");
                polygon = ReadNodeFile(path);
            }
        }

        /// <summary>
        /// Reads a mesh from .node, .poly or .ele files.
        /// </summary>
        public void Read(string filename, out Polygon geometry, out List<ITriangle> triangles)
        {
            triangles = null;

            Read(filename, out geometry);

            string path = Path.ChangeExtension(filename, ".ele");

            if (File.Exists(path) && geometry != null)
            {
                triangles = ReadEleFile(path);
            }
        }

        /// <summary>
        /// Reads geometry information from .node or .poly files.
        /// </summary>
        public IPolygon Read(string filename)
        {
            Polygon geometry = null;

            Read(filename, out geometry);

            return geometry;
        }

        #endregion

        /// <summary>
        /// Read the vertices from a file, which may be a .node or .poly file.
        /// </summary>
        /// <param name="nodefilename"></param>
        /// <remarks>Will NOT read associated .ele by default.</remarks>
        public Polygon ReadNodeFile(string nodefilename)
        {
            return ReadNodeFile(nodefilename, false);
        }

        /// <summary>
        /// Read the vertices from a file, which may be a .node or .poly file.
        /// </summary>
        /// <param name="nodefilename"></param>
        /// <param name="readElements"></param>
        public Polygon ReadNodeFile(string nodefilename, bool readElements)
        {
            Polygon data;

            startIndex = 0;

            string[] line;
            int invertices = 0, attributes = 0, nodemarkers = 0;

            using (var reader = new StreamReader(nodefilename))
            {
                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file.");
                }

                // Read number of vertices, number of dimensions, number of vertex
                // attributes, and number of boundary markers.
                invertices = int.Parse(line[0]);

                if (invertices < 3)
                {
                    throw new Exception("Input must have at least three input vertices.");
                }

                if (line.Length > 1)
                {
                    if (int.Parse(line[1]) != 2)
                    {
                        throw new Exception("Triangle only works with two-dimensional meshes.");
                    }
                }

                if (line.Length > 2)
                {
                    attributes = int.Parse(line[2]);
                }

                if (line.Length > 3)
                {
                    nodemarkers = int.Parse(line[3]);
                }

                data = new Polygon(invertices);

                // Read the vertices.
                if (invertices > 0)
                {
                    for (int i = 0; i < invertices; i++)
                    {
                        if (!TryReadLine(reader, out line))
                        {
                            throw new Exception("Can't read input file (vertices).");
                        }

                        if (line.Length < 3)
                        {
                            throw new Exception("Invalid vertex.");
                        }

                        if (i == 0)
                        {
                            startIndex = int.Parse(line[0], nfi);
                        }

                        ReadVertex(data.Points, i, line, attributes, nodemarkers);
                    }
                }
            }

            if (readElements)
            {
                // Read area file
                string elefile = Path.ChangeExtension(nodefilename, ".ele");
                if (File.Exists(elefile))
                {
                    ReadEleFile(elefile, true);
                }
            }

            return data;
        }

        /// <summary>
        /// Read the vertices and segments from a .poly file.
        /// </summary>
        /// <param name="polyfilename"></param>
        /// <remarks>Will NOT read associated .ele by default.</remarks>
        public Polygon ReadPolyFile(string polyfilename)
        {
            return ReadPolyFile(polyfilename, false, false);
        }

        /// <summary>
        /// Read the vertices and segments from a .poly file.
        /// </summary>
        /// <param name="polyfilename"></param>
        /// <param name="readElements">If true, look for an associated .ele file.</param>
        /// <remarks>Will NOT read associated .area by default.</remarks>
        public Polygon ReadPolyFile(string polyfilename, bool readElements)
        {
            return ReadPolyFile(polyfilename, readElements, false);
        }

        /// <summary>
        /// Read the vertices and segments from a .poly file.
        /// </summary>
        /// <param name="polyfilename"></param>
        /// <param name="readElements">If true, look for an associated .ele file.</param>
        /// <param name="readElements">If true, look for an associated .area file.</param>
        public Polygon ReadPolyFile(string polyfilename, bool readElements, bool readArea)
        {
            // Read poly file
            Polygon data;

            startIndex = 0;

            string[] line;
            int invertices = 0, attributes = 0, nodemarkers = 0;

            using (var reader = new StreamReader(polyfilename))
            {
                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file.");
                }

                // Read number of vertices, number of dimensions, number of vertex
                // attributes, and number of boundary markers.
                invertices = int.Parse(line[0]);

                if (line.Length > 1)
                {
                    if (int.Parse(line[1]) != 2)
                    {
                        throw new Exception("Triangle only works with two-dimensional meshes.");
                    }
                }

                if (line.Length > 2)
                {
                    attributes = int.Parse(line[2]);
                }

                if (line.Length > 3)
                {
                    nodemarkers = int.Parse(line[3]);
                }

                // Read the vertices.
                if (invertices > 0)
                {
                    data = new Polygon(invertices);

                    for (int i = 0; i < invertices; i++)
                    {
                        if (!TryReadLine(reader, out line))
                        {
                            throw new Exception("Can't read input file (vertices).");
                        }

                        if (line.Length < 3)
                        {
                            throw new Exception("Invalid vertex.");
                        }

                        if (i == 0)
                        {
                            // Set the start index!
                            startIndex = int.Parse(line[0], nfi);
                        }

                        ReadVertex(data.Points, i, line, attributes, nodemarkers);
                    }
                }
                else
                {
                    // If the .poly file claims there are zero vertices, that means that
                    // the vertices should be read from a separate .node file.
                    data = ReadNodeFile(Path.ChangeExtension(polyfilename, ".node"));

                    invertices = data.Points.Count;
                }

                var points = data.Points;

                if (points.Count == 0)
                {
                    throw new Exception("No nodes available.");
                }

                // Read the segments from a .poly file.

                // Read number of segments and number of boundary markers.
                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file (segments).");
                }

                int insegments = int.Parse(line[0]);

                int segmentmarkers = 0;
                if (line.Length > 1)
                {
                    segmentmarkers = int.Parse(line[1]);
                }

                int end1, end2, mark;
                // Read and insert the segments.
                for (int i = 0; i < insegments; i++)
                {
                    if (!TryReadLine(reader, out line))
                    {
                        throw new Exception("Can't read input file (segments).");
                    }

                    if (line.Length < 3)
                    {
                        throw new Exception("Segment has no endpoints.");
                    }

                    // TODO: startIndex ok?
                    end1 = int.Parse(line[1]) - startIndex;
                    end2 = int.Parse(line[2]) - startIndex;
                    mark = 0;

                    if (segmentmarkers > 0 && line.Length > 3)
                    {
                        mark = int.Parse(line[3]);
                    }

                    if ((end1 < 0) || (end1 >= invertices))
                    {
                        if (Log.Verbose)
                        {
                            Log.Instance.Warning("Invalid first endpoint of segment.",
                                "MeshReader.ReadPolyfile()");
                        }
                    }
                    else if ((end2 < 0) || (end2 >= invertices))
                    {
                        if (Log.Verbose)
                        {
                            Log.Instance.Warning("Invalid second endpoint of segment.",
                                "MeshReader.ReadPolyfile()");
                        }
                    }
                    else
                    {
                        data.Add(new Segment(points[end1], points[end2], mark));
                    }
                }

                // Read holes from a .poly file.

                // Read the holes.
                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file (holes).");
                }

                int holes = int.Parse(line[0]);
                if (holes > 0)
                {
                    for (int i = 0; i < holes; i++)
                    {
                        if (!TryReadLine(reader, out line))
                        {
                            throw new Exception("Can't read input file (holes).");
                        }

                        if (line.Length < 3)
                        {
                            throw new Exception("Invalid hole.");
                        }

                        data.Holes.Add(new Point(double.Parse(line[1], nfi),
                            double.Parse(line[2], nfi)));
                    }
                }

                // Read area constraints (optional).
                if (TryReadLine(reader, out line))
                {
                    int id, regions = int.Parse(line[0]);

                    if (regions > 0)
                    {
                        for (int i = 0; i < regions; i++)
                        {
                            if (!TryReadLine(reader, out line))
                            {
                                throw new Exception("Can't read input file (region).");
                            }

                            if (line.Length < 4)
                            {
                                throw new Exception("Invalid region attributes.");
                            }

                            if (!int.TryParse(line[3], out id))
                            {
                                id = i;
                            }

                            double area = 0.0;

                            if (line.Length > 4)
                            {
                                double.TryParse(line[4], NumberStyles.Number, nfi, out area);
                            }

                            // Triangle's .poly file format allows region definitions with
                            // either 4 or 5 parameters, and different interpretations for
                            // them depending on the number of parameters.
                            //
                            // See http://www.cs.cmu.edu/~quake/triangle.poly.html
                            //
                            // The .NET version will interpret the fourth parameter always
                            // as an integer region id and the optional fifth parameter as
                            // an area constraint.

                            data.Regions.Add(new RegionPointer(
                                double.Parse(line[1], nfi), // Region x
                                double.Parse(line[2], nfi), // Region y
                                id, area));
                        }
                    }
                }
            }

            // Read ele file
            if (readElements)
            {
                string elefile = Path.ChangeExtension(polyfilename, ".ele");
                if (File.Exists(elefile))
                {
                    ReadEleFile(elefile, readArea);
                }
            }

            return data;
        }

        /// <summary>
        /// Read elements from an .ele file.
        /// </summary>
        /// <param name="elefilename">The file name.</param>
        /// <returns>A list of triangles.</returns>
        public List<ITriangle> ReadEleFile(string elefilename)
        {
            return ReadEleFile(elefilename, false);
        }

        /// <summary>
        /// Read the elements from an .ele file.
        /// </summary>
        /// <param name="elefilename"></param>
        /// <param name="data"></param>
        /// <param name="readArea"></param>
        private List<ITriangle> ReadEleFile(string elefilename, bool readArea)
        {
            int intriangles = 0, attributes = 0;

            List<ITriangle> triangles;

            using (var reader = new StreamReader(elefilename))
            {
                // Read number of elements and number of attributes.
                string[] line;
                bool validRegion = false;

                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file (elements).");
                }

                intriangles = int.Parse(line[0]);

                // We irgnore index 1 (number of nodes per triangle)
                attributes = 0;
                if (line.Length > 2)
                {
                    attributes = int.Parse(line[2]);
                    validRegion = true;
                }

                if (attributes > 1)
                {
                    Log.Instance.Warning("Triangle attributes not supported.", "FileReader.Read");
                }

                triangles = new List<ITriangle>(intriangles);

                InputTriangle tri;

                // Read triangles.
                for (int i = 0; i < intriangles; i++)
                {
                    if (!TryReadLine(reader, out line))
                    {
                        throw new Exception("Can't read input file (elements).");
                    }

                    if (line.Length < 4)
                    {
                        throw new Exception("Triangle has no nodes.");
                    }

                    // TODO: startIndex ok?
                    tri = new InputTriangle(
                        int.Parse(line[1]) - startIndex,
                        int.Parse(line[2]) - startIndex,
                        int.Parse(line[3]) - startIndex);

                    // Read triangle region
                    if (attributes > 0 && validRegion)
                    {
                        int region = 0;
                        validRegion = int.TryParse(line[4], out region);
                        tri.label = region;
                    }

                    triangles.Add(tri);
                }
            }

            // Read area file
            if (readArea)
            {
                string areafile = Path.ChangeExtension(elefilename, ".area");
                if (File.Exists(areafile))
                {
                    ReadAreaFile(areafile, intriangles);
                }
            }

            return triangles;
        }

        /// <summary>
        /// Read the area constraints from an .area file.
        /// </summary>
        /// <param name="areafilename"></param>
        /// <param name="intriangles"></param>
        /// <param name="data"></param>
        private double[] ReadAreaFile(string areafilename, int intriangles)
        {
            double[] data = null;

            using (var reader = new StreamReader(areafilename))
            {
                string[] line;

                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file (area).");
                }

                if (int.Parse(line[0]) != intriangles)
                {
                    Log.Instance.Warning("Number of area constraints doesn't match number of triangles.",
                        "ReadAreaFile()");
                    return null;
                }

                data = new double[intriangles];

                // Read area constraints.
                for (int i = 0; i < intriangles; i++)
                {
                    if (!TryReadLine(reader, out line))
                    {
                        throw new Exception("Can't read input file (area).");
                    }

                    if (line.Length != 2)
                    {
                        throw new Exception("Triangle has no nodes.");
                    }

                    data[i] = double.Parse(line[1], nfi);
                }
            }

            return data;
        }

        /// <summary>
        /// Read an .edge file.
        /// </summary>
        /// <param name="edgeFile">The file name.</param>
        /// <param name="invertices">The number of input vertices (read from a .node or .poly file).</param>
        /// <returns>A List of edges.</returns>
        public List<Edge> ReadEdgeFile(string edgeFile, int invertices)
        {
            // Read poly file
            List<Edge> data = null;

            startIndex = 0;

            string[] line;

            using (var reader = new StreamReader(edgeFile))
            {
                // Read the edges from a .edge file.

                // Read number of segments and number of boundary markers.
                if (!TryReadLine(reader, out line))
                {
                    throw new Exception("Can't read input file (segments).");
                }

                int inedges = int.Parse(line[0]);

                int edgemarkers = 0;
                if (line.Length > 1)
                {
                    edgemarkers = int.Parse(line[1]);
                }

                if (inedges > 0)
                {
                    data = new List<Edge>(inedges);
                }

                int end1, end2, mark;
                // Read and insert the segments.
                for (int i = 0; i < inedges; i++)
                {
                    if (!TryReadLine(reader, out line))
                    {
                        throw new Exception("Can't read input file (segments).");
                    }

                    if (line.Length < 3)
                    {
                        throw new Exception("Segment has no endpoints.");
                    }

                    // TODO: startIndex ok?
                    end1 = int.Parse(line[1]) - startIndex;
                    end2 = int.Parse(line[2]) - startIndex;
                    mark = 0;

                    if (edgemarkers > 0 && line.Length > 3)
                    {
                        mark = int.Parse(line[3]);
                    }

                    if ((end1 < 0) || (end1 >= invertices))
                    {
                        if (Log.Verbose)
                        {
                            Log.Instance.Warning("Invalid first endpoint of segment.",
                                "MeshReader.ReadPolyfile()");
                        }
                    }
                    else if ((end2 < 0) || (end2 >= invertices))
                    {
                        if (Log.Verbose)
                        {
                            Log.Instance.Warning("Invalid second endpoint of segment.",
                                "MeshReader.ReadPolyfile()");
                        }
                    }
                    else
                    {
                        data.Add(new Edge(end1, end2, mark));
                    }
                }
            }

            return data;
        }
    }
}
