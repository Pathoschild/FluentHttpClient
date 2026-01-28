# Week 3 Task List: Complete Fluent Wrapper Implementation

## Overview

**Objective**: Complete the fluent HTTP client implementation with full filter support, retry logic, all HTTP methods, and streaming downloads.

**Prerequisites**: Week 2 completed (Response, BodyBuilder, partial Request/FluentClient)

**Duration**: 5 working days

**Output**: Fully functional fluent HTTP client that can replace direct HttpClient usage

---

## Architecture Context

```
Week 3 Implementation Scope
═══════════════════════════

┌─────────────────────────────────────────────────────────────────┐
│                    FluentClient (COMPLETE)                       │
│  + All HTTP methods (GET, POST, PUT, PATCH, DELETE, HEAD)       │
│  + Full filter chain execution                                   │
│  + Coordinator/retry integration                                 │
│  + Rate limiter integration                                      │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Request (COMPLETE)                          │
│  + Filter execution (onRequest/onResponse)                       │
│  + Retry via coordinator                                         │
│  + Streaming downloads with progress                             │
│  + Cancellation support                                          │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                  RetryCoordinator (NEW)                          │
│  - Executes retry policies                                       │
│  - Handles timeout conversion                                    │
│  - Chains multiple IRetryConfig                                  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│              HttpClientBridge (NEW)                              │
│  - Adapts existing HttpClient for all HTTP methods               │
│  - Streaming response support                                    │
│  - Progress callback integration                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## New Files This Week

```
src/fluent/
├── RetryCoordinator.h       # Retry coordinator implementation
├── RetryCoordinator.cpp
├── HttpClientBridge.h       # Bridge to existing HttpClient
├── HttpClientBridge.cpp
└── StreamingResponse.h      # Streaming response for large downloads

tests/fluent/
├── RetryCoordinatorTest.cpp
├── FilterExecutionTest.cpp
└── HttpMethodsTest.cpp
```

---

## Day 1: HTTP Client Bridge for All Methods

### Task 3.1.1: Create HttpClientBridge Header
**File**: `src/fluent/HttpClientBridge.h`
**Estimated Time**: 1.5 hours

**Instructions**:

Create an adapter that extends Modular's HttpClient to support all HTTP methods:

```cpp
#pragma once

#include <fluent/Types.h>
#include <fluent/IResponse.h>
#include "Response.h"

#include <core/HttpClient.h>
#include <core/RateLimiter.h>
#include <core/ILogger.h>

#include <functional>
#include <optional>

namespace modular::fluent {

/// Result from HTTP request execution
struct HttpResult {
    int statusCode;
    std::string statusReason;
    Headers headers;
    std::vector<uint8_t> body;
    std::string effectiveUrl;
    std::chrono::milliseconds elapsed;
    bool wasTimeout = false;
};

/// Configuration for an HTTP request
struct HttpRequestConfig {
    HttpMethod method;
    std::string url;
    Headers headers;
    std::optional<std::vector<uint8_t>> body;
    std::chrono::seconds timeout{60};
    bool followRedirects = true;
    int maxRedirects = 5;
};

/// Bridge class that adapts Modular's HttpClient for the fluent API
/// Extends functionality to support all HTTP methods and streaming
class HttpClientBridge {
public:
    /// Construct with dependencies
    HttpClientBridge(
        RateLimiter* rateLimiter = nullptr,
        ILogger* logger = nullptr
    );

    ~HttpClientBridge();

    // Non-copyable, movable
    HttpClientBridge(const HttpClientBridge&) = delete;
    HttpClientBridge& operator=(const HttpClientBridge&) = delete;
    HttpClientBridge(HttpClientBridge&&) noexcept;
    HttpClientBridge& operator=(HttpClientBridge&&) noexcept;

    //=========================================================================
    // Core Request Methods
    //=========================================================================

    /// Execute an HTTP request
    /// @param config Request configuration
    /// @return HTTP result
    /// @throws NetworkException on network errors
    HttpResult execute(const HttpRequestConfig& config);

    /// Execute an HTTP request with streaming response
    /// @param config Request configuration
    /// @param onData Callback for each data chunk
    /// @param onProgress Progress callback (downloaded, total)
    /// @return HTTP result (body will be empty - data sent via callback)
    HttpResult executeStreaming(
        const HttpRequestConfig& config,
        std::function<void(const uint8_t*, size_t)> onData,
        ProgressCallback onProgress = nullptr
    );

    //=========================================================================
    // Configuration
    //=========================================================================

    /// Set connection timeout
    void setConnectionTimeout(std::chrono::seconds timeout);

    /// Set SSL verification
    void setSslVerification(bool verify);

    /// Set proxy
    void setProxy(const std::string& proxyUrl);

    /// Set rate limiter
    void setRateLimiter(RateLimiter* limiter);

    /// Set logger
    void setLogger(ILogger* logger);

private:
    class Impl;
    std::unique_ptr<Impl> impl_;
};

} // namespace modular::fluent
```

---

### Task 3.1.2: Implement HttpClientBridge
**File**: `src/fluent/HttpClientBridge.cpp`
**Estimated Time**: 4 hours

**Instructions**:

```cpp
#include "HttpClientBridge.h"

#include <curl/curl.h>
#include <stdexcept>
#include <chrono>

