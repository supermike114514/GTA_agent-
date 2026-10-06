using System;
using System.Windows.Forms;
using GTA;

namespace GTA5Buddy
{
    /// <summary>
    /// Key bindings. GTA binds E to "enter vehicle" natively, so the revive key is also
    /// exposed on H; the mod accepts either while standing next to a downed companion.
    /// </summary>
    internal static class KeyHint
    {
        internal const string Toggle = "K";   // buddy stands by / resumes following
        internal const string Revive = "E";   // revive a downed buddy (alternate: H)
        internal const string Spawn = "J";    // spawn / recall the buddy
        internal const string Hud = "L";      // toggle the companion HUD
        internal const string Menu = "F5";    // open / close the control panel
        internal const string MenuAlt = "F7"; // alternate panel key
        internal const string MenuPad = "M";  // game-control panel toggle (polled every frame)
        internal const string Report = "F6";  // write a diagnostic report to the log

        /// <summary>The panel's own key, read as a WinForms key so it never fights a game binding.</summary>
        internal const Keys MenuKey = Keys.F5;

        /// <summary>Second panel key: function keys are occasionally swallowed by overlays.</summary>
        internal const Keys MenuKeyAlt = Keys.F7;

        /// <summary>
        /// Panel toggle read from the game's own control state instead of the keyboard.
        ///
        /// This is the belt-and-braces path. KeyDown delivery proved unreliable here, while
        /// control polling is what GTA hands to scripts every frame. The game's interaction
        /// control is used because it is idle during normal play, and the comparison uses
        /// IsControlJustPressed, which reads the raw state and therefore still fires while the
        /// panel has DISABLE_ALL_CONTROL_ACTIONS active.
        /// </summary>
        internal const GTA.Control MenuControl = GTA.Control.InteractionMenu;

        /// <summary>Writes a full field report into GTA5Buddy.log.</summary>
        internal const Keys ReportKey = Keys.F6;

        private static int _lastTick;

        /// <summary>Edge-triggered key read; true once per press.</summary>
        internal static bool Pressed(Keys key)
        {
            try
            {
                if (!Game.IsKeyPressed(key)) return false;
                int now = Game.GameTime;
                if (now - _lastTick < 250) return false;
                _lastTick = now;
                return true;
            }
            catch (Exception) { return false; }
        }

        internal static bool RevivePressed()
        {
            return Pressed(Keys.E) || Pressed(Keys.H);
        }
    }
}
