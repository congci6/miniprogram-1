using System;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Simulation
{
    public sealed class CitySimulationCore
    {
        private readonly CityConfig config;

        // 公共访问器
        public CityConfig Config => config;
        private readonly List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        private readonly List<RoadNode> roads = new List<RoadNode>();
        private readonly List<CityPolicy> activePolicies = new List<CityPolicy>();
        private readonly List<string> recentEvents = new List<string>();
        private readonly AdvisorContextTracker advisorContext = new AdvisorContextTracker();
        private const int CityEventDigestLimit = 8;
        private const string CityEventDigestMarker = "CITY_EVENT_DIGEST";
        private const string DemandDriverAnalysisMarker = "DEMAND_DRIVER_ANALYSIS";
        private const string RiskForecastAdvisorMarker = "RISK_FORECAST_ADVISOR";
        private const string BudgetBreakdownAdvisorMarker = "BUDGET_BREAKDOWN_ADVISOR";
        private const string DistrictPriorityAdvisorMarker = "DISTRICT_PRIORITY_ADVISOR";
        private const string RoadHierarchyAdvisorMarker = "ROAD_HIERARCHY_ADVISOR";
        private const string CommuteCorridorAdvisorMarker = "COMMUTE_CORRIDOR_ADVISOR";
        private const string EconomicSpecializationAdvisorMarker = "ECONOMIC_SPECIALIZATION_ADVISOR";
        private const string ServiceGapAdvisorMarker = "SERVICE_GAP_ADVISOR";
        private const string GrowthBottleneckAdvisorMarker = "GROWTH_BOTTLENECK_ADVISOR";
        private const string HousingAffordabilityAdvisorMarker = "HOUSING_AFFORDABILITY_ADVISOR";
        private const string BuildingUpgradeReadinessAdvisorMarker = "BUILDING_UPGRADE_READINESS_ADVISOR";
        private const string InfrastructureResilienceAdvisorMarker = "INFRASTRUCTURE_RESILIENCE_ADVISOR";
        private CityTaxLevel taxLevel = CityTaxLevel.Normal;
        private CityServiceBudgetLevel serviceBudgetLevel = CityServiceBudgetLevel.Standard;
        private int bondPrincipal;
        private float dayAccumulator;
        private int nextId = 1;

        // Performance optimization: metrics dirty flag
        private bool metricsDirty = true;
        private int metricsComputeCount = 0;

        public void MarkMetricsDirty()
        {
            metricsDirty = true;
        }

        public CityGridCore Grid { get; private set; }
        public CityMetrics Metrics { get; private set; }
        public IReadOnlyList<PlacedBuilding> Buildings { get { return buildings; } }
        public IReadOnlyList<RoadNode> Roads { get { return roads; } }
        public IReadOnlyList<CityPolicy> ActivePolicies { get { return activePolicies; } }
        public CityTaxLevel TaxLevel { get { return taxLevel; } }
        public CityServiceBudgetLevel ServiceBudgetLevel { get { return serviceBudgetLevel; } }
        public AdvisorContextTracker AdvisorContext { get { return advisorContext; } }

        public CitySimulationCore(CityConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.config = config;
            Grid = new CityGridCore(config.MapWidth, config.MapHeight);
            Metrics = new CityMetrics();
            Reset();
        }

        public void Reset()
        {
            buildings.Clear();
            roads.Clear();
            activePolicies.Clear();
            taxLevel = CityTaxLevel.Normal;
            serviceBudgetLevel = CityServiceBudgetLevel.Standard;
            bondPrincipal = 0;
            nextId = 1;
            dayAccumulator = 0f;
            recentEvents.Clear();
            Grid = new CityGridCore(config.MapWidth, config.MapHeight);
            Metrics = new CityMetrics
            {
                Day = 1,
                Population = 0,
                Cash = config.InitialCash,
                Happiness = config.InitialHappiness,
                CityScore = 50,
                CityLevelName = "新生街区"
            };

            SeedStartingRoad();
            SeedStartingZones();
            SeedStarterBuildings();
            AddCityEvent("\u57ce\u5e02\u5f00\u5c40\uff1a\u5df2\u51c6\u5907\u9996\u4e2a\u8857\u533a");
            MarkMetricsDirty();
            RecomputeMetrics();
            advisorContext.Reset();
        }

        public void Tick(float deltaSeconds)
        {
            var secondsPerDay = Math.Max(1, config.SecondsPerSimulationDay);
            dayAccumulator += Math.Max(0f, deltaSeconds);

            while (dayAccumulator >= secondsPerDay)
            {
                dayAccumulator -= secondsPerDay;
                AdvanceDay();
            }
        }

        public CitySaveData CreateSaveData()
        {
            var save = new CitySaveData
            {
                Version = 7,
                Day = Metrics.Day,
                Population = Metrics.Population,
                Cash = Metrics.Cash,
                Happiness = Metrics.Happiness,
                BondPrincipal = bondPrincipal,
                TaxLevel = taxLevel,
                ServiceBudgetLevel = serviceBudgetLevel,
                NextId = nextId,
                DayAccumulator = dayAccumulator,
                LockedExpansionUnlocked = Metrics.LockedExpansionUnlocked
            };
            save.AdvisorContext = advisorContext.CreateSaveData();

            for (var i = 0; i < roads.Count; i += 1)
            {
                save.Roads.Add(roads[i].Pos);
                save.RoadSegments.Add(new SavedRoadSegment
                {
                    Pos = roads[i].Pos,
                    Tier = roads[i].Tier
                });
            }

            foreach (var pos in Grid.AllPositions())
            {
                var tile = Grid.GetTile(pos);
                if (tile.Zone != ZoneType.None)
                {
                    save.Zones.Add(new SavedZoneTile
                    {
                        Pos = pos,
                        Zone = tile.Zone
                    });
                }
            }

            for (var i = 0; i < buildings.Count; i += 1)
            {
                save.Buildings.Add(new SavedBuilding
                {
                    Id = buildings[i].Id,
                    ConfigId = buildings[i].ConfigId,
                    Pos = buildings[i].Pos,
                    AgeDays = buildings[i].AgeDays,
                    Level = BuildingLevel(buildings[i]),
                    AutoDeveloped = buildings[i].AutoDeveloped
                });
            }

            for (var i = 0; i < Metrics.UnlockedBuildingIds.Count; i += 1)
            {
                save.UnlockedBuildingIds.Add(Metrics.UnlockedBuildingIds[i]);
            }

            for (var i = 0; i < activePolicies.Count; i += 1)
            {
                save.ActivePolicies.Add(activePolicies[i]);
            }

            return save;
        }

        public bool ApplySaveData(CitySaveData save)
        {
            if (save == null || save.Version <= 0)
            {
                return false;
            }

            buildings.Clear();
            roads.Clear();
            activePolicies.Clear();
            recentEvents.Clear();
            taxLevel = save.Version >= 2 && Enum.IsDefined(typeof(CityTaxLevel), save.TaxLevel)
                ? save.TaxLevel
                : CityTaxLevel.Normal;
            serviceBudgetLevel = save.Version >= 5 && Enum.IsDefined(typeof(CityServiceBudgetLevel), save.ServiceBudgetLevel)
                ? save.ServiceBudgetLevel
                : CityServiceBudgetLevel.Standard;
            bondPrincipal = save.Version >= 6 ? Math.Max(0, save.BondPrincipal) : 0;
            Grid = new CityGridCore(config.MapWidth, config.MapHeight);
            nextId = Math.Max(1, save.NextId);
            dayAccumulator = Math.Max(0f, save.DayAccumulator);
            Metrics = new CityMetrics
            {
                Day = Math.Max(1, save.Day),
                Population = Math.Max(0, save.Population),
                Cash = save.Cash,
                Happiness = ClampToScore(save.Happiness),
                CityScore = 50,
                CityLevelName = CityLevelNameForPopulation(save.Population)
            };

            Metrics.LockedExpansionUnlocked = save.Version < 6 || save.LockedExpansionUnlocked;
            Grid.ExpansionUnlocked = Metrics.LockedExpansionUnlocked;

            if (save.Zones != null)
            {
                for (var i = 0; i < save.Zones.Count; i += 1)
                {
                    if (Enum.IsDefined(typeof(ZoneType), save.Zones[i].Zone) && Grid.InBounds(save.Zones[i].Pos) && string.IsNullOrEmpty(Grid.CanSetZone(save.Zones[i].Pos, save.Zones[i].Zone)))
                    {
                        Grid.SetZone(save.Zones[i].Pos, save.Zones[i].Zone);
                    }
                }
            }

            if (save.RoadSegments != null && save.RoadSegments.Count > 0)
            {
                for (var i = 0; i < save.RoadSegments.Count; i += 1)
                {
                    var segment = save.RoadSegments[i];
                    if (segment != null && Enum.IsDefined(typeof(RoadTier), segment.Tier) && Grid.CanPlaceRoad(segment.Pos) && string.IsNullOrEmpty(Grid.GetTile(segment.Pos).RoadId))
                    {
                        AddRoadTile(segment.Pos, segment.Tier);
                    }
                }
            }
            else if (save.Roads != null)
            {
                for (var i = 0; i < save.Roads.Count; i += 1)
                {
                    if (Grid.CanPlaceRoad(save.Roads[i]) && string.IsNullOrEmpty(Grid.GetTile(save.Roads[i]).RoadId))
                    {
                        AddRoadTile(save.Roads[i]);
                    }
                }
            }

            if (save.Buildings != null)
            {
                for (var i = 0; i < save.Buildings.Count; i += 1)
                {
                    RestoreBuilding(save.Buildings[i]);
                }
            }

            if (save.UnlockedBuildingIds != null)
            {
                for (var i = 0; i < save.UnlockedBuildingIds.Count; i += 1)
                {
                    if (!Metrics.UnlockedBuildingIds.Contains(save.UnlockedBuildingIds[i]))
                    {
                        Metrics.UnlockedBuildingIds.Add(save.UnlockedBuildingIds[i]);
                    }
                }
            }

            if (save.ActivePolicies != null)
            {
                for (var i = 0; i < save.ActivePolicies.Count; i += 1)
                {
                    if (Enum.IsDefined(typeof(CityPolicy), save.ActivePolicies[i]) && !activePolicies.Contains(save.ActivePolicies[i]))
                    {
                        activePolicies.Add(save.ActivePolicies[i]);
                    }
                }
            }
            if (save.Version >= 7)
            {
                advisorContext.ApplySaveData(save.AdvisorContext);
            }
            else
            {
                advisorContext.Reset();
            }

            AddCityEvent("\u8bfb\u53d6\u5b58\u6863\uff1a\u7b2c " + Metrics.Day + " \u5929");
            MarkMetricsDirty();
            RecomputeMetrics();
            return true;
        }

        public void CycleTaxLevel()
        {
            if (taxLevel == CityTaxLevel.Normal)
            {
                taxLevel = CityTaxLevel.High;
            }
            else if (taxLevel == CityTaxLevel.High)
            {
                taxLevel = CityTaxLevel.Low;
            }
            else
            {
                taxLevel = CityTaxLevel.Normal;
            }

            AddCityEvent("\u7a0e\u7387\u8c03\u6574\uff1a" + TaxLevelLabel(taxLevel));
            advisorContext.RecordAction("cycle_tax");
            MarkMetricsDirty();
            RecomputeMetrics();
        }

        public void CycleServiceBudgetLevel()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Standard)
            {
                serviceBudgetLevel = CityServiceBudgetLevel.Boosted;
            }
            else if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted)
            {
                serviceBudgetLevel = CityServiceBudgetLevel.Lean;
            }
            else
            {
                serviceBudgetLevel = CityServiceBudgetLevel.Standard;
            }

            AddCityEvent("\u670d\u52a1\u9884\u7b97\uff1a" + ServiceBudgetLevelLabel(serviceBudgetLevel));
            advisorContext.RecordAction("cycle_budget");
            MarkMetricsDirty();
            RecomputeMetrics();
        }

        public bool IssueMunicipalBond()
        {
            if (bondPrincipal + MunicipalBondPrincipal() > MunicipalBondDebtLimit())
            {
                return false;
            }

            bondPrincipal += MunicipalBondPrincipal();
            Metrics.Cash += MunicipalBondCash();
            AddCityEvent("\u53d1\u884c\u5e02\u653f\u503a\uff1a\u73b0\u91d1 +" + MunicipalBondCash());
            MarkMetricsDirty();
            RecomputeMetrics();
            return true;
        }

        public bool IsPolicyActive(CityPolicy policy)
        {
            return activePolicies.Contains(policy);
        }

        public void TogglePolicy(CityPolicy policy)
        {
            if (activePolicies.Contains(policy))
            {
                activePolicies.Remove(policy);
                AddCityEvent("\u5173\u95ed\u653f\u7b56\uff1a" + CityPolicyLabel(policy));
            }
            else
            {
                activePolicies.Add(policy);
                AddCityEvent("\u542f\u7528\u653f\u7b56\uff1a" + CityPolicyLabel(policy));
            }

            advisorContext.RecordAction("toggle_policy");
            MarkMetricsDirty();
            RecomputeMetrics();
        }

        public ConstructionPreview PreviewBuilding(string buildingId, GridPos pos)
        {
            var definition = config.GetBuilding(buildingId);
            if (definition == null)
            {
                return BlockedPreview("未知建筑", "建筑配置缺失");
            }

            var preview = new ConstructionPreview
            {
                Title = definition.Name,
                ConfirmLabel = "建造"
            };
            preview.Lines.Add("花费 " + definition.Cost + "  维护 " + definition.Upkeep + "/月");
            preview.Lines.Add(BuildingEffectLine(definition));
            if (definition.PreferredZone != ZoneType.None)
            {
                preview.Lines.Add("推荐分区：" + ZoneLabel(definition.PreferredZone));
            }

            if (IsUpgradeableBuilding(definition))
            {
                preview.Lines.Add("地价、公交、服务和品质充足时会自然升级");
            }

            var unlockReason = UnlockReason(definition);
            if (!string.IsNullOrEmpty(unlockReason))
            {
                preview.Ok = false;
                preview.Lines.Insert(0, unlockReason);
                return preview;
            }

            if (definition.Cost > Metrics.Cash)
            {
                preview.Ok = false;
                preview.Lines.Insert(0, CashShortfallReason(definition.Cost));
                return preview;
            }

            var placementReason = Grid.CanPlaceBuilding(pos, definition.Size);
            if (!string.IsNullOrEmpty(placementReason))
            {
                preview.Ok = false;
                preview.Lines.Insert(0, placementReason);
                return preview;
            }

            var zoneReason = Grid.ZoneReasonForBuilding(pos, definition.Size, definition.PreferredZone);
            if (!string.IsNullOrEmpty(zoneReason))
            {
                preview.Ok = false;
                preview.Lines.Insert(0, zoneReason);
                return preview;
            }

            var connectedRoadId = NearestRoadId(pos, definition.Size);
            var hasRoad = !string.IsNullOrEmpty(connectedRoadId);
            preview.Ok = true;
            preview.SiteScore = BuildingSiteScore(pos, definition, hasRoad);
            preview.SiteDiagnosis = SiteDiagnosis(pos, definition, hasRoad, preview.SiteScore);
            preview.Lines.Add(!hasRoad
                ? "附近无道路，建成后只有 20% 效率"
                : "接路良好，建筑可满效率运行");
            preview.Lines.Add("再次点击同一地块确认");
            return preview;
        }

        public ConstructionPreview PreviewRoad(GridPos from, GridPos to)
        {
            var points = UniquePositions(ManhattanLine(from, to));
            var newTiles = 0;

            for (var i = 0; i < points.Count; i += 1)
            {
                if (!Grid.InBounds(points[i]))
                {
                    return BlockedPreview("道路方案", "道路超出地图边界");
                }

                if (Grid.IsLockedExpansionTile(points[i]))
                {
                    return BlockedPreview("道路方案", points[i].X + "," + points[i].Y + " 未解锁区域");
                }

                var tile = Grid.GetTile(points[i]);
                if (string.IsNullOrEmpty(tile.RoadId))
                {
                    if (!Grid.CanPlaceRoad(points[i]))
                    {
                        return BlockedPreview("道路方案", points[i].X + "," + points[i].Y + " 不能铺路");
                    }

                    newTiles += 1;
                }
            }

            var cost = newTiles * config.RoadCostPerTile;
            if (cost > Metrics.Cash)
            {
                return BlockedPreview("道路方案", CashShortfallReason(cost));
            }

            var preview = new ConstructionPreview
            {
                Title = "道路方案",
                Ok = true,
                ConfirmLabel = "铺设"
            };
            preview.Lines.Add("长度 " + points.Count + " 格");
            preview.Lines.Add("新建 " + newTiles + " 格  花费 " + cost);
            preview.Lines.Add("道路会带来容量，也会产生维护费用");
            return preview;
        }

        public ConstructionPreview PreviewRoadUpgrade(GridPos pos)
        {
            if (!Grid.InBounds(pos))
            {
                return BlockedPreview("道路升级", "道路超出地图边界");
            }

            var road = FindRoadAt(pos);
            if (road == null)
            {
                return BlockedPreview("道路升级", "此处没有道路");
            }

            if (road.Tier == RoadTier.Arterial)
            {
                return BlockedPreview("道路升级", "已经是主干道");
            }

            var cost = ArterialRoadUpgradeCost();
            if (cost > Metrics.Cash)
            {
                return BlockedPreview("道路升级", CashShortfallReason(cost));
            }

            var preview = new ConstructionPreview
            {
                Title = "升级主干道",
                Ok = true,
                ConfirmLabel = "升级"
            };
            preview.Lines.Add("花费 " + cost + "  维护 +" + (RoadUpkeepForTier(RoadTier.Arterial) - RoadUpkeepForTier(RoadTier.Local)) + "/月");
            preview.Lines.Add("容量 " + RoadCapacityForTier(RoadTier.Local) + " -> " + RoadCapacityForTier(RoadTier.Arterial));
            preview.Lines.Add("主干道缓解拥堵，但会提高沿线噪声");
            return preview;
        }

        public ConstructionPreview PreviewZone(GridPos from, GridPos to, ZoneType zone)
        {
            var points = RectPositions(from, to);
            var changedTiles = 0;

            for (var i = 0; i < points.Count; i += 1)
            {
                var reason = Grid.CanSetZone(points[i], zone);
                if (!string.IsNullOrEmpty(reason))
                {
                    return BlockedPreview("分区规划", reason);
                }

                if (Grid.GetTile(points[i]).Zone != zone)
                {
                    changedTiles += 1;
                }
            }

            var cost = zone == ZoneType.None ? 0 : changedTiles * config.ZoneCostPerTile;
            if (cost > Metrics.Cash)
            {
                return BlockedPreview("分区规划", CashShortfallReason(cost));
            }

            var preview = new ConstructionPreview
            {
                Title = "分区规划",
                Ok = true,
                ConfirmLabel = zone == ZoneType.None ? "取消分区" : "划定"
            };
            preview.Lines.Add(ZoneLabel(zone) + "  " + changedTiles + " 格");
            preview.Lines.Add("花费 " + cost);
            if (zone == ZoneType.Residential || zone == ZoneType.Commercial || zone == ZoneType.Industrial || zone == ZoneType.Office || zone == ZoneType.MixedUse)
            {
                preview.Lines.Add("适宜度 " + ZoneSuitabilityForRect(points, zone) + "%");
                preview.Lines.Add("缓冲风险 " + ZoneConflictRiskForRect(points, zone) + "%");
            }

            preview.Lines.Add("分区会影响需求、地价和建筑适配");
            return preview;
        }

        public ConstructionPreview PreviewDemolish(GridPos pos)
        {
            var buildingId = Grid.FindBuildingIdAt(pos);
            if (string.IsNullOrEmpty(buildingId))
            {
                return BlockedPreview("拆除", "此处没有建筑");
            }

            var placed = FindPlacedBuilding(buildingId);
            if (placed == null)
            {
                return BlockedPreview("拆除", "建筑数据缺失");
            }

            var definition = config.GetBuilding(placed.ConfigId);
            if (definition == null)
            {
                return BlockedPreview("拆除", "建筑配置缺失");
            }

            var refund = (int)Math.Round(definition.Cost * config.DemolishRefundRate);
            var preview = new ConstructionPreview
            {
                Title = "拆除 " + definition.Name,
                Ok = true,
                ConfirmLabel = "拆除"
            };
            preview.Lines.Add("返还 " + refund);
            preview.Lines.Add("会立即移除容量、岗位和服务效果");
            return preview;
        }

        public bool TryPlaceBuilding(string buildingId, GridPos pos, out ConstructionPreview preview)
        {
            preview = PreviewBuilding(buildingId, pos);
            if (!preview.Ok)
            {
                return false;
            }

            var serviceGapBefore = Metrics.ServiceGapPressure;
            var happinessBefore = Metrics.Happiness;
            PlaceBuildingInternal(buildingId, pos, true);
            AddCityEvent("\u5efa\u6210\uff1a" + preview.Title);

            RecordBuildingAction(buildingId);

            MarkMetricsDirty();
            RecomputeMetrics();
            preview.Lines.Insert(0, BuildingCommandImpactLine(serviceGapBefore, happinessBefore));
            return true;
        }

        public ConstructionPreview PreviewPlaceBuilding(string buildingId, GridPos pos, int rotation)
        {
            return PreviewBuilding(buildingId, pos);
        }

        public bool TryPlaceBuildingAt(string buildingId, GridPos pos, int rotation, out ConstructionPreview preview)
        {
            return TryPlaceBuilding(buildingId, pos, out preview);
        }

        public bool TryDemolish(string buildingId)
        {
            var placed = FindPlacedBuilding(buildingId);
            if (placed == null) return false;
            ConstructionPreview preview;
            return TryDemolishAt(placed.BuildingOrigin, out preview);
        }

        public bool TryBuildRoad(GridPos from, GridPos to, out ConstructionPreview preview)
        {
            preview = PreviewRoad(from, to);
            if (!preview.Ok)
            {
                return false;
            }

            var points = UniquePositions(ManhattanLine(from, to));
            var newTiles = new List<GridPos>();
            for (var i = 0; i < points.Count; i += 1)
            {
                if (string.IsNullOrEmpty(Grid.GetTile(points[i]).RoadId))
                {
                    newTiles.Add(points[i]);
                }
            }

            var connectivityBefore = Metrics.RoadConnectivity;
            var bottleneckBefore = Metrics.RoadBottleneckPressure;
            Metrics.Cash -= newTiles.Count * config.RoadCostPerTile;
            for (var i = 0; i < newTiles.Count; i += 1)
            {
                AddRoadTile(newTiles[i]);
            }

            if (newTiles.Count > 0)
            {
                AddCityEvent("\u94fa\u8def\uff1a" + newTiles.Count + " \u683c");
                advisorContext.RecordAction("build_road");
            }

            MarkMetricsDirty();
            RecomputeMetrics();
            preview.Lines.Insert(0, RoadCommandImpactLine(connectivityBefore, bottleneckBefore));
            return true;
        }

        public bool TryUpgradeRoad(GridPos pos, out ConstructionPreview preview)
        {
            preview = PreviewRoadUpgrade(pos);
            if (!preview.Ok)
            {
                return false;
            }

            var road = FindRoadAt(pos);
            if (road == null || road.Tier == RoadTier.Arterial)
            {
                return false;
            }

            var connectivityBefore = Metrics.RoadConnectivity;
            var bottleneckBefore = Metrics.RoadBottleneckPressure;
            road.Tier = RoadTier.Arterial;
            Metrics.Cash -= ArterialRoadUpgradeCost();
            AddCityEvent("\u5347\u7ea7\u4e3b\u5e72\u9053\uff1a" + pos.X + "," + pos.Y);
            advisorContext.RecordAction("upgrade_road");
            MarkMetricsDirty();
            RecomputeMetrics();
            preview.Lines.Insert(0, RoadCommandImpactLine(connectivityBefore, bottleneckBefore));
            return true;
        }

        public bool TrySetZone(GridPos from, GridPos to, ZoneType zone, out ConstructionPreview preview)
        {
            preview = PreviewZone(from, to, zone);
            if (!preview.Ok)
            {
                return false;
            }

            var demandBefore = Metrics.DemandUrgency;
            var idleBefore = Metrics.IdleZoneTiles;
            var points = RectPositions(from, to);
            var changedTiles = 0;
            for (var i = 0; i < points.Count; i += 1)
            {
                if (Grid.GetTile(points[i]).Zone != zone)
                {
                    changedTiles += 1;
                    Grid.SetZone(points[i], zone);
                }
            }

            if (zone != ZoneType.None)
            {
                Metrics.Cash -= changedTiles * config.ZoneCostPerTile;
            }

            if (changedTiles > 0)
            {
                AddCityEvent("\u5206\u533a\u8c03\u6574\uff1a" + ZoneLabel(zone) + " " + changedTiles + " \u683c");
                advisorContext.RecordAction("set_zone");
            }

            MarkMetricsDirty();
            RecomputeMetrics();
            preview.Lines.Insert(0, ZoneCommandImpactLine(demandBefore, idleBefore));
            return true;
        }

        public bool TryDemolishAt(GridPos pos, out ConstructionPreview preview)
        {
            preview = PreviewDemolish(pos);
            if (!preview.Ok)
            {
                return false;
            }

            var buildingId = Grid.FindBuildingIdAt(pos);
            var placed = FindPlacedBuilding(buildingId);
            var definition = placed != null ? config.GetBuilding(placed.ConfigId) : null;
            if (placed == null || definition == null)
            {
                return false;
            }

            var serviceGapBefore = Metrics.ServiceGapPressure;
            var happinessBefore = Metrics.Happiness;
            var cashBefore = Metrics.Cash;
            var capacityBefore = Metrics.HousingCapacity;
            buildings.Remove(placed);
            Grid.RemoveBuilding(buildingId);
            Metrics.Cash += (int)Math.Round(definition.Cost * config.DemolishRefundRate);
            AddCityEvent("\u62c6\u9664\uff1a" + definition.Name);
            MarkMetricsDirty();
            RecomputeMetrics();
            preview.Lines.Insert(0, DemolishCommandImpactLine(serviceGapBefore, happinessBefore, cashBefore, capacityBefore));
            return true;
        }

        public void DamageBuilding(string buildingId, int damage)
        {
            var placed = FindPlacedBuilding(buildingId);
            if (placed == null) return;

            placed.Efficiency = Math.Max(0f, placed.Efficiency - damage / 100f);

            if (placed.Efficiency <= 0f)
            {
                buildings.Remove(placed);
                Grid.RemoveBuilding(buildingId);
                AddCityEvent("\u5efa\u7b51\u88ab\u6467\u6bc1");
                MarkMetricsDirty();
                RecomputeMetrics();
            }
        }

        /// <summary>
        /// \u73a9\u5bb6\u624b\u52a8\u5347\u7ea7\u5efa\u7b51\uff08\u9700\u8981\u6750\u6599\uff09
        /// </summary>
        public bool TryUpgradeBuildingWithMaterials(string buildingId)
        {
            var placed = FindPlacedBuilding(buildingId);
            if (placed == null || placed.Level >= 5) return false;

            var definition = config.GetBuilding(placed.ConfigId);
            if (definition == null) return false;

            // \u68c0\u67e5\u662f\u5426\u6ee1\u8db3\u5929\u6570\u8981\u6c42
            int requiredDays = placed.Level switch
            {
                1 => 60,
                2 => 120,
                3 => 180,
                4 => 250,
                _ => int.MaxValue
            };

            if (placed.AgeDays < requiredDays)
            {
                AddCityEvent($"\u5efa\u7b51\u9700\u8981{requiredDays - placed.AgeDays}\u5929\u540e\u624d\u80fd\u5347\u7ea7");
                return false;
            }

            // \u5347\u7ea7\u6210\u529f
            placed.Level++;
            AddCityEvent($"{definition.Name} \u5347\u7ea7\u5230 Lv.{placed.Level}");
            MarkMetricsDirty();
            RecomputeMetrics();
            return true;
        }

        /// <summary>
        /// \u68c0\u67e5\u5efa\u7b51\u662f\u5426\u53ef\u4ee5\u5347\u7ea7
        /// </summary>
        public bool CanUpgradeBuilding(string buildingId, out int requiredDays)
        {
            requiredDays = 0;
            var placed = FindPlacedBuilding(buildingId);
            if (placed == null || placed.Level >= 5) return false;

            requiredDays = placed.Level switch
            {
                1 => 60,
                2 => 120,
                3 => 180,
                4 => 250,
                _ => int.MaxValue
            };

            return placed.AgeDays >= requiredDays;
        }

        public void RecomputeMetrics()
        {
            // Performance optimization: skip if already computed (dirty flag pattern)
            if (!metricsDirty)
            {
                return;
            }

            metricsComputeCount++;
            metricsDirty = false;

            EnsureMetricLists();
            RefreshActivePolicyMetrics();
            RefreshBuildingRoadConnections();
            RefreshRoadNeighborCounts();
            Grid.ResetDynamicTileValues();

            var powerSupply = 0;
            var powerDemand = 0;
            var waterSupply = 0;
            var waterDemand = 0;
            var upkeep = 0;
            var serviceBudgetExpense = 0;

            for (var i = 0; i < buildings.Count; i += 1)
            {
                var placed = buildings[i];
                var definition = config.GetBuilding(placed.ConfigId);
                if (definition == null)
                {
                    continue;
                }

                var connectionEfficiency = string.IsNullOrEmpty(placed.ConnectedRoadId) ? 0.2f : 1f;
                powerSupply += (int)Math.Floor(BudgetAdjustedMunicipalOutput(definition, definition.PowerOutput) * connectionEfficiency);
                waterSupply += (int)Math.Floor(BudgetAdjustedMunicipalOutput(definition, definition.WaterOutput) * connectionEfficiency);
                var level = BuildingLevel(placed);
                powerDemand += LevelScaledUtilityUse(definition.PowerUse, level);
                waterDemand += LevelScaledUtilityUse(definition.WaterUse, level);
                var baseUpkeep = LevelScaledUpkeep(definition.Upkeep, level);
                var adjustedUpkeep = BudgetAdjustedBuildingUpkeep(definition, baseUpkeep);
                upkeep += adjustedUpkeep;
                serviceBudgetExpense += adjustedUpkeep - baseUpkeep;
            }

            var utilityEfficiency = UtilityEfficiency(powerSupply, powerDemand, waterSupply, waterDemand);
            var utilityLoad = UtilityLoad(powerDemand, waterDemand);
            var utilityCapacity = UtilityCapacity(powerSupply, waterSupply);
            var utilityUtilization = UtilityUtilization(utilityLoad, utilityCapacity);
            var utilityReliability = UtilityReliability(powerSupply, powerDemand, waterSupply, waterDemand);
            var parkBuildings = ConnectedParkBuildings();
            var healthBuildings = ConnectedHealthBuildings();
            var deathcareBuildings = ConnectedDeathcareBuildings();
            var educationBuildings = ConnectedEducationBuildings();
            var advancedEducationBuildings = ConnectedAdvancedEducationBuildings();
            var innovationBuildings = ConnectedInnovationBuildings();
            var attractionBuildings = ConnectedAttractionBuildings();
            var shelterBuildings = ConnectedShelterBuildings();
            var safetyBuildings = ConnectedSafetyBuildings();
            var fireBuildings = ConnectedFireBuildings();
            var securityBuildings = ConnectedSecurityBuildings();
            var transitBuildings = ConnectedTransitBuildings();
            var regionalConnectionBuildings = ConnectedRegionalConnectionBuildings();
            var logisticsBuildings = ConnectedLogisticsBuildings();
            var warehouseBuildings = ConnectedWarehouseBuildings();
            var resourceBuildings = ConnectedResourceBuildings();
            var freightRailBuildings = ConnectedFreightRailBuildings();
            var wasteBuildings = ConnectedWasteBuildings();
            var wastewaterBuildings = ConnectedWastewaterBuildings();
            var communicationBuildings = ConnectedCommunicationBuildings();
            var mailBuildings = ConnectedMailBuildings();
            var roadMaintenanceBuildings = ConnectedRoadMaintenanceBuildings();
            var parkingBuildings = ConnectedParkingBuildings();
            var stormwaterBuildings = ConnectedStormwaterBuildings();
            var administrationBuildings = ConnectedAdministrationBuildings();
            var wasteCapacity = BudgetAdjustedServiceValue(WasteCapacityForBuildings(wasteBuildings));
            var wastewaterCapacity = BudgetAdjustedServiceValue(WastewaterCapacityForBuildings(wastewaterBuildings));
            var stormwaterCapacity = StormwaterCapacityForBuildings(stormwaterBuildings);
            var administrationCapacity = AdministrationCapacityForBuildings(administrationBuildings);
            var administrationLoad = AdministrationLoad(Metrics.Population, activePolicies.Count);
            var administrationEfficiency = ComputeAdministrationEfficiency(administrationCapacity, Metrics.Population, activePolicies.Count);
            var administrationUtilization = AdministrationUtilization(administrationLoad, administrationCapacity);
            var policyBacklog = ComputePolicyBacklog(Metrics.Population, activePolicies.Count, administrationEfficiency, administrationUtilization);
            var innovationBase = InnovationBaseForBuildings(innovationBuildings);
            var attractionParkingDemand = AttractionParkingDemandForBuildings(attractionBuildings);
            var housing = 0;
            var jobs = 0;
            var officeJobs = 0;
            var commercialGoodsJobs = 0;
            var industrialJobs = 0;
            var pollution = 0;
            var noise = 0;
            var residentialCapacity = 0;
            var parkedResidentialCapacity = 0;
            var healthyResidentialCapacity = 0;
            var deathcareEligible = 0;
            var deathcareCovered = 0;
            var educatedResidentialCapacity = 0;
            var safetyEligible = 0;
            var safetyCovered = 0;
            var safetyRisk = 0;
            var fireLoad = 0;
            var fireProtected = 0;
            var securityEligible = 0;
            var securityCovered = 0;
            var transitEligible = 0;
            var transitCovered = 0;
            var logisticsEligible = 0;
            var logisticsCovered = 0;
            var wasteEligible = 0;
            var wasteCovered = 0;
            var communicationEligible = 0;
            var communicationCovered = 0;
            var mailEligible = 0;
            var mailCovered = 0;
            var advancedEducationEligible = 0;
            var advancedEducationCovered = 0;
            var parkingEligible = 0;
            var parkingCovered = 0;
            var wastePollution = 0;
            var connectedBuildings = 0;
            var zonedDevelopmentBuildings = 0;
            var highDensityResidentialBuildings = 0;
            var developedZoneTiles = 0;
            var residentialServiceScoreTotal = 0;
            var residentialServiceWeight = 0;
            var underservedServiceWeight = 0;
            var parkServiceGapWeight = 0;
            var healthServiceGapWeight = 0;
            var educationServiceGapWeight = 0;
            var transitServiceGapWeight = 0;
            var safetyServiceGapWeight = 0;
            var securityServiceGapWeight = 0;
            var wasteServiceGapWeight = 0;
            var communicationServiceGapWeight = 0;
            var mailServiceGapWeight = 0;
            var deathcareServiceGapWeight = 0;
            var mixedUseBuildings = 0;
            var landmarkBuildings = 0;
            var upgradedBuildings = 0;
            var maxBuildingLevel = 1;
            var arterialRoadTiles = 0;
            var deadEndRoadTiles = 0;
            var intersectionRoadTiles = 0;
            var roadLoad = 0;
            var buildingTax = 0;

            for (var i = 0; i < roads.Count; i += 1)
            {
                roads[i].Load = 0;
            }

            for (var i = 0; i < buildings.Count; i += 1)
            {
                var placed = buildings[i];
                var definition = config.GetBuilding(placed.ConfigId);
                if (definition == null)
                {
                    continue;
                }

                var connected = !string.IsNullOrEmpty(placed.ConnectedRoadId);
                if (connected)
                {
                    connectedBuildings += 1;
                }

                if (placed.AutoDeveloped)
                {
                    zonedDevelopmentBuildings += 1;
                }

                if (IsGrowthZoneBuilding(definition))
                {
                    developedZoneTiles += Math.Max(1, definition.Size.W * definition.Size.H);
                }

                if (placed.ConfigId == "apartment_block")
                {
                    highDensityResidentialBuildings += 1;
                }

                if (IsMixedUseBuilding(definition))
                {
                    mixedUseBuildings += 1;
                }

                if (IsAttractionBuilding(definition))
                {
                    landmarkBuildings += 1;
                }

                var connectionEfficiency = connected ? 1f : 0.2f;
                var buildingEfficiency = connectionEfficiency * utilityEfficiency;
                if (definition.Category == BuildingCategory.Utility &&
                    (definition.PowerOutput > 0 || definition.WaterOutput > 0))
                {
                    buildingEfficiency = connectionEfficiency;
                }

                placed.Efficiency = buildingEfficiency;
                var level = BuildingLevel(placed);
                if (level > 1)
                {
                    upgradedBuildings += 1;
                }

                maxBuildingLevel = Math.Max(maxBuildingLevel, level);
                var buildingCapacity = (int)Math.Floor(LevelScaledOutput(definition.Capacity, level) * buildingEfficiency);
                var buildingJobs = (int)Math.Floor(LevelScaledOutput(definition.Jobs, level) * buildingEfficiency);
                housing += buildingCapacity;
                jobs += buildingJobs;
                if (IsOfficeBuilding(definition))
                {
                    officeJobs += buildingJobs;
                }

                if (definition.Category == BuildingCategory.Commercial && !IsOfficeBuilding(definition))
                {
                    commercialGoodsJobs += buildingJobs;
                }

                if (definition.Category == BuildingCategory.Industrial)
                {
                    industrialJobs += buildingJobs;
                }

                var transitWeight = Math.Max(0, buildingCapacity + buildingJobs);
                var coveredByTransit = IsCoveredByTransit(placed, transitBuildings);
                var coveredByLogistics = IsCoveredByService(placed, logisticsBuildings);
                var logisticsWeight = LogisticsWeightForBuilding(definition, buildingJobs);
                var coveredByWaste = IsCoveredByService(placed, wasteBuildings);
                var wasteWeight = WasteWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var coveredByDeathcare = IsCoveredByService(placed, deathcareBuildings);
                var deathcareWeight = DeathcareWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var coveredByCommunication = IsCoveredByService(placed, communicationBuildings);
                var communicationWeight = CommunicationWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var coveredByMail = IsCoveredByService(placed, mailBuildings);
                var mailWeight = MailWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var coveredByAdvancedEducation = IsCoveredByService(placed, advancedEducationBuildings);
                var advancedEducationWeight = AdvancedEducationWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var coveredByParking = IsCoveredByService(placed, parkingBuildings);
                var parkingWeight = ParkingWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var coveredBySafety = IsCoveredByService(placed, safetyBuildings);
                var coveredByFire = IsCoveredByService(placed, fireBuildings);
                var safetyWeight = SafetyWeightForBuilding(definition, buildingCapacity, buildingJobs);
                var fireRiskWeight = FireRiskForBuilding(definition, buildingCapacity, buildingJobs, level);
                var coveredBySecurity = IsCoveredByService(placed, securityBuildings);
                var securityWeight = SecurityWeightForBuilding(definition, buildingCapacity, buildingJobs);
                if (transitWeight > 0)
                {
                    transitEligible += transitWeight;
                    if (coveredByTransit)
                    {
                        transitCovered += transitWeight;
                    }
                }

                if (logisticsWeight > 0)
                {
                    logisticsEligible += logisticsWeight;
                    if (coveredByLogistics)
                    {
                        logisticsCovered += logisticsWeight;
                    }
                }

                if (safetyWeight > 0)
                {
                    safetyEligible += safetyWeight;
                    if (coveredBySafety)
                    {
                        safetyCovered += safetyWeight;
                    }
                    else
                    {
                        safetyRisk += SafetyRiskPenalty(safetyWeight);
                    }
                }

                if (fireRiskWeight > 0)
                {
                    fireLoad += fireRiskWeight;
                    if (coveredByFire)
                    {
                        fireProtected += fireRiskWeight;
                    }
                }

                if (securityWeight > 0)
                {
                    securityEligible += securityWeight;
                    if (coveredBySecurity)
                    {
                        securityCovered += securityWeight;
                    }
                }

                if (wasteWeight > 0)
                {
                    wasteEligible += wasteWeight;
                    if (coveredByWaste)
                    {
                        wasteCovered += wasteWeight;
                    }
                    else
                    {
                        wastePollution += WasteShortfallPollution(wasteWeight);
                    }
                }

                if (deathcareWeight > 0)
                {
                    deathcareEligible += deathcareWeight;
                    if (coveredByDeathcare)
                    {
                        deathcareCovered += deathcareWeight;
                    }
                }

                if (communicationWeight > 0)
                {
                    communicationEligible += communicationWeight;
                    if (coveredByCommunication)
                    {
                        communicationCovered += communicationWeight;
                    }
                }

                if (mailWeight > 0)
                {
                    mailEligible += mailWeight;
                    if (coveredByMail)
                    {
                        mailCovered += mailWeight;
                    }
                }

                if (advancedEducationWeight > 0)
                {
                    advancedEducationEligible += advancedEducationWeight;
                    if (coveredByAdvancedEducation)
                    {
                        advancedEducationCovered += advancedEducationWeight;
                    }
                }

                if (parkingWeight > 0)
                {
                    parkingEligible += parkingWeight;
                    if (coveredByParking)
                    {
                        parkingCovered += parkingWeight;
                    }
                }

                var effectivePollution = EffectivePollution(definition.Pollution);
                var effectiveNoise = EffectiveNoise(definition.Noise);
                pollution += effectivePollution;
                noise += effectiveNoise;
                buildingTax += (int)Math.Floor(LevelScaledTax(definition.TaxValue, level) * buildingEfficiency);
                if (coveredByLogistics && IsLogisticsSensitiveBuilding(definition))
                {
                    buildingTax += Math.Max(1, LevelScaledTax(definition.TaxValue, level) / 8);
                }

                if (coveredByMail && IsMailSensitiveBuilding(definition))
                {
                    buildingTax += Math.Max(1, LevelScaledTax(definition.TaxValue, level) / 12);
                }

                var traffic = Math.Max(0, definition.TrafficGeneration + buildingCapacity / 10 + buildingJobs / 8);
                if (coveredByLogistics && IsLogisticsSensitiveBuilding(definition))
                {
                    traffic = traffic * 78 / 100;
                }

                if (coveredByTransit)
                {
                    traffic = traffic * 72 / 100;
                }

                if (coveredByCommunication && IsCommunicationSensitiveBuilding(definition))
                {
                    traffic = traffic * 94 / 100;
                }

                if (coveredByMail && IsMailSensitiveBuilding(definition))
                {
                    traffic = traffic * 96 / 100;
                }

                if (coveredByParking && IsParkingSensitiveBuilding(definition))
                {
                    traffic = traffic * 92 / 100;
                }

                if (IsPolicyActive(CityPolicy.TransitPriority))
                {
                    traffic = traffic * 85 / 100;
                }

                if (IsPolicyActive(CityPolicy.CompleteStreets) && connected)
                {
                    traffic = traffic * 93 / 100;
                }

                if (connected)
                {
                    AddRoadLoad(placed.ConnectedRoadId, traffic);
                    roadLoad += traffic;
                }

                ApplyBuildingTilePressure(placed, definition, traffic, effectivePollution, effectiveNoise);
                if (!coveredByWaste && wasteWeight > 0)
                {
                    ApplyWasteShortfallPressure(placed, wasteWeight);
                }

                if (IsParkBuilding(definition))
                {
                    ApplyParkTileAccess(placed, definition);
                }

                if (IsHealthBuilding(definition))
                {
                    ApplyHealthTileAccess(placed, definition);
                }

                if (IsDeathcareBuilding(definition))
                {
                    ApplyDeathcareTileAccess(placed, definition);
                }

                if (IsShelterBuilding(definition))
                {
                    ApplyHealthTileAccess(placed, definition);
                    ApplySafetyTileAccess(placed, definition);
                }

                if (IsEducationBuilding(definition))
                {
                    ApplyEducationTileAccess(placed, definition);
                }

                if (IsSafetyBuilding(definition))
                {
                    ApplySafetyTileAccess(placed, definition);
                    ApplyFireProtectionTileAccess(placed, definition);
                }

                if (IsSecurityBuilding(definition))
                {
                    ApplySecurityTileAccess(placed, definition);
                }

                if (IsTransitBuilding(definition))
                {
                    ApplyTransitTileAccess(placed, definition);
                }

                if (IsLogisticsBuilding(definition))
                {
                    ApplyLogisticsTileAccess(placed, definition);
                }

                if (IsWasteBuilding(definition))
                {
                    ApplyWasteTileAccess(placed, definition);
                }

                if (IsCommunicationBuilding(definition))
                {
                    ApplyCommunicationTileAccess(placed, definition);
                }

                if (IsMailBuilding(definition))
                {
                    ApplyMailTileAccess(placed, definition);
                }

                if (IsRoadMaintenanceBuilding(definition))
                {
                    ApplyRoadMaintenanceTileAccess(placed, definition);
                }

                if (IsParkingBuilding(definition))
                {
                    ApplyParkingTileAccess(placed, definition);
                }

                if (IsStormwaterBuilding(definition))
                {
                    ApplyStormwaterTileAccess(placed, definition);
                }

                if (IsResidentialSensitiveBuilding(definition) && buildingCapacity > 0)
                {
                    var coveredByPark = IsCoveredByService(placed, parkBuildings);
                    var coveredByHealth = IsCoveredByService(placed, healthBuildings);
                    var coveredByEducation = IsCoveredByService(placed, educationBuildings);
                    var residentialServiceScore = ResidentialServiceScore(coveredByTransit, coveredByWaste, coveredBySafety, coveredBySecurity, coveredByCommunication, coveredByMail, coveredByPark, coveredByHealth, coveredByDeathcare, coveredByEducation);
                    residentialServiceScoreTotal += residentialServiceScore * buildingCapacity;
                    residentialServiceWeight += buildingCapacity;
                    underservedServiceWeight += Math.Max(0, 55 - residentialServiceScore) * buildingCapacity;
                    AddServiceGap(ref parkServiceGapWeight, !coveredByPark, buildingCapacity, 22);
                    AddServiceGap(ref healthServiceGapWeight, !coveredByHealth, buildingCapacity, 20);
                    AddServiceGap(ref educationServiceGapWeight, !coveredByEducation, buildingCapacity, 16);
                    AddServiceGap(ref transitServiceGapWeight, !coveredByTransit, buildingCapacity, 14);
                    AddServiceGap(ref safetyServiceGapWeight, !coveredBySafety, buildingCapacity, 10);
                    AddServiceGap(ref securityServiceGapWeight, !coveredBySecurity, buildingCapacity, 10);
                    AddServiceGap(ref wasteServiceGapWeight, !coveredByWaste, buildingCapacity, 8);
                    AddServiceGap(ref communicationServiceGapWeight, !coveredByCommunication, buildingCapacity, 6);
                    AddServiceGap(ref mailServiceGapWeight, !coveredByMail, buildingCapacity, 5);
                    AddServiceGap(ref deathcareServiceGapWeight, !coveredByDeathcare, buildingCapacity, 4);
                    residentialCapacity += buildingCapacity;
                    if (coveredByPark)
                    {
                        parkedResidentialCapacity += buildingCapacity;
                    }

                    if (coveredByHealth)
                    {
                        healthyResidentialCapacity += buildingCapacity;
                    }

                    if (coveredByEducation)
                    {
                        educatedResidentialCapacity += buildingCapacity;
                    }
                }
            }

            var roadCapacity = 0;
            for (var i = 0; i < roads.Count; i += 1)
            {
                roadCapacity += roads[i].Capacity;
                if (roads[i].NeighborCount <= 1)
                {
                    deadEndRoadTiles += 1;
                }

                if (roads[i].NeighborCount >= 3)
                {
                    intersectionRoadTiles += 1;
                }

                if (roads[i].Tier == RoadTier.Arterial)
                {
                    arterialRoadTiles += 1;
                }
            }

            var roadConnectivity = ComputeRoadConnectivity(roads.Count, deadEndRoadTiles, intersectionRoadTiles, arterialRoadTiles, connectedBuildings, buildings.Count);
            var roadMaintenanceCoverage = RoadMaintenanceCoverageForRoads(roadMaintenanceBuildings);
            var transitCapacity = TransitCapacityForBuildings(transitBuildings);
            var regionalConnectionCapacity = RegionalConnectionCapacityForBuildings(regionalConnectionBuildings);
            var transitUtilization = TransitUtilization(transitCovered, transitCapacity);
            var transitReliability = TransitReliability(transitCovered, transitCapacity);
            var transitOverloadRoadLoad = TransitOverloadRoadLoad(transitCovered, transitCapacity);
            roadLoad += transitOverloadRoadLoad;
            var logisticsCapacity = LogisticsCapacityForBuildings(logisticsBuildings);
            var logisticsUtilization = LogisticsUtilization(logisticsCovered, logisticsCapacity);
            var logisticsReliability = LogisticsReliability(logisticsCovered, logisticsCapacity);
            var logisticsOverloadRoadLoad = LogisticsOverloadRoadLoad(logisticsCovered, logisticsCapacity);
            roadLoad += logisticsOverloadRoadLoad;
            var parkingCapacity = ParkingCapacityForBuildings(parkingBuildings);
            var parkingUtilization = ParkingUtilization(parkingCovered, parkingCapacity);
            var parkingReliability = ParkingReliability(parkingCovered, parkingCapacity);
            var rawParkingCoverage = BudgetAdjustedCoverage(parkingEligible == 0 ? 0 : ClampToScore((int)Math.Round(parkingCovered * 100.0 / parkingEligible)));
            var parkingCoverage = ClampToScore(rawParkingCoverage * parkingReliability / 100);
            var congestion = roadCapacity == 0 ? 0 : ClampToScore((int)Math.Round(roadLoad * 100.0 / roadCapacity));
            congestion = PolicyAdjustedCongestion(congestion, intersectionRoadTiles, roadConnectivity, roads.Count);
            var intersectionDelay = PolicyAdjustedIntersectionDelay(ComputeIntersectionDelay(roads.Count, intersectionRoadTiles, deadEndRoadTiles, arterialRoadTiles, congestion, roadConnectivity), intersectionRoadTiles, roadConnectivity, roads.Count);
            var roadBottleneckPressure = ComputeRoadBottleneckPressure(congestion, roadConnectivity, deadEndRoadTiles, intersectionRoadTiles, arterialRoadTiles, intersectionDelay, roads.Count);
            congestion = ClampToScore(congestion + roadBottleneckPressure / 8);
            var parkCoverage = BudgetAdjustedCoverage(residentialCapacity == 0 ? 0 : ClampToScore((int)Math.Round(parkedResidentialCapacity * 100.0 / residentialCapacity)));
            var serviceLoad = PublicServiceLoad(Metrics.Population, safetyEligible, securityEligible);
            var serviceCapacity = PublicServiceCapacityForBuildings(healthBuildings, educationBuildings, safetyBuildings, securityBuildings, shelterBuildings);
            var serviceUtilization = ServiceUtilization(serviceLoad, serviceCapacity);
            var rawServiceReliability = ServiceReliability(serviceLoad, serviceCapacity);
            var maintenanceCondition = ComputeMaintenanceCondition(Metrics.Cash, ServiceBudgetPercent(), serviceUtilization, utilityUtilization, congestion, buildings.Count, roads.Count, roadMaintenanceCoverage);
            var serviceReliability = ApplyMaintenanceCondition(rawServiceReliability, maintenanceCondition);
            var healthCapacity = BudgetAdjustedServiceValue(HealthCapacityForBuildings(healthBuildings));
            var healthLoad = HealthcareLoad(Metrics.Population, jobs, pollution, noise);
            var healthUtilization = HealthUtilization(healthLoad, healthCapacity);
            var healthReliability = HealthReliability(healthLoad, healthCapacity);
            var healthCoverage = ApplyServiceReliability(BudgetAdjustedCoverage(residentialCapacity == 0 ? 0 : ClampToScore((int)Math.Round(healthyResidentialCapacity * 100.0 / residentialCapacity))), serviceReliability);
            healthCoverage = ClampToScore(healthCoverage * healthReliability / 100);
            var educationCapacity = BudgetAdjustedServiceValue(EducationCapacityForBuildings(educationBuildings));
            var educationLoad = EducationLoad(Metrics.Population, jobs, officeJobs, industrialJobs);
            var educationUtilization = EducationUtilization(educationLoad, educationCapacity);
            var educationReliability = EducationReliability(educationLoad, educationCapacity);
            var educationCoverage = ApplyServiceReliability(BudgetAdjustedCoverage(residentialCapacity == 0 ? 0 : ClampToScore((int)Math.Round(educatedResidentialCapacity * 100.0 / residentialCapacity))), serviceReliability);
            educationCoverage = ClampToScore(educationCoverage * educationReliability / 100);
            var deathcareCapacity = BudgetAdjustedServiceValue(DeathcareCapacityForBuildings(deathcareBuildings));
            var deathcareUtilization = DeathcareUtilization(deathcareEligible, deathcareCapacity);
            var deathcareReliability = DeathcareReliability(deathcareEligible, deathcareCapacity);
            var rawDeathcareCoverage = BudgetAdjustedCoverage(deathcareEligible == 0 ? 0 : ClampToScore((int)Math.Round(deathcareCovered * 100.0 / deathcareEligible)));
            var deathcareCoverage = ClampToScore(rawDeathcareCoverage * deathcareReliability / 100);
            var advancedEducationCoverage = ApplyServiceReliability(BudgetAdjustedCoverage(advancedEducationEligible == 0 ? 0 : ClampToScore((int)Math.Round(advancedEducationCovered * 100.0 / advancedEducationEligible))), serviceReliability);
            advancedEducationCoverage = ClampToScore(advancedEducationCoverage * educationReliability / 100);
            var studentBacklog = ComputeStudentBacklog(Metrics.Population, educationLoad, educationCapacity, educationCoverage, advancedEducationCoverage, educationUtilization);
            var learningPipeline = ComputeLearningPipeline(educationCoverage, advancedEducationCoverage, educationUtilization, studentBacklog, serviceReliability);
            var safetyCoverage = ApplyServiceReliability(BudgetAdjustedCoverage(safetyEligible == 0 ? 0 : ClampToScore((int)Math.Round(safetyCovered * 100.0 / safetyEligible))), serviceReliability);
            var securityCapacity = BudgetAdjustedServiceValue(SecurityCapacityForBuildings(securityBuildings));
            var securityUtilization = SecurityUtilization(securityEligible, securityCapacity);
            var securityReliability = SecurityReliability(securityEligible, securityCapacity);
            var rawSecurityCoverage = ApplyServiceReliability(BudgetAdjustedCoverage(securityEligible == 0 ? 0 : ClampToScore((int)Math.Round(securityCovered * 100.0 / securityEligible))), serviceReliability);
            var securityCoverage = ClampToScore(rawSecurityCoverage * securityReliability / 100);
            var fireCapacity = BudgetAdjustedServiceValue(FireCapacityForBuildings(fireBuildings));
            var fireUtilization = FireUtilization(fireLoad, fireCapacity);
            var fireProtectionBase = ApplyServiceReliability(BudgetAdjustedCoverage(fireLoad == 0 ? 70 : ClampToScore((int)Math.Round(fireProtected * 100.0 / fireLoad))), serviceReliability);
            var safetyServiceCoverage = Metrics.Population >= 200 ? safetyCoverage : 70;
            var securityServiceCoverage = Metrics.Population >= 220 ? securityCoverage : 70;
            var deathcareServiceCoverage = Metrics.Population >= 260 ? deathcareCoverage : 70;
            var serviceCoverage = ClampToScore((int)Math.Round(parkCoverage * 0.29 + healthCoverage * 0.20 + educationCoverage * 0.17 + deathcareServiceCoverage * 0.08 + safetyServiceCoverage * 0.13 + securityServiceCoverage * 0.13));
            var serviceEquity = ComputeServiceEquity(residentialServiceScoreTotal, residentialServiceWeight, serviceCoverage, serviceUtilization);
            var underservedResidents = ComputeUnderservedResidents(Metrics.Population, residentialCapacity, underservedServiceWeight);
            var serviceGapPressure = ComputeServiceGapPressure(residentialCapacity, parkServiceGapWeight, healthServiceGapWeight, educationServiceGapWeight, transitServiceGapWeight, safetyServiceGapWeight, securityServiceGapWeight, wasteServiceGapWeight, communicationServiceGapWeight, mailServiceGapWeight, deathcareServiceGapWeight);
            var serviceGapFocus = ServiceGapFocusLabel(parkServiceGapWeight, healthServiceGapWeight, educationServiceGapWeight, transitServiceGapWeight, safetyServiceGapWeight, securityServiceGapWeight, wasteServiceGapWeight, communicationServiceGapWeight, mailServiceGapWeight, deathcareServiceGapWeight);
            var serviceEquityPenalty = Metrics.Population >= 140 ? ServiceEquityPenalty(serviceEquity) : 0;
            var serviceEquityBonus = ServiceEquityBonus(serviceEquity);
            var rawTransitCoverage = BudgetAdjustedCoverage(transitEligible == 0 ? 0 : ClampToScore((int)Math.Round(transitCovered * 100.0 / transitEligible)));
            var transitCoverage = ClampToScore(rawTransitCoverage * transitReliability / 100);
            var transitWaitPressure = ComputeTransitWaitPressure(rawTransitCoverage, transitCoverage, transitUtilization, transitReliability, congestion, roadConnectivity, serviceReliability);
            var transitImpactWaitPressure = Metrics.Population >= 120 ? transitWaitPressure : 0;
            var rawLogisticsCoverage = BudgetAdjustedCoverage(logisticsEligible == 0 ? 0 : ClampToScore((int)Math.Round(logisticsCovered * 100.0 / logisticsEligible)));
            var logisticsCoverage = ClampToScore(rawLogisticsCoverage * logisticsReliability / 100);
            var communicationCapacity = BudgetAdjustedServiceValue(CommunicationCapacityForBuildings(communicationBuildings));
            var communicationUtilization = CommunicationUtilization(communicationCovered, communicationCapacity);
            var communicationReliability = CommunicationReliability(communicationCovered, communicationCapacity);
            var rawCommunicationCoverage = BudgetAdjustedCoverage(communicationEligible == 0 ? 0 : ClampToScore((int)Math.Round(communicationCovered * 100.0 / communicationEligible)));
            var communicationCoverage = ClampToScore(rawCommunicationCoverage * communicationReliability / 100);
            var mailCapacity = BudgetAdjustedServiceValue(MailCapacityForBuildings(mailBuildings));
            var mailUtilization = MailUtilization(mailCovered, mailCapacity);
            var mailReliability = MailReliability(mailCovered, mailCapacity);
            var rawMailCoverage = BudgetAdjustedCoverage(mailEligible == 0 ? 0 : ClampToScore((int)Math.Round(mailCovered * 100.0 / mailEligible)));
            var mailCoverage = ClampToScore(rawMailCoverage * mailReliability / 100);
            var wasteLoad = Math.Max(0, Metrics.Population / 5 + jobs / 8 + wasteEligible / 3);
            var wasteUtilization = WasteUtilization(wasteLoad, wasteCapacity);
            var wasteReliability = WasteReliability(wasteLoad, wasteCapacity);
            var wasteDistanceCoverage = BudgetAdjustedCoverage(wasteEligible == 0 ? 0 : ClampToScore((int)Math.Round(wasteCovered * 100.0 / wasteEligible)));
            var wasteCoverage = wasteEligible == 0 ? 0 : Math.Min(wasteDistanceCoverage, wasteReliability);
            var wasteCapacityShortfall = Math.Max(0, wasteLoad - wasteCapacity);
            pollution += wastePollution + wasteCapacityShortfall / 18;
            noise += wasteCapacityShortfall / 30;
            var wastewaterLoad = WastewaterLoad(Metrics.Population, jobs, industrialJobs, waterDemand);
            var wastewaterUtilization = WastewaterUtilization(wastewaterLoad, wastewaterCapacity);
            var wastewaterReliability = WastewaterReliability(wastewaterLoad, wastewaterCapacity);
            var wastewaterShortfall = Math.Max(0, wastewaterLoad - wastewaterCapacity);
            pollution += WastewaterShortfallPollution(wastewaterShortfall);
            noise += wastewaterShortfall / 36;
            var maintenanceShortfallPenalty = Metrics.Population >= 160 ? Math.Max(0, 60 - maintenanceCondition) : 0;
            var employable = (int)Math.Round(Metrics.Population * 0.52);
            var employment = Math.Min(jobs, employable);
            var unemployment = employable == 0 ? 0 : ClampToScore((int)Math.Round((employable - employment) * 100.0 / employable));
            var landValue = AverageLandValue();
            var residentialZoneTiles = Grid.CountZoneTiles(ZoneType.Residential);
            var commercialZoneTiles = Grid.CountZoneTiles(ZoneType.Commercial);
            var industrialZoneTiles = Grid.CountZoneTiles(ZoneType.Industrial);
            var officeZoneTiles = Grid.CountZoneTiles(ZoneType.Office);
            var mixedUseZoneTiles = Grid.CountZoneTiles(ZoneType.MixedUse);
            var utilityZoneTiles = Grid.CountZoneTiles(ZoneType.Utility);
            var civicZoneTiles = Grid.CountZoneTiles(ZoneType.Civic);
            var growthZoneTiles = residentialZoneTiles + commercialZoneTiles + industrialZoneTiles + officeZoneTiles + mixedUseZoneTiles;
            var landUseEfficiency = ComputeLandUseEfficiency(developedZoneTiles, growthZoneTiles);
            var idleZoneTiles = Math.Max(0, growthZoneTiles - developedZoneTiles);
            var stormwaterRawLoad = StormwaterLoad(Metrics.Population, jobs, roads.Count, developedZoneTiles, industrialJobs, buildings.Count, StormwaterTerrainExposure());
            var stormwaterLoad = PolicyAdjustedStormwaterLoad(stormwaterRawLoad, parkCoverage);
            var stormwaterUtilization = StormwaterUtilization(stormwaterLoad, stormwaterCapacity);
            var stormwaterResilience = StormwaterResilience(stormwaterLoad, stormwaterCapacity, parkCoverage);
            var floodRisk = ComputeFloodRisk(stormwaterUtilization, stormwaterResilience, roads.Count, developedZoneTiles, parkCoverage, landUseEfficiency);
            var stormwaterShortfall = Math.Max(0, stormwaterLoad - stormwaterCapacity);
            pollution += StormwaterShortfallPollution(stormwaterShortfall, floodRisk);
            var idleZonePenalty = Metrics.Population >= 160 ? IdleZonePenalty(landUseEfficiency, idleZoneTiles) : 0;
            var compactLandUseBonus = CompactLandUseBonus(landUseEfficiency);
            var developmentQuality = ComputeDevelopmentQuality();
            var developmentQualityBonus = DevelopmentQualityBonus(developmentQuality);
            var developmentQualityPenalty = Metrics.Population >= 120 ? DevelopmentQualityPenalty(developmentQuality) : 0;
            var landUseConflict = ComputeLandUseConflict();
            var landUseConflictPenalty = Metrics.Population >= 120 ? LandUseConflictPenalty(landUseConflict) : 0;
            var landUseBufferBonus = LandUseBufferBonus(landUseConflict);
            var wasteShortfallPenalty = Metrics.Population >= 220 ? Math.Max(0, 55 - wasteCoverage) : 0;
            var wasteDemandBonus = Metrics.Population >= 220 ? wasteCoverage : 0;
            var disconnectedBuildings = buildings.Count - connectedBuildings;
            var emergencyResponse = ComputeEmergencyResponse(healthCoverage, safetyCoverage, securityCoverage, serviceReliability, roadConnectivity, congestion, deadEndRoadTiles, serviceUtilization, connectedBuildings, disconnectedBuildings);
            var medicalResponse = ComputeMedicalResponse(healthCoverage, emergencyResponse, roadConnectivity, congestion, healthUtilization, serviceReliability);
            var fireResponse = ComputeFireResponse(safetyCoverage, emergencyResponse, roadConnectivity, congestion, fireUtilization);
            var fireProtection = ClampToScore((fireProtectionBase * 2 + fireResponse) / 3 - Math.Max(0, fireUtilization - 100) / 4);
            var fireRisk = ComputeFireRisk(fireLoad, fireProtection, fireResponse, fireUtilization, pollution, congestion, maintenanceCondition, Metrics.Population, industrialJobs);
            var policeResponse = ComputePoliceResponse(securityCoverage, emergencyResponse, roadConnectivity, congestion, securityUtilization, serviceReliability);
            var responseShortfallPenalty = Metrics.Population >= 180 ? Math.Max(0, 55 - emergencyResponse) : 0;
            var safetyShortfallPenalty = Metrics.Population >= 200 ? Math.Max(0, 55 - safetyCoverage) + Math.Max(0, 55 - fireProtection) / 2 + safetyRisk / 3 + fireRisk / 4 + responseShortfallPenalty / 4 : 0;
            var logisticsDemandBonus = jobs >= 120 ? logisticsCoverage : 0;
            var transitOverloadPenalty = Metrics.Population >= 180 ? Math.Max(0, transitUtilization - 100) : 0;
            var rentPressure = ComputeRentPressure(housing, landValue, serviceCoverage, transitCoverage);
            var rentHappinessPenalty = RentHappinessPenalty(rentPressure);
            var rentGrowthPenalty = RentGrowthPenalty(rentPressure);
            var rentHousingDemand = Metrics.Population >= 160 ? Math.Max(0, rentPressure - 55) / 4 : 0;
            var jobsHousingBalance = ComputeJobsHousingBalance(employable, jobs);
            var regionalConnectivity = ComputeRegionalConnectivity(regionalConnectionCapacity, Metrics.Population, jobs);
            var commuteEfficiency = ComputeCommuteEfficiency(transitCoverage, regionalConnectivity, congestion, jobsHousingBalance, mixedUseBuildings, arterialRoadTiles, roadConnectivity, connectedBuildings, disconnectedBuildings);
            commuteEfficiency = ClampToScore(commuteEfficiency - transitImpactWaitPressure / 4 - roadBottleneckPressure / 5);
            var carDependency = ComputeCarDependency(commuteEfficiency, transitCoverage, regionalConnectivity, mixedUseBuildings, congestion, jobsHousingBalance);
            carDependency = PolicyAdjustedCarDependency(carDependency, transitCoverage, mixedUseBuildings, roadConnectivity);
            var parkingPressure = ComputeParkingPressure(Metrics.Population, jobs, commercialGoodsJobs, officeJobs, attractionParkingDemand, carDependency, transitCoverage, roadConnectivity, mixedUseBuildings, roads.Count, arterialRoadTiles, landUseEfficiency, congestion, parkingCapacity, parkingCoverage);
            parkingPressure = PolicyAdjustedParkingPressure(parkingPressure, roadConnectivity, transitCoverage, parkingCoverage);
            var parkingSearchRoadLoad = ParkingSearchRoadLoad(Metrics.Population, jobs, parkingPressure, carDependency);
            if (parkingSearchRoadLoad > 0)
            {
                roadLoad += parkingSearchRoadLoad;
                congestion = roadCapacity == 0 ? 0 : ClampToScore((int)Math.Round(roadLoad * 100.0 / roadCapacity));
                congestion = PolicyAdjustedCongestion(congestion, intersectionRoadTiles, roadConnectivity, roads.Count);
                intersectionDelay = PolicyAdjustedIntersectionDelay(ComputeIntersectionDelay(roads.Count, intersectionRoadTiles, deadEndRoadTiles, arterialRoadTiles, congestion, roadConnectivity), intersectionRoadTiles, roadConnectivity, roads.Count);
                roadBottleneckPressure = ComputeRoadBottleneckPressure(congestion, roadConnectivity, deadEndRoadTiles, intersectionRoadTiles, arterialRoadTiles, intersectionDelay, roads.Count);
                congestion = ClampToScore(congestion + roadBottleneckPressure / 8);
                transitWaitPressure = ComputeTransitWaitPressure(rawTransitCoverage, transitCoverage, transitUtilization, transitReliability, congestion, roadConnectivity, serviceReliability);
                transitImpactWaitPressure = Metrics.Population >= 120 ? transitWaitPressure : 0;
                commuteEfficiency = ComputeCommuteEfficiency(transitCoverage, regionalConnectivity, congestion, jobsHousingBalance, mixedUseBuildings, arterialRoadTiles, roadConnectivity, connectedBuildings, disconnectedBuildings);
                commuteEfficiency = ClampToScore(commuteEfficiency - transitImpactWaitPressure / 4 - roadBottleneckPressure / 5);
                carDependency = ComputeCarDependency(commuteEfficiency, transitCoverage, regionalConnectivity, mixedUseBuildings, congestion, jobsHousingBalance);
                carDependency = PolicyAdjustedCarDependency(carDependency, transitCoverage, mixedUseBuildings, roadConnectivity);
                parkingPressure = ComputeParkingPressure(Metrics.Population, jobs, commercialGoodsJobs, officeJobs, attractionParkingDemand, carDependency, transitCoverage, roadConnectivity, mixedUseBuildings, roads.Count, arterialRoadTiles, landUseEfficiency, congestion, parkingCapacity, parkingCoverage);
                parkingPressure = PolicyAdjustedParkingPressure(parkingPressure, roadConnectivity, transitCoverage, parkingCoverage);
            }

            var parkingHappinessPenalty = 0;
            var parkingAccessBonus = 0;
            var parkingAccessPenalty = 0;
            var walkability = ClampToScore(ComputeWalkability(roadConnectivity, transitCoverage, serviceCoverage, parkCoverage, landUseEfficiency, mixedUseBuildings, carDependency, congestion, deadEndRoadTiles, connectedBuildings) + PolicyWalkabilityBonus(roadConnectivity, transitCoverage, mixedUseBuildings));
            var accidentRisk = ComputeAccidentRisk(congestion, roadConnectivity, deadEndRoadTiles, intersectionRoadTiles, arterialRoadTiles, roadMaintenanceCoverage, maintenanceCondition, emergencyResponse, walkability, roads.Count);
            accidentRisk = ClampToScore(accidentRisk - PolicyAccidentRiskRelief(roadMaintenanceCoverage, emergencyResponse, intersectionRoadTiles, roadConnectivity));
            var accidentRoadLoad = AccidentRoadLoad(roadLoad, accidentRisk);
            if (accidentRoadLoad > 0)
            {
                roadLoad += accidentRoadLoad;
                congestion = roadCapacity == 0 ? 0 : ClampToScore((int)Math.Round(roadLoad * 100.0 / roadCapacity));
                congestion = PolicyAdjustedCongestion(congestion, intersectionRoadTiles, roadConnectivity, roads.Count);
                intersectionDelay = PolicyAdjustedIntersectionDelay(ComputeIntersectionDelay(roads.Count, intersectionRoadTiles, deadEndRoadTiles, arterialRoadTiles, congestion, roadConnectivity), intersectionRoadTiles, roadConnectivity, roads.Count);
                roadBottleneckPressure = ComputeRoadBottleneckPressure(congestion, roadConnectivity, deadEndRoadTiles, intersectionRoadTiles, arterialRoadTiles, intersectionDelay, roads.Count);
                congestion = ClampToScore(congestion + roadBottleneckPressure / 8);
                transitWaitPressure = ComputeTransitWaitPressure(rawTransitCoverage, transitCoverage, transitUtilization, transitReliability, congestion, roadConnectivity, serviceReliability);
                transitImpactWaitPressure = Metrics.Population >= 120 ? transitWaitPressure : 0;
                commuteEfficiency = ComputeCommuteEfficiency(transitCoverage, regionalConnectivity, congestion, jobsHousingBalance, mixedUseBuildings, arterialRoadTiles, roadConnectivity, connectedBuildings, disconnectedBuildings);
                commuteEfficiency = ClampToScore(commuteEfficiency - transitImpactWaitPressure / 4 - roadBottleneckPressure / 5);
                carDependency = ComputeCarDependency(commuteEfficiency, transitCoverage, regionalConnectivity, mixedUseBuildings, congestion, jobsHousingBalance);
                carDependency = PolicyAdjustedCarDependency(carDependency, transitCoverage, mixedUseBuildings, roadConnectivity);
                parkingPressure = ComputeParkingPressure(Metrics.Population, jobs, commercialGoodsJobs, officeJobs, attractionParkingDemand, carDependency, transitCoverage, roadConnectivity, mixedUseBuildings, roads.Count, arterialRoadTiles, landUseEfficiency, congestion, parkingCapacity, parkingCoverage);
                parkingPressure = PolicyAdjustedParkingPressure(parkingPressure, roadConnectivity, transitCoverage, parkingCoverage);
                walkability = ClampToScore(ComputeWalkability(roadConnectivity, transitCoverage, serviceCoverage, parkCoverage, landUseEfficiency, mixedUseBuildings, carDependency, congestion, deadEndRoadTiles, connectedBuildings) + PolicyWalkabilityBonus(roadConnectivity, transitCoverage, mixedUseBuildings));
                accidentRisk = ComputeAccidentRisk(congestion, roadConnectivity, deadEndRoadTiles, intersectionRoadTiles, arterialRoadTiles, roadMaintenanceCoverage, maintenanceCondition, emergencyResponse, walkability, roads.Count);
                accidentRisk = ClampToScore(accidentRisk - PolicyAccidentRiskRelief(roadMaintenanceCoverage, emergencyResponse, intersectionRoadTiles, roadConnectivity));
            }

            var roadSafety = ClampToScore(ComputeRoadSafety(accidentRisk, roadMaintenanceCoverage, roadConnectivity, emergencyResponse, walkability) + PolicyRoadSafetyBonus());
            parkingHappinessPenalty = Metrics.Population >= 160 ? ParkingHappinessPenalty(parkingPressure) : 0;
            parkingAccessBonus = ParkingAccessBonus(parkingPressure);
            parkingAccessPenalty = ParkingAccessPenalty(parkingPressure);
            var caseBacklog = ComputeCaseBacklog(Metrics.Population, securityEligible, securityCapacity, securityCoverage, securityUtilization, policeResponse, unemployment, rentPressure);
            var crimePressure = ComputeCrimePressure(securityCoverage, unemployment, rentPressure, congestion, securityEligible, policeResponse, securityUtilization, caseBacklog);
            var crimeHappinessPenalty = CrimeHappinessPenalty(crimePressure);
            var workforceSkill = ComputeWorkforceSkill(Metrics.Population, employment, educationCoverage, advancedEducationCoverage, officeJobs, upgradedBuildings, landValue, crimePressure, pollution, innovationBase);
            workforceSkill = ClampToScore(workforceSkill + learningPipeline / 12 - studentBacklog / 8 - Math.Max(0, educationUtilization - 115) / 10);
            var laborShortage = ComputeLaborShortage(jobs, employable, workforceSkill);
            var innovationCapacity = ComputeInnovationCapacity(innovationBase, advancedEducationCoverage, communicationCoverage, communicationUtilization, workforceSkill, utilityReliability, officeJobs);
            var businessEfficiency = ComputeBusinessEfficiency(communicationCoverage, communicationUtilization, mailCoverage, mailUtilization, utilityReliability, workforceSkill, logisticsCoverage, commuteEfficiency, congestion, innovationCapacity);
            var productivityBonus = ComputeProductivityBonus(employment, workforceSkill, advancedEducationCoverage, logisticsCoverage, officeJobs, businessEfficiency, innovationCapacity);
            var commuteHappinessPenalty = Metrics.Population >= 120 ? CommuteHappinessPenalty(commuteEfficiency, carDependency) : 0;
            var environmentQuality = ComputeEnvironmentQuality(pollution, noise, parkCoverage, wasteCoverage, transitCoverage, carDependency, wastewaterReliability, stormwaterResilience, floodRisk);
            var noiseStress = ComputeNoiseStress(noise, congestion, carDependency, transitCoverage, parkCoverage);
            var environmentHappinessPenalty = Metrics.Population >= 100 ? EnvironmentHappinessPenalty(environmentQuality, noiseStress) : 0;
            var disasterPreparedness = ComputeDisasterPreparedness(DisasterPreparednessCapacityForBuildings(shelterBuildings), Metrics.Population, emergencyResponse, stormwaterResilience, utilityReliability, roadConnectivity, maintenanceCondition);
            var publicHealthBase = ClampToScore(ComputePublicHealth(healthCoverage, emergencyResponse, environmentQuality, wasteCoverage, utilityEfficiency, pollution, noiseStress, wastewaterReliability, stormwaterResilience, floodRisk) + disasterPreparedness / 16);
            var healthRiskBase = ClampToScore(ComputeHealthRisk(publicHealthBase, emergencyResponse, pollution, noiseStress, utilityEfficiency, wastewaterReliability, wastewaterUtilization, stormwaterResilience, floodRisk) - disasterPreparedness / 10);
            var patientBacklog = ComputePatientBacklog(Metrics.Population, healthLoad, healthCapacity, healthCoverage, healthUtilization, medicalResponse, publicHealthBase, healthRiskBase);
            var mortalityPressure = ComputeMortalityPressure(Metrics.Population, deathcareCoverage, deathcareUtilization, publicHealthBase, healthRiskBase, disasterPreparedness);
            var publicHealth = ClampToScore(publicHealthBase + medicalResponse / 20 + deathcareCoverage / 18 - patientBacklog / 4 - mortalityPressure / 4);
            var healthRisk = ClampToScore(healthRiskBase + patientBacklog / 4 + mortalityPressure / 5 - medicalResponse / 25 - deathcareCoverage / 20);
            var disasterRisk = ComputeDisasterRisk(disasterPreparedness, floodRisk, healthRisk, accidentRisk, fireRisk, utilityReliability, wastewaterReliability, congestion);
            var healthHappinessPenalty = Metrics.Population >= 140 ? HealthHappinessPenalty(healthRisk) + DisasterRiskHappinessPenalty(disasterRisk) : 0;
            var livingCondition = ComputeLivingCondition(Metrics.Population, serviceCoverage, serviceEquity, parkCoverage, educationCoverage, deathcareCoverage, transitCoverage, transitImpactWaitPressure, commuteEfficiency, walkability, rentPressure, crimePressure, environmentQuality, publicHealth, healthRisk, noiseStress, roadBottleneckPressure, parkingPressure, utilityReliability);
            var livingPressure = ComputeLivingPressure(livingCondition, rentPressure, crimePressure, healthRisk, noiseStress, roadBottleneckPressure, transitImpactWaitPressure, serviceEquity);
            var livingConditionPenalty = Metrics.Population >= 160 ? LivingConditionPenalty(livingCondition, livingPressure) : 0;
            var livingConditionBonus = LivingConditionBonus(livingCondition, livingPressure);
            var attractiveness = ClampToScore(ComputeAttractiveness(AttractionScoreForBuildings(attractionBuildings), serviceCoverage, parkCoverage, transitCoverage, regionalConnectivity, securityCoverage, mailCoverage, landValue, pollution, congestion, crimePressure, mixedUseBuildings) - parkingAccessPenalty);
            var visitors = ComputeVisitors(attractiveness, Metrics.Population, jobs, landmarkBuildings, regionalConnectivity);
            var tourismIncome = visitors * 2 + LandmarkTourismIncomeForBuildings(attractionBuildings) + RegionalTourismBonus(regionalConnectivity);
            var goodsDemand = ComputeGoodsDemand(Metrics.Population, commercialGoodsJobs, visitors, mixedUseBuildings);
            var resourcePotential = ResourcePotentialForBuildings(resourceBuildings);
            var resourceSpecialization = ComputeResourceSpecialization(resourcePotential, logisticsCoverage, utilityReliability, workforceSkill);
            var industrialSpecialization = ComputeIndustrialSpecialization(resourceSpecialization, logisticsCoverage, industrialZoneTiles, industrialJobs);
            var localGoodsSupply = ComputeLocalGoodsSupply(resourceBuildings, logisticsCoverage, utilityReliability, workforceSkill, resourceSpecialization);
            var freightImportSupply = ComputeFreightImportSupply(freightRailBuildings, logisticsCoverage, logisticsUtilization, utilityReliability);
            var goodsStorage = ComputeGoodsStorage(warehouseBuildings, logisticsCoverage, logisticsUtilization, utilityReliability);
            var rawGoodsSupply = ComputeGoodsSupply(industrialJobs, logisticsCoverage, workforceSkill, logisticsUtilization, regionalConnectivity, localGoodsSupply, freightImportSupply);
            var supplyChainStability = ComputeSupplyChainStability(goodsStorage, rawGoodsSupply, goodsDemand, logisticsCoverage, logisticsUtilization);
            var goodsSupply = ApplyGoodsStorageBuffer(rawGoodsSupply, goodsDemand, goodsStorage, supplyChainStability);
            var goodsBalance = ComputeGoodsBalance(goodsSupply, goodsDemand);
            var goodsShortagePenalty = GoodsShortagePenalty(goodsBalance, goodsDemand);
            var goodsMarketBonus = GoodsMarketBonus(goodsBalance, goodsDemand);

            Metrics.HousingCapacity = housing;
            Metrics.Jobs = jobs;
            Metrics.OfficeJobs = officeJobs;
            Metrics.PowerSupply = powerSupply;
            Metrics.PowerDemand = powerDemand;
            Metrics.WaterSupply = waterSupply;
            Metrics.WaterDemand = waterDemand;
            Metrics.UtilityLoad = utilityLoad;
            Metrics.UtilityCapacity = utilityCapacity;
            Metrics.UtilityUtilization = utilityUtilization;
            Metrics.UtilityReliability = utilityReliability;
            Metrics.WastewaterLoad = wastewaterLoad;
            Metrics.WastewaterCapacity = wastewaterCapacity;
            Metrics.WastewaterUtilization = wastewaterUtilization;
            Metrics.WastewaterReliability = wastewaterReliability;
            Metrics.StormwaterLoad = stormwaterLoad;
            Metrics.StormwaterCapacity = stormwaterCapacity;
            Metrics.StormwaterUtilization = stormwaterUtilization;
            Metrics.StormwaterResilience = stormwaterResilience;
            Metrics.FloodRisk = floodRisk;
            Metrics.Congestion = congestion;
            Metrics.Pollution = pollution;
            Metrics.Noise = noise;
            Metrics.ServiceCoverage = serviceCoverage;
            Metrics.ServiceLoad = serviceLoad;
            Metrics.ServiceCapacity = serviceCapacity;
            Metrics.ServiceUtilization = serviceUtilization;
            Metrics.ServiceEquity = serviceEquity;
            Metrics.UnderservedResidents = underservedResidents;
            Metrics.ServiceGapPressure = serviceGapPressure;
            Metrics.ServiceGapFocus = serviceGapFocus;
            Metrics.MaintenanceCondition = maintenanceCondition;
            Metrics.ParkCoverage = parkCoverage;
            Metrics.HealthCoverage = healthCoverage;
            Metrics.HealthLoad = healthLoad;
            Metrics.HealthCapacity = healthCapacity;
            Metrics.HealthUtilization = healthUtilization;
            Metrics.MedicalResponse = medicalResponse;
            Metrics.PatientBacklog = patientBacklog;
            Metrics.DeathcareCoverage = deathcareCoverage;
            Metrics.DeathcareLoad = deathcareEligible;
            Metrics.DeathcareCapacity = deathcareCapacity;
            Metrics.DeathcareUtilization = deathcareUtilization;
            Metrics.MortalityPressure = mortalityPressure;
            Metrics.EducationCoverage = educationCoverage;
            Metrics.AdvancedEducationCoverage = advancedEducationCoverage;
            Metrics.EducationLoad = educationLoad;
            Metrics.EducationCapacity = educationCapacity;
            Metrics.EducationUtilization = educationUtilization;
            Metrics.StudentBacklog = studentBacklog;
            Metrics.LearningPipeline = learningPipeline;
            Metrics.SafetyCoverage = safetyCoverage;
            Metrics.FireProtection = fireProtection;
            Metrics.FireLoad = fireLoad;
            Metrics.FireCapacity = fireCapacity;
            Metrics.FireUtilization = fireUtilization;
            Metrics.FireRisk = fireRisk;
            Metrics.FireResponse = fireResponse;
            Metrics.SecurityCoverage = securityCoverage;
            Metrics.SecurityLoad = securityEligible;
            Metrics.SecurityCapacity = securityCapacity;
            Metrics.SecurityUtilization = securityUtilization;
            Metrics.PoliceResponse = policeResponse;
            Metrics.CaseBacklog = caseBacklog;
            Metrics.TransitCoverage = transitCoverage;
            Metrics.TransitLoad = transitCovered;
            Metrics.TransitCapacity = transitCapacity;
            Metrics.TransitUtilization = transitUtilization;
            Metrics.TransitReliability = transitReliability;
            Metrics.TransitWaitPressure = transitWaitPressure;
            Metrics.LogisticsCoverage = logisticsCoverage;
            Metrics.LogisticsLoad = logisticsCovered;
            Metrics.LogisticsCapacity = logisticsCapacity;
            Metrics.LogisticsUtilization = logisticsUtilization;
            Metrics.WasteCoverage = wasteCoverage;
            Metrics.WasteLoad = wasteLoad;
            Metrics.WasteCapacity = wasteCapacity;
            Metrics.WasteUtilization = wasteUtilization;
            Metrics.WasteReliability = wasteReliability;
            Metrics.CommunicationCoverage = communicationCoverage;
            Metrics.CommunicationLoad = communicationCovered;
            Metrics.CommunicationCapacity = communicationCapacity;
            Metrics.CommunicationUtilization = communicationUtilization;
            Metrics.BusinessEfficiency = businessEfficiency;
            Metrics.MailCoverage = mailCoverage;
            Metrics.MailLoad = mailCovered;
            Metrics.MailCapacity = mailCapacity;
            Metrics.MailUtilization = mailUtilization;
            Metrics.MailReliability = mailReliability;
            Metrics.EmergencyResponse = emergencyResponse;
            Metrics.DisasterPreparedness = disasterPreparedness;
            Metrics.DisasterRisk = disasterRisk;
            Metrics.CrimePressure = crimePressure;
            Metrics.Attractiveness = attractiveness;
            Metrics.Visitors = visitors;
            Metrics.TourismIncome = tourismIncome;
            Metrics.RegionalConnectivity = regionalConnectivity;
            Metrics.GoodsSupply = goodsSupply;
            Metrics.LocalGoodsSupply = localGoodsSupply;
            Metrics.FreightImportSupply = freightImportSupply;
            Metrics.GoodsStorage = goodsStorage;
            Metrics.SupplyChainStability = supplyChainStability;
            Metrics.GoodsDemand = goodsDemand;
            Metrics.GoodsBalance = goodsBalance;
            Metrics.ResourcePotential = resourcePotential;
            Metrics.ResourceSpecialization = resourceSpecialization;
            Metrics.IndustrialSpecialization = industrialSpecialization;
            Metrics.WorkforceSkill = workforceSkill;
            Metrics.LaborShortage = laborShortage;
            Metrics.ProductivityBonus = productivityBonus;
            Metrics.InnovationCapacity = innovationCapacity;
            Metrics.JobsHousingBalance = jobsHousingBalance;
            Metrics.CommuteEfficiency = commuteEfficiency;
            Metrics.CarDependency = carDependency;
            Metrics.ParkingPressure = parkingPressure;
            Metrics.ParkingCoverage = parkingCoverage;
            Metrics.ParkingLoad = parkingCovered;
            Metrics.ParkingCapacity = parkingCapacity;
            Metrics.ParkingUtilization = parkingUtilization;
            Metrics.Walkability = walkability;
            Metrics.EnvironmentQuality = environmentQuality;
            Metrics.NoiseStress = noiseStress;
            Metrics.PublicHealth = publicHealth;
            Metrics.HealthRisk = healthRisk;
            Metrics.RoadTiles = roads.Count;
            Metrics.ArterialRoadTiles = arterialRoadTiles;
            Metrics.RoadCapacity = roadCapacity;
            Metrics.RoadLoad = roadLoad;
            Metrics.RoadConnectivity = roadConnectivity;
            Metrics.IntersectionDelay = intersectionDelay;
            Metrics.RoadBottleneckPressure = roadBottleneckPressure;
            Metrics.DeadEndRoadTiles = deadEndRoadTiles;
            Metrics.IntersectionRoadTiles = intersectionRoadTiles;
            Metrics.RoadMaintenanceCoverage = roadMaintenanceCoverage;
            Metrics.AccidentRisk = accidentRisk;
            Metrics.RoadSafety = roadSafety;
            Metrics.BuildingCount = buildings.Count;
            Metrics.ZonedDevelopmentBuildings = zonedDevelopmentBuildings;
            Metrics.HighDensityResidentialBuildings = highDensityResidentialBuildings;
            Metrics.DevelopedZoneTiles = developedZoneTiles;
            Metrics.LandUseEfficiency = landUseEfficiency;
            Metrics.IdleZoneTiles = idleZoneTiles;
            Metrics.DevelopmentQuality = developmentQuality;
            Metrics.LandUseConflict = landUseConflict;
            Metrics.MixedUseBuildings = mixedUseBuildings;
            Metrics.LandmarkBuildings = landmarkBuildings;
            Metrics.UpgradedBuildings = upgradedBuildings;
            Metrics.MaxBuildingLevel = maxBuildingLevel;
            Metrics.ConnectedBuildings = connectedBuildings;
            Metrics.DisconnectedBuildings = disconnectedBuildings;
            Metrics.AverageLandValue = landValue;
            Metrics.Employment = employment;
            Metrics.Unemployment = unemployment;
            Metrics.ResidentialZoneTiles = residentialZoneTiles;
            Metrics.CommercialZoneTiles = commercialZoneTiles;
            Metrics.IndustrialZoneTiles = industrialZoneTiles;
            Metrics.OfficeZoneTiles = officeZoneTiles;
            Metrics.MixedUseZoneTiles = mixedUseZoneTiles;
            Metrics.UtilityZoneTiles = utilityZoneTiles;
            Metrics.ZonedTiles = growthZoneTiles + utilityZoneTiles + civicZoneTiles;
            Metrics.RentPressure = rentPressure;
            Metrics.LivingCondition = livingCondition;
            Metrics.LivingPressure = livingPressure;
            Metrics.AdministrationEfficiency = administrationEfficiency;
            Metrics.AdministrationLoad = administrationLoad;
            Metrics.AdministrationCapacity = administrationCapacity;
            Metrics.AdministrationUtilization = administrationUtilization;
            Metrics.PolicyBacklog = policyBacklog;
            Metrics.PolicyExpense = PolicyMonthlyExpense(administrationEfficiency, policyBacklog);
            Metrics.BondPrincipal = bondPrincipal;
            Metrics.BondPayment = ComputeBondPayment();
            Metrics.ServiceBudgetLevel = serviceBudgetLevel;
            Metrics.ServiceBudgetPercent = ServiceBudgetPercent();
            Metrics.ServiceBudgetExpense = serviceBudgetExpense;
            Metrics.UpkeepExpense = upkeep;
            Metrics.RoadExpense = TotalRoadUpkeep();
            Metrics.TaxLevel = taxLevel;
            Metrics.TaxRatePercent = TaxRatePercent();
            var baseTaxIncome = Metrics.Population * config.ResidentTaxPerPerson + employment * config.JobTaxPerWorker + buildingTax + EducationTaxBonus(employment, educationCoverage) + productivityBonus + BusinessEfficiencyTaxBonus(employment, businessEfficiency) + InnovationTaxBonus(employment, innovationCapacity) + IndustrialSpecializationTaxBonus(industrialJobs, industrialSpecialization) + AdministrationTaxBonus(employment, buildingTax, administrationEfficiency) + tourismIncome + goodsMarketBonus * 2;

            // 幸福度奖励加成
            var happinessBonus = HappinessRewardSystem.GetTaxBonus(Metrics.Happiness);
            baseTaxIncome = (int)(baseTaxIncome * (1f + happinessBonus));

            Metrics.TaxIncome = baseTaxIncome * Metrics.TaxRatePercent / 100;
            Metrics.NetIncome = Metrics.TaxIncome - Metrics.UpkeepExpense - Metrics.RoadExpense - Metrics.PolicyExpense - Metrics.BondPayment;
            var municipalExpense = Math.Max(1, Metrics.UpkeepExpense + Metrics.RoadExpense + Math.Max(0, Metrics.PolicyExpense) + Metrics.BondPayment);
            var debtPressure = ComputeDebtPressure(Metrics.Cash, Metrics.NetIncome, municipalExpense, Metrics.Population, bondPrincipal);
            var fiscalHealth = ClampToScore(ComputeFiscalHealth(Metrics.Cash, Metrics.NetIncome, municipalExpense, debtPressure) + AdministrationFiscalBonus(administrationEfficiency));
            Metrics.DebtPressure = debtPressure;
            Metrics.FiscalHealth = fiscalHealth;
            Metrics.Happiness = ClampToScore(ComputeHappiness(serviceCoverage, parkCoverage, healthCoverage, educationCoverage, safetyCoverage, transitCoverage, wasteCoverage, safetyRisk, utilityEfficiency, congestion, pollution, unemployment, landValue) + PolicyHappinessBonus() + TaxHappinessModifier() + ServiceBudgetHappinessModifier() + walkability / 18 + emergencyResponse / 20 + medicalResponse / 24 + policeResponse / 28 + fireProtection / 38 + maintenanceCondition / 20 + communicationCoverage / 30 + mailCoverage / 45 + deathcareCoverage / 45 + innovationCapacity / 55 + learningPipeline / 45 + roadSafety / 35 + fiscalHealth / 45 + administrationEfficiency / 50 + stormwaterResilience / 35 + supplyChainStability / 50 + industrialSpecialization / 70 + developmentQualityBonus + landUseBufferBonus + serviceEquityBonus - Math.Max(0, 45 - walkability) / 8 - responseShortfallPenalty / 12 - Math.Max(0, 48 - medicalResponse) / 9 - patientBacklog / 8 - Math.Max(0, healthUtilization - 115) / 10 - Math.Max(0, 48 - fireProtection) / 10 - fireRisk / 8 - Math.Max(0, 48 - policeResponse) / 10 - caseBacklog / 9 - Math.Max(0, securityUtilization - 115) / 10 - maintenanceShortfallPenalty / 10 - Math.Max(0, 42 - administrationEfficiency) / 8 - policyBacklog / 10 - Math.Max(0, administrationUtilization - 115) / 10 - Math.Max(0, 45 - communicationCoverage) / 9 - Math.Max(0, 40 - mailCoverage) / 12 - Math.Max(0, 45 - deathcareCoverage) / 12 - mortalityPressure / 7 - Math.Max(0, deathcareUtilization - 115) / 10 - studentBacklog / 9 - Math.Max(0, educationUtilization - 115) / 10 - Math.Max(0, 45 - supplyChainStability) / 10 - Math.Max(0, 60 - wastewaterReliability) / 10 - Math.Max(0, wastewaterUtilization - 120) / 12 - Math.Max(0, 60 - stormwaterResilience) / 12 - floodRisk / 12 - accidentRisk / 8 - roadBottleneckPressure / 12 - transitImpactWaitPressure / 10 - debtPressure / 9 - serviceEquityPenalty - developmentQualityPenalty - landUseConflictPenalty - rentHappinessPenalty - crimeHappinessPenalty - parkingHappinessPenalty - laborShortage / 8 - commuteHappinessPenalty - environmentHappinessPenalty - healthHappinessPenalty - goodsShortagePenalty / 12);
            Metrics.Happiness = ClampToScore(Metrics.Happiness + livingConditionBonus - livingConditionPenalty);
            Metrics.CityScore = ClampToScore(Metrics.Happiness + Metrics.Cash / 300 + serviceCoverage / 8 + serviceEquity / 14 + transitCoverage / 10 + regionalConnectivity / 12 + parkingCoverage / 14 + communicationCoverage / 12 + mailCoverage / 14 + deathcareCoverage / 14 + businessEfficiency / 10 + innovationCapacity / 8 + learningPipeline / 14 + roadSafety / 10 + medicalResponse / 12 + fireProtection / 12 + policeResponse / 14 + fiscalHealth / 10 + administrationEfficiency / 10 + commuteEfficiency / 8 + walkability / 10 + emergencyResponse / 10 + maintenanceCondition / 12 + roadConnectivity / 10 + developmentQuality / 12 + landUseBufferBonus + environmentQuality / 10 + publicHealth / 9 + logisticsCoverage / 12 + supplyChainStability / 12 + industrialSpecialization / 14 + wasteCoverage / 12 + wastewaterReliability / 12 + stormwaterResilience / 12 + advancedEducationCoverage / 12 + safetyCoverage / 12 + securityCoverage / 12 + attractiveness / 8 + workforceSkill / 10 + productivityBonus / 8 + goodsMarketBonus / 5 + compactLandUseBonus + landValue / 10 + parkingAccessBonus / 2 - pollution * 2 - congestion / 5 - roadBottleneckPressure / 5 - accidentRisk / 5 - fireRisk / 5 - patientBacklog / 4 - caseBacklog / 4 - studentBacklog / 4 - policyBacklog / 4 - floodRisk / 5 - debtPressure / 4 - carDependency / 10 - parkingAccessPenalty / 2 - Math.Max(0, parkingUtilization - 115) / 4 - transitImpactWaitPressure / 5 - noiseStress / 5 - healthRisk / 5 - mortalityPressure / 5 - Math.Max(0, 45 - administrationEfficiency) / 4 - Math.Max(0, administrationUtilization - 115) / 5 - Math.Max(0, 45 - communicationCoverage) / 4 - Math.Max(0, 40 - mailCoverage) / 5 - Math.Max(0, 42 - deathcareCoverage) / 5 - Math.Max(0, 45 - supplyChainStability) / 4 - Math.Max(0, communicationUtilization - 115) / 5 - Math.Max(0, mailUtilization - 115) / 5 - Math.Max(0, deathcareUtilization - 115) / 5 - Math.Max(0, healthUtilization - 115) / 5 - Math.Max(0, educationUtilization - 115) / 5 - Math.Max(0, securityUtilization - 115) / 5 - Math.Max(0, fireUtilization - 115) / 5 - Math.Max(0, wastewaterUtilization - 110) / 5 - Math.Max(0, stormwaterUtilization - 110) / 5 - Math.Max(0, 45 - advancedEducationCoverage) / 6 - transitOverloadPenalty / 5 - wasteShortfallPenalty / 5 - safetyShortfallPenalty / 4 - responseShortfallPenalty / 5 - maintenanceShortfallPenalty / 4 - serviceEquityPenalty / 2 - goodsShortagePenalty / 4 - developmentQualityPenalty / 2 - landUseConflictPenalty / 2 - idleZonePenalty / 3 - deadEndRoadTiles / 2 - rentHappinessPenalty - crimeHappinessPenalty - laborShortage / 3 - Metrics.DisconnectedBuildings * 4);
            Metrics.CityScore = ClampToScore(Metrics.CityScore + livingCondition / 14 - livingPressure / 5);
            Metrics.Demand.Residential = ClampToScore(52 + Metrics.Happiness / 2 + serviceCoverage / 6 + serviceEquity / 18 + transitCoverage / 20 + roadSafety / 24 + fiscalHealth / 28 + commuteEfficiency / 16 + walkability / 16 + developmentQualityBonus / 2 + landUseBufferBonus / 2 + environmentQuality / 12 + publicHealth / 12 + deathcareCoverage / 28 + learningPipeline / 24 + wasteDemandBonus / 24 + safetyCoverage / 22 + securityCoverage / 28 + rentHousingDemand + laborShortage / 5 + PolicyDemandBoost(ZoneType.Residential) + TaxDemandModifier() - serviceEquityPenalty / 2 - developmentQualityPenalty / 2 - landUseConflictPenalty - rentGrowthPenalty - crimeHappinessPenalty / 2 - parkingHappinessPenalty / 2 - accidentRisk / 12 - debtPressure / 12 - carDependency / 12 - noiseStress / 8 - healthRisk / 10 - mortalityPressure / 10 - studentBacklog / 10 - Math.Max(0, educationUtilization - 120) / 12 - Math.Max(0, deathcareUtilization - 120) / 12 - Math.Max(0, housing - Metrics.Population) / 4);
            Metrics.Demand.Residential = ClampToScore(Metrics.Demand.Residential + livingCondition / 24 - livingPressure / 7);
            Metrics.Demand.Commercial = ClampToScore(35 + Metrics.Population / 9 + landValue / 8 + transitCoverage / 12 + regionalConnectivity / 20 + parkingCoverage / 20 + communicationCoverage / 18 + mailCoverage / 20 + businessEfficiency / 12 + innovationCapacity / 14 + roadSafety / 22 + fiscalHealth / 16 + commuteEfficiency / 12 + walkability / 14 + developmentQualityBonus + landUseBufferBonus / 2 + serviceEquityBonus / 2 + parkingAccessBonus + environmentQuality / 18 + publicHealth / 18 + logisticsDemandBonus / 18 + supplyChainStability / 18 + goodsMarketBonus / 3 + educationCoverage / 15 + advancedEducationCoverage / 18 + workforceSkill / 10 + wasteDemandBonus / 22 + safetyCoverage / 24 + securityCoverage / 20 + PolicyDemandBoost(ZoneType.Commercial) + TaxDemandModifier() - serviceEquityPenalty / 3 - developmentQualityPenalty / 3 - landUseConflictPenalty / 3 - parkingAccessPenalty - Math.Max(0, mailUtilization - 115) / 7 - rentGrowthPenalty / 2 - crimeHappinessPenalty - accidentRisk / 11 - debtPressure / 9 - laborShortage / 5 - carDependency / 16 - noiseStress / 12 - healthRisk / 16 - goodsShortagePenalty / 2 - Math.Max(0, jobs - employable) / 5);
            Metrics.Demand.Industrial = ClampToScore(28 + Metrics.Population / 10 + transitCoverage / 18 + roadSafety / 28 + fiscalHealth / 24 + commuteEfficiency / 18 + developmentQualityBonus / 2 + logisticsDemandBonus / 10 + supplyChainStability / 24 + industrialSpecialization / 12 + resourceSpecialization / 16 + goodsShortagePenalty / 3 + goodsMarketBonus / 5 + localGoodsSupply / 16 + innovationCapacity / 10 + educationCoverage / 18 + advancedEducationCoverage / 22 + workforceSkill / 12 + wasteDemandBonus / 20 + PolicyDemandBoost(ZoneType.Industrial) + TaxDemandModifier() - developmentQualityPenalty / 4 - landUseConflictPenalty / 4 - pollution * 2 - congestion / 5 - accidentRisk / 12 - debtPressure / 12 - wasteShortfallPenalty / 6 - safetyShortfallPenalty / 8 - laborShortage / 4);
            Metrics.Demand.Office = ClampToScore(18 + Metrics.Population / 12 + educationCoverage / 4 + advancedEducationCoverage / 4 + workforceSkill / 4 + businessEfficiency / 5 + innovationCapacity / 4 + landValue / 6 + communicationCoverage / 8 + mailCoverage / 18 + transitCoverage / 14 + regionalConnectivity / 22 + parkingCoverage / 24 + roadSafety / 20 + fiscalHealth / 14 + commuteEfficiency / 10 + walkability / 20 + developmentQualityBonus + landUseBufferBonus / 2 + serviceEquityBonus / 2 + parkingAccessBonus / 2 + environmentQuality / 16 + publicHealth / 20 + securityCoverage / 22 + PolicyDemandBoost(ZoneType.Office) + TaxDemandModifier() - serviceEquityPenalty / 3 - developmentQualityPenalty / 3 - landUseConflictPenalty / 2 - parkingAccessPenalty / 2 - Math.Max(0, mailUtilization - 115) / 8 - Math.Max(0, officeJobs - employable / 3) / 5 - congestion / 8 - accidentRisk / 10 - debtPressure / 8 - crimeHappinessPenalty - laborShortage / 3 - carDependency / 18 - noiseStress / 14 - healthRisk / 18);
            Metrics.Demand.MixedUse = ClampToScore(18 + (Metrics.Demand.Residential + Metrics.Demand.Commercial) / 3 + landValue / 8 + transitCoverage / 8 + parkingCoverage / 24 + communicationCoverage / 16 + mailCoverage / 22 + businessEfficiency / 18 + innovationCapacity / 16 + roadSafety / 22 + fiscalHealth / 18 + commuteEfficiency / 8 + walkability / 10 + developmentQualityBonus + landUseBufferBonus / 2 + serviceEquityBonus + parkingAccessBonus / 2 + environmentQuality / 14 + publicHealth / 16 + serviceCoverage / 15 + advancedEducationCoverage / 18 + workforceSkill / 12 + securityCoverage / 28 + PolicyDemandBoost(ZoneType.MixedUse) + TaxDemandModifier() - serviceEquityPenalty / 3 - developmentQualityPenalty / 3 - landUseConflictPenalty / 2 - parkingAccessPenalty / 2 - Math.Max(0, mailUtilization - 115) / 9 - rentGrowthPenalty / 2 - congestion / 10 - accidentRisk / 12 - debtPressure / 10 - crimeHappinessPenalty / 2 - laborShortage / 6 - carDependency / 20 - noiseStress / 12 - healthRisk / 16);
            Metrics.Demand.MixedUse = ClampToScore(Metrics.Demand.MixedUse + livingCondition / 28 - livingPressure / 8);
            Metrics.Demand.Service = ClampToScore(60 - serviceCoverage + Metrics.Population / 18 + underservedResidents / 22 + safetyShortfallPenalty / 2 + responseShortfallPenalty / 4 + Math.Max(0, 55 - medicalResponse) / 4 + Math.Max(0, healthUtilization - 105) / 5 + patientBacklog / 4 + Math.Max(0, 55 - fireProtection) / 3 + fireRisk / 4 + Math.Max(0, 55 - policeResponse) / 4 + Math.Max(0, securityUtilization - 105) / 5 + caseBacklog / 4 + maintenanceShortfallPenalty / 3 + serviceEquityPenalty / 2 + developmentQualityPenalty / 2 + landUseConflictPenalty / 2 + Math.Max(0, serviceUtilization - 100) / 4 + Math.Max(0, 38 - mailCoverage) / 3 + Math.Max(0, mailUtilization - 105) / 5 + Math.Max(0, 45 - deathcareCoverage) / 3 + Math.Max(0, deathcareUtilization - 105) / 5 + mortalityPressure / 4 + Math.Max(0, 45 - educationCoverage) / 3 + Math.Max(0, educationUtilization - 105) / 5 + studentBacklog / 4 + policyBacklog / 4 + Math.Max(0, administrationUtilization - 105) / 6 + transitOverloadPenalty / 4 + transitImpactWaitPressure / 5 + roadBottleneckPressure / 6 + accidentRisk / 5 + crimePressure / 5 + Math.Max(0, 55 - environmentQuality) / 4 + Math.Max(0, 50 - walkability) / 6 + noiseStress / 10 + healthRisk / 6 + PolicyDemandBoost(ZoneType.Civic) + ServiceBudgetServiceDemandModifier() - AdministrationServiceDemandRelief(administrationEfficiency));
            Metrics.Demand.Service = ClampToScore(Metrics.Demand.Service + livingPressure / 5 - Math.Max(0, livingCondition - 70) / 12);
            Metrics.Demand.Utility = ClampToScore(Math.Max(0, powerDemand - powerSupply) + Math.Max(0, waterDemand - waterSupply) + Metrics.Population / 30 + wasteShortfallPenalty + Math.Max(0, 70 - wastewaterReliability) / 2 + Math.Max(0, wastewaterUtilization - 100) / 3 + Math.Max(0, 70 - stormwaterResilience) / 2 + Math.Max(0, stormwaterUtilization - 100) / 3 + Math.Max(0, floodRisk - 45) / 3 + Math.Max(0, 48 - communicationCoverage) / 2 + Math.Max(0, communicationUtilization - 110) / 4 + Math.Max(0, parkingUtilization - 105) / 4 + Math.Max(0, 35 - parkingCoverage) / 3 + maintenanceShortfallPenalty / 2 + Math.Max(0, 55 - roadMaintenanceCoverage) / 3);
            Metrics.DemandUrgency = AnalyzeDemandDrivers();
            Metrics.CashRunwayDays = ComputeCashRunwayDays(Metrics.Cash, Metrics.NetIncome);
            Metrics.ForecastRisk = RiskForecastAdvisor();
            Metrics.BudgetStress = BudgetBreakdownAdvisor();
            Metrics.DistrictPriorityScore = DistrictPriorityAdvisor();
            Metrics.RoadHierarchyPressure = RoadHierarchyAdvisor();
            Metrics.CommuteCorridorScore = CommuteCorridorAdvisor();
            Metrics.EconomicSpecializationScore = EconomicSpecializationAdvisor();
            Metrics.ServiceGapAdvisorScore = ServiceGapAdvisor();
            Metrics.GrowthBottleneckScore = GrowthBottleneckAdvisor();
            Metrics.HousingAffordabilityScore = HousingAffordabilityAdvisor();
            Metrics.BuildingUpgradeReadinessScore = BuildingUpgradeReadinessAdvisor();
            Metrics.InfrastructureResilienceScore = InfrastructureResilienceAdvisor();

            RefreshAlerts(utilityEfficiency);
            RefreshUnlocks();
            RefreshMilestones();
            PublishRecentEvents();
        }

        private void AdvanceDay()
        {
            Metrics.Day += 1;
            for (var i = 0; i < buildings.Count; i += 1)
            {
                buildings[i].AgeDays += 1;
            }

            // Optimization: Batch all daily updates before recomputing metrics once
            var buildingsChanged = false;
            var populationBefore = 0;

            // First metrics computation for the day
            MarkMetricsDirty();
            RecomputeMetrics();
            populationBefore = Metrics.Population;

            // 禁用自动升级 - 玩家必须手动使用材料升级
            // if (UpdateBuildingLevels())
            // {
            //     buildingsChanged = true;
            // }

            // Auto-develop zones
            if (TryAutoDevelopZones())
            {
                buildingsChanged = true;
            }

            // Update population (uses current metrics)
            UpdatePopulation();
            var populationDelta = Metrics.Population - populationBefore;
            if (populationDelta > 0)
            {
                AddCityEvent("\u4eba\u53e3\u589e\u957f\uff1a+" + populationDelta);
            }
            else if (populationDelta < 0)
            {
                AddCityEvent("\u4eba\u53e3\u56de\u843d\uff1a" + populationDelta);
            }

            // CRITICAL FIX: Recompute metrics after population change BEFORE budget settlement
            // This ensures ApplyBudget uses up-to-date tax income based on new population
            if (populationDelta != 0 || buildingsChanged)
            {
                MarkMetricsDirty();
                RecomputeMetrics();
            }

            // Apply budget if needed (now uses latest metrics)
            if (Metrics.Day % Math.Max(1, config.DaysPerBudgetPeriod) == 0)
            {
                ApplyBudget();
            }
        }

        private void UpdatePopulation()
        {
            if (Metrics.HousingCapacity <= 0)
            {
                Metrics.Population = 0;
                return;
            }

            if (Metrics.Population < Metrics.HousingCapacity)
            {
                var gap = Metrics.HousingCapacity - Metrics.Population;
                var growth = Math.Max(1, Math.Min(12, gap / 8 + Metrics.Happiness / 22));
                if (IsPolicyActive(CityPolicy.GrowthGrants))
                {
                    growth += Math.Max(2, Math.Min(8, gap / 12 + 2));
                }

                if (IsPolicyActive(CityPolicy.AffordableHousing) && Metrics.Population >= 80)
                {
                    growth += Metrics.RentPressure > 55 ? 2 : 1;
                }

                growth = Math.Max(0, growth - Math.Max(0, Metrics.RentPressure - 70) / 8);
                growth = Math.Max(0, growth - Math.Max(0, Metrics.HealthRisk - 55) / 12);
                if (Metrics.PublicHealth > 70)
                {
                    growth += 1;
                }

                if (Metrics.Happiness < 35)
                {
                    // 极低幸福度触发人口流失
                    growth = -(40 - Metrics.Happiness);
                }

                // 幸福度奖励：高幸福度加速人口增长
                var happinessGrowthBonus = HappinessRewardSystem.GetPopulationGrowthBonus(Metrics.Happiness);
                growth = (int)(growth * (1f + happinessGrowthBonus));

                Metrics.Population = Math.Min(Metrics.HousingCapacity, Metrics.Population + growth);
            }
            else if (Metrics.Population > Metrics.HousingCapacity)
            {
                var overflow = Metrics.Population - Metrics.HousingCapacity;
                Metrics.Population -= Math.Max(1, overflow / 4);
            }
        }

        private bool TryAutoDevelopZones()
        {
            if (Metrics.Demand == null || Metrics.Cash <= 0)
            {
                return false;
            }

            var candidates = new List<AutoDevelopmentCandidate>();
            AddAutoDevelopmentCandidate(candidates, ZoneType.Residential, "residential_pod", Metrics.Demand.Residential);
            AddAutoDevelopmentCandidate(candidates, ZoneType.Residential, "apartment_block", HighDensityResidentialDemand());
            AddAutoDevelopmentCandidate(candidates, ZoneType.Commercial, "market_corner", Metrics.Demand.Commercial);
            AddAutoDevelopmentCandidate(candidates, ZoneType.MixedUse, "mixed_use_block", Metrics.Demand.MixedUse);
            AddAutoDevelopmentCandidate(candidates, ZoneType.Office, "office_studio", Metrics.Demand.Office);
            AddAutoDevelopmentCandidate(candidates, ZoneType.Industrial, "maker_yard", Metrics.Demand.Industrial);
            candidates.Sort(CompareAutoDevelopmentCandidates);

            var built = 0;
            var projectLimit = AutoDevelopmentProjectLimit();
            for (var i = 0; i < candidates.Count && built < projectLimit; i += 1)
            {
                var quota = AutoDevelopmentQuota(candidates[i].Demand);
                while (quota > 0 && built < projectLimit)
                {
                    if (!TryAutoDevelopBuilding(candidates[i].Zone, candidates[i].BuildingId))
                    {
                        break;
                    }

                    built += 1;
                    quota -= 1;
                }
            }

            if (built > 0)
            {
                AddCityEvent("\u81ea\u52a8\u5f00\u53d1\uff1a" + built + " \u680b");
            }

            return built > 0;
        }

        private void AddAutoDevelopmentCandidate(List<AutoDevelopmentCandidate> candidates, ZoneType zone, string buildingId, int demand)
        {
            if (demand < 55)
            {
                return;
            }

            var definition = config.GetBuilding(buildingId);
            if (definition == null || !string.IsNullOrEmpty(UnlockReason(definition)))
            {
                return;
            }

            candidates.Add(new AutoDevelopmentCandidate
            {
                Zone = zone,
                BuildingId = buildingId,
                Demand = demand
            });
        }

        private bool TryAutoDevelopBuilding(ZoneType zone, string buildingId)
        {
            var definition = config.GetBuilding(buildingId);
            if (definition == null)
            {
                return false;
            }

            var grant = AutoDevelopmentGrant(definition);
            // 保留最低3000现金储备，避免自动开发耗尽现金
            if (Metrics.Cash < grant + 3000)
            {
                return false;
            }

            GridPos site;
            if (!FindAutoDevelopmentSite(zone, definition, out site))
            {
                return false;
            }

            PlaceBuildingInternal(buildingId, site, false, true);
            Metrics.Cash -= grant;
            return true;
        }

        private bool FindAutoDevelopmentSite(ZoneType zone, BuildingDefinition definition, out GridPos bestSite)
        {
            bestSite = new GridPos(0, 0);
            var bestScore = int.MinValue;
            for (var y = 0; y <= Grid.Height - definition.Size.H; y += 1)
            {
                for (var x = 0; x <= Grid.Width - definition.Size.W; x += 1)
                {
                    var pos = new GridPos(x, y);
                    if (!CanAutoDevelopAt(pos, definition, zone))
                    {
                        continue;
                    }

                    var suitability = ZoneSuitabilityForRect(pos, definition.Size, zone);
                    if (suitability < MinZoneSuitabilityForAutoDevelopment(zone, definition))
                    {
                        continue;
                    }

                    var score = AutoDevelopmentSiteScore(pos, definition, zone, suitability);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSite = pos;
                    }
                }
            }

            return bestScore > int.MinValue;
        }

        private bool HasAutoDevelopmentSite(ZoneType zone, string buildingId)
        {
            var definition = config.GetBuilding(buildingId);
            if (definition == null || !string.IsNullOrEmpty(UnlockReason(definition)))
            {
                return false;
            }

            GridPos ignored;
            return FindAutoDevelopmentSite(zone, definition, out ignored);
        }

        private bool CanAutoDevelopAt(GridPos pos, BuildingDefinition definition, ZoneType zone)
        {
            if (!string.IsNullOrEmpty(Grid.CanPlaceBuilding(pos, definition.Size)))
            {
                return false;
            }

            foreach (var tilePos in Grid.PositionsInRect(pos, definition.Size))
            {
                if (!Grid.InBounds(tilePos) || Grid.GetTile(tilePos).Zone != zone)
                {
                    return false;
                }
            }

            return !string.IsNullOrEmpty(NearestRoadId(pos, definition.Size));
        }

        private int ZoneSuitabilityForRect(List<GridPos> points, ZoneType zone)
        {
            if (points == null || points.Count == 0 || zone == ZoneType.None || zone == ZoneType.Civic || zone == ZoneType.Utility)
            {
                return 0;
            }

            var total = 0;
            var count = 0;
            for (var i = 0; i < points.Count; i += 1)
            {
                if (!Grid.InBounds(points[i]))
                {
                    continue;
                }

                total += ZoneSuitabilityForTile(Grid.GetTile(points[i]), zone);
                count += 1;
            }

            return count == 0 ? 0 : ClampToScore(total / count);
        }

        private int ZoneSuitabilityForRect(GridPos pos, GridSize size, ZoneType zone)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(pos, size))
            {
                if (!Grid.InBounds(tilePos))
                {
                    continue;
                }

                total += ZoneSuitabilityForTile(Grid.GetTile(tilePos), zone);
                count += 1;
            }

            var roadDistance = NearestRoadDistance(pos, size);
            var roadAccess = Math.Max(0, config.MaxRoadSearchDistance - roadDistance + 1) * 8;
            return count == 0 ? 0 : ClampToScore(total / count + roadAccess);
        }

        private static int ZoneSuitabilityForTile(TileData tile, ZoneType zone)
        {
            var pollutionPressure = tile.Pollution * 5 + tile.Noise * 3;
            if (zone == ZoneType.Residential)
            {
                return ClampToScore(32 + tile.LandValue / 3 + tile.ParkAccess / 5 + tile.HealthAccess / 5 + tile.EducationAccess / 6 + tile.TransitAccess / 8 + tile.WasteAccess / 8 - pollutionPressure / 3 - tile.Traffic / 5);
            }

            if (zone == ZoneType.Commercial)
            {
                return ClampToScore(28 + tile.LandValue / 3 + tile.TransitAccess / 3 + tile.EducationAccess / 8 + tile.SecurityAccess / 8 + tile.Traffic / 8 - tile.Pollution * 2);
            }

            if (zone == ZoneType.Office)
            {
                return ClampToScore(22 + tile.LandValue / 2 + tile.TransitAccess / 3 + tile.EducationAccess / 3 + tile.SecurityAccess / 7 + tile.ParkAccess / 8 - tile.Noise * 2 - tile.Pollution * 3);
            }

            if (zone == ZoneType.MixedUse)
            {
                return ClampToScore(24 + tile.LandValue / 3 + tile.TransitAccess / 3 + tile.ParkAccess / 6 + tile.HealthAccess / 7 + tile.EducationAccess / 6 + tile.SecurityAccess / 8 - pollutionPressure / 4);
            }

            if (zone == ZoneType.Industrial)
            {
                var resourceTerrainBonus = tile.Terrain == TerrainType.Hill ? 8 : 0;
                return ClampToScore(35 + resourceTerrainBonus + tile.LogisticsAccess / 2 + tile.TransitAccess / 8 + tile.WasteAccess / 5 + tile.Traffic / 10 - tile.LandValue / 8 - tile.ParkAccess / 10);
            }

            return 0;
        }

        private static int MinZoneSuitabilityForAutoDevelopment(ZoneType zone, BuildingDefinition definition)
        {
            if (definition != null && definition.Id == "apartment_block")
            {
                return 46;
            }

            if (zone == ZoneType.Industrial)
            {
                return 30;
            }

            if (zone == ZoneType.Commercial || zone == ZoneType.MixedUse || zone == ZoneType.Office)
            {
                return 34;
            }

            return 32;
        }

        private int AutoDevelopmentSiteScore(GridPos pos, BuildingDefinition definition, ZoneType zone, int suitability)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(pos, definition.Size))
            {
                var tile = Grid.GetTile(tilePos);
                total += tile.LandValue;
                if (zone == ZoneType.Residential)
                {
                    total += tile.ParkAccess / 2 + tile.HealthAccess / 2 + tile.EducationAccess / 3 + tile.WasteAccess / 4;
                    if (definition.Id == "apartment_block")
                    {
                        total += tile.TransitAccess / 3 + tile.LandValue / 3;
                    }
                }
                else if (zone == ZoneType.Commercial)
                {
                    total += tile.TransitAccess / 2 + tile.EducationAccess / 4;
                }
                else if (zone == ZoneType.Office)
                {
                    total += tile.LandValue / 2 + tile.TransitAccess / 2 + tile.EducationAccess / 2 + tile.SecurityAccess / 4;
                }
                else if (zone == ZoneType.MixedUse)
                {
                    total += tile.LandValue / 2 + tile.TransitAccess / 2 + tile.ParkAccess / 3 + tile.HealthAccess / 3 + tile.EducationAccess / 3 + tile.SecurityAccess / 5;
                }
                else if (zone == ZoneType.Industrial)
                {
                    total += tile.TransitAccess / 4 + tile.WasteAccess / 2 - tile.Pollution / 2;
                }

                count += 1;
            }

            var roadDistance = NearestRoadDistance(pos, definition.Size);
            var roadScore = Math.Max(0, config.MaxRoadSearchDistance - roadDistance + 1) * 10;
            var variation = Math.Abs((pos.X * 17 + pos.Y * 31 + Metrics.Day * 7) % 9);
            return count == 0 ? int.MinValue : total / count + suitability * 2 + roadScore + variation;
        }

        private int BuildingSiteScore(GridPos pos, BuildingDefinition definition, bool hasRoad)
        {
            var landValue = AverageSiteValue(pos, definition.Size, tile => tile.LandValue);
            var pollution = AverageSiteValue(pos, definition.Size, tile => tile.Pollution);
            var noise = AverageSiteValue(pos, definition.Size, tile => tile.Noise);
            var traffic = AverageSiteValue(pos, definition.Size, tile => tile.Traffic);
            var transit = AverageSiteValue(pos, definition.Size, tile => tile.TransitAccess);
            var logistics = AverageSiteValue(pos, definition.Size, tile => tile.LogisticsAccess);
            var communication = AverageSiteValue(pos, definition.Size, tile => tile.CommunicationAccess);
            var mail = AverageSiteValue(pos, definition.Size, tile => tile.MailAccess);
            var parking = AverageSiteValue(pos, definition.Size, tile => tile.ParkingAccess);
            var stormwater = AverageSiteValue(pos, definition.Size, tile => tile.StormwaterAccess);
            var park = AverageSiteValue(pos, definition.Size, tile => tile.ParkAccess);
            var health = AverageSiteValue(pos, definition.Size, tile => tile.HealthAccess);
            var education = AverageSiteValue(pos, definition.Size, tile => tile.EducationAccess);
            var security = AverageSiteValue(pos, definition.Size, tile => tile.SecurityAccess);
            var waste = AverageSiteValue(pos, definition.Size, tile => tile.WasteAccess);
            var score = 48 + (hasRoad ? 12 : -24);

            if (IsGrowthZoneBuilding(definition))
            {
                score += (ZoneSuitabilityForRect(pos, definition.Size, definition.PreferredZone) - 50) / 2;
            }

            if (definition.Category == BuildingCategory.Residential)
            {
                score += landValue / 6 + park / 7 + health / 7 + education / 7 + transit / 10 + waste / 10 + security / 12 - pollution * 2 - noise * 2 - traffic / 10;
            }
            else if (IsMixedUseBuilding(definition))
            {
                score += landValue / 6 + transit / 5 + park / 10 + health / 12 + education / 10 + security / 12 + parking / 12 - pollution - noise;
            }
            else if (IsOfficeBuilding(definition))
            {
                score += landValue / 5 + transit / 5 + education / 6 + communication / 6 + mail / 10 + security / 12 + park / 12 - pollution * 2 - noise;
            }
            else if (definition.Category == BuildingCategory.Commercial)
            {
                score += landValue / 7 + transit / 5 + parking / 8 + communication / 8 + mail / 9 + traffic / 12 + security / 14 - pollution - noise / 2;
            }
            else if (definition.Category == BuildingCategory.Industrial || IsLogisticsBuilding(definition) || IsResourceBuilding(definition))
            {
                score += logistics / 5 + waste / 10 + transit / 12 + traffic / 14 - landValue / 12 - park / 14;
            }
            else if (IsTransitBuilding(definition))
            {
                score += Math.Max(0, 60 - transit) / 2 + traffic / 12 + landValue / 12;
            }
            else if (IsParkingBuilding(definition))
            {
                score += Math.Max(0, 70 - parking) / 2 + traffic / 10 + landValue / 14;
            }
            else if (IsStormwaterBuilding(definition))
            {
                score += Math.Max(0, 70 - stormwater) / 2 + pollution + traffic / 14;
            }
            else if (IsHealthBuilding(definition))
            {
                score += Math.Max(0, 65 - health) / 2 + transit / 12 + park / 14 - pollution - noise;
            }
            else if (IsEducationBuilding(definition))
            {
                score += Math.Max(0, 65 - education) / 2 + transit / 12 + park / 14 - pollution - noise;
            }
            else if (IsSecurityBuilding(definition))
            {
                score += Math.Max(0, 65 - security) / 2 + traffic / 14;
            }
            else if (IsCommunicationBuilding(definition))
            {
                score += Math.Max(0, 65 - communication) / 2 + landValue / 12;
            }
            else if (IsMailBuilding(definition))
            {
                score += Math.Max(0, 65 - mail) / 2 + logistics / 12 + traffic / 14;
            }
            else if (IsParkBuilding(definition) || IsAttractionBuilding(definition))
            {
                score += landValue / 8 + transit / 8 + Math.Max(0, 55 - park) / 3 - pollution - noise;
            }
            else if (definition.Category == BuildingCategory.Utility)
            {
                score += logistics / 12 + stormwater / 14 - landValue / 12 - park / 12;
            }

            return ClampToScore(score);
        }

        private string SiteDiagnosis(GridPos pos, BuildingDefinition definition, bool hasRoad, int siteScore)
        {
            var strengths = new List<string>();
            var risks = new List<string>();
            var landValue = AverageSiteValue(pos, definition.Size, tile => tile.LandValue);
            var pollution = AverageSiteValue(pos, definition.Size, tile => tile.Pollution);
            var noise = AverageSiteValue(pos, definition.Size, tile => tile.Noise);
            var transit = AverageSiteValue(pos, definition.Size, tile => tile.TransitAccess);
            var logistics = AverageSiteValue(pos, definition.Size, tile => tile.LogisticsAccess);
            var communication = AverageSiteValue(pos, definition.Size, tile => tile.CommunicationAccess);
            var mail = AverageSiteValue(pos, definition.Size, tile => tile.MailAccess);
            var parking = AverageSiteValue(pos, definition.Size, tile => tile.ParkingAccess);
            var stormwater = AverageSiteValue(pos, definition.Size, tile => tile.StormwaterAccess);
            var park = AverageSiteValue(pos, definition.Size, tile => tile.ParkAccess);
            var health = AverageSiteValue(pos, definition.Size, tile => tile.HealthAccess);
            var education = AverageSiteValue(pos, definition.Size, tile => tile.EducationAccess);
            var security = AverageSiteValue(pos, definition.Size, tile => tile.SecurityAccess);
            var waste = AverageSiteValue(pos, definition.Size, tile => tile.WasteAccess);

            if (!hasRoad)
            {
                risks.Add("需先补路");
            }

            if (IsGrowthZoneBuilding(definition))
            {
                var suitability = ZoneSuitabilityForRect(pos, definition.Size, definition.PreferredZone);
                if (suitability >= 65) strengths.Add("分区适配高");
                if (suitability < 40) risks.Add("适宜度偏低");
            }

            if (definition.Category == BuildingCategory.Residential || IsMixedUseBuilding(definition))
            {
                var livingAccess = (park + health + education + security) / 4;
                if (livingAccess >= 55) strengths.Add("生活服务近");
                if (livingAccess < 30) risks.Add("服务缺口");
                if (pollution + noise > 18) risks.Add("污染噪声重");
                if (transit >= 45) strengths.Add("公交便利");
            }
            else if (IsOfficeBuilding(definition))
            {
                if (communication >= 45) strengths.Add("通信条件好");
                if (education >= 45) strengths.Add("人才服务近");
                if (transit < 30) risks.Add("公交偏弱");
                if (pollution + noise > 18) risks.Add("环境干扰");
            }
            else if (definition.Category == BuildingCategory.Commercial)
            {
                if (transit >= 45) strengths.Add("客流可达");
                if (parking >= 45) strengths.Add("停车承接好");
                if (communication < 30 || mail < 30) risks.Add("商务配套弱");
            }
            else if (definition.Category == BuildingCategory.Industrial || IsLogisticsBuilding(definition) || IsResourceBuilding(definition))
            {
                if (logistics >= 45) strengths.Add("货运便利");
                if (waste >= 40) strengths.Add("回收近");
                if (logistics < 25) risks.Add("货运偏弱");
                if (landValue > 70) risks.Add("地价偏高");
            }
            else if (IsTransitBuilding(definition))
            {
                if (transit < 35) strengths.Add("补公交空白");
                if (landValue >= 55) strengths.Add("服务核心区");
            }
            else if (IsParkingBuilding(definition))
            {
                if (parking < 35) strengths.Add("补停车缺口");
                if (transit < 25) risks.Add("公交替代弱");
            }
            else if (IsStormwaterBuilding(definition))
            {
                if (stormwater < 35) strengths.Add("补雨洪缺口");
                if (pollution > 8) strengths.Add("承接污染片区");
            }
            else if (IsHealthBuilding(definition))
            {
                if (health < 35) strengths.Add("补医疗缺口");
                if (transit < 25) risks.Add("就医可达弱");
            }
            else if (IsEducationBuilding(definition))
            {
                if (education < 35) strengths.Add("补学位缺口");
                if (pollution + noise > 16) risks.Add("环境不安静");
            }
            else if (IsSecurityBuilding(definition))
            {
                if (security < 35) strengths.Add("补警务缺口");
            }
            else if (IsCommunicationBuilding(definition))
            {
                if (communication < 35) strengths.Add("补通信缺口");
            }
            else if (IsMailBuilding(definition))
            {
                if (mail < 35) strengths.Add("补邮政缺口");
            }

            if (strengths.Count == 0 && risks.Count == 0)
            {
                strengths.Add("条件均衡");
            }

            var summary = strengths.Count > 0 ? strengths[0] : risks[0];
            if (strengths.Count > 0 && risks.Count > 0)
            {
                summary += "，" + risks[0];
            }
            else if (strengths.Count > 1)
            {
                summary += "，" + strengths[1];
            }
            else if (risks.Count > 1)
            {
                summary += "，" + risks[1];
            }

            return "选址诊断 " + siteScore + "%：" + summary;
        }

        private int AverageSiteValue(GridPos pos, GridSize size, Func<TileData, int> selector)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(pos, size))
            {
                if (!Grid.InBounds(tilePos))
                {
                    continue;
                }

                total += selector(Grid.GetTile(tilePos));
                count += 1;
            }

            return count == 0 ? 0 : total / count;
        }

        private int AutoDevelopmentProjectLimit()
        {
            if (Metrics.Population >= 520)
            {
                return 3;
            }

            return Metrics.Population >= 160 ? 2 : 1;
        }

        private static int AutoDevelopmentQuota(int demand)
        {
            if (demand >= 88)
            {
                return 3;
            }

            if (demand >= 72)
            {
                return 2;
            }

            return demand >= 55 ? 1 : 0;
        }

        private int HighDensityResidentialDemand()
        {
            if (Metrics.Population < 180 || Metrics.RentPressure < 58)
            {
                return 0;
            }

            return ClampToScore(Metrics.Demand.Residential + Math.Max(0, Metrics.RentPressure - 55) * 2 + Metrics.Population / 90);
        }

        private static int AutoDevelopmentGrant(BuildingDefinition definition)
        {
            return Math.Max(0, definition.Cost / 10);
        }

        private static int CompareAutoDevelopmentCandidates(AutoDevelopmentCandidate a, AutoDevelopmentCandidate b)
        {
            return b.Demand.CompareTo(a.Demand);
        }

        private void ApplyBudget()
        {
            Metrics.LastBudgetChange = Metrics.NetIncome;
            Metrics.Cash += Metrics.NetIncome;
            AddCityEvent("\u9884\u7b97\u7ed3\u7b97\uff1a" + FormatSigned(Metrics.NetIncome));
            if (bondPrincipal > 0)
            {
                bondPrincipal = Math.Max(0, bondPrincipal - Math.Min(bondPrincipal, Metrics.BondPayment));
                Metrics.BondPrincipal = bondPrincipal;
            }
        }

        private int ComputeBondPayment()
        {
            if (bondPrincipal <= 0)
            {
                return 0;
            }

            return Math.Min(bondPrincipal, 90 + bondPrincipal / 24);
        }

        private static int MunicipalBondCash()
        {
            return 3000;
        }

        private static int MunicipalBondPrincipal()
        {
            return 3600;
        }

        private static int MunicipalBondDebtLimit()
        {
            return 14400;
        }

        private static int ComputeDebtPressure(int cash, int netIncome, int monthlyExpense, int population, int bondPrincipal)
        {
            if (population < 80 && cash >= 0 && netIncome >= 0)
            {
                return 0;
            }

            var expense = Math.Max(1, monthlyExpense);
            var deficitPressure = netIncome < 0 ? Math.Min(55, -netIncome * 100 / expense) : 0;
            var reserveShortfall = Math.Max(0, expense - Math.Max(0, cash));
            var reservePressure = Math.Min(35, reserveShortfall * 100 / expense);
            var insolvencyPressure = cash < 0 ? Math.Min(35, -cash / 250) : 0;
            var bondPressure = Math.Min(35, Math.Max(0, bondPrincipal) / 400);
            var earlyCityGrace = population < 180 ? 10 : 0;
            return ClampToScore(deficitPressure + reservePressure + insolvencyPressure + bondPressure - earlyCityGrace);
        }

        private static int ComputeFiscalHealth(int cash, int netIncome, int monthlyExpense, int debtPressure)
        {
            var expense = Math.Max(1, monthlyExpense);
            var reserveScore = ClampToScore(Math.Max(0, cash) * 100 / (expense * 4));
            var incomeScore = netIncome >= 0 ? Math.Min(25, netIncome * 100 / expense) : -Math.Min(35, -netIncome * 100 / expense);
            return ClampToScore(42 + reserveScore / 2 + incomeScore - debtPressure / 2);
        }

        private static int ComputeCashRunwayDays(int cash, int netIncome)
        {
            if (netIncome >= 0)
            {
                return 999;
            }

            if (cash <= 0)
            {
                return 0;
            }

            return Math.Min(999, Math.Max(0, cash * 30 / Math.Max(1, -netIncome)));
        }

        private int RiskForecastAdvisor()
        {
            string focus;
            string action;
            var risk = ComputeForecastRisk(out focus, out action);
            Metrics.ForecastFocus = focus;
            Metrics.ForecastAction = action;
            return risk;
        }

        private int ComputeForecastRisk(out string focus, out string action)
        {
            var bestRisk = 0;
            focus = "\u5e73\u7a33";
            action = "\u7ee7\u7eed\u8865\u9f50\u5f53\u524d\u77ed\u677f";

            var cashRisk = 0;
            if (Metrics.Cash < 0)
            {
                cashRisk = 100;
            }
            else if (Metrics.NetIncome < 0)
            {
                cashRisk = 45 + Math.Max(0, 120 - Metrics.CashRunwayDays) / 2;
                if (Metrics.CashRunwayDays <= 30)
                {
                    cashRisk = Math.Max(cashRisk, 90);
                }
                else if (Metrics.CashRunwayDays <= 60)
                {
                    cashRisk = Math.Max(cashRisk, 72);
                }
            }

            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, cashRisk, "\u73b0\u91d1", "\u63a7\u9884\u7b97\u6216\u6269\u7a0e\u57fa");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Metrics.DebtPressure, Math.Max(0, 65 - Metrics.FiscalHealth) + Math.Max(0, -Metrics.NetIncome) / 60), "\u8d22\u653f", "\u964d\u8d64\u5b57\u548c\u503a\u52a1");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Math.Max(Metrics.UtilityUtilization - 75, Metrics.WastewaterUtilization - 75), Math.Max(0, 92 - Metrics.UtilityReliability) + Math.Max(0, 70 - Metrics.WastewaterReliability)), "\u6c34\u7535\u6c61\u6c34", "\u8865\u7535\u6c34\u548c\u6c61\u6c34");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Metrics.DisasterRisk, Math.Max(Metrics.FloodRisk, Math.Max(0, 70 - Metrics.StormwaterResilience) + Math.Max(0, 45 - Metrics.DisasterPreparedness))), "\u96e8\u6d2a\u707e\u5907", "\u8865\u96e8\u6d2a\u6216\u907f\u96be");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Math.Max(Metrics.HealthRisk, Metrics.PatientBacklog), Math.Max(Metrics.FireRisk, Math.Max(Metrics.CaseBacklog, 55 - Metrics.EmergencyResponse))), "\u533b\u7597\u5b89\u5168", "\u8865\u533b\u7597\u6d88\u9632\u8b66\u52a1");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Math.Max(Metrics.Congestion, Metrics.RoadBottleneckPressure), Math.Max(Metrics.AccidentRisk, Metrics.TransitWaitPressure)), "\u4ea4\u901a", "\u5347\u4e3b\u5e72\u6216\u8865\u516c\u4ea4");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Metrics.ServiceGapPressure, Math.Max(0, 60 - Metrics.ServiceEquity) + Math.Max(0, Metrics.ServiceUtilization - 100)), ForecastServiceFocus(), ForecastServiceAction());
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Math.Max(0, 75 - Metrics.GoodsBalance), Math.Max(0, 65 - Metrics.SupplyChainStability) + Math.Max(0, Metrics.LogisticsUtilization - 100)), "\u4f9b\u5e94\u94fe", "\u8865\u8d27\u8fd0\u4ed3\u50a8\u6216\u8d44\u6e90");
            AddForecastRiskCandidate(ref bestRisk, ref focus, ref action, Math.Max(Math.Max(0, 60 - Metrics.LivingCondition), Math.Max(Metrics.LivingPressure, Math.Max(Metrics.RentPressure, Metrics.CrimePressure))), "\u5b9c\u5c45", "\u964d\u751f\u6d3b\u538b\u529b");

            return ClampToScore(bestRisk);
        }

        private string ForecastServiceFocus()
        {
            if (!string.IsNullOrEmpty(Metrics.ServiceGapFocus) && Metrics.ServiceGapFocus != "\u5747\u8861")
            {
                return Metrics.ServiceGapFocus;
            }

            return "\u670d\u52a1\u516c\u5e73";
        }

        private string ForecastServiceAction()
        {
            if (!string.IsNullOrEmpty(Metrics.ServiceGapFocus) && Metrics.ServiceGapFocus != "\u5747\u8861")
            {
                return "\u8865" + Metrics.ServiceGapFocus + "\u8986\u76d6";
            }

            return "\u8865\u516c\u5171\u670d\u52a1\u5bb9\u91cf";
        }

        private int ServiceGapAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeServiceGapAdvisor(out focus, out driver, out action);
            Metrics.ServiceGapAdvisorFocus = focus;
            Metrics.ServiceGapAdvisorDriver = driver;
            Metrics.ServiceGapAdvisorAction = action;
            return score;
        }

        private int ComputeServiceGapAdvisor(out string focus, out string driver, out string action)
        {
            var bestScore = 0;
            focus = "\u5747\u8861";
            driver = "\u8986\u76d6\u53ef\u63a7";
            action = "\u7ee7\u7eed\u8865\u9f50\u5f53\u524d\u77ed\u677f";

            var equityScore = Math.Max(Metrics.ServiceGapPressure, Math.Max(0, 65 - Metrics.ServiceEquity) + Math.Max(0, Metrics.ServiceUtilization - 100));
            equityScore = Math.Max(equityScore, Metrics.UnderservedResidents / 5);
            if (Metrics.Population < 140 && Metrics.ServiceGapPressure <= 0)
            {
                equityScore = Math.Min(equityScore, 40);
            }

            AddServiceGapAdvisorCandidate(ref bestScore, ref focus, ref driver, ref action, equityScore, ServiceGapAdvisorEquityFocus(), ServiceGapAdvisorEquityDriver(), ForecastServiceAction());

            var parkScore = Metrics.Population > 30 ? Math.Max(0, 45 - Metrics.ParkCoverage) + ServiceGapFocusBoost("\u516c\u56ed") : 0;
            if (Metrics.Population > 80 && Metrics.ParkCoverage < 25)
            {
                parkScore = Math.Max(parkScore, 62 + (25 - Metrics.ParkCoverage) / 2);
            }

            AddServiceGapAdvisorCandidate(ref bestScore, ref focus, ref driver, ref action, parkScore, "\u516c\u56ed", "\u8986" + Metrics.ParkCoverage + "/\u5b9c\u5c45" + Metrics.LivingCondition, "\u8865\u516c\u56ed\u6216\u5e7f\u573a\u8986\u76d6");

            var healthScore = Metrics.Population >= 120 ? Math.Max(Math.Max(0, 45 - Metrics.HealthCoverage) + 20, Math.Max(Metrics.HealthRisk, Metrics.PatientBacklog)) : 0;
            healthScore = Math.Max(healthScore, Math.Max(0, Metrics.HealthUtilization - 95) + ServiceGapFocusBoost("\u533b\u7597"));
            if (Metrics.Population >= 180 && Metrics.MedicalResponse < 50)
            {
                healthScore = Math.Max(healthScore, 55 + (50 - Metrics.MedicalResponse));
            }

            AddServiceGapAdvisorCandidate(ref bestScore, ref focus, ref driver, ref action, healthScore, "\u533b\u7597", ServiceGapAdvisorHealthDriver(), "\u8865\u8bca\u6240/\u533b\u9662\u964d\u79ef\u538b");

            var educationScore = Metrics.Population >= 260 ? Math.Max(Math.Max(0, 45 - Metrics.EducationCoverage) + 18, Math.Max(Metrics.StudentBacklog, Math.Max(0, Metrics.EducationUtilization - 95))) : 0;
            educationScore = Math.Max(educationScore, Math.Max(0, 45 - Metrics.LearningPipeline) + ServiceGapFocusBoost("\u6559\u80b2"));
            AddServiceGapAdvisorCandidate(ref bestScore, ref focus, ref driver, ref action, educationScore, "\u6559\u80b2", ServiceGapAdvisorEducationDriver(), "\u8865\u5b66\u6821/\u793e\u533a\u5b66\u9662");

            var fireScore = Metrics.Population >= 200 ? Math.Max(Math.Max(0, 45 - Metrics.SafetyCoverage) + 18, Math.Max(Metrics.FireRisk, Math.Max(0, 45 - Metrics.FireProtection) + 15)) : 0;
            fireScore = Math.Max(fireScore, Math.Max(0, Metrics.FireUtilization - 95) + ServiceGapFocusBoost("\u6d88\u9632"));
            if (Metrics.Population >= 220 && Metrics.FireResponse < 50)
            {
                fireScore = Math.Max(fireScore, 55 + (50 - Metrics.FireResponse));
            }

            AddServiceGapAdvisorCandidate(ref bestScore, ref focus, ref driver, ref action, fireScore, "\u6d88\u9632", ServiceGapAdvisorFireDriver(), "\u8865\u6d88\u9632\u8986\u76d6\u548c\u54cd\u5e94");

            var policeScore = Metrics.Population >= 220 ? Math.Max(Math.Max(0, 45 - Metrics.SecurityCoverage) + 18, Math.Max(Metrics.CrimePressure, Metrics.CaseBacklog)) : 0;
            policeScore = Math.Max(policeScore, Math.Max(0, Metrics.SecurityUtilization - 95) + ServiceGapFocusBoost("\u8b66\u52a1"));
            if (Metrics.Population >= 240 && Metrics.PoliceResponse < 50)
            {
                policeScore = Math.Max(policeScore, 55 + (50 - Metrics.PoliceResponse));
            }

            AddServiceGapAdvisorCandidate(ref bestScore, ref focus, ref driver, ref action, policeScore, "\u8b66\u52a1", ServiceGapAdvisorPoliceDriver(), "\u8865\u8b66\u52a1\u8986\u76d6\u964d\u79ef\u6848");

            return ClampToScore(bestScore);
        }

        private static void AddServiceGapAdvisorCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string ServiceGapAdvisorEquityFocus()
        {
            if (!string.IsNullOrEmpty(Metrics.ServiceGapFocus) && Metrics.ServiceGapFocus != "\u5747\u8861")
            {
                return Metrics.ServiceGapFocus;
            }

            return "\u7247\u533a\u516c\u670d";
        }

        private string ServiceGapAdvisorEquityDriver()
        {
            if (Metrics.ServiceGapPressure > 35)
            {
                return "\u7f3a" + Metrics.ServiceGapPressure + "/" + ForecastPartOrFallback(Metrics.ServiceGapFocus, "\u5747\u8861");
            }

            if (Metrics.ServiceEquity < 55)
            {
                return "\u516c\u5e73" + Metrics.ServiceEquity + "/\u672a\u670d" + Metrics.UnderservedResidents;
            }

            return "\u516c\u670d\u6ee1" + Metrics.ServiceUtilization + "/\u8986" + Metrics.ServiceCoverage;
        }

        private string ServiceGapAdvisorHealthDriver()
        {
            if (Metrics.HealthCoverage < 45) return "\u533b\u8986" + Metrics.HealthCoverage;
            if (Metrics.HealthUtilization > 100) return "\u533b\u6ee1" + Metrics.HealthUtilization;
            if (Metrics.MedicalResponse < 50) return "\u533b\u54cd" + Metrics.MedicalResponse;
            return "\u5065\u9669" + Metrics.HealthRisk + "/\u60a3" + Metrics.PatientBacklog;
        }

        private string ServiceGapAdvisorEducationDriver()
        {
            if (Metrics.EducationCoverage < 45) return "\u5b66\u8986" + Metrics.EducationCoverage;
            if (Metrics.EducationUtilization > 100) return "\u5b66\u6ee1" + Metrics.EducationUtilization;
            if (Metrics.LearningPipeline < 45) return "\u80b2\u6210" + Metrics.LearningPipeline;
            return "\u79ef" + Metrics.StudentBacklog + "/\u9ad8" + Metrics.AdvancedEducationCoverage;
        }

        private string ServiceGapAdvisorFireDriver()
        {
            if (Metrics.SafetyCoverage < 45) return "\u6d88\u8986" + Metrics.SafetyCoverage;
            if (Metrics.FireProtection < 45) return "\u4fdd" + Metrics.FireProtection;
            if (Metrics.FireUtilization > 100) return "\u6d88\u6ee1" + Metrics.FireUtilization;
            return "\u706b\u9669" + Metrics.FireRisk + "/\u54cd" + Metrics.FireResponse;
        }

        private string ServiceGapAdvisorPoliceDriver()
        {
            if (Metrics.SecurityCoverage < 45) return "\u8b66\u8986" + Metrics.SecurityCoverage;
            if (Metrics.SecurityUtilization > 100) return "\u8b66\u6ee1" + Metrics.SecurityUtilization;
            if (Metrics.PoliceResponse < 50) return "\u8b66\u54cd" + Metrics.PoliceResponse;
            return "\u6cbb\u5b89" + Metrics.CrimePressure + "/\u6848" + Metrics.CaseBacklog;
        }

        private int ServiceGapFocusBoost(string marker)
        {
            return !string.IsNullOrEmpty(Metrics.ServiceGapFocus) && Metrics.ServiceGapFocus.Contains(marker) ? 12 : 0;
        }

        private int GrowthBottleneckAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeGrowthBottleneckAdvice(out focus, out driver, out action);
            Metrics.GrowthBottleneckFocus = focus;
            Metrics.GrowthBottleneckDriver = driver;
            Metrics.GrowthBottleneckAction = action;
            return score;
        }

        private int ComputeGrowthBottleneckAdvice(out string focus, out string driver, out string action)
        {
            // GROWTH_BOTTLENECK_ADVISOR converts existing city systems into one next-best growth fix.
            var bestScore = 0;
            focus = "\u5e73\u7a33";
            driver = "\u52a8\u80fd\u53ef\u63a7";
            action = "\u7ee7\u7eed\u8865\u9f50\u5f53\u524d\u77ed\u677f";

            var housingGap = Math.Max(0, Metrics.Population + 24 - Metrics.HousingCapacity);
            var housingScore = Math.Max(Metrics.RentPressure, housingGap * 3 + Math.Max(0, 62 - Metrics.LivingCondition));
            if (Metrics.Population < 80 && housingGap <= 0)
            {
                housingScore = Math.Min(housingScore, 35);
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, housingScore, "\u4f4f\u623f\u6269\u5bb9", GrowthHousingDriver(housingGap), Metrics.RentPressure > 65 ? "\u8865\u4f4f\u5b85/\u516c\u5bd3\u964d\u79df\u538b" : "\u8865\u4f4f\u623f\u5e76\u63d0\u5b9c\u5c45");

            var fiscalScore = Math.Max(Metrics.BudgetStress, Math.Max(Metrics.DebtPressure, Math.Max(0, 62 - Metrics.FiscalHealth)));
            if (Metrics.Cash < 0)
            {
                fiscalScore = 100;
            }
            else if (Metrics.NetIncome < 0)
            {
                fiscalScore = Math.Max(fiscalScore, Metrics.CashRunwayDays <= 45 ? 90 : 68);
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, fiscalScore, "\u8d22\u653f\u7eed\u822a", GrowthFiscalDriver(), ForecastPartOrFallback(Metrics.BudgetAction, "\u63a7\u652f\u51fa\u5e76\u6269\u7a0e\u57fa"));

            var mobilityScore = Math.Max(Metrics.RoadHierarchyPressure, Math.Max(Metrics.Congestion, Metrics.RoadBottleneckPressure));
            mobilityScore = Math.Max(mobilityScore, Math.Max(0, 55 - Metrics.CommuteEfficiency) + Math.Max(0, Metrics.TransitWaitPressure - 40) / 2);
            if (Metrics.RoadTiles < 12 && Metrics.Population < 120)
            {
                mobilityScore = Math.Min(mobilityScore, 42);
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, mobilityScore, "\u901a\u52e4\u74f6\u9888", GrowthMobilityDriver(), GrowthMobilityAction());

            var serviceScore = Math.Max(Metrics.ServiceGapAdvisorScore, Math.Max(Metrics.ServiceGapPressure, Math.Max(0, 62 - Metrics.ServiceEquity)));
            serviceScore = Math.Max(serviceScore, Math.Max(0, Metrics.ServiceUtilization - 95) + Metrics.UnderservedResidents / 8);
            if (Metrics.Population < 140 && Metrics.ServiceGapPressure <= 0)
            {
                serviceScore = Math.Min(serviceScore, 42);
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, serviceScore, "\u516c\u670d\u77ed\u677f", GrowthServiceDriver(), ForecastServiceAction());

            var utilityScore = Math.Max(Math.Max(0, Metrics.UtilityUtilization - 45), Math.Max(0, Metrics.WastewaterUtilization - 45));
            utilityScore = Math.Max(utilityScore, Math.Max(Math.Max(0, 95 - Metrics.UtilityReliability), Math.Max(0, 75 - Metrics.WastewaterReliability)));
            utilityScore = Math.Max(utilityScore, Math.Max(Metrics.FloodRisk, Math.Max(0, Metrics.StormwaterUtilization - 45)));
            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, utilityScore, "\u57fa\u7840\u8bbe\u65bd", GrowthUtilityDriver(), "\u8865\u7535\u6c34/\u6c61\u6c34/\u96e8\u6d2a\u5bb9\u91cf");

            var jobsGap = Math.Max(0, (int)Math.Round(Metrics.Population * 0.52) - Metrics.Jobs);
            var economyScore = Math.Max(Metrics.Unemployment, Math.Max(Metrics.LaborShortage, Math.Max(0, 52 - Metrics.WorkforceSkill)));
            economyScore = Math.Max(economyScore, jobsGap / 3);
            economyScore = Math.Max(economyScore, Math.Max(0, 55 - Metrics.BusinessEfficiency));
            if (Metrics.Population < 120 && jobsGap < 16)
            {
                economyScore = Math.Min(economyScore, 40);
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, economyScore, "\u5c31\u4e1a/\u4eba\u624d", GrowthEconomyDriver(jobsGap), GrowthEconomyAction());

            var goodsScore = 0;
            if (Metrics.GoodsDemand > 0)
            {
                goodsScore = Math.Max(goodsScore, Math.Max(0, 78 - Metrics.GoodsBalance));
            }

            goodsScore = Math.Max(goodsScore, Math.Max(0, 62 - Metrics.SupplyChainStability));
            goodsScore = Math.Max(goodsScore, Math.Max(0, Metrics.LogisticsUtilization - 95));
            if (Metrics.GoodsDemand <= 0 && Metrics.Jobs < 120)
            {
                goodsScore = 0;
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, goodsScore, "\u4f9b\u5e94\u94fe", GrowthGoodsDriver(), "\u8865\u8d27\u8fd0/\u4ed3\u50a8/\u8d44\u6e90\u94fe");

            var livabilityScore = Math.Max(Math.Max(0, 62 - Metrics.LivingCondition), Metrics.LivingPressure);
            livabilityScore = Math.Max(livabilityScore, Math.Max(Math.Max(0, 60 - Metrics.EnvironmentQuality), Metrics.HealthRisk));
            livabilityScore = Math.Max(livabilityScore, Math.Max(Metrics.CrimePressure, Metrics.NoiseStress));
            if (Metrics.Population < 160)
            {
                livabilityScore = Math.Min(livabilityScore, 48);
            }

            AddGrowthBottleneckCandidate(ref bestScore, ref focus, ref driver, ref action, livabilityScore, "\u5b9c\u5c45\u7559\u4eba", GrowthLivabilityDriver(), "\u964d\u751f\u6d3b\u538b\u529b\u5e76\u8865\u516c\u56ed\u5065\u5eb7");

            return ClampToScore(bestScore);
        }

        private static void AddGrowthBottleneckCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string GrowthHousingDriver(int housingGap)
        {
            if (housingGap > 0) return "\u623f\u7f3a" + housingGap + "/\u79df" + Metrics.RentPressure;
            if (Metrics.RentPressure > 65) return "\u79df\u538b" + Metrics.RentPressure + "/\u5bb9" + Metrics.HousingCapacity;
            return "\u5b9c\u5c45" + Metrics.LivingCondition + "/\u538b" + Metrics.LivingPressure;
        }

        private string GrowthFiscalDriver()
        {
            if (Metrics.NetIncome < 0) return "\u51c0" + FormatSigned(Metrics.NetIncome) + "/\u73b0" + Metrics.CashRunwayDays + "\u5929";
            if (Metrics.DebtPressure > 45) return "\u503a\u538b" + Metrics.DebtPressure + "/\u4ed8" + Metrics.BondPayment;
            return "\u8d22\u4fe1" + Metrics.FiscalHealth + "/\u9884\u538b" + Metrics.BudgetStress;
        }

        private string GrowthMobilityDriver()
        {
            if (Metrics.RoadHierarchyPressure > 55) return ForecastPartOrFallback(Metrics.RoadHierarchyFocus, "\u8def\u7f51") + Metrics.RoadHierarchyPressure;
            if (Metrics.CommuteEfficiency < 55) return "\u901a\u52e4" + Metrics.CommuteEfficiency + "/\u8f66" + Metrics.CarDependency;
            if (Metrics.TransitWaitPressure > 50) return "\u5019\u8f66" + Metrics.TransitWaitPressure + "/\u6ee1" + Metrics.TransitUtilization;
            return "\u5835" + Metrics.Congestion + "/\u74f6" + Metrics.RoadBottleneckPressure;
        }

        private string GrowthMobilityAction()
        {
            if (!string.IsNullOrEmpty(Metrics.RoadHierarchyAction) && Metrics.RoadHierarchyPressure >= 55)
            {
                return Metrics.RoadHierarchyAction;
            }

            if (Metrics.TransitWaitPressure > 50 || Metrics.TransitUtilization > 110) return "\u8865\u516c\u4ea4/\u5730\u94c1\u8fd0\u529b";
            if (Metrics.ParkingPressure > 60) return "\u8865\u505c\u8f66\u5e76\u964d\u8f66\u4f9d\u8d56";
            return "\u5347\u4e3b\u5e72\u758f\u901a\u74f6\u9888";
        }

        private string GrowthServiceDriver()
        {
            if (Metrics.ServiceGapAdvisorScore > 55) return ForecastPartOrFallback(Metrics.ServiceGapAdvisorFocus, "\u516c\u670d") + Metrics.ServiceGapAdvisorScore;
            if (Metrics.ServiceGapPressure > 35) return "\u7f3a\u53e3" + Metrics.ServiceGapPressure + "/" + ForecastPartOrFallback(Metrics.ServiceGapFocus, "\u5747\u8861");
            return "\u516c\u5e73" + Metrics.ServiceEquity + "/\u672a\u670d" + Metrics.UnderservedResidents;
        }

        private string GrowthUtilityDriver()
        {
            if (Metrics.UtilityUtilization > 105 || Metrics.UtilityReliability < 90) return "\u6c34\u7535\u6ee1" + Metrics.UtilityUtilization + "/\u7a33" + Metrics.UtilityReliability;
            if (Metrics.WastewaterUtilization > 105 || Metrics.WastewaterReliability < 75) return "\u6c61\u6c34\u6ee1" + Metrics.WastewaterUtilization + "/\u7a33" + Metrics.WastewaterReliability;
            return "\u96e8\u6d2a\u6ee1" + Metrics.StormwaterUtilization + "/\u6d9d" + Metrics.FloodRisk;
        }

        private string GrowthEconomyDriver(int jobsGap)
        {
            if (jobsGap > 0) return "\u5c97\u7f3a" + jobsGap + "/\u5931" + Metrics.Unemployment;
            if (Metrics.LaborShortage > 45) return "\u7528\u5de5" + Metrics.LaborShortage + "/\u4eba\u624d" + Metrics.WorkforceSkill;
            return "\u4f01\u6548" + Metrics.BusinessEfficiency + "/\u521b" + Metrics.InnovationCapacity;
        }

        private string GrowthEconomyAction()
        {
            if (Metrics.WorkforceSkill < 45 || Metrics.AdvancedEducationCoverage < 35) return "\u8865\u5b66\u6821/\u793e\u533a\u5b66\u9662";
            if (Metrics.LaborShortage > 45) return "\u8865\u4f4f\u623f\u5e76\u63d0\u901a\u52e4";
            return "\u8865\u529e\u516c\u5546\u4e1a\u5c31\u4e1a";
        }

        private string GrowthGoodsDriver()
        {
            if (Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 75) return "\u5e73\u8861" + Metrics.GoodsBalance + "/\u9700" + Metrics.GoodsDemand;
            if (Metrics.SupplyChainStability < 60) return "\u4f9b\u7a33" + Metrics.SupplyChainStability + "/\u4ed3" + Metrics.GoodsStorage;
            return "\u8d27\u6ee1" + Metrics.LogisticsUtilization + "/\u672c" + Metrics.LocalGoodsSupply;
        }

        private string GrowthLivabilityDriver()
        {
            if (Metrics.LivingPressure > 55) return "\u751f\u6d3b\u538b" + Metrics.LivingPressure;
            if (Metrics.HealthRisk > 55) return "\u5065\u9669" + Metrics.HealthRisk + "/\u533b" + Metrics.HealthCoverage;
            if (Metrics.CrimePressure > 55) return "\u6cbb\u5b89" + Metrics.CrimePressure;
            return "\u73af\u5883" + Metrics.EnvironmentQuality + "/\u566a" + Metrics.NoiseStress;
        }

        private int HousingAffordabilityAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeHousingAffordabilityAdvice(out focus, out driver, out action);
            Metrics.HousingAffordabilityFocus = focus;
            Metrics.HousingAffordabilityDriver = driver;
            Metrics.HousingAffordabilityAction = action;
            return score;
        }

        private int ComputeHousingAffordabilityAdvice(out string focus, out string driver, out string action)
        {
            // HOUSING_AFFORDABILITY_ADVISOR explains rent, housing supply, and livability blockers without changing formulas.
            var bestScore = 0;
            focus = "\u4f4f\u623f\u5e73\u7a33";
            driver = "\u4f9b\u9700\u53ef\u63a7";
            action = "\u7ee7\u7eed\u89c2\u5bdf\u4f4f\u623f";

            var housingGap = Math.Max(0, Metrics.Population + 24 - Metrics.HousingCapacity);
            var occupancy = Metrics.HousingCapacity <= 0 ? 100 : ClampToScore((int)Math.Round(Metrics.Population * 100.0 / Math.Max(1, Metrics.HousingCapacity)));
            var supplyPressure = 0;
            if (Metrics.Population >= 80 && (housingGap > 0 || occupancy > 88 || Metrics.Demand.Residential > 70))
            {
                supplyPressure = Math.Max(housingGap * 3, Math.Max(0, occupancy - 72) * 2);
                supplyPressure = Math.Max(supplyPressure, Metrics.Demand.Residential);
                if (!HasAutoDevelopmentSite(ZoneType.Residential, "residential_pod") && !HasAutoDevelopmentSite(ZoneType.Residential, "apartment_block"))
                {
                    supplyPressure += 14;
                }
            }

            AddHousingAffordabilityCandidate(ref bestScore, ref focus, ref driver, ref action, supplyPressure, "\u4f4f\u623f\u4f9b\u7ed9", HousingSupplyDriver(housingGap, occupancy), HousingSupplyAction());

            var rentPressure = 0;
            if (Metrics.Population >= 120 && Metrics.RentPressure > 45)
            {
                rentPressure = Metrics.RentPressure + Math.Max(0, Metrics.AverageLandValue - 55) / 2;
                if (taxLevel == CityTaxLevel.High)
                {
                    rentPressure += 12;
                }

                if (!IsPolicyActive(CityPolicy.AffordableHousing) && Metrics.RentPressure >= 65)
                {
                    rentPressure += 8;
                }
            }

            AddHousingAffordabilityCandidate(ref bestScore, ref focus, ref driver, ref action, rentPressure, "\u79df\u91d1\u538b\u529b", HousingRentDriver(), HousingRentAction());

            var livabilityPressure = 0;
            if (Metrics.Population >= 140 && (Metrics.LivingCondition < 60 || Metrics.LivingPressure > 45))
            {
                livabilityPressure = Math.Max(Math.Max(0, 68 - Metrics.LivingCondition), Metrics.LivingPressure);
                livabilityPressure = Math.Max(livabilityPressure, Math.Max(0, 58 - Metrics.ServiceEquity));
                livabilityPressure = Math.Max(livabilityPressure, Math.Max(Metrics.HealthRisk, Math.Max(Metrics.CrimePressure, Metrics.NoiseStress)));
            }

            AddHousingAffordabilityCandidate(ref bestScore, ref focus, ref driver, ref action, livabilityPressure, "\u5b9c\u5c45\u8fc1\u5165", HousingLivabilityDriver(), HousingLivabilityAction());

            var zoningPressure = 0;
            if (Metrics.Population >= 100 && (Metrics.IdleZoneTiles > 10 || Metrics.LandUseEfficiency < 55 || Metrics.ResidentialZoneTiles <= 0 || Metrics.MixedUseZoneTiles <= 0))
            {
                zoningPressure = Math.Max(0, 55 - Metrics.LandUseEfficiency) + Metrics.IdleZoneTiles / 2;
                if (Metrics.ResidentialZoneTiles <= 0 && Metrics.MixedUseZoneTiles <= 0)
                {
                    zoningPressure = Math.Max(zoningPressure, 65);
                }

                if (Metrics.Demand.MixedUse > 65)
                {
                    zoningPressure = Math.Max(zoningPressure, Metrics.Demand.MixedUse - 8);
                }
            }

            AddHousingAffordabilityCandidate(ref bestScore, ref focus, ref driver, ref action, zoningPressure, "\u5206\u533a\u843d\u5730", HousingZoningDriver(), HousingZoningAction());

            var balancePressure = 0;
            if (Metrics.Population >= 160 && (Metrics.JobsHousingBalance < 55 || Metrics.CommuteEfficiency < 55 || (Metrics.Jobs > 90 && Metrics.MixedUseBuildings < 2)))
            {
                balancePressure = Math.Max(0, 68 - Metrics.JobsHousingBalance);
                balancePressure = Math.Max(balancePressure, Math.Max(0, 62 - Metrics.CommuteEfficiency));
                balancePressure += Math.Max(0, 2 - Metrics.MixedUseBuildings) * 5;
            }

            AddHousingAffordabilityCandidate(ref bestScore, ref focus, ref driver, ref action, balancePressure, "\u4f4f\u5c97\u5e73\u8861", HousingBalanceDriver(), HousingBalanceAction());

            return ClampToScore(bestScore);
        }

        private static void AddHousingAffordabilityCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string HousingSupplyDriver(int housingGap, int occupancy)
        {
            if (housingGap > 0) return "\u623f\u7f3a" + housingGap + "/\u5360" + occupancy;
            if (Metrics.HousingCapacity <= Metrics.Population + 12) return "\u5bb9" + Metrics.HousingCapacity + "/\u4eba" + Metrics.Population;
            return "\u4f4f\u9700" + Metrics.Demand.Residential + "/\u79df" + Metrics.RentPressure;
        }

        private string HousingSupplyAction()
        {
            if (Metrics.ResidentialZoneTiles <= 0 && Metrics.MixedUseZoneTiles <= 0) return "\u5212\u4f4f\u5b85/\u6df7\u5408\u5e76\u63a5\u8def";
            if (Metrics.Population >= 180 && !HasAutoDevelopmentSite(ZoneType.Residential, "apartment_block")) return "\u7559\u516c\u5bd3\u5730\u5e76\u63a5\u4e3b\u8def";
            if (!HasAutoDevelopmentSite(ZoneType.Residential, "residential_pod")) return "\u63a5\u8def\u6fc0\u6d3b\u4f4f\u5b85\u533a";
            if (Metrics.HighDensityResidentialBuildings < 2 && Metrics.Population >= 180) return "\u8865\u516c\u5bd3\u627f\u63a5\u4eba\u53e3";
            return "\u8865\u4f4f\u5b85\u5bb9\u91cf";
        }

        private string HousingRentDriver()
        {
            if (taxLevel == CityTaxLevel.High) return "\u7a0e" + TaxRatePercent() + "/\u79df" + Metrics.RentPressure;
            if (Metrics.AverageLandValue > 60) return "\u5730" + Metrics.AverageLandValue + "/\u79df" + Metrics.RentPressure;
            if (!IsPolicyActive(CityPolicy.AffordableHousing)) return "\u79df" + Metrics.RentPressure + "/\u4fdd0";
            return "\u79df" + Metrics.RentPressure + "/\u670d" + Metrics.ServiceEquity;
        }

        private string HousingRentAction()
        {
            if (!IsPolicyActive(CityPolicy.AffordableHousing) && Metrics.Population >= 160) return "\u542f\u4fdd\u969c\u623f\u5e76\u8865\u516c\u5bd3";
            if (taxLevel == CityTaxLevel.High) return "\u964d\u7a0e\u538b\u5e76\u6269\u7a0e\u57fa";
            if (Metrics.TransitCoverage < 35) return "\u63a5\u516c\u4ea4\u964d\u901a\u52e4\u6210\u672c";
            if (Metrics.ServiceEquity < 50) return "\u8865\u516c\u670d\u516c\u5e73\u964d\u79df\u538b";
            return "\u8865\u4f4f\u5b85/\u6df7\u5408\u964d\u79df\u538b";
        }

        private string HousingLivabilityDriver()
        {
            if (Metrics.LivingPressure > 55) return "\u751f\u6d3b\u538b" + Metrics.LivingPressure;
            if (Metrics.ServiceEquity < 50) return "\u516c\u5e73" + Metrics.ServiceEquity + "/\u7f3a" + Metrics.ServiceGapPressure;
            if (Metrics.HealthRisk > 55) return "\u5065\u9669" + Metrics.HealthRisk + "/\u533b" + Metrics.HealthCoverage;
            if (Metrics.CrimePressure > 55) return "\u6cbb\u5b89" + Metrics.CrimePressure;
            return "\u5b9c" + Metrics.LivingCondition + "/\u73af" + Metrics.EnvironmentQuality;
        }

        private string HousingLivabilityAction()
        {
            if (Metrics.ServiceEquity < 50) return "\u8865\u533b\u6559\u6d88\u8b66\u516c\u5e73";
            if (Metrics.HealthRisk > 55) return "\u8865\u533b\u7597\u56de\u6536\u964d\u5065\u9669";
            if (Metrics.CrimePressure > 55) return "\u8865\u8b66\u52a1\u7167\u660e\u964d\u6cbb\u5b89";
            if (Metrics.NoiseStress > 55 || Metrics.EnvironmentQuality < 45) return "\u8865\u516c\u56ed\u5e76\u964d\u566a\u6c61";
            return "\u8865\u516c\u56ed\u5065\u5eb7\u63d0\u5b9c\u5c45";
        }

        private string HousingZoningDriver()
        {
            if (Metrics.ResidentialZoneTiles <= 0) return "\u4f4f\u533a0/\u9700" + Metrics.Demand.Residential;
            if (Metrics.IdleZoneTiles > 10) return "\u7a7a" + Metrics.IdleZoneTiles + "/\u6548" + Metrics.LandUseEfficiency;
            if (Metrics.MixedUseZoneTiles <= 0 && Metrics.JobsHousingBalance < 60) return "\u6df70/\u4f4f\u5c97" + Metrics.JobsHousingBalance;
            return "\u4f4f" + Metrics.ResidentialZoneTiles + "/\u6df7" + Metrics.MixedUseZoneTiles;
        }

        private string HousingZoningAction()
        {
            if (Metrics.IdleZoneTiles > 12 && Metrics.LandUseEfficiency < 50) return "\u5148\u63a5\u8def\u6fc0\u6d3b\u7a7a\u5206\u533a";
            if (Metrics.Demand.MixedUse > 70 || Metrics.JobsHousingBalance < 55) return "\u5728\u5c31\u4e1a\u8fb9\u7f18\u5212\u6df7\u5408";
            return "\u8865\u5c0f\u4f4f\u5b85\u8857\u533a";
        }

        private string HousingBalanceDriver()
        {
            if (Metrics.JobsHousingBalance < 55) return "\u4f4f\u5c97" + Metrics.JobsHousingBalance + "/\u5c97" + Metrics.Jobs;
            if (Metrics.CommuteEfficiency < 55) return "\u901a" + Metrics.CommuteEfficiency + "/\u8f66" + Metrics.CarDependency;
            return "\u6df7" + Metrics.MixedUseBuildings + "/\u901a" + Metrics.TransitCoverage;
        }

        private string HousingBalanceAction()
        {
            if (Metrics.MixedUseBuildings < 2) return "\u5c31\u4e1a\u8fb9\u7f18\u8865\u6df7\u5408";
            if (Metrics.TransitCoverage < 35) return "\u516c\u4ea4\u8fde\u4f4f\u5b85\u5c31\u4e1a";
            return "\u5728\u5c97\u4f4d\u8fb9\u8865\u4f4f\u623f";
        }

        private static void AddForecastRiskCandidate(ref int bestRisk, ref string focus, ref string action, int risk, string candidateFocus, string candidateAction)
        {
            var normalizedRisk = ClampToScore(risk);
            if (normalizedRisk <= bestRisk)
            {
                return;
            }

            bestRisk = normalizedRisk;
            focus = candidateFocus;
            action = candidateAction;
        }

        private int BudgetBreakdownAdvisor()
        {
            string focus;
            string driver;
            string action;
            var stress = ComputeBudgetBreakdown(out focus, out driver, out action);
            Metrics.BudgetFocus = focus;
            Metrics.BudgetDriver = driver;
            Metrics.BudgetAction = action;
            return stress;
        }

        private int ComputeBudgetBreakdown(out string focus, out string driver, out string action)
        {
            var bestStress = 0;
            focus = "\u5e73\u7a33";
            driver = "\u6536\u652f\u53ef\u63a7";
            action = "\u7ee7\u7eed\u89c2\u5bdf\u9884\u7b97";

            var monthlyExpense = Math.Max(1, Metrics.UpkeepExpense + Metrics.RoadExpense + Math.Max(0, Metrics.PolicyExpense) + Metrics.BondPayment);
            var taxBase = Math.Max(1, Metrics.TaxIncome);

            var cashStress = 0;
            if (Metrics.Cash < 0)
            {
                cashStress = 100;
            }
            else if (Metrics.NetIncome < 0)
            {
                cashStress = 55 + Math.Min(35, -Metrics.NetIncome * 100 / monthlyExpense);
                if (Metrics.CashRunwayDays <= 30)
                {
                    cashStress = Math.Max(cashStress, 88);
                }
                else if (Metrics.CashRunwayDays <= 60)
                {
                    cashStress = Math.Max(cashStress, 74);
                }
            }
            else if (Metrics.Cash < monthlyExpense)
            {
                cashStress = 40 + (monthlyExpense - Math.Max(0, Metrics.Cash)) * 30 / monthlyExpense;
            }

            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, cashStress, "\u73b0\u91d1/\u8d64\u5b57", "\u51c0\u6536\u652f" + FormatSigned(Metrics.NetIncome) + " \u73b0\u91d1" + Metrics.Cash, Metrics.NetIncome < 0 ? "\u6682\u7f13\u6269\u5efa\u5e76\u6269\u7a0e\u57fa" : "\u7559\u8db3\u73b0\u91d1\u7f13\u51b2");

            var debtShare = Metrics.BondPayment * 100 / taxBase;
            var debtStress = Metrics.BondPrincipal <= 0 ? 0 : Math.Max(Metrics.DebtPressure, debtShare + Math.Min(25, Metrics.BondPrincipal / 600));
            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, debtStress, "\u503a\u52a1", "\u503a\u4ed8" + Metrics.BondPayment + "/\u6536" + debtShare + "%", debtShare > 25 ? "\u6682\u505c\u53d1\u503a\u5e76\u964d\u8d64\u5b57" : "\u4fdd\u6301\u503a\u52a1\u670d\u52a1");

            var policyExpense = Math.Max(0, Metrics.PolicyExpense);
            var policyStress = policyExpense * 100 / taxBase + Metrics.PolicyBacklog + Math.Max(0, Metrics.AdministrationUtilization - 100) / 2 + Math.Max(0, 55 - Metrics.AdministrationEfficiency) / 2;
            if (Metrics.ActivePolicies.Count == 0 && policyExpense == 0)
            {
                policyStress = 0;
            }

            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, policyStress, "\u653f\u7b56\u6267\u884c", "\u653f\u7b56" + Metrics.ActivePolicies.Count + "/\u652f" + policyExpense + "/\u79ef" + Metrics.PolicyBacklog, BudgetPolicyAction(policyExpense, taxBase));

            var serviceStress = Math.Max(Math.Max(0, 65 - Metrics.MaintenanceCondition), Math.Max(0, Metrics.ServiceUtilization - 100));
            serviceStress = Math.Max(serviceStress, Metrics.ServiceGapPressure / 2 + Math.Max(0, 65 - Metrics.ServiceEquity) / 2);
            if (Metrics.Population < 120)
            {
                serviceStress = 0;
            }

            if (Metrics.NetIncome < 0)
            {
                serviceStress = Math.Max(serviceStress, Math.Max(0, Metrics.UpkeepExpense + Metrics.ServiceBudgetExpense) * 100 / taxBase / 2);
            }

            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, serviceStress, "\u7ef4\u62a4/\u516c\u670d", BudgetServiceDriver(), BudgetServiceAction());

            var utilityStress = Math.Max(Math.Max(0, Metrics.UtilityUtilization - 85), Math.Max(0, 92 - Metrics.UtilityReliability));
            utilityStress = Math.Max(utilityStress, Math.Max(Math.Max(0, Metrics.WastewaterUtilization - 95), Math.Max(0, 70 - Metrics.WastewaterReliability)));
            utilityStress = Math.Max(utilityStress, Math.Max(Math.Max(0, Metrics.StormwaterUtilization - 95), Math.Max(Metrics.FloodRisk, Math.Max(0, 70 - Metrics.StormwaterResilience))));
            if (Metrics.Population < 80 && Metrics.UtilityUtilization <= 100 && Metrics.WastewaterUtilization <= 100)
            {
                utilityStress = 0;
            }

            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, utilityStress, "\u6c34\u7535/\u6c61\u6c34/\u96e8\u6d2a", BudgetUtilityDriver(), "\u8865\u7535\u6c34\u6c61\u6c34\u96e8\u6d2a\u5bb9\u91cf");

            var networkStress = Math.Max(Math.Max(0, Metrics.TransitUtilization - 95) + Math.Max(0, 35 - Metrics.TransitCoverage), Math.Max(0, Metrics.LogisticsUtilization - 95) + Math.Max(0, 35 - Metrics.LogisticsCoverage));
            networkStress = Math.Max(networkStress, Math.Max(0, Metrics.CommunicationUtilization - 95) + Math.Max(0, 40 - Metrics.CommunicationCoverage));
            networkStress = Math.Max(networkStress, Math.Max(0, Metrics.MailUtilization - 95) + Math.Max(0, 40 - Metrics.MailCoverage));
            if (Metrics.Population < 180 && Metrics.Jobs < 120)
            {
                networkStress = 0;
            }

            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, networkStress, BudgetNetworkFocus(), BudgetNetworkDriver(), BudgetNetworkAction());

            var roadOpsStress = Metrics.RoadTiles >= 18 || Metrics.Population >= 180 ? Math.Max(0, 60 - Metrics.RoadMaintenanceCoverage) + Metrics.AccidentRisk / 3 + Metrics.RoadBottleneckPressure / 4 : 0;
            var parkingOpsStress = Metrics.Population >= 160 ? Math.Max(Metrics.ParkingPressure, Math.Max(0, Metrics.ParkingUtilization - 95) + Math.Max(0, 45 - Metrics.ParkingCoverage)) : 0;
            var wasteOpsStress = Metrics.Population >= 220 ? Math.Max(Math.Max(0, Metrics.WasteUtilization - 95), Math.Max(0, 70 - Metrics.WasteReliability) + Math.Max(0, 45 - Metrics.WasteCoverage)) : 0;
            var streetOpsStress = Math.Max(roadOpsStress, Math.Max(parkingOpsStress, wasteOpsStress));
            AddBudgetBreakdownCandidate(ref bestStress, ref focus, ref driver, ref action, streetOpsStress, BudgetStreetOpsFocus(roadOpsStress, parkingOpsStress, wasteOpsStress), BudgetStreetOpsDriver(roadOpsStress, parkingOpsStress, wasteOpsStress), BudgetStreetOpsAction(roadOpsStress, parkingOpsStress, wasteOpsStress));

            return ClampToScore(bestStress);
        }

        private static void AddBudgetBreakdownCandidate(ref int bestStress, ref string focus, ref string driver, ref string action, int stress, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedStress = ClampToScore(stress);
            if (normalizedStress <= bestStress)
            {
                return;
            }

            bestStress = normalizedStress;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string BudgetPolicyAction(int policyExpense, int taxBase)
        {
            if (Metrics.AdministrationUtilization > 115 || Metrics.PolicyBacklog > 55)
            {
                return "\u5efa\u5e02\u653f\u5385\u63d0\u884c\u653f\u5bb9\u91cf";
            }

            if (policyExpense > Math.Max(50, taxBase / 2))
            {
                return "\u5173\u4f4e\u6548\u653f\u7b56\u63a7\u652f\u51fa";
            }

            return "\u4fdd\u7559\u9ad8\u6536\u76ca\u653f\u7b56";
        }

        private string BudgetServiceDriver()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted && Metrics.NetIncome < 0)
            {
                return "\u52a0\u5f3a\u9884\u7b97\u63a8\u9ad8\u8d64\u5b57";
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean && (Metrics.ServiceCoverage < 55 || Metrics.ServiceGapPressure > 35))
            {
                return "\u7cbe\u7b80\u9884\u7b97\u538b\u4f4e\u670d\u52a1";
            }

            if (Metrics.MaintenanceCondition < 55)
            {
                return "\u7ef4\u62a4\u72b6\u6001" + Metrics.MaintenanceCondition;
            }

            if (Metrics.ServiceUtilization > 110)
            {
                return "\u516c\u670d\u6ee1\u8f7d" + Metrics.ServiceUtilization;
            }

            return "\u7f3a\u53e3:" + Metrics.ServiceGapFocus;
        }

        private string BudgetServiceAction()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted && Metrics.NetIncome < 0)
            {
                return "\u56de\u6807\u51c6\u9884\u7b97\u5e76\u8865\u5173\u952e\u5bb9\u91cf";
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean && (Metrics.ServiceCoverage < 55 || Metrics.ServiceGapPressure > 35))
            {
                return "\u6062\u590d\u6807\u51c6\u9884\u7b97\u4fdd\u57fa\u7840";
            }

            return ForecastServiceAction();
        }

        private string BudgetUtilityDriver()
        {
            if (Metrics.UtilityUtilization > 95 || Metrics.UtilityReliability < 90)
            {
                return "\u6c34\u7535\u6ee1" + Metrics.UtilityUtilization + "/\u7a33" + Metrics.UtilityReliability;
            }

            if (Metrics.WastewaterUtilization > 95 || Metrics.WastewaterReliability < 70)
            {
                return "\u6c61\u6c34\u6ee1" + Metrics.WastewaterUtilization;
            }

            return "\u96e8\u6d2a\u6ee1" + Metrics.StormwaterUtilization + "/\u6d9d" + Metrics.FloodRisk;
        }

        private string BudgetNetworkFocus()
        {
            var transit = Math.Max(0, Metrics.TransitUtilization - 95) + Math.Max(0, 35 - Metrics.TransitCoverage);
            var logistics = Math.Max(0, Metrics.LogisticsUtilization - 95) + Math.Max(0, 35 - Metrics.LogisticsCoverage);
            var communication = Math.Max(0, Metrics.CommunicationUtilization - 95) + Math.Max(0, 40 - Metrics.CommunicationCoverage);
            var mail = Math.Max(0, Metrics.MailUtilization - 95) + Math.Max(0, 40 - Metrics.MailCoverage);
            if (transit >= logistics && transit >= communication && transit >= mail) return "\u516c\u4ea4";
            if (logistics >= communication && logistics >= mail) return "\u8d27\u8fd0";
            if (communication >= mail) return "\u901a\u4fe1";
            return "\u90ae\u653f";
        }

        private string BudgetNetworkDriver()
        {
            var focusText = BudgetNetworkFocus();
            if (focusText == "\u516c\u4ea4") return "\u516c\u4ea4\u6ee1" + Metrics.TransitUtilization + "/\u7b49" + Metrics.TransitWaitPressure;
            if (focusText == "\u8d27\u8fd0") return "\u8d27\u8fd0\u6ee1" + Metrics.LogisticsUtilization + "/\u4f9b\u5e94" + Metrics.SupplyChainStability;
            if (focusText == "\u901a\u4fe1") return "\u901a\u4fe1\u6ee1" + Metrics.CommunicationUtilization + "/\u6548" + Metrics.BusinessEfficiency;
            return "\u90ae\u653f\u6ee1" + Metrics.MailUtilization + "/\u7a33" + Metrics.MailReliability;
        }

        private string BudgetNetworkAction()
        {
            var focusText = BudgetNetworkFocus();
            if (focusText == "\u516c\u4ea4") return "\u8865\u516c\u4ea4/\u5730\u94c1\u8fd0\u529b";
            if (focusText == "\u8d27\u8fd0") return "\u8865\u8d27\u8fd0/\u4ed3\u50a8\u8282\u70b9";
            if (focusText == "\u901a\u4fe1") return "\u8865\u901a\u4fe1\u8986\u76d6\u548c\u5bb9\u91cf";
            return "\u8865\u90ae\u653f\u8282\u70b9\u964d\u914d\u9001\u538b";
        }

        private static string BudgetStreetOpsFocus(int roadOpsStress, int parkingOpsStress, int wasteOpsStress)
        {
            if (roadOpsStress >= parkingOpsStress && roadOpsStress >= wasteOpsStress) return "\u9053\u8def\u517b\u62a4";
            if (parkingOpsStress >= wasteOpsStress) return "\u505c\u8f66";
            return "\u56de\u6536";
        }

        private string BudgetStreetOpsDriver(int roadOpsStress, int parkingOpsStress, int wasteOpsStress)
        {
            var focusText = BudgetStreetOpsFocus(roadOpsStress, parkingOpsStress, wasteOpsStress);
            if (focusText == "\u9053\u8def\u517b\u62a4") return "\u517b\u62a4" + Metrics.RoadMaintenanceCoverage + "/\u4e8b\u6545" + Metrics.AccidentRisk;
            if (focusText == "\u505c\u8f66") return "\u505c\u8f66\u538b" + Metrics.ParkingPressure + "/\u6ee1" + Metrics.ParkingUtilization;
            return "\u56de\u6536\u6ee1" + Metrics.WasteUtilization + "/\u7a33" + Metrics.WasteReliability;
        }

        private string BudgetStreetOpsAction(int roadOpsStress, int parkingOpsStress, int wasteOpsStress)
        {
            var focusText = BudgetStreetOpsFocus(roadOpsStress, parkingOpsStress, wasteOpsStress);
            if (focusText == "\u9053\u8def\u517b\u62a4") return "\u8865\u517b\u62a4\u7ad9\u5e76\u5347\u4e3b\u5e72";
            if (focusText == "\u505c\u8f66") return "\u8865\u505c\u8f66\u5e76\u964d\u5c0f\u8f66\u4f9d\u8d56";
            return "\u8865\u56de\u6536\u6216\u5783\u573e\u7535\u5bb9\u91cf";
        }

        private int DistrictPriorityAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeDistrictPriority(out focus, out driver, out action);
            Metrics.DistrictPriorityFocus = focus;
            Metrics.DistrictPriorityDriver = driver;
            Metrics.DistrictPriorityAction = action;
            return score;
        }

        private int ComputeDistrictPriority(out string focus, out string driver, out string action)
        {
            var bestScore = 0;
            focus = "\u5e73\u7a33";
            driver = "\u5404\u7cfb\u7edf\u53ef\u63a7";
            action = "\u7ee7\u7eed\u8865\u9f50\u5f53\u524d\u77ed\u677f";

            var trafficScore = Math.Max(Math.Max(Metrics.Congestion, Metrics.RoadBottleneckPressure), Math.Max(Metrics.IntersectionDelay, Metrics.TransitWaitPressure));
            trafficScore = Math.Max(trafficScore, Math.Max(0, 60 - Metrics.RoadConnectivity) + Math.Max(0, Metrics.ParkingPressure - 50) / 2);
            if (Metrics.RoadTiles < 12 && Metrics.Population < 120)
            {
                trafficScore = Math.Min(trafficScore, 45);
            }

            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, trafficScore, "\u4ea4\u901a\u74f6\u9888", DistrictTrafficDriver(), DistrictTrafficAction());

            var serviceScore = Math.Max(Metrics.ServiceGapPressure, Math.Max(0, 70 - Metrics.ServiceEquity) + Math.Max(0, Metrics.ServiceUtilization - 100));
            serviceScore = Math.Max(serviceScore, Metrics.UnderservedResidents / 6);
            serviceScore = Math.Max(serviceScore, Math.Max(Math.Max(0, 45 - Metrics.HealthCoverage), Math.Max(0, 45 - Metrics.EducationCoverage)));
            if (Metrics.Population < 160 && Metrics.ServiceGapPressure <= 0)
            {
                serviceScore = Math.Min(serviceScore, 45);
            }

            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, serviceScore, DistrictServiceFocus(), DistrictServiceDriver(), ForecastServiceAction());

            var housingGap = Math.Max(0, Metrics.Population + 24 - Metrics.HousingCapacity);
            var housingScore = Math.Max(Metrics.RentPressure, Metrics.LivingPressure + housingGap / 4);
            housingScore = Math.Max(housingScore, Math.Max(0, 60 - Metrics.LivingCondition));
            if (Metrics.Population < 120)
            {
                housingScore = Math.Min(housingScore, 45);
            }

            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, housingScore, "\u4f4f\u623f/\u5c45\u4f4f\u6210\u672c", DistrictHousingDriver(housingGap), Metrics.RentPressure > 65 ? "\u8865\u4f4f\u5b85/\u516c\u5bd3\u5e76\u63a7\u7a0e\u538b" : "\u8865\u5b9c\u5c45\u670d\u52a1\u964d\u751f\u6d3b\u538b");

            var fiscalScore = Math.Max(Metrics.BudgetStress, Math.Max(Metrics.DebtPressure, Math.Max(0, 60 - Metrics.FiscalHealth)));
            if (Metrics.Cash < 0)
            {
                fiscalScore = 100;
            }
            else if (Metrics.NetIncome < 0)
            {
                fiscalScore = Math.Max(fiscalScore, Metrics.CashRunwayDays <= 45 ? 85 : 65);
            }

            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, fiscalScore, "\u8d22\u653f/\u9884\u7b97", DistrictFiscalDriver(), ForecastPartOrFallback(Metrics.BudgetAction, "\u63a7\u652f\u51fa\u5e76\u6269\u7a0e\u57fa"));

            var utilityScore = Math.Max(Math.Max(0, Metrics.UtilityUtilization - 30), Math.Max(0, Metrics.WastewaterUtilization - 30));
            utilityScore = Math.Max(utilityScore, Math.Max(Math.Max(0, Metrics.StormwaterUtilization - 30), Metrics.FloodRisk));
            utilityScore = Math.Max(utilityScore, Math.Max(Math.Max(0, 95 - Metrics.UtilityReliability), Math.Max(0, 75 - Metrics.WastewaterReliability)));
            utilityScore = Math.Max(utilityScore, Math.Max(0, 70 - Metrics.StormwaterResilience));
            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, utilityScore, "\u6c34\u7535\u6c61\u6c34\u96e8\u6d2a", DistrictUtilityDriver(), "\u8865\u7535\u6c34/\u6c61\u6c34/\u96e8\u6d2a\u5bb9\u91cf");

            var safetyScore = Math.Max(Math.Max(Metrics.HealthRisk, Metrics.PatientBacklog), Math.Max(Metrics.FireRisk, Metrics.CaseBacklog));
            safetyScore = Math.Max(safetyScore, Math.Max(Metrics.CrimePressure, Math.Max(0, 60 - Metrics.EmergencyResponse)));
            safetyScore = Math.Max(safetyScore, Math.Max(Math.Max(0, Metrics.HealthUtilization - 45), Math.Max(0, Metrics.FireUtilization - 45)));
            safetyScore = Math.Max(safetyScore, Math.Max(0, Metrics.SecurityUtilization - 45));
            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, safetyScore, DistrictSafetyFocus(), DistrictSafetyDriver(), DistrictSafetyAction());

            var goodsScore = 0;
            if (Metrics.GoodsDemand > 0)
            {
                goodsScore = Math.Max(goodsScore, Math.Max(0, 80 - Metrics.GoodsBalance));
            }

            goodsScore = Math.Max(goodsScore, Math.Max(0, 65 - Metrics.SupplyChainStability));
            goodsScore = Math.Max(goodsScore, Math.Max(0, Metrics.LogisticsUtilization - 45));
            if (Metrics.GoodsDemand <= 0 && Metrics.Jobs < 120)
            {
                goodsScore = 0;
            }

            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, goodsScore, "\u5546\u54c1\u7269\u6d41/\u4f9b\u5e94\u94fe", DistrictGoodsDriver(), "\u8865\u8d27\u8fd0/\u4ed3\u50a8/\u8d44\u6e90\u94fe");

            var livabilityScore = Math.Max(Math.Max(0, 70 - Metrics.EnvironmentQuality), Metrics.NoiseStress);
            livabilityScore = Math.Max(livabilityScore, Math.Max(Metrics.Pollution * 2, Metrics.LivingPressure));
            livabilityScore = Math.Max(livabilityScore, Metrics.LandUseConflict + Math.Max(0, 60 - Metrics.DevelopmentQuality) / 2);
            if (Metrics.Population < 120)
            {
                livabilityScore = Math.Min(livabilityScore, 45);
            }

            AddDistrictPriorityCandidate(ref bestScore, ref focus, ref driver, ref action, livabilityScore, "\u5b9c\u5c45/\u73af\u5883", DistrictLivabilityDriver(), "\u8865\u516c\u56ed\u56de\u6536\u5e76\u964d\u6c61\u67d3\u566a\u58f0");

            return ClampToScore(bestScore);
        }

        private static void AddDistrictPriorityCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string DistrictTrafficDriver()
        {
            if (Metrics.RoadConnectivity < 50) return "\u8def\u7f51" + Metrics.RoadConnectivity + "/\u65ad" + Metrics.DeadEndRoadTiles;
            if (Metrics.IntersectionDelay > Metrics.RoadBottleneckPressure) return "\u8def\u53e3\u5ef6" + Metrics.IntersectionDelay;
            if (Metrics.TransitWaitPressure > 55) return "\u5019\u8f66\u538b" + Metrics.TransitWaitPressure + "/\u516c\u4ea4\u6ee1" + Metrics.TransitUtilization;
            if (Metrics.ParkingPressure > 60) return "\u505c\u8f66\u538b" + Metrics.ParkingPressure;
            return "\u62e5\u5835" + Metrics.Congestion + "/\u74f6" + Metrics.RoadBottleneckPressure;
        }

        private string DistrictTrafficAction()
        {
            if (Metrics.RoadConnectivity < 50 || Metrics.DeadEndRoadTiles > 0) return "\u6253\u901a\u65ad\u5934\u8def\u5e76\u63a5\u4e3b\u5e72";
            if (Metrics.IntersectionDelay > 50) return "\u542f\u7528\u4fe1\u53f7\u6216\u5347\u4e3b\u5e72";
            if (Metrics.TransitWaitPressure > 55 || Metrics.TransitUtilization > 110) return "\u8865\u516c\u4ea4/\u5730\u94c1\u8fd0\u529b";
            if (Metrics.ParkingPressure > 60) return "\u8865\u505c\u8f66\u5e76\u964d\u8f66\u4f9d\u8d56";
            return "\u5347\u4e3b\u5e72\u758f\u901a\u74f6\u9888";
        }

        private string DistrictServiceFocus()
        {
            if (!string.IsNullOrEmpty(Metrics.ServiceGapFocus) && Metrics.ServiceGapFocus != "\u5747\u8861")
            {
                return Metrics.ServiceGapFocus + "\u670d\u52a1\u7f3a\u53e3";
            }

            return "\u670d\u52a1\u516c\u5e73/\u7f3a\u53e3";
        }

        private string DistrictServiceDriver()
        {
            if (Metrics.ServiceGapPressure > 35) return "\u7f3a\u53e3" + Metrics.ServiceGapPressure + "/" + ForecastPartOrFallback(Metrics.ServiceGapFocus, "\u5747\u8861");
            if (Metrics.ServiceUtilization > 105) return "\u516c\u670d\u6ee1" + Metrics.ServiceUtilization;
            return "\u516c\u5e73" + Metrics.ServiceEquity + "/\u672a\u670d" + Metrics.UnderservedResidents;
        }

        private string DistrictHousingDriver(int housingGap)
        {
            if (housingGap > 0) return "\u623f\u7f3a" + housingGap + "/\u79df" + Metrics.RentPressure;
            if (Metrics.RentPressure > 65) return "\u79df\u538b" + Metrics.RentPressure;
            return "\u5b9c\u5c45" + Metrics.LivingCondition + "/\u538b" + Metrics.LivingPressure;
        }

        private string DistrictFiscalDriver()
        {
            if (Metrics.NetIncome < 0) return "\u51c0" + FormatSigned(Metrics.NetIncome) + "/\u73b0" + Metrics.CashRunwayDays + "\u5929";
            if (Metrics.DebtPressure > 45) return "\u503a\u538b" + Metrics.DebtPressure + "/\u4ed8" + Metrics.BondPayment;
            return "\u8d22\u4fe1" + Metrics.FiscalHealth + "/\u9884\u538b" + Metrics.BudgetStress;
        }

        private string DistrictUtilityDriver()
        {
            if (Metrics.UtilityUtilization > 105 || Metrics.UtilityReliability < 90) return "\u6c34\u7535\u6ee1" + Metrics.UtilityUtilization + "/\u7a33" + Metrics.UtilityReliability;
            if (Metrics.WastewaterUtilization > 105 || Metrics.WastewaterReliability < 75) return "\u6c61\u6c34\u6ee1" + Metrics.WastewaterUtilization + "/\u7a33" + Metrics.WastewaterReliability;
            return "\u96e8\u6d2a\u6ee1" + Metrics.StormwaterUtilization + "/\u6d9d" + Metrics.FloodRisk;
        }

        private string DistrictSafetyFocus()
        {
            var medical = Math.Max(Math.Max(Metrics.HealthRisk, Metrics.PatientBacklog), Math.Max(0, Metrics.HealthUtilization - 45));
            var fire = Math.Max(Metrics.FireRisk, Math.Max(0, Metrics.FireUtilization - 45));
            var police = Math.Max(Math.Max(Metrics.CrimePressure, Metrics.CaseBacklog), Math.Max(0, Metrics.SecurityUtilization - 45));
            if (medical >= fire && medical >= police) return "\u533b\u7597/\u516c\u5171\u5065\u5eb7";
            if (fire >= police) return "\u6d88\u9632/\u706b\u9669";
            return "\u8b66\u52a1/\u6cbb\u5b89";
        }

        private string DistrictSafetyDriver()
        {
            var focusText = DistrictSafetyFocus();
            if (focusText == "\u533b\u7597/\u516c\u5171\u5065\u5eb7") return "\u5065\u9669" + Metrics.HealthRisk + "/\u60a3" + Metrics.PatientBacklog;
            if (focusText == "\u6d88\u9632/\u706b\u9669") return "\u706b\u9669" + Metrics.FireRisk + "/\u54cd" + Metrics.FireResponse;
            return "\u6cbb\u5b89" + Metrics.CrimePressure + "/\u6848" + Metrics.CaseBacklog;
        }

        private string DistrictSafetyAction()
        {
            var focusText = DistrictSafetyFocus();
            if (focusText == "\u533b\u7597/\u516c\u5171\u5065\u5eb7") return "\u8865\u8bca\u6240/\u533b\u9662\u964d\u79ef\u538b";
            if (focusText == "\u6d88\u9632/\u706b\u9669") return "\u8865\u6d88\u9632\u8986\u76d6\u548c\u54cd\u5e94";
            return "\u8865\u8b66\u52a1\u8986\u76d6\u964d\u79ef\u6848";
        }

        private string DistrictGoodsDriver()
        {
            if (Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 75) return "\u5e73\u8861" + Metrics.GoodsBalance + "/\u9700" + Metrics.GoodsDemand;
            if (Metrics.SupplyChainStability < 60) return "\u4f9b\u5e94\u7a33" + Metrics.SupplyChainStability;
            return "\u8d27\u8fd0\u6ee1" + Metrics.LogisticsUtilization + "/\u672c" + Metrics.LocalGoodsSupply;
        }

        private string DistrictLivabilityDriver()
        {
            if (Metrics.EnvironmentQuality < 60) return "\u73af\u5883" + Metrics.EnvironmentQuality + "/\u6c61" + Metrics.Pollution;
            if (Metrics.NoiseStress > 45) return "\u566a\u58f0" + Metrics.NoiseStress;
            if (Metrics.LandUseConflict > 30) return "\u7528\u5730\u51b2" + Metrics.LandUseConflict;
            return "\u5b9c\u5c45\u538b" + Metrics.LivingPressure;
        }

        private int RoadHierarchyAdvisor()
        {
            string focus;
            string driver;
            string action;
            var pressure = ComputeRoadHierarchyAdvice(out focus, out driver, out action);
            Metrics.RoadHierarchyFocus = focus;
            Metrics.RoadHierarchyDriver = driver;
            Metrics.RoadHierarchyAction = action;
            return pressure;
        }

        private int ComputeRoadHierarchyAdvice(out string focus, out string driver, out string action)
        {
            var bestPressure = 0;
            focus = "\u5e73\u7a33";
            driver = "\u5c42\u7ea7\u53ef\u63a7";
            action = "\u7ee7\u7eed\u89c2\u5bdf\u8def\u7f51";

            var arterialTarget = Math.Max(4, Metrics.RoadTiles / 6);
            var arterialGap = Math.Max(0, arterialTarget - Metrics.ArterialRoadTiles);
            var arterialPressure = 0;
            if (Metrics.RoadTiles >= 12 && (Metrics.Congestion >= 55 || Metrics.RoadBottleneckPressure >= 45 || (Metrics.Population >= 220 && arterialGap >= 3)))
            {
                arterialPressure = 32 + arterialGap * 7 + Math.Max(0, Metrics.Congestion - 50) / 2 + Math.Max(0, Metrics.RoadBottleneckPressure - 40) / 2;
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, arterialPressure, "\u4e3b\u5e72\u9053\u4e0d\u8db3", "\u4e3b\u5e72" + Metrics.ArterialRoadTiles + "/\u8def" + Metrics.RoadTiles + "/\u5835" + Metrics.Congestion, "\u5347\u7ea7\u8fde\u7eed\u4e3b\u5e72\u9aa8\u67b6");

            var deadEndPressure = 0;
            if (Metrics.RoadTiles >= 12 && Metrics.DeadEndRoadTiles > 2)
            {
                deadEndPressure = 30 + Metrics.DeadEndRoadTiles * 7 + Math.Max(0, 55 - Metrics.RoadConnectivity) / 2;
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, deadEndPressure, "\u65ad\u5934\u8def", "\u65ad" + Metrics.DeadEndRoadTiles + "/\u8fde" + Metrics.RoadConnectivity, "\u6253\u901a\u65ad\u5934\u8def\u5e76\u63a5\u6210\u73af");

            var connectivityPressure = 0;
            if (Metrics.RoadTiles >= 18 && Metrics.RoadConnectivity < 55)
            {
                connectivityPressure = 50 + (55 - Metrics.RoadConnectivity) + Metrics.DeadEndRoadTiles * 2 + Metrics.DisconnectedBuildings * 4;
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, connectivityPressure, "\u8def\u7f51\u8fde\u901a\u4e0d\u8db3", "\u8fde" + Metrics.RoadConnectivity + "/\u672a\u63a5" + Metrics.DisconnectedBuildings, "\u8865\u652f\u8def\u8fde\u901a\u4e3b\u5e72");

            var intersectionPressure = 0;
            if (Metrics.RoadTiles >= 18 && Metrics.IntersectionDelay > 45)
            {
                intersectionPressure = Metrics.IntersectionDelay + Math.Min(12, Metrics.IntersectionRoadTiles * 2) + Math.Max(0, Metrics.Congestion - 60) / 4;
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, intersectionPressure, "\u8def\u53e3\u5ef6\u8bef", "\u5ef6" + Metrics.IntersectionDelay + "/\u8def\u53e3" + Metrics.IntersectionRoadTiles, "\u4f18\u5316\u4fe1\u53f7\u5e76\u5206\u6d41\u4e3b\u5e72");

            var bottleneckPressure = 0;
            if (Metrics.RoadTiles >= 18 && Metrics.RoadBottleneckPressure > 45)
            {
                bottleneckPressure = Metrics.RoadBottleneckPressure + Math.Max(0, Metrics.Congestion - 55) / 3 + Metrics.DeadEndRoadTiles / 2;
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, bottleneckPressure, "\u9053\u8def\u74f6\u9888", "\u74f6" + Metrics.RoadBottleneckPressure + "/\u5835" + Metrics.Congestion, "\u5347\u7ea7\u74f6\u9888\u6bb5\u63a5\u4e3b\u5e72");

            var congestionPressure = Metrics.Congestion > 60 ? Metrics.Congestion + Math.Max(0, Metrics.CarDependency - 60) / 3 : 0;
            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, congestionPressure, "\u62e5\u5835", "\u5835" + Metrics.Congestion + "/\u8f66" + Metrics.CarDependency, "\u4e3b\u5e72\u5206\u6d41\u5e76\u964d\u8f66\u4f9d\u8d56");

            var transitPressure = 0;
            if (Metrics.Population >= 180 && (Metrics.TransitCoverage < 25 || Metrics.TransitUtilization > 110 || Metrics.TransitReliability < 60 || Metrics.TransitWaitPressure > 50))
            {
                transitPressure = Math.Max(transitPressure, Metrics.TransitWaitPressure);
                transitPressure = Math.Max(transitPressure, Math.Max(0, Metrics.TransitUtilization - 45));
                transitPressure = Math.Max(transitPressure, Metrics.TransitReliability < 60 ? 60 + (60 - Metrics.TransitReliability) / 2 : 0);
                transitPressure = Math.Max(transitPressure, Metrics.TransitCoverage < 25 ? 60 + (25 - Metrics.TransitCoverage) : 0);
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, transitPressure, "\u516c\u4ea4\u5019\u8f66/\u8fd0\u529b", RoadHierarchyTransitDriver(), RoadHierarchyTransitAction());

            var parkingPressure = 0;
            if (Metrics.Population >= 160 && (Metrics.ParkingPressure > 55 || Metrics.ParkingUtilization > 110 || (Metrics.ParkingCoverage < 30 && Metrics.ParkingPressure > 45)))
            {
                parkingPressure = Math.Max(Metrics.ParkingPressure, Math.Max(0, Metrics.ParkingUtilization - 45));
                parkingPressure = Math.Max(parkingPressure, Metrics.ParkingCoverage < 30 ? 55 + (30 - Metrics.ParkingCoverage) : 0);
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, parkingPressure, "\u505c\u8f66\u538b\u529b", RoadHierarchyParkingDriver(), RoadHierarchyParkingAction());

            var maintenancePressure = 0;
            if (Metrics.RoadTiles >= 18 && (Metrics.RoadMaintenanceCoverage < 45 || Metrics.AccidentRisk > 50 || Metrics.RoadSafety < 45))
            {
                maintenancePressure = Math.Max(maintenancePressure, Metrics.RoadMaintenanceCoverage < 45 ? 55 + (45 - Metrics.RoadMaintenanceCoverage) : 0);
                maintenancePressure = Math.Max(maintenancePressure, Metrics.AccidentRisk);
                maintenancePressure = Math.Max(maintenancePressure, Metrics.RoadSafety < 45 ? 60 + (45 - Metrics.RoadSafety) : 0);
            }

            AddRoadHierarchyCandidate(ref bestPressure, ref focus, ref driver, ref action, maintenancePressure, "\u4e8b\u6545/\u517b\u62a4", RoadHierarchyMaintenanceDriver(), RoadHierarchyMaintenanceAction());

            return ClampToScore(bestPressure);
        }

        private static void AddRoadHierarchyCandidate(ref int bestPressure, ref string focus, ref string driver, ref string action, int pressure, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedPressure = ClampToScore(pressure);
            if (normalizedPressure <= bestPressure)
            {
                return;
            }

            bestPressure = normalizedPressure;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string RoadHierarchyTransitDriver()
        {
            if (Metrics.TransitCoverage < 25) return "\u516c\u4ea4\u8986" + Metrics.TransitCoverage;
            if (Metrics.TransitUtilization > 110) return "\u516c\u4ea4\u6ee1" + Metrics.TransitUtilization + "/\u5019" + Metrics.TransitWaitPressure;
            if (Metrics.TransitReliability < 60) return "\u516c\u4ea4\u7a33" + Metrics.TransitReliability;
            return "\u5019\u8f66\u538b" + Metrics.TransitWaitPressure + "/\u5835" + Metrics.Congestion;
        }

        private string RoadHierarchyTransitAction()
        {
            if (Metrics.TransitCoverage < 25) return "\u8865\u516c\u4ea4\u9aa8\u67b6\u8986\u76d6";
            if (Metrics.TransitUtilization > 110 || Metrics.TransitWaitPressure > 55) return "\u8865\u516c\u4ea4/\u5730\u94c1\u8fd0\u529b";
            return "\u4f18\u5316\u4fe1\u53f7\u4fdd\u516c\u4ea4\u53ef\u9760";
        }

        private string RoadHierarchyParkingDriver()
        {
            if (Metrics.ParkingUtilization > 110) return "\u505c\u8f66\u6ee1" + Metrics.ParkingUtilization;
            if (Metrics.ParkingCoverage < 30) return "\u505c\u8f66\u8986" + Metrics.ParkingCoverage;
            return "\u505c\u8f66\u538b" + Metrics.ParkingPressure + "/\u8f66" + Metrics.CarDependency;
        }

        private string RoadHierarchyParkingAction()
        {
            if (Metrics.ParkingCoverage < 30 || Metrics.ParkingUtilization > 110) return "\u8865\u505c\u8f66\u5e76\u63a5\u516c\u4ea4";
            return "\u6536\u8d39/\u6df7\u5408\u7528\u5730\u964d\u627e\u8f66\u4f4d";
        }

        private string RoadHierarchyMaintenanceDriver()
        {
            if (Metrics.RoadMaintenanceCoverage < 45) return "\u517b\u62a4" + Metrics.RoadMaintenanceCoverage;
            if (Metrics.AccidentRisk > 50) return "\u4e8b\u6545" + Metrics.AccidentRisk;
            return "\u8def\u5b89" + Metrics.RoadSafety;
        }

        private string RoadHierarchyMaintenanceAction()
        {
            if (Metrics.RoadMaintenanceCoverage < 45) return "\u8865\u9053\u8def\u517b\u62a4\u8986\u76d6";
            if (Metrics.AccidentRisk > 50) return "\u542f\u5b89\u5168\u5e76\u4f18\u5316\u8def\u53e3";
            return "\u7ee7\u7eed\u964d\u4e8b\u6545\u98ce\u9669";
        }

        private int InfrastructureResilienceAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeInfrastructureResilienceAdvice(out focus, out driver, out action);
            Metrics.InfrastructureResilienceFocus = focus;
            Metrics.InfrastructureResilienceDriver = driver;
            Metrics.InfrastructureResilienceAction = action;
            return score;
        }

        private int ComputeInfrastructureResilienceAdvice(out string focus, out string driver, out string action)
        {
            // INFRASTRUCTURE_RESILIENCE_ADVISOR combines road care, utilities, stormwater and emergency readiness.
            var bestScore = 0;
            focus = "\u57fa\u5efa\u7a33\u5b9a";
            driver = "\u97e7\u6027\u53ef\u63a7";
            action = "\u7ee7\u7eed\u89c2\u5bdf\u57fa\u5efa";

            var utilityScore = 0;
            if (Metrics.Population >= 120 && (Metrics.UtilityReliability < 92 || Metrics.UtilityUtilization > 105 || Metrics.WastewaterReliability < 70 || Metrics.WastewaterUtilization > 105))
            {
                utilityScore = Math.Max(utilityScore, Math.Max(0, 96 - Metrics.UtilityReliability));
                utilityScore = Math.Max(utilityScore, Math.Max(0, Metrics.UtilityUtilization - 55));
                utilityScore = Math.Max(utilityScore, Math.Max(0, 82 - Metrics.WastewaterReliability));
                utilityScore = Math.Max(utilityScore, Math.Max(0, Metrics.WastewaterUtilization - 60));
            }

            AddInfrastructureResilienceCandidate(ref bestScore, ref focus, ref driver, ref action, utilityScore, "\u6c34\u7535/\u6c61\u6c34", InfrastructureUtilityDriver(), InfrastructureUtilityAction());

            var stormwaterScore = 0;
            if (Metrics.Population >= 160 && (Metrics.StormwaterUtilization > 95 || Metrics.FloodRisk > 38 || Metrics.StormwaterResilience < 70))
            {
                stormwaterScore = Math.Max(stormwaterScore, Math.Max(0, Metrics.StormwaterUtilization - 45));
                stormwaterScore = Math.Max(stormwaterScore, Metrics.FloodRisk + Math.Max(0, Metrics.FloodRisk - 45) / 2);
                stormwaterScore = Math.Max(stormwaterScore, Metrics.StormwaterResilience < 70 ? 60 + (70 - Metrics.StormwaterResilience) / 2 : 0);
            }

            AddInfrastructureResilienceCandidate(ref bestScore, ref focus, ref driver, ref action, stormwaterScore, "\u96e8\u6d2a/\u5185\u6d9d", InfrastructureStormwaterDriver(), InfrastructureStormwaterAction());

            var roadCareScore = 0;
            if (Metrics.RoadTiles >= 18 && (Metrics.RoadMaintenanceCoverage < 55 || Metrics.AccidentRisk > 45 || Metrics.RoadSafety < 55))
            {
                roadCareScore = Math.Max(roadCareScore, Metrics.RoadMaintenanceCoverage < 55 ? 55 + (55 - Metrics.RoadMaintenanceCoverage) : 0);
                roadCareScore = Math.Max(roadCareScore, Metrics.AccidentRisk + Math.Max(0, Metrics.RoadBottleneckPressure - 45) / 3);
                roadCareScore = Math.Max(roadCareScore, Metrics.RoadSafety < 55 ? 55 + (55 - Metrics.RoadSafety) / 2 : 0);
            }

            AddInfrastructureResilienceCandidate(ref bestScore, ref focus, ref driver, ref action, roadCareScore, "\u9053\u8def\u517b\u62a4", InfrastructureRoadCareDriver(), InfrastructureRoadCareAction());

            var emergencyScore = 0;
            if (Metrics.Population >= 180 && (Metrics.EmergencyResponse < 55 || Metrics.DisasterPreparedness < 55 || Metrics.DisasterRisk > 45))
            {
                emergencyScore = Math.Max(emergencyScore, Metrics.EmergencyResponse < 55 ? 62 + (55 - Metrics.EmergencyResponse) : 0);
                emergencyScore = Math.Max(emergencyScore, Metrics.DisasterPreparedness < 55 ? 58 + (55 - Metrics.DisasterPreparedness) : 0);
                emergencyScore = Math.Max(emergencyScore, Metrics.DisasterRisk + Math.Max(0, Metrics.DisasterRisk - 50) / 2);
            }

            AddInfrastructureResilienceCandidate(ref bestScore, ref focus, ref driver, ref action, emergencyScore, "\u5e94\u6025/\u707e\u5907", InfrastructureEmergencyDriver(), InfrastructureEmergencyAction());

            var maintenanceScore = 0;
            if (Metrics.Population >= 120 && Metrics.MaintenanceCondition < 60)
            {
                maintenanceScore = 52 + (60 - Metrics.MaintenanceCondition);
                if (Metrics.ServiceBudgetLevel == CityServiceBudgetLevel.Lean)
                {
                    maintenanceScore += 10;
                }
            }

            AddInfrastructureResilienceCandidate(ref bestScore, ref focus, ref driver, ref action, maintenanceScore, "\u57ce\u5e02\u8fd0\u7ef4", InfrastructureMaintenanceDriver(), InfrastructureMaintenanceAction());

            return ClampToScore(bestScore);
        }

        private static void AddInfrastructureResilienceCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string InfrastructureUtilityDriver()
        {
            if (Metrics.UtilityReliability < 92 || Metrics.UtilityUtilization > 105)
            {
                return "\u6c34\u7535\u7a33" + Metrics.UtilityReliability + "/\u8f7d" + Metrics.UtilityUtilization;
            }

            return "\u6c61\u6c34\u7a33" + Metrics.WastewaterReliability + "/\u8f7d" + Metrics.WastewaterUtilization;
        }

        private string InfrastructureUtilityAction()
        {
            if (Metrics.UtilityUtilization > 105 || Metrics.UtilityReliability < 88)
            {
                return "\u8865\u7535\u6c34\u5bb9\u91cf\u4fdd\u53ef\u9760";
            }

            return "\u8865\u6c61\u6c34\u5904\u7406\u8282\u70b9";
        }

        private string InfrastructureStormwaterDriver()
        {
            if (Metrics.FloodRisk > 45)
            {
                return "\u6d9d" + Metrics.FloodRisk + "/\u97e7" + Metrics.StormwaterResilience;
            }

            return "\u96e8\u6d2a\u8f7d" + Metrics.StormwaterUtilization + "/\u97e7" + Metrics.StormwaterResilience;
        }

        private string InfrastructureStormwaterAction()
        {
            if (Metrics.FloodRisk > 45 || Metrics.StormwaterUtilization > 105)
            {
                return "\u8865\u96e8\u82b1\u56ed\u6216\u96e8\u6d2a\u5bb9\u91cf";
            }

            return "\u7528\u7eff\u5730\u548c\u96e8\u6d2a\u5de5\u7a0b\u63d0\u97e7\u6027";
        }

        private string InfrastructureRoadCareDriver()
        {
            if (Metrics.RoadMaintenanceCoverage < 55)
            {
                return "\u517b\u62a4" + Metrics.RoadMaintenanceCoverage + "/\u8def" + Metrics.RoadTiles;
            }

            if (Metrics.AccidentRisk > 45)
            {
                return "\u4e8b\u6545" + Metrics.AccidentRisk + "/\u5b89" + Metrics.RoadSafety;
            }

            return "\u8def\u5b89" + Metrics.RoadSafety + "/\u74f6" + Metrics.RoadBottleneckPressure;
        }

        private string InfrastructureRoadCareAction()
        {
            if (Metrics.RoadMaintenanceCoverage < 55)
            {
                return "\u8865\u9053\u8def\u517b\u62a4\u7ad9";
            }

            if (Metrics.AccidentRisk > 45)
            {
                return "\u542f\u4ea4\u901a\u5b89\u5168\u5e76\u6539\u8def\u53e3";
            }

            return "\u5347\u74f6\u9888\u8def\u6bb5\u4fdd\u5b89\u5168";
        }

        private string InfrastructureEmergencyDriver()
        {
            if (Metrics.EmergencyResponse < 55)
            {
                return "\u54cd\u5e94" + Metrics.EmergencyResponse + "/\u707e\u9669" + Metrics.DisasterRisk;
            }

            return "\u707e\u5907" + Metrics.DisasterPreparedness + "/\u707e\u9669" + Metrics.DisasterRisk;
        }

        private string InfrastructureEmergencyAction()
        {
            if (Metrics.EmergencyResponse < 55)
            {
                return "\u8865\u533b\u6d88\u8b66\u63d0\u5e94\u6025";
            }

            return "\u5efa\u907f\u96be\u4e2d\u5fc3\u5e76\u63a5\u8def";
        }

        private string InfrastructureMaintenanceDriver()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean)
            {
                return "\u9884\u7b97\u7cbe\u7b80/\u8fd0\u7ef4" + Metrics.MaintenanceCondition;
            }

            return "\u8fd0\u7ef4" + Metrics.MaintenanceCondition + "/\u670d\u6ee1" + Metrics.ServiceUtilization;
        }

        private string InfrastructureMaintenanceAction()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean)
            {
                return "\u6062\u590d\u6807\u51c6\u670d\u52a1\u9884\u7b97";
            }

            return "\u8865\u517b\u62a4\u548c\u57fa\u7840\u670d\u52a1";
        }

        private int CommuteCorridorAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeCommuteCorridorAdvice(out focus, out driver, out action);
            Metrics.CommuteCorridorFocus = focus;
            Metrics.CommuteCorridorDriver = driver;
            Metrics.CommuteCorridorAction = action;
            return score;
        }

        private int ComputeCommuteCorridorAdvice(out string focus, out string driver, out string action)
        {
            // COMMUTE_CORRIDOR_ADVISOR turns mobility metrics into one corridor-level action.
            var bestScore = 0;
            focus = "\u901a\u52e4\u5e73\u7a33";
            driver = "\u8d70\u5eca\u53ef\u63a7";
            action = "\u7ee7\u7eed\u89c2\u5bdf\u901a\u52e4";

            var mobilityPressure = Math.Max(0, 62 - Metrics.CommuteEfficiency) + Math.Max(0, Metrics.CarDependency - 58) / 2 + Math.Max(0, 60 - Metrics.JobsHousingBalance) / 3;
            if (Metrics.Population >= 160 && (Metrics.CommuteEfficiency < 50 || Metrics.CarDependency > 65 || Metrics.JobsHousingBalance < 45))
            {
                mobilityPressure += 28;
            }

            AddCommuteCorridorCandidate(ref bestScore, ref focus, ref driver, ref action, mobilityPressure, "\u901a\u52e4\u8d70\u5eca", CommuteCorridorMobilityDriver(), CommuteCorridorMobilityAction());

            var transitPressure = 0;
            if (Metrics.Population >= 180 && (Metrics.TransitCoverage < 35 || Metrics.TransitWaitPressure > 45 || Metrics.TransitUtilization > 105 || Metrics.TransitReliability < 65))
            {
                transitPressure = Math.Max(transitPressure, Metrics.TransitWaitPressure + Math.Max(0, Metrics.TransitUtilization - 95) / 2);
                transitPressure = Math.Max(transitPressure, Metrics.TransitCoverage < 35 ? 62 + (35 - Metrics.TransitCoverage) : 0);
                transitPressure = Math.Max(transitPressure, Metrics.TransitReliability < 65 ? 58 + (65 - Metrics.TransitReliability) / 2 : 0);
            }

            AddCommuteCorridorCandidate(ref bestScore, ref focus, ref driver, ref action, transitPressure, "\u516c\u4ea4\u8f74\u7ebf", CommuteCorridorTransitDriver(), CommuteCorridorTransitAction());

            var parkingPressure = 0;
            if (Metrics.Population >= 160 && (Metrics.ParkingPressure > 55 || Metrics.ParkingUtilization > 108 || Metrics.ParkingCoverage < 32))
            {
                parkingPressure = Math.Max(Metrics.ParkingPressure, Math.Max(0, Metrics.ParkingUtilization - 45));
                parkingPressure = Math.Max(parkingPressure, Metrics.ParkingCoverage < 32 ? 55 + (32 - Metrics.ParkingCoverage) : 0);
                parkingPressure += Math.Max(0, Metrics.CarDependency - 62) / 2;
            }

            AddCommuteCorridorCandidate(ref bestScore, ref focus, ref driver, ref action, parkingPressure, "\u505c\u8f66\u641c\u7d22", CommuteCorridorParkingDriver(), CommuteCorridorParkingAction());

            var networkPressure = 0;
            if (Metrics.RoadTiles >= 16 && (Metrics.RoadBottleneckPressure > 45 || Metrics.IntersectionDelay > 45 || Metrics.RoadConnectivity < 55 || Metrics.DisconnectedBuildings > 0))
            {
                networkPressure = Math.Max(Metrics.RoadBottleneckPressure, Metrics.IntersectionDelay);
                networkPressure += Math.Max(0, 55 - Metrics.RoadConnectivity) / 2 + Metrics.DisconnectedBuildings * 3 + Metrics.DeadEndRoadTiles;
            }

            AddCommuteCorridorCandidate(ref bestScore, ref focus, ref driver, ref action, networkPressure, "\u6362\u4e58\u8fde\u901a", CommuteCorridorNetworkDriver(), CommuteCorridorNetworkAction());

            var freightPressure = 0;
            if (Metrics.Jobs >= 120 && (Metrics.LogisticsCoverage < 35 || Metrics.LogisticsUtilization > 110 || Metrics.SupplyChainStability < 55 || Metrics.GoodsBalance < 70))
            {
                freightPressure = Math.Max(0, 65 - Metrics.LogisticsCoverage);
                freightPressure = Math.Max(freightPressure, Math.Max(0, Metrics.LogisticsUtilization - 45));
                freightPressure = Math.Max(freightPressure, Math.Max(0, 70 - Metrics.GoodsBalance) + Math.Max(0, 55 - Metrics.SupplyChainStability) / 2);
            }

            AddCommuteCorridorCandidate(ref bestScore, ref focus, ref driver, ref action, freightPressure, "\u8d27\u8fd0\u8d70\u5eca", CommuteCorridorFreightDriver(), CommuteCorridorFreightAction());

            var regionalPressure = 0;
            if (Metrics.Population >= 260 && (Metrics.RegionalConnectivity < 35 || Metrics.Attractiveness < 40 || Metrics.Visitors > 0))
            {
                regionalPressure = Math.Max(0, 45 - Metrics.RegionalConnectivity) + Metrics.Visitors / 16 + Math.Max(0, Metrics.ParkingPressure - 55) / 3;
            }

            AddCommuteCorridorCandidate(ref bestScore, ref focus, ref driver, ref action, regionalPressure, "\u5bf9\u5916\u8fde\u63a5", "\u5916" + Metrics.RegionalConnectivity + "/\u5ba2" + Metrics.Visitors, "\u8865\u57ce\u9645\u7ad9\u70b9\u5e76\u63a5\u516c\u4ea4");

            return ClampToScore(bestScore);
        }

        private static void AddCommuteCorridorCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string CommuteCorridorMobilityDriver()
        {
            if (Metrics.CommuteEfficiency < 50) return "\u901a" + Metrics.CommuteEfficiency + "/\u8f66" + Metrics.CarDependency;
            if (Metrics.JobsHousingBalance < 55) return "\u4f4f\u5c97" + Metrics.JobsHousingBalance + "/\u5c97" + Metrics.Jobs;
            return "\u6b65" + Metrics.Walkability + "/\u6df7" + Metrics.MixedUseBuildings;
        }

        private string CommuteCorridorMobilityAction()
        {
            if (Metrics.JobsHousingBalance < 55) return "\u5728\u5c31\u4e1a\u533a\u9644\u8fd1\u8865\u4f4f\u623f";
            if (Metrics.Walkability < 45 || Metrics.MixedUseBuildings < 2) return "\u6df7\u5408\u7528\u5730\u5e76\u63a5\u652f\u8def";
            return "\u964d\u8f66\u4f9d\u8d56\u5e76\u63a5\u516c\u4ea4";
        }

        private string CommuteCorridorTransitDriver()
        {
            if (Metrics.TransitCoverage < 35) return "\u8986" + Metrics.TransitCoverage + "/\u901a" + Metrics.CommuteEfficiency;
            if (Metrics.TransitUtilization > 105) return "\u6ee1" + Metrics.TransitUtilization + "/\u5019" + Metrics.TransitWaitPressure;
            if (Metrics.TransitReliability < 65) return "\u7a33" + Metrics.TransitReliability + "/\u5835" + Metrics.Congestion;
            return "\u5019" + Metrics.TransitWaitPressure + "/\u8f66" + Metrics.CarDependency;
        }

        private string CommuteCorridorTransitAction()
        {
            if (Metrics.TransitCoverage < 35) return "\u94fa\u516c\u4ea4\u9aa8\u67b6\u8fde\u5c45\u4f4f\u5c31\u4e1a";
            if (Metrics.TransitUtilization > 105 || Metrics.TransitWaitPressure > 45) return "\u52a0\u516c\u4ea4/\u8f68\u9053\u8fd0\u529b";
            return "\u4f18\u5316\u4fe1\u53f7\u4fdd\u516c\u4ea4\u51c6\u70b9";
        }

        private string CommuteCorridorParkingDriver()
        {
            if (Metrics.ParkingUtilization > 108) return "\u6ee1" + Metrics.ParkingUtilization + "/\u538b" + Metrics.ParkingPressure;
            if (Metrics.ParkingCoverage < 32) return "\u8986" + Metrics.ParkingCoverage + "/\u8f66" + Metrics.CarDependency;
            return "\u538b" + Metrics.ParkingPressure + "/\u5835" + Metrics.Congestion;
        }

        private string CommuteCorridorParkingAction()
        {
            if (Metrics.TransitCoverage < 35) return "\u505c\u8f66\u6362\u4e58\u63a5\u516c\u4ea4";
            if (Metrics.ParkingCoverage < 32 || Metrics.ParkingUtilization > 108) return "\u8865\u505c\u8f66\u5e76\u542f\u6536\u8d39";
            return "\u6df7\u5408\u7528\u5730\u964d\u627e\u4f4d\u8def\u7a0b";
        }

        private string CommuteCorridorNetworkDriver()
        {
            if (Metrics.RoadConnectivity < 55) return "\u8fde" + Metrics.RoadConnectivity + "/\u65ad" + Metrics.DeadEndRoadTiles;
            if (Metrics.IntersectionDelay > 45) return "\u5ef6" + Metrics.IntersectionDelay + "/\u53e3" + Metrics.IntersectionRoadTiles;
            return "\u74f6" + Metrics.RoadBottleneckPressure + "/\u672a" + Metrics.DisconnectedBuildings;
        }

        private string CommuteCorridorNetworkAction()
        {
            if (Metrics.RoadConnectivity < 55 || Metrics.DisconnectedBuildings > 0) return "\u6253\u901a\u652f\u8def\u63a5\u4e3b\u5e72";
            if (Metrics.IntersectionDelay > 45) return "\u4f18\u5316\u4fe1\u53f7\u5e76\u5206\u6d41";
            return "\u5347\u74f6\u9888\u6bb5\u5e76\u63a5\u516c\u4ea4";
        }

        private string CommuteCorridorFreightDriver()
        {
            if (Metrics.LogisticsCoverage < 35) return "\u8d27\u8986" + Metrics.LogisticsCoverage + "/\u5c97" + Metrics.Jobs;
            if (Metrics.LogisticsUtilization > 110) return "\u8d27\u6ee1" + Metrics.LogisticsUtilization + "/\u94fe" + Metrics.SupplyChainStability;
            return "\u54c1" + Metrics.GoodsBalance + "/\u94fe" + Metrics.SupplyChainStability;
        }

        private string CommuteCorridorFreightAction()
        {
            if (Metrics.LogisticsCoverage < 35) return "\u8865\u8d27\u8fd0\u8986\u76d6\u5230\u4ea7\u4e1a\u533a";
            if (Metrics.LogisticsUtilization > 110) return "\u52a0\u8d27\u8fd0/\u94c1\u8def\u8fd0\u529b";
            return "\u8865\u4ed3\u50a8\u5e76\u7a33\u4f9b\u5e94\u94fe";
        }

        private int EconomicSpecializationAdvisor()
        {
            string focus;
            string driver;
            string action;
            var score = ComputeEconomicSpecializationAdvice(out focus, out driver, out action);
            Metrics.EconomicSpecializationFocus = focus;
            Metrics.EconomicSpecializationDriver = driver;
            Metrics.EconomicSpecializationAction = action;
            return score;
        }

        private int ComputeEconomicSpecializationAdvice(out string focus, out string driver, out string action)
        {
            // ECONOMIC_SPECIALIZATION_ADVISOR turns existing economy metrics into one specialization bet.
            var bestScore = 0;
            focus = "\u7ecf\u6d4e\u5e73\u7a33";
            driver = "\u4ea7\u4e1a\u53ef\u63a7";
            action = "\u7ee7\u7eed\u89c2\u5bdf\u4ea7\u4e1a";

            var industryScore = 0;
            if (Metrics.Jobs >= 80 || Metrics.Demand.Industrial > 60 || Metrics.LocalGoodsSupply > 0)
            {
                industryScore = Math.Max(Metrics.Demand.Industrial, Metrics.IndustrialSpecialization);
                industryScore = Math.Max(industryScore, Math.Max(0, 68 - Metrics.ResourceSpecialization) + Metrics.LocalGoodsSupply / 10);
                if (Metrics.ResourcePotential > 55 && Metrics.ResourceSpecialization < 55)
                {
                    industryScore += 12;
                }
            }

            AddEconomicSpecializationCandidate(ref bestScore, ref focus, ref driver, ref action, industryScore, "\u8d44\u6e90\u5de5\u4e1a", EconomicIndustryDriver(), EconomicIndustryAction());

            var logisticsScore = 0;
            if (Metrics.GoodsDemand > 0 || Metrics.Jobs >= 120 || Metrics.LogisticsLoad > 0)
            {
                logisticsScore = Math.Max(0, 85 - Metrics.GoodsBalance);
                logisticsScore = Math.Max(logisticsScore, Math.Max(0, 68 - Metrics.SupplyChainStability));
                logisticsScore = Math.Max(logisticsScore, Math.Max(0, Metrics.LogisticsUtilization - 88));
                logisticsScore = Math.Max(logisticsScore, Math.Max(0, 45 - Metrics.LogisticsCoverage) + Metrics.GoodsDemand / 20);
            }

            AddEconomicSpecializationCandidate(ref bestScore, ref focus, ref driver, ref action, logisticsScore, "\u7269\u6d41\u4f9b\u5e94\u94fe", EconomicLogisticsDriver(), EconomicLogisticsAction());

            var knowledgeScore = 0;
            if (Metrics.Population >= 240 || Metrics.OfficeJobs > 30 || Metrics.Demand.Office > 60)
            {
                knowledgeScore = Math.Max(Metrics.Demand.Office, Math.Max(0, 72 - Metrics.InnovationCapacity));
                knowledgeScore = Math.Max(knowledgeScore, Math.Max(0, 65 - Metrics.WorkforceSkill));
                knowledgeScore = Math.Max(knowledgeScore, Math.Max(0, 45 - Metrics.AdvancedEducationCoverage) + Metrics.OfficeJobs / 8);
                if (Metrics.BusinessEfficiency < 50 && Metrics.OfficeJobs >= 60)
                {
                    knowledgeScore += 10;
                }
            }

            AddEconomicSpecializationCandidate(ref bestScore, ref focus, ref driver, ref action, knowledgeScore, "\u529e\u516c\u521b\u65b0", EconomicKnowledgeDriver(), EconomicKnowledgeAction());

            var tourismScore = 0;
            if (Metrics.Population >= 240 || Metrics.LandmarkBuildings > 0 || Metrics.Visitors > 0)
            {
                tourismScore = Math.Max(0, 68 - Metrics.Attractiveness);
                tourismScore = Math.Max(tourismScore, Math.Max(0, 55 - Metrics.RegionalConnectivity));
                tourismScore = Math.Max(tourismScore, Metrics.Visitors / 18 + Math.Max(0, 45 - Metrics.ParkingCoverage) / 2);
                if (Metrics.TourismIncome > 0 && Metrics.Attractiveness < 55)
                {
                    tourismScore += 10;
                }
            }

            AddEconomicSpecializationCandidate(ref bestScore, ref focus, ref driver, ref action, tourismScore, "\u65c5\u6e38\u4f1a\u5c55", EconomicTourismDriver(), EconomicTourismAction());

            var mixedCommerceScore = 0;
            if (Metrics.Population >= 160 || Metrics.Demand.Commercial > 65 || Metrics.Demand.MixedUse > 65)
            {
                mixedCommerceScore = Math.Max(Metrics.Demand.Commercial, Metrics.Demand.MixedUse);
                mixedCommerceScore = Math.Max(mixedCommerceScore, Math.Max(0, 62 - Metrics.BusinessEfficiency));
                mixedCommerceScore = Math.Max(mixedCommerceScore, Math.Max(0, 50 - Metrics.Walkability) + Math.Max(0, 2 - Metrics.MixedUseBuildings) * 8);
            }

            AddEconomicSpecializationCandidate(ref bestScore, ref focus, ref driver, ref action, mixedCommerceScore, "\u6df7\u5408\u5546\u4e1a", EconomicMixedCommerceDriver(), EconomicMixedCommerceAction());

            return ClampToScore(bestScore);
        }

        private static void AddEconomicSpecializationCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private string EconomicIndustryDriver()
        {
            if (Metrics.ResourcePotential > 55 && Metrics.ResourceSpecialization < 55) return "\u8d44\u6f5c" + Metrics.ResourcePotential + "/\u9002" + Metrics.ResourceSpecialization;
            if (Metrics.IndustrialSpecialization < 55) return "\u4ea7\u4e13" + Metrics.IndustrialSpecialization + "/\u672c" + Metrics.LocalGoodsSupply;
            return "\u5de5\u9700" + Metrics.Demand.Industrial + "/\u5de5" + Metrics.Jobs;
        }

        private string EconomicIndustryAction()
        {
            if (Metrics.ResourcePotential > 55 && Metrics.ResourceSpecialization < 55) return "\u63a5\u8d44\u6e90\u52a0\u5de5\u56ed";
            if (Metrics.LogisticsCoverage < 35) return "\u8865\u8d27\u8fd0\u8986\u76d6\u4ea7\u4e1a";
            if (Metrics.WorkforceSkill < 45) return "\u8865\u5b66\u9662\u63d0\u5de5\u4e1a\u6280\u80fd";
            return "\u6269\u5de5\u4e1a\u5e76\u7a33\u672c\u5730\u4f9b\u7ed9";
        }

        private string EconomicLogisticsDriver()
        {
            if (Metrics.GoodsBalance < 75) return "\u54c1" + Metrics.GoodsBalance + "/\u9700" + Metrics.GoodsDemand;
            if (Metrics.SupplyChainStability < 60) return "\u94fe" + Metrics.SupplyChainStability + "/\u4ed3" + Metrics.GoodsStorage;
            if (Metrics.LogisticsUtilization > 105) return "\u8d27\u6ee1" + Metrics.LogisticsUtilization;
            return "\u8d27\u8986" + Metrics.LogisticsCoverage + "/\u94c1" + Metrics.FreightImportSupply;
        }

        private string EconomicLogisticsAction()
        {
            if (Metrics.LogisticsCoverage < 35) return "\u8865\u8d27\u8fd0\u5230\u5546\u5de5\u533a";
            if (Metrics.GoodsStorage < Metrics.GoodsDemand / 3) return "\u8865\u4ed3\u50a8\u7f13\u51b2\u4f9b\u5e94";
            if (Metrics.FreightImportSupply <= 0 && Metrics.Jobs >= 220) return "\u63a5\u94c1\u8def\u8d27\u8fd0\u5bfc\u5165";
            return "\u6269\u8d27\u8fd0\u5e76\u7a33\u4f9b\u5e94\u94fe";
        }

        private string EconomicKnowledgeDriver()
        {
            if (Metrics.InnovationCapacity < 55) return "\u521b" + Metrics.InnovationCapacity + "/\u529e" + Metrics.OfficeJobs;
            if (Metrics.WorkforceSkill < 55) return "\u4eba\u624d" + Metrics.WorkforceSkill + "/\u9ad8" + Metrics.AdvancedEducationCoverage;
            if (Metrics.BusinessEfficiency < 55) return "\u4f01\u6548" + Metrics.BusinessEfficiency + "/\u901a" + Metrics.CommunicationCoverage;
            return "\u529e\u9700" + Metrics.Demand.Office + "/\u521b" + Metrics.InnovationCapacity;
        }

        private string EconomicKnowledgeAction()
        {
            if (Metrics.AdvancedEducationCoverage < 35) return "\u8865\u793e\u533a\u5b66\u9662";
            if (Metrics.CommunicationCoverage < 45) return "\u8865\u901a\u4fe1\u652f\u6491\u529e\u516c";
            if (Metrics.InnovationCapacity < 55) return "\u5efa\u7814\u53d1\u56ed\u63d0\u521b\u65b0";
            return "\u6269\u529e\u516c\u5e76\u7559\u4eba\u624d";
        }

        private string EconomicTourismDriver()
        {
            if (Metrics.Attractiveness < 50) return "\u5438" + Metrics.Attractiveness + "/\u5ba2" + Metrics.Visitors;
            if (Metrics.RegionalConnectivity < 45) return "\u5916" + Metrics.RegionalConnectivity + "/\u65c5" + Metrics.TourismIncome;
            if (Metrics.ParkingPressure > 60) return "\u505c" + Metrics.ParkingPressure + "/\u5ba2" + Metrics.Visitors;
            return "\u5730\u6807" + Metrics.LandmarkBuildings + "/\u5ba2" + Metrics.Visitors;
        }

        private string EconomicTourismAction()
        {
            if (Metrics.LandmarkBuildings <= 0) return "\u5efa\u5730\u6807/\u4f1a\u5c55\u63d0\u5438\u5f15";
            if (Metrics.RegionalConnectivity < 45) return "\u63a5\u57ce\u9645\u5e76\u8865\u516c\u4ea4";
            if (Metrics.ParkingPressure > 60) return "\u505c\u8f66\u6362\u4e58\u63a5\u5ba2\u6d41";
            return "\u4e32\u8054\u5730\u6807\u5546\u4e1a\u52a8\u7ebf";
        }

        private string EconomicMixedCommerceDriver()
        {
            if (Metrics.MixedUseBuildings < 2) return "\u6df7" + Metrics.MixedUseBuildings + "/\u5546" + Metrics.Demand.Commercial;
            if (Metrics.BusinessEfficiency < 55) return "\u4f01\u6548" + Metrics.BusinessEfficiency + "/\u90ae" + Metrics.MailCoverage;
            if (Metrics.Walkability < 50) return "\u6b65" + Metrics.Walkability + "/\u8f66" + Metrics.CarDependency;
            return "\u5546" + Metrics.Demand.Commercial + "/\u6df7" + Metrics.Demand.MixedUse;
        }

        private string EconomicMixedCommerceAction()
        {
            if (Metrics.MixedUseBuildings < 2) return "\u5728\u5c45\u4f4f\u5c31\u4e1a\u95f4\u8865\u6df7\u5408";
            if (Metrics.MailCoverage < 35 || Metrics.CommunicationCoverage < 45) return "\u8865\u90ae\u653f/\u901a\u4fe1\u63d0\u4f01\u6548";
            if (Metrics.Walkability < 50) return "\u5b8c\u6574\u8857\u9053\u63d0\u6b65\u884c";
            return "\u6269\u5546\u4e1a\u5e76\u7a33\u5ba2\u6d41";
        }

        private static string ForecastPartOrFallback(string text, string fallback)
        {
            return string.IsNullOrEmpty(text) ? fallback : text;
        }

        private int AnalyzeDemandDrivers()
        {
            var demand = Metrics.Demand;
            var bestValue = demand.Residential;
            var bestKind = "\u4f4f\u5b85";
            AddDemandCandidate(ref bestValue, ref bestKind, demand.Commercial, "\u5546\u4e1a");
            AddDemandCandidate(ref bestValue, ref bestKind, demand.MixedUse, "\u6df7\u5408");
            AddDemandCandidate(ref bestValue, ref bestKind, demand.Office, "\u529e\u516c");
            AddDemandCandidate(ref bestValue, ref bestKind, demand.Industrial, "\u5de5\u4e1a");
            AddDemandCandidate(ref bestValue, ref bestKind, demand.Service, "\u670d\u52a1");
            AddDemandCandidate(ref bestValue, ref bestKind, demand.Utility, "\u8bbe\u65bd");

            Metrics.DemandFocus = bestKind;
            Metrics.DemandDriver = DemandDriverFor(bestKind);
            Metrics.DemandAction = DemandActionFor(bestKind);
            return ClampToScore(bestValue + DemandPressureBonus(bestKind));
        }

        private static void AddDemandCandidate(ref int bestValue, ref string bestKind, int value, string kind)
        {
            if (value <= bestValue)
            {
                return;
            }

            bestValue = value;
            bestKind = kind;
        }

        private string DemandDriverFor(string kind)
        {
            if (kind == "\u4f4f\u5b85")
            {
                if (Metrics.HousingCapacity <= Metrics.Population + 24) return "\u4f4f\u623f\u5bb9\u91cf\u7d27";
                if (Metrics.RentPressure > 65) return "\u5c45\u4f4f\u6210\u672c\u9ad8";
                if (Metrics.LivingCondition < 55 || Metrics.LivingPressure > 55) return "\u5b9c\u5c45\u538b\u529b";
                if (Metrics.ServiceGapPressure > 35) return "\u670d\u52a1\u7f3a\u53e3";
                return "\u4eba\u53e3\u548c\u5e78\u798f\u63a8\u52a8";
            }

            if (kind == "\u5546\u4e1a")
            {
                if (Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 75) return "\u5546\u54c1\u4f9b\u7ed9\u4e0d\u8db3";
                if (Metrics.ParkingPressure > 60) return "\u505c\u8f66\u627f\u538b";
                if (Metrics.BusinessEfficiency < 48) return "\u4f01\u4e1a\u6548\u7387\u4f4e";
                return "\u4eba\u6d41\u548c\u6d88\u8d39\u589e\u957f";
            }

            if (kind == "\u6df7\u5408")
            {
                if (Metrics.Walkability < 52) return "\u6b65\u884c\u8fde\u63a5\u4e0d\u8db3";
                if (Metrics.CommuteEfficiency < 48) return "\u901a\u52e4\u6548\u7387\u4f4e";
                if (Metrics.RentPressure > 62) return "\u4f4f\u5546\u9700\u8981\u5c31\u8fd1";
                return "\u4f4f\u5b85\u4e0e\u5546\u4e1a\u8054\u52a8";
            }

            if (kind == "\u529e\u516c")
            {
                if (Metrics.WorkforceSkill < 45) return "\u4eba\u624d\u50a8\u5907\u4e0d\u8db3";
                if (Metrics.InnovationCapacity < 45) return "\u521b\u65b0\u80fd\u529b\u4e0d\u8db3";
                if (Metrics.CommunicationCoverage < 45) return "\u901a\u4fe1\u8986\u76d6\u4e0d\u8db3";
                return "\u77e5\u8bc6\u5c97\u4f4d\u6269\u5f20";
            }

            if (kind == "\u5de5\u4e1a")
            {
                if (Metrics.LogisticsCoverage < 35 || Metrics.LogisticsUtilization > 110) return "\u7269\u6d41\u77ed\u677f";
                if (Metrics.ResourceSpecialization < 45 && Metrics.LocalGoodsSupply > 0) return "\u8d44\u6e90\u9002\u914d\u4e0d\u8db3";
                if (Metrics.LaborShortage > 45) return "\u7528\u5de5\u7f3a\u53e3";
                return "\u672c\u5730\u4f9b\u7ed9\u6269\u5f20";
            }

            if (kind == "\u670d\u52a1")
            {
                if (Metrics.ServiceGapPressure > 35 && Metrics.ServiceGapFocus != "\u5747\u8861") return "\u4e3b\u7f3a\u53e3:" + Metrics.ServiceGapFocus;
                if (Metrics.PatientBacklog > 35) return "\u75c5\u60a3\u79ef\u538b";
                if (Metrics.StudentBacklog > 35) return "\u5165\u5b66\u79ef\u538b";
                if (Metrics.FireRisk > 45) return "\u706b\u707e\u98ce\u9669";
                if (Metrics.CaseBacklog > 35) return "\u6848\u4ef6\u79ef\u538b";
                return "\u516c\u5171\u670d\u52a1\u5bb9\u91cf";
            }

            if (Metrics.PowerSupply < Metrics.PowerDemand) return "\u7535\u529b\u4e0d\u8db3";
            if (Metrics.WaterSupply < Metrics.WaterDemand) return "\u4f9b\u6c34\u4e0d\u8db3";
            if (Metrics.WastewaterUtilization > 105) return "\u6c61\u6c34\u8fc7\u8f7d";
            if (Metrics.StormwaterUtilization > 105 || Metrics.FloodRisk > 45) return "\u96e8\u6d2a\u627f\u538b";
            if (Metrics.CommunicationCoverage < 45) return "\u901a\u4fe1\u8986\u76d6";
            return "\u57fa\u7840\u8bbe\u65bd\u5bb9\u91cf";
        }

        private string DemandActionFor(string kind)
        {
            if (kind == "\u4f4f\u5b85") return Metrics.RentPressure > 65 ? "\u8865\u4f4f\u5b85\u6216\u516c\u5bd3" : "\u5212\u4f4f\u5b85\u533a";
            if (kind == "\u5546\u4e1a") return Metrics.GoodsBalance < 75 ? "\u8865\u4f9b\u5e94\u94fe\u518d\u5efa\u5546\u4e1a" : "\u8865\u5546\u94fa/\u6df7\u5408";
            if (kind == "\u6df7\u5408") return "\u5212\u6df7\u5408\u533a\u8fde\u516c\u4ea4";
            if (kind == "\u529e\u516c") return Metrics.WorkforceSkill < 45 ? "\u8865\u5b66\u9662\u548c\u7814\u53d1" : "\u5efa\u529e\u516c\u548c\u901a\u4fe1";
            if (kind == "\u5de5\u4e1a") return "\u8865\u7269\u6d41/\u8d44\u6e90\u94fe";
            if (kind == "\u670d\u52a1") return ForecastServiceAction();
            return "\u8865\u7535\u6c34\u6c61\u6c34\u96e8\u6d2a";
        }

        private int DemandPressureBonus(string kind)
        {
            if (kind == "\u4f4f\u5b85") return Math.Max(0, Math.Max(Metrics.RentPressure - 60, Metrics.LivingPressure - 50)) / 3;
            if (kind == "\u5546\u4e1a") return Math.Max(0, 75 - Metrics.GoodsBalance) / 3 + Math.Max(0, Metrics.ParkingPressure - 55) / 4;
            if (kind == "\u6df7\u5408") return Math.Max(0, 55 - Metrics.Walkability) / 3 + Math.Max(0, 55 - Metrics.CommuteEfficiency) / 4;
            if (kind == "\u529e\u516c") return Math.Max(0, 55 - Metrics.WorkforceSkill) / 3 + Math.Max(0, 55 - Metrics.InnovationCapacity) / 4;
            if (kind == "\u5de5\u4e1a") return Math.Max(0, 55 - Metrics.LogisticsCoverage) / 3 + Metrics.LaborShortage / 8;
            if (kind == "\u670d\u52a1") return Metrics.ServiceGapPressure / 5 + Math.Max(0, Metrics.ServiceUtilization - 100) / 4;
            return Math.Max(0, Metrics.UtilityUtilization - 95) / 3 + Math.Max(0, Metrics.WastewaterUtilization - 95) / 3 + Math.Max(0, Metrics.FloodRisk - 45) / 4;
        }

        private void AddCityEvent(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            PushCityEvent("\u7b2c " + Metrics.Day + " \u5929 " + message);
        }

        private void PushCityEvent(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (recentEvents.Count > 0 && recentEvents[0] == text)
            {
                return;
            }

            recentEvents.Insert(0, text);
            while (recentEvents.Count > CityEventDigestLimit)
            {
                recentEvents.RemoveAt(recentEvents.Count - 1);
            }
        }

        private void PublishRecentEvents()
        {
            Metrics.RecentEvents.Clear();
            for (var i = 0; i < recentEvents.Count; i += 1)
            {
                Metrics.RecentEvents.Add(recentEvents[i]);
            }
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private static string TaxLevelLabel(CityTaxLevel level)
        {
            if (level == CityTaxLevel.Low) return "\u4f4e";
            if (level == CityTaxLevel.High) return "\u9ad8";
            return "\u6807\u51c6";
        }

        private static string ServiceBudgetLevelLabel(CityServiceBudgetLevel level)
        {
            if (level == CityServiceBudgetLevel.Lean) return "\u7cbe\u7b80";
            if (level == CityServiceBudgetLevel.Boosted) return "\u52a0\u5f3a";
            return "\u6807\u51c6";
        }

        private static string CityPolicyLabel(CityPolicy policy)
        {
            if (policy == CityPolicy.GreenCode) return "\u7eff\u8272\u89c4\u8303";
            if (policy == CityPolicy.TransitPriority) return "\u516c\u4ea4\u4f18\u5148";
            if (policy == CityPolicy.GrowthGrants) return "\u589e\u957f\u8865\u8d34";
            if (policy == CityPolicy.AffordableHousing) return "\u4fdd\u969c\u4f4f\u623f";
            if (policy == CityPolicy.TrafficSafetyCampaign) return "\u4ea4\u901a\u5b89\u5168";
            if (policy == CityPolicy.CompleteStreets) return "\u5b8c\u6574\u8857\u9053";
            if (policy == CityPolicy.SignalOptimization) return "\u4fe1\u53f7\u4f18\u5316";
            if (policy == CityPolicy.CongestionPricing) return "\u62e5\u5835\u6536\u8d39";
            if (policy == CityPolicy.ParkingFees) return "\u505c\u8f66\u6536\u8d39";
            return policy.ToString();
        }

        private void PlaceBuildingInternal(string buildingId, GridPos pos, bool chargeCash, bool autoDeveloped = false)
        {
            var definition = config.GetBuilding(buildingId);
            if (definition == null)
            {
                return;
            }

            var id = "building-" + nextId;
            nextId += 1;
            Grid.OccupyBuilding(id, pos, definition.Size);
            buildings.Add(new PlacedBuilding
            {
                Id = id,
                ConfigId = definition.Id,
                Pos = pos,
                Size = definition.Size,
                Level = 1,
                AutoDeveloped = autoDeveloped,
                ConnectedRoadId = NearestRoadId(pos, definition.Size)
            });

            if (chargeCash)
            {
                Metrics.Cash -= definition.Cost;
            }
        }

        private void SeedStartingRoad()
        {
            var y = config.MapHeight / 2;
            var from = Math.Max(4, config.MapWidth / 2 - 5);
            var to = Math.Min(config.MapWidth - 5, from + 10);
            for (var x = from; x <= to; x += 1)
            {
                var pos = new GridPos(x, y);
                if (Grid.CanPlaceRoad(pos))
                {
                    AddRoadTile(pos);
                }
            }
        }

        private void SeedStartingZones()
        {
            var centerX = config.MapWidth / 2;
            var centerY = config.MapHeight / 2;
            TrySeedZone(new GridPos(centerX - 6, centerY - 6), new GridSize(6, 4), ZoneType.Residential);
            TrySeedZone(new GridPos(centerX + 1, centerY - 6), new GridSize(5, 4), ZoneType.Commercial);
            TrySeedZone(new GridPos(centerX + 6, centerY - 7), new GridSize(5, 5), ZoneType.Industrial);
            TrySeedZone(new GridPos(centerX - 9, centerY - 1), new GridSize(4, 3), ZoneType.Office);
            TrySeedZone(new GridPos(centerX - 2, centerY - 1), new GridSize(5, 3), ZoneType.MixedUse);
            TrySeedZone(new GridPos(centerX - 9, centerY + 2), new GridSize(7, 4), ZoneType.Utility);
            TrySeedZone(new GridPos(centerX + 2, centerY + 2), new GridSize(5, 4), ZoneType.Civic);
        }

        private void SeedStarterBuildings()
        {
            if (config.Buildings.Count == 0)
            {
                return;
            }

            var centerX = config.MapWidth / 2;
            var centerY = config.MapHeight / 2;
            TrySeed("residential_pod", new GridPos(centerX - 5, centerY - 4));
            TrySeed("residential_pod", new GridPos(centerX - 2, centerY - 4));
            TrySeed("market_corner", new GridPos(centerX + 2, centerY - 4));
            TrySeed("maker_yard", new GridPos(centerX + 6, centerY - 5));
            TrySeed("micro_power", new GridPos(centerX - 8, centerY + 3));
            TrySeed("water_tower", new GridPos(centerX - 5, centerY + 3));
        }

        private void TrySeed(string buildingId, GridPos pos)
        {
            var definition = config.GetBuilding(buildingId);
            if (definition == null || !string.IsNullOrEmpty(Grid.CanPlaceBuilding(pos, definition.Size)))
            {
                return;
            }

            PlaceBuildingInternal(buildingId, pos, false);
        }

        private void RestoreBuilding(SavedBuilding saved)
        {
            if (saved == null)
            {
                return;
            }

            var definition = config.GetBuilding(saved.ConfigId);
            if (definition == null || !string.IsNullOrEmpty(Grid.CanPlaceBuilding(saved.Pos, definition.Size)))
            {
                return;
            }

            var id = string.IsNullOrEmpty(saved.Id) ? "building-" + nextId : saved.Id;
            Grid.OccupyBuilding(id, saved.Pos, definition.Size);
            buildings.Add(new PlacedBuilding
            {
                Id = id,
                ConfigId = definition.Id,
                Pos = saved.Pos,
                Size = definition.Size,
                AgeDays = Math.Max(0, saved.AgeDays),
                Level = Math.Max(1, Math.Min(3, saved.Level)),
                AutoDeveloped = saved.AutoDeveloped,
                ConnectedRoadId = NearestRoadId(saved.Pos, definition.Size)
            });
            nextId = Math.Max(nextId, NextIdAfter(id));
        }

        private void TrySeedZone(GridPos pos, GridSize size, ZoneType zone)
        {
            foreach (var tilePos in Grid.PositionsInRect(pos, size))
            {
                if (string.IsNullOrEmpty(Grid.CanSetZone(tilePos, zone)))
                {
                    Grid.SetZone(tilePos, zone);
                }
            }
        }

        private void AddRoadTile(GridPos pos, RoadTier tier = RoadTier.Local)
        {
            var id = "road-" + pos.X + "-" + pos.Y;
            Grid.SetRoad(pos, id);
            roads.Add(new RoadNode
            {
                Id = id,
                Pos = pos,
                Tier = tier,
                Capacity = RoadCapacityForTier(tier)
            });
        }

        private void RefreshBuildingRoadConnections()
        {
            for (var i = 0; i < buildings.Count; i += 1)
            {
                buildings[i].ConnectedRoadId = NearestRoadId(buildings[i].Pos, buildings[i].Size);
            }
        }

        private void RefreshRoadNeighborCounts()
        {
            for (var i = 0; i < roads.Count; i += 1)
            {
                var pos = roads[i].Pos;
                roads[i].Capacity = RoadCapacityForTier(roads[i].Tier);
                var count = 0;
                if (HasRoad(new GridPos(pos.X + 1, pos.Y))) count += 1;
                if (HasRoad(new GridPos(pos.X - 1, pos.Y))) count += 1;
                if (HasRoad(new GridPos(pos.X, pos.Y + 1))) count += 1;
                if (HasRoad(new GridPos(pos.X, pos.Y - 1))) count += 1;
                roads[i].NeighborCount = count;
            }
        }

        private bool UpdateBuildingLevels()
        {
            var changed = false;
            var upgraded = 0;
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var placed = buildings[i];
                var definition = config.GetBuilding(placed.ConfigId);
                if (!IsUpgradeableBuilding(definition))
                {
                    continue;
                }

                var level = BuildingLevel(placed);
                if (level >= 3 || placed.AgeDays < RequiredAgeForNextLevel(level))
                {
                    continue;
                }

                if (BuildingUpgradeScore(placed, definition) >= RequiredScoreForNextLevel(level))
                {
                    placed.Level = level + 1;
                    changed = true;
                    upgraded += 1;
                }
            }

            if (upgraded > 0)
            {
                AddCityEvent("\u5efa\u7b51\u5347\u7ea7\uff1a" + upgraded + " \u680b");
            }

            return changed;
        }

        private int BuildingUpgradeScore(PlacedBuilding placed, BuildingDefinition definition)
        {
            var landValue = AverageBuildingTileValue(placed, true);
            var transitAccess = AverageBuildingTileValue(placed, false);
            var siteQuality = DevelopmentQualityForBuilding(placed, definition);
            var score = landValue + transitAccess / 4 + siteQuality / 8 + Metrics.DevelopmentQuality / 20 + (string.IsNullOrEmpty(placed.ConnectedRoadId) ? -18 : 8);

            if (definition.Category == BuildingCategory.Residential)
            {
                score += Metrics.ParkCoverage / 10 + Metrics.HealthCoverage / 14 + Metrics.EducationCoverage / 16 + Metrics.SafetyCoverage / 20;
            }
            else if (definition.Category == BuildingCategory.Commercial)
            {
                score += transitAccess / 5 + Metrics.LogisticsCoverage / 18 + Metrics.EducationCoverage / 18 + Metrics.SafetyCoverage / 22 + Metrics.SecurityCoverage / 18;
                if (IsMixedUseBuilding(definition))
                {
                    score += Metrics.ParkCoverage / 14 + Metrics.HealthCoverage / 16 + Metrics.ServiceCoverage / 18 + Metrics.AdvancedEducationCoverage / 22;
                }

                if (IsOfficeBuilding(definition))
                {
                    score += Metrics.WorkforceSkill / 12 + Metrics.EducationCoverage / 12 + Metrics.AdvancedEducationCoverage / 10 + Metrics.SecurityCoverage / 20;
                }
            }
            else if (definition.Category == BuildingCategory.Industrial)
            {
                score += transitAccess / 8 + Metrics.LogisticsCoverage / 14 + Metrics.EducationCoverage / 20 + Metrics.AdvancedEducationCoverage / 28 + Metrics.SafetyCoverage / 24 - definition.Pollution;
            }

            score += Metrics.WasteCoverage / 20;
            score -= definition.Noise / 2;
            return score;
        }

        private int BuildingUpgradeReadinessAdvisor()
        {
            string focus;
            string driver;
            string action;
            int readyCount;
            int blockedCount;
            var score = ComputeBuildingUpgradeReadiness(out readyCount, out blockedCount, out focus, out driver, out action);
            Metrics.BuildingUpgradeReadyCount = readyCount;
            Metrics.BuildingUpgradeBlockedCount = blockedCount;
            Metrics.BuildingUpgradeReadinessFocus = focus;
            Metrics.BuildingUpgradeReadinessDriver = driver;
            Metrics.BuildingUpgradeReadinessAction = action;
            return score;
        }

        private int ComputeBuildingUpgradeReadiness(out int readyCount, out int blockedCount, out string focus, out string driver, out string action)
        {
            // BUILDING_UPGRADE_READINESS_ADVISOR explains why growable buildings are not leveling yet.
            var bestScore = 0;
            var eligibleCount = 0;
            var maturingCount = 0;
            var largestAgeGap = 0;
            readyCount = 0;
            blockedCount = 0;
            focus = "\u6210\u957f\u4e2d";
            driver = "\u7b49\u5f85\u6210\u719f";
            action = "\u7ee7\u7eed\u63d0\u5347\u533a\u4f4d\u4e0e\u670d\u52a1";

            for (var i = 0; i < buildings.Count; i += 1)
            {
                var placed = buildings[i];
                var definition = config.GetBuilding(placed.ConfigId);
                if (!IsUpgradeableBuilding(definition))
                {
                    continue;
                }

                var level = BuildingLevel(placed);
                if (level >= 3)
                {
                    continue;
                }

                eligibleCount += 1;
                var requiredAge = RequiredAgeForNextLevel(level);
                var requiredScore = RequiredScoreForNextLevel(level);
                var upgradeScore = BuildingUpgradeScore(placed, definition);
                var ageReady = placed.AgeDays >= requiredAge;

                if (upgradeScore >= requiredScore)
                {
                    if (ageReady)
                    {
                        readyCount += 1;
                        AddBuildingUpgradeCandidate(ref bestScore, ref focus, ref driver, ref action, 70 + Math.Min(20, readyCount * 4), "\u53ef\u5347\u7ea7", "\u5019" + readyCount + "/\u5206" + upgradeScore, "\u7b49\u5f85\u81ea\u52a8\u5347\u7ea7\u5e76\u7ee7\u7eed\u63d0\u5bc6");
                    }
                    else
                    {
                        maturingCount += 1;
                        largestAgeGap = Math.Max(largestAgeGap, requiredAge - placed.AgeDays);
                    }

                    continue;
                }

                if (!ageReady && upgradeScore >= requiredScore - 8)
                {
                    maturingCount += 1;
                    largestAgeGap = Math.Max(largestAgeGap, requiredAge - placed.AgeDays);
                    continue;
                }

                if (!ageReady && placed.AgeDays < requiredAge / 2)
                {
                    continue;
                }

                blockedCount += 1;
                string blockerFocus;
                string blockerDriver;
                string blockerAction;
                var blockerPressure = BuildingUpgradeBlocker(placed, definition, out blockerFocus, out blockerDriver, out blockerAction);
                var pressure = Math.Max(0, requiredScore - upgradeScore) + blockerPressure + (level == 2 ? 8 : 0) + Math.Min(10, blockedCount);
                AddBuildingUpgradeCandidate(ref bestScore, ref focus, ref driver, ref action, pressure, blockerFocus, blockerDriver, blockerAction);
            }

            if (bestScore == 0 && maturingCount > 0)
            {
                AddBuildingUpgradeCandidate(ref bestScore, ref focus, ref driver, ref action, 42 + Math.Min(12, maturingCount * 2), "\u6210\u719f\u65f6\u95f4", "\u5019" + maturingCount + "/\u5dee" + largestAgeGap + "\u5929", "\u7ef4\u6301\u670d\u52a1\u7b49\u5efa\u7b51\u6210\u719f");
            }

            if (bestScore == 0 && eligibleCount > 0 && Metrics.Population >= 220 && Metrics.UpgradedBuildings == 0)
            {
                AddBuildingUpgradeCandidate(ref bestScore, ref focus, ref driver, ref action, 52, "\u5347\u7ea7\u505c\u6ede", "\u5efa" + eligibleCount + "/\u5df2\u5347" + Metrics.UpgradedBuildings, "\u63d0\u5730\u4ef7\u3001\u516c\u4ea4\u548c\u670d\u52a1");
            }

            return ClampToScore(bestScore);
        }

        private int BuildingUpgradeBlocker(PlacedBuilding placed, BuildingDefinition definition, out string focus, out string driver, out string action)
        {
            var bestPressure = 0;
            focus = "\u533a\u4f4d\u8d28\u91cf";
            driver = "\u6210\u957f\u6761\u4ef6\u4e0d\u8db3";
            action = "\u63d0\u5347\u5730\u4ef7\u3001\u670d\u52a1\u548c\u4ea4\u901a";

            var landValue = AverageBuildingTileValue(placed, true);
            var transitAccess = AverageBuildingTileValue(placed, false);
            var siteQuality = DevelopmentQualityForBuilding(placed, definition);

            AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, string.IsNullOrEmpty(placed.ConnectedRoadId) ? 72 : 0, "\u672a\u63a5\u8def", "\u63a5\u8def\u7f3a\u5931", "\u5148\u63a5\u9053\u8def\u518d\u63d0\u670d\u52a1");
            AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, Math.Max(0, 68 - landValue), "\u5730\u4ef7", "\u5730" + landValue + "/\u5747" + Metrics.AverageLandValue, "\u8865\u516c\u56ed\u670d\u52a1\u63d0\u5730\u4ef7");
            AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, Math.Max(0, 56 - transitAccess) + Math.Max(0, Metrics.RoadHierarchyPressure - 60) / 2, "\u4ea4\u901a", "\u516c\u4ea4" + transitAccess + "/\u8def" + Metrics.RoadHierarchyPressure, "\u8865\u516c\u4ea4\u6216\u5347\u4e3b\u5e72");
            AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, Math.Max(0, 64 - siteQuality) + Math.Max(0, Metrics.LandUseConflict - 25) / 2, "\u9009\u5740", "\u8d28" + siteQuality + "/\u51b2" + Metrics.LandUseConflict, "\u4f18\u5316\u5206\u533a\u548c\u7f13\u51b2");

            if (definition.Category == BuildingCategory.Residential)
            {
                var servicePressure = Math.Max(Math.Max(0, 56 - Metrics.ParkCoverage), Math.Max(0, 52 - Metrics.HealthCoverage));
                servicePressure = Math.Max(servicePressure, Math.Max(Math.Max(0, 50 - Metrics.EducationCoverage), Math.Max(0, 48 - Metrics.SafetyCoverage)));
                AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, servicePressure, "\u4f4f\u5b85\u670d\u52a1", "\u516c" + Metrics.ParkCoverage + "/\u533b" + Metrics.HealthCoverage + "/\u5b66" + Metrics.EducationCoverage, "\u8865\u516c\u56ed\u533b\u7597\u6559\u80b2");
            }
            else if (definition.Category == BuildingCategory.Commercial)
            {
                var commercePressure = Math.Max(Math.Max(0, 54 - Metrics.LogisticsCoverage), Math.Max(0, 50 - Metrics.SecurityCoverage));
                commercePressure = Math.Max(commercePressure, Math.Max(0, 52 - transitAccess));
                AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, commercePressure, "\u5546\u4e1a\u914d\u5957", "\u8d27" + Metrics.LogisticsCoverage + "/\u5b89" + Metrics.SecurityCoverage, "\u8865\u8d27\u8fd0\u6cbb\u5b89\u548c\u5ba2\u6d41");

                if (IsOfficeBuilding(definition))
                {
                    var officePressure = Math.Max(Math.Max(0, 54 - Metrics.WorkforceSkill), Math.Max(0, 45 - Metrics.AdvancedEducationCoverage));
                    officePressure = Math.Max(officePressure, Math.Max(0, 50 - Metrics.CommunicationCoverage));
                    AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, officePressure, "\u4eba\u624d\u901a\u4fe1", "\u4eba" + Metrics.WorkforceSkill + "/\u9ad8" + Metrics.AdvancedEducationCoverage, "\u8865\u9ad8\u6559\u3001\u901a\u4fe1\u548c\u7814\u53d1");
                }
            }
            else if (definition.Category == BuildingCategory.Industrial)
            {
                var industryPressure = Math.Max(Math.Max(0, 58 - Metrics.LogisticsCoverage), Math.Max(0, 48 - Metrics.SafetyCoverage));
                industryPressure = Math.Max(industryPressure, Math.Max(0, 58 - Metrics.SupplyChainStability));
                AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, industryPressure, "\u4ea7\u4e1a\u7269\u6d41", "\u8d27" + Metrics.LogisticsCoverage + "/\u94fe" + Metrics.SupplyChainStability, "\u8865\u8d27\u8fd0\u4ed3\u50a8\u548c\u5b89\u5168");
            }

            var environmentPressure = Math.Max(0, 52 - Metrics.EnvironmentQuality) + definition.Pollution + definition.Noise / 2;
            AddBuildingUpgradeBlockerCandidate(ref bestPressure, ref focus, ref driver, ref action, environmentPressure, "\u73af\u5883", "\u73af" + Metrics.EnvironmentQuality + "/\u566a" + Metrics.NoiseStress, "\u8865\u7eff\u5316\u56de\u6536\u5e76\u964d\u566a");

            return ClampToScore(bestPressure);
        }

        private static void AddBuildingUpgradeCandidate(ref int bestScore, ref string focus, ref string driver, ref string action, int score, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedScore = ClampToScore(score);
            if (normalizedScore <= bestScore)
            {
                return;
            }

            bestScore = normalizedScore;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private static void AddBuildingUpgradeBlockerCandidate(ref int bestPressure, ref string focus, ref string driver, ref string action, int pressure, string candidateFocus, string candidateDriver, string candidateAction)
        {
            var normalizedPressure = ClampToScore(pressure);
            if (normalizedPressure <= bestPressure)
            {
                return;
            }

            bestPressure = normalizedPressure;
            focus = candidateFocus;
            driver = candidateDriver;
            action = candidateAction;
        }

        private int AverageBuildingTileValue(PlacedBuilding building, bool landValue)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(building.Pos, building.Size))
            {
                if (!Grid.InBounds(tilePos))
                {
                    continue;
                }

                var tile = Grid.GetTile(tilePos);
                total += landValue ? tile.LandValue : tile.TransitAccess;
                count += 1;
            }

            return count == 0 ? 0 : total / count;
        }

        private bool HasRoad(GridPos pos)
        {
            return Grid.InBounds(pos) && !string.IsNullOrEmpty(Grid.GetTile(pos).RoadId);
        }

        private RoadNode FindRoadAt(GridPos pos)
        {
            if (!Grid.InBounds(pos))
            {
                return null;
            }

            var roadId = Grid.GetTile(pos).RoadId;
            if (string.IsNullOrEmpty(roadId))
            {
                return null;
            }

            for (var i = 0; i < roads.Count; i += 1)
            {
                if (roads[i].Id == roadId)
                {
                    return roads[i];
                }
            }

            return null;
        }

        private void AddRoadLoad(string roadId, int load)
        {
            for (var i = 0; i < roads.Count; i += 1)
            {
                if (roads[i].Id == roadId)
                {
                    roads[i].Load += load;
                    var noise = roads[i].Tier == RoadTier.Arterial ? load / 5 : load / 8;
                    var landValueDelta = roads[i].Tier == RoadTier.Arterial ? -load / 28 : -load / 40;
                    Grid.AddTilePressure(roads[i].Pos, load, 0, noise, landValueDelta);
                    return;
                }
            }
        }

        private string NearestRoadId(GridPos origin, GridSize size)
        {
            string closestId = string.Empty;
            var closestDistance = int.MaxValue;
            var maxX = origin.X + size.W - 1;
            var maxY = origin.Y + size.H - 1;

            for (var i = 0; i < roads.Count; i += 1)
            {
                var pos = roads[i].Pos;
                var dx = pos.X < origin.X ? origin.X - pos.X : pos.X > maxX ? pos.X - maxX : 0;
                var dy = pos.Y < origin.Y ? origin.Y - pos.Y : pos.Y > maxY ? pos.Y - maxY : 0;
                var distance = dx + dy;
                if (distance <= config.MaxRoadSearchDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestId = roads[i].Id;
                }
            }

            return closestId;
        }

        private int NearestRoadDistance(GridPos origin, GridSize size)
        {
            var closestDistance = int.MaxValue;
            var maxX = origin.X + size.W - 1;
            var maxY = origin.Y + size.H - 1;

            for (var i = 0; i < roads.Count; i += 1)
            {
                var pos = roads[i].Pos;
                var dx = pos.X < origin.X ? origin.X - pos.X : pos.X > maxX ? pos.X - maxX : 0;
                var dy = pos.Y < origin.Y ? origin.Y - pos.Y : pos.Y > maxY ? pos.Y - maxY : 0;
                var distance = dx + dy;
                if (distance <= config.MaxRoadSearchDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance == int.MaxValue ? config.MaxRoadSearchDistance + 1 : closestDistance;
        }

        private List<PlacedBuilding> ConnectedParkBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsParkBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedHealthBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsHealthBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedDeathcareBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsDeathcareBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedEducationBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsEducationBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedAdvancedEducationBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsAdvancedEducationBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedInnovationBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsInnovationBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedAttractionBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsAttractionBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedShelterBuildings()
        {
            var shelters = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsShelterBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    shelters.Add(buildings[i]);
                }
            }

            return shelters;
        }

        private List<PlacedBuilding> ConnectedSafetyBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsSafetyBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedFireBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsSafetyBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedSecurityBuildings()
        {
            var services = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsSecurityBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    services.Add(buildings[i]);
                }
            }

            return services;
        }

        private List<PlacedBuilding> ConnectedTransitBuildings()
        {
            var transit = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsTransitBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    transit.Add(buildings[i]);
                }
            }

            return transit;
        }

        private List<PlacedBuilding> ConnectedRegionalConnectionBuildings()
        {
            var regional = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsRegionalConnectionBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId))
                {
                    regional.Add(buildings[i]);
                }
            }

            return regional;
        }

        private List<PlacedBuilding> ConnectedLogisticsBuildings()
        {
            var logistics = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsLogisticsBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    logistics.Add(buildings[i]);
                }
            }

            return logistics;
        }

        private List<PlacedBuilding> ConnectedWarehouseBuildings()
        {
            var warehouses = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsWarehouseBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    warehouses.Add(buildings[i]);
                }
            }

            return warehouses;
        }

        private List<PlacedBuilding> ConnectedResourceBuildings()
        {
            var resources = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsResourceBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    resources.Add(buildings[i]);
                }
            }

            return resources;
        }

        private List<PlacedBuilding> ConnectedFreightRailBuildings()
        {
            var freightRail = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsFreightRailBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    freightRail.Add(buildings[i]);
                }
            }

            return freightRail;
        }

        private List<PlacedBuilding> ConnectedWasteBuildings()
        {
            var waste = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsWasteBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    waste.Add(buildings[i]);
                }
            }

            return waste;
        }

        private List<PlacedBuilding> ConnectedWastewaterBuildings()
        {
            var wastewater = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsWastewaterBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId))
                {
                    wastewater.Add(buildings[i]);
                }
            }

            return wastewater;
        }

        private List<PlacedBuilding> ConnectedCommunicationBuildings()
        {
            var communications = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsCommunicationBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    communications.Add(buildings[i]);
                }
            }

            return communications;
        }

        private List<PlacedBuilding> ConnectedMailBuildings()
        {
            var mail = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsMailBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    mail.Add(buildings[i]);
                }
            }

            return mail;
        }

        private List<PlacedBuilding> ConnectedRoadMaintenanceBuildings()
        {
            var maintenance = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsRoadMaintenanceBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    maintenance.Add(buildings[i]);
                }
            }

            return maintenance;
        }

        private List<PlacedBuilding> ConnectedParkingBuildings()
        {
            var parking = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsParkingBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    parking.Add(buildings[i]);
                }
            }

            return parking;
        }

        private List<PlacedBuilding> ConnectedStormwaterBuildings()
        {
            var stormwater = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsStormwaterBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId) &&
                    definition.ServiceRadius > 0)
                {
                    stormwater.Add(buildings[i]);
                }
            }

            return stormwater;
        }

        private List<PlacedBuilding> ConnectedAdministrationBuildings()
        {
            var administration = new List<PlacedBuilding>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var definition = config.GetBuilding(buildings[i].ConfigId);
                if (definition != null &&
                    IsAdministrationBuilding(definition) &&
                    !string.IsNullOrEmpty(buildings[i].ConnectedRoadId))
                {
                    administration.Add(buildings[i]);
                }
            }

            return administration;
        }

        private int RoadMaintenanceCoverageForRoads(List<PlacedBuilding> maintenanceBuildings)
        {
            if (roads.Count == 0)
            {
                return 0;
            }

            var eligible = 0;
            var covered = 0;
            for (var i = 0; i < roads.Count; i += 1)
            {
                var weight = RoadMaintenanceWeightForRoad(roads[i]);
                eligible += weight;
                if (IsRoadCoveredByService(roads[i].Pos, maintenanceBuildings))
                {
                    covered += weight;
                }
            }

            return BudgetAdjustedCoverage(eligible == 0 ? 0 : ClampToScore((int)Math.Round(covered * 100.0 / eligible)));
        }

        private int WasteCapacityForBuildings(List<PlacedBuilding> wasteBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < wasteBuildings.Count; i += 1)
            {
                capacity += WasteBuildingCapacity(config.GetBuilding(wasteBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int WastewaterCapacityForBuildings(List<PlacedBuilding> wastewaterBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < wastewaterBuildings.Count; i += 1)
            {
                capacity += WastewaterBuildingCapacity(config.GetBuilding(wastewaterBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int TransitCapacityForBuildings(List<PlacedBuilding> transitBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < transitBuildings.Count; i += 1)
            {
                capacity += TransitBuildingCapacity(config.GetBuilding(transitBuildings[i].ConfigId));
            }

            if (IsPolicyActive(CityPolicy.TransitPriority))
            {
                capacity = capacity * 120 / 100;
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int RegionalConnectionCapacityForBuildings(List<PlacedBuilding> regionalConnectionBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < regionalConnectionBuildings.Count; i += 1)
            {
                capacity += RegionalConnectionBuildingCapacity(config.GetBuilding(regionalConnectionBuildings[i].ConfigId));
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int LogisticsCapacityForBuildings(List<PlacedBuilding> logisticsBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < logisticsBuildings.Count; i += 1)
            {
                capacity += LogisticsBuildingCapacity(config.GetBuilding(logisticsBuildings[i].ConfigId));
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int CommunicationCapacityForBuildings(List<PlacedBuilding> communicationBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < communicationBuildings.Count; i += 1)
            {
                capacity += CommunicationBuildingCapacity(config.GetBuilding(communicationBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int MailCapacityForBuildings(List<PlacedBuilding> mailBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < mailBuildings.Count; i += 1)
            {
                capacity += MailBuildingCapacity(config.GetBuilding(mailBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int DeathcareCapacityForBuildings(List<PlacedBuilding> deathcareBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < deathcareBuildings.Count; i += 1)
            {
                capacity += DeathcareBuildingCapacity(config.GetBuilding(deathcareBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int HealthCapacityForBuildings(List<PlacedBuilding> healthBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < healthBuildings.Count; i += 1)
            {
                capacity += HealthBuildingCapacity(config.GetBuilding(healthBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int EducationCapacityForBuildings(List<PlacedBuilding> educationBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < educationBuildings.Count; i += 1)
            {
                capacity += EducationBuildingCapacity(config.GetBuilding(educationBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int ParkingCapacityForBuildings(List<PlacedBuilding> parkingBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < parkingBuildings.Count; i += 1)
            {
                capacity += ParkingBuildingCapacity(config.GetBuilding(parkingBuildings[i].ConfigId));
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int StormwaterCapacityForBuildings(List<PlacedBuilding> stormwaterBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < stormwaterBuildings.Count; i += 1)
            {
                capacity += StormwaterBuildingCapacity(config.GetBuilding(stormwaterBuildings[i].ConfigId));
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int AdministrationCapacityForBuildings(List<PlacedBuilding> administrationBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < administrationBuildings.Count; i += 1)
            {
                capacity += AdministrationBuildingCapacity(config.GetBuilding(administrationBuildings[i].ConfigId));
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int DisasterPreparednessCapacityForBuildings(List<PlacedBuilding> shelterBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < shelterBuildings.Count; i += 1)
            {
                capacity += DisasterPreparednessBuildingCapacity(config.GetBuilding(shelterBuildings[i].ConfigId));
            }

            return BudgetAdjustedServiceValue(capacity);
        }

        private int PublicServiceCapacityForBuildings(List<PlacedBuilding> healthBuildings, List<PlacedBuilding> educationBuildings, List<PlacedBuilding> safetyBuildings, List<PlacedBuilding> securityBuildings, List<PlacedBuilding> shelterBuildings)
        {
            var capacity = 0;
            capacity += PublicServiceCapacityForList(healthBuildings, 95);
            capacity += PublicServiceCapacityForList(educationBuildings, 90);
            capacity += PublicServiceCapacityForList(safetyBuildings, 85);
            capacity += PublicServiceCapacityForList(securityBuildings, 90);
            capacity += PublicServiceCapacityForList(shelterBuildings, 70);
            return BudgetAdjustedServiceValue(capacity);
        }

        private int FireCapacityForBuildings(List<PlacedBuilding> safetyBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < safetyBuildings.Count; i += 1)
            {
                capacity += FireBuildingCapacity(config.GetBuilding(safetyBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int SecurityCapacityForBuildings(List<PlacedBuilding> securityBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < securityBuildings.Count; i += 1)
            {
                capacity += SecurityBuildingCapacity(config.GetBuilding(securityBuildings[i].ConfigId));
            }

            return capacity;
        }

        private int PublicServiceCapacityForList(List<PlacedBuilding> serviceBuildings, int baseCapacity)
        {
            var capacity = 0;
            for (var i = 0; i < serviceBuildings.Count; i += 1)
            {
                capacity += PublicServiceBuildingCapacity(config.GetBuilding(serviceBuildings[i].ConfigId), baseCapacity);
            }

            return capacity;
        }

        private int AttractionScoreForBuildings(List<PlacedBuilding> attractionBuildings)
        {
            var score = 0;
            for (var i = 0; i < attractionBuildings.Count; i += 1)
            {
                var definition = config.GetBuilding(attractionBuildings[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                score += Math.Max(12, definition.ServiceValue * 2 + definition.ServiceRadius);
            }

            return score;
        }

        private int AttractionParkingDemandForBuildings(List<PlacedBuilding> attractionBuildings)
        {
            var demand = 0;
            for (var i = 0; i < attractionBuildings.Count; i += 1)
            {
                var definition = config.GetBuilding(attractionBuildings[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                demand += Math.Max(6, definition.ServiceValue / 2 + definition.TrafficGeneration / 2 + definition.Jobs / 8);
            }

            return demand;
        }

        private int LandmarkTourismIncomeForBuildings(List<PlacedBuilding> attractionBuildings)
        {
            var income = 0;
            for (var i = 0; i < attractionBuildings.Count; i += 1)
            {
                var definition = config.GetBuilding(attractionBuildings[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                income += Math.Max(10, definition.ServiceValue + definition.Jobs / 2 + definition.TrafficGeneration / 2);
            }

            return income;
        }

        private static int ComputeAttractiveness(int attractionScore, int serviceCoverage, int parkCoverage, int transitCoverage, int regionalConnectivity, int securityCoverage, int mailCoverage, int landValue, int pollution, int congestion, int crimePressure, int mixedUseBuildings)
        {
            return ClampToScore(12 + attractionScore + serviceCoverage / 4 + parkCoverage / 5 + transitCoverage / 4 + regionalConnectivity / 5 + securityCoverage / 5 + mailCoverage / 8 + landValue / 8 + mixedUseBuildings * 3 - pollution * 2 - congestion / 4 - crimePressure / 3);
        }

        private static int ComputeVisitors(int attractiveness, int population, int jobs, int landmarkBuildings, int regionalConnectivity)
        {
            if (attractiveness <= 0)
            {
                return 0;
            }

            var baseDraw = population / 10 + jobs / 14 + landmarkBuildings * 18 + regionalConnectivity * 4;
            var regionalMultiplier = 100 + regionalConnectivity / 4;
            return Math.Max(0, baseDraw * attractiveness * regionalMultiplier / 10000);
        }

        private static int RegionalTourismBonus(int regionalConnectivity)
        {
            return Math.Max(0, regionalConnectivity / 4);
        }

        private static int ComputeGoodsDemand(int population, int commercialGoodsJobs, int visitors, int mixedUseBuildings)
        {
            return Math.Max(0, population / 3 + commercialGoodsJobs * 2 + visitors / 3 + mixedUseBuildings * 8);
        }

        private int ResourceSpecializationForBuildings(List<PlacedBuilding> resourceBuildings)
        {
            return ResourcePotentialForBuildings(resourceBuildings);
        }

        private int ResourcePotentialForBuildings(List<PlacedBuilding> resourceBuildings)
        {
            var total = 0;
            var count = 0;
            for (var i = 0; i < resourceBuildings.Count; i += 1)
            {
                var definition = config.GetBuilding(resourceBuildings[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                total += ResourcePotentialForBuilding(resourceBuildings[i], definition);
                count += 1;
            }

            return count == 0 ? 0 : ClampToScore(total / count);
        }

        private int ResourcePotentialForBuilding(PlacedBuilding placed, BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            var terrain = TerrainResourcePotentialForRect(placed.Pos, definition.Size);
            var zoneFit = IndustrialZoneFitForRect(placed.Pos, definition.Size);
            var logisticsFit = AverageLogisticsAccessForRect(placed.Pos, definition.Size);
            return ClampToScore(terrain * 40 / 100 + zoneFit * 30 / 100 + logisticsFit * 30 / 100);
        }

        private int TerrainResourcePotentialForRect(GridPos pos, GridSize size)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(pos, size))
            {
                if (!Grid.InBounds(tilePos))
                {
                    continue;
                }

                var tile = Grid.GetTile(tilePos);
                total += tile.Terrain == TerrainType.Hill ? 100 : tile.Terrain == TerrainType.Plain ? 40 : 0;
                count += 1;
            }

            return count == 0 ? 0 : ClampToScore(total / count);
        }

        private int IndustrialZoneFitForRect(GridPos pos, GridSize size)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(pos, size))
            {
                if (!Grid.InBounds(tilePos))
                {
                    continue;
                }

                var zone = Grid.GetTile(tilePos).Zone;
                if (zone == ZoneType.Industrial)
                {
                    total += 100;
                }
                else if (zone == ZoneType.Utility)
                {
                    total += 55;
                }
                else if (zone == ZoneType.None)
                {
                    total += 35;
                }
                else
                {
                    total += 12;
                }

                count += 1;
            }

            return count == 0 ? 0 : ClampToScore(total / count);
        }

        private int AverageLogisticsAccessForRect(GridPos pos, GridSize size)
        {
            var total = 0;
            var count = 0;
            foreach (var tilePos in Grid.PositionsInRect(pos, size))
            {
                if (!Grid.InBounds(tilePos))
                {
                    continue;
                }

                total += Grid.GetTile(tilePos).LogisticsAccess;
                count += 1;
            }

            return count == 0 ? 0 : ClampToScore(total / count);
        }

        private static int ComputeResourceSpecialization(int resourcePotential, int logisticsCoverage, int utilityReliability, int workforceSkill)
        {
            if (resourcePotential <= 0)
            {
                return 0;
            }

            return ClampToScore(resourcePotential * 55 / 100 + logisticsCoverage / 5 + utilityReliability / 8 + workforceSkill / 8);
        }

        private static int ComputeIndustrialSpecialization(int resourceSpecialization, int logisticsCoverage, int industrialZoneTiles, int industrialJobs)
        {
            if (resourceSpecialization <= 0)
            {
                return 0;
            }

            var zoneCommitment = Math.Min(24, industrialZoneTiles);
            var productionBase = Math.Min(18, industrialJobs / 8);
            return ClampToScore(resourceSpecialization * 65 / 100 + logisticsCoverage / 4 + zoneCommitment + productionBase);
        }

        private int ComputeLocalGoodsSupply(List<PlacedBuilding> resourceBuildings, int logisticsCoverage, int utilityReliability, int workforceSkill, int resourceSpecialization)
        {
            var baseSupply = 0;
            for (var i = 0; i < resourceBuildings.Count; i += 1)
            {
                baseSupply += ResourceBuildingSupply(config.GetBuilding(resourceBuildings[i].ConfigId));
            }

            if (baseSupply <= 0)
            {
                return 0;
            }

            var support = Math.Min(135, Math.Max(35, logisticsCoverage / 2 + utilityReliability / 3 + workforceSkill / 5 + resourceSpecialization / 3));
            return Math.Max(0, baseSupply * support / 100);
        }

        private int ComputeFreightImportSupply(List<PlacedBuilding> freightRailBuildings, int logisticsCoverage, int logisticsUtilization, int utilityReliability)
        {
            var baseSupply = 0;
            for (var i = 0; i < freightRailBuildings.Count; i += 1)
            {
                baseSupply += FreightRailImportSupply(config.GetBuilding(freightRailBuildings[i].ConfigId));
            }

            if (baseSupply <= 0)
            {
                return 0;
            }

            var overloadPenalty = Math.Max(0, logisticsUtilization - 100) / 2;
            var support = Math.Max(35, logisticsCoverage / 2 + utilityReliability / 3 + 30 - overloadPenalty);
            return Math.Max(0, baseSupply * support / 100);
        }

        private int ComputeGoodsStorage(List<PlacedBuilding> warehouseBuildings, int logisticsCoverage, int logisticsUtilization, int utilityReliability)
        {
            var baseStorage = 0;
            for (var i = 0; i < warehouseBuildings.Count; i += 1)
            {
                baseStorage += WarehouseStorageCapacity(config.GetBuilding(warehouseBuildings[i].ConfigId));
            }

            if (baseStorage <= 0)
            {
                return 0;
            }

            var overloadPenalty = Math.Max(0, logisticsUtilization - 100) / 2;
            var support = Math.Max(0, logisticsCoverage / 2 + utilityReliability / 3 + 10 - overloadPenalty);
            return Math.Max(0, baseStorage * support / 100);
        }

        private static int ComputeSupplyChainStability(int goodsStorage, int rawGoodsSupply, int goodsDemand, int logisticsCoverage, int logisticsUtilization)
        {
            if (goodsDemand <= 0)
            {
                return goodsStorage > 0 ? 100 : 55;
            }

            var storageCoverage = ClampToScore((int)Math.Round(goodsStorage * 100.0 / Math.Max(1, goodsDemand / 2)));
            var supplyCoverage = ClampToScore((int)Math.Round(rawGoodsSupply * 100.0 / Math.Max(1, goodsDemand)));
            var logisticsHealth = ClampToScore(logisticsCoverage - Math.Max(0, logisticsUtilization - 100) / 2);
            return ClampToScore(12 + storageCoverage / 3 + supplyCoverage / 5 + logisticsHealth / 2);
        }

        private static int ApplyGoodsStorageBuffer(int rawGoodsSupply, int goodsDemand, int goodsStorage, int supplyChainStability)
        {
            if (goodsDemand <= rawGoodsSupply || goodsStorage <= 0)
            {
                return rawGoodsSupply;
            }

            var deficit = goodsDemand - rawGoodsSupply;
            var availableBuffer = goodsStorage * Math.Max(30, supplyChainStability) / 100;
            return rawGoodsSupply + Math.Min(deficit, availableBuffer);
        }

        private static int ComputeGoodsSupply(int industrialJobs, int logisticsCoverage, int workforceSkill, int logisticsUtilization, int regionalConnectivity, int localGoodsSupply, int freightImportSupply)
        {
            var importedGoods = Math.Max(0, regionalConnectivity / 2);
            if (industrialJobs <= 0)
            {
                return importedGoods + localGoodsSupply + freightImportSupply;
            }

            var logisticsFactor = Math.Max(35, 60 + logisticsCoverage / 2 - Math.Max(0, logisticsUtilization - 100) / 3);
            var workforceFactor = 85 + Math.Max(0, workforceSkill - 45) / 3;
            return Math.Max(0, industrialJobs * 3 * logisticsFactor * workforceFactor / 10000 + importedGoods + localGoodsSupply + freightImportSupply);
        }

        private static int ComputeRegionalConnectivity(int regionalConnectionCapacity, int population, int jobs)
        {
            if (population < 280 && regionalConnectionCapacity <= 0)
            {
                return 38;
            }

            var load = Math.Max(80, population / 4 + jobs / 6);
            if (regionalConnectionCapacity <= 0)
            {
                return population >= 680 ? 18 : 32;
            }

            var capacityScore = ClampToScore((int)Math.Round(regionalConnectionCapacity * 100.0 / load));
            return ClampToScore(20 + capacityScore * 80 / 100);
        }

        private static int ComputeGoodsBalance(int supply, int demand)
        {
            if (demand <= 0)
            {
                return supply > 0 ? 150 : 100;
            }

            return Math.Min(150, Math.Max(0, (int)Math.Round(supply * 100.0 / demand)));
        }

        private static int GoodsShortagePenalty(int goodsBalance, int goodsDemand)
        {
            return goodsDemand <= 0 ? 0 : Math.Max(0, 85 - goodsBalance);
        }

        private static int GoodsMarketBonus(int goodsBalance, int goodsDemand)
        {
            return goodsDemand <= 0 ? 0 : Math.Min(20, Math.Max(0, goodsBalance - 95));
        }

        private static int ComputeLandUseEfficiency(int developedZoneTiles, int growthZoneTiles)
        {
            if (growthZoneTiles <= 0)
            {
                return 0;
            }

            return ClampToScore((int)Math.Round(developedZoneTiles * 100.0 / growthZoneTiles));
        }

        private int ZoneConflictRiskForRect(List<GridPos> points, ZoneType zone)
        {
            if (points == null || points.Count == 0 || !IsLandUseConflictZone(zone))
            {
                return 0;
            }

            var total = 0;
            var count = 0;
            for (var i = 0; i < points.Count; i += 1)
            {
                if (!Grid.InBounds(points[i]))
                {
                    continue;
                }

                total += LandUseConflictForTile(points[i], zone);
                count += 1;
            }

            return count == 0 ? 0 : ClampToScore(total / count);
        }

        private int ComputeLandUseConflict()
        {
            var total = 0;
            var weight = 0;
            foreach (var pos in Grid.AllPositions())
            {
                var zone = Grid.GetTile(pos).Zone;
                if (!IsLandUseConflictZone(zone))
                {
                    continue;
                }

                var zoneWeight = LandUseConflictWeight(zone);
                total += LandUseConflictForTile(pos, zone) * zoneWeight;
                weight += zoneWeight;
            }

            return weight == 0 ? 0 : ClampToScore((int)Math.Round(total * 1.0 / weight));
        }

        private int LandUseConflictForTile(GridPos pos, ZoneType zone)
        {
            var tile = Grid.GetTile(pos);
            var conflict = (IsSensitiveZone(zone) ? tile.Pollution * 3 + tile.Noise * 2 : tile.Pollution + tile.Noise) / 2;
            conflict += LandUseConflictWithNeighbor(zone, new GridPos(pos.X + 1, pos.Y));
            conflict += LandUseConflictWithNeighbor(zone, new GridPos(pos.X - 1, pos.Y));
            conflict += LandUseConflictWithNeighbor(zone, new GridPos(pos.X, pos.Y + 1));
            conflict += LandUseConflictWithNeighbor(zone, new GridPos(pos.X, pos.Y - 1));
            return ClampToScore(conflict);
        }

        private int LandUseConflictWithNeighbor(ZoneType zone, GridPos neighborPos)
        {
            if (!Grid.InBounds(neighborPos))
            {
                return 0;
            }

            var neighbor = Grid.GetTile(neighborPos).Zone;
            if (neighbor == ZoneType.None || neighbor == zone)
            {
                return 0;
            }

            if (neighbor == ZoneType.Civic)
            {
                return -5;
            }

            if ((IsHazardZone(zone) && IsResidentialLikeZone(neighbor)) || (IsHazardZone(neighbor) && IsResidentialLikeZone(zone)))
            {
                return 42;
            }

            if ((IsHazardZone(zone) && neighbor == ZoneType.Office) || (IsHazardZone(neighbor) && zone == ZoneType.Office))
            {
                return 26;
            }

            if ((IsHazardZone(zone) && neighbor == ZoneType.Commercial) || (IsHazardZone(neighbor) && zone == ZoneType.Commercial))
            {
                return 14;
            }

            if ((zone == ZoneType.Residential && neighbor == ZoneType.Commercial) || (zone == ZoneType.Commercial && neighbor == ZoneType.Residential))
            {
                return 6;
            }

            if ((zone == ZoneType.Industrial && neighbor == ZoneType.Office) || (zone == ZoneType.Office && neighbor == ZoneType.Industrial))
            {
                return 18;
            }

            if ((neighbor == ZoneType.MixedUse && !IsHazardZone(zone)) || (zone == ZoneType.MixedUse && !IsHazardZone(neighbor)))
            {
                return -2;
            }

            return 0;
        }

        private static bool IsLandUseConflictZone(ZoneType zone)
        {
            return zone == ZoneType.Residential ||
                   zone == ZoneType.Commercial ||
                   zone == ZoneType.Industrial ||
                   zone == ZoneType.Office ||
                   zone == ZoneType.MixedUse ||
                   zone == ZoneType.Utility;
        }

        private static bool IsSensitiveZone(ZoneType zone)
        {
            return zone == ZoneType.Residential || zone == ZoneType.MixedUse || zone == ZoneType.Office;
        }

        private static bool IsResidentialLikeZone(ZoneType zone)
        {
            return zone == ZoneType.Residential || zone == ZoneType.MixedUse;
        }

        private static bool IsHazardZone(ZoneType zone)
        {
            return zone == ZoneType.Industrial || zone == ZoneType.Utility;
        }

        private static int LandUseConflictWeight(ZoneType zone)
        {
            if (zone == ZoneType.Residential || zone == ZoneType.MixedUse)
            {
                return 4;
            }

            if (zone == ZoneType.Office || zone == ZoneType.Commercial)
            {
                return 3;
            }

            return 2;
        }

        private static int LandUseConflictPenalty(int landUseConflict)
        {
            return landUseConflict <= 22 ? 0 : Math.Min(18, (landUseConflict - 22) / 2);
        }

        private static int LandUseBufferBonus(int landUseConflict)
        {
            return landUseConflict >= 18 ? 0 : Math.Min(6, (18 - landUseConflict) / 4);
        }

        private int ComputeDevelopmentQuality()
        {
            var total = 0;
            var weight = 0;
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var placed = buildings[i];
                var definition = config.GetBuilding(placed.ConfigId);
                if (!IsGrowthZoneBuilding(definition))
                {
                    continue;
                }

                var buildingWeight = DevelopmentQualityWeight(definition);
                total += DevelopmentQualityForBuilding(placed, definition) * buildingWeight;
                weight += buildingWeight;
            }

            return weight == 0 ? 55 : ClampToScore((int)Math.Round(total * 1.0 / weight));
        }

        private int DevelopmentQualityForBuilding(PlacedBuilding placed, BuildingDefinition definition)
        {
            var quality = ZoneSuitabilityForRect(placed.Pos, placed.Size, definition.PreferredZone);
            quality += Math.Min(10, (BuildingLevel(placed) - 1) * 5);
            if (placed.AutoDeveloped)
            {
                quality += 3;
            }

            if (string.IsNullOrEmpty(placed.ConnectedRoadId))
            {
                quality -= 18;
            }

            return ClampToScore(quality);
        }

        private static int DevelopmentQualityWeight(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            return Math.Max(1, footprint + definition.Capacity / 8 + definition.Jobs / 8);
        }

        private void RecordBuildingAction(string buildingId)
        {
            var definition = config.GetBuilding(buildingId);
            if (IsEducationBuilding(definition))
            {
                advisorContext.RecordAction("build_school");
            }
            else if (IsHealthBuilding(definition))
            {
                advisorContext.RecordAction("build_clinic");
            }
            else if (IsParkBuilding(definition) || IsSafetyBuilding(definition) || IsSecurityBuilding(definition) || (definition != null && definition.Category == BuildingCategory.Service))
            {
                advisorContext.RecordAction("build_service");
            }
            else
            {
                advisorContext.RecordAction("build");
            }
        }

        private static int DevelopmentQualityBonus(int developmentQuality)
        {
            return developmentQuality <= 58 ? 0 : Math.Min(10, (developmentQuality - 58) / 4);
        }

        private static int DevelopmentQualityPenalty(int developmentQuality)
        {
            return developmentQuality >= 45 ? 0 : Math.Min(16, (45 - developmentQuality) / 2);
        }

        private static int IdleZonePenalty(int landUseEfficiency, int idleZoneTiles)
        {
            if (idleZoneTiles < 18 || landUseEfficiency >= 55)
            {
                return 0;
            }

            return Math.Min(30, (55 - landUseEfficiency) / 2 + idleZoneTiles / 8);
        }

        private static int CompactLandUseBonus(int landUseEfficiency)
        {
            return landUseEfficiency <= 60 ? 0 : Math.Min(10, (landUseEfficiency - 60) / 4);
        }

        private static int ComputeRoadConnectivity(int roadTiles, int deadEndRoadTiles, int intersectionRoadTiles, int arterialRoadTiles, int connectedBuildings, int buildingCount)
        {
            if (roadTiles <= 0)
            {
                return 0;
            }

            var buildingAccess = buildingCount <= 0 ? 80 : ClampToScore((int)Math.Round(connectedBuildings * 100.0 / buildingCount));
            var deadEndRate = ClampToScore((int)Math.Round(deadEndRoadTiles * 100.0 / roadTiles));
            var intersectionBonus = Math.Min(18, intersectionRoadTiles * 160 / Math.Max(6, roadTiles));
            var arterialBonus = Math.Min(14, arterialRoadTiles * 2);
            return ClampToScore(28 + buildingAccess / 2 + intersectionBonus + arterialBonus - deadEndRate / 2);
        }

        private static int ComputeIntersectionDelay(int roadTiles, int intersectionRoadTiles, int deadEndRoadTiles, int arterialRoadTiles, int congestion, int roadConnectivity)
        {
            if (roadTiles < 8 || intersectionRoadTiles <= 0)
            {
                return 0;
            }

            var intersectionDensity = ClampToScore((int)Math.Round(intersectionRoadTiles * 100.0 / roadTiles));
            var junctionLoad = Math.Max(0, congestion - 35) / 2;
            var deadEndPenalty = Math.Min(16, deadEndRoadTiles * 2);
            var arterialRelief = Math.Min(14, arterialRoadTiles * 2);
            var connectivityRelief = roadConnectivity / 6;
            return ClampToScore(8 + intersectionDensity / 2 + junctionLoad + deadEndPenalty - arterialRelief - connectivityRelief);
        }

        private static int ComputeRoadBottleneckPressure(int congestion, int roadConnectivity, int deadEndRoadTiles, int intersectionRoadTiles, int arterialRoadTiles, int intersectionDelay, int roadTiles)
        {
            if (roadTiles < 8)
            {
                return 0;
            }

            var congestionStress = Math.Max(0, congestion - 40) / 2;
            var connectivityGap = Math.Max(0, 65 - roadConnectivity) / 2;
            var deadEndStress = Math.Min(18, deadEndRoadTiles * 2);
            var junctionStress = Math.Min(18, Math.Max(0, intersectionRoadTiles - arterialRoadTiles / 2) * 2);
            return ClampToScore(intersectionDelay / 2 + congestionStress + connectivityGap + deadEndStress + junctionStress);
        }

        private static int TransitReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int TransitUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int ComputeTransitWaitPressure(int rawTransitCoverage, int transitCoverage, int transitUtilization, int transitReliability, int congestion, int roadConnectivity, int serviceReliability)
        {
            if (rawTransitCoverage < 15)
            {
                return 0;
            }

            var crowding = Math.Max(0, transitUtilization - 85) / 2;
            var reliabilityGap = Math.Max(0, 85 - transitReliability) / 2;
            var effectiveCoverageDrop = Math.Max(0, rawTransitCoverage - transitCoverage) / 3;
            var congestionDelay = Math.Max(0, congestion - 35) / 3;
            var roadGap = Math.Max(0, 55 - roadConnectivity) / 3;
            var serviceGap = Math.Max(0, 80 - serviceReliability) / 4;
            return ClampToScore(crowding + reliabilityGap + effectiveCoverageDrop + congestionDelay + roadGap + serviceGap);
        }

        private static int TransitOverloadRoadLoad(int load, int capacity)
        {
            return Math.Max(0, load - capacity) / 5;
        }

        private static int LogisticsReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int LogisticsUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int LogisticsOverloadRoadLoad(int load, int capacity)
        {
            return Math.Max(0, load - capacity) / 4;
        }

        private static int ParkingReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int ParkingUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int StormwaterLoad(int population, int jobs, int roadTiles, int developedZoneTiles, int industrialJobs, int buildingCount, int terrainExposure)
        {
            if (population < 60 && roadTiles < 8 && developedZoneTiles < 8)
            {
                return 0;
            }

            return Math.Max(0, population / 7 + jobs / 10 + roadTiles * 3 + developedZoneTiles * 2 + industrialJobs / 5 + buildingCount * 2 + terrainExposure);
        }

        private int PolicyAdjustedStormwaterLoad(int rawLoad, int parkCoverage)
        {
            if (rawLoad <= 0)
            {
                return 0;
            }

            var completeStreetsRelief = IsPolicyActive(CityPolicy.CompleteStreets) ? 8 : 0;
            var reliefPercent = Math.Min(35, parkCoverage / 4 + completeStreetsRelief);
            return Math.Max(0, rawLoad * (100 - reliefPercent) / 100);
        }

        private static int StormwaterUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 160;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int StormwaterResilience(int load, int capacity, int parkCoverage)
        {
            if (load <= 0)
            {
                return ClampToScore(75 + parkCoverage / 5);
            }

            if (capacity <= 0)
            {
                return ClampToScore(30 + parkCoverage / 6);
            }

            var baseResilience = capacity >= load ? 100 : ClampToScore(100 - (load - capacity) * 100 / Math.Max(1, load));
            return ClampToScore(baseResilience + parkCoverage / 8);
        }

        private static int ComputeFloodRisk(int utilization, int resilience, int roadTiles, int developedZoneTiles, int parkCoverage, int landUseEfficiency)
        {
            var hardscapePressure = Math.Min(28, roadTiles / 4 + developedZoneTiles / 14);
            var compactRelief = Math.Max(0, landUseEfficiency - 55) / 6;
            return ClampToScore(Math.Max(0, utilization - 85) / 2 + Math.Max(0, 75 - resilience) / 2 + hardscapePressure - parkCoverage / 8 - compactRelief);
        }

        private int StormwaterTerrainExposure()
        {
            var exposure = 0;
            for (var y = 0; y < Grid.Height; y += 1)
            {
                for (var x = 0; x < Grid.Width; x += 1)
                {
                    var tile = Grid.GetTile(new GridPos(x, y));
                    if (tile.Terrain == TerrainType.Hill && (!string.IsNullOrEmpty(tile.RoadId) || !string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None))
                    {
                        exposure += 3;
                    }
                    else if (tile.Terrain == TerrainType.Water)
                    {
                        exposure += 1;
                    }
                }
            }

            return Math.Min(120, exposure);
        }

        private static int CommunicationReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int CommunicationUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int MailReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int MailUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int WasteReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int WasteUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int WastewaterLoad(int population, int jobs, int industrialJobs, int waterDemand)
        {
            return Math.Max(0, population / 3 + jobs / 10 + industrialJobs / 4 + waterDemand / 2);
        }

        private static int WastewaterReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int WastewaterUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int PublicServiceLoad(int population, int safetyEligible, int securityEligible)
        {
            var healthDemand = population;
            var educationDemand = population * 3 / 4;
            return Math.Max(0, healthDemand + educationDemand + safetyEligible + securityEligible);
        }

        private static int ResidentialServiceScore(bool transit, bool waste, bool safety, bool security, bool communication, bool mail, bool park, bool health, bool deathcare, bool education)
        {
            var score = 0;
            if (park) score += 22;
            if (health) score += 20;
            if (education) score += 16;
            if (transit) score += 14;
            if (safety) score += 10;
            if (security) score += 10;
            if (waste) score += 8;
            if (communication) score += 6;
            if (mail) score += 5;
            if (deathcare) score += 4;
            return ClampToScore(score);
        }

        private static void AddServiceGap(ref int gapWeight, bool missing, int buildingCapacity, int serviceWeight)
        {
            if (missing && buildingCapacity > 0 && serviceWeight > 0)
            {
                gapWeight += buildingCapacity * serviceWeight;
            }
        }

        private static int ComputeServiceEquity(int scoreTotal, int weight, int serviceCoverage, int serviceUtilization)
        {
            if (weight <= 0)
            {
                return serviceCoverage > 0 ? serviceCoverage : 70;
            }

            var localScore = ClampToScore((int)Math.Round(scoreTotal * 1.0 / weight));
            var overloadPenalty = Math.Max(0, serviceUtilization - 100) / 6;
            return ClampToScore((localScore * 3 + serviceCoverage) / 4 - overloadPenalty);
        }

        private static int ComputeUnderservedResidents(int population, int residentialCapacity, int underservedServiceWeight)
        {
            if (population <= 0 || residentialCapacity <= 0)
            {
                return 0;
            }

            var affectedCapacity = underservedServiceWeight / 55;
            return Math.Min(population, (int)Math.Round(population * affectedCapacity * 1.0 / residentialCapacity));
        }

        private static int ComputeServiceGapPressure(int residentialCapacity, int park, int health, int education, int transit, int safety, int security, int waste, int communication, int mail, int deathcare)
        {
            if (residentialCapacity <= 0)
            {
                return 0;
            }

            var totalGapWeight = park + health + education + transit + safety + security + waste + communication + mail + deathcare;
            return ClampToScore((int)Math.Round(totalGapWeight * 1.0 / residentialCapacity));
        }

        private static string ServiceGapFocusLabel(int park, int health, int education, int transit, int safety, int security, int waste, int communication, int mail, int deathcare)
        {
            var labels = new[]
            {
                "\u516c\u56ed",
                "\u533b\u7597",
                "\u6559\u80b2",
                "\u516c\u4ea4",
                "\u6d88\u9632",
                "\u8b66\u52a1",
                "\u56de\u6536",
                "\u901a\u4fe1",
                "\u90ae\u653f",
                "\u751f\u547d"
            };
            var values = new[] { park, health, education, transit, safety, security, waste, communication, mail, deathcare };
            var first = -1;
            var second = -1;

            for (var i = 0; i < values.Length; i += 1)
            {
                if (first < 0 || values[i] > values[first])
                {
                    second = first;
                    first = i;
                }
                else if (second < 0 || values[i] > values[second])
                {
                    second = i;
                }
            }

            if (first < 0 || values[first] <= 0)
            {
                return "\u5747\u8861";
            }

            if (second >= 0 && values[second] > 0 && values[second] * 100 >= values[first] * 65)
            {
                return labels[first] + "+" + labels[second];
            }

            return labels[first];
        }

        private static int ServiceEquityPenalty(int serviceEquity)
        {
            return serviceEquity >= 50 ? 0 : Math.Min(16, (50 - serviceEquity) / 3);
        }

        private static int ServiceEquityBonus(int serviceEquity)
        {
            return serviceEquity <= 70 ? 0 : Math.Min(6, (serviceEquity - 70) / 5);
        }

        private static int ServiceReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int ServiceUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int RoadMaintenanceWeightForRoad(RoadNode road)
        {
            if (road == null)
            {
                return 0;
            }

            var tierWeight = road.Tier == RoadTier.Arterial ? 2 : 1;
            var deadEndStress = road.NeighborCount <= 1 ? 1 : 0;
            var junctionStress = road.NeighborCount >= 3 ? 1 : 0;
            return Math.Max(1, tierWeight + deadEndStress + junctionStress);
        }

        private static int ComputeAccidentRisk(int congestion, int roadConnectivity, int deadEndRoadTiles, int intersectionRoadTiles, int arterialRoadTiles, int roadMaintenanceCoverage, int maintenanceCondition, int emergencyResponse, int walkability, int roadTiles)
        {
            if (roadTiles <= 0)
            {
                return 0;
            }

            var congestionPressure = congestion * 2 / 5;
            var deadEndPressure = Math.Min(18, deadEndRoadTiles * 2);
            var intersectionPressure = Math.Min(16, Math.Max(0, intersectionRoadTiles - arterialRoadTiles / 2));
            var arterialSpeedPressure = Math.Min(12, arterialRoadTiles * 2);
            var maintenanceShortfall = Math.Max(0, 70 - roadMaintenanceCoverage) / 2 + Math.Max(0, 65 - maintenanceCondition) / 3;
            return ClampToScore(10 + congestionPressure + deadEndPressure + intersectionPressure + arterialSpeedPressure + maintenanceShortfall - roadConnectivity / 10 - emergencyResponse / 8 - walkability / 12);
        }

        private static int AccidentRoadLoad(int roadLoad, int accidentRisk)
        {
            return accidentRisk <= 35 ? 0 : Math.Max(1, roadLoad * (accidentRisk - 35) / 500);
        }

        private static int ComputeRoadSafety(int accidentRisk, int roadMaintenanceCoverage, int roadConnectivity, int emergencyResponse, int walkability)
        {
            return ClampToScore(88 - accidentRisk + roadMaintenanceCoverage / 6 + roadConnectivity / 12 + emergencyResponse / 10 + walkability / 14);
        }

        private static int ComputeMaintenanceCondition(int cash, int serviceBudgetPercent, int serviceUtilization, int utilityUtilization, int congestion, int buildingCount, int roadTiles, int roadMaintenanceCoverage)
        {
            var cashBuffer = Math.Min(10, Math.Max(-25, cash / 1000));
            var budgetSupport = (serviceBudgetPercent - 100) / 2;
            var serviceStress = Math.Max(0, serviceUtilization - 100) / 3;
            var utilityStress = Math.Max(0, utilityUtilization - 105) / 4;
            var cityScaleWear = Math.Min(12, (buildingCount + roadTiles) / 80);
            return ClampToScore(78 + cashBuffer + budgetSupport + roadMaintenanceCoverage / 12 - serviceStress - utilityStress - congestion / 8 - cityScaleWear);
        }

        private static int ApplyMaintenanceCondition(int reliability, int maintenanceCondition)
        {
            if (maintenanceCondition >= 70)
            {
                return reliability;
            }

            return ClampToScore(reliability * (70 + maintenanceCondition) / 140);
        }

        private static int ComputeEmergencyResponse(int healthCoverage, int safetyCoverage, int securityCoverage, int serviceReliability, int roadConnectivity, int congestion, int deadEndRoadTiles, int serviceUtilization, int connectedBuildings, int disconnectedBuildings)
        {
            if (connectedBuildings <= 0)
            {
                return 0;
            }

            var serviceReadiness = (healthCoverage * 3 + safetyCoverage * 4 + securityCoverage * 3) / 10;
            var overloadPenalty = Math.Max(0, serviceUtilization - 100) / 3;
            var deadEndPenalty = Math.Min(18, deadEndRoadTiles * 2);
            return ClampToScore(14 + serviceReadiness / 2 + serviceReliability / 5 + roadConnectivity / 4 - congestion / 4 - overloadPenalty - deadEndPenalty - disconnectedBuildings * 3);
        }

        private static int ComputeFireResponse(int safetyCoverage, int emergencyResponse, int roadConnectivity, int congestion, int fireUtilization)
        {
            var overloadPenalty = Math.Max(0, fireUtilization - 100) / 4;
            return ClampToScore(8 + safetyCoverage / 3 + emergencyResponse / 3 + roadConnectivity / 5 - congestion / 6 - overloadPenalty);
        }

        private static int ComputeMedicalResponse(int healthCoverage, int emergencyResponse, int roadConnectivity, int congestion, int healthUtilization, int serviceReliability)
        {
            var overloadPenalty = Math.Max(0, healthUtilization - 100) / 4;
            return ClampToScore(12 + healthCoverage / 3 + emergencyResponse / 4 + serviceReliability / 8 + roadConnectivity / 6 - congestion / 6 - overloadPenalty);
        }

        private static int ComputeFireRisk(int fireLoad, int fireProtection, int fireResponse, int fireUtilization, int pollution, int congestion, int maintenanceCondition, int population, int industrialJobs)
        {
            if (population < 80 && fireLoad <= 0)
            {
                return 0;
            }

            var exposure = Math.Min(38, fireLoad / 18 + population / 180 + industrialJobs / 32);
            var overload = Math.Max(0, fireUtilization - 100) / 3;
            var maintenanceShortfall = Math.Max(0, 62 - maintenanceCondition) / 4;
            return ClampToScore(10 + exposure + pollution / 2 + congestion / 10 + overload + maintenanceShortfall - fireProtection / 2 - fireResponse / 4);
        }

        private static int ComputePoliceResponse(int securityCoverage, int emergencyResponse, int roadConnectivity, int congestion, int securityUtilization, int serviceReliability)
        {
            var overloadPenalty = Math.Max(0, securityUtilization - 100) / 4;
            return ClampToScore(10 + securityCoverage / 3 + emergencyResponse / 4 + serviceReliability / 7 + roadConnectivity / 5 - congestion / 5 - overloadPenalty);
        }

        private static int ComputeCaseBacklog(int population, int securityLoad, int securityCapacity, int securityCoverage, int securityUtilization, int policeResponse, int unemployment, int rentPressure)
        {
            if (population < 180 && securityLoad <= 0)
            {
                return 0;
            }

            var coverageGap = Math.Max(0, 58 - securityCoverage);
            var overload = Math.Max(0, securityUtilization - 100);
            var responseGap = Math.Max(0, 60 - policeResponse);
            var capacityGap = Math.Max(0, securityLoad - securityCapacity);
            return ClampToScore(6 + population / 180 + unemployment / 4 + rentPressure / 10 + coverageGap / 2 + overload / 3 + responseGap / 3 + capacityGap / 16);
        }

        private static int ComputePatientBacklog(int population, int healthLoad, int healthCapacity, int healthCoverage, int healthUtilization, int medicalResponse, int publicHealth, int healthRisk)
        {
            if (population < 140 && healthLoad <= 0)
            {
                return 0;
            }

            var coverageGap = Math.Max(0, 60 - healthCoverage);
            var overload = Math.Max(0, healthUtilization - 100);
            var responseGap = Math.Max(0, 62 - medicalResponse);
            var capacityGap = Math.Max(0, healthLoad - healthCapacity);
            var publicHealthGap = Math.Max(0, 62 - publicHealth);
            return ClampToScore(5 + population / 180 + coverageGap / 2 + overload / 3 + responseGap / 3 + capacityGap / 18 + publicHealthGap / 4 + healthRisk / 4);
        }

        private static int ComputeDisasterPreparedness(int shelterCapacity, int population, int emergencyResponse, int stormwaterResilience, int utilityReliability, int roadConnectivity, int maintenanceCondition)
        {
            if (population <= 0)
            {
                return 0;
            }

            var shelterCoverage = shelterCapacity <= 0 ? 0 : ClampToScore((int)Math.Round(shelterCapacity * 100.0 / Math.Max(80, population)));
            return ClampToScore(10 + shelterCoverage / 2 + emergencyResponse / 6 + stormwaterResilience / 7 + utilityReliability / 8 + roadConnectivity / 8 + maintenanceCondition / 10);
        }

        private static int ComputeDisasterRisk(int disasterPreparedness, int floodRisk, int healthRisk, int accidentRisk, int fireRisk, int utilityReliability, int wastewaterReliability, int congestion)
        {
            var utilityRisk = Math.Max(0, 95 - utilityReliability) / 2;
            var sanitationRisk = Math.Max(0, 75 - wastewaterReliability) / 3;
            return ClampToScore(18 + floodRisk / 2 + healthRisk / 3 + accidentRisk / 4 + fireRisk / 3 + congestion / 8 + utilityRisk + sanitationRisk - disasterPreparedness / 2);
        }

        private static int ApplyServiceReliability(int coverage, int reliability)
        {
            return ClampToScore(coverage * reliability / 100);
        }

        private static int ComputeWorkforceSkill(int population, int employment, int educationCoverage, int advancedEducationCoverage, int officeJobs, int upgradedBuildings, int landValue, int crimePressure, int pollution, int innovationBase)
        {
            if (population < 80)
            {
                return ClampToScore(20 + educationCoverage / 3 + advancedEducationCoverage / 4 + Math.Min(10, upgradedBuildings * 2));
            }

            var officeDepth = Math.Min(24, officeJobs / 6);
            var upgradeDepth = Math.Min(18, upgradedBuildings * 4);
            var employmentDepth = Math.Min(10, employment / 30);
            var innovationDepth = Math.Min(12, innovationBase / 24);
            return ClampToScore(18 + educationCoverage / 2 + advancedEducationCoverage / 3 + landValue / 9 + officeDepth + upgradeDepth + employmentDepth + innovationDepth - crimePressure / 4 - pollution);
        }

        private static int ComputeLaborShortage(int jobs, int employable, int workforceSkill)
        {
            if (jobs <= employable || jobs <= 0)
            {
                return 0;
            }

            var gap = ClampToScore((int)Math.Round((jobs - employable) * 100.0 / Math.Max(1, jobs)));
            return ClampToScore(gap * 2 - workforceSkill / 4);
        }

        private static int ComputeInnovationCapacity(int innovationBase, int advancedEducationCoverage, int communicationCoverage, int communicationUtilization, int workforceSkill, int utilityReliability, int officeJobs)
        {
            var communicationOverloadPenalty = Math.Max(0, communicationUtilization - 100) / 3;
            return ClampToScore(innovationBase / 12 + advancedEducationCoverage / 4 + communicationCoverage / 4 + workforceSkill / 5 + utilityReliability / 10 + Math.Min(12, officeJobs / 18) - communicationOverloadPenalty);
        }

        private static int ComputeBusinessEfficiency(int communicationCoverage, int communicationUtilization, int mailCoverage, int mailUtilization, int utilityReliability, int workforceSkill, int logisticsCoverage, int commuteEfficiency, int congestion, int innovationCapacity)
        {
            var overloadPenalty = Math.Max(0, communicationUtilization - 100) / 3 + Math.Max(0, mailUtilization - 100) / 4;
            return ClampToScore(20 + communicationCoverage / 3 + mailCoverage / 5 + utilityReliability / 5 + workforceSkill / 5 + innovationCapacity / 5 + logisticsCoverage / 8 + commuteEfficiency / 10 - congestion / 8 - overloadPenalty);
        }

        private static int ComputeProductivityBonus(int employment, int workforceSkill, int advancedEducationCoverage, int logisticsCoverage, int officeJobs, int businessEfficiency, int innovationCapacity)
        {
            if (employment <= 0)
            {
                return 0;
            }

            var qualityPercent = Math.Max(0, workforceSkill - 35) / 2 + Math.Max(0, advancedEducationCoverage - 45) / 5 + Math.Max(0, logisticsCoverage - 25) / 6 + Math.Max(0, businessEfficiency - 50) / 4 + Math.Max(0, innovationCapacity - 45) / 5 + Math.Min(15, officeJobs / 20);
            return Math.Max(0, employment * qualityPercent / 100);
        }

        private static int ComputeJobsHousingBalance(int employable, int jobs)
        {
            if (employable <= 0 && jobs <= 0)
            {
                return 100;
            }

            var baseValue = Math.Max(1, Math.Max(employable, jobs));
            var mismatch = Math.Abs(jobs - employable) * 100 / baseValue;
            return ClampToScore(100 - mismatch);
        }

        private static int ComputeCommuteEfficiency(int transitCoverage, int regionalConnectivity, int congestion, int jobsHousingBalance, int mixedUseBuildings, int arterialRoadTiles, int roadConnectivity, int connectedBuildings, int disconnectedBuildings)
        {
            var mixedUseRelief = Math.Min(18, mixedUseBuildings * 4);
            var arterialRelief = Math.Min(12, arterialRoadTiles * 2);
            var networkBase = connectedBuildings > 0 ? 22 : 0;
            return ClampToScore(networkBase + transitCoverage / 3 + regionalConnectivity / 10 + roadConnectivity / 8 + jobsHousingBalance / 3 + mixedUseRelief + arterialRelief - congestion / 3 - disconnectedBuildings * 4);
        }

        private static int ComputeCarDependency(int commuteEfficiency, int transitCoverage, int regionalConnectivity, int mixedUseBuildings, int congestion, int jobsHousingBalance)
        {
            var mixedUseRelief = Math.Min(18, mixedUseBuildings * 3);
            return ClampToScore(85 - commuteEfficiency / 2 - transitCoverage / 4 - regionalConnectivity / 8 - mixedUseRelief - jobsHousingBalance / 6 + congestion / 3);
        }

        private static int ComputeParkingPressure(int population, int jobs, int commercialJobs, int officeJobs, int attractionParkingDemand, int carDependency, int transitCoverage, int roadConnectivity, int mixedUseBuildings, int roadTiles, int arterialRoadTiles, int landUseEfficiency, int congestion, int parkingCapacity, int parkingCoverage)
        {
            if (population < 60 || roadTiles <= 0)
            {
                return 0;
            }

            var tripDemand = population / 7 + jobs / 9 + commercialJobs / 5 + officeJobs / 7 + attractionParkingDemand;
            var carTrips = tripDemand * Math.Max(10, carDependency) / 100;
            var parkingSupply = roadTiles * 4 + arterialRoadTiles * 5 + mixedUseBuildings * 10 + transitCoverage / 2 + roadConnectivity / 3 + landUseEfficiency / 3 + parkingCapacity + parkingCoverage / 2;
            var shortage = Math.Max(0, carTrips - parkingSupply);
            var spare = Math.Max(0, parkingSupply - carTrips);
            var pressure = 18 + carDependency / 3 + congestion / 6 + shortage * 100 / Math.Max(30, tripDemand) - transitCoverage / 10 - parkingCoverage / 8 - mixedUseBuildings - Math.Min(18, spare / 5);
            return ClampToScore(pressure);
        }

        private static int ParkingSearchRoadLoad(int population, int jobs, int parkingPressure, int carDependency)
        {
            if (parkingPressure <= 45)
            {
                return 0;
            }

            var carTrips = (population / 8 + jobs / 10) * Math.Max(0, carDependency) / 100;
            return Math.Max(1, carTrips * (parkingPressure - 45) / 120);
        }

        private static int ParkingHappinessPenalty(int parkingPressure)
        {
            return parkingPressure <= 50 ? 0 : Math.Min(12, (parkingPressure - 50) / 5);
        }

        private static int ParkingAccessBonus(int parkingPressure)
        {
            return parkingPressure >= 28 ? 0 : Math.Min(6, (28 - parkingPressure) / 4);
        }

        private static int ParkingAccessPenalty(int parkingPressure)
        {
            return parkingPressure <= 50 ? 0 : Math.Min(14, (parkingPressure - 50) / 4);
        }

        private static int ComputeWalkability(int roadConnectivity, int transitCoverage, int serviceCoverage, int parkCoverage, int landUseEfficiency, int mixedUseBuildings, int carDependency, int congestion, int deadEndRoadTiles, int connectedBuildings)
        {
            if (connectedBuildings <= 0)
            {
                return 0;
            }

            var mixedUseBonus = Math.Min(16, mixedUseBuildings * 4);
            var compactBonus = Math.Min(12, Math.Max(0, landUseEfficiency - 45) / 4);
            var deadEndPenalty = Math.Min(18, deadEndRoadTiles * 2);
            return ClampToScore(18 + roadConnectivity / 4 + transitCoverage / 5 + serviceCoverage / 5 + parkCoverage / 8 + mixedUseBonus + compactBonus - carDependency / 5 - congestion / 6 - deadEndPenalty);
        }

        private static int CommuteHappinessPenalty(int commuteEfficiency, int carDependency)
        {
            return Math.Max(0, 48 - commuteEfficiency) / 4 + Math.Max(0, carDependency - 65) / 8;
        }

        private static int ComputeEnvironmentQuality(int pollution, int noise, int parkCoverage, int wasteCoverage, int transitCoverage, int carDependency, int wastewaterReliability, int stormwaterResilience, int floodRisk)
        {
            return ClampToScore(72 + parkCoverage / 5 + wasteCoverage / 6 + transitCoverage / 8 + wastewaterReliability / 10 + stormwaterResilience / 12 - pollution * 3 - noise / 2 - carDependency / 6 - Math.Max(0, 60 - wastewaterReliability) / 3 - Math.Max(0, 60 - stormwaterResilience) / 4 - floodRisk / 6);
        }

        private static int ComputeNoiseStress(int noise, int congestion, int carDependency, int transitCoverage, int parkCoverage)
        {
            return ClampToScore(noise * 3 + congestion / 4 + carDependency / 5 - transitCoverage / 8 - parkCoverage / 10);
        }

        private static int EnvironmentHappinessPenalty(int environmentQuality, int noiseStress)
        {
            return Math.Max(0, 48 - environmentQuality) / 3 + Math.Max(0, noiseStress - 45) / 5;
        }

        private static int ComputeLivingCondition(int population, int serviceCoverage, int serviceEquity, int parkCoverage, int educationCoverage, int deathcareCoverage, int transitCoverage, int transitWaitPressure, int commuteEfficiency, int walkability, int rentPressure, int crimePressure, int environmentQuality, int publicHealth, int healthRisk, int noiseStress, int roadBottleneckPressure, int parkingPressure, int utilityReliability)
        {
            if (population <= 0)
            {
                return 70;
            }

            var earlyCityGrace = population < 160 ? 10 : 0;
            var value = 28 + earlyCityGrace + serviceCoverage / 7 + serviceEquity / 5 + parkCoverage / 8 + educationCoverage / 12 + deathcareCoverage / 16 + transitCoverage / 11 + commuteEfficiency / 8 + walkability / 7 + environmentQuality / 7 + publicHealth / 8 + utilityReliability / 16;
            value -= rentPressure / 5 + crimePressure / 5 + healthRisk / 6 + noiseStress / 7 + roadBottleneckPressure / 8 + transitWaitPressure / 8 + parkingPressure / 10;
            return ClampToScore(value);
        }

        private static int ComputeLivingPressure(int livingCondition, int rentPressure, int crimePressure, int healthRisk, int noiseStress, int roadBottleneckPressure, int transitWaitPressure, int serviceEquity)
        {
            var serviceGap = Math.Max(0, 55 - serviceEquity);
            var externalPressure = rentPressure / 2 + crimePressure / 2 + healthRisk / 2 + noiseStress / 3 + roadBottleneckPressure / 3 + transitWaitPressure / 3 + serviceGap / 2;
            return ClampToScore(Math.Max(0, 100 - livingCondition) * 2 / 3 + externalPressure / 3);
        }

        private static int LivingConditionPenalty(int livingCondition, int livingPressure)
        {
            return Math.Max(0, 50 - livingCondition) / 4 + Math.Max(0, livingPressure - 55) / 5;
        }

        private static int LivingConditionBonus(int livingCondition, int livingPressure)
        {
            return livingCondition < 70 || livingPressure > 35 ? 0 : Math.Min(6, (livingCondition - 70) / 5 + Math.Max(0, 35 - livingPressure) / 12);
        }

        private static int ComputePublicHealth(int healthCoverage, int emergencyResponse, int environmentQuality, int wasteCoverage, float utilityEfficiency, int pollution, int noiseStress, int wastewaterReliability, int stormwaterResilience, int floodRisk)
        {
            var utilityBonus = utilityEfficiency >= 0.98f ? 8 : utilityEfficiency >= 0.9f ? 2 : -12;
            return ClampToScore(30 + healthCoverage / 3 + emergencyResponse / 7 + environmentQuality / 4 + wasteCoverage / 8 + wastewaterReliability / 9 + stormwaterResilience / 14 + utilityBonus - pollution * 2 - noiseStress / 7 - floodRisk / 7);
        }

        private static int ComputeHealthRisk(int publicHealth, int emergencyResponse, int pollution, int noiseStress, float utilityEfficiency, int wastewaterReliability, int wastewaterUtilization, int stormwaterResilience, int floodRisk)
        {
            var utilityRisk = utilityEfficiency >= 0.98f ? 0 : utilityEfficiency >= 0.85f ? 8 : 18;
            return ClampToScore(70 - publicHealth + Math.Max(0, pollution - 8) * 2 + Math.Max(0, noiseStress - 45) / 2 + Math.Max(0, 65 - wastewaterReliability) / 3 + Math.Max(0, wastewaterUtilization - 120) / 6 + Math.Max(0, 65 - stormwaterResilience) / 4 + floodRisk / 3 + utilityRisk + Math.Max(0, 50 - emergencyResponse) / 5);
        }

        private static int ComputeMortalityPressure(int population, int deathcareCoverage, int deathcareUtilization, int publicHealth, int healthRisk, int disasterPreparedness)
        {
            if (population < 180)
            {
                return 0;
            }

            var coverageGap = Math.Max(0, 58 - deathcareCoverage);
            var overload = Math.Max(0, deathcareUtilization - 100);
            var healthGap = Math.Max(0, 62 - publicHealth);
            return ClampToScore(8 + population / 140 + coverageGap / 2 + overload / 3 + healthRisk / 3 + healthGap / 4 - disasterPreparedness / 8);
        }

        private static int HealthHappinessPenalty(int healthRisk)
        {
            return Math.Max(0, healthRisk - 45) / 5;
        }

        private static int DisasterRiskHappinessPenalty(int disasterRisk)
        {
            return Math.Max(0, disasterRisk - 45) / 5;
        }

        private bool IsCoveredByService(PlacedBuilding building, List<PlacedBuilding> services)
        {
            var center = BuildingCenter(building);
            for (var i = 0; i < services.Count; i += 1)
            {
                var definition = config.GetBuilding(services[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                var serviceCenter = BuildingCenter(services[i]);
                var distance = Math.Abs(center.X - serviceCenter.X) + Math.Abs(center.Y - serviceCenter.Y);
                if (distance <= definition.ServiceRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCoveredByTransit(PlacedBuilding building, List<PlacedBuilding> transitBuildings)
        {
            var center = BuildingCenter(building);
            for (var i = 0; i < transitBuildings.Count; i += 1)
            {
                var definition = config.GetBuilding(transitBuildings[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                var serviceCenter = BuildingCenter(transitBuildings[i]);
                var distance = Math.Abs(center.X - serviceCenter.X) + Math.Abs(center.Y - serviceCenter.Y);
                if (distance <= definition.ServiceRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsRoadCoveredByService(GridPos roadPos, List<PlacedBuilding> services)
        {
            for (var i = 0; i < services.Count; i += 1)
            {
                var definition = config.GetBuilding(services[i].ConfigId);
                if (definition == null)
                {
                    continue;
                }

                var serviceCenter = BuildingCenter(services[i]);
                var distance = Math.Abs(roadPos.X - serviceCenter.X) + Math.Abs(roadPos.Y - serviceCenter.Y);
                if (distance <= definition.ServiceRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyBuildingTilePressure(PlacedBuilding placed, BuildingDefinition definition, int traffic, int pollution, int noise)
        {
            var center = BuildingCenter(placed);
            foreach (var tilePos in Grid.PositionsInRect(placed.Pos, placed.Size))
            {
                Grid.AddTilePressure(tilePos, traffic / 4, pollution, noise, definition.ServiceValue);
            }

            var radius = Math.Max(2, Math.Max(pollution, noise) / 2);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var pos = new GridPos(x, y);
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var fade = radius - distance + 1;
                    Grid.AddTilePressure(pos, 0, pollution * fade / Math.Max(1, radius), noise * fade / Math.Max(1, radius), -pollution * fade / Math.Max(1, radius));
                }
            }

            if (definition.ServiceRadius > 0 && definition.ServiceValue > 0)
            {
                var serviceValue = BudgetAdjustedServiceValue(definition.ServiceValue);
                for (var y = center.Y - definition.ServiceRadius; y <= center.Y + definition.ServiceRadius; y += 1)
                {
                    for (var x = center.X - definition.ServiceRadius; x <= center.X + definition.ServiceRadius; x += 1)
                    {
                        var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                        if (distance <= definition.ServiceRadius)
                        {
                            var value = serviceValue * (definition.ServiceRadius - distance + 1) / Math.Max(1, definition.ServiceRadius);
                            Grid.AddTilePressure(new GridPos(x, y), 0, 0, 0, value);
                        }
                    }
                }
            }
        }

        private void ApplyWasteShortfallPressure(PlacedBuilding placed, int wasteWeight)
        {
            var center = BuildingCenter(placed);
            var pollution = WasteShortfallPollution(wasteWeight);
            var noise = Math.Max(0, wasteWeight / 40);
            foreach (var tilePos in Grid.PositionsInRect(placed.Pos, placed.Size))
            {
                Grid.AddTilePressure(tilePos, 0, pollution, noise, -pollution * 2);
            }

            var radius = Math.Max(1, Math.Min(4, wasteWeight / 18));
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var fade = radius - distance + 1;
                    Grid.AddTilePressure(new GridPos(x, y), 0, pollution * fade / (radius + 1), noise * fade / (radius + 1), -pollution * fade / Math.Max(1, radius));
                }
            }
        }

        private void ApplyTransitTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    Grid.AddTransitAccess(new GridPos(x, y), access);
                    Grid.AddTilePressure(new GridPos(x, y), 0, 0, 0, access / 18);
                }
            }
        }

        private void ApplyLogisticsTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddLogisticsAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, access / 38, -access / 55);
                }
            }
        }

        private void ApplyCommunicationTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddCommunicationAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, 0, access / 28);
                }
            }
        }

        private void ApplyMailTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddMailAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, access / 45, access / 34);
                }
            }
        }

        private void ApplyRoadMaintenanceTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddRoadMaintenanceAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, 0, access / 45);
                }
            }
        }

        private void ApplyParkingTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddParkingAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, access / 42, access / 70);
                }
            }
        }

        private void ApplyStormwaterTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddStormwaterAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, 0, access / 55);
                }
            }
        }

        private void ApplyWasteTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddWasteAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, 0, access / 32);
                }
            }
        }

        private void ApplyParkTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            ApplyServiceTileAccess(placed, definition, ServiceAccessKind.Park);
        }

        private void ApplyHealthTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            ApplyServiceTileAccess(placed, definition, ServiceAccessKind.Health);
        }

        private void ApplyDeathcareTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    Grid.AddDeathcareAccess(pos, access);
                    Grid.AddTilePressure(pos, 0, 0, access / 60, access / 42);
                }
            }
        }

        private void ApplyEducationTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            ApplyServiceTileAccess(placed, definition, ServiceAccessKind.Education);
        }

        private void ApplySafetyTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            ApplyServiceTileAccess(placed, definition, ServiceAccessKind.Safety);
        }

        private void ApplyFireProtectionTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    Grid.AddFireProtectionAccess(new GridPos(x, y), access);
                }
            }
        }

        private void ApplySecurityTileAccess(PlacedBuilding placed, BuildingDefinition definition)
        {
            ApplyServiceTileAccess(placed, definition, ServiceAccessKind.Security);
        }

        private void ApplyServiceTileAccess(PlacedBuilding placed, BuildingDefinition definition, ServiceAccessKind kind)
        {
            var center = BuildingCenter(placed);
            var radius = Math.Max(1, definition.ServiceRadius);
            for (var y = center.Y - radius; y <= center.Y + radius; y += 1)
            {
                for (var x = center.X - radius; x <= center.X + radius; x += 1)
                {
                    var distance = Math.Abs(center.X - x) + Math.Abs(center.Y - y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var access = BudgetAdjustedAccess(100 * (radius - distance + 1) / (radius + 1));
                    var pos = new GridPos(x, y);
                    if (kind == ServiceAccessKind.Park)
                    {
                        Grid.AddParkAccess(pos, access);
                    }
                    else if (kind == ServiceAccessKind.Health)
                    {
                        Grid.AddHealthAccess(pos, access);
                    }
                    else if (kind == ServiceAccessKind.Education)
                    {
                        Grid.AddEducationAccess(pos, access);
                    }
                    else if (kind == ServiceAccessKind.Safety)
                    {
                        Grid.AddSafetyAccess(pos, access);
                    }
                    else
                    {
                        Grid.AddSecurityAccess(pos, access);
                    }
                }
            }
        }

        private int AverageLandValue()
        {
            var total = 0;
            var count = 0;
            foreach (var pos in Grid.AllPositions())
            {
                var tile = Grid.GetTile(pos);
                if (tile.Terrain == TerrainType.Water)
                {
                    continue;
                }

                total += tile.LandValue;
                count += 1;
            }

            return count == 0 ? 0 : (int)Math.Round(total * 1.0 / count);
        }

        private int ComputeHappiness(int serviceCoverage, int parkCoverage, int healthCoverage, int educationCoverage, int safetyCoverage, int transitCoverage, int wasteCoverage, int safetyRisk, float utilityEfficiency, int congestion, int pollution, int unemployment, int landValue)
        {
            var value = config.HappinessTarget + parkCoverage / 8 + healthCoverage / 6 + educationCoverage / 14 + safetyCoverage / 16 + transitCoverage / 12 + wasteCoverage / 16 + landValue / 8;
            if (parkCoverage < 45)
            {
                value -= config.LowServiceHappinessPenalty / 2;
            }

            if (healthCoverage < 35 && Metrics.Population > 120)
            {
                value -= config.LowServiceHappinessPenalty / 2;
            }

            if (safetyCoverage < 35 && Metrics.Population > 200)
            {
                value -= config.LowServiceHappinessPenalty / 2;
            }

            if (wasteCoverage < 35 && Metrics.Population > 220)
            {
                value -= config.LowServiceHappinessPenalty / 2;
            }

            if (utilityEfficiency < 0.98f)
            {
                value -= config.UtilityShortageHappinessPenalty;
            }

            value -= congestion * config.CongestionHappinessPenalty / 100;
            value -= pollution * 2;
            value -= safetyRisk / 4;
            value -= unemployment / 2;
            value -= Metrics.DisconnectedBuildings * 4;
            return ClampToScore(value);
        }

        private int ComputeRentPressure(int housingCapacity, int landValue, int serviceCoverage, int transitCoverage)
        {
            if (Metrics.Population < 80 || housingCapacity <= 0)
            {
                return 0;
            }

            var occupiedPercent = ClampToScore((int)Math.Round(Metrics.Population * 100.0 / Math.Max(1, housingCapacity)));
            var scarcityPressure = Math.Max(0, occupiedPercent - 68);
            var landPressure = Math.Max(0, landValue - 42) * 2 / 3;
            var serviceRelief = serviceCoverage / 8;
            var transitRelief = transitCoverage / 10;
            var spareHousingRelief = Math.Min(16, Math.Max(0, housingCapacity - Metrics.Population) / 12);
            return ClampToScore(20 + scarcityPressure + landPressure + TaxRentPressure() - PolicyRentPressureRelief() - serviceRelief - transitRelief - spareHousingRelief);
        }

        private int TaxRentPressure()
        {
            if (taxLevel == CityTaxLevel.Low)
            {
                return -7;
            }

            if (taxLevel == CityTaxLevel.High)
            {
                return 15;
            }

            return 0;
        }

        private int PolicyRentPressureRelief()
        {
            return IsPolicyActive(CityPolicy.AffordableHousing) ? 14 : 0;
        }

        private int ComputeCrimePressure(int securityCoverage, int unemployment, int rentPressure, int congestion, int securityEligible, int policeResponse, int securityUtilization, int caseBacklog)
        {
            if (Metrics.Population < 180 || securityEligible <= 0)
            {
                return 0;
            }

            var securityShortfall = Math.Max(0, 62 - securityCoverage);
            var responseShortfall = Math.Max(0, 58 - policeResponse);
            var overload = Math.Max(0, securityUtilization - 100);
            var value = 12 + Metrics.Population / 130 + unemployment / 2 + rentPressure / 6 + congestion / 8 + securityShortfall * 2 / 3 + responseShortfall / 4 + overload / 4 + caseBacklog / 3;
            return ClampToScore(value);
        }

        private static int CrimeHappinessPenalty(int crimePressure)
        {
            return crimePressure <= 50 ? 0 : (crimePressure - 50) / 3;
        }

        private static int RentHappinessPenalty(int rentPressure)
        {
            return rentPressure <= 62 ? 0 : (rentPressure - 62) / 3;
        }

        private static int RentGrowthPenalty(int rentPressure)
        {
            return rentPressure <= 72 ? 0 : (rentPressure - 72) / 3;
        }

        private void RefreshActivePolicyMetrics()
        {
            Metrics.ActivePolicies.Clear();
            for (var i = 0; i < activePolicies.Count; i += 1)
            {
                Metrics.ActivePolicies.Add(activePolicies[i]);
            }
        }

        private int RoadCapacityPerTile()
        {
            return PolicyAdjustedRoadCapacity(config.RoadCapacity);
        }

        private int RoadCapacityForTier(RoadTier tier)
        {
            var capacity = tier == RoadTier.Arterial
                ? config.RoadCapacity * 2
                : config.RoadCapacity;

            return PolicyAdjustedRoadCapacity(capacity);
        }

        private int RoadUpkeepPerTile()
        {
            return config.RoadUpkeepPerTile + PolicyRoadUpkeepSurcharge();
        }

        private int RoadUpkeepForTier(RoadTier tier)
        {
            var upkeep = tier == RoadTier.Arterial
                ? config.RoadUpkeepPerTile * 3
                : config.RoadUpkeepPerTile;

            return upkeep + PolicyRoadUpkeepSurcharge();
        }

        private int TotalRoadUpkeep()
        {
            var upkeep = 0;
            for (var i = 0; i < roads.Count; i += 1)
            {
                upkeep += RoadUpkeepForTier(roads[i].Tier);
            }

            return upkeep;
        }

        private int ArterialRoadUpgradeCost()
        {
            return config.RoadCostPerTile * 3;
        }

        private int EffectivePollution(int value)
        {
            return IsPolicyActive(CityPolicy.GreenCode) ? value * 65 / 100 : value;
        }

        private int EffectiveNoise(int value)
        {
            var reduced = IsPolicyActive(CityPolicy.GreenCode) ? value * 80 / 100 : value;
            if (IsPolicyActive(CityPolicy.TransitPriority))
            {
                reduced = reduced * 90 / 100;
            }

            return IsPolicyActive(CityPolicy.CompleteStreets) ? reduced * 90 / 100 : reduced;
        }

        private int PolicyMonthlyExpense(int administrationEfficiency, int policyBacklog)
        {
            var expense = 0;
            if (IsPolicyActive(CityPolicy.GreenCode))
            {
                expense += 20 + buildings.Count * 3;
            }

            if (IsPolicyActive(CityPolicy.TransitPriority))
            {
                expense += 15 + roads.Count;
            }

            if (IsPolicyActive(CityPolicy.GrowthGrants))
            {
                expense += 40 + Metrics.Population / 4;
            }

            if (IsPolicyActive(CityPolicy.AffordableHousing))
            {
                expense += 35 + Metrics.Population / 5 + Metrics.HighDensityResidentialBuildings * 6;
            }

            if (IsPolicyActive(CityPolicy.TrafficSafetyCampaign))
            {
                expense += 18 + roads.Count / 2 + Metrics.Population / 80;
            }

            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                expense += 12 + roads.Count * 2 / 3 + Metrics.Population / 120;
            }

            if (IsPolicyActive(CityPolicy.SignalOptimization))
            {
                expense += 10 + Metrics.IntersectionRoadTiles * 3 + Metrics.Population / 150;
            }

            if (IsPolicyActive(CityPolicy.CongestionPricing))
            {
                expense -= PolicyCongestionChargeRevenue();
            }

            if (IsPolicyActive(CityPolicy.ParkingFees))
            {
                expense -= PolicyParkingFeeRevenue();
            }

            var adjustedExpense = AdministrationAdjustedPolicyExpense(expense, administrationEfficiency);
            return activePolicies.Count == 0 ? adjustedExpense : adjustedExpense + policyBacklog / 2;
        }

        private int ServiceBudgetPercent()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean)
            {
                return 80;
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted)
            {
                return 125;
            }

            return 100;
        }

        private int BudgetAdjustedBuildingUpkeep(BuildingDefinition definition, int baseUpkeep)
        {
            if (!IsMunicipalServiceBudgetBuilding(definition))
            {
                return baseUpkeep;
            }

            return Math.Max(0, baseUpkeep * ServiceBudgetPercent() / 100);
        }

        private int BudgetAdjustedMunicipalOutput(BuildingDefinition definition, int value)
        {
            if (!IsMunicipalServiceBudgetBuilding(definition))
            {
                return value;
            }

            return BudgetAdjustedServiceValue(value);
        }

        private int BudgetAdjustedServiceValue(int value)
        {
            return Math.Max(0, value * ServiceBudgetPercent() / 100);
        }

        private int BudgetAdjustedCoverage(int value)
        {
            return ClampToScore(BudgetAdjustedServiceValue(value));
        }

        private int BudgetAdjustedAccess(int value)
        {
            return ClampToScore(BudgetAdjustedServiceValue(value));
        }

        private int ServiceBudgetHappinessModifier()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean)
            {
                return Metrics.ServiceCoverage < 55 ? -4 : -1;
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted)
            {
                return Metrics.ServiceCoverage >= 55 ? 3 : 1;
            }

            return 0;
        }

        private int ServiceBudgetServiceDemandModifier()
        {
            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean)
            {
                return 12;
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted)
            {
                return -8;
            }

            return 0;
        }

        private int PolicyHappinessBonus()
        {
            var bonus = 0;
            if (IsPolicyActive(CityPolicy.GreenCode))
            {
                bonus += 3;
            }

            if (IsPolicyActive(CityPolicy.TransitPriority) && Metrics.Congestion < 70)
            {
                bonus += 2;
            }

            if (IsPolicyActive(CityPolicy.AffordableHousing))
            {
                bonus += Metrics.RentPressure > 55 ? 3 : 1;
            }

            if (IsPolicyActive(CityPolicy.TrafficSafetyCampaign) && Metrics.RoadSafety >= 55)
            {
                bonus += 1;
            }

            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                bonus += Metrics.Walkability >= 50 ? 2 : 1;
            }

            if (IsPolicyActive(CityPolicy.SignalOptimization) && Metrics.Congestion < 65)
            {
                bonus += 1;
            }

            if (IsPolicyActive(CityPolicy.CongestionPricing))
            {
                bonus -= Metrics.CarDependency > 60 && Metrics.TransitCoverage < 35 ? 2 : 1;
                if (Metrics.Congestion <= 55 && Metrics.TransitCoverage >= 35)
                {
                    bonus += 1;
                }
            }

            if (IsPolicyActive(CityPolicy.ParkingFees))
            {
                bonus -= Metrics.ParkingPressure > 58 && Metrics.TransitCoverage < 35 ? 2 : 1;
                if (Metrics.ParkingPressure <= 45 && Metrics.TransitCoverage >= 35)
                {
                    bonus += 1;
                }
            }

            return bonus;
        }

        private int PolicyDemandBoost(ZoneType zone)
        {
            var boost = 0;
            if (IsPolicyActive(CityPolicy.GrowthGrants))
            {
                if (zone == ZoneType.Residential) boost += 14;
                if (zone == ZoneType.Commercial) boost += 8;
                if (zone == ZoneType.Industrial) boost += 6;
                if (zone == ZoneType.Office) boost += 5;
                if (zone == ZoneType.MixedUse) boost += 7;
            }

            if (IsPolicyActive(CityPolicy.GreenCode))
            {
                if (zone == ZoneType.Industrial) boost -= 5;
                if (zone == ZoneType.Office) boost += 3;
                if (zone == ZoneType.MixedUse) boost += 2;
                if (zone == ZoneType.Civic) boost += 5;
            }

            if (IsPolicyActive(CityPolicy.TransitPriority))
            {
                if (zone == ZoneType.Commercial) boost += 4;
                if (zone == ZoneType.Office) boost += 4;
                if (zone == ZoneType.MixedUse) boost += 5;
                if (zone == ZoneType.Industrial) boost += 3;
            }

            if (IsPolicyActive(CityPolicy.AffordableHousing))
            {
                if (zone == ZoneType.Residential) boost += 8;
                if (zone == ZoneType.MixedUse) boost += 4;
                if (zone == ZoneType.Civic) boost += 2;
            }

            if (IsPolicyActive(CityPolicy.TrafficSafetyCampaign))
            {
                if (zone == ZoneType.Residential) boost += 3;
                if (zone == ZoneType.Commercial) boost += 2;
                if (zone == ZoneType.Office) boost += 2;
                if (zone == ZoneType.MixedUse) boost += 2;
                if (zone == ZoneType.Civic) boost += 3;
            }

            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                if (zone == ZoneType.Residential) boost += 3;
                if (zone == ZoneType.Commercial) boost += 4;
                if (zone == ZoneType.Office) boost += 3;
                if (zone == ZoneType.MixedUse) boost += 6;
                if (zone == ZoneType.Civic) boost += 2;
                if (zone == ZoneType.Industrial) boost -= 2;
            }

            if (IsPolicyActive(CityPolicy.SignalOptimization))
            {
                if (zone == ZoneType.Commercial) boost += 2;
                if (zone == ZoneType.Office) boost += 3;
                if (zone == ZoneType.MixedUse) boost += 2;
                if (zone == ZoneType.Industrial) boost += 1;
            }

            if (IsPolicyActive(CityPolicy.CongestionPricing))
            {
                if (zone == ZoneType.Residential) boost += 1;
                if (zone == ZoneType.Commercial) boost -= 2;
                if (zone == ZoneType.Office) boost += 2;
                if (zone == ZoneType.MixedUse) boost += 3;
                if (zone == ZoneType.Industrial) boost -= 1;
            }

            if (IsPolicyActive(CityPolicy.ParkingFees))
            {
                var transitReady = Metrics.TransitCoverage >= 35 || Metrics.Walkability >= 55;
                var parkingManaged = Metrics.ParkingPressure <= 50 || Metrics.ParkingCoverage >= 40;
                if (zone == ZoneType.Commercial) boost += parkingManaged ? 1 : -2;
                if (zone == ZoneType.Office) boost += transitReady ? 2 : 0;
                if (zone == ZoneType.MixedUse) boost += transitReady && parkingManaged ? 2 : -1;
                if (zone == ZoneType.Industrial) boost -= Metrics.ParkingPressure > 65 ? 1 : 0;
            }

            return boost;
        }

        private int PolicyAccidentRiskRelief(int roadMaintenanceCoverage, int emergencyResponse, int intersectionRoadTiles, int roadConnectivity)
        {
            var relief = 0;
            if (IsPolicyActive(CityPolicy.TrafficSafetyCampaign))
            {
                relief += 8 + roadMaintenanceCoverage / 12 + emergencyResponse / 15;
            }

            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                relief += 5 + roadMaintenanceCoverage / 18;
            }

            relief += PolicySignalAccidentRelief(intersectionRoadTiles, roadConnectivity);

            return relief;
        }

        private int PolicyRoadSafetyBonus()
        {
            var bonus = IsPolicyActive(CityPolicy.TrafficSafetyCampaign) ? 6 : 0;
            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                bonus += 4;
            }

            bonus += PolicySignalRoadSafetyBonus();

            return bonus;
        }

        private int PolicyAdjustedCongestion(int congestion, int intersectionRoadTiles, int roadConnectivity, int roadTiles)
        {
            return ClampToScore(congestion - PolicySignalCongestionRelief(congestion, intersectionRoadTiles, roadConnectivity, roadTiles) - PolicyCongestionPricingRelief(congestion, roadConnectivity, roadTiles));
        }

        private int PolicyAdjustedIntersectionDelay(int intersectionDelay, int intersectionRoadTiles, int roadConnectivity, int roadTiles)
        {
            if (intersectionDelay <= 0)
            {
                return 0;
            }

            var relief = 0;
            if (IsPolicyActive(CityPolicy.SignalOptimization) && roadTiles >= 10)
            {
                relief += Math.Min(18, 5 + intersectionRoadTiles * 2 + roadConnectivity / 20);
            }

            if (IsPolicyActive(CityPolicy.CongestionPricing) && Metrics.Population >= 120)
            {
                relief += Math.Min(8, 2 + roadConnectivity / 35);
            }

            return ClampToScore(intersectionDelay - relief);
        }

        private int PolicySignalCongestionRelief(int congestion, int intersectionRoadTiles, int roadConnectivity, int roadTiles)
        {
            if (!IsPolicyActive(CityPolicy.SignalOptimization) || congestion <= 0 || roadTiles < 10)
            {
                return 0;
            }

            var intersectionRelief = Math.Min(12, intersectionRoadTiles * 2);
            var connectivityRelief = roadConnectivity >= 55 ? 5 : roadConnectivity >= 35 ? 3 : 1;
            return Math.Min(18, 3 + intersectionRelief + connectivityRelief);
        }

        private int PolicySignalAccidentRelief(int intersectionRoadTiles, int roadConnectivity)
        {
            if (!IsPolicyActive(CityPolicy.SignalOptimization))
            {
                return 0;
            }

            return Math.Min(10, 2 + intersectionRoadTiles + roadConnectivity / 25);
        }

        private int PolicySignalRoadSafetyBonus()
        {
            return IsPolicyActive(CityPolicy.SignalOptimization) ? 3 : 0;
        }

        private int PolicyCongestionPricingRelief(int congestion, int roadConnectivity, int roadTiles)
        {
            if (!IsPolicyActive(CityPolicy.CongestionPricing) || congestion <= 0 || roadTiles < 8 || Metrics.Population < 120)
            {
                return 0;
            }

            var networkRelief = roadConnectivity >= 55 ? 4 : roadConnectivity >= 35 ? 3 : 1;
            return Math.Min(15, 3 + networkRelief + congestion / 10 + Metrics.Population / 250);
        }

        private int PolicyCongestionPricingCarRelief(int transitCoverage, int roadConnectivity)
        {
            if (!IsPolicyActive(CityPolicy.CongestionPricing) || Metrics.Population < 120)
            {
                return 0;
            }

            return Math.Min(10, 3 + transitCoverage / 22 + roadConnectivity / 35);
        }

        private int PolicyCongestionPricingParkingRelief(int roadConnectivity, int transitCoverage)
        {
            if (!IsPolicyActive(CityPolicy.CongestionPricing) || Metrics.Population < 120)
            {
                return 0;
            }

            return Math.Min(12, 4 + roadConnectivity / 30 + transitCoverage / 24);
        }

        private int PolicyCongestionChargeRevenue()
        {
            if (!IsPolicyActive(CityPolicy.CongestionPricing) || Metrics.Population < 120 || roads.Count < 8)
            {
                return 0;
            }

            return Math.Min(120, 10 + Metrics.Population / 35 + Metrics.Congestion / 2 + Metrics.CarDependency / 3);
        }

        private int PolicyParkingFeeRevenue()
        {
            if (!IsPolicyActive(CityPolicy.ParkingFees) || Metrics.Population < 140 || roads.Count < 8)
            {
                return 0;
            }

            return Math.Min(90, 6 + Metrics.Population / 55 + Metrics.ParkingCoverage / 3 + Metrics.ParkingPressure / 5 + Metrics.Visitors / 28);
        }

        private int PolicyParkingFeeCarRelief(int transitCoverage, int mixedUseBuildings, int roadConnectivity)
        {
            if (!IsPolicyActive(CityPolicy.ParkingFees) || Metrics.Population < 140)
            {
                return 0;
            }

            if (transitCoverage < 25 && roadConnectivity < 45)
            {
                return 0;
            }

            return Math.Min(6, 1 + transitCoverage / 28 + roadConnectivity / 40 + Math.Min(2, mixedUseBuildings / 2));
        }

        private int PolicyParkingFeePressureRelief(int parkingCoverage, int transitCoverage)
        {
            if (!IsPolicyActive(CityPolicy.ParkingFees) || Metrics.Population < 140)
            {
                return 0;
            }

            if (parkingCoverage < 20 && transitCoverage < 25)
            {
                return 0;
            }

            return Math.Min(9, 2 + parkingCoverage / 25 + transitCoverage / 30);
        }

        private int PolicyAdjustedRoadCapacity(int capacity)
        {
            var adjusted = capacity;
            if (IsPolicyActive(CityPolicy.TransitPriority))
            {
                adjusted = Math.Max(1, adjusted * 125 / 100);
            }

            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                adjusted = Math.Max(1, adjusted * 92 / 100);
            }

            return adjusted;
        }

        private int PolicyRoadUpkeepSurcharge()
        {
            var surcharge = IsPolicyActive(CityPolicy.TransitPriority) ? 1 : 0;
            return surcharge + (IsPolicyActive(CityPolicy.CompleteStreets) ? 1 : 0);
        }

        private int PolicyAdjustedCarDependency(int carDependency, int transitCoverage, int mixedUseBuildings, int roadConnectivity)
        {
            var adjusted = carDependency;
            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                adjusted -= Math.Min(12, 4 + transitCoverage / 25 + roadConnectivity / 30 + Math.Min(3, mixedUseBuildings));
            }

            adjusted -= PolicyCongestionPricingCarRelief(transitCoverage, roadConnectivity);
            adjusted -= PolicyParkingFeeCarRelief(transitCoverage, mixedUseBuildings, roadConnectivity);
            return ClampToScore(adjusted);
        }

        private int PolicyAdjustedParkingPressure(int parkingPressure, int roadConnectivity, int transitCoverage, int parkingCoverage)
        {
            var adjusted = parkingPressure;
            if (IsPolicyActive(CityPolicy.CompleteStreets))
            {
                adjusted -= Math.Min(10, 4 + roadConnectivity / 24 + transitCoverage / 30);
            }

            adjusted -= PolicyCongestionPricingParkingRelief(roadConnectivity, transitCoverage);
            adjusted -= PolicyParkingFeePressureRelief(parkingCoverage, transitCoverage);
            return ClampToScore(adjusted);
        }

        private int PolicyWalkabilityBonus(int roadConnectivity, int transitCoverage, int mixedUseBuildings)
        {
            if (!IsPolicyActive(CityPolicy.CompleteStreets))
            {
                return 0;
            }

            return Math.Min(14, 5 + roadConnectivity / 15 + transitCoverage / 25 + Math.Min(4, mixedUseBuildings));
        }

        private int TaxRatePercent()
        {
            if (taxLevel == CityTaxLevel.Low)
            {
                return 85;
            }

            if (taxLevel == CityTaxLevel.High)
            {
                return 125;
            }

            return 100;
        }

        private int TaxHappinessModifier()
        {
            if (taxLevel == CityTaxLevel.Low)
            {
                return 3;
            }

            if (taxLevel == CityTaxLevel.High)
            {
                return -7;
            }

            return 0;
        }

        private static int EducationTaxBonus(int employment, int educationCoverage)
        {
            return employment * educationCoverage / 100;
        }

        private static int BusinessEfficiencyTaxBonus(int employment, int businessEfficiency)
        {
            return employment <= 0 || businessEfficiency <= 55 ? 0 : employment * Math.Min(18, businessEfficiency - 55) / 100;
        }

        private static int InnovationTaxBonus(int employment, int innovationCapacity)
        {
            return employment <= 0 || innovationCapacity <= 50 ? 0 : employment * Math.Min(16, innovationCapacity - 50) / 100;
        }

        private static int IndustrialSpecializationTaxBonus(int industrialJobs, int industrialSpecialization)
        {
            return industrialJobs <= 0 || industrialSpecialization <= 50 ? 0 : industrialJobs * Math.Min(18, industrialSpecialization - 50) / 100;
        }

        private static int AdministrationBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(0, definition.ServiceValue * 5 + definition.Jobs * 2 + definition.ServiceRadius * 3);
        }

        private static int AdministrationLoad(int population, int activePolicyCount)
        {
            if (population < 120 && activePolicyCount <= 0)
            {
                return 0;
            }

            return Math.Max(30, population / 3 + activePolicyCount * 24);
        }

        private static int AdministrationUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int ComputeAdministrationEfficiency(int administrationCapacity, int population, int activePolicyCount)
        {
            if (population < 120 && administrationCapacity <= 0)
            {
                return 55;
            }

            var load = AdministrationLoad(population, activePolicyCount);
            if (load <= 0)
            {
                return administrationCapacity > 0 ? 75 : 55;
            }

            if (administrationCapacity <= 0)
            {
                return population >= 300 ? 28 : 42;
            }

            var coverage = ClampToScore((int)Math.Round(administrationCapacity * 100.0 / load));
            return ClampToScore(35 + coverage * 65 / 100);
        }

        private static int ComputePolicyBacklog(int population, int activePolicyCount, int administrationEfficiency, int administrationUtilization)
        {
            if (activePolicyCount <= 0 && administrationUtilization <= 100)
            {
                return 0;
            }

            var policyPressure = activePolicyCount * 8;
            var overload = Math.Max(0, administrationUtilization - 100);
            var efficiencyGap = Math.Max(0, 60 - administrationEfficiency);
            return ClampToScore(policyPressure + overload / 2 + efficiencyGap / 2 + population / 500);
        }

        private static int AdministrationAdjustedPolicyExpense(int expense, int administrationEfficiency)
        {
            if (expense <= 0)
            {
                return expense;
            }

            var reliefPercent = administrationEfficiency <= 55 ? 0 : Math.Min(18, (administrationEfficiency - 55) / 2);
            return Math.Max(0, expense * (100 - reliefPercent) / 100);
        }

        private static int AdministrationTaxBonus(int employment, int buildingTax, int administrationEfficiency)
        {
            if (administrationEfficiency <= 55)
            {
                return 0;
            }

            var bonusPercent = Math.Min(8, (administrationEfficiency - 55) / 4);
            return Math.Max(0, (employment + buildingTax) * bonusPercent / 100);
        }

        private static int AdministrationFiscalBonus(int administrationEfficiency)
        {
            return administrationEfficiency <= 50 ? 0 : Math.Min(12, (administrationEfficiency - 50) / 4);
        }

        private static int AdministrationServiceDemandRelief(int administrationEfficiency)
        {
            return administrationEfficiency <= 55 ? 0 : Math.Min(8, (administrationEfficiency - 55) / 5);
        }

        private int TaxDemandModifier()
        {
            if (taxLevel == CityTaxLevel.Low)
            {
                return 7;
            }

            if (taxLevel == CityTaxLevel.High)
            {
                return -10;
            }

            return 0;
        }

        private static int BuildingLevel(PlacedBuilding building)
        {
            return building == null ? 1 : Math.Max(1, Math.Min(3, building.Level));
        }

        private static int LevelScaledOutput(int value, int level)
        {
            return value <= 0 ? 0 : value * (100 + (Math.Max(1, level) - 1) * 24) / 100;
        }

        private static int LevelScaledTax(int value, int level)
        {
            return value <= 0 ? 0 : value * (100 + (Math.Max(1, level) - 1) * 30) / 100;
        }

        private static int LevelScaledUtilityUse(int value, int level)
        {
            return value <= 0 ? 0 : value * (100 + (Math.Max(1, level) - 1) * 12) / 100;
        }

        private static int LevelScaledUpkeep(int value, int level)
        {
            return value <= 0 ? 0 : value * (100 + (Math.Max(1, level) - 1) * 18) / 100;
        }

        private static int RequiredAgeForNextLevel(int level)
        {
            return level <= 1 ? 8 : 20;
        }

        private static int RequiredScoreForNextLevel(int level)
        {
            return level <= 1 ? 78 : 90;
        }

        private static bool IsUpgradeableBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   (definition.Category == BuildingCategory.Residential ||
                    definition.Category == BuildingCategory.Commercial ||
                    definition.Category == BuildingCategory.Industrial);
        }

        private static bool IsTransitBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "transit" || IsRegionalConnectionBuilding(definition));
        }

        private static bool IsRegionalConnectionBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "intercity";
        }

        private static bool IsLogisticsBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "logistics" || IsWarehouseBuilding(definition) || IsFreightRailBuilding(definition));
        }

        private static bool IsResourceBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "resource";
        }

        private static bool IsWarehouseBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "warehouse";
        }

        private static bool IsFreightRailBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "freight_rail";
        }

        private static bool IsCommunicationBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "communications";
        }

        private static bool IsMailBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "mail";
        }

        private static bool IsDeathcareBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "deathcare";
        }

        private static bool IsRoadMaintenanceBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "road_maintenance";
        }

        private static bool IsParkingBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "parking";
        }

        private static bool IsStormwaterBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "stormwater";
        }

        private static bool IsLogisticsSensitiveBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   (definition.Category == BuildingCategory.Commercial ||
                    definition.Category == BuildingCategory.Industrial);
        }

        private static bool IsCommunicationSensitiveBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   (definition.Category == BuildingCategory.Residential ||
                    definition.Category == BuildingCategory.Commercial ||
                    definition.Category == BuildingCategory.Industrial);
        }

        private static bool IsMailSensitiveBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   !IsMailBuilding(definition) &&
                   (definition.Category == BuildingCategory.Residential ||
                    definition.Category == BuildingCategory.Commercial ||
                    IsOfficeBuilding(definition) ||
                    definition.Category == BuildingCategory.Industrial ||
                    IsMixedUseBuilding(definition) ||
                    IsAttractionBuilding(definition));
        }

        private static bool IsDeathcareSensitiveBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   !IsDeathcareBuilding(definition) &&
                   (definition.Category == BuildingCategory.Residential ||
                    definition.Category == BuildingCategory.Commercial ||
                    definition.Category == BuildingCategory.Industrial ||
                    IsOfficeBuilding(definition) ||
                    IsMixedUseBuilding(definition) ||
                    IsAttractionBuilding(definition) ||
                    IsHealthBuilding(definition) ||
                    IsShelterBuilding(definition));
        }

        private static bool IsParkingSensitiveBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   !IsParkingBuilding(definition) &&
                   (definition.Category == BuildingCategory.Residential ||
                    definition.Category == BuildingCategory.Commercial ||
                    definition.Category == BuildingCategory.Industrial ||
                    IsMixedUseBuilding(definition));
        }

        private static bool IsOfficeBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "office" || IsInnovationBuilding(definition));
        }

        private static bool IsInnovationBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "innovation";
        }

        private static bool IsMixedUseBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "mixed_use";
        }

        private static bool IsResidentialSensitiveBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   (definition.Category == BuildingCategory.Residential || IsMixedUseBuilding(definition));
        }

        private static bool IsGrowthZoneBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   (definition.PreferredZone == ZoneType.Residential ||
                    definition.PreferredZone == ZoneType.Commercial ||
                    definition.PreferredZone == ZoneType.Industrial ||
                    definition.PreferredZone == ZoneType.Office ||
                    definition.PreferredZone == ZoneType.MixedUse);
        }

        private static bool IsParkBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "park" || definition.ModelKey == "plaza");
        }

        private static bool IsAttractionBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "plaza" || definition.ModelKey == "landmark");
        }

        private static bool IsHealthBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "clinic";
        }

        private static bool IsShelterBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "shelter";
        }

        private static bool IsEducationBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "school" || IsAdvancedEducationBuilding(definition));
        }

        private static bool IsAdvancedEducationBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "advanced_education";
        }

        private static bool IsSafetyBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "safety";
        }

        private static bool IsSecurityBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "security";
        }

        private static bool IsWasteBuilding(BuildingDefinition definition)
        {
            return definition != null && (definition.ModelKey == "recycling" || definition.ModelKey == "waste_to_energy");
        }

        private static bool IsWastewaterBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "sewage";
        }

        private static bool IsAdministrationBuilding(BuildingDefinition definition)
        {
            return definition != null && definition.ModelKey == "administration";
        }

        private static bool IsMunicipalServiceBudgetBuilding(BuildingDefinition definition)
        {
            return definition != null &&
                   (definition.Category == BuildingCategory.Service ||
                    definition.Category == BuildingCategory.Utility);
        }

        private static int SafetyWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (definition == null)
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residentRisk = capacity / 6;
            var workplaceRisk = jobs / 8;
            var hazardRisk = definition.Pollution * 4 + definition.Noise * 2 + definition.PowerUse / 2;
            return Math.Max(1, footprint + residentRisk + workplaceRisk + hazardRisk);
        }

        private static int SafetyRiskPenalty(int safetyWeight)
        {
            return Math.Max(1, safetyWeight / 14);
        }

        private static int FireRiskForBuilding(BuildingDefinition definition, int capacity, int jobs, int level)
        {
            if (definition == null || IsSafetyBuilding(definition))
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var peopleExposure = capacity / 8 + jobs / 10;
            var utilityExposure = definition.PowerUse * 2 + definition.PowerOutput / 12;
            var hazardExposure = definition.Pollution * 3 + definition.Noise + definition.TrafficGeneration / 3;
            var industrialExposure = definition.Category == BuildingCategory.Industrial ? 18 : 0;
            var utilityBuildingExposure = definition.Category == BuildingCategory.Utility ? 12 : 0;
            var densityExposure = definition.Id == "apartment_block" ? 10 : 0;
            var levelExposure = Math.Max(0, level - 1) * 3;
            return Math.Max(0, footprint * 2 + peopleExposure + utilityExposure + hazardExposure + industrialExposure + utilityBuildingExposure + densityExposure + levelExposure);
        }

        private static int FireBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(80, definition.ServiceRadius * 18 + definition.Jobs * 5 + definition.ServiceValue * 9);
        }

        private static int FireUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int DeathcareReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int DeathcareUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int SecurityWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (definition == null)
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residentPressure = capacity / 8;
            var workplacePressure = jobs / 6;
            var commercialPressure = definition.Category == BuildingCategory.Commercial ? 14 : 0;
            var densityPressure = definition.Id == "apartment_block" ? 10 : 0;
            return Math.Max(1, footprint + residentPressure + workplacePressure + commercialPressure + densityPressure);
        }

        private static int SecurityBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(75, definition.ServiceRadius * 18 + definition.Jobs * 5 + definition.ServiceValue * 10);
        }

        private static int SecurityReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int SecurityUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int AdvancedEducationWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (definition == null)
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residents = IsResidentialSensitiveBuilding(definition) ? capacity / 7 : 0;
            var officeDepth = IsOfficeBuilding(definition) ? jobs / 2 : 0;
            var workplaceDepth = (definition.Category == BuildingCategory.Commercial || definition.Category == BuildingCategory.Industrial || IsMixedUseBuilding(definition)) ? jobs / 5 : 0;
            return Math.Max(0, footprint + residents + officeDepth + workplaceDepth);
        }

        private static int WasteBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            var energyRecoveryCapacity = definition.ModelKey == "waste_to_energy" ? definition.PowerOutput : 0;
            return Math.Max(30, definition.ServiceRadius * 18 + definition.Jobs * 3 + definition.ServiceValue * 8 + energyRecoveryCapacity);
        }

        private static int WastewaterBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(70, definition.ServiceRadius * 20 + definition.Jobs * 5 + definition.ServiceValue * 12);
        }

        private static int TransitBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(60, definition.ServiceRadius * 18 + definition.Jobs * 5 + definition.ServiceValue * 8);
        }

        private static int RegionalConnectionBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(120, definition.ServiceRadius * 18 + definition.Jobs * 4 + definition.ServiceValue * 10);
        }

        private static int LogisticsBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(90, definition.ServiceRadius * 20 + definition.Jobs * 6 + definition.ServiceValue * 10);
        }

        private static int ResourceBuildingSupply(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(60, definition.ServiceRadius * 5 + definition.Jobs * 2 + definition.ServiceValue * 4);
        }

        private static int FreightRailImportSupply(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(140, definition.ServiceRadius * 8 + definition.Jobs * 3 + definition.ServiceValue * 6);
        }

        private static int WarehouseStorageCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(95, definition.ServiceRadius * 9 + definition.Jobs * 3 + definition.ServiceValue * 7);
        }

        private static int DisasterPreparednessBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(120, definition.ServiceRadius * 10 + definition.Jobs * 4 + definition.ServiceValue * 7);
        }

        private static int CommunicationBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(80, definition.ServiceRadius * 22 + definition.Jobs * 5 + definition.ServiceValue * 12);
        }

        private static int MailBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(70, definition.ServiceRadius * 18 + definition.Jobs * 5 + definition.ServiceValue * 10);
        }

        private static int DeathcareBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(80, definition.ServiceRadius * 16 + definition.Jobs * 4 + definition.ServiceValue * 9);
        }

        private static int HealthBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            var hospitalDepth = definition.Id == "district_hospital" ? 120 : 0;
            return Math.Max(80, definition.ServiceRadius * 20 + definition.Jobs * 6 + definition.ServiceValue * 10 + hospitalDepth);
        }

        private static int EducationBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            var collegeDepth = IsAdvancedEducationBuilding(definition) ? 140 : 0;
            return Math.Max(85, definition.ServiceRadius * 18 + definition.Jobs * 5 + definition.ServiceValue * 11 + collegeDepth);
        }

        private static int HealthcareLoad(int population, int jobs, int pollution, int noise)
        {
            if (population <= 0)
            {
                return 0;
            }

            return Math.Max(0, population / 3 + jobs / 14 + Math.Max(0, pollution) * 4 + Math.Max(0, noise) * 2);
        }

        private static int HealthReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int HealthUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int EducationLoad(int population, int jobs, int officeJobs, int industrialJobs)
        {
            if (population <= 0)
            {
                return 0;
            }

            return Math.Max(0, population * 3 / 10 + jobs / 18 + officeJobs / 5 + industrialJobs / 24);
        }

        private static int EducationReliability(int load, int capacity)
        {
            if (load <= 0)
            {
                return 100;
            }

            if (capacity <= 0)
            {
                return 0;
            }

            return capacity >= load ? 100 : ClampToScore((int)Math.Round(capacity * 100.0 / load));
        }

        private static int EducationUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int ComputeStudentBacklog(int population, int educationLoad, int educationCapacity, int educationCoverage, int advancedEducationCoverage, int educationUtilization)
        {
            if (population < 160)
            {
                return 0;
            }

            var capacityGap = Math.Max(0, educationLoad - educationCapacity);
            var coverageGap = Math.Max(0, 58 - educationCoverage);
            var advancedGap = population >= 360 ? Math.Max(0, 42 - advancedEducationCoverage) : 0;
            var overload = Math.Max(0, educationUtilization - 100);
            return ClampToScore(6 + population / 220 + capacityGap / 12 + coverageGap / 2 + advancedGap / 3 + overload / 3);
        }

        private static int ComputeLearningPipeline(int educationCoverage, int advancedEducationCoverage, int educationUtilization, int studentBacklog, int serviceReliability)
        {
            var overloadPenalty = Math.Max(0, educationUtilization - 95) / 2;
            return ClampToScore(educationCoverage / 2 + advancedEducationCoverage / 3 + serviceReliability / 6 - studentBacklog / 2 - overloadPenalty);
        }

        private int InnovationBaseForBuildings(List<PlacedBuilding> innovationBuildings)
        {
            var capacity = 0;
            for (var i = 0; i < innovationBuildings.Count; i += 1)
            {
                capacity += InnovationBuildingPotential(config.GetBuilding(innovationBuildings[i].ConfigId));
            }

            return capacity;
        }

        private static int InnovationBuildingPotential(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(75, definition.ServiceRadius * 8 + definition.Jobs * 3 + definition.ServiceValue * 5);
        }

        private static int ParkingBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(45, definition.ServiceRadius * 16 + definition.Jobs * 4 + definition.ServiceValue * 9);
        }

        private static int StormwaterBuildingCapacity(BuildingDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(55, definition.ServiceRadius * 14 + definition.Jobs * 4 + definition.ServiceValue * 8);
        }

        private static int PublicServiceBuildingCapacity(BuildingDefinition definition, int baseCapacity)
        {
            if (definition == null)
            {
                return 0;
            }

            return Math.Max(baseCapacity, definition.ServiceRadius * 15 + definition.Jobs * 4 + definition.ServiceValue * 8);
        }

        private static int WasteWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (definition == null)
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residentWaste = definition.Category == BuildingCategory.Residential ? capacity / 4 : 0;
            var workerWaste = jobs / 5;
            var dirtyWaste = definition.Pollution * 3 + definition.Noise / 2;
            return Math.Max(1, footprint + residentWaste + workerWaste + dirtyWaste);
        }

        private static int LogisticsWeightForBuilding(BuildingDefinition definition, int jobs)
        {
            if (!IsLogisticsSensitiveBuilding(definition))
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var freight = definition.TrafficGeneration * 2 + jobs / 3 + definition.Pollution * 2;
            return Math.Max(1, footprint + freight);
        }

        private static int CommunicationWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (!IsCommunicationSensitiveBuilding(definition))
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residents = definition.Category == BuildingCategory.Residential ? capacity / 5 : capacity / 8;
            var workforce = jobs / 3;
            var commerce = definition.Category == BuildingCategory.Commercial ? 12 : 0;
            return Math.Max(1, footprint + residents + workforce + commerce + definition.TrafficGeneration / 2);
        }

        private static int MailWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (!IsMailSensitiveBuilding(definition))
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residents = IsResidentialSensitiveBuilding(definition) ? capacity / 6 : 0;
            var commerce = definition.Category == BuildingCategory.Commercial ? jobs / 2 + 10 : 0;
            var office = IsOfficeBuilding(definition) ? jobs / 3 + 8 : 0;
            var visitor = IsAttractionBuilding(definition) ? definition.ServiceValue * 2 : 0;
            return Math.Max(1, footprint + residents + commerce + office + visitor + definition.TrafficGeneration / 3);
        }

        private static int DeathcareWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (!IsDeathcareSensitiveBuilding(definition))
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residents = IsResidentialSensitiveBuilding(definition) ? capacity / 7 : 0;
            var workforce = jobs / 16;
            var visitor = IsAttractionBuilding(definition) ? definition.ServiceValue : 0;
            var healthExposure = IsHealthBuilding(definition) ? 16 : 0;
            var shelterExposure = IsShelterBuilding(definition) ? 12 : 0;
            var hazardExposure = definition.Pollution + definition.Noise / 2;
            return Math.Max(1, footprint + residents + workforce + visitor + healthExposure + shelterExposure + hazardExposure);
        }

        private static int ParkingWeightForBuilding(BuildingDefinition definition, int capacity, int jobs)
        {
            if (!IsParkingSensitiveBuilding(definition))
            {
                return 0;
            }

            var footprint = Math.Max(1, definition.Size.W * definition.Size.H);
            var residents = IsResidentialSensitiveBuilding(definition) ? capacity / 7 : 0;
            var commerce = definition.Category == BuildingCategory.Commercial ? jobs / 3 + definition.TrafficGeneration : 0;
            var workplace = definition.Category == BuildingCategory.Industrial ? jobs / 8 : jobs / 6;
            var visitor = IsAttractionBuilding(definition) ? definition.ServiceValue * 2 : 0;
            return Math.Max(1, footprint + residents + commerce + workplace + visitor + definition.TrafficGeneration / 2);
        }

        private static int WasteShortfallPollution(int wasteWeight)
        {
            return Math.Max(1, wasteWeight / 16);
        }

        private static int WastewaterShortfallPollution(int shortfall)
        {
            return shortfall <= 0 ? 0 : Math.Max(1, shortfall / 14);
        }

        private static int StormwaterShortfallPollution(int shortfall, int floodRisk)
        {
            return shortfall <= 0 && floodRisk <= 35 ? 0 : shortfall / 28 + Math.Max(0, floodRisk - 35) / 8;
        }

        private void RefreshAlerts(float utilityEfficiency)
        {
            Metrics.Alerts.Clear();

            if (Metrics.PowerDemand > Metrics.PowerSupply)
            {
                Metrics.Alerts.Add("电力不足");
            }

            if (Metrics.WaterDemand > Metrics.WaterSupply)
            {
                Metrics.Alerts.Add("供水不足");
            }

            if (Metrics.Population >= 180 && Metrics.UtilityUtilization > 115)
            {
                Metrics.Alerts.Add("水电负荷过高");
            }

            if (Metrics.Population >= 320 && Metrics.UtilityUtilization > 95 && CountBuildingsById("solar_farm") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u6e05\u6d01\u7535\u529b");
            }

            if (Metrics.Population >= 180 && Metrics.WastewaterUtilization > 115)
            {
                Metrics.Alerts.Add("\u6c61\u6c34\u5904\u7406\u8fc7\u8f7d");
            }

            if (Metrics.Population >= 180 && Metrics.WastewaterReliability < 65)
            {
                Metrics.Alerts.Add("\u6c34\u73af\u5883\u98ce\u9669\u504f\u9ad8");
            }

            if (Metrics.Population >= 180 && Metrics.StormwaterUtilization > 115)
            {
                Metrics.Alerts.Add("\u96e8\u6d2a\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 220 && Metrics.FloodRisk > 55)
            {
                Metrics.Alerts.Add("\u5185\u6d9d\u98ce\u9669\u504f\u9ad8");
            }

            if (Metrics.ParkCoverage < 45 && Metrics.Population > 30)
            {
                Metrics.Alerts.Add("公园覆盖偏低");
            }

            if (Metrics.HealthCoverage < 35 && Metrics.Population > 120)
            {
                Metrics.Alerts.Add("医疗覆盖偏低");
            }

            if (Metrics.Population >= 180 && Metrics.HealthUtilization > 115)
            {
                Metrics.Alerts.Add("\u533b\u7597\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 220 && Metrics.MedicalResponse < 45)
            {
                Metrics.Alerts.Add("\u533b\u7597\u54cd\u5e94\u504f\u4f4e");
            }

            if (Metrics.Population >= 240 && Metrics.PatientBacklog > 55)
            {
                Metrics.Alerts.Add("\u75c5\u60a3\u79ef\u538b\u504f\u9ad8");
            }

            if (Metrics.Population >= 420 && Metrics.HealthCoverage < 50 && CountBuildingsById("district_hospital") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u533a\u57df\u533b\u9662");
            }

            if (Metrics.Population >= 300 && Metrics.DeathcareCoverage < 35)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u751f\u547d\u5173\u6000");
            }

            if (Metrics.Population >= 360 && Metrics.DeathcareUtilization > 115)
            {
                Metrics.Alerts.Add("\u751f\u547d\u5173\u6000\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 360 && Metrics.MortalityPressure > 55)
            {
                Metrics.Alerts.Add("\u6b7b\u4ea1\u538b\u529b\u504f\u9ad8");
            }

            if (Metrics.EducationCoverage < 35 && Metrics.Population > 260)
            {
                Metrics.Alerts.Add("教育覆盖偏低");
            }

            if (Metrics.Population >= 360 && Metrics.AdvancedEducationCoverage < 30)
            {
                Metrics.Alerts.Add("\u9ad8\u7b49\u6559\u80b2\u4e0d\u8db3");
            }

            if (Metrics.Population >= 260 && Metrics.EducationUtilization > 115)
            {
                Metrics.Alerts.Add("\u6559\u80b2\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 320 && Metrics.StudentBacklog > 55)
            {
                Metrics.Alerts.Add("\u5165\u5b66\u79ef\u538b\u504f\u9ad8");
            }

            if (Metrics.Population >= 360 && Metrics.LearningPipeline < 35)
            {
                Metrics.Alerts.Add("\u5b66\u4e60\u901a\u9053\u8584\u5f31");
            }

            if (Metrics.SafetyCoverage < 35 && Metrics.Population > 200)
            {
                Metrics.Alerts.Add("消防覆盖不足");
            }

            if (Metrics.Population >= 200 && Metrics.FireProtection < 35)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u6d88\u9632\u8986\u76d6");
            }

            if (Metrics.Population >= 260 && Metrics.FireUtilization > 115)
            {
                Metrics.Alerts.Add("\u6d88\u9632\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 220 && Metrics.FireRisk > 55)
            {
                Metrics.Alerts.Add("\u706b\u707e\u98ce\u9669\u504f\u9ad8");
            }

            if (Metrics.SecurityCoverage < 35 && Metrics.Population > 220)
            {
                Metrics.Alerts.Add("警务覆盖不足");
            }

            if (Metrics.Population >= 260 && Metrics.SecurityUtilization > 115)
            {
                Metrics.Alerts.Add("\u8b66\u52a1\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 280 && Metrics.PoliceResponse < 45)
            {
                Metrics.Alerts.Add("\u8b66\u52a1\u54cd\u5e94\u504f\u4f4e");
            }

            if (Metrics.Population >= 300 && Metrics.CaseBacklog > 55)
            {
                Metrics.Alerts.Add("\u6848\u4ef6\u79ef\u538b\u504f\u9ad8");
            }

            if (Metrics.CrimePressure > 60)
            {
                Metrics.Alerts.Add("治安压力偏高");
            }

            if (Metrics.Population >= 240 && Metrics.Attractiveness < 35)
            {
                Metrics.Alerts.Add("城市吸引力偏低");
            }

            if (Metrics.Population >= 620 && Metrics.Attractiveness < 45 && CountBuildingsById("convention_center") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u4f1a\u5c55\u5730\u6807");
            }

            if (CountBuildingsById("convention_center") > 0 && Metrics.Visitors >= 30 && Metrics.ParkingPressure > 65 && Metrics.TransitCoverage < 45)
            {
                Metrics.Alerts.Add("\u4f1a\u5c55\u4ea4\u901a\u627f\u538b");
            }

            if (Metrics.Population >= 260 && Metrics.WorkforceSkill < 35)
            {
                Metrics.Alerts.Add("劳动力素质偏低");
            }

            if (Metrics.Population >= 520 && Metrics.OfficeJobs >= 90 && Metrics.InnovationCapacity < 35 && CountBuildingsById("research_campus") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u7814\u53d1\u56ed\u533a");
            }

            if (CountBuildingsById("research_campus") > 0 && Metrics.InnovationCapacity < 40 && (Metrics.AdvancedEducationCoverage < 35 || Metrics.CommunicationCoverage < 45))
            {
                Metrics.Alerts.Add("\u7814\u53d1\u914d\u5957\u4e0d\u8db3");
            }

            if (Metrics.Population >= 150 && Metrics.LaborShortage > 45)
            {
                Metrics.Alerts.Add("用工缺口偏高");
            }

            if (Metrics.Population >= 180 && Metrics.CommuteEfficiency < 40)
            {
                Metrics.Alerts.Add("通勤效率偏低");
            }

            if (Metrics.Population >= 180 && Metrics.Walkability < 42)
            {
                Metrics.Alerts.Add("步行可达性偏低");
            }

            if (Metrics.Population >= 220 && Metrics.CarDependency > 72)
            {
                Metrics.Alerts.Add("汽车依赖偏高");
            }

            if (Metrics.Population >= 220 && Metrics.ParkingPressure > 60)
            {
                Metrics.Alerts.Add("停车压力偏高");
            }

            if (Metrics.Population >= 180 && Metrics.ParkingPressure > 60 && Metrics.ParkingCoverage < 30)
            {
                Metrics.Alerts.Add("\u505c\u8f66\u8bbe\u65bd\u4e0d\u8db3");
            }

            if (Metrics.Population >= 180 && Metrics.ParkingUtilization > 115)
            {
                Metrics.Alerts.Add("\u505c\u8f66\u8bbe\u65bd\u6ee1\u8f7d");
            }

            if (Metrics.Population >= 160 && Metrics.EnvironmentQuality < 42)
            {
                Metrics.Alerts.Add("环境质量偏低");
            }

            if (Metrics.Population >= 180 && Metrics.NoiseStress > 55)
            {
                Metrics.Alerts.Add("噪声压力偏高");
            }

            if (Metrics.Population >= 180 && Metrics.HealthRisk > 55)
            {
                Metrics.Alerts.Add("公共健康风险偏高");
            }

            if (Metrics.Population >= 220 && Metrics.PublicHealth < 40)
            {
                Metrics.Alerts.Add("公共健康偏低");
            }

            if (Metrics.Population >= 180 && Metrics.ServiceUtilization > 115)
            {
                Metrics.Alerts.Add("公共服务容量不足");
            }

            if (Metrics.Population >= 180 && Metrics.ServiceEquity < 45)
            {
                Metrics.Alerts.Add("片区服务不均");
            }

            if (Metrics.Population >= 180 && Metrics.ServiceGapPressure > 45 && Metrics.ServiceGapFocus != "\u5747\u8861")
            {
                Metrics.Alerts.Add("\u670d\u52a1\u7f3a\u53e3\uff1a" + Metrics.UnderservedResidents + "\u4eba/" + Metrics.ServiceGapFocus);
            }

            if (Metrics.Population >= 160 && Metrics.LivingCondition < 45)
            {
                Metrics.Alerts.Add("宜居度偏低");
            }

            if (Metrics.Population >= 220 && Metrics.LivingPressure > 60)
            {
                Metrics.Alerts.Add("生活压力偏高");
            }

            if (Metrics.Population >= 160 && Metrics.MaintenanceCondition < 45)
            {
                Metrics.Alerts.Add("城市维护状态偏低");
            }

            if (Metrics.Population >= 180 && Metrics.EmergencyResponse < 42)
            {
                Metrics.Alerts.Add("应急响应偏低");
            }

            if (Metrics.Population >= 360 && Metrics.DisasterPreparedness < 45)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u5e94\u6025\u907f\u96be");
            }

            if (Metrics.Population >= 220 && Metrics.DisasterRisk > 58)
            {
                Metrics.Alerts.Add("\u57ce\u5e02\u707e\u5bb3\u98ce\u9669\u504f\u9ad8");
            }

            if (Metrics.WasteCoverage < 35 && Metrics.Population >= 220)
            {
                Metrics.Alerts.Add("回收覆盖不足");
            }

            if (Metrics.Population >= 220 && Metrics.WasteUtilization > 115)
            {
                Metrics.Alerts.Add("\u56de\u6536\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Population >= 520 && Metrics.WasteUtilization > 105 && CountBuildingsById("waste_to_energy_plant") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u5783\u573e\u53d1\u7535");
            }

            if (Metrics.Congestion > 70)
            {
                Metrics.Alerts.Add("道路拥堵严重");
            }

            if (Metrics.Congestion > 65 && Metrics.ArterialRoadTiles < 6 && Metrics.RoadTiles >= 12)
            {
                Metrics.Alerts.Add("可升级主干道缓解拥堵");
            }

            if (Metrics.RoadTiles >= 18 && Metrics.RoadConnectivity < 45)
            {
                Metrics.Alerts.Add("路网连通性偏低");
            }

            if (Metrics.RoadTiles >= 18 && Metrics.RoadBottleneckPressure > 55)
            {
                Metrics.Alerts.Add("道路瓶颈偏高");
            }

            if (Metrics.RoadTiles >= 18 && Metrics.IntersectionDelay > 50)
            {
                Metrics.Alerts.Add("路口延误偏高");
            }

            if (Metrics.RoadTiles >= 18 && Metrics.RoadMaintenanceCoverage < 35)
            {
                Metrics.Alerts.Add("道路养护不足");
            }

            if (Metrics.Population >= 180 && Metrics.AccidentRisk > 55)
            {
                Metrics.Alerts.Add("道路事故风险偏高");
            }

            if (Metrics.RoadTiles >= 24 && Metrics.RoadSafety < 45)
            {
                Metrics.Alerts.Add("道路安全偏低");
            }

            if (Metrics.Population >= 180 && Metrics.TransitCoverage < 25)
            {
                Metrics.Alerts.Add("公共交通覆盖不足");
            }

            if (Metrics.Population >= 220 && Metrics.TransitUtilization > 115)
            {
                Metrics.Alerts.Add("公交运力不足");
            }

            if (Metrics.Population >= 240 && Metrics.TransitCoverage >= 25 && Metrics.TransitReliability < 60)
            {
                Metrics.Alerts.Add("\u516c\u4ea4\u53ef\u9760\u6027\u504f\u4f4e");
            }

            if (Metrics.Population >= 260 && Metrics.TransitWaitPressure > 55)
            {
                Metrics.Alerts.Add("\u516c\u4ea4\u5019\u8f66\u538b\u529b\u504f\u9ad8");
            }

            if (Metrics.Population >= 520 && Metrics.TransitUtilization > 105 && CountBuildingsById("metro_station") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u8f68\u9053\u4ea4\u901a");
            }

            if (Metrics.Population >= 680 && Metrics.RegionalConnectivity < 35)
            {
                Metrics.Alerts.Add("\u5916\u90e8\u8fde\u63a5\u4e0d\u8db3");
            }

            if (Metrics.Jobs >= 120 && Metrics.LogisticsCoverage < 25)
            {
                Metrics.Alerts.Add("货运覆盖不足");
            }

            if (Metrics.Jobs >= 180 && Metrics.LogisticsUtilization > 115)
            {
                Metrics.Alerts.Add("货运运力不足");
            }

            if (Metrics.Population >= 180 && Metrics.CommunicationCoverage < 35)
            {
                Metrics.Alerts.Add("通信覆盖不足");
            }

            if (Metrics.Population >= 260 && Metrics.CommunicationUtilization > 115)
            {
                Metrics.Alerts.Add("通信容量不足");
            }

            if (Metrics.Jobs >= 180 && Metrics.BusinessEfficiency < 45)
            {
                Metrics.Alerts.Add("企业效率偏低");
            }

            if (Metrics.Population >= 240 && Metrics.MailCoverage < 35)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u90ae\u653f\u670d\u52a1");
            }

            if (Metrics.Population >= 360 && Metrics.MailUtilization > 115)
            {
                Metrics.Alerts.Add("\u90ae\u653f\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.Jobs >= 220 && Metrics.MailReliability < 55)
            {
                Metrics.Alerts.Add("\u90ae\u4ef6\u914d\u9001\u53d7\u963b");
            }

            if (Metrics.Population >= 160 && Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 70)
            {
                Metrics.Alerts.Add("商品供应不足");
            }

            if (Metrics.Population >= 260 && Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 78 && CountBuildingsById("resource_processor") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u672c\u5730\u8d44\u6e90");
            }

            if (Metrics.Population >= 420 && Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 82 && Metrics.GoodsStorage == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u914d\u9001\u4e2d\u5fc3");
            }

            if (CountBuildingsById("resource_processor") > 0 && Metrics.LocalGoodsSupply < 55 && Metrics.LogisticsCoverage < 35)
            {
                Metrics.Alerts.Add("\u8d44\u6e90\u7269\u6d41\u4e0d\u8db3");
            }

            if (CountConnectedBuildingsById("resource_processor") > 0 && Metrics.Population >= 260 && Metrics.GoodsDemand > 0 && (Metrics.ResourceSpecialization < 45 || Metrics.IndustrialSpecialization < 40))
            {
                Metrics.Alerts.Add("\u672c\u5730\u8d44\u6e90\u9002\u914d\u4e0d\u8db3");
            }

            if (Metrics.GoodsStorage > 0 && Metrics.SupplyChainStability < 45 && Metrics.LogisticsUtilization > 110)
            {
                Metrics.Alerts.Add("\u4ed3\u50a8\u8c03\u5ea6\u53d7\u963b");
            }

            if (Metrics.Population >= 760 && Metrics.GoodsDemand > 0 && Metrics.GoodsBalance < 85 && CountBuildingsById("freight_rail_terminal") == 0)
            {
                Metrics.Alerts.Add("\u7f3a\u5c11\u8d27\u8fd0\u94c1\u8def");
            }

            if (CountBuildingsById("freight_rail_terminal") > 0 && Metrics.FreightImportSupply < 90 && Metrics.LogisticsUtilization > 115)
            {
                Metrics.Alerts.Add("\u94c1\u8def\u8d27\u8fd0\u53d7\u963b");
            }

            if (Metrics.Population >= 220 && Metrics.IdleZoneTiles >= 25 && Metrics.LandUseEfficiency < 45)
            {
                Metrics.Alerts.Add("空置分区过多");
            }

            if (Metrics.Population >= 180 && Metrics.DevelopmentQuality < 45)
            {
                Metrics.Alerts.Add("片区品质偏低");
            }

            if (Metrics.Population >= 180 && Metrics.LandUseConflict > 35)
            {
                Metrics.Alerts.Add("用地冲突偏高");
            }

            if (Metrics.Population >= 260 && Metrics.UpgradedBuildings == 0)
            {
                Metrics.Alerts.Add("建筑成长停滞");
            }

            if (Metrics.Population >= 160 && Metrics.RentPressure > 72)
            {
                Metrics.Alerts.Add("居住成本过高");
            }

            if (Metrics.Population >= 180 && Metrics.RentPressure > 72 && !HasAutoDevelopmentSite(ZoneType.Residential, "apartment_block"))
            {
                Metrics.Alerts.Add("缺少高密住宅地块");
            }

            if (Metrics.Demand.Residential > 75 && !HasAutoDevelopmentSite(ZoneType.Residential, "residential_pod"))
            {
                Metrics.Alerts.Add("住宅分区缺少适宜地块");
            }

            if (Metrics.Demand.Commercial > 75 && !HasAutoDevelopmentSite(ZoneType.Commercial, "market_corner"))
            {
                Metrics.Alerts.Add("商业分区缺少适宜地块");
            }

            if (Metrics.Demand.Office > 75 && !HasAutoDevelopmentSite(ZoneType.Office, "office_studio"))
            {
                Metrics.Alerts.Add("办公分区缺少适宜地块");
            }

            if (Metrics.Demand.MixedUse > 75 && !HasAutoDevelopmentSite(ZoneType.MixedUse, "mixed_use_block"))
            {
                Metrics.Alerts.Add("混合用地缺少适宜地块");
            }

            if (Metrics.Demand.Industrial > 75 && !HasAutoDevelopmentSite(ZoneType.Industrial, "maker_yard"))
            {
                Metrics.Alerts.Add("工业分区缺少适宜地块");
            }

            if (Metrics.NetIncome < 0)
            {
                Metrics.Alerts.Add("预算赤字");
            }

            if (Metrics.Population >= 120 && Metrics.FiscalHealth < 42)
            {
                Metrics.Alerts.Add("\u8d22\u653f\u4fe1\u7528\u504f\u4f4e");
            }

            if (Metrics.Population >= 300 && Metrics.AdministrationEfficiency < 45)
            {
                Metrics.Alerts.Add("\u884c\u653f\u6548\u7387\u504f\u4f4e");
            }

            if (Metrics.Population >= 300 && Metrics.AdministrationUtilization > 115)
            {
                Metrics.Alerts.Add("\u884c\u653f\u5bb9\u91cf\u4e0d\u8db3");
            }

            if (Metrics.ActivePolicies.Count >= 3 && (Metrics.AdministrationEfficiency < 55 || Metrics.PolicyBacklog > 45))
            {
                Metrics.Alerts.Add("\u653f\u7b56\u6267\u884c\u8fc7\u8f7d");
            }

            if (Metrics.ActivePolicies.Count >= 2 && Metrics.PolicyBacklog > 55)
            {
                Metrics.Alerts.Add("\u653f\u7b56\u79ef\u538b\u504f\u9ad8");
            }

            if (Metrics.Population >= 160 && Metrics.DebtPressure > 60)
            {
                Metrics.Alerts.Add("\u503a\u52a1\u538b\u529b\u504f\u9ad8");
            }

            if (Metrics.BondPrincipal > 0 && Metrics.BondPayment > Math.Max(120, Metrics.TaxIncome / 4))
            {
                Metrics.Alerts.Add("\u503a\u52a1\u670d\u52a1\u8fc7\u9ad8");
            }

            if (Metrics.Population >= 100 && Metrics.Cash < Math.Max(500, Metrics.UpkeepExpense + Metrics.RoadExpense + Metrics.PolicyExpense))
            {
                Metrics.Alerts.Add("\u73b0\u91d1\u7f13\u51b2\u4e0d\u8db3");
            }

            if (Metrics.NetIncome < 0 && Metrics.CashRunwayDays <= 45)
            {
                Metrics.Alerts.Add("\u73b0\u91d1\u8dd1\u9053\u4e0d\u8db3\uff1a" + Metrics.CashRunwayDays + "\u5929");
            }

            if (Metrics.ForecastRisk >= 75)
            {
                Metrics.Alerts.Add("\u98ce\u9669\u9884\u8b66\uff1a" + Metrics.ForecastFocus + "/" + Metrics.ForecastAction);
            }
            else if (Metrics.ForecastRisk >= 60)
            {
                Metrics.Alerts.Add("\u8fd0\u8425\u9884\u8b66\uff1a" + Metrics.ForecastFocus + "/" + Metrics.ForecastAction);
            }

            if (taxLevel == CityTaxLevel.High && Metrics.Happiness < 60)
            {
                Metrics.Alerts.Add("税率压力偏高");
            }

            if (Metrics.PolicyExpense > 0 && Metrics.PolicyExpense > Math.Max(50, Metrics.TaxIncome / 2))
            {
                Metrics.Alerts.Add("政策支出偏高");
            }

            if (IsPolicyActive(CityPolicy.CompleteStreets) && Metrics.Congestion > 75 && Metrics.CarDependency > 65)
            {
                Metrics.Alerts.Add("\u5b8c\u6574\u8857\u9053\u62e5\u5835");
            }

            if (IsPolicyActive(CityPolicy.SignalOptimization) && Metrics.Congestion > 70 && Metrics.IntersectionRoadTiles >= 6)
            {
                Metrics.Alerts.Add("\u4fe1\u53f7\u4f18\u5316\u8fc7\u8f7d");
            }

            if (IsPolicyActive(CityPolicy.CongestionPricing) && Metrics.Population >= 160 && Metrics.CarDependency > 68 && Metrics.TransitCoverage < 28)
            {
                Metrics.Alerts.Add("\u62e5\u5835\u6536\u8d39\u963b\u529b");
            }

            if (IsPolicyActive(CityPolicy.ParkingFees) && Metrics.Population >= 180 && Metrics.ParkingPressure > 58 && Metrics.TransitCoverage < 32)
            {
                Metrics.Alerts.Add("\u505c\u8f66\u6536\u8d39\u963b\u529b");
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Lean && Metrics.ServiceCoverage < 55 && Metrics.Population >= 120)
            {
                Metrics.Alerts.Add("服务预算偏低");
            }

            if (serviceBudgetLevel == CityServiceBudgetLevel.Boosted && Metrics.NetIncome < 0)
            {
                Metrics.Alerts.Add("服务预算推高赤字");
            }

            if (Metrics.DisconnectedBuildings > 0)
            {
                Metrics.Alerts.Add("有建筑未接入道路");
            }

            if (utilityEfficiency < 0.8f)
            {
                Metrics.Alerts.Add("基础设施效率下降");
            }
        }

        private void RefreshMilestones()
        {
            Metrics.Milestones.Clear();
            AddMilestone("road_grid", "形成路网", "铺设 24 格道路", Metrics.RoadTiles, 24);
            AddMilestone("connected_grid", "连通路网", "路网连通性达到 60%", Metrics.RoadConnectivity, 60);
            AddMilestone("arterial_spine", "主干路网", "升级 8 格主干道", Metrics.ArterialRoadTiles, 8);
            AddMilestone("road_care", "道路养护", "道路养护覆盖达到 60% 且维护状态达到 65%", Metrics.RoadMaintenanceCoverage >= 60 && Metrics.MaintenanceCondition >= 65 ? 1 : 0, 1);
            AddMilestone("safe_roads", "安全道路", "道路安全达到 70 且事故风险不高于 32", Metrics.RoadSafety >= 70 && Metrics.AccidentRisk <= 32 ? 1 : 0, 1);
            AddMilestone("traffic_flow", "交通流线", "路网连通性达到 60%，道路瓶颈不高于 35 且路口延误不高于 35", Metrics.RoadConnectivity >= 60 && Metrics.RoadBottleneckPressure <= 35 && Metrics.IntersectionDelay <= 35 ? 1 : 0, 1);
            AddMilestone("first_residents", "第一批居民", "人口达到 120", Metrics.Population, 120);
            AddMilestone("affordable_city", "可负担社区", "人口 250 且居住成本压力不高", Metrics.Population >= 250 && Metrics.RentPressure <= 45 ? 1 : 0, 1);
            AddMilestone("livable_district", "宜居街区", "人口 250 后宜居度达到 65 且生活压力不高于 35", Metrics.Population >= 250 && Metrics.LivingCondition >= 65 && Metrics.LivingPressure <= 35 ? 1 : 0, 1);
            AddMilestone("zoned_growth", "分区生长", "通过分区吸引 6 栋建筑", Metrics.ZonedDevelopmentBuildings, 6);
            AddMilestone("compact_city", "紧凑用地", "用地效率达到 60%", Metrics.LandUseEfficiency, 60);
            AddMilestone("quality_blocks", "优质片区", "发展品质达到 68%", Metrics.DevelopmentQuality, 68);
            AddMilestone("zoning_buffer", "功能缓冲", "人口 160 后用地冲突控制在 18% 以下", Metrics.Population >= 160 ? Math.Max(0, 100 - Metrics.LandUseConflict) : 0, 82);
            AddMilestone("density_core", "高密住区", "形成 3 栋公寓楼", Metrics.HighDensityResidentialBuildings, 3);
            AddMilestone("mixed_core", "混合核心", "形成 3 栋混合街区", Metrics.MixedUseBuildings, 3);
            AddMilestone("knowledge_economy", "知识经济", "办公岗位达到 120", Metrics.OfficeJobs, 120);
            AddMilestone("innovation_district", "\u521b\u65b0\u9ad8\u5730", "\u5efa\u6210 1 \u5ea7\u7814\u53d1\u56ed\u533a\u4e14\u521b\u65b0\u80fd\u529b\u8fbe\u5230 65", CountBuildingsById("research_campus") > 0 && Metrics.InnovationCapacity >= 65 ? 1 : 0, 1);
            AddMilestone("city_attraction", "城市吸引力", "吸引力达到 60", Metrics.Attractiveness, 60);
            AddMilestone("convention_draw", "\u4f1a\u5c55\u5ba2\u6d41", "\u5efa\u6210 1 \u5ea7\u4f1a\u5c55\u4e2d\u5fc3\u4e14\u6e38\u5ba2\u8fbe\u5230 80", CountBuildingsById("convention_center") > 0 && Metrics.Visitors >= 80 ? 1 : 0, 1);
            AddMilestone("talent_pool", "人才城市", "劳动力素质达到 65", Metrics.WorkforceSkill, 65);
            AddMilestone("higher_education", "\u9ad8\u7b49\u6559\u80b2", "\u9ad8\u7b49\u6559\u80b2\u8986\u76d6\u8fbe\u5230 55% \u4e14\u52b3\u52a8\u529b\u7d20\u8d28\u8fbe\u5230 65", Metrics.AdvancedEducationCoverage >= 55 && Metrics.WorkforceSkill >= 65 ? 1 : 0, 1);
            AddMilestone("walkable_city", "步行城市", "步行可达性达到 65%", Metrics.Walkability, 65);
            AddMilestone("smooth_commute", "顺畅通勤", "通勤效率达到 65", Metrics.CommuteEfficiency, 65);
            AddMilestone("low_car_core", "低车依赖", "人口 220 后汽车依赖不高于 55 且停车压力不高于 38", Metrics.Population >= 220 && Metrics.CarDependency <= 55 && Metrics.ParkingPressure <= 38 ? 1 : 0, 1);
            AddMilestone("parking_relief", "\u505c\u8f66\u8c03\u5ea6", "\u505c\u8f66\u8986\u76d6\u8fbe\u5230 45% \u4e14\u5229\u7528\u7387\u4e0d\u9ad8\u4e8e 100%", Metrics.ParkingCoverage >= 45 && (Metrics.ParkingLoad == 0 || Metrics.ParkingUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("green_city", "绿色宜居", "环境质量达到 70", Metrics.EnvironmentQuality, 70);
            AddMilestone("healthy_city", "健康城市", "公共健康达到 70", Metrics.PublicHealth, 70);
            AddMilestone("balanced_utilities", "基础设施平衡", "电力和供水都满足需求", BalancedUtilityProgress(), 2);
            AddMilestone("utility_resilience", "水电韧性", "水电可靠性达到 95% 且利用率不高于 100%", Metrics.UtilityReliability >= 95 && (Metrics.UtilityLoad == 0 || Metrics.UtilityUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("renewable_power", "\u6e05\u6d01\u7535\u529b", "\u5efa\u6210 1 \u5ea7\u592a\u9633\u80fd\u9635\u5217\u4e14\u6c34\u7535\u53ef\u9760\u6027\u8fbe\u5230 95%", CountBuildingsById("solar_farm") > 0 && Metrics.UtilityReliability >= 95 ? 1 : 0, 1);
            AddMilestone("water_sanitation", "\u6c34\u73af\u5883", "\u6c61\u6c34\u5904\u7406\u53ef\u9760\u6027\u8fbe\u5230 85% \u4e14\u5229\u7528\u7387\u4e0d\u9ad8\u4e8e 100%", Metrics.WastewaterReliability >= 85 && (Metrics.WastewaterLoad == 0 || Metrics.WastewaterUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("stormwater_ready", "\u96e8\u6d2a\u97e7\u6027", "\u96e8\u6d2a\u97e7\u6027\u8fbe\u5230 75 \u4e14\u5185\u6d9d\u98ce\u9669\u4e0d\u9ad8\u4e8e 32", Metrics.StormwaterResilience >= 75 && Metrics.FloodRisk <= 32 ? 1 : 0, 1);
            AddMilestone("maintenance_ready", "城市运维", "维护状态达到 70%", Metrics.MaintenanceCondition, 70);
            AddMilestone("service_core", "生活服务圈", "综合服务覆盖达到 65%", Metrics.ServiceCoverage, 65);
            AddMilestone("service_capacity", "公共服务容量", "服务覆盖达到 60% 且利用率不高于 100%", Metrics.ServiceCoverage >= 60 && (Metrics.ServiceLoad == 0 || Metrics.ServiceUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("balanced_services", "均衡服务", "人口 200 后片区服务公平达到 65%", Metrics.Population >= 200 ? Metrics.ServiceEquity : 0, 65);
            AddMilestone("response_ready", "应急响应", "应急响应达到 65%", Metrics.EmergencyResponse, 65);
            AddMilestone("disaster_preparedness", "\u707e\u5bb3\u51c6\u5907", "\u5efa\u6210 1 \u5ea7\u63a5\u8def\u5e94\u6025\u907f\u96be\u4e2d\u5fc3\u4e14\u707e\u5907\u8fbe\u5230 65", CountConnectedBuildingsById("emergency_shelter") > 0 && Metrics.DisasterPreparedness >= 65 ? 1 : 0, 1);
            AddMilestone("health_net", "社区医疗网", "医疗覆盖达到 50%", Metrics.HealthCoverage, 50);
            AddMilestone("regional_healthcare", "\u533a\u57df\u533b\u7597\u4e2d\u5fc3", "\u5efa\u6210 1 \u5ea7\u533a\u57df\u533b\u9662\u4e14\u533b\u7597\u8986\u76d6\u8fbe\u5230 65%", CountBuildingsById("district_hospital") > 0 && Metrics.HealthCoverage >= 65 ? 1 : 0, 1);
            AddMilestone("healthcare_capacity", "\u533b\u7597\u5bb9\u91cf", "\u533b\u7597\u8986\u76d6\u8fbe\u5230 60%\uff0c\u533b\u7597\u54cd\u5e94\u8fbe\u5230 65\uff0c\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 100% \u4e14\u75c5\u60a3\u79ef\u538b\u4e0d\u9ad8\u4e8e 35", Metrics.HealthCoverage >= 60 && Metrics.MedicalResponse >= 65 && (Metrics.HealthLoad == 0 || Metrics.HealthUtilization <= 100) && Metrics.PatientBacklog <= 35 ? 1 : 0, 1);
            AddMilestone("deathcare_ready", "\u751f\u547d\u5173\u6000", "\u5efa\u6210 1 \u5ea7\u63a5\u8def\u751f\u547d\u82b1\u56ed\uff0c\u8986\u76d6\u8fbe\u5230 55% \u4e14\u6b7b\u4ea1\u538b\u529b\u4e0d\u9ad8\u4e8e 40", CountConnectedBuildingsById("memorial_garden") > 0 && Metrics.DeathcareCoverage >= 55 && Metrics.MortalityPressure <= 40 && (Metrics.DeathcareLoad == 0 || Metrics.DeathcareUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("education_net", "教育网络", "教育覆盖达到 45%", Metrics.EducationCoverage, 45);
            AddMilestone("education_capacity", "\u5b66\u4f4d\u5bb9\u91cf", "\u6559\u80b2\u8986\u76d6\u8fbe\u5230 55%\uff0c\u5b66\u4e60\u901a\u9053\u8fbe\u5230 55\uff0c\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 100% \u4e14\u5165\u5b66\u79ef\u538b\u4e0d\u9ad8\u4e8e 35", Metrics.EducationCoverage >= 55 && Metrics.LearningPipeline >= 55 && (Metrics.EducationLoad == 0 || Metrics.EducationUtilization <= 100) && Metrics.StudentBacklog <= 35 ? 1 : 0, 1);
            AddMilestone("safety_net", "消防网络", "消防覆盖达到 45%", Metrics.SafetyCoverage, 45);
            AddMilestone("fire_resilience", "\u706b\u707e\u97e7\u6027", "\u6d88\u9632\u4fdd\u62a4\u8fbe\u5230 70\uff0c\u706b\u707e\u98ce\u9669\u4e0d\u9ad8\u4e8e 32\uff0c\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 100% \u4e14\u54cd\u5e94\u8fbe\u5230 65", CountConnectedBuildingsById("fire_station") > 0 && Metrics.FireProtection >= 70 && Metrics.FireRisk <= 32 && (Metrics.FireLoad == 0 || Metrics.FireUtilization <= 100) && Metrics.FireResponse >= 65 ? 1 : 0, 1);
            AddMilestone("secure_blocks", "平安街区", "警务覆盖达到 45%", Metrics.SecurityCoverage, 45);
            AddMilestone("police_readiness", "\u8b66\u52a1\u54cd\u5e94", "\u5efa\u6210 1 \u5ea7\u63a5\u8def\u8b66\u52a1\u5206\u5c40\uff0c\u8b66\u52a1\u54cd\u5e94\u8fbe\u5230 65\uff0c\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 100% \u4e14\u6848\u4ef6\u79ef\u538b\u4e0d\u9ad8\u4e8e 35", CountConnectedBuildingsById("police_precinct") > 0 && Metrics.PoliceResponse >= 65 && (Metrics.SecurityLoad == 0 || Metrics.SecurityUtilization <= 100) && Metrics.CaseBacklog <= 35 ? 1 : 0, 1);
            AddMilestone("clean_blocks", "清洁街区", "回收覆盖达到 50%", Metrics.WasteCoverage, 50);
            AddMilestone("waste_capacity", "\u56de\u6536\u5bb9\u91cf", "\u56de\u6536\u8986\u76d6\u8fbe\u5230 50% \u4e14\u5229\u7528\u7387\u4e0d\u9ad8\u4e8e 100%", Metrics.WasteCoverage >= 50 && (Metrics.WasteLoad == 0 || Metrics.WasteUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("waste_to_energy", "\u8d44\u6e90\u56de\u6536\u80fd\u6e90", "\u5efa\u6210 1 \u5ea7\u5783\u573e\u53d1\u7535\u5382\u4e14\u56de\u6536\u7a33\u5b9a\u5ea6\u8fbe\u5230 75%", CountBuildingsById("waste_to_energy_plant") > 0 && Metrics.WasteReliability >= 75 ? 1 : 0, 1);
            AddMilestone("freight_loop", "货运循环", "货运覆盖达到 45%", Metrics.LogisticsCoverage, 45);
            AddMilestone("freight_capacity", "货运运力", "货运覆盖达到 45% 且利用率不高于 100%", Metrics.LogisticsCoverage >= 45 && (Metrics.LogisticsLoad == 0 || Metrics.LogisticsUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("supply_chain_buffer", "\u4f9b\u5e94\u94fe\u7f13\u51b2", "\u5efa\u6210 1 \u5ea7\u63a5\u8def\u914d\u9001\u4e2d\u5fc3\u4e14\u4f9b\u5e94\u94fe\u7a33\u5b9a\u8fbe\u5230 65", CountConnectedBuildingsById("distribution_center") > 0 && Metrics.GoodsStorage > 0 && Metrics.SupplyChainStability >= 65 ? 1 : 0, 1);
            AddMilestone("rail_freight_gateway", "\u94c1\u8def\u8d27\u8fd0", "\u5efa\u6210 1 \u5ea7\u8d27\u8fd0\u94c1\u8def\u7ad9\u4e14\u94c1\u8def\u5bfc\u5165\u4e0d\u4f4e\u4e8e 100", CountBuildingsById("freight_rail_terminal") > 0 && Metrics.FreightImportSupply >= 100 ? 1 : 0, 1);
            AddMilestone("connected_business", "智慧商务", "通信覆盖达到 55% 且企业效率达到 60", Metrics.CommunicationCoverage >= 55 && Metrics.BusinessEfficiency >= 60 ? 1 : 0, 1);
            AddMilestone("communication_capacity", "通信容量", "通信覆盖达到 55% 且通信利用率不高于 100%", Metrics.CommunicationCoverage >= 55 && (Metrics.CommunicationLoad == 0 || Metrics.CommunicationUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("mail_service", "\u90ae\u653f\u7f51\u7edc", "\u5efa\u6210 1 \u5ea7\u63a5\u8def\u90ae\u653f\u7ad9\u4e14\u90ae\u653f\u8986\u76d6\u8fbe\u5230 55% \u4e14\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 100%", CountConnectedBuildingsById("post_office") > 0 && Metrics.MailCoverage >= 55 && (Metrics.MailLoad == 0 || Metrics.MailUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("goods_market", "商品市场", "商品供给达到需求的 90%", Metrics.GoodsDemand == 0 || Metrics.GoodsBalance >= 90 ? 1 : 0, 1);
            AddMilestone("local_supply", "\u672c\u5730\u4f9b\u7ed9", "\u5efa\u6210 1 \u5ea7\u8d44\u6e90\u52a0\u5de5\u56ed\u4e14\u5546\u54c1\u5e73\u8861\u8fbe\u5230 95%", CountBuildingsById("resource_processor") > 0 && Metrics.LocalGoodsSupply >= 55 && Metrics.GoodsBalance >= 95 ? 1 : 0, 1);
            AddMilestone("specialized_industry", "\u4ea7\u4e1a\u4e13\u7cbe", "\u63a5\u8def\u8d44\u6e90\u52a0\u5de5\u56ed\u8d44\u6e90\u9002\u914d\u8fbe\u5230 65 \u4e14\u4ea7\u4e1a\u4e13\u7cbe\u8fbe\u5230 60", CountConnectedBuildingsById("resource_processor") > 0 && Metrics.ResourceSpecialization >= 65 && Metrics.IndustrialSpecialization >= 60 ? 1 : 0, 1);
            AddMilestone("service_budget_balance", "服务预算平衡", "服务覆盖达到 60% 且月净收入不为负", Metrics.ServiceCoverage >= 60 && Metrics.NetIncome >= 0 ? 1 : 0, 1);
            AddMilestone("healthy_budget", "财政转正", "月度净收入不低于 0", Metrics.NetIncome >= 0 && Metrics.Population >= 80 ? 1 : 0, 1);
            AddMilestone("fiscal_credit", "\u8d22\u653f\u4fe1\u7528", "\u8d22\u653f\u4fe1\u7528\u8fbe\u5230 70 \u4e14\u503a\u52a1\u538b\u529b\u4e0d\u9ad8\u4e8e 25", Metrics.FiscalHealth >= 70 && Metrics.DebtPressure <= 25 ? 1 : 0, 1);
            AddMilestone("debt_service_control", "\u507f\u503a\u7eaa\u5f8b", "\u8d22\u653f\u4fe1\u7528\u8fbe\u5230 60 \u4e14\u503a\u52a1\u538b\u529b\u4e0d\u9ad8\u4e8e 35", Metrics.FiscalHealth >= 60 && Metrics.DebtPressure <= 35 ? 1 : 0, 1);
            AddMilestone("civic_administration", "\u5e02\u653f\u4e2d\u5fc3", "\u5efa\u6210 1 \u5ea7\u5e02\u653f\u5385\u4e14\u884c\u653f\u6548\u7387\u8fbe\u5230 65%", CountBuildingsById("city_hall") > 0 && Metrics.AdministrationEfficiency >= 65 ? 1 : 0, 1);
            AddMilestone("administration_capacity", "\u884c\u653f\u5bb9\u91cf", "\u884c\u653f\u6548\u7387\u8fbe\u5230 65\uff0c\u884c\u653f\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 100% \u4e14\u653f\u7b56\u79ef\u538b\u4e0d\u9ad8\u4e8e 35", Metrics.AdministrationEfficiency >= 65 && (Metrics.AdministrationLoad == 0 || Metrics.AdministrationUtilization <= 100) && Metrics.PolicyBacklog <= 35 ? 1 : 0, 1);
            AddMilestone("policy_trial", "政策试点", "启用 1 项城市政策", Metrics.ActivePolicies.Count, 1);
            AddMilestone("complete_streets", "\u5b8c\u6574\u8857\u9053", "\u542f\u7528\u5b8c\u6574\u8857\u9053\uff0c\u6b65\u884c\u8fbe\u5230 65% \u4e14\u4e8b\u6545\u98ce\u9669\u4e0d\u9ad8\u4e8e 35", IsPolicyActive(CityPolicy.CompleteStreets) && Metrics.Walkability >= 65 && Metrics.AccidentRisk <= 35 ? 1 : 0, 1);
            AddMilestone("signal_optimization", "\u4fe1\u53f7\u4f18\u5316", "\u542f\u7528\u4fe1\u53f7\u4f18\u5316\uff0c\u62e5\u5835\u4e0d\u9ad8\u4e8e 55 \u4e14\u4e8b\u6545\u98ce\u9669\u4e0d\u9ad8\u4e8e 35", IsPolicyActive(CityPolicy.SignalOptimization) && Metrics.Congestion <= 55 && Metrics.AccidentRisk <= 35 ? 1 : 0, 1);
            AddMilestone("congestion_pricing", "\u62e5\u5835\u6536\u8d39", "\u542f\u7528\u62e5\u5835\u6536\u8d39\uff0c\u62e5\u5835\u4e0d\u9ad8\u4e8e 55 \u4e14\u6c7d\u8f66\u4f9d\u8d56\u4e0d\u9ad8\u4e8e 55", IsPolicyActive(CityPolicy.CongestionPricing) && Metrics.Congestion <= 55 && Metrics.CarDependency <= 55 ? 1 : 0, 1);
            AddMilestone("parking_fees", "\u505c\u8f66\u6536\u8d39", "\u542f\u7528\u505c\u8f66\u6536\u8d39\uff0c\u505c\u8f66\u538b\u529b\u4e0d\u9ad8\u4e8e 50 \u4e14\u516c\u4ea4\u8986\u76d6\u8fbe\u5230 35%", IsPolicyActive(CityPolicy.ParkingFees) && Metrics.ParkingPressure <= 50 && Metrics.TransitCoverage >= 35 ? 1 : 0, 1);
            AddMilestone("transit_spine", "公交骨架", "公共交通覆盖达到 45%", Metrics.TransitCoverage, 45);
            AddMilestone("transit_capacity", "公交运力", "公交利用率不高于 100% 且覆盖达到 45%", Metrics.TransitCoverage >= 45 && (Metrics.TransitLoad == 0 || Metrics.TransitUtilization <= 100) ? 1 : 0, 1);
            AddMilestone("transit_reliability", "\u516c\u4ea4\u53ef\u9760\u6027", "\u516c\u4ea4\u8986\u76d6\u8fbe\u5230 45%\uff0c\u53ef\u9760\u6027\u8fbe\u5230 70 \u4e14\u5019\u8f66\u538b\u529b\u4e0d\u9ad8\u4e8e 35", Metrics.TransitCoverage >= 45 && Metrics.TransitReliability >= 70 && Metrics.TransitWaitPressure <= 35 ? 1 : 0, 1);
            AddMilestone("metro_network", "\u8f68\u9053\u9aa8\u67b6", "\u5efa\u6210 1 \u5ea7\u8f68\u9053\u4ea4\u901a\u7ad9\u4e14\u516c\u4ea4\u6ee1\u8f7d\u7387\u4e0d\u9ad8\u4e8e 95%", CountBuildingsById("metro_station") > 0 && (Metrics.TransitLoad == 0 || Metrics.TransitUtilization <= 95) ? 1 : 0, 1);
            AddMilestone("regional_gateway", "\u533a\u57df\u95e8\u6237", "\u5efa\u6210 1 \u5ea7\u57ce\u9645\u67a2\u7ebd\u4e14\u5916\u90e8\u8fde\u63a5\u8fbe\u5230 60", CountBuildingsById("intercity_terminal") > 0 && Metrics.RegionalConnectivity >= 60 ? 1 : 0, 1);
            AddMilestone("vertical_growth", "街区成长", "培育 4 栋 2 级以上建筑", Metrics.UpgradedBuildings, 4);

            Metrics.ActiveObjective = new CityObjective
            {
                Title = "城市稳定运行",
                Hint = "继续扩路、划分区、补服务并保持预算健康",
                Progress = 1,
                Required = 1,
                Done = true
            };

            for (var i = 0; i < Metrics.Milestones.Count; i += 1)
            {
                if (!Metrics.Milestones[i].Done)
                {
                    Metrics.ActiveObjective = new CityObjective
                    {
                        Title = Metrics.Milestones[i].Title,
                        Hint = ObjectiveHintWithAdvice(Metrics.Milestones[i]),
                        Progress = Metrics.Milestones[i].Progress,
                        Required = Metrics.Milestones[i].Required,
                        Done = false
                    };
                    break;
                }
            }

            RefreshExpansionUnlockState();
            Metrics.CityLevelName = CityLevelNameForPopulation(Metrics.Population);
        }

        private void RefreshExpansionUnlockState()
        {
            if (Grid == null)
            {
                return;
            }

            if (IsMilestoneDone("compact_city"))
            {
                Metrics.LockedExpansionUnlocked = true;
            }

            Grid.ExpansionUnlocked = Metrics.LockedExpansionUnlocked;
        }

        private bool IsMilestoneDone(string milestoneId)
        {
            if (Metrics.Milestones == null)
            {
                return false;
            }

            for (var i = 0; i < Metrics.Milestones.Count; i += 1)
            {
                var milestone = Metrics.Milestones[i];
                if (milestone != null && milestone.Id == milestoneId)
                {
                    return milestone.Done;
                }
            }

            return false;
        }

        private string ObjectiveHintWithAdvice(CityMilestone milestone)
        {
            if (milestone == null)
            {
                return string.Empty;
            }

            var advice = ObjectiveAdviceFor(milestone.Id);
            return string.IsNullOrEmpty(advice) ? milestone.Hint : milestone.Hint + "  建议：" + advice;
        }

        private string ObjectiveAdviceFor(string milestoneId)
        {
            if (milestoneId == "road_grid")
            {
                return "铺路连接可开发地块";
            }

            if (milestoneId == "connected_grid")
            {
                return Metrics.DeadEndRoadTiles > 0 ? "打通断头路" : "补接路建筑";
            }

            if (milestoneId == "arterial_spine" || milestoneId == "traffic_flow")
            {
                return Metrics.IntersectionDelay > 45 ? "启用信号或补主干" : "升级主干并疏通瓶颈";
            }

            if (milestoneId == "road_care" || milestoneId == "safe_roads")
            {
                return Metrics.RoadMaintenanceCoverage < 55 ? "补道路养护覆盖" : "降事故风险";
            }

            if (milestoneId == "first_residents")
            {
                return "划住宅区并补水电";
            }

            if (milestoneId == "affordable_city")
            {
                return "补住房供给并控税率";
            }

            if (milestoneId == "livable_district")
            {
                return Metrics.ServiceGapPressure > 35 ? ServiceGapAdvice() : "降生活压力";
            }

            if (milestoneId == "zoned_growth")
            {
                return "划接路且适宜的分区";
            }

            if (milestoneId == "compact_city" || milestoneId == "quality_blocks" || milestoneId == "zoning_buffer" || milestoneId == "density_core" || milestoneId == "mixed_core")
            {
                return "提高适宜度并减少冲突";
            }

            if (milestoneId == "knowledge_economy" || milestoneId == "innovation_district" || milestoneId == "talent_pool" || milestoneId == "higher_education")
            {
                return "补高教/研发/通信";
            }

            if (milestoneId == "city_attraction" || milestoneId == "convention_draw")
            {
                return "补地标、公园和公交";
            }

            if (milestoneId == "walkable_city" || milestoneId == "smooth_commute" || milestoneId == "low_car_core")
            {
                return Metrics.TransitWaitPressure > 45 ? "补公交容量降候车" : "混合开发并降车依赖";
            }

            if (milestoneId == "parking_relief" || milestoneId == "parking_fees")
            {
                return "补公交和停车覆盖";
            }

            if (milestoneId == "green_city" || milestoneId == "healthy_city")
            {
                return Metrics.HealthRisk > 45 ? "补医疗/回收/污水" : "降污染和噪声";
            }

            if (milestoneId == "balanced_utilities" || milestoneId == "utility_resilience" || milestoneId == "renewable_power" || milestoneId == "water_sanitation" || milestoneId == "stormwater_ready")
            {
                return "补水电/污水/雨洪容量";
            }

            if (milestoneId == "maintenance_ready" || milestoneId == "service_budget_balance")
            {
                return Metrics.NetIncome < 0 ? "先控支出保现金" : "提高服务预算效率";
            }

            if (milestoneId == "service_core" || milestoneId == "service_capacity" || milestoneId == "balanced_services")
            {
                return ServiceGapAdvice();
            }

            if (milestoneId == "response_ready" || milestoneId == "disaster_preparedness")
            {
                return "补应急覆盖并疏通路网";
            }

            if (milestoneId == "health_net" || milestoneId == "regional_healthcare" || milestoneId == "healthcare_capacity")
            {
                return Metrics.PatientBacklog > 35 ? "扩医疗容量降积压" : "补诊所或医院覆盖";
            }

            if (milestoneId == "deathcare_ready")
            {
                return "建生命花园降死亡压力";
            }

            if (milestoneId == "education_net" || milestoneId == "education_capacity")
            {
                return Metrics.StudentBacklog > 35 ? "扩学位降积压" : "补学校/学院覆盖";
            }

            if (milestoneId == "safety_net" || milestoneId == "fire_resilience")
            {
                return Metrics.FireRisk > 40 ? "补消防响应降风险" : "补消防覆盖";
            }

            if (milestoneId == "secure_blocks" || milestoneId == "police_readiness")
            {
                return Metrics.CaseBacklog > 35 ? "扩警务容量降积案" : "补警务覆盖";
            }

            if (milestoneId == "clean_blocks" || milestoneId == "waste_capacity" || milestoneId == "waste_to_energy")
            {
                return "补回收容量和稳定度";
            }

            if (milestoneId == "freight_loop" || milestoneId == "freight_capacity" || milestoneId == "supply_chain_buffer" || milestoneId == "rail_freight_gateway" || milestoneId == "goods_market" || milestoneId == "local_supply" || milestoneId == "specialized_industry")
            {
                return "补货运、仓储和资源适配";
            }

            if (milestoneId == "connected_business" || milestoneId == "communication_capacity" || milestoneId == "mail_service")
            {
                return "补通信/邮政容量";
            }

            if (milestoneId == "healthy_budget" || milestoneId == "fiscal_credit" || milestoneId == "debt_service_control")
            {
                return Metrics.DebtPressure > 35 ? "先压债务和赤字" : "扩税基并控维护费";
            }

            if (milestoneId == "civic_administration" || milestoneId == "administration_capacity")
            {
                return Metrics.PolicyBacklog > 35 ? "补行政容量降积压" : "建市政厅提效率";
            }

            if (milestoneId == "policy_trial" || milestoneId == "complete_streets" || milestoneId == "signal_optimization" || milestoneId == "congestion_pricing")
            {
                return "点政策按钮看即时预览";
            }

            if (milestoneId == "transit_spine" || milestoneId == "transit_capacity" || milestoneId == "transit_reliability" || milestoneId == "metro_network" || milestoneId == "regional_gateway")
            {
                return Metrics.TransitUtilization > 100 ? "补公交/轨道容量" : "补公交覆盖和外部连接";
            }

            if (milestoneId == "vertical_growth")
            {
                return "提升地价、服务和公交";
            }

            return Metrics.Alerts.Count > 0 ? "先处理" + Metrics.Alerts[0] : "继续补齐当前短板";
        }

        private string ServiceGapAdvice()
        {
            if (Metrics.ServiceGapPressure > 0 && !string.IsNullOrEmpty(Metrics.ServiceGapFocus) && Metrics.ServiceGapFocus != "\u5747\u8861")
            {
                return "补" + Metrics.ServiceGapFocus + "覆盖";
            }

            return "补公园/医疗/教育";
        }

        private void AddMilestone(string id, string title, string hint, int progress, int required)
        {
            Metrics.Milestones.Add(new CityMilestone
            {
                Id = id,
                Title = title,
                Hint = hint,
                Progress = Math.Min(progress, required),
                Required = required,
                Done = progress >= required
            });
        }

        private int CountBuildingsById(string buildingId)
        {
            var count = 0;
            for (var i = 0; i < buildings.Count; i += 1)
            {
                if (buildings[i].ConfigId == buildingId)
                {
                    count += 1;
                }
            }

            return count;
        }

        private int CountConnectedBuildingsById(string buildingId)
        {
            var count = 0;
            for (var i = 0; i < buildings.Count; i += 1)
            {
                if (buildings[i].ConfigId == buildingId && !string.IsNullOrEmpty(buildings[i].ConnectedRoadId))
                {
                    count += 1;
                }
            }

            return count;
        }

        private int BalancedUtilityProgress()
        {
            var progress = 0;
            if (Metrics.PowerSupply >= Metrics.PowerDemand)
            {
                progress += 1;
            }

            if (Metrics.WaterSupply >= Metrics.WaterDemand)
            {
                progress += 1;
            }

            return progress;
        }

        private void RefreshUnlocks()
        {
            for (var i = 0; i < config.Buildings.Count; i += 1)
            {
                var definition = config.Buildings[i];
                if (string.IsNullOrEmpty(UnlockReason(definition)) && !Metrics.UnlockedBuildingIds.Contains(definition.Id))
                {
                    Metrics.UnlockedBuildingIds.Add(definition.Id);
                    if (Metrics.Day > 1 || Metrics.Population > 0)
                    {
                        AddCityEvent("\u89e3\u9501\u5efa\u7b51\uff1a" + definition.Name);
                    }
                }
            }
        }

        private string UnlockReason(BuildingDefinition definition)
        {
            if (Metrics.UnlockedBuildingIds.Contains(definition.Id))
            {
                return string.Empty;
            }

            if (definition.UnlockMinPopulation > 0 && Metrics.Population < definition.UnlockMinPopulation)
            {
                return "需要人口 " + definition.UnlockMinPopulation;
            }

            if (definition.UnlockMinCityScore > 0 && Metrics.CityScore < definition.UnlockMinCityScore)
            {
                return "需要评分 " + definition.UnlockMinCityScore;
            }

            return string.Empty;
        }

        public PlacedBuilding FindPlacedBuilding(string id)
        {
            for (var i = 0; i < buildings.Count; i += 1)
            {
                if (buildings[i].Id == id)
                {
                    return buildings[i];
                }
            }

            return null;
        }

        private void EnsureMetricLists()
        {
            if (Metrics.Demand == null)
            {
                Metrics.Demand = new DemandMetrics();
            }

            if (Metrics.ActiveObjective == null)
            {
                Metrics.ActiveObjective = new CityObjective();
            }

            if (Metrics.Milestones == null)
            {
                Metrics.Milestones = new List<CityMilestone>();
            }

            if (Metrics.Alerts == null)
            {
                Metrics.Alerts = new List<string>();
            }

            if (Metrics.RecentEvents == null)
            {
                Metrics.RecentEvents = new List<string>();
            }

            if (Metrics.UnlockedBuildingIds == null)
            {
                Metrics.UnlockedBuildingIds = new List<string>();
            }

            if (Metrics.ActivePolicies == null)
            {
                Metrics.ActivePolicies = new List<CityPolicy>();
            }
        }

        private static GridPos BuildingCenter(PlacedBuilding building)
        {
            return new GridPos(building.Pos.X + building.Size.W / 2, building.Pos.Y + building.Size.H / 2);
        }

        private static int NextIdAfter(string id)
        {
            const string prefix = "building-";
            if (string.IsNullOrEmpty(id) || !id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return 1;
            }

            int parsed;
            return int.TryParse(id.Substring(prefix.Length), out parsed) ? parsed + 1 : 1;
        }

        private static ConstructionPreview BlockedPreview(string title, string reason)
        {
            var preview = new ConstructionPreview
            {
                Title = title,
                Ok = false,
                ConfirmLabel = "不可执行"
            };
            preview.Lines.Add(reason);
            return preview;
        }

        private string CashShortfallReason(int cost)
        {
            return "\u73b0\u91d1\u4e0d\u8db3\uff0c\u7f3a " + Math.Max(0, cost - Metrics.Cash) + " / \u5f53\u524d " + Metrics.Cash;
        }

        private string BuildingCommandImpactLine(int serviceGapBefore, int happinessBefore)
        {
            return "\u57ce\u5e02\u53d8\u5316 \u670d\u52a1\u7f3a\u53e3 " + FormatSigned(serviceGapBefore - Metrics.ServiceGapPressure)
                + "  \u5e78\u798f " + FormatSigned(Metrics.Happiness - happinessBefore);
        }

        private string RoadCommandImpactLine(int connectivityBefore, int bottleneckBefore)
        {
            return "\u57ce\u5e02\u53d8\u5316 \u8fde\u901a " + FormatSigned(Metrics.RoadConnectivity - connectivityBefore)
                + "  \u74f6\u9888 " + FormatSigned(bottleneckBefore - Metrics.RoadBottleneckPressure);
        }

        private string ZoneCommandImpactLine(int demandBefore, int idleBefore)
        {
            return "\u57ce\u5e02\u53d8\u5316 \u9700\u6c42\u538b\u529b " + FormatSigned(demandBefore - Metrics.DemandUrgency)
                + "  \u95f2\u7f6e\u5730 " + FormatSigned(Metrics.IdleZoneTiles - idleBefore);
        }

        private string DemolishCommandImpactLine(int serviceGapBefore, int happinessBefore, int cashBefore, int capacityBefore)
        {
            return "\u57ce\u5e02\u53d8\u5316 \u670d\u52a1\u7f3a\u53e3 " + FormatSigned(serviceGapBefore - Metrics.ServiceGapPressure)
                + "  \u5e78\u798f " + FormatSigned(Metrics.Happiness - happinessBefore)
                + "  \u73b0\u91d1 " + FormatSigned(Metrics.Cash - cashBefore)
                + "  \u5bb9\u91cf " + FormatSigned(Metrics.HousingCapacity - capacityBefore);
        }

        private static string BuildingEffectLine(BuildingDefinition definition)
        {
            var effects = new List<string>();
            if (definition.Capacity > 0) effects.Add("住宅 +" + definition.Capacity);
            if (definition.Jobs > 0) effects.Add("岗位 +" + definition.Jobs);
            if (definition.PowerOutput > 0) effects.Add("供电 +" + definition.PowerOutput);
            if (definition.WaterOutput > 0) effects.Add("供水 +" + definition.WaterOutput);
            if (IsTransitBuilding(definition)) effects.Add("\u516c\u4ea4\u8fd0\u529b +" + TransitBuildingCapacity(definition));
            if (IsRegionalConnectionBuilding(definition)) effects.Add("\u5916\u90e8\u8fde\u63a5 +" + RegionalConnectionBuildingCapacity(definition));
            if (IsLogisticsBuilding(definition)) effects.Add("\u8d27\u8fd0\u8fd0\u529b +" + LogisticsBuildingCapacity(definition));
            if (IsResourceBuilding(definition)) effects.Add("\u672c\u5730\u4f9b\u7ed9 +" + ResourceBuildingSupply(definition));
            if (IsWarehouseBuilding(definition)) effects.Add("\u4ed3\u50a8 +" + WarehouseStorageCapacity(definition));
            if (IsFreightRailBuilding(definition)) effects.Add("\u94c1\u8def\u5bfc\u5165 +" + FreightRailImportSupply(definition));
            if (IsAttractionBuilding(definition)) effects.Add("\u5438\u5f15\u529b +" + Math.Max(12, definition.ServiceValue * 2 + definition.ServiceRadius));
            if (IsInnovationBuilding(definition)) effects.Add("\u521b\u65b0\u80fd\u529b +" + InnovationBuildingPotential(definition));
            if (IsHealthBuilding(definition)) effects.Add("\u533b\u7597\u5bb9\u91cf +" + HealthBuildingCapacity(definition));
            if (IsEducationBuilding(definition)) effects.Add("\u6559\u80b2\u5bb9\u91cf +" + EducationBuildingCapacity(definition));
            if (IsWasteBuilding(definition)) effects.Add("\u56de\u6536\u5bb9\u91cf +" + WasteBuildingCapacity(definition));
            if (IsWastewaterBuilding(definition)) effects.Add("\u6c61\u6c34\u5904\u7406 +" + WastewaterBuildingCapacity(definition));
            if (IsDeathcareBuilding(definition)) effects.Add("\u751f\u547d\u5173\u6000 +" + DeathcareBuildingCapacity(definition));
            if (IsAdministrationBuilding(definition)) effects.Add("\u884c\u653f +" + AdministrationBuildingCapacity(definition));
            if (IsShelterBuilding(definition)) effects.Add("\u707e\u5907 +" + DisasterPreparednessBuildingCapacity(definition));
            if (IsSecurityBuilding(definition)) effects.Add("\u8b66\u52a1\u5bb9\u91cf +" + SecurityBuildingCapacity(definition));
            if (IsParkingBuilding(definition)) effects.Add("\u505c\u8f66\u5bb9\u91cf +" + ParkingBuildingCapacity(definition));
            if (IsStormwaterBuilding(definition)) effects.Add("\u96e8\u6d2a\u5bb9\u91cf +" + StormwaterBuildingCapacity(definition));
            if (definition.ServiceRadius > 0) effects.Add("服务半径 " + definition.ServiceRadius);
            if (definition.ServiceValue > 0) effects.Add("地价 +" + definition.ServiceValue);
            if (definition.PowerUse > 0) effects.Add("用电 " + definition.PowerUse);
            if (definition.WaterUse > 0) effects.Add("用水 " + definition.WaterUse);
            if (definition.Pollution > 0) effects.Add("污染 " + definition.Pollution);
            if (definition.TrafficGeneration > 0) effects.Add("交通 " + definition.TrafficGeneration);
            return string.Join("  ", effects.ToArray());
        }

        private static List<GridPos> ManhattanLine(GridPos from, GridPos to)
        {
            var points = new List<GridPos>();
            var stepX = from.X <= to.X ? 1 : -1;
            var stepY = from.Y <= to.Y ? 1 : -1;

            for (var x = from.X; x != to.X + stepX; x += stepX)
            {
                points.Add(new GridPos(x, from.Y));
            }

            for (var y = from.Y + stepY; y != to.Y + stepY; y += stepY)
            {
                points.Add(new GridPos(to.X, y));
            }

            return points;
        }

        private static List<GridPos> RectPositions(GridPos from, GridPos to)
        {
            var points = new List<GridPos>();
            var minX = Math.Min(from.X, to.X);
            var maxX = Math.Max(from.X, to.X);
            var minY = Math.Min(from.Y, to.Y);
            var maxY = Math.Max(from.Y, to.Y);

            for (var y = minY; y <= maxY; y += 1)
            {
                for (var x = minX; x <= maxX; x += 1)
                {
                    points.Add(new GridPos(x, y));
                }
            }

            return points;
        }

        private static List<GridPos> UniquePositions(List<GridPos> points)
        {
            var seen = new HashSet<string>();
            var unique = new List<GridPos>();
            for (var i = 0; i < points.Count; i += 1)
            {
                var key = points[i].X + "," + points[i].Y;
                if (seen.Add(key))
                {
                    unique.Add(points[i]);
                }
            }

            return unique;
        }

        private static float UtilityEfficiency(int powerSupply, int powerDemand, int waterSupply, int waterDemand)
        {
            var powerFactor = powerDemand <= 0 ? 1f : Math.Min(1f, powerSupply * 1f / powerDemand);
            var waterFactor = waterDemand <= 0 ? 1f : Math.Min(1f, waterSupply * 1f / waterDemand);
            return Math.Max(0.35f, Math.Min(powerFactor, waterFactor));
        }

        private static int UtilityLoad(int powerDemand, int waterDemand)
        {
            return Math.Max(0, powerDemand + waterDemand);
        }

        private static int UtilityCapacity(int powerSupply, int waterSupply)
        {
            return Math.Max(0, powerSupply + waterSupply);
        }

        private static int UtilityUtilization(int load, int capacity)
        {
            if (load <= 0)
            {
                return 0;
            }

            if (capacity <= 0)
            {
                return 200;
            }

            return Math.Min(200, Math.Max(0, (int)Math.Round(load * 100.0 / capacity)));
        }

        private static int UtilityReliability(int powerSupply, int powerDemand, int waterSupply, int waterDemand)
        {
            var powerReliability = powerDemand <= 0 ? 100 : ClampToScore((int)Math.Round(powerSupply * 100.0 / powerDemand));
            var waterReliability = waterDemand <= 0 ? 100 : ClampToScore((int)Math.Round(waterSupply * 100.0 / waterDemand));
            return Math.Min(powerReliability, waterReliability);
        }

        private static int ClampToScore(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }

        private static string ZoneLabel(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return "住宅区";
            if (zone == ZoneType.Commercial) return "商业区";
            if (zone == ZoneType.Industrial) return "工业区";
            if (zone == ZoneType.Office) return "办公区";
            if (zone == ZoneType.MixedUse) return "混合用地区";
            if (zone == ZoneType.Civic) return "公共服务区";
            if (zone == ZoneType.Utility) return "基础设施区";
            return "未分区";
        }

        private static string CityLevelNameForPopulation(int population)
        {
            if (population >= 900) return "城市新区";
            if (population >= 420) return "成长小镇";
            if (population >= 120) return "活力社区";
            return "新生街区";
        }

        private enum ServiceAccessKind
        {
            Park,
            Health,
            Education,
            Safety,
            Security
        }

        private sealed class AutoDevelopmentCandidate
        {
            public ZoneType Zone;
            public string BuildingId = string.Empty;
            public int Demand;
        }
    }
}
