// -----------------------------------------------------------------------
// <copyright file="TriangleFormat.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using TriangleNet.Geometry;
    using TriangleNet.Meshing;

    /// <summary>
    /// Implements geometry and mesh file formats of the the original Triangle code.
    /// </summary>
    public class TriangleFormat : IPolygonFormat, IMeshFormat
    {
        public bool IsSupported(string file)
        {
            string ext = Path.GetExtension(file).ToLower();

            if (ext == ".node" || ext == ".poly" || ext == ".ele")
            {
                return true;
            }

            return false;
        }

        public IMesh Import(string filename)
        {
            string ext = Path.GetExtension(filename);

            if (ext == ".node" || ext == ".poly" || ext == ".ele")
            {
                List<ITriangle> triangles;
                Polygon geometry;

                (new TriangleReader()).Read(filename, out geometry, out triangles);

                if (geometry != null && triangles != null)
                {
                    return Converter.ToMesh(geometry, triangles.ToArray());
                }
            }

            throw new NotSupportedException("Could not load '" + filename + "' file.");
        }

        public void Write(IMesh mesh, string filename)
        {
            var writer = new TriangleWriter();

            writer.WritePoly((Mesh)mesh, Path.ChangeExtension(filename, ".poly"));
            writer.WriteElements((Mesh)mesh, Path.ChangeExtension(filename, ".ele"));
        }

        public void Write(IMesh mesh, Stream stream)
        {
            throw new NotImplementedException();
        }

        public IPolygon Read(string filename)
        {
            string ext = Path.GetExtension(filename);

            if (ext == ".node")
            {
                return (new TriangleReader()).ReadNodeFile(filename);
            }
            else if (ext == ".poly")
            {
                return (new TriangleReader()).ReadPolyFile(filename);
            }

            throw new NotSupportedException("File format '" + ext + "' not supported.");
        }


        public void Write(IPolygon polygon, string filename)
        {
            (new TriangleWriter()).WritePoly(polygon, filename);
        }

        public void Write(IPolygon polygon, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
