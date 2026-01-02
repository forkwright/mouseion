// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Core.MediaFiles.Spectral;

public interface ISpectralAnalyzer
{
    Task<SpectralAnalysisResult?> AnalyzeAsync(string filePath, CancellationToken ct = default);
    SpectralAnalysisResult? Analyze(string filePath);
}

public class SpectralAnalysisResult
{
    public string FilePath { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public int MaxFrequency { get; set; }
    public int EffectiveCutoffFrequency { get; set; }
    public bool IsFakeHiRes { get; set; }
    public double ConfidenceScore { get; set; }
    public string? Reason { get; set; }
}
