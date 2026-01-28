# Week 6 Task List: GameBanana API Migration

## Overview

**Objective**: Migrate Modular's GameBanana API client to the new fluent API, following the patterns established with NexusMods in Week 5.

**Prerequisites**: Week 5 completed (NexusMods migration)

**Duration**: 5 working days

**Output**: GameBanana API client with fluent interface, matching NexusMods patterns

---

## GameBanana API Overview

GameBanana uses a different API structure than NexusMods:
- No API key authentication (public API)
- HTML scraping for some data
- Different rate limiting approach
- Download links from Core API

```
GameBanana API Endpoints
════════════════════════

Core API (api.gamebanana.com):
- /Core/Item/Data - Get item metadata
- /Core/List/New - List new items
- /Core/List/Like - Search items

Core V2 API:
- /Mod/{id}/DownloadPage - Get download info
- /Mod/{id}/Files - List files
```

---

## Directory Structure

```
src/api/
├── GameBananaApi.h          # API interface
├── GameBananaApi.cpp        # Implementation
├── GameBananaTypes.h        # Data types
├── GameBananaParser.h       # HTML parsing utilities
├── GameBananaParser.cpp
└── CMakeLists.txt (update)

tests/api/
├── GameBananaApiTest.cpp
└── GameBananaIntegrationTest.cpp
```

---

## Day 1: Data Types and Parser

### Task 6.1.1: Create GameBanana Data Types
**File**: `src/api/GameBananaTypes.h`
**Estimated Time**: 2 hours

```cpp
#pragma once

#include <nlohmann/json.hpp>
#include <string>
#include <vector>
#include <optional>
#include <chrono>

namespace modular::api::gamebanana {

//=============================================================================
// Game Types
//=============================================================================

struct Game {
    int id;
    std::string name;
    std::string profileUrl;
    int modCount;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(Game, id, name, profileUrl, modCount)
};

//=============================================================================
// Mod Types
//=============================================================================

struct ModAuthor {
    int id;
    std::string name;
    std::string profileUrl;
};

struct ModFile {
    int fileId;
    std::string fileName;
    std::string description;
    int64_t fileSize;
    std::string downloadUrl;
    int downloadCount;
    std::chrono::system_clock::time_point dateAdded;
    std::optional<std::string> md5Checksum;
};

struct Mod {
    int id;
    std::string name;
    std::string description;
    std::string profileUrl;
    std::string thumbnailUrl;
    ModAuthor owner;
    std::vector<ModFile> files;

    int likeCount;
    int viewCount;
    int downloadCount;

    std::chrono::system_clock::time_point dateAdded;
    std::chrono::system_clock::time_point dateModified;

    std::vector<std::string> tags;
    std::optional<std::string> version;
};

//=============================================================================
// Search/List Types
//=============================================================================

struct ModListItem {
    int id;
    std::string name;
    std::string thumbnailUrl;
    std::string ownerName;
    int likeCount;
    int downloadCount;
    std::chrono::system_clock::time_point dateAdded;
};

struct ModListResponse {
    std::vector<ModListItem> items;
    int totalCount;
    int page;
    int pageSize;
    bool hasMore;
};

//=============================================================================
// Download Types
//=============================================================================

struct DownloadInfo {
    std::string url;
    std::string fileName;
    int64_t fileSize;
    std::optional<std::string> md5;
};

//=============================================================================
// API Response Wrapper
//=============================================================================

template<typename T>
struct ApiResponse {
    bool success;
    std::optional<std::string> error;
    std::optional<T> data;
};

} // namespace modular::api::gamebanana
```

---

### Task 6.1.2: Create HTML Parser for GameBanana
**File**: `src/api/GameBananaParser.h` and `.cpp`
**Estimated Time**: 3 hours

GameBanana requires HTML parsing for some data:

```cpp
// GameBananaParser.h
#pragma once

#include "GameBananaTypes.h"
#include <string>
#include <string_view>

namespace modular::api::gamebanana {

/// Parser for GameBanana HTML responses
class GameBananaParser {
public:
    /// Parse mod page HTML to extract download links
    static std::vector<DownloadInfo> parseDownloadPage(std::string_view html);

    /// Parse search results HTML
    static std::vector<ModListItem> parseSearchResults(std::string_view html);

    /// Extract value from HTML meta tag
    static std::string extractMetaContent(std::string_view html, std::string_view name);

    /// Extract JSON-LD structured data
    static nlohmann::json extractJsonLd(std::string_view html);

private:
    /// Simple HTML tag content extraction
    static std::string extractTagContent(
        std::string_view html,
        std::string_view tagName,
        std::string_view className = ""
    );

    /// Extract attribute value
    static std::string extractAttribute(
        std::string_view tag,
        std::string_view attrName
    );
};

} // namespace modular::api::gamebanana
```

