**Pathoschild.FluentHttpClient** is a strongly-typed easy-to-use asynchronous REST API client, built on top of the .NET 4.5 [HttpClient][]. The client provides a single fluent interface that lets you create an HTTP request, dispatch and wait for it, and process the response. (The client will automatically inject any required HTTP configuration, like `User-Agent` and `Accept` headers.)

## Usage
You start by creating a client, and chain methods to configure your request and response:

```c#
     IClient client = new FluentClient("http://example.org/api/");
```

### Build a request
The most common use is an asynchronous HTTP GET, with the response deserialized into a class instance. Let's say we're fetching an `Idea` object from a REST API (it could be JSON, XML, or anything else):

```c#
     Idea idea = await client
        .GetAsync("ideas/14")
        .As<Idea>();
```

You can fluently configure the request:

```c#
     Idea idea = await client
        .GetAsync("ideas")
        .WithHeader("Content-Type", "application/json")
        .WithArgument("id", 14)
        .WithArgument("tenant", "tenant-name")
        .As<Idea>();
```

And use any HTTP verb:

```c#
     Idea idea = await client
        .PostAsync("ideas", new Idea())
        .As<Idea>();
```

You can even configure a range of features like credentials and cookies using the default [HTTP message handler][HttpClientHandler]:
```c#
     client.MessageHandler.Credentials = new NetworkCredential("username", "password");
     client.MessageHandler.CookieContainer.Add(new Cookie(...));
     client.MessageHandler.Proxy = new WebProxy(...);
```

_(For brevity some methods aren't shown, but every method is fully documented so it's easy to just use the client.)_

### Retrieve the response
The response can be fluently configured in the same way. By default you can retrieve the response as a deserialized model, list of models, byte array, string, or stream:

```c#
     string jsonIdea = await client
        .GetAsync("ideas/14")
        .AsString();
```

If you don't need the response, you can just wait for the request to complete. (This will still handle errors, so you don't have to worry about hiding those.)

```c#
     await client.PostAsync("ideas", new Idea());
```

### Synchronous use
The client is designed to take advantage of the `async` and `await` keywords in .NET 4.5, but you can use the client synchronously:

```c#
     Idea idea = client
        .GetAsync("ideas/14")
        .AsString()
        .Result;
```

Or if you don't need the response:

```c#
     client.PostAsync("ideas", new Idea()).Wait();
```

## Installation
The fluent client is available as the [Pathoschild.Http.FluentClient][] NuGet package.

Optional addons:
* [Pathoschild.Http.Formatters.Core][] includes `Pathoschild.Http.Formatters`;
* [Pathoschild.Http.Formatters.JsonNet][] includes `Pathoschild.Http.Formatters.JsonNet` and `Pathoschild.Http.Formatters`.

## Extension
### Custom formats
The client uses .NET's [MediaTypeFormatter][]s for serializing and deserializing HTTP messages, and uses [content negotiation][] to select the relevant format. These are the same ones used by the HttpClient itself and the ASP.NET Web API. When creating a client for an ASP.NET Web API, this lets you seamlessly use the same formatters on both sides.

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
You can create your own implementations of the client interfaces (`IClient` and `IRequest`) — the default classes have virtual methods, so you can subclass them to override individual methods and properties.

#### Factory
You can inject your own implementations of the interfaces using the `IFactory`, which is called whenever the client needs an implementation. You can subclass the default `Factory` and only override the methods you're interested in.
```c#
     IClient client = new FluentClient("http://example.org/api/", new CustomFactory());
```

#### Decorator pattern
You can combine the factory with the [decorator pattern] using the delegating implementations (`DelegatingFluentClient` and `DelegatingRequest`) to inject specialized behaviour. These implementations let you override individual methods and properties while delegating everything else to another implementation.

For example, this delegating response tracks time spent waiting for HTTP requests using [MiniProfiler]:
```c#
     public class ProfiledRequest : DelegatingRequest
     {
        public ProfiledRequest(IRequest request)
           : base(request) { }

        public override Task<HttpResponseMessage> AsMessage()
        {
           using (MiniProfiler.Current.Step("Waiting for API"))
              return base.AsMessage();
        }
     }
```

You can then combine decorators to inject the behaviour you want:
```c#
     // override factory
     public class CustomFactory : Factory
     {
        public override IRequest GetRequest(HttpRequestMessage message, MediaTypeFormatterCollection formatters, Func<IRequest, Task<HttpResponseMessage>> dispatcher)
        {
           IRequest request = base.GetRequest(message, formatters, dispatcher);
           request = new ProfiledRequest(request);
           request = new AuditedRequest(request);
           return request;
        }
     }
	 
     // use the client without worrying about what behaviour is injected
     IClient client = new FluentClient("http://example.org/api/", new CustomFactory());
     Idea idea = await client
        .Get("ideas/14")
        .As<Idea>();
```

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