using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace SVTXPainter
{
    [ExecuteInEditMode]
    public class SVTXObject : MonoBehaviour
    {
#if UNITY_EDITOR
        [Serializable]
        public class Record
        {
            // for undo
            public int index = 0;
            public Color[] colors;
        }

        [SerializeField] Record m_record = new Record();
        int m_historyIndex = 0;

        public void PushUndo()
        {

            var mesh = SVTXPainterUtils.GetMesh(gameObject);
            if(mesh != null)
            {
                m_record.index = m_historyIndex;
                m_historyIndex++;
                var colors = mesh.colors;
                m_record.colors = new Color[colors.Length];
                Array.Copy(colors, m_record.colors, colors.Length);
                Undo.RegisterCompleteObjectUndo(this, "Simple Vertex Painter [" + m_record.index + "]");
                //Debug.Log("Change Vertex Color");
            }
        }

        public void OnUndoRedo()
        {
            var mesh = SVTXPainterUtils.GetMesh(gameObject);
            if (mesh == null) {
                return;
            }
            if (m_historyIndex != m_record.index)
            {
                m_historyIndex = m_record.index;
                if (m_record.colors != null && mesh.colors != null && m_record.colors.Length == mesh.colors.Length)
                {
                    mesh.colors = m_record.colors;
                    //Debug.Log("UndoRedo");
                }
            }
        }

        private void OnEnable()
        {
            UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void OnDisable()
        {
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif
    }
}
