using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    /// <summary>
    /// Utility functions for particles.
    /// </summary>
    public static class TerrainParticleUtility
    {
        static List<Vector2> s_RandomPoints = new List<Vector2>();

        /// <summary>
        /// Generates random points inside the shovel polygon, then uses them to collect the corresponding particles on terrains and adds them to the particles list.
        /// </summary>
        /// <param name="shovel">The shovel polygon is used to generate random points.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="count">The number of random points.</param>
        /// <param name="seed">The random state.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(Shovel shovel, List<TerrainParticle> particles, int count, int seed = 0, int layerMask = -1)
        {
            GetParticles(shovel.GetPolygon(), particles, count, seed, layerMask);
        }


        /// <summary>
        /// Generates random points inside the shovel polygon, then uses them to collect the corresponding particles on terrains and adds them to the particles list.
        /// </summary>
        /// <param name="shovel">The shovel polygon is used to generate random points.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="count">The number of random points.</param>
        /// <param name="seed">The random state.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(Vector2[] shovelPolygon, List<TerrainParticle> particles, int count, int seed = 0, int layerMask = -1)
        {
            RandomUtility.GeneratePointsInsidePolygon(shovelPolygon, s_RandomPoints, count, seed);
            GetParticles(s_RandomPoints, particles, layerMask);
        }

        /// <summary>
        /// <para>Collects particles at the given points and adds them to the particles list.</para>
        /// If a point is inside the terrain, it will be removed from the points list and the corresponding particle will be added to the particles list.
        /// </summary>
        /// <param name="points">A list of points in world space.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(List<Vector2> points, List<TerrainParticle> particles, int layerMask = -1)
        {
            particles.Clear();
            foreach (var terrain in Terrain2D.FindByMask(layerMask, false))
            {
                terrain.GetParticles(points, particles);
            }
        }

        /// <summary>
        /// Generates random points inside the shovel polygon, then uses them to collect the corresponding particles on terrains and adds them to the particles list.
        /// </summary>
        /// <param name="shovel">The shovel polygon is used to generate random points.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="count">The number of random points.</param>
        /// <param name="seed">The random state.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(Shovel shovel, List<Vector2> particles, int count, int seed = 0, int layerMask = -1)
        {
            GetParticles(shovel.GetPolygon(), particles, count, seed, layerMask);
        }


        /// <summary>
        /// Generates random points inside the shovel polygon, then uses them to collect the corresponding particles on terrains and adds them to the particles list.
        /// </summary>
        /// <param name="shovel">The shovel polygon is used to generate random points.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="count">The number of random points.</param>
        /// <param name="seed">The random state.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(Vector2[] shovelPolygon, List<Vector2> particles, int count, int seed = 0, int layerMask = -1)
        {
            RandomUtility.GeneratePointsInsidePolygon(shovelPolygon, s_RandomPoints, count, seed);
            GetParticles(s_RandomPoints, particles, layerMask);
        }

        /// <summary>
        /// <para>Collects particles at the given points and adds them to the particles list.</para>
        /// If a point is inside the terrain, it will be removed from the points list and the corresponding particle will be added to the particles list.
        /// </summary>
        /// <param name="points">A list of points in world space.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(List<Vector2> points, List<Vector2> particles, int layerMask = -1)
        {
            particles.Clear();
            foreach (var terrain in Terrain2D.FindByMask(layerMask, false))
            {
                terrain.GetParticles(points, particles);
            }
        }


        /// <summary>
        /// Generates random points inside a circle, then uses them to collect the corresponding particles on terrains and adds them to the particles list.
        /// </summary>
        /// <param name="center">The center position of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="count">The number of random points.</param>
        /// <param name="seed">The random state.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(Vector2 center, float radius, List<TerrainParticle> particles, int count, int seed = 0, int layerMask = -1)
        {
            s_RandomPoints.Clear();
            RandomUtility.GeneratePointsInsideCircle(center, radius, s_RandomPoints, count, seed);
            GetParticles(s_RandomPoints, particles, layerMask);
        }


        /// <summary>
        /// Generates random points inside a circle, then uses them to collect the corresponding particles on terrains and adds them to the particles list.
        /// </summary>
        /// <param name="center">The center position of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="particles">A list to add the collected particles to.</param>
        /// <param name="count">The number of random points.</param>
        /// <param name="seed">The random state.</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore some terrains.</param>
        public static void GetParticles(Vector2 center, float radius, List<Vector2> particles, int count, int seed = 0, int layerMask = -1)
        {
            s_RandomPoints.Clear();
            RandomUtility.GeneratePointsInsideCircle(center, radius, s_RandomPoints, count, seed);
            GetParticles(s_RandomPoints, particles, layerMask);
        }
    }
}