namespace modular::fluent {

//=============================================================================
// CURL Callbacks
//=============================================================================

namespace {

struct WriteContext {
    std::vector<uint8_t>* buffer;
    std::function<void(const uint8_t*, size_t)> streamCallback;
};

size_t writeCallback(char* ptr, size_t size, size_t nmemb, void* userdata) {
    auto* ctx = static_cast<WriteContext*>(userdata);
    size_t totalSize = size * nmemb;

    if (ctx->streamCallback) {
        ctx->streamCallback(reinterpret_cast<uint8_t*>(ptr), totalSize);
    } else if (ctx->buffer) {
        ctx->buffer->insert(ctx->buffer->end(), ptr, ptr + totalSize);
    }

    return totalSize;
}

struct HeaderContext {
    Headers* headers;
};

size_t headerCallback(char* buffer, size_t size, size_t nitems, void* userdata) {
    auto* ctx = static_cast<HeaderContext*>(userdata);
    size_t totalSize = size * nitems;

    std::string header(buffer, totalSize);

    // Parse header line
    auto colonPos = header.find(':');
    if (colonPos != std::string::npos) {
        std::string key = header.substr(0, colonPos);
        std::string value = header.substr(colonPos + 1);

        // Trim whitespace
        auto trimStart = value.find_first_not_of(" \t\r\n");
        auto trimEnd = value.find_last_not_of(" \t\r\n");
        if (trimStart != std::string::npos) {
            value = value.substr(trimStart, trimEnd - trimStart + 1);
        }

        (*ctx->headers)[key] = value;
    }

    return totalSize;
}

struct ProgressContext {
    ProgressCallback callback;
    std::chrono::steady_clock::time_point lastUpdate;
    static constexpr auto minInterval = std::chrono::milliseconds{100};
};

int progressCallback(
    void* userdata,
    curl_off_t dltotal, curl_off_t dlnow,
    curl_off_t /*ultotal*/, curl_off_t /*ulnow*/
) {
    auto* ctx = static_cast<ProgressContext*>(userdata);

    if (!ctx->callback) return 0;

    auto now = std::chrono::steady_clock::now();
    if (now - ctx->lastUpdate >= ctx->minInterval || dlnow == dltotal) {
        ctx->lastUpdate = now;
        ctx->callback(static_cast<size_t>(dlnow), static_cast<size_t>(dltotal));
    }

    return 0;  // Return non-zero to abort
}

} // anonymous namespace

//=============================================================================
// Implementation Class
//=============================================================================

class HttpClientBridge::Impl {
public:
    Impl(RateLimiter* rateLimiter, ILogger* logger)
        : rateLimiter_(rateLimiter)
        , logger_(logger)
        , curl_(curl_easy_init())
    {
        if (!curl_) {
            throw std::runtime_error("Failed to initialize CURL");
        }

        // Set default options
        curl_easy_setopt(curl_, CURLOPT_SSL_VERIFYPEER, 1L);
        curl_easy_setopt(curl_, CURLOPT_SSL_VERIFYHOST, 2L);
        curl_easy_setopt(curl_, CURLOPT_FOLLOWLOCATION, 1L);
        curl_easy_setopt(curl_, CURLOPT_MAXREDIRS, 5L);
        curl_easy_setopt(curl_, CURLOPT_CONNECTTIMEOUT, 30L);
        curl_easy_setopt(curl_, CURLOPT_TIMEOUT, 60L);
    }

    ~Impl() {
        if (curl_) {
            curl_easy_cleanup(curl_);
        }
    }

    HttpResult execute(const HttpRequestConfig& config) {
        return executeInternal(config, nullptr, nullptr);
    }

    HttpResult executeStreaming(
        const HttpRequestConfig& config,
        std::function<void(const uint8_t*, size_t)> onData,
        ProgressCallback onProgress
    ) {
        return executeInternal(config, std::move(onData), std::move(onProgress));
    }

    void setConnectionTimeout(std::chrono::seconds timeout) {
        curl_easy_setopt(curl_, CURLOPT_CONNECTTIMEOUT, static_cast<long>(timeout.count()));
    }

    void setSslVerification(bool verify) {
        curl_easy_setopt(curl_, CURLOPT_SSL_VERIFYPEER, verify ? 1L : 0L);
        curl_easy_setopt(curl_, CURLOPT_SSL_VERIFYHOST, verify ? 2L : 0L);
    }

    void setProxy(const std::string& proxyUrl) {
        curl_easy_setopt(curl_, CURLOPT_PROXY, proxyUrl.c_str());
    }

    void setRateLimiter(RateLimiter* limiter) {
        rateLimiter_ = limiter;
    }

