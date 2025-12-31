// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Mouseion.Api.Streaming
{
    [ApiController]
    [Route("api/v3/stream")]
    public class StreamingController : ControllerBase
    {
        private readonly ILogger<StreamingController> _logger;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        public StreamingController(ILogger<StreamingController> _logger)
        {
            this._logger = _logger;
            _contentTypeProvider = new FileExtensionContentTypeProvider();
        }

        [HttpGet("{mediaId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status416RangeNotSatisfiable)]
        public IActionResult StreamMedia(int mediaId, [FromQuery] string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("File path is required");
            }

            if (!System.IO.File.Exists(path))
            {
                _logger.LogWarning("File not found: {Path}", path);
                return NotFound();
            }

            var fileInfo = new FileInfo(path);
            var fileLength = fileInfo.Length;

            if (!_contentTypeProvider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var rangeHeader = Request.GetTypedHeaders().Range;

            if (rangeHeader == null || rangeHeader.Ranges.Count == 0)
            {
                _logger.LogDebug("Serving full file: {Path} ({Size} bytes)", path, fileLength);
                return PhysicalFile(path, contentType, enableRangeProcessing: true);
            }

            var range = rangeHeader.Ranges.FirstOrDefault();
            if (range == null)
            {
                return PhysicalFile(path, contentType, enableRangeProcessing: true);
            }

            long start = range.From ?? 0;
            long end = range.To ?? fileLength - 1;

            if (start < 0 || start >= fileLength || end < start || end >= fileLength)
            {
                _logger.LogWarning("Invalid range request: {Start}-{End}/{Size}", start, end, fileLength);
                return StatusCode((int)HttpStatusCode.RequestedRangeNotSatisfiable,
                    new { Error = "Invalid range" });
            }

            long contentLength = end - start + 1;

            _logger.LogDebug("Serving partial content: {Path} (range: {Start}-{End}/{Size}, {ContentLength} bytes)",
                path, start, end, fileLength, contentLength);

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(start, SeekOrigin.Begin);

            Response.StatusCode = (int)HttpStatusCode.PartialContent;
            Response.Headers.ContentLength = contentLength;
            Response.Headers.ContentType = contentType;
            Response.Headers[HeaderNames.ContentRange] = $"bytes {start}-{end}/{fileLength}";
            Response.Headers[HeaderNames.AcceptRanges] = "bytes";

            return File(stream, contentType, enableRangeProcessing: false);
        }
    }
}
