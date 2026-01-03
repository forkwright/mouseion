// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Core.SystemInfo;

public class SystemInfo
{
    public string Version { get; set; } = string.Empty;
    public string BuildTime { get; set; } = string.Empty;
    public bool IsDebug { get; set; }
    public bool IsProduction { get; set; }
    public bool IsLinux { get; set; }
    public bool IsOsx { get; set; }
    public bool IsWindows { get; set; }
    public bool IsDocker { get; set; }
    public string OsName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string RuntimeName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string AppData { get; set; } = string.Empty;
}
