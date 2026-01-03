// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mouseion.Core.Music;

namespace Mouseion.Api.Tracks;

[ApiController]
[Route("api/v3/tracks")]
[Authorize]
public class TracksController : ControllerBase
{
    private readonly IAudioAnalysisService _audioAnalysisService;

    public TracksController(IAudioAnalysisService audioAnalysisService)
    {
        _audioAnalysisService = audioAnalysisService;
    }

    [HttpGet("{id:int}/audio-analysis")]
    public async Task<ActionResult<AudioAnalysisResource>> GetAudioAnalysis(int id, CancellationToken ct = default)
    {
        var analysis = await _audioAnalysisService.GetAnalysisAsync(id, ct).ConfigureAwait(false);
        if (analysis == null)
        {
            return NotFound(new { error = $"Track {id} not found or has no associated music file" });
        }

        return Ok(ToResource(analysis));
    }

    private static AudioAnalysisResource ToResource(AudioAnalysis analysis)
    {
        return new AudioAnalysisResource
        {
            Format = analysis.Format,
            SampleRate = analysis.SampleRate,
            BitDepth = analysis.BitDepth,
            Channels = analysis.Channels,
            DynamicRange = analysis.DynamicRange,
            ReplayGain = analysis.ReplayGain != null ? new ReplayGainResource
            {
                TrackGain = analysis.ReplayGain.TrackGain,
                TrackPeak = analysis.ReplayGain.TrackPeak,
                AlbumGain = analysis.ReplayGain.AlbumGain,
                AlbumPeak = analysis.ReplayGain.AlbumPeak
            } : null,
            Lossless = analysis.Lossless,
            Transcoded = analysis.Transcoded
        };
    }
}

public class AudioAnalysisResource
{
    public string Format { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public int BitDepth { get; set; }
    public int Channels { get; set; }
    public int? DynamicRange { get; set; }
    public ReplayGainResource? ReplayGain { get; set; }
    public bool Lossless { get; set; }
    public bool Transcoded { get; set; }
}

public class ReplayGainResource
{
    public double? TrackGain { get; set; }
    public double? TrackPeak { get; set; }
    public double? AlbumGain { get; set; }
    public double? AlbumPeak { get; set; }
}
