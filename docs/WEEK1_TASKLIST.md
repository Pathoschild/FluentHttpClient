# Week 1 Task List: Interface Definition Phase

## Overview

**Objective**: Define abstract C++ interfaces that capture FluentHttpClient's fluent API design while accommodating Modular's specific requirements (rate limiting, download progress, file downloads).

**Duration**: 5 working days
**Output**: Header files defining the core interface contracts for the fluent HTTP client layer

**Reference Materials**:
- FluentHttpClient interfaces: `Client/IClient.cs`, `Client/IRequest.cs`, `Client/IResponse.cs`
- FluentHttpClient extensibility: `Client/Extensibility/IHttpFilter.cs`, `Client/Retry/*.cs`
- Modular current implementation: `include/core/HttpClient.h`, `include/core/RateLimiter.h`

---

## Prerequisites Checklist

Before starting Week 1, ensure the following:

- [ ] C++17 compatible compiler available (GCC 8+, Clang 7+, MSVC 2019+)
- [ ] CMake 3.20+ installed
- [ ] nlohmann/json library available (for JSON type references)
- [ ] Familiarity with Modular's existing `HttpClient`, `RateLimiter`, and `ILogger` interfaces
- [ ] Access to FluentHttpClient source code for reference
- [ ] Development environment configured with Modular project structure

---

## Directory Structure

Create the following directory structure in Modular:

```
include/
└── fluent/
    ├── IFluentClient.h      # Main client interface
    ├── IRequest.h           # Request builder interface
    ├── IResponse.h          # Response wrapper interface
    ├── IHttpFilter.h        # Middleware/filter interface
    ├── IBodyBuilder.h       # Request body construction interface
    ├── IRetryConfig.h       # Retry policy interface
    ├── IRequestCoordinator.h # Request dispatch coordinator interface
    ├── IRateLimiter.h       # Rate limiter interface (Modular-specific)
    ├── Types.h              # Common types, enums, and aliases
    └── Exceptions.h         # Exception hierarchy for fluent layer
```

---

## Day 1: Foundation Types and Common Definitions

### Task 1.1: Create Types.h - Common Type Definitions
**File**: `include/fluent/Types.h`
**Estimated Time**: 2 hours

**Instructions**:

1. Create the header file with standard include guards
2. Define the following types:

```cpp
#pragma once

#include <chrono>
#include <functional>
#include <map>
#include <memory>
#include <optional>
#include <string>
#include <string_view>
#include <variant>
#include <vector>

namespace modular::fluent {

//=============================================================================
// Forward Declarations
//=============================================================================
class IFluentClient;
class IRequest;
class IResponse;
class IHttpFilter;
class IBodyBuilder;
class IRetryConfig;
class IRequestCoordinator;
class IRateLimiter;

//=============================================================================
// Type Aliases
//=============================================================================

/// HTTP header collection (case-insensitive keys recommended)
using Headers = std::map<std::string, std::string>;

/// Query parameter collection
using QueryParams = std::vector<std::pair<std::string, std::string>>;

/// Progress callback: (bytesDownloaded, totalBytes) -> void
/// totalBytes may be 0 if Content-Length is unknown
using ProgressCallback = std::function<void(size_t downloaded, size_t total)>;

/// Request customization callback
using RequestCustomizer = std::function<void(IRequest&)>;

//=============================================================================
// Enumerations
//=============================================================================

/// HTTP methods supported by the fluent client
enum class HttpMethod {
    GET,
    POST,
    PUT,
    PATCH,
    DELETE,
    HEAD,
    OPTIONS
};

/// When to consider the response "complete"
enum class HttpCompletionOption {
    /// Wait for full response content to be read (default)
    ResponseContentRead,
    /// Return as soon as headers are received (for streaming)
    ResponseHeadersRead
};

/// HTTP status code categories
enum class StatusCategory {
    Informational,  // 1xx
    Success,        // 2xx
    Redirection,    // 3xx
    ClientError,    // 4xx
    ServerError     // 5xx
};

//=============================================================================
// Configuration Structures
//=============================================================================

/// Options that can be set per-request or as client defaults
struct RequestOptions {
    /// Whether HTTP error responses (4xx/5xx) should NOT throw exceptions
    std::optional<bool> ignoreHttpErrors;

    /// Whether null/empty arguments should be omitted from query string
    std::optional<bool> ignoreNullArguments;

    /// When to consider the response complete
    std::optional<HttpCompletionOption> completeWhen;

    /// Request timeout duration
    std::optional<std::chrono::seconds> timeout;
};

/// Retry policy configuration
struct RetryPolicy {
    /// Maximum number of retry attempts (0 = no retries)
    int maxRetries = 3;

    /// Initial delay before first retry
    std::chrono::milliseconds initialDelay{1000};

    /// Maximum delay between retries
    std::chrono::milliseconds maxDelay{16000};

    /// Whether to use exponential backoff (true) or fixed delay (false)
    bool exponentialBackoff = true;

    /// Jitter factor (0.0-1.0) to randomize delays
    double jitterFactor = 0.1;
};

//=============================================================================
// Utility Functions
//=============================================================================

/// Convert HttpMethod enum to string
constexpr std::string_view to_string(HttpMethod method) {
    switch (method) {
        case HttpMethod::GET:     return "GET";
        case HttpMethod::POST:    return "POST";
        case HttpMethod::PUT:     return "PUT";
        case HttpMethod::PATCH:   return "PATCH";
        case HttpMethod::DELETE:  return "DELETE";
        case HttpMethod::HEAD:    return "HEAD";
        case HttpMethod::OPTIONS: return "OPTIONS";
    }
    return "UNKNOWN";
}

/// Determine status category from HTTP status code
constexpr StatusCategory categorize_status(int statusCode) {
    if (statusCode >= 100 && statusCode < 200) return StatusCategory::Informational;
    if (statusCode >= 200 && statusCode < 300) return StatusCategory::Success;
    if (statusCode >= 300 && statusCode < 400) return StatusCategory::Redirection;
    if (statusCode >= 400 && statusCode < 500) return StatusCategory::ClientError;
    return StatusCategory::ServerError;
}

/// Check if status code indicates success (2xx)
constexpr bool is_success_status(int statusCode) {
    return statusCode >= 200 && statusCode < 300;
}

} // namespace modular::fluent
```

**Verification**:
- [ ] File compiles without errors
- [ ] All forward declarations are present
- [ ] Enums cover all HTTP methods used by Modular
- [ ] Type aliases are intuitive and match C++ conventions

---

### Task 1.2: Create Exceptions.h - Exception Hierarchy
**File**: `include/fluent/Exceptions.h`
**Estimated Time**: 2 hours

**Instructions**:

1. Design exception hierarchy mirroring Modular's existing exceptions but compatible with fluent API
2. Include response information in API exceptions

