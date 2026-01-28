# Week 7 Task List: Download Progress Support and Final Polish

## Overview

**Objective**: Complete the fluent HTTP client integration with advanced download features, comprehensive testing, performance optimization, and final documentation.

**Prerequisites**: Week 6 completed (all API migrations)

**Duration**: 5 working days

**Output**: Production-ready fluent HTTP client library fully integrated with Modular

---

## Week 7 Focus Areas

```
Week 7 Deliverables
═══════════════════

┌─────────────────────────────────────────────────────────────────┐
│                   Download Manager (NEW)                         │
│  - Queue management                                              │
│  - Concurrent downloads                                          │
│  - Resume/pause support                                          │
│  - MD5 verification                                              │
└─────────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────────┐
│                   Performance Optimization                       │
│  - Connection pooling verification                               │
│  - Memory usage optimization                                     │
│  - Progress callback throttling                                  │
└─────────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────────┐
│                   Comprehensive Testing                          │
│  - End-to-end integration tests                                  │
│  - Performance benchmarks                                        │
│  - Error scenario coverage                                       │
└─────────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────────┐
│                   Documentation & Examples                       │
│  - API reference                                                 │
│  - Usage examples                                                │
│  - Migration guide completion                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Day 1: Download Manager

### Task 7.1.1: Create Download Manager
**File**: `src/fluent/DownloadManager.h` and `.cpp`
**Estimated Time**: 4 hours

A download manager that handles queuing, concurrent downloads, and resume support:

```cpp
// DownloadManager.h
#pragma once

#include <fluent/Fluent.h>

#include <queue>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <atomic>
#include <future>
#include <filesystem>

namespace modular::fluent {

/// Status of a download
enum class DownloadStatus {
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
    Cancelled
};

/// Information about a download
struct DownloadInfo {
    std::string id;
    std::string url;
    std::filesystem::path outputPath;
    DownloadStatus status;
    size_t totalBytes;
    size_t downloadedBytes;
    std::optional<std::string> expectedMd5;
    std::optional<std::string> error;
    std::chrono::steady_clock::time_point startTime;
    std::chrono::steady_clock::time_point endTime;

    double progressPercent() const {
        return totalBytes > 0 ? (downloadedBytes * 100.0 / totalBytes) : 0;
    }

    std::chrono::milliseconds elapsed() const {
        auto end = endTime > startTime ? endTime : std::chrono::steady_clock::now();
        return std::chrono::duration_cast<std::chrono::milliseconds>(end - startTime);
    }

    double speedBytesPerSecond() const {
        auto elapsedMs = elapsed().count();
        return elapsedMs > 0 ? (downloadedBytes * 1000.0 / elapsedMs) : 0;
    }
};

/// Callback for download events
using DownloadEventCallback = std::function<void(const DownloadInfo&)>;

/// Configuration for download manager
struct DownloadManagerConfig {
    /// Maximum concurrent downloads
    int maxConcurrentDownloads = 3;

    /// Retry failed downloads
    int maxRetries = 3;

    /// Delay between retries
    std::chrono::seconds retryDelay{5};

    /// Verify MD5 checksums if available
    bool verifyChecksum = true;

    /// Delete partial files on failure
    bool deletePartialOnFailure = true;

    /// Progress update interval
    std::chrono::milliseconds progressInterval{100};
};

/// Manages download queue with concurrent execution
class DownloadManager {
public:
    /// Create with configuration
    explicit DownloadManager(DownloadManagerConfig config = {});

    /// Create with client and configuration
    DownloadManager(ClientPtr client, DownloadManagerConfig config = {});

    ~DownloadManager();

    // Non-copyable
    DownloadManager(const DownloadManager&) = delete;
    DownloadManager& operator=(const DownloadManager&) = delete;

    //=========================================================================
    // Queue Management
    //=========================================================================

    /// Add a download to the queue
    /// @return Download ID for tracking
    std::string enqueue(
        const std::string& url,
        const std::filesystem::path& outputPath,
        const std::optional<std::string>& expectedMd5 = std::nullopt
    );

    /// Add multiple downloads
    std::vector<std::string> enqueueAll(
        const std::vector<std::pair<std::string, std::filesystem::path>>& downloads
    );

