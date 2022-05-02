// Copyright © Joerg Battermann 2014, Matt Hunt 2017

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeoJSON.Net.CoordinateReferenceSystem
{
    /// <summary>
    /// Base class for all IGeometryObject implementing types
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class CRSBase : IEqualityComparer<CRSBase>, IEquatable<CRSBase>
    {
        /// <summary>
        /// Gets the properties.
        /// </summary>
        [JsonProperty(PropertyName = "properties", Required = Required.Always)]
        public Dictionary<string, object> Properties { get; internal set; }

        /// <summary>
        /// Gets the type of the GeometryObject object.
        /// </summary>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public CRSType Type { get; internal set; }

        #region IEqualityComparer, IEquatable

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as CRSBase);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public bool Equals(CRSBase other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public bool Equals(CRSBase left, CRSBase right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (ReferenceEquals(null, right))
            {
                return false;
            }

            if (left.Type != right.Type)
            {
                return false;
            }

            var leftIsNull = ReferenceEquals(null, left.Properties);
            var rightIsNull = ReferenceEquals(null, right.Properties);
            var bothAreMissing = leftIsNull && rightIsNull;

            if (bothAreMissing || leftIsNull != rightIsNull)
            {
                return bothAreMissing;
            }

            foreach (var item in left.Properties)
            {
                if (!right.Properties.TryGetValue(item.Key, out object rightValue))
                {
                    return false;
                }
                if (!object.Equals(item.Value, rightValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public static bool operator ==(CRSBase left, CRSBase right)
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
        public static bool operator !=(CRSBase left, CRSBase right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = ((int)Type).GetHashCode();
            if (Properties != null)
            {
                foreach (var item in Properties)
                {
                    string toString;
                    if (item.Value == null)
                    {
                        toString = item.Key;
                    }
                    else
                    {
                        toString = $"{item.Key}:{item.Value}";
                    }
                    hashCode = (hashCode * 397) ^ toString.GetHashCode();
                }
            }
            return hashCode;
        }

        /// <summary>
        /// Returns the hash code for the specified object
        /// </summary>
        public int GetHashCode(CRSBase obj)
        {
            return obj.GetHashCode();
        }

        #endregion
    }
}