```cpp
// GameBananaParser.cpp
#include "GameBananaParser.h"
#include <regex>
#include <sstream>

namespace modular::api::gamebanana {

std::vector<DownloadInfo> GameBananaParser::parseDownloadPage(std::string_view html) {
    std::vector<DownloadInfo> downloads;

    // Find download links in the HTML
    // GameBanana uses specific patterns for download URLs
    std::regex downloadRegex(
        R"(href="(https://files\.gamebanana\.com/[^"]+)"[^>]*>([^<]+)</a>)",
        std::regex::icase
    );

    std::string htmlStr(html);
    std::smatch match;
    std::string::const_iterator searchStart(htmlStr.cbegin());

    while (std::regex_search(searchStart, htmlStr.cend(), match, downloadRegex)) {
        DownloadInfo info;
        info.url = match[1].str();
        info.fileName = match[2].str();
        // Size and MD5 would need additional parsing
        downloads.push_back(std::move(info));

        searchStart = match.suffix().first;
    }

    return downloads;
}

std::vector<ModListItem> GameBananaParser::parseSearchResults(std::string_view html) {
    std::vector<ModListItem> items;

    // Parse search result items from HTML
    // This is a simplified implementation - real parsing would be more robust

    return items;
}

std::string GameBananaParser::extractMetaContent(
    std::string_view html,
    std::string_view name
) {
    std::string pattern = R"(<meta\s+(?:name|property)=["'])" +
                         std::string(name) +
                         R"(["']\s+content=["']([^"']+)["'])";

    std::regex metaRegex(pattern, std::regex::icase);
    std::smatch match;
    std::string htmlStr(html);

    if (std::regex_search(htmlStr, match, metaRegex)) {
        return match[1].str();
    }

    return "";
}

nlohmann::json GameBananaParser::extractJsonLd(std::string_view html) {
    std::regex jsonLdRegex(
        R"(<script\s+type=["']application/ld\+json["']>([^<]+)</script>)",
        std::regex::icase
    );

    std::smatch match;
    std::string htmlStr(html);

    if (std::regex_search(htmlStr, match, jsonLdRegex)) {
        try {
            return nlohmann::json::parse(match[1].str());
        } catch (...) {
            return nullptr;
        }
    }

    return nullptr;
}

} // namespace modular::api::gamebanana
```

---

## Day 2-3: API Implementation

### Task 6.2.1: Create GameBanana API Interface
**File**: `src/api/GameBananaApi.h`
**Estimated Time**: 2 hours

