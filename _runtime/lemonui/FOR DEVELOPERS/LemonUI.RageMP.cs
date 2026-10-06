// LemonUI for RageMP
// Generated on 2025-05-22 20:52:21.855814

#define RAGEMP

using Font = RAGE.Game.Font;
using InstructionalButton = LemonUI.Scaleform.InstructionalButton;
using LemonUI.Elements;
using LemonUI.Scaleform;
using LemonUI.Tools;
using RAGE.Game;
using RAGE.NUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

// SafeZoneChangedEventArgs.cs
namespace LemonUI
{
    /// <summary>
    /// Represents the information after a Safe Zone Change in the game.
    /// </summary>
    public class SafeZoneChangedEventArgs
    {
        #region Properties

        /// <summary>
        /// The raw Safezone size before the change.
        /// </summary>
        public float Before { get; }
        /// <summary>
        /// The Safezone size after the change.
        /// </summary>
        public float After { get; }

        #endregion

        #region Constructors

        internal SafeZoneChangedEventArgs(float before, float after)
        {
            Before = before;
            After = after;
        }

        #endregion
    }
}

// CancelEventHandler.cs
namespace LemonUI // Previously System.ComponentModel
{
    /// <summary>
    /// Represents the method that will handle the event raised when canceling an event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="CancelEventArgs"/> that contains the event data.</param>
    public delegate void CancelEventHandler(object sender, CancelEventArgs e);
}

// ResolutionChangedEventArgs.cs
namespace LemonUI
{
    /// <summary>
    /// Represents the information after a Resolution Change in the game.
    /// </summary>
    public class ResolutionChangedEventArgs
    {
        #region Properties

        /// <summary>
        /// The Game Resolution before it was changed.
        /// </summary>
        public SizeF Before { get; }
        /// <summary>
        /// The Game Resolution after it was changed.
        /// </summary>
        public SizeF After { get; }

        #endregion

        #region Constructors

        internal ResolutionChangedEventArgs(SizeF before, SizeF after)
        {
            Before = before;
            After = after;
        }

        #endregion
    }
}

// IRecalculable.cs
namespace LemonUI
{
    /// <summary>
    /// Interface for classes that have values that need to be recalculated on resolution changes.
    /// </summary>
    public interface IRecalculable
    {
        #region Functions

        /// <summary>
        /// Recalculates the values.
        /// </summary>
        void Recalculate();

        #endregion
    }
}

// Screen.cs
namespace LemonUI
{
    /// <summary>
    /// Contains a set of tools to work with the screen information.
    /// </summary>
    [Obsolete("Use the LemonUI.Tools and LemonUI.Math namespaces.")]
    public static class Screen
    {
        #region Properties

        /// <summary>
        /// The Aspect Ratio of the screen resolution.
        /// </summary>
        public static float AspectRatio => GameScreen.AspectRatio;
#if ALTV
        /// <summary>
        /// Gets the actual Screen resolution the game is being rendered at.
        /// </summary>
        public static Size Resolution => GameScreen.AbsoluteResolution.ToSize();
#endif
        /// <summary>
        /// The location of the cursor on screen between 0 and 1.
        /// </summary>
        public static PointF CursorPositionRelative => GameScreen.Cursor.ToRelative();

        #endregion

        #region Functions

        /// <summary>
        /// Converts a relative resolution into one scaled to 1080p.
        /// </summary>
        /// <param name="relativeX">The relative value of X.</param>
        /// <param name="relativeY">The relative value of Y.</param>
        /// <param name="absoluteX">The value of X scaled to 1080p.</param>
        /// <param name="absoluteY">The value of Y scaled to 1080p.</param>
        public static void ToAbsolute(float relativeX, float relativeY, out float absoluteX, out float absoluteY)
        {
            absoluteX = relativeX.ToXScaled();
            absoluteY = relativeY.ToYScaled();
        }
        /// <summary>
        /// Converts a 1080p-based resolution into relative values.
        /// </summary>
        /// <param name="absoluteX">The 1080p based X coord.</param>
        /// <param name="absoluteY">The 1080p based Y coord.</param>
        /// <param name="relativeX">The value of X converted to relative.</param>
        /// <param name="relativeY">The value of Y converted to relative.</param>
        public static void ToRelative(float absoluteX, float absoluteY, out float relativeX, out float relativeY)
        {
            relativeX = absoluteX.ToXRelative();
            relativeY = absoluteY.ToYRelative();
        }
        /// <summary>
        /// Checks if the cursor is inside of the specified area.
        /// </summary>
        /// <remarks>
        /// This function takes values scaled to 1080p and is aware of the alignment set via SET_SCRIPT_GFX_ALIGN.
        /// </remarks>
        /// <param name="pos">The start of the area.</param>
        /// <param name="size">The size of the area to check.</param>
        /// <returns><see langword="true"/> if the cursor is in the specified bounds, <see langword="false"/> otherwise.</returns>
        public static bool IsCursorInArea(PointF pos, SizeF size) => IsCursorInArea(pos.X, pos.Y, size.Width, size.Height);
        /// <summary>
        /// Checks if the cursor is inside of the specified area.
        /// </summary>
        /// <remarks>
        /// This function takes values scaled to 1080p and is aware of the alignment set via SET_SCRIPT_GFX_ALIGN.
        /// </remarks>
        /// <param name="x">The start X position.</param>
        /// <param name="y">The start Y position.</param>
        /// <param name="width">The height of the search area from X.</param>
        /// <param name="height">The height of the search area from Y.</param>
        /// <returns><see langword="true"/> if the cursor is in the specified bounds, <see langword="false"/> otherwise.</returns>
        public static bool IsCursorInArea(float x, float y, float width, float height)
        {
            // intentionally kept this way to avoid breaking backwards compatibility
            PointF realPos = GetRealPosition(x, y).ToRelative();
            return GameScreen.IsCursorInArea(realPos.X, realPos.Y, width, height);
        }
        /// <summary>
        /// Converts the specified position into one that is aware of <see cref="SetElementAlignment(GFXAlignment, GFXAlignment)"/>.
        /// </summary>
        /// <param name="og">The original 1080p based position.</param>
        /// <returns>A new 1080p based position that is aware of the the Alignment.</returns>
        public static PointF GetRealPosition(PointF og) => GetRealPosition(og.X, og.Y);
        /// <summary>
        /// Converts the specified position into one that is aware of <see cref="SetElementAlignment(GFXAlignment, GFXAlignment)"/>.
        /// </summary>
        /// <param name="x">The 1080p based X position.</param>
        /// <param name="y">The 1080p based Y position.</param>
        /// <returns>A new 1080p based position that is aware of the the Alignment.</returns>
        public static PointF GetRealPosition(float x, float y) => SafeZone.GetSafePosition(x, y);
        /// <summary>
        /// Shows the cursor during the current game frame.
        /// </summary>
        public static void ShowCursorThisFrame() => GameScreen.ShowCursorThisFrame();
        /// <summary>
        /// Sets the alignment of game elements like <see cref="Elements.ScaledRectangle"/>, <see cref="Elements.ScaledText"/> and <see cref="Elements.ScaledTexture"/>.
        /// </summary>
        /// <param name="horizontal">The Horizontal alignment of the items.</param>
        /// <param name="vertical">The vertical alignment of the items.</param>
        public static void SetElementAlignment(Alignment horizontal, GFXAlignment vertical) => SafeZone.SetAlignment(horizontal, vertical);
        /// <summary>
        /// Sets the alignment of game elements like <see cref="Elements.ScaledRectangle"/>, <see cref="Elements.ScaledText"/> and <see cref="Elements.ScaledTexture"/>.
        /// </summary>
        /// <param name="horizontal">The Horizontal alignment of the items.</param>
        /// <param name="vertical">The vertical alignment of the items.</param>
        public static void SetElementAlignment(GFXAlignment horizontal, GFXAlignment vertical) => SafeZone.SetAlignment(horizontal, vertical);
        /// <summary>
        /// Resets the alignment of the game elements.
        /// </summary>
        public static void ResetElementAlignment() => SafeZone.ResetAlignment();

        #endregion
    }
}

// SafeZoneChangedEventHandler.cs
namespace LemonUI
{
    /// <summary>
    /// Represents the method that reports a Safe Zone change in the Game Settings.
    /// </summary>
    /// <param name="sender">The source of the event event.</param>
    /// <param name="e">A <see cref="ResolutionChangedEventArgs"/> containing the Previous and Current Safe Zone.</param>
    public delegate void SafeZoneChangedEventHandler(object sender, SafeZoneChangedEventArgs e);
}

// IProcessable.cs
namespace LemonUI
{
    /// <summary>
    /// Interface for items that can be processed in an Object Pool.
    /// </summary>
    public interface IProcessable
    {
        #region Properties

        /// <summary>
        /// If this processable item is visible on the screen.
        /// </summary>
        bool Visible { get; set; }

        #endregion

        #region Functions

        /// <summary>
        /// Processes the object.
        /// </summary>
        void Process();

        #endregion
    }
}

// CancelEventArgs.cs
namespace LemonUI // Previously System.ComponentModel
{
    /// <summary>
    /// EventArgs used to describe a cancel event.
    /// </summary>
    public class CancelEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether we should cancel the operation or not
        /// </summary>
        public bool Cancel { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public CancelEventArgs()
        {
        }
        /// <summary>
        /// Helper constructor
        /// </summary>
        /// <param name="cancel"></param>
        public CancelEventArgs(bool cancel) => Cancel = cancel;

        #endregion
    }
}

// Controls.cs
namespace LemonUI
{
    /// <summary>
    /// Tools for dealing with controls.
    /// </summary>
    internal static class Controls
    {
        #region Properties

        /// <summary>
        /// Gets if the player used a controller for the last input.
        /// </summary>
        public static bool IsUsingController
        {
            get
            {
#if FIVEM
                return !API.IsInputDisabled(2);
#elif ALTV
                return !Alt.Natives.IsUsingKeyboardAndMouse(2);
#elif RAGEMP
                return !Invoker.Invoke<bool>(Natives.IsInputDisabled, 2);
#elif RPH
                return !NativeFunction.CallByHash<bool>(0xA571D46727E2B718, 2);
#elif SHVDN3 || SHVDNC
                return !Function.Call<bool>(Hash.IS_USING_KEYBOARD_AND_MOUSE, 2);
#endif
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Checks if a control was pressed during the last frame.
        /// </summary>
        /// <param name="control">The control to check.</param>
        /// <returns>true if the control was pressed, false otherwise.</returns>
        public static bool IsJustPressed(Control control)
        {
#if FIVEM
            return API.IsDisabledControlJustPressed(0, (int)control);
#elif ALTV
            return Alt.Natives.IsDisabledControlJustPressed(0, (int)control);
#elif RAGEMP
            return Invoker.Invoke<bool>(Natives.IsDisabledControlJustPressed, 0, (int)control);
#elif RPH
            return NativeFunction.CallByHash<bool>(0x91AEF906BCA88877, 0, (int)control);
#elif SHVDN3 || SHVDNC
            return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)control);
#elif ALTV
            return Alt.Natives.IsControlJustPressed((uint)control);
#endif
        }
        /// <summary>
        /// Checks if a control is currently pressed.
        /// </summary>
        /// <param name="control">The control to check.</param>
        /// <returns>true if the control is pressed, false otherwise.</returns>
        public static bool IsPressed(Control control)
        {
#if FIVEM
            return API.IsDisabledControlPressed(0, (int)control);
#elif ALTV
            return Alt.Natives.IsDisabledControlPressed(0, (int)control);
#elif RAGEMP
            return Invoker.Invoke<bool>(Natives.IsDisabledControlJustPressed, 0, (int)control);
#elif RPH
            return NativeFunction.CallByHash<bool>(0xE2587F8CBBD87B1D, 0, (int)control);
#elif SHVDN3 || SHVDNC
            return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, (int)control);
#endif
        }
        /// <summary>
        /// Disables all of the controls during the next frame.
        /// </summary>
        public static void DisableAll(int inputGroup = 0)
        {
#if FIVEM
            API.DisableAllControlActions(inputGroup);
#elif ALTV
            Alt.Natives.DisableAllControlActions(inputGroup);
#elif RAGEMP
            Invoker.Invoke(Natives.DisableAllControlActions, inputGroup);
#elif RPH
            NativeFunction.CallByHash<int>(0x5F4B6931816E599B, inputGroup);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, inputGroup);
#endif
        }
        /// <summary>
        /// Enables a control during the next frame.
        /// </summary>
        /// <param name="control">The control to enable.</param>
        public static void EnableThisFrame(Control control)
        {
#if FIVEM
            API.EnableControlAction(0, (int)control, true);
#elif ALTV
            Alt.Natives.EnableControlAction(0, (int)control, true);
#elif RAGEMP
            Invoker.Invoke(Natives.EnableControlAction, 0, (int)control, true);
#elif RPH
            NativeFunction.CallByHash<int>(0x351220255D64C155, 0, (int)control);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)control);
#endif
        }
        /// <summary>
        /// Enables a specific set of controls during the next frame.
        /// </summary>
        /// <param name="controls">The controls to enable.</param>
        public static void EnableThisFrame(IEnumerable<Control> controls)
        {
            foreach (Control control in controls)
            {
                EnableThisFrame(control);
            }
        }
        /// <summary>
        /// Disables a control during the next frame.
        /// </summary>
        /// <param name="control">The control to disable.</param>
        public static void DisableThisFrame(Control control)
        {
#if FIVEM
            API.DisableControlAction(0, (int)control, true);
#elif ALTV
            Alt.Natives.DisableControlAction(0, (int)control, true);
#elif RAGEMP
            Invoker.Invoke(Natives.DisableControlAction, 0, (int)control, true);
#elif RPH
            NativeFunction.CallByHash<int>(0xFE99B66D079CF6BC, 0, (int)control, true);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)control, true);
#endif
        }

        #endregion
    }
}

// Sound.cs
namespace LemonUI
{
    /// <summary>
    /// Contains information for a Game Sound that is played at specific times.
    /// </summary>
    public class Sound
    {
        #region Fields

        private string set = string.Empty;
        private string file = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The ID of the sound, if is being played.
        /// </summary>
        public int Id { get; private set; } = -1;
        /// <summary>
        /// The Set where the sound is located.
        /// </summary>
        public string Set
        {
            get => set;
            set => set = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The name of the sound file.
        /// </summary>
        public string File
        {
            get => file;
            set => file = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="Sound"/> class with the specified Sound Set and File.
        /// </summary>
        /// <param name="set">The Set where the sound is located.</param>
        /// <param name="file">The name of the sound file.</param>
        public Sound(string set, string file)
        {
            Set = set ?? throw new ArgumentNullException(nameof(set));
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        #endregion

        #region Functions

        /// <summary>
        /// Plays the sound for the local <see cref="Player"/>.
        /// </summary>
        public void PlayFrontend() => PlayFrontend(true);
        /// <summary>
        /// Plays the sound for the local <see cref="Player"/>.
        /// </summary>
        /// <param name="release">If the sound ID should be automatically released.</param>
        public void PlayFrontend(bool release)
        {
#if FIVEM
            Id = API.GetSoundId();
            API.PlaySoundFrontend(Id, File, Set, true);
#elif RAGEMP
            Id = Invoker.Invoke<int>(Natives.GetSoundId);
            Invoker.Invoke(Natives.PlaySoundFrontend, Id, File, Set, true);
#elif ALTV
            Id = Alt.Natives.GetSoundId();
            Alt.Natives.PlaySoundFrontend(Id, File, Set, true);
#elif RPH
            Id = NativeFunction.CallByHash<int>(0x430386FE9BF80B45);
            NativeFunction.CallByHash<int>(0x67C540AA08E4A6F5, Id, File, Set, true);
#elif SHVDN3 || SHVDNC
            Id = Function.Call<int>(Hash.GET_SOUND_ID);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, Id, File, Set, true);
#endif

            if (release)
            {
                Release();
            }
        }
        /// <summary>
        /// Stops the audio from playing.
        /// </summary>
        public void Stop()
        {
            if (Id == -1)
            {
                return;
            }

#if FIVEM
            API.StopSound(Id);
#elif RAGEMP
            Invoker.Invoke(Natives.StopSound, Id);
#elif RPH
            NativeFunction.CallByHash<int>(0xA3B0C41BA5CC0BB5, Id);
#elif ALTV
            Alt.Natives.StopSound(Id);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.STOP_SOUND, Id);
#endif
            Release();
        }
        /// <summary>
        /// Releases the Sound ID.
        /// </summary>
        public void Release()
        {
            if (Id == -1)
            {
                return;
            }

#if FIVEM
            API.ReleaseSoundId(Id);
#elif RAGEMP
            Invoker.Invoke(Natives.ReleaseSoundId, Id);
#elif RPH
            NativeFunction.CallByHash<int>(0x353FC880830B88FA, Id);
#elif ALTV
            Alt.Natives.ReleaseSoundId(Id);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.RELEASE_SOUND_ID, Id);
#endif
            Id = -1;
        }

        #endregion
    }
}

// IContainer.cs
namespace LemonUI
{
    /// <summary>
    /// Represents a container that can hold other UI Elements.
    /// </summary>
    /// <typeparam name="T">The type of Element that this container handles.</typeparam>
    public interface IContainer<T> : IRecalculable, IProcessable
    {
        #region Functions

        /// <summary>
        /// Adds the specified item into the Container.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void Add(T item);
        /// <summary>
        /// Removes the item from the container.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        void Remove(T item);
        /// <summary>
        /// Removes all of the items that match the function.
        /// </summary>
        /// <param name="func">The function to check items.</param>
        void Remove(Func<T, bool> func);
        /// <summary>
        /// Clears all of the items in the container.
        /// </summary>
        void Clear();
        /// <summary>
        /// Checks if the item is part of the container.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><see langword="true"/> if the item is in this container, <see langword="false"/> otherwise.</returns>
        bool Contains(T item);

        #endregion
    }
}

// Control.cs
namespace LemonUI
{
    public enum Control
    {
        NextCamera = 0,
        LookLr = 1,
        LookUd = 2,
        LookUpOnly = 3,
        LookDownOnly = 4,
        LookLeftOnly = 5,
        LookRightOnly = 6,
        CinematicSlowmo = 7,
        ScriptedFlyUd = 8,
        ScriptedFlyLr = 9,
        ScriptedFlyZup = 10,
        ScriptedFlyZdown = 11,
        WeaponWheelUd = 12,
        WeaponWheelLr = 13,
        WeaponWheelNext = 14,
        WeaponWheelPrev = 15,
        SelectNextWeapon = 16,
        SelectPrevWeapon = 17,
        SkipCutscene = 18,
        CharacterWheel = 19,
        MultiplayerInfo = 20,
        Sprint = 21,
        Jump = 22,
        Enter = 23,
        Attack = 24,
        Aim = 25,
        LookBehind = 26,
        Phone = 27,
        SpecialAbility = 28,
        SpecialAbilitySecondary = 29,
        MoveLr = 30,
        MoveUd = 31,
        MoveUpOnly = 32,
        MoveDownOnly = 33,
        MoveLeftOnly = 34,
        MoveRightOnly = 35,
        Duck = 36,
        SelectWeapon = 37,
        Pickup = 38,
        SniperZoom = 39,
        SniperZoomInOnly = 40,
        SniperZoomOutOnly = 41,
        SniperZoomInSecondary = 42,
        SniperZoomOutSecondary = 43,
        Cover = 44,
        Reload = 45,
        Talk = 46,
        Detonate = 47,
        HudSpecial = 48,
        Arrest = 49,
        AccurateAim = 50,
        Context = 51,
        ContextSecondary = 52,
        WeaponSpecial = 53,
        WeaponSpecialTwo = 54,
        Dive = 55,
        DropWeapon = 56,
        DropAmmo = 57,
        ThrowGrenade = 58,
        VehMoveLr = 59,
        VehMoveUd = 60,
        VehMoveUpOnly = 61,
        VehMoveDownOnly = 62,
        VehMoveLeftOnly = 63,
        VehMoveRightOnly = 64,
        VehSpecial = 65,
        VehGunLr = 66,
        VehGunUd = 67,
        VehAim = 68,
        VehAttack = 69,
        VehAttack2 = 70,
        VehAccelerate = 71,
        VehBrake = 72,
        VehDuck = 73,
        VehHeadlight = 74,
        VehExit = 75,
        VehHandbrake = 76,
        VehHotwireLeft = 77,
        VehHotwireRight = 78,
        VehLookBehind = 79,
        VehCinCam = 80,
        VehNextRadio = 81,
        VehPrevRadio = 82,
        VehNextRadioTrack = 83,
        VehPrevRadioTrack = 84,
        VehRadioWheel = 85,
        VehHorn = 86,
        VehFlyThrottleUp = 87,
        VehFlyThrottleDown = 88,
        VehFlyYawLeft = 89,
        VehFlyYawRight = 90,
        VehPassengerAimRight = 91,
        VehPassengerAttack = 92,
        VehSpecialAbilityFranklin = 93,
        VehStuntUD = 94,
        VehCinematicUD = 95,
        VehCinematicUpOnly = 96,
        VehCinematicDownOnly = 97,
        VehCinematicLR = 98,
        VehSelectNextWeapon = 99,
        VehSelectPrevWeapon = 100,
        VehRoof = 101,
        VehJump = 102,
        VehGrapplingHook = 103,
        VehShuffle = 104,
        VehDropProjectile = 105,
        VehMouseControlOverride = 106,
        VehFlyRollLR = 107,
        VehFlyRollLeftOnly = 108,
        VehFlyRollRightOnly = 109,
        VehFlyPitchUD = 110,
        VehFlyPitchUpOnly = 111,
        VehFlyPitchDownOnly = 112,
        VehFlyUndercarriage = 113,
        VehFlyAttack = 114,
        VehFlySelectNextWeapon = 115,
        VehFlySelectPrevWeapon = 116,
        VehFlySelectTargetLeft = 117,
        VehFlySelectTargetRight = 118,
        VehFlyVerticalFlightMode = 119,
        VehFlyDuck = 120,
        VehFlyAttackCamera = 121,
        VehFlyMouseControlOverride = 122,
        VehSubTurnLR = 123,
        VehSubTurnLeftOnly = 124,
        VehSubTurnRightOnly = 125,
        VehSubPitchUD = 126,
        VehSubPitchUpOnly = 127,
        VehSubPitchDownOnly = 128,
        VehSubThrottleUp = 129,
        VehSubThrottleDown = 130,
        VehSubAscend = 131,
        VehSubDescend = 132,
        VehSubTurnHardLeft = 133,
        VehSubTurnHardRight = 134,
        VehSubMouseControlOverride = 135,
        VehPushbikePedal = 136,
        VehPushbikeSprint = 137,
        VehPushbikeFrontBrake = 138,
        VehPushbikeRearBrake = 139,
        MeleeAttackLight = 140,
        MeleeAttackHeavy = 141,
        MeleeAttackAlternate = 142,
        MeleeBlock = 143,
        ParachuteDeploy = 144,
        ParachuteDetach = 145,
        ParachuteTurnLR = 146,
        ParachuteTurnLeftOnly = 147,
        ParachuteTurnRightOnly = 148,
        ParachutePitchUD = 149,
        ParachutePitchUpOnly = 150,
        ParachutePitchDownOnly = 151,
        ParachuteBrakeLeft = 152,
        ParachuteBrakeRight = 153,
        ParachuteSmoke = 154,
        ParachutePrecisionLanding = 155,
        Map = 156,
        SelectWeaponUnarmed = 157,
        SelectWeaponMelee = 158,
        SelectWeaponHandgun = 159,
        SelectWeaponShotgun = 160,
        SelectWeaponSMG = 161,
        SelectWeaponAutoRifle = 162,
        SelectWeaponSniper = 163,
        SelectWeaponHeavy = 164,
        SelectWeaponSpecial = 165,
        SelectCharacterMichael = 166,
        SelectCharacterFranklin = 167,
        SelectCharacterTrevor = 168,
        SelectCharacterMultiplayer = 169,
        SaveReplayClip = 170,
        SpecialAbilityPC = 171,
        CellPhoneUp = 172,
        CellPhoneDown = 173,
        CellPhoneLeft = 174,
        CellPhoneRight = 175,
        CellPhoneSelect = 176,
        CellPhoneCancel = 177,
        CellPhoneOption = 178,
        CellPhoneExtraOption = 179,
        CellPhoneScrollForward = 180,
        CellPhoneScrollBackward = 181,
        CellPhoneCameraFocusLock = 182,
        CellPhoneCameraGrid = 183,
        CellPhoneCameraSelfie = 184,
        CellPhoneCameraDOF = 185,
        CellPhoneCameraExpression = 186,
        FrontendDown = 187,
        FrontendUp = 188,
        FrontendLeft = 189,
        FrontendRight = 190,
        FrontendRDown = 191,
        FrontendRUp = 192,
        FrontendRLeft = 193,
        FrontendRRight = 194,
        FrontendAxisX = 195,
        FrontendAxisY = 196,
        FrontendRightAxisX = 197,
        FrontendRightAxisY = 198,
        FrontendPause = 199,
        FrontendPauseAlternate = 200,
        FrontendAccept = 201,
        FrontendCancel = 202,
        FrontendX = 203,
        FrontendY = 204,
        FrontendLB = 205,
        FrontendRB = 206,
        FrontendLT = 207,
        FrontendRT = 208,
        FrontendLS = 209,
        FrontendRS = 210,
        FrontendLeaderboard = 211,
        FrontendSocialClub = 212,
        FrontendSocialClubSecondary = 213,
        FrontendDelete = 214,
        FrontendEndscreenAccept = 215,
        FrontendEndscreenExpand = 216,
        FrontendSelect = 217,
        ScriptLeftAxisX = 218,
        ScriptLeftAxisY = 219,
        ScriptRightAxisX = 220,
        ScriptRightAxisY = 221,
        ScriptRUp = 222,
        ScriptRDown = 223,
        ScriptRLeft = 224,
        ScriptRRight = 225,
        ScriptLB = 226,
        ScriptRB = 227,
        ScriptLT = 228,
        ScriptRT = 229,
        ScriptLS = 230,
        ScriptRS = 231,
        ScriptPadUp = 232,
        ScriptPadDown = 233,
        ScriptPadLeft = 234,
        ScriptPadRight = 235,
        ScriptSelect = 236,
        CursorAccept = 237,
        CursorCancel = 238,
        CursorX = 239,
        CursorY = 240,
        CursorScrollUp = 241,
        CursorScrollDown = 242,
        EnterCheatCode = 243,
        InteractionMenu = 244,
        MPTextChatAll = 245,
        MPTextChatTeam = 246,
        MPTextChatFriends = 247,
        MPTextChatCrew = 248,
        PushToTalk = 249,
        CreatorLs = 250,
        CreatorRs = 251,
        CreatorLt = 252,
        CreatorRt = 253,
        CreatorMenuToggle = 254,
        CreatorAccept = 255,
        CreatorDelete = 256,
        Attack2 = 257,
        RappelJump = 258,
        RappelLongJump = 259,
        RappelSmashWindow = 260,
        PrevWeapon = 261,
        NextWeapon = 262,
        MeleeAttack1 = 263,
        MeleeAttack2 = 264,
        Whistle = 265,
        MoveLeft = 266,
        MoveRight = 267,
        MoveUp = 268,
        MoveDown = 269,
        LookLeft = 270,
        LookRight = 271,
        LookUp = 272,
        LookDown = 273,
        SniperZoomIn = 274,
        SniperZoomOut = 275,
        SniperZoomInAlternate = 276,
        SniperZoomOutAlternate = 277,
        VehMoveLeft = 278,
        VehMoveRight = 279,
        VehMoveUp = 280,
        VehMoveDown = 281,
        VehGunLeft = 282,
        VehGunRight = 283,
        VehGunUp = 284,
        VehGunDown = 285,
        VehLookLeft = 286,
        VehLookRight = 287,
        ReplayStartStopRecording = 288,
        ReplayStartStopRecordingSecondary = 289,
        ScaledLookLr = 290,
        ScaledLookUd = 291,
        ScaledLookUpOnly = 292,
        ScaledLookDownOnly = 293,
        ScaledLookLeftOnly = 294,
        ScaledLookRightOnly = 295,
        ReplayMarkerDelete = 296,
        ReplayClipDelete = 297,
        ReplayPause = 298,
        ReplayRewind = 299,
        ReplayFfwd = 300,
        ReplayNewmarker = 301,
        ReplayRecord = 302,
        ReplayScreenshot = 303,
        ReplayHidehud = 304,
        ReplayStartpoint = 305,
        ReplayEndpoint = 306,
        ReplayAdvance = 307,
        ReplayBack = 308,
        ReplayTools = 309,
        ReplayRestart = 310,
        ReplayShowhotkey = 311,
        ReplayCyclemarkerleft = 312,
        ReplayCyclemarkerright = 313,
        ReplayFovincrease = 314,
        ReplayFovdecrease = 315,
        ReplayCameraup = 316,
        ReplayCameradown = 317,
        ReplaySave = 318,
        ReplayToggletime = 319,
        ReplayToggletips = 320,
        ReplayPreview = 321,
        ReplayToggleTimeline = 322,
        ReplayTimelinePickupClip = 323,
        ReplayTimelineDuplicateClip = 324,
        ReplayTimelinePlaceClip = 325,
        ReplayCtrl = 326,
        ReplayTimelineSave = 327,
        ReplayPreviewAudio = 328,
        VehDriveLook = 329,
        VehDriveLook2 = 330,
        VehFlyAttack2 = 331,
        RadioWheelUd = 332,
        RadioWheelLr = 333,
        VehicleSlowmoUd = 334,
        VehSlowmoUpOnly = 335,
        VehSlowmoDownOnly = 336,
        VehHydraulicsControlToggle = 337,
        VehHydraulicsControlLeft = 338,
        VehHydraulicsControlRight = 339,
        VehHydraulicsControlUp = 340,
        VehHydraulicsControlDown = 341,
        VehHydraulicsControlUD = 342,
        VehHydraulicsControlLR = 343,
        SwitchVisor = 344,
        VehMeleeHold = 345,
        VehMeleeLeft = 346,
        VehMeleeRight = 347,
        MapPOI = 348,
        ReplaySnapmaticPhoto = 349,
        VehCarJump = 350,
        VehRocketBoost = 351,
        VehFlyBoost = 352,
        VehParachute = 353,
        VehBikeWings = 354,
        VehFlyBombBay = 355,
        VehFlyCounter = 356,
        VehTransform = 357
    }
}

// ObjectPool.cs
namespace LemonUI
{
    /// <summary>
    /// Manager for Menus and Items.
    /// </summary>
    public class ObjectPool : IEnumerable<IProcessable>
    {
        #region Fields

        /// <summary>
        /// The last known resolution by the object pool.
        /// </summary>
        private SizeF lastKnownResolution = GameScreen.AbsoluteResolution;
        /// <summary>
        /// The last know Safezone size.
        /// </summary>
        private float lastKnownSafezone = SafeZone.Size;
        /// <summary>
        /// The list of processable objects.
        /// </summary>
        private readonly List<IProcessable> objects = new List<IProcessable>();

        #endregion

        #region Properties

        /// <summary>
        /// Checks if there are objects visible on the screen.
        /// </summary>
        public bool AreAnyVisible
        {
            get
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    if (objects[i].Visible)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the game resolution is changed.
        /// </summary>
        public event ResolutionChangedEventHandler ResolutionChanged;
        /// <summary>
        /// Event triggered when the Safezone size option in the Display settings is changed.
        /// </summary>
        public event SafeZoneChangedEventHandler SafezoneChanged;

        #endregion

        #region Tools

        /// <summary>
        /// Detects resolution changes by comparing the last known resolution and the current one.
        /// </summary>
        private void DetectResolutionChanges()
        {
            // Get the current resolution
            SizeF resolution = GameScreen.AbsoluteResolution;
            // If the old res does not matches the current one
            if (lastKnownResolution != resolution)
            {
                // Trigger the event
                ResolutionChanged?.Invoke(this, new ResolutionChangedEventArgs(lastKnownResolution, resolution));
                // Refresh everything
                RefreshAll();
                // And save the new resolution
                lastKnownResolution = resolution;
            }
        }
        /// <summary>
        /// Detects Safezone changes by comparing the last known value to the current one.
        /// </summary>
        private void DetectSafezoneChanges()
        {
            // Get the current Safezone size
            float safezone = SafeZone.Size;
            // If is not the same as the last one
            if (lastKnownSafezone != safezone)
            {
                // Trigger the event
                SafezoneChanged?.Invoke(this, new SafeZoneChangedEventArgs(lastKnownSafezone, safezone));
                // Refresh everything
                RefreshAll();
                // And save the new safezone
                lastKnownSafezone = safezone;
            }
        }

        #endregion

        #region Functions

        /// <inheritdoc/>
        public IEnumerator<IProcessable> GetEnumerator() => objects.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Adds the object into the pool.
        /// </summary>
        /// <param name="obj">The object to add.</param>
        public void Add(IProcessable obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (objects.Contains(obj))
            {
                throw new InvalidOperationException("The object is already part of this pool.");
            }

            objects.Add(obj);
        }
        /// <summary>
        /// Removes the object from the pool.
        /// </summary>
        /// <param name="obj">The object to remove.</param>
        public void Remove(IProcessable obj) => objects.Remove(obj);
        /// <summary>
        /// Performs the specified action on each element that matches T.
        /// </summary>
        /// <typeparam name="T">The type to match.</typeparam>
        /// <param name="action">The action delegate to perform on each T.</param>
        public void ForEach<T>(Action<T> action)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is T conv)
                {
                    action(conv);
                }
            }
        }
        /// <summary>
        /// Refreshes all of the items.
        /// </summary>
        public void RefreshAll()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is IRecalculable recal)
                {
                    recal.Recalculate();
                }
            }
        }
        /// <summary>
        /// Hides all of the objects.
        /// </summary>
        public void HideAll()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].Visible = false;
            }
        }
        /// <summary>
        /// Processes the objects and features in this pool.
        /// This needs to be called every tick.
        /// </summary>
        public void Process()
        {
            DetectResolutionChanges();
            DetectSafezoneChanges();

            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].Process();
            }
        }

        #endregion
    }
}

// ResolutionChangedEventHandler.cs
namespace LemonUI
{
    /// <summary>
    /// Represents the method that reports a Resolution change in the Game Settings.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="ResolutionChangedEventArgs"/> containing the Previous and Current resolution.</param>
    public delegate void ResolutionChangedEventHandler(object sender, ResolutionChangedEventArgs e);
}

// IDrawable.cs
namespace LemonUI
{
    /// <summary>
    /// Represents an item that can be drawn.
    /// </summary>
    public interface IDrawable
    {
        #region Functions

        /// <summary>
        /// Draws the item on the screen.
        /// </summary>
        void Draw();

        #endregion
    }
}

// GFXAlignment.cs
namespace LemonUI
{
    /// <summary>
    /// Represents the internal alignment of screen elements.
    /// </summary>
    public enum GFXAlignment
    {
        /// <summary>
        /// Vertical Alignment to the Bottom.
        /// </summary>
        Bottom = 66,
        /// <summary>
        /// Vertical Alignment to the Top.
        /// </summary>
        Top = 84,
        /// <summary>
        /// Centered Vertically or Horizontally.
        /// </summary>
        Center = 67,
        /// <summary>
        /// Horizontal Alignment to the Left.
        /// </summary>
        Left = 76,
        /// <summary>
        /// Horizontal Alignment to the Right.
        /// </summary>
        Right = 82,
    }
}

// Scaleform/BaseScaleform.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// Represents a generic Scaleform object.
    /// </summary>
    public abstract class BaseScaleform : IScaleform
    {
        #region Fields

#if FIVEM || SHVDN3
        /// <summary>
        /// The ID of the scaleform.
        /// </summary>
        [Obsolete("Please use the Handle or Name properties and call the methods manually.", true)]
#endif
#if FIVEM
        protected CitizenFX.Core.Scaleform scaleform = new CitizenFX.Core.Scaleform(string.Empty);
#elif SHVDN3
        protected GTA.Scaleform scaleform = new GTA.Scaleform(string.Empty);
#endif

        #endregion

        #region Properties

        /// <summary>
        /// The ID or Handle of the Scaleform.
        /// </summary>
        public int Handle { get; private set; }
        /// <summary>
        /// The Name of the Scaleform.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// If the Scaleform should be visible or not.
        /// </summary>
        public virtual bool Visible { get; set; }
        /// <summary>
        /// If the Scaleform is loaded or not.
        /// </summary>
        public bool IsLoaded
        {
            get
            {
#if FIVEM
                return API.HasScaleformMovieLoaded(Handle);
#elif ALTV
                return Alt.Natives.HasScaleformMovieLoaded(Handle);
#elif RAGEMP
                return Invoker.Invoke<bool>(Natives.HasScaleformMovieLoaded, Handle);
#elif RPH
                return NativeFunction.CallByHash<bool>(0x85F01B8D5B90570E, Handle);
#elif SHVDN3 || SHVDNC
                return Function.Call<bool>(Hash.HAS_SCALEFORM_MOVIE_LOADED, Handle);
#endif
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Scaleform class with the specified Scaleform object name.
        /// </summary>
        /// <param name="sc">The Scalform object.</param>
        public BaseScaleform(string sc)
        {
            Name = sc ?? throw new ArgumentNullException(nameof(sc));

#if FIVEM
            Handle = API.RequestScaleformMovie(Name);
#elif ALTV
            Handle = Alt.Natives.RequestScaleformMovie(Name);
#elif RAGEMP
            Handle = Invoker.Invoke<int>(Natives.RequestScaleformMovie, Name);
#elif RPH
            Handle = NativeFunction.CallByHash<int>(0x11FE353CF9733E6F, Name);
#elif SHVDN3 || SHVDNC
            Handle = Function.Call<int>(Hash.REQUEST_SCALEFORM_MOVIE, Name);
#endif
        }

        #endregion

        #region Tools

        private static void AddInteger(int number)
        {
#if FIVEM
            API.ScaleformMovieMethodAddParamInt(number);
#elif ALT
            Alt.Natives.ScaleformMovieMethodAddParamInt(number);
#elif RAGEMP
            Invoker.Invoke(0xC3D0841A0CC546A6, number);
#elif RPH
            NativeFunction.CallByHash<int>(0xC3D0841A0CC546A6, number);
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0xC3D0841A0CC546A6, number);
#endif
        }
        private static void AddFloat(float number)
        {
#if FIVEM
            API.ScaleformMovieMethodAddParamFloat(number);
#elif ALTV
            Alt.Natives.ScaleformMovieMethodAddParamFloat(number);
#elif RAGEMP
            Invoker.Invoke(0xD69736AAE04DB51A, number);
#elif RPH
            NativeFunction.CallByHash<int>(0xD69736AAE04DB51A, number);
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0xD69736AAE04DB51A, number);
#endif
        }
        private void CallFunctionBase(string function, params object[] parameters)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function), "The function name is null.");
            }

            if (string.IsNullOrWhiteSpace(function))
            {
                throw new ArgumentOutOfRangeException(nameof(function), "The function name is empty or white space.");
            }

