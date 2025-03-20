using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [ExecuteInEditMode]
    public abstract class Shape2D : MonoBehaviour
    {
        static HashSet<Shape2D> s_Instances = new HashSet<Shape2D>();

        /// <summary>
        /// This property is used to track shapes in the editor. Please do not modify it.
        /// </summary>
        public static Action<Shape2D> onEnable;

        /// <summary>
        /// This property is used to track shapes in the editor. Please do not modify it.
        /// </summary>
        public static Action<Shape2D> onDestroy;

        /// <summary>
        /// Returns an array of Shape2D objects.
        /// </summary>
        public static Shape2D[] instances => s_Instances.ToArray();

        [SerializeField, HideInInspector] bool m_Fill = true;


        /// <summary>
        /// <para>If the shape is used for terrain, Set 'true' to fill areas, and set 'false' to remove areas.</para>
        /// <para>If the shape is used for shovel, the value of this property does not matter.</para>
        /// <para>(true = counter clockwise winding order, false = clockwise winding order)</para>
        /// </summary>
        public bool fill
        {
            get => m_Fill;
            set => m_Fill = value;
        }

        void OnEnable()
        {
            s_Instances.Add(this);
            onEnable?.Invoke(this);
#if UNITY_EDITOR
            UpdateGizmos();
#endif
        }

        void OnDestroy()
        {
            s_Instances.Remove(this);
            onDestroy?.Invoke(this);
        }


        /// <summary>
        /// Returns the shape points in world space.
        /// </summary>
        public Vector2[] GetWorldPoints()
        {
            var points = CreateLocalPoints();
            PolygonUtility.DoTransform(points, transform.localToWorldMatrix);
            FixWindingOrder(points);
            return points;
        }

        /// <summary>
        /// Returns the shape points in local space.
        /// </summary>
        public Vector2[] GetLocalPoints()
        {
            var points = CreateLocalPoints();
            FixWindingOrder(points);
            return points;
        }

        /// <summary>
        /// Returns the shape points in local space relative to the given transform.
        /// </summary>
        public Vector2[] GetRelativePoints(Transform transform)
        {
            return GetRelativePoints(transform.worldToLocalMatrix);
        }

        /// <summary>
        /// Returns the shape points in local space relative to the given matrix.
        /// </summary>
        public Vector2[] GetRelativePoints(Matrix4x4 worldToLocalMatrix)
        {
            var points = CreateLocalPoints();
            PolygonUtility.DoTransform(points, worldToLocalMatrix * transform.localToWorldMatrix);
            FixWindingOrder(points);
            return points;
        }

        void FixWindingOrder(Vector2[] points)
        {
            if (!m_Fill ^ PolygonUtility.IsClockwise(points))
            {
                PolygonUtility.DoReverse(points);
            }
        }


        protected abstract Vector2[] CreateLocalPoints();


#if UNITY_EDITOR
        static MeshData m_MeshData = new MeshData(1, 0);
        [NonSerialized] Mesh m_GizmoMesh;
        [NonSerialized] Vector2[] m_GizmoPolygon;

        void Reset()
        {
            UpdateGizmos();
        }

        public void UpdateGizmos()
        {
            m_GizmoPolygon = GetLocalPoints();

            if (m_GizmoPolygon.Length == 0) return;

            if (m_GizmoMesh == null)
            {
                m_GizmoMesh = new Mesh();
                m_GizmoMesh.hideFlags = HideFlags.DontSave;
            }

            if (!m_Fill)
                PolygonUtility.DoReverse(m_GizmoPolygon);

            m_MeshData.Clear();
            PolygonMeshUtility.CreateFillMesh(m_MeshData, 0, m_GizmoPolygon, false);
            m_MeshData.CopyToMesh(m_GizmoMesh);
        }

        void OnDrawGizmos()
        {
            if (!enabled) return;
            if (m_GizmoMesh == null) return;
            if (m_GizmoPolygon == null) return;
            if (m_GizmoPolygon.Length == 0) return;


            bool selected = GizmosUtility.IsSelected(gameObject);
            bool isRootSelected = false;
            if (!selected)
            {
                Transform root = transform.root;
                if (root != null)
                {
                    isRootSelected = GizmosUtility.IsSelected(root.gameObject);
                    if (!isRootSelected && !GizmosUtility.IsChildSelected(root)) return;
                }
                else return;
            }

            Color color = selected ? Color.yellow : Color.white;
            float alpha = selected ? 0.3f : 0.1f;
            if (isRootSelected) alpha = 0;
            using (new GizmosUtility.Scope(color, alpha))
            {
                Vector3 p = transform.position;
                Quaternion r = transform.rotation;
                Vector3 s = transform.lossyScale;
                Gizmos.DrawMesh(m_GizmoMesh, 0, p, r, s);

                Matrix4x4 matrix = transform.localToWorldMatrix;
                GizmosUtility.DrawPolygon(PolygonUtility.Transform(m_GizmoPolygon, matrix));
            }
        }
#endif
    }
}