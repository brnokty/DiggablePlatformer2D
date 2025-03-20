using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BoxShape2D))]
    class BoxShape2DEditor : Shape2DEditor
    {
        static class GUIContents
        {
            public static readonly GUIContent fill = new GUIContent("Fill", "Set 'true' to fill areas, set 'false' to remove areas.");
            public static readonly GUIContent width = new GUIContent("Width", "The width of the box.");
            public static readonly GUIContent height = new GUIContent("Height", "The height of the box");
            public static readonly GUIContent cornerRadius = new GUIContent("Corner Radius", "The radius used to round the corners.");
            public static readonly GUIContent cornerPointCount = new GUIContent("Corner Point Count", "The number of additional points that are added to round the corners.");
        }

        SerializedProperty m_FillProp;
        SerializedProperty m_WidthProp;
        SerializedProperty m_HeightProp;
        SerializedProperty m_CornerPointCountProp;
        SerializedProperty m_CornerRadiusProp;


        protected override void DrawInspector()
        {
            EditorGUILayout.PropertyField(m_FillProp, GUIContents.fill);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_WidthProp, GUIContents.width);
            if (EditorGUI.EndChangeCheck())
            {
                m_WidthProp.floatValue = Mathf.Max(m_WidthProp.floatValue, BoxShape2D.MinSize);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_HeightProp, GUIContents.height);
            if (EditorGUI.EndChangeCheck())
            {
                m_HeightProp.floatValue = Mathf.Max(m_HeightProp.floatValue, BoxShape2D.MinSize);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CornerRadiusProp, GUIContents.cornerRadius);
            if (EditorGUI.EndChangeCheck())
            {

                m_CornerRadiusProp.floatValue = Mathf.Max(m_CornerRadiusProp.floatValue, BoxShape2D.MinSize);
            }

            EditorGUILayout.IntSlider(m_CornerPointCountProp, BoxShape2D.MinCornerPointCount, BoxShape2D.MaxCornerPointCount, GUIContents.cornerPointCount);
        }

        protected override string GetHelpInfo()
        {
            return "To snap a handle, hold the <b>Ctrl</b> button.";
        }

        protected override void DrawHandles()
        {
            BoxShape2D path = target as BoxShape2D;
            Transform transform = path.transform;
            Matrix4x4 matrix = transform.localToWorldMatrix;
            Vector2 size = path.size;
            Rect rectOld = new Rect(-size / 2, size);

            EditorGUI.BeginChangeCheck();
            Rect rect = DoRectHandle(rectOld, matrix);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Edit Box Path");

                Vector3 offset = rect.center - rectOld.center;
                transform.position += matrix.MultiplyVector(offset);
                path.size = rect.size;
            }
        }

        Rect DoRectHandle(Rect rect, Matrix4x4 local2World)
        {
            Vector2 offset = rect.min;
            Vector2 scale = rect.size;

            Vector2 downL = offset;
            Vector2 downR = new Vector2(offset.x + scale.x, offset.y);
            Vector2 upR = new Vector2(offset.x + scale.x, offset.y + scale.y);
            Vector2 upL = new Vector2(offset.x, offset.y + scale.y);
            Vector2 center = new Vector2(offset.x + scale.x / 2, offset.y + scale.y / 2);

            downL = local2World.MultiplyPoint(downL);
            downR = local2World.MultiplyPoint(downR);
            upR = local2World.MultiplyPoint(upR);
            upL = local2World.MultiplyPoint(upL);
            center = local2World.MultiplyPoint(center);

            Matrix4x4 world2Local = local2World.inverse;



            using (new Handles.DrawingScope(PathHandleUtility.lineColor))
            {
                Handles.DrawAAPolyLine(PathHandleUtility.lineWidth, downL, downR, upR, upL, downL);
            }
            bool isL = false;
            bool isR = false;


            using (new Handles.DrawingScope(PathHandleUtility.handleColor))
            {
                if ((isL = DoPositionHandle(ref downL)) || (isR = DoPositionHandle(ref upR)))
                {
                    downL = world2Local.MultiplyPoint(downL);
                    upR = world2Local.MultiplyPoint(upR);

                    if (isL)
                    {
                        float dx = upR.x - downL.x;
                        float dy = upR.y - downL.y;
                        dx = Mathf.Clamp(dx, BoxShape2D.MinSize, float.PositiveInfinity);
                        dy = Mathf.Clamp(dy, BoxShape2D.MinSize, float.PositiveInfinity);
                        downL.x = upR.x - dx;
                        downL.y = upR.y - dy;
                    }

                    if (isR)
                    {
                        float dx = upR.x - downL.x;
                        float dy = upR.y - downL.y;
                        dx = Mathf.Clamp(dx, BoxShape2D.MinSize, float.PositiveInfinity);
                        dy = Mathf.Clamp(dy, BoxShape2D.MinSize, float.PositiveInfinity);
                        upR.x = downL.x + dx;
                        upR.y = downL.y + dy;
                    }

                    rect.position = downL;
                    rect.size = upR - downL;
                }

                if ((isR = DoPositionHandle(ref downR)) || (isL = DoPositionHandle(ref upL)))
                {
                    downR = world2Local.MultiplyPoint(downR);
                    upL = world2Local.MultiplyPoint(upL);

                    if (isL)
                    {
                        float dx = downR.x - upL.x;
                        float dy = upL.y - downR.y;
                        dx = Mathf.Clamp(dx, BoxShape2D.MinSize, float.PositiveInfinity);
                        dy = Mathf.Clamp(dy, BoxShape2D.MinSize, float.PositiveInfinity);
                        upL.x = downR.x - dx;
                        upL.y = downR.y + dy;
                    }

                    if (isR)
                    {
                        float dx = downR.x - upL.x;
                        float dy = upL.y - downR.y;
                        dx = Mathf.Clamp(dx, BoxShape2D.MinSize, float.PositiveInfinity);
                        dy = Mathf.Clamp(dy, BoxShape2D.MinSize, float.PositiveInfinity);
                        downR.x = upL.x + dx;
                        downR.y = upL.y - dy;
                    }


                    center = (downR + upL) / 2;
                    downL = new Vector2(upL.x, downR.y);
                    rect.position = downL;
                    rect.size = (center - downL) * 2;
                }

                return rect;
            }
        }

        bool DoPositionHandle(ref Vector2 po)
        {
            EditorGUI.BeginChangeCheck();
            float handleSize = PathHandleUtility.GetHandleSize(po);
            var fmh_180_45_638780058722656417 = Quaternion.identity; po = Handles.FreeMoveHandle(po, handleSize, Vector3.zero, Handles.SphereHandleCap);
            if (Event.current.control) po = EditorGridUtility.SnapToGrid2D(po);
            return EditorGUI.EndChangeCheck();
        }
    }
}