# Week 2 Task List: Fluent Wrapper Foundation

## Overview

**Objective**: Implement the core fluent wrapper classes around Modular's existing `HttpClient`, establishing the foundation for the fluent API.

**Prerequisites**: Week 1 completed (all interfaces defined and compiling)

**Duration**: 5 working days

**Output**: Working implementations of `Response`, `BodyBuilder`, and the foundation of `Request` and `FluentClient` classes

---

## Architecture Context

```
Week 2 Implementation Scope
═══════════════════════════

┌─────────────────────────────────────────────────────────────────┐
│                      FluentClient (partial)                      │
│  - Constructor with base URL                                     │
│  - HTTP method factories (getAsync, postAsync, etc.)            │
│  - Basic configuration (setUserAgent, setAuthentication)        │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │ creates
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Request (partial)                           │
│  - URL and header building                                       │
│  - Body attachment                                               │
│  - Basic execution (no filters/retry yet)                       │
└─────────────────────────────────────────────────────────────────┘
                               │
                               │ returns
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Response (complete)                         │  ✓ Full implementation
│  - Status/header access                                          │
│  - All parsing methods (asString, asJson, as<T>)                │
│  - File download with progress                                   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      BodyBuilder (complete)                      │  ✓ Full implementation
│  - JSON serialization                                            │
│  - Form URL encoding                                             │
│  - Multipart file upload                                         │
└─────────────────────────────────────────────────────────────────┘

Integration with existing Modular code:
┌─────────────────┐
│ HttpClient.h/cpp│  ← Existing CURL-based implementation
│ (unchanged)     │  ← FluentClient wraps this internally
└─────────────────┘
```

---

## Directory Structure

Create the following source files:

```
src/
└── fluent/
    ├── CMakeLists.txt           # Build configuration
    ├── Response.h               # Response implementation header
    ├── Response.cpp             # Response implementation
    ├── BodyBuilder.h            # BodyBuilder implementation header
    ├── BodyBuilder.cpp          # BodyBuilder implementation
    ├── Request.h                # Request implementation header (partial)
    ├── Request.cpp              # Request implementation (partial)
    ├── FluentClient.h           # FluentClient implementation header (partial)
    ├── FluentClient.cpp         # FluentClient implementation (partial)
    └── Utils.h                  # Internal utilities (URL encoding, base64, etc.)

tests/
└── fluent/
    ├── ResponseTest.cpp         # Response unit tests
    ├── BodyBuilderTest.cpp      # BodyBuilder unit tests
    └── IntegrationTest.cpp      # Basic integration tests
```

---

## Day 1: Utilities and Response Implementation (Part 1)

### Task 2.1.1: Create Internal Utilities
**File**: `src/fluent/Utils.h`
**Estimated Time**: 2 hours

**Instructions**:

Create utility functions needed by multiple implementation classes:

```cpp
#pragma once

#include <string>
#include <string_view>
#include <vector>
#include <map>
#include <algorithm>
#include <cctype>
#include <sstream>
#include <iomanip>
#include <random>

namespace modular::fluent::detail {

//=============================================================================
// URL Encoding
//=============================================================================

/// URL-encode a string (RFC 3986)
inline std::string urlEncode(std::string_view input) {
    std::ostringstream encoded;
    encoded.fill('0');
    encoded << std::hex;

    for (char c : input) {
        // Keep alphanumeric and other accepted characters intact
        if (std::isalnum(static_cast<unsigned char>(c)) ||
            c == '-' || c == '_' || c == '.' || c == '~') {
            encoded << c;
        } else {
            // Percent-encode all other characters
            encoded << std::uppercase;
            encoded << '%' << std::setw(2) << int(static_cast<unsigned char>(c));
            encoded << std::nouppercase;
        }
    }

    return encoded.str();
}

/// URL-decode a string
inline std::string urlDecode(std::string_view input) {
    std::string decoded;
    decoded.reserve(input.size());

    for (size_t i = 0; i < input.size(); ++i) {
        if (input[i] == '%' && i + 2 < input.size()) {
            int value;
            std::istringstream iss(std::string(input.substr(i + 1, 2)));
            if (iss >> std::hex >> value) {
                decoded += static_cast<char>(value);
                i += 2;
            } else {
                decoded += input[i];
            }
        } else if (input[i] == '+') {
            decoded += ' ';
        } else {
            decoded += input[i];
        }
    }

    return decoded;
}

/// Build a query string from key-value pairs
inline std::string buildQueryString(
    const std::vector<std::pair<std::string, std::string>>& params
) {
    if (params.empty()) return "";

    std::ostringstream oss;
    bool first = true;
    for (const auto& [key, value] : params) {
        if (!first) oss << '&';
        first = false;
        oss << urlEncode(key) << '=' << urlEncode(value);
    }
    return oss.str();
}

//=============================================================================
// Base64 Encoding (for Basic Auth)
//=============================================================================

inline constexpr char base64Chars[] =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

inline std::string base64Encode(std::string_view input) {
    std::string encoded;
    encoded.reserve(((input.size() + 2) / 3) * 4);

    int val = 0;
    int bits = -6;

    for (unsigned char c : input) {
        val = (val << 8) + c;
        bits += 8;
        while (bits >= 0) {
            encoded.push_back(base64Chars[(val >> bits) & 0x3F]);
            bits -= 6;
        }
    }

    if (bits > -6) {
        encoded.push_back(base64Chars[((val << 8) >> (bits + 8)) & 0x3F]);
    }

    while (encoded.size() % 4) {
        encoded.push_back('=');
    }

    return encoded;
}

//=============================================================================
// String Utilities
//=============================================================================

/// Case-insensitive string comparison
inline bool iequals(std::string_view a, std::string_view b) {
    if (a.size() != b.size()) return false;
    return std::equal(a.begin(), a.end(), b.begin(),
        [](char ca, char cb) {
            return std::tolower(static_cast<unsigned char>(ca)) ==
                   std::tolower(static_cast<unsigned char>(cb));
        });
}

/// Trim whitespace from both ends
inline std::string trim(std::string_view str) {
    auto start = str.find_first_not_of(" \t\r\n");
    if (start == std::string_view::npos) return "";
    auto end = str.find_last_not_of(" \t\r\n");
    return std::string(str.substr(start, end - start + 1));
}

/// Split string by delimiter
inline std::vector<std::string> split(std::string_view str, char delimiter) {
    std::vector<std::string> result;
    size_t start = 0;
    size_t end;

    while ((end = str.find(delimiter, start)) != std::string_view::npos) {
        result.emplace_back(str.substr(start, end - start));
        start = end + 1;
    }
    result.emplace_back(str.substr(start));

    return result;
}

/// Join strings with delimiter
template<typename Container>
std::string join(const Container& items, std::string_view delimiter) {
    std::ostringstream oss;
    bool first = true;
    for (const auto& item : items) {
        if (!first) oss << delimiter;
        first = false;
        oss << item;
    }
    return oss.str();
}

//=============================================================================
// Multipart Boundary Generation
//=============================================================================

inline std::string generateBoundary() {
    static constexpr char chars[] =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<> dis(0, sizeof(chars) - 2);

    std::string boundary = "----ModularBoundary";
    for (int i = 0; i < 16; ++i) {
        boundary += chars[dis(gen)];
    }
    return boundary;
}

//=============================================================================
// MIME Type Detection
//=============================================================================

inline std::string getMimeType(const std::filesystem::path& path) {
    static const std::map<std::string, std::string> mimeTypes = {
        {".json", "application/json"},
        {".xml", "application/xml"},
        {".zip", "application/zip"},
        {".7z", "application/x-7z-compressed"},
        {".rar", "application/vnd.rar"},
        {".txt", "text/plain"},
        {".html", "text/html"},
        {".css", "text/css"},
        {".js", "application/javascript"},
        {".png", "image/png"},
        {".jpg", "image/jpeg"},
        {".jpeg", "image/jpeg"},
        {".gif", "image/gif"},
        {".webp", "image/webp"},
        {".pdf", "application/pdf"},
    };

    auto ext = path.extension().string();
    std::transform(ext.begin(), ext.end(), ext.begin(),
        [](unsigned char c) { return std::tolower(c); });

    auto it = mimeTypes.find(ext);
    return it != mimeTypes.end() ? it->second : "application/octet-stream";
}

//=============================================================================
// Header Parsing
//=============================================================================

/// Parse a header value with parameters (e.g., "text/html; charset=utf-8")
struct HeaderValue {
    std::string value;
    std::map<std::string, std::string> params;
};

inline HeaderValue parseHeaderValue(std::string_view header) {
    HeaderValue result;
    auto parts = split(header, ';');

    if (!parts.empty()) {
        result.value = trim(parts[0]);

        for (size_t i = 1; i < parts.size(); ++i) {
            auto param = trim(parts[i]);
            auto eq = param.find('=');
            if (eq != std::string::npos) {
                auto key = trim(param.substr(0, eq));
                auto val = trim(param.substr(eq + 1));
                // Remove quotes if present
                if (val.size() >= 2 && val.front() == '"' && val.back() == '"') {
                    val = val.substr(1, val.size() - 2);
                }
                result.params[std::string(key)] = std::string(val);
            }
        }
    }

    return result;
}

} // namespace modular::fluent::detail
```

