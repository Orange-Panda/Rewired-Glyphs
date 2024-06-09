using JetBrains.Annotations;
using Rewired;
using System;
using System.Collections.Generic;

namespace LMirman.RewiredGlyphs
{
	internal class RuntimeGlyphCollection
	{
		/// <summary>
		/// Automatically generated glyphs for inputs that have no glyph defined anywhere.
		/// </summary>
		private readonly Dictionary<string, Glyph> fallbackGlyphs = new Dictionary<string, Glyph>();
		/// <summary>
		/// Glyph mapping based on hardware specific glyphs and hardware specific input ids, found via hardware Guid.
		/// </summary>
		private readonly Dictionary<Guid, Dictionary<int, Glyph>> guidGlyphMaps = new Dictionary<Guid, Dictionary<int, Glyph>>();
		/// <summary>
		/// Glyph mapping based on hardware specific glyphs and hardware specific input ids, found via hardware type definition.
		/// </summary>
		private readonly Dictionary<HardwareDefinition, Dictionary<int, Glyph>> hardwareGlyphMaps = new Dictionary<HardwareDefinition, Dictionary<int, Glyph>>();
		/// <summary>
		/// Glyph mapping based on template glyphs and template input ids.
		/// </summary>
		private readonly Dictionary<SymbolPreference, Dictionary<int, Glyph>> templateGlyphMaps = new Dictionary<SymbolPreference, Dictionary<int, Glyph>>();
		internal readonly GlyphCollection collection;
		/// <inheritdoc cref="InputGlyphs.NullGlyph"/>
		internal readonly Glyph nullGlyph;
		/// <inheritdoc cref="InputGlyphs.UnboundGlyph"/>
		internal readonly Glyph unboundGlyph;
		/// <inheritdoc cref="InputGlyphs.UninitializedGlyph"/>
		internal readonly Glyph uninitializedGlyph;

		internal RuntimeGlyphCollection()
		{
			collection = null;
			guidGlyphMaps.Clear();
			hardwareGlyphMaps.Clear();
			templateGlyphMaps.Clear();
			uninitializedGlyph = new Glyph("Uninitialized", type: Glyph.Type.Uninitialized);
			unboundGlyph = new Glyph("Unbound", type: Glyph.Type.Unbound);
			nullGlyph = new Glyph("Null", type: Glyph.Type.Null);
		}

		internal RuntimeGlyphCollection(GlyphCollection collection)
		{
			this.collection = collection;

			// Create guid glyph lookup
			guidGlyphMaps.Clear();
			foreach (GlyphCollection.GuidEntry guidEntry in collection.GuidMaps)
			{
				guidGlyphMaps[guidEntry.GuidValue] = guidEntry.glyphMap.CreateDictionary();
				foreach (Glyph glyph in guidEntry.glyphMap.Glyphs)
				{
					// HACK: Big assumption here that every GUID reference is a Joystick.
					// I am not aware of a way to determine controller type of controllers not plugged in.
					// Maybe we can load from the Controller data file?
					glyph.ControllerType = ControllerType.Joystick;
				}
			}

			// Create hardware glyph lookup
			hardwareGlyphMaps.Clear();
			foreach (GlyphCollection.HardwareEntry entry in collection.HardwareMaps)
			{
				hardwareGlyphMaps[entry.hardwareDefinition] = entry.glyphMap.CreateDictionary();
				foreach (Glyph glyph in entry.glyphMap.Glyphs)
				{
					glyph.ControllerType = entry.hardwareDefinition switch
					{
						HardwareDefinition.Unknown => null,
						HardwareDefinition.Keyboard => ControllerType.Keyboard,
						HardwareDefinition.Mouse => ControllerType.Mouse,
						_ => ControllerType.Joystick
					};
				}
			}

			// Create template glyph lookup
			templateGlyphMaps.Clear();
			foreach (GlyphCollection.TemplateEntry entry in collection.TemplateMaps)
			{
				templateGlyphMaps[entry.symbolPreference] = entry.glyphMap.CreateDictionary();
				foreach (Glyph glyph in entry.glyphMap.Glyphs)
				{
					// We can safely assume any template entry is a joystick since template maps are used exclusively by that controller type.
					glyph.ControllerType = ControllerType.Joystick;
				}
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
		/// You are encouraged to use <see cref="InputGlyphs.GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> instead.
		/// </remarks>
		/// <param name="hardwareGuid">The hardware guid that the <see cref="elementID"/> maps to</param>
		/// <param name="elementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of this hardware's glyph map. Returns null (<b>NOT</b> <see cref="nullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public Glyph GetNativeGlyphFromGuidMap(Guid hardwareGuid, int elementID)
		{
			bool hasGuidGlyphMap = guidGlyphMaps.TryGetValue(hardwareGuid, out Dictionary<int, Glyph> value);
			return hasGuidGlyphMap && value.TryGetValue(elementID, out Glyph glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a glyph for this element id that belongs to a specific hardware setup, via hardware type.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="InputGlyphs.GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> or <see cref="GetKeyboardMouseGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> instead.
		/// </remarks>
		/// <param name="controller">The hardware type that the <see cref="elementID"/> maps to</param>
		/// <param name="elementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of this hardware's glyph map. Returns null (<b>NOT</b> <see cref="nullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public Glyph GetNativeGlyphFromHardwareMap(HardwareDefinition controller, int elementID)
		{
			bool hasHardwareGlyphMap = hardwareGlyphMaps.TryGetValue(controller, out Dictionary<int, Glyph> value);
			return hasHardwareGlyphMap && value.TryGetValue(elementID, out Glyph glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a <see cref="SymbolPreference"/> styled glyph for this <see cref="templateElementID"/> via the generic glyph mapping.
		/// </summary>
		/// <remarks>
		/// Usage of this method is not recommended in most cases and should only be used if you need fine control over glyph display.<br/><br/>
		/// You are encouraged to use <see cref="InputGlyphs.GetJoystickGlyph(int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,int,bool)"/> instead.
		/// </remarks>
		/// <param name="symbolPreference">The preferred symbol styling to present for this template element</param>
		/// <param name="templateElementID">The element input id to get a glyph for</param>
		/// <returns>The found <see cref="Glyph"/> inside of a template glyph map. Returns null (<b>NOT</b> <see cref="nullGlyph"/>) if none is found.</returns>
		[CanBeNull]
		public Glyph GetNativeGlyphFromTemplateMap(SymbolPreference symbolPreference, int templateElementID)
		{
			bool hasTemplateGlyphMap = templateGlyphMaps.TryGetValue(symbolPreference, out Dictionary<int, Glyph> templateGlyphMap);
			return hasTemplateGlyphMap && templateGlyphMap.TryGetValue(templateElementID, out Glyph glyph) ? glyph : null;
		}

		/// <summary>
		/// Retrieve a glyph that just has a description.
		/// </summary>
		/// <remarks>
		/// This is mainly used when an action is valid, has an element map, but that element can't be found in any of the glyph maps.
		/// Since we obviously can't assume what this element would look like with a sprite we at least give it a description so text glyph outputs can still function.<br/><br/>
		/// Ultimately this is a fallback mechanism and having specific element definitions for all inputs should be done.
		/// </remarks>
		internal Glyph GetFallbackGlyph(string name)
		{
			if (!fallbackGlyphs.ContainsKey(name))
			{
				fallbackGlyphs.Add(name, new Glyph(name, nullGlyph.FullSprite));
			}

			return fallbackGlyphs[name];
		}
	}
}