#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public class EmojiSpriteAssetGenerator : EditorWindow
{
    private string spritesFolder = "--spritePath--";
    private string savePath = "--assetPath--";
    private int atlasSize = 2048;
    private int padding = 2;

    [MenuItem("Tools/Emoji/Generate TMP Sprite Asset")]
    public static void ShowWindow()
    {
        GetWindow<EmojiSpriteAssetGenerator>("Emoji Generator");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Emoji TMP Sprite Asset Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        spritesFolder = EditorGUILayout.TextField("Sprites Folder", spritesFolder);
        savePath = EditorGUILayout.TextField("Save Asset Path", savePath);
        atlasSize = EditorGUILayout.IntPopup("Atlas Size", atlasSize,
            new[] { "1024", "2048", "4096", "8192" },
            new[] { 1024, 2048, 4096, 8192 });
        padding = EditorGUILayout.IntField("Padding (px)", padding);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate", GUILayout.Height(30)))
        {
            Generate();
        }
    }

    void Generate()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Emoji Generator", "Loading textures...", 0f);

            // 1) PNG 텍스처 로드
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesFolder });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No textures found in folder.", "OK");
                return;
            }

            var textures = new List<Texture2D>();
            var names = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                // 텍스처 import 설정 확인 및 수정
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool needsReimport = false;
                    if (!importer.isReadable)
                    {
                        importer.isReadable = true;
                        needsReimport = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        needsReimport = true;
                    }
                    if (needsReimport)
                    {
                        importer.SaveAndReimport();
                        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                }

                // 텍스처 복사본 생성 (PackTextures용)
                var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                copy.SetPixels(tex.GetPixels());
                copy.Apply();

                textures.Add(copy);
                names.Add(Path.GetFileNameWithoutExtension(path).ToUpperInvariant());

                if (i % 100 == 0)
                {
                    EditorUtility.DisplayProgressBar("Emoji Generator",
                        $"Loading textures... {i}/{guids.Length}", (float)i / guids.Length * 0.3f);
                }
            }

            if (textures.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No valid textures found.", "OK");
                return;
            }

            Debug.Log($"[EmojiGenerator] Loaded {textures.Count} textures");

            // 2) Atlas 생성
            EditorUtility.DisplayProgressBar("Emoji Generator", "Packing atlas...", 0.3f);

            var atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);
            var rects = atlas.PackTextures(textures.ToArray(), padding, atlasSize, false);
            atlas.Apply();

            if (rects == null || rects.Length != textures.Count)
            {
                EditorUtility.DisplayDialog("Error", "Failed to pack textures. Try larger atlas size.", "OK");
                return;
            }

            // 3) 에셋 경로 설정
            string directory = Path.GetDirectoryName(savePath)?.Replace("\\", "/") ?? "Assets";
            string baseName = Path.GetFileNameWithoutExtension(savePath);

            string atlasPath = $"{directory}/{baseName}_Atlas.asset";
            string materialPath = $"{directory}/{baseName}_Material.mat";

            // 디렉토리 생성
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string[] folders = directory.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            // 4) Atlas 텍스처 저장
            EditorUtility.DisplayProgressBar("Emoji Generator", "Saving atlas...", 0.5f);

            // Atlas 이름 설정 (경고 방지)
            atlas.name = baseName + "_Atlas";

            var existingAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (existingAtlas != null)
            {
                EditorUtility.CopySerialized(atlas, existingAtlas);
                existingAtlas.name = baseName + "_Atlas";
                atlas = existingAtlas;
            }
            else
            {
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            // 5) Material 생성/업데이트
            EditorUtility.DisplayProgressBar("Emoji Generator", "Creating material...", 0.6f);

            var shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Error", "TextMeshPro/Sprite shader not found.", "OK");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.shader = shader;
            }
            material.SetTexture("_MainTex", atlas);

            // 6) TMP_SpriteAsset 생성/업데이트
            EditorUtility.DisplayProgressBar("Emoji Generator", "Creating sprite asset...", 0.7f);

            var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(savePath);
            if (spriteAsset == null)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, savePath);
            }

            // 기존 Sub-Asset Sprite 삭제
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(savePath);
            foreach (var sub in subAssets)
            {
                if (sub is Sprite)
                {
                    DestroyImmediate(sub, true);
                }
            }

            // private 필드에 접근하여 리스트 초기화
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            var glyphTableField = typeof(TMP_SpriteAsset).GetField("m_SpriteGlyphTable", bindingFlags);
            var charTableField = typeof(TMP_SpriteAsset).GetField("m_SpriteCharacterTable", bindingFlags);
            var versionField = typeof(TMP_SpriteAsset).GetField("m_Version", bindingFlags);

            var glyphList = new List<TMP_SpriteGlyph>();
            var charList = new List<TMP_SpriteCharacter>();

            glyphTableField?.SetValue(spriteAsset, glyphList);
            charTableField?.SetValue(spriteAsset, charList);

            // 버전을 설정하여 UpgradeSpriteAsset() 건너뛰기
            versionField?.SetValue(spriteAsset, "1.1.0");

            // Material과 Atlas 연결
            spriteAsset.material = material;
            spriteAsset.spriteSheet = atlas;

            // 7) Glyph/Character 테이블 생성
            EditorUtility.DisplayProgressBar("Emoji Generator", "Building glyph tables...", 0.8f);

            for (int i = 0; i < textures.Count; i++)
            {
                var uvRect = rects[i];

                // UV 좌표를 픽셀 좌표로 변환
                int x = Mathf.RoundToInt(uvRect.x * atlas.width);
                int y = Mathf.RoundToInt(uvRect.y * atlas.height);
                int w = Mathf.RoundToInt(uvRect.width * atlas.width);
                int h = Mathf.RoundToInt(uvRect.height * atlas.height);

                // Sprite 생성 (Sub-Asset으로)
                var sprite = Sprite.Create(atlas, new Rect(x, y, w, h), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = names[i];
                AssetDatabase.AddObjectToAsset(sprite, spriteAsset);

                // Glyph 생성
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    sprite = sprite,
                    glyphRect = new GlyphRect(x, y, w, h),
                    metrics = new GlyphMetrics(w, h, 0, h * 0.75f, w),
                    scale = 1.0f
                };
                glyphList.Add(glyph);

                // Character 생성
                var character = new TMP_SpriteCharacter((uint)i, glyph)
                {
                    name = names[i],
                    scale = 1.0f
                };
                charList.Add(character);

                if (i % 100 == 0)
                {
                    EditorUtility.DisplayProgressBar("Emoji Generator",
                        $"Building glyphs... {i}/{textures.Count}",
                        0.8f + (float)i / textures.Count * 0.15f);
                }
            }

            // 8) Lookup 테이블 업데이트 및 저장
            EditorUtility.DisplayProgressBar("Emoji Generator", "Finalizing...", 0.95f);

            // 먼저 에셋 저장
            EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(spriteAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 저장 후 다시 로드
            spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(savePath);

            // UpdateLookupTables 호출 (안전하게)
            try
            {
                spriteAsset.UpdateLookupTables();
                EditorUtility.SetDirty(spriteAsset);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EmojiGenerator] UpdateLookupTables warning: {ex.Message}");
            }

            // 임시 텍스처 정리
            foreach (var tex in textures)
            {
                DestroyImmediate(tex);
            }

            Debug.Log($"[EmojiGenerator] Complete! Characters: {charList.Count}, Glyphs: {glyphList.Count}");

            EditorUtility.DisplayDialog("Success",
                $"Sprite Asset: {savePath}\n" +
                $"Atlas: {atlasPath}\n" +
                $"Material: {materialPath}\n" +
                $"Emoji Count: {charList.Count}",
                "OK");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Error", e.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif
