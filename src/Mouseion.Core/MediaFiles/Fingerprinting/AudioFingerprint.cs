// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Core.MediaFiles.Fingerprinting;

public class AudioFingerprint
{
    public string FilePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int Duration { get; set; }
    public DateTime Generated { get; set; }
}
