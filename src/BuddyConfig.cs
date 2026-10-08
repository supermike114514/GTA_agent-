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

        /// <summary>DefaultModel 的枚举形式，避免每次生成都做字符串到哈希的转换。</summary>
        internal const PedHash DefaultPedHash = PedHash.Security01SMM;
        internal const float SpawnDistance = 3.0f;

        // ---- 队友自驾跟随的硬参数 ----
        //
        // 超过这个距离就直接把"队友 + 他的车"一起传送到玩家附近的路面上。
        //
        // 这是最可靠的兜底：引擎的驾驶 AI 对履带车/特种载具经常拒绝执行，
        // 而直接控制又容易让几辆车互相顶、堆在一起。与其在几十米外挣扎，
        // 不如超过阈值就直接挪过来。
        //
        // 给得较大，正常跟得上时不会触发；实测"跟不上"通常发生在 60 米以上。
        internal const float EscortTeleportBeyond = 110.0f;

        /// <summary>传送时落在玩家后方的距离（米）。</summary>
        internal const float EscortTeleportBehind = 16.0f;

        /// <summary>
        /// 小队成员之间的分离距离（米），交给引擎的编队算法使用。
        ///
        /// 这是"借 GTA 自带算法"的一部分：与其我自己算间距，不如把引擎的
        /// SET_GROUP_SEPARATION_RANGE 调大，让引擎负责把成员拉开。
        /// </summary>
        internal const float GroupSeparationRange = 25.0f;

        /// <summary>
        /// 直接驾驶时每个队友的编队位距玩家的距离（米），按编号递增。
        ///
        /// 这是解决"车辆堆积"的关键：每辆车有**自己的目标位**，而不是都朝玩家开。
        /// 都朝同一点开必然挤在一起；各有各的位置才会排成队列。
        /// </summary>
        internal const float ConvoySlotBase = 12.0f;
        internal const float ConvoySlotStep = 7.0f;

        /// <summary>进入自己的编队位多少米内就停车（米）。</summary>
        internal const float ConvoySlotArrive = 7.0f;

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
