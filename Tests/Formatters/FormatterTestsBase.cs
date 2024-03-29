using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Pathoschild.Http.Client.Formatters;

namespace Pathoschild.Http.Tests.Formatters
{
    /// <summary>Provides generic helper methods for <see cref="MediaTypeFormatterBase" /> unit tests.</summary>
    public abstract class FormatterTestsBase
    {
        /*********
        ** Fields
        *********/
        /// <summary>A null transport context.</summary>
        /// <remarks><see cref="MediaTypeFormatterBase.Serialize"/> has the transport context marked as non-nullable, but we know the default implementations tested here allow null values. Since we can't easily construct a transport context, this field returns a null transport context marked as non-nullable.</remarks>
        private readonly TransportContext NullTransportContext = null!;

        /// <summary>A null formatter logger.</summary>
        /// <remarks>See remarks on <see cref="NullTransportContext"/>.</remarks>
        private readonly IFormatterLogger FormatterLogger = null!;


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an HTTP request message.</summary>
        /// <typeparam name="T">The request body type.</typeparam>
        /// <param name="content">The request body content.</param>
        /// <param name="formatter">The formatter with which the content can be serialized.</param>
        /// <param name="contentType">The HTTP Accept and Content-Type header values.</param>
        protected HttpRequestMessage GetRequest<T>(T content, MediaTypeFormatter formatter, string? contentType = null)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return this.GetRequest(content, formatter, typeof(T), contentType);
        }

        /// <summary>Construct an HTTP request message.</summary>
        /// <param name="content">The request body content.</param>
        /// <param name="formatter">The formatter with which the content can be serialized.</param>
        /// <param name="type">The object type of the <paramref name="content"/>.</param>
        /// <param name="contentType">The HTTP Accept and Content-Type header values.</param>
        protected HttpRequestMessage GetRequest(object content, MediaTypeFormatter formatter, Type type, string? contentType = null)
        {
            HttpRequestMessage message = new(HttpMethod.Get, "http://example.org")
            {
                Content = new ObjectContent(type, content, formatter)
            };
            if (contentType != null)
            {
                message.Headers.Accept.Clear();
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                message.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
            return message;
        }

        /// <summary>Get the serialized representation of a request body.</summary>
        /// <typeparam name="T">The request body type.</typeparam>
        /// <param name="content">The request body content.</param>
        /// <param name="request">The HTTP request to handle.</param>
        /// <param name="formatter">The media type formatter which will serialize the request body.</param>
        protected string GetSerialized<T>(T content, HttpRequestMessage request, MediaTypeFormatterBase formatter)
        {
            if (request.Content is null)
                throw new InvalidOperationException("Can't get the serialized representation for a null message body.");

            using MemoryStream stream = new();
            using StreamReader reader = new(stream);
            formatter.Serialize(typeof(string), content, stream, request.Content, this.NullTransportContext);
            stream.Position = 0;
            return reader.ReadToEnd();
        }

        /// <summary>Get the deserialized representation of a request body.</summary>
        /// <param name="type">The request body type.</param>
        /// <param name="content">The request body content.</param>
        /// <param name="request">The HTTP request to handle.</param>
        /// <param name="formatter">The media type formatter which will serialize the request body.</param>
        protected object GetDeserialized(Type type, string? content, HttpRequestMessage request, MediaTypeFormatterBase formatter)
        {
            if (request.Content is null)
                throw new InvalidOperationException("Can't get the deserialized representation for a null message body.");

            using MemoryStream stream = new();
            using StreamWriter writer = new(stream);

            // write content
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            // deserialize
            return formatter.Deserialize(type, stream, request.Content, this.FormatterLogger);
        }
    }
}
