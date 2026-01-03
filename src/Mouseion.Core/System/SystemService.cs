// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Runtime.InteropServices;
using Mouseion.Common.EnvironmentInfo;

namespace Mouseion.Core.SystemInfo;

public interface ISystemService
{
    SystemInfo GetSystemInfo();
}

public class SystemService : ISystemService
{
    private readonly IAppFolderInfo _appFolderInfo;
    private readonly DateTime _startTime;

    public SystemService(IAppFolderInfo appFolderInfo)
    {
        _appFolderInfo = appFolderInfo;
        _startTime = DateTime.UtcNow;
    }

    public SystemInfo GetSystemInfo()
    {
        return new SystemInfo
        {
            Version = BuildInfo.Version.ToString(),
            BuildTime = BuildInfo.BuildTime.ToString("o"),
            IsDebug = BuildInfo.IsDebug,
            IsProduction = !BuildInfo.IsDebug,
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            IsOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            IsDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true",
            OsName = RuntimeInformation.OSDescription,
            OsVersion = Environment.OSVersion.Version.ToString(),
            RuntimeVersion = Environment.Version.ToString(),
            RuntimeName = RuntimeInformation.FrameworkDescription,
            StartTime = _startTime,
            AppData = _appFolderInfo.AppDataFolder
        };
    }
}
