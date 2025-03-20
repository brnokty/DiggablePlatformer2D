using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    static class PolygonClipping
    {
        class PolygonClippingAlgorithmError : UnityException
        {
            public PolygonClippingAlgorithmError(string msg) : base(msg) { }
        }

        class Vert
        {
            public Vector2 pos;

            public Vert next;
            public Vert neighbor;

            public float intersectionDis;
            public bool isIntersection;
            public bool isUsed;
        }

        static Vert[] ConvertVectorsToVerts(Vector2[] polygon)
        {
            int n = polygon.Length;
            Vert[] verts = new Vert[n];

            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vert();
            }

            int iPrev = n - 1;
            for (int i = 0; i < n; i++)
            {
                Vert prev = verts[iPrev];
                prev.pos = polygon[iPrev];
                prev.next = verts[i];
                iPrev = i;
            }

            return verts;
        }

        static bool s_Changed;

        public static bool TryClip(List<Vector2[]> polygons, Vector2[] clip, out List<Vector2[]> clipedPolygons)
        {
            s_Changed = false;
            clipedPolygons = Clip(polygons, clip);
            return s_Changed;
        }

        public static List<Vector2[]> Clip(List<Vector2[]> polygons, Vector2[] clip)
        {
            clip = PolygonUtility.Extrude(clip, 0.01f);

            bool fillMode = !PolygonUtility.IsClockwise(clip);

            bool[][] boundsCheckArrays = GetBoundsCheckArrays(polygons, clip);

            OffsetDegeneratePoints(polygons, clip, boundsCheckArrays);

            List<Vector2[]> output = new List<Vector2[]>();

            Vert[] entryVerts = GetEntryVerts(polygons, clip, boundsCheckArrays, output, fillMode);

            if (entryVerts.Length == 0) return output;

            ConnectVerts(entryVerts, output);

            return output;
        }

        static bool[][] GetBoundsCheckArrays(List<Vector2[]> polygons, Vector2[] clip)
        {
            Bounds clipBounds = PolygonUtility.GetBounds(clip, 1f);
            int polygonCount = polygons.Count;
            bool[][] boundsCheckArrays = new bool[polygonCount][];

            for (int i = 0; i < polygonCount; i++)
            {
                Vector2[] polygon = polygons[i];
                int pointCount = polygon.Length;
                bool[] boundsCheckArray = new bool[pointCount];
                boundsCheckArrays[i] = boundsCheckArray;

                int jPrev = pointCount - 1;
                for (int j = 0; j < pointCount; j++)
                {
                    Bounds bounds = LineUtility.GetBounds(polygon[jPrev], polygon[j]);
                    boundsCheckArray[jPrev] = !bounds.Intersects(clipBounds);
                    jPrev = j;
                }
            }

            return boundsCheckArrays;
        }

        static void OffsetDegeneratePoints(List<Vector2[]> polygons, Vector2[] clip, bool[][] boundsCheckArrays)
        {
            /*
            if (PolygonUtility.GetMinEdgeLength(clip) < 0.05f)
            {
                throw new PolygonClippingAlgorithmError("The clip verts are too close!");
            }
            */

            Random.InitState(0);
            Vector2[] normals = null;
            int n = clip.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = clip[i];

                while (IsPointOnPolygonsPerimeter(p, polygons, boundsCheckArrays, 0.001f))
                {
                    if (normals == null) normals = PolygonUtility.GetNormals(clip);

                    Quaternion q = Quaternion.Euler(0, 0, Random.Range(-30, 30));
                    p += (Vector2)(q * normals[i] * 0.005f);
                    clip[i] = p;
                }
            }
        }

        static bool IsPointOnPolygonsPerimeter(Vector2 p, List<Vector2[]> polygons, bool[][] boundsCheckArrays, float dis)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                if (IsPointOnPolygonPerimeter(p, polygons[i], boundsCheckArrays[i], dis))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsPointOnPolygonPerimeter(Vector2 point, Vector2[] polygon, bool[] boundsChecks, float dis)
        {
            float sqrDis = dis * dis;

            float px = point.x;
            float py = point.y;

            int n = polygon.Length;
            int iPrev = n - 1;

            for (int i = 0; i < n; i++)
            {
                if (!boundsChecks[i])
                {
                    float ax = polygon[iPrev].x;
                    float ay = polygon[iPrev].y;

                    float bx = polygon[i].x;
                    float by = polygon[i].y;

                    float dotAPBP = (px - ax) * (bx - ax) + (py - ay) * (by - ay);
                    float sqrAB = (bx - ax) * (bx - ax) + (by - ay) * (by - ay);
                    float t = dotAPBP / sqrAB;

                    if (t <= 0 && (px - ax) * (px - ax) + (py - ay) * (py - ay) < sqrDis) return true;
                    if (t >= 1 && (px - bx) * (px - bx) + (py - by) * (py - by) < sqrDis) return true;

                    float x = ax + (bx - ax) * t;
                    float y = ay + (by - ay) * t;

                    if (t < 1 && (px - x) * (px - x) + (py - y) * (py - y) < sqrDis) return true;
                }

                iPrev = i;
            }

            return false;
        }



        static Vert[] GetEntryVerts(List<Vector2[]> polygons, Vector2[] clip, bool[][] boundsCheckArrays, List<Vector2[]> output, bool fillMode)
        {
            List<Vert> entryVerts = new List<Vert>();

            Vert[] clipVerts = ConvertVectorsToVerts(clip);

            int polygonCount = polygons.Count;
            int nClip = clip.Length;
            int intersectionCount = 0;

            for (int polygonIndex = 0; polygonIndex < polygonCount; polygonIndex++)
            {
                Vector2[] points = polygons[polygonIndex];
                bool[] boundsCheckArray = boundsCheckArrays[polygonIndex];
                Vert[] verts = ConvertVectorsToVerts(points);
                int pointCount = points.Length;
                bool noIntersection = true;

                for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                {
                    if (boundsCheckArray[pointIndex]) continue;

                    int pointIndexNext = LoopUtility.LoopIndex(pointIndex + 1, points.Length);
                    Vector2 p2 = points[pointIndex];
                    Vector2 p3 = points[pointIndexNext];

                    for (int clipIndex = 0; clipIndex < nClip; clipIndex++)
                    {
                        int iBNext = LoopUtility.LoopIndex(clipIndex + 1, clip.Length);
                        Vector2 p0 = clip[clipIndex];
                        Vector2 p1 = clip[iBNext];

                        bool p2hit = LineUtility.IsPointOnLineCheckedByDistance(p2, p0, p1);
                        bool p3hit = LineUtility.IsPointOnLineCheckedByDistance(p3, p0, p1);

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
                            bool isEntry;

                            if (p2hit)
                            {
                                isEntry = LineUtility.GetPointSide(p3, p0, p1) > 0;

                                if (!isEntry) continue;

                                // case V and A
                                Vector2 p2Prev = points[LoopUtility.LoopIndex(pointIndex - 1, pointCount)];
                                if (!LineUtility.IsPointOnLineCheckedByDistance(p2Prev, p0, p1))
                                {
                                    if (LineUtility.GetPointSide(p2Prev, p0, p1) > 0) continue;
                                }
                            }
                            else
                            if (p3hit)
                            {
                                isEntry = LineUtility.GetPointSide(p2, p0, p1) < 0;

                                if (isEntry) continue;

                                Vector2 p3next = points[LoopUtility.LoopIndex(pointIndex + 2, pointCount)];
                                if (!LineUtility.IsPointOnLineCheckedByDistance(p3next, p0, p1))
                                {
                                    if (LineUtility.GetPointSide(p3next, p0, p1) > 0) continue;
                                }
                            }
                            else
                            {
                                isEntry = Vector2.Dot(p1 - p0, VectorUtility.GetNormal(p3 - p2)) > 0;
                            }

                            noIntersection = false;

                            Vert intersectionVertA = new Vert();
                            intersectionVertA.pos = hit;
                            intersectionVertA.intersectionDis = Vector2.Distance(p2, hit);
                            intersectionVertA.isIntersection = true;

                            Vert intersectionVertB = new Vert();
                            intersectionVertB.pos = hit;
                            intersectionVertB.intersectionDis = Vector2.Distance(p0, hit);
                            intersectionVertB.isIntersection = true;

                            intersectionVertA.neighbor = intersectionVertB;
                            intersectionVertB.neighbor = intersectionVertA;

                            InsertIntersectionVert(verts[pointIndex], intersectionVertA);
                            InsertIntersectionVert(clipVerts[clipIndex], intersectionVertB);

                            if (isEntry ^ fillMode) entryVerts.Add(intersectionVertA);

                            intersectionCount++;
                        }
                    }
                }

                if (noIntersection)
                {
                    if (!PolygonUtility.IsPolygonInsidePolygon(points, clip))
                    {
                        output.Add(points);
                    }
                    else s_Changed = true;
                }
            }

            if (intersectionCount % 2 != 0)
            {
                Debug.Break();
                throw new PolygonClippingAlgorithmError("IntersectionCount %  2 != 0");
            }

            if (intersectionCount == 0)
            {
                int insideCount = 0;
                for (int i = 0; i < polygonCount; i++)
                {
                    Vector2[] polygon = polygons[i];
                    if (PolygonUtility.IsPointInside(clip[0], polygon)) insideCount++;
                }

                if (!fillMode && insideCount % 2 != 0)
                {
                    output.Add(clip);
                    s_Changed = true;
                }

                if (fillMode && insideCount % 2 == 0)
                {
                    output.Add(clip);
                    s_Changed = true;
                }
            }
            else s_Changed = true;

            return entryVerts.ToArray();
        }

        static void InsertIntersectionVert(Vert currentVert, Vert intersectionVert)
        {
            float dis = intersectionVert.intersectionDis;

            while (currentVert.next.isIntersection && currentVert.next.intersectionDis < dis)
            {
                currentVert = currentVert.next;
            }

            intersectionVert.next = currentVert.next;
            currentVert.next = intersectionVert;
        }

        static void ConnectVerts(Vert[] entryVerts, List<Vector2[]> output)
        {
            Vert start = FindUnusedVert(entryVerts);
            Vert current = start;
            List<Vector2> polygon = new List<Vector2>();
            Vector2 prevPos = current.pos + Vector2.right * 100;

            while (true)
            {
                Vector2 pos = current.pos;
                if (pos != prevPos)
                {
                    polygon.Add(current.pos);
                    prevPos = pos;
                }

                if (current.isIntersection)
                {
                    current.isUsed = true;
                    current = current.neighbor;
                    current.isUsed = true;
                }

                current = current.next;

                if (current == start)
                {
                    int n = polygon.Count;
                    if (n > 2)
                    {
                        pos = start.pos;

                        if (pos == prevPos)
                        {
                            polygon.RemoveAt(n - 1);
                            n--;
                        }

                        Vector2[] poly = polygon.ToArray();
                        if (n > 2)
                        {
                            n = polygon.Count;
                            output.Add(poly);
                        }
                    }

                    start = FindUnusedVert(entryVerts);

                    if (start == null) break;

                    current = start;
                    polygon = new List<Vector2>();
                    prevPos = current.pos + Vector2.right;
                }
            }
        }

        static Vert FindUnusedVert(Vert[] verts)
        {
            foreach (var vert in verts)
            {
                if (!vert.isUsed)
                {
                    return vert;
                }
            }
            return null;
        }
    }
}