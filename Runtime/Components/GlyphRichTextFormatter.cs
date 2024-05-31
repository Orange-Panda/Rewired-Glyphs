using JetBrains.Annotations;
using Rewired;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Components
{
	/// <summary>
	/// Adds support for inline input glyphs on a <see cref="TMP_Text"/> component.
	/// </summary>
	/// <remarks>
	/// The two main ways to use this component are the following:<br/><br/>
	/// Director Pattern (Recommended): The text mesh's text is set exclusively via this component's <see cref="SetFormattedText"/> method.<br/>
	/// - This pattern is achieved by disabling the <see cref="automaticallyCheckForTextChanges"/> field and never changing the text mesh component's text elsewhere.<br/>
	/// - This pattern is encouraged because it ensures that glyph tags are not lost in translation, which can especially happen if you are appending text.<br/><br/>
	/// Observer Pattern: The text mesh's text is updated whenever it is changed externally.<br/>
	/// - This pattern is achieved by enabling the <see cref="automaticallyCheckForTextChanges"/> field.<br/>
	/// - This pattern is useful if you can't change text via the <see cref="SetFormattedText"/>. This pattern does however run the risk of losing preformatted text if not used carefully.
	/// </remarks>
	[PublicAPI]
	[RequireComponent(typeof(TMP_Text))]
	[AddComponentMenu("Rewired Glyphs/Glyph TMP Rich Text Formatter")]
	public class GlyphRichTextFormatter : MonoBehaviour
	{
		[Header("Update Contexts")]
		[SerializeField]
		[Tooltip("When enabled the text will be formatted in start.\n\n" +
		         "This should usually be enabled unless you are setting formatted text elsewhere during initialization.")]
		private bool formatTextOnStart = true;
		[SerializeField]
		[Tooltip("When enabled the text will be updated with new sprites when glyphs are rebuilt.\n\n" +
		         "This should usually be enabled so you are always showing up to date glyphs. However, if you do not want the value of text mesh to change consider disabling this.")]
		private bool updateTextOnRebuildGlyphs = true;
		[SerializeField]
		[Tooltip("When enabled will check for text changes every frame. When text changes will automatically reformat text.\n\n" +
		         "This is somewhat expensive and may lose glyph references if you append text so only enable if you don't want to or can't use the \"SetText\" method of this class.")]
		private bool automaticallyCheckForTextChanges;

		[Header("Output Customization")]
		[SerializeField]
		[Tooltip("When enabled the glyph rich text will be replaced with an TMP inline sprite instead of text." +
		         "If there is no sprite available for the glyph will fallback to description.\n\n" +
		         "When disabled: Will always show the text description of the Glyph instead of its sprite.")]
		private bool useSpritesWhenAvailable = true;
		[SerializeField]
		[Tooltip("The format for Glyph descriptions that replace glyph rich text values. Default: \"[{0}]\" will output \"[R2]\" for a primary fire action for example.")]
		private string descriptionFormat = "[{0}]";

		private TMP_Text textMesh;
		private int lastHashCode;
		private bool lastPreformatTextHasGlyph;
		private string lastPreformatText;

		public TMP_Text TextMesh => textMesh;

		private void Awake()
		{
			textMesh = GetComponent<TMP_Text>();
		}

		protected virtual void OnEnable()
		{
			if (lastPreformatTextHasGlyph)
			{
				SetFormattedText(lastPreformatText);
			}

			InputGlyphs.RebuildGlyphs += InputGlyphsOnRebuildGlyphs;
		}

		protected virtual void OnDisable()
		{
			InputGlyphs.RebuildGlyphs -= InputGlyphsOnRebuildGlyphs;
		}

		private void InputGlyphsOnRebuildGlyphs()
		{
			if (updateTextOnRebuildGlyphs && lastPreformatTextHasGlyph)
			{
				SetFormattedText(lastPreformatText);
			}
		}

		private void Start()
		{
			if (formatTextOnStart)
			{
				UpdateTextFromObservedValue();
			}
		}

		private void LateUpdate()
		{
			if (automaticallyCheckForTextChanges)
			{
				UpdateTextFromObservedValue();
			}
		}

		private void UpdateTextFromObservedValue()
		{
			// Don't set formatted text if it is deemed identical to the last formatted text.
			int hashCode = textMesh.text.GetHashCode();
			if (hashCode == lastHashCode)
			{
				return;
			}

			lastHashCode = hashCode;
			SetFormattedText(textMesh.text);
		}

		/// <summary>
		/// Set the text for this text output, replacing glyph rich text such as &amp;lt;glyph "Jump"&amp;gt; with its glyph's sprite or description.
		/// </summary>
		/// <param name="text"></param>
		public void SetFormattedText(string text)
		{
			// Use empty string if null is provided. Otherwise, an exception will occur. We assume the intent of passing a null value is equivalent to an empty string.
			text ??= string.Empty;

			lastPreformatText = text;
			lastPreformatTextHasGlyph = GlyphRegex.IsMatch(text);
			string textToSet = lastPreformatTextHasGlyph ? ReplaceGlyphTagsWithSpriteTags(text, useSpritesWhenAvailable, descriptionFormat) : text;
			textMesh.SetText(textToSet);
			lastHashCode = textToSet.GetHashCode();
		}

		#region Static
		private static readonly StringBuilder Output = new StringBuilder();
		private static readonly Regex GlyphRegex = new Regex("<glyph ([^>|^<]*)>", RegexOptions.IgnoreCase);

		/// <summary>
		/// Replace glyph rich text such as &lt;glyph "Jump"&gt; in the provided <paramref name="text"/> with a sprite or description for its corresponding Glyph in the Rewired Glyph system.
		/// <br/><br/>
		/// <b>Syntax Rules:</b><br/>
		/// - The glyph tag begins with verbatim `&lt;glyph ` and ends at any `&gt;` character.<br/>
		///	- The first argument must <b>always</b> represent the action to represent as either an integer for the action ID or a string for the action name <i>if and only if</i> it does not contain any spaces.<br/>
		/// - Following the action argument all arguments may appear in any order.<br/>
		/// - Arguments are delimited by any occurrence of the space character, <i>even if the space is contained within quotes</i>.<br/>
		/// <br/>
		/// <b>Additional Arguments:</b><br/>
		/// Following the action argument you may optionally include the following arguments to control the output glyph.
		/// <br/><br/>
		/// - `Player ID` (Default: 0)<br/>
		/// Specify the player you'd like to show glyph for using `player=2` where 2 can be any int.
		/// If an integer is alone without any specifier it is interpret as player id, such as `&lt;glyph Jump 2&gt;` where 2 is player id
		/// <br/><br/>
		/// - `Polarity` (Default: Positive)<br/>
		/// Specify the expected axis or pole input for the action such as positive for "Move Right" and negative for "Move Left" on a Move Horizontal action
		/// using `pole=Positive`, `pole=Negative`, or `pole=FullAxis`. You may exclude `pole` specifier from your input.
		/// <br/><br/>
		/// - `Controller Type` (Default: Current)<br/>
		/// Specify the controller type to show symbols for using `type=Joystick`.
		/// Valid values are `type=Current`, `type=Keyboard`, `type=Mouse`, and `type=Joystick`.<br/>
		/// Warning: Specifying a controller that the runtime device doesn't have connected (such as Joystick) will show the `UNBOUND` symbol until they connect that controller type.<br/>
		/// Warning: Specifying a controller that does not have the specified action bound to that specific controller will show the `UNBOUND` symbol without falling back to another controller.
		/// <br/><br/>
		/// - `Symbol Preference` (Default: <see cref="InputGlyphs.PreferredSymbols"/>)<br/>
		/// Specify the symbols to use for `Joystick` glyphs.
		/// Valid values are `symbol=auto`, `symbol=xbox`, `symbol=ps`, or `symbol=switch`.
		/// <br/><br/>
		/// All additional arguments that are string based are <b>case-insensitive</b> such that "TYPE=JOYSTICK" and "type=joystick" are equivalent inputs.
		/// </summary>
		/// <remarks>Does not mutate the provided string.</remarks>
		[Pure]
		public static string ReplaceGlyphTagsWithSpriteTags(string text, bool useSpritesWhenAvailable = true, string descriptionFormat = "[{0}]")
		{
			Output.Clear();
			Output.Append(text);
			foreach (Match match in GlyphRegex.Matches(text))
			{
				if (match.Groups.Count <= 1)
				{
					continue;
				}

				string[] splitArgs = match.Groups[1].Value.Split(' ');
				GlyphParseResult result = new GlyphParseResult(splitArgs);
				Glyph glyph = result.GetGlyph(out AxisRange axisRange);
				Sprite sprite = glyph.GetSprite(axisRange);
				if (useSpritesWhenAvailable && sprite != null && !glyph.PreferDescription)
				{
					Output.Replace(match.Groups[0].Value, $"<sprite=\"{glyph.TextMeshSpriteSheetName}\" name=\"{sprite.name}\">");
				}
				else
				{
					Output.Replace(match.Groups[0].Value, string.Format(descriptionFormat, glyph.GetDescription(axisRange)));
				}
			}

			return Output.ToString();
		}
		#endregion
	}
}