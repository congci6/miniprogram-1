using UnityEngine;
using System.Collections;
using PocketCity.Disaster;
using PocketCity.Simulation;
using PocketCity.Core;

namespace PocketCity.Disaster
{
    /// <summary>
    /// 灾难恢复系统 - 解决F-15灾难废墟/战后恢复
    /// </summary>
    public class DisasterRecoverySystem : MonoBehaviour
    {
        public static DisasterRecoverySystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float autoRepairDelay = 60f; // 60秒后开始自动修复
        [SerializeField] private float repairTickInterval = 10f; // 每10秒恢复一次
        [SerializeField] private int repairPerTick = 10; // 每次恢复10点耐久度

        [Header("References")]
        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private DamageSystem damageSystem;
        [SerializeField] private DebrisCleanupSystem debrisSystem;

        private float lastDisasterTime;
        private bool isRecovering = false;

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
            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }

            if (damageSystem == null)
                damageSystem = FindAnyObjectByType<DamageSystem>();

            if (debrisSystem == null)
                debrisSystem = FindAnyObjectByType<DebrisCleanupSystem>();
        }

        /// <summary>
        /// 灾难发生时调用
        /// </summary>
        public void OnDisasterOccurred()
        {
            lastDisasterTime = Time.time;
            isRecovering = false;

            // 停止当前恢复协程
            StopAllCoroutines();

            // 启动恢复协程
            StartCoroutine(RecoveryProcess());
        }

        private IEnumerator RecoveryProcess()
        {
            // 等待自动修复延迟
            yield return new WaitForSeconds(autoRepairDelay);

            isRecovering = true;

            // 显示通知
            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.Generic,
                    "🔧 灾后恢复",
                    "城市开始自动修复...",
                    Vector3.zero
                );
            }

            // 持续修复直到所有建筑恢复
            while (HasDamagedBuildings())
            {
                RepairTick();
                yield return new WaitForSeconds(repairTickInterval);
            }

            isRecovering = false;

            // 恢复完成通知
            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.Generic,
                    "✅ 恢复完成",
                    "所有建筑已修复",
                    Vector3.zero
                );
            }
        }

        private void RepairTick()
        {
            if (simulation == null || damageSystem == null)
                return;

            int repairedCount = 0;

            foreach (var building in simulation.Buildings)
            {
                var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                if (damage != null && damage.durability < 100)
                {
                    // 恢复耐久度
                    int newDurability = (int)Mathf.Min(100, damage.durability + repairPerTick);
                    damage.durability = newDurability;

                    repairedCount++;

                    // 完全恢复时清除视觉效果
                    if (newDurability >= 100)
                    {
                        // TODO: 清除损坏视觉效果
                    }
                }
            }

            if (repairedCount > 0)
            {
                Debug.Log($"修复了 {repairedCount} 栋建筑");
            }
        }

        private bool HasDamagedBuildings()
        {
            if (simulation == null || damageSystem == null)
                return false;

            foreach (var building in simulation.Buildings)
            {
                var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                if (damage != null && damage.durability < 100)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 立即修复所有建筑（消费金币）
        /// </summary>
        public bool InstantRepairAll(int cost)
        {
            if (UnifiedCurrencySystem.Instance == null)
                return false;

            // 检查金币
            if (!UnifiedCurrencySystem.Instance.SpendCash(cost))
            {
                Debug.Log($"金币不足，需要 {cost} 金币");
                return false;
            }

            // 修复所有建筑
            if (simulation != null && damageSystem != null)
            {
                foreach (var building in simulation.Buildings)
                {
                    var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                    if (damage != null)
                    {
                        damage.durability = 100;
                    }
                }
            }

            // 停止自动恢复
            StopAllCoroutines();
            isRecovering = false;

            Debug.Log("✅ 所有建筑已立即修复");
            return true;
        }

        /// <summary>
        /// 获取总修复成本
        /// </summary>
        public int GetTotalRepairCost()
        {
            if (simulation == null || damageSystem == null)
                return 0;

            int totalCost = 0;

            foreach (var building in simulation.Buildings)
            {
                var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                if (damage != null && damage.durability < 100)
                {
                    int missingDurability = (int)(100 - damage.durability);
                    totalCost += missingDurability * 10; // 每点耐久度10金币
                }
            }

            return totalCost;
        }

        public bool IsRecovering => isRecovering;

        /// <summary>
        /// 获取恢复进度（0-1）
        /// </summary>
        public float GetRecoveryProgress()
        {
            if (simulation == null || damageSystem == null)
                return 1f;

            int totalBuildings = 0;
            int totalDurability = 0;

            foreach (var building in simulation.Buildings)
            {
                var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                if (damage != null)
                {
                    totalBuildings++;
                    totalDurability += (int)damage.durability;
                }
            }

            if (totalBuildings == 0)
                return 1f;

            return totalDurability / (float)(totalBuildings * 100);
        }
    }
}
