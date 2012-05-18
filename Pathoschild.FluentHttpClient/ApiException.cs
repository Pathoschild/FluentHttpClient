using System;
using System.Net;

namespace Pathoschild.FluentHttpClient
{
	/// <summary>Represents an error returned by the upstream server.</summary>
	public class ApiException : Exception
	{
		/*********
		** Accessors
		*********/
		/// <summary>The HTTP status of the response.</summary>
		public HttpStatusCode Status { get; protected set; }


		/*********
		** Public methods
		*********/
		/// <summary>Construct an instance.</summary>
		/// <param name="status">The HTTP status of the response.</param>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception (or <c>null</c> for no inner exception).</param>
		public ApiException(HttpStatusCode status, string message, Exception innerException = null)
			: base(message, innerException)
		{
			this.Status = status;
		}
	}
}