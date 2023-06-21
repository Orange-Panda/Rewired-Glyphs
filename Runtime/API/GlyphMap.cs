using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A set of input glyphs for a particular circumstance, such as hardware device or template scheme.
	/// </summary>
	[CreateAssetMenu(menuName = "Rewired Glyphs/Glyph Map")]
	public class GlyphMap : ScriptableObject
	{
		[SerializeField]
		private Glyph[] glyphs = Array.Empty<Glyph>();

		public Dictionary<int, Glyph> CreateDictionary()
		{
			// TODO: This could probably be cached.
			Dictionary<int, Glyph> lookup = new Dictionary<int, Glyph>();
			foreach (Glyph current in glyphs)
			{
				lookup.Add(current.InputID, current);
			}

			return lookup;
		}

		// Easy way to quickly populate the glyph fields with ids and names.
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