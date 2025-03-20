using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for textures.
    /// </summary>
    public static class TextureUtility
    {
        public static Color GetAverageColor(Texture texture)
        {
            Color color = Color.white; ;
            try
            {
                RenderTexture active = RenderTexture.active;
                RenderTexture temp = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, temp);

                var request = UnityEngine.Rendering.AsyncGPUReadback.Request(temp);
                request.WaitForCompletion();
                var data = request.GetData<byte>();
                color = new Color32(data[0], data[1], data[2], data[3]);

                RenderTexture.ReleaseTemporary(temp);
                RenderTexture.active = active;

                return color;
            }
            catch (Exception e)
            {
                Debug.LogWarning("DiggableTerrains2D.TextureUtility.GetAverageColor(): The AsyncGPUReadback.Request() method failed.\n" + e);
                if (texture.isReadable)
                {
                    Color[] colors = (texture as Texture2D).GetPixels();
                    int n = 0;
                    for (int i = 0; i < colors.Length; i++)
                    {
                        if (colors[i].a < 0.5f) continue;
                        n++;
                        color += colors[i];
                    }
                    color /= n;
                    Debug.Log("DiggableTerrains2D.TextureUtility.GetAverageColor(): The Texture2D.GetPixels() method is used instead of the AsyncGPUReadback.Request() method.");
                }

                Debug.LogWarning("DiggableTerrains2D.TextureUtility.GetAverageColor(): Please use Unity 2021.3.14f1 or higher, 2022.1.23f1 or higher, 2022.2.0b15 or higher, 2023.1.0a17 or higher.");
            }
            return color;
        }

        internal static Vector4 GetTextureST(Rect uvRect)
        {
            return new Vector4(1 / uvRect.width, 1 / uvRect.height, -uvRect.x / uvRect.width, -uvRect.y / uvRect.height);
        }

        internal static Texture2D Duplicate(Texture2D texture)
        {
            Texture2D copy = new Texture2D(texture.width, texture.height, texture.format, false);
            Graphics.CopyTexture(texture, copy);
            return copy;
        }

        internal static Color GetPixelFromGPUMemory(Texture2D m_SplatMapTexture, int x, int y)
        {
            var request = UnityEngine.Rendering.AsyncGPUReadback.Request(m_SplatMapTexture, 0, x, 1, y, 1, 0, 1);
            request.WaitForCompletion();
            var data = request.GetData<byte>();
            Color color = new Color32(data[0], data[1], data[2], data[3]);
            return color;
        }

        internal static void GPU2CPU(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            RenderTexture active = RenderTexture.active;

            var temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = temp;
            Graphics.Blit(texture, temp);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.ReleaseTemporary(temp);

            RenderTexture.active = active;
        }

        internal static byte[] GetRawTextureData(Texture2D texture, bool compress)
        {
            byte[] data;

            if (compress)
            {
                int width = texture.width;
                int height = texture.height;

                Texture2D temp = new Texture2D(width, height, TextureFormat.ARGB32, false);
                Graphics.CopyTexture(texture, temp);
                GPU2CPU(temp);
                temp.Compress(true);
                data = temp.GetRawTextureData();

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(temp);
                }
                else UnityEngine.Object.DestroyImmediate(temp);
            }
            else
            {
                data = texture.GetRawTextureData();
            }

            return data;
        }

        internal static void LoadRawTextureData(Texture2D texture, byte[] data, bool compress)
        {
            if (compress)
            {
                int width = texture.width;
                int height = texture.height;
                Texture2D temp = new Texture2D(width, height, TextureFormat.ARGB32, false);
                temp.Compress(true);
                temp.LoadRawTextureData(data);
                temp.Apply();
                Graphics.ConvertTexture(temp, texture);
                UnityEngine.Object.Destroy(temp);
            }
            else
            {
                texture.LoadRawTextureData(data);
            }

        }
    }
}