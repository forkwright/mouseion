# Streaming API Test Plan

Test HTTP 206 range request support for Akroasis client integration.

## Endpoint

```
GET /api/v3/stream/{mediaId}?path={filePath}
```

## Test Requirements

**Tools**: curl (installed: 8.15.0)

## Test Cases

### 1. Full File Request (HTTP 200)

```bash
curl -v "http://localhost:5000/api/v3/stream/1?path=/path/to/test.flac"
```

**Expected**:
- HTTP 200 OK
- `Accept-Ranges: bytes`
- Full file content

### 2. Range Request (HTTP 206)

```bash
curl -v -H "Range: bytes=0-999" \
  "http://localhost:5000/api/v3/stream/1?path=/path/to/test.flac"
```

**Expected**:
- HTTP 206 Partial Content
- `Content-Range: bytes 0-999/{fileSize}`
- `Content-Length: 1000`
- First 1000 bytes of file

### 3. Range Request - Middle of File

```bash
curl -v -H "Range: bytes=1000-1999" \
  "http://localhost:5000/api/v3/stream/1?path=/path/to/test.flac"
```

**Expected**:
- HTTP 206 Partial Content
- `Content-Range: bytes 1000-1999/{fileSize}`
- `Content-Length: 1000`
- Bytes 1000-1999 of file

### 4. Range Request - From Offset to End

```bash
curl -v -H "Range: bytes=5000-" \
  "http://localhost:5000/api/v3/stream/1?path=/path/to/test.flac"
```

**Expected**:
- HTTP 206 Partial Content
- `Content-Range: bytes 5000-{fileSize-1}/{fileSize}`
- Remaining bytes from offset 5000

### 5. Invalid Range (HTTP 416)

```bash
curl -v -H "Range: bytes=999999999-" \
  "http://localhost:5000/api/v3/stream/1?path=/path/to/test.flac"
```

**Expected**:
- HTTP 416 Range Not Satisfiable
- Error response

### 6. File Not Found (HTTP 404)

```bash
curl -v "http://localhost:5000/api/v3/stream/1?path=/nonexistent/file.flac"
```

**Expected**:
- HTTP 404 Not Found

### 7. Missing Path (HTTP 400)

```bash
curl -v "http://localhost:5000/api/v3/stream/1"
```

**Expected**:
- HTTP 400 Bad Request
- Error: "File path is required"

## MIME Type Detection

**Audio Formats** (Akroasis Priority):
- `.flac` → `audio/flac`
- `.m4b` → `audio/mp4`
- `.mp3` → `audio/mpeg`
- `.dsf` → `audio/dsf` (DSD)
- `.dff` → `audio/dff` (DSD)
- `.wav` → `audio/wav`

**Fallback**: `application/octet-stream`

## Security Considerations

**Path Traversal Protection** (TODO):
- Validate file path is within allowed directories
- Reject paths containing `..`
- Verify mediaId matches file ownership

**Authentication** (TODO):
- Require `X-Api-Key` header
- Return 401 Unauthorized if missing/invalid

## Integration with Akroasis

**Required Headers**:
- `Range: bytes={start}-{end}` for seeking
- `X-Api-Key: {apiKey}` for auth (when implemented)

**Response Headers**:
- `Accept-Ranges: bytes` (enables seeking)
- `Content-Range: bytes {start}-{end}/{total}` (206 responses)
- `Content-Length: {bytes}` (actual content size)
- `Content-Type: {mimeType}` (audio format)

## Next Steps

1. Implement authentication middleware
2. Add path traversal protection
3. Integrate with MediaFile service (database lookup)
4. Add chapter marker support (M4B/MP3 parsing)
5. Performance testing (large files, concurrent requests)