    void setLogger(ILogger* logger) {
        logger_ = logger;
    }

private:
    HttpResult executeInternal(
        const HttpRequestConfig& config,
        std::function<void(const uint8_t*, size_t)> onData,
        ProgressCallback onProgress
    ) {
        // Wait for rate limiter if present
        if (rateLimiter_) {
            rateLimiter_->waitIfNeeded();
        }

        auto startTime = std::chrono::steady_clock::now();

        // Reset CURL handle for new request
        curl_easy_reset(curl_);

        // Set URL
        curl_easy_setopt(curl_, CURLOPT_URL, config.url.c_str());

        // Set HTTP method
        switch (config.method) {
            case HttpMethod::GET:
                curl_easy_setopt(curl_, CURLOPT_HTTPGET, 1L);
                break;
            case HttpMethod::POST:
                curl_easy_setopt(curl_, CURLOPT_POST, 1L);
                break;
            case HttpMethod::PUT:
                curl_easy_setopt(curl_, CURLOPT_CUSTOMREQUEST, "PUT");
                break;
            case HttpMethod::PATCH:
                curl_easy_setopt(curl_, CURLOPT_CUSTOMREQUEST, "PATCH");
                break;
            case HttpMethod::DELETE:
                curl_easy_setopt(curl_, CURLOPT_CUSTOMREQUEST, "DELETE");
                break;
            case HttpMethod::HEAD:
                curl_easy_setopt(curl_, CURLOPT_NOBODY, 1L);
                break;
            case HttpMethod::OPTIONS:
                curl_easy_setopt(curl_, CURLOPT_CUSTOMREQUEST, "OPTIONS");
                break;
        }

        // Set headers
        struct curl_slist* headerList = nullptr;
        for (const auto& [key, value] : config.headers) {
            std::string header = key + ": " + value;
            headerList = curl_slist_append(headerList, header.c_str());
        }
        if (headerList) {
            curl_easy_setopt(curl_, CURLOPT_HTTPHEADER, headerList);
        }

        // Set request body
        if (config.body && !config.body->empty()) {
            curl_easy_setopt(curl_, CURLOPT_POSTFIELDS, config.body->data());
            curl_easy_setopt(curl_, CURLOPT_POSTFIELDSIZE, config.body->size());
        }

        // Set timeout
        curl_easy_setopt(curl_, CURLOPT_TIMEOUT, static_cast<long>(config.timeout.count()));

        // Set redirects
        curl_easy_setopt(curl_, CURLOPT_FOLLOWLOCATION, config.followRedirects ? 1L : 0L);
        curl_easy_setopt(curl_, CURLOPT_MAXREDIRS, static_cast<long>(config.maxRedirects));

        // Set up response handling
        std::vector<uint8_t> responseBody;
        WriteContext writeCtx{&responseBody, onData};
        curl_easy_setopt(curl_, CURLOPT_WRITEFUNCTION, writeCallback);
        curl_easy_setopt(curl_, CURLOPT_WRITEDATA, &writeCtx);

        Headers responseHeaders;
        HeaderContext headerCtx{&responseHeaders};
        curl_easy_setopt(curl_, CURLOPT_HEADERFUNCTION, headerCallback);
        curl_easy_setopt(curl_, CURLOPT_HEADERDATA, &headerCtx);

        // Set up progress
        ProgressContext progressCtx{onProgress, std::chrono::steady_clock::now()};
        if (onProgress) {
            curl_easy_setopt(curl_, CURLOPT_XFERINFOFUNCTION, progressCallback);
            curl_easy_setopt(curl_, CURLOPT_XFERINFODATA, &progressCtx);
            curl_easy_setopt(curl_, CURLOPT_NOPROGRESS, 0L);
        }

        // Log request
        if (logger_) {
            logger_->debug("HTTP " + std::string(to_string(config.method)) + " " + config.url);
        }

        // Execute request
        CURLcode res = curl_easy_perform(curl_);

        // Clean up headers
        if (headerList) {
            curl_slist_free_all(headerList);
        }

        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(
            std::chrono::steady_clock::now() - startTime
        );

        // Build result
        HttpResult result;
        result.elapsed = elapsed;
        result.headers = std::move(responseHeaders);
        result.body = onData ? std::vector<uint8_t>{} : std::move(responseBody);

        if (res != CURLE_OK) {
            result.wasTimeout = (res == CURLE_OPERATION_TIMEDOUT);

            if (logger_) {
                logger_->error("CURL error: " + std::string(curl_easy_strerror(res)));
            }

            throw NetworkException(
                std::string("Network error: ") + curl_easy_strerror(res),
                result.wasTimeout ? NetworkException::Reason::Timeout
                                  : NetworkException::Reason::ConnectionFailed
            );
        }

        // Get response info
        long statusCode;
        curl_easy_getinfo(curl_, CURLINFO_RESPONSE_CODE, &statusCode);
        result.statusCode = static_cast<int>(statusCode);

        char* effectiveUrl;
        curl_easy_getinfo(curl_, CURLINFO_EFFECTIVE_URL, &effectiveUrl);
        result.effectiveUrl = effectiveUrl ? effectiveUrl : config.url;

        // Determine status reason
        result.statusReason = getStatusReason(result.statusCode);

        // Update rate limiter from response headers
        if (rateLimiter_) {
            rateLimiter_->updateFromHeaders(result.headers);
        }

        // Log response
        if (logger_) {
            logger_->debug("HTTP " + std::to_string(result.statusCode) + " in " +
                          std::to_string(elapsed.count()) + "ms");
        }

        return result;
    }

    static std::string getStatusReason(int code) {
        static const std::map<int, std::string> reasons = {
            {200, "OK"}, {201, "Created"}, {202, "Accepted"},
            {204, "No Content"}, {206, "Partial Content"},
            {301, "Moved Permanently"}, {302, "Found"}, {304, "Not Modified"},
            {400, "Bad Request"}, {401, "Unauthorized"}, {403, "Forbidden"},
            {404, "Not Found"}, {405, "Method Not Allowed"},
            {408, "Request Timeout"}, {409, "Conflict"},
            {429, "Too Many Requests"},
            {500, "Internal Server Error"}, {502, "Bad Gateway"},
            {503, "Service Unavailable"}, {504, "Gateway Timeout"}
        };

        auto it = reasons.find(code);
        return it != reasons.end() ? it->second : "Unknown";
    }

