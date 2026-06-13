using PocketCity.Core;
using PocketCity.Production;
using PocketCity.Simulation;
using UnityEngine;

namespace PocketCity.Integration
{
    /// <summary>
    /// 将生产系统与城市发展绑定
    /// </summary>
    public class ProductionCityBridge : MonoBehaviour
    {
        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private ProductionChainSystem production;
        [SerializeField] private StorageSystem storage;

        private void Start()
        {
            if (production != null)
            {
                production.OnProductionComplete += OnGoodsProduced;
            }
        }

        private void OnGoodsProduced(MaterialData material)
        {
            // 商业区消耗货物获得税收加成
            int commercialBonus = material.baseValue * 2;
            if (simulation != null)
            {
                simulation.Metrics.TaxIncome += commercialBonus;
            }

            // 工业产出增加就业满意度
            if (material.tier == MaterialTier.Basic)
            {
                if (simulation != null)
                {
                    simulation.Metrics.Happiness += 1;
                }
            }
        }

        /// <summary>
        /// 检查是否有足够材料升级
        /// </summary>
        public bool CanUpgradeBuilding(string buildingId)
        {
            if (simulation == null || storage == null) return false;

            var building = simulation.FindPlacedBuilding(buildingId);
            if (building == null || building.Level >= 5) return false;

            // 检查天数要求
            if (!simulation.CanUpgradeBuilding(buildingId, out int requiredDays))
                return false;

            // 检查材料（使用ID映射）
            string[] requiredMaterials = GetUpgradeMaterials(building.Level);
            requiredMaterials = MaterialIdMapper.NormalizeIds(requiredMaterials);

            foreach (var material in requiredMaterials)
            {
                if (storage.GetItemCount(material) < 1)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 尝试使用材料升级建筑
        /// </summary>
        public bool TryUpgradeWithMaterials(string buildingId)
        {
            if (!CanUpgradeBuilding(buildingId)) return false;

            var building = simulation.FindPlacedBuilding(buildingId);
            string[] materials = GetUpgradeMaterials(building.Level);
            materials = MaterialIdMapper.NormalizeIds(materials);

            // 消耗材料
            foreach (var material in materials)
            {
                if (!storage.RemoveItem(material, 1))
                    return false;
            }

            // 升级建筑
            bool success = simulation.TryUpgradeBuildingWithMaterials(buildingId);

            if (success && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingUpgrade);
            }

            return success;
        }

        private string[] GetUpgradeMaterials(int currentLevel)
        {
            switch (currentLevel)
            {
                case 1: return new[] { "nails", "wood" };
                case 2: return new[] { "nails", "wood", "planks" };
                case 3: return new[] { "planks", "bricks", "cement" };
                case 4: return new[] { "bricks", "cement", "steel" };
                default: return new string[0];
            }
        }

        /// <summary>
        /// 获取升级材料需求（用于UI显示）
        /// </summary>
        public string GetUpgradeRequirementsText(string buildingId)
        {
            var building = simulation?.FindPlacedBuilding(buildingId);
            if (building == null) return "";

            string[] materials = GetUpgradeMaterials(building.Level);
            if (materials.Length == 0) return "已达最高等级";

            string text = "需要材料：";
            foreach (var material in materials)
            {
                int has = storage.GetItemCount(material);
                text += $"\n{material}: {has}/1";
            }
            return text;
        }
    }


}
