/*
The Rocket component is used to control the fired projectile. 
It checks for collision events and digs if the rocket hits terrain colliders. 
It does not use the Shovel component and edits terrains directly.
 */


using ScriptBoy.DiggableTerrains2D;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    public class Rocket : MonoBehaviour
    {
        [SerializeField] float m_DestructionRadius;

        [SerializeField] Rigidbody2D m_Body;
        [SerializeField] Collider2D m_Collider;
        [SerializeField] ParticleSystem m_Trail;
        [SerializeField] ParticleSystem m_Dirt;
        [SerializeField] SpriteRenderer m_SpriteRenderer;
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_HitSound;

        [SerializeField] float m_Lifetime;
        [SerializeField] float m_Force;

        [SerializeField] int m_DirtCount;
        [SerializeField] float m_DirtForce;

        static List<Vector2> s_DirtParticles = new List<Vector2>();
        static int s_DirtParticleSeed;
        bool m_IsExploded;
        Vector2 m_CollisionNormal;

        void Start()
        {
            Destroy(gameObject, m_Lifetime);
        }

        //Checking for collision events and starting to dig if the rocket hits the terrain.
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (m_IsExploded) return;

            if (collision.gameObject.tag == "Ground")
            {
                m_CollisionNormal = collision.GetContact(0).normal;
                m_Collider.enabled = false;
                SpawnDirtParticles();
                DigTerrains();
                m_Trail.Stop();
                m_Body.bodyType = RigidbodyType2D.Static;
                m_SpriteRenderer.enabled = false;
                m_IsExploded = true;
                m_AudioSource.Stop();
                m_AudioSource.PlayOneShot(m_HitSound);
            }
        }

        //Digging all terrains inside the destruction circle.
        void DigTerrains()
        {
            foreach (var terrain in Terrain2D.activeInstances)
            {
                terrain.DigCircle(transform.position, m_DestructionRadius);
            }
        }

        //Emitting particles.
        void SpawnDirtParticles()
        {
            List<Vector2> particles = GetDirtParticles();
            s_DirtParticleSeed++;
            int count = particles.Count;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                emit.position = particles[i];
                emit.velocity = m_CollisionNormal * m_DirtForce;
                m_Dirt.Emit(emit, 1);
            }
        }

        List<Vector2> GetDirtParticles()
        {
            TerrainParticleUtility.GetParticles(transform.position, m_DestructionRadius, s_DirtParticles, m_DirtCount, s_DirtParticleSeed);
            return s_DirtParticles;
        }

        void Update()
        {
            UpdateTrailRotation();
            if (!m_IsExploded) return;

            if (m_Trail.particleCount == 0 && m_Dirt.particleCount == 0)
            {
                Destroy(gameObject);
            }
        }

        void UpdateTrailRotation()
        {
            var startRotation = m_Trail.main.startRotation;
            startRotation.constant = -transform.eulerAngles.z * Mathf.Deg2Rad;
            var main = m_Trail.main;
            main.startRotation = startRotation;
        }

        void FixedUpdate()
        {
            if (m_IsExploded) return;
            m_Body.AddRelativeForce(new Vector2(m_Force, 0), ForceMode2D.Force);
        }

        void OnDrawGizmosSelected()
        {
            //Visualizing the destruction circle.
            Gizmos.DrawWireSphere(transform.position, m_DestructionRadius);

            //Visualizing the generated positions for the dirt particles.
            foreach (var p in GetDirtParticles())
            {
                Gizmos.DrawSphere(p, 0.05f);
            }
        }
    }
}