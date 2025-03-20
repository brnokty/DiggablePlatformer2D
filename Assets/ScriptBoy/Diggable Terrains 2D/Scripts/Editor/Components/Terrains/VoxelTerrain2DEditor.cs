using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CustomEditor(typeof(VoxelTerrain2D))]
    class VoxelTerrain2DEditor : Terrain2DEditor
    {
        Tool m_MapRectTool;

        SerializedProperty m_MapWidthProp;
        SerializedProperty m_MapHeightProp;
        SerializedProperty m_MapTransformProp;
        SerializedProperty m_MapPaddingProp;
        SerializedProperty m_MapPositionProp;
        SerializedProperty m_MapScaleProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_MapRectTool = new Tool(EditMapRect);
        }

        protected override void OnFooterGUI()
        {
            bool foldout = m_MapScaleProp.isExpanded;
            foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Voxel Map"));
            m_MapScaleProp.isExpanded = foldout;

            if (foldout)
            {
                bool autoTransform = m_MapTransformProp.enumValueIndex == 0;

                if (!autoTransform)
                {
                    DrawToolButton(m_MapRectTool, "Transform");
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MapWidthProp, new GUIContent("Width"));
                EditorGUILayout.PropertyField(m_MapHeightProp, new GUIContent("Height"));
                EditorGUILayout.PropertyField(m_MapTransformProp, new GUIContent("Transform"));

                if (autoTransform)
                {
                    EditorGUILayout.PropertyField(m_MapPaddingProp, new GUIContent("Padding"));
                }
                else
                {
                    EditorGUILayout.PropertyField(m_MapPositionProp, new GUIContent("Position"));
                    EditorGUILayout.PropertyField(m_MapScaleProp, new GUIContent("Scale"));
                }

                EditorGUI.indentLevel--;
            }
        }

        protected override void OnSceneGUI(SceneView scene)
        {
            base.OnSceneGUI(scene);
            //DrawMapBounds();
        }

        void EditMapRect()
        {
            if (m_MapTransformProp.enumValueIndex == 0)
            {
                m_ActiveTool = null;
                return;
            }

            int w = m_MapWidthProp.enumValueFlag;
            int h = m_MapHeightProp.enumValueFlag;
            float scale = m_MapScaleProp.floatValue;
            float raitoX = w > h ? 1 : (float)h / w;
            float raitoY = w < h ? 1 : (float)w / h;
            Vector2 position = m_MapPositionProp.vector2Value;
            Vector2 size = new Vector2(scale, scale);
            //DrawBounds(position, size, Color.white.Fade(0.2f));

            size = new Vector2(scale / raitoX, scale / raitoY);

            if (DoRectHandale(ref position, ref size))
            {
                m_MapPositionProp.vector2Value = position;
                m_MapScaleProp.floatValue = Mathf.Max(size.x * raitoX, size.y * raitoY);
            }
        }

        private void DrawMapBounds()
        {
            int w = m_MapWidthProp.enumValueFlag;
            int h = m_MapHeightProp.enumValueFlag;
            float scale = m_MapScaleProp.floatValue;
            float raitoX = w > h ? 1 : (float)h / w;
            float raitoY = w < h ? 1 : (float)w / h;
            Vector2 position = m_MapPositionProp.vector2Value;
            Vector2 size = new Vector2(scale / raitoX, scale / raitoY);

            DrawBounds(m_Terrain.transform, position, size);
        }

        void DrawBounds(Transform transform, Vector2 min, Vector2 size)
        {
            Vector3 a = new Vector3(min.x, min.y);
            Vector3 b = new Vector3(min.x + size.x, min.y);
            Vector3 c = new Vector3(min.x, min.y + size.y);
            Vector3 d = new Vector3(min.x + size.x, min.y + size.y);

            a = transform.TransformPoint(a);
            b = transform.TransformPoint(b);
            c = transform.TransformPoint(c);
            d = transform.TransformPoint(d);

            Handles.DrawAAPolyLine(2, a, b, d, c, a);
        }

        void DrawBounds(Vector2 min, Vector2 size, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                Vector3 a = new Vector3(min.x, min.y);
                Vector3 b = new Vector3(min.x + size.x, min.y);
                Vector3 c = new Vector3(min.x, min.y + size.y);
                Vector3 d = new Vector3(min.x + size.x, min.y + size.y);
                Handles.DrawPolyLine(a, b, d, c, a);
            }
        }
    }
}