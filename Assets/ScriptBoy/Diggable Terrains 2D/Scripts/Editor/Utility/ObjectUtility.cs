using UnityEditor;

namespace ScriptBoy.DiggableTerrains2D
{
    static class ObjectUtility
    {
        public static object Clone(object obj)
        {
            return CloneObject(obj);
        }

        public static T Clone<T>(T obj)
        {
            return (T)CloneObject(obj);
        }

        static object CloneObject(object obj)
        {
            string json = EditorJsonUtility.ToJson(obj);
            obj = System.Activator.CreateInstance(obj.GetType());
            EditorJsonUtility.FromJsonOverwrite(json, obj);
            return obj;
        }
    }
}