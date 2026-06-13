using UnityEngine;
using System.Collections.Generic;
using System;

namespace PocketCity.Achievements
{
    [Serializable]
    public class AchievementDef
    {
        public string id;
        public string name;
        public string description;
        public AchievementCategory category;
        public int target;
        public RewardType rewardType;
        public int rewardAmount;
        public string rewardBuildingId;
        public bool isUnlocked;
        public int currentProgress;
    }

    public enum AchievementCategory
    {
        Building,      // 建筑相关
        Production,    // 生产相关
        Population,    // 人口相关
        Disaster,      // 灾难相关
        Economy,       // 经济相关
        Service,       // 服务相关
        Specialization // 专精相关
    }

    public enum RewardType
    {
        Cash,
        GoldenKey,
        Premium,
        UniqueBuilding,
        PopulationBoost,
        MaterialDiscount
    }

    /// <summary>
    /// 扩展成就系统 - 30+成就定义
    /// </summary>
    public class ExtendedAchievementSystem : MonoBehaviour
    {
        public static ExtendedAchievementSystem Instance { get; private set; }

        private List<AchievementDef> achievements = new List<AchievementDef>();

        public event Action<AchievementDef> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            InitializeAchievements();
            LoadProgress();
        }

        private void InitializeAchievements()
        {
            achievements.Clear();

            // === 建筑相关 (8个) ===
            AddAchievement("build_first", "初出茅庐", "建造第一栋建筑", AchievementCategory.Building, 1, RewardType.Cash, 500);
            AddAchievement("build_10", "小镇规划师", "建造10栋建筑", AchievementCategory.Building, 10, RewardType.GoldenKey, 1);
            AddAchievement("build_50", "城市建设者", "建造50栋建筑", AchievementCategory.Building, 50, RewardType.GoldenKey, 2);
            AddAchievement("build_100", "大都市设计师", "建造100栋建筑", AchievementCategory.Building, 100, RewardType.Premium, 50);
            AddAchievement("upgrade_10", "升级专家", "升级10次建筑", AchievementCategory.Building, 10, RewardType.Cash, 2000);
            AddAchievement("max_level_5", "满级收藏家", "拥有5栋满级建筑", AchievementCategory.Building, 5, RewardType.GoldenKey, 3);
            AddAchievement("residential_5", "住宅系列", "建齐5种不同住宅", AchievementCategory.Building, 5, RewardType.UniqueBuilding, 0, "town_hall_small");
            AddAchievement("service_5", "服务系列", "建齐5种服务建筑", AchievementCategory.Building, 5, RewardType.UniqueBuilding, 0, "city_hall");

            // === 生产相关 (7个) ===
            AddAchievement("produce_first", "工厂启动", "完成第一次生产", AchievementCategory.Production, 1, RewardType.Cash, 300);
            AddAchievement("produce_100", "生产大师", "完成100次生产", AchievementCategory.Production, 100, RewardType.GoldenKey, 2);
            AddAchievement("produce_tier4", "高级制造", "生产第一个Tier4材料", AchievementCategory.Production, 1, RewardType.Cash, 5000);
            AddAchievement("factory_upgrade", "工厂扩张", "升级一座工厂到Lv.3", AchievementCategory.Production, 1, RewardType.GoldenKey, 2);
            AddAchievement("material_100", "囤积狂", "仓库存储100件材料", AchievementCategory.Production, 100, RewardType.Cash, 3000);
            AddAchievement("cargo_10", "货运专家", "完成10个货运订单", AchievementCategory.Production, 10, RewardType.GoldenKey, 3);
            AddAchievement("urgent_5", "紧急响应", "完成5个紧急订单", AchievementCategory.Production, 5, RewardType.Premium, 100);

            // === 人口相关 (5个) ===
            AddAchievement("pop_100", "小村庄", "人口达到100", AchievementCategory.Population, 100, RewardType.Cash, 1000);
            AddAchievement("pop_1000", "小镇", "人口达到1000", AchievementCategory.Population, 1000, RewardType.GoldenKey, 2);
            AddAchievement("pop_5000", "城市", "人口达到5000", AchievementCategory.Population, 5000, RewardType.Premium, 50);
            AddAchievement("pop_10000", "大都市", "人口达到10000", AchievementCategory.Population, 10000, RewardType.Premium, 100);
            AddAchievement("happiness_90", "幸福之城", "幸福度达到90", AchievementCategory.Population, 90, RewardType.PopulationBoost, 10);

            // === 灾难相关 (5个) ===
            AddAchievement("survive_first", "灾后重建", "击退第一次灾难", AchievementCategory.Disaster, 1, RewardType.Cash, 2000);
            AddAchievement("survive_10", "灾难守护者", "击退10次灾难", AchievementCategory.Disaster, 10, RewardType.GoldenKey, 3);
            AddAchievement("perfect_defense", "完美防御", "完美防御（0损毁）1次", AchievementCategory.Disaster, 1, RewardType.Premium, 50);
            AddAchievement("all_disaster_types", "灾难百科", "击退所有7种灾难", AchievementCategory.Disaster, 7, RewardType.UniqueBuilding, 0, "disaster_museum");
            AddAchievement("debris_clear_10", "废墟清理工", "清理10个废墟", AchievementCategory.Disaster, 10, RewardType.Cash, 5000);

            // === 经济相关 (3个) ===
            AddAchievement("cash_50k", "小富即安", "拥有50000金币", AchievementCategory.Economy, 50000, RewardType.GoldenKey, 1);
            AddAchievement("cash_200k", "百万富翁", "拥有200000金币", AchievementCategory.Economy, 200000, RewardType.Premium, 100);
            AddAchievement("tax_10k", "税收大户", "单次收税10000", AchievementCategory.Economy, 10000, RewardType.Cash, 5000);

            // === 服务相关 (3个) ===
            AddAchievement("full_coverage", "全面覆盖", "所有建筑都被服务覆盖", AchievementCategory.Service, 1, RewardType.GoldenKey, 3);
            AddAchievement("transit_5", "公交网络", "建造5个公交/地铁站", AchievementCategory.Service, 5, RewardType.Cash, 8000);
            AddAchievement("road_100", "道路大师", "铺设100格道路", AchievementCategory.Service, 100, RewardType.GoldenKey, 2);

            // === 专精相关 (5个) ===
            AddAchievement("beach_5", "海滨度假", "建造5个海滩建筑", AchievementCategory.Specialization, 5, RewardType.UniqueBuilding, 0, "beach_resort_luxury");
            AddAchievement("casino_3", "赌城大亨", "建造3个赌场", AchievementCategory.Specialization, 3, RewardType.Cash, 20000);
            AddAchievement("education_5", "教育强市", "建造5个教育建筑", AchievementCategory.Specialization, 5, RewardType.PopulationBoost, 20);
            AddAchievement("all_specializations", "全面发展", "解锁所有5种专精", AchievementCategory.Specialization, 5, RewardType.Premium, 200);
            AddAchievement("master_specialization", "专精大师", "单一专精达到10个建筑", AchievementCategory.Specialization, 10, RewardType.UniqueBuilding, 0, "golden_statue");

            Debug.Log($"初始化 {achievements.Count} 个成就");
        }

