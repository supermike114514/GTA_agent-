using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>L1: the strategic intent the brain settles on each frame.</summary>
    internal enum BuddyState
    {
        Hidden,     // cutscene / not usable
        Downed,     // incapacitated, waiting for the player to revive
        InVehicle,  // mounted in a vehicle with (or driving to) the player
        Combat,     // threat present
        Follow,     // default escort
        Idle,       // player far away / nothing to do
    }

    /// <summary>
    /// The companion. Owns the ped, the three-layer state machine and all native calls.
    ///
    /// L1 BuddyBrain  : DecideState()  - priority decision, no native task calls
    /// L2 Behaviors   : Enter*/Update*  - translate intent into engine tasks
    /// L3 Engine tasks: TASK_FOLLOW_TO_OFFSET_OF_ENTITY / TASK_COMBAT_PED / drive missions
    /// </summary>
    internal sealed class Companion
    {
        private Ped _ped;

        /// <summary>
        /// 本队友的皮肤。生成、死亡补位、换皮肤都更新它，
        /// 这样一个队友在整局里始终保持同一张脸。
        /// </summary>
        private PedHash _spawnModel = BuddyConfig.DefaultPedHash;
        private Blip _blip;
        private RelationshipGroup _group;
        private bool _groupCreated;
        private RelationshipGroup _playerGroup;
        private bool _playerGroupCaptured;

        private readonly BuddyHud _hud = new BuddyHud();

        // --- brain state ---
        private BuddyState _state = BuddyState.Idle;
        private BuddyState _prevState = BuddyState.Idle;
        private string _stateReason = "init";

        // --- threat tracking ---
        private Ped _threat;
        private int _nextThreatScan;
        private int _nextReloadCheck;
        private int _nextCombatIssue;

        // --- survival ---
        private int _nextMedkitAllowed;
        private int _nextSurvivalCheck;
        private int _downedSince;
        private int _invulnerableUntil;
        private bool _pendingRevive;

        // --- driving ---
        private Vehicle _driveTarget;
        private int _nextVehicleScan;
        private int _enterVehicleIssuedAt;

        // --- follow ---
        private int _nextFollowReissue;
        private int _lastFollowIssueAt;
        private bool _userStandby;

        internal bool Exists => _ped != null && _ped.Exists();
        internal Ped Ped => _ped;
        internal BuddyState State => _state;
        internal bool IsDowned => _state == BuddyState.Downed;

        // ------------------------------------------------------------------
        // lifecycle
        // ------------------------------------------------------------------

        /// <summary>Spawns the companion with the default model.</summary>
        internal bool Spawn()
        {
            return SpawnWithModel(_spawnModel);
        }

        /// <summary>
        /// Spawns the companion with a chosen model, so the panel can let the user pick a skin
        /// and bring the buddy in already wearing it.
        ///
        /// The model has to be streamed in first - CreatePed on a model that is not resident
        /// yields an invisible or empty ped.
        /// </summary>
        internal bool SpawnWithModel(PedHash hash, string label = null)
        {
            _spawnModel = hash;
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                {
                    Notify("~r~生成失败：找不到玩家");
                    return false;
                }

                Model model = new Model(hash);
                if (!model.IsPed)
                {
                    Notify("~r~生成失败：该模型不是人物模型");
                    return false;
                }

                // Bring the model in before touching the world.
                model.Request(1000);
                int waited = 0;
                while (!model.IsLoaded && waited < 5000)
                {
                    Script.Yield();
                    waited += 50;
                }
                if (!model.IsLoaded)
                {
                    Notify("~r~生成失败：模型加载超时（该皮肤可能在本安装中缺失）");
                    return false;
                }

                // Already have one? Swap its body instead of leaving a stray behind.
                if (Exists)
                {
                    RequestModelChange(hash);
                    Notify("~b~队友已在场，正在为他更换皮肤");
                    return true;
                }

                EnsureRelationshipGroups(player);

                Vector3 spawnPos = player.Position + (player.ForwardVector * -BuddyConfig.SpawnDistance);
                Vector3 ground = spawnPos;
                float groundZ;
                if (World.GetGroundHeight(spawnPos, out groundZ))
                {
                    ground = new Vector3(spawnPos.X, spawnPos.Y, groundZ + 0.5f);
                }

                _ped = World.CreatePed(model, ground, player.Heading);
                if (_ped == null || !_ped.Exists())
                {
                    Notify("~r~生成失败：CreatePed 返回空");
                    return false;
                }

                ConfigurePed(player);
                AttachBlip();
                _state = BuddyState.Idle;
                _downedSince = 0;
                _pendingRevive = false;

                Notify("~g~" + (string.IsNullOrEmpty(label) ? BuddyConfig.BuddyName : label + " 队友") +
                       "已就位~s~  按 " + KeyHint.Toggle + " 待命/跟随，按 " + KeyHint.Revive + " 施救");
                return true;
            }
            catch (Exception ex)
            {
                Notify("~r~生成异常: " + ex.Message);
                return false;
            }
        }

        /// <summary>Spawns with the default model, using the string name from config.</summary>
        internal bool SpawnWithModel(string modelName)
        {
            try
            {
                Model m = new Model(modelName);
                return SpawnWithModel((PedHash)m.Hash, modelName);
            }
            catch (Exception ex)
            {
                BuddyLog.Error("SpawnWithModel(name) failed", ex);
                return false;
            }
        }

        internal void Despawn()
        {
            try
            {
                if (_blip != null && _blip.Exists()) _blip.Delete();
                _blip = null;
                if (Exists)
                {
                    _ped.Task.ClearAllImmediately();
                    _ped.MarkAsNoLongerNeeded();
                }
                _ped = null;
                _state = BuddyState.Idle;
                Notify("~y~" + BuddyConfig.BuddyName + "已离开");
            }
            catch (Exception) { _ped = null; }
        }

        /// <summary>K: hold position where it stands, or resume escorting the player.</summary>
        internal void ToggleStandby()
        {
            if (!Exists) return;

            _userStandby = !_userStandby;
            try
            {
                _ped.Task.ClearAll();
                if (_userStandby)
                {
                    _ped.Task.StandStill(-1);
                    Notify("~y~" + BuddyConfig.BuddyName + "原地待命~s~  再按 " + KeyHint.Toggle + " 恢复跟随");
                }
                else
                {
                    _nextFollowReissue = 0;
                    Notify("~g~" + BuddyConfig.BuddyName + "恢复跟随");
                }
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------
        // panel-issued orders
        // ------------------------------------------------------------------

        /// <summary>Resume escorting the player (panel: 跟随我).</summary>
        internal void OrderFollow()
        {
            if (!Exists) return;

            _userStandby = false;
            try
            {
                _ped.Task.ClearAll();
                _nextFollowReissue = 0;
                _state = BuddyState.Follow;
                Notify("~g~指令：跟随");
                BuddyLog.Info("order: follow");
            }
            catch (Exception) { }
        }

        /// <summary>Hold this exact spot (panel: 原地待命).</summary>
        internal void OrderStay()
        {
            if (!Exists) return;

            _userStandby = true;
            try
            {
                _ped.Task.ClearAll();
                _ped.Task.StandStill(-1);
                _state = BuddyState.Idle;
                Notify("~y~指令：原地待命");
                BuddyLog.Info("order: stay");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Walk to the map waypoint (panel: 前往路径点). Reports when no waypoint is set
        /// rather than silently doing nothing.
        /// </summary>
        internal void OrderMoveToWaypoint()
        {
            if (!Exists) return;

            try
            {
                if (!Function.Call<bool>(Hash.IS_WAYPOINT_ACTIVE))
                {
                    Notify("~y~指令失败：地图上没有设置路径点");
                    return;
                }

                int blip = Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, 4); // 4 = waypoint blip sprite
                if (blip == 0)
                {
                    Notify("~y~指令失败：找不到路径点坐标");
                    return;
                }

                Vector3 pos = Function.Call<Vector3>(Hash.GET_BLIP_COORDS, blip);

                _userStandby = false;
                _ped.Task.ClearAll();
                _ped.Task.FollowNavMeshTo(pos, PedMoveBlendRatio.Run, -1, 2.0f,
                                          FollowNavMeshFlags.Default, 0f);
                _state = BuddyState.Follow;
                Notify("~g~指令：前往路径点");
                BuddyLog.Info("order: move to waypoint " + pos.ToString());
            }
            catch (Exception ex) { BuddyLog.Error("OrderMoveToWaypoint failed", ex); }
        }

        /// <summary>Get into the player's current vehicle (panel: 上我的车).</summary>
        internal void OrderEnterPlayerVehicle()
        {
            if (!Exists) return;

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.IsInVehicle())
                {
                    Notify("~y~指令失败：你不在载具里");
                    return;
                }

                Vehicle v = player.CurrentVehicle;
                if (v == null || !v.Exists())
                {
                    Notify("~y~指令失败：找不到你的载具");
                    return;
                }

                _userStandby = false;
                _ped.Task.ClearAll();
                _ped.Task.EnterVehicle(v, VehicleSeat.Any, 12000, 1.0f, EnterVehicleFlags.None);
                _enterVehicleIssuedAt = Game.GameTime;
                _state = BuddyState.InVehicle;
                Notify("~g~指令：上我的车");
                BuddyLog.Info("order: enter player vehicle");
            }
            catch (Exception ex) { BuddyLog.Error("OrderEnterPlayerVehicle failed", ex); }
        }

        /// <summary>Leave whatever vehicle it is in (panel: 下车).</summary>
        internal void OrderLeaveVehicle()
        {
            if (!Exists) return;

            try
            {
                if (!_ped.IsInVehicle())
                {
                    Notify("~y~指令失败：队友不在载具里");
                    return;
                }

                _ped.Task.LeaveVehicle(LeaveVehicleFlags.None);
                _driveTarget = null;
                Notify("~g~指令：下车");
                BuddyLog.Info("order: leave vehicle");
            }
            catch (Exception ex) { BuddyLog.Error("OrderLeaveVehicle failed", ex); }
        }

        /// <summary>
        /// Attack whatever the player is aiming at or currently locked onto
        /// (panel: 攻击我瞄准的目标).
        /// </summary>
        internal void OrderAttackAimedTarget()
        {
            if (!Exists) return;

            try
            {
                Entity aimed = Game.Player.TargetedEntity;
                Ped target = (aimed != null && aimed.Exists()) ? aimed as Ped : null;

                // fall back to the nearest ped the player is already fighting
                if (target == null || target.IsDead || target.IsPlayer)
                {
                    target = FindThreat(Game.Player.Character);
                }

                if (target == null || !target.Exists() || target.IsDead)
                {
                    Notify("~y~指令失败：没有有效目标（瞄准一个行人再试）");
                    return;
                }

                _userStandby = false;
                _threat = target;
                _nextCombatIssue = 0;
                _ped.Task.ClearAll();
                _ped.Task.Combat(target);
                _state = BuddyState.Combat;
                Notify("~g~指令：攻击目标");
                BuddyLog.Info("order: attack target " + target.Handle);
            }
            catch (Exception ex) { BuddyLog.Error("OrderAttackAimedTarget failed", ex); }
        }

        /// <summary>Break off and return to the player (panel: 停止战斗).</summary>
        internal void OrderStopCombat()
        {
            if (!Exists) return;

            try
            {
                _threat = null;
                _ped.Task.ClearAll();
                _nextFollowReissue = 0;
                _state = BuddyState.Follow;
                Notify("~g~指令：停止战斗，归队");
                BuddyLog.Info("order: stop combat");
            }
            catch (Exception ex) { BuddyLog.Error("OrderStopCombat failed", ex); }
        }

        /// <summary>Emergency resync: put the companion next to the player (panel: 瞬移到身边).</summary>
        internal void OrderTeleportHere()
        {
            if (!Exists) return;

            try
            {
                TeleportNear(Game.Player.Character);
                Notify("~g~指令：已归位");
                BuddyLog.Info("order: teleport here");
            }
            catch (Exception ex) { BuddyLog.Error("OrderTeleportHere failed", ex); }
        }

        /// <summary>Puts a fresh ped in place of a dead one (respawn after a wipe).</summary>
        internal void ReplaceDeadPed()        {
            try
            {
                if (_blip != null && _blip.Exists()) _blip.Delete();
                _blip = null;
                _ped = null;
                Spawn();
            }
            catch (Exception) { _ped = null; }
        }

        private void ConfigurePed(Ped player)
        {
            _ped.IsPersistent = true;
            // Mission-entity semantics: the engine will not stream it away, and it keeps
            // its relationship group after any script cleanup.
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, _ped, true, true);
            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, _ped, Function.Call<int>(Hash.GET_PED_GROUP_INDEX, player));
            Function.Call(Hash.SET_PED_NEVER_LEAVES_GROUP, _ped, true);

            // ---- 借引擎自带的小队算法 --------------------------------------
            //
            // GTA 自己就有一套"小队跟随"的实现（游戏本体的小队任务用它），
            // 比自己写驾驶逻辑可靠得多。这里把其中两个关键开关打开：
            //
            //   SET_GROUP_SEPARATION_RANGE        : 成员之间保持的间距。
            //       调大之后引擎会主动把成员拉开 —— 这正是"车辆堆积"需要的。
            //
            //   SET_PED_CAN_TELEPORT_TO_GROUP_LEADER
            //       : **引擎自己会把掉队的成员传送到队长身边**。
            //         也就是说"玩家跑远了就把队友传过来"这件事，游戏本身就支持，
            //         不需要我自己写传送逻辑（我前面手写的那套可以当作兜底）。
            try
            {
                int group = Function.Call<int>(Hash.GET_PED_GROUP_INDEX, player);
                if (group != 0)
                {
                    Function.Call(Hash.SET_GROUP_SEPARATION_RANGE, group, BuddyConfig.GroupSeparationRange);
                    Function.Call(Hash.SET_PED_CAN_TELEPORT_TO_GROUP_LEADER, _ped, group, true);
                }
            }
            catch (Exception ex) { BuddyLog.Error("group formation setup failed", ex); }

            _ped.RelationshipGroup = _group;
            _ped.SetConfigFlag(PedConfigFlagToggles.KeepRelationshipGroupAfterCleanUp, true);
            _ped.SetConfigFlag(PedConfigFlagToggles.TreatAsFriendlyForTargetingAndDamage, true);
            _ped.SetConfigFlag(PedConfigFlagToggles.ForcePedLoadCover, true);
            _ped.SetConfigFlag(PedConfigFlagToggles.AllowNearbyCoverUsage, true);
            _ped.SetConfigFlag(PedConfigFlagToggles.BlockCoweringInCover, true);
            _ped.SetConfigFlag(PedConfigFlagToggles.DontActivateRagdollFromBulletImpact, true);
            _ped.SetConfigFlag(PedConfigFlagToggles.KeepTasksAfterCleanUp, true);

            _ped.MaxHealth = 220;
            _ped.Health = 220;
            _ped.Armor = 50;
            _ped.Accuracy = 60;
            _ped.CanBeDraggedOutOfVehicle = false;
            _ped.CanRagdoll = true;
            _ped.DiesOnLowHealth = false;       // we own the downed state, not the engine
            _ped.KeepTaskWhenMarkedAsNoLongerNeeded = true;

            if (_playerGroupCaptured)
            {
                // Damage source filter: the player's group (player + allies) cannot hurt the buddy.
                _ped.SetNotDamagedByRelGroup(_playerGroup);
            }

            GiveCombatLoadout();
        }

        private void GiveCombatLoadout()
        {
            _ped.Weapons.Give(WeaponHash.CarbineRifle, 400, true, true);
            _ped.Weapons.Give(WeaponHash.Pistol, 200, false, true);
            _ped.Weapons.Select(WeaponHash.CarbineRifle);
        }

        // ------------------------------------------------------------------
        // changing the companion's model (its "skin")
        // ------------------------------------------------------------------

        private Model _pendingModel;
        private int _modelRequestedAt;
        private bool _modelChangePending;

        /// <summary>True while a skin change is waiting for the model to stream in.</summary>
        internal bool ModelChangePending { get { return _modelChangePending; } }

        /// <summary>
        /// Queues a model change for the companion. The model has to stream in first, so the
        /// swap happens in <see cref="UpdateModelChange"/> rather than immediately - spawning a
        /// ped from a model that is not loaded yields an invisible or empty ped.
        /// </summary>
        internal void RequestModelChange(PedHash hash)
        {
            _spawnModel = hash;
            try
            {
                if (!Exists)
                {
                    Notify("~r~队友不在场，先召唤队友");
                    return;
                }

                var model = new Model(hash);
                if (!model.IsValid)
                {
                    Notify("~r~该模型在本安装中不可用");
                    return;
                }

                model.Request(1000);
                _pendingModel = model;
                _modelRequestedAt = Game.GameTime;
                _modelChangePending = true;
                Notify("~b~正在为队友更换皮肤…");
            }
            catch (Exception ex)
            {
                _modelChangePending = false;
                BuddyLog.Error("companion model change request failed", ex);
            }
        }

        private void UpdateModelChange()
        {
            if (!_modelChangePending) return;

            try
            {
                bool ready = _pendingModel.IsLoaded;
                if (!ready && Game.GameTime - _modelRequestedAt <= 5000) return;

                _modelChangePending = false;

                if (!ready)
                {
                    Notify("~r~模型加载超时，该皮肤可能在本安装中缺失");
                    return;
                }

                if (!Exists)
                {
                    Notify("~r~队友已不在场，皮肤更改取消");
                    return;
                }

                PedHash hash = (PedHash)_pendingModel.Hash;
                Ped player = Game.Player.Character;

                // Take the old body's position first so the new one can appear exactly there
                // and the swap is not visible as a teleport.
                Vector3 oldPosition = _ped.Position;
                float oldHeading = _ped.Heading;
                Ped old = _ped;

                if (!SpawnAs(hash)) { Notify("~r~更换皮肤失败"); return; }

                if (Exists)
                {
                    _ped.Position = oldPosition;
                    _ped.Heading = oldHeading;
                }

                if (old != null && old.Exists() && old != _ped) old.Delete();

                if (player != null && player.Exists()) ConfigurePed(player);
                Notify("~g~队友皮肤已更换");
            }
            catch (Exception ex)
            {
                _modelChangePending = false;
                BuddyLog.Error("companion model change failed", ex);
            }
        }

        /// <summary>
        /// Creates the ped for a script that was registered with the model it should use.
        /// Returns false when the engine refuses the model.
        /// </summary>
        private bool SpawnAs(PedHash model)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return false;

                Vector3 pos = player.Position + player.RightVector * 1.2f;
                Ped created = World.CreatePed(model, pos);

                if (created == null || !created.Exists()) return false;

                _ped = created;
                return true;
            }
            catch (Exception ex)
            {
                BuddyLog.Error("SpawnAs failed", ex);
                return false;
            }
        }

        /// <summary>Arms the companion with the given weapon and a generous ammo reserve.</summary>
        internal void GiveWeapon(WeaponHash hash, int ammo)
        {
            try
            {
                if (!Exists) { Notify("~r~队友不在场"); return; }
                _ped.Weapons.Give(hash, ammo, true, true);
                _ped.Weapons.Select(hash);
                Notify("~g~已为队友发放武器");
            }
            catch (Exception ex) { BuddyLog.Error("companion give weapon failed", ex); }
        }

        /// <summary>Restores the companion to full health and armour.</summary>
        internal void Restore()
        {
            try
            {
                if (!Exists) { Notify("~r~队友不在场"); return; }

                if (_ped.MaxHealth < 220) _ped.MaxHealth = 220;
                _ped.Health = _ped.MaxHealth;
                _ped.Armor = 100;
                Notify("~g~队友已恢复满状态");
            }
            catch (Exception ex) { BuddyLog.Error("companion restore failed", ex); }
        }

        /// <summary>Removes every weapon the companion carries.</summary>
        internal void StripWeapons()
        {
            try
            {
                if (!Exists) { Notify("~r~队友不在场"); return; }
                _ped.Weapons.RemoveAll();
                Notify("~y~已收缴队友的全部武器");
            }
            catch (Exception ex) { BuddyLog.Error("companion strip weapons failed", ex); }
        }

        /// <summary>Turns the companion's ammo supply on or off for every weapon it carries.</summary>
        internal void SetInfiniteAmmo(bool on)
        {
            try
            {
                if (!Exists) { Notify("~r~队友不在场"); return; }

                foreach (WeaponHash hash in _ped.Weapons.GetAllWeaponHashes())
                {
                    Weapon w = _ped.Weapons[hash];
                    if (w == null || !w.IsPresent) continue;
                    w.InfiniteAmmo = on;
                    w.InfiniteAmmoClip = on;
                }
                Notify(on ? "~g~队友无限弹药已开启" : "~y~队友无限弹药已关闭");
            }
            catch (Exception ex) { BuddyLog.Error("companion infinite ammo failed", ex); }
        }

        // ==================================================================
        // 载具能力
        //
        // 这些操作默认作用于"玩家驾驶的载具"，若玩家没开车则作用于队友自己的车。
        // 这样同一个菜单项在两种场景下都符合直觉，不必让用户先选目标。
        // ==================================================================

        /// <summary>
        /// The vehicle these actions should act on: the player's when the player is driving,
        /// otherwise whatever the companion is in. Null when neither is in a car.
        /// </summary>
        private Vehicle TargetVehicle()
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player != null && player.Exists() && player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    if (v != null && v.Exists()) return v;
                }

                Ped buddy = _ped;
                if (buddy != null && buddy.Exists() && buddy.IsInVehicle())
                {
                    Vehicle v = buddy.CurrentVehicle;
                    if (v != null && v.Exists()) return v;
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>Makes the target vehicle immune to damage (and optionally to visual damage).</summary>
        internal void SetVehicleInvincible(bool on)
        {
            try
            {
                Vehicle v = TargetVehicle();
                if (v == null) { Notify("~r~不在载具里"); return; }

                v.IsInvincible = on;
                Function.Call(Hash.SET_VEHICLE_CAN_BREAK, v, !on);
                Function.Call(Hash.SET_VEHICLE_STRONG, v, on);
                Function.Call(Hash.SET_VEHICLE_TYRES_CAN_BURST, v, !on);
                Function.Call(Hash.SET_VEHICLE_EXPLODES_ON_HIGH_EXPLOSION_DAMAGE, v, !on);
                Function.Call(Hash.SET_DISABLE_VEHICLE_PETROL_TANK_DAMAGE, v, on);

                if (on)
                {
                    // top everything up as well, otherwise a car that was already wrecked
                    // stays visually wrecked even though it can no longer take damage
                    Function.Call(Hash.SET_VEHICLE_BODY_HEALTH, v, 1000.0f);
                    Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, v, 1000.0f);
                    Function.Call(Hash.SET_VEHICLE_PETROL_TANK_HEALTH, v, 1000.0f);
                }
                Notify(on ? "~g~载具无敌已开启" : "~y~载具无敌已关闭");
            }
            catch (Exception ex) { BuddyLog.Error("SetVehicleInvincible failed", ex); }
        }

        /// <summary>Raises engine power and torque, giving the target vehicle a speed boost.</summary>
        internal void SetVehicleBoost(float multiplier)
        {
            try
            {
                Vehicle v = TargetVehicle();
                if (v == null) { Notify("~r~不在载具里"); return; }

                v.EnginePowerMultiplier = multiplier;
                v.EngineTorqueMultiplier = multiplier;
                Notify("~g~载具动力：" + multiplier + "x");
            }
            catch (Exception ex) { BuddyLog.Error("SetVehicleBoost failed", ex); }
        }

        /// <summary>Restores the target vehicle: body, engine, tank, deformation, wheels, dirt.</summary>
        internal void RepairVehicle()
        {
            try
            {
                Vehicle v = TargetVehicle();
                if (v == null) { Notify("~r~不在载具里"); return; }

                Function.Call(Hash.SET_VEHICLE_FIXED, v);
                Function.Call(Hash.SET_VEHICLE_DEFORMATION_FIXED, v);
                Function.Call(Hash.SET_VEHICLE_BODY_HEALTH, v, 1000.0f);
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, v, 1000.0f);
                Function.Call(Hash.SET_VEHICLE_PETROL_TANK_HEALTH, v, 1000.0f);
                Function.Call(Hash.SET_VEHICLE_DIRT_LEVEL, v, 0.0f);
                Function.Call(Hash.SET_VEHICLE_WHEELS_CAN_BREAK, v, true);
                Notify("~g~载具已修好");
            }
            catch (Exception ex) { BuddyLog.Error("RepairVehicle failed", ex); }
        }

        /// <summary>
        /// Applies a stock "fully modded" build: max engine, brakes, transmission, suspension
        /// and armour, plus turbo. Armour doubles as the防弹 upgrade, which is why it is maxed.
        /// </summary>
        internal void ApplyVehicleModKit()
        {
            try
            {
                Vehicle v = TargetVehicle();
                if (v == null) { Notify("~r~不在载具里"); return; }

                // A mod kit must be installed before any mod can be applied.
                Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);

                var upgrades = new[]
                {
                    VehicleModType.Engine,
                    VehicleModType.Brakes,
                    VehicleModType.Transmission,
                    VehicleModType.Suspension,
                    VehicleModType.Armor,
                };

                int applied = 0;
                foreach (VehicleModType type in upgrades)
                {
                    try
                    {
                        VehicleMod mod = v.Mods[type];
                        if (mod == null || mod.Count <= 0) continue;
                        mod.Index = mod.Count - 1;   // highest available level
                        applied++;
                    }
                    catch (Exception) { }
                }

                Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, 18, true);   // 18 = Turbo

                Notify("~g~已应用改装（性能件 " + applied + " 项 + 涡轮）");
            }
            catch (Exception ex) { BuddyLog.Error("ApplyVehicleModKit failed", ex); }
        }

        /// <summary>Paints the target vehicle and tints its windows.</summary>
        internal void ApplyVehicleCosmetics(int r, int g, int b, int windowTint, string label)
        {
            try
            {
                Vehicle v = TargetVehicle();
                if (v == null) { Notify("~r~不在载具里"); return; }

                Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, v, r, g, b);
                Function.Call(Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR, v, r, g, b);
                if (windowTint >= 0) Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, v, windowTint);
                Notify("~g~" + label);
            }
            catch (Exception ex) { BuddyLog.Error("ApplyVehicleCosmetics failed", ex); }
        }

        // ==================================================================
        // 队友自己开车跟随玩家
        // ==================================================================

        private Vehicle _escortVehicle;
        private bool _escortMode;
        private int _nextEscortTask;

        // 待生成的座驾。命令只发起模型请求，真正的生成放到 tick 里做，
        // 这样菜单回调不会因为等待模型加载而中断 LemonUI 的处理流程。
        private bool _escortPending;
        private Model _escortModel;
        private int _escortRequestedAt;
        private int _nextEscortDiag;   // 诊断日志节流
        private Vector3? _lastEscortPos;   // 上次下发任务时车的位置，用于算"动了多远"

        // 直接驾驶兜底：引擎驾驶 AI 对履带/特种载具常常无效，卡住 5 秒后启用。
        private bool _escortDirectDrive;
        private int _escortStallSince;

        // 传送防抖：距离超阈值后的起始时间、上次传送时间
        private int _escortFarSince;
        private int _lastEscortTeleport;

        // 卡住时的处理阶梯（全部用引擎自带的任务，不自己开车）
        private bool _escortLongRange;     // 已切到 DRIVE_TO_COORD_LONGRANGE
        private bool _escortClearedOnce;   // 已清空过一次任务让引擎重规划

        /// <summary>
        /// 变形金刚模式：保留最初的"直接推"行为。
        ///
        /// 那个行为会让装甲车互相顶、叠成罗汉塔（实测截图很欢乐），
        /// 但它同时也是"车真的在动"的证明 —— 所以作为特色功能单独保留，默认关闭。
        /// 开启后跳过间距与避障，车辆会互相攀爬叠起来。
        /// </summary>
        internal static bool TransformerMode { get; set; }

        // 车载开火的节流与状态
        private int _nextEscortCombat;
        private bool _escortEngaging;

        /// <summary>True while the companion is driving its own car to keep up with the player.</summary>
        internal bool EscortMode { get { return _escortMode; } }

        /// <summary>
        /// Spawns a car for the companion and puts it in the driver's seat, then keeps it
        /// driving toward the player. Unlike "上我的车" this gives the buddy its own vehicle,
        /// so it can follow a player who is on foot or in a different car.
        /// </summary>
        internal void OrderVehicleEscort(VehicleHash model)
        {
            try
            {
                if (!Exists) { Notify("~r~队友不在场"); return; }

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                // 已经有车就直接恢复跟随模式，不再生成第二辆
                if (_escortVehicle != null && _escortVehicle.Exists() && !_escortVehicle.IsDead)
                {
                    _escortMode = true;
                    _escortPending = false;
                    _nextEscortTask = 0;
                    Notify("~g~队友已开始驾驶跟随");
                    return;
                }

                var carModel = new Model(model);
                if (!carModel.IsValid) { Notify("~r~载具模型不可用"); return; }

                // 只发起请求，不在这一帧等它加载完。
                //
                // 这里原来是 while(!IsLoaded) Script.Yield()，但这个方法是在 LemonUI 的
                // item.Activated 回调里被调用的 —— 在菜单的事件处理中间让出帧会把
                // LemonUI 的处理流程打断，多队友时表现就是"只有最近的那个生效"。
                // 现在改成记录请求，由 UpdateVehicleEscort 在正常的 tick 里完成生成。
                carModel.Request(1000);

                _escortModel = carModel;
                _escortPending = true;
                _escortMode = true;
                _escortRequestedAt = Game.GameTime;
                _nextEscortTask = 0;
                _lastEscortPos = null;

                BuddyLog.Info("escort[" + _squadIndex + "]: order accepted, waiting for model");
                Notify("~b~正在为队友准备座驾…");
            }
            catch (Exception ex) { BuddyLog.Error("OrderVehicleEscort failed", ex); }
        }

        /// <summary>待生成的座驾：模型加载完成后由 tick 创建，避免在菜单回调里阻塞。</summary>
        private void UpdateEscortSpawn(Ped player)
        {
            if (!_escortPending) return;

            try
            {
                if (!_escortModel.IsLoaded)
                {
                    // 超时保护：模型可能在本安装中缺失
                    if (Game.GameTime - _escortRequestedAt > 10000)
                    {
                        _escortPending = false;
                        _escortMode = false;
                        Notify("~r~座驾模型加载超时");
                    }
                    return;
                }

                _escortPending = false;

                // 每个队友的车在**不同位置**生成，但间距不能随编号线性外推：
                // 原来 back = 8 + (index/3)*5.5，第 30 个队友的车会生成在 60 米开外，
                // 整队排成近百米长的一列，看起来完全不像"跟随"。
                // 改成 6 个位置循环使用，整体控制在一个紧凑的范围内。
                int slot = _squadIndex % 6;
                float side = ((slot % 3) - 1) * 5.0f;            // -5 / 0 / +5 三列
                float back = 8.0f + ((slot / 3) * 6.0f);         // 两排：8m / 14m

                Vector3 pos = player.Position
                            + player.ForwardVector * back
                            + player.RightVector * side;

                float groundZ;
                if (World.GetGroundHeight(pos, out groundZ)) pos = new Vector3(pos.X, pos.Y, groundZ + 0.5f);

                Vehicle car = World.CreateVehicle(_escortModel, pos, player.Heading);
                if (car == null || !car.Exists())
                {
                    _escortMode = false;
                    Notify("~r~生成载具失败");
                    return;
                }

                car.IsPersistent = true;

                // 飞行器留出高度：贴地生成会让直升机/固定翼卡在地形里起不来。
                // 地面载具照旧贴地。
                if (car.IsAircraft)
                {
                    car.Position = new Vector3(pos.X, pos.Y, pos.Z + 12.0f);
                }
                else
                {
                    car.PlaceOnGround();
                }

                car.IsEngineRunning = true;

                _ped.Task.WarpIntoVehicle(car, VehicleSeat.Driver);

                Function.Call(Hash.SET_DRIVER_ABILITY, _ped, 1.0f);
                Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _ped, 0.5f);

                _escortVehicle = car;
                _escortDirectDrive = false;   // 新车重新从"AI 驾驶"开始试
                _escortStallSince = 0;
                _lastEscortPos = null;
                _state = BuddyState.InVehicle;
                _nextEscortTask = 0;

                Notify("~g~队友 " + (_squadIndex + 1) + " 号已就车，开始跟随");
                BuddyLog.Info("escort: agent " + _squadIndex + " got vehicle at slot " + slot);
            }
            catch (Exception ex)
            {
                _escortPending = false;
                _escortMode = false;
                BuddyLog.Error("UpdateEscortSpawn failed", ex);
            }
        }

        /// <summary>Stops the companion's own-car escort and removes that car.</summary>
        internal void StopVehicleEscort()
        {
            try
            {
                _escortMode = false;
                _escortPending = false;      // 取消还没生成完的座驾请求
                _escortDirectDrive = false;
                _escortStallSince = 0;
                _lastEscortPos = null;
                _nextEscortTask = 0;

                // 解除"必须待在车里"，恢复正常的 AI 反应
                ReleasePedVehicleLock();

                if (_escortVehicle != null && _escortVehicle.Exists())
                {
                    if (_ped != null && _ped.Exists() && _ped.IsInVehicle())
                    {
                        _ped.Task.LeaveVehicle(LeaveVehicleFlags.None);
                    }
                    _escortVehicle.Delete();
                }
                _escortVehicle = null;
                _state = BuddyState.Follow;
                Notify("~y~队友已停止驾驶跟随");
            }
            catch (Exception ex) { BuddyLog.Error("StopVehicleEscort failed", ex); }
        }

        /// <summary>
        /// 驾驶跟随的标志位。
        ///
        /// 关键是这两个（默认的 Normal=786603 里**没有**它们，这正是车不走道路的原因）：
        ///
        ///   PreferNavmeshRoute              —— 优先走道路网路径
        ///   UseWanderFallbackInsteadOfStraightLine
        ///                                   —— 寻路失败时沿路绕行，而不是朝目标直冲
        ///
        /// 其余位保持"遵守交通、避让行人车辆"的常规行为。
        /// </summary>
        private const VehicleDrivingFlags EscortDrivingFlags =
            VehicleDrivingFlags.DrivingModeStopForVehicles
            | VehicleDrivingFlags.PreferNavmeshRoute
            | VehicleDrivingFlags.UseWanderFallbackInsteadOfStraightLine;

        /// <summary>
        /// Per-tick upkeep for escort mode.
        ///
        /// 用 Task.VehicleFollow —— SHVDN 对 TASK_VEHICLE_ESCORT/跟随 的封装，
        /// 是"开这辆车跟着那个实体"的专用任务，比手搓 DRIVE_TO_COORD 可靠得多：
        /// 前者由引擎自己维持跟随关系，后者需要我反复重发坐标，一旦重发节奏不对
        /// 车就会停在原地。
        /// </summary>
        private void UpdateVehicleEscort(Ped player)
        {
            if (!_escortMode && !_escortPending) return;

            try
            {
                // 诊断：定期把 escort 的关键状态写进日志。
                // 这个功能反复出问题却看不到内部状态，所以留一条明确的观测线。
                if (Game.GameTime >= _nextEscortDiag)
                {
                    _nextEscortDiag = Game.GameTime + 2000;
                    bool inVeh = _ped != null && _ped.Exists() && _ped.IsInVehicle();
                    bool inMyCar = inVeh && _ped.CurrentVehicle == _escortVehicle;
                    BuddyLog.Info("escort[" + _squadIndex + "] pending=" + _escortPending +
                                  " escortVeh=" + (_escortVehicle != null && _escortVehicle.Exists()) +
                                  " pedInVeh=" + inVeh + " inMyCar=" + inMyCar +
                                  " speed=" + (_escortVehicle != null && _escortVehicle.Exists()
                                               ? _escortVehicle.Speed.ToString("0.0") : "n/a"));
                }

                // 先处理"座驾还没生成"的状态：模型加载完才创建车辆
                if (_escortPending)
                {
                    UpdateEscortSpawn(player);
                    return;
                }

                if (_escortVehicle == null || !_escortVehicle.Exists() || _escortVehicle.IsDead)
                {
                    _escortMode = false;
                    _escortVehicle = null;
                    return;
                }

                // Nothing to do while the player is riding with us.
                if (player.IsInVehicle() && player.CurrentVehicle == _escortVehicle) return;

                // Make sure the buddy is actually behind the wheel; it can be dragged out.
                if (!_ped.IsInVehicle() || _ped.CurrentVehicle != _escortVehicle)
                {
                    if (Game.GameTime - _enterVehicleIssuedAt > 2000)
                    {
                        _enterVehicleIssuedAt = Game.GameTime;

                        // 直接用 SET_PED_INTO_VEHICLE：这是即时生效的放置，
                        // 比给一个 Task 更可靠（Task 可能被其它任务排队挡住）。
                        Function.Call(Hash.SET_PED_INTO_VEHICLE, _ped, _escortVehicle, (int)VehicleSeat.Driver);

                        BuddyLog.Info("escort[" + _squadIndex + "]: re-seated driver by native call");
                    }
                    return;
                }

                float distance = _escortVehicle.Position.DistanceTo(player.Position);

                // ---- 任务是否仍然存在（**不是**"是否正在执行"）------------------
                //
                // 这是本功能之前一直失败的核心原因，日志给出了铁证：
                //
                //   issued 行里的 status 分布：Dormant x1271 / Performing x664 / Vacant x17
                //
                //   Dormant 表示"任务已分配但尚未激活"，对载具任务来说是很常见的中间态。
                //   我原来只把 Performing / WaitingToStart 当作"任务在跑"，于是 Dormant 时
                //   判定为"没任务" -> 重发 -> **每次重发都替换掉原任务** -> 永远停在 Dormant。
                //   实测因此重发了 1952 次，车速有一半时间是 0。
                //
                // 正确判定：Performing / WaitingToStart / Dormant 都算"任务已存在，别动它"。
                ScriptTaskStatus status = _ped.CurrentScriptTaskStatus;
                bool taskPresent = status == ScriptTaskStatus.Performing ||
                                   status == ScriptTaskStatus.WaitingToStart ||
                                   status == ScriptTaskStatus.Dormant;

                // 位移追踪：用来判断"任务在、车却没动"（卡住）这种例外情况。
                Vector3 now = _escortVehicle.Position;
                float moved = _lastEscortPos.HasValue ? now.DistanceTo(_lastEscortPos.Value) : 0f;
                _lastEscortPos = now;

                bool stalled = _escortVehicle.Speed < 1.0f && distance > 15.0f;

                // ---- 硬参数：玩家跑远了就把队友连车一起传送过来 ------------------
                //
                // 用户明确要求的兜底，也是最有保证的做法：与其让 AI 或直接控制在几十米外
                // 挣扎（还会互相堆积），不如超过阈值就干脆把人车一起挪到玩家附近的路面上。
                //
                // 两个防抖条件，解决"一直在不断传送"：
                //   1. 超出阈值后还要**持续 3 秒**才真的传送（玩家高速掠过不误触发）
                //   2. 传送后**冷却 12 秒**，让驾驶 AI 有机会自己追上
                //      没有冷却的话，传送后玩家仍在移动，距离很快又超阈值，
                //      就会反复传送 —— 表现就是队友在你身边闪来闪去。
                if (distance > BuddyConfig.EscortTeleportBeyond)
                {
                    if (_escortFarSince == 0) _escortFarSince = Game.GameTime;

                    bool cooledDown = Game.GameTime - _lastEscortTeleport >=
                                      BuddyConfig.EscortTeleportCooldownMs;
                    bool sustained = Game.GameTime - _escortFarSince >=
                                     BuddyConfig.EscortTeleportDelayMs;

                    if (cooledDown && sustained)
                    {
                        TeleportEscortNear(player, distance);
                        _lastEscortTeleport = Game.GameTime;
                        _escortFarSince = 0;
                        return;
                    }
                }
                else
                {
                    _escortFarSince = 0;
                }

                // ---- 卡住检测 → 自动切到"直接驾驶" ----------------------------
                //
                // 引擎的驾驶 AI 对**履带车/特种载具**（坦克 Rhino、Khanjali、APC、
                // HalfTrack 等）经常拒绝正常执行：任务下发成功、状态也正常，但车就是不动。
                //
                // 这类车用任何 TASK_VEHICLE_* 都救不回来，所以兜底：持续不动超过 5 秒就
                // 改用**直接控制**（自己给前进速度和转向），完全绕过引擎驾驶 AI。
                if (stalled)
                {
                    if (_escortStallSince == 0) _escortStallSince = Game.GameTime;

                    // ---- 卡住的处理阶梯：**全部用引擎自带的任务/机制** ----------
                    //
                    // 用户质疑得对："为什么 AI 失控时要切换自动驾驶，而不是用引擎内的
                    // 车辆跟随函数和算法？"
                    //
                    // 直接驾驶（自己写 ForwardSpeed + Heading）绕过了引擎全部的碰撞规避、
                    // 路网与物理约束 —— 那正是"车辆堆积成罗汉塔"的根源。
                    // 所以默认路径不再用它，改成在引擎的任务之间换挡：
                    //
                    //   阶梯 1（4 秒）：换成 TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE
                    //                   —— 引擎的另一套长距离寻路（专为远距离赶路设计）
                    //   阶梯 2（10 秒）：清空任务让引擎重新规划
                    //   再不行：交给引擎的小队传送（SET_PED_CAN_TELEPORT_TO_GROUP_LEADER）
                    //           和 5 公里硬传送兜底
                    //
                    // 直接驾驶只保留给"变形金刚模式"这个特色功能。
                    int stalledMs = Game.GameTime - _escortStallSince;

                    if (!_escortLongRange && stalledMs > 4000)
                    {
                        _escortLongRange = true;
                        BuddyLog.Info("escort[" + _squadIndex + "]: stalled 4s, switching to " +
                                      "DRIVE_TO_COORD_LONGRANGE (model=" +
                                      _escortVehicle.Model.Hash + ", class=" +
                                      _escortVehicle.ClassType + ")");
                    }

                    if (_escortLongRange && stalledMs > 10000 && !_escortClearedOnce)
                    {
                        _escortClearedOnce = true;
                        try { Function.Call(Hash.CLEAR_PED_TASKS, _ped); } catch (Exception) { }
                        BuddyLog.Info("escort[" + _squadIndex + "]: still stalled 10s, " +
                                      "cleared tasks to let the engine re-plan");
                    }
                }
                else
                {
                    _escortStallSince = 0;
                }

                // 变形金刚模式（特色功能）：唯一的直接驾驶路径，默认关闭
                if (TransformerMode && _escortDirectDrive)
                {
                    DirectDriveToward(player, distance);
                    return;
                }

                // 任务已存在、而且车没卡住 —— 什么都不要做，别去打断它
                if (taskPresent && !stalled) return;

                // 卡住时给它一段观察期再重发，避免频繁重置
                if (taskPresent && stalled && Game.GameTime < _nextEscortTask) return;

                // ---- 阶梯 1 生效中：用引擎的长距离寻路任务 ----------------------
                if (_escortLongRange && !_escortVehicle.IsHelicopter && !_escortVehicle.IsPlane)
                {
                    _nextEscortTask = Game.GameTime + BuddyConfig.NavmeshReissueMs;
                    Vector3 lr = player.Position;
                    Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                                  _ped, _escortVehicle,
                                  lr.X, lr.Y, lr.Z,
                                  distance > 200.0f ? 45.0f : 28.0f,
                                  (int)EscortDrivingFlags,
                                  distance > 100.0f ? 25.0f : 12.0f);

                    if (Game.GameTime >= _nextEscortDiag)
                    {
                        _nextEscortDiag = Game.GameTime + 3000;
                        BuddyLog.Info("escort[" + _squadIndex + "]: issued LONGRANGE  dist=" +
                                      distance.ToString("0.0") + " vehSpeed=" +
                                      _escortVehicle.Speed.ToString("0.0"));
                    }
                    return;
                }

                // ---- 按载具类型选用对应的任务原生 ------------------------------
                //
                // 直升机 / 固定翼用引擎的**持续型追击任务**，下发一次由引擎自己维持。
                // 地面载具改用 TASK_VEHICLE_GOTO_NAVMESH（用户建议，确实是更合适的选择）：
                //
                //   * 它走 **navmesh 寻路**，不是朝目标点走直线；
                //   * 目标点可以直接给**玩家的实时坐标**，
                //     而 TASK_VEHICLE_ESCORT 要求目标必须是"载具"（玩家步行时用不了）；
                //   * 点任务的代价是必须按节奏重发来更新目标点 —— 这本来就是
                //     "用点任务跟随移动实体"的正确用法。
                //     之前 1952 次重发的毛病是**每帧**重发；现在是固定间隔，性质不同。
                if (_escortVehicle.IsHelicopter)
                {
                    if (taskPresent) return;      // 持续型任务，已在下发就别动它
                    Function.Call(Hash.TASK_HELI_CHASE, _ped, player, 0.0f, -18.0f, 12.0f);
                    BuddyLog.Info("escort[" + _squadIndex + "]: issued HELI_CHASE");
                    return;
                }

                if (_escortVehicle.IsPlane)
                {
                    if (taskPresent) return;
                    Function.Call(Hash.TASK_PLANE_CHASE, _ped, player, 0.0f, -40.0f, 25.0f);
                    BuddyLog.Info("escort[" + _squadIndex + "]: issued PLANE_CHASE");
                    return;
                }

                // ---- 地面载具：GOTO_NAVMESH，按固定节奏更新目标点 ----
                if (Game.GameTime < _nextEscortTask) return;
                _nextEscortTask = Game.GameTime + BuddyConfig.NavmeshReissueMs;

                float cruise = distance > 45.0f ? 35.0f : (_squadIndex == 0 ? 24.0f : 20.0f);
                float stopRange = distance > 45.0f ? 18.0f : 12.0f;

                // 目标点：玩家当前位置。不再用"编队偏移位" ——
                // 偏移位在玩家移动时经常落在路边/建筑/水里，navmesh 找不到路就停住了。
                // 保持队形交给引擎的 SET_GROUP_SEPARATION_RANGE 处理。
                Vector3 dest = player.Position;

                Function.Call(Hash.TASK_VEHICLE_GOTO_NAVMESH,
                              _ped, _escortVehicle,
                              dest.X, dest.Y, dest.Z,
                              cruise,
                              (int)EscortDrivingFlags,
                              stopRange);

                BuddyLog.Info("escort[" + _squadIndex + "]: issued GOTO_NAVMESH  dist=" +
                              distance.ToString("0.0") + " cruise=" + cruise.ToString("0.0") +
                              " stopRange=" + stopRange.ToString("0.0") +
                              " vehSpeed=" + _escortVehicle.Speed.ToString("0.0") +
                              " status=" + status);
            }
            catch (Exception ex) { BuddyLog.Error("UpdateVehicleEscort failed", ex); }
        }

        // ==================================================================
        // 直接驾驶兜底
        //
        // 引擎的驾驶 AI 对履带/特种载具（坦克 Rhino、Khanjali、APC、HalfTrack 等）
        // 经常拒绝执行：任务下发成功、状态是 Performing，车却一动不动。
        // 这种情况下任何 TASK_VEHICLE_* 都救不回来，只能自己控制。
        //
        // 做法：每 tick 让车头朝玩家方向转一个小角度，并直接设前进速度。
        // 不依赖任何驾驶 AI，任何有 ForwardSpeed 的地面载具都能用。
        // ==================================================================

        private void DirectDriveToward(Ped player, float distance)
        {
            try
            {
                Vehicle car = _escortVehicle;
                if (car == null || !car.Exists()) return;

                // 飞行器不走这条路：它们由 HELI_CHASE / PLANE_CHASE 负责，
                // 直接设 ForwardSpeed 会让它们掉下来。
                if (car.IsAircraft) return;

                // 足够近就停住，别再推着车往玩家身上撞
                if (distance < 12.0f)
                {
                    car.ForwardSpeed = 0.0f;
                    return;
                }

                // ---- 变形金刚模式：保留最初的粗暴推法（会叠罗汉，作为特色保留）----
                if (TransformerMode)
                {
                    Vector3 tpos = car.Position;
                    float tdesired = HeadingTo(tpos, player.Position);
                    float tdelta = NormalizeAngle(tdesired - car.Heading);
                    const float tTurn = 2.5f;
                    if (tdelta > tTurn) tdelta = tTurn;
                    if (tdelta < -tTurn) tdelta = -tTurn;
                    car.Heading = Normalize360(car.Heading + tdelta);

                    car.ForwardSpeed = distance > 60.0f ? 30.0f : (distance > 25.0f ? 18.0f : 10.0f);

                    if (Game.GameTime >= _nextEscortDiag)
                    {
                        _nextEscortDiag = Game.GameTime + 2000;
                        BuddyLog.Info("escort[" + _squadIndex + "]: TRANSFORMER push  dist=" +
                                      distance.ToString("0.0") + " vehSpeed=" +
                                      car.Speed.ToString("0.0"));
                    }
                    return;
                }

                // ---- 避障：前方有别的车/人就刹车 ----------------------------
                //
                // 这是"叠罗汉/堆积"的直接成因：直接设 ForwardSpeed 绕过了引擎的碰撞规避，
                // 几辆车互相顶就爬到一起了。前方有障碍时停下来即可根治。
                float keepClear = 7.0f + (_squadIndex * 1.5f);
                if (IsBlockedAhead(car, keepClear))
                {
                    car.ForwardSpeed = 0.0f;
                    return;
                }

                // ---- 驶向**自己的编队位**，而不是玩家所在的那一点 ----------------
                //
                // 这是解决堆积的关键。原来所有车都朝玩家的位置开，必然挤在同一点、
                // 互相顶、最后叠起来。改成每个队友有一个专属的停车位：
                // 玩家正后方，距离按编号递增，横向三列错开，于是自然排成队列。
                //
                //   0 号：玩家后方 12 米，中间
                //   1 号：玩家后方 19 米，偏左
                //   2 号：玩家后方 26 米，偏右
                //   ...
                float slotBack = BuddyConfig.ConvoySlotBase + (_squadIndex * BuddyConfig.ConvoySlotStep);
                int lane = (_squadIndex % 3) - 1;              // -1 / 0 / +1
                Vector3 slot = player.Position
                             - player.ForwardVector * slotBack
                             + player.RightVector * (lane * 3.5f);

                // 已经停进自己的位子就不再推它
                if (car.Position.DistanceTo(slot) < BuddyConfig.ConvoySlotArrive)
                {
                    car.ForwardSpeed = 0.0f;
                    return;
                }

                // 目标点吸附到路面，避免朝野地/建筑开
                Vector3 roadTarget = SnapToRoad(slot);

                float desired = HeadingTo(car.Position, roadTarget);
                float delta = NormalizeAngle(desired - car.Heading);

                // 每次最多转 4°，且不会转过头
                float maxTurn = Math.Min(4.0f, Math.Abs(delta));
                if (delta != 0f) delta = Math.Sign(delta) * maxTurn;
                car.Heading = Normalize360(car.Heading + delta);

                // ---- 前进：离自己的位子越远开得越快；大角度转向时减速，避免原地打转 ----
                float toSlot = car.Position.DistanceTo(slot);
                float speed = toSlot > 60.0f ? 28.0f : (toSlot > 25.0f ? 16.0f : 9.0f);
                if (Math.Abs(delta) > 45.0f) speed *= 0.35f;
                car.ForwardSpeed = speed;

                if (Game.GameTime >= _nextEscortDiag)
                {
                    _nextEscortDiag = Game.GameTime + 2000;
                    BuddyLog.Info("escort[" + _squadIndex + "]: DIRECT DRIVE to slot  slotDist=" +
                                  toSlot.ToString("0.0") + " playerDist=" +
                                  distance.ToString("0.0") + " speed=" + speed.ToString("0.0") +
                                  " vehSpeed=" + car.Speed.ToString("0.0"));
                }
            }
            catch (Exception ex) { BuddyLog.Error("DirectDriveToward failed", ex); }
        }

        /// <summary>
        /// 前方是否有障碍（别的载具或行人）。用于直接驾驶时的刹车判断，
        /// 这是防止几辆车互相攀爬叠起来的关键。
        ///
        /// 用 GetNearbyEntities 而不是 GetNearbyVehicles/GetNearbyPeds：
        /// 后两者要多传一个 Model[] 参数，我之前传了 null，很可能每次都抛异常
        /// （被 catch 吞掉后一律返回 false）——**避障就从来没有生效过**，
        /// 这正是车辆堆积的直接原因。GetNearbyEntities 只需要位置和半径。
        /// </summary>
        private static bool IsBlockedAhead(Vehicle car, float lookAhead)
        {
            try
            {
                Vector3 probe = car.Position + car.ForwardVector * lookAhead;
                Ped me = Game.Player.Character;

                foreach (Entity e in World.GetNearbyEntities(probe, 5.0f))
                {
                    if (e == null || !e.Exists()) continue;

                    var v = e as Vehicle;
                    if (v != null)
                    {
                        if (v != car) return true;
                        continue;
                    }

                    var p = e as Ped;
                    if (p != null)
                    {
                        if (me != null && p == me) continue;   // 玩家不算障碍
                        return true;
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// 玩家与本车之间、靠近玩家的一个点。直接朝玩家开会穿野地撞墙，
        /// 用这个点再吸附到道路节点，就能沿路接近。
        /// </summary>
        private static Vector3 ApproachPoint(Vector3 carPos, Vector3 playerPos, float backOff)
        {
            Vector3 dir = carPos - playerPos;
            float len = dir.Length();
            if (len < 0.5f) return playerPos;
            dir = dir * (1.0f / len);
            return playerPos + dir * backOff;
        }

        /// <summary>把一个点吸附到最近的道路节点；失败时返回原点。</summary>
        private static Vector3 SnapToRoad(Vector3 point)
        {
            try
            {
                var outPos = new OutputArgument();
                bool ok = Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE,
                                              point.X, point.Y, point.Z,
                                              outPos,
                                              1,        // 1 = 只考虑道路节点
                                              3.0f,     // 搜索半径
                                              0f);
                if (ok)
                {
                    Vector3 node = outPos.GetResult<Vector3>();
                    // 道路节点有时会吸到高架/地下，Z 差太多就放弃吸附
                    if (Math.Abs(node.Z - point.Z) < 8.0f) return node;
                }
            }
            catch (Exception) { }
            return point;
        }

        /// <summary>
        /// 找当前应该打的敌人。
        ///
        /// 这里必须**主动扫描**，不能只靠缓存：
        ///
        ///   原实现只在玩家 IsInCombat 时去查，或者用 _threat 字段。
        ///   但 escort（开车跟随）模式**跳过了整个状态机**，而 _threat 正是在状态机的
        ///   战斗分支里被赋值的 —— 于是开车跟随期间 _threat 永远是 null。
        ///   日志证据：vehicle combat / engaging 出现 **0 次**，战斗从未触发过。
        ///
        /// 所以改成三级查找：玩家交战的敌人 -> 已缓存威胁 -> **主动扫描附近敌对 ped**。
        /// </summary>
        private Ped FindCombatTarget(Ped player)
        {
            // 1. 玩家正在交战的敌人
            try
            {
                if (player != null && player.Exists() && player.IsInCombat)
                {
                    Ped t = FindThreat(player);
                    if (t != null && t.Exists() && !t.IsDead) return t;
                }
            }
            catch (Exception) { }

            // 2. 队友自己已经锁定的威胁
            try
            {
                if (_threat != null && _threat.Exists() && !_threat.IsDead) return _threat;
            }
            catch (Exception) { }

            // 3. 主动扫描：附近有没有和我们敌对的 ped
            return ScanForHostileNearby(player);
        }

        /// <summary>
        /// 主动扫描附近的敌对 ped。
        ///
        /// 判据（满足其一即视为敌人）：
        ///   * 它正在开火 / 处于战斗状态
        ///   * 它和我们这一位的关系是 Dislike(4) 或 Hate(5)
        ///
        /// 跳过友军：玩家本人、队友、以及同小队成员。
        /// </summary>
        private Ped ScanForHostileNearby(Ped player)
        {
            try
            {
                Vector3 origin = (player != null && player.Exists())
                    ? player.Position
                    : (_ped != null && _ped.Exists() ? _ped.Position : Vector3.Zero);

                if (origin == Vector3.Zero) return null;

                foreach (Ped p in World.GetNearbyPeds(origin, BuddyConfig.VehicleCombatScanRadius))
                {
                    if (p == null || !p.Exists() || p.IsDead) continue;
                    if (player != null && p == player) continue;
                    if (p == _ped) continue;

                    // 跳过同小队成员
                    try
                    {
                        if (Function.Call<bool>(Hash.IS_PED_GROUP_MEMBER, p,
                                                Function.Call<int>(Hash.GET_PED_GROUP_INDEX, player)))
                            continue;
                    }
                    catch (Exception) { }

                    bool hostile = false;

                    try
                    {
                        int rel = Function.Call<int>(Hash.GET_RELATIONSHIP_BETWEEN_PEDS, _ped, p);
                        if (rel == 4 || rel == 5) hostile = true;      // Dislike / Hate
                    }
                    catch (Exception) { }

                    if (!hostile)
                    {
                        try
                        {
                            if (p.IsInCombat || p.IsShooting) hostile = true;
                        }
                        catch (Exception) { }
                    }

                    if (hostile)
                    {
                        // 记到 _threat 上，后续帧就能直接命中缓存
                        _threat = p;
                        return p;
                    }
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// 乘车战斗：任何坐在车里的队友（**驾驶位和乘客位都算**）对敌人还击。
        ///
        /// 这是"车队行驶中无法攻击敌人"的修复。之前有两处问题：
        ///
        ///   1. KeepPedInVehicle 里设了 SET_BLOCKING_OF_NON_TEMPORARY_EVENTS，
        ///      那条原生会屏蔽 ped 对"临时事件"（包括被攻击）的反应 —— 队友对警察
        ///      和攻击者完全视而不见。已移除。
        ///
        ///   2. 只有驾驶位跑战斗逻辑，**乘客完全不会还手**。
        ///      现在乘客用 TASK_DRIVE_BY（引擎自带的探身射击任务）。
        ///
        /// 载具类型决定开火方式：
        ///   * 有车载武器（坦克炮、APC 机枪）-> SET_VEHICLE_SHOOT_AT_TARGET 让炮塔瞄准
        ///   * 普通载具 -> 驾驶位用 TASK_VEHICLE_SHOOT_AT_PED；
        ///                乘客用 TASK_DRIVE_BY 探身射击
        /// </summary>
        private void UpdateVehicleCombat(Ped player)
        {
            if (Game.GameTime < _nextEscortCombat) return;
            _nextEscortCombat = Game.GameTime + 400;

            try
            {
                if (_ped == null || !_ped.Exists() || !_ped.IsInVehicle()) return;

                Ped enemy = FindCombatTarget(player);
                if (enemy == null)
                {
                    _escortEngaging = false;
                    return;
                }

                Vehicle car = _ped.CurrentVehicle;
                if (car == null || !car.Exists()) return;

                // 判断这一位是不是驾驶位
                Ped driver = null;
                try { driver = car.Driver; } catch (Exception) { }
                bool isDriver = driver != null && driver.Exists() && driver == _ped;

                // 车载武器判定（坦克炮、APC 机枪等）
                bool hasVehicleWeapon = false;
                try
                {
                    var weaponOut = new OutputArgument();
                    hasVehicleWeapon = Function.Call<bool>(Hash.GET_CURRENT_PED_VEHICLE_WEAPON,
                                                           _ped, weaponOut);
                }
                catch (Exception) { }

                if (isDriver)
                {
                    if (hasVehicleWeapon)
                    {
                        // 炮塔/车载武器直接瞄准目标
                        Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, _ped, enemy, 0f, 0f, 0f);
                    }

                    // 驾驶位：用车上武器打目标（引擎会处理成驾驶位射击）
                    Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, _ped, enemy, 1.0f);
                }
                else
                {
                    // 乘客：探身射击（引擎自带的 drive-by 任务）
                    //   p3..p5 = 瞄准偏移（0 = 直接瞄目标）
                    //   p7     = 使用的武器（0 = 当前武器）
                    //   p8     = true 表示同时自己瞄准
                    Function.Call(Hash.TASK_DRIVE_BY,
                                  _ped, enemy,
                                  0, 0f, 0f, 0f,
                                  0, true,
                                  0);
                }

                if (!_escortEngaging)
                {
                    _escortEngaging = true;
                    BuddyLog.Info("vehicle combat: agent " + _squadIndex +
                                  (isDriver ? " (driver)" : " (passenger)") +
                                  " engaging target=" + enemy.Handle +
                                  " vehicleWeapon=" + hasVehicleWeapon);
                }
            }
            catch (Exception ex) { BuddyLog.Error("UpdateVehicleCombat failed", ex); }
        }

        /// <summary>
        /// 特种载具内开火。
        ///
        /// 场景：玩家遭到攻击时，坐在坦克/APC 里的队友应该用车载武器还击，
        /// 而不是干坐着。判定分两种：
        ///
        ///   * 载具有车载武器（坦克的炮、APC 的重机枪等）
        ///       -> SET_VEHICLE_SHOOT_AT_TARGET + TASK_VEHICLE_SHOOT_AT_PED，
        ///          让炮塔/车载武器瞄准并射击
        ///   * 普通载具（只能探身射击）
        ///       -> 同样用 TASK_VEHICLE_SHOOT_AT_PED，引擎会处理成驾驶位射击
        ///
        /// 触发条件：玩家进入战斗，或队友自己已经锁定了敌人。
        /// </summary>
        private void UpdateEscortCombat(Ped player)
        {
            // 每 500ms 判一次即可，没必要每帧查
            if (Game.GameTime < _nextEscortCombat) return;
            _nextEscortCombat = Game.GameTime + 500;

            try
            {
                if (_ped == null || !_ped.Exists() || !_ped.IsInVehicle()) return;

                // 用统一的查找（含**主动扫描**）—— 原来只看 player.IsInCombat 和 _threat，
                // 而 escort 模式下 _threat 永远是 null，所以战斗从未触发。
                Ped enemy = FindCombatTarget(player);

                if (enemy == null || !enemy.Exists() || enemy.IsDead)
                {
                    _escortEngaging = false;
                    return;
                }

                // 车载武器判定
                var weaponOut = new OutputArgument();
                bool hasVehicleWeapon = false;
                try
                {
                    hasVehicleWeapon = Function.Call<bool>(Hash.GET_CURRENT_PED_VEHICLE_WEAPON,
                                                           _ped, weaponOut);
                }
                catch (Exception) { }

                if (hasVehicleWeapon)
                {
                    // 坦克炮塔 / APC 车载武器：直接瞄准目标开火
                    Function.Call(Hash.SET_VEHICLE_SHOOT_AT_TARGET, _ped, enemy, 0f, 0f, 0f);
                }

                // 这一条对两种载具都适用：让驾驶员用车上的武器打目标
                // p2 是射击精度相关的参数，给 1.0 表示尽量瞄准
                Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, _ped, enemy, 1.0f);

                if (!_escortEngaging)
                {
                    _escortEngaging = true;
                    BuddyLog.Info("escort[" + _squadIndex + "]: engaging from vehicle, " +
                                  "vehicleWeapon=" + hasVehicleWeapon +
                                  " target=" + enemy.Handle);
                }
            }
            catch (Exception ex) { BuddyLog.Error("UpdateEscortCombat failed", ex); }
        }

        /// <summary>
        /// 硬兜底：把队友连人带车传送到离玩家约 1.8 公里外的公路上。
        ///
        /// 触发条件是玩家已经跑出 <see cref="BuddyConfig.EscortTeleportBeyond"/> 米（5 公里）。
        ///
        /// 为什么落在 1.8 公里外而不是玩家身边：
        /// 传到眼前会突兀地"冒出来"。落在两公里内的公路上，让 AI 把剩下的路开完，
        /// 观感上就是"队友从后面追上来了"。
        /// </summary>
        private void TeleportEscortNear(Ped player, float distance)
        {
            try
            {
                if (_escortVehicle == null || !_escortVehicle.Exists()) return;

                // 从玩家指向本车的方向，退到目标距离处 —— 这样队友是从"玩家来的方向"
                // 那一侧出现，而不是凭空出现在前方。
                Vector3 away = _escortVehicle.Position - player.Position;
                float len = away.Length();
                if (len < 1.0f) away = -player.ForwardVector;
                else away = away * (1.0f / len);

                float land = BuddyConfig.EscortTeleportLandDistance;
                if (land > BuddyConfig.EscortTeleportMaxDistance)
                    land = BuddyConfig.EscortTeleportMaxDistance;

                Vector3 spot = FindRoadSpot(player.Position, away, land);

                _escortVehicle.Position = spot;
                _escortVehicle.PlaceOnGround();
                _escortVehicle.IsEngineRunning = true;

                // 司机必须留在驾驶位：传送会把 ped 甩出车外，这里一并处理
                Function.Call(Hash.SET_PED_INTO_VEHICLE, _ped, _escortVehicle, (int)VehicleSeat.Driver);

                _lastEscortPos = null;
                _escortStallSince = 0;
                _escortDirectDrive = false;   // 新车位，重新给 AI 一次机会

                BuddyLog.Info("escort[" + _squadIndex + "]: TELEPORTED (was " +
                              distance.ToString("0") + "m away) -> landed " +
                              _escortVehicle.Position.DistanceTo(player.Position).ToString("0") +
                              "m from player");
            }
            catch (Exception ex) { BuddyLog.Error("TeleportEscortNear failed", ex); }
        }

        /// <summary>
        /// 在玩家周围找一个**公路上的落点**，距离约 <paramref name="wanted"/> 米。
        ///
        /// 长距离的位置未必正好压在道路上，所以从目标距离开始逐步往玩家方向收，
        /// 直到吸附到道路节点为止 —— 避免把车传进荒山、楼顶或水里。
        /// </summary>
        private static Vector3 FindRoadSpot(Vector3 origin, Vector3 awayDir, float wanted)
        {
            // 先试目标距离，不行就按 80% 递减，最多试 8 次（收到约 0.17 倍）
            float d = wanted;
            Vector3 fallback = origin + awayDir * wanted;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 probe = origin + awayDir * d;

                try
                {
                    var outPos = new OutputArgument();
                    bool ok = Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE,
                                                  probe.X, probe.Y, probe.Z,
                                                  outPos, 1, 60.0f, 0f);
                    if (ok)
                    {
                        Vector3 node = outPos.GetResult<Vector3>();
                        // 道路节点有时吸到高架/地下，Z 差太多就当成没找到
                        if (Math.Abs(node.Z - probe.Z) < 25.0f && node.DistanceTo(origin) <= BuddyConfig.EscortTeleportMaxDistance)
                            return node;
                    }
                }
                catch (Exception) { }

                fallback = probe;      // 记下最后一个位置，作为兜底
                d *= 0.8f;
            }

            return fallback;
        }

        /// <summary>
        /// 世界坐标下的朝向角。GTA 约定：0=北，逆时针增大（90=西，180=南，270=东）。
        /// 推导：朝北 (dx=0, dy=1) 应为 0；朝西 (dx=-1, dy=0) 应为 90。
        /// 因此 heading = atan2(-dx, dy)。
        /// </summary>
        private static float HeadingTo(Vector3 from, Vector3 to)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            double deg = Math.Atan2(-dx, dy) * 180.0 / Math.PI;
            return Normalize360((float)deg);
        }

        private static float Normalize360(float deg)
        {
            deg %= 360f;
            if (deg < 0f) deg += 360f;
            return deg;
        }

        /// <summary>把角度差归一到 [-180, 180]，用于取最短转向方向。</summary>
        private static float NormalizeAngle(float deg)
        {
            deg = Normalize360(deg);
            if (deg > 180f) deg -= 360f;
            return deg;
        }

        // ==================================================================
        // 关系组：多队友之间必须互相友好
        //
        // 这是"队友互相攻击"的根因所在：
        //
        //   每个队友有自己独立的关系组（BUDDY_MOD_GROUP_1、_2 …），这是为了不让引擎
        //   把不同队友当成同一组。但只设置"队友组 ↔ 玩家组"是不够的 ——
        //   在队友 1 眼里，队友 2 属于一个**陌生组**，默认没有友好关系，于是直接开打。
        //
        //   所以每创建一个新组，都要和**已存在的所有队友组**互相建立友好关系。
        //   用一张静态表记录所有队友组，新组创建时补齐两两关系。
        // ==================================================================

        private static readonly List<RelationshipGroup> s_allyGroups = new List<RelationshipGroup>();

        /// <summary>把新建立的队友组与已有的每一个队友组设为互相友好。</summary>
        private static void LinkAllyGroup(RelationshipGroup group)
        {
            try
            {
                // 遍历副本：LinkAllyGroup 之外可能并发注册新组
                RelationshipGroup[] existing = s_allyGroups.ToArray();

                foreach (RelationshipGroup other in existing)
                {
                    if (other.Hash == group.Hash) continue;

                    // RESPECT 双向：既不主动攻击，也不会被对方攻击
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, group, other, "RESPECT");
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, other, group, "RESPECT");

                    // 再显式标记为友好，避免"中立"被某些任务判成可攻击目标
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, group, other, "RESPECT");
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 1, other, group, "RESPECT");
                }

                s_allyGroups.Add(group);
            }
            catch (Exception ex) { BuddyLog.Error("LinkAllyGroup failed", ex); }
        }

        private void EnsureRelationshipGroups(Ped player)
        {
            try
            {
                if (!_groupCreated)
                {
                    // 每个队友用唯一的关系组名，见本段开头的说明。
                    _group = World.AddRelationshipGroup(_relationshipGroupName);
                    _groupCreated = true;
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, _group, _group, "BUDDY");

                    // 关键：与所有已有队友组建立友好关系，否则队友之间会互相攻击
                    LinkAllyGroup(_group);
                }
                if (!_playerGroupCaptured)
                {
                    _playerGroup = player.RelationshipGroup;
                    _playerGroupCaptured = true;
                }
                // The buddy must not be hostile to the player, and vice versa.
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, _group, _playerGroup.Hash, "RESPECT");
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, _playerGroup.Hash, _group.Hash, "RESPECT");

                // 每个新组都要和玩家组也建立一遍（LinkAllyGroup 只管队友之间）
                LinkAllyGroupToPlayer(_playerGroup);
            }
            catch (Exception)
            {
                _groupCreated = false;
            }
        }

        /// <summary>
        /// 确保所有队友组与当前玩家组互相友好。玩家组在游戏过程中可能变化
        /// （例如换角色），所以每次调用都重放一遍关系设置。
        /// </summary>
        private static void LinkAllyGroupToPlayer(RelationshipGroup playerGroup)
        {
            try
            {
                foreach (RelationshipGroup ally in s_allyGroups.ToArray())
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, ally, playerGroup.Hash, "RESPECT");
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 0, playerGroup.Hash, ally, "RESPECT");
                }
            }
            catch (Exception) { }
        }

        // ==================================================================
        // 小队支持
        // ==================================================================

        private string _relationshipGroupName = BuddyConfig.RelationshipGroupName;
        private int _squadIndex;

        /// <summary>
        /// 队伍编号。用于生成唯一的关系组名，并决定谁负责画 HUD。
        /// 由 AgentSquad 在创建实例后立即调用。
        /// </summary>
        internal void SetSquadIdentity(int index)
        {
            _squadIndex = index;
            _relationshipGroupName = BuddyConfig.RelationshipGroupName + "_" + index;
        }

        /// <summary>由 AgentSquad 指定这一位的皮肤（生成前调用）。</summary>
        internal void SetSpawnModel(PedHash hash) { _spawnModel = hash; }

        /// <summary>这一位的 Ped 是否就是 <paramref name="ped"/>（供小队按驾驶员反查队友）。</summary>
        internal bool MatchesPed(Ped ped)
        {
            try { return _ped != null && _ped.Exists() && ped != null && ped.Exists() && _ped == ped; }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// 把"已经坐在某辆车驾驶位"的队友转入开车跟随模式。
        ///
        /// 用于"车队跟随"：队友先按座位坐满车辆（LoadIntoVehicles），
        /// 然后每辆车的驾驶员用这个方法开始跟住玩家。
        /// 与 <see cref="OrderVehicleEscort"/> 的区别是后者会**自己生成一辆车**，
        /// 而这里复用已经生成并坐好的车。
        /// </summary>
        internal void ArmEscortForExistingVehicle(Vehicle vehicle)
        {
            try
            {
                if (!Exists || vehicle == null || !vehicle.Exists()) return;

                _escortVehicle = vehicle;
                _driveTarget = vehicle;
                _escortMode = true;
                _escortPending = false;
                _nextEscortTask = 0;
                _escortStallSince = 0;
                _escortLongRange = false;
                _escortClearedOnce = false;
                _escortDirectDrive = false;
                _lastEscortPos = null;

                _state = BuddyState.InVehicle;

                Function.Call(Hash.SET_DRIVER_ABILITY, _ped, 1.0f);
                Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _ped, 0.5f);

                // 驾驶员要留在驾驶位；乘客由 AgentSquad 的座位强锁维持
                KeepPedInVehicle();
                PrepareVehicleCombat();   // 换单手武器 + 战斗属性，否则车内无法还击

                BuddyLog.Info("escort[" + _squadIndex + "]: armed on existing vehicle model=" +
                              vehicle.Model.Hash);
            }
            catch (Exception ex) { BuddyLog.Error("ArmEscortForExistingVehicle failed", ex); }
        }

        /// <summary>
        /// 这一位被安排的车（自驾跟随的车，或指定座驾）。
        /// 座位强锁用它来把溜下车的队友塞回去。
        /// </summary>
        internal Vehicle AssignedVehicle
        {
            get
            {
                try
                {
                    if (_escortVehicle != null && _escortVehicle.Exists()) return _escortVehicle;
                    if (_driveTarget != null && _driveTarget.Exists()) return _driveTarget;
                }
                catch (Exception) { }
                return null;
            }
        }

        /// <summary>
        /// 座位强锁：把这一位立刻按回它自己的车里。
        ///
        /// 用于"玩家在车上时队友不许下车"。做法是先找它原来的座位，
        /// 座位被占了就找任意空位；找不到空位才退到驾驶位。
        ///
        /// 返回是否成功把它放回车上。
        /// </summary>
        internal bool ForceBackIntoAssignedVehicle()
        {
            try
            {
                if (!Exists) return false;
                if (_ped.IsInVehicle()) return true;      // 已经在车上，不用管

                Vehicle car = AssignedVehicle;
                if (car == null || !car.Exists()) return false;

                // 优先坐回驾驶位（自驾跟随的情况）；否则找第一个空位
                int maxSeat = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, car);

                int chosen = int.MinValue;
                for (int seat = -1; seat <= maxSeat; seat++)
                {
                    bool free;
                    try { free = car.IsSeatFree((VehicleSeat)seat); }
                    catch (Exception) { free = false; }

                    if (free) { chosen = seat; break; }
                }

                if (chosen == int.MinValue) return false;   // 车满了

                Function.Call(Hash.SET_PED_INTO_VEHICLE, _ped, car, chosen);
                KeepPedInVehicle();
                PrepareVehicleCombat();   // 换单手武器 + 战斗属性，否则车内无法还击
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>把这一位直接放进指定座位（用于队伍安排上车）。</summary>
        internal void WarpIntoVehicle(Vehicle vehicle, VehicleSeat seat)
        {
            try
            {
                if (!Exists || vehicle == null || !vehicle.Exists()) return;

                Function.Call(Hash.SET_PED_INTO_VEHICLE, _ped, vehicle, (int)seat);
                _state = BuddyState.InVehicle;

                // 关键：告诉状态机"我这辆车就是安排给我的那辆"。
                //
                // 原来这里只设置了 _escortVehicle，而状态机的 UpdateVehicle 读的是
                // _driveTarget —— 它不知道我们给了队友一辆车，于是会去找/偷别的车，
                // 表现就是"坐进同一辆载具的队友不跟随"。
                _driveTarget = vehicle;

                // 坐驾驶位的人由 escort 逻辑接管驾驶；乘客只要留在车上即可。
                // 注意：**所有座位都要上锁**，不只是驾驶位。
                // 之前只对驾驶位调 KeepPedInVehicle，结果就是"只有驾驶员留在车里，
                // 其他人都跑出去了" —— 因为队友同时是玩家小队成员，小队 AI 会让他们
                // 下车徒步跟随玩家。
                KeepPedInVehicle();
                PrepareVehicleCombat();   // 换单手武器 + 战斗属性，否则车内无法还击

                if (seat == VehicleSeat.Driver)
                {
                    _escortVehicle = vehicle;
                    _escortMode = true;
                    _escortPending = false;
                    _nextEscortTask = 0;
                    Function.Call(Hash.SET_DRIVER_ABILITY, _ped, 1.0f);
                    Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _ped, 0.5f);
                }

                BuddyLog.Info("vehicle: agent " + _squadIndex + " -> seat " + (int)seat +
                              (seat == VehicleSeat.Driver ? " (driver)" : " (passenger)") +
                              " model=" + vehicle.Model.Hash);
            }
            catch (Exception ex) { BuddyLog.Error("WarpIntoVehicle failed", ex); }
        }

        /// <summary>这一位是否正坐在座位上（供队伍统计座位占用）。</summary>
        internal bool IsSeated
        {
            get
            {
                try { return _ped != null && _ped.Exists() && _ped.IsInVehicle(); }
                catch (Exception) { return false; }
            }
        }

        private int _outsideSince;

        /// <summary>
        /// 这一位是否**已经离开载具超过 <paramref name="ms"/> 毫秒**。
        ///
        /// 为什么需要"离开多久"而不是简单的"在不在车上"：
        ///
        ///   座位强锁每 500ms 调一次 SET_PED_INTO_VEHICLE 把人塞回去。
        ///   但那个原生**会清掉 ped 当前的脚本任务** —— 驾驶任务和射击任务
        ///   全部被取消。实测日志里 "re-seated driver" 出现 **802 次**，
        ///   意味着队友的驾驶/战斗任务在那段时间里被反复打断，什么都做不成。
        ///
        /// 加一个宽限期：只在确实离开了一段时间之后才塞回去，
        /// 中间这段时间留给驾驶任务和战斗任务执行。
        /// 真正的保障是配置标志（PreventVehExitDueToInvalidWeapon 等），
        /// 从源头减少他们下车的动机。
        /// </summary>
        internal bool OutsideFor(int ms)
        {
            try
            {
                if (IsSeated)
                {
                    _outsideSince = 0;
                    return false;
                }

                if (_outsideSince == 0)
                {
                    _outsideSince = Game.GameTime;
                    return false;
                }

                return Game.GameTime - _outsideSince >= ms;
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// 让这一位老老实实待在车里。
        ///
        /// 实测队友会自己跑下车（日志里 175 次 "re-seated driver"）：原因是他们同时
        /// 是玩家小队成员，**小队 AI 会让他们下车徒步跟随玩家**，和我们的驾驶任务打架。
        ///
        /// 用一层配置标志把"下车"这条路堵住：
        ///
        ///   DontLeaveVehicleIfLeaderNotInVehicle : 队长不在车上时不许下车（关键的一条）
        ///   DontDragMeOutCar / StayInCarOnJack   : 不被拽下车
        ///
        /// **刻意不设 SET_BLOCKING_OF_NON_TEMPORARY_EVENTS。**
        /// 之前设了它，结果队友对警察和攻击者完全视而不见 —— 那条原生会屏蔽 ped
        /// 对"临时事件"（包括被攻击）的反应。让队友留在车里的真正保障是 AgentSquad
        /// 的座位强锁（每 500ms 把溜下车的人塞回去），不需要牺牲战斗反应能力。
        /// </summary>
        private void KeepPedInVehicle()
        {
            try
            {
                if (_ped == null || !_ped.Exists()) return;

                Function.Call(Hash.SET_PED_CAN_BE_DRAGGED_OUT, _ped, false);
                Function.Call(Hash.SET_PED_STAY_IN_VEHICLE_WHEN_JACKED, _ped, true);

                _ped.SetConfigFlag(PedConfigFlagToggles.DontLeaveVehicleIfLeaderNotInVehicle, true);
                _ped.SetConfigFlag(PedConfigFlagToggles.DontDragMeOutCar, true);
                _ped.SetConfigFlag(PedConfigFlagToggles.StayInCarOnJack, true);

                // ---- 针对"队友疯狂跳车"的两条关键标志 ----------------------------
                //
                // 实测日志里 re-seated driver 出现了 **802 次** —— 队友在拼命下车。
                //
                // 最大嫌疑是 PreventVehExitDueToInvalidWeapon：
                // 如果队友身上是**长武器**（步枪、狙击枪等），这类武器在 GTA 里
                // **不能在车内使用**，引擎会因此**强制他下车去开枪**；
                // 我的座位锁又把他塞回去 -> 死循环。
                //
                // 这两条标志从源头堵住：不许因为武器不合适而下车，也不许跟着队长跳车。
                _ped.SetConfigFlag(PedConfigFlagToggles.PreventVehExitDueToInvalidWeapon, true);
                _ped.SetConfigFlag(PedConfigFlagToggles.DisableJumpingFromVehiclesAfterLeader, true);

                // 明确允许车载战斗（这条若为 true 会彻底禁掉车内的还击能力）
                _ped.SetConfigFlag(PedConfigFlagToggles.DisableVehicleCombat, false);
                _ped.SetConfigFlag(PedConfigFlagToggles.CanBeShotInVehicle, true);
            }
            catch (Exception ex) { BuddyLog.Error("KeepPedInVehicle failed", ex); }
        }

        /// <summary>
        /// 乘车战斗的准备：换成**单手武器** + 打开战斗属性。
        ///
        /// 为什么必须换单手武器：
        ///   在 GTA 里**只有单手武器能在车内使用**（微型冲锋枪、手枪、AP 手枪、
        ///   短管霰弹枪）。队友如果拿着步枪/狙击枪，引擎会判定"武器不能在车内使用"，
        ///   于是强制他下车 —— 这正是队友反复跳车、且在车里一枪不放的根本原因。
        ///
        /// 战斗属性让引擎自己的战斗 AI 能接管射击，不需要我每帧下发射击任务
        /// （驾驶任务和射击任务共用同一个脚本任务槽位，互相取消）。
        /// </summary>
        private void PrepareVehicleCombat()
        {
            try
            {
                if (_ped == null || !_ped.Exists()) return;

                // ---- 1. 换单手武器（能在车内使用）----
                Hash current = Function.Call<Hash>(Hash.GET_BEST_PED_WEAPON, _ped, false);
                bool alreadyOneHanded = current == (Hash)(uint)WeaponHash.MicroSMG ||
                                        current == (Hash)(uint)WeaponHash.APPistol ||
                                        current == (Hash)(uint)WeaponHash.Pistol ||
                                        current == (Hash)(uint)WeaponHash.CombatPistol;

                if (!alreadyOneHanded)
                {
                    Function.Call(Hash.GIVE_WEAPON_TO_PED, _ped,
                                  (Hash)(uint)WeaponHash.MicroSMG, 9999, false, true);
                    Function.Call(Hash.SET_PED_INFINITE_AMMO, _ped, true,
                                  (Hash)(uint)WeaponHash.MicroSMG);
                    BuddyLog.Info("escort[" + _squadIndex +
                                  "]: switched to a one-handed weapon for in-vehicle use");
                }

                // ---- 2. 战斗属性：让引擎的战斗 AI 能接管 ----
                //   5  = AlwaysFight（见到敌人就打，不等被攻击）
                //   46 = CanFightArmedPedsWhenNotArmed
                //   1  = CanUseVehicles
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, _ped, 5, true);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, _ped, 46, true);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, _ped, 1, true);

                Function.Call(Hash.SET_PED_COMBAT_ABILITY, _ped, 2);   // 2 = 专业水平
                Function.Call(Hash.SET_PED_COMBAT_RANGE, _ped, 2);     // 2 = 远距离
                Function.Call(Hash.SET_PED_ACCURACY, _ped, 55);
                Function.Call(Hash.SET_PED_ALERTNESS, _ped, 3);        // 3 = 最高警觉
            }
            catch (Exception ex) { BuddyLog.Error("PrepareVehicleCombat failed", ex); }
        }

        /// <summary>解除"必须待在车里"的限制，恢复正常的 AI 反应。</summary>
        internal void ReleasePedVehicleLock() { ReleaseVehicleLock(); }

        /// <summary>
        /// 解除载具锁定。玩家下车时由 AgentSquad 调用 —— 这时队友可以自由下车。
        /// </summary>
        internal void ReleaseVehicleLock()
        {
            try
            {
                if (_ped == null || !_ped.Exists()) return;

                Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, _ped, false);
                Function.Call(Hash.SET_PED_CAN_BE_DRAGGED_OUT, _ped, true);
                Function.Call(Hash.SET_PED_STAY_IN_VEHICLE_WHEN_JACKED, _ped, false);

                _ped.SetConfigFlag(PedConfigFlagToggles.DontLeaveVehicleIfLeaderNotInVehicle, false);
                _ped.SetConfigFlag(PedConfigFlagToggles.DontDragMeOutCar, false);
                _ped.SetConfigFlag(PedConfigFlagToggles.StayInCarOnJack, false);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 是否绘制 HUD。多个队友同时画会在同一屏幕位置叠成一团，
        /// 所以只有队伍里的第一个队友开启。
        /// </summary>
        internal bool ShowHud { get; set; }

        /// <summary>队伍编号（0 表示第一个）。</summary>
        internal int SquadIndex { get { return _squadIndex; } }

        private void AttachBlip()
        {
            try
            {
                _blip = _ped.AddBlip();
                if (_blip != null && _blip.Exists())
                {
                    _blip.Sprite = BlipSprite.Friend;
                    _blip.Color = BlipColor.Green;
                    _blip.Name = BuddyConfig.BuddyName;
                    _blip.IsFriendly = true;
                    _blip.Scale = 0.85f;
                }
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------
        // main loop
        // ------------------------------------------------------------------

        internal void Update()
        {
            UpdateModelChange();

            if (!Exists) return;

            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                // 队友自己开车跟随：独立于状态机的支线。生存保护和 HUD 仍要照常跑，
                // 否则开车跟随期间队友不会吃药、HUD 也会停在旧状态。
                if (_escortMode)
                {
                    UpdateVehicleEscort(player);
                    GuardSurvival(player);
                    UpdateEscortCombat(player);
                    if (ShowHud) _hud.Draw(_ped, "驾驶跟随");
                    return;
                }

                // ---- 乘车战斗：乘客也要能还击 ----------------------------------
                //
                // 之前只有驾驶位跑 UpdateEscortCombat，**乘客完全不会还手** ——
                // 所以"全部坐进一辆车"时车里只有司机能开枪。
                // 这里对任何坐在车里的队友都跑一次乘车战斗，乘客用 TASK_DRIVE_BY 探身射击。
                if (_ped.IsInVehicle())
                {
                    UpdateVehicleCombat(player);
                    GuardSurvival(player);
                    if (ShowHud) _hud.Draw(_ped, "乘车");
                    return;
                }

                BuddyState next = DecideState(player);
                if (next != _state)
                {
                    _prevState = _state;
                    _state = next;
                    OnStateEnter(next, player);
                }

                switch (_state)
                {
                    case BuddyState.Hidden: UpdateHidden(player); break;
                    case BuddyState.Downed: UpdateDowned(player); break;
                    case BuddyState.InVehicle: UpdateVehicle(player); break;
                    case BuddyState.Combat: UpdateCombat(player); break;
                    case BuddyState.Follow: UpdateFollow(player); break;
                    default: UpdateIdle(player); break;
                }

                if (_state != BuddyState.Downed)
                {
                    GuardSurvival(player);
                }

                if (ShowHud) _hud.Draw(_ped, _state + " (" + _stateReason + ")");
            }
            catch (Exception)
            {
                // never let one bad frame kill the companion
            }
        }

        // ==================================================================
        // L1 - the brain: pure decision, ordered by priority
        // ==================================================================

        private BuddyState DecideState(Ped player)
        {
            // 1. cutscene / no control: get out of the way
            if (Game.IsCutsceneActive || !Game.Player.CanControlCharacter)
            {
                _stateReason = "过场";
                return BuddyState.Hidden;
            }

            // 2. dead (not downed - actually gone)
            if (_ped.IsDead)
            {
                _stateReason = "已阵亡";
                return BuddyState.Hidden;
            }

            // 3. downed: we parked it here and only a player revive gets it up
            if (_pendingRevive || (_state == BuddyState.Downed && _ped.Health > 0))
            {
                _stateReason = "倒地待救";
                return BuddyState.Downed;
            }

            // 3b. manual standby: stay put, but still fight if something attacks first
            if (_userStandby)
            {
                Ped attacker = FindThreat(player);
                if (attacker != null && _ped.IsInCombat)
                {
                    _threat = attacker;
                    _stateReason = "待命还击";
                    return BuddyState.Combat;
                }
                _stateReason = "待命(手动)";
                return BuddyState.Idle;
            }

            // 4. on foot with a threat: fight
            Ped threat = FindThreat(player);
            if (threat != null)
            {
                _threat = threat;
                _stateReason = "交战";
                return BuddyState.Combat;
            }
            _threat = null;

            // 5. player is mounted, or we should be: ride along / drive over
            if (ShouldUseVehicle(player))
            {
                _stateReason = player.IsInVehicle() ? "同乘" : "驾车接应";
                return BuddyState.InVehicle;
            }

            // 6. too far to walk: hoof it rather than teleport-spam
            float dist = _ped.Position.DistanceTo(player.Position);
            if (dist > BuddyConfig.TeleportIfFartherThan)
            {
                TeleportNear(player);
                _stateReason = "归队";
                return BuddyState.Follow;
            }

            if (dist <= BuddyConfig.ThreatScanRadius)
            {
                _stateReason = "跟随";
                return BuddyState.Follow;
            }

            _stateReason = "待命";
            return BuddyState.Idle;
        }

        // ==================================================================
        // L2 - behaviors
        // ==================================================================

        private void OnStateEnter(BuddyState state, Ped player)
        {
            switch (state)
            {
                case BuddyState.Hidden:
                    _ped.Task.ClearAll();
                    _ped.IsPositionFrozen = true;
                    Function.Call(Hash.SET_ENTITY_VISIBLE, _ped, false, false);
                    Function.Call(Hash.SET_ENTITY_COLLISION, _ped, false, false);
                    break;

                case BuddyState.Downed:
                    EnterDowned();
                    break;

                case BuddyState.InVehicle:
                    _ped.Task.ClearAll();
                    break;

                case BuddyState.Combat:
                    _ped.Task.ClearAll();
                    _nextCombatIssue = 0;      // allow an immediate combat task next update
                    break;

                case BuddyState.Follow:
                    if (_prevState == BuddyState.Hidden || _prevState == BuddyState.Downed)
                    {
                        ReleaseHidden();
                    }
                    _ped.Task.ClearAll();
                    break;

                default:
                    if (_prevState == BuddyState.Hidden || _prevState == BuddyState.Downed)
                    {
                        ReleaseHidden();
                    }
                    _ped.Task.ClearAll();
                    break;
            }
        }

        private void ReleaseHidden()
        {
            try
            {
                _ped.IsPositionFrozen = false;
                Function.Call(Hash.SET_ENTITY_VISIBLE, _ped, true, false);
                Function.Call(Hash.SET_ENTITY_COLLISION, _ped, true, true);
            }
            catch (Exception) { }
        }

        // ---- Hidden ----
        private void UpdateHidden(Ped player)
        {
            if (!Game.IsCutsceneActive && Game.Player.CanControlCharacter)
            {
                ReleaseHidden();
                // nudge the brain back into a real state next frame
                _state = BuddyState.Idle;
            }
        }

        // ---- Downed ----
        private void EnterDowned()
        {
            try
            {
                _downedSince = Game.GameTime;
                _ped.Task.ClearAllImmediately();
                _ped.IsPositionFrozen = true;
                _ped.CanRagdoll = true;
                _ped.Health = BuddyConfig.DownedHealthFloor;
                Function.Call(Hash.SET_PED_TO_RAGDOLL, _ped, 20000, 20000, 0, false, false, false);
                _ped.CanBeTargetted = false;   // enemies stop shooting a downed buddy
                Notify("~r~" + BuddyConfig.BuddyName + "倒地！~s~走近后按 " + KeyHint.Revive + " 施救");
            }
            catch (Exception) { }
        }

        private void UpdateDowned(Ped player)
        {
            // keep it visibly alive so the engine does not run the death flow
            if (_ped.Health <= 0)
            {
                _ped.Health = BuddyConfig.DownedHealthFloor;
            }
            _ped.IsPositionFrozen = true;

            int downedFor = Game.GameTime - _downedSince;

            float dist = _ped.Position.DistanceTo(player.Position);
            if (dist <= 3.0f && !player.IsInVehicle())
            {
                if (KeyHint.RevivePressed())
                {
                    Revive();
                    return;
                }
                if ((Game.GameTime / 500) % 2 == 0)
                {
                    GTA.UI.Screen.ShowHelpTextThisFrame("按 " + KeyHint.Revive + " 扶起" + BuddyConfig.BuddyName);
                }
            }

            if (downedFor > BuddyConfig.DownedTimeoutMs)
            {
                Notify("~r~" + BuddyConfig.BuddyName + "因失血过多阵亡");
                _ped.Health = 0;
                Function.Call(Hash.SET_ENTITY_HEALTH, _ped, 0);
            }
        }

        private void Revive()
        {
            try
            {
                _pendingRevive = false;
                _ped.IsPositionFrozen = false;
                _ped.CanRagdoll = false;
                Function.Call(Hash.SET_PED_TO_RAGDOLL, _ped, 0, 0, 0, false, false, false);
                _ped.CanBeTargetted = true;
                _ped.Health = BuddyConfig.ReviveHealth;
                _ped.Armor = 25;
                _invulnerableUntil = Game.GameTime + BuddyConfig.ReviveInvulnMs;
                _ped.Task.ClearAll();
                _state = BuddyState.Idle;
                Notify("~g~" + BuddyConfig.BuddyName + "已被扶起");
            }
            catch (Exception) { }
        }

        // ---- Combat ----
        private void UpdateCombat(Ped player)
        {
            Ped threat = _threat;
            if (threat == null || !threat.Exists() || threat.IsDead)
            {
                _ped.Task.ClearAll();
                return;
            }

            // Re-issue combat when the engine dropped it. Never re-issue while the engine is
            // mid-behaviour (cover transitions, knockdowns) or the task would thrash.
            if (!_ped.IsInCombat && Game.GameTime >= _nextCombatIssue)
            {
                _nextCombatIssue = Game.GameTime + 2000;
                // Let the engine do the work: TASK_COMBAT_PED internally handles cover
                // selection (CTaskCombatSeekCover), target selection, firing and reloading.
                _ped.Task.Combat(threat);
            }

            MaybeReload();
            MaybeTakeCover(threat);
        }

        /// <summary>Belt-and-braces reload: the combat task reloads on its own, this covers dry lulls.</summary>
        private void MaybeReload()
        {
            if (Game.GameTime < _nextReloadCheck) return;
            _nextReloadCheck = Game.GameTime + BuddyConfig.ReloadCheckMs;

            try
            {
                Weapon w = _ped.Weapons.Current;
                if (w == null) return;
                if (w.AmmoInClip > BuddyConfig.ReloadIfAmmoBelow) return;
                if (w.Ammo <= 0) return;          // nothing left to reload
                if (_ped.IsInCover) return;        // don't break cover to reload
                _ped.Task.ReloadWeapon();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Extra cover pressure. TASK_COMBAT_PED already seeks cover; this pushes the ped into
        /// cover when it is standing in the open taking fire and has not moved for a while.
        /// </summary>
        private void MaybeTakeCover(Ped threat)
        {
            try
            {
                if (_ped.IsInCover || _ped.IsRunning || _ped.IsWalking) return;
                if (Game.GameTime - _lastFollowIssueAt < 1500) return;
                Function.Call(Hash.TASK_SEEK_COVER_FROM_PED, _ped, threat, 8000, false);
            }
            catch (Exception) { }
        }

        // ---- Follow ----
        private void UpdateFollow(Ped player)
        {
            if (player.IsInVehicle() && ShouldUseVehicle(player))
            {
                _state = BuddyState.InVehicle;
                return;
            }

            float dist = _ped.Position.DistanceTo(player.Position);

            // Hard resync: cheap, and the alternative is a companion stuck behind a wall
            // forever while the player drives away.
            if (dist > BuddyConfig.TeleportIfFartherThan)
            {
                TeleportNear(player);
                return;
            }

            // persistFollowing=true makes the engine task hold the offset on its own, so the
            // re-issue here is only a watchdog for tasks the game aborted (ragdoll, jacking,
            // entering a car, a mission stealing the ped).
            bool needTask =
                _ped.CurrentScriptTaskStatus == ScriptTaskStatus.Vacant ||
                _ped.CurrentScriptTaskStatus == ScriptTaskStatus.Finished ||
                dist > BuddyConfig.ReissueFollowIfFartherThan;

            if (!needTask || Game.GameTime < _nextFollowReissue) return;

            _nextFollowReissue = Game.GameTime + 1500;
            _lastFollowIssueAt = Game.GameTime;

            // The engine task owns path-finding, obstacle avoidance and speed matching,
            // so "you run he runs, you stop he stops, he does not clip walls" is engine behaviour.
            _ped.Task.FollowToOffsetFromEntity(
                player,
                new Vector3(BuddyConfig.FollowOffsetSide, BuddyConfig.FollowOffsetBack, 0f),
                BuddyConfig.FollowSpeed,
                -1,
                BuddyConfig.FollowRadius,
                true);
        }

        private void UpdateIdle(Ped player)
        {
            if (_ped.CurrentScriptTaskStatus == ScriptTaskStatus.Vacant)
            {
                _ped.Task.StandStill(-1);
            }
        }

        // ---- Vehicle ----
        private bool ShouldUseVehicle(Ped player)
        {
            if (player.IsInVehicle()) return true;

            // Player on foot but the buddy is still in a car it used to catch up: get out.
            if (_ped.IsInVehicle()) return true;

            // Player on foot far away and we have a car: drive over instead of a long run.
            if (_driveTarget != null && _driveTarget.Exists() && !_driveTarget.IsDead)
            {
                float d = _ped.Position.DistanceTo(player.Position);
                if (d > BuddyConfig.ExitVehicleWhenPlayerOnFoot) return true;
            }

            return false;
        }

        private void UpdateVehicle(Ped player)
        {
            try
            {
                // already riding along
                if (_ped.IsInVehicle())
                {
                    Vehicle mine = _ped.CurrentVehicle;

                    if (player.IsInVehicle())
                    {
                        Vehicle theirs = player.CurrentVehicle;
                        if (theirs != null && theirs.Exists() && mine != theirs)
                        {
                            // player changed car: bail and re-board
                            _ped.Task.LeaveVehicle(LeaveVehicleFlags.None);
                            _enterVehicleIssuedAt = 0;
                        }
                        return;
                    }

                    // player got out; do we still need the car?
                    float d = _ped.Position.DistanceTo(player.Position);
                    if (d <= BuddyConfig.ExitVehicleWhenPlayerOnFoot)
                    {
                        // Ask once, then wait for the engine to actually put the ped on the
                        // ground. Re-asking every frame is what makes peds strobe in and out
                        // of vehicles.
                        if (_ped.CurrentScriptTaskStatus != ScriptTaskStatus.Performing)
                        {
                            _ped.Task.LeaveVehicle(LeaveVehicleFlags.None);
                        }
                        _driveTarget = null;
                        return;
                    }
                    return;
                }

                // on foot: get into the player's car, or fetch one
                if (player.IsInVehicle())
                {
                    Vehicle theirs = player.CurrentVehicle;
                    if (theirs != null && theirs.Exists() && !theirs.IsDead)
                    {
                        if (Game.GameTime - _enterVehicleIssuedAt > 4000)
                        {
                            _enterVehicleIssuedAt = Game.GameTime;
                            _ped.Task.EnterVehicle(
                                theirs,
                                VehicleSeat.Any,
                                12000,
                                1.0f,
                                EnterVehicleFlags.None);
                        }
                    }
                    return;
                }

                // player on foot: drive to them if we already have a car, else take one
                if (_driveTarget == null || !_driveTarget.Exists() || _driveTarget.IsDead)
                {
                    _driveTarget = AcquireVehicle(player);
                    if (_driveTarget == null)
                    {
                        // no wheels available: just walk, the follow behaviour is fine
                        _state = BuddyState.Follow;
                        return;
                    }
                }

                Vehicle car = _driveTarget;
                if (car.Driver != _ped && !_ped.IsInVehicle(car))
                {
                    // Approach / jack first; only drive once actually seated.
                    if (Game.GameTime - _enterVehicleIssuedAt > 4000)
                    {
                        _enterVehicleIssuedAt = Game.GameTime;
                        _ped.Task.EnterVehicle(car, VehicleSeat.Driver, 8000, 1.0f, EnterVehicleFlags.JackAnyone);
                    }
                    return;
                }

                // Vehicle mission: the engine drives, obeys traffic and stops on arrival.
                _ped.Task.DriveTo(
                    car,
                    player.Position,
                    4.0f,
                    VehicleDrivingFlags.DrivingModeAvoidVehicles |
                    VehicleDrivingFlags.StopAtTrafficLights |
                    VehicleDrivingFlags.StopForVehicles,
                    8.0f);
            }
            catch (Exception) { }
        }

        private Vehicle AcquireVehicle(Ped player)
        {
            if (Game.GameTime < _nextVehicleScan) return _driveTarget;
            _nextVehicleScan = Game.GameTime + 3000;

            try
            {
                Vehicle[] nearby = World.GetNearbyVehicles(_ped.Position, BuddyConfig.CarHijackRadius, null);
                Vehicle best = null;
                float bestDist = float.MaxValue;

                foreach (Vehicle v in nearby)
                {
                    if (v == null || !v.Exists() || v.IsDead) continue;
                    if (v.Model.IsHelicopter || v.Model.IsPlane) continue;

                    Ped driver = v.Driver;
                    if (driver != null && driver.Exists() && driver.IsPlayer) continue;

                    float d = v.Position.DistanceTo(_ped.Position);
                    bool free = driver == null || !driver.Exists() || driver.IsDead;
                    if (!free) d += 40f;   // prefer empty cars, but jacking is allowed

                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = v;
                    }
                }
                return best;
            }
            catch (Exception) { return null; }
        }

        // ---- between states ----
        private void TeleportNear(Ped player)
        {
            try
            {
                Vector3 target = player.Position + (player.ForwardVector * -2.0f);
                float groundZ;
                if (World.GetGroundHeight(target, out groundZ))
                {
                    target = new Vector3(target.X, target.Y, groundZ + 0.5f);
                }
                Function.Call(Hash.SET_ENTITY_COORDS, _ped, target.X, target.Y, target.Z, false, false, false, true);
                Function.Call(Hash.SET_ENTITY_HEADING, _ped, player.Heading);
                _ped.Task.ClearAll();
                _nextFollowReissue = 0;
            }
            catch (Exception) { }
        }

        // ==================================================================
        // survival
        // ==================================================================

        private void GuardSurvival(Ped player)
        {
            if (Game.GameTime < _nextSurvivalCheck) return;
            _nextSurvivalCheck = Game.GameTime + 100;

            try
            {
                if (Game.GameTime < _invulnerableUntil)
                {
                    _ped.IsInvincible = true;
                }
                else
                {
                    _ped.IsInvincible = false;
                }

                // Intercept the death flow: the engine kills a ped at 0 health immediately,
                // so a "downed" state has to be created before that point and held above 0.
                if (!_pendingRevive && !_ped.IsDead && _ped.Health <= 1)
                {
                    _pendingRevive = true;
                    EnterDowned();
                    return;
                }

                if (_pendingRevive) return;

                int maxHealth = Math.Max(1, _ped.MaxHealth);
                int pct = (int)(100f * _ped.Health / maxHealth);

                if (_ped.IsInCombat)
                {
                    // in a fight: patch up so the buddy does not fold mid-firefight
                    if (pct <= BuddyConfig.HealthPanicPercent && CanUseMedkit())
                    {
                        UseMedkit();
                    }
                    if (_ped.Armor <= BuddyConfig.ArmorLowValue)
                    {
                        _ped.Armor = BuddyConfig.ArmorGrantAmount;
                    }
                }
                else if (pct <= BuddyConfig.HealthLowPercent && CanUseMedkit())
                {
                    UseMedkit();
                }
            }
            catch (Exception) { }
        }

        private bool CanUseMedkit()
        {
            return Game.GameTime >= _nextMedkitAllowed && !_ped.IsInCover;
        }

        /// <summary>Heals and plays a swallow animation so the heal is visible in-world.</summary>
        private void UseMedkit()
        {
            try
            {
                _nextMedkitAllowed = Game.GameTime + BuddyConfig.MedkitCooldownMs;
                int healed = Math.Min(_ped.MaxHealth, _ped.Health + BuddyConfig.MedkitHealAmount);
                _ped.Health = healed;
                _ped.Task.PlayAnimation(
                    "mp_suicide",
                    "pill",
                    2.0f,
                    -1,
                    (AnimationFlags)49);   // loop + upper body, so combat can resume
                Function.Call(Hash.TASK_PLAY_ANIM, _ped,
                    "mp_suicide", "pill", 2.0f, -1.0f, 2000, 49, 0f, false, false, false);
            }
            catch (Exception) { }
        }

        // ==================================================================
        // threat detection
        // ==================================================================

        private Ped FindThreat(Ped player)
        {
            if (Game.GameTime < _nextThreatScan)
            {
                if (_threat != null && _threat.Exists() && !_threat.IsDead) return _threat;
            }
            _nextThreatScan = Game.GameTime + 400;

            try
            {
                Ped best = null;
                float bestDist = float.MaxValue;

                // 1. whoever the player is actually fighting is the buddy's primary target
                foreach (Ped p in World.GetNearbyPeds(player, BuddyConfig.ThreatScanRadius, null))
                {
                    if (!IsValidEnemy(p, player)) continue;

                    bool fightingPlayer = p.IsInCombatAgainst(player) || player.IsInCombatAgainst(p);
                    float d = p.Position.DistanceTo(_ped.Position);
                    if (fightingPlayer) d -= 25f;   // heavy preference: help with the player's fight

                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = p;
                    }
                }

                // 2. a ped shooting at / aiming at the buddy itself is also a threat
                if (best == null)
                {
                    foreach (Ped p in World.GetNearbyPeds(_ped.Position, BuddyConfig.ThreatScanRadius, null))
                    {
                        if (!IsValidEnemy(p, player)) continue;
                        if (p.IsInCombatAgainst(_ped))
                        {
                            best = p;
                            break;
                        }
                    }
                }

                // 3. player under fire but no explicit target resolved: fight the nearest
                //    hostile. Player.IsAiming alone is deliberately NOT a trigger - aiming
                //    at nothing must not put the buddy into combat.
                if (best == null && player.IsInCombat)
                {
                    foreach (Ped p in World.GetNearbyPeds(player, BuddyConfig.ThreatScanRadius, null))
                    {
                        if (!IsValidEnemy(p, player)) continue;
                        if (p.IsInCombat)
                        {
                            best = p;
                            break;
                        }
                    }
                }

                return best;
            }
            catch (Exception) { return null; }
        }

        private static bool IsValidEnemy(Ped p, Ped player)
        {
            if (p == null || !p.Exists()) return false;
            if (p.IsDead) return false;
            if (p.IsPlayer) return false;
            if (p == player) return false;
            if (!p.IsHuman) return false;
            return true;
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static void Notify(string msg)
        {
            try { Notification.PostTicker(msg, false, true); }
            catch (Exception) { }
        }
    }
}
