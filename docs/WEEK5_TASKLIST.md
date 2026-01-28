# Week 5 Task List: NexusMods API Migration

## Overview

**Objective**: Migrate Modular's NexusMods API client from direct HttpClient usage to the new fluent API, demonstrating the improved developer experience.

**Prerequisites**: Week 4 completed (all filters implemented)

**Duration**: 5 working days

**Output**: NexusMods API client rewritten with fluent HTTP client

---

## Migration Strategy

```
Current Implementation                    New Implementation
══════════════════════                    ══════════════════

┌─────────────────────┐                   ┌─────────────────────┐
│    NexusMods.h      │                   │   NexusModsApi.h    │
│                     │                   │                     │
│ - http_get()        │      ────────►    │ - Fluent client     │
│ - Manual headers    │                   │ - Type-safe methods │
│ - Manual JSON parse │                   │ - Automatic parsing │
│ - Callback progress │                   │ - Progress support  │
└─────────────────────┘                   └─────────────────────┘
         │                                         │
         ▼                                         ▼
┌─────────────────────┐                   ┌─────────────────────┐
│    HttpClient       │                   │    FluentClient     │
│    (libcurl)        │                   │    (fluent API)     │
└─────────────────────┘                   └─────────────────────┘
```

---

## API Endpoints to Migrate

| Endpoint | Current Function | New Method |
|----------|-----------------|------------|
| `/v1/users/validate` | `get_user_info()` | `validateUser()` |
| `/v1/user/tracked_mods` | `get_tracked_mods()` | `getTrackedMods()` |
| `/v1/games/{domain}/mods/{id}/files` | `get_file_ids()` | `getModFiles()` |
| `/v1/games/{domain}/mods/{id}/files/{file_id}/download_link` | `generate_download_links()` | `getDownloadLinks()` |
| Download files | `download_files()` | `downloadFile()` |

---

## Directory Structure

```
src/api/
├── NexusModsApi.h           # New fluent API interface
├── NexusModsApi.cpp         # Implementation
├── NexusModsTypes.h         # Data types (TrackedMod, FileInfo, etc.)
├── NexusModsException.h     # Domain-specific exceptions
└── CMakeLists.txt

tests/api/
├── NexusModsApiTest.cpp     # Unit tests
├── NexusModsMockTest.cpp    # Mock-based tests
└── NexusModsIntegrationTest.cpp  # Live API tests (disabled by default)
```

---

## Day 1: Data Types and API Interface

### Task 5.1.1: Create NexusMods Data Types
**File**: `src/api/NexusModsTypes.h`
**Estimated Time**: 2 hours

**Instructions**:

Define strongly-typed data structures for NexusMods API responses:

