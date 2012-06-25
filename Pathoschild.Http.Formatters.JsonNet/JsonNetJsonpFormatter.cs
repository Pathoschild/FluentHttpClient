using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Pathoschild.Http.Formatters.JsonNet
{
	/// <summary>Serializes and deserializes data as JSONP.</summary>
	/// <remarks>This formatter provides JavaScript callback; see <see href="https://en.wikipedia.org/wiki/JSONP">JSONP</see> and <see href="https://www.rfc-editor.org/rfc/rfc4329.txt">RFC4329 on scripting media types</see>. This is partly derived from <see href="https://github.com/WebApiContrib/WebAPIContrib/blob/master/src/WebApiContrib/Formatting/JsonpFormatter.cs"/>.</remarks>
	public class JsonNetJsonpFormatter : JsonNetFormatter
	{
		/*********
		** Protected
		*********/
		/// <summary>The current HTTP request message.</summary>
		protected HttpRequestMessage Request { get; set; }


		/*********
		** Accessors
		*********/
		/// <summary>The name of the parameter which defines the callback method name.</summary>
		public string CallbackParameter { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		public JsonNetJsonpFormatter()
		{
			this.SupportedMediaTypes.Clear();
			this
				.AddMediaType("application/javascript")
				.AddMediaType("application/ecmascript")
				.AddMediaType("text/javascript")
				.AddMediaType("text/ecmascript");
			this.CallbackParameter = "callback";
		}

		/// <summary>Construct an instance.</summary>
		/// <param name="request">The HTTP request message.</param>
		public JsonNetJsonpFormatter(HttpRequestMessage request)
			: this()
		{
			this.Request = request;
		}

		/// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can deserialize an object of the specified type.</summary>
		/// <param name="type">The type of object that will be deserialized.</param>
		/// <returns>true if this <see cref="MediaTypeFormatter"/> can deserialize an object of that type; otherwise false.</returns>
		/// <remarks>This implementation always returns <c>false</c>, because this format is output-only.</remarks>
		public override bool CanReadType(Type type)
		{
			return false;
		}

		/// <summary>Get a formatter to handle an individual request.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="request">The HTTP request message.</param>
		/// <param name="mediaType">The requested content format.</param>
		public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
		{
			// construct a new formatter
			JsonNetJsonpFormatter formatter = new JsonNetJsonpFormatter(request)
			{
				CallbackParameter = this.CallbackParameter,
				Format = this.Format
			};

			// copy configuration
			formatter.RequiredMemberSelector = this.RequiredMemberSelector;
			formatter.SerializerSettings = this.SerializerSettings;
			this
				.OverwriteList(this.MediaTypeMappings, formatter.MediaTypeMappings)
				.OverwriteList(this.SupportedMediaTypes, formatter.SupportedMediaTypes)
				.OverwriteList(this.SupportedEncodings, formatter.SupportedEncodings);
			return formatter;
		}

		/// <summary>Deserialize an object from the stream.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="stream">The stream from which to read.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being read.</param>
		/// <param name="formatterLogger">The trace message logger.</param>
		/// <returns>Returns a deserialized object.</returns>
		public override object Deserialize(Type type, Stream stream, HttpContentHeaders contentHeaders, IFormatterLogger formatterLogger)
		{
			throw new NotSupportedException("The JSONP formatter is output-only.");
		}

		/// <summary>Serialize an object into the stream.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The stream to which to write.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being written.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		public override void Serialize(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext transportContext)
		{
			string callback = this.GetCallback(this.Request);

			StreamWriter writer = new StreamWriter(stream); // don't dispose (stream disposal is handled elsewhere)
			writer.Write(callback + "(");
			writer.Flush();
			base.Serialize(type, value, stream, contentHeaders, transportContext);
			writer.Write(")");
			writer.Flush();
		}


		/*********
		** Protected methods
		*********/
		/// <summary>Get the name of the JavaScript method to invoke.</summary>
		/// <param name="request">The HTTP request message.</param>
		protected string GetCallback(HttpRequestMessage request)
		{
			const string defaultCallback = "callback";
			if (request == null)
				return defaultCallback;
			var value = request.RequestUri.ParseQueryString()[this.CallbackParameter];
			return value ?? defaultCallback;
		}

		/// <summary>Replace all elements of the <paramref name="destination"/> list with elements of the <paramref name="source"/>.</summary>
		/// <typeparam name="T">The element type.</typeparam>
		/// <param name="source">The collection from which to take elements.</param>
		/// <param name="destination">The collection whose elements to overwrite.</param>
		protected JsonNetJsonpFormatter OverwriteList<T>(Collection<T> source, Collection<T> destination)
		{
			destination.Clear();
			foreach (T item in source)
				destination.Add(item);
			return this;
		}
	}
}
