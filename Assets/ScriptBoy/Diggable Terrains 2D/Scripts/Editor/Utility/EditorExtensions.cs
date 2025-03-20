using System.Reflection;
using UnityEditor;

namespace ScriptBoy.DiggableTerrains2D
{
    static class EditorExtensions
    {
        /// <summary>
        /// SerializedProperty m_NameProp = serializedObject.FindProperty("m_Name");
        /// </summary>
        public static void FindProperties(this Editor editor)
        {
            SerializedObject serializedObject = editor.serializedObject;
            var type = editor.GetType();
            while (type != typeof(Editor))
            {
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(SerializedProperty))
                    {
                        string name = field.Name;
                        name = name.Remove(name.Length - 4);//m_NameProp => m_Name
                        field.SetValue(editor, serializedObject.FindProperty(name));
                    }
                }

                type = type.BaseType;
            }
        }
    }
}