```cpp
#pragma once

#include <nlohmann/json.hpp>
#include <string>
#include <vector>
#include <chrono>
#include <optional>

namespace modular::api::nexus {

//=============================================================================
// User Types
//=============================================================================

struct UserProfile {
    int userId;
    std::string username;
    std::string email;
    std::string profileUrl;
    bool isPremium;
    bool isSupporter;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(UserProfile,
        userId, username, email, profileUrl, isPremium, isSupporter)
};

//=============================================================================
// Mod Types
//=============================================================================

struct TrackedMod {
    int modId;
    std::string domainName;
    std::string modName;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(TrackedMod, modId, domainName, modName)
};

struct ModInfo {
    int modId;
    int gameId;
    std::string domainName;
    std::string name;
    std::string summary;
    std::string description;
    std::string version;
    std::string author;
    std::string uploadedBy;
    int uploadedUsersProfileId;
    bool containsAdultContent;
    std::string status;
    bool available;
    std::string pictureUrl;

    int endorsementCount;
    int uniqueDownloads;

    std::chrono::system_clock::time_point createdTime;
    std::chrono::system_clock::time_point updatedTime;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(ModInfo,
        modId, gameId, domainName, name, summary, version, author,
        containsAdultContent, status, available, pictureUrl,
        endorsementCount, uniqueDownloads)
};

//=============================================================================
// File Types
//=============================================================================

struct FileCategory {
    int categoryId;
    std::string name;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(FileCategory, categoryId, name)
};

struct ModFile {
    int fileId;
    std::string name;
    std::string version;
    int categoryId;
    std::string categoryName;
    bool isPrimary;
    int64_t size;  // Size in bytes
    std::string fileName;
    std::chrono::system_clock::time_point uploadedTime;
    std::optional<std::string> modVersion;
    std::optional<std::string> externalVirusScanUrl;
    std::optional<std::string> description;
    std::optional<std::string> changelog;
    std::optional<std::string> contentPreviewLink;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(ModFile,
        fileId, name, version, categoryId, categoryName, isPrimary,
        size, fileName, uploadedTime)
};

struct ModFilesResponse {
    std::vector<ModFile> files;
    std::vector<FileCategory> fileUpdates;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(ModFilesResponse, files, fileUpdates)
};

//=============================================================================
// Download Types
//=============================================================================

struct DownloadLink {
    std::string name;
    std::string shortName;
    std::string uri;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(DownloadLink, name, shortName, uri)
};

//=============================================================================
// Game Types
//=============================================================================

struct GameInfo {
    int id;
    std::string name;
    std::string forumUrl;
    std::string nexusmodsUrl;
    std::string genre;
    int fileCount;
    int downloads;
    std::string domainName;
    int approvedDate;
    int fileViews;
    int authors;
    int fileEndorsements;
    int mods;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(GameInfo,
        id, name, domainName, genre, fileCount, downloads, mods)
};

//=============================================================================
// Rate Limit Info (from headers)
//=============================================================================

struct RateLimitInfo {
    int dailyLimit;
    int dailyRemaining;
    std::chrono::system_clock::time_point dailyReset;
    int hourlyLimit;
    int hourlyRemaining;
    std::chrono::system_clock::time_point hourlyReset;
};

} // namespace modular::api::nexus
```

---

### Task 5.1.2: Create NexusMods API Interface
**File**: `src/api/NexusModsApi.h`
**Estimated Time**: 2.5 hours

**Instructions**:

