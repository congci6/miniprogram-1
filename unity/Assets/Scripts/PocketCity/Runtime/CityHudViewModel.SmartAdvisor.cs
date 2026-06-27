using System.Collections.Generic;
using PocketCity.Core;
using PocketCity.Simulation;
using UnityEngine;

namespace PocketCity.Runtime
{
    public static class CityHudViewModelSmartAdvisor
    {
        private const float ContextBoostScale = 1f;
        private static readonly AdvisorPriorityScorer Scorer = new AdvisorPriorityScorer();
        private static AdvisorContextTracker contextTracker;

        public static void SetContextTracker(AdvisorContextTracker tracker)
        {
            contextTracker = tracker;
            Scorer.SetContextTracker(tracker);
        }

        public static void ResetRuntimeState(AdvisorContextTracker tracker)
        {
            contextTracker = tracker;
            Scorer.ResetState();
            Scorer.SetContextTracker(tracker);
        }

        public static AdvisorPrioritySaveData ExportRuntimeState()
        {
            return Scorer.CreateSaveData();
        }

        public static void ImportRuntimeState(AdvisorContextTracker tracker, AdvisorPrioritySaveData save)
        {
            contextTracker = tracker;
            Scorer.ResetState();
            Scorer.SetContextTracker(tracker);
            Scorer.ApplySaveData(save);
        }

        public static List<string> BuildSmartInsightPriorityStack(CityHudSnapshot snapshot, CityMetrics metrics, int maxInsights = 3)
        {
            var result = new List<string>();
            if (snapshot == null || maxInsights <= 0)
            {
                return result;
            }

            var objectiveInsight = BuildObjectiveProgressInsight(snapshot);
            if (!string.IsNullOrEmpty(objectiveInsight))
            {
                result.Add(objectiveInsight);
            }

            if (metrics == null || result.Count >= maxInsights)
            {
                return result;
            }

            var candidates = new List<InsightPriority>();
            AddInsightPriority(candidates, "RISK_FORECAST_ADVISOR", SmartAdvisorTextGenerator.EnhanceBudgetAdvice(snapshot.ForecastText, metrics), metrics.ForecastRisk, 0);
            AddInsightPriority(candidates, "BUDGET_BREAKDOWN_ADVISOR", SmartAdvisorTextGenerator.EnhanceBudgetAdvice(snapshot.BudgetInsightText, metrics), metrics.BudgetStress, 1);
            AddInsightPriority(candidates, "DEMAND_DRIVER_ANALYSIS", snapshot.DemandInsightText, metrics.DemandUrgency, 2);
            AddInsightPriority(candidates, "DISTRICT_PRIORITY_ADVISOR", snapshot.DistrictPriorityText, metrics.DistrictPriorityScore, 3);
            AddInsightPriority(candidates, "ROAD_HIERARCHY_ADVISOR", SmartAdvisorTextGenerator.EnhanceRoadHierarchyAdvice(snapshot.RoadHierarchyText, metrics), metrics.RoadHierarchyPressure, 4);
            AddInsightPriority(candidates, "INFRASTRUCTURE_RESILIENCE_ADVISOR", snapshot.InfrastructureResilienceText, metrics.InfrastructureResilienceScore, 5);
            AddInsightPriority(candidates, "COMMUTE_CORRIDOR_ADVISOR", snapshot.CommuteCorridorText, metrics.CommuteCorridorScore, 6);
            AddInsightPriority(candidates, "SERVICE_GAP_ADVISOR", SmartAdvisorTextGenerator.EnhanceServiceGapAdvice(snapshot.ServiceGapText, metrics), metrics.ServiceGapAdvisorScore, 7);
            AddInsightPriority(candidates, "ECONOMIC_SPECIALIZATION_ADVISOR", snapshot.EconomicSpecializationText, metrics.EconomicSpecializationScore, 8);
            AddInsightPriority(candidates, "HOUSING_AFFORDABILITY_ADVISOR", snapshot.HousingAffordabilityText, metrics.HousingAffordabilityScore, 9);
            AddInsightPriority(candidates, "GROWTH_BOTTLENECK_ADVISOR", SmartAdvisorTextGenerator.EnhanceGrowthBottleneckAdvice(snapshot.GrowthBottleneckText, metrics), metrics.GrowthBottleneckScore, 10);
            AddInsightPriority(candidates, "BUILDING_UPGRADE_READINESS_ADVISOR", snapshot.BuildingUpgradeReadinessText, metrics.BuildingUpgradeReadinessScore, 11);

            candidates.Sort((left, right) =>
            {
                var priority = SmartPriority(right, metrics, Scorer).CompareTo(SmartPriority(left, metrics, Scorer));
                return priority != 0 ? priority : left.Order.CompareTo(right.Order);
            });

            for (var i = 0; i < candidates.Count && result.Count < maxInsights; i += 1)
            {
                if (contextTracker != null && !contextTracker.ShouldShowAdvisor(candidates[i].Type))
                {
                    continue;
                }

                result.Add(candidates[i].Text);
                MarkAdvisorShown(candidates[i].Type);
            }

            if (result.Count < maxInsights)
            {
                AppendContextHint(result, metrics, maxInsights);
            }

            if (result.Count < maxInsights && !string.IsNullOrEmpty(snapshot.RecentEventText))
            {
                result.Add(snapshot.RecentEventText);
            }

            return result;
        }

