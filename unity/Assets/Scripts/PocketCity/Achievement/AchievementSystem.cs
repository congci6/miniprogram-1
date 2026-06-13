using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Achievement
{
    public enum AchievementType
    {
        PopulationMilestone,
        BuildingCount,
        MoneyEarned,
        ProductionComplete,
        DisasterSurvived,
        HappinessHigh,
        CityAge
    }

    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public AchievementType type;
        public int targetValue;
        public int premiumReward;
        public int goldenKeyReward;
        public bool unlocked;
        public int progress;
    }

    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        [SerializeField] private List<Achievement> achievements = new List<Achievement>();

        public event System.Action<Achievement> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            InitializeAchievements();
            LoadProgress();
        }

        private void InitializeAchievements()
        {
            if (achievements.Count > 0) return;

            achievements.Add(new Achievement
            {
                id = "pop_1000",
                title = "小镇镇长",
                description = "城市人口达到1000",
                type = AchievementType.PopulationMilestone,
                targetValue = 1000,
                premiumReward = 50,
                goldenKeyReward = 1
            });

            achievements.Add(new Achievement
            {
                id = "pop_5000",
                title = "城市市长",
                description = "城市人口达到5000",
                type = AchievementType.PopulationMilestone,
                targetValue = 5000,
                premiumReward = 100,
                goldenKeyReward = 2
            });

            achievements.Add(new Achievement
            {
                id = "building_50",
                title = "建筑大师",
                description = "建造50座建筑",
                type = AchievementType.BuildingCount,
                targetValue = 50,
                premiumReward = 30,
                goldenKeyReward = 1
            });

            achievements.Add(new Achievement
            {
                id = "money_100k",
                title = "富可敌国",
                description = "累计赚取100,000金币",
                type = AchievementType.MoneyEarned,
                targetValue = 100000,
                premiumReward = 75,
                goldenKeyReward = 2
            });

            achievements.Add(new Achievement
            {
                id = "production_100",
                title = "工业巨头",
                description = "完成100次生产",
                type = AchievementType.ProductionComplete,
                targetValue = 100,
                premiumReward = 50,
                goldenKeyReward = 1
            });

            achievements.Add(new Achievement
            {
                id = "happiness_90",
                title = "幸福城市",
                description = "幸福度保持90以上",
                type = AchievementType.HappinessHigh,
                targetValue = 90,
                premiumReward = 100,
                goldenKeyReward = 3
            });
        }

        public void UpdateProgress(AchievementType type, int value)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.unlocked || achievement.type != type) continue;

                achievement.progress = value;

                if (achievement.progress >= achievement.targetValue)
                {
                    UnlockAchievement(achievement);
                }
            }

            SaveProgress();
        }

        private void UnlockAchievement(Achievement achievement)
        {
            achievement.unlocked = true;

            // 发放奖励
            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddPremium(achievement.premiumReward);
                UnifiedCurrencySystem.Instance.AddGoldenKeys(achievement.goldenKeyReward);
            }

            OnAchievementUnlocked?.Invoke(achievement);

            SaveProgress();
        }

        public List<Achievement> GetAllAchievements()
        {
            return new List<Achievement>(achievements);
        }

        public List<Achievement> GetUnlockedAchievements()
        {
            return achievements.FindAll(a => a.unlocked);
        }

        public int GetUnlockedCount()
        {
            return achievements.FindAll(a => a.unlocked).Count;
        }

        public float GetCompletionPercentage()
        {
            if (achievements.Count == 0) return 0f;
            return (float)GetUnlockedCount() / achievements.Count * 100f;
        }

        private void SaveProgress()
        {
            foreach (var achievement in achievements)
            {
                PlayerPrefs.SetInt("Achievement_" + achievement.id + "_Unlocked", achievement.unlocked ? 1 : 0);
                PlayerPrefs.SetInt("Achievement_" + achievement.id + "_Progress", achievement.progress);
            }
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            foreach (var achievement in achievements)
            {
                achievement.unlocked = PlayerPrefs.GetInt("Achievement_" + achievement.id + "_Unlocked", 0) == 1;
                achievement.progress = PlayerPrefs.GetInt("Achievement_" + achievement.id + "_Progress", 0);
            }
        }
    }
}
