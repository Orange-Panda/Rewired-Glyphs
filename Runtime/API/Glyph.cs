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
		/// <inheritdoc cref="FullDescription"/>
		[SerializeField]
		private string description;
		/// <inheritdoc cref="PositiveDescription"/>
		[SerializeField]
		private string positiveDescription;
		/// <inheritdoc cref="NegativeDescription"/>
		[SerializeField]
		private string negativeDescription;
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
		/// Deprecated description of the input that formerly represent an absolute description of the Glyph.
		/// Superseded by <see cref="GetDescription"/> and <see cref="FullDescription"/>.
		/// </summary>
		/// <seealso cref="GetDescription"/><seealso cref="FullDescription"/>.
		[Obsolete("Description is obsolete since it does not represent a particular axis. " +
		          "Use GetDescription(AxisRange) to get a dynamic description or FullDescription to explicitly get the AxisRange.Full description.")]
		public string Description => description;

		/// <summary>
		/// The description of this input for the device for the <see cref="AxisRange.Full"/> axis.
		/// </summary>
		/// <remarks>
		/// You should usually use <see cref="GetDescription"/> instead.
		/// </remarks>
		/// <seealso cref="GetDescription"/>
		public string FullDescription => description;

		/// <summary>
		/// The description of this input for the device for the <see cref="AxisRange.Positive"/> axis.
		/// </summary>
		/// <remarks>
		/// Does not fallback to <see cref="FullDescription"/> unlike <see cref="GetDescription"/>.<br/><br/>
		/// You should usually use <see cref="GetDescription"/> instead.
		/// </remarks>
		/// <seealso cref="GetDescription"/>
		public string PositiveDescription => positiveDescription;

		/// <summary>
		/// The description of this input for the device for the <see cref="AxisRange.Negative"/> axis.
		/// </summary>
		/// <remarks>
		/// Does not fallback to <see cref="FullDescription"/> unlike <see cref="GetDescription"/>.<br/><br/>
		/// You should usually use <see cref="GetDescription"/> instead.
		/// </remarks>
		/// <seealso cref="GetDescription"/>
		public string NegativeDescription => negativeDescription;

		/// <summary>
		/// The sprite representing this input for the <see cref="AxisRange.Full"/> axis.
		/// </summary>
		/// <remarks>
		/// You should usually use <see cref="GetSprite"/> instead.
		/// </remarks>
		/// <seealso cref="GetSprite"/>
		public Sprite FullSprite => sprite;

		/// <summary>
		/// The sprite representing this input for the <see cref="AxisRange.Positive"/> axis.
		/// </summary>
		/// <remarks>
		/// Does not fallback to <see cref="FullSprite"/> unlike <see cref="GetSprite"/>.
		/// Will be null if <see cref="positiveSprite"/> is undefined.<br/><br/>
		/// You should usually use <see cref="GetSprite"/> instead.
		/// </remarks>
		/// <seealso cref="GetSprite"/>
		public Sprite PositiveSprite => positiveSprite;

		/// <summary>
		/// The sprite representing this input for the <see cref="AxisRange.Negative"/> axis.
		/// </summary>
		/// <remarks>
		/// Does not fallback to <see cref="FullSprite"/> unlike <see cref="GetSprite"/>.
		/// Will be null if <see cref="negativeSprite"/> is undefined.<br/><br/>
		/// You should usually use <see cref="GetSprite"/> instead.
		/// </remarks>
		/// <seealso cref="GetSprite"/>
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
		/// When true this Glyph should show as text instead of its sprite.
		/// </summary>
		public bool PreferDescription { get; set; }

		/// <summary>
		/// What this Glyph is intended to represent at runtime.
		/// </summary>
		/// <seealso cref="Type"/>
		public Type GlyphType { get; internal set; }

		/// <summary>
		/// The <see cref="Rewired.ControllerType"/> that this Glyph intends to represent.
		/// </summary>
		/// <remarks>
		/// <b>Important:</b> Depending on the value of <see cref="GlyphType"/> this value may be null if it doesn't represent a specific device.<br/><br/>
		/// For example: a <see cref="Type.Null"/> or <see cref="Type.Uninitialized"/> glyph will have a null value since it doesn't represent any specific device.
		/// </remarks>
		[CanBeNull]
		public ControllerType? ControllerType { get; internal set; }

		/// <summary>
		/// True when <see cref="GlyphType"/> is <see cref="Type.Input"/>.
		/// </summary>
		public bool IsInputGlyph => GlyphType == Type.Input;

		/// <summary>
		/// True when <see cref="GlyphType"/> is <i>not</i> <see cref="Type.Undefined"/> or <see cref="Type.Input"/>.
		/// </summary>
		/// <remarks>
		/// Consider evaluating this in custom components to hide the output when there isn't an appropriate element to output.
		/// </remarks>
		public bool IsFallbackGlyph => GlyphType != Type.Undefined && GlyphType != Type.Input;

		/// <summary>
		/// Get the description for a particular input direction.
		/// </summary>
		/// <param name="axis">The direction of input.</param>
		/// <returns>The description defined for the <see cref="AxisRange"/> or the <see cref="AxisRange.Full"/> if one is not found for the positive or negative direction.</returns>
		public string GetDescription(AxisRange axis)
		{
			switch (axis)
			{
				case AxisRange.Full:
					return description ?? string.Empty;
				case AxisRange.Positive:
					return !string.IsNullOrWhiteSpace(positiveDescription) ? positiveDescription : description ?? string.Empty;
				case AxisRange.Negative:
					return !string.IsNullOrWhiteSpace(negativeDescription) ? negativeDescription : description ?? string.Empty;
				default:
					return description ?? string.Empty;
			}
		}

		/// <summary>
		/// Get the sprite for a particular input direction.
		/// </summary>
		/// <param name="axis">The direction of input.</param>
		/// <returns>The sprite defined for the <see cref="AxisRange"/> or the <see cref="AxisRange.Full"/> if one is not found for the positive or negative direction.</returns>
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

		public Glyph(int inputID, string description, ControllerType? controllerType, Sprite sprite, Sprite positiveSprite, Sprite negativeSprite, Type type = Type.Input)
		{
			this.inputID = inputID;
			this.description = description;
			this.sprite = sprite;
			this.positiveSprite = positiveSprite;
			this.negativeSprite = negativeSprite;
			PreferDescription = false;
			GlyphType = type;
			ControllerType = controllerType;
		}

		public Glyph(int inputID, string description, ControllerType? controllerType, Sprite sprite, Type type = Type.Input)
		{
			this.inputID = inputID;
			this.description = description;
			this.sprite = sprite;
			positiveSprite = null;
			negativeSprite = null;
			PreferDescription = sprite != null;
			GlyphType = type;
			ControllerType = controllerType;
		}

		/// <summary>
		/// Create a fallback glyph that only contains a description.
		/// </summary>
		/// <remarks>The main use case is if there is no glyph found but there is <i>some</i> information about the input that can allow it to be shown in text.</remarks>
		public Glyph(string description, ControllerType? controllerType, Sprite sprite = null, Type type = Type.Input)
		{
			inputID = -1;
			this.description = description;
			this.sprite = sprite;
			positiveSprite = null;
			negativeSprite = null;
			PreferDescription = true;
			GlyphType = type;
			ControllerType = controllerType;
		}

		#region Obsolete
		/// <inheritdoc cref="Glyph(int, string, Rewired.ControllerType?, Sprite, Sprite, Sprite, Type)"/>
		[Obsolete("This constructor does not specify ControllerType which may cause issues with other systems. " +
		          "Use Glyph(int, string, Rewired.ControllerType, Sprite, Sprite, Sprite, Type) instead.")]
		public Glyph(int inputID, string description, Sprite sprite, Sprite positiveSprite, Sprite negativeSprite, Type type = Type.Input)
		{
			this.inputID = inputID;
			this.description = description;
			this.sprite = sprite;
			this.positiveSprite = positiveSprite;
			this.negativeSprite = negativeSprite;
			PreferDescription = false;
			GlyphType = type;
		}

		/// <inheritdoc cref="Glyph(int, string, Rewired.ControllerType?, Sprite, Type)"/>
		[Obsolete("This constructor does not specify ControllerType which may cause issues with other systems. " +
		          "Glyph(int, string, Rewired.ControllerType, Sprite, Type) instead.")]
		public Glyph(int inputID, string description, Sprite sprite, Type type = Type.Input)
		{
			this.inputID = inputID;
			this.description = description;
			this.sprite = sprite;
			positiveSprite = null;
			negativeSprite = null;
			PreferDescription = sprite != null;
			GlyphType = type;
		}

		/// <inheritdoc cref="Glyph(string, Rewired.ControllerType?, Sprite, Type)"/>
		[Obsolete("This constructor does not specify ControllerType which may cause issues with other systems. " +
		          "Use Glyph(string, ControllerType) instead.")]
		public Glyph(string description, Sprite sprite = null, Type type = Type.Input)
		{
			inputID = -1;
			this.description = description;
			this.sprite = sprite;
			positiveSprite = null;
			negativeSprite = null;
			PreferDescription = true;
			GlyphType = type;
		}
		#endregion

		[PublicAPI]
		public enum Type
		{
			/// <summary>
			/// The intention of this glyph is unspecified.
			/// You are highly encouraged to use another type if one is appropriate.
			/// </summary>
			Undefined = -1,
			/// <summary>
			/// This glyph represents an element for a particular hardware.
			/// </summary>
			Input = 0,
			/// <summary>
			/// This glyph represents an action that does not exist.
			/// Likely due to an invalid action id.
			/// </summary>
			Null = 1,
			/// <summary>
			/// This glyph represents an action that does not have any elements that control it.
			/// Likely due to the Input Manager maps missing an ActionElementMap for it.
			/// </summary>
			Unbound = 2,
			/// <summary>
			/// This glyph represents an action that does not exist for the specific reason that either <see cref="InputGlyphs"/> or Rewired itself are not ready.
			/// If this is output for more than a single frame there is likely no valid Input Manager present or it does not contain a <see cref="RewiredGlyphManager"/> component.
			/// </summary>
			Uninitialized = 3
		}
	}
}