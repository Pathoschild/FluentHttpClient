using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;

namespace Pathoschild.Http.Client.Formatters
{
    /// <summary>Serializes and deserializes data as plaintext.</summary>
    /// <remarks>This formatter is derived from <a href="http://github.com/WebApiContrib">WebApiContrib</a>, which was not compatible with the release candidate at the time of creation.</remarks>
    public class PlainTextFormatter : MediaTypeFormatterBase
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether to allow formatting of types that cannot be deserialized using a <see cref="PlainTextFormatter"/>.</summary>
        public bool AllowIrreversibleSerialization { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public PlainTextFormatter()
        {
            this.AddMediaType("text/plain");
        }

        /// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can deserialize an object of the specified type.</summary>
        /// <param name="type">The type of object that will be deserialized.</param>
        /// <returns>true if this <see cref="MediaTypeFormatter"/> can deserialize an object of that type; otherwise false.</returns>
        public override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }

        /// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can serialize an object of the specified type. </summary>
        /// <param name="type">The type of object that will be serialized.</param>
        /// <returns>true if this <see cref="MediaTypeFormatter"/> can serialize an object of that type; otherwise false.</returns>
        public override bool CanWriteType(Type type)
        {
            return type == typeof(string) || (this.AllowIrreversibleSerialization && typeof(IFormattable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
        }

        /// <summary>Deserialize an object from the stream.</summary>
        /// <param name="type">The type of object to read.</param>
        /// <param name="stream">The stream from which to read.</param>
        /// <param name="content">The HTTP content being read.</param>
        /// <param name="formatterLogger">The trace message logger.</param>
        /// <returns>Returns a deserialized object.</returns>
        public override object Deserialize(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var reader = new StreamReader(stream); // don't dispose (stream disposal is handled elsewhere)
            return reader.ReadToEnd();
        }

        /// <summary>Serialize an object into the stream.</summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object instance to write.</param>
        /// <param name="stream">The stream to which to write.</param>
        /// <param name="content">The HTTP content being written.</param>
        /// <param name="transportContext">The <see cref="TransportContext"/>.</param>
        public override void Serialize(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            var writer = new StreamWriter(stream); // don't dispose (stream disposal is handled elsewhere)
            writer.Write(value != null ? value.ToString() : String.Empty);
            writer.Flush();
        }
    }
}
