// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Mouseion.Api.Common;
using Mouseion.Api.MediaItems;

namespace Mouseion.Api.Tests.MediaItems;

public class MediaItemsControllerTests : ControllerTestBase
{
    public MediaItemsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessfully()
    {
        var response = await Client.GetAsync("/api/v3/media");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsPagedResult()
    {
        var response = await Client.GetAsync("/api/v3/media?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAll_WithInvalidMediaType_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/v3/media?mediaType=InvalidType");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithValidMediaType_ReturnsFilteredResults()
    {
        var response = await Client.GetAsync("/api/v3/media?mediaType=Book");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        var response = await Client.GetAsync("/api/v3/media/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetModifiedSince_WithoutParameter_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/v3/media/sync");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetModifiedSince_WithValidDate_ReturnsSuccessfully()
    {
        var date = DateTime.UtcNow.AddDays(-1).ToString("o");
        var response = await Client.GetAsync($"/api/v3/media/sync?modifiedSince={date}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<MediaItemResource>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetModifiedSince_WithInvalidMediaType_ReturnsBadRequest()
    {
        var date = DateTime.UtcNow.AddDays(-1).ToString("o");
        var response = await Client.GetAsync($"/api/v3/media/sync?modifiedSince={date}&mediaType=Invalid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync("/api/v3/media/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_EnforcesMaxPageSize()
    {
        var response = await Client.GetAsync("/api/v3/media?pageSize=500");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
        Assert.Equal(250, result.PageSize);
    }

    [Fact]
    public async Task GetAll_EnforcesMinPage()
    {
        var response = await Client.GetAsync("/api/v3/media?page=0");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetAll_EnforcesMinPageSize()
    {
        var response = await Client.GetAsync("/api/v3/media?pageSize=0");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task GetAll_WithMovieMediaType_ReturnsFilteredResults()
    {
        var response = await Client.GetAsync("/api/v3/media?mediaType=Movie");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAll_WithAudiobookMediaType_ReturnsFilteredResults()
    {
        var response = await Client.GetAsync("/api/v3/media?mediaType=Audiobook");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MediaItemResource>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetModifiedSince_WithMediaTypeFilter_ReturnsFilteredResults()
    {
        var date = DateTime.UtcNow.AddDays(-1).ToString("o");
        var response = await Client.GetAsync($"/api/v3/media/sync?modifiedSince={date}&mediaType=Book");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<MediaItemResource>>();
        Assert.NotNull(result);
    }
}
