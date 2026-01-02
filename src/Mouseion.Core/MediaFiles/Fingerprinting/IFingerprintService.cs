// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Core.MediaFiles.Fingerprinting;

public interface IFingerprintService
{
    Task<AudioFingerprint?> FingerprintAsync(string filePath, CancellationToken ct = default);
    AudioFingerprint? Fingerprint(string filePath);

    Task<List<(int trackId, double similarity)>> FindDuplicatesAsync(
        string fingerprint,
        double similarityThreshold = 0.9,
        CancellationToken ct = default);
    List<(int trackId, double similarity)> FindDuplicates(
        string fingerprint,
        double similarityThreshold = 0.9);

    double CalculateSimilarity(string fingerprint1, string fingerprint2);
}
