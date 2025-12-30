# Mouseion Development

Unified media manager for Movies, Books, Audiobooks, Music, Podcasts, and TV Shows.

<critical_rules>

## Git Workflow

CRITICAL: NEVER push directly to main - ALWAYS feature branch → PR
CRITICAL: Keep movie functionality working during development
REQUIRED: Follow global CLAUDE.md coding standards
REQUIRED: Add GPL-3.0 headers to all new source files

</critical_rules>

## Project Info

| Item | Value |
|------|-------|
| Stack | .NET 8.0, React 19, TypeScript |
| Database | SQLite (Dapper ORM) |
| Logging | Serilog |
| Telemetry | OpenTelemetry |
| Real-time | SignalR |
| License | GPL-3.0 (Radarr derivative) |

## Quick Commands

```bash
# Build
dotnet build Mouseion.sln

# Run
dotnet run --project src/Mouseion.Host

# Test
dotnet test

# Frontend dev
cd frontend && npm run dev
```

## Project Structure

```
src/
├── Mouseion.Common/      # Utilities, DI, HTTP, extensions
├── Mouseion.Core/        # Business logic, entities, services
│   ├── Datastore/       # Dapper, migrations
│   ├── MediaItems/      # Base media abstraction
│   ├── Movies/          # Movie entities and services
│   ├── Books/           # Book entities and services
│   ├── Audiobooks/      # Audiobook entities and services
│   ├── Music/           # Artist, Album, Track
│   ├── Podcasts/        # PodcastShow, PodcastEpisode
│   ├── TV/              # TVShow, Season, Episode
│   ├── Download/        # Download clients (16)
│   ├── Indexers/        # Search providers (11)
│   └── Notifications/   # Notification providers (26)
├── Mouseion.Api/         # REST controllers, middleware
├── Mouseion.SignalR/     # Real-time hub
└── Mouseion.Host/        # Entry point, DI setup
```

## MediaType Enum

```csharp
public enum MediaType
{
    Movie = 0,
    Book = 1,
    Audiobook = 2,
    Music = 3,
    Podcast = 4,
    TV = 5
}
```

## API Patterns

Base URL: `/api/v3/`

Standard endpoints per media type:
```
GET    /{type}           # List all
GET    /{type}/{id}      # Get one
POST   /{type}           # Create
PUT    /{type}/{id}      # Update
DELETE /{type}/{id}      # Delete
```

Special endpoints:
```
GET    /stream/{id}                    # File streaming (HTTP 206)
GET    /progress/{type}/{id}           # Get playback progress
POST   /progress/{type}/{id}           # Update progress
POST   /history/listen                 # Record play event
GET    /search?q={query}&type={type}   # Unified search
POST   /bulk/tracks                    # Bulk operations
```

## GPL-3.0 Source Header

Add to all new C# files:
```csharp
// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later
```

## Akroasis Integration

Mouseion serves as the backend for Akroasis (media player client).

**Mouseion owns**:
- Library management
- Metadata
- File streaming
- Download automation
- Progress tracking
- Listening history

**Akroasis owns**:
- Playback state
- Device management
- Queue management
- Playlists

## Key Dependencies

| Package | Purpose |
|---------|---------|
| Dapper | Data access (micro-ORM) |
| Serilog | Structured logging |
| OpenTelemetry | Traces and metrics |
| FluentMigrator | Database migrations |
| FluentValidation | Input validation |
| DryIoc | Dependency injection |
| TagLib# | Audio metadata parsing |
| ImageSharp | Image resizing |

## Quality Ranges

| Type | Range |
|------|-------|
| Movie/TV | 0-31 |
| Book | 100-109 |
| Audiobook | 200-209 |
| Music | 300-389 |
| Podcast | 400-409 |

## Sub-Agent Usage

**Use Explore subagent for**:
- Finding existing patterns in codebase
- Understanding service implementations

**Keep in main session**:
- Direct code changes
- Git operations
- Testing