```cpp
#pragma once

#include "Types.h"
#include <stdexcept>
#include <string>
#include <memory>

namespace modular::fluent {

//=============================================================================
// Base Exception
//=============================================================================

/// Base exception for all fluent HTTP client errors
class FluentException : public std::runtime_error {
public:
    explicit FluentException(const std::string& message)
        : std::runtime_error(message) {}

    explicit FluentException(const std::string& message, std::exception_ptr cause)
        : std::runtime_error(message), cause_(cause) {}

    /// Get the underlying cause, if any
    std::exception_ptr cause() const noexcept { return cause_; }

private:
    std::exception_ptr cause_;
};

//=============================================================================
// Network Exceptions
//=============================================================================

/// Exception for network-level failures (DNS, connection, timeout)
class NetworkException : public FluentException {
public:
    enum class Reason {
        ConnectionFailed,
        DnsResolutionFailed,
        Timeout,
        SslError,
        Unknown
    };

    NetworkException(const std::string& message, Reason reason)
        : FluentException(message), reason_(reason) {}

    Reason reason() const noexcept { return reason_; }

    bool isTimeout() const noexcept { return reason_ == Reason::Timeout; }

private:
    Reason reason_;
};

//=============================================================================
// API Exceptions (HTTP-level errors)
//=============================================================================

/// Exception for HTTP error responses (4xx, 5xx)
/// Mirrors FluentHttpClient's ApiException
class ApiException : public FluentException {
public:
    ApiException(
        const std::string& message,
        int statusCode,
        std::string statusReason,
        Headers responseHeaders,
        std::string responseBody
    )
        : FluentException(message)
        , statusCode_(statusCode)
        , statusReason_(std::move(statusReason))
        , responseHeaders_(std::move(responseHeaders))
        , responseBody_(std::move(responseBody))
    {}

    /// HTTP status code (e.g., 404, 500)
    int statusCode() const noexcept { return statusCode_; }

    /// HTTP status reason phrase (e.g., "Not Found")
    const std::string& statusReason() const noexcept { return statusReason_; }

    /// Response headers from the failed request
    const Headers& responseHeaders() const noexcept { return responseHeaders_; }

    /// Response body content (may be empty)
    const std::string& responseBody() const noexcept { return responseBody_; }

    /// Check if this is a client error (4xx)
    bool isClientError() const noexcept {
        return statusCode_ >= 400 && statusCode_ < 500;
    }

    /// Check if this is a server error (5xx)
    bool isServerError() const noexcept {
        return statusCode_ >= 500;
    }

private:
    int statusCode_;
    std::string statusReason_;
    Headers responseHeaders_;
    std::string responseBody_;
};

//=============================================================================
// Specialized API Exceptions
//=============================================================================

/// Exception for rate limit exceeded (HTTP 429)
/// Includes retry timing information from response headers
class RateLimitException : public ApiException {
public:
    RateLimitException(
        const std::string& message,
        Headers responseHeaders,
        std::string responseBody,
        std::chrono::seconds retryAfter
    )
        : ApiException(message, 429, "Too Many Requests",
                       std::move(responseHeaders), std::move(responseBody))
        , retryAfter_(retryAfter)
    {}

    /// Suggested time to wait before retrying
    std::chrono::seconds retryAfter() const noexcept { return retryAfter_; }

private:
    std::chrono::seconds retryAfter_;
};

/// Exception for authentication failures (HTTP 401, 403)
class AuthException : public ApiException {
public:
    enum class Reason {
        Unauthorized,    // 401 - Missing or invalid credentials
        Forbidden        // 403 - Valid credentials but insufficient permissions
    };

    AuthException(
        const std::string& message,
        int statusCode,
        Headers responseHeaders,
        std::string responseBody
    )
        : ApiException(message, statusCode,
                       statusCode == 401 ? "Unauthorized" : "Forbidden",
                       std::move(responseHeaders), std::move(responseBody))
        , reason_(statusCode == 401 ? Reason::Unauthorized : Reason::Forbidden)
    {}

    Reason reason() const noexcept { return reason_; }

private:
    Reason reason_;
};

//=============================================================================
// Parse/Serialization Exceptions
//=============================================================================

/// Exception for JSON/response parsing failures
class ParseException : public FluentException {
public:
    ParseException(const std::string& message, const std::string& content)
        : FluentException(message), content_(content) {}

    /// The content that failed to parse
    const std::string& content() const noexcept { return content_; }

private:
    std::string content_;
};

//=============================================================================
// Configuration Exceptions
//=============================================================================

/// Exception for invalid configuration or setup errors
class ConfigurationException : public FluentException {
public:
    explicit ConfigurationException(const std::string& message)
        : FluentException(message) {}
};

} // namespace modular::fluent
```

**Verification**:
- [ ] Exception hierarchy is logical and follows C++ best practices
- [ ] All exceptions inherit from `std::exception` via `FluentException`
- [ ] `ApiException` captures all relevant response information
- [ ] Rate limit and auth exceptions carry domain-specific data
- [ ] Exceptions are move-efficient (use `std::move`)

---

### Task 1.3: Review and Document Design Decisions
**Estimated Time**: 1 hour

**Instructions**:

1. Create a design notes document capturing key decisions:

```markdown
# Day 1 Design Notes

## Type Decisions

1. **Headers as std::map**: Using `std::map<std::string, std::string>` for simplicity.
   Consider case-insensitive comparison if needed later.

2. **ProgressCallback signature**: `void(size_t, size_t)` matches Modular's existing
   callback signature for compatibility.

3. **RequestOptions as optionals**: Using `std::optional` allows distinguishing
   between "not set" (use default) and "explicitly set to value".

## Exception Design

1. **Separate NetworkException**: Network failures (DNS, timeout) are fundamentally
   different from HTTP errors and should be catchable separately.

2. **Rich ApiException**: Carrying full response allows error handlers to inspect
   headers (e.g., for rate limit info) and body (e.g., for error details).

3. **RateLimitException**: Critical for NexusMods API - includes `retryAfter` to
   enable intelligent retry logic.

## Open Questions

- Should we support multiple values per header key?
- Do we need async exception propagation (std::exception_ptr in futures)?
```

**Deliverable**: `docs/design/WEEK1_DAY1_NOTES.md`

---

## Day 2: Core Response and Filter Interfaces

### Task 2.1: Create IResponse.h - Response Interface
**File**: `include/fluent/IResponse.h`
**Estimated Time**: 3 hours

**Instructions**:

1. Define the response interface that wraps HTTP responses
2. Include both sync and async access patterns
3. Support JSON deserialization via nlohmann/json

```cpp
#pragma once

#include "Types.h"
#include "Exceptions.h"
#include <future>
#include <nlohmann/json.hpp>
#include <filesystem>

namespace modular::fluent {

/// Represents an HTTP response with methods to access and parse the body.
/// Mirrors FluentHttpClient's IResponse interface.
class IResponse {
public:
    virtual ~IResponse() = default;

    //=========================================================================
    // Status Information
    //=========================================================================

    /// Whether the HTTP response indicates success (2xx status code)
    virtual bool isSuccessStatusCode() const = 0;

    /// The HTTP status code (e.g., 200, 404, 500)
    virtual int statusCode() const = 0;

    /// The HTTP status reason phrase (e.g., "OK", "Not Found")
    virtual std::string statusReason() const = 0;

    //=========================================================================
    // Headers
    //=========================================================================

    /// Get all response headers
    virtual const Headers& headers() const = 0;

    /// Get a specific header value (empty string if not found)
    virtual std::string header(std::string_view name) const = 0;

    /// Check if a header exists
    virtual bool hasHeader(std::string_view name) const = 0;

    /// Get Content-Type header value
    virtual std::string contentType() const = 0;

    /// Get Content-Length header value (-1 if not present)
    virtual int64_t contentLength() const = 0;

    //=========================================================================
    // Body Access - Synchronous
    //=========================================================================

    /// Get the response body as a string
    /// @throws ParseException if body cannot be read
    virtual std::string asString() = 0;

    /// Get the response body as raw bytes
    virtual std::vector<uint8_t> asByteArray() = 0;

    /// Get the response body as a JSON object
    /// @throws ParseException if body is not valid JSON
    virtual nlohmann::json asJson() = 0;

    /// Deserialize the response body to a typed object
    /// @throws ParseException if deserialization fails
    template<typename T>
    T as() {
        return asJson().get<T>();
    }

    /// Deserialize the response body to an array of typed objects
    template<typename T>
    std::vector<T> asArray() {
        auto json = asJson();
        if (!json.is_array()) {
            throw ParseException("Expected JSON array", json.dump());
        }
        return json.get<std::vector<T>>();
    }

    //=========================================================================
    // Body Access - Asynchronous
    //=========================================================================

    /// Asynchronously get the response body as a string
    virtual std::future<std::string> asStringAsync() = 0;

    /// Asynchronously get the response body as raw bytes
    virtual std::future<std::vector<uint8_t>> asByteArrayAsync() = 0;

    /// Asynchronously get the response body as JSON
    virtual std::future<nlohmann::json> asJsonAsync() = 0;

    //=========================================================================
    // Stream Access (for large responses)
    //=========================================================================

    /// Save the response body to a file
    /// @param path Destination file path
    /// @param progress Optional progress callback
    /// @throws std::filesystem::filesystem_error on file I/O errors
    virtual void saveToFile(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) = 0;

    /// Asynchronously save the response body to a file
    virtual std::future<void> saveToFileAsync(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) = 0;

    //=========================================================================
    // Metadata
    //=========================================================================

    /// Get the final URL (after any redirects)
    virtual std::string effectiveUrl() const = 0;

    /// Get the total time taken for the request (including redirects)
    virtual std::chrono::milliseconds elapsed() const = 0;

    /// Check if the response was from a redirect
    virtual bool wasRedirected() const = 0;
};

/// Unique pointer type for response objects
using ResponsePtr = std::unique_ptr<IResponse>;

} // namespace modular::fluent
```

