using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Tessera
{
    [CanEditMultipleObjects]
    public abstract class TesseraTileEditorToolBase : EditorTool
    {
        private static string Paint = "Paint";
        private static string AddCubes = "Add Cubes";
        private static string RemoveCubes = "Remove Cubes";

        protected abstract PaintMode PaintMode { get; }

        private static EditorTool[] s_Tile3dEditorTools = null;
        private static float s_Tile3dEditorToolsToolbarSize = 0.0f;

        /// <summary>
        /// All currently active Editor Tools which work with the Tile Palette
        /// </summary>
        public static EditorTool[] tile3dEditorTools
        {
            get
            {
                if (IsCachedEditorToolsInvalid())
                    InstantiateEditorTools();
                return s_Tile3dEditorTools;
            }
        }

        /// <summary>
        /// The horizontal size of a Toolbar with all the TilemapEditorTools
        /// </summary>
        public static float tilemapEditorToolsToolbarSize
        {
            get
            {
                if (IsCachedEditorToolsInvalid())
                    InstantiateEditorTools();
                return s_Tile3dEditorToolsToolbarSize;
            }
        }

        public static EditorTool[] S_Tile3dEditorTools { get => s_Tile3dEditorTools; set => s_Tile3dEditorTools = value; }

        internal static bool IsActive(Type toolType)
        {
            return EditorTools.activeToolType != null && EditorTools.activeToolType == toolType;
        }

        private static bool IsCachedEditorToolsInvalid()
        {

            return s_Tile3dEditorTools == null || s_Tile3dEditorTools.Length == 0
                // Editor tools keep being set to null?
                || s_Tile3dEditorTools.Any(x => x == null);
        }

        private static void InstantiateEditorTools()
        {
            s_Tile3dEditorTools = new EditorTool[]
            {
            CreateInstance<PencilPaintTool>(),
            CreateInstance<EdgePaintTool>(),
            CreateInstance<FacePaintTool>(),
            CreateInstance<VertexPaintTool>(),
            CreateInstance<AddCubeTool>(),
            CreateInstance<RemoveCubeTool>(),
            };
            GUIStyle toolbarStyle = "Command";
            s_Tile3dEditorToolsToolbarSize = s_Tile3dEditorTools.Sum(x => toolbarStyle.CalcSize(x.toolbarIcon).x);
        }


        // https://forum.unity.com/threads/tools-api.587716/
        #region Temporary workaround for Activate and Deactive methods being missing..

        private static TesseraTileEditorToolBase _instance;
        private static bool _isToolActivated;
        [InitializeOnLoadMethod]
        static void CheckForToolChange()
        {
            EditorTools.activeToolChanged += () =>
            {
                var toolIsActive = EditorTools.activeToolType.IsSubclassOf(typeof(TesseraTileEditorToolBase));
                if (toolIsActive && _isToolActivated == false)
                {
                    _isToolActivated = true;
                    _instance.Activate();
                }
                else if (toolIsActive == false && _isToolActivated)
                {
                    _isToolActivated = false;
                    _instance.Deactivate();
                }
            };
        }

        void OnEnable()
        {
            _instance = this;
        }

        #endregion

        private void Activate()
        {
            TesseraTilePaintEditorWindow.Init();
        }

        private void Deactivate()
        {

        }

        private float Max(Vector3 v)
        {
            return Mathf.Max(v.x, v.y, v.z);
        }

        private float Min(Vector3 v)
        {
            return Mathf.Min(v.x, v.y, v.z);
        }

        private IEnumerable<TesseraTile> Tile3dTargets()
        {
            var targetList = targets;
            if(TesseraTilePaintingState.showAll)
            {
                targetList = FindObjectsOfType(typeof(TesseraTile));
            }
            foreach (var t in targetList)
            {
                if (t is TesseraTile tile3d)
                {
                    yield return tile3d;
                }
                else if (t is GameObject g)
                {
                    tile3d = g.GetComponent<TesseraTile>();
                    if (tile3d != null)
                    {
                        yield return tile3d;
                    }
                }
            }
        }

        // Casts a ray at an axis aligned box, and reutrns which face and where the hit is.
        private bool Raycast(Vector3 center, Vector3 hsize, Ray ray, out FaceDir faceDir, out Vector3 point)
        {
            var dir = ray.direction.normalized;
            // r.dir is unit direction vector of ray
            var dirfrac = new Vector3(1 / dir.x, 1 / dir.y, 1 / dir.z);
            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            Vector3 t1 = Vector3.Scale(center - hsize - ray.origin, dirfrac);
            Vector3 t2 = Vector3.Scale(center + hsize - ray.origin, dirfrac);

            var t3 = Vector3.Min(t1, t2);
            float tmin = Max(t3);
            float tmax = Min(Vector3.Max(t1, t2));

            float t;
            // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
            if (tmax < 0)
            {
                t = tmax;
                faceDir = default;
                point = default;
                return false;
            }

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                t = tmax;
                faceDir = default;
                point = default;
                return false;
            }

            t = tmin;
            point = ray.origin + t * dir;
            faceDir = tmin == t3.x ? (dir.x > 0 ? FaceDir.Left : FaceDir.Right)
                : tmin == t3.y ? (dir.y > 0 ? FaceDir.Down : FaceDir.Up)
                : (dir.z > 0 ? FaceDir.Back : FaceDir.Forward);
            return true;
        }

        private struct Util
        {
            public TesseraTile t;
            public Vector3 center;
            public Vector3 size;
            public Vector3 hsize;

            public Util(TesseraTile t)
            {
                this.t = t;
                center = t.center;
                size = t.tileSize;
                hsize = size * 0.5f;
            }


            public Vector3 ScaleByHSize(Vector3 v)
            {
                return new Vector3(v.x * hsize.x, v.y * hsize.y, v.z * hsize.z);
            }

            public Vector3 ScaleBySize(Vector3 v)
            {
                return new Vector3(v.x * size.x, v.y * size.y, v.z * size.z);
            }
        }

        private class Hit
        {
            public TesseraTile target;
            public Vector3Int offset;
            public FaceDir dir;
            public Vector3 point;
            public Vector2Int p;
        }

        // Ray casts to all targets and finds what the cursor is pointing at, if anything
        private Hit FindHit(PaintMode? forcePaintMode = null)
        {
            Hit best = null;
            foreach (var t in Tile3dTargets())
            {
                var u = new Util(t);

                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                var localRay = new Ray(t.transform.InverseTransformPoint(ray.origin), t.transform.InverseTransformVector(ray.direction));

                if (TesseraTilePaintingState.showBackface)
                {
                    localRay = new Ray(localRay.origin + 1000 * localRay.direction, -localRay.direction);
                }

                foreach (var offset in t.offsets)
                {
                    var currentHit = Raycast(t.center + u.ScaleBySize(offset), u.hsize, localRay, out var currentHitDir, out var currentHitPoint);

                    if (!currentHit)
                        continue;

                    if (best == null || (currentHitPoint - localRay.origin).sqrMagnitude < (best.point - localRay.origin).sqrMagnitude)
                    {
                        best = new Hit
                        {
                            target = t,
                            offset = offset,
                            dir = currentHitDir,
                            point = currentHitPoint,
                        };

                        var up = currentHitDir.Up();
                        var forward = currentHitDir.Forward();
                        var right = Vector3.Cross(forward, up);

                        var p = currentHitPoint - (t.center + u.ScaleBySize(offset) + u.ScaleByHSize(forward));
                        p = new Vector3(p.x / u.hsize.x, p.y / u.hsize.y, p.z / u.hsize.z);
                        var p2 = new Vector2(Vector3.Dot(p, right), Vector3.Dot(p, up));
                        // p2 now normalized to [-1, 1] on each axis, in appropriate plane
                        switch (forcePaintMode ?? PaintMode)
                        {
                            case PaintMode.Vertex:
                                best.p = new Vector2Int(
                                    (int)(p2.x + 1) * 2 - 1,
                                    (int)(p2.y + 1) * 2 - 1);
                                break;
                            case PaintMode.Edge:
                                if (Math.Abs(p2.x) > Math.Abs(p2.y))
                                {
                                    best.p = new Vector2Int(Math.Sign(p2.x), 0);
                                }
                                else
                                {
                                    best.p = new Vector2Int(0, Math.Sign(p2.y));
                                }
                                break;
                            default:
                                best.p = new Vector2Int(
                                    (int)((p2.x + 1) / 2.0f * 3.0f) - 1,
                                    (int)((p2.y + 1) / 2.0f * 3.0f) - 1);
                                break;
                        }
                    }
                }
            }
            return best;
        }

        // Returns if the hit would affect the selected location with the given paiting mode
        bool IsHighlight(Hit hit, TesseraTile t, Vector3Int offset, FaceDir dir, Vector2Int p)
        {
            if (hit?.target != t)
                return false;

            var up = dir.Up();
            var forward = dir.Forward();
            var right = Vector3.Cross(forward, up);


            var up2 = hit.dir.Up();
            var forward2 = hit.dir.Forward();
            var right2 = Vector3.Cross(forward2, up2);

            Vector3 v1, v2;

            switch (PaintMode)
            {
                case PaintMode.Pencil:
                    return offset == hit.offset && dir == hit.dir && p == hit.p;
                case PaintMode.Vertex:
                    v1 = offset * 2 + forward + right * p.x + up * p.y;
                    v2 = hit.offset * 2 + forward2 + right2 * hit.p.x + up2 * hit.p.y;
                    return (v1 - v2).sqrMagnitude < 0.01;
                case PaintMode.Edge:
                    var v = forward2 + hit.p.x * right2 + hit.p.y * (Vector3)up2;
                    var isX = Math.Abs(Vector3.Dot(v, right));
                    var isY = Math.Abs(Vector3.Dot(v, up));
                    v1 = offset * 2 + forward + right * p.x * isX + (Vector3)up * p.y * isY;
                    v2 = hit.offset * 2 + forward2 + right2 * hit.p.x + up2 * hit.p.y;
                    return (v1 - v2).sqrMagnitude < 0.01;
                case PaintMode.Face:
                case PaintMode.Add:
                    return offset == hit.offset && dir == hit.dir;
                case PaintMode.Remove:
                    return offset == hit.offset;
            }

            return false;
        }

        // Draws all the targets, plus highlighting
        private void Repaint(Hit hit, int controlId)
        {
            var facesToDraw = new List<(Hit, FaceDetails, int)>();

            foreach (var t in Tile3dTargets())
            {
                foreach (var (offset, dir, faceDetails) in t.faceDetails)
                {
                    var u = new Util(t);

                    var up = dir.Up();
                    var forward = dir.Forward();
                    var right = Vector3.Cross(forward, up);

                    var currentCamera = Camera.current;
                    var point = currentCamera.transform.position - t.transform.TransformPoint(t.center + u.ScaleBySize(offset) + u.ScaleByHSize(forward));

                    bool backface;
                    if (currentCamera.orthographic)
                    {
                        backface = Vector3.Dot(t.transform.TransformVector(forward), currentCamera.transform.forward) > 0;
                    }
                    else
                    {
                        backface = Vector3.Dot(t.transform.TransformVector(forward), point) < 0;
                    }

                    //Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    if (backface ^ (TesseraTilePaintingState.showBackface))
                        continue;

                    foreach (var (p, index) in faceDetails)
                    {
                        var h = new Hit
                        {
                            target = t,
                            offset = offset,
                            dir = dir,
                            p = p,
                            point = point,
                        };
                        facesToDraw.Add((h, faceDetails, index));
                    }
                }
            }

            // Painters algorithm for drawing
            facesToDraw = facesToDraw.OrderBy(x => -x.Item1.point.sqrMagnitude).ToList();

            // Work out when to draw wireframes
            var doWireframes = new bool[facesToDraw.Count];
            var drawnWireframes = new HashSet<(TesseraTile, Vector3Int, FaceDir)>();
            for (var i = facesToDraw.Count - 1; i >= 0; i--)
            {
                var h = facesToDraw[i].Item1;
                doWireframes[i] = drawnWireframes.Add((h.target, h.offset, h.dir));
            }


            foreach (var ((h, faceDetails, index), doWireframe) in facesToDraw.Zip(doWireframes, (a, b) => Tuple.Create(a, b)))
            {
                var t = h.target;
                var offset = h.offset;
                var dir = h.dir;
                var p = h.p;

                var u = new Util(t);

                var up = dir.Up();
                var forward = dir.Forward();
                var right = Vector3.Cross(forward, up);


                var isHighlight = HandleUtility.nearestControl == controlId && IsHighlight(hit, t, offset, dir, p);
                var paintIndex = isHighlight ? TesseraTilePaintingState.paintIndex : index;
                var palette = t.palette ?? TesseraPalette.defaultPalette;
                var color = palette.GetColor(paintIndex);
                if (paintIndex == 0)
                {
                    color.a = TesseraTilePaintingState.opacity * 0.5f;
                }
                else if (isHighlight)
                {
                    color.a = TesseraTilePaintingState.opacity * 1.2f;
                }
                else
                {
                    color.a = TesseraTilePaintingState.opacity;
                }
                Handles.color = color;

                var corner = t.center + u.ScaleBySize(offset) + u.ScaleByHSize(forward) + (-1 + (p.y + 1) * 2.0f / 3.0f) * u.ScaleByHSize(up) + (-1 + (p.x + 1) * 2.0f / 3.0f) * u.ScaleByHSize(right);
                var v1 = t.transform.TransformPoint(corner);
                var v2 = t.transform.TransformPoint(corner + 2.0f / 3.0f * u.ScaleByHSize(right));
                var v4 = t.transform.TransformPoint(corner + 2.0f / 3.0f * u.ScaleByHSize(up));
                var v3 = t.transform.TransformPoint(corner + +2.0f / 3.0f * u.ScaleByHSize(right) + 2.0f / 3.0f * u.ScaleByHSize(up));

                Handles.DrawAAConvexPolygon(v1, v2, v3, v4);


                if (doWireframe)
                {
                    corner = t.center + u.ScaleBySize(offset) + u.ScaleByHSize(forward) + -1 * u.ScaleByHSize(up) + -1 * u.ScaleByHSize(right);
                    v1 = t.transform.TransformPoint(corner);
                    v2 = t.transform.TransformPoint(corner + 2.0f * u.ScaleByHSize(right));
                    v4 = t.transform.TransformPoint(corner + 2.0f * u.ScaleByHSize(up));
                    v3 = t.transform.TransformPoint(corner + 2.0f * u.ScaleByHSize(right) + 2.0f * u.ScaleByHSize(up));
                    Handles.color = Color.black;
                    Handles.DrawAAPolyLine(v1, v2, v3, v4);
                }
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (Tile3dTargets().Count() == 0)
            {
                return;
            }

            int controlId = EditorGUIUtility.GetControlID(FocusType.Passive);

            var hit = FindHit();

            if (hit != null)
            {
                EditorGUIUtility.AddCursorRect(new Rect(-5000, -5000, 10000, 10000), MouseCursor.Arrow);
            }

            var pencilHit = FindHit(PaintMode.Pencil);

            // Show tooltip
            if (pencilHit != null)
            {
                var palette = pencilHit.target.palette ?? TesseraPalette.defaultPalette;
                var index = pencilHit.target.Get(pencilHit.offset, pencilHit.dir)[pencilHit.p];
                var entry = palette.entries[index];
                var content = new GUIContent("", entry.name);
                var style = GUI.skin.label;
                var position = pencilHit.target.transform.TransformPoint(pencilHit.point);
                Handles.BeginGUI();
                var rect = HandleUtility.WorldPointToSizedRect(position, content, style);
                GUI.Label(rect, content, style);
                Handles.EndGUI();
                //Handles.Label(position, content, style);
            }

            void DoPaint()
            {
                var t = hit.target;
                if (PaintMode == PaintMode.Add)
                {
                    Undo.RecordObject(t, AddCubes);

                    var o2 = hit.offset + hit.dir.Forward();
                    t.AddOffset(o2);
                }
                else if (PaintMode == PaintMode.Remove)
                {
                    Undo.RecordObject(t, RemoveCubes);

                    if (t.offsets.Count > 1)
                    {
                        t.RemoveOffset(hit.offset);
                    }
                }
                else
                {
                    Undo.RecordObject(t, Paint);
                    foreach (var (offset, dir, faceDetails) in t.faceDetails)
                    {
                        foreach (var (p, index) in faceDetails.ToList())
                        {
                            if (HandleUtility.nearestControl == controlId && IsHighlight(hit, t, offset, dir, p))
                            {
                                faceDetails[p] = TesseraTilePaintingState.paintIndex;
                            }
                        }
                    }
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                Repaint(hit, controlId);
            }
            else if (Event.current.type == EventType.Layout)
            {
                if (hit != null)
                {
                    HandleUtility.AddControl(controlId, 3.0f);
                }
            }
            else if (Event.current.type == EventType.MouseDown)
            {
                if (hit != null && HandleUtility.nearestControl == controlId && Event.current.button == 0)
                {
                    DoPaint();
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.MouseMove)
            {
                // TODO: Only if necessary?
                window.Repaint();
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                if (HandleUtility.nearestControl == controlId && GUIUtility.hotControl == controlId)
                {
                    if (PaintMode != PaintMode.Add && PaintMode != PaintMode.Remove)
                    {
                        DoPaint();
                    }
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                if (HandleUtility.nearestControl == controlId && GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.KeyDown)
            {
                CheckKeyPresses();
            }
        }

        internal static void CheckKeyPresses()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Z && Event.current.modifiers == EventModifiers.None)
                {
                    TesseraTilePaintingState.showBackface = !TesseraTilePaintingState.showBackface;
                    Event.current.Use();
                    EditorWindow.GetWindow(typeof(TesseraTilePaintEditorWindow))?.Repaint();
                }
            }
        }
    }


    [EditorTool("Tile Vertex Paint", typeof(TesseraTile))]
    public class PencilPaintTool : TesseraTileEditorToolBase
    {
        private static class Styles
        {
            public static GUIContent toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tessera/Editor/Resources/paint_pencil.png"), "Tile Pencil Paint");
        }

        protected override PaintMode PaintMode => PaintMode.Pencil;
        public override GUIContent toolbarIcon => Styles.toolbarIcon;
    }

    [EditorTool("Tile Edge Paint", typeof(TesseraTile))]
    public class EdgePaintTool : TesseraTileEditorToolBase
    {
        private static class Styles
        {
            public static GUIContent toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tessera/Editor/Resources/paint_edge.png"), "Tile Edge Paint");
        }

        protected override PaintMode PaintMode => PaintMode.Edge;
        public override GUIContent toolbarIcon => Styles.toolbarIcon;
    }

    [EditorTool("Tile Face Paint", typeof(TesseraTile))]
    public class FacePaintTool : TesseraTileEditorToolBase
    {
        private static class Styles
        {
            public static GUIContent toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tessera/Editor/Resources/paint_face.png"), "Tile Face Paint");
        }

        protected override PaintMode PaintMode => PaintMode.Face;
        public override GUIContent toolbarIcon => Styles.toolbarIcon;
    }

    [EditorTool("Tile Vertex Paint", typeof(TesseraTile))]
    public class VertexPaintTool : TesseraTileEditorToolBase
    {
        private static class Styles
        {
            public static GUIContent toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tessera/Editor/Resources/paint_vertex.png"), "Tile Vertex Paint");
        }

        protected override PaintMode PaintMode => PaintMode.Vertex;
        public override GUIContent toolbarIcon => Styles.toolbarIcon;
    }

    [EditorTool("Tile Add Cube", typeof(TesseraTile))]
    public class AddCubeTool : TesseraTileEditorToolBase
    {
        private static class Styles
        {
            public static GUIContent toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tessera/Editor/Resources/cube_add.png"), "Tile Add Cube");
        }

        protected override PaintMode PaintMode => PaintMode.Add;
        public override GUIContent toolbarIcon => Styles.toolbarIcon;
    }

    [EditorTool("Tile Remove Cube", typeof(TesseraTile))]
    public class RemoveCubeTool : TesseraTileEditorToolBase
    {
        private static class Styles
        {
            public static GUIContent toolbarIcon = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tessera/Editor/Resources/cube_remove.png"), "Tile Remove Cube");
        }

        protected override PaintMode PaintMode => PaintMode.Remove;
        public override GUIContent toolbarIcon => Styles.toolbarIcon;
    }
}