```cpp
#pragma once

#include "NexusModsTypes.h"
#include <fluent/Fluent.h>
#include <fluent/filters/FilterFactory.h>

#include <filesystem>
#include <future>

namespace modular::api::nexus {

/// Configuration for NexusMods API client
struct NexusModsConfig {
    /// API key from NexusMods
    std::string apiKey;

    /// Application name for User-Agent
    std::string appName = "Modular";

    /// Application version for User-Agent
    std::string appVersion = "1.0";

    /// Contact email (optional, for API identification)
    std::string contactEmail;

    /// Rate limiter instance
    fluent::RateLimiterPtr rateLimiter;

    /// Logger instance
    std::shared_ptr<ILogger> logger;

    /// Enable request logging
    bool enableLogging = true;

    /// Request timeout
    std::chrono::seconds timeout{60};
};

/// Progress callback for downloads
using DownloadProgress = fluent::ProgressCallback;

/// Fluent wrapper for the NexusMods API
///
/// Example usage:
/// @code
/// NexusModsConfig config;
/// config.apiKey = "your-api-key";
/// config.rateLimiter = createRateLimiter();
///
/// NexusModsApi api(config);
///
/// // Validate user
/// auto user = api.validateUser();
/// std::cout << "Hello, " << user.username << "!\n";
///
/// // Get tracked mods
/// auto mods = api.getTrackedMods();
/// for (const auto& mod : mods) {
///     std::cout << mod.modName << " (" << mod.domainName << ")\n";
/// }
///
/// // Download a mod file
/// api.downloadFile("skyrimspecialedition", 12345, 67890, "/path/to/file.zip",
///     [](size_t downloaded, size_t total) {
///         std::cout << "\rProgress: " << (downloaded * 100 / total) << "%" << std::flush;
///     });
/// @endcode
class NexusModsApi {
public:
    /// Create API client with configuration
    explicit NexusModsApi(NexusModsConfig config);

    /// Create API client with just API key (uses defaults)
    explicit NexusModsApi(const std::string& apiKey);

    ~NexusModsApi() = default;

    // Non-copyable, movable
    NexusModsApi(const NexusModsApi&) = delete;
    NexusModsApi& operator=(const NexusModsApi&) = delete;
    NexusModsApi(NexusModsApi&&) = default;
    NexusModsApi& operator=(NexusModsApi&&) = default;

    //=========================================================================
    // User Endpoints
    //=========================================================================

    /// Validate API key and get user profile
    /// GET /v1/users/validate
    UserProfile validateUser();

    /// Async version
    std::future<UserProfile> validateUserAsync();

    //=========================================================================
    // Tracked Mods Endpoints
    //=========================================================================

    /// Get all tracked mods for the user
    /// GET /v1/user/tracked_mods
    std::vector<TrackedMod> getTrackedMods();

    /// Get tracked mods for a specific game
    std::vector<TrackedMod> getTrackedMods(const std::string& gameDomain);

    /// Async version
    std::future<std::vector<TrackedMod>> getTrackedModsAsync();

    /// Check if a specific mod is tracked
    bool isModTracked(const std::string& gameDomain, int modId);

    /// Track a mod
    /// POST /v1/user/tracked_mods
    void trackMod(const std::string& gameDomain, int modId);

    /// Untrack a mod
    /// DELETE /v1/user/tracked_mods
    void untrackMod(const std::string& gameDomain, int modId);

    //=========================================================================
    // Mod Info Endpoints
    //=========================================================================

    /// Get mod information
    /// GET /v1/games/{domain}/mods/{id}
    ModInfo getModInfo(const std::string& gameDomain, int modId);

    /// Async version
    std::future<ModInfo> getModInfoAsync(const std::string& gameDomain, int modId);

    //=========================================================================
    // Files Endpoints
    //=========================================================================

    /// Get list of files for a mod
    /// GET /v1/games/{domain}/mods/{id}/files
    ModFilesResponse getModFiles(const std::string& gameDomain, int modId);

    /// Async version
    std::future<ModFilesResponse> getModFilesAsync(
        const std::string& gameDomain, int modId);

    /// Get download links for a file
    /// GET /v1/games/{domain}/mods/{id}/files/{file_id}/download_link
    std::vector<DownloadLink> getDownloadLinks(
        const std::string& gameDomain,
        int modId,
        int fileId,
        const std::string& key = "",
        int expiry = 0
    );

    /// Async version
    std::future<std::vector<DownloadLink>> getDownloadLinksAsync(
        const std::string& gameDomain,
        int modId,
        int fileId,
        const std::string& key = "",
        int expiry = 0
    );

    //=========================================================================
    // Download Operations
    //=========================================================================

    /// Download a mod file to disk
    /// @param gameDomain Game domain (e.g., "skyrimspecialedition")
    /// @param modId Mod ID
    /// @param fileId File ID
    /// @param outputPath Destination file path
    /// @param progress Progress callback
    void downloadFile(
        const std::string& gameDomain,
        int modId,
        int fileId,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    );

    /// Download from a direct URL (from getDownloadLinks)
    void downloadFromUrl(
        const std::string& url,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    );

    /// Async download
    std::future<void> downloadFileAsync(
        const std::string& gameDomain,
        int modId,
        int fileId,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    );

    //=========================================================================
    // Games Endpoints
    //=========================================================================

    /// Get list of all games
    /// GET /v1/games
    std::vector<GameInfo> getGames();

    /// Get info for a specific game
    /// GET /v1/games/{domain}
    GameInfo getGameInfo(const std::string& gameDomain);

    //=========================================================================
    // Rate Limit Access
    //=========================================================================

    /// Get current rate limit status
    RateLimitInfo getRateLimitInfo() const;

    /// Get underlying fluent client (for advanced usage)
    fluent::IFluentClient& client();

private:
    NexusModsConfig config_;
    fluent::ClientPtr client_;

    static constexpr const char* BASE_URL = "https://api.nexusmods.com";

    void setupClient();
    std::string buildUserAgent() const;
};

} // namespace modular::api::nexus
```

