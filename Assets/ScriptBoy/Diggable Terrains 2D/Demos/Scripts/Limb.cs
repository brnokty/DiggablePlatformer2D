/*
The Limb component is used to create arms and legs of the player. 

Here is how it works:
1. Gets 3 transforms as control points.
2. Creates an array of positions along the control points.
3. Sends these positions to the LineRenderer component for rendering.
*/


using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(LineRenderer))]
    public class Limb : MonoBehaviour
    {
        [SerializeField] Transform m_A;
        [SerializeField] Transform m_B;
        [SerializeField] Transform m_C;

        LineRenderer m_LineRenderer;

        const int k_PositionCount = 20;
        Vector3[] m_Positions = new Vector3[k_PositionCount];

        public Transform a { get => m_A; set => m_A = value; }
        public Transform b { get => m_B; set => m_B = value; }
        public Transform c { get => m_C; set => m_C = value; }

        public LineRenderer lineRenderer
        {
            get
            {
                if (m_LineRenderer == null)
                {
                    m_LineRenderer = GetComponent<LineRenderer>();
                }
                return m_LineRenderer;
            }
        }

        public void Update()
        {
            if (m_A == null) return;
            if (m_B == null) return;
            if (m_C == null) return;

            Vector3 a = transform.InverseTransformPoint(m_A.position);
            Vector3 b = transform.InverseTransformPoint(m_B.position);
            Vector3 c = transform.InverseTransformPoint(m_C.position);

            for (int i = 0; i < k_PositionCount; i++)
            {
                float t = (float)i / (k_PositionCount - 1);
                m_Positions[i] = Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t);
            }

            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = k_PositionCount;
            lineRenderer.SetPositions(m_Positions);
        }


#if UNITY_EDITOR
        static List<Limb> limbs = new List<Limb>();
        void OnEnable() => limbs.Add(this);
        void OnDisable() => limbs.Remove(this);

        class Editor
        {
            [InitializeOnLoadMethod]
            static void Init() => new Editor();

            const float k_HandleSize = 0.1f;

            Editor()
            {
                SceneView.duringSceneGui += OnSceneGUI;
            }

            void OnSceneGUI(SceneView sceneView)
            {
                var activeTransform = Selection.activeTransform;
                if (activeTransform == null) return;
                var root = activeTransform.root;

                foreach (var limb in limbs)
                {
                    if (limb.transform.root == root)
                    {
                        DoLimbEditor(limb);
                    }
                }
            }

            void DoLimbEditor(Limb limb)
            {
                if (limb == null) return;
                if (limb.a == null) return;
                if (limb.b == null) return;
                if (limb.c == null) return;

                using (new Handles.DrawingScope(limb.b.IsChildOf(limb.a) ? Color.white : Color.yellow))
                {
                    EditorGUI.BeginChangeCheck();
                    DoMoveHandle(limb.a);
                    DoMoveHandle(limb.b);
                    DoMoveHandle(limb.c);
                    if (EditorGUI.EndChangeCheck())
                    {
                        limb.Update();
                    }

                    Handles.DrawAAPolyLine(limb.a.position, limb.b.position, limb.c.position);
                }
            }

            void DoMoveHandle(Transform transform)
            {
                Vector3 position = transform.position;
                float size = HandleUtility.GetHandleSize(position) * k_HandleSize;
                EditorGUI.BeginChangeCheck();
                var fmh_130_61_638528429454605964 = Quaternion.identity; var fmh_130_118_638780058721348326 = Quaternion.identity; position = Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(transform, "Move");
                    transform.position = position;
                }
            }
        }
#endif
    }
}