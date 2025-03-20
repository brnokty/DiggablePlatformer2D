using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

namespace ScriptBoy.DiggableTerrains2D
{
    [ExecuteInEditMode]
    [AddComponentMenu(" Script Boy/Diggable Terrains 2D/Terrains/Polygon Terrain 2D")]
    public sealed class PolygonTerrain2D : Terrain2D
    {
        public new static PolygonTerrain2D[] instances
        {
            get => FindByFilter<PolygonTerrain2D>(null, true);
        }

        public new static PolygonTerrain2D[] activeInstances
        {
            get => FindByFilter<PolygonTerrain2D>(null, false);
        }

        public new static PolygonTerrain2D[] FindByMask(int layerMask, bool includeInactive)
        {
            return FindByMask<PolygonTerrain2D>(layerMask, includeInactive);
        }

        public new static PolygonTerrain2D[] FindByTag(string tag, bool includeInactive)
        {
            return FindByTag<PolygonTerrain2D>(tag, includeInactive);
        }

        [SerializeField, HideInInspector] bool m_UseDelaunay;
        [SerializeField, HideInInspector] bool m_EnableHoles;
        [SerializeField, HideInInspector] bool m_EnablePhysics;
        [SerializeField, HideInInspector] Vector2[] m_Anchors;

        List<Chunk> m_Chunks;

        public override void Build()
        {
            base.Build();

            if (Application.isPlaying)
            {
                if (m_Chunks != null)
                {
                    foreach (var chunk in m_Chunks)
                    {
                        DestroyImmediate(chunk.gameObject);
                    }
                }
            }
            else
            {
                var chunks = GetComponentsInChildren<MeshRenderer>();
                foreach (var chunk in chunks) DestroyImmediate(chunk.gameObject);
            }

            m_Chunks = null;

            List<Vector2[]> paths = GetShapes();

            if (isFillable || isDiggable)
            {
                int maxCount = 0;
                int totalCount = 0;
                foreach (var path in paths)
                {
                    int count = path.Length;
                    if (count > maxCount)
                        maxCount = count;
                    totalCount += count;
                }

                if (isFillable && totalCount > 2000 || !isFillable && isDiggable && maxCount > 2000)
                {
                    Debug.Log($"The performance of PolygonTerrain2D decreases as the number of polygon's points increases. Currently, the point count is {totalCount}! Please use VoxelTerrain2D instead.");
                }
            }

            List<List<Vector2[]>> polygonPacks;
            if (m_EnableHoles)
            {
                polygonPacks = PolygonUtility.Pack(paths.ToArray());
            }
            else
            {
                polygonPacks = new List<List<Vector2[]>>();

                List<Vector2[]> holes = new List<Vector2[]>();
                paths.RemoveAll(e =>
                {
                    if (PolygonUtility.IsClockwise(e))
                    {
                        holes.Add(e);
                        return true;
                    }
                    return false;
                });

                foreach (var path in paths)
                {
                    if (PolygonUtility.IsClockwise(path)) continue;
                    if (holes.Exists(hole => PolygonUtility.IsPointInside(path[0], hole))) continue;
                    polygonPacks.Add(new List<Vector2[]>() { path });
                }
            }

            m_Chunks = new List<Chunk>(polygonPacks.Count);
            foreach (var polygons in polygonPacks)
            {
                Chunk chunk = CreateChunk();
                chunk.polygons = polygons;
                UpdateChunk(chunk);
                m_Chunks.Add(chunk);

                if (m_EnablePhysics)
                {
                    foreach (var anchor in m_Anchors)
                    {
                        if (PolygonUtility.IsPointInside(anchor, polygons[0]))
                        {
                            bool insideHoles = false;
                            for (int i = 1; i < polygons.Count; i++)
                            {
                                if (insideHoles = PolygonUtility.IsPointInside(anchor, polygons[i])) break;
                            }
                            if (!insideHoles) chunk.anchors.Add(anchor);
                        }
                    }

                    if (chunk.anchors.Count == 0)
                    {
                        if (chunk.rigidbody == null)
                        {
                            chunk.rigidbody = chunk.gameObject.AddComponent<Rigidbody2D>();
                            chunk.rigidbody.useAutoMass = true;
                        }
                    }
                }
            }
        }

