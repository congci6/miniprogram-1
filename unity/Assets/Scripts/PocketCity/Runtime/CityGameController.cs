using System.Collections.Generic;
using PocketCity.Core;
using PocketCity.Simulation;
using UnityEngine;

namespace PocketCity.Runtime
{
    public sealed class CityGameController : MonoBehaviour
    {
        [SerializeField] private CityConfig config;
        [SerializeField] private WeChatMiniGameBridge platformBridge;
        [SerializeField] private OverlayMode overlayMode = OverlayMode.Normal;
        [SerializeField] private bool paused;
        [SerializeField] private float simulationSpeed = 1f;

        private CitySimulationCore simulation;
        private ConstructionPreview currentPreview;
        private int commandFeedbackVersion;
        private bool lastCommandSucceeded;
        private string lastCommandFeedbackText = string.Empty;
        private string lastPublishedCityEvent = string.Empty;
        private int lastSettlementFeedbackDay = -1;
        private bool lastExpansionUnlocked;

        public CityMetrics Metrics
        {
            get { return simulation != null ? simulation.Metrics : null; }
        }

        public ConstructionPreview CurrentPreview
        {
            get { return currentPreview; }
        }

        public int CommandFeedbackVersion
        {
            get { return commandFeedbackVersion; }
        }

        public bool LastCommandSucceeded
        {
            get { return lastCommandSucceeded; }
        }

        public string LastCommandFeedbackText
        {
            get { return lastCommandFeedbackText; }
        }

        public OverlayMode OverlayMode
        {
            get { return overlayMode; }
        }

        public CityHudSnapshot HudSnapshot
        {
            get { return CityHudViewModel.FromMetrics(Metrics); }
        }

        public CityGridCore Grid
        {
            get { return simulation != null ? simulation.Grid : null; }
        }

        public IReadOnlyList<PlacedBuilding> Buildings
        {
            get { return simulation != null ? simulation.Buildings : null; }
        }

        public IReadOnlyList<RoadNode> Roads
        {
            get { return simulation != null ? simulation.Roads : null; }
        }

        public IReadOnlyList<CityPolicy> ActivePolicies
        {
            get { return simulation != null ? simulation.ActivePolicies : null; }
        }

        public CityTaxLevel TaxLevel
        {
            get { return simulation != null ? simulation.TaxLevel : CityTaxLevel.Normal; }
        }

        public CityServiceBudgetLevel ServiceBudgetLevel
        {
            get { return simulation != null ? simulation.ServiceBudgetLevel : CityServiceBudgetLevel.Standard; }
        }

        public bool Paused
        {
            get { return paused; }
        }

        public float SimulationSpeed
        {
            get { return simulationSpeed; }
        }

        public CitySimulationCore Simulation
        {
            get { return simulation; }
        }

        public void ResetCity()
        {
            if (simulation != null)
            {
                simulation.Reset();
                simulation.MarkMetricsDirty();
            }
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("CityGameController requires a CityConfig asset.");
                enabled = false;
                return;
            }

            if (platformBridge == null)
            {
                platformBridge = GetComponent<WeChatMiniGameBridge>();
            }

            simulation = new CitySimulationCore(config);

            // 初始化智能顾问系统
            CityHudViewModelSmartAdvisor.SetContextTracker(simulation.AdvisorContext);

            lastExpansionUnlocked = Metrics != null && Metrics.LockedExpansionUnlocked;
        }

        private void Update()
        {
            if (simulation == null)
            {
                return;
            }

            if (!paused)
            {
                var buildingCountBefore = Buildings != null ? Buildings.Count : 0;
                var metricsBefore = Metrics;
                var dayBefore = metricsBefore != null ? metricsBefore.Day : 0;
                var expansionUnlockedBefore = metricsBefore != null && metricsBefore.LockedExpansionUnlocked;
                var settlementBefore = PolicyImpactPreview.Capture(metricsBefore);
                var recentEventBefore = LatestRecentEvent();
                simulation.Tick(Time.deltaTime * Mathf.Max(0f, simulationSpeed));
                var buildingCountAfter = Buildings != null ? Buildings.Count : 0;
                var addedBuildings = buildingCountAfter - buildingCountBefore;
                var metricsAfter = Metrics;
                var dayAfter = metricsAfter != null ? metricsAfter.Day : dayBefore;
                var expansionUnlockedAfter = metricsAfter != null && metricsAfter.LockedExpansionUnlocked;
                var recentEventAfter = LatestRecentEvent();
                var settlementAfter = PolicyImpactPreview.Capture(metricsAfter);
                if (expansionUnlockedAfter && !expansionUnlockedBefore && !lastExpansionUnlocked)
                {
                    lastExpansionUnlocked = true;
                    lastPublishedCityEvent = recentEventAfter;
                    PublishHudFeedback(BuildCityOperationsHudSummary(BuildCityEventLabel("\u65b0\u533a\u5f00\u653e"), settlementBefore, settlementAfter, metricsAfter, true), true);
                    return;
                }

                lastExpansionUnlocked = expansionUnlockedAfter;
                if (!string.IsNullOrEmpty(recentEventAfter) && recentEventAfter != recentEventBefore && recentEventAfter != lastPublishedCityEvent)
                {
                    lastPublishedCityEvent = recentEventAfter;
                    var eventLabel = addedBuildings > 0
                        ? CompactCommandPart(recentEventAfter + " +" + addedBuildings + "\u680b", 10)
                        : CompactCommandPart(recentEventAfter, 10);
                    PublishHudFeedback(BuildCityOperationsHudSummary(BuildCityEventLabel(eventLabel), settlementBefore, settlementAfter, metricsAfter, true), true);
                    return;
                }

                if (addedBuildings > 0)
                {
                    PublishHudFeedback(BuildCityOperationsHudSummary(BuildCityEventLabel("\u5165\u9a7b+" + addedBuildings + "\u680b"), settlementBefore, settlementAfter, metricsAfter, true), true);
                    lastPublishedCityEvent = recentEventAfter;
                    return;
                }

                if (ShouldPublishSettlementFeedback(dayBefore, dayAfter, metricsAfter))
                {
                    lastSettlementFeedbackDay = dayAfter;
                    PublishHudFeedback(BuildSettlementFeedback(dayAfter, settlementBefore, settlementAfter, metricsAfter), SettlementFeedbackIsPositive(metricsAfter));
                }
            }
        }

