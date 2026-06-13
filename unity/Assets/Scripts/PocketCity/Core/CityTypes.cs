using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Core
{
    public enum BuildingCategory
    {
        Residential,
        Commercial,
        Industrial,
        Utility,
        Service,
        Decoration,  // 装饰性建筑
        Park,        // 公园
        Office,      // 办公
        MixedUse     // 混合用途
    }

    public enum OverlayMode
    {
        Normal,
        Traffic,
        Pollution,
        Zoning,
        Services,
        Transit,
        LandValue,
        Waste,
        Logistics,
        Utilities,
        Communications,
        RoadSafety,
        Parking,
        Stormwater
    }

    public enum ZoneType
    {
        None,
        Residential,
        Commercial,
        Industrial,
        Civic,
        Utility,
        Office,
        MixedUse
    }

    public enum TerrainType
    {
        Plain,
        Water,
        Hill
    }

    public enum CityPolicy
    {
        GreenCode,
        TransitPriority,
        GrowthGrants,
        AffordableHousing,
        TrafficSafetyCampaign,
        CompleteStreets,
        SignalOptimization,
        CongestionPricing,
        ParkingFees
    }

    public enum CityTaxLevel
    {
        Low,
        Normal,
        High
    }

    public enum CityServiceBudgetLevel
    {
        Lean,
        Standard,
        Boosted
    }

    public enum RoadTier
    {
        Local,
        Arterial
    }

    public enum BuildingRotation { None = 0, North, East, South, West }

    public enum RoadType { None, Local, Road, Highway, Boulevard, Avenue }

    [Serializable]
    public struct GridPos
    {
        public int X;
        public int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, 0, Y);
        }

        public Vector3 ToVector3(float yOffset)
        {
            return new Vector3(X, yOffset, Y);
        }

        public static int ManhattanDistance(GridPos a, GridPos b)
        {
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }
    }

    [Serializable]
    public struct GridSize
    {
        public int W;
        public int H;

        public GridSize(int w, int h)
        {
            W = w;
            H = h;
        }
    }

    [Serializable]
    public sealed class BuildingDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public BuildingCategory Category;
        public GridSize Size = new GridSize(1, 1);
        public int Cost;
        public int Upkeep;
        public int Capacity;
        public int Jobs;
        public int PowerUse;
        public int PowerOutput;
        public int WaterUse;
        public int WaterOutput;
        public int Pollution;
        public int Noise;
        public int TaxValue;
        public int TrafficGeneration;
        public int ServiceValue;
        public int ServiceRadius;
        public int UnlockMinPopulation;
        public int UnlockMinCityScore;
        public int RequiredPlayerLevel; // 等级门控
        public ZoneType PreferredZone = ZoneType.None;
        public string ModelKey = string.Empty;
    }

    [Serializable]
    public sealed class PlacedBuilding
    {
        public string Id = string.Empty;
        public string ConfigId = string.Empty;
        public GridPos Pos;
        public GridSize Size;
        public string ConnectedRoadId = string.Empty;
        public int AgeDays;
        public int Level = 1;
        public bool AutoDeveloped;
        public float Efficiency = 1f;

        // 建筑原点（FootprintOrigin）- 兼容性属性
        public GridPos FootprintOrigin => Pos;
        public GridPos BuildingOrigin => Pos;

        // CustomData for BuildingTraitSystem and extensions
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
    }

    [Serializable]
    public sealed class RoadNode
    {
        public string Id = string.Empty;
        public GridPos Pos;
        public int Load;
        public int Capacity;
        public int NeighborCount;
        public RoadTier Tier = RoadTier.Local;
    }

    [Serializable]
    public sealed class TileData
    {
        public TerrainType Terrain;
        public ZoneType Zone;
        public string RoadId = string.Empty;
        public string BuildingId = string.Empty;
        public int Traffic;
        public int Pollution;
        public int Noise;
        public int LandValue;
        public int TransitAccess;
        public int LogisticsAccess;
        public int ParkAccess;
        public int HealthAccess;
        public int DeathcareAccess;
        public int EducationAccess;
        public int WasteAccess;
        public int SafetyAccess;
        public int FireProtectionAccess;
        public int SecurityAccess;
        public int CommunicationAccess;
        public int MailAccess;
        public int RoadMaintenanceAccess;
        public int ParkingAccess;
        public int StormwaterAccess;
    }

    [Serializable]
    public sealed class DemandMetrics
    {
        public int Residential;
        public int Commercial;
        public int Industrial;
        public int Office;
        public int MixedUse;
        public int Service;
        public int Utility;
    }

    [Serializable]
    public sealed class CityObjective
    {
        public string Title = string.Empty;
        public string Hint = string.Empty;
        public int Progress;
        public int Required;
        public bool Done;
    }

    [Serializable]
    public sealed class CityMilestone
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string Hint = string.Empty;
        public int Progress;
        public int Required;
        public bool Done;
    }

    [Serializable]
    public sealed class CityMetrics
    {
        public int Day;
        public int Population;
        public int Cash;
        public int Happiness;
        public int HousingCapacity;
        public int Jobs;
        public int PowerSupply;
        public int PowerDemand;
        public int WaterSupply;
        public int WaterDemand;
        public int UtilityLoad;
        public int UtilityCapacity;
        public int UtilityUtilization;
        public int UtilityReliability;
        public int WastewaterLoad;
        public int WastewaterCapacity;
        public int WastewaterUtilization;
        public int WastewaterReliability;
        public int StormwaterLoad;
        public int StormwaterCapacity;
        public int StormwaterUtilization;
        public int StormwaterResilience;
        public int FloodRisk;
        public int Congestion;
        public int Pollution;
        public int Noise;
        public int ServiceCoverage;
        public int ServiceLoad;
        public int ServiceCapacity;
        public int ServiceUtilization;
        public int ServiceEquity;
        public int UnderservedResidents;
        public int ServiceGapPressure;
        public string ServiceGapFocus = string.Empty;
        public int ServiceGapAdvisorScore;
        public string ServiceGapAdvisorFocus = string.Empty;
        public string ServiceGapAdvisorDriver = string.Empty;
        public string ServiceGapAdvisorAction = string.Empty;
        public int GrowthBottleneckScore;
        public string GrowthBottleneckFocus = string.Empty;
        public string GrowthBottleneckDriver = string.Empty;
        public string GrowthBottleneckAction = string.Empty;
        public int MaintenanceCondition;
        public int ParkCoverage;
        public int HealthCoverage;
        public int HealthLoad;
        public int HealthCapacity;
        public int HealthUtilization;
        public int MedicalResponse;
        public int PatientBacklog;
        public int DeathcareCoverage;
        public int DeathcareLoad;
        public int DeathcareCapacity;
        public int DeathcareUtilization;
        public int MortalityPressure;
        public int EducationCoverage;
        public int AdvancedEducationCoverage;
        public int EducationLoad;
        public int EducationCapacity;
        public int EducationUtilization;
        public int StudentBacklog;
        public int LearningPipeline;
        public int SafetyCoverage;
        public int FireProtection;
        public int FireLoad;
        public int FireCapacity;
        public int FireUtilization;
        public int FireRisk;
        public int FireResponse;
        public int SecurityCoverage;
        public int SecurityLoad;
        public int SecurityCapacity;
        public int SecurityUtilization;
        public int PoliceResponse;
        public int CaseBacklog;
        public int TransitCoverage;
        public int TransitLoad;
        public int TransitCapacity;
        public int TransitUtilization;
        public int TransitReliability;
        public int TransitWaitPressure;
        public int LogisticsCoverage;
        public int LogisticsLoad;
        public int LogisticsCapacity;
        public int LogisticsUtilization;
        public int WasteCoverage;
        public int WasteLoad;
        public int WasteCapacity;
        public int WasteUtilization;
        public int WasteReliability;
        public int CommunicationCoverage;
        public int CommunicationLoad;
        public int CommunicationCapacity;
        public int CommunicationUtilization;
        public int BusinessEfficiency;
        public int MailCoverage;
        public int MailLoad;
        public int MailCapacity;
        public int MailUtilization;
        public int MailReliability;
        public int RoadMaintenanceCoverage;
        public int AccidentRisk;
        public int RoadSafety;
        public int EmergencyResponse;
        public int DisasterPreparedness;
        public int DisasterRisk;
        public int InfrastructureResilienceScore;
        public string InfrastructureResilienceFocus = string.Empty;
        public string InfrastructureResilienceDriver = string.Empty;
        public string InfrastructureResilienceAction = string.Empty;
        public int CrimePressure;
        public int Attractiveness;
        public int Visitors;
        public int TourismIncome;
        public int RegionalConnectivity;
        public int GoodsSupply;
        public int LocalGoodsSupply;
        public int FreightImportSupply;
        public int GoodsStorage;
        public int SupplyChainStability;
        public int GoodsDemand;
        public int GoodsBalance;
        public int ResourcePotential;
        public int ResourceSpecialization;
        public int IndustrialSpecialization;
        public int WorkforceSkill;
        public int LaborShortage;
        public int ProductivityBonus;
        public int InnovationCapacity;
        public int EconomicSpecializationScore;
        public string EconomicSpecializationFocus = string.Empty;
        public string EconomicSpecializationDriver = string.Empty;
        public string EconomicSpecializationAction = string.Empty;
        public int JobsHousingBalance;
        public int CommuteEfficiency;
        public int CarDependency;
        public int ParkingPressure;
        public int ParkingCoverage;
        public int ParkingLoad;
        public int ParkingCapacity;
        public int ParkingUtilization;
        public int Walkability;
        public int EnvironmentQuality;
        public int NoiseStress;
        public int PublicHealth;
        public int HealthRisk;
        public int CityScore;
        public int RoadTiles;
        public int ArterialRoadTiles;
        public int RoadCapacity;
        public int RoadLoad;
        public int RoadConnectivity;
        public int IntersectionDelay;
        public int RoadBottleneckPressure;
        public int RoadHierarchyPressure;
        public string RoadHierarchyFocus = string.Empty;
        public string RoadHierarchyDriver = string.Empty;
        public string RoadHierarchyAction = string.Empty;
        public int CommuteCorridorScore;
        public string CommuteCorridorFocus = string.Empty;
        public string CommuteCorridorDriver = string.Empty;
        public string CommuteCorridorAction = string.Empty;
        public int DeadEndRoadTiles;
        public int IntersectionRoadTiles;
        public int BuildingCount;
        public int ZonedDevelopmentBuildings;
        public int HighDensityResidentialBuildings;
        public int DevelopedZoneTiles;
        public int LandUseEfficiency;
        public int IdleZoneTiles;
        public int DevelopmentQuality;
        public int LandUseConflict;
        public int UpgradedBuildings;
        public int MaxBuildingLevel = 1;
        public int BuildingUpgradeReadinessScore;
        public int BuildingUpgradeReadyCount;
        public int BuildingUpgradeBlockedCount;
        public string BuildingUpgradeReadinessFocus = string.Empty;
        public string BuildingUpgradeReadinessDriver = string.Empty;
        public string BuildingUpgradeReadinessAction = string.Empty;
        public int ConnectedBuildings;
        public int DisconnectedBuildings;
        public int ZonedTiles;
        public int ResidentialZoneTiles;
        public int CommercialZoneTiles;
        public int IndustrialZoneTiles;
        public int OfficeZoneTiles;
        public int MixedUseZoneTiles;
        public int UtilityZoneTiles;
        public int OfficeJobs;
        public int MixedUseBuildings;
        public int LandmarkBuildings;
        public int AverageLandValue;
        public int RentPressure;
        public int HousingAffordabilityScore;
        public string HousingAffordabilityFocus = string.Empty;
        public string HousingAffordabilityDriver = string.Empty;
        public string HousingAffordabilityAction = string.Empty;
        public int LivingCondition;
        public int LivingPressure;
        public int TaxIncome;
        public CityTaxLevel TaxLevel = CityTaxLevel.Normal;
        public int TaxRatePercent = 100;
        public CityServiceBudgetLevel ServiceBudgetLevel = CityServiceBudgetLevel.Standard;
        public int ServiceBudgetPercent = 100;
        public int UpkeepExpense;
        public int RoadExpense;
        public int PolicyExpense;
        public int ServiceBudgetExpense;
        public int NetIncome;
        public int FiscalHealth;
        public int DebtPressure;
        public int BondPrincipal;
        public int BondPayment;
        public int CashRunwayDays;
        public int ForecastRisk;
        public string ForecastFocus = string.Empty;
        public string ForecastAction = string.Empty;
        public int BudgetStress;
        public string BudgetFocus = string.Empty;
        public string BudgetDriver = string.Empty;
        public string BudgetAction = string.Empty;
        public int DistrictPriorityScore;
        public string DistrictPriorityFocus = string.Empty;
        public string DistrictPriorityDriver = string.Empty;
        public string DistrictPriorityAction = string.Empty;
        public int DemandUrgency;
        public string DemandFocus = string.Empty;
        public string DemandDriver = string.Empty;
        public string DemandAction = string.Empty;
        public int AdministrationEfficiency;
        public int AdministrationLoad;
        public int AdministrationCapacity;
        public int AdministrationUtilization;
        public int PolicyBacklog;
        public int LastBudgetChange;
        public int Employment;
        public int Unemployment;
        public string CityLevelName = "新生街区";
        public DemandMetrics Demand = new DemandMetrics();
        public CityObjective ActiveObjective = new CityObjective();
        public List<CityMilestone> Milestones = new List<CityMilestone>();
        public List<string> Alerts = new List<string>();
        public List<string> RecentEvents = new List<string>();
        public List<string> UnlockedBuildingIds = new List<string>();
        public List<CityPolicy> ActivePolicies = new List<CityPolicy>();
        public bool LockedExpansionUnlocked;
    }

    [Serializable]
    public sealed class ConstructionPreview
    {
        public string Title = string.Empty;
        public List<string> Lines = new List<string>();
        public bool Ok;
        public string ConfirmLabel = string.Empty;
        public int SiteScore;
        public string SiteDiagnosis = string.Empty;
        public string buildingId = string.Empty;
    }

    [Serializable]
    public sealed class SavedBuilding
    {
        public string Id = string.Empty;
        public string ConfigId = string.Empty;
        public GridPos Pos;
        public int AgeDays;
        public int Level = 1;
        public bool AutoDeveloped;
    }

    [Serializable]
    public sealed class SavedZoneTile
    {
        public GridPos Pos;
        public ZoneType Zone;
    }

    [Serializable]
    public sealed class SavedRoadSegment
    {
        public GridPos Pos;
        public RoadTier Tier = RoadTier.Local;
    }

    [Serializable]
    public sealed class CitySaveData
    {
        public int Version = 1;
        public int Day;
        public int Population;
        public int Cash;
        public int Happiness;
        public int BondPrincipal;
        public CityTaxLevel TaxLevel = CityTaxLevel.Normal;
        public CityServiceBudgetLevel ServiceBudgetLevel = CityServiceBudgetLevel.Standard;
        public int NextId;
        public float DayAccumulator;
        public List<GridPos> Roads = new List<GridPos>();
        public List<SavedRoadSegment> RoadSegments = new List<SavedRoadSegment>();
        public List<SavedZoneTile> Zones = new List<SavedZoneTile>();
        public List<SavedBuilding> Buildings = new List<SavedBuilding>();
        public List<string> UnlockedBuildingIds = new List<string>();
        public List<CityPolicy> ActivePolicies = new List<CityPolicy>();
        public bool LockedExpansionUnlocked;
    }
}
