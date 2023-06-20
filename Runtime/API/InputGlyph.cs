using Rewired;
using System;
using UnityEngine;

/// <summary>
/// Data about a particular input including <see cref="Sprite"/> and <see cref="string"/>. 
/// </summary>
[Serializable]
public class InputGlyph
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
	public Sprite Sprite => sprite;

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

	public InputGlyph(int inputID, string description, Sprite sprite = null)
	{
		this.inputID = inputID;
		this.description = description;
		this.sprite = sprite;
		positiveSprite = null;
		negativeSprite = null;
	}

	public InputGlyph(string description)
	{
		inputID = -1;
		this.description = description;
		sprite = null;
		positiveSprite = null;
		negativeSprite = null;
	}
}