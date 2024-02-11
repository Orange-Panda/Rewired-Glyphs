using JetBrains.Annotations;
using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// The InputGlyphs class contains various methods to easily get <see cref="Sprite"/> and <see cref="string"/> information about user input controls.
	/// </summary>
	/// <remarks>The <see cref="GetCurrentGlyph(int, Pole, out AxisRange, int, bool)"/> function should provide most functionality needed.</remarks>
	[PublicAPI]
	public static class InputGlyphs
	{
		internal static bool glyphsDirty;

		/// <summary>
		/// The type Guid of the Controller Template.
		/// </summary>
		private static readonly Guid GamepadTemplateGuid = new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5");
		private static readonly IList<ControllerTemplateElementTarget> TemplateTargets = new List<ControllerTemplateElementTarget>(2);
		/// <summary>
		/// Automatically generated glyphs for inputs that have no glyph defined anywhere.
		/// </summary>
		private static readonly Dictionary<string, Glyph> FallbackGlyphs = new Dictionary<string, Glyph>();
		/// <summary>
		/// Cache lookup for players 
		/// </summary>
		private static readonly Dictionary<int, Player> Players = new Dictionary<int, Player>();
		/// <summary>
		/// Glyph mapping based on hardware specific glyphs and hardware specific input ids, found via hardware Guid.
		/// </summary>
		private static readonly Dictionary<Guid, Dictionary<int, Glyph>> GuidGlyphMaps = new Dictionary<Guid, Dictionary<int, Glyph>>();
		/// <summary>
		/// Glyph mapping based on hardware specific glyphs and hardware specific input ids, found via hardware type definition.
		/// </summary>
		private static readonly Dictionary<HardwareDefinition, Dictionary<int, Glyph>> HardwareGlyphMaps = new Dictionary<HardwareDefinition, Dictionary<int, Glyph>>();
		/// <summary>
		/// Glyph mapping based on template glyphs and template input ids.
		/// </summary>
		private static readonly Dictionary<SymbolPreference, Dictionary<int, Glyph>> TemplateGlyphMaps = new Dictionary<SymbolPreference, Dictionary<int, Glyph>>();
		/// <summary>
		/// Lookup table that matches hardware ids to what kind of glyphs should be shown.
		/// </summary>
		/// <remarks>
		/// This table is not exhaustive and only contains the most common controller guids.
		/// </remarks>
		private static readonly Dictionary<Guid, HardwareDefinition> ControllerGuids = new Dictionary<Guid, HardwareDefinition>
		{
			{ new Guid("d74a350e-fe8b-4e9e-bbcd-efff16d34115"), HardwareDefinition.Xbox }, // Xbox 360
			{ new Guid("19002688-7406-4f4a-8340-8d25335406c8"), HardwareDefinition.Xbox }, // Xbox One
			{ new Guid("c3ad3cad-c7cf-4ca8-8c2e-e3df8d9960bb"), HardwareDefinition.Playstation2 }, // Playstation 2
			{ new Guid("71dfe6c8-9e81-428f-a58e-c7e664b7fbed"), HardwareDefinition.Playstation3 }, // Playstation 3
			{ new Guid("cd9718bf-a87a-44bc-8716-60a0def28a9f"), HardwareDefinition.Playstation }, // Playstation 4
			{ new Guid("5286706d-19b4-4a45-b635-207ce78d8394"), HardwareDefinition.Playstation }, // Playstation 5
			{ new Guid("521b808c-0248-4526-bc10-f1d16ee76bf1"), HardwareDefinition.NintendoSwitch }, // Joycons (Dual)
			{ new Guid("1fbdd13b-0795-4173-8a95-a2a75de9d204"), HardwareDefinition.NintendoSwitch }, // Joycons (Handheld)
			{ new Guid("7bf3154b-9db8-4d52-950f-cd0eed8a5819"), HardwareDefinition.NintendoSwitch } // Pro controller
		};

		/// <summary>
		/// Glyph representing an action that is invalid.
		/// Most commonly occurs due an invalid action name or action id.
		/// </summary>
		/// <remarks>
		/// Remedied by correcting the glyph display's target value
		/// </remarks>
		public static Glyph NullGlyph { get; private set; } = new Glyph("Null", type: Glyph.Type.Null);
		/// <summary>
		/// Glyph representing an action that does not have an input assigned.
		/// </summary>
		/// <remarks>
		/// Remedied by assigning an input to the action either in the Rewired Input Manager at editor time or a control remapping tool at runtime.
		/// </remarks>
		public static Glyph UnboundGlyph { get; private set; } = new Glyph("Unbound", type: Glyph.Type.Unbound);
		/// <summary>
		/// Glyph representing a query that occured before the InputGlyphs system was ready.
		/// </summary>
		/// <remarks>
		/// Remedied by ensuring the presence of a <see cref="RewiredGlyphManager"/> component on the Rewired Input Manager prefab.<br/><br/>
		/// The rewired glyph system is initialized in Awake.
		/// Depending on script execution order your display may query before the glyph system is ready.
		/// The safest approach is to query in OnEnable or Start.
		/// </remarks>
		public static Glyph UninitializedGlyph { get; private set; } = new Glyph("Uninitialized", type: Glyph.Type.Uninitialized);
		/// <summary>
		/// The preferred hardware (Controller or Mouse/Keyboard) to display in <see cref="GetCurrentGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// </summary>
		/// <seealso cref="SetGlyphPreferences"/>
		public static HardwarePreference PreferredHardware { get; private set; } = HardwarePreference.Auto;
		/// <summary>
		/// The preferred symbols to display for gamepad glyphs.
		/// </summary>
		/// <seealso cref="SetGlyphPreferences"/>
		public static SymbolPreference PreferredSymbols { get; private set; } = SymbolPreference.Auto;
		private static bool CanRetrieveGlyph => Application.isPlaying && ReInput.isReady;

		/// <summary>
		/// An event that is invoked when the output of the input glyph may have changed.
		/// This occurs usually when preferences are changed or the most recently used device by a player has changed.
		/// </summary>
		/// <remarks>
		/// The aforementioned preferences can mutate the output of glyph symbol queries.
		/// As such this event gives the opportunity for others to update the glyph output without having to query the InputGlyphs system every frame. 
		/// </remarks>
		public static event Action RebuildGlyphs = delegate { };

		static InputGlyphs()
		{
			ReInput.InitializedEvent += ReInputOnInitializedEvent;
			ReInput.ShutDownEvent += ReInputOnShutDownEvent;
		}

		private static void ReInputOnInitializedEvent()
		{
			FlushPlayersCache();
			glyphsDirty = true;
		}

		private static void ReInputOnShutDownEvent()
		{
			FlushPlayersCache();
		}

		internal static Controller GetMostRecentController(this Player.ControllerHelper controllers)
		{
			Controller last = controllers.GetLastActiveController();
			if (last != null)
			{
				return last;
			}

			ReInput.ControllerHelper allControllers = ReInput.controllers;
			last = allControllers.GetLastActiveController();
			if (last != null)
			{
				return last;
			}

			if (allControllers.joystickCount > 0)
			{
				return allControllers.Joysticks[0];
			}

			return allControllers.controllerCount > 0 ? allControllers.Controllers[0] : null;
		}

		internal static Controller GetMostRecentController(this Player.ControllerHelper controllers, ControllerType controllerType)
		{
			Controller last = controllers.GetLastActiveController(controllerType);
			if (last != null)
			{
				return last;
			}

			ReInput.ControllerHelper allControllers = ReInput.controllers;
			last = allControllers.GetLastActiveController(controllerType);
			if (last != null)
			{
				return last;
			}

			switch (controllerType)
			{
				case ControllerType.Keyboard:
					return allControllers.Keyboard;
				case ControllerType.Mouse:
					return allControllers.Mouse;
				case ControllerType.Joystick:
					return allControllers.joystickCount > 0 ? allControllers.Joysticks[0] : null;
				case ControllerType.Custom:
					return allControllers.customControllerCount > 0 ? allControllers.CustomControllers[0] : null;
				default:
					throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null);
			}
		}

		/// <summary>
		/// Reinitialize the InputGlyphs system with a new <see cref="GlyphCollection"/>.
		/// </summary>
		/// <remarks>
		/// Unloads all glyph maps and glyph definitions, replacing them with the provided data from the provided <see cref="GlyphCollection"/>.<br/><br/>
		/// Usage of this method is not necessary unless you wish to intentionally change the glyphs displayed due to special circumstances in your application.
		/// Such cases may be unique glyphs for a particular environment, team, or faction.<br/><br/>
		/// Automatically called in Awake by <see cref="RewiredGlyphManager"/>.
		/// </remarks>
		public static void LoadGlyphCollection(GlyphCollection collection)
		{
			// Create guid glyph lookup
			GuidGlyphMaps.Clear();
			foreach (GlyphCollection.GuidEntry guidEntry in collection.GuidMaps)
			{
				GuidGlyphMaps[guidEntry.GuidValue] = guidEntry.glyphMap.CreateDictionary();
			}

			// Create hardware glyph lookup
			HardwareGlyphMaps.Clear();
			foreach (GlyphCollection.HardwareEntry entry in collection.HardwareMaps)
			{
				HardwareGlyphMaps[entry.hardwareDefinition] = entry.glyphMap.CreateDictionary();
			}

			// Create template glyph lookup
			TemplateGlyphMaps.Clear();
			foreach (GlyphCollection.TemplateEntry entry in collection.TemplateMaps)
			{
				TemplateGlyphMaps[entry.symbolPreference] = entry.glyphMap.CreateDictionary();
			}

			UninitializedGlyph = collection.UninitializedGlyph;
			UninitializedGlyph.GlyphType = Glyph.Type.Uninitialized;
			UnboundGlyph = collection.UnboundGlyph;
			UnboundGlyph.GlyphType = Glyph.Type.Unbound;
			NullGlyph = collection.NullGlyph;
			NullGlyph.GlyphType = Glyph.Type.Null;
			MarkGlyphsDirty();
		}

		/// <summary>
		/// Set the user preference for glyph display.
		/// </summary>
		/// <param name="hardwarePreference">The preferred hardware to show symbols for</param>
		/// <param name="symbolPreference">The preferred type of symbols to display</param>
		public static void SetGlyphPreferences(HardwarePreference hardwarePreference, SymbolPreference symbolPreference)
		{
			PreferredHardware = hardwarePreference;
			PreferredSymbols = symbolPreference;
			MarkGlyphsDirty();
		}

		#region Core - Get By Pole
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the most recently used device, dynamically switching when the user inputs with a different device.
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
		public static Glyph GetCurrentGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false)
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
					return player.GetJoystickGlyph(actionID, last, pole, out axisRange);
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
						return player.GetJoystickGlyph(actionID, last, pole, out axisRange);
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
				return player.GetJoystickGlyph(actionID, null, pole, out axisRange);
			}
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the mouse or keyboard devices (in that order).
		/// </summary>
		/// <remarks>
		/// If an input is present on both the keyboard and mouse, precedence is given to the mouse. 
		/// </remarks>
		public static Glyph GetKeyboardMouseGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap mouseMap = player.GetActionElementMap(ControllerType.Mouse, action.id, pole, forceAxis, out AxisRange expectedMouse);
			ActionElementMap keyboardMap = player.GetActionElementMap(ControllerType.Keyboard, action.id, pole, forceAxis, out AxisRange expectedKeyboard);
			if (mouseMap != null)
			{
				axisRange = expectedMouse;
				Glyph glyph = GetNativeGlyphFromHardwareMap(HardwareDefinition.Mouse, mouseMap.elementIdentifierId);
				return glyph ?? GetFallbackGlyph(mouseMap.elementIdentifierName);
			}

			if (keyboardMap != null)
			{
				axisRange = expectedKeyboard;
				Glyph glyph = GetNativeGlyphFromHardwareMap(HardwareDefinition.Keyboard, keyboardMap.elementIdentifierId);
				return glyph ?? GetFallbackGlyph(keyboardMap.elementIdentifierName);
			}

			return UnboundGlyph;
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the keyboard device.
		/// </summary>
		public static Glyph GetKeyboardGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap keyboardMap = player.GetActionElementMap(ControllerType.Keyboard, action.id, pole, forceAxis, out AxisRange expectedAxis);
			if (keyboardMap != null)
			{
				axisRange = expectedAxis;
				Glyph glyph = GetNativeGlyphFromHardwareMap(HardwareDefinition.Keyboard, keyboardMap.elementIdentifierId);
				return glyph ?? GetFallbackGlyph(keyboardMap.elementIdentifierName);
			}

			return UnboundGlyph;
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the mouse device.
		/// </summary>
		public static Glyph GetMouseGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap mouseMap = player.GetActionElementMap(ControllerType.Mouse, action.id, pole, forceAxis, out AxisRange expectedAxis);
			if (mouseMap != null)
			{
				axisRange = expectedAxis;
				Glyph glyph = GetNativeGlyphFromHardwareMap(HardwareDefinition.Mouse, mouseMap.elementIdentifierId);
				return glyph ?? GetFallbackGlyph(mouseMap.elementIdentifierName);
			}

			return UnboundGlyph;
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user.
		/// <br/><br/>
		/// Will pick an icon to represent the joystick device.
		/// </summary>
		public static Glyph GetJoystickGlyph(this Player player, int actionID, Controller controller, Pole pole, out AxisRange axisRange, bool forceAxis = false)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			// Initialize variables
			Glyph glyph;
			InputAction action = ReInput.mapping.GetAction(actionID);

			// Make sure the action expected is valid, escape with null glyph if invalid action given by developer.
			if (action == null)
			{
				return NullGlyph;
			}

			// Make sure the action is actually bound, if not escape with an unbound glyph.
			ActionElementMap map = player.GetActionElementMap(ControllerType.Joystick, action.id, pole, forceAxis, out AxisRange expectedAxis);
			if (map == null)
			{
				return UnboundGlyph;
			}

			axisRange = expectedAxis;

			// Try to retrieve a glyph that is specific to the user's controller hardware.
			if (controller != null && PreferredSymbols == SymbolPreference.Auto)
			{
				glyph = GetNativeGlyphFromGuidMap(controller.hardwareTypeGuid, map.elementIdentifierId);
				if (glyph != null)
				{
					return glyph;
				}

				HardwareDefinition controllerType = GetHardwareDefinition(controller);
				glyph = GetNativeGlyphFromHardwareMap(controllerType, map.elementIdentifierId);
				if (glyph != null)
				{
					return glyph;
				}
			}

			// Try to retrieve a glyph that is mapped to the gamepad template (since at this point one was not found for the user's controller)
			// Determine the element expected on the template
			controller = player.controllers.GetFirstControllerWithTemplate(GamepadTemplateGuid);
			IControllerTemplate template = controller.GetTemplate(GamepadTemplateGuid);
			int targets = template.GetElementTargets(map, TemplateTargets);
			int templateElementId = targets > 0 ? TemplateTargets[0].element.id : -1;

			// Use the template glyph if one exists.
			glyph = GetNativeGlyphFromTemplateMap(PreferredSymbols, templateElementId);
			return glyph ?? GetFallbackGlyph(map.elementIdentifierName);
		}
		#endregion

		#region Convenience Overloads
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
		#endregion

		/// <summary>
		/// Determine the hardware definition to use for a controller based on the controller's hardwareTypeGuid.
		/// </summary>
		/// <remarks>
		/// The hardware id is searched within <see cref="ControllerGuids"/>.
		/// </remarks>
		/// <param name="controller">The controller the evaluate the hardware definition of</param>
		/// <returns>
		/// The <see cref="HardwareDefinition"/> for the provided controller.
		/// Will return <see cref="HardwareDefinition.Unknown"/> if the controller is null or no definition found in <see cref="ControllerGuids"/>
		/// </returns>
		public static HardwareDefinition GetHardwareDefinition(Controller controller)
		{
			if (!CanRetrieveGlyph || controller == null)
			{
				return HardwareDefinition.Unknown;
			}

			bool controllerHasHardwareDefinition = ControllerGuids.TryGetValue(controller.hardwareTypeGuid, out HardwareDefinition controllerHardwareDefinition);
			return controllerHasHardwareDefinition ? controllerHardwareDefinition : HardwareDefinition.Unknown;
		}

		/// <summary>
		/// Clear out the player cache of the input glyph system.
		/// </summary>
		/// <remarks>
		/// Invoking this method is rarely necessary but might be required in cases where the player list indices change.
		/// </remarks>
		public static void FlushPlayersCache()
		{
			Players.Clear();
		}

		/// <summary>
		/// Notify the glyph system that input state has changed and glyphs should be reevaluated.
		/// </summary>
		/// <remarks>
		/// For performance reasons the input glyph system only evaluates glyphs when it believes the output has changed such as a player using a different input device.
		/// By default however, the InputGlyph system is unaware of changes such as control remapping and needs to be manually informed of such changes using this method. 
		/// </remarks>
		public static void MarkGlyphsDirty()
		{
			glyphsDirty = true;
		}

		/// <summary>
		/// Will invoke <see cref="RebuildGlyphs"/> event when glyphs are dirty or <see cref="forceRebuild"/> parameter is true.
		/// </summary>
		/// <param name="forceRebuild">When true will rebuild regardless of the value of <see cref="glyphsDirty"/>. When false will only rebuild if glyphs are dirty.</param>
		public static void InvokeRebuild(bool forceRebuild = false)
		{
			if (!glyphsDirty && !forceRebuild)
			{
				return;
			}

			foreach (Delegate evaluateDelegate in RebuildGlyphs.GetInvocationList())
			{
				try
				{
					evaluateDelegate.DynamicInvoke();
				}
				catch (Exception e)
				{
#if UNITY_EDITOR
					Debug.LogException(e);
#endif
				}
			}

			glyphsDirty = false;
		}

		#region Public Unsafe
		/// <summary>
		/// Retrieve a glyph for this element id that belongs to a specific hardware setup, via hardware guid.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> instead.
		/// </remarks>
		/// <seealso cref="GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// <param name="hardwareGuid">The hardware guid that the <see cref="elementID"/> maps to</param>
		/// <param name="elementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of this hardware's glyph map. Returns null (not <see cref="NullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public static Glyph GetNativeGlyphFromGuidMap(Guid hardwareGuid, int elementID)
		{
			bool hasGuidGlyphMap = GuidGlyphMaps.TryGetValue(hardwareGuid, out Dictionary<int, Glyph> value);
			return hasGuidGlyphMap && value.TryGetValue(elementID, out Glyph glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a glyph for this element id that belongs to a specific hardware setup, via hardware type.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> or <see cref="GetKeyboardMouseGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> instead.
		/// </remarks>
		/// <seealso cref="GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// <seealso cref="GetKeyboardMouseGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// <seealso cref="GetKeyboardGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// <seealso cref="GetMouseGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// <param name="controller">The hardware type that the <see cref="elementID"/> maps to</param>
		/// <param name="elementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of this hardware's glyph map. Returns null (not <see cref="NullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public static Glyph GetNativeGlyphFromHardwareMap(HardwareDefinition controller, int elementID)
		{
			bool hasHardwareGlyphMap = HardwareGlyphMaps.TryGetValue(controller, out Dictionary<int, Glyph> value);
			return hasHardwareGlyphMap && value.TryGetValue(elementID, out Glyph glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a <see cref="SymbolPreference"/> styled glyph for this <see cref="templateElementID"/> via the generic glyph mapping.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> instead.
		/// </remarks>
		/// <seealso cref="GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/>
		/// <param name="symbolPreference">The preferred symbol styling to present for this template element</param>
		/// <param name="templateElementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of a template glyph map. Returns null (not <see cref="NullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public static Glyph GetNativeGlyphFromTemplateMap(SymbolPreference symbolPreference, int templateElementID)
		{
			bool hasTemplateGlyphMap = TemplateGlyphMaps.TryGetValue(symbolPreference, out Dictionary<int, Glyph> templateGlyphMap);
			return hasTemplateGlyphMap && templateGlyphMap.TryGetValue(templateElementID, out Glyph glyph) ? glyph : null;
		}
		#endregion

		#region Internal Use
		/// <summary>
		/// Retrieve a cached player reference from <see cref="ReInput"/>.
		/// </summary>
		private static bool TryGetPlayer(int index, out Player player)
		{
			if (!CanRetrieveGlyph)
			{
				player = null;
				return false;
			}

			if (Players.TryGetValue(index, out player) && player != null)
			{
				return true;
			}

			player = ReInput.players.GetPlayer(index);
			if (player == null)
			{
				return false;
			}

			Players.Add(index, player);
			return true;
		}

		/// <summary>
		/// Tries to find the template glyph map for a specific <see cref="SymbolPreference"/>.
		/// If none is found will use the automatic template glyph map.
		/// If that doesn't exist outputs null.
		/// </summary>
		private static bool TryGetTemplateGlyphMap(SymbolPreference symbolPreference, out Dictionary<int, Glyph> templateGlyphMap)
		{
			// If we have a specific symbol preference, try to get the glyph map for that symbol type
			if (symbolPreference != SymbolPreference.Auto && TemplateGlyphMaps.TryGetValue(symbolPreference, out templateGlyphMap))
			{
				return true;
			}
			// If we are in the auto mode or finding a specific map failed, fallback to the auto glyph map
			else if (TemplateGlyphMaps.TryGetValue(SymbolPreference.Auto, out templateGlyphMap))
			{
				return true;
			}
			// There is no auto glyph map or symbol specific glyph map available.
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Retrieve a glyph that just has a description.
		/// </summary>
		/// <remarks>
		/// This is mainly used when an action is valid, has an element map, but that element can't be found in any of the glyph maps.
		/// Since we obviously can't assume what this element would look like with a sprite we at least give it a description so text glyph outputs can still function.<br/><br/>
		/// Ultimately this is a fallback mechanism and having specific element definitions for all inputs should be done.
		/// </remarks>
		private static Glyph GetFallbackGlyph(string name)
		{
			if (!FallbackGlyphs.ContainsKey(name))
			{
				FallbackGlyphs.Add(name, new Glyph(name, NullGlyph.FullSprite));
			}

			return FallbackGlyphs[name];
		}

		private static readonly List<ActionElementMap> MapLookupResults = new List<ActionElementMap>();

		/// <summary>
		/// Find the first mapping that is for this controller and with the correct pole direction. Null if no such map exists.
		/// </summary>
		private static ActionElementMap GetActionElementMap(this Player player, ControllerType controller, int actionID, Pole pole, bool getAsAxis, out AxisRange expectedAxis)
		{
			InputAction inputAction = ReInput.mapping.GetAction(actionID);
			if (inputAction == null)
			{
				expectedAxis = AxisRange.Full;
				return null;
			}

			bool actionIsAxis = inputAction.type == InputActionType.Axis;
			int count = player.controllers.maps.GetElementMapsWithAction(controller, actionID, false, MapLookupResults);
			for (int i = 0; i < count; i++)
			{
				ActionElementMap elementMap = MapLookupResults[i];
				switch (getAsAxis)
				{
					// Pick this when the element is an axis and we want to get an axis element.
					case true when actionIsAxis && elementMap.axisType == AxisType.Normal:
						expectedAxis = elementMap.axisRange;
						return elementMap;
					// Pick this when the element is an axis but we want to get a specific axis range for it.
					case false when actionIsAxis && (elementMap.axisType != AxisType.None || elementMap.axisContribution == pole):
						expectedAxis = pole == Pole.Positive ? AxisRange.Positive : AxisRange.Negative;
						return elementMap;
					// Pick this when the action is a button and it has the expected axis contribution value
					case false when !actionIsAxis && elementMap.axisContribution == pole:
						expectedAxis = elementMap.axisRange;
						return elementMap;
				}
			}

			expectedAxis = AxisRange.Full;
			return null;
		}
		#endregion
	}
}