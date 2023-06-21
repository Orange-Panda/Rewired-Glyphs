using Rewired;
using System;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// Responsible for firing the <see cref="OnGlyphsDirty"/> event to update all glyph displays when they are out of date.
	/// </summary>
	/// <remarks>
	/// Add this component to your input manager for it to function.
	/// </remarks>
	public class InputGlyphObserver : MonoBehaviour
	{
		/// <summary>
		/// Event fired when the user's preferences have changed or their most recent input hardware has changed.
		/// </summary>
		public static event Action OnGlyphsDirty = delegate { };

		private Player player;
		private HardwareDefinition lastType;

		private void OnEnable()
		{
			InputGlyphs.GlyphPreferencesChanged += Preferences_PreferencesChanged;
		}

		private void OnDisable()
		{
			InputGlyphs.GlyphPreferencesChanged -= Preferences_PreferencesChanged;
		}

		private void Preferences_PreferencesChanged()
		{
			InvokeRebuild();
		}

		private void Start()
		{
			player = ReInput.players.GetPlayer(0);
		}

		private void Update()
		{
			HardwareDefinition controllerType = InputGlyphs.GetHardwareDefinition(player.controllers.GetLastActiveController());
			if (controllerType != lastType && !(IsKeyboardMouse(lastType) && IsKeyboardMouse(controllerType)))
			{
				lastType = controllerType;
				InvokeRebuild();
			}
		}

		private bool IsKeyboardMouse(HardwareDefinition type)
		{
			return type == HardwareDefinition.Mouse || type == HardwareDefinition.Keyboard;
		}

		private static void InvokeRebuild()
		{
			try
			{
				OnGlyphsDirty.Invoke();
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}
	}
}