---

## Day 2: Core API Implementation

### Task 5.2.1: Implement NexusMods API Client
**File**: `src/api/NexusModsApi.cpp`
**Estimated Time**: 5 hours

**Instructions**:

```cpp
#include "NexusModsApi.h"
#include <fluent/filters/FilterFactory.h>

namespace modular::api::nexus {

//=============================================================================
// Constructors
//=============================================================================

NexusModsApi::NexusModsApi(NexusModsConfig config)
    : config_(std::move(config))
{
    setupClient();
}

NexusModsApi::NexusModsApi(const std::string& apiKey)
{
    config_.apiKey = apiKey;
    setupClient();
}

void NexusModsApi::setupClient() {
    using namespace fluent;
    using namespace fluent::filters;

    // Create client with base URL
    client_ = createFluentClient(BASE_URL);

    // Set user agent
    client_->setUserAgent(buildUserAgent());

    // Set timeout
    client_->setRequestTimeout(config_.timeout);

    // Add filters using factory
    auto filters = FilterFactory::createNexusModsFilters(
        config_.apiKey,
        config_.rateLimiter,
        config_.logger.get()
    );

    for (auto& filter : filters) {
        client_->addFilter(std::move(filter));
    }

    // Set default options
    client_->setOptions(RequestOptions{
        .ignoreHttpErrors = false,
        .ignoreNullArguments = true
    });
}

std::string NexusModsApi::buildUserAgent() const {
    std::string ua = config_.appName + "/" + config_.appVersion;
    if (!config_.contactEmail.empty()) {
        ua += " (" + config_.contactEmail + ")";
    }
    return ua;
}

//=============================================================================
// User Endpoints
//=============================================================================

UserProfile NexusModsApi::validateUser() {
    return client_->getAsync("v1/users/validate")
        ->as<UserProfile>();
}

std::future<UserProfile> NexusModsApi::validateUserAsync() {
    return client_->getAsync("v1/users/validate")
        ->asAsync<UserProfile>();
}

//=============================================================================
// Tracked Mods Endpoints
//=============================================================================

std::vector<TrackedMod> NexusModsApi::getTrackedMods() {
    return client_->getAsync("v1/user/tracked_mods")
        ->as<std::vector<TrackedMod>>();
}

std::vector<TrackedMod> NexusModsApi::getTrackedMods(const std::string& gameDomain) {
    auto allMods = getTrackedMods();

    std::vector<TrackedMod> filtered;
    std::copy_if(allMods.begin(), allMods.end(), std::back_inserter(filtered),
        [&gameDomain](const TrackedMod& mod) {
            return mod.domainName == gameDomain;
        });

    return filtered;
}

std::future<std::vector<TrackedMod>> NexusModsApi::getTrackedModsAsync() {
    return client_->getAsync("v1/user/tracked_mods")
        ->asAsync<std::vector<TrackedMod>>();
}

bool NexusModsApi::isModTracked(const std::string& gameDomain, int modId) {
    auto mods = getTrackedMods(gameDomain);
    return std::any_of(mods.begin(), mods.end(),
        [modId](const TrackedMod& mod) { return mod.modId == modId; });
}

void NexusModsApi::trackMod(const std::string& gameDomain, int modId) {
    client_->postAsync("v1/user/tracked_mods")
        ->withArgument("domain_name", gameDomain)
        .withFormBody({
            {"domain_name", gameDomain},
            {"mod_id", std::to_string(modId)}
        })
        .asResponse();  // Just need to complete, no response body
}

void NexusModsApi::untrackMod(const std::string& gameDomain, int modId) {
    client_->deleteAsync("v1/user/tracked_mods")
        ->withArgument("domain_name", gameDomain)
        .withArgument("mod_id", std::to_string(modId))
        .asResponse();
}

//=============================================================================
// Mod Info Endpoints
//=============================================================================

ModInfo NexusModsApi::getModInfo(const std::string& gameDomain, int modId) {
    std::string path = "v1/games/" + gameDomain + "/mods/" + std::to_string(modId);
    return client_->getAsync(path)
        ->as<ModInfo>();
}

std::future<ModInfo> NexusModsApi::getModInfoAsync(
    const std::string& gameDomain, int modId
) {
    std::string path = "v1/games/" + gameDomain + "/mods/" + std::to_string(modId);
    return client_->getAsync(path)
        ->asAsync<ModInfo>();
}

//=============================================================================
// Files Endpoints
//=============================================================================

ModFilesResponse NexusModsApi::getModFiles(const std::string& gameDomain, int modId) {
    std::string path = "v1/games/" + gameDomain + "/mods/" +
                       std::to_string(modId) + "/files";
    return client_->getAsync(path)
        ->as<ModFilesResponse>();
}

std::future<ModFilesResponse> NexusModsApi::getModFilesAsync(
    const std::string& gameDomain, int modId
) {
    std::string path = "v1/games/" + gameDomain + "/mods/" +
                       std::to_string(modId) + "/files";
    return client_->getAsync(path)
        ->asAsync<ModFilesResponse>();
}

std::vector<DownloadLink> NexusModsApi::getDownloadLinks(
    const std::string& gameDomain,
    int modId,
    int fileId,
    const std::string& key,
    int expiry
) {
    std::string path = "v1/games/" + gameDomain + "/mods/" +
                       std::to_string(modId) + "/files/" +
                       std::to_string(fileId) + "/download_link";

    auto request = client_->getAsync(path);

    // Add optional NXM key parameters
    if (!key.empty()) {
        request->withArgument("key", key);
        if (expiry > 0) {
            request->withArgument("expires", std::to_string(expiry));
        }
    }

    return request->as<std::vector<DownloadLink>>();
}

std::future<std::vector<DownloadLink>> NexusModsApi::getDownloadLinksAsync(
    const std::string& gameDomain,
    int modId,
    int fileId,
    const std::string& key,
    int expiry
) {
    return std::async(std::launch::async, [=, this]() {
        return getDownloadLinks(gameDomain, modId, fileId, key, expiry);
    });
}

//=============================================================================
// Download Operations
//=============================================================================

void NexusModsApi::downloadFile(
    const std::string& gameDomain,
    int modId,
    int fileId,
    const std::filesystem::path& outputPath,
    DownloadProgress progress
) {
    // Get download links
    auto links = getDownloadLinks(gameDomain, modId, fileId);

    if (links.empty()) {
        throw fluent::ApiException(
            "No download links available",
            404, "Not Found", {}, ""
        );
    }

    // Use first link (typically CDN)
    downloadFromUrl(links[0].uri, outputPath, progress);
}

void NexusModsApi::downloadFromUrl(
    const std::string& url,
    const std::filesystem::path& outputPath,
    DownloadProgress progress
) {
    // Create a separate client for downloads (no auth needed for CDN)
    auto downloadClient = fluent::createFluentClient();
    downloadClient->setUserAgent(buildUserAgent());
    downloadClient->setRequestTimeout(std::chrono::seconds{600});  // 10 min for downloads

    downloadClient->getAsync(url)
        ->downloadTo(outputPath, progress);
}

std::future<void> NexusModsApi::downloadFileAsync(
    const std::string& gameDomain,
    int modId,
    int fileId,
    const std::filesystem::path& outputPath,
    DownloadProgress progress
) {
    return std::async(std::launch::async, [=, this]() {
        downloadFile(gameDomain, modId, fileId, outputPath, progress);
    });
}

//=============================================================================
// Games Endpoints
//=============================================================================

std::vector<GameInfo> NexusModsApi::getGames() {
    return client_->getAsync("v1/games")
        ->as<std::vector<GameInfo>>();
}

GameInfo NexusModsApi::getGameInfo(const std::string& gameDomain) {
    return client_->getAsync("v1/games/" + gameDomain)
        ->as<GameInfo>();
}

//=============================================================================
// Rate Limit Access
//=============================================================================

RateLimitInfo NexusModsApi::getRateLimitInfo() const {
    if (!config_.rateLimiter) {
        return {};
    }

    auto status = config_.rateLimiter->status();
    return RateLimitInfo{
        .dailyLimit = status.dailyLimit,
        .dailyRemaining = status.dailyRemaining,
        .dailyReset = status.dailyReset,
        .hourlyLimit = status.hourlyLimit,
        .hourlyRemaining = status.hourlyRemaining,
        .hourlyReset = status.hourlyReset
    };
}

fluent::IFluentClient& NexusModsApi::client() {
    return *client_;
}

} // namespace modular::api::nexus
```

