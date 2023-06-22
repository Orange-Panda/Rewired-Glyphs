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
	/// - This pattern is achieved by enabled the <see cref="automaticallyCheckForTextChanges"/> field.<br/>
	/// - This pattern is useful if you can't change text via the <see cref="SetFormattedText"/> but runs the risk of losing preformatted text.
	/// </remarks>
	[PublicAPI]
	[RequireComponent(typeof(TMP_Text))]
	[AddComponentMenu("Rewired Glyphs/Glyph TMP Rich Text Formatter")]
	public class GlyphRichTextFormatter : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("When enabled the text will be updated with new sprites when glyphs are rebuilt.\n\n" +
				 "This should usually be enabled so you are always showing up to date glyphs. However, if you do not want the value of text mesh to change consider disabling this.")]
		private bool updateTextOnRebuildGlyphs = true;
		[SerializeField]
		[Tooltip("When enabled will check for text changes every frame. When text changes will automatically reformat text.\n\n" +
				 "This is somewhat expensive and may lose glyph references if you append text so only enable if you don't want to or can't use the \"SetText\" method of this class.")]
		private bool automaticallyCheckForTextChanges;

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

		public void SetFormattedText(string text)
		{
			lastPreformatText = text;
			lastPreformatTextHasGlyph = GlyphRegex.IsMatch(text);
			string textToSet = lastPreformatTextHasGlyph ? ReplaceGlyphTagsWithSpriteTags(text) : text;
			textMesh.SetText(textToSet);
			lastHashCode = textToSet.GetHashCode();
		}

		public static string ReplaceGlyphTagsWithSpriteTags(string text)
		{
			Output.Clear();
			Output.Append(text);
			foreach (Match match in GlyphRegex.Matches(text))
			{
				if (match.Groups.Count > 1)
				{
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
					if (sprite != null)
					{
						Output.Replace(match.Groups[0].Value, $"<sprite=\"{glyph.TextMeshSpriteSheetName}\" name=\"{sprite.name}\">");
					}
					else
					{
						Output.Replace(match.Groups[0].Value, $"[{glyph.Description}]");
					}
				}
			}

			return Output.ToString();
		}
	}
}