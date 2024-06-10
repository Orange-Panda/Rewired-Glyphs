using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace LMirman.RewiredGlyphs.Editor
{
	[CustomEditor(typeof(RewiredGlyphManager))]
	public class RewiredGlyphManagerEditor : UnityEditor.Editor
	{
		private readonly HashSet<string> usedSpriteSheetNames = new HashSet<string>();

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Generate TMP Sprite Assets") &&
			    EditorUtility.DisplayDialog("Confirm Generation",
				    "This action will create TextMeshPro Sprite Assets for every Collection on this manager, allowing them be used inline with TextMeshPro. This action is irreversible.",
				    "Generate", "Cancel"))
			{
				usedSpriteSheetNames.Clear();
				// Get the collection to generate from
				GlyphCollection collection = serializedObject.FindProperty("glyphCollection").objectReferenceValue as GlyphCollection;
				if (collection == null)
				{
					Debug.LogError("No glyph collection defined on Rewired Glyph Manager.");
				}
				else
				{
					GenerateForCollection(collection);
				}

				SerializedProperty additionalCollections = serializedObject.FindProperty("additionalCollections");
				for (int i = 0; i < additionalCollections.arraySize; i++)
				{
					GlyphCollection additionalCollection = additionalCollections.GetArrayElementAtIndex(i).objectReferenceValue as GlyphCollection;
					if (additionalCollection == null)
					{
						Debug.LogError($"Invalid additional glyph collection on Rewired Glyph Manager at index {i}.");
					}
					else
					{
						GenerateForCollection(additionalCollection);
					}
				}
			}

			return;

			void GenerateForCollection(GlyphCollection collection)
			{
				// Get all glyphs
				List<Glyph> glyphs = new List<Glyph> { collection.NullGlyph, collection.UnboundGlyph, collection.UninitializedGlyph };
				glyphs.AddRange(collection.GuidMaps.SelectMany(guidEntry => guidEntry.glyphMap.Glyphs));
				glyphs.AddRange(collection.HardwareMaps.SelectMany(hardwareEntry => hardwareEntry.glyphMap.Glyphs));
				glyphs.AddRange(collection.TemplateMaps.SelectMany(templateEntry => templateEntry.glyphMap.Glyphs));

				// Get all unique asset names from all glyphs.
				// Note: We naively check the FullSprite only because exclusively having Positive or Negative sprites on a separate sprite sheet is impractical
				HashSet<string> spriteAssetPaths = new HashSet<string>();
				foreach (string assetPath in from glyph in glyphs select glyph.FullSprite into sprite where sprite != null select AssetDatabase.GetAssetPath(sprite))
				{
					spriteAssetPaths.Add(assetPath);
				}

				Dictionary<string, SpriteSheetOutput> spriteSheetOutputs = new Dictionary<string, SpriteSheetOutput>();
				foreach (string spriteAssetPath in spriteAssetPaths)
				{
					Texture2D assetAtPath = AssetDatabase.LoadAssetAtPath<Texture2D>(spriteAssetPath);
					SpriteSheetOutput sheetOutput = GenerateSpriteAsset(assetAtPath);
					if (usedSpriteSheetNames.Add(sheetOutput.spriteAssetName) == false)
					{
						Debug.LogError($"Generated multiple sprite sheets with name \"{sheetOutput.spriteAssetName}\". Multiple sprite sheets can't have the same name, please rename.");
					}

					spriteSheetOutputs.Add(sheetOutput.spriteAssetPath, sheetOutput);
				}

				foreach (Glyph glyph in glyphs)
				{
					Sprite sprite = glyph.FullSprite;
					if (sprite == null)
					{
						continue;
					}

					string assetPath = AssetDatabase.GetAssetPath(sprite);
					if (spriteSheetOutputs.TryGetValue(assetPath, out SpriteSheetOutput spriteSheetOutput))
					{
						glyph.TextMeshSpriteSheetName = spriteSheetOutput.spriteAssetName;
					}
				}

				// Set assets as dirty
				foreach (GlyphCollection.GuidEntry guidEntry in collection.GuidMaps)
				{
					EditorUtility.SetDirty(guidEntry.glyphMap);
				}

				foreach (GlyphCollection.HardwareEntry hardwareEntry in collection.HardwareMaps)
				{
					EditorUtility.SetDirty(hardwareEntry.glyphMap);
				}

				foreach (GlyphCollection.TemplateEntry templateEntry in collection.TemplateMaps)
				{
					EditorUtility.SetDirty(templateEntry.glyphMap);
				}

				EditorUtility.SetDirty(collection);
			}
		}

		// Based on: TMP_SpriteAssetMenu
		private static SpriteSheetOutput GenerateSpriteAsset(Texture2D target)
		{
			// Get the path to the selected asset.
			string filePathWithName = AssetDatabase.GetAssetPath(target);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePathWithName);

			// Create new Sprite Asset
			TMP_SpriteAsset spriteAsset = CreateInstance<TMP_SpriteAsset>();
			Directory.CreateDirectory($"{Application.dataPath}/Resources/{TMP_Settings.defaultSpriteAssetPath}");
			AssetDatabase.CreateAsset(spriteAsset, $"Assets//Resources/{TMP_Settings.defaultSpriteAssetPath}{fileNameWithoutExtension}.asset");
			SetProperty(spriteAsset, nameof(TMP_SpriteAsset.version), "1.1.0");

			// Compute the hash code for the sprite asset.
			spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);

			List<TMP_SpriteGlyph> spriteGlyphTable = new List<TMP_SpriteGlyph>();
			List<TMP_SpriteCharacter> spriteCharacterTable = new List<TMP_SpriteCharacter>();

			// Assign new Sprite Sheet texture to the Sprite Asset.
			spriteAsset.spriteSheet = target;
			PopulateSpriteTables(target, ref spriteCharacterTable, ref spriteGlyphTable);
			SetProperty(spriteAsset, nameof(TMP_SpriteAsset.spriteCharacterTable), spriteCharacterTable);
			SetProperty(spriteAsset, nameof(TMP_SpriteAsset.spriteGlyphTable), spriteGlyphTable);

			// Add new default material for sprite asset.
			AddDefaultMaterial(spriteAsset);

			// Update Lookup tables.
			spriteAsset.UpdateLookupTables();

			// Set dirty
			EditorUtility.SetDirty(spriteAsset);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));
			return new SpriteSheetOutput(filePathWithName, fileNameWithoutExtension);
		}

		private static void SetProperty(TMP_SpriteAsset spriteAsset, string propertyName, object value)
		{
			PropertyInfo propertyInfo = typeof(TMP_SpriteAsset).GetProperty(propertyName);
			if (propertyInfo != null)
			{
				propertyInfo.SetValue(spriteAsset, value);
			}
		}

		// Based on: TMP_SpriteAssetMenu
		private static void PopulateSpriteTables(Texture source, ref List<TMP_SpriteCharacter> spriteCharacterTable, ref List<TMP_SpriteGlyph> spriteGlyphTable)
		{
			string filePath = AssetDatabase.GetAssetPath(source);

			// Get all the Sprites sorted by Index
			Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).OrderByDescending(x => x.rect.y).ThenBy(x => x.rect.x).ToArray();

			for (int i = 0; i < sprites.Length; i++)
			{
				Sprite sprite = sprites[i];
				TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph
				{
					index = (uint)i,
					metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, 0, sprite.rect.height * 0.9f, sprite.rect.width),
					glyphRect = new GlyphRect(sprite.rect),
					scale = 1.0f,
					sprite = sprite
				};

				spriteGlyphTable.Add(spriteGlyph);

				TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph) { name = sprite.name, scale = 1.0f };

				spriteCharacterTable.Add(spriteCharacter);
			}
		}

		// Based on: TMP_SpriteAssetMenu
		private static void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
		{
			Shader shader = Shader.Find("TextMeshPro/Sprite");
			Material material = new Material(shader);
			material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

			spriteAsset.material = material;
			material.hideFlags = HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(material, spriteAsset);
		}

		private class SpriteSheetOutput
		{
			public readonly string spriteAssetPath;
			public readonly string spriteAssetName;

			public SpriteSheetOutput(string spriteAssetPath, string spriteAssetName)
			{
				this.spriteAssetPath = spriteAssetPath;
				this.spriteAssetName = spriteAssetName;
			}
		}
	}
}