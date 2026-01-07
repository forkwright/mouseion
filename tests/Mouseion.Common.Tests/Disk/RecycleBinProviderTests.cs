// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Common.Disk;
using Mouseion.Common.EnvironmentInfo;

namespace Mouseion.Common.Tests.Disk;

public class RecycleBinProviderTests
{
    private readonly RecycleBinProvider _provider;

    public RecycleBinProviderTests()
    {
        _provider = new RecycleBinProvider();
    }

    [Fact]
    public void IsAvailable_should_return_true_on_supported_platforms()
    {
        // IsAvailable depends on actual OS, test that it returns a boolean
        var isAvailable = _provider.IsAvailable;

        // Should be true on Windows, Linux, or macOS
        if (OsInfo.IsWindows || OsInfo.IsLinux || OsInfo.IsOsx)
        {
            Assert.True(isAvailable);
        }
    }

    [Fact]
    public void DeleteFile_should_return_false_when_file_does_not_exist()
    {
        var result = _provider.DeleteFile("/nonexistent/file.txt");

        Assert.False(result);
    }

    [Fact]
    public void DeleteFolder_should_return_false_when_folder_does_not_exist()
    {
        var result = _provider.DeleteFolder("/nonexistent/folder");

        Assert.False(result);
    }

    [Fact]
    public void GetRecycleBinPath_should_return_path_on_supported_platforms()
    {
        var path = _provider.GetRecycleBinPath();

        if (OsInfo.IsWindows)
        {
            Assert.Equal("$Recycle.Bin", path);
        }
        else if (OsInfo.IsLinux || OsInfo.IsOsx)
        {
            // Should contain .local/share/Trash if it exists
            if (path != null)
            {
                Assert.Contains(".local", path);
                Assert.Contains("Trash", path);
            }
        }
    }
}
