using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SVTXPainter;

namespace SVTXPainterEditor
{
    [CustomEditor(typeof(SVTXObject))]
    public class SVTXObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Lauch Vertex Painter"))
                SVTXPainterWindow.LauchVertexPainter();

            EditorGUILayout.Space();
        }
    }
}