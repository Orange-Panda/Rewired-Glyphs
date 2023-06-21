using JetBrains.Annotations;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// An input glyph display that shows both an image and text for a particular input glyph.
	/// </summary>
	/// <remarks>
	/// The text does not necessarily relate to the action and can be any set by <see cref="SetText"/>.
	/// The main purpose of this class is to automatically handle layout of the elements.
	/// </remarks>
	[PublicAPI]
	[AddComponentMenu("Rewired Glyphs/Labeled Image Output")]
	public class GlyphLabeledImageOutput : GlyphDisplay
	{
		[Header("Auto Layout")]
		[SerializeField, Tooltip("Space between the text and glyph icon")]
		private float spacing;
		[SerializeField, Tooltip("Padding from the edges of the parent rect transform and the graphic components")]
		private float padding;

		[Header("Component Reference")]
		[SerializeField, Tooltip("Image to set the glyph sprite to")]
		private Image image;
		[SerializeField, Tooltip("The text mesh to control the text of")]
		private TextMeshProUGUI textMesh;
		[SerializeField, Tooltip("The parent rect transform of the image and text mesh component")]
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

		public override void SetGlyph(Glyph glyph, AxisRange axisRange)
		{
			image.sprite = glyph.GetSprite(axisRange);
			SetLayout();
		}
	}
}