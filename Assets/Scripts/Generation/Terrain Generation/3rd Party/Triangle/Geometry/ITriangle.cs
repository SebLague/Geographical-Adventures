// -----------------------------------------------------------------------
// <copyright file="ITriangle.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using TriangleNet.Topology;

    /// <summary>
    /// Triangle interface.
    /// </summary>
    public interface ITriangle
    {
        /// <summary>
        /// Gets or sets the triangle ID.
        /// </summary>
        int ID { get; set; }

        /// <summary>
        /// Gets or sets a general-purpose label.
        /// </summary>
        /// <remarks>
        /// This is used for region information.
        /// </remarks>
        int Label { get; set; }

        /// <summary>
        /// Gets or sets the triangle area constraint.
        /// </summary>
        double Area { get; set; }

        /// <summary>
        /// Gets the vertex at given index.
        /// </summary>
        /// <param name="index">The local index (0, 1 or 2).</param>
        /// <returns>The vertex.</returns>
        Vertex GetVertex(int index);

        /// <summary>
        /// Gets the ID of the vertex at given index.
        /// </summary>
        /// <param name="index">The local index (0, 1 or 2).</param>
        /// <returns>The vertex ID.</returns>
        int GetVertexID(int index);

        /// <summary>
        /// Gets the neighbor triangle at given index.
        /// </summary>
        /// <param name="index">The local index (0, 1 or 2).</param>
        /// <returns>The neighbor triangle.</returns>
        ITriangle GetNeighbor(int index);

        /// <summary>
        /// Gets the ID of the neighbor triangle at given index.
        /// </summary>
        /// <param name="index">The local index (0, 1 or 2).</param>
        /// <returns>The neighbor triangle ID.</returns>
        int GetNeighborID(int index);

        /// <summary>
        /// Gets the segment at given index.
        /// </summary>
        /// <param name="index">The local index (0, 1 or 2).</param>
        /// <returns>The segment.</returns>
        ISegment GetSegment(int index);
    }
}