    /// Cancel a download
    bool cancel(const std::string& downloadId);

    /// Cancel all downloads
    void cancelAll();

    /// Pause a download (if supported by server)
    bool pause(const std::string& downloadId);

    /// Resume a paused download
    bool resume(const std::string& downloadId);

    //=========================================================================
    // Status and Progress
    //=========================================================================

    /// Get download info
    std::optional<DownloadInfo> getDownloadInfo(const std::string& downloadId) const;

    /// Get all downloads
    std::vector<DownloadInfo> getAllDownloads() const;

    /// Get downloads by status
    std::vector<DownloadInfo> getDownloadsByStatus(DownloadStatus status) const;

    /// Get overall progress
    double overallProgress() const;

    /// Get number of active downloads
    int activeDownloadCount() const;

    /// Get number of queued downloads
    int queuedDownloadCount() const;

    //=========================================================================
    // Events
    //=========================================================================

    /// Set callback for download start
    void onDownloadStart(DownloadEventCallback callback);

    /// Set callback for download progress
    void onDownloadProgress(DownloadEventCallback callback);

    /// Set callback for download complete
    void onDownloadComplete(DownloadEventCallback callback);

    /// Set callback for download error
    void onDownloadError(DownloadEventCallback callback);

    //=========================================================================
    // Control
    //=========================================================================

    /// Start processing the queue
    void start();

    /// Stop processing (completes active downloads)
    void stop();

    /// Wait for all downloads to complete
    void waitForAll();

    /// Check if manager is running
    bool isRunning() const;

private:
    DownloadManagerConfig config_;
    ClientPtr client_;

    std::queue<std::string> downloadQueue_;
    std::map<std::string, DownloadInfo> downloads_;
    std::map<std::string, std::future<void>> activeFutures_;

    mutable std::mutex mutex_;
    std::condition_variable cv_;
    std::atomic<bool> running_{false};
    std::atomic<int> activeCount_{0};

    std::thread workerThread_;

    // Callbacks
    DownloadEventCallback onStart_;
    DownloadEventCallback onProgress_;
    DownloadEventCallback onComplete_;
    DownloadEventCallback onError_;

    void workerLoop();
    void processDownload(const std::string& downloadId);
    std::string generateId();
    bool verifyMd5(const std::filesystem::path& file, const std::string& expected);
    void notifyProgress(const std::string& downloadId, size_t downloaded, size_t total);
};

} // namespace modular::fluent
```

---

### Task 7.1.2: Implement Download Manager
**File**: `src/fluent/DownloadManager.cpp`
**Estimated Time**: 4 hours

```cpp
#include "DownloadManager.h"
#include "Utils.h"

#include <openssl/md5.h>
#include <fstream>
#include <random>

