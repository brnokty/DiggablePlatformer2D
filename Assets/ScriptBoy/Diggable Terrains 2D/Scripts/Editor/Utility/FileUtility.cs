using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    static class FileUtility
    {
        public static string GetRelativePath(string path)
        {
            return FileUtil.GetProjectRelativePath(path);
        }

        public static string GetAbsolutePath(string path)
        {
            return Application.dataPath + path.Remove(0, 6);
        }
    }
}