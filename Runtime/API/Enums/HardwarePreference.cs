using JetBrains.Annotations;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Determines the preferred hardware to show input glyphs for.
	/// </summary>
	[PublicAPI]
	public enum HardwarePreference
	{
		/// <summary>
		/// The preferred hardware is the most recently used hardware by the player.
		/// </summary>
		Auto,
		/// <summary>
		/// The preferred hardware is the keyboard and mouse.
		/// </summary>
		KeyboardMouse,
		/// <summary>
		/// The preferred hardware is the gamepad.
		/// </summary>
		Gamepad
	}
}