**Verification**:
- [ ] Interface covers all body access patterns (string, bytes, JSON, typed)
- [ ] Both sync and async variants are provided
- [ ] File download with progress is supported (Modular requirement)
- [ ] Header access is convenient with helper methods
- [ ] Template methods compile correctly

---

### Task 2.2: Create IHttpFilter.h - Filter/Middleware Interface
**File**: `include/fluent/IHttpFilter.h`
**Estimated Time**: 2 hours

**Instructions**:

1. Define the filter interface for request/response interception
2. Match FluentHttpClient's design with `OnRequest` and `OnResponse` methods
3. Add Modular-specific considerations (rate limiting awareness)

```cpp
#pragma once

#include "Types.h"
#include <memory>

namespace modular::fluent {

// Forward declarations
class IRequest;
class IResponse;

/// Middleware interface for intercepting and modifying HTTP requests and responses.
/// Filters are executed in order: OnRequest is called before the request is sent,
/// and OnResponse is called after the response is received.
///
/// This mirrors FluentHttpClient's IHttpFilter interface.
///
/// Common use cases:
/// - Authentication (add headers on request)
/// - Logging (log requests and responses)
/// - Error handling (throw on error status codes)
/// - Rate limiting (check/update limits)
/// - Caching (return cached response, skip request)
class IHttpFilter {
public:
    virtual ~IHttpFilter() = default;

    /// Called just before the HTTP request is sent.
    /// Implementations can modify the outgoing request (add headers, change URL, etc.)
    ///
    /// @param request The request about to be sent (mutable)
    ///
    /// @note Throwing an exception here will abort the request
    /// @note Filters are called in the order they were added to the client
    virtual void onRequest(IRequest& request) = 0;

    /// Called just after the HTTP response is received.
    /// Implementations can inspect or modify the response, or throw exceptions.
    ///
    /// @param response The response received (mutable)
    /// @param httpErrorAsException Whether HTTP errors (4xx/5xx) should throw
    ///        This reflects the client's `ignoreHttpErrors` setting
    ///
    /// @note Throwing an exception here will propagate to the caller
    /// @note Filters are called in reverse order (last added = first called)
    virtual void onResponse(IResponse& response, bool httpErrorAsException) = 0;

    /// Get a human-readable name for this filter (for logging/debugging)
    virtual std::string name() const { return "IHttpFilter"; }

    /// Get the priority of this filter (lower = earlier execution)
    /// Default filters use priority 1000. Use lower values to run earlier.
    virtual int priority() const { return 1000; }
};

/// Shared pointer type for filters (filters may be shared across requests)
using FilterPtr = std::shared_ptr<IHttpFilter>;

//=============================================================================
// Filter Collection Helper
//=============================================================================

/// A collection of filters with helper methods for management
class FilterCollection {
public:
    /// Add a filter to the collection
    void add(FilterPtr filter) {
        filters_.push_back(std::move(filter));
        sortByPriority();
    }

    /// Remove a specific filter instance
    bool remove(const FilterPtr& filter) {
        auto it = std::find(filters_.begin(), filters_.end(), filter);
        if (it != filters_.end()) {
            filters_.erase(it);
            return true;
        }
        return false;
    }

    /// Remove all filters of a specific type
    template<typename T>
    size_t removeAll() {
        size_t removed = 0;
        filters_.erase(
            std::remove_if(filters_.begin(), filters_.end(),
                [&removed](const FilterPtr& f) {
                    if (dynamic_cast<T*>(f.get())) {
                        ++removed;
                        return true;
                    }
                    return false;
                }),
            filters_.end()
        );
        return removed;
    }

    /// Check if collection contains a filter of a specific type
    template<typename T>
    bool contains() const {
        for (const auto& f : filters_) {
            if (dynamic_cast<T*>(f.get())) return true;
        }
        return false;
    }

    /// Get all filters (for iteration)
    const std::vector<FilterPtr>& all() const { return filters_; }

    /// Clear all filters
    void clear() { filters_.clear(); }

    /// Get number of filters
    size_t size() const { return filters_.size(); }

    /// Check if empty
    bool empty() const { return filters_.empty(); }

private:
    void sortByPriority() {
        std::sort(filters_.begin(), filters_.end(),
            [](const FilterPtr& a, const FilterPtr& b) {
                return a->priority() < b->priority();
            });
    }

    std::vector<FilterPtr> filters_;
};

} // namespace modular::fluent
```

**Verification**:
- [ ] Filter interface matches FluentHttpClient's design
- [ ] `FilterCollection` provides convenient management methods
- [ ] Type-safe removal via `removeAll<T>()` template
- [ ] Priority system allows filter ordering
- [ ] Documentation explains filter execution order

---

### Task 2.3: Create IRetryConfig.h - Retry Policy Interface
**File**: `include/fluent/IRetryConfig.h`
**Estimated Time**: 1.5 hours

**Instructions**:

```cpp
#pragma once

#include "Types.h"
#include <chrono>

namespace modular::fluent {

/// Interface for configuring retry strategies.
/// Multiple retry configs can be chained; each gets a chance to retry.
/// Mirrors FluentHttpClient's IRetryConfig interface.
class IRetryConfig {
public:
    virtual ~IRetryConfig() = default;

    /// Maximum number of times this config will retry a request
    virtual int maxRetries() const = 0;

    /// Determine whether a failed response should be retried
    /// @param statusCode HTTP status code (or 0 for network failures)
    /// @param isTimeout Whether the failure was a timeout
    /// @return true if the request should be retried
    virtual bool shouldRetry(int statusCode, bool isTimeout) const = 0;

    /// Calculate the delay before the next retry attempt
    /// @param attempt The retry attempt number (1 = first retry)
    /// @param statusCode HTTP status code (or 0 for network failures)
    /// @return Duration to wait before retrying
    virtual std::chrono::milliseconds getDelay(int attempt, int statusCode) const = 0;

    /// Get a human-readable name for this retry config (for logging)
    virtual std::string name() const { return "IRetryConfig"; }
};

/// Shared pointer type for retry configs
using RetryConfigPtr = std::shared_ptr<IRetryConfig>;

//=============================================================================
// Common Retry Configurations
//=============================================================================

/// Retry config that retries on server errors (5xx) with exponential backoff
class ServerErrorRetryConfig : public IRetryConfig {
public:
    explicit ServerErrorRetryConfig(
        int maxRetries = 3,
        std::chrono::milliseconds initialDelay = std::chrono::milliseconds{1000},
        std::chrono::milliseconds maxDelay = std::chrono::milliseconds{16000}
    )
        : maxRetries_(maxRetries)
        , initialDelay_(initialDelay)
        , maxDelay_(maxDelay)
    {}

    int maxRetries() const override { return maxRetries_; }

    bool shouldRetry(int statusCode, bool isTimeout) const override {
        // Retry on 5xx server errors or timeouts
        return isTimeout || (statusCode >= 500 && statusCode < 600);
    }

    std::chrono::milliseconds getDelay(int attempt, int /*statusCode*/) const override {
        // Exponential backoff: 1s, 2s, 4s, 8s, ...
        auto delay = initialDelay_ * (1 << (attempt - 1));
        return std::min(delay, maxDelay_);
    }

    std::string name() const override { return "ServerErrorRetryConfig"; }

private:
    int maxRetries_;
    std::chrono::milliseconds initialDelay_;
    std::chrono::milliseconds maxDelay_;
};

/// Retry config specifically for rate limit responses (HTTP 429)
/// Respects Retry-After header when available
class RateLimitRetryConfig : public IRetryConfig {
public:
    explicit RateLimitRetryConfig(int maxRetries = 1)
        : maxRetries_(maxRetries) {}

    int maxRetries() const override { return maxRetries_; }

    bool shouldRetry(int statusCode, bool /*isTimeout*/) const override {
        return statusCode == 429;
    }

    std::chrono::milliseconds getDelay(int /*attempt*/, int /*statusCode*/) const override {
        // Default delay; actual implementation should parse Retry-After header
        // This will be overridden by the coordinator with actual header value
        return std::chrono::seconds{60};
    }

    std::string name() const override { return "RateLimitRetryConfig"; }

private:
    int maxRetries_;
};

/// Retry config for network timeouts only
class TimeoutRetryConfig : public IRetryConfig {
public:
    explicit TimeoutRetryConfig(
        int maxRetries = 2,
        std::chrono::milliseconds delay = std::chrono::milliseconds{1000}
    )
        : maxRetries_(maxRetries)
        , delay_(delay)
    {}

    int maxRetries() const override { return maxRetries_; }

    bool shouldRetry(int /*statusCode*/, bool isTimeout) const override {
        return isTimeout;
    }

    std::chrono::milliseconds getDelay(int /*attempt*/, int /*statusCode*/) const override {
        return delay_;
    }

    std::string name() const override { return "TimeoutRetryConfig"; }

private:
    int maxRetries_;
    std::chrono::milliseconds delay_;
};

} // namespace modular::fluent
```

