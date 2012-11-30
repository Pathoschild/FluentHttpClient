using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Pathoschild.Http.Formatters.JsonNet
{
	/// <summary>Serializes and deserializes data as JSON or JSONP.</summary>
	/// <remarks>This is derived from Christian Weyers' implementation: <see href="https://github.com/thinktecture/Thinktecture.Web.Http/blob/master/Thinktecture.Web.Http/Formatters/JsonpFormatter.cs"/>.</remarks>
	public class JsonNetFormatter : MediaTypeFormatterBase
	{
		/*********
		** Properties
		*********/
		/// <summary>The current HTTP request message.</summary>
		protected HttpRequestMessage Request { get; set; }

		/// <summary>The HTTP content types that represent JSON.</summary>
		protected static readonly string[] ContentTypes = new[] { "application/json", "text/json" };

		/// <summary>The HTTP content types that represent JSONP.</summary>
		protected static readonly string[] PaddedContentTypes = new[] { "application/javascript", "application/ecmascript", "text/javascript", "text/ecmascript" };


		/*********
		** Accessors
		*********/
		/// <summary>The JSON serialization settings.</summary>
		public JsonSerializerSettings SerializerSettings { get; set; }

		/// <summary>The name of the parameter which defines the callback method name.</summary>
		public string CallbackParameterName { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct a new instance.</summary>
		public JsonNetFormatter()
		{
			foreach (string contentType in ContentTypes.Union(PaddedContentTypes))
				this.AddMediaType(contentType);
			this.SerializerSettings = new JsonSerializerSettings();
			this.CallbackParameterName = "callback";
		}

		/// <summary>Get a formatter to handle an individual request.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="request">The HTTP request message.</param>
		/// <param name="mediaType">The requested content format.</param>
		public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
		{
			// construct a new formatter
			JsonNetFormatter formatter = new JsonNetFormatter(request)
			{
				CallbackParameterName = this.CallbackParameterName,
				RequiredMemberSelector = this.RequiredMemberSelector,
				SerializerSettings = this.SerializerSettings
			};

			// copy configuration
			this
				.OverwriteList(this.MediaTypeMappings, formatter.MediaTypeMappings)
				.OverwriteList(this.SupportedMediaTypes, formatter.SupportedMediaTypes)
				.OverwriteList(this.SupportedEncodings, formatter.SupportedEncodings);
			return formatter;
		}

		/// <summary>Deserialize an object from the stream.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="stream">The stream from which to read.</param>
		/// <param name="content">The HTTP content being read.</param>
		/// <param name="formatterLogger">The trace message logger.</param>
		/// <returns>Returns a deserialized object.</returns>
		public override object Deserialize(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger)
		{
			JsonTextReader reader = new JsonTextReader(new StreamReader(stream)); // don't dispose (stream disposal is handled elsewhere)
			try
			{
				return this.GetSerializer().Deserialize(reader, type);
			}
			catch (JsonReaderException exception)
			{
				if (this.IsJsonp(content.Headers.ContentType))
					throw new NotSupportedException("The JSONP response could not be deserialized. (Possible cause: deserializing JSONP with a JavaScript callback is not supported.)", exception);
				throw;
			}
		}

		/// <summary>Serialize an object into the stream.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The stream to which to write.</param>
		/// <param name="content">The HTTP content being written.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		public override void Serialize(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
		{
			JsonTextWriter writer = new JsonTextWriter(new StreamWriter(stream)); // don't dispose (stream disposal is handled elsewhere)

			bool hasCallback = this.IsJsonp(content.Headers.ContentType);
			if (hasCallback)
			{
				string callbackName = this.GetCallback(this.Request);
				writer.WriteRaw(callbackName + "(");
			}
			this.GetSerializer().Serialize(writer, value);
			if (hasCallback)
				writer.WriteRaw(")");
			writer.Flush();
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="request">The HTTP request message.</param>
		protected JsonNetFormatter(HttpRequestMessage request)
			: this()
		{
			this.Request = request;
		}

		/// <summary>Get whether the content type represents JSONP.</summary>
		/// <param name="header">The content type header.</param>
		protected bool IsJsonp(MediaTypeHeaderValue header)
		{
			return PaddedContentTypes.Contains(header.MediaType);
		}

		/// <summary>Get a JSON.NET serializer.</summary>
		protected JsonSerializer GetSerializer()
		{
			return JsonSerializer.Create(this.SerializerSettings);
		}

		/// <summary>Get the name of the JavaScript method to invoke.</summary>
		/// <param name="request">The HTTP request message.</param>
		protected string GetCallback(HttpRequestMessage request)
		{
			const string defaultCallback = "callback";
			if (request == null)
				return defaultCallback;
			var value = request.RequestUri.ParseQueryString()[this.CallbackParameterName];
			return value ?? defaultCallback;
		}

		/// <summary>Replace all elements of the <paramref name="destination"/> list with elements of the <paramref name="source"/>.</summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="source">The collection from which to take elements.</param>
		/// <param name="destination">The collection whose elements to overwrite.</param>
		protected JsonNetFormatter OverwriteList<T>(Collection<T> source, Collection<T> destination)
		{
			destination.Clear();
			foreach (T item in source)
				destination.Add(item);
			return this;
		}
	}
}
