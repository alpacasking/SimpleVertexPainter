using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SVTXPainter;

namespace SVTXPainterEditor
{
    public class SVTXPainterWindow : EditorWindow
    {
        #region Variables
        private GUIStyle titleStyle;
        private bool allowPainting = false;
        private bool changingBrushValue = false;
        private bool allowSelect = false;
        private bool isPainting = false;
        private bool isRecord = false;

        private Vector2 mousePos = Vector2.zero;
        private Vector2 lastMousePos = Vector2.zero;
        private RaycastHit curHit;


        private float brushSize = 0.1f;
        private float brushOpacity = 1f;
        private float brushFalloff = 0.1f;

        private Color brushColor;
        private float brushIntensity;

        private const float MinBrushSize = 0.01f;
        public const float MaxBrushSize = 10f;


        private int curColorChannel = (int)PaintType.All;

        private Mesh curMesh;
        private SVTXObject m_target;
        private GameObject m_active;

        #endregion

        #region Main Method
        [MenuItem("Window/Simple Vertex Painter")]
        public static void LauchVertexPainter()
        {
            var window = EditorWindow.GetWindow<SVTXPainterWindow>();
            window.titleContent = new GUIContent("Simple Vertex Painter");
            window.Show();
            window.OnSelectionChange();
            window.GenerateStyles();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
            SceneView.duringSceneGui += this.OnSceneGUI;
            if (titleStyle == null)
            {
                GenerateStyles();
            }
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
        }

        private void OnSelectionChange()
        {
            m_target = null;
            m_active = null;
            curMesh = null;
            if (Selection.activeGameObject != null)
            {
                m_target = Selection.activeGameObject.GetComponent<SVTXObject>();
                curMesh = SVTXPainterUtils.GetMesh(Selection.activeGameObject);

                var activeGameObject = Selection.activeGameObject;
                if (curMesh != null)
                {
                    m_active = activeGameObject;
                }
            }
            allowSelect = (m_target == null);
           
            Repaint();
        }

        #endregion

