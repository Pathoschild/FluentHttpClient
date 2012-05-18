**Pathoschild.FluentHttpClient** is a fluent wrapper over the [.NET 4.5 HttpClient](http://code.msdn.microsoft.com/Introduction-to-HttpClient-4a2d9cee) for creating strongly-typed easy-to-use API clients.

## Usage
The client is a fluent wrapper over `HttpClient`. You start by creating a client, and chain methods to configure your request and response.

     IClient client = new Client("http://example.org/api/");

The most common usage is a synchronous HTTP GET, with the response deserialized into a class instance:

     Idea idea = client
        .Get("ideas/14")
        .Retrieve<Idea>();

You can fluently customize the request, and even directly alter the [`HttpRequestMessage`](http://msdn.microsoft.com/en-us/library/system.net.http.httprequestmessage.aspx):
  
     Idea idea = client
        .Post("ideas", new Idea())
        .WithHeader("Content-Type", "application/json")
        .WithArgument("tenant", "company-name")
        .WithCustom(message =>
        {
           message.Method = HttpMethod.Get;
           message.RequestUri = new Uri("http://example.org/api2/", "ideas");
        })
        .Retrieve<Idea>();

The response can also be fluently configured:

     string jsonIdea = client
        .Get("ideas/14")
        .Retrieve()
        .AsString();

You also can do everything asynchronously:

     Task<Idea> query = client
        .Get("ideas/14")
        .Retrieve()
        .AsAsync<Idea>();

## Extending the client
### Media type formatters
The client uses `MediaTypeFormatter`s for serializing and deserializing models for HTTP messages. This is the same type used by the underlying `HttpClient` and the .NET Web API, which means there are many implementations already available. You can easily create your own implementation by subclassing `SerializerMediaTypeFormatterBase`.

For example, to replace the default `DataContractJsonSerializer` with JSON.NET:

     IClient client = new Client("http://example.org/api/");
     client.Formatters.Remove(client.Formatters.JsonFormatter);
     client.Formatters.Add(new JsonNetMediaTypeFormatter());