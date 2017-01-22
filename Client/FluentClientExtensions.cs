using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;
using Pathoschild.Http.Client.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace Pathoschild.Http.Client
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
            IEnumerable<TFilter> tFilters = filters.OfType<TFilter>();
            return tFilters.Any() && filters.Remove(tFilters.First());
        }

        /// <summary>Create an asynchronous HTTP DELETE request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest DeleteAsync(this IClient client, string resource)
        {
            return client.SendAsync(HttpMethod.Delete, resource);
        }

        /// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest GetAsync(this IClient client, string resource)
        {
            return client.SendAsync(HttpMethod.Get, resource);
        }

        /// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest PostAsync(this IClient client, string resource)
        {
            return client.SendAsync(HttpMethod.Post, resource);
        }

        /// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <typeparam name="TBody">The request body type.</typeparam>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest PostAsync<TBody>(this IClient client, string resource, TBody body)
        {
            return client.PostAsync(resource).WithBody(body);
        }

        /// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest PutAsync(this IClient client, string resource)
        {
            return client.SendAsync(HttpMethod.Put, resource);
        }

        /// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <typeparam name="TBody">The request body type.</typeparam>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest PutAsync<TBody>(this IClient client, string resource, TBody body)
        {
            return client.PutAsync(resource).WithBody(body);
        }

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="client">The client.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public static IRequest SendAsync(this IClient client, HttpMethod method, string resource)
        {
            var uri = new Uri(client.BaseClient.BaseAddress, resource);
            var message = Factory.GetRequestMessage(method, uri, client.Formatters);
            return client.SendAsync(message);
        }

        /// <summary>Specify the username and password that will be used with every request.</summary>
        /// <param name="client">The client.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public static IClient SetBasicAuthentication(this IClient client, string username, string password)
        {
            return client.SetAuthentication("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Concat(username, ":", password))));
        }

        /// <summary>Specify the 'Bearer' authentication that will be used with every request.</summary>
        /// <param name="client">The client.</param>
        /// <param name="key">The bearer key (typically, this is an API key).</param>
        public static IClient SetBearerAuthentication(this IClient client, string key)
        {
            return client.SetAuthentication("Bearer", key);
        }

        /// <summary>Use basic authentication with this request.</summary>
        /// <param name="request">The request.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public static IRequest WithBasicAuthentication(this IRequest request, string username, string password)
        {
            return request.WithAuthentication("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Concat(username, ":", password))));
        }

        /// <summary>Use 'Bearer' authentication with this request.</summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The bearer key (typically, this is an API key).</param>
        public static IRequest WithBearerAuthentication(this IRequest request, string key)
        {
            return request.WithAuthentication("Bearer", key);
        }

        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="request">The request.</param>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="contentType">The request body format (or <c>null</c> to use the first supported Content-Type in the <see cref="Formatters"/>).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        /// <exception cref="InvalidOperationException">No MediaTypeFormatters are available on the API client for this content type.</exception>
        public static IRequest WithBody<T>(this IRequest request, T body, MediaTypeHeaderValue contentType = null)
        {
            MediaTypeFormatter formatter = Factory.GetFormatter(request.Formatters, contentType);
            string mediaType = contentType != null ? contentType.MediaType : null;
            return request.WithBody(body, formatter, mediaType);
        }

        /// <summary>Set the body content of the HTTP request.</summary>
        /// <param name="request">The request.</param>
        /// <param name="body">The value to serialize into the HTTP body content.</param>
        /// <param name="formatter">The media type formatter with which to format the request body format.</param>
        /// <param name="mediaType">The HTTP media type (or <c>null</c> for the <paramref name="formatter"/>'s default).</param>
        /// <returns>Returns the request builder for chaining.</returns>
        public static IRequest WithBody<T>(this IRequest request, T body, MediaTypeFormatter formatter, string mediaType = null)
        {
            return request.WithBodyContent(new ObjectContent<T>(body, formatter, mediaType));
        }

        /// <summary>Set the default request coordinator</summary>
        /// <param name="client">The client.</param>
        /// <param name="shouldRetry">A lambda which returns whether a request should be retried.</param>
        /// <param name="intervals">The intervals between each retry attempt.</param>
        public static IClient SetRequestCoordinator(this IClient client, Func<HttpResponseMessage, bool> shouldRetry, params TimeSpan[] intervals)
        {
            return client.SetRequestCoordinator(new RetryCoordinator(shouldRetry, intervals));
        }

        /// <summary>Set the default request coordinator</summary>
        /// <param name="client">The client.</param>
        /// <param name="maxRetries">The maximum number of times to retry a request before failing.</param>
        /// <param name="shouldRetry">A lambda which returns whether a request should be retried.</param>
        /// <param name="getDelay">A lambda which returns the time to wait until the next retry.</param>
        public static IClient SetRequestCoordinator(this IClient client, int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay)
        {
            return client.SetRequestCoordinator(new RetryCoordinator(maxRetries, shouldRetry, getDelay));
        }

        /// <summary>Set the default request coordinator</summary>
        /// <param name="client">The client.</param>
        /// <param name="config">The retry configuration.</param>
        /// <remarks>If the retry configuration is null, it will cause requests to be executed once without any retry attempts.</remarks>
        public static IClient SetRequestCoordinator(this IClient client, IRetryConfig config)
        {
            return client.SetRequestCoordinator(new RetryCoordinator(config));
        }

        /// <summary>Set the request coordinator for this request</summary>
        /// <param name="request">The request.</param>
        /// <param name="shouldRetry">A lambda which returns whether a request should be retried.</param>
        /// <param name="intervals">The intervals between each retry attempt.</param>
        public static IRequest WithRequestCoordinator(this IRequest request, Func<HttpResponseMessage, bool> shouldRetry, params TimeSpan[] intervals)
        {
            return request.WithRequestCoordinator(new RetryCoordinator(shouldRetry, intervals));
        }

        /// <summary>Set the request coordinator for this request</summary>
        /// <param name="request">The request.</param>
        /// <param name="maxRetries">The maximum number of times to retry a request before failing.</param>
        /// <param name="shouldRetry">A lambda which returns whether a request should be retried.</param>
        /// <param name="getDelay">A lambda which returns the time to wait until the next retry.</param>
        public static IRequest WithRequestCoordinator(this IRequest request, int maxRetries, Func<HttpResponseMessage, bool> shouldRetry, Func<int, HttpResponseMessage, TimeSpan> getDelay)
        {
            return request.WithRequestCoordinator(new RetryCoordinator(maxRetries, shouldRetry, getDelay));
        }

        /// <summary>Set the request coordinator for this request</summary>
        /// <param name="request">The request.</param>
        /// <param name="config">The retry configuration.</param>
        /// <remarks>If the retry configuration is null, it will cause requests to be executed once without any retry attempts.</remarks>
        public static IRequest WithRequestCoordinator(this IRequest request, IRetryConfig config)
        {
            return request.WithRequestCoordinator(new RetryCoordinator(config));
        }

        /// <summary>
        /// Clones the specified request.
        /// </summary>
        /// <param name="req">The request.</param>
        /// <returns>A http request</returns>
        /// <remarks>Please note that you must clone a request BEFORE dispatching it because the content stream is automatically disposed after the request is dispatched which, therefore, prevents cloning the request.</remarks>
        internal static HttpRequestMessage Clone(this HttpRequestMessage req)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, req.RequestUri);

            clone.Content = req.Content;
            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}