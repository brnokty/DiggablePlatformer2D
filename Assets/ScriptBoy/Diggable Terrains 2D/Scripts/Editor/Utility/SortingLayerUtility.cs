using UnityEditor;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    static class SortingLayerUtility
    {
        static class GUIContents
        {
            public readonly static GUIContent sortingLayer = new GUIContent("Sorting Layer",
                "Name of the Renderer's sorting layer.");

            public readonly static GUIContent sortingOrder = new GUIContent("Order in Layer",
                "Renderer's order within a sorting layer.");
        }

        public static void RenderSortingLayerFields(SerializedProperty sortingOrder, SerializedProperty sortingLayer)
        {
            SortingLayerField(GUIContents.sortingLayer, sortingLayer);
            EditorGUILayout.PropertyField(sortingOrder, GUIContents.sortingOrder);
        }

        static void SortingLayerField(GUIContent label, SerializedProperty layerID)
        {
            SortingLayer[] layers = SortingLayer.layers;
            string[] names = new string[layers.Length];
            int selectedIndex = 0;
            int id = layerID.intValue;
            for (int i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i].name;
                if (id == layers[i].id) selectedIndex = i;
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup(label, selectedIndex, names);
            if (EditorGUI.EndChangeCheck())
            {
                id = layers[selectedIndex].id;
                layerID.intValue = id;
            }
        }
    }
}