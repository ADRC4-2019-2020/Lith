using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Tessera
{
    internal static class TextureUtil
    {
        private static Texture eraser;

        public static Texture Eraser => eraser = (eraser ?? EditorGUIUtility.IconContent("Grid.EraserTool").image);

        public static Color GetContrastColor(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            return v > 0.5 ? Color.black : Color.white;
        }

        public static void DrawBox(Rect tileRect, bool selected, TesseraPalette palette, int index)
        {
            var entry = palette.entries[index];
            var c = entry.color;
            var texture = index == 0 ? Eraser : MakeTexture((int)tileRect.width, (int)tileRect.height, c);
            if (selected)
            {
                var contrast = GetContrastColor(c);
                var contrastTexture = MakeTexture((int)tileRect.width, (int)tileRect.height, contrast);
                GUI.Box(tileRect, new GUIContent(contrastTexture, entry.name), GUIStyle.none);
                GUI.DrawTexture(new RectOffset(2, 2, 2, 2).Remove(tileRect), texture);
            }
            else
            {
                //GUI.DrawTexture(tileRect, texture);
                GUI.Box(tileRect, new GUIContent(texture, entry.name), GUIStyle.none);
            }
        }

        public static Texture2D MakeTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height);
            Color[] pixels = Enumerable.Repeat(color, width * height).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }

    public class TesseraTilePaintEditorWindow : EditorWindow
    {
        private Texture2D paintPencil;
        private Texture2D paintEdge;
        private Texture2D paintFace;
        private Texture2D paintVertex;
        private Texture2D cubeAdd;
        private Texture2D cubeRemove;

        [MenuItem("Window/Tessera Tile Painting")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = (TesseraTilePaintEditorWindow)GetWindow(typeof(TesseraTilePaintEditorWindow));
            window.Show();
        }

        private void OnEnable()
        {
            paintPencil = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tile3d/Editor/Resources/paint_pencil.png");
            paintEdge = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tile3d/Editor/Resources/paint_edge.png");
            paintFace = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tile3d/Editor/Resources/paint_face.png");
            paintVertex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tile3d/Editor/Resources/paint_vertex.png");
            cubeAdd = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tile3d/Editor/Resources/cube_add.png");
            cubeRemove = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tile3d/Editor/Resources/cube_remove.png");

            titleContent = new GUIContent("Tile Painting");

            EditorTools.activeToolChanged += () => Repaint();
        }

        void OnGUI()
        {
            GUILayout.Label("View", EditorStyles.boldLabel);

            TesseraTilePaintingState.showBackface = EditorGUILayout.Toggle(new GUIContent("Show Backfaces", "Z to toggle"), TesseraTilePaintingState.showBackface);

            TesseraTilePaintingState.opacity = EditorGUILayout.Slider("Opacity", TesseraTilePaintingState.opacity, 0, 1);

            TesseraTilePaintingState.showAll = EditorGUILayout.Toggle(new GUIContent("Show All"), TesseraTilePaintingState.showAll);

            GUILayout.Label("Painting", EditorStyles.boldLabel);

            // TODO: See C:\Program Files\Unity\Hub\Editor\2019.2.3f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.2d.tilemap\Editor\EditorTools
            // For some further stuff to do here

            GUILayout.Label("Paint Mode");

            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EditorToolbar(TesseraTileEditorToolBase.tile3dEditorTools);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Label("Paint Color");

            var r = EditorGUILayout.BeginVertical();

            r.width = position.width;

            var boxStyle = GUI.skin.box;
            var tileSize = 20;

            var boxRect = boxStyle.margin.Remove(r);
            var innerRect = boxStyle.padding.Remove(boxRect);

            // Get palette 
            TesseraPalette palette = null;
            if(Selection.activeGameObject != null)
            {
                if(Selection.activeGameObject.GetComponent<TesseraTile>() is TesseraTile t)
                {
                    palette = t.palette;
                }
            }
            if(palette == null)
            {
                palette = TesseraPalette.defaultPalette;
            }

            var tilesPerRow = (int)(innerRect.width / tileSize);
            if (tilesPerRow > 0)
            {
                
                innerRect.height = (palette.entryCount + tilesPerRow - 1) / tilesPerRow * tileSize;
                boxRect = boxStyle.padding.Add(innerRect);

                GUI.Box(boxRect, "");

                for (var i = 0; i < palette.entryCount; i++)
                {
                    var x = innerRect.x + (i % tilesPerRow) * tileSize;
                    var y = innerRect.y + (i / tilesPerRow) * tileSize;
                    var tileRect = new Rect(x, y, tileSize, tileSize);

                    var selected = TesseraTilePaintingState.paintIndex == i;
                    if (Event.current.type == EventType.Repaint)
                    {
                        TextureUtil.DrawBox(tileRect, selected, palette, i);
                    }
                }

                if (Event.current.type == EventType.MouseDown)
                {
                    var mousePosition = Event.current.mousePosition;
                    if (innerRect.Contains(mousePosition))
                    {
                        var x = (int)(mousePosition.x - innerRect.x) / tileSize;
                        var y = (int)(mousePosition.y - innerRect.y) / tileSize;
                        var i = x + y * tilesPerRow;
                        TesseraTilePaintingState.paintIndex = i;
                        Repaint();
                    }
                }

                // TODO: Handle tooltip


            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(boxStyle.margin.Add(boxRect).height);

            TesseraTileEditorToolBase.CheckKeyPresses();
        }
    }
}