        protected override float DoEditByCircle(Vector2 position, float radius, bool fill)
        {
            Vector2[] polygon = PolygonUtility.CreateCircle(position, 20, radius);
            return DoEditByPolygon(polygon, fill);
        }

        protected override float DoEditByPolygon(Vector2[] polygon, bool fill)
        {
            Matrix4x4 m = transform.worldToLocalMatrix;
            PolygonUtility.DoTransform(polygon, m);
            polygon = PolygonSimplifier.Simplify(polygon, m_Simplification);
            PolygonUtility.DoTransform(polygon, m.inverse);

            float prevArea = GetAllArea();
            if (fill) Fill(polygon); else Dig(polygon);
            return Mathf.Abs(prevArea - GetAllArea());
        }

        protected override bool DoTryGetParticle(Vector2 point, out TerrainParticle particle)
        {
            foreach (var chunk in m_Chunks)
            {
                if (chunk.collider.OverlapPoint(point))
                {
                    particle.position = point;

                    if (m_EnablePhysics)
                    {
                        point = chunk.transform.InverseTransformPoint(point);
                    }
                    else
                    {
                        point = transform.InverseTransformPoint(point);
                    }

                    Color splatMapColor = PickSplatMapColor(point);
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
                        if (m_EnablePhysics)
                        {
                            point = chunk.transform.InverseTransformPoint(point);
                        }
                        else
                        {
                            point = transform.InverseTransformPoint(point);
                        }

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

        float GetAllArea()
        {
            float area = 0;
            foreach (var chunk in m_Chunks)
            {
                area += PolygonUtility.GetArea(chunk.polygons.ToArray());
            }
            return area;
        }

        Chunk CreateChunk()
        {
            GameObject gameObject = new GameObject();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.tag = tag;
            gameObject.layer = this.gameObject.layer;
            Transform transform = gameObject.transform;
            transform.SetParent(this.transform, false);

            PolygonCollider2D collider = gameObject.AddComponent<PolygonCollider2D>();

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();

            meshFilter.sharedMesh = mesh;

            SetMeshRendererMaterials(meshRenderer, Matrix4x4.identity, m_EnablePhysics);

            Chunk chunk = new Chunk();
            chunk.gameObject = gameObject;
            chunk.transform = transform;
            chunk.collider = collider;
            chunk.meshRenderer = meshRenderer;
            chunk.meshFilter = meshFilter;
            chunk.mesh = mesh;

            if (m_EnablePhysics)
            {
                chunk.anchors = new List<Vector2>();
                gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            }

            return chunk;
        }

        void UpdateChunk(Chunk chunk)
        {
            UpdateChunkCollider(chunk);
            UpdateChunkMesh(chunk);
        }

        void UpdateChunkMesh(Chunk chunk)
        {
            var polygons = chunk.polygons;
            var mesh = chunk.mesh;

            float height = m_EdgeHeight;
            float offset = m_EdgeHeight * m_EdgeOffset;

            MeshData mData = new MeshData(3, 0);
            PolygonMeshUtility.CreateFillMesh(mData, 0, polygons, m_UseDelaunay);
            polygons = ApplyCornerType(polygons);
            PolygonMeshUtility.CreateEdgeMesh(mData, 1, polygons, height, offset);

            mData.CopyToMesh(chunk.mesh);
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
                    polygons[i] = PolygonUtility.RoundCornerPro(polygons[i], 30, height * 1.5f + offset);
                }
            }

            return polygons;
        } 

        void UpdateChunkCollider(Chunk chunk)
        {
            var collider = chunk.collider;
            var polygons = chunk.polygons;

            float colliderOffset = -m_EdgeHeight * (0.5f + m_EdgeOffset) * (m_ColliderOffset);
            int n = polygons.Count;
            collider.pathCount = n;
            for (int i = 0; i < n; i++)
            {
                collider.SetPath(i, PolygonUtility.Extrude(polygons[i], colliderOffset));
            }
        }

        void Dig(Vector2[] clip)
        {
            if (!PolygonUtility.IsClockwise(clip))
            {
                PolygonUtility.DoReverse(clip);
            }

            if (m_EnablePhysics)
            {
                Dig_PhysicsModeOn(clip);
            }
            else
            {
                Dig_PhysicsModeOff(clip);
            }
        }

        void Fill(Vector2[] clip)
        {
            if (m_EnablePhysics)
                Debug.LogWarning("The 'Fill' function is not fully supported when the 'Physics' feature is enabled.");

            if (PolygonUtility.IsClockwise(clip))
            {
                PolygonUtility.DoReverse(clip);
            }

            if (m_EnablePhysics)
            {
                Fill_PhysicsModeOn(clip);
            }
            else
            {
                Fill_PhysicsModeOff(clip);
            }
        }

        void Dig_PhysicsModeOff(Vector2[] clip)
        {
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            clip = PolygonUtility.Transform(clip, worldToLocal);
            Bounds clipBounds = PolygonUtility.GetBounds(clip);

            foreach (var terrainChunk in m_Chunks.ToArray())
            {
                Bounds chunkBounds = PolygonUtility.GetBounds(terrainChunk.polygons[0]);
                if (!clipBounds.Intersects(chunkBounds)) continue;

                if (!PolygonClipping.TryClip(terrainChunk.polygons, clip, out List<Vector2[]> clipedPolygons))
                    continue;

                if (!m_EnableHoles)
                {
                    if (clipedPolygons.RemoveAll(e => PolygonUtility.IsClockwise(e)) > 0)
                    {
                        continue;
                    }
                }


                if (clipedPolygons.Exists(p => PolygonUtility.IsSelfIntersecting(p)))
                {
                    Debug.Log("A");
                }
                clipedPolygons = PolygonSimplifier.Simplify(clipedPolygons, m_Simplification);
                if (clipedPolygons.Exists(p => PolygonUtility.IsSelfIntersecting(p)))
                {
                    Debug.Log("B");
                }


                float minArea = Mathf.Sqrt(m_Simplification) * 2;
                clipedPolygons.RemoveAll(e => e.Length < 3 || PolygonUtility.GetArea(e) < minArea);


                if (clipedPolygons.Count == 0)
                {
                    Destroy(terrainChunk.gameObject);
                    m_Chunks.Remove(terrainChunk);
                    continue;
                }

                List<List<Vector2[]>> polygonPacks = PolygonUtility.Pack(clipedPolygons.ToArray());
                Chunk chunk = terrainChunk;
                for (int i = 0; i < polygonPacks.Count; i++)
                {
                    if (i > 0)
                    {
                        chunk = CreateChunk();
                        m_Chunks.Add(chunk);
                    }

                    chunk.polygons = polygonPacks[i];
                    UpdateChunk(chunk);
                }
            }
        }

        void Fill_PhysicsModeOff(Vector2[] clip)
        {
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            clip = PolygonUtility.Transform(clip, worldToLocal);
            Bounds clipBounds = PolygonUtility.GetBounds(clip);

            List<Chunk> activeChunks = new List<Chunk>();
            List<Vector2[]> activePolygons = new List<Vector2[]>();
            foreach (var chunk in m_Chunks)
            {
                Bounds chunkBounds = PolygonUtility.GetBounds(chunk.polygons[0]);
                if (clipBounds.Intersects(chunkBounds))
                {
                    activeChunks.Add(chunk);
                    activePolygons.AddRange(chunk.polygons);
                }
            }

            if (activeChunks.Count == 0)
            {
                Chunk chunk = CreateChunk();
                chunk.polygons = new List<Vector2[]>() { clip };
                UpdateChunk(chunk);
                m_Chunks.Add(chunk);
                return;
            }

            if (!PolygonClipping.TryClip(activePolygons, clip, out List<Vector2[]> clipedPolygons)) return;


            if (!m_EnableHoles)
            {
                List<Vector2[]> holes = new List<Vector2[]>();
                clipedPolygons.RemoveAll(e =>
                {
                    if (PolygonUtility.IsClockwise(e))
                    {
                        holes.Add(e);
                        return true;
                    }
                    return false;
                });

                clipedPolygons.RemoveAll(p => holes.Exists(h => PolygonUtility.IsPointInside(p[0], h)));
                m_Chunks.RemoveAll(c =>
                {
                    if (holes.Exists(h => PolygonUtility.IsPointInside(c.polygons[0][0], h)))
                    {
                        activeChunks.Remove(c);
                        Destroy(c.gameObject);
                        return true;
                    }
                    return false;
                });
            }

            activeChunks.RemoveAll(c =>
            {
                int i = clipedPolygons.IndexOf(c.polygons[0]);
                if (i != -1)
                {
                    if (m_EnableHoles)
                    {
                        if (c.polygons.TrueForAll(p => clipedPolygons.Contains(p)))
                        {
                            clipedPolygons.RemoveAll(p => c.polygons.Contains(p));
                            return true;
                        }
                    }
                    else
                    {
                        clipedPolygons.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            });


            clipedPolygons = PolygonSimplifier.Simplify(clipedPolygons, m_Simplification);
            clipedPolygons.RemoveAll(e => e.Length < 3);

            var polygonPacks = PolygonUtility.Pack(clipedPolygons.ToArray());



            if (polygonPacks.Count > activeChunks.Count)
            {
                int delta = polygonPacks.Count - activeChunks.Count;
                for (int i = 0; i < delta; i++)
                {
                    Chunk chunk = CreateChunk();
                    activeChunks.Add(chunk);
                    m_Chunks.Add(chunk);
                }
            }
            else if (polygonPacks.Count < activeChunks.Count)
            {
                int delta = activeChunks.Count - polygonPacks.Count;
                for (int i = 0; i < delta; i++)
                {
                    int j = activeChunks.Count - 1;
                    Destroy(activeChunks[j].gameObject);
                    m_Chunks.Remove(activeChunks[j]);
                    activeChunks.RemoveAt(j);
                }
            }

            for (int i = 0; i < polygonPacks.Count; i++)
            {
                var chunk = activeChunks[i];
                var polygons = polygonPacks[i];
                chunk.polygons = polygons;
                UpdateChunk(chunk);
            }
        }

        void Dig_PhysicsModeOn(Vector2[] clip)
        {
            Bounds worldClipBounds = PolygonUtility.GetBounds(clip);

            foreach (var terrainChunk in m_Chunks.ToArray())
            {
                Matrix4x4 worldToLocal = terrainChunk.transform.worldToLocalMatrix;

                Vector3 min = worldToLocal.MultiplyPoint(worldClipBounds.min);
                Vector3 max = worldToLocal.MultiplyPoint(worldClipBounds.max);
                (min.x, max.x) = (Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x));
                (min.y, max.y) = (Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y));
                min.z = -1;
                max.z = 1;
                Bounds localClipBounds = new Bounds();
                localClipBounds.SetMinMax(min, max);

                Bounds chunkBounds = PolygonUtility.GetBounds(terrainChunk.polygons[0]);
                if (!localClipBounds.Intersects(chunkBounds)) continue;


                Vector2[] localClip = PolygonUtility.Transform(clip, worldToLocal);
                if (!PolygonClipping.TryClip(terrainChunk.polygons, localClip, out var clipedPolygons)) continue;

                if (!m_EnableHoles)
                {
                    clipedPolygons.RemoveAll(e => PolygonUtility.IsClockwise(e));
                }

                clipedPolygons = PolygonSimplifier.Simplify(clipedPolygons, m_Simplification);
                clipedPolygons.RemoveAll(e => e.Length < 3);

                if (clipedPolygons.Count == 0)
                {
                    Destroy(terrainChunk.gameObject);
                    m_Chunks.Remove(terrainChunk);
                    continue;
                }

                List<Vector2> anchors = new List<Vector2>(terrainChunk.anchors);
                bool hasRigidbody = terrainChunk.rigidbody != null;
                terrainChunk.anchors.Clear();

                List<List<Vector2[]>> polygonPacks = PolygonUtility.Pack(clipedPolygons.ToArray());

                Chunk chunk = terrainChunk;
                for (int i = 0; i < polygonPacks.Count; i++)
                {
                    if (i > 0)
                    {
                        chunk = CreateChunk();
                        m_Chunks.Add(chunk);

                        chunk.transform.position = terrainChunk.transform.position;
                        chunk.transform.rotation = terrainChunk.transform.rotation;
                        chunk.transform.localScale = terrainChunk.transform.localScale;

                        if (hasRigidbody && chunk.rigidbody == null)
                        {
                            chunk.rigidbody = chunk.gameObject.AddComponent<Rigidbody2D>();
                            chunk.rigidbody.useAutoMass = true;
                        }
                    }

                    var polygons = polygonPacks[i];
                    chunk.polygons = polygons;
                    UpdateChunk(chunk);

                    if (chunk.rigidbody == null)
                    {
                        foreach (var anchor in anchors)
                        {
                            if (PolygonUtility.IsPointInside(anchor, polygons[0]))
                            {
                                if (m_EnableHoles)
                                {
                                    bool insideHoles = false;

                                    for (int j = 1; j < polygons.Count; j++)
                                    {
                                        if (insideHoles |= PolygonUtility.IsPointInside(anchor, polygons[j]))
                                            break;
                                    }

                                    if (!insideHoles) chunk.anchors.Add(anchor);
                                }
                                else chunk.anchors.Add(anchor);
                            }
                        }

                        if (chunk.anchors.Count == 0 && chunk.rigidbody == null)
                        {
                            chunk.rigidbody = chunk.gameObject.AddComponent<Rigidbody2D>();
                            chunk.rigidbody.useAutoMass = true;
                        }
                    }
                }
            }
        }

        void Fill_PhysicsModeOn(Vector2[] clip)
        {

            Bounds worldClipBounds = PolygonUtility.GetBounds(clip);

            List<Chunk> activeChunks = new List<Chunk>();
            List<Vector2[]> activePolygons = new List<Vector2[]>();

            foreach (var chunk in m_Chunks)
            {
                Matrix4x4 world2local = chunk.transform.worldToLocalMatrix;

                Bounds localClipBounds = new Bounds();
                Vector3 min = world2local.MultiplyPoint(worldClipBounds.min);
                Vector3 max = world2local.MultiplyPoint(worldClipBounds.max);
                (min.x, max.x) = (Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x));
                (min.y, max.y) = (Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y));
                min.z = -1;
                max.z = 1;
                localClipBounds.SetMinMax(min, max);

                Bounds chunkBounds = PolygonUtility.GetBounds(chunk.polygons[0]);
                if (chunkBounds.Intersects(localClipBounds))
                {
                    activeChunks.Add(chunk);
                    activePolygons.AddRange(chunk.polygons);

                    Matrix4x4 local2world = world2local.inverse;
                    foreach (var polygon in chunk.polygons)
                    {
                        PolygonUtility.DoTransform(polygon, local2world);
                    }
                }
            }

