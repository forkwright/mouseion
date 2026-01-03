// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.RegularExpressions;
using Mouseion.Core.Datastore;

namespace Mouseion.Core.HealthCheck;

public partial class HealthCheck : ModelBase
{
    [GeneratedRegex("[^a-z ]", RegexOptions.Compiled)]
    private static partial Regex CleanFragmentRegex();

    public Type Source { get; set; } = typeof(object);
    public HealthCheckResult Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string WikiUrl { get; set; } = string.Empty;

    public HealthCheck()
    {
    }

    public HealthCheck(Type source)
    {
        Source = source;
        Type = HealthCheckResult.Ok;
    }

    public HealthCheck(Type source, HealthCheckResult type, string message, string? wikiFragment = null)
    {
        Source = source;
        Type = type;
        Message = message;
        WikiUrl = MakeWikiUrl(wikiFragment ?? MakeWikiFragment(message));
    }

    private static string MakeWikiFragment(string message)
    {
        return "#" + CleanFragmentRegex().Replace(message.ToLower(), string.Empty).Replace(' ', '-');
    }

    private static string MakeWikiUrl(string fragment)
    {
        return $"https://wiki.servarr.com/mouseion/system#{fragment}";
    }
}
