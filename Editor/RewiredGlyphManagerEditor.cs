using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	[CustomEditor(typeof(RewiredGlyphManager))]
	public class RewiredGlyphManagerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Generate TMP Sprite Sheet"))
			{
				// TODO: Generate TMP Sprite Sheet and add it to TMP_Settings sprite sheet folder.
			}
		}
	}
}