---

## Day 3-4: Testing and Error Handling

### Task 5.3.1: Create Unit Tests
**File**: `tests/api/NexusModsApiTest.cpp`
**Estimated Time**: 4 hours

```cpp
#include <gtest/gtest.h>
#include "api/NexusModsApi.h"

using namespace modular::api::nexus;

// Mock response helper
class NexusModsApiTest : public ::testing::Test {
protected:
    // These tests require mocking - use a mock HTTP layer
};

TEST(NexusModsTypesTest, TrackedModDeserialization) {
    nlohmann::json json = R"({
        "mod_id": 12345,
        "domain_name": "skyrimspecialedition",
        "mod_name": "Test Mod"
    })"_json;

    auto mod = json.get<TrackedMod>();

    EXPECT_EQ(mod.modId, 12345);
    EXPECT_EQ(mod.domainName, "skyrimspecialedition");
    EXPECT_EQ(mod.modName, "Test Mod");
}

TEST(NexusModsTypesTest, UserProfileDeserialization) {
    nlohmann::json json = R"({
        "user_id": 123,
        "key": "test-key",
        "name": "TestUser",
        "email": "test@example.com",
        "profile_url": "https://nexusmods.com/users/123",
        "is_premium": true,
        "is_supporter": false
    })"_json;

    auto user = json.get<UserProfile>();

    EXPECT_EQ(user.userId, 123);
    EXPECT_EQ(user.username, "TestUser");
    EXPECT_TRUE(user.isPremium);
}

TEST(NexusModsTypesTest, ModFileDeserialization) {
    nlohmann::json json = R"({
        "file_id": 67890,
        "name": "Main File",
        "version": "1.0",
        "category_id": 1,
        "category_name": "MAIN",
        "is_primary": true,
        "size": 1048576,
        "file_name": "mod-1.0.zip",
        "uploaded_timestamp": 1609459200
    })"_json;

    auto file = json.get<ModFile>();

    EXPECT_EQ(file.fileId, 67890);
    EXPECT_EQ(file.name, "Main File");
    EXPECT_EQ(file.size, 1048576);
    EXPECT_TRUE(file.isPrimary);
}

TEST(NexusModsApiTest, BuildsCorrectUserAgent) {
    NexusModsConfig config;
    config.apiKey = "test-key";
    config.appName = "TestApp";
    config.appVersion = "2.0";
    config.contactEmail = "test@example.com";

    NexusModsApi api(config);

    // Would need to verify User-Agent header is set correctly
    // This requires mocking or inspecting the client
}
```

