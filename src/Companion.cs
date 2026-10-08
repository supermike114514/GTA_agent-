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
                car.PlaceOnGround();
                car.IsEngineRunning = true;

                _ped.Task.WarpIntoVehicle(car, VehicleSeat.Driver);

                Function.Call(Hash.SET_DRIVER_ABILITY, _ped, 1.0f);
                Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _ped, 0.5f);

                _escortVehicle = car;
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
                _nextEscortTask = 0;

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

                // ---- 任务是否仍在运行 ----------------------------------------
                //
                // 这是本功能之前一直失败的核心原因：
                //
                //   TASK_* 原生是**替换型**的 —— 调用它会替换掉 ped 当前的脚本任务。
                //   我原来每隔 1.2~2.2 秒无条件重发一次驾驶任务，等于不断地把车
                //   "刚开始执行的路线规划"重置掉，车永远跑不过两秒就被打断，
                //   表现就是停在原地。
                //
                // 正确做法：持续型任务只需下发一次，之后由引擎自己维持跟随关系；
                // 只有任务确实结束了才需要重新下发。
                ScriptTaskStatus status = _ped.CurrentScriptTaskStatus;
                bool taskRunning = status == ScriptTaskStatus.Performing ||
                                   status == ScriptTaskStatus.WaitingToStart;

                // 位移追踪：用来判断"任务在跑但车没动"（卡住）这种例外情况。
                Vector3 now = _escortVehicle.Position;
                float moved = _lastEscortPos.HasValue ? now.DistanceTo(_lastEscortPos.Value) : 0f;
                _lastEscortPos = now;

                bool stalled = _escortVehicle.Speed < 1.0f && distance > 15.0f;

                // 任务在正常跑、而且车没卡住 —— 什么都不要做，别去打断它
                if (taskRunning && !stalled) return;

                // 卡住时给它一段观察期再重发，避免频繁重置
                if (taskRunning && stalled && Game.GameTime < _nextEscortTask) return;

                _nextEscortTask = Game.GameTime + (taskRunning ? 4000 : 0);

                float cruise = distance > 45.0f ? 35.0f : (_squadIndex == 0 ? 22.0f : 19.0f);
                string kind;

                if (player.IsInVehicle() && player.CurrentVehicle != null && player.CurrentVehicle.Exists())
                {
                    // ---- 玩家在车上：用 escort（持续型，引擎自己维持编队）----
                    //
                    // p3 = mode：Rear(-1) 表示跟在目标车后方
                    // p7 = -1（惯例值）
                    // noRoadsDistance：离道路多远之内允许脱离路网，给大一点避免卡住
                    Function.Call(Hash.TASK_VEHICLE_ESCORT,
                                  _ped, _escortVehicle, player.CurrentVehicle,
                                  (int)VehicleEscortType.Rear,
                                  cruise,
                                  (int)EscortDrivingFlags,
                                  15.0f,      // minDistance：保持的最小间距
                                  -1,
                                  30.0f);     // noRoadsDistance
                    kind = "ESCORT(rear)";
                }
                else
                {
                    // ---- 玩家步行：用 ped-target 的 Follow 任务（同样是持续型）----
                    //
                    // p6 = targetReachedDist：到这个距离就算"跟上"，车会停
                    // p7 = straightLineDist：**不能设 0**，设 0 会让车试图直线冲向目标
                    //      （撞墙、抄野地）；给 10 米表示"最后 10 米才走直线"
                    Function.Call(Hash.TASK_VEHICLE_MISSION_PED_TARGET,
                                  _ped, _escortVehicle, player,
                                  7,          // VehicleMissionType.Follow
                                  cruise,
                                  (int)EscortDrivingFlags,
                                  14.0f,      // targetReachedDist
                                  10.0f,      // straightLineDist
                                  false);     // driveAgainstTraffic
                    kind = "FOLLOW(ped)";
                }

                BuddyLog.Info("escort[" + _squadIndex + "]: issued " + kind +
                              "  wasRunning=" + taskRunning + " status=" + status +
                              " dist=" + distance.ToString("0.0") +
                              " cruise=" + cruise.ToString("0.0") +
                              " vehSpeed=" + _escortVehicle.Speed.ToString("0.0") +
                              " moved=" + moved.ToString("0.0"));
            }
            catch (Exception ex) { BuddyLog.Error("UpdateVehicleEscort failed", ex); }
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
                              (seat == VehicleSeat.Driver ? " (driver, escort armed)" : " (passenger)"));
            }
            catch (Exception ex) { BuddyLog.Error("WarpIntoVehicle failed", ex); }
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
                    if (ShowHud) _hud.Draw(_ped, "驾驶跟随");
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
