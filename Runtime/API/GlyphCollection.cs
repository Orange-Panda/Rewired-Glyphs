using JetBrains.Annotations;
using Rewired;
using Rewired.Data;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A <see cref="ScriptableObject"/> asset that references <see cref="GlyphMap"/> assets that should be utilized at runtime.
	/// </summary>
	[PublicAPI]
	[CreateAssetMenu(menuName = "Rewired Glyphs/Glyph Collection")]
	public class GlyphCollection : ScriptableObject
	{
		[SerializeField, UsedImplicitly, Tooltip("Used for validation purposes. Is not required but will significantly improve editor experience.")]
		private ControllerDataFiles controllerDataFiles;
		/// <inheritdoc cref="Key"/>
		[Tooltip("A unique alphanumeric key used for referencing this collection at runtime if it is not the default collection.\n\n" +
		         "Must not contain spaces, special characters, or casing.")]
		[SerializeField]
		private string key = "default";

		// ---- [ MAPS ] ----

		/// <inheritdoc cref="GuidMaps"/>
		[Tooltip("Maps that associate glyphs with action ids of hardware with a specific guid.")]
		[SerializeField]
		private GuidEntry[] guidMaps = Array.Empty<GuidEntry>();
		/// <inheritdoc cref="TemplateMaps"/>
		[FormerlySerializedAs("genericMaps")]
		[Space]
		[Tooltip("Maps that associate glyphs with action ids of controller templates.")]
		[SerializeField]
		private TemplateEntry[] templateMaps = Array.Empty<TemplateEntry>();
		/// <inheritdoc cref="unboundGlyph"/>

		// ---- [ GLYPHS ] ----
		[Space]
		[Tooltip("The glyph to be shown if the action exists but there is no input mapping to it.")]
		[SerializeField]
		private Glyph unboundGlyph = new Glyph(-1, "UNBOUND", null, null, Glyph.Type.Unbound);
		/// <inheritdoc cref="nullGlyph"/>
		[Space]
		[Tooltip("The glyph to be shown if the action does not exist, usually as a result of an invalid action id query.")]
		[SerializeField]
		private Glyph nullGlyph = new Glyph(-1, "NULL", null, null, Glyph.Type.Null);
		[Space]
		[Tooltip("The glyph to be shown if the input system is not ready to show input icons, usually due to edit mode query or inactive input system")]
		[SerializeField]
		private Glyph uninitializedGlyph = new Glyph(-1, "UNINITIALIZED", null, null, Glyph.Type.Uninitialized);

		/// <summary>
		/// A unique alphanumeric key used for referencing this collection at runtime if it is not the default collection.
		/// </summary>
		/// <remarks>
		/// Must not contain spaces, special characters, or casing.
		/// </remarks>
		public string Key => key ?? string.Empty;

		/// <summary>
		/// The GlyphMap to fallback to if there is no glyph map found for a particular SymbolPreference
		/// </summary>
		[CanBeNull]
		internal GlyphMap DefaultGamepadTemplateMap => templateMaps.FirstOrDefault(entry => entry.SymbolPreference == SymbolPreference.Auto)?.GlyphMap ?? templateMaps.FirstOrDefault()?.GlyphMap;

		/// <summary>
		/// Maps that associate glyphs with action ids of specific controllers.
		/// </summary>
		public GuidEntry[] GuidMaps => guidMaps;

		/// <summary>
		/// Maps that associate glyphs with action ids of controller templates.
		/// </summary>
		public TemplateEntry[] TemplateMaps => templateMaps;

		/// <summary>
		/// The glyph to be shown if the action exists but there is no input mapping to it.
		/// </summary>
		public Glyph UnboundGlyph => unboundGlyph;

		/// <summary>
		/// The glyph to be shown if the action does not exist, usually as a result of an invalid action id query.
		/// </summary>
		public Glyph NullGlyph => nullGlyph;

		/// <summary>
		/// The glyph to be shown if the input system is not ready to show input icons, usually due to edit mode query or inactive input system
		/// </summary>
		public Glyph UninitializedGlyph => uninitializedGlyph;

		[Serializable]
		public class GuidEntry
		{
			[SerializeField]
			private ControllerType controllerType;
			[Tooltip("Use this entry's glyphMap for a user that is querying for a glyph while using hardware with this guid.")]
			[SerializeField]
			private string guid = "d74a350e-fe8b-4e9e-bbcd-efff16d34115";
			[SerializeField]
			private GlyphMap glyphMap;

			public ControllerType ControllerType => controllerType;

			[CanBeNull]
			public GlyphMap GlyphMap => glyphMap;

			public Guid GuidValue =>
				controllerType switch
				{
					ControllerType.Keyboard => ReInput.isReady ? ReInput.controllers.Keyboard.hardwareTypeGuid : Guid.Empty,
					ControllerType.Mouse => ReInput.isReady ? ReInput.controllers.Mouse.hardwareTypeGuid : Guid.Empty,
					ControllerType.Joystick => Guid.TryParse(guid, out Guid guidValue) ? guidValue : Guid.Empty,
					ControllerType.Custom => Guid.TryParse(guid, out Guid guidValue) ? guidValue : Guid.Empty,
					_ => throw new ArgumentOutOfRangeException()
				};
		}

		/// <summary>
		/// A template entry is used for assigning a <see cref="GlyphMap"/> to a <see cref="GlyphCollection"/> and associating it with a particular <see cref="SymbolPreference"/>.
		/// </summary>
		/// <remarks>
		/// Only `Joystick` glyph maps that map to the `Gamepad` template should be used here.
		/// <br/><br/>
		/// If there is no map defined for a particular <see cref="SymbolPreference"/> will use <see cref="GlyphCollection.DefaultGamepadTemplateMap"/> as a fallback.
		/// <br/><br/>
		/// The <see cref="symbolPreference"/> value is a unique key for the glyph map at runtime.
		/// Therefore, having multiple template entries with the same <see cref="symbolPreference"/> value should be avoided since only the latest one will be available.
		/// </remarks>
		[Serializable]
		public class TemplateEntry
		{
			[Tooltip("Use this entry's glyphMap for a user that is querying for a template glyph with this SymbolPreference.")]
			[FormerlySerializedAs("controllerType")]
			[SerializeField]
			private SymbolPreference symbolPreference;
			[SerializeField]
			private GlyphMap glyphMap;

			public SymbolPreference SymbolPreference => symbolPreference;

			[CanBeNull]
			public GlyphMap GlyphMap => glyphMap;
		}

		private void OnValidate()
		{
			key = key.ToCleansedCollectionKey();
		}
	}
}