namespace Pathoschild.Http.Client.Extensibility
{
    /// <summary>A middleware class which can intercept and modify HTTP requests and responses. This can be used to implement common authentication, error-handling, etc.</summary>
    public interface IHttpFilter
    {
        /// <summary>Method invoked just before the HTTP request is submitted. This method can modify the outgoing HTTP request.</summary>
        /// <param name="request">The HTTP request.</param>
        void OnRequest(IRequest request);

        /// <summary>Method invoked just after the HTTP response is received. This method can modify the incoming HTTP response.</summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="httpErrorAsException">Whether HTTP error responses (e.g. HTTP 404) should be raised as exceptions.</param>
        void OnResponse(IResponse response, bool httpErrorAsException);
    }
}
