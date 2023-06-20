using Rewired;
using UnityEngine;

/// <summary>
/// An abstract class that provides common functionality for a ui element to display an <see cref="InputGlyph"/>.
/// </summary>
public abstract class InputGlyphDisplay : MonoBehaviour
{
	[Header("Input Glyph Information")]
	[SerializeField, Tooltip("The name of the action as defined in the input manager")]
	protected string actionName = "Jump";
	[SerializeField, Tooltip("The direction of input to represent. In most cases should be positive unless it represents a dual-axis input (i.e move left instead move right)")]
	protected Pole pole = Pole.Positive;

	protected virtual void OnEnable()
	{
		UpdateGlyph();
		InputGlyphObserver.OnGlyphsDirty += InputGlyphObserver_OnGlyphsDirty;
	}

	protected virtual void OnDisable()
	{
		InputGlyphObserver.OnGlyphsDirty -= InputGlyphObserver_OnGlyphsDirty;
	}

	private void InputGlyphObserver_OnGlyphsDirty()
	{
		UpdateGlyph();
	}

	[ContextMenu("Update Glyph")]
	public void UpdateGlyph()
	{
		InputGlyph glyph = InputGlyphs.GetCurrentGlyph(actionName, pole, out AxisRange axisRange);
		SetGlyph(glyph, axisRange);
	}

	public abstract void SetGlyph(InputGlyph glyph, AxisRange axisRange);

	/// <summary>
	/// Change the input action this should display.
	/// </summary>
	/// <param name="actionName">The name of the action as defined in the input manager</param>
	/// <param name="pole">The direction of input to represent. In most cases should be positive unless it represents a dual-axis input (i.e move left instead move right)</param>
	public void SetTarget(string actionName, Pole pole)
	{
		this.actionName = actionName;
		this.pole = pole;
		UpdateGlyph();
	}
}