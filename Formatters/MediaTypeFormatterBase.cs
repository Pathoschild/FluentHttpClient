using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Pathoschild.Http.Formatters
{
    /// <summary>Base implementation of an HTTP <see cref="MediaTypeFormatter"/> for serialization providers.</summary>
    /// <remarks>This class handles the common code for implementing a media type formatter, so most subclasses only need to implement the <see cref="Serialize"/> and <see cref="Deserialize"/> methods.</remarks>
    public abstract class MediaTypeFormatterBase : MediaTypeFormatter
    {
        /*********
        ** Public methods
        *********/
        /***
        ** Generic
        ***/
        /// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can deserialize an object of the specified type.</summary>
        /// <param name="type">The type of object that will be deserialized.</param>
        /// <returns>true if this <see cref="MediaTypeFormatter"/> can deserialize an object of that type; otherwise false.</returns>
        public override bool CanReadType(Type type)
        {
            return true;
        }

        /// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can serialize an object of the specified type. </summary>
        /// <param name="type">The type of object that will be serialized.</param>
        /// <returns>true if this <see cref="MediaTypeFormatter"/> can serialize an object of that type; otherwise false.</returns>
        public override bool CanWriteType(Type type)
        {
            return true;
        }

        /// <summary>Reads an object from the stream asynchronously.</summary>
        /// <param name="type">The type of object to read.</param>
        /// <param name="stream">The stream from which to read.</param>
        /// <param name="content">The HTTP content being read.</param>
        /// <param name="formatterLogger">The trace message logger.</param>
        /// <returns>A task which writes the object to the stream asynchronously.</returns>
        public override Task<object> ReadFromStreamAsync(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger)
        {
            var completionSource = new TaskCompletionSource<object>();
            try
            {
                object result = this.Deserialize(type, stream, content, formatterLogger);
                completionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }

            return completionSource.Task;
        }

        /// <summary>Writes an object to the stream asynchronously.</summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object instance to write.</param>
        /// <param name="stream">The stream to which to write.</param>
        /// <param name="content">The HTTP content being written.</param>
        /// <param name="transportContext">The <see cref="TransportContext"/>.</param>
        /// <returns>A task which writes the object to the stream asynchronously.</returns>
        public override Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            var completionSource = new TaskCompletionSource<object>();
            try
            {
                this.Serialize(type, value, stream, content, transportContext);
                completionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }

            return completionSource.Task;
        }

        /// <summary>Add a media type which can be read or written by this formatter.</summary>
        /// <param name="mediaType">The media type string.</param>
        /// <param name="quality">The relative quality factor.</param>
        public MediaTypeFormatterBase AddMediaType(string mediaType, double? quality = null)
        {
            this.SupportedMediaTypes.Add(quality.HasValue
                ? new MediaTypeWithQualityHeaderValue(mediaType, quality.Value)
                : new MediaTypeHeaderValue(mediaType)
            );
            return this;
        }

        /***
        ** Abstract
        ***/
        /// <summary>Deserialize an object from the stream.</summary>
        /// <param name="type">The type of object to read.</param>
        /// <param name="stream">The stream from which to read.</param>
        /// <param name="content">The HTTP content being read.</param>
        /// <param name="formatterLogger">The trace message logger.</param>
        /// <returns>Returns a deserialized object.</returns>
        public abstract object Deserialize(Type type, Stream stream, HttpContent content, IFormatterLogger formatterLogger);

        /// <summary>Serialize an object into the stream.</summary>
        /// <param name="type">The type of object to write.</param>
        /// <param name="value">The object instance to write.</param>
        /// <param name="stream">The stream to which to write.</param>
        /// <param name="content">The HTTP content being written.</param>
        /// <param name="transportContext">The <see cref="TransportContext"/>.</param>
        public abstract void Serialize(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext);


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        protected MediaTypeFormatterBase()
        {
            this.SupportedEncodings.Add(new UTF8Encoding());
        }
    }
}
