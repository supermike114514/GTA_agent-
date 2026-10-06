using System;
using System.Collections.Generic;
using GTA;

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

        /// <summary>读一份快照，避免遍历时列表被改动。</summary>
        internal Companion[] Snapshot() { return _agents.ToArray(); }

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

            int made = 0;
            for (int i = 0; i < quantity; i++)
            {
                // 每次都新建一个空实例（尚未生成 Ped），再让它自己按这个模型生成。
                Companion agent = CreateAgent();
                agent.SetSpawnModel(hash);

                if (agent.SpawnWithModel(hash)) made++;
                else
                {
                    // 生成失败就把这个空壳移出队伍，避免留下"存在但没有 Ped"的队友拖慢遍历
                    _agents.Remove(agent);
                }

                Script.Wait(0);   // 逐个生成之间推进一帧，避免密集生成卡住
            }

            if (made == 0) error = "生成失败（世界拒绝了该模型）";
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
            }
            catch (Exception ex) { BuddyLog.Error("squad update failed", ex); }
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
