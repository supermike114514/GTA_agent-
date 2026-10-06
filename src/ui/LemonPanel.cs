using System;
using System.Collections.Generic;
using LemonUI;
using LemonUI.Menus;
using GTA;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>
    /// Graphical panel built on LemonUI.
    ///
    /// WHY LEMONUI: on GTA V Enhanced every script-level graphics path reachable from a C#
    /// script renders nothing here. ContainerElement, the DRAW_RECT native and Sprites were each
    /// tried in turn; each produced a panel of bare text with no background, while TextElement
    /// text rendered correctly. LemonUI draws through GTA's Scaleform UI system instead - the
    /// same system the game's own menus use, and the nearest available equivalent to the DirectX
    /// present-callback path that makes the C++ NativeTrainer menu work on this setup.
    ///
    /// This class is deliberately self-contained: it reads the catalogues through their data
    /// accessors and performs the stat edits itself with natives, so it does not disturb the
    /// hand-rolled panels that are still present as a fallback.
    ///
    /// The pool is only processed while the panel is open, so nothing is drawn and no input is
    /// consumed when it is closed.
    /// </summary>
    internal sealed class LemonPanel
    {
        private readonly Companion _buddy;
        private readonly VehiclePanel _vehicleData;
        private readonly SkinPanel _skinData;

        private readonly ObjectPool _pool = new ObjectPool();

        private readonly NativeMenu _root;
        private readonly NativeMenu _commands;
        private readonly NativeMenu _spawner;
        private readonly NativeMenu _vehicles;
        private readonly NativeMenu _categories;
        private readonly NativeMenu _skins;
        private readonly NativeMenu _stats;

        // 队友专属：皮肤与武器。队友的装备不该和玩家混在一起，所以单独成页。
        private readonly NativeMenu _buddySkin;
        private readonly NativeMenu _buddySkinCategories;
        private readonly NativeMenu _buddyWeapons;

        private bool _wired;
        private bool _open;

        private NativeCheckboxItem _applyToCompanion;   // 人物项作用于队友还是玩家

        /// <summary>
        /// Model the companion starts with, and what "恢复默认外观" switches back to.
        /// Mirrors BuddyConfig.DefaultModel ("s_m_m_security_01").
        /// </summary>
        private static readonly PedHash DefaultBuddyModel = PedHash.Security01SMM;

        // The catalogues index across frames (see MenuController.WarmUpCatalogues), so the
        // lists are rebuilt when each one transitions to "built" rather than only on open.
        private bool _vehicleCatalogueWasReady;
        private bool _skinCatalogueWasReady;

        internal LemonPanel(Companion buddy, VehiclePanel vehicleData, SkinPanel skinData)
        {
            _buddy = buddy;
            _vehicleData = vehicleData;
            _skinData = skinData;

            _root = new NativeMenu("GTA5Buddy 控制台", "主菜单");
            _commands = new NativeMenu("队友指令", "向队友下达指令");
            _spawner = new NativeMenu("生成器", "载具与角色皮肤");
            _vehicles = new NativeMenu("载具生成器", "回车直接刷出");
            _categories = new NativeMenu("载具分类", "选定后返回列表");
            _skins = new NativeMenu("角色皮肤", "回车在身旁生成 NPC");
            _stats = new NativeMenu("人物与武器", "血量 / 护甲 / 弹药");
            _buddySkin = new NativeMenu("队友皮肤", "直接把队友换成该模型");
            _buddySkinCategories = new NativeMenu("队友皮肤分类", "选定后返回模型列表");
            _buddyWeapons = new NativeMenu("队友武器", "为队友配枪与弹药");

            _pool.Add(_root);
            _pool.Add(_commands);
            _pool.Add(_spawner);
            _pool.Add(_vehicles);
            _pool.Add(_categories);
            _pool.Add(_skins);
            _pool.Add(_stats);
            _pool.Add(_buddySkin);
            _pool.Add(_buddySkinCategories);
            _pool.Add(_buddyWeapons);

            WireMenus();
        }

        internal bool IsOpen { get { return _open; } }

        internal void Toggle()
        {
            _open = !_open;

            try
            {
                if (_open)
                {
                    EnsureWired();
                    RefreshLists();
                    _pool.HideAll();
                    _root.Visible = true;
                }
                else
                {
                    _pool.HideAll();
                }
            }
            catch (Exception ex)
            {
                BuddyLog.Error("lemon panel toggle failed", ex);
            }
        }

        internal void Close()
        {
            _open = false;
            try { _pool.HideAll(); } catch (Exception) { }
        }

        /// <summary>Processes input and drawing. Does nothing while the panel is closed.</summary>
        internal void Update()
        {
            if (!_open) return;

            try
            {
                _pool.Process();
                UpdateSubtitle();
                RefreshWhenCatalogueFinishes();
            }
            catch (Exception ex)
            {
                BuddyLog.Error("lemon panel process failed", ex);
            }
        }

        /// <summary>
        /// Rebuilds the vehicle and skin lists the moment their catalogue finishes indexing.
        /// Opening the panel early (while the catalogue is still empty) therefore fills in by
        /// itself instead of leaving the user staring at a placeholder row.
        /// </summary>
        private void RefreshWhenCatalogueFinishes()
        {
            bool vehicleReady = _vehicleData.Built;
            if (vehicleReady && !_vehicleCatalogueWasReady)
            {
                _vehicleCatalogueWasReady = true;
                RebuildCategories();
                RebuildVehicles();
                BuddyLog.Info("lemon panel: vehicle catalogue ready, " +
                              _vehicleData.TotalCount + " models, " +
                              _vehicleData.CategoryCount + " categories");
            }

            bool skinReady = _skinData.Built;
            if (skinReady && !_skinCatalogueWasReady)
            {
                _skinCatalogueWasReady = true;
                RebuildSkins();
                RebuildBuddySkinCategories();
                RebuildBuddySkin();
                BuddyLog.Info("lemon panel: skin catalogue ready, " + _skinData.TotalCount + " models");
            }
        }

        // ------------------------------------------------------------------

        private void WireMenus()
        {
            _root.AddSubMenu(_commands);
            _root.AddSubMenu(_spawner);
            _root.AddSubMenu(_stats);
            _root.AddSubMenu(_buddySkin);
            _root.AddSubMenu(_buddyWeapons);

            _spawner.AddSubMenu(_vehicles);
            _spawner.AddSubMenu(_skins);

            AddAction(_commands, "跟随我", "同伴跟随玩家", delegate { _buddy.OrderFollow(); });
            AddAction(_commands, "原地待命", "留在当前位置", delegate { _buddy.OrderStay(); });
            AddAction(_commands, "前往地图路径点", "队友跑向地图标记", delegate { _buddy.OrderMoveToWaypoint(); });
            AddAction(_commands, "上我的车", "进入玩家当前载具", delegate { _buddy.OrderEnterPlayerVehicle(); });
            AddAction(_commands, "下车", "离开当前载具", delegate { _buddy.OrderLeaveVehicle(); });
            AddAction(_commands, "攻击我瞄准的目标", "攻击玩家瞄准的行为", delegate { _buddy.OrderAttackAimedTarget(); });
            AddAction(_commands, "停止战斗并归队", "取消交战", delegate { _buddy.OrderStopCombat(); });
            AddAction(_commands, "紧急归位", "瞬移到玩家身边", delegate { _buddy.OrderTeleportHere(); });
            AddAction(_commands, "召唤队友", "生成队友", delegate { _buddy.Spawn(); });
            AddAction(_commands, "收回队友", "移除队友", delegate { _buddy.Despawn(); });

            BuildBuddyWeapons();

            // ---- 人物：目标选择 ----
            // 作用于玩家还是队友，所有人物项都跟随这个选择
            _applyToCompanion = new NativeCheckboxItem("目标：作用于队友",
                                                       "取消勾选则作用于玩家自己",
                                                       false);
            _applyToCompanion.CheckboxChanged += delegate
            {
                Notify(_applyToCompanion.Checked ? "~b~人物选项将作用于队友" : "~b~人物选项将作用于玩家");
            };
            _stats.Add(_applyToCompanion);

            // ---- 人物：血量与护甲 ----
            AddAction(_stats, "血量 · 回满", "设为当前上限", delegate { SetHealth(200); });
            AddAction(_stats, "血量 · 上限 200", "同时回满", delegate { SetHealth(200); });
            AddAction(_stats, "血量 · 上限 500", "同时回满", delegate { SetHealth(500); });
            AddAction(_stats, "血量 · 上限 2000", "同时回满", delegate { SetHealth(2000); });
            AddAction(_stats, "护甲 · 50", "中型护甲", delegate { SetArmor(50); });
            AddAction(_stats, "护甲 · 100", "满护甲", delegate { SetArmor(100); });
            AddAction(_stats, "清除身上的血迹", "恢复外观", delegate { ClearBlood(); });

            // ---- 人物：无敌与生存 ----
            AddAction(_stats, "无敌 · 开", "免疫一切伤害", delegate { SetInvincible(true); });
            AddAction(_stats, "无敌 · 关", "恢复正常受伤", delegate { SetInvincible(false); });
            AddAction(_stats, "免疫要害伤害 · 开", "不会被爆头击杀", delegate { SetCriticalHits(false); });
            AddAction(_stats, "免疫要害伤害 · 关", "恢复正常", delegate { SetCriticalHits(true); });

            // ---- 人物：战斗能力 ----
            AddAction(_stats, "枪法 · 100% 命中", "射击精准度拉满", delegate { SetAccuracy(100); });
            AddAction(_stats, "枪法 · 50% 命中", "中等精准度", delegate { SetAccuracy(50); });
            AddAction(_stats, "枪法 · 0% 命中", "完全打不中", delegate { SetAccuracy(0); });
            AddAction(_stats, "视野 · 极远", "更早发现敌人", delegate { SetSeeingRange(500.0f); });
            AddAction(_stats, "听力 · 极远", "更早察觉动静", delegate { SetHearingRange(500.0f); });
            AddAction(_stats, "移动速度 · 1.0x", "正常速度", delegate { SetMoveRate(1.0f); });
            AddAction(_stats, "移动速度 · 1.5x", "明显加速", delegate { SetMoveRate(1.5f); });
            AddAction(_stats, "移动速度 · 2.5x", "极速移动", delegate { SetMoveRate(2.5f); });
            AddAction(_stats, "允许被击倒 · 开", "会进入倒地状态", delegate { SetCanRagdoll(true); });
            AddAction(_stats, "允许被击倒 · 关", "免疫击倒", delegate { SetCanRagdoll(false); });

            // ---- 人物：武器与弹药 ----
            AddAction(_stats, "全部携带武器弹药补满", "遍历实际携带的武器", delegate { RefillAllWeapons(); });
            AddAction(_stats, "无限弹药 · 开", "逐把武器开启", delegate { SetInfiniteAmmo(true); });
            AddAction(_stats, "无限弹药 · 关", "逐把武器关闭", delegate { SetInfiniteAmmo(false); });
            AddAction(_stats, "发放 卡宾步枪 + 弹药", "9999 发", delegate { GiveWeapon(WeaponHash.CarbineRifle); });
            AddAction(_stats, "发放 突击步枪 + 弹药", "9999 发", delegate { GiveWeapon(WeaponHash.AssaultRifle); });
            AddAction(_stats, "发放 冲锋枪 + 弹药", "9999 发", delegate { GiveWeapon(WeaponHash.MicroSMG); });
            AddAction(_stats, "发放 霰弹枪 + 弹药", "9999 发", delegate { GiveWeapon(WeaponHash.PumpShotgun); });
            AddAction(_stats, "发放 狙击枪 + 弹药", "9999 发", delegate { GiveWeapon(WeaponHash.HeavySniper); });
            AddAction(_stats, "发放 加特林 + 弹药", "9999 发", delegate { GiveWeapon(WeaponHash.Minigun); });
            AddAction(_stats, "发放 RPG + 弹药", "50 发", delegate { GiveWeapon(WeaponHash.RPG); });
            AddAction(_stats, "发放 手雷 x25", "投掷武器", delegate { GiveWeapon(WeaponHash.Grenade); });
            AddAction(_stats, "收缴全部武器", "卸下所有武器", delegate { RemoveAllWeapons(); });

            // ---- 人物：状态与通缉 ----
            AddAction(_stats, "消除通缉", "通缉等级归零", delegate { ClearWanted(); });
            AddAction(_stats, "五档通缉（测试用）", "立刻通缉 5 星", delegate { SetWanted(5); });
            AddAction(_stats, "报告当前状态", "写入日志并提示", delegate { ReportStatus(); });
        }

        private void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            _vehicleData.Build();
            _skinData.Build();
        }

        private static void AddAction(NativeMenu menu, string title, string description, Action action)
        {
            var item = new NativeItem(title, description);
            item.Activated += delegate { try { action(); } catch (Exception) { } };
            menu.Add(item);
        }

        /// <summary>
        /// Companion weapon page. These act on the buddy directly rather than going through the
        /// player/companion target switch, because a companion loadout is a separate concern.
        /// </summary>
        private void BuildBuddyWeapons()
        {
            _buddyWeapons.Clear();

            AddAction(_buddyWeapons, "卡宾步枪 + 400 发", "默认配枪", delegate
                { _buddy.GiveWeapon(WeaponHash.CarbineRifle, 400); });
            AddAction(_buddyWeapons, "突击步枪 + 400 发", "威力更高", delegate
                { _buddy.GiveWeapon(WeaponHash.AssaultRifle, 400); });
            AddAction(_buddyWeapons, "冲锋枪 + 600 发", "射速快", delegate
                { _buddy.GiveWeapon(WeaponHash.MicroSMG, 600); });
            AddAction(_buddyWeapons, "霰弹枪 + 200 发", "近战强力", delegate
                { _buddy.GiveWeapon(WeaponHash.PumpShotgun, 200); });
            AddAction(_buddyWeapons, "重型狙击枪 + 100 发", "远程精确", delegate
                { _buddy.GiveWeapon(WeaponHash.HeavySniper, 100); });
            AddAction(_buddyWeapons, "加特林 + 9999 发", "火力压制", delegate
                { _buddy.GiveWeapon(WeaponHash.Minigun, 9999); });
            AddAction(_buddyWeapons, "RPG + 100 发", "重型火力", delegate
                { _buddy.GiveWeapon(WeaponHash.RPG, 100); });
            AddAction(_buddyWeapons, "手雷 + 25 颗", "投掷武器", delegate
                { _buddy.GiveWeapon(WeaponHash.Grenade, 25); });
            AddAction(_buddyWeapons, "武士刀", "近身武器", delegate
                { _buddy.GiveWeapon(WeaponHash.Knife, 1); });

            AddAction(_buddyWeapons, "无限弹药 · 开", "队友所有武器不消耗弹药", delegate
                { _buddy.SetInfiniteAmmo(true); });
            AddAction(_buddyWeapons, "无限弹药 · 关", "恢复正常消耗", delegate
                { _buddy.SetInfiniteAmmo(false); });
            AddAction(_buddyWeapons, "补满队友护甲与血量", "恢复到满状态", delegate
                { _buddy.Restore(); });
            AddAction(_buddyWeapons, "收缴队友全部武器", "清空队友装备", delegate
                { _buddy.StripWeapons(); });
        }

        // ------------------------------------------------------------------
        // dynamic lists
        // ------------------------------------------------------------------

        private void RefreshLists()
        {
            RebuildCategories();
            RebuildVehicles();
            RebuildSkins();
            RebuildBuddySkinCategories();
            RebuildBuddySkin();
        }

        private void RebuildCategories()
        {
            _categories.Clear();

            if (!_vehicleData.Built)
            {
                _categories.Add(new NativeItem("载具数据库正在建立…",
                                               "已索引 " + _vehicleData.TotalCount + " 个模型，请稍候"));
                return;
            }

            for (int i = 0; i < _vehicleData.CategoryCount; i++)
            {
                string name = _vehicleData.CategoryNameAt(i);
                int count = _vehicleData.CountInCategory(i);
                if (count == 0) continue;

                int captured = i;
                var item = new NativeItem(name, count + " 辆载具");
                item.Activated += delegate
                {
                    _vehicleData.SetCategory(captured);
                    RebuildVehicles();
                    _categories.Visible = false;
                    _vehicles.Visible = true;
                };
                _categories.Add(item);
            }

            if (_categories.Items.Count == 0)
            {
                _categories.Add(new NativeItem("(没有可用分类)", string.Empty));
            }
        }

        private void RebuildVehicles()
        {
            _vehicles.Clear();

            var catItem = new NativeItem("分类浏览    [" + _vehicleData.CategoryLabel + "]",
                                        "按载具分类挑选");
            catItem.Activated += delegate
            {
                _vehicles.Visible = false;
                _categories.Visible = true;
            };
            _vehicles.Add(catItem);

            int shown = 0;
            foreach (VehiclePanel.Entry e in _vehicleData.VisibleEntries())
            {
                VehiclePanel.Entry captured = e;
                var item = new NativeItem(captured.Display, captured.ClassLabel);
                item.Activated += delegate { _vehicleData.SpawnByHash(captured.Hash); };
                _vehicles.Add(item);

                shown++;
                if (shown >= 300) break;
            }

            if (shown == 0)
            {
                _vehicles.Add(new NativeItem(
                    _vehicleData.Built ? "(此分类下没有载具)" : "载具数据库正在建立…",
                    _vehicleData.Built ? string.Empty
                                       : "已索引 " + _vehicleData.TotalCount + " 个模型，请稍候"));
            }
        }

        private void RebuildSkins()
        {
            _skins.Clear();

            int shown = 0;
            foreach (SkinPanel.Entry e in _skinData.VisibleEntries())
            {
                SkinPanel.Entry captured = e;
                var item = new NativeItem(captured.Label, captured.CategoryName);
                item.Activated += delegate { _skinData.SpawnByHash(captured.Hash); };
                _skins.Add(item);

                shown++;
                if (shown >= 300) break;
            }

            if (shown == 0)
            {
                _skins.Add(new NativeItem(
                    _skinData.Built ? "(此分类下没有皮肤)" : "人物模型数据库正在建立…",
                    _skinData.Built ? string.Empty
                                    : "已索引 " + _skinData.TotalCount + " 个模型，请稍候"));
            }
        }

        // ------------------------------------------------------------------
        // companion skin (replaces the companion's model instead of spawning an NPC)
        // ------------------------------------------------------------------

        private void RebuildBuddySkinCategories()
        {
            _buddySkinCategories.Clear();

            if (!_skinData.Built)
            {
                _buddySkinCategories.Add(new NativeItem("人物模型数据库正在建立…",
                                                        "已索引 " + _skinData.TotalCount + " 个模型"));
                return;
            }

            for (int i = 0; i < _skinData.CategoryCount; i++)
            {
                int count = _skinData.CountInCategory(i);
                if (count == 0) continue;

                int captured = i;
                var item = new NativeItem(_skinData.CategoryNameAt(i), count + " 个模型");
                item.Activated += delegate
                {
                    _skinData.SetCategory(captured);
                    RebuildBuddySkin();
                    _buddySkinCategories.Visible = false;
                    _buddySkin.Visible = true;
                };
                _buddySkinCategories.Add(item);
            }

            if (_buddySkinCategories.Items.Count == 0)
            {
                _buddySkinCategories.Add(new NativeItem("(没有可用分类)", string.Empty));
            }
        }

        private void RebuildBuddySkin()
        {
            _buddySkin.Clear();

            var catItem = new NativeItem("分类浏览    [" + _skinData.CategoryLabel + "]",
                                         "按分类挑选队友皮肤");
            catItem.Activated += delegate
            {
                _buddySkin.Visible = false;
                _buddySkinCategories.Visible = true;
            };
            _buddySkin.Add(catItem);

            AddAction(_buddySkin, "恢复默认外观", "换回初始队友模型", delegate
                { _buddy.RequestModelChange(DefaultBuddyModel); });

            int shown = 0;
            foreach (SkinPanel.Entry e in _skinData.VisibleEntries())
            {
                SkinPanel.Entry captured = e;
                var item = new NativeItem(captured.Label, captured.CategoryName);
                item.Activated += delegate { _buddy.RequestModelChange(captured.Hash); };
                _buddySkin.Add(item);

                shown++;
                if (shown >= 300) break;
            }

            if (shown == 0 && !_skinData.Built)
            {
                _buddySkin.Add(new NativeItem("人物模型数据库正在建立…",
                                              "已索引 " + _skinData.TotalCount + " 个模型"));
            }
        }

        private void UpdateSubtitle()
        {
            try
            {
                // LemonUI deprecated Subtitle in favour of Name
                _root.Name = _buddy.Exists
                    ? "队友 " + _buddy.State
                    : "队友未在场";
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------
        // stat actions
        //
        // Everything below acts on Target(), which is either the player or the companion
        // depending on the "目标：作用于队友" checkbox at the top of the menu.
        // ------------------------------------------------------------------

        private static Ped Player
        {
            get
            {
                Ped p = Game.Player.Character;
                return (p != null && p.Exists()) ? p : null;
            }
        }

        /// <summary>The ped the character options act on.</summary>
        private Ped Target()
        {
            try
            {
                if (_applyToCompanion != null && _applyToCompanion.Checked)
                {
                    Ped c = _buddy.Ped;
                    if (c != null && c.Exists()) return c;
                    Notify("~r~队友不在场，已作用于玩家");
                }
            }
            catch (Exception) { }
            return Player;
        }

        private void SetHealth(int value)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                // max must be raised first, otherwise the game clamps health back down
                if (p.MaxHealth < value) p.MaxHealth = value;
                p.Health = value;
                Notify("~g~血量已设为 " + value + "/" + p.MaxHealth);
            }
            catch (Exception) { }
        }

        private void SetArmor(int value)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                p.Armor = value;
                Notify("~g~护甲已设为 " + value);
            }
            catch (Exception) { }
        }

        private void SetInvincible(bool on)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                p.IsInvincible = on;
                Notify(on ? "~g~无敌已开启" : "~y~无敌已关闭");
            }
            catch (Exception) { }
        }

        private void SetCriticalHits(bool on)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                p.CanSufferCriticalHits = on;
                Notify(on ? "~y~已恢复要害伤害" : "~g~要害伤害已免疫");
            }
            catch (Exception) { }
        }

        private void SetAccuracy(int value)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                p.Accuracy = value;
                Notify("~g~枪法精准度：" + value + "%");
            }
            catch (Exception) { }
        }

        private void SetSeeingRange(float value)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                Function.Call(Hash.SET_PED_SEEING_RANGE, p.Handle, value);
                Notify("~g~视野范围已扩大");
            }
            catch (Exception) { }
        }

        private void SetHearingRange(float value)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                Function.Call(Hash.SET_PED_HEARING_RANGE, p.Handle, value);
                Notify("~g~听力范围已扩大");
            }
            catch (Exception) { }
        }

        private void SetMoveRate(float value)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, p.Handle, value);
                Notify("~g~移动速度：" + value + "x");
            }
            catch (Exception) { }
        }

        private void SetCanRagdoll(bool on)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                p.CanRagdoll = on;
                Notify(on ? "~y~允许被击倒" : "~g~免疫击倒");
            }
            catch (Exception) { }
        }

        private void ClearBlood()
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                Function.Call(Hash.CLEAR_PED_BLOOD_DAMAGE, p.Handle);
                Notify("~g~血迹已清除");
            }
            catch (Exception) { }
        }

        private void RemoveAllWeapons()
        {
            try
            {
                Ped p = Target();
                if (p == null) return;
                p.Weapons.RemoveAll();
                Notify("~y~已收缴全部武器");
            }
            catch (Exception) { }
        }

        private void ClearWanted()
        {
            try
            {
                Game.Player.Wanted.SetWantedLevel(0, false);
                Game.Player.Wanted.ApplyWantedLevelChangeNow(false);
                Notify("~g~通缉已消除");
            }
            catch (Exception) { }
        }

        private void SetWanted(int level)
        {
            try
            {
                Game.Player.Wanted.SetWantedLevel(level, false);
                Game.Player.Wanted.ApplyWantedLevelChangeNow(false);
                Notify("~r~通缉等级：" + level);
            }
            catch (Exception) { }
        }

        private void RefillAllWeapons()
        {
            try
            {
                Ped p = Target();
                if (p == null) return;

                int n = 0;
                foreach (WeaponHash hash in p.Weapons.GetAllWeaponHashes())
                {
                    Weapon w = p.Weapons[hash];
                    if (w == null || !w.IsPresent) continue;
                    w.AmmoInClip = w.MaxAmmoInClip > 0 ? w.MaxAmmoInClip : w.DefaultClipSize;
                    w.Ammo = w.MaxAmmo > 0 ? w.MaxAmmo : 9999;
                    n++;
                }
                Notify("~g~已补满 " + n + " 把武器");
            }
            catch (Exception) { }
        }

        private void SetInfiniteAmmo(bool on)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;

                foreach (WeaponHash hash in p.Weapons.GetAllWeaponHashes())
                {
                    Weapon w = p.Weapons[hash];
                    if (w == null || !w.IsPresent) continue;
                    w.InfiniteAmmo = on;
                    w.InfiniteAmmoClip = on;
                }
                Notify(on ? "~g~无限弹药已开启" : "~y~无限弹药已关闭");
            }
            catch (Exception) { }
        }

        private void GiveWeapon(WeaponHash hash)
        {
            try
            {
                Ped p = Target();
                if (p == null) return;

                p.Weapons.Give(hash, 9999, true, true);
                Weapon w = p.Weapons[hash];
                if (w != null)
                {
                    w.AmmoInClip = w.MaxAmmoInClip > 0 ? w.MaxAmmoInClip : w.DefaultClipSize;
                    w.Ammo = 9999;
                }
                Notify("~g~已发放武器");
            }
            catch (Exception) { }
        }

        private void ReportStatus()
        {
            try
            {
                Ped p = Target();
                if (p == null) return;

                string line = "血量 " + p.Health + "/" + p.MaxHealth + "  护甲 " + p.Armor;
                Weapon w = p.Weapons.Current;
                if (w != null) line += "  " + w.DisplayName + " " + w.AmmoInClip + "/" + w.Ammo;

                Notify("~b~" + line);
                BuddyLog.Info("stats: " + line);
            }
            catch (Exception) { }
        }

        private static void Notify(string message)
        {
            try { GTA.UI.Notification.PostTicker(message, false, true); }
            catch (Exception) { }
        }
    }
}
