using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for 2D polylines.
    /// </summary>
    public static class PolylineUtility
    {
        public static Vector2[] RoundCorner(Vector2[] polyline, int cornerPointCount, float cornerRadius)
        {
            int polygonLength = polyline.Length;
            List<Vector2> verts = new List<Vector2>(polygonLength);

            for (int i = 0; i < polygonLength; i++)
            {
                if (i != 0 && i != polygonLength - 1)
                {
                    Vector2 a = polyline[LoopUtility.PrevIndex(i, polygonLength)];
                    Vector2 b = polyline[i];
                    Vector2 c = polyline[LoopUtility.NextIndex(i, polygonLength)];

                    Vector2 ba = a - b;
                    Vector2 bc = c - b;

                    float mba = ba.magnitude;
                    float mbc = bc.magnitude;

                    float fba = i == 1 ? 1 : 0.5f;
                    float fbc = i == polygonLength - 2 ? 1 : 0.5f;

                    float rba = Mathf.Min(mba * (fba - 0.5f / (cornerPointCount + 1)), cornerRadius);
                    float rbc = Mathf.Min(mbc * (fbc - 0.5f / (cornerPointCount + 1)), cornerRadius);

                    a = b + ba * (rba / mba);
                    c = b + bc * (rbc / mbc);

                    for (int j = 0; j <= cornerPointCount; j++)
                    {
                        float t = (float)j / cornerPointCount;
                        Vector2 p = BezierUtility.Evaluate(a, b, c, t);
                        verts.Add(p);
                    }
                }
                else verts.Add(polyline[i]);
            }

            return verts.ToArray();
        }

        public static Vector2[] RoundCornerPro(Vector2[] polyline, int cornerPointCount, float cornerRadius)
        {
            int polygonLength = polyline.Length;
            List<Vector2> verts = new List<Vector2>(polygonLength);

            for (int i = 0; i < polygonLength; i++)
            {
                if (i != 0 && i != polygonLength - 1)
                {
                    Vector2 a = polyline[LoopUtility.PrevIndex(i, polygonLength)];
                    Vector2 b = polyline[i];
                    Vector2 c = polyline[LoopUtility.NextIndex(i, polygonLength)];
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
                    else verts.Add(b);
                }
                else verts.Add(polyline[i]);
            }

            return verts.ToArray();
        }


        public static Vector2[] BevelCorner(Vector2[] polyline, float cornerRadius, bool keepAllPoints = false)
        {
            int polygonLength = polyline.Length;
            List<Vector2> verts = new List<Vector2>(polygonLength);
            for (int i = 0; i < polygonLength; i++)
            {
                if (i != 0 && i != polygonLength - 1)
                {
                    Vector2 a = polyline[i];
                    Vector2 b = polyline[LoopUtility.LoopIndex(i + 1, polygonLength)];
                    Vector2 c = polyline[LoopUtility.LoopIndex(i + 2, polygonLength)];

                    float r = Mathf.Min((a - b).magnitude * 0.459f, (c - b).magnitude * 0.459f, cornerRadius * 1.5f);

                    a = b + (a - b).normalized * r;
                    c = b + (c - b).normalized * r;

                    verts.Add(a);
                    if (keepAllPoints) verts.Add(b);
                    verts.Add(c);
                }
                else verts.Add(polyline[i]);
            }

            return verts.ToArray();
        }

        public static Vector2[] BevelCornerPro(Vector2[] polyline, float cornerRadius, bool keepAllPoints = false)
        {
            int polygonLength = polyline.Length;
            List<Vector2> verts = new List<Vector2>(polygonLength);
            for (int i = 0; i < polygonLength; i++)
            {
                if (i != 0 && i != polygonLength - 1)
                {
                    Vector2 a = polyline[LoopUtility.PrevIndex(i, polygonLength)];
                    Vector2 b = polyline[i];
                    Vector2 c = polyline[LoopUtility.NextIndex(i, polygonLength)];

                    Vector2 ab = b - a;
                    Vector2 cb = b - c;

                    float interiorAngle = Vector2.Angle(ab, cb);
                    if (interiorAngle < 160)
                    {
                        float r = Mathf.Min((a - b).magnitude * 0.459f, (c - b).magnitude * 0.459f, cornerRadius * 1.5f);

                        a = b + (a - b).normalized * r;
                        c = b + (c - b).normalized * r;

                        verts.Add(a);
                        if (keepAllPoints) verts.Add(b);
                        verts.Add(c);
                    }
                    else
                    {
                        verts.Add(b);
                    }
                }
                else verts.Add(polyline[i]);
            }

            return verts.ToArray();
        }


        public static Vector2[] CreatePolygon(Vector2[] polyline, float thickness, int capPoints)
        {
            int pCount = polyline.Length;
            Vector2[] output = new Vector2[pCount * 2 + capPoints * 2];

            Vector2 normal = Vector2.zero;
            for (int i = 0; i < pCount; i++)
            {
                Vector2 a = polyline[LoopUtility.PrevIndex(i, pCount)];
                Vector2 b = polyline[i];
                Vector2 c = polyline[LoopUtility.NextIndex(i, pCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;

                normal = ab;

                ab.Normalize();
                bc.Normalize();
                ab = VectorUtility.GetNormal(ab);
                bc = VectorUtility.GetNormal(bc);
                Vector2 n = (ab + bc) / 2;

                if (i == 0) n = bc;
                if (i == pCount - 1) n = ab;
                n.Normalize();

                output[i] = b - n * thickness;
            }

            Vector2 right = new Vector2(thickness, 0);
            normal *= -1;
            float angle = Mathf.Atan2(normal.x, -normal.y) * Mathf.Rad2Deg;
            float delta = 180f / (capPoints + 1);
            angle += delta;
            for (int i = 0; i < capPoints; i++)
            {
                Quaternion q = Quaternion.Euler(0, 0, angle);
                Vector2 v = q * right;
                output[pCount + i] = polyline[pCount - 1] + v;
                angle += delta;
            }

            for (int i = 0; i < pCount; i++)
            {
                Vector2 a = polyline[LoopUtility.PrevIndex(i, pCount)];
                Vector2 b = polyline[i];
                Vector2 c = polyline[LoopUtility.NextIndex(i, pCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;



                ab.Normalize();
                bc.Normalize();
                ab = VectorUtility.GetNormal(ab);
                bc = VectorUtility.GetNormal(bc);
                Vector2 n = (ab + bc) / 2;

                if (i == 0) n = bc;
                if (i == pCount - 1) n = ab;
                n.Normalize();

                output[pCount - i + pCount - 1 + capPoints] = b + n * thickness;
            }


            normal = polyline[0] - polyline[1];
            normal *= -1;
            angle = Mathf.Atan2(normal.x, -normal.y) * Mathf.Rad2Deg;
            angle += delta;
            for (int i = 0; i < capPoints; i++)
            {
                Quaternion q = Quaternion.Euler(0, 0, angle);
                Vector2 v = q * right;
                output[pCount * 2 + capPoints + i] = polyline[0] + v;
                angle += delta;
            }

            List<Vector2> list = new List<Vector2>(output);


            return list.ToArray();
        }

        public static Vector2[] CreatePolygon(Vector2[] polyline, float thickness)
        {
            int positionCount = polyline.Length;
            Vector2[] output = new Vector2[positionCount * 2];

            for (int i = 0; i < positionCount; i++)
            {
                output[i] = polyline[i];
            }

            for (int i = 0; i < positionCount; i++)
            {
                Vector2 a = polyline[LoopUtility.PrevIndex(i, positionCount)];
                Vector2 b = polyline[i];
                Vector2 c = polyline[LoopUtility.NextIndex(i, positionCount)];

                Vector2 ab = b - a;
                Vector2 bc = c - b;

                ab.Normalize();
                bc.Normalize();
                ab = VectorUtility.GetNormal(ab);
                bc = VectorUtility.GetNormal(bc);
                Vector2 n = (ab + bc) / 2;
                n.Normalize();

                output[positionCount - i + positionCount - 1] = b + n * thickness;
            }

            List<Vector2> list = new List<Vector2>(output);

            Vector2 p0 = output[0];
            Vector2 p1 = output[positionCount * 2 - 1];
            list.Insert(positionCount, p0);
            list.Insert(positionCount + 1, p1);
            //

            return list.ToArray();
        }
    }
}