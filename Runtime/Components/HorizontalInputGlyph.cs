using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HorizontalInputGlyph : InputGlyphDisplay
{
	[Header("Auto Layout")]
	[SerializeField]
	private float spacing;
	[SerializeField]
	private float padding;

	[Header("Component Reference")]
	[SerializeField]
	private Image image;
	[SerializeField]
	private TextMeshProUGUI textMesh;
	[SerializeField]
	private RectTransform rectTransform;

	public void SetText(string text)
	{
		textMesh.text = text;
		SetLayout();
	}

	[ContextMenu("Set Layout")]
	public void SetLayout()
	{
		float textWidth = textMesh.preferredWidth;
		float imageWidth = image.rectTransform.rect.width;
		image.rectTransform.anchorMin = new Vector2(0, 0.5f);
		image.rectTransform.anchorMax = new Vector2(0, 0.5f);
		image.rectTransform.pivot = new Vector2(0, 0.5f);
		textMesh.rectTransform.anchorMin = new Vector2(0, 0.5f);
		textMesh.rectTransform.anchorMax = new Vector2(0, 0.5f);
		textMesh.rectTransform.pivot = new Vector2(0, 0.5f);
		rectTransform.sizeDelta = new Vector2(textWidth + imageWidth + padding + spacing, rectTransform.sizeDelta.y);
		textMesh.rectTransform.sizeDelta = new Vector2(textWidth + padding, textMesh.rectTransform.sizeDelta.y);
		image.rectTransform.anchoredPosition = new Vector2(padding, 0);
		textMesh.rectTransform.anchoredPosition = new Vector2(padding + spacing + imageWidth, 0);
		LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
	}

	public override void SetGlyph(InputGlyph glyph, AxisRange axisRange)
	{
		image.sprite = glyph.GetSprite(axisRange);
		SetLayout();
	}
}