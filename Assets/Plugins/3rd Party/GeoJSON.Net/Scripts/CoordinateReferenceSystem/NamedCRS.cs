// Copyright © Joerg Battermann 2014, Matt Hunt 2017

using System;
using System.Collections.Generic;

namespace GeoJSON.Net.CoordinateReferenceSystem
{
    /// <summary>
    /// Defines the Named CRS type. 
    /// </summary>
    /// <remarks>
    /// See http://geojson.org/geojson-spec.html#named-crs
    /// The current RFC removes the CRS type, but allows to be left in for backwards compatibility.
    /// See https://tools.ietf.org/html/rfc7946#section-4
    /// </remarks>
    public class NamedCRS : CRSBase, ICRSObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedCRS" /> class.
        /// </summary>
        /// <param name="name">
        /// The mandatory name member must be a string identifying a coordinate reference system. OGC CRS URNs such as
        /// 'urn:ogc:def:crs:OGC:1.3:CRS84' shall be preferred over legacy identifiers such as 'EPSG:4326'.
        /// </param>
        public NamedCRS(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("must be specified", nameof(name));
            }

            Properties = new Dictionary<string, object> { { "name", name } };
            Type = CRSType.Name;
        }
    }
}