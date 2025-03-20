using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for visualizing 2D polygons by gizmos.
    /// </summary>
    public static class GizmosUtility
    {
        public static void DrawPolygon(List<Vector2[]> polygons, Color color)
        {
            using (new Scope(color))
            {
                DrawPolygon(polygons);
            }
        }

        public static void DrawPolygon(List<Vector2[]> polygons)
        {
            foreach (var polygon in polygons)
            {
                DrawPolygon(polygon);
            }
        }

        public static void DrawPolygon(Vector2[] polygon, Color color)
        {
            using (new Scope(color))
            {
                DrawPolygon(polygon);
            }
        }


        public static void DrawPolygon(Vector2[] polygon)
        {
            int n = polygon.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[LoopUtility.NextIndex(i, n)];
                Gizmos.DrawLine(a, b);
            }
        }

        public static void DrawPolyline(Vector2[] polyline, Color color)
        {
            using (new Scope(color))
            {
                DrawPolyline(polyline);
            }
        }

        public static void DrawPolyline(Vector2[] polyline)
        {
            int n = polyline.Length;
            for (int i = 1; i < n; i++)
            {
                Vector2 a = polyline[i - 1];
                Vector2 b = polyline[i];
                Gizmos.DrawLine(a, b);
            }
        }

        public static void DrawSpheres(Vector2[] positions, float radius, Color color)
        {
            using (new Scope(color))
            {
                DrawSpheres(positions, radius);
            }
        }

        public static void DrawSpheres(Vector2[] positions, float radius)
        {
            foreach (var p in positions)
            {
                Gizmos.DrawSphere(p, radius);
            }
        }

        public static void DrawRect(Vector2 position, Vector2 size, Color color)
        {
            using (new Scope(color))
            {
                DrawRect(position, size);
            }
        }

        public static void DrawRect(Vector2 position, Vector2 size, Matrix4x4 matrix)
        {
            Vector3 a = new Vector3(position.x, position.y);
            Vector3 b = new Vector3(position.x + size.x, position.y);
            Vector3 c = new Vector3(position.x + size.x, position.y + size.y);
            Vector3 d = new Vector3(position.x, position.y + size.y);

            a = matrix.MultiplyPoint3x4(a);
            b = matrix.MultiplyPoint3x4(b);
            c = matrix.MultiplyPoint3x4(c);
            d = matrix.MultiplyPoint3x4(d);

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        public static void DrawRect(Vector2 position, Vector2 size)
        {
            Vector3 a = new Vector3(position.x, position.y);
            Vector3 b = new Vector3(position.x + size.x, position.y);
            Vector3 c = new Vector3(position.x + size.x, position.y + size.y);
            Vector3 d = new Vector3(position.x, position.y + size.y);

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }


        public static bool IsSelected(GameObject gameObject)
        {

#if UNITY_EDITOR
            return UnityEditor.Selection.Contains(gameObject);
#else
            return false;
#endif
        }

        public static bool IsChildSelected(Transform transform)
        {
#if UNITY_EDITOR
            foreach (var selectedTransform in UnityEditor.Selection.transforms)
            {
                if (selectedTransform.IsChildOf(transform) && selectedTransform != transform) return true;
            }
#endif
            return false;
        }

        public class Scope : System.IDisposable
        {
            Color m_DefaultColor;

            public Scope(Color color)
            {
                m_DefaultColor = Gizmos.color;
                Gizmos.color = color;
            }


            public Scope(Color color, float alpha)
            {
                m_DefaultColor = Gizmos.color;
                color.a = alpha;
                Gizmos.color = color;
            }

            public void Dispose()
            {
                Gizmos.color = m_DefaultColor;
            }
        }
    }
}