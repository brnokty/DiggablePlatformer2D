
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [CustomEditor(typeof(Shovel))]
    class ShovelEditor : Editor
    {
        static class GUIContents
        {
            public static readonly GUIContent shape = new GUIContent("Shape", "The shape is used to create the shove polygon.");
            public static readonly GUIContent simplification = new GUIContent("Simplification", "The simplification threshold.");
            public static readonly GUIContent enableDemo = new GUIContent("Enable Demo", "Enables a quick demo for testing the dig function.");
            public static readonly GUIContent enableWave = new GUIContent("Enable Wave", "Enables an effect that randomizes the shovel polygon.");
            public static readonly GUIContent waveSettings = new GUIContent("Wave", "The wave settings.");
            public static readonly GUIContent waveLength = new GUIContent("Length", "The length of the wave.");
            public static readonly GUIContent waveAmplitude = new GUIContent("Amplitude", "The amplitude of the wave.");
        }


        Shovel m_Shovel;

        SerializedProperty m_ShapeProp;
        SerializedProperty m_SimplificationProp;
        SerializedProperty m_EnableDemoProp;
        SerializedProperty m_EnableWaveProp;
        SerializedProperty m_WaveLengthProp;
        SerializedProperty m_WaveAmplitudeProp;
        
        void OnEnable()
        {
            m_Shovel = target as Shovel;
            this.FindProperties();
        }

        public override void OnInspectorGUI()
        {
            Shape2D shape = m_ShapeProp.objectReferenceValue as Shape2D;


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_ShapeProp, GUIContents.shape);
            if (shape == null)
            {
                if (GUILayout.Button("New", GUILayout.Width(75)))
                {
                    var menu = new GenericMenu();
                    var types = AssemblyUtility.FindSubclassOf<Shape2D>();
                    foreach (var type in types)
                    {
                        string name = type.Name.AddWordSpaces();
                        menu.AddItem(new GUIContent(type.Name.AddWordSpaces()), false, () =>
                        {
                            GameObject gameObject = new GameObject(name.Replace(" Shape 2D", ""));
                            gameObject.transform.SetParent(m_Shovel.transform, false);
                            m_ShapeProp.objectReferenceValue = gameObject.AddComponent(type);
                            m_ShapeProp.serializedObject.ApplyModifiedProperties();
                            Undo.RegisterCreatedObjectUndo(gameObject, "Create Shape");
                        });
                    }
                    menu.ShowAsContext();
                }
            }
            else
            {
                if (GUILayout.Button("Remove", GUILayout.Width(75)))
                {
                    m_ShapeProp.objectReferenceValue = null;
                    m_ShapeProp.serializedObject.ApplyModifiedProperties();
                    if(shape.gameObject != m_Shovel.gameObject)
                    Undo.DestroyObjectImmediate(shape.gameObject);
                    else Undo.DestroyObjectImmediate(shape);
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Slider(m_SimplificationProp, Shovel.MinSimplification, Shovel.MaxSimplification, GUIContents.simplification);
            EditorGUILayout.PropertyField(m_EnableDemoProp, GUIContents.enableDemo);
            EditorGUILayout.PropertyField(m_EnableWaveProp, GUIContents.enableWave);

            if (shape != null && m_EnableWaveProp.boolValue)
            {
                if (!(shape is CircleShape2D || shape is BoxShape2D))
                {
                    EditorGUILayout.HelpBox("The wave effect is only supported for CircleShape2D and BoxShape2D.", MessageType.Warning);
                }
                else
                {
                    bool foldout = m_EnableWaveProp.isExpanded;
                    foldout = EditorGUILayout.Foldout(foldout, GUIContents.waveSettings);
                    m_EnableWaveProp.isExpanded = foldout;

                    if (foldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Slider(m_WaveLengthProp, Shovel.MinWaveLength, Shovel.MaxWaveLength, GUIContents.waveLength);
                        EditorGUILayout.Slider(m_WaveAmplitudeProp, Shovel.MinWaveAmplitude, Shovel.MaxWaveAmplitude, GUIContents.waveAmplitude);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            if (m_EnableDemoProp.boolValue)
            {
                EditorGUILayout.HelpBox("You can dig with mouse clicks, but it's just a quick demo. You should call shovel functions from your own scripts.", MessageType.Warning);
            }

   


            serializedObject.ApplyModifiedProperties();
        }
    }
}