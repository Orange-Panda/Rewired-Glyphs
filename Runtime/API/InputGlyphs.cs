using JetBrains.Annotations;
using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The InputGlyphs class contains various methods to easily get <see cref="Sprite"/> and <see cref="string"/> information about user input controls.
/// </summary>
/// <remarks>The <see cref="GetCurrentGlyph(string, Pole, out AxisRange)"/> function should provide most functionality needed.</remarks>
[PublicAPI]
public static class InputGlyphs
{
	private static Player defaultPlayer;

	/// <summary>
	/// The type Guid of the Controller Template.
	/// </summary>
	private static readonly Guid GamepadGuid = new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5");
	private static readonly List<ActionElementMap> MapLookupResults = new List<ActionElementMap>();
	/// <summary>
	/// Automatically generated glyphs for inputs that have no glyph defined anywhere.
	/// </summary>
	private static readonly Dictionary<string, InputGlyph> FallbackGlyphs = new Dictionary<string, InputGlyph>();
	/// <summary>
	/// Glyph mapping based on hardware specific glyphs and hardware specific input ids.
	/// </summary>
	private static readonly Dictionary<HardwareSymbols, Dictionary<int, InputGlyph>> HardwareGlyphMaps = new Dictionary<HardwareSymbols, Dictionary<int, InputGlyph>>();
	/// <summary>
	/// Glyph mapping based on template glyphs and template input ids.
	/// </summary>
	private static readonly Dictionary<SymbolPreference, Dictionary<int, InputGlyph>> GenericGlyphMaps = new Dictionary<SymbolPreference, Dictionary<int, InputGlyph>>();
	/// <summary>
	/// Lookup table that matches hardware ids to what kind of glyphs should be shown.
	/// </summary>
	private static readonly Dictionary<Guid, HardwareSymbols> ControllerGuids = new Dictionary<Guid, HardwareSymbols>
	{
		{ new Guid("d74a350e-fe8b-4e9e-bbcd-efff16d34115"), HardwareSymbols.Xbox }, // Xbox 360
		{ new Guid("19002688-7406-4f4a-8340-8d25335406c8"), HardwareSymbols.Xbox }, // Xbox One
		{ new Guid("c3ad3cad-c7cf-4ca8-8c2e-e3df8d9960bb"), HardwareSymbols.Playstation2 }, // Playstation 2
		{ new Guid("71dfe6c8-9e81-428f-a58e-c7e664b7fbed"), HardwareSymbols.Playstation3 }, // Playstation 3
		{ new Guid("cd9718bf-a87a-44bc-8716-60a0def28a9f"), HardwareSymbols.Playstation }, // Playstation 4
		{ new Guid("5286706d-19b4-4a45-b635-207ce78d8394"), HardwareSymbols.Playstation }, // Playstation 5
		{ new Guid("521b808c-0248-4526-bc10-f1d16ee76bf1"), HardwareSymbols.NintendoSwitch }, // Joycons (Dual)
		{ new Guid("1fbdd13b-0795-4173-8a95-a2a75de9d204"), HardwareSymbols.NintendoSwitch }, // Joycons (Handheld)
		{ new Guid("7bf3154b-9db8-4d52-950f-cd0eed8a5819"), HardwareSymbols.NintendoSwitch } // Pro controller
	};

	public static InputGlyph NullGlyph { get; private set; }
	public static InputGlyph UnboundGlyph { get; private set; }
	public static DisplayMode PreferredDisplayMode { get; private set; } = DisplayMode.Auto;
	public static SymbolPreference PreferredSymbols { get; private set; } = SymbolPreference.Auto;
	private static Player DefaultPlayer => defaultPlayer ??= ReInput.players.GetPlayer(0);

	/// <summary>
	/// An event to let the <see cref="InputGlyphObserver"/> know that the <see cref="InputGlyphs"/> is up to date with preference changes.
	/// </summary>
	public static event Action GlyphPreferencesChanged = delegate { };

