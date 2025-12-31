// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Mouseion.Common.Http;

namespace Mouseion.Common.Cloud
{
    public interface IMouseionCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory TMDB { get; }
        IHttpRequestBuilderFactory MouseionMetadata { get; }
    }

    public class MouseionCloudRequestBuilder : IMouseionCloudRequestBuilder
    {
        // Default TMDB read-only API token (can be overridden via TMDB_API_TOKEN env var)
        // This is a public read-only token for TMDB API, not a secret credential
#pragma warning disable S6418 // Hard-coded secrets
        private const string DefaultTmdbToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIxYTczNzMzMDE5NjFkMDNmOTdmODUzYTg3NmRkMTIxMiIsInN1YiI6IjU4NjRmNTkyYzNhMzY4MGFiNjAxNzUzNCIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.gh1BwogCCKOda6xj9FRMgAAj_RYKMMPC3oNlcBtlmwk";
#pragma warning restore S6418

        public MouseionCloudRequestBuilder()
        {
            Services = new HttpRequestBuilder("https://radarr.servarr.com/v1/")
                .CreateFactory();

            var tmdbToken = Environment.GetEnvironmentVariable("TMDB_API_TOKEN") ?? DefaultTmdbToken;
            TMDB = new HttpRequestBuilder("https://api.themoviedb.org/{api}/{route}/{id}{secondaryRoute}")
                .SetHeader("Authorization", $"Bearer {tmdbToken}")
                .CreateFactory();

            MouseionMetadata = new HttpRequestBuilder("https://api.radarr.video/v1/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; private set; }
        public IHttpRequestBuilderFactory TMDB { get; private set; }
        public IHttpRequestBuilderFactory MouseionMetadata { get; private set; }
    }
}
