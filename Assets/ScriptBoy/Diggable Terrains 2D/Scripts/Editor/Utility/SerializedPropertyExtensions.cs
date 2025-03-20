using System;
using System.Reflection;
using UnityEditor;

namespace ScriptBoy.DiggableTerrains2D
{
    static class SerializedPropertyExtensions
    {
        public static T GetValue<T>(this SerializedProperty property)
        {
            object value;
            if (property.IsArrayElement())
            {
                var array = property.GetParentProperty().GetValue<object>();
                var indexer = array.GetType().GetProperty("Item", new[] { typeof(int) });
                value = indexer.GetValue(array, new object[] { property.GetArrayElementIndex() });
            }
            else
            {
                value = property.GetFieldInfo().GetValue(property.serializedObject.targetObject);
            }

            return (T)value;
        }

        public static void SetValue(this SerializedProperty property, object value)
        {
            if (property.IsArrayElement())
            {
                var array = property.GetParentProperty().GetValue<object>();
                var indexer = array.GetType().GetProperty("Item", new[] { typeof(int) });
                indexer.SetValue(array, value, new object[] { property.GetArrayElementIndex() });
            }
            else
            {
                property.GetFieldInfo().SetValue(property.serializedObject.targetObject, value);
            }
        }

        public static bool HasNullElement(this SerializedProperty property)
        {
            int arraySize = property.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == null) return true;
            }
            return false;
        }

        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            string path = property.propertyPath;

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = property.serializedObject.targetObject.GetType();
            if (!path.Contains(".")) return type.GetField(path, bindingFlags);

            FieldInfo field = null;
            string[] paths = path.Split('.');
            for (int i = 0; i < paths.Length; i++)
            {
                if (type.IsArray)
                {
                    type = type.GetElementType();
                    i += 2;//skip "Array.data[n]" (xxx.Array.data[n].yyy)
                    continue;
                }

                if (type.IsGenericType)
                {
                    type = type.GetGenericArguments()[0];
                    i += 2;
                    continue;
                }

                field = type.GetField(paths[i], bindingFlags);
                if (field == null) return null;
                type = field.FieldType;
            }

            return field;
        }

        public static SerializedProperty GetParentProperty(this SerializedProperty property)
        {
            string path = property.propertyPath;

            if (path[path.Length - 1] == ']')
            {
                //xxx.parent.Array.data[n] => xxx.parent
                path = path.Remove(path.LastIndexOf(".A"));
                return property.serializedObject.FindProperty(path);
            }

            int dotIndex = path.LastIndexOf(".");

            //propertyName => No parent
            if (dotIndex == -1) return null;

            //xxx.parent.propertyName => xxx.parent
            path = path.Remove(dotIndex);
            return property.serializedObject.FindProperty(path);
        }

        public static int GetArrayElementIndex(this SerializedProperty property)
        {
            return int.Parse(property.displayName.Split(' ')[1]);
        }

        public static void AddArrayElement(this SerializedProperty property, object value)
        {
            int index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            property.serializedObject.ApplyModifiedProperties();
            property.GetArrayElementAtIndex(index).managedReferenceValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        public static bool IsArrayElement(this SerializedProperty property)
        {
            return property.propertyPath.EndsWith("]");
        }
    }
}