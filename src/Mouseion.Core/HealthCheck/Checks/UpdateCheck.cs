// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

namespace Mouseion.Core.HealthCheck.Checks;

public class UpdateCheck : HealthCheckBase
{
    public override HealthCheck Check()
    {
        // Placeholder - update system not yet implemented
        return new HealthCheck(GetType());
    }

    public override bool CheckOnStartup => false;
}