namespace modular::fluent {

DownloadManager::DownloadManager(DownloadManagerConfig config)
    : config_(std::move(config))
    , client_(createFluentClient())
{}

DownloadManager::DownloadManager(ClientPtr client, DownloadManagerConfig config)
    : config_(std::move(config))
    , client_(std::move(client))
{}

DownloadManager::~DownloadManager() {
    stop();
}

//=============================================================================
// Queue Management
//=============================================================================

std::string DownloadManager::generateId() {
    static std::random_device rd;
    static std::mt19937 gen(rd());
    static std::uniform_int_distribution<> dis(0, 15);
    static const char* hex = "0123456789abcdef";

    std::string id;
    for (int i = 0; i < 16; ++i) {
        id += hex[dis(gen)];
    }
    return id;
}

std::string DownloadManager::enqueue(
    const std::string& url,
    const std::filesystem::path& outputPath,
    const std::optional<std::string>& expectedMd5
) {
    std::lock_guard<std::mutex> lock(mutex_);

    std::string id = generateId();

    DownloadInfo info;
    info.id = id;
    info.url = url;
    info.outputPath = outputPath;
    info.status = DownloadStatus::Queued;
    info.totalBytes = 0;
    info.downloadedBytes = 0;
    info.expectedMd5 = expectedMd5;

    downloads_[id] = std::move(info);
    downloadQueue_.push(id);

    cv_.notify_one();

    return id;
}

std::vector<std::string> DownloadManager::enqueueAll(
    const std::vector<std::pair<std::string, std::filesystem::path>>& downloads
) {
    std::vector<std::string> ids;
    ids.reserve(downloads.size());

    for (const auto& [url, path] : downloads) {
        ids.push_back(enqueue(url, path));
    }

    return ids;
}

bool DownloadManager::cancel(const std::string& downloadId) {
    std::lock_guard<std::mutex> lock(mutex_);

    auto it = downloads_.find(downloadId);
    if (it == downloads_.end()) return false;

    if (it->second.status == DownloadStatus::Queued) {
        it->second.status = DownloadStatus::Cancelled;
        return true;
    }

    // TODO: Support cancelling active downloads
    return false;
}

void DownloadManager::cancelAll() {
    std::lock_guard<std::mutex> lock(mutex_);

    for (auto& [id, info] : downloads_) {
        if (info.status == DownloadStatus::Queued) {
            info.status = DownloadStatus::Cancelled;
        }
    }
}

//=============================================================================
// Status and Progress
//=============================================================================

std::optional<DownloadInfo> DownloadManager::getDownloadInfo(
    const std::string& downloadId
) const {
    std::lock_guard<std::mutex> lock(mutex_);

    auto it = downloads_.find(downloadId);
    if (it == downloads_.end()) return std::nullopt;
    return it->second;
}

std::vector<DownloadInfo> DownloadManager::getAllDownloads() const {
    std::lock_guard<std::mutex> lock(mutex_);

    std::vector<DownloadInfo> result;
    for (const auto& [id, info] : downloads_) {
        result.push_back(info);
    }
    return result;
}

std::vector<DownloadInfo> DownloadManager::getDownloadsByStatus(
    DownloadStatus status
) const {
    std::lock_guard<std::mutex> lock(mutex_);

    std::vector<DownloadInfo> result;
    for (const auto& [id, info] : downloads_) {
        if (info.status == status) {
            result.push_back(info);
        }
    }
    return result;
}

double DownloadManager::overallProgress() const {
    std::lock_guard<std::mutex> lock(mutex_);

    size_t totalBytes = 0;
    size_t downloadedBytes = 0;

    for (const auto& [id, info] : downloads_) {
        totalBytes += info.totalBytes;
        downloadedBytes += info.downloadedBytes;
    }

    return totalBytes > 0 ? (downloadedBytes * 100.0 / totalBytes) : 0;
}

int DownloadManager::activeDownloadCount() const {
    return activeCount_.load();
}

int DownloadManager::queuedDownloadCount() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return static_cast<int>(downloadQueue_.size());
}

//=============================================================================
// Events
//=============================================================================

void DownloadManager::onDownloadStart(DownloadEventCallback callback) {
    onStart_ = std::move(callback);
}

void DownloadManager::onDownloadProgress(DownloadEventCallback callback) {
    onProgress_ = std::move(callback);
}

void DownloadManager::onDownloadComplete(DownloadEventCallback callback) {
    onComplete_ = std::move(callback);
}

void DownloadManager::onDownloadError(DownloadEventCallback callback) {
    onError_ = std::move(callback);
}

//=============================================================================
// Control
//=============================================================================

void DownloadManager::start() {
    if (running_.exchange(true)) return;  // Already running

    workerThread_ = std::thread(&DownloadManager::workerLoop, this);
}

void DownloadManager::stop() {
    running_ = false;
    cv_.notify_all();

    if (workerThread_.joinable()) {
        workerThread_.join();
    }
}

void DownloadManager::waitForAll() {
    std::unique_lock<std::mutex> lock(mutex_);

    cv_.wait(lock, [this]() {
        return downloadQueue_.empty() && activeCount_ == 0;
    });
}

bool DownloadManager::isRunning() const {
    return running_.load();
}

//=============================================================================
// Worker Thread
//=============================================================================

