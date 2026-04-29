using UnityEngine;

namespace AI
{
    public static class FuzzyMembership
    {
        public static float Tri(float x, float a, float b, float c)
        {
            if (x <= a || x >= c) return 0f;
            if (Mathf.Approximately(x, b)) return 1f;
            if (x < b) return (x - a) / (b - a);
            return (c - x) / (c - b);
        }

        public static float Trap(float x, float a, float b, float c, float d)
        {
            if (x <= a || x >= d) return 0f;
            if (x >= b && x <= c) return 1f;
            if (x < b) return (x - a) / (b - a);
            return (d - x) / (d - c);
        }

        public static float And(float a, float b) => Mathf.Min(a, b);
        public static float Or(float a, float b) => Mathf.Max(a, b);
    }
}