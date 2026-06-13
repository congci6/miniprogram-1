using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Specialized
{
    public enum RegionType
    {
        MainCity,
        Beach,
        GreenValley,
        Mountain,
        Desert
    }

    [System.Serializable]
    public class Region
    {
        public string id;
        public string name;
        public RegionType type;
        public int goldCost;
        public int materialCost;
        public int goldenKeyCost;
        public int requiredLevel;
        public bool isUnlocked;
        public List<string> prerequisiteRegions = new List<string>();
    }

    public class RegionalUnlockSystem : MonoBehaviour
    {
        public static RegionalUnlockSystem Instance { get; private set; }

        [SerializeField] private List<Region> regions = new List<Region>();
        private Dictionary<string, Region> regionDict = new Dictionary<string, Region>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            InitializeRegions();
        }

        private void InitializeRegions()
        {
            foreach (var region in regions)
            {
                regionDict[region.id] = region;
            }

            // Main city starts unlocked
            if (regionDict.ContainsKey("main_city"))
            {
                regionDict["main_city"].isUnlocked = true;
            }
        }

        public bool CanUnlockRegion(string regionId, int playerLevel, int gold, int materials, int goldenKeys)
        {
            if (!regionDict.TryGetValue(regionId, out var region)) return false;
            if (region.isUnlocked) return false;

            // Check prerequisites
            foreach (var prereqId in region.prerequisiteRegions)
            {
                if (!regionDict.TryGetValue(prereqId, out var prereq) || !prereq.isUnlocked)
                {
                    return false;
                }
            }

            return playerLevel >= region.requiredLevel &&
                   gold >= region.goldCost &&
                   materials >= region.materialCost &&
                   goldenKeys >= region.goldenKeyCost;
        }

        public bool UnlockRegion(string regionId)
        {
            if (!regionDict.TryGetValue(regionId, out var region)) return false;
            if (CurrencySystem.Instance == null || GoldenKeySystem.Instance == null) return false;

            var currency = CurrencySystem.Instance;
            if (!CanUnlockRegion(regionId, currency.Level, currency.Coins, currency.Materials, GoldenKeySystem.Instance.GetKeyCount())) return false;

            currency.SpendGold(region.goldCost);
            currency.SpendMaterials(region.materialCost);
            GoldenKeySystem.Instance.SpendKeys(region.goldenKeyCost);
            region.isUnlocked = true;

            return true;
        }

        public List<Region> GetUnlockedRegions()
        {
            return regions.FindAll(r => r.isUnlocked);
        }

        public Region GetRegion(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            regionDict.TryGetValue(id, out var region);
            return region;
        }

        public int GetUnlockedRegionCount()
        {
            return regions.FindAll(r => r.isUnlocked).Count;
        }
    }
}
