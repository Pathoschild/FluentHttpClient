// ReSharper disable once CheckNamespace -- deliberate to make it available without an explicit namespace import
namespace Pathoschild.Http.Client
{
    /// <summary>Wraps newer .NET features that improve performance, but aren't available on older platforms.</summary>
    internal static class LegacyShims
    {
        /*********
        ** Arrays
        *********/
        /// <summary>Get an empty array without allocating a new array each time.</summary>
        /// <typeparam name="T">The array value type.</typeparam>
        public static T[] EmptyArray<T>()
        {
#if NET452
            return EmptyArrayShim<T>.Value;
#else
            return System.Array.Empty<T>();
#endif
        }

#if NET452
        /// <summary>A singleton class for an array type.</summary>
        /// <typeparam name="T">The array value type.</typeparam>
        private static class EmptyArrayShim<T>
        {
            /// <summary>The empty array instance for the type.</summary>
            internal static readonly T[] Value = new T[0];
        }
#endif


        /*********
        ** Strings
        *********/
#if !NET5_0_OR_GREATER
        /// <summary>Get whether the first character of the string is the given character.</summary>
        /// <param name="value">The string to search.</param>
        /// <param name="ch">The character to find.</param>
        public static bool StartsWith(this string value, char ch)
        {
            return value.Length > 0 && value[0] == ch;
        }

        /// <summary>Get whether the last character of the string is the given character.</summary>
        /// <param name="value">The string to search.</param>
        /// <param name="ch">The character to find.</param>
        public static bool EndsWith(this string value, char ch)
        {
            return value.Length > 0 && value[value.Length - 1] == ch;
        }
#endif
    }
}