        #region GUI Methods
        private void OnGUI()
        {
            //Header
            GUILayout.BeginHorizontal();
            GUILayout.Box("Simple Vertex Painter", titleStyle, GUILayout.Height(60), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            //Body
            GUILayout.BeginVertical(GUI.skin.box);

            if (m_target != null)
            {
                if (!m_target.isActiveAndEnabled)
                {
                    EditorGUILayout.LabelField("(Enable " + m_target.name + " to show Simple Vertex Painter)");
                }
                else
                {
                    //bool lastAP = allowPainting;
                    allowPainting = GUILayout.Toggle(allowPainting, "Paint Mode");

                    if (allowPainting)
                    {
                        //Selection.activeGameObject = null;
                        Tools.current = Tool.None;
                    }


                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Paint Type:", GUILayout.Width(90));
                    string[] channelName = { "All", "R", "G", "B", "A" };
                    int[] channelIds = { 0, 1, 2, 3, 4 };
                    curColorChannel = EditorGUILayout.IntPopup(curColorChannel, channelName, channelIds, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (curColorChannel == (int)PaintType.All)
                    {
                        brushColor = EditorGUILayout.ColorField("Brush Color:", brushColor);
                    }
                    else
                    {
                        brushIntensity = EditorGUILayout.Slider("Intensity:", brushIntensity, 0, 1);
                    }
                    if (GUILayout.Button("Fill"))
                    {
                        FillVertexColor();
                    }
                    GUILayout.EndHorizontal();
                    brushSize = EditorGUILayout.Slider("Brush Size:", brushSize, MinBrushSize, MaxBrushSize);
                    brushOpacity = EditorGUILayout.Slider("Brush Opacity:", brushOpacity, 0, 1);
                    brushFalloff = EditorGUILayout.Slider("Brush Falloff:", brushFalloff, MinBrushSize, brushSize);

                    if (GUILayout.Button("Export .asset file") && curMesh != null)
                    {
                        string path = EditorUtility.SaveFilePanel("Export .asset file", "Assets", SVTXPainterUtils.SanitizeForFileName(curMesh.name), "asset");
                        if (path.Length > 0)
                        {
                            var dataPath = Application.dataPath;
                            if (!path.StartsWith(dataPath))
                            {
                                Debug.LogError("Invalid path: Path must be under " + dataPath);
                            }
                            else
                            {
                                path = path.Replace(dataPath, "Assets");
                                AssetDatabase.CreateAsset(Instantiate(curMesh), path);
                                Debug.Log("Asset exported: " + path);
                            }
                        }
                    }




                    //Footer
                    GUILayout.Label("Key Z:Turn on or off\nRight mouse button:Paint\nRight mouse button+Shift:Opacity\nRight mouse button+Ctrl:Size\nRight mouse button+Shift+Ctrl:Falloff\n", EditorStyles.helpBox);
                    Repaint();
                }
            }
            else if (m_active != null)
            {
                if (GUILayout.Button("Add SVTX Object to " + m_active.name))
                {
                    m_active.AddComponent<SVTXObject>();
                    OnSelectionChange();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Please select a mesh or skinnedmesh.");
            }
            GUILayout.EndVertical();
        }
        void OnSceneGUI(SceneView sceneView)
        {
            if (allowPainting )
            {
                bool isHit = false;
                if (!allowSelect)
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
                Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePos);
                if (m_target != null && curMesh != null)
                {
                    Matrix4x4 mtx = m_target.transform.localToWorldMatrix;
                    RaycastHit tempHit;
                    isHit = RXLookingGlass.IntersectRayMesh(worldRay, curMesh, mtx, out tempHit);
                    if (isHit)
                    {
                        if (!changingBrushValue)
                        {
                            curHit = tempHit;
                        }
                        //Debug.Log("ray cast success");
                        if (isPainting && m_target.isActiveAndEnabled && !changingBrushValue)
                        {
                            PaintVertexColor();
                        }
                    }
                }

                if (isHit|| changingBrushValue)
                {

                    Handles.color = getSolidDiscColor((PaintType)curColorChannel);
                    Handles.DrawSolidDisc(curHit.point, curHit.normal, brushSize);
                    Handles.color = getWireDiscColor((PaintType)curColorChannel);
                    Handles.DrawWireDisc(curHit.point, curHit.normal, brushSize);
                    Handles.DrawWireDisc(curHit.point, curHit.normal, brushFalloff);
                }
            }


            ProcessInputs();

            sceneView.Repaint();

        }

        private void OnInspectorUpdate()
        {
            OnSelectionChange();
        }
        #endregion

        #region TempPainter Method
        void PaintVertexColor()
        {
            if (m_target&&m_active)
            {
                curMesh = SVTXPainterUtils.GetMesh(m_active);
                if (curMesh)
                {
                    if (isRecord)
                    {
                        m_target.PushUndo();
                        isRecord = false;
                    }
                    Vector3[] verts = curMesh.vertices;
                    Color[] colors = new Color[0];
                    if (curMesh.colors.Length > 0)
                    {
                        colors = curMesh.colors;
                    }
                    else
                    {
                        colors = new Color[verts.Length];
                    }
                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 vertPos = m_target.transform.TransformPoint(verts[i]);
                        float mag = (vertPos - curHit.point).magnitude;
                        if (mag > brushSize)
                        {
                            continue;
                        }
                        float falloff = SVTXPainterUtils.LinearFalloff(mag, brushSize);
                        falloff = Mathf.Pow(falloff, Mathf.Clamp01(1 - brushFalloff / brushSize)) * brushOpacity;
                        if (curColorChannel == (int)PaintType.All)
                        {
                            colors[i] = SVTXPainterUtils.VTXColorLerp(colors[i], brushColor, falloff);
                        }
                        else
                        {
                            colors[i] = SVTXPainterUtils.VTXOneChannelLerp(colors[i], brushIntensity, falloff, (PaintType)curColorChannel);
                        }
                        //Debug.Log("Blend");
                    }
                    curMesh.colors = colors;
                }
                else
                {
                    OnSelectionChange();
                    Debug.LogWarning("Nothing to paint!");
                }
               
            }
            else
            {
                OnSelectionChange();
                Debug.LogWarning("Nothing to paint!");
            }
        }

        void FillVertexColor()
        {
            if (curMesh)
            {
                Vector3[] verts = curMesh.vertices;
                Color[] colors = new Color[0];
                if (curMesh.colors.Length > 0)
                {
                    colors = curMesh.colors;
                }
                else
                {
                    colors = new Color[verts.Length];
                }
                for (int i = 0; i < verts.Length; i++)
                {
                    if (curColorChannel == (int)PaintType.All)
                    {
                        colors[i] = brushColor;
                    }
                    else
                    {
                        colors[i] = SVTXPainterUtils.VTXOneChannelLerp(colors[i], brushIntensity, 1, (PaintType)curColorChannel);
                    }
                    //Debug.Log("Blend");
                }
                curMesh.colors = colors;
            }
            else
            {
                Debug.LogWarning("Nothing to fill!");
            }
        }
        #endregion

        #region Utility Methods
        void ProcessInputs()
        {
            if (m_target == null)
            {
                return;
            }
            Event e = Event.current;
            mousePos = e.mousePosition;
            if (e.type == EventType.KeyDown)
            {
                if (e.isKey)
                {
                    if (e.keyCode == KeyCode.Z)
                    {
                        allowPainting = !allowPainting;
                        if (allowPainting)
                        {
                            Tools.current = Tool.None;
                        }
                    }
                }
            }
            if (e.type == EventType.MouseUp)
            {
                changingBrushValue = false;
                isPainting = false;

            }
            if (lastMousePos == mousePos)
            {
                isPainting = false;
            }
            if (allowPainting)
            {
                if (e.type == EventType.MouseDrag && e.control && e.button == 0 && !e.shift)
                {
                    brushSize += e.delta.x * 0.005f;
                    brushSize = Mathf.Clamp(brushSize, MinBrushSize, MaxBrushSize);
                    brushFalloff = Mathf.Clamp(brushFalloff, MinBrushSize, brushSize);
                    changingBrushValue = true;
                }
                if (e.type == EventType.MouseDrag && !e.control && e.button == 0 && e.shift)
                {
                    brushOpacity += e.delta.x * 0.005f;
                    brushOpacity = Mathf.Clamp01(brushOpacity);
                    changingBrushValue = true;
                }
                if (e.type == EventType.MouseDrag && e.control && e.button == 0 && e.shift)
                {
                    brushFalloff += e.delta.x * 0.005f;
                    brushFalloff = Mathf.Clamp(brushFalloff, MinBrushSize, brushSize);
                    changingBrushValue = true;
                }
                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.control && e.button == 0 && !e.shift && !e.alt)
                {
                    isPainting = true;
                    if (e.type == EventType.MouseDown)
                    {
                        isRecord = true;
                    }
                }
            }
            lastMousePos = mousePos;
        }
        void GenerateStyles()
        {
            titleStyle = new GUIStyle();
            titleStyle.border = new RectOffset(3, 3, 3, 3);
            titleStyle.margin = new RectOffset(2, 2, 2, 2);
            titleStyle.fontSize = 25;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
        }

        Color getSolidDiscColor(PaintType pt)
        {
            switch (pt)
            {
                case PaintType.All:
                    return new Color(brushColor.r, brushColor.g, brushColor.b, brushOpacity);
                case PaintType.R:
                    return new Color(brushIntensity, 0, 0, brushOpacity);
                case PaintType.G:
                    return new Color(0, brushIntensity, 0, brushOpacity);
                case PaintType.B:
                    return new Color(0, 0, brushIntensity, brushOpacity);
                case PaintType.A:
                    return new Color(brushIntensity, 0, brushIntensity, brushOpacity);

            }
            return Color.white;
        }
        Color getWireDiscColor(PaintType pt)
        {
            switch (pt)
            {
                case PaintType.All:
                    return new Color(1 - brushColor.r, 1 - brushColor.g, 1 - brushColor.b, 1);
                case PaintType.R:
                    return Color.white;
                case PaintType.G:
                    return Color.white;
                case PaintType.B:
                    return Color.white;
                case PaintType.A:
                    return Color.white;
            }
            return Color.white;
        }
        #endregion

    }

}