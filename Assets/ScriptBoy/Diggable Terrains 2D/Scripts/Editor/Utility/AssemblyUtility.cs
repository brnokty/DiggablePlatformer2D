using System;
using System.Collections.Generic;
using System.Reflection;

namespace ScriptBoy.DiggableTerrains2D
{
    static class AssemblyUtility
    {
        public static List<Type> FindSubclassOf(Type baseType)
        {
            var assembly = Assembly.GetAssembly(baseType);

            List<Type> types = new List<Type>();

            foreach (var type in assembly.GetTypes())
            {
                if (type == baseType) continue;
                if (!type.IsSubclassOf(baseType)) continue;
                types.Add(type);
            }
            return types;
        }


        public static List<Type> FindSubclassOf<T>()
        {
            return FindSubclassOf(typeof(T));
        }
    }
}