using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>
    /// 队友小队：管理一个到多个 Companion 实例。
    ///
    /// 为什么需要这一层：
    ///   原来的 Companion 是单例，只能有一个队友。要支持"自定义生成数量"就必须让
    ///   Companion 多实例化，而多实例会带出两个共享资源的冲突：
    ///
    ///     1. 关系组：每个 Companion 只能拥有一个 RelationshipGroup，用同一个名字创建两次
    ///        会互相干扰。所以这里给每个队友分配唯一的名字（<base>_1、<base>_2 …）。
    ///
    ///     2. HUD：BuddyHud 是 Companion 的字段，N 个队友就会有 N 个 HUD 叠在一起。
    ///        所以只有 0 号队友绘制 HUD，其余只受状态机驱动。
    ///
    /// 指令策略：面板上的"跟随/待命/上车/攻击"等命令**作用于全部队友**——
    ///   为一支小队逐个下令的操作成本远大于收益。
    ///
    /// 数量：不设上限。数量很大时会明显掉帧，这一点由使用者自行决定，
    ///   面板会显示当前数量并在超过 15 个时给出提示。
    /// </summary>
    internal sealed class AgentSquad
    {
        private readonly List<Companion> _agents = new List<Companion>();
        private int _spawnCounter;

        /// <summary>当前在场的队友数量。</summary>
        internal int Count { get { return _agents.Count; } }

        /// <summary>
        /// 0 号队友。多数"单个队友"的操作（换皮肤、指定召唤、开车跟随）都以它为目标，
        /// 因为它通常就是用户最早召唤、最常交互的那个。
        /// </summary>
        internal Companion Primary
        {
            get { return _agents.Count > 0 ? _agents[0] : null; }
        }

        /// <summary>所有在场的队友，供遍历下达指令。</summary>
        internal IEnumerable<Companion> All { get { return _agents; } }

        /// <summary>
        /// 通知助手。AgentSquad 不是 Script 派生类，用不到继承来的 Notify，
        /// 所以直接走底层的 ticker 接口。
        /// </summary>
        private static void Notify(string message)
        {
            try { GTA.UI.Notification.PostTicker(message, false, true); }
            catch (Exception) { }
        }
        /// <summary>读一份快照，避免遍历时列表被改动。</summary>
        internal Companion[] Snapshot() { return _agents.ToArray(); }

        /// <summary>
        /// 为队伍里的队友分配车辆座位。
        ///
        /// 规则（按用户要求）：
        ///   * 一辆车要尽量坐满 —— 按座位索引遍历，找空位就坐，而不是一人一辆车；
        ///   * 坐满之后才生成下一辆，继续往里塞；
        ///   * 0 号坐驾驶位，这样车会真的开起来而不是停着。
        ///
        /// 座位索引：-1 是驾驶位，0 是副驾，1..N 是后排与附加座。
        /// </summary>
        internal void LoadIntoVehicles(VehicleHash model)
        {
            try
            {
                Model carModel = new Model(model);
                if (!carModel.IsValid) { Notify("~r~载具模型不可用"); return; }

                carModel.Request(1000);
                int waited = 0;
                while (!carModel.IsLoaded && waited < 4000) { Script.Wait(0); waited += 50; }
                if (!carModel.IsLoaded) { Notify("~r~载具模型加载超时"); return; }

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                Vehicle car = null;
                int seat = -1;
                int maxSeat = 0;
                int loaded = 0;
                int carsMade = 0;

                // 用 while + 手工推进下标，而不是 foreach。
                //
                // 原实现是 foreach + `if (座位满了) { car = null; continue; }`，那个 continue
                // 会跳到**下一个队友**，于是当前这个队友被整个跳过、永远没有座位。
                // 实测 35 个队友只坐上了 7 个就是这个原因。
                // 正确做法是：车满了就只换车，**不推进队友下标**，让同一个队友重试。
                Companion[] agents = _agents.ToArray();
                int idx = 0;

                while (idx < agents.Length)
                {
                    Companion agent = agents[idx];
                    if (!agent.Exists) { idx++; continue; }

                    // 需要一辆新车：还没有车，或当前车座位已满
                    if (car == null || seat > maxSeat)
                    {
                        // 车距不要随数量线性外推，否则 30 个队友会在前方排出近百米。
                        // 按 6 个一组循环使用位置，整体控制在一个紧凑的范围内。
                        int slot = carsMade % 6;
                        float side = ((slot % 3) - 1) * 5.0f;
                        float back = 8.0f + ((slot / 3) * 6.0f);

                        Vector3 pos = player.Position
                                    + player.ForwardVector * back
                                    + player.RightVector * side;

                        float groundZ;
                        if (World.GetGroundHeight(pos, out groundZ))
                            pos = new Vector3(pos.X, pos.Y, groundZ + 0.5f);

                        car = World.CreateVehicle(carModel, pos, player.Heading);
                        if (car == null || !car.Exists())
                        {
                            Notify("~r~生成载具失败，已安排 " + loaded + " 个队友");
                            return;
                        }

                        car.IsPersistent = true;
                        car.PlaceOnGround();
                        car.IsEngineRunning = true;
                        carsMade++;

                        // 登记为小队车辆：之后 EnforceSeating 会用这批车来维持座位，
                        // 队友被小队 AI 赶下车后会重新被塞回这些座位。
                        RegisterSquadVehicle(car);

                        // 座位总数含驾驶位：PassengerCapacity 不含驾驶员
                        maxSeat = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, car);
                        seat = -1;
                        Script.Wait(0);
                    }

                    // 找到这辆车里下一个空座位
                    while (seat <= maxSeat && !car.IsSeatFree((VehicleSeat)seat)) seat++;

                    if (seat > maxSeat)
                    {
                        // 这辆车满了：换车，但**不推进 idx**，下一个循环让同一个队友重试
                        car = null;
                        continue;
                    }

                    agent.WarpIntoVehicle(car, (VehicleSeat)seat);
                    seat++;
                    loaded++;
                    idx++;
                }

                Notify("~g~已安排 " + loaded + "/" + agents.Length + " 个队友上车，用了 " +
                       carsMade + " 辆车（尽量坐满每辆）");
                BuddyLog.Info("squad: loaded " + loaded + "/" + agents.Length +
                              " agents into " + carsMade + " vehicles, model=" + model);
            }
            catch (Exception ex) { BuddyLog.Error("LoadIntoVehicles failed", ex); }
        }

        /// <summary>
        /// 生成指定数量的队友，全部使用同一个模型。
        ///
        /// 注意这里必须"每个队友一个新实例再生成"，不能复用 Companion.SpawnWithModel 的语义——
        /// 那个方法在队友已存在时会转为"换皮肤"，用在循环里会让 N 次调用全都作用在同一个
        /// 队友身上，结果只多出一个而不是 N 个。
        ///
        /// 模型只加载一次然后复用：N 个队友用同一个模型时重复 Request 是浪费，
        /// 而模型加载是这一步最慢的环节。
        /// </summary>
        internal int SpawnMany(PedHash hash, int quantity, out string error)
        {
            error = null;
            if (quantity < 1) quantity = 1;

            Model model = new Model(hash);
            if (!model.IsPed)
            {
                error = "该模型不是人物模型";
                return 0;
            }

            model.Request(1000);
            int waited = 0;
            while (!model.IsLoaded && waited < 5000)
            {
                Script.Wait(0);
                waited += 50;
            }

            if (!model.IsLoaded)
            {
                error = "模型加载超时（该皮肤可能在本安装中缺失）";
                return 0;
            }

            return SpawnLoop(hash, quantity, null, out error);
        }

        /// <summary>
        /// 批量生成，每个队友从给定的皮肤列表里轮换取一个。
        ///
        /// 轮换而不是全部随机：随机会出现"5 个人里 4 个长得一样"。按顺序循环能保证
        /// 队形在视觉上尽量分散，同时第 N 个和第 N+len 个仍然可能相同，这是可接受的。
        /// </summary>
        internal int SpawnManyVaried(IList<PedHash> skins, int quantity, out string error)
        {
            error = null;
            if (skins == null || skins.Count == 0)
            {
                error = "皮肤列表为空";
                return 0;
            }
            if (quantity < 1) quantity = 1;

            return SpawnLoop(skins[0], quantity, skins, out error);
        }

        /// <summary>生成循环：skins 为 null 时全部用 baseHash，否则逐个轮换。</summary>
        private int SpawnLoop(PedHash baseHash, int quantity, IList<PedHash> skins, out string error)
        {
            error = null;
            var loaded = new Dictionary<PedHash, Model>();

            int made = 0;
            for (int i = 0; i < quantity; i++)
            {
                PedHash hash = (skins == null) ? baseHash : skins[i % skins.Count];

                // 每个用到的模型只加载一次；批量生成同皮肤时这是最省时的做法
                Model model;
                if (!loaded.TryGetValue(hash, out model))
                {
                    model = new Model(hash);
                    if (!model.IsPed)
                    {
                        BuddyLog.Warn("squad: skipped non-ped model in batch, hash=" + hash);
                        continue;
                    }

                    model.Request(1000);
                    int waited = 0;
                    while (!model.IsLoaded && waited < 5000) { Script.Wait(0); waited += 50; }
                    if (!model.IsLoaded)
                    {
                        BuddyLog.Warn("squad: model failed to load in batch, hash=" + hash);
                        continue;
                    }
                    loaded[hash] = model;
                }

                Companion agent = CreateAgent();
                agent.SetSpawnModel(hash);

                if (agent.SpawnWithModel(hash)) made++;
                else _agents.Remove(agent);   // 生成失败就移除空壳，别拖慢遍历

                Script.Wait(0);
            }

            if (made == 0) error = "生成失败（世界拒绝了这些模型）";
            return made;
        }

        /// <summary>新建一个 Companion，并给它唯一的关系组名与 HUD 可见性。</summary>
        private Companion CreateAgent()
        {
            _spawnCounter++;

            var agent = new Companion();
            agent.SetSquadIdentity(_spawnCounter);

            // 只有第一个队友画 HUD：多个 HUD 会叠在同一个屏幕位置变成一团。
            agent.ShowHud = _agents.Count == 0;

            _agents.Add(agent);
            return agent;
        }

        /// <summary>
        /// 确保队伍里至少有一个队友实例（不生成 Ped，只是让命令有对象可发）。
        /// 首次按 J 召唤时会用到。
        /// </summary>
        internal Companion EnsureAtLeastOne()
        {
            if (_agents.Count == 0) CreateAgent();
            return _agents[0];
        }

        /// <summary>每帧驱动全部队友。</summary>
        internal void Update()
        {
            if (_agents.Count == 0) return;

            try
            {
                Companion[] snapshot = _agents.ToArray();
                foreach (Companion agent in snapshot)
                {
                    try
                    {
                        if (!agent.Exists)
                        {
                            // 队友的 Ped 已被世界回收（或它自己被收回），把这一位释放掉。
                            // 用 Remove 而不是就地删除，避免在遍历中改动集合。
                            _agents.Remove(agent);
                            continue;
                        }

                        agent.Update();
                    }
                    catch (Exception ex)
                    {
                        BuddyLog.Error("squad: one agent failed to update", ex);
                    }
                }

                // 第一个队友的 HUD 位置固定；如果 0 号退场了，把 HUD 交给新的 0 号，
                // 否则会出现"队友还在但没有 HUD"。
                if (_agents.Count > 0 && !_agents[0].ShowHud)
                {
                    _agents[0].ShowHud = true;
                }

                EnforceSeating();
            }
            catch (Exception ex) { BuddyLog.Error("squad update failed", ex); }
        }

        // ==================================================================
        // 队伍车辆登记 + 座位持续维持
        //
        // 用户要求：**检测到玩家在载具内时，agent 要去找之前生成的载具并坐进座位**，
        // 而且所有座位都要有 agent。
        //
        // 为什么必须"持续维持"而不是"安排一次"：
        //   队友同时是玩家小队成员，小队 AI 会不断让他们下车徒步跟随玩家。
        //   之前只在点菜单那一刻安排上车，过一会儿车里就只剩驾驶员一个
        //   ——这正是用户报的"只有驾驶员在车内，其他人依旧在车外"。
        // ==================================================================

        private readonly List<Vehicle> _squadVehicles = new List<Vehicle>();
        private int _nextSeatingCheck;

        /// <summary>把一辆车登记为"小队车辆"，座位维持会用到它。</summary>
        internal void RegisterSquadVehicle(Vehicle vehicle)
        {
            try
            {
                if (vehicle == null || !vehicle.Exists()) return;
                if (_squadVehicles.Contains(vehicle)) return;

                _squadVehicles.Add(vehicle);

                int maxSeat = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
                BuddyLog.Info("squad: registered vehicle model=" + vehicle.Model.Hash +
                              " seats=" + (maxSeat + 1));
            }
            catch (Exception ex) { BuddyLog.Error("RegisterSquadVehicle failed", ex); }
        }

        /// <summary>
        /// 持续维持座位：玩家在载具内时，把所有没上车的队友塞进已登记车辆的空座位。
        ///
        /// 每 2 秒检查一次 —— 没必要每帧，而且塞人本身是有开销的操作。
        /// 只在**玩家自己也在载具里**时才做：玩家步行时队友应该跟着走路。
        /// </summary>
        private void EnforceSeating()
        {
            if (Game.GameTime < _nextSeatingCheck) return;
            _nextSeatingCheck = Game.GameTime + 2000;

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                if (!player.IsInVehicle()) return;

                // 清掉已被世界回收的车
                for (int i = _squadVehicles.Count - 1; i >= 0; i--)
                {
                    Vehicle v = _squadVehicles[i];
                    if (v == null || !v.Exists()) _squadVehicles.RemoveAt(i);
                }

                if (_squadVehicles.Count == 0) return;

                // 找出还没上车的队友
                var waiting = new List<Companion>();
                foreach (Companion a in _agents.ToArray())
                {
                    if (!a.Exists) continue;
                    if (!a.IsSeated) waiting.Add(a);
                }

                if (waiting.Count == 0) return;

                int placed = 0;

                foreach (Vehicle car in _squadVehicles.ToArray())
                {
                    if (waiting.Count == 0) break;
                    if (car == null || !car.Exists()) continue;

                    int maxSeat = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, car);

                    // 座位索引：-1 = 驾驶位，0..maxSeat = 乘客位
                    for (int seat = -1; seat <= maxSeat && waiting.Count > 0; seat++)
                    {
                        bool free;
                        try { free = car.IsSeatFree((VehicleSeat)seat); }
                        catch (Exception) { free = false; }

                        if (!free) continue;      // 座位有人，跳过；**不推进 waiting**（关键）

                        Companion agent = waiting[0];
                        agent.WarpIntoVehicle(car, (VehicleSeat)seat);
                        waiting.RemoveAt(0);
                        placed++;
                    }
                }

                if (placed > 0)
                {
                    BuddyLog.Info("squad: seating pass placed " + placed +
                                  " agents, " + waiting.Count + " still waiting");
                }
            }
            catch (Exception ex) { BuddyLog.Error("EnforceSeating failed", ex); }
        }

        /// <summary>收回全部队友。</summary>
        internal void DespawnAll()
        {
            foreach (Companion agent in _agents.ToArray())
            {
                try { agent.Despawn(); } catch (Exception) { }
            }
            _agents.Clear();
        }

        /// <summary>队伍里是否至少有一个队友在场。</summary>
        internal bool AnyExists()
        {
            foreach (Companion agent in _agents)
            {
                if (agent.Exists) return true;
            }
            return false;
        }

        /// <summary>队伍里是否已有队友实例（不代表 Ped 已生成）。</summary>
        internal bool HasAgent { get { return _agents.Count > 0; } }

        // ------------------------------------------------------------------
        // 指令：全部作用于队内每一个队友
        // ------------------------------------------------------------------

        internal void OrderFollow()          { ForEach(a => a.OrderFollow()); }
        internal void OrderStay()            { ForEach(a => a.OrderStay()); }
        internal void OrderMoveToWaypoint()  { ForEach(a => a.OrderMoveToWaypoint()); }
        internal void OrderEnterPlayerVehicle() { ForEach(a => a.OrderEnterPlayerVehicle()); }
        internal void OrderLeaveVehicle()    { ForEach(a => a.OrderLeaveVehicle()); }
        internal void OrderAttackAimedTarget() { ForEach(a => a.OrderAttackAimedTarget()); }
        internal void OrderStopCombat()      { ForEach(a => a.OrderStopCombat()); }
        internal void OrderTeleportHere()    { ForEach(a => a.OrderTeleportHere()); }

        internal void GiveWeapon(WeaponHash hash, int ammo) { ForEach(a => a.GiveWeapon(hash, ammo)); }
        internal void StripWeapons()         { ForEach(a => a.StripWeapons()); }
        internal void Restore()              { ForEach(a => a.Restore()); }
        internal void SetInfiniteAmmo(bool on) { ForEach(a => a.SetInfiniteAmmo(on)); }

        internal void SetVehicleInvincible(bool on) { ForEach(a => a.SetVehicleInvincible(on)); }
        internal void SetVehicleBoost(float m)      { ForEach(a => a.SetVehicleBoost(m)); }
        internal void RepairVehicle()               { ForEach(a => a.RepairVehicle()); }
        internal void ApplyVehicleModKit()          { ForEach(a => a.ApplyVehicleModKit()); }

        internal void ApplyVehicleCosmetics(int r, int g, int b, int tint, string label)
        {
            ForEach(a => a.ApplyVehicleCosmetics(r, g, b, tint, label));
        }

        /// <summary>让全部队友各自生成一辆车并开车跟随。</summary>
        internal void OrderVehicleEscortAll(VehicleHash model)
        {
            ForEach(a => a.OrderVehicleEscort(model));
        }

        internal void StopVehicleEscortAll()
        {
            ForEach(a => a.StopVehicleEscort());
        }

        /// <summary>把某个皮肤应用到全部队友身上。</summary>
        internal void RequestModelChangeAll(PedHash hash)
        {
            ForEach(a => a.RequestModelChange(hash));
        }

        private void ForEach(Action<Companion> action)
        {
            try
            {
                foreach (Companion agent in _agents.ToArray())
                {
                    try { action(agent); } catch (Exception) { }
                }
            }
            catch (Exception ex) { BuddyLog.Error("squad command failed", ex); }
        }
    }
}