**Verification**:
- [ ] Interface matches FluentHttpClient's IRetryConfig
- [ ] Common retry configs are provided as reference implementations
- [ ] Exponential backoff calculation is correct
- [ ] Rate limit handling is accommodated

---

## Day 3: Request Builder Interface

### Task 3.1: Create IBodyBuilder.h - Body Construction Interface
**File**: `include/fluent/IBodyBuilder.h`
**Estimated Time**: 2 hours

**Instructions**:

```cpp
#pragma once

#include "Types.h"
#include <nlohmann/json.hpp>
#include <filesystem>
#include <sstream>

namespace modular::fluent {

/// Represents an HTTP request body that can be sent
struct RequestBody {
    /// The body content as bytes
    std::vector<uint8_t> content;

    /// The Content-Type header value
    std::string contentType;

    /// Create empty body
    RequestBody() = default;

    /// Create body from string
    RequestBody(std::string data, std::string type)
        : content(data.begin(), data.end())
        , contentType(std::move(type))
    {}

    /// Create body from bytes
    RequestBody(std::vector<uint8_t> data, std::string type)
        : content(std::move(data))
        , contentType(std::move(type))
    {}

    bool empty() const { return content.empty(); }
    size_t size() const { return content.size(); }
};

/// Interface for constructing HTTP request bodies.
/// Mirrors FluentHttpClient's IBodyBuilder interface.
class IBodyBuilder {
public:
    virtual ~IBodyBuilder() = default;

    //=========================================================================
    // Form URL Encoded
    //=========================================================================

    /// Create a form URL-encoded body from key-value pairs
    /// Content-Type: application/x-www-form-urlencoded
    virtual RequestBody formUrlEncoded(
        const std::vector<std::pair<std::string, std::string>>& arguments
    ) = 0;

    /// Create a form URL-encoded body from a map
    virtual RequestBody formUrlEncoded(
        const std::map<std::string, std::string>& arguments
    ) = 0;

    //=========================================================================
    // JSON Model
    //=========================================================================

    /// Create a JSON body from a serializable object
    /// Content-Type: application/json
    template<typename T>
    RequestBody model(const T& value) {
        nlohmann::json j = value;
        return jsonBody(j);
    }

    /// Create a JSON body from a json object
    virtual RequestBody jsonBody(const nlohmann::json& json) = 0;

    /// Create a JSON body from a raw string (no validation)
    virtual RequestBody rawJson(const std::string& jsonString) = 0;

    //=========================================================================
    // File Upload (Multipart Form Data)
    //=========================================================================

    /// Create a multipart form-data body for file upload
    /// Content-Type: multipart/form-data; boundary=...
    virtual RequestBody fileUpload(const std::filesystem::path& filePath) = 0;

    /// Create a multipart form-data body for multiple files
    virtual RequestBody fileUpload(
        const std::vector<std::filesystem::path>& filePaths
    ) = 0;

    /// Create a multipart form-data body with custom field names
    virtual RequestBody fileUpload(
        const std::vector<std::pair<std::string, std::filesystem::path>>& files
    ) = 0;

    /// Create a multipart form-data body from in-memory data
    virtual RequestBody fileUpload(
        const std::string& fieldName,
        const std::string& fileName,
        const std::vector<uint8_t>& data,
        const std::string& mimeType = "application/octet-stream"
    ) = 0;

    //=========================================================================
    // Raw Content
    //=========================================================================

    /// Create a body with raw string content
    virtual RequestBody raw(
        const std::string& content,
        const std::string& contentType = "text/plain"
    ) = 0;

    /// Create a body with raw binary content
    virtual RequestBody raw(
        const std::vector<uint8_t>& content,
        const std::string& contentType = "application/octet-stream"
    ) = 0;
};

/// Unique pointer type for body builder
using BodyBuilderPtr = std::unique_ptr<IBodyBuilder>;

} // namespace modular::fluent
```

**Verification**:
- [ ] All body formats from FluentHttpClient are supported
- [ ] File upload supports single file, multiple files, and in-memory data
- [ ] JSON serialization uses nlohmann/json
- [ ] Template method compiles correctly
- [ ] Multipart form-data boundary handling is specified

---

### Task 3.2: Create IRequest.h - Request Builder Interface
**File**: `include/fluent/IRequest.h`
**Estimated Time**: 4 hours

**Instructions**:

This is the most complex interface - the fluent request builder.

