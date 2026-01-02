// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.MediaFiles.Fingerprinting;
using Mouseion.Core.Music;
using Mouseion.Core.Qualities;

namespace Mouseion.Core.MediaFiles.Import.Specifications;

public class UpgradeSpecification : IImportSpecification
{
    private readonly IMusicFileRepository _musicFileRepository;
    private readonly IFingerprintService _fingerprintService;
    private readonly ILogger<UpgradeSpecification> _logger;

    public UpgradeSpecification(
        IMusicFileRepository musicFileRepository,
        IFingerprintService fingerprintService,
        ILogger<UpgradeSpecification> logger)
    {
        _musicFileRepository = musicFileRepository;
        _fingerprintService = fingerprintService;
        _logger = logger;
    }

    public Task<ImportRejection?> IsSatisfiedByAsync(MusicFileInfo musicFileInfo, CancellationToken ct = default)
    {
        return Task.FromResult(IsSatisfiedBy(musicFileInfo));
    }

    public ImportRejection? IsSatisfiedBy(MusicFileInfo musicFileInfo)
    {
        var existingFile = _musicFileRepository.FindByPath(musicFileInfo.Path);

        if (existingFile == null && !string.IsNullOrEmpty(musicFileInfo.Fingerprint))
        {
            var duplicates = _fingerprintService.FindDuplicates(musicFileInfo.Fingerprint, 0.95);
            if (duplicates.Any())
            {
                var (trackId, _) = duplicates.First();
                existingFile = _musicFileRepository.Find(trackId);
            }
        }

        if (existingFile == null)
        {
            return null;
        }

        var existingQuality = existingFile.Quality?.Quality ?? Quality.MusicUnknown;
        var newQuality = musicFileInfo.Quality;

        if ((int)newQuality > (int)existingQuality)
        {
            _logger.LogInformation("Quality upgrade: {Path} ({OldQuality} → {NewQuality})",
                musicFileInfo.Path, existingQuality, newQuality);
            return null;
        }

        if ((int)newQuality < (int)existingQuality)
        {
            _logger.LogDebug("Not a quality upgrade: {Path} ({NewQuality} vs existing {OldQuality})",
                musicFileInfo.Path, newQuality, existingQuality);
            return new ImportRejection(
                ImportRejectionReason.NotQualityUpgrade,
                $"File quality ({newQuality}) is not better than existing ({existingQuality})");
        }

        return null;
    }
}
