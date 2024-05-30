using Rewired;
using System;
using System.Linq;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Internal only: data type that parses and outputs display values for a user input glyph tag.
	/// </summary>
	internal class GlyphParseResult
	{
		internal readonly int actionId;
		internal readonly int playerId;
		internal readonly Pole pole = Pole.Positive;
		internal readonly bool forceAxis;
		/// <summary>
		/// When true this means we use the most recently input controller for this player.
		/// </summary>
		internal readonly bool useCurrentController = true;
		internal readonly ControllerType controllerType = ControllerType.Joystick;

		private const string PlayerSpecifier = "player=";
		private const string PolaritySpecifier = "pole=";
		private const string ControllerSpecifier = "type=";
		private static readonly string[] PositivePoleKeywords =
		{
			"Positive",
			"+"
		};
		private static readonly string[] NegativePoleKeywords =
		{
			"Negative",
			"-"
		};
		private static readonly string[] FullAxisKeywords =
		{
			"Full",
			"FullAxis",
			"Full Axis"
		};
		private static readonly string[] ControllerCurrentKeywords =
		{
			"Current",
			"Default",
			"Active"
		};
		private static readonly string[] ControllerKeyboardKeywords =
		{
			"Keyboard",
			"KB"
		};
		private static readonly string[] ControllerMouseKeywords =
		{
			"Mouse",
			"M"
		};
		private static readonly string[] ControllerJoystickKeywords =
		{
			"Gamepad",
			"Joystick",
			"Controller"
		};
		private static readonly string[] ControllerCustomKeywords =
		{
			"Custom",
			"Other"
		};

		internal GlyphParseResult(string[] tagContents)
		{
			bool didSetActionId = false;
			foreach (string splitArg in tagContents)
			{
				if (string.IsNullOrWhiteSpace(splitArg))
				{
					continue;
				}

				// Remove undesired characters from. These characters should never be used in action names.
				string trimmedArg = splitArg.Trim('\"', '/', '\\');

				// The first arg is always interpret as the action id.
				if (!didSetActionId)
				{
					bool isInt = int.TryParse(trimmedArg, out int foundActionId);

					// If the value is an int, we set it directly as the action id.
					// Otherwise, try to find an action id with the string value from rewired
					actionId = isInt ? foundActionId : ReInput.mapping.GetActionId(trimmedArg);
					didSetActionId = true;
					continue;
				}

				// ----
				// ---- Beyond this point we are no longer evaluating the first arg (since action id has been set) ----
				// ----

				// ---- LEGACY: Support Unspecified Parameters ----
				// Support for these may be removed in a major release but this is unlikely.

				// -- LEGACY: Player ID if lonely integer
				if (int.TryParse(trimmedArg, out int parsedPlayerId))
				{
					playerId = parsedPlayerId;
					continue;
				}

				// -- LEGACY: Polarity if lonely polarity values
				if (ArgEqualsAny(trimmedArg, PositivePoleKeywords))
				{
					pole = Pole.Positive;
					continue;
				}
				else if (ArgEqualsAny(trimmedArg, NegativePoleKeywords))
				{
					pole = Pole.Negative;
					continue;
				}
				else if (ArgEqualsAny(trimmedArg, FullAxisKeywords))
				{
					forceAxis = true;
					continue;
				}

				// -- Parsing Player ID --
				if (trimmedArg.StartsWith(PlayerSpecifier, StringComparison.OrdinalIgnoreCase))
				{
					trimmedArg = trimmedArg.Remove(0, PlayerSpecifier.Length).Trim('\"');
					if (int.TryParse(trimmedArg, out int specifiedParsedPlayerId))
					{
						playerId = specifiedParsedPlayerId;
					}
				}
				// -- Parsing Polarity --
				else if (trimmedArg.StartsWith(PolaritySpecifier, StringComparison.OrdinalIgnoreCase))
				{
					trimmedArg = trimmedArg.Remove(0, PolaritySpecifier.Length).Trim('\"');
					if (ArgEqualsAny(trimmedArg, PositivePoleKeywords))
					{
						pole = Pole.Positive;
					}
					else if (ArgEqualsAny(trimmedArg, NegativePoleKeywords))
					{
						pole = Pole.Negative;
					}
					else if (ArgEqualsAny(trimmedArg, FullAxisKeywords))
					{
						forceAxis = true;
					}
				}
				// -- Parsing Controller Type --
				else if (trimmedArg.StartsWith(ControllerSpecifier, StringComparison.OrdinalIgnoreCase))
				{
					trimmedArg = trimmedArg.Remove(0, ControllerSpecifier.Length).Trim('\"');
					if (ArgEqualsAny(trimmedArg, ControllerCurrentKeywords))
					{
						useCurrentController = true;
						controllerType = ControllerType.Joystick;
					}
					else if (ArgEqualsAny(trimmedArg, ControllerKeyboardKeywords))
					{
						useCurrentController = false;
						controllerType = ControllerType.Keyboard;
					}
					else if (ArgEqualsAny(trimmedArg, ControllerMouseKeywords))
					{
						useCurrentController = false;
						controllerType = ControllerType.Mouse;
					}
					else if (ArgEqualsAny(trimmedArg, ControllerJoystickKeywords))
					{
						useCurrentController = false;
						controllerType = ControllerType.Joystick;
					}
					else if (ArgEqualsAny(trimmedArg, ControllerCustomKeywords))
					{
						useCurrentController = false;
						controllerType = ControllerType.Custom;
					}
				}
			}
		}

		private static bool ArgEqualsAny(string arg, string[] keywords)
		{
			return keywords.Any(keyword => arg.Equals(keyword, StringComparison.OrdinalIgnoreCase));
		}
	}
}