**Verification**:
- [ ] URL encoding handles all special characters correctly
- [ ] Base64 encoding matches standard implementation
- [ ] MIME type detection covers common file types
- [ ] All functions are `inline` or in anonymous namespace to avoid ODR issues

---

### Task 2.1.2: Create Response Implementation Header
**File**: `src/fluent/Response.h`
**Estimated Time**: 1.5 hours

**Instructions**:

```cpp
#pragma once

#include <fluent/IResponse.h>
#include <fluent/Exceptions.h>
#include "Utils.h"

#include <fstream>
#include <mutex>

namespace modular::fluent {

/// Concrete implementation of IResponse
/// Wraps HTTP response data and provides parsing methods
class Response : public IResponse {
public:
    /// Construct from raw HTTP response data
    /// @param statusCode HTTP status code
    /// @param statusReason HTTP status reason phrase
    /// @param headers Response headers
    /// @param body Response body content
    /// @param effectiveUrl Final URL after redirects
    /// @param elapsed Time taken for request
    Response(
        int statusCode,
        std::string statusReason,
        Headers headers,
        std::vector<uint8_t> body,
        std::string effectiveUrl,
        std::chrono::milliseconds elapsed
    );

    /// Construct from raw HTTP response with string body
    Response(
        int statusCode,
        std::string statusReason,
        Headers headers,
        std::string body,
        std::string effectiveUrl,
        std::chrono::milliseconds elapsed
    );

    // Non-copyable but movable
    Response(const Response&) = delete;
    Response& operator=(const Response&) = delete;
    Response(Response&&) = default;
    Response& operator=(Response&&) = default;

    ~Response() override = default;

    //=========================================================================
    // IResponse Implementation - Status
    //=========================================================================

    bool isSuccessStatusCode() const override;
    int statusCode() const override;
    std::string statusReason() const override;

    //=========================================================================
    // IResponse Implementation - Headers
    //=========================================================================

    const Headers& headers() const override;
    std::string header(std::string_view name) const override;
    bool hasHeader(std::string_view name) const override;
    std::string contentType() const override;
    int64_t contentLength() const override;

    //=========================================================================
    // IResponse Implementation - Body Access (Sync)
    //=========================================================================

    std::string asString() override;
    std::vector<uint8_t> asByteArray() override;
    nlohmann::json asJson() override;

    //=========================================================================
    // IResponse Implementation - Body Access (Async)
    //=========================================================================

    std::future<std::string> asStringAsync() override;
    std::future<std::vector<uint8_t>> asByteArrayAsync() override;
    std::future<nlohmann::json> asJsonAsync() override;

    //=========================================================================
    // IResponse Implementation - File Operations
    //=========================================================================

    void saveToFile(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) override;

    std::future<void> saveToFileAsync(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) override;

    //=========================================================================
    // IResponse Implementation - Metadata
    //=========================================================================

    std::string effectiveUrl() const override;
    std::chrono::milliseconds elapsed() const override;
    bool wasRedirected() const override;

    //=========================================================================
    // Factory Methods (for internal use)
    //=========================================================================

    /// Create a Response from existing HttpClient response structure
    /// This bridges Modular's existing HttpClient with the fluent API
    static std::unique_ptr<Response> fromHttpClientResponse(
        int statusCode,
        const std::string& body,
        const std::map<std::string, std::string>& headers,
        const std::string& url,
        std::chrono::milliseconds elapsed
    );

private:
    int statusCode_;
    std::string statusReason_;
    Headers headers_;
    std::vector<uint8_t> body_;
    std::string effectiveUrl_;
    std::chrono::milliseconds elapsed_;
    std::string originalUrl_;  // For redirect detection

    // Cached parsed values
    mutable std::optional<std::string> cachedString_;
    mutable std::optional<nlohmann::json> cachedJson_;
    mutable std::mutex cacheMutex_;

    // Helper to find header case-insensitively
    std::string findHeader(std::string_view name) const;
};

} // namespace modular::fluent
```

**Verification**:
- [ ] All IResponse methods are declared
- [ ] Factory method bridges to existing Modular HttpClient
- [ ] Caching is thread-safe with mutex
- [ ] Move semantics are enabled

---

### Task 2.1.3: Implement Response Class
**File**: `src/fluent/Response.cpp`
**Estimated Time**: 3 hours

**Instructions**:

```cpp
#include "Response.h"
#include <algorithm>
#include <fstream>

namespace modular::fluent {

//=============================================================================
// Constructors
//=============================================================================

Response::Response(
    int statusCode,
    std::string statusReason,
    Headers headers,
    std::vector<uint8_t> body,
    std::string effectiveUrl,
    std::chrono::milliseconds elapsed
)
    : statusCode_(statusCode)
    , statusReason_(std::move(statusReason))
    , headers_(std::move(headers))
    , body_(std::move(body))
    , effectiveUrl_(std::move(effectiveUrl))
    , elapsed_(elapsed)
    , originalUrl_(effectiveUrl_)  // Will be set properly by Request
{}

Response::Response(
    int statusCode,
    std::string statusReason,
    Headers headers,
    std::string body,
    std::string effectiveUrl,
    std::chrono::milliseconds elapsed
)
    : statusCode_(statusCode)
    , statusReason_(std::move(statusReason))
    , headers_(std::move(headers))
    , body_(body.begin(), body.end())
    , effectiveUrl_(std::move(effectiveUrl))
    , elapsed_(elapsed)
    , originalUrl_(effectiveUrl_)
{}

//=============================================================================
// Status Methods
//=============================================================================

bool Response::isSuccessStatusCode() const {
    return statusCode_ >= 200 && statusCode_ < 300;
}

int Response::statusCode() const {
    return statusCode_;
}

std::string Response::statusReason() const {
    return statusReason_;
}

//=============================================================================
// Header Methods
//=============================================================================

const Headers& Response::headers() const {
    return headers_;
}

std::string Response::findHeader(std::string_view name) const {
    // Case-insensitive header lookup
    for (const auto& [key, value] : headers_) {
        if (detail::iequals(key, name)) {
            return value;
        }
    }
    return "";
}

std::string Response::header(std::string_view name) const {
    return findHeader(name);
}

bool Response::hasHeader(std::string_view name) const {
    for (const auto& [key, value] : headers_) {
        if (detail::iequals(key, name)) {
            return true;
        }
    }
    return false;
}

std::string Response::contentType() const {
    auto ct = findHeader("Content-Type");
    if (ct.empty()) return "";

    // Parse out just the media type, without parameters
    auto parsed = detail::parseHeaderValue(ct);
    return parsed.value;
}

int64_t Response::contentLength() const {
    auto cl = findHeader("Content-Length");
    if (cl.empty()) return -1;

    try {
        return std::stoll(cl);
    } catch (...) {
        return -1;
    }
}

//=============================================================================
// Body Access - Synchronous
//=============================================================================

std::string Response::asString() {
    std::lock_guard<std::mutex> lock(cacheMutex_);

    if (!cachedString_) {
        cachedString_ = std::string(body_.begin(), body_.end());
    }
    return *cachedString_;
}

std::vector<uint8_t> Response::asByteArray() {
    return body_;  // Return copy
}

nlohmann::json Response::asJson() {
    std::lock_guard<std::mutex> lock(cacheMutex_);

    if (!cachedJson_) {
        try {
            auto str = std::string(body_.begin(), body_.end());
            cachedJson_ = nlohmann::json::parse(str);
        } catch (const nlohmann::json::parse_error& e) {
            throw ParseException(
                std::string("Failed to parse JSON: ") + e.what(),
                std::string(body_.begin(), body_.end())
            );
        }
    }
    return *cachedJson_;
}

//=============================================================================
// Body Access - Asynchronous
//=============================================================================

std::future<std::string> Response::asStringAsync() {
    return std::async(std::launch::async, [this]() {
        return asString();
    });
}

std::future<std::vector<uint8_t>> Response::asByteArrayAsync() {
    return std::async(std::launch::async, [this]() {
        return asByteArray();
    });
}

std::future<nlohmann::json> Response::asJsonAsync() {
    return std::async(std::launch::async, [this]() {
        return asJson();
    });
}

//=============================================================================
// File Operations
//=============================================================================

void Response::saveToFile(
    const std::filesystem::path& path,
    ProgressCallback progress
) {
    // Ensure parent directory exists
    if (path.has_parent_path()) {
        std::filesystem::create_directories(path.parent_path());
    }

    std::ofstream file(path, std::ios::binary);
    if (!file) {
        throw std::filesystem::filesystem_error(
            "Failed to open file for writing",
            path,
            std::make_error_code(std::errc::io_error)
        );
    }

    const size_t totalSize = body_.size();
    const size_t chunkSize = 8192;  // 8KB chunks for progress reporting
    size_t written = 0;

    while (written < totalSize) {
        size_t toWrite = std::min(chunkSize, totalSize - written);
        file.write(reinterpret_cast<const char*>(body_.data() + written), toWrite);

        if (!file) {
            throw std::filesystem::filesystem_error(
                "Failed to write to file",
                path,
                std::make_error_code(std::errc::io_error)
            );
        }

        written += toWrite;

        if (progress) {
            progress(written, totalSize);
        }
    }

    file.close();
}

std::future<void> Response::saveToFileAsync(
    const std::filesystem::path& path,
    ProgressCallback progress
) {
    return std::async(std::launch::async, [this, path, progress]() {
        saveToFile(path, progress);
    });
}

//=============================================================================
// Metadata
//=============================================================================

std::string Response::effectiveUrl() const {
    return effectiveUrl_;
}

std::chrono::milliseconds Response::elapsed() const {
    return elapsed_;
}

bool Response::wasRedirected() const {
    return effectiveUrl_ != originalUrl_ && !originalUrl_.empty();
}

//=============================================================================
// Factory Methods
//=============================================================================

std::unique_ptr<Response> Response::fromHttpClientResponse(
    int statusCode,
    const std::string& body,
    const std::map<std::string, std::string>& headers,
    const std::string& url,
    std::chrono::milliseconds elapsed
) {
    // Determine status reason from code
    static const std::map<int, std::string> statusReasons = {
        {200, "OK"}, {201, "Created"}, {204, "No Content"},
        {301, "Moved Permanently"}, {302, "Found"}, {304, "Not Modified"},
        {400, "Bad Request"}, {401, "Unauthorized"}, {403, "Forbidden"},
        {404, "Not Found"}, {429, "Too Many Requests"},
        {500, "Internal Server Error"}, {502, "Bad Gateway"},
        {503, "Service Unavailable"}, {504, "Gateway Timeout"}
    };

    std::string reason = "Unknown";
    if (auto it = statusReasons.find(statusCode); it != statusReasons.end()) {
        reason = it->second;
    }

    return std::make_unique<Response>(
        statusCode,
        reason,
        headers,
        body,
        url,
        elapsed
    );
}

} // namespace modular::fluent
```

