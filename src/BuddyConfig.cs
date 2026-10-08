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
        // 超过这个距离才传送。用户要求放到 **5 公里**：
        // 近距离就让 AI 自己开（那才是"跟随"该有的样子），
        // 只有真的差了几公里、AI 明显跟不上时才动用传送。
        internal const float EscortTeleportBeyond = 5000.0f;

        /// <summary>
        /// 传送时落在离玩家多远的路面上（米）。用户要求落在 **2 公里以内**。
        ///
        /// 刻意**不**传到玩家身边：那样会突兀地"冒出来"。
        /// 落在 1.8 公里外的公路上，剩下这段路由 AI 开完，观感自然得多。
        /// </summary>
        internal const float EscortTeleportLandDistance = 1800.0f;

        /// <summary>传送落点的最大允许距离（用户要求的 2 公里上限）。</summary>
        internal const float EscortTeleportMaxDistance = 2000.0f;

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

        /// <summary>
        /// TASK_VEHICLE_GOTO_NAVMESH 的重发间隔（毫秒）。
        ///
        /// 这是个**点任务**，不会自己跟踪移动实体，所以必须按节奏把玩家的新坐标喂给它。
        /// 间隔要权衡：太短会不断打断路径规划，太长会跟不上移动的玩家。
        /// 1500ms 在 20 m/s 下对应约 30 米的前瞻，够用。
        /// </summary>
        internal const int NavmeshReissueMs = 1500;

        /// <summary>
        /// 传送冷却（毫秒）。**这是防止"一直在不断传送"的关键。**
        ///
        /// 传送本身会让距离瞬间归零，如果没有冷却，下一帧距离又可能因为玩家在动
        /// 而重新超过阈值，于是反复传送 —— 表现就是队友在你身边闪来闪去。
        /// 传送后强制安静一段时间，让 AI 有机会自己跟上。
        /// </summary>
        internal const int EscortTeleportCooldownMs = 20000;

        /// <summary>
        /// 距离超过阈值后还要持续这么久才真的传送（毫秒）。
        /// 避免玩家高速掠过时误触发。
        /// </summary>
        internal const int EscortTeleportDelayMs = 2000;

        /// <summary>
        /// 座位强锁的检查间隔（毫秒）。
        ///
        /// 用户要求"只有玩家下车时 agent 才出来"。光靠配置标志做不到
        /// （实测队友还是会在玩家停车时下车），所以改成**持续强制**：
        /// 玩家在车上期间，每隔这么久就把溜下车的队友立刻塞回座位。
        ///
        /// 500ms 足够快 —— 下车的动画都来不及播完就被按回去。
        /// </summary>
        internal const int SeatingLockIntervalMs = 500;

        /// <summary>
        /// 乘车战斗的敌人扫描半径（米）。
        ///
        /// 为什么需要主动扫描：escort（开车跟随）模式**跳过了整个状态机**，
        /// 而威胁缓存 _threat 正是在状态机的战斗分支里被赋值的 ——
        /// 于是开车跟随期间它永远是 null。
        /// 日志证据：vehicle combat / engaging 出现 **0 次**，战斗从未触发过。
        /// </summary>
        internal const float VehicleCombatScanRadius = 70.0f;

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
