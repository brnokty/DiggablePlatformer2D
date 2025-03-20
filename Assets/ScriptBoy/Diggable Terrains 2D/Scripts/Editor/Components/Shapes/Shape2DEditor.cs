using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Shape2D), true)]
    class Shape2DEditor : Editor
    {
        static bool s_EditMode;

        void OnEnable()
        {
            foreach (var target in targets)
            {
                if (target.name == "GameObject")
                    target.name = target.GetType().Name.AddWordSpaces();
            }

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            this.FindProperties();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
        }

        void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            foreach (var target in targets)
            {
                if (target == null) return;

                var path = target as Shape2D;
                if (path.gameObject.GetInstanceID() == instanceID)
                {
                    CheckDeleteCommand();
                    return;
                }
            }
        }

        void CheckDeleteCommand()
        {
            Event uEvent = Event.current;
            if (uEvent.type == EventType.KeyDown && uEvent.keyCode == KeyCode.Delete)
            {
                Transform parent = null;
                foreach (var target in targets)
                {
                    var shape = target as Shape2D;

                    if (parent == null)
                    {
                        parent = shape.transform.parent;
                    }
                    Undo.DestroyObjectImmediate(shape.gameObject);
                }

                if (parent != null)
                {
                    Selection.activeTransform = parent;
                }
                uEvent.Use();
                return;
            }
        }

        void OnSceneGUI(SceneView scene)
        {
            CheckDeleteCommand();

            if (!s_EditMode) return;
            if(target == null) return;

            EditorGUI.BeginChangeCheck();
            DrawHandles();
            if (EditorGUI.EndChangeCheck())
            {
                Shape2D shape = target as Shape2D;
                shape.UpdateGizmos();
                ShapeTracker.RecordChange(shape);
            }

            scene.Repaint();
        }

        public override void OnInspectorGUI()
        {
            EditModeButton.Draw(ref s_EditMode, "Edit Mode");
            if (s_EditMode)
            {
                HelpBox.Draw(GetHelpInfo(), 0);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawInspector();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                foreach (var target in targets)
                {
                    Shape2D shape = target as Shape2D;
                    shape.UpdateGizmos();
                    ShapeTracker.RecordChange(shape);
                }
            }
        }


        protected virtual void DrawInspector()
        {
            base.OnInspectorGUI();
        }

        protected virtual string GetHelpInfo()
        {
            return "";
        }

        protected virtual void DrawHandles()
        {
            
        }

        protected static class PathHandleUtility
        {

            public static readonly Color lineColor = new Color32(200, 250, 100, 255);
            public static readonly float lineWidth = 2.0f;

            public static readonly Color handleColor = new Color32(220, 250, 250, 255);
            public static readonly float handleSize = 0.15f;

      
            public static float GetHandleSize(Vector3 position)
            {
                return HandleUtility.GetHandleSize(position) * handleSize;
            }
        }
    }

    static class ShapeTracker
    {
        public static Action<Shape2D> onChanged;
        static Dictionary<Shape2D, MonoBehaviourState> m_States;

        public static void RecordChange(Shape2D path)
        {
            onChanged?.Invoke(path);
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            m_States = new Dictionary<Shape2D, MonoBehaviourState>();
            Shape2D.onEnable += OnEnablePath;
            Shape2D.onDestroy += OnDestroyPath;
            EditorApplication.update += Update;
        }

        static void OnEnablePath(Shape2D path)
        {
            if (!m_States.ContainsKey(path))
            {
                m_States.Add(path, new MonoBehaviourState(path));
            }
            RecordChange(path);
        }

        static void OnDestroyPath(Shape2D path)
        {
            m_States.Remove(path);
            RecordChange(path);
        }

        static void Update()
        {
            foreach (var state in m_States)
            {
                if (state.Value.HasChanged())
                {
                    RecordChange(state.Key);
                }
            }
        }
    }

    class MonoBehaviourState
    {
        MonoBehaviour m_MonoBehaviour;
        Matrix4x4 m_Matrix;
        Type m_Type;
        bool m_Enabled;
        bool m_Active;

        public MonoBehaviourState(MonoBehaviour monoBehaviour)
        {
            m_MonoBehaviour = monoBehaviour;
            m_Type = monoBehaviour.GetType();
            m_Matrix = monoBehaviour.transform.localToWorldMatrix;
            m_Enabled = monoBehaviour.enabled;
            m_Active = monoBehaviour.gameObject.activeInHierarchy;
        }

        public bool HasChanged()
        {
            bool changed = false;

            if (m_MonoBehaviour == null)
            {
                if (!ReferenceEquals(m_MonoBehaviour, null))
                {
                    int id = m_MonoBehaviour.GetInstanceID();
                    m_MonoBehaviour = UnityEngine.Object.FindObjectsOfType(m_MonoBehaviour.GetType()).FirstOrDefault(p => p.GetInstanceID() == id) as MonoBehaviour;
                    changed = true;
                }
            }


            Matrix4x4 matrix = m_MonoBehaviour.transform.localToWorldMatrix;
            if (m_Matrix != matrix)
            {
                m_Matrix = matrix;
                changed = true;
            }

            bool enabled = m_MonoBehaviour.enabled;
            if (m_Enabled != enabled)
            {
                m_Enabled = enabled;
                changed = true;
            }

            bool active = m_MonoBehaviour.gameObject.activeInHierarchy;
            if (m_Active != active)
            {
                m_Active = active;
                changed = true;
            }

            return changed;
        }
    }
}