**Verification**:
- [ ] All IResponse methods are implemented
- [ ] JSON parsing errors throw `ParseException`
- [ ] File operations create parent directories
- [ ] Progress callback is called during file writes
- [ ] Header lookup is case-insensitive
- [ ] Thread-safe caching works correctly

---

## Day 2: BodyBuilder Implementation

### Task 2.2.1: Create BodyBuilder Implementation Header
**File**: `src/fluent/BodyBuilder.h`
**Estimated Time**: 1 hour

**Instructions**:

```cpp
#pragma once

#include <fluent/IBodyBuilder.h>
#include "Utils.h"

#include <fstream>

namespace modular::fluent {

/// Concrete implementation of IBodyBuilder
/// Constructs HTTP request bodies in various formats
class BodyBuilder : public IBodyBuilder {
public:
    BodyBuilder() = default;
    ~BodyBuilder() override = default;

    //=========================================================================
    // IBodyBuilder Implementation - Form URL Encoded
    //=========================================================================

    RequestBody formUrlEncoded(
        const std::vector<std::pair<std::string, std::string>>& arguments
    ) override;

    RequestBody formUrlEncoded(
        const std::map<std::string, std::string>& arguments
    ) override;

    //=========================================================================
    // IBodyBuilder Implementation - JSON
    //=========================================================================

    RequestBody jsonBody(const nlohmann::json& json) override;
    RequestBody rawJson(const std::string& jsonString) override;

    //=========================================================================
    // IBodyBuilder Implementation - File Upload
    //=========================================================================

    RequestBody fileUpload(const std::filesystem::path& filePath) override;

    RequestBody fileUpload(
        const std::vector<std::filesystem::path>& filePaths
    ) override;

    RequestBody fileUpload(
        const std::vector<std::pair<std::string, std::filesystem::path>>& files
    ) override;

    RequestBody fileUpload(
        const std::string& fieldName,
        const std::string& fileName,
        const std::vector<uint8_t>& data,
        const std::string& mimeType = "application/octet-stream"
    ) override;

    //=========================================================================
    // IBodyBuilder Implementation - Raw Content
    //=========================================================================

    RequestBody raw(
        const std::string& content,
        const std::string& contentType = "text/plain"
    ) override;

    RequestBody raw(
        const std::vector<uint8_t>& content,
        const std::string& contentType = "application/octet-stream"
    ) override;

private:
    /// Build multipart form data body
    RequestBody buildMultipartBody(
        const std::vector<std::tuple<std::string, std::string, std::vector<uint8_t>, std::string>>& parts
    );
};

} // namespace modular::fluent
```

---

### Task 2.2.2: Implement BodyBuilder Class
**File**: `src/fluent/BodyBuilder.cpp`
**Estimated Time**: 3 hours

**Instructions**:

```cpp
#include "BodyBuilder.h"
#include <sstream>
#include <fstream>

namespace modular::fluent {

//=============================================================================
// Form URL Encoded
//=============================================================================

RequestBody BodyBuilder::formUrlEncoded(
    const std::vector<std::pair<std::string, std::string>>& arguments
) {
    std::string body = detail::buildQueryString(arguments);
    return RequestBody(
        std::move(body),
        "application/x-www-form-urlencoded"
    );
}

RequestBody BodyBuilder::formUrlEncoded(
    const std::map<std::string, std::string>& arguments
) {
    std::vector<std::pair<std::string, std::string>> pairs;
    pairs.reserve(arguments.size());
    for (const auto& [key, value] : arguments) {
        pairs.emplace_back(key, value);
    }
    return formUrlEncoded(pairs);
}

//=============================================================================
// JSON
//=============================================================================

RequestBody BodyBuilder::jsonBody(const nlohmann::json& json) {
    return RequestBody(
        json.dump(),
        "application/json"
    );
}

RequestBody BodyBuilder::rawJson(const std::string& jsonString) {
    return RequestBody(
        jsonString,
        "application/json"
    );
}

//=============================================================================
// File Upload - Multipart Form Data
//=============================================================================

RequestBody BodyBuilder::buildMultipartBody(
    const std::vector<std::tuple<std::string, std::string, std::vector<uint8_t>, std::string>>& parts
) {
    std::string boundary = detail::generateBoundary();
    std::vector<uint8_t> body;

    for (const auto& [fieldName, fileName, data, mimeType] : parts) {
        // Boundary line
        std::string boundaryLine = "--" + boundary + "\r\n";
        body.insert(body.end(), boundaryLine.begin(), boundaryLine.end());

        // Content-Disposition header
        std::string disposition = "Content-Disposition: form-data; name=\"" + fieldName + "\"";
        if (!fileName.empty()) {
            disposition += "; filename=\"" + fileName + "\"";
        }
        disposition += "\r\n";
        body.insert(body.end(), disposition.begin(), disposition.end());

        // Content-Type header
        std::string contentType = "Content-Type: " + mimeType + "\r\n\r\n";
        body.insert(body.end(), contentType.begin(), contentType.end());

        // File data
        body.insert(body.end(), data.begin(), data.end());

        // Trailing CRLF
        body.push_back('\r');
        body.push_back('\n');
    }

    // Final boundary
    std::string finalBoundary = "--" + boundary + "--\r\n";
    body.insert(body.end(), finalBoundary.begin(), finalBoundary.end());

    return RequestBody(
        std::move(body),
        "multipart/form-data; boundary=" + boundary
    );
}

RequestBody BodyBuilder::fileUpload(const std::filesystem::path& filePath) {
    if (!std::filesystem::exists(filePath)) {
        throw std::filesystem::filesystem_error(
            "File not found",
            filePath,
            std::make_error_code(std::errc::no_such_file_or_directory)
        );
    }

    // Read file content
    std::ifstream file(filePath, std::ios::binary);
    std::vector<uint8_t> content(
        (std::istreambuf_iterator<char>(file)),
        std::istreambuf_iterator<char>()
    );

    std::string mimeType = detail::getMimeType(filePath);
    std::string fileName = filePath.filename().string();

    return buildMultipartBody({
        {"file", fileName, std::move(content), mimeType}
    });
}

RequestBody BodyBuilder::fileUpload(
    const std::vector<std::filesystem::path>& filePaths
) {
    std::vector<std::tuple<std::string, std::string, std::vector<uint8_t>, std::string>> parts;

    int index = 0;
    for (const auto& path : filePaths) {
        if (!std::filesystem::exists(path)) {
            throw std::filesystem::filesystem_error(
                "File not found",
                path,
                std::make_error_code(std::errc::no_such_file_or_directory)
            );
        }

        std::ifstream file(path, std::ios::binary);
        std::vector<uint8_t> content(
            (std::istreambuf_iterator<char>(file)),
            std::istreambuf_iterator<char>()
        );

        parts.emplace_back(
            "file" + std::to_string(index++),
            path.filename().string(),
            std::move(content),
            detail::getMimeType(path)
        );
    }

    return buildMultipartBody(parts);
}

RequestBody BodyBuilder::fileUpload(
    const std::vector<std::pair<std::string, std::filesystem::path>>& files
) {
    std::vector<std::tuple<std::string, std::string, std::vector<uint8_t>, std::string>> parts;

    for (const auto& [fieldName, path] : files) {
        if (!std::filesystem::exists(path)) {
            throw std::filesystem::filesystem_error(
                "File not found",
                path,
                std::make_error_code(std::errc::no_such_file_or_directory)
            );
        }

        std::ifstream file(path, std::ios::binary);
        std::vector<uint8_t> content(
            (std::istreambuf_iterator<char>(file)),
            std::istreambuf_iterator<char>()
        );

        parts.emplace_back(
            fieldName,
            path.filename().string(),
            std::move(content),
            detail::getMimeType(path)
        );
    }

    return buildMultipartBody(parts);
}

RequestBody BodyBuilder::fileUpload(
    const std::string& fieldName,
    const std::string& fileName,
    const std::vector<uint8_t>& data,
    const std::string& mimeType
) {
    return buildMultipartBody({
        {fieldName, fileName, data, mimeType}
    });
}

//=============================================================================
// Raw Content
//=============================================================================

RequestBody BodyBuilder::raw(
    const std::string& content,
    const std::string& contentType
) {
    return RequestBody(content, contentType);
}

RequestBody BodyBuilder::raw(
    const std::vector<uint8_t>& content,
    const std::string& contentType
) {
    return RequestBody(content, contentType);
}

} // namespace modular::fluent
```

