
namespace TriangleNet.Meshing
{
    using System;
    using TriangleNet.Geometry;

    /// <summary>
    /// Mesh constraint options for quality triangulation.
    /// </summary>
    public class QualityOptions
    {
        /// <summary>
        /// Gets or sets a maximum angle constraint.
        /// </summary>
        public double MaximumAngle { get; set; }

        /// <summary>
        /// Gets or sets a minimum angle constraint.
        /// </summary>
        public double MinimumAngle { get; set; }

        /// <summary>
        /// Gets or sets a maximum triangle area constraint.
        /// </summary>
        public double MaximumArea { get; set; }

        /// <summary>
        /// Gets or sets a user-defined triangle constraint.
        /// </summary>
        /// <remarks>
        /// The test function will be called for each triangle in the mesh. The
        /// second argument is the area of the triangle tested. If the function
        /// returns true, the triangle is considered bad and will be refined.
        /// </remarks>
        public Func<ITriangle, double, bool> UserTest { get; set; }

        /// <summary>
        /// Gets or sets an area constraint per triangle.
        /// </summary>
        /// <remarks>
        /// If this flag is set to true, the <see cref="ITriangle.Area"/> value will
        /// be used to check if a triangle needs refinement.
        /// </remarks>
        public bool VariableArea { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of Steiner points to be inserted into the mesh.
        /// </summary>
        /// <remarks>
        /// If the value is 0 (default), an unknown number of Steiner points may be inserted
        /// to meet the other quality constraints.
        /// </remarks>
        public int SteinerPoints { get; set; }
    }
}
