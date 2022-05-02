// Copyright © Joerg Battermann 2014, Matt Hunt 2017

using System;
using System.Collections.Generic;

namespace GeoJSON.Net.CoordinateReferenceSystem
{
    /// <summary>
    /// Defines the Linked CRS type.
    /// </summary>
    /// <remarks>
    /// This was originally defined in the spec http://geojson.org/geojson-spec.html#named-crs
    /// The current RFC removes the CRS type, but allows to be left in for backwards compatibility. 
    /// See https://tools.ietf.org/html/rfc7946#section-4
    /// </remarks>
    public class LinkedCRS : CRSBase, ICRSObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedCRS" /> class.
        /// </summary>
        /// <param name="href">
        /// The mandatory href member must be a dereferenceable URI.
        /// </param>
        /// <param name="type">
        /// The optional type member will be put in the properties Dictionary
        /// </param>
        public LinkedCRS(string href, string type = "")
        {
            if (href == null)
            {
                throw new ArgumentNullException(nameof(href));
            }

            Uri uri;
            if (href.Length == 0 || !Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out uri))
            {
                throw new ArgumentException("must be a dereferenceable URI", nameof(href));
            }

            Properties = new Dictionary<string, object> { { "href", href } };

            if (!string.IsNullOrEmpty(type))
            {
                Properties.Add("type", type);
            }

            Type = CRSType.Link;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedCRS" /> class.
        /// </summary>
        /// <param name="href">
        /// The mandatory href member must be a dereferenceable URI.
        /// </param>
        /// <param name="type">
        /// The optional type member will be put in the properties Dictionary
        /// </param>
        public LinkedCRS(Uri href, string type = "") : this(href != null ? href.ToString() : null, type)
        {
        }
    }
}