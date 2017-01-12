using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Pathoschild.Http.Client.Extensibility;
using Pathoschild.Http.Client.Internal;
using System.Threading;

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
            TFilter filter = filters.OfType<TFilter>().FirstOrDefault();
            return filter != null && filters.Remove(filter);

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
    }
}