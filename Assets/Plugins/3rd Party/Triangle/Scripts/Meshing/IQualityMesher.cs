
namespace TriangleNet.Meshing
{
    using TriangleNet.Geometry;

    /// <summary>
    /// Interface for polygon triangulation with quality constraints.
    /// </summary>
    public interface IQualityMesher
    {
        /// <summary>
        /// Triangulates a polygon, applying quality options.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="quality">Quality options.</param>
        /// <returns>Mesh</returns>
        IMesh Triangulate(IPolygon polygon, QualityOptions quality);

        /// <summary>
        /// Triangulates a polygon, applying quality and constraint options.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="options">Constraint options.</param>
        /// <param name="quality">Quality options.</param>
        /// <returns>Mesh</returns>
        IMesh Triangulate(IPolygon polygon, ConstraintOptions options, QualityOptions quality);
    }
}
