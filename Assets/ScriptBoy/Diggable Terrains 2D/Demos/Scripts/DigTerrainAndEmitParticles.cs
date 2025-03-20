/*
This is a simple example of the Dig function that shows how to play sound and emit particles.
*/

using UnityEngine;
using ScriptBoy.DiggableTerrains2D;
using System.Collections.Generic;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [RequireComponent(typeof(Shovel))]
    public class DigTerrainAndEmitParticles : MonoBehaviour
    {
        [SerializeField] Shovel m_Shovel;

        [Space]
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_DigSound;

        [Space]
        [SerializeField] ParticleSystem m_Dirt;
        [SerializeField] int m_DirtParticleCount;

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
            if (m_Shovel.Dig(out float diggedArea) && diggedArea > 0.01f)
            {
                if (m_DigSoundDelay < 0)
                {
                    m_DigSoundDelay = 0.15f;
                    m_AudioSource.pitch = Random.Range(0.8f, 1.2f);
                    m_AudioSource.PlayOneShot(m_DigSound, Random.Range(0.2f, 0.5f));
                }

                int particleCount = (int)(Mathf.InverseLerp(0, 2f, diggedArea) * m_DirtParticleCount);
                m_Dirt.Emit(particleCount);
            }

            m_DigSoundDelay -= Time.deltaTime;
        }
    }
}