#if FIVEM
            API.BeginScaleformMovieMethod(Handle, function);
#elif ALTV
            Alt.Natives.BeginScaleformMovieMethod(Handle, function);
#elif RAGEMP
            Invoker.Invoke(0xF6E48914C7A8694E, Handle, function);
#elif RPH
            NativeFunction.CallByHash<int>(0xF6E48914C7A8694E, Handle, function);
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0xF6E48914C7A8694E, Handle, function);
#endif

            foreach (object obj in parameters)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException(nameof(parameters), "Unexpected null function argument in parameters.");
                }

                // Short and Byte can be converted to integer
                // Double can be converted to float (while loosing precision)
                // I'm not sure about long when it comes to the RAGE Engine

                if (obj is int objInt)
                {
                    AddInteger(objInt);
                }
                else if (obj is short objShort)
                {
                    AddInteger(objShort);
                }
                else if (obj is byte objByte)
                {
                    AddInteger(objByte);
                }
                else if (obj is float objFloat)
                {
                    AddFloat(objFloat);
                }
                else if (obj is double objDouble)
                {
                    AddFloat((float)objDouble);
                }
                else if (obj is string objString)
                {
#if FIVEM
                    API.BeginTextCommandScaleformString("STRING");
                    API.AddTextComponentSubstringPlayerName(objString);
                    API.EndTextCommandScaleformString();
#elif ALTV
                    Alt.Natives.BeginTextCommandScaleformString("STRING");
                    Alt.Natives.AddTextComponentSubstringPlayerName(objString);
                    Alt.Natives.EndTextCommandScaleformString();
#elif RAGEMP
                    Invoker.Invoke(Natives.BeginTextCommandScaleformString, "STRING");
                    Invoker.Invoke(Natives.AddTextComponentSubstringPlayerName, objString);
                    Invoker.Invoke(Natives.EndTextCommandScaleformString);
#elif RPH
                    NativeFunction.CallByHash<int>(0x80338406F3475E55, "STRING");
                    NativeFunction.CallByHash<int>(0x6C188BE134E074AA, objString);
                    NativeFunction.CallByHash<int>(0x362E2D3FE93A9959);
#elif SHVDN3 || SHVDNC
                    Function.Call((Hash)0x80338406F3475E55, "STRING");
                    Function.Call((Hash)0x6C188BE134E074AA, objString);
                    Function.Call((Hash)0x362E2D3FE93A9959);
#endif
                }
                else if (obj is bool objBool)
                {
#if FIVEM
                    API.ScaleformMovieMethodAddParamBool(objBool);
#elif ALTV
                    Alt.Natives.ScaleformMovieMethodAddParamBool(objBool);
#elif RAGEMP
                    Invoker.Invoke(0xC58424BA936EB458, objBool);
#elif RPH
                    NativeFunction.CallByHash<int>(0xC58424BA936EB458, objBool);
#elif SHVDN3 || SHVDNC
                    Function.Call((Hash)0xC58424BA936EB458, objBool);
#endif
                }
                else
                {
                    throw new ArgumentException($"Unexpected argument type {obj.GetType().Name}.", nameof(parameters));
                }
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Checks if the specified Scaleform Return Value is ready to be fetched.
        /// </summary>
        /// <param name="id">The Identifier of the Value.</param>
        /// <returns><see langword="true"/> if the value is ready, <see langword="false"/> otherwise.</returns>
        public bool IsValueReady(int id)
        {
#if FIVEM
            return API.IsScaleformMovieMethodReturnValueReady(id);
#elif ALTV
            return Alt.Natives.IsScaleformMovieMethodReturnValueReady(id);
#elif RAGEMP
            return Invoker.Invoke<bool>(Natives._0x768FF8961BA904D6, id);
#elif RPH
            return NativeFunction.CallByHash<bool>(0x768FF8961BA904D6, id);
#elif SHVDN3 || SHVDNC
            return Function.Call<bool>(Hash.IS_SCALEFORM_MOVIE_METHOD_RETURN_VALUE_READY, id);
#endif
        }
        /// <summary>
        /// Gets a specific value.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="id">The Identifier of the value.</param>
        /// <returns>The value returned by the native.</returns>
        public T GetValue<T>(int id)
        {
            if (typeof(T) == typeof(string))
            {
#if FIVEM
                return (T)(object)API.GetScaleformMovieMethodReturnValueString(id);
#elif ALTV
                return (T)(object)Alt.Natives.GetScaleformMovieMethodReturnValueString(id);
#elif RAGEMP
                return Invoker.Invoke<T>(0xE1E258829A885245, id);
#elif RPH
                return (T)NativeFunction.CallByHash(0xE1E258829A885245, typeof(string), id);
#elif SHVDN3 || SHVDNC
                return (T)(object)Function.Call<string>(Hash.GET_SCALEFORM_MOVIE_METHOD_RETURN_VALUE_STRING, id);
#endif
            }
            else if (typeof(T) == typeof(int))
            {
#if FIVEM
                return (T)(object)API.GetScaleformMovieMethodReturnValueInt(id);
#elif ALTV
                return (T)(object)Alt.Natives.GetScaleformMovieMethodReturnValueInt(id);
#elif RAGEMP
                return Invoker.Invoke<T>(0x2DE7EFA66B906036, id);
#elif RPH
                return (T)(object)NativeFunction.CallByHash<int>(0x2DE7EFA66B906036, id);
#elif SHVDN3 || SHVDNC
                return (T)(object)Function.Call<int>(Hash.GET_SCALEFORM_MOVIE_METHOD_RETURN_VALUE_INT, id);
#endif
            }
            else if (typeof(T) == typeof(bool))
            {
#if FIVEM
                return (T)(object)API.GetScaleformMovieMethodReturnValueBool(id);
#elif ALTV
                return (T)(object)Alt.Natives.GetScaleformMovieMethodReturnValueBool(id);
#elif RAGEMP
                return Invoker.Invoke<T>(0xD80A80346A45D761, id);
#elif RPH
                return (T)(object)NativeFunction.CallByHash<bool>(0xD80A80346A45D761, id);
#elif SHVDN3 || SHVDNC
                return (T)(object)Function.Call<bool>(Hash.GET_SCALEFORM_MOVIE_METHOD_RETURN_VALUE_BOOL, id);
#endif
            }
            else
            {
                throw new InvalidOperationException($"Expected string, int or bool, got {typeof(T).Name}.");
            }
        }
        /// <summary>
        /// Calls a Scaleform function.
        /// </summary>
        /// <param name="function">The name of the function to call.</param>
        /// <param name="parameters">The parameters to pass.</param>
        public void CallFunction(string function, params object[] parameters)
        {
            CallFunctionBase(function, parameters);
#if FIVEM
            API.EndScaleformMovieMethod();
#elif ALTV
            Alt.Natives.EndScaleformMovieMethod();
#elif RAGEMP
            Invoker.Invoke(0xC6796A8FFA375E53);
#elif RPH
            NativeFunction.CallByHash<int>(0xC6796A8FFA375E53);
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0xC6796A8FFA375E53);
#endif
        }
#if FIVEM || SHVDN3 || SHVDNC
        /// <summary>
        /// Calls a scaleform function and gets it's return value as soon as is available. 
        /// </summary>
        /// <param name="function">The function to call.</param>
        /// <param name="parameters">The parameters to call the function with.</param>
        /// <typeparam name="T">The type of return parameter.</typeparam>
        /// <returns>The value returned by the function.</returns>
        #if FIVEM
        public async Task<T> CallFunction<T>(string function, params object[] parameters)
        #else
        public T CallFunction<T>(string function, params object[] parameters)
        #endif
        {
            int id = CallFunctionReturn(function, parameters);

            while (!IsValueReady(id))
            {
                #if FIVEM
                await BaseScript.Delay(0);
                #elif SHVDN3 || SHVDNC
                Script.Yield();
                #endif
            }

            return GetValue<T>(id);
        }
#endif
        /// <summary>
        /// Calls a Scaleform function with a return value.
        /// </summary>
        /// <param name="function">The name of the function to call.</param>
        /// <param name="parameters">The parameters to pass.</param>
        public int CallFunctionReturn(string function, params object[] parameters)
        {
            CallFunctionBase(function, parameters);
#if FIVEM
            return API.EndScaleformMovieMethodReturnValue();
#elif ALTV
            return Alt.Natives.EndScaleformMovieMethodReturnValue();
#elif RAGEMP
            return Invoker.Invoke<int>(0xC50AA39A577AF886);
#elif RPH
            return NativeFunction.CallByHash<int>(0xC50AA39A577AF886);
#elif SHVDN3 || SHVDNC
            return Function.Call<int>((Hash)0xC50AA39A577AF886);
#endif
        }
        /// <summary>
        /// Updates the parameters of the Scaleform.
        /// </summary>
        public abstract void Update();
        /// <summary>
        /// Draws the scaleform full screen.
        /// </summary>
        public virtual void DrawFullScreen()
        {
            if (!Visible)
            {
                return;
            }
            Update();
#if FIVEM
            API.DrawScaleformMovieFullscreen(Handle, 255, 255, 255, 255, 0);
#elif ALTV
            Alt.Natives.DrawScaleformMovieFullscreen(Handle, 255, 255, 255, 255, 0);
#elif RAGEMP
            Invoker.Invoke(Natives.DrawScaleformMovieFullscreen, Handle, 255, 255, 255, 255, 0);
#elif RPH
            NativeFunction.CallByHash<int>(0x0DF606929C105BE1, Handle, 255, 255, 255, 255, 0);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.DRAW_SCALEFORM_MOVIE_FULLSCREEN, Handle, 255, 255, 255, 255, 0);
#endif
        }
        /// <summary>
        /// Draws the scaleform full screen.
        /// </summary>
        public virtual void Draw() => DrawFullScreen();
        /// <summary>
        /// Draws the scaleform full screen.
        /// </summary>
        public virtual void Process() => DrawFullScreen();
        /// <summary>
        /// Marks the scaleform as no longer needed.
        /// </summary>
        public void Dispose()
        {
            int id = Handle;
#if FIVEM
            API.SetScaleformMovieAsNoLongerNeeded(ref id);
#elif ALTV
            Alt.Natives.SetScaleformMovieAsNoLongerNeeded(ref id);
#elif RAGEMP
            IntReference idPtr = new IntReference(id);
            Invoker.Invoke(Natives.SetScaleformMovieAsNoLongerNeeded, idPtr);
#elif RPH
            using (NativePointer idPtr = new NativePointer(4))
            {
                idPtr.SetValue(id);
                NativeFunction.CallByHash<int>(0x6DD8F5AA635EB4B2, idPtr);
            }
#elif SHVDN3 || SHVDNC
            unsafe
            {
                Function.Call(Hash.SET_SCALEFORM_MOVIE_AS_NO_LONGER_NEEDED, &id);
            }
#endif
        }

        #endregion
    }
}

// Scaleform/CelebrationForeground.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The foreground of the celebration scaleform.
    /// </summary>
    public class CelebrationForeground : CelebrationCore
    {
        #region Constructors

        /// <summary>
        /// Initializes a new Celebration foreground.
        /// </summary>
        public CelebrationForeground() : base("MP_CELEBRATION_FG")
        {
        }

        #endregion
    }
}

// Scaleform/BruteForceBackground.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The Background of the BruteForce Hack Minigame.
    /// </summary>
    public enum BruteForceBackground
    {
        /// <summary>
        /// A simple Black background.
        /// </summary>
        Black = 0,
        /// <summary>
        /// A simple Purple background.
        /// </summary>
        Purple = 1,
        /// <summary>
        /// A simple Gray background.
        /// </summary>
        Gray = 2,
        /// <summary>
        /// A simple Light Blue background.
        /// </summary>
        LightBlue = 3,
        /// <summary>
        /// A Light Blue Wallpaper.
        /// </summary>
        Wallpaper1 = 4,
        /// <summary>
        /// A Fade from Gray in the center to Black in the corners.
        /// </summary>
        DarkFade = 5,
    }
}

// Scaleform/InstructionalButton.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// An individual instructional button.
    /// </summary>
    public struct InstructionalButton
    {
        #region Fields

        private Control control;
        private string raw;
        private string description;

        #endregion

        #region Properties

        /// <summary>
        /// The description of this button.
        /// </summary>
        public string Description
        {
            get => description;
            set => description = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The Control used by this button.
        /// </summary>
        public Control Control
        {
            get => control;
            set
            {
                control = value;
#if FIVEM
                raw = API.GetControlInstructionalButton(2, (int)value, 1);
#elif RAGEMP
                raw = Invoker.Invoke<string>(Natives.GetControlInstructionalButton, 2, (int)value, 1);
#elif RPH
                raw = (string)NativeFunction.CallByHash(0x0499D7B09FC9B407, typeof(string), 2, (int)control, 1);
#elif SHVDN3 || SHVDNC
                raw = Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, (int)value, 1);
#elif ALTV
                raw = Alt.Natives.GetControlInstructionalButtonsString(2, (int)value, true);
#endif
            }
        }
        /// <summary>
        /// The Raw Control sent to the Scaleform.
        /// </summary>
        public string Raw
        {
            get => raw;
            set
            {
                raw = value ?? throw new ArgumentNullException(nameof(value));
                control = (Control)(-1);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instructional button for a Control.
        /// </summary>
        /// <param name="description">The text for the description.</param>
        /// <param name="control">The control to use.</param>
        public InstructionalButton(string description, Control control)
        {
            this.description = description ?? throw new ArgumentNullException(nameof(description));
            this.control = control;
#if FIVEM
            raw = API.GetControlInstructionalButton(2, (int)control, 1);
#elif ALTV
            raw = Alt.Natives.GetControlInstructionalButtonsString(2, (int)control, true);
#elif RAGEMP
            raw = Invoker.Invoke<string>(Natives.GetControlInstructionalButton, 2, (int)control, 1);
#elif RPH
            raw = (string)NativeFunction.CallByHash(0x0499D7B09FC9B407, typeof(string), 2, (int)control, 1);
#elif SHVDN3 || SHVDNC
            raw = Function.Call<string>(Hash.GET_CONTROL_INSTRUCTIONAL_BUTTONS_STRING, 2, (int)control, 1);
#endif
        }
        /// <summary>
        /// Creates an instructional button for a raw control.
        /// </summary>
        /// <param name="description">The text for the description.</param>
        /// <param name="raw">The raw value of the control.</param>
        public InstructionalButton(string description, string raw)
        {
            this.description = description ?? throw new ArgumentNullException(nameof(description));
            control = (Control)(-1);
            this.raw = raw ?? throw new ArgumentNullException(nameof(raw));
        }

        #endregion
    }
}

// Scaleform/BruteForceFinishedEventArgs.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// Event information after an the BruteForce hack has finished.
    /// </summary>
    public class BruteForceFinishedEventArgs
    {
        #region Properties

        /// <summary>
        /// The final status of the Hack.
        /// </summary>
        public BruteForceStatus Status { get; }

        #endregion

        #region Constructors

        internal BruteForceFinishedEventArgs(BruteForceStatus status)
        {
            Status = status;
        }

        #endregion
    }
}

// Scaleform/BruteForceFinishedEventHandler.cs
namespace LemonUI.Scaleform
{

    /// <summary>
    /// Represents the method that is called when the end user finishes the BruteForce hack.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="BruteForceFinishedEventArgs"/> with the hack status.</param>
    public delegate void BruteForceFinishedEventHandler(object sender, BruteForceFinishedEventArgs e);
}

// Scaleform/InstructionalButtons.cs
namespace LemonUI.Scaleform
{

    /// <summary>
    /// Buttons shown on the bottom right of the screen.
    /// </summary>
    public class InstructionalButtons : BaseScaleform
    {
        #region Fields

        /// <summary>
        /// The buttons used in this Scaleform.
        /// </summary>
        private readonly List<InstructionalButton> buttons = new List<InstructionalButton>();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new set of Instructional Buttons.
        /// </summary>
        /// <param name="buttons">The buttons to add into this menu.</param>
        public InstructionalButtons(params InstructionalButton[] buttons) : base("INSTRUCTIONAL_BUTTONS")
        {
            this.buttons.AddRange(buttons);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds an Instructional Button.
        /// </summary>
        /// <param name="button">The button to add.</param>
        public void Add(InstructionalButton button)
        {
            // If the item is already in the list, raise an exception
            if (buttons.Contains(button))
            {
                throw new InvalidOperationException("The button is already in the Scaleform.");
            }

            // Otherwise, add it to the list of items
            buttons.Add(button);
        }
        /// <summary>
        /// Removes an Instructional Button.
        /// </summary>
        /// <param name="button">The button to remove.</param>
        public void Remove(InstructionalButton button)
        {
            // If the button is not in the list, return
            if (!buttons.Contains(button))
            {
                return;
            }

            // Otherwise, remove it
            buttons.Remove(button);
        }
        /// <summary>
        /// Removes all of the instructional buttons.
        /// </summary>
        public void Clear()
        {
            buttons.Clear();
        }
        /// <summary>
        /// Refreshes the items shown in the Instructional buttons.
        /// </summary>
        public override void Update()
        {
            // Clear all of the existing items
            CallFunction("CLEAR_ALL");
            CallFunction("TOGGLE_MOUSE_BUTTONS", 0);
            CallFunction("CREATE_CONTAINER");

            // And add them again
            for (int i = 0; i < buttons.Count; i++)
            {
                InstructionalButton button = buttons[i];
                CallFunction("SET_DATA_SLOT", i, button.Raw, button.Description);
            }

            CallFunction("DRAW_INSTRUCTIONAL_BUTTONS", -1);
        }

        #endregion
    }
}

// Scaleform/Countdown.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The Countdown scaleform in the GTA Online races.
    /// </summary>
    public class Countdown : BaseScaleform
    {
        #region Fields

        private int duration = 3;
        private long lastStepTime = 0;

        #endregion

        #region Defaults

        // TODO: Add the Stunt Race sounds, Countdown_1 and Countdown_GO in DLC_AW_Frontend_Sounds
        // TODO: See why Countdown_GO doesn't plays correctly

        /// <summary>
        /// The default sound played when the countdown
        /// </summary>
        public static Sound DefaultCountSound = new Sound("HUD_MINI_GAME_SOUNDSET", "CHECKPOINT_NORMAL");
        /// <summary>
        /// The default sound when th
        /// </summary>
        public static Sound DefaultGoSound = new Sound("HUD_MINI_GAME_SOUNDSET", "GO");

        #endregion

        #region Properties

        /// <summary>
        /// The sound played when counting down.
        /// </summary>
        public Sound CountSound { get; set; } = DefaultCountSound;
        /// <summary>
        /// The sound played when the countdown has finished.
        /// </summary>
        public Sound GoSound { get; set; } = DefaultGoSound;
        /// <summary>
        /// The duration of the countdown.
        /// </summary>
        public int Duration
        {
            get => duration;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The duration can't be equal or under zero.");
                }

                duration = value;
            }
        }
        /// <summary>
        /// The current count.
        /// </summary>
        public int Current { get; private set; }
        /// <summary>
        /// The color used for the numbers.
        /// </summary>
        public Color ColorNumbers { get; set; } = Color.FromArgb(255, 255, 255);
        /// <summary>
        /// The color used for the GO at the end.
        /// </summary>
        public Color ColorGo { get; set; } = Color.FromArgb(255, 255, 255);

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the countdown starts.
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Event triggered when the countdown has finished.
        /// </summary>
        public event EventHandler Finished;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new countdown scaleform.
        /// </summary>
        public Countdown() : base("COUNTDOWN")
        {
        }

        #endregion

        #region Tools

        private void ShowStep(int step)
        {
            string asString = step == 0 ? "GO" : step.ToString();
            Color color = step == 0 ? ColorGo : ColorNumbers;

            CallFunction("SET_MESSAGE", asString, (int)color.R, (int)color.G, (int)color.B, true);
            CallFunction("FADE_MP", asString, (int)color.R, (int)color.G, (int)color.B);

            if (step == 0)
            {
                GoSound?.PlayFrontend();
            }
            else
            {
                CountSound?.PlayFrontend();
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Starts the countdown.
        /// </summary>
        public void Start()
        {
            Visible = true;

#if ALTV
            lastStepTime = Alt.Natives.GetGameTimer();
#elif RAGEMP
            lastStepTime = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            lastStepTime = Game.GameTime;
#endif

            Current = duration;
            ShowStep(Current);
            Started?.Invoke(this, EventArgs.Empty);
        }
        /// <inheritdoc/>
        public override void Process()
        {
            if (!Visible)
            {
                return;
            }

            if (lastStepTime > 0)
            {
#if ALTV
                long currentTime = Alt.Natives.GetGameTimer();
#elif RAGEMP
                long currentTime = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
                long currentTime = Game.GameTime;
#endif

                if (currentTime - lastStepTime >= 1000)
                {
                    if (Current == 0)
                    {
                        lastStepTime = 0;
                        Visible = false;
                        return;
                    }

                    lastStepTime = currentTime;
                    Current--;
                    ShowStep(Current);

                    if (Current == 0)
                    {
                        Finished?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            DrawFullScreen();
        }
        /// <inheritdoc/>
        public override void Update()
        {
        }

        #endregion
    }
}

// Scaleform/CelebrationStyle.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The style of the Celebration scaleform.
    /// </summary>
    public enum CelebrationStyle
    {
        /// <summary>
        /// General purpose clean style. used in missions.
        /// </summary>
        Clean = 0,
        /// <summary>
        /// The style used in the heist prep missions.
        /// </summary>
        Prep = 1,
        /// <summary>
        /// The style used in the heist finals and end of prep/final.
        /// </summary>
        Heist = 2,
        /// <summary>
        /// The style used for the Stunt Races.
        /// </summary>
        Race = 3
    }
}

// Scaleform/BruteForceStatus.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The status of the BruteForce Hack after finishing.
    /// </summary>
    public enum BruteForceStatus
    {
        /// <summary>
        /// The user completed the hack successfully.
        /// </summary>
        Completed = 0,
        /// <summary>
        /// The user ran out of time.
        /// </summary>
        OutOfTime = 1,
        /// <summary>
        /// The player ran out of lives.
        /// </summary>
        OutOfLives = 2,
    }
}

// Scaleform/BruteForce.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The BruteForce Hacking Minigame shown in multiple missions.
    /// </summary>
    public class BruteForce : BaseScaleform
    {
        #region Fields

        private static readonly Random random = new Random();
        private static readonly Sound soundRowSwitch = new Sound(string.Empty, "HACKING_MOVE_CURSOR");
        private static readonly Sound soundRowCompleted = new Sound(string.Empty, "HACKING_CLICK");
        private static readonly Sound soundRowFailed = new Sound(string.Empty, "HACKING_CLICK_BAD");
        private static readonly Sound soundSuccess = new Sound(string.Empty, "HACKING_SUCCESS");

        private int hideTime = -1;
        private int output = 0;
        private bool inProgress = false;

        private BruteForceBackground background = BruteForceBackground.Black;
        private string word = "LEMONADE";
        private int livesTotal = 5;
        private int livesCurrent = 5;
        private int closeAfter = -1;
        private TimeSpan end = TimeSpan.Zero;
        private TimeSpan countdown = TimeSpan.Zero;
        private bool showLives = true;
        private bool visible;

        #endregion

        #region Properties

        /// <inheritdoc />
        public override bool Visible
        {
            get => visible;
            set
            {
                visible = value;

                if (visible)
                {
                    Reset();
                }
            }
        }
        /// <summary>
        /// The Word shown to select in the menu.
        /// </summary>
        public string Word
        {
            get => word;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 8)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The word needs to be exactly 8 characters long.");
                }
                word = value;
                CallFunction("SET_ROULETTE_WORD", value);
            }
        }
        /// <summary>
        /// The background of the Hacking minigame.
        /// </summary>
        public BruteForceBackground Background
        {
            get => background;
            set
            {
                background = value;
                CallFunction("SET_BACKGROUND", (int)value);
            }
        }
        /// <summary>
        /// The number of Lives of the minigame.
        /// </summary>
        public int TotalLives
        {
            get => livesTotal;
            set
            {
                livesTotal = value;
                if (livesCurrent > value)
                {
                    livesCurrent = value;
                }
                CallFunction("SET_LIVES", livesCurrent, value);
            }
        }
        /// <summary>
        /// The current number of lives that the player has.
        /// </summary>
        public int CurrentLives => livesCurrent;
        /// <summary>
        /// The messages that might appear on success.
        /// </summary>
        public List<string> SuccessMessages { get; } = new List<string>();
        /// <summary>
        /// The messages that will appear when the player fails.
        /// </summary>
        public List<string> FailMessages { get; } = new List<string>();
        /// <summary>
        /// The time in milliseconds to wait before closing the Hack window automatically.
        /// </summary>
        /// <remarks>
        /// This can be set to -1 to keep the Hack window open.
        /// </remarks>
        public int CloseAfter
        {
            get => closeAfter;
            set
            {
                if (value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The Closure time can't be under -1.");
                }
                closeAfter = value;
            }
        }
        /// <summary>
        /// If the player can retry the hack after failing.
        /// </summary>
        public bool CanRetry { get; set; } = false;
        /// <summary>
        /// The countdown of the Hack minigame.
        /// </summary>
        public TimeSpan Countdown
        {
            get => countdown;
            set => countdown = value;
        }
        /// <summary>
        /// If the lives of the player should be shown on the top right.
        /// </summary>
        public bool ShowLives
        {
            get => showLives;
            set
            {
                showLives = value;
                CallFunction("SHOW_LIVES", value);
            }
        }
        /// <summary>
        /// If all of the rows should be restarted after the player fails one.
        /// </summary>
        public bool ResetOnRowFail { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the player finishes a hack.
        /// </summary>
        public event BruteForceFinishedEventHandler HackFinished;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Hacking Scaleform.
        /// </summary>
        public BruteForce() : base("HACKING_PC")
        {
            visible = false;
            for (int i = 0; i < 8; i++)
            {
                SetColumnSpeed(i, 100);
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Resets the entire Hacking minigame.
        /// </summary>
        public void Reset()
        {
            inProgress = true;

            Background = background;
            RunProgram(4);
            RunProgram(83);
            TotalLives = livesTotal;
            livesCurrent = livesTotal;
            CallFunction("SET_LIVES", livesCurrent, livesTotal);
            Word = word;
            ShowLives = showLives;

#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif

            end = TimeSpan.FromMilliseconds(time) + countdown;
        }
        /// <summary>
        /// Sets the speed of one of the 8 columns.
        /// </summary>
        /// <param name="index">The index of the column.</param>
        /// <param name="speed">The speed of the column.</param>
        public void SetColumnSpeed(int index, float speed)
        {
            if (index >= 8 || index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "The index needs to be between 0 and 7.");
            }
            CallFunction("SET_COLUMN_SPEED", index, speed);
        }
        /// <summary>
        /// Runs the specified Hacking program.
        /// </summary>
        /// <param name="program">The program to open.</param>
        public void RunProgram(int program)
        {
            CallFunction("RUN_PROGRAM", program);
        }
        /// <summary>
        /// Updates the information of the Hacking window.
        /// </summary>
        public override void Update()
        {
#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif

            // If there is a time set to hide the Hack window
            if (hideTime != -1)
            {
                // If that time has already passed, go ahead and hide the window
                if (hideTime <= time)
                {
                    Visible = false;
                    hideTime = -1;
                    return;
                }
            }

            // If the hack minigame is not in progress but the player can retry and he pressed enter, reset it
            if (!inProgress && CanRetry && Controls.IsJustPressed(Control.FrontendAccept))
            {
                Reset();
                hideTime = -1;
                return;
            }

            // If the Hack minigame is in progress
            if (inProgress)
            {
                // If there is a countdown set
                if (countdown > TimeSpan.Zero)
                {
                    // Calculate the time left
                    TimeSpan span = countdown - (TimeSpan.FromMilliseconds(time) - end);

                    // If is lower or equal than zero, the player failed
                    if (span <= TimeSpan.Zero)
                    {
                        CallFunction("SET_COUNTDOWN", 0, 0, 0);
                        string err = FailMessages.Count == 0 ? string.Empty : FailMessages[random.Next(FailMessages.Count)];
                        CallFunction("SET_ROULETTE_OUTCOME", false, err);
                        hideTime = closeAfter == -1 ? -1 : (int)time + CloseAfter;
                        inProgress = false;
                        HackFinished?.Invoke(this, new BruteForceFinishedEventArgs(BruteForceStatus.OutOfTime));
                        return;
                    }
                    // Otherwise, update the visible time
                    else
                    {
                        CallFunction("SET_COUNTDOWN", span.Minutes, span.Seconds, span.Milliseconds);
                    }
                }

                // If the user pressed left, go to the left
                if (Controls.IsJustPressed(Control.MoveLeftOnly) || Controls.IsJustPressed(Control.FrontendLeft))
                {
                    soundRowSwitch.PlayFrontend();
                    CallFunction("SET_INPUT_EVENT", 10);
                }
                // If the user pressed right, go to the right
                else if (Controls.IsJustPressed(Control.MoveRightOnly) || Controls.IsJustPressed(Control.FrontendRight))
                {
                    soundRowSwitch.PlayFrontend();
                    CallFunction("SET_INPUT_EVENT", 11);
                }
                // If the user pressed accept, send the selection event
                else if (Controls.IsJustPressed(Control.FrontendAccept))
                {
                    output = CallFunctionReturn("SET_INPUT_EVENT_SELECT");
                }

                // If there is some output to receive
                if (output != 0)
                {
                    // If the value is ready, go ahead and check it
                    if (IsValueReady(output))
                    {
                        switch (GetValue<int>(output))
                        {
                            case 86: // Hack Completed
                                string ok = SuccessMessages.Count == 0 ? string.Empty : SuccessMessages[random.Next(SuccessMessages.Count)];
                                CallFunction("SET_ROULETTE_OUTCOME", true, ok);
                                soundSuccess.PlayFrontend();
                                HackFinished?.Invoke(this, new BruteForceFinishedEventArgs(BruteForceStatus.Completed));
                                hideTime = closeAfter == -1 ? -1 : (int)time + CloseAfter;
                                inProgress = false;
                                break;
                            case 87: // Row Failed (or lives failed)
                                livesCurrent--;
                                CallFunction("SET_LIVES", livesCurrent, livesTotal);
                                soundRowFailed.PlayFrontend();
                                if (livesCurrent <= 0)
                                {
                                    string err = FailMessages.Count == 0 ? string.Empty : FailMessages[random.Next(FailMessages.Count)];
                                    CallFunction("SET_ROULETTE_OUTCOME", false, err);
                                    hideTime = closeAfter == -1 ? -1 : (int)time + CloseAfter;
                                    inProgress = false;
                                    HackFinished?.Invoke(this, new BruteForceFinishedEventArgs(BruteForceStatus.OutOfLives));
                                }
                                break;
                            case 92: // Row Completed
                                soundRowCompleted.PlayFrontend();
                                break;
                        }
                        output = 0;
                    }
                }
            }
        }

        #endregion
    }
}

// Scaleform/IScaleform.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// Scaleforms are 2D Adobe Flash-like objects.
    /// </summary>
    public interface IScaleform : IDrawable, IProcessable, IDisposable
    {
        #region Properties

        /// <summary>
        /// Draws the Scaleform in full screen.
        /// </summary>
        void DrawFullScreen();

        #endregion
    }
}

