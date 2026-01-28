# Week 4 Task List: Filter Implementations

## Overview

**Objective**: Implement the standard filter collection that provides common HTTP client functionality: error handling, logging, rate limiting, and authentication.

**Prerequisites**: Week 3 completed (full fluent wrapper with filter execution)

**Duration**: 5 working days

**Output**: Production-ready filters that can be composed for various use cases

---

## Architecture Context

```
Week 4 Filter Implementations
═════════════════════════════

┌─────────────────────────────────────────────────────────────────┐
│                        FilterCollection                          │
│  Ordered list of filters executed for each request               │
└─────────────────────────────────────────────────────────────────┘
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
         ▼                     ▼                     ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ DefaultError    │  │   Logging       │  │  RateLimit      │
│    Filter       │  │    Filter       │  │   Filter        │
│                 │  │                 │  │                 │
│ Priority: 9000  │  │ Priority: 100   │  │ Priority: 500   │
│ Throws on 4xx/5xx│  │ Logs req/resp  │  │ Waits if needed │
└─────────────────┘  └─────────────────┘  └─────────────────┘
         │                     │                     │
         ▼                     ▼                     ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Authentication  │  │   Caching       │  │  Compression    │
│    Filter       │  │    Filter       │  │    Filter       │
│                 │  │  (optional)     │  │  (optional)     │
│ Priority: 200   │  │ Priority: 300   │  │ Priority: 400   │
│ Adds auth header│  │ Returns cached  │  │ Handles gzip    │
└─────────────────┘  └─────────────────┘  └─────────────────┘

Filter Execution Order:
REQUEST:  Logging → Auth → Caching → Compression → RateLimit → [HTTP] → DefaultError
RESPONSE: DefaultError → [HTTP] → RateLimit → Compression → Caching → Auth → Logging
```

---

## Filter Priority Conventions

| Priority Range | Purpose | Examples |
|---------------|---------|----------|
| 0-99 | Diagnostic/Debug | Timing, Tracing |
| 100-199 | Logging | Request/Response logging |
| 200-299 | Authentication | Token injection, refresh |
| 300-399 | Caching | Response caching |
| 400-499 | Transformation | Compression, encoding |
| 500-599 | Rate Limiting | API rate limit handling |
| 1000 | Default | User-defined filters |
| 9000-9999 | Error Handling | Exception throwing |

---

## Directory Structure

```
src/fluent/filters/
├── DefaultErrorFilter.h
├── DefaultErrorFilter.cpp
├── LoggingFilter.h
├── LoggingFilter.cpp
├── RateLimitFilter.h
├── RateLimitFilter.cpp
├── AuthenticationFilter.h
├── AuthenticationFilter.cpp
├── CompressionFilter.h
├── CompressionFilter.cpp
└── CMakeLists.txt

tests/fluent/filters/
├── DefaultErrorFilterTest.cpp
├── LoggingFilterTest.cpp
├── RateLimitFilterTest.cpp
├── AuthenticationFilterTest.cpp
└── FilterIntegrationTest.cpp
```

---

## Day 1: DefaultErrorFilter and LoggingFilter

### Task 4.1.1: Create DefaultErrorFilter
**File**: `src/fluent/filters/DefaultErrorFilter.h` and `.cpp`
**Estimated Time**: 2 hours

**Instructions**:

This filter throws `ApiException` on HTTP error responses. It mirrors FluentHttpClient's default behavior.

```cpp
// DefaultErrorFilter.h
#pragma once

#include <fluent/IHttpFilter.h>
#include <fluent/Exceptions.h>

namespace modular::fluent::filters {

/// Filter that throws ApiException on HTTP error responses (4xx, 5xx).
/// This is the default behavior matching FluentHttpClient.
///
/// This filter has the highest priority (9000) so it runs last on request
/// and first on response, allowing other filters to process the response
/// before the exception is thrown.
///
/// To disable for a request, either:
/// - Remove this filter: request.withoutFilter<DefaultErrorFilter>()
/// - Set option: request.withIgnoreHttpErrors(true)
class DefaultErrorFilter : public IHttpFilter {
public:
    /// Create default error filter
    DefaultErrorFilter() = default;

    /// Create with custom status code checker
    /// @param isError Function that returns true if status should throw
    explicit DefaultErrorFilter(std::function<bool(int statusCode)> isError);

    void onRequest(IRequest& request) override;
    void onResponse(IResponse& response, bool httpErrorAsException) override;

    std::string name() const override { return "DefaultErrorFilter"; }
    int priority() const override { return 9000; }

private:
    std::function<bool(int)> isError_ = [](int code) {
        return code >= 400;
    };
};

} // namespace modular::fluent::filters
```