void DownloadManager::workerLoop() {
    while (running_) {
        std::string downloadId;

        {
            std::unique_lock<std::mutex> lock(mutex_);

            cv_.wait(lock, [this]() {
                return !running_ ||
                       (!downloadQueue_.empty() &&
                        activeCount_ < config_.maxConcurrentDownloads);
            });

            if (!running_) break;
            if (downloadQueue_.empty()) continue;

            downloadId = downloadQueue_.front();
            downloadQueue_.pop();

            auto& info = downloads_[downloadId];
            if (info.status != DownloadStatus::Queued) continue;

            info.status = DownloadStatus::Downloading;
            info.startTime = std::chrono::steady_clock::now();
            ++activeCount_;
        }

        // Process download outside lock
        processDownload(downloadId);
    }
}

void DownloadManager::processDownload(const std::string& downloadId) {
    DownloadInfo infoCopy;
    {
        std::lock_guard<std::mutex> lock(mutex_);
        infoCopy = downloads_[downloadId];
    }

    if (onStart_) onStart_(infoCopy);

    try {
        client_->getAsync(infoCopy.url)
            ->downloadTo(infoCopy.outputPath,
                [this, downloadId](size_t downloaded, size_t total) {
                    notifyProgress(downloadId, downloaded, total);
                });

        // Verify checksum if expected
        if (config_.verifyChecksum && infoCopy.expectedMd5) {
            if (!verifyMd5(infoCopy.outputPath, *infoCopy.expectedMd5)) {
                throw std::runtime_error("MD5 checksum mismatch");
            }
        }

        {
            std::lock_guard<std::mutex> lock(mutex_);
            auto& info = downloads_[downloadId];
            info.status = DownloadStatus::Completed;
            info.endTime = std::chrono::steady_clock::now();
            infoCopy = info;
        }

        if (onComplete_) onComplete_(infoCopy);

    } catch (const std::exception& e) {
        {
            std::lock_guard<std::mutex> lock(mutex_);
            auto& info = downloads_[downloadId];
            info.status = DownloadStatus::Failed;
            info.error = e.what();
            info.endTime = std::chrono::steady_clock::now();
            infoCopy = info;
        }

        if (config_.deletePartialOnFailure) {
            std::filesystem::remove(infoCopy.outputPath);
        }

        if (onError_) onError_(infoCopy);
    }

    --activeCount_;
    cv_.notify_all();
}

void DownloadManager::notifyProgress(
    const std::string& downloadId,
    size_t downloaded,
    size_t total
) {
    {
        std::lock_guard<std::mutex> lock(mutex_);
        auto& info = downloads_[downloadId];
        info.downloadedBytes = downloaded;
        info.totalBytes = total;
    }

    if (onProgress_) {
        auto info = getDownloadInfo(downloadId);
        if (info) onProgress_(*info);
    }
}

