using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LMirman.RewiredGlyphs.Editor
{
	/// <summary>
	/// An editor window that makes modifying <see cref="GlyphMap"/>s easier.
	/// </summary>
	[CustomEditor(typeof(GlyphMap))]
	public class GlyphMapEditor : UnityEditor.Editor
	{
		private bool wantsToValidateMap = true;
		private int mapWarnings;
		private int mapErrors;
		private Vector2 scrollPosition;
		private int page = 1;
		private int PageMax => (serializedObject.FindProperty("glyphs").arraySize / PageSize) + 1;

		private const int PageSize = 10;

		private int requestMoveUpAt = -1;
		private int requestMoveDownAt = -1;
		private int requestDeleteAt = -1;

		private static readonly Object[] CopiedObjects = new Object[3];
		private static readonly HashSet<int> ExpectedIds = new HashSet<int>();

		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();
			GUILayout.Label("Generate and Validate Map", EditorStyles.largeLabel);
			SerializedProperty glyphs = serializedObject.FindProperty("glyphs");
			SerializedProperty controllerDataFilesProperty = serializedObject.FindProperty("controllerDataFiles");
			ControllerDataFiles controllerDataFiles = controllerDataFilesProperty.objectReferenceValue as ControllerDataFiles;

			// -- Create Mouse or Keyboard Glyphs --
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate Keyboard") && EditorUtility.DisplayDialog("Confirm Action", "Are you sure? This action is irreversible.", "Ok", "Cancel"))
			{
				if (!ReInput.isReady || ReInput.controllers.Keyboard == null)
				{
					EditorUtility.DisplayDialog("Error - ReInput not Ready", "ReInput wasn't ready. You may need to have your application running to execute this function.", "Continue");
					return;
				}

				glyphs.ClearArray();
				foreach (ControllerElementIdentifier element in ReInput.controllers.Keyboard.ElementIdentifiers)
				{
					glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
					SerializedProperty glyphProperty = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
					SerializedProperty inputID = glyphProperty.FindPropertyRelative("inputID");
					SerializedProperty description = glyphProperty.FindPropertyRelative("description");
					SerializedProperty positiveDescription = glyphProperty.FindPropertyRelative("positiveDescription");
					SerializedProperty negativeDescription = glyphProperty.FindPropertyRelative("negativeDescription");
					inputID.intValue = element.id;
					description.stringValue = element.name;
					positiveDescription.stringValue = element.positiveName;
					negativeDescription.stringValue = element.negativeName;
				}

				glyphs.serializedObject.ApplyModifiedProperties();
				serializedObject.ApplyModifiedProperties();
				Repaint();
			}

			if (GUILayout.Button("Generate Mouse") && EditorUtility.DisplayDialog("Confirm Action", "Are you sure? This action is irreversible.", "Ok", "Cancel"))
			{
				if (!ReInput.isReady || ReInput.controllers.Mouse == null)
				{
					EditorUtility.DisplayDialog("Error - ReInput not Ready", "ReInput wasn't ready. You may need to have your application running to execute this function.", "Continue");
					return;
				}

				glyphs.ClearArray();
				foreach (ControllerElementIdentifier element in ReInput.controllers.Mouse.ElementIdentifiers)
				{
					glyphs.InsertArrayElementAtIndex(glyphs.arraySize);
					SerializedProperty glyphProperty = glyphs.GetArrayElementAtIndex(glyphs.arraySize - 1);
					SerializedProperty inputID = glyphProperty.FindPropertyRelative("inputID");
					SerializedProperty description = glyphProperty.FindPropertyRelative("description");
					SerializedProperty positiveDescription = glyphProperty.FindPropertyRelative("positiveDescription");
					SerializedProperty negativeDescription = glyphProperty.FindPropertyRelative("negativeDescription");
					inputID.intValue = element.id;
					description.stringValue = element.name;
					positiveDescription.stringValue = element.positiveName;
					negativeDescription.stringValue = element.negativeName;
				}

				glyphs.serializedObject.ApplyModifiedProperties();
				serializedObject.ApplyModifiedProperties();
				Repaint();
			}

			EditorGUILayout.EndHorizontal();

			if (controllerDataFiles == null)
			{
				EditorGUILayout.HelpBox("Controller data file asset required to generate and validate Joystick glyph map", MessageType.Warning, true);
			}

			EditorGUILayout.PropertyField(controllerDataFilesProperty);

			SerializedProperty controllerGuidProperty = serializedObject.FindProperty("controllerGuid");
			bool hasGuid = Guid.TryParse(controllerGuidProperty.stringValue, out Guid targetGuid);
			bool hasTemplateTarget = TryGetTemplateMap(controllerDataFiles, targetGuid, out HardwareJoystickTemplateMap templateTarget);
			bool hasHardwareTarget = TryGetHardwareMap(controllerDataFiles, targetGuid, out HardwareJoystickMap hardwareTarget);
			string targetName = hasHardwareTarget ? hardwareTarget.ControllerName : hasTemplateTarget ? templateTarget.ControllerName : "UNASSIGNED";

			if (controllerDataFiles != null)
			{
				wantsToValidateMap = EditorGUILayout.Toggle("Validate Map", wantsToValidateMap);
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label($"Based on: {targetName}");

				if (EditorGUILayout.DropdownButton(new GUIContent("Set by Controller"), FocusType.Keyboard))
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
				if (EditorUtility.DisplayDialog($"Generate Controller Actions - {targetName}", "Would you like to generate actions? This will erase current glyph definitions.", "Confirm", "Cancel"))
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
			bool validateMap = wantsToValidateMap && controllerGuidProperty.stringValue != string.Empty;
			if (validateMap)
			{
				ExpectedIds.Clear();
				if (hasHardwareTarget)
				{
					foreach (ControllerElementIdentifier identifier in hardwareTarget.ElementIdentifiers)
					{
						ExpectedIds.Add(identifier.id);
					}
				}
				else if (hasTemplateTarget)
				{
					foreach (ControllerTemplateElementIdentifier identifier in templateTarget.ElementIdentifiers)
					{
						ExpectedIds.Add(identifier.id);
					}
				}

				for (int i = 0; glyphs.isArray && i < glyphs.arraySize; i++)
				{
					SerializedProperty glyph = glyphs.GetArrayElementAtIndex(i);
					SerializedProperty inputId = glyph.FindPropertyRelative("inputID");
					ExpectedIds.Remove(inputId.intValue);
				}

				if (ExpectedIds.Count > 0)
				{
					EditorGUILayout.HelpBox($"{ExpectedIds.Count} expected element(s) from the target map are missing from this glyph map!", MessageType.Error);
				}

				// HACK: Technically these values are from the previous draw.
				// I think it is more valuable to show this value before input glyphs though so it is here instead.
				// This could be fixed by doing a separate pass on all the input glyphs but that feels excessive and separates the help box drawing functionality from error calculation.
				if (mapErrors > 0)
				{
					EditorGUILayout.HelpBox($"{mapErrors} error(s) found in glyph map!", MessageType.Error, true);
				}

				if (mapWarnings > 0)
				{
					EditorGUILayout.HelpBox($"{mapWarnings} warning(s) found in glyph map!", MessageType.Warning, true);
				}
			}

			mapErrors = 0;
			mapWarnings = 0;
			if (validateMap)
			{
				for (int i = 0; glyphs.isArray && i < glyphs.arraySize; i++)
				{
					bool drawGlyph = i >= (page - 1) * PageSize && i < page * PageSize;
					ProcessInputGlyph(glyphs.GetArrayElementAtIndex(i), i, drawGlyph);
				}
			}
			else
			{
				for (int i = (page - 1) * PageSize; glyphs.isArray && i < glyphs.arraySize && i < page * PageSize; i++)
				{
					bool drawGlyph = i >= (page - 1) * PageSize && i < page * PageSize;
					ProcessInputGlyph(glyphs.GetArrayElementAtIndex(i), i, drawGlyph);
				}
			}

			if (validateMap)
			{
				foreach (int expectedId in ExpectedIds)
				{
					EditorGUILayout.HelpBox($"Expected glyph with input id of \"{expectedId}\", but no such glyph was found!", MessageType.Error);
				}
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

			void ProcessInputGlyph(SerializedProperty glyph, int index, bool drawGlyph)
			{
				if (!validateMap && !drawGlyph)
				{
					return;
				}

				SerializedProperty inputID = glyph.FindPropertyRelative("inputID");
				SerializedProperty description = glyph.FindPropertyRelative("description");
				SerializedProperty positiveDescription = glyph.FindPropertyRelative("positiveDescription");
				SerializedProperty negativeDescription = glyph.FindPropertyRelative("negativeDescription");
				SerializedProperty sprite = glyph.FindPropertyRelative("sprite");
				SerializedProperty positiveSprite = glyph.FindPropertyRelative("positiveSprite");
				SerializedProperty negativeSprite = glyph.FindPropertyRelative("negativeSprite");

				if (validateMap)
				{
					try
					{
						ValidateMap();
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}

				if (!drawGlyph)
				{
					return;
				}

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
				sprite.objectReferenceValue = EditorGUILayout.ObjectField(sprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(GlyphPropertyDrawer.SpriteSize), GUILayout.Height(GlyphPropertyDrawer.SpriteSize));
				positiveSprite.objectReferenceValue =
					EditorGUILayout.ObjectField(positiveSprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(GlyphPropertyDrawer.SpriteSize), GUILayout.Height(GlyphPropertyDrawer.SpriteSize));
				negativeSprite.objectReferenceValue =
					EditorGUILayout.ObjectField(negativeSprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Width(GlyphPropertyDrawer.SpriteSize), GUILayout.Height(GlyphPropertyDrawer.SpriteSize));
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
				GUILayout.Label("Full", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(GlyphPropertyDrawer.SpriteSize));
				GUILayout.Label("Positive", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(GlyphPropertyDrawer.SpriteSize));
				GUILayout.Label("Negative", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(GlyphPropertyDrawer.SpriteSize));
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
				return;

				void ValidateMap()
				{
					if (!hasHardwareTarget && !hasTemplateTarget)
					{
						HelpBox("Glyph can't be validated due to a missing map", MessageType.Info);
						return;
					}

					string elementName;
					string positiveName;
					string negativeName;

					if (hasHardwareTarget)
					{
						ControllerElementIdentifier elementIdentifier = hardwareTarget.GetElementIdentifier(inputID.intValue);
						if (elementIdentifier == null)
						{
							HelpBox($"The controller map does not contain an action with input id {inputID.intValue}", MessageType.Error);
							mapErrors++;
							return;
						}

						elementName = elementIdentifier.name;
						positiveName = elementIdentifier.positiveName;
						negativeName = elementIdentifier.negativeName;
					}
					else
					{
						ControllerTemplateElementIdentifier elementIdentifier = templateTarget.GetElementIdentifier(inputID.intValue);
						if (elementIdentifier == null)
						{
							HelpBox($"The template map does not contain an action with input id {inputID.intValue}", MessageType.Error);
							mapErrors++;
							return;
						}

						elementName = elementIdentifier.name;
						positiveName = elementIdentifier.positiveName;
						negativeName = elementIdentifier.negativeName;
					}

					if (!string.Equals(elementName, description.stringValue, StringComparison.OrdinalIgnoreCase))
					{
						HelpBox($"Description mismatch between target map and glyph. Expected \"{elementName}\"", MessageType.Warning);
						mapWarnings++;
					}
					else if (!string.Equals(positiveName, positiveDescription.stringValue, StringComparison.OrdinalIgnoreCase))
					{
						HelpBox($"Positive description mismatch between target map and glyph. Expected \"{elementName}\"", MessageType.Warning);
						mapWarnings++;
					}
					else if (!string.Equals(negativeName, negativeDescription.stringValue, StringComparison.OrdinalIgnoreCase))
					{
						HelpBox($"Negative description mismatch between target map and glyph. Expected \"{elementName}\"", MessageType.Warning);
						mapWarnings++;
					}
				}

				void HelpBox(string message, MessageType messageType)
				{
					if (!drawGlyph)
					{
						return;
					}

					EditorGUILayout.HelpBox(message, messageType, true);
				}
			}
		}
	}
}