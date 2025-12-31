# Mouseion Development

Inherits all rules from wrapper `/CLAUDE.md`.

## Project-Specific Context

**Repository Structure:**
- Fresh-start migration at `/home/ck/Projects/mouseion-wrapper/mouseion/`
- Archived production version at `/home/ck/Projects/mouseion-wrapper/mouseion-archive-20251230/`
- Active work happens in fresh-start migration

**Current Status:**
- Phase: Phase 0 complete → Phase 1 in progress (quality system)
- Test Suite: 134 tests (115 unit + 19 integration)
- Stack: .NET 10.0, React 19, Serilog, OpenTelemetry

## Key Technical Details

**MediaType Values:**
- Book = 4
- Audiobook = 5
- Music = 6
- TV = 7
- Podcast = 8
- Comic = 9

**Database Schema:**
- `MediaItems` table: Polymorphic storage for all media types
- Filter by `MediaType` column in all queries
- Metadata stored in media-specific columns (JSON where needed)

**Quality System:**
- 103 quality definitions across all media types
- Quality parser for filename-based detection
- Quality profiles for user preferences

**Metadata Providers:**
- Books: OpenLibrary API (https://openlibrary.org)
- Audiobooks: Audnexus API (https://api.audnex.us)
- Torrents: MyAnonamouse indexer

**Async Patterns:**
- All I/O operations use async/await with `CancellationToken ct = default`
- `ConfigureAwait(false)` for library code
- Polly resilience wrapper for SQLite BUSY errors
- Use `QueryFirstOrDefaultAsync` (not `QuerySingleOrDefaultAsync`) for duplicate detection

**Repository Pattern:**
- `IBasicRepository<T>` - Base interface for CRUD operations
- `BasicRepository<T>` - Generic Dapper-based implementation
- Media-specific repositories override methods to filter by MediaType
- Use `QueryFirstOrDefault` for finds (handles duplicates gracefully)

**Testing:**
- Unit tests: Quality parsers, business logic
- Integration tests: Full API testing with TestWebApplicationFactory
- Each test class uses IClassFixture for shared database instance
- In-memory SQLite database per test run

**Dependencies:**
- DryIoc 6.2.0 (stable) for DI container
- Dapper for data access
- Polly 8.6.5 for resilience
- xUnit for testing

## Common Gotchas

1. **QuerySingleOrDefault vs QueryFirstOrDefault**: Always use `QueryFirstOrDefault` for duplicate detection queries - `QuerySingleOrDefault` throws when multiple matches exist

2. **MediaType Filtering**: All queries against `MediaItems` table must include `WHERE "MediaType" = {value}` to prevent cross-contamination

3. **Hard-coded MediaType Values**: Currently using integers directly in queries (e.g., `MediaType = 4`). Planned refactor to use enum constants.

4. **Test Isolation**: Integration tests share database per test class via IClassFixture - tests must use unique data or expect duplicates

## Current Phase Work

**Phase A: Async/Await Modernization (Active)**
- All repository methods have async equivalents
- All service methods are async
- All controller actions are async
- All async methods accept CancellationToken
- DryIoc downgraded to stable version
- All 134 tests passing

**Next: Phase B (Pagination, Validation, Caching)**