// Scaleform/PopUp.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// A warning pop-up.
    /// </summary>
    public class PopUp : BaseScaleform
    {
        private string title;
        private string subtitle;
        private string prompt;
        private string error;

        #region Properties

        /// <summary>
        /// The title of the Pop-up.
        /// </summary>
        public string Title
        {
            get => title;
            set => title = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The subtitle of the Pop-up.
        /// </summary>
        public string Subtitle
        {
            get => subtitle;
            set => subtitle = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The prompt of the Pop-up.
        /// </summary>
        public string Prompt
        {
            get => prompt;
            set => prompt = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// If the black background should be shown.
        /// </summary>
        public bool ShowBackground { get; set; } = true;
        /// <summary>
        /// The error message to show.
        /// </summary>
        public string Error
        {
            get => error;
            set => error = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Pop-up instance.
        /// </summary>
        public PopUp() : base("POPUP_WARNING")
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Updates the texts of the Pop-up.
        /// </summary>
        public override void Update()
        {
            // first parameter "msecs" is unused
            CallFunction("SHOW_POPUP_WARNING", 0, Title, Subtitle, Prompt, ShowBackground, 0, Error);
        }

        #endregion
    }
}

// Scaleform/Celebration.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The foreground of the celebration scaleform.
    /// </summary>
    public class Celebration : CelebrationCore
    {
        #region Fields

        private long showUntil = 0;
        private bool cancel = false;
        private string mode = string.Empty;
        private string job = string.Empty;
        private string challenge = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The background of the scaleform.
        /// </summary>
        public CelebrationBackground Background { get; } = new CelebrationBackground();
        /// <summary>
        /// The foreground of the scaleform.
        /// </summary>
        public CelebrationForeground Foreground { get; } = new CelebrationForeground();

        /// <summary>
        /// The mode name shown.
        /// </summary>
        /// <remarks>
        /// This is used to show the current mode, like "HEIST", "RACE", "LAST TEAM STANDING", etc.
        /// </remarks>
        public string Mode
        {
            get => mode;
            set => mode = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The job name shown.
        /// </summary>
        /// <remarks>
        /// This is used to show the current job name like "The Prison Break" or "Premium Race - East Coast".
        /// </remarks>
        public string Job
        {
            get => job;
            set => job = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The challenge shown.
        /// </summary>
        /// <remarks>
        /// This is used to show messages like "ROUND 10" or "POTENTIAL CUT $1000000".
        /// </remarks>
        public string Challenge
        {
            get => challenge;
            set => challenge = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// For how long the scalefom is show.
        /// </summary>
        public int Duration { get; set; } = 2;
        /// <summary>
        /// The style of the celebration.
        /// </summary>
        public CelebrationStyle Style { get; set; } = CelebrationStyle.Clean;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the scaleform is shown on the screen.
        /// </summary>
        public event EventHandler Shown;
        /// <summary>
        /// Event triggered when the scaleform has finished fading out.
        /// </summary>
        public event EventHandler Finished;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new Celebration scaleform.
        /// </summary>
        public Celebration() : base("MP_CELEBRATION")
        {
        }

        #endregion

        #region Tools

        private void CallFunctionOnAll(string function, params object[] parameters)
        {
            Background.CallFunction(function, parameters);
            Foreground.CallFunction(function, parameters);
            CallFunction(function, parameters);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Shows the celebration scaleform.
        /// </summary>
        public void Show()
        {
            cancel = false;
            Visible = true;

            const string wallId = "intro";

            CallFunctionOnAll("CLEANUP", wallId);
            CallFunctionOnAll("CREATE_STAT_WALL", wallId, "HUD_COLOUR_BLACK", 40);

            CallFunctionOnAll("SET_PAUSE_DURATION", Duration);
            // challengeTextLabel appears to be used as a bool and challengePartsText is appended
            CallFunctionOnAll("ADD_INTRO_TO_WALL", wallId, Mode, Job, true, Challenge, string.Empty, Challenge, Duration, string.Empty, true, "HUD_COLOUR_WHITE");
            CallFunctionOnAll("ADD_BACKGROUND_TO_WALL", wallId, 75, (int)Style);
            CallFunctionOnAll("SHOW_STAT_WALL", wallId);

            Shown?.Invoke(this, EventArgs.Empty);

            // Explanation for future me
            // 333ms going in
            // 333ms going out
#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif
            showUntil = time + 333 + 333 + (Duration * 1000);
        }
        /// <summary>
        /// Cancels the current screen being shown.
        /// </summary>
        public void Cancel()
        {
            if (Visible)
            {
                cancel = true;
            }
        }
        /// <inheritdoc/>
        public override void Process()
        {
            if (!Visible)
            {
                return;
            }

            if (cancel)
            {
                showUntil = -1;
                Visible = false;
                return;
            }

            DrawFullScreen();

#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif

            if (showUntil < time)
            {
                showUntil = 0;
                Visible = false;
                Finished?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Draws the celebration scaleform.
        /// </summary>
        public override void DrawFullScreen()
        {
            if (!Visible)
            {
                return;
            }

            #if ALTV
            Alt.Natives.DrawScaleformMovieFullscreenMasked(Background.Handle, Foreground.Handle, 255, 255, 255, 255);
            Alt.Natives.DrawScaleformMovieFullscreen(Handle, 255, 255, 255, 255, 255);
            #elif FIVEM
            API.DrawScaleformMovieFullscreenMasked(Background.Handle, Foreground.Handle, 255, 255, 255, 255);
            API.DrawScaleformMovieFullscreen(Handle, 255, 255, 255, 255, 255);
            #elif RAGEMP
            Invoker.Invoke(Natives.DrawScaleformMovieFullscreenMasked, Background.Handle, Foreground.Handle, 255, 255, 255, 255);
            Invoker.Invoke(Natives.DrawScaleformMovieFullscreen, Handle, 255, 255, 255, 255);
            #elif RPH
            NativeFunction.CallByHash<int>(0xCF537FDE4FBD4CE5, Background.Handle, Foreground.Handle, 255, 255, 255, 255);
            NativeFunction.CallByHash<int>(0x0DF606929C105BE1, Handle, 255, 255, 255, 255);
            #elif SHVDN3
            Function.Call(Hash.DRAW_SCALEFORM_MOVIE_FULLSCREEN_MASKED, Background.Handle, Foreground.Handle, 255, 255, 255, 255);
            Function.Call(Hash.DRAW_SCALEFORM_MOVIE_FULLSCREEN, Handle, 255, 255, 255, 255);
            #endif
        }

        #endregion
    }
}

// Scaleform/BigMessage.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// A customizable big message.
    /// </summary>
    public class BigMessage : BaseScaleform
    {
        #region Constants

        private const uint unarmed = 0xA2719263;

        #endregion

        #region Fields

        private MessageType type;
        private uint weaponHash;
        private long hideAfter;
        private string title;
        private string message;
        private string rank;
        private int textColor;
        private int backgroundColor;

        #endregion

        #region Properties

        /// <summary>
        /// The title of the message.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value ?? throw new ArgumentNullException(nameof(value));
                UpdateInternal();
            }
        }
        /// <summary>
        /// The subtitle or description of the message.
        /// </summary>
        public string Message
        {
            get => message;
            set
            {
                message = value ?? throw new ArgumentNullException(nameof(value));
                UpdateInternal();
            }
        }
        /// <summary>
        /// The color of the text.
        /// Only used on the Customizable message type.
        /// </summary>
        public int TextColor
        {
            get => textColor;
            set
            {
                textColor = value;
                UpdateInternal();
            }
        }
        /// <summary>
        /// The color of the background in the Customizable message type.
        /// </summary>
        public int BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;
                UpdateInternal();
            }
        }
        /// <summary>
        /// The Rank when the mode is set to Cops and Crooks.
        /// </summary>
        public string Rank
        {
            get => rank;
            set
            {
                rank = value ?? throw new ArgumentNullException(nameof(value));
                UpdateInternal();
            }
        }
#if !RAGEMP && !ALTV
        /// <summary>
        /// The hash of the Weapon as an enum.
        /// </summary>
        public WeaponHash Weapon
        {
            get => (WeaponHash)weaponHash;
            set => weaponHash = (uint)value;
        }
#endif
        /// <summary>
        /// The hash of the Weapon as it's native value.
        /// </summary>
        public uint WeaponHash
        {
            get => weaponHash;
            set => weaponHash = value;
        }
        /// <summary>
        /// The type of message to show.
        /// </summary>
        public MessageType Type
        {
            get => type;
            set
            {
                if (!Enum.IsDefined(typeof(MessageType), value))
                {
                    throw new InvalidOperationException($"{value} is not a valid message type.");
                }
                type = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a standard customizable message with just a title.
        /// </summary>
        /// <param name="title">The title to use.</param>
        public BigMessage(string title) : this(title, string.Empty, string.Empty, unarmed, 0, 0, MessageType.Customizable)
        {
        }
        /// <summary>
        /// Creates a custom message with the specified title.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="type">The type of message.</param>
        public BigMessage(string title, MessageType type) : this(title, string.Empty, string.Empty, unarmed, 0, 0, type)
        {
        }
        /// <summary>
        /// Creates a standard customizable message with a title and message.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="message">The message to show.</param>
        public BigMessage(string title, string message) : this(title, message, string.Empty, unarmed, 0, 0, MessageType.Customizable)
        {
        }
        /// <summary>
        /// Creates a Cops and Crooks message type.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="rank">Text to show in the Rank space.</param>
        public BigMessage(string title, string message, string rank) : this(title, message, rank, unarmed, 0, 0, MessageType.CopsAndCrooks)
        {
        }
        /// <summary>
        /// Creates a message with the specified type, title and message.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="type">The type of message.</param>
        public BigMessage(string title, string message, MessageType type) : this(title, message, string.Empty, unarmed, 0, 0, type)
        {
        }
        /// <summary>
        /// Creates a standard customizable message with a title and a custom text color.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="colorText">The color of the text.</param>
        public BigMessage(string title, int colorText) : this(title, string.Empty, string.Empty, unarmed, colorText, 0, MessageType.Customizable)
        {
        }
        /// <summary>
        /// Creates a standard customizable message with a specific title and custom colors.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="colorText">The color of the text.</param>
        /// <param name="colorBackground">The color of the background.</param>
        public BigMessage(string title, int colorText, int colorBackground) : this(title, string.Empty, string.Empty, unarmed, colorText, colorBackground, MessageType.Customizable)
        {
        }
#if !RAGEMP && !ALTV
        /// <summary>
        /// Creates a Weapon Purchase message with a custom text and weapons.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="weapon">The name of the Weapon.</param>
        /// <param name="hash">The hash of the Weapon image.</param>
        public BigMessage(string title, string weapon, WeaponHash hash) : this(title, string.Empty, weapon, hash, 0, 0, MessageType.Weapon)
        {
        }
        /// <summary>
        /// Creates a Weapon Purchase message with a custom text and weapons.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="weapon">The name of the Weapon.</param>
        /// <param name="hash">The hash of the Weapon image.</param>
        public BigMessage(string title, string message, string weapon, WeaponHash hash) : this(title, message, weapon, hash, 0, 0, MessageType.Weapon)
        {
        }
        /// <summary>
        /// Creates a message with all of the selected information.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="rank">The Rank on Cops and Crooks.</param>
        /// <param name="weapon">The hash of the Weapon image.</param>
        /// <param name="colorText">The color of the text.</param>
        /// <param name="colorBackground">The color of the background.</param>
        /// <param name="type">The type of message.</param>
        public BigMessage(string title, string message, string rank, WeaponHash weapon, int colorText, int colorBackground, MessageType type) : this(title, message, rank, (uint)weapon, colorText, colorBackground, type)
        {
        }
#endif
        /// <summary>
        /// Creates a message with all of the selected information.
        /// </summary>
        /// <param name="title">The title to use.</param>
        /// <param name="message">The message to show.</param>
        /// <param name="rank">The Rank on Cops and Crooks.</param>
        /// <param name="weapon">The hash of the Weapon image.</param>
        /// <param name="colorText">The color of the text.</param>
        /// <param name="colorBackground">The color of the background.</param>
        /// <param name="type">The type of message.</param>
        public BigMessage(string title, string message, string rank, uint weapon, int colorText, int colorBackground, MessageType type) : base("MP_BIG_MESSAGE_FREEMODE")
        {
            this.title = title;
            this.message = message;
            this.rank = rank;
            this.weaponHash = weapon;
            this.textColor = colorText;
            this.backgroundColor = colorBackground;
            Type = type;
            UpdateInternal();
        }

        #endregion

        #region Tools

        private void UpdateInternal()
        {
            // Select the correct function to call
            string function;
            switch (type)
            {
                case MessageType.Customizable:
                    function = "SHOW_SHARD_CENTERED_MP_MESSAGE";
                    break;
                case MessageType.RankUp:
                    function = "SHOW_SHARD_CREW_RANKUP_MP_MESSAGE";
                    break;
                case MessageType.MissionPassedOldGen:
                    function = "SHOW_MISSION_PASSED_MESSAGE";
                    break;
                case MessageType.Wasted:
                    function = "SHOW_SHARD_WASTED_MP_MESSAGE";
                    break;
                case MessageType.Plane:
                    function = "SHOW_PLANE_MESSAGE";
                    break;
                case MessageType.CopsAndCrooks:
                    function = "SHOW_BIG_MP_MESSAGE";
                    break;
                case MessageType.Weapon:
                    function = "SHOW_WEAPON_PURCHASED";
                    break;
                case MessageType.CenteredLarge:
                    function = "SHOW_CENTERED_MP_MESSAGE_LARGE";
                    break;
                default:
                    throw new InvalidOperationException($"{type} is not a valid message type.");
            }

            // And add the parameters
            switch (type)
            {
                case MessageType.Customizable:
                    CallFunction(function, Title, Message, TextColor, BackgroundColor);
                    break;
                case MessageType.CopsAndCrooks:
                    CallFunction(function, Title, Message, Rank);
                    break;
                case MessageType.Weapon:
                    CallFunction(function, Title, Message, (int)weaponHash);
                    break;
                default:
                    CallFunction(function, Title, Message);
                    break;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Update()
        {
        }
        /// <summary>
        /// Fades the big message out.
        /// </summary>
        /// <param name="time">The time it will take to do the fade.</param>
        public void FadeOut(int time)
        {
            if (time < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(time), "Time can't be under zero.");
            }

            CallFunction("SHARD_ANIM_OUT", 0, time / 1000.0f, 0);

#if ALTV
            long currentTime = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long currentTime = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long currentTime = Game.GameTime;
#endif
            hideAfter = currentTime + time;
        }
        /// <inheritdoc/>
        public override void DrawFullScreen()
        {
#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif

            if (hideAfter > 0 && time > hideAfter)
            {
                Visible = false;
                hideAfter = 0;
            }

            base.DrawFullScreen();
        }

        #endregion
    }
}

// Scaleform/CelebrationBackground.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The background of the celebration scaleform.
    /// </summary>
    public class CelebrationBackground : CelebrationCore
    {
        #region Constructors

        /// <summary>
        /// Initializes a new Celebration background.
        /// </summary>
        public CelebrationBackground() : base("MP_CELEBRATION_BG")
        {
        }

        #endregion
    }
}

// Scaleform/MessageType.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The type for a big message.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// A centered message with customizable text an d background colors.
        /// Internally called SHOW_SHARD_CENTERED_MP_MESSAGE.
        /// </summary>
        Customizable = 0,
        /// <summary>
        /// Used when you rank up on GTA Online.
        /// Internally called SHOW_SHARD_CREW_RANKUP_MP_MESSAGE.
        /// </summary>
        RankUp = 1,
        /// <summary>
        /// The Mission Passed screen on PS3 and Xbox 360.
        /// Internally called SHOW_MISSION_PASSED_MESSAGE.
        /// </summary>
        MissionPassedOldGen = 2,
        /// <summary>
        /// The Message Type shown on the Wasted screen.
        /// Internally called SHOW_SHARD_WASTED_MP_MESSAGE.
        /// </summary>
        Wasted = 3,
        /// <summary>
        /// Used on the GTA Online Freemode event announcements.
        /// Internally called SHOW_PLANE_MESSAGE.
        /// </summary>
        Plane = 4,
        /// <summary>
        /// Development leftover from when GTA Online was Cops and Crooks.
        /// Internally called SHOW_BIG_MP_MESSAGE.
        /// </summary>
        CopsAndCrooks = 5,
        /// <summary>
        /// Message shown when the player purchases a weapon.
        /// Internally called SHOW_WEAPON_PURCHASED.
        /// </summary>
        Weapon = 6,
        /// <summary>
        /// Unknown where this one is used.
        /// Internally called SHOW_CENTERED_MP_MESSAGE_LARGE.
        /// </summary>
        CenteredLarge = 7,
    }
}

// Scaleform/LoadingScreen.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// Loading screen like the transition between story mode and online.
    /// </summary>
    public class LoadingScreen : BaseScaleform
    {
        private string title;
        private string subtitle;
        private string description;

        #region Properties

        /// <summary>
        /// The title of the loading screen.
        /// </summary>
        public string Title
        {
            get => title;
            set => title = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The subtitle of the loading screen.
        /// </summary>
        public string Subtitle
        {
            get => subtitle;
            set => subtitle = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The description of the loading screen.
        /// </summary>
        public string Description
        {
            get => description;
            set => description = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The Texture Dictionary (TXD) where the texture is loaded.
        /// </summary>
        public string Dictionary { get; private set; }
        /// <summary>
        /// The texture in the dictionary.
        /// </summary>
        public string Texture { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new GTA Online like loading screen with no image.
        /// </summary>
        /// <param name="title">The title of the screen.</param>
        /// <param name="subtitle">The subtitle of the screen.</param>
        /// <param name="description">The description of the screen.</param>
        public LoadingScreen(string title, string subtitle, string description) : this(title, subtitle, description, string.Empty, string.Empty)
        {
        }
        /// <summary>
        /// Creates a new GTA Online like loading screen with a custom texture.
        /// </summary>
        /// <param name="title">The title of the screen.</param>
        /// <param name="subtitle">The subtitle of the screen.</param>
        /// <param name="description">The description of the screen.</param>
        /// <param name="dictionary">The dictionary where the texture is located.</param>
        /// <param name="texture">The texture to use on the right.</param>
        public LoadingScreen(string title, string subtitle, string description, string dictionary, string texture) : base("GTAV_ONLINE")
        {
            // Tell the Scaleform to use the online loading screen
            CallFunction("HIDE_ONLINE_LOGO");
            CallFunction("SETUP_BIGFEED", false);
            // Save the values
            Title = title;
            Subtitle = subtitle;
            Description = description;
            Dictionary = dictionary;
            Texture = texture;
            // And send them back to the scaleform
            Update();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Changes the texture shown on the loading screen.
        /// </summary>
        /// <param name="dictionary">The Texture Dictionary or TXD.</param>
        /// <param name="texture">The Texture name.</param>
        public void ChangeTexture(string dictionary, string texture)
        {
            Dictionary = dictionary;
            Texture = texture;
            Update();
        }
        /// <summary>
        /// Updates the Title, Description and Image of the loading screen.
        /// </summary>
        public override void Update()
        {
            CallFunction("SET_BIGFEED_INFO", "footerStr", Description, 0, Dictionary, Texture, Subtitle, "urlDeprecated", Title);
        }

        #endregion
    }
}

// Scaleform/CelebrationCore.cs
namespace LemonUI.Scaleform
{
    /// <summary>
    /// The base of all MP_CELEBRATION* scaleforms.
    /// </summary>
    public abstract class CelebrationCore : BaseScaleform
    {
        #region Constructors

        /// <summary>
        /// Initializes a new class with the core behavior of the Celebration scaleform.
        /// </summary>
        /// <param name="sc">The scaleform to use.</param>
        public CelebrationCore(string sc) : base(sc)
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Updates the celebration scaleform.
        /// </summary>
        public override void Update()
        {
        }

        #endregion
    }
}

// Extensions/FloatExtensions.cs
namespace LemonUI.Extensions
{
    /// <summary>
    /// Extensions for the float class.
    /// </summary>
    [Obsolete("Please use LemonUI.Tools.Extensions instead.", true)]
    public static class FloatExtensions
    {
        /// <summary>
        /// Converts a scaled X or Width float to a relative one.
        /// </summary>
        /// <param name="fin">The float to convert.</param>
        /// <returns>A relative float between 0 and 1.</returns>
        public static float ToXRelative(this float fin) => Tools.Extensions.ToXRelative(fin);
        /// <summary>
        /// Converts a scaled Y or Height float to a relative one.
        /// </summary>
        /// <param name="fin">The float to convert.</param>
        /// <returns>A relative float between 0 and 1.</returns>
        public static float ToYRelative(this float fin) => Tools.Extensions.ToYRelative(fin);
        /// <summary>
        /// Converts an relative X or Width float to an scaled one.
        /// </summary>
        /// <param name="fin">The float to convert.</param>
        /// <returns>A scaled float.</returns>
        public static float ToXAbsolute(this float fin) => Tools.Extensions.ToXScaled(fin);
        /// <summary>
        /// Converts an relative Y or Height float to an scaled one.
        /// </summary>
        /// <param name="fin">The float to convert.</param>
        /// <returns>A scaled float.</returns>
        public static float ToYAbsolute(this float fin) => Tools.Extensions.ToYScaled(fin);
    }
}

// Extensions/PointExtensions.cs
namespace LemonUI.Extensions
{
    /// <summary>
    /// Extensions for the Point and PointF classes.
    /// </summary>
    [Obsolete("Please use LemonUI.Tools.Extensions instead.", true)]
    public static class PointExtensions
    {
        /// <summary>
        /// Converts a scaled 1080-based position into a relative one.
        /// </summary>
        /// <param name="point">The scaled PointF.</param>
        /// <returns>A new PointF with relative values.</returns>
        public static PointF ToRelative(this PointF point) => Tools.Extensions.ToRelative(point);
        /// <summary>
        /// Converts a normalized 0-1 position into a scaled one.
        /// </summary>
        /// <param name="point">The relative PointF.</param>
        /// <returns>A new PointF with scaled values.</returns>
        public static PointF ToAbsolute(this PointF point) => Tools.Extensions.ToScaled(point);
    }
}

// Extensions/SizeExtensions.cs
namespace LemonUI.Extensions
{
    /// <summary>
    /// Extensions for the Size and SizeF classes.
    /// </summary>
    [Obsolete("Please use LemonUI.Tools.Extensions instead.", true)]
    public static class SizeExtensions
    {
        /// <summary>
        /// Converts a scaled 1080-based size into a relative one.
        /// </summary>
        /// <param name="size">The scaled SizeF.</param>
        /// <returns>A new SizeF with relative values.</returns>
        public static SizeF ToRelative(this SizeF size) => Tools.Extensions.ToRelative(size);
        /// <summary>
        /// Converts a normalized 0-1 size into a scaled one.
        /// </summary>
        /// <param name="size">The relative SizeF.</param>
        /// <returns>A new SizeF with scaled values.</returns>
        public static SizeF ToAbsolute(this SizeF size) => Tools.Extensions.ToScaled(size);
    }
}

// Tools/Extensions.cs
namespace LemonUI.Tools
{
    /// <summary>
    /// Extensions for converting values between relative and scaled.
    /// </summary>
    public static class Extensions
    {
        #region Float

        /// <summary>
        /// Converts the scaled X or Width to a relative one.
        /// </summary>
        /// <param name="x">The value to convert.</param>
        /// <returns>A relative float between 0 and 1.</returns>
        public static float ToXRelative(this float x) => x / (1080f * GameScreen.AspectRatio);
        /// <summary>
        /// Converts the scaled Y or Height to a relative one.
        /// </summary>
        /// <param name="y">The value to convert.</param>
        /// <returns>A relative float between 0 and 1.</returns>
        public static float ToYRelative(this float y) => y / 1080f;
        /// <summary>
        /// Converts the relative X or Width float to a scaled one.
        /// </summary>
        /// <param name="x">The float to convert.</param>
        /// <returns>A scaled float.</returns>
        public static float ToXScaled(this float x) => (1080f * GameScreen.AspectRatio) * x;
        /// <summary>
        /// Converts the relative Y or Height float to a scaled one.
        /// </summary>
        /// <param name="y">The float to convert.</param>
        /// <returns>A scaled float.</returns>
        public static float ToYScaled(this float y) => 1080f * y;

        #endregion

        #region PointF

        /// <summary>
        /// Converts a scaled 1080p-based position into a relative one.
        /// </summary>
        /// <param name="point">The scaled PointF.</param>
        /// <returns>A new PointF with relative values.</returns>
        public static PointF ToRelative(this PointF point) => new PointF(point.X.ToXRelative(), point.Y.ToYRelative());
        /// <summary>
        /// Converts a relative 0-1 position into a scaled one.
        /// </summary>
        /// <param name="point">The relative PointF.</param>
        /// <returns>A new PointF with scaled values.</returns>
        public static PointF ToScaled(this PointF point) => new PointF(point.X.ToXScaled(), point.Y.ToYScaled());

        #endregion

        #region SizeF

        /// <summary>
        /// Converts a scaled 1080p-based position into a relative one.
        /// </summary>
        /// <param name="size">The scaled SizeF.</param>
        /// <returns>A new SizeF with relative values.</returns>
        public static SizeF ToRelative(this SizeF size) => new SizeF(size.Width.ToXRelative(), size.Height.ToYRelative());
        /// <summary>
        /// Converts a relative 0-1 position into a scaled one.
        /// </summary>
        /// <param name="size">The relative SizeF.</param>
        /// <returns>A new SizeF with scaled values.</returns>
        public static SizeF ToScaled(this SizeF size) => new SizeF(size.Width.ToXScaled(), size.Height.ToYScaled());

        #endregion
    }
}

// Tools/GameScreen.cs
namespace LemonUI.Tools
{
    /// <summary>
    /// The screen of the game being rendered.
    /// </summary>
    public static class GameScreen
    {
        #region Properties

        /// <summary>
        /// Gets the actual Screen resolution the game is being rendered at.
        /// </summary>
        public static SizeF AbsoluteResolution
        {
            get
            {
#if ALTV
                int height = 0, width = 0;
                Alt.Natives.GetActualScreenResolution(ref width, ref height);
                return new SizeF(width, height);
#elif FIVEM
                return CitizenFX.Core.UI.Screen.Resolution;
#elif RAGEMP
                ScreenResolutionType raw = Game.ScreenResolution;
                return new SizeF(raw.Width, raw.Height);
#elif RPH
                return Game.Resolution;
#elif SHVDN3 || SHVDNC
                return GTA.UI.Screen.Resolution;
#endif
            }
        }
        /// <summary>
        /// The Aspect Ratio of the screen.
        /// </summary>
        public static float AspectRatio
        {
            get
            {
#if FIVEM
                return API.GetAspectRatio(false);
#elif RAGEMP
                return Invoker.Invoke<float>(Natives.GetAspectRatio);
#elif RPH
                return NativeFunction.CallByHash<float>(0xF1307EF624A80D87, false);
#elif SHVDN3 || SHVDNC
                return Function.Call<float>(Hash.GET_ASPECT_RATIO, false);
#elif ALTV
                return Alt.Natives.GetAspectRatio(false);
#endif
            }
        }
        /// <summary>
        /// The location of the cursor on screen between 0 and 1.
        /// </summary>
        public static PointF Cursor
        {
            get
            {
#if FIVEM
                float cursorX = API.GetControlNormal(0, (int)Control.CursorX);
                float cursorY = API.GetControlNormal(0, (int)Control.CursorY);
#elif ALTV
                float cursorX = Alt.Natives.GetControlNormal(0, (int)Control.CursorX);
                float cursorY = Alt.Natives.GetControlNormal(0, (int)Control.CursorY);
#elif RAGEMP
                float cursorX = Invoker.Invoke<float>(Natives.GetControlNormal, 0, (int)Control.CursorX);
                float cursorY = Invoker.Invoke<float>(Natives.GetControlNormal, 0, (int)Control.CursorY);
#elif RPH
                float cursorX = NativeFunction.CallByHash<float>(0xEC3C9B8D5327B563, 0, (int)Control.CursorX);
                float cursorY = NativeFunction.CallByHash<float>(0xEC3C9B8D5327B563, 0, (int)Control.CursorY);
#elif SHVDN3 || SHVDNC
                float cursorX = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorX);
                float cursorY = Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)Control.CursorY);
#endif
                return new PointF(cursorX.ToXScaled(), cursorY.ToYScaled());
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Checks if the cursor is inside of the scaled area.
        /// </summary>
        /// <param name="pos">The scaled position.</param>
        /// <param name="size">The scaled size of the area.</param>
        /// <returns><see langword="true"/> if the cursor is in the specified bounds, <see langword="false"/> otherwise.</returns>
        public static bool IsCursorInArea(PointF pos, SizeF size) => IsCursorInArea(pos.X, pos.Y, size.Width, size.Height);
        /// <summary>
        /// Checks if the cursor is inside of the scaled area.
        /// </summary>
        /// <param name="x">The scaled X position.</param>
        /// <param name="y">The scaled Y position.</param>
        /// <param name="width">The scaled width of the area.</param>
        /// <param name="height">The scaled height of the area.</param>
        /// <returns><see langword="true"/> if the cursor is in the specified bounds, <see langword="false"/> otherwise.</returns>
        public static bool IsCursorInArea(float x, float y, float width, float height)
        {
            PointF cursorPosition = Cursor;

            bool isX = cursorPosition.X >= x && cursorPosition.X <= x + width;
            bool isY = cursorPosition.Y > y && cursorPosition.Y < y + height;
            return isX && isY;
        }
        /// <summary>
        /// Shows the cursor during the current game frame.
        /// </summary>
        public static void ShowCursorThisFrame()
        {
#if FIVEM
            API.SetMouseCursorActiveThisFrame();
#elif ALTV
            Alt.Natives.SetMouseCursorThisFrame();
#elif RAGEMP
            Invoker.Invoke(0xAAE7CE1D63167423);
#elif RPH
            NativeFunction.CallByHash<int>(0xAAE7CE1D63167423);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.SET_MOUSE_CURSOR_THIS_FRAME);
#endif
        }

        #endregion
    }
}

// Tools/SafeZone.cs
namespace LemonUI.Tools
{
    /// <summary>
    /// Tools for changing, resetting and retrieving the Safe Zone of the game.
    /// </summary>
    public static class SafeZone
    {
        #region Properties

        /// <summary>
        /// The size of the safe zone.
        /// </summary>
        /// <remarks>
        /// This property should not be used to manually calculate the safe zone. Use <see cref="GetPositionAt(System.Drawing.PointF,LemonUI.GFXAlignment,LemonUI.GFXAlignment)"/> to get the safe zone size.
        /// </remarks>
        public static float Size
        {
            get
            {
#if ALTV
                return Alt.Natives.GetSafeZoneSize();
#elif FIVEM
                return API.GetSafeZoneSize();
#elif RAGEMP
                return Invoker.Invoke<float>(Natives.GetSafeZoneSize);
#elif RPH
                return NativeFunction.CallByHash<float>(0xBAF107B6BB2C97F0);
#elif SHVDN3 || SHVDNC
                return Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE);
#endif
            }
        }
        /// <summary>
        /// The top left corner after the safe zone.
        /// </summary>
        public static PointF TopLeft => GetPositionAt(PointF.Empty, GFXAlignment.Left, GFXAlignment.Top);
        /// <summary>
        /// The top right corner after the safe zone.
        /// </summary>
        public static PointF TopRight => GetPositionAt(PointF.Empty, GFXAlignment.Right, GFXAlignment.Top);
        /// <summary>
        /// The bottom left corner after the safe zone.
        /// </summary>
        public static PointF BottomLeft => GetPositionAt(PointF.Empty, GFXAlignment.Left, GFXAlignment.Bottom);
        /// <summary>
        /// The bottom right corner after the safe zone.
        /// </summary>
        public static PointF BottomRight => GetPositionAt(PointF.Empty, GFXAlignment.Right, GFXAlignment.Bottom);

        #endregion

        #region Tools

        private static GFXAlignment AlignmentToGFXAlignment(Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.Left:
                    return GFXAlignment.Left;
                case Alignment.Right:
                    return GFXAlignment.Right;
                case Alignment.Center:
                    return GFXAlignment.Center;
                default:
                    throw new ArgumentException("Alignment is not one of the allowed values (Left, Right, Center).", nameof(alignment));
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Converts the specified position into one that is aware of the safe zone.
        /// </summary>
        /// <param name="og">The original 1080p based position.</param>
        /// <returns>A new 1080p based position that is aware of the the Alignment.</returns>
        public static PointF GetSafePosition(PointF og) => GetSafePosition(og.X, og.Y);
        /// <summary>
        /// Converts the specified position into one that is aware of <see cref="SetAlignment(GFXAlignment, GFXAlignment)"/>.
        /// </summary>
        /// <param name="x">The 1080p based X position.</param>
        /// <param name="y">The 1080p based Y position.</param>
        /// <returns>A new 1080p based position that is aware of the the Alignment.</returns>
        public static PointF GetSafePosition(float x, float y)
        {
            float relativeX = x.ToXRelative();
            float relativeY = y.ToYRelative();

            float realX = 0, realY = 0;
#if FIVEM
            API.GetScriptGfxPosition(relativeX, relativeY, ref realX, ref realY);
#elif ALTV
            Alt.Natives.GetScriptGfxAlignPosition(relativeX, relativeY, ref realX, ref realY);
#elif RAGEMP
            FloatReference argX = new FloatReference();
            FloatReference argY = new FloatReference();
            Invoker.Invoke<int>(0x6DD8F5AA635EB4B2, relativeX, relativeY, argX, argY);
            realX = argX.Value;
            realY = argY.Value;
#elif RPH
            using (NativePointer argX = new NativePointer(4))
            using (NativePointer argY = new NativePointer(4))
            {
                NativeFunction.CallByHash<int>(0x6DD8F5AA635EB4B2, relativeX, relativeY, argX, argY);
                realX = argX.GetValue<float>();
                realY = argY.GetValue<float>();
            }
#elif SHVDN3 || SHVDNC
            unsafe
            {
                Function.Call(Hash.GET_SCRIPT_GFX_ALIGN_POSITION, relativeX, relativeY, &realX, &realY);
            }
#endif

            return new PointF(realX.ToXScaled(), realY.ToYScaled());
        }
        /// <summary>
        /// Sets the alignment for the safe zone.
        /// </summary>
        /// <param name="horizontal">The Horizontal alignment of the items.</param>
        /// <param name="vertical">The vertical alignment of the items.</param>
        public static void SetAlignment(Alignment horizontal, GFXAlignment vertical) => SetAlignment(AlignmentToGFXAlignment(horizontal), vertical);
        /// <summary>
        /// Sets the alignment for the safe zone.
        /// </summary>
        /// <param name="horizontal">The Horizontal alignment of the items.</param>
        /// <param name="vertical">The vertical alignment of the items.</param>
        public static void SetAlignment(GFXAlignment horizontal, GFXAlignment vertical)
        {
#if FIVEM
            API.SetScriptGfxAlign((int)horizontal, (int)vertical);
            API.SetScriptGfxAlignParams(0, 0, 0, 0);
#elif ALTV
            Alt.Natives.SetScriptGfxAlign((int)horizontal, (int)vertical);
            Alt.Natives.SetScriptGfxAlignParams(0, 0, 0, 0);
#elif RAGEMP
            Invoker.Invoke(0xB8A850F20A067EB6, (int)horizontal, (int)vertical);
            Invoker.Invoke(0xF5A2C681787E579D, 0, 0, 0, 0);
#elif RPH
            NativeFunction.CallByHash<int>(0xB8A850F20A067EB6, (int)horizontal, (int)vertical);
            NativeFunction.CallByHash<int>(0xF5A2C681787E579D, 0, 0, 0, 0);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.SET_SCRIPT_GFX_ALIGN, (int)horizontal, (int)vertical);
            Function.Call(Hash.SET_SCRIPT_GFX_ALIGN_PARAMS, 0, 0, 0, 0);
#endif
        }
        /// <summary>
        /// Resets the alignment of the safe zone.
        /// </summary>
        public static void ResetAlignment()
        {
#if FIVEM
            API.ResetScriptGfxAlign();
#elif ALTV
            Alt.Natives.ResetScriptGfxAlign();
#elif RAGEMP
            Invoker.Invoke(0xE3A3DB414A373DAB);
#elif RPH
            NativeFunction.CallByHash<int>(0xE3A3DB414A373DAB);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.RESET_SCRIPT_GFX_ALIGN);
#endif
        }
        /// <summary>
        /// Gets the specified position with the specified safe zone alignment.
        /// </summary>
        /// <param name="position">The position to get.</param>
        /// <param name="horizontal">The horizontal alignment.</param>
        /// <param name="vertical">The vertical alignment.</param>
        /// <returns>The  safe zone alignment.</returns>
        public static PointF GetPositionAt(PointF position, Alignment horizontal, GFXAlignment vertical) => GetPositionAt(position, AlignmentToGFXAlignment(horizontal), vertical);
        /// <summary>
        /// Gets the specified position with the specified safe zone alignment.
        /// </summary>
        /// <param name="position">The position to get.</param>
        /// <param name="horizontal">The horizontal alignment.</param>
        /// <param name="vertical">The vertical alignment.</param>
        /// <returns>The scaled safe zone alignment.</returns>
        public static PointF GetPositionAt(PointF position, GFXAlignment horizontal, GFXAlignment vertical)
        {
            SetAlignment(horizontal, vertical);
            PointF pos = GetSafePosition(position);
            ResetAlignment();
            return pos;
        }

        #endregion
    }
}

// Elements/ScaledBink.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A Bink Video file.
    /// </summary>
    public class ScaledBink : BaseElement, IDisposable
    {
        #region Fields

        private string name = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The ID of the Bink Video Instance.
        /// </summary>
        public int Id { get; private set; } = -1;
        /// <summary>
        /// The name of the Bink Video file.
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                name = value ?? throw new ArgumentNullException(nameof(value));

#if ALTV
                Id = Alt.Natives.SetBinkMovie(name);
#elif FIVEM
                Id = API.SetBinkMovie(name);
#elif RAGEMP
                Id = Invoker.Invoke<int>(0xfc36643f7a64338f, name);
#elif RPH
                Id = NativeFunction.CallByHash<int>(0xfc36643f7a64338f, name);
#elif SHVDN3 || SHVDNC
                Id = Function.Call<int>(Hash.SET_BINK_MOVIE, name);
#endif
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Bink Video playback.
        /// </summary>
        /// <param name="name">The name of the bik file.</param>
        public ScaledBink(string name) : this(name, PointF.Empty, SizeF.Empty)
        {
        }
        /// <summary>
        /// Creates a new Bink Video playback.
        /// </summary>
        /// <param name="name">The name of the bik file.</param>
        /// <param name="size">The size of the video window.</param>
        public ScaledBink(string name, SizeF size) : this(name, PointF.Empty, size)
        {
        }
        /// <summary>
        /// Creates a new Bink Video playback.
        /// </summary>
        /// <param name="name">The name of the bik file.</param>
        /// <param name="pos">The position of the video window.</param>
        /// <param name="size">The size of the video window.</param>
        public ScaledBink(string name, PointF pos, SizeF size) : base(pos, size)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        #endregion

        #region Finalizer

        /// <summary>
        /// Finalizes an instance of the <see cref="ScaledBink"/> class.
        /// </summary>
        ~ScaledBink() => Dispose();

        #endregion

        #region Functions

        /// <summary>
        /// Draws the Bink Movie at the specified location.
        /// </summary>
        public override void Draw()
        {
            if (Id == -1)
            {
                return;
            }

#if ALTV
            Alt.Natives.PlayBinkMovie(Id);
            Alt.Natives.DrawBinkMovie(Id, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, 0.0f, 255, 255, 255, 255);
#elif FIVEM
            API.PlayBinkMovie(Id);
            API.DrawBinkMovie(Id, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, 0.0f, 255, 255, 255, 255);
#elif RAGEMP
            Invoker.Invoke<int>(0xE178310643033958, Id);
            Invoker.Invoke<int>(0x7118E83EEB9F7238, Id, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, 0.0f, 255, 255, 255, 255);
#elif RPH
            NativeFunction.CallByHash<int>(0xE178310643033958, Id);
            NativeFunction.CallByHash<int>(0x7118E83EEB9F7238, Id, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, 0.0f, 255, 255, 255, 255);
#elif SHVDN3 || SHVDNC
            Function.Call<int>(Hash.PLAY_BINK_MOVIE, Id);
            Function.Call<int>(Hash.DRAW_BINK_MOVIE, Id, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, 0.0f, 255, 255, 255, 255);
#endif
        }
        /// <summary>
        /// Stops the playback of the Bink Video.
        /// </summary>
        /// <remarks>
        /// If <see cref="Draw"/> is called after this function. playback will start again.
        /// </remarks>
        public void Stop()
        {
            if (Id == -1)
            {
                return;
            }

#if ALTV
            Alt.Natives.StopBinkMovie(Id);
#elif FIVEM
            API.StopBinkMovie(Id);
#elif RAGEMP
            Invoker.Invoke<int>(0x63606A61DE68898A, Id);
#elif RPH
            NativeFunction.CallByHash<int>(0x63606A61DE68898A, Id);
#elif SHVDN3 || SHVDNC
            Function.Call<int>(Hash.STOP_BINK_MOVIE, Id);
#endif
        }
        /// <summary>
        /// Disposes the Bink Video ID.
        /// </summary>
        public void Dispose()
        {
            if (Id == -1)
            {
                return;
            }

#if ALTV
            Alt.Natives.ReleaseBinkMovie(Id);
#elif FIVEM
            API.ReleaseBinkMovie(Id);
#elif RAGEMP
            Invoker.Invoke<int>(0x04D950EEFA4EED8C, Id);
#elif RPH
            NativeFunction.CallByHash<int>(0x04D950EEFA4EED8C, Id);
#elif SHVDN3 || SHVDNC
            Function.Call<int>(Hash.RELEASE_BINK_MOVIE, Id);
#endif

            Id = -1;
        }
        /// <inheritdoc/>
        public override void Recalculate()
        {
            base.Recalculate();
            relativePosition.X += relativeSize.Width * 0.5f;
            relativePosition.Y += relativeSize.Height * 0.5f;
        }

        #endregion
    }
}

// Elements/ScaledText.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A text string.
    /// </summary>
    public class ScaledText : IText
    {
        #region Constants

        /// <summary>
        /// The size of every chunk of text.
        /// </summary>
        private const int chunkSize = 90;

        #endregion

        #region Fields

        /// <summary>
        /// The scaled 1080p based screen position.
        /// </summary>
        private PointF scaledPosition = PointF.Empty;
        /// <summary>
        /// The relative 0-1 relative position.
        /// </summary>
        private PointF relativePosition = PointF.Empty;
        /// <summary>
        /// The raw string of text.
        /// </summary>
        private string text = string.Empty;
        /// <summary>
        /// The raw string split into equally sized strings.
        /// </summary>
        private List<string> chunks = new List<string>();
        /// <summary>
        /// The alignment of the item.
        /// </summary>
        private Alignment alignment = Alignment.Left;
        /// <summary>
        /// The word wrap value passed by the user.
        /// </summary>
        private float internalWrap = 0f;
        /// <summary>
        /// The real word wrap value based on the position of the text.
        /// </summary>
        private float realWrap = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// The position of the text.
        /// </summary>
        public PointF Position
        {
            get => scaledPosition;
            set
            {
                scaledPosition = value;
                relativePosition = value.ToRelative();
            }
        }
        /// <summary>
        /// The text to draw.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                text = value ?? throw new ArgumentNullException(nameof(value));
                Slice();
            }
        }
        /// <summary>
        /// The color of the text.
        /// </summary>
        public Color Color { get; set; } = Color.FromArgb(255, 255, 255, 255);
        /// <summary>
        /// The game font to use.
        /// </summary>
        public Font Font { get; set; } = Font.ChaletLondon;
        /// <summary>
        /// The scale of the text.
        /// </summary>
        public float Scale { get; set; } = 1f;
        /// <summary>
        /// If the text should have a drop down shadow.
        /// </summary>
        public bool Shadow { get; set; } = false;
        /// <summary>
        /// If the test should have an outline.
        /// </summary>
        public bool Outline { get; set; } = false;
        /// <summary>
        /// The alignment of the text.
        /// </summary>
        public Alignment Alignment
        {
            get => alignment;
            set
            {
                alignment = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The distance from the start position where the text will be wrapped into new lines.
        /// </summary>
        public float WordWrap
        {
            get
            {
                return internalWrap;
            }
            set
            {
                internalWrap = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The width that the text takes from the screen.
        /// </summary>
        public float Width
        {
            get
            {
#if FIVEM
                API.BeginTextCommandWidth("CELL_EMAIL_BCON");
                Add();
                return API.EndTextCommandGetWidth(true) * 1f.ToXScaled();
#elif ALTV
                Alt.Natives.BeginTextCommandGetScreenWidthOfDisplayText("CELL_EMAIL_BCON");
                Add();
                return Alt.Natives.EndTextCommandGetScreenWidthOfDisplayText(true) * 1f.ToXScaled();
#elif RAGEMP
                Invoker.Invoke(Natives.BeginTextCommandWidth, "CELL_EMAIL_BCON");
                Add();
                return Invoker.Invoke<float>(Natives.EndTextCommandGetWidth) * 1f.ToXScaled();
#elif RPH
                NativeFunction.CallByHash<int>(0x54CE8AC98E120CAB, "CELL_EMAIL_BCON");
                Add();
                return NativeFunction.CallByHash<float>(0x85F061DA64ED2F67, true) * 1f.ToXScaled();
#elif SHVDN3 || SHVDNC
                Function.Call(Hash.BEGIN_TEXT_COMMAND_GET_SCREEN_WIDTH_OF_DISPLAY_TEXT, "CELL_EMAIL_BCON");
                Add();
                return Function.Call<float>(Hash.END_TEXT_COMMAND_GET_SCREEN_WIDTH_OF_DISPLAY_TEXT, true) * 1f.ToXScaled();
#endif
            }
        }
        /// <summary>
        /// The number of lines used by this text.
        /// </summary>
        public int LineCount
        {
            get
            {
#if FIVEM
                API.BeginTextCommandLineCount("CELL_EMAIL_BCON");
#elif ALTV
                Alt.Natives.BeginTextCommandGetNumberOfLinesForString("CELL_EMAIL_BCON");
#elif RAGEMP
                Invoker.Invoke(Natives.BeginTextCommandLineCount, "CELL_EMAIL_BCON");
#elif RPH
                NativeFunction.CallByHash<int>(0x521FB041D93DD0E4, "CELL_EMAIL_BCON");
#elif SHVDN3 || SHVDNC
                Function.Call(Hash.BEGIN_TEXT_COMMAND_GET_NUMBER_OF_LINES_FOR_STRING, "CELL_EMAIL_BCON");
#endif
                Add();
#if FIVEM
                return API.EndTextCommandGetLineCount(relativePosition.X, relativePosition.Y);
#elif ALTV
                return Alt.Natives.EndTextCommandGetNumberOfLinesForString(relativePosition.X, relativePosition.Y);
#elif RAGEMP
                return Invoker.Invoke<int>(Natives.EndTextCommandGetLineCount, relativePosition.X, relativePosition.Y);
#elif RPH
                return NativeFunction.CallByHash<int>(0x9040DFB09BE75706, relativePosition.X, relativePosition.Y);
#elif SHVDN3 || SHVDNC
                return Function.Call<int>(Hash.END_TEXT_COMMAND_GET_NUMBER_OF_LINES_FOR_STRING, relativePosition.X, relativePosition.Y);
#endif
            }
        }
        /// <summary>
        /// The relative height of each line in the text.
        /// </summary>
        public float LineHeight
        {
            get
            {
                // Height will always be 1080
#if FIVEM
                return 1080 * API.GetTextScaleHeight(Scale, (int)Font);
#elif ALTV
                return 1080 * Alt.Natives.GetRenderedCharacterHeight(Scale, (int)Font);
#elif RAGEMP
                return 1080 * Invoker.Invoke<float>(Natives.GetTextScaleHeight, Scale, (int)Font);
#elif RPH
                return 1080 * NativeFunction.CallByHash<float>(0xDB88A37483346780, Scale, (int)Font);
#elif SHVDN3 || SHVDNC
                return 1080 * Function.Call<float>(Hash.GET_RENDERED_CHARACTER_HEIGHT, Scale, (int)Font);
#endif
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a text with the specified options.
        /// </summary>
        /// <param name="pos">The position where the text should be located.</param>
        /// <param name="text">The text to show.</param>
        public ScaledText(PointF pos, string text) : this(pos, text, 1f, Font.ChaletLondon)
        {
        }
        /// <summary>
        /// Creates a text with the specified options.
        /// </summary>
        /// <param name="pos">The position where the text should be located.</param>
        /// <param name="text">The text to show.</param>
        /// <param name="scale">The scale of the text.</param>
        public ScaledText(PointF pos, string text, float scale) : this(pos, text, scale, Font.ChaletLondon)
        {
        }
        /// <summary>
        /// Creates a text with the specified options
        /// </summary>
        /// <param name="pos">The position where the text should be located.</param>
        /// <param name="text">The text to show.</param>
        /// <param name="scale">The scale of the text.</param>
        /// <param name="font">The font to use.</param>
        public ScaledText(PointF pos, string text, float scale, Font font)
        {
            Position = pos;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Scale = scale;
            Font = font;
        }

        #endregion

        #region Tools

        /// <summary>
        /// Adds the text information for measurement.
        /// </summary>
        private void Add()
        {
            if (Scale == 0)
            {
                return;
            }
#if FIVEM
            foreach (string chunk in chunks)
            {
                API.AddTextComponentString(chunk);
            }
            API.SetTextFont((int)Font);
            API.SetTextScale(1f, Scale);
            API.SetTextColour(Color.R, Color.G, Color.B, Color.A);
            API.SetTextJustification((int)Alignment);
            if (Shadow)
            {
                API.SetTextDropShadow();
            }
            if (Outline)
            {
                API.SetTextOutline();
            }
            if (WordWrap > 0)
            {
                switch (Alignment)
                {
                    case Alignment.Center:
                        API.SetTextWrap(relativePosition.X - (realWrap * 0.5f), relativePosition.X + (realWrap * 0.5f));
                        break;
                    case Alignment.Left:
                        API.SetTextWrap(relativePosition.X, relativePosition.X + realWrap);
                        break;
                    case Alignment.Right:
                        API.SetTextWrap(relativePosition.X - realWrap, relativePosition.X);
                        break;
                }
            }
            else if (Alignment == Alignment.Right)
            {
                API.SetTextWrap(0f, relativePosition.X);
            }
#elif ALTV
            foreach (var chunk in chunks)
            {
                Alt.Natives.AddTextComponentSubstringPlayerName(chunk);
            }
            Alt.Natives.SetTextFont((int)Font);
            Alt.Natives.SetTextScale(1f, Scale);
            Alt.Natives.SetTextColour(Color.R, Color.G, Color.B, Color.A);
            Alt.Natives.SetTextJustification((int)Alignment);
            if (Shadow)
            {
                Alt.Natives.SetTextDropShadow();
            }
            if (Outline)
            {
                Alt.Natives.SetTextOutline();
            }
            if (WordWrap > 0)
            {
                switch (Alignment)
                {
                    case Alignment.Center:
                        Alt.Natives.SetTextWrap(relativePosition.X - (realWrap * 0.5f), relativePosition.X + (realWrap * 0.5f));
                        break;
                    case Alignment.Left:
                        Alt.Natives.SetTextWrap(relativePosition.X, relativePosition.X + realWrap);
                        break;
                    case Alignment.Right:
                        Alt.Natives.SetTextWrap(relativePosition.X - realWrap, relativePosition.X);
                        break;
                }
            }
            else if (Alignment == Alignment.Right)
            {
                Alt.Natives.SetTextWrap(0f, relativePosition.X);
            }
#elif RAGEMP
            foreach (string chunk in chunks)
            {
                Invoker.Invoke(Natives.AddTextComponentSubstringPlayerName, chunk);
            }
            Invoker.Invoke(Natives.SetTextFont, (int)Font);
            Invoker.Invoke(Natives.SetTextScale, 1f, Scale);
            Invoker.Invoke(Natives.SetTextColour, Color.R, Color.G, Color.B, Color.A);
            Invoker.Invoke(Natives.SetTextJustification, (int)Alignment);
            if (Shadow)
            {
                Invoker.Invoke(Natives.SetTextDropShadow);
            }
            if (Outline)
            {
                Invoker.Invoke(Natives.SetTextOutline);
            }
            if (WordWrap > 0)
            {
                switch (Alignment)
                {
                    case Alignment.Center:
                        Invoker.Invoke(Natives.SetTextWrap, relativePosition.X - (realWrap * 0.5f), relativePosition.X + (realWrap * 0.5f));
                        break;
                    case Alignment.Left:
                        Invoker.Invoke(Natives.SetTextWrap, relativePosition.X, relativePosition.X + realWrap);
                        break;
                    case Alignment.Right:
                        Invoker.Invoke(Natives.SetTextWrap, relativePosition.X - realWrap, relativePosition.X);
                        break;
                }
            }
            else if (Alignment == Alignment.Right)
            {
                Invoker.Invoke(0x63145D9C883A1A70, 0f, relativePosition.X);
            }
#elif RPH
            foreach (string chunk in chunks)
            {
                NativeFunction.CallByHash<int>(0x6C188BE134E074AA, chunk);
            }
            NativeFunction.CallByHash<int>(0x66E0276CC5F6B9DA, (int)Font);
            NativeFunction.CallByHash<int>(0x07C837F9A01C34C9, 1f, Scale);
            NativeFunction.CallByHash<int>(0xBE6B23FFA53FB442, Color.R, Color.G, Color.B, Color.A);
            NativeFunction.CallByHash<int>(0x4E096588B13FFECA, (int)Alignment);
            if (Shadow)
            {
                NativeFunction.CallByHash<int>(0x1CA3E9EAC9D93E5E);
            }
            if (Outline)
            {
                NativeFunction.CallByHash<int>(0x2513DFB0FB8400FE);
            }
            if (WordWrap > 0)
            {
                switch (Alignment)
                {
                    case Alignment.Center:
                        NativeFunction.CallByHash<int>(0x63145D9C883A1A70, relativePosition.X - (realWrap * 0.5f), relativePosition.X + (realWrap * 0.5f));
                        break;
                    case Alignment.Left:
                        NativeFunction.CallByHash<int>(0x63145D9C883A1A70, relativePosition.X, relativePosition.X + realWrap);
                        break;
                    case Alignment.Right:
                        NativeFunction.CallByHash<int>(0x63145D9C883A1A70, relativePosition.X - realWrap, relativePosition.X);
                        break;
                }
            }
            else if (Alignment == Alignment.Right)
            {
                NativeFunction.CallByHash<int>(0x63145D9C883A1A70, 0f, relativePosition.X);
            }
#elif SHVDN3 || SHVDNC
            foreach (string chunk in chunks)
            {
                Function.Call((Hash)0x6C188BE134E074AA, chunk); // _ADD_TEXT_COMPONENT_STRING on v2, ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME on v3
            }
            Function.Call(Hash.SET_TEXT_FONT, (int)Font);
            Function.Call(Hash.SET_TEXT_SCALE, 1f, Scale);
            Function.Call(Hash.SET_TEXT_COLOUR, Color.R, Color.G, Color.B, Color.A);
            Function.Call(Hash.SET_TEXT_JUSTIFICATION, (int)Alignment);
            if (Shadow)
            {
                Function.Call(Hash.SET_TEXT_DROP_SHADOW);
            }
            if (Outline)
            {
                Function.Call(Hash.SET_TEXT_OUTLINE);
            }
            if (WordWrap > 0)
            {
                switch (Alignment)
                {
                    case Alignment.Center:
                        Function.Call(Hash.SET_TEXT_WRAP, relativePosition.X - (realWrap * 0.5f), relativePosition.X + (realWrap * 0.5f));
                        break;
                    case Alignment.Left:
                        Function.Call(Hash.SET_TEXT_WRAP, relativePosition.X, relativePosition.X + realWrap);
                        break;
                    case Alignment.Right:
                        Function.Call(Hash.SET_TEXT_WRAP, relativePosition.X - realWrap, relativePosition.X);
                        break;
                }
            }
            else if (Alignment == Alignment.Right)
            {
                Function.Call(Hash.SET_TEXT_WRAP, 0f, relativePosition.X);
            }
#endif
        }
        /// <summary>
        /// Slices the string of text into appropiately saved chunks.
        /// </summary>
        private void Slice()
        {
            // If the entire text is under 90 bytes, save it as is and return
            if (Encoding.UTF8.GetByteCount(text) <= chunkSize)
            {
                chunks.Clear();
                chunks.Add(text);
                return;
            }

            // Create a new list of chunks and a temporary string
            List<string> newChunks = new List<string>();
            string temp = string.Empty;

            // Iterate over the characters in the string
            foreach (char character in text)
            {
                // Create a temporary string with the character
                string with = string.Concat(temp, character);
                // If this string is higher than 90 bytes, add the existing string onto the list
                if (Encoding.UTF8.GetByteCount(with) > chunkSize)
                {
                    newChunks.Add(temp);
                    temp = character.ToString();
                    continue;
                }
                // And save the new string generated
                temp = with;
            }

            // If after finishing we still have a piece, save it
            if (temp != string.Empty)
            {
                newChunks.Add(temp);
            }

            // Once we have finished, replace the old chunks
            chunks = newChunks;
        }
        /// <summary>
        /// Recalculates the size, position and word wrap of this item.
        /// </summary>
        public void Recalculate()
        {
            // Do the normal Size and Position recalculation
            relativePosition = scaledPosition.ToRelative();
            // And recalculate the word wrap if necessary
            if (internalWrap <= 0)
            {
                realWrap = 0;
            }
            else
            {
                realWrap = internalWrap.ToXRelative();
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Draws the text on the screen.
        /// </summary>
        public void Draw()
        {
#if FIVEM
            API.SetTextEntry("CELL_EMAIL_BCON");
#elif ALTV
            Alt.Natives.BeginTextCommandDisplayText("CELL_EMAIL_BCON");
#elif RAGEMP
            Invoker.Invoke(Natives.BeginTextCommandDisplayText, "CELL_EMAIL_BCON");
#elif RPH
            NativeFunction.CallByHash<int>(0x25FBB336DF1804CB, "CELL_EMAIL_BCON");
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0x25FBB336DF1804CB, "CELL_EMAIL_BCON"); // _SET_TEXT_ENTRY on v2, BEGIN_TEXT_COMMAND_DISPLAY_TEXT on v3
#endif

            Add();

#if FIVEM
            API.DrawText(relativePosition.X, relativePosition.Y);
#elif ALTV
            Alt.Natives.EndTextCommandDisplayText(relativePosition.X, relativePosition.Y, 0);
#elif RAGEMP
            Invoker.Invoke(Natives.EndTextCommandDisplayText, relativePosition.X, relativePosition.Y);
#elif RPH
            NativeFunction.CallByHash<int>(0xCD015E5BB0D96A57, relativePosition.X, relativePosition.Y);
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0xCD015E5BB0D96A57, relativePosition.X, relativePosition.Y); // _DRAW_TEXT on v2, END_TEXT_COMMAND_DISPLAY_TEXT on v3
#endif
        }

        #endregion
    }
}

// Elements/BaseElement.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// Base class for all of the 2D elements.
    /// </summary>
    public abstract class BaseElement : I2Dimensional
    {
        #region Fields

        /// <summary>
        /// The 1080 scaled position.
        /// </summary>
        protected internal PointF literalPosition = PointF.Empty;
        /// <summary>
        /// The relative position between 0 and 1.
        /// </summary>
        protected internal PointF relativePosition = PointF.Empty;
        /// <summary>
        /// The 1080 scaled size.
        /// </summary>
        protected internal SizeF literalSize = SizeF.Empty;
        /// <summary>
        /// The relative size between 0 and 1.
        /// </summary>
        protected internal SizeF relativeSize = SizeF.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The Position of the drawable.
        /// </summary>
        public virtual PointF Position
        {
            get => literalPosition;
            set
            {
                literalPosition = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The Size of the drawable.
        /// </summary>
        public virtual SizeF Size
        {
            get => literalSize;
            set
            {
                literalSize = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The Color of the drawable.
        /// </summary>
        public virtual Color Color { get; set; } = Color.FromArgb(255, 255, 255, 255);
        /// <summary>
        /// The rotation of the drawable.
        /// </summary>
        public virtual float Heading { get; set; } = 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="BaseElement"/> with the specified Position and Size.
        /// </summary>
        /// <param name="pos">The position of the Element.</param>
        /// <param name="size">The size of the Element.</param>
        public BaseElement(PointF pos, SizeF size)
        {
            literalPosition = pos;
            literalSize = size;
            Recalculate();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the size and position of this item.
        /// </summary>
        public virtual void Recalculate()
        {
            relativePosition = literalPosition.ToRelative();
            relativeSize = literalSize.ToRelative();
        }
        /// <summary>
        /// Draws the item on the screen.
        /// </summary>
        public abstract void Draw();

        #endregion
    }
}

// Elements/ScaledRectangle.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A 2D rectangle.
    /// </summary>
    public class ScaledRectangle : BaseElement
    {
        #region Constructor

        /// <summary>
        /// Creates a new <see cref="ScaledRectangle"/> with the specified Position and Size.
        /// </summary>
        /// <param name="pos">The position of the Rectangle.</param>
        /// <param name="size">The size of the Rectangle.</param>
        public ScaledRectangle(PointF pos, SizeF size) : base(pos, size)
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Draws the rectangle on the screen.
        /// </summary>
        public override void Draw()
        {
            if (Size == SizeF.Empty)
            {
                return;
            }
#if FIVEM
            API.DrawRect(relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Color.R, Color.G, Color.B, Color.A);
#elif RAGEMP
            Invoker.Invoke(Natives.DrawRect, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Color.R, Color.G, Color.B, Color.A);
#elif ALTV
            Alt.Natives.DrawRect(relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Color.R, Color.G, Color.B, Color.A,
                false);
#elif RPH
            NativeFunction.CallByHash<int>(0x3A618A217E5154F0, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Color.R, Color.G, Color.B, Color.A);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.DRAW_RECT, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Color.R, Color.G, Color.B, Color.A);
#endif
        }
        /// <summary>
        /// Recalculates the position based on the size.
        /// </summary>
        public override void Recalculate()
        {
            base.Recalculate();
            relativePosition.X += relativeSize.Width * 0.5f;
            relativePosition.Y += relativeSize.Height * 0.5f;
        }

        #endregion
    }
}

// Elements/ScaledTexture.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A 2D game texture.
    /// </summary>
    public class ScaledTexture : BaseElement
    {
        #region Fields

        private string dictionary;
        private string texture;

        #endregion

        #region Properties

        /// <summary>
        /// The dictionary where the texture is loaded.
        /// </summary>
        public string Dictionary
        {
            get => dictionary;
            set => dictionary = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The texture to draw from the dictionary.
        /// </summary>
        public string Texture
        {
            get => texture;
            set => texture = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="ScaledTexture"/> with a Position and Size of Zero.
        /// </summary>
        /// <param name="dictionary">The dictionary where the texture is located.</param>
        /// <param name="texture">The texture to draw.</param>
        public ScaledTexture(string dictionary, string texture) : this(PointF.Empty, SizeF.Empty, dictionary, texture)
        {
        }
        /// <summary>
        /// Creates a new <see cref="ScaledTexture"/> with a Position and Size of zero.
        /// </summary>
        /// <param name="pos">The position of the Texture.</param>
        /// <param name="size">The size of the Texture.</param>
        /// <param name="dictionary">The dictionary where the texture is located.</param>
        /// <param name="texture">The texture to draw.</param>
        public ScaledTexture(PointF pos, SizeF size, string dictionary, string texture) : base(pos, size)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            Request();
        }

        #endregion

        #region Tools

        /// <summary>
        /// Requests the texture dictionary for this class.
        /// </summary>
        private void Request()
        {
#if FIVEM
            if (!API.HasStreamedTextureDictLoaded(Dictionary))
            {
                API.RequestStreamedTextureDict(Dictionary, true);
            }
#elif RAGEMP
            if (!Invoker.Invoke<bool>(Natives.HasStreamedTextureDictLoaded, Dictionary))
            {
                Invoker.Invoke(Natives.RequestStreamedTextureDict, Dictionary, true);
            }
#elif ALTV
            if (!Alt.Natives.HasStreamedTextureDictLoaded(Dictionary))
            {
                Alt.Natives.RequestStreamedTextureDict(Dictionary, true);
            }
#elif RPH
            if (!NativeFunction.CallByHash<bool>(0x0145F696AAAAD2E4, Dictionary))
            {
                NativeFunction.CallByHash<int>(0xDFA2EF8E04127DD5, Dictionary, true);
            }
#elif SHVDN3 || SHVDNC
            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, Dictionary))
            {
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, Dictionary, true);
            }
#endif
        }

        #endregion

        #region Functions

        /// <summary>
        /// Draws the texture on the screen.
        /// </summary>
        public override void Draw()
        {
            if (Size == SizeF.Empty)
            {
                return;
            }
            Request();
#if FIVEM
            API.DrawSprite(Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Heading, Color.R, Color.G, Color.B, Color.A);
#elif RAGEMP
            Invoker.Invoke(Natives.DrawSprite, Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Heading, Color.R, Color.G, Color.B, Color.A);
#elif RPH
            NativeFunction.CallByHash<int>(0xE7FFAE5EBF23D890, Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Heading, Color.R, Color.G, Color.B, Color.A);
#elif ALTV
            Alt.Natives.DrawSprite(Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Heading, Color.R, Color.G, Color.B, Color.A, false, 0);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.DRAW_SPRITE, Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, Heading, Color.R, Color.G, Color.B, Color.A);
#endif
        }
        /// <summary>
        /// Draws a specific part of the texture on the screen.
        /// </summary>
        /// <param name="topLeft">The top left corner of the area to draw.</param>
        /// <param name="bottomRight">The bottom right corner of the area to draw.</param>
        public virtual void DrawSpecific(PointF topLeft, PointF bottomRight)
        {
            if (Size == SizeF.Empty)
            {
                return;
            }
            Request();
#if FIVEM
            API.DrawSpriteUv(Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y, Heading, Color.R, Color.G, Color.B, Color.A);
#elif RAGEMP
            Invoker.Invoke(0x95812F9B26074726, Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y, Heading, Color.R, Color.G, Color.B, Color.A);
#elif RPH
            NativeFunction.CallByHash<int>(0x95812F9B26074726, Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y, Heading, Color.R, Color.G, Color.B, Color.A);
#elif ALTV
            Alt.Natives.DrawSpriteArxWithUv(Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y, Heading, Color.R, Color.G, Color.B, Color.A, 0);
#elif SHVDN3 || SHVDNC
            Function.Call((Hash)0x95812F9B26074726, Dictionary, Texture, relativePosition.X, relativePosition.Y, relativeSize.Width, relativeSize.Height, topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y, Heading, Color.R, Color.G, Color.B, Color.A);
#endif
        }
        /// <summary>
        /// Recalculates the position based on the size.
        /// </summary>
        public override void Recalculate()
        {
            base.Recalculate();
            relativePosition.X += relativeSize.Width * 0.5f;
            relativePosition.Y += relativeSize.Height * 0.5f;
        }

        #endregion
    }
}

// Elements/Alignment.cs
namespace LemonUI
{
    /// <summary>
    /// The alignment of the element to draw.
    /// </summary>
    public enum Alignment
    {
        /// <summary>
        /// Aligns the element to the Center.
        /// </summary>
        Center = 0,
        /// <summary>
        /// Aligns the element to the Left.
        /// </summary>
        Left = 1,
        /// <summary>
        /// Aligns the element to the RIght.
        /// </summary>
        Right = 2,
    }
}

// Elements/IText.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A Drawable screen text.
    /// </summary>
    public interface IText : IRecalculable, IDrawable
    {
        #region Properties

        /// <summary>
        /// The position of the text.
        /// </summary>
        PointF Position { get; set; }
        /// <summary>
        /// The text to draw.
        /// </summary>
        string Text { get; set; }
        /// <summary>
        /// The color of the text.
        /// </summary>
        Color Color { get; set; }
        /// <summary>
        /// The game font to use.
        /// </summary>
        Font Font { get; set; }
        /// <summary>
        /// The scale of the text.
        /// </summary>
        float Scale { get; set; }
        /// <summary>
        /// If the text should have a drop down shadow.
        /// </summary>
        bool Shadow { get; set; }
        /// <summary>
        /// If the text should have an outline.
        /// </summary>
        bool Outline { get; set; }
        /// <summary>
        /// The alignment of the text.
        /// </summary>
        Alignment Alignment { get; set; }
        /// <summary>
        /// The maximum distance from X where the text would wrap into a new line.
        /// </summary>
        float WordWrap { get; set; }
        /// <summary>
        /// The width that the text takes from the screen.
        /// </summary>
        float Width { get; }
        /// <summary>
        /// The number of lines used by this text.
        /// </summary>
        int LineCount { get; }
        /// <summary>
        /// The height of each line of text.
        /// </summary>
        float LineHeight { get; }

        #endregion
    }
}

// Elements/I2Dimensional.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A 2D item that can be drawn on the screen.
    /// </summary>
    public interface I2Dimensional : IRecalculable, IDrawable
    {
        #region Properties

        /// <summary>
        /// The Position of the drawable.
        /// </summary>
        PointF Position { get; set; }
        /// <summary>
        /// The Size of the drawable.
        /// </summary>
        SizeF Size { get; set; }
        /// <summary>
        /// The Color of the drawable.
        /// </summary>
        Color Color { get; set; }

        #endregion
    }
}

// Elements/ScaledAnim.cs
namespace LemonUI.Elements
{
    /// <summary>
    /// A scaled animation using YTD files with all of the frames.
    /// </summary>
    public class ScaledAnim : ScaledTexture
    {
        #region Fields

        private float frameRate;
        private long start = 0;
        private int duration;

        #endregion

        #region Properties

        /// <summary>
        /// The total number of frames per second.
        /// </summary>
        public float FrameRate
        {
            get => frameRate;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The Frame Rate can't be equal or lower to zero.");
                }

                frameRate = value;
            }
        }
        /// <summary>
        /// The duration of the animation in milliseconds.
        /// </summary>
        public int Duration
        {
            get => duration;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The duration can't be under zero.");
                }

                duration = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new dictionary based animation.
        /// </summary>
        /// <param name="dict">The texture dictionary (YTD) to use.</param>
        public ScaledAnim(string dict) : this(dict, PointF.Empty, SizeF.Empty)
        {
        }
        /// <summary>
        /// Creates a new dictionary based animation.
        /// </summary>
        /// <param name="dict">The texture dictionary (YTD) to use.</param>
        /// <param name="size">The size of the animation.</param>
        public ScaledAnim(string dict, SizeF size) : this(dict, PointF.Empty, size)
        {
        }
        /// <summary>
        /// Creates a new dictionary based animation.
        /// </summary>
        /// <param name="dict">The texture dictionary (YTD) to use.</param>
        /// <param name="pos">The position of the animation.</param>
        /// <param name="size">The size of the animation.</param>
        public ScaledAnim(string dict, PointF pos, SizeF size) : base(pos, size, dict, string.Empty)
        {
            Dictionary = dict ?? throw new ArgumentNullException(nameof(dict));
        }

        #endregion

        #region Tools

        private void UpdateTexture()
        {
            if (Duration <= 0)
            {
                return;
            }

#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif

            long end = start + Duration;

            if (start == 0 || end <= time)
            {
                start = time;
            }

            float progress = (time - (float)start) / Duration;
            int totalFrames = (int)((duration / 1000.0f) * frameRate);
            int currentFrame = (int)(totalFrames * progress) + 1;

            if (progress < 0)
            {
                currentFrame = 1;
                start = time;
            }
            else if (currentFrame >= totalFrames)
            {
                currentFrame = totalFrames;
                start = time;
            }

            Texture = currentFrame.ToString();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Draws the animation.
        /// </summary>
        public override void Draw()
        {
            UpdateTexture();
            base.Draw();
        }
        /// <summary>
        /// Draws the animation while only drawing a specific part of the element.
        /// </summary>
        /// <param name="topLeft">The top left corner of the area to draw.</param>
        /// <param name="bottomRight">The bottom right corner of the area to draw.</param>
        public override void DrawSpecific(PointF topLeft, PointF bottomRight)
        {
            UpdateTexture();
            base.DrawSpecific(topLeft, bottomRight);
        }

        #endregion
    }
}

