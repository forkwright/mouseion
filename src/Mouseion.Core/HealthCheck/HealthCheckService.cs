// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mouseion.Core.HealthCheck;

public interface IHealthCheckService
{
    List<HealthCheck> Results();
    void RunChecks(bool scheduledOnly = true);
}

public class HealthCheckService : IHealthCheckService
{
    private readonly IEnumerable<IProvideHealthCheck> _healthChecks;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ConcurrentDictionary<string, HealthCheck> _healthCheckResults;

    public HealthCheckService(
        IEnumerable<IProvideHealthCheck> healthChecks,
        ILogger<HealthCheckService> logger)
    {
        _healthChecks = healthChecks;
        _logger = logger;
        _healthCheckResults = new ConcurrentDictionary<string, HealthCheck>();
    }

    public List<HealthCheck> Results()
    {
        return _healthCheckResults.Values.ToList();
    }

    public void RunChecks(bool scheduledOnly = true)
    {
        var checksToRun = scheduledOnly
            ? _healthChecks.Where(h => h.CheckOnSchedule)
            : _healthChecks;

        foreach (var check in checksToRun)
        {
            try
            {
                _logger.LogTrace("Running health check: {CheckType}", check.GetType().Name);
                var result = check.Check();

                if (result.Type == HealthCheckResult.Ok)
                {
                    _healthCheckResults.TryRemove(result.Source.Name, out _);
                }
                else
                {
                    _healthCheckResults[result.Source.Name] = result;
                }

                _logger.LogTrace("Health check completed: {CheckType} - {Result}",
                    check.GetType().Name, result.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed: {CheckType}", check.GetType().Name);
            }
        }
    }
}
