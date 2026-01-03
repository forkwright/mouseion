// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.RootFolders;

namespace Mouseion.Core.HealthCheck.Checks;

public class RootFolderCheck : HealthCheckBase
{
    private readonly IRootFolderService _rootFolderService;

    public RootFolderCheck(IRootFolderService rootFolderService)
    {
        _rootFolderService = rootFolderService;
    }

    public override HealthCheck Check()
    {
        var rootFolders = _rootFolderService.All();

        if (rootFolders.Count == 0)
        {
            return new HealthCheck(GetType(), HealthCheckResult.Warning,
                "No root folders configured",
                "#no-root-folders");
        }

        var inaccessible = rootFolders.Where(r => !r.Accessible).ToList();

        if (inaccessible.Any())
        {
            return new HealthCheck(GetType(), HealthCheckResult.Error,
                $"Root folders are inaccessible: {string.Join(", ", inaccessible.Select(r => r.Path))}",
                "#root-folders-inaccessible");
        }

        return new HealthCheck(GetType());
    }
}