// Menus/GridValueChangedEventHandler.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the method that is called when the value on a grid is changed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="ItemActivatedArgs"/> with the item information.</param>
    public delegate void GridValueChangedEventHandler(object sender, GridValueChangedArgs e);
}

// Menus/Direction.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The movement direction of the item change.
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// The Direction is Unknown.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The item was moved to the Left.
        /// </summary>
        Left = 1,
        /// <summary>
        /// The item was moved to the Right.
        /// </summary>
        Right = 2,
    }
}

// Menus/NativeGridPanel.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents a grid where you can select X and Y values.
    /// </summary>
    public class NativeGridPanel : NativePanel
    {
        #region Fields

        private PointF position = PointF.Empty;
        private float width = 0;

        private readonly ScaledText labelTop = new ScaledText(PointF.Empty, "Y+", 0.33f)
        {
            Alignment = Alignment.Center
        };
        private readonly ScaledText labelBottom = new ScaledText(PointF.Empty, "Y-", 0.33f)
        {
            Alignment = Alignment.Center
        };
        private readonly ScaledText labelLeft = new ScaledText(PointF.Empty, "X-", 0.33f)
        {
            Alignment = Alignment.Right
        };
        private readonly ScaledText labelRight = new ScaledText(PointF.Empty, "X+", 0.33f);
        private readonly ScaledTexture grid = new ScaledTexture("pause_menu_pages_char_mom_dad", "nose_grid")
        {
            Color = Color.FromArgb(205, 105, 105, 102)
        };
        private readonly ScaledTexture dot = new ScaledTexture("commonmenu", "common_medal")
        {
            Color = Color.FromArgb(255, 255, 255, 255)
        };
        private PointF innerPosition = PointF.Empty;
        private SizeF innerSize = SizeF.Empty;
        private GridStyle style = GridStyle.Full;
        private float x;
        private float y;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override bool Clickable => true;
        /// <summary>
        /// The X value between 0 and 1.
        /// </summary>
        public float X
        {
            get
            {
                switch (style)
                {
                    case GridStyle.Full:
                    case GridStyle.Row:
                        return x;
                    default:
                        return 0.5f;
                }
            }
            set
            {
                if (value > 1 || value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (style == GridStyle.Column)
                {
                    return;
                }

                PointF before = new PointF(X, Y);
                x = value;
                UpdateDot(before);
            }
        }
        /// <summary>
        /// The X value between 0 and 1.
        /// </summary>
        public float Y
        {
            get
            {
                switch (style)
                {
                    case GridStyle.Full:
                    case GridStyle.Column:
                        return y;
                    default:
                        return 0.5f;
                }
            }
            set
            {
                if (value > 1 || value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (style == GridStyle.Row)
                {
                    return;
                }

                PointF before = new PointF(X, Y);
                y = value;
                UpdateDot(before);
            }
        }
        /// <summary>
        /// The text label shown on the top.
        /// </summary>
        public string LabelTop
        {
            get => labelTop.Text;
            set => labelTop.Text = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The text label shown on the bottom.
        /// </summary>
        public string LabelBottom
        {
            get => labelBottom.Text;
            set => labelBottom.Text = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The text label shown on the left.
        /// </summary>
        public string LabelLeft
        {
            get => labelLeft.Text;
            set => labelLeft.Text = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The text label shown on the right.
        /// </summary>
        public string LabelRight
        {
            get => labelRight.Text;
            set => labelRight.Text = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The style of this grid.
        /// </summary>
        public GridStyle Style
        {
            get => style;
            set
            {
                if (!Enum.IsDefined(typeof(GridStyle), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The Grid style is not valid! Expected Full, Row or Column.");
                }

                style = value;
                Recalculate();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when X and/or Y values are changed.
        /// </summary>
        public event GridValueChangedEventHandler ValuesChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="NativeGridPanel"/>.
        /// </summary>
        public NativeGridPanel() : base()
        {
        }

        #endregion

        #region Functions

        private void Recalculate() => Recalculate(position, width);
        private void UpdateDot(PointF before, bool trigger = true)
        {
            float posX = innerSize.Width * (style == GridStyle.Full || style == GridStyle.Row ? x : 0.5f);
            float posY = innerSize.Height * (style == GridStyle.Full || style == GridStyle.Column ? y : 0.5f);

            dot.Size = new SizeF(45, 45);
            dot.Position = new PointF(innerPosition.X + posX - (dot.Size.Width * 0.5f),
                                      innerPosition.Y + posY - (dot.Size.Height * 0.5f));

            if (trigger)
            {
                ValuesChanged?.Invoke(this, new GridValueChangedArgs(before, new PointF(X, Y)));
            }
        }
        /// <inheritdoc/>
        public override void Recalculate(PointF position, float width)
        {
            this.position = position;
            this.width = width;

            const float height = 270;
            const int offsetX = 20;
            const int offsetY = 20;

            base.Recalculate(position, width);
            Background.Size = new SizeF(width, height);

            switch (style)
            {
                case GridStyle.Full:
                    grid.Position = new PointF(position.X + (width * 0.5f) - 95, position.Y + (height * 0.5f) - 94);
                    grid.Size = new SizeF(192, 192);
                    break;
                case GridStyle.Row:
                    grid.Position = new PointF(position.X + (width * 0.5f) - 95, position.Y + (height * 0.5f) - 15);
                    grid.Size = new SizeF(192, 36);
                    break;
                case GridStyle.Column:
                    grid.Position = new PointF(position.X + (width * 0.5f) - 17, position.Y + (height * 0.5f) - 94);
                    grid.Size = new SizeF(36, 192);
                    break;
            }

            labelTop.Position = new PointF(position.X + (width * 0.5f), position.Y + 10);
            labelBottom.Position = new PointF(position.X + (width * 0.5f), position.Y + height - 34);
            labelLeft.Position = new PointF(position.X + (width * 0.5f) - 102, position.Y + (height * 0.5f) - (labelLeft.LineHeight * 0.5f));
            labelRight.Position = new PointF(position.X + (width * 0.5f) + 102, position.Y + (height * 0.5f) - (labelLeft.LineHeight * 0.5f));

            innerPosition = new PointF(grid.Position.X + offsetX, grid.Position.Y + offsetY);
            innerSize = new SizeF(grid.Size.Width - (offsetX * 2), grid.Size.Height - (offsetY * 2));

            UpdateDot(PointF.Empty, false);
        }
        /// <inheritdoc/>
        public override void Process()
        {
            float previousX = X;
            float previousY = Y;

            Background.Draw();
            switch (style)
            {
                case GridStyle.Full:
                    labelTop.Draw();
                    labelBottom.Draw();
                    labelLeft.Draw();
                    labelRight.Draw();
                    grid.Draw();
                    break;
                case GridStyle.Row:
                    labelLeft.Draw();
                    labelRight.Draw();
                    grid.DrawSpecific(new PointF(0, 0.4f), new PointF(1, 0.6f));
                    break;
                case GridStyle.Column:
                    labelTop.Draw();
                    labelBottom.Draw();
                    grid.DrawSpecific(new PointF(0.4f, 0), new PointF(0.6f, 1));
                    break;
            }
            dot.Draw();

            if (!Controls.IsUsingController)
            {
                if (GameScreen.IsCursorInArea(grid.Position, grid.Size) && Controls.IsPressed(Control.CursorAccept))
                {
                    PointF cursor = GameScreen.Cursor;
                    PointF pos = innerPosition;

                    PointF start = new PointF(cursor.X - pos.X, cursor.Y - pos.Y);
                    SizeF size = innerSize;

                    x = (start.X / size.Width).ToXRelative();
                    y = (start.Y / size.Height).ToYRelative();
                }
                else
                {
                    return;
                }
            }
            else
            {
#if ALTV
                Controls.DisableThisFrame(Control.LookUd);
                Controls.DisableThisFrame(Control.LookLr);
#else
                Controls.DisableThisFrame(Control.LookUpDown);
                Controls.DisableThisFrame(Control.LookLeftRight);
#endif
                Controls.EnableThisFrame(Control.ScriptRightAxisX);
                Controls.EnableThisFrame(Control.ScriptRightAxisY);

#if FIVEM
                float rX = Game.GetControlNormal(0, Control.ScriptRightAxisX);
                float rY = Game.GetControlNormal(0, Control.ScriptRightAxisY);
                float frameTime = Game.LastFrameTime;
#elif ALTV
                float rX = Alt.Natives.GetControlNormal(0, (int)Control.ScriptRightAxisX);
                float rY = Alt.Natives.GetControlNormal(0, (int)Control.ScriptRightAxisY);
                float frameTime = Alt.Natives.GetFrameTime();
#elif RAGEMP
                float rX = Invoker.Invoke<float>(0xEC3C9B8D5327B563, 0, (int)Control.ScriptRightAxisX);
                float rY = Invoker.Invoke<float>(0xEC3C9B8D5327B563, 0, (int)Control.ScriptRightAxisY);
                float frameTime = Invoker.Invoke<float>(Natives.GetFrameTime);
#elif RPH
                float rX = NativeFunction.CallByHash<float>(0xEC3C9B8D5327B563, 0, (int)Control.ScriptRightAxisX);
                float rY = NativeFunction.CallByHash<float>(0xEC3C9B8D5327B563, 0, (int)Control.ScriptRightAxisY);
                float frameTime = Game.FrameTime;
#elif SHVDN3 || SHVDNC
                float rX = Game.GetControlValueNormalized(Control.ScriptRightAxisX);
                float rY = Game.GetControlValueNormalized(Control.ScriptRightAxisY);
                float frameTime = Game.LastFrameTime;
#endif

                x += rX * frameTime;
                y += rY * frameTime;
            }

            // Make sure that the values are not under zero or over one
            if (x < 0)
            {
                x = 0;
            }
            else if (x > 1)
            {
                x = 1;
            }
            if (y < 0)
            {
                y = 0;
            }
            else if (y > 1)
            {
                y = 1;
            }

            if (previousX != x || previousY != y)
            {
                UpdateDot(new PointF(previousX, previousX));
            }
        }

        #endregion
    }
}

// Menus/NativeSeparatorItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// An item used to have a space between the items with text or no text.
    /// </summary>
    public class NativeSeparatorItem : NativeItem
    {
        #region Constructors

        /// <summary>
        /// Creates a new separator.
        /// </summary>
        public NativeSeparatorItem() : this(string.Empty)
        {
        }
        /// <summary>
        /// Creates a new separator with a specific title.
        /// </summary>
        /// <param name="title">The title of the item.</param>
        public NativeSeparatorItem(string title) : base(title, string.Empty, string.Empty)
        {
            this.title.Alignment = Alignment.Center;
        }

        #endregion

        #region Functions

        /// <inheritdoc/>
        public override void Recalculate(PointF pos, SizeF size, bool selected)
        {
            base.Recalculate(pos, size, selected);

            title.Position = new PointF(pos.X + (size.Width * 0.5f), title.Position.Y);
        }
        /// <inheritdoc/>
        public override void Draw()
        {
            if (UseCustomBackground)
            {
                background.Draw();
            }

            title.Draw();
        }

        #endregion
    }
}

// Menus/NativeListItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Base class for list items.
    /// </summary>
    public abstract class NativeListItem : NativeSlidableItem
    {
        #region Fields

        /// <summary>
        /// The text of the current item.
        /// </summary>
        protected internal ScaledText text = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new list item with a title and subtitle.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="subtitle">The subtitle of the Item.</param>
        public NativeListItem(string title, string subtitle) : base(title, subtitle)
        {
            text = new ScaledText(PointF.Empty, string.Empty, 0.35f);
        }

        #endregion
    }
}

// Menus/MenuMouseBehavior.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Defines the behavior of the mouse when a menu is open.
    /// </summary>
    public enum MenuMouseBehavior
    {
        /// <summary>
        /// The mouse will not be usable in the menu.
        /// </summary>
        Disabled,
        /// <summary>
        /// The menu can be used to click the items and navigate to them.
        /// </summary>
        Movement,
        /// <summary>
        /// The wheel can be used to navigate in the menu, click can be used to confirm and right click to return/exit.
        /// </summary>
        Scrolling
    }
}

// Menus/NativeColorData.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the Color Information shown on the Panel.
    /// </summary>
    public class NativeColorData
    {
        #region Fields

        private string name = string.Empty;
        internal readonly ScaledRectangle rectangle = new ScaledRectangle(PointF.Empty, SizeF.Empty);

        #endregion

        #region Properties

        /// <summary>
        /// The name of the color.
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The RGBA values of the color.
        /// </summary>
        public Color Color
        {
            get => rectangle.Color;
            set => rectangle.Color = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Color Panel information.
        /// </summary>
        /// <param name="name">The name of the color.</param>
        /// <param name="color">The RGBA values of the color.</param>
        public NativeColorData(string name, Color color)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            rectangle.Color = color;
        }

        #endregion
    }
}

// Menus/SubtitleBehavior.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The behavior of the <see cref="NativeMenu"/>'s subtitle.
    /// </summary>
    [Obsolete("Please use HeaderBehavior instead", true)]
    public enum SubtitleBehavior
    {
        /// <summary>
        /// The subtitle will always be shown.
        /// </summary>
        AlwaysShow = 0,
        /// <summary>
        /// The subtitle will always be shown, except when is empty.
        /// </summary>
        ShowIfRequired = 1,
        /// <summary>
        /// The subtitle will never be shown.
        /// </summary>
        AlwaysHide = 2
    }
}

// Menus/NativeItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Basic Rockstar-like item.
    /// </summary>
    public class NativeItem : IDrawable
    {
        #region Fields

        /// <summary>
        /// The title of the object.
        /// </summary>
        protected internal ScaledText title;
        /// <summary>
        /// The last known Item Position.
        /// </summary>
        protected internal PointF lastPosition = PointF.Empty;
        /// <summary>
        /// The last known Item Size.
        /// </summary>
        protected internal SizeF lastSize = SizeF.Empty;
        /// <summary>
        /// The last known Item Selection.
        /// </summary>
        protected internal bool lastSelected;
        /// <summary>
        /// The left badge of the Item.
        /// </summary>
        protected internal I2Dimensional badgeLeft;
        /// <summary>
        /// The left badge of the Item.
        /// </summary>
        protected internal I2Dimensional badgeRight;
        /// <summary>
        /// The alternate title of the menu.
        /// </summary>
        protected internal ScaledText altTitle;

        private bool enabled = true;
        private BadgeSet badgeSetLeft;
        private BadgeSet badgeSetRight;
        private ColorSet colors = new ColorSet();
        internal ScaledRectangle background = new ScaledRectangle(PointF.Empty, SizeF.Empty);
        private string description = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// If this item can be used or not.
        /// </summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                {
                    return;
                }
                enabled = value;
                EnabledChanged?.Invoke(this, EventArgs.Empty);
                UpdateColors();
            }
        }
        /// <summary>
        /// Object that contains data about this Item.
        /// </summary>
        public virtual object Tag { get; set; }
        /// <summary>
        /// The title of the item.
        /// </summary>
        public string Title
        {
            get => title.Text;
            set => title.Text = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The alternative title of the item shown on the right.
        /// </summary>
        public string AltTitle
        {
            get => altTitle.Text;
            set
            {
                altTitle.Text = value ?? throw new ArgumentNullException(nameof(value));
                Recalculate();
            }
        }
        /// <summary>
        /// The font of title item.
        /// </summary>
        public Font TitleFont
        {
            get => title.Font;
            set => title.Font = value;
        }
        /// <summary>
        /// The font of alternative title item shown on the right.
        /// </summary>
        public Font AltTitleFont
        {
            get => altTitle.Font;
            set => altTitle.Font = value;
        }
        /// <summary>
        /// The description of the item.
        /// </summary>
        public string Description
        {
            get => description;
            set => description = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The Left badge of the Item.
        /// </summary>
        public I2Dimensional LeftBadge
        {
            get => badgeLeft;
            set
            {
                if (badgeLeft == value)
                {
                    return;
                }

                badgeLeft = value;

                Recalculate();
                UpdateColors();
            }
        }
        /// <summary>
        /// The Left badge set of the Item.
        /// </summary>
        public BadgeSet LeftBadgeSet
        {
            get => badgeSetLeft;
            set
            {
                if (badgeSetLeft == value)
                {
                    return;
                }

                badgeSetLeft = value;

                if (badgeSetLeft == null)
                {
                    badgeLeft = null;
                }

                Recalculate();
                UpdateColors();
            }
        }
        /// <summary>
        /// The Right badge of the Item.
        /// </summary>
        public I2Dimensional RightBadge
        {
            get => badgeRight;
            set
            {
                if (badgeRight == value)
                {
                    return;
                }

                badgeRight = value;

                Recalculate();
                UpdateColors();
            }
        }
        /// <summary>
        /// The Right badge set of the Item.
        /// </summary>
        public BadgeSet RightBadgeSet
        {
            get => badgeSetRight;
            set
            {
                if (badgeSetRight == value)
                {
                    return;
                }

                badgeSetRight = value;

                if (badgeSetRight == null)
                {
                    badgeRight = null;
                }

                Recalculate();
                UpdateColors();
            }
        }
        /// <summary>
        /// The different colors that change dynamically when the item is used.
        /// </summary>
        public ColorSet Colors
        {
            get => colors;
            set
            {
                colors = value;
                UpdateColors();
            }
        }
        /// <summary>
        /// The Panel associated to this <see cref="NativeItem"/>.
        /// </summary>
        public NativePanel Panel { get; set; } = null;
        /// <summary>
        /// If a custom colored background should be used.
        /// </summary>
        public bool UseCustomBackground { get; set; }
        /// <summary>
        /// If this item is being hovered.
        /// </summary>
        public bool IsHovered => GameScreen.IsCursorInArea(background.Position, background.Size);

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the item is selected.
        /// </summary>
        public event SelectedEventHandler Selected;
        /// <summary>
        /// Event triggered when the item is activated.
        /// </summary>
        public event EventHandler Activated;
        /// <summary>
        /// Event triggered when the <see cref="Enabled"/> property is changed.
        /// </summary>
        public event EventHandler EnabledChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="NativeItem"/>.
        /// </summary>
        /// <param name="title">The title of the item.</param>
        public NativeItem(string title) : this(title, string.Empty, string.Empty)
        {
        }
        /// <summary>
        /// Creates a new <see cref="NativeItem"/>.
        /// </summary>
        /// <param name="title">The title of the item.</param>
        /// <param name="description">The description of the item.</param>
        public NativeItem(string title, string description) : this(title, description, string.Empty)
        {
        }
        /// <summary>
        /// Creates a new <see cref="NativeItem"/>.
        /// </summary>
        /// <param name="title">The title of the item.</param>
        /// <param name="description">The description of the item.</param>
        /// <param name="altTitle">The alternative title of the item, shown on the right.</param>
        public NativeItem(string title, string description, string altTitle)
        {
            this.title = new ScaledText(PointF.Empty, title, 0.345f);
            Description = description;
            this.altTitle = new ScaledText(PointF.Empty, altTitle, 0.345f);
        }

        #endregion

        #region Tools

        /// <summary>
        /// Triggers the Selected event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="SelectedEventArgs"/> with the index information.</param>
        protected internal void OnSelected(object sender, SelectedEventArgs e) => Selected?.Invoke(sender, e);
        /// <summary>
        /// Triggers the Activated event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        protected internal void OnActivated(object sender) => Activated?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Recalculates the item with the last known values.
        /// </summary>
        protected void Recalculate() => Recalculate(lastPosition, lastSize, lastSelected);

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the item positions and sizes with the specified values.
        /// </summary>
        /// <param name="pos">The position of the item.</param>
        /// <param name="size">The size of the item.</param>
        /// <param name="selected">If this item has been selected.</param>
        public virtual void Recalculate(PointF pos, SizeF size, bool selected)
        {
            lastPosition = pos;
            lastSize = size;
            lastSelected = selected;

            background.Position = pos;
            background.Size = size;

            if (badgeSetLeft != null)
            {
                if (!(badgeLeft is ScaledTexture))
                {
                    badgeLeft = new ScaledTexture(string.Empty, string.Empty);
                }
                ScaledTexture left = (ScaledTexture)badgeLeft;
                left.Dictionary = selected ? badgeSetLeft.HoveredDictionary : badgeSetLeft.NormalDictionary;
                left.Texture = selected ? badgeSetLeft.HoveredTexture : badgeSetLeft.NormalTexture;
            }
            if (badgeSetRight != null)
            {
                if (!(badgeRight is ScaledTexture))
                {
                    badgeRight = new ScaledTexture(string.Empty, string.Empty);
                }
                ScaledTexture right = (ScaledTexture)badgeRight;
                right.Dictionary = selected ? badgeSetRight.HoveredDictionary : badgeSetRight.NormalDictionary;
                right.Texture = selected ? badgeSetRight.HoveredTexture : badgeSetRight.NormalTexture;
            }

            if (badgeLeft != null)
            {
                badgeLeft.Position = new PointF(pos.X + 2, pos.Y - 3);
                badgeLeft.Size = new SizeF(45, 45);
            }
            if (badgeRight != null)
            {
                badgeRight.Position = new PointF(pos.X + size.Width - 47, pos.Y - 3);
                badgeRight.Size = new SizeF(45, 45);
            }

            title.Position = new PointF(pos.X + (badgeLeft == null ? 0 : 34) + 6, pos.Y + 3);
            altTitle.Position = new PointF(pos.X + size.Width - (badgeRight == null ? 0 : 34) - altTitle.Width - 6, pos.Y + 3);

            UpdateColors();
        }
        /// <summary>
        /// Draws the item.
        /// </summary>
        public virtual void Draw()
        {
            if (UseCustomBackground)
            {
                background.Draw();
            }

            title.Draw();
            altTitle.Draw();
            badgeLeft?.Draw();
            badgeRight?.Draw();
        }
        /// <summary>
        /// Updates the colors of the <see cref="Elements"/> from the <see cref="Colors"/> <see cref="ColorSet"/>.
        /// </summary>
        public virtual void UpdateColors()
        {
            if (!Enabled)
            {
                background.Color = Colors.BackgroundDisabled;
                title.Color = Colors.TitleDisabled;
                altTitle.Color = Colors.AltTitleDisabled;
                if (badgeLeft != null)
                {
                    badgeLeft.Color = Colors.BadgeLeftDisabled;
                }
                if (badgeRight != null)
                {
                    badgeRight.Color = Colors.BadgeRightDisabled;
                }
            }
            else if (lastSelected && !(this is NativeSeparatorItem))
            {
                background.Color = Colors.BackgroundHovered;
                title.Color = Colors.TitleHovered;
                altTitle.Color = Colors.AltTitleHovered;
                if (badgeLeft != null)
                {
                    badgeLeft.Color = Colors.BadgeLeftHovered;
                }
                if (badgeRight != null)
                {
                    badgeRight.Color = Colors.BadgeRightHovered;
                }
            }
            else
            {
                background.Color = Colors.BackgroundNormal;
                title.Color = Colors.TitleNormal;
                altTitle.Color = Colors.AltTitleNormal;
                if (badgeLeft != null)
                {
                    badgeLeft.Color = Colors.BadgeLeftNormal;
                }
                if (badgeRight != null)
                {
                    badgeRight.Color = Colors.BadgeRightNormal;
                }
            }
        }

        #endregion
    }
}

// Menus/GridStyle.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The style of the Grid Panel.
    /// </summary>
    public enum GridStyle
    {
        /// <summary>
        /// The full grid with X and Y values.
        /// </summary>
        Full = 0,
        /// <summary>
        /// A single row on the center with the X value only.
        /// </summary>
        Row = 1,
        /// <summary>
        /// A single column on the center with the Y value only.
        /// </summary>
        Column = 2,
    }
}

