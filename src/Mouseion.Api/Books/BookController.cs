// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mouseion.Core.Books;

namespace Mouseion.Api.Books;

[ApiController]
[Route("api/v3/books")]
[Authorize]
public class BookController : ControllerBase
{
    private readonly IBookRepository _bookRepository;
    private readonly IAddBookService _addBookService;
    private readonly IBookStatisticsService _statisticsService;

    public BookController(
        IBookRepository bookRepository,
        IAddBookService addBookService,
        IBookStatisticsService statisticsService)
    {
        _bookRepository = bookRepository;
        _addBookService = addBookService;
        _statisticsService = statisticsService;
    }

    [HttpGet]
    public ActionResult<List<BookResource>> GetBooks()
    {
        var books = _bookRepository.All().ToList();
        return Ok(books.Select(ToResource).ToList());
    }

    [HttpGet("{id:int}")]
    public ActionResult<BookResource> GetBook(int id)
    {
        var book = _bookRepository.Find(id);
        if (book == null)
        {
            return NotFound(new { error = $"Book {id} not found" });
        }

        return Ok(ToResource(book));
    }

    [HttpGet("author/{authorId:int}")]
    public ActionResult<List<BookResource>> GetBooksByAuthor(int authorId)
    {
        var books = _bookRepository.GetByAuthorId(authorId);
        return Ok(books.Select(ToResource).ToList());
    }

    [HttpGet("series/{seriesId:int}")]
    public ActionResult<List<BookResource>> GetBooksBySeries(int seriesId)
    {
        var books = _bookRepository.GetBySeriesId(seriesId);
        return Ok(books.Select(ToResource).ToList());
    }

    [HttpGet("statistics")]
    public ActionResult<BookStatistics> GetStatistics()
    {
        var stats = _statisticsService.GetStatistics();
        return Ok(stats);
    }

    [HttpGet("statistics/author/{authorId:int}")]
    public ActionResult<BookStatistics> GetAuthorStatistics(int authorId)
    {
        var stats = _statisticsService.GetAuthorStatistics(authorId);
        return Ok(stats);
    }

    [HttpGet("statistics/series/{seriesId:int}")]
    public ActionResult<BookStatistics> GetSeriesStatistics(int seriesId)
    {
        var stats = _statisticsService.GetSeriesStatistics(seriesId);
        return Ok(stats);
    }

    [HttpPost]
    public ActionResult<BookResource> AddBook([FromBody] BookResource resource)
    {
        try
        {
            var book = ToModel(resource);
            var added = _addBookService.AddBook(book);
            return CreatedAtAction(nameof(GetBook), new { id = added.Id }, ToResource(added));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("batch")]
    public ActionResult<List<BookResource>> AddBooks([FromBody] List<BookResource> resources)
    {
        try
        {
            var books = resources.Select(ToModel).ToList();
            var added = _addBookService.AddBooks(books);
            return Ok(added.Select(ToResource).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public ActionResult<BookResource> UpdateBook(int id, [FromBody] BookResource resource)
    {
        var book = _bookRepository.Find(id);
        if (book == null)
        {
            return NotFound(new { error = $"Book {id} not found" });
        }

        book.Title = resource.Title;
        book.Year = resource.Year;
        book.Monitored = resource.Monitored;
        book.QualityProfileId = resource.QualityProfileId;
        book.AuthorId = resource.AuthorId;
        book.BookSeriesId = resource.BookSeriesId;
        book.Tags = resource.Tags?.ToHashSet() ?? new HashSet<int>();
        book.Metadata = ToMetadata(resource.Metadata);

        var updated = _bookRepository.Update(book);
        return Ok(ToResource(updated));
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteBook(int id)
    {
        var book = _bookRepository.Find(id);
        if (book == null)
        {
            return NotFound(new { error = $"Book {id} not found" });
        }

        _bookRepository.Delete(id);
        return NoContent();
    }

    private static BookResource ToResource(Book book)
    {
        return new BookResource
        {
            Id = book.Id,
            Title = book.Title,
            Year = book.Year,
            Monitored = book.Monitored,
            QualityProfileId = book.QualityProfileId,
            Added = book.Added,
            AuthorId = book.AuthorId,
            BookSeriesId = book.BookSeriesId,
            Tags = book.Tags?.ToList(),
            Metadata = new BookMetadataResource
            {
                Description = book.Metadata.Description,
                ForeignBookId = book.Metadata.ForeignBookId,
                GoodreadsId = book.Metadata.GoodreadsId,
                OpenLibraryId = book.Metadata.OpenLibraryId,
                GoogleBooksId = book.Metadata.GoogleBooksId,
                Isbn = book.Metadata.Isbn,
                Isbn13 = book.Metadata.Isbn13,
                Asin = book.Metadata.Asin,
                PageCount = book.Metadata.PageCount,
                ReleaseDate = book.Metadata.ReleaseDate,
                Publisher = book.Metadata.Publisher,
                Language = book.Metadata.Language,
                Genres = book.Metadata.Genres,
                SeriesPosition = book.Metadata.SeriesPosition
            }
        };
    }

    private static Book ToModel(BookResource resource)
    {
        return new Book
        {
            Id = resource.Id,
            Title = resource.Title,
            Year = resource.Year,
            Monitored = resource.Monitored,
            QualityProfileId = resource.QualityProfileId,
            Added = resource.Added,
            AuthorId = resource.AuthorId,
            BookSeriesId = resource.BookSeriesId,
            Tags = resource.Tags?.ToHashSet() ?? new HashSet<int>(),
            Metadata = ToMetadata(resource.Metadata)
        };
    }

    private static BookMetadata ToMetadata(BookMetadataResource? resource)
    {
        if (resource == null)
        {
            return new BookMetadata();
        }

        return new BookMetadata
        {
            Description = resource.Description,
            ForeignBookId = resource.ForeignBookId,
            GoodreadsId = resource.GoodreadsId,
            OpenLibraryId = resource.OpenLibraryId,
            GoogleBooksId = resource.GoogleBooksId,
            Isbn = resource.Isbn,
            Isbn13 = resource.Isbn13,
            Asin = resource.Asin,
            PageCount = resource.PageCount,
            ReleaseDate = resource.ReleaseDate,
            Publisher = resource.Publisher,
            Language = resource.Language,
            Genres = resource.Genres ?? new List<string>(),
            SeriesPosition = resource.SeriesPosition
        };
    }
}

public class BookResource
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public int Year { get; set; }
    public bool Monitored { get; set; }
    public int QualityProfileId { get; set; }
    public DateTime Added { get; set; }
    public int? AuthorId { get; set; }
    public int? BookSeriesId { get; set; }
    public List<int>? Tags { get; set; }
    public BookMetadataResource Metadata { get; set; } = new();
}

public class BookMetadataResource
{
    public string? Description { get; set; }
    public string? ForeignBookId { get; set; }
    public string? GoodreadsId { get; set; }
    public string? OpenLibraryId { get; set; }
    public string? GoogleBooksId { get; set; }
    public string? Isbn { get; set; }
    public string? Isbn13 { get; set; }
    public string? Asin { get; set; }
    public int? PageCount { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? Publisher { get; set; }
    public string? Language { get; set; }
    public List<string> Genres { get; set; } = new();
    public int? SeriesPosition { get; set; }
}
