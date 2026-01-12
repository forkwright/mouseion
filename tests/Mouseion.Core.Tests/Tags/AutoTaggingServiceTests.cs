// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Moq;
using Mouseion.Core.MediaItems;
using Mouseion.Core.MediaTypes;
using Mouseion.Core.Movies;
using Mouseion.Core.Tags;
using Mouseion.Core.Tags.AutoTagging;

namespace Mouseion.Core.Tests.Tags;

public class AutoTaggingServiceTests
{
    private readonly Mock<IAutoTaggingRuleRepository> _ruleRepository;
    private readonly Mock<ITagRepository> _tagRepository;
    private readonly Mock<IMediaItemRepository> _mediaItemRepository;
    private readonly Mock<ILogger<AutoTaggingService>> _logger;
    private readonly AutoTaggingService _service;

    public AutoTaggingServiceTests()
    {
        _ruleRepository = new Mock<IAutoTaggingRuleRepository>();
        _tagRepository = new Mock<ITagRepository>();
        _mediaItemRepository = new Mock<IMediaItemRepository>();
        _logger = new Mock<ILogger<AutoTaggingService>>();
        _service = new AutoTaggingService(
            _ruleRepository.Object,
            _tagRepository.Object,
            _mediaItemRepository.Object,
            _logger.Object);
    }

    [Fact]
    public async Task EvaluateRulesAsync_NoRules_ReturnsEmpty()
    {
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AutoTaggingRule>());

        var movie = CreateMovie("Test Movie", new List<string> { "Action" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Empty(result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_GenreContainsRule_MatchesGenre()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action", "Thriller" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Single(result);
        Assert.Contains(10, result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_GenreContainsRule_DoesNotMatchWhenNoGenre()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("The Notebook", new List<string> { "Romance", "Drama" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Empty(result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_MediaTypeFilter_AppliesOnlyToMatchingType()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Movies Only",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10,
                MediaTypeFilter = MediaType.Movie
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Single(result);
        Assert.Contains(10, result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_MultipleRules_ReturnsAllMatchingTags()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            },
            new AutoTaggingRule
            {
                Id = 2,
                Name = "Thriller Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Thriller",
                TagId = 20
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action", "Thriller" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Equal(2, result.Count);
        Assert.Contains(10, result);
        Assert.Contains(20, result);
    }

    [Fact]
    public async Task ApplyAutoTagsAsync_AddsMatchingTagsToItem()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action" });
        Assert.Empty(movie.Tags);

        await _service.ApplyAutoTagsAsync(movie);

        Assert.Single(movie.Tags);
        Assert.Contains(10, movie.Tags);
    }

    [Fact]
    public async Task ApplyAutoTagsAsync_DoesNotDuplicateTags()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action" });
        movie.Tags.Add(10);

        await _service.ApplyAutoTagsAsync(movie);

        Assert.Single(movie.Tags);
    }

    [Fact]
    public void EvaluateRules_Sync_MatchesGenre()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRules())
            .Returns(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action" });
        var result = _service.EvaluateRules(movie);

        Assert.Single(result);
        Assert.Contains(10, result);
    }

    [Fact]
    public void ApplyAutoTags_Sync_AddsMatchingTags()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRules())
            .Returns(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "Action" });
        _service.ApplyAutoTags(movie);

        Assert.Single(movie.Tags);
        Assert.Contains(10, movie.Tags);
    }

    [Fact]
    public async Task EvaluateRulesAsync_CaseInsensitiveGenreMatch()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Action Genre",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "action",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Die Hard", new List<string> { "ACTION" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Single(result);
        Assert.Contains(10, result);
    }

    [Fact]
    public async Task EvaluateRulesAsync_PartialGenreMatch()
    {
        var rules = new List<AutoTaggingRule>
        {
            new AutoTaggingRule
            {
                Id = 1,
                Name = "Science Fiction",
                Enabled = true,
                ConditionType = AutoTaggingConditionType.GenreContains,
                ConditionValue = "Sci",
                TagId = 10
            }
        };
        _ruleRepository.Setup(x => x.GetEnabledRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        var movie = CreateMovie("Star Wars", new List<string> { "Science Fiction", "Adventure" });
        var result = await _service.EvaluateRulesAsync(movie);

        Assert.Single(result);
        Assert.Contains(10, result);
    }

    private static Movie CreateMovie(string title, List<string> genres)
    {
        return new Movie
        {
            Id = 1,
            Title = title,
            Year = 2024,
            Genres = genres,
            QualityProfileId = 1
        };
    }
}
