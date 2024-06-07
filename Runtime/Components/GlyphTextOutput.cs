using Rewired;
using TMPro;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// A <see cref="GlyphDisplay"/> element that will set it's <see cref="textMesh"/> component to the <see cref="GlyphDisplay.actionName"/>'s <see cref="Glyph.GetDescription"/>
	/// </summary>
	[AddComponentMenu("Rewired Glyphs/Text Output")]
	[RequireComponent(typeof(TMP_Text))]
	public class GlyphTextOutput : GlyphDisplay
	{
		private TMP_Text textMesh;

		protected override void Awake()
		{
			base.Awake();
			textMesh = GetComponent<TMP_Text>();
		}

		public override void SetGlyph(Glyph glyph, AxisRange axisRange)
		{
			textMesh.text = ShouldHideGlyph(glyph) ? string.Empty : glyph.GetDescription(axisRange);
		}
	}
}