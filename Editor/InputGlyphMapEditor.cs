using UnityEngine;
using UnityEditor;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// An editor window that makes modifying <see cref="InputGlyphMap"/>s easier.
	/// </summary>
	[CustomEditor(typeof(InputGlyphMap))]
	public class InputGlyphMapEditor : Editor
	{
		private int page = 1;
		private int PageMax => (serializedObject.FindProperty("glyphs").arraySize / PageSize) + 1;

		private const int PageSize = 10;
		private const int SpriteSize = 96;

		private int requestMoveUpAt = -1;
		private int requestMoveDownAt = -1;
		private int requestDeleteAt = -1;

		private static Object[] copiedObjects = new Object[3];

		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();
			GUILayout.Label("Input Glyph Map Editor", EditorStyles.largeLabel);
			page = EditorGUILayout.IntSlider(page, 1, PageMax);
			SerializedProperty glyphs = serializedObject.FindProperty("glyphs");
			for (int i = (page - 1) * PageSize; glyphs.isArray && i < glyphs.arraySize && i < page * PageSize; i++)
			{
				DrawInputGlyph(glyphs.GetArrayElementAtIndex(i), i);
			}

			if (requestMoveUpAt >= 0)
			{
				glyphs.MoveArrayElement(requestMoveUpAt, requestMoveUpAt - 1);
				requestMoveUpAt = -1;
			}

			if (requestMoveDownAt >= 0)
			{
				glyphs.MoveArrayElement(requestMoveDownAt, requestMoveDownAt + 1);
				requestMoveDownAt = -1;
			}

			if (requestDeleteAt >= 0)
			{
				glyphs.DeleteArrayElementAtIndex(requestDeleteAt);
				requestDeleteAt = -1;
			}

			glyphs.serializedObject.ApplyModifiedProperties();
			serializedObject.ApplyModifiedProperties();
			Repaint();
		}

		private void DrawInputGlyph(SerializedProperty glyph, int index)
		{
			SerializedProperty inputID = glyph.FindPropertyRelative("inputID");
			SerializedProperty description = glyph.FindPropertyRelative("description");
			SerializedProperty sprite = glyph.FindPropertyRelative("sprite");
			SerializedProperty positiveSprite = glyph.FindPropertyRelative("positiveSprite");
			SerializedProperty negativeSprite = glyph.FindPropertyRelative("negativeSprite");

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = false;
			EditorGUILayout.IntField(index, GUILayout.Width(32));
			GUI.enabled = true;
			inputID.intValue = EditorGUILayout.IntField(inputID.intValue, GUILayout.Width(32));
			description.stringValue = EditorGUILayout.TextField(description.stringValue);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			sprite.objectReferenceValue = EditorGUILayout.ObjectField(sprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(SpriteSize), GUILayout.Height(SpriteSize));
			negativeSprite.objectReferenceValue =
				EditorGUILayout.ObjectField(negativeSprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(SpriteSize), GUILayout.Height(SpriteSize));
			positiveSprite.objectReferenceValue =
				EditorGUILayout.ObjectField(positiveSprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(SpriteSize), GUILayout.Height(SpriteSize));
			EditorGUILayout.BeginVertical();
			if (GUILayout.Button("Move Up"))
			{
				requestMoveUpAt = index;
			}

			if (GUILayout.Button("Move Down"))
			{
				requestMoveDownAt = index;
			}

			if (GUILayout.Button("Delete"))
			{
				requestDeleteAt = index;
			}

			if (GUILayout.Button("Copy"))
			{
				copiedObjects[0] = sprite.objectReferenceValue;
				copiedObjects[1] = negativeSprite.objectReferenceValue;
				copiedObjects[2] = positiveSprite.objectReferenceValue;
			}

			if (GUILayout.Button("Paste"))
			{
				sprite.objectReferenceValue = copiedObjects[0];
				negativeSprite.objectReferenceValue = copiedObjects[1];
				positiveSprite.objectReferenceValue = copiedObjects[2];
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Full", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(SpriteSize));
			GUILayout.Label("Negative", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(SpriteSize));
			GUILayout.Label("Positive", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(SpriteSize));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}
	}
}