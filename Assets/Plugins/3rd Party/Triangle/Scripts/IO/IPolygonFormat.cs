// -----------------------------------------------------------------------
// <copyright file="IPolygonFormat.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.IO
{
    using System.IO;
    using TriangleNet.Geometry;

    /// <summary>
    /// Interface for geometry input.
    /// </summary>
    public interface IPolygonFormat : IFileFormat
    {
        /// <summary>
        /// Read a file containing polygon geometry.
        /// </summary>
        /// <param name="filename">The path of the file to read.</param>
        /// <returns>An instance of the <see cref="IPolygon" /> class.</returns>
        IPolygon Read(string filename);

        /// <summary>
        /// Save a polygon geometry to disk.
        /// </summary>
        /// <param name="polygon">An instance of the <see cref="IPolygon" /> class.</param>
        /// <param name="filename">The path of the file to save.</param>
        void Write(IPolygon polygon, string filename);

        /// <summary>
        /// Save a polygon geometry to a <see cref="Stream" />.
        /// </summary>
        /// <param name="polygon">An instance of the <see cref="IPolygon" /> class.</param>
        /// <param name="stream">The stream to save to.</param>
        void Write(IPolygon polygon, Stream stream);
    }
}