// Menus/MenuModifiedEventHandler.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the method that is called when the items on a menu are changed (added or removed).
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="MenuModifiedEventArgs"/> with the menu operation.</param>
    public delegate void MenuModifiedEventHandler(object sender, MenuModifiedEventArgs e);
}

// Menus/ColorTitleStyle.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The Style of title for the Color Panel.
    /// </summary>
    public enum ColorTitleStyle
    {
        /// <summary>
        /// Does not shows any Title.
        /// The count will still be shown if <see cref="NativeColorPanel.ShowCount"/> is set to <see langword="true"/>.
        /// </summary>
        None = -1,
        /// <summary>
        /// Shows a Simple Title for all of the Colors.
        /// </summary>
        Simple = 0,
        /// <summary>
        /// Shows the Color Name as the Title.
        /// </summary>
        ColorName = 1
    }
}

// Menus/HeaderBehavior.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The behavior of the <see cref="NativeMenu"/>'s header.
    /// </summary>
    public enum HeaderBehavior
    {
        /// <summary>
        /// The header will always be shown.
        /// </summary>
        AlwaysShow = 0,
        /// <summary>
        /// The header will always be shown, except when is empty.
        /// </summary>
        ShowIfRequired = 1,
        /// <summary>
        /// The header will never be shown.
        /// </summary>
        AlwaysHide = 2
    }
}

// Menus/CountVisibility.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The visibility setting for the Item Count of the Menu.
    /// </summary>
    public enum CountVisibility
    {
        /// <summary>
        /// The Item Count is never shown.
        /// </summary>
        Never = -1,
        /// <summary>
        /// The Item Count is shown when is not possible to show all of the items in the screen.
        /// </summary>
        Auto = 0,
        /// <summary>
        /// The Item Count is always shown.
        /// </summary>
        Always = 1
    }
}

// Menus/MenuModifiedEventArgs.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the different
    /// </summary>
    public class MenuModifiedEventArgs
    {
        #region Properties

        /// <summary>
        /// The item that was modified.
        /// </summary>
        public NativeItem Item { get; }
        /// <summary>
        /// The operation that was performed in the item.
        /// </summary>
        public ItemOperation Operation { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="MenuModifiedEventArgs"/>.
        /// </summary>
        /// <param name="item">The item that was modified.</param>
        /// <param name="operation">The operation that was performed in the item.</param>
        public MenuModifiedEventArgs(NativeItem item, ItemOperation operation)
        {
            Item = item;
            Operation = operation;
        }

        #endregion
    }
}

