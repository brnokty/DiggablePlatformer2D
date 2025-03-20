/*
This is a simple example of the Fill function. It is a reversed version of the dig function and adds terrain area instead of removing it.
*/

using UnityEngine;
using ScriptBoy.DiggableTerrains2D;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [RequireComponent(typeof(Shovel))]
    public class FillTerrain : MonoBehaviour
    {
        [SerializeField] Shovel m_Shovel;

        void Update()
        {
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButton(0))
            {
                m_Shovel.Fill();
            }
        }
    }
}