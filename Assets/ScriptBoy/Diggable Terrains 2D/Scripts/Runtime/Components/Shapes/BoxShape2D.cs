using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Shapes/Box Shape 2D")]
    public sealed class BoxShape2D : Shape2D
    {
        public const float MinSize = 0.25f;
        public const int MinCornerPointCount = 0;
        public const int MaxCornerPointCount = 20;


        [SerializeField, HideInInspector] float m_Width;
        [SerializeField, HideInInspector] float m_Height;
        [SerializeField, HideInInspector] float m_CornerRadius;
        [SerializeField, HideInInspector] int m_CornerPointCount;


        /// <summary>
        /// The width of the box.
        /// </summary>
        public float width
        {
            get => m_Width;
            set => m_Width = Mathf.Max(value, MinSize);
        }

        /// <summary>
        /// The height of the box.
        /// </summary>
        public float height
        {
            get => m_Height;
            set => m_Height = Mathf.Max(value, MinSize);
        }

        /// <summary>
        /// The radius that is used to round the corners.
        /// </summary>
        public float cornerRadius
        {
            get => m_CornerRadius;
            set => m_CornerRadius = Mathf.Max(value, MinSize);
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
        /// The size of the box.
        /// </summary>
        public Vector2 size
        {
            get => new Vector2(width, height);
            set
            {
                width = value.x;
                height = value.y;
            }
        }

        public BoxShape2D()
        {
            m_Width = 2;
            m_Height = 2;
            m_CornerRadius = 0.5f;
            m_CornerPointCount = 0;
        }

        protected override Vector2[] CreateLocalPoints()
        {
            Vector2[] box = PolygonUtility.CreateBox(Vector2.zero, size);
            if (m_CornerRadius > 0 && m_CornerPointCount > 0)
            {
                box = PolygonUtility.RoundCorner(box, m_CornerPointCount, m_CornerRadius);
            }
            return box;
        }
    }
}