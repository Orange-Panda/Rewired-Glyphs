using JetBrains.Annotations;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Determines the preferred hardware to show input glyphs considering or disregarding the user's most recently used device.
	/// </summary>
	[PublicAPI]
	public enum HardwarePreference
	{
		/// <summary>
		/// The preferred hardware is the most recently used hardware by the player.
		/// </summary>
		/// <example>
		/// If the player inputs on keyboard it will show keyboard glyphs, then when inputting on a Joystick the glyphs will change to represent a Joystick.
		/// </example>
		Auto,
		/// <summary>
		/// The preferred hardware is the keyboard and mouse.
		/// </summary>
		/// <remarks>
		/// Will try to always show keyboard glyphs regardless of most recently used device.
		/// </remarks>
		KeyboardMouse,
		/// <summary>
		/// The preferred hardware is the gamepad.
		/// </summary>
		/// <remarks>
		/// Will try to always show gamepad (a.k.a Joystick) glyphs regardless of most recently used device.
		/// </remarks>
		Gamepad
	}
}