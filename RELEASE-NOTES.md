# Release notes
## 3.0
Upcoming release.

* Added support for fault tolerance using `IRequestCoordinator`.
* Added an optional `RetryCoordinator` that automatically retries failed requests.
* Added fluent methods to set authentication headers, with built-in support for basic auth and bearer tokens.
* Added fluent method to override the default `User-Agent` header.
* Added support for `IWebProxy`.
* Added support for cancellation tokens.
* Revamped `IResponse` to make it easier to read response data before parsing the body.
* Simplified `IClient` and `IRequest` by moving some methods into extension methods.
* Changed `response.AsList<T>` to return an array instead of `List<T>`.
* Fixed the underlying `HttpClient` being disposed when it isn't owned by the fluent client.
* Fixed `client.Filters` not added to the interface.
* Fixed `client.Filters.Remove<T>()` only removing the first matching filter.

## 2.3
Released 12 December 2016. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/2.2.0..2.3).

* Migrated to .NET Standard 1.3 + .NET Core to improve crossplatform support.

## 2.2
Released 30 June 2016. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/2.1.0..2.2.0).

* Updated to latest version of Json.NET.
* Merged formatters library into client.
* Prepared for migration to .NET Core.


## 2.1
Released 08 May 2016. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/2.0.0..2.1.0).

* Migrated to PCL for cross-platform compatibility.
* Removed support for JSONP.  
  _(This isn't needed since a JSONP API most likely supports JSON, and removing it eliminates a
  dependency on non-PCL code.)_
* Deprecated `JsonNetFormatter`.  
  _(The underlying HttpClient now uses Json.NET by default.)_

## 2.0
Released 28 April 2016. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/1.2.1..2.0.0).

* Replace `IFactory` with a new extensibility model using `IHttpFilter`.  
  _(This enables simpler and more powerful extensibility without exposing implementation details.
  For example, error handling can now read the underlying HTTP response directly without
  temporarily changing the `RaiseErrors` flag.)_
* Removed request cloning (no longer needed).
* Updated to the latest version of HttpClient and Json.NET.

## 1.2.1
Released 28 October 2015. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/1.2.0..1.2.1).

* The client is now `IDisposable`.

## 1.2
Released 30 October 2013. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/1.1.0..1.2.0).

* Updated to latest versions of HttpClient and Json.NET.

## 1.1
Released 28 August 2013. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/1.0.0..1.1.0).

* Added request cloning to support use cases like batch queries.
* Added UTF-8 as a supported encoding by default.


## 1.0
Released 23 May 2012. See [log](https://github.com/Pathoschild/FluentHttpClient/compare/a316a15a7aaa8b3882fa9111db192a1d962b72ed...1.0.0).

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


