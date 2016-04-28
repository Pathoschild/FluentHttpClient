using System;
using System.Net;
using System.Net.Http;

namespace Pathoschild.Http.Client
{
    /// <summary>Represents an error returned by the upstream server.</summary>
    public class ApiException : Exception
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The HTTP status of the response.</summary>
        public HttpStatusCode Status { get; protected set; }

        /// <summary>The HTTP response which caused the exception.</summary>
        public IResponse Response { get; protected set; }

        /// <summary>The HTTP response message which caused the exception.</summary>
        public HttpResponseMessage ResponseMessage { get; protected set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="response">The HTTP response which caused the exception.</param>
        /// <param name="responseMessage">The HTTP response message which caused the exception.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception (or <c>null</c> for no inner exception).</param>
        public ApiException(IResponse response, HttpResponseMessage responseMessage, string message, Exception innerException = null)
            : base(message, innerException)
        {
            this.Response = response;
            this.ResponseMessage = responseMessage;
            this.Status = responseMessage.StatusCode;
        }
    }
}