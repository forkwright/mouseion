// Copyright (C) 2025 Mouseion Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.Datastore;
using Mouseion.Core.MediaTypes;

namespace Mouseion.Core.ImportLists.ImportExclusions;

/// <summary>
/// Excludes specific items from being auto-added by import lists
/// </summary>
public class ImportListExclusion : ModelBase
{
    public MediaType MediaType { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }

    // Movie/TV identifiers
    public int TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public int TvdbId { get; set; }

    // Book identifiers
    public long GoodreadsId { get; set; }
    public string? Isbn { get; set; }

    // Music identifiers
    public Guid MusicBrainzId { get; set; }

    // Audiobook identifiers
    public string? Asin { get; set; }
}
