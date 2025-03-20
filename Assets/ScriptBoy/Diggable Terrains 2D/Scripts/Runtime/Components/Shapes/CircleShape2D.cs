using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Shapes/Circle Shape 2D")]
    public sealed class CircleShape2D : Shape2D
    {
        public const float MinRadius = 0.25f;
        public const int MinPointCount = 6;
        public const int MaxPointCount = 40;

        [SerializeField, HideInInspector] float m_Radius;
        [SerializeField, HideInInspector] int m_PointCount;


        /// <summary>
        /// The radius of the circle.
        /// </summary>
        public float radius
        {
            get => m_Radius;
            set => m_Radius = Mathf.Max(value, MinRadius);
        }

        /// <summary>
        /// The number of points that form the cicrle.
        /// </summary>
        public int pointCount
        {
            get => m_PointCount;
            set => m_PointCount = Mathf.Clamp(value, MinPointCount, MaxPointCount);
        }

        public CircleShape2D()
        {
            m_Radius = 1;
            m_PointCount = 20;
        }

        protected override Vector2[] CreateLocalPoints()
        {
            return PolygonUtility.CreateCircle(Vector2.zero, m_PointCount, m_Radius);
        }
    }
}