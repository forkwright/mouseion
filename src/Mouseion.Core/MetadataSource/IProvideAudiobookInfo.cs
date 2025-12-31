// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Core.Audiobooks;

namespace Mouseion.Core.MetadataSource;

public interface IProvideAudiobookInfo
{
    Audiobook? GetByAsin(string asin);
    Audiobook? GetById(int id);
    List<Audiobook> SearchByTitle(string title);
    List<Audiobook> SearchByAuthor(string author);
    List<Audiobook> SearchByNarrator(string narrator);
    List<Audiobook> GetTrending();
    List<Audiobook> GetPopular();
}