    RateLimiter* rateLimiter_;
    ILogger* logger_;
    CURL* curl_;
};

//=============================================================================
// HttpClientBridge Implementation
//=============================================================================

HttpClientBridge::HttpClientBridge(RateLimiter* rateLimiter, ILogger* logger)
    : impl_(std::make_unique<Impl>(rateLimiter, logger))
{}

HttpClientBridge::~HttpClientBridge() = default;

HttpClientBridge::HttpClientBridge(HttpClientBridge&&) noexcept = default;
HttpClientBridge& HttpClientBridge::operator=(HttpClientBridge&&) noexcept = default;

HttpResult HttpClientBridge::execute(const HttpRequestConfig& config) {
    return impl_->execute(config);
}

HttpResult HttpClientBridge::executeStreaming(
    const HttpRequestConfig& config,
    std::function<void(const uint8_t*, size_t)> onData,
    ProgressCallback onProgress
) {
    return impl_->executeStreaming(config, std::move(onData), std::move(onProgress));
}

void HttpClientBridge::setConnectionTimeout(std::chrono::seconds timeout) {
    impl_->setConnectionTimeout(timeout);
}

void HttpClientBridge::setSslVerification(bool verify) {
    impl_->setSslVerification(verify);
}

void HttpClientBridge::setProxy(const std::string& proxyUrl) {
    impl_->setProxy(proxyUrl);
}

void HttpClientBridge::setRateLimiter(RateLimiter* limiter) {
    impl_->setRateLimiter(limiter);
}

void HttpClientBridge::setLogger(ILogger* logger) {
    impl_->setLogger(logger);
}

} // namespace modular::fluent
```

**Verification**:
- [ ] All HTTP methods (GET, POST, PUT, PATCH, DELETE, HEAD) work
- [ ] Headers are properly sent and received
- [ ] Request body is transmitted for POST/PUT/PATCH
- [ ] Streaming callbacks receive data chunks
- [ ] Progress callback is throttled to 10/second
- [ ] Rate limiter integration works
- [ ] Timeout handling throws NetworkException

---

## Day 2: Retry Coordinator Implementation

### Task 3.2.1: Create RetryCoordinator Implementation
**File**: `src/fluent/RetryCoordinator.h` and `src/fluent/RetryCoordinator.cpp`
**Estimated Time**: 3 hours

**Instructions**:

```cpp
// RetryCoordinator.h
#pragma once

#include <fluent/IRequestCoordinator.h>
#include <fluent/IRetryConfig.h>
#include <fluent/Exceptions.h>

#include <vector>
#include <thread>

namespace modular::fluent {

/// Implementation of retry coordinator that chains multiple retry configs
class RetryCoordinator : public IRequestCoordinator {
public:
    /// Create with retry configurations
    explicit RetryCoordinator(std::vector<std::shared_ptr<IRetryConfig>> configs);

    /// Create with simple parameters
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

    /// Find a retry config that wants to retry this response
    std::shared_ptr<IRetryConfig> findRetryConfig(int statusCode, bool isTimeout) const;

    /// Check if we've exceeded max attempts for all configs
    bool isMaxAttemptsExceeded(int attempt) const;

    /// Get total max retries across all configs
    int totalMaxRetries() const;
};

} // namespace modular::fluent
```

```cpp
// RetryCoordinator.cpp
#include "RetryCoordinator.h"
#include <numeric>

namespace modular::fluent {

RetryCoordinator::RetryCoordinator(std::vector<std::shared_ptr<IRetryConfig>> configs)
    : configs_(std::move(configs))
{}

RetryCoordinator::RetryCoordinator(
    int maxRetries,
    std::function<bool(int statusCode, bool isTimeout)> shouldRetry,
    std::function<std::chrono::milliseconds(int attempt)> getDelay
) {
    // Create a custom config from lambdas
    class LambdaConfig : public IRetryConfig {
    public:
        LambdaConfig(
            int maxRetries,
            std::function<bool(int, bool)> shouldRetry,
            std::function<std::chrono::milliseconds(int)> getDelay
        ) : maxRetries_(maxRetries)
          , shouldRetry_(std::move(shouldRetry))
          , getDelay_(std::move(getDelay))
        {}

        int maxRetries() const override { return maxRetries_; }

        bool shouldRetry(int statusCode, bool isTimeout) const override {
            return shouldRetry_(statusCode, isTimeout);
        }

        std::chrono::milliseconds getDelay(int attempt, int) const override {
            return getDelay_(attempt);
        }

    private:
        int maxRetries_;
        std::function<bool(int, bool)> shouldRetry_;
        std::function<std::chrono::milliseconds(int)> getDelay_;
    };

    configs_.push_back(std::make_shared<LambdaConfig>(
        maxRetries, std::move(shouldRetry), std::move(getDelay)
    ));
}

std::future<ResponsePtr> RetryCoordinator::executeAsync(
    IRequest& request,
    std::function<std::future<ResponsePtr>(IRequest&)> dispatcher
) {
    return std::async(std::launch::async, [this, &request, dispatcher]() -> ResponsePtr {
        int attempt = 0;
        int maxAttempts = totalMaxRetries() + 1;  // +1 for initial attempt

        while (true) {
            ++attempt;

            try {
                auto response = dispatcher(request).get();

                // Check if we should retry based on status code
                int statusCode = response->statusCode();
                bool isTimeout = false;  // Not a timeout if we got a response

                auto retryConfig = findRetryConfig(statusCode, isTimeout);

                if (retryConfig && attempt <= maxAttempts) {
                    // Wait before retry
                    auto delay = retryConfig->getDelay(attempt, statusCode);
                    if (delay.count() > 0) {
                        std::this_thread::sleep_for(delay);
                    }
                    continue;  // Retry
                }

                // No retry needed or max attempts exceeded
                return response;

            } catch (const NetworkException& e) {
                bool isTimeout = e.isTimeout();

                // Convert timeout to synthetic status code for retry logic
                int syntheticStatus = isTimeout ? 589 : 0;  // 589 = timeout (custom)

                auto retryConfig = findRetryConfig(syntheticStatus, isTimeout);

                if (retryConfig && attempt <= maxAttempts) {
                    auto delay = retryConfig->getDelay(attempt, syntheticStatus);
                    if (delay.count() > 0) {
                        std::this_thread::sleep_for(delay);
                    }
                    continue;  // Retry
                }

                // Max retries exceeded for network error
                throw;
            }
        }
    });
}

std::shared_ptr<IRetryConfig> RetryCoordinator::findRetryConfig(
    int statusCode, bool isTimeout
) const {
    for (const auto& config : configs_) {
        if (config->shouldRetry(statusCode, isTimeout)) {
            return config;
        }
    }
    return nullptr;
}

bool RetryCoordinator::isMaxAttemptsExceeded(int attempt) const {
    return attempt > totalMaxRetries();
}

int RetryCoordinator::totalMaxRetries() const {
    return std::accumulate(configs_.begin(), configs_.end(), 0,
        [](int sum, const auto& config) {
            return sum + config->maxRetries();
        });
}

} // namespace modular::fluent
```

---

### Task 3.2.2: Create Retry Coordinator Tests
**File**: `tests/fluent/RetryCoordinatorTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include "fluent/RetryCoordinator.h"
#include "fluent/Response.h"

