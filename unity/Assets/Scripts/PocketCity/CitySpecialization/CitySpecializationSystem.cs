using UnityEngine;
using PocketCity.Simulation;
using PocketCity.Specialized;

namespace PocketCity.CitySpecialization
{
    /// <summary>
    /// 专精类型枚举
    /// </summary>
    public enum SpecializationType
    {
        Beach,
        Mountain,
        Casino,
        Education,
        Entertainment
    }

    /// <summary>
    /// 城市专精效果系统 - 每条线独特机制
    /// </summary>
    public class CitySpecializationSystem : MonoBehaviour
    {
        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private SpecializedBuildingSystem specializedBuildings;

        // 专精计数
        private int beachBuildingCount = 0;
        private int mountainBuildingCount = 0;
        private int casinoBuildingCount = 0;
        private int educationBuildingCount = 0;
        private int entertainmentBuildingCount = 0;

        /// <summary>
        /// 获取指定专精的建筑数量
        /// </summary>
        public int GetSpecializationBuildingCount(SpecializationType type)
        {
            return type switch
            {
                SpecializationType.Beach => beachBuildingCount,
                SpecializationType.Mountain => mountainBuildingCount,
                SpecializationType.Casino => casinoBuildingCount,
                SpecializationType.Education => educationBuildingCount,
                SpecializationType.Entertainment => entertainmentBuildingCount,
                _ => 0
            };
        }

        private void Update()
        {
            if (simulation == null) return;

            // 每秒更新一次
            if (Time.frameCount % 60 == 0)
            {
                UpdateSpecializationEffects();
            }
        }

        private void UpdateSpecializationEffects()
        {
            CountSpecializedBuildings();
            ApplySpecializationEffects();
        }

        private void CountSpecializedBuildings()
        {
            if (specializedBuildings == null) return;

            beachBuildingCount = specializedBuildings.GetBuildingsByType(BuildingType.Beach).FindAll(b => b.isUnlocked).Count;
            mountainBuildingCount = specializedBuildings.GetBuildingsByType(BuildingType.Mountain).FindAll(b => b.isUnlocked).Count;
            casinoBuildingCount = specializedBuildings.GetBuildingsByType(BuildingType.Casino).FindAll(b => b.isUnlocked).Count;
            educationBuildingCount = specializedBuildings.GetBuildingsByType(BuildingType.Education).FindAll(b => b.isUnlocked).Count;
            entertainmentBuildingCount = specializedBuildings.GetBuildingsByType(BuildingType.Entertainment).FindAll(b => b.isUnlocked).Count;
        }

        private void ApplySpecializationEffects()
        {
            // Beach：旅游收入 + 满意度
            if (beachBuildingCount > 0)
            {
                int tourismIncome = beachBuildingCount * 500;
                simulation.Metrics.TaxIncome += tourismIncome;

                int happinessBonus = Mathf.Min(beachBuildingCount * 2, 10);
                simulation.Metrics.Happiness += happinessBonus;
            }

            // Mountain：高端地产 + 旅游
            if (mountainBuildingCount > 0)
            {
                int tourismIncome = mountainBuildingCount * 300;
                simulation.Metrics.TaxIncome += tourismIncome;
            }

            // Casino：高税收 + 高犯罪
            if (casinoBuildingCount > 0)
            {
                int casinoIncome = casinoBuildingCount * 1000;
                simulation.Metrics.TaxIncome += casinoIncome;

                // 增加犯罪风险（降低幸福度）
                int crimePenalty = casinoBuildingCount * 3;
                simulation.Metrics.Happiness -= crimePenalty;
            }

            // Education：生产力加成
            if (educationBuildingCount > 0)
            {
                // 教育建筑不产生直接税收，但提升工业效率
                // 通过CustomData标记已应用教育加成
            }

            // Entertainment：夜间活动 + 噪音
            if (entertainmentBuildingCount > 0)
            {
                int entertainmentIncome = entertainmentBuildingCount * 400;
                simulation.Metrics.TaxIncome += entertainmentIncome;

                // 噪音污染（轻微降低周边满意度）
                int noisePenalty = entertainmentBuildingCount;
                simulation.Metrics.Happiness -= noisePenalty;
            }
        }

        /// <summary>
        /// 获取专精加成描述
        /// </summary>
        public string GetSpecializationBonus(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Beach:
                    return $"+{beachBuildingCount * 500} 旅游收入\n+{Mathf.Min(beachBuildingCount * 2, 10)} 幸福度";

                case BuildingType.Mountain:
                    return $"+{mountainBuildingCount * 300} 旅游收入\n高端地产区";

                case BuildingType.Casino:
                    return $"+{casinoBuildingCount * 1000} 赌场收入\n-{casinoBuildingCount * 3} 幸福度（犯罪）";

                case BuildingType.Education:
                    return $"+{educationBuildingCount * 10}% 工业生产力\n解锁高级建筑";

                case BuildingType.Entertainment:
                    return $"+{entertainmentBuildingCount * 400} 娱乐收入\n-{entertainmentBuildingCount} 幸福度（噪音）";

                default:
                    return "";
            }
        }