	static InputGlyphs()
	{
		const string ResourcePath = "Rewired Glyphs/Default Glyphs";
		InputGlyphCollection collection = Resources.Load<InputGlyphCollection>(ResourcePath);
		if (collection == null)
		{
			throw new NullReferenceException($"There was no {nameof(InputGlyphCollection)} found at \"{ResourcePath}\". Please create one.");
		}

		LoadGlyphCollection(collection);
		SetGlyphPreferences(DisplayMode.Auto, SymbolPreference.Auto);
	}

	public static void LoadGlyphCollection(InputGlyphCollection collection)
	{
		// Create hardware glyph lookup
		HardwareGlyphMaps.Clear();
		foreach (InputGlyphCollection.Entry entry in collection.Maps)
		{
			HardwareGlyphMaps[entry.controllerType] = entry.glyphMap.CreateDictionary();
		}

		// Create template glyph lookup
		GenericGlyphMaps.Clear();
		foreach (InputGlyphCollection.GenericEntry entry in collection.GenericMaps)
		{
			GenericGlyphMaps[entry.controllerType] = entry.glyphMap.CreateDictionary();
		}

		UnboundGlyph = collection.UnboundGlyph;
		NullGlyph = collection.NullGlyph;
		GlyphPreferencesChanged.Invoke();
	}

	/// <summary>
	/// Read the user's preference file for their glyph rendering settings.
	/// </summary>
	public static void SetGlyphPreferences(DisplayMode displayMode, SymbolPreference symbolPreference)
	{
		PreferredDisplayMode = displayMode;
		PreferredSymbols = symbolPreference;
		GlyphPreferencesChanged.Invoke();
	}

	/// <summary>
	/// Get the InputGlyph that represents the glyph for the current input scheme
	/// </summary>
	public static InputGlyph GetCurrentGlyph(string actionName, Pole pole, out AxisRange axisRange)
	{
		Player player = DefaultPlayer;
		Controller last = player.controllers.GetLastActiveController();

		// Force a particular mode if the user preferences say so
		if (PreferredDisplayMode != DisplayMode.Auto)
		{
			return PreferredDisplayMode == DisplayMode.KeyboardMouse
				? GetKeyboardMouseGlyph(actionName, pole, out axisRange)
				: GetJoystickGlyph(actionName, last, pole, out axisRange);
		}

		// Use the expected mode for this hardware if there is no controller is active (usually only at the start of the application)
		if (last == null)
		{
			return SystemInfo.deviceType == DeviceType.Desktop
				? GetKeyboardMouseGlyph(actionName, pole, out axisRange)
				: GetJoystickGlyph(actionName, null, pole, out axisRange);
		}

		// Use the mode that matches the last controller used by user.
		return last.type == ControllerType.Keyboard || last.type == ControllerType.Mouse
			? GetKeyboardMouseGlyph(actionName, pole, out axisRange)
			: GetJoystickGlyph(actionName, last, pole, out axisRange);
	}

	/// <summary>
	/// Get the InputGlyph that represents the input action on the Keyboard/Mouse.
	/// If an action is present on both devices, precedence is given to the mouse device.
	/// </summary>
	public static InputGlyph GetKeyboardMouseGlyph(string actionName, Pole pole, out AxisRange axisRange)
	{
		axisRange = AxisRange.Full;
		InputAction action = ReInput.mapping.GetAction(actionName);
		if (string.IsNullOrWhiteSpace(actionName) || action == null)
		{
			return NullGlyph;
		}

		ActionElementMap mouseMap = GetActionElementMap(ControllerType.Mouse, action.id, pole);
		ActionElementMap keyboardMap = GetActionElementMap(ControllerType.Keyboard, action.id, pole);
		if (mouseMap != null)
		{
			axisRange = mouseMap.axisRange;
			InputGlyph glyph = GetHardwareGlyph(HardwareSymbols.Mouse, mouseMap.elementIdentifierId);
			return glyph ?? GetFallbackGlyph(mouseMap.elementIdentifierName);
		}

		if (keyboardMap != null)
		{
			axisRange = keyboardMap.axisRange;
			InputGlyph glyph = GetHardwareGlyph(HardwareSymbols.Keyboard, keyboardMap.elementIdentifierId);
			return glyph ?? GetFallbackGlyph(keyboardMap.elementIdentifierName);
		}

		return UnboundGlyph;
	}

