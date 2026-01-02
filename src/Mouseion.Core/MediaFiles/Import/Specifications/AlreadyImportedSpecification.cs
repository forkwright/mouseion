// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.MediaFiles.Fingerprinting;
using Mouseion.Core.Music;

namespace Mouseion.Core.MediaFiles.Import.Specifications;

public class AlreadyImportedSpecification : IImportSpecification
{
    private readonly IMusicFileRepository _musicFileRepository;
    private readonly IFingerprintService _fingerprintService;
    private readonly ILogger<AlreadyImportedSpecification> _logger;
    private const double FingerprintSimilarityThreshold = 0.95;

    public AlreadyImportedSpecification(
        IMusicFileRepository musicFileRepository,
        IFingerprintService fingerprintService,
        ILogger<AlreadyImportedSpecification> _logger)
    {
        _musicFileRepository = musicFileRepository;
        _fingerprintService = fingerprintService;
        this._logger = _logger;
    }

    public async Task<ImportRejection?> IsSatisfiedByAsync(MusicFileInfo musicFileInfo, CancellationToken ct = default)
    {
        var relativePath = GetRelativePath(musicFileInfo.Path);

        var existingFile = await _musicFileRepository.FindByPathAsync(relativePath, ct).ConfigureAwait(false);

        if (existingFile != null && existingFile.Size == musicFileInfo.Size)
        {
            _logger.LogDebug("File already imported: {Path} (Size: {Size})", musicFileInfo.Path, musicFileInfo.Size);
            return new ImportRejection(
                ImportRejectionReason.AlreadyImported,
                $"File already imported: {relativePath}");
        }

        if (!string.IsNullOrEmpty(musicFileInfo.Fingerprint))
        {
            var duplicates = await _fingerprintService.FindDuplicatesAsync(
                musicFileInfo.Fingerprint,
                FingerprintSimilarityThreshold,
                ct).ConfigureAwait(false);

            if (duplicates.Any())
            {
                var (trackId, similarity) = duplicates.First();
                _logger.LogDebug("Duplicate found via fingerprint: {Path} matches Track {TrackId} ({Similarity:P1})",
                    musicFileInfo.Path, trackId, similarity);
                return new ImportRejection(
                    ImportRejectionReason.AlreadyImported,
                    $"Duplicate file detected (fingerprint match: {similarity:P1})");
            }
        }

        return null;
    }

    public ImportRejection? IsSatisfiedBy(MusicFileInfo musicFileInfo)
    {
        var relativePath = GetRelativePath(musicFileInfo.Path);

        var existingFile = _musicFileRepository.FindByPath(relativePath);

        if (existingFile != null && existingFile.Size == musicFileInfo.Size)
        {
            _logger.LogDebug("File already imported: {Path} (Size: {Size})", musicFileInfo.Path, musicFileInfo.Size);
            return new ImportRejection(
                ImportRejectionReason.AlreadyImported,
                $"File already imported: {relativePath}");
        }

        if (!string.IsNullOrEmpty(musicFileInfo.Fingerprint))
        {
            var duplicates = _fingerprintService.FindDuplicates(
                musicFileInfo.Fingerprint,
                FingerprintSimilarityThreshold);

            if (duplicates.Any())
            {
                var (trackId, similarity) = duplicates.First();
                _logger.LogDebug("Duplicate found via fingerprint: {Path} matches Track {TrackId} ({Similarity:P1})",
                    musicFileInfo.Path, trackId, similarity);
                return new ImportRejection(
                    ImportRejectionReason.AlreadyImported,
                    $"Duplicate file detected (fingerprint match: {similarity:P1})");
            }
        }

        return null;
    }

    private static string GetRelativePath(string fullPath)
    {
        return fullPath;
    }
}
