using System;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace Pathoschild.Http.Client
{
    /// <summary>Sends HTTP requests and receives responses from REST URIs.</summary>
    public interface IClient : IDisposable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying HTTP client.</summary>
        HttpClient BaseClient { get; }

        /// <summary>The formatters used for serializing and deserializing message bodies.</summary>
        MediaTypeFormatterCollection Formatters { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Create an asynchronous HTTP DELETE request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest DeleteAsync(string resource);

        /// <summary>Create an asynchronous HTTP GET request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest GetAsync(string resource);

        /// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest PostAsync(string resource);

        /// <summary>Create an asynchronous HTTP POST request message (but don't dispatch it yet).</summary>
        /// <typeparam name="TBody">The request body type.</typeparam>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest PostAsync<TBody>(string resource, TBody body);

        /// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest PutAsync(string resource);

        /// <summary>Create an asynchronous HTTP PUT request message (but don't dispatch it yet).</summary>
        /// <typeparam name="TBody">The request body type.</typeparam>
        /// <param name="resource">The URI to send the request to.</param>
        /// <param name="body">The request body.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest PutAsync<TBody>(string resource, TBody body);

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="resource">The URI to send the request to.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest SendAsync(HttpMethod method, string resource);

        /// <summary>Create an asynchronous HTTP request message (but don't dispatch it yet).</summary>
        /// <param name="message">The HTTP request message to send.</param>
        /// <returns>Returns a request builder.</returns>
        IRequest SendAsync(HttpRequestMessage message);

        /// <summary>Set the useragent string that will be used with every request.</summary>
        /// <param name="useragent">The useragent string.</param>
        void SetUserAgent(string useragent);
    }
}
