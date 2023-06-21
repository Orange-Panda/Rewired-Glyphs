using JetBrains.Annotations;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Definitions for hardware that can be automatically handled by the Input Glyph system.
	/// These values are usually used to automatically determine what glyphs to show based on the player's most recent controller used.
	/// </summary>
	[PublicAPI]
	public enum HardwareDefinition
	{
		Unknown = -1, Generic, Keyboard, Mouse, Xbox, Playstation2, Playstation3, Playstation, NintendoSwitch
	}
}