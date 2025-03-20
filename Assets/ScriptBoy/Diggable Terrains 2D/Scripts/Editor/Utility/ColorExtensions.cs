using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    static class ColorExtensions
    {
        public static Color Fade(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}