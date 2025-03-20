
using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for 2D polygons.
    /// </summary>
    public static class PolygonUtility
    {
        public static Vector2[] CreateBox(Vector2 center, Vector2 size)
        {
            Vector2[] polygon = new Vector2[4];

            size /= 2;
            polygon[0] = new Vector2(size.x, size.y) + center;
            polygon[1] = new Vector2(-size.x, size.y) + center;
            polygon[2] = new Vector2(-size.x, -size.y) + center;
            polygon[3] = new Vector2(size.x, -size.y) + center;

            return polygon;
        }

        public static Vector2[] CreateCircle(Vector2 center, int pointCount, float radius)
        {
            Vector2[] polygon = new Vector2[pointCount];

            Vector2 right = new Vector2(radius, 0);
            float angle = 0;
            float delta = 360f / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                Quaternion q = Quaternion.Euler(0, 0, angle);
                Vector2 v = q * right;
                polygon[i] = center + v;
                angle += delta;
            }

            return polygon;
        }


        public static Vector2[] RoundCorner(Vector2[] polygon, int cornerPointCount, float cornerRadius)
        {
            int polygonLength = polygon.Length;

            Vector2[] verts = new Vector2[polygonLength * (cornerPointCount + 1)];
            int vertIndex = 0;

            for (int i = 0; i < polygonLength; i++)
            {
                Vector2 a = polygon[LoopUtility.PrevIndex(i, polygonLength)];
                Vector2 b = polygon[i];
                Vector2 c = polygon[LoopUtility.NextIndex(i, polygonLength)];

                Vector2 ba = a - b;
                Vector2 bc = c - b;

                float mba = ba.magnitude;
                float mbc = bc.magnitude;

                float rba = Mathf.Min(mba * (0.5f - 0.5f / (cornerPointCount + 1)), cornerRadius);
                float rbc = Mathf.Min(mbc * (0.5f - 0.5f / (cornerPointCount + 1)), cornerRadius);

                a = b + ba * (rba / mba);
                c = b + bc * (rbc / mbc);

                for (int j = 0; j <= cornerPointCount; j++)
                {
                    float t = (float)j / cornerPointCount;
                    Vector2 p = BezierUtility.Evaluate(a, b, c, t);
                    verts[vertIndex] = p;
                    vertIndex++;
                }
            }

            return verts;
        }

        public static Vector2[] RoundCornerPro(Vector2[] polygon, int cornerPointCount, float cornerRadius)
        {
            int polygonLength = polygon.Length;
            List<Vector2> verts = new List<Vector2>(polygonLength);

            for (int i = 0; i < polygonLength; i++)
            {
                Vector2 a = polygon[LoopUtility.PrevIndex(i, polygonLength)];
                Vector2 b = polygon[i];
                Vector2 c = polygon[LoopUtility.NextIndex(i, polygonLength)];

                Vector2 ba = a - b;
                Vector2 bc = c - b;

                float interiorAngle = Vector2.Angle(-ba, -bc);
                int n = (int)((1 - interiorAngle / 180f) * cornerPointCount);

                if (n > 2)
                {
                    float mba = ba.magnitude;
                    float mbc = bc.magnitude;

                    float rba = Mathf.Min(mba * (0.5f - 0.5f / n), cornerRadius);
                    float rbc = Mathf.Min(mbc * (0.5f - 0.5f / n), cornerRadius);

                    a = b + ba * (rba / mba);
                    c = b + bc * (rbc / mbc);

                    for (int j = 0; j <= n; j++)
                    {
                        float t = (float)j / n;
                        Vector2 p = Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), t);
                        verts.Add(p);
                    }
                }
                else
                {
                    verts.Add(b);
                }
            }

            return verts.ToArray();
        }

        public static Vector2[] BevelCorner(Vector2[] polygon, float cornerRadius, bool keepAllPoints = false)
        {
            int polygonLength = polygon.Length;
            List<Vector2> list = new List<Vector2>(polygonLength);
            for (int i = 0; i < polygonLength; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[LoopUtility.LoopIndex(i + 1, polygonLength)];
                Vector2 c = polygon[LoopUtility.LoopIndex(i + 2, polygonLength)];

                float r = Mathf.Min((a - b).magnitude * 0.459f, (c - b).magnitude * 0.459f, cornerRadius * 1.5f);

                a = b + (a - b).normalized * r;
                c = b + (c - b).normalized * r;

                list.Add(a);
                if (keepAllPoints) list.Add(b);
                list.Add(c);
            }

            return list.ToArray();
        }

        public static Vector2[] BevelCornerPro(Vector2[] polygon, float cornerRadius, bool keepAllPoints = false)
        {
            int polygonLength = polygon.Length;
            List<Vector2> list = new List<Vector2>(polygonLength);
            for (int i = 0; i < polygonLength; i++)
            {
                Vector2 a = polygon[LoopUtility.PrevIndex(i, polygonLength)];
                Vector2 b = polygon[i];
                Vector2 c = polygon[LoopUtility.NextIndex(i, polygonLength)];

                Vector2 ab = b - a;
                Vector2 cb = b - c;

                float interiorAngle = Vector2.Angle(ab, cb);
                if (interiorAngle < 160)
                {
                    float r = Mathf.Min((a - b).magnitude * 0.459f, (c - b).magnitude * 0.459f, cornerRadius * 1.5f);

                    a = b + (a - b).normalized * r;
                    c = b + (c - b).normalized * r;

                    list.Add(a);
                    if (keepAllPoints) list.Add(b);
                    list.Add(c);
                }
                else
                {
                    list.Add(b);
                }
            }

            return list.ToArray();
        }


        public static Vector2[] Remesh(Vector2[] polygon, float edgeLength, bool keepCorners)
        {
            int positionCount = polygon.Length;
            int capacity = positionCount + Mathf.FloorToInt(GetPerimeter(polygon) / edgeLength);
            List<Vector2> list = new List<Vector2>(capacity);

            float edge = 0;
            float perimeter = 0;

            for (int i = 0; i < positionCount; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[LoopUtility.NextIndex(i, positionCount)];
                float ab = Vector2.Distance(a, b);
                float prevPerimeter = perimeter;
                perimeter += ab;

                if (keepCorners) list.Add(a);
                if (keepCorners && ab < edgeLength)
                {
                    edge += ab;
                }
                else
                {
                    while (edge <= perimeter)
                    {
                        if (edge != 0)
                        {
                            float t = Mathf.InverseLerp(prevPerimeter, perimeter, edge);
                            Vector2 p = Vector2.Lerp(a, b, t);
                            if (!keepCorners || p != b)
                                list.Add(p);
                        }
                        edge += edgeLength;
                    }
                }
            }

            if (list.Count < 3) list.Clear();
            return list.ToArray();
        }

        public static Vector2[] Extrude(Vector2[] polygon, float value)
        {
            int positionCount = polygon.Length;
            Vector2[] output = new Vector2[positionCount];

            for (int i = 0; i < positionCount; i++)
            {
                Vector2 a = polygon[LoopUtility.PrevIndex(i, positionCount)];
                Vector2 b = polygon[i];
                Vector2 c = polygon[LoopUtility.NextIndex(i, positionCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;

                ab.Normalize();
                bc.Normalize();
                ab = VectorUtility.GetNormal(ab);
                bc = VectorUtility.GetNormal(bc);
                Vector2 n = (ab + bc) / 2;
                n.Normalize();
                //  Debug.DrawLine(b, b + n * value);
                output[i] = b + n * value;
            }

            return output;
        }



        public static Vector2[] Wave(Vector2[] polygon, float waveLength, float waveAmplitude, int seed)
        {
            int positionCount = polygon.Length;
            Vector2[] output = new Vector2[positionCount];

            var prevRandomState = Random.state;
            Random.InitState(seed);

            for (int i = 0; i < positionCount; i++)
            {
                Vector2 a = polygon[LoopUtility.PrevIndex(i, positionCount)];
                Vector2 b = polygon[i];
                Vector2 c = polygon[LoopUtility.NextIndex(i, positionCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;

                ab.Normalize();
                bc.Normalize();
                ab = VectorUtility.GetNormal(ab);
                bc = VectorUtility.GetNormal(bc);
                Vector2 n = (ab + bc) / 2;
                n.Normalize();


                float noise = Mathf.PerlinNoise(b.x / waveLength + 10000, b.y / waveLength + 10000);
                output[i] = b - n * noise * waveAmplitude;
            }

            Random.state = prevRandomState;
            return output;
        }

        public static Vector2[] Reverse(Vector2[] polygon)
        {
            Vector2[] output = polygon.Clone() as Vector2[];
            DoReverse(output);
            return output;
        }

        public static Vector2[] Transform(Vector2[] polygon, Matrix4x4 matrix)
        {
            Vector2[] output = polygon.Clone() as Vector2[];
            DoTransform(output, matrix);
            return output;
        }

        public static Vector2[] Offset(Vector2[] polygon, Vector2 Offset)
        {
            Vector2[] output = polygon.Clone() as Vector2[];
            DoOffset(output, Offset);
            return output;
        }

        public static Vector2[] Clockwise(Vector2[] polygon)
        {
            Vector2[] output = polygon.Clone() as Vector2[];
            DoClockwise(output);
            return output;
        }


        public static List<List<Vector2[]>> Pack(Vector2[][] polygons)
        {
            List<List<Vector2[]>> packs = new List<List<Vector2[]>>();
            List<Vector2[]> kids = new List<Vector2[]>();

            foreach (var polygon in polygons)
            {
                if (IsClockwise(polygon))
                {
                    kids.Add(polygon);
                }
                else
                {
                    List<Vector2[]> pack = new List<Vector2[]>();
                    pack.Add(polygon);
                    packs.Add(pack);
                }
            }

            int parentCount = packs.Count;
            int[] insideTests = new int[parentCount];
            for (int i = 0; i < parentCount; i++)
            {
                int inside = 0;
                Vector3 pos = packs[i][0][0];
                for (int j = 0; j < parentCount; j++)
                {
                    if (i == j) continue;
                    if (IsPointInside(pos, packs[j][0])) inside++;
                }
                insideTests[i] = inside;
            }


            int kidCount = kids.Count;
            for (int i = 0; i < kidCount; i++)
            {
                Vector3 pos = kids[i][0];
                int packIndex = -1;

                for (int j = 0; j < parentCount; j++)
                {
                    if (IsPointInside(pos, packs[j][0]))
                    {
                        if (packIndex == -1 || insideTests[j] > insideTests[packIndex])
                        {
                            packIndex = j;
                        }
                    }
                }

                if (packIndex != -1)
                {
                    packs[packIndex].Add(kids[i]);
                }
            }

            return packs;
        }


        public static void DoSort(Vector2[][] polygons)
        {
            int count = polygons.Length;
            int[] insideTests = new int[count];
            for (int i = 0; i < count; i++)
            {
                int inside = 0;
                Vector3 pos = polygons[i][0];
                for (int j = 0; j < count; j++)
                {
                    if (i == j) continue;
                    if (IsPointInside(pos, polygons[j])) inside++;
                }
                insideTests[i] = inside;
            }
            System.Array.Sort(insideTests, polygons);
        }

        public static void DoClockwise(Vector2[] polygon)
        {
            if (!IsClockwise(polygon)) DoReverse(polygon);
        }

        public static void DoReverse(Vector2[] polygon)
        {
            Vector2 tmp;
            for (int low = 0, high = polygon.Length - 1; low < high; low++, high--)
            {
                tmp = polygon[high];
                polygon[high] = polygon[low];
                polygon[low] = tmp;
            }
        }

        public static void DoTransform(Vector2[] polygon, Matrix4x4 matrix)
        {
            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                polygon[i] = matrix.MultiplyPoint3x4(polygon[i]);
            }
        }

        public static void DoOffset(Vector2[] polygon, Vector2 Offset)
        {
            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                polygon[i] += Offset;
            }
        }

        public static void DoTransform(Vector2[][] polygons, Matrix4x4 matrix)
        {
            foreach (var polygon in polygons)
            {
                int count = polygon.Length;
                for (int i = 0; i < count; i++)
                {
                    polygon[i] = matrix.MultiplyPoint3x4(polygon[i]);
                }
            }
        }



        public static Vector2[] GetNormals(Vector2[] polygon)
        {
            int positionCount = polygon.Length;
            Vector2[] output = new Vector2[positionCount];
            for (int i = 0; i < positionCount; i++)
            {
                Vector2 a = polygon[LoopUtility.PrevIndex(i, positionCount)];
                Vector2 b = polygon[i];
                Vector2 c = polygon[LoopUtility.NextIndex(i, positionCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;

                ab.Normalize();
                bc.Normalize();
                ab = VectorUtility.GetNormal(ab);
                bc = VectorUtility.GetNormal(bc);
                Vector2 n = (ab + bc) / 2;
                n.Normalize();
                output[i] = n;
            }

            return output;
        }

        public static Bounds GetBounds(Vector2[][] polygons, float padding = 0.1f)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity);

            foreach (var polygon in polygons)
            {
                for (int i = 0; i < polygon.Length; i++)
                {
                    if (min.x > polygon[i].x)
                    {
                        min.x = polygon[i].x;
                    }

                    if (min.y > polygon[i].y)
                    {
                        min.y = polygon[i].y;
                    }

                    if (max.x < polygon[i].x)
                    {
                        max.x = polygon[i].x;
                    }

                    if (max.y < polygon[i].y)
                    {
                        max.y = polygon[i].y;
                    }
                }
            }

            min -= Vector3.one * padding;
            max += Vector3.one * padding;

            Vector3 size = (min - max);
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = 1;

            return new Bounds((min + max) / 2, size);
        }

        public static Bounds GetBounds(Vector2[] polygon, float padding = 0.1f)
        {
            Vector3 min = polygon[0];
            Vector3 max = polygon[0];

            for (int i = 0; i < polygon.Length; i++)
            {
                if (min.x > polygon[i].x)
                {
                    min.x = polygon[i].x;
                }

                if (min.y > polygon[i].y)
                {
                    min.y = polygon[i].y;
                }

                if (max.x < polygon[i].x)
                {
                    max.x = polygon[i].x;
                }

                if (max.y < polygon[i].y)
                {
                    max.y = polygon[i].y;
                }
            }

            min -= Vector3.one * padding;
            max += Vector3.one * padding;

            Vector3 size = (min - max);
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = 1;

            return new Bounds((min + max) / 2, size);
        }

        public static Bounds[] GetBoundsOfEdges(Vector2[] polygon)
        {
            int n = polygon.Length;
            Bounds[] bounds = new Bounds[n];

            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[LoopUtility.NextIndex(i, n)];
                bounds[i] = LineUtility.GetBounds(a, b, 0.1f);
                a = b;
            }
            return bounds;
        }

        public static Vector2 GetCenter(Vector2[] polygon)
        {
            Vector2 center = Vector2.zero;
            int positionCount = polygon.Length;
            for (int i = 0; i < positionCount; i++)
            {
                center += polygon[i];
            }
            return center / positionCount;
        }

        public static float GetArea(Vector2[] polygon)
        {
            int pointCount = polygon.Length;
            float sum = 0;
            for (int a = pointCount - 1, b = 0; b < pointCount; a = b++)
            {
                sum += polygon[a].x * polygon[b].y - polygon[b].x * polygon[a].y;
            }
            return Mathf.Abs(sum / 2);
        }

        public static float GetArea(Vector2[][] polygons)
        {
            float sum = 0;

            foreach (var polygon in polygons)
            {
                int n = polygon.Length;
                for (int a = n - 1, b = 0; b < n; a = b++)
                {
                    sum += polygon[a].x * polygon[b].y - polygon[b].x * polygon[a].y;
                }
            }

            return Mathf.Abs(sum / 2);
        }

        public static float GetPerimeter(Vector2[] polygon)
        {
            int pointCount = polygon.Length;
            float perimeter = 0;
            for (int a = pointCount - 1, b = 0; b < pointCount; a = b++)
            {
                perimeter += Vector2.Distance(polygon[a], polygon[b]);
            }
            return perimeter;
        }

        public static float GetMinEdgeLength(Vector2[] polygon)
        {
            float min = float.PositiveInfinity;
            int n = polygon.Length;
            Vector2 prev = polygon[n - 1];
            for (int i = 0; i < n; i++)
            {
                Vector2 current = polygon[i];
                float dis = Vector2.Distance(prev, current);
                if (dis < min) min = dis;
                prev = current;
            }
            return min;
        }

        public static bool GetSelfIntersections(Vector2[] polygon, out Vector2[] intersections)
        {
            List<Vector2> list = new List<Vector2>();
            int pCount = polygon.Length;

            for (int i = 0; i < pCount; i++)
            {
                int iNext = LoopUtility.NextIndex(i, pCount);
                Vector2 a = polygon[i];
                Vector2 b = polygon[iNext];

                for (int j = 0; j < pCount; j++)
                {
                    if (j == i) continue;

                    int jNext = LoopUtility.NextIndex(j, pCount);
                    Vector2 c = polygon[j];
                    Vector2 d = polygon[LoopUtility.NextIndex(j, pCount)];

                    if (LineUtility.IntersectLines(a, b, c, d, out Vector2 hit))
                    {
                        if (iNext == j && hit == b) continue;
                        if (jNext == i && hit == a) continue;

                        list.Add(hit);
                    }
                }
            }

            intersections = list.ToArray();
            return intersections.Length > 0;
        }







        public static bool IsSelfOverlpaing(Vector2[] polygon, float minDis)
        {
            int pCount = polygon.Length;
            for (int i = 0; i < pCount; i++)
            {
                Vector2 a = polygon[i];

                if (IsPointOnPerimeter(polygon[i], polygon, minDis))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsSelfIntersecting(Vector2[] polygon)
        {
            int pCount = polygon.Length;
            for (int i = 0; i < pCount; i++)
            {
                int iNext = LoopUtility.NextIndex(i, pCount);
                Vector2 a = polygon[i];
                Vector2 b = polygon[iNext];

                for (int j = 0; j < pCount; j++)
                {
                    if (j == i) continue;

                    int jNext = LoopUtility.NextIndex(j, pCount);
                    Vector2 c = polygon[j];
                    Vector2 d = polygon[LoopUtility.NextIndex(j, pCount)];

                    if (LineUtility.IntersectLines(a, b, c, d, out Vector2 hit))
                    {
                        if (iNext == j && hit == b) continue;
                        if (jNext == i && hit == a) continue;

                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsClockwise(params Vector2[] polygon)
        {
            int pCount = polygon.Length;
            float sum = 0;
            for (int i = 0; i < pCount; i++)
            {
                int next = LoopUtility.NextIndex(i, pCount);
                sum += (polygon[next].x - polygon[i].x) * (polygon[next].y + polygon[i].y);
            }
            return sum > 0;
        }

        public static bool IsPointInside(Vector2 point, Vector2[] polygon)
        {
            int polygonLength = polygon.Length, i = 0;
            double pointX = point.x, pointY = point.y;
            double startX, startY, endX, endY;
            Vector2 endPoint = polygon[polygonLength - 1];
            endX = endPoint.x;
            endY = endPoint.y;

            bool output = false;
            bool output2 = false;
            while (i < polygonLength)
            {
                startX = endX; startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.x;
                endY = endPoint.y;

                output ^= (endY > pointY ^ startY > pointY) &&
                    ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));

                output2 ^= (startY > pointY ^ endY > pointY) &&
                    ((pointX - startX) < (pointY - startY) * (endX - startX) / (endY - startY));
            }
            return output || output2;
        }

        public static bool IsPointOnPerimeter(Vector2 point, Vector2[] polygon, float minDis)
        {
            int n = polygon.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[LoopUtility.NextIndex(i, n)];

                if (LineUtility.GetPointDistance(point, a, b) < minDis) return true;
            }
            return false;
        }

        public static bool IsPolygonInsidePolygon(Vector2[] a, Vector2[] b)
        {
            int pCount = a.Length;
            for (int i = 0; i < pCount; i++)
            {
                if (!IsPointInside(a[i], b)) return false;
            }
            return true;
        }
    }
}