using PocketCity.Core;
using PocketCity.Simulation;
using UnityEngine;

namespace PocketCity.Buildings
{
    /// <summary>
    /// 建筑特性系统 - 为不同建筑类型添加独特功能
    /// </summary>
    public class BuildingTraitSystem : MonoBehaviour
    {
        [SerializeField] private CitySimulationCore simulation;

        private void Start()
        {
            // 暂时禁用事件订阅，因为CitySimulationCore还未实现OnBuildingPlaced事件
            // 可以后续添加
        }

        private void OnDestroy()
        {
            // 暂时禁用
        }

        /// <summary>
        /// 手动应用建筑特性（供外部调用）
        /// </summary>
        public void ApplyTraitToBuilding(PlacedBuilding building)
        {
            if (building == null || simulation == null) return;

            // 从配置中查找建筑定义
            var definition = simulation.Config?.GetBuilding(building.ConfigId);
            if (definition != null)
            {
                ApplyBuildingTrait(building, definition);
            }
        }

        /// <summary>
        /// 根据建筑类型应用特殊效果
        /// </summary>
        public void ApplyBuildingTrait(PlacedBuilding building, BuildingDefinition definition)
        {
            if (building == null || definition == null) return;

            // 根据建筑ID应用特性
            switch (definition.Id)
            {
                // === 能源建筑 ===
                case "coal_power":
                    ApplyPowerPlantTrait(building, 500, -5); // 500供电，-5幸福度（污染）
                    break;
                case "wind_turbine":
                    ApplyPowerPlantTrait(building, 200, 2); // 200供电，+2幸福度（清洁）
                    break;
                case "solar_farm":
                    ApplyPowerPlantTrait(building, 300, 3);
                    break;

                // === 医疗建筑 ===
                case "clinic":
                    ApplyHealthcareTrait(building, 30, 2); // 30%覆盖，-2疾病风险
                    break;
                case "hospital":
                    ApplyHealthcareTrait(building, 60, 5);
                    break;

                // === 教育建筑 ===
                case "school":
                    ApplyEducationTrait(building, 40, 1); // 40%覆盖，+1生产力
                    break;
                case "university":
                    ApplyEducationTrait(building, 80, 3);
                    break;

                // === 安全建筑 ===
                case "police_station":
                    ApplySafetyTrait(building, 25, 3); // 25格范围，-3犯罪
                    break;
                case "fire_station":
                    ApplyFireProtectionTrait(building, 20, 50); // 20格范围，50%减少火灾损失
                    break;

                // === 公园娱乐 ===
                case "park":
                    ApplyParkTrait(building, 10, 5); // 10格范围，+5幸福度
                    break;
                case "stadium":
                    ApplyParkTrait(building, 30, 10);
                    break;

                // === 交通建筑 ===
                case "bus_station":
                    ApplyTransitTrait(building, 15, 20); // 15格范围，-20%拥堵
                    break;
                case "subway_station":
                    ApplyTransitTrait(building, 25, 40);
                    break;
            }
        }

        private void ApplyPowerPlantTrait(PlacedBuilding building, int powerOutput, int happinessModifier)
        {
            building.CustomData["PowerOutput"] = powerOutput;
            building.CustomData["HappinessModifier"] = happinessModifier;
        }

        private void ApplyHealthcareTrait(PlacedBuilding building, int coverage, int diseaseReduction)
        {
            building.CustomData["HealthCoverage"] = coverage;
            building.CustomData["DiseaseReduction"] = diseaseReduction;
        }

        private void ApplyEducationTrait(PlacedBuilding building, int coverage, int productivityBonus)
        {
            building.CustomData["EducationCoverage"] = coverage;
            building.CustomData["ProductivityBonus"] = productivityBonus;
        }

        private void ApplySafetyTrait(PlacedBuilding building, int range, int crimeReduction)
        {
            building.CustomData["SafetyRange"] = range;
            building.CustomData["CrimeReduction"] = crimeReduction;
        }

        private void ApplyFireProtectionTrait(PlacedBuilding building, int range, int damageReduction)
        {
            building.CustomData["ProtectionRange"] = range;
            building.CustomData["FireDamageReduction"] = damageReduction;
        }

        private void ApplyParkTrait(PlacedBuilding building, int range, int happinessBonus)
        {
            building.CustomData["InfluenceRange"] = range;
            building.CustomData["HappinessBonus"] = happinessBonus;
        }

        private void ApplyTransitTrait(PlacedBuilding building, int range, int congestionReduction)
        {
            building.CustomData["TransitRange"] = range;
            building.CustomData["CongestionReduction"] = congestionReduction;
        }

        /// <summary>
        /// 计算建筑周围的影响效果
        /// </summary>
        public int CalculateAreaEffect(GridPos center, string effectType, int range)
        {
            int totalEffect = 0;

            for (int dy = -range; dy <= range; dy++)
            {
                for (int dx = -range; dx <= range; dx++)
                {
                    var pos = new GridPos(center.X + dx, center.Y + dy);
                    if (!simulation.Grid.IsInBounds(pos)) continue;

                    int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (distance > range) continue;

                    var buildingId = simulation.Grid.FindBuildingIdAt(pos);
                    if (string.IsNullOrEmpty(buildingId)) continue;

                    var building = simulation.FindPlacedBuilding(buildingId);
                    if (building == null || !building.CustomData.ContainsKey(effectType)) continue;

                    int effect = (int)building.CustomData[effectType];
                    float distanceFactor = 1f - ((float)distance / range);
                    totalEffect += Mathf.RoundToInt(effect * distanceFactor);
                }
            }

            return totalEffect;
        }
    }
}