bool DownloadManager::verifyMd5(
    const std::filesystem::path& file,
    const std::string& expected
) {
    std::ifstream f(file, std::ios::binary);
    if (!f) return false;

    MD5_CTX ctx;
    MD5_Init(&ctx);

    char buffer[8192];
    while (f.read(buffer, sizeof(buffer))) {
        MD5_Update(&ctx, buffer, f.gcount());
    }
    MD5_Update(&ctx, buffer, f.gcount());

    unsigned char digest[MD5_DIGEST_LENGTH];
    MD5_Final(digest, &ctx);

    std::ostringstream oss;
    for (int i = 0; i < MD5_DIGEST_LENGTH; ++i) {
        oss << std::hex << std::setw(2) << std::setfill('0')
            << static_cast<int>(digest[i]);
    }

    return oss.str() == expected;
}

} // namespace modular::fluent
```

---

## Day 2-3: Comprehensive Testing

### Task 7.2.1: Create End-to-End Tests
**File**: `tests/fluent/EndToEndTest.cpp`
**Estimated Time**: 3 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>
#include "fluent/DownloadManager.h"
#include "api/NexusModsApi.h"
#include "api/GameBananaApi.h"

using namespace modular::fluent;
using namespace modular::api;

class EndToEndTest : public ::testing::Test {
protected:
    std::filesystem::path tempDir;

    void SetUp() override {
        tempDir = std::filesystem::temp_directory_path() / "fluent_e2e_test";
        std::filesystem::create_directories(tempDir);
    }

    void TearDown() override {
        std::filesystem::remove_all(tempDir);
    }
};

TEST_F(EndToEndTest, DISABLED_FullNexusModsWorkflow) {
    // This test requires a valid API key
    const char* apiKey = std::getenv("NEXUSMODS_API_KEY");
    if (!apiKey) GTEST_SKIP();

    nexus::NexusModsApi api(apiKey);

    // 1. Validate user
    auto user = api.validateUser();
    EXPECT_FALSE(user.username.empty());

    // 2. Get tracked mods
    auto mods = api.getTrackedMods();
    // May be empty if user has no tracked mods

    // 3. Get games list
    auto games = api.getGames();
    EXPECT_FALSE(games.empty());
}

TEST_F(EndToEndTest, DownloadManagerQueue) {
    DownloadManager manager;

    std::vector<DownloadInfo> completedDownloads;
    manager.onDownloadComplete([&](const DownloadInfo& info) {
        completedDownloads.push_back(info);
    });

    // Queue test downloads (using httpbin for testing)
    auto id1 = manager.enqueue(
        "https://httpbin.org/bytes/1024",
        tempDir / "file1.bin"
    );
    auto id2 = manager.enqueue(
        "https://httpbin.org/bytes/2048",
        tempDir / "file2.bin"
    );

    manager.start();
    manager.waitForAll();

    EXPECT_EQ(completedDownloads.size(), 2);
    EXPECT_TRUE(std::filesystem::exists(tempDir / "file1.bin"));
    EXPECT_TRUE(std::filesystem::exists(tempDir / "file2.bin"));
}

TEST_F(EndToEndTest, DownloadWithProgress) {
    auto client = createFluentClient();

    std::vector<std::pair<size_t, size_t>> progressUpdates;

    client->getAsync("https://httpbin.org/bytes/10240")
        ->downloadTo(tempDir / "progress_test.bin",
            [&](size_t downloaded, size_t total) {
                progressUpdates.emplace_back(downloaded, total);
            });

    EXPECT_FALSE(progressUpdates.empty());
    EXPECT_EQ(progressUpdates.back().first, progressUpdates.back().second);
}
```

---

### Task 7.2.2: Create Performance Benchmarks
**File**: `tests/fluent/PerformanceTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>
#include <chrono>

using namespace modular::fluent;

class PerformanceTest : public ::testing::Test {
protected:
    ClientPtr client;

    void SetUp() override {
        client = createFluentClient("https://httpbin.org");
    }
};

TEST_F(PerformanceTest, DISABLED_RequestLatency) {
    const int iterations = 10;
    std::vector<std::chrono::milliseconds> latencies;

    for (int i = 0; i < iterations; ++i) {
        auto start = std::chrono::steady_clock::now();

        auto response = client->getAsync("get")->asResponse();

        auto end = std::chrono::steady_clock::now();
        latencies.push_back(std::chrono::duration_cast<std::chrono::milliseconds>(end - start));
    }

    // Calculate average
    auto sum = std::accumulate(latencies.begin(), latencies.end(),
        std::chrono::milliseconds{0});
    auto avg = sum / iterations;

    std::cout << "Average latency: " << avg.count() << "ms\n";
    // Should be reasonable for network requests
}

TEST_F(PerformanceTest, DISABLED_ConcurrentRequests) {
    const int concurrency = 10;
    std::vector<std::future<ResponsePtr>> futures;

    auto start = std::chrono::steady_clock::now();

    for (int i = 0; i < concurrency; ++i) {
        futures.push_back(client->getAsync("get")->asResponseAsync());
    }

    for (auto& f : futures) {
        f.get();
    }

    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);

    std::cout << "Concurrent requests (" << concurrency << "): "
              << elapsed.count() << "ms\n";
}

TEST_F(PerformanceTest, MemoryUsageBaseline) {
    // Create many requests to check for memory leaks
    for (int i = 0; i < 100; ++i) {
        auto request = client->getAsync("get")
            ->withArgument("iteration", std::to_string(i))
            .withHeader("X-Test", "value");
        // Request goes out of scope - should be cleaned up
    }

    // Manual memory check would go here
    SUCCEED();
}
```

---

## Day 4: Documentation

### Task 7.4.1: Create API Reference
**File**: `docs/fluent/API_REFERENCE.md`
**Estimated Time**: 3 hours