        private string LatestRecentEvent()
        {
            var metrics = Metrics;
            return metrics != null && metrics.RecentEvents != null && metrics.RecentEvents.Count > 0
                ? metrics.RecentEvents[0]
                : string.Empty;
        }

        private bool ShouldPublishSettlementFeedback(int dayBefore, int dayAfter, CityMetrics metrics)
        {
            if (metrics == null || dayAfter <= dayBefore || dayAfter == lastSettlementFeedbackDay)
            {
                return false;
            }

            var budgetPeriod = config != null ? Mathf.Max(1, config.DaysPerBudgetPeriod) : 30;
            if (dayAfter <= 2 || dayAfter % budgetPeriod == 0)
            {
                return true;
            }

            if (dayAfter / 5 != dayBefore / 5)
            {
                return true;
            }

            return (metrics.ForecastRisk >= 72 || metrics.NetIncome < 0 || metrics.ServiceGapPressure >= 68 || metrics.RoadBottleneckPressure >= 68)
                && (lastSettlementFeedbackDay < 0 || dayAfter - lastSettlementFeedbackDay >= 3);
        }

        private static bool SettlementFeedbackIsPositive(CityMetrics metrics)
        {
            return metrics != null
                && metrics.ForecastRisk < 70
                && metrics.NetIncome >= -250
                && metrics.Happiness >= 45;
        }

        private static string BuildSettlementFeedback(int day, PolicyImpactPreview before, PolicyImpactPreview after, CityMetrics metrics)
        {
            return BuildCityOperationsHudSummary(BuildSettlementFocusLabel(day, metrics), before, after, metrics, true);
        }

        private static string BuildSettlementFocusLabel(int day, CityMetrics metrics)
        {
            var label = "\u56de\u5408 D" + day;
            if (metrics == null)
            {
                return label;
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && !objective.Done && objective.Required > 0)
            {
                var required = Mathf.Max(1, objective.Required);
                var progress = Mathf.Clamp(objective.Progress, 0, required);
                return label + " \u4efb" + progress + "/" + required;
            }

            return metrics.DemandUrgency >= 60 ? label + " \u9700" + metrics.DemandUrgency : label;
        }

        private string BuildCurrentOperationsHudFeedback(string label)
        {
            var snapshot = PolicyImpactPreview.Capture(Metrics);
            return BuildCityOperationsHudSummary(label, snapshot, snapshot, Metrics, true);
        }

        private static string BuildCityOperationsHudSummary(string label, PolicyImpactPreview before, PolicyImpactPreview after, CityMetrics metrics, bool success)
        {
            var summaryLabel = BuildOperationsLogLabel(label, success);
            var population = metrics != null ? metrics.Population : after.Population;
            return CompactCommandFeedbackText(summaryLabel
                + " " + BuildOperationsBriefLine(metrics, success)
                + "\n" + BuildOperationsOutcomeLine(before, after)
                + " \u73b0" + after.Cash
                + " \u6c11" + population);
        }

        private static string BuildOperationsLogLabel(string label, bool success)
        {
            var summaryLabel = string.IsNullOrEmpty(label) ? "\u57ce\u8fd0" : label;
            return (success ? "\u57ce\u5fd7 " : "\u8b66 ") + summaryLabel;
        }

        private static string BuildCityEventLabel(string label)
        {
            return string.IsNullOrEmpty(label) ? "\u4e8b" : "\u4e8b " + label;
        }

        private static string BuildOperationsBriefLine(CityMetrics metrics, bool success)
        {
            return "\u72b6:" + CompactCommandPart(BuildOperationsIssue(metrics, success), 5)
                + " \u56e0:" + CompactCommandPart(BuildOperationsCause(metrics, success), 6)
                + " \u505a:" + CompactCommandPart(BuildOperationsAdvice(metrics, success), 7);
        }

        private static string BuildOperationsIssue(CityMetrics metrics, bool success)
        {
            if (metrics == null)
            {
                return "\u5f85\u6570\u636e";
            }

            if (!success)
            {
                return BlockedCommandIssue(metrics);
            }

            if (metrics.ForecastRisk >= 70)
            {
                return "\u9669\u504f\u9ad8";
            }

            if (metrics.NetIncome < 0 || metrics.BudgetStress >= 60)
            {
                return "\u8d22\u538b";
            }

            if (metrics.RoadBottleneckPressure >= 65 || metrics.Congestion >= 68)
            {
                return "\u8def\u74f6\u9888";
            }

            if (metrics.ServiceGapPressure >= 58 || metrics.ServiceCoverage < 55)
            {
                return "\u670d\u7f3a";
            }

            if (metrics.DemandUrgency >= 60)
            {
                return "\u9700\u538b";
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5f85\u5347";
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && !objective.Done && objective.Required > 0)
            {
                return "\u4efb\u63a8\u8fdb";
            }

            return "\u57ce\u7a33";
        }

        private static string BuildOperationsCause(CityMetrics metrics, bool success)
        {
            if (metrics == null)
            {
                return "\u6570\u636e\u5f85";
            }

            if (!success)
            {
                return BlockedCommandIssue(metrics);
            }

            if (metrics.ForecastRisk >= 70)
            {
                return PreferText(metrics.ForecastFocus, metrics.BudgetDriver, "\u7efc\u5408\u9669");
            }

            if (metrics.NetIncome < 0 || metrics.BudgetStress >= 60)
            {
                return PreferText(metrics.BudgetDriver, metrics.BudgetFocus, "\u8d22\u7ed3\u6784");
            }

            if (metrics.RoadBottleneckPressure >= 65 || metrics.Congestion >= 68)
            {
                return PreferText(metrics.RoadHierarchyDriver, metrics.RoadHierarchyFocus, "\u8def\u74f6\u9888");
            }

            if (metrics.ServiceGapPressure >= 58 || metrics.ServiceCoverage < 55)
            {
                return PreferText(metrics.ServiceGapAdvisorDriver, metrics.ServiceGapAdvisorFocus, "\u670d\u8986\u76d6");
            }

            if (metrics.DemandUrgency >= 60)
            {
                return PreferText(metrics.DemandDriver, metrics.DemandFocus, "\u9700\u9a71\u52a8");
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return PreferText(metrics.BuildingUpgradeReadinessDriver, metrics.BuildingUpgradeReadinessFocus, "\u5347\u6761\u4ef6");
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && !objective.Done && objective.Required > 0)
            {
                return "\u4efb";
            }

            return "\u6307\u6807\u7a33";
        }

