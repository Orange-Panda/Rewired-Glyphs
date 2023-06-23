using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// A <see cref="GlyphDisplay"/> element that will set it's <see cref="textMesh"/> and <see cref="image"/> components to the <see cref="GlyphDisplay.actionName"/>'s <see cref="Glyph.Description"/> and <see cref="Glyph.GetSprite(AxisRange)"/> values respectively.
	/// </summary>
	/// <remarks>
	/// Using the <see cref="alwaysRenderBoth"/> as true can show both sprite and description elements where using it as false can utilize the text component as a fallback if the InputGlyph does not have a sprite defined.
	/// </remarks>
	[AddComponentMenu("Rewired Glyphs/Image and Text Hybrid Output")]
	public class GlyphHybridOutput : GlyphDisplay
	{
		[SerializeField]
		private bool alwaysRenderBoth;

		private TMP_Text textMesh;
		private Image image;

		protected override void Awake()
		{
			base.Awake();
			textMesh = GetComponentInChildren<TMP_Text>();
			image = GetComponentInChildren<Image>();
		}

		public override void SetGlyph(Glyph glyph, AxisRange axisRange)
		{
			Sprite sprite = glyph.GetSprite(axisRange);
			bool useSprite = sprite != null && !glyph.PreferDescription;
			image.enabled = useSprite;
			image.sprite = sprite;

			textMesh.enabled = alwaysRenderBoth || !useSprite;
			textMesh.text = glyph.Description;
		}
	}
}