```cpp
#pragma once

#include "GameBananaTypes.h"
#include "GameBananaParser.h"
#include <fluent/Fluent.h>

#include <filesystem>
#include <future>

namespace modular::api::gamebanana {

/// Configuration for GameBanana API client
struct GameBananaConfig {
    /// Application name for User-Agent
    std::string appName = "Modular";

    /// Application version
    std::string appVersion = "1.0";

    /// Logger instance
    std::shared_ptr<ILogger> logger;

    /// Request timeout
    std::chrono::seconds timeout{60};

    /// Delay between requests (to be respectful to the API)
    std::chrono::milliseconds requestDelay{100};
};

/// Progress callback for downloads
using DownloadProgress = fluent::ProgressCallback;

/// Fluent wrapper for the GameBanana API
///
/// Unlike NexusMods, GameBanana's API is public and doesn't require authentication.
/// However, it mixes JSON API responses with HTML pages that need parsing.
class GameBananaApi {
public:
    /// Create API client with configuration
    explicit GameBananaApi(GameBananaConfig config = {});

    ~GameBananaApi() = default;

    //=========================================================================
    // Games
    //=========================================================================

    /// Get list of supported games
    std::vector<Game> getGames();

    /// Search for a game by name
    std::optional<Game> findGame(const std::string& name);

    //=========================================================================
    // Mods - Core API
    //=========================================================================

    /// Get mod information by ID
    /// Uses Core API /Mod/{id}
    Mod getModInfo(int modId);

    /// Get mod information async
    std::future<Mod> getModInfoAsync(int modId);

    /// List new mods for a game
    ModListResponse listNewMods(
        int gameId,
        int page = 1,
        int pageSize = 20
    );

    /// Search mods
    ModListResponse searchMods(
        int gameId,
        const std::string& query,
        int page = 1,
        int pageSize = 20
    );

    //=========================================================================
    // Files and Downloads
    //=========================================================================

    /// Get files for a mod
    std::vector<ModFile> getModFiles(int modId);

    /// Get download info for a specific file
    DownloadInfo getDownloadInfo(int modId, int fileId);

    /// Download a mod file
    void downloadFile(
        int modId,
        int fileId,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    );

    /// Download from direct URL
    void downloadFromUrl(
        const std::string& url,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    );

    /// Async download
    std::future<void> downloadFileAsync(
        int modId,
        int fileId,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    );

    //=========================================================================
    // Advanced
    //=========================================================================

    /// Get underlying fluent client
    fluent::IFluentClient& client();

private:
    GameBananaConfig config_;
    fluent::ClientPtr apiClient_;      // For api.gamebanana.com
    fluent::ClientPtr webClient_;      // For gamebanana.com (HTML)
    fluent::ClientPtr downloadClient_; // For files.gamebanana.com

    static constexpr const char* API_BASE = "https://api.gamebanana.com";
    static constexpr const char* WEB_BASE = "https://gamebanana.com";
    static constexpr const char* FILES_BASE = "https://files.gamebanana.com";

    void setupClients();
    std::string buildUserAgent() const;

    // Rate limiting helper
    void respectRateLimit();
    std::chrono::steady_clock::time_point lastRequest_;
    std::mutex requestMutex_;
};

} // namespace modular::api::gamebanana
```

---

### Task 6.2.2: Implement GameBanana API
**File**: `src/api/GameBananaApi.cpp`
**Estimated Time**: 4 hours

