// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mouseion.Core.Authors;

namespace Mouseion.Api.Authors;

[ApiController]
[Route("api/v3/authors")]
[Authorize]
public class AuthorController : ControllerBase
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IAddAuthorService _addAuthorService;

    public AuthorController(
        IAuthorRepository authorRepository,
        IAddAuthorService addAuthorService)
    {
        _authorRepository = authorRepository;
        _addAuthorService = addAuthorService;
    }

    [HttpGet]
    public ActionResult<List<AuthorResource>> GetAuthors()
    {
        var authors = _authorRepository.All().ToList();
        return Ok(authors.Select(ToResource).ToList());
    }

    [HttpGet("{id:int}")]
    public ActionResult<AuthorResource> GetAuthor(int id)
    {
        var author = _authorRepository.Find(id);
        if (author == null)
        {
            return NotFound(new { error = $"Author {id} not found" });
        }

        return Ok(ToResource(author));
    }

    [HttpGet("foreignId/{foreignId}")]
    public ActionResult<AuthorResource> GetByForeignId(string foreignId)
    {
        var author = _authorRepository.FindByForeignId(foreignId);
        if (author == null)
        {
            return NotFound(new { error = $"Author with foreign ID {foreignId} not found" });
        }

        return Ok(ToResource(author));
    }

    [HttpPost]
    public ActionResult<AuthorResource> AddAuthor([FromBody] AuthorResource resource)
    {
        try
        {
            var author = ToModel(resource);
            var added = _addAuthorService.AddAuthor(author);
            return CreatedAtAction(nameof(GetAuthor), new { id = added.Id }, ToResource(added));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public ActionResult<AuthorResource> UpdateAuthor(int id, [FromBody] AuthorResource resource)
    {
        var author = _authorRepository.Find(id);
        if (author == null)
        {
            return NotFound(new { error = $"Author {id} not found" });
        }

        author.Name = resource.Name;
        author.SortName = resource.SortName;
        author.Description = resource.Description;
        author.ForeignAuthorId = resource.ForeignAuthorId;
        author.Monitored = resource.Monitored;
        author.Path = resource.Path;
        author.RootFolderPath = resource.RootFolderPath;
        author.QualityProfileId = resource.QualityProfileId;
        author.Tags = resource.Tags?.ToHashSet() ?? new HashSet<int>();

        var updated = _authorRepository.Update(author);
        return Ok(ToResource(updated));
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteAuthor(int id)
    {
        var author = _authorRepository.Find(id);
        if (author == null)
        {
            return NotFound(new { error = $"Author {id} not found" });
        }

        _authorRepository.Delete(id);
        return NoContent();
    }

    private static AuthorResource ToResource(Author author)
    {
        return new AuthorResource
        {
            Id = author.Id,
            Name = author.Name,
            SortName = author.SortName,
            Description = author.Description,
            ForeignAuthorId = author.ForeignAuthorId,
            Monitored = author.Monitored,
            Path = author.Path,
            RootFolderPath = author.RootFolderPath,
            QualityProfileId = author.QualityProfileId,
            Added = author.Added,
            Tags = author.Tags?.ToList()
        };
    }

    private static Author ToModel(AuthorResource resource)
    {
        return new Author
        {
            Id = resource.Id,
            Name = resource.Name,
            SortName = resource.SortName,
            Description = resource.Description,
            ForeignAuthorId = resource.ForeignAuthorId,
            Monitored = resource.Monitored,
            Path = resource.Path,
            RootFolderPath = resource.RootFolderPath,
            QualityProfileId = resource.QualityProfileId,
            Added = resource.Added,
            Tags = resource.Tags?.ToHashSet() ?? new HashSet<int>()
        };
    }
}

public class AuthorResource
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? SortName { get; set; }
    public string? Description { get; set; }
    public string? ForeignAuthorId { get; set; }
    public bool Monitored { get; set; }
    public string? Path { get; set; }
    public string? RootFolderPath { get; set; }
    public int QualityProfileId { get; set; }
    public DateTime Added { get; set; }
    public List<int>? Tags { get; set; }
}
