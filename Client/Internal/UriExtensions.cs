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
        /// <param name="ignoreNullArguments">Whether to ignore arguments with a null value.</param>
        /// <remarks>This method can't use <see cref="System.Net.Http.UriExtensions.ParseQueryString" /> because it isn't compatible with portable class libraries.</remarks>
        public static Uri WithArguments(this Uri uri, bool ignoreNullArguments, params KeyValuePair<string, object?>[] arguments)
        {
            // concat new arguments
            string newQueryString = string.Join("&",
                from argument in arguments
                where !ignoreNullArguments || argument.Value != null
                let key = WebUtility.UrlEncode(argument.Key)
                let value = argument.Value != null ? WebUtility.UrlEncode(argument.Value.ToString()) : string.Empty
                select key + "=" + value
            );
            if (string.IsNullOrWhiteSpace(newQueryString))
                return uri;

            // adjust URL
            UriBuilder builder = new UriBuilder(uri);
            builder.Query = !string.IsNullOrWhiteSpace(builder.Query)
                ? builder.Query.TrimStart('?') + "&" + newQueryString
                : newQueryString;

            return builder.Uri;
        }
    }
}
