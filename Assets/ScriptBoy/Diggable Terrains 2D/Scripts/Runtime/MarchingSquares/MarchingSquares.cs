using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    class MarchingSquares
    {
        class MarchingSquaresFailed : UnityException
        {
            public MarchingSquaresFailed(string msg) : base(msg) { }
        }

        class Node
        {
            public bool state;

            public Vert vert;
            public Vert rightVert;
            public Vert upVert;

            public Node(float x, float y)
            {
                vert = new Vert(new Vector2(x, y));
                rightVert = new Vert();
                upVert = new Vert();
            }

            public void Refresh(bool state, float edgeRight, float edgeUp)
            {
                this.state = state;

                vert.Refresh();
                rightVert.Refresh();
                upVert.Refresh();

                Vector2 po = vert.position;
                rightVert.position = new Vector2(po.x + edgeRight, po.y);
                upVert.position = new Vector2(po.x, po.y + edgeUp);
            }
        }

        class Vert
        {
            public Vector2 position;
            public Vert prev;
            public Vert next;

            public int index;
            public bool isOutline;
            public int boundaryCase;

            public float area
            {
                get
                {
                    Vector2 a = prev.position;
                    Vector2 b = position;
                    Vector2 c = next.position;

                    float area = 0f;

                    area += (a.x * b.y - a.y * b.x);
                    area += (b.x * c.y - b.y * c.x);
                    area += (c.x * a.y - c.y * a.x);

                    area *= 0.5f;

                    return Mathf.Abs(area);
                }
            }

            public Vert()
            {

            }

            public Vert(Vector2 position)
            {
                this.position = position;
            }

            public void Refresh()
            {
                next = null;
                index = -1;
                isOutline = false;
            }

            public void Dissolve()
            {
                prev.next = next;
                next.prev = prev;
            }
        }


        static MarchingSquares s_Instance;

        const bool k_FixThinConnection = true;//Case6 & Case9
        const float k_ThinConnectionWidth = 0.4f;



        Node[][] m_Nodes;
        Vector2Int m_Res;
        List<Vert> m_OutlineVerts;
        List<int> m_MeshTriangles;
        List<Vector3> m_MeshVerts;
        List<Vector2[]> m_ColliderPaths;
        List<Vector2[]> m_ClosedPaths;
        List<Vector2[]> m_OpenPaths;

        float m_Area;
        float m_XMax;
        float m_YMax;



        public float area => m_Area;
        public List<int> triangles => m_MeshTriangles;
        public List<Vector3> verts => m_MeshVerts;
        public List<Vector2[]> colliderPaths => m_ColliderPaths;
        public List<Vector2[]> closedPaths => m_ClosedPaths;
        public List<Vector2[]> openPaths => m_OpenPaths;

        public static MarchingSquares instance
        {
            get
            {
                if (s_Instance == null) s_Instance = new MarchingSquares();
                return s_Instance;
            }
        }


        MarchingSquares()
        {
            m_OutlineVerts = new List<Vert>();
            m_MeshVerts = new List<Vector3>();
            m_MeshTriangles = new List<int>();

            m_ColliderPaths = new List<Vector2[]>();
            m_ClosedPaths = new List<Vector2[]>();
            m_OpenPaths = new List<Vector2[]>();
        }


        public void ReadMap(VoxelMap map)
        {
            SetMaxRes(map.resolution);

            m_OutlineVerts.Clear();
            m_MeshVerts.Clear();
            m_MeshTriangles.Clear();
            m_ColliderPaths.Clear();
            m_OpenPaths.Clear();
            m_ClosedPaths.Clear();

            if (map.overallState == 0)
            {
                return;
            }

            if (map.overallState == 1)
            {
                Vector2Int from = Vector2Int.zero;
                Vector2Int to = map.resolution;
                Vert[] poly = new Vert[4];
                poly[0] = m_Nodes[from.x][from.y].vert;
                poly[1] = m_Nodes[from.x][to.y - 1].vert;
                poly[2] = m_Nodes[to.x - 1][to.y - 1].vert;
                poly[3] = m_Nodes[to.x - 1][from.y].vert;
                foreach (var v in poly)
                {
                    v.index = -1;
                    v.isOutline = false;
                }
                SaveVerts(poly, 4);
                Triangulate(poly, 4);
                AddBoundaryOutlines(poly, 4);
                m_Area = to.x * to.y;
                return;
            }


            RefreshNodes(map);
            m_Area = 0;
            EnterQuad(Vector2Int.zero, map.resolution);
        }

        public void UpdatePaths(float simplificationThreshold)
        {
            //simplificationThreshold = 0;
            m_ColliderPaths.Clear();
            m_ClosedPaths.Clear();
            m_OpenPaths.Clear();

            List<Vector2> path = new List<Vector2>(m_OutlineVerts.Count);

            foreach (var vert in m_OutlineVerts)
            {
                if (!vert.isOutline) continue;

                bool hasBoundaryVert = false;
                Vert boundaryVert = null;
                var start = vert;
                var follow = start;
                int safe = 10000;
                do
                {
                    safe--;

                    if (safe < 0) throw new MarchingSquaresFailed("Stuck in a loop while finding paths.");

                    follow.isOutline = false;

                    var next = follow.next;
                    var prev = follow.prev;

                    bool isBoundary = IsBoundaryVert(follow);
                    bool isCorner = IsCornerVert(follow);

                    
                    if (!isCorner && follow.area < simplificationThreshold)
                    {
                        if (isBoundary)
                        {
                            if (IsBoundaryVert(next) && IsBoundaryVert(prev) && CompareCorners(prev, next) && CompareCorners(follow, next))
                            {

                            }
                            else goto Add;
                        }
                        else if (IsBoundaryVert(next) || IsBoundaryVert(prev)) goto Add;

                        if (next == start) break;
                        if (next.next == prev)
                        {
                            next.isOutline = false;
                            prev.isOutline = false;
                            break;
                        }

                        if (path.Count == 0) start = next;
                        follow.Dissolve();
                        follow = next;

                        if (!isBoundary && IsBoundaryVert(next) && IsBoundaryVert(prev) && CompareCorners(prev, next))
                        {
                            m_OpenPaths.Add(new Vector2[] { prev.position, next.position });
                        }
                        continue;
                    }
                    


                Add:
                    if (isBoundary)
                    {
                        boundaryVert = follow;
                        hasBoundaryVert = true;
                    }
                    Vector2 p = follow.position;
                    path.Add(p);
                    follow = follow.next;

                } while (follow != start || path.Count == 0);


                if (path.Count > 2)
                {
                    var arrayPath = path.ToArray();
                    PolygonUtility.DoReverse(arrayPath);
                    m_ColliderPaths.Add(arrayPath);

                    if (hasBoundaryVert)
                    {
                        path.Clear();
                        start = boundaryVert;
                        follow = start;
                        do
                        {
                            var next = follow.next;
                            bool isBoundary = IsBoundaryVert(follow);
                            bool isNextBoundary = IsBoundaryVert(next);

                            if (isBoundary && isNextBoundary)
                            {
                                if (!CompareCorners(follow, next))
                                {
                                    path.Add(follow.position);
                                    path.Add(next.position);
                                    arrayPath = path.ToArray();
                                    PolygonUtility.DoReverse(arrayPath);
                                    m_OpenPaths.Add(arrayPath);
                                    path.Clear();
                                }
                                follow = next;
                                continue;
                            }

                            if (isBoundary && !isNextBoundary)
                            {
                                path.Add(follow.position);
                                follow = next;
                                continue;
                            }

                            if (!isBoundary && isNextBoundary)
                            {
                                path.Add(follow.position);
                                path.Add(next.position);
                                arrayPath = path.ToArray();
                                PolygonUtility.DoReverse(arrayPath);
                                m_OpenPaths.Add(arrayPath);
                                path.Clear();
                                follow = next;
                                continue;
                            }

                            path.Add(follow.position);
                            follow = next;
                        } while (follow != start);
                    }
                    else
                    {
                        m_ClosedPaths.Add(arrayPath);
                    }
                }

                path.Clear();
           
                var copy = openPaths.ToArray();
                /*
                openPaths.Clear();
                foreach (var item in copy)
                {
                    List<Vector2> a = new List<Vector2>(item);
                    List<Vector2> b = new List<Vector2>();
                    UnityEngine.LineUtility.Simplify(a, 0, b);
                    openPaths.Add(b.ToArray());
                }*/

                }
        }

        public void OffsetColliderPaths(VoxelMap map, float offset)
        {
            if (offset == 0) return;

            for (int i = 0; i < m_ColliderPaths.Count; i++)
            {
                var path = m_ColliderPaths[i];
                int n = path.Length;
                Vector2[] newPath = new Vector2[n];
                for (int j = 0; j < n; j++)
                {
                    Vector2 a = path[LoopUtility.PrevIndex(j, n)];
                    Vector2 b = path[j];
                    Vector2 c = path[LoopUtility.NextIndex(j, n)];

                    Vector2 normal;
                    if (IsBoundaryVert(b))
                    {
                        normal = -map.GetBoundaryVertNormal(b);

                        if (IsCornerVert(b))
                        {
                            normal *= 0;
                        }
                    }
                    else
                    {
                        Vector2 ab = b - a;
                        Vector2 bc = c - b;

                        normal = VectorUtility.GetNormal(((ab.normalized + bc.normalized) / 2).normalized);
                    }

                    newPath[j] = b - normal * offset;
                }

                m_ColliderPaths[i] = newPath;
            }
        }




        void EnterQuad(Vector2Int from, Vector2Int to)
        {
            bool state = m_Nodes[from.x][from.y].state;

            for (int x = from.x; x < to.x; x++)
            {
                for (int y = from.y; y < to.y; y++)
                {
                    if (m_Nodes[x][y].state != state)
                    {
                        Vector2Int size = (to - from) / 2;
                        if (size.x <= 1 || size.y <= 1)
                        {
                            for (int i = from.x; i < to.x - 1; i++)
                            {
                                for (int j = from.y; j < to.y - 1; j++)
                                {
                                    HandleSqure(i, j);
                                }
                            }

                            return;
                        }
                        for (int i = 0; i < 2; i++)
                        {
                            for (int j = 0; j < 2; j++)
                            {
                                Vector2Int p = from;
                                p.x += size.x * i;
                                p.y += size.y * j;
                                EnterQuad(p, p + size + Vector2Int.one);
                            }
                        }
                        return;
                    }
                }
            }

            if (state)
            {
                Vert[] poly = new Vert[4];
                poly[0] = m_Nodes[from.x][from.y].vert;
                poly[1] = m_Nodes[from.x][to.y - 1].vert;
                poly[2] = m_Nodes[to.x - 1][to.y - 1].vert;
                poly[3] = m_Nodes[to.x - 1][from.y].vert;
                SaveVerts(poly, 4);
                Triangulate(poly, 4);
                AddBoundaryOutlines(poly, 4);
            }
        }

        void HandleSqure(int x, int y)
        {
            Vert[] poly = new Vert[6];
            int polyCount = 0;
            
            Node a = m_Nodes[x + 0][y + 0];
            Node b = m_Nodes[x + 1][y + 0];
            Node c = m_Nodes[x + 0][y + 1];
            Node d = m_Nodes[x + 1][y + 1];

            int caseNum = 0;
            if (c.state) caseNum += 2; if (d.state) caseNum += 1;
            if (a.state) caseNum += 8; if (b.state) caseNum += 4;
            if (caseNum == 0) return;

            switch (caseNum)
            {
                case 15:
                    polyCount = 4;
                    poly[0] = a.vert;
                    poly[1] = c.vert;
                    poly[2] = d.vert;
                    poly[3] = b.vert;
                    break;
                case 1:
                    polyCount = 3;
                    poly[0] = b.upVert;
                    poly[1] = c.rightVert;
                    poly[2] = d.vert;
                    AddOutline(b.upVert, c.rightVert);
                    break;
                case 2:
                    polyCount = 3;
                    poly[0] = c.rightVert;
                    poly[1] = a.upVert;
                    poly[2] = c.vert;
                    AddOutline(c.rightVert, a.upVert);
                    break;
                case 3:
                    polyCount = 4;
                    poly[0] = b.upVert;
                    poly[1] = a.upVert;
                    poly[2] = c.vert;
                    poly[3] = d.vert;
                    AddOutline(b.upVert, a.upVert);
                    break;
                case 4:
                    polyCount = 3;
                    poly[0] = a.rightVert;
                    poly[1] = b.upVert;
                    poly[2] = b.vert;
                    AddOutline(a.rightVert, b.upVert);
                    break;
                case 5:
                    polyCount = 4;
                    poly[0] = a.rightVert;
                    poly[1] = c.rightVert;
                    poly[2] = d.vert;
                    poly[3] = b.vert;
                    AddOutline(a.rightVert, c.rightVert);
                    break;
                case 6:
                    if (k_FixThinConnection)
                    {
                        // c  !d
                        //!a   b 
                        if ((b.upVert.position - a.rightVert.position).sqrMagnitude < k_ThinConnectionWidth ||
                            (a.upVert.position - c.rightVert.position).sqrMagnitude < k_ThinConnectionWidth)
                        {
                            polyCount = 3;
                            poly[0] = a.rightVert;
                            poly[1] = b.upVert;
                            poly[2] = b.vert;
                            AddOutline(a.rightVert, b.upVert);
                            SaveVerts(poly, polyCount);
                            AddBoundaryOutlines(poly, 3);
                            Triangulate(poly, polyCount);


                            poly[0] = a.upVert;
                            poly[1] = c.vert;
                            poly[2] = c.rightVert;
                            AddOutline(c.rightVert, a.upVert);
                            SaveVerts(poly, polyCount);
                            AddBoundaryOutlines(poly, 3);
                            Triangulate(poly, polyCount);
                            return;
                        }
                    }

                    polyCount = 6;
                    poly[0] = c.rightVert;
                    poly[1] = b.upVert;
                    poly[2] = b.vert;
                    poly[3] = a.rightVert;
                    poly[4] = a.upVert;
                    poly[5] = c.vert;
                    AddOutline(c.rightVert, b.upVert);
                    AddOutline(a.rightVert, a.upVert);
                    break;
                case 7:
                    polyCount = 5;
                    poly[0] = d.vert;
                    poly[1] = b.vert;
                    poly[2] = a.rightVert;
                    poly[3] = a.upVert;
                    poly[4] = c.vert;
                    AddOutline(a.rightVert, a.upVert);
                    break;
                case 8:
                    polyCount = 3;
                    poly[0] = a.upVert;
                    poly[1] = a.rightVert;
                    poly[2] = a.vert;
                    AddOutline(a.upVert, a.rightVert);
                    break;
                case 9:
                    if (k_FixThinConnection)
                    {
                        //!c  d
                        ///a !b 
                        if ((a.upVert.position - a.rightVert.position).sqrMagnitude < k_ThinConnectionWidth ||
                            (b.upVert.position - c.rightVert.position).sqrMagnitude < k_ThinConnectionWidth)
                        {
                            polyCount = 3;
                            poly[0] = a.upVert;
                            poly[1] = a.rightVert;
                            poly[2] = a.vert;

                            AddOutline(a.upVert, a.rightVert);
                            SaveVerts(poly, polyCount);
                            AddBoundaryOutlines(poly, 3);
                            Triangulate(poly, polyCount);


                            poly[0] = c.rightVert;
                            poly[1] = d.vert;
                            poly[2] = b.upVert;

                            AddOutline(b.upVert, c.rightVert);
                            SaveVerts(poly, polyCount);
                            AddBoundaryOutlines(poly, 3);
                            Triangulate(poly, polyCount);
                            return;
                        }
                    }
                    polyCount = 6;
                    poly[0] = a.upVert;
                    poly[1] = c.rightVert;
                    poly[2] = d.vert;
                    poly[3] = b.upVert;
                    poly[4] = a.rightVert;
                    poly[5] = a.vert;
                    AddOutline(a.upVert, c.rightVert);
                    AddOutline(b.upVert, a.rightVert);
                    break;
                case 10:
                    polyCount = 4;
                    poly[0] = c.rightVert;
                    poly[1] = a.rightVert;
                    poly[2] = a.vert;
                    poly[3] = c.vert;
                    AddOutline(c.rightVert, a.rightVert);
                    break;
                case 11:
                    polyCount = 5;
                    poly[0] = c.vert;
                    poly[1] = d.vert;
                    poly[2] = b.upVert;
                    poly[3] = a.rightVert;
                    poly[4] = a.vert;
                    AddOutline(b.upVert, a.rightVert);
                    break;
                case 12:
                    polyCount = 4;
                    poly[0] = a.upVert;
                    poly[1] = b.upVert;
                    poly[2] = b.vert;
                    poly[3] = a.vert;
                    AddOutline(a.upVert, b.upVert);
                    break;
                case 13:
                    polyCount = 5;
                    poly[0] = b.vert;
                    poly[1] = a.vert;
                    poly[2] = a.upVert;
                    poly[3] = c.rightVert;
                    poly[4] = d.vert;
                    AddOutline(a.upVert, c.rightVert);
                    break;
                case 14:
                    polyCount = 5;
                    poly[0] = a.vert;
                    poly[1] = c.vert;
                    poly[2] = c.rightVert;
                    poly[3] = b.upVert;
                    poly[4] = b.vert;
                    AddOutline(c.rightVert, b.upVert);
                    break;
            }

            SaveVerts(poly, polyCount);
            AddBoundaryOutlines(poly, polyCount);
            Triangulate(poly, polyCount);
        }

        int GetBoundaryVertCase(Vert v)
        {
            float x = v.position.x;
            float y = v.position.y;

            return GetCaseNum(x == 0, y == 0, x == m_XMax, y == m_YMax);
        }

        int GetCaseNum(bool a, bool b, bool c, bool d)
        {
            int caseNum = 0;
            if (c) caseNum += 2; if (d) caseNum += 1;
            if (a) caseNum += 8; if (b) caseNum += 4;
            return caseNum;
        }

        bool IsCornerVert(Vert v)
        {
            float x = v.position.x;
            float y = v.position.y;

            bool L = y == 0;
            bool R = y == m_YMax;
            bool U = x == m_XMax;
            bool D = x == 0;

            return L && D || L && U || R && D || R && U;
        }

        bool IsCornerVert(Vector2 position)
        {
            float x = position.x;
            float y = position.y;

            bool L = y == 0;
            bool R = y == m_YMax;
            bool U = x == m_XMax;
            bool D = x == 0;

            return L && D || L && U || R && D || R && U;
        }

        bool CompareCorners(Vert a, Vert b)
        {
            float aX = a.position.x;
            float aY = a.position.y;
            float bX = b.position.x;
            float bY = b.position.y;

            return
                aY == 0 && bY == 0 ||
                aX == 0 && bX == 0 ||
                aY == m_YMax && bY == m_YMax ||
                aX == m_XMax && bX == m_XMax;
        }

        bool CompareCorners(Vector2 a, Vector2 b)
        {
            float aX = a.x;
            float aY = a.y;
            float bX = b.x;
            float bY = b.y;

            return
                aY == 0 && bY == 0 ||
                aX == 0 && bX == 0 ||
                aY == m_YMax && bY == m_YMax ||
                aX == m_XMax && bX == m_XMax;
        }

        int GetCornerCase(Vert v)
        {
            float x = v.position.x;
            float y = v.position.y;

            bool L = y == 0;
            bool R = y == m_YMax;
            bool U = x == m_XMax;
            bool D = x == 0;

            if (L && D) return 1;
            if (L && U) return 2;
            if (R && D) return 3;
            if (R && U) return 4;

            return 0;
        }

        bool SetMaxRes(Vector2Int maxRes)
        {
            m_XMax = maxRes.x - 1;
            m_YMax = maxRes.y - 1;

            if (m_Res.x < maxRes.x || m_Res.y < maxRes.y)
            {
                m_Res = maxRes;
                int resX = maxRes.x;
                int resY = maxRes.y;
                m_Nodes = new Node[resX][];

                for (int x = 0; x < resX; x++)
                {
                    var array = m_Nodes[x] = new Node[resY];
                    for (int y = 0; y < resY; y++)
                        array[y] = new Node(x, y);
                }

                return true;
            }
            return false;
        }

        void RefreshNodes(VoxelMap map)
        {
            int resX = map.resolution.x;
            int resY = map.resolution.y;

            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    int i = y * resX + x;
                    m_Nodes[x][y].Refresh(map.states[i], map.edges[i].x, map.edges[i].y);
                }
            }
        }

        void SaveVerts(Vert[] poly, int polyCount)
        {
            for (int i = 0; i < polyCount; i++)
            {
                Vert v = poly[i];
                if (v.index == -1)
                {
                    v.index = m_MeshVerts.Count;
                    m_MeshVerts.Add(v.position);
                }
            }
        }

        void AddBoundaryOutlines(Vert[] poly, int polyCount)
        {
            Vert prevV = poly[polyCount - 1];
            for (int i = 0; i < polyCount; i++)
            {
                Vert currentV = poly[i];
                if (!prevV.isOutline &&
                    IsBoundaryVert(prevV) &&
                    IsBoundaryVert(currentV))
                {
                    m_OutlineVerts.Add(poly[i]);
                    currentV.prev = prevV;
                    prevV.next = currentV;
                    prevV.isOutline = true;
                }
                prevV = currentV;
            }
        }

        void Triangulate(Vert[] poly, int polyCount)
        {
            float triArea = 0;
            int triCount = polyCount - 2;
            for (int i = 0; i < triCount; i++)
            {
                Vert va = poly[0];
                Vert vb = poly[i + 1];
                Vert vc = poly[i + 2];

                m_MeshTriangles.Add(va.index);
                m_MeshTriangles.Add(vb.index);
                m_MeshTriangles.Add(vc.index);

                Vector2 a = va.position;
                Vector2 b = vb.position;
                Vector2 c = vc.position;

                triArea += c.x * a.y - c.y * a.x;
                triArea += a.x * b.y - a.y * b.x;
                triArea += b.x * c.y - b.y * c.x;
            }

            triArea *= -0.5f;
            m_Area += triArea;
        }

        void AddOutline(Vert a, Vert b)
        {
            b.prev = a;
            a.next = b;
            a.isOutline = true;
            m_OutlineVerts.Add(a);
        }

        bool IsBoundaryVert(Vert vert)
        {
            float x = vert.position.x;
            float y = vert.position.y;
            return x == 0 || y == 0 || x == m_XMax || y == m_YMax;
        }

        bool IsBoundaryVert(Vector2 position)
        {
            float x = position.x;
            float y = position.y;
            return x == 0 || y == 0 || x == m_XMax || y == m_YMax;
        }
    }
}