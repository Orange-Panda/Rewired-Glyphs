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
	public class InputGlyphMap : ScriptableObject
	{
		[SerializeField]
		private InputGlyph[] glyphs = Array.Empty<InputGlyph>();

		public Dictionary<int, InputGlyph> CreateDictionary()
		{
			// TODO: This could probably be cached.
			Dictionary<int, InputGlyph> lookup = new Dictionary<int, InputGlyph>();
			foreach (InputGlyph current in glyphs)
			{
				lookup.Add(current.InputID, current);
			}

			return lookup;
		}

		// Easy way to quickly populate the glyph fields with ids and names.
		[ContextMenu("Generate Mouse")]
		private void GenerateMouse()
		{
			List<InputGlyph> newGlyphs = new List<InputGlyph>();
			Mouse mouse = ReInput.controllers.Mouse;
			foreach (ControllerElementIdentifier element in mouse.ElementIdentifiers)
			{
				newGlyphs.Add(new InputGlyph(element.id, element.name));
			}

			glyphs = newGlyphs.ToArray();
		}

		[ContextMenu("Generate Keyboard")]
		private void GenerateKeyboard()
		{
			List<InputGlyph> newGlyphs = new List<InputGlyph>();
			Keyboard keyboard = ReInput.controllers.Keyboard;
			foreach (ControllerElementIdentifier element in keyboard.ElementIdentifiers)
			{
				newGlyphs.Add(new InputGlyph(element.id, element.name));
			}

			glyphs = newGlyphs.ToArray();
		}
	}
}