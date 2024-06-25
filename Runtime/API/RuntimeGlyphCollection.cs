using JetBrains.Annotations;
using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	internal class RuntimeGlyphCollection
	{
		internal static readonly RuntimeGlyphCollection Default = new RuntimeGlyphCollection();

		private readonly Dictionary<int, Glyph> defaultGamepadMap = new Dictionary<int, Glyph>();
		private readonly Dictionary<int, Glyph> keyboardMap = new Dictionary<int, Glyph>();
		private readonly Dictionary<int, Glyph> mouseMap = new Dictionary<int, Glyph>();

		/// <summary>
		/// Glyph mapping based on hardware specific glyphs and hardware specific input ids, found via hardware Guid.
		/// </summary>
		private readonly Dictionary<Guid, Dictionary<int, Glyph>> joystickGuidMaps = new Dictionary<Guid, Dictionary<int, Glyph>>();

		/// <summary>
		/// Glyph mapping based on template glyphs and template input ids.
		/// </summary>
		private readonly Dictionary<SymbolPreference, Dictionary<int, Glyph>> templateGlyphMaps = new Dictionary<SymbolPreference, Dictionary<int, Glyph>>();

		/// <summary>
		/// Automatically generated glyphs for inputs that have no glyph defined anywhere.
		/// </summary>
		private readonly Dictionary<(string, ControllerType?), Glyph> fallbackGlyphs = new Dictionary<(string, ControllerType?), Glyph>();

		internal readonly GlyphCollection collection;
		/// <inheritdoc cref="InputGlyphs.NullGlyph"/>
		internal readonly Glyph nullGlyph;
		/// <inheritdoc cref="InputGlyphs.UnboundGlyph"/>
		internal readonly Glyph unboundGlyph;
		/// <inheritdoc cref="InputGlyphs.UninitializedGlyph"/>
		internal readonly Glyph uninitializedGlyph;

		private RuntimeGlyphCollection()
		{
			collection = null;
			uninitializedGlyph = new Glyph("Uninitialized", controllerType: null, type: Glyph.Type.Uninitialized);
			unboundGlyph = new Glyph("Unbound", controllerType: null, type: Glyph.Type.Unbound);
			nullGlyph = new Glyph("Null", controllerType: null, type: Glyph.Type.Null);
		}

		internal RuntimeGlyphCollection(GlyphCollection collection)
		{
			this.collection = collection;

			// Create guid glyph lookup
			foreach (GlyphCollection.GuidEntry guidEntry in collection.GuidMaps)
			{
				if (guidEntry.GlyphMap == null)
				{
					Debug.LogError($"Null glyph map defined for \"{guidEntry.ControllerType}\" controller map entry on collection \"{collection.name}\"", collection);
					continue;
				}

				switch (guidEntry.ControllerType)
				{
					case ControllerType.Keyboard:
						keyboardMap = guidEntry.GlyphMap.CreateDictionary();
						break;
					case ControllerType.Mouse:
						mouseMap = guidEntry.GlyphMap.CreateDictionary();
						break;
					case ControllerType.Joystick:
					case ControllerType.Custom:
						joystickGuidMaps[guidEntry.GuidValue] = guidEntry.GlyphMap.CreateDictionary();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				foreach (Glyph glyph in guidEntry.GlyphMap.Glyphs)
				{
					glyph.ControllerType = guidEntry.ControllerType;
				}
			}

			// Create template glyph lookup
			foreach (GlyphCollection.TemplateEntry entry in collection.TemplateMaps)
			{
				if (entry.GlyphMap == null)
				{
					Debug.LogError($"Null glyph map defined for a \"{entry.SymbolPreference}\" template map entry on collection \"{collection.name}\"", collection);
					continue;
				}

				templateGlyphMaps[entry.SymbolPreference] = entry.GlyphMap.CreateDictionary();
				foreach (Glyph glyph in entry.GlyphMap.Glyphs)
				{
					// We can safely assume any template entry is a joystick since template maps are used exclusively by that controller type.
					glyph.ControllerType = ControllerType.Joystick;
				}
			}

			// Create default glyph lookup
			if (collection.DefaultGamepadTemplateMap != null)
			{
				defaultGamepadMap = collection.DefaultGamepadTemplateMap.CreateDictionary();
			}
			else
			{
				Debug.LogError($"No default gamepad template glyph map found on \"{collection.name}\". Some Joystick devices may not be able to show glyphs.", collection);
			}

			if (mouseMap.Count <= 0)
			{
				Debug.LogError($"No \"Mouse\" glyphs found on \"{collection.name}\" collection. Glyphs can't be displayed for this device until one is defined.", collection);
			}

			if (keyboardMap.Count <= 0)
			{
				Debug.LogError($"No \"Keyboard\" glyphs found on \"{collection.name}\" collection. Glyphs can't be displayed for this device until one is defined.", collection);
			}

			uninitializedGlyph = collection.UninitializedGlyph;
			unboundGlyph = collection.UnboundGlyph;
			nullGlyph = collection.NullGlyph;
			uninitializedGlyph.GlyphType = Glyph.Type.Uninitialized;
			unboundGlyph.GlyphType = Glyph.Type.Unbound;
			nullGlyph.GlyphType = Glyph.Type.Null;
		}

		/// <summary>
		/// Retrieve a glyph for this element id that belongs to a specific hardware setup, via hardware guid.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="InputGlyphs.GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool, string)"/> instead.
		/// </remarks>
		/// <param name="controllerType">The type of controller to get a glyph for</param>
		/// <param name="hardwareGuid">The hardware guid for the controller</param>
		/// <param name="elementID">The element input id to get a glyph for on the <see cref="GlyphMap"/></param>
		/// <returns>The found <see cref="Glyph"/> inside of this hardware's glyph map. Returns null (<b>NOT</b> <see cref="nullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public Glyph GetNativeGlyphFromGuidMap(ControllerType controllerType, Guid hardwareGuid, int elementID)
		{
			Dictionary<int, Glyph> guidMap = GetGuidMap(controllerType, hardwareGuid);
			return guidMap != null && guidMap.TryGetValue(elementID, out Glyph glyph) ? glyph : null;
		}

		/// <inheritdoc cref="GetNativeGlyphFromGuidMap(Rewired.ControllerType,System.Guid,int)"/>
		[CanBeNull]
		public Glyph GetNativeGlyphFromGuidMap(Controller controller, int elementID)
		{
			return GetNativeGlyphFromGuidMap(controller.type, controller.hardwareTypeGuid, elementID);
		}

		[CanBeNull]
		private Dictionary<int, Glyph> GetGuidMap(ControllerType controllerType, Guid hardwareGuid)
		{
			return controllerType switch
			{
				ControllerType.Keyboard => keyboardMap,
				ControllerType.Mouse => mouseMap,
				ControllerType.Joystick => joystickGuidMaps.TryGetValue(hardwareGuid, out Dictionary<int, Glyph> value) ? value : default,
				ControllerType.Custom => joystickGuidMaps.TryGetValue(hardwareGuid, out Dictionary<int, Glyph> value) ? value : default,
				_ => throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null)
			};
		}

		/// <summary>
		/// Retrieve a <see cref="SymbolPreference"/> styled glyph for this <see cref="templateElementID"/> via the generic glyph mapping.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="InputGlyphs.GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool, string)"/> instead.
		/// </remarks>
		/// <param name="symbolPreference">The preferred symbol styling to present for this template element</param>
		/// <param name="templateElementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of a template glyph map. Returns null (<b>NOT</b> <see cref="nullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public Glyph GetNativeGlyphFromTemplateMap(SymbolPreference symbolPreference, int templateElementID)
		{
			bool hasTemplateGlyphMap = templateGlyphMaps.TryGetValue(symbolPreference, out Dictionary<int, Glyph> templateGlyphMap);
			return hasTemplateGlyphMap && templateGlyphMap.TryGetValue(templateElementID, out Glyph glyph) ? glyph : defaultGamepadMap.TryGetValue(templateElementID, out glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a glyph that just has a description.
		/// </summary>
		/// <remarks>
		/// This is mainly used when an action is valid, has an element map, but that element can't be found in any of the glyph maps.
		/// Since we obviously can't assume what this element would look like with a sprite we at least give it a description so text glyph outputs can still function.<br/><br/>
		/// Ultimately this is a fallback mechanism and having specific element definitions for all inputs should be done.
		/// </remarks>
		internal Glyph GetFallbackGlyph(string name, ControllerType? controllerType)
		{
			(string name, ControllerType? controllerType) valueTuple = (name, controllerType);
			if (fallbackGlyphs.TryGetValue(valueTuple, out Glyph glyph))
			{
				return glyph;
			}

			Glyph fallbackGlyph = new Glyph(name, controllerType, nullGlyph.FullSprite);
			fallbackGlyphs.Add(valueTuple, fallbackGlyph);
			return fallbackGlyphs[valueTuple];
		}
	}
}