**Verification**:
- [ ] Form URL encoding properly escapes special characters
- [ ] JSON body sets correct Content-Type
- [ ] Multipart boundaries are unique and properly formatted
- [ ] File upload throws on missing files
- [ ] Multiple file upload works correctly
- [ ] MIME types are detected correctly

---

## Day 3: Request Implementation (Foundation)

### Task 2.3.1: Create Request Implementation Header
**File**: `src/fluent/Request.h`
**Estimated Time**: 2 hours

**Instructions**:

```cpp
#pragma once

#include <fluent/IRequest.h>
#include <fluent/IHttpFilter.h>
#include "Response.h"
#include "BodyBuilder.h"

#include <atomic>

namespace modular::fluent {

// Forward declarations
class FluentClient;

/// Concrete implementation of IRequest
/// Builds and executes HTTP requests with fluent API
class Request : public IRequest {
public:
    /// Construct a new request
    /// @param client The owning client (for shared configuration)
    /// @param method HTTP method
    /// @param url Full URL or relative path
    Request(
        FluentClient* client,
        HttpMethod method,
        std::string url
    );

    // Non-copyable, movable
    Request(const Request&) = delete;
    Request& operator=(const Request&) = delete;
    Request(Request&&) = default;
    Request& operator=(Request&&) = default;

    ~Request() override = default;

    //=========================================================================
    // IRequest Implementation - Read-only Information
    //=========================================================================

    HttpMethod method() const override;
    std::string url() const override;
    const Headers& headers() const override;
    const RequestOptions& options() const override;

    //=========================================================================
    // IRequest Implementation - URL Arguments
    //=========================================================================

    IRequest& withArgument(std::string_view key, std::string_view value) override;

    IRequest& withArguments(
        const std::vector<std::pair<std::string, std::string>>& arguments
    ) override;

    IRequest& withArguments(
        const std::map<std::string, std::string>& arguments
    ) override;

    //=========================================================================
    // IRequest Implementation - Headers
    //=========================================================================

    IRequest& withHeader(std::string_view key, std::string_view value) override;
    IRequest& withHeaders(const Headers& headers) override;
    IRequest& withoutHeader(std::string_view key) override;

    //=========================================================================
    // IRequest Implementation - Authentication
    //=========================================================================

    IRequest& withAuthentication(
        std::string_view scheme,
        std::string_view parameter
    ) override;

    IRequest& withBearerAuth(std::string_view token) override;
    IRequest& withBasicAuth(std::string_view username, std::string_view password) override;

    //=========================================================================
    // IRequest Implementation - Body
    //=========================================================================

    IRequest& withBody(std::function<RequestBody(IBodyBuilder&)> builder) override;
    IRequest& withBody(RequestBody body) override;

    IRequest& withFormBody(
        const std::vector<std::pair<std::string, std::string>>& fields
    ) override;

    //=========================================================================
    // IRequest Implementation - Options
    //=========================================================================

    IRequest& withOptions(const RequestOptions& options) override;
    IRequest& withIgnoreHttpErrors(bool ignore) override;
    IRequest& withTimeout(std::chrono::seconds timeout) override;
    IRequest& withCancellation(std::stop_token token) override;

    //=========================================================================
    // IRequest Implementation - Filters (Week 3)
    //=========================================================================

    IRequest& withFilter(FilterPtr filter) override;
    IRequest& withoutFilter(const FilterPtr& filter) override;
    IRequest& withRetryConfig(std::shared_ptr<IRetryConfig> config) override;
    IRequest& withNoRetry() override;

    //=========================================================================
    // IRequest Implementation - Custom
    //=========================================================================

    IRequest& withCustom(std::function<void(IRequest&)> customizer) override;

    //=========================================================================
    // IRequest Implementation - Execution (Async)
    //=========================================================================

    std::future<ResponsePtr> asResponseAsync() override;
    std::future<std::string> asStringAsync() override;
    std::future<nlohmann::json> asJsonAsync() override;

    std::future<void> downloadToAsync(
        const std::filesystem::path& path,
        ProgressCallback progress = nullptr
    ) override;

protected:
    void removeFiltersOfType(const std::type_info& type) override;

private:
    FluentClient* client_;  // Non-owning pointer to parent client
    HttpMethod method_;
    std::string baseUrl_;
    std::vector<std::pair<std::string, std::string>> queryParams_;
    Headers headers_;
    RequestOptions options_;
    std::optional<RequestBody> body_;
    std::stop_token cancellationToken_;

    // Request-specific filters (added to client's filters)
    std::vector<FilterPtr> additionalFilters_;
    std::vector<std::type_index> removedFilterTypes_;

    // Request-specific retry config
    std::shared_ptr<IRetryConfig> retryConfig_;
    bool disableRetry_ = false;

    /// Build the full URL with query parameters
    std::string buildFullUrl() const;

    /// Execute the HTTP request (internal)
    ResponsePtr executeInternal();

    /// Apply filters before request
    void applyRequestFilters();

    /// Apply filters after response
    void applyResponseFilters(Response& response);
};

} // namespace modular::fluent
```

---

### Task 2.3.2: Implement Request Class (Part 1 - Building)
**File**: `src/fluent/Request.cpp`
**Estimated Time**: 4 hours

**Instructions**:

Implement the request building methods (URL, headers, body, options). Execution will be completed in Day 4.