// Menus/NativeMenu.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Menu that looks like the ones used by Rockstar.
    /// </summary>
    public class NativeMenu : IContainer<NativeItem>, IEnumerable<NativeItem>
    {
        #region Constants

        /// <summary>
        /// The height of the menu name background.
        /// </summary>
        internal const float nameHeight = 38;
        /// <summary>
        /// The height of one of the items in the screen.
        /// </summary>
        internal const float itemHeight = 37.4f;
        /// <summary>
        /// The height difference between the description and the end of the items.
        /// </summary>
        internal const float heightDiffDescImg = 4;
        /// <summary>
        /// The height difference between the background and text of the description.
        /// </summary>
        internal const float heightDiffDescTxt = 3;
        /// <summary>
        /// The X position of the description text.
        /// </summary>
        internal const float posXDescTxt = 6;
        /// <summary>
        /// The offset to the X value of the item title.
        /// </summary>
        internal const float itemOffsetX = 6;
        /// <summary>
        /// The offset to the Y value of the item title.
        /// </summary>
        internal const float itemOffsetY = 3;

        #endregion

        #region Fields

        /// <summary>
        /// The default <see cref="Sound"/> played when the current <see cref="NativeItem"/> is changed or activated.
        /// </summary>
        public static readonly Sound DefaultActivatedSound = new Sound("HUD_FRONTEND_DEFAULT_SOUNDSET", "SELECT");
        /// <summary>
        /// The default <see cref="Sound"/> played when the menu is closed.
        /// </summary>
        public static readonly Sound DefaultCloseSound = new Sound("HUD_FRONTEND_DEFAULT_SOUNDSET", "BACK");
        /// <summary>
        /// The default <see cref="Sound"/> played when the user navigates Up and Down.
        /// </summary>
        public static readonly Sound DefaultUpDownSound = new Sound("HUD_FRONTEND_DEFAULT_SOUNDSET", "NAV_UP_DOWN");
        /// <summary>
        /// The default <see cref="Sound"/> played when the user navigates Left and Right on a <see cref="NativeSlidableItem"/>.
        /// </summary>
        public static readonly Sound DefaultLeftRightSound = new Sound("HUD_FRONTEND_DEFAULT_SOUNDSET", "NAV_LEFT_RIGHT");
        /// <summary>
        /// The default <see cref="Sound"/> played when the user activates a <see cref="NativeItem"/> that is disabled.
        /// </summary>
        public static readonly Sound DefaultDisabledSound = new Sound("HUD_FRONTEND_DEFAULT_SOUNDSET", "ERROR");

        internal static readonly Color colorWhiteSmoke = Color.FromArgb(255, 245, 245, 245);
        /// <summary>
        /// The search area size for the cursor rotation.
        /// </summary>
        internal static readonly SizeF searchAreaSize = new SizeF(30, 1080);
        /// <summary>
        /// Controls required for the camera to work.
        /// </summary>
        internal static readonly List<Control> controlsCamera = new List<Control>
        {
#if ALTV
            Control.LookUd,
            Control.LookLr,
#else
            Control.LookUpDown,
            Control.LookLeftRight,
#endif
        };
        /// <summary>
        /// The controls required in a gamepad.
        /// </summary>
        internal static readonly List<Control> controlsGamepad = new List<Control>
        {
            Control.Aim,
            Control.Attack
        };

        /// <summary>
        /// A list of GTA V Controls that are not required by the menu with both a gamepad and mouse + keyboard.
        /// </summary>
        private static readonly Control[] controls;
        /// <summary>
        /// If the menu has just been opened and we should check the controls.
        /// </summary>
        private bool justOpenedControlChecks = false;
        /// <summary>
        /// The list of visible items on the screen.
        /// </summary>
        private List<NativeItem> visibleItems = new List<NativeItem>();
        /// <summary>
        /// The name of the menu, without any changes.
        /// </summary>
        private string name = string.Empty;
        /// <summary>
        /// If the menu is visible or not.
        /// </summary>
        private bool visible = false;
        /// <summary>
        /// If the menu has just been closed.
        /// </summary>
        private bool justClosed = false;
        /// <summary>
        /// If this menu should be aware of the Safe Zone when doing calculations.
        /// </summary>
        private bool safeZoneAware = true;
        /// <summary>
        /// The index of the selected item in the menu.
        /// </summary>
        private int index = -1;
        /// <summary>
        /// The width of the menu itself.
        /// </summary>
        private float width = 433;
        /// <summary>
        /// The alignment of the menu.
        /// </summary>
        private Alignment alignment = Alignment.Left;
        /// <summary>
        /// The offset of the menu.
        /// </summary>
        private PointF offset = PointF.Empty;
        /// <summary>
        /// The banner of the menu.
        /// </summary>
        private I2Dimensional bannerImage = null;
        /// <summary>
        /// The background of the drawable text.
        /// </summary>
        private readonly ScaledRectangle nameImage = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(255, 0, 0, 0)
        };
        /// <summary>
        /// The text of the name.
        /// </summary>
        private readonly ScaledText nameText = new ScaledText(PointF.Empty, string.Empty, 0.345f, Font.ChaletLondon)
        {
            Color = colorWhiteSmoke
        };
        /// <summary>
        /// The text that shows the current total and index of items.
        /// </summary>
        private readonly ScaledText countText = new ScaledText(PointF.Empty, string.Empty, 0.345f, Font.ChaletLondon)
        {
            Color = colorWhiteSmoke
        };
        /// <summary>
        /// The image on the background of the menu.
        /// </summary>
        private readonly ScaledTexture backgroundImage = new ScaledTexture("commonmenu", "gradient_bgd");
        /// <summary>
        /// The rectangle that shows the currently selected item.
        /// </summary>
        private readonly ScaledTexture selectedRect = new ScaledTexture("commonmenu", "gradient_nav");
        private readonly ScaledTexture hoveredRect = new ScaledTexture("commonmenu", "gradient_nav")
        {
            Color = Color.FromArgb(20, 255, 255, 255)
        };
        /// <summary>
        /// The rectangle with the description text.
        /// </summary>
        private readonly ScaledTexture descriptionRect = new ScaledTexture("commonmenu", "gradient_bgd");
        /// <summary>
        /// The text with the description text.
        /// </summary>
        private readonly ScaledText descriptionText = new ScaledText(PointF.Empty, string.Empty, 0.351f);
        /// <summary>
        /// The maximum allowed number of items in the menu at once.
        /// </summary>
        private int maxItems = 10;
        /// <summary>
        /// The first item in the menu.
        /// </summary>
        private int firstItem = 0;
        /// <summary>
        /// The search area on the right side of the screen.
        /// </summary>
        private PointF searchAreaRight = PointF.Empty;
        /// <summary>
        /// The time since the player has been pressing the Up button.
        /// </summary>
        private long heldSince = -1;
        private HeaderBehavior nameBehavior = HeaderBehavior.AlwaysShow;
        private string description = string.Empty;
        private string noItemsText = "There are no items available";
        private bool keepNameCasing;

        #endregion

        #region Properties

        private bool ShouldDrawNameBackground => nameBehavior == HeaderBehavior.AlwaysShow || (nameBehavior == HeaderBehavior.ShowIfRequired && (ShouldDrawCount || !string.IsNullOrWhiteSpace(name)));
        private bool ShouldDrawCount => ItemCount == CountVisibility.Always || (ItemCount == CountVisibility.Auto && Items.Count > MaxItems);

        /// <summary>
        /// If the menu is visible on the screen.
        /// </summary>
        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value)
                {
                    return;
                }

                if (value)
                {
                    CancelEventArgs args = new CancelEventArgs();
                    Opening?.Invoke(this, args);
                    if (args.Cancel)
                    {
                        return;
                    }

                    if (ResetCursorWhenOpened)
                    {
                        ResetCursor();
                    }

                    justOpenedControlChecks = true;
                    visible = true;

                    SoundOpened?.PlayFrontend();

                    Shown?.Invoke(this, EventArgs.Empty);
                    TriggerSelectedItem();
                }
                else
                {
                    CancelEventArgs args = new CancelEventArgs();
                    Closing?.Invoke(this, args);
                    if (args.Cancel)
                    {
                        return;
                    }

                    visible = false;
                    justClosed = true;
                    Closed?.Invoke(this, EventArgs.Empty);
                    SoundClose?.PlayFrontend();
                }
            }
        }
        /// <summary>
        /// The text shown on top of the banner.
        /// </summary>
        public ScaledText BannerText { get; set; }
        /// <summary>
        /// The text shown on top of the banner.
        /// </summary>
        [Obsolete("Please use BannerText instead.", true)]
        public ScaledText Title
        {
            get => BannerText;
            set => BannerText = value;
        }
        /// <summary>
        /// The font of the text shown on top of the banner.
        /// </summary>
        [Obsolete("Please use BannerText.Font instead.", true)]
        public Font TitleFont
        {
            get => BannerText.Font;
            set => BannerText.Font = value;
        }
        /// <summary>
        /// The font of name text.
        /// </summary>
        [Obsolete("Please use NameFont instead.", true)]
        public Font SubtitleFont
        {
            get => nameText.Font;
            set => nameText.Font = value;
        }
        /// <summary>
        /// The font of name text.
        /// </summary>
        public Font NameFont
        {
            get => nameText.Font;
            set => nameText.Font = value;
        }
        /// <summary>
        /// The font of description text.
        /// </summary>
        public Font DescriptionFont
        {
            get => descriptionText.Font;
            set => descriptionText.Font = value;
        }
        /// <summary>
        /// The font of item count text.
        /// </summary>
        public Font ItemCountFont
        {
            get => countText.Font;
            set => countText.Font = value;
        }
        /// <summary>
        /// The banner shown at the top of the menu.
        /// </summary>
        public I2Dimensional Banner
        {
            get => bannerImage;
            set
            {
                bannerImage = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The offset of the menu position.
        /// </summary>
        public PointF Offset
        {
            get => offset;
            set
            {
                offset = value;
                Recalculate();
            }
        }
        /// <summary>
        /// Returns the currently selected item.
        /// </summary>
        public NativeItem SelectedItem
        {
            get
            {
                // If there are no items or is over the maximum, return null
                int currentIndex = SelectedIndex;
                if (Items.Count == 0 || currentIndex >= Items.Count || currentIndex == -1)
                {
                    return null;
                }
                // Otherwise, return the correct item from the list
                return Items[currentIndex];
            }
            set
            {
                // If the item is not part of the menu, raise an exception
                if (!Items.Contains(value))
                {
                    throw new InvalidOperationException("Item is not part of the Menu.");
                }
                // Otherwise, set the correct index
                SelectedIndex = Items.IndexOf(value);
            }
        }
        /// <summary>
        /// The current index of the menu.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                // If there are no items or is over the maximum, return -1
                if (Items.Count == 0 || index >= Items.Count)
                {
                    return -1;
                }
                // Otherwise, return the real index
                return index;
            }
            set
            {
                // If the list of items is empty, don't allow the user to set the index
                if (Items == null || Items.Count == 0)
                {
                    throw new InvalidOperationException("There are no items in this menu.");
                }
                // If the value is over or equal than the number of items, raise an exception
                else if (value >= Items.Count)
                {
                    throw new InvalidOperationException($"The index is over {Items.Count - 1}.");
                }
                // If the value is under zero, raise an exception
                else if (value < 0)
                {
                    throw new InvalidOperationException($"The index is under zero.");
                }

                // Calculate the bounds of the menu
                int lower = firstItem;
                int upper = firstItem + maxItems;

                // Time to set the first item based on the total number of items
                // If the item is between the allowed values, do nothing because we are on the correct first item
                if (value >= lower && value < upper - 1)
                {
                }
                // If the upper bound + 1 equals the new index, increase it by one
                else if (upper == value)
                {
                    firstItem += 1;
                }
                // If the first item minus one equals the value, decrease it by one
                else if (lower - 1 == value)
                {
                    firstItem -= 1;
                }
                // Otherwise, set it somewhere
                else
                {
                    // If the value is under the max items, set it to zero
                    if (value < maxItems)
                    {
                        firstItem = 0;
                    }
                    // Otherwise, set it at the bottom
                    else
                    {
                        firstItem = value - maxItems + 1;
                    }
                }

                // Save the index
                index = value;

                // And update the items
                UpdateItemList();
                UpdateItems();

                // If the menu is visible, play the up and down sound
                if (Visible)
                {
                    SoundUpDown?.PlayFrontend();
                }

                // If an item was selected
                if (SelectedItem != null)
                {
                    // And trigger it
                    TriggerSelectedItem();
                }
            }
        }
        /// <summary>
        /// The width of the menu.
        /// </summary>
        public float Width
        {
            get => width;
            set
            {
                width = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The alignment of the menu.
        /// </summary>
        public Alignment Alignment
        {
            get => alignment;
            set
            {
                if (alignment == value)
                {
                    return;
                }

                if (!Enum.IsDefined(typeof(Alignment), value) || value == Alignment.Center)
                {
                    throw new ArgumentException("The Menu can only be aligned to the Left and Right.", nameof(value));
                }

                alignment = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The name of this menu.
        /// </summary>
        /// <remarks>
        /// This is shown under the menu banner, and by the menu items when added as a submenu.
        /// </remarks>
        public string Name
        {
            get => name;
            set
            {
                name = value ?? throw new ArgumentNullException(nameof(value));
                nameText.Text = KeepNameCasing ? value : value.ToUpperInvariant();
            }
        }
        /// <summary>
        /// Whether the name of the menu should keep its casing or not.
        /// </summary>
        public bool KeepNameCasing
        {
            get => keepNameCasing;
            set
            {
                keepNameCasing = value;
                nameText.Text = KeepNameCasing ? name : name.ToUpperInvariant();
            }
        }
        /// <summary>
        /// The name of the menu.
        /// </summary>
        [Obsolete("Please use Name instead.", true)]
        public string Subtitle
        {
            get => Name;
            set => Name = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The description used when this menu is used as a submenu.
        /// </summary>
        public string Description
        {
            get => description;
            set => description = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// If the mouse should be used for navigating the menu.
        /// </summary>
        [Obsolete("This parameter is ambiguous, please use the MouseBehavior property instead instead.", true)]
        public bool UseMouse
        {
            get => MouseBehavior == MenuMouseBehavior.Movement;
            set => MouseBehavior = value ? MenuMouseBehavior.Movement : MenuMouseBehavior.Disabled;
        }
        /// <summary>
        /// The behavior of the mouse when the menu is open.
        /// </summary>
        public MenuMouseBehavior MouseBehavior { get; set; } = MenuMouseBehavior.Movement;
        /// <summary>
        /// If the menu should be closed when the user clicks out of bounds (aka anywhere else other than the items).
        /// </summary>
        public bool CloseOnInvalidClick { get; set; } = true;
        /// <summary>
        /// If the camera should be rotated when the cursor is on the left and right corners of the screen.
        /// </summary>
        public bool RotateCamera { get; set; } = false;
        /// <summary>
        /// The items that this menu contain.
        /// </summary>
        public List<NativeItem> Items { get; } = new List<NativeItem>();

        /// <summary>
        /// Text shown when there are no items in the menu.
        /// </summary>
        public string NoItemsText
        {
            get => noItemsText;
            set => noItemsText = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// If the cursor should be reset when the menu is opened.
        /// </summary>
        public bool ResetCursorWhenOpened { get; set; } = true;
        /// <summary>
        /// The maximum allowed number of items in the menu at once.
        /// </summary>
        public int MaxItems
        {
            get => maxItems;
            set
            {
                if (value == maxItems)
                {
                    return;
                }
                if (value < 1)
                {
                    throw new InvalidOperationException("The maximum numbers on the screen can't be under 1.");
                }
                maxItems = value;
                UpdateItemList();
            }
        }
        /// <summary>
        /// If this menu should be aware of the Safe Zone when doing calculations.
        /// </summary>
        public bool SafeZoneAware
        {
            get => safeZoneAware;
            set
            {
                safeZoneAware = value;
                Recalculate();
            }
        }
        /// <summary>
        /// If the count of items should be shown on the right of the name.
        /// </summary>
        public CountVisibility ItemCount { get; set; }
        /// <summary>
        /// The instructional buttons shown in the bottom right.
        /// </summary>
        public InstructionalButtons Buttons { get; } = new InstructionalButtons(new InstructionalButton("Select", (Control)176 /*PhoneSelect*/), new InstructionalButton("Back", (Control)177 /*PhoneCancel*/))
        {
            Visible = true
        };
        /// <summary>
        /// The parent menu of this menu.
        /// </summary>
        public NativeMenu Parent { get; set; } = null;
        /// <summary>
        /// If the menu accepts user input for navigation.
        /// </summary>
        public bool AcceptsInput { get; set; } = true;
        /// <summary>
        /// If the conflictive controls should be disabled while the menu is open.
        /// </summary>
        public bool DisableControls { get; set; } = true;
        /// <summary>
        /// The time between item changes when holding left, right, up or down.
        /// </summary>
        /// <remarks>
        /// This property can be set to zero to completely disable it.
        /// </remarks>
        public int HeldTime { get; set; } = 166;
        /// <summary>
        /// The controls that are required for some menu operations.
        /// </summary>
        /// <remarks>
        /// Add controls to this list when you want to detect them as pressed while the menu is open.
        /// </remarks>
        public List<Control> RequiredControls { get; } = new List<Control>();
        /// <summary>
        /// The behavior of the black bar showing the name.
        /// </summary>
        [Obsolete("Please use NameBehavior instead.", true)]
        public SubtitleBehavior SubtitleBehavior
        {
            get => (SubtitleBehavior)HeaderBehavior;
            set => HeaderBehavior = (HeaderBehavior)value;
        }
        /// <summary>
        /// The behavior of the black bar showing the name.
        /// </summary>
        public HeaderBehavior HeaderBehavior
        {
            get => nameBehavior;
            set
            {
                if (nameBehavior == value)
                {
                    return;
                }

                nameBehavior = value;
                Recalculate();
            }
        }
        /// <summary>
        /// The <see cref="Sound"/> played when the menu is opened.
        /// </summary>
        public Sound SoundOpened { get; set; } = DefaultActivatedSound;
        /// <summary>
        /// The <see cref="Sound"/> played when a <see cref="NativeItem"/> is activated.
        /// </summary>
        public Sound SoundActivated { get; set; } = DefaultActivatedSound;
        /// <summary>
        /// The <see cref="Sound"/> played when the menu is closed.
        /// </summary>
        public Sound SoundClose { get; set; } = DefaultCloseSound;
        /// <summary>
        /// The <see cref="Sound"/> played when the user navigates Up or Down the menu.
        /// </summary>
        public Sound SoundUpDown { get; set; } = DefaultUpDownSound;
        /// <summary>
        /// The <see cref="Sound"/> played when the user navigates Left and Right on a <see cref="NativeSlidableItem"/>.
        /// </summary>
        public Sound SoundLeftRight { get; set; } = DefaultLeftRightSound;
        /// <summary>
        /// The <see cref="Sound"/> played when the user activates a <see cref="NativeItem"/> that is disabled.
        /// </summary>
        public Sound SoundDisabled { get; set; } = DefaultDisabledSound;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the menu is being opened.
        /// </summary>
        public event CancelEventHandler Opening;
        /// <summary>
        /// Event triggered when the menu is opened and shown to the user.
        /// </summary>
        public event EventHandler Shown;
        /// <summary>
        /// Event triggered when the menu starts closing.
        /// </summary>
        public event CancelEventHandler Closing;
        /// <summary>
        /// Event triggered when the menu finishes closing.
        /// </summary>
        public event EventHandler Closed;
        /// <summary>
        /// Event triggered when the index has been changed.
        /// </summary>
        public event SelectedEventHandler SelectedIndexChanged;
        /// <summary>
        /// Event triggered when an item in the menu is activated.
        /// </summary>
        public event ItemActivatedEventHandler ItemActivated;
        /// <summary>
        /// Event triggered when the contents of the menu are changed.
        /// </summary>
        public event MenuModifiedEventHandler MenuModified;

        #endregion

        #region Constructors

        static NativeMenu()
        {
            // The controls required by the menu with both a gamepad and mouse + keyboard
            HashSet<Control> controlsRequired = new HashSet<Control>
            {
                // Menu Controls
                Control.FrontendAccept,
                Control.FrontendAxisX,
                Control.FrontendAxisY,
                Control.FrontendDown,
                Control.FrontendUp,
                Control.FrontendLeft,
                Control.FrontendRight,
                Control.FrontendCancel,
                Control.FrontendSelect,
                Control.CursorScrollDown,
                Control.CursorScrollUp,
                Control.CursorX,
                Control.CursorY,
#if ALTV
                Control.MoveUd,
                Control.MoveLr,
#else
                Control.MoveUpDown,
                Control.MoveLeftRight,
#endif
                // Camera
                Control.LookBehind,
#if ALTV
                Control.VehLookBehind,
#else
                Control.VehicleLookBehind,
#endif
                // Player
                Control.Sprint,
                Control.Jump,
                Control.Enter,
                Control.SpecialAbility,
                Control.SpecialAbilityPC,
                Control.SpecialAbilitySecondary,
#if ALTV
                Control.VehSpecialAbilityFranklin,
                // Driving
                Control.VehExit,
                Control.VehAccelerate,
                Control.VehBrake,
                Control.VehMoveLr,
                Control.VehHandbrake,
                Control.VehHorn,
                // Bikes
                Control.VehPushbikePedal,
                Control.VehPushbikeSprint,
                Control.VehPushbikeFrontBrake,
                Control.VehPushbikeRearBrake,
                // Flying
                Control.VehFlyThrottleUp,
                Control.VehFlyThrottleDown,
                Control.VehFlyYawLeft,
                Control.VehFlyYawRight,
                Control.VehFlyRollLR,
                Control.VehFlyRollLeftOnly,
                Control.VehFlyRollRightOnly,
                Control.VehFlyPitchUD,
                Control.VehFlyPitchUpOnly,
                Control.VehFlyPitchDownOnly,
#else
                Control.VehicleSpecialAbilityFranklin,
                // Driving
                Control.VehicleExit,
                Control.VehicleAccelerate,
                Control.VehicleBrake,
                Control.VehicleMoveLeftRight,
                Control.VehicleHandbrake,
                Control.VehicleHorn,
                // Bikes
                Control.VehiclePushbikePedal,
                Control.VehiclePushbikeSprint,
                Control.VehiclePushbikeFrontBrake,
                Control.VehiclePushbikeRearBrake,
                // Flying
                Control.VehicleFlyThrottleUp,
                Control.VehicleFlyThrottleDown,
                Control.VehicleFlyYawLeft,
                Control.VehicleFlyYawRight,
                Control.VehicleFlyRollLeftRight,
                Control.VehicleFlyRollLeftOnly,
                Control.VehicleFlyRollRightOnly,
                Control.VehicleFlyPitchUpDown,
                Control.VehicleFlyPitchUpOnly,
                Control.VehicleFlyPitchDownOnly,
#endif
#if RPH
                Control.ScriptedFlyUpDown,
                Control.ScriptedFlyLeftRight,
#elif ALTV
                Control.ScriptedFlyUd,
                Control.ScriptedFlyLr,
#else
                Control.FlyUpDown,
                Control.FlyLeftRight,
#endif
                // Rockstar Editor
                Control.SaveReplayClip,
                Control.ReplayStartStopRecording,
                Control.ReplayStartStopRecordingSecondary,
                Control.ReplayRecord,
                Control.ReplaySave,
            };

            controls = ((Control[])Enum.GetValues(typeof(Control))).Except(controlsRequired).ToArray();
        }

        /// <summary>
        /// Creates a new menu with the specified title.
        /// </summary>
        /// <param name="title">The title of the menu.</param>
        public NativeMenu(string title) : this(title, string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Creates a new menu with the specified banner text and name.
        /// </summary>
        /// <param name="bannerText">The title of the menu.</param>
        /// <param name="name">The name of this menu.</param>
        public NativeMenu(string bannerText, string name) : this(bannerText, name, string.Empty)
        {
        }

        /// <summary>
        /// Creates a new menu with the specified banner text, name and description.
        /// </summary>
        /// <param name="bannerText">The title of the menu.</param>
        /// <param name="name">The name of this menu.</param>
        /// <param name="description">The description used for submenus.</param>
        public NativeMenu(string bannerText, string name, string description) : this(bannerText, name, description, new ScaledTexture(PointF.Empty, new SizeF(0, 108), "commonmenu", "interaction_bgd"))
        {
        }

        /// <summary>
        /// Creates a new menu with the specified banner text, name, description and banner.
        /// </summary>
        /// <param name="bannerText">The title of the menu.</param>
        /// <param name="name">The name of this menu.</param>
        /// <param name="description">The description used for submenus.</param>
        /// <param name="banner">The drawable to use as the banner.</param>
        public NativeMenu(string bannerText, string name, string description, I2Dimensional banner)
        {
            this.name = name;
            Description = description;
            bannerImage = banner;
            BannerText = new ScaledText(PointF.Empty, bannerText, 1.02f, Font.HouseScript)
            {
                Alignment = Alignment.Center
            };
            nameText.Text = name.ToUpperInvariant();
            Recalculate();
        }

        #endregion

        #region Tools

        /// <summary>
        /// Updates the list of visible items on the screen.
        /// </summary>
        private void UpdateItemList()
        {
            // Create a new list for the items
            List<NativeItem> list = new List<NativeItem>();

            // Iterate over the number of items while staying under the maximum
            for (int i = 0; i < MaxItems; i++)
            {
                // Calculate the start of our items
                int start = firstItem + i;

                // If the number of items is over the ones in the list, something went wrong
                // TODO: Decide what to do in this case (exception? silently ignore?)
                if (start >= Items.Count)
                {
                    break;
                }

                // Otherwise, return it as part of the iterator or add it to the list
                list.Add(Items[start]);
            }

            // Finally, replace the list of items
            visibleItems = list;
        }
        /// <summary>
        /// Triggers the Selected event for the current item.
        /// </summary>
        private void TriggerSelectedItem()
        {
            // Get the currently selected item
            NativeItem item = SelectedItem;

            // If is null or the menu is closed, return
            if (item == null || !Visible)
            {
                return;
            }

            // Update the panel
            RecalculatePanel();
            // And trigger the selected event for this menu
            SelectedEventArgs args = new SelectedEventArgs(index, index - firstItem);
            SelectedItem.OnSelected(this, args);
            SelectedIndexChanged?.Invoke(this, args);
        }
        /// <summary>
        /// Recalculates the Position and Size of the Panel of the Selected Item.
        /// </summary>
        private void RecalculatePanel()
        {
            if (SelectedItem?.Panel == null)
            {
                return;
            }

            const int separation = 10;

            PointF position = new PointF(descriptionRect.Position.X, descriptionRect.Position.Y + descriptionRect.Size.Height + separation);
            SelectedItem.Panel.Recalculate(position, Width);
        }
        /// <summary>
        /// Resets the current position of the cursor.
        /// </summary>
        public void ResetCursor()
        {
            const float extraX = 35;
            const float extraY = 325;

            PointF pos = PointF.Empty;
            if (SafeZoneAware)
            {
                SafeZone.SetAlignment(Alignment, GFXAlignment.Top);
                float x = 0;
                switch (Alignment)
                {
                    case Alignment.Left:
                        x = Offset.X + Width + extraX;
                        break;
                    case Alignment.Right:
                        x = Offset.X - Width - extraX;
                        break;
                }
                pos = SafeZone.GetSafePosition(x, Offset.Y + extraY).ToRelative();
                SafeZone.ResetAlignment();
            }
            else
            {
                float x = 0;
                switch (Alignment)
                {
                    case Alignment.Left:
                        x = Offset.X + Width + extraX;
                        break;
                    case Alignment.Right:
                        x = 1f.ToXScaled() - Offset.X - Width - extraX;
                        break;
                }
                pos = new PointF(x, Offset.Y + extraY).ToRelative();
            }
            // And set the position of the cursor
#if FIVEM
            API.SetCursorLocation(pos.X, pos.Y);
#elif RAGEMP
            Invoker.Invoke(Natives.SetCursorLocation, pos.X, pos.Y);
#elif RPH
            NativeFunction.CallByHash<int>(0xFC695459D4D0E219, pos.X, pos.Y);
#elif ALTV
            Alt.Natives.SetCursorPosition(pos.X, pos.Y);
#elif SHVDN3 || SHVDNC
            Function.Call(Hash.SET_CURSOR_POSITION, pos.X, pos.Y);
#endif
        }
        /// <summary>
        /// Updates the positions of the items.
        /// </summary>
        private void UpdateItems()
        {
            // Store the current values of X and Y
            PointF pos;
            if (SafeZoneAware)
            {
                SafeZone.SetAlignment(Alignment, GFXAlignment.Top);
                float x = 0;
                switch (Alignment)
                {
                    case Alignment.Left:
                        x = Offset.X;
                        break;
                    case Alignment.Right:
                        x = Offset.X - Width;
                        break;
                }
                pos = SafeZone.GetSafePosition(x, Offset.Y);
                SafeZone.ResetAlignment();
            }
            else
            {
                float x = 0;
                switch (Alignment)
                {
                    case Alignment.Left:
                        x = Offset.X;
                        break;
                    case Alignment.Right:
                        x = 1f.ToXScaled() - Width - Offset.X;
                        break;
                }
                pos = new PointF(x, Offset.Y);
            }

            // Add the heights of the banner and title (if there are any)
            if (bannerImage != null)
            {
                pos.Y += bannerImage.Size.Height;
            }
            if (ShouldDrawNameBackground || ShouldDrawCount)
            {
                countText.Text = $"{SelectedIndex + 1} / {Items.Count}";
                countText.Position = new PointF(pos.X + width - countText.Width - 6, pos.Y + 4.2f);
                pos.Y += nameImage.Size.Height;
            }

            // Set the position and size of the background image
            backgroundImage.literalPosition = new PointF(pos.X, pos.Y);
            backgroundImage.literalSize = new SizeF(width, itemHeight * visibleItems.Count);
            backgroundImage.Recalculate();
            // Set the position of the rectangle that marks the current item
            selectedRect.Position = new PointF(pos.X, pos.Y + ((index - firstItem) * itemHeight));
            // And then do the description background and text
            float description = pos.Y + ((Items.Count > maxItems ? maxItems : Items.Count) * itemHeight) + heightDiffDescImg;
            descriptionRect.Position = new PointF(pos.X, description);
            descriptionText.Position = new PointF(pos.X + posXDescTxt, description + heightDiffDescTxt);
            UpdateDescription();

            // Save the size of the items
            SizeF size = new SizeF(width, itemHeight);
            // And start recalculating them
            int i = 0;
            foreach (NativeItem item in visibleItems)
            {
                // Tell the item to recalculate the position
                item.Recalculate(new PointF(pos.X, pos.Y), size, item == SelectedItem);
                // And increase the index of the item and Y position
                i++;
                pos.Y += itemHeight;
            }

            // Finally, recalculate the panel of the selected item
            RecalculatePanel();
        }
        /// <summary>
        /// Updates the size and text of the description.
        /// </summary>
        private void UpdateDescription()
        {
            descriptionText.Text = Items.Count == 0 || SelectedIndex == -1 ? NoItemsText : SelectedItem.Description;
            int lineCount = descriptionText.LineCount;
            descriptionRect.Size = new SizeF(width, (lineCount * (descriptionText.LineHeight + 5)) + (lineCount - 1) + 10);
        }
        /// <summary>
        /// Processes the button presses.
        /// </summary>
        private void ProcessControls()
        {
            // If the user wants to disable the controls, do so but only the ones required
            if (DisableControls)
            {
                bool isUsingController = Controls.IsUsingController;

                foreach (Control control in controls)
                {
                    // If the player is using a controller and is required on gamepads
                    if (isUsingController && controlsGamepad.Contains(control))
                    {
                        continue;
                    }
                    // If the player is using a controller or mouse usage is disabled and is a camera control
                    if ((isUsingController || MouseBehavior == MenuMouseBehavior.Disabled) && controlsCamera.Contains(control))
                    {
                        continue;
                    }
                    // If the control is required by the mod developer
                    if (RequiredControls.Contains(control))
                    {
                        continue;
                    }

                    Controls.DisableThisFrame(control);
                }
            }

            // If the menu is just opened, don't start processing controls until the player has stopped pressing the accept or cancel buttons
            if (justOpenedControlChecks)
            {
                if (Controls.IsPressed((Control)177 /*PhoneCancel*/) || Controls.IsPressed(Control.FrontendPause) ||
                    Controls.IsPressed(Control.FrontendAccept) || Controls.IsPressed((Control)176 /*PhoneSelect*/) ||
                    Controls.IsPressed(Control.CursorAccept))
                {
                    return;
                }
                justOpenedControlChecks = false;
            }

            // If the controls are disabled, the menu has just been opened or the text input field is active, return
#if FIVEM
            bool isKeyboardActive = API.UpdateOnscreenKeyboard() == 0;
#elif ALTV
            bool isKeyboardActive = Alt.Natives.UpdateOnscreenKeyboard() == 0;
#elif RAGEMP
            bool isKeyboardActive = Invoker.Invoke<int>(Natives.UpdateOnscreenKeyboard) == 0;
#elif RPH
            bool isKeyboardActive = NativeFunction.CallByHash<int>(0x0CF2B696BBF945AE) == 0;
#elif SHVDN3 || SHVDNC
            bool isKeyboardActive = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD) == 0;
#endif
            if (!AcceptsInput || isKeyboardActive)
            {
                return;
            }

            // Check if the controls necessary were pressed
            bool leftClick = Controls.IsJustPressed(Control.CursorAccept);
            bool rightClick = Controls.IsJustPressed(Control.CursorCancel);
            bool acceptPressed = Controls.IsJustPressed(Control.FrontendAccept) || (leftClick && MouseBehavior == MenuMouseBehavior.Scrolling);
            bool cancelPressed = Controls.IsJustPressed((Control)177 /*PhoneCancel*/) || Controls.IsJustPressed(Control.FrontendPause);

            if (MouseBehavior == MenuMouseBehavior.Disabled && rightClick && cancelPressed)
            {
                cancelPressed = false;
            }

            bool upPressed = Controls.IsJustPressed((Control)172 /*PhoneUp*/) || (Controls.IsJustPressed(Control.CursorScrollUp) && MouseBehavior == MenuMouseBehavior.Scrolling);
            bool downPressed = Controls.IsJustPressed((Control)173 /*PhoneDown*/) || (Controls.IsJustPressed(Control.CursorScrollDown) && MouseBehavior == MenuMouseBehavior.Scrolling);
            bool leftPressed = Controls.IsJustPressed((Control)174 /*PhoneLeft*/);
            bool rightPressed = Controls.IsJustPressed((Control)175 /*PhoneRight*/);

            bool upHeld = Controls.IsPressed((Control)172 /*PhoneUp*/) || (Controls.IsPressed(Control.CursorScrollUp) && MouseBehavior == MenuMouseBehavior.Scrolling);
            bool downHeld = Controls.IsPressed((Control)173 /*PhoneDown*/) || (Controls.IsPressed(Control.CursorScrollDown) && MouseBehavior == MenuMouseBehavior.Scrolling);
            bool leftHeld = Controls.IsPressed((Control)174 /*PhoneLeft*/);
            bool rightHeld = Controls.IsPressed((Control)175 /*PhoneRight*/);

            // If the player pressed the back button, go back or close the menu
            if (cancelPressed)
            {
                Back();
                return;
            }

#if ALTV
            long time = Alt.Natives.GetGameTimer();
#elif RAGEMP
            long time = Misc.GetGameTimer();
#elif FIVEM || RPH || SHVDN3 || SHVDNC
            long time = Game.GameTime;
#endif

            if (HeldTime > 0 && (upHeld || downHeld || leftHeld || rightHeld))
            {
                if (heldSince == -1)
                {
                    heldSince = time;
                }
            }
            else
            {
                heldSince = -1;
            }

            // If the player pressed up, go to the previous item
            if ((upPressed && !downPressed) || (upHeld && !downHeld && heldSince > 0 && heldSince + HeldTime < time))
            {
                heldSince = time;
                Previous();
                return;
            }
            // If he pressed down, go to the next item
            if ((downPressed && !upPressed) || (downHeld && !upHeld && heldSince > 0 && heldSince + HeldTime < time))
            {
                heldSince = time;
                Next();
                return;
            }

            // Get the currently selected item for later use (for the sake of performance)
            NativeItem selectedItem = SelectedItem;

            // If the mouse controls are enabled and the user is not using a controller
            if (MouseBehavior == MenuMouseBehavior.Movement && !Controls.IsUsingController)
            {
                // If the camera should be rotated when the cursor is on the left and right sections of the screen, do so
                if (RotateCamera)
                {
                    if (GameScreen.IsCursorInArea(PointF.Empty, searchAreaSize))
                    {
#if FIVEM || SHVDN3 || SHVDNC
                        GameplayCamera.RelativeHeading += 5;
#elif ALTV
                        float current = Alt.Natives.GetGameplayCamRelativeHeading();
                        Alt.Natives.SetGameplayCamRelativeHeading(current + 5);
#elif RAGEMP
                        float current = Invoker.Invoke<float>(0x743607648ADD4587);
                        Invoker.Invoke(0xB4EC2312F4E5B1F1, current + 5);
#elif RPH
                        Camera.RenderingCamera.Heading += 5;
#endif
                    }
                    else if (GameScreen.IsCursorInArea(searchAreaRight, searchAreaSize))
                    {
#if FIVEM || SHVDN3 || SHVDNC
                        GameplayCamera.RelativeHeading -= 5;
#elif ALTV
                        float current = Alt.Natives.GetGameplayCamRelativeHeading();
                        Alt.Natives.SetGameplayCamRelativeHeading(current - 5);
#elif RAGEMP
                        float current = Invoker.Invoke<float>(0x743607648ADD4587);
                        Invoker.Invoke(0xB4EC2312F4E5B1F1, current - 5);
#elif RPH
                        Camera.RenderingCamera.Heading -= 5;
#endif
                    }
                }

                // If the player pressed the click button
                if (leftClick && MouseBehavior == MenuMouseBehavior.Movement)
                {
                    // Iterate over the items on the screen
                    foreach (NativeItem item in visibleItems)
                    {
                        // If the item is selected and slidable
                        if (item == selectedItem && item is NativeSlidableItem slidable)
                        {
                            // If the right arrow was pressed, go to the right
                            if (GameScreen.IsCursorInArea(slidable.RightArrow.Position, slidable.RightArrow.Size))
                            {
                                if (item.Enabled)
                                {
                                    slidable.GoRight();
                                    SoundLeftRight?.PlayFrontend();
                                }
                                else
                                {
                                    SoundDisabled?.PlayFrontend();
                                }
                                return;
                            }
                            // If the user pressed the left arrow, go to the right
                            else if (GameScreen.IsCursorInArea(slidable.LeftArrow.Position, slidable.LeftArrow.Size))
                            {
                                if (item.Enabled)
                                {
                                    slidable.GoLeft();
                                    SoundLeftRight?.PlayFrontend();
                                }
                                else
                                {
                                    SoundDisabled?.PlayFrontend();
                                }
                                return;
                            }
                        }

                        // If the cursor is inside of the selection rectangle
                        if (item.IsHovered)
                        {
                            if (item is NativeSeparatorItem)
                            {
                                return;
                            }

                            // If the item is selected, activate it
                            if (item == selectedItem)
                            {
                                if (item.Enabled)
                                {
                                    ItemActivated?.Invoke(this, new ItemActivatedArgs(selectedItem));
                                    item.OnActivated(this);
                                    SoundActivated?.PlayFrontend();
                                    if (item is NativeCheckboxItem checkboxItem)
                                    {
                                        checkboxItem.UpdateTexture(true);
                                    }
                                }
                                else
                                {
                                    SoundDisabled?.PlayFrontend();
                                }
                            }
                            // If is is not, set it as the selected item
                            else
                            {
                                SelectedItem = item;
                            }

                            // We found the item that was clicked, stop the function
                            return;
                        }
                    }

                    // If we got here, the user clicked outside of the selected item area
                    // So close the menu if required (same behavior of the interaction menu)
                    if (CloseOnInvalidClick)
                    {
                        if (selectedItem is { IsHovered: true } || selectedItem?.Panel is { Clickable: true })
                        {
                            return;
                        }
                        Visible = false;
                    }
                    return;
                }
            }

            // If the player pressed the left or right button, trigger the event and sound
            if (SelectedItem is NativeSlidableItem slidableItem && !upHeld && !downHeld)
            {
                if ((leftPressed && !rightPressed) || (leftHeld && heldSince > 0 && heldSince + HeldTime < time))
                {
                    heldSince = time;

                    if (SelectedItem.Enabled)
                    {
                        slidableItem.GoLeft();
                        SoundLeftRight?.PlayFrontend();
                    }
                    else
                    {
                        SoundDisabled?.PlayFrontend();
                    }
                    return;
                }
                if ((rightPressed && !leftPressed) || (rightHeld && heldSince > 0 && heldSince + HeldTime < time))
                {
                    heldSince = time;

                    if (SelectedItem.Enabled)
                    {
                        slidableItem.GoRight();
                        SoundLeftRight?.PlayFrontend();
                    }
                    else
                    {
                        SoundDisabled?.PlayFrontend();
                    }
                    return;
                }
            }

            // If the player selected an item, activate it
            if (acceptPressed)
            {
                if (SelectedItem != null && SelectedItem.Enabled)
                {
                    ItemActivated?.Invoke(this, new ItemActivatedArgs(selectedItem));
                    SelectedItem.OnActivated(this);
                    SoundActivated?.PlayFrontend();
                    if (SelectedItem is NativeCheckboxItem check)
                    {
                        check.UpdateTexture(true);
                    }
                }
                else
                {
                    SoundDisabled?.PlayFrontend();
                }
            }
        }
        /// <summary>
        /// Draws the UI Elements.
        /// </summary>
        private void Draw()
        {
            NativeItem selected = SelectedItem;

            if (bannerImage != null)
            {
                bannerImage.Draw();
                BannerText?.Draw();
            }

            if (ShouldDrawNameBackground)
            {
                nameImage.Draw();
                nameText?.Draw();
                if (ShouldDrawCount)
                {
                    countText.Draw();
                }
            }

            if (!string.IsNullOrWhiteSpace(descriptionText.Text))
            {
                descriptionRect.Draw();
                descriptionText.Draw();
            }

            if (Items.Count == 0)
            {
                return;
            }

            backgroundImage?.Draw();

            for (int i = 0; i < visibleItems.Count; i++)
            {
                NativeItem item = visibleItems[i];

                if (item == selected)
                {
                    continue;
                }

                if (item.IsHovered && MouseBehavior == MenuMouseBehavior.Movement && !(item is NativeSeparatorItem))
                {
                    hoveredRect.Position = item.lastPosition;
                    hoveredRect.Size = item.lastSize;
                    hoveredRect.Draw();
                }

                item.Draw();
            }
            // Continue with the white selection rectangle
            if (selected != null && !selected.UseCustomBackground)
            {
                selectedRect.Draw();
            }
            // And finish with the selected item on top (if any)
            if (selected != null)
            {
                selected.Draw();
                if (selected.Panel != null && selected.Panel.Visible)
                {
                    selected.Panel.Process();
                }
            }
        }

        #endregion

        #region Functions

        /// <inheritdoc/>
        public IEnumerator<NativeItem> GetEnumerator() => Items.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Adds an item at the end of the menu.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(NativeItem item) => Add(Items.Count, item);
        /// <summary>
        /// Adds a specific menu as a submenu with an item.
        /// </summary>
        /// <param name="menu">The menu to add.</param>
        public void Add(NativeMenu menu) => AddSubMenu(menu);
        /// <summary>
        /// Adds an item at the specified position.
        /// </summary>
        /// <param name="position">The position of the item.</param>
        /// <param name="item">The item to add.</param>
        public virtual void Add(int position, NativeItem item)
        {
            if (Items.Contains(item))
            {
                throw new InvalidOperationException("The item is already part of the menu.");
            }
            if (position < 0 || position > Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "The index under Zero or is over the Item Count.");
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Items.Insert(position, item);

            if (Items.Count != 0 && SelectedIndex == -1)
            {
                SelectedIndex = 0;
            }
            else
            {
                UpdateItemList();
            }

            MenuModified?.Invoke(this, new MenuModifiedEventArgs(item, ItemOperation.Added));

            UpdateItems();
        }
        /// <summary>
        /// Adds a specific menu as a submenu with an item.
        /// </summary>
        /// <param name="menu">The menu to add.</param>
        /// <returns>The item that points to the submenu.</returns>
        public NativeSubmenuItem AddSubMenu(NativeMenu menu)
        {
            // If the menu is null, raise an exception
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            // Create a new menu item, add it and return it
            NativeSubmenuItem item = new NativeSubmenuItem(menu, this);
            Add(item);
            return item;
        }
        /// <summary>
        /// Adds a specific menu as a submenu with an item and endlabel string.
        /// </summary>
        /// <param name="menu">The menu to add.</param>
        /// <param name="endlabel">The alternative title of the item, shown on the right.</param>
        /// <returns>The item that points to the submenu.</returns>
        public NativeSubmenuItem AddSubMenu(NativeMenu menu, string endlabel)
        {
            NativeSubmenuItem item = AddSubMenu(menu);
            item.AltTitle = endlabel;
            return item;
        }
        /// <summary>
        /// Removes an item from the menu.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void Remove(NativeItem item)
        {
            if (!Items.Contains(item))
            {
                return;
            }

            Items.Remove(item);

            if (SelectedIndex >= Items.Count)
            {
                SelectedIndex = Items.Count - 1;
            }
            else
            {
                UpdateItemList();
            }

            MenuModified?.Invoke(this, new MenuModifiedEventArgs(item, ItemOperation.Removed));

            UpdateItems();
        }
        /// <summary>
        /// Removes the items that match the predicate.
        /// </summary>
        /// <param name="pred">The function to use as a check.</param>
        public void Remove(Func<NativeItem, bool> pred)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                NativeItem item = Items[i];

                if (!pred(item))
                {
                    continue;
                }

                Items.Remove(item);
                MenuModified?.Invoke(this, new MenuModifiedEventArgs(item, ItemOperation.Added));
            }

            if (SelectedIndex >= Items.Count)
            {
                SelectedIndex = Items.Count - 1;
            }
            else
            {
                UpdateItemList();
            }

            UpdateItems();
        }
        /// <summary>
        /// Removes all of the items from this menu.
        /// </summary>
        public void Clear()
        {
            List<NativeItem> items = new List<NativeItem>(Items);

            Items.Clear();

            foreach (NativeItem item in items)
            {
                MenuModified?.Invoke(this, new MenuModifiedEventArgs(item, ItemOperation.Added));
            }

            index = 0;
            firstItem = 0;

            UpdateItemList();
            UpdateItems();
        }
        /// <summary>
        /// Checks if an item is part of the menu.
        /// </summary>
        /// <param name="item">The item to check.</param>
        public bool Contains(NativeItem item) => Items.Contains(item);
        /// <summary>
        /// Draws the menu and handles the controls.
        /// </summary>
        public virtual void Process()
        {
            if (!visible)
            {
                if (!justClosed)
                {
                    return;
                }

#if FIVEM
                API.SetInputExclusive(0, (int)Control.PhoneCancel);
                API.SetInputExclusive(0, (int)Control.FrontendPause);
#elif ALTV
                Alt.Natives.SetInputExclusive(0, (int)Control.CellPhoneCancel);
                Alt.Natives.SetInputExclusive(0, (int)Control.FrontendPause);
#elif RAGEMP
                Invoker.Invoke(Natives.SetInputExclusive, 0, (int)Control.PhoneCancel);
                Invoker.Invoke(Natives.SetInputExclusive, 0, (int)Control.FrontendPause);
#elif RPH
                NativeFunction.CallByHash<int>(0x351220255D64C155, 0, (int)Control.CellphoneCancel);
                NativeFunction.CallByHash<int>(0x351220255D64C155, 0, (int)Control.FrontendPause);
#elif SHVDN3 || SHVDNC
                Function.Call(Hash.SET_INPUT_EXCLUSIVE, 0, (int)Control.PhoneCancel);
                Function.Call(Hash.SET_INPUT_EXCLUSIVE, 0, (int)Control.FrontendPause);
#endif

                if (!Controls.IsPressed((Control)177 /*PhoneCancel*/) && !Controls.IsPressed(Control.FrontendPause))
                {
                    justClosed = false;
                }

                return;
            }

            NativeItem selected = SelectedItem;
            if (selected != null && descriptionText.Text != selected.Description)
            {
                UpdateDescription();
            }

            if (MouseBehavior == MenuMouseBehavior.Movement)
            {
                GameScreen.ShowCursorThisFrame();
            }

            Draw();
            ProcessControls();
            Buttons.Draw();
        }
        /// <summary>
        /// Calculates the positions and sizes of the elements.
        /// </summary>
        public virtual void Recalculate()
        {
            // Store the current values of X and Y
            PointF pos;
            if (SafeZoneAware)
            {
                float x = 0;
                switch (Alignment)
                {
                    case Alignment.Left:
                        x = Offset.X;
                        break;
                    case Alignment.Right:
                        x = Offset.X - Width;
                        break;
                }

                pos = SafeZone.GetPositionAt(new PointF(x, Offset.Y), Alignment, GFXAlignment.Top);
            }
            else
            {
                float x = 0;
                switch (Alignment)
                {
                    case Alignment.Left:
                        x = Offset.X;
                        break;
                    case Alignment.Right:
                        x = 1f.ToXScaled() - Width - Offset.X;
                        break;
                }
                pos = new PointF(x, Offset.Y);
            }

            // If there is a banner and is a valid element
            if (bannerImage != null && bannerImage is BaseElement bannerImageBase)
            {
                // Set the position and size of the banner
                bannerImageBase.literalPosition = new PointF(pos.X, pos.Y);
                bannerImageBase.literalSize = new SizeF(width, bannerImageBase.Size.Height);
                bannerImageBase.Recalculate();
                // If there is a text element, also set the position of it
                if (BannerText != null)
                {
                    BannerText.Position = new PointF(pos.X + 209, pos.Y + 22);
                }
                // Finally, increase the current position of Y based on the banner height
                pos.Y += bannerImageBase.Size.Height;
            }

            // Time for the name background
            // Set the position and size of it
            nameImage.literalPosition = new PointF(pos.X, pos.Y);
            nameImage.literalSize = new SizeF(width, nameHeight);
            nameImage.Recalculate();
            // If there is a text, also set the position of it
            if (nameText != null)
            {
                nameText.Position = new PointF(pos.X + 6, pos.Y + 4.2f);
            }
            // Finally, increase the size based on the name height
            // currentY += nameHeight;

            // Set the size of the selection rectangle
            selectedRect.Size = new SizeF(width, itemHeight);
            // And set the word wrap of the description
            descriptionText.WordWrap = width - posXDescTxt;

            // Set the right size of the rotation
            searchAreaRight = new PointF(1f.ToXScaled() - 30, 0);

            // Then, continue with an item update
            UpdateItems();
        }
        /// <summary>
        /// Returns to the previous menu or closes the existing one.
        /// </summary>
        public void Back()
        {
            Visible = false;

            if (Visible)
            {
                return;
            }

            justClosed = true;

            if (Parent != null)
            {
                Parent.Visible = true;
            }
        }
        /// <summary>
        /// Opens the menu.
        /// </summary>
        [Obsolete("Set Visible to true instead.", true)]
        public void Open() => Visible = true;
        /// <summary>
        /// Closes the menu.
        /// </summary>
        [Obsolete("Set Visible to false instead.", true)]
        public void Close() => Visible = false;
        /// <summary>
        /// Moves to the previous item.
        /// Does nothing if the menu has no items.
        /// </summary>
        public void Previous()
        {
            if (Items.Count == 0)
            {
                return;
            }

            int initialIndex = SelectedIndex;
            int nextIndex = SelectedIndex;

            nextIndex -= 1;

            while (true)
            {
                if (nextIndex < 0)
                {
                    nextIndex = Items.Count - 1;
                }

                if (nextIndex == initialIndex)
                {
                    return;
                }

                if (Items[nextIndex] is not NativeSeparatorItem)
                {
                    SelectedIndex = nextIndex;
                    return;
                }

                nextIndex -= 1;
            }
        }
        /// <summary>
        /// Moves to the next item.
        /// Does nothing if the menu has no items.
        /// </summary>
        public void Next()
        {
            if (Items.Count == 0)
            {
                return;
            }

            int initialIndex = SelectedIndex;
            int nextIndex = SelectedIndex;

            nextIndex += 1;

            while (true)
            {
                if (nextIndex >= Items.Count)
                {
                    nextIndex = 0;
                }

                if (nextIndex == initialIndex)
                {
                    return;
                }

                if (Items[nextIndex] is not NativeSeparatorItem)
                {
                    SelectedIndex = nextIndex;
                    return;
                }

                nextIndex += 1;
            }
        }

        #endregion
    }
}

// Menus/NativeCheckboxItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Rockstar-like checkbox item.
    /// </summary>
    public class NativeCheckboxItem : NativeItem
    {
        #region Fields

        /// <summary>
        /// The image shown on the checkbox.
        /// </summary>
        protected internal ScaledTexture check = new ScaledTexture(PointF.Empty, SizeF.Empty, "commonmenu", string.Empty);
        /// <summary>
        /// If this item is checked or not.
        /// </summary>
        private bool checked_ = false;

        #endregion

        #region Defaults

        /// <summary>
        /// The default checkbox textures when the checkbox is checked.
        /// </summary>
        public static readonly BadgeSet DefaultCheckedSet = new BadgeSet
        {
            NormalDictionary = "commonmenu",
            NormalTexture = "shop_box_tick",
            HoveredDictionary = "commonmenu",
            HoveredTexture = "shop_box_tickb"
        };
        /// <summary>
        /// The default checkbox textures when the checkbox is not checked.
        /// </summary>
        public static readonly BadgeSet DefaultUncheckedSet = new BadgeSet
        {
            NormalDictionary = "commonmenu",
            NormalTexture = "shop_box_blank",
            HoveredDictionary = "commonmenu",
            HoveredTexture = "shop_box_blankb"
        };

        #endregion

        #region Properties

        /// <summary>
        /// If this item is checked or not.
        /// </summary>
        public bool Checked
        {
            get => checked_;
            set
            {
                if (checked_ == value)
                {
                    return;
                }
                checked_ = value;
                UpdateTexture(lastSelected);
                CheckboxChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// The textures used when the checkbox is checked.
        /// </summary>
        public BadgeSet CheckedSet { get; set; } = DefaultCheckedSet;
        /// <summary>
        /// The textures used when the checkbox is unchecked.
        /// </summary>
        public BadgeSet UncheckedSet { get; set; } = DefaultUncheckedSet;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the checkbox changes.
        /// </summary>
        public event EventHandler CheckboxChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new <see cref="NativeCheckboxItem"/>.
        /// </summary>
        /// <param name="title">The title used for the Item.</param>
        public NativeCheckboxItem(string title) : this(title, string.Empty, false)
        {
        }
        /// <summary>
        /// Creates a new <see cref="NativeCheckboxItem"/>.
        /// </summary>
        /// <param name="title">The title used for the Item.</param>
        /// <param name="check">If the checkbox should be enabled or not.</param>
        public NativeCheckboxItem(string title, bool check) : this(title, string.Empty, check)
        {
        }
        /// <summary>
        /// Creates a new <see cref="NativeCheckboxItem"/>.
        /// </summary>
        /// <param name="title">The title used for the Item.</param>
        /// <param name="description">The description of the Item.</param>
        public NativeCheckboxItem(string title, string description) : this(title, description, false)
        {
        }
        /// <summary>
        /// Creates a new <see cref="NativeCheckboxItem"/>.
        /// </summary>
        /// <param name="title">The title used for the Item.</param>
        /// <param name="description">The description of the Item.</param>
        /// <param name="check">If the checkbox should be enabled or not.</param>
        public NativeCheckboxItem(string title, string description, bool check) : base(title, description)
        {
            Checked = check;
            Activated += Toggle;
            EnabledChanged += NativeCheckboxItem_EnabledChanged;
        }

        #endregion

        #region Event Functions

        private void NativeCheckboxItem_EnabledChanged(object sender, EventArgs e) => UpdateTexture(lastSelected);

        #endregion

        #region Tools

        /// <summary>
        /// Inverts the checkbox activation.
        /// </summary>
        private void Toggle(object sender, EventArgs e) => Checked = !Checked;
        /// <summary>
        /// Updates the texture of the sprite.
        /// </summary>
        protected internal void UpdateTexture(bool selected)
        {
            bool showLight = !selected || !Enabled;

            // If the item is not selected or is not enabled, use the white pictures
            if (Checked)
            {
                check.Texture = showLight ? CheckedSet.NormalTexture : CheckedSet.HoveredTexture;
            }
            // Otherwise, use the black ones
            else
            {
                check.Texture = showLight ? UncheckedSet.NormalTexture : UncheckedSet.HoveredTexture;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the item positions and sizes with the specified values.
        /// </summary>
        /// <param name="pos">The position of the item.</param>
        /// <param name="size">The size of the item.</param>
        /// <param name="selected">If this item has been selected.</param>
        public override void Recalculate(PointF pos, SizeF size, bool selected)
        {
            base.Recalculate(pos, size, selected);
            // Set the correct texture
            UpdateTexture(selected);
            // And set the checkbox positions
            check.Position = new PointF(pos.X + size.Width - 50, pos.Y - 6);
            check.Size = new SizeF(50, 50);
        }
        /// <summary>
        /// Draws the Checkbox on the screen.
        /// </summary>
        public override void Draw()
        {
            if (UseCustomBackground)
            {
                background.Draw();
            }

            title.Draw();
            badgeLeft?.Draw();
            check.Draw();
        }
        /// <inheritdoc/>
        public override void UpdateColors()
        {
            base.UpdateColors();

            if (!Enabled)
            {
                check.Color = Colors.BadgeRightDisabled;
            }
            else if (lastSelected)
            {
                check.Color = Colors.BadgeRightHovered;
            }
            else
            {
                check.Color = Colors.BadgeRightNormal;
            }
        }

        #endregion
    }
}

// Menus/SelectedEventHandler.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the method that is called when a new item is selected in the Menu.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="SelectedEventArgs"/> with the index information.</param>
    public delegate void SelectedEventHandler(object sender, SelectedEventArgs e);
}

// Menus/SelectedEventArgs.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the selection of an item in the screen.
    /// </summary>
    public class SelectedEventArgs
    {
        #region Properties

        /// <summary>
        /// The index of the item in the full list of items.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// The index of the item in the screen.
        /// </summary>
        public int OnScreen { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="SelectedEventArgs"/>.
        /// </summary>
        /// <param name="index">The index of the item in the menu.</param>
        /// <param name="screen">The index of the item based on the number of items shown on screen,</param>
        public SelectedEventArgs(int index, int screen)
        {
            Index = index;
            OnScreen = screen;
        }

        #endregion
    }
}

// Menus/NativeDynamicItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Dynamic Items allow you to dynamically change the item shown to the user.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    public class NativeDynamicItem<T> : NativeSlidableItem
    {
        #region Fields

        private readonly ScaledText text = new ScaledText(PointF.Empty, string.Empty, 0.35f);
        private T item = default;

        #endregion

        #region Properties

        /// <summary>
        /// The currently selected item.
        /// </summary>
        public T SelectedItem
        {
            get => item;
            set
            {
                item = value;
                UpdateItemName();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the user has changed the item.
        /// </summary>
        public event ItemChangedEventHandler<T> ItemChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Dynamic List Item.
        /// </summary>
        /// <param name="title">The Title of the item.</param>
        public NativeDynamicItem(string title) : this(title, string.Empty, default)
        {
        }
        /// <summary>
        /// Creates a new Dynamic List Item.
        /// </summary>
        /// <param name="title">The Title of the item.</param>
        /// <param name="item">The Item to set.</param>
        public NativeDynamicItem(string title, T item) : this(title, string.Empty, item)
        {
        }
        /// <summary>
        /// Creates a new Dynamic List Item.
        /// </summary>
        /// <param name="title">The Title of the item.</param>
        /// <param name="description">The Description of the item.</param>
        public NativeDynamicItem(string title, string description) : this(title, description, default)
        {
        }
        /// <summary>
        /// Creates a new Dynamic List Item.
        /// </summary>
        /// <param name="title">The Title of the item.</param>
        /// <param name="description">The Description of the item.</param>
        /// <param name="item">The Item to set.</param>
        public NativeDynamicItem(string title, string description, T item) : base(title, description)
        {
            this.item = item;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Updates the currently selected item based on the index.
        /// </summary>
        private void UpdateItemName()
        {
            // This is the SAME as the normal NativeListItem

            text.Text = SelectedItem == null ? string.Empty : SelectedItem.ToString();

            text.Position = new PointF(RightArrow.Position.X - text.Width + 3, text.Position.Y);
            LeftArrow.Position = new PointF(text.Position.X - LeftArrow.Size.Width, LeftArrow.Position.Y);
        }
        /// <summary>
        /// Gets the previous item.
        /// </summary>
        public override void GoLeft()
        {
            ItemChangedEventArgs<T> arguments = new ItemChangedEventArgs<T>(item, -1, Direction.Left);
            ItemChanged?.Invoke(this, arguments);
            SelectedItem = arguments.Object;
            UpdateItemName();
        }
        /// <summary>
        /// Gets the next item.
        /// </summary>
        public override void GoRight()
        {
            ItemChangedEventArgs<T> arguments = new ItemChangedEventArgs<T>(item, -1, Direction.Right);
            ItemChanged?.Invoke(this, arguments);
            SelectedItem = arguments.Object;
            UpdateItemName();
        }
        /// <summary>
        /// Recalculates the position of the current List Item.
        /// </summary>
        /// <param name="pos">The new position of the item.</param>
        /// <param name="size">The Size of the item.</param>
        /// <param name="selected">If the item is selected or not.</param>
        public override void Recalculate(PointF pos, SizeF size, bool selected)
        {
            // This is the SAME as the normal NativeListItem

            base.Recalculate(pos, size, selected);

            float textWidth = RightArrow.Size.Width;
            text.Position = new PointF(pos.X + size.Width - textWidth - 1 - text.Width, pos.Y + 3);
            LeftArrow.Position = new PointF(text.Position.X - LeftArrow.Size.Width, pos.Y + 4);

            UpdateItemName();
        }
        /// <summary>
        /// Draws the List on the screen.
        /// </summary>
        public override void Draw()
        {
            base.Draw(); // Arrows, Title and Left Badge
            text.Draw();
        }
        /// <inheritdoc/>
        public override void UpdateColors()
        {
            base.UpdateColors();

            if (!Enabled)
            {
                text.Color = Colors.TitleDisabled;
            }
            else if (lastSelected)
            {
                text.Color = Colors.TitleHovered;
            }
            else
            {
                text.Color = Colors.TitleNormal;
            }
        }

        #endregion
    }
}

// Menus/ColorSet.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Stores the different colors required to make the colors of a <see cref="NativeItem"/> dynamic.
    /// </summary>
    public class ColorSet
    {
        #region Fields

        private static readonly Color colorWhite = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color colorWhiteSmoke = Color.FromArgb(255, 245, 245, 245);
        private static readonly Color colorBlack = Color.FromArgb(255, 0, 0, 0);
        private static readonly Color colorDisabled = Color.FromArgb(255, 163, 159, 148);

        #endregion

        #region Properties

        /// <summary>
        /// The color of the <see cref="NativeItem.Title"/> when the <see cref="NativeItem"/> is not hovered and enabled.
        /// </summary>
        public Color TitleNormal { get; set; } = colorWhiteSmoke;
        /// <summary>
        /// The color of the <see cref="NativeItem.Title"/> when the <see cref="NativeItem"/> is hovered.
        /// </summary>
        public Color TitleHovered { get; set; } = colorBlack;
        /// <summary>
        /// The color of the <see cref="NativeItem.Title"/> when the <see cref="NativeItem"/> is disabled.
        /// </summary>
        public Color TitleDisabled { get; set; } = colorDisabled;

        /// <summary>
        /// The color of the <see cref="NativeItem.AltTitle"/> when the <see cref="NativeItem"/> is not hovered and enabled.
        /// </summary>
        public Color AltTitleNormal { get; set; } = colorWhiteSmoke;
        /// <summary>
        /// The color of the <see cref="NativeItem.AltTitle"/> when the <see cref="NativeItem"/> is hovered.
        /// </summary>
        public Color AltTitleHovered { get; set; } = colorBlack;
        /// <summary>
        /// The color of the <see cref="NativeItem.AltTitle"/> when the <see cref="NativeItem"/> is disabled.
        /// </summary>
        public Color AltTitleDisabled { get; set; } = colorDisabled;

        /// <summary>
        /// The color of the <see cref="NativeSlidableItem"/> arrows when the item is not hovered and enabled.
        /// </summary>
        public Color ArrowsNormal { get; set; } = colorWhiteSmoke;
        /// <summary>
        /// The color of the <see cref="NativeSlidableItem"/> arrows when the item is hovered.
        /// </summary>
        public Color ArrowsHovered { get; set; } = colorBlack;
        /// <summary>
        /// The color of the <see cref="NativeSlidableItem"/> arrows when the item is disabled.
        /// </summary>
        public Color ArrowsDisabled { get; set; } = colorDisabled;

        /// <summary>
        /// The color of the <see cref="NativeItem.LeftBadge"/> when the <see cref="NativeItem"/> is not hovered and enabled.
        /// </summary>
        public Color BadgeLeftNormal { get; set; } = colorWhite;
        /// <summary>
        /// The color of the <see cref="NativeItem.LeftBadge"/> when the <see cref="NativeItem"/> is hovered.
        /// </summary>
        public Color BadgeLeftHovered { get; set; } = colorWhite;
        /// <summary>
        /// The color of the <see cref="NativeItem.LeftBadge"/> when the <see cref="NativeItem"/> is disabled.
        /// </summary>
        public Color BadgeLeftDisabled { get; set; } = colorWhite;

        /// <summary>
        /// The color of the <see cref="NativeItem.RightBadge"/> or <see cref="NativeCheckboxItem"/> checkbox when the <see cref="NativeItem"/> is not hovered and enabled.
        /// </summary>
        public Color BadgeRightNormal { get; set; } = colorWhite;
        /// <summary>
        /// The color of the <see cref="NativeItem.RightBadge"/> or <see cref="NativeCheckboxItem"/> checkbox when the <see cref="NativeItem"/> is hovered.
        /// </summary>
        public Color BadgeRightHovered { get; set; } = colorWhite;
        /// <summary>
        /// The color of the <see cref="NativeItem.RightBadge"/> or <see cref="NativeCheckboxItem"/> checkbox when the <see cref="NativeItem"/> is disabled.
        /// </summary>
        public Color BadgeRightDisabled { get; set; } = colorWhite;

        /// <summary>
        /// The normal color of the custom background if <see cref="NativeItem.UseCustomBackground"/> is set to <see langword="true"/>.
        /// </summary>
        public Color BackgroundNormal { get; set; } = colorWhite;
        /// <summary>
        /// The hovered color of the custom background if <see cref="NativeItem.UseCustomBackground"/> is set to <see langword="true"/>.
        /// </summary>
        public Color BackgroundHovered { get; set; } = colorWhite;
        /// <summary>
        /// The disabled color of the custom background if <see cref="NativeItem.UseCustomBackground"/> is set to <see langword="true"/>.
        /// </summary>
        public Color BackgroundDisabled { get; set; } = colorWhite;

        #endregion
    }
}

// Menus/NativeStatsInfo.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the Information of a specific field in a <see cref="NativeStatsPanel"/>.
    /// </summary>
    public class NativeStatsInfo
    {
        #region Fields

        private const float barWidth = 33;
        private const float barHeight = 9;
        private readonly ScaledText text = new ScaledText(PointF.Empty, string.Empty, 0.35f);
        private float value = 100;
        private readonly List<ScaledRectangle> backgrounds = new List<ScaledRectangle>();
        private readonly List<ScaledRectangle> foregrounds = new List<ScaledRectangle>();

        #endregion

        #region Properties

        /// <summary>
        /// The name of the Stats Field.
        /// </summary>
        public string Name
        {
            get => text.Text;
            set => text.Text = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The value of the Stats bar.
        /// </summary>
        public float Value
        {
            get => value;
            set
            {
                if (value > 100 || value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The Value of the Stat can't be over 100 or under 0.");
                }

                this.value = value;
                UpdateBars();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Stat Info with the specified name and value set to zero.
        /// </summary>
        /// <param name="name">The name of the Stat.</param>
        public NativeStatsInfo(string name) : this(name, 0)
        {
        }
        /// <summary>
        /// Creates a new Stat Info with the specified name and value.
        /// </summary>
        /// <param name="name">The name of the Stat.</param>
        /// <param name="value"></param>
        public NativeStatsInfo(string name, int value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.value = value;

            for (int i = 0; i < 5; i++)
            {
                backgrounds.Add(new ScaledRectangle(PointF.Empty, SizeF.Empty));
                foregrounds.Add(new ScaledRectangle(PointF.Empty, SizeF.Empty));
            }
        }

        #endregion

        #region Functions

        internal void SetColor(Color background, Color foreground)
        {
            foreach (ScaledRectangle rectangle in backgrounds)
            {
                rectangle.Color = background;
            }
            foreach (ScaledRectangle rectangle in foregrounds)
            {
                rectangle.Color = foreground;
            }
        }
        /// <summary>
        /// Updates the values of the bars.
        /// </summary>
        private void UpdateBars()
        {
            SizeF @default = new SizeF(barWidth, barHeight);

            // FIRST BAR
            if (value > 0 && value < 20)
            {
                foregrounds[0].Size = new SizeF(barWidth * (value / 20), barHeight);
            }
            else
            {
                foregrounds[0].Size = value > 20 ? @default : SizeF.Empty;
            }

            // SECOND BAR
            if (value > 20 && value < 40)
            {
                foregrounds[1].Size = new SizeF(barWidth * ((value - 20) / 20), barHeight);
            }
            else
            {
                foregrounds[1].Size = value > 40 ? @default : SizeF.Empty;
            }

            // THIRD BAR
            if (value > 40 && value < 60)
            {
                foregrounds[2].Size = new SizeF(barWidth * ((value - 40) / 20), barHeight);
            }
            else
            {
                foregrounds[2].Size = value > 60 ? @default : SizeF.Empty;
            }

            // FOURTH BAR
            if (value > 60 && value < 80)
            {
                foregrounds[3].Size = new SizeF(barWidth * ((value - 60) / 20), barHeight);
            }
            else
            {
                foregrounds[3].Size = value > 80 ? @default : SizeF.Empty;
            }

            // FIFTH BAR
            if (value > 80 && value < 100)
            {
                foregrounds[4].Size = new SizeF(barWidth * ((value - 80) / 20), barHeight);
            }
            else
            {
                foregrounds[4].Size = value == 100 ? @default : SizeF.Empty;
            }
        }
        /// <summary>
        /// Recalculates the position of the stat Text and Bar.
        /// </summary>
        /// <param name="position">The new position fot the Stat.</param>
        /// <param name="width">The Width of the parent Stats Panel.</param>
        public void Recalculate(PointF position, float width)
        {
            const float barOffsetTop = 11;
            const float offsetLeft = 9;
            const float separatorSize = 4;
            const float rightMargin = 42;

            text.Position = new PointF(position.X + offsetLeft, position.Y);

            for (int i = 0; i < 5; i++)
            {
                PointF pos = new PointF(position.X + width - rightMargin - (barWidth * (5 - i)) - (separatorSize * (5 - i)), position.Y + barOffsetTop);

                backgrounds[i].Position = pos;
                backgrounds[i].Size = new SizeF(barWidth, barHeight);
                foregrounds[i].Position = pos;
            }

            UpdateBars();
        }
        /// <summary>
        /// Draws the stat information.
        /// </summary>
        public void Draw()
        {
            foreach (ScaledRectangle background in backgrounds)
            {
                background.Draw();
            }
            foreach (ScaledRectangle foreground in foregrounds)
            {
                foreground.Draw();
            }
            text.Draw();
        }

        #endregion
    }
}

// Menus/GridValueChangedArgs.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the Previous and Current X and Y values when changing the position on a grid.
    /// </summary>
    public class GridValueChangedArgs
    {
        #region Properties

        /// <summary>
        /// The values present before they were changed.
        /// </summary>
        public PointF Before { get; }
        /// <summary>
        /// The values present after they were changed.
        /// </summary>
        public PointF After { get; }

        #endregion

        #region Constructors

        internal GridValueChangedArgs(PointF before, PointF after)
        {
            Before = before;
            After = after;
        }

        #endregion
    }
}

// Menus/NativeSliderItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// A slider item for changing integer values.
    /// </summary>
    public class NativeSliderItem : NativeSlidableItem
    {
        #region Fields

        /// <summary>
        /// The background of the slider.
        /// </summary>
        internal protected ScaledRectangle background = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(255, 4, 32, 57)
        };
        /// <summary>
        /// THe front of the slider.
        /// </summary>
        internal protected ScaledRectangle slider = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(255, 57, 116, 200)
        };

        /// <summary>
        /// The maximum value of the slider.
        /// </summary>
        private int maximum = 0;
        /// <summary>
        /// The current value of the slider.
        /// </summary>
        private int _value = 100;

        #endregion

        #region Properties

        /// <summary>
        /// The color of the Slider.
        /// </summary>
        public Color SliderColor
        {
            get => slider.Color;
            set => slider.Color = value;
        }
        /// <summary>
        /// The maximum value of the slider.
        /// </summary>
        public int Maximum
        {
            get => maximum;
            set
            {
                // If the value was not changed, return
                if (maximum == value)
                {
                    return;
                }
                // Otherwise, save the new value
                maximum = value;
                // If the current value is higher than the max, set the max
                if (_value > maximum)
                {
                    _value = maximum;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
                // Finally, update the location of the slider
                UpdatePosition();
            }
        }
        /// <summary>
        /// The current value of the slider.
        /// </summary>
        public int Value
        {
            get => _value;
            set
            {
                // If the value is over the limit, raise an exception
                if (value > maximum)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"The value is over the maximum of {maximum - 1}");
                }
                // Otherwise, save it
                _value = value;
                // Trigger the respective event
                ValueChanged?.Invoke(this, EventArgs.Empty);
                // And update the location of the slider
                UpdatePosition();
            }
        }
        /// <summary>
        /// The multiplier for increasing and decreasing the value.
        /// </summary>
        public int Multiplier { get; set; } = 1;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the value of the menu changes.
        /// </summary>
        public event EventHandler ValueChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a <see cref="NativeSliderItem"/> with a maximum of 100.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        public NativeSliderItem(string title) : this(title, string.Empty, 100, 0)
        {
        }
        /// <summary>
        /// Creates a <see cref="NativeSliderItem"/> with a maximum of 100.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="description">The description of the Item.</param>
        public NativeSliderItem(string title, string description) : this(title, description, 100, 0)
        {
        }
        /// <summary>
        /// Creates a <see cref="NativeSliderItem"/> with a specific current and maximum value.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="max">The maximum value of the Slider.</param>
        /// <param name="value">The current value of the Slider.</param>
        public NativeSliderItem(string title, int max, int value) : this(title, string.Empty, max, value)
        {
        }
        /// <summary>
        /// Creates a <see cref="NativeSliderItem"/> with a specific maximum.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="description">The description of the Item.</param>
        /// <param name="max">The maximum value of the Slider.</param>
        /// <param name="value">The current value of the Slider.</param>
        public NativeSliderItem(string title, string description, int max, int value) : base(title, description)
        {
            maximum = max;
            _value = value;
        }

        #endregion

        #region Tools

        /// <summary>
        /// Updates the position of the bar based on the value.
        /// </summary>
        internal protected void UpdatePosition()
        {
            // Calculate the increment, and then the value of X
            float increment = _value / (float)maximum;
            float x = (background.Size.Width - slider.Size.Width) * increment;
            // Then, add the X to the slider position
            slider.Position = new PointF(background.Position.X + x, background.Position.Y);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the item positions and sizes with the specified values.
        /// </summary>
        /// <param name="pos">The position of the item.</param>
        /// <param name="size">The size of the item.</param>
        /// <param name="selected">If this item has been selected.</param>
        public override void Recalculate(PointF pos, SizeF size, bool selected)
        {
            base.Recalculate(pos, size, selected);
            // Set the position and size of the background
            background.Size = new SizeF(150, 9);
            background.Position = new PointF(pos.X + size.Width - background.Size.Width - 7 - LeftArrow.Size.Width, pos.Y + 14);
            // And do the same for the left arrow
            LeftArrow.Position = new PointF(background.Position.X - LeftArrow.Size.Width, pos.Y + 4);
            // Finally, set the correct position of the slider
            slider.Size = new SizeF(75, 9);
            UpdatePosition();
        }
        /// <summary>
        /// Reduces the value of the slider.
        /// </summary>
        public override void GoLeft()
        {
            // Calculate the new value
            int newValue = _value - Multiplier;
            // If is under zero, set it to zero
            if (newValue < 0)
            {
                Value = 0;
            }
            // Otherwise, set it to the new value
            else
            {
                Value = newValue;
            }
        }
        /// <summary>
        /// Increases the value of the slider.
        /// </summary>
        public override void GoRight()
        {
            // Calculate the new value
            int newValue = _value + Multiplier;
            // If the value is over the maximum, set the max
            if (newValue > maximum)
            {
                Value = maximum;
            }
            // Otherwise, set the calculated value
            else
            {
                Value = newValue;
            }
        }
        /// <summary>
        /// Draws the slider.
        /// </summary>
        public override void Draw()
        {
            base.Draw(); // Arrows, Title and Left Badge
            background.Draw();
            slider.Draw();
        }

        #endregion
    }
}

// Menus/BadgeSet.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents a badge that can be applied to a <see cref="NativeItem"/>.
    /// </summary>
    public class BadgeSet
    {
        #region Fields

        private string normalDict = string.Empty;
        private string normalTexture = string.Empty;
        private string hoveredDict = string.Empty;
        private string hoveredTexture = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The texture dictionary where the normal texture is located.
        /// </summary>
        public string NormalDictionary
        {
            get => normalDict;
            set => normalDict = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The texture to use when the item is not hovered.
        /// </summary>
        public string NormalTexture
        {
            get => normalTexture;
            set => normalTexture = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The texture dictionary where the normal texture is located.
        /// </summary>
        public string HoveredDictionary
        {
            get => hoveredDict;
            set => hoveredDict = value ?? throw new ArgumentNullException(nameof(value));
        }
        /// <summary>
        /// The texture to use when the item is hovered.
        /// </summary>
        public string HoveredTexture
        {
            get => hoveredTexture;
            set => hoveredTexture = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new empty <see cref="BadgeSet"/>.
        /// </summary>
        public BadgeSet()
        {
        }
        /// <summary>
        /// Creates a new <see cref="BadgeSet"/> where both textures are in the same dictionary.
        /// </summary>
        /// <param name="dict">The dictionary where the textures are located.</param>
        /// <param name="normal">The normal texture name.</param>
        /// <param name="hovered">The hovered texture name.</param>
        public BadgeSet(string dict, string normal, string hovered)
        {
            normalDict = dict ?? throw new ArgumentNullException(nameof(dict));
            normalTexture = normal ?? throw new ArgumentNullException(nameof(normal));
            hoveredDict = dict;
            hoveredTexture = hovered ?? throw new ArgumentNullException(nameof(hovered));
        }
        /// <summary>
        /// Creates a new <see cref="BadgeSet"/> where both textures are in different dictionaries.
        /// </summary>
        /// <param name="normalDict">The dictionary where the normal texture is located.</param>
        /// <param name="normalTexture">The normal texture name.</param>
        /// <param name="hoveredDict">The dictionary where the hovered texture is located.</param>
        /// <param name="hoveredTexture">The hovered texture name.</param>
        public BadgeSet(string normalDict, string normalTexture, string hoveredDict, string hoveredTexture)
        {
            this.normalDict = normalDict ?? throw new ArgumentNullException(nameof(normalDict));
            this.normalTexture = normalTexture ?? throw new ArgumentNullException(nameof(normalTexture));
            this.hoveredDict = hoveredDict ?? throw new ArgumentNullException(nameof(hoveredDict));
            this.hoveredTexture = hoveredTexture ?? throw new ArgumentNullException(nameof(hoveredTexture));
        }

        #endregion
    }
}

// Menus/NativeColorPanel.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// A Panel that allows you to select a Color.
    /// </summary>
    public class NativeColorPanel : NativePanel, IEnumerable<NativeColorData>
    {
        #region Constants

        /// <summary>
        /// The space difference for the colors and opacity bar on the left.
        /// </summary>
        private const float leftDifference = 16;

        /// <summary>
        /// The space difference for the colors and opacity bar on the left.
        /// </summary>
        private const float rightDifference = 12;

        #endregion

        #region Fields

        /// <summary>
        /// The position reported after the last Recalculation.
        /// </summary>
        private PointF lastPosition = PointF.Empty;

        /// <summary>
        /// The Width reported after the last Recalculation.
        /// </summary>
        private float lastWidth = 0;

        /// <summary>
        /// The title of the Color Panel.
        /// </summary>
        private ScaledText title = new ScaledText(PointF.Empty, string.Empty, 0.325f)
        {
            Alignment = Alignment.Center
        };

        /// <summary>
        /// The rectangle used for marking the item selection on the screen.
        /// </summary>
        private ScaledRectangle selectionRectangle = new ScaledRectangle(PointF.Empty, SizeF.Empty);

        /// <summary>
        /// The "Opacity" text when the opacity bar is enabled
        /// </summary>
        private ScaledText opacityText = new ScaledText(PointF.Empty, "Opacity", 0.325f)
        {
            Alignment = Alignment.Center
        };

        /// <summary>
        /// The zero percent text when the opacity bar is enabled.
        /// </summary>
        private ScaledText percentMin = new ScaledText(PointF.Empty, "0%", 0.325f);

        /// <summary>
        /// The 100 percent text when the opacity bar is enabled.
        /// </summary>
        private ScaledText percentMax = new ScaledText(PointF.Empty, "100%", 0.325f);

        /// <summary>
        /// The top section of the opacity bar.
        /// </summary>
        private ScaledRectangle opacityForeground = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(255, 240, 240, 240)
        };

        /// <summary>
        /// The background of the opacity bar.
        /// </summary>
        private ScaledRectangle opacityBackground = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(150, 88, 88, 88)
        };

        /// <summary>
        /// If the opacity bar is available to the user.
        /// </summary>
        private bool showOpacity = false;

        /// <summary>
        /// The current value of the opacity slider.
        /// </summary>
        private int opacity = 0;

        /// <summary>
        /// The current index of the Colors.
        /// </summary>
        private int index = 0;

        /// <summary>
        /// The position of the first item.
        /// </summary>
        private int firstItem = 0;

        /// <summary>
        /// The maximum number of items shown at once.
        /// </summary>
        private int maxItems = 9;

        /// <summary>
        /// The generic title for this color.
        /// </summary>
        private string simpleTitle = "Color";

        /// <summary>
        /// The style of the title.
        /// </summary>
        private ColorTitleStyle titleStyle = ColorTitleStyle.Simple;

        /// <summary>
        /// If the number of colors should be shown.
        /// </summary>
        private bool showCount = true;

        /// <summary>
        /// The items that are currently visible on the screen.
        /// </summary>
        private List<NativeColorData> visibleItems = new List<NativeColorData>();

        /// <summary>
        /// The default sound used for the Color Navigation.
        /// </summary>
        public static readonly Sound DefaultSound = new Sound("HUD_FRONTEND_DEFAULT_SOUNDSET", "NAV_LEFT_RIGHT");

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override bool Clickable => true;

        /// <summary>
        /// If the Opacity selector should be shown.
        /// </summary>
        public bool ShowOpacity
        {
            get => showOpacity;
            set
            {
                showOpacity = value;
                Recalculate();
            }
        }

        /// <summary>
        /// The opacity value of the color.
        /// </summary>
        /// <remarks>
        /// The value needs to be set between 100 and 0.
        /// It will return -1 if <see cref="ShowOpacity"/> is set to <see langword="false"/>.
        /// </remarks>
        public int Opacity
        {
            get
            {
                if (!ShowOpacity)
                {
                    return -1;
                }

                return opacity;
            }
            set
            {
                if (!ShowOpacity)
                {
                    return;
                }

                if (value > 100 || value < 0)
                {
                    throw new IndexOutOfRangeException("The value needs to be over 0 and under 100.");
                }

                opacity = value;
                UpdateOpacityBar();
            }
        }

        /// <summary>
        /// The currently selected color.
        /// </summary>
        /// <remarks>
        /// If <see cref="ShowOpacity"/> is set to <see langword="true"/>.
        /// </remarks>
        public Color SelectedColor
        {
            get
            {
                // If there is no selected color information, return
                NativeColorData data = SelectedItem;

                if (data == null)
                {
                    return default;
                }

                // Otherwise, return the color
                return Color.FromArgb(ShowOpacity ? (int)(255 * (Opacity * 0.01f)) : 255, data.Color.R, data.Color.G, data.Color.B);
            }
        }

        /// <summary>
        /// Returns the currently selected <see cref="NativeColorData"/>.
        /// </summary>
        public NativeColorData SelectedItem
        {
            get
            {
                if (Colors.Count == 0 || index >= Colors.Count)
                {
                    return null;
                }

                return Colors[SelectedIndex];
            }
        }

        /// <summary>
        /// The index of the currently selected Color.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                if (Colors.Count == 0 || index >= Colors.Count)
                {
                    return -1;
                }

                return index;
            }
            set
            {
                // If the list of items is empty, don't allow the user to set the index
                if (Colors == null || Colors.Count == 0)
                {
                    throw new InvalidOperationException("There are no items in this menu.");
                }
                // If the value is over or equal than the number of items, raise an exception
                else if (value >= Colors.Count)
                {
                    throw new InvalidOperationException($"The index is over {Colors.Count - 1}");
                }

                // Calculate the bounds of the menu
                int lower = firstItem;
                int upper = firstItem + maxItems;

                // Time to set the first item based on the total number of items
                // If the item is between the allowed values, do nothing because we are on the correct first item
                if (value >= lower && value < upper - 1)
                {
                }
                // If the upper bound + 1 equals the new index, increase it by one
                else if (upper == value)
                {
                    firstItem += 1;
                }
                // If the first item minus one equals the value, decrease it by one
                else if (lower - 1 == value)
                {
                    firstItem -= 1;
                }
                // Otherwise, set it somewhere
                else
                {
                    // If the value is under the max items, set it to zero
                    if (value < maxItems)
                    {
                        firstItem = 0;
                    }
                    // Otherwise, set it at the bottom
                    else
                    {
                        firstItem = value - maxItems + 1;
                    }
                }

                // Save the index and update the items
                index = value;
                UpdateItems();
                // Finally, play the switch change sound
                Sound?.PlayFrontend();
            }
        }
        /// <summary>
        /// The Title used for the Panel when <see cref="TitleStyle"/> is set to <see cref="ColorTitleStyle.Simple"/>.
        /// </summary>
        public string Title
        {
            get => simpleTitle;
            set
            {
                simpleTitle = value ?? throw new ArgumentNullException(nameof(value));
                UpdateTitle();
            }
        }
        /// <summary>
        /// The style of the Panel Title.
        /// </summary>
        public ColorTitleStyle TitleStyle
        {
            get => titleStyle;
            set
            {
                titleStyle = value;
                UpdateTitle();
            }
        }
        /// <summary>
        /// If the count of items should be shown as part of the title.
        /// </summary>
        public bool ShowCount
        {
            get => showCount;
            set
            {
                showCount = value;
                UpdateTitle();
            }
        }
        /// <summary>
        /// THe maximum number of items shown on the screen.
        /// </summary>
        public int MaxItems
        {
            get => maxItems;
            set
            {
                if (value == maxItems)
                {
                    return;
                }

                maxItems = value;
                UpdateItems();
                UpdateTitle();
            }
        }
        /// <summary>
        /// The colors shown on this Panel.
        /// </summary>
        public List<NativeColorData> Colors { get; } = new List<NativeColorData>();
        /// <summary>
        /// The sound played when the item is changed.
        /// </summary>
        public Sound Sound { get; set; } = DefaultSound;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a color panel with no Items or Title.
        /// </summary>
        public NativeColorPanel() : this(string.Empty)
        {
        }
        /// <summary>
        /// Creates a Panel with a specific Title and set of Colors.
        /// </summary>
        /// <param name="title">The title of the panel.</param>
        /// <param name="colors">The colors of the panel.</param>
        public NativeColorPanel(string title, params NativeColorData[] colors)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Colors.AddRange(colors);
        }

        #endregion

        #region Tools

        /// <summary>
        /// Updates the Text of the Title.
        /// </summary>
        private void UpdateTitle()
        {
            string newTitle = string.Empty;

            // Add the title based on the correct style
            switch (titleStyle)
            {
                case ColorTitleStyle.Simple:
                    newTitle = Title;

                    break;
                case ColorTitleStyle.ColorName:
                    newTitle = SelectedItem == null ? string.Empty : SelectedItem.Name;
                    break;
            }

            // If we need to add the count of colors
            if (ShowCount)
            {
                // Add a space at the end if required
                if (!newTitle.EndsWith(" "))
                {
                    newTitle += " ";
                }

                // And add the item count
                newTitle += $"({SelectedIndex + 1} of {Colors.Count})";
            }

            // And finally set the new title
            title.Text = newTitle;
        }

        /// <summary>
        /// Updates the position of the Items.
        /// </summary>
        private void UpdateItems()
        {
            // See UpdateItemList() on LemonUI.Menus.NativeMenu to understand this section
            List<NativeColorData> list = new List<NativeColorData>();

            for (int i = 0; i < MaxItems; i++)
            {
                int start = firstItem + i;

                if (start >= Colors.Count)
                {
                    break;
                }

                list.Add(Colors[start]);
            }

            visibleItems = list;

            // Get the width based on the maximum number of items
            float width = (lastWidth - leftDifference - rightDifference) / maxItems;
            // And store the number of items already completed
            int count = 0;

            // Select the correct extra distance based on the prescence of the Opacity toggle
            float extra = ShowOpacity ? 78 : 0;

            // Then, start iterating over the colors visible on the screen
            foreach (NativeColorData color in visibleItems)
            {
                // Set the position based on the number of items completed
                color.rectangle.Position = new PointF(lastPosition.X + leftDifference + (width * count), lastPosition.Y + extra + 54);
                // And set the size of it based on the number of items
                color.rectangle.Size = new SizeF(width, 45);
                // Finally, increase the count by one
                count++;
            }

            // If there is a selected color item
            if (SelectedItem != null)
            {
                // Set the position and size of the selection rectangle based on the currently selected color
                ScaledRectangle colorRect = SelectedItem.rectangle;
                const float height = 8;
                selectionRectangle.Position = new PointF(colorRect.Position.X, colorRect.Position.Y - height);
                selectionRectangle.Size = new SizeF(colorRect.Size.Width, height);
            }

            // Finally, update the text of the title
            UpdateTitle();
        }

        /// <summary>
        /// Updates the size of the opacity bar.
        /// </summary>
        private void UpdateOpacityBar()
        {
            // If the opacity bar is disabled, return
            if (!ShowOpacity)
            {
                return;
            }

            // Otherwise, set the size based in the last known position
            float x = lastPosition.X + 13;
            float y = lastPosition.Y + 48;
            float width = lastWidth - leftDifference - rightDifference;
            const float height = 9;
            opacityBackground.Position = new PointF(x, y);
            opacityBackground.Size = new SizeF(width, height);
            opacityForeground.Position = new PointF(x, y);
            opacityForeground.Size = new SizeF(width * (Opacity * 0.01f), height);
        }

        /// <summary>
        /// Recalculates the Color panel with the last known Position and Width.
        /// </summary>
        private void Recalculate() => Recalculate(lastPosition, lastWidth);

        #endregion

        #region Functions

        /// <inheritdoc/>
        public IEnumerator<NativeColorData> GetEnumerator() => Colors.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Moves to the Previous Color.
        /// </summary>
        public void Previous()
        {
            // If there are no items, return
            if (Colors.Count == 0)
            {
                return;
            }

            // If we are on the first item, go back to the last one
            if (SelectedIndex == 0)
            {
                SelectedIndex = Colors.Count - 1;
            }
            // Otherwise, reduce it by one
            else
            {
                SelectedIndex -= 1;
            }
        }

        /// <summary>
        /// Moves to the Next Color.
        /// </summary>
        public void Next()
        {
            // If there are no items, return
            if (Colors.Count == 0)
            {
                return;
            }

            // If we are on the last item, go back to the first one
            if (Colors.Count - 1 == SelectedIndex)
            {
                SelectedIndex = 0;
            }
            // Otherwise, increase it by one
            else
            {
                SelectedIndex += 1;
            }
        }

        /// <summary>
        /// Adds a color to the Panel.
        /// </summary>
        /// <param name="color">The color to add.</param>
        public void Add(NativeColorData color)
        {
            if (Colors.Contains(color))
            {
                throw new ArgumentException("Color is already part of the Panel.", nameof(color));
            }

            Colors.Add(color);
            Recalculate();
        }

        /// <summary>
        /// Removes a color from the panel.
        /// </summary>
        /// <param name="color">The color to remove.</param>
        public void Remove(NativeColorData color)
        {
            // Remove it if there
            // If not, ignore it
            Colors.Remove(color);

            // If the index is higher or equal than the max number of items
            // Set the max - 1
            if (SelectedIndex >= Colors.Count)
            {
                SelectedIndex = Colors.Count - 1;
            }
            else
            {
                UpdateItems();
            }
        }

        /// <summary>
        /// Removes all of the 
        /// </summary>
        /// <param name="func"></param>
        public void Remove(Func<NativeColorData, bool> func)
        {
            for (int i = 0; i < Colors.Count; i++)
            {
                NativeColorData color = Colors[i];

                if (func(color))
                {
                    Colors.Remove(color);
                }
            }

            Recalculate();
        }

        /// <summary>
        /// Removes all of the colors from the Panel.
        /// </summary>
        public void Clear()
        {
            Colors.Clear();
            Recalculate();
        }

        /// <summary>
        /// Checks if the Color Data is present on this Panel.
        /// </summary>
        /// <param name="color">The Color Data to check.</param>
        public void Contains(NativeColorData color) => Colors.Contains(color);

        /// <summary>
        /// Recalculates the position of the Color Panel.
        /// </summary>
        /// <param name="position">The position of the panel.</param>
        /// <param name="width">The width of the menu.</param>
        public override void Recalculate(PointF position, float width)
        {
            // Save the last position and width
            lastPosition = position;
            lastWidth = width;

            // Select the correct extra distance based on the prescence of the Opacity toggle
            float extra = ShowOpacity ? 78 : 0;

            // Set the position and size of the Background
            Background.Position = position;
            Background.Size = new SizeF(width, ShowOpacity ? 188 : 111);
            // And then set the position of the text
            title.Position = new PointF(position.X + (width * 0.5f), position.Y + extra + 10f);
            // Then, set the position of the opacity bar and texts
            UpdateOpacityBar();
            opacityText.Position = new PointF(position.X + (width * 0.5f), position.Y + 10f);
            percentMin.Position = new PointF(position.X + 9, position.Y + 11);
            percentMax.Position = new PointF(position.X + width - 60, position.Y + 11);

            // Finally, update the list of items
            UpdateItems();
        }

        /// <summary>
        /// Draws the Color Panel.
        /// </summary>
        public override void Process()
        {
            // If the user pressed one of the keys, move to the left or right
#if ALTV
            if (Controls.IsJustPressed(Control.FrontendLT))
#else
            if (Controls.IsJustPressed(Control.FrontendLt))
#endif
            {
                Previous();
            }
#if ALTV
            else if (Controls.IsJustPressed(Control.FrontendRT))
#else
            else if (Controls.IsJustPressed(Control.FrontendRt))
#endif
            {
                Next();
            }
            // If the user pressed one of the bumpers with the Opacity bar enabled, increase or decrease it
#if ALTV
            else if (ShowOpacity && Controls.IsJustPressed(Control.FrontendLB))
#else
            else if (ShowOpacity && Controls.IsJustPressed(Control.FrontendLb))
#endif
            {
                if (Opacity > 0)
                {
                    Opacity--;
                    Sound?.PlayFrontend();
                }
            }
#if ALTV
            else if (ShowOpacity && Controls.IsJustPressed(Control.FrontendRB))
#else
            else if (ShowOpacity && Controls.IsJustPressed(Control.FrontendRb))
#endif
            {
                if (Opacity < 100)
                {
                    Opacity++;
                    Sound?.PlayFrontend();
                }
            }

            // Draw the items
            base.Process();
            title.Draw();

            foreach (NativeColorData color in visibleItems)
            {
                color.rectangle.Draw();
            }

            if (Colors.Count != 0)
            {
                selectionRectangle.Draw();
            }

            if (ShowOpacity)
            {
                opacityText.Draw();
                percentMin.Draw();
                percentMax.Draw();
                opacityBackground.Draw();
                opacityForeground.Draw();
            }
        }

        #endregion

    }
}

