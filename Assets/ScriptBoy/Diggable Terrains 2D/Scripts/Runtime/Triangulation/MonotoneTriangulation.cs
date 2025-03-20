using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Triangulating 2D polygons using the Monotone Decomposition algorithm.
    /// </summary>
    public class MonotoneTriangulation
    {
        class MonotoneTriangulationFailed : UnityException
        {
            public MonotoneTriangulationFailed() : base("Self-intersecting polygon is not supported!") { }
        }

        public static int[] Triangulate(List<Vector2[]> polygons, out Vector3[] verts)
        {
            var monoton = new MonotoneTriangulation(polygons);
            monoton.Decompose();
            verts = monoton.m_Verts;
            return monoton.Triangulate();
        }

        Vector3[] m_Verts;
        Edge[] m_Edges;

        MonotoneTriangulation(List<Vector2[]> polygons)
        {
            int polygonCount = polygons.Count;
            int vertCount = 0;
            for (int j = 0; j < polygonCount; j++)
            {
                vertCount += polygons[j].Length;
            }

            m_Verts = new Vector3[vertCount];
            m_Edges = new Edge[vertCount];

            int vertOffset = 0;
            for (int j = 0; j < polygonCount; j++)
            {

                AddPolygon(polygons[j], vertOffset);
                vertOffset += polygons[j].Length;
            }
        }

        void AddPolygon(Vector2[] polygon, int vertOffset)
        {
            int pointCount = polygon.Length;

            for (int i = 0; i < pointCount; i++)
            {
                Edge edge = new Edge();

                Vector2 currentP = polygon[i];
                Vector2 nextP = polygon[NextIndex(i, pointCount)];

                if (currentP.y == nextP.y)
                {
                    float sign = nextP.x - currentP.x < 0 ? 0.001f : -0.001f;
                    currentP.y += sign * (1 + LoopIndex(i, 3));
                    polygon[i] = currentP;
                }

                int vertIndex = i + vertOffset;
                edge.vertIndex = vertIndex;
                m_Verts[vertIndex] = currentP;
                m_Edges[vertIndex] = edge;
            }

            for (int i = 0; i < pointCount; i++)
            {
                int prevI = PrevIndex(i, pointCount);
                int nextI = NextIndex(i, pointCount);

                Edge edge = m_Edges[i + vertOffset];
                edge.prev = m_Edges[prevI + vertOffset];
                edge.next = m_Edges[nextI + vertOffset];

                Vector2 a = polygon[prevI];
                Vector2 b = polygon[i];
                Vector2 c = polygon[nextI];

                bool isConvex = GetInteriorAngle(a, b, c) < 180;
                bool isPrevUp = b.y < a.y;
                bool isNextUp = b.y < c.y;

                VertType vertType;
                if (isConvex && !isPrevUp && !isNextUp)
                {
                    vertType = VertType.Start;
                }
                else if (isConvex && isPrevUp && isNextUp)
                {
                    vertType = VertType.End;
                }
                else if (!isConvex && !isPrevUp && !isNextUp)
                {
                    vertType = VertType.Split;
                }
                else if (!isConvex && isPrevUp && isNextUp)
                {
                    vertType = VertType.Merge;
                }
                else if (isPrevUp && !isNextUp)
                {
                    vertType = VertType.LeftChain;
                }
                else
                {
                    vertType = VertType.RightChain;
                }
                edge.vertType = vertType;
            }
        }

        float GetInteriorAngle(Vector2 a, Vector2 b, Vector2 c)
        {
            float bcx = c.x - b.x;
            float bcy = c.y - b.y;
            float bax = a.x - b.x;
            float bay = a.y - b.y;
            float cos = (bcx * bax + bcy * bay) / Mathf.Sqrt((bcx * bcx + bcy * bcy) * (bax * bax + bay * bay));
            float angle = Mathf.Acos(cos) * Mathf.Rad2Deg;
            if (bcx * bay - bcy * bax < 0) return 360 - angle;
            return angle;
        }

        void Decompose()
        {
            try
            {
                System.Array.Sort(m_Edges, ComparerEdge);
                List<Edge> activeLeftEdges = new List<Edge>();
                for (int i = m_Edges.Length - 1; i >= 0; i--)
                {
                    Edge edge = m_Edges[i];
                    Edge leftMost;

                    switch (edge.vertType)
                    {
                        case VertType.Start:

                            activeLeftEdges.Add(edge);
                            edge.helper = edge;
                            break;

                        case VertType.End:

                            if (edge.prev.helper.vertType == VertType.Merge)
                            {
                                AddDiagonal(edge, edge.prev.helper);
                            }
                            activeLeftEdges.Remove(edge.prev);
                            break;

                        case VertType.Split:

                            leftMost = FindLeftMostEdge(activeLeftEdges, m_Verts[edge.vertIndex]);
                            AddDiagonal(edge, leftMost.helper);
                            leftMost.helper = edge;
                            activeLeftEdges.Add(edge);
                            edge.helper = edge;
                            break;

                        case VertType.Merge:

                            if (edge.prev.helper.vertType == VertType.Merge)
                            {
                                AddDiagonal(edge, edge.prev.helper);
                            }
                            activeLeftEdges.Remove(edge.prev);
                            leftMost = FindLeftMostEdge(activeLeftEdges, m_Verts[edge.vertIndex]);
                            if (leftMost.helper.vertType == VertType.Merge)
                            {
                                AddDiagonal(edge, leftMost.helper);
                            }
                            leftMost.helper = edge;
                            break;

                        case VertType.LeftChain:

                            if (edge.prev.helper.vertType == VertType.Merge)
                            {
                                AddDiagonal(edge, edge.prev.helper);
                            }
                            activeLeftEdges.Remove(edge.prev);
                            activeLeftEdges.Add(edge);
                            edge.helper = edge;
                            break;

                        case VertType.RightChain:

                            leftMost = FindLeftMostEdge(activeLeftEdges, m_Verts[edge.vertIndex]);
                            if (leftMost.helper.vertType == VertType.Merge)
                            {
                                AddDiagonal(edge, leftMost.helper);
                            }
                            leftMost.helper = edge;
                            break;
                    }
                }
            }
            catch
            {
                throw new MonotoneTriangulationFailed();
            }
        }

        Edge FindLeftMostEdge(List<Edge> T, Vector2 point)
        {
            Edge edge = null;
            float minDis = float.PositiveInfinity;
            foreach (var e in T)
            {
                float dis = GetEdgeDistance(e, point);

                if (minDis > dis)
                {
                    minDis = dis;
                    edge = e;
                }
            }
            return edge;
        }

        float GetEdgeDistance(Edge edge, Vector2 point)
        {
            Vector2 start = m_Verts[edge.vertIndex];
            Vector2 end = m_Verts[edge.next.vertIndex];

            float tY = (point.y - start.y) / (end.y - start.y);
            float x = start.x + (end.x - start.x) * tY;

            if (x < point.x && (tY >= 0 && tY <= 1))
            {
                return point.x - x;
            }

            if (start.y == end.y && start.y == point.y)
            {
                if (start.x > end.x)
                {
                    return point.x - end.x;
                }
                return point.x - start.x;
            }

            return float.PositiveInfinity;
        }

        void AddDiagonal(Edge edgeA, Edge edgeB)
        {
            //Debug.DrawLine(verts[edgeA.vertIndex], verts[edgeB.vertIndex], Color.red, 1);
            while (edgeB.prev.twin != null)
            {
                Vector2 a = m_Verts[edgeB.prev.vertIndex];
                Vector2 b = m_Verts[edgeB.vertIndex];
                Vector2 c = m_Verts[edgeB.next.vertIndex];
                Vector2 p = m_Verts[edgeA.vertIndex];

                if (IsPointInsideView(a, b, c, p))
                {
                    break;
                }

                edgeB = edgeB.prev.twin;
            }

            Edge edgeAB = new Edge();
            Edge edgeBA = new Edge();

            edgeAB.diagonal = true;
            edgeBA.diagonal = true;

            edgeAB.twin = edgeBA;
            edgeBA.twin = edgeAB;

            edgeAB.vertIndex = edgeA.vertIndex;
            edgeAB.prev = edgeA.prev;
            edgeAB.next = edgeB;
            edgeA.prev.next = edgeAB;
            edgeA.prev = edgeBA;

            edgeBA.vertIndex = edgeB.vertIndex;
            edgeBA.prev = edgeB.prev;
            edgeBA.next = edgeA;
            edgeB.prev.next = edgeBA;
            edgeB.prev = edgeAB;
        }

        bool IsPointInsideView(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float ax = a.x;
            float bx = b.x;
            float cx = c.x;
            float px = p.x;
            float ay = a.y;
            float by = b.y;
            float cy = c.y;
            float py = p.y;

            bool abp = (bx - ax) * (py - ay) - (by - ay) * (px - ax) > 0;
            bool bcp = (cx - bx) * (py - by) - (cy - by) * (px - bx) > 0;
            bool abc = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax) > 0;
            return abc ? abp && bcp : abp || bcp;
        }

        int ComparerEdge(Edge a, Edge b)
        {
            float ay = m_Verts[a.vertIndex].y;
            float by = m_Verts[b.vertIndex].y;
            if (ay == by)
            {
                ay = m_Verts[a.vertIndex].x;
                by = m_Verts[b.vertIndex].x;
            }
            return ay.CompareTo(by);
        }

        int[] Triangulate()
        {
            List<int> tris = new List<int>();
            List<Edge> edges = new List<Edge>();
            List<Edge> stack = new List<Edge>();

            foreach (var edge in m_Edges)
            {
                if (edge.skip) continue;

                Edge start = edge;
                Edge current = start;
                do
                {
                    edges.Add(current);
                    current.skip = true;
                    current = current.next;
                } while (current != start);

                Triangulate(stack, edges, tris);
                edges.Clear();
                stack.Clear();
            }

            return tris.ToArray();
        }

        void Triangulate(List<Edge> stack, List<Edge> edges, List<int> tris)
        {
            edges.Sort(ComparerEdge);

            stack.Add(edges[0]);
            stack.Add(edges[1]);
            int stackCount = 2;
            int edgeCount = edges.Count;

            for (int i = 2; i < edgeCount; i++)
            {
                Edge edge = edges[i];

                bool isLeftChain = stack[stackCount - 1].next == edge;
                bool isRightChain = edge.next == stack[stackCount - 1];

                if (isLeftChain || isRightChain)
                {
                    stack.Add(edge);
                    stackCount++;

                    do
                    {
                        int i1 = stackCount - 3;
                        int i2 = stackCount - 2;
                        int i3 = stackCount - 1;

                        int a = stack[i1].vertIndex;
                        int b = stack[i2].vertIndex;
                        int c = stack[i3].vertIndex;

                        float x1 = m_Verts[a].x;
                        float x2 = m_Verts[b].x;
                        float x3 = m_Verts[c].x;

                        float y1 = m_Verts[a].y;
                        float y2 = m_Verts[b].y;
                        float y3 = m_Verts[c].y;

                        bool reverse = (x2 - x1) * (y3 - y1) - (y2 - y1) * (x3 - x1) > 0;

                        if (isRightChain ^ reverse)
                        {
                            if (reverse)
                            {
                                int w = a;
                                a = b;
                                b = w;
                            }

                            tris.Add(a);
                            tris.Add(b);
                            tris.Add(c);


                            stack[i2] = stack[i3];
                            stack.RemoveAt(i3);
                            stackCount--;
                        }
                        else break;

                    } while (stackCount >= 3);
                }
                else
                {
                    bool reverse = m_Verts[edge.next.vertIndex].y < m_Verts[edge.vertIndex].y;

                    stackCount--;
                    for (int j = 0; j < stackCount; j++)
                    {
                        int a = stack[j].vertIndex;
                        int b = stack[j + 1].vertIndex;
                        int c = edge.vertIndex;

                        if (reverse)
                        {
                            int w = a;
                            a = b;
                            b = w;
                        }

                        tris.Add(a);
                        tris.Add(b);
                        tris.Add(c);
                    }

                    Edge last = stack[stackCount];
                    stack.Clear();
                    stack.Add(last);
                    stack.Add(edge);
                    stackCount = 2;
                }
            }
        }

        int NextIndex(int i, int n)
        {
            i += 1;
            if (i == n) return 0;
            return i;
        }

        int PrevIndex(int i, int n)
        {
            i -= 1;
            if (i == -1) return n - 1;
            return i;
        }

        int LoopIndex(int value, int m)
        {
            return value - Mathf.FloorToInt((float)value / m) * m;
        }

        enum VertType
        {
            Start, End, Split, Merge, LeftChain, RightChain
        }

        class Edge
        {
            public VertType vertType;
            public int vertIndex;
            public Edge prev;
            public Edge next;
            public Edge twin;
            public Edge helper;
            public bool skip;
            public bool diagonal;
        }
    }
}