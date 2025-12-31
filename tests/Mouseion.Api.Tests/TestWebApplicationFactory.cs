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
using Mouseion.Core.Datastore;

namespace Mouseion.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey"] = "test-api-key",
                ["AllowedOrigins:0"] = "http://localhost"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Use in-memory database for testing
            var dbPath = Path.Combine(Path.GetTempPath(), $"mouseion_test_{Guid.NewGuid()}.db");

            // Override IAppFolderInfo to use temp directory
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAppFolderInfo));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddSingleton<IAppFolderInfo>(new TestAppFolderInfo(dbPath));
        });

        builder.UseEnvironment("Test");
    }
}

public class TestAppFolderInfo : IAppFolderInfo
{
    private readonly string _testPath;

    public TestAppFolderInfo(string testPath)
    {
        _testPath = Path.GetDirectoryName(testPath) ?? Path.GetTempPath();
    }

    public string AppDataFolder => _testPath;
    public string TempFolder => Path.Combine(_testPath, "temp");
    public string StartUpFolder => _testPath;
}
