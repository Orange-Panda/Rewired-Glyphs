using JetBrains.Annotations;
using Rewired;
using System.Collections;
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

		/// <remarks>
		/// When this is true: Keyboard and Mouse glyphs are still input to the <see cref="SetGlyph"/> method.
		/// Therefore, if you are creating a custom component you <b>must</b> implement this functionality yourself for it to do anything.
		/// </remarks>
		[Header("Extra Settings")]
		[SerializeField]
		[Tooltip("When true inform the component that you'd rather not show the glyph at all if it is a Keyboard or Mouse glyph." +
		         "Therefore, will only show a glyph if the device is a Joystick and show nothing if the device is a Keyboard or Mouse")]
		protected bool hideKeyboardMouseGlyphs;
		[SerializeField]
		[Tooltip("When true inform the component that you'd rather not show glyphs that don't represent an actual input (such as Null or Uninitialized).\n\n" +
		         "Caution: A value of true cause misconfigurations to go unnoticed.")]
		protected bool hideNonInputGlyphs;

		public int ActionID { get; protected set; }

		public Pole PoleValue { get; protected set; }

		private bool hasSetValues;

		public abstract void SetGlyph(Glyph glyph, AxisRange axisRange);

		protected virtual void Awake()
		{
			if (!ReInput.isReady)
			{
				StartCoroutine(InitializeWhenReady());
			}
			else
			{
				InitializeValues();
			}
		}

		protected virtual IEnumerator InitializeWhenReady()
		{
			while (!ReInput.isReady)
			{
				yield return null;
			}

			InitializeValues();

			// We must update the glyph manually since OnEnable has likely already occurred.
			// We don't do it in InitializeValues() since that method may be called in Awake
			UpdateGlyph();
		}

		protected virtual IEnumerator SetTargetWhenReady(string newActionName, Pole newPole)
		{
			while (!ReInput.isReady)
			{
				yield return null;
			}

			SetTarget(ReInput.mapping.GetActionId(newActionName), newPole);
		}

		protected virtual void InitializeValues(bool forced = false)
		{
			if (!forced && hasSetValues)
			{
				return;
			}

			ActionID = useActionID ? actionID : ReInput.mapping.GetActionId(actionName);
			PoleValue = pole;
			hasSetValues = true;
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
			AxisRange axisRange = AxisRange.Full;
			Glyph glyph = hasSetValues ? InputGlyphs.GetCurrentGlyph(ActionID, PoleValue, out axisRange, playerIndex) : InputGlyphs.UninitializedGlyph;
			SetGlyph(glyph, axisRange);
		}

		/// <summary>
		/// Change the input action this should display.
		/// </summary>
		/// <param name="newActionName">The name of the action as defined in the input manager</param>
		/// <param name="newPole">The direction of input to represent. In most cases should be positive unless it represents a dual-axis input (i.e move left instead move right)</param>
		public void SetTarget(string newActionName, Pole newPole)
		{
			if (ReInput.isReady)
			{
				StopAllCoroutines();
				SetTarget(ReInput.mapping.GetActionId(newActionName), newPole);
			}
			else
			{
				StopAllCoroutines();
				StartCoroutine(SetTargetWhenReady(newActionName, newPole));
			}
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
			hasSetValues = true;
			UpdateGlyph();
		}

		/// <summary>
		/// Evaluate if the configuration of this Glyph would rather not show the Glyph in certain contexts.
		/// </summary>
		/// <example>
		/// If <see cref="hideKeyboardMouseGlyphs"/> is true and the glyph represents a keyboard or mouse glyph this will return true.
		/// </example>
		/// <remarks>
		/// The exact behavior of "hidden" is up to the component itself.
		/// For instance, a <see cref="GlyphRichTextFormatter"/> will not show the glyph for that action while a <see cref="GlyphImageOutput"/> may hide itself entirely.
		/// </remarks>
		/// <returns>True if the glyph should be hidden, false if the glyph should be shown.</returns>
		protected bool ShouldHideGlyph(Glyph glyph)
		{
			if (hideNonInputGlyphs && glyph.GlyphType != Glyph.Type.Input)
			{
				return true;
			}

			return hideKeyboardMouseGlyphs && glyph.ControllerType.IsKeyboardOrMouse();
		}
	}
}