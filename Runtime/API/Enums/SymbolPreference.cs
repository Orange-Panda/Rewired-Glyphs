using JetBrains.Annotations;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Determines the preferred symbol type to show to the player when acquiring glyphs.
	/// </summary>
	[PublicAPI]
	public enum SymbolPreference
	{
		/// <summary>
		/// Use the symbols that match the hardware id of the controller being used, when possible.
		/// </summary>
		/// <seealso cref="InputGlyphs.ControllerGuids"/> <seealso cref="InputGlyphs.GetHardwareDefinition"/>
		Auto,
		/// <summary>
		/// Prefer the Xbox symbols, even when this controller is recognized as a different type.
		/// </summary>
		Xbox,
		/// <summary>
		/// Prefer the Playstation symbols, even when this controller is recognized as a different type.
		/// </summary>
		Playstation,
		/// <summary>
		/// Prefer the Nintendo Switch symbols, even when this controller is recognized as a different type.
		/// </summary>
		NintendoSwitch
	}
}