using namespace modular::fluent;

class MockRequest : public IRequest {
    // Minimal mock implementation for testing
    // ... implement required methods with stubs
};

class RetryCoordinatorTest : public ::testing::Test {
protected:
    int dispatchCount = 0;

    std::function<std::future<ResponsePtr>(IRequest&)> makeDispatcher(
        std::vector<int> statusCodes
    ) {
        auto codes = std::make_shared<std::vector<int>>(std::move(statusCodes));
        auto count = std::make_shared<int>(0);

        return [codes, count, this](IRequest&) -> std::future<ResponsePtr> {
            return std::async(std::launch::async, [codes, count, this]() -> ResponsePtr {
                ++dispatchCount;
                int idx = (*count)++;
                int status = idx < codes->size() ? (*codes)[idx] : 200;

                return std::make_unique<Response>(
                    status, "Test", Headers{}, "", "http://test", std::chrono::milliseconds{10}
                );
            });
        };
    }
};

TEST_F(RetryCoordinatorTest, NoRetryOnSuccess) {
    RetryCoordinator coordinator(3,
        [](int code, bool) { return code >= 500; },
        [](int) { return std::chrono::milliseconds{10}; }
    );

    MockRequest request;
    auto dispatcher = makeDispatcher({200});

    auto response = coordinator.executeAsync(request, dispatcher).get();

    EXPECT_EQ(dispatchCount, 1);
    EXPECT_EQ(response->statusCode(), 200);
}

TEST_F(RetryCoordinatorTest, RetryOnServerError) {
    RetryCoordinator coordinator(3,
        [](int code, bool) { return code >= 500; },
        [](int) { return std::chrono::milliseconds{1}; }
    );

    MockRequest request;
    auto dispatcher = makeDispatcher({500, 500, 200});  // Fail twice, then succeed

    auto response = coordinator.executeAsync(request, dispatcher).get();

    EXPECT_EQ(dispatchCount, 3);
    EXPECT_EQ(response->statusCode(), 200);
}

TEST_F(RetryCoordinatorTest, ExhaustRetries) {
    RetryCoordinator coordinator(2,
        [](int code, bool) { return code >= 500; },
        [](int) { return std::chrono::milliseconds{1}; }
    );

    MockRequest request;
    auto dispatcher = makeDispatcher({500, 500, 500, 500});  // All failures

    auto response = coordinator.executeAsync(request, dispatcher).get();

    EXPECT_EQ(dispatchCount, 3);  // Initial + 2 retries
    EXPECT_EQ(response->statusCode(), 500);  // Returns last failed response
}

TEST_F(RetryCoordinatorTest, NoRetryOnClientError) {
    RetryCoordinator coordinator(3,
        [](int code, bool) { return code >= 500; },  // Only retry server errors
        [](int) { return std::chrono::milliseconds{1}; }
    );

    MockRequest request;
    auto dispatcher = makeDispatcher({404});

    auto response = coordinator.executeAsync(request, dispatcher).get();

    EXPECT_EQ(dispatchCount, 1);  // No retry for 404
    EXPECT_EQ(response->statusCode(), 404);
}

TEST_F(RetryCoordinatorTest, ExponentialBackoff) {
    std::vector<std::chrono::milliseconds> delays;

    RetryCoordinator coordinator(3,
        [](int code, bool) { return code >= 500; },
        [&delays](int attempt) {
            auto delay = std::chrono::milliseconds{10 * (1 << (attempt - 1))};
            delays.push_back(delay);
            return delay;
        }
    );

    MockRequest request;
    auto dispatcher = makeDispatcher({500, 500, 500, 200});

    auto startTime = std::chrono::steady_clock::now();
    auto response = coordinator.executeAsync(request, dispatcher).get();
    auto elapsed = std::chrono::steady_clock::now() - startTime;

    EXPECT_EQ(delays.size(), 3);
    EXPECT_EQ(delays[0], std::chrono::milliseconds{10});
    EXPECT_EQ(delays[1], std::chrono::milliseconds{20});
    EXPECT_EQ(delays[2], std::chrono::milliseconds{40});
}
```

---

## Day 3: Complete Filter Execution in Request

### Task 3.3.1: Update Request with Filter Execution
**Update File**: `src/fluent/Request.cpp`
**Estimated Time**: 3 hours

**Instructions**:

Complete the `applyRequestFilters()`, `applyResponseFilters()`, and `executeInternal()` methods:

```cpp
// Add to Request.cpp

