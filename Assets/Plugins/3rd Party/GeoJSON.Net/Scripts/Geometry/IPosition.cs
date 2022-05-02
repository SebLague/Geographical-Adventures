// Copyright © Joerg Battermann 2014, Matt Hunt 2017
namespace GeoJSON.Net.Geometry
{
    /// <summary>
    /// Defines the Geographic Position type.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.1
    /// </remarks>
    public interface IPosition
    {
        /// <summary>
        /// Gets the altitude.
        /// </summary>
        double? Altitude { get; }
    
        /// <summary>
        /// Gets the latitude.
        /// </summary>
        /// <value>The latitude.</value>
         double Latitude { get; }

        /// <summary>
        /// Gets the longitude.
        /// </summary>
        /// <value>The longitude.</value>
        double Longitude { get; }
    }
}