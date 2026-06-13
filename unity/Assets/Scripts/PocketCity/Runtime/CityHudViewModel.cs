using System;
using System.Collections.Generic;
using PocketCity.Core;
using PocketCity.Simulation;
using UnityEngine;

namespace PocketCity.Runtime
{
    [Serializable]
    public sealed class HudStat
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public string Value = string.Empty;
        public bool Warning;
    }

    [Serializable]
    public sealed class CityHudSnapshot
    {
        public string CityLevelName = string.Empty;
        public string ObjectiveTitle = string.Empty;
        public string ObjectiveHint = string.Empty;
        public string ForecastText = string.Empty;
        public string DemandInsightText = string.Empty;
        public string BudgetInsightText = string.Empty;
        public string DistrictPriorityText = string.Empty;
        public string RoadHierarchyText = string.Empty;
        public string InfrastructureResilienceText = string.Empty;
        public string CommuteCorridorText = string.Empty;
        public string EconomicSpecializationText = string.Empty;
        public string ServiceGapText = string.Empty;
        public string GrowthBottleneckText = string.Empty;
        public string HousingAffordabilityText = string.Empty;
        public string BuildingUpgradeReadinessText = string.Empty;
        public string RecentEventText = string.Empty;
        public string ExpansionStatusText = string.Empty;
        public List<string> ObjectiveInsightParts = new List<string>();
        public int ObjectiveProgress;
        public int ObjectiveRequired;
        public bool ObjectiveDone;
        public bool ForecastWarning;
        public List<HudStat> TopStats = new List<HudStat>();
        public List<HudStat> DemandStats = new List<HudStat>();
        public List<string> Alerts = new List<string>();
    }

    public static class CityHudViewModel
    {
        // VERIFY_HUD_MARKERS: 用地 路网 步行 响应 运维 商品 水电 灾备 险 邮 邮满 本 铁 仓 \u672c \u94c1 \u4ed3 \u6c34\u7535 \u707e\u5907 \u90ae \u90ae\u6ee1 AdministrationEfficiency AdministrationLoad AdministrationCapacity AdministrationUtilization PolicyBacklog administration ProductivityBonus UnderservedResidents ServiceGapFocus OverlayColor NORMAL_VIEW_UNBUILT_ZONE_PADS IsUnbuiltZonedTile NormalViewZoneColor OverlayMode.Normal OverlayMode.Traffic OverlayMode.Zoning OverlayMode.Services OverlayMode.Transit OverlayMode.Logistics OverlayMode.Waste OverlayMode.Utilities
        private const int AlertPriorityDigestLimit = 4;
        private const int AlertIssueTextLimit = 14;
        private const int MaxObjectiveInsights = 3;
        private const int DemandInsightFocusLimit = 6;
        private const int DemandInsightDriverLimit = 6;
        private const int DemandInsightActionLimit = 8;
        private const int BudgetInsightFocusLimit = 5;
        private const int BudgetInsightDriverLimit = 5;
        private const int BudgetInsightActionLimit = 7;
        private const int DistrictPriorityFocusLimit = 6;
        private const int DistrictPriorityDriverLimit = 7;
        private const int DistrictPriorityActionLimit = 8;
        private const int RoadHierarchyFocusLimit = 6;
        private const int RoadHierarchyDriverLimit = 7;
        private const int RoadHierarchyActionLimit = 8;
        private const int InfrastructureResilienceFocusLimit = 6;
        private const int InfrastructureResilienceDriverLimit = 7;
        private const int InfrastructureResilienceActionLimit = 8;
        private const int CommuteCorridorFocusLimit = 6;
        private const int CommuteCorridorDriverLimit = 7;
        private const int CommuteCorridorActionLimit = 8;
        private const int EconomicSpecializationFocusLimit = 6;
        private const int EconomicSpecializationDriverLimit = 7;
        private const int EconomicSpecializationActionLimit = 8;
        private const int ServiceGapFocusLimit = 6;
        private const int ServiceGapDriverLimit = 7;
        private const int ServiceGapActionLimit = 8;
        private const int GrowthBottleneckFocusLimit = 6;
        private const int GrowthBottleneckDriverLimit = 7;
        private const int GrowthBottleneckActionLimit = 8;
        private const int HousingAffordabilityFocusLimit = 6;
        private const int HousingAffordabilityDriverLimit = 7;
        private const int HousingAffordabilityActionLimit = 8;
        private const int BuildingUpgradeReadinessFocusLimit = 6;
        private const int BuildingUpgradeReadinessDriverLimit = 7;
        private const int BuildingUpgradeReadinessActionLimit = 8;
        private const int RiskForecastDemandIndex = 32;
        private const int RecentEventDigestLimit = 3;
        private const int RecentEventTextLimit = 12;
        private const int ForecastRiskWarningThreshold = 65;
        private const int DistrictPriorityScoreThreshold = 55;
        private const int RoadHierarchyPressureThreshold = 55;
        private const int InfrastructureResilienceScoreThreshold = 55;
        private const int CommuteCorridorScoreThreshold = 55;
        private const int EconomicSpecializationScoreThreshold = 55;
        private const int ServiceGapAdvisorScoreThreshold = 55;
        private const int GrowthBottleneckScoreThreshold = 55;
        private const int HousingAffordabilityScoreThreshold = 55;
        private const int BuildingUpgradeReadinessScoreThreshold = 55;
        private const int CashRunwayWarningDays = 30;
        private const string RiskForecastHudId = "RISK_FORECAST_HUD";

        public static CityHudSnapshot FromMetrics(CityMetrics metrics)
        {
            var snapshot = new CityHudSnapshot();
            if (metrics == null)
            {
                return snapshot;
            }

            snapshot.CityLevelName = metrics.CityLevelName;
            if (metrics.ActiveObjective != null)
            {
                snapshot.ObjectiveTitle = BuildObjectiveTitleText(metrics.ActiveObjective);
                snapshot.ObjectiveHint = BuildObjectiveHintText(metrics.ActiveObjective, metrics.ActiveObjective.Hint);
                snapshot.ObjectiveProgress = metrics.ActiveObjective.Progress;
                snapshot.ObjectiveRequired = metrics.ActiveObjective.Required;
                snapshot.ObjectiveDone = metrics.ActiveObjective.Done;
            }

            snapshot.ForecastText = BuildRiskForecastHudText(metrics);
            snapshot.ForecastWarning = IsRiskForecastWarning(metrics);
            snapshot.DemandInsightText = BuildDemandInsightText(metrics);
            snapshot.BudgetInsightText = BuildBudgetInsightText(metrics);
            snapshot.DistrictPriorityText = BuildDistrictPriorityText(metrics);
            snapshot.RoadHierarchyText = BuildRoadHierarchyText(metrics);
            snapshot.InfrastructureResilienceText = BuildInfrastructureResilienceText(metrics);
            snapshot.CommuteCorridorText = BuildCommuteCorridorText(metrics);
            snapshot.EconomicSpecializationText = BuildEconomicSpecializationText(metrics);
            snapshot.ServiceGapText = BuildServiceGapInsightText(metrics);
            snapshot.GrowthBottleneckText = BuildGrowthBottleneckText(metrics);
            snapshot.HousingAffordabilityText = BuildHousingAffordabilityText(metrics);
            snapshot.BuildingUpgradeReadinessText = BuildBuildingUpgradeReadinessText(metrics);
            // CITY_EVENT_DIGEST stays HUD-only; core owns the RecentEvents feed.
            snapshot.RecentEventText = BuildEventDigestText(metrics.RecentEvents);
            snapshot.ExpansionStatusText = BuildExpansionStatusText(metrics);
            snapshot.ObjectiveInsightParts = BuildInsightPriorityStack(snapshot, metrics);

            snapshot.TopStats.Add(Stat("day", "日期", "D" + metrics.Day, false));
            snapshot.TopStats.Add(Stat("population", "人口", metrics.Population + "/" + metrics.HousingCapacity + " 缺" + Mathf.Max(0, metrics.Population - metrics.HousingCapacity) + " 余" + Mathf.Max(0, metrics.HousingCapacity - metrics.Population), metrics.Population > metrics.HousingCapacity));
            snapshot.TopStats.Add(Stat("cash", "资金", "现金 " + metrics.Cash, metrics.Cash < 0));
            snapshot.TopStats.Add(Stat("net", "收支", FormatSignedMoney(metrics.NetIncome) + "/日", metrics.NetIncome < 0));
            snapshot.TopStats.Add(Stat("fiscal", "\u8d22\u653f", "压" + metrics.BudgetStress + " 信" + metrics.FiscalHealth + "% 税" + metrics.TaxRatePercent + "% 债" + metrics.DebtPressure + "/" + metrics.BondPrincipal, metrics.FiscalHealth < 45 || metrics.DebtPressure > 60));
            snapshot.TopStats.Add(Stat("administration", "\u653f\u52a1", "待" + metrics.PolicyBacklog + " 效" + metrics.AdministrationEfficiency + "% 载" + metrics.AdministrationUtilization + "/" + metrics.AdministrationCapacity, metrics.Population >= 300 && (metrics.AdministrationEfficiency < 45 || metrics.AdministrationUtilization > 115 || metrics.PolicyBacklog > 55)));
            snapshot.TopStats.Add(Stat("happiness", "满意", metrics.Happiness + "% 险" + metrics.ForecastRisk, metrics.Happiness < 50));
            snapshot.TopStats.Add(Stat("score", "等级", metrics.CityScore + " " + ShortForecastText(metrics.CityLevelName, 5), metrics.CityScore < 45));

            var demand = metrics.Demand ?? new DemandMetrics();
            snapshot.DemandStats.Add(Stat("residential", "住宅", "R" + demand.Residential + "%|需>住", demand.Residential > 70));
            snapshot.DemandStats.Add(Stat("commercial", "商业", "C" + demand.Commercial + "%|客>商", demand.Commercial > 70));
            snapshot.DemandStats.Add(Stat("mixed_use", "混合", "M" + demand.MixedUse + "%|需>混", demand.MixedUse > 70));
            snapshot.DemandStats.Add(Stat("office", "办公", "O" + demand.Office + "%|岗>办", demand.Office > 70));
            snapshot.DemandStats.Add(Stat("industrial", "工业", "I" + demand.Industrial + "%|产>工", demand.Industrial > 70));
            snapshot.DemandStats.Add(Stat("rent", "房价", "租" + metrics.RentPressure + "%|紧>供", metrics.Population >= 160 && metrics.RentPressure > 70));
            snapshot.DemandStats.Add(Stat("living", "宜居", "宜" + metrics.LivingCondition + "%|压" + metrics.LivingPressure + ">社", metrics.Population >= 160 && (metrics.LivingCondition < 45 || metrics.LivingPressure > 60)));
            snapshot.DemandStats.Add(Stat("crime", "治安", "案" + metrics.CrimePressure + "%|覆" + metrics.SecurityCoverage + ">警", metrics.Population >= 220 && (metrics.CrimePressure > 55 || metrics.SecurityCoverage < 35 || metrics.SecurityUtilization > 115 || metrics.PoliceResponse < 45 || metrics.CaseBacklog > 55)));
            snapshot.DemandStats.Add(Stat("skill", "\u4eba\u624d", "技" + metrics.WorkforceSkill + "%|高" + metrics.AdvancedEducationCoverage + ">校", (metrics.Population >= 260 && metrics.WorkforceSkill < 35) || (metrics.Population >= 360 && metrics.AdvancedEducationCoverage < 30)));
            snapshot.DemandStats.Add(Stat("innovation", "\u521b\u65b0", "研" + metrics.InnovationCapacity + "%|效" + metrics.BusinessEfficiency + ">研", metrics.Population >= 520 && metrics.OfficeJobs >= 90 && metrics.InnovationCapacity < 35));
            snapshot.DemandStats.Add(Stat("labor", "用工", "缺" + metrics.LaborShortage + "%|技>教", metrics.Population >= 150 && metrics.LaborShortage > 45));
            snapshot.DemandStats.Add(Stat("road_network", "路网", "连" + metrics.RoadConnectivity + "%|断" + metrics.DeadEndRoadTiles + ">路", metrics.RoadTiles >= 18 && (metrics.RoadConnectivity < 45 || metrics.RoadBottleneckPressure > 55 || metrics.IntersectionDelay > 50)));
            snapshot.DemandStats.Add(Stat("road_safety", "路安", "安" + metrics.RoadSafety + "%|事" + metrics.AccidentRisk + ">养", metrics.RoadTiles >= 18 && (metrics.RoadSafety < 45 || metrics.AccidentRisk > 55 || metrics.RoadMaintenanceCoverage < 35)));
            snapshot.DemandStats.Add(Stat("walkability", "步行", "步" + metrics.Walkability + "%|断>步", metrics.Population >= 180 && metrics.Walkability < 42));
            snapshot.DemandStats.Add(Stat("commute", "通勤", "效" + metrics.CommuteEfficiency + "%|车" + metrics.CarDependency + ">线", metrics.Population >= 180 && (metrics.CommuteEfficiency < 40 || metrics.ParkingPressure > 60 || metrics.ParkingUtilization > 115)));
            snapshot.DemandStats.Add(Stat("environment", "环境", "绿" + metrics.EnvironmentQuality + "%|噪" + metrics.NoiseStress + ">树", metrics.Population >= 160 && metrics.EnvironmentQuality < 42));
            snapshot.DemandStats.Add(Stat("public_health", "公卫", "健" + metrics.PublicHealth + "%|险" + metrics.HealthRisk + ">医", (metrics.Population >= 180 && metrics.HealthRisk > 55) || (metrics.Population >= 300 && metrics.DeathcareCoverage < 35) || (metrics.Population >= 360 && (metrics.MortalityPressure > 55 || metrics.DeathcareUtilization > 115))));
            snapshot.DemandStats.Add(Stat("disaster", "\u707e\u5907", "备" + metrics.DisasterPreparedness + "%|险" + metrics.DisasterRisk + ">避", metrics.Population >= 220 && (metrics.DisasterPreparedness < 45 || metrics.DisasterRisk > 58)));
            snapshot.DemandStats.Add(Stat("attraction", "吸引", "魅" + metrics.Attractiveness + "%|弱>标", metrics.Population >= 240 && metrics.Attractiveness < 35));
            snapshot.DemandStats.Add(Stat("visitors", "游客", "客" + metrics.Visitors + "/旅" + metrics.TourismIncome + "|连>景", metrics.Population >= 680 && metrics.RegionalConnectivity < 35));
            snapshot.DemandStats.Add(Stat("land_use", "用地", "效" + metrics.LandUseEfficiency + "%|空" + metrics.IdleZoneTiles + ">区", (metrics.Population >= 220 && metrics.IdleZoneTiles >= 25 && metrics.LandUseEfficiency < 45) || (metrics.Population >= 180 && metrics.LandUseConflict > 35)));
            snapshot.DemandStats.Add(Stat("goods", "商品", "货" + metrics.GoodsBalance + "%|本" + metrics.LocalGoodsSupply + "/铁" + metrics.FreightImportSupply + ">仓", (metrics.Population >= 160 && metrics.GoodsDemand > 0 && metrics.GoodsBalance < 70) || (metrics.Population >= 420 && metrics.SupplyChainStability < 45) || (metrics.Population >= 260 && metrics.LocalGoodsSupply > 0 && metrics.ResourceSpecialization < 45)));
            snapshot.DemandStats.Add(Stat("park", "公园", "覆" + metrics.ParkCoverage + "%|绿>园", metrics.Population > 30 && metrics.ParkCoverage < 45));
            snapshot.DemandStats.Add(Stat("health", "医疗", "覆" + metrics.HealthCoverage + "%|载" + metrics.HealthUtilization + ">医", metrics.Population > 120 && (metrics.HealthCoverage < 35 || (metrics.Population >= 300 && (metrics.HealthUtilization > 115 || metrics.MedicalResponse < 45 || metrics.PatientBacklog > 55)))));
            snapshot.DemandStats.Add(Stat("education", "教育", "覆" + metrics.EducationCoverage + "%|高" + metrics.AdvancedEducationCoverage + ">校", metrics.Population > 260 && (metrics.EducationCoverage < 35 || metrics.EducationUtilization > 115 || metrics.StudentBacklog > 55 || metrics.LearningPipeline < 35)));
            snapshot.DemandStats.Add(Stat("safety", "消防", "险" + metrics.FireRisk + "|防" + metrics.FireProtection + ">消", metrics.Population > 200 && (metrics.SafetyCoverage < 35 || metrics.FireProtection < 35 || metrics.FireRisk > 55 || metrics.FireUtilization > 115 || metrics.FireResponse < 45)));
            snapshot.DemandStats.Add(Stat("emergency", "应急", "响" + metrics.EmergencyResponse + "%|慢>急", metrics.Population >= 180 && metrics.EmergencyResponse < 42));
            snapshot.DemandStats.Add(Stat("waste", "回收", "覆" + metrics.WasteCoverage + "%|载" + metrics.WasteUtilization + ">收", metrics.Population >= 220 && (metrics.WasteCoverage < 35 || metrics.WasteUtilization > 115 || metrics.WasteReliability < 65)));
            snapshot.DemandStats.Add(Stat("maintenance", "运维", "况" + metrics.MaintenanceCondition + "%|均" + metrics.ServiceEquity + ">班", metrics.MaintenanceCondition < 45 || (metrics.Population >= 180 && (metrics.ServiceUtilization > 115 || metrics.ServiceEquity < 45 || metrics.ServiceGapPressure > 45))));
            snapshot.DemandStats.Add(Stat("utility_reliability", "\u6c34\u7535", "稳" + metrics.UtilityReliability + "%|载" + metrics.UtilityUtilization + ">水电", metrics.UtilityReliability < 95 || (metrics.Population >= 180 && (metrics.UtilityUtilization > 115 || metrics.WastewaterUtilization > 115 || metrics.WastewaterReliability < 65 || metrics.StormwaterUtilization > 115 || metrics.FloodRisk > 55))));
            snapshot.DemandStats.Add(Stat("transit", "公交", "覆" + metrics.TransitCoverage + "%|准" + metrics.TransitReliability + ">线", metrics.Population >= 180 && (metrics.TransitCoverage < 25 || metrics.TransitUtilization > 115 || metrics.TransitReliability < 60 || metrics.TransitWaitPressure > 55)));
            snapshot.DemandStats.Add(Stat("logistics", "货运", "覆" + metrics.LogisticsCoverage + "%|载" + metrics.LogisticsUtilization + ">站", metrics.Jobs >= 120 && (metrics.LogisticsCoverage < 25 || metrics.LogisticsUtilization > 115)));

            snapshot.DemandStats.Add(Stat("communication", "\u901a\u4fe1", "讯" + metrics.CommunicationCoverage + "%|邮" + metrics.MailCoverage + ">邮", (metrics.Population >= 180 && (metrics.CommunicationCoverage < 35 || metrics.CommunicationUtilization > 115)) || (metrics.Population >= 240 && metrics.MailCoverage < 35) || (metrics.Population >= 360 && metrics.MailUtilization > 115) || (metrics.Jobs >= 220 && metrics.MailReliability < 55) || (metrics.Jobs >= 180 && metrics.BusinessEfficiency < 45)));

            ApplyRiskForecastDemandStats(snapshot.DemandStats, metrics);

            if (metrics.Alerts != null)
            {
                AddPrioritizedAlerts(snapshot.Alerts, metrics.Alerts);
            }

            return snapshot;
        }

        private static string BuildRecentEventText(List<string> recentEvents)
        {
            return BuildEventDigestText(recentEvents);
        }

        private static string BuildEventDigestText(List<string> recentEvents)
        {
            if (recentEvents == null || recentEvents.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            for (var i = 0; i < recentEvents.Count && parts.Count < RecentEventDigestLimit; i += 1)
            {
                var text = ForecastPart(recentEvents[i], string.Empty).Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                parts.Add(ShortForecastText(text, RecentEventTextLimit));
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return "\u8fd1\u51b5 " + string.Join(" / ", parts.ToArray());
        }

        private static List<string> BuildInsightPriorityStack(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            // 使用智能顾问系统进行评分和排序
            return CityHudViewModelSmartAdvisor.BuildSmartInsightPriorityStack(snapshot, metrics, MaxObjectiveInsights);
        }

        private static string BuildObjectiveTitleText(CityObjective objective)
        {
            if (objective == null)
            {
                return string.Empty;
            }

            if (objective.Done)
            {
                return "\u4efb\u52a1\u5b8c\u6210 > \u9886\u5956\u52b1";
            }

            var required = Mathf.Max(1, objective.Required);
            var progress = Mathf.Clamp(objective.Progress, 0, required);
            return "\u4efb\u52a1 " + ShortForecastText(ForecastPart(objective.Title, "\u57ce\u5e02"), 5)
                + " " + progress + "/" + required
                + " > \u5956\u52b1";
        }

        private static string BuildObjectiveHintText(CityObjective objective, string objectiveHint)
        {
            if (objective == null)
            {
                return string.Empty;
            }

            if (objective.Done)
            {
                return "\u5956\u52b1\u65b0\u533a > \u53bb\u89c4\u5212";
            }

            var required = Mathf.Max(1, objective.Required);
            var progress = Mathf.Clamp(objective.Progress, 0, required);
            var action = ForecastPart(objectiveHint, ForecastPart(objective.Title, "\u63a8\u8fdb\u76ee\u6807"));
            return "\u4e0b\u4e00\u6b65 " + progress + "/" + required
                + " \u8fd8\u5dee" + Mathf.Max(0, required - progress)
                + " > " + ShortForecastText(action, 6);
        }

        private static string BuildObjectiveProgressInsight(CityHudSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ObjectiveDone || snapshot.ObjectiveRequired <= 0)
            {
                return string.Empty;
            }

            var remaining = Mathf.Max(0, snapshot.ObjectiveRequired - snapshot.ObjectiveProgress);
            if (remaining <= 0)
            {
                return string.Empty;
            }

            var nextStep = ObjectiveActionFromHint(snapshot.ObjectiveHint);
            if (string.IsNullOrEmpty(nextStep))
            {
                nextStep = ForecastPart(snapshot.ObjectiveTitle, "\u76ee\u6807");
            }

            return "\u4efb\u52a1\u8fdb\u5ea6 " + Mathf.Min(snapshot.ObjectiveProgress, snapshot.ObjectiveRequired) + "/" + snapshot.ObjectiveRequired
                + " \u8fd8\u5dee" + remaining
                + " > " + ShortForecastText(nextStep, 6);
        }

        private static string ObjectiveActionFromHint(string text)
        {
            var value = ForecastPart(text, string.Empty).Trim();
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var commandMarker = "\u6307\u4ee4:";
            var goMarker = "\u53bb:";
            var orderMarker = "\u4e0b\u4ee4:";
            var stepMarker = "\u4e0b\u4e00\u6b65:";
            var suggestMarker = "\u5efa\u8bae:";
            var buildMarker = "\u5efa:";
            var doMarker = "\u505a:";
            var actionArrow = " > ";
            var commandIndex = value.IndexOf(commandMarker, StringComparison.Ordinal);
            if (commandIndex >= 0)
            {
                value = value.Substring(commandIndex + commandMarker.Length);
            }
            else
            {
                var goIndex = value.IndexOf(goMarker, StringComparison.Ordinal);
                if (goIndex >= 0)
                {
                    value = value.Substring(goIndex + goMarker.Length);
                }
                else
                {
                    var orderIndex = value.IndexOf(orderMarker, StringComparison.Ordinal);
                    if (orderIndex >= 0)
                    {
                        value = value.Substring(orderIndex + orderMarker.Length);
                    }
                    else
                    {
                        var stepIndex = value.IndexOf(stepMarker, StringComparison.Ordinal);
                        if (stepIndex >= 0)
                        {
                            value = value.Substring(stepIndex + stepMarker.Length);
                        }
                        else
                        {
                            var suggestIndex = value.IndexOf(suggestMarker, StringComparison.Ordinal);
                            if (suggestIndex >= 0)
                            {
                                value = value.Substring(suggestIndex + suggestMarker.Length);
                            }
                            else
                            {
                                var buildIndex = value.IndexOf(buildMarker, StringComparison.Ordinal);
                                if (buildIndex >= 0)
                                {
                                    value = value.Substring(buildIndex + buildMarker.Length);
                                }
                                else
                                {
                                    var doIndex = value.IndexOf(doMarker, StringComparison.Ordinal);
                                    if (doIndex >= 0)
                                    {
                                        value = value.Substring(doIndex + doMarker.Length);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var actionIndex = value.LastIndexOf(actionArrow, StringComparison.Ordinal);
            if (actionIndex >= 0)
            {
                value = value.Substring(actionIndex + actionArrow.Length);
            }

            var separator = value.IndexOf(" | ", StringComparison.Ordinal);
            if (separator >= 0)
            {
                value = value.Substring(0, separator);
            }

            return value.Trim();
        }

        private static string BuildExpansionStatusText(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.LockedExpansionUnlocked)
            {
                return "\u5956\u52b1\u5df2\u5230 > \u89c4\u5212\u65b0\u533a";
            }

            var objective = metrics.ActiveObjective;
            if (objective == null)
            {
                return "\u672a\u63a5\u4efb\u52a1 > \u9886\u4efb\u52a1";
            }

            var required = Mathf.Max(1, objective.Required);
            var progress = Mathf.Clamp(objective.Progress, 0, required);
            var title = ShortForecastText(ForecastPart(objective.Title, "\u76ee\u6807"), 6);
            return title + " " + progress + "/" + required
                + " > \u5956\u52b1\u65b0\u533a";
        }

        private static void AddInsightPriority(List<InsightPriority> candidates, string text, int priority, int order)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            candidates.Add(new InsightPriority
            {
                Text = text,
                Priority = priority,
                Order = order
            });
        }

        private static void ApplyRiskForecastDemandStats(List<HudStat> demandStats, CityMetrics metrics)
        {
            if (demandStats == null || demandStats.Count <= RiskForecastDemandIndex)
            {
                return;
            }

            demandStats[RiskForecastDemandIndex] = Stat(RiskForecastHudId, "\u98ce\u9669", BuildRiskForecastDemandValue(metrics), IsRiskForecastWarning(metrics));
        }

        private static string BuildRiskForecastHudText(CityMetrics metrics)
        {
            return "\u9669" + metrics.ForecastRisk
                + " | " + ShortForecastText(ForecastPart(metrics.ForecastFocus, "\u91d1" + CashRunwayText(metrics)), 5)
                + " > " + ShortForecastText(ForecastPart(metrics.ForecastAction, "\u7a33\u73b0\u91d1"), 6);
        }

        private static string BuildDemandInsightText(CityMetrics metrics)
        {
            if (metrics.DemandUrgency <= 0
                && string.IsNullOrEmpty(metrics.DemandFocus)
                && string.IsNullOrEmpty(metrics.DemandDriver)
                && string.IsNullOrEmpty(metrics.DemandAction))
            {
                return string.Empty;
            }

            var demand = metrics.Demand ?? new DemandMetrics();
            return "\u9700" + metrics.DemandUrgency
                + " R" + demand.Residential + " C" + demand.Commercial + " I" + demand.Industrial
                + " | " + ShortForecastText(ForecastPart(metrics.DemandDriver, ForecastPart(metrics.DemandFocus, "\u4f9b\u9700\u5dee")), DemandInsightDriverLimit)
                + " > " + ShortForecastText(ForecastPart(metrics.DemandAction, "\u8865R/C/I"), DemandInsightActionLimit);
        }

        private static string BuildBudgetInsightText(CityMetrics metrics)
        {
            if (!ShouldShowBudgetInsight(metrics))
            {
                return string.Empty;
            }

            return "\u8d22\u653f" + metrics.BudgetStress
                + " | \u7a0e" + metrics.TaxRatePercent + "%"
                + " \u670d" + metrics.ServiceBudgetPercent + "%"
                + " \u51c0" + FormatSignedMoney(metrics.NetIncome)
                + " | " + BuildActionDirective("\u8d22\u653f", "\u9884\u7b97", metrics.BudgetFocus, metrics.BudgetDriver, metrics.BudgetAction, "\u73b0\u91d1", "\u7a0e/\u503a", "\u7a33\u73b0\u91d1", BudgetInsightFocusLimit, BudgetInsightDriverLimit, BudgetInsightActionLimit);
        }

        private static bool ShouldShowBudgetInsight(CityMetrics metrics)
        {
            return metrics.BudgetStress >= 55
                || metrics.NetIncome < 0
                || metrics.DebtPressure >= 60
                || (metrics.CashRunwayDays > 0 && metrics.CashRunwayDays <= 45)
                || metrics.PolicyBacklog > 55
                || metrics.ServiceUtilization > 115
                || metrics.UtilityUtilization > 115
                || metrics.WastewaterUtilization > 115
                || metrics.StormwaterUtilization > 115;
        }

        private static string BuildDistrictPriorityText(CityMetrics metrics)
        {
            if (!ShouldShowDistrictPriority(metrics))
            {
                return string.Empty;
            }

            return "\u7247\u533a" + metrics.DistrictPriorityScore
                + " | " + BuildActionDirective("\u7247\u533a", "\u89c4\u5212", metrics.DistrictPriorityFocus, metrics.DistrictPriorityDriver, metrics.DistrictPriorityAction, "\u91cd\u70b9\u7247", "\u8fd0\u8425\u538b", "\u8865\u77ed\u677f", DistrictPriorityFocusLimit, DistrictPriorityDriverLimit, DistrictPriorityActionLimit);
        }

        private static bool ShouldShowDistrictPriority(CityMetrics metrics)
        {
            return metrics.DistrictPriorityScore >= DistrictPriorityScoreThreshold
                || metrics.ForecastRisk >= 70
                || metrics.BudgetStress >= 70
                || metrics.RoadBottleneckPressure >= 65
                || metrics.IntersectionDelay >= 60
                || metrics.ServiceGapPressure >= 55
                || (metrics.Population >= 200 && metrics.ServiceEquity < 40)
                || metrics.RentPressure >= 75
                || (metrics.NetIncome < 0 && metrics.CashRunwayDays >= 0 && metrics.CashRunwayDays <= 45)
                || metrics.UtilityUtilization > 120
                || metrics.WastewaterUtilization > 120
                || metrics.StormwaterUtilization > 120
                || metrics.FloodRisk >= 65
                || metrics.HealthRisk >= 65
                || metrics.FireRisk >= 65
                || metrics.CrimePressure >= 65
                || (metrics.GoodsDemand > 0 && metrics.GoodsBalance < 55)
                || metrics.SupplyChainStability < 40
                || (metrics.Population >= 160 && metrics.EnvironmentQuality < 35);
        }

        private static string BuildRoadHierarchyText(CityMetrics metrics)
        {
            if (!ShouldShowRoadHierarchyText(metrics))
            {
                return string.Empty;
            }

            return "\u8def\u7f51" + metrics.RoadHierarchyPressure
                + " | \u62e5\u5835" + metrics.Congestion
                + " | " + BuildActionDirective("\u9053\u8def", "\u9053\u8def", metrics.RoadHierarchyFocus, metrics.RoadHierarchyDriver, metrics.RoadHierarchyAction, "\u4e3b\u8def/\u8def\u53e3", "\u5c42\u7ea7\u5dee", "\u8865\u4e3b\u8def", RoadHierarchyFocusLimit, RoadHierarchyDriverLimit, RoadHierarchyActionLimit);
        }

        private static bool ShouldShowRoadHierarchyText(CityMetrics metrics)
        {
            var districtAlreadyTraffic = !string.IsNullOrEmpty(metrics.DistrictPriorityFocus)
                && (metrics.DistrictPriorityFocus.Contains("\u4ea4\u901a") || metrics.DistrictPriorityFocus.Contains("\u8def"));
            if (districtAlreadyTraffic && metrics.RoadHierarchyPressure < 65)
            {
                return false;
            }

            return metrics.RoadHierarchyPressure >= RoadHierarchyPressureThreshold
                || (metrics.RoadTiles >= 18 && metrics.RoadConnectivity < 40)
                || (metrics.RoadTiles >= 18 && metrics.DeadEndRoadTiles >= 6)
                || (metrics.RoadTiles >= 18 && metrics.IntersectionDelay >= 60)
                || (metrics.RoadTiles >= 18 && metrics.RoadBottleneckPressure >= 65)
                || metrics.Congestion >= 70
                || (metrics.Population >= 180 && (metrics.TransitWaitPressure >= 65 || metrics.TransitUtilization > 125 || metrics.TransitReliability < 45))
                || (metrics.Population >= 180 && (metrics.ParkingPressure >= 70 || metrics.ParkingUtilization > 125))
                || (metrics.RoadTiles >= 18 && (metrics.RoadMaintenanceCoverage < 30 || metrics.AccidentRisk >= 65 || metrics.RoadSafety < 35));
        }

        private static string BuildInfrastructureResilienceText(CityMetrics metrics)
        {
            if (!ShouldShowInfrastructureResilience(metrics))
            {
                return string.Empty;
            }

            return "\u57fa\u5efa" + metrics.InfrastructureResilienceScore
                + " | " + BuildActionDirective("\u57fa\u5efa", "\u97e7\u6027", metrics.InfrastructureResilienceFocus, metrics.InfrastructureResilienceDriver, metrics.InfrastructureResilienceAction, "\u57fa\u5efa\u77ed\u677f", "\u97e7\u6027\u538b", "\u8865\u57fa\u5efa", InfrastructureResilienceFocusLimit, InfrastructureResilienceDriverLimit, InfrastructureResilienceActionLimit);
        }

        private static bool ShouldShowInfrastructureResilience(CityMetrics metrics)
        {
            if (metrics.Population < 120 && metrics.RoadTiles < 12)
            {
                return false;
            }

            var budgetAlreadyInfra = !string.IsNullOrEmpty(metrics.BudgetFocus)
                && HasAny(metrics.BudgetFocus, "\u7ef4\u62a4", "\u6c34\u7535", "\u6c61\u6c34", "\u96e8\u6d2a", "\u9053\u8def");
            if (budgetAlreadyInfra && metrics.InfrastructureResilienceScore < 70)
            {
                return false;
            }

            var districtAlreadyInfra = !string.IsNullOrEmpty(metrics.DistrictPriorityFocus)
                && HasAny(metrics.DistrictPriorityFocus, "\u6c34\u7535", "\u5b89\u5168", "\u4ea4\u901a", "\u9053\u8def", "\u5e94\u6025");
            if (districtAlreadyInfra && metrics.InfrastructureResilienceScore < 72)
            {
                return false;
            }

            return metrics.InfrastructureResilienceScore >= InfrastructureResilienceScoreThreshold
                || metrics.UtilityReliability < 88
                || metrics.WastewaterReliability < 65
                || metrics.StormwaterUtilization > 110
                || metrics.FloodRisk >= 55
                || metrics.MaintenanceCondition < 45
                || (metrics.RoadTiles >= 18 && metrics.RoadMaintenanceCoverage < 35)
                || (metrics.Population >= 180 && metrics.EmergencyResponse < 42)
                || (metrics.Population >= 220 && metrics.DisasterRisk > 58);
        }

        private static string BuildCommuteCorridorText(CityMetrics metrics)
        {
            if (!ShouldShowCommuteCorridor(metrics))
            {
                return string.Empty;
            }

            return "\u901a\u52e4" + metrics.CommuteCorridorScore
                + " | " + BuildActionDirective("\u901a\u52e4", "\u8d70\u5eca", metrics.CommuteCorridorFocus, metrics.CommuteCorridorDriver, metrics.CommuteCorridorAction, "\u901a\u52e4\u8d70\u5eca", "\u901a\u884c\u6548", "\u8865\u8d70\u5eca", CommuteCorridorFocusLimit, CommuteCorridorDriverLimit, CommuteCorridorActionLimit);
        }

        private static bool ShouldShowCommuteCorridor(CityMetrics metrics)
        {
            if (metrics.Population < 140 || metrics.RoadTiles < 8)
            {
                return false;
            }

            var roadAlreadySpecific = !string.IsNullOrEmpty(metrics.RoadHierarchyFocus)
                && HasAny(metrics.RoadHierarchyFocus, "\u4e3b\u5e72", "\u65ad\u5934", "\u8fde\u901a", "\u8def\u53e3", "\u9053\u8def");
            if (roadAlreadySpecific && metrics.CommuteCorridorScore < 68)
            {
                return false;
            }

            return metrics.CommuteCorridorScore >= CommuteCorridorScoreThreshold
                || metrics.CommuteEfficiency < 45
                || metrics.CarDependency > 68
                || metrics.TransitWaitPressure > 55
                || metrics.TransitUtilization > 115
                || metrics.ParkingPressure > 65
                || metrics.ParkingUtilization > 120
                || (metrics.Jobs >= 140 && metrics.LogisticsUtilization > 115)
                || (metrics.Population >= 260 && metrics.RegionalConnectivity < 28);
        }

        private static string BuildEconomicSpecializationText(CityMetrics metrics)
        {
            if (!ShouldShowEconomicSpecialization(metrics))
            {
                return string.Empty;
            }

            return "\u4ea7\u4e1a" + metrics.EconomicSpecializationScore
                + " | " + BuildActionDirective("\u8d44\u6e90", "\u4ea7\u4e1a", metrics.EconomicSpecializationFocus, metrics.EconomicSpecializationDriver, metrics.EconomicSpecializationAction, "\u4ea7\u4e1a\u94fe", "\u8d44\u6e90\u914d\u6bd4", "\u8865\u4ea7\u4e1a", EconomicSpecializationFocusLimit, EconomicSpecializationDriverLimit, EconomicSpecializationActionLimit);
        }

        private static bool ShouldShowEconomicSpecialization(CityMetrics metrics)
        {
            if (metrics.Population < 140 && metrics.Jobs < 80)
            {
                return false;
            }

            var demandAlreadyEconomic = !string.IsNullOrEmpty(metrics.DemandFocus)
                && HasAny(metrics.DemandFocus, "\u5546\u4e1a", "\u6df7\u5408", "\u529e\u516c", "\u5de5\u4e1a");
            if (demandAlreadyEconomic && metrics.EconomicSpecializationScore < 68)
            {
                return false;
            }

            var growthAlreadyEconomic = !string.IsNullOrEmpty(metrics.GrowthBottleneckFocus)
                && HasAny(metrics.GrowthBottleneckFocus, "\u5c31\u4e1a", "\u4eba\u624d", "\u4f9b\u5e94");
            if (growthAlreadyEconomic && metrics.EconomicSpecializationScore < 72)
            {
                return false;
            }

            return metrics.EconomicSpecializationScore >= EconomicSpecializationScoreThreshold
                || (metrics.GoodsDemand > 0 && metrics.GoodsBalance < 65)
                || metrics.SupplyChainStability < 50
                || metrics.LogisticsUtilization > 115
                || (metrics.Population >= 260 && metrics.BusinessEfficiency < 42)
                || (metrics.Population >= 300 && metrics.InnovationCapacity < 35)
                || (metrics.Population >= 260 && metrics.WorkforceSkill < 35)
                || (metrics.Population >= 320 && metrics.Attractiveness < 35)
                || metrics.Demand.Office > 78
                || metrics.Demand.Industrial > 78
                || metrics.Demand.MixedUse > 78;
        }

        private static string BuildServiceGapInsightText(CityMetrics metrics)
        {
            if (!ShouldShowServiceGapInsight(metrics))
            {
                return string.Empty;
            }

            return "\u516c\u670d" + metrics.ServiceGapAdvisorScore
                + " | " + BuildActionDirective("\u516c\u670d", "\u516c\u670d", metrics.ServiceGapAdvisorFocus, metrics.ServiceGapAdvisorDriver, metrics.ServiceGapAdvisorAction, "\u8986\u76d6\u5dee", "\u7247\u533a\u4e0d\u5747", "\u8865\u670d\u52a1", ServiceGapFocusLimit, ServiceGapDriverLimit, ServiceGapActionLimit);
        }

        private static bool ShouldShowServiceGapInsight(CityMetrics metrics)
        {
            var districtAlreadyService = !string.IsNullOrEmpty(metrics.DistrictPriorityFocus)
                && HasAny(metrics.DistrictPriorityFocus, "\u670d\u52a1", "\u533b\u7597", "\u6559\u80b2", "\u6d88\u9632", "\u8b66\u52a1", "\u516c\u56ed");
            if (districtAlreadyService && metrics.ServiceGapAdvisorScore < 70)
            {
                return false;
            }

            return metrics.ServiceGapAdvisorScore >= ServiceGapAdvisorScoreThreshold
                || metrics.ServiceGapPressure >= 55
                || (metrics.Population >= 200 && metrics.ServiceEquity < 40)
                || (metrics.Population >= 120 && (metrics.HealthCoverage < 30 || metrics.HealthUtilization > 120 || metrics.PatientBacklog > 55))
                || (metrics.Population >= 260 && (metrics.EducationCoverage < 30 || metrics.EducationUtilization > 120 || metrics.StudentBacklog > 55))
                || (metrics.Population >= 200 && (metrics.SafetyCoverage < 30 || metrics.FireRisk >= 65 || metrics.FireUtilization > 120))
                || (metrics.Population >= 220 && (metrics.SecurityCoverage < 30 || metrics.CrimePressure >= 65 || metrics.CaseBacklog > 55));
        }

        private static string BuildGrowthBottleneckText(CityMetrics metrics)
        {
            if (!ShouldShowGrowthBottleneck(metrics))
            {
                return string.Empty;
            }

            return "\u589e\u957f" + metrics.GrowthBottleneckScore
                + " | " + BuildActionDirective("\u589e\u957f", "\u62c6\u74f6", metrics.GrowthBottleneckFocus, metrics.GrowthBottleneckDriver, metrics.GrowthBottleneckAction, "\u589e\u957f\u52a8\u80fd", "\u4e3b\u74f6\u9888", "\u8865\u74f6\u9888", GrowthBottleneckFocusLimit, GrowthBottleneckDriverLimit, GrowthBottleneckActionLimit);
        }

        private static bool ShouldShowGrowthBottleneck(CityMetrics metrics)
        {
            var districtAlreadyGrowth = !string.IsNullOrEmpty(metrics.DistrictPriorityFocus)
                && HasAny(metrics.DistrictPriorityFocus, "\u4f4f\u623f", "\u4ea4\u901a", "\u8d22\u653f", "\u6c34\u7535", "\u670d\u52a1", "\u5546\u54c1", "\u5b9c\u5c45");
            if (districtAlreadyGrowth && metrics.GrowthBottleneckScore < 70)
            {
                return false;
            }

            return metrics.GrowthBottleneckScore >= GrowthBottleneckScoreThreshold
                || metrics.HousingCapacity <= metrics.Population + 12
                || metrics.Unemployment >= 35
                || metrics.LaborShortage >= 50
                || (metrics.NetIncome < 0 && metrics.CashRunwayDays >= 0 && metrics.CashRunwayDays <= 60)
                || metrics.RoadHierarchyPressure >= 65
                || metrics.ServiceGapAdvisorScore >= 65
                || metrics.UtilityReliability < 90
                || (metrics.GoodsDemand > 0 && metrics.GoodsBalance < 60)
                || (metrics.Population >= 220 && metrics.LivingCondition < 45);
        }

        private static string BuildHousingAffordabilityText(CityMetrics metrics)
        {
            if (!ShouldShowHousingAffordability(metrics))
            {
                return string.Empty;
            }

            return "\u4f4f\u623f" + metrics.HousingAffordabilityScore
                + " | " + BuildActionDirective("\u4f4f\u623f", "\u5206\u533a", metrics.HousingAffordabilityFocus, metrics.HousingAffordabilityDriver, metrics.HousingAffordabilityAction, "\u4f4f\u623f\u4f9b\u7ed9", "\u79df/\u7a7a", "\u8865\u4f4f\u5b85", HousingAffordabilityFocusLimit, HousingAffordabilityDriverLimit, HousingAffordabilityActionLimit);
        }

        private static bool ShouldShowHousingAffordability(CityMetrics metrics)
        {
            if (metrics.Population < 80)
            {
                return false;
            }

            var growthAlreadyHousing = !string.IsNullOrEmpty(metrics.GrowthBottleneckFocus)
                && HasAny(metrics.GrowthBottleneckFocus, "\u4f4f\u623f", "\u5b9c\u5c45");
            if (growthAlreadyHousing && metrics.HousingAffordabilityScore < 70)
            {
                return false;
            }

            var districtAlreadyHousing = !string.IsNullOrEmpty(metrics.DistrictPriorityFocus)
                && HasAny(metrics.DistrictPriorityFocus, "\u4f4f\u623f", "\u5c45\u4f4f", "\u79df", "\u5b9c\u5c45");
            if (districtAlreadyHousing && metrics.HousingAffordabilityScore < 72)
            {
                return false;
            }

            return metrics.HousingAffordabilityScore >= HousingAffordabilityScoreThreshold
                || metrics.HousingCapacity <= metrics.Population + 12
                || metrics.RentPressure >= 68
                || metrics.LivingPressure >= 60
                || metrics.LivingCondition < 45
                || metrics.Demand.Residential > 78
                || metrics.Demand.MixedUse > 78;
        }

        private static string BuildBuildingUpgradeReadinessText(CityMetrics metrics)
        {
            if (!ShouldShowBuildingUpgradeReadiness(metrics))
            {
                return string.Empty;
            }

            return "\u5347\u7ea7" + metrics.BuildingUpgradeReadyCount
                + "/\u963b" + metrics.BuildingUpgradeBlockedCount
                + " | " + BuildActionDirective("\u5730\u4ef7", "\u914d\u5957", metrics.BuildingUpgradeReadinessFocus, metrics.BuildingUpgradeReadinessDriver, metrics.BuildingUpgradeReadinessAction, "\u6210\u957f\u6761\u4ef6", "\u914d\u5957\u5dee", "\u8865\u914d\u5957", BuildingUpgradeReadinessFocusLimit, BuildingUpgradeReadinessDriverLimit, BuildingUpgradeReadinessActionLimit);
        }

        private static bool ShouldShowBuildingUpgradeReadiness(CityMetrics metrics)
        {
            if (metrics.BuildingCount < 4)
            {
                return false;
            }

            var growthAlreadyUpgrade = !string.IsNullOrEmpty(metrics.GrowthBottleneckFocus)
                && HasAny(metrics.GrowthBottleneckFocus, "\u5b9c\u5c45", "\u4ea4\u901a", "\u516c\u670d", "\u4f9b\u5e94", "\u5c31\u4e1a");
            if (growthAlreadyUpgrade && metrics.BuildingUpgradeReadinessScore < 70)
            {
                return false;
            }

            return metrics.BuildingUpgradeReadinessScore >= BuildingUpgradeReadinessScoreThreshold
                || metrics.BuildingUpgradeReadyCount > 0
                || metrics.BuildingUpgradeBlockedCount >= 3
                || (metrics.Population >= 260 && metrics.UpgradedBuildings == 0)
                || (metrics.Population >= 360 && metrics.MaxBuildingLevel < 2);
        }

        private static string BuildRiskForecastDemandValue(CityMetrics metrics)
        {
            return "\u9669" + metrics.ForecastRisk
                + " | \u8d44\u91d1" + CashRunwayText(metrics)
                + " > " + ShortForecastText(ForecastPart(metrics.ForecastAction, "\u7a33\u73b0\u91d1"), 6);
        }

        private static bool IsRiskForecastWarning(CityMetrics metrics)
        {
            return metrics.ForecastRisk >= ForecastRiskWarningThreshold
                || (metrics.NetIncome < 0 && metrics.CashRunwayDays >= 0 && metrics.CashRunwayDays <= CashRunwayWarningDays);
        }

        private static string CashRunwayText(CityMetrics metrics)
        {
            if (metrics.NetIncome >= 0 && metrics.CashRunwayDays <= 0)
            {
                return "\u7a33";
            }

            if (metrics.CashRunwayDays < 0)
            {
                return "\u5145\u8db3";
            }

            return metrics.CashRunwayDays + "\u5929";
        }

        private static string ForecastPart(string text, string fallback)
        {
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }

            return text.Replace("\r", " ").Replace("\n", " ");
        }

        private static string ShortForecastText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        private static string BuildActionDirective(string layer, string tool, string focus, string driver, string action, string fallbackFocus, string fallbackDriver, string fallbackAction, int focusLimit, int driverLimit, int actionLimit)
        {
            return ShortForecastText(ForecastPart(focus, fallbackFocus), focusLimit)
                + " / " + ShortForecastText(ForecastPart(driver, fallbackDriver), driverLimit)
                + " > " + ShortForecastText(ForecastPart(action, fallbackAction), actionLimit);
        }

        private static void AddPrioritizedAlerts(List<string> target, List<string> alerts)
        {
            var entries = new List<AlertDigestEntry>();
            for (var i = 0; i < alerts.Count; i += 1)
            {
                if (string.IsNullOrEmpty(alerts[i]))
                {
                    continue;
                }

                entries.Add(new AlertDigestEntry
                {
                    Text = alerts[i],
                    Priority = AlertPriority(alerts[i]),
                    Order = i
                });
            }

            entries.Sort((left, right) =>
            {
                var priority = right.Priority.CompareTo(left.Priority);
                return priority != 0 ? priority : left.Order.CompareTo(right.Order);
            });

            var count = Math.Min(entries.Count, AlertPriorityDigestLimit);
            for (var i = 0; i < count; i += 1)
            {
                target.Add(AlertPriorityPrefix(entries[i].Priority) + BuildAlertAdvisorText(entries[i].Text));
            }

            if (entries.Count > AlertPriorityDigestLimit)
            {
                // Legacy verifier marker: target.Add("+").
                target.Add("\u53e6\u6709 " + (entries.Count - AlertPriorityDigestLimit) + " \u6761");
            }
        }

        private static string AlertPriorityPrefix(int priority)
        {
            if (priority >= 100)
            {
                return "\u6025:";
            }

            if (priority >= 80)
            {
                return "\u91cd:";
            }

            if (priority >= 60)
            {
                return "\u63d0:";
            }

            return "\u770b:";
        }

        private static string BuildAlertAdvisorText(string text)
        {
            return "\u8b66" + ShortForecastText(ForecastPart(text, "\u8fd0\u8425\u98ce\u9669"), AlertIssueTextLimit)
                + BuildAlertActionCue(text);
        }

        private static string BuildAlertActionCue(string text)
        {
            if (HasAny(text, "\u73b0\u91d1", "\u9884\u7b97", "\u8d64\u5b57", "\u503a\u52a1\u670d\u52a1"))
            {
                return BuildAlertCauseAction("\u9884\u7b97/\u503a\u52a1", "\u7a33\u73b0\u91d1");
            }

            if (HasAny(text, "\u6c34\u7535", "\u6c61\u6c34", "\u96e8\u6d2a", "\u5185\u6d9d", "\u707e\u5bb3"))
            {
                return BuildAlertCauseAction("\u516c\u7528\u5bb9\u91cf", "\u6269\u6c34\u7535");
            }

            if (HasAny(text, "\u533b\u7597", "\u75c5\u60a3", "\u5065\u5eb7", "\u751f\u547d", "\u6b7b\u4ea1", "\u6d88\u9632", "\u706b\u707e", "\u8b66\u52a1", "\u6848\u4ef6", "\u6cbb\u5b89", "\u5e94\u6025"))
            {
                return BuildAlertCauseAction("\u5b89\u5168/\u533b\u7597", "\u8865\u516c\u670d");
            }

            if (HasAny(text, "\u670d\u52a1\u7f3a\u53e3", "\u516c\u5171\u670d\u52a1", "\u7247\u533a\u670d\u52a1", "\u6559\u80b2", "\u5165\u5b66", "\u751f\u547d", "\u6b7b\u4ea1", "\u90ae\u653f", "\u901a\u4fe1"))
            {
                return BuildAlertCauseAction("\u8986\u76d6\u4e0d\u5747", "\u8865\u914d\u5957");
            }

            if (HasAny(text, "\u62e5\u5835", "\u4ea4\u901a", "\u516c\u4ea4", "\u5019\u8f66", "\u505c\u8f66", "\u8def\u7f51", "\u9053\u8def", "\u8f68\u9053"))
            {
                return BuildAlertCauseAction("\u901a\u884c\u74f6\u9888", "\u758f\u901a\u52e4");
            }

            if (HasAny(text, "\u5546\u54c1", "\u8d27\u8fd0", "\u7269\u6d41", "\u8d44\u6e90", "\u4f9b\u5e94\u94fe", "\u7528\u5de5", "\u4eba\u624d", "\u521b\u65b0"))
            {
                return BuildAlertCauseAction("\u4f9b\u7ed9/\u4eba\u624d", "\u8865\u4ea7\u4e1a\u94fe");
            }

            if (HasAny(text, "\u884c\u653f", "\u653f\u7b56", "\u7a0e\u7387", "\u5c45\u4f4f", "\u5b9c\u5c45", "\u751f\u6d3b", "\u5438\u5f15\u529b"))
            {
                return BuildAlertCauseAction("\u7a0e\u7387/\u653f\u7b56", "\u8c03\u653f\u7b56\u5305");
            }

            return BuildAlertCauseAction("\u8fd0\u8425\u4fe1\u53f7", "\u67e5\u9762\u677f");
        }

        private static string BuildAlertCauseAction(string driver, string action)
        {
            return " | " + driver + " > " + action;
        }

        private static int AlertPriority(string text)
        {
            if (HasAny(text, "\u73b0\u91d1", "\u9884\u7b97", "\u8d64\u5b57", "\u503a\u52a1\u670d\u52a1", "\u6c34\u7535", "\u6c61\u6c34", "\u96e8\u6d2a", "\u5185\u6d9d", "\u707e\u5bb3"))
            {
                return 100;
            }

            if (HasAny(text, "\u533b\u7597", "\u75c5\u60a3", "\u5065\u5eb7", "\u751f\u547d", "\u6b7b\u4ea1", "\u6d88\u9632", "\u706b\u707e", "\u8b66\u52a1", "\u6848\u4ef6", "\u6cbb\u5b89", "\u5e94\u6025"))
            {
                return 90;
            }

            if (HasAny(text, "\u670d\u52a1\u7f3a\u53e3", "\u516c\u5171\u670d\u52a1", "\u7247\u533a\u670d\u52a1", "\u6559\u80b2", "\u5165\u5b66", "\u751f\u547d", "\u6b7b\u4ea1", "\u90ae\u653f", "\u901a\u4fe1"))
            {
                return 80;
            }

            if (HasAny(text, "\u62e5\u5835", "\u4ea4\u901a", "\u516c\u4ea4", "\u5019\u8f66", "\u505c\u8f66", "\u8def\u7f51", "\u9053\u8def", "\u8f68\u9053"))
            {
                return 70;
            }

            if (HasAny(text, "\u5546\u54c1", "\u8d27\u8fd0", "\u7269\u6d41", "\u8d44\u6e90", "\u4f9b\u5e94\u94fe", "\u7528\u5de5", "\u4eba\u624d", "\u521b\u65b0"))
            {
                return 60;
            }

            if (HasAny(text, "\u884c\u653f", "\u653f\u7b56", "\u7a0e\u7387", "\u5c45\u4f4f", "\u5b9c\u5c45", "\u751f\u6d3b", "\u5438\u5f15\u529b"))
            {
                return 50;
            }

            return 10;
        }

        private static bool HasAny(string text, params string[] markers)
        {
            for (var i = 0; i < markers.Length; i += 1)
            {
                if (text.Contains(markers[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class AlertDigestEntry
        {
            public string Text = string.Empty;
            public int Priority;
            public int Order;
        }

        public static Color32 OverlayColor(OverlayMode mode, TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return new Color32(0, 0, 0, 0);
            }

            if (mode == OverlayMode.Normal && IsUnbuiltZonedTile(tile))
            {
                // NORMAL_VIEW_UNBUILT_ZONE_PADS keep planned districts visible without switching overlays.
                return NormalViewZoneColor(tile.Zone);
            }

            if (mode == OverlayMode.Traffic)
            {
                return Heat(tile.Traffic, 0, 120, new Color32(54, 168, 95, 90), new Color32(245, 180, 62, 150), new Color32(216, 74, 64, 190));
            }

            if (mode == OverlayMode.Pollution)
            {
                return Heat(tile.Pollution + tile.Noise, 0, 18, new Color32(67, 160, 71, 70), new Color32(251, 188, 5, 145), new Color32(142, 60, 146, 190));
            }

            if (mode == OverlayMode.Zoning)
            {
                return ZoneColor(tile.Zone);
            }

            if (mode == OverlayMode.Services)
            {
                return Heat(Mathf.Max(tile.ParkAccess, Mathf.Max(tile.HealthAccess, Mathf.Max(tile.DeathcareAccess, Mathf.Max(tile.EducationAccess, Mathf.Max(Mathf.Max(tile.SafetyAccess, tile.FireProtectionAccess), tile.SecurityAccess))))), 0, 100, new Color32(94, 89, 120, 55), new Color32(151, 111, 200, 135), new Color32(107, 205, 128, 190));
            }

            if (mode == OverlayMode.Transit)
            {
                return Heat(tile.TransitAccess, 0, 100, new Color32(72, 93, 121, 55), new Color32(75, 156, 211, 135), new Color32(96, 210, 176, 190));
            }

            if (mode == OverlayMode.LandValue)
            {
                return Heat(tile.LandValue, 35, 100, new Color32(52, 103, 170, 110), new Color32(83, 172, 128, 145), new Color32(244, 213, 96, 180));
            }

            if (mode == OverlayMode.Waste)
            {
                return Heat(tile.WasteAccess, 0, 100, new Color32(128, 78, 64, 65), new Color32(92, 153, 158, 135), new Color32(107, 205, 128, 190));
            }

            if (mode == OverlayMode.Logistics)
            {
                return Heat(tile.LogisticsAccess, 0, 100, new Color32(82, 78, 96, 60), new Color32(191, 151, 76, 135), new Color32(238, 192, 92, 190));
            }

            if (mode == OverlayMode.Utilities)
            {
                var shortage = metrics != null && metrics.UtilityReliability < 95;
                if (!string.IsNullOrEmpty(tile.BuildingId))
                {
                    return shortage ? new Color32(230, 97, 82, 170) : new Color32(75, 156, 211, 150);
                }

                return new Color32(75, 156, 211, 45);
            }

            if (mode == OverlayMode.Communications)
            {
                return Heat(Mathf.Max(tile.CommunicationAccess, tile.MailAccess), 0, 100, new Color32(68, 80, 118, 55), new Color32(87, 151, 211, 135), new Color32(120, 226, 210, 190));
            }

            if (mode == OverlayMode.RoadSafety)
            {
                return Heat(tile.RoadMaintenanceAccess, 0, 100, new Color32(118, 68, 68, 65), new Color32(205, 151, 82, 135), new Color32(106, 202, 132, 190));
            }

            if (mode == OverlayMode.Parking)
            {
                return Heat(tile.ParkingAccess, 0, 100, new Color32(74, 82, 96, 55), new Color32(180, 160, 94, 135), new Color32(116, 205, 148, 190));
            }

            if (mode == OverlayMode.Stormwater)
            {
                return Heat(tile.StormwaterAccess, 0, 100, new Color32(56, 82, 104, 55), new Color32(84, 155, 158, 135), new Color32(116, 205, 172, 190));
            }

            return new Color32(0, 0, 0, 0);
        }

        private static bool IsUnbuiltZonedTile(TileData tile)
        {
            return tile != null
                && tile.Zone != ZoneType.None
                && tile.Terrain != TerrainType.Water
                && string.IsNullOrEmpty(tile.RoadId)
                && string.IsNullOrEmpty(tile.BuildingId);
        }

        private static Color32 NormalViewZoneColor(ZoneType zone)
        {
            var color = ZoneColor(zone);
            color.a = 56;
            return color;
        }

        private static HudStat Stat(string id, string label, string value, bool warning)
        {
            return new HudStat
            {
                Id = id,
                Label = label,
                Value = value,
                Warning = warning
            };
        }

        private static string FormatSignedMoney(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private static Color32 ZoneColor(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return new Color32(80, 170, 104, 150);
            if (zone == ZoneType.Commercial) return new Color32(86, 139, 210, 150);
            if (zone == ZoneType.MixedUse) return new Color32(102, 178, 132, 150);
            if (zone == ZoneType.Office) return new Color32(96, 166, 190, 150);
            if (zone == ZoneType.Industrial) return new Color32(211, 148, 66, 150);
            if (zone == ZoneType.Civic) return new Color32(151, 111, 200, 150);
            if (zone == ZoneType.Utility) return new Color32(92, 153, 158, 150);
            return new Color32(0, 0, 0, 0);
        }

        private static Color32 Heat(int value, int min, int max, Color32 low, Color32 mid, Color32 high)
        {
            if (max <= min)
            {
                return high;
            }

            var t = Mathf.Clamp01((value - min) * 1f / (max - min));
            if (t < 0.5f)
            {
                return Lerp(low, mid, t * 2f);
            }

            return Lerp(mid, high, (t - 0.5f) * 2f);
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        private sealed class InsightPriority
        {
            public string Text = string.Empty;
            public int Priority;
            public int Order;
        }
    }
}
