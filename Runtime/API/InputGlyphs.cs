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
	/// <remarks>The <see cref="GetCurrentGlyph(int, Pole, out AxisRange, int)"/> function should provide most functionality needed.</remarks>
	[PublicAPI]
	public static class InputGlyphs
	{
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
		/// Glyph mapping based on hardware specific glyphs and hardware specific input ids.
		/// </summary>
		private static readonly Dictionary<HardwareDefinition, Dictionary<int, Glyph>> HardwareGlyphMaps = new Dictionary<HardwareDefinition, Dictionary<int, Glyph>>();
		/// <summary>
		/// Glyph mapping based on template glyphs and template input ids.
		/// </summary>
		private static readonly Dictionary<SymbolPreference, Dictionary<int, Glyph>> TemplateGlyphMaps = new Dictionary<SymbolPreference, Dictionary<int, Glyph>>();
		/// <summary>
		/// Lookup table that matches hardware ids to what kind of glyphs should be shown.
		/// </summary>
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

		public static Glyph NullGlyph { get; private set; }
		public static Glyph UnboundGlyph { get; private set; }
		public static HardwarePreference PreferredHardware { get; private set; } = HardwarePreference.Auto;
		public static SymbolPreference PreferredSymbols { get; private set; } = SymbolPreference.Auto;

		/// <summary>
		/// An event that is invoked when preferences relating to the input glyph system such as <see cref="PreferredHardware"/> or <see cref="PreferredSymbols"/> have changed.
		/// </summary>
		/// <remarks>
		/// The aforementioned preferences can mutate the output of glyph symbol queries.
		/// As such this event gives the opportunity for others to update the glyph output without having to query the InputGlyphs system every frame. 
		/// </remarks>
		public static event Action GlyphPreferencesChanged = delegate { };

		static InputGlyphs()
		{
			const string ResourcePath = "Rewired Glyphs/Default Glyphs";
			GlyphCollection collection = Resources.Load<GlyphCollection>(ResourcePath);
			if (collection == null)
			{
				throw new NullReferenceException($"There was no {nameof(GlyphCollection)} found at \"{ResourcePath}\". Please create one.");
			}

			LoadGlyphCollection(collection);
		}

		public static void LoadGlyphCollection(GlyphCollection collection)
		{
			// Create hardware glyph lookup
			HardwareGlyphMaps.Clear();
			foreach (GlyphCollection.HardwareEntry entry in collection.HardwareMaps)
			{
				HardwareGlyphMaps[entry.controllerType] = entry.glyphMap.CreateDictionary();
			}

			// Create template glyph lookup
			TemplateGlyphMaps.Clear();
			foreach (GlyphCollection.TemplateEntry entry in collection.TemplateMaps)
			{
				TemplateGlyphMaps[entry.controllerType] = entry.glyphMap.CreateDictionary();
			}

			UnboundGlyph = collection.UnboundGlyph;
			NullGlyph = collection.NullGlyph;
			GlyphPreferencesChanged.Invoke();
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
			GlyphPreferencesChanged.Invoke();
		}

		/// <inheritdoc cref="GetCurrentGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange)"/>
		public static Glyph GetCurrentGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0)
		{
			return GetPlayer(playerIndex).GetCurrentGlyph(actionID, pole, out axisRange);
		}

		/// <summary>
		/// Get the InputGlyph that represents the glyph for the current input scheme
		/// </summary>
		public static Glyph GetCurrentGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange)
		{
			Controller last = player.controllers.GetLastActiveController();

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

		/// <inheritdoc cref="GetKeyboardMouseGlyph(Player, int, Rewired.Pole, out Rewired.AxisRange)"/>
		public static Glyph GetKeyboardMouseGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0)
		{
			return GetPlayer(playerIndex).GetKeyboardMouseGlyph(actionID, pole, out axisRange);
		}

		/// <summary>
		/// Get the InputGlyph that represents the input action on the Keyboard/Mouse.
		/// If an action is present on both devices, precedence is given to the mouse device.
		/// </summary>
		public static Glyph GetKeyboardMouseGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange)
		{
			axisRange = AxisRange.Full;
			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap mouseMap = player.GetActionElementMap(ControllerType.Mouse, action.id, pole);
			ActionElementMap keyboardMap = player.GetActionElementMap(ControllerType.Keyboard, action.id, pole);
			if (mouseMap != null)
			{
				axisRange = mouseMap.axisRange;
				Glyph glyph = GetGlyphFromHardwareMap(HardwareDefinition.Mouse, mouseMap.elementIdentifierId);
				return glyph ?? GetFallbackGlyph(mouseMap.elementIdentifierName);
			}

			if (keyboardMap != null)
			{
				axisRange = keyboardMap.axisRange;
				Glyph glyph = GetGlyphFromHardwareMap(HardwareDefinition.Keyboard, keyboardMap.elementIdentifierId);
				return glyph ?? GetFallbackGlyph(keyboardMap.elementIdentifierName);
			}

			return UnboundGlyph;
		}

		/// <inheritdoc cref="GetJoystickGlyph(Player, int, Controller, Rewired.Pole, out Rewired.AxisRange)"/>
		public static Glyph GetJoystickGlyph(int actionID, Controller controller, Pole pole, out AxisRange axisRange, int playerIndex = 0)
		{
			return GetPlayer(playerIndex).GetJoystickGlyph(actionID, controller, pole, out axisRange);
		}

		/// <summary>
		/// Get the InputGlyph that represents the Joystick input action.
		/// </summary>
		public static Glyph GetJoystickGlyph(this Player player, int actionID, Controller controller, Pole pole, out AxisRange axisRange)
		{
			// Initialize variables
			Glyph glyph;
			axisRange = AxisRange.Full;
			InputAction action = ReInput.mapping.GetAction(actionID);

			// Make sure the action expected is valid, escape with null glyph if invalid action given by developer.
			if (action == null)
			{
				return NullGlyph;
			}

			// Make sure the action is actually bound, if not escape with an unbound glyph.
			ActionElementMap map = player.GetActionElementMap(ControllerType.Joystick, action.id, pole);
			if (map == null)
			{
				return UnboundGlyph;
			}

			axisRange = map.axisRange;

			// Try to retrieve a glyph that is specific to the user's controller hardware.
			if (controller != null && PreferredSymbols == SymbolPreference.Auto)
			{
				HardwareDefinition controllerType = GetHardwareDefinition(controller);
				glyph = GetGlyphFromHardwareMap(controllerType, map.elementIdentifierId);
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
			glyph = GetGlyphFromTemplateMap(PreferredSymbols, templateElementId);
			return glyph ?? GetFallbackGlyph(map.elementIdentifierName);
		}

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
			if (controller == null)
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
		/// Retrieve a cached player reference from <see cref="ReInput"/>.
		/// </summary>
		private static Player GetPlayer(int index)
		{
			if (!Players.TryGetValue(index, out Player player) || player == null)
			{
				player = ReInput.players.GetPlayer(index);
				Players.Add(index, player);
				return player;
			}

			return player;
		}

		#region Internal Use
		/// <summary>
		/// Retrieve a glyph for this element id that belongs to a specific hardware setup.
		/// </summary>
		/// <param name="controller">The hardware type that the <see cref="elementID"/> maps to</param>
		/// <param name="elementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of this hardware's glyph map. Returns null (not <see cref="NullGlyph"/>) if none is found.</returns>
		private static Glyph GetGlyphFromHardwareMap(HardwareDefinition controller, int elementID)
		{
			bool hasHardwareGlyphMap = HardwareGlyphMaps.TryGetValue(controller, out Dictionary<int, Glyph> value);
			return hasHardwareGlyphMap && value.TryGetValue(elementID, out Glyph glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a <see cref="SymbolPreference"/> styled glyph for this <see cref="templateElementID"/> via the generic glyph mapping.
		/// </summary>
		/// <param name="symbolPreference">The preferred symbol styling to present for this template element</param>
		/// <param name="templateElementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of a template glyph map. Returns null (not <see cref="NullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		private static Glyph GetGlyphFromTemplateMap(SymbolPreference symbolPreference, int templateElementID)
		{
			bool hasTemplateGlyphMap = TemplateGlyphMaps.TryGetValue(symbolPreference, out Dictionary<int, Glyph> templateGlyphMap);
			return hasTemplateGlyphMap && templateGlyphMap.TryGetValue(templateElementID, out Glyph glyph) ? glyph : null;
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
				// TODO: Maybe there should be a missing glyph sprite to inform developer? The InputGlyph would need to recognize it should prefer description in that case.
				FallbackGlyphs.Add(name, new Glyph(-1, name));
			}

			return FallbackGlyphs[name];
		}
		
		private static readonly List<ActionElementMap> MapLookupResults = new List<ActionElementMap>();
		
		/// <summary>
		/// Find the first mapping that is for this controller and with the correct pole direction. Null if no such map exists.
		/// </summary>
		private static ActionElementMap GetActionElementMap(this Player player, ControllerType controller, int actionID, Pole pole)
		{
			int count = player.controllers.maps.GetElementMapsWithAction(controller, actionID, false, MapLookupResults);
			for (int i = 0; i < count; i++)
			{
				if (MapLookupResults[i].axisContribution == pole)
				{
					return MapLookupResults[i];
				}
			}

			return null;
		}
		#endregion
	}
}