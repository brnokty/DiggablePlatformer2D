using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    static class PolygonBorderTracing
    {
        public static bool selfIntersection;
        public static bool directionChange;
        public static bool intersectionVertDoubled;
        public static List<int> doubledVertIndexList;

        class PolygonBorderTracingFailed : UnityException
        {
            public PolygonBorderTracingFailed(string msg) : base(msg) { }
        }

        class Vert
        {
            public Vector2 pos;

            public Vert prev;
            public Vert next;
            public Vert neighbor;

            public float intersectionDis;
            public bool isIntersection;
            public int used;
        }

        public static Vector2[] Trace(Vector2[] polygon)
        {
            polygon = polygon.Clone() as Vector2[];

            OffsetDegeneratePoints(polygon);

            Vert[] verts = ConvertVectorsToVerts(polygon);


            int intersectionCount = DoIntersection(verts, polygon);

            selfIntersection = intersectionCount > 0;
            directionChange = false;
            intersectionVertDoubled = false;
            doubledVertIndexList = new List<int>();


            if (intersectionCount == 0) return polygon;

            Vert entryVert = verts[GetTopmostPointIndex(polygon)];

            return DoTracing(entryVert, verts.Length, intersectionCount);
        }

        public static void OffsetDegeneratePoints(Vector2[] polygon)
        {
            if (PolygonUtility.GetMinEdgeLength(polygon) < 0.001f)
            {
                throw new PolygonBorderTracingFailed("The verts are too close!");
            }

            Random.InitState(0);
            int n = polygon.Length;
            int safety = n * n;

            Bounds[] bounds = PolygonUtility.GetBoundsOfEdges(polygon);

            while (true)
            {
                bool fix = true;

                for (int i = 0; i < n; i++)
                {
                    Vector2 p = polygon[i];

                    for (int j = 0; j < n; j++)
                    {
                        if (j == i) continue;
                        int jNext = LoopUtility.NextIndex(j, n);
                        if (jNext == i) continue;

                        Bounds box = bounds[j];

                        Vector2 a = polygon[j];
                        Vector2 b = polygon[jNext];

                        if (box.Contains(p) && LineUtility.IsPointOnLineCheckedByDistance(p, a, b, 0.001f))
                        {
                            Vector2 normal = VectorUtility.GetNormal(b - a);
                            polygon[i] = p + normal * 0.1f;
                            fix = false;
                            break;
                        }
                    }
                }

                if (fix) break;

                if (safety-- == 0)
                {
                    throw new PolygonBorderTracingFailed("Endless loop when offsetting degenerate points!");
                }
            }
        }


        static int GetTopmostPointIndex(Vector2[] polygon)
        {
            int n = polygon.Length;
            int index = 0;
            float yMax = float.NegativeInfinity;

            for (int i = 0; i < n; i++)
            {
                float y = polygon[i].y;
                if (yMax < y)
                {
                    yMax = y;
                    index = i;
                }
            }
            return index;
        }

        static int DoIntersection(Vert[] verts, Vector2[] polygon)
        {
            Bounds[] bounds = PolygonUtility.GetBoundsOfEdges(polygon);

            int intersectionCount = 0;
            int n = polygon.Length;
            for (int i = 0; i < n; i++)
            {
                int iPrev = LoopUtility.PrevIndex(i, n);
                int iNext = LoopUtility.NextIndex(i, n);

                Vector3 p2 = polygon[i];
                Vector3 p3 = polygon[iNext];
                Bounds iBounds = bounds[i];
                for (int j = i; j < n; j++)
                {
                    if (j == iPrev) continue;
                    if (j == i) continue;
                    if (j == iNext) continue;

                    Bounds jBounds = bounds[j];
                    if (!iBounds.Intersects(jBounds)) continue;

                    int jNext = LoopUtility.NextIndex(j, n);

                    Vector2 p0 = polygon[j];
                    Vector2 p1 = polygon[jNext];

                    bool p2hit = LineUtility.IsPointOnLineCheckedByDistance(p2, p0, p1, 0.001f);
                    bool p3hit = LineUtility.IsPointOnLineCheckedByDistance(p3, p0, p1, 0.001f);

                    if (p2hit && p3hit) continue;

                    Vector2 hit;
                    bool intersection;
                    if (!p2hit && !p3hit)
                    {
                        intersection = LineUtility.IntersectLines(p0, p1, p2, p3, out hit);
                    }
                    else if (p2hit)
                    {
                        intersection = true;
                        hit = p2;
                    }
                    else//if (p3h)
                    {
                        intersection = true;
                        hit = p3;
                    }


                    if (intersection)
                    {
                        Vert intersectionVertA = new Vert();
                        intersectionVertA.pos = hit;
                        intersectionVertA.intersectionDis = Vector3.Distance(p2, hit);
                        intersectionVertA.isIntersection = true;

                        Vert intersectionVertB = new Vert();
                        intersectionVertB.pos = hit;
                        intersectionVertB.intersectionDis = Vector3.Distance(p0, hit);
                        intersectionVertB.isIntersection = true;

                        intersectionVertA.neighbor = intersectionVertB;
                        intersectionVertB.neighbor = intersectionVertA;

                        InsertIntersectionVert(verts[i], intersectionVertA);
                        InsertIntersectionVert(verts[j], intersectionVertB);

                        intersectionCount++;
                    }
                }
            }

            return intersectionCount;
        }

        static Vector2[] DoTracing(Vert start, int vertCount, int intersectionVertCount)
        {
            Vert current = start;
            bool forward = !PolygonUtility.IsClockwise(current.prev.pos, current.pos, current.next.pos);
            List<Vector2> pointList = new List<Vector2>();
            Vector2 prevPos = current.pos + Vector2.right * 100;

            int safety = vertCount + intersectionVertCount * 2;
            while (true)
            {

                if (safety-- == 0)
                {
                    throw new PolygonBorderTracingFailed("Endless Loop!");
                }

                current.used++;
                Vector2 pos = current.pos;
                if (pos != prevPos)
                {
                    pointList.Add(current.pos);
                    prevPos = pos;
                }

                if (current.isIntersection)
                {

                    if (current.used > 1)
                    {
                        intersectionVertDoubled = true;
                        doubledVertIndexList.Add(pointList.Count - 1);
                        doubledVertIndexList.Add(pointList.IndexOf(current.pos));
                    }

                    Vector3 a = current.pos;
                    Vector3 b = current.next.pos;

                    current = current.neighbor;


                    Vector3 c = current.prev.pos;
                    Vector3 d = current.next.pos;


                    bool newForward = !forward ^ LineUtility.GetPointSide(a, b, d) > 0;

                    if (forward != newForward)
                    {
                        directionChange = true;
                        forward = newForward;
                    }
                }

                current = forward ? current.next : current.prev;

                if (current == start)
                {
                    return pointList.ToArray();
                }
            }
        }

        static Vert[] ConvertVectorsToVerts(Vector2[] polygon)
        {
            int n = polygon.Length;
            Vert[] verts = new Vert[n];

            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vert();
            }

            for (int i = 0; i < n; i++)
            {
                Vert current = verts[i];
                current.pos = polygon[i];
                current.prev = verts[LoopUtility.PrevIndex(i, n)];
                current.next = verts[LoopUtility.NextIndex(i, n)];
            }

            return verts;
        }

        static void InsertIntersectionVert(Vert vert, Vert intersectionVert)
        {
            float dis = intersectionVert.intersectionDis;

            while (vert.next.isIntersection && vert.next.intersectionDis < dis)
            {
                vert = vert.next;
            }

            intersectionVert.next = vert.next;
            vert.next.prev = intersectionVert;
            vert.next = intersectionVert;
            intersectionVert.prev = vert;
        }
    }
}