// Menus/ItemChangedEventHandler.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the method that is called when the selected item is changed on a List Item.
    /// </summary>
    /// <typeparam name="T">The type of item that was changed.</typeparam>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="ItemChangedEventArgs{T}"/> with the information of the selected item.</param>
    public delegate void ItemChangedEventHandler<T>(object sender, ItemChangedEventArgs<T> e);
}

// Menus/ItemOperation.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// The operation performed when the menu items are modified.
    /// </summary>
    public enum ItemOperation
    {
        /// <summary>
        /// The item has been removed.
        /// </summary>
        Removed = -1,
        /// <summary>
        /// The item has been added.
        /// </summary>
        Added = 0,
    }
}

// Menus/ItemActivatedEventHandler.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the method that is called when an item is activated on a menu.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="ItemActivatedArgs"/> with the item information.</param>
    public delegate void ItemActivatedEventHandler(object sender, ItemActivatedArgs e);
}

// Menus/ItemChangedEventArgs.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the change of the selection of an item.
    /// </summary>
    /// <typeparam name="T">The type of object that got changed.</typeparam>
    public class ItemChangedEventArgs<T>
    {
        #region Properties

        /// <summary>
        /// The new object.
        /// </summary>
        public T Object { get; set; }
        /// <summary>
        /// The index of the object.
        /// </summary>
        public int Index { get; }
        /// <summary>
        /// The direction of the Item Changed event.
        /// </summary>
        public Direction Direction { get; }

        #endregion

        #region Constructors

        internal ItemChangedEventArgs(T obj, int index, Direction direction)
        {
            Object = obj;
            Index = index;
            Direction = direction;
        }

        #endregion
    }
}

