// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Mouseion.Core.Music;

namespace Mouseion.Core.MediaFiles.Fingerprinting;

public class FingerprintService : IFingerprintService
{
    private readonly ILogger<FingerprintService> _logger;
    private readonly IMusicFileRepository _musicFileRepository;

    public FingerprintService(
        ILogger<FingerprintService> logger,
        IMusicFileRepository musicFileRepository)
    {
        _logger = logger;
        _musicFileRepository = musicFileRepository;
    }

    public async Task<AudioFingerprint?> FingerprintAsync(string filePath, CancellationToken ct = default)
    {
        return await Task.Run(() => Fingerprint(filePath), ct).ConfigureAwait(false);
    }

    public AudioFingerprint? Fingerprint(string filePath)
    {
        try
        {
            var decoder = new FFMpegAudioDecoder(_logger);
            var audioData = decoder.DecodeAudio(filePath);

            if (!audioData.HasValue)
            {
                return null;
            }

            var (samples, sampleRate, channels) = audioData.Value;

            var hash = GenerateAudioHash(samples, sampleRate, channels);
            var duration = (int)(samples.Length / (double)sampleRate / channels);

            return new AudioFingerprint
            {
                FilePath = filePath,
                Hash = hash,
                Duration = duration,
                Generated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating fingerprint for {Path}", filePath);
            return null;
        }
    }

    public async Task<List<(int trackId, double similarity)>> FindDuplicatesAsync(
        string fingerprint,
        double similarityThreshold = 0.9,
        CancellationToken ct = default)
    {
        return await Task.Run(() => FindDuplicates(fingerprint, similarityThreshold), ct).ConfigureAwait(false);
    }

    public List<(int trackId, double similarity)> FindDuplicates(
        string fingerprint,
        double similarityThreshold = 0.9)
    {
        var duplicates = new List<(int, double)>();

        var allMusicFiles = _musicFileRepository.All();

        foreach (var musicFile in allMusicFiles)
        {
            if (string.IsNullOrEmpty(musicFile.Fingerprint))
                continue;

            var similarity = CalculateSimilarity(fingerprint, musicFile.Fingerprint);
            if (similarity >= similarityThreshold)
            {
                duplicates.Add((musicFile.Id, similarity));
            }
        }

        return duplicates.OrderByDescending(d => d.Item2).ToList();
    }

    public double CalculateSimilarity(string fingerprint1, string fingerprint2)
    {
        if (fingerprint1 == fingerprint2)
            return 1.0;

        try
        {
            var bytes1 = Convert.FromBase64String(fingerprint1);
            var bytes2 = Convert.FromBase64String(fingerprint2);

            if (bytes1.Length != bytes2.Length)
                return 0.0;

            int differences = 0;
            for (int i = 0; i < bytes1.Length; i++)
            {
                byte xor = (byte)(bytes1[i] ^ bytes2[i]);
                differences += CountSetBits(xor);
            }

            int maxBits = bytes1.Length * 8;
            return 1.0 - ((double)differences / maxBits);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid fingerprint format");
            return 0.0;
        }
    }

    private static string GenerateAudioHash(short[] samples, int sampleRate, int channels)
    {
        const int chunkSize = 4096;
        var chunkCount = Math.Min(100, samples.Length / chunkSize);
        var hashData = new List<byte>();

        for (int i = 0; i < chunkCount; i++)
        {
            var chunkStart = i * chunkSize;
            long sum = 0;

            for (int j = 0; j < chunkSize && (chunkStart + j) < samples.Length; j++)
            {
                sum += Math.Abs(samples[chunkStart + j]);
            }

            var average = (int)(sum / chunkSize);
            hashData.AddRange(BitConverter.GetBytes(average));
        }

        hashData.AddRange(BitConverter.GetBytes(sampleRate));
        hashData.AddRange(BitConverter.GetBytes(channels));
        hashData.AddRange(BitConverter.GetBytes(samples.Length));

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(hashData.ToArray());
        return Convert.ToBase64String(hash);
    }

    private static int CountSetBits(byte value)
    {
        int count = 0;
        while (value > 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }
}