        public static string GetContextHint(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.Cash < 1000)
            {
                return "\u8d44\u91d1\u7d27\u5f20 > \u5148\u67e5\u9884\u7b97";
            }

            if (metrics.ServiceGapPressure > 60)
            {
                return "\u670d\u52a1\u7f3a\u53e3 > \u8865\u516c\u5171\u670d\u52a1";
            }

            if (metrics.RoadBottleneckPressure > 70)
            {
                return "\u4ea4\u901a\u74f6\u9888 > \u4f18\u5316\u8def\u7f51";
            }

            if (metrics.Happiness < 40)
            {
                return "\u6ee1\u610f\u5ea6\u4f4e > \u8865\u5b9c\u5c45\u6761\u4ef6";
            }

            if (metrics.Population > metrics.HousingCapacity * 0.9f)
            {
                return "\u4f4f\u623f\u4e0d\u8db3 > \u8865\u4f4f\u5b85\u5bb9\u91cf";
            }

            return string.Empty;
        }

        public static string GetAdvisorActionType(int lane, CityMetrics metrics)
        {
            foreach (var advisorType in GetAdvisorTypesForLane(lane, metrics))
            {
                return advisorType;
            }

            return string.Empty;
        }

        public static void RecordAdvisorAdoption(string advisorType)
        {
            if (!string.IsNullOrEmpty(advisorType))
            {
                Scorer.RecordUserAction(advisorType);
            }
        }

        private static void AddInsightPriority(List<InsightPriority> candidates, string type, string text, int priority, int order)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            candidates.Add(new InsightPriority
            {
                Type = type,
                Text = text,
                Priority = ClampScore(priority),
                Order = order
            });
        }

        private static float SmartPriority(InsightPriority insight, CityMetrics metrics, AdvisorPriorityScorer scorer)
        {
            return insight.Priority + scorer.CalculatePriority(insight.Type, insight.Text, metrics) * ContextBoostScale;
        }

        private static int ClampScore(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }

        private static string BuildObjectiveProgressInsight(CityHudSnapshot snapshot)
        {
            return snapshot == null || string.IsNullOrEmpty(snapshot.ObjectiveHint)
                ? string.Empty
                : snapshot.ObjectiveHint;
        }

        private static IEnumerable<string> GetAdvisorTypesForLane(int lane, CityMetrics metrics)
        {
            if (lane == 0)
            {
                var roadPressure = metrics != null ? Mathf.Max(metrics.RoadBottleneckPressure, metrics.IntersectionDelay) : 0;
                var commutePressure = metrics != null ? Mathf.Max(0, 100 - metrics.CommuteEfficiency) : 0;
                yield return roadPressure >= commutePressure
                    ? "ROAD_HIERARCHY_ADVISOR"
                    : "COMMUTE_CORRIDOR_ADVISOR";
                yield break;
            }

            if (lane == 1)
            {
                var riskPressure = metrics != null ? Mathf.Max(metrics.HealthRisk, metrics.FireRisk) : 0;
                yield return riskPressure > 80
                    ? "RISK_FORECAST_ADVISOR"
                    : "SERVICE_GAP_ADVISOR";
                yield break;
            }

            yield return "BUDGET_BREAKDOWN_ADVISOR";
        }

        private static void MarkAdvisorShown(string advisorType)
        {
            Scorer.MarkShown(advisorType);
            if (contextTracker != null)
            {
                contextTracker.MarkAdvisorShown(advisorType);
            }
        }

        private static void AppendContextHint(List<string> result, CityMetrics metrics, int maxInsights)
        {
            if (result == null || result.Count >= maxInsights)
            {
                return;
            }

            var hint = contextTracker != null
                ? contextTracker.GetContextHint(metrics)
                : GetContextHint(metrics);
            if (string.IsNullOrEmpty(hint) || result.Contains(hint))
            {
                return;
            }

            result.Add(hint);
        }

        private sealed class InsightPriority
        {
            public string Type = string.Empty;
            public string Text = string.Empty;
            public int Priority;
            public int Order;
        }
    }
}
