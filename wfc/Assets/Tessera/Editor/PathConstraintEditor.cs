using System;
using System.Text;
using System.Threading.Tasks;
using Tessera;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(PathConstraint))]
    public class PathConstraintEditor : Editor
    {
        TileList tileList;
        ColorList colorList;

        public void OnEnable()
        {
            var generator = ((TesseraConstraint)target).GetComponent<TesseraGenerator>();
            tileList = new TileList(generator, "Path Tiles", serializedObject.FindProperty("pathTiles"), allowSingleTile: false);
            colorList = new ColorList(serializedObject.FindProperty("pathColors"), generator.palette);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasPathTiles"));
            if (serializedObject.FindProperty("hasPathTiles").boolValue)
            {
                tileList.Draw();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasPathColors"));
            if (serializedObject.FindProperty("hasPathColors").boolValue)
            {
                colorList.Draw();
            }
            if (!serializedObject.FindProperty("hasPathTiles").boolValue && !serializedObject.FindProperty("hasPathColors").boolValue)
            {
                EditorGUILayout.LabelField("Warning: Path Constraint needs at least one of path tiles or path colors.");
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