```cpp
#include "GameBananaApi.h"
#include <fluent/filters/FilterFactory.h>
#include <thread>

namespace modular::api::gamebanana {

GameBananaApi::GameBananaApi(GameBananaConfig config)
    : config_(std::move(config))
{
    setupClients();
}

void GameBananaApi::setupClients() {
    using namespace fluent;
    using namespace fluent::filters;

    std::string userAgent = buildUserAgent();

    // API client for api.gamebanana.com
    apiClient_ = createFluentClient(API_BASE);
    apiClient_->setUserAgent(userAgent);
    apiClient_->setRequestTimeout(config_.timeout);
    apiClient_->addFilter(FilterFactory::createErrorFilter());
    if (config_.logger) {
        apiClient_->addFilter(FilterFactory::createLoggingFilter(config_.logger.get()));
    }

    // Web client for gamebanana.com (HTML pages)
    webClient_ = createFluentClient(WEB_BASE);
    webClient_->setUserAgent(userAgent);
    webClient_->setRequestTimeout(config_.timeout);
    webClient_->addFilter(FilterFactory::createErrorFilter());

    // Download client for files.gamebanana.com
    downloadClient_ = createFluentClient(FILES_BASE);
    downloadClient_->setUserAgent(userAgent);
    downloadClient_->setRequestTimeout(std::chrono::seconds{600});
}

std::string GameBananaApi::buildUserAgent() const {
    return config_.appName + "/" + config_.appVersion;
}

void GameBananaApi::respectRateLimit() {
    std::lock_guard<std::mutex> lock(requestMutex_);

    auto now = std::chrono::steady_clock::now();
    auto elapsed = now - lastRequest_;

    if (elapsed < config_.requestDelay) {
        std::this_thread::sleep_for(config_.requestDelay - elapsed);
    }

    lastRequest_ = std::chrono::steady_clock::now();
}

//=============================================================================
// Games
//=============================================================================

std::vector<Game> GameBananaApi::getGames() {
    respectRateLimit();

    // GameBanana provides a games list endpoint
    return apiClient_->getAsync("Core/Game/Index")
        ->as<std::vector<Game>>();
}

std::optional<Game> GameBananaApi::findGame(const std::string& name) {
    auto games = getGames();

    auto it = std::find_if(games.begin(), games.end(),
        [&name](const Game& g) {
            // Case-insensitive partial match
            std::string lowerName = name;
            std::string lowerGameName = g.name;
            std::transform(lowerName.begin(), lowerName.end(), lowerName.begin(), ::tolower);
            std::transform(lowerGameName.begin(), lowerGameName.end(), lowerGameName.begin(), ::tolower);
            return lowerGameName.find(lowerName) != std::string::npos;
        });

    return it != games.end() ? std::optional{*it} : std::nullopt;
}

//=============================================================================
// Mods
//=============================================================================

Mod GameBananaApi::getModInfo(int modId) {
    respectRateLimit();

    // Core API endpoint for mod data
    auto json = apiClient_->getAsync("Core/Item/Data")
        ->withArgument("itemtype", "Mod")
        .withArgument("itemid", std::to_string(modId))
        .withArgument("fields", "name,description,Owner().name,Files().aFiles()")
        .asJson();

    Mod mod;
    mod.id = modId;
    mod.name = json.value("name", "");
    mod.description = json.value("description", "");

    // Parse owner
    if (json.contains("Owner()") && json["Owner()"].contains("name")) {
        mod.owner.name = json["Owner()"]["name"];
    }

    // Parse files
    if (json.contains("Files()") && json["Files()"].contains("aFiles()")) {
        for (const auto& [fileId, fileData] : json["Files()"]["aFiles()"].items()) {
            ModFile file;
            file.fileId = std::stoi(fileId);
            file.fileName = fileData.value("_sFile", "");
            file.fileSize = fileData.value("_nFilesize", 0);
            file.downloadUrl = fileData.value("_sDownloadUrl", "");
            mod.files.push_back(std::move(file));
        }
    }

    return mod;
}

std::future<Mod> GameBananaApi::getModInfoAsync(int modId) {
    return std::async(std::launch::async, [this, modId]() {
        return getModInfo(modId);
    });
}

ModListResponse GameBananaApi::listNewMods(int gameId, int page, int pageSize) {
    respectRateLimit();

    auto json = apiClient_->getAsync("Core/List/New")
        ->withArgument("gameid", std::to_string(gameId))
        .withArgument("page", std::to_string(page))
        .withArgument("itemsperpage", std::to_string(pageSize))
        .asJson();

    ModListResponse response;
    response.page = page;
    response.pageSize = pageSize;

    if (json.is_array()) {
        for (const auto& item : json) {
            ModListItem mod;
            mod.id = item.value("_idRow", 0);
            mod.name = item.value("_sName", "");
            mod.thumbnailUrl = item.value("_sPreviewUrl", "");
            mod.likeCount = item.value("_nLikeCount", 0);
            mod.downloadCount = item.value("_nDownloadCount", 0);
            response.items.push_back(std::move(mod));
        }
    }

    response.totalCount = response.items.size();  // API doesn't provide total
    response.hasMore = response.items.size() == pageSize;

    return response;
}

ModListResponse GameBananaApi::searchMods(
    int gameId,
    const std::string& query,
    int page,
    int pageSize
) {
    respectRateLimit();

    auto json = apiClient_->getAsync("Core/List/Like")
        ->withArgument("gameid", std::to_string(gameId))
        .withArgument("name", query)
        .withArgument("page", std::to_string(page))
        .withArgument("itemsperpage", std::to_string(pageSize))
        .asJson();

    ModListResponse response;
    response.page = page;
    response.pageSize = pageSize;

    if (json.is_array()) {
        for (const auto& item : json) {
            ModListItem mod;
            mod.id = item.value("_idRow", 0);
            mod.name = item.value("_sName", "");
            mod.thumbnailUrl = item.value("_sPreviewUrl", "");
            response.items.push_back(std::move(mod));
        }
    }

    response.totalCount = response.items.size();
    response.hasMore = response.items.size() == pageSize;

    return response;
}

//=============================================================================
// Files and Downloads
//=============================================================================

std::vector<ModFile> GameBananaApi::getModFiles(int modId) {
    auto mod = getModInfo(modId);
    return mod.files;
}

DownloadInfo GameBananaApi::getDownloadInfo(int modId, int fileId) {
    auto files = getModFiles(modId);

    auto it = std::find_if(files.begin(), files.end(),
        [fileId](const ModFile& f) { return f.fileId == fileId; });

    if (it == files.end()) {
        throw fluent::ApiException(
            "File not found",
            404, "Not Found", {}, ""
        );
    }

    return DownloadInfo{
        .url = it->downloadUrl,
        .fileName = it->fileName,
        .fileSize = it->fileSize,
        .md5 = it->md5Checksum
    };
}

void GameBananaApi::downloadFile(
    int modId,
    int fileId,
    const std::filesystem::path& outputPath,
    DownloadProgress progress
) {
    auto info = getDownloadInfo(modId, fileId);
    downloadFromUrl(info.url, outputPath, progress);
}

void GameBananaApi::downloadFromUrl(
    const std::string& url,
    const std::filesystem::path& outputPath,
    DownloadProgress progress
) {
    downloadClient_->getAsync(url)
        ->downloadTo(outputPath, progress);
}

std::future<void> GameBananaApi::downloadFileAsync(
    int modId,
    int fileId,
    const std::filesystem::path& outputPath,
    DownloadProgress progress
) {
    return std::async(std::launch::async, [=, this]() {
        downloadFile(modId, fileId, outputPath, progress);
    });
}

fluent::IFluentClient& GameBananaApi::client() {
    return *apiClient_;
}

} // namespace modular::api::gamebanana
```

