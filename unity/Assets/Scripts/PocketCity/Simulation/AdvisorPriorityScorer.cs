using System;
using System.Collections.Generic;
using System.Linq;
using PocketCity.Core;
using UnityEngine;

namespace PocketCity.Simulation
{
    /// <summary>
    /// 顾问智能优先级评分系统
    /// 根据紧急度、影响范围、可操作性、新鲜度评分
    /// 支持学习和权重自适应
    /// </summary>
    public class AdvisorPriorityScorer
    {
        private const float UrgencyWeight = 0.4f;
        private const float ImpactWeight = 0.3f;
        private const float ActionabilityWeight = 0.2f;
        private const float NoveltyWeight = 0.1f;

        private Dictionary<string, float> lastShownTime = new Dictionary<string, float>();
        private Dictionary<string, int> shownCount = new Dictionary<string, int>();
        private Dictionary<string, int> userActedCount = new Dictionary<string, int>(); // 用户采纳次数
        private AdvisorContextTracker contextTracker;

        public void SetContextTracker(AdvisorContextTracker tracker)
        {
            contextTracker = tracker;
        }

        public void ResetState()
        {
            lastShownTime.Clear();
            shownCount.Clear();
            userActedCount.Clear();
        }

        public AdvisorPrioritySaveData CreateSaveData()
        {
            var save = new AdvisorPrioritySaveData();
            var now = SimulationTime.Time;

            foreach (var pair in lastShownTime)
            {
                save.LastShownSecondsAgo.Add(new SavedStringFloatEntry
                {
                    Key = pair.Key,
                    Value = now - pair.Value
                });
            }

            foreach (var pair in shownCount)
            {
                save.ShownCounts.Add(new SavedStringIntEntry
                {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }

            foreach (var pair in userActedCount)
            {
                save.UserActedCounts.Add(new SavedStringIntEntry
                {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }

            return save;
        }

        public void ApplySaveData(AdvisorPrioritySaveData save)
        {
            ResetState();
            if (save == null)
            {
                return;
            }

            var now = SimulationTime.Time;
            if (save.LastShownSecondsAgo != null)
            {
                for (var i = 0; i < save.LastShownSecondsAgo.Count; i += 1)
                {
                    var entry = save.LastShownSecondsAgo[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.Key))
                    {
                        lastShownTime[entry.Key] = now - entry.Value;
                    }
                }
            }

            if (save.ShownCounts != null)
            {
                for (var i = 0; i < save.ShownCounts.Count; i += 1)
                {
                    var entry = save.ShownCounts[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Value > 0)
                    {
                        shownCount[entry.Key] = entry.Value;
                    }
                }
            }

            if (save.UserActedCounts != null)
            {
                for (var i = 0; i < save.UserActedCounts.Count; i += 1)
                {
                    var entry = save.UserActedCounts[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Value > 0)
                    {
                        userActedCount[entry.Key] = entry.Value;
                    }
                }
            }
        }

        // 记录用户采纳了某个顾问的建议
        public void RecordUserAction(string advisorType)
        {
            userActedCount[advisorType] = userActedCount.GetValueOrDefault(advisorType, 0) + 1;
        }

        public class ScoredInsight
        {
            public string Type;
            public string Message;
            public float Score;
            public float Urgency;
            public float Impact;
            public float Actionability;
            public float Novelty;
        }

        public float CalculatePriority(string advisorType, string message, CityMetrics metrics)
        {
            var urgency = CalculateUrgency(advisorType, metrics);
            var impact = CalculateImpact(advisorType, metrics);
            var actionability = CalculateActionability(advisorType, metrics);
            var novelty = CalculateNovelty(advisorType);

            var score = urgency * UrgencyWeight
                 + impact * ImpactWeight
                 + actionability * ActionabilityWeight
                 + novelty * NoveltyWeight;

            // 应用上下文增强
            if (contextTracker != null && contextTracker.ShouldBoostAdvisor(advisorType))
            {
                score *= 1.3f; // 提升30%优先级
            }

            // 学习加成：用户经常采纳的顾问提升优先级
            var adoptionRate = GetAdoptionRate(advisorType);
            if (adoptionRate > 0.5f) // 超过50%采纳率
            {
                score *= (1f + adoptionRate * 0.2f); // 最多提升20%
            }

            return score;
        }

        // 计算用户采纳率
        private float GetAdoptionRate(string advisorType)
        {
            var shown = shownCount.GetValueOrDefault(advisorType, 0);
            var acted = userActedCount.GetValueOrDefault(advisorType, 0);

            if (shown == 0) return 0f;
            return Mathf.Min(1f, acted / (float)shown);
        }

        private float CalculateUrgency(string advisorType, CityMetrics metrics)
        {
            switch (advisorType)
            {
                case "RISK_FORECAST_ADVISOR":
                    return NormalizeScore(Math.Max(metrics.HealthRisk, metrics.FireRisk), 0, 100);

                case "SERVICE_GAP_ADVISOR":
                    return NormalizeScore(metrics.ServiceGapPressure, 0, 100);

                case "BUDGET_BREAKDOWN_ADVISOR":
                    if (metrics.Cash < 0) return 1.0f;
                    if (metrics.Cash < 1000) return 0.8f;
                    return 0.3f;

                case "GROWTH_BOTTLENECK_ADVISOR":
                    var housingShortage = Math.Max(0, metrics.Population - metrics.HousingCapacity);
                    return NormalizeScore(housingShortage, 0, metrics.Population * 0.2f);

                case "ROAD_HIERARCHY_ADVISOR":
                    return NormalizeScore(metrics.RoadBottleneckPressure, 0, 100);

                case "HOUSING_AFFORDABILITY_ADVISOR":
                    return NormalizeScore(metrics.RentPressure, 0, 100);

                default:
                    return 0.5f;
            }
        }

        private float CalculateImpact(string advisorType, CityMetrics metrics)
        {
            var population = Math.Max(1, metrics.Population);

            switch (advisorType)
            {
                case "SERVICE_GAP_ADVISOR":
                case "GROWTH_BOTTLENECK_ADVISOR":
                    return Math.Min(1.0f, population / 500f);

                case "BUDGET_BREAKDOWN_ADVISOR":
                    return metrics.Cash < 0 ? 1.0f : 0.6f;

                case "ROAD_HIERARCHY_ADVISOR":
                    return NormalizeScore(metrics.RoadConnectivity, 0, 100);

                case "ECONOMIC_SPECIALIZATION_ADVISOR":
                    return Math.Min(1.0f, population / 300f);

                default:
                    return 0.5f;
            }
        }

        private float CalculateActionability(string advisorType, CityMetrics metrics)
        {
            var hasCash = metrics.Cash > 5000 ? 1.0f : metrics.Cash > 1000 ? 0.6f : 0.2f;

            switch (advisorType)
            {
                case "SERVICE_GAP_ADVISOR":
                case "GROWTH_BOTTLENECK_ADVISOR":
                case "ROAD_HIERARCHY_ADVISOR":
                    return hasCash;

                case "BUDGET_BREAKDOWN_ADVISOR":
                case "HOUSING_AFFORDABILITY_ADVISOR":
                    return 0.9f;

                case "DEMAND_DRIVER_ANALYSIS":
                    return 0.7f;

                default:
                    return 0.5f;
            }
        }

        private float CalculateNovelty(string advisorType)
        {
            if (!lastShownTime.ContainsKey(advisorType))
            {
                return 1.0f;
            }

            var timeSinceShown = SimulationTime.Time - lastShownTime[advisorType];
            var count = shownCount.GetValueOrDefault(advisorType, 0);

            var timeFactor = Math.Min(1.0f, timeSinceShown / 120f);
            var repeatPenalty = 1.0f / (1.0f + count * 0.3f);

            return timeFactor * repeatPenalty;
        }

        public void MarkShown(string advisorType)
        {
            lastShownTime[advisorType] = SimulationTime.Time;
            shownCount[advisorType] = shownCount.GetValueOrDefault(advisorType, 0) + 1;
        }

        public List<ScoredInsight> ScoreAndSortInsights(Dictionary<string, string> insights, CityMetrics metrics)
        {
            var scored = new List<ScoredInsight>();

            foreach (var kvp in insights)
            {
                var urgency = CalculateUrgency(kvp.Key, metrics);
                var impact = CalculateImpact(kvp.Key, metrics);
                var actionability = CalculateActionability(kvp.Key, metrics);
                var novelty = CalculateNovelty(kvp.Key);

                scored.Add(new ScoredInsight
                {
                    Type = kvp.Key,
                    Message = kvp.Value,
                    Score = urgency * UrgencyWeight + impact * ImpactWeight + actionability * ActionabilityWeight + novelty * NoveltyWeight,
                    Urgency = urgency,
                    Impact = impact,
                    Actionability = actionability,
                    Novelty = novelty
                });
            }

            return scored.OrderByDescending(s => s.Score).ToList();
        }

        public List<ScoredInsight> GetTopInsights(Dictionary<string, string> insights, CityMetrics metrics, int count = 3)
        {
            var sorted = ScoreAndSortInsights(insights, metrics);
            var top = sorted.Take(count).ToList();

            foreach (var insight in top)
            {
                MarkShown(insight.Type);
            }

            return top;
        }

        private float NormalizeScore(float value, float min, float max)
        {
            if (max <= min) return 0.5f;
            return Math.Max(0f, Math.Min(1f, (value - min) / (max - min)));
        }
    }
}
