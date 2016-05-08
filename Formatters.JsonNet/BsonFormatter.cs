using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Pathoschild.Http.Formatters.JsonNet
{
    /// <summary>Serializes and deserializes data as BSON.</summary>
    public class BsonFormatter : MediaTypeFormatterBase
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct a new instance.</summary>
        public BsonFormatter()
        {
            this.AddMediaType("application/bson");
        }

        /// <summary>Deserialize an object from the stream.</summary>
        /// <param name="type">The type of object to read.</param>
        /// <param name="stream">The stream from which to read.</param>
        /// <param name="content">The HTTP content being read.</param>
        /// <param name="formatterLogger">The trace message logger.</param>
        /// <returns>Returns a deserialized object.</returns>
        public override object Deserialize(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger)
        {
            JsonSerializer serializer = new JsonSerializer();
            BsonReader reader = new BsonReader(stream);
            return serializer.Deserialize(reader, type);
        }

        /// <summary>Serialize an object into the stream.</summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object instance to write.</param>
        /// <param name="stream">The stream to which to write.</param>
        /// <param name="content">The HTTP content being written.</param>
        /// <param name="transportContext">The <see cref="TransportContext"/>.</param>
        public override void Serialize(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            JsonSerializer serializer = new JsonSerializer();
            BsonWriter writer = new BsonWriter(stream);
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>Serializes and deserializes data as BSON.</summary>
    [Obsolete("This class has been renamed to `BsonFormatter`.")]
    public class JsonNetBsonFormatter : BsonFormatter { }
}
