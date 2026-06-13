using PocketCity.Core;
using UnityEditor;
using UnityEngine;

namespace PocketCity.Editor
{
    public static class DefaultCityConfigFactory
    {
        private const string AssetPath = "Assets/Resources/CityConfig.asset";

        [MenuItem("Pocket City/Create Default City Config")]
        public static void CreateDefaultCityConfig()
        {
            EnsureFolder("Assets/Resources");

            var config = ScriptableObject.CreateInstance<CityConfig>();
            config.MapWidth = 64;
            config.MapHeight = 64;
            config.InitialCash = 12000;
            config.InitialHappiness = 62;
            config.RoadCostPerTile = 40;
            config.RoadCapacity = 120;
            config.RoadUpkeepPerTile = 1;
            config.ZoneCostPerTile = 6;
            config.DemolishRefundRate = 0.25f;
            config.MaxRoadSearchDistance = 5;
            config.SecondsPerSimulationDay = 3;
            config.DaysPerBudgetPeriod = 30;
            config.ResidentTaxPerPerson = 2;
            config.JobTaxPerWorker = 3;
            config.HappinessTarget = 68;
            config.LowServiceHappinessPenalty = 12;
            config.UtilityShortageHappinessPenalty = 18;
            config.CongestionHappinessPenalty = 10;

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "residential_pod",
                Name = "住宅舱",
                Category = BuildingCategory.Residential,
                Size = new GridSize(2, 2),
                Cost = 260,
                Upkeep = 4,
                Capacity = 48,
                PowerUse = 2,
                WaterUse = 2,
                TaxValue = 6,
                TrafficGeneration = 5,
                PreferredZone = ZoneType.Residential,
                ModelKey = "residential"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "apartment_block",
                Name = "公寓楼",
                Category = BuildingCategory.Residential,
                Size = new GridSize(2, 3),
                Cost = 720,
                Upkeep = 12,
                Capacity = 104,
                PowerUse = 5,
                WaterUse = 5,
                Noise = 1,
                TaxValue = 12,
                TrafficGeneration = 14,
                UnlockMinPopulation = 180,
                UnlockMinCityScore = 55,
                PreferredZone = ZoneType.Residential,
                ModelKey = "residential"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "market_corner",
                Name = "街角商铺",
                Category = BuildingCategory.Commercial,
                Size = new GridSize(2, 2),
                Cost = 420,
                Upkeep = 8,
                Jobs = 24,
                PowerUse = 4,
                WaterUse = 2,
                Pollution = 1,
                Noise = 2,
                TaxValue = 18,
                TrafficGeneration = 16,
                PreferredZone = ZoneType.Commercial,
                ModelKey = "commercial"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "office_studio",
                Name = "共享办公楼",
                Category = BuildingCategory.Commercial,
                Size = new GridSize(2, 3),
                Cost = 920,
                Upkeep = 16,
                Jobs = 46,
                PowerUse = 6,
                WaterUse = 2,
                Noise = 1,
                TaxValue = 34,
                TrafficGeneration = 14,
                UnlockMinPopulation = 260,
                UnlockMinCityScore = 60,
                PreferredZone = ZoneType.Office,
                ModelKey = "office"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "research_campus",
                Name = "\u7814\u53d1\u56ed\u533a",
                Category = BuildingCategory.Commercial,
                Size = new GridSize(3, 3),
                Cost = 2800,
                Upkeep = 46,
                Jobs = 34,
                PowerUse = 9,
                WaterUse = 3,
                Noise = 3,
                TaxValue = 26,
                ServiceRadius = 12,
                ServiceValue = 24,
                TrafficGeneration = 16,
                UnlockMinPopulation = 520,
                UnlockMinCityScore = 66,
                PreferredZone = ZoneType.Office,
                ModelKey = "innovation"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "mixed_use_block",
                Name = "混合街区",
                Category = BuildingCategory.Commercial,
                Size = new GridSize(2, 3),
                Cost = 840,
                Upkeep = 15,
                Capacity = 56,
                Jobs = 22,
                PowerUse = 5,
                WaterUse = 4,
                Noise = 2,
                TaxValue = 26,
                TrafficGeneration = 13,
                UnlockMinPopulation = 220,
                UnlockMinCityScore = 58,
                PreferredZone = ZoneType.MixedUse,
                ModelKey = "mixed_use"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "maker_yard",
                Name = "制造工坊",
                Category = BuildingCategory.Industrial,
                Size = new GridSize(3, 3),
                Cost = 760,
                Upkeep = 14,
                Jobs = 60,
                PowerUse = 8,
                WaterUse = 5,
                Pollution = 8,
                Noise = 6,
                TaxValue = 28,
                TrafficGeneration = 24,
                UnlockMinPopulation = 80,
                UnlockMinCityScore = 55,
                PreferredZone = ZoneType.Industrial,
                ModelKey = "industrial"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "resource_processor",
                Name = "\u8d44\u6e90\u52a0\u5de5\u56ed",
                Category = BuildingCategory.Industrial,
                Size = new GridSize(3, 3),
                Cost = 1680,
                Upkeep = 32,
                Jobs = 24,
                PowerUse = 6,
                WaterUse = 4,
                Pollution = 4,
                Noise = 5,
                TaxValue = 22,
                ServiceRadius = 9,
                ServiceValue = 22,
                TrafficGeneration = 18,
                UnlockMinPopulation = 260,
                UnlockMinCityScore = 60,
                PreferredZone = ZoneType.Industrial,
                ModelKey = "resource"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "pocket_park",
                Name = "口袋公园",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 540,
                Upkeep = 10,
                Jobs = 4,
                PowerUse = 1,
                WaterUse = 1,
                ServiceRadius = 8,
                ServiceValue = 10,
                TrafficGeneration = 4,
                UnlockMinPopulation = 40,
                UnlockMinCityScore = 55,
                PreferredZone = ZoneType.Civic,
                ModelKey = "park"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "city_plaza",
                Name = "城市广场",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 760,
                Upkeep = 14,
                Jobs = 6,
                PowerUse = 2,
                WaterUse = 1,
                ServiceRadius = 9,
                ServiceValue = 14,
                TrafficGeneration = 10,
                UnlockMinPopulation = 120,
                UnlockMinCityScore = 56,
                PreferredZone = ZoneType.Civic,
                ModelKey = "plaza"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "convention_center",
                Name = "\u4f1a\u5c55\u4e2d\u5fc3",
                Category = BuildingCategory.Service,
                Size = new GridSize(4, 3),
                Cost = 3200,
                Upkeep = 58,
                Jobs = 38,
                PowerUse = 8,
                WaterUse = 5,
                Noise = 8,
                TaxValue = 20,
                ServiceRadius = 14,
                ServiceValue = 30,
                TrafficGeneration = 26,
                UnlockMinPopulation = 620,
                UnlockMinCityScore = 68,
                PreferredZone = ZoneType.Civic,
                ModelKey = "landmark"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "city_hall",
                Name = "\u5e02\u653f\u5385",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 3),
                Cost = 2400,
                Upkeep = 52,
                Jobs = 32,
                PowerUse = 6,
                WaterUse = 4,
                Noise = 2,
                ServiceRadius = 14,
                ServiceValue = 24,
                TrafficGeneration = 14,
                UnlockMinPopulation = 300,
                UnlockMinCityScore = 62,
                PreferredZone = ZoneType.Civic,
                ModelKey = "administration"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "health_post",
                Name = "社区诊所",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 820,
                Upkeep = 18,
                Jobs = 12,
                PowerUse = 3,
                WaterUse = 3,
                ServiceRadius = 10,
                ServiceValue = 12,
                TrafficGeneration = 8,
                UnlockMinPopulation = 140,
                UnlockMinCityScore = 58,
                PreferredZone = ZoneType.Civic,
                ModelKey = "clinic"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "district_hospital",
                Name = "\u533a\u57df\u533b\u9662",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 3),
                Cost = 2100,
                Upkeep = 46,
                Jobs = 36,
                PowerUse = 8,
                WaterUse = 6,
                Noise = 3,
                ServiceRadius = 15,
                ServiceValue = 26,
                TrafficGeneration = 16,
                UnlockMinPopulation = 420,
                UnlockMinCityScore = 66,
                PreferredZone = ZoneType.Civic,
                ModelKey = "clinic"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "memorial_garden",
                Name = "\u751f\u547d\u7eaa\u5ff5\u82b1\u56ed",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 1280,
                Upkeep = 26,
                Jobs = 10,
                PowerUse = 2,
                WaterUse = 2,
                Noise = 1,
                ServiceRadius = 12,
                ServiceValue = 20,
                TrafficGeneration = 7,
                UnlockMinPopulation = 300,
                UnlockMinCityScore = 60,
                PreferredZone = ZoneType.Civic,
                ModelKey = "deathcare"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "emergency_shelter",
                Name = "\u5e94\u6025\u907f\u96be\u4e2d\u5fc3",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 1750,
                Upkeep = 36,
                Jobs = 18,
                PowerUse = 4,
                WaterUse = 3,
                Noise = 2,
                ServiceRadius = 13,
                ServiceValue = 18,
                TrafficGeneration = 10,
                UnlockMinPopulation = 360,
                UnlockMinCityScore = 62,
                PreferredZone = ZoneType.Civic,
                ModelKey = "shelter"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "bus_hub",
                Name = "街区公交站",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 720,
                Upkeep = 16,
                Jobs = 8,
                PowerUse = 2,
                ServiceRadius = 9,
                ServiceValue = 8,
                TrafficGeneration = 2,
                UnlockMinPopulation = 180,
                UnlockMinCityScore = 60,
                PreferredZone = ZoneType.Civic,
                ModelKey = "transit"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "metro_station",
                Name = "\u8f68\u9053\u4ea4\u901a\u7ad9",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 3),
                Cost = 2200,
                Upkeep = 42,
                Jobs = 24,
                PowerUse = 8,
                WaterUse = 3,
                Noise = 8,
                ServiceRadius = 14,
                ServiceValue = 20,
                TrafficGeneration = 4,
                UnlockMinPopulation = 520,
                UnlockMinCityScore = 68,
                PreferredZone = ZoneType.Civic,
                ModelKey = "transit"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "intercity_terminal",
                Name = "\u57ce\u9645\u67a2\u7ebd",
                Category = BuildingCategory.Service,
                Size = new GridSize(4, 3),
                Cost = 3600,
                Upkeep = 68,
                Jobs = 42,
                PowerUse = 10,
                WaterUse = 4,
                Noise = 9,
                TaxValue = 18,
                ServiceRadius = 16,
                ServiceValue = 28,
                TrafficGeneration = 10,
                UnlockMinPopulation = 680,
                UnlockMinCityScore = 70,
                PreferredZone = ZoneType.Civic,
                ModelKey = "intercity"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "cargo_depot",
                Name = "货运站",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 1180,
                Upkeep = 24,
                Jobs = 18,
                PowerUse = 4,
                WaterUse = 2,
                Pollution = 2,
                Noise = 7,
                TaxValue = 10,
                ServiceRadius = 10,
                ServiceValue = 4,
                TrafficGeneration = 16,
                UnlockMinPopulation = 240,
                UnlockMinCityScore = 60,
                PreferredZone = ZoneType.Civic,
                ModelKey = "logistics"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "distribution_center",
                Name = "\u914d\u9001\u4e2d\u5fc3",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 1850,
                Upkeep = 34,
                Jobs = 24,
                PowerUse = 5,
                WaterUse = 2,
                Pollution = 2,
                Noise = 8,
                TaxValue = 13,
                ServiceRadius = 12,
                ServiceValue = 10,
                TrafficGeneration = 14,
                UnlockMinPopulation = 420,
                UnlockMinCityScore = 64,
                PreferredZone = ZoneType.Industrial,
                ModelKey = "warehouse"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "freight_rail_terminal",
                Name = "\u8d27\u8fd0\u94c1\u8def\u7ad9",
                Category = BuildingCategory.Service,
                Size = new GridSize(4, 3),
                Cost = 4200,
                Upkeep = 72,
                Jobs = 46,
                PowerUse = 12,
                WaterUse = 3,
                Pollution = 3,
                Noise = 10,
                TaxValue = 24,
                ServiceRadius = 16,
                ServiceValue = 18,
                TrafficGeneration = 12,
                UnlockMinPopulation = 760,
                UnlockMinCityScore = 72,
                PreferredZone = ZoneType.Industrial,
                ModelKey = "freight_rail"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "primary_school",
                Name = "社区学校",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 1100,
                Upkeep = 22,
                Jobs = 18,
                PowerUse = 4,
                WaterUse = 3,
                ServiceRadius = 11,
                ServiceValue = 10,
                TrafficGeneration = 10,
                UnlockMinPopulation = 260,
                UnlockMinCityScore = 62,
                PreferredZone = ZoneType.Civic,
                ModelKey = "school"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "community_college",
                Name = "\u793e\u533a\u5b66\u9662",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 3),
                Cost = 1680,
                Upkeep = 34,
                Jobs = 26,
                PowerUse = 6,
                WaterUse = 4,
                ServiceRadius = 10,
                ServiceValue = 13,
                TaxValue = 10,
                TrafficGeneration = 14,
                UnlockMinPopulation = 380,
                UnlockMinCityScore = 66,
                PreferredZone = ZoneType.Civic,
                ModelKey = "advanced_education"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "fire_station",
                Name = "社区消防站",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 960,
                Upkeep = 20,
                Jobs = 16,
                PowerUse = 3,
                WaterUse = 4,
                ServiceRadius = 10,
                ServiceValue = 8,
                TrafficGeneration = 9,
                UnlockMinPopulation = 200,
                UnlockMinCityScore = 60,
                PreferredZone = ZoneType.Civic,
                ModelKey = "safety"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "police_kiosk",
                Name = "社区警务站",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 860,
                Upkeep = 18,
                Jobs = 12,
                PowerUse = 3,
                WaterUse = 2,
                ServiceRadius = 9,
                ServiceValue = 7,
                TrafficGeneration = 7,
                UnlockMinPopulation = 220,
                UnlockMinCityScore = 58,
                PreferredZone = ZoneType.Civic,
                ModelKey = "security"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "police_precinct",
                Name = "警务分局",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 1850,
                Upkeep = 36,
                Jobs = 28,
                PowerUse = 5,
                WaterUse = 3,
                ServiceRadius = 14,
                ServiceValue = 13,
                TrafficGeneration = 11,
                UnlockMinPopulation = 560,
                UnlockMinCityScore = 66,
                PreferredZone = ZoneType.Civic,
                ModelKey = "security"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "telecom_hub",
                Name = "通信枢纽",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 1040,
                Upkeep = 22,
                Jobs = 10,
                PowerUse = 5,
                WaterUse = 1,
                ServiceRadius = 11,
                ServiceValue = 11,
                TaxValue = 8,
                TrafficGeneration = 6,
                UnlockMinPopulation = 180,
                UnlockMinCityScore = 58,
                PreferredZone = ZoneType.Civic,
                ModelKey = "communications"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "post_office",
                Name = "\u90ae\u653f\u670d\u52a1",
                Category = BuildingCategory.Service,
                Size = new GridSize(2, 2),
                Cost = 880,
                Upkeep = 18,
                Jobs = 12,
                PowerUse = 3,
                WaterUse = 1,
                ServiceRadius = 10,
                ServiceValue = 10,
                TaxValue = 6,
                TrafficGeneration = 8,
                UnlockMinPopulation = 160,
                UnlockMinCityScore = 58,
                PreferredZone = ZoneType.Civic,
                ModelKey = "mail"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "road_maintenance_depot",
                Name = "\u9053\u8def\u517b\u62a4\u7ad9",
                Category = BuildingCategory.Service,
                Size = new GridSize(3, 2),
                Cost = 940,
                Upkeep = 18,
                Jobs = 12,
                PowerUse = 3,
                WaterUse = 1,
                ServiceRadius = 10,
                ServiceValue = 9,
                TrafficGeneration = 6,
                UnlockMinPopulation = 160,
                UnlockMinCityScore = 56,
                PreferredZone = ZoneType.Civic,
                ModelKey = "road_maintenance"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "parking_garage",
                Name = "\u90bb\u91cc\u505c\u8f66\u697c",
                Category = BuildingCategory.Utility,
                Size = new GridSize(2, 2),
                Cost = 760,
                Upkeep = 14,
                Jobs = 6,
                PowerUse = 2,
                WaterUse = 1,
                Noise = 3,
                TaxValue = 5,
                TrafficGeneration = 5,
                ServiceRadius = 8,
                ServiceValue = 10,
                UnlockMinPopulation = 140,
                UnlockMinCityScore = 54,
                PreferredZone = ZoneType.Utility,
                ModelKey = "parking"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "rain_garden",
                Name = "\u96e8\u6c34\u82b1\u56ed",
                Category = BuildingCategory.Utility,
                Size = new GridSize(2, 2),
                Cost = 620,
                Upkeep = 10,
                Jobs = 4,
                PowerUse = 1,
                WaterUse = 1,
                ServiceRadius = 8,
                ServiceValue = 9,
                TaxValue = 3,
                TrafficGeneration = 3,
                UnlockMinPopulation = 110,
                UnlockMinCityScore = 52,
                PreferredZone = ZoneType.Utility,
                ModelKey = "stormwater"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "micro_power",
                Name = "微型电站",
                Category = BuildingCategory.Utility,
                Size = new GridSize(3, 2),
                Cost = 900,
                Upkeep = 18,
                PowerOutput = 72,
                WaterUse = 1,
                Pollution = 5,
                Noise = 5,
                TaxValue = 4,
                TrafficGeneration = 6,
                ServiceRadius = 10,
                PreferredZone = ZoneType.Utility,
                ModelKey = "power"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "solar_farm",
                Name = "\u592a\u9633\u80fd\u9635\u5217",
                Category = BuildingCategory.Utility,
                Size = new GridSize(4, 3),
                Cost = 1600,
                Upkeep = 20,
                Jobs = 8,
                PowerOutput = 112,
                Noise = 2,
                TaxValue = 6,
                TrafficGeneration = 3,
                UnlockMinPopulation = 320,
                UnlockMinCityScore = 62,
                PreferredZone = ZoneType.Utility,
                ModelKey = "solar"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "water_tower",
                Name = "净水塔",
                Category = BuildingCategory.Utility,
                Size = new GridSize(2, 2),
                Cost = 680,
                Upkeep = 12,
                PowerUse = 2,
                WaterOutput = 80,
                ServiceRadius = 10,
                ServiceValue = 2,
                TrafficGeneration = 4,
                PreferredZone = ZoneType.Utility,
                ModelKey = "water"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "water_reclaimer",
                Name = "\u6c61\u6c34\u5904\u7406\u7ad9",
                Category = BuildingCategory.Utility,
                Size = new GridSize(3, 2),
                Cost = 1120,
                Upkeep = 22,
                Jobs = 12,
                PowerUse = 6,
                WaterUse = 1,
                Pollution = 2,
                Noise = 4,
                TaxValue = 8,
                TrafficGeneration = 8,
                ServiceRadius = 9,
                ServiceValue = 12,
                UnlockMinPopulation = 180,
                UnlockMinCityScore = 56,
                PreferredZone = ZoneType.Utility,
                ModelKey = "sewage"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "waste_to_energy_plant",
                Name = "\u5783\u573e\u53d1\u7535\u5382",
                Category = BuildingCategory.Utility,
                Size = new GridSize(4, 3),
                Cost = 2600,
                Upkeep = 46,
                Jobs = 28,
                PowerOutput = 96,
                WaterUse = 3,
                Pollution = 7,
                Noise = 7,
                TaxValue = 14,
                TrafficGeneration = 14,
                ServiceRadius = 11,
                UnlockMinPopulation = 520,
                UnlockMinCityScore = 64,
                PreferredZone = ZoneType.Utility,
                ModelKey = "waste_to_energy"
            });

            config.Buildings.Add(new BuildingDefinition
            {
                Id = "recycling_yard",
                Name = "回收处理站",
                Category = BuildingCategory.Utility,
                Size = new GridSize(3, 2),
                Cost = 980,
                Upkeep = 20,
                Jobs = 18,
                PowerUse = 5,
                WaterUse = 2,
                Pollution = 3,
                Noise = 4,
                TaxValue = 12,
                TrafficGeneration = 12,
                ServiceRadius = 8,
                UnlockMinPopulation = 220,
                UnlockMinCityScore = 62,
                PreferredZone = ZoneType.Utility,
                ModelKey = "recycling"
            });

            var existing = AssetDatabase.LoadAssetAtPath<CityConfig>(AssetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(config, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(config, AssetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created Pocket City default config at " + AssetPath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            // Split path and create hierarchy
            var parts = path.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets")
            {
                Debug.LogError($"Invalid asset path: {path}. Must start with 'Assets/'");
                return;
            }

            var currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
