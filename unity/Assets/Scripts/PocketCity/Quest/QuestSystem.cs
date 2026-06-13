using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Quest
{
    public enum QuestType { Production, Tax, Upgrade, Disaster, Trade, Population }

    [Serializable]
    public class Quest
    {
        public string Id;
        public string Name;
        public QuestType Type;
        public int Target;
        public int Current;
        public bool Completed => Current >= Target;
        public Dictionary<string, int> Rewards = new Dictionary<string, int>();
    }

    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        private List<Quest> dailyQuests = new List<Quest>();
        private List<Quest> vuTowerQuests = new List<Quest>();
        private float lastRefreshTime = -3600f;
        private const float RefreshCooldown = 3600f; // 1小时冷却

        public event Action<Quest> OnQuestCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void GenerateDailyQuests(int count = 3)
        {
            dailyQuests.Clear();
            for (int i = 0; i < count; i++)
            {
                dailyQuests.Add(CreateRandomQuest($"daily_{i}"));
            }
        }

        public void GenerateVuTowerQuests()
        {
            vuTowerQuests.Clear();
            for (int i = 0; i < 3; i++)
            {
                vuTowerQuests.Add(CreateRandomQuest($"vu_{i}"));
            }
        }

        Quest CreateRandomQuest(string id)
        {
            var types = (QuestType[])Enum.GetValues(typeof(QuestType));
            var type = types[UnityEngine.Random.Range(0, types.Length)];
            return new Quest
            {
                Id = id,
                Name = GetQuestName(type),
                Type = type,
                Target = GetTargetForType(type),
                Rewards = GetRewards(type)
            };
        }

        string GetQuestName(QuestType type)
        {
            switch (type)
            {
                case QuestType.Production: return "生产货物";
                case QuestType.Tax: return "收集税金";
                case QuestType.Upgrade: return "升级建筑";
                case QuestType.Disaster: return "应对灾难";
                case QuestType.Trade: return "完成交易";
                case QuestType.Population: return "增加人口";
                default: return "任务";
            }
        }

        int GetTargetForType(QuestType type)
        {
            switch (type)
            {
                case QuestType.Production: return UnityEngine.Random.Range(5, 15);
                case QuestType.Tax: return UnityEngine.Random.Range(1000, 5000);
                case QuestType.Upgrade: return UnityEngine.Random.Range(2, 5);
                case QuestType.Disaster: return UnityEngine.Random.Range(1, 3);
                case QuestType.Trade: return UnityEngine.Random.Range(3, 8);
                case QuestType.Population: return UnityEngine.Random.Range(10, 50);
                default: return 1;
            }
        }

        Dictionary<string, int> GetRewards(QuestType type)
        {
            var rewards = new Dictionary<string, int>();
            rewards[RewardKeys.Coins] = UnityEngine.Random.Range(500, 2000);
            if (UnityEngine.Random.value > 0.7f) rewards[RewardKeys.GoldenKeys] = 1;
            if (UnityEngine.Random.value > 0.9f) rewards[RewardKeys.Simcash] = UnityEngine.Random.Range(5, 20);
            return rewards;
        }

        public void UpdateProgress(QuestType type, int amount)
        {
            UpdateQuestList(dailyQuests, type, amount);
            UpdateQuestList(vuTowerQuests, type, amount);
        }

        void UpdateQuestList(List<Quest> quests, QuestType type, int amount)
        {
            foreach (var quest in quests)
            {
                if (quest.Type == type && !quest.Completed)
                {
                    quest.Current = Math.Min(quest.Current + amount, quest.Target);
                    if (quest.Completed) OnQuestCompleted?.Invoke(quest);
                }
            }
        }

        public bool CanRefreshQuest()
        {
            return Time.time - lastRefreshTime >= RefreshCooldown;
        }

        public void RefreshQuest(string questId)
        {
            if (!CanRefreshQuest()) return;
            lastRefreshTime = Time.time;
            GenerateDailyQuests();
        }
        public List<Quest> GetDailyQuests() => dailyQuests;
        public List<Quest> GetVuTowerQuests() => vuTowerQuests;
    }
}
