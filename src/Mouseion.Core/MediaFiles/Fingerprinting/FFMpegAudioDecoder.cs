// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace Mouseion.Core.MediaFiles.Fingerprinting;

public class FFMpegAudioDecoder
{
    private readonly ILogger _logger;

    public FFMpegAudioDecoder(ILogger logger)
    {
        _logger = logger;
    }

    public (short[] samples, int sampleRate, int channels)? DecodeAudio(string filePath)
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
                _logger.LogWarning("No primary audio stream in {Path}", filePath);
                return null;
            }

            var sampleRate = audioStream.SampleRateHz;
            var channels = audioStream.Channels;

            var pcmFile = Path.GetTempFileName();

            FFMpegArguments
                .FromFileInput(filePath)
                .OutputToFile(pcmFile, true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioSamplingRate(sampleRate)
                    .ForceFormat("s16le"))
                .ProcessSynchronously();

            var samples = ReadPcmSamples(pcmFile);
            File.Delete(pcmFile);

            return (samples, sampleRate, channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode audio from {Path}", filePath);
            return null;
        }
    }

    private static short[] ReadPcmSamples(string pcmFilePath)
    {
        using var fs = File.OpenRead(pcmFilePath);
        using var br = new BinaryReader(fs);

        var sampleCount = (int)(fs.Length / sizeof(short));
        var samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = br.ReadInt16();
        }

        return samples;
    }
}
