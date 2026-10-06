using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>
    /// Entry point. Owns the single Companion and drives it from the script tick.
    ///
    /// STORY MODE ONLY. The constructor refuses to initialise unless OnlineGuard can
    /// positively confirm a single-player session, and the tick keeps watching so that a
    /// session which turns into Online shuts the companion down instead of following the
    /// player into it.
    ///
    /// The mode verdict and its raw signal values are written to disk immediately at
    /// construction - before the game is playable - so a gate decision is on record even
    /// if the session ends moments later.
    /// </summary>
    public class BuddyMain : Script
    {
        // 队伍而非单个队友：面板可以生成任意数量的队友，"指令"作用于全部。
        // _buddy 保留为队伍里的第一个实例（0 号），用于按键 J/K 这类单人操作。
        private readonly AgentSquad _squad = new AgentSquad();
        private readonly MenuController _menu;
        private readonly LemonPanel _panel;
        private bool _autoSpawnDone;
        private bool _allowedToRun;
        private bool _playableReported;
        private bool _spawnReported;
        private bool _onlineRefusalReported;
        private bool _controlToggleProven;

        /// <summary>0 号队友；不存在时按需创建（这样 J 键总有对象可发）。</summary>
        private Companion Buddy { get { return _squad.EnsureAtLeastOne(); } }

        public BuddyMain()
        {
            // settings first: the HUD and the panel read their font/position/visibility from here
            BuddyIni.Load();

            Interval = BuddyConfig.TickIntervalMs;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAborted;

            _menu = new MenuController(Buddy);
            // LemonUI draws through GTA's Scaleform UI system, which is the only graphics path
            // that renders on this setup; the old MenuController stays for its catalogue logic.
            _panel = new LemonPanel(_squad, _menu.Vehicles, _menu.Skins);

            string detail;
            _allowedToRun = OnlineGuard.IsStoryMode(out detail);

            // Log first, cosmetic messages second: diagnostics must not depend on the game
            // being in a state where notifications work.
            BuddyLog.Blank();
            BuddyLog.Info("=== GTA5Buddy startup (mode gate evaluated in the constructor) ===");
            BuddyLog.Info("mode    : " + (_allowedToRun ? "STORY -> companion enabled" : "NOT STORY -> companion disabled"));
            BuddyLog.Info("signals : " + detail);
            BuddyLog.Info("logfile : " + BuddyLog.ActivePath);
            BuddyLog.Blank();

            try
            {
                if (_allowedToRun)
                {
                    Notification.PostTicker(
                        "~g~队友模组已加载~s~  " + KeyHint.Menu + " 控制面板 / " + KeyHint.Spawn +
                        " 召唤 / " + KeyHint.Toggle + " 待命跟随 / " + KeyHint.Report + " 诊断报告",
                        false,
                        true);
                }
                else
                {
                    // Fail closed, and say exactly why in the log so a false positive is
                    // diagnosable without guesswork.
                    Notification.PostTicker(
                        "~r~队友模组未装载：仅限故事模式（检测到线上会话）",
                        false,
                        true);
                }
            }
            catch (Exception) { }
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                // --- mode gate: this runs before anything else can touch the world ---
                string detail;
                bool story = OnlineGuard.IsStoryMode(out detail);

                if (!_allowedToRun)
                {
                    if (story)
                    {
                        // The construction-time probe ran before the session was up, or it
                        // was a false positive. Recover instead of staying dead all session.
                        _allowedToRun = true;
                    }
                    else
                    {
                        ReportRefusal(detail, false);
                        return;
                    }
                }
                else if (!story)
                {
                    // Legitimate transition out of story mode: tear the companion down and
                    // stop touching the world. ScriptHookV closes the game anyway - the point
                    // is that this mod does nothing on the way out.
                    ReportRefusal(detail, true);
                    _allowedToRun = false;
                    _autoSpawnDone = false;
                    _squad.DespawnAll();
                    return;
                }

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                ReportPlayableOnce(detail);

                // Spawn once, a moment after the session is actually playable.
                if (!_autoSpawnDone && !Game.IsCutsceneActive && Game.Player.CanControlCharacter)
                {
                    _autoSpawnDone = true;
                    SpawnCompanion();
                }

                // A dead companion is replaced rather than left as a corpse forever.
                // 队友死亡后由队伍逐个补位（多队友时只补死掉的那个）
                if (_autoSpawnDone) ReplaceDeadAgents();

                ReportSpawnOnce();

                // Panel toggle read from the game's control state as well as from KeyDown:
                // KeyDown delivery proved unreliable here, and this path only needs the GTA
                // control, which is delivered to scripts every frame.
                PollMenuControl();

                // The catalogues stream in across frames so opening the spawner never stutters.
                _menu.WarmUpCatalogues();

                // LemonUI handles its own navigation, drawing and input while open.
                _panel.Update();

                _squad.Update();
            }
            catch (Exception ex)
            {
                // A single bad frame must not tear the script down - but it must be visible.
                BuddyLog.Error("tick failed", ex);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // NOTE: deliberately NOT gated on _allowedToRun. An earlier version returned early
            // here when the mode gate failed, which silently killed every hotkey - including
            // the panel - and made a running script look completely dead. The gate must only
            // restrict companion behaviour, never the user's ability to open the panel.
            try
            {
                // F5 (or F7) toggles the panel; LemonUI handles its own navigation once open.
                if (e.KeyCode == KeyHint.MenuKey || e.KeyCode == KeyHint.MenuKeyAlt)
                {
                    _panel.Toggle();
                    return;
                }

                // F6 writes a full field report to GTA5Buddy.log. It is deliberately available
                // even while the panel is open, because that is exactly when something is
                // likely to be misbehaving.
                if (e.KeyCode == KeyHint.ReportKey)
                {
                    BuddyDiagnostics.Dump(Buddy, _menu);
                    return;
                }

                // while the panel is open the game hotkeys must not fire behind it
                if (_panel.IsOpen) return;

                // world hotkeys stay gated: they drive the companion, which is the thing the
                // mode gate is actually protecting
                if (!_allowedToRun) return;

                switch (e.KeyCode)
                {
                    case Keys.J:
                        if (_squad.AnyExists()) _squad.DespawnAll();
                        else SpawnCompanion();
                        break;

                    case Keys.K:
                        _squad.OrderStay();
                        break;
                }
            }
            catch (Exception ex) { BuddyLog.Error("key handler failed", ex); }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                BuddyLog.Info("script aborted (reload or session end)");
                // Give the ped back to the engine but drop our blip, so a script reload does
                // not stack blips. Recall with J afterwards.
                _squad.DespawnAll();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Records once when the session actually became playable, so the log shows whether
        /// the mod ever got past the loading screen.
        /// </summary>
        /// <summary>
        /// Opens/closes the panel from the game's own control state.
        ///
        /// Uses the DISABLED-aware native deliberately: the panel calls
        /// DISABLE_ALL_CONTROL_ACTIONS every frame while it is open, and SHVDN's
        /// Game.IsControlJustPressed maps to IS_CONTROL_JUST_PRESSED, which reports false for
        /// a disabled control. Reading that wrapper therefore made the panel impossible to
        /// read input from once it had frozen the player - including closing itself again.
        /// </summary>
        private void PollMenuControl()
        {
            try
            {
                bool pressed = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)KeyHint.MenuControl)
                            || Function.Call<bool>(Hash.IS_CONTROL_JUST_PRESSED, 0, (int)KeyHint.MenuControl);

                if (!pressed) return;

                if (!_controlToggleProven)
                {
                    _controlToggleProven = true;
                    BuddyLog.Info("panel toggled via game control (" + KeyHint.MenuControl + ")");
                }
                _panel.Toggle();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Spawns the companion and logs the outcome. Also the path taken by the panel's
        /// 召唤队友 row and the J hotkey.
        /// </summary>
        internal bool SpawnCompanion()
        {
            bool ok = Buddy.Spawn();
            try
            {
                Ped player = Game.Player.Character;
                BuddyLog.Info("spawn " + (ok ? "succeeded" : "FAILED") +
                              (player != null && player.Exists() ? " near " + player.Position.ToString() : string.Empty));
            }
            catch (Exception) { }
            return ok;
        }

        private void ReportPlayableOnce(string detail)
        {
            if (_playableReported) return;
            _playableReported = true;

            BuddyLog.Info("session is playable; mode re-check : " + detail);
        }

        private void ReportSpawnOnce()
        {
            if (_spawnReported) return;
            _spawnReported = true;

            if (!_squad.AnyExists())
            {
                BuddyLog.Warn("companion does not exist after the spawn attempt - see the auto-spawn line above");
                return;
            }

            Ped p = Buddy.Ped;
            BuddyLog.Info(string.Format(
                "companion: health={0}/{1} armor={2} persistent={3} model={4} pos={5}",
                p.Health, p.MaxHealth, p.Armor, p.IsPersistent, p.Model.Hash, p.Position.ToString()));
        }

        /// <summary>
        /// 逐个补位：多队友时只重新生成死掉的那个，不会把整队换掉。
        ///
        /// Companion.ReplaceDeadPed 只处理自己这一位，所以这里遍历队伍逐个询问即可。
        /// </summary>
        private void ReplaceDeadAgents()
        {
            try
            {
                foreach (Companion agent in _squad.Snapshot())
                {
                    if (!agent.Exists) continue;
                    if (agent.Ped == null || !agent.Ped.IsDead) continue;

                    BuddyLog.Info("squad: an agent died; respawning that one");
                    agent.ReplaceDeadPed();
                }
            }
            catch (Exception ex) { BuddyLog.Error("ReplaceDeadAgents failed", ex); }
        }

        private void ReportRefusal(string detail, bool transitioned)
        {
            if (_onlineRefusalReported) return;
            _onlineRefusalReported = true;

            BuddyLog.Blank();
            BuddyLog.Warn(transitioned
                ? "=== session left story mode - companion disabled, nothing further will run ==="
                : "=== refused to load: not a story-mode session ===");
            BuddyLog.Warn("signals : " + detail);
            BuddyLog.Warn("story mode only. ScriptHookV is expected to close the game now.");
            BuddyLog.Blank();
        }
    }
}
