using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;
using PocketCity.Disaster;

namespace PocketCity.Disaster
{
    /// <summary>
    /// 灾难战后奖励系统
    /// </summary>
    public class DisasterRewardSystem : MonoBehaviour
    {
        public static DisasterRewardSystem Instance { get; private set; }

        [SerializeField] private DisasterSystem disasterSystem;

        private Dictionary<DisasterType, int> disasterSurvivalCount = new Dictionary<DisasterType, int>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            InitializeCounts();
        }

        private void InitializeCounts()
        {
            foreach (DisasterType type in System.Enum.GetValues(typeof(DisasterType)))
            {
                disasterSurvivalCount[type] = 0;
            }
        }

        /// <summary>
        /// 灾难结束后调用，发放奖励
        /// </summary>
        public void OnDisasterEnded(DisasterType type, int level, int buildingsDestroyed)
        {
            disasterSurvivalCount[type]++;

            // 基础奖励
            GiveBasicRewards(type, level);

            // 特殊奖励（首次击退）
            if (disasterSurvivalCount[type] == 1)
            {
                GiveFirstTimeReward(type);
            }

            // 里程碑奖励（每5次）
            if (disasterSurvivalCount[type] % 5 == 0)
            {
                GiveMilestoneReward(type, disasterSurvivalCount[type]);
            }

            // 完美防御奖励（0建筑损毁）
            if (buildingsDestroyed == 0)
            {
                GivePerfectDefenseReward(type, level);
            }
        }

        private void GiveBasicRewards(DisasterType type, int level)
        {
            // 金币奖励
            int cashReward = 500 + level * 200;
            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddCash(cashReward);
            }

            // 金钥匙奖励
            int keyReward = level >= 3 ? 1 : 0;
            if (keyReward > 0 && UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddGoldenKeys(keyReward);
            }

            Debug.Log($"灾难奖励：{cashReward} 金币 + {keyReward} 金钥匙");
        }

        private void GiveFirstTimeReward(DisasterType type)
        {
            string rewardName = GetFirstTimeRewardName(type);
            int cashBonus = 2000;

            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddCash(cashBonus);
            }

            // 解锁特殊装饰建筑
            UnlockUniqueBuilding(type);

            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.Achievement,
                    "🎉 首次击退！",
                    $"获得 {rewardName}\n+{cashBonus} 金币",
                    Vector3.zero
                );
            }

            Debug.Log($"首次击退 {type}：解锁 {rewardName}");
        }

        private void GiveMilestoneReward(DisasterType type, int count)
        {
            int cashBonus = count * 500;
            int premiumBonus = count / 5;

            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddCash(cashBonus);
                UnifiedCurrencySystem.Instance.AddPremium(premiumBonus);
            }

            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.Achievement,
                    "🏆 里程碑达成！",
                    $"击退 {type} {count} 次\n+{cashBonus} 金币 +{premiumBonus} 高级货币",
                    Vector3.zero
                );
            }
        }

        private void GivePerfectDefenseReward(DisasterType type, int level)
        {
            int cashBonus = 1000 + level * 300;
            int keyBonus = 1;

            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddCash(cashBonus);
                UnifiedCurrencySystem.Instance.AddGoldenKeys(keyBonus);
            }

            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.Achievement,
                    "💎 完美防御！",
                    $"无建筑损毁\n+{cashBonus} 金币 +{keyBonus} 金钥匙",
                    Vector3.zero
                );
            }
        }

        private void UnlockUniqueBuilding(DisasterType type)
        {
            // TODO: 实际解锁建筑逻辑
            string buildingId = GetUniqueBuildingId(type);
            Debug.Log($"解锁唯一建筑：{buildingId}");
        }

        private string GetFirstTimeRewardName(DisasterType type)
        {
            return type switch
            {
                DisasterType.Earthquake => "抗震建筑标准（升级材料-10%折扣）",
                DisasterType.Tornado => "风车装饰建筑",
                DisasterType.Meteor => "陨石坑公园",
                DisasterType.Fire => "消防经验勋章 + 重建补贴",
                DisasterType.Alien => "UFO纪念碑",
                DisasterType.Robot => "机器人残骸雕塑",
                DisasterType.Monster => "怪兽博物馆",
                _ => "特殊奖励"
            };
        }

        private string GetUniqueBuildingId(DisasterType type)
        {
            return type switch
            {
                DisasterType.Earthquake => "earthquake_memorial",
                DisasterType.Tornado => "windmill_decoration",
                DisasterType.Meteor => "meteor_crater_park",
                DisasterType.Fire => "fire_memorial",
                DisasterType.Alien => "ufo_monument",
                DisasterType.Robot => "robot_sculpture",
                DisasterType.Monster => "monster_museum",
                _ => "disaster_memorial"
            };
        }

        /// <summary>
        /// 获取灾难统计
        /// </summary>
        public string GetDisasterStats()
        {
            string stats = "灾难统计：\n";
            foreach (var kvp in disasterSurvivalCount)
            {
                stats += $"{kvp.Key}: {kvp.Value} 次\n";
            }
            return stats;
        }

        /// <summary>
        /// 获取击退次数
        /// </summary>
        public int GetSurvivalCount(DisasterType type)
        {
            return disasterSurvivalCount.TryGetValue(type, out int count) ? count : 0;
        }
    }
}
