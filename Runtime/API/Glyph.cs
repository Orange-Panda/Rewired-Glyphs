using JetBrains.Annotations;
using Rewired;
using System;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Data about a particular input including <see cref="Sprite"/> and <see cref="string"/>. 
	/// </summary>
	[Serializable, PublicAPI]
	public class Glyph
	{
		/// <inheritdoc cref="InputID"/>
		[SerializeField]
		private int inputID;
		/// <inheritdoc cref="Description"/>
		[SerializeField]
		private string description;
		/// <inheritdoc cref="Sprite"/>
		[SerializeField]
		private Sprite sprite;
		/// <inheritdoc cref="PositiveSprite"/>
		[SerializeField]
		private Sprite positiveSprite;
		/// <inheritdoc cref="NegativeSprite"/>
		[SerializeField]
		private Sprite negativeSprite;
		/// <inheritdoc cref="TextMeshSpriteSheetName"/>
		[SerializeField]
		private string textMeshSpriteSheetName;

		/// <summary>
		/// The ID for this glyph in the context of this glyph map.
		/// </summary>
		/// <remarks>
		/// Please reference the official Rewired documentation for more information about what element ids map to for each hardware device.<br/>
		/// https://guavaman.com/projects/rewired/docs/HowTos.html#display-glyph-for-action
		/// </remarks>
		public int InputID => inputID;
		/// <summary>
		/// Description of the input on the device.
		/// </summary>
		public string Description => description;
		/// <summary>
		/// Get the sprite for the <see cref="AxisRange.Full"/>
		/// </summary>
		public Sprite FullSprite => sprite;
		/// <summary>
		/// Explicitly get the sprite for the <see cref="AxisRange.Positive"/>.
		/// </summary>
		/// <remarks>
		/// Does not fallback to <see cref="FullSprite"/> unlike <see cref="GetSprite"/>. Will be null if <see cref="positiveSprite"/> is undefined.
		/// </remarks>
		public Sprite PositiveSprite => positiveSprite;
		/// <summary>
		/// Explicitly get the sprite for the <see cref="AxisRange.Negative"/>.
		/// </summary>
		/// <remarks>
		/// Does not fallback to <see cref="FullSprite"/> unlike <see cref="GetSprite"/>. Will be null if <see cref="negativeSprite"/> is undefined.
		/// </remarks>
		public Sprite NegativeSprite => negativeSprite;
		/// <summary>
		/// The name of the text mesh sprite sheet that contains the sprites for this glyph.
		/// </summary>
		/// <remarks>
		/// Used by <see cref="LMirman.RewiredGlyphs.Components.GlyphRichTextFormatter"/> to display in line glyphs in Text Mesh Pro text.
		/// </remarks>
		public string TextMeshSpriteSheetName
		{
			get => textMeshSpriteSheetName;
			set => textMeshSpriteSheetName = value;
		}

		/// <summary>
		/// Get the sprite for a particular input direction.
		/// </summary>
		/// <param name="axis">The direction of input.</param>
		/// <returns>The sprite define for the <see cref="AxisRange"/> or the <see cref="AxisRange.Full"/> if one is not found for the positive or negative direction.</returns>
		public Sprite GetSprite(AxisRange axis)
		{
			switch (axis)
			{
				case AxisRange.Full:
					return sprite;
				case AxisRange.Positive:
					return positiveSprite != null ? positiveSprite : sprite;
				case AxisRange.Negative:
					return negativeSprite != null ? negativeSprite : sprite;
				default:
					return sprite;
			}
		}

		public Glyph(int inputID, string description, Sprite sprite, Sprite positiveSprite, Sprite negativeSprite)
		{
			this.inputID = inputID;
			this.description = description;
			this.sprite = sprite;
			this.positiveSprite = positiveSprite;
			this.negativeSprite = negativeSprite;
		}

		public Glyph(int inputID, string description, Sprite sprite = null)
		{
			this.inputID = inputID;
			this.description = description;
			this.sprite = sprite;
			positiveSprite = null;
			negativeSprite = null;
		}

		/// <summary>
		/// Create a fallback glyph that only contains a description.
		/// </summary>
		/// <remarks>The main use case is if there is no glyph found but there is <i>some</i> information about the input that can allow it to be shown in text.</remarks>
		public Glyph(string description)
		{
			inputID = -1;
			this.description = description;
			sprite = null;
			positiveSprite = null;
			negativeSprite = null;
		}
	}
}