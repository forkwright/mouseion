// Copyright (C) 2025 Mouseion Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.Datastore;
using Mouseion.Core.MediaTypes;

namespace Mouseion.Core.ImportLists;

/// <summary>
/// Database-persisted configuration for an import list
/// </summary>
public class ImportListDefinition : ModelBase
{
    public string Name { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool EnableAuto { get; set; }
    public ImportListType ListType { get; set; }
    public MediaType MediaType { get; set; }
    public TimeSpan MinRefreshInterval { get; set; }

    // Monitor settings (applies when adding items)
    public bool Monitor { get; set; }
    public int QualityProfileId { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public bool SearchOnAdd { get; set; }

    // Settings as JSON
    public string Settings { get; set; } = "{}";
    public List<string> Tags { get; set; } = new();
}
