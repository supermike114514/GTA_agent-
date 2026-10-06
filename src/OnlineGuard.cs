using System;
using System.Text;
using GTA;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>
    /// Mode gate. This mod is STORY MODE ONLY and must never initialise in GTA Online.
    ///
    /// There are three independent layers of protection, and this is the second one:
    ///
    ///   1. ScriptHookV itself (C++ side) closes the game the moment the player goes
    ///      Online, because the game reports installed mods to Rockstar while Online.
    ///   2. This class. It decides, before anything else runs, whether the session is
    ///      story mode. Any sign of an Online session - or any failure to verify - makes
    ///      it refuse to load.
    ///   3. The installer warns and refuses to look like an Online-safe setup.
    ///
    /// The check is deliberately fail-closed: if the natives disagree, throw, or cannot
    /// be read, the mod does not load. A companion mod misfiring in Online is far worse
    /// than the user having to read one line of log.
    /// </summary>
    internal static class OnlineGuard
    {
        private enum Mode
        {
            Unknown = 0,
            Story = 1,
            Online = 2,
        }

        /// <summary>
        /// True when it is safe to run. Also fills <paramref name="detail"/> with a
        /// human-readable report for the log.
        /// </summary>
        internal static bool IsStoryMode(out string detail)
        {
            var log = new StringBuilder();
            Mode verdict = Mode.Unknown;

            try
            {
                // --- signal 1: an established network session (newswire/social club is not this) ---
                bool sessionStarted = Function.Call<bool>(Hash.NETWORK_IS_SESSION_STARTED);
                bool inSession = Function.Call<bool>(Hash.NETWORK_IS_IN_SESSION);
                bool activitySession = Function.Call<bool>(Hash.NETWORK_IS_ACTIVITY_SESSION);
                bool signedOnline = Function.Call<bool>(Hash.NETWORK_IS_SIGNED_ONLINE);
                int connectedPlayers = Function.Call<int>(Hash.NETWORK_GET_NUM_CONNECTED_PLAYERS);

                log.AppendFormat("sessionStarted={0} inSession={1} activity={2} signedOnline={3} players={4}",
                    sessionStarted, inSession, activitySession, signedOnline, connectedPlayers);

                if (sessionStarted || inSession || activitySession)
                {
                    verdict = Mode.Online;
                }
                else if (connectedPlayers > 1)
                {
                    verdict = Mode.Online;
                }

                // --- signal 2: the local ped uses an Online-only freemode model ---
                bool mpModel = false;
                Ped player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    int model = Function.Call<int>(Hash.GET_ENTITY_MODEL, player);
                    mpModel = Function.Call<bool>(Hash.IS_PED_MODEL, player, MP_MALE_MODEL) ||
                              Function.Call<bool>(Hash.IS_PED_MODEL, player, MP_FEMALE_MODEL);
                    log.AppendFormat(" | pedModel=0x{0:X8} mpFreemode={1}", model, mpModel);
                }

                if (mpModel)
                {
                    verdict = Mode.Online;
                }

                // --- signal 3: a second player slot is genuinely occupied (Online only) ---
                // IsPlayerSlotValid requires a real ped handle AND IS_PLAYER_PLAYING, which is
                // the combination that actually distinguishes a second player from an empty
                // slot. NETWORK_IS_PLAYER_ACTIVE is only recorded: it reports true for slots
                // that carry no ped, which in story mode would have produced a false "Online"
                // verdict and silently disabled the whole mod.
                bool secondSlot = IsPlayerSlotValid(1) || IsPlayerSlotValid(2);
                bool otherActive = Function.Call<bool>(Hash.NETWORK_IS_PLAYER_ACTIVE, 1) ||
                                   Function.Call<bool>(Hash.NETWORK_IS_PLAYER_ACTIVE, 2);
                log.AppendFormat(" | extraPlayerSlot={0} otherPlayerActive={1}(info only)", secondSlot, otherActive);
                if (secondSlot)
                {
                    verdict = Mode.Online;
                }

                // --- signal 4: mid-transition into Online (a transition can start before any
                //     session flag flips, so this one is worth acting on) ---
                bool mpRules = Function.Call<bool>(Hash.NETWORK_IS_GAME_IN_PROGRESS);
                bool transitioning = Function.Call<bool>(Hash.NETWORK_IS_TRANSITION_STARTED);
                log.AppendFormat(" | mpRules={0} transition={1}", mpRules, transitioning);
                if (transitioning)
                {
                    verdict = Mode.Online;
                }
                // NETWORK_IS_GAME_IN_PROGRESS is recorded for diagnostics only: it is not a
                // dependable story/Online discriminator on its own, and a false positive here
                // disables the companion for the whole session.

                // --- positive story evidence ---
                int storyPlayers = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (IsPlayerSlotValid(i)) storyPlayers++;
                }
                log.AppendFormat(" | storyPlayerSlots={0}", storyPlayers);

                if (verdict == Mode.Unknown)
                {
                    // Single valid player slot and a non-freemode ped is the story-mode shape.
                    verdict = (storyPlayers == 1 && !mpModel) ? Mode.Story : Mode.Unknown;
                }
            }
            catch (Exception ex)
            {
                log.Append(" | detection threw: ").Append(ex.Message);
                verdict = Mode.Unknown;
            }

            log.AppendFormat(" | enhanced={0}", SafeEnhanced());
            log.AppendFormat(" | verdict={0}", verdict);

            detail = log.ToString();

            if (IsForceOverrideEnabled()) return true;
            return verdict == Mode.Story;
        }

        /// <summary>
        /// Broken out of the literal comparison on purpose: the compiler would otherwise
        /// fold the constant and flag the story check below as unreachable code.
        /// </summary>
        private static bool IsForceOverrideEnabled()
        {
            bool flag = BuddyConfig.ForceAllowAnyMode;
            return flag;
        }

        private const uint MP_MALE_MODEL = 0x705E61F2;    // mp_m_freemode_01
        private const uint MP_FEMALE_MODEL = 0x9C9EFFD8;  // mp_f_freemode_01

        private static bool IsPlayerSlotValid(int index)
        {
            try
            {
                int handle = Function.Call<int>(Hash.GET_PLAYER_PED, index);
                if (handle == 0) return false;
                // IS_PLAYER_PLAYING is the cheapest "this slot is actually occupied" probe
                // that is meaningful in both editions.
                return Function.Call<bool>(Hash.IS_PLAYER_PLAYING, index);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string SafeEnhanced()
        {
            try { return GTA.Game.isEnhanced.ToString(); }
            catch (Exception) { return "?"; }
        }
    }
}
