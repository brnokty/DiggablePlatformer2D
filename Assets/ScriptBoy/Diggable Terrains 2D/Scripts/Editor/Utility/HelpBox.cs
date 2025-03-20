using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ScriptBoy.DiggableTerrains2D
{
    static class HelpBox
    {
        static GUIContent s_Content;
        static GUIStyle s_Style;
        static List<int> s_IDList;

        static bool GetState(int controlID)
        {
            return s_IDList.Contains(controlID);
        }

        static void SetState(int controlID, bool state)
        {
            if (state)
            {
                s_IDList.Add(controlID);
            }
            else
            {
                s_IDList.Remove(controlID);
            }
        }

        static HelpBox()
        {
            s_Content = EditorGUIUtility.IconContent("console.infoicon");
            s_Style = new GUIStyle(EditorStyles.helpBox);
            s_Style.richText = true;
            s_Style.fontSize = (int)(s_Style.fontSize * 1.4f);
            s_IDList = new List<int>();
        }

        public static void Draw(string text, int id)
        {
            bool state = GetState(id);

            s_Content.text = state ? text : "Open Help Box";
            s_Style.alignment = state ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            //s_Style.fixedHeight = state ? 0 : 30;

            EditorGUI.BeginChangeCheck();
            state = GUILayout.Toggle(state, s_Content, s_Style);
            if (EditorGUI.EndChangeCheck())
            {
                SetState(id, state);
            }
        }
    }
}