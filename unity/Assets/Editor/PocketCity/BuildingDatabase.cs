using UnityEngine;
using PocketCity.Core;

namespace PocketCity.Editor
{
    [System.Serializable]
    public class BuildingConfigData
    {
        public string id;
        public string name;
        public int cost;
        public int upkeep;
        public GridSize size;
        public BuildingCategory category;
        public int capacity;
        public int powerConsumption;
        public int powerOutput;
        public int waterConsumption;
        public int waterOutput;
        public int pollution;
        public int noise;
        public bool requiresRoad;
        public bool requiresPower;
        public bool requiresWater;
        public int utilityReliability;
    }

    [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Pocket City/Building Database")]
    public class BuildingDatabase : ScriptableObject
    {
        public BuildingConfigData[] buildings = new BuildingConfigData[0];

        public void Validate()
        {
            for (int i = 0; i < buildings.Length; i++)
            {
                var b = buildings[i];
                if (string.IsNullOrEmpty(b.id))
                {
                    Debug.LogError($"Building at index {i} has empty ID");
                }
                if (b.cost < 0)
                {
                    Debug.LogWarning($"Building {b.id} has negative cost: {b.cost}");
                }
                if (b.upkeep < 0)
                {
                    Debug.LogWarning($"Building {b.id} has negative upkeep: {b.upkeep}");
                }
                if (b.capacity < 0)
                {
                    Debug.LogWarning($"Building {b.id} has negative capacity: {b.capacity}");
                }
                if (b.requiresPower && b.powerOutput == 0 && b.powerConsumption == 0)
                {
                    Debug.LogWarning($"Building {b.id} requires power but has no power consumption set");
                }
                if (b.requiresWater && b.waterOutput == 0 && b.waterConsumption == 0)
                {
                    Debug.LogWarning($"Building {b.id} requires water but has no water consumption set");
                }
                if (b.category == BuildingCategory.Utility && b.utilityReliability == 0)
                {
                    Debug.LogWarning($"Utility building {b.id} has no reliability value set");
                }
            }
        }

        [ContextMenu("Validate All Buildings")]
        public void ValidateInEditor()
        {
            Validate();
            Debug.Log($"Validation complete for {buildings.Length} buildings");
        }
    }
}