            if (activeChunks.Count == 0)
            {
                Chunk chunk = CreateChunk();
                chunk.polygons = new List<Vector2[]>() { PolygonUtility.Transform(clip, transform.worldToLocalMatrix) };
                UpdateChunk(chunk);
                m_Chunks.Add(chunk);
                return;
            }


            if (!PolygonClipping.TryClip(activePolygons, clip, out var clipedPolygons))
            {
                foreach (var chunk in activeChunks)
                {
                    Matrix4x4 world2local = chunk.transform.worldToLocalMatrix;
                    foreach (var polygon in chunk.polygons)
                    {
                        PolygonUtility.DoTransform(polygon, world2local);
                    }
                }

                return;
            }

            if (!m_EnableHoles)
            {
                List<Vector2[]> holes = new List<Vector2[]>();
                clipedPolygons.RemoveAll(e =>
                {
                    if (PolygonUtility.IsClockwise(e))
                    {
                        holes.Add(e);
                        return true;
                    }
                    return false;
                });

                clipedPolygons.RemoveAll(p => holes.Exists(h => PolygonUtility.IsPointInside(p[0], h)));
                m_Chunks.RemoveAll(c =>
                {
                    if (holes.Exists(h => PolygonUtility.IsPointInside(c.polygons[0][0], h)))
                    {
                        activeChunks.Remove(c);
                        Destroy(c.gameObject);
                        return true;
                    }
                    return false;
                });
            }