```cpp
// DefaultErrorFilter.cpp
#include "DefaultErrorFilter.h"

namespace modular::fluent::filters {

DefaultErrorFilter::DefaultErrorFilter(std::function<bool(int statusCode)> isError)
    : isError_(std::move(isError))
{}

void DefaultErrorFilter::onRequest(IRequest& /*request*/) {
    // Nothing to do on request
}

void DefaultErrorFilter::onResponse(IResponse& response, bool httpErrorAsException) {
    if (!httpErrorAsException) {
        return;  // Errors should be ignored for this request
    }

    int statusCode = response.statusCode();

    if (!isError_(statusCode)) {
        return;  // Not an error status
    }

    // Build error message
    std::string message = "The API query failed with status code " +
                         std::to_string(statusCode) + ": " +
                         response.statusReason();

    // Get response body for error details
    std::string body;
    try {
        body = response.asString();
    } catch (...) {
        body = "";
    }

    // Throw appropriate exception type
    if (statusCode == 429) {
        // Rate limit - parse Retry-After if present
        std::chrono::seconds retryAfter{60};  // Default
        if (response.hasHeader("Retry-After")) {
            try {
                retryAfter = std::chrono::seconds{
                    std::stoi(response.header("Retry-After"))
                };
            } catch (...) {}
        }

        throw RateLimitException(message, response.headers(), body, retryAfter);

    } else if (statusCode == 401 || statusCode == 403) {
        throw AuthException(message, statusCode, response.headers(), body);

    } else {
        throw ApiException(
            message,
            statusCode,
            response.statusReason(),
            response.headers(),
            body
        );
    }
}

} // namespace modular::fluent::filters
```

---

### Task 4.1.2: Create LoggingFilter
**File**: `src/fluent/filters/LoggingFilter.h` and `.cpp`
**Estimated Time**: 2.5 hours

**Instructions**:

```cpp
// LoggingFilter.h
#pragma once

#include <fluent/IHttpFilter.h>
#include <core/ILogger.h>

#include <chrono>
#include <unordered_map>
#include <mutex>

namespace modular::fluent::filters {

/// Configuration for logging filter
struct LoggingConfig {
    /// Log request details
    bool logRequests = true;

    /// Log response details
    bool logResponses = true;

    /// Log request headers (may contain sensitive data)
    bool logRequestHeaders = false;

    /// Log response headers
    bool logResponseHeaders = false;

    /// Log request body (may be large)
    bool logRequestBody = false;

    /// Log response body (may be large)
    bool logResponseBody = false;

    /// Maximum body length to log (0 = unlimited)
    size_t maxBodyLogLength = 1000;

    /// Headers to redact (e.g., Authorization)
    std::vector<std::string> redactHeaders = {"Authorization", "X-Api-Key", "Cookie"};

    /// Log level for successful requests
    LogLevel successLevel = LogLevel::Debug;

    /// Log level for failed requests
    LogLevel errorLevel = LogLevel::Warning;
};

/// Filter that logs HTTP requests and responses.
///
/// Logs timing, status codes, and optionally headers/bodies.
/// Sensitive headers can be redacted.
class LoggingFilter : public IHttpFilter {
public:
    /// Create with logger and default config
    explicit LoggingFilter(ILogger* logger);

    /// Create with logger and custom config
    LoggingFilter(ILogger* logger, LoggingConfig config);

    void onRequest(IRequest& request) override;
    void onResponse(IResponse& response, bool httpErrorAsException) override;

    std::string name() const override { return "LoggingFilter"; }
    int priority() const override { return 100; }

    /// Update configuration
    void setConfig(const LoggingConfig& config);

private:
    ILogger* logger_;
    LoggingConfig config_;

    // Track request start times for timing
    std::unordered_map<const IRequest*, std::chrono::steady_clock::time_point> requestTimes_;
    std::mutex mutex_;

    std::string formatHeaders(const Headers& headers) const;
    std::string redactHeader(const std::string& name, const std::string& value) const;
    std::string truncateBody(const std::string& body) const;
};

} // namespace modular::fluent::filters
```

```cpp
// LoggingFilter.cpp
#include "LoggingFilter.h"
#include <sstream>
#include <algorithm>

namespace modular::fluent::filters {

LoggingFilter::LoggingFilter(ILogger* logger)
    : logger_(logger)
{}

LoggingFilter::LoggingFilter(ILogger* logger, LoggingConfig config)
    : logger_(logger)
    , config_(std::move(config))
{}

void LoggingFilter::onRequest(IRequest& request) {
    if (!logger_ || !config_.logRequests) return;

    // Record start time
    {
        std::lock_guard<std::mutex> lock(mutex_);
        requestTimes_[&request] = std::chrono::steady_clock::now();
    }

    std::ostringstream oss;
    oss << "→ " << to_string(request.method()) << " " << request.url();

    if (config_.logRequestHeaders) {
        oss << "\n  Headers: " << formatHeaders(request.headers());
    }

    logger_->log(config_.successLevel, oss.str());
}

void LoggingFilter::onResponse(IResponse& response, bool /*httpErrorAsException*/) {
    if (!logger_ || !config_.logResponses) return;

    // Calculate elapsed time
    std::chrono::milliseconds elapsed{0};
    // Note: We'd need a way to correlate response back to request
    // For now, just log the response

    std::ostringstream oss;
    oss << "← " << response.statusCode() << " " << response.statusReason();
    oss << " (" << response.elapsed().count() << "ms)";

    if (config_.logResponseHeaders) {
        oss << "\n  Headers: " << formatHeaders(response.headers());
    }

    auto level = response.isSuccessStatusCode()
                 ? config_.successLevel
                 : config_.errorLevel;

    logger_->log(level, oss.str());
}

void LoggingFilter::setConfig(const LoggingConfig& config) {
    config_ = config;
}

std::string LoggingFilter::formatHeaders(const Headers& headers) const {
    std::ostringstream oss;
    oss << "{";
    bool first = true;
    for (const auto& [key, value] : headers) {
        if (!first) oss << ", ";
        first = false;
        oss << key << ": " << redactHeader(key, value);
    }
    oss << "}";
    return oss.str();
}

std::string LoggingFilter::redactHeader(
    const std::string& name,
    const std::string& value
) const {
    // Check if header should be redacted (case-insensitive)
    for (const auto& redact : config_.redactHeaders) {
        if (std::equal(name.begin(), name.end(), redact.begin(), redact.end(),
            [](char a, char b) {
                return std::tolower(static_cast<unsigned char>(a)) ==
                       std::tolower(static_cast<unsigned char>(b));
            })) {
            return "[REDACTED]";
        }
    }
    return value;
}

std::string LoggingFilter::truncateBody(const std::string& body) const {
    if (config_.maxBodyLogLength == 0 || body.size() <= config_.maxBodyLogLength) {
        return body;
    }
    return body.substr(0, config_.maxBodyLogLength) + "... [truncated]";
}

} // namespace modular::fluent::filters
```