```cpp
#pragma once

#include "Types.h"
#include "IResponse.h"
#include "IBodyBuilder.h"
#include "IHttpFilter.h"
#include <future>
#include <functional>
#include <stop_token>

namespace modular::fluent {

// Forward declarations
class IRetryConfig;
class IRequestCoordinator;

/// Fluent interface for building and executing HTTP requests.
/// Supports method chaining and async execution.
/// Mirrors FluentHttpClient's IRequest interface.
///
/// Example usage:
/// @code
/// auto response = client->getAsync("users")
///     .withArgument("page", "1")
///     .withArgument("limit", "10")
///     .withBearerAuth(token)
///     .asResponse();
/// @endcode
class IRequest {
public:
    virtual ~IRequest() = default;

    //=========================================================================
    // Request Information (Read-only)
    //=========================================================================

    /// Get the HTTP method for this request
    virtual HttpMethod method() const = 0;

    /// Get the current URL (including any added query parameters)
    virtual std::string url() const = 0;

    /// Get the current headers
    virtual const Headers& headers() const = 0;

    /// Get the current request options
    virtual const RequestOptions& options() const = 0;

    //=========================================================================
    // URL Arguments (Query String)
    //=========================================================================

    /// Add a query string argument
    /// @param key Parameter name
    /// @param value Parameter value (will be URL-encoded)
    /// @return Reference to this request for chaining
    virtual IRequest& withArgument(std::string_view key, std::string_view value) = 0;

    /// Add a query string argument with numeric value
    template<typename T, typename = std::enable_if_t<std::is_arithmetic_v<T>>>
    IRequest& withArgument(std::string_view key, T value) {
        return withArgument(key, std::to_string(value));
    }

    /// Add multiple query string arguments
    virtual IRequest& withArguments(
        const std::vector<std::pair<std::string, std::string>>& arguments
    ) = 0;

    /// Add query string arguments from a map
    virtual IRequest& withArguments(
        const std::map<std::string, std::string>& arguments
    ) = 0;

    //=========================================================================
    // Headers
    //=========================================================================

    /// Set an HTTP header (replaces if exists)
    virtual IRequest& withHeader(std::string_view key, std::string_view value) = 0;

    /// Set multiple headers
    virtual IRequest& withHeaders(const Headers& headers) = 0;

    /// Remove a header
    virtual IRequest& withoutHeader(std::string_view key) = 0;

    //=========================================================================
    // Authentication
    //=========================================================================

    /// Set authentication header with custom scheme
    /// @param scheme Authentication scheme (e.g., "Bearer", "Basic")
    /// @param parameter Authentication parameter/token
    virtual IRequest& withAuthentication(
        std::string_view scheme,
        std::string_view parameter
    ) = 0;

    /// Set Bearer token authentication (OAuth, API keys)
    /// Equivalent to: withAuthentication("Bearer", token)
    virtual IRequest& withBearerAuth(std::string_view token) = 0;

    /// Set Basic authentication
    /// @param username The username
    /// @param password The password
    virtual IRequest& withBasicAuth(
        std::string_view username,
        std::string_view password
    ) = 0;

    //=========================================================================
    // Request Body
    //=========================================================================

    /// Set the request body using a builder function
    /// @param builder Function that constructs the body
    virtual IRequest& withBody(std::function<RequestBody(IBodyBuilder&)> builder) = 0;

    /// Set the request body directly
    virtual IRequest& withBody(RequestBody body) = 0;

    /// Set JSON body from a serializable object
    template<typename T>
    IRequest& withJsonBody(const T& value) {
        return withBody([&value](IBodyBuilder& b) {
            return b.model(value);
        });
    }

    /// Set form URL-encoded body
    virtual IRequest& withFormBody(
        const std::vector<std::pair<std::string, std::string>>& fields
    ) = 0;

    //=========================================================================
    // Options and Configuration
    //=========================================================================

    /// Set request-specific options (overrides client defaults)
    virtual IRequest& withOptions(const RequestOptions& options) = 0;

    /// Set whether HTTP errors should throw exceptions for this request
    virtual IRequest& withIgnoreHttpErrors(bool ignore = true) = 0;

    /// Set request timeout
    virtual IRequest& withTimeout(std::chrono::seconds timeout) = 0;

    /// Set cancellation token for this request
    virtual IRequest& withCancellation(std::stop_token token) = 0;

    //=========================================================================
    // Filters and Retry
    //=========================================================================

    /// Add a filter for this request only
    virtual IRequest& withFilter(FilterPtr filter) = 0;

    /// Remove a filter from this request
    virtual IRequest& withoutFilter(const FilterPtr& filter) = 0;

    /// Remove all filters of a specific type
    template<typename T>
    IRequest& withoutFilter() {
        removeFiltersOfType<T>();
        return *this;
    }

    /// Set request-specific retry configuration
    virtual IRequest& withRetryConfig(std::shared_ptr<IRetryConfig> config) = 0;

    /// Disable retries for this request
    virtual IRequest& withNoRetry() = 0;

    //=========================================================================
    // Custom Modifications
    //=========================================================================

    /// Apply custom modifications to the request
    /// Use sparingly - prefer specific methods when available
    virtual IRequest& withCustom(std::function<void(IRequest&)> customizer) = 0;

    //=========================================================================
    // Response - Asynchronous (Primary API)
    //=========================================================================

    /// Execute the request and return the response asynchronously
    virtual std::future<ResponsePtr> asResponseAsync() = 0;

    /// Execute and deserialize response to typed object asynchronously
    template<typename T>
    std::future<T> asAsync() {
        return std::async(std::launch::async, [this]() {
            auto response = asResponseAsync().get();
            if (!response->isSuccessStatusCode() && !options().ignoreHttpErrors.value_or(false)) {
                // Exception will be thrown by response parsing
            }
            return response->as<T>();
        });
    }

    /// Execute and get response as string asynchronously
    virtual std::future<std::string> asStringAsync() = 0;

    /// Execute and get response as JSON asynchronously
    virtual std::future<nlohmann::json> asJsonAsync() = 0;

    /// Execute and download response to file asynchronously
    virtual std::future<void> downloadToAsync(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) = 0;

    //=========================================================================
    // Response - Synchronous (Convenience)
    //=========================================================================

    /// Execute the request and return the response (blocking)
    ResponsePtr asResponse() {
        return asResponseAsync().get();
    }

    /// Execute and deserialize response to typed object (blocking)
    template<typename T>
    T as() {
        return asAsync<T>().get();
    }

    /// Execute and get response as string (blocking)
    std::string asString() {
        return asStringAsync().get();
    }

    /// Execute and get response as JSON (blocking)
    nlohmann::json asJson() {
        return asJsonAsync().get();
    }

    /// Execute and download to file (blocking)
    void downloadTo(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) {
        return downloadToAsync(path, progress).get();
    }

protected:
    /// Helper for removing filters by type (called by template)
    virtual void removeFiltersOfType(const std::type_info& type) = 0;

    template<typename T>
    void removeFiltersOfType() {
        removeFiltersOfType(typeid(T));
    }
};

/// Unique pointer type for request objects
using RequestPtr = std::unique_ptr<IRequest>;

} // namespace modular::fluent
```

**Verification**:
- [ ] All method chaining returns `IRequest&` for fluency
- [ ] Both sync and async response methods are available
- [ ] Template methods for typed deserialization work correctly
- [ ] Authentication helpers match FluentHttpClient API
- [ ] Download with progress callback is supported (Modular requirement)
- [ ] Filter management methods are complete
- [ ] Cancellation via `std::stop_token` is supported

---

## Day 4: Client and Coordinator Interfaces

### Task 4.1: Create IRequestCoordinator.h - Request Dispatch Interface
**File**: `include/fluent/IRequestCoordinator.h`
**Estimated Time**: 1.5 hours

**Instructions**:

```cpp
#pragma once

#include "Types.h"
#include "IRequest.h"
#include "IResponse.h"
#include <functional>
#include <future>

namespace modular::fluent {

/// Interface for controlling how requests are dispatched and retried.
/// Only one coordinator can be active on a client at a time.
/// Use filters for additional cross-cutting concerns.
///
/// Mirrors FluentHttpClient's IRequestCoordinator interface.
///
/// This is the integration point for:
/// - Custom retry logic (e.g., Polly-style policies)
/// - Circuit breakers
/// - Request queuing
/// - Rate limiting coordination
class IRequestCoordinator {
public:
    virtual ~IRequestCoordinator() = default;

    /// Execute an HTTP request with coordination logic.
    ///
    /// @param request The request to execute
    /// @param dispatcher Function that performs the actual HTTP request.
    ///        Call this to send the request; may be called multiple times for retries.
    /// @return The final response (after any retries)
    ///
    /// @note The coordinator owns the retry loop - dispatcher just sends one request
    /// @note Coordinator should handle exceptions from dispatcher appropriately
    ///
    /// Example implementation:
    /// @code
    /// std::future<ResponsePtr> ExecuteAsync(
    ///     IRequest& request,
    ///     std::function<std::future<ResponsePtr>(IRequest&)> dispatcher
    /// ) override {
    ///     return std::async([&]() {
    ///         int attempts = 0;
    ///         while (true) {
    ///             try {
    ///                 auto response = dispatcher(request).get();
    ///                 if (shouldRetry(response) && attempts < maxRetries_) {
    ///                     ++attempts;
    ///                     std::this_thread::sleep_for(getDelay(attempts));
    ///                     continue;
    ///                 }
    ///                 return response;
    ///             } catch (const NetworkException& e) {
    ///                 if (e.isTimeout() && attempts < maxRetries_) {
    ///                     ++attempts;
    ///                     continue;
    ///                 }
    ///                 throw;
    ///             }
    ///         }
    ///     });
    /// }
    /// @endcode
    virtual std::future<ResponsePtr> executeAsync(
        IRequest& request,
        std::function<std::future<ResponsePtr>(IRequest&)> dispatcher
    ) = 0;

    /// Get a human-readable name for this coordinator (for logging)
    virtual std::string name() const { return "IRequestCoordinator"; }
};

/// Shared pointer type for coordinators
using CoordinatorPtr = std::shared_ptr<IRequestCoordinator>;

//=============================================================================
// Default Coordinator Implementations
//=============================================================================

/// Coordinator that provides simple retry with exponential backoff
class RetryCoordinator : public IRequestCoordinator {
public:
    /// Create a retry coordinator from retry configs
    /// @param configs Retry configurations (tried in order)
    explicit RetryCoordinator(std::vector<std::shared_ptr<IRetryConfig>> configs)
        : configs_(std::move(configs)) {}

    /// Create a retry coordinator with simple parameters
    RetryCoordinator(
        int maxRetries,
        std::function<bool(int statusCode, bool isTimeout)> shouldRetry,
        std::function<std::chrono::milliseconds(int attempt)> getDelay
    );

    std::future<ResponsePtr> executeAsync(
        IRequest& request,
        std::function<std::future<ResponsePtr>(IRequest&)> dispatcher
    ) override;

    std::string name() const override { return "RetryCoordinator"; }

private:
    std::vector<std::shared_ptr<IRetryConfig>> configs_;
};

/// Coordinator that passes requests through without modification
class PassThroughCoordinator : public IRequestCoordinator {
public:
    std::future<ResponsePtr> executeAsync(
        IRequest& request,
        std::function<std::future<ResponsePtr>(IRequest&)> dispatcher
    ) override {
        return dispatcher(request);
    }

    std::string name() const override { return "PassThroughCoordinator"; }
};

} // namespace modular::fluent
```

