// -----------------------------------------------------------------------
// <copyright file="IPolygon.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Geometry
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Polygon interface.
    /// </summary>
    public interface IPolygon
    {
        /// <summary>
        /// Gets the vertices of the polygon.
        /// </summary>
        List<Vertex> Points { get; }

        /// <summary>
        /// Gets the segments of the polygon.
        /// </summary>
        List<ISegment> Segments { get; }

        /// <summary>
        /// Gets a list of points defining the holes of the polygon.
        /// </summary>
        List<Point> Holes { get; }

        /// <summary>
        /// Gets a list of pointers defining the regions of the polygon.
        /// </summary>
        List<RegionPointer> Regions { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the vertices have marks or not.
        /// </summary>
        bool HasPointMarkers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the segments have marks or not.
        /// </summary>
        bool HasSegmentMarkers { get; set; }

        [Obsolete("Use polygon.Add(contour) method instead.")]
        void AddContour(IEnumerable<Vertex> points, int marker, bool hole, bool convex);

        [Obsolete("Use polygon.Add(contour) method instead.")]
        void AddContour(IEnumerable<Vertex> points, int marker, Point hole);

        /// <summary>
        /// Compute the bounds of the polygon.
        /// </summary>
        /// <returns>Rectangle defining an axis-aligned bounding box.</returns>
        Rectangle Bounds();

        /// <summary>
        /// Add a vertex to the polygon.
        /// </summary>
        /// <param name="vertex">The vertex to insert.</param>
        void Add(Vertex vertex);

        /// <summary>
        /// Add a segment to the polygon.
        /// </summary>
        /// <param name="segment">The segment to insert.</param>
        /// <param name="insert">If true, both endpoints will be added to the points list.</param>
        void Add(ISegment segment, bool insert = false);

        /// <summary>
        /// Add a segment to the polygon.
        /// </summary>
        /// <param name="segment">The segment to insert.</param>
        /// <param name="index">The index of the segment endpoint to add to the points list (must be 0 or 1).</param>
        void Add(ISegment segment, int index);

        /// <summary>
        /// Add a contour to the polygon.
        /// </summary>
        /// <param name="contour">The contour to insert.</param>
        /// <param name="hole">Treat contour as a hole.</param>
        void Add(Contour contour, bool hole = false);

        /// <summary>
        /// Add a contour to the polygon.
        /// </summary>
        /// <param name="contour">The contour to insert.</param>
        /// <param name="hole">Point inside the contour, making it a hole.</param>
        void Add(Contour contour, Point hole);
    }
}
