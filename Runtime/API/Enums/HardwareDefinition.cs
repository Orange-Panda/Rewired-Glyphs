using JetBrains.Annotations;

namespace LMirman.RewiredGlyphs
{
	// INTERNAL NOTE: This design is *really* rigid and not very flexible. Perhaps this should be overhauled in some major release in the future.
	/// <summary>
	/// Definitions for hardware that can be automatically handled by the Input Glyph system.
	/// These values are usually used to automatically determine what glyphs to show based on the player's most recent controller used.
	/// </summary>
	[PublicAPI]
	public enum HardwareDefinition
	{
		/// <summary>
		/// This hardware is unrecognized or doesn't fit any other category
		/// </summary>
		Unknown = -1,
		Generic = 0,
		Keyboard = 1,
		Mouse = 2,
		Xbox = 3,
		Playstation2 = 4,
		Playstation3 = 5,
		Playstation = 6,
		NintendoSwitch = 7
	}
}