        private static string BuildOperationsOutcomeLine(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            return "\u5956:" + BuildPrimaryBenefit(before, after)
                + " \u9669:" + BuildPrimaryRisk(before, after);
        }

        private static string BuildPrimaryBenefit(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            var roadDelta = DominantRoadDelta(before, after);
            if (after.ServiceGapPressure < before.ServiceGapPressure) return BuildDeltaToken("\u670d", after.ServiceGapPressure - before.ServiceGapPressure);
            if (roadDelta < 0) return BuildDeltaToken("\u8def", roadDelta);
            if (after.ForecastRisk < before.ForecastRisk) return BuildDeltaToken("\u9669", after.ForecastRisk - before.ForecastRisk);
            if (after.ParkingPressure < before.ParkingPressure) return BuildDeltaToken("\u505c", after.ParkingPressure - before.ParkingPressure);
            if (after.AccidentRisk < before.AccidentRisk) return BuildDeltaToken("\u4e8b", after.AccidentRisk - before.AccidentRisk);
            if (after.FloodRisk < before.FloodRisk) return BuildDeltaToken("\u6d9d", after.FloodRisk - before.FloodRisk);
            if (after.DemandUrgency < before.DemandUrgency) return BuildDeltaToken("\u9700", after.DemandUrgency - before.DemandUrgency);
            if (after.Happiness > before.Happiness) return BuildDeltaToken("\u5e78", after.Happiness - before.Happiness);
            if (after.Population > before.Population) return BuildDeltaToken("\u6c11", after.Population - before.Population);
            if (after.NetIncome > before.NetIncome) return BuildDeltaToken("\u6536", after.NetIncome - before.NetIncome);
            if (after.Cash > before.Cash) return BuildDeltaToken("\u73b0", after.Cash - before.Cash);
            return "\u7a33";
        }

        private static string BuildPrimaryRisk(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            var roadDelta = DominantRoadDelta(before, after);
            if (after.ForecastRisk > before.ForecastRisk) return BuildDeltaToken("\u9669", after.ForecastRisk - before.ForecastRisk);
            if (after.ServiceGapPressure > before.ServiceGapPressure) return BuildDeltaToken("\u670d", after.ServiceGapPressure - before.ServiceGapPressure);
            if (roadDelta > 0) return BuildDeltaToken("\u8def", roadDelta);
            if (after.DemandUrgency > before.DemandUrgency) return BuildDeltaToken("\u9700", after.DemandUrgency - before.DemandUrgency);
            if (after.NetIncome < before.NetIncome) return BuildDeltaToken("\u6536", after.NetIncome - before.NetIncome);
            if (after.DebtPressure > before.DebtPressure) return BuildDeltaToken("\u503a", after.DebtPressure - before.DebtPressure);
            if (after.PolicyBacklog > before.PolicyBacklog) return BuildDeltaToken("\u538b", after.PolicyBacklog - before.PolicyBacklog);
            if (after.Cash < before.Cash) return BuildDeltaToken("\u73b0", after.Cash - before.Cash);
            return "\u63a7";
        }

        private static int DominantRoadDelta(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            var congestionDelta = after.Congestion - before.Congestion;
            var bottleneckDelta = after.RoadBottleneckPressure - before.RoadBottleneckPressure;
            return Mathf.Abs(bottleneckDelta) >= Mathf.Abs(congestionDelta) ? bottleneckDelta : congestionDelta;
        }

        private static string BuildDeltaToken(string label, int delta)
        {
            return label + FormatSigned(delta);
        }

        private static string BuildOperationsAdvice(CityMetrics metrics, bool success)
        {
            if (metrics == null)
            {
                return "\u5de1\u68c0";
            }

            if (!success)
            {
                return BlockedCommandAdvice(metrics);
            }

            if (metrics.ForecastRisk >= 70)
            {
                return PreferText(metrics.ForecastAction, metrics.ForecastFocus, "\u5148\u964d\u9669");
            }

            if (metrics.RoadBottleneckPressure >= 65 || metrics.Congestion >= 68)
            {
                return PreferText(metrics.RoadHierarchyAction, metrics.CommuteCorridorAction, "\u8865\u4e3b\u8def");
            }

            if (metrics.ServiceGapPressure >= 58 || metrics.ServiceCoverage < 55)
            {
                return PreferText(metrics.ServiceGapAdvisorAction, metrics.ServiceGapFocus, "\u8865\u7f3a\u53e3");
            }

            if (metrics.DemandUrgency >= 60)
            {
                return PreferText(metrics.DemandAction, metrics.DemandFocus, "\u8865\u5206\u533a");
            }

            if (metrics.NetIncome < 0)
            {
                return PreferText(metrics.BudgetAction, metrics.BudgetFocus, "\u63a7\u9884\u7b97/\u6269\u7a0e\u57fa");
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7" + metrics.BuildingUpgradeReadyCount;
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && !objective.Done && objective.Required > 0)
            {
                return "\u505a\u4efb";
            }

            return "\u5de1\u68c0";
        }

        private static string FormatDeltaSuffix(int value)
        {
            return value == 0 ? string.Empty : "(" + FormatSigned(value) + ")";
        }

        public string ExportSaveJson()
        {
            return simulation == null ? string.Empty : JsonUtility.ToJson(simulation.CreateSaveData());
        }

        public bool ImportSaveJson(string json)
        {
            if (simulation == null || string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                var save = JsonUtility.FromJson<CitySaveData>(json);
                var importedSimulation = new CitySimulationCore(config);
                var imported = importedSimulation.ApplySaveData(save);
                if (imported)
                {
                    simulation = importedSimulation;
                    currentPreview = null;
                    lastExpansionUnlocked = Metrics != null && Metrics.LockedExpansionUnlocked;
                }

                return imported;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("ImportSaveJson failed: " + error.Message);
                return false;
            }
        }

        public void TogglePause()
        {
            paused = !paused;
            PublishPauseFeedback();
        }

        public void SetPaused(bool value)
        {
            paused = value;
            PublishPauseFeedback();
        }

        public void CycleSimulationSpeed()
        {
            if (paused)
            {
                paused = false;
                simulationSpeed = 1f;
                PublishHudFeedback(BuildCurrentOperationsHudFeedback(BuildTimeControlLogLabel(paused, simulationSpeed)), true);
                return;
            }

            if (simulationSpeed < 1.5f)
            {
                simulationSpeed = 2f;
                PublishHudFeedback(BuildCurrentOperationsHudFeedback(BuildTimeControlLogLabel(paused, simulationSpeed)), true);
            }
            else if (simulationSpeed < 3.5f)
            {
                simulationSpeed = 4f;
                PublishHudFeedback(BuildCurrentOperationsHudFeedback(BuildTimeControlLogLabel(paused, simulationSpeed)), true);
            }
            else
            {
                paused = true;
                simulationSpeed = 1f;
                PublishHudFeedback(BuildCurrentOperationsHudFeedback(BuildTimeControlLogLabel(paused, simulationSpeed)), true);
            }
        }