```cpp
#include "Request.h"
#include "FluentClient.h"
#include "Utils.h"

#include <sstream>
#include <typeindex>

namespace modular::fluent {

//=============================================================================
// Constructor
//=============================================================================

Request::Request(
    FluentClient* client,
    HttpMethod method,
    std::string url
)
    : client_(client)
    , method_(method)
    , baseUrl_(std::move(url))
{
    // Copy default options from client
    if (client_) {
        options_ = client_->options();
    }
}

//=============================================================================
// Read-only Information
//=============================================================================

HttpMethod Request::method() const {
    return method_;
}

std::string Request::url() const {
    return buildFullUrl();
}

const Headers& Request::headers() const {
    return headers_;
}

const RequestOptions& Request::options() const {
    return options_;
}

//=============================================================================
// URL Building
//=============================================================================

std::string Request::buildFullUrl() const {
    std::string fullUrl = baseUrl_;

    // Add query parameters
    if (!queryParams_.empty()) {
        // Check if URL already has query string
        bool hasQuery = (fullUrl.find('?') != std::string::npos);
        fullUrl += hasQuery ? '&' : '?';
        fullUrl += detail::buildQueryString(queryParams_);
    }

    return fullUrl;
}

//=============================================================================
// URL Arguments
//=============================================================================

IRequest& Request::withArgument(std::string_view key, std::string_view value) {
    // Check if we should ignore null/empty arguments
    if (options_.ignoreNullArguments.value_or(true) && value.empty()) {
        return *this;
    }

    queryParams_.emplace_back(std::string(key), std::string(value));
    return *this;
}

IRequest& Request::withArguments(
    const std::vector<std::pair<std::string, std::string>>& arguments
) {
    for (const auto& [key, value] : arguments) {
        withArgument(key, value);
    }
    return *this;
}

IRequest& Request::withArguments(
    const std::map<std::string, std::string>& arguments
) {
    for (const auto& [key, value] : arguments) {
        withArgument(key, value);
    }
    return *this;
}

//=============================================================================
// Headers
//=============================================================================

IRequest& Request::withHeader(std::string_view key, std::string_view value) {
    headers_[std::string(key)] = std::string(value);
    return *this;
}

IRequest& Request::withHeaders(const Headers& headers) {
    for (const auto& [key, value] : headers) {
        headers_[key] = value;
    }
    return *this;
}

IRequest& Request::withoutHeader(std::string_view key) {
    // Case-insensitive removal
    std::string keyStr(key);
    for (auto it = headers_.begin(); it != headers_.end(); ) {
        if (detail::iequals(it->first, keyStr)) {
            it = headers_.erase(it);
        } else {
            ++it;
        }
    }
    return *this;
}

//=============================================================================
// Authentication
//=============================================================================

IRequest& Request::withAuthentication(
    std::string_view scheme,
    std::string_view parameter
) {
    std::string authValue = std::string(scheme) + " " + std::string(parameter);
    return withHeader("Authorization", authValue);
}

IRequest& Request::withBearerAuth(std::string_view token) {
    return withAuthentication("Bearer", token);
}

IRequest& Request::withBasicAuth(
    std::string_view username,
    std::string_view password
) {
    std::string credentials = std::string(username) + ":" + std::string(password);
    std::string encoded = detail::base64Encode(credentials);
    return withAuthentication("Basic", encoded);
}

//=============================================================================
// Body
//=============================================================================

IRequest& Request::withBody(std::function<RequestBody(IBodyBuilder&)> builder) {
    BodyBuilder bb;
    body_ = builder(bb);

    // Set Content-Type header from body if not already set
    if (!body_->contentType.empty() && headers_.find("Content-Type") == headers_.end()) {
        headers_["Content-Type"] = body_->contentType;
    }

    return *this;
}

IRequest& Request::withBody(RequestBody body) {
    body_ = std::move(body);

    if (!body_->contentType.empty() && headers_.find("Content-Type") == headers_.end()) {
        headers_["Content-Type"] = body_->contentType;
    }

    return *this;
}

IRequest& Request::withFormBody(
    const std::vector<std::pair<std::string, std::string>>& fields
) {
    return withBody([&fields](IBodyBuilder& b) {
        return b.formUrlEncoded(fields);
    });
}

//=============================================================================
// Options
//=============================================================================

IRequest& Request::withOptions(const RequestOptions& options) {
    // Merge options (only override if specified)
    if (options.ignoreHttpErrors.has_value()) {
        options_.ignoreHttpErrors = options.ignoreHttpErrors;
    }
    if (options.ignoreNullArguments.has_value()) {
        options_.ignoreNullArguments = options.ignoreNullArguments;
    }
    if (options.completeWhen.has_value()) {
        options_.completeWhen = options.completeWhen;
    }
    if (options.timeout.has_value()) {
        options_.timeout = options.timeout;
    }
    return *this;
}

IRequest& Request::withIgnoreHttpErrors(bool ignore) {
    options_.ignoreHttpErrors = ignore;
    return *this;
}

IRequest& Request::withTimeout(std::chrono::seconds timeout) {
    options_.timeout = timeout;
    return *this;
}

IRequest& Request::withCancellation(std::stop_token token) {
    cancellationToken_ = std::move(token);
    return *this;
}

//=============================================================================
// Filters
//=============================================================================

IRequest& Request::withFilter(FilterPtr filter) {
    additionalFilters_.push_back(std::move(filter));
    return *this;
}

IRequest& Request::withoutFilter(const FilterPtr& filter) {
    auto it = std::find(additionalFilters_.begin(), additionalFilters_.end(), filter);
    if (it != additionalFilters_.end()) {
        additionalFilters_.erase(it);
    }
    return *this;
}

void Request::removeFiltersOfType(const std::type_info& type) {
    removedFilterTypes_.emplace_back(type);
}

IRequest& Request::withRetryConfig(std::shared_ptr<IRetryConfig> config) {
    retryConfig_ = std::move(config);
    return *this;
}

IRequest& Request::withNoRetry() {
    disableRetry_ = true;
    return *this;
}

//=============================================================================
// Custom
//=============================================================================

IRequest& Request::withCustom(std::function<void(IRequest&)> customizer) {
    customizer(*this);
    return *this;
}

//=============================================================================
// Execution - Placeholder (completed in Day 4)
//=============================================================================

std::future<ResponsePtr> Request::asResponseAsync() {
    return std::async(std::launch::async, [this]() -> ResponsePtr {
        return executeInternal();
    });
}

std::future<std::string> Request::asStringAsync() {
    return std::async(std::launch::async, [this]() {
        auto response = executeInternal();
        return response->asString();
    });
}

std::future<nlohmann::json> Request::asJsonAsync() {
    return std::async(std::launch::async, [this]() {
        auto response = executeInternal();
        return response->asJson();
    });
}

std::future<void> Request::downloadToAsync(
    const std::filesystem::path& path,
    ProgressCallback progress
) {
    return std::async(std::launch::async, [this, path, progress]() {
        auto response = executeInternal();
        response->saveToFile(path, progress);
    });
}

// Placeholder - actual implementation connects to HttpClient in Day 4
ResponsePtr Request::executeInternal() {
    // TODO: Implement in Day 4
    // This will:
    // 1. Apply request filters
    // 2. Call the underlying HttpClient
    // 3. Apply response filters
    // 4. Return Response
    throw std::runtime_error("executeInternal not yet implemented");
}

void Request::applyRequestFilters() {
    // TODO: Implement in Day 4
}

void Request::applyResponseFilters(Response& response) {
    // TODO: Implement in Day 4
}

} // namespace modular::fluent
```

**Verification**:
- [ ] All fluent methods return `*this` for chaining
- [ ] Query parameters are URL-encoded
- [ ] Headers are case-insensitive for removal
- [ ] Basic auth encodes credentials in base64
- [ ] Body Content-Type is set automatically
- [ ] Options are properly merged

---

## Day 4: FluentClient Foundation and Request Execution

### Task 2.4.1: Create FluentClient Implementation Header
**File**: `src/fluent/FluentClient.h`
**Estimated Time**: 2 hours

**Instructions**:

