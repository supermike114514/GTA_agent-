using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>
    /// Minimal INI reader for the handful of settings worth exposing without a rebuild.
    ///
    /// The file is looked for next to the assembly and in the game root, the same two places
    /// <see cref="BuddyLog"/> probes, because a Steam library folder is often read-only for
    /// the user running the game.
    ///
    /// If no file exists, a commented template is written next to the assembly so the options
    /// are discoverable, and defaults are used.
    /// </summary>
    internal static class BuddyIni
    {
        private const string FileName = "GTA5Buddy.ini";

        private static bool _loaded;
        private static string _loadedFrom = "(defaults)";

        // ---- settings ----
        internal static bool HudEnabled = true;
        internal static bool MenuEnabled = true;
        internal static UIFontKind Font = UIFontKind.Default;
        internal static float PanelX = 0.03f;
        internal static float PanelY = 0.06f;
        internal static float PanelScale = 1.0f;

        /// <summary>Which font family the panel draws with.</summary>
        internal enum UIFontKind
        {
            /// <summary>SHVDN's default (ChaletLondon). Might not carry CJK glyphs.</summary>
            Default,
            /// <summary>Labeled font, often a safer carrier for non-Latin scripts.</summary>
            Labeled,
            /// <summary>Fixed-width font.</summary>
            Monospace,
            /// <summary>Leaderboard font.</summary>
            Leaderboard,
        }

        internal static string LoadedFrom { get { return _loadedFrom; } }

        internal static Font ResolveFont()
        {
            switch (Font)
            {
                case UIFontKind.Labeled: return GTA.UI.Font.ChaletComprimeCologne;
                // Font.Monospace is obsolete in SHVDN v3; RockstarTag is its replacement
                case UIFontKind.Monospace: return GTA.UI.Font.RockstarTag;
                case UIFontKind.Leaderboard: return GTA.UI.Font.Leaderboard;
                default: return GTA.UI.Font.ChaletLondon;
            }
        }

        internal static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                string path = FindFile();
                if (path == null)
                {
                    WriteTemplate();
                    BuddyLog.Info("no " + FileName + " found; using defaults");
                    return;
                }

                _loadedFrom = path;
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line[0] == ';' || line[0] == '#' || line.StartsWith("//")) continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = line.Substring(eq + 1).Trim();

                    switch (key)
                    {
                        case "hud": HudEnabled = ParseBool(val, true); break;
                        case "menu": MenuEnabled = ParseBool(val, true); break;
                        case "font": Font = ParseFont(val); break;
                        case "panelx": PanelX = ParseFloat(val, PanelX); break;
                        case "panely": PanelY = ParseFloat(val, PanelY); break;
                        case "panelscale": PanelScale = ParseFloat(val, PanelScale); break;
                    }
                }

                BuddyLog.Info("config loaded from " + path + " : hud=" + HudEnabled + " menu=" + MenuEnabled +
                              " font=" + Font + " panel=" + PanelX + "," + PanelY + " scale=" + PanelScale);
            }
            catch (Exception ex)
            {
                BuddyLog.Error("config load failed", ex);
            }
        }

        private static string FindFile()
        {
            string assemblyDir = ".";
            try { assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "."; }
            catch (Exception) { }

            var candidates = new System.Collections.Generic.List<string>();
            try
            {
                DirectoryInfo parent = new DirectoryInfo(assemblyDir).Parent;
                if (parent != null && parent.Exists) candidates.Add(Path.Combine(parent.FullName, FileName));
            }
            catch (Exception) { }
            candidates.Add(Path.Combine(assemblyDir, FileName));

            foreach (string c in candidates)
            {
                try { if (File.Exists(c)) return c; }
                catch (Exception) { }
            }
            return null;
        }

        /// <summary>
        /// Writes a commented template so the options are discoverable. Best effort only - the
        /// folder is frequently read-only.
        /// </summary>
        private static void WriteTemplate()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
                string path = Path.Combine(assemblyDir, FileName);
                if (File.Exists(path)) return;

                var sb = new StringBuilder();
                sb.AppendLine("; GTA5Buddy 配置 - 改完重进游戏或用 Insert 热重载即生效");
                sb.AppendLine("; 如果面板里的中文显示成方块/空白，把 font 依次改成 labeled / monospace / leaderboard 试");
                sb.AppendLine();
                sb.AppendLine("; 是否显示队友 HUD（true/false）");
                sb.AppendLine("hud=true");
                sb.AppendLine();
                sb.AppendLine("; 是否启用 F5 控制面板（true/false）");
                sb.AppendLine("menu=true");
                sb.AppendLine();
                sb.AppendLine("; 面板字体: default | labeled | monospace | leaderboard");
                sb.AppendLine("font=default");
                sb.AppendLine();
                sb.AppendLine("; 面板左上角位置（0.0 - 1.0 的屏幕比例）");
                sb.AppendLine("panelx=0.03");
                sb.AppendLine("panely=0.06");
                sb.AppendLine();
                sb.AppendLine("; 面板整体缩放倍率");
                sb.AppendLine("panelscale=1.0");

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                _loadedFrom = path + " (template written)";
            }
            catch (Exception)
            {
                // read-only folder - defaults stand
            }
        }

        private static bool ParseBool(string v, bool fallback)
        {
            if (string.IsNullOrEmpty(v)) return fallback;
            v = v.Trim().ToLowerInvariant();
            if (v == "1" || v == "true" || v == "yes" || v == "on") return true;
            if (v == "0" || v == "false" || v == "no" || v == "off") return false;
            return fallback;
        }

        private static float ParseFloat(string v, float fallback)
        {
            float f;
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
            return fallback;
        }

        private static UIFontKind ParseFont(string v)
        {
            switch ((v ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "labeled":
                case "chaletcomprimecologne": return UIFontKind.Labeled;
                case "monospace": return UIFontKind.Monospace;
                case "leaderboard": return UIFontKind.Leaderboard;
                default: return UIFontKind.Default;
            }
        }
    }
}