        private void PublishPauseFeedback()
        {
            PublishHudFeedback(BuildCurrentOperationsHudFeedback(BuildTimeControlLogLabel(paused, simulationSpeed)), true);
        }

        private static string BuildTimeControlLogLabel(bool isPaused, float speed)
        {
            if (isPaused)
            {
                return "\u65f6 \u6682\u505c";
            }

            return "\u65f6 x" + Mathf.Max(1, Mathf.RoundToInt(speed));
        }

        public bool IsPolicyActive(CityPolicy policy)
        {
            return simulation != null && simulation.IsPolicyActive(policy);
        }

        public void TogglePolicy(CityPolicy policy)
        {
            if (simulation != null)
            {
                var wasActive = simulation.IsPolicyActive(policy);
                var before = PolicyImpactPreview.Capture(simulation.Metrics);
                simulation.TogglePolicy(policy);
                var after = PolicyImpactPreview.Capture(simulation.Metrics);
                currentPreview = BuildPolicyImpactPreview(policy, !wasActive, before, after, simulation.Metrics);
                PlayCityCommandFeedback(true);
            }
        }

        public void CycleTaxLevel()
        {
            if (simulation != null)
            {
                var before = PolicyImpactPreview.Capture(simulation.Metrics);
                simulation.CycleTaxLevel();
                var after = PolicyImpactPreview.Capture(simulation.Metrics);
                currentPreview = BuildManagementImpactPreview("\u7a0e\u52a1\u9762\u677f", TaxLevelLabel(simulation.TaxLevel), before, after, simulation.Metrics);
                PlayCityCommandFeedback(true);
            }
        }

        public void CycleServiceBudgetLevel()
        {
            if (simulation != null)
            {
                var before = PolicyImpactPreview.Capture(simulation.Metrics);
                simulation.CycleServiceBudgetLevel();
                var after = PolicyImpactPreview.Capture(simulation.Metrics);
                currentPreview = BuildManagementImpactPreview("\u670d\u52a1\u9884\u7b97", ServiceBudgetLabel(simulation.ServiceBudgetLevel), before, after, simulation.Metrics);
                PlayCityCommandFeedback(true);
            }
        }

        public bool IssueMunicipalBond()
        {
            if (simulation == null)
            {
                PlayCityCommandFeedback(false);
                return false;
            }

            var before = PolicyImpactPreview.Capture(simulation.Metrics);
            var issued = simulation.IssueMunicipalBond();
            var after = PolicyImpactPreview.Capture(simulation.Metrics);
            currentPreview = issued
                ? BuildManagementImpactPreview("\u503a\u52a1\u9762\u677f", "\u503a\u5238\u5df2\u5165\u8d26", before, after, simulation.Metrics)
                : BuildManagementBlockedPreview("\u503a\u52a1\u9762\u677f", before, simulation.Metrics);
            PlayCityCommandFeedback(issued);
            return issued;
        }

        public ConstructionPreview PreviewBuilding(string buildingId, int gridX, int gridY)
        {
            currentPreview = simulation.PreviewBuilding(buildingId, new GridPos(gridX, gridY));
            return currentPreview;
        }

        public bool ConfirmBuilding(string buildingId, int gridX, int gridY)
        {
            ConstructionPreview preview;
            var before = PolicyImpactPreview.Capture(Metrics);
            var placed = simulation.TryPlaceBuilding(buildingId, new GridPos(gridX, gridY), out preview);
            AddCommandCityDeltaLine(preview, before, PolicyImpactPreview.Capture(Metrics), placed);
            currentPreview = preview;
            PlayCityCommandFeedback(placed);
            return placed;
        }

        public ConstructionPreview PreviewRoad(int fromX, int fromY, int toX, int toY)
        {
            currentPreview = simulation.PreviewRoad(new GridPos(fromX, fromY), new GridPos(toX, toY));
            return currentPreview;
        }

        public bool ConfirmRoad(int fromX, int fromY, int toX, int toY)
        {
            ConstructionPreview preview;
            var before = PolicyImpactPreview.Capture(Metrics);
            var built = simulation.TryBuildRoad(new GridPos(fromX, fromY), new GridPos(toX, toY), out preview);
            AddCommandCityDeltaLine(preview, before, PolicyImpactPreview.Capture(Metrics), built);
            currentPreview = preview;
            PlayCityCommandFeedback(built);
            return built;
        }

        public ConstructionPreview PreviewRoadUpgrade(int gridX, int gridY)
        {
            currentPreview = simulation.PreviewRoadUpgrade(new GridPos(gridX, gridY));
            return currentPreview;
        }

        public bool ConfirmRoadUpgrade(int gridX, int gridY)
        {
            ConstructionPreview preview;
            var before = PolicyImpactPreview.Capture(Metrics);
            var upgraded = simulation.TryUpgradeRoad(new GridPos(gridX, gridY), out preview);
            AddCommandCityDeltaLine(preview, before, PolicyImpactPreview.Capture(Metrics), upgraded);
            currentPreview = preview;
            PlayCityCommandFeedback(upgraded);
            return upgraded;
        }

        public ConstructionPreview PreviewZone(int fromX, int fromY, int toX, int toY, ZoneType zone)
        {
            currentPreview = simulation.PreviewZone(new GridPos(fromX, fromY), new GridPos(toX, toY), zone);
            return currentPreview;
        }

        public bool ConfirmZone(int fromX, int fromY, int toX, int toY, ZoneType zone)
        {
            ConstructionPreview preview;
            var before = PolicyImpactPreview.Capture(Metrics);
            var zoned = simulation.TrySetZone(new GridPos(fromX, fromY), new GridPos(toX, toY), zone, out preview);
            AddCommandCityDeltaLine(preview, before, PolicyImpactPreview.Capture(Metrics), zoned);
            currentPreview = preview;
            PlayCityCommandFeedback(zoned);
            return zoned;
        }

        public ConstructionPreview PreviewDemolish(int gridX, int gridY)
        {
            currentPreview = simulation.PreviewDemolish(new GridPos(gridX, gridY));
            return currentPreview;
        }

