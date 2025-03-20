using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for 2D lines.
    /// </summary>
    internal static class LineUtility
    {
        /// <summary>
        /// Calculates the square distance from a point to a line segment.
        /// </summary>
        public static float GetPointSqrDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            float ap_dot_ab = (p.x - a.x) * (b.x - a.x) + (p.y - a.y) * (b.y - a.y);
            float ab_sqr_magnitude = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
            float t = ap_dot_ab / ab_sqr_magnitude;

            if (t < 0) return (p.x - a.x) * (p.x - a.x) + (p.y - a.y) * (p.y - a.y);
            if (t > 1) return (p.x - b.x) * (p.x - b.x) + (p.y - b.y) * (p.y - b.y);

            float x = a.x + (b.x - a.x) * t;
            float y = a.y + (b.y - a.y) * t;
            return (p.x - x) * (p.x - x) + (p.y - y) * (p.y - y);
        }

        /// <summary>
        /// Calculates the distance from a point to a line segment.
        /// </summary>
        public static float GetPointDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            return Mathf.Sqrt(GetPointSqrDistance(p, a, b));
        }

        /// <summary>
        /// Calculates the side of a point to a line.
        /// <para>Returns (positive) if the point is on the right side of the line.</para>
        /// <para>Returns (zero)     if the point is on the line.</para>
        /// <para>Returns (negative) if the point is on the left side of the line.</para>
        /// </summary>
        public static float GetPointSide(Vector2 p, Vector2 a, Vector2 b)
        {
            return -Vector2.Dot(VectorUtility.GetNormal(b - a), p - a);
        }

        /// <summary>
        /// Returns true if the point is on the line segment.
        /// </summary>
        public static bool IsPointOnLine(Vector2 p, Vector2 a, Vector2 b, float threshold = 0.00001f)
        {
            float crossProduct = (p.y - a.y) * (b.x - a.x) - (p.x - a.x) * (b.y - a.y);

            if (!(crossProduct < threshold && crossProduct > -threshold))
                return false;

            float dotProduct = (p.x - a.x) * (b.x - a.x) + (p.y - a.y) * (b.y - a.y);

            if (dotProduct < -threshold)
                return false;

            float squaredLengthBA = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);

            if (dotProduct > squaredLengthBA)
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if the point is on the line segment.
        /// </summary>
        public static bool IsPointOnLineCheckedByDistance(Vector2 p, Vector2 a, Vector2 b, float threshold = 0.0001f)
        {
            return GetPointDistance(p, a, b) < threshold;
        }

        /// <summary>
        /// Calculates the bounding box of the line segment.
        /// </summary>
        public static Bounds GetBounds(Vector2 a, Vector2 b, float padding = 0.05f)
        {
            float minX = a.x;
            float maxX = b.x;

            float minY = a.y;
            float maxY = b.y;

            if (minX > maxX)
            {
                minX = b.x;
                maxX = a.x;
            }

            if (minY > maxY)
            {
                minY = b.y;
                maxY = a.y;
            }

            Vector2 center;
            center.x = (minX + maxX) / 2;
            center.y = (minY + maxY) / 2;

            Vector3 size;
            size.x = maxX - minX + padding;
            size.y = maxY - minY + padding;
            size.z = 1;

            return new Bounds(center, size);
        }

        /// <summary>
        /// Intersects two line segments.
        /// </summary>
        public static bool IntersectLines(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 hit)
        {
            hit = new Vector2();
            float denominator = (b.x - a.x) * (d.y - c.y) - (b.y - a.y) * (d.x - c.x);

            if (denominator == 0)
            {
                // The lines are parallel or coincident
                return false;
            }

            //Lerp(a, b, t) = Lerp(c, d, u)
            float t = ((c.x - a.x) * (d.y - c.y) - (c.y - a.y) * (d.x - c.x)) / denominator;
            float u = -((a.x - b.x) * (a.y - c.y) - (a.y - b.y) * (a.x - c.x)) / denominator;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                hit.x = a.x + t * (b.x - a.x);
                hit.y = a.y + t * (b.y - a.y);
                return true;
            }

            return false;
        }
    }
}