using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SplineShape2D))]
    class SplineShape2DEditor : Shape2DEditor
    {
        static class GUIContents
        {
            public static readonly GUIContent fill = new GUIContent("Fill", "Set 'true' to fill areas, set 'false' to remove areas.");
            public static readonly GUIContent loop = new GUIContent("Loop", "Connects the starting and ending points to form a closed shape.");
            public static readonly GUIContent thickness = new GUIContent("Thickness", "The thickness of the spline.");
            public static readonly GUIContent capPointCount = new GUIContent("Cap Point Count", "The number of points that are added to round the start and end of the spline.");
            public static readonly GUIContent midPointCount = new GUIContent("Mid Point Count", "The number of points between two control points.");
            public static readonly GUIContent controlPoints = new GUIContent("Control Points", "The control points of the spline.");
        }

        SerializedProperty m_FillProp;
        SerializedProperty m_LoopProp;
        SerializedProperty m_ThicknessProp;
        SerializedProperty m_CapPointCountProp;
        SerializedProperty m_MidPointCountProp;
        SerializedProperty m_ControlPointsProp;

        static Vector3[] s_Polyline = new Vector3[20];


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

                EditorGUILayout.IntSlider(m_CapPointCountProp, SplineShape2D.MinCapPointCount, SplineShape2D.MaxCapPointCount, GUIContents.capPointCount);

            }
            EditorGUILayout.IntSlider(m_MidPointCountProp, SplineShape2D.MinMidPointCount, SplineShape2D.MaxMidPointCount, GUIContents.midPointCount);

            serializedObject.forceChildVisibility = true;
            EditorGUILayout.PropertyField(m_ControlPointsProp, GUIContents.controlPoints);
            serializedObject.forceChildVisibility = false;
        }

        protected override string GetHelpInfo()
        {
            
            return
                "A <b>Control</b> handle is represented by a <b>Sphere</b>, serving as the position of a control point.\n\n" +
                "A <b>Tangent</b> handle is represented by a <b>Cone</b>, serving as the in or out tangent of a control point.\n\n" +
                "To snap a handle, hold the <b>Ctrl</b> button.\n\n" +
                "To delete a <b>Control</b> handle, <b>right-click</b> on it and then choose <b>Delete</b> from the menu that appears.\n\n" +
                "To add a <b>Control</b> handle, move your mouse over a curve segment and then drag the new transparent handle that appears.\n\n" +
                "To rest tangents, hold the <b>Alt</b> button and then drag the <b>Control</b> handle.\n\n" +
                "To break tangents connection, hold the <b>Alt</b> button then drag the <b>Tangent</b> handle.";
        }

        protected override void DrawHandles()
        {
            SplineShape2D path = target as SplineShape2D;
            SplineControlPoint[] controlPoints = path.worldControlPoints;

            DrawCurves(path, controlPoints);
            DrawHandles(path, controlPoints); 
        }


        void DrawCurves(SplineShape2D path, SplineControlPoint[] controlPoints)
        {
            int n = controlPoints.Length;
            for (int i = path.loop ? 0 : 1; i < n; i++)
            {
                SplineControlPoint startControlPoint = controlPoints[LoopUtility.PrevIndex(i, n)];
                SplineControlPoint endControlPoint = controlPoints[i];

                Vector3 start = startControlPoint.position;
                Vector3 end = endControlPoint.position;
                Vector3 startTangent = startControlPoint.outTangent;
                Vector3 endTangent = endControlPoint.inTangent;

                startTangent += start;
                endTangent += end;

                Handles.DrawBezier(start, end, startTangent, endTangent, PathHandleUtility.lineColor, null, PathHandleUtility.lineWidth);
            }
        }

        void DrawHandles(SplineShape2D path, SplineControlPoint[] controlPoints)
        {
            int controlPointCount = controlPoints.Length;
            int handleHash = "GSDSAFSDDF".GetHashCode();


            bool hoverHandle = false;
            for (int i = 0; i < controlPointCount; i++)
            {
                SplineControlPoint controlPoint = controlPoints[i];
                Vector3 position = controlPoint.position;
                Vector3 inTangent = controlPoint.inTangent;
                Vector3 outTangent = controlPoint.outTangent;
                inTangent += position;
                outTangent += position;

                if (HandleUtility.DistanceToCircle(position, PathHandleUtility.GetHandleSize(position)) < 10)
                {
                    hoverHandle = true;
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                    {
                        new ControlPointMenu(path, i).Open();
                        Event.current.Use();
                    }
                }

                if (HandleUtility.DistanceToCircle(inTangent, PathHandleUtility.GetHandleSize(inTangent)) < 10)
                {
                    hoverHandle = true;
                }

                if (HandleUtility.DistanceToCircle(outTangent, PathHandleUtility.GetHandleSize(outTangent)) < 10)
                {
                    hoverHandle = true;
                }
            }



            int addHandleID = GUIUtility.GetControlID(handleHash, FocusType.Passive);

            for (int i = 0; i < controlPointCount; i++)
            {
                SplineControlPoint controlPoint = controlPoints[i];

                Vector3 position = controlPoint.position;
                Vector3 inTangent = controlPoint.inTangent;
                Vector3 outTangent = controlPoint.outTangent;


                bool tangentConnection = inTangent.normalized != -outTangent.normalized;
                float tangentRatio = inTangent.magnitude / outTangent.magnitude;


                int handleID = GUIUtility.GetControlID(handleHash, FocusType.Passive);
                if (TangentUtility.IsFree(controlPoint.outTangent))
                {
                    Vector3 tangentHandlePos = outTangent + position;
                    Color color = GUIUtility.hotControl == handleID ? Color.yellow : PathHandleUtility.handleColor;
                    if (!path.loop && i == controlPointCount - 1) color.a = 0.1f;
                    Handles.color = color;
                    Handles.DrawLine(position, tangentHandlePos);
                    EditorGUI.BeginChangeCheck();
                    tangentHandlePos = SplineHandleUtility.DoTangentHandle(handleID, tangentHandlePos, outTangent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        controlPoint.outTangent = tangentHandlePos - position;
                        if (TangentUtility.IsFree(controlPoint.inTangent) && !tangentConnection && !Event.current.alt)
                        {
                            if (Event.current.shift)
                            {
                                controlPoint.inTangent = -controlPoint.outTangent.normalized * controlPoint.inTangent.magnitude;
                            }
                            else
                            {
                                controlPoint.inTangent = -controlPoint.outTangent * tangentRatio;
                            }
                        }

                        Undo.RecordObject(path, "Edit Spline Control Point");
                        path.SetWorldControlPoint(controlPoint, i);
                    }
                }

                handleID = GUIUtility.GetControlID(handleHash, FocusType.Passive);
                if (TangentUtility.IsFree(controlPoint.inTangent))
                {
                    Vector3 tangentPosition = inTangent + position;
                    Color color = GUIUtility.hotControl == handleID ? Color.yellow : PathHandleUtility.handleColor;
                    if (!path.loop && i == 0) color.a = 0.1f;
                    Handles.color = color;
                    Handles.DrawLine(position, tangentPosition);
                    EditorGUI.BeginChangeCheck();
                    tangentPosition = SplineHandleUtility.DoTangentHandle(handleID, tangentPosition, -controlPoint.inTangent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        controlPoint.inTangent = tangentPosition - position;

                        if (TangentUtility.IsFree(controlPoint.outTangent) && !tangentConnection && !Event.current.alt)
                        {
                            if (Event.current.shift)
                            {
                                controlPoint.outTangent = -controlPoint.inTangent.normalized * controlPoint.outTangent.magnitude;
                            }
                            else
                            {
                                controlPoint.outTangent = -controlPoint.inTangent / tangentRatio;
                            }
                        }

                        Undo.RecordObject(path, "Edit Spline Control Point");
                        path.SetWorldControlPoint(controlPoint, i);
                    }
                }



                handleID = GUIUtility.GetControlID(handleHash, FocusType.Passive);
                Handles.color = GUIUtility.hotControl == handleID ? Color.yellow : PathHandleUtility.handleColor;
                EditorGUI.BeginChangeCheck();
                position = SplineHandleUtility.DoControlHandle(handleID, position);
                if (EditorGUI.EndChangeCheck())
                {
                    if (Event.current.alt)
                    {
                        Vector3 delta = (Vector3)controlPoint.position - position;
                        controlPoint.outTangent = -delta;
                        controlPoint.inTangent = delta;
                    }
                    else
                    {
                        controlPoint.position = position;
                    }

                    Undo.RecordObject(path, "Edit Spline Control Point");
                    path.SetWorldControlPoint(controlPoint, i);
                }


                if (!hoverHandle && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == addHandleID) && (path.loop || i != 0))
                {
                    SplineControlPoint startControlPoint = controlPoints[LoopUtility.PrevIndex(i, controlPointCount)];
                    SplineControlPoint endControlPoint = controlPoints[i];

                    Vector3 start = startControlPoint.position;
                    Vector3 end = endControlPoint.position;
                    Vector3 startTangent = startControlPoint.outTangent;
                    Vector3 endTangent = endControlPoint.inTangent;

                    startTangent += start;
                    endTangent += end;

                    PolylineUtility.CopyCurveToPolyline(start, end, startTangent, endTangent, s_Polyline);
                    if (HandleUtility.DistanceToPolyLine(s_Polyline) < 10)
                    {
                        Vector3 point = HandleUtility.ClosestPointToPolyLine(s_Polyline);
                        EditorGUI.BeginChangeCheck();
                        using (new Handles.DrawingScope(PathHandleUtility.handleColor.Fade(0.1f)))
                        {
                            var fmh_256_72_638780058723299765 = Quaternion.identity; Handles.FreeMoveHandle(addHandleID, point, PathHandleUtility.GetHandleSize(point), Vector3.zero, Handles.SphereHandleCap);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(path, "Edit Spline Path");
                            GUIUtility.hotControl = handleID;
                            GUI.changed = true;
                            float t = PolylineUtility.InverseLerp(s_Polyline, point);
                            Split(path, i, t);
                        }
                        hoverHandle = true;
                    }
                }
            }
        }

        void Split(SplineShape2D path, int index, float time)
        {
            path.InsertLocalControlPoint(new SplineControlPoint(), index);
            int count = path.controlPointCount;

            int ia = LoopUtility.PrevIndex(index, count);
            int ib = index;
            int ic = LoopUtility.NextIndex(index, count);

            SplineControlPoint a = path.GetLocalControlPoint(ia);
            SplineControlPoint b = path.GetLocalControlPoint(ib);
            SplineControlPoint c = path.GetLocalControlPoint(ic);

            Vector2 start = a.position;
            Vector2 end = c.position;
            Vector2 startTangent = a.outTangent + start;
            Vector2 endTangent = c.inTangent + end;
            Vector2 inTangent, outTangent;
            Vector2 position = BezierUtility.Split(start, ref startTangent, ref endTangent, end, out inTangent, out outTangent, time);
            inTangent = inTangent - position;
            outTangent = outTangent - position;

            a.position = start;
            a.outTangent = startTangent - start;

            b.position = position;
            b.inTangent = inTangent;
            b.outTangent = outTangent;

            c.position = end;
            c.inTangent = endTangent - end;

            path.SetLocalControlPoint(a, ia);
            path.SetLocalControlPoint(b, ib);
            path.SetLocalControlPoint(c, ic);
        }


        static class SplineHandleUtility
        {
            static Handles.CapFunction s_TangentHandleCapFunction = Handles.ConeHandleCap;
            static Quaternion s_TangentHandleCapRotation;

            public static Vector3 DoControlHandle(int id, Vector3 position)
            {
                float handleSize = PathHandleUtility.GetHandleSize(position);
                var fmh_318_65_638780058723304996 = Quaternion.identity; position = Handles.FreeMoveHandle(id, position, handleSize, Vector3.zero, Handles.SphereHandleCap);
                if (Event.current.control) position = EditorGridUtility.SnapToGrid(position);
                return position;
            }

            public static Vector3 DoTangentHandle(int id, Vector3 position, Vector3 dir)
            {
                float angle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.Euler(angle, 90, 0);
                float handleSize = PathHandleUtility.GetHandleSize(position);
                s_TangentHandleCapRotation = q;

                position = Handles.FreeMoveHandle(id, position, handleSize, Vector3.zero, TangentHandleCap);
                if (Event.current.control) position = EditorGridUtility.SnapToGrid(position);
                return position;
            }

            static void TangentHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
            {
                s_TangentHandleCapFunction.Invoke(controlID, position, s_TangentHandleCapRotation, size, eventType);
            }
        }

        static class TangentUtility
        {
            public static bool IsFree(Vector2 tangent)
            {
                return tangent.x != 0 || tangent.y != 0;
            }

            public static bool IsLiner(Vector2 tangent)
            {
                return tangent == Vector2.zero;
            }
        }

        static class PolylineUtility
        {
            public static float InverseLerp(Vector3[] polyline, Vector3 point)
            {
                for (int i = 1; i < polyline.Length; i++)
                {
                    Vector3 a = polyline[i - 1];
                    Vector3 b = polyline[i];
                    if (HandleUtility.DistancePointToLineSegment(point, a, b) < 0.01f)
                    {
                        float tA = (float)(i - 1) / (polyline.Length - 1);
                        float tB = (float)(i) / (polyline.Length - 1);
                        float t = Vector3.Distance(a, point) / Vector3.Distance(a, b);
                        return Mathf.Lerp(tA, tB, t);
                    }
                }

                return 0;
            }

            public static void CopyCurveToPolyline(Vector2 a, Vector2 b, Vector2 aTan, Vector2 bTan, Vector3[] polyline)
            {
                int iMax = polyline.Length;
                float i2Time = 1f / (iMax - 1);
                for (int i = 0; i < iMax; i++)
                {
                    float time = i * i2Time;
                    polyline[i] = BezierUtility.Evaluate(a, aTan, bTan, b, time);
                }
            }
        }

        class ControlPointMenu
        {
            SplineShape2D m_Path;
            int m_Index;

            public ControlPointMenu(SplineShape2D splinePath, int index)
            {
                m_Path = splinePath;
                m_Index = index;
            }

            public void Open()
            {
                GenericMenu menu = new GenericMenu();

                if (m_Path.controlPointCount > 2) menu.AddItem(new GUIContent("Delete"), false, Delete);

                SplineControlPoint controlPoint = m_Path.GetLocalControlPoint(m_Index);
                menu.AddSeparator("");

                bool outFree = TangentUtility.IsFree(controlPoint.outTangent);
                bool inFree = TangentUtility.IsFree(controlPoint.inTangent);

                menu.AddItem(new GUIContent("In Tangent/Free"), inFree, SetTangents, new Vector2Int(1, 0));
                menu.AddItem(new GUIContent("In Tangent/Liner"), !inFree, SetTangents, new Vector2Int(-1, 0));

                menu.AddItem(new GUIContent("Out Tangent/Free"), outFree, SetTangents, new Vector2Int(0, 1));
                menu.AddItem(new GUIContent("Out Tangent/Liner"), !outFree, SetTangents, new Vector2Int(0, -1));

                menu.AddItem(new GUIContent("Both Tangents/Free"), outFree && inFree, SetTangents, new Vector2Int(1, 1));
                menu.AddItem(new GUIContent("Both Tangents/Liner"), !outFree && !inFree, SetTangents, new Vector2Int(-1, -1));

                menu.ShowAsContext();
            }

            void Delete()
            {
                Undo.RecordObject(m_Path, "Delete Control Point");
                m_Path.RemoveControlPointAt(m_Index);
                ShapeTracker.RecordChange(m_Path);
            }

            void SetTangents(object data)
            {
                Vector2Int tangent = (Vector2Int)data;

                Undo.RecordObject(m_Path, "Spline Control Point Tangents");
                SplineControlPoint controlPoint = m_Path.GetLocalControlPoint(m_Index);

                if (tangent.x != 0)
                    controlPoint.inTangent = tangent.x == 1 ? Vector3.left : Vector3.zero;

                if (tangent.y != 0)
                    controlPoint.outTangent = tangent.y == 1 ? Vector3.right : Vector3.zero;

                m_Path.SetLocalControlPoint(controlPoint, m_Index);
                ShapeTracker.RecordChange(m_Path);
            }
        }
    }
}