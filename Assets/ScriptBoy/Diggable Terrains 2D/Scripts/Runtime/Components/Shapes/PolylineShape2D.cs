using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Shapes/Polyline Shape 2D")]
    public sealed class PolylineShape2D : Shape2D
    {
        public const float MinCornerRadius = 0;
        public const float MinThickness = 0.1f;
        public const int MinCornerPointCount = 0;
        public const int MaxCornerPointCount = 20;
        public const int MinCapPointCount = 0;
        public const int MaxCapPointCount = 20;

        [SerializeField, HideInInspector] bool m_Loop;
        [SerializeField, HideInInspector] float m_Thickness;
        [SerializeField, HideInInspector] int m_CapPointCount;
        [SerializeField, HideInInspector] int m_CornerPointCount;
        [SerializeField, HideInInspector] float m_CornerRadius;
        [SerializeField, HideInInspector] List<Vector2> m_ControlPoints;


        /// <summary>
        /// The radius used to round the corners.
        /// </summary>
        public float cornerRadius
        {
            get => m_CornerRadius;
            set => m_CornerRadius = Mathf.Max(value, MinCornerRadius);
        }

        /// <summary>
        /// The number of additional points that are added to round the corners.
        /// </summary>
        public int cornerPointCount
        {
            get => m_CornerPointCount;
            set => m_CornerPointCount = Mathf.Clamp(value, MinCornerPointCount, MaxCornerPointCount);
        }

        /// <summary>
        /// The number of points that are added to round the start and end of the polyline (when the loop is false).
        /// </summary>
        public int capPointCount
        {
            get => m_CapPointCount;
            set => m_CapPointCount = Mathf.Clamp(value, MinCapPointCount, MaxCapPointCount);
        }

        /// <summary>
        /// The thickness of the polyline (when the loop is false).
        /// </summary>
        public float thickness
        {
            get => m_Thickness;
            set => m_Thickness = Mathf.Max(value, MinThickness);
        }

        /// <summary>
        /// Connects the first and last control points to create a closed shape.
        /// </summary>
        public bool loop
        {
            get => m_Loop;
            set => m_Loop = value;
        }

        /// <summary>
        /// The number of the control points.
        /// </summary>
        public int controlPointCount => m_ControlPoints.Count;

        /// <summary>
        /// Gets or sets an array of the control points in local space.
        /// </summary>
        public Vector2[] localControlPoints
        {
            get => m_ControlPoints.ToArray();
            set => m_ControlPoints = new List<Vector2>(value);
        }

        /// <summary>
        /// Gets or sets an array of the control points in world space.
        /// </summary>
        public Vector2[] worldControlPoints
        {
            get => PolygonUtility.Transform(localControlPoints, transform.localToWorldMatrix);
            set
            {
                localControlPoints = PolygonUtility.Transform(value, transform.localToWorldMatrix.inverse);
            }
        }

        public PolylineShape2D()
        {
            m_CornerRadius = 0.5f;
            m_CornerPointCount = 5;
            m_CapPointCount = 5;
            m_Thickness = 0.5f;
            m_ControlPoints = new List<Vector2>(PolygonUtility.CreateBox(Vector2.zero, Vector2.one * 2));
        }

        /// <summary>
        /// Gets the control point in world space at the given indexs.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public Vector2 GetWorldControlPoint(int index)
        {
            return transform.TransformPoint(GetLocalControlPoint(index));
        }

        /// <summary>
        /// Gets the control point in local space at the given index.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public Vector2 GetLocalControlPoint(int index)
        {
            return m_ControlPoints[index];
        }

        /// <summary>
        /// Sets the control point in world space at the given index .
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public void SetWorldControlPoint(Vector2 point, int index)
        {
            SetLocalControlPoint(transform.InverseTransformPoint(point), index);
        }

        /// <summary>
        /// Sets the control point in local space at the given index.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public void SetLocalControlPoint(Vector2 point, int index)
        {
            m_ControlPoints[index] = point;
        }

        /// <summary>
        /// Inserts a control point in world space at the given index.
        /// </summary>
        /// <param name="control">The control point to insert.</param>
        /// <param name="index">The index where the control point should be inserted.</param>
        public void InsertWorldControlPoint(Vector2 point, int index)
        {
            InsertLocalControlPoint(transform.InverseTransformPoint(point), index);
        }

        /// <summary>
        /// Inserts a control point in local space at the given index.
        /// </summary>
        /// <param name="control">The control point to insert.</param>
        /// <param name="index">The index where the control point should be inserted.</param>
        public void InsertLocalControlPoint(Vector2 point, int index)
        {
            m_ControlPoints.Insert(index, point);
        }

        /// <summary>
        /// Removes the control point at the given index.
        /// </summary>
        /// <param name="index">The index of the control point to remove.</param>
        public void RemoveControlPoint(int index)
        {
            m_ControlPoints.RemoveAt(index);
        }

        protected override Vector2[] CreateLocalPoints()
        {
            var verts = m_ControlPoints.ToArray();

            if (m_Loop)
            {
                if (m_CornerRadius > 0 && m_CornerPointCount > 0)
                {
                    verts = PolygonUtility.RoundCorner(verts, m_CornerPointCount, m_CornerRadius);
                }

                if (PolygonUtility.IsSelfIntersecting(verts)) return new Vector2[0];
            }
            else
            {
                if (m_CornerRadius > 0 && m_CornerPointCount > 0)
                {
                    verts = PolylineUtility.RoundCorner(verts, m_CornerPointCount, m_CornerRadius);
                }

                verts = PolylineUtility.CreatePolygon(verts, m_Thickness, m_CapPointCount);
                verts = PolygonSimplifier.Simplify(verts, 0.001f, false);
                verts = PolygonBorderTracing.Trace(verts);
            }

            return verts;
        }
    }
}
