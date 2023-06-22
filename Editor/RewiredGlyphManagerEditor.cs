using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

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
				// Get the collection to generate from
				GlyphCollection collection = serializedObject.FindProperty("glyphCollection").objectReferenceValue as GlyphCollection;
				if (collection == null)
				{
					Debug.LogError("A glyph collection is required.");
					return;
				}

				// Get all glyphs
				List<Glyph> glyphs = new List<Glyph>() { collection.NullGlyph, collection.UnboundGlyph };
				foreach (GlyphCollection.HardwareEntry hardwareEntry in collection.HardwareMaps)
				{
					EditorUtility.SetDirty(hardwareEntry.glyphMap);
					foreach (Glyph glyph in hardwareEntry.glyphMap.Glyphs)
					{
						glyphs.Add(glyph);
					}
				}

				foreach (GlyphCollection.TemplateEntry templateEntry in collection.TemplateMaps)
				{
					EditorUtility.SetDirty(templateEntry.glyphMap);
					foreach (Glyph glyph in templateEntry.glyphMap.Glyphs)
					{
						glyphs.Add(glyph);
					}
				}

				// Get all unique asset names from all glyphs.
				// Note: We naively check the FullSprite only because exclusively having Positive or Negative sprites on a separate sprite sheet is impractical
				HashSet<string> spriteAssetPaths = new HashSet<string>();
				foreach (Glyph glyph in glyphs)
				{
					Sprite sprite = glyph.FullSprite;
					if (sprite == null)
					{
						continue;
					}

					string assetPath = AssetDatabase.GetAssetPath(sprite);
					spriteAssetPaths.Add(assetPath);
				}

				Dictionary<string, SpriteSheetOutput> spriteSheetOutputs = new Dictionary<string, SpriteSheetOutput>();
				foreach (string spriteAssetPath in spriteAssetPaths)
				{
					Texture2D assetAtPath = AssetDatabase.LoadAssetAtPath<Texture2D>(spriteAssetPath);
					SpriteSheetOutput sheetOutput = GenerateSpriteAsset(assetAtPath);
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
			}
		}

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
			else
			{
				Debug.LogError($"No property found for \"{propertyName}\".");
			}
		}
		
		private static void PopulateSpriteTables(Texture source, ref List<TMP_SpriteCharacter> spriteCharacterTable, ref List<TMP_SpriteGlyph> spriteGlyphTable)
		{
			//Debug.Log("Creating new Sprite Asset.");

			string filePath = AssetDatabase.GetAssetPath(source);

			// Get all the Sprites sorted by Index
			Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(filePath).Select(x => x as Sprite).Where(x => x != null).OrderByDescending(x => x.rect.y).ThenBy(x => x.rect.x).ToArray();

			for (int i = 0; i < sprites.Length; i++)
			{
				Sprite sprite = sprites[i];

				TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph();
				spriteGlyph.index = (uint)i;
				spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, 0, sprite.rect.height * 0.9f, sprite.rect.width);
				spriteGlyph.glyphRect = new GlyphRect(sprite.rect);
				spriteGlyph.scale = 1.0f;
				spriteGlyph.sprite = sprite;

				spriteGlyphTable.Add(spriteGlyph);

				TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);
				spriteCharacter.name = sprite.name;
				spriteCharacter.scale = 1.0f;

				spriteCharacterTable.Add(spriteCharacter);
			}
		}
		
		/// <summary>
		/// Create and add new default material to sprite asset.
		/// </summary>
		/// <param name="spriteAsset"></param>
		// Imported from TMP Source Code 
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