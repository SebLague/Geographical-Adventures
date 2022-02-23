// -----------------------------------------------------------------------
// <copyright file="Enums.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet
{
    /// <summary>
    /// The type of the mesh vertex.
    /// </summary>
    public enum VertexType { InputVertex, SegmentVertex, FreeVertex, DeadVertex, UndeadVertex };

    /// <summary>
    /// Node renumbering algorithms.
    /// </summary>
    public enum NodeNumbering { None, Linear, CuthillMcKee };

    /// <summary>
    /// Labels that signify the result of point location.
    /// </summary>
    /// <remarks>The result of a search indicates that the point falls in the 
    /// interior of a triangle, on an edge, on a vertex, or outside the mesh.
    /// </remarks>
    public enum LocateResult { InTriangle, OnEdge, OnVertex, Outside };

    /// <summary>
    /// Labels that signify the result of vertex insertion.
    /// </summary>
    /// <remarks>The result indicates that the vertex was inserted with complete 
    /// success, was inserted but encroaches upon a subsegment, was not inserted 
    /// because it lies on a segment, or was not inserted because another vertex 
    /// occupies the same location.
    /// </remarks>
    enum InsertVertexResult { Successful, Encroaching, Violating, Duplicate };

    /// <summary>
    /// Labels that signify the result of direction finding.
    /// </summary>
    /// <remarks>The result indicates that a segment connecting the two query 
    /// points falls within the direction triangle, along the left edge of the 
    /// direction triangle, or along the right edge of the direction triangle.
    /// </remarks>
    enum FindDirectionResult { Within, Leftcollinear, Rightcollinear };
}
