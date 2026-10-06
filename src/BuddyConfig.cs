using System;
using System.Collections.Generic;
using GTA;

namespace GTA5Buddy
{
    /// <summary>All tunables in one place so behaviour can be retuned without touching logic.</summary>
    internal static class BuddyConfig
    {
        // ---- identity ----
        internal const string BuddyName = "队友";
        internal const string RelationshipGroupName = "BUDDY_MOD_GROUP";

        // ---- model used when spawning ----
        internal const string DefaultModel = "s_m_m_security_01";
        internal const float SpawnDistance = 3.0f;

        // ---- follow (L2/F1) ----
        internal const float FollowOffsetSide = 1.2f;   // + = to the buddy's right of the player
        internal const float FollowOffsetBack = -1.1f;  // - = behind the player
        internal const float FollowRadius = 2.0f;       // considered "in position" inside this
        internal const float FollowSpeed = 1.0f;        // MBR; the engine scales to the player's actual speed
        internal const float TeleportIfFartherThan = 45f;
        internal const float ReissueFollowIfFartherThan = 3.5f;

        // ---- combat (L2/F2) ----
        internal const float ThreatScanRadius = 55f;
        internal const float CoverScanRadius = 35f;
        internal const int ReloadCheckMs = 900;
        internal const int ReloadIfAmmoBelow = 1;

        // ---- driving (L2/F3) ----
        internal const float CarHijackRadius = 60f;
        internal const float ExitVehicleWhenPlayerOnFoot = 22f;

        // ---- survival (L2/F5) ----
        internal const int HealthLowPercent = 45;
        internal const int HealthPanicPercent = 25;
        internal const int MedkitCooldownMs = 12000;
        internal const int MedkitHealAmount = 35;
        internal const int ArmorLowValue = 20;
        internal const int ArmorGrantAmount = 100;
        internal const int DownedTimeoutMs = 30000;     // bleed out if not revived
        internal const int ReviveHealth = 120;
        internal const int ReviveInvulnMs = 3000;
        internal const int DownedHealthFloor = 5;       // keep above the death threshold

        // ---- HUD ----
        internal const bool DrawHud = true;
        internal const float HudX = 0.016f;
        internal const float HudY = 0.62f;
        internal const float HudScale = 0.32f;

        // ---- misc ----
        internal const int TickIntervalMs = 0;          // every frame; the brain throttles its own scans

        // ---- mode gate ----
        // Story mode only. Leave this false. Setting it true loads the companion in any
        // session, which is exactly what the Online guard exists to prevent - it is here
        // solely so a false positive can be diagnosed without recompiling the check.
        internal const bool ForceAllowAnyMode = false;
    }
}
