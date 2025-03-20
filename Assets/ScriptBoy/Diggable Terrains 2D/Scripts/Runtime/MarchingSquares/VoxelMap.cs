using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    class VoxelMap
    {
        bool[] m_States;
        Vector2[] m_Edges;
        Vector2[] m_Normals;
        Vector2Int m_Resolution;
        int m_TrueStateCount;

        public Vector2Int resolution => m_Resolution;
        public float overallState => (float)m_TrueStateCount / m_States.Length;
        public int trueStateCount => m_TrueStateCount;
        public bool[] states => m_States;
        public Vector2[] edges => m_Edges;
        public Vector2[] normals => m_Normals;

        public VoxelMap(Vector2Int resolution)
        {
            m_Resolution = resolution;
            m_States = new bool[resolution.x * resolution.y];
            m_Edges = new Vector2[resolution.x * resolution.y];
            m_Normals = new Vector2[(resolution.x + resolution.y) * 2];
        }

        public VoxelMap(Vector2Int resolution, int trueStateCount, bool[] states, Vector2[] edges, Vector2[] normals)
        {
            m_Resolution = resolution;
            m_TrueStateCount = trueStateCount;
            m_States = states;
            m_Edges = edges;
            m_Normals = normals;
        }

        public void SetOverallState(int trueStateCount)
        {
            m_TrueStateCount = trueStateCount;
        }

        public void SetOverallState(bool state)
        {
            m_TrueStateCount = state ? m_States.Length : 0;
        }

        public void RerfreshOverallState()
        {
            m_TrueStateCount = 0;
            foreach (var state in m_States)
            {
                if (state) m_TrueStateCount++;
            }
        }

        public Vector2 GetBoundaryVertNormal(Vector2 p)
        {
            if (p.y == 0)
            {
                return m_Normals[(int)p.x];
            }
            else if (p.y == m_Resolution.y - 1)
            {
                return m_Normals[(int)p.x + m_Resolution.x];
            }

            if (p.x == 0)
            {
                return m_Normals[(int)p.y + m_Resolution.x * 2];
            }
            else if (p.x == m_Resolution.x - 1)
            {
                return m_Normals[(int)p.y + m_Resolution.y + m_Resolution.x * 2];
            }

            return Vector2.zero;
        }

        public void Clear()
        {
            m_TrueStateCount = 0;
            int n = m_States.Length;
            for (int i = 0; i < n; i++)
            {
                m_States[i] = false;
            }
        }

        public bool EditByPolygon(Vector2[] polygon, bool fill)
        {
            Bounds polygonBounds = PolygonUtility.GetBounds(polygon);
            int resX = m_Resolution.x;
            int resY = m_Resolution.y;


            int xMin = Mathf.Clamp(Mathf.FloorToInt(polygonBounds.min.x), 0, resX);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(polygonBounds.max.x), 0, resX);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(polygonBounds.min.y), 0, resY);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(polygonBounds.max.y), 0, resY);

            if (xMin == xMax || yMin == yMax) return false;

            bool hasChanged = false;

            if (xMin == 0 && yMin == 0 && xMax == resX && yMax == resY)
            {
                Vector3 box0;
                box0.x = xMin;
                box0.y = yMin;
                box0.z = 0;

                Vector3 box1;
                box1.x = xMin;
                box1.y = yMax;
                box1.z = 0;

                Vector3 box2;
                box2.x = xMax;
                box2.y = yMax;
                box2.z = 0;

                Vector3 box3;
                box3.x = xMax;
                box3.y = yMin;
                box3.z = 0;

                bool insideTest = PolygonUtility.IsPointInside(box0, polygon);
                insideTest &= PolygonUtility.IsPointInside(box1, polygon);
                insideTest &= PolygonUtility.IsPointInside(box2, polygon);
                insideTest &= PolygonUtility.IsPointInside(box3, polygon);

                if (insideTest)
                {
                    //box1  box2
                    //box0  box3
                    bool intersectionTest = YEdgeCastPolygon(box0.y, box1.y, box0.x, polygon, out float hit, out Vector2 normal);
                    intersectionTest = intersectionTest || XEdgeCastPolygon(box1.x, box2.x, box1.y, polygon, out hit, out normal);
                    intersectionTest = intersectionTest || YEdgeCastPolygon(box3.y, box2.y, box2.x, polygon, out hit, out normal);
                    intersectionTest = intersectionTest || XEdgeCastPolygon(box0.x, box3.x, box3.y, polygon, out hit, out normal);

                    if (!intersectionTest)
                    {
                        int n = resX * resY;
                        for (int i = 0; i < n; i++)
                        {
                            if (m_States[i] != fill)
                            {
                                m_States[i] = fill;
                                hasChanged = true;
                            }
                        }
                        m_TrueStateCount = fill ? n : 0;
                        return hasChanged;
                    }
                }
            }

            int polygonLength = polygon.Length;
            float mx = resX - 1;
            float my = resY - 1;
            float boundaryError = 0.001f;
            for (int i = 0; i < polygonLength; i++)
            {
                Vector2 p = polygon[i];
                bool changed = false;

                if ((p.x <= 0 || p.x >= mx) && Mathf.Round(p.y) == p.y)
                {
                    p.y += boundaryError;
                    changed = true;
                }

                if ((p.y <= 0 || p.y >= my) && Mathf.Round(p.x) == p.x)
                {
                    p.x += boundaryError;
                    changed = true;
                }

                if (changed) polygon[i] = p;
            }



            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    int i = y * resX + x;
                    if (m_States[i] != fill)
                    {
                        Vector3 p;
                        p.x = x;
                        p.y = y;
                        p.z = 0;

                        if (PolygonUtility.IsPointInside(p, polygon))
                        {

                            m_States[i] = fill;
                            m_TrueStateCount += fill ? +1 : -1;


                            m_Edges[i].x = 0;
                            m_Edges[i].y = 0;
                            if (x != 0) m_Edges[y * resX + x - 1].x = 1;
                            if (y != 0) m_Edges[(y - 1) * resX + x].y = 1;
                            hasChanged = true;
                        }
                    }
                }
            }

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    int i = y * resX + x;
                    bool state = m_States[i];
                    float xStart = x;
                    float yStart = y;

                    if (x == xMax - 1 || states[y * resX + x + 1] != state)
                    {
                        float xEdge = m_Edges[i].x;
                        if (state == fill)
                        {
                            if (XEdgeCastPolygon(xStart + xEdge, xStart + 1, yStart, polygon, out float hit, out Vector2 normal))
                            {

                                float ex = hit - xStart;
                                if (x == 0 || x == resX - 2)
                                {
                                   ex = Mathf.Clamp(ex, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].x = ex;
                                hasChanged = true;

                                if (y == 0)
                                {
                                    m_Normals[x] = normal;
                                }
                                else if (y == resY - 1)
                                {
                                    m_Normals[x + resX] = normal;
                                }
                            }
                        }
                        else
                        {
                            if (XEdgeCastPolygon(xStart, xStart + xEdge, yStart, polygon, out float hit, out Vector2 normal))
                            {
                                float ex = hit - xStart;
                                if (x == 0 || x == resX - 2)
                                {
                                    ex = Mathf.Clamp(ex, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].x = ex;
                                hasChanged = true;

                                if (y == 0)
                                {
                                    m_Normals[x] = normal;
                                }
                                else if (y == resY - 1)
                                {
                                    m_Normals[x + resX] = normal;
                                }
                            }
                        }
                    }

                    if (y == yMax - 1 || states[(y + 1) * resX + x] != state)
                    {
                        float yEdge = m_Edges[i].y;

                        if (state == fill)
                        {
                            if (YEdgeCastPolygon(yStart + yEdge, yStart + 1, xStart, polygon, out float hit, out Vector2 normal))
                            {
                                float ey = hit - yStart;
                                if (y == 0 || y == resY - 2)
                                {
                                   ey = Mathf.Clamp(ey, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].y = ey;
                                hasChanged = true;

                                if (x == 0)
                                {
                                    m_Normals[y + resX * 2] = normal;
                                }
                                else if (x == resX - 1)
                                {
                                    m_Normals[y + resY + resX * 2] = normal;
                                }
                            }
                        }
                        else
                        {
                            if (YEdgeCastPolygon(yStart, yStart + yEdge, xStart, polygon, out float hit, out Vector2 normal))
                            {
                                float ey = hit - yStart;
                                if (y == 0 || y == resY - 2)
                                {
                                    ey = Mathf.Clamp(ey, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].y = ey;
                                hasChanged = true;

                                if (x == 0)
                                {
                                    m_Normals[y + resX * 2] = normal;
                                }
                                else if (x == resX - 1)
                                {
                                    m_Normals[y + resY + resX * 2] = normal;
                                }
                            }
                        }
                    }
                }
            }





            return hasChanged;
        }

        public bool EditByCircle(Vector2 position, float radius, bool fill)
        {
            int resX = m_Resolution.x;
            int resY = m_Resolution.y;

            int xMin = Mathf.Clamp(Mathf.FloorToInt(position.x - radius), 0, resX);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(position.x + radius), 0, resX);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(position.y - radius), 0, resY);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(position.y + radius), 0, resY);
            if (xMin == xMax || yMin == yMax) return false;

            bool hasChanged = false;

            if (overallState == 1 && fill) return false;
            if (overallState == 0 && !fill) return false;

            float sqrRadius = radius * radius;
            if (IsInsideCircle(position.x, position.y, sqrRadius))
            {
                for (int i = 0, length = m_States.Length; i < length; i++)
                {
                    if (m_States[i] != fill)
                    {
                        m_States[i] = fill;
                        hasChanged = true;
                    }
                }
                m_TrueStateCount = fill ? m_States.Length : 0;
                return hasChanged;
            }

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    int i = y * resX + x;
                    if (m_States[i] != fill)
                    {
                        float xDelta = x - position.x;
                        float yDelta = y - position.y;
                        if (xDelta * xDelta + yDelta * yDelta < sqrRadius)
                        {
                            m_States[i] = fill;
                            m_TrueStateCount += fill ? +1 : -1;

                            m_Edges[i].x = 0;
                            m_Edges[i].y = 0;
                            i = y * resX + x - 1;
                            if (x != 0) m_Edges[i].x = 1;
                            i = (y - 1) * resX + x;
                            if (y != 0) m_Edges[i].y = 1;

                            hasChanged = true;
                        }
                    }
                }
            }


            float boundaryError = 0.001f;

            Vector2 hitPoint = Vector2.zero;
            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    int i = y * resX + x;

                    bool state = m_States[i];
                    float xStart = x;
                    float yStart = y;

                    if (x == xMax - 1 || states[y * resX + x + 1] != state)
                    {
                        float xEdge = m_Edges[i].x;
                        if (state == fill)
                        {
                            if (XEdgeCastCircle(xStart + xEdge, xStart + 1, yStart, sqrRadius, position, out float hit))
                            {
                                float ex = hit - xStart;
                                if (x == 0 || x == resX - 2)
                                {
                                    ex = Mathf.Clamp(ex, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].x = ex;
                                hasChanged = true;
                                Vector2 normal = position - new Vector2(hit, yStart);

                                if (fill) normal *= -1;

                                if (y == 0)
                                {
                                    m_Normals[x] = normal.normalized;
                                }
                                else if (y == resY - 1)
                                {
                                    m_Normals[x + resX] = normal.normalized;
                                }
                            }
                        }
                        else
                        {
                            if (XEdgeCastCircle(xStart, xStart + xEdge, yStart, sqrRadius, position, out float hit))
                            {
                                float ex = hit - xStart;
                                if (x == 0 || x == resX - 2)
                                {
                                    ex = Mathf.Clamp(ex, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].x = ex;
                                hasChanged = true;
                                Vector2 normal = position - new Vector2(hit, yStart);
                                if (fill) normal *= -1;

                                if (y == 0)
                                {
                                    m_Normals[x] = normal.normalized;
                                }
                                else if (y == resY - 1)
                                {
                                    m_Normals[x + resX] = normal.normalized;
                                }
                            }
                        }
                    }

                    if (y == yMax - 1 || states[(y + 1) * resX + x] != state)
                    {
                        float yEdge = m_Edges[i].y;

                        if (state == fill)
                        {
                            if (YEdgeCastCircle(yStart + yEdge, yStart + 1, xStart, sqrRadius, position, out float hit))
                            {
                                float ey = hit - yStart;
                                if (y == 0 || y == resY - 2)
                                {
                                    ey = Mathf.Clamp(ey, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].y = ey;
                                hasChanged = true;

                                Vector2 normal = position - new Vector2(xStart, hit);

                                if (fill) normal *= -1;

                                if (x == 0)
                                {
                                    m_Normals[y + resX * 2] = normal.normalized;
                                }
                                else if (x == resX - 1)
                                {
                                    m_Normals[y + resY + resX * 2] = normal.normalized;
                                }
                            }
                        }
                        else
                        {
                            if (YEdgeCastCircle(yStart, yStart + yEdge, xStart, sqrRadius, position, out float hit))
                            {
                                float ey = hit - yStart;
                                if (y == 0 || y == resY - 2)
                                {
                                    ey = Mathf.Clamp(ey, boundaryError, 1 - boundaryError);
                                }
                                m_Edges[i].y = ey;
                                hasChanged = true;

                                Vector2 normal = position - new Vector2(xStart, hit);

                                if (fill) normal *= -1;

                                if (x == 0)
                                {
                                    m_Normals[y + resX * 2] = normal.normalized;
                                }
                                else if (x == resX - 1)
                                {
                                    m_Normals[y + resY + resX * 2] = normal.normalized;
                                }
                            }
                        }
                    }
                }
            }

            return hasChanged;
        }

        bool XEdgeCastPolygon(float cx, float dx, float y, Vector2[] polygon, out float hit, out Vector2 normal)
        {
            int n = polygon.Length;
            Vector3 b = polygon[n - 1];
            for (int i = 0; i < n; i++)
            {
                Vector3 a = polygon[i];
                float abY = (b.y - a.y);
                if (abY != 0)//Parallel
                {
                    float tX = (y - a.y) / abY;
                    if (tX >= 0 && tX <= 1)
                    {
                        float abX = b.x - a.x;
                        float x = a.x + abX * tX;
                        if (x >= cx && x <= dx)
                        {
                            hit = x;
                            normal.x = -abY;
                            normal.y = abX;
                            normal.Normalize();
                            return true;
                        }
                    }
                }
                b = a;
            }
            hit = 0;
            normal.x = 0;
            normal.y = 0;
            return false;
        }

        bool YEdgeCastPolygon(float cy, float dy, float x, Vector2[] polygon, out float hit, out Vector2 normal)
        {
            int n = polygon.Length;
            Vector3 b = polygon[n - 1];
            for (int i = 0; i < n; i++)
            {
                Vector3 a = polygon[i];
                float abX = (b.x - a.x);
                if (abX != 0)//Parallel
                {
                    float tY = (x - a.x) / abX;
                    if (tY >= 0 && tY <= 1)
                    {
                        float abY = b.y - a.y;
                        float y = a.y + tY * abY;
                        if (y >= cy && y <= dy)
                        {
                            hit = y;
                            normal.x = -abY;
                            normal.y = abX;
                            normal.Normalize();
                            return true;
                        }
                    }
                }
                b = a;
            }
            hit = 0;
            normal.x = 0;
            normal.y = 0;
            return false;
        }

        bool YEdgeCastCircle(float y0, float y1, float x, float sqrR, Vector2 p, out float hit)
        {
            hit = 0;
            float px = p.x;
            float py = p.y;
            float dx = (x - p.x) * (x - px);
            bool cInside = dx + (y0 - py) * (y0 - py) <= sqrR;
            bool dInside = dx + (y1 - py) * (y1 - py) <= sqrR;
            if (cInside == dInside) return false;

            //(x - px)^2 + (y - py)^2 = r ^ 2
            float root = Mathf.Sqrt(sqrR - dx);
            float y = (cInside ? +1 : -1) * root + py;

            hit = y;
            return true;
        }

        bool XEdgeCastCircle(float x0, float x1, float y, float sqrR, Vector2 p, out float hit)
        {
            hit = 0;
            float px = p.x;
            float py = p.y;
            float dy = (y - py) * (y - py);
            bool cInside = (x0 - p.x) * (x0 - px) + dy <= sqrR;
            bool dInside = (x1 - p.x) * (x1 - px) + dy <= sqrR;
            if (cInside == dInside) return false;

            //(x - px)^2 + (y - py)^2 = r ^ 2
            float root = Mathf.Sqrt(sqrR - dy);
            float x = (cInside ? +1 : -1) * root + px;
            hit = x;
            return true;
        }

        bool LineCastCircle(Vector2 p0, Vector2 p1, Vector2 o, float sqrRadius, ref Vector2 hitPoint)
        {
            Vector2 p0p1 = p1 - p0;
            float a = Vector2.Dot(p0p1, p0p1); if (Mathf.Abs(a) < float.Epsilon) return false;
            Vector2 op1 = p0 - o;
            float b = 2 * Vector2.Dot(p0p1, op1);
            float c = Vector2.Dot(o, o) - 2 * Vector2.Dot(o, p0) + Vector2.Dot(p0, p0) - sqrRadius;
            float delta = b * b - 4 * a * c; if (delta < 0) return false;
            float deltaRoot = Mathf.Sqrt(delta);
            float d = 1 / (2 * a);
            float mu1 = (-b + deltaRoot) * d;
            float mu2 = (-b - deltaRoot) * d;
            hitPoint = p0 + ((op1.sqrMagnitude > sqrRadius && mu1 > mu2) ? mu2 : mu1) * p0p1;
            bool hit = Vector2.Dot(hitPoint - p0, p0p1) > 0 && Vector2.Dot(hitPoint - p1, -p0p1) > 0;
            return hit;
        }

        bool IsInsideCircle(float x, float y, float sqrRadius)
        {
            float resX = m_Resolution.x;
            float resY = m_Resolution.y;
            float dx = x; float dy = y;
            if (dx * dx + dy * dy > sqrRadius) return false;

            dx = x - resX; dy = y;
            if (dx * dx + dy * dy > sqrRadius) return false;

            dx = x; dy = y - resY;
            if (dx * dx + dy * dy > sqrRadius) return false;

            dx = x - resX; dy = y - resY;
            if (dx * dx + dy * dy > sqrRadius) return false;

            return true;
        }
    }
}