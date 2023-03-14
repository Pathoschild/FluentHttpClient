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

        /// <inheritdoc />
        public override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }

        /// <inheritdoc />
        public override bool CanWriteType(Type type)
        {
            return type == typeof(string) || (this.AllowIrreversibleSerialization && typeof(IFormattable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
        }

        /// <inheritdoc />
        public override object Deserialize(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger)
        {
            StreamReader reader = new(stream); // don't dispose (stream disposal is handled elsewhere)
            return reader.ReadToEnd();
        }

        /// <inheritdoc />
        public override void Serialize(Type type, object? value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            StreamWriter writer = new(stream); // don't dispose (stream disposal is handled elsewhere)
            writer.Write(value != null ? value.ToString() : string.Empty);
            writer.Flush();
        }
    }
}