	/// <summary>
	/// Get the InputGlyph that represents the Joystick input action.
	/// </summary>
	public static InputGlyph GetJoystickGlyph(string actionName, Controller controller, Pole pole, out AxisRange axisRange)
	{
		// Initialize variables
		InputGlyph glyph;
		axisRange = AxisRange.Full;
		Player player = DefaultPlayer;
		InputAction action = ReInput.mapping.GetAction(actionName);

		// Make sure the action expected is valid, escape with null glyph if invalid action given by developer.
		if (string.IsNullOrWhiteSpace(actionName) || action == null)
		{
			return NullGlyph;
		}

		// Make sure the action is actually bound, if not escape with an unbound glyph.
		ActionElementMap map = GetActionElementMap(ControllerType.Joystick, action.id, pole);
		if (map == null)
		{
			return UnboundGlyph;
		}

		axisRange = map.axisRange;
		// Try to retrieve a glyph that is specific to the user's specific controller hardware.
		if (controller != null && PreferredSymbols == SymbolPreference.Auto)
		{
			HardwareSymbols controllerType = GetControllerType(controller);
			glyph = GetHardwareGlyph(controllerType, map.elementIdentifierId);
			if (glyph != null)
			{
				return glyph;
			}
		}

		// Try to retrieve a glyph that is mapped to the generic gamepad template (since at this point one was not found for the user's controller)
		// Determine the element expected on the template
		controller = player.controllers.GetFirstControllerWithTemplate(GamepadGuid);
		IControllerTemplate template = controller.GetTemplate(GamepadGuid);
		IList<ControllerTemplateElementTarget> targets = new List<ControllerTemplateElementTarget>(2);
		template.GetElementTargets(map, targets);
		int templateElementId = targets.Count > 0 ? targets[0].element.id : -1;

		// Use the generic glyph if one exists.
		glyph = GetGenericGlyph(PreferredSymbols, templateElementId);
		return glyph ?? GetFallbackGlyph(map.elementIdentifierName);
	}

	/// <summary>
	/// Find the first mapping that is for this controller and with the correct pole direction. Null if no such map exists.
	/// </summary>
	private static ActionElementMap GetActionElementMap(ControllerType controller, int actionID, Pole pole)
	{
		int count = DefaultPlayer.controllers.maps.GetElementMapsWithAction(controller, actionID, false, MapLookupResults);
		for (int i = 0; i < count; i++)
		{
			if (MapLookupResults[i].axisContribution == pole)
			{
				return MapLookupResults[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Retrieve or create a glyph with just a description.
	/// Useful if a glyph does not already exist.
	/// </summary>
	private static InputGlyph GetFallbackGlyph(string name)
	{
		if (!FallbackGlyphs.ContainsKey(name))
		{
			FallbackGlyphs.Add(name, new InputGlyph(-1, name));
		}

		return FallbackGlyphs[name];
	}

	private static InputGlyph GetHardwareGlyph(HardwareSymbols controller, int elementID)
	{
		return HardwareGlyphMaps.TryGetValue(controller, out Dictionary<int, InputGlyph> value) && value.TryGetValue(elementID, out InputGlyph glyph)
			? glyph
			: null;
	}

	private static InputGlyph GetGenericGlyph(SymbolPreference symbols, int elementID)
	{
		return GenericGlyphMaps.TryGetValue(symbols, out Dictionary<int, InputGlyph> value) && value.TryGetValue(elementID, out InputGlyph glyph)
			? glyph
			: null;
	}

	public static HardwareSymbols GetControllerType(Controller controller)
	{
		return controller != null && ControllerGuids.TryGetValue(controller.hardwareTypeGuid, out HardwareSymbols controllerType)
			? controllerType
			: HardwareSymbols.Unknown;
	}

	public enum DisplayMode
	{
		Auto, KeyboardMouse, Joystick
	}

	public enum HardwareSymbols
	{
		Unknown = -1, Generic, Keyboard, Mouse, Xbox, Playstation2, Playstation3, Playstation, NintendoSwitch
	}

	public enum SymbolPreference
	{
		Auto, Xbox, Playstation, NintendoSwitch
	}
}