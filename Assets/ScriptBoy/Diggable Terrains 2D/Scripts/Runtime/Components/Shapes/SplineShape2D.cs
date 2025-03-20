using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Shapes/Spline Shape 2D")]
    public sealed class SplineShape2D : Shape2D
    {
        public const float MinThickness = 0.1f;
        public const int MinMidPointCount = 5;
        public const int MaxMidPointCount = 20;
        public const int MinCapPointCount = 0;
        public const int MaxCapPointCount = 20;


        [SerializeField, HideInInspector] bool m_Loop;
        [SerializeField, HideInInspector] float m_Thickness;
        [SerializeField, HideInInspector] int m_CapPointCount;
        [SerializeField, HideInInspector] int m_MidPointCount;
        [SerializeField, HideInInspector] List<SplineControlPoint> m_ControlPoints;


        /// <summary>
        /// The number of points between two control points.
        /// </summary>
        public int midPointCount
        {
            get => m_MidPointCount;
            set => m_MidPointCount = Mathf.Clamp(value, MinMidPointCount, MaxMidPointCount );
        }

        /// <summary>
        /// The number of points that are added to round the start and end of the spline (when the loop is false).
        /// </summary>
        public int capPointCount
        {
            get => m_CapPointCount;
            set => m_CapPointCount = Mathf.Clamp(value, MinCapPointCount, MaxCapPointCount);
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
        /// The thickness of the spline (when the loop is false).
        /// </summary>
        public float thickness
        {
            get => m_Thickness;
            set => m_Thickness = Mathf.Max(MinThickness, value);
        }

        /// <summary>
        /// The number of the control points.
        /// </summary>
        public int controlPointCount
        {
            get => m_ControlPoints.Count;
        }

        /// <summary>
        /// Gets or sets an array of the control points in local space.
        /// </summary>
        public SplineControlPoint[] localControlPoints
        {
            get => m_ControlPoints.ToArray();
            set => m_ControlPoints = new List<SplineControlPoint>(value);
        }

        /// <summary>
        /// Gets or sets an array of the control points in world space.
        /// </summary>
        public SplineControlPoint[] worldControlPoints
        {
            get
            {
                int n = m_ControlPoints.Count;
                Matrix4x4 matrix = transform.localToWorldMatrix;
                SplineControlPoint[] controls = m_ControlPoints.ToArray();
                for (int i = 0; i < n; i++)
                {
                    controls[i].DoTransform(matrix);
                }
                return controls;
            }

            set
            {
                int n = value.Length;
                m_ControlPoints = new List<SplineControlPoint>(value);
                Matrix4x4 matrix = transform.localToWorldMatrix.inverse;
                for (int i = 0; i < n; i++)
                {
                    m_ControlPoints[i].DoTransform(matrix);
                }
            }
        }


        public SplineShape2D()
        {
            m_Thickness = 0.5f;
            m_MidPointCount = 10;
            m_CapPointCount = 5;

            m_ControlPoints = new List<SplineControlPoint>()
            {
                new SplineControlPoint(new Vector2(-4, 2), new Vector2(0, 0), new Vector2(0, 0)),
                new SplineControlPoint(new Vector2(0, 2), new Vector2(-2, -2), new Vector2(2, 2)),
                new SplineControlPoint(new Vector2(4, 1), new Vector2(0, 0), new Vector2(0, 0)),
                new SplineControlPoint(new Vector2(4, -2), new Vector2(0, 0), new Vector2(0, 0)),
                new SplineControlPoint(new Vector2(-4, -2), new Vector2(0, 0), new Vector2(0, 0))
            };
        }

        /// <summary>
        /// Gets the control point in world space at the given index.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public SplineControlPoint GetWorldControlPoint(int index)
        {
            return GetLocalControlPoint(index).Transform(transform.localToWorldMatrix);
        }

        /// <summary>
        /// Gets the control point in local space at the given index.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public SplineControlPoint GetLocalControlPoint(int index)
        {
            return m_ControlPoints[index];
        }

        /// <summary>
        /// Sets the control point in world space at the given index.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public void SetWorldControlPoint(SplineControlPoint control, int index)
        {
            SetLocalControlPoint(control.Transform(transform.worldToLocalMatrix), index);
        }

        /// <summary>
        /// Sets the control point in local space at the given index.
        /// </summary>
        /// <param name="index">The index of the control point.</param>
        public void SetLocalControlPoint(SplineControlPoint control, int index)
        {
            m_ControlPoints[index] = control;
        }

        /// <summary>
        /// Inserts a control point in world space at the given index.
        /// </summary>
        /// <param name="control">The control point to insert.</param>
        /// <param name="index">The index where the control point should be inserted.</param>
        public void InsertWorldControlPoint(SplineControlPoint control, int index)
        {
            InsertLocalControlPoint(control.Transform(transform.worldToLocalMatrix), index);
        }

        /// <summary>
        /// Inserts a control point in local space at the given index.
        /// </summary>
        /// <param name="control">The control point to insert.</param>
        /// <param name="index">The index where the control point should be inserted.</param>
        public void InsertLocalControlPoint(SplineControlPoint control, int index)
        {
            m_ControlPoints.Insert(index, control);
        }

        /// <summary>
        /// Removes the control point at the given index.
        /// </summary>
        /// <param name="index">The index of the control point to remove.</param>
        public void RemoveControlPointAt(int index)
        {
            m_ControlPoints.RemoveAt(index);
        }

        protected override Vector2[] CreateLocalPoints()
        {
            int cCount = m_ControlPoints.Count;
            int pointCount = m_MidPointCount + 1;
            int pCount = (m_Loop ? cCount : cCount - 1) * pointCount ;
            if (!m_Loop) pCount++;
            Vector2[] verts = new Vector2[pCount];
            int pIndex = 0;
            for (int i = 0; i < (m_Loop ? cCount : cCount - 1); i++)
            {
                var startControl = m_ControlPoints[i];
                var endControl = m_ControlPoints[LoopUtility.LoopIndex(i + 1, cCount)];

                Vector2 start = startControl.position;
                Vector2 end = endControl.position;
                Vector2 startTangent = startControl.outTangent + start;
                Vector2 endTangent = endControl.inTangent + end;

                for (int j = 0; j < pointCount; j++)
                {
                    float t = (float)j / (pointCount);
                    verts[pIndex] = BezierUtility.Evaluate(start, startTangent, endTangent, end, t);
                    pIndex++;
                }
            }

            if (!m_Loop)
            {
                verts[pCount - 1] = m_ControlPoints[cCount - 1].position;
                verts = PolylineUtility.CreatePolygon(verts, m_Thickness, m_CapPointCount);
                verts = PolygonSimplifier.Simplify(verts, 0.005f, false);
                verts = PolygonBorderTracing.Trace(verts);
            }

            if (PolygonUtility.IsSelfIntersecting(verts))
                return new Vector2[0];

            return verts;
        }
    }

    [System.Serializable]
    public struct SplineControlPoint
    {
        public Vector2 position;
        public Vector2 inTangent;
        public Vector2 outTangent;

        public SplineControlPoint(Vector2 position, Vector2 inTangent, Vector2 outTangent)
        {
            this.position = position;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
        }

        public SplineControlPoint Transform(Matrix4x4 matrix)
        {
            SplineControlPoint control = this;
            control.DoTransform(matrix);
            return control;
        }

        public void DoTransform(Matrix4x4 matrix)
        {
            position = matrix.MultiplyPoint3x4(position);
            inTangent = matrix.MultiplyVector(inTangent);
            outTangent = matrix.MultiplyVector(outTangent);
        }
    }
}