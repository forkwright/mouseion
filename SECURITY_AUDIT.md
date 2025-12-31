# Security & Performance Audit - Fixes Tracker

**Audit Date:** 2025-12-31
**Status:** In Progress

## CRITICAL - Immediate Action Required

### 1. Dependencies (2 Critical)

- [ ] **CVE-2013-5042: SignalR 1.1.0 XSS Vulnerability**
  - Current: Microsoft.AspNetCore.SignalR.Core 1.1.0
  - Required: Upgrade to 1.1.4+ or migrate to modern SignalR
  - File: `src/Mouseion.SignalR/Mouseion.SignalR.csproj:9`
  - Severity: HIGH

- [ ] **.NET Version Mismatch**
  - Issue: .NET 8.0 target using .NET 9.0 packages
  - Packages: Microsoft.Extensions.Hosting.WindowsServices 9.0.0, Microsoft.Data.Sqlite 9.0.0, Npgsql 9.0.2
  - Files: `src/Mouseion.Common/Mouseion.Common.csproj`, `src/Mouseion.Core/Mouseion.Core.csproj`
  - Severity: MEDIUM

### 2. Security (7 Critical/High)

- [ ] **Missing Authentication on API Endpoints**
  - File: `src/Mouseion.Api/Chapters/ChaptersController.cs:13-45`
  - Fix: Add `[Authorize]` attribute or API key validation
  - Severity: CRITICAL

- [ ] **Path Traversal Vulnerability**
  - File: `src/Mouseion.Api/Chapters/ChaptersController.cs:28-44`
  - Fix: Validate and sanitize file paths before access
  - Severity: CRITICAL

- [ ] **MD5 Cryptographic Weakness**
  - File: `src/Mouseion.Common/Crypto/HashProvider.cs:19-28`
  - Fix: Replace MD5 with SHA256 for integrity checks
  - Severity: HIGH

- [ ] **SHA1 Cryptographic Weakness**
  - File: `src/Mouseion.Common/Crypto/HashConverter.cs:21-25`
  - Fix: Replace SHA1 with SHA256
  - Severity: HIGH

- [ ] **Command Injection - PowerShell**
  - File: `src/Mouseion.Common/Processes/ProcessProvider.cs:355-373`
  - Fix: Escape arguments, remove `-ExecutionPolicy Bypass`
  - Severity: HIGH

- [ ] **Command Injection - cmd.exe**
  - File: `src/Mouseion.Common/Processes/ProcessProvider.cs:355-373`
  - Fix: Properly escape arguments
  - Severity: HIGH

- [ ] **Missing HTTP Security Headers**
  - File: `src/Mouseion.Host/Program.cs:75-80`
  - Fix: Add Strict-Transport-Security, X-Content-Type-Options, X-Frame-Options, CSP
  - Severity: HIGH

### 3. Performance (5 Critical)

- [ ] **Blocking Async Calls - Thread Pool Starvation**
  - File: `src/Mouseion.Common/Http/HttpClient.cs` (lines 133, 318, 329, 343, 354, 365)
  - Fix: Remove `.GetAwaiter().GetResult()`, make callers async
  - Severity: CRITICAL

- [ ] **N+1 Database Queries**
  - File: `src/Mouseion.Core/Datastore/BasicRepository.cs:104-115`
  - Fix: Implement batch operations for InsertMany/UpdateMany
  - Severity: CRITICAL

- [ ] **Uncached Reflection in Hot Path**
  - File: `src/Mouseion.Core/Datastore/BasicRepository.cs:120-146`
  - Fix: Cache PropertyInfo and SQL statements
  - Severity: CRITICAL

- [ ] **Lock Contention on HTTP Requests**
  - File: `src/Mouseion.Common/Http/HttpClient.cs:185,244`
  - Fix: Use ConcurrentDictionary instead of lock
  - Severity: HIGH

- [ ] **Blocking Thread.Sleep in Request Path**
  - File: `src/Mouseion.Common/TPL/RateLimitService.cs:52`
  - Fix: Replace with async Task.Delay
  - Severity: HIGH

## HIGH Priority

### Dependencies
- [ ] OpenTelemetry version alignment (1.10.0 → 1.14.0)
- [ ] DryIoc preview-04 → stable 8.x when available

### Security
- [ ] Weak PRNG for OAuth (use RandomNumberGenerator)
- [ ] AllowedHosts wildcard
- [ ] Missing HTTPS enforcement
- [ ] Bare catch blocks
- [ ] ReDoS potential in regex

### Performance
- [ ] Multiple filesystem enumerations
- [ ] ToList() on property access
- [ ] Process.GetProcesses() not cached

## MEDIUM Priority
- [ ] CORS configuration
- [ ] Connection string parsing fragility
- [ ] Inefficient string operations

## Fix Strategy

1. **Week 1 Day 1**: Document issues (DONE)
2. **Phase 1**: Fix CRITICAL security (authentication, path traversal, crypto)
3. **Phase 1**: Upgrade vulnerable dependencies
4. **Phase 1**: Fix CRITICAL performance (async blocking, N+1 queries)
5. **Phase 2**: Fix HIGH severity issues
6. **Phase 3**: Fix MEDIUM severity issues during code cleanup

## Notes

- These fixes will be integrated into Phase 1 work (quality system development)
- Each fix will include tests
- Security fixes take precedence over features
- Performance fixes tracked separately from feature development
