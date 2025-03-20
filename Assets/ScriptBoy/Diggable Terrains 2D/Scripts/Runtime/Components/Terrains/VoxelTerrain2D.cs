using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    [ExecuteInEditMode]
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Terrains/Voxel Terrain 2D")]
    public sealed class VoxelTerrain2D : Terrain2D
    {
        public new static VoxelTerrain2D[] instances
        {
            get => FindByFilter<VoxelTerrain2D>(null, true);
        }

        public new static VoxelTerrain2D[] activeInstances
        {
            get => FindByFilter<VoxelTerrain2D>(null, false);
        }

        public new static VoxelTerrain2D[] FindByMask(int layerMask, bool includeInactive)
        {
            return FindByMask<VoxelTerrain2D>(layerMask, includeInactive);
        }

        public new static VoxelTerrain2D[] FindByTag(string tag, bool includeInactive)
        {
            return FindByTag<VoxelTerrain2D>(tag, includeInactive);
        }


        Chunk[] m_Chunks;
        Transform m_ChunksTransform;

        [SerializeField] Res m_MapWidth = Res._64;
        [SerializeField] Res m_MapHeight = Res._64;
        [SerializeField] MapTransform m_MapTransform;
        [SerializeField, Min(0)] float m_MapPadding = 2;
        [SerializeField] Vector2 m_MapPosition;
        [SerializeField] float m_MapScale = 1;

        float mapScale => m_MapScale / Mathf.Max((float)m_MapWidth, (float)m_MapHeight);

        Vector2Int chunkCount
        {
            get
            {
                int x = (int)m_MapWidth / 32;
                int y = (int)m_MapHeight / 32;
                return new Vector2Int(x, y);
            }
        }

        enum Res
        {
            _32 = 32,
            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
        }

        enum MapTransform
        {
            Auto, Manual
        }

        public override void Build()
        {
            if (this == null) return;

            base.Build();

            Vector2[][] polygons = GetShapes().ToArray();

            if (polygons != null)
            {
                if (m_MapTransform == MapTransform.Auto)
                {
                    int w = (int)m_MapWidth;
                    int h = (int)m_MapHeight;
                    Bounds bounds = PolygonUtility.GetBounds(polygons, m_MapPadding);
                    m_MapPosition = bounds.min;

                    float raitoX = w > h ? 1 : (float)h / w;
                    float raitoY = w < h ? 1 : (float)w / h;
                    float x = bounds.size.x * raitoX;
                    float y = bounds.size.y * raitoY;
                    m_MapScale = Mathf.Max(x, y);
                }
            }

            if (m_MapScale < 0.01f || float.IsInfinity(m_MapScale)) return;

            CreateChunks();

            if(polygons != null)
                ApplyPolygons(polygons);

            foreach (var chunk in m_Chunks)
            {
                UpdateChunk(chunk);
            }
        }

        protected override bool DoTryGetParticle(Vector2 point, out TerrainParticle particle)
        {
            foreach (var chunk in m_Chunks)
            {
                if (chunk.collider.OverlapPoint(point))
                {
                    point = transform.InverseTransformPoint(point);
                    Color splatMapColor = PickSplatMapColor(point);
                    particle.position = point;
                    particle.color = ConvertSplatMapColorToParticleColor(splatMapColor);
                    return true;
                }
            }

            particle = new TerrainParticle();
            return false;
        }

        protected override void DoGetParticles(List<Vector2> points, List<TerrainParticle> particles)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                Vector2 point = points[i];
                foreach (var chunk in m_Chunks)
                {
                    if (chunk.collider.OverlapPoint(point))
                    {
                        TerrainParticle particle;
                        particle.position = point;

                        point = transform.InverseTransformPoint(point);
                        Color splatMapColor = PickSplatMapColor(point);

                        particle.color = ConvertSplatMapColorToParticleColor(splatMapColor);
                        particles.Add(particle);
                        points.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        protected override void DoGetParticles(List<Vector2> points, List<Vector2> particles)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                Vector2 point = points[i];
                foreach (var chunk in m_Chunks)
                {
                    if (chunk.collider.OverlapPoint(point))
                    {
                        particles.Add(point);
                        points.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        protected override float DoEditByCircle(Vector2 position, float radius, bool fill)
        {
            position = transform.transform.InverseTransformPoint(position);
            position -= m_MapPosition;

            int w = (int)m_MapWidth;
            int h = (int)m_MapHeight;

            Vector2Int chunkCount = this.chunkCount;
            Vector2 chunkRes = new Vector2(w / chunkCount.x, h / chunkCount.y);

            int xMin = (int)((position.x - radius - chunkRes.x) / chunkRes.x);
            int xMax = (int)((position.x + radius + chunkRes.x) / chunkRes.x);
            int yMin = (int)((position.y - radius - chunkRes.y) / chunkRes.y);
            int yMax = (int)((position.y + radius + chunkRes.y) / chunkRes.y);

            xMin = Mathf.Max(xMin, 0);
            yMin = Mathf.Max(yMin, 0);

            xMax = Mathf.Min(xMax, chunkCount.x);
            yMax = Mathf.Min(yMax, chunkCount.y);


            float area = 0;
            for (int x = 0; x < chunkCount.x; x++)
            {
                for (int y = 0; y < chunkCount.y; y++)
                {
                    int i = y * chunkCount.x + x;
                    Vector2 offset = new Vector2(x * chunkRes.x, y * chunkRes.y);
                    var chunk = m_Chunks[i];
                    VoxelMap map = chunk.map;

                    if (map.EditByCircle(position / mapScale - offset, radius / mapScale, fill))
                    {
                        float prevArea = chunk.area;
                        UpdateChunk(chunk);
                        area += Mathf.Abs(chunk.area - prevArea);
                    }
                }
            }
            return area;
        }

        protected override float DoEditByPolygon(Vector2[] polygon, bool fill)
        {
            polygon = PolygonUtility.Transform(polygon, m_ChunksTransform.worldToLocalMatrix);
            if (!fill ^ PolygonUtility.IsClockwise(polygon))
            {
                PolygonUtility.DoReverse(polygon);
            }

            int w = (int)m_MapWidth;
            int h = (int)m_MapHeight;
            Vector2Int chunkCount = this.chunkCount;
            Vector2 chunkRes = new Vector2(w / chunkCount.x, h / chunkCount.y);

            float area = 0;
            for (int x = 0; x < chunkCount.x; x++)
            {
                for (int y = 0; y < chunkCount.y; y++)
                {
                    int i = y * chunkCount.x + x;
                    Vector2 offset = -new Vector2(x * chunkRes.x, y * chunkRes.y);
                    var chunk = m_Chunks[i];
                    VoxelMap map = chunk.map;
                    if (map.EditByPolygon(PolygonUtility.Offset(polygon, offset), fill))
                    {
                        float prevArea = chunk.area;
                        UpdateChunk(chunk);
                        area += Mathf.Abs(chunk.area - prevArea);
                    }
                }
            }

            return area;
        }

        void CreateChunks()
        {
            m_Chunks = null;
            m_ChunksTransform = transform.Find("Chunks");
            if (m_ChunksTransform != null) DestroyImmediate(m_ChunksTransform.gameObject);

            GameObject chunksGameObject = new GameObject("Chunks");
            chunksGameObject.hideFlags = HideFlags.HideAndDontSave;
            m_ChunksTransform = chunksGameObject.transform;
            m_ChunksTransform.SetParent(transform, false);
            m_ChunksTransform.localScale = new Vector3(mapScale, mapScale, 1);
            m_ChunksTransform.localPosition = m_MapPosition;

            Vector2Int chunkCount = this.chunkCount;
            m_Chunks = new Chunk[chunkCount.x * chunkCount.y];
            int w = (int)m_MapWidth;
            int h = (int)m_MapHeight;
            Vector2Int chunkOffset = new Vector2Int(w / chunkCount.x, h / chunkCount.y);
            for (int x = 0; x < chunkCount.x; x++)
            {
                for (int y = 0; y < chunkCount.y; y++)
                {
                    Vector3 pos = new Vector2(x * chunkOffset.x, y * chunkOffset.y);
                    VoxelMap map = new VoxelMap(chunkOffset + Vector2Int.one);
                    Chunk chunk = CreateChunk(map, m_ChunksTransform, pos);
                    m_Chunks[y * chunkCount.x + x] = chunk;
                }
            }
        }

        Chunk CreateChunk(VoxelMap map, Transform parent, Vector2 position)
        {
            GameObject gameObject = new GameObject();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.tag = tag;
            gameObject.layer = this.gameObject.layer;
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = position;

            PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();

            meshFilter.sharedMesh = mesh;

            Matrix4x4 matrix = transform.localToWorldMatrix;
            matrix = this.transform.localToWorldMatrix.inverse * matrix;
            SetMeshRendererMaterials(meshRenderer, matrix, false);

            Chunk chunk = new Chunk();
            chunk.gameObject = gameObject;
            chunk.transform = transform;
            chunk.collider = collider;
            chunk.meshRenderer = meshRenderer;
            chunk.meshFilter = meshFilter;
            chunk.mesh = mesh;
            chunk.map = map;
            return chunk;
        }

        void UpdateChunksTransform()
        {
            m_ChunksTransform.localScale = new Vector3(mapScale, mapScale, 1);
            m_ChunksTransform.localPosition = m_MapPosition;
            Vector2Int chunkCount = this.chunkCount;
            int w = (int)m_MapWidth;
            int h = (int)m_MapHeight;
            Vector2Int chunkOffset = new Vector2Int(w / chunkCount.x, h / chunkCount.y);
            for (int x = 0; x < chunkCount.x; x++)
            {
                for (int y = 0; y < chunkCount.y; y++)
                {
                    Vector3 pos = new Vector2(x * chunkOffset.x, y * chunkOffset.y);
                    Chunk chunk = m_Chunks[y * chunkCount.x + x];
                    chunk.transform.localPosition = pos;
                }
            }
        }

        Chunk CreateChunk(Transform parent, Vector2 position, Vector2Int mapSize)
        {
            GameObject gameObject = new GameObject();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.tag = tag;
            gameObject.layer = this.gameObject.layer;
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = position;

            PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();

            VoxelMap map = new VoxelMap(mapSize);

            meshFilter.sharedMesh = mesh;

            Matrix4x4 matrix = transform.localToWorldMatrix;
            matrix = this.transform.localToWorldMatrix.inverse * matrix;
            SetMeshRendererMaterials(meshRenderer, matrix, false);

            Chunk chunk = new Chunk();
            chunk.gameObject = gameObject;
            chunk.transform = transform;
            chunk.collider = collider;
            chunk.meshRenderer = meshRenderer;
            chunk.meshFilter = meshFilter;
            chunk.mesh = mesh;
            chunk.map = map;
            return chunk;
        }

        void UpdateChunk(Chunk chunk)
        {
            Mesh mesh = chunk.mesh;
            VoxelMap map = chunk.map;
            PolygonCollider2D collider = chunk.collider;

            bool hidden = map.overallState == 0;
            chunk.gameObject.SetActive(!hidden);
            if (hidden)
            {
                mesh.Clear();
                chunk.area = 0;
                collider.pathCount = 0;
                chunk.openPaths = new Vector2[0][];
                chunk.closedPaths = new Vector2[0][];
                return;
            }

            MeshData mData = new MeshData(2, 0);

            var marchingSquares = MarchingSquares.instance;
            marchingSquares.ReadMap(chunk.map);

            mData.vertices.AddRange(marchingSquares.verts);
            mData.subMeshs[0].AddRange(marchingSquares.triangles);
            mData.normals.AddRange(VectorUtility.CreateArray(Vector3.back, mData.vertices.Count));

            marchingSquares.UpdatePaths(m_Simplification);
      
            float colliderOffset = m_EdgeHeight * (0.5f + m_EdgeOffset) * (m_ColliderOffset);
            marchingSquares.OffsetColliderPaths(map, colliderOffset / mapScale);
            var colliderPaths = marchingSquares.colliderPaths;
            
            collider.pathCount = colliderPaths.Count;
            for (int i = 0; i < colliderPaths.Count; i++)
            {
                var path = colliderPaths[i];
                collider.SetPath(i, path);
            }

            var closedPaths = ApplyCornerType(marchingSquares.closedPaths);
            var openPaths = ApplyCornerTypeForPolyline(marchingSquares.openPaths);
            float height = m_EdgeHeight / mapScale;
            float offset = m_EdgeHeight * m_EdgeOffset / mapScale;
            PolygonMeshUtility.CreateEdgeMesh(mData, 1, closedPaths, height, offset);

            foreach (var path in openPaths)
            {
                Vector2 firtNormal = map.GetBoundaryVertNormal(path[0]);
                Vector2 lastNormal = map.GetBoundaryVertNormal(path[path.Length - 1]);
                PolylineMeshUtility.CreateEdgeMesh(mData, 1, path, firtNormal, lastNormal, height, offset);
            }

            chunk.area = marchingSquares.area;
            chunk.openPaths = marchingSquares.openPaths.ToArray();
            chunk.closedPaths = marchingSquares.closedPaths.ToArray();

            mData.CopyToMesh(mesh);
        }

        List<Vector2[]> ApplyCornerType(List<Vector2[]> polygons)
        {
            if (m_EdgeCornerType == CornerType.Simple) return polygons;

            polygons = new List<Vector2[]>(polygons);
            float height = m_EdgeHeight * 0.5f;
            float offset = m_EdgeOffset * m_EdgeHeight;
            for (int i = 0; i < polygons.Count; i++)
            {
                if (m_EdgeCornerType == CornerType.Normal)
                {
                    polygons[i] = PolygonUtility.BevelCornerPro(polygons[i], height + offset, true);
                }
                else if (m_EdgeCornerType == CornerType.Rounded)
                {
                    polygons[i] = PolygonUtility.RoundCornerPro(polygons[i], 20, height * 1.5f + offset);
                }
            }

            return polygons;
        }

        List<Vector2[]> ApplyCornerTypeForPolyline(List<Vector2[]> polyline)
        {
            if (m_EdgeCornerType == CornerType.Simple) return polyline;

            polyline = new List<Vector2[]>(polyline);
            float height = m_EdgeHeight * 0.5f;
            float offset = m_EdgeOffset * m_EdgeHeight;
            for (int i = 0; i < polyline.Count; i++)
            {
                if (m_EdgeCornerType == CornerType.Normal)
                {
                    polyline[i] = PolylineUtility.BevelCornerPro(polyline[i], height + offset, true);
                }
                else if (m_EdgeCornerType == CornerType.Rounded)
                {
                    polyline[i] = PolylineUtility.RoundCornerPro(polyline[i], 20, height * 1.5f + offset);
                }
            }

            return polyline;
        }

        void ApplyPolygons(Vector2[][] polygons)
        {
            PolygonUtility.DoSort(polygons);
            PolygonUtility.DoTransform(polygons, m_ChunksTransform.worldToLocalMatrix * transform.localToWorldMatrix);
            int w = (int)m_MapWidth;
            int h = (int)m_MapHeight;
            Vector2Int chunkCount = this.chunkCount;
            Vector2 chunkRes = new Vector2(w / chunkCount.x, h / chunkCount.y);
            for (int x = 0; x < chunkCount.x; x++)
            {
                for (int y = 0; y < chunkCount.y; y++)
                {
                    int i = y * chunkCount.x + x;
                    Vector2 offset = -new Vector2(x * chunkRes.x, y * chunkRes.y);
                    var chunk = m_Chunks[i];
                    VoxelMap map = chunk.map;
                    foreach (var path in polygons)
                    {
                        Vector2[] polygon = path;
                        polygon = PolygonUtility.Offset(polygon, offset);
                        map.EditByPolygon(polygon, !PolygonUtility.IsClockwise(polygon));
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (m_Chunks == null) return;

            foreach (var chunk in m_Chunks)
            {
                Matrix4x4 matrix = chunk.transform.localToWorldMatrix;
                if (m_ColliderOffset > 0)
                {
                    for (int i = 0; i < chunk.openPaths.Length; i++)
                    {
                        Vector2[] path = chunk.openPaths[i];
                        path = PolygonUtility.Transform(path, matrix);
                        GizmosUtility.DrawPolyline(path, Color.cyan);
                    }

                    for (int i = 0; i < chunk.closedPaths.Length; i++)
                    {
                        Vector2[] path = chunk.closedPaths[i];
                        path = PolygonUtility.Transform(path, matrix);
                        GizmosUtility.DrawPolygon(path, Color.cyan);
                    }
                }

                var collider = chunk.collider;
                for (int i = 0; i < collider.pathCount; i++)
                {
                    Vector2[] path = collider.GetPath(i);
                    PolygonUtility.DoTransform(path, matrix);
                    GizmosUtility.DrawPolygon(path, Color.green);
                }
            }
        }
#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            bool selected = UnityEditor.Selection.Contains(gameObject);

            if (!selected)
            {
                bool isChildSelected = false;
                var selectedTransforms = UnityEditor.Selection.transforms;
                foreach (var selectedTransform in selectedTransforms)
                {
                    if (selectedTransform.IsChildOf(transform))
                    {
                        isChildSelected = true;
                        break;
                    }
                }
                if (!isChildSelected) return;
            }


            int w = (int)m_MapWidth;
            int h = (int)m_MapHeight;
            float scale = m_MapScale;
            float raitoX = w > h ? 1 : (float)h / w;
            float raitoY = w < h ? 1 : (float)w / h;
            Vector2 position = m_MapPosition;
            Vector2 size = new Vector2(scale, scale);
            size = new Vector2(scale / raitoX, scale / raitoY);

            GizmosUtility.DrawRect(position, size, transform.localToWorldMatrix);
        }
#endif

        class Chunk
        {
            public VoxelMap map;
            public GameObject gameObject;
            public Transform transform;
            public PolygonCollider2D collider;
            public MeshRenderer meshRenderer;
            public MeshFilter meshFilter;
            public Mesh mesh;
            public float area;
            public Vector2[][] openPaths = new Vector2[0][];
            public Vector2[][] closedPaths = new Vector2[0][];
        }

        protected override IEnumerable<MeshRenderer> GetRenderers()
        {
            foreach (var chunk in m_Chunks)
            {
                yield return chunk.meshRenderer;
            }
        }

        #region Read & Write Data

        static Version s_DataVersion = new Version("2.1.0");

        protected override void OnWriteData(BinaryWriter writer)
        {
            base.OnWriteData(writer);
            writer.WriteVersion(s_DataVersion);
            WriteData_2_1_0(writer);
        }

        protected override void OnReadData(BinaryReader reader)
        {
            base.OnReadData(reader);

            Version version = reader.ReadVersion();
            if (version > s_DataVersion)
            {
                Debug.LogError($"ReadVoxelTerrainDataFailed: Unsupported Data Version! Please update to DiggableTerrains2D Version {version} or higher.");
                return;
            }
            ReadData_2_1_0(reader);
        }

        void WriteData_2_1_0(BinaryWriter writer)
        {
            writer.Write((int)m_MapWidth);
            writer.Write((int)m_MapHeight);
            writer.WriteVector2(m_MapPosition);
            writer.Write(m_MapScale);
            writer.WriteVoxelMapArray(GetVoxelMaps());
        }

        void ReadData_2_1_0(BinaryReader reader)
        {
            Res mapWidth = (Res)reader.ReadInt32();
            Res mapHeight = (Res)reader.ReadInt32();
            Vector2 mapPosition = reader.ReadVector2();
            float mapScale = reader.ReadSingle();
            bool resCenaged = mapWidth != m_MapWidth || mapHeight != m_MapHeight;
            bool transformCenaged = mapPosition != m_MapPosition || mapScale != m_MapScale;

            m_MapWidth = mapWidth;
            m_MapHeight = mapHeight;
            m_MapPosition = mapPosition;
            m_MapScale = mapScale;

            if (!isBuilt)
            {
                base.Build();
                CreateChunks();
                SetVoxelMaps(reader.ReadVoxelMapArray());
            }
            else if (resCenaged)
            {
                CreateChunks();
                SetVoxelMaps(reader.ReadVoxelMapArray());
            }
            else
            {
                if (transformCenaged) UpdateChunksTransform();
                SetVoxelMaps(reader.ReadVoxelMapArray());
            }
        }

        VoxelMap[] GetVoxelMaps()
        {
            int n = m_Chunks.Length;
            VoxelMap[] maps = new VoxelMap[n];
            for (int i = 0; i < n; i++)
            {
                maps[i] = m_Chunks[i].map;
            }
            return maps;
        }

        void SetVoxelMaps(VoxelMap[] maps)
        {
            int n = m_Chunks.Length;
            for (int i = 0; i < n; i++)
            {
                Chunk chunk = m_Chunks[i];
                chunk.map = maps[i];
                UpdateChunk(chunk);
            }
        }
        #endregion
    }
}