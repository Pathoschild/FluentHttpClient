# Release notes
## Upcoming release
* Updated to Json.Net 13.0.1.
* Fixed IntelliSense docs for `With`/`SetOptions` (thanks to Jericho!).

## 4.1
Released 11 March 2021.

* Added support for [chained retry policies](README.md#chained-retry-policies) (thanks to Jericho!).
* Fixed `WithBody(null)` no longer allowed in 4.0.

## 4.0
Released 13 May 2020.

* Added nullable reference type annotations.
* Added support for creating a client without a base URL.
* Improved documentation.
* Updated dependencies in .NET Standard 1.3 package.
* Fixed `FormUrlEncoded` body created from a dictionary being restricted to URL length and not allowing null.
* **Breaking changes:**
  * Null arguments like `request.WithArgument("x", null)` are now ignored instead of sending a blank value. You can use the previous behaviour using `client.SetOptions(ignoreNullArguments: false)`.
  * Simplified constructors to make usage more intuitive.
  * Removed deprecated code:

    old code | migration
    -------- | ---------
    `client.SetHttpErrorAsException` | use `client.SetOptions`.
    `request.WithBodyContent` | use `request.WithBody`.
    some `request.WithBody` model overloads | use `request.WithBody(builder => builder.Model(...))`.
    `request.WithHttpErrorAsException` | use `request.WithOptions`.
    `BsonFormatter` | use a BSON `MediaTypeFormatter` package like [WebApiContrib.Core.Formatter.Bson](https://www.nuget.org/packages/WebApiContrib.Core.Formatter.Bson).

## 3.3.1
Released 19 July 2019.

* `request.WithBody` now allows `HttpContent` input.
* `bodyBuilder.FormUrlEncoded` now has an overload for dictionary input.
* Fixed form-URL-encoded body helper enforcing URL length limits.

## 3.3
Released 27 April 2019.

* Added support for reading responses into `JToken`, `JObject`, `JArray`, or `dynamic`.
* Added fluent HTTP body builders, and deprecated some older body methods to simplify usage.
* Added `IsSuccessStatusCode` to response.
* Fixed default web proxy not being used.
* Updated dependencies.

## 3.2
Released 18 April 2018.

* Added options to finetune behavior (see `client.SetOptions` and `request.WithOptions`).
* The base URL now behaves more intuitively in many cases.
* `Request.WithArguments` now omits null argument values by default.  
* `Request.WithArguments` now also accepts key/value pairs.
* Fixed cookie headers not being set.
* Fixed error when passing an object with an indexer property to `WithArguments`.
* Fixed `WithArguments` changing the URL incorrectly when it has a #fragment.
* Deprecated `client.SetHttpErrorAsException` and `request.WithHttpErrorAsException` (use `client.SetOptions` and `request.WithOptions` instead).
* Compatibility changes:
  * Added .NET Framework 4.5 (previously 4.5.2).
  * Simplified dependencies in .NET Framework 4.5+ and .NET Standard 2.0+.

**Possible impacting changes:**
  * The base URL is no longer truncated in some cases. For example, a base URL `https://example.org/index.php` with resource `api` now resolves to `https://example.org/index.php/api` instead of `https://example.org/api`.

## 3.1
Released 19 September 2017.

* Added option to set default behaviour for all requests.  
  <small>_For example, when using an API with URL-based authentication, you can do `client.AddDefault(request => request.WithArgument("token", "..."))` to add that argument to all later requests._</small>
* Added support for retrying timed-out requests.
* Deprecated `BsonFormatter`.  
  <small>_This uses Json.NET's `BsonReader`, which is now deprecated. The format isn't used often enough to justify adding a new dependency. If you use it, you can switch to another BSON media type formatter or copy it from the FluentHttpClient code before it's removed._</small>
* Fixed error when retrying a request with `POST` content.
* Fixed `IRetryConfig.MaxRetries` counting the initial request as a retry.  
  <small>_For example, `maxRetries: 1` never retried. This value now sets the maximum number of retries after the initial request.</small>_

## 3.0
Released 08 February 2017.

* New features:
  * Added built-in retry support.
  * Added methods to set authentication headers (with wrappers for basic auth and OAuth bearer tokens).
  * Added support for cancellation tokens.
  * Added support for disabling HTTP-errors-as-exceptions per-request or per-client.
  * Added support for fault tolerance using `IRequestCoordinator`.
  * Added support for `IWebProxy`.
  * Added `client.PatchAsync`.
  * Added `client.SetUserAgent` to override default `User-Agent` header.
* Breaking changes:
  * Replaced `response.AsList<T>` with `reponse.AsArray<T>`.
  * Removed `JsonNetFormatter` (deprecated since 2.1, now built-in).
  * Revamped `IResponse` to make it easier to read response data.
  * Simplified `IClient` and `IRequest` by moving some methods into extension methods.
  * Simplified `IHttpFilter` by removing the message arguments (already accessible via `IRequest` and `IResponse`).
* Improvements:
  * Fixed the underlying `HttpClient` being disposed when it isn't owned by the fluent client.
  * Fixed `client.Filters` not added to the interface.
  * Fixed `client.Filters.Remove<T>()` only removing the first match.
  * Fixed unintuitive behaviour when the base URL doesn't end in a slash.
* Relicensed from CC-BY 3.0 to more permissive MIT license.

## 2.3
Released 12 December 2016.

* Migrated to .NET Standard 1.3 + .NET Core to improve crossplatform support.

## 2.2
Released 30 June 2016.

* Updated to latest version of Json.NET.
* Merged formatters library into client.
* Prepared for migration to .NET Core.


## 2.1
Released 08 May 2016.

* Migrated to PCL for cross-platform compatibility.
* Removed support for JSONP.  
  _(This isn't needed since a JSONP API most likely supports JSON, and removing it eliminates a
  dependency on non-PCL code.)_
* Deprecated `JsonNetFormatter`.  
  _(The underlying HttpClient now uses Json.NET by default.)_

## 2.0
Released 28 April 2016.

* Replace `IFactory` with a new extensibility model using `IHttpFilter`.  
  _(This enables simpler and more powerful extensibility without exposing implementation details.
  For example, error handling can now read the underlying HTTP response directly without
  temporarily changing the `RaiseErrors` flag.)_
* Removed request cloning (no longer needed).
* Updated to the latest version of HttpClient and Json.NET.

## 1.2.1
Released 28 October 2015.

* The client is now `IDisposable`.

## 1.2
Released 30 October 2013.

* Updated to latest versions of HttpClient and Json.NET.

## 1.1
Released 28 August 2013.

* Added request cloning to support use cases like batch queries.
* Added UTF-8 as a supported encoding by default.


## 1.0
Released 23 May 2012.

* Initial client release:
  * Wrapped HttpClient with a fluent interface.
  * Added user-agent and accept headers by default.
  * Added `ApiException` thrown when server returns a non-success error code.
  * Added `request.WithArgument` to format URL arguments from an anonymous object or key + value.
  * Added `response.As<T>()`, `.AsList<T>()`, `.AsByteArray()`, `AsString()`, and `AsStream()` to parse the response body.
  * Added `response.Wait()` to simplify synchronous use.
  * Added support for customising the message handler.
  * Added `IFactory` for extensibility.
* Initial formatters release:
  * Added base `MediaTypeFormatter` class to simplify implementations.
  * Added `MediaTypeFormatter` implementations for BSON, JSON, and JSONP using Json.NET.
  * Added `MediaTypeFormatter` for plaintext (serialisation only).
* Added unit tests.