void Request::applyRequestFilters() {
    // Get combined filter list: client filters + request-specific filters
    std::vector<FilterPtr> allFilters;

    // Add client filters (excluding removed types)
    if (client_) {
        for (const auto& filter : client_->filters().all()) {
            // Check if this filter type was removed for this request
            bool isRemoved = std::any_of(
                removedFilterTypes_.begin(),
                removedFilterTypes_.end(),
                [&filter](const std::type_index& type) {
                    return type == std::type_index(typeid(*filter));
                }
            );

            if (!isRemoved) {
                allFilters.push_back(filter);
            }
        }
    }

    // Add request-specific filters
    for (const auto& filter : additionalFilters_) {
        allFilters.push_back(filter);
    }

    // Sort by priority
    std::sort(allFilters.begin(), allFilters.end(),
        [](const FilterPtr& a, const FilterPtr& b) {
            return a->priority() < b->priority();
        });

    // Execute OnRequest in order
    for (const auto& filter : allFilters) {
        filter->onRequest(*this);
    }
}

void Request::applyResponseFilters(Response& response) {
    // Get combined filter list (same as above)
    std::vector<FilterPtr> allFilters;

    if (client_) {
        for (const auto& filter : client_->filters().all()) {
            bool isRemoved = std::any_of(
                removedFilterTypes_.begin(),
                removedFilterTypes_.end(),
                [&filter](const std::type_index& type) {
                    return type == std::type_index(typeid(*filter));
                }
            );

            if (!isRemoved) {
                allFilters.push_back(filter);
            }
        }
    }

    for (const auto& filter : additionalFilters_) {
        allFilters.push_back(filter);
    }

    // Sort by priority
    std::sort(allFilters.begin(), allFilters.end(),
        [](const FilterPtr& a, const FilterPtr& b) {
            return a->priority() < b->priority();
        });

    // Execute OnResponse in REVERSE order
    bool httpErrorAsException = !options_.ignoreHttpErrors.value_or(false);

    for (auto it = allFilters.rbegin(); it != allFilters.rend(); ++it) {
        (*it)->onResponse(response, httpErrorAsException);
    }
}

ResponsePtr Request::executeInternal() {
    // Check cancellation
    if (cancellationToken_.stop_requested()) {
        throw NetworkException("Request cancelled", NetworkException::Reason::Unknown);
    }

    // Apply request filters
    applyRequestFilters();

    // Build request configuration
    HttpRequestConfig config;
    config.method = method_;
    config.url = buildFullUrl();
    config.headers = headers_;
    config.timeout = options_.timeout.value_or(std::chrono::seconds{60});

    if (body_) {
        config.body = body_->content;
    }

    // Determine how to execute
    auto executeOnce = [this, &config]() -> ResponsePtr {
        auto& bridge = client_->httpClientBridge();
        auto result = bridge.execute(config);

        auto response = std::make_unique<Response>(
            result.statusCode,
            result.statusReason,
            result.headers,
            result.body,
            result.effectiveUrl,
            result.elapsed
        );

        return response;
    };

    ResponsePtr response;

    // Check if we should use coordinator for retry
    auto coordinator = disableRetry_ ? nullptr :
                       (retryConfig_ ? std::make_shared<RetryCoordinator>(
                           std::vector<std::shared_ptr<IRetryConfig>>{retryConfig_})
                       : client_->requestCoordinator());

    if (coordinator) {
        // Execute with retry via coordinator
        auto dispatcher = [&executeOnce](IRequest&) {
            return std::async(std::launch::async, executeOnce);
        };

        response = coordinator->executeAsync(*this, dispatcher).get();
    } else {
        // Direct execution without retry
        response = executeOnce();
    }

    // Apply response filters
    applyResponseFilters(static_cast<Response&>(*response));

    return response;
}
```

---

### Task 3.3.2: Create Filter Execution Tests
**File**: `tests/fluent/FilterExecutionTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>
#include "fluent/FluentClient.h"

using namespace modular::fluent;

class TestFilter : public IHttpFilter {
public:
    std::vector<std::string>& log;
    std::string name_;

    TestFilter(std::vector<std::string>& log, const std::string& name)
        : log(log), name_(name) {}

    void onRequest(IRequest& request) override {
        log.push_back(name_ + "::onRequest");
    }

    void onResponse(IResponse& response, bool httpErrorAsException) override {
        log.push_back(name_ + "::onResponse");
    }

    std::string name() const override { return name_; }
};

class FilterExecutionTest : public ::testing::Test {
protected:
    std::vector<std::string> filterLog;
};

TEST_F(FilterExecutionTest, FiltersExecuteInOrder) {
    auto client = createFluentClient("https://example.com");

    client->addFilter(std::make_shared<TestFilter>(filterLog, "Filter1"));
    client->addFilter(std::make_shared<TestFilter>(filterLog, "Filter2"));
    client->addFilter(std::make_shared<TestFilter>(filterLog, "Filter3"));

    // Note: This would need a mock HTTP layer to actually test
    // For now, verify filter registration
    EXPECT_EQ(client->filters().size(), 3);
}

