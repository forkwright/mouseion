// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Api.Tests;

public abstract class ControllerTestBase : IDisposable
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected ControllerTestBase()
    {
        Factory = new TestWebApplicationFactory();
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
    }

    public void Dispose()
    {
        Client?.Dispose();
        Factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
