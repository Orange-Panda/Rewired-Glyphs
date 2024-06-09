using Rewired;
using System;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	// INTERNAL NOTE: This file contains wrapper methods that make using InputGlyphs more convenient for the user
	public static partial class InputGlyphs
	{
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user for the most recently used device.
		/// </summary>
		/// <param name="player">
		/// The player to evaluate for. This is relevant since different players are likely using different devices and maps.
		/// </param>
		/// <param name="actionID">
		/// The integer ID for the action to evaluate.
		/// <br/><br/>
		/// Integers are more efficient but consider using the following if you only have access to string names:
		/// <code>int actionID = ReInput.mapping.GetActionId("your_action_string_here");</code>
		/// </param>
		/// <param name="pole">
		/// What is the polarity for the action?
		/// <br/><br/>
		/// Pole.Positive is most common but you can expect Pole.Negative for inputs such as "Move Left" since that would be the negative side of a "Move Horizontal" axis.
		/// </param>
		/// <param name="axisRange">
		/// The axis range that should be utilized in the output glyph.
		/// <br/><br/>
		/// For elements that use axis input (sticks, mouse movement, etc.) we need to know direction of the element we want to display.
		/// <br/><br/>
		/// For example: <see cref="AxisRange.Positive"/> would represent "Joystick Right". <see cref="AxisRange.Negative"/> would represent "Joystick Left", and <see cref="AxisRange.Full"/> would represent "Joystick Horizontal"
		/// </param>
		/// <param name="symbolPreference">
		/// The preferred hardware symbol type to use for Joystick glyphs
		/// </param>
		/// <param name="forceAxis">
		/// Usually the Input Glyph system, when <paramref name="forceAxis"/> is false, only checks for single axis inputs such that it will only look for "Move Left" with <see cref="Pole.Negative"/> and "Move Right" with <see cref="Pole.Positive"/> and thus never checking "Move Horizontal" as an axis itself.
		/// <br/><br/>
		/// When true, explicitly request for the Input Glyph system to evaluate this glyph as the full axis ("Move Horizontal" in the example case) so we can represent it properly for axis inputs.
		/// <br/><br/>
		/// TL;DR: For axis actions: True represents the axis itself "Move Horizontal". False represents negative pole "Move Left" and positive pole "Move Right". In most cases should be false.
		/// </param>
		/// <returns>
		/// A <see cref="Glyph"/> that can be utilized in UI elements to display the element that is associated with this particular action.
		/// <br/><br/>
		/// In cases where there is no element available this may return a "fallback" glyph
		/// </returns>
		/// <remarks>
		/// This method should only be used if you want to explicitly determine the type of symbol (Xbox, Playstation, etc.) to show for the joystick glyph.<br/>
		/// Use <see cref="GetCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,bool)"/> in the cases where you don't want to specify.
		/// </remarks>
		public static Glyph GetSpecificCurrentGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, SymbolPreference symbolPreference, bool forceAxis = false)
		{
			if (!CanRetrieveGlyph)
			{
				axisRange = AxisRange.Full;
				return UninitializedGlyph;
			}

			Controller last = player.controllers.GetMostRecentController();

			// Force a particular mode if the user preferences say so
			switch (PreferredHardware)
			{
				case HardwarePreference.Auto:
					break;
				case HardwarePreference.KeyboardMouse:
					return player.GetKeyboardMouseGlyph(actionID, pole, out axisRange);
				case HardwarePreference.Gamepad:
					return player.GetSpecificJoystickGlyph(actionID, last, pole, out axisRange, symbolPreference);
				default:
					throw new ArgumentOutOfRangeException();
			}

			// Use the mode that matches the last controller used by user.
			if (last != null)
			{
				switch (last.type)
				{
					case ControllerType.Keyboard:
					case ControllerType.Mouse:
						return player.GetKeyboardMouseGlyph(actionID, pole, out axisRange);
					case ControllerType.Joystick:
					case ControllerType.Custom:
						return player.GetSpecificJoystickGlyph(actionID, last, pole, out axisRange, symbolPreference);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			// Use the expected mode for this hardware if there is no controller is active (usually only at the start of the application)
			if (SystemInfo.deviceType == DeviceType.Desktop)
			{
				return player.GetKeyboardMouseGlyph(actionID, pole, out axisRange);
			}
			else
			{
				return player.GetSpecificJoystickGlyph(actionID, null, pole, out axisRange, symbolPreference);
			}
		}

		// Region: Contains wrappers that use global properties as a 'default' parameter for other funtions.

		#region Global Value Wrappers
		/// <inheritdoc cref="GetSpecificCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,SymbolPreference,bool)"/>
		/// <remarks>
		/// Uses the value of <see cref="PreferredSymbols"/> for determining Joystick glyph symbols.
		/// </remarks>
		/// <seealso cref="GetSpecificCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,SymbolPreference,bool)"/>
		public static Glyph GetCurrentGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			return player.GetSpecificCurrentGlyph(actionID, pole, out axisRange, PreferredSymbols, forceAxis);
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user for the `Joystick` device.
		/// </summary>
		/// <remarks>
		/// Uses the value of <see cref="PreferredSymbols"/> for determining Joystick glyph symbols.
		/// </remarks>
		public static Glyph GetJoystickGlyph(this Player player, int actionID, Controller controller, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			return player.GetSpecificJoystickGlyph(actionID, controller, pole, out axisRange, PreferredSymbols, forceAxis);
		}
		#endregion

		// Region: Contains wrappers that branch to call other methods depending on a 'ControllerType' value.

		#region ControllerType Wrappers
		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the <paramref name="controllerType"/>
		/// </summary>
		public static Glyph GetGlyph(this Player player, ControllerType controllerType, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			switch (controllerType)
			{
				case ControllerType.Keyboard:
					return player.GetKeyboardGlyph(actionID, pole, out axisRange, forceAxis);
				case ControllerType.Mouse:
					return player.GetMouseGlyph(actionID, pole, out axisRange, forceAxis);
				case ControllerType.Joystick:
				case ControllerType.Custom:
					return player.GetJoystickGlyph(actionID, player.controllers.GetMostRecentController(controllerType), pole, out axisRange, forceAxis);
				default:
					throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null);
			}
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the <paramref name="controllerType"/>
		/// </summary>
		/// <remarks>
		/// This method should only be used if you want to explicitly determine the type of symbol (Xbox, Playstation, etc.) to show for the joystick glyph.
		/// </remarks>
		public static Glyph GetSpecificGlyph(this Player player, ControllerType controllerType, int actionID, Pole pole, out AxisRange axisRange, SymbolPreference symbolPreference,
			bool forceAxis = false)
		{
			switch (controllerType)
			{
				case ControllerType.Keyboard:
					return player.GetKeyboardGlyph(actionID, pole, out axisRange, forceAxis);
				case ControllerType.Mouse:
					return player.GetMouseGlyph(actionID, pole, out axisRange, forceAxis);
				case ControllerType.Joystick:
				case ControllerType.Custom:
					return player.GetSpecificJoystickGlyph(actionID, player.controllers.GetMostRecentController(controllerType), pole, out axisRange, symbolPreference, forceAxis);
				default:
					throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null);
			}
		}
		#endregion

		// Region: Contains wrappers that get a player through index instead of using the 'Player' type extension methods.

		#region Get Player by Index Wrapper
		/// <inheritdoc cref="InputGlyphs.GetGlyph(Player, ControllerType, int, Pole, out AxisRange, bool)"/>
		public static Glyph GetGlyph(ControllerType controllerType, int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetGlyph(controllerType, actionID, pole, out axisRange, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetCurrentGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange, bool)"/>
		public static Glyph GetCurrentGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetCurrentGlyph(actionID, pole, out axisRange, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="InputGlyphs.GetGlyph(Player, ControllerType, int, Pole, out AxisRange, bool)"/>
		/// <remarks>
		/// This method should only be used if you want to explicitly determine the type of symbol (Xbox, Playstation, etc.) to show for the joystick glyph.
		/// </remarks>
		public static Glyph GetSpecificGlyph(ControllerType controllerType, int actionID, Pole pole, out AxisRange axisRange, SymbolPreference symbolPreference, int playerIndex = 0,
			bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetSpecificGlyph(controllerType, actionID, pole, out axisRange, symbolPreference, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetCurrentGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange, bool)"/>
		/// <remarks>
		/// This method should only be used if you want to explicitly determine the type of symbol (Xbox, Playstation, etc.) to show for the joystick glyph.
		/// </remarks>
		/// <seealso cref="GetCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,bool)"/>
		public static Glyph GetSpecificCurrentGlyph(int actionID, Pole pole, out AxisRange axisRange, SymbolPreference symbolPreference, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetSpecificCurrentGlyph(actionID, pole, out axisRange, symbolPreference, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetKeyboardMouseGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange, bool)"/>
		public static Glyph GetKeyboardMouseGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetKeyboardMouseGlyph(actionID, pole, out axisRange, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetKeyboardGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange, bool)"/>
		public static Glyph GetKeyboardGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetKeyboardGlyph(actionID, pole, out axisRange, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetMouseGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange, bool)"/>
		public static Glyph GetMouseGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetMouseGlyph(actionID, pole, out axisRange, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetJoystickGlyph(Player, int, Controller, Rewired.Pole, out Rewired.AxisRange, bool)"/>
		public static Glyph GetJoystickGlyph(int actionID, Controller controller, Pole pole, out AxisRange axisRange, int playerIndex = 0, bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetJoystickGlyph(actionID, controller, pole, out axisRange, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}

		/// <inheritdoc cref="GetSpecificJoystickGlyph(Player, int, Controller, Rewired.Pole, out Rewired.AxisRange, SymbolPreference, bool)"/>
		public static Glyph GetSpecificJoystickGlyph(int actionID, Controller controller, Pole pole, out AxisRange axisRange, SymbolPreference symbolPreference, int playerIndex = 0,
			bool forceAxis = false)
		{
			if (TryGetPlayer(playerIndex, out Player player))
			{
				return player.GetSpecificJoystickGlyph(actionID, controller, pole, out axisRange, symbolPreference, forceAxis);
			}

			axisRange = AxisRange.Full;
			return UninitializedGlyph;
		}
		#endregion
	}
}