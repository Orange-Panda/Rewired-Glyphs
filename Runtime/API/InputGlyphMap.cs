using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

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
	// TODO: Make separate methods for each type of controller.
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