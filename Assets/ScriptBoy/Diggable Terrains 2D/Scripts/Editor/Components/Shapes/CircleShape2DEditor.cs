using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CircleShape2D))]
    class CircleShape2DEditor : Shape2DEditor
    {
        static class GUIContents
        {
            public static readonly GUIContent fill = new GUIContent("Fill", "Set 'true' to fill areas, set 'false' to remove areas.");
            public static readonly GUIContent radius = new GUIContent("Radius", "The radius of the circle.");
            public static readonly GUIContent pointCount = new GUIContent("Point Count", "The number of points that form the cicrle.");
        }

        SerializedProperty m_FillProp;
        SerializedProperty m_RadiusProp;
        SerializedProperty m_PointCountProp;


        protected override void DrawInspector()
        {
            EditorGUILayout.PropertyField(m_FillProp, GUIContents.fill);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_RadiusProp, GUIContents.radius);
            if (EditorGUI.EndChangeCheck())
            {
                m_RadiusProp.floatValue = Mathf.Max(m_RadiusProp.floatValue, CircleShape2D.MinRadius);
            }

            EditorGUILayout.IntSlider(m_PointCountProp, CircleShape2D.MinPointCount, CircleShape2D.MaxPointCount, GUIContents.pointCount);
        }

        protected override string GetHelpInfo()
        {
            return "To snap a handle, hold the <b>Ctrl</b> button.";
        }

        protected override void DrawHandles()
        {
            CircleShape2D path = target as CircleShape2D;
            Matrix4x4 matrix = path.transform.localToWorldMatrix;
            Vector3 center = matrix.MultiplyPoint(Vector3.zero);
            Vector3 position = matrix.MultiplyPoint(Vector3.left * path.radius);

            using (new Handles.DrawingScope(PathHandleUtility.lineColor))
            {
                Vector2[] circle = PolygonUtility.CreateCircle(center, 40, (position - center).magnitude);
                int n = circle.Length;
                Vector3[] circle3 = new Vector3[n + 1];
                for (int i = 0; i < n; i++)
                {
                    circle3[i] = circle[i];
                }
                circle3[n] = circle[0];
                Handles.DrawAAPolyLine(PathHandleUtility.lineWidth, circle3);
            }

            using (new Handles.DrawingScope(PathHandleUtility.handleColor))
            {
                EditorGUI.BeginChangeCheck();
                float handleSize = PathHandleUtility.GetHandleSize(position);
                var fmh_65_61_638780058722344251 = Quaternion.identity; position = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path, "CirclePathEditor");
                    position = matrix.inverse.MultiplyPoint(position);
                    path.radius = position.magnitude;
                }
            }
        }
    }
}