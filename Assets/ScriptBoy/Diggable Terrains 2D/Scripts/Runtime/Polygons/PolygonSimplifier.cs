using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Simplify 2D polygons.
    /// </summary>
    public static class PolygonSimplifier
    {
        public static Vector2[] Simplify(Vector2[] polygon, float threshold, bool avoidIntersection = true)
        {
            return Simplify(polygon, null, threshold, avoidIntersection);
        }

        public static List<Vector2[]> Simplify(List<Vector2[]> polygons, float threshold, bool avoidIntersection = true)
        {
            int n = polygons.Count;
            for (int i = 0; i < n; i++)
            {
                var polygon = polygons[i];
                polygon = Simplify(polygon, polygons, threshold, avoidIntersection);
                polygons[i] = polygon;
            }
            polygons.RemoveAll(p => p.Length < 3);
            return polygons;
        }

        static Vector2[] Simplify(Vector2[] polygon, List<Vector2[]> holes, float threshold, bool avoidIntersection = true)
        {
            bool reverse = false;
            if (PolygonUtility.IsClockwise(polygon))
            {
                PolygonUtility.DoReverse(polygon);
                reverse = true;
            }

            Vert[] verts = InitDataStructure(polygon);
            bool hasHoles = holes != null;

            while (true)
            {
                bool end = true;
                foreach (var vert in verts)
                {
                    if (!vert.dissolved && vert.area < threshold)
                    {
                        if (avoidIntersection)
                        {
                            bool overlap = false;
                            foreach (var v in verts)
                            {
                                if (v.dissolved) continue;
                                if (v == vert || v == vert.prev || v == vert.next) continue;
                                if (vert.OverlapPoint(v.pos))
                                {
                                    overlap = true;
                                    break;
                                }
                            }

                            if (hasHoles && !overlap)
                            {
                                foreach (var hole in holes)
                                {
                                    if (hole == polygon) continue;

                                    foreach (var v in hole)
                                    {
                                        if (vert.OverlapPoint(v))
                                        {
                                            overlap = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (overlap) continue;
                        }

                        vert.Dissolve();
                        end = false;
                    }
                }

                if (end) break;
            }


            List<Vector2> list = new List<Vector2>();
            foreach (var vert in verts)
            {
                if (!vert.dissolved)
                {
                    list.Add(vert.pos);
                }
            }

            if (reverse)
            {
                return PolygonUtility.Reverse(list.ToArray());
            }

            return list.ToArray();
        }

        static Vert[] InitDataStructure(Vector2[] polygon)
        {
            int n = polygon.Length;
            Vert[] verts = new Vert[n];

            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vert();
            }

            for (int i = 0; i < n; i++)
            {
                Vert prev = verts[LoopUtility.LoopIndex(i - 1, n)];
                Vert current = verts[i];
                Vert next = verts[LoopUtility.LoopIndex(i + 1, n)];

                current.prev = prev;
                current.pos = polygon[i];
                current.next = next;
            }

            return verts;
        }

        class Vert
        {
            public Vector2 pos;
            public Vert prev;
            public Vert next;

            public bool dissolved;

            public float area
            {
                get
                {
                    return CalculateTriangleArea(prev.pos, pos, next.pos);
                }
            }

            public void Dissolve()
            {
                prev.next = next;
                next.prev = prev;
                dissolved = true;
            }


            public bool OverlapPoint(Vector2 point)
            {
                return OverlapPoint(prev.pos, pos, next.pos, point);
            }

            float PrepDot(Vector2 lhs, Vector2 rhs)
            {
                return lhs.x * rhs.y - lhs.y * rhs.x;
            }

            bool OverlapPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
            {
                Vector2 ab = b - a;
                Vector2 ca = a - c;

                float l = PrepDot(ab, -ca);
                if (l < 0)
                {
                    (a, c) = (c, a);

                    ab = b - a;
                    ca = a - c;
                }

                Vector2 bc = c - b;

                return (l != 0 && PrepDot(ab, p - a) >= 0 && PrepDot(bc, p - b) >= 0 && PrepDot(ca, p - c) >= 0) ||
                    (PrepDot(ca, p - c) < 0 && GetDistance(p, a, c) < 0.0001f);
            }

            float GetDistance(Vector2 point, Vector2 start, Vector2 end)
            {
                float dot = Vector2.Dot(point - start, end - start);
                float ab = (end - start).sqrMagnitude;
                float t = dot / ab;

                if (t < 0)
                {
                    return float.PositiveInfinity;
                }
                else if (t > 1)
                {
                    return float.PositiveInfinity;
                }

                return Vector2.Distance(start + (end - start) * t, point);
            }

            float CalculateTriangleArea(Vector2 a, Vector2 b, Vector2 c)
            {
                float area = ((a.x * b.y - a.y * b.x) + (b.x * c.y - b.y * c.x) + (c.x * a.y - c.y * a.x)) * 0.5f;
                if (area < 0) return -area;
                return area;
            }
        }
    }
}