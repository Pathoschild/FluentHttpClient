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
        /// <param name="ignoreNullArguments">Indicates whether to ignore arguments with null value.</param>
        /// <remarks>This method can't use <see cref="System.Net.Http.UriExtensions.ParseQueryString" /> because it isn't compatible with portable class libraries.</remarks>
        public static Uri WithArguments(this Uri uri, bool ignoreNullArguments, params KeyValuePair<string, object>[] arguments)
        {
            string argumentsWithValue = string.Join("&",
                from argument in arguments
                where argument.Value != null
                let key = WebUtility.UrlEncode(argument.Key)
                let value = WebUtility.UrlEncode(argument.Value.ToString())
                select key + "=" + value
            );

            string argumentsWithoutValue = string.Join("&",
                from argument in arguments
                where argument.Value == null
                let key = WebUtility.UrlEncode(argument.Key)
                select key + "="
            );

            string queryString = argumentsWithValue + (!ignoreNullArguments && !string.IsNullOrEmpty(argumentsWithoutValue) ? "&" + argumentsWithoutValue : string.Empty);

            return new Uri(
                uri
                + (string.IsNullOrWhiteSpace(uri.Query) ? "?" : "&")
                + queryString
            );
        }
    }
}
