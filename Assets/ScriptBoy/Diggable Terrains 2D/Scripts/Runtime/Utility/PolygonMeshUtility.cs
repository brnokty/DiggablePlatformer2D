using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for generating a mesh from polygons.
    /// </summary>
    public static class PolygonMeshUtility
    {
        public static void CreateFillMesh(MeshData mesh, int submesh, List<Vector2[]> polygons, bool useDelaunay)
        {
            if (polygons.Count == 1)
            {
                CreateFillMesh(mesh, submesh, polygons[0], useDelaunay);
                return;
            }

            int[] triangles = MonotoneTriangulation.Triangulate(polygons, out Vector3[] verts);
            if (useDelaunay)
            {
                triangles = DelaunayTriangulation.Triangulate(triangles, verts);
            }
            mesh.vertices.AddRange(verts);
            mesh.normals.AddRange(VectorUtility.CreateArray(Vector3.back, verts.Length));
            mesh.subMeshs[submesh].AddRange(triangles);
        }

        public static void CreateFillMesh(MeshData mesh, int submesh, Vector2[] polygon, bool useDelaunay)
        {
            Vector3[] vertices = VectorUtility.ConvertToVector3(polygon);
            int[] triangles = EarClippingTriangulation.Triangulate(polygon);

            if (useDelaunay)
            {
                triangles = DelaunayTriangulation.Triangulate(triangles, vertices);
            }

            mesh.vertices.AddRange(vertices);
            mesh.normals.AddRange(VectorUtility.CreateArray(Vector3.back, polygon.Length));
            mesh.subMeshs[submesh].AddRange(triangles);
        }

        public static void CreateEdgeMesh(MeshData mesh, int submesh, List<Vector2[]> polygons, float height, float offset)
        {
            foreach (var polygon in polygons)
            {
                CreateEdgeMesh(mesh, submesh, polygon, height, offset);
            }
        }

        public static void CreateEdgeMesh(MeshData mesh, int submesh, Vector2[] polygon, float height, float offset)
        {
            int pCount = polygon.Length;
            int vCount = (pCount + 1) * 4;
            Vector3[] vArray = new Vector3[vCount];
            Vector3[] nArray = new Vector3[vCount];
            int[] tArray = new int[pCount * 6];
            height *= 0.5f;
            for (int i = 0; i <= pCount; i++)
            {
                Vector2 a = polygon[LoopUtility.LoopIndex(i - 1, pCount)];
                Vector2 b = polygon[LoopUtility.LoopIndex(i, pCount)];
                Vector2 c = polygon[LoopUtility.LoopIndex(i + 1, pCount)];

                Vector2 tangent = (((c - b).normalized + (b - a).normalized) / 2).normalized;
                Vector2 normal = - VectorUtility.GetNormal(tangent);
        
                Vector3 innerV = b - normal * (height - offset);
                Vector3 outterV = b + normal * (height + offset);

                innerV.z = 0;
                outterV.z = 0.01f;

                vArray[i * 2] = innerV;
                vArray[i * 2 + 1] = outterV;


                nArray[i * 2] = normal;
                nArray[i * 2 + 1] = normal;


                //Debug.DrawRay(innerV, normal / 4, Color.green, 0.2f);
                //Debug.DrawRay(outterV, normal / 4, Color.red, 0.2f);

            //    nArray[i * 2] = new Vector3(normal.x, normal.y, -100).normalized;
               // nArray[i * 2 + 1] = new Vector3(normal.x, normal.y, -1.5f).normalized;
            }

            int tOffset = mesh.vertices.Count;
            for (int j = 0; j < pCount; j++)
            {
                int j6 = j * 6;
                int j2 = j * 2;

                tArray[j6 + 0] = tOffset + j2 + 1;
                tArray[j6 + 1] = tOffset + j2 + 0;
                tArray[j6 + 2] = tOffset + j2 + 2;

                tArray[j6 + 3] = tOffset + j2 + 1;
                tArray[j6 + 4] = tOffset + j2 + 2;
                tArray[j6 + 5] = tOffset + j2 + 3;
            }

            mesh.vertices.AddRange(vArray);
            mesh.normals.AddRange(nArray);
            mesh.subMeshs[submesh].AddRange(tArray);
        }
    }
}