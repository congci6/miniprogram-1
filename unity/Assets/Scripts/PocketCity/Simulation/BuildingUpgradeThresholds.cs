namespace PocketCity.Simulation
{
    /// <summary>
    /// 建筑升级阈值配置
    /// 明确的升级时间和进度提示
    /// </summary>
    public static class BuildingUpgradeThresholds
    {
        // 升级所需天数
        public const int LEVEL_2_DAYS = 60;   // 2个月
        public const int LEVEL_3_DAYS = 120;  // 4个月
        public const int LEVEL_4_DAYS = 180;  // 6个月
        public const int LEVEL_5_DAYS = 250;  // 8个月+

        // 加速升级成本（金币）
        public const int ACCELERATE_COST_BASE = 200;
        public const float ACCELERATE_COST_MULTIPLIER = 1.5f;

        /// <summary>
        /// 获取升级到下一级所需天数
        /// </summary>
        public static int GetDaysForNextLevel(int currentLevel)
        {
            switch (currentLevel)
            {
                case 1: return LEVEL_2_DAYS;
                case 2: return LEVEL_3_DAYS;
                case 3: return LEVEL_4_DAYS;
                case 4: return LEVEL_5_DAYS;
                default: return int.MaxValue; // 已达最高级
            }
        }

        /// <summary>
        /// 计算升级进度百分比
        /// </summary>
        public static float GetUpgradeProgress(int ageDays, int currentLevel)
        {
            var requiredDays = GetDaysForNextLevel(currentLevel);
            if (requiredDays == int.MaxValue) return 1f; // 已满级

            var progress = (float)ageDays / requiredDays;
            return progress < 0f ? 0f : (progress > 1f ? 1f : progress);
        }

        /// <summary>
        /// 检查是否可以升级
        /// </summary>
        public static bool CanUpgrade(int ageDays, int currentLevel, int maxLevel = 5)
        {
            if (currentLevel >= maxLevel) return false;
            return ageDays >= GetDaysForNextLevel(currentLevel);
        }

        /// <summary>
        /// 计算加速升级成本
        /// </summary>
        public static int GetAccelerateCost(int currentLevel, int remainingDays)
        {
            var baseCost = ACCELERATE_COST_BASE * currentLevel;
            var timeFactor = remainingDays / 10f;
            return (int)(baseCost * timeFactor * ACCELERATE_COST_MULTIPLIER);
        }

        /// <summary>
        /// 获取升级进度描述
        /// </summary>
        public static string GetProgressDescription(int ageDays, int currentLevel)
        {
            var requiredDays = GetDaysForNextLevel(currentLevel);
            if (requiredDays == int.MaxValue)
                return "✅ 已达最高等级";

            var progress = GetUpgradeProgress(ageDays, currentLevel);
            if (progress >= 1f)
                return "🎉 可升级！";

            var remaining = requiredDays - ageDays;
            return $"⏳ 升级进度: {(int)(progress * 100)}% ({remaining}天后可升级)";
        }

        /// <summary>
        /// 获取等级名称
        /// </summary>
        public static string GetLevelName(int level)
        {
            switch (level)
            {
                case 1: return "初级";
                case 2: return "中级";
                case 3: return "高级";
                case 4: return "精英";
                case 5: return "顶级";
                default: return $"等级{level}";
            }
        }
    }
}
