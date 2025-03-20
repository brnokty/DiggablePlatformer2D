using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// The Shovel component is responsible for modifying terrains in runtime.
    /// </summary>
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Shovel")]
    public sealed class Shovel : MonoBehaviour
    {
        public const float MinSimplification = 0.001f;
        public const float MaxSimplification = 0.1f;
        public const float MinWaveLength = 0.4f;
        public const float MaxWaveLength = 2f;
        public const float MinWaveAmplitude = 0.1f;
        public const float MaxWaveAmplitude = 1f;


        [SerializeField, HideInInspector] Shape2D m_Shape;
        [SerializeField, HideInInspector] float m_Simplification;
        [SerializeField, HideInInspector] bool m_EnableDemo;
        [SerializeField, HideInInspector] bool m_EnableWave;
        [SerializeField, HideInInspector] float m_WaveLength;
        [SerializeField, HideInInspector] float m_WaveAmplitude;

        /// <summary>
        /// The shape that is used to create the shove polygon.
        /// </summary>
        public Shape2D shape
        {
            get => m_Shape;
            set => m_Shape = value;
        }

        /// <summary>
        /// The simplification threshold.
        /// </summary>
        public float simplification
        {
            get => m_Simplification;
            set => m_Simplification = Mathf.Max(value, MinSimplification);
        }

        /// <summary>
        /// Enables a quick demo for testing the dig function.
        /// </summary>
        public bool enableDemo
        {
            get => m_EnableDemo;
            set => m_EnableDemo = value;
        }

        /// <summary>
        /// Enables an effect that randomizes the shovel polygon.
        /// </summary>
        public bool enableWave
        {
            get => m_EnableWave;
            set => m_EnableWave = value;
        }

        /// <summary>
        /// The length of the wave.
        /// </summary>
        public float waveLength
        {
            get => m_WaveLength;
            set => m_WaveLength = Mathf.Clamp(value, MinWaveLength, MaxWaveLength);
        }


        /// <summary>
        /// The amplitude of the wave.
        /// </summary>
        public float waveAmplitude
        {
            get => m_WaveAmplitude;
            set => m_WaveAmplitude = Mathf.Clamp(value, MinWaveAmplitude, MaxWaveAmplitude);
        }


        public Shovel()
        {
            simplification = 0.001f;
            waveLength = 1;
            waveAmplitude = 0.5f;
        }

        /// <summary>
        /// Returns the shovel polygon in the world space.
        /// </summary>
        public Vector2[] GetPolygon()
        {
            Vector2[] polygon = m_Shape.GetWorldPoints();
            if (m_EnableWave && (m_Shape is CircleShape2D || m_Shape is BoxShape2D))
            {
                polygon = PolygonUtility.Remesh(polygon, m_WaveLength / 5f, true);
                polygon = PolygonUtility.Wave(polygon, m_WaveLength, m_WaveAmplitude, 0);
            }
            polygon = PolygonSimplifier.Simplify(polygon, m_Simplification, true);
            return polygon;
        }

        /// <summary>
        /// Removes the area within the shove polygon and returns false if it fails. 
        /// </summary>
        public bool Dig()
        {
            return Dig(out float dugArea);
        }

        /// <summary>
        /// Removes the area within the shove polygon and returns false if it fails. 
        /// </summary>
        /// <param name="dugArea">The amount of dug area.</param>
        public bool Dig(out float dugArea)
        {
            return EditTerrains(false, out dugArea);
        }

        /// <summary>
        /// Removes the area within the shove polygon and returns false if it fails. 
        /// </summary>
        /// <param name="dugArea">The amount of dug area.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public bool Dig(out float dugArea, int layerMask)
        {
            return EditTerrains(false, out dugArea, layerMask);
        }


        /// <summary>
        /// Fills the area within the shove polygon and returns false if it fails. 
        /// </summary>
        public bool Fill()
        {
            return Fill(out float filledArea);
        }

        /// <summary>
        /// Fills the area within the shove polygon and returns false if it fails. 
        /// </summary>
        /// <param name="filledArea">The amount of filled area.</param>
        public bool Fill(out float filledArea)
        {
            return EditTerrains(true, out filledArea);
        }

        /// <summary>
        /// Fills the area within the shove polygon and returns false if it fails. 
        /// </summary>
        /// <param name="filledArea">The amount of filled area.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public bool Fill(out float filledArea, int layerMask)
        {
            return EditTerrains(true, out filledArea, layerMask);
        }


        bool EditTerrains(bool fill)
        {
            return EditTerrains(fill, out float modifiedArea);
        }

        bool EditTerrains(bool fill, out float modifiedArea)
        {
            return EditTerrains(fill, out modifiedArea, -1);
        }

        bool EditTerrains(bool fill, out float modifiedArea, int layerMask)
        {
            modifiedArea = 0;
      
            if (m_Shape != null)
            {
                if (IsCircle())
                {
                    CircleShape2D c = m_Shape as CircleShape2D;
                    float radius = c.radius * c.transform.lossyScale.x * transform.lossyScale.x;
                    Vector2 position = c.transform.position + transform.position;

                    var vTerrains = VoxelTerrain2D.FindByMask(layerMask, false);
                    if (vTerrains.Length > 0)
                    {
                        foreach (var terrain in vTerrains)
                        {
                            modifiedArea += terrain.EditByCircle(position, radius, fill);
                        }
                    }

                    var pTerrains = PolygonTerrain2D.FindByMask(layerMask, false);
                    if (pTerrains.Length > 0)
                    {
                        var polygon = GetPolygon();
                        foreach (var terrain in pTerrains)
                        {
                            modifiedArea += terrain.EditByPolygon(polygon, fill);
                        }
                    }
                }
                else
                {
                    var terrains = Terrain2D.FindByMask(layerMask, false);
                    var polygon = GetPolygon();
                    if (fill && terrains.Length > 1)
                    {
                        Debug.LogError("The 'Fill' function is not supported when there are multiple terrain components in the scene.");
                        return false;
                    }

                    foreach (var terrain in terrains)
                    {
                        modifiedArea += terrain.EditByPolygon(polygon, fill);
                    }
                }
            }
            return modifiedArea > 0;
        }


        bool IsCircle()
        {
            if (m_EnableWave) return false;
            if (m_Shape is CircleShape2D == false) return false;
            if ((m_Shape as CircleShape2D).pointCount >= 20) return false;

            Vector2 a = m_Shape.transform.lossyScale;
            Vector2 b = transform.lossyScale;

            return Mathf.Abs(a.x * b.x) == Mathf.Abs(a.y * b.y);
        }


        void Update()
        {
            if (!m_EnableDemo) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector2 m = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = m;

            if (Input.GetMouseButton(0)) Dig();
            if (Input.GetMouseButton(1)) Fill();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (m_Shape == null) return;

            if (!GizmosUtility.IsSelected(gameObject))
            {
                if (!GizmosUtility.IsChildSelected(transform)) return;
            }

            GizmosUtility.DrawPolygon(GetPolygon(), Color.yellow);
        }
#endif
    }
}