            //Remove unchanged chunks
            activeChunks.RemoveAll(chunk =>
            {
                int i = clipedPolygons.IndexOf(chunk.polygons[0]);
                if (i != -1)
                {
                    if (m_EnableHoles)
                    {
                        if (chunk.polygons.TrueForAll(p => clipedPolygons.Contains(p)))
                        {
                            clipedPolygons.RemoveAll(p => chunk.polygons.Contains(p));
                            Matrix4x4 world2local = chunk.transform.worldToLocalMatrix;
                            foreach (var polygon in chunk.polygons)
                            {
                                PolygonUtility.DoTransform(polygon, world2local);
                            }
                            return true;
                        }
                    }
                    else
                    {
                        clipedPolygons.RemoveAt(i);
                        PolygonUtility.DoTransform(chunk.polygons[0], chunk.transform.worldToLocalMatrix);
                        return true;
                    }
                }
                return false;
            });

            clipedPolygons = PolygonSimplifier.Simplify(clipedPolygons, m_Simplification);
            clipedPolygons.RemoveAll(e => e.Length < 3);

            var polygonPacks = PolygonUtility.Pack(clipedPolygons.ToArray());



            List<Vector2> activeAnchors = new List<Vector2>();

            foreach (var chunk in activeChunks)
            {
                Matrix4x4 matrix = chunk.transform.localToWorldMatrix;

                foreach (var a in chunk.anchors)
                {
                    activeAnchors.Add(matrix.MultiplyPoint(a));
                }
                chunk.anchors.Clear();
            }


