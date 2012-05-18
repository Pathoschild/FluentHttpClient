using System;
using System.IO;
using System.Net;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Pathoschild.FluentHttpClient.Framework
{
	/// <summary>Base implementation of an HTTP <see cref="MediaTypeFormatter"/> for serialization providers.</summary>
	/// <remarks>This class handles the common code for implementing a media type formatter, so most subclasses only need to implement the <see cref="Serialize"/> and <see cref="Deserialize"/> methods.</remarks>
	public abstract class SerializerMediaTypeFormatterBase : MediaTypeFormatter
	{
		/*********
		** Protected methods
		*********/
		/***
		** Generic
		***/
		/// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can deserialize an object of the specified type.</summary>
		/// <param name="type">The type of object that will be deserialized.</param>
		/// <returns>Returns <c>true</c> if this <see cref="MediaTypeFormatter"/> can deserialize an object of that type; otherwise false.</returns>
		protected override bool CanReadType(Type type)
		{
			return type != typeof(IKeyValueModel);
		}

		/// <summary>Determines whether this <see cref="MediaTypeFormatter"/> can serialize an object of the specified type.</summary>
		/// <param name="type">The type of object that will be serialized.</param>
		/// <returns>true if this <see cref="MediaTypeFormatter"/> can serialize an object of that type; otherwise false.</returns>
		protected override bool CanWriteType(Type type)
		{
			return type != typeof(IKeyValueModel);
		}

		/// <summary>Called to read an object from the stream asynchronously.</summary>
		/// <param name="type">The type of object to read.</param>
		/// <param name="stream">The <see cref="Stream"/> from which to read.</param>
		/// <param name="contentHeaders">The <see cref="HttpContentHeaders"/> for the content being read.</param>
		/// <param name="formatterContext">The <see cref="FormatterContext"/> containing the respective request or response.</param>
		/// <returns>A <see cref="Task"/> that will write the object to the stream asynchronously.</returns>
		protected override Task<object> OnReadFromStreamAsync(Type type, Stream stream, HttpContentHeaders contentHeaders, FormatterContext formatterContext)
		{
			var completionSource = new TaskCompletionSource<object>();
			try
			{
				object result = this.Deserialize(type, stream, contentHeaders, formatterContext);
				completionSource.SetResult(result);
			}
			catch (Exception ex)
			{
				completionSource.SetException(ex);
			}

			return completionSource.Task;
		}

		/// <summary>Called to write an object to the stream asynchronously.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The <see cref="Stream"/> to which to write.</param>
		/// <param name="contentHeaders">The <see cref="HttpContentHeaders"/> for the content being written.</param>
		/// <param name="formatterContext">The <see cref="FormatterContext"/> containing the respective request or response.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		/// <returns>A <see cref="Task"/> that will read the object from the stream asynchronously.</returns>
		protected override Task OnWriteToStreamAsync(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, FormatterContext formatterContext, TransportContext transportContext)
		{
			var completionSource = new TaskCompletionSource<object>();
			try
			{
				this.Serialize(type, value, stream, contentHeaders, formatterContext, transportContext);
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
		/// <param name="stream">The <see cref="Stream"/> from which to read.</param>
		/// <param name="contentHeaders">The <see cref="HttpContentHeaders"/> for the content being read.</param>
		/// <param name="formatterContext">The <see cref="FormatterContext"/> containing the respective request or response.</param>
		/// <returns>Returns a deserialized object.</returns>
		protected abstract object Deserialize(Type type, Stream stream, HttpContentHeaders contentHeaders, FormatterContext formatterContext);

		/// <summary>Serialize an object into the stream.</summary>
		/// <param name="type">The type of object to write.</param>
		/// <param name="value">The object instance to write.</param>
		/// <param name="stream">The <see cref="Stream"/> to which to write.</param>
		/// <param name="contentHeaders">The <see cref="HttpContentHeaders"/> for the content being written.</param>
		/// <param name="formatterContext">The <see cref="FormatterContext"/> containing the respective request or response.</param>
		/// <param name="transportContext">The <see cref="TransportContext"/>.</param>
		protected abstract void Serialize(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, FormatterContext formatterContext, TransportContext transportContext);
	}
}
