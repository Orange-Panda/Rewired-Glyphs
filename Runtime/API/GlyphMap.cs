using JetBrains.Annotations;
using Rewired.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A set of glyphs to be used for a particular input device or template.
	/// </summary>
	[CreateAssetMenu(menuName = "Rewired Glyphs/Glyph Map")]
	public class GlyphMap : ScriptableObject
	{
		[SerializeField, UsedImplicitly, Tooltip("Used for validation purposes. Is not required but will significantly improve editor experience.")]
		private ControllerDataFiles controllerDataFiles;
		[SerializeField, UsedImplicitly, Tooltip("Used for validation purposes. The GUID of the device we intend to validate for.")]
		private string controllerGuid;

		[SerializeField]
		private Glyph[] glyphs = Array.Empty<Glyph>();

		public Glyph[] Glyphs => glyphs;

		public Dictionary<int, Glyph> CreateDictionary()
		{
			return glyphs.ToDictionary(current => current.InputID);
		}
	}
}