using System;
using System.Text;
using System.Threading.Tasks;
using Tessera;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(MirrorConstraint))]
    public class MirrorConstraintEditor : Editor
    {
        TileList tileListX;
        TileList tileListZ;

        public void OnEnable()
        {
            tileListX = new TileList(((TesseraConstraint)target).GetComponent<TesseraGenerator>(), "X-Symmetric Tiles", serializedObject.FindProperty("symmetricTilesX"), allowSingleTile: false, allowList: false);
            tileListZ = new TileList(((TesseraConstraint)target).GetComponent<TesseraGenerator>(), "Z-Symmetric Tiles", serializedObject.FindProperty("symmetricTilesZ"), allowSingleTile: false, allowList: false);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var hasSymmetricTilesProperty = serializedObject.FindProperty("hasSymmetricTiles");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(hasSymmetricTilesProperty);
            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ((MirrorConstraint)target).SetSymmetricTiles();
                serializedObject.Update();
            }
            if (hasSymmetricTilesProperty.boolValue)
            {
                tileListX.Draw();
                tileListZ.Draw();
            }
            else
            {
                EditorGUILayout.HelpBox("Symmetric tiles autodetected.", MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
