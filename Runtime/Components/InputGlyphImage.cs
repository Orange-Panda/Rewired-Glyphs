using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// A <see cref="InputGlyphDisplay"/> element that will set it's <see cref="image"/> component to the <see cref="InputGlyphDisplay.actionName"/>'s <see cref="InputGlyph.GetSprite(AxisRange)"/> value.
	/// </summary>
	[RequireComponent(typeof(Image))]
	public class InputGlyphImage : InputGlyphDisplay
	{
		private Image image;

		private void Awake()
		{
			image = GetComponent<Image>();
		}

		public override void SetGlyph(InputGlyph glyph, AxisRange axisRange)
		{
			Sprite sprite = glyph.GetSprite(axisRange);
			image.sprite = sprite;
			image.enabled = sprite != null;
		}
	}
}