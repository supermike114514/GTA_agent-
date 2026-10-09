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

        // ---- 近距离"跟不上"兜底（用户要求：以玩家位置移动）----
        //
        // 5 公里那一档太远，日常"跟不上"通常发生在几百米。
        // 所以再加一档更实用的：车卡住不动、且已经落后 200 米以上时，
        // 直接把它挪到玩家后方约 60 米的路面上。
        internal const float EscortCloseTeleportBeyond = 200.0f;
        internal const float EscortCloseTeleportBehind = 60.0f;
        internal const int EscortCloseTeleportStalledMs = 6000;
        internal const int EscortCloseTeleportCooldownMs = 15000;

        // ==================================================================
        // Pure Pursuit 驾驶（参考 GTA 内部 CCarAI / CAutoPilot 的做法）
        // ==================================================================
        //
        // GTA 内部的 CCarAI + CAutoPilot 是这样开车的：
        //   1. JoinCarWithRoadSystem 把目标位置映射到路网
        //   2. 用 A* / Dijkstra 在路网上算出一条路径
        //   3. 用 **Pure Pursuit** 沿路径行驶 —— 在车前方取一个"前视点"，
        //      算出追踪它所需的转向角，从而平滑地贴着路网开
        //
        // Pure Pursuit 的转向律（经典形式）：
        //
        //     曲率  k     = 2 * sin(alpha) / Ld
        //     前轮角 steer = atan(轴距 * k)
        //
        //   alpha = 车头朝向 与 车->前视点方向 的夹角
        //   Ld    = 前视距离（**随车速增大**，这是"不画龙"的关键）
        //
        // Ld 随速度变化这点特别重要：固定的前视距离在低速时会左右摆、
        // 高速时会切弯内。这也正是脚本层手写"直接朝目标转"最常犯的错。

        /// <summary>前视距离 = 基准 + 速度系数 * 当前速度（米）。</summary>
        internal const float PurePursuitLookAheadBase = 6.0f;
        internal const float PurePursuitLookAheadPerSpeed = 1.1f;
        internal const float PurePursuitLookAheadMin = 8.0f;
        internal const float PurePursuitLookAheadMax = 40.0f;

        /// <summary>车辆轴距（米）。Pure Pursuit 对轴距不敏感，取常见轿车值即可。</summary>
        internal const float PurePursuitWheelBase = 2.8f;

        /// <summary>最大前轮转角（弧度），用于把转向角归一化成 SteeringScale。</summary>
        internal const float PurePursuitMaxSteerAngle = 0.6f;

        /// <summary>距离玩家多少米以内就停车（米）。</summary>
        internal const float PurePursuitStopRange = 14.0f;

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
        /// **这是点任务，只下发一次它执行完就停了** —— 必须在 tick 循环里
        /// 持续重发并传入玩家的最新坐标，否则 AI 只会动一下就静止。
        ///
        /// 用户建议约每 20 个游戏周期一调：60fps 下 20 tick ≈ 333ms。
        /// 之前我设成 1500ms，后来又加了"意图不变就不重发"的优化 ——
        /// 那个优化对持续型任务（ESCORT/HELI_CHASE）正确，对点任务是致命的，
        /// 实测表现就是车队全部停在原地。
        /// </summary>
        internal const int NavmeshReissueMs = 333;

        /// <summary>
        /// 跟随时使用的驾驶风格。
        ///
        /// 用户建议先用一个**已知完整、配置成套**的值来看行为是否正常，
        /// 而不是零散地拼标志位：
        ///   786603 = DrivingModeStopForVehicles，GTA 自己常用的完整风格
        ///          （= FollowTraffic|YieldToPeds|AvoidVehicles|AvoidEmptyVehicles|
        ///             AvoidPeds|AvoidObjects|StopAtTrafficLights|...）
        ///
        /// 先确认基础行为正常，再考虑叠加 ChangeLanesAroundObstructions、
        /// UseStringPullingAtJunctions 这类进阶标志。
        /// </summary>
        internal const int EscortDrivingStyle = 786603;

        /// <summary>
        /// 进阶驾驶风格（含变道、逆行超车、路口抄近路）。行为确认正常后可以切到这个。
        /// </summary>
        internal const int EscortDrivingStyleAggressive = 786603
            | 0x80000      // ChangeLanesAroundObstructions (524288)
            | 0x2000000    // UseStringPullingAtJunctions (33554432)
            | 0x200        // AllowGoingWrongWay (512)
            | 0x100;       // GoOffRoadWhenAvoiding (256)

        /// <summary>
        /// 防卡死：车速持续为零超过这么久，就清任务并短暂倒车脱离（毫秒）。
        /// </summary>
        internal const int AntiStallTriggerMs = 2500;

        /// <summary>倒车脱离的持续时间（毫秒）。</summary>
        internal const int AntiStallReverseMs = 900;

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

        /// <summary>
        /// 需要把**玩家的关系**镜像到队友组上的关系组名。
        ///
        /// 为什么必须做这件事：
        ///   队友本来只有两种关系定义 —— 队友组之间 BUDDY、队友组对玩家组 RESPECT。
        ///   对警察、帮派、雇佣兵等**完全没有定义**，而没定义在 GTA 里就是**中立**。
        ///   于是玩家被警察围攻时，队友看到的警察是"中立"的，**根本不会还击** ——
        ///   换武器、开战斗属性、下攻击任务都救不了，因为关系表里就没写。
        ///
        /// 做法是把玩家对这些组的当前关系**原样抄到队友组上**：
        /// 玩家和某个帮派打起来了，游戏会把该帮派设为仇恨玩家，
        /// 镜像之后队友也就仇恨该帮派，于是会一起开火。
        /// </summary>
        internal static readonly string[] MirroredRelationGroups = new string[]
        {
            "AMBIENT_GANG_LOST",
            "AMBIENT_GANG_MEXICAN",
            "AMBIENT_GANG_FAMILY",
            "AMBIENT_GANG_BALLAS",
            "AMBIENT_GANG_SALVA",
            "AMBIENT_GANG_WEICHENG",
            "AMBIENT_GANG_HILLBILLY",
            "AMBIENT_GANG_VAGOS",
            "GANG_1", "GANG_2", "GANG_3", "GANG_4", "GANG_5",
            "GANG_6", "GANG_7", "GANG_8", "GANG_9", "GANG_10",
            "MERRYWEATHER",
            "SECURITY",
            "ARMY",
            "PRIVATE_SECURITY",
            "HATES_PLAYER",
            "FIREMAN",
            "MEDIC",
        };

        /// <summary>
        /// 警察关系组的名字。单独处理，因为警察的敌意由**通缉等级**驱动，
        /// 而不是由 PLAYER 关系组的静态关系决定。
        /// </summary>
        internal const string CopRelationshipGroup = "COP";

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