        public bool ConfirmDemolish(int gridX, int gridY)
        {
            ConstructionPreview preview;
            var before = PolicyImpactPreview.Capture(Metrics);
            var demolished = simulation.TryDemolishAt(new GridPos(gridX, gridY), out preview);
            AddCommandCityDeltaLine(preview, before, PolicyImpactPreview.Capture(Metrics), demolished);
            currentPreview = preview;
            PlayCityCommandFeedback(demolished);
            return demolished;
        }

        private void PlayCityCommandFeedback(bool success)
        {
            // COMMAND_FEEDBACK_PULSE exposes command results to the runtime HUD even without a platform bridge.
            lastCommandSucceeded = success;
            lastCommandFeedbackText = BuildCommandFeedbackText(currentPreview, success, Metrics);
            commandFeedbackVersion += 1;

            // WECHAT_SAFE_LIFECYCLE_FEEDBACK keeps command feedback optional and platform-safe.
            if (platformBridge == null)
            {
                return;
            }

            if (success)
            {
                platformBridge.VibrateSuccess();
            }
            else
            {
                platformBridge.VibrateWarning();
            }
        }

        public void PublishHudFeedback(string text, bool success)
        {
            // TOOL_SWITCH_HUD_PULSE updates the HUD without invoking platform vibration.
            lastCommandSucceeded = success;
            lastCommandFeedbackText = CompactCommandFeedbackText(text);
            commandFeedbackVersion += 1;
        }

        private static string BuildCommandFeedbackText(ConstructionPreview preview, bool success, CityMetrics metrics)
        {
            // COMMAND_FEEDBACK_DETAIL_SUMMARY keeps the HUD pulse tied to the command that was just clicked.
            if (preview == null)
            {
                return CompactCommandFeedbackText(success
                    ? "\u5b8c\u6210  " + BuildPlannerNextStep(metrics, true)
                    : "\u53d7\u963b  " + BuildPlannerNextStep(metrics, false));
            }

            var title = string.IsNullOrEmpty(preview.Title) ? (success ? "\u5b8c\u6210" : "\u53d7\u963b") : preview.Title;
            var action = success && !string.IsNullOrEmpty(preview.ConfirmLabel) ? preview.ConfirmLabel : title;
            if (IsCityManagementFeedback(preview))
            {
                var snapshot = PolicyImpactPreview.Capture(metrics);
                var label = string.IsNullOrEmpty(preview.ConfirmLabel) ? action : preview.ConfirmLabel;
                return BuildCityOperationsHudSummary(label, snapshot, snapshot, metrics, success);
            }

            var detail = BuildCommandFeedbackDetail(preview);
            var objective = success ? BuildObjectiveProgressCue(metrics) : string.Empty;
            var planner = BuildPlannerNextStep(metrics, success);
            var text = string.IsNullOrEmpty(detail) ? action : action + "  " + detail;
            if (!string.IsNullOrEmpty(objective))
            {
                text += "  " + objective;
            }

            var mobileReward = success ? BuildMobileOrderRewardCue(metrics) : string.Empty;
            if (!string.IsNullOrEmpty(mobileReward))
            {
                text += "  " + mobileReward;
            }

            return CompactCommandFeedbackText(string.IsNullOrEmpty(planner) ? text : text + "  " + planner);
        }

        private static bool IsCityManagementFeedback(ConstructionPreview preview)
        {
            return preview != null
                && (preview.Title == "\u57ce\u5e02\u7ba1\u7406\u53cd\u9988"
                    || preview.Title == "\u653f\u7b56\u6548\u679c\u53cd\u9988"
                    || preview.Title == "\u7ba1\u7406\u56de\u6267"
                    || preview.Title == "\u653f\u7b56\u56de\u6267");
        }

        private static string BuildObjectiveProgressCue(CityMetrics metrics)
        {
            var objective = metrics != null ? metrics.ActiveObjective : null;
            if (objective == null || objective.Required <= 0)
            {
                return string.Empty;
            }

            var required = Mathf.Max(1, objective.Required);
            var progress = Mathf.Clamp(objective.Progress, 0, required);
            var title = CompactCommandPart(objective.Title, 5);
            return (objective.Done ? "\u5956 " : "\u4efb ")
                + progress + "/" + required
                + (string.IsNullOrEmpty(title) ? string.Empty : " " + title);
        }

        private static string BuildMobileOrderRewardCue(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && objective.Required > 0)
            {
                if (objective.Done)
                {
                    return "\u8ba2\u5355\u5b8c\u6210 \u53ef\u9886\u5956\u52b1";
                }

                var required = Mathf.Max(1, objective.Required);
                var progress = Mathf.Clamp(objective.Progress, 0, required);
                var remaining = Mathf.Max(0, required - progress);
                if (remaining <= 3)
                {
                    return "\u8ba2\u5355\u5feb\u5b8c\u6210 \u8fd8\u5dee" + remaining;
                }
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u53ef\u5347\u7ea7 +" + metrics.BuildingUpgradeReadyCount;
            }

            if (metrics.DemandUrgency >= 60)
            {
                return "\u65b0\u8ba2\u5355 \u54cd\u5e94\u9700\u6c42";
            }