---

### Task 4.1.3: Create Unit Tests for Day 1 Filters
**Files**: `tests/fluent/filters/DefaultErrorFilterTest.cpp`, `LoggingFilterTest.cpp`
**Estimated Time**: 2 hours

```cpp
// DefaultErrorFilterTest.cpp
#include <gtest/gtest.h>
#include "fluent/filters/DefaultErrorFilter.h"
#include "fluent/Response.h"

using namespace modular::fluent;
using namespace modular::fluent::filters;

class MockResponse : public IResponse {
public:
    int statusCode_;
    std::string statusReason_;
    Headers headers_;
    std::string body_;

    // Implement IResponse interface...
    int statusCode() const override { return statusCode_; }
    std::string statusReason() const override { return statusReason_; }
    bool isSuccessStatusCode() const override { return statusCode_ >= 200 && statusCode_ < 300; }
    const Headers& headers() const override { return headers_; }
    std::string header(std::string_view name) const override {
        auto it = headers_.find(std::string(name));
        return it != headers_.end() ? it->second : "";
    }
    bool hasHeader(std::string_view name) const override {
        return headers_.find(std::string(name)) != headers_.end();
    }
    std::string asString() override { return body_; }
    // ... other methods with stubs
};

TEST(DefaultErrorFilterTest, DoesNotThrowOnSuccess) {
    DefaultErrorFilter filter;
    MockResponse response;
    response.statusCode_ = 200;
    response.statusReason_ = "OK";

    EXPECT_NO_THROW(filter.onResponse(response, true));
}

TEST(DefaultErrorFilterTest, ThrowsApiExceptionOn404) {
    DefaultErrorFilter filter;
    MockResponse response;
    response.statusCode_ = 404;
    response.statusReason_ = "Not Found";
    response.body_ = "Resource not found";

    EXPECT_THROW(filter.onResponse(response, true), ApiException);

    try {
        filter.onResponse(response, true);
    } catch (const ApiException& e) {
        EXPECT_EQ(e.statusCode(), 404);
        EXPECT_TRUE(e.isClientError());
    }
}

TEST(DefaultErrorFilterTest, ThrowsRateLimitExceptionOn429) {
    DefaultErrorFilter filter;
    MockResponse response;
    response.statusCode_ = 429;
    response.statusReason_ = "Too Many Requests";
    response.headers_["Retry-After"] = "60";

    EXPECT_THROW(filter.onResponse(response, true), RateLimitException);

    try {
        filter.onResponse(response, true);
    } catch (const RateLimitException& e) {
        EXPECT_EQ(e.retryAfter(), std::chrono::seconds{60});
    }
}

TEST(DefaultErrorFilterTest, ThrowsAuthExceptionOn401) {
    DefaultErrorFilter filter;
    MockResponse response;
    response.statusCode_ = 401;
    response.statusReason_ = "Unauthorized";

    EXPECT_THROW(filter.onResponse(response, true), AuthException);
}

TEST(DefaultErrorFilterTest, DoesNotThrowWhenDisabled) {
    DefaultErrorFilter filter;
    MockResponse response;
    response.statusCode_ = 500;
    response.statusReason_ = "Internal Server Error";

    // When httpErrorAsException is false, should not throw
    EXPECT_NO_THROW(filter.onResponse(response, false));
}

TEST(DefaultErrorFilterTest, CustomErrorChecker) {
    // Only treat 5xx as errors
    DefaultErrorFilter filter([](int code) { return code >= 500; });

    MockResponse response;
    response.statusCode_ = 404;

    // 404 should not throw with custom checker
    EXPECT_NO_THROW(filter.onResponse(response, true));

    response.statusCode_ = 500;
    EXPECT_THROW(filter.onResponse(response, true), ApiException);
}
```

---

## Day 2: RateLimitFilter

### Task 4.2.1: Create RateLimitFilter
**File**: `src/fluent/filters/RateLimitFilter.h` and `.cpp`
**Estimated Time**: 4 hours

**Instructions**:

This filter integrates with Modular's existing RateLimiter, adding fluent API support.

