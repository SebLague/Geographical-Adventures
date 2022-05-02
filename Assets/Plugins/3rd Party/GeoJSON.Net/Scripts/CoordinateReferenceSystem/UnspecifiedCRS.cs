// Copyright © Joerg Battermann 2014, Matt Hunt 2017

namespace GeoJSON.Net.CoordinateReferenceSystem
{
    /// <summary>
    /// Represents an unspecified Coordinate Reference System 
    /// i.e. where a geojson object has a null crs
    /// </summary>
    public class UnspecifiedCRS : ICRSObject
    {
        /// <summary>
        /// Gets the CRS type.
        /// </summary>
        public CRSType Type
        {
            get
            {
                return CRSType.Unspecified;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as ICRSObject);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        public bool Equals(ICRSObject obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return Type == obj.Type;
        }

        /// <summary>
        /// Determines whether the specified object instances are considered equal
        /// </summary>
        public static bool operator ==(UnspecifiedCRS left, UnspecifiedCRS right)
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
        public static bool operator !=(UnspecifiedCRS left, UnspecifiedCRS right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

    }
}