            if (polygonPacks.Count > activeChunks.Count)
            {
                int delta = polygonPacks.Count - activeChunks.Count;
                for (int i = 0; i < delta; i++)
                {
                    Chunk chunk = CreateChunk();
                    activeChunks.Add(chunk);
                    m_Chunks.Add(chunk);
                }
            }
            else if (polygonPacks.Count < activeChunks.Count)
            {
                int delta = activeChunks.Count - polygonPacks.Count;
                for (int i = 0; i < delta; i++)
                {
                    int j = activeChunks.Count - 1;
                    Destroy(activeChunks[j].gameObject);
                    m_Chunks.Remove(activeChunks[j]);
                    activeChunks.RemoveAt(j);
                }
            }

            for (int i = 0; i < polygonPacks.Count; i++)
            {
                var chunk = activeChunks[i];
                var polygons = polygonPacks[i];


                Matrix4x4 worldToLocal = chunk.transform.worldToLocalMatrix;

                foreach (var anchor in activeAnchors)
                {
                    if (PolygonUtility.IsPointInside(anchor, polygons[0]))
                    {
                        bool insideHoles = false;
                        for (int j = 1; j < polygons.Count; j++)
                        {
                            if (insideHoles = PolygonUtility.IsPointInside(anchor, polygons[j])) break;
                        }

                        if (!insideHoles) chunk.anchors.Add(worldToLocal.MultiplyPoint(anchor));
                    }
                }

                if (chunk.anchors.Count > 0)
                {
                    if (chunk.rigidbody != null) Destroy(chunk.rigidbody);


                    Matrix4x4 localToWorld = transform.worldToLocalMatrix * worldToLocal.inverse;
                    List<Vector2> anchors = chunk.anchors;
                    for (int j = 0; j < chunk.anchors.Count; j++)
                    {
                        anchors[j] = localToWorld.MultiplyPoint(anchors[j]);
                    }
                    worldToLocal = transform.worldToLocalMatrix;
                    chunk.transform.localPosition = Vector3.zero;
                    chunk.transform.localRotation = Quaternion.identity;
                    chunk.transform.localScale = Vector3.one;
                }
                else if (chunk.rigidbody == null)
                {
                    chunk.rigidbody = chunk.gameObject.AddComponent<Rigidbody2D>();
                }

                foreach (var polygon in polygons)
                {
                    PolygonUtility.DoTransform(polygon, worldToLocal);
                }

                chunk.polygons = polygons;
                UpdateChunk(chunk);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (m_Chunks == null) return;

            foreach (var chunk in m_Chunks)
            {
                if (m_ColliderOffset > 0)
                {
                    for (int i = 0; i < chunk.polygons.Count; i++)
                    {
                        Vector2[] polygon = chunk.polygons[i];
                        polygon = PolygonUtility.Transform(polygon, chunk.transform.localToWorldMatrix);
                        GizmosUtility.DrawPolygon(polygon, Color.cyan);
                    }
                }

                var collider = chunk.collider;
                for (int i = 0; i < collider.pathCount; i++)
                {
                    Vector2[] polygon = collider.GetPath(i);
                    PolygonUtility.DoTransform(polygon, chunk.transform.localToWorldMatrix);
                    GizmosUtility.DrawPolygon(polygon, Color.green);
                }
            }
        }


