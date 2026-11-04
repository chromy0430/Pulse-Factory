using UnityEngine;

namespace EnhancedRendering.Bloom
{
    public static class BloomPyramid
    {
        public const int MaxLevels = 8;

        public static Vector2Int SizeAt(int width, int height, int level)
        {
            int shift = Mathf.Clamp(level + 1, 1, MaxLevels);
            return new Vector2Int(Mathf.Max(1, width >> shift), Mathf.Max(1, height >> shift));
        }

        public static int LevelCount(int width, int height, int requested)
        {
            int count = 1;
            var size = SizeAt(width, height, 0);
            while (count < Mathf.Clamp(requested, 1, MaxLevels) && size.x > 1 && size.y > 1)
            {
                size = new Vector2Int(Mathf.Max(1, size.x >> 1), Mathf.Max(1, size.y >> 1));
                count++;
            }
            return count;
        }
    }
}
