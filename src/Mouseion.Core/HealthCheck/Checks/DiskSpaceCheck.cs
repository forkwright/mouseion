// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.RootFolders;

namespace Mouseion.Core.HealthCheck.Checks;

public class DiskSpaceCheck : HealthCheckBase
{
    private readonly IRootFolderService _rootFolderService;
    private const long MinimumFreeSpace = 100 * 1024 * 1024; // 100 MB

    public DiskSpaceCheck(IRootFolderService rootFolderService)
    {
        _rootFolderService = rootFolderService;
    }

    public override HealthCheck Check()
    {
        var rootFolders = _rootFolderService.GetAll();

        var lowSpace = rootFolders
            .Where(r => r.Accessible && r.FreeSpace.HasValue && r.FreeSpace.Value < MinimumFreeSpace)
            .ToList();

        if (lowSpace.Any())
        {
            return new HealthCheck(GetType(), HealthCheckResult.Error,
                $"Low disk space on: {string.Join(", ", lowSpace.Select(r => r.Path))}",
                "#low-disk-space");
        }

        return new HealthCheck(GetType());
    }
}
