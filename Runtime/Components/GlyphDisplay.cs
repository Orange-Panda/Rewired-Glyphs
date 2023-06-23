using JetBrains.Annotations;
using Rewired;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// An abstract class that provides common functionality for a ui element to display an <see cref="Glyph"/>.
	/// </summary>
	/// <remarks>
	/// Usage of this class is completely optional.
	/// </remarks>
	[PublicAPI]
	public abstract class GlyphDisplay : MonoBehaviour
	{
		[Header("Input Glyph Information")]
		[SerializeField, Min(-1)]
		protected int playerIndex;

		[Header("Initial Glyph Values")]
		[SerializeField, Tooltip("When true will use the action index instead of action name")]
		protected bool useActionID;
		[SerializeField, Tooltip("The id of the action in the input manager")]
		protected int actionID;
		[SerializeField, Tooltip("The name of the action as defined in the input manager")]
		protected string actionName = "Jump";
		[SerializeField, Tooltip("The direction of input to represent. In most cases should be positive unless it represents a dual-axis input (i.e move left instead move right)")]
		protected Pole pole = Pole.Positive;

		public int ActionID { get; protected set; }
		public Pole PoleValue { get; protected set; }

		public abstract void SetGlyph(Glyph glyph, AxisRange axisRange);

		protected virtual void Awake()
		{
			ActionID = useActionID ? actionID : ReInput.mapping.GetActionId(actionName);
			PoleValue = pole;
		}

		protected virtual void OnEnable()
		{
			UpdateGlyph();
			InputGlyphs.RebuildGlyphs += InputGlyphsOnRebuildGlyphs;
		}

		protected virtual void OnDisable()
		{
			InputGlyphs.RebuildGlyphs -= InputGlyphsOnRebuildGlyphs;
		}

		protected virtual void InputGlyphsOnRebuildGlyphs()
		{
			UpdateGlyph();
		}

		[ContextMenu("Update Glyph")]
		public void UpdateGlyph()
		{
			Glyph glyph = InputGlyphs.GetCurrentGlyph(ActionID, PoleValue, out AxisRange axisRange, playerIndex);
			SetGlyph(glyph, axisRange);
		}

		/// <summary>
		/// Change the input action this should display.
		/// </summary>
		/// <param name="newActionName">The name of the action as defined in the input manager</param>
		/// <param name="newPole">The direction of input to represent. In most cases should be positive unless it represents a dual-axis input (i.e move left instead move right)</param>
		public void SetTarget(string newActionName, Pole newPole)
		{
			ActionID = ReInput.mapping.GetActionId(newActionName);
			PoleValue = newPole;
			UpdateGlyph();
		}

		/// <summary>
		/// Change the input action this should display.
		/// </summary>
		/// <param name="newActionID">The id of the action as defined in the input manager</param>
		/// <param name="newPole">The direction of input to represent. In most cases should be positive unless it represents a dual-axis input (i.e move left instead move right)</param>
		public void SetTarget(int newActionID, Pole newPole)
		{
			ActionID = newActionID;
			PoleValue = newPole;
			UpdateGlyph();
		}
	}
}