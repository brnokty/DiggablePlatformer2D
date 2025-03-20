/*
In this example, the DigByShovel and DigByPickaxe functions are created but not used by scripts. 
Instead, they will be called by animation events to synchronize with the player's animation.
 */

using UnityEngine;
using ScriptBoy.DiggableTerrains2D;
using System.Collections.Generic;

namespace ScriptBoy.DiggableTerrains2D_Demos
{
    [ExecuteInEditMode]
    //[RequireComponent(typeof(Animator), typeof(CharacterController), typeof(AudioSource))]
    public class Player : MonoBehaviour
    {
        [Space]
        [SerializeField] Shovel m_Shovel;
        [SerializeField] ParticleSystem m_ShovelParticleSystem;


        [SerializeField] Shovel m_Pickaxe;
        [SerializeField] ParticleSystem m_PickaxeParticleSystem;

        [SerializeField, HideInInspector] bool m_IsDemo9;

        [Space]
        [SerializeField] AudioClip m_DigSound;
        [SerializeField] AudioClip m_FootstepSound;
        [SerializeField] AudioClip m_RocketSound;

        [Space]
        [SerializeField] float m_RunSpeed;

        [Space]
        [SerializeField] Transform m_RocketAimPivot;
        [SerializeField] GameObject m_RocketPivot;
        [SerializeField] GameObject m_RocketPrefab;
        [SerializeField] float m_RocketColdownDuration;

        [Space]
        [SerializeField] Limb m_LeftArm;
        [SerializeField] Limb m_RightArm;

        [Space]
        [SerializeField] State m_NormalState;
        [SerializeField] State m_ShovelState;
        [SerializeField] State m_PickaxeState;
        [SerializeField] State m_RocketState;

        CharacterController2D m_CharacterController;
        AudioSource m_AudioSource;
        Animator m_Animator;
        State m_CurrentState;

        float m_Run;
        bool m_IsAimming;
        float m_AimAngle;
        float m_AimWeight;
        float m_Coldown;


        //This function will be called by an animation event.
        void DigByShovel()
        {
            DigAndEmitParticles(m_Shovel, m_ShovelParticleSystem);
        }

        //This function will be called by an animation event.
        void DigByPickaxe()
        {
            DigAndEmitParticles(m_Pickaxe, m_PickaxeParticleSystem);
        }

        //This function will be called by an animation event.
        void PlayFootstepSound()
        {
            m_AudioSource.pitch = Random.Range(0.5f, 1f);
            m_AudioSource.PlayOneShot(m_FootstepSound, Random.Range(0.05f, 0.1f));
        }


        void DigAndEmitParticles(Shovel shovel, ParticleSystem particleSystem)
        {
            if (m_IsDemo9)
            {
                List<TerrainParticle> particles = new List<TerrainParticle>();
                TerrainParticleUtility.GetParticles(shovel, particles, 200);

                if (shovel.Dig(out float diggedArea))
                {
                    Vector3 velocity = particleSystem.transform.right * particleSystem.transform.lossyScale.x * 10;

                    for (int i = 0; i < particles.Count; i++)
                    {
                        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                        emit.position = particles[i].position;
                        emit.startColor = particles[i].color;
                        emit.velocity = velocity;
                        particleSystem.Emit(emit, 1);
                    }
                    
                    if (diggedArea > 2) PlayDigSound();
                }
            }
            else
            {
                if (shovel.Dig(out float diggedArea))
                {
                    int particleCount = (int)(100f * Mathf.InverseLerp(2, 15, diggedArea));
                    particleSystem.Emit(particleCount);

                    if (diggedArea > 2) PlayDigSound();
                }
            }
        }

        void PlayDigSound()
        {
            m_AudioSource.pitch = Random.Range(0.8f, 1.2f);
            m_AudioSource.PlayOneShot(m_DigSound, Random.Range(0.2f, 0.5f));
        }

        void PlayFireSound()
        {
            m_AudioSource.pitch = Random.Range(0.8f, 1.2f);
            m_AudioSource.PlayOneShot(m_RocketSound, Random.Range(0.75f, 1));
        }






        void Awake()
        {
            //Getting components attached to the gameObject
            m_Animator = GetComponent<Animator>();
            m_CharacterController = GetComponent<CharacterController2D>();
            m_AudioSource = GetComponent<AudioSource>();

            Physics2D.IgnoreLayerCollision(6, 7);//Ignore Player & Bones Collision
        }

        void OnEnable()
        {
            //Setting player state
            m_ShovelState.weapon.SetActive(false);
            m_PickaxeState.weapon.SetActive(false);
            m_RocketState.weapon.SetActive(false);
            ChangeState(m_NormalState);
        }

