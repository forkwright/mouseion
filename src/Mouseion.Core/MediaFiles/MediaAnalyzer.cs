// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using TagLib;

namespace Mouseion.Core.MediaFiles;

public interface IMediaAnalyzer
{
    List<ChapterInfo> GetChapters(string filePath);
    MediaFileInfo GetMediaInfo(string filePath);
}

public class MediaAnalyzer : IMediaAnalyzer
{
    public List<ChapterInfo> GetChapters(string filePath)
    {
        var chapters = new List<ChapterInfo>();

        try
        {
            using var file = TagLib.File.Create(filePath);

            // Try MP3 ID3v2 CTOC/CHAP frames (fully supported in TagLibSharp 2.3.0)
            if (file is TagLib.Mpeg.AudioFile mp3File)
            {
                chapters = ParseId3v2Chapters(mp3File);
                if (chapters.Count > 0)
                {
                    return chapters;
                }
            }

            // TODO: M4B/MP4 chapter atoms (chpl atom) - TagLibSharp doesn't expose this
            // Would require custom binary parsing of moov.udta.chpl atom
            // Deferred to future enhancement or external tool integration

            // Fallback: single chapter spanning the file
            if (file.Properties.Duration.TotalMilliseconds > 0)
            {
                chapters.Add(new ChapterInfo
                {
                    Index = 0,
                    Title = "Full Book",
                    StartTimeMs = 0,
                    EndTimeMs = (long)file.Properties.Duration.TotalMilliseconds
                });
            }
        }
        catch
        {
            // Graceful degradation: return empty list if file can't be parsed
        }

        return chapters;
    }

    private List<ChapterInfo> ParseId3v2Chapters(TagLib.Mpeg.AudioFile mp3File)
    {
        var chapters = new List<ChapterInfo>();

        try
        {
            var id3v2 = mp3File.GetTag(TagLib.TagTypes.Id3v2) as TagLib.Id3v2.Tag;
            if (id3v2 == null)
            {
                return chapters;
            }

            // Get all chapter frames directly (no CTOC required)
            var chapterFrames = id3v2.GetFrames<TagLib.Id3v2.ChapterFrame>().ToList();
            if (chapterFrames.Count == 0)
            {
                return chapters;
            }

            // Sort by start time
            chapterFrames = chapterFrames.OrderBy(c => c.StartMilliseconds).ToList();

            for (int i = 0; i < chapterFrames.Count; i++)
            {
                var chapFrame = chapterFrames[i];

                // Use chapter ID as title, fallback to numbered chapter
                // TODO: Extract title from TIT2 subframe when present
                var title = !string.IsNullOrEmpty(chapFrame.Id)
                    ? chapFrame.Id
                    : $"Chapter {i + 1}";

                chapters.Add(new ChapterInfo
                {
                    Index = i,
                    Title = title,
                    StartTimeMs = (long)chapFrame.StartMilliseconds,
                    EndTimeMs = (long)chapFrame.EndMilliseconds
                });
            }
        }
        catch
        {
            // Graceful degradation
        }

        return chapters;
    }

    public MediaFileInfo GetMediaInfo(string filePath)
    {
        using var file = TagLib.File.Create(filePath);

        return new MediaFileInfo
        {
            DurationSeconds = (int)file.Properties.Duration.TotalSeconds,
            Bitrate = file.Properties.AudioBitrate,
            SampleRate = file.Properties.AudioSampleRate,
            Channels = file.Properties.AudioChannels,
            Format = file.MimeType
        };
    }
}

public class MediaFileInfo
{
    public int DurationSeconds { get; set; }
    public int Bitrate { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public string Format { get; set; } = string.Empty;
}
