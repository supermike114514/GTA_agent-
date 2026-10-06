using System;
using GTA;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>
    /// Character stats and weapon loadout controls: health, armour, ammo and weapon grants.
    ///
    /// Ammo is restored through WeaponCollection.GetAllWeaponHashes() so every weapon the ped
    /// actually carries is covered, instead of a hardcoded weapon list.
    /// </summary>
    internal sealed class StatsPanel
    {
        private const int MaxHealthValue = 200;
        private const int MaxArmorValue = 100;

        private readonly MenuPanel _menu = new MenuPanel(
            "人物与武器",
            "上下选择 · 回车执行 · 退格返回 · F5 关闭");

        // When set, every action applies to this ped instead of the player. Captured from
        // what the player is aiming at, because the panel freezes the player's controls and
        // the aim target cannot be re-read afterwards.
        private Ped _target;

        internal MenuPanel Menu { get { return _menu; } }

        /// <summary>Name of the current subject, for the panel hint.</summary>
        internal string TargetName
        {
            get
            {
                Ped p = Resolve();
                if (p == null) return "无";
                if (p.IsPlayer) return "玩家";
                return "行人 #" + p.Handle;
            }
        }

        /// <summary>
        /// Binds the subject to whatever the player is aiming at, or back to the player when
        /// nothing is targeted. Must be called while the player still has control.
        /// </summary>
        internal void CaptureTarget()
        {
            try
            {
                Entity aimed = Game.Player.TargetedEntity;
                Ped ped = (aimed != null && aimed.Exists()) ? aimed as Ped : null;

                if (ped == null || ped.IsDead)
                {
                    _target = null;
                    Notify("~y~未瞄准目标，作用于玩家自己");
                    return;
                }

                _target = ped;
                Notify(ped.IsPlayer ? "~g~目标：玩家" : "~g~目标：行人 #" + ped.Handle);
                BuddyLog.Info("panel target set: player=" + ped.IsPlayer + " handle=" + ped.Handle);
            }
            catch (Exception ex) { BuddyLog.Error("CaptureTarget failed", ex); }
        }

        /// <summary>The ped the actions should act on: the captured target, else the player.</summary>
        private Ped Resolve()
        {
            if (_target != null && _target.Exists() && !_target.IsDead) return _target;

            Ped p = Game.Player.Character;
            return (p != null && p.Exists()) ? p : null;
        }

        internal void Build()
        {
            _menu.Clear();

            _menu.Add(">> 选取瞄准的目标", delegate { CaptureTarget(); }, "TARGET");

            _menu.Add("回满血量 (" + MaxHealthValue + ")",
                delegate { SetHealth(MaxHealthValue); }, "HP");

            _menu.Add("回满护甲 (" + MaxArmorValue + ")",
                delegate { SetArmor(MaxArmorValue); }, "AR");

            _menu.Add("血量 + 护甲 全满",
                delegate { SetHealth(MaxHealthValue); SetArmor(MaxArmorValue); Notify("~g~血量与护甲已回满"); }, "ALL");

            _menu.Add("设为无敌", delegate { SetInvincible(true); }, string.Empty);

            _menu.Add("取消无敌", delegate { SetInvincible(false); }, string.Empty);

            _menu.Add("当前武器弹药补满", delegate { RefillCurrentWeapon(); }, "AMMO");

            _menu.Add("全部携带武器弹药补满", delegate { RefillAllWeapons(); }, "AMMO");

            _menu.Add("无限弹药 开", delegate { SetInfiniteAmmo(true); }, string.Empty);

            _menu.Add("无限弹药 关", delegate { SetInfiniteAmmo(false); }, string.Empty);

            _menu.Add("发放 卡宾步枪 + 弹药", delegate { GiveWeapon(WeaponHash.CarbineRifle); }, "GIVE");

            _menu.Add("发放 突击步枪 + 弹药", delegate { GiveWeapon(WeaponHash.AssaultRifle); }, "GIVE");

            _menu.Add("发放 战斗手枪 + 弹药", delegate { GiveWeapon(WeaponHash.CombatPistol); }, "GIVE");

            _menu.Add("发放 泵动霰弹枪 + 弹药", delegate { GiveWeapon(WeaponHash.PumpShotgun); }, "GIVE");

            _menu.Add("发放 狙击步枪 + 弹药", delegate { GiveWeapon(WeaponHash.SniperRifle); }, "GIVE");

            _menu.Add("发放 RPG + 弹药", delegate { GiveWeapon(WeaponHash.RPG); }, "GIVE");

            _menu.Add("发放 微型冲锋枪 + 弹药", delegate { GiveWeapon(WeaponHash.MicroSMG); }, "GIVE");

            _menu.Add("状态：报告当前血量/护甲",
                delegate { ReportStatus(); }, "INFO");

            _menu.Normalize();
        }

        // ------------------------------------------------------------------
        // actions - all of them resolve their subject through Resolve(), so they work on the
        // player or on a captured aimed-at ped with no further branching
        // ------------------------------------------------------------------

        private void SetHealth(int value)
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;

                // raise the ceiling too, otherwise the engine clamps the value straight back
                if (p.MaxHealth < value) p.MaxHealth = value;
                p.Health = value;
                Notify("~g~" + TargetName + " 血量已设为 " + value);
            }
            catch (Exception ex) { BuddyLog.Error("SetHealth failed", ex); }
        }

        private void SetArmor(int value)
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;
                p.Armor = value;
                Notify("~g~" + TargetName + " 护甲已设为 " + value);
            }
            catch (Exception ex) { BuddyLog.Error("SetArmor failed", ex); }
        }

        private void SetInvincible(bool on)
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;
                p.IsInvincible = on;
                Notify(on ? "~g~" + TargetName + " 已开启无敌" : "~y~" + TargetName + " 已取消无敌");
            }
            catch (Exception ex) { BuddyLog.Error("SetInvincible failed", ex); }
        }

        private void RefillCurrentWeapon()
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;

                Weapon w = p.Weapons.Current;
                if (w == null)
                {
                    Notify("~y~" + TargetName + " 当前没有武器");
                    return;
                }

                int clip = w.MaxAmmoInClip > 0 ? w.MaxAmmoInClip : w.DefaultClipSize;
                w.AmmoInClip = clip;
                w.Ammo = w.MaxAmmo > 0 ? w.MaxAmmo : 9999;
                Notify("~g~已补满 " + w.DisplayName);
            }
            catch (Exception ex) { BuddyLog.Error("RefillCurrentWeapon failed", ex); }
        }

        private void RefillAllWeapons()
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;

                int count = 0;
                foreach (WeaponHash hash in p.Weapons.GetAllWeaponHashes())
                {
                    Weapon w = p.Weapons[hash];
                    if (w == null || !w.IsPresent) continue;

                    int clip = w.MaxAmmoInClip > 0 ? w.MaxAmmoInClip : w.DefaultClipSize;
                    w.AmmoInClip = clip;
                    w.Ammo = w.MaxAmmo > 0 ? w.MaxAmmo : 9999;
                    count++;
                }

                Notify("~g~已补满 " + TargetName + " 的 " + count + " 把武器弹药");
            }
            catch (Exception ex) { BuddyLog.Error("RefillAllWeapons failed", ex); }
        }

        private void SetInfiniteAmmo(bool on)
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;

                foreach (WeaponHash hash in p.Weapons.GetAllWeaponHashes())
                {
                    Weapon w = p.Weapons[hash];
                    if (w == null || !w.IsPresent) continue;
                    w.InfiniteAmmo = on;
                    w.InfiniteAmmoClip = on;
                }

                Notify(on ? "~g~" + TargetName + " 无限弹药已开启" : "~y~" + TargetName + " 无限弹药已关闭");
            }
            catch (Exception ex) { BuddyLog.Error("SetInfiniteAmmo failed", ex); }
        }

        private void GiveWeapon(WeaponHash hash)
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;

                p.Weapons.Give(hash, 9999, true, true);
                Weapon w = p.Weapons[hash];
                if (w != null)
                {
                    w.AmmoInClip = w.MaxAmmoInClip > 0 ? w.MaxAmmoInClip : w.DefaultClipSize;
                    w.Ammo = 9999;
                }
                Notify("~g~已向 " + TargetName + " 发放 " + (w != null ? w.DisplayName : hash.ToString()));
            }
            catch (Exception ex) { BuddyLog.Error("GiveWeapon failed", ex); }
        }

        private void ReportStatus()
        {
            try
            {
                Ped p = Resolve();
                if (p == null) return;

                string line = TargetName + " · 血量 " + p.Health + "/" + p.MaxHealth + " · 护甲 " + p.Armor;
                Weapon w = p.Weapons.Current;
                if (w != null) line += " · " + w.DisplayName + " " + w.AmmoInClip + "/" + w.Ammo;

                Notify("~b~" + line);
                BuddyLog.Info("stats: " + line);
            }
            catch (Exception ex) { BuddyLog.Error("ReportStatus failed", ex); }
        }

        private static void Notify(string message)
        {
            try { GTA.UI.Notification.PostTicker(message, false, true); }
            catch (Exception) { }
        }
    }
}