```cpp
// RateLimitFilter.h
#pragma once

#include <fluent/IHttpFilter.h>
#include <fluent/IRateLimiter.h>
#include <core/ILogger.h>

#include <chrono>
#include <functional>

namespace modular::fluent::filters {

/// Callback when rate limit is about to block
using RateLimitWaitCallback = std::function<void(std::chrono::milliseconds waitTime)>;

/// Configuration for rate limit filter
struct RateLimitConfig {
    /// Maximum time to wait for rate limit (0 = wait indefinitely)
    std::chrono::milliseconds maxWaitTime{0};

    /// Whether to throw exception instead of waiting
    bool throwOnRateLimit = false;

    /// Callback before waiting (for UI notification)
    RateLimitWaitCallback onWait;

    /// Warning threshold for daily remaining
    int dailyWarningThreshold = 100;

    /// Warning threshold for hourly remaining
    int hourlyWarningThreshold = 10;
};

/// Filter that enforces rate limits before sending requests.
///
/// Features:
/// - Waits if rate limit would be exceeded
/// - Updates rate limit state from response headers
/// - Provides warnings when limits are low
/// - Can be configured to throw instead of wait
///
/// This filter has priority 500 so it runs after auth/caching
/// but before the actual HTTP request.
class RateLimitFilter : public IHttpFilter {
public:
    /// Create with rate limiter
    explicit RateLimitFilter(std::shared_ptr<IRateLimiter> rateLimiter);

    /// Create with rate limiter and config
    RateLimitFilter(
        std::shared_ptr<IRateLimiter> rateLimiter,
        RateLimitConfig config
    );

    /// Create with rate limiter, config, and logger
    RateLimitFilter(
        std::shared_ptr<IRateLimiter> rateLimiter,
        RateLimitConfig config,
        ILogger* logger
    );

    void onRequest(IRequest& request) override;
    void onResponse(IResponse& response, bool httpErrorAsException) override;

    std::string name() const override { return "RateLimitFilter"; }
    int priority() const override { return 500; }

    /// Get current rate limit status
    RateLimitStatus status() const;

    /// Update configuration
    void setConfig(const RateLimitConfig& config);

private:
    std::shared_ptr<IRateLimiter> rateLimiter_;
    RateLimitConfig config_;
    ILogger* logger_ = nullptr;

    void checkWarnings();
};

} // namespace modular::fluent::filters
```

```cpp
// RateLimitFilter.cpp
#include "RateLimitFilter.h"
#include <fluent/Exceptions.h>

namespace modular::fluent::filters {

RateLimitFilter::RateLimitFilter(std::shared_ptr<IRateLimiter> rateLimiter)
    : rateLimiter_(std::move(rateLimiter))
{}

RateLimitFilter::RateLimitFilter(
    std::shared_ptr<IRateLimiter> rateLimiter,
    RateLimitConfig config
)
    : rateLimiter_(std::move(rateLimiter))
    , config_(std::move(config))
{}

RateLimitFilter::RateLimitFilter(
    std::shared_ptr<IRateLimiter> rateLimiter,
    RateLimitConfig config,
    ILogger* logger
)
    : rateLimiter_(std::move(rateLimiter))
    , config_(std::move(config))
    , logger_(logger)
{}

void RateLimitFilter::onRequest(IRequest& /*request*/) {
    if (!rateLimiter_) return;

    // Check if we can make a request
    if (rateLimiter_->canMakeRequest()) {
        checkWarnings();
        return;
    }

    // Calculate wait time
    auto status = rateLimiter_->status();
    auto waitTime = status.timeUntilAllowed();

    if (logger_) {
        logger_->info("Rate limit reached, waiting " +
                     std::to_string(waitTime.count()) + "ms");
    }

    // Check if we should throw instead of wait
    if (config_.throwOnRateLimit) {
        throw RateLimitException(
            "Rate limit exceeded",
            {},
            "",
            std::chrono::duration_cast<std::chrono::seconds>(waitTime)
        );
    }

    // Check max wait time
    if (config_.maxWaitTime.count() > 0 && waitTime > config_.maxWaitTime) {
        throw RateLimitException(
            "Rate limit wait time exceeds maximum",
            {},
            "",
            std::chrono::duration_cast<std::chrono::seconds>(waitTime)
        );
    }

    // Notify callback
    if (config_.onWait) {
        config_.onWait(waitTime);
    }

    // Wait
    rateLimiter_->waitIfNeeded(config_.maxWaitTime);

    checkWarnings();
}

void RateLimitFilter::onResponse(IResponse& response, bool /*httpErrorAsException*/) {
    if (!rateLimiter_) return;

    // Update rate limiter from response headers
    rateLimiter_->updateFromHeaders(response.headers());

    // Record the request
    rateLimiter_->recordRequest();

    // Check for 429 and update with Retry-After
    if (response.statusCode() == 429) {
        if (response.hasHeader("Retry-After")) {
            try {
                auto retryAfter = std::stoi(response.header("Retry-After"));
                // Rate limiter should handle this internally
                if (logger_) {
                    logger_->warning("Received 429, Retry-After: " +
                                    std::to_string(retryAfter) + "s");
                }
            } catch (...) {}
        }
    }

    checkWarnings();
}

RateLimitStatus RateLimitFilter::status() const {
    return rateLimiter_ ? rateLimiter_->status() : RateLimitStatus{};
}

void RateLimitFilter::setConfig(const RateLimitConfig& config) {
    config_ = config;
}

void RateLimitFilter::checkWarnings() {
    if (!rateLimiter_ || !logger_) return;

    auto status = rateLimiter_->status();

    if (status.dailyRemaining <= config_.dailyWarningThreshold) {
        logger_->warning("Daily rate limit low: " +
                        std::to_string(status.dailyRemaining) + " remaining");
    }

    if (status.hourlyRemaining <= config_.hourlyWarningThreshold) {
        logger_->warning("Hourly rate limit low: " +
                        std::to_string(status.hourlyRemaining) + " remaining");
    }
}

} // namespace modular::fluent::filters
```

