using UnityEngine;
using System.Collections.Generic;
using PocketCity.Materials;
using PocketCity.Simulation;

namespace PocketCity.Integration
{
    /// <summary>
    /// 统一升级材料管理器 - 解决F-7双材料系统冲突
    /// 整合UpgradeMaterialSystem和UnifiedStorageBridge
    /// </summary>
    public class UnifiedUpgradeManager : MonoBehaviour
    {
        public static UnifiedUpgradeManager Instance { get; private set; }

        [SerializeField] private UpgradeMaterialSystem materialSystem;
        [SerializeField] private CitySimulationCore simulation;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (materialSystem == null)
                materialSystem = FindAnyObjectByType<UpgradeMaterialSystem>();

            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }
        }

        /// <summary>
        /// 获取建筑升级需求（统一入口）
        /// </summary>
        public Dictionary<string, int> GetBuildingRequirements(string buildingId, int currentLevel)
        {
            if (materialSystem == null)
                return new Dictionary<string, int>();

            // 使用UpgradeMaterialSystem作为主系统
            return materialSystem.GetBuildingRequirements(buildingId, currentLevel);
        }

        /// <summary>
        /// 尝试升级建筑（统一入口，F-7修复）
        /// </summary>
        public bool TryUpgradeBuilding(string buildingId, int currentLevel)
        {
            if (materialSystem == null || simulation == null)
                return false;

            // 获取需求
            var requirements = GetBuildingRequirements(buildingId, currentLevel);
            if (requirements == null || requirements.Count == 0)
            {
                Debug.LogWarning($"建筑 {buildingId} Lv.{currentLevel} 无升级需求定义");
                return false;
            }

            // 检查材料是否足够
            foreach (var req in requirements)
            {
                int has = materialSystem.GetAmount(req.Key);
                if (has < req.Value)
                {
                    Debug.Log($"材料不足：{req.Key} 需要 {req.Value}，拥有 {has}");
                    return false;
                }
            }

            // 消耗材料
            foreach (var req in requirements)
            {
                if (!materialSystem.Remove(req.Key, req.Value))
                {
                    Debug.LogError($"移除材料失败：{req.Key}");
                    return false;
                }
            }

            // 实际升级建筑
            var building = simulation.FindPlacedBuilding(buildingId);
            if (building != null)
            {
                building.Level++;
                simulation.MarkMetricsDirty();
                simulation.RecomputeMetrics();

                Debug.Log($"✅ 建筑升级成功：{buildingId} → Lv.{building.Level}");

                // 播放音效
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingUpgrade);
                }

                // 通知
                if (Notifications.NotificationSystem.Instance != null)
                {
                    Notifications.NotificationSystem.Instance.ShowNotification(
                        Notifications.NotificationType.BuildingUpgrade,
                        "建筑升级",
                        $"已升级到 Lv.{building.Level}",
                        building.FootprintOrigin.ToVector3()
                    );
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否可以升级
        /// </summary>
        public bool CanUpgradeBuilding(string buildingId, int currentLevel)
        {
            if (materialSystem == null)
                return false;

            var requirements = GetBuildingRequirements(buildingId, currentLevel);
            if (requirements == null || requirements.Count == 0)
                return false;

            foreach (var req in requirements)
            {
                int has = materialSystem.GetAmount(req.Key);
                if (has < req.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取升级需求文本（UI显示）
        /// </summary>
        public string GetUpgradeRequirementsText(string buildingId, int currentLevel)
        {
            var requirements = GetBuildingRequirements(buildingId, currentLevel);
            if (requirements == null || requirements.Count == 0)
                return "无升级需求";

            string text = $"升级到 Lv.{currentLevel + 1} 需要：\n";

            foreach (var req in requirements)
            {
                int has = materialSystem?.GetMaterialAmount(req.Key) ?? 0;
                string checkMark = has >= req.Value ? "✅" : "❌";
                text += $"{checkMark} {req.Key}: {has}/{req.Value}\n";
            }

            return text;
        }

        /// <summary>
        /// 添加材料
        /// </summary>
        public void AddMaterial(string materialId, int amount)
        {
            if (materialSystem != null)
            {
                materialSystem.AddMaterial(materialId, amount);
            }
        }

        /// <summary>
        /// 移除材料
        /// </summary>
        public bool RemoveMaterial(string materialId, int amount)
        {
            if (materialSystem == null)
                return false;

            return materialSystem.RemoveMaterial(materialId, amount);
        }

        /// <summary>
        /// 获取材料数量
        /// </summary>
        public int GetMaterialAmount(string materialId)
        {
            if (materialSystem == null)
                return 0;

            return materialSystem.GetAmount(materialId);
        }
    }
}
