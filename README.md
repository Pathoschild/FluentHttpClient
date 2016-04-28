**Pathoschild.FluentHttpClient** is an easy strongly-typed asynchronous REST API client, built on top of the .NET 4.5 [HttpClient][]. The client provides a single fluent interface that lets you create an HTTP request, dispatch and wait for it, and process the response. The client will automatically inject any required HTTP configuration (like `User-Agent` and `Accept` headers) and handle the plumbing code.

The fluent client is available as the [Pathoschild.Http.FluentClient][] NuGet package.

## Using the client
### Basic usage
You start by creating a client:

```c#
IClient client = new FluentClient("https://example.org/api/");
```

Next you chain methods to configure your request and response handling. For example, here's a simple GET request whose response will be deserialised into an `Item` object:
```c#
Item item = await client
    .GetAsync("items/14")
    .As<Item>();
```

You can fetch the response as a model, list of models, byte array, string, or stream:
```c#
string json = await client
    .GetAsync("items/14")
    .AsString();
```

If you don't need the response, you can just wait for the request to complete.
```c#
await client.PostAsync("items", new Item());
```

You can configure some pretty complex requests using the fluent interface (the client will take care of the details like input sanitisation and URL encoding):
```c#
Item item = await client
    .GetAsync("items")
    .WithHeader("Content-Type", "application/json")
    .WithArguments(new { id = 14, tenant = "tenant-name" }) // equivalent to .WithArgument("id", 14).WithArgument("tenant", "tenant-name")
    .As<Item>();
```

A lot of features aren't shown in these examples, but it should be fairly discoverable since every method is fully code-documented for IntelliSense.

### Error handling
If the server returns a non-success HTTP code, the client will raise an `ApiException` by default (see _customising behaviour_ below if you want to change that). The `ApiException` includes all the information needed to troubleshoot the error, including the underlying HTTP request and response.

For example, here's how you'd throw a new exception containing the actual text of the server response:
```c#
try
{
    return await client
        .Get("items")
        .AsList<Item>();
}
catch(ApiException ex)
{
    HttpStatusCode statusCode = ex.ResponseMessage.StatusCode;
    string responseText = await ex.ResponseMessage.Content.ReadAsStringAsync();
    throw new YourApiException($"The API responded with HTTP {statusCode}: {responseText}");
}
```

### Synchronous use
The client is designed to take advantage of the `async` and `await` keywords in .NET 4.5, but you can use the client synchronously. This is *not* recommended — it complicates error-handling (e.g. errors get wrapped into [AggregateException][]), and it's very easy to cause thread deadlocks when you do this (see _[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!](http://blogs.msdn.com/b/pfxteam/archive/2011/01/13/10115163.aspx)_ and _[Don't Block on Async Code](http://nitoprograms.blogspot.ca/2012/07/dont-block-on-async-code.html))._

If you really need to use it synchronously, you can just call the `Result` property:
```c#
Item item = client
    .GetAsync("items/14")
    .As<Item>()
    .Result;
```

Or if you don't need the response:

```c#
client.PostAsync("items", new Item()).Wait();
```

## Customising the client
### Custom behaviour
You can customise the client by injecting implementations of `IHttpFilter`, which intercept outgoing requests and incoming responses. Each filter can directly modify the underlying HTTP requests (e.g. for authentication) and responses (e.g. for error handling or to normalise API responses). For example, you can easily replace the default error handling (see _Error handling_ above):
```c#
client.Filters.Remove<DefaultErrorFilter>();
client.Filters.Add(YourErrorFilter());
```

For reference, the default error filter is essentially this one method:
```c#
/// <summary>Method invoked just after the HTTP response is received. This method can modify the incoming HTTP response.</summary>
/// <param name="response">The HTTP response.</param>
/// <param name="responseMessage">The underlying HTTP response message.</param>
public void OnResponse(IResponse response, HttpResponseMessage responseMessage)
{
    if (responseMessage.IsSuccessStatusCode)
        return;

    throw new ApiException(response, responseMessage, String.Format("The API query failed with status code {0}: {1}", responseMessage.StatusCode, responseMessage.ReasonPhrase));
}
```

That's a pretty simple filter, but you can do some much more advanced things by changing the request and response messages. For example, you can even rewrite HTTP responses from the server before they're parsed.

### Custom formats
By default the client uses `HttpClient`'s default formatters, which includes basic JSON and XML support.

The optional [Pathoschild.Http.Formatters.JsonNet][] NuGet package adds support for three formats using the popular [Json.NET][] library: [BSON][] (`application/bson`), [JSON][] (`application/json`, `text/json`), and [JSONP][] (`application/javascript`, `application/ecmascript`, `text/javascript`, `text/ecmascript`). After installing the package, just register it with the client:
```c#
IClient client = new FluentClient("http://example.org/api/");
client.Formatters.Remove(client.Formatters.JsonFormatter); // or client.Formatters.Clear();
client.Formatters.Add(new JsonNetFormatter());
```

You can also use any other [MediaTypeFormatter][], or create your own (optionally using the [Pathoschild.Http.Formatters.Core][] package to simplify your implementation).

### Custom message handler
You can access the underlying [HTTP message handler][HttpClientHandler] to configure low-level behaviour:
```c#
     client.MessageHandler.Credentials = new NetworkCredential("username", "password");
     client.MessageHandler.CookieContainer.Add(new Cookie(...));
     client.MessageHandler.Proxy = new WebProxy(...);
```

For really advanced scenarios you can inject your own low-level handler and client:
```c#
     var handler = new CustomClientHandler();
     var client = new FluentClient(new HttpClient(handler), handler, "https://example.org/api/");
```

[AggregateException]: http://msdn.microsoft.com/en-us/library/system.aggregateexception.aspx
[HttpClient]: http://code.msdn.microsoft.com/Introduction-to-HttpClient-4a2d9cee
[HttpClientHandler]: http://msdn.microsoft.com/en-us/library/system.net.http.httpclienthandler.aspx
[HttpRequestMessage]: http://msdn.microsoft.com/en-us/library/system.net.http.httprequestmessage.aspx
[MediaTypeFormatter]: http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx

[Json.NET]: http://james.newtonking.com/projects/json-net.aspx
[BSON]: https://en.wikipedia.org/wiki/BSON
[content negotiation]: http://en.wikipedia.org/wiki/Content_negotiation
[decorator pattern]: http://en.wikipedia.org/wiki/Decorator_pattern
[JSON]: https://en.wikipedia.org/wiki/JSON
[JSONP]: https://en.wikipedia.org/wiki/JSONP
[MiniProfiler]: http://miniprofiler.com/

[IClient]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6
[IRequest]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequest.cs#L12
[MediaTypeFormatterBase]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters/MediaTypeFormatterBase.cs#L10
[JsonNetBsonFormatter]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetBsonFormatter.cs#L11
[JsonNetFormatter]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetFormatter.cs#L10

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
[Pathoschild.Http.Formatters.Core]: https://nuget.org/packages/Pathoschild.Http.Formatters.Core
[Pathoschild.Http.Formatters.JsonNet]: https://nuget.org/packages/Pathoschild.Http.Formatters.JsonNet