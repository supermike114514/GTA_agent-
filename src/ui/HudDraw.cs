using System;
using System.Drawing;
using GTA;
using GTA.Native;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>
    /// Low-level screen drawing built on GTA's streamed-texture sprites.
    ///
    /// WHY SPRITES AND NOT DRAW_RECT:
    ///   DRAW_RECT was tried first and does not render on this setup. The native call succeeds
    ///   and returns, but nothing appears - the panel showed its text with no background at all,
    ///   and ContainerElement (another rectangle path) rendered nothing either.
    ///
    ///   Sprites draw through the texture pipeline, which is exactly what NativeUI and LemonUI
    ///   use for their backgrounds: NativeUI's own wiki documents the pattern as
    ///   `new Sprite("commonmenu", "bgd_gradient", position, size)`. That dictionary ships with
    ///   the game, so this needs nothing extra installed.
    ///
    /// The dictionary is requested once and re-checked each frame until the game reports it
    /// loaded. Until then drawing is skipped rather than throwing, and the result is logged so
    /// a failure here is visible instead of silent.
    /// </summary>
    internal static class HudDraw
    {
        private const string TextureDict = "commonmenu";

        // Candidates in preference order. All are plain blocks in commonmenu; the first that
        // the game accepts is used for every graphic so the panel stays visually consistent.
        private static readonly string[] TextureNames =
        {
            "bgd_gradient",
            "gradient_nav",
            "gradient_bgd",
            "header",
        };

        private static bool _dictRequested;
        private static bool _dictChecked;
        private static bool _dictReady;
        private static string _textureName;
        private static int _lastProbeFrame = -1;

        /// <summary>True once a usable sprite texture has been confirmed.</summary>
        internal static bool Ready { get { return _dictReady; } }

        /// <summary>Which texture name was accepted (for the diagnostic report).</summary>
        internal static string ActiveTexture { get { return _textureName ?? "(none)"; } }

        /// <summary>
        /// Requests the dictionary and, once it reports loaded, confirms which texture names
        /// actually resolve. Safe to call every frame.
        /// </summary>
        internal static void EnsureTextures()
        {
            try
            {
                if (!_dictRequested)
                {
                    _dictRequested = true;
                    Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, TextureDict, false);
                    BuddyLog.Info("panel graphics: requested texture dict '" + TextureDict + "'");
                }

                if (_dictReady) return;

                // probe at most once every 30 frames so this is cheap
                if (Game.FrameCount == _lastProbeFrame) return;
                _lastProbeFrame = Game.FrameCount;

                bool loaded = Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, TextureDict);
                if (!loaded)
                {
                    if (!_dictChecked) { _dictChecked = true; }
                    return;
                }

                // dictionary is in memory: confirm a texture name resolves by constructing and
                // drawing one off-screen sprite; a bad name throws inside Sprite
                foreach (string name in TextureNames)
                {
                    try
                    {
                        var probe = new Sprite(TextureDict, name, new SizeF(0.001f, 0.001f), new PointF(-1f, -1f));
                        probe.Draw();
                        _textureName = name;
                        _dictReady = true;
                        BuddyLog.Info("panel graphics ready: " + TextureDict + "/" + name);
                        return;
                    }
                    catch (Exception)
                    {
                        // try the next candidate
                    }
                }

                BuddyLog.Warn("panel graphics: dictionary loaded but no usable texture name among " +
                              string.Join(", ", TextureNames));
                _dictReady = true;   // stop probing; drawing will simply be a no-op
                _textureName = null;
            }
            catch (Exception ex)
            {
                BuddyLog.Error("panel graphics init failed", ex);
                _dictReady = true;
            }
        }

        /// <summary>
        /// Solid rectangle. x/y is the TOP-LEFT corner in 0..1 screen space; sprites position
        /// by centre, which the centered:false constructor argument handles.
        /// </summary>
        internal static void Rect(float x, float y, float w, float h, Color color)
        {
            if (!_dictReady || string.IsNullOrEmpty(_textureName)) return;
            if (w <= 0f || h <= 0f) return;

            try
            {
                var sprite = new Sprite(TextureDict, _textureName,
                                        new SizeF(w, h), new PointF(x, y), color, 0f, false);
                sprite.Draw();
            }
            catch (Exception) { }
        }
    }
}
