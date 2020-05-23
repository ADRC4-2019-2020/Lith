using UnityEditor;
using UnityEngine;
using System;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Tessera
{
    [CustomEditor(typeof(TesseraGenerator))]
    public class TesseraGeneratorEditor : Editor
    {
        private const string ChangeBounds = "Change Bounds";

        private class CustomHandle : BoxBoundsHandle
        {
            public TesseraGenerator generator;

            protected override Bounds OnHandleChanged(
                PrimitiveBoundsHandle.HandleDirection handle,
                Bounds boundsOnClick,
                Bounds newBounds)
            {
                // Enforce minimum size for bounds
                // And ensure it is property quantized
                switch (handle)
                {
                    case HandleDirection.NegativeX:
                    case HandleDirection.NegativeY:
                    case HandleDirection.NegativeZ:
                        newBounds.min = Vector3.Min(newBounds.min, newBounds.max - generator.tileSize);
                        newBounds.min = Round(newBounds.min - newBounds.max) + newBounds.max;
                        break;
                    case HandleDirection.PositiveX:
                    case HandleDirection.PositiveY:
                    case HandleDirection.PositiveZ:
                        newBounds.max = Vector3.Max(newBounds.max, newBounds.min + generator.tileSize);
                        newBounds.max = Round(newBounds.max - newBounds.min) + newBounds.min;
                        break;
                }
                Undo.RecordObject(generator, ChangeBounds);

                generator.bounds = newBounds;

                return newBounds;
            }

            Vector3 Round(Vector3 m)
            {
                m.x = generator.tileSize.x * ((int)Math.Round(m.x / generator.tileSize.x));
                m.y = generator.tileSize.y * ((int)Math.Round(m.y / generator.tileSize.y));
                m.z = generator.tileSize.z * ((int)Math.Round(m.z / generator.tileSize.z));
                return m;
            }
        }

        private const string GenerateTiles = "Generate tiles";

        private ReorderableList rl;

        SerializedProperty list;
        private GUIStyle headerBackground;
        int controlId;

        int selectorIndex = -1;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;

        CustomHandle h = new CustomHandle();


        private void OnEnable()
        {
            list = serializedObject.FindProperty("tiles");

            rl = new ReorderableList(serializedObject, list, true, false, true, true);


            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {

                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;
                var tileProperty = targetElement.FindPropertyRelative("tile");
                var weightProperty = targetElement.FindPropertyRelative("weight");
                var tile = (TesseraTile)tileProperty.objectReferenceValue;
                var tileName = tile?.gameObject.name ?? "None";

                var tileRect = rect;
                tileRect.height = EditorGUI.GetPropertyHeight(tileProperty);
                var weightRect = rect;
                weightRect.yMin = tileRect.yMax + k_fieldPadding;
                weightRect.height = EditorGUI.GetPropertyHeight(weightProperty);
                EditorGUI.PropertyField(tileRect, tileProperty);
                EditorGUI.PropertyField(weightRect, weightProperty);
            };

            rl.elementHeightCallback = (int index) =>
            {
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                var tileProperty = targetElement.FindPropertyRelative("tile");
                var weightProperty = targetElement.FindPropertyRelative("weight");
                return EditorGUI.GetPropertyHeight(tileProperty) + k_fieldPadding + EditorGUI.GetPropertyHeight(weightProperty) + k_elementPadding;
            };

            rl.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                var styleHighlight = GUI.skin.FindStyle("MeTransitionSelectHead");
                if (focused == false)
                    return;
                rect.height = rl.elementHeightCallback(index);
                GUI.Box(rect, GUIContent.none, styleHighlight);
            };

            rl.onAddCallback = l =>
            {
                ++rl.serializedProperty.arraySize;
                rl.index = rl.serializedProperty.arraySize - 1;
                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("weight").floatValue = 1.0f;
                selectorIndex = rl.index;
                controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<TesseraTile>(this, true, null, controlId);
            };

            var generator = target as TesseraGenerator;

            h.center = generator.center;
            h.size = Vector3.Scale(generator.tileSize, generator.size);
            h.generator = generator;
        }

        public override void OnInspectorGUI()
        {

            this.headerBackground = this.headerBackground ?? (GUIStyle)"RL Header";
            serializedObject.Update();


            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_center"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backtrack"));
            if (!((TesseraGenerator)target).backtrack)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("retries"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skyBox"));
            TileListGUI();
            serializedObject.ApplyModifiedProperties();

            var generator = target as TesseraGenerator;

            EditorUtility.ClearProgressBar();

            var clearable = !GetTileOutput(generator).IsEmpty;

            GUI.enabled = clearable;

            if (GUILayout.Button("Clear"))
            {
                Clear();
            }

            GUI.enabled = true;

            if (GUILayout.Button(clearable ? "Regenerate" : "Generate"))
            {
                // Undo the last generation
                Undo.SetCurrentGroupName(GenerateTiles);
                if (clearable)
                {
                    Clear();
                }

                Generate(generator);
            }
        }

        private void Clear()
        {
            var generator = target as TesseraGenerator;
            var tileOutput = GetTileOutput(generator);
            tileOutput.ClearTiles();
        }

        private ITesseraTileOutput GetTileOutput(TesseraGenerator generator)
        {
            var tileOutput = generator.GetComponent<ITesseraTileOutput>();

            if (tileOutput is TesseraTilemapOutput tmo)
            {
                return new RegisterUndo(tmo?.tilemap?.gameObject, tmo);
            }
            if (tileOutput is TesseraMeshOutput tmo2)
            {
                // TODO: Manually handle mesh serialization for undo
                // https://answers.unity.com/questions/607527/is-this-possible-to-apply-undo-to-meshfiltermesh.html
                return tmo2;
            }
            if(tileOutput == null)
            {
                return new EditorTileOutout(generator.transform);
            }
            else
            {
                // Something custom, just use it verbatim
                return tileOutput;
            }
        }

        private class RegisterUndo : ITesseraTileOutput
        {
            private readonly UnityEngine.Object go;
            private readonly ITesseraTileOutput underlying;

            public RegisterUndo(UnityEngine.Object go, ITesseraTileOutput underlying)
            {
                this.go = go;
                this.underlying = underlying;
            }

            public bool IsEmpty => underlying.IsEmpty;

            public bool SupportsIncremental => false;

            public void ClearTiles()
            {
                if (go != null)
                {
                    Undo.RegisterCompleteObjectUndo(go, GenerateTiles);
                }
                underlying.ClearTiles();
            }

            public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
            {
                if (go != null)
                {
                    Undo.RegisterCompleteObjectUndo(go, GenerateTiles);
                }
                underlying.UpdateTiles(tileInstances);
            }
        }

        private class EditorTileOutout : ITesseraTileOutput
        {
            private Transform transform;

            public EditorTileOutout(Transform transform)
            {
                this.transform = transform;
            }

            public bool IsEmpty => transform.childCount == 0;

            public bool SupportsIncremental => true;

            public void ClearTiles()
            {
                var children = transform.Cast<Transform>().ToList();
                foreach (var child in children)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            public void UpdateTiles(IEnumerable<TesseraTileInstance> tileInstances)
            {

                foreach (var i in tileInstances)
                {
                    foreach (var go in TesseraGenerator.Instantiate(i, transform))
                    {
                        Undo.RegisterCreatedObjectUndo(go, GenerateTiles);
                    }
                }
            }
        }

        // Wraps generation with a progress bar and cancellation button.
        private void Generate(TesseraGenerator generator)
        {
            var cts = new CancellationTokenSource();
            string progressText = "";
            float progress = 0.0f;

            var tileOutput = GetTileOutput(generator);

            void OnComplete(TesseraCompletion completion)
            {
                if (!completion.success)
                {
                    if (completion.contradictionLocation != null)
                    {
                        var loc = completion.contradictionLocation;
                        Debug.LogError($"Failed to complete generation, issue at tile {loc}");
                    }
                    else
                    {
                        Debug.LogError("Failed to complete generation");
                    }
                    return;
                }

                if(tileOutput != null)
                {
                    tileOutput.UpdateTiles(completion.tileInstances);
                }
                else
                {
                }
            }

            var enumerator = generator.StartGenerate(new TesseraGenerateOptions
            {
                onComplete = OnComplete,
                progress = (t, p) => { progressText = t; progress = p; },
                cancellationToken = cts.Token
            });

            var last = DateTime.Now;
            // Update progress this frequently.
            // Too fast and it'll slow down generation.
            var freq = TimeSpan.FromSeconds(0.1);
            try
            {
                while (enumerator.MoveNext())
                {
                    var a = enumerator.Current;
                    if (last + freq < DateTime.Now)
                    {
                        last = DateTime.Now;
                        if (EditorUtility.DisplayCancelableProgressBar("Generating", progressText, progress))
                        {
                            cts.Cancel();
                            EditorUtility.ClearProgressBar();
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            EditorUtility.ClearProgressBar();
            GUIUtility.ExitGUI();
        }

        protected virtual void OnSceneGUI()
        {
            var generator = target as TesseraGenerator;

            if (Event.current.type == EventType.MouseDown)
            {
                mouseDown = true;
            }
            if (Event.current.type == EventType.MouseUp)
            {
                mouseDown = false;
            }

            EditorGUI.BeginChangeCheck();
            Handles.matrix = generator.gameObject.transform.localToWorldMatrix;
            h.DrawHandle();
            Handles.matrix = Matrix4x4.identity;
            if (EditorGUI.EndChangeCheck())
            {
            }

            if (!mouseDown)
            {
                h.center = generator.center;
                h.size = Vector3.Scale(generator.tileSize, generator.size);
            }
            else
            {
            }
        }

        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);
        private bool mouseDown;

        private void TileListGUI()
        {
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                if (selectorIndex >= 0)
                {
                    var tileObject = (GameObject)EditorGUIUtility.GetObjectPickerObject();
                    var tile = tileObject.GetComponent<TesseraTile>();
                    list.GetArrayElementAtIndex(selectorIndex).FindPropertyRelative("tile").objectReferenceValue = tile;
                }
            }
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                selectorIndex = -1;
            }

            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, new GUIContent("Tiles"));

            if (list.isExpanded)
            {
                var r1 = GUILayoutUtility.GetLastRect();

                rl.DoLayoutList();

                var r2 = GUILayoutUtility.GetLastRect();

                var r = new Rect(r1.xMin, r1.yMax, r1.width, r2.yMax - r1.yMax);

                if (r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            var t = (DragAndDrop.objectReferences[i] as TesseraTile) ?? (DragAndDrop.objectReferences[i] as GameObject)?.GetComponent<TesseraTile>();
                            if (t != null)
                            {
                                ++rl.serializedProperty.arraySize;
                                rl.index = rl.serializedProperty.arraySize - 1;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("weight").floatValue = 1.0f;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("tile").objectReferenceValue = t;
                            }
                        }
                        Event.current.Use();
                    }
                }
            }



            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && rl.index >= 0)
            {
                list.DeleteArrayElementAtIndex(rl.index);
                if (rl.index >= list.arraySize - 1)
                {
                    rl.index = list.arraySize - 1;
                }
            }
        }
    }
}