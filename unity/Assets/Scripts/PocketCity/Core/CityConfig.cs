using System.Collections.Generic;
using PocketCity.Core;
using UnityEngine;

namespace PocketCity.Core
{
    [CreateAssetMenu(menuName = "Pocket City/City Config", fileName = "CityConfig")]
    public sealed class CityConfig : ScriptableObject
    {
        [Header("Map")]
        public int MapWidth = 64;
        public int MapHeight = 64;

        [Header("Economy")]
        public int InitialCash = 15000;
        public int InitialHappiness = 65;
        public int RoadCostPerTile = 35;
        public int RoadCapacity = 80;
        public int ArterialRoadCapacity = 200;
        public int ArterialRoadUpgradeCost = 60;
        public int RoadUpkeepPerTile = 1;
        public int ArterialRoadUpkeepPerTile = 2;
        public int ZoneCostPerTile = 12;
        public float DemolishRefundRate = 0.4f;
        public int MaxRoadSearchDistance = 5;
        public int SecondsPerSimulationDay = 2;
        public int DaysPerBudgetPeriod = 20;
        public int ResidentTaxPerPerson = 3;
        public int JobTaxPerWorker = 2;
        public int HappinessTarget = 70;
        public int LowServiceHappinessPenalty = 8;
        public int UtilityShortageHappinessPenalty = 12;
        public int CongestionHappinessPenalty = 8;

        [Header("Buildings")]
        public List<BuildingDefinition> Buildings = new List<BuildingDefinition>();

        [Header("Production")]
        public int FactoryMaxSlots = 2;

        public BuildingDefinition GetBuilding(string id)
        {
            for (var i = 0; i < Buildings.Count; i += 1)
            {
                if (Buildings[i].Id == id)
                {
                    return Buildings[i];
                }
            }

            Debug.LogError("Unknown building id: " + id);
            return null;
        }
    }
}
