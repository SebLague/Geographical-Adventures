#if (!NET35 || !NET40)
using System;
using System.Reflection;
#endif

namespace GeoJSON.Net.Converters
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Encapsulates the framework-dependent preprocessor guards.
        /// </summary>
        public static bool IsAssignableFromType(this Type self, Type other)
        {
#if (NET35 || NET40)
            return self.IsAssignableFrom(other);
#else
            return self.GetTypeInfo().IsAssignableFrom(other.GetTypeInfo());
#endif
        }

    }
}