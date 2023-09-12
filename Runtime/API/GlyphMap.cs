using JetBrains.Annotations;
using Rewired.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A set of glyphs for an input device.
	/// Glyph maps can map to a particular hardware device or a controller template.
	/// </summary>
	[CreateAssetMenu(menuName = "Rewired Glyphs/Glyph Map")]
	public class GlyphMap : ScriptableObject
	{
		[SerializeField, UsedImplicitly]
		private ControllerDataFiles controllerDataFiles;
		[SerializeField, UsedImplicitly]
		private string controllerGuid;

		[SerializeField]
		private Glyph[] glyphs = Array.Empty<Glyph>();

		public Glyph[] Glyphs => glyphs;

		public Dictionary<int, Glyph> CreateDictionary()
		{
			Dictionary<int, Glyph> lookup = new Dictionary<int, Glyph>();
			foreach (Glyph current in glyphs)
			{
				lookup.Add(current.InputID, current);
			}

			return lookup;
		}
	}
}