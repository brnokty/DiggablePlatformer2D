using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for generating a mesh from a polyline.
    /// </summary>
    public static class PolylineMeshUtility
    {
        public static void CreateEdgeMesh(MeshData mesh, int submesh, Vector2[] polyline, float height, float offset)
        {
            Vector2 firstNormal = VectorUtility.GetNormal(polyline[1] - polyline[0]);
            Vector2 lastNormal = VectorUtility.GetNormal(polyline[1] - polyline[0]);
            CreateEdgeMesh(mesh, submesh, polyline, firstNormal, lastNormal, height, offset);
        }

        public static void CreateEdgeMesh(MeshData mesh, int submesh, Vector2[] polyline, Vector2 firstNormal, Vector2 lastNormal, float height, float offset)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.subMeshs[submesh];

            height *= 0.5f;
            int vertOffset = vertices.Count;
            int polyCount = polyline.Length;
            {
                Vector2 p = polyline[0];
                Vector2 normal = firstNormal;
                Vector3 a = p - normal * (height - offset);
                Vector3 b = p + normal * (height + offset);
                a.z = 0;
                b.z = 0.01f;

                vertices.Add(a);
                vertices.Add(b);
                normals.Add(normal);
                normals.Add(normal);
            }

            for (int i = 0; i < polyCount - 2; i++)
            {
                Vector2 a = polyline[i];
                Vector2 b = polyline[LoopUtility.NextIndex(i, polyCount)];
                Vector2 c = polyline[LoopUtility.LoopIndex(i + 1, polyCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;

                Vector2 normal = -VectorUtility.GetNormal((ab.normalized + bc.normalized) / 2).normalized;

                Vector3 v0 = b - normal * (height - offset);
                Vector3 v1 = b + normal * (height + offset);

                v0.z = 0;
                v1.z = 0.01f;

                vertices.Add(v0);
                vertices.Add(v1);
                normals.Add(normal);
                normals.Add(normal);
            }

            {
                Vector2 p = polyline[polyCount - 1];
                Vector2 normal = lastNormal;

                Vector3 a = p - normal * (height - offset);
                Vector3 b = p + normal * (height + offset);
                a.z = 0;
                b.z = 0.01f;
                vertices.Add(a);
                vertices.Add(b);
                normals.Add(normal);
                normals.Add(normal);
            }

            for (int i = 0; i < polyCount - 1; i++)
            {
                int j = i * 2 + vertOffset;

                triangles.Add(j);
                triangles.Add(j + 2);
                triangles.Add(j + 1);

                triangles.Add(j + 1);
                triangles.Add(j + 2);
                triangles.Add(j + 3);
            }
        }
    }
}