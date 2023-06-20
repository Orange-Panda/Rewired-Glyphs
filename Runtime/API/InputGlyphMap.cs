using Rewired;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of input glyphs for a particular circumstance, such as hardware device or template scheme.
/// </summary>
[CreateAssetMenu(menuName = "ENA Dream BBQ/UI/Input Glyph Map")]
public class InputGlyphMap : ScriptableObject
{
	[SerializeField]
	private InputGlyph[] glyphs = new InputGlyph[0];

	public Dictionary<int, InputGlyph> CreateDictionary()
	{
		Dictionary<int, InputGlyph> lookup = new Dictionary<int, InputGlyph>();
		for (int i = 0; i < glyphs.Length; i++)
		{
			InputGlyph current = glyphs[i];
			lookup.Add(current.InputID, current);
		}

		return lookup;
	}

	// Easy way to quickly populate the glyph fields with ids and names.
	[ContextMenu("Generate")]
	private void Generate()
	{
		List<InputGlyph> newGlyphs = new List<InputGlyph>();
		Mouse mouse = ReInput.controllers.Mouse;
		foreach (ControllerElementIdentifier element in mouse.ElementIdentifiers)
		{
			newGlyphs.Add(new InputGlyph(element.id, element.name));
		}

		glyphs = newGlyphs.ToArray();
	}
}