**Verification**:
- [ ] Interface matches FluentHttpClient's IRequestCoordinator
- [ ] Default implementations provide useful starting points
- [ ] Documentation explains the dispatcher callback pattern
- [ ] Async pattern using `std::future` is consistent

---

### Task 4.2: Create IRateLimiter.h - Rate Limiter Interface (Modular-Specific)
**File**: `include/fluent/IRateLimiter.h`
**Estimated Time**: 2 hours

**Instructions**:

This interface is specific to Modular's needs for NexusMods API compliance.

```cpp
#pragma once

#include "Types.h"
#include <chrono>
#include <filesystem>

namespace modular::fluent {

/// Rate limit status information
struct RateLimitStatus {
    /// Daily requests remaining
    int dailyRemaining = 0;

    /// Daily request limit
    int dailyLimit = 0;

    /// When daily limit resets
    std::chrono::system_clock::time_point dailyReset;

    /// Hourly requests remaining
    int hourlyRemaining = 0;

    /// Hourly request limit
    int hourlyLimit = 0;

    /// When hourly limit resets
    std::chrono::system_clock::time_point hourlyReset;

    /// Whether we can make a request right now
    bool canRequest() const {
        return dailyRemaining > 0 && hourlyRemaining > 0;
    }

    /// Time until next request is allowed (0 if allowed now)
    std::chrono::milliseconds timeUntilAllowed() const;
};

/// Interface for rate limiting HTTP requests.
/// This is a Modular-specific extension for NexusMods API compliance.
///
/// NexusMods rate limits:
/// - 2,500 requests per day (resets at midnight UTC)
/// - 100 requests per hour (once daily exhausted)
///
/// Usage:
/// @code
/// // In a filter or coordinator
/// rateLimiter->waitIfNeeded();  // Block until allowed
/// auto response = sendRequest();
/// rateLimiter->updateFromHeaders(response.headers());
/// @endcode
class IRateLimiter {
public:
    virtual ~IRateLimiter() = default;

    //=========================================================================
    // Request Control
    //=========================================================================

    /// Check if a request can be made immediately
    virtual bool canMakeRequest() const = 0;

    /// Block until a request is allowed, then return
    /// @param maxWait Maximum time to wait (0 = wait indefinitely)
    /// @return true if request is now allowed, false if maxWait exceeded
    virtual bool waitIfNeeded(
        std::chrono::milliseconds maxWait = std::chrono::milliseconds{0}
    ) = 0;

    /// Record that a request was made (decrements counters)
    virtual void recordRequest() = 0;

    //=========================================================================
    // State Updates
    //=========================================================================

    /// Update rate limit state from HTTP response headers
    /// Parses headers like: x-rl-daily-remaining, x-rl-hourly-remaining, etc.
    /// @param headers Response headers to parse
    virtual void updateFromHeaders(const Headers& headers) = 0;

    /// Manually set rate limit values (for testing or manual override)
    virtual void setLimits(
        int dailyRemaining, int dailyLimit, std::chrono::system_clock::time_point dailyReset,
        int hourlyRemaining, int hourlyLimit, std::chrono::system_clock::time_point hourlyReset
    ) = 0;

    //=========================================================================
    // State Access
    //=========================================================================

    /// Get current rate limit status
    virtual RateLimitStatus status() const = 0;

    /// Get daily requests remaining
    virtual int dailyRemaining() const = 0;

    /// Get hourly requests remaining
    virtual int hourlyRemaining() const = 0;

    //=========================================================================
    // Persistence
    //=========================================================================

    /// Save rate limit state to file
    /// @param path File path to save state
    virtual void saveState(const std::filesystem::path& path) const = 0;

    /// Load rate limit state from file
    /// @param path File path to load state from
    /// @return true if state was loaded successfully
    virtual bool loadState(const std::filesystem::path& path) = 0;

    //=========================================================================
    // Events
    //=========================================================================

    /// Callback type for rate limit warnings
    using WarningCallback = std::function<void(const RateLimitStatus&)>;

    /// Set callback for when rate limits are low
    /// @param threshold Trigger when remaining drops below this
    /// @param callback Function to call with current status
    virtual void onLowLimit(int threshold, WarningCallback callback) = 0;
};

/// Shared pointer type for rate limiters
using RateLimiterPtr = std::shared_ptr<IRateLimiter>;

} // namespace modular::fluent
```

**Verification**:
- [ ] Interface captures Modular's existing RateLimiter functionality
- [ ] NexusMods-specific limits are documented
- [ ] State persistence methods are included
- [ ] Warning callback system is provided
- [ ] Thread-safety requirements are clear (add docs if needed)

---

### Task 4.3: Create IFluentClient.h - Main Client Interface
**File**: `include/fluent/IFluentClient.h`
**Estimated Time**: 3 hours

**Instructions**:

