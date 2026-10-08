using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GTA5Buddy
{
    /// <summary>
    /// Dead-simple, fail-safe diagnostic log.
    ///
    /// Location matters. The game process usually runs with the same restricted user token
    /// that cannot write into a Steam library, so the mod's own folder can be READ-ONLY.
    /// ScriptHookV and ScriptHookVDotNet already write their logs into the game's root
    /// folder, which proves that folder IS writable - so it is the first choice, with the
    /// assembly folder and %TEMP% as fallbacks.
    ///
    /// The location is probed once with a real write rather than assumed, so a read-only
    /// scripts folder degrades to a working log instead of a permanently silent mod.
    ///
    /// Every write is wrapped: a logging failure must never affect the companion.
    /// </summary>
    internal static class BuddyLog
    {
        private static readonly object Gate = new object();
        private static string _path;
        private static bool _disabled;
        private static string _triedPaths = string.Empty;

        internal static void Info(string message) { Write("INFO ", message); }
        internal static void Warn(string message) { Write("WARN ", message); }
        internal static void Error(string message) { Write("ERROR", message); }
        internal static void Error(string message, Exception ex)
        {
            Write("ERROR", message + " :: " + ex.GetType().Name + ": " + ex.Message);
        }
        internal static void Blank() { Write(null, null); }

        /// <summary>Which file the log actually landed in ("(none)" when logging is off).</summary>
        internal static string ActivePath { get { return _path ?? "(none)"; } }

        /// <summary>Per-directory failure reasons, for when logging could not start at all.</summary>
        internal static string FailureDetail { get { return _triedPaths; } }

        private static string ResolvePath()
        {
            string assemblyDir;
            try { assemblyDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "."; }
            catch (Exception) { assemblyDir = "."; }

            var dirs = new List<string>();

            // 1. 桌面优先。
            //
            // 这一点是踩过坑才定下来的：SHVDN 会把 scripts\ 里的程序集**影子复制**到
            // %LOCALAPPDATA%\assembly\dl3\... 再加载，于是 Assembly.Location 指向那个
            // 缓存目录，它的父目录恰好可写 —— 结果日志被写进了
            //   ...\assembly\dl3\<乱码>\<乱码>\GTA5Buddy.log
            // 一个谁也找不到的深层路径（实测那里躺了 220 KB 的诊断日志）。
            // 桌面是用户一定能找到的位置，所以放在第一位。
            try
            {
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(profile))
                {
                    dirs.Add(System.IO.Path.Combine(profile, "Desktop"));
                    dirs.Add(System.IO.Path.Combine(profile, "Documents"));
                }
            }
            catch (Exception) { }

            // 2. 游戏根目录：影子复制之后 assemblyDir 已经不是 scripts\ 了，
            //    所以这里单独尝试从进程路径推一次游戏目录，方便就地排查。
            try
            {
                string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                string gameDir = System.IO.Path.GetDirectoryName(exe);
                if (!string.IsNullOrEmpty(gameDir)) dirs.Add(gameDir);
            }
            catch (Exception) { }

            // 3. the game root: parent of scripts\, proven writable by SHV/SHVDN's own logs
            try
            {
                DirectoryInfo parent = new DirectoryInfo(assemblyDir).Parent;
                if (parent != null && parent.Exists) dirs.Add(parent.FullName);
            }
            catch (Exception) { }

            // 4. next to the assembly (classic SHVDN convention)
            dirs.Add(assemblyDir);

            // 5. the user profile: Desktop / Documents / UserProfile. In practice the game
            //    folder and the scripts folder both turned out to be unwritable for the game
            //    process, which left the mod completely silent; a profile path is the last
            //    place that is reliably writable, so the diagnostics stay readable.
            try
            {
                string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(profile))
                {
                    dirs.Add(System.IO.Path.Combine(profile, "Desktop"));
                    dirs.Add(System.IO.Path.Combine(profile, "Documents"));
                    dirs.Add(profile);
                }
            }
            catch (Exception) { }

            // 4. temp, as a final resort
            try { dirs.Add(System.IO.Path.GetTempPath()); }
            catch (Exception) { }

            var tried = new StringBuilder();

            foreach (string dir in dirs)
            {
                string candidate;
                try { candidate = System.IO.Path.Combine(dir, "GTA5Buddy.log"); }
                catch (Exception) { continue; }

                try
                {
                    // Probe with a real append: creates the file when the directory is
                    // writable, and is harmless when it already exists.
                    File.AppendAllText(candidate, string.Empty);
                    return candidate;
                }
                catch (Exception ex)
                {
                    tried.Append(dir).Append(" => ").Append(ex.GetType().Name).Append("; ");
                }
            }

            _triedPaths = tried.ToString();
            return null;
        }

        private static string Resolve()
        {
            if (_disabled) return null;
            if (_path != null) return _path;

            lock (Gate)
            {
                if (_path == null && !_disabled)
                {
                    _path = ResolvePath();
                    if (_path == null) _disabled = true;
                }
            }
            return _path;
        }

        private static void Write(string level, string message)
        {
            string path = Resolve();
            if (path == null) return;

            try
            {
                lock (Gate)
                {
                    var sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    if (level != null)
                    {
                        sb.Append(" [").Append(level).Append("] ").Append(message);
                    }
                    sb.AppendLine();
                    File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Never let diagnostics break gameplay. Re-probe next time in case the
                // folder became writable or the handle was transiently locked.
                _path = null;
            }
        }
    }
}
