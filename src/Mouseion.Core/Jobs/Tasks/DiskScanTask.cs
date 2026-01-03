// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;

namespace Mouseion.Core.Jobs.Tasks;

public class DiskScanTask : IScheduledTask
{
    private readonly ILogger<DiskScanTask> _logger;

    public DiskScanTask(ILogger<DiskScanTask> logger)
    {
        _logger = logger;
    }

    public string Name => "Disk Scan";
    public int Interval => 1440; // 24 hours

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Disk scan task executed (placeholder)");
        // TODO: Implement full disk scanning when MediaFile scanning is ready
        return Task.CompletedTask;
    }
}