```markdown
# Fluent HTTP Client API Reference

## Quick Start

```cpp
#include <fluent/Fluent.h>
using namespace modular::fluent;

// Create client
auto client = createFluentClient("https://api.example.com");

// Simple GET
auto data = client->getAsync("users")
    ->as<std::vector<User>>();

// POST with body
auto result = client->postAsync("users", newUser)
    ->as<User>();

// Download with progress
client->getAsync("files/large.zip")
    ->downloadTo("/tmp/large.zip", [](size_t dl, size_t total) {
        std::cout << dl * 100 / total << "%\n";
    });
```

## IFluentClient

### Construction

```cpp
// Basic
auto client = createFluentClient("https://api.example.com");

// With rate limiter
auto client = createFluentClient(baseUrl, rateLimiter, logger);
```

### HTTP Methods

| Method | Description |
|--------|-------------|
| `getAsync(resource)` | Create GET request |
| `postAsync(resource)` | Create POST request |
| `postAsync(resource, body)` | POST with JSON body |
| `putAsync(resource)` | Create PUT request |
| `patchAsync(resource)` | Create PATCH request |
| `deleteAsync(resource)` | Create DELETE request |
| `headAsync(resource)` | Create HEAD request |
| `sendAsync(method, resource)` | Custom method |

### Configuration

```cpp
client->setBaseUrl("https://api.example.com")
      .setUserAgent("MyApp/1.0")
      .setBearerAuth(token)
      .setRequestTimeout(std::chrono::seconds{60});
```

## IRequest

### Query Parameters

```cpp
request->withArgument("key", "value")
       .withArgument("page", 1)  // Numeric values supported
       .withArguments({{"a", "1"}, {"b", "2"}});
```

### Headers

```cpp
request->withHeader("Accept", "application/json")
       .withHeader("X-Custom", "value")
       .withBearerAuth(token)
       .withBasicAuth(username, password);
```

### Body

```cpp
// JSON from object
request->withJsonBody(myObject);

// Form data
request->withFormBody({{"field", "value"}});

// Builder pattern
request->withBody([](IBodyBuilder& b) {
    return b.formUrlEncoded({{"key", "value"}});
});
```

### Response

```cpp
// Typed
auto user = request->as<User>();
auto users = request->asArray<User>();

// Raw
auto json = request->asJson();
auto str = request->asString();
auto bytes = request->asByteArray();

// Response object
auto response = request->asResponse();

// Download
request->downloadTo("/path/to/file", progressCallback);
```

### Async

```cpp
// All methods have async versions
auto future = request->asAsync<User>();
auto user = future.get();
```

## IResponse

```cpp
if (response->isSuccessStatusCode()) {
    auto data = response->as<MyType>();
}

// Access details
int status = response->statusCode();
std::string type = response->contentType();
int64_t length = response->contentLength();
std::string header = response->header("X-Custom");
```

## Filters

### Adding Filters

```cpp
client->addFilter(FilterFactory::createErrorFilter());
client->addFilter(FilterFactory::createLoggingFilter(logger));
client->addFilter(FilterFactory::createRateLimitFilter(limiter));
client->addFilter(FilterFactory::createBearerAuth(token));
```

### Custom Filter

```cpp
class MyFilter : public IHttpFilter {
    void onRequest(IRequest& request) override {
        request.withHeader("X-Custom", getValue());
    }
    void onResponse(IResponse& response, bool throwOnError) override {
        // Process response
    }
};
```

## Error Handling

```cpp
try {
    auto data = client->getAsync("resource")->as<Data>();
} catch (const ApiException& e) {
    std::cerr << "HTTP " << e.statusCode() << ": " << e.what() << "\n";
    std::cerr << "Body: " << e.responseBody() << "\n";
} catch (const RateLimitException& e) {
    std::cerr << "Rate limited, retry after " << e.retryAfter().count() << "s\n";
} catch (const NetworkException& e) {
    std::cerr << "Network error: " << e.what() << "\n";
}
```

## Download Manager

```cpp
DownloadManager manager;

manager.onDownloadProgress([](const DownloadInfo& info) {
    std::cout << info.id << ": " << info.progressPercent() << "%\n";
});

