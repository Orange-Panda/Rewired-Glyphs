using JetBrains.Annotations;
using System;
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
		/// <inheritdoc cref="GuidMaps"/>
		[Tooltip("Maps that associate glyphs with action ids of hardware with a specific guid.")]
		[SerializeField]
		private GuidEntry[] guidMaps = Array.Empty<GuidEntry>();
		/// <inheritdoc cref="HardwareMaps"/>
		[FormerlySerializedAs("maps")]
		[Space]
		[Tooltip("Maps that associate glyphs with action ids of specific hardware types.")]
		[SerializeField]
		private HardwareEntry[] hardwareMaps = Array.Empty<HardwareEntry>();
		/// <inheritdoc cref="TemplateMaps"/>
		[FormerlySerializedAs("genericMaps")]
		[Space]
		[Tooltip("Maps that associate glyphs with action ids of controller templates.")]
		[SerializeField]
		private TemplateEntry[] templateMaps = Array.Empty<TemplateEntry>();
		/// <inheritdoc cref="unboundGlyph"/>
		[Space]
		[Tooltip("The glyph to be shown if the action exists but there is no input mapping to it.")]
		[SerializeField]
		private Glyph unboundGlyph;
		/// <inheritdoc cref="nullGlyph"/>
		[Space]
		[Tooltip("The glyph to be shown if the action does not exist, usually as a result of an invalid action id query.")]
		[SerializeField]
		private Glyph nullGlyph;

		/// <summary>
		/// Maps that associate glyphs with action ids of hardware with a specific guid.
		/// </summary>
		public GuidEntry[] GuidMaps => guidMaps;
		/// <summary>
		/// Maps that associate glyphs with action ids of specific hardware types.
		/// </summary>
		public HardwareEntry[] HardwareMaps => hardwareMaps;
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
		/// A hardware entry associates a glyph map with specific hardware action ids.
		/// </summary>
		/// <remarks>
		/// Including at least a keyboard and mouse glyph map here is highly encouraged.
		/// Otherwise there will be no glyphs presented for this input mode.
		/// <br/>
		/// <br/>
		/// The <see cref="hardwareDefinition"/> is the key for the glyph map, thus having multiple template entries with the same <see cref="hardwareDefinition"/> value should be avoided.
		/// </remarks>
		[Serializable]
		public class HardwareEntry
		{
			[Tooltip("Use this entry's glyphMap for a user that is querying for a glyph while using this type of hardware.")]
			[FormerlySerializedAs("controllerType")]
			public HardwareDefinition hardwareDefinition;
			public GlyphMap glyphMap;
		}
		
		[Serializable]
		public class GuidEntry
		{
			[Tooltip("Use this entry's glyphMap for a user that is querying for a glyph while using hardware with this guid.")]
			[SerializeField]
			private string guid = "d74a350e-fe8b-4e9e-bbcd-efff16d34115";
			public GlyphMap glyphMap;

			private Guid? guidValue;
			public Guid GuidValue => guidValue ??= new Guid(guid);
		}

		/// <summary>
		/// A template entry associates a glyph map with a particular <see cref="SymbolPreference"/>.
		/// </summary>
		/// <remarks>
		/// Only controller inputs will use templates so keyboard and mouse glyphs should not be included.
		/// <br/>
		/// <br/>
		/// When checking for a specific <see cref="SymbolPreference"/> and it is not found the <see cref="SymbolPreference.Auto"/> will be used as a fallback.
		/// Therefore you can consider a TemplateEntry with <see cref="SymbolPreference.Auto"/> as the default glyph display.
		/// <br/>
		/// <br/>
		/// The <see cref="symbolPreference"/> is the key for the glyph map, thus having multiple template entries with the same <see cref="symbolPreference"/> value should be avoided.
		/// </remarks>
		[Serializable]
		public class TemplateEntry
		{
			[Tooltip("Use this entry's glyphMap for a user that is querying for a template glyph with this SymbolPreference.")]
			[FormerlySerializedAs("controllerType")]
			public SymbolPreference symbolPreference;
			public GlyphMap glyphMap;
		}
	}
}