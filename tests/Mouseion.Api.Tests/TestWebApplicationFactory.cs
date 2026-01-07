// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mouseion.Common.EnvironmentInfo;

namespace Mouseion.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _testDbPath;

    public TestWebApplicationFactory()
    {
        // Use temp directory with unique GUID for test database
        _testDbPath = Path.Combine(Path.GetTempPath(), $"mouseion_test_{Guid.NewGuid()}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration including custom AppData path
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey"] = "test-api-key",
                ["AllowedOrigins:0"] = "http://localhost",
                ["AppData"] = _testDbPath
            });
        });

        builder.UseEnvironment("Test");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_testDbPath))
        {
            try
            {
                Directory.Delete(_testDbPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
