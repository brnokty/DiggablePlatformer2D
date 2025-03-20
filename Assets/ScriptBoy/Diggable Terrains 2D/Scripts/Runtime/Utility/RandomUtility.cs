using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for generating random points.
    /// </summary>
    public static class RandomUtility
    {
        public static void GeneratePointsInsidePolygon(Vector2[] polygon, List<Vector2> points, int count, int seed)
        {
            using (new Scope(seed))
            {
                points.Clear();
                var prevState = Random.state;
                Random.InitState(seed);
                Bounds bounds = PolygonUtility.GetBounds(polygon);
                for (int i = 0; i < count; i++)
                {
                    Vector2 point;
                    point.x = Random.Range(bounds.min.x, bounds.max.x);
                    point.y = Random.Range(bounds.min.y, bounds.max.y);
                    if (PolygonUtility.IsPointInside(point, polygon))
                    {
                        points.Add(point);
                    }
                }
                Random.state = prevState;
            }
        }

        public static void GeneratePointsInsideCircle(Vector2 center, float radius, List<Vector2> points, int count, int seed)
        {
            using (new Scope(seed))
            {
                points.Clear();
                for (int i = 0; i < count; i++)
                {
                    points.Add(Random.insideUnitCircle * radius + center);
                }
            }
        }


        public class Scope : System.IDisposable
        {
            Random.State m_State;

            public Scope(int seed)
            {
                m_State = Random.state;
                Random.InitState(seed);
            }

            public void Dispose()
            {
                Random.state = m_State;
            }
        }
    }
}