---

### Task 5.3.2: Create Integration Tests
**File**: `tests/api/NexusModsIntegrationTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include "api/NexusModsApi.h"

using namespace modular::api::nexus;

// These tests are disabled by default - require valid API key
class NexusModsIntegrationTest : public ::testing::Test {
protected:
    void SetUp() override {
        const char* apiKey = std::getenv("NEXUSMODS_API_KEY");
        if (!apiKey) {
            GTEST_SKIP() << "NEXUSMODS_API_KEY not set";
        }

        NexusModsConfig config;
        config.apiKey = apiKey;
        api_ = std::make_unique<NexusModsApi>(config);
    }

    std::unique_ptr<NexusModsApi> api_;
};

TEST_F(NexusModsIntegrationTest, DISABLED_ValidateUser) {
    auto user = api_->validateUser();

    EXPECT_FALSE(user.username.empty());
    EXPECT_GT(user.userId, 0);
}

TEST_F(NexusModsIntegrationTest, DISABLED_GetTrackedMods) {
    auto mods = api_->getTrackedMods();

    // Just verify it doesn't throw
    // Content depends on user's tracked mods
}

TEST_F(NexusModsIntegrationTest, DISABLED_GetGames) {
    auto games = api_->getGames();

    EXPECT_FALSE(games.empty());

    // Find Skyrim Special Edition
    auto it = std::find_if(games.begin(), games.end(),
        [](const GameInfo& g) { return g.domainName == "skyrimspecialedition"; });

    EXPECT_NE(it, games.end());
}

TEST_F(NexusModsIntegrationTest, DISABLED_RateLimitInfo) {
    // Make a request to populate rate limit
    api_->validateUser();

    auto info = api_->getRateLimitInfo();

    EXPECT_GT(info.dailyLimit, 0);
    EXPECT_GE(info.dailyRemaining, 0);
}
```

