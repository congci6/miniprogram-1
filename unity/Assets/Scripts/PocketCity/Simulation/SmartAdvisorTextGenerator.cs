using PocketCity.Core;

namespace PocketCity.Simulation
{
    public static class SmartAdvisorTextGenerator
    {
        public static string EnhanceServiceGapAdvice(string originalText, CityMetrics metrics)
        {
            if (string.IsNullOrEmpty(originalText) || metrics == null)
            {
                return originalText;
            }

            if (metrics.ServiceGapPressure > 70)
            {
                var serviceType = GetMostNeededService(metrics);
                if (!string.IsNullOrEmpty(serviceType))
                {
                    return originalText + " > \u4f18\u5148\u8865\u5145" + serviceType;
                }
            }

            return originalText;
        }

        public static string EnhanceRoadHierarchyAdvice(string originalText, CityMetrics metrics)
        {
            if (string.IsNullOrEmpty(originalText) || metrics == null)
            {
                return originalText;
            }

            if (metrics.RoadBottleneckPressure > 70)
            {
                return originalText + " > \u5347\u7ea7\u4e3b\u5e72\u9053\u7f13\u89e3\u62e5\u5835";
            }

            return originalText;
        }

        public static string EnhanceBudgetAdvice(string originalText, CityMetrics metrics)
        {
            if (string.IsNullOrEmpty(originalText) || metrics == null)
            {
                return originalText;
            }

            if (metrics.Cash < 1000)
            {
                if (metrics.TaxRatePercent < 12)
                {
                    return originalText + " > \u53ef\u8003\u8651\u63d0\u9ad8\u7a0e\u7387";
                }

                if (metrics.BondPrincipal < 5000)
                {
                    return originalText + " > \u53ef\u53d1\u884c\u503a\u5238\u7b79\u8d44";
                }
            }

            return originalText;
        }

        public static string EnhanceGrowthBottleneckAdvice(string originalText, CityMetrics metrics)
        {
            if (string.IsNullOrEmpty(originalText) || metrics == null)
            {
                return originalText;
            }

            var shortage = metrics.Population - metrics.HousingCapacity;
            if (shortage > 50)
            {
                return originalText + " > \u9700\u589e\u52a0" + shortage + "\u4eba\u4f4f\u623f\u5bb9\u91cf";
            }

            return originalText;
        }

        private static string GetMostNeededService(CityMetrics metrics)
        {
            var maxGap = 0f;
            var serviceType = string.Empty;

            UpdateServiceGap(metrics.HealthCoverage, 40, metrics.Population > 120, "\u533b\u7597", ref maxGap, ref serviceType);
            UpdateServiceGap(metrics.EducationCoverage, 40, metrics.Population > 160, "\u6559\u80b2", ref maxGap, ref serviceType);
            UpdateServiceGap(metrics.SafetyCoverage, 40, metrics.Population > 200, "\u6d88\u9632", ref maxGap, ref serviceType);
            UpdateServiceGap(metrics.SecurityCoverage, 40, metrics.Population > 220, "\u6cbb\u5b89", ref maxGap, ref serviceType);
            UpdateServiceGap(metrics.ParkCoverage, 40, metrics.Population > 100, "\u516c\u56ed", ref maxGap, ref serviceType);

            return serviceType;
        }

        private static void UpdateServiceGap(int coverage, int target, bool active, string label, ref float maxGap, ref string serviceType)
        {
            if (!active || coverage >= target)
            {
                return;
            }

            var gap = target - coverage;
            if (gap <= maxGap)
            {
                return;
            }

            maxGap = gap;
            serviceType = label;
        }
    }
}