// Menus/ItemActivatedArgs.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents the arguments of an item activation.
    /// </summary>
    public class ItemActivatedArgs
    {
        #region Properties

        /// <summary>
        /// The item that was just activated.
        /// </summary>
        public NativeItem Item { get; }

        #endregion

        #region Constructors

        internal ItemActivatedArgs(NativeItem item)
        {
            Item = item;
        }

        #endregion
    }
}

// Menus/NativeListItem{T}.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// An item that allows you to scroll between a set of objects.
    /// </summary>
    public class NativeListItem<T> : NativeListItem, IEnumerable<T>
    {
        #region Fields

        private int index = 0;
        private List<T> items = new List<T>();

        #endregion

        #region Properties

        /// <summary>
        /// The index of the currently selected index.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                if (items.Count == 0)
                {
                    return -1;
                }
                return index;
            }
            set
            {
                if (items.Count == 0)
                {
                    throw new InvalidOperationException("There are no available items.");
                }
                if (value < 0)
                {
                    throw new InvalidOperationException("The index is under zero.");
                }
                if (value >= Items.Count)
                {
                    throw new InvalidOperationException($"The index is over the limit of {Items.Count - 1}");
                }
                if (index == value)
                {
                    return;
                }
                index = value;
                TriggerEvent(value, Direction.Unknown);
                UpdateIndex();
            }
        }
        /// <summary>
        /// The currently selected item.
        /// </summary>
        public T SelectedItem
        {
            get
            {
                return Items.Count == 0 ? default : Items[index];
            }
            set
            {
                if (Items.Count == 0)
                {
                    return;
                }

                int newIndex = Items.IndexOf(value);

                if (newIndex == -1)
                {
                    throw new InvalidOperationException("The object is not the list of Items.");
                }

                SelectedIndex = newIndex;
            }
        }

        /// <summary>
        /// The objects used by this item.
        /// </summary>
        public List<T> Items
        {
            get => items;
            set
            {
                items = value;
                UpdateIndex();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the selected item is changed.
        /// </summary>
        public event ItemChangedEventHandler<T> ItemChanged;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="NativeListItem"/>.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="objs">The objects that are available on the Item.</param>
        public NativeListItem(string title, params T[] objs) : this(title, string.Empty, objs)
        {
        }
        /// <summary>
        /// Creates a new <see cref="NativeListItem"/>.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="subtitle">The subtitle of the Item.</param>
        /// <param name="objs">The objects that are available on the Item.</param>
        public NativeListItem(string title, string subtitle, params T[] objs) : base(title, subtitle)
        {
            items = new List<T>();
            items.AddRange(objs);
            UpdateIndex();
        }

        #endregion

        #region Tools

        /// <summary>
        /// Triggers the <seealso cref="ItemChangedEventHandler{T}"/> event.
        /// </summary>
        private void TriggerEvent(int index, Direction direction)
        {
            ItemChanged?.Invoke(this, new ItemChangedEventArgs<T>(items[index], index, direction));
        }
        private void FixIndexIfRequired()
        {
            if (items.Count == 0)
            {
                index = 0;
                UpdateIndex();
            }
            else if (index >= items.Count)
            {
                index = items.Count - 1;
                UpdateIndex();
            }
        }
        /// <summary>
        /// Updates the currently selected item based on the index.
        /// </summary>
        private void UpdateIndex()
        {
            text.Text = SelectedItem == null ? string.Empty : SelectedItem.ToString();

            text.Position = new PointF(RightArrow.Position.X - text.Width + 3, text.Position.Y);
            LeftArrow.Position = new PointF(text.Position.X - LeftArrow.Size.Width, LeftArrow.Position.Y);
        }

        #endregion

        #region Functions

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Adds a <typeparamref name="T" /> into this item.
        /// </summary>
        /// <param name="item">The <typeparamref name="T" /> to add.</param>
        public void Add(T item) => Add(items.Count, item);
        /// <summary>
        /// Adds a <typeparamref name="T" /> in a specific location.
        /// </summary>
        /// <param name="position">The position where the item should be added.</param>
        /// <param name="item">The <typeparamref name="T" /> to add.</param>
        public void Add(int position, T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (position < 0 || position > Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "The position is out of the range of items.");
            }

            items.Insert(position, item);

            FixIndexIfRequired();

            if (items.Count == 1)
            {
                UpdateIndex();
            }
        }
        /// <summary>
        /// Removes a specific <typeparamref name="T" />.
        /// </summary>
        /// <param name="item">The <typeparamref name="T" /> to remove.</param>
        public void Remove(T item)
        {
            if (items.Remove(item))
            {
                FixIndexIfRequired();
            }
        }
        /// <summary>
        /// Removes a <typeparamref name="T" /> at a specific location.
        /// </summary>
        /// <param name="position">The position of the <typeparamref name="T" />.</param>
        public void RemoveAt(int position)
        {
            if (position >= items.Count)
            {
                return;
            }

            items.RemoveAt(position);
            FixIndexIfRequired();
            UpdateIndex();
        }
        /// <summary>
        /// Removes all of the items that match the <paramref name="pred"/>.
        /// </summary>
        /// <param name="pred">The function to use as a check.</param>
        public void Remove(Func<T, bool> pred)
        {
            if (items.RemoveAll(pred.Invoke) > 0)
            {
                FixIndexIfRequired();
            }
        }
        /// <summary>
        /// Removes all of the <typeparamref name="T" /> from this item.
        /// </summary>
        public void Clear()
        {
            items.Clear();

            UpdateIndex();
        }
        /// <summary>
        /// Recalculates the item positions and sizes with the specified values.
        /// </summary>
        /// <param name="pos">The position of the item.</param>
        /// <param name="size">The size of the item.</param>
        /// <param name="selected">If this item has been selected.</param>
        public override void Recalculate(PointF pos, SizeF size, bool selected)
        {
            base.Recalculate(pos, size, selected);

            text.Position = new PointF(pos.X + size.Width - RightArrow.Size.Width - 1 - text.Width, pos.Y + 3);
            LeftArrow.Position = new PointF(text.Position.X - LeftArrow.Size.Width, pos.Y + 4);
        }
        /// <summary>
        /// Moves to the previous item.
        /// </summary>
        public override void GoLeft()
        {
            if (Items.Count == 0)
            {
                return;
            }

            if (index == 0)
            {
                index = Items.Count - 1;
            }
            else
            {
                index--;
            }

            TriggerEvent(index, Direction.Left);
            UpdateIndex();
        }
        /// <summary>
        /// Moves to the next item.
        /// </summary>
        public override void GoRight()
        {
            if (Items.Count == 0)
            {
                return;
            }

            if (index == Items.Count - 1)
            {
                index = 0;
            }
            else
            {
                index++;
            }

            TriggerEvent(index, Direction.Right);
            UpdateIndex();
        }
        /// <summary>
        /// Draws the List on the screen.
        /// </summary>
        public override void Draw()
        {
            base.Draw(); // Arrows, Title and Left Badge
            text.Draw();
        }
        /// <inheritdoc/>
        public override void UpdateColors()
        {
            base.UpdateColors();

            if (!Enabled)
            {
                text.Color = Colors.TitleDisabled;
            }
            else if (lastSelected)
            {
                text.Color = Colors.TitleHovered;
            }
            else
            {
                text.Color = Colors.TitleNormal;
            }
        }

        #endregion
    }
}

// Menus/NativePanel.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents a panel shown under the description of the item description.
    /// </summary>
    public abstract class NativePanel
    {
        #region Properties

        /// <summary>
        /// If this panel is visible to the user.
        /// </summary>
        public virtual bool Visible { get; set; } = true;
        /// <summary>
        /// If the item has controls that can be clicked.
        /// </summary>
        public virtual bool Clickable { get; } = false;
        /// <summary>
        /// The Background of the panel itself.
        /// </summary>
        public ScaledTexture Background { get; } = new ScaledTexture("commonmenu", "gradient_bgd");

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the menu contents.
        /// </summary>
        /// <param name="position">The position of the panel.</param>
        /// <param name="width">The width of the menu.</param>
        public virtual void Recalculate(PointF position, float width)
        {
            Background.Position = position;
        }
        /// <summary>
        /// Processes and Draws the panel.
        /// </summary>
        public virtual void Process()
        {
            Background.Draw();
        }

        #endregion
    }
}

// Menus/NativeSlidableItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Basic elements for a slidable item.
    /// </summary>
    public abstract class NativeSlidableItem : NativeItem
    {
        #region Fields

        /// <summary>
        /// The arrow pointing to the Left.
        /// </summary>
        [Obsolete("arrowLeft is Obsolete, use LeftArrow instead.")]
        internal protected ScaledTexture arrowLeft = null;
        /// <summary>
        /// The arrow pointing to the Right.
        /// </summary>
        [Obsolete("arrowRight is Obsolete, use RightArrow instead.")]
        internal protected ScaledTexture arrowRight = null;

        private bool alwaysVisible = false;

        #endregion

        #region Properties

        /// <summary>
        /// The arrow pointing to the Left.
        /// </summary>
        public ScaledTexture LeftArrow { get; }
        /// <summary>
        /// The arrow pointing to the Right.
        /// </summary>
        public ScaledTexture RightArrow { get; }
        /// <summary>
        /// Whether the arrows should always be shown regardless of the visibility of the Item.
        /// </summary>
        public bool ArrowsAlwaysVisible
        {
            get => alwaysVisible;
            set
            {
                alwaysVisible = value;
                Recalculate();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new item that can be sliden.
        /// </summary>
        /// <param name="title">The title of the Item.</param>
        /// <param name="description">The description of the Item.</param>
        public NativeSlidableItem(string title, string description) : base(title, description)
        {
            LeftArrow = new ScaledTexture(PointF.Empty, SizeF.Empty, "commonmenu", "arrowleft");
            RightArrow = new ScaledTexture(PointF.Empty, SizeF.Empty, "commonmenu", "arrowright");
#pragma warning disable CS0618
            arrowLeft = LeftArrow;
            arrowRight = RightArrow;
#pragma warning restore CS0618
            EnabledChanged += NativeSlidableItem_EnabledChanged;
        }

        #endregion

        #region Tools

        private void NativeSlidableItem_EnabledChanged(object sender, EventArgs e) => Recalculate();

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the item positions and sizes with the specified values.
        /// </summary>
        /// <param name="pos">The position of the item.</param>
        /// <param name="size">The size of the item.</param>
        /// <param name="selected">If this item has been selected.</param>
        public override void Recalculate(PointF pos, SizeF size, bool selected)
        {
            base.Recalculate(pos, size, selected);

            LeftArrow.Size = (selected && Enabled) || ArrowsAlwaysVisible ? new SizeF(30, 30) : SizeF.Empty;
            RightArrow.Size = (selected && Enabled) || ArrowsAlwaysVisible ? new SizeF(30, 30) : SizeF.Empty;

            RightArrow.Position = new PointF(pos.X + size.Width - RightArrow.Size.Width - 5, pos.Y + 4);
        }
        /// <summary>
        /// Moves to the previous item.
        /// </summary>
        public abstract void GoLeft();
        /// <summary>
        /// Moves to the next item.
        /// </summary>
        public abstract void GoRight();
        /// <summary>
        /// Draws the left and right arrow.
        /// </summary>
        public override void Draw()
        {
            if (UseCustomBackground)
            {
                background.Draw();
            }

            title.Draw();
            badgeLeft?.Draw();
            LeftArrow.Draw();
            RightArrow.Draw();
        }
        /// <inheritdoc/>
        public override void UpdateColors()
        {
            base.UpdateColors();

            if (!Enabled)
            {
                LeftArrow.Color = Colors.ArrowsDisabled;
                RightArrow.Color = Colors.ArrowsDisabled;
            }
            else if (lastSelected)
            {
                LeftArrow.Color = Colors.ArrowsHovered;
                RightArrow.Color = Colors.ArrowsHovered;
            }
            else
            {
                LeftArrow.Color = Colors.ArrowsNormal;
                RightArrow.Color = Colors.ArrowsNormal;
            }
        }

        #endregion
    }
}

// Menus/NativeStatsPanel.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Represents a Statistics panel.
    /// </summary>
    public class NativeStatsPanel : NativePanel, IContainer<NativeStatsInfo>, IEnumerable<NativeStatsInfo>
    {
        #region Fields

        private readonly List<NativeStatsInfo> fields = new List<NativeStatsInfo>();
        private PointF lastPosition = PointF.Empty;
        private float lastWidth = 0;
        private Color backgroundColor = Color.FromArgb(255, 169, 169, 169);
        private Color foregroundColor = Color.FromArgb(255, 255, 255, 255);

        #endregion

        #region Properties

        /// <summary>
        /// The color of the background of the bars.
        /// </summary>
        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                backgroundColor = value;

                foreach (NativeStatsInfo field in fields)
                {
                    field.SetColor(value, foregroundColor);
                }
            }
        }
        /// <summary>
        /// The color of the foreground of the bars.
        /// </summary>
        public Color ForegroundColor
        {
            get => foregroundColor;
            set
            {
                foregroundColor = value;

                foreach (NativeStatsInfo field in fields)
                {
                    field.SetColor(backgroundColor, value);
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Stats Panel.
        /// </summary>
        /// <param name="stats">The Statistics to add.</param>
        public NativeStatsPanel(params NativeStatsInfo[] stats)
        {
            foreach (NativeStatsInfo field in stats)
            {
                Add(field);
            }
        }

        #endregion

        #region Functions

        /// <inheritdoc/>
        public IEnumerator<NativeStatsInfo> GetEnumerator() => fields.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Adds a stat to the player field.
        /// </summary>
        /// <param name="field">The Field to add.</param>
        public void Add(NativeStatsInfo field)
        {
            if (fields.Contains(field))
            {
                throw new InvalidOperationException("Stat is already part of the panel.");
            }
            fields.Add(field);
            field.SetColor(backgroundColor, foregroundColor);
        }
        /// <summary>
        /// Removes a field from the panel.
        /// </summary>
        /// <param name="field">The field to remove.</param>
        public void Remove(NativeStatsInfo field)
        {
            if (!fields.Contains(field))
            {
                return;
            }
            fields.Remove(field);
            Recalculate();
        }
        /// <summary>
        /// Removes the items that match the function.
        /// </summary>
        /// <param name="func">The function used to match items.</param>
        public void Remove(Func<NativeStatsInfo, bool> func)
        {
            foreach (NativeStatsInfo item in new List<NativeStatsInfo>(fields))
            {
                if (func(item))
                {
                    Remove(item);
                }
            }
        }
        /// <summary>
        /// Removes all of the Stats fields.
        /// </summary>
        public void Clear()
        {
            fields.Clear();
            Recalculate();
        }
        /// <summary>
        /// Checks if the field is part of the Stats Panel.
        /// </summary>
        /// <param name="field">The field to check.</param>
        /// <returns><see langword="true"/> if the item is part of the Panel, <see langword="false"/> otherwise.</returns>
        public bool Contains(NativeStatsInfo field) => fields.Contains(field);
        /// <summary>
        /// Recalculates the Stats panel with the last known Position and Width.
        /// </summary>
        public void Recalculate() => Recalculate(lastPosition, lastWidth);
        /// <summary>
        /// Recalculates the position of the Stats panel.
        /// </summary>
        /// <param name="position">The new position of the Stats Panel.</param>
        /// <param name="width">The width of the menu.</param>
        public override void Recalculate(PointF position, float width)
        {
            base.Recalculate(position, width);

            const float differenceTop = 7;
            const float perStatSeparation = 38;

            Background.Size = new SizeF(width, differenceTop + (perStatSeparation * fields.Count));

            for (int i = 0; i < fields.Count; i++)
            {
                PointF fieldPosition = new PointF(position.X, position.Y + differenceTop + (perStatSeparation * i));
                fields[i].Recalculate(fieldPosition, width);
            }
        }
        /// <summary>
        /// Processes the Stats Panel.
        /// </summary>
        public override void Process()
        {
            base.Process();

            foreach (NativeStatsInfo info in fields)
            {
                info.Draw();
            }
        }

        #endregion
    }
}

// Menus/NativeSubmenuItem.cs
namespace LemonUI.Menus
{
    /// <summary>
    /// Item used for opening submenus.
    /// </summary>
    public class NativeSubmenuItem : NativeItem
    {
        #region Properties

        /// <summary>
        /// The menu opened by this item.
        /// </summary>
        public NativeMenu Menu { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Item that opens a Submenu.
        /// </summary>
        /// <param name="menu">The menu that this item will open.</param>
        /// <param name="parent">The parent menu where this item will be located.</param>
        public NativeSubmenuItem(NativeMenu menu, NativeMenu parent) : this(menu, parent, ">>>")
        {
        }
        /// <summary>
        /// Creates a new Item that opens a Submenu.
        /// </summary>
        /// <param name="menu">The menu that this item will open.</param>
        /// <param name="parent">The parent menu where this item will be located.</param>
        /// <param name="endLabel">The alternative title of the item, shown on the right.</param>
        public NativeSubmenuItem(NativeMenu menu, NativeMenu parent, string endLabel) : base(menu.Name, menu.Description, endLabel)
        {
            Menu = menu ?? throw new ArgumentNullException(nameof(menu));
            Menu.Parent = parent ?? throw new ArgumentNullException(nameof(parent));

            Activated += NativeSubmenuItem_Activated;
        }

        #endregion

        #region Event Functions

        private void NativeSubmenuItem_Activated(object sender, EventArgs e)
        {
            Menu.Parent.Visible = false;

            if (!Menu.Parent.Visible)
            {
                Menu.Visible = true;
            }
        }

        #endregion

        #region Functions

        /// <inheritdoc/>
        public override void Draw()
        {
            if (Description != Menu.Description)
            {
                Description = Menu.Description;
            }

            base.Draw();
        }

        #endregion
    }
}

// TimerBars/TimerBarObjective.cs
namespace LemonUI.TimerBars
{
    /// <summary>
    /// A timer bar for a specific amount of objectives.
    /// </summary>
    public class TimerBarObjective : TimerBar
    {
        #region Fields

        private const float width = 20;
        private const float height = 20;

        private static readonly Color colorWhite = Color.FromArgb(255, 255, 255);
        private static readonly Color colorCompleted = Color.FromArgb(101, 180, 212);

        private readonly List<ScaledTexture> objectives = new List<ScaledTexture>();

        private PointF lastPosition = default;

        private int count = 1;
        private int completed = 0;
        private Color colorSet = colorCompleted;
        private ObjectiveSpacing objectiveSpacing = ObjectiveSpacing.Equal;

        #endregion

        #region Properties

        /// <summary>
        /// The number of objectives shown in the timer bar.
        /// </summary>
        public int Count
        {
            get => count;
            set
            {
                if (count == value)
                {
                    return;
                }

                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The number of objectives can't be under zero.");
                }

                count = value;
                UpdateObjectiveCount();
            }
        }
        /// <summary>
        /// The number of completed objectives.
        /// </summary>
        public int Completed
        {
            get => completed;
            set
            {
                if (completed == value)
                {
                    return;
                }

                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The number of completed objectives can't be under zero.");
                }
                if (value > Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The number of completed objectives can't be over the total number of objectives.");
                }

                completed = value;

                UpdateObjectiveColors();
            }
        }
        /// <summary>
        /// The color used for completed objectives.
        /// </summary>
        public Color CompletedColor
        {
            get => colorSet;
            set
            {
                if (colorSet == value)
                {
                    return;
                }

                colorSet = value;
                UpdateObjectiveColors();
            }
        }
        /// <summary>
        /// The type of spacing between the objectives .
        /// </summary>
        public ObjectiveSpacing Spacing
        {
            get => objectiveSpacing;
            set
            {
                if (objectiveSpacing == value)
                {
                    return;
                }

                objectiveSpacing = value;
                Recalculate(lastPosition);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new timer bar used to show objectives.
        /// </summary>
        /// <param name="title">The title of the objective bar.</param>
        public TimerBarObjective(string title) : base(title, string.Empty)
        {
            UpdateObjectiveCount();
        }

        #endregion

        #region Tools

        private void UpdateObjectiveCount()
        {
            // just to make sure
            if (completed > count)
            {
                completed = count;
            }

            objectives.Clear();

            for (int i = 0; i < count; i++)
            {
                objectives.Add(new ScaledTexture("timerbars", "circle_checkpoints"));
            }

            UpdateObjectiveColors();
        }
        private void UpdateObjectiveColors()
        {
            for (int i = 0; i < objectives.Count; i++)
            {
                ScaledTexture texture = objectives[i];
                texture.Color = i < completed ? colorSet : colorWhite;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Draws the objective timer bar.
        /// </summary>
        public override void Draw()
        {
            background.Draw();
            title.Draw();

            foreach (ScaledTexture texture in objectives)
            {
                texture.Draw();
            }
        }
        /// <inheritdoc/>
        public override void Recalculate(PointF pos)
        {
            lastPosition = pos;

            base.Recalculate(pos);

            const float safe = width + 5;
            float startY = pos.Y + (backgroundHeight * 0.5f) - (height * 0.5f);

            switch (objectiveSpacing)
            {
                case ObjectiveSpacing.Equal:
                {
                    const float half = backgroundWidth * 0.5f;
                    float startX = pos.X + half;
                    float spacingWidth = (half - safe) / (objectives.Count - 1);

                    for (int i = 0; i < objectives.Count; i++)
                    {
                        ScaledTexture texture = objectives[i];
                        texture.Size = new SizeF(width, height);
                        texture.Position = new PointF(startX + (spacingWidth * i), startY);
                    }

                    break;
                }
                case ObjectiveSpacing.Fixed:
                {
                    float startX = pos.X + backgroundWidth - safe - (width * (count - 1));

                    for (int i = 0; i < objectives.Count; i++)
                    {
                        ScaledTexture texture = objectives[i];
                        texture.Size = new SizeF(width, height);
                        texture.Position = new PointF(startX + (i * width), startY);
                    }

                    break;
                }
            }
        }

        #endregion
    }
}

// TimerBars/TimerBar.cs
namespace LemonUI.TimerBars
{
    /// <summary>
    /// Represents a Bar with text information shown in the bottom right.
    /// </summary>
    public class TimerBar : IDrawable
    {
        #region Fields

        /// <summary>
        /// The separation between the different timer bars.
        /// </summary>
        internal const float separation = 6.25f;
        /// <summary>
        /// The width of the background.
        /// </summary>
        internal const float backgroundWidth = 220;
        /// <summary>
        /// The height of the background.
        /// </summary>
        internal const float backgroundHeight = 37;

        /// <summary>
        /// The background of the timer bar.
        /// </summary>
        protected internal readonly ScaledTexture background = new ScaledTexture("timerbars", "all_black_bg")
        {
            Color = Color.FromArgb(160, 255, 255, 255)
        };
        /// <summary>
        /// The title of the timer bar.
        /// </summary>
        protected internal readonly ScaledText title = new ScaledText(PointF.Empty, string.Empty, 0.29f)
        {
            Alignment = Alignment.Right,
            WordWrap = 1000
        };
        /// <summary>
        /// The information of the Timer Bar.
        /// </summary>
        protected internal readonly ScaledText info = new ScaledText(PointF.Empty, string.Empty, 0.5f)
        {
            Alignment = Alignment.Right,
            WordWrap = 1000
        };

        private string rawTitle = string.Empty;
        private string rawInfo = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The title of the bar, shown on the left.
        /// </summary>
        public string Title
        {
            get => rawTitle;
            set
            {
                rawTitle = value ?? throw new ArgumentNullException(nameof(value));
                title.Text = value.ToUpperInvariant();
            }
        }
        /// <summary>
        /// The information shown on the right.
        /// </summary>
        public string Info
        {
            get => rawInfo;
            set
            {
                rawInfo = value ?? throw new ArgumentNullException(nameof(value));
                info.Text = value.ToUpperInvariant();
            }
        }
        /// <summary>
        /// The Width of the information text.
        /// </summary>
        public float InfoWidth => info.Width;
        /// <summary>
        /// The color of the information text.
        /// </summary>
        public Color Color
        {
            get => info.Color;
            set => info.Color = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="TimerBar"/> with the specified Title and Value.
        /// </summary>
        /// <param name="title">The title of the bar.</param>
        /// <param name="info">The information shown on the bar.</param>
        public TimerBar(string title, string info)
        {
            Title = title;
            Info = info;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the position of the timer bar elements based on the location of it on the screen.
        /// </summary>
        /// <param name="pos">The Top Left position of the Timer Bar.</param>
        public virtual void Recalculate(PointF pos)
        {
            background.Position = pos;
            background.Size = new SizeF(backgroundWidth, backgroundHeight);
            title.Position = new PointF(pos.X + 91, pos.Y + 8);
            info.Position = new PointF(pos.X + 218, pos.Y - 3);
        }
        /// <summary>
        /// Draws the timer bar information.
        /// </summary>
        public virtual void Draw()
        {
            background.Draw();
            title.Draw();
            info.Draw();
        }

        #endregion
    }
}

// TimerBars/TimerBarCollection.cs
namespace LemonUI.TimerBars
{
    /// <summary>
    /// A collection or Set of <see cref="TimerBar"/>.
    /// </summary>
    public class TimerBarCollection : IContainer<TimerBar>
    {
        #region Fields

        private PointF offset = PointF.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// If this collection of Timer Bars is visible to the user.
        /// </summary>
        public bool Visible { get; set; } = true;
        /// <summary>
        /// The <see cref="TimerBar"/>s that are part of this collection.
        /// </summary>
        public List<TimerBar> TimerBars { get; } = new List<TimerBar>();
        /// <summary>
        /// The offset from it's starting point on the bottom right.
        /// </summary>
        public PointF Offset
        {
            get => offset;
            set
            {
                offset = value;
                Recalculate();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new collection of Timer Bars.
        /// </summary>
        /// <param name="bars"></param>
        public TimerBarCollection(params TimerBar[] bars)
        {
            TimerBars.AddRange(bars);
            Recalculate();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds a <see cref="TimerBar"/> onto this collection.
        /// </summary>
        /// <param name="bar">The <see cref="TimerBar"/> to add.</param>
        public void Add(TimerBar bar)
        {
            // If the item is already on the list, raise an exception
            if (TimerBars.Contains(bar))
            {
                throw new InvalidOperationException("The item is already part of the menu.");
            }
            // Also raise an exception if is null
            if (bar == null)
            {
                throw new ArgumentNullException(nameof(bar));
            }
            // If we got here, add it
            TimerBars.Add(bar);
            // And recalculate the positions of the existing items
            Recalculate();
        }
        /// <summary>
        /// Removes a <see cref="TimerBar"/> from the Collection.
        /// </summary>
        /// <param name="bar">The <see cref="TimerBar"/> to remove.</param>
        public void Remove(TimerBar bar)
        {
            // If the bar is not present, return
            if (!TimerBars.Contains(bar))
            {
                return;
            }

            // Otherwise, remove it
            TimerBars.Remove(bar);
            // And recalculate the positions
            Recalculate();
        }
        /// <summary>
        /// Removes all of the <see cref="TimerBar"/> that match the function.
        /// </summary>
        /// <param name="func">The function to check the <see cref="TimerBar"/>.</param>
        public void Remove(Func<TimerBar, bool> func)
        {
            for (int i = 0; i < TimerBars.Count; i++)
            {
                TimerBar bar = TimerBars[i];

                if (func(bar))
                {
                    TimerBars.Remove(bar);
                }
            }
            // Finally, recalculate the positions
            Recalculate();
        }
        /// <summary>
        /// Removes all of the <see cref="TimerBar"/> in this collection.
        /// </summary>
        public void Clear() => TimerBars.Clear();
        /// <summary>
        /// Checks if the <see cref="TimerBar"/> is part of this collection.
        /// </summary>
        /// <param name="bar">The <see cref="TimerBar"/> to check.</param>
        public bool Contains(TimerBar bar) => TimerBars.Contains(bar);
        /// <summary>
        /// Recalculates the positions and sizes of the <see cref="TimerBar"/>.
        /// </summary>
        public void Recalculate()
        {
            PointF pos = SafeZone.BottomRight;

            pos.X += offset.X;
            pos.Y += offset.Y;

            int count = 0;

            for (int i = 0; i < TimerBars.Count; i++)
            {
                TimerBars[i].Recalculate(new PointF(pos.X - TimerBar.backgroundWidth, pos.Y - (TimerBar.backgroundHeight * (TimerBars.Count - count)) - (TimerBar.separation * (TimerBars.Count - count - 1))));
                count++;
            }
        }
        /// <summary>
        /// Draws the known timer bars.
        /// </summary>
        public void Process()
        {
            // If there are no timer bars or the collection is disabled, return
            if (TimerBars.Count == 0 || !Visible)
            {
                return;
            }

            // Hide the texts in the bottom right corner of the screen
#if FIVEM
            CitizenFX.Core.UI.Screen.Hud.HideComponentThisFrame(HudComponent.AreaName);
            CitizenFX.Core.UI.Screen.Hud.HideComponentThisFrame(HudComponent.StreetName);
            CitizenFX.Core.UI.Screen.Hud.HideComponentThisFrame(HudComponent.VehicleName);
#elif RAGEMP
            Invoker.Invoke(Natives.HideHudComponentThisFrame, HudComponent.AreaName);
            Invoker.Invoke(Natives.HideHudComponentThisFrame, HudComponent.StreetName);
            Invoker.Invoke(Natives.HideHudComponentThisFrame, HudComponent.VehicleName);
#elif RPH
            NativeFunction.CallByHash<int>(0x6806C51AD12B83B8, 7);
            NativeFunction.CallByHash<int>(0x6806C51AD12B83B8, 9);
            NativeFunction.CallByHash<int>(0x6806C51AD12B83B8, 6);
#elif ALTV
            Alt.Natives.HideHudComponentThisFrame(7);
            Alt.Natives.HideHudComponentThisFrame(9);
            Alt.Natives.HideHudComponentThisFrame(6);
#elif SHVDN3 || SHVDNC
            Hud.HideComponentThisFrame(HudComponent.AreaName);
            Hud.HideComponentThisFrame(HudComponent.StreetName);
            Hud.HideComponentThisFrame(HudComponent.VehicleName);
#endif
            for (int i = 0; i < TimerBars.Count; i++)
            {
                TimerBars[i].Draw();
            }
        }

        #endregion
    }
}

// TimerBars/ObjectiveSpacing.cs
namespace LemonUI.TimerBars
{
    /// <summary>
    /// The spacing of the objectives in the timer bar.
    /// </summary>
    public enum ObjectiveSpacing
    {
        /// <summary>
        /// The objectives will be equally spaced.
        /// </summary>
        /// <remarks>
        /// If you have way too many objectives and not enough width, you might end up with objectives overlapping each other.
        /// </remarks>
        Equal = 0,
        /// <summary>
        /// The items will all have the same spacing.
        /// </summary>
        /// <remarks>
        /// If you have way too many objectives and not enough width, the objectives might end up outside of the timer bar.
        /// </remarks>
        Fixed = 1
    }
}

// TimerBars/TimerBarProgress.cs
namespace LemonUI.TimerBars
{
    /// <summary>
    /// Represents a Timer Bar that shows the progress of something.
    /// </summary>
    public class TimerBarProgress : TimerBar
    {
        #region Constants

        private const float barWidth = 108;
        private const float barHeight = 15;

        #endregion

        #region Fields

        /// <summary>
        /// The background of the Progress Bar.
        /// </summary>
        protected internal readonly ScaledRectangle barBackground = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(255, 139, 0, 0)
        };
        /// <summary>
        /// The foreground of the Progress Bar.
        /// </summary>
        protected internal readonly ScaledRectangle barForeground = new ScaledRectangle(PointF.Empty, SizeF.Empty)
        {
            Color = Color.FromArgb(255, 255, 0, 0)
        };

        private float progress = 100;

        #endregion

        #region Properties

        /// <summary>
        /// The progress of the bar.
        /// </summary>
        public float Progress
        {
            get => progress;
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                progress = value;
                barForeground.Size = new SizeF(barWidth * (value * 0.01f), barHeight);
            }
        }
        /// <summary>
        /// The Foreground color of the Progress bar.
        /// </summary>
        public Color ForegroundColor
        {
            get => barForeground.Color;
            set => barForeground.Color = value;
        }
        /// <summary>
        /// The Background color of the Progress bar.
        /// </summary>
        public Color BackgroundColor
        {
            get => barBackground.Color;
            set => barBackground.Color = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="TimerBarProgress"/> with the specified title.
        /// </summary>
        /// <param name="title">The title of the bar.</param>
        public TimerBarProgress(string title) : base(title, string.Empty)
        {
        }

        #endregion

        #region Functions

        /// <summary>
        /// Recalculates the position of the timer bar elements based on the location of it on the screen.
        /// </summary>
        /// <param name="pos">The Top Left position of the Timer Bar.</param>
        public override void Recalculate(PointF pos)
        {
            // Recalculate the base elements
            base.Recalculate(pos);
            // And set the size and position of the progress bar
            PointF barPos = new PointF(pos.X + 103, pos.Y + 12);
            barBackground.Position = barPos;
            barBackground.Size = new SizeF(barWidth, barHeight);
            barForeground.Position = barPos;
            barForeground.Size = new SizeF(barWidth * (progress * 0.01f), barHeight);
        }
        /// <summary>
        /// Draws the TimerBar.
        /// </summary>
        public override void Draw()
        {
            background.Draw();
            title.Draw();
            barBackground.Draw();
            barForeground.Draw();
        }

        #endregion
    }
}
