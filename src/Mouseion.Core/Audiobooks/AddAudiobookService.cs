// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.Authors;

namespace Mouseion.Core.Audiobooks;

public interface IAddAudiobookService
{
    Task<Audiobook> AddAudiobookAsync(Audiobook audiobook, CancellationToken ct = default);
    Task<List<Audiobook>> AddAudiobooksAsync(List<Audiobook> audiobooks, CancellationToken ct = default);

    Audiobook AddAudiobook(Audiobook audiobook);
    List<Audiobook> AddAudiobooks(List<Audiobook> audiobooks);
}

public class AddAudiobookService : IAddAudiobookService
{
    private readonly IAudiobookRepository _audiobookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ILogger<AddAudiobookService> _logger;

    public AddAudiobookService(
        IAudiobookRepository audiobookRepository,
        IAuthorRepository authorRepository,
        ILogger<AddAudiobookService> logger)
    {
        _audiobookRepository = audiobookRepository;
        _authorRepository = authorRepository;
        _logger = logger;
    }

    public async Task<Audiobook> AddAudiobookAsync(Audiobook audiobook, CancellationToken ct = default)
    {
        ValidateAudiobook(audiobook);

        // Verify author exists
        if (audiobook.AuthorId.HasValue)
        {
            var author = await _authorRepository.FindAsync(audiobook.AuthorId.Value, ct).ConfigureAwait(false);
            if (author == null)
            {
                throw new ArgumentException($"Author with ID {audiobook.AuthorId.Value} not found", nameof(audiobook));
            }
        }

        // Check for existing audiobook
        if (audiobook.AuthorId.HasValue)
        {
            var existing = await _audiobookRepository.FindByTitleAsync(audiobook.Title, audiobook.Year, ct).ConfigureAwait(false);
            if (existing != null && existing.AuthorId == audiobook.AuthorId)
            {
                _logger.LogInformation("Audiobook already exists: {AudiobookTitle} ({Year}) - Narrator: {Narrator}",
                    existing.Title, existing.Year, existing.Metadata.Narrator);
                return existing;
            }
        }

        // Set defaults
        audiobook.Added = DateTime.UtcNow;
        audiobook.Monitored = true;

        var added = await _audiobookRepository.InsertAsync(audiobook, ct).ConfigureAwait(false);
        _logger.LogInformation("Added audiobook: {AudiobookTitle} ({Year}) - Narrator: {Narrator}, Author ID: {AuthorId}",
            added.Title, added.Year, added.Metadata.Narrator, added.AuthorId);

        return added;
    }

    public Audiobook AddAudiobook(Audiobook audiobook)
    {
        ValidateAudiobook(audiobook);

        // Verify author exists
        if (audiobook.AuthorId.HasValue)
        {
            var author = _authorRepository.Find(audiobook.AuthorId.Value);
            if (author == null)
            {
                throw new ArgumentException($"Author with ID {audiobook.AuthorId.Value} not found", nameof(audiobook));
            }
        }

        // Check for existing audiobook
        if (audiobook.AuthorId.HasValue)
        {
            var existing = _audiobookRepository.FindByTitle(audiobook.Title, audiobook.Year);
            if (existing != null && existing.AuthorId == audiobook.AuthorId)
            {
                _logger.LogInformation("Audiobook already exists: {AudiobookTitle} ({Year}) - Narrator: {Narrator}",
                    existing.Title, existing.Year, existing.Metadata.Narrator);
                return existing;
            }
        }

        // Set defaults
        audiobook.Added = DateTime.UtcNow;
        audiobook.Monitored = true;

        var added = _audiobookRepository.Insert(audiobook);
        _logger.LogInformation("Added audiobook: {AudiobookTitle} ({Year}) - Narrator: {Narrator}, Author ID: {AuthorId}",
            added.Title, added.Year, added.Metadata.Narrator, added.AuthorId);

        return added;
    }

    public async Task<List<Audiobook>> AddAudiobooksAsync(List<Audiobook> audiobooks, CancellationToken ct = default)
    {
        var addedAudiobooks = new List<Audiobook>();

        foreach (var audiobook in audiobooks)
        {
            try
            {
                var added = await AddAudiobookAsync(audiobook, ct).ConfigureAwait(false);
                addedAudiobooks.Add(added);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding audiobook: {AudiobookTitle}", audiobook.Title);
            }
        }

        return addedAudiobooks;
    }

    public List<Audiobook> AddAudiobooks(List<Audiobook> audiobooks)
    {
        var addedAudiobooks = new List<Audiobook>();

        foreach (var audiobook in audiobooks)
        {
            try
            {
                var added = AddAudiobook(audiobook);
                addedAudiobooks.Add(added);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding audiobook: {AudiobookTitle}", audiobook.Title);
            }
        }

        return addedAudiobooks;
    }

    private static void ValidateAudiobook(Audiobook audiobook)
    {
        if (string.IsNullOrWhiteSpace(audiobook.Title))
        {
            throw new ArgumentException("Audiobook title is required", nameof(audiobook));
        }

        if (audiobook.QualityProfileId <= 0)
        {
            throw new ArgumentException("Quality profile ID must be set", nameof(audiobook));
        }
    }
}
