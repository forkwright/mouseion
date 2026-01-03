// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.Filtering;
using Mouseion.Core.Music;

namespace Mouseion.Core.Library;

public interface ILibraryFilterService
{
    Task<FilterResult> FilterTracksAsync(FilterRequest request, CancellationToken ct = default);
    FilterResult FilterTracks(FilterRequest request);
}

public class FilterResult
{
    public List<Track> Tracks { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class LibraryFilterService : ILibraryFilterService
{
    private readonly ITrackRepository _trackRepository;
    private readonly IFilterQueryBuilder _queryBuilder;

    public LibraryFilterService(
        ITrackRepository trackRepository,
        IFilterQueryBuilder queryBuilder)
    {
        _trackRepository = trackRepository;
        _queryBuilder = queryBuilder;
    }

    public async Task<FilterResult> FilterTracksAsync(FilterRequest request, CancellationToken ct = default)
    {
        ValidateRequest(request);

        var totalCount = await _trackRepository.CountAsync(ct).ConfigureAwait(false);
        var tracks = await _trackRepository.FilterAsync(request, ct).ConfigureAwait(false);

        return new FilterResult
        {
            Tracks = tracks,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public FilterResult FilterTracks(FilterRequest request)
    {
        ValidateRequest(request);

        var totalCount = _trackRepository.Count();
        var tracks = _trackRepository.Filter(request);

        return new FilterResult
        {
            Tracks = tracks,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private void ValidateRequest(FilterRequest request)
    {
        if (request.Conditions.Count == 0)
        {
            throw new ArgumentException("At least one filter condition is required");
        }

        if (request.Page < 1)
        {
            throw new ArgumentException("Page must be >= 1");
        }

        if (request.PageSize < 1 || request.PageSize > 250)
        {
            throw new ArgumentException("PageSize must be between 1 and 250");
        }

        foreach (var condition in request.Conditions)
        {
            if (string.IsNullOrWhiteSpace(condition.Field))
            {
                throw new ArgumentException("Filter field cannot be empty");
            }

            if (!_queryBuilder.IsValidField(condition.Field))
            {
                throw new ArgumentException($"Field '{condition.Field}' is not allowed for filtering");
            }
        }
    }
}
