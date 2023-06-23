using Rewired;
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

		[ContextMenu("Generate Mouse")]
		private void GenerateMouse()
		{
			List<Glyph> newGlyphs = new List<Glyph>();
			Mouse mouse = ReInput.controllers.Mouse;
			foreach (ControllerElementIdentifier element in mouse.ElementIdentifiers)
			{
				newGlyphs.Add(new Glyph(element.id, element.name));
			}

			glyphs = newGlyphs.ToArray();
		}

		[ContextMenu("Generate Keyboard")]
		private void GenerateKeyboard()
		{
			List<Glyph> newGlyphs = new List<Glyph>();
			Keyboard keyboard = ReInput.controllers.Keyboard;
			foreach (ControllerElementIdentifier element in keyboard.ElementIdentifiers)
			{
				newGlyphs.Add(new Glyph(element.id, element.name));
			}

			glyphs = newGlyphs.ToArray();
		}
	}
}