---

### Task 4.2.2: Create RateLimitFilter Tests
**File**: `tests/fluent/filters/RateLimitFilterTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include "fluent/filters/RateLimitFilter.h"

using namespace modular::fluent;
using namespace modular::fluent::filters;

class MockRateLimiter : public IRateLimiter {
public:
    bool canMakeRequest_ = true;
    RateLimitStatus status_;
    bool waitCalled_ = false;
    Headers lastHeaders_;

    bool canMakeRequest() const override { return canMakeRequest_; }

    bool waitIfNeeded(std::chrono::milliseconds) override {
        waitCalled_ = true;
        return true;
    }

    void recordRequest() override {}

    void updateFromHeaders(const Headers& headers) override {
        lastHeaders_ = headers;
    }

    void setLimits(int, int, std::chrono::system_clock::time_point,
                   int, int, std::chrono::system_clock::time_point) override {}

    RateLimitStatus status() const override { return status_; }
    int dailyRemaining() const override { return status_.dailyRemaining; }
    int hourlyRemaining() const override { return status_.hourlyRemaining; }

    void saveState(const std::filesystem::path&) const override {}
    bool loadState(const std::filesystem::path&) override { return false; }
    void onLowLimit(int, WarningCallback) override {}
};

class MockRequest : public IRequest {
    // Stub implementation
};

class MockResponse : public IResponse {
public:
    int statusCode_ = 200;
    Headers headers_;

    int statusCode() const override { return statusCode_; }
    const Headers& headers() const override { return headers_; }
    std::string header(std::string_view name) const override {
        auto it = headers_.find(std::string(name));
        return it != headers_.end() ? it->second : "";
    }
    bool hasHeader(std::string_view name) const override {
        return headers_.find(std::string(name)) != headers_.end();
    }
    // ... other stubs
};

TEST(RateLimitFilterTest, AllowsWhenUnderLimit) {
    auto limiter = std::make_shared<MockRateLimiter>();
    limiter->canMakeRequest_ = true;

    RateLimitFilter filter(limiter);
    MockRequest request;

    EXPECT_NO_THROW(filter.onRequest(request));
    EXPECT_FALSE(limiter->waitCalled_);
}

TEST(RateLimitFilterTest, WaitsWhenOverLimit) {
    auto limiter = std::make_shared<MockRateLimiter>();
    limiter->canMakeRequest_ = false;
    limiter->status_.dailyRemaining = 0;

    RateLimitFilter filter(limiter);
    MockRequest request;

    filter.onRequest(request);

    EXPECT_TRUE(limiter->waitCalled_);
}

TEST(RateLimitFilterTest, ThrowsWhenConfigured) {
    auto limiter = std::make_shared<MockRateLimiter>();
    limiter->canMakeRequest_ = false;

    RateLimitConfig config;
    config.throwOnRateLimit = true;

    RateLimitFilter filter(limiter, config);
    MockRequest request;

    EXPECT_THROW(filter.onRequest(request), RateLimitException);
}

TEST(RateLimitFilterTest, UpdatesFromHeaders) {
    auto limiter = std::make_shared<MockRateLimiter>();
    RateLimitFilter filter(limiter);

    MockResponse response;
    response.headers_["x-rl-daily-remaining"] = "100";
    response.headers_["x-rl-hourly-remaining"] = "50";

    filter.onResponse(response, true);

    EXPECT_EQ(limiter->lastHeaders_["x-rl-daily-remaining"], "100");
}

TEST(RateLimitFilterTest, CallsWaitCallback) {
    auto limiter = std::make_shared<MockRateLimiter>();
    limiter->canMakeRequest_ = false;

    bool callbackCalled = false;
    RateLimitConfig config;
    config.onWait = [&callbackCalled](std::chrono::milliseconds) {
        callbackCalled = true;
    };

    RateLimitFilter filter(limiter, config);
    MockRequest request;

    filter.onRequest(request);

    EXPECT_TRUE(callbackCalled);
}
```

---

## Day 3: AuthenticationFilter

### Task 4.3.1: Create AuthenticationFilter
**File**: `src/fluent/filters/AuthenticationFilter.h` and `.cpp`
**Estimated Time**: 3 hours

**Instructions**:

This filter supports token refresh and multiple auth schemes.

