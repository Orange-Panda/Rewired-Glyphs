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
		[SerializeField]
		private int inputID;
		[SerializeField]
		private string description;
		[SerializeField]
		private Sprite sprite;
		[SerializeField]
		private Sprite positiveSprite;
		[SerializeField]
		private Sprite negativeSprite;
		[SerializeField]
		private string textMeshSpriteSheetName;

		/// <summary>
		/// The id for the input on the Input Manager.
		/// </summary>
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