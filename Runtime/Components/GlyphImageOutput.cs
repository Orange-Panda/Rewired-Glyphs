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

		protected override void Awake()
		{
			base.Awake();
			image = GetComponent<Image>();
		}

		public override void SetGlyph(Glyph glyph, AxisRange axisRange)
		{
			bool shouldShow = !ShouldHideGlyph(glyph);
			Sprite sprite = glyph.GetSprite(axisRange);
			image.sprite = sprite;
			image.enabled = shouldShow && sprite != null;
		}
	}
}