using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CustomEditor(typeof(PolygonTerrain2D))]
    class PolygonTerrain2DEditor : Terrain2DEditor
    {
        static class GUIContents
        {
            public static GUIContent useDelaunay = new GUIContent("Use Delaunay", "");
            public static GUIContent enableHoles = new GUIContent("Enable Holes", "Does the terrain support holes?");
            public static GUIContent enablePhysics = new GUIContent("Enable Physics");
            public static GUIContent anchors = new GUIContent("Anchors");
        }

        SerializedProperty m_UseDelaunayProp;
        SerializedProperty m_EnableHolesProp;
        SerializedProperty m_EnablePhysicsProp;
        SerializedProperty m_AnchorsProp;

        AnchorsList m_AnchorsList;
        Tool m_AnchorsTool;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_AnchorsList = new AnchorsList(m_AnchorsProp);
            m_AnchorsTool = new Tool(DrawAnchorsEditor);
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            EditorGUILayout.PropertyField(m_UseDelaunayProp, GUIContents.useDelaunay);
            EditorGUILayout.PropertyField(m_EnableHolesProp, GUIContents.enableHoles);
            EditorGUILayout.PropertyField(m_EnablePhysicsProp, GUIContents.enablePhysics);
        }

        protected override void OnFooterGUI()
        {
            DrawAnchorsGUI();
        }

        void DrawAnchorsGUI()
        {
            if (m_EnablePhysicsProp.boolValue)
            {
                bool foldout = m_AnchorsProp.isExpanded;
                foldout = EditorGUILayout.Foldout(foldout, GUIContents.anchors);
                m_AnchorsProp.isExpanded = foldout;
                if (foldout)
                {
                    EditorGUI.indentLevel++;
                    DrawToolButton(m_AnchorsTool, "Edit Mode");

                    string msg =
                        "To snap a handle, hold the <b>Ctrl</b> button.\n\n" +
                        "To duplicate a handle, <b>right-click</b> on it and then choose <b>Duplicate</b> from the menu.\n\n" +
                        "To delete a handle, <b>right-click</b> on it and then choose <b>Delete</b> from the menu.\n\n";


                    if (m_AnchorsTool == m_ActiveTool)
                        HelpBox.Draw(msg, 745);

                    m_AnchorsList.DoLayoutList();
                    EditorGUI.indentLevel--;
                }
            }
        }

        void DrawAnchorsEditor()
        {
            bool rightClick = Event.current.type == EventType.MouseDown && Event.current.button == 1;
            int n = m_AnchorsProp.arraySize;
            Matrix4x4 matrix = m_Terrain.transform.localToWorldMatrix;
            for (int i = 0; i < n; i++)
            {
                var anchorProp = m_AnchorsProp.GetArrayElementAtIndex(i);
                Vector3 anchor = anchorProp.vector2Value;
                anchor = matrix.MultiplyPoint(anchor);
                float handleSize = HandleUtility.GetHandleSize(anchor) * 0.14f;
                Quaternion q = Quaternion.identity;
                EditorGUI.BeginChangeCheck();
                anchor = Handles.FreeMoveHandle(anchor, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    anchor = matrix.inverse.MultiplyPoint(anchor);
                    anchorProp.vector2Value = anchor;
                    serializedObject.ApplyModifiedProperties();
                }

                if (GUIUtility.hotControl == 0 && rightClick && HandleUtility.DistanceToCircle(anchor, handleSize) < 10)
                {
                    GenericMenu menu = new GenericMenu();


                    menu.AddItem(new GUIContent("Duplicate"), false, () =>
                    {
                        m_AnchorsProp.InsertArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                    });

                    menu.AddItem(new GUIContent("Delete"), false, () =>
                    {
                        m_AnchorsProp.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                    });

                    menu.ShowAsContext();
                    Event.current.Use();
                    return;
                }
            }
        }

        class AnchorsList : ReorderableList
        {
            public AnchorsList(SerializedProperty elements) : base(elements.serializedObject, elements, true, false, true, true)
            {
                elementHeightCallback = ElementHeightCallback;
                drawElementCallback = DrawElementCallback;
            }

            float ElementHeightCallback(int index)
            {
                SerializedProperty prop = serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(prop, true);
            }

            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                SerializedProperty prop = serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(rect, prop);
                EditorGUI.indentLevel--;
            }
        }
    }
}