            return string.Empty;
        }

        private static string BuildCommandFeedbackDetail(ConstructionPreview preview)
        {
            var reason = !string.IsNullOrEmpty(preview.SiteDiagnosis)
                ? preview.SiteDiagnosis
                : (preview.Lines != null && preview.Lines.Count > 0 ? preview.Lines[0] : string.Empty);
            var economy = FirstCommandEconomyLine(preview);
            if (string.IsNullOrEmpty(economy) || economy == reason)
            {
                return reason;
            }

            return string.IsNullOrEmpty(reason) ? economy : economy + "  " + reason;
        }

        private static void AddCommandCityDeltaLine(ConstructionPreview preview, PolicyImpactPreview before, PolicyImpactPreview after, bool success)
        {
            if (!success || preview == null || preview.Lines == null)
            {
                return;
            }

            preview.Lines.Insert(0, BuildCommandCityDeltaLine(before, after));
            preview.Lines.Insert(0, BuildOperationsOutcomeLine(before, after));
        }

        private static string BuildCommandCityDeltaLine(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            // CITY_COMMAND_DELTA_RECEIPT makes every build action read like a city simulation consequence.
            return "\u8d26 \u73b0" + FormatSigned(after.Cash - before.Cash)
                + " \u6536" + FormatSigned(after.NetIncome - before.NetIncome)
                + " \u9669" + FormatSigned(after.ForecastRisk - before.ForecastRisk)
                + " \u8def" + FormatSigned(after.RoadBottleneckPressure - before.RoadBottleneckPressure)
                + " \u670d" + FormatSigned(after.ServiceGapPressure - before.ServiceGapPressure)
                + " \u9700" + FormatSigned(after.DemandUrgency - before.DemandUrgency);
        }

        private static string FirstCommandEconomyLine(ConstructionPreview preview)
        {
            if (preview == null || preview.Lines == null)
            {
                return string.Empty;
            }

            for (var i = 0; i < preview.Lines.Count; i += 1)
            {
                var line = preview.Lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.IndexOf("\u5956:", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u9669:", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u6536\u76ca", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u98ce\u9669", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u82b1\u8d39", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u57ce\u5e02\u53d8\u5316", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u6307\u6807\u8d26\u672c", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u8d26 ", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u65e5\u5fd7\u6458\u8981", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u8d44\u91d1\u6d41", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u8d22\u653f\u9762", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u503a\u52a1\u9762", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u8fd4\u8fd8", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u65b0\u5efa", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u7ef4\u62a4", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u73b0\u91d1", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u6708\u6536\u652f", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u6708\u5ea6\u6536\u652f", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u503a\u52a1", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u8d22\u653f", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u653f\u7b56\u6536\u652f", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u653f\u7b56\u6210\u672c", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u9884\u7b97\u652f\u51fa", System.StringComparison.Ordinal) >= 0
                    || line.IndexOf("\u73b0\u91d1\u4e0d\u8db3", System.StringComparison.Ordinal) >= 0)
                {
                    return line;
                }
            }

            return string.Empty;
        }

        private static string BuildPlannerNextStep(CityMetrics metrics, bool success)
        {
            // CITY_SKYLINES_STYLE_COMMAND_ADVISOR adds a compact next-action cue after each build command.
            if (metrics == null)
            {
                return string.Empty;
            }

            if (!success)
            {
                return "\u56e0 " + CompactCommandPart(BlockedCommandIssue(metrics), 6)
                    + " \u505a " + CompactCommandPart(BlockedCommandAdvice(metrics), 8);
            }

            if (metrics.ForecastRisk >= 70)
            {
                return "\u505a \u964d\u9669:" + CompactCommandPart(PreferText(metrics.ForecastAction, metrics.ForecastFocus, "\u5148\u7a33\u5b9a\u8fd0\u8425"), 8);
            }

            if (metrics.RoadBottleneckPressure >= 65 || metrics.Congestion >= 68)
            {
                return "\u505a \u758f\u901a:" + CompactCommandPart(PreferText(metrics.RoadHierarchyAction, metrics.CommuteCorridorAction, "\u8865\u4e3b\u8def"), 8);
            }

            if (metrics.ServiceGapPressure >= 58 || metrics.ServiceCoverage < 55)
            {
                return "\u505a \u8865\u670d:" + CompactCommandPart(PreferText(metrics.ServiceGapAdvisorAction, metrics.ServiceGapFocus, "\u8865\u7f3a\u53e3"), 8);
            }

            if (metrics.DemandUrgency >= 60)
            {
                return "\u505a \u54cd\u9700:" + CompactCommandPart(PreferText(metrics.DemandAction, metrics.DemandFocus, "\u8865\u5206\u533a"), 8);
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u505a \u5347\u7ea7 " + metrics.BuildingUpgradeReadyCount;
            }

            if (metrics.NetIncome < 0)
            {
                return "\u505a \u63a7\u9884:" + CompactCommandPart(PreferText(metrics.BudgetAction, metrics.BudgetFocus, "\u51cf\u8d64\u5b57"), 8);
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && !objective.Done && objective.Required > 0)
            {
                return "\u4efb " + objective.Progress + "/" + objective.Required + " " + CompactCommandPart(objective.Title, 7);
            }

            return "\u57ce\u7a33";
        }

        private static string BlockedCommandAdvice(CityMetrics metrics)
        {
            if (metrics.Cash < 500)
            {
                return "\u6269\u7a0e\u57fa/\u7f13\u5efa";
            }

            if (metrics.DebtPressure >= 60)
            {
                return "\u5148\u63a7\u503a";
            }

            if (metrics.RoadConnectivity < 55 || metrics.RoadBottleneckPressure >= 65)
            {
                return "\u8865\u4e34\u8def\u4e3b\u9053";
            }

            if (metrics.ServiceGapPressure >= 58)
            {
                return "\u8865\u670d\u8986\u76d6";
            }

            if (metrics.UtilityReliability < 70)
            {
                return "\u5148\u8865\u6c34\u7535";
            }

            return "\u6362\u4e34\u8def\u7a7a\u5730/\u7f29\u8303\u56f4";
        }

        private static string BlockedCommandIssue(CityMetrics metrics)
        {
            if (metrics.Cash < 500)
            {
                return "\u73b0\u91d1\u4e0d\u8db3";
            }

            if (metrics.DebtPressure >= 60)
            {
                return "\u503a\u504f\u9ad8";
            }

            if (metrics.RoadConnectivity < 55 || metrics.RoadBottleneckPressure >= 65)
            {
                return "\u8def\u672a\u901a";
            }

            if (metrics.ServiceGapPressure >= 58)
            {
                return "\u670d\u4e0d\u8db3";
            }

            if (metrics.UtilityReliability < 70)
            {
                return "\u6c34\u7535\u4e0d\u8db3";
            }

            return "\u9009\u5740\u53d7\u9650";
        }

        private static string PreferText(string primary, string secondary, string fallback)
        {
            if (!string.IsNullOrEmpty(primary))
            {
                return primary;
            }

            return string.IsNullOrEmpty(secondary) ? fallback : secondary;
        }

        private static string CompactCommandPart(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return string.IsNullOrEmpty(value) ? string.Empty : value;
            }

            return value.Substring(0, Mathf.Max(1, maxLength));
        }

        private static string CompactCommandFeedbackText(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 68)
            {
                return string.IsNullOrEmpty(value) ? string.Empty : value;
            }

            return value.Substring(0, 67) + "...";
        }

        private static ConstructionPreview BuildPolicyImpactPreview(CityPolicy policy, bool enabled, PolicyImpactPreview before, PolicyImpactPreview after, CityMetrics metrics)
        {
            var preview = new ConstructionPreview
            {
                Title = "\u653f\u7b56\u56de\u6267",
                Ok = true,
                ConfirmLabel = (enabled ? "\u5b8c\u6210 \u653f+ " : "\u5b8c\u6210 \u653f- ") + PolicyLabel(policy)
            };

            preview.Lines.Add(BuildOperationsBriefLine(metrics, true));
            preview.Lines.Add(BuildOperationsOutcomeLine(before, after));
            preview.Lines.Add(BuildCityImpactLine(before, after));
            preview.Lines.Add(BuildPolicyPrimaryImpactLine(before, after));
            preview.Lines.Add("\u8d44\u6d41 \u6536 " + FormatSigned(after.NetIncome - before.NetIncome) + "  \u653f\u672c " + FormatSigned(-after.PolicyExpense + before.PolicyExpense));
            preview.Lines.Add("\u8def\u9762 \u5835 " + FormatSigned(after.Congestion - before.Congestion) + "  \u505c " + FormatSigned(after.ParkingPressure - before.ParkingPressure) + "  \u8f66\u4f9d " + FormatSigned(after.CarDependency - before.CarDependency));
            preview.Lines.Add("\u8857\u9762 \u6b65 " + FormatSigned(after.Walkability - before.Walkability) + "  \u4e8b " + FormatSigned(after.AccidentRisk - before.AccidentRisk) + "  \u6d2a " + FormatSigned(after.StormwaterResilience - before.StormwaterResilience));
            preview.Lines.Add("\u6c11\u9762 \u6d9d " + FormatSigned(after.FloodRisk - before.FloodRisk) + "  \u538b " + FormatSigned(after.PolicyBacklog - before.PolicyBacklog) + "  \u5e78 " + FormatSigned(after.Happiness - before.Happiness));
            return preview;
        }

        private static ConstructionPreview BuildManagementImpactPreview(string title, string label, PolicyImpactPreview before, PolicyImpactPreview after, CityMetrics metrics)
        {
            // MANAGEMENT_COMMAND_IMPACT_PREVIEW turns citywide commands into readable HUD feedback.
            var preview = new ConstructionPreview
            {
                Title = "\u7ba1\u7406\u56de\u6267",
                Ok = true,
                ConfirmLabel = title + " " + label
            };

            preview.Lines.Add(BuildOperationsBriefLine(metrics, true));
            preview.Lines.Add(BuildOperationsOutcomeLine(before, after));
            preview.Lines.Add(BuildCityImpactLine(before, after));
            preview.Lines.Add(BuildManagementPrimaryImpactLine(before, after));
            preview.Lines.Add("\u8d44\u6d41 \u73b0 " + FormatSigned(after.Cash - before.Cash) + "  \u6536 " + FormatSigned(after.NetIncome - before.NetIncome) + "  \u503a " + FormatSigned(after.BondPrincipal - before.BondPrincipal));
            preview.Lines.Add("\u8d22\u9762 \u5eb7 " + FormatSigned(after.FiscalHealth - before.FiscalHealth) + "  \u503a\u538b " + FormatSigned(after.DebtPressure - before.DebtPressure) + "  \u9669 " + FormatSigned(after.ForecastRisk - before.ForecastRisk));
            preview.Lines.Add("\u670d\u9762 \u76d6 " + FormatSigned(after.ServiceCoverage - before.ServiceCoverage) + "  \u62e8 " + FormatSigned(after.ServiceBudgetExpense - before.ServiceBudgetExpense));
            preview.Lines.Add("\u6c11\u9762 \u5e78 " + FormatSigned(after.Happiness - before.Happiness) + "  \u9700 " + FormatSigned(after.DemandUrgency - before.DemandUrgency));
            return preview;
        }

        private static string BuildCityImpactLine(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            return "\u8d26 \u73b0" + FormatSigned(after.Cash - before.Cash)
                + " \u6c11" + after.Population
                + " \u9669" + FormatSigned(after.ForecastRisk - before.ForecastRisk)
                + " \u670d" + FormatSigned(after.ServiceGapPressure - before.ServiceGapPressure)
                + " \u8def" + FormatSigned(after.Congestion - before.Congestion) + "/" + FormatSigned(after.RoadBottleneckPressure - before.RoadBottleneckPressure);
        }

        private static string BuildPolicyPrimaryImpactLine(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            // POLICY_PRIMARY_CITY_IMPACT_SUMMARY surfaces playable city deltas before accounting details.
            return "\u653f \u5835" + FormatSigned(after.Congestion - before.Congestion)
                + " \u505c" + FormatSigned(after.ParkingPressure - before.ParkingPressure)
                + " \u6b65" + FormatSigned(after.Walkability - before.Walkability)
                + " \u4e8b" + FormatSigned(after.AccidentRisk - before.AccidentRisk);
        }

        private static string BuildManagementPrimaryImpactLine(PolicyImpactPreview before, PolicyImpactPreview after)
        {
            // MANAGEMENT_PRIMARY_CITY_IMPACT_SUMMARY makes budget/tax commands feel like city levers.
            return "\u7ba1 \u670d" + FormatSigned(after.ServiceCoverage - before.ServiceCoverage)
                + " \u9884" + FormatSigned(after.ForecastRisk - before.ForecastRisk)
                + " \u5e78" + FormatSigned(after.Happiness - before.Happiness)
                + " \u9700" + FormatSigned(after.DemandUrgency - before.DemandUrgency);
        }

        private static ConstructionPreview BuildManagementBlockedPreview(string title, PolicyImpactPreview before, CityMetrics metrics)
        {
            var preview = new ConstructionPreview
            {
                Title = "\u7ba1\u7406\u56de\u6267",
                Ok = false,
                ConfirmLabel = title + " \u53d7\u963b"
            };

            preview.Lines.Add(BuildOperationsBriefLine(metrics, false));
            preview.Lines.Add("\u9669:\u73b0" + before.Cash + " \u503a" + before.DebtPressure + " \u6536" + before.NetIncome);
            preview.Lines.Add(BuildCityImpactLine(before, before));
            preview.Lines.Add("\u503a\u9762 \u73b0 " + before.Cash + "  \u672c " + before.BondPrincipal + "  \u503a\u538b " + before.DebtPressure);
            preview.Lines.Add("\u505a:\u5148\u6269\u7a0e\u57fa/\u63a7\u652f\u51fa/\u964d\u503a");
            return preview;
        }

        private static string PolicyLabel(CityPolicy policy)
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

        private static string TaxLevelLabel(CityTaxLevel level)
        {
            if (level == CityTaxLevel.Low) return "\u4f4e\u7a0e\u7387";
            if (level == CityTaxLevel.High) return "\u9ad8\u7a0e\u7387";
            return "\u6807\u51c6\u7a0e\u7387";
        }

        private static string ServiceBudgetLabel(CityServiceBudgetLevel level)
        {
            if (level == CityServiceBudgetLevel.Lean) return "\u7cbe\u7b80\u62e8\u6b3e";
            if (level == CityServiceBudgetLevel.Boosted) return "\u52a0\u7801\u62e8\u6b3e";
            return "\u6807\u51c6\u62e8\u6b3e";
        }

        private static string FormatSigned(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private struct PolicyImpactPreview
        {
            public int Cash;
            public int Population;
            public int NetIncome;
            public int PolicyExpense;
            public int Congestion;
            public int ParkingPressure;
            public int CarDependency;
            public int Walkability;
            public int AccidentRisk;
            public int StormwaterResilience;
            public int FloodRisk;
            public int PolicyBacklog;
            public int Happiness;
            public int FiscalHealth;
            public int DebtPressure;
            public int BondPrincipal;
            public int ServiceCoverage;
            public int ServiceGapPressure;
            public int RoadBottleneckPressure;
            public int ServiceBudgetExpense;
            public int ForecastRisk;
            public int DemandUrgency;

            public static PolicyImpactPreview Capture(CityMetrics metrics)
            {
                if (metrics == null)
                {
                    return new PolicyImpactPreview();
                }

                return new PolicyImpactPreview
                {
                    Cash = metrics.Cash,
                    Population = metrics.Population,
                    NetIncome = metrics.NetIncome,
                    PolicyExpense = metrics.PolicyExpense,
                    Congestion = metrics.Congestion,
                    ParkingPressure = metrics.ParkingPressure,
                    CarDependency = metrics.CarDependency,
                    Walkability = metrics.Walkability,
                    AccidentRisk = metrics.AccidentRisk,
                    StormwaterResilience = metrics.StormwaterResilience,
                    FloodRisk = metrics.FloodRisk,
                    PolicyBacklog = metrics.PolicyBacklog,
                    Happiness = metrics.Happiness,
                    FiscalHealth = metrics.FiscalHealth,
                    DebtPressure = metrics.DebtPressure,
                    BondPrincipal = metrics.BondPrincipal,
                    ServiceCoverage = metrics.ServiceCoverage,
                    ServiceGapPressure = metrics.ServiceGapPressure,
                    RoadBottleneckPressure = metrics.RoadBottleneckPressure,
                    ServiceBudgetExpense = metrics.ServiceBudgetExpense,
                    ForecastRisk = metrics.ForecastRisk,
                    DemandUrgency = metrics.DemandUrgency
                };
            }
        }

        public TileData GetTile(int gridX, int gridY)
        {
            if (simulation == null || !simulation.Grid.InBounds(new GridPos(gridX, gridY)))
            {
                return null;
            }

            return simulation.Grid.GetTile(new GridPos(gridX, gridY));
        }

        public PlacedBuilding GetPlacedBuildingAt(int gridX, int gridY)
        {
            if (simulation == null || simulation.Buildings == null)
            {
                return null;
            }

            for (var i = 0; i < simulation.Buildings.Count; i += 1)
            {
                var building = simulation.Buildings[i];
                if (gridX >= building.Pos.X
                    && gridY >= building.Pos.Y
                    && gridX < building.Pos.X + building.Size.W
                    && gridY < building.Pos.Y + building.Size.H)
                {
                    return building;
                }
            }

            return null;
        }

        public RoadNode GetRoadAt(int gridX, int gridY)
        {
            if (simulation == null || simulation.Roads == null)
            {
                return null;
            }

            for (var i = 0; i < simulation.Roads.Count; i += 1)
            {
                var road = simulation.Roads[i];
                if (road.Pos.X == gridX && road.Pos.Y == gridY)
                {
                    return road;
                }
            }

            return null;
        }

        public BuildingDefinition GetBuildingDefinition(string configId)
        {
            if (config == null || string.IsNullOrEmpty(configId))
            {
                return null;
            }

            return config.GetBuilding(configId);
        }

        public void SetOverlay(OverlayMode mode)
        {
            if (overlayMode == mode)
            {
                return;
            }

            overlayMode = mode;
            PublishOverlayFeedback();
        }

        public Color32 GetOverlayColor(int gridX, int gridY)
        {
            return CityHudViewModel.OverlayColor(overlayMode, GetTile(gridX, gridY), Metrics);
        }

        public void CycleOverlay()
        {
            var values = (OverlayMode[])System.Enum.GetValues(typeof(OverlayMode));
            var nextIndex = 0;
            for (var i = 0; i < values.Length; i += 1)
            {
                if (values[i] == overlayMode)
                {
                    nextIndex = (i + 1) % values.Length;
                    break;
                }
            }

            overlayMode = values[nextIndex];
            PublishOverlayFeedback();
        }

        private void PublishOverlayFeedback()
        {
            PublishHudFeedback(BuildCurrentOperationsHudFeedback(BuildOverlayLogLabel(overlayMode)), true);
        }

        private static string BuildOverlayLogLabel(OverlayMode mode)
        {
            return "\u5c42 " + OverlayHudLabel(mode);
        }

        private static string OverlayHudLabel(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic) return "\u4ea4\u901a\u6d41";
            if (mode == OverlayMode.Zoning) return "\u7528\u5730\u5206\u533a";
            if (mode == OverlayMode.Services) return "\u670d\u52a1\u8986\u76d6";
            if (mode == OverlayMode.Transit) return "\u516c\u4ea4\u7ebf\u7f51";
            if (mode == OverlayMode.Logistics) return "\u8d27\u8fd0";
            if (mode == OverlayMode.Utilities) return "\u6c34\u7535";
            if (mode == OverlayMode.Communications) return "\u901a\u4fe1";
            if (mode == OverlayMode.RoadSafety) return "\u8def\u53e3\u5b89\u5168";
            if (mode == OverlayMode.Parking) return "\u505c\u8f66";
            if (mode == OverlayMode.Stormwater) return "\u96e8\u6d2a";
            if (mode == OverlayMode.Waste) return "\u56de\u6536";
            if (mode == OverlayMode.Pollution) return "\u6c61\u67d3";
            if (mode == OverlayMode.LandValue) return "\u5730\u4ef7";
            return "\u603b\u89c8";
        }
    }
}