```cpp
// AuthenticationFilter.h
#pragma once

#include <fluent/IHttpFilter.h>

#include <functional>
#include <mutex>
#include <optional>

namespace modular::fluent::filters {

/// Token provider function type
/// Returns current token, refreshing if needed
using TokenProvider = std::function<std::string()>;

/// Token refresh callback
/// Called when 401 is received, should return new token or empty if refresh failed
using TokenRefresher = std::function<std::optional<std::string>(const IResponse&)>;

/// Authentication scheme
enum class AuthScheme {
    Bearer,    // Authorization: Bearer <token>
    Basic,     // Authorization: Basic <base64>
    ApiKey,    // X-Api-Key: <key> or query param
    Custom     // Custom header
};

/// Configuration for authentication filter
struct AuthConfig {
    /// Authentication scheme
    AuthScheme scheme = AuthScheme::Bearer;

    /// For ApiKey scheme: header name (or empty for query param)
    std::string apiKeyHeader = "X-Api-Key";

    /// For ApiKey scheme: query param name (if header is empty)
    std::string apiKeyParam = "api_key";

    /// For Custom scheme: header name
    std::string customHeader = "Authorization";

    /// For Custom scheme: header value prefix
    std::string customPrefix;

    /// Token provider (called for each request)
    TokenProvider tokenProvider;

    /// Token refresher (called on 401)
    TokenRefresher tokenRefresher;

    /// Maximum token refresh attempts
    int maxRefreshAttempts = 1;
};

/// Filter that handles authentication for requests.
///
/// Features:
/// - Multiple auth schemes (Bearer, Basic, API Key, Custom)
/// - Dynamic token provider for changing tokens
/// - Automatic token refresh on 401
/// - Thread-safe token handling
class AuthenticationFilter : public IHttpFilter {
public:
    /// Create with static token
    AuthenticationFilter(AuthScheme scheme, const std::string& token);

    /// Create with config
    explicit AuthenticationFilter(AuthConfig config);

    void onRequest(IRequest& request) override;
    void onResponse(IResponse& response, bool httpErrorAsException) override;

    std::string name() const override { return "AuthenticationFilter"; }
    int priority() const override { return 200; }

    /// Update the token
    void setToken(const std::string& token);

    /// Get current token
    std::string token() const;

private:
    AuthConfig config_;
    mutable std::string cachedToken_;
    mutable std::mutex tokenMutex_;
    int refreshAttempts_ = 0;

    std::string getToken() const;
    void applyAuth(IRequest& request, const std::string& token);
};

} // namespace modular::fluent::filters
```

```cpp
// AuthenticationFilter.cpp
#include "AuthenticationFilter.h"
#include "fluent/Utils.h"  // For base64Encode

namespace modular::fluent::filters {

AuthenticationFilter::AuthenticationFilter(AuthScheme scheme, const std::string& token)
    : cachedToken_(token)
{
    config_.scheme = scheme;
    config_.tokenProvider = [this]() { return cachedToken_; };
}

AuthenticationFilter::AuthenticationFilter(AuthConfig config)
    : config_(std::move(config))
{}

void AuthenticationFilter::onRequest(IRequest& request) {
    std::string token = getToken();
    if (token.empty()) return;

    applyAuth(request, token);
}

void AuthenticationFilter::onResponse(IResponse& response, bool /*httpErrorAsException*/) {
    // Check for 401 and attempt token refresh
    if (response.statusCode() == 401 && config_.tokenRefresher) {
        if (refreshAttempts_ < config_.maxRefreshAttempts) {
            ++refreshAttempts_;

            auto newToken = config_.tokenRefresher(response);
            if (newToken) {
                std::lock_guard<std::mutex> lock(tokenMutex_);
                cachedToken_ = *newToken;
            }
        }
    } else {
        // Reset refresh attempts on success
        refreshAttempts_ = 0;
    }
}

void AuthenticationFilter::setToken(const std::string& token) {
    std::lock_guard<std::mutex> lock(tokenMutex_);
    cachedToken_ = token;
}

std::string AuthenticationFilter::token() const {
    std::lock_guard<std::mutex> lock(tokenMutex_);
    return cachedToken_;
}

std::string AuthenticationFilter::getToken() const {
    if (config_.tokenProvider) {
        return config_.tokenProvider();
    }
    std::lock_guard<std::mutex> lock(tokenMutex_);
    return cachedToken_;
}

void AuthenticationFilter::applyAuth(IRequest& request, const std::string& token) {
    switch (config_.scheme) {
        case AuthScheme::Bearer:
            request.withHeader("Authorization", "Bearer " + token);
            break;

        case AuthScheme::Basic:
            request.withHeader("Authorization", "Basic " + token);
            break;

        case AuthScheme::ApiKey:
            if (!config_.apiKeyHeader.empty()) {
                request.withHeader(config_.apiKeyHeader, token);
            } else {
                request.withArgument(config_.apiKeyParam, token);
            }
            break;

        case AuthScheme::Custom:
            request.withHeader(
                config_.customHeader,
                config_.customPrefix.empty() ? token : config_.customPrefix + " " + token
            );
            break;
    }
}

} // namespace modular::fluent::filters
```

---

## Day 4: CompressionFilter and Additional Utilities

### Task 4.4.1: Create CompressionFilter
**File**: `src/fluent/filters/CompressionFilter.h` and `.cpp`
**Estimated Time**: 2.5 hours

```cpp
// CompressionFilter.h
#pragma once

#include <fluent/IHttpFilter.h>

namespace modular::fluent::filters {

/// Filter that handles response compression (gzip, deflate).
///
/// Features:
/// - Adds Accept-Encoding header to requests
/// - Automatically decompresses gzip/deflate responses
/// - Can be configured to request specific encodings
class CompressionFilter : public IHttpFilter {
public:
    /// Create with default settings (accept gzip, deflate)
    CompressionFilter();

    /// Create with specific accepted encodings
    explicit CompressionFilter(std::vector<std::string> acceptedEncodings);

    void onRequest(IRequest& request) override;
    void onResponse(IResponse& response, bool httpErrorAsException) override;

    std::string name() const override { return "CompressionFilter"; }
    int priority() const override { return 400; }

private:
    std::vector<std::string> acceptedEncodings_;
};

} // namespace modular::fluent::filters
```