        class Chunk
        {
            public List<Vector2[]> polygons;
            public List<Vector2> anchors;
            public GameObject gameObject;
            public Transform transform;
            public Rigidbody2D rigidbody;
            public PolygonCollider2D collider;
            public MeshRenderer meshRenderer;
            public MeshFilter meshFilter;
            public Mesh mesh;
            public float area;
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
       
        /// <summary>
        /// Whether to include transform data in the save file. 
        /// </summary>
        public static bool includePhysicsDataInSave = false;

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
                Debug.LogError($"ReadPolygonTerrainDataFailed: Unsupported Data Version! Please update to DiggableTerrains2D Version {version} or higher.");
                return;
            }
            ReadData_2_1_0(reader);
        }

        void WriteData_2_1_0(BinaryWriter writer)
        {
            writer.Write(m_EnableHoles);
            writer.Write(m_EnablePhysics);
            writer.Write(m_Chunks.Count);
            foreach (var chunk in m_Chunks)
            {
                writer.WritePolygonList(chunk.polygons);
                if (m_EnablePhysics)
                {
                    bool hasRigidbody = chunk.rigidbody != null;
                    writer.Write(hasRigidbody);
                    if (hasRigidbody)
                    {
                        writer.Write(chunk.rigidbody.mass);
                        writer.WriteVector3(chunk.transform.localPosition);
                        writer.WriteVector3(chunk.transform.localEulerAngles);
                    }
                    else
                    {
                        writer.WriteVector2List(chunk.anchors);
                    }
                }
            }
        }

