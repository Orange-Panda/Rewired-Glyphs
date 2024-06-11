using UnityEditor;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Editor
{
	[CustomPropertyDrawer(typeof(Glyph))]
	public class GlyphPropertyDrawer : PropertyDrawer
	{
		public const int SpriteSize = 96;
		private const int Height = 18;
		private const int Spacing = 20;
		private const int DescriptionLabelWidth = 120;

		public override void OnGUI(Rect position, SerializedProperty glyph, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, glyph);
			SerializedProperty inputID = glyph.FindPropertyRelative("inputID");
			SerializedProperty description = glyph.FindPropertyRelative("description");
			SerializedProperty positiveDescription = glyph.FindPropertyRelative("positiveDescription");
			SerializedProperty negativeDescription = glyph.FindPropertyRelative("negativeDescription");
			SerializedProperty sprite = glyph.FindPropertyRelative("sprite");
			SerializedProperty positiveSprite = glyph.FindPropertyRelative("positiveSprite");
			SerializedProperty negativeSprite = glyph.FindPropertyRelative("negativeSprite");

			Rect inputIDRect = new Rect(position) { height = Height, width = 32 };
			inputID.intValue = EditorGUI.IntField(inputIDRect, inputID.intValue);

			Rect displayNameRect = new Rect(position) { height = Height, width = position.width - 36 };
			displayNameRect.x += 36;
			EditorGUI.LabelField(displayNameRect, glyph.displayName, EditorStyles.boldLabel);

			Rect descriptionLabelRect = new Rect(position) { height = Height, width = DescriptionLabelWidth };
			Rect descriptionFieldRect = new Rect(position) { height = Height, width = position.width - DescriptionLabelWidth };
			descriptionFieldRect.x += DescriptionLabelWidth;

			descriptionLabelRect.y += Spacing;
			descriptionFieldRect.y += Spacing;
			EditorGUI.LabelField(descriptionLabelRect, "Full Description");
			description.stringValue = EditorGUI.TextField(descriptionFieldRect, description.stringValue);

			descriptionLabelRect.y += Spacing;
			descriptionFieldRect.y += Spacing;
			EditorGUI.LabelField(descriptionLabelRect, "Positive Description");
			positiveDescription.stringValue = EditorGUI.TextField(descriptionFieldRect, positiveDescription.stringValue);

			descriptionLabelRect.y += Spacing;
			descriptionFieldRect.y += Spacing;
			EditorGUI.LabelField(descriptionLabelRect, "Negative Description");
			negativeDescription.stringValue = EditorGUI.TextField(descriptionFieldRect, negativeDescription.stringValue);

			descriptionLabelRect.y += Spacing;
			descriptionFieldRect.y += Spacing;

			Rect spriteRect = new Rect(descriptionLabelRect.position, new Vector2(SpriteSize, SpriteSize));
			sprite.objectReferenceValue = EditorGUI.ObjectField(spriteRect, sprite.objectReferenceValue, typeof(Sprite), false);
			spriteRect.x += SpriteSize;
			positiveSprite.objectReferenceValue = EditorGUI.ObjectField(spriteRect, positiveSprite.objectReferenceValue, typeof(Sprite), false);
			spriteRect.x += SpriteSize;
			negativeSprite.objectReferenceValue = EditorGUI.ObjectField(spriteRect, negativeSprite.objectReferenceValue, typeof(Sprite), false);

			spriteRect.x = position.x;
			spriteRect.height = 20;
			spriteRect.y += SpriteSize;

			GUI.Label(spriteRect, "Full", EditorStyles.centeredGreyMiniLabel);
			spriteRect.x += SpriteSize;
			GUI.Label(spriteRect, "Positive", EditorStyles.centeredGreyMiniLabel);
			spriteRect.x += SpriteSize;
			GUI.Label(spriteRect, "Negative", EditorStyles.centeredGreyMiniLabel);
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return (Spacing * 5) + SpriteSize;
		}
	}
}