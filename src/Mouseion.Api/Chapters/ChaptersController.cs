// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using Mouseion.Core.Datastore;
using Mouseion.Core.MediaFiles;

namespace Mouseion.Api.Chapters;

[ApiController]
[Route("api/v3/chapters")]
public class ChaptersController : ControllerBase
{
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IMediaAnalyzer _mediaAnalyzer;

    public ChaptersController(
        IMediaFileRepository mediaFileRepository,
        IMediaAnalyzer mediaAnalyzer)
    {
        _mediaFileRepository = mediaFileRepository;
        _mediaAnalyzer = mediaAnalyzer;
    }

    [HttpGet("{mediaFileId:int}")]
    public ActionResult<List<ChapterInfo>> GetChapters(int mediaFileId)
    {
        var mediaFile = _mediaFileRepository.Find(mediaFileId);
        if (mediaFile == null)
        {
            return NotFound(new { error = $"MediaFile {mediaFileId} not found" });
        }

        if (!System.IO.File.Exists(mediaFile.Path))
        {
            return NotFound(new { error = $"File not found: {mediaFile.Path}" });
        }

        var chapters = _mediaAnalyzer.GetChapters(mediaFile.Path);
        return Ok(chapters);
    }
}
