/*
This is a simple example of saving and loading terrain.
*/

using UnityEngine;
using ScriptBoy.DiggableTerrains2D;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    public class SaveAndLoadTerrain : MonoBehaviour
    {
        [SerializeField] Terrain2D m_Terrain;
        [SerializeField] string m_FileName;

        string filePath => Application.persistentDataPath + "/" + m_FileName;

        void Save()
        {
            m_Terrain.Save(filePath);
        }

        void Load()
        {
            m_Terrain.Load(filePath);
        }

        void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(5, 5, 100, 100)))
            {
                if (GUILayout.Button("Save")) Save();
                if (GUILayout.Button("Load")) Load();
            }
        }
    }
}