        void Update()
        {
            if (!Application.isPlaying) return;

            UpdateRunAnimation();
            UpdateDigAnimation();
            UpdateAimAnimation();
        }

        void UpdateRunAnimation()
        {
            if (m_CharacterController.isGrounded)
            {
                float horizontalAxis = Input.GetAxis("Horizontal");
                m_Run = Mathf.Abs(horizontalAxis);
                m_Animator.SetFloat("Run", m_Run);
                m_CharacterController.Move(horizontalAxis * m_RunSpeed);
            }
            else
            {
                m_Run = Mathf.Lerp(m_Run, 0, Time.deltaTime);
                m_Animator.SetFloat("Run", m_Run);
            }
        }

        void UpdateDigAnimation()
        {
            if (m_IsAimming) return;
            if (!m_CharacterController.isGrounded) return;

            if (Input.GetMouseButtonDown(1))
            {
                m_Animator.SetTrigger("Shovel");
                ChangeState(m_ShovelState);
            }

            if (Input.GetMouseButtonDown(0))
            {
                m_Animator.SetTrigger("Pickaxe");
                ChangeState(m_PickaxeState);
            }
        }

        void UpdateAimAnimation()
        {
            if (Input.GetMouseButtonDown(2))
            {
                ChangeState(m_RocketState);
                m_Animator.SetLayerWeight(1, 1);
                m_IsAimming = true;
            }

            if (Input.GetMouseButtonUp(2))
            {
                ChangeState(m_NormalState);
                m_IsAimming = false;
            }

            if (m_IsAimming)
            {
                if (m_Coldown == 0 && Input.GetMouseButton(0))
                {
                    FireRocket();
                }

                Vector2 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 p = m_RocketAimPivot.position;
                Vector2 dir = m - p;

                if (!m_CharacterController.isFacingRight)
                {
                    dir.x = -dir.x;
                }

                float angle = Vector2.SignedAngle(Vector2.right, dir);

                m_AimAngle = Mathf.Lerp(m_AimAngle, angle, Time.deltaTime * 10);

                m_Animator.SetFloat("Aim", m_AimAngle);

                if (m_AimWeight < 1)
                {
                    m_AimWeight = Mathf.Lerp(m_AimWeight, 1, Time.deltaTime * 10);
                    m_Animator.SetLayerWeight(1, m_AimWeight);
                }
            }
            else if (m_AimWeight > 0)
            {
                m_AimWeight = Mathf.Lerp(m_AimWeight, 0, Time.deltaTime * 10);
                m_Animator.SetLayerWeight(1, m_AimWeight);
            }

            if (m_Coldown != 0)
            {
                m_Coldown -= Time.deltaTime;
                if (m_Coldown < 0)
                {
                    m_Coldown = 0;
                    m_RocketPivot.SetActive(true);
                }
            }
        }

        void FireRocket()
        {
            m_RocketPivot.SetActive(false);

            Vector3 position = m_RocketPivot.transform.position;
            Quaternion rotation = m_RocketPivot.transform.rotation;
            if (!m_CharacterController.isFacingRight)
            {
                rotation *= Quaternion.Euler(0, 0, 180);
            }
            Instantiate(m_RocketPrefab, position, rotation);


            PlayFireSound();

            m_Coldown = m_RocketColdownDuration;
        }


        void ChangeState(State state)
        {
            if (m_CurrentState != null && m_CurrentState != state)
            {
                if (m_CurrentState.weapon != null)
                {
                    m_CurrentState.weapon.SetActive(false);
                }
            }

            if (state.weapon != null)
            {
                state.weapon.SetActive(true);
            }

            m_RightArm.b = state.rightForearmTarget;
            m_RightArm.c = state.rightHandTarget;
            m_LeftArm.b = state.leftForearmTarget;
            m_LeftArm.c = state.leftHandTarget;

            m_CurrentState = state;
        }


        //These functions help to change player state in the editor.
        //[EButton.BeginHorizontal, EButton]
        [ContextMenu("Normal_State")]
        void Normal_State() => ChangeState(m_NormalState);
        //[EButton]
        [ContextMenu("Shovel_State")]
        void Shovel_State() => ChangeState(m_ShovelState);
        //[EButton]
        [ContextMenu("Pickaxe_State")]
        void Pickaxe_State() => ChangeState(m_PickaxeState);
        //[EButton, EButton.EndHorizontal]
        [ContextMenu("Rocket_State")]
        void Rocket_State() => ChangeState(m_RocketState);


        //This class is used to change player state.
        [System.Serializable]
        class State
        {
            public Transform rightForearmTarget;
            public Transform rightHandTarget;
            public Transform leftForearmTarget;
            public Transform leftHandTarget;

            public GameObject weapon;
        }
    }
}