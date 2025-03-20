using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// This is an abstract base class for the PolygonTerrain2D and VoxelTerrain2D.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [ExecuteInEditMode]
    public abstract class Terrain2D : MonoBehaviour
    {
        static HashSet<Terrain2D> s_Instances = new HashSet<Terrain2D>();

        /// <summary>
        /// Returns an array of Terrain2D objects.
        /// </summary>
        public static Terrain2D[] instances
        {
            get => s_Instances.ToArray();
        }

        /// <summary>
        /// Returns an array of active Terrain2D objects.
        /// </summary>
        public static Terrain2D[] activeInstances
        {
            get => FindByFilter<Terrain2D>(null, false);
        }

        /// <summary>
        /// Finds and returns an array of Terrain2D objects based on a layer mask.
        /// </summary>
        public static Terrain2D[] FindByMask(int layerMask, bool includeInactive)
        {
            return FindByMask<Terrain2D>(layerMask, includeInactive);
        }

        /// <summary>
        /// Finds and returns an array of Terrain2D objects based on a tag.
        /// </summary>
        public static Terrain2D[] FindByTag(string tag, bool includeInactive)
        {
            return FindByTag<Terrain2D>(tag, includeInactive);
        }

        protected static T[] FindByMask<T>(int layerMask, bool includeInactive) where T : Terrain2D
        {
            return FindByFilter<T>((terrain) =>
            {
                return layerMask == -1 || (layerMask & (1 << terrain.gameObject.layer)) != 0;
            }, includeInactive);
        }

        protected static T[] FindByTag<T>(string tag, bool includeInactive) where T : Terrain2D
        {
            return FindByFilter<T>((terrain) =>
            {
                return terrain.CompareTag(tag);
            }, includeInactive);
        }

        protected static T[] FindByFilter<T>(Func<T, bool> filter, bool includeInactive) where T : Terrain2D
        {
            List<T> terrains = new List<T>(s_Instances.Count);
            foreach (var instance in s_Instances)
            {
                if (instance is T)
                {
                    T terrain = instance as T;
                    if (includeInactive || terrain.isActiveAndEnabled)
                    {
                        if (filter == null || filter(terrain))
                        {
                            terrains.Add(terrain);
                        }
                    }
                }
            }
            return terrains.ToArray();
        }

        public const float MinSimplification = 0.001f;
        public const float MaxSimplification = 0.1f;
        public const float MinColliderOffset = 0;
        public const float MaxColliderOffset = 1;
        public const float MinEdgeHeight = 0;
        public const float MaxEdgeHeight = 1;
        public const float MinEdgeOffset = -0.25f;
        public const float MaxEdgeOffset = 0.25f;

        [SerializeField, HideInInspector] protected bool m_BuildOnAwake = true;
        [SerializeField, HideInInspector] protected float m_Simplification;
        [SerializeField, HideInInspector] protected int m_SortingLayerID;
        [SerializeField, HideInInspector] protected int m_SortingOrder;
        [SerializeField, HideInInspector] protected float m_EdgeHeight;
        [SerializeField, HideInInspector] protected float m_EdgeOffset;
        [SerializeField, HideInInspector] private protected CornerType m_EdgeCornerType;
        [SerializeField, HideInInspector] private protected EdgeUVMapping m_EdgeUVMapping;
        [SerializeField, HideInInspector] protected float m_ColliderOffset;
        [SerializeField, HideInInspector] protected bool m_ColliderDelaunay;
        [SerializeField, HideInInspector] TerrainLayer[] m_Layers;
        [SerializeField, HideInInspector] Texture2D m_SplatMapTexture;
        [SerializeField, HideInInspector] Rect m_SplatMapUVRect;
        [SerializeField, HideInInspector] bool m_UseDefaultCheckerTexture;
        [SerializeField, HideInInspector] bool m_IsDiggable = true;
        [SerializeField, HideInInspector] bool m_IsFillable;

        [SerializeField, HideInInspector] BinaryCompressionMethod m_CompressionMethod;
        [SerializeField, HideInInspector] BinaryCompressionLevel m_CompressionLevel;
        [SerializeField, HideInInspector] bool m_IncludeSplatMapInSave;
        [SerializeField, HideInInspector] bool m_CompressSplatMapInSave;

        [NonSerialized] SortingGroup m_SortingGroup;
        [NonSerialized] Material[] m_Materials;
        [NonSerialized] MaterialPropertyBlock[] m_MaterialPropertyBlocks;
        [NonSerialized] bool m_IsBuilt;
        [NonSerialized] Color[] m_AverageColors;
        [NonSerialized] bool m_IsSplatMapPainted;

        public bool isBuilt => m_IsBuilt;

        /// <summary>
        /// Is the terrain diggable?
        /// </summary>
        public bool isDiggable
        {
            get => m_IsDiggable;
        }

        /// <summary>
        /// Is the terrain fillable?
        /// </summary>
        public bool isFillable
        {
            get => m_IsFillable;
        }

        /// <summary>
        /// Compression method used when saving data. 
        /// </summary>
        public BinaryCompressionMethod compressionMethod
        {
            get => m_CompressionMethod;
            set => m_CompressionMethod = value;
        }
        /// <summary>
        /// Compression level used when saving data. 
        /// </summary>
        public BinaryCompressionLevel compressionLevel
        {
            get => m_CompressionLevel;
            set => m_CompressionLevel = value;
        }

        /// <summary>
        /// Whether to include the splat map in the save data. 
        /// </summary>
        public bool includeSplatMapInSave
        {
            get => m_IncludeSplatMapInSave;
            set => m_IncludeSplatMapInSave = value;
        }
        /// <summary>
        /// Whether to compress the splat map in the save data.
        /// </summary>
        public bool compressSplatMapInSave
        {
            get => m_CompressSplatMapInSave;
            set => m_CompressSplatMapInSave = value;
        }



        public Terrain2D()
        {
            m_Layers = new TerrainLayer[1];
            m_SplatMapUVRect = new Rect(-5, -5, 10, 10);
            m_EdgeHeight = 0.5f;
            m_Simplification = 0.001f;
            m_EdgeCornerType = CornerType.Normal;
            m_EdgeUVMapping = EdgeUVMapping.XY;

            TerrainLayer layer = new TerrainLayer();
            layer.color = Color.white;
            layer.fillColor = Color.white;
            layer.edgeColor = new Color(0.8f, 0.8f, 0.8f, 1);
            layer.fillUVRect = new Rect(0, 0, 1, 1);
            layer.edgeUVRect = new Rect(0, 0, 1, 1);
            m_Layers[0] = layer;

            m_UseDefaultCheckerTexture = true;
        }

        void OnEnable()
        {
            bool useDefaultCheckerTexture = m_UseDefaultCheckerTexture;
            m_UseDefaultCheckerTexture = useDefaultCheckerTexture;

            s_Instances.Add(this);

            if (m_BuildOnAwake || !Application.isPlaying)
            {
                if (m_IsBuilt) return;

                Build();
            }
        }

        void OnDestroy()
        {
            s_Instances.Remove(this);
        }

        protected abstract IEnumerable<MeshRenderer> GetRenderers();


        void OnSplatMapUVRectChanged()
        {
            Vector4 ST = TextureUtility.GetTextureST(m_SplatMapUVRect);
            foreach (var renderer in GetRenderers())
            {
                MaterialPropertyBlock m = new MaterialPropertyBlock();
                for (int i = 0; i < 2; i++)
                {
                    renderer.GetPropertyBlock(m, i);
                    m.SetVector("_TexS_ST", ST);
                    renderer.SetPropertyBlock(m, i);

                }
            }
        }

        void UpdateMaterials()
        {
            int nLayers = m_Layers.Length;
            if (m_SplatMapTexture == null) nLayers = 1;

            m_Materials = TerrainMaterials.GetTerrainMats(m_EdgeUVMapping, nLayers);

            m_MaterialPropertyBlocks = new MaterialPropertyBlock[2];
            m_MaterialPropertyBlocks[0] = new MaterialPropertyBlock();
            m_MaterialPropertyBlocks[1] = new MaterialPropertyBlock();

            m_MaterialPropertyBlocks[1].SetFloat("_Height", m_EdgeHeight * 0.5f);

#if UNITY_2023_1_OR_NEWER
            for (int i = 0; i < 2; i++)
            {
                m_MaterialPropertyBlocks[i].SetVector("unity_SpriteColor", new Vector4(1, 1, 1, 1));
                m_MaterialPropertyBlocks[i].SetVector("unity_SpriteProps", new Vector4(1, 1, 1, 1));
            }
#endif

            for (int i = 0; i < m_Layers.Length; i++)
            {
                var layer = m_Layers[i];

                Texture texture = layer.fillTexture;
                if (texture == null) texture = Texture2D.whiteTexture;

                m_MaterialPropertyBlocks[0].SetTexture("_Tex" + i, texture);
                m_MaterialPropertyBlocks[0].SetVector($"_Tex{i}_ST", TextureUtility.GetTextureST(layer.fillUVRect));
                m_MaterialPropertyBlocks[0].SetColor("_Col" + i, layer.fillColor * layer.color);


                texture = layer.edgeTexture;
                if (texture == null) texture = Texture2D.whiteTexture;

                m_MaterialPropertyBlocks[1].SetTexture("_Tex" + i, texture);
                m_MaterialPropertyBlocks[1].SetVector($"_Tex{i}_ST", TextureUtility.GetTextureST(layer.edgeUVRect));
                m_MaterialPropertyBlocks[1].SetColor("_Col" + i, layer.edgeColor * layer.color);
            }

            if (nLayers > 1)
            {
                Vector4 ST = TextureUtility.GetTextureST(m_SplatMapUVRect);

                if (Application.isPlaying && m_SplatMapTexture != null)
                    m_SplatMapTexture = TextureUtility.Duplicate(m_SplatMapTexture);

                for (int i = 0; i < 2; i++)
                {
                    m_MaterialPropertyBlocks[i].SetVector("_TexS_ST", ST);
                    m_MaterialPropertyBlocks[i].SetTexture("_TexS", m_SplatMapTexture);
                }
            }


            m_AverageColors = new Color[nLayers];
            for (int i = 0; i < nLayers; i++)
            {
                TerrainLayer layer = m_Layers[i];
                Color averageColor = layer.color * layer.fillColor;

                if (layer.fillTexture != null)
                {
                    averageColor *= TextureUtility.GetAverageColor(layer.fillTexture);
                }
                m_AverageColors[i] = averageColor;
            }
        }

        protected void SetMeshRendererMaterials(MeshRenderer meshRenderer, Matrix4x4 matrix, bool createNewMaterials)
        {
            m_MaterialPropertyBlocks[0].SetMatrix("_Matrix", matrix);
            m_MaterialPropertyBlocks[1].SetMatrix("_Matrix", matrix);

            if (createNewMaterials)
            {
                for (int i = 0; i < m_Materials.Length; i++)
                {
                    m_Materials[i] = new Material(m_Materials[i]);
                }
            }

            meshRenderer.sharedMaterials = m_Materials;
            meshRenderer.SetPropertyBlock(m_MaterialPropertyBlocks[0], 0);
            meshRenderer.SetPropertyBlock(m_MaterialPropertyBlocks[1], 1);
        }

        protected List<Vector2[]> GetShapes()
        {
            Matrix4x4 matrix = transform.worldToLocalMatrix;
            Shape2D[] shapes = GetComponentsInChildren<Shape2D>(false);
            List<Vector2[]> polygons = new List<Vector2[]>();
            float simplification = m_Simplification;
            float minArea = Mathf.Sqrt(simplification);
            foreach (var shape in shapes)
            {
                if (polygons.Count == 0 && !shape.fill)
                    continue;

                Vector2[] points = shape.GetRelativePoints(matrix);
                if (points == null) continue;
                if (points.Length < 2) continue;
                if (PolygonUtility.GetArea(points) < minArea) continue;

                points = PolygonSimplifier.Simplify(points, simplification, true);

                if (points.Length < 2) continue;
    
                if (polygons.Count == 0)
                {
                    polygons.Add(points);
                    continue;
                }

                polygons = PolygonClipping.Clip(polygons, points);
                //polygons = PolygonSimplifier.Simplify(polygons, simplification);
            }

            /*
            var x = polygons.ToArray();
            polygons.Clear();

            foreach (var z in x)
            {
                if (!PolygonUtility.IsClockwise(z)) polygons.Add(z);
            }

            foreach (var z in x)
            {
                if (PolygonUtility.IsClockwise(z)) polygons.Add(z);
            }
            */
            return polygons;
        }


        /// <summary>
        /// <para>Builds the terrain.</para>
        /// If the terrain is already built, it will delete everything and build it again. However, it is not recommended to use this function as a reset/rebuild function due to its heavy resource usage.
        /// </summary>
        public virtual void Build()
        {
            if (this == null) return;

            m_IsBuilt = true;

            if (m_SortingGroup == null)
            {
                m_SortingGroup = gameObject.GetComponent<SortingGroup>();
                if (m_SortingGroup == null) m_SortingGroup = gameObject.AddComponent<SortingGroup>();
                m_SortingGroup.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            }
            m_SortingGroup.sortingLayerID = m_SortingLayerID;
            m_SortingGroup.sortingOrder = m_SortingOrder;

            UpdateMaterials();

#if UNITY_EDITOR
            UpdateGizmos();
#endif
        }


        /// <summary>
        /// Edits the area within a circle and returns the amount of modified area.
        /// </summary>
        /// <param name="position">The position of the circle in world space.</param>
        /// <param name="radius">The radius of the circle in world space.</param>
        /// <param name="fill">Set 'true' to fill the area, set 'false' to remove the area.</param>
        public float EditByCircle(Vector2 position, float radius, bool fill)
        {
            return EditTerrainByCircle(position, radius, fill);
        }


        /// <summary>
        /// Edits the area within a polygon and returns the amount of modified area.
        /// </summary>
        /// <param name="polygon">An array of points in world space that form a polygon.</param>
        /// <param name="fill">Set 'true' to fill areas, set 'false' to remove areas.</param>
        public float EditByPolygon(Vector2[] polygon, bool fill)
        {
            return EditTerrainByPolygon(polygon, fill);
        }


        /// <summary>
        /// Fills the area within a circle and returns the amount of modified area.
        /// </summary>
        /// <param name="position">The position of the circle in world space.</param>
        /// <param name="radius">The radius of the circle in world space.</param>
        public float FillCircle(Vector2 position, float radius)
        {
            return EditTerrainByCircle(position, radius, true);
        }


        /// <summary>
        /// Removes the area within a circle and returns the amount of modified area.
        /// </summary>
        /// <param name="position">The position of the circle in world space.</param>
        /// <param name="radius">The radius of the circle in world space.</param>
        public float DigCircle(Vector2 position, float radius)
        {
            return EditTerrainByCircle(position, radius, false);
        }


        /// <summary>
        /// Fills the area within a polygon and returns the amount of modified area.
        /// </summary>
        /// <param name="polygon">An array of points in world space that form a polygon.</param>
        public float FillPolygon(Vector2[] polygon)
        {
            return EditTerrainByPolygon(polygon, true);
        }


        /// <summary>
        /// Removes the area within a polygon and returns the amount of modified area.
        /// </summary>
        /// <param name="polygon">An array of points in world space that form a polygon.</param>
        public float DigPolygon(Vector2[] polygon)
        {
            return EditTerrainByPolygon(polygon, false);
        }

        float EditTerrainByCircle(Vector2 position, float radius, bool fill)
        {
            if (!m_IsBuilt) return 0;
            if (fill && !m_IsFillable || !fill && !m_IsDiggable) return 0;
            return DoEditByCircle(position, radius, fill);
        }

        float EditTerrainByPolygon(Vector2[] polygon, bool fill)
        {
            if (!m_IsBuilt) return 0;
            if (fill && !m_IsFillable || !fill && !m_IsDiggable) return 0;
            return DoEditByPolygon(polygon, fill);
        }

        protected abstract float DoEditByCircle(Vector2 position, float radius, bool fill);

        protected abstract float DoEditByPolygon(Vector2[] polygon, bool fill);


        /// <summary>
        /// Paints the splat map with a circle brush.
        /// </summary>
        /// <param name="point">The position of the brush in world space.</param>
        /// <param name="radius">The radius of the circle in world space.</param>
        /// <param name="softness">The softness of the brush, ranging from 0 to 1.</param>
        /// <param name="opacity">The opacity of the brush, ranging from 0 to 1.</param>
        /// <param name="layer">Select a layer by index.</param>
        public void PaintSplatMap(Vector2 point, float radius, float softness, float opacity, int layer)
        {
            PaintSplatMap(point, radius, softness, opacity, ConvertLayerToColor(layer));
        }

        /// <summary>
        /// Paints the splat map with a circle brush.
        /// </summary>
        /// <param name="point">The position of the brush in world space.</param>
        /// <param name="radius">The radius of the circle in world space.</param>
        /// <param name="softness">The softness of the brush, ranging from 0 to 1.</param>
        /// <param name="opacity">The opacity of the brush, ranging from 0 to 1.</param>
        /// <param name="layer">Select 2 layers by transition. (e.g. The value 2.3f means that the weight of Layer 2 is 0.7f and the weight of Layer 3 is 0.3f.)</param>
        public void PaintSplatMap(Vector2 point, float radius, float softness, float opacity, float layer)
        {
            PaintSplatMap(point, radius, softness, opacity, ConvertLayerToColor(layer));
        }

        /// <summary>
        /// Paints the splat map with a circle brush.
        /// </summary>
        /// <param name="point">The position of the brush in world space.</param>
        /// <param name="radius">The radius of the circle in world space.</param>
        /// <param name="softness">The softness of the brush, ranging from 0 to 1.</param>
        /// <param name="opacity">The opacity of the brush, ranging from 0 to 1.</param>
        /// <param name="color">Select layers by color channels. (R = Layer 0, G = Layer 1, B = Layer 2, A = Layer 4, R + G + B + A = 1)</param>
        public void PaintSplatMap(Vector2 point, float radius, float softness, float opacity, Color color)
        {
            if (!m_IsBuilt) return;

            point = transform.InverseTransformPoint(point);

            Material mat = TerrainMaterials.splatMapCircleBrushMat;

            Rect rect = m_SplatMapUVRect;
            Vector4 circlePS;
            circlePS.x = (point.x - m_SplatMapUVRect.x) / m_SplatMapUVRect.width;
            circlePS.y = (point.y - m_SplatMapUVRect.y) / m_SplatMapUVRect.height;
            circlePS.z = radius / m_SplatMapUVRect.width;
            circlePS.w = radius / m_SplatMapUVRect.height;
            mat.SetVector("_Circle_PS", circlePS);
            mat.SetFloat("_BrushSoftness", softness);
            mat.SetColor("_BrushColor", color);
            mat.SetFloat("_BrushOpacity", opacity);

            int width = m_SplatMapTexture.width;
            int height = m_SplatMapTexture.height;
            var temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(m_SplatMapTexture, temp);
            Graphics.Blit(m_SplatMapTexture, temp, mat);
            Graphics.CopyTexture(temp, m_SplatMapTexture);
            RenderTexture.ReleaseTemporary(temp);

            m_IsSplatMapPainted = true;
        }


        /// <summary>
        /// Paints the splat map with a texture brush.
        /// </summary>
        /// <param name="point">The position of the brush in world space.</param>
        /// <param name="texture">The texture of the brush.</param>
        /// <param name="size">The size of the brush in world space.</param>
        /// <param name="opacity">The opacity of the brush, ranging from 0 to 1.</param>
        /// <param name="layer">Select a layer by index.</param>
        public void PaintSplatMap(Vector2 point, Texture texture, float size, float opacity, int layer)
        {
            PaintSplatMap(point, texture, size, opacity, ConvertLayerToColor(layer));
        }

        /// <summary>
        /// Paints the splat map with a texture brush.
        /// </summary>
        /// <param name="point">The position of the brush in world space.</param>
        /// <param name="texture">The texture of the brush.</param>
        /// <param name="size">The size of the brush in world space.</param>
        /// <param name="opacity">The opacity of the brush, ranging from 0 to 1.</param>
        /// <param name="layer">Select 2 layers by transition. (e.g. The value 2.3f means that the weight of Layer 2 is 0.7f and the weight of Layer 3 is 0.3f.)</param>
        public void PaintSplatMap(Vector2 point, Texture texture, float size, float opacity, float layer)
        {
            PaintSplatMap(point, texture, size, opacity, ConvertLayerToColor(layer));

        }

        /// <summary>
        /// Paints the splat map with a texture brush.
        /// </summary>
        /// <param name="point">The position of the brush in world space.</param>
        /// <param name="texture">The texture of the brush.</param>
        /// <param name="size">The size of the brush in world space.</param>
        /// <param name="opacity">The opacity of the brush, ranging from 0 to 1.</param>
        /// <param name="color">Select layers by color channels. (R = Layer 0, G = Layer 1, B = Layer 2, A = Layer 4, R + G + B + A = 1)</param>
        public void PaintSplatMap(Vector2 point, Texture texture, float size, float opacity, Color color)
        {
            if (!m_IsBuilt) return;

            point = transform.InverseTransformPoint(point);

            Material mat = TerrainMaterials.splatMapTextureBrushMat;

            mat.SetTexture("_BrushTex", texture);

            Rect rect = new Rect(point.x - size / 2, point.y - size / 2, size, size);

            rect.x = (rect.x - m_SplatMapUVRect.x) / m_SplatMapUVRect.width;
            rect.y = (rect.y - m_SplatMapUVRect.y) / m_SplatMapUVRect.height;
            rect.width = rect.width / m_SplatMapUVRect.width;
            rect.height = rect.height / m_SplatMapUVRect.height;

            Vector4 ST = TextureUtility.GetTextureST(rect);

            mat.SetVector("_BrushTex_ST", ST);
            mat.SetColor("_BrushColor", color);
            mat.SetFloat("_BrushOpacity", opacity);

            int width = m_SplatMapTexture.width;
            int height = m_SplatMapTexture.height;
            var temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(m_SplatMapTexture, temp);
            Graphics.Blit(m_SplatMapTexture, temp, mat);
            Graphics.CopyTexture(temp, m_SplatMapTexture);
            RenderTexture.ReleaseTemporary(temp);

            m_IsSplatMapPainted = true;
        }


        /// <summary>
        /// Tries to get a particle at the specified point and returns false if it fails. 
        /// </summary>
        /// <param name="point">The position of the particle in world space.</param>
        public bool TryGetParticle(Vector2 point, out TerrainParticle particle)
        {
            if (!m_IsDiggable || !m_IsBuilt)
            {
                particle = new TerrainParticle();
                return false;
            }

            return DoTryGetParticle(point, out particle);
        }

        /// <summary>
        /// <para>Collects particles at the given points and adds them to the particles list.</para>
        /// If a point is outside terrain, nothing happens.If a point is inside the terrain, it will be removed from the points list and the corresponding particle will be added to the particles list.
        /// </summary>
        /// <param name="points">A list of points in world space.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        public void GetParticles(List<Vector2> points, List<TerrainParticle> particles)
        {
            if (!m_IsBuilt) return;
            if (m_IsDiggable) DoGetParticles(points, particles);
        }


        /// <summary>
        /// <para>Collects particles at the given points and adds them to the particles list.</para>
        /// If a point is outside terrain, nothing happens.If a point is inside the terrain, it will be removed from the points list and the corresponding particle will be added to the particles list.
        /// </summary>
        /// <param name="points">A list of points in world space.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        public void GetParticles(List<Vector2> points, List<Vector2> particles)
        {
            if (!m_IsBuilt) return;
            if (m_IsDiggable) DoGetParticles(points, particles);
        }


        protected abstract bool DoTryGetParticle(Vector2 point, out TerrainParticle particle);

        protected abstract void DoGetParticles(List<Vector2> points, List<TerrainParticle> particles);

        protected abstract void DoGetParticles(List<Vector2> points, List<Vector2> particles);

        protected Color PickSplatMapColor(Vector2 localPoint)
        {
            float u = (localPoint.x - m_SplatMapUVRect.x) / m_SplatMapUVRect.width;
            float v = (localPoint.y - m_SplatMapUVRect.y) / m_SplatMapUVRect.height;

            int x = (int)(u * m_SplatMapTexture.width);
            int y = (int)(v * m_SplatMapTexture.height);

            if (m_IsSplatMapPainted) return TextureUtility.GetPixelFromGPUMemory(m_SplatMapTexture, x, y);

            return m_SplatMapTexture.GetPixel(x, y);
        }

        protected Color ConvertSplatMapColorToParticleColor(Color color)
        {
            Color sum = color.r * m_AverageColors[0];
            int n = m_Layers.Length;
            if (n > 1) sum += color.g * m_AverageColors[1];
            if (n > 2) sum += color.b * m_AverageColors[2];
            if (n > 3) sum += color.a * m_AverageColors[3];
            return sum;
        }

        Color ConvertLayerToColor(int layer)
        {
            if (layer == 0) return new Color(1, 0, 0, 0);
            else if (layer == 1) return new Color(0, 1, 0, 0);
            else if (layer == 2) return new Color(0, 0, 1, 0);
            else if (layer == 3) return new Color(0, 0, 0, 1);

            return new Color(0, 0, 0, 0);
        }

        Color ConvertLayerToColor(float layer)
        {
            if (layer < 0)
            {
                return new Color(1, 0, 0, 0);
            }
            else if (layer <= 1)
            {
                return Color.Lerp(new Color(1, 0, 0, 0), new Color(0, 1, 0, 0), layer);
            }
            else if (layer <= 2)
            {
                return Color.Lerp(new Color(0, 1, 0, 0), new Color(0, 0, 1, 0), layer - 1);
            }
            else if (layer <= 3)
            {
                return Color.Lerp(new Color(0, 0, 1, 0), new Color(0, 0, 0, 1), layer - 2);
            }

            return new Color(0, 0, 0, 0);
        }


#if UNITY_EDITOR
        static MeshData m_MeshData = new MeshData(1, 0);
        [NonSerialized] Mesh m_GizmoMesh;
        [NonSerialized] List<Vector2[]> m_GizmoPolygons;

        public void UpdateGizmos()
        {
            if (this == null) return;
            if (m_GizmoMesh == null)
            {
                m_GizmoMesh = new Mesh();
                m_GizmoMesh.hideFlags = HideFlags.DontSave;
            }

            m_GizmoPolygons = GetShapes();

            m_MeshData.Clear();
            PolygonMeshUtility.CreateFillMesh(m_MeshData, 0, m_GizmoPolygons, false);
            m_MeshData.CopyToMesh(m_GizmoMesh);
        }

        protected virtual void OnDrawGizmos()
        {
            if (m_GizmoMesh == null) return;
            if (m_GizmoPolygons.Count == 0) return;

            bool selected = GizmosUtility.IsSelected(gameObject);

            if (selected) return;

            bool isChildSelected = GizmosUtility.IsChildSelected(transform);

            float alpha = isChildSelected ? 0.5f : 0;

            using (new GizmosUtility.Scope(Color.yellow, alpha))
            {
                Vector3 p = transform.position;
                Quaternion r = transform.rotation;
                Vector3 s = transform.lossyScale;
                Gizmos.DrawMesh(m_GizmoMesh, 0, p, r, s);

                Matrix4x4 matrix = transform.localToWorldMatrix;
                foreach (var polygon in m_GizmoPolygons)
                {
                    GizmosUtility.DrawPolygon(PolygonUtility.Transform(polygon, matrix));
                }
            }
        }
#endif

        #region Save & Load Data

        string dataSignature => GetType().Name + ".Data";

        public void Save(string path)
        {
            if (!m_IsBuilt) return;

            using (Stream stream = File.Open(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    WriteData(writer);
                }
            }
        }

        public void Load(string path)
        {
            using (Stream stream = File.Open(path, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    ReadData(reader);
                }
            }
        }

        public byte[] GetData()
        {
            if (!m_IsBuilt) return new byte[0];

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    WriteData(writer);
                    data = stream.ToArray();
                }
            }
            return data;
        }

        public void LoadData(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    ReadData(reader);
                }
            }
        }

        void WriteData(BinaryWriter writer)
        {
            writer.Write(dataSignature);
            writer.Write((int)m_CompressionMethod);
            if (m_CompressionMethod != BinaryCompressionMethod.NoCompression)
            {
                byte[] bytes;
                using (MemoryStream mem = new MemoryStream())
                {
                    using (BinaryWriter writer2 = new BinaryWriter(mem))
                    {
                        OnWriteData(writer2);
                    }
                    bytes = mem.ToArray();
                }
                bytes = BinaryCompressionUtility.Compress(bytes, m_CompressionMethod, m_CompressionLevel);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else
            {
                OnWriteData(writer);
            }
        }

        void ReadData(BinaryReader reader)
        {
            string signature = reader.ReadString();
            if (signature != dataSignature)
            {
                Debug.LogError($"ReadTerrainDataFailed: Invalid Data Signature! ('{signature}' != '{dataSignature}')");
                return;
            }

            BinaryCompressionMethod compressionMethod = (BinaryCompressionMethod)reader.ReadInt32();
            if (compressionMethod != BinaryCompressionMethod.NoCompression)
            {
                int length = reader.ReadInt32();
                byte[] bytes = reader.ReadBytes(length);
                bytes = BinaryCompressionUtility.Decmpress(bytes, compressionMethod);
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    using (BinaryReader reader2 = new BinaryReader(stream))
                    {
                        OnReadData(reader2);
                    }
                }
            }
            else
            {
                OnReadData(reader);
            }
        }

        protected virtual void OnWriteData(BinaryWriter writer)
        {
            WriteTerrainData(writer);
        }

        protected virtual void OnReadData(BinaryReader reader)
        {
            ReadTerrainData(reader);
        }

        static Version s_TerrainDataVersion = new Version("2.1.0");

        void WriteTerrainData(BinaryWriter writer)
        {
            writer.WriteVersion(s_TerrainDataVersion);
            WriteTerrainData_2_1_0(writer);
        }

        void ReadTerrainData(BinaryReader reader)
        {
            Version version = reader.ReadVersion();
            if (version > s_TerrainDataVersion)
            {
                Debug.LogError($"ReadTerrainDataFailed: Unsupported Data Version! Please update to DiggableTerrains2D Version {version} or higher.");
                return;
            }

            ReadTerrainData_2_1_0(reader);
        }

        void WriteTerrainData_2_1_0(BinaryWriter writer)
        {
            bool hasSplatMap = m_IncludeSplatMapInSave && m_SplatMapTexture != null;
            writer.Write(hasSplatMap);
            if (hasSplatMap)
            {
                byte[] data = TextureUtility.GetRawTextureData(m_SplatMapTexture, m_CompressSplatMapInSave);

                writer.Write(m_CompressSplatMapInSave);
                writer.WriteRect(m_SplatMapUVRect);
                writer.Write(m_SplatMapTexture.width);
                writer.Write(m_SplatMapTexture.height);
                writer.Write(data.Length);
                writer.Write(data);
            }
        }

        void ReadTerrainData_2_1_0(BinaryReader reader)
        {
            bool hasSplatMap = reader.ReadBoolean();
            if (hasSplatMap)
            {
                bool compressed = reader.ReadBoolean();
                Rect rect = reader.ReadRect();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                int length = reader.ReadInt32();
                byte[] data = reader.ReadBytes(length);

                if (m_SplatMapTexture == null) return;

                if (m_SplatMapTexture.width != width || m_SplatMapTexture.height != height)
                {
                    m_SplatMapTexture.Reinitialize(width, height);
                }

                TextureUtility.LoadRawTextureData(m_SplatMapTexture, data, compressed);

                if (m_SplatMapUVRect != rect)
                {
                    m_SplatMapUVRect = rect;
                    OnSplatMapUVRectChanged();
                }
            }
        }
        #endregion
    }

    static class TerrainMaterials
    {
        struct Key
        {
            public EdgeUVMapping edge;
            public int layers;
            public bool isURP;

            public Key(EdgeUVMapping edge, int layers, bool isURP)
            {
                this.edge = edge;
                this.layers = layers;
                this.isURP = isURP;
            }
        }

        static Dictionary<Key, Material> s_EdgeMats = new Dictionary<Key, Material>();
        static Dictionary<Key, Material> s_FillMats = new Dictionary<Key, Material>();
        static Material s_SplatMapBrushMat;
        static Material s_CircleBrushMat;


        public static bool isURP
        {
            get
            {
                var pipeline = GraphicsSettings.defaultRenderPipeline;
                if (pipeline != null)
                {
                    return pipeline.GetType().Name == "UniversalRenderPipelineAsset";
                }
                return false;
            }
        }

        public static Material splatMapTextureBrushMat
        {
            get
            {
                if (s_SplatMapBrushMat == null)
                {
                    Shader shader = Shader.Find("Hidden/DiggableTerrains2D/SplatMapTextureBrush");
                    s_SplatMapBrushMat = new Material(shader);
                    s_SplatMapBrushMat.hideFlags = HideFlags.DontSave;
                }

                return s_SplatMapBrushMat;
            }
        }

        public static Material splatMapCircleBrushMat
        {
            get
            {
                if (s_CircleBrushMat == null)
                {
                    Shader shader = Shader.Find("Hidden/DiggableTerrains2D/SplatMapCircleBrush");
                    s_CircleBrushMat = new Material(shader);
                    s_CircleBrushMat.hideFlags = HideFlags.DontSave;
                }

                return s_CircleBrushMat;
            }
        }

        public static Material[] GetTerrainMats(EdgeUVMapping edge, int layers)
        {
            Material[] mats = new Material[2];
            mats[0] = GetTerrainFillMat(layers);
            mats[1] = GetTerrainEdgeMat(edge, layers);
            return mats;
        }


        static Material GetTerrainFillMat(int layers)
        {
            Key key = new Key(0, layers, isURP);
            if (s_FillMats.ContainsKey(key))
            {
                return s_FillMats[key];
            }

            Shader shader = isURP ?
                Shader.Find("Hidden/DiggableTerrains2D/FillURP") :
                Shader.Find("Hidden/DiggableTerrains2D/Fill");

            Material mat = new Material(shader);
            mat.hideFlags = HideFlags.DontSave;
            SetKeyword_LAYERS(mat, layers);
            mat.renderQueue = 2998;
            s_FillMats.Add(key, mat);
            return mat;
        }

        static Material GetTerrainEdgeMat(EdgeUVMapping edge, int layers)
        {
            Key key = new Key(edge, layers, isURP);
            if (s_EdgeMats.ContainsKey(key))
            {
                return s_EdgeMats[key];
            }

            Shader shader = isURP ?
                Shader.Find("Hidden/DiggableTerrains2D/EdgeURP") :
                Shader.Find("Hidden/DiggableTerrains2D/Edge");

            Material mat = new Material(shader);
            mat.hideFlags = HideFlags.DontSave;
            mat.renderQueue = 2999;
            SetKeyword_LAYERS(mat, layers);
            SetKeyword_UV_MAPPING(mat, edge);
            s_EdgeMats.Add(key, mat);
            return mat;
        }

        static void SetKeyword_LAYERS(Material mat, int layers)
        {
            mat.DisableKeyword("_LAYERS_N1");
            if (layers == 1) mat.EnableKeyword("_LAYERS_N1");
            else if (layers == 2) mat.EnableKeyword("_LAYERS_N2");
            else if (layers == 3) mat.EnableKeyword("_LAYERS_N3");
            else if (layers == 4) mat.EnableKeyword("_LAYERS_N4");
        }

        static void SetKeyword_UV_MAPPING(Material mat, EdgeUVMapping edge)
        {
            if (edge == EdgeUVMapping.X) mat.EnableKeyword("_UV_MAPPING_X");
            else if (edge == EdgeUVMapping.Y) mat.EnableKeyword("_UV_MAPPING_Y");
            else if (edge == EdgeUVMapping.XY) mat.EnableKeyword("_UV_MAPPING_XY");
        }
    }

    public struct TerrainParticle
    {
        public Vector2 position;
        public Color color;
    }

    [Serializable]
    struct TerrainLayer
    {
        public Color color;
        [Space]
        public Color fillColor;
        public Texture fillTexture;
        public Rect fillUVRect;
        [Space]
        public Color edgeColor;
        public Texture edgeTexture;
        public Rect edgeUVRect;
    }

    enum EdgeUVMapping
    {
        X, Y, XY
    }

    enum CornerType
    {
        Simple, Normal, Rounded
    }
}