        private void AddAchievement(string id, string name, string desc, AchievementCategory cat, int target, RewardType reward, int amount, string buildingId = "")
        {
            achievements.Add(new AchievementDef
            {
                id = id,
                name = name,
                description = desc,
                category = cat,
                target = target,
                rewardType = reward,
                rewardAmount = amount,
                rewardBuildingId = buildingId,
                isUnlocked = false,
                currentProgress = 0
            });
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public void UpdateProgress(string achievementId, int progress)
        {
            var achievement = achievements.Find(a => a.id == achievementId);
            if (achievement == null || achievement.isUnlocked) return;

            achievement.currentProgress = progress;

            if (achievement.currentProgress >= achievement.target)
            {
                UnlockAchievement(achievement);
            }
        }

        /// <summary>
        /// 增量更新
        /// </summary>
        public void IncrementProgress(string achievementId, int amount = 1)
        {
            var achievement = achievements.Find(a => a.id == achievementId);
            if (achievement == null || achievement.isUnlocked) return;

            achievement.currentProgress += amount;

            if (achievement.currentProgress >= achievement.target)
            {
                UnlockAchievement(achievement);
            }
        }

        private void UnlockAchievement(AchievementDef achievement)
        {
            achievement.isUnlocked = true;

            // 发放奖励
            GiveReward(achievement);

            OnAchievementUnlocked?.Invoke(achievement);

            // 通知
            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.Achievement,
                    $"🏆 成就解锁",
                    $"{achievement.name}\n{achievement.description}",
                    Vector3.zero
                );
            }

            SaveProgress();
            Debug.Log($"解锁成就：{achievement.name}");
        }

        private void GiveReward(AchievementDef achievement)
        {
            if (Economy.UnifiedCurrencyManager.Instance == null) return;

            switch (achievement.rewardType)
            {
                case RewardType.Cash:
                    Economy.UnifiedCurrencyManager.Instance.AddCash(achievement.rewardAmount);
                    break;
                case RewardType.GoldenKey:
                    Economy.UnifiedCurrencyManager.Instance.AddGoldenKeys(achievement.rewardAmount);
                    break;
                case RewardType.Premium:
                    Economy.UnifiedCurrencyManager.Instance.AddPremium(achievement.rewardAmount);
                    break;
                case RewardType.UniqueBuilding:
                    // TODO: 解锁唯一建筑
                    Debug.Log($"解锁建筑：{achievement.rewardBuildingId}");
                    break;
            }
        }

        public List<AchievementDef> GetAchievementsByCategory(AchievementCategory category)
        {
            return achievements.FindAll(a => a.category == category);
        }

        public List<AchievementDef> GetAllAchievements()
        {
            return new List<AchievementDef>(achievements);
        }

        public int GetUnlockedCount()
        {
            return achievements.FindAll(a => a.isUnlocked).Count;
        }

        public int GetTotalCount()
        {
            return achievements.Count;
        }

        private void SaveProgress()
        {
            // TODO: 持久化成就进度
        }

        private void LoadProgress()
        {
            // TODO: 加载成就进度
        }
    }
}
