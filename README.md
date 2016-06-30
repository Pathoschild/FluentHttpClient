**Pathoschild.FluentHttpClient** is an easy asynchronous HTTP client for REST APIs. It provides a fluent interface that lets you create an HTTP request, dispatch and wait for it, and parse the response. The client takes care of the gritty details for you (like deserialisation, [content negotiation][], and URL encoding), and is easy to extend and customise.

## Installing
The fluent client is [available on NuGet][Pathoschild.Http.FluentClient]:
> Install-Package Pathoschild.Http.FluentClient

This is a portable library compatible with multiple platforms (.NET 4.5+, Windows 8+ apps, Universal Windows Platform, and ASP.NET Core 1.0).

## Using the client
### Basic usage
You start by creating a client:

```c#
IClient client = new FluentClient("https://example.org/api/");
```

Next you chain methods to configure your request and response handling. For example, here's a simple GET request whose response will be parsed into an `Item` model:
```c#
Item item = await client
    .GetAsync("items/14")
    .As<Item>();
```

You can get the response as a model, list of models, byte array, string, or stream:
```c#
string json = await client
    .GetAsync("items/14")
    .AsString();
```

If you don't need the response, you can just wait for the request to complete.
```c#
await client.PostAsync("items", new Item(..));
```

You can configure some pretty complex requests using the fluent interface (the client will take care of the details like input sanitisation and URL encoding):
```c#
Item item = await client
    .GetAsync("items")
    .WithHeader("Content-Type", "application/json")
    .WithArguments(new { id = 14, tenant = "tenant-name" }) // or .WithArgument("id", 14).WithArgument("tenant", "tenant-name")
    .As<Item>();
```

A lot of features aren't shown in these examples, but it should be fairly discoverable since every method is fully code-documented for IntelliSense.

### Error handling
If the server returns a non-success HTTP code, the client will raise an `ApiException` by default. The exception includes all the information needed to troubleshoot the error, including the underlying HTTP request and response.

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
    string responseText = await ex.ResponseMessage.Content.ReadAsStringAsync();
    throw new Exception($"The API responded with HTTP {ex.ResponseMessage.StatusCode}: {responseText}");
}
```

If you don't want the client to throw an exception, you can simply remove the default error handler:
```c#
client.Filters.Remove<DefaultErrorFilter>();
```

You can also add your own error handling; see _customising the client_ below.


### Synchronous use
The client is designed to take advantage of the `async` and `await` keywords in .NET 4.5, but you can use the client synchronously. This is *not* recommended — it complicates error-handling (e.g. errors get wrapped into [AggregateException][]), and it's very easy to cause thread deadlocks when you do this (see _[Parallel Programming with .NET: Await, and UI, and deadlocks! Oh my!](http://blogs.msdn.com/b/pfxteam/archive/2011/01/13/10115163.aspx)_ and _[Don't Block on Async Code](http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html))._

If you really need to use it synchronously, you can just call the `Result` property:
```c#
Item item = client
    .GetAsync("items/14")
    .As<Item>()
    .Result;
```

Or if you don't need the response:

```c#
client.PostAsync("items", new Item()).AsMessage().Wait();
```

## Customising the client
### Custom formats
By default the client supports JSON and XML. The client recognises [`MediaTypeFormatter` implementations][MediaTypeFormatter], so you can easily add different formats:
```c#
client.Formatters.Add(new BsonFormatter());
```

You can use [one of the many `MediaTypeFormatter` implementations](https://www.nuget.org/packages?q=MediaTypeFormatter), use the included BSON formatter, or create your own (optionally using the included `MediaTypeFormatterBase` base class).

### Custom behaviour
You can customise the client by adding your own implementations of `IHttpFilter`. Each filter can read and change the underlying HTTP requests (e.g. for authentication) and responses (e.g. for error handling). For example, you can easily replace the default error handling (see _Error handling_ above):
```c#
client.Filters.Remove<DefaultErrorFilter>();
client.Filters.Add(new YourErrorFilter());
```

For reference, the default error filter is essentially this one method:
```c#
/// <summary>Method invoked just after the HTTP response is received. This method can modify the incoming HTTP response.</summary>
/// <param name="response">The HTTP response.</param>
/// <param name="responseMessage">The underlying HTTP response message.</param>
public void OnResponse(IResponse response, HttpResponseMessage responseMessage)
{
    if (!responseMessage.IsSuccessStatusCode)
        throw new ApiException(response, responseMessage, $"The API query failed with status code {responseMessage.StatusCode}: {responseMessage.ReasonPhrase}");
}
```

That's a pretty simple filter, but you can do some much more advanced things by changing the request and response messages. For example, here's a minimal filter that injects an authentication token into every HTTP request:
```c#
/// <summary>Method invoked just before the HTTP request is submitted. This method can modify the outgoing HTTP request.</summary>
/// <param name="request">The HTTP request.</param>
/// <param name="requestMessage">The underlying HTTP request message.</param>
public void OnRequest(IRequest request, HttpRequestMessage requestMessage)
{
    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", "...");
}
```

You can even rewrite HTTP responses from the server before they're parsed if you want to.

### Custom HTTP client
For really advanced scenarios, you can customise the underlying [HttpClient][] and [HttpClientHandler][]:
```c#
// create custom HTTP handler
var handler = new HttpClientHandler()
{
    Credentials = new NetworkCredential("username", "password"),
    Proxy = new WebProxy(...)
};
handler.CookieContainer.Add(new Cookie(...));

// create client
var client = new FluentClient("http://example.org/api/", new HttpClient(handler));
```

[AggregateException]: http://msdn.microsoft.com/en-us/library/system.aggregateexception.aspx
[HttpClient]: https://msdn.microsoft.com/en-us/library/system.net.http.httpclient.aspx
[HttpClientHandler]: http://msdn.microsoft.com/en-us/library/system.net.http.httpclienthandler.aspx
[MediaTypeFormatter]: http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx

[Json.NET]: http://james.newtonking.com/projects/json-net.aspx
[BSON]: https://en.wikipedia.org/wiki/BSON
[content negotiation]: http://en.wikipedia.org/wiki/Content_negotiation
[JSON]: https://en.wikipedia.org/wiki/JSON
[JSONP]: https://en.wikipedia.org/wiki/JSONP

[IClient]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6
[IRequest]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequest.cs#L12

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
