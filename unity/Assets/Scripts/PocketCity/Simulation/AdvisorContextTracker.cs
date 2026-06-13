using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Simulation
{
    public class AdvisorContextTracker
    {
        private const int MaxRecentActions = 10;
        private const float MinDisplayInterval = 60f;

        private readonly Queue<string> recentActions = new Queue<string>(MaxRecentActions);
        private readonly Dictionary<string, int> actionCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, float> lastShownTime = new Dictionary<string, float>();
        private float lastActionTime;

        public void RecordAction(string actionType)
        {
            if (string.IsNullOrEmpty(actionType))
            {
                return;
            }

            recentActions.Enqueue(actionType);
            while (recentActions.Count > MaxRecentActions)
            {
                recentActions.Dequeue();
            }

            int count;
            actionCounts.TryGetValue(actionType, out count);
            actionCounts[actionType] = count + 1;
            lastActionTime = SimulationTime.Time;
        }

        public bool ShouldBoostAdvisor(string advisorType)
        {
            if (string.IsNullOrEmpty(advisorType))
            {
                return false;
            }

            if ((HasRecentAction("build_school") || HasRecentAction("build_clinic") || HasRecentAction("build_service"))
                && advisorType == "SERVICE_GAP_ADVISOR")
            {
                return true;
            }

            if ((HasRecentAction("build_road") || HasRecentAction("upgrade_road"))
                && (advisorType == "ROAD_HIERARCHY_ADVISOR" || advisorType == "COMMUTE_CORRIDOR_ADVISOR"))
            {
                return true;
            }

            if (HasRecentAction("set_zone")
                && (advisorType == "DEMAND_DRIVER_ANALYSIS" || advisorType == "GROWTH_BOTTLENECK_ADVISOR" || advisorType == "HOUSING_AFFORDABILITY_ADVISOR"))
            {
                return true;
            }

            if ((HasRecentAction("cycle_tax") || HasRecentAction("cycle_budget") || HasRecentAction("issue_bond"))
                && advisorType == "BUDGET_BREAKDOWN_ADVISOR")
            {
                return true;
            }

            if ((HasRecentAction("build_power") || HasRecentAction("build_water") || HasRecentAction("build_stormwater"))
                && advisorType == "INFRASTRUCTURE_RESILIENCE_ADVISOR")
            {
                return true;
            }

            if (HasRecentAction("toggle_policy")
                && advisorType == "DISTRICT_PRIORITY_ADVISOR")
            {
                return true;
            }

            return false;
        }

        public bool ShouldShowAdvisor(string advisorType)
        {
            if (string.IsNullOrEmpty(advisorType))
            {
                return false;
            }

            float shownAt;
            if (!lastShownTime.TryGetValue(advisorType, out shownAt))
            {
                return true;
            }

            return SimulationTime.Time - shownAt >= MinDisplayInterval;
        }

        public void MarkAdvisorShown(string advisorType)
        {
            if (!string.IsNullOrEmpty(advisorType))
            {
                lastShownTime[advisorType] = SimulationTime.Time;
            }
        }

        public string GetContextHint(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.Cash < 1000 && HasRecentAction("build"))
            {
                return "\u8d44\u91d1\u7d27\u5f20 > \u5148\u67e5\u9884\u7b97\u62c6\u89e3";
            }

            if (metrics.ServiceGapPressure > 60 && !HasRecentAction("build_service"))
            {
                return "\u670d\u52a1\u7f3a\u53e3\u8f83\u9ad8 > \u8865\u516c\u5171\u670d\u52a1";
            }

            if (metrics.RoadBottleneckPressure > 70 && !HasRecentAction("build_road"))
            {
                return "\u4ea4\u901a\u62e5\u5835\u52a0\u5267 > \u4f18\u5316\u8def\u7f51";
            }

            return string.Empty;
        }

        public float GetActionRecency()
        {
            var timeSinceAction = SimulationTime.Time - lastActionTime;
            if (timeSinceAction < 30f) return 1.0f;
            if (timeSinceAction < 60f) return 0.7f;
            if (timeSinceAction < 120f) return 0.4f;
            return 0.1f;
        }

        public void Reset()
        {
            recentActions.Clear();
            actionCounts.Clear();
            lastShownTime.Clear();
        }

        private bool HasRecentAction(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            foreach (var action in recentActions)
            {
                if (action.StartsWith(prefix))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