manager.enqueue("https://example.com/file1.zip", "/tmp/file1.zip");
manager.enqueue("https://example.com/file2.zip", "/tmp/file2.zip");

manager.start();
manager.waitForAll();
```
```

---

### Task 7.4.2: Create Complete Example Application
**File**: `examples/mod_downloader.cpp`
**Estimated Time**: 2 hours

```cpp
/// @file mod_downloader.cpp
/// @brief Example application demonstrating the fluent HTTP client
///
/// This example shows how to:
/// - Configure the fluent client with filters
/// - Use the NexusMods API wrapper
/// - Download files with progress
/// - Handle errors properly

#include <fluent/Fluent.h>
#include <fluent/DownloadManager.h>
#include <api/NexusModsApi.h>

#include <iostream>
#include <iomanip>

using namespace modular;
using namespace modular::fluent;
using namespace modular::api::nexus;

void printProgress(const DownloadInfo& info) {
    std::cout << "\r[" << std::setw(3) << static_cast<int>(info.progressPercent()) << "%] "
              << info.outputPath.filename().string()
              << " (" << std::fixed << std::setprecision(1)
              << (info.speedBytesPerSecond() / 1024.0 / 1024.0) << " MB/s)"
              << std::flush;
}

int main(int argc, char* argv[]) {
    if (argc < 2) {
        std::cerr << "Usage: mod_downloader <api_key> [game_domain]\n";
        return 1;
    }

    std::string apiKey = argv[1];
    std::string gameDomain = argc > 2 ? argv[2] : "skyrimspecialedition";

    try {
        // Configure API client
        NexusModsConfig config;
        config.apiKey = apiKey;
        config.appName = "ModDownloader";
        config.appVersion = "1.0";

        NexusModsApi api(config);

        // Validate API key
        std::cout << "Validating API key...\n";
        auto user = api.validateUser();
        std::cout << "Hello, " << user.username << "!\n";
        std::cout << "Premium: " << (user.isPremium ? "Yes" : "No") << "\n\n";

        // Get tracked mods
        std::cout << "Fetching tracked mods for " << gameDomain << "...\n";
        auto mods = api.getTrackedMods(gameDomain);

        if (mods.empty()) {
            std::cout << "No tracked mods found.\n";
            return 0;
        }

        std::cout << "Found " << mods.size() << " tracked mods:\n";
        for (const auto& mod : mods) {
            std::cout << "  - " << mod.modName << " (ID: " << mod.modId << ")\n";
        }

        // Set up download manager
        DownloadManagerConfig dmConfig;
        dmConfig.maxConcurrentDownloads = 2;
        dmConfig.verifyChecksum = true;

        DownloadManager manager(api.client().shared_from_this(), dmConfig);

        manager.onDownloadProgress(printProgress);

        manager.onDownloadComplete([](const DownloadInfo& info) {
            std::cout << "\n✓ Completed: " << info.outputPath.filename().string() << "\n";
        });

        manager.onDownloadError([](const DownloadInfo& info) {
            std::cout << "\n✗ Failed: " << info.outputPath.filename().string()
                      << " - " << info.error.value_or("Unknown error") << "\n";
        });

        // Queue downloads for first mod
        if (!mods.empty()) {
            auto files = api.getModFiles(gameDomain, mods[0].modId);

            for (const auto& file : files.files) {
                auto links = api.getDownloadLinks(gameDomain, mods[0].modId, file.fileId);

                if (!links.empty()) {
                    manager.enqueue(
                        links[0].uri,
                        std::filesystem::current_path() / file.fileName
                    );
                }
            }
        }

        // Start downloads
        std::cout << "\nStarting downloads...\n";
        manager.start();
        manager.waitForAll();

        std::cout << "\nAll downloads complete!\n";

        // Show rate limit status
        auto limits = api.getRateLimitInfo();
        std::cout << "\nRate limits: "
                  << limits.dailyRemaining << "/" << limits.dailyLimit << " daily, "
                  << limits.hourlyRemaining << "/" << limits.hourlyLimit << " hourly\n";

    } catch (const ApiException& e) {
        std::cerr << "API Error: HTTP " << e.statusCode() << " - " << e.what() << "\n";
        return 1;
    } catch (const NetworkException& e) {
        std::cerr << "Network Error: " << e.what() << "\n";
        return 1;
    } catch (const std::exception& e) {
        std::cerr << "Error: " << e.what() << "\n";
        return 1;
    }

    return 0;
}
```

