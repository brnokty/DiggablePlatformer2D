using UnityEngine;
using UnityEditor;

namespace ScriptBoy.DiggableTerrains2D
{
    static class EditModeButton
    {
        static GUIContent s_Content;
        static GUIStyle s_Style;

        static EditModeButton()
        {
            s_Content = EditorGUIUtility.IconContent("d_EditCollider");
            s_Content = new GUIContent();
            s_Content.text = "  Edit Mode";
            s_Style = new GUIStyle(GUI.skin.button);
            s_Style.fontSize = 15;
            s_Style.fontStyle = FontStyle.Bold;
            s_Style.fixedHeight = 30;
        }

        public static void Draw(ref bool editMode)
        {
            editMode = GUILayout.Toggle(editMode, s_Content, s_Style);
        }

        public static void Draw(ref bool editMode, string lable)
        {
            s_Content.text = "  " + lable;
            editMode = GUILayout.Toggle(editMode, s_Content, s_Style);
        }
    }
}