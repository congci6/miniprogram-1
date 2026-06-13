using UnityEngine;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 建筑变体生成器
    /// 为同类建筑生成多个视觉变体
    /// </summary>
    public static class BuildingVariantGenerator
    {
        public static BuildingVariant GenerateVariant(string buildingType, int seed)
        {
            Random.InitState(seed);

            var variant = new BuildingVariant
            {
                HeightScale = Random.Range(0.9f, 1.1f),
                WidthScale = Random.Range(0.95f, 1.05f),
                DepthScale = Random.Range(0.95f, 1.05f),
                RoofType = Random.Range(0, 3),
                WindowPattern = Random.Range(0, 5),
                ColorVariation = Random.Range(0, 8),
                HasBalcony = Random.value > 0.6f,
                HasRoofDetail = Random.value > 0.5f
            };

            return variant;
        }
    }

    public struct BuildingVariant
    {
        public float HeightScale;
        public float WidthScale;
        public float DepthScale;
        public int RoofType;       // 0=平顶, 1=尖顶, 2=圆顶
        public int WindowPattern;  // 0-4不同窗户排列
        public int ColorVariation; // 0-7颜色变体
        public bool HasBalcony;
        public bool HasRoofDetail;
    }
}
