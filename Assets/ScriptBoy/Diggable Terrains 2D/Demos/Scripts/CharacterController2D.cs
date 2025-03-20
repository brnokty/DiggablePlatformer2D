/*
The CharacterController component is used to control the movement of the player.
*/

using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterController2D : MonoBehaviour
    {
        [SerializeField] Transform[] m_FlipTransforms;

        Rigidbody2D m_Rigidbody2D;
        Vector3 m_Velocity;

        bool m_IsFacingRight = true;
        bool m_IsGrounded;

        /// <summary>
        /// Determining which way the player is facing.
        /// </summary>
        public bool isFacingRight => m_IsFacingRight;
        /// <summary>
        /// Whether or not the player is grounded.
        /// </summary>
        public bool isGrounded => m_IsGrounded;



        int m_Grounds = 0;

        void Start()
        {
            //Get the Rigidbody2D attached to the GameObject
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
        }

        public void Move(float move)
        {
            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(move, m_Rigidbody2D.linearVelocity.y);
            // And then smoothing it out and applying it to the character
            m_Rigidbody2D.linearVelocity = Vector3.SmoothDamp(m_Rigidbody2D.linearVelocity, targetVelocity, ref m_Velocity, 0.1f);


            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !m_IsFacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && m_IsFacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }

        // Switch the way the player is facing.
        void Flip()
        {
            m_IsFacingRight = !m_IsFacingRight;
            foreach (var t in m_FlipTransforms)
            {
                Vector3 theScale = t.localScale;
                theScale.x *= -1;
                t.localScale = theScale;
            }
        }

        // Detect collision enter with ground.
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.tag == "Ground")
            {
                m_Grounds++;
            }

            m_IsGrounded = m_Grounds > 0;
        }

        // Detect collision exit with ground.
        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.tag == "Ground")
            {
                m_IsGrounded = false;
                m_Grounds--;
            }

            m_IsGrounded = m_Grounds > 0;
        }
    }
}