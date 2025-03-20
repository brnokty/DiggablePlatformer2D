/*
The Underground component is used to change the state of underground items.
It checks the OnTriggerExit2D events and activates the rigidbody if the player digs the surrounding area of the item. 
*/

using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    public class Underground : MonoBehaviour
    {
        [SerializeField] Rigidbody2D m_Body;
        [SerializeField] Collider2D m_Collider;
        [SerializeField] SpriteRenderer m_SpriteRenderer;
        [SerializeField] int m_UndergroundSortingOrder;
        [SerializeField] int m_OvergroundSortingOrder;
        [SerializeField] LayerMask m_TerrainLayerMask;

        void Start()
        {
            SetUndergroundState(true);
        }

        void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Ground"))
            {
                if (!m_Collider.IsTouchingLayers(m_TerrainLayerMask))//If you are only using voxel terrains, this check is not needed.
                {
                    SetUndergroundState(false);
                }
            }
        }

        void SetUndergroundState(bool isUnderground)
        {
            m_Body.isKinematic = isUnderground;
            m_Collider.isTrigger = isUnderground;
            m_SpriteRenderer.sortingOrder = isUnderground ? m_UndergroundSortingOrder : m_OvergroundSortingOrder;

            if (!isUnderground) Destroy(this);
        }
    }
}