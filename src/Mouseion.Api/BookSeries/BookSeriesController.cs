// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mouseion.Core.BookSeries;

namespace Mouseion.Api.BookSeries;

[ApiController]
[Route("api/v3/series")]
[Authorize]
public class BookSeriesController : ControllerBase
{
    private readonly IBookSeriesRepository _seriesRepository;

    public BookSeriesController(IBookSeriesRepository seriesRepository)
    {
        _seriesRepository = seriesRepository;
    }

    [HttpGet]
    public ActionResult<List<BookSeriesResource>> GetAllSeries()
    {
        var series = _seriesRepository.All().ToList();
        return Ok(series.Select(ToResource).ToList());
    }

    [HttpGet("{id:int}")]
    public ActionResult<BookSeriesResource> GetSeries(int id)
    {
        var series = _seriesRepository.Find(id);
        if (series == null)
        {
            return NotFound(new { error = $"Series {id} not found" });
        }

        return Ok(ToResource(series));
    }

    [HttpGet("author/{authorId:int}")]
    public ActionResult<List<BookSeriesResource>> GetSeriesByAuthor(int authorId)
    {
        var series = _seriesRepository.GetByAuthorId(authorId);
        return Ok(series.Select(ToResource).ToList());
    }

    [HttpGet("foreignId/{foreignId}")]
    public ActionResult<BookSeriesResource> GetByForeignId(string foreignId)
    {
        var series = _seriesRepository.FindByForeignId(foreignId);
        if (series == null)
        {
            return NotFound(new { error = $"Series with foreign ID {foreignId} not found" });
        }

        return Ok(ToResource(series));
    }

    [HttpPost]
    public ActionResult<BookSeriesResource> AddSeries([FromBody] BookSeriesResource resource)
    {
        try
        {
            var series = ToModel(resource);
            var added = _seriesRepository.Insert(series);
            return CreatedAtAction(nameof(GetSeries), new { id = added.Id }, ToResource(added));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public ActionResult<BookSeriesResource> UpdateSeries(int id, [FromBody] BookSeriesResource resource)
    {
        var series = _seriesRepository.Find(id);
        if (series == null)
        {
            return NotFound(new { error = $"Series {id} not found" });
        }

        series.Title = resource.Title;
        series.SortTitle = resource.SortTitle;
        series.Description = resource.Description;
        series.ForeignSeriesId = resource.ForeignSeriesId;
        series.AuthorId = resource.AuthorId;
        series.Monitored = resource.Monitored;

        var updated = _seriesRepository.Update(series);
        return Ok(ToResource(updated));
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteSeries(int id)
    {
        var series = _seriesRepository.Find(id);
        if (series == null)
        {
            return NotFound(new { error = $"Series {id} not found" });
        }

        _seriesRepository.Delete(id);
        return NoContent();
    }

    private static BookSeriesResource ToResource(Core.BookSeries.BookSeries series)
    {
        return new BookSeriesResource
        {
            Id = series.Id,
            Title = series.Title,
            SortTitle = series.SortTitle,
            Description = series.Description,
            ForeignSeriesId = series.ForeignSeriesId,
            AuthorId = series.AuthorId,
            Monitored = series.Monitored
        };
    }

    private static Core.BookSeries.BookSeries ToModel(BookSeriesResource resource)
    {
        return new Core.BookSeries.BookSeries
        {
            Id = resource.Id,
            Title = resource.Title,
            SortTitle = resource.SortTitle,
            Description = resource.Description,
            ForeignSeriesId = resource.ForeignSeriesId,
            AuthorId = resource.AuthorId,
            Monitored = resource.Monitored
        };
    }
}

public class BookSeriesResource
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? SortTitle { get; set; }
    public string? Description { get; set; }
    public string? ForeignSeriesId { get; set; }
    public int? AuthorId { get; set; }
    public bool Monitored { get; set; }
}
