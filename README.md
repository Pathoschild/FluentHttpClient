**Pathoschild.FluentHttpClient** is a fluent wrapper over the .NET 4.5 [HttpClient][] for creating strongly-typed easy-to-use REST API clients.

## Installation
The fluent client is available as the [Pathoschild.Http.FluentClient][] NuGet package.

Optional addons:
* [Pathoschild.Http.Formatters.Core][] includes `Pathoschild.Http.Formatters`;
* [Pathoschild.Http.Formatters.JsonNet][] includes `Pathoschild.Http.Formatters.JsonNet` and `Pathoschild.Http.Formatters`.

## Usage
The client is a fluent wrapper over HttpClient. You start by creating a client, and chain methods to configure your request and response.

```c#
     IClient client = new FluentClient("http://example.org/api/");
```

The most common usage is a synchronous HTTP GET, with the response deserialized into a class instance:

```c#
     Idea idea = client
        .Get("ideas/14")
        .RetrieveAs<Idea>();
```

You can fluently customize the request and directly alter the [HttpRequestMessage][]:

```c#
     Idea idea = client
        .Post("ideas", new Idea())
        .WithHeader("Content-Type", "application/json")
        .WithArgument("tenant", "company-name")
        .WithCustom(message => { message.RequestUri = new Uri("http://example.org/api2/", "ideas"); })
        .RetrieveAs<Idea>();
```

The response can also be fluently configured:

```c#
     string jsonIdea = client
        .Get("ideas/14")
        .Retrieve()
        .AsString(); // or As<T>, AsList<T>, AsMessage, AsByteArray, AsStream
```

And you can do everything asynchronously:

```c#
     Task<Idea> query = client
        .Get("ideas/14")
        .Retrieve()
        .AsAsync<Idea>();
```

And you can configure a range of features like credentials and cookies using the default [HTTP message handler][HttpClientHandler]:
```c#
     client.MessageHandler.Credentials = new NetworkCredential("username", "password");
     client.MessageHandler.CookieContainer.Add(new Cookie(...));
     client.MessageHandler.Proxy = new WebProxy(...);
```

The code documentation provides more details on usage: see [IClient][], [IRequestBuilder][], and [IResponse][].

## Extension
### Custom formats
The client uses .NET's [MediaTypeFormatter][]s for serializing and deserializing HTTP messages — the same ones used by the HttpClient itself and the ASP.NET Web API. The client uses Microsoft's default formatters by default. When creating a client for an ASP.NET Web API, this lets you seamlessly use the same formatters on both sides.

You can use any of the many implementations already available (including the Json.NET formatters below), or create your own ([MediaTypeFormatterBase][] might help). For example, to replace the default JSON formatter with the formatter below:
```c#
     IClient client = new FluentClient("http://example.org/api/");
     client.Formatters.Remove(client.Formatters.JsonFormatter);
     client.Formatters.Add(new JsonNetFormatter());
```

#### Json.NET
The [Pathoschild.Http.Formatters.JsonNet][] package provides three formats using [Json.NET][]: [BSON][] (`application/bson`), [JSON][] (`application/json`, `text/json`), and [JSONP][] (`application/javascript`, `application/ecmascript`, `text/javascript`, `text/ecmascript`). JSONP requests can include an optional `callback` query parameter that specifies the JavaScript method name to invoke.
```c#
     client.Formatters.Add(new JsonNetBsonFormatter());
     client.Formatters.Add(new JsonNetFormatter());
```

### Custom message handler
You can inject your own [HTTP message handler][HttpClientHandler] to do pretty much anything you want. For example, you could easily create a custom handler for unit tests which talks directly to a mock without actual HTTP calls:
```c#
     UnitTestHandler handler = new UnitTestHandler() { WasCalled = false };
     IClient<UnitTestHandler> client = new FluentClient<UnitTestHandler>(new HttpClient(handler), handler);
     bool wasCalled = client.MessageHandler.WasCalled; // strongly-typed access to the handler
```

### Custom implementations
You can create your own implementations of the client interfaces (`IClient`, `IRequestBuilder`, and `IResponse`) — the default classes have virtual methods, so you can subclass them to override individual methods and properties.

You can also use the [decorator pattern] with the delegating implementations (`DelegatingFluentClient`, `DelegatingRequestBuilder`, and `DelegatingResponse`) to easily inject specialized behaviour without reimplementing entire interfaces. These implementations let you override individual methods and properties while delegating everything else to another implementation.

For example, this delegating response tracks time spent waiting for HTTP requests using [MiniProfiler]:
```c#
     public class ProfiledResponse : DelegatingResponse
     {
        public ProfiledResponse(IResponse response)
           : base(response) { }

        public override IResponse Wait()
        {
           using (MiniProfiler.Current.Step("Waiting for API"))
              return base.Wait();
        }
     }
```

You can then combine decorators to inject the behaviour you want:
```c#
     // create decorated instance
     IClient client = new FluentClient("http://example.org/api/");
     client = new ProfiledClient(client);  // example implementation which constructs ProfiledResponse
     client = new AuditedClient(client);

     // use it the same way
     Idea idea = client
        .Get("ideas/14")
        .RetrieveAs<Idea>();
```

[HttpClient]: http://code.msdn.microsoft.com/Introduction-to-HttpClient-4a2d9cee
[HttpClientHandler]: http://msdn.microsoft.com/en-us/library/system.net.http.httpclienthandler.aspx
[HttpRequestMessage]: http://msdn.microsoft.com/en-us/library/system.net.http.httprequestmessage.aspx
[MediaTypeFormatter]: http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx

[Json.NET]: http://james.newtonking.com/projects/json-net.aspx
[BSON]: https://en.wikipedia.org/wiki/BSON
[decorator pattern]: http://en.wikipedia.org/wiki/Decorator_pattern
[JSON]: https://en.wikipedia.org/wiki/JSON
[JSONP]: https://en.wikipedia.org/wiki/JSONP
[MiniProfiler]: http://miniprofiler.com/

[IClient]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IClient.cs#L6
[IRequestBuilder]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IRequestBuilder.cs#L10
[IResponse]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Client/IResponse.cs#L10
[MediaTypeFormatterBase]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters/MediaTypeFormatterBase.cs#L10
[JsonNetBsonFormatter]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetBsonFormatter.cs#L11
[JsonNetFormatter]: https://github.com/Pathoschild/Pathoschild.FluentHttpClient/blob/master/Formatters.JsonNet/JsonNetFormatter.cs#L10

[Pathoschild.Http.FluentClient]: https://nuget.org/packages/Pathoschild.Http.FluentClient
[Pathoschild.Http.Formatters.Core]: https://nuget.org/packages/Pathoschild.Http.Formatters.Core
[Pathoschild.Http.Formatters.JsonNet]: https://nuget.org/packages/Pathoschild.Http.Formatters.JsonNet