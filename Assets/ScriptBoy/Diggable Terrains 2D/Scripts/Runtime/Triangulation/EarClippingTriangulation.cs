using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Triangulating a 2D polygon using the Ear Clipping algorithm.
    /// </summary>
    public static class EarClippingTriangulation
    {
        class EarClippingTriangulationFailed : UnityException
        {
            public EarClippingTriangulationFailed() : base("Self-intersecting polygon is not supported!") { }
        }

        public static int[] Triangulate(Vector2[] polygon)
        {
            int pointCount = polygon.Length;
            int triangleCount = 0;

            int[] triangles = new int[(pointCount - 2) * 3];
            int[] indexes = new int[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                indexes[i] = i + 1;
            }
            indexes[pointCount - 1] = 0;

            int prev = 0;
            int current = 1;
            int safety = 2 * pointCount;
            while (pointCount > 2)
            {
                if (safety-- == -1)
                {
                    throw new EarClippingTriangulationFailed();
                }

                int next = indexes[current];
                int nextNext = indexes[next];

                if (nextNext == prev)
                {
                    triangles[triangleCount * 3 + 2] = prev;
                    triangles[triangleCount * 3 + 1] = current;
                    triangles[triangleCount * 3 + 0] = next;
                    triangleCount++;
                    break;
                }

                float aX = polygon[prev].x;
                float bX = polygon[current].x;
                float cX = polygon[next].x;

                float aY = polygon[prev].y;
                float bY = polygon[current].y;
                float cY = polygon[next].y;

                if (0 <= (bX - aX) * (cY - aY) - (bY - aY) * (cX - aX))
                {
                    bool noPointInsideTriangle = true;

                    int start = nextNext;
                    while (start != prev)
                    {
                        float pX = polygon[start].x;
                        float pY = polygon[start].y;

                        if ((cX - bX) * (pY - bY) - (cY - bY) * (pX - bX) >= 0 &&
                            (bX - aX) * (pY - aY) - (bY - aY) * (pX - aX) >= 0 &&
                            (aX - cX) * (pY - cY) - (aY - cY) * (pX - cX) >= 0)
                        {
                            noPointInsideTriangle = false;
                            break;
                        }

                        start = indexes[start];
                    }

                    if (noPointInsideTriangle)
                    {
                        triangles[triangleCount * 3 + 2] = prev;
                        triangles[triangleCount * 3 + 1] = current;
                        triangles[triangleCount * 3 + 0] = next;
                        triangleCount++;

                        indexes[prev] = next;
                        prev = next;
                        current = nextNext;
                        pointCount--;
                        safety = pointCount * 2;
                        continue;
                    }
                }

                prev = current;
                current = next;
            }

            return triangles;
        }
    }
}