---

## Day 4-5: Testing, Documentation, and Unified API

### Task 6.4.1: Create Unit Tests
**File**: `tests/api/GameBananaApiTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include "api/GameBananaApi.h"

using namespace modular::api::gamebanana;

TEST(GameBananaParserTest, ExtractDownloadLinks) {
    std::string html = R"(
        <a href="https://files.gamebanana.com/mods/test.zip">Download</a>
    )";

    auto downloads = GameBananaParser::parseDownloadPage(html);

    EXPECT_EQ(downloads.size(), 1);
    EXPECT_EQ(downloads[0].url, "https://files.gamebanana.com/mods/test.zip");
}

TEST(GameBananaApiTest, BuildsCorrectUserAgent) {
    GameBananaConfig config;
    config.appName = "TestApp";
    config.appVersion = "1.0";

    GameBananaApi api(config);
    // Verify user agent is set correctly
}
```

---

### Task 6.4.2: Create Unified Mod Repository Interface
**File**: `src/api/ModRepository.h`
**Estimated Time**: 2 hours

Create a common interface for both NexusMods and GameBanana:

```cpp
#pragma once

#include <string>
#include <vector>
#include <filesystem>
#include <future>
#include <functional>

namespace modular::api {

/// Progress callback for downloads
using DownloadProgress = std::function<void(size_t downloaded, size_t total)>;

/// Generic mod information
struct GenericMod {
    std::string source;      // "nexusmods" or "gamebanana"
    std::string id;          // Source-specific ID
    std::string name;
    std::string author;
    std::string description;
    std::string version;
    std::string url;
    int64_t downloadCount;
};

/// Generic file information
struct GenericFile {
    std::string id;
    std::string name;
    std::string version;
    int64_t size;
    std::string downloadUrl;
};

/// Abstract interface for mod repositories
class IModRepository {
public:
    virtual ~IModRepository() = default;

    /// Get repository name
    virtual std::string name() const = 0;

    /// Search for mods
    virtual std::vector<GenericMod> searchMods(
        const std::string& query,
        const std::string& gameDomain
    ) = 0;

    /// Get mod information
    virtual GenericMod getModInfo(const std::string& modId) = 0;

    /// Get mod files
    virtual std::vector<GenericFile> getModFiles(const std::string& modId) = 0;

    /// Download a file
    virtual void downloadFile(
        const std::string& modId,
        const std::string& fileId,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    ) = 0;

    /// Async download
    virtual std::future<void> downloadFileAsync(
        const std::string& modId,
        const std::string& fileId,
        const std::filesystem::path& outputPath,
        DownloadProgress progress = nullptr
    ) = 0;
};

} // namespace modular::api
```

---

## Deliverables Checklist

### Source Files
- [ ] `src/api/GameBananaTypes.h`
- [ ] `src/api/GameBananaParser.h/.cpp`
- [ ] `src/api/GameBananaApi.h/.cpp`
- [ ] `src/api/ModRepository.h` (unified interface)

### Test Files
- [ ] `tests/api/GameBananaApiTest.cpp`
- [ ] `tests/api/GameBananaIntegrationTest.cpp`

### Documentation
- [ ] `docs/api/GAMEBANANA_MIGRATION.md`

---

## Definition of Done

Week 6 is complete when:

1. ✅ GameBanana API wrapper is functional
2. ✅ HTML parsing for downloads works
3. ✅ Downloads with progress work
4. ✅ Unified IModRepository interface exists
5. ✅ Unit tests pass
6. ✅ Integration tests pass (disabled by default)