```cpp
#pragma once

#include <fluent/IFluentClient.h>
#include "Request.h"

// Include existing Modular HttpClient
#include <core/HttpClient.h>
#include <core/RateLimiter.h>
#include <core/ILogger.h>

namespace modular::fluent {

/// Concrete implementation of IFluentClient
/// Wraps Modular's existing HttpClient with a fluent API
class FluentClient : public IFluentClient {
public:
    /// Construct with base URL
    explicit FluentClient(std::string_view baseUrl = "");

    /// Construct with base URL and existing rate limiter
    FluentClient(
        std::string_view baseUrl,
        RateLimiterPtr rateLimiter,
        std::shared_ptr<ILogger> logger = nullptr
    );

    ~FluentClient() override;

    // Non-copyable, movable
    FluentClient(const FluentClient&) = delete;
    FluentClient& operator=(const FluentClient&) = delete;
    FluentClient(FluentClient&&) = default;
    FluentClient& operator=(FluentClient&&) = default;

    //=========================================================================
    // IFluentClient Implementation - HTTP Methods
    //=========================================================================

    RequestPtr getAsync(std::string_view resource) override;
    RequestPtr postAsync(std::string_view resource) override;
    RequestPtr putAsync(std::string_view resource) override;
    RequestPtr patchAsync(std::string_view resource) override;
    RequestPtr deleteAsync(std::string_view resource) override;
    RequestPtr headAsync(std::string_view resource) override;
    RequestPtr sendAsync(HttpMethod method, std::string_view resource) override;

    //=========================================================================
    // IFluentClient Implementation - Configuration
    //=========================================================================

    IFluentClient& setBaseUrl(std::string_view baseUrl) override;
    std::string baseUrl() const override;

    IFluentClient& setOptions(const RequestOptions& options) override;
    const RequestOptions& options() const override;

    IFluentClient& setUserAgent(std::string_view userAgent) override;

    //=========================================================================
    // IFluentClient Implementation - Authentication
    //=========================================================================

    IFluentClient& setAuthentication(
        std::string_view scheme,
        std::string_view parameter
    ) override;

    IFluentClient& setBearerAuth(std::string_view token) override;
    IFluentClient& setBasicAuth(std::string_view username, std::string_view password) override;
    IFluentClient& clearAuthentication() override;

    //=========================================================================
    // IFluentClient Implementation - Filters
    //=========================================================================

    FilterCollection& filters() override;
    const FilterCollection& filters() const override;

    //=========================================================================
    // IFluentClient Implementation - Retry
    //=========================================================================

    IFluentClient& setRequestCoordinator(CoordinatorPtr coordinator) override;

    IFluentClient& setRetryPolicy(
        int maxRetries,
        std::function<bool(int statusCode, bool isTimeout)> shouldRetry,
        std::function<std::chrono::milliseconds(int attempt)> getDelay
    ) override;

    IFluentClient& setRetryPolicy(
        std::vector<std::shared_ptr<IRetryConfig>> configs
    ) override;

    IFluentClient& disableRetries() override;
    CoordinatorPtr requestCoordinator() const override;

    //=========================================================================
    // IFluentClient Implementation - Rate Limiting
    //=========================================================================

    IFluentClient& setRateLimiter(RateLimiterPtr rateLimiter) override;
    RateLimiterPtr rateLimiter() const override;

    //=========================================================================
    // IFluentClient Implementation - Defaults
    //=========================================================================

    IFluentClient& addDefault(RequestCustomizer configure) override;
    IFluentClient& clearDefaults() override;

    //=========================================================================
    // IFluentClient Implementation - Timeouts
    //=========================================================================

    IFluentClient& setConnectionTimeout(std::chrono::seconds timeout) override;
    IFluentClient& setRequestTimeout(std::chrono::seconds timeout) override;

    //=========================================================================
    // IFluentClient Implementation - Logging
    //=========================================================================

    IFluentClient& setLogger(std::shared_ptr<ILogger> logger) override;

    //=========================================================================
    // Internal Methods (used by Request)
    //=========================================================================

    /// Execute an HTTP request using the underlying HttpClient
    /// Called by Request::executeInternal()
    ResponsePtr executeRequest(
        HttpMethod method,
        const std::string& url,
        const Headers& headers,
        const std::optional<RequestBody>& body,
        const RequestOptions& options
    );

    /// Get the underlying HttpClient (for advanced usage)
    HttpClient& httpClient();

private:
    std::string baseUrl_;
    RequestOptions defaultOptions_;
    std::string userAgent_ = "Modular-FluentClient/1.0";

    // Authentication
    std::optional<std::pair<std::string, std::string>> authentication_;  // scheme, parameter

    // Filters
    FilterCollection filters_;

    // Coordinator
    CoordinatorPtr coordinator_;

    // Rate limiter
    RateLimiterPtr rateLimiter_;

    // Default customizers
    std::vector<RequestCustomizer> defaults_;

    // Timeouts
    std::chrono::seconds connectionTimeout_{30};
    std::chrono::seconds requestTimeout_{60};

    // Logger
    std::shared_ptr<ILogger> logger_;

    // Underlying HTTP client (Modular's existing implementation)
    std::unique_ptr<HttpClient> httpClient_;

    /// Build full URL from base + resource
    std::string buildUrl(std::string_view resource) const;

    /// Apply default configurations to a request
    void applyDefaults(Request& request);
};

//=============================================================================
// Factory Implementation
//=============================================================================

inline ClientPtr createFluentClient(std::string_view baseUrl) {
    return std::make_unique<FluentClient>(baseUrl);
}

inline ClientPtr createFluentClient(
    std::string_view baseUrl,
    RateLimiterPtr rateLimiter,
    std::shared_ptr<ILogger> logger
) {
    return std::make_unique<FluentClient>(baseUrl, std::move(rateLimiter), std::move(logger));
}

} // namespace modular::fluent
```

---

### Task 2.4.2: Implement FluentClient Class
**File**: `src/fluent/FluentClient.cpp`
**Estimated Time**: 4 hours

**Instructions**:

```cpp
#include "FluentClient.h"
#include "Utils.h"

namespace modular::fluent {

//=============================================================================
// Constructors
//=============================================================================

FluentClient::FluentClient(std::string_view baseUrl)
    : baseUrl_(baseUrl)
{
    // Create underlying HttpClient
    // Note: Modular's HttpClient requires RateLimiter and Logger
    // For now, create with nulls - they can be set later
    httpClient_ = std::make_unique<HttpClient>(nullptr, nullptr);
}

FluentClient::FluentClient(
    std::string_view baseUrl,
    RateLimiterPtr rateLimiter,
    std::shared_ptr<ILogger> logger
)
    : baseUrl_(baseUrl)
    , rateLimiter_(std::move(rateLimiter))
    , logger_(std::move(logger))
{
    // Create underlying HttpClient with provided dependencies
    httpClient_ = std::make_unique<HttpClient>(rateLimiter_.get(), logger_.get());
}

FluentClient::~FluentClient() = default;

//=============================================================================
// URL Building
//=============================================================================

std::string FluentClient::buildUrl(std::string_view resource) const {
    if (baseUrl_.empty()) {
        return std::string(resource);
    }

    std::string url = baseUrl_;

    // Ensure single slash between base and resource
    if (!url.empty() && url.back() == '/') {
        url.pop_back();
    }
    if (!resource.empty() && resource.front() != '/') {
        url += '/';
    }
    url += resource;

    return url;
}

//=============================================================================
// HTTP Methods
//=============================================================================

RequestPtr FluentClient::getAsync(std::string_view resource) {
    return sendAsync(HttpMethod::GET, resource);
}

RequestPtr FluentClient::postAsync(std::string_view resource) {
    return sendAsync(HttpMethod::POST, resource);
}

RequestPtr FluentClient::putAsync(std::string_view resource) {
    return sendAsync(HttpMethod::PUT, resource);
}

RequestPtr FluentClient::patchAsync(std::string_view resource) {
    return sendAsync(HttpMethod::PATCH, resource);
}

RequestPtr FluentClient::deleteAsync(std::string_view resource) {
    return sendAsync(HttpMethod::DELETE, resource);
}

RequestPtr FluentClient::headAsync(std::string_view resource) {
    return sendAsync(HttpMethod::HEAD, resource);
}

RequestPtr FluentClient::sendAsync(HttpMethod method, std::string_view resource) {
    std::string url = buildUrl(resource);
    auto request = std::make_unique<Request>(this, method, std::move(url));

    // Apply client-level authentication
    if (authentication_) {
        request->withAuthentication(authentication_->first, authentication_->second);
    }

    // Apply User-Agent
    if (!userAgent_.empty()) {
        request->withHeader("User-Agent", userAgent_);
    }

    // Apply default customizers
    applyDefaults(*request);

    return request;
}

void FluentClient::applyDefaults(Request& request) {
    for (const auto& customizer : defaults_) {
        customizer(request);
    }
}

//=============================================================================
// Configuration
//=============================================================================

IFluentClient& FluentClient::setBaseUrl(std::string_view baseUrl) {
    baseUrl_ = baseUrl;
    return *this;
}

std::string FluentClient::baseUrl() const {
    return baseUrl_;
}

IFluentClient& FluentClient::setOptions(const RequestOptions& options) {
    defaultOptions_ = options;
    return *this;
}

const RequestOptions& FluentClient::options() const {
    return defaultOptions_;
}

IFluentClient& FluentClient::setUserAgent(std::string_view userAgent) {
    userAgent_ = userAgent;
    return *this;
}

//=============================================================================
// Authentication
//=============================================================================

IFluentClient& FluentClient::setAuthentication(
    std::string_view scheme,
    std::string_view parameter
) {
    authentication_ = std::make_pair(std::string(scheme), std::string(parameter));
    return *this;
}

IFluentClient& FluentClient::setBearerAuth(std::string_view token) {
    return setAuthentication("Bearer", token);
}

IFluentClient& FluentClient::setBasicAuth(
    std::string_view username,
    std::string_view password
) {
    std::string credentials = std::string(username) + ":" + std::string(password);
    return setAuthentication("Basic", detail::base64Encode(credentials));
}

IFluentClient& FluentClient::clearAuthentication() {
    authentication_.reset();
    return *this;
}

//=============================================================================
// Filters
//=============================================================================

FilterCollection& FluentClient::filters() {
    return filters_;
}

const FilterCollection& FluentClient::filters() const {
    return filters_;
}

//=============================================================================
// Retry Configuration
//=============================================================================

IFluentClient& FluentClient::setRequestCoordinator(CoordinatorPtr coordinator) {
    coordinator_ = std::move(coordinator);
    return *this;
}

IFluentClient& FluentClient::setRetryPolicy(
    int maxRetries,
    std::function<bool(int statusCode, bool isTimeout)> shouldRetry,
    std::function<std::chrono::milliseconds(int attempt)> getDelay
) {
    // Create a custom retry config
    class LambdaRetryConfig : public IRetryConfig {
    public:
        LambdaRetryConfig(
            int maxRetries,
            std::function<bool(int, bool)> shouldRetry,
            std::function<std::chrono::milliseconds(int)> getDelay
        )
            : maxRetries_(maxRetries)
            , shouldRetry_(std::move(shouldRetry))
            , getDelay_(std::move(getDelay))
        {}

        int maxRetries() const override { return maxRetries_; }

        bool shouldRetry(int statusCode, bool isTimeout) const override {
            return shouldRetry_(statusCode, isTimeout);
        }

        std::chrono::milliseconds getDelay(int attempt, int /*statusCode*/) const override {
            return getDelay_(attempt);
        }

    private:
        int maxRetries_;
        std::function<bool(int, bool)> shouldRetry_;
        std::function<std::chrono::milliseconds(int)> getDelay_;
    };

    auto config = std::make_shared<LambdaRetryConfig>(
        maxRetries,
        std::move(shouldRetry),
        std::move(getDelay)
    );

    return setRetryPolicy({config});
}

IFluentClient& FluentClient::setRetryPolicy(
    std::vector<std::shared_ptr<IRetryConfig>> configs
) {
    coordinator_ = std::make_shared<RetryCoordinator>(std::move(configs));
    return *this;
}

IFluentClient& FluentClient::disableRetries() {
    coordinator_ = std::make_shared<PassThroughCoordinator>();
    return *this;
}

CoordinatorPtr FluentClient::requestCoordinator() const {
    return coordinator_;
}

//=============================================================================
// Rate Limiting
//=============================================================================

IFluentClient& FluentClient::setRateLimiter(RateLimiterPtr rateLimiter) {
    rateLimiter_ = std::move(rateLimiter);
    // Update underlying HttpClient
    // Note: This may require modifying Modular's HttpClient to support this
    return *this;
}

RateLimiterPtr FluentClient::rateLimiter() const {
    return rateLimiter_;
}

//=============================================================================
// Defaults
//=============================================================================

IFluentClient& FluentClient::addDefault(RequestCustomizer configure) {
    defaults_.push_back(std::move(configure));
    return *this;
}

IFluentClient& FluentClient::clearDefaults() {
    defaults_.clear();
    return *this;
}

//=============================================================================
// Timeouts
//=============================================================================

IFluentClient& FluentClient::setConnectionTimeout(std::chrono::seconds timeout) {
    connectionTimeout_ = timeout;
    return *this;
}

IFluentClient& FluentClient::setRequestTimeout(std::chrono::seconds timeout) {
    requestTimeout_ = timeout;
    return *this;
}

//=============================================================================
// Logging
//=============================================================================

IFluentClient& FluentClient::setLogger(std::shared_ptr<ILogger> logger) {
    logger_ = std::move(logger);
    return *this;
}

//=============================================================================
// Request Execution
//=============================================================================

HttpClient& FluentClient::httpClient() {
    return *httpClient_;
}

ResponsePtr FluentClient::executeRequest(
    HttpMethod method,
    const std::string& url,
    const Headers& headers,
    const std::optional<RequestBody>& body,
    const RequestOptions& options
) {
    // Convert Headers to vector format expected by Modular's HttpClient
    std::vector<std::string> headerVec;
    for (const auto& [key, value] : headers) {
        headerVec.push_back(key + ": " + value);
    }

    auto startTime = std::chrono::steady_clock::now();

    try {
        // Currently Modular's HttpClient only supports GET
        // This will need to be extended for other methods
        if (method == HttpMethod::GET) {
            auto result = httpClient_->get(url, headerVec);

            auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::steady_clock::now() - startTime
            );

            return Response::fromHttpClientResponse(
                result.statusCode,
                result.body,
                result.headers,
                url,
                elapsed
            );
        } else {
            // TODO: Implement POST, PUT, PATCH, DELETE when HttpClient supports them
            throw std::runtime_error(
                "HTTP method " + std::string(to_string(method)) + " not yet supported"
            );
        }
    } catch (const ApiException& e) {
        // Re-throw as fluent exception
        throw fluent::ApiException(
            e.what(),
            e.statusCode(),
            e.statusReason(),
            {},  // Headers not available from Modular exception
            e.responseBody()
        );
    } catch (const NetworkException& e) {
        throw fluent::NetworkException(
            e.what(),
            e.isTimeout() ? fluent::NetworkException::Reason::Timeout
                          : fluent::NetworkException::Reason::ConnectionFailed
        );
    }
}

} // namespace modular::fluent
```

