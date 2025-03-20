using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for 2D and 3D vectors.
    /// </summary>
    public static class VectorUtility
    {
        /// <summary>
        /// Converts 3D vectors to 2D vectors.
        /// </summary>
        public static Vector3[] ConvertToVector3(Vector2[] vectors)
        {
            int n = vectors.Length;
            Vector3[] output = new Vector3[n];
            for (int i = 0; i < n; i++) output[i] = vectors[i];
            return output;
        }

        /// <summary>
        /// Converts 2D vectors to 3D vectors.
        /// </summary>
        public static Vector2[] ConvertToVector2(Vector3[] vectors)
        {
            int n = vectors.Length;
            Vector2[] output = new Vector2[n];
            for (int i = 0; i < n; i++) output[i] = vectors[i];
            return output;
        }


        /// <summary>
        /// Returns perpendicular of the given vector.
        /// </summary>
        public static Vector2 GetNormal(Vector2 vector)
        {
            return new Vector2(-vector.y, vector.x);
        }

        /// <summary>
        /// Creates an array of vectors, where each element is set to the given vector.
        /// </summary>
        internal static Vector3[] CreateArray(Vector3 vector, int count)
        {
            Vector3[] array = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = vector;
            }
            return array;
        }
    }
}