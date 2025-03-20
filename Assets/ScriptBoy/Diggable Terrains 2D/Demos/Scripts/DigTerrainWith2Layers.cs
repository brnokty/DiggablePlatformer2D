/*
The DigTerrainWith2Layers component is used in the "04 Layers" demo scene.
The TerrainParticleUtility class is used to get a list of TerrainParticle which contains position and color.
*/

using System.Collections.Generic;
using UnityEngine;
using ScriptBoy.DiggableTerrains2D;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [RequireComponent(typeof(Shovel))]
    public class DigTerrainWith2Layers : MonoBehaviour
    {
        [SerializeField] Shovel m_Shovel;

        [Space]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_DigSound;

        [Space]
        [SerializeField] ParticleSystem m_Dirt;
        [SerializeField] int m_DirtParticleCount;
        [SerializeField] int m_DirtParticleSpeed;

        List<TerrainParticle> m_Particles = new List<TerrainParticle>();

        static int s_DirtParticleSeed;

        float m_DigSoundDelay;

        void Start()
        {
            m_Dirt.Play();
        }

        void Update()
        {
            transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButton(0))
            {
                Dig();
            }
        }

        void Dig()
        {
            TerrainParticleUtility.GetParticles(m_Shovel, m_Particles, m_DirtParticleCount, m_DirtParticleSpeed);

            if (m_Shovel.Dig(out float diggedArea) && diggedArea > 0.01f)
            {
                s_DirtParticleSeed++;
                int n = m_Particles.Count;
                for (int i = 0; i < n; i++)
                {
                    TerrainParticle particle = m_Particles[i];

                    ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                    emit.position = particle.position;
                    emit.velocity = Random.insideUnitCircle * m_DirtParticleSpeed;
                    emit.startColor = particle.color;
                    m_Dirt.Emit(emit, 1);
                }

                if (m_DigSoundDelay < 0)
                {
                    m_AudioSource.pitch = Random.Range(0.8f, 1.2f);
                    m_AudioSource.PlayOneShot(m_DigSound, Random.Range(0.2f, 0.5f));
                    m_DigSoundDelay = 0.1f;
                }
            }

            m_DigSoundDelay -= Time.deltaTime;
        }
    }
}