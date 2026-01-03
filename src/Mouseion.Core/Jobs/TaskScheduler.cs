// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mouseion.Core.Jobs;

public class TaskScheduler : BackgroundService
{
    private readonly IEnumerable<IScheduledTask> _tasks;
    private readonly ILogger<TaskScheduler> _logger;
    private readonly Dictionary<string, DateTime> _lastExecution = new();

    public TaskScheduler(
        IEnumerable<IScheduledTask> tasks,
        ILogger<TaskScheduler> logger)
    {
        _tasks = tasks;
        _logger = logger;

        foreach (var task in _tasks)
        {
            _lastExecution[task.Name] = DateTime.UtcNow;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task scheduler started with {Count} tasks", _tasks.Count());

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var task in _tasks)
                {
                    var nextExecution = _lastExecution[task.Name].AddMinutes(task.Interval);

                    if (DateTime.UtcNow >= nextExecution)
                    {
                        _logger.LogDebug("Executing scheduled task: {TaskName}", task.Name);

                        try
                        {
                            await task.ExecuteAsync(stoppingToken);
                            _lastExecution[task.Name] = DateTime.UtcNow;
                            _logger.LogDebug("Completed scheduled task: {TaskName}", task.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing scheduled task: {TaskName}", task.Name);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Task scheduler stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in task scheduler main loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Task scheduler stopped");
    }
}
