using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Creating delaunay triangles using the Edge Flip algorithm.
    /// </summary>
    public class DelaunayTriangulation
    {
        public static int[] Triangulate(int[] triangles, Vector3[] verts, int tryCount = 8)
        {
            var delaunay = new DelaunayTriangulation(verts, triangles);
            delaunay.DoEdgeFlip(tryCount);
            return delaunay.GetTriangles();
        }

        Vector3[] m_Verts;
        int[] m_Triangles;
        int[] m_Nexts;
        int[] m_Twins;
        bool[] m_Skips;

        DelaunayTriangulation(Vector3[] verts, int[] triangles)
        {
            this.m_Verts = verts;
            this.m_Triangles = triangles;

            int edgeCount = triangles.Length;

            m_Nexts = new int[edgeCount];
            m_Twins = new int[edgeCount];
            m_Skips = new bool[edgeCount];

            for (int i = 0; i < edgeCount; i += 3)
            {
                m_Nexts[i + 0] = i + 1;
                m_Nexts[i + 1] = i + 2;
                m_Nexts[i + 2] = i + 0;

                m_Twins[i + 0] = -1;
                m_Twins[i + 1] = -1;
                m_Twins[i + 2] = -1;
            }

            for (int i = 0; i < edgeCount; i++)
            {
                if (m_Twins[i] != -1) continue;

                int ia = triangles[i];
                int ib = triangles[m_Nexts[i]];

                for (int j = i + 1; j < edgeCount; j++)
                {
                    if (m_Twins[j] != -1) continue;

                    int ic = triangles[j];
                    int id = triangles[m_Nexts[j]];

                    if (ia == id && ib == ic)
                    {
                        m_Twins[i] = j;
                        m_Twins[j] = i;
                        break;
                    }
                }
            }
        }

        int[] GetTriangles()
        {
            int j = 0;
            int edgeCount = m_Nexts.Length;
            for (int i = 0; i < edgeCount; i++)
            {
                int ia = m_Nexts[i]; if (ia == -1) continue;
                int ib = m_Nexts[ia];
                int ic = m_Nexts[ib];

                m_Nexts[i] = -1;
                m_Nexts[ia] = -1;
                m_Nexts[ib] = -1;

                m_Twins[j++] = m_Triangles[ia];
                m_Twins[j++] = m_Triangles[ib];
                m_Twins[j++] = m_Triangles[ic];
            }

            return m_Twins;
        }

        void DoEdgeFlip(int tryCount)
        {
            int edgeCount = m_Triangles.Length;
            int safety = tryCount;
            bool hasFlip;
            do
            {
                if (safety-- < 0) break;
                hasFlip = false;
                for (int i = 0; i < edgeCount; i++)
                {
                    if (m_Skips[i]) continue;
                    if (m_Twins[i] == -1) continue;

                    int iab = i;
                    int ibc = m_Nexts[iab];
                    int ica = m_Nexts[ibc];

                    int iba = m_Twins[i];
                    int iad = m_Nexts[iba];
                    int idb = m_Nexts[iad];

                    if (NeedFlip(m_Triangles[iab], m_Triangles[ibc], m_Triangles[ica], m_Triangles[idb]))
                    {
                        m_Triangles[iab] = m_Triangles[idb];

                        m_Nexts[iab] = ica;
                        m_Nexts[ica] = iad;
                        m_Nexts[iad] = iab;

                        m_Triangles[iba] = m_Triangles[ica];

                        m_Nexts[iba] = idb;
                        m_Nexts[idb] = ibc;
                        m_Nexts[ibc] = iba;

                        m_Skips[iab] = false;
                        m_Skips[ibc] = false;
                        m_Skips[ica] = false;
                        m_Skips[iba] = false;
                        m_Skips[iad] = false;
                        m_Skips[idb] = false;

                        hasFlip = true;
                    }
                    else
                    {
                        m_Skips[i] = true;
                    }
                }
            } while (hasFlip);
        }

        bool NeedFlip(int ia, int ib, int ic, int id)
        {
            float ax = m_Verts[ia].x;
            float ay = m_Verts[ia].y;
            float bx = m_Verts[ib].x;
            float by = m_Verts[ib].y;
            float cx = m_Verts[ic].x;
            float cy = m_Verts[ic].y;
            float dx = m_Verts[id].x;
            float dy = m_Verts[id].y;

            if ((ax - cx) * (dy - cy) - (ay - cy) * (dx - cx) < 0 &&
                (dx - ax) * (by - ay) - (dy - ay) * (bx - ax) < 0 &&
                (bx - dx) * (cy - dy) - (by - dy) * (cx - dx) < 0 &&
                (cx - bx) * (ay - by) - (cy - by) * (ax - bx) < 0)
            {
                float w10 = ((bx * bx) - (ax * ax) + (by * by) - (ay * ay)) * 0.5f;
                float w21 = ((cx * cx) - (bx * bx) + (cy * cy) - (by * by)) * 0.5f;
                float x01 = bx - ax;
                float x12 = cx - bx;
                float y01 = by - ay;
                float y12 = cy - by;
                float h = (y12 * w10 - y01 * w21) / (y12 * x01 - y01 * x12);
                float k = (x12 * w10 - x01 * w21) / (x12 * y01 - x01 * y12);
                float hdx = (dx - h);
                float kdy = (dy - k);

                if ((hdx * hdx + kdy * kdy) / ((ax - h) * (ax - h) + (ay - k) * (ay - k)) >= 1)
                    return false;

                w10 = ((ax * ax) - (cx * cx) + (ay * ay) - (cy * cy)) * 0.5f;
                w21 = ((dx * dx) - (ax * ax) + (dy * dy) - (ay * ay)) * 0.5f;
                x01 = ax - cx;
                x12 = dx - ax;
                y01 = ay - cy;
                y12 = dy - ay;
                h = (y12 * w10 - y01 * w21) / (y12 * x01 - y01 * x12);
                k = (x12 * w10 - x01 * w21) / (x12 * y01 - x01 * y12);
                hdx = (bx - h);
                kdy = (by - k);

                if ((hdx * hdx + kdy * kdy) / ((cx - h) * (cx - h) + (cy - k) * (cy - k)) > 1)
                    return true;
            }
            return false;
        }
    }
}