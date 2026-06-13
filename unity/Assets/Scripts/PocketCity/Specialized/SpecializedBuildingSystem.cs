using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Specialized
{
    public enum BuildingType
    {
        Beach,
        Mountain,
        Casino,
        Education,
        Entertainment
    }

    [System.Serializable]
    public class SpecializedBuilding
    {
        public string id;
        public string name;
        public BuildingType type;
        public int requiredLevel;
        public int requiredPopulation;
        public int goldCost;
        public int goldenKeyCost;
        public bool isUnlocked;
    }

    public class SpecializedBuildingSystem : MonoBehaviour
    {
        public static SpecializedBuildingSystem Instance { get; private set; }

        [SerializeField] private List<SpecializedBuilding> buildings = new List<SpecializedBuilding>();
        private Dictionary<string, SpecializedBuilding> buildingDict = new Dictionary<string, SpecializedBuilding>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            InitializeBuildings();
        }

        private void InitializeBuildings()
        {
            foreach (var building in buildings)
            {
                buildingDict[building.id] = building;
                building.isUnlocked = PlayerPrefs.GetInt("SpecializedBuilding_" + building.id, 0) == 1;
            }
        }

        public bool CanUnlock(string buildingId, int playerLevel, int population, int gold, int goldenKeys)
        {
            if (!buildingDict.TryGetValue(buildingId, out var building)) return false;
            if (building.isUnlocked) return false;

            return playerLevel >= building.requiredLevel &&
                   population >= building.requiredPopulation &&
                   gold >= building.goldCost &&
                   goldenKeys >= building.goldenKeyCost;
        }

        public bool UnlockBuilding(string buildingId)
        {
            if (!buildingDict.TryGetValue(buildingId, out var building)) return false;
            if (CurrencySystem.Instance == null || GoldenKeySystem.Instance == null) return false;

            var currency = CurrencySystem.Instance;
            if (!CanUnlock(buildingId, currency.Level, currency.Population, currency.Coins, GoldenKeySystem.Instance.GetKeyCount())) return false;

            currency.SpendGold(building.goldCost);
            GoldenKeySystem.Instance.SpendKeys(building.goldenKeyCost);
            building.isUnlocked = true;

            OnBuildingUnlocked?.Invoke(buildingId);
            PlayerPrefs.SetInt("SpecializedBuilding_" + buildingId, 1);
            PlayerPrefs.Save();

            return true;
        }

        public event System.Action<string> OnBuildingUnlocked;

        public List<SpecializedBuilding> GetBuildingsByType(BuildingType type)
        {
            return buildings.FindAll(b => b.type == type);
        }

        public SpecializedBuilding GetBuilding(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            buildingDict.TryGetValue(id, out var building);
            return building;
        }
    }
}