---

### Task 2.4.3: Complete Request Execution
**Update File**: `src/fluent/Request.cpp`
**Estimated Time**: 2 hours

Add the implementation for `executeInternal()`:

```cpp
// Add to Request.cpp

ResponsePtr Request::executeInternal() {
    // Check cancellation
    if (cancellationToken_.stop_requested()) {
        throw NetworkException("Request cancelled", NetworkException::Reason::Unknown);
    }

    // Build the request
    std::string fullUrl = buildFullUrl();

    // Merge with client headers
    Headers allHeaders = headers_;

    // Apply request filters (Week 3 - for now, skip)
    // applyRequestFilters();

    // Execute via client
    auto response = client_->executeRequest(
        method_,
        fullUrl,
        allHeaders,
        body_,
        options_
    );

    // Apply response filters (Week 3 - for now, skip)
    // applyResponseFilters(*response);

    // Check for HTTP errors
    bool ignoreErrors = options_.ignoreHttpErrors.value_or(false);
    if (!ignoreErrors && !response->isSuccessStatusCode()) {
        throw ApiException(
            "HTTP request failed with status " + std::to_string(response->statusCode()),
            response->statusCode(),
            response->statusReason(),
            response->headers(),
            response->asString()
        );
    }

    return response;
}
```

---

## Day 5: Unit Tests and Integration

### Task 2.5.1: Create Response Unit Tests
**File**: `tests/fluent/ResponseTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include "fluent/Response.h"

using namespace modular::fluent;

class ResponseTest : public ::testing::Test {
protected:
    std::unique_ptr<Response> makeResponse(
        int status,
        const std::string& body,
        const Headers& headers = {}
    ) {
        return std::make_unique<Response>(
            status,
            "OK",
            headers,
            body,
            "https://example.com/test",
            std::chrono::milliseconds{100}
        );
    }
};

TEST_F(ResponseTest, StatusCodeSuccess) {
    auto response = makeResponse(200, "");
    EXPECT_TRUE(response->isSuccessStatusCode());
    EXPECT_EQ(response->statusCode(), 200);
}

TEST_F(ResponseTest, StatusCodeError) {
    auto response = makeResponse(404, "Not Found");
    EXPECT_FALSE(response->isSuccessStatusCode());
    EXPECT_EQ(response->statusCode(), 404);
}

TEST_F(ResponseTest, HeaderAccess) {
    Headers headers = {
        {"Content-Type", "application/json"},
        {"X-Custom", "value"}
    };
    auto response = makeResponse(200, "{}", headers);

    EXPECT_EQ(response->header("Content-Type"), "application/json");
    EXPECT_EQ(response->header("content-type"), "application/json");  // Case insensitive
    EXPECT_TRUE(response->hasHeader("X-Custom"));
    EXPECT_FALSE(response->hasHeader("X-Missing"));
}

TEST_F(ResponseTest, AsString) {
    auto response = makeResponse(200, "Hello, World!");
    EXPECT_EQ(response->asString(), "Hello, World!");
}

TEST_F(ResponseTest, AsJson) {
    auto response = makeResponse(200, R"({"name": "test", "value": 42})");
    auto json = response->asJson();

    EXPECT_EQ(json["name"], "test");
    EXPECT_EQ(json["value"], 42);
}

TEST_F(ResponseTest, AsJsonInvalid) {
    auto response = makeResponse(200, "not valid json");
    EXPECT_THROW(response->asJson(), ParseException);
}

TEST_F(ResponseTest, AsTyped) {
    struct TestModel {
        std::string name;
        int value;
        NLOHMANN_DEFINE_TYPE_INTRUSIVE(TestModel, name, value)
    };

    auto response = makeResponse(200, R"({"name": "test", "value": 42})");
    auto model = response->as<TestModel>();

    EXPECT_EQ(model.name, "test");
    EXPECT_EQ(model.value, 42);
}

TEST_F(ResponseTest, ContentType) {
    Headers headers = {{"Content-Type", "application/json; charset=utf-8"}};
    auto response = makeResponse(200, "{}", headers);

    EXPECT_EQ(response->contentType(), "application/json");
}

TEST_F(ResponseTest, ContentLength) {
    Headers headers = {{"Content-Length", "1234"}};
    auto response = makeResponse(200, "", headers);

    EXPECT_EQ(response->contentLength(), 1234);
}

TEST_F(ResponseTest, Elapsed) {
    auto response = makeResponse(200, "");
    EXPECT_EQ(response->elapsed(), std::chrono::milliseconds{100});
}
```

