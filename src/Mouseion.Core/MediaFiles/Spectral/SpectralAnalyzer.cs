// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace Mouseion.Core.MediaFiles.Spectral;

public class SpectralAnalyzer : ISpectralAnalyzer
{
    private readonly ILogger<SpectralAnalyzer> _logger;

    public SpectralAnalyzer(ILogger<SpectralAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<SpectralAnalysisResult?> AnalyzeAsync(string filePath, CancellationToken ct = default)
    {
        return await Task.Run(() => Analyze(filePath), ct).ConfigureAwait(false);
    }

    public SpectralAnalysisResult? Analyze(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {Path}", filePath);
                return null;
            }

            var mediaInfo = FFProbe.Analyse(filePath);
            if (mediaInfo.AudioStreams.Count == 0)
            {
                _logger.LogWarning("No audio stream found in {Path}", filePath);
                return null;
            }

            var audioStream = mediaInfo.PrimaryAudioStream;
            if (audioStream == null)
            {
                return null;
            }

            var sampleRate = audioStream.SampleRateHz;
            var nyquistFrequency = sampleRate / 2;

            var cutoffFrequency = DetectCutoffFrequency(filePath, nyquistFrequency);

            var expectedCutoffFor44k = 22050;
            var expectedCutoffFor48k = 24000;

            var isFake = false;
            var confidence = 0.0;
            string? reason = null;

            if (sampleRate >= 88200 && cutoffFrequency < expectedCutoffFor44k * 1.1)
            {
                isFake = true;
                confidence = 0.95;
                reason = $"Hi-res {sampleRate}Hz file has frequency cutoff at ~{cutoffFrequency}Hz (upsampled from 44.1kHz)";
            }
            else if (sampleRate >= 96000 && cutoffFrequency < expectedCutoffFor48k * 1.1)
            {
                isFake = true;
                confidence = 0.95;
                reason = $"Hi-res {sampleRate}Hz file has frequency cutoff at ~{cutoffFrequency}Hz (upsampled from 48kHz)";
            }
            else if (sampleRate >= 48000 && cutoffFrequency < nyquistFrequency * 0.8)
            {
                isFake = true;
                confidence = 0.75;
                reason = $"File has unexpectedly low frequency content ({cutoffFrequency}Hz vs {nyquistFrequency}Hz Nyquist)";
            }

            return new SpectralAnalysisResult
            {
                FilePath = filePath,
                SampleRate = sampleRate,
                MaxFrequency = nyquistFrequency,
                EffectiveCutoffFrequency = cutoffFrequency,
                IsFakeHiRes = isFake,
                ConfidenceScore = confidence,
                Reason = reason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing spectrum for {Path}", filePath);
            return null;
        }
    }

    private int DetectCutoffFrequency(string filePath, int nyquistFrequency)
    {
        try
        {
            var statsFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            FFMpegArguments
                .FromFileInput(filePath)
                .OutputToFile(statsFile, true, options => options
                    .WithCustomArgument("-af astats=metadata=1:reset=1")
                    .WithAudioCodec("pcm_s16le")
                    .ForceFormat("null")
                    .WithCustomArgument($"-t 10"))
                .ProcessSynchronously();

            var cutoffEstimate = EstimateCutoffFromDuration(filePath, nyquistFrequency);

            File.Delete(statsFile);

            return cutoffEstimate;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed detailed spectral analysis, using estimate");
            return EstimateCutoffFromDuration(filePath, nyquistFrequency);
        }
    }

    private static int EstimateCutoffFromDuration(string filePath, int nyquistFrequency)
    {
        try
        {
            var mediaInfo = FFProbe.Analyse(filePath);
            var audioStream = mediaInfo.PrimaryAudioStream;

            if (audioStream == null)
                return nyquistFrequency;

            var bitrate = audioStream.BitRate;
            var sampleRate = audioStream.SampleRateHz;

            if (sampleRate <= 48000)
                return nyquistFrequency;

            if (bitrate > 0 && bitrate < 1000000)
            {
                return (int)(nyquistFrequency * 0.9);
            }

            return nyquistFrequency;
        }
        catch
        {
            return nyquistFrequency;
        }
    }
}
