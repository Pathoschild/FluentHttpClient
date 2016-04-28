using System.Collections.Generic;
using System.Linq;
using Pathoschild.Http.Client.Extensibility;

namespace Pathoschild.Http.Client.Default
{
    /// <summary>Provides convenience methods for configuring the HTTP client.</summary>
    public static class FluentClientExtensions
    {
        /// <summary>Remove the first HTTP filter of the specified type.</summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <param name="filters">The filters to adjust.</param>
        /// <returns>Returns whether a filter was removed.</returns>
        public static bool Remove<TFilter>(this List<IHttpFilter> filters)
            where TFilter : IHttpFilter
        {
            TFilter filter = filters.OfType<TFilter>().FirstOrDefault();
            return filter != null && filters.Remove(filter);

        }
    }
}