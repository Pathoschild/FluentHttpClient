namespace Pathoschild.Http.Client
{
    /// <summary>Options for a request.</summary>
    public class RequestOptions
    {
        /// <summary>Whether to ignore arguments with null value when the request is dispatched.</summary>
        public bool? IgnoreNullArguments { get; set; }

        /// <summary>Whether HTTP error responses (e.g. HTTP 404) should be ignored (else raised as exceptions).</summary>
        public bool? IgnoreHttpErrors { get; set; }
    }
}
