using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A <see cref="InputGlyphDisplay"/> element that will set it's <see cref="textMesh"/> and <see cref="image"/> components to the <see cref="InputGlyphDisplay.actionName"/>'s <see cref="InputGlyph.Description"/> and <see cref="InputGlyph.GetSprite(AxisRange)"/> values respectively.
/// </summary>
/// <remarks>
/// Using the <see cref="alwaysRenderBoth"/> as true can show both sprite and description elements where using it as false can utilize the text component as a fallback if the InputGlyph does not have a sprite defined.
/// </remarks>
public class InputGlyphHybrid : InputGlyphDisplay
{
	[SerializeField]
	private bool alwaysRenderBoth;

	private TMP_Text textMesh;
	private Image image;

	private void Awake()
	{
		textMesh = GetComponentInChildren<TMP_Text>();
		image = GetComponentInChildren<Image>();
	}

	public override void SetGlyph(InputGlyph glyph, AxisRange axisRange)
	{
		Sprite sprite = glyph.GetSprite(axisRange);
		image.enabled = sprite != null;
		image.sprite = sprite;

		textMesh.enabled = alwaysRenderBoth || sprite == null;
		textMesh.text = glyph.Description;
	}
}