using UnityEditor;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Editor
{
	[CustomPropertyDrawer(typeof(GlyphCollection.TemplateEntry))]
	public class GlyphCollectionTemplateEntryPropertyDrawer : PropertyDrawer
	{
		private const int Spacing = 20;
		private const int Padding = 8;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty glyphMap = property.FindPropertyRelative("glyphMap");
			SerializedProperty symbolPreference = property.FindPropertyRelative("symbolPreference");

			Rect drawPosition = new Rect(position) { height = Spacing - 2 };
			drawPosition.y += Padding;
			EditorGUI.PropertyField(drawPosition, glyphMap);
			drawPosition.y += Spacing;
			EditorGUI.PropertyField(drawPosition, symbolPreference);
			drawPosition.y += Spacing;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return (Spacing * 2) + (Padding * 2);
		}
	}
}