```cpp
#pragma once

#include "Types.h"
#include "IRequest.h"
#include "IResponse.h"
#include "IHttpFilter.h"
#include "IRetryConfig.h"
#include "IRequestCoordinator.h"
#include "IRateLimiter.h"

namespace modular::fluent {

/// Main interface for the fluent HTTP client.
/// Entry point for creating and executing HTTP requests.
/// Mirrors FluentHttpClient's IClient interface.
///
/// Example usage:
/// @code
/// auto client = createFluentClient("https://api.nexusmods.com");
/// client->setBearerAuth(apiKey);
/// client->setRateLimiter(rateLimiter);
///
/// auto mods = client->getAsync("v1/user/tracked_mods")
///     .withArgument("game_domain", "skyrimspecialedition")
///     .as<std::vector<TrackedMod>>();
/// @endcode
class IFluentClient {
public:
    virtual ~IFluentClient() = default;

    //=========================================================================
    // HTTP Methods - Request Builders
    //=========================================================================

    /// Create a GET request
    /// @param resource The resource path (relative to base URL)
    virtual RequestPtr getAsync(std::string_view resource) = 0;

    /// Create a POST request
    virtual RequestPtr postAsync(std::string_view resource) = 0;

    /// Create a POST request with body
    template<typename T>
    RequestPtr postAsync(std::string_view resource, const T& body) {
        auto request = postAsync(resource);
        request->withJsonBody(body);
        return request;
    }

    /// Create a PUT request
    virtual RequestPtr putAsync(std::string_view resource) = 0;

    /// Create a PUT request with body
    template<typename T>
    RequestPtr putAsync(std::string_view resource, const T& body) {
        auto request = putAsync(resource);
        request->withJsonBody(body);
        return request;
    }

    /// Create a PATCH request
    virtual RequestPtr patchAsync(std::string_view resource) = 0;

    /// Create a DELETE request
    virtual RequestPtr deleteAsync(std::string_view resource) = 0;

    /// Create a HEAD request
    virtual RequestPtr headAsync(std::string_view resource) = 0;

    /// Create a request with custom HTTP method
    virtual RequestPtr sendAsync(HttpMethod method, std::string_view resource) = 0;

    //=========================================================================
    // Client-Level Configuration
    //=========================================================================

    /// Set the base URL for all requests
    virtual IFluentClient& setBaseUrl(std::string_view baseUrl) = 0;

    /// Get the current base URL
    virtual std::string baseUrl() const = 0;

    /// Set default options for all requests
    virtual IFluentClient& setOptions(const RequestOptions& options) = 0;

    /// Get current default options
    virtual const RequestOptions& options() const = 0;

    /// Set the User-Agent header for all requests
    virtual IFluentClient& setUserAgent(std::string_view userAgent) = 0;

    //=========================================================================
    // Authentication
    //=========================================================================

    /// Set authentication for all requests
    /// @param scheme Authentication scheme (e.g., "Bearer", "Basic")
    /// @param parameter Authentication parameter/token
    virtual IFluentClient& setAuthentication(
        std::string_view scheme,
        std::string_view parameter
    ) = 0;

    /// Set Bearer token authentication for all requests
    virtual IFluentClient& setBearerAuth(std::string_view token) = 0;

    /// Set Basic authentication for all requests
    virtual IFluentClient& setBasicAuth(
        std::string_view username,
        std::string_view password
    ) = 0;

    /// Clear authentication
    virtual IFluentClient& clearAuthentication() = 0;

    //=========================================================================
    // Filters
    //=========================================================================

    /// Get the filter collection for this client
    virtual FilterCollection& filters() = 0;
    virtual const FilterCollection& filters() const = 0;

    /// Add a filter (convenience method)
    IFluentClient& addFilter(FilterPtr filter) {
        filters().add(std::move(filter));
        return *this;
    }

    /// Remove all filters of a type (convenience method)
    template<typename T>
    IFluentClient& removeFilters() {
        filters().removeAll<T>();
        return *this;
    }

    //=========================================================================
    // Retry and Coordination
    //=========================================================================

    /// Set the request coordinator (controls retry and dispatch)
    virtual IFluentClient& setRequestCoordinator(CoordinatorPtr coordinator) = 0;

    /// Set up simple retry with parameters
    virtual IFluentClient& setRetryPolicy(
        int maxRetries,
        std::function<bool(int statusCode, bool isTimeout)> shouldRetry,
        std::function<std::chrono::milliseconds(int attempt)> getDelay
    ) = 0;

    /// Set up retry with config objects
    virtual IFluentClient& setRetryPolicy(
        std::vector<std::shared_ptr<IRetryConfig>> configs
    ) = 0;

    /// Disable all retries
    virtual IFluentClient& disableRetries() = 0;

    /// Get the current coordinator (may be null)
    virtual CoordinatorPtr requestCoordinator() const = 0;

    //=========================================================================
    // Rate Limiting (Modular-Specific)
    //=========================================================================

    /// Set the rate limiter for this client
    virtual IFluentClient& setRateLimiter(RateLimiterPtr rateLimiter) = 0;

    /// Get the current rate limiter (may be null)
    virtual RateLimiterPtr rateLimiter() const = 0;

    //=========================================================================
    // Default Request Configuration
    //=========================================================================

    /// Add a default configuration that applies to all requests
    /// @param configure Function that configures each request
    ///
    /// Example:
    /// @code
    /// client->addDefault([](IRequest& req) {
    ///     req.withHeader("X-Client-Version", "1.0");
    /// });
    /// @endcode
    virtual IFluentClient& addDefault(RequestCustomizer configure) = 0;

    /// Clear all default configurations
    virtual IFluentClient& clearDefaults() = 0;

    //=========================================================================
    // Timeout Configuration
    //=========================================================================

    /// Set default connection timeout
    virtual IFluentClient& setConnectionTimeout(std::chrono::seconds timeout) = 0;

    /// Set default request timeout
    virtual IFluentClient& setRequestTimeout(std::chrono::seconds timeout) = 0;

    //=========================================================================
    // Logging Integration
    //=========================================================================

    /// Set logger for this client (optional)
    /// If not set, logging is disabled
    virtual IFluentClient& setLogger(std::shared_ptr<class ILogger> logger) = 0;
};

/// Unique pointer type for client objects
using ClientPtr = std::unique_ptr<IFluentClient>;

//=============================================================================
// Factory Function
//=============================================================================

/// Create a new FluentClient instance
/// @param baseUrl The base URL for all requests
/// @return A new client instance
ClientPtr createFluentClient(std::string_view baseUrl = "");

/// Create a new FluentClient with custom configuration
/// @param baseUrl The base URL
/// @param rateLimiter Optional rate limiter
/// @param logger Optional logger
ClientPtr createFluentClient(
    std::string_view baseUrl,
    RateLimiterPtr rateLimiter,
    std::shared_ptr<class ILogger> logger = nullptr
);

} // namespace modular::fluent
```

**Verification**:
- [ ] All HTTP methods are supported (GET, POST, PUT, PATCH, DELETE, HEAD)
- [ ] Client-level authentication matches FluentHttpClient
- [ ] Filter collection is accessible and modifiable
- [ ] Rate limiter integration is included (Modular requirement)
- [ ] Default configurations can be added
- [ ] Factory functions provide convenient construction
- [ ] Template methods compile correctly

---

## Day 5: Integration and Documentation

### Task 5.1: Create Master Header File
**File**: `include/fluent/Fluent.h`
**Estimated Time**: 30 minutes

**Instructions**:

Create a convenience header that includes all fluent interfaces:

```cpp
#pragma once

/// @file Fluent.h
/// @brief Master include file for the Fluent HTTP Client library
///
/// This file includes all public interfaces for the fluent HTTP client.
/// Include this single header to access the complete API.
///
/// @code
/// #include <fluent/Fluent.h>
/// using namespace modular::fluent;
///
/// auto client = createFluentClient("https://api.example.com");
/// auto result = client->getAsync("users").as<std::vector<User>>();
/// @endcode

#include "Types.h"
#include "Exceptions.h"
#include "IResponse.h"
#include "IBodyBuilder.h"
#include "IRequest.h"
#include "IHttpFilter.h"
#include "IRetryConfig.h"
#include "IRequestCoordinator.h"
#include "IRateLimiter.h"
#include "IFluentClient.h"

namespace modular::fluent {

/// Library version
constexpr const char* VERSION = "1.0.0";

/// Library version as components
constexpr int VERSION_MAJOR = 1;
constexpr int VERSION_MINOR = 0;
constexpr int VERSION_PATCH = 0;

} // namespace modular::fluent
```

---

### Task 5.2: Create CMakeLists.txt for Interface Library
**File**: `include/fluent/CMakeLists.txt`
**Estimated Time**: 30 minutes

**Instructions**:

```cmake
# Fluent HTTP Client Interface Library
# This is a header-only interface library

add_library(fluent_interfaces INTERFACE)
add_library(modular::fluent_interfaces ALIAS fluent_interfaces)

target_include_directories(fluent_interfaces
    INTERFACE
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/..>
        $<INSTALL_INTERFACE:include>
)

target_compile_features(fluent_interfaces INTERFACE cxx_std_17)

# Dependencies
find_package(nlohmann_json REQUIRED)
target_link_libraries(fluent_interfaces INTERFACE nlohmann_json::nlohmann_json)

# Install headers
install(
    DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/
    DESTINATION include/fluent
    FILES_MATCHING PATTERN "*.h"
)

install(
    TARGETS fluent_interfaces
    EXPORT fluent_interfaces-targets
)
```

---