        void ReadData_2_1_0(BinaryReader reader)
        {
            m_EnableHoles = reader.ReadBoolean();
            m_EnablePhysics = reader.ReadBoolean();
            int chunkCount = reader.ReadInt32();

            if(!isBuilt) base.Build();

            DeleteChunks();
            m_Chunks = new List<Chunk>(chunkCount);
            for (int i = 0; i < chunkCount; i++)
            {
                Chunk chunk = CreateChunk();
                chunk.polygons = reader.ReadPolygonList();
                if (m_EnablePhysics)
                {
                    bool hasRigidbody = reader.ReadBoolean();
                    if (hasRigidbody)
                    {
                        chunk.rigidbody = chunk.gameObject.AddComponent<Rigidbody2D>();
                        chunk.rigidbody.mass = reader.ReadSingle();
                        chunk.transform.localPosition = reader.ReadVector3();
                        chunk.transform.localEulerAngles = reader.ReadVector3();
                    }
                    else
                    {
                        chunk.anchors = reader.ReadVector2List();
                    }
                }
                UpdateChunk(chunk);
                m_Chunks.Add(chunk);
            }
        }

        void DeleteChunks()
        {
            if (Application.isPlaying)
            {
                if (m_Chunks != null)
                {
                    foreach (var chunk in m_Chunks)
                    {
                        Destroy(chunk.gameObject);
                    }
                }
            }
            else
            {
                var chunks = GetComponentsInChildren<MeshRenderer>();
                foreach (var chunk in chunks) DestroyImmediate(chunk.gameObject);
            }

            m_Chunks = null;
        }
        #endregion
    }
}