        /// <summary>
        /// 获取专精建议
        /// </summary>
        public string GetSpecializationSuggestion()
        {
            int total = beachBuildingCount + mountainBuildingCount + casinoBuildingCount +
                       educationBuildingCount + entertainmentBuildingCount;

            if (total == 0)
                return "建议选择一条专精发展：旅游/赌场/教育/娱乐";

            // 找出主导专精
            int maxCount = Mathf.Max(beachBuildingCount, mountainBuildingCount, casinoBuildingCount,
                                     educationBuildingCount, entertainmentBuildingCount);

            if (maxCount < 3)
                return "建议集中发展单一专精以获得更强加成";

            if (beachBuildingCount == maxCount)
                return "旅游专精：继续建造海滩设施，注意海岸线利用率";

            if (casinoBuildingCount == maxCount)
            {
                int policeStations = 0; // TODO: 统计警察局数量
                if (casinoBuildingCount > policeStations * 2)
                    return "赌场专精：犯罪率过高！建议增加警察局";
                return "赌场专精：高收入高风险，需要充足警力";
            }

            if (educationBuildingCount == maxCount)
                return "教育专精：提升工业产出，适合生产流玩家";

            if (entertainmentBuildingCount == maxCount)
                return "娱乐专精：平衡收入与噪音，避免过度集中";

            return "多专精平衡发展";
        }

        /// <summary>
        /// 检查土地冲突
        /// </summary>
        public bool HasLandConflict()
        {
            int total = beachBuildingCount + mountainBuildingCount + casinoBuildingCount +
                       educationBuildingCount + entertainmentBuildingCount;

            // 专精建筑总数超过10时开始竞争土地
            return total > 10;
        }
    }

    /// <summary>
    /// 专精解锁条件
    /// </summary>
    public static class SpecializationUnlockConditions
    {
        public static bool CanUnlockBeach(CitySimulationCore simulation)
        {
            return simulation.Metrics.Population >= 5000 &&
                   simulation.Metrics.Happiness >= 60;
        }

        public static bool CanUnlockMountain(CitySimulationCore simulation)
        {
            return simulation.Metrics.Population >= 10000 &&
                   simulation.Metrics.Cash >= 50000;
        }

        public static bool CanUnlockCasino(CitySimulationCore simulation)
        {
            return simulation.Metrics.Population >= 15000 &&
                   simulation.Metrics.Happiness >= 70; // 需要高幸福度才能承受犯罪
        }

        public static bool CanUnlockEducation(CitySimulationCore simulation)
        {
            return simulation.Metrics.Population >= 8000;
        }

        public static bool CanUnlockEntertainment(CitySimulationCore simulation)
        {
            return simulation.Metrics.Population >= 6000;
        }

        public static string GetUnlockRequirement(BuildingType type, CitySimulationCore simulation)
        {
            switch (type)
            {
                case BuildingType.Beach:
                    if (simulation.Metrics.Population < 5000)
                        return $"需要人口: {simulation.Metrics.Population}/5000";
                    if (simulation.Metrics.Happiness < 60)
                        return $"需要幸福度: {simulation.Metrics.Happiness}/60";
                    return "已满足条件";

                case BuildingType.Mountain:
                    if (simulation.Metrics.Population < 10000)
                        return $"需要人口: {simulation.Metrics.Population}/10000";
                    if (simulation.Metrics.Cash < 50000)
                        return $"需要金币: {simulation.Metrics.Cash}/50000";
                    return "已满足条件";

                case BuildingType.Casino:
                    if (simulation.Metrics.Population < 15000)
                        return $"需要人口: {simulation.Metrics.Population}/15000";
                    if (simulation.Metrics.Happiness < 70)
                        return $"需要幸福度: {simulation.Metrics.Happiness}/70";
                    return "已满足条件";

                case BuildingType.Education:
                    if (simulation.Metrics.Population < 8000)
                        return $"需要人口: {simulation.Metrics.Population}/8000";
                    return "已满足条件";

                case BuildingType.Entertainment:
                    if (simulation.Metrics.Population < 6000)
                        return $"需要人口: {simulation.Metrics.Population}/6000";
                    return "已满足条件";

                default:
                    return "";
            }
        }
    }
}
