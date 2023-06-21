using Rewired;
using UnityEngine;
using UnityEngine.UI;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// A <see cref="GlyphDisplay"/> element that will set it's <see cref="image"/> component to the <see cref="GlyphDisplay.actionName"/>'s <see cref="Glyph.GetSprite(AxisRange)"/> value.
	/// </summary>
	[AddComponentMenu("Rewired Glyphs/Image Output")]
	[RequireComponent(typeof(Image))]
	public class GlyphImageOutput : GlyphDisplay
	{
		private Image image;

		private void Awake()
		{
			image = GetComponent<Image>();
		}

		public override void SetGlyph(Glyph glyph, AxisRange axisRange)
		{
			Sprite sprite = glyph.GetSprite(axisRange);
			image.sprite = sprite;
			image.enabled = sprite != null;
		}
	}
}