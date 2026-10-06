using System;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>
    /// One-key field report.
    ///
    /// Why this exists: the mod cannot be observed from outside the game process, and
    /// screenshots or prose descriptions lose exactly the detail needed to debug a failure.
    /// Pressing the report key writes a complete, machine-readable snapshot into
    /// GTA5Buddy.log - mode gate decision, config, catalogue sizes, companion state and world
    /// counters - so a problem can be diagnosed from the log alone.
    /// </summary>
    internal static class BuddyDiagnostics
    {
        internal static void Dump(Companion buddy, MenuController menu)
        {
            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("================ DIAGNOSTIC REPORT ================");
                sb.AppendLine("time          : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("gameTime      : " + Game.GameTime + " ms   frame=" + Game.FrameCount);
                sb.AppendLine("fps           : " + SafeFps());
                sb.AppendLine("game version  : enhanced=" + SafeEnhanced());

                // ---- mode gate: the story-mode-only guarantee ----
                string detail;
                bool story = OnlineGuard.IsStoryMode(out detail);
                sb.AppendLine("mode gate     : " + (story ? "STORY (companion allowed)" : "NOT STORY (companion blocked)"));
                sb.AppendLine("mode signals  : " + detail);

                // ---- config ----
                sb.AppendLine("config file   : " + BuddyIni.LoadedFrom);
                sb.AppendLine("config values : hud=" + BuddyIni.HudEnabled + " menu=" + BuddyIni.MenuEnabled +
                              " font=" + BuddyIni.Font + " panel=" + BuddyIni.PanelX + "," + BuddyIni.PanelY +
                              " scale=" + BuddyIni.PanelScale);
                sb.AppendLine("log file      : " + BuddyLog.ActivePath);
                if (!string.IsNullOrEmpty(BuddyLog.FailureDetail))
                {
                    sb.AppendLine("log fallbacks : " + BuddyLog.FailureDetail);
                }

                // ---- catalogues: proof the enumeration ran ----
                sb.AppendLine("vehicle cat.  : built=" + menu.Vehicles.Built +
                              " models=" + menu.Vehicles.TotalCount +
                              " categories=" + menu.Vehicles.CategoryCount +
                              " active=" + menu.Vehicles.CategoryLabel);
                sb.AppendLine("skin cat.     : built=" + menu.Skins.Built +
                              " models=" + menu.Skins.TotalCount +
                              " categories=" + menu.Skins.CategoryCount +
                              " active=" + menu.Skins.CategoryLabel);

                // ---- panel ----
                sb.AppendLine("panel         : open=" + menu.IsOpen + " page=" + menu.Page);

                // ---- companion ----
                if (buddy.Exists)
                {
                    Ped p = buddy.Ped;
                    sb.AppendLine("companion     : state=" + buddy.State + " downed=" + buddy.IsDowned);
                    sb.AppendLine("companion ped : health=" + p.Health + "/" + p.MaxHealth +
                                  " armor=" + p.Armor +
                                  " persistent=" + p.IsPersistent +
                                  " dead=" + p.IsDead +
                                  " invincible=" + p.IsInvincible);
                    sb.AppendLine("companion pos : " + p.Position.ToString() +
                                  "   inVehicle=" + p.IsInVehicle() +
                                  " inCombat=" + p.IsInCombat +
                                  " taskStatus=" + p.CurrentScriptTaskStatus);
                }
                else
                {
                    sb.AppendLine("companion     : NOT SPAWNED");
                }

                // ---- player ----
                Ped player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    sb.AppendLine("player        : health=" + player.Health + "/" + player.MaxHealth +
                                  " armor=" + player.Armor +
                                  " inVehicle=" + player.IsInVehicle() +
                                  " wanted=" + Game.Player.Wanted.WantedLevel);
                    Weapon w = player.Weapons.Current;
                    if (w != null)
                    {
                        sb.AppendLine("player weapon : " + w.DisplayName + "  clip=" + w.AmmoInClip + "/" + w.Ammo);
                    }
                }

                // ---- world counters: catches "nothing spawned" situations ----
                sb.AppendLine("world         : peds=" + World.PedCount + "/" + World.PedCapacity +
                              " vehicles=" + World.VehicleCount + "/" + World.VehicleCapacity +
                              " weather=" + World.Weather);

                sb.AppendLine("===================================================");

                // write as one block so a report is never interleaved with other lines
                foreach (string line in sb.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    BuddyLog.Info(line);
                }

                Notify("~b~诊断报告已写入 " + System.IO.Path.GetFileName(BuddyLog.ActivePath));
            }
            catch (Exception ex)
            {
                BuddyLog.Error("diagnostic dump failed", ex);
            }
        }

        private static string SafeFps()
        {
            try { return Game.FPS.ToString("0.0"); }
            catch (Exception) { return "?"; }
        }

        private static string SafeEnhanced()
        {
            try { return Game.isEnhanced.ToString(); }
            catch (Exception) { return "?"; }
        }

        private static void Notify(string message)
        {
            try { GTA.UI.Notification.PostTicker(message, false, true); }
            catch (Exception) { }
        }
    }
}
