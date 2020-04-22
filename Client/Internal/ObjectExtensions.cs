using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pathoschild.Http.Client.Internal
{
    /// <summary>Provides extension methods for <see cref="object"/>.</summary>
    internal static class ObjectExtensions
    {
        /// <summary>Get the key/value arguments from an object representation.</summary>
        /// <param name="arguments">The arguments to parse.</param>
        public static IEnumerable<KeyValuePair<string, object?>> GetKeyValueArguments(this object? arguments)
        {
            if (arguments == null)
                return Enumerable.Empty<KeyValuePair<string, object?>>();

            return (
                from property in arguments.GetType().GetRuntimeProperties()
                where property.CanRead && property.GetIndexParameters().Any() != true
                select new KeyValuePair<string, object?>(property.Name, property.GetValue(arguments))
            ).ToArray();
        }
    }
}
