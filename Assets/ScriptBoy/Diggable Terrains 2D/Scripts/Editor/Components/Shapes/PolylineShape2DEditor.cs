using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PolylineShape2D))]
    class PolylineShape2DEditor : Shape2DEditor
    {
        static class GUIContents
        {
            public static readonly GUIContent fill = new GUIContent("Fill", "Set 'true' to fill areas, set 'false' to remove areas.");
            public static readonly GUIContent loop = new GUIContent("Loop", "Connects the first and last control points to create a closed shape.");
            public static readonly GUIContent thickness = new GUIContent("Thickness", "The thickness of the polyline.");
            public static readonly GUIContent capPointCount = new GUIContent("Cap Point Count", "The number of points that are added to round the start and end of the polyline.");
            public static readonly GUIContent cornerPointCount = new GUIContent("Corner Point Count", "The number of additional points that are added to round the corners.");
            public static readonly GUIContent cornerRadius = new GUIContent("Corner Radius", "The radius used to round the corners.");
            public static readonly GUIContent controlPoints = new GUIContent("Control Points", "The main points of the polyline.");
        }

        SerializedProperty m_FillProp;
        SerializedProperty m_LoopProp;
        SerializedProperty m_ThicknessProp;
        SerializedProperty m_CapPointCountProp;
        SerializedProperty m_CornerPointCountProp;
        SerializedProperty m_CornerRadiusProp;
        SerializedProperty m_ControlPointsProp;

        protected override void DrawInspector()
        {
            EditorGUILayout.PropertyField(m_FillProp, GUIContents.fill);
            EditorGUILayout.PropertyField(m_LoopProp, GUIContents.loop);
            if (!m_LoopProp.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ThicknessProp, GUIContents.thickness);
                if (EditorGUI.EndChangeCheck())
                {
                    m_ThicknessProp.floatValue = Mathf.Max(m_ThicknessProp.floatValue, PolylineShape2D.MinThickness);
                }

                EditorGUILayout.IntSlider(m_CapPointCountProp, PolylineShape2D.MinCapPointCount, PolylineShape2D.MaxCapPointCount, GUIContents.capPointCount);
            }
            EditorGUILayout.IntSlider(m_CornerPointCountProp, PolylineShape2D.MinCornerPointCount, PolylineShape2D.MaxCornerPointCount, GUIContents.cornerPointCount);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CornerRadiusProp, GUIContents.cornerRadius);
            if (EditorGUI.EndChangeCheck())
            {
                m_CornerRadiusProp.floatValue = Mathf.Max(m_CornerRadiusProp.floatValue, PolylineShape2D.MinCornerRadius);
            }

            EditorGUILayout.PropertyField(m_ControlPointsProp, GUIContents.controlPoints); 
        }

        protected override string GetHelpInfo()
        {
            return "To snap a handle, hold the <b>Ctrl</b> button.\n\n" +
                "To delete a handle, <b>right-click</b> on it and then choose <b>Delete</b> from the menu that appears.\n\n" +
                "To add a handle, move your mouse over a line segment and then drag the new transparent handle that appears.";
        }

        protected override void DrawHandles()
        {
            PolylineShape2D path = target as PolylineShape2D;
            Vector2[] controlPoints = path.worldControlPoints;
            bool loop = path.loop;
            Event EVENT = Event.current;
            bool rightClick = EVENT.type == EventType.MouseDown && EVENT.button == 1;
            bool hoverHandle = false;
            int n = controlPoints.Length;

            Vector3[] points = new Vector3[n + (loop ? 1 : 0)];
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = controlPoints[i];
                if (HandleUtility.DistanceToCircle(pos, PathHandleUtility.GetHandleSize(pos)) < 5)
                {
                    hoverHandle = true;

                    if (rightClick)
                    {
                        GenericMenu menu = new GenericMenu();
                        int j = i;
                        menu.AddItem(new GUIContent("Delete"), false, () =>
                        {
                            Undo.RecordObject(path, "Delete Control Point");
                            path.RemoveControlPoint(j);
                            ShapeTracker.RecordChange(path);
                        });
                        menu.ShowAsContext();
                        EVENT.Use();
                    }
                }
                points[i] = pos;
            }
            if(loop) points[n] = points[0];

            using (new Handles.DrawingScope(PathHandleUtility.lineColor))
            {
                Handles.DrawAAPolyLine(PathHandleUtility.lineWidth, points);
            }

            using (new Handles.DrawingScope(PathHandleUtility.handleColor))
            {
                int addId = GUIUtility.GetControlID(FocusType.Passive);

                for (int i = 0; i < n; i++)
                {
                    int id = GUIUtility.GetControlID(FocusType.Passive);
                    Vector3 pos = points[i];
                    float size = PathHandleUtility.GetHandleSize(pos);

                    EditorGUI.BeginChangeCheck();
                    var fmh_116_59_638780058722968612 = Quaternion.identity; pos = Handles.FreeMoveHandle(id, pos, size, Vector3.zero, Handles.SphereHandleCap);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(path, "Edit Polyline Path");
                        if (EVENT.control) pos = EditorGridUtility.SnapToGrid2D(pos);
                        path.SetWorldControlPoint(pos, i);
                        return;
                    }

                    if (!hoverHandle && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == addId) && (path.loop || i != 0))
                    {
                        int prevIndex = LoopUtility.PrevIndex(i, n);
                        Vector3 prevPos = points[prevIndex];

                        if (HandleUtility.DistanceToLine(prevPos, pos) < 5)
                        {
                            Vector3 addPos = HandleUtility.ClosestPointToPolyLine(new Vector3[] { pos, prevPos });

                            EditorGUI.BeginChangeCheck();
                            using (new Handles.DrawingScope(PathHandleUtility. handleColor.Fade(0.1f)))
                            {
                                var fmh_137_80_638780058722971503 = Quaternion.identity; addPos = Handles.FreeMoveHandle(addId, addPos, size, Vector3.zero, Handles.SphereHandleCap);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(path, "Edit Polyline Path");
                                if (EVENT.control) addPos = EditorGridUtility.SnapToGrid2D(addPos);
                                path.InsertWorldControlPoint(addPos, i);
                                GUIUtility.hotControl = id;
                                GUI.changed = true;
                            }
                        }
                    }
                }
            }
        }
    }
}