### Task 5.3: Create Interface Documentation
**File**: `docs/fluent/INTERFACES.md`
**Estimated Time**: 2 hours

**Instructions**:

Create documentation explaining:
1. Overview of the interface architecture
2. How interfaces map to FluentHttpClient
3. Modular-specific extensions
4. Usage examples
5. Implementation guidelines for Week 2

```markdown
# Fluent HTTP Client Interfaces

## Architecture Overview

The fluent interfaces provide a modern, chainable API for HTTP operations while
maintaining compatibility with Modular's requirements.

```
┌─────────────────────────────────────────────────────────────────┐
│                        IFluentClient                             │
│  Entry point for creating requests and setting client defaults   │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │ creates
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                          IRequest                                │
│  Fluent builder for configuring individual HTTP requests         │
│  Methods return IRequest& for chaining                          │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │ executes → returns
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                          IResponse                               │
│  Wrapper for HTTP responses with parsing methods                 │
└─────────────────────────────────────────────────────────────────┘

Supporting Interfaces:
┌─────────────┐  ┌──────────────────┐  ┌───────────────┐
│ IHttpFilter │  │ IRequestCoord.   │  │ IRateLimiter  │
│ Middleware  │  │ Retry/Dispatch   │  │ API Limits    │
└─────────────┘  └──────────────────┘  └───────────────┘
```

## Interface Mapping

| FluentHttpClient (C#) | Modular Fluent (C++) | Notes |
|----------------------|---------------------|-------|
| IClient              | IFluentClient       | Main entry point |
| IRequest             | IRequest            | Request builder |
| IResponse            | IResponse           | Response wrapper |
| IHttpFilter          | IHttpFilter         | Middleware |
| IRetryConfig         | IRetryConfig        | Retry policy |
| IRequestCoordinator  | IRequestCoordinator | Dispatch control |
| IBodyBuilder         | IBodyBuilder        | Body construction |
| (none)               | IRateLimiter        | Modular-specific |

## Usage Examples

### Basic GET Request

```cpp
#include <fluent/Fluent.h>
using namespace modular::fluent;

auto client = createFluentClient("https://api.nexusmods.com");
client->setBearerAuth(apiKey);

auto mods = client->getAsync("v1/user/tracked_mods")
    .withArgument("game_domain", "skyrimspecialedition")
    .as<std::vector<TrackedMod>>();
```

### POST with JSON Body

```cpp
NewMod mod{.name = "Test", .version = "1.0"};

auto result = client->postAsync("v1/mods", mod)
    .as<ModResponse>();
```

### Custom Error Handling

```cpp
auto response = client->getAsync("v1/mods/12345")
    .withIgnoreHttpErrors(true)
    .asResponse();

if (!response->isSuccessStatusCode()) {
    auto error = response->asJson();
    std::cerr << "Error: " << error["message"] << std::endl;
}
```

### File Download with Progress

```cpp
client->getAsync("v1/games/skyrim/mods/12345/files/67890/download")
    .downloadTo("/tmp/mod.zip", [](size_t downloaded, size_t total) {
        int percent = total > 0 ? (downloaded * 100 / total) : 0;
        std::cout << "\rDownloading: " << percent << "%" << std::flush;
    });
```

## Implementation Guidelines (Week 2)

When implementing these interfaces:

1. **Thread Safety**: All public methods should be thread-safe
2. **Exception Safety**: Use RAII, don't leak on exceptions
3. **Move Semantics**: Prefer moves over copies for efficiency
4. **Cancellation**: Respect stop_token for long operations
5. **Logging**: Use ILogger when available for debugging
```

---

### Task 5.4: Compile and Validate Interfaces
**Estimated Time**: 2 hours

**Instructions**:

1. Create a test file that includes all headers and instantiates types:

```cpp
// tests/fluent/interface_compile_test.cpp
#include <fluent/Fluent.h>
#include <cassert>

using namespace modular::fluent;

// Verify all types are complete
static_assert(sizeof(RequestOptions) > 0);
static_assert(sizeof(RetryPolicy) > 0);
static_assert(sizeof(RateLimitStatus) > 0);

// Verify enum values
static_assert(to_string(HttpMethod::GET) == "GET");
static_assert(is_success_status(200));
static_assert(!is_success_status(404));

// Verify exception hierarchy
static_assert(std::is_base_of_v<std::exception, FluentException>);
static_assert(std::is_base_of_v<FluentException, ApiException>);
static_assert(std::is_base_of_v<ApiException, RateLimitException>);

int main() {
    // Test exception construction
    try {
        throw ApiException("Test error", 404, "Not Found", {}, "");
    } catch (const std::exception& e) {
        assert(std::string(e.what()) == "Test error");
    }

    // Test FilterCollection
    FilterCollection filters;
    assert(filters.empty());
    assert(filters.size() == 0);

    // Test RequestOptions
    RequestOptions opts;
    assert(!opts.ignoreHttpErrors.has_value());
    opts.ignoreHttpErrors = true;
    assert(opts.ignoreHttpErrors.value() == true);

    return 0;
}
```

2. Add to CMakeLists.txt:

```cmake
add_executable(interface_compile_test tests/fluent/interface_compile_test.cpp)
target_link_libraries(interface_compile_test PRIVATE modular::fluent_interfaces)
add_test(NAME InterfaceCompileTest COMMAND interface_compile_test)
```

3. Run compilation and tests:

```bash
mkdir -p build && cd build
cmake ..
cmake --build . --target interface_compile_test
ctest -R InterfaceCompileTest -V
```

---

## Deliverables Checklist

At the end of Week 1, verify all deliverables:

### Header Files
- [ ] `include/fluent/Types.h` - Common types, enums, aliases
- [ ] `include/fluent/Exceptions.h` - Exception hierarchy
- [ ] `include/fluent/IResponse.h` - Response interface
- [ ] `include/fluent/IHttpFilter.h` - Filter/middleware interface
- [ ] `include/fluent/IRetryConfig.h` - Retry policy interface
- [ ] `include/fluent/IBodyBuilder.h` - Body construction interface
- [ ] `include/fluent/IRequest.h` - Request builder interface
- [ ] `include/fluent/IRequestCoordinator.h` - Request dispatch interface
- [ ] `include/fluent/IRateLimiter.h` - Rate limiter interface (Modular-specific)
- [ ] `include/fluent/IFluentClient.h` - Main client interface
- [ ] `include/fluent/Fluent.h` - Master include file

### Build Files
- [ ] `include/fluent/CMakeLists.txt` - Interface library definition

### Documentation
- [ ] `docs/fluent/INTERFACES.md` - Interface documentation
- [ ] `docs/design/WEEK1_DAY1_NOTES.md` - Design decisions

### Tests
- [ ] `tests/fluent/interface_compile_test.cpp` - Compilation validation

### Quality Checks
- [ ] All headers compile without warnings (`-Wall -Wextra -Wpedantic`)
- [ ] All headers are self-contained (can be included independently)
- [ ] No circular dependencies between headers
- [ ] Doxygen comments on all public interfaces
- [ ] Code follows project coding standards

---

## Definition of Done

Week 1 is complete when:

1. ✅ All interface header files are created and compile cleanly
2. ✅ Exception hierarchy is complete with Modular-specific exceptions
3. ✅ Interfaces match FluentHttpClient's design philosophy
4. ✅ Modular-specific extensions (IRateLimiter, progress callbacks) are included
5. ✅ Documentation explains the architecture and usage
6. ✅ Compilation test passes on all target platforms
7. ✅ Code review completed by team lead
8. ✅ Interfaces committed to feature branch

---

## Notes for Week 2

The implementations in Week 2 will need to:

1. Create concrete classes implementing each interface
2. Integrate with existing Modular `HttpClient` (libcurl-based)
3. Port the `RateLimiter` to implement `IRateLimiter`
4. Create default filters (ErrorFilter, LoggingFilter, RateLimitFilter)
5. Wire up the fluent API to the existing HTTP infrastructure

The interfaces defined this week provide the contract; Week 2 provides the implementation.