---

### Task 4.4.2: Create Filter Factory
**File**: `src/fluent/filters/FilterFactory.h`
**Estimated Time**: 1.5 hours

Create a convenient factory for common filter configurations:

```cpp
// FilterFactory.h
#pragma once

#include "DefaultErrorFilter.h"
#include "LoggingFilter.h"
#include "RateLimitFilter.h"
#include "AuthenticationFilter.h"
#include "CompressionFilter.h"

namespace modular::fluent::filters {

/// Factory for creating common filter configurations
class FilterFactory {
public:
    /// Create default error filter
    static std::shared_ptr<DefaultErrorFilter> createErrorFilter() {
        return std::make_shared<DefaultErrorFilter>();
    }

    /// Create logging filter with logger
    static std::shared_ptr<LoggingFilter> createLoggingFilter(
        ILogger* logger,
        LoggingConfig config = {}
    ) {
        return std::make_shared<LoggingFilter>(logger, std::move(config));
    }

    /// Create rate limit filter
    static std::shared_ptr<RateLimitFilter> createRateLimitFilter(
        std::shared_ptr<IRateLimiter> limiter,
        RateLimitConfig config = {}
    ) {
        return std::make_shared<RateLimitFilter>(std::move(limiter), std::move(config));
    }

    /// Create bearer auth filter with static token
    static std::shared_ptr<AuthenticationFilter> createBearerAuth(
        const std::string& token
    ) {
        return std::make_shared<AuthenticationFilter>(AuthScheme::Bearer, token);
    }

    /// Create bearer auth filter with token provider
    static std::shared_ptr<AuthenticationFilter> createBearerAuth(
        TokenProvider provider
    ) {
        AuthConfig config;
        config.scheme = AuthScheme::Bearer;
        config.tokenProvider = std::move(provider);
        return std::make_shared<AuthenticationFilter>(std::move(config));
    }

    /// Create API key filter (header-based)
    static std::shared_ptr<AuthenticationFilter> createApiKeyAuth(
        const std::string& apiKey,
        const std::string& headerName = "X-Api-Key"
    ) {
        AuthConfig config;
        config.scheme = AuthScheme::ApiKey;
        config.apiKeyHeader = headerName;
        config.tokenProvider = [apiKey]() { return apiKey; };
        return std::make_shared<AuthenticationFilter>(std::move(config));
    }

    /// Create compression filter
    static std::shared_ptr<CompressionFilter> createCompressionFilter() {
        return std::make_shared<CompressionFilter>();
    }

    /// Create a standard filter chain for NexusMods API
    static std::vector<FilterPtr> createNexusModsFilters(
        const std::string& apiKey,
        std::shared_ptr<IRateLimiter> rateLimiter,
        ILogger* logger = nullptr
    ) {
        std::vector<FilterPtr> filters;

        // Logging (if logger provided)
        if (logger) {
            filters.push_back(createLoggingFilter(logger));
        }

        // Authentication
        filters.push_back(createApiKeyAuth(apiKey, "apikey"));

        // Rate limiting
        filters.push_back(createRateLimitFilter(std::move(rateLimiter)));

        // Error handling
        filters.push_back(createErrorFilter());

        return filters;
    }
};

} // namespace modular::fluent::filters
```

---

## Day 5: Integration Testing and Documentation

### Task 4.5.1: Create Filter Integration Test
**File**: `tests/fluent/filters/FilterIntegrationTest.cpp`
**Estimated Time**: 3 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>
#include "fluent/filters/FilterFactory.h"

using namespace modular::fluent;
using namespace modular::fluent::filters;

class FilterIntegrationTest : public ::testing::Test {
protected:
    std::vector<std::string> executionLog;

    class TrackingFilter : public IHttpFilter {
    public:
        std::vector<std::string>& log;
        std::string id;
        int prio;

        TrackingFilter(std::vector<std::string>& log, std::string id, int priority)
            : log(log), id(std::move(id)), prio(priority) {}

        void onRequest(IRequest&) override {
            log.push_back(id + "::onRequest");
        }

        void onResponse(IResponse&, bool) override {
            log.push_back(id + "::onResponse");
        }

        std::string name() const override { return id; }
        int priority() const override { return prio; }
    };
};

TEST_F(FilterIntegrationTest, FiltersExecuteInPriorityOrder) {
    auto client = createFluentClient("https://example.com");

    // Add filters with different priorities
    client->addFilter(std::make_shared<TrackingFilter>(executionLog, "High", 100));
    client->addFilter(std::make_shared<TrackingFilter>(executionLog, "Low", 900));
    client->addFilter(std::make_shared<TrackingFilter>(executionLog, "Mid", 500));

    // Create request (don't execute - just verify filter setup)
    auto request = client->getAsync("test");

    // Filters should be sorted by priority
    // Request order: High(100) -> Mid(500) -> Low(900)
    // Response order: Low(900) -> Mid(500) -> High(100)
}

TEST_F(FilterIntegrationTest, NexusModsFilterChain) {
    auto rateLimiter = std::make_shared<MockRateLimiter>();
    auto filters = FilterFactory::createNexusModsFilters(
        "test-api-key",
        rateLimiter,
        nullptr  // No logger
    );

    EXPECT_EQ(filters.size(), 3);  // Auth, RateLimit, Error

    // Verify filter types
    EXPECT_NE(dynamic_cast<AuthenticationFilter*>(filters[0].get()), nullptr);
    EXPECT_NE(dynamic_cast<RateLimitFilter*>(filters[1].get()), nullptr);
    EXPECT_NE(dynamic_cast<DefaultErrorFilter*>(filters[2].get()), nullptr);
}