---

## Day 5: Final Integration and Release Preparation

### Task 7.5.1: Update Main CMakeLists.txt
**File**: `CMakeLists.txt` (root)
**Estimated Time**: 1 hour

Add all fluent components to the main build:

```cmake
# Add fluent HTTP client library
add_subdirectory(src/fluent)
add_subdirectory(src/fluent/filters)
add_subdirectory(src/api)

# Examples (optional)
option(BUILD_EXAMPLES "Build example applications" OFF)
if(BUILD_EXAMPLES)
    add_subdirectory(examples)
endif()

# Tests
if(BUILD_TESTING)
    add_subdirectory(tests/fluent)
    add_subdirectory(tests/api)
endif()
```

---

### Task 7.5.2: Create Release Checklist
**File**: `docs/RELEASE_CHECKLIST.md`
**Estimated Time**: 1 hour

```markdown
# Release Checklist for Fluent HTTP Client Integration

## Pre-Release

- [ ] All unit tests pass
- [ ] All integration tests pass (where applicable)
- [ ] Memory leak check with Valgrind/ASan
- [ ] Performance benchmarks are acceptable
- [ ] Documentation is complete and accurate
- [ ] Example code compiles and runs correctly
- [ ] API key handling is secure (no hardcoding)

## Code Quality

- [ ] No compiler warnings with -Wall -Wextra -Wpedantic
- [ ] Code follows project style guide
- [ ] All public APIs are documented with Doxygen
- [ ] Error messages are clear and helpful
- [ ] Logging is consistent and useful

## Compatibility

- [ ] Builds on Linux (GCC 8+)
- [ ] Builds on macOS (Clang)
- [ ] Builds on Windows (MSVC 2019+)
- [ ] Works with CMake 3.20+
- [ ] Dependencies are properly specified

## Migration

- [ ] Migration guide is complete
- [ ] Old API still works (if backwards compatibility needed)
- [ ] Breaking changes are documented
- [ ] Deprecation warnings are in place

## Final Steps

- [ ] Version number updated
- [ ] Changelog updated
- [ ] Git tags created
- [ ] Release notes written
```

---

## Deliverables Checklist

### Source Files
- [ ] `src/fluent/DownloadManager.h/.cpp`
- [ ] Updated CMakeLists.txt files

### Test Files
- [ ] `tests/fluent/EndToEndTest.cpp`
- [ ] `tests/fluent/PerformanceTest.cpp`
- [ ] `tests/fluent/DownloadManagerTest.cpp`

### Documentation
- [ ] `docs/fluent/API_REFERENCE.md`
- [ ] `docs/RELEASE_CHECKLIST.md`

### Examples
- [ ] `examples/mod_downloader.cpp`
- [ ] `examples/CMakeLists.txt`

---

## Definition of Done

Week 7 is complete when:

1. ✅ Download manager with queue/progress works
2. ✅ End-to-end tests pass
3. ✅ Performance is acceptable
4. ✅ API reference is complete
5. ✅ Example application works
6. ✅ All documentation is complete
7. ✅ Release checklist is satisfied
8. ✅ Code is merged to main branch

---

## Project Completion Summary

At the end of Week 7, the Modular project will have:

1. **Fluent HTTP Client Library**
   - Modern C++ fluent API inspired by FluentHttpClient
   - Full filter/middleware support
   - Retry with exponential backoff
   - Rate limiting integration

2. **API Wrappers**
   - NexusMods API with type-safe methods
   - GameBanana API with HTML parsing
   - Unified IModRepository interface

3. **Download Management**
   - Queue-based download manager
   - Concurrent downloads
   - Progress callbacks
   - MD5 verification

4. **Documentation**
   - Complete API reference
   - Migration guides
   - Example applications
   - Release checklist

The fluent HTTP client brings the best of FluentHttpClient's developer experience to C++ while maintaining Modular's strengths in rate limiting and download management.
