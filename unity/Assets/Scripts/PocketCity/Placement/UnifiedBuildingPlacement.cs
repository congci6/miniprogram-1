using UnityEngine;
using PocketCity.Simulation;
using PocketCity.Core;
using System.Collections.Generic;

namespace PocketCity.Placement
{
    /// <summary>
    /// 统一建筑放置管理器 - 解决F-6双放置流程冲突
    /// 统一API签名，兼容旧ID和新ID
    /// </summary>
    public class UnifiedBuildingPlacement : MonoBehaviour
    {
        public static UnifiedBuildingPlacement Instance { get; private set; }

        [SerializeField] private CitySimulationCore simulation;

        // 建筑ID映射表（旧ID → 新ID）
        private Dictionary<string, string> buildingIdMap = new Dictionary<string, string>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeBuildingIdMap();
        }

        private void Start()
        {
            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }
        }

        /// <summary>
        /// 初始化建筑ID映射（兼容旧系统）
        /// </summary>
        private void InitializeBuildingIdMap()
        {
            // residential_pod → residential_1
            buildingIdMap["residential_pod"] = "residential_1";
            buildingIdMap["residential_small"] = "residential_1";
            buildingIdMap["residential_medium"] = "residential_2";
            buildingIdMap["residential_large"] = "residential_3";

            // commercial
            buildingIdMap["commercial_pod"] = "commercial_1";
            buildingIdMap["commercial_small"] = "commercial_1";
            buildingIdMap["shop"] = "commercial_2";
            buildingIdMap["market"] = "commercial_3";

            // industrial
            buildingIdMap["industrial_pod"] = "industrial_1";
            buildingIdMap["industrial_small"] = "industrial_1";
            buildingIdMap["factory"] = "industrial_2";

            Debug.Log($"建筑ID映射已初始化：{buildingIdMap.Count} 条规则");
        }

        /// <summary>
        /// 标准化建筑ID（统一入口）
        /// </summary>
        public string NormalizeBuildingId(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
                return buildingId;

            // 尝试映射
            if (buildingIdMap.TryGetValue(buildingId, out string newId))
            {
                return newId;
            }

            return buildingId;
        }

        /// <summary>
        /// 统一预览API（简化版，无rotation）
        /// </summary>
        public bool CanPlaceBuilding(string buildingId, GridPos position)
        {
            if (simulation == null)
                return false;

            // 标准化ID
            string normalizedId = NormalizeBuildingId(buildingId);

            // 调用TryPlaceBuilding预览
            return simulation.TryPlaceBuilding(normalizedId, position, out _);
        }

        /// <summary>
        /// 统一放置API（简化版）
        /// </summary>
        public bool TryPlaceBuilding(string buildingId, GridPos position, out string placedId)
        {
            placedId = null;

            if (simulation == null)
                return false;

            // 标准化ID
            string normalizedId = NormalizeBuildingId(buildingId);

            // 调用新API
            bool success = simulation.TryPlaceBuilding(normalizedId, position, out var preview);

            if (success && preview != null)
            {
                placedId = preview.buildingId;
                Debug.Log($"✅ 放置建筑：{buildingId} → {normalizedId}");
            }

            return success;
        }

        /// <summary>
        /// 快速放置（无输出参数）
        /// </summary>
        public bool TryPlaceBuilding(string buildingId, GridPos position)
        {
            return TryPlaceBuilding(buildingId, position, out _);
        }

        /// <summary>
        /// 获取建筑定义（兼容新旧ID）
        /// </summary>
        public BuildingDefinition GetBuildingDefinition(string buildingId)
        {
            if (simulation == null || simulation.Config == null)
                return null;

            string normalizedId = NormalizeBuildingId(buildingId);
            return simulation.Config.GetBuilding(normalizedId);
        }

        /// <summary>
        /// 添加自定义映射
        /// </summary>
        public void AddBuildingIdMapping(string oldId, string newId)
        {
            buildingIdMap[oldId] = newId;
            Debug.Log($"添加建筑ID映射：{oldId} → {newId}");
        }
    }
}