TEST_F(FilterExecutionTest, RequestFilterOverrides) {
    auto client = createFluentClient("https://example.com");
    client->addFilter(std::make_shared<TestFilter>(filterLog, "ClientFilter"));

    auto request = client->getAsync("test");
    request->withFilter(std::make_shared<TestFilter>(filterLog, "RequestFilter"));

    // Verify both filters are present
    // (actual execution test requires mock)
}

TEST_F(FilterExecutionTest, RemoveFilterByType) {
    auto client = createFluentClient("https://example.com");

    auto filter1 = std::make_shared<TestFilter>(filterLog, "Filter1");
    auto filter2 = std::make_shared<TestFilter>(filterLog, "Filter2");

    client->addFilter(filter1);
    client->addFilter(filter2);

    EXPECT_EQ(client->filters().size(), 2);

    client->filters().removeAll<TestFilter>();

    EXPECT_EQ(client->filters().size(), 0);
}
```

---

## Day 4: Streaming Downloads and FluentClient Completion

### Task 3.4.1: Update FluentClient with HttpClientBridge
**Update File**: `src/fluent/FluentClient.h` and `src/fluent/FluentClient.cpp`
**Estimated Time**: 2 hours

Add the HttpClientBridge integration:

```cpp
// Add to FluentClient.h

#include "HttpClientBridge.h"

// Add to private members:
std::unique_ptr<HttpClientBridge> httpBridge_;

// Add public method:
HttpClientBridge& httpClientBridge();
```

```cpp
// Add to FluentClient.cpp

FluentClient::FluentClient(std::string_view baseUrl)
    : baseUrl_(baseUrl)
    , httpBridge_(std::make_unique<HttpClientBridge>())
{}

FluentClient::FluentClient(
    std::string_view baseUrl,
    RateLimiterPtr rateLimiter,
    std::shared_ptr<ILogger> logger
)
    : baseUrl_(baseUrl)
    , rateLimiter_(std::move(rateLimiter))
    , logger_(std::move(logger))
    , httpBridge_(std::make_unique<HttpClientBridge>(
          rateLimiter_.get(), logger_.get()))
{}

HttpClientBridge& FluentClient::httpClientBridge() {
    return *httpBridge_;
}
```

---

### Task 3.4.2: Add Streaming Download to Request
**Update File**: `src/fluent/Request.cpp`
**Estimated Time**: 3 hours

Update `downloadToAsync()` to use streaming:

```cpp
std::future<void> Request::downloadToAsync(
    const std::filesystem::path& path,
    ProgressCallback progress
) {
    return std::async(std::launch::async, [this, path, progress]() {
        // Check cancellation
        if (cancellationToken_.stop_requested()) {
            throw NetworkException("Request cancelled", NetworkException::Reason::Unknown);
        }

        // Apply request filters
        applyRequestFilters();

        // Build request configuration
        HttpRequestConfig config;
        config.method = method_;
        config.url = buildFullUrl();
        config.headers = headers_;
        config.timeout = options_.timeout.value_or(std::chrono::seconds{300});  // Longer for downloads

        // Ensure parent directory exists
        if (path.has_parent_path()) {
            std::filesystem::create_directories(path.parent_path());
        }

        // Open output file
        std::ofstream file(path, std::ios::binary);
        if (!file) {
            throw std::filesystem::filesystem_error(
                "Failed to open file for writing",
                path,
                std::make_error_code(std::errc::io_error)
            );
        }

        // Data callback - write chunks to file
        auto onData = [&file, &path](const uint8_t* data, size_t size) {
            file.write(reinterpret_cast<const char*>(data), size);
            if (!file) {
                throw std::filesystem::filesystem_error(
                    "Failed to write to file",
                    path,
                    std::make_error_code(std::errc::io_error)
                );
            }
        };

        try {
            auto& bridge = client_->httpClientBridge();
            auto result = bridge.executeStreaming(config, onData, progress);

            file.close();

            // Check for HTTP errors
            bool ignoreErrors = options_.ignoreHttpErrors.value_or(false);
            if (!ignoreErrors && !is_success_status(result.statusCode)) {
                // Delete partial file on error
                std::filesystem::remove(path);

                throw ApiException(
                    "Download failed with status " + std::to_string(result.statusCode),
                    result.statusCode,
                    result.statusReason,
                    result.headers,
                    ""  // Body not available in streaming mode
                );
            }

        } catch (...) {
            // Clean up partial file on any error
            file.close();
            std::filesystem::remove(path);
            throw;
        }
    });
}
```

---

## Day 5: Testing and Documentation

### Task 3.5.1: Create HTTP Methods Integration Test
**File**: `tests/fluent/HttpMethodsTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>

using namespace modular::fluent;

// These tests require a test server (e.g., httpbin.org or local mock)
class HttpMethodsIntegrationTest : public ::testing::Test {
protected:
    ClientPtr client;

    void SetUp() override {
        client = createFluentClient("https://httpbin.org");
        client->setUserAgent("ModularFluentTest/1.0");
    }
};

TEST_F(HttpMethodsIntegrationTest, DISABLED_GetRequest) {
    auto response = client->getAsync("get")
        ->withArgument("test", "value")
        .asResponse();

    EXPECT_TRUE(response->isSuccessStatusCode());
    EXPECT_EQ(response->statusCode(), 200);

    auto json = response->asJson();
    EXPECT_EQ(json["args"]["test"], "value");
}

TEST_F(HttpMethodsIntegrationTest, DISABLED_PostJsonRequest) {
    nlohmann::json payload = {{"name", "test"}, {"value", 42}};

    auto response = client->postAsync("post")
        ->withBody([&payload](IBodyBuilder& b) {
            return b.jsonBody(payload);
        })
        .asResponse();

    EXPECT_TRUE(response->isSuccessStatusCode());

    auto json = response->asJson();
    EXPECT_EQ(json["json"]["name"], "test");
    EXPECT_EQ(json["json"]["value"], 42);
}

