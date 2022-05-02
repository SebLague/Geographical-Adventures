// Copyright © Joerg Battermann 2014, Matt Hunt 2017

using System;
using GeoJSON.Net.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeoJSON.Net.Geometry
{
    /// <summary>
    /// Defines the Point type.
    /// In geography, a point refers to a Position on a map, expressed in latitude and longitude.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.2
    /// </remarks>
    public class Point : GeoJSONObject, IGeometryObject, IEqualityComparer<Point>, IEquatable<Point>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Point" /> class.
        /// </summary>
        /// <param name="coordinates">The Position.</param>
        public Point(IPosition coordinates)
        {
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
        }

        public override GeoJSONObjectType Type => GeoJSONObjectType.Point;

        /// <summary>
        /// The <see cref="IPosition" /> underlying this point.
        /// </summary>
        [JsonProperty("coordinates", Required = Required.Always)]
        [JsonConverter(typeof(PositionConverter))]
        public IPosition Coordinates { get; }

        #region IEqualityComparer, IEquatable

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as Point);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public bool Equals(Point other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public bool Equals(Point left, Point right)
        {
            if (base.Equals(left, right))
            {
                return left.Coordinates.Equals(right.Coordinates);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public static bool operator ==(Point left, Point right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (ReferenceEquals(null, right))
            {
                return false;
            }
            return left != null && left.Equals(right);
        }

        /// <summary>
        /// Determines whether the specified object instances are not considered equal
        /// </summary>
        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }
        
        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = (hash * 397) ^ Coordinates.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns the hash code for the specified object
        /// </summary>
        public int GetHashCode(Point other)
        {
            return other.GetHashCode();
        }

        #endregion
    }
}