using System;
using System.IO;
using System.Net;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Pathoschild.Http.Formatters.Core;

namespace Pathoschild.Http.Formatters.JsonNet
{
	/// <summary>Serializes and deserializes data as BSON.</summary>
	public class JsonNetBsonFormatter : MediaTypeFormatterBase
	{
		/*********
		** Public methods
		*********/
		/// <summary>Construct a new instance.</summary>
		public JsonNetBsonFormatter()
		{
			this.AddMediaType("application/bson");
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
			BsonReader reader = new BsonReader(stream);
			return serializer.Deserialize(reader);
		}

		/// <summary>Serialize an object into the stream.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The stream to which to write.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being written.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		public override void Serialize(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext transportContext)
		{
			JsonSerializer serializer = new JsonSerializer();
			BsonWriter writer = new BsonWriter(stream);
			serializer.Serialize(writer, value);
		}
	}
}
