using Rewired;
using System;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Components
{
	[RequireComponent(typeof(TMP_Text))]
	public class GlyphRichTextOutput : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("When enabled will check for text changes every frame. When text changes will automatically reformat text.\n\n" +
				 "This is somewhat expensive so only enable if you don't want to or can't use the \"SetText\" method of this class.")]
		private bool autoUpdate;

		private readonly StringBuilder stringBuilder = new StringBuilder();
		private TMP_Text textMesh;
		private int lastHashCode;
		private static readonly Regex GlyphRegex = new Regex("<glyph ([^>|^<]*)>");

		private void Awake()
		{
			textMesh = GetComponent<TMP_Text>();
		}

		private void Start()
		{
			AutoUpdateText();
		}

		private void LateUpdate()
		{
			if (autoUpdate)
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
			if (!GlyphRegex.IsMatch(text))
			{
				textMesh.SetText(text);
				lastHashCode = text.GetHashCode();
				return;
			}

			stringBuilder.Clear();
			stringBuilder.Append(text);
			foreach (Match match in GlyphRegex.Matches(text))
			{
				if (match.Groups.Count > 1)
				{
					// TODO: It would be nice if the regex made space separated groups but I am too stupid to figure it out.
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
					string assetName = "TODO"; //TODO: Actually generate such sprite sheet in editor and point to it. Probably need to cache sprite sheet name in Glyph itself.
					stringBuilder.Replace(match.Groups[0].Value, $"<sprite=\"{assetName}\" name=\"{sprite.name}\">");
				}
			}

			string finalText = stringBuilder.ToString();
			textMesh.SetText(finalText);
			lastHashCode = finalText.GetHashCode();
		}
	}
}