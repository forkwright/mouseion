// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.Authors;

namespace Mouseion.Core.Books;

public interface IAddBookService
{
    Book AddBook(Book book);
    List<Book> AddBooks(List<Book> books);
}

public class AddBookService : IAddBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ILogger<AddBookService> _logger;

    public AddBookService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        ILogger<AddBookService> logger)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _logger = logger;
    }

    public Book AddBook(Book book)
    {
        ValidateBook(book);

        // Verify author exists
        if (book.AuthorId.HasValue)
        {
            var author = _authorRepository.Find(book.AuthorId.Value);
            if (author == null)
            {
                throw new ArgumentException($"Author with ID {book.AuthorId.Value} not found", nameof(book));
            }
        }

        // Check for existing book
        if (book.AuthorId.HasValue)
        {
            var existing = _bookRepository.FindByTitle(book.Title, book.Year);
            if (existing != null && existing.AuthorId == book.AuthorId)
            {
                _logger.LogInformation("Book already exists: {BookTitle} ({Year})", existing.Title, existing.Year);
                return existing;
            }
        }

        // Set defaults
        book.Added = DateTime.UtcNow;
        book.Monitored = true;

        var added = _bookRepository.Insert(book);
        _logger.LogInformation("Added book: {BookTitle} ({Year}) - Author ID: {AuthorId}",
            added.Title, added.Year, added.AuthorId);

        return added;
    }

    public List<Book> AddBooks(List<Book> books)
    {
        var addedBooks = new List<Book>();

        foreach (var book in books)
        {
            try
            {
                var added = AddBook(book);
                addedBooks.Add(added);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book: {BookTitle}", book.Title);
            }
        }

        return addedBooks;
    }

    private static void ValidateBook(Book book)
    {
        if (string.IsNullOrWhiteSpace(book.Title))
        {
            throw new ArgumentException("Book title is required", nameof(book));
        }

        if (book.QualityProfileId <= 0)
        {
            throw new ArgumentException("Quality profile ID must be set", nameof(book));
        }
    }
}
