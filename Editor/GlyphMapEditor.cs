using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// An editor window that makes modifying <see cref="GlyphMap"/>s easier.
	/// </summary>
	[CustomEditor(typeof(GlyphMap))]
	public class GlyphMapEditor : Editor
	{
		private Vector2 scrollPosition;
		private int page = 1;
		private int PageMax => (serializedObject.FindProperty("glyphs").arraySize / PageSize) + 1;

		private const int PageSize = 10;
		private const int SpriteSize = 96;

		private int requestMoveUpAt = -1;
		private int requestMoveDownAt = -1;
		private int requestDeleteAt = -1;

		private static readonly Object[] CopiedObjects = new Object[3];

		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();
			GUILayout.Label("Generate and Validate Map", EditorStyles.largeLabel);
			SerializedProperty controllerDataFilesProperty = serializedObject.FindProperty("controllerDataFiles");
			ControllerDataFiles controllerDataFiles = controllerDataFilesProperty.objectReferenceValue as ControllerDataFiles;

			if (controllerDataFiles == null)
			{
				EditorGUILayout.HelpBox("Select a controller data files asset to generate and validate your glyph map:", MessageType.Error, true);
			}

			EditorGUILayout.PropertyField(controllerDataFilesProperty);

			SerializedProperty controllerGuidProperty = serializedObject.FindProperty("controllerGuid");
			bool hasGuid = Guid.TryParse(controllerGuidProperty.stringValue, out Guid targetGuid);
			bool hasTemplateTarget = TryGetTemplateMap(controllerDataFiles, targetGuid, out HardwareJoystickTemplateMap templateTarget);
			bool hasHardwareTarget = TryGetHardwareMap(controllerDataFiles, targetGuid, out HardwareJoystickMap hardwareTarget);
			string targetName = hasHardwareTarget ? hardwareTarget.ControllerName : hasTemplateTarget ? templateTarget.ControllerName : "UNASSIGNED";

			SerializedProperty glyphs = serializedObject.FindProperty("glyphs");
			if (controllerDataFiles != null)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label($"Based on: {targetName}");

				if (EditorGUILayout.DropdownButton(new GUIContent("Set by Hardware"), FocusType.Keyboard))
				{
					GenericMenu hardwareMenu = new GenericMenu();
					IEnumerable<HardwareJoystickMap> hardwareJoystickMaps = controllerDataFiles.HardwareJoystickMaps.OrderBy(map => map.ControllerName);
					foreach (HardwareJoystickMap hardwareJoystickMap in hardwareJoystickMaps)
					{
						void Callback(object data)
						{
							controllerGuidProperty.stringValue = ((HardwareJoystickMap)data).Guid.ToString();
							serializedObject.ApplyModifiedProperties();
						}

						bool isSelected = hasGuid && hardwareJoystickMap.Guid == targetGuid;
						hardwareMenu.AddItem(new GUIContent(hardwareJoystickMap.ControllerName), isSelected, Callback, hardwareJoystickMap);
					}

					hardwareMenu.DropDown(EditorGUILayout.GetControlRect());
				}

				if (EditorGUILayout.DropdownButton(new GUIContent("Set by Template"), FocusType.Keyboard))
				{
					GenericMenu templateMenu = new GenericMenu();
					IEnumerable<HardwareJoystickTemplateMap> hardwareJoystickTemplateMaps = controllerDataFiles.JoystickTemplates.OrderBy(map => map.ControllerName);
					foreach (HardwareJoystickTemplateMap templateMap in hardwareJoystickTemplateMaps)
					{
						void Callback(object data)
						{
							controllerGuidProperty.stringValue = ((HardwareJoystickTemplateMap)data).Guid.ToString();
							serializedObject.ApplyModifiedProperties();
						}

						bool isSelected = hasGuid && templateMap.Guid == targetGuid;
						templateMenu.AddItem(new GUIContent(templateMap.ControllerName), isSelected, Callback, templateMap);
					}

					templateMenu.DropDown(EditorGUILayout.GetControlRect());
				}

				if (GUILayout.Button("X"))
				{
					controllerGuidProperty.stringValue = string.Empty;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(10);
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Actions:", EditorStyles.largeLabel);
			if (hasTemplateTarget && GUILayout.Button("Generate Defaults"))
			{
				if (EditorUtility.DisplayDialog($"Generate Template Actions - {targetName}", "Would you like to generate actions? This will erase current glyph definitions.", "Confirm", "Cancel"))
				{
					glyphs.ClearArray();
					foreach (ControllerTemplateElementIdentifier elementIdentifier in templateTarget.ElementIdentifiers)
					{
						glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
						SerializedProperty glyphProperty = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
						SerializedProperty inputID = glyphProperty.FindPropertyRelative("inputID");
						SerializedProperty description = glyphProperty.FindPropertyRelative("description");
						SerializedProperty positiveDescription = glyphProperty.FindPropertyRelative("positiveDescription");
						SerializedProperty negativeDescription = glyphProperty.FindPropertyRelative("negativeDescription");
						inputID.intValue = elementIdentifier.id;
						description.stringValue = elementIdentifier.name;
						positiveDescription.stringValue = elementIdentifier.positiveName;
						negativeDescription.stringValue = elementIdentifier.negativeName;
					}

					glyphs.serializedObject.ApplyModifiedProperties();
					serializedObject.ApplyModifiedProperties();
					Repaint();
					return;
				}
			}
			else if (hasHardwareTarget && GUILayout.Button("Generate Defaults"))
			{
				if (EditorUtility.DisplayDialog($"Generate Hardware Actions - {targetName}", "Would you like to generate actions? This will erase current glyph definitions.", "Confirm", "Cancel"))
				{
					glyphs.ClearArray();
					foreach (ControllerElementIdentifier elementIdentifier in hardwareTarget.ElementIdentifiers)
					{
						glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
						SerializedProperty glyphProperty = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
						SerializedProperty inputID = glyphProperty.FindPropertyRelative("inputID");
						SerializedProperty description = glyphProperty.FindPropertyRelative("description");
						SerializedProperty positiveDescription = glyphProperty.FindPropertyRelative("positiveDescription");
						SerializedProperty negativeDescription = glyphProperty.FindPropertyRelative("negativeDescription");
						inputID.intValue = elementIdentifier.id;
						description.stringValue = elementIdentifier.name;
						positiveDescription.stringValue = elementIdentifier.positiveName;
						negativeDescription.stringValue = elementIdentifier.negativeName;
					}

					glyphs.serializedObject.ApplyModifiedProperties();
					serializedObject.ApplyModifiedProperties();
					Repaint();
					return;
				}
			}

			if (GUILayout.Button("Insert New Element"))
			{
				glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
				SerializedProperty glyphProperty = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
				SerializedProperty inputID = glyphProperty.FindPropertyRelative("inputID");
				SerializedProperty description = glyphProperty.FindPropertyRelative("description");
				inputID.intValue = glyphs.arraySize - 1;
				description.stringValue = "New Input Action";
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Page:", EditorStyles.largeLabel);
			page = EditorGUILayout.IntSlider(page, 1, PageMax);
			EditorGUILayout.EndHorizontal();

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			for (int i = (page - 1) * PageSize; glyphs.isArray && i < glyphs.arraySize && i < page * PageSize; i++)
			{
				DrawInputGlyph(glyphs.GetArrayElementAtIndex(i), i);
			}

			EditorGUILayout.EndScrollView();

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
			return;

			bool TryGetTemplateMap(ControllerDataFiles dataFiles, Guid guid, out HardwareJoystickTemplateMap templateMap)
			{
				templateMap = dataFiles != null ? dataFiles.GetJoystickTemplate(guid) : null;
				return templateMap != null;
			}

			bool TryGetHardwareMap(ControllerDataFiles dataFiles, Guid guid, out HardwareJoystickMap hardwareMap)
			{
				hardwareMap = dataFiles != null ? dataFiles.GetHardwareJoystickMap(guid) : null;
				return hardwareMap != null;
			}
		}

		private void DrawInputGlyph(SerializedProperty glyph, int index)
		{
			SerializedProperty inputID = glyph.FindPropertyRelative("inputID");
			SerializedProperty description = glyph.FindPropertyRelative("description");
			SerializedProperty positiveDescription = glyph.FindPropertyRelative("positiveDescription");
			SerializedProperty negativeDescription = glyph.FindPropertyRelative("negativeDescription");
			SerializedProperty sprite = glyph.FindPropertyRelative("sprite");
			SerializedProperty positiveSprite = glyph.FindPropertyRelative("positiveSprite");
			SerializedProperty negativeSprite = glyph.FindPropertyRelative("negativeSprite");

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = false;
			EditorGUILayout.IntField(index, GUILayout.Width(32));
			GUI.enabled = true;
			inputID.intValue = EditorGUILayout.IntField(inputID.intValue, GUILayout.Width(32));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Full Description", GUILayout.Width(120));
			description.stringValue = EditorGUILayout.TextField(description.stringValue);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Positive Description", GUILayout.Width(120));
			positiveDescription.stringValue = EditorGUILayout.TextField(positiveDescription.stringValue);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Negative Description", GUILayout.Width(120));
			negativeDescription.stringValue = EditorGUILayout.TextField(negativeDescription.stringValue);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			sprite.objectReferenceValue = EditorGUILayout.ObjectField(sprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(SpriteSize), GUILayout.Height(SpriteSize));
			negativeSprite.objectReferenceValue = EditorGUILayout.ObjectField(negativeSprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(SpriteSize), GUILayout.Height(SpriteSize));
			positiveSprite.objectReferenceValue = EditorGUILayout.ObjectField(positiveSprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(SpriteSize), GUILayout.Height(SpriteSize));
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
				CopiedObjects[0] = sprite.objectReferenceValue;
				CopiedObjects[1] = negativeSprite.objectReferenceValue;
				CopiedObjects[2] = positiveSprite.objectReferenceValue;
			}

			if (GUILayout.Button("Paste"))
			{
				sprite.objectReferenceValue = CopiedObjects[0];
				negativeSprite.objectReferenceValue = CopiedObjects[1];
				positiveSprite.objectReferenceValue = CopiedObjects[2];
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