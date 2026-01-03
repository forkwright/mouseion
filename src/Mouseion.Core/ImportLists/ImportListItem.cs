// Copyright (C) 2025 Mouseion Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.Datastore;
using Mouseion.Core.MediaTypes;

namespace Mouseion.Core.ImportLists;

/// <summary>
/// Represents a media item discovered from an import list
/// </summary>
public class ImportListItem : ModelBase
{
    public int ListId { get; set; }
    public MediaType MediaType { get; set; }

    // Common identifiers
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }

    // Movie/TV identifiers
    public int TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public int TvdbId { get; set; }

    // Book identifiers
    public string? Isbn { get; set; }
    public long GoodreadsId { get; set; }

    // Music identifiers
    public Guid MusicBrainzId { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }

    // Audiobook identifiers
    public string? Narrator { get; set; }
    public string? Asin { get; set; }

    // Podcast identifiers
    public string? PodcastGuid { get; set; }
    public string? FeedUrl { get; set; }
}
