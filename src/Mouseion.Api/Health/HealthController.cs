// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mouseion.Core.HealthCheck;

namespace Mouseion.Api.Health;

[Authorize]
[ApiController]
[Route("api/v3/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        var results = _healthCheckService.Results();
        return Ok(results);
    }

    [HttpPost("check")]
    public IActionResult RunHealthChecks()
    {
        _healthCheckService.RunChecks(scheduledOnly: false);
        var results = _healthCheckService.Results();
        return Ok(results);
    }
}
