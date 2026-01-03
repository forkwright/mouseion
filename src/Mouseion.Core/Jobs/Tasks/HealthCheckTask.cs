// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.HealthCheck;

namespace Mouseion.Core.Jobs.Tasks;

public class HealthCheckTask : IScheduledTask
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthCheckTask(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public string Name => "Health Check";
    public int Interval => 360; // 6 hours

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _healthCheckService.RunChecks(scheduledOnly: true);
        return Task.CompletedTask;
    }
}
