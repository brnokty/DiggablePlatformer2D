/*
This is a simple example of the Dig function. It removes the terrain area that is inside the shovel polygon.
 */

using UnityEngine;
using ScriptBoy.DiggableTerrains2D;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [RequireComponent(typeof(Shovel))]
    public class DigTerrain : MonoBehaviour
    {
        [SerializeField] Shovel m_Shovel;

        void Update()
        {
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButton(0))
            {
                m_Shovel.Dig();
            }
        }
    }
}