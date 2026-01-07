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
    private string? _dbPath;

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
            // Override IAppFolderInfo to use temp directory for test database
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAppFolderInfo));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var testDir = Path.Combine(Path.GetTempPath(), $"mouseion_test_{Guid.NewGuid()}");
            _dbPath = Path.Combine(testDir, "mouseion.db");
            services.AddSingleton<IAppFolderInfo>(new TestAppFolderInfo(testDir));
        });

        builder.UseEnvironment("Test");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _dbPath != null)
        {
            try
            {
                // Clean up test database directory
                var testDir = Path.GetDirectoryName(_dbPath);
                if (testDir != null && Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        base.Dispose(disposing);
    }
}

public class TestAppFolderInfo : IAppFolderInfo
{
    private readonly string _testDir;

    public TestAppFolderInfo(string testDir)
    {
        _testDir = testDir;
        Directory.CreateDirectory(_testDir);
    }

    public string AppDataFolder => _testDir;
    public string TempFolder => Path.Combine(_testDir, "temp");
    public string StartUpFolder => _testDir;

    public string GetMediaCoverPath()
    {
        return Path.Combine(_testDir, "MediaCover");
    }
}
