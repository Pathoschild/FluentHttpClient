using System;
using System.IO;
using System.Net;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.Http.Formatters.Core
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
		/// <param name="contentHeaders">The HTTP content headers for the content being read.</param>
		/// <param name="formatterLogger">The trace message logger.</param>
		/// <returns>A task which writes the object to the stream asynchronously.</returns>
		public override Task<object> ReadFromStreamAsync(Type type, Stream stream, HttpContentHeaders contentHeaders, IFormatterLogger formatterLogger)
		{
			var completionSource = new TaskCompletionSource<object>();
			try
			{
				object result = this.Deserialize(type, stream, contentHeaders, formatterLogger);
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
		/// <param name="contentHeaders">The HTTP content headers for the content being written.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		/// <returns>A task which writes the object to the stream asynchronously.</returns>
		public override Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext transportContext)
		{
			var completionSource = new TaskCompletionSource<object>();
			try
			{
				this.Serialize(type, value, stream, contentHeaders, transportContext);
				completionSource.SetResult(null);
			}
			catch (Exception ex)
			{
				completionSource.SetException(ex);
			}

			return completionSource.Task;
		}


		/***
		** Abstract
		***/
		/// <summary>Deserialize an object from the stream.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="stream">The stream from which to read.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being read.</param>
		/// <param name="formatterLogger">The trace message logger.</param>
		/// <returns>Returns a deserialized object.</returns>
		public abstract object Deserialize(Type type, Stream stream, HttpContentHeaders contentHeaders, IFormatterLogger formatterLogger);

		/// <summary>Serialize an object into the stream.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The stream to which to write.</param>
		/// <param name="contentHeaders">The HTTP content headers for the content being written.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		public abstract void Serialize(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, TransportContext transportContext);
	}
}
