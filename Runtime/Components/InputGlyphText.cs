using Rewired;
using TMPro;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A <see cref="InputGlyphDisplay"/> element that will set it's <see cref="textMesh"/> component to the <see cref="InputGlyphDisplay.actionName"/>'s <see cref="InputGlyph.Description"/>
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	public class InputGlyphText : InputGlyphDisplay
	{
		private TMP_Text textMesh;

		private void Awake()
		{
			textMesh = GetComponent<TMP_Text>();
		}

		public override void SetGlyph(InputGlyph glyph, AxisRange axisRange)
		{
			textMesh.text = glyph.Description;
		}
	}
}