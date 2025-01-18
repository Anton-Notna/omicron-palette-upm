using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OmicronPalette
{
    [CustomEditor(typeof(Palette))]
    public class PaletteEditor : Editor
    {
        private static readonly Dictionary<int, Texture2D> _textures = new Dictionary<int, Texture2D>();
        private static readonly HashSet<int> _activeTextures = new HashSet<int>();

        private Texture2D _lastGeneration;
        private bool _lastGenerationSaved;

        private void OnDisable()
        {
            UnmarkTextures();
            DestroyUnmarkedTextures();

            if (_lastGeneration != null)
            {
                GameObject.DestroyImmediate(_lastGeneration);
                _lastGeneration = null;
            }
        }

        public override void OnInspectorGUI()
        {
            var palette = (Palette)target;
            serializedObject.Update();
            UnmarkTextures();
            DrawDefaultInspector();
            DestroyUnmarkedTextures();

            GUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("Sort"))
                {
                    Undo.RecordObject(palette, "Sort Palette Units");
                    palette.Sort();
                    EditorUtility.SetDirty(palette);
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            {
                GUILayout.Label($"Resolution: {palette.Resolution}x{palette.Resolution}");
                GUILayout.Label($"Max Units: {palette.Raws * palette.Columns}");
                GUILayout.Label($"Unit Resolution: {palette.Resolution / palette.Columns}x{palette.Resolution / palette.Raws}");

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Generate Preview"))
                    {
                        _lastGeneration = GenerateTexture(palette);
                        _lastGenerationSaved = false;
                    }

                    if (GUILayout.Button("Save PNG"))
                    {
                        _lastGeneration = GenerateAndSaveTexture(palette);
                        _lastGenerationSaved = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            

            if (_lastGeneration != null)
            {
                GUILayout.BeginVertical("box");
                {
                    GUILayout.Label("Last Saved Texture:");
                    Rect rect = GUILayoutUtility.GetAspectRect(1f);
                    EditorGUI.DrawPreviewTexture(rect, _lastGeneration);
                }
                GUILayout.EndVertical();
            }
        }

        public static Texture2D GetUnitTexture(PaletteUnit unit, out int hash)
        {
            hash = unit.GetHashCode();
            if (_textures.TryGetValue(hash, out Texture2D texture) == false)
            {
                texture = GenerateUnitTexture(unit);
                _textures.Add(hash, texture);
            }

            if (texture == null)
            {
                texture = GenerateUnitTexture(unit);
                _textures[hash] = texture;
            }

            return texture;
        }

        public static void MarkTexture(int hash) => _activeTextures.Add(hash);

        private static void UnmarkTextures() => _activeTextures.Clear();

        private static void DestroyUnmarkedTextures()
        {
            var toDestroy = new List<KeyValuePair<int, Texture2D>>();

            foreach (var item in _textures)
            {
                if (_activeTextures.Contains(item.Key))
                    continue;

                toDestroy.Add(item);
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                var item = toDestroy[i];
                _activeTextures.Remove(item.Key);
                if (item.Value != null)
                    GameObject.DestroyImmediate(item.Value);
            }
        }

        private static Texture2D GenerateUnitTexture(PaletteUnit unit)
        {
            int width = 128;
            int height = 1;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] gradient = new Color[width];
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                gradient[x] = unit.Interpolate(t);
            }

            texture.SetPixels(gradient);
            texture.Apply();

            return texture;
        }

        private Texture2D GenerateAndSaveTexture(Palette palette)
        {
            string path = EditorUtility.SaveFilePanel("Save Palette Texture", "", "PaletteTexture.png", "png");

            if (string.IsNullOrEmpty(path))
                return null;

            Texture2D texture = GenerateTexture(palette);
            SaveTextureAsPNG(texture, path);
            AssetDatabase.Refresh();

            return texture;
        }

        private Texture2D GenerateTexture(Palette palette)
        {
            int resolution = palette.Resolution;
            int columns = palette.Columns;
            int raws = palette.Raws;

            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            int cellWidth = resolution / columns;
            int cellHeight = resolution / raws;

            Color black = Color.black;
            texture.SetPixels(Enumerable.Repeat(black, resolution * resolution).ToArray());

            int unitIndex = 0;
            for (int row = 0; row < raws; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    if (unitIndex >= palette.Units.Count)
                        break;

                    PaletteUnit unit = palette.Units[unitIndex];
                    DrawUnit(texture, unit, col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                    unitIndex++;
                }
            }

            texture.Apply();
            return texture;
        }

        private void DrawUnit(Texture2D texture, PaletteUnit unit, int offsetX, int offsetY, int cellWidth, int cellHeight)
        {
            for (int y = 0; y < cellHeight; y++)
            {
                float t = (float)y / (cellHeight - 1);
                Color color = unit.Interpolate(t);

                for (int x = 0; x < cellWidth; x++)
                {
                    texture.SetPixel(offsetX + x, offsetY + y, color);
                }
            }
        }

        private void SaveTextureAsPNG(Texture2D texture, string path)
        {
            byte[] pngData = texture.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                Debug.Log($"Texture saved to {path}");
            }
            else
            {
                Debug.LogError("Failed to encode texture to PNG.");
            }
        }
    }
}