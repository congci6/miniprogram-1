using UnityEngine;
using System.Collections.Generic;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 程序化建筑材质管理器 - 使用MaterialPropertyBlock优化
    /// </summary>
    public static class ProceduralBuildingMaterial
    {
        private static readonly Dictionary<string, Material> sharedMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<int, MaterialPropertyBlock> propertyBlocks = new Dictionary<int, MaterialPropertyBlock>();
        private static readonly int colorPropertyID = Shader.PropertyToID("_Color");

        public static Material GetSharedMaterial(string buildingType, Material baseMaterial)
        {
            if (baseMaterial == null) return null;

            if (!sharedMaterials.TryGetValue(buildingType, out var mat))
            {
                mat = new Material(baseMaterial);
                sharedMaterials[buildingType] = mat;
            }
            return mat;
        }

        public static MaterialPropertyBlock GetPropertyBlock(string buildingType, int colorVariation)
        {
            int hash = buildingType.GetHashCode() ^ colorVariation;

            if (!propertyBlocks.TryGetValue(hash, out var block))
            {
                block = new MaterialPropertyBlock();
                var color = GetColorForType(buildingType, colorVariation);
                block.SetColor(colorPropertyID, color);
                propertyBlocks[hash] = block;
            }
            return block;
        }

        public static Material GenerateMaterial(string buildingType, int colorVariation, Material baseMaterial)
        {
            var mat = GetSharedMaterial(buildingType, baseMaterial);
            if (mat == null) return null;
            var block = GetPropertyBlock(buildingType, colorVariation);
            mat.SetColor(colorPropertyID, GetColorForType(buildingType, colorVariation));
            return mat;
        }

        /// <summary>
        /// 清理缓存 - 场景卸载时调用
        /// </summary>
        public static void ClearCache()
        {
            // 销毁所有创建的Material
            foreach (var mat in sharedMaterials.Values)
            {
                if (mat != null)
                {
                    UnityEngine.Object.Destroy(mat);
                }
            }
            sharedMaterials.Clear();
            propertyBlocks.Clear();
        }

        private static Color GetColorForType(string buildingType, int variation)
        {
            if (buildingType.Contains("residential"))
                return GetResidentialColor(variation);
            else if (buildingType.Contains("commercial"))
                return GetCommercialColor(variation);
            else if (buildingType.Contains("office"))
                return GetOfficeColor(variation);
            else if (buildingType.Contains("industrial"))
                return GetIndustrialColor(variation);

            return Color.white;
        }

        private static readonly Color[] residentialColors = new Color[]
        {
            new Color(0.95f, 0.92f, 0.85f),
            new Color(0.85f, 0.80f, 0.70f),
            new Color(0.90f, 0.88f, 0.82f),
            new Color(0.82f, 0.78f, 0.72f),
            new Color(0.88f, 0.85f, 0.80f),
            new Color(0.92f, 0.90f, 0.85f),
            new Color(0.80f, 0.75f, 0.68f),
            new Color(0.87f, 0.83f, 0.77f)
        };

        private static readonly Color[] commercialColors = new Color[]
        {
            new Color(0.95f, 0.95f, 0.95f),
            new Color(0.85f, 0.90f, 0.95f),
            new Color(0.90f, 0.85f, 0.80f),
            new Color(0.95f, 0.90f, 0.85f),
            new Color(0.88f, 0.92f, 0.96f),
            new Color(0.92f, 0.88f, 0.80f),
            new Color(0.90f, 0.90f, 0.90f),
            new Color(0.85f, 0.88f, 0.92f)
        };

        private static readonly Color[] officeColors = new Color[]
        {
            new Color(0.70f, 0.75f, 0.80f),
            new Color(0.65f, 0.70f, 0.78f),
            new Color(0.75f, 0.75f, 0.75f),
            new Color(0.68f, 0.72f, 0.76f),
            new Color(0.72f, 0.76f, 0.80f),
            new Color(0.70f, 0.70f, 0.70f),
            new Color(0.65f, 0.68f, 0.72f),
            new Color(0.78f, 0.78f, 0.78f)
        };

        private static readonly Color[] industrialColors = new Color[]
        {
            new Color(0.65f, 0.65f, 0.60f),
            new Color(0.70f, 0.65f, 0.55f),
            new Color(0.60f, 0.60f, 0.60f),
            new Color(0.68f, 0.62f, 0.52f),
            new Color(0.62f, 0.62f, 0.58f),
            new Color(0.72f, 0.68f, 0.60f),
            new Color(0.58f, 0.58f, 0.55f),
            new Color(0.75f, 0.70f, 0.62f)
        };

        private static Color GetResidentialColor(int variation) => residentialColors[variation % residentialColors.Length];
        private static Color GetCommercialColor(int variation) => commercialColors[variation % commercialColors.Length];
        private static Color GetOfficeColor(int variation) => officeColors[variation % officeColors.Length];
        private static Color GetIndustrialColor(int variation) => industrialColors[variation % industrialColors.Length];
    }
}