---

## Day 5: Migration Verification and Documentation

### Task 5.5.1: Create Migration Guide
**File**: `docs/api/NEXUSMODS_MIGRATION.md`
**Estimated Time**: 2 hours

```markdown
# NexusMods API Migration Guide

## Overview

This guide shows how to migrate from Modular's original NexusMods API
implementation to the new fluent-based implementation.

## Before and After

### Getting User Info

**Before:**
```cpp
std::string api_key = "your-key";
auto json = http_get("https://api.nexusmods.com/v1/users/validate",
    {"apikey: " + api_key});
auto user = parse_user_response(json);
```

**After:**
```cpp
NexusModsApi api("your-key");
auto user = api.validateUser();
// user is already typed - no manual parsing
```

### Getting Tracked Mods

**Before:**
```cpp
auto json = http_get("https://api.nexusmods.com/v1/user/tracked_mods",
    {"apikey: " + api_key});
std::vector<TrackedMod> mods;
for (const auto& item : json) {
    mods.push_back({
        item["mod_id"].get<int>(),
        item["domain_name"].get<std::string>(),
        item["mod_name"].get<std::string>()
    });
}
```

**After:**
```cpp
auto mods = api.getTrackedMods();
// Done - automatic deserialization

// Or filter by game:
auto skyrimMods = api.getTrackedMods("skyrimspecialedition");
```

### Downloading Files

**Before:**
```cpp
auto links = generate_download_links(domain, mod_id, file_id, api_key);
download_files({links[0]}, output_dir, false, [](size_t dl, size_t total) {
    printf("\r%zu/%zu", dl, total);
});
```

**After:**
```cpp
api.downloadFile(domain, modId, fileId, outputPath,
    [](size_t dl, size_t total) {
        std::cout << "\r" << (dl * 100 / total) << "%" << std::flush;
    });
```

## Key Improvements

1. **Type Safety**: All responses are deserialized to typed structures
2. **Error Handling**: Exceptions contain full response details
3. **Rate Limiting**: Automatic waiting when limits are hit
4. **Progress**: Built-in progress callback support
5. **Async Support**: All operations have async versions
6. **Fluent API**: Chainable, discoverable methods
```

---

## Deliverables Checklist

### Source Files
- [ ] `src/api/NexusModsTypes.h` - Data types
- [ ] `src/api/NexusModsApi.h` - API interface
- [ ] `src/api/NexusModsApi.cpp` - Implementation
- [ ] `src/api/CMakeLists.txt` - Build config

### Test Files
- [ ] `tests/api/NexusModsApiTest.cpp` - Unit tests
- [ ] `tests/api/NexusModsIntegrationTest.cpp` - Integration tests

### Documentation
- [ ] `docs/api/NEXUSMODS_MIGRATION.md` - Migration guide

---

## Definition of Done

Week 5 is complete when:

1. ✅ All NexusMods endpoints are wrapped
2. ✅ Type-safe request/response handling works
3. ✅ Downloads with progress work
4. ✅ Rate limiting is integrated
5. ✅ Unit tests pass
6. ✅ Migration guide is complete
