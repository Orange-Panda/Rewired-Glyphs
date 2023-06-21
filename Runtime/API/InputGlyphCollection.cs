using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A <see cref="ScriptableObject"/> asset that references <see cref="InputGlyphMap"/> assets that should be utilized at runtime.
	/// </summary>
	[PublicAPI]
	[CreateAssetMenu(menuName = "Rewired Glyphs/Glyph Collection")]
	public class InputGlyphCollection : ScriptableObject
	{
		[FormerlySerializedAs("maps")]
		[Tooltip("Maps to be used on specific user hardware.")]
		[SerializeField]
		private HardwareEntry[] hardwareMaps = Array.Empty<HardwareEntry>();
		[FormerlySerializedAs("genericMaps")]
		[Space]
		[Tooltip("Template maps to be used when unable to find hardware specific glyphs.")]
		[SerializeField]
		private TemplateEntry[] templateMaps = Array.Empty<TemplateEntry>();
		[Space]
		[Tooltip("The glyph to be shown if the action exists but there is not input mapping to it.")]
		[SerializeField]
		private InputGlyph unboundGlyph;
		[Space]
		[Tooltip("The glyph to be shown if the action does not exist, usually as a result of an action name mismatch.")]
		[SerializeField]
		private InputGlyph nullGlyph;

		public HardwareEntry[] HardwareMaps => hardwareMaps;
		public TemplateEntry[] TemplateMaps => templateMaps;
		public InputGlyph UnboundGlyph => unboundGlyph;
		public InputGlyph NullGlyph => nullGlyph;

		[Serializable]
		public class HardwareEntry
		{
			public HardwareDefinition controllerType;
			public InputGlyphMap glyphMap;
		}

		[Serializable]
		public class TemplateEntry
		{
			public SymbolPreference controllerType;
			public InputGlyphMap glyphMap;
		}
	}
}