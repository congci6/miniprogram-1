using PocketCity.Core;

namespace PocketCity.Simulation
{
    /// <summary>
    /// 幸福度奖励系统
    /// 高幸福度给予正向激励
    /// </summary>
    public static class HappinessRewardSystem
    {
        /// <summary>
        /// 计算幸福度税收加成
        /// </summary>
        public static float GetTaxBonus(int happiness)
        {
            if (happiness >= 90) return 0.20f;  // 90+ : +20%
            if (happiness >= 80) return 0.10f;  // 80-89: +10%
            if (happiness >= 70) return 0.05f;  // 70-79: +5%
            return 0f;
        }

        /// <summary>
        /// 计算幸福度人口增长加成
        /// </summary>
        public static float GetPopulationGrowthBonus(int happiness)
        {
            if (happiness >= 90) return 0.30f;  // 90+ : +30%
            if (happiness >= 80) return 0.20f;  // 80-89: +20%
            if (happiness >= 70) return 0.10f;  // 70-79: +10%
            return 0f;
        }

        /// <summary>
        /// 计算幸福度服务效率加成
        /// </summary>
        public static float GetServiceEfficiencyBonus(int happiness)
        {
            if (happiness >= 90) return 0.15f;  // 90+ : +15%
            if (happiness >= 80) return 0.10f;  // 80-89: +10%
            if (happiness >= 70) return 0.05f;  // 70-79: +5%
            return 0f;
        }

        /// <summary>
        /// 检查是否解锁特殊建筑
        /// </summary>
        public static bool CanUnlockSpecialBuilding(int happiness, int population)
        {
            return happiness >= 85 && population >= 500;
        }

        /// <summary>
        /// 获取幸福度描述文本
        /// </summary>
        public static string GetHappinessRewardText(int happiness)
        {
            if (happiness >= 90)
                return "🌟 极高幸福度：税收+20%，人口增长+30%，服务效率+15%";
            if (happiness >= 80)
                return "😊 高幸福度：税收+10%，人口增长+20%，服务效率+10%";
            if (happiness >= 70)
                return "🙂 良好幸福度：税收+5%，人口增长+10%，服务效率+5%";
            if (happiness >= 60)
                return "😐 中等幸福度：暂无加成";
            if (happiness >= 50)
                return "😕 偏低幸福度：需要改善";
            return "😢 低幸福度：居民不满";
        }
    }
}
