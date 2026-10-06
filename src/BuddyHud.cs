using System;
using System.Drawing;
using System.Text;
using GTA;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>
    /// Draws the companion HUD: name, state, health and armour.
    ///
    /// Like the panel, the whole HUD is ONE multi-line TextElement. Stacking three separate
    /// positioned elements produced exactly the same overlap as the panel did on this setup,
    /// because TextElement.Position is not being honoured - so line breaks are delegated to the
    /// engine's text layout instead.
    /// </summary>
    internal sealed class BuddyHud
    {
        private readonly TextElement _text;
        private readonly StringBuilder _sb = new StringBuilder(256);

        internal BuddyHud()
        {
            _text = new TextElement(string.Empty, new PointF(BuddyConfig.HudX, BuddyConfig.HudY), BuddyConfig.HudScale)
            {
                Color = Color.FromArgb(255, 225, 238, 255),
                Outline = true,
                Shadow = true,
            };
        }

        /// <summary>Renders the HUD. Never throws into the script tick.</summary>
        internal void Draw(Ped buddy, string stateLabel)
        {
            if (!BuddyConfig.DrawHud || !BuddyIni.HudEnabled) return;
            if (buddy == null || !buddy.Exists()) return;

            try
            {
                int health = buddy.Health;
                int maxHealth = Math.Max(1, buddy.MaxHealth);
                int armor = buddy.Armor;
                float hpPct = Clamp01(health / (float)maxHealth);

                _sb.Length = 0;
                _sb.Append(BuddyConfig.BuddyName).Append("  ").Append(stateLabel).Append('\n');
                _sb.Append("HP ").Append(health).Append('/').Append(maxHealth)
                   .Append("   AR ").Append(armor);

                _text.Caption = _sb.ToString();
                _text.Position = new PointF(BuddyConfig.HudX, BuddyConfig.HudY);
                _text.Color = hpPct < 0.3f
                    ? Color.FromArgb(255, 255, 130, 130)
                    : Color.FromArgb(255, 225, 238, 255);
                _text.Font = BuddyIni.ResolveFont();
                _text.Draw();
            }
            catch (Exception)
            {
                // A HUD failure must never kill the companion loop.
            }
        }

        private static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
    }
}
