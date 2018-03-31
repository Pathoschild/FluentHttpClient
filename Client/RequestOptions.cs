namespace Pathoschild.Http.Client
{
    /// <summary>Options for a request.</summary>
    public class RequestOptions
    {
        /// <summary>Indicates whether arguments with null value be ignored when the request is dispatched.</summary>
        public bool? IgnoreNullArguments { get; set; }

        /// <summary>Indicates whether HTTP error responses (e.g. HTTP 404) should be ignored or should be raised as exceptions</summary>
        public bool? IgnoreHttpErrors { get; set; }
    }
}
