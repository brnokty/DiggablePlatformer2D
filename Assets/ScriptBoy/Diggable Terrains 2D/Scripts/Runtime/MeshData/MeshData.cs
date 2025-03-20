using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// A class to store mesh data.
    /// </summary>
    [System.Serializable]
    public class MeshData
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Color> colors;
        public List<Vector2>[] uvChannels;
        public List<int>[] subMeshs;

        public MeshData(int subMeshCount, int uvChannelCount)
        {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            colors = new List<Color>();

            subMeshs = new List<int>[subMeshCount];
            for (int i = 0; i < subMeshCount; i++)
            {
                subMeshs[i] = new List<int>();
            }

            uvChannels = new List<Vector2>[uvChannelCount];
            for (int i = 0; i < uvChannelCount; i++)
            {
                uvChannels[i] = new List<Vector2>();
            }
        }

        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            colors.Clear();
            foreach (var subMesh in subMeshs) subMesh.Clear();
            foreach (var uvChannel in uvChannels) uvChannel.Clear();
        }

        public void CopyToMesh(Mesh mesh)
        {
            mesh.Clear();
            mesh.subMeshCount = subMeshs.Length;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);

            for (int i = 0; i < subMeshs.Length; i++)
            {
                mesh.SetTriangles(subMeshs[i], i);
            }

            for (int i = 0; i < uvChannels.Length; i++)
            {
                mesh.SetUVs(i, uvChannels[i]);
            }
        }
    }
}