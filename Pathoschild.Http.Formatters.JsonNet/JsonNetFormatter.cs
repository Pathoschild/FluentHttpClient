using System;
using System.IO;
using System.Net;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Pathoschild.Http.Formatters.Core;

namespace Pathoschild.Http.Formatters.JsonNet
{
	/// <summary>Serializes and deserializes data as JSON.</summary>
	public class JsonNetFormatter : SerializerMediaTypeFormatterBase
	{
		/*********
		** Accessors
		*********/
		/// <summary>Whether to format the serialized JSON for human-readability.</summary>
		public bool Format { get; set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct a new instance.</summary>
		public JsonNetFormatter()
		{
			this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
			this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/json"));
		}

		/// <summary>Deserialize an object from the stream.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="stream">The stream from which to read.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being read.</param>
		/// <param name="formatterLogger">The trace message logger.</param>
		/// <returns>Returns a deserialized object.</returns>
		public override object Deserialize(Type type, Stream stream, HttpContentHeaders contentHeaders, IFormatterLogger formatterLogger)
		{
			JsonSerializer serializer = new JsonSerializer();
			StreamReader streamReader = new StreamReader(stream); // don't dispose (stream disposal is handled elsewhere)
			JsonTextReader reader = new JsonTextReader(streamReader);
			return serializer.Deserialize(reader, type);
		}

		/// <summary>Serialize an object into the stream.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The stream to which to write.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being written.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		public override void Serialize(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext transportContext)
		{
			JsonSerializer serializer = new JsonSerializer { Formatting = this.Format ? Formatting.Indented : Formatting.None };
			StreamWriter streamWriter = new StreamWriter(stream); // don't dispose (stream disposal is handled elsewhere)
			JsonTextWriter writer = new JsonTextWriter(streamWriter);
			serializer.Serialize(writer, value);
			writer.Flush();
		}
	}
}
