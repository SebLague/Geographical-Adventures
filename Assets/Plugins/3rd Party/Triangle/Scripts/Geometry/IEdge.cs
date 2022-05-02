// -----------------------------------------------------------------------
// <copyright file="IEdge.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    public interface IEdge
    {
        /// <summary>
        /// Gets the first endpoints index.
        /// </summary>
        int P0 { get; }

        /// <summary>
        /// Gets the second endpoints index.
        /// </summary>
        int P1 { get; }

        /// <summary>
        /// Gets or sets a general-purpose label.
        /// </summary>
        /// <remarks>
        /// This is used for the segments boundary mark.
        /// </remarks>
        int Label { get; }
    }
}
