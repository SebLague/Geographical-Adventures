// Copyright © Joerg Battermann 2014, Matt Hunt 2017

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GeoJSON.Net.Converters;
using Newtonsoft.Json;

namespace GeoJSON.Net.Geometry
{
    /// <summary>
    /// Defines the Polygon type.
    /// Coordinates of a Polygon are a list of linear rings coordinate arrays. The first element in 
    /// the array represents the exterior ring. Any subsequent elements represent interior rings (or holes).
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.6
    /// </remarks>
    public class Polygon : GeoJSONObject, IGeometryObject, IEqualityComparer<Polygon>, IEquatable<Polygon>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// </summary>
        /// <param name="coordinates">
        /// The linear rings with the first element in the array representing the exterior ring. 
        /// Any subsequent elements represent interior rings (or holes).
        /// </param>
        public Polygon(IEnumerable<LineString> coordinates)
        {
            Coordinates = new ReadOnlyCollection<LineString>(
                coordinates?.ToArray() ?? throw new ArgumentNullException(nameof(coordinates)));
            if (Coordinates.Any(linearRing => !linearRing.IsLinearRing()))
            {
                throw new ArgumentException("All elements must be closed LineStrings with 4 or more positions" +
                                            " (see GeoJSON spec at 'https://tools.ietf.org/html/rfc7946#section-3.1.6').", nameof(coordinates));
            }

            
        }

        /// <summary>
        /// Initializes a new <see cref="Polygon" /> from a 3-d array of <see cref="double" />s
        /// that matches the "coordinates" field in the JSON representation.
        /// </summary>
        [JsonConstructor]
        public Polygon(IEnumerable<IEnumerable<IEnumerable<double>>> coordinates)
            : this(coordinates?.Select(line => new LineString(line))
              ?? throw new ArgumentNullException(nameof(coordinates)))
        {
        }

        public override GeoJSONObjectType Type => GeoJSONObjectType.Polygon;

        /// <summary>
        /// Gets the list of linestrings defining this <see cref="Polygon" />.
        /// </summary>
        [JsonProperty("coordinates", Required = Required.Always)]
        [JsonConverter(typeof(LineStringEnumerableConverter))]
        public ReadOnlyCollection<LineString> Coordinates { get; }

        #region IEqualityComparer, IEquatable

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as Polygon);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public bool Equals(Polygon other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public bool Equals(Polygon left, Polygon right)
        {
            if (base.Equals(left, right))
            {
                return left.Coordinates.SequenceEqual(right.Coordinates);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public static bool operator ==(Polygon left, Polygon right)
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
        public static bool operator !=(Polygon left, Polygon right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            foreach (var item in Coordinates)
            {
                hash = (hash * 397) ^ item.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns the hash code for the specified object
        /// </summary>
        public int GetHashCode(Polygon other)
        {
            return other.GetHashCode();
        }

        #endregion
    }
}