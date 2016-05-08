using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Pathoschild.Http.Client.Internal
{
    /// <summary>Provides extension methods for <see cref="Uri"/>.</summary>
    internal static class UriExtensions
    {
        /// <summary>Add raw arguments to the URI's query string.</summary>
        /// <param name="uri">The URI to extend.</param>
        /// <param name="arguments">The raw arguments to add.</param>
        /// <remarks>This method can't use <see cref="System.Net.Http.UriExtensions.ParseQueryString" /> because it isn't compatible with portable class libraries.</remarks>
        public static Uri WithArguments(this Uri uri, params KeyValuePair<string, object>[] arguments)
        {
            string queryString = string.Join("&",
                from argument in arguments
                let key = WebUtility.UrlEncode(argument.Key)
                let value = argument.Value != null ? WebUtility.UrlEncode(argument.Value.ToString()) : string.Empty
                select key + "=" + value
            );
            return new Uri(
                uri
                + (string.IsNullOrWhiteSpace(uri.Query) ? "?" : "&")
                + queryString
            );
        }
    }
}