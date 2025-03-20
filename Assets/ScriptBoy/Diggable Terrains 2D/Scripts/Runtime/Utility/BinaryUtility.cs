using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace ScriptBoy.DiggableTerrains2D
{
    static class BinaryUtility
    {
        //Rect
        public static void WriteRect(this BinaryWriter writer, Rect rect)
        {
            writer.Write(rect.x);
            writer.Write(rect.y);
            writer.Write(rect.width);
            writer.Write(rect.height);
        }

        public static Rect ReadRect(this BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float w = reader.ReadSingle();
            float h = reader.ReadSingle();
            return new Rect(x, y, w, h);
        }


        //Version
        public static void WriteVersion(this BinaryWriter writer, Version version)
        {
            writer.Write(version.ToString());
        }

        public static Version ReadVersion(this BinaryReader reader)
        {
            return new Version(reader.ReadString());
        }


        //Vector2Int
        public static void WriteVector2Int(this BinaryWriter writer, Vector2Int vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        public static Vector2Int ReadVector2Int(this BinaryReader reader)
        {
            return new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        }


        //VoxelMapArray
        public static void WriteVoxelMapArray(this BinaryWriter writer, VoxelMap[] array)
        {
            int length = array.Length;
            writer.Write(length);
            for (int i = 0; i < length; i++)
            {
                WriteVoxelMap(writer, array[i]);
            }
        }

        public static VoxelMap[] ReadVoxelMapArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            VoxelMap[] array = new VoxelMap[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = ReadVoxelMap(reader);
            }
            return array;
        }


        //VoxelMap
        public static void WriteVoxelMap(this BinaryWriter writer, VoxelMap map)
        {
            Vector2Int resolution = map.resolution;
            int trueStateCount = map.trueStateCount;
            bool[] states = map.states;
            Vector2[] edges = map.edges;
            Vector2[] normals = map.normals;

            WriteVector2Int(writer, resolution);
            writer.Write(trueStateCount);
            WriteBooleanArray(writer, states);
            WriteVector2Array(writer, edges);
            WriteVector2Array(writer, normals);
        }

        public static VoxelMap ReadVoxelMap(this BinaryReader reader)
        {
            Vector2Int resolution = ReadVector2Int(reader);
            int trueStateCount = reader.ReadInt32();
            bool[] states = ReadBooleanArray(reader);
            Vector2[] edges = ReadVector2Array(reader);
            Vector2[] normals = ReadVector2Array(reader);
            return new VoxelMap(resolution, trueStateCount, states, edges, normals);
        }


        //BooleanArray
        public static void WriteBooleanArray(this BinaryWriter writer, bool[] array)
        {
            int length = array.Length;
            writer.Write(length);
            for (int i = 0; i < length; i++)
            {
                writer.Write(array[i]);
            }
        }

        public static bool[] ReadBooleanArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            bool[] array = new bool[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = reader.ReadBoolean();
            }
            return array;
        }


        //PolygonList
        public static void WritePolygonList(this BinaryWriter writer, List<Vector2[]> polygons)
        {
            int polygonCount = polygons.Count;
            writer.Write(polygonCount);
            for (int i = 0; i < polygonCount; i++)
            {
                WriteVector2Array(writer, polygons[i]);
            }
        }

        public static List<Vector2[]> ReadPolygonList(this BinaryReader reader)
        {
            int polygonCount = reader.ReadInt32();
            List<Vector2[]> polygons = new List<Vector2[]>(polygonCount);
            for (int i = 0; i < polygonCount; i++)
            {
                polygons.Add(ReadVector2Array(reader));
            }
            return polygons;
        }


        //Vector2List
        public static void WriteVector2List(this BinaryWriter writer, List<Vector2> list)
        {
            writer.WriteVector2Array(list.ToArray());
        }

        public static List<Vector2> ReadVector2List(this BinaryReader reader)
        {
            return new List<Vector2>(reader.ReadVector2Array());
        }


        //Vector2Array
        public static void WriteVector2Array(this BinaryWriter writer, Vector2[] array)
        {
            int length = array.Length;
            writer.Write(length);
            for (int j = 0; j < length; j++)
            {
                writer.Write(array[j].x);
                writer.Write(array[j].y);
            }
        }

        public static Vector2[] ReadVector2Array(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            Vector2[] array = new Vector2[length];
            for (int j = 0; j < length; j++)
            {
                array[j].x = reader.ReadSingle();
                array[j].y = reader.ReadSingle();
            }
            return array;
        }


        //Vector2
        public static void WriteVector2(this BinaryWriter writer, Vector2 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            Vector2 vector;
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            return vector;
        }


        //Vector2
        public static void WriteVector3(this BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            Vector3 vector;
            vector.x = reader.ReadSingle();
            vector.y = reader.ReadSingle();
            vector.z = reader.ReadSingle();
            return vector;
        }
    }
}