---

### Task 2.5.2: Create BodyBuilder Unit Tests
**File**: `tests/fluent/BodyBuilderTest.cpp`
**Estimated Time**: 1.5 hours

```cpp
#include <gtest/gtest.h>
#include "fluent/BodyBuilder.h"

using namespace modular::fluent;

class BodyBuilderTest : public ::testing::Test {
protected:
    BodyBuilder builder;
};

TEST_F(BodyBuilderTest, FormUrlEncoded) {
    auto body = builder.formUrlEncoded({
        {"name", "John Doe"},
        {"email", "john@example.com"}
    });

    EXPECT_EQ(body.contentType, "application/x-www-form-urlencoded");

    std::string content(body.content.begin(), body.content.end());
    EXPECT_TRUE(content.find("name=John%20Doe") != std::string::npos);
    EXPECT_TRUE(content.find("email=john%40example.com") != std::string::npos);
}

TEST_F(BodyBuilderTest, JsonBody) {
    nlohmann::json json = {{"key", "value"}, {"number", 42}};
    auto body = builder.jsonBody(json);

    EXPECT_EQ(body.contentType, "application/json");

    std::string content(body.content.begin(), body.content.end());
    auto parsed = nlohmann::json::parse(content);
    EXPECT_EQ(parsed["key"], "value");
    EXPECT_EQ(parsed["number"], 42);
}

TEST_F(BodyBuilderTest, RawJson) {
    auto body = builder.rawJson(R"({"raw": true})");

    EXPECT_EQ(body.contentType, "application/json");

    std::string content(body.content.begin(), body.content.end());
    EXPECT_EQ(content, R"({"raw": true})");
}

TEST_F(BodyBuilderTest, RawContent) {
    auto body = builder.raw("plain text content", "text/plain");

    EXPECT_EQ(body.contentType, "text/plain");

    std::string content(body.content.begin(), body.content.end());
    EXPECT_EQ(content, "plain text content");
}

TEST_F(BodyBuilderTest, FileUploadMemory) {
    std::vector<uint8_t> data = {0x01, 0x02, 0x03, 0x04};
    auto body = builder.fileUpload("file", "test.bin", data, "application/octet-stream");

    EXPECT_TRUE(body.contentType.find("multipart/form-data") != std::string::npos);
    EXPECT_TRUE(body.contentType.find("boundary=") != std::string::npos);

    std::string content(body.content.begin(), body.content.end());
    EXPECT_TRUE(content.find("Content-Disposition: form-data") != std::string::npos);
    EXPECT_TRUE(content.find("filename=\"test.bin\"") != std::string::npos);
}
```

---

### Task 2.5.3: Create Integration Test
**File**: `tests/fluent/IntegrationTest.cpp`
**Estimated Time**: 2 hours

```cpp
#include <gtest/gtest.h>
#include <fluent/Fluent.h>

using namespace modular::fluent;

class FluentClientIntegrationTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Create client without real network calls for unit testing
        client = createFluentClient("https://api.example.com");
    }

    ClientPtr client;
};

TEST_F(FluentClientIntegrationTest, CreateGetRequest) {
    auto request = client->getAsync("users");

    EXPECT_EQ(request->method(), HttpMethod::GET);
    EXPECT_EQ(request->url(), "https://api.example.com/users");
}

TEST_F(FluentClientIntegrationTest, RequestWithArguments) {
    auto request = client->getAsync("users")
        ->withArgument("page", "1")
        .withArgument("limit", "10");

    std::string url = request->url();
    EXPECT_TRUE(url.find("page=1") != std::string::npos);
    EXPECT_TRUE(url.find("limit=10") != std::string::npos);
}

TEST_F(FluentClientIntegrationTest, RequestWithHeaders) {
    auto request = client->getAsync("users")
        ->withHeader("X-Custom", "value")
        .withHeader("Accept", "application/json");

    const auto& headers = request->headers();
    EXPECT_EQ(headers.at("X-Custom"), "value");
    EXPECT_EQ(headers.at("Accept"), "application/json");
}

TEST_F(FluentClientIntegrationTest, RequestWithBearerAuth) {
    auto request = client->getAsync("users")
        ->withBearerAuth("my-token");

    const auto& headers = request->headers();
    EXPECT_EQ(headers.at("Authorization"), "Bearer my-token");
}

TEST_F(FluentClientIntegrationTest, ClientLevelAuth) {
    client->setBearerAuth("global-token");
    auto request = client->getAsync("users");

    const auto& headers = request->headers();
    EXPECT_EQ(headers.at("Authorization"), "Bearer global-token");
}

TEST_F(FluentClientIntegrationTest, RequestWithJsonBody) {
    struct Payload {
        std::string name;
        int value;
        NLOHMANN_DEFINE_TYPE_INTRUSIVE(Payload, name, value)
    };

    auto request = client->postAsync("items")
        ->withJsonBody(Payload{"test", 42});

    const auto& headers = request->headers();
    EXPECT_EQ(headers.at("Content-Type"), "application/json");
}

TEST_F(FluentClientIntegrationTest, RequestOptions) {
    auto request = client->getAsync("users")
        ->withIgnoreHttpErrors(true)
        .withTimeout(std::chrono::seconds{30});

    const auto& options = request->options();
    EXPECT_TRUE(options.ignoreHttpErrors.value_or(false));
    EXPECT_EQ(options.timeout.value_or(std::chrono::seconds{0}), std::chrono::seconds{30});
}

TEST_F(FluentClientIntegrationTest, ClientDefaults) {
    client->addDefault([](IRequest& req) {
        req.withHeader("X-Default", "applied");
    });

    auto request = client->getAsync("users");
    const auto& headers = request->headers();
    EXPECT_EQ(headers.at("X-Default"), "applied");
}
```

---

### Task 2.5.4: Create CMakeLists.txt
**File**: `src/fluent/CMakeLists.txt`
**Estimated Time**: 30 minutes

```cmake
# Fluent HTTP Client Implementation Library

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
)

add_library(modular::fluent_client ALIAS fluent_client)

target_include_directories(fluent_client
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/../..>
        $<BUILD_INTERFACE:${CMAKE_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
)

target_compile_features(fluent_client PUBLIC cxx_std_17)

# Dependencies
find_package(nlohmann_json REQUIRED)
find_package(CURL REQUIRED)
find_package(OpenSSL REQUIRED)

target_link_libraries(fluent_client
    PUBLIC
        modular::fluent_interfaces
        nlohmann_json::nlohmann_json
    PRIVATE
        modular::core  # Existing Modular core library
        CURL::libcurl
        OpenSSL::SSL
        OpenSSL::Crypto
)

# Installation
install(TARGETS fluent_client
    EXPORT fluent_client-targets
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

install(
    FILES
        Response.h
        BodyBuilder.h
        Request.h
        FluentClient.h
        Utils.h
    DESTINATION include/fluent/impl
)
```

---

## Deliverables Checklist

### Source Files
- [ ] `src/fluent/Utils.h` - Internal utilities
- [ ] `src/fluent/Response.h` - Response header
- [ ] `src/fluent/Response.cpp` - Response implementation
- [ ] `src/fluent/BodyBuilder.h` - BodyBuilder header
- [ ] `src/fluent/BodyBuilder.cpp` - BodyBuilder implementation
- [ ] `src/fluent/Request.h` - Request header
- [ ] `src/fluent/Request.cpp` - Request implementation (partial)
- [ ] `src/fluent/FluentClient.h` - FluentClient header
- [ ] `src/fluent/FluentClient.cpp` - FluentClient implementation
- [ ] `src/fluent/CMakeLists.txt` - Build configuration

### Test Files
- [ ] `tests/fluent/ResponseTest.cpp` - Response unit tests
- [ ] `tests/fluent/BodyBuilderTest.cpp` - BodyBuilder unit tests
- [ ] `tests/fluent/IntegrationTest.cpp` - Integration tests

### Quality Checks
- [ ] All code compiles without warnings
- [ ] Unit tests pass
- [ ] Code follows project style guide
- [ ] Public APIs are documented

---

## Definition of Done

Week 2 is complete when:

1. ✅ Response class fully implements IResponse
2. ✅ BodyBuilder class fully implements IBodyBuilder
3. ✅ Request class can build requests (fluent methods work)
4. ✅ FluentClient can create requests and execute simple GETs
5. ✅ All unit tests pass
6. ✅ Integration with existing HttpClient established
7. ✅ Code reviewed and merged to feature branch

---

## Notes for Week 3

Week 3 will complete the Request and FluentClient implementations:

1. Complete filter execution in Request
2. Implement retry logic via coordinator
3. Add support for POST, PUT, PATCH, DELETE methods to HttpClient bridge
4. Implement rate limiter integration
5. Add comprehensive error handling
6. Add streaming download support with progress