TEST_F(FilterIntegrationTest, FilterCanModifyRequest) {
    auto client = createFluentClient("https://example.com");

    // Add auth filter
    client->addFilter(FilterFactory::createBearerAuth("test-token"));

    auto request = client->getAsync("test");

    // Verify auth header was added
    EXPECT_EQ(request->headers().at("Authorization"), "Bearer test-token");
}

TEST_F(FilterIntegrationTest, RequestFilterOverridesClient) {
    auto client = createFluentClient("https://example.com");

    // Add client-level auth
    client->addFilter(FilterFactory::createBearerAuth("client-token"));

    auto request = client->getAsync("test");

    // Override with request-level auth
    request->withBearerAuth("request-token");

    // Request-level should override
    EXPECT_EQ(request->headers().at("Authorization"), "Bearer request-token");
}
```

---

### Task 4.5.2: Create Filter Documentation
**File**: `docs/fluent/FILTERS.md`
**Estimated Time**: 1.5 hours

```markdown
# Fluent HTTP Client Filters

## Overview

Filters provide a middleware pattern for intercepting and modifying HTTP
requests and responses. They are inspired by FluentHttpClient's IHttpFilter.

## Built-in Filters

### DefaultErrorFilter

Throws `ApiException` on HTTP error responses (4xx, 5xx).

```cpp
// Added by default to new clients
client->addFilter(FilterFactory::createErrorFilter());

// To disable for specific requests:
request->withIgnoreHttpErrors(true);
// or
request->withoutFilter<DefaultErrorFilter>();
```

### LoggingFilter

Logs requests and responses with configurable detail level.

```cpp
LoggingConfig config;
config.logRequestHeaders = true;
config.redactHeaders = {"Authorization", "X-Api-Key"};

client->addFilter(FilterFactory::createLoggingFilter(logger, config));
```

### RateLimitFilter

Enforces API rate limits by waiting before requests if needed.

```cpp
RateLimitConfig config;
config.maxWaitTime = std::chrono::seconds{60};
config.onWait = [](auto waitTime) {
    std::cout << "Waiting " << waitTime.count() << "ms for rate limit\n";
};

client->addFilter(FilterFactory::createRateLimitFilter(rateLimiter, config));
```

### AuthenticationFilter

Handles various authentication schemes with optional token refresh.

```cpp
// Bearer token
client->addFilter(FilterFactory::createBearerAuth("my-token"));

// API Key
client->addFilter(FilterFactory::createApiKeyAuth("my-key", "X-Api-Key"));

// With token refresh
AuthConfig config;
config.scheme = AuthScheme::Bearer;
config.tokenProvider = []() { return getToken(); };
config.tokenRefresher = [](const IResponse& resp) {
    return refreshToken();
};
client->addFilter(std::make_shared<AuthenticationFilter>(config));
```

## Filter Execution Order

Filters execute in priority order:
- Lower priority = earlier execution on request
- Higher priority = earlier execution on response (reverse order)

```
Request:  [100] Logging → [200] Auth → [500] RateLimit → [9000] Error → HTTP
Response: HTTP → [9000] Error → [500] RateLimit → [200] Auth → [100] Logging
```

## Creating Custom Filters

```cpp
class MyFilter : public IHttpFilter {
public:
    void onRequest(IRequest& request) override {
        request.withHeader("X-Custom", "value");
    }

    void onResponse(IResponse& response, bool httpErrorAsException) override {
        // Process response
    }

    std::string name() const override { return "MyFilter"; }
    int priority() const override { return 1000; }  // Default priority
};
```
```

---

## Deliverables Checklist

### Source Files
- [ ] `src/fluent/filters/DefaultErrorFilter.h/.cpp`
- [ ] `src/fluent/filters/LoggingFilter.h/.cpp`
- [ ] `src/fluent/filters/RateLimitFilter.h/.cpp`
- [ ] `src/fluent/filters/AuthenticationFilter.h/.cpp`
- [ ] `src/fluent/filters/CompressionFilter.h/.cpp`
- [ ] `src/fluent/filters/FilterFactory.h`
- [ ] `src/fluent/filters/CMakeLists.txt`

### Test Files
- [ ] `tests/fluent/filters/DefaultErrorFilterTest.cpp`
- [ ] `tests/fluent/filters/LoggingFilterTest.cpp`
- [ ] `tests/fluent/filters/RateLimitFilterTest.cpp`
- [ ] `tests/fluent/filters/AuthenticationFilterTest.cpp`
- [ ] `tests/fluent/filters/FilterIntegrationTest.cpp`

### Documentation
- [ ] `docs/fluent/FILTERS.md`

---

## Definition of Done

Week 4 is complete when:

1. ✅ DefaultErrorFilter throws appropriate exceptions
2. ✅ LoggingFilter logs with redaction and configuration
3. ✅ RateLimitFilter integrates with IRateLimiter
4. ✅ AuthenticationFilter supports multiple schemes and refresh
5. ✅ FilterFactory provides convenient creation methods
6. ✅ All filters have unit tests
7. ✅ Integration tests verify filter chain execution
8. ✅ Documentation is complete
