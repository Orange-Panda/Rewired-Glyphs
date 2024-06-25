using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LMirman.RewiredGlyphs.Editor
{
	[CustomPropertyDrawer(typeof(GlyphCollection.GuidEntry))]
	public class GlyphCollectionGuidEntryPropertyDrawer : PropertyDrawer
	{
		private const int Spacing = 20;
		private const int Padding = 8;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUIUtility.labelWidth = 100;
			SerializedProperty glyphMap = property.FindPropertyRelative("glyphMap");
			SerializedProperty controllerType = property.FindPropertyRelative("controllerType");
			SerializedProperty guid = property.FindPropertyRelative("guid");
			SerializedProperty controllerDataFilesProperty = property.serializedObject.FindProperty("controllerDataFiles");
			ControllerDataFiles controllerDataFiles = controllerDataFilesProperty.objectReferenceValue as ControllerDataFiles;

			Rect drawPosition = new Rect(position) { height = Spacing - 2 };
			drawPosition.y += Padding;
			EditorGUI.PropertyField(drawPosition, glyphMap);
			drawPosition.y += Spacing;
			EditorGUI.PropertyField(drawPosition, controllerType);
			drawPosition.y += Spacing;

			if (!UsesGuid(property))
			{
				return;
			}

			if (controllerDataFiles == null)
			{
				EditorGUI.PropertyField(drawPosition, guid);
				return;
			}

			Rect labelPosition = new Rect(drawPosition);
			Rect dropdownPosition = new Rect(drawPosition);
			labelPosition.width = 100;
			dropdownPosition.width -= 100;
			dropdownPosition.x += 100;

			bool hasGuid = Guid.TryParse(guid.stringValue, out Guid targetGuid);
			bool hasHardwareTarget = TryGetHardwareMap(controllerDataFiles, targetGuid, out HardwareJoystickMap hardwareTarget);
			string targetName = hasHardwareTarget ? hardwareTarget.ControllerName : "** ERROR: UNASSIGNED **";
			EditorGUI.LabelField(labelPosition, "Hardware");
			if (EditorGUI.DropdownButton(dropdownPosition, new GUIContent(targetName), FocusType.Keyboard))
			{
				GenericMenu hardwareMenu = new GenericMenu();
				IEnumerable<HardwareJoystickMap> hardwareJoystickMaps = controllerDataFiles.HardwareJoystickMaps.OrderBy(map => map.ControllerName);
				foreach (HardwareJoystickMap hardwareJoystickMap in hardwareJoystickMaps)
				{
					bool isSelected = hasGuid && hardwareJoystickMap.Guid == targetGuid;
					hardwareMenu.AddItem(new GUIContent(hardwareJoystickMap.ControllerName), isSelected, Callback, hardwareJoystickMap);
					continue;

					void Callback(object data)
					{
						guid.stringValue = ((HardwareJoystickMap)data).Guid.ToString();
						property.serializedObject.ApplyModifiedProperties();
					}
				}

				hardwareMenu.DropDown(EditorGUILayout.GetControlRect());
			}

			return;

			static bool TryGetHardwareMap(ControllerDataFiles dataFiles, Guid targetGuid, out HardwareJoystickMap hardwareMap)
			{
				hardwareMap = dataFiles != null ? dataFiles.GetHardwareJoystickMap(targetGuid) : null;
				return hardwareMap != null;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return (Spacing * (UsesGuid(property) ? 3 : 2)) + (Padding * 2);
		}

		private static bool UsesGuid(SerializedProperty property)
		{
			ControllerType controllerType = (ControllerType)property.FindPropertyRelative("controllerType").intValue;
			return controllerType == ControllerType.Joystick || controllerType == ControllerType.Custom;
		}
	}
}