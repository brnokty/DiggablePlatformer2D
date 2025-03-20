using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for 2D curves.
    /// </summary>
    public static class BezierUtility
    {
        /// <summary>
        /// Returns a point on a linear bezier curve at the given time.
        /// </summary>
        public static Vector2 Evaluate(Vector2 p0, Vector2 p1, float t)
        {
            return p0 * (1 - t) + p1 * t;
        }

        /// <summary>
        /// Returns a point on a quadratic bezier curve at the given time.
        /// </summary>
        public static Vector2 Evaluate(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float i = (1 - t);
            return i * i * p0 + 2 * i * t * p1 + t * t * p2;
        }

        /// <summary>
        /// Returns a point on a cubic bezier curve at the given time.
        /// </summary>
        public static Vector2 Evaluate(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float i = (1 - t);
            return i * i * i * p0 + 3 * i * i * t * p1 + 3 * i * t * t * p2 + t * t * t * p3;
        }

        /// <summary>
        /// Splits a cubic bezier curve at the given time.
        /// </summary>
        public static Vector2 Split(Vector2 p0, ref Vector2 p1, ref Vector2 p2, Vector2 p3, out Vector2 inTan, out Vector2 outTan, float t)
        {
            Vector2 p = Evaluate(p0, p1, p2, p3, t);
            inTan = Evaluate(p0, p1, p2, t);
            outTan = Evaluate(p1, p2, p3, t);
            p1 = Evaluate(p0, p1, t);
            p2 = Evaluate(p2, p3, t);
            return p;
        }
    }
}