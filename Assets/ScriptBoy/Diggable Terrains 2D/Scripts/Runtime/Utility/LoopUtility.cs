using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for iterating.
    /// </summary>
    public static class LoopUtility
    {
        /// <summary>
        /// Returns the next index.
        /// </summary>
        public static int NextIndex(int index, int arrayLength)
        {
            index++;
            if (index == arrayLength) return 0;
            return index;
        }

        /// <summary>
        /// Returns the previous index.
        /// </summary>
        public static int PrevIndex(int index, int arrayLength)
        {
            if (index == 0) return arrayLength - 1;
            return index - 1;
        }

        /// <summary>
        /// Loops the given index within the array length.
        /// </summary>
        public static int LoopIndex(int index, int arrayLength)
        {
            return index - Mathf.FloorToInt((float)index / arrayLength) * arrayLength;
        }














        /// <summary>
        /// Returns the next index.
        /// </summary>
        public static int NextIndex<T>(int index, T[] array)
        {
            return NextIndex(index, array.Length);
        }

        /// <summary>
        /// Returns the previous index.
        /// </summary>
        public static int PrevIndex<T>(int index, T[] array)
        {
            return PrevIndex(index, array.Length);
        }

        /// <summary>
        /// Loops the given index within the array length.
        /// </summary>
        public static int LoopIndex<T>(int index, T[] array)
        {
            return LoopIndex(index, array.Length);
        }

        /// <summary>
        /// Returns the next index.
        /// </summary>
        public static int NextIndex<T>(int index, List<T> list)
        {
            return NextIndex(index, list.Count);
        }

        /// <summary>
        /// Returns the previous index.
        /// </summary>
        public static int PrevIndex<T>(int index, List<T> list)
        {
            return PrevIndex(index, list.Count);
        }

        /// <summary>
        /// Loops the given index within the list count.
        /// </summary>
        public static int LoopIndex<T>(int index, List<T> list)
        {
            return LoopIndex(index, list.Count);
        }
    }
}