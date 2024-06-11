using Rewired;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Editor
{
	[CustomEditor(typeof(GlyphCollection))]
	public class GlyphCollectionEditor : UnityEditor.Editor
	{
		private ViewState viewState;
		private readonly HashSet<int> definedControllerTypes = new HashSet<int>();
		private readonly HashSet<Guid> definedGuidValues = new HashSet<Guid>();

		public override void OnInspectorGUI()
		{
			if (Application.isPlaying)
			{
				EditorGUILayout.LabelField("Collection Settings", EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Settings can't be modified while the application is playing.", EditorStyles.wordWrappedLabel);
				return;
			}

			// -- Get Properties --
			SerializedProperty controllerDataFiles = serializedObject.FindProperty("controllerDataFiles");
			SerializedProperty key = serializedObject.FindProperty("key");
			SerializedProperty guidMaps = serializedObject.FindProperty("guidMaps");
			SerializedProperty templateMaps = serializedObject.FindProperty("templateMaps");
			SerializedProperty unboundGlyph = serializedObject.FindProperty("unboundGlyph");
			SerializedProperty nullGlyph = serializedObject.FindProperty("nullGlyph");
			SerializedProperty uninitializedGlyph = serializedObject.FindProperty("uninitializedGlyph");
			// --------------------

			switch (viewState)
			{
				case ViewState.Main:
					DrawMain();
					break;
				case ViewState.ControllerMaps:
					DrawControllerMaps();
					break;
				case ViewState.TemplateMaps:
					DrawTemplateMaps();
					break;
				case ViewState.NonInputGlyphs:
					DrawNonInputGlyphs();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			serializedObject.ApplyModifiedProperties();
			return;

			void DrawMain()
			{
				EditorGUILayout.LabelField("Collection Settings", EditorStyles.boldLabel);
				EditorGUILayout.Space();

				EditorGUILayout.PropertyField(key);
				HelpBoxIf(DoesNotHaveControllerDataFiles, "Controller data file asset required to validate Controller Maps", MessageType.Warning);
				EditorGUILayout.PropertyField(controllerDataFiles);
				EditorGUILayout.Space();

				EditorGUILayout.LabelField("Edit Collection", EditorStyles.boldLabel);
				EditorGUILayout.Space();

				DrawControllerMapValidations();
				Button("Edit Controller Maps", GoToControllerMaps);
				DrawTemplateMapValidations();
				Button("Edit Template Maps", GoToTemplateMaps);
				Button("Edit Non-Input Glyphs", GoToNonInputGlyphs);
			}

			void DrawControllerMaps()
			{
				Button("Return to Main", GoToMain);
				DrawControllerMapValidations();
				EditorGUILayout.LabelField("Glyph Collection - Controller Maps", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Assign hardware specific maps for controllers such as 'Keyboard', 'Mouse', 'Xbox One Controller', or 'DualSense Controller'.",
					EditorStyles.wordWrappedLabel);
				EditorGUILayout.PropertyField(guidMaps);
			}

			void DrawTemplateMaps()
			{
				Button("Return to Main", GoToMain);
				DrawTemplateMapValidations();
				EditorGUILayout.LabelField("Glyph Collection - Template Maps", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Assign template maps for Joystick controllers that can input as \"Gamepad\" template.\n\n" +
				                           "The first entry with \"Auto\" is used for a symbol preference query that doesn't have an entry for it's symbol preference. " +
				                           "If no map is set to \"Auto\" the first map in this list is used instead.", EditorStyles.wordWrappedLabel);
				EditorGUILayout.PropertyField(templateMaps);
			}

			void DrawNonInputGlyphs()
			{
				Button("Return to Main", GoToMain);
				EditorGUILayout.LabelField("Glyph Collection - Non-Input Glyphs", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(unboundGlyph);
				EditorGUILayout.PropertyField(nullGlyph);
				EditorGUILayout.PropertyField(uninitializedGlyph);
			}

			bool DoesNotHaveControllerDataFiles()
			{
				return controllerDataFiles.objectReferenceValue == null;
			}

			bool IsMouseMapMissing()
			{
				for (int i = 0; i < guidMaps.arraySize; i++)
				{
					SerializedProperty element = guidMaps.GetArrayElementAtIndex(i);
					if (element.FindPropertyRelative("controllerType").enumValueFlag == (int)ControllerType.Mouse)
					{
						return false;
					}
				}

				return true;
			}

			bool IsKeyboardMapMissing()
			{
				for (int i = 0; i < guidMaps.arraySize; i++)
				{
					SerializedProperty element = guidMaps.GetArrayElementAtIndex(i);
					if (element.FindPropertyRelative("controllerType").enumValueFlag == (int)ControllerType.Keyboard)
					{
						return false;
					}
				}

				return true;
			}

			bool IsNoTemplateMap()
			{
				return templateMaps.arraySize <= 0;
			}

			bool ControllerMapsHasNull()
			{
				for (int i = 0; i < guidMaps.arraySize; i++)
				{
					SerializedProperty element = guidMaps.GetArrayElementAtIndex(i);
					if (element.FindPropertyRelative("glyphMap").objectReferenceValue == null)
					{
						return true;
					}
				}

				return false;
			}

			bool TemplateMapsHasNull()
			{
				for (int i = 0; i < templateMaps.arraySize; i++)
				{
					SerializedProperty element = templateMaps.GetArrayElementAtIndex(i);
					if (element.FindPropertyRelative("glyphMap").objectReferenceValue == null)
					{
						return true;
					}
				}

				return false;
			}

			bool ControllerMapsHasDuplicate()
			{
				bool hasMouse = false;
				bool hasKeyboard = false;
				definedGuidValues.Clear();
				for (int i = 0; i < guidMaps.arraySize; i++)
				{
					SerializedProperty element = guidMaps.GetArrayElementAtIndex(i);
					SerializedProperty controllerType = element.FindPropertyRelative("controllerType");
					SerializedProperty guid = element.FindPropertyRelative("guid");
					switch ((ControllerType)controllerType.enumValueFlag)
					{
						case ControllerType.Keyboard:
							if (hasKeyboard)
							{
								return true;
							}

							hasKeyboard = true;
							break;
						case ControllerType.Mouse:
							if (hasMouse)
							{
								return true;
							}

							hasMouse = true;
							break;
						case ControllerType.Joystick:
						case ControllerType.Custom:
							if (Guid.TryParse(guid.stringValue, out Guid targetGuid) && !definedGuidValues.Add(targetGuid))
							{
								return true;
							}

							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				return false;
			}

			bool ControllerMapsHasBadGuid()
			{
				for (int i = 0; i < guidMaps.arraySize; i++)
				{
					SerializedProperty element = guidMaps.GetArrayElementAtIndex(i);
					SerializedProperty controllerType = element.FindPropertyRelative("controllerType");
					SerializedProperty guid = element.FindPropertyRelative("guid");
					switch ((ControllerType)controllerType.enumValueFlag)
					{
						case ControllerType.Keyboard:
						case ControllerType.Mouse:
							break;
						case ControllerType.Joystick:
						case ControllerType.Custom:
							if (!Guid.TryParse(guid.stringValue, out Guid _))
							{
								return true;
							}

							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				return false;
			}

			bool TemplateMapsHasDuplicate()
			{
				definedControllerTypes.Clear();
				for (int i = 0; i < templateMaps.arraySize; i++)
				{
					SerializedProperty element = templateMaps.GetArrayElementAtIndex(i);
					if (!definedControllerTypes.Add(element.FindPropertyRelative("symbolPreference").enumValueFlag))
					{
						return true;
					}
				}

				return false;
			}

			void DrawControllerMapValidations()
			{
				HelpBoxIf(IsMouseMapMissing, "No controller map found for Mouse!", MessageType.Error);
				HelpBoxIf(IsKeyboardMapMissing, "No controller map found for Keyboard!", MessageType.Error);
				HelpBoxIf(ControllerMapsHasNull, "Some controller maps have no Glyph Map assigned!", MessageType.Error);
				HelpBoxIf(ControllerMapsHasDuplicate, "There are multiple controller maps that are targeting the same device!\n\n" +
				                                      "Only one map may be defined per device", MessageType.Warning);
				HelpBoxIf(ControllerMapsHasBadGuid, "Some controller maps have an invalid controller target!", MessageType.Warning);
			}

			void DrawTemplateMapValidations()
			{
				HelpBoxIf(IsNoTemplateMap, "There are no template maps! Glyphs may not be shown for all Joystick devices.", MessageType.Error);
				HelpBoxIf(TemplateMapsHasNull, "Some template maps have no Glyph Map assigned!", MessageType.Error);
				HelpBoxIf(TemplateMapsHasDuplicate, "There are multiple template maps that are targeting the same symbol set!\n\n" +
				                                    "Only one map may be defined per symbol", MessageType.Warning);
			}
		}

		private static void HelpBoxIf(Func<bool> condition, string message, MessageType messageType)
		{
			if (condition.Invoke())
			{
				EditorGUILayout.HelpBox(message, messageType);
			}
		}

		private void GoToMain()
		{
			SwitchViewState(ViewState.Main);
		}

		private void GoToControllerMaps()
		{
			SwitchViewState(ViewState.ControllerMaps);
		}

		private void GoToTemplateMaps()
		{
			SwitchViewState(ViewState.TemplateMaps);
		}

		private void GoToNonInputGlyphs()
		{
			SwitchViewState(ViewState.NonInputGlyphs);
		}

		private void SwitchViewState(ViewState value)
		{
			viewState = value;
		}

		private static void Button(string text, Action action)
		{
			if (GUILayout.Button(text))
			{
				action.Invoke();
			}
		}

		private enum ViewState
		{
			Main, ControllerMaps, TemplateMaps, NonInputGlyphs
		}
	}
}