using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(TesseraTile))]
    [CanEditMultipleObjects]
    public class TesseraTileEditor : Editor
    {
        void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Paint");
            EditorGUILayout.EditorToolbar(TesseraTileEditorToolBase.tile3dEditorTools);
            GUILayout.EndHorizontal();
        }
    }
}