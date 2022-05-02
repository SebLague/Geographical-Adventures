using System;
using System.Collections.Generic;
using GeoJSON.Net.Geometry;

namespace GeoJSON.Net
{
    internal static class PositionExtensions
    {
        internal static Position ToPosition(this IEnumerable<double> coordinates)
        {
            using (var enumerator = coordinates.GetEnumerator())
            {
                double lat, lng, alt;
                if (!enumerator.MoveNext())
                {
                    throw new ArgumentException("Expected 2 or 3 coordinates but got 0");
                }
                lng = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    throw new ArgumentException("Expected 2 or 3 coordinates but got 1");
                }
                lat = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    return new Position(lat, lng);
                }
                alt = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    throw new ArgumentException("Expected 2 or 3 coordinates but got >= 4");
                }
                return new Position(lat, lng, alt);
            }
        }
    }
}