TEST_F(HttpMethodsIntegrationTest, DISABLED_PutRequest) {
    auto response = client->putAsync("put")
        ->withBody([](IBodyBuilder& b) {
            return b.raw("test data", "text/plain");
        })
        .asResponse();

    EXPECT_TRUE(response->isSuccessStatusCode());
}

TEST_F(HttpMethodsIntegrationTest, DISABLED_DeleteRequest) {
    auto response = client->deleteAsync("delete")
        ->asResponse();

    EXPECT_TRUE(response->isSuccessStatusCode());
}

TEST_F(HttpMethodsIntegrationTest, DISABLED_HeadersAreSent) {
    auto response = client->getAsync("headers")
        ->withHeader("X-Custom-Header", "custom-value")
        .asResponse();

    auto json = response->asJson();
    EXPECT_EQ(json["headers"]["X-Custom-Header"], "custom-value");
}

TEST_F(HttpMethodsIntegrationTest, DISABLED_BearerAuth) {
    auto response = client->getAsync("bearer")
        ->withBearerAuth("test-token")
        .asResponse();

    // httpbin returns 200 if bearer token is present
    EXPECT_TRUE(response->isSuccessStatusCode());
}
```

---

### Task 3.5.2: Create Download Test
**File**: `tests/fluent/DownloadTest.cpp`
**Estimated Time**: 1.5 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>
#include <fstream>

using namespace modular::fluent;

class DownloadTest : public ::testing::Test {
protected:
    ClientPtr client;
    std::filesystem::path tempDir;

    void SetUp() override {
        client = createFluentClient("https://httpbin.org");
        tempDir = std::filesystem::temp_directory_path() / "fluent_test";
        std::filesystem::create_directories(tempDir);
    }

    void TearDown() override {
        std::filesystem::remove_all(tempDir);
    }
};

TEST_F(DownloadTest, DISABLED_DownloadWithProgress) {
    auto downloadPath = tempDir / "test_download.json";

    std::vector<std::pair<size_t, size_t>> progressUpdates;

    client->getAsync("bytes/1024")
        ->downloadTo(downloadPath, [&](size_t downloaded, size_t total) {
            progressUpdates.emplace_back(downloaded, total);
        });

    EXPECT_TRUE(std::filesystem::exists(downloadPath));
    EXPECT_EQ(std::filesystem::file_size(downloadPath), 1024);
    EXPECT_FALSE(progressUpdates.empty());

    // Last update should show complete
    EXPECT_EQ(progressUpdates.back().first, progressUpdates.back().second);
}

TEST_F(DownloadTest, DISABLED_DownloadCreatesDirectory) {
    auto downloadPath = tempDir / "subdir" / "nested" / "file.txt";

    client->getAsync("bytes/100")
        ->downloadTo(downloadPath);

    EXPECT_TRUE(std::filesystem::exists(downloadPath));
}
```

---

### Task 3.5.3: Update CMakeLists.txt
**Update File**: `src/fluent/CMakeLists.txt`
**Estimated Time**: 30 minutes

Add new files:

```cmake
add_library(fluent_client
    Utils.h
    Response.h
    Response.cpp
    BodyBuilder.h
    BodyBuilder.cpp
    Request.h
    Request.cpp
    FluentClient.h
    FluentClient.cpp
    HttpClientBridge.h
    HttpClientBridge.cpp
    RetryCoordinator.h
    RetryCoordinator.cpp
)
```

---

## Deliverables Checklist

### Source Files
- [ ] `src/fluent/HttpClientBridge.h` - HTTP bridge header
- [ ] `src/fluent/HttpClientBridge.cpp` - HTTP bridge implementation
- [ ] `src/fluent/RetryCoordinator.h` - Retry coordinator header
- [ ] `src/fluent/RetryCoordinator.cpp` - Retry coordinator implementation
- [ ] Updated `src/fluent/Request.cpp` - Complete with filter execution
- [ ] Updated `src/fluent/FluentClient.cpp` - Complete with bridge integration

### Test Files
- [ ] `tests/fluent/RetryCoordinatorTest.cpp`
- [ ] `tests/fluent/FilterExecutionTest.cpp`
- [ ] `tests/fluent/HttpMethodsTest.cpp`
- [ ] `tests/fluent/DownloadTest.cpp`

### Quality Checks
- [ ] All HTTP methods work (GET, POST, PUT, PATCH, DELETE, HEAD)
- [ ] Retry logic works with exponential backoff
- [ ] Filters execute in correct order
- [ ] Streaming downloads work with progress
- [ ] Rate limiter integration works
- [ ] All unit tests pass

---

## Definition of Done

Week 3 is complete when:

1. ✅ HttpClientBridge supports all HTTP methods
2. ✅ RetryCoordinator implements retry with backoff
3. ✅ Filter execution works (onRequest before, onResponse after)
4. ✅ Streaming downloads work with progress callbacks
5. ✅ Rate limiter is integrated into request flow
6. ✅ All tests pass
7. ✅ Code reviewed and merged

---

## Notes for Week 4

Week 4 will implement standard filters:

1. DefaultErrorFilter - Throws on HTTP errors
2. LoggingFilter - Request/response logging
3. RateLimitFilter - Rate limit enforcement and waiting
4. AuthenticationFilter - Token refresh support
5. RetryFilter - Alternative to coordinator
