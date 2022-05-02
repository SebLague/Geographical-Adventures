// Copyright © Joerg Battermann 2014, Matt Hunt 2017

using System.Runtime.Serialization;

namespace GeoJSON.Net.CoordinateReferenceSystem
{
    /// <summary>
    /// Defines the GeoJSON Coordinate Reference System Objects (CRS) types as originally defined in the geojson.org v1.0 spec
    /// </summary>
    /// <remarks>
    /// Originally defined http://geojson.org/geojson-spec.html#coordinate-reference-system-objects
    /// The current RFC removes the CRS type, but allows to be left in for backwards compatibility. See https://tools.ietf.org/html/rfc7946#section-4
    /// </remarks>
    public enum CRSType
    {
        /// <summary>
        /// Defines a CRS type where the CRS cannot be assumed
        /// </summary>
        [EnumMember(Value = "unspecified")]
        Unspecified,

        /// <summary>
        /// Defines the Named CRS type.
        /// </summary>
        [EnumMember(Value = "name")]
        Name,

        /// <summary>
        /// Defines the Linked CRS type.
        /// </summary>
        [EnumMember(Value = "link")]
        Link
    }
}