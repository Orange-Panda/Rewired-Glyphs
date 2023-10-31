using JetBrains.Annotations;
using Rewired;
using System;
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

		private static readonly StringBuilder Output = new StringBuilder();
		private static readonly Regex GlyphRegex = new Regex("<glyph ([^>|^<]*)>", RegexOptions.IgnoreCase);

		public TMP_Text TextMesh => textMesh;

		private void Awake()
		{
			textMesh = GetComponent<TMP_Text>();
		}

		protected virtual void OnEnable()
		{
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
			AutoUpdateText();
		}

		private void LateUpdate()
		{
			if (automaticallyCheckForTextChanges)
			{
				AutoUpdateText();
			}
		}

		private void AutoUpdateText()
		{
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
			lastPreformatText = text;
			lastPreformatTextHasGlyph = GlyphRegex.IsMatch(text);
			string textToSet = lastPreformatTextHasGlyph ? ReplaceGlyphTagsWithSpriteTags(text, useSpritesWhenAvailable, descriptionFormat) : text;
			textMesh.SetText(textToSet);
			lastHashCode = textToSet.GetHashCode();
		}

		/// <summary>
		/// Replace glyph rich text such as &lt;glyph "Jump"&gt; in the provided <paramref name="text"/> with a sprite or description for its corresponding Glyph in the Rewired Glyph system.
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
				bool didSetActionId = false;
				int actionId = 0;
				int player = 0;
				Pole pole = Pole.Positive;
				foreach (string splitArg in splitArgs)
				{
					if (string.IsNullOrWhiteSpace(splitArg))
					{
						continue;
					}

					string trimmedArg = splitArg.Trim('\"', '/', '\\');
					// The first arg is always interpret as the action id.
					if (!didSetActionId)
					{
						bool isInt = int.TryParse(trimmedArg, out int foundActionId);
						actionId = isInt ? foundActionId : ReInput.mapping.GetActionId(trimmedArg);
						didSetActionId = true;
					}
					// If this isn't the first arg (since action id has been set) interpret it as player id if it is int.
					else if (int.TryParse(trimmedArg, out int playerId))
					{
						player = playerId;
					}
					else if (trimmedArg.Equals("Positive", StringComparison.OrdinalIgnoreCase))
					{
						pole = Pole.Positive;
					}
					else if (trimmedArg.Equals("Negative", StringComparison.OrdinalIgnoreCase))
					{
						pole = Pole.Negative;
					}
				}

				Glyph glyph = InputGlyphs.GetCurrentGlyph(actionId, pole, out AxisRange axisRange, player);
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
	}
}