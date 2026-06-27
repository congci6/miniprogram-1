using System;
using System.Collections.Generic;
using PocketCity.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PocketCity.Runtime
{
    public sealed class CityRuntimeHud : MonoBehaviour
    {
        [SerializeField] private CityGameController controller;
        [SerializeField] private CityInteractionController interaction;
        [SerializeField] private CitySaveController saveController;
        [SerializeField] private CityCameraController cameraController;
        [SerializeField] private Font font;
        [SerializeField] private float refreshInterval = 0.2f;

        private int lastMetricsHash;
        private bool isDirty = true;

        private readonly List<Text> topTexts = new List<Text>();
        private readonly List<Outline> topTextOutlines = new List<Outline>();
        private readonly List<Image> topStatScanMarkers = new List<Image>();
        private readonly List<Image> topStatRowBackplates = new List<Image>();
        private readonly List<Text> demandTexts = new List<Text>();
        private readonly List<Image> demandFillBars = new List<Image>();
        private readonly List<Image> demandGroupBars = new List<Image>();
        private readonly List<Image> demandHotCorners = new List<Image>();
        private readonly List<Text> demandGroupTags = new List<Text>();
        private readonly List<Image> demandSummaryFills = new List<Image>();
        private readonly List<Text> demandSummaryTexts = new List<Text>();
        private readonly List<Image> cityOpsChipImages = new List<Image>();
        private readonly List<Image> cityOpsChipFills = new List<Image>();
        private readonly List<Text> cityOpsChipTexts = new List<Text>();
        private readonly List<Image> priorityCommandChipImages = new List<Image>();
        private readonly List<Image> priorityCommandChipFills = new List<Image>();
        private readonly List<Text> priorityCommandChipTexts = new List<Text>();
        private readonly List<Image> priorityCommandBadgeImages = new List<Image>();
        private readonly List<Text> priorityCommandBadgeTexts = new List<Text>();
        private readonly List<Image> citySnapshotMetricFills = new List<Image>();
        private readonly List<Text> citySnapshotMetricTexts = new List<Text>();
        private readonly List<OverlayButtonBinding> overlayButtons = new List<OverlayButtonBinding>();
        private readonly List<Image> overlaySwatches = new List<Image>();
        private readonly List<Image> overlayPressureFills = new List<Image>();
        private readonly List<Image> overlayStateRails = new List<Image>();
        private readonly List<Image> overlayRecommendationBadges = new List<Image>();
        private readonly List<Text> overlayRecommendationBadgeGlyphs = new List<Text>();
        private readonly List<OverlayMode> overlaySwatchModes = new List<OverlayMode>();
        private readonly List<ToolButtonBinding> toolButtons = new List<ToolButtonBinding>();
        private readonly List<PolicyButtonBinding> policyButtons = new List<PolicyButtonBinding>();
        private readonly List<AdvisorActionBinding> advisorActionCards = new List<AdvisorActionBinding>();
        private readonly List<FeaturedBuildCardBinding> featuredBuildCards = new List<FeaturedBuildCardBinding>();
        private readonly List<Image> miniMapCells = new List<Image>();
        private readonly List<Image> miniMapCellFacets = new List<Image>();
        private readonly List<Outline> miniMapCellOutlines = new List<Outline>();
        private readonly List<MapPlanningPinBinding> mapPlanningPins = new List<MapPlanningPinBinding>();
        private readonly List<Text> topCapsuleSegmentTexts = new List<Text>();
        private readonly List<Image> topCapsuleImages = new List<Image>();
        private readonly List<Image> topCapsuleAccentImages = new List<Image>();
        private readonly List<Image> topCapsulePlusImages = new List<Image>();
        private readonly List<Image> topCapsuleDividerImages = new List<Image>();
        private readonly List<Image> topCapsuleStatusStrips = new List<Image>();
        private readonly List<Image> topCapsuleStatusBadgeImages = new List<Image>();
        private readonly List<Text> topCapsuleStatusBadgeTexts = new List<Text>();
        private readonly List<Outline> topCapsuleOutlines = new List<Outline>();
        private readonly List<Image> milestoneTaskThumbnailBlocks = new List<Image>();
        private RectTransform miniMapViewportFrame;
        private Text advisorRadarTitleText;
        private Text cityTitleText;
        private Image resourceLevelBadgeImage;
        private Text resourceLevelBadgeText;
        private Text topCapsuleText;
        private Image managementCapsuleImage;
        private Image managementCapsulePressureFill;
        private Image managementCapsuleStateBadgeImage;
        private Text managementCapsuleText;
        private Text managementCapsuleStateText;
        private Image demandRibbonImage;
        private Text demandRibbonText;
        private Image resourceObjectiveProgressFill;
        private Text resourceObjectiveProgressText;
        private Image cityOpsPanelImage;
        private Image cityOpsAccentImage;
        private Image cityOpsPressureFill;
        private Text cityOpsTitleText;
        private Text cityOpsActionText;
        private Image priorityCommandStripImage;
        private Text priorityCommandTitleText;
        private Image policyBudgetCardImage;
        private Image policyBudgetAccentImage;
        private Image policyBudgetFillImage;
        private Image policyBudgetStateBadgeImage;
        private Text policyBudgetTitleText;
        private Text policyBudgetDetailText;
        private Text policyBudgetStateText;
        private Text objectiveText;
        private Text milestoneTaskText;
        private Image milestoneTaskPreviewImage;
        private Image milestoneTaskProgressFill;
        private Image milestoneTaskProgressCap;
        private Outline milestoneTaskPreviewOutline;
        private Text milestoneTaskPreviewText;
        private Text milestoneTaskProgressLabel;
        private Text milestoneTaskRewardText;
        private Image milestoneTaskRewardStripImage;
        private Image milestoneTaskPriorityStrip;
        private Image milestoneTaskStageImage;
        private Text milestoneTaskStageText;
        private Image milestoneTaskStampImage;
        private Text milestoneTaskStampText;
        private Text alertText;
        private Text cityPulseText;
        private Image citySnapshotPanelImage;
        private Text citySnapshotTitleText;
        private Image advisorSeverityStrip;
        private Image advisorSeverityBadge;
        private Text advisorSeverityBadgeText;
        private Outline advisorPanelOutline;
        private readonly List<Image> advisorRadarFills = new List<Image>();
        private readonly List<Text> advisorRadarLabels = new List<Text>();
        private Text toolStatusText;
        private Text previewText;
        private Text saveStatusText;
        private Text miniMapRiskSummaryText;
        private Image miniMapCameraZoomFill;
        private Text miniMapCameraStatusText;
        private Image miniMapViewportFrameImage;
        private Outline miniMapViewportFrameOutline;
        private Image simulationStatusBadgeImage;
        private Image simulationStatusBadgeIconImage;
        private Image simulationStatusRewardBadgeImage;
        private Text simulationStatusBadgeText;
        private Text simulationStatusBadgeIconText;
        private Text simulationStatusBadgeSubText;
        private Text simulationStatusRewardBadgeText;
        private Image buildDockBadgeImage;
        private Text buildDockBadgeText;
        private Text buildDockBadgeGlyphText;
        private Image rightCommandStackImage;
        private Image rightCommandStackAccentImage;
        private Text rightCommandStackHintText;
        private readonly List<RightCommandBinding> rightCommandButtons = new List<RightCommandBinding>();
        private readonly List<Image> planningLensCards = new List<Image>();
        private readonly List<Image> planningLensFills = new List<Image>();
        private readonly List<Image> planningLensBadgeImages = new List<Image>();
        private readonly List<Text> planningLensTexts = new List<Text>();
        private readonly List<Text> planningLensBadgeTexts = new List<Text>();
        private readonly List<Outline> planningLensOutlines = new List<Outline>();
        private Image overlayLegendCardImage;
        private Image overlayLegendPressureFill;
        private Image overlayLegendAccentImage;
        private Image overlayLegendStateBadgeImage;
        private Text overlayLegendTitleText;
        private Text overlayLegendDetailText;
        private Text overlayLegendStateText;
        private Image placementQuoteCardImage;
        private Image placementQuoteAccentImage;
        private Image placementQuoteScoreFill;
        private Image placementQuoteStateBadgeImage;
        private Text placementQuoteTitleText;
        private Text placementQuoteMetricText;
        private Text placementQuoteDetailText;
        private Text placementQuoteStateText;
        private Image actionChainStripImage;
        private Image actionChainPressureFill;
        private Text actionChainText;
        private Image featuredBuildShelfImage;
        private Text featuredBuildShelfTitleText;
        private Image selectedTileDetailCardImage;
        private Image selectedTileDetailAccentImage;
        private Image selectedTileDetailActionImage;
        private Image selectedTileDetailStateBadgeImage;
        private Image selectedTileTrafficFill;
        private Image selectedTileServiceFill;
        private Image selectedTileLandFill;
        private Text selectedTileDetailTitleText;
        private Text selectedTileDetailSubtitleText;
        private Text selectedTileTrafficText;
        private Text selectedTileServiceText;
        private Text selectedTileLandText;
        private Text selectedTileDetailActionText;
        private Text selectedTileDetailStateText;
        private Image unlockRegionCalloutImage;
        private Image unlockRegionProgressFill;
        private Image unlockRegionAccentImage;
        private Text unlockRegionTitleText;
        private Text unlockRegionDetailText;
        private Text unlockRegionActionText;
        private int lastMiniMapSevereSamples;
        private int lastMiniMapWarningSamples;
        private float refreshTimer;
        private float commandFeedbackPulseTimer;
        private float objectivePulseTimer;
        private int seenCommandFeedbackVersion;
        private int seenRuntimeSessionVersion = -1;
        private int lastObjectiveProgress;
        private int lastObjectiveRequired;
        private bool lastCommandFeedbackSucceeded;
        private bool lastObjectiveDone;
        private bool objectivePulsePrimed;
        private string commandFeedbackText = string.Empty;
        private string objectivePulseText = string.Empty;
        private string lastObjectiveTitle = string.Empty;
        private string pendingAdvisorType = string.Empty;
        private int pendingAdvisorFeedbackVersion = -1;
        private float pendingAdvisorExpireTime;
        private const int MiniMapColumns = 14;
        private const int MiniMapRows = 6;
        private const float PendingAdvisorAdoptionLifetime = 20f;

        private sealed class OverlayButtonBinding
        {
            public Button Button;
            public Text Label;
            public OverlayMode Mode;
        }

        private sealed class ToolButtonBinding
        {
            public Button Button;
            public Text Label;
            public Image Accent;
            public Image IconSwatch;
            public Image SelectionGlow;
            public Image StateBadge;
            public Text IconGlyph;
            public Text MetaLabel;
            public Text StateBadgeText;
            public Outline Outline;
            public CityToolMode ToolMode;
            public ZoneType Zone;
            public string BuildingId = string.Empty;
        }

        private sealed class PolicyButtonBinding
        {
            public Button Button;
            public Text Label;
            public CityPolicy Policy;
        }

        private sealed class RightCommandBinding
        {
            public Image Card;
            public Image Swatch;
            public Image StateRail;
            public Image PressureFill;
            public Text Glyph;
            public Text Label;
            public Text StateText;
            public Outline Outline;
            public Color32 Accent;
            public int Kind;
        }

        private sealed class AdvisorActionBinding
        {
            public Button Button;
            public Image Card;
            public Image Fill;
            public Image Accent;
            public Image StageBadge;
            public Text Title;
            public Text Detail;
            public Text StageText;
            public int Lane;
        }

        private sealed class FeaturedBuildCardBinding
        {
            public Button Button;
            public Image Card;
            public Image Fill;
            public Image StateBadge;
            public Image IconPanel;
            public Text Glyph;
            public Text Title;
            public Text Cost;
            public Text Detail;
            public Text StateText;
            public Outline Outline;
            public CityToolMode ToolMode;
            public ZoneType Zone;
            public string BuildingId = string.Empty;
        }

        private sealed class MapPlanningPinBinding
        {
            public Image Card;
            public Image Accent;
            public Image Stem;
            public Text Title;
            public Text Detail;
            public int Kind;
        }

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<CityGameController>();
            }

            if (interaction == null)
            {
                interaction = GetComponent<CityInteractionController>();
            }

            if (saveController == null)
            {
                saveController = GetComponent<CitySaveController>();
            }

            if (cameraController == null)
            {
                cameraController = Camera.main != null ? Camera.main.GetComponent<CityCameraController>() : null;
            }

            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            BuildHud();
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            if (commandFeedbackPulseTimer > 0f)
            {
                commandFeedbackPulseTimer = Mathf.Max(0f, commandFeedbackPulseTimer - Time.deltaTime);
            }

            if (objectivePulseTimer > 0f)
            {
                objectivePulseTimer = Mathf.Max(0f, objectivePulseTimer - Time.deltaTime);
            }

            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = refreshInterval;
            Refresh();
        }

        public void Refresh()
        {
            var snapshot = controller.HudSnapshot;
            var metrics = controller != null ? controller.Metrics : null;

            // 计算简单的hash来检测变化
            int currentHash = ComputeMetricsHash(metrics);
            if (!isDirty && currentHash == lastMetricsHash)
            {
                return; // 没有变化，跳过更新
            }

            lastMetricsHash = currentHash;
            isDirty = false;

            RefreshObjectivePulseState(snapshot);
            SetStatTexts(topTexts, snapshot.TopStats);
            SetStatTexts(demandTexts, snapshot.DemandStats);

            if (cityTitleText != null)
            {
                cityTitleText.text = BuildCityTitleText(metrics);
            }

            if (resourceLevelBadgeText != null)
            {
                resourceLevelBadgeText.text = BuildCityLevelBadgeText(metrics);
            }

            if (resourceLevelBadgeImage != null)
            {
                resourceLevelBadgeImage.color = ResourceLevelBadgeColor(snapshot);
            }

            if (topCapsuleSegmentTexts.Count >= 3)
            {
                RefreshTopResourceCapsules(metrics);
                RefreshManagementCapsule(metrics);
            }
            else if (topCapsuleText != null)
            {
                topCapsuleText.text = BuildTopCapsuleText(metrics);
            }

            RefreshDemandRibbon(metrics);
            RefreshDemandSummaryRails(metrics);
            RefreshCitySnapshotBoard(metrics);
            RefreshCityOperationsStrip(metrics);
            RefreshPolicyBudgetForecast(metrics);
            RefreshAdvisorActionQueue(metrics);
            RefreshPriorityCommandStrip(metrics);
            RefreshResourceObjectiveProgress(snapshot);
            RefreshUnlockRegionCallout(snapshot, metrics);
            RefreshMapPlanningPins(metrics);

            if (objectiveText != null)
            {
                objectiveText.text = BuildObjectiveCardText(snapshot) + ObjectivePulseCardLine();
            }

            if (milestoneTaskText != null)
            {
                milestoneTaskText.text = BuildMilestoneTaskCardText(snapshot, controller.Metrics);
            }

            RefreshMilestoneTaskPreview(snapshot, metrics);

            if (alertText != null)
            {
                alertText.text = BuildCityEventTickerText(snapshot, metrics);
                alertText.color = CityEventTickerColor(snapshot, metrics);
            }

            if (cityPulseText != null)
            {
                cityPulseText.text = BuildCityPulseText(metrics);
                cityPulseText.color = metrics != null && (metrics.ForecastRisk >= 65 || metrics.RoadBottleneckPressure >= 60 || metrics.ServiceGapPressure >= 55)
                    ? new Color32(171, 92, 48, 255)
                    : new Color32(43, 64, 70, 255);
                RefreshAdvisorRadar(metrics);
            }

            if (advisorSeverityStrip != null)
            {
                var advisorColor = AdvisorSeverityColor(metrics);
                advisorSeverityStrip.color = advisorColor;
                if (advisorSeverityBadge != null)
                {
                    advisorSeverityBadge.color = new Color32(advisorColor.r, advisorColor.g, advisorColor.b, 218);
                }

                if (advisorSeverityBadgeText != null)
                {
                    advisorSeverityBadgeText.text = AdvisorSeverityBadgeLabel(metrics);
                    advisorSeverityBadgeText.color = AdvisorSeverityBadgeTextColor(metrics);
                }

                if (advisorPanelOutline != null)
                {
                    // REFERENCE_IMAGE_TASK_CARD_RISK_OUTLINE syncs the task card rim with the advisor strip.
                    advisorPanelOutline.effectColor = new Color32(advisorColor.r, advisorColor.g, advisorColor.b, 138);
                    advisorPanelOutline.effectDistance = AdvisorSeverityOutlineDistance(metrics);
                }
            }

            if (toolStatusText != null)
            {
                toolStatusText.text = BuildToolStatusText();
            }

            if (previewText != null)
            {
                RefreshCommandFeedbackPulse();
                var preview = BuildPreviewText();
                previewText.text = commandFeedbackPulseTimer > 0f ? BuildCommandFeedbackPulseText(preview) : preview;
                previewText.color = CommandFeedbackPreviewColor();
                previewText.fontStyle = commandFeedbackPulseTimer > 0f ? FontStyle.Bold : FontStyle.Normal;
            }

            if (saveStatusText != null)
            {
                saveStatusText.text = saveController != null && !string.IsNullOrEmpty(saveController.LastStatus)
                    ? saveController.LastStatus
                    : BuildHudFooterStatusText(snapshot, metrics);
            }

            RefreshSimulationStatusBadge(metrics);

            if (buildDockBadgeText != null)
            {
                buildDockBadgeText.text = BuildDockBadgeText();
            }

            if (buildDockBadgeGlyphText != null)
            {
                buildDockBadgeGlyphText.text = BuildDockBadgeGlyphText();
            }

            if (buildDockBadgeImage != null)
            {
                buildDockBadgeImage.color = BuildDockBadgeColor();
            }

            RefreshRightCommandStack(metrics);
            RefreshPlanningLensStrip(metrics);
            RefreshOverlayLegendCard(metrics);
            RefreshPlacementQuoteCard(metrics);
            RefreshActionChainStrip(metrics);
            RefreshSelectedTileDetailCard(metrics);
            RefreshOverlayButtons();
            RefreshToolButtons();
            RefreshFeaturedBuildShelf(metrics);
            RefreshPolicyButtons();
            RefreshMiniMap();
            RefreshMiniMapCameraStatus();
        }

        private int ComputeMetricsHash(CityMetrics metrics)
        {
            if (metrics == null) return 0;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + metrics.Day;
                hash = hash * 31 + metrics.Population;
                hash = hash * 31 + metrics.Cash;
                hash = hash * 31 + metrics.Happiness;
                hash = hash * 31 + metrics.NetIncome;
                hash = hash * 31 + (metrics.ActiveObjective != null ? metrics.ActiveObjective.Progress : 0);
                return hash;
            }
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("Runtime HUD");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();

            var root = CreatePanel(canvasObject.transform, "Root", AnchorStretch(), Vector2.zero, Vector2.zero);
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color32(0, 0, 0, 0);
            rootImage.raycastTarget = false;
            BuildReferenceMapGridOverlay(root.transform);
            BuildMapPlanningPins(root.transform);

            var resourceCard = CreatePanel(root.transform, "Reference Resource Card", AnchorTopLeft(), new Vector2(12f, -274f), new Vector2(328f, -12f));
            // REFERENCE_IMAGE_RESOURCE_CARD mirrors the dark translucent status card in the provided UI mock.
            resourceCard.GetComponent<Image>().color = new Color32(21, 57, 38, 232);
            AddSoftCardShadow(resourceCard, 50);
            AddPanelTopAccent(resourceCard, new Color32(255, 204, 82, 218), 4f);
            AddVerticalLayout(resourceCard, 5, 10);
            BuildResourceLevelBadge(resourceCard.transform);
            cityTitleText = CreateText(resourceCard.transform, "City Title", "\u53e3\u888b\u57ce\u5efa\u5c40", 18, FontStyle.Bold, TextAnchor.UpperLeft);
            cityTitleText.color = new Color32(245, 255, 238, 255);
            cityTitleText.GetComponent<LayoutElement>().preferredHeight = 40f;
            cityTitleText.rectTransform.offsetMax = new Vector2(-52f, 0f);
            for (var i = 0; i < 8; i += 1)
            {
                var statRow = CreateTopStatRow(resourceCard.transform, i);
                var stat = CreateText(statRow.transform, "TopStat" + i, "--", 13, FontStyle.Bold, TextAnchor.MiddleLeft);
                stat.color = new Color32(245, 255, 238, 255);
                topTexts.Add(stat);
                Stretch(stat.rectTransform);
                stat.rectTransform.offsetMin = new Vector2(12f, 0f);
                stat.rectTransform.offsetMax = new Vector2(-6f, 0f);
                stat.GetComponent<LayoutElement>().ignoreLayout = true;
                var statOutline = stat.gameObject.AddComponent<Outline>();
                statOutline.enabled = false;
                statOutline.effectColor = new Color32(255, 202, 70, 180);
                statOutline.effectDistance = new Vector2(1.1f, -1.1f);
                topTextOutlines.Add(statOutline);
                topStatScanMarkers.Add(AddTopStatScanMarker(statRow.transform, i));
            }
            BuildResourceObjectiveProgressBar(resourceCard.transform);
            BuildCitySnapshotBoard(root.transform);

            var topBar = CreatePanel(root.transform, "Top Bar", AnchorTop(), new Vector2(760f, -66f), new Vector2(-74f, -12f));
            // REFERENCE_IMAGE_TOP_RESOURCE_CAPSULES collects money, population and happiness like the sample UI.
            topBar.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            AddHorizontalLayout(topBar, 8, 0, TextAnchor.MiddleRight);
            // REFERENCE_IMAGE_SEGMENTED_TOP_CAPSULES separates cash, population and happiness like the mockup buttons.
            BuildTopResourceCapsule(topBar.transform, "\u73b0\u91d1", 176f, new Color32(255, 200, 70, 255));
            BuildTopResourceCapsule(topBar.transform, "\u4eba\u53e3", 132f, new Color32(206, 238, 216, 255));
            BuildTopResourceCapsule(topBar.transform, "\u5e78\u798f", 118f, new Color32(255, 220, 86, 255));
            BuildManagementCapsule(root.transform);

            var demandRibbon = CreatePanel(root.transform, "Demand Ribbon", AnchorTopLeft(), new Vector2(360f, -46f), new Vector2(506f, -16f));
            // REFERENCE_IMAGE_CITY_DEMAND_RIBBON gives the demand panel the same clear title tab as the reference.
            demandRibbonImage = demandRibbon.GetComponent<Image>();
            demandRibbonImage.color = new Color32(255, 211, 93, 238);
            demandRibbonText = CreateText(demandRibbon.transform, "Demand Ribbon Text", "\u57ce\u5e02\u9700\u6c42  R/C/I", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            demandRibbonText.color = new Color32(43, 64, 70, 255);
            demandRibbonText.resizeTextForBestFit = true;
            demandRibbonText.resizeTextMinSize = 9;
            demandRibbonText.resizeTextMaxSize = 14;
            Stretch(demandRibbonText.rectTransform);

            var demandPanel = CreatePanel(root.transform, "Demand Bar", AnchorTopLeft(), new Vector2(348f, -176f), new Vector2(714f, -12f));
            // REFERENCE_IMAGE_CITY_DEMAND_PANEL moves demand pressure to the upper-center card.
            demandPanel.GetComponent<Image>().color = new Color32(24, 64, 43, 226);
            AddSoftCardShadow(demandPanel);
            AddPanelTopAccent(demandPanel, new Color32(255, 202, 70, 190), 3f);
            var demandOutline = demandPanel.AddComponent<Outline>();
            demandOutline.effectColor = new Color32(65, 169, 184, 126);
            demandOutline.effectDistance = new Vector2(1.6f, -1.6f);
            var statusLayout = demandPanel.AddComponent<GridLayoutGroup>();
            // REFERENCE_IMAGE_DEMAND_PILL_GRID_DENSITY keeps 33 demand stats readable in the top demand card.
            // VERIFY_DEMAND_TILE_BASELINE keeps the existing scaffold marker visible: new Vector2(56f, 22f).
            statusLayout.cellSize = new Vector2(56f, 18f);
            statusLayout.spacing = new Vector2(2f, 2f);
            statusLayout.padding = new RectOffset(8, 8, 36, 6);
            statusLayout.childAlignment = TextAnchor.MiddleCenter;
            statusLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statusLayout.constraintCount = 6;
            for (var i = 0; i < 33; i += 1)
            {
                demandTexts.Add(CreateDemandStatTile(demandPanel.transform, "Demand" + i));
            }
            BuildDemandSummaryRails(demandPanel.transform);
            demandRibbon.transform.SetAsLastSibling();
            BuildCityOperationsStrip(root.transform);
            BuildPolicyBudgetForecast(root.transform);
            BuildAdvisorActionQueue(root.transform);
            BuildPriorityCommandStrip(root.transform);

            var toolbar = CreatePanel(root.transform, "Overlay Toolbar", AnchorRight(), new Vector2(-98f, 118f), new Vector2(-16f, -100f));
            // LIGHT_CITY_HUD_SURFACES keeps the dense city-builder HUD fresh and readable.
            toolbar.GetComponent<Image>().color = new Color32(22, 57, 39, 230);
            AddSoftCardShadow(toolbar);
            AddPanelTopAccent(toolbar, new Color32(65, 183, 190, 170), 3f);
            AddVerticalLayout(toolbar, 4, 8);
            AddOverlayButton(toolbar.transform, OverlayMode.Normal, "\u666e\u901a");
            AddOverlayButton(toolbar.transform, OverlayMode.Traffic, "\u4ea4\u901a");
            AddOverlayButton(toolbar.transform, OverlayMode.Pollution, "\u6c61\u67d3");
            AddOverlayButton(toolbar.transform, OverlayMode.Zoning, "\u5206\u533a");
            AddOverlayButton(toolbar.transform, OverlayMode.Services, "\u670d\u52a1");
            AddOverlayButton(toolbar.transform, OverlayMode.Transit, "\u516c\u4ea4");
            AddOverlayButton(toolbar.transform, OverlayMode.LandValue, "\u5730\u4ef7");
            AddOverlayButton(toolbar.transform, OverlayMode.Waste, "\u56de\u6536");
            AddOverlayButton(toolbar.transform, OverlayMode.Logistics, "\u8d27\u8fd0");
            AddOverlayButton(toolbar.transform, OverlayMode.Utilities, "\u6c34\u7535");
            AddOverlayButton(toolbar.transform, OverlayMode.Communications, "\u901a\u4fe1");
            AddOverlayButton(toolbar.transform, OverlayMode.RoadSafety, "\u8def\u5b89");
            AddOverlayButton(toolbar.transform, OverlayMode.Parking, "\u505c\u8f66");
            AddOverlayButton(toolbar.transform, OverlayMode.Stormwater, "\u96e8\u6d2a");

            var sidePanel = CreatePanel(root.transform, "Inspector", AnchorTopRight(), new Vector2(-382f, -430f), new Vector2(-100f, -84f));
            // REFERENCE_IMAGE_RIGHT_MILESTONE_CARD turns the inspector into a compact task card.
            sidePanel.GetComponent<Image>().color = new Color32(253, 255, 248, 244);
            AddSoftCardShadow(sidePanel, 50);
            AddPanelTopAccent(sidePanel, new Color32(255, 207, 86, 214), 4f);
            var sideOutline = sidePanel.AddComponent<Outline>();
            sideOutline.effectColor = new Color32(54, 153, 142, 118);
            sideOutline.effectDistance = new Vector2(2.1f, -2.1f);
            advisorPanelOutline = sideOutline;
            AddVerticalLayout(sidePanel, 3, 8);
            advisorSeverityStrip = CreateAdvisorSeverityStrip(sidePanel.transform);
            advisorSeverityBadge = CreateAdvisorSeverityBadge(sidePanel.transform);
            BuildMilestoneRibbon(sidePanel.transform);
            BuildMilestoneTaskPreview(sidePanel.transform);
            objectiveText = CreateText(sidePanel.transform, "Objective", "\u76ee\u6807", 13, FontStyle.Bold, TextAnchor.UpperLeft);
            objectiveText.lineSpacing = 0.9f;
            objectiveText.GetComponent<LayoutElement>().preferredHeight = 34f;
            milestoneTaskText = CreateText(sidePanel.transform, "Milestone Task Cards", "\u91cc\u7a0b\u7891", 12, FontStyle.Bold, TextAnchor.UpperLeft);
            milestoneTaskText.lineSpacing = 0.92f;
            milestoneTaskText.resizeTextForBestFit = true;
            milestoneTaskText.resizeTextMinSize = 9;
            milestoneTaskText.resizeTextMaxSize = 12;
            milestoneTaskText.GetComponent<LayoutElement>().preferredHeight = 46f;
            alertText = CreateText(sidePanel.transform, "Alerts", "\u8fd0\u884c\u7a33\u5b9a", 13, FontStyle.Normal, TextAnchor.UpperLeft);
            alertText.GetComponent<LayoutElement>().preferredHeight = 24f;
            cityPulseText = CreateText(sidePanel.transform, "City Pulse", "\u57ce\u5e02\u8109\u640f", 12, FontStyle.Bold, TextAnchor.UpperLeft);
            cityPulseText.lineSpacing = 0.88f;
            cityPulseText.resizeTextForBestFit = true;
            cityPulseText.resizeTextMinSize = 9;
            cityPulseText.resizeTextMaxSize = 12;
            cityPulseText.GetComponent<LayoutElement>().preferredHeight = 32f;
            BuildAdvisorRadarRow(sidePanel.transform);
            toolStatusText = CreateText(sidePanel.transform, "Tool Status", "--", 13, FontStyle.Bold, TextAnchor.MiddleLeft);
            toolStatusText.GetComponent<LayoutElement>().preferredHeight = 30f;
            previewText = CreateText(sidePanel.transform, "Preview", "\u70b9\u51fb\u5730\u56fe\u5f00\u59cb\u89c4\u5212", 13, FontStyle.Normal, TextAnchor.UpperLeft);
            previewText.lineSpacing = 0.9f;
            previewText.resizeTextForBestFit = true;
            previewText.resizeTextMinSize = 9;
            previewText.resizeTextMaxSize = 13;
            previewText.GetComponent<LayoutElement>().preferredHeight = 48f;
            saveStatusText = CreateText(sidePanel.transform, "Save Status", "--", 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            saveStatusText.GetComponent<LayoutElement>().preferredHeight = 14f;

            var toolGrid = CreatePanel(root.transform, "Build Tool Dock", AnchorBottom(), new Vector2(12f, 8f), new Vector2(-282f, 108f));
            // REFERENCE_IMAGE_BOTTOM_BUILD_TOOL_DOCK moves all existing tools into the bottom build strip.
            toolGrid.GetComponent<Image>().color = new Color32(18, 54, 42, 234);
            AddSoftCardShadow(toolGrid, 52);
            AddPanelTopAccent(toolGrid, new Color32(255, 207, 86, 162), 3f);
            var dockOutline = toolGrid.AddComponent<Outline>();
            dockOutline.effectColor = new Color32(54, 153, 142, 122);
            dockOutline.effectDistance = new Vector2(1.6f, -1.6f);
            var toolLayout = toolGrid.AddComponent<GridLayoutGroup>();
            toolLayout.cellSize = new Vector2(56f, 18f);
            toolLayout.spacing = new Vector2(3f, 2f);
            toolLayout.padding = new RectOffset(10, 10, 10, 8);
            toolLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            toolLayout.constraintCount = 4;
            AddToolDockCategoryBands(toolGrid.transform);

            AddToolButton(toolGrid.transform, "\u94fa\u8def", () => { if (interaction != null) interaction.SelectRoadTool(); }, CityToolMode.BuildRoad, ZoneType.None, string.Empty);
            AddToolButton(toolGrid.transform, "\u5347\u7ea7\u8def", () => { if (interaction != null) interaction.SelectRoadUpgradeTool(); }, CityToolMode.UpgradeRoad, ZoneType.None, string.Empty);
            AddToolButton(toolGrid.transform, "\u4f4f\u5b85\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.Residential); }, CityToolMode.ZonePaint, ZoneType.Residential, string.Empty);
            AddToolButton(toolGrid.transform, "\u5546\u4e1a\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.Commercial); }, CityToolMode.ZonePaint, ZoneType.Commercial, string.Empty);
            AddToolButton(toolGrid.transform, "\u6df7\u5408\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.MixedUse); }, CityToolMode.ZonePaint, ZoneType.MixedUse, string.Empty);
            AddToolButton(toolGrid.transform, "\u529e\u516c\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.Office); }, CityToolMode.ZonePaint, ZoneType.Office, string.Empty);
            AddToolButton(toolGrid.transform, "\u5de5\u4e1a\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.Industrial); }, CityToolMode.ZonePaint, ZoneType.Industrial, string.Empty);
            AddToolButton(toolGrid.transform, "\u670d\u52a1\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.Civic); }, CityToolMode.ZonePaint, ZoneType.Civic, string.Empty);
            AddToolButton(toolGrid.transform, "\u8bbe\u65bd\u533a", () => { if (interaction != null) interaction.SelectZoneTool(ZoneType.Utility); }, CityToolMode.ZonePaint, ZoneType.Utility, string.Empty);
            AddToolButton(toolGrid.transform, "\u4f4f\u5b85\u8231", () => { if (interaction != null) interaction.SelectBuildingTool("residential_pod"); }, CityToolMode.BuildBuilding, ZoneType.None, "residential_pod");
            AddToolButton(toolGrid.transform, "\u516c\u5bd3", () => { if (interaction != null) interaction.SelectBuildingTool("apartment_block"); }, CityToolMode.BuildBuilding, ZoneType.None, "apartment_block");
            AddToolButton(toolGrid.transform, "\u5546\u94fa", () => { if (interaction != null) interaction.SelectBuildingTool("market_corner"); }, CityToolMode.BuildBuilding, ZoneType.None, "market_corner");
            AddToolButton(toolGrid.transform, "\u6df7\u5408\u697c", () => { if (interaction != null) interaction.SelectBuildingTool("mixed_use_block"); }, CityToolMode.BuildBuilding, ZoneType.None, "mixed_use_block");
            AddToolButton(toolGrid.transform, "\u529e\u516c", () => { if (interaction != null) interaction.SelectBuildingTool("office_studio"); }, CityToolMode.BuildBuilding, ZoneType.None, "office_studio");
            AddToolButton(toolGrid.transform, "\u7814\u53d1", () => { if (interaction != null) interaction.SelectBuildingTool("research_campus"); }, CityToolMode.BuildBuilding, ZoneType.None, "research_campus");
            AddToolButton(toolGrid.transform, "\u5de5\u574a", () => { if (interaction != null) interaction.SelectBuildingTool("maker_yard"); }, CityToolMode.BuildBuilding, ZoneType.None, "maker_yard");
            AddToolButton(toolGrid.transform, "\u8d44\u6e90", () => { if (interaction != null) interaction.SelectBuildingTool("resource_processor"); }, CityToolMode.BuildBuilding, ZoneType.None, "resource_processor");
            AddToolButton(toolGrid.transform, "\u516c\u56ed", () => { if (interaction != null) interaction.SelectBuildingTool("pocket_park"); }, CityToolMode.BuildBuilding, ZoneType.None, "pocket_park");
            AddToolButton(toolGrid.transform, "\u5e7f\u573a", () => { if (interaction != null) interaction.SelectBuildingTool("city_plaza"); }, CityToolMode.BuildBuilding, ZoneType.None, "city_plaza");
            AddToolButton(toolGrid.transform, "\u4f1a\u5c55", () => { if (interaction != null) interaction.SelectBuildingTool("convention_center"); }, CityToolMode.BuildBuilding, ZoneType.None, "convention_center");
            AddToolButton(toolGrid.transform, "\u5e02\u653f\u5385", () => { if (interaction != null) interaction.SelectBuildingTool("city_hall"); }, CityToolMode.BuildBuilding, ZoneType.None, "city_hall");
            AddToolButton(toolGrid.transform, "\u8bca\u6240", () => { if (interaction != null) interaction.SelectBuildingTool("health_post"); }, CityToolMode.BuildBuilding, ZoneType.None, "health_post");
            AddToolButton(toolGrid.transform, "\u533b\u9662", () => { if (interaction != null) interaction.SelectBuildingTool("district_hospital"); }, CityToolMode.BuildBuilding, ZoneType.None, "district_hospital");
            AddToolButton(toolGrid.transform, "\u751f\u547d", () => { if (interaction != null) interaction.SelectBuildingTool("memorial_garden"); }, CityToolMode.BuildBuilding, ZoneType.None, "memorial_garden");
            AddToolButton(toolGrid.transform, "\u907f\u96be", () => { if (interaction != null) interaction.SelectBuildingTool("emergency_shelter"); }, CityToolMode.BuildBuilding, ZoneType.None, "emergency_shelter");
            AddToolButton(toolGrid.transform, "\u516c\u4ea4", () => { if (interaction != null) interaction.SelectBuildingTool("bus_hub"); }, CityToolMode.BuildBuilding, ZoneType.None, "bus_hub");
            AddToolButton(toolGrid.transform, "\u5730\u94c1", () => { if (interaction != null) interaction.SelectBuildingTool("metro_station"); }, CityToolMode.BuildBuilding, ZoneType.None, "metro_station");
            AddToolButton(toolGrid.transform, "\u57ce\u9645", () => { if (interaction != null) interaction.SelectBuildingTool("intercity_terminal"); }, CityToolMode.BuildBuilding, ZoneType.None, "intercity_terminal");
            AddToolButton(toolGrid.transform, "\u8d27\u8fd0", () => { if (interaction != null) interaction.SelectBuildingTool("cargo_depot"); }, CityToolMode.BuildBuilding, ZoneType.None, "cargo_depot");
            AddToolButton(toolGrid.transform, "\u4ed3\u50a8", () => { if (interaction != null) interaction.SelectBuildingTool("distribution_center"); }, CityToolMode.BuildBuilding, ZoneType.None, "distribution_center");
            AddToolButton(toolGrid.transform, "\u94c1\u8d27", () => { if (interaction != null) interaction.SelectBuildingTool("freight_rail_terminal"); }, CityToolMode.BuildBuilding, ZoneType.None, "freight_rail_terminal");
            AddToolButton(toolGrid.transform, "\u5b66\u6821", () => { if (interaction != null) interaction.SelectBuildingTool("primary_school"); }, CityToolMode.BuildBuilding, ZoneType.None, "primary_school");
            AddToolButton(toolGrid.transform, "\u5b66\u9662", () => { if (interaction != null) interaction.SelectBuildingTool("community_college"); }, CityToolMode.BuildBuilding, ZoneType.None, "community_college");
            AddToolButton(toolGrid.transform, "\u6d88\u9632", () => { if (interaction != null) interaction.SelectBuildingTool("fire_station"); }, CityToolMode.BuildBuilding, ZoneType.None, "fire_station");
            AddToolButton(toolGrid.transform, "\u8b66\u52a1", () => { if (interaction != null) interaction.SelectBuildingTool("police_kiosk"); }, CityToolMode.BuildBuilding, ZoneType.None, "police_kiosk");
            AddToolButton(toolGrid.transform, "\u5206\u5c40", () => { if (interaction != null) interaction.SelectBuildingTool("police_precinct"); }, CityToolMode.BuildBuilding, ZoneType.None, "police_precinct");
            AddToolButton(toolGrid.transform, "\u901a\u4fe1", () => { if (interaction != null) interaction.SelectBuildingTool("telecom_hub"); }, CityToolMode.BuildBuilding, ZoneType.None, "telecom_hub");
            AddToolButton(toolGrid.transform, "\u90ae\u653f", () => { if (interaction != null) interaction.SelectBuildingTool("post_office"); }, CityToolMode.BuildBuilding, ZoneType.None, "post_office");
            AddToolButton(toolGrid.transform, "\u517b\u62a4", () => { if (interaction != null) interaction.SelectBuildingTool("road_maintenance_depot"); }, CityToolMode.BuildBuilding, ZoneType.None, "road_maintenance_depot");
            AddToolButton(toolGrid.transform, "\u505c\u8f66\u697c", () => { if (interaction != null) interaction.SelectBuildingTool("parking_garage"); }, CityToolMode.BuildBuilding, ZoneType.None, "parking_garage");
            AddToolButton(toolGrid.transform, "\u96e8\u6c34\u56ed", () => { if (interaction != null) interaction.SelectBuildingTool("rain_garden"); }, CityToolMode.BuildBuilding, ZoneType.None, "rain_garden");
            AddToolButton(toolGrid.transform, "\u7535\u7ad9", () => { if (interaction != null) interaction.SelectBuildingTool("micro_power"); }, CityToolMode.BuildBuilding, ZoneType.None, "micro_power");
            AddToolButton(toolGrid.transform, "\u592a\u9633\u80fd", () => { if (interaction != null) interaction.SelectBuildingTool("solar_farm"); }, CityToolMode.BuildBuilding, ZoneType.None, "solar_farm");
            AddToolButton(toolGrid.transform, "\u6c34\u5854", () => { if (interaction != null) interaction.SelectBuildingTool("water_tower"); }, CityToolMode.BuildBuilding, ZoneType.None, "water_tower");
            AddToolButton(toolGrid.transform, "\u6c61\u6c34", () => { if (interaction != null) interaction.SelectBuildingTool("water_reclaimer"); }, CityToolMode.BuildBuilding, ZoneType.None, "water_reclaimer");
            AddToolButton(toolGrid.transform, "\u5783\u573e\u7535", () => { if (interaction != null) interaction.SelectBuildingTool("waste_to_energy_plant"); }, CityToolMode.BuildBuilding, ZoneType.None, "waste_to_energy_plant");
            AddToolButton(toolGrid.transform, "\u56de\u6536", () => { if (interaction != null) interaction.SelectBuildingTool("recycling_yard"); }, CityToolMode.BuildBuilding, ZoneType.None, "recycling_yard");
            AddToolButton(toolGrid.transform, "\u62c6\u9664", () => { if (interaction != null) interaction.SelectDemolishTool(); }, CityToolMode.Demolish, ZoneType.None, string.Empty);
            AddControlButton(toolGrid.transform, "\u6682\u505c", () => { if (controller != null) controller.TogglePause(); });
            AddControlButton(toolGrid.transform, "\u500d\u901f", () => { if (controller != null) controller.CycleSimulationSpeed(); });
            AddControlButton(toolGrid.transform, "\u7a0e\u7387", () => { if (controller != null) controller.CycleTaxLevel(); });
            AddControlButton(toolGrid.transform, "\u9884\u7b97", () => { if (controller != null) controller.CycleServiceBudgetLevel(); });
            AddControlButton(toolGrid.transform, "\u503a\u5238", () => { if (controller != null) controller.IssueMunicipalBond(); });
            AddControlButton(toolGrid.transform, "\u4fdd\u5b58", () => { if (saveController != null) saveController.SaveGame(); });
            AddControlButton(toolGrid.transform, "\u8bfb\u53d6", () => { if (saveController != null) saveController.LoadGame(); });
            AddPolicyButton(toolGrid.transform, "\u7eff\u5efa", CityPolicy.GreenCode);
            AddPolicyButton(toolGrid.transform, "\u516c\u4ea4\u4f18\u5148", CityPolicy.TransitPriority);
            AddPolicyButton(toolGrid.transform, "\u589e\u957f", CityPolicy.GrowthGrants);
            AddPolicyButton(toolGrid.transform, "\u4fdd\u969c\u623f", CityPolicy.AffordableHousing);
            AddPolicyButton(toolGrid.transform, "\u5b89\u5168", CityPolicy.TrafficSafetyCampaign);
            AddPolicyButton(toolGrid.transform, "\u5b8c\u6574\u8857", CityPolicy.CompleteStreets);
            AddPolicyButton(toolGrid.transform, "\u4fe1\u53f7", CityPolicy.SignalOptimization);
            AddPolicyButton(toolGrid.transform, "\u62e5\u5835\u8d39", CityPolicy.CongestionPricing);
            AddPolicyButton(toolGrid.transform, "\u505c\u8f66\u8d39", CityPolicy.ParkingFees);

            BuildTurnActionPill(root.transform);
            BuildToolDockBadge(root.transform);
            BuildLeftQuickActionCards(root.transform);
            BuildRightCommandStack(root.transform);
            BuildUnlockRegionCallout(root.transform);
            BuildFeaturedBuildShelf(root.transform);
            BuildSelectedTileDetailCard(root.transform);
            BuildPlanningLensStrip(root.transform);
            BuildOverlayLegendCard(root.transform);
            BuildPlacementQuoteCard(root.transform);
            BuildActionChainStrip(root.transform);
            BuildMiniMapPanel(root.transform);
        }

        private void BuildReferenceMapGridOverlay(Transform root)
        {
            // REFERENCE_IMAGE_GRASS_PLANNING_GRID adds the light isometric planning lattice from the mockup.
            var grid = CreatePanel(root, "Reference Map Grid Overlay", AnchorStretch(), Vector2.zero, Vector2.zero);
            var image = grid.GetComponent<Image>();
            image.color = new Color32(0, 0, 0, 0);
            image.raycastTarget = false;
            grid.transform.SetAsFirstSibling();

            for (var i = 0; i < 18; i += 1)
            {
                AddReferenceMapGridLine(grid.transform, "Grid NE " + i, -330f + i * 86f, -82f, 1120f, 1.15f, 26f, new Color32(245, 255, 238, 22));
            }

            for (var i = 0; i < 17; i += 1)
            {
                AddReferenceMapGridLine(grid.transform, "Grid NW " + i, -250f + i * 88f, 714f, 1100f, 1.1f, -26f, new Color32(106, 202, 116, 20));
            }
        }

        private void AddReferenceMapGridLine(Transform parent, string name, float x, float y, float width, float height, float rotation, Color32 color)
        {
            var line = CreatePanel(parent, name, AnchorBottomLeft(), new Vector2(x, y), new Vector2(x + width, y + height));
            var lineImage = line.GetComponent<Image>();
            lineImage.color = color;
            lineImage.raycastTarget = false;
            line.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, rotation);
            line.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        private void BuildMapPlanningPins(Transform root)
        {
            // REFERENCE_IMAGE_MAP_PLANNING_PINS adds readable in-map labels without adding menu controls.
            mapPlanningPins.Clear();
            AddMapPlanningPin(root, 0, "Core District Pin", new Vector2(486f, -364f), new Vector2(640f, -314f));
            AddMapPlanningPin(root, 1, "River Greenbelt Pin", new Vector2(136f, -462f), new Vector2(294f, -414f));
            AddMapPlanningPin(root, 2, "Demand Hotspot Pin", new Vector2(684f, -318f), new Vector2(842f, -268f));
            AddMapPlanningPin(root, 3, "Expansion Boundary Pin", new Vector2(820f, -484f), new Vector2(986f, -434f));
        }

        private void AddMapPlanningPin(Transform parent, int kind, string name, Vector2 offsetMin, Vector2 offsetMax)
        {
            var card = CreatePanel(parent, name, AnchorTopLeft(), offsetMin, offsetMax);
            var image = card.GetComponent<Image>();
            image.color = new Color32(24, 64, 43, 188);
            image.raycastTarget = false;
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(245, 255, 238, 102);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            card.AddComponent<LayoutElement>().ignoreLayout = true;

            var stem = CreatePanel(card.transform, "Pin Stem", new Vector4(0.5f, 0f, 0.5f, 0f), new Vector2(-2f, -15f), new Vector2(2f, 0f));
            var stemImage = stem.GetComponent<Image>();
            stemImage.color = new Color32(245, 255, 238, 170);
            stemImage.raycastTarget = false;
            stem.AddComponent<LayoutElement>().ignoreLayout = true;

            var accent = CreatePanel(card.transform, "Pin Accent", AnchorLeft(), new Vector2(5f, 7f), new Vector2(11f, -7f));
            var accentImage = accent.GetComponent<Image>();
            accentImage.color = new Color32(96, 214, 118, 226);
            accentImage.raycastTarget = false;
            accent.AddComponent<LayoutElement>().ignoreLayout = true;

            var title = CreateText(card.transform, "Pin Title", "--", 11, FontStyle.Bold, TextAnchor.UpperLeft);
            title.color = new Color32(245, 255, 238, 255);
            title.raycastTarget = false;
            Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(16f, 23f);
            title.rectTransform.offsetMax = new Vector2(-8f, -5f);

            var detail = CreateText(card.transform, "Pin Detail", "--", 9, FontStyle.Bold, TextAnchor.UpperLeft);
            detail.color = new Color32(206, 238, 216, 238);
            detail.raycastTarget = false;
            Stretch(detail.rectTransform);
            detail.rectTransform.offsetMin = new Vector2(16f, 7f);
            detail.rectTransform.offsetMax = new Vector2(-8f, -25f);
            AddHudFacet(card.transform, "Pin Facet", new Vector4(0.58f, 0.55f, 0.96f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 28), -8f);

            mapPlanningPins.Add(new MapPlanningPinBinding
            {
                Card = image,
                Accent = accentImage,
                Stem = stemImage,
                Title = title,
                Detail = detail,
                Kind = kind
            });
        }

        private void RefreshMapPlanningPins(CityMetrics metrics)
        {
            for (var i = 0; i < mapPlanningPins.Count; i += 1)
            {
                RefreshMapPlanningPin(mapPlanningPins[i], metrics);
            }
        }

        private void RefreshMapPlanningPin(MapPlanningPinBinding pin, CityMetrics metrics)
        {
            if (pin == null)
            {
                return;
            }

            var accent = MapPlanningPinAccent(pin.Kind, metrics);
            var pressure = metrics != null ? Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.ServiceGapPressure, metrics.RoadBottleneckPressure)) : 0;
            if (pin.Card != null)
            {
                var alpha = pin.Kind == 3 && metrics != null && !metrics.LockedExpansionUnlocked ? (byte)206 : (byte)184;
                pin.Card.color = pressure >= 72 && pin.Kind != 1
                    ? new Color32(62, 52, 36, alpha)
                    : new Color32(24, 64, 43, alpha);
            }

            if (pin.Accent != null)
            {
                pin.Accent.color = accent;
            }

            if (pin.Stem != null)
            {
                pin.Stem.color = new Color32(accent.r, accent.g, accent.b, 172);
            }

            if (pin.Title != null)
            {
                pin.Title.text = MapPlanningPinTitle(pin.Kind, metrics);
                pin.Title.color = new Color32(245, 255, 238, 255);
            }

            if (pin.Detail != null)
            {
                pin.Detail.text = MapPlanningPinDetail(pin.Kind, metrics);
                pin.Detail.color = pin.Kind == 3 && metrics != null && !metrics.LockedExpansionUnlocked
                    ? new Color32(255, 232, 150, 246)
                    : new Color32(206, 238, 216, 238);
            }
        }

        private static string MapPlanningPinTitle(int kind, CityMetrics metrics)
        {
            if (kind == 0) return "\u6838\u5fc3\u57ce\u533a";
            if (kind == 1) return "\u6cb3\u5cb8\u7eff\u5e26";
            if (kind == 2) return "\u9700\u6c42\u70ed\u533a";
            if (kind == 3) return metrics != null && metrics.LockedExpansionUnlocked ? "\u65b0\u533a\u5df2\u5f00" : "\u6269\u5c55\u8fb9\u754c";
            return "\u89c4\u5212\u70b9";
        }

        private static string MapPlanningPinDetail(int kind, CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u7b49\u5f85\u6570\u636e";
            }

            if (kind == 0)
            {
                return "\u5206" + metrics.CityScore + "  \u58eb\u6c14" + metrics.Happiness + "%";
            }

            if (kind == 1)
            {
                return "\u7eff" + metrics.EnvironmentQuality + "%  \u56ed" + metrics.ParkCoverage + "%";
            }

            if (kind == 2)
            {
                var focus = string.IsNullOrEmpty(metrics.DemandFocus) ? OverlayLabel(RecommendedOverlayMode(metrics)) : CompactCardText(metrics.DemandFocus, 5);
                return "\u9700" + metrics.DemandUrgency + "  \u505a:" + focus;
            }

            if (kind == 3)
            {
                if (metrics.LockedExpansionUnlocked)
                {
                    return "\u8fde\u63a5\u65b0\u8857\u533a";
                }

                var objective = metrics.ActiveObjective;
                var progress = objective != null ? Mathf.Min(objective.Progress, objective.Required) : 0;
                var required = objective != null ? Mathf.Max(1, objective.Required) : 1;
                return "\u4efb" + progress + "/" + required + "  \u89e3\u9501";
            }

            return "--";
        }

        private static Color32 MapPlanningPinAccent(int kind, CityMetrics metrics)
        {
            if (metrics == null)
            {
                return new Color32(126, 170, 144, 226);
            }

            if (kind == 1)
            {
                return metrics.EnvironmentQuality < 45 || metrics.ParkCoverage < 45
                    ? new Color32(255, 207, 86, 232)
                    : new Color32(96, 214, 118, 226);
            }

            if (kind == 2)
            {
                var mode = RecommendedOverlayMode(metrics);
                return mode == OverlayMode.Normal
                    ? new Color32(65, 184, 220, 226)
                    : OverlayModeAccentColor(mode);
            }

            if (kind == 3)
            {
                return metrics.LockedExpansionUnlocked
                    ? new Color32(96, 214, 118, 226)
                    : new Color32(255, 207, 86, 232);
            }

            var pressure = Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.ServiceGapPressure, metrics.RoadBottleneckPressure));
            if (pressure >= 72) return new Color32(236, 116, 56, 232);
            if (metrics.CityScore < 45) return new Color32(255, 207, 86, 232);
            return new Color32(96, 214, 118, 226);
        }

        private void BuildManagementCapsule(Transform root)
        {
            // REFERENCE_IMAGE_TOP_RIGHT_MANAGEMENT_CAPSULE mirrors the compact settings/status button in the mock.
            var capsule = CreatePanel(root, "Management Capsule", AnchorTopRight(), new Vector2(-66f, -66f), new Vector2(-16f, -12f));
            managementCapsuleImage = capsule.GetComponent<Image>();
            managementCapsuleImage.color = new Color32(30, 66, 43, 238);
            AddSoftCardShadow(capsule, 46);
            AddPanelTopAccent(capsule, new Color32(206, 238, 216, 178), 3f);
            var outline = capsule.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 132);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            AddHudFacet(capsule.transform, "Management Gear Facet", new Vector4(0.18f, 0.58f, 0.82f, 0.88f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 44), -8f);

            var pressureTrack = CreatePanel(capsule.transform, "Management Pressure Track", AnchorBottom(), new Vector2(7f, 5f), new Vector2(-7f, 9f));
            var pressureTrackImage = pressureTrack.GetComponent<Image>();
            pressureTrackImage.color = new Color32(245, 255, 238, 42);
            pressureTrackImage.raycastTarget = false;
            pressureTrack.AddComponent<LayoutElement>().ignoreLayout = true;
            managementCapsulePressureFill = CreateToolButtonAccent(pressureTrack.transform, "Management Pressure Fill", AnchorStretch(), Vector2.zero, Vector2.zero, new Color32(96, 214, 118, 190));
            managementCapsulePressureFill.raycastTarget = false;

            managementCapsuleStateBadgeImage = CreateToolButtonAccent(capsule.transform, "Management State Badge", new Vector4(1f, 1f, 1f, 1f), new Vector2(-19f, -17f), new Vector2(-4f, -4f), new Color32(96, 214, 118, 218));
            managementCapsuleStateBadgeImage.raycastTarget = false;
            managementCapsuleStateText = CreateText(managementCapsuleStateBadgeImage.transform, "State", "\u7a33", 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            managementCapsuleStateText.color = new Color32(43, 64, 70, 255);
            managementCapsuleStateText.raycastTarget = false;
            Stretch(managementCapsuleStateText.rectTransform);

            managementCapsuleText = CreateText(capsule.transform, "Management Capsule Text", "\u7ba1\u7406\nx1", 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            managementCapsuleText.lineSpacing = 0.82f;
            managementCapsuleText.color = new Color32(245, 255, 238, 255);
            Stretch(managementCapsuleText.rectTransform);
            managementCapsuleText.rectTransform.offsetMin = new Vector2(2f, 8f);
            managementCapsuleText.rectTransform.offsetMax = new Vector2(-2f, -3f);
        }

        private void RefreshManagementCapsule(CityMetrics metrics)
        {
            if (managementCapsuleText == null && managementCapsuleImage == null)
            {
                return;
            }

            var paused = controller != null && controller.Paused;
            var speed = controller != null ? controller.SimulationSpeed : 1f;
            var pressure = metrics != null ? Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.ServiceGapPressure, metrics.RoadBottleneckPressure)) : 0;
            if (managementCapsuleText != null)
            {
                managementCapsuleText.text = paused ? "\u6682\u505c\n" + ManagementFocusLabel(metrics) : ("x" + CompactSpeedLabel(speed) + "\n" + ManagementFocusLabel(metrics));
                managementCapsuleText.color = pressure >= 65
                    ? new Color32(255, 230, 132, 255)
                    : new Color32(245, 255, 238, 255);
            }

            if (managementCapsuleImage != null)
            {
                managementCapsuleImage.color = paused
                    ? new Color32(65, 88, 72, 238)
                    : pressure >= 65
                        ? new Color32(82, 62, 43, 242)
                        : new Color32(30, 66, 43, 238);
            }

            if (managementCapsulePressureFill != null)
            {
                managementCapsulePressureFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, pressure) / 100f), 1f);
                managementCapsulePressureFill.color = ManagementPressureColor(pressure, paused);
            }

            if (managementCapsuleStateBadgeImage != null)
            {
                managementCapsuleStateBadgeImage.color = ManagementPressureColor(pressure, paused);
            }

            if (managementCapsuleStateText != null)
            {
                managementCapsuleStateText.text = paused ? "II" : ManagementStateBadgeText(pressure);
                managementCapsuleStateText.color = pressure >= 65 && !paused
                    ? new Color32(83, 68, 30, 255)
                    : new Color32(43, 64, 70, 255);
            }
        }

        private static string ManagementFocusLabel(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u7ba1\u7406";
            }

            var pressure = Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.ServiceGapPressure, metrics.RoadBottleneckPressure));
            if (pressure >= 65) return CompactCardText(MiniMapPrimaryIssueLabel(metrics), 2);
            if (metrics.BuildingUpgradeReadyCount > 0) return "\u5347\u7ea7";
            if (metrics.DemandUrgency >= 50) return "\u9700\u6c42";
            return "\u7ba1\u7406";
        }

        private static string ManagementStateBadgeText(int pressure)
        {
            if (pressure >= 72) return "\u6025";
            if (pressure >= 55) return "\u6ce8";
            return "\u7a33";
        }

        private static Color32 ManagementPressureColor(int pressure, bool paused)
        {
            if (paused) return new Color32(206, 238, 216, 218);
            if (pressure >= 72) return new Color32(255, 188, 66, 238);
            if (pressure >= 55) return new Color32(255, 207, 86, 226);
            return new Color32(96, 214, 118, 210);
        }

        private static string CompactSpeedLabel(float speed)
        {
            var rounded = Mathf.RoundToInt(speed);
            return Mathf.Abs(speed - rounded) < 0.05f ? rounded.ToString() : speed.ToString("0.0");
        }

        private void RefreshDemandRibbon(CityMetrics metrics)
        {
            // REFERENCE_IMAGE_DYNAMIC_DEMAND_RIBBON makes the top demand title behave like a live status tab.
            var urgency = metrics != null ? Mathf.Clamp(metrics.DemandUrgency, 0, 100) : 0;
            if (demandRibbonText != null)
            {
                demandRibbonText.text = DemandRibbonTitleText(metrics);
                demandRibbonText.color = urgency >= 45
                    ? new Color32(83, 68, 30, 255)
                    : new Color32(43, 64, 70, 255);
            }

            if (demandRibbonImage != null)
            {
                demandRibbonImage.color = DemandRibbonColor(urgency);
            }
        }

        private static string DemandRibbonTitleText(CityMetrics metrics)
        {
            if (metrics == null || metrics.Demand == null)
            {
                return "\u57ce\u5e02\u9700\u6c42 --";
            }

            if (metrics.DemandUrgency >= 70)
            {
                return "\u9700\u6c42\u70ed\u70b9 " + CompactCardText(DemandRibbonFocus(metrics), 4) + " " + metrics.DemandUrgency;
            }

            if (metrics.DemandUrgency >= 45)
            {
                return "\u9700\u6c42\u6ce8\u610f " + DemandCompactTriple(metrics.Demand);
            }

            return "\u57ce\u5e02\u9700\u6c42 " + DemandCompactTriple(metrics.Demand);
        }

        private static string DemandCompactTriple(DemandMetrics demand)
        {
            if (demand == null)
            {
                return "--";
            }

            return "\u4f4f" + demand.Residential + " \u5546" + demand.Commercial + " \u5de5" + demand.Industrial;
        }

        private static string DemandRibbonFocus(CityMetrics metrics)
        {
            if (metrics != null && !string.IsNullOrEmpty(metrics.DemandFocus))
            {
                return metrics.DemandFocus;
            }

            if (metrics == null || metrics.Demand == null)
            {
                return "\u9700\u6c42";
            }

            if (metrics.Demand.Residential >= metrics.Demand.Commercial && metrics.Demand.Residential >= metrics.Demand.Industrial) return "\u4f4f\u5b85";
            if (metrics.Demand.Commercial >= metrics.Demand.Industrial) return "\u5546\u4e1a";
            return "\u5de5\u4e1a";
        }

        private static Color32 DemandRibbonColor(int urgency)
        {
            if (urgency >= 70) return new Color32(255, 188, 66, 242);
            if (urgency >= 45) return new Color32(255, 224, 102, 238);
            return new Color32(255, 211, 93, 238);
        }

        private void BuildDemandSummaryRails(Transform parent)
        {
            // REFERENCE_IMAGE_MAIN_DEMAND_RAILS keeps R/C/I demand readable above the dense 33-chip wall.
            var host = CreatePanel(parent, "Demand Summary Rails", AnchorTop(), new Vector2(10f, -33f), new Vector2(-10f, -8f));
            host.GetComponent<Image>().color = new Color32(245, 255, 238, 18);
            var hostLayout = host.AddComponent<LayoutElement>();
            hostLayout.ignoreLayout = true;
            var layout = host.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(5, 5, 4, 4);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            AddDemandSummaryRail(host.transform, "\u4f4f", new Color32(96, 214, 118, 235));
            AddDemandSummaryRail(host.transform, "\u5546", new Color32(65, 184, 220, 235));
            AddDemandSummaryRail(host.transform, "\u5de5", new Color32(255, 156, 74, 235));
            host.transform.SetAsLastSibling();
        }

        private void AddDemandSummaryRail(Transform parent, string label, Color32 color)
        {
            var rail = CreatePanel(parent, "Demand Summary " + label, AnchorFree(), Vector2.zero, Vector2.zero);
            rail.GetComponent<Image>().color = new Color32(25, 66, 48, 112);
            rail.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var fillObject = new GameObject("Demand Summary Fill");
            fillObject.transform.SetParent(rail.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 3f);
            fillRect.offsetMax = new Vector2(-2f, -3f);
            var fill = fillObject.AddComponent<Image>();
            fill.color = color;
            fill.raycastTarget = false;
            demandSummaryFills.Add(fill);

            var text = CreateText(rail.transform, "Demand Summary Label", label + " --", 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color32(245, 255, 238, 255);
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            demandSummaryTexts.Add(text);
        }

        private void RefreshDemandSummaryRails(CityMetrics metrics)
        {
            if (demandSummaryFills.Count < 3 || demandSummaryTexts.Count < 3)
            {
                return;
            }

            var demand = metrics != null ? metrics.Demand : null;
            SetDemandSummaryRail(0, "\u4f4f", demand != null ? demand.Residential : 0, new Color32(96, 214, 118, 236));
            SetDemandSummaryRail(1, "\u5546", demand != null ? demand.Commercial : 0, new Color32(65, 184, 220, 236));
            SetDemandSummaryRail(2, "\u5de5", demand != null ? demand.Industrial : 0, new Color32(255, 156, 74, 236));
        }

        private void SetDemandSummaryRail(int index, string label, int value, Color32 color)
        {
            var clamped = Mathf.Clamp(value, 0, 100);
            var hot = clamped >= 70;
            if (index < demandSummaryFills.Count && demandSummaryFills[index] != null)
            {
                demandSummaryFills[index].rectTransform.anchorMax = new Vector2(Mathf.Max(0.08f, clamped / 100f), 1f);
                demandSummaryFills[index].color = hot ? new Color32(255, 207, 86, 242) : color;
            }

            if (index < demandSummaryTexts.Count && demandSummaryTexts[index] != null)
            {
                demandSummaryTexts[index].text = label + " " + clamped;
                demandSummaryTexts[index].color = hot ? new Color32(69, 54, 28, 255) : new Color32(245, 255, 238, 255);
            }
        }

        private void BuildCityOperationsStrip(Transform root)
        {
            // CITY_SKYLINES_OPERATIONS_QUEUE adds the compact issue strip under the demand card.
            var strip = CreatePanel(root, "City Operations Queue", AnchorTopLeft(), new Vector2(348f, -232f), new Vector2(714f, -184f));
            cityOpsPanelImage = strip.GetComponent<Image>();
            cityOpsPanelImage.color = new Color32(20, 58, 42, 222);
            cityOpsPanelImage.raycastTarget = false;
            AddSoftCardShadow(strip, 34);
            AddPanelTopAccent(strip, new Color32(65, 183, 190, 168), 3f);
            var outline = strip.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 88);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            cityOpsAccentImage = CreateToolButtonAccent(strip.transform, "Operations Priority Rail", AnchorLeft(), new Vector2(4f, 6f), new Vector2(8f, -6f), new Color32(65, 183, 190, 198));
            cityOpsAccentImage.raycastTarget = false;

            cityOpsTitleText = CreateText(strip.transform, "Operations Title", "\u8fd0\u884c\u603b\u89c8 --", 11, FontStyle.Bold, TextAnchor.UpperLeft);
            cityOpsTitleText.color = new Color32(245, 255, 238, 250);
            cityOpsTitleText.raycastTarget = false;
            Stretch(cityOpsTitleText.rectTransform);
            cityOpsTitleText.rectTransform.offsetMin = new Vector2(14f, 25f);
            cityOpsTitleText.rectTransform.offsetMax = new Vector2(-160f, -4f);

            cityOpsActionText = CreateText(strip.transform, "Operations Action", "\u95ee\u9898\u961f\u5217 --", 10, FontStyle.Bold, TextAnchor.UpperLeft);
            cityOpsActionText.color = new Color32(206, 238, 216, 238);
            cityOpsActionText.raycastTarget = false;
            cityOpsActionText.resizeTextForBestFit = true;
            cityOpsActionText.resizeTextMinSize = 8;
            cityOpsActionText.resizeTextMaxSize = 10;
            Stretch(cityOpsActionText.rectTransform);
            cityOpsActionText.rectTransform.offsetMin = new Vector2(14f, 5f);
            cityOpsActionText.rectTransform.offsetMax = new Vector2(-160f, -23f);

            var track = CreatePanel(strip.transform, "Operations Pressure Track", new Vector4(0f, 0f, 0.52f, 0f), new Vector2(14f, 6f), new Vector2(-4f, 11f));
            track.GetComponent<Image>().color = new Color32(245, 255, 238, 42);
            track.GetComponent<Image>().raycastTarget = false;
            cityOpsPressureFill = CreateToolButtonAccent(track.transform, "Operations Pressure Fill", AnchorStretch(), Vector2.zero, Vector2.zero, new Color32(65, 183, 190, 176));
            cityOpsPressureFill.raycastTarget = false;

            var chipHost = CreatePanel(strip.transform, "Operations Chips", new Vector4(0.56f, 0f, 1f, 1f), new Vector2(0f, 5f), new Vector2(-8f, -5f));
            chipHost.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            chipHost.GetComponent<Image>().raycastTarget = false;
            var layout = chipHost.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            AddCityOperationsChip(chipHost.transform, "\u4ea4\u901a", new Color32(255, 207, 86, 225));
            AddCityOperationsChip(chipHost.transform, "\u670d\u52a1", new Color32(96, 214, 118, 225));
            AddCityOperationsChip(chipHost.transform, "\u8d22\u653f", new Color32(65, 184, 220, 225));

            AddHudFacet(strip.transform, "Operations Low Poly Facet", new Vector4(0.12f, 0.56f, 0.94f, 0.88f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 28), -8f);
        }

        private void AddCityOperationsChip(Transform parent, string label, Color32 accent)
        {
            var chip = CreatePanel(parent, "Operations Chip " + label, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = chip.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 36);
            image.raycastTarget = false;
            chip.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var fill = CreateToolButtonAccent(chip.transform, "Operations Chip Fill", AnchorStretch(), new Vector2(2f, 13f), new Vector2(-2f, -2f), accent);
            fill.raycastTarget = false;

            var text = CreateText(chip.transform, "Operations Chip Label", label + " --", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color32(245, 255, 238, 248);
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(2f, 1f);
            text.rectTransform.offsetMax = new Vector2(-2f, -1f);

            cityOpsChipImages.Add(image);
            cityOpsChipFills.Add(fill);
            cityOpsChipTexts.Add(text);
        }

        private void RefreshCityOperationsStrip(CityMetrics metrics)
        {
            if (cityOpsTitleText == null && cityOpsActionText == null && cityOpsChipTexts.Count == 0)
            {
                return;
            }

            var traffic = CityOperationsTrafficPressure(metrics);
            var service = CityOperationsServicePressure(metrics);
            var fiscal = CityOperationsFiscalPressure(metrics);
            var pressure = Mathf.Max(traffic, Mathf.Max(service, fiscal));
            var accent = CityOperationsPressureColor(pressure);

            if (cityOpsTitleText != null)
            {
                cityOpsTitleText.text = "\u8fd0\u884c\u603b\u89c8  \u4ea4" + traffic + "  \u670d" + service + "  \u8d22" + fiscal;
                cityOpsTitleText.color = pressure >= 70 ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 250);
            }

            if (cityOpsActionText != null)
            {
                cityOpsActionText.text = BuildCityOperationsActionLine(metrics, traffic, service, fiscal);
                cityOpsActionText.color = pressure >= 70 ? new Color32(255, 224, 132, 250) : new Color32(206, 238, 216, 238);
            }

            if (cityOpsPressureFill != null)
            {
                cityOpsPressureFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, pressure) / 100f), 1f);
                cityOpsPressureFill.color = new Color32(accent.r, accent.g, accent.b, 184);
            }

            if (cityOpsAccentImage != null)
            {
                cityOpsAccentImage.color = new Color32(accent.r, accent.g, accent.b, 210);
            }

            if (cityOpsPanelImage != null)
            {
                cityOpsPanelImage.color = pressure >= 70
                    ? new Color32(62, 49, 35, 224)
                    : new Color32(20, 58, 42, 222);
            }

            SetCityOperationsChip(0, "\u4ea4", traffic, new Color32(255, 207, 86, 226));
            SetCityOperationsChip(1, "\u670d", service, new Color32(96, 214, 118, 226));
            SetCityOperationsChip(2, "\u8d22", fiscal, new Color32(65, 184, 220, 226));
        }

        private void SetCityOperationsChip(int index, string label, int value, Color32 accent)
        {
            var clamped = Mathf.Clamp(value, 0, 100);
            var color = CityOperationsPressureColor(clamped);
            if (index < cityOpsChipImages.Count && cityOpsChipImages[index] != null)
            {
                cityOpsChipImages[index].color = clamped >= 70
                    ? new Color32(255, 232, 150, 74)
                    : new Color32(245, 255, 238, 36);
            }

            if (index < cityOpsChipFills.Count && cityOpsChipFills[index] != null)
            {
                cityOpsChipFills[index].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(10, clamped) / 100f), 1f);
                cityOpsChipFills[index].color = clamped >= 42 ? new Color32(color.r, color.g, color.b, 218) : accent;
            }

            if (index < cityOpsChipTexts.Count && cityOpsChipTexts[index] != null)
            {
                cityOpsChipTexts[index].text = label + " " + clamped;
                cityOpsChipTexts[index].color = clamped >= 70
                    ? new Color32(73, 55, 27, 255)
                    : new Color32(245, 255, 238, 248);
            }
        }

        private static int CityOperationsTrafficPressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var pressure = Mathf.Max(metrics.RoadBottleneckPressure, metrics.IntersectionDelay);
            pressure = Mathf.Max(pressure, 100 - metrics.CommuteEfficiency);
            pressure = Mathf.Max(pressure, metrics.CarDependency - 28);
            pressure = Mathf.Max(pressure, metrics.ParkingPressure);
            return Mathf.Clamp(pressure, 0, 100);
        }

        private static int CityOperationsServicePressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var pressure = Mathf.Max(metrics.ServiceGapPressure, 100 - metrics.ServiceCoverage);
            pressure = Mathf.Max(pressure, metrics.FireRisk);
            pressure = Mathf.Max(pressure, metrics.HealthRisk);
            pressure = Mathf.Max(pressure, metrics.CrimePressure);
            pressure = Mathf.Max(pressure, 100 - metrics.EducationCoverage);
            return Mathf.Clamp(pressure, 0, 100);
        }

        private static int CityOperationsFiscalPressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var pressure = Mathf.Max(metrics.BudgetStress, metrics.ForecastRisk);
            pressure = Mathf.Max(pressure, metrics.DebtPressure);
            pressure = Mathf.Max(pressure, 100 - metrics.FiscalHealth);
            if (metrics.NetIncome < 0)
            {
                pressure = Mathf.Max(pressure, 58 + Mathf.Min(32, Mathf.Abs(metrics.NetIncome) / 18));
            }

            return Mathf.Clamp(pressure, 0, 100);
        }

        private string BuildCityOperationsActionLine(CityMetrics metrics, int traffic, int service, int fiscal)
        {
            if (metrics == null)
            {
                return "\u95ee\u9898\u961f\u5217 \u7b49\u5f85\u57ce\u5e02\u6570\u636e";
            }

            var mode = controller != null ? controller.OverlayMode : OverlayMode.Normal;
            var recommended = RecommendedOverlayMode(metrics);
            var target = recommended == OverlayMode.Normal ? mode : recommended;
            var driver = "\u4ea4\u901a";
            if (service >= traffic && service >= fiscal)
            {
                driver = "\u670d\u52a1";
            }
            else if (fiscal >= traffic && fiscal >= service)
            {
                driver = "\u8d22\u653f";
            }

            var issue = CompactCardText(MiniMapPrimaryIssueLabel(metrics), 5);
            var action = CompactCardText(BuildLayerToolActionChain(metrics, mode, target), 20);
            return "\u4f18\u5148 " + driver + " / \u4e3b\u56e0 " + issue + " / " + action;
        }

        private static Color32 CityOperationsPressureColor(int value)
        {
            if (value >= 70) return new Color32(255, 188, 66, 255);
            if (value >= 42) return new Color32(65, 184, 220, 255);
            return new Color32(96, 214, 118, 255);
        }

        private void BuildPolicyBudgetForecast(Transform root)
        {
            // CITY_SKYLINES_POLICY_BUDGET_FORECAST mirrors a compact finance/service management readout.
            var card = CreatePanel(root, "Policy Budget Forecast", AnchorTopLeft(), new Vector2(722f, -232f), new Vector2(890f, -184f));
            policyBudgetCardImage = card.GetComponent<Image>();
            policyBudgetCardImage.color = new Color32(20, 58, 42, 222);
            policyBudgetCardImage.raycastTarget = false;
            AddSoftCardShadow(card, 34);
            AddPanelTopAccent(card, new Color32(255, 207, 86, 160), 3f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 92);
            outline.effectDistance = new Vector2(1.15f, -1.15f);

            policyBudgetAccentImage = CreateToolButtonAccent(card.transform, "Budget Forecast Accent", AnchorLeft(), new Vector2(4f, 6f), new Vector2(8f, -6f), new Color32(255, 207, 86, 190));
            policyBudgetAccentImage.raycastTarget = false;

            policyBudgetTitleText = CreateText(card.transform, "Budget Forecast Title", "\u8d22\u653f\u8c03\u5ea6 --", 10, FontStyle.Bold, TextAnchor.UpperLeft);
            policyBudgetTitleText.color = new Color32(245, 255, 238, 250);
            policyBudgetTitleText.raycastTarget = false;
            Stretch(policyBudgetTitleText.rectTransform);
            policyBudgetTitleText.rectTransform.offsetMin = new Vector2(13f, 27f);
            policyBudgetTitleText.rectTransform.offsetMax = new Vector2(-38f, -4f);

            policyBudgetStateBadgeImage = CreateToolButtonAccent(card.transform, "Budget Forecast State Badge", new Vector4(1f, 1f, 1f, 1f), new Vector2(-34f, -18f), new Vector2(-7f, -5f), new Color32(96, 214, 118, 218));
            policyBudgetStateBadgeImage.raycastTarget = false;
            policyBudgetStateText = CreateText(policyBudgetStateBadgeImage.transform, "State", "\u7a33", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            policyBudgetStateText.color = new Color32(43, 64, 70, 255);
            policyBudgetStateText.raycastTarget = false;
            Stretch(policyBudgetStateText.rectTransform);

            policyBudgetDetailText = CreateText(card.transform, "Budget Forecast Detail", "\u7a0e/\u9884\u7b97/\u653f\u7b56 --", 9, FontStyle.Bold, TextAnchor.UpperLeft);
            policyBudgetDetailText.color = new Color32(206, 238, 216, 238);
            policyBudgetDetailText.lineSpacing = 0.86f;
            policyBudgetDetailText.resizeTextForBestFit = true;
            policyBudgetDetailText.resizeTextMinSize = 7;
            policyBudgetDetailText.resizeTextMaxSize = 9;
            policyBudgetDetailText.raycastTarget = false;
            Stretch(policyBudgetDetailText.rectTransform);
            policyBudgetDetailText.rectTransform.offsetMin = new Vector2(13f, 7f);
            policyBudgetDetailText.rectTransform.offsetMax = new Vector2(-6f, -22f);

            var track = CreatePanel(card.transform, "Budget Forecast Track", new Vector4(0f, 0f, 1f, 0f), new Vector2(13f, 7f), new Vector2(-7f, 12f));
            var trackImage = track.GetComponent<Image>();
            trackImage.color = new Color32(245, 255, 238, 34);
            trackImage.raycastTarget = false;
            track.AddComponent<LayoutElement>().ignoreLayout = true;
            policyBudgetFillImage = CreateToolButtonAccent(track.transform, "Budget Forecast Fill", AnchorStretch(), Vector2.zero, Vector2.zero, new Color32(96, 214, 118, 176));
            policyBudgetFillImage.raycastTarget = false;
            AddHudFacet(card.transform, "Budget Forecast Facet", new Vector4(0.34f, 0.56f, 0.96f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 24), -8f);
        }

        private void RefreshPolicyBudgetForecast(CityMetrics metrics)
        {
            if (policyBudgetTitleText == null && policyBudgetDetailText == null && policyBudgetFillImage == null)
            {
                return;
            }

            var pressure = PolicyBudgetPressure(metrics);
            var health = metrics != null ? Mathf.Clamp(100 - pressure, 0, 100) : 0;
            var accent = CityOperationsPressureColor(pressure);
            if (policyBudgetTitleText != null)
            {
                policyBudgetTitleText.text = BuildPolicyBudgetTitle(metrics);
                policyBudgetTitleText.color = pressure >= 70
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 250);
            }

            if (policyBudgetDetailText != null)
            {
                policyBudgetDetailText.text = BuildPolicyBudgetDetail(metrics, pressure);
                policyBudgetDetailText.color = pressure >= 70
                    ? new Color32(255, 224, 132, 248)
                    : new Color32(206, 238, 216, 238);
            }

            if (policyBudgetFillImage != null)
            {
                policyBudgetFillImage.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, health) / 100f), 1f);
                policyBudgetFillImage.color = PolicyBudgetHealthColor(health, pressure);
            }

            if (policyBudgetAccentImage != null)
            {
                policyBudgetAccentImage.color = new Color32(accent.r, accent.g, accent.b, 204);
            }

            if (policyBudgetStateBadgeImage != null)
            {
                policyBudgetStateBadgeImage.color = PolicyBudgetStateBadgeColor(pressure, health);
            }

            if (policyBudgetStateText != null)
            {
                policyBudgetStateText.text = PolicyBudgetStateLabel(pressure, health);
                policyBudgetStateText.color = pressure >= 70
                    ? new Color32(83, 68, 30, 255)
                    : new Color32(43, 64, 70, 255);
            }

            if (policyBudgetCardImage != null)
            {
                policyBudgetCardImage.color = pressure >= 70
                    ? new Color32(64, 50, 35, 222)
                    : new Color32(20, 58, 42, 222);
            }
        }

        private static string BuildPolicyBudgetTitle(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u8d22\u653f\u8c03\u5ea6 --";
            }

            var count = metrics.ActivePolicies != null ? metrics.ActivePolicies.Count : 0;
            return "\u8d22\u653f " + FormatSigned(metrics.NetIncome) + "  \u653f" + count + "  \u5065" + metrics.FiscalHealth;
        }

        private static string BuildPolicyBudgetDetail(CityMetrics metrics, int pressure)
        {
            if (metrics == null)
            {
                return "\u7a0e/\u9884\u7b97/\u653f\u7b56 --";
            }

            var action = string.IsNullOrEmpty(metrics.BudgetAction)
                ? (pressure >= 70 ? "\u5148\u63a7\u652f\u51fa" : "\u7ef4\u6301\u6269\u5efa")
                : metrics.BudgetAction;
            return "\u7a0e" + TaxLabel(metrics.TaxLevel)
                + " \u670d" + BudgetLabel(metrics.ServiceBudgetLevel)
                + " \u503a" + metrics.DebtPressure
                + "\n" + BuildPolicyBudgetOrderLine(metrics, pressure, action);
        }

        private static string BuildPolicyBudgetOrderLine(CityMetrics metrics, int pressure, string action)
        {
            var prefix = "\u653f\u7b56\u5355";
            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return prefix + " \u53ef\u9886 > " + CompactCardText(action, 8);
            }

            if (pressure >= 72)
            {
                return prefix + " \u63a7\u652f > " + CompactCardText(action, 8);
            }

            if (metrics.PolicyBacklog > 0)
            {
                return prefix + " \u5f85\u529e" + metrics.PolicyBacklog + " > " + CompactCardText(action, 7);
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return prefix + " \u8865\u8d34\u5347" + metrics.BuildingUpgradeReadyCount;
            }

            return prefix + " \u7a33\u5b9a > " + CompactCardText(action, 8);
        }

        private static string PolicyBudgetStateLabel(int pressure, int health)
        {
            if (pressure >= 72) return "\u6025";
            if (pressure >= 55) return "\u538b";
            if (health >= 72) return "\u7a33";
            return "\u6ce8";
        }

        private static Color32 PolicyBudgetStateBadgeColor(int pressure, int health)
        {
            if (pressure >= 72) return new Color32(255, 188, 66, 238);
            if (pressure >= 55) return new Color32(255, 207, 86, 228);
            if (health >= 72) return new Color32(96, 214, 118, 218);
            return new Color32(65, 184, 220, 218);
        }

        private static Color32 PolicyBudgetHealthColor(int health, int pressure)
        {
            if (pressure >= 72) return new Color32(255, 188, 66, 222);
            if (health >= 72) return new Color32(96, 214, 118, 190);
            if (health >= 45) return new Color32(65, 184, 220, 184);
            return new Color32(255, 207, 86, 210);
        }

        private static int PolicyBudgetPressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var deficit = metrics.NetIncome < 0 ? 58 + Mathf.Min(32, Mathf.Abs(metrics.NetIncome) / 18) : 0;
            var policyLoad = Mathf.Clamp(metrics.PolicyBacklog + metrics.PolicyExpense / 25, 0, 100);
            return Mathf.Clamp(Mathf.Max(metrics.BudgetStress, Mathf.Max(metrics.DebtPressure, Mathf.Max(deficit, policyLoad))), 0, 100);
        }

        private void BuildPriorityCommandStrip(Transform root)
        {
            // REFERENCE_IMAGE_PRIORITY_COMMAND_STRIP adds compact city-priority pills like the target HUD.
            var strip = CreatePanel(root, "Priority Command Strip", AnchorTopLeft(), new Vector2(348f, -338f), new Vector2(890f, -292f));
            priorityCommandStripImage = strip.GetComponent<Image>();
            priorityCommandStripImage.color = new Color32(18, 54, 42, 210);
            priorityCommandStripImage.raycastTarget = false;
            AddSoftCardShadow(strip, 28);
            AddPanelTopAccent(strip, new Color32(255, 207, 86, 126), 3f);
            var outline = strip.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 84);
            outline.effectDistance = new Vector2(1.1f, -1.1f);

            priorityCommandTitleText = CreateText(strip.transform, "Priority Command Title", "\u57ce\u5e02\u4f18\u5148\u7ea7", 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            priorityCommandTitleText.color = new Color32(245, 255, 238, 245);
            priorityCommandTitleText.raycastTarget = false;
            priorityCommandTitleText.resizeTextForBestFit = true;
            priorityCommandTitleText.resizeTextMinSize = 8;
            priorityCommandTitleText.resizeTextMaxSize = 10;
            Stretch(priorityCommandTitleText.rectTransform);
            priorityCommandTitleText.rectTransform.offsetMin = new Vector2(12f, 24f);
            priorityCommandTitleText.rectTransform.offsetMax = new Vector2(-402f, -4f);

            var host = CreatePanel(strip.transform, "Priority Command Pills", new Vector4(0.24f, 0f, 1f, 1f), new Vector2(0f, 6f), new Vector2(-8f, -6f));
            host.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            host.GetComponent<Image>().raycastTarget = false;
            var layout = host.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddPriorityCommandChip(host.transform, 0, "\u98ce\u9669", "\u9669", new Color32(255, 188, 66, 226));
            AddPriorityCommandChip(host.transform, 1, "\u9700\u6c42", "\u9700", new Color32(96, 214, 118, 226));
            AddPriorityCommandChip(host.transform, 2, "\u670d\u52a1", "\u670d", new Color32(244, 139, 124, 226));
            AddPriorityCommandChip(host.transform, 3, "\u9053\u8def", "\u8def", new Color32(244, 173, 66, 226));
            AddPriorityCommandChip(host.transform, 4, "\u5347\u7ea7", "\u5347", new Color32(65, 184, 220, 226));
            AddHudFacet(strip.transform, "Priority Command Shine", new Vector4(0.54f, 0.58f, 0.98f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 22), -8f);
        }

        private void AddPriorityCommandChip(Transform parent, int kind, string title, string glyph, Color32 accent)
        {
            var chip = CreatePanel(parent, "Priority Command " + title, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = chip.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 30);
            chip.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var button = chip.AddComponent<Button>();
            button.onClick.AddListener(() => SelectPriorityCommand(kind));

            var fill = CreateToolButtonAccent(chip.transform, "Priority Fill", AnchorBottom(), new Vector2(4f, 3f), new Vector2(-4f, 7f), accent);
            fill.raycastTarget = false;
            priorityCommandChipFills.Add(fill);

            var glyphChip = CreateToolButtonAccent(chip.transform, "Priority Glyph", AnchorLeft(), new Vector2(4f, 5f), new Vector2(24f, -5f), accent);
            glyphChip.raycastTarget = false;
            var glyphText = CreateText(glyphChip.transform, "Glyph", glyph, 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyphText.color = new Color32(43, 64, 70, 255);
            glyphText.raycastTarget = false;
            Stretch(glyphText.rectTransform);

            var label = CreateText(chip.transform, "Label", title + " --", 9, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.color = new Color32(245, 255, 238, 245);
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 7;
            label.resizeTextMaxSize = 9;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(28f, 3f);
            label.rectTransform.offsetMax = new Vector2(-22f, -3f);

            var badge = CreateToolButtonAccent(chip.transform, "Recommended Badge", AnchorTopRight(), new Vector2(-24f, -18f), new Vector2(-3f, -4f), new Color32(255, 207, 86, 0));
            badge.raycastTarget = false;
            var badgeText = CreateText(badge.transform, "Badge Text", string.Empty, 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            badgeText.color = new Color32(43, 64, 70, 255);
            badgeText.raycastTarget = false;
            Stretch(badgeText.rectTransform);

            priorityCommandChipImages.Add(image);
            priorityCommandChipTexts.Add(label);
            priorityCommandBadgeImages.Add(badge);
            priorityCommandBadgeTexts.Add(badgeText);
        }

        private void RefreshPriorityCommandStrip(CityMetrics metrics)
        {
            if (priorityCommandChipTexts.Count == 0)
            {
                return;
            }

            var strongest = 0;
            var strongestIndex = 0;
            for (var i = 0; i < priorityCommandChipTexts.Count; i += 1)
            {
                var value = PriorityCommandValue(i, metrics);
                if (value > strongest)
                {
                    strongest = value;
                    strongestIndex = i;
                }
            }

            for (var i = 0; i < priorityCommandChipTexts.Count; i += 1)
            {
                var value = PriorityCommandValue(i, metrics);
                var accent = PriorityCommandColor(i, value);
                var recommended = metrics != null && value > 0 && i == strongestIndex;
                priorityCommandChipTexts[i].text = PriorityCommandLabel(i, metrics, value);
                priorityCommandChipTexts[i].color = value >= 70 ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 245);

                if (i < priorityCommandChipFills.Count && priorityCommandChipFills[i] != null)
                {
                    priorityCommandChipFills[i].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(6, value) / 100f), 1f);
                    priorityCommandChipFills[i].color = new Color32(accent.r, accent.g, accent.b, value >= 70 ? (byte)226 : (byte)174);
                }

                if (i < priorityCommandChipImages.Count && priorityCommandChipImages[i] != null)
                {
                    priorityCommandChipImages[i].color = recommended
                        ? (value >= 70 ? new Color32(74, 56, 34, 172) : new Color32(44, 86, 54, 142))
                        : new Color32(245, 255, 238, 30);
                }

                if (i < priorityCommandBadgeImages.Count && priorityCommandBadgeImages[i] != null)
                {
                    priorityCommandBadgeImages[i].color = recommended
                        ? (value >= 70 ? new Color32(255, 207, 86, 238) : new Color32(96, 214, 118, 222))
                        : new Color32(255, 207, 86, 0);
                }

                if (i < priorityCommandBadgeTexts.Count && priorityCommandBadgeTexts[i] != null)
                {
                    priorityCommandBadgeTexts[i].text = recommended ? PriorityCommandBadgeText(value) : string.Empty;
                }
            }

            if (priorityCommandTitleText != null)
            {
                priorityCommandTitleText.text = metrics != null && strongest > 0
                    ? "\u4f18\u5148\u7ea7 \u63a8:" + PriorityCommandName(strongestIndex)
                    : "\u4f18\u5148\u7ea7 " + PriorityCommandHeadline(metrics, strongest);
                priorityCommandTitleText.color = strongest >= 70 ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 245);
            }

            if (priorityCommandStripImage != null)
            {
                priorityCommandStripImage.color = strongest >= 70 ? new Color32(52, 45, 34, 216) : new Color32(18, 54, 42, 210);
            }
        }

        private void BuildAdvisorActionQueue(Transform root)
        {
            // CITY_SKYLINES_ADVISOR_ACTION_QUEUE turns diagnosis cards into direct planning actions.
            var queue = CreatePanel(root, "Advisor Action Queue", AnchorTopLeft(), new Vector2(348f, -286f), new Vector2(890f, -240f));
            var image = queue.GetComponent<Image>();
            image.color = new Color32(18, 54, 42, 214);
            image.raycastTarget = false;
            AddSoftCardShadow(queue, 32);
            AddPanelTopAccent(queue, new Color32(65, 183, 190, 146), 3f);
            var outline = queue.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 86);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var header = CreateText(queue.transform, "Advisor Queue Header", "\u987e\u95ee\u884c\u52a8", 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            header.color = new Color32(245, 255, 238, 245);
            header.raycastTarget = false;
            Stretch(header.rectTransform);
            header.rectTransform.offsetMin = new Vector2(12f, 27f);
            header.rectTransform.offsetMax = new Vector2(-450f, -3f);

            var host = CreatePanel(queue.transform, "Advisor Queue Cards", new Vector4(0.19f, 0f, 1f, 1f), new Vector2(0f, 5f), new Vector2(-7f, -5f));
            host.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            host.GetComponent<Image>().raycastTarget = false;
            var layout = host.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddAdvisorActionCard(host.transform, 0, "\u4ea4\u901a", "\u770b\u8def\u7f51", new Color32(255, 207, 86, 226));
            AddAdvisorActionCard(host.transform, 1, "\u670d\u52a1", "\u8865\u7f3a\u53e3", new Color32(96, 214, 118, 226));
            AddAdvisorActionCard(host.transform, 2, "\u8d22\u653f", "\u8c03\u9884\u7b97", new Color32(65, 184, 220, 226));
            AddHudFacet(queue.transform, "Advisor Queue Facet", new Vector4(0.5f, 0.56f, 0.98f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 22), -8f);
        }

        private void AddAdvisorActionCard(Transform parent, int lane, string title, string detail, Color32 accent)
        {
            var card = CreatePanel(parent, "Advisor Action " + title, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = card.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 34);
            card.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var button = card.AddComponent<Button>();
            button.onClick.AddListener(() => OnAdvisorActionClicked(lane));

            var fill = CreateToolButtonAccent(card.transform, "Advisor Action Fill", AnchorStretch(), new Vector2(2f, 24f), new Vector2(-2f, -2f), accent);
            fill.raycastTarget = false;
            var rail = CreateToolButtonAccent(card.transform, "Advisor Action Rail", AnchorLeft(), new Vector2(2f, 3f), new Vector2(5f, -3f), accent);
            rail.raycastTarget = false;

            var stageBadge = CreateToolButtonAccent(card.transform, "Advisor Action Stage Badge", AnchorTopRight(), new Vector2(-38f, -17f), new Vector2(-4f, -3f), new Color32(255, 207, 86, 160));
            stageBadge.raycastTarget = false;
            var stageText = CreateText(stageBadge.transform, "Stage Text", "\u89c2\u5bdf", 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            stageText.color = new Color32(28, 54, 39, 245);
            stageText.raycastTarget = false;
            Stretch(stageText.rectTransform);
            stageText.rectTransform.offsetMin = Vector2.zero;
            stageText.rectTransform.offsetMax = Vector2.zero;

            var titleText = CreateText(card.transform, "Title", title + " --", 9, FontStyle.Bold, TextAnchor.UpperLeft);
            titleText.color = new Color32(245, 255, 238, 250);
            titleText.raycastTarget = false;
            Stretch(titleText.rectTransform);
            titleText.rectTransform.offsetMin = new Vector2(9f, 20f);
            titleText.rectTransform.offsetMax = new Vector2(-43f, -2f);

            var detailText = CreateText(card.transform, "Detail", detail, 8, FontStyle.Bold, TextAnchor.UpperLeft);
            detailText.color = new Color32(206, 238, 216, 238);
            detailText.raycastTarget = false;
            detailText.resizeTextForBestFit = true;
            detailText.resizeTextMinSize = 7;
            detailText.resizeTextMaxSize = 8;
            Stretch(detailText.rectTransform);
            detailText.rectTransform.offsetMin = new Vector2(9f, 4f);
            detailText.rectTransform.offsetMax = new Vector2(-5f, -19f);

            advisorActionCards.Add(new AdvisorActionBinding
            {
                Button = button,
                Card = image,
                Fill = fill,
                Accent = rail,
                StageBadge = stageBadge,
                Title = titleText,
                Detail = detailText,
                StageText = stageText,
                Lane = lane
            });
        }

        private void RefreshAdvisorActionQueue(CityMetrics metrics)
        {
            for (var i = 0; i < advisorActionCards.Count; i += 1)
            {
                var binding = advisorActionCards[i];
                var pressure = AdvisorActionPressure(binding.Lane, metrics);
                var accent = AdvisorActionAccent(binding.Lane, pressure);
                if (binding.Card != null)
                {
                    binding.Card.color = pressure >= 70
                        ? new Color32(255, 226, 138, 72)
                        : new Color32(245, 255, 238, 34);
                }

                if (binding.Fill != null)
                {
                    binding.Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(9, pressure) / 100f), 1f);
                    binding.Fill.color = new Color32(accent.r, accent.g, accent.b, pressure >= 70 ? (byte)220 : (byte)176);
                }

                if (binding.Accent != null)
                {
                    binding.Accent.color = new Color32(accent.r, accent.g, accent.b, 210);
                }

                if (binding.StageBadge != null)
                {
                    binding.StageBadge.color = AdvisorActionStageColor(pressure);
                }

                if (binding.StageText != null)
                {
                    binding.StageText.text = AdvisorActionStageLabel(pressure);
                    binding.StageText.color = pressure >= 70
                        ? new Color32(74, 42, 18, 248)
                        : new Color32(28, 54, 39, 245);
                }

                if (binding.Title != null)
                {
                    binding.Title.text = AdvisorActionTitle(binding.Lane) + " " + pressure + " " + AdvisorActionCadence(pressure);
                    binding.Title.color = pressure >= 70
                        ? new Color32(255, 232, 150, 255)
                        : new Color32(245, 255, 238, 250);
                }

                if (binding.Detail != null)
                {
                    binding.Detail.text = AdvisorActionDetail(binding.Lane, metrics);
                }
            }
        }

        private void OnAdvisorActionClicked(int lane)
        {
            if (controller == null)
            {
                return;
            }

            var metrics = controller.Metrics;
            var advisorType = CityHudViewModelSmartAdvisor.GetAdvisorActionType(lane, metrics);
            if (lane == 0)
            {
                controller.SetOverlay(OverlayMode.Traffic);
                if (interaction != null)
                {
                    if (metrics != null && (metrics.RoadBottleneckPressure >= 58 || metrics.IntersectionDelay >= 52))
                    {
                        interaction.SelectRoadUpgradeTool();
                    }
                    else
                    {
                        interaction.SelectRoadTool();
                    }
                }

                controller.PublishHudFeedback("\u987e\u95ee \u4ea4\u901a\uff1a\u5df2\u5207\u5230\u4ea4\u901a\u5c42 -> \u4fee\u901a\u65ad\u70b9/\u5347\u7ea7\u74f6\u9888\u8def", true);
                ArmPendingAdvisorAdoption(advisorType);
                return;
            }

            if (lane == 1)
            {
                controller.SetOverlay(OverlayMode.Services);
                if (interaction != null)
                {
                    interaction.SelectBuildingTool(AdvisorServiceBuildingId(metrics));
                }

                controller.PublishHudFeedback("\u987e\u95ee \u670d\u52a1\uff1a\u5df2\u5207\u5230\u670d\u52a1\u5c42 -> \u628a\u8bbe\u65bd\u653e\u5728\u7f3a\u53e3\u4f4f\u533a\u8def\u53e3", true);
                ArmPendingAdvisorAdoption(advisorType);
                return;
            }

            controller.SetOverlay(OverlayMode.LandValue);
            controller.PublishHudFeedback("\u987e\u95ee \u8d22\u653f\uff1a\u770b\u5730\u4ef7/\u9884\u7b97 -> \u8c03\u7a0e\u7387\u3001\u670d\u52a1\u9884\u7b97\u6216\u6682\u7f13\u65b0\u5efa", true);
            ArmPendingAdvisorAdoption(advisorType);
        }

        private static int AdvisorActionPressure(int lane, CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            if (lane == 0)
            {
                return Mathf.Clamp(Mathf.Max(metrics.RoadBottleneckPressure, Mathf.Max(metrics.IntersectionDelay, 100 - metrics.CommuteEfficiency)), 0, 100);
            }

            if (lane == 1)
            {
                return Mathf.Clamp(Mathf.Max(metrics.ServiceGapPressure, Mathf.Max(100 - metrics.ServiceCoverage, Mathf.Max(metrics.HealthRisk, metrics.FireRisk))), 0, 100);
            }

            return PolicyBudgetPressure(metrics);
        }

        private static Color32 AdvisorActionAccent(int lane, int pressure)
        {
            if (pressure >= 70) return new Color32(255, 188, 66, 255);
            if (lane == 0) return new Color32(255, 207, 86, 255);
            if (lane == 1) return new Color32(96, 214, 118, 255);
            return new Color32(65, 184, 220, 255);
        }

        private static string AdvisorActionTitle(int lane)
        {
            if (lane == 0) return "\u4ea4\u901a";
            if (lane == 1) return "\u670d\u52a1";
            return "\u8d22\u653f";
        }

        private static string AdvisorActionStageLabel(int pressure)
        {
            if (pressure >= 82) return "\u6025\u529e";
            if (pressure >= 62) return "\u8ddf\u8fdb";
            return "\u89c2\u5bdf";
        }

        private static string AdvisorActionCadence(int pressure)
        {
            if (pressure >= 82) return "\u25b6\u25b6";
            if (pressure >= 62) return "\u25b6";
            return "\u2022";
        }

        private static Color32 AdvisorActionStageColor(int pressure)
        {
            if (pressure >= 82) return new Color32(255, 207, 86, 238);
            if (pressure >= 62) return new Color32(118, 221, 119, 214);
            return new Color32(222, 246, 219, 156);
        }

        private static string AdvisorActionDetail(int lane, CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u7b49\u5f85\u6570\u636e";
            }

            if (lane == 0)
            {
                return "\u8def\u74f6" + metrics.RoadBottleneckPressure + " \u901a\u52e4" + metrics.CommuteEfficiency + " > " + (metrics.IntersectionDelay >= 52 ? "\u5347\u7ea7\u8def\u53e3" : "\u63a5\u4e3b\u8def");
            }

            if (lane == 1)
            {
                var focus = string.IsNullOrEmpty(metrics.ServiceGapFocus) ? "\u4f4f\u533a" : CompactCardText(metrics.ServiceGapFocus, 4);
                return "\u7f3a\u53e3" + metrics.ServiceGapPressure + " " + focus + " > " + AdvisorServiceLabel(metrics);
            }

            return "\u6536\u652f" + FormatSigned(metrics.NetIncome) + " \u538b" + PolicyBudgetPressure(metrics) + " > " + CompactCardText(string.IsNullOrEmpty(metrics.BudgetAction) ? "\u8c03\u9884\u7b97" : metrics.BudgetAction, 5);
        }

        private static string AdvisorServiceBuildingId(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "pocket_park";
            }

            if (metrics.FireRisk >= 55 || metrics.SafetyCoverage < 35) return "fire_station";
            if (metrics.CrimePressure >= 55 || metrics.SecurityCoverage < 35) return "police_kiosk";
            if (metrics.HealthRisk >= 55 || metrics.HealthCoverage < 35) return "health_post";
            if (metrics.EducationCoverage < 35 || metrics.StudentBacklog > 55) return "primary_school";
            if (metrics.ParkCoverage < 45) return "pocket_park";
            if (metrics.CommunicationCoverage < 35) return "telecom_hub";
            return "pocket_park";
        }

        private static string AdvisorServiceLabel(CityMetrics metrics)
        {
            var id = AdvisorServiceBuildingId(metrics);
            if (id == "fire_station") return "\u6d88\u9632";
            if (id == "police_kiosk") return "\u8b66\u52a1";
            if (id == "health_post") return "\u8bca\u6240";
            if (id == "primary_school") return "\u5b66\u6821";
            if (id == "telecom_hub") return "\u901a\u4fe1";
            return "\u516c\u56ed";
        }

        private void SelectPriorityCommand(int kind)
        {
            if (controller == null)
            {
                return;
            }

            var metrics = controller.Metrics;
            if (kind == 0)
            {
                var recommended = RecommendedOverlayMode(metrics);
                controller.SetOverlay(recommended);
                controller.PublishHudFeedback("\u4f18\u5148 \u98ce\u9669 > \u770b" + OverlayLabel(recommended), true);
                return;
            }

            if (kind == 1)
            {
                controller.SetOverlay(OverlayMode.Zoning);
                if (interaction != null)
                {
                    interaction.SelectZoneTool(SelectedTileRecommendedZone(metrics));
                }

                controller.PublishHudFeedback("\u4f18\u5148 \u9700\u6c42 > \u8865\u5206\u533a", true);
                return;
            }

            if (kind == 2)
            {
                controller.SetOverlay(OverlayMode.Services);
                if (interaction != null)
                {
                    interaction.SelectBuildingTool(AdvisorServiceBuildingId(metrics));
                }

                controller.PublishHudFeedback("\u4f18\u5148 \u670d\u52a1 > \u8865\u7f3a\u53e3", true);
                return;
            }

            if (kind == 3)
            {
                controller.SetOverlay(OverlayMode.Traffic);
                if (interaction != null)
                {
                    interaction.SelectRoadUpgradeTool();
                }

                controller.PublishHudFeedback("\u4f18\u5148 \u9053\u8def > \u5347\u7ea7\u74f6\u9888", true);
                return;
            }

            controller.SetOverlay(OverlayMode.LandValue);
            if (interaction != null)
            {
                interaction.SelectBuildingTool("pocket_park");
            }

            controller.PublishHudFeedback("\u4f18\u5148 \u5347\u7ea7 > \u8865\u914d\u5957", true);
        }

        private static int PriorityCommandValue(int kind, CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            if (kind == 0) return Mathf.Clamp(Mathf.Max(metrics.ForecastRisk, metrics.BudgetStress), 0, 100);
            if (kind == 1) return Mathf.Clamp(metrics.DemandUrgency, 0, 100);
            if (kind == 2) return Mathf.Clamp(Mathf.Max(metrics.ServiceGapPressure, 100 - metrics.ServiceCoverage), 0, 100);
            if (kind == 3) return Mathf.Clamp(Mathf.Max(metrics.RoadBottleneckPressure, metrics.IntersectionDelay), 0, 100);
            return Mathf.Clamp(Mathf.Max(metrics.BuildingUpgradeReadinessScore, metrics.BuildingUpgradeReadyCount * 12 + metrics.BuildingUpgradeBlockedCount * 10), 0, 100);
        }

        private static string PriorityCommandLabel(int kind, CityMetrics metrics, int value)
        {
            if (metrics == null)
            {
                return "--";
            }

            if (kind == 0) return "\u9669 " + value;
            if (kind == 1) return "\u9700 " + value;
            if (kind == 2) return "\u670d " + value;
            if (kind == 3) return "\u8def " + value;
            return "\u5347 " + metrics.BuildingUpgradeReadyCount + "/" + metrics.BuildingUpgradeBlockedCount;
        }

        private static string PriorityCommandHeadline(CityMetrics metrics, int strongest)
        {
            if (metrics == null)
            {
                return "--";
            }

            if (strongest >= 70) return "\u9ad8\u538b";
            if (metrics.BuildingUpgradeReadyCount > 0) return "\u53ef\u5347\u7ea7";
            if (metrics.DemandUrgency >= 45) return "\u8865\u9700\u6c42";
            return "\u7a33\u5b9a";
        }

        private static string PriorityCommandBadgeText(int value)
        {
            return value >= 70 ? "\u6025" : "\u63a8";
        }

        private static string PriorityCommandName(int kind)
        {
            if (kind == 0) return "\u98ce\u9669";
            if (kind == 1) return "\u9700\u6c42";
            if (kind == 2) return "\u670d\u52a1";
            if (kind == 3) return "\u9053\u8def";
            return "\u5347\u7ea7";
        }

        private static Color32 PriorityCommandColor(int kind, int value)
        {
            if (value >= 70) return new Color32(255, 188, 66, 226);
            if (kind == 1) return new Color32(96, 214, 118, 210);
            if (kind == 2) return new Color32(244, 139, 124, 210);
            if (kind == 3) return new Color32(244, 173, 66, 210);
            if (kind == 4) return new Color32(65, 184, 220, 210);
            return new Color32(206, 238, 216, 210);
        }

        private void BuildTopResourceCapsule(Transform parent, string label, float preferredWidth, Color32 accent)
        {
            var capsule = CreatePanel(parent, "Top Capsule " + label, AnchorFree(), Vector2.zero, Vector2.zero);
            var capsuleImage = capsule.GetComponent<Image>();
            capsuleImage.color = new Color32(30, 66, 43, 238);
            var outline = capsule.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 145);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            AddPanelTopAccent(capsule, accent, 3f);
            var layout = capsule.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = 48f;

            var accentObject = new GameObject("Accent");
            accentObject.transform.SetParent(capsule.transform, false);
            var accentRect = accentObject.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = new Vector2(5f, 0f);
            var accentImage = accentObject.AddComponent<Image>();
            accentImage.color = accent;

            AddTopCapsuleIcon(capsule.transform, label, accent);
            var plusImage = AddTopCapsulePlusBadge(capsule.transform);
            var dividerImage = AddTopCapsuleDivider(capsule.transform, accent);
            AddTopCapsuleFacet(capsule.transform, accent);
            var statusStrip = AddTopCapsuleStatusStrip(capsule.transform, accent);
            Text statusBadgeText;
            var statusBadge = AddTopCapsuleStateBadge(capsule.transform, accent, out statusBadgeText);

            var text = CreateText(capsule.transform, "Label", label + " --", 13, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.color = new Color32(245, 255, 238, 255);
            text.lineSpacing = 0.82f;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = 13;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(42f, 4f);
            text.rectTransform.offsetMax = new Vector2(-42f, -3f);
            topCapsuleSegmentTexts.Add(text);
            topCapsuleImages.Add(capsuleImage);
            topCapsuleAccentImages.Add(accentImage);
            topCapsulePlusImages.Add(plusImage);
            topCapsuleDividerImages.Add(dividerImage);
            topCapsuleStatusStrips.Add(statusStrip);
            topCapsuleStatusBadgeImages.Add(statusBadge);
            topCapsuleStatusBadgeTexts.Add(statusBadgeText);
            topCapsuleOutlines.Add(outline);
        }

        private void AddTopCapsuleIcon(Transform parent, string label, Color32 accent)
        {
            // REFERENCE_IMAGE_TOP_CAPSULE_ICON_SWATCH adds quick resource recognition to the top bar.
            var icon = new GameObject("Resource Icon");
            icon.transform.SetParent(parent, false);
            var rect = icon.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(23f, 23f);
            rect.anchoredPosition = new Vector2(22f, 0f);
            var image = icon.AddComponent<Image>();
            image.color = accent;
            image.raycastTarget = false;
            var outline = icon.AddComponent<Outline>();
            outline.effectColor = new Color32(245, 255, 238, 166);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var glyph = CreateText(icon.transform, "Glyph", TopCapsuleIconGlyph(label), 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(38, 76, 45, 255);
            Stretch(glyph.rectTransform);
        }

        private Image AddTopCapsulePlusBadge(Transform parent)
        {
            // REFERENCE_IMAGE_TOP_CAPSULE_PLUS_BADGE mirrors the small green add affordance in the mockup.
            var badge = new GameObject("Plus Badge");
            badge.transform.SetParent(parent, false);
            var rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(24f, 24f);
            rect.anchoredPosition = new Vector2(-18f, 0f);
            var image = badge.AddComponent<Image>();
            image.color = new Color32(104, 205, 92, 248);
            image.raycastTarget = false;

            var plus = CreateText(badge.transform, "Plus", "+", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            plus.color = new Color32(245, 255, 238, 255);
            Stretch(plus.rectTransform);
            return image;
        }

        private Image AddTopCapsuleDivider(Transform parent, Color32 accent)
        {
            // CITY_RESOURCE_CAPSULE_DIVIDER keeps action badges visually separate from the resource value.
            var divider = new GameObject("Capsule Divider");
            divider.transform.SetParent(parent, false);
            var rect = divider.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1.5f, 28f);
            rect.anchoredPosition = new Vector2(-39f, 1f);
            var image = divider.AddComponent<Image>();
            image.color = new Color32(accent.r, accent.g, accent.b, 92);
            image.raycastTarget = false;
            var layout = divider.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private void AddTopCapsuleFacet(Transform parent, Color32 accent)
        {
            // REFERENCE_IMAGE_TOP_CAPSULE_FACETS gives resource pills the bright low-poly sheen.
            var shine = new GameObject("Capsule Facet Shine");
            shine.transform.SetParent(parent, false);
            var shineRect = shine.AddComponent<RectTransform>();
            shineRect.anchorMin = new Vector2(0f, 1f);
            shineRect.anchorMax = new Vector2(1f, 1f);
            shineRect.pivot = new Vector2(0.5f, 1f);
            shineRect.offsetMin = new Vector2(32f, -13f);
            shineRect.offsetMax = new Vector2(-38f, -5f);
            var shineImage = shine.AddComponent<Image>();
            shineImage.color = new Color32(255, 255, 255, 76);
            shineImage.raycastTarget = false;
            var shineLayout = shine.AddComponent<LayoutElement>();
            shineLayout.ignoreLayout = true;

            var chip = new GameObject("Capsule Accent Chip");
            chip.transform.SetParent(parent, false);
            var chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0f, 0f);
            chipRect.anchorMax = new Vector2(0f, 0f);
            chipRect.pivot = new Vector2(0f, 0f);
            chipRect.sizeDelta = new Vector2(34f, 7f);
            chipRect.anchoredPosition = new Vector2(10f, 7f);
            var chipImage = chip.AddComponent<Image>();
            chipImage.color = new Color32(accent.r, accent.g, accent.b, 68);
            chipImage.raycastTarget = false;
            var chipLayout = chip.AddComponent<LayoutElement>();
            chipLayout.ignoreLayout = true;
        }

        private Image AddTopCapsuleStatusStrip(Transform parent, Color32 accent)
        {
            // REFERENCE_IMAGE_TOP_CAPSULE_STATUS_STRIP mirrors the compact resource chips in the target mockup.
            var strip = new GameObject("Capsule Status Strip");
            strip.transform.SetParent(parent, false);
            var rect = strip.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0f);
            rect.anchorMax = new Vector2(0.78f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, 4f);
            rect.offsetMax = new Vector2(0f, 8f);
            var image = strip.AddComponent<Image>();
            image.color = new Color32(accent.r, accent.g, accent.b, 178);
            image.raycastTarget = false;
            var layout = strip.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private Image AddTopCapsuleStateBadge(Transform parent, Color32 accent, out Text badgeText)
        {
            // REFERENCE_IMAGE_TOP_RESOURCE_STATE_BADGES adds tiny health chips to each top resource pill.
            var badge = new GameObject("Capsule State Badge");
            badge.transform.SetParent(parent, false);
            var rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(28f, 15f);
            rect.anchoredPosition = new Vector2(-36f, 7f);
            var image = badge.AddComponent<Image>();
            image.color = new Color32(accent.r, accent.g, accent.b, 185);
            image.raycastTarget = false;
            var layout = badge.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            badgeText = CreateText(badge.transform, "State", "\u7a33", 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            badgeText.color = new Color32(43, 64, 70, 255);
            Stretch(badgeText.rectTransform);
            return image;
        }

        private static string TopCapsuleIconGlyph(string label)
        {
            if (label == "\u73b0\u91d1") return "$";
            if (label == "\u4eba\u53e3") return "P";
            return "%";
        }

        private Image AddTopStatScanMarker(Transform parent, int index)
        {
            // REFERENCE_IMAGE_RESOURCE_ROW_SCAN_MARKERS improves the top-left card's resource scanning.
            var marker = new GameObject("Top Stat Scan Marker");
            marker.transform.SetParent(parent, false);
            var rect = marker.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(4f, 13f);
            rect.anchoredPosition = new Vector2(-7f, 0f);
            var image = marker.AddComponent<Image>();
            image.color = TopStatScanMarkerColor(index, false);
            image.raycastTarget = false;
            var layout = marker.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private GameObject CreateTopStatRow(Transform parent, int index)
        {
            // REFERENCE_IMAGE_RESOURCE_ROW_BACKPLATES makes the left resource card scan like the reference panel.
            var row = CreatePanel(parent, "TopStatRow" + index, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = row.GetComponent<Image>();
            image.color = TopStatRowBackplateColor(index, false);
            image.raycastTarget = false;
            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 16f;
            layout.flexibleWidth = 1f;
            topStatRowBackplates.Add(image);
            return row;
        }

        private static Color32 TopStatRowBackplateColor(int index, bool warning)
        {
            if (warning)
            {
                return new Color32(255, 232, 150, 132);
            }

            return index % 2 == 0
                ? new Color32(245, 255, 238, 24)
                : new Color32(118, 206, 118, 30);
        }

        private static Color32 TopStatScanMarkerColor(int index, bool warning)
        {
            if (warning)
            {
                return new Color32(244, 116, 71, 240);
            }

            var band = index % 4;
            if (band == 0) return new Color32(93, 199, 116, 230);
            if (band == 1) return new Color32(83, 188, 206, 224);
            if (band == 2) return new Color32(255, 207, 86, 226);
            return new Color32(137, 211, 154, 224);
        }

        private void RefreshTopResourceCapsules(CityMetrics metrics)
        {
            if (metrics == null)
            {
                topCapsuleSegmentTexts[0].text = "\u73b0\u91d1 --";
                topCapsuleSegmentTexts[1].text = "\u4eba\u53e3 --";
                topCapsuleSegmentTexts[2].text = "\u5e78\u798f --";
                RefreshTopCapsuleStatus(0, new Color32(30, 66, 43, 238), new Color32(255, 207, 86, 255));
                RefreshTopCapsuleStatus(1, new Color32(30, 66, 43, 238), new Color32(126, 218, 142, 255));
                RefreshTopCapsuleStatus(2, new Color32(30, 66, 43, 238), new Color32(255, 224, 92, 255));
                RefreshTopCapsuleStateBadge(0, "--", new Color32(255, 200, 70, 255), false);
                RefreshTopCapsuleStateBadge(1, "--", new Color32(206, 238, 216, 255), false);
                RefreshTopCapsuleStateBadge(2, "--", new Color32(255, 220, 86, 255), false);
                return;
            }

            topCapsuleSegmentTexts[0].text = CashCapsuleText(metrics);
            topCapsuleSegmentTexts[1].text = "\u4eba\u53e3 " + metrics.Population + "/" + metrics.HousingCapacity + "\n" + PopulationCapsuleDetailText(metrics);
            topCapsuleSegmentTexts[2].text = "\u5e78\u798f " + metrics.Happiness + "%  \u8bc4" + metrics.CityScore + "\n" + HappinessCapsuleDetailText(metrics);
            RefreshTopCapsuleStatus(0, CashCapsuleSurface(metrics), CashCapsuleAccent(metrics));
            RefreshTopCapsuleStatus(1, PopulationCapsuleSurface(metrics), PopulationCapsuleAccent(metrics));
            RefreshTopCapsuleStatus(2, HappinessCapsuleSurface(metrics), HappinessCapsuleAccent(metrics));
            RefreshTopCapsuleStateBadge(0, CashCapsuleStateLabel(metrics), CashCapsuleAccent(metrics), CashCapsuleBudgetPressure(metrics));
            RefreshTopCapsuleStateBadge(1, PopulationCapsuleStateLabel(metrics), PopulationCapsuleAccent(metrics), PopulationCapsuleAtCapacity(metrics));
            RefreshTopCapsuleStateBadge(2, HappinessCapsuleStateLabel(metrics), HappinessCapsuleAccent(metrics), metrics.Happiness < 50 || metrics.ForecastRisk >= 65);
        }

        private void RefreshTopCapsuleStatus(int index, Color32 surface, Color32 accent)
        {
            // REFERENCE_IMAGE_DYNAMIC_TOP_CAPSULE_STATUS makes top resources read like live city-builder status chips.
            if (index < topCapsuleImages.Count && topCapsuleImages[index] != null)
            {
                topCapsuleImages[index].color = surface;
            }

            if (index < topCapsuleAccentImages.Count && topCapsuleAccentImages[index] != null)
            {
                topCapsuleAccentImages[index].color = accent;
            }

            if (index < topCapsulePlusImages.Count && topCapsulePlusImages[index] != null)
            {
                topCapsulePlusImages[index].color = BlendToolRecommendationColor(new Color32(104, 205, 92, 248), accent, 0.3f);
            }

            if (index < topCapsuleDividerImages.Count && topCapsuleDividerImages[index] != null)
            {
                topCapsuleDividerImages[index].color = new Color32(accent.r, accent.g, accent.b, 116);
            }

            if (index < topCapsuleStatusStrips.Count && topCapsuleStatusStrips[index] != null)
            {
                topCapsuleStatusStrips[index].color = new Color32(accent.r, accent.g, accent.b, 184);
            }

            if (index < topCapsuleSegmentTexts.Count && topCapsuleSegmentTexts[index] != null)
            {
                topCapsuleSegmentTexts[index].color = TopCapsuleStatusTextColor(accent);
            }

            if (index < topCapsuleOutlines.Count && topCapsuleOutlines[index] != null)
            {
                topCapsuleOutlines[index].effectColor = new Color32(accent.r, accent.g, accent.b, 148);
                topCapsuleOutlines[index].effectDistance = new Vector2(1.8f, -1.8f);
            }
        }

        private void RefreshTopCapsuleStateBadge(int index, string label, Color32 accent, bool warning)
        {
            if (index < topCapsuleStatusBadgeImages.Count && topCapsuleStatusBadgeImages[index] != null)
            {
                topCapsuleStatusBadgeImages[index].color = warning
                    ? new Color32(accent.r, accent.g, accent.b, 238)
                    : new Color32(72, 156, 92, 222);
            }

            if (index < topCapsuleStatusBadgeTexts.Count && topCapsuleStatusBadgeTexts[index] != null)
            {
                topCapsuleStatusBadgeTexts[index].text = label;
                topCapsuleStatusBadgeTexts[index].color = warning
                    ? new Color32(83, 55, 24, 255)
                    : new Color32(245, 255, 238, 255);
            }
        }

        private static Color32 TopCapsuleStatusTextColor(Color32 accent)
        {
            // REFERENCE_IMAGE_TOP_CAPSULE_STATUS_TEXT lets urgent top chips read faster than numbers alone.
            if (accent.r > 220 && accent.g < 150)
            {
                return new Color32(255, 226, 194, 255);
            }

            if (accent.r > 230 && accent.g >= 190)
            {
                return new Color32(255, 239, 176, 255);
            }

            return new Color32(245, 255, 238, 255);
        }

        private static Color32 CashCapsuleSurface(CityMetrics metrics)
        {
            if (CashCapsuleBudgetPressure(metrics))
            {
                return new Color32(78, 57, 45, 238);
            }

            return new Color32(30, 66, 43, 238);
        }

        private static Color32 CashCapsuleAccent(CityMetrics metrics)
        {
            if (CashCapsuleBudgetPressure(metrics))
            {
                return new Color32(236, 116, 56, 255);
            }

            return new Color32(255, 200, 70, 255);
        }

        private static string CashCapsuleText(CityMetrics metrics)
        {
            // CITY_SKYLINES_BUDGET_RUNWAY_CHIP surfaces short-runway budget risk in the top resource strip.
            var detail = FormatSigned(metrics.NetIncome);
            if (metrics.BudgetStress >= 55)
            {
                detail += "  \u9884\u7b97" + metrics.BudgetStress;
            }
            else if (metrics.CashRunwayDays > 0 && (metrics.CashRunwayDays <= 45 || metrics.NetIncome < 0))
            {
                detail += "  \u8dd1" + metrics.CashRunwayDays + "\u5929";
            }

            detail = AppendTopCapsuleOrderCue(detail, metrics);
            return "\u73b0\u91d1 " + metrics.Cash + "\n" + detail;
        }

        private static string CashCapsuleStateLabel(CityMetrics metrics)
        {
            if (metrics.NetIncome < 0) return "\u4e8f";
            if (metrics.BudgetStress >= 55 || (metrics.CashRunwayDays > 0 && metrics.CashRunwayDays <= 45)) return "\u538b";
            return "\u7a33";
        }

        private static bool CashCapsuleBudgetPressure(CityMetrics metrics)
        {
            return metrics.Cash < 0
                || metrics.NetIncome < 0
                || metrics.BudgetStress >= 55
                || (metrics.CashRunwayDays > 0 && metrics.CashRunwayDays <= 45);
        }

        private static bool PopulationCapsuleAtCapacity(CityMetrics metrics)
        {
            return metrics.HousingCapacity > 0 && metrics.Population >= metrics.HousingCapacity - 8;
        }

        private static Color32 PopulationCapsuleSurface(CityMetrics metrics)
        {
            return PopulationCapsuleAtCapacity(metrics)
                ? new Color32(74, 68, 42, 238)
                : new Color32(30, 66, 43, 238);
        }

        private static Color32 PopulationCapsuleAccent(CityMetrics metrics)
        {
            return PopulationCapsuleAtCapacity(metrics)
                ? new Color32(255, 202, 70, 255)
                : new Color32(96, 190, 122, 255);
        }

        private static string PopulationCapsuleStateLabel(CityMetrics metrics)
        {
            if (PopulationCapsuleAtCapacity(metrics)) return "\u6ee1";
            if (metrics.HousingCapacity > metrics.Population && metrics.Happiness >= 55) return "\u589e";
            return "\u7a33";
        }

        private static string PopulationCapsuleDetailText(CityMetrics metrics)
        {
            if (PopulationCapsuleAtCapacity(metrics))
            {
                return AppendTopCapsuleOrderCue("\u4f4f\u623f\u63a5\u8fd1\u6ee1\u8f7d", metrics);
            }

            return AppendTopCapsuleOrderCue("\u4f59\u91cf " + Mathf.Max(0, metrics.HousingCapacity - metrics.Population), metrics);
        }

        private static Color32 HappinessCapsuleSurface(CityMetrics metrics)
        {
            return metrics.Happiness < 50
                ? new Color32(78, 57, 45, 238)
                : new Color32(30, 66, 43, 238);
        }

        private static Color32 HappinessCapsuleAccent(CityMetrics metrics)
        {
            return metrics.Happiness < 50
                ? new Color32(236, 116, 56, 255)
                : new Color32(255, 220, 86, 255);
        }

        private static string HappinessCapsuleStateLabel(CityMetrics metrics)
        {
            if (metrics.Happiness < 50) return "\u4f4e";
            if (metrics.ForecastRisk >= 65) return "\u9669";
            return "\u7a33";
        }

        private static string HappinessCapsuleDetailText(CityMetrics metrics)
        {
            if (metrics.Happiness < 50)
            {
                return AppendTopCapsuleOrderCue("\u5e02\u6c11\u538b\u529b\u9ad8", metrics);
            }

            if (metrics.ForecastRisk >= 65)
            {
                return AppendTopCapsuleOrderCue("\u98ce\u9669 " + metrics.ForecastRisk, metrics);
            }

            return AppendTopCapsuleOrderCue("\u57ce\u5e02\u8fd0\u884c\u7a33", metrics);
        }

        private static string AppendTopCapsuleOrderCue(string detail, CityMetrics metrics)
        {
            var cue = TopCapsuleOrderCue(metrics);
            if (string.IsNullOrEmpty(cue))
            {
                return detail;
            }

            return CompactCardText(detail, 8) + "  " + cue;
        }

        private static string TopCapsuleOrderCue(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return "\u5355\u53ef\u9886";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                var progress = Mathf.Clamp(metrics.ActiveObjective.Progress, 0, metrics.ActiveObjective.Required);
                return "\u5355" + progress + "/" + metrics.ActiveObjective.Required;
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347+" + metrics.BuildingUpgradeReadyCount;
            }

            return string.Empty;
        }

        private void BuildMilestoneRibbon(Transform parent)
        {
            // REFERENCE_IMAGE_MILESTONE_RIBBON adds the yellow tab from the right-side task card.
            var ribbon = CreatePanel(parent, "Milestone Ribbon", AnchorFree(), Vector2.zero, Vector2.zero);
            ribbon.GetComponent<Image>().color = new Color32(255, 211, 93, 248);
            ribbon.AddComponent<LayoutElement>().preferredHeight = 24f;
            var text = CreateText(ribbon.transform, "Label", "\u89e3\u9501\u65b0\u533a / \u4e0b\u4e00\u6b65", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color32(43, 64, 70, 255);
            Stretch(text.rectTransform);
        }

        private void BuildMilestoneTaskPreview(Transform parent)
        {
            // REFERENCE_IMAGE_TASK_THUMBNAIL_CARD mirrors the right-side objective card with art and progress.
            var card = CreatePanel(parent, "Milestone Visual Task Card", AnchorFree(), Vector2.zero, Vector2.zero);
            milestoneTaskPreviewImage = card.GetComponent<Image>();
            milestoneTaskPreviewImage.color = new Color32(253, 255, 246, 248);
            card.AddComponent<LayoutElement>().preferredHeight = 82f;
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(65, 183, 190, 158);
            outline.effectDistance = new Vector2(2f, -2f);
            milestoneTaskPreviewOutline = outline;

            var priority = CreatePanel(card.transform, "Task Priority Rail", new Vector4(0f, 0f, 0f, 1f), new Vector2(4f, 7f), new Vector2(8f, -7f));
            milestoneTaskPriorityStrip = priority.GetComponent<Image>();
            milestoneTaskPriorityStrip.color = new Color32(255, 207, 86, 224);
            milestoneTaskPriorityStrip.raycastTarget = false;
            priority.AddComponent<LayoutElement>().ignoreLayout = true;

            var thumbnail = CreatePanel(card.transform, "Task Thumbnail", new Vector4(0f, 0f, 0f, 1f), new Vector2(10f, 8f), new Vector2(72f, -8f));
            thumbnail.GetComponent<Image>().color = new Color32(166, 226, 132, 250);
            var thumbOutline = thumbnail.AddComponent<Outline>();
            thumbOutline.effectColor = new Color32(255, 255, 255, 126);
            thumbOutline.effectDistance = new Vector2(1.5f, -1.5f);
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb River", new Color32(86, 188, 220, 248), AnchorBottom(), new Vector2(0f, 0f), new Vector2(0f, 20f));
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Bridge", new Color32(237, 226, 151, 248), AnchorBottom(), new Vector2(8f, 18f), new Vector2(-8f, 27f));
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Parcel", new Color32(255, 202, 70, 242), new Vector4(0.54f, 0.48f, 1f, 1f), new Vector2(0f, 0f), new Vector2(-8f, -8f));
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Tree", new Color32(86, 190, 83, 248), new Vector4(0f, 0.52f, 0.38f, 1f), new Vector2(8f, 2f), new Vector2(0f, -10f));
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Sun Facet", new Color32(245, 255, 238, 98), new Vector4(0.2f, 0.72f, 0.66f, 1f), new Vector2(0f, 0f), new Vector2(0f, -6f));
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Route", new Color32(43, 64, 70, 154), new Vector4(0f, 0.45f, 1f, 0.45f), new Vector2(10f, -2f), new Vector2(-10f, 3f));
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Target", new Color32(245, 255, 238, 188), new Vector4(0.66f, 0.62f, 0.9f, 0.86f), Vector2.zero, Vector2.zero);
            AddTaskThumbnailBlock(thumbnail.transform, "Task Thumb Service Pin", new Color32(255, 207, 86, 230), new Vector4(0.16f, 0.62f, 0.32f, 0.78f), Vector2.zero, Vector2.zero);
            AddTaskThumbnailRotatedBlock(thumbnail.transform, "Task Thumb Avenue", new Color32(43, 64, 70, 132), new Vector4(0.08f, 0.32f, 0.9f, 0.32f), new Vector2(0f, -1.8f), new Vector2(0f, 2.2f), -18f);
            AddTaskThumbnailRotatedBlock(thumbnail.transform, "Task Thumb Bridge Glint", new Color32(245, 255, 238, 132), new Vector4(0.18f, 0.48f, 0.76f, 0.48f), new Vector2(0f, -1.2f), new Vector2(0f, 1.8f), -18f);
            AddTaskThumbnailRotatedBlock(thumbnail.transform, "Task Thumb Unlock Dash A", new Color32(245, 255, 238, 174), new Vector4(0.58f, 0.78f, 0.9f, 0.78f), new Vector2(0f, -1.2f), new Vector2(0f, 1.4f), -18f);
            AddTaskThumbnailRotatedBlock(thumbnail.transform, "Task Thumb Unlock Dash B", new Color32(245, 255, 238, 142), new Vector4(0.58f, 0.58f, 0.9f, 0.58f), new Vector2(0f, -1.2f), new Vector2(0f, 1.4f), -18f);
            AddTaskThumbnailRotatedBlock(thumbnail.transform, "Task Thumb Unlock Dash C", new Color32(245, 255, 238, 154), new Vector4(0.56f, 0.56f, 0.56f, 0.84f), new Vector2(-1.3f, 0f), new Vector2(1.3f, 0f), -18f);
            AddTaskThumbnailRotatedBlock(thumbnail.transform, "Task Thumb Unlock Dash D", new Color32(245, 255, 238, 154), new Vector4(0.9f, 0.56f, 0.9f, 0.84f), new Vector2(-1.3f, 0f), new Vector2(1.3f, 0f), -18f);

            var stamp = CreatePanel(thumbnail.transform, "Task Thumbnail Stamp", new Vector4(0f, 0f, 1f, 0f), new Vector2(5f, 5f), new Vector2(-5f, 19f));
            milestoneTaskStampImage = stamp.GetComponent<Image>();
            milestoneTaskStampImage.color = new Color32(38, 90, 76, 204);
            milestoneTaskStampImage.raycastTarget = false;
            milestoneTaskStampText = CreateText(stamp.transform, "Stamp Text", "\u89e3\u9501\u65b0\u533a", 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            milestoneTaskStampText.color = new Color32(245, 255, 238, 255);
            Stretch(milestoneTaskStampText.rectTransform);
            stamp.AddComponent<LayoutElement>().ignoreLayout = true;

            var stageChip = CreatePanel(card.transform, "Task Stage Chip", new Vector4(1f, 1f, 1f, 1f), new Vector2(-62f, -26f), new Vector2(-8f, -8f));
            milestoneTaskStageImage = stageChip.GetComponent<Image>();
            milestoneTaskStageImage.color = new Color32(54, 176, 190, 232);
            milestoneTaskStageImage.raycastTarget = false;
            milestoneTaskStageText = CreateText(stageChip.transform, "Stage Text", "\u4e0b\u4e00\u6b65", 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            milestoneTaskStageText.color = new Color32(245, 255, 238, 255);
            Stretch(milestoneTaskStageText.rectTransform);
            stageChip.AddComponent<LayoutElement>().ignoreLayout = true;

            var detailBackplate = CreatePanel(card.transform, "Task Detail Backplate", new Vector4(0.34f, 0.43f, 0.98f, 0.78f), Vector2.zero, Vector2.zero);
            detailBackplate.GetComponent<Image>().color = new Color32(37, 103, 84, 18);
            detailBackplate.GetComponent<Image>().raycastTarget = false;
            detailBackplate.AddComponent<LayoutElement>().ignoreLayout = true;
            var detailDivider = CreatePanel(card.transform, "Task Detail Divider", new Vector4(0.34f, 0.41f, 0.98f, 0.41f), new Vector2(0f, -0.7f), new Vector2(0f, 0.7f));
            detailDivider.GetComponent<Image>().color = new Color32(65, 153, 142, 76);
            detailDivider.GetComponent<Image>().raycastTarget = false;
            detailDivider.AddComponent<LayoutElement>().ignoreLayout = true;

            milestoneTaskPreviewText = CreateText(card.transform, "Task Preview Text", "\u4e0b\u4e00\u6b65\uff1a\u89e3\u9501\u65b0\u533a\n\u8fdb\u5ea6 0/1", 12, FontStyle.Bold, TextAnchor.UpperLeft);
            milestoneTaskPreviewText.color = new Color32(43, 64, 70, 255);
            milestoneTaskPreviewText.lineSpacing = 0.88f;
            milestoneTaskPreviewText.resizeTextForBestFit = true;
            milestoneTaskPreviewText.resizeTextMinSize = 9;
            milestoneTaskPreviewText.resizeTextMaxSize = 12;
            milestoneTaskPreviewText.rectTransform.anchorMin = new Vector2(0f, 0.36f);
            milestoneTaskPreviewText.rectTransform.anchorMax = new Vector2(1f, 1f);
            milestoneTaskPreviewText.rectTransform.offsetMin = new Vector2(82f, 0f);
            milestoneTaskPreviewText.rectTransform.offsetMax = new Vector2(-66f, -8f);

            var rewardStrip = CreatePanel(card.transform, "Task Reward Strip", new Vector4(0f, 0f, 1f, 0f), new Vector2(82f, 20f), new Vector2(-8f, 37f));
            milestoneTaskRewardStripImage = rewardStrip.GetComponent<Image>();
            milestoneTaskRewardStripImage.color = new Color32(255, 236, 150, 232);
            milestoneTaskRewardText = CreateText(rewardStrip.transform, "Task Reward Text", "\u5956\u52b1 \u91d1+2000 / \u4eba+20", 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            milestoneTaskRewardText.color = new Color32(83, 68, 30, 255);
            milestoneTaskRewardText.resizeTextForBestFit = true;
            milestoneTaskRewardText.resizeTextMinSize = 7;
            milestoneTaskRewardText.resizeTextMaxSize = 10;
            Stretch(milestoneTaskRewardText.rectTransform);
            milestoneTaskRewardText.rectTransform.offsetMin = new Vector2(36f, 0f);
            AddMilestoneRewardPip(rewardStrip.transform, 4f, "\u91d1", new Color32(255, 203, 70, 248));
            AddMilestoneRewardPip(rewardStrip.transform, 21f, "\u4eba", new Color32(96, 214, 118, 238));

            var progressDivider = CreatePanel(card.transform, "Task Progress Divider", new Vector4(0.34f, 0.2f, 0.98f, 0.2f), new Vector2(0f, -0.7f), new Vector2(0f, 0.7f));
            progressDivider.GetComponent<Image>().color = new Color32(255, 207, 86, 66);
            progressDivider.GetComponent<Image>().raycastTarget = false;
            progressDivider.AddComponent<LayoutElement>().ignoreLayout = true;

            var progressTrack = CreatePanel(card.transform, "Task Preview Progress", new Vector4(0f, 0f, 1f, 0f), new Vector2(82f, 8f), new Vector2(-8f, 18f));
            progressTrack.GetComponent<Image>().color = new Color32(218, 238, 222, 242);
            var progressOutline = progressTrack.AddComponent<Outline>();
            progressOutline.effectColor = new Color32(255, 255, 255, 146);
            progressOutline.effectDistance = new Vector2(1.2f, -1.2f);
            var fillObject = new GameObject("Task Preview Progress Fill");
            fillObject.transform.SetParent(progressTrack.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            milestoneTaskProgressFill = fillObject.AddComponent<Image>();
            milestoneTaskProgressFill.color = new Color32(86, 198, 104, 242);
            AddTaskPreviewProgressTicks(progressTrack.transform);
            milestoneTaskProgressCap = AddTaskPreviewProgressCap(progressTrack.transform);
            milestoneTaskProgressLabel = CreateText(progressTrack.transform, "Task Preview Progress Label", "0/1", 8, FontStyle.Bold, TextAnchor.MiddleRight);
            milestoneTaskProgressLabel.color = new Color32(43, 64, 70, 232);
            milestoneTaskProgressLabel.raycastTarget = false;
            Stretch(milestoneTaskProgressLabel.rectTransform);
            milestoneTaskProgressLabel.rectTransform.offsetMin = new Vector2(5f, 0f);
            milestoneTaskProgressLabel.rectTransform.offsetMax = new Vector2(-5f, 0f);
            AddTaskPreviewCardFacets(card.transform);
        }

        private void AddMilestoneRewardPip(Transform parent, float x, string label, Color32 color)
        {
            var pip = CreatePanel(parent, "Task Reward Pip " + label, new Vector4(0f, 0.5f, 0f, 0.5f), new Vector2(x, -6f), new Vector2(x + 14f, 6f));
            var image = pip.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var text = CreateText(pip.transform, "Label", label, 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color32(43, 64, 70, 248);
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            var layout = pip.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private Image AddTaskPreviewProgressCap(Transform parent)
        {
            // REFERENCE_IMAGE_TASK_PROGRESS_CAP gives the task bar a crisp city-builder progress endpoint.
            var cap = new GameObject("Task Preview Progress Cap");
            cap.transform.SetParent(parent, false);
            var rect = cap.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(7f, 15f);
            rect.anchoredPosition = Vector2.zero;
            var image = cap.AddComponent<Image>();
            image.color = new Color32(245, 255, 238, 238);
            image.raycastTarget = false;
            var layout = cap.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private void AddTaskThumbnailBlock(Transform parent, string name, Color32 color, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax)
        {
            var block = CreatePanel(parent, name, anchors, offsetMin, offsetMax);
            var image = block.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            milestoneTaskThumbnailBlocks.Add(image);
        }

        private void AddTaskThumbnailRotatedBlock(Transform parent, string name, Color32 color, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax, float rotation)
        {
            var block = CreatePanel(parent, name, anchors, offsetMin, offsetMax);
            var image = block.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = block.GetComponent<RectTransform>();
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var layout = block.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private void AddTaskPreviewProgressTicks(Transform parent)
        {
            // REFERENCE_IMAGE_TASK_PROGRESS_TICKS makes the milestone progress bar read like the mockup card.
            for (var i = 1; i < 4; i += 1)
            {
                var tick = new GameObject("Task Preview Progress Tick " + i);
                tick.transform.SetParent(parent, false);
                var rect = tick.AddComponent<RectTransform>();
                var anchor = i / 4f;
                rect.anchorMin = new Vector2(anchor, 0f);
                rect.anchorMax = new Vector2(anchor, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1.5f, 0f);
                var image = tick.AddComponent<Image>();
                image.color = new Color32(245, 255, 238, 124);
                image.raycastTarget = false;
                var layout = tick.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;
            }
        }

        private void AddTaskPreviewCardFacets(Transform parent)
        {
            // REFERENCE_IMAGE_TASK_CARD_GEOMETRY adds small bright facets around the reward card.
            AddHudFacet(parent, "Task Preview Upper Spark", new Vector4(0.78f, 0.77f, 0.96f, 0.9f), Vector2.zero, Vector2.zero, new Color32(255, 255, 255, 62), -8f);
            AddHudFacet(parent, "Task Preview Reward Glow", new Vector4(0.37f, 0.2f, 0.96f, 0.38f), Vector2.zero, Vector2.zero, new Color32(255, 207, 86, 58), 0f);
            AddHudFacet(parent, "Task Preview Thumbnail Shine", new Vector4(0.05f, 0.72f, 0.24f, 0.9f), Vector2.zero, Vector2.zero, new Color32(255, 255, 255, 72), -12f);
            AddHudFacet(parent, "Task Preview Planning Fold", new Vector4(0.58f, 0.58f, 0.96f, 0.68f), Vector2.zero, Vector2.zero, new Color32(65, 183, 190, 46), 0f);
            AddHudFacet(parent, "Task Preview Priority Tab", new Vector4(0.08f, 0.08f, 0.28f, 0.2f), Vector2.zero, Vector2.zero, new Color32(255, 207, 86, 66), 0f);
        }

        private void RefreshMilestoneTaskPreview(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            if (milestoneTaskPreviewText == null || milestoneTaskProgressFill == null)
            {
                return;
            }

            var required = Mathf.Max(1, snapshot.ObjectiveRequired);
            var progress = Mathf.Clamp(snapshot.ObjectiveProgress, 0, required);
            var amount = Mathf.Clamp01(progress / (float)required);
            milestoneTaskProgressFill.rectTransform.anchorMax = new Vector2(amount, 1f);
            milestoneTaskProgressFill.color = objectivePulseTimer > 0f
                ? (snapshot.ObjectiveDone ? new Color32(86, 190, 83, 255) : new Color32(255, 202, 70, 255))
                : snapshot.ObjectiveDone
                ? new Color32(86, 190, 83, 245)
                : new Color32(65, 169, 184, 235);

            if (milestoneTaskProgressCap != null)
            {
                var capRect = milestoneTaskProgressCap.rectTransform;
                capRect.anchorMin = new Vector2(amount, 0.5f);
                capRect.anchorMax = new Vector2(amount, 0.5f);
                capRect.anchoredPosition = Vector2.zero;
                milestoneTaskProgressCap.color = snapshot.ObjectiveDone
                    ? new Color32(245, 255, 238, 248)
                    : (objectivePulseTimer > 0f ? new Color32(255, 248, 198, 248) : new Color32(255, 207, 86, 238));
            }

            if (milestoneTaskProgressLabel != null)
            {
                milestoneTaskProgressLabel.text = progress + "/" + required;
                milestoneTaskProgressLabel.color = snapshot.ObjectiveDone
                    ? new Color32(35, 95, 62, 245)
                    : new Color32(43, 64, 70, 232);
            }

            var title = snapshot.ObjectiveDone
                ? "\u65b0\u533a\u5df2\u89e3\u9501"
                : "\u4e0b\u4e00\u6b65\uff1a\u89e3\u9501\u65b0\u533a";
            var detail = snapshot.ObjectiveDone
                ? "\u53ef\u9886\u53d6\u5956\u52b1"
                : CompactCardText(string.IsNullOrEmpty(snapshot.ObjectiveTitle) ? "\u5b8c\u6210\u5f53\u524d\u76ee\u6807" : snapshot.ObjectiveTitle, 8);
            milestoneTaskPreviewText.text = title + "\n" + detail + " " + progress + "/" + required + ObjectivePulseInlineText();

            if (milestoneTaskRewardStripImage != null)
            {
                milestoneTaskRewardStripImage.color = snapshot.ObjectiveDone
                    ? new Color32(210, 246, 192, 238)
                    : (objectivePulseTimer > 0f ? new Color32(255, 232, 126, 242) : new Color32(255, 240, 168, 234));
            }

            if (milestoneTaskRewardText != null)
            {
                var cashReward = snapshot.ObjectiveDone ? 2500 : 2000;
                var populationReward = metrics != null && metrics.Population >= 240 ? 30 : 20;
                milestoneTaskRewardText.text = BuildMilestoneRewardLine(snapshot, metrics, cashReward, populationReward);
                milestoneTaskRewardText.color = snapshot.ObjectiveDone
                    ? new Color32(35, 95, 62, 255)
                    : new Color32(83, 68, 30, 255);
            }

            if (milestoneTaskPreviewImage != null)
            {
                milestoneTaskPreviewImage.color = objectivePulseTimer > 0f
                    ? (snapshot.ObjectiveDone ? new Color32(221, 255, 206, 252) : new Color32(255, 244, 190, 252))
                    : snapshot.ObjectiveDone
                    ? new Color32(235, 252, 220, 248)
                    : new Color32(249, 255, 240, 246);
            }

            if (milestoneTaskPreviewOutline != null)
            {
                var severity = AdvisorSeverityColor(metrics);
                milestoneTaskPreviewOutline.effectColor = new Color32(severity.r, severity.g, severity.b, 150);
                milestoneTaskPreviewOutline.effectDistance = AdvisorSeverityOutlineDistance(metrics);
                if (milestoneTaskPriorityStrip != null)
                {
                    milestoneTaskPriorityStrip.color = new Color32(severity.r, severity.g, severity.b, 224);
                }
            }

            RefreshMilestoneTaskStage(snapshot, metrics);
            RefreshMilestoneTaskThumbnail(snapshot, metrics);
        }

        private void RefreshMilestoneTaskStage(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            // CITY_TASK_CARD_STAGE_CHIPS turn the advisor card into a readable work order.
            var done = snapshot != null && snapshot.ObjectiveDone;
            var warning = metrics != null && (metrics.ForecastRisk >= 65 || metrics.ServiceGapPressure >= 60 || metrics.RoadBottleneckPressure >= 60);
            var accent = done
                ? new Color32(86, 190, 83, 236)
                : warning
                    ? new Color32(224, 106, 82, 236)
                    : new Color32(65, 169, 184, 226);

            if (milestoneTaskStageImage != null)
            {
                milestoneTaskStageImage.color = accent;
            }

            if (milestoneTaskStageText != null)
            {
                milestoneTaskStageText.text = MilestoneStageLabel(snapshot, metrics, warning);
                milestoneTaskStageText.color = done || warning
                    ? new Color32(245, 255, 238, 255)
                    : new Color32(232, 255, 255, 255);
            }

            if (milestoneTaskStampImage != null)
            {
                milestoneTaskStampImage.color = done
                    ? new Color32(86, 190, 83, 220)
                    : warning
                        ? new Color32(83, 55, 24, 202)
                        : new Color32(43, 64, 70, 188);
            }

            if (milestoneTaskStampText != null)
            {
                milestoneTaskStampText.text = done ? "\u5b8c\u6210" : "\u89e3\u9501\u65b0\u533a";
            }
        }

        private static string BuildMilestoneRewardLine(CityHudSnapshot snapshot, CityMetrics metrics, int cashReward, int populationReward)
        {
            if (snapshot != null && snapshot.ObjectiveDone)
            {
                return "\u9886\u53d6 \u91d1+" + cashReward + " / \u4eba+" + populationReward + " > \u89c4\u5212\u65b0\u533a";
            }

            var state = TaskCardStateLabel(snapshot, metrics);
            var action = CompactCardText(MilestoneActionHint(snapshot, metrics), 8);
            return state + " \u91d1+" + cashReward + " \u4eba+" + populationReward + " > " + action;
        }

        private static string MilestoneStageLabel(CityHudSnapshot snapshot, CityMetrics metrics, bool warning)
        {
            if (snapshot != null && snapshot.ObjectiveDone)
            {
                return "\u53ef\u9886\u53d6";
            }

            if (warning)
            {
                return "\u9ad8\u4f18\u5148";
            }

            if (snapshot != null && snapshot.ObjectiveProgress > 0)
            {
                return "\u63a8\u8fdb\u4e2d";
            }

            return "\u4e0b\u4e00\u6b65";
        }

        private static string MilestoneActionHint(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            if (metrics != null)
            {
                if (metrics.RoadBottleneckPressure >= 60) return "\u4fee\u74f6\u9888\u8def";
                if (metrics.ServiceGapPressure >= 60) return "\u8865\u670d\u52a1";
                if (metrics.ForecastRisk >= 65 && !string.IsNullOrEmpty(metrics.ForecastAction)) return metrics.ForecastAction;
                if (metrics.DemandUrgency >= 50) return "\u8865\u5206\u533a";
            }

            if (snapshot != null && !string.IsNullOrEmpty(snapshot.ExpansionStatusText))
            {
                return snapshot.ExpansionStatusText;
            }

            return "\u8fde\u63a5\u65b0\u533a";
        }

        private void BuildAdvisorRadar(Transform parent)
        {
            // CITY_OPERATIONS_RADAR makes the side card behave like a compact management dashboard.
            advisorRadarFills.Clear();
            advisorRadarLabels.Clear();
            AddAdvisorRadarLane(parent, 0, "\u9669");
            AddAdvisorRadarLane(parent, 1, "\u8def");
            AddAdvisorRadarLane(parent, 2, "\u670d");
            AddAdvisorRadarLane(parent, 3, "\u8d22");
        }

        private void BuildAdvisorRadarRow(Transform parent)
        {
            // CITY_OPERATIONS_RADAR_ROW separates the four pressure bars from prose so the card scans like CS2.
            var row = CreatePanel(parent, "Advisor Radar Row", AnchorFree(), Vector2.zero, Vector2.zero);
            var rowImage = row.GetComponent<Image>();
            rowImage.color = new Color32(43, 64, 70, 22);
            rowImage.raycastTarget = false;
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 26f;
            rowLayout.flexibleWidth = 1f;
            var outline = row.AddComponent<Outline>();
            outline.effectColor = new Color32(65, 169, 184, 70);
            outline.effectDistance = new Vector2(1f, -1f);

            advisorRadarTitleText = CreateText(row.transform, "Advisor Radar Title", "\u8fd0\u8425", 8, FontStyle.Bold, TextAnchor.MiddleLeft);
            advisorRadarTitleText.color = new Color32(43, 64, 70, 190);
            advisorRadarTitleText.raycastTarget = false;
            Stretch(advisorRadarTitleText.rectTransform);
            advisorRadarTitleText.rectTransform.offsetMin = new Vector2(6f, 1f);
            advisorRadarTitleText.rectTransform.offsetMax = new Vector2(-228f, -1f);
            var titleLayout = advisorRadarTitleText.GetComponent<LayoutElement>();
            if (titleLayout != null)
            {
                titleLayout.ignoreLayout = true;
            }

            var laneHost = CreatePanel(row.transform, "Advisor Radar Lanes", new Vector4(0f, 0f, 1f, 1f), new Vector2(42f, 0f), new Vector2(-4f, 0f));
            laneHost.GetComponent<Image>().color = new Color32(255, 255, 255, 0);
            laneHost.AddComponent<LayoutElement>().ignoreLayout = true;
            BuildAdvisorRadar(laneHost.transform);
        }

        private void AddAdvisorRadarLane(Transform parent, int index, string labelText)
        {
            var min = index * 0.25f;
            var max = (index + 1) * 0.25f;
            var lane = CreatePanel(parent, "Advisor Radar " + index, new Vector4(min, 0f, max, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f));
            var laneImage = lane.GetComponent<Image>();
            laneImage.color = new Color32(255, 255, 255, 0);
            laneImage.raycastTarget = false;
            var layout = lane.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var label = CreateText(lane.transform, "Radar Label", labelText + "--", 8, FontStyle.Bold, TextAnchor.UpperCenter);
            label.color = new Color32(43, 64, 70, 220);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(0f, 8f);
            label.rectTransform.offsetMax = new Vector2(0f, -1f);
            var labelLayout = label.GetComponent<LayoutElement>();
            if (labelLayout != null)
            {
                labelLayout.ignoreLayout = true;
            }

            var track = CreatePanel(lane.transform, "Radar Track", new Vector4(0f, 0f, 1f, 0f), new Vector2(5f, 2f), new Vector2(-5f, 7f));
            var trackImage = track.GetComponent<Image>();
            trackImage.color = new Color32(43, 64, 70, 52);
            trackImage.raycastTarget = false;
            track.AddComponent<LayoutElement>().ignoreLayout = true;

            var fillObject = new GameObject("Radar Fill");
            fillObject.transform.SetParent(track.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillObject.AddComponent<Image>();
            fill.color = new Color32(88, 204, 96, 228);
            fill.raycastTarget = false;

            advisorRadarLabels.Add(label);
            advisorRadarFills.Add(fill);
        }

        private void RefreshAdvisorRadar(CityMetrics metrics)
        {
            if (advisorRadarFills.Count < 4 || advisorRadarLabels.Count < 4)
            {
                return;
            }

            var risk = metrics != null ? Mathf.Clamp(metrics.ForecastRisk, 0, 100) : 0;
            var road = metrics != null ? RoadRadarPressure(metrics) : 0;
            var service = metrics != null ? ServiceRadarPressure(metrics) : 0;
            var fiscal = metrics != null ? FiscalRadarPressure(metrics) : 0;
            SetAdvisorRadarLane(0, "\u9669", risk);
            SetAdvisorRadarLane(1, "\u8def", road);
            SetAdvisorRadarLane(2, "\u670d", service);
            SetAdvisorRadarLane(3, "\u8d22", fiscal);
            if (advisorRadarTitleText != null)
            {
                var focus = metrics != null ? MiniMapPrimaryIssueLabel(metrics) : "--";
                var peak = Mathf.Max(risk, Mathf.Max(road, Mathf.Max(service, fiscal)));
                advisorRadarTitleText.text = "\u7126\u70b9 " + CompactCardText(focus, 3);
                advisorRadarTitleText.color = AdvisorRadarColor(peak);
            }
        }

        private void SetAdvisorRadarLane(int index, string label, int pressure)
        {
            if (index < 0 || index >= advisorRadarFills.Count || index >= advisorRadarLabels.Count)
            {
                return;
            }

            var clamped = Mathf.Clamp(pressure, 0, 100);
            var fill = advisorRadarFills[index];
            if (fill != null)
            {
                fill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.08f, clamped / 100f), 1f);
                fill.color = AdvisorRadarColor(clamped);
            }

            var text = advisorRadarLabels[index];
            if (text != null)
            {
                text.text = label + clamped;
                text.color = clamped >= 65
                    ? new Color32(128, 68, 36, 255)
                    : new Color32(43, 64, 70, 230);
            }
        }

        private static Color32 AdvisorRadarColor(int pressure)
        {
            if (pressure >= 72)
            {
                return new Color32(224, 106, 82, 238);
            }

            if (pressure >= 55)
            {
                return new Color32(255, 202, 70, 236);
            }

            if (pressure >= 35)
            {
                return new Color32(65, 169, 184, 224);
            }

            return new Color32(88, 204, 96, 220);
        }

        private static int RoadRadarPressure(CityMetrics metrics)
        {
            return Mathf.Clamp(Mathf.Max(metrics.RoadBottleneckPressure, Mathf.Max(metrics.Congestion, Mathf.Max(100 - metrics.CommuteEfficiency, metrics.CarDependency - 12))), 0, 100);
        }

        private static int ServiceRadarPressure(CityMetrics metrics)
        {
            return Mathf.Clamp(Mathf.Max(metrics.ServiceGapPressure, Mathf.Max(100 - metrics.ServiceCoverage, metrics.ServiceUtilization - 8)), 0, 100);
        }

        private static int FiscalRadarPressure(CityMetrics metrics)
        {
            var deficitPressure = metrics.NetIncome < 0 ? Mathf.Clamp(42 + (-metrics.NetIncome / 35), 0, 100) : 0;
            return Mathf.Clamp(Mathf.Max(metrics.BudgetStress, Mathf.Max(metrics.DebtPressure, Mathf.Max(100 - metrics.FiscalHealth, deficitPressure))), 0, 100);
        }

        private void RefreshMilestoneTaskThumbnail(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            // REFERENCE_IMAGE_DYNAMIC_TASK_THUMBNAIL turns the right task card art into a live layer cue.
            if (milestoneTaskThumbnailBlocks.Count < 8)
            {
                return;
            }

            var recommended = RecommendedOverlayMode(metrics);
            var accent = recommended == OverlayMode.Normal
                ? new Color32(96, 190, 122, 248)
                : OverlayModeAccentColor(recommended);
            var done = snapshot != null && snapshot.ObjectiveDone;
            SetMilestoneTaskThumbnailBlock(0, done ? new Color32(120, 210, 142, 248) : TaskThumbnailGroundColor(recommended));
            SetMilestoneTaskThumbnailBlock(1, done ? new Color32(197, 236, 156, 248) : new Color32(237, 226, 151, 248));
            SetMilestoneTaskThumbnailBlock(2, done ? new Color32(166, 224, 120, 242) : new Color32(accent.r, accent.g, accent.b, 242));
            SetMilestoneTaskThumbnailBlock(3, new Color32(86, 190, 83, 248));
            SetMilestoneTaskThumbnailBlock(4, new Color32(245, 255, 238, objectivePulseTimer > 0f ? (byte)138 : (byte)98));
            SetMilestoneTaskThumbnailBlock(5, TaskThumbnailRouteColor(recommended, done));
            SetMilestoneTaskThumbnailBlock(6, done ? new Color32(245, 255, 238, 214) : TaskThumbnailTargetColor(recommended));
            SetMilestoneTaskThumbnailBlock(7, done ? new Color32(255, 236, 150, 236) : new Color32(255, 207, 86, 230));
            if (milestoneTaskStampText != null && !done)
            {
                milestoneTaskStampText.text = TaskThumbnailStampLabel(recommended);
            }
        }

        private void SetMilestoneTaskThumbnailBlock(int index, Color32 color)
        {
            if (index >= 0 && index < milestoneTaskThumbnailBlocks.Count && milestoneTaskThumbnailBlocks[index] != null)
            {
                milestoneTaskThumbnailBlocks[index].color = color;
            }
        }

        private static Color32 TaskThumbnailGroundColor(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking)
            {
                return new Color32(99, 118, 118, 248);
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Waste)
            {
                return new Color32(86, 188, 220, 248);
            }

            if (mode == OverlayMode.Pollution)
            {
                return new Color32(150, 206, 98, 248);
            }

            return new Color32(154, 220, 104, 248);
        }

        private static Color32 TaskThumbnailRouteColor(OverlayMode mode, bool done)
        {
            if (done)
            {
                return new Color32(245, 255, 238, 188);
            }

            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking)
            {
                return new Color32(245, 255, 238, 208);
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Waste)
            {
                return new Color32(86, 188, 220, 214);
            }

            return new Color32(43, 64, 70, 154);
        }

        private static Color32 TaskThumbnailTargetColor(OverlayMode mode)
        {
            if (mode == OverlayMode.Services || mode == OverlayMode.Transit)
            {
                return new Color32(96, 214, 118, 220);
            }

            if (mode == OverlayMode.Traffic || mode == OverlayMode.Parking || mode == OverlayMode.RoadSafety)
            {
                return new Color32(244, 173, 66, 224);
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Waste)
            {
                return new Color32(86, 197, 224, 224);
            }

            return new Color32(245, 255, 238, 188);
        }

        private static string TaskThumbnailStampLabel(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety) return "\u4fee\u8def";
            if (mode == OverlayMode.Services) return "\u8865\u670d\u52a1";
            if (mode == OverlayMode.Transit) return "\u516c\u4ea4";
            if (mode == OverlayMode.Parking) return "\u505c\u8f66";
            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Waste) return "\u4fdd\u4f9b";
            if (mode == OverlayMode.Zoning) return "\u89c4\u5212";
            if (mode == OverlayMode.Pollution || mode == OverlayMode.LandValue) return "\u73af\u5883";
            if (mode == OverlayMode.Logistics) return "\u8d27\u8fd0";
            if (mode == OverlayMode.Communications) return "\u901a\u4fe1";
            return "\u89e3\u9501\u65b0\u533a";
        }

        private void BuildResourceLevelBadge(Transform parent)
        {
            // REFERENCE_IMAGE_RESOURCE_LEVEL_BADGE mirrors the bold rank medallion in the top-left card.
            var badge = new GameObject("Resource Level Badge");
            badge.transform.SetParent(parent, false);
            var rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(46f, 46f);
            rect.anchoredPosition = new Vector2(292f, -36f);
            var image = badge.AddComponent<Image>();
            image.color = new Color32(79, 113, 108, 236);
            resourceLevelBadgeImage = image;
            image.raycastTarget = false;
            var outline = badge.AddComponent<Outline>();
            outline.effectColor = new Color32(188, 225, 225, 185);
            outline.effectDistance = new Vector2(2f, -2f);
            var layout = badge.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            resourceLevelBadgeText = CreateText(badge.transform, "Level", "1", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            resourceLevelBadgeText.color = new Color32(245, 255, 238, 255);
            Stretch(resourceLevelBadgeText.rectTransform);
        }

        private Image CreateAdvisorSeverityStrip(Transform parent)
        {
            var stripObject = new GameObject("Advisor Severity Strip");
            stripObject.transform.SetParent(parent, false);
            var rect = stripObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(9f, 0f);
            rect.anchoredPosition = Vector2.zero;
            var image = stripObject.AddComponent<Image>();
            image.color = new Color32(88, 204, 96, 235);
            image.raycastTarget = false;
            var layout = stripObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private Image CreateAdvisorSeverityBadge(Transform parent)
        {
            // REFERENCE_IMAGE_TASK_CARD_STATUS_BADGE adds a compact city status chip to the milestone card.
            var badge = new GameObject("Advisor Severity Badge");
            badge.transform.SetParent(parent, false);
            var rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(28f, 18f);
            rect.anchoredPosition = new Vector2(-12f, -30f);
            var image = badge.AddComponent<Image>();
            image.color = new Color32(88, 204, 96, 218);
            image.raycastTarget = false;
            advisorSeverityBadgeText = CreateText(badge.transform, "Status", "\u7a33", 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            advisorSeverityBadgeText.color = new Color32(245, 255, 238, 255);
            Stretch(advisorSeverityBadgeText.rectTransform);
            var layout = badge.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private void BuildTurnActionPill(Transform root)
        {
            // REFERENCE_IMAGE_SIMULATION_STATUS_BADGE keeps the gold reference pill as the main playable action.
            var pill = CreatePanel(root, "Simulation Status Badge", AnchorBottomRight(), new Vector2(-266f, 116f), new Vector2(-100f, 168f));
            simulationStatusBadgeImage = pill.GetComponent<Image>();
            simulationStatusBadgeImage.color = new Color32(255, 190, 57, 245);
            var outline = pill.AddComponent<Outline>();
            outline.effectColor = new Color32(140, 103, 31, 130);
            outline.effectDistance = new Vector2(1.8f, -1.8f);
            var button = pill.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (controller != null)
                {
                    controller.CycleSimulationSpeed();
                }
            });
            AddHudFacet(pill.transform, "Turn Button Shine", new Vector4(0.1f, 0.62f, 0.9f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 42), -7f);

            var icon = CreatePanel(pill.transform, "Turn Button Play Icon", AnchorLeft(), new Vector2(10f, 9f), new Vector2(42f, -9f));
            simulationStatusBadgeIconImage = icon.GetComponent<Image>();
            simulationStatusBadgeIconImage.color = new Color32(253, 255, 248, 240);
            simulationStatusBadgeIconImage.raycastTarget = false;
            simulationStatusBadgeIconText = CreateText(icon.transform, "Glyph", "\u25b6", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            simulationStatusBadgeIconText.color = new Color32(255, 178, 45, 255);
            simulationStatusBadgeIconText.raycastTarget = false;
            Stretch(simulationStatusBadgeIconText.rectTransform);
            simulationStatusBadgeIconText.rectTransform.offsetMin = new Vector2(2f, 0f);

            simulationStatusBadgeText = CreateText(pill.transform, "Label", "\u5b8c\u6210\u56de\u5408", 15, FontStyle.Bold, TextAnchor.MiddleLeft);
            simulationStatusBadgeText.color = new Color32(83, 68, 30, 255);
            Stretch(simulationStatusBadgeText.rectTransform);
            simulationStatusBadgeText.rectTransform.offsetMin = new Vector2(50f, 15f);
            simulationStatusBadgeText.rectTransform.offsetMax = new Vector2(-54f, -4f);

            simulationStatusRewardBadgeImage = CreateToolButtonAccent(pill.transform, "Turn Reward Badge", AnchorTopRight(), new Vector2(-48f, -18f), new Vector2(-8f, -4f), new Color32(255, 236, 150, 220));
            simulationStatusRewardBadgeImage.raycastTarget = false;
            simulationStatusRewardBadgeText = CreateText(simulationStatusRewardBadgeImage.transform, "Reward", "\u5956\u52b1", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            simulationStatusRewardBadgeText.color = new Color32(83, 68, 30, 255);
            simulationStatusRewardBadgeText.raycastTarget = false;
            Stretch(simulationStatusRewardBadgeText.rectTransform);

            simulationStatusBadgeSubText = CreateText(pill.transform, "Sub Label", "x1  \u6b63\u5e38\u63a8\u8fdb", 9, FontStyle.Bold, TextAnchor.MiddleLeft);
            simulationStatusBadgeSubText.color = new Color32(103, 82, 32, 230);
            simulationStatusBadgeSubText.raycastTarget = false;
            Stretch(simulationStatusBadgeSubText.rectTransform);
            simulationStatusBadgeSubText.rectTransform.offsetMin = new Vector2(50f, 4f);
            simulationStatusBadgeSubText.rectTransform.offsetMax = new Vector2(-12f, -28f);
        }

        private void BuildRightCommandStack(Transform root)
        {
            // CITY_SKYLINES_RIGHT_COMMAND_STACK adds the view/diagnose/build actions beside the task card.
            rightCommandButtons.Clear();
            var stack = CreatePanel(root, "Right Command Stack", AnchorBottomRight(), new Vector2(-180f, 166f), new Vector2(-104f, 320f));
            var image = stack.GetComponent<Image>();
            rightCommandStackImage = image;
            image.color = new Color32(22, 57, 39, 232);
            AddSoftCardShadow(stack, 42);
            AddPanelTopAccent(stack, new Color32(65, 183, 190, 174), 3f);
            var outline = stack.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 112);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 7, 6);
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            rightCommandStackAccentImage = AddRightCommandHeader(stack.transform);
            BuildRightCommandButton(stack.transform, 0, "\u89c6\u89d2", "\u25c7", new Color32(206, 238, 216, 238), () =>
            {
                if (cameraController != null)
                {
                    cameraController.FrameMap();
                }

                if (controller != null)
                {
                    controller.PublishHudFeedback("\u89c6\u89d2\uff1a\u5df2\u56de\u5230\u57ce\u5e02\u5168\u666f\uff0c\u53ef\u5728\u53f3\u4e0b\u5c0f\u5730\u56fe\u7ee7\u7eed\u7f29\u653e", true);
                }
            });
            BuildRightCommandButton(stack.transform, 1, "\u64a4\u9500", "\u21a9", new Color32(206, 238, 216, 238), () =>
            {
                if (interaction != null)
                {
                    interaction.CancelActivePlanning();
                    if (controller != null)
                    {
                        controller.PublishHudFeedback("\u64a4\u9500\uff1a\u5df2\u56de\u5230\u67e5\u770b\u72b6\u6001", true);
                    }
                }
                else if (controller != null)
                {
                    controller.PublishHudFeedback("\u64a4\u9500 \u5df2\u56de\u5230\u67e5\u770b\u72b6\u6001", true);
                }
            });
            BuildRightCommandButton(stack.transform, 2, "\u9053\u8def", "\u8def", new Color32(255, 207, 86, 238), () =>
            {
                if (interaction != null)
                {
                    interaction.SelectRoadTool();
                }

                if (controller != null)
                {
                    controller.SetOverlay(OverlayMode.Traffic);
                    controller.PublishHudFeedback("\u9053\u8def\uff1a\u5df2\u5207\u5230\u4ea4\u901a\u5c42\uff0c\u8fde\u63a5\u65b0\u533a\u6216\u4fee\u901a\u74f6\u9888\u8def", true);
                }
            });
            BuildRightCommandButton(stack.transform, 3, "\u63a8\u8350", "\u25c6", new Color32(96, 214, 118, 238), SelectRecommendedCommandTool);
        }

        private Image AddRightCommandHeader(Transform parent)
        {
            var header = CreatePanel(parent, "Command Stack Header", AnchorFree(), Vector2.zero, Vector2.zero);
            header.GetComponent<Image>().color = new Color32(245, 255, 238, 24);
            header.AddComponent<LayoutElement>().preferredHeight = 20f;
            rightCommandStackHintText = CreateText(header.transform, "Hint", "\u57ce\u5e02\u64cd\u4f5c", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            rightCommandStackHintText.color = new Color32(245, 255, 238, 238);
            rightCommandStackHintText.lineSpacing = 0.82f;
            rightCommandStackHintText.resizeTextForBestFit = true;
            rightCommandStackHintText.resizeTextMinSize = 6;
            rightCommandStackHintText.resizeTextMaxSize = 8;
            Stretch(rightCommandStackHintText.rectTransform);

            var accent = CreateToolButtonAccent(header.transform, "Header Accent", AnchorBottom(), new Vector2(6f, 1f), new Vector2(-6f, 4f), new Color32(65, 183, 190, 180));
            accent.raycastTarget = false;
            return accent;
        }

        private void BuildRightCommandButton(Transform parent, int kind, string labelText, string glyphText, Color32 accent, UnityAction action)
        {
            var obj = CreatePanel(parent, "Command " + labelText, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = obj.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 34);
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color32(accent.r, accent.g, accent.b, 96);
            outline.effectDistance = new Vector2(1.1f, -1.1f);
            obj.AddComponent<LayoutElement>().preferredHeight = 26f;
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);

            var swatch = CreateToolButtonAccent(obj.transform, "Glyph Swatch", AnchorLeft(), new Vector2(4f, 4f), new Vector2(23f, -4f), accent);
            swatch.raycastTarget = false;
            var glyph = CreateText(swatch.transform, "Glyph", glyphText, 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(43, 64, 70, 255);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);

            var label = CreateText(obj.transform, "Label", labelText, 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.color = new Color32(245, 255, 238, 255);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(29f, 0f);
            label.rectTransform.offsetMax = new Vector2(-4f, 0f);
            var rail = CreateToolButtonAccent(obj.transform, "Command State Rail", AnchorRight(), new Vector2(-5f, 5f), new Vector2(-2f, -5f), new Color32(0, 0, 0, 0));
            rail.raycastTarget = false;
            var pressureTrack = CreatePanel(obj.transform, "Command Pressure Track", AnchorBottom(), new Vector2(29f, 2f), new Vector2(-9f, 5f));
            var pressureTrackImage = pressureTrack.GetComponent<Image>();
            pressureTrackImage.color = new Color32(245, 255, 238, 42);
            pressureTrackImage.raycastTarget = false;
            pressureTrack.AddComponent<LayoutElement>().ignoreLayout = true;
            var pressureFill = CreatePanel(pressureTrack.transform, "Command Pressure Fill", AnchorStretch(), Vector2.zero, Vector2.zero);
            var pressureFillImage = pressureFill.GetComponent<Image>();
            pressureFillImage.color = new Color32(accent.r, accent.g, accent.b, 0);
            pressureFillImage.raycastTarget = false;
            pressureFill.AddComponent<LayoutElement>().ignoreLayout = true;
            var stateText = CreateText(obj.transform, "Command State Text", string.Empty, 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            stateText.color = new Color32(245, 255, 238, 0);
            stateText.raycastTarget = false;
            stateText.rectTransform.anchorMin = new Vector2(1f, 1f);
            stateText.rectTransform.anchorMax = new Vector2(1f, 1f);
            stateText.rectTransform.pivot = new Vector2(1f, 1f);
            stateText.rectTransform.sizeDelta = new Vector2(14f, 10f);
            stateText.rectTransform.anchoredPosition = new Vector2(-7f, -3f);
            stateText.GetComponent<LayoutElement>().ignoreLayout = true;
            AddHudFacet(obj.transform, "Command Facet", new Vector4(0.48f, 0.54f, 0.96f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 26), -8f);
            rightCommandButtons.Add(new RightCommandBinding
            {
                Card = image,
                Swatch = swatch,
                StateRail = rail,
                PressureFill = pressureFillImage,
                Glyph = glyph,
                Label = label,
                StateText = stateText,
                Outline = outline,
                Accent = accent,
                Kind = kind
            });
        }

        private void SelectRecommendedCommandTool()
        {
            if (controller == null)
            {
                return;
            }

            var metrics = controller.Metrics;
            var recommended = RecommendedOverlayMode(metrics);
            controller.SetOverlay(recommended);

            if (interaction == null)
            {
                controller.PublishHudFeedback("\u63a8\u8350 \u5df2\u5207\u56fe\u5c42:" + OverlayLabel(recommended), true);
                return;
            }

            if (recommended == OverlayMode.Traffic || (metrics != null && metrics.RoadBottleneckPressure >= 58))
            {
                interaction.SelectRoadUpgradeTool();
                controller.PublishHudFeedback("\u63a8\u8350 \u770b\u4ea4\u901a > \u5347\u7ea7\u74f6\u9888\u8def", true);
                return;
            }

            if (recommended == OverlayMode.Services || (metrics != null && metrics.ServiceGapPressure >= 48))
            {
                interaction.SelectBuildingTool(AdvisorServiceBuildingId(metrics));
                controller.PublishHudFeedback("\u63a8\u8350 \u8865\u670d\u52a1 > \u653e\u7f3a\u53e3\u8def\u53e3", true);
                return;
            }

            if (recommended == OverlayMode.Zoning || (metrics != null && metrics.DemandUrgency >= 45))
            {
                interaction.SelectZoneTool(SelectedTileRecommendedZone(metrics));
                controller.PublishHudFeedback("\u63a8\u8350 \u8865\u5206\u533a > \u62d6\u5237\u9700\u6c42\u70ed\u533a", true);
                return;
            }

            interaction.SelectBuildingTool("pocket_park");
            controller.PublishHudFeedback("\u63a8\u8350 \u63d0\u5347\u5730\u4ef7 > \u653e\u516c\u56ed", true);
        }

        private void RefreshRightCommandStack(CityMetrics metrics)
        {
            if (rightCommandStackHintText == null && rightCommandStackAccentImage == null)
            {
                return;
            }

            var activeMode = controller != null ? controller.OverlayMode : OverlayMode.Normal;
            var recommended = MiniMapFocusOverlayMode(activeMode, metrics);
            var pressure = OverlayPressureScore(recommended, metrics);
            var issue = metrics != null ? MiniMapPrimaryIssueLabel(metrics) : "--";
            var accent = recommended == OverlayMode.Normal
                ? new Color32(96, 190, 122, 220)
                : OverlayModeAccentColor(recommended);

            if (rightCommandStackHintText != null)
            {
                var urgency = MiniMapUrgencyTag(metrics, lastMiniMapSevereSamples, lastMiniMapWarningSamples);
                rightCommandStackHintText.text = "\u7126" + OverlayLabel(recommended) + " " + pressure + " " + CompactCardText(issue, 3)
                    + "\n" + urgency + ">" + CompactCardText(RightCommandActionHint(metrics, recommended), 5);
                rightCommandStackHintText.color = pressure >= 70
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 238);
            }

            if (rightCommandStackAccentImage != null)
            {
                rightCommandStackAccentImage.color = new Color32(accent.r, accent.g, accent.b, pressure >= 70 ? (byte)238 : (byte)180);
            }

            if (rightCommandStackImage != null)
            {
                rightCommandStackImage.color = pressure >= 70
                    ? new Color32(62, 48, 36, 236)
                    : new Color32(22, 57, 39, 232);
            }

            for (var i = 0; i < rightCommandButtons.Count; i += 1)
            {
                RefreshRightCommandButton(rightCommandButtons[i], recommended, metrics);
            }
        }

        private void RefreshRightCommandButton(RightCommandBinding binding, OverlayMode recommended, CityMetrics metrics)
        {
            if (binding == null)
            {
                return;
            }

            var active = IsRightCommandActive(binding.Kind);
            var pressure = RightCommandPressure(binding.Kind, recommended, metrics);
            var recommendedCommand = binding.Kind == 3 && pressure >= 42;
            var roadCommandPressure = binding.Kind == 2 && pressure >= 48;
            var cancelCommandPressure = binding.Kind == 1 && active;
            var highlight = recommendedCommand || roadCommandPressure || cancelCommandPressure;
            var accent = binding.Kind == 3 && recommended != OverlayMode.Normal
                ? OverlayModeAccentColor(recommended)
                : binding.Kind == 2 && pressure >= 48
                    ? OverlayModeAccentColor(OverlayMode.Traffic)
                : binding.Accent;

            if (binding.Card != null)
            {
                binding.Card.color = active
                    ? new Color32(43, 166, 184, 250)
                    : highlight
                        ? new Color32(255, 207, 86, 86)
                        : new Color32(245, 255, 238, 34);
            }

            if (binding.Swatch != null)
            {
                binding.Swatch.color = active
                    ? new Color32(245, 255, 238, 250)
                    : new Color32(accent.r, accent.g, accent.b, highlight ? (byte)248 : (byte)226);
            }

            if (binding.Glyph != null)
            {
                binding.Glyph.text = RightCommandGlyph(binding.Kind, recommended, pressure, active);
                binding.Glyph.color = active
                    ? new Color32(28, 94, 82, 255)
                    : (highlight ? new Color32(83, 68, 30, 255) : new Color32(43, 64, 70, 255));
            }

            if (binding.Label != null)
            {
                binding.Label.text = binding.Kind == 3 && recommended != OverlayMode.Normal
                    ? "\u63a8" + CompactCardText(OverlayLabel(recommended), 2)
                    : RightCommandBaseLabel(binding.Kind);
                binding.Label.color = active
                    ? Color.white
                    : (highlight ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 245));
            }

            if (binding.Outline != null)
            {
                binding.Outline.enabled = active || highlight;
                binding.Outline.effectColor = active
                    ? new Color32(245, 255, 238, 238)
                    : new Color32(accent.r, accent.g, accent.b, 220);
                binding.Outline.effectDistance = active ? new Vector2(2f, -2f) : new Vector2(1.45f, -1.45f);
            }

            if (binding.StateRail != null)
            {
                binding.StateRail.color = active
                    ? new Color32(245, 255, 238, 232)
                    : (highlight ? new Color32(accent.r, accent.g, accent.b, 218) : new Color32(0, 0, 0, 0));
            }

            if (binding.PressureFill != null)
            {
                binding.PressureFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(7, pressure) / 100f), 1f);
                binding.PressureFill.color = active
                    ? new Color32(245, 255, 238, 224)
                    : new Color32(accent.r, accent.g, accent.b, highlight ? (byte)214 : (byte)116);
            }

            if (binding.StateText != null)
            {
                binding.StateText.text = RightCommandStateLabel(binding.Kind, recommended, pressure, active, highlight);
                binding.StateText.color = active
                    ? new Color32(28, 94, 82, 255)
                    : (highlight ? new Color32(83, 68, 30, 255) : new Color32(245, 255, 238, 132));
            }
        }

        private int RightCommandPressure(int kind, OverlayMode recommended, CityMetrics metrics)
        {
            // REFERENCE_IMAGE_RIGHT_COMMAND_PRESSURE gives each vertical action button its own readable state.
            if (kind == 0)
            {
                return cameraController != null && cameraController.IsCameraSettling ? 70 : 24;
            }

            if (kind == 1)
            {
                return interaction != null && interaction.ToolMode != CityToolMode.Inspect ? 74 : 12;
            }

            if (kind == 2)
            {
                return metrics == null
                    ? 18
                    : Mathf.Clamp(Mathf.Max(metrics.RoadBottleneckPressure, Mathf.Max(metrics.IntersectionDelay, 100 - metrics.RoadConnectivity)), 8, 100);
            }

            var overlayPressure = OverlayPressureScore(recommended, metrics);
            var toolPressure = StrongestToolRecommendationScore(metrics);
            return Mathf.Clamp(Mathf.Max(overlayPressure, toolPressure), 8, 100);
        }

        private static string RightCommandActionHint(CityMetrics metrics, OverlayMode recommended)
        {
            if (metrics == null)
            {
                return "\u770b\u57ce\u5e02";
            }

            if (recommended == OverlayMode.Traffic || metrics.RoadBottleneckPressure >= 60) return "\u5347\u7ea7\u8def";
            if (recommended == OverlayMode.Services || metrics.ServiceGapPressure >= 58) return "\u8865\u670d\u52a1";
            if (recommended == OverlayMode.Zoning || metrics.DemandUrgency >= 50) return "\u8865\u5206\u533a";
            if (recommended == OverlayMode.Utilities || metrics.UtilityReliability < 86) return "\u8865\u6c34\u7535";
            if (recommended == OverlayMode.Parking || metrics.ParkingPressure >= 55) return "\u505c\u8f66";
            if (metrics.BuildingUpgradeReadyCount > 0) return "\u5347\u7ea7";
            return "\u5b8c\u6210\u56de\u5408";
        }

        private static string RightCommandGlyph(int kind, OverlayMode recommended, int pressure, bool active)
        {
            if (kind == 0) return active ? "\u25ce" : "\u25c7";
            if (kind == 1) return active ? "\u2715" : "\u21a9";
            if (kind == 2) return pressure >= 58 ? "!" : "\u8def";
            return recommended != OverlayMode.Normal ? OverlayModeGlyph(recommended) : "\u25c6";
        }

        private static string RightCommandStateLabel(int kind, OverlayMode recommended, int pressure, bool active, bool highlight)
        {
            if (kind == 0) return active ? "\u52a8" : "\u955c";
            if (kind == 1) return active ? "\u9000" : "\u7a7a";
            if (kind == 2) return recommended == OverlayMode.Traffic && pressure >= 58 ? "\u5835" : "\u8def";
            if (pressure >= 70) return "\u6025";
            if (highlight) return "\u63a8";
            return "\u7a33";
        }

        private bool IsRightCommandActive(int kind)
        {
            if (kind == 0)
            {
                return cameraController != null && cameraController.IsCameraSettling;
            }

            if (interaction == null)
            {
                return false;
            }

            if (kind == 1)
            {
                return interaction.ToolMode != CityToolMode.Inspect;
            }

            if (kind == 2)
            {
                return interaction.ToolMode == CityToolMode.BuildRoad || interaction.ToolMode == CityToolMode.UpgradeRoad;
            }

            return false;
        }

        private static string RightCommandBaseLabel(int kind)
        {
            if (kind == 0) return "\u89c6\u89d2";
            if (kind == 1) return "\u64a4\u9500";
            if (kind == 2) return "\u9053\u8def";
            return "\u63a8\u8350";
        }

        private void BuildUnlockRegionCallout(Transform root)
        {
            // REFERENCE_IMAGE_UNLOCK_REGION_CALLOUT mirrors the dashed locked-district panel on the right side of the map.
            var callout = CreatePanel(root, "Unlock Region Callout", AnchorTopRight(), new Vector2(-674f, -414f), new Vector2(-398f, -318f));
            unlockRegionCalloutImage = callout.GetComponent<Image>();
            unlockRegionCalloutImage.color = new Color32(231, 248, 136, 72);
            AddSoftCardShadow(callout, 22);
            var outline = callout.AddComponent<Outline>();
            outline.effectColor = new Color32(245, 255, 238, 118);
            outline.effectDistance = new Vector2(1.35f, -1.35f);
            var button = callout.AddComponent<Button>();
            button.onClick.AddListener(OnUnlockRegionCalloutClicked);

            BuildUnlockRegionDashFrame(callout.transform);

            var lockBadge = CreatePanel(callout.transform, "Unlock Region Lock Badge", new Vector4(0f, 0.5f, 0f, 0.5f), new Vector2(14f, -18f), new Vector2(50f, 18f));
            var lockImage = lockBadge.GetComponent<Image>();
            lockImage.color = new Color32(245, 255, 238, 206);
            lockImage.raycastTarget = false;
            var lockText = CreateText(lockBadge.transform, "Lock Glyph", "\u9501", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            lockText.color = new Color32(52, 100, 54, 255);
            lockText.raycastTarget = false;
            Stretch(lockText.rectTransform);

            unlockRegionTitleText = CreateText(callout.transform, "Unlock Region Title", "\u672a\u89e3\u9501\u533a\u57df", 14, FontStyle.Bold, TextAnchor.UpperLeft);
            unlockRegionTitleText.color = new Color32(245, 255, 238, 255);
            unlockRegionTitleText.raycastTarget = false;
            Stretch(unlockRegionTitleText.rectTransform);
            unlockRegionTitleText.rectTransform.offsetMin = new Vector2(60f, 50f);
            unlockRegionTitleText.rectTransform.offsetMax = new Vector2(-12f, -8f);

            unlockRegionDetailText = CreateText(callout.transform, "Unlock Region Detail", "\u8fde\u63a5\u9053\u8def\u5e76\u5b8c\u6210\u76ee\u6807", 10, FontStyle.Bold, TextAnchor.UpperLeft);
            unlockRegionDetailText.color = new Color32(245, 255, 238, 232);
            unlockRegionDetailText.raycastTarget = false;
            unlockRegionDetailText.resizeTextForBestFit = true;
            unlockRegionDetailText.resizeTextMinSize = 8;
            unlockRegionDetailText.resizeTextMaxSize = 10;
            Stretch(unlockRegionDetailText.rectTransform);
            unlockRegionDetailText.rectTransform.offsetMin = new Vector2(60f, 27f);
            unlockRegionDetailText.rectTransform.offsetMax = new Vector2(-12f, -35f);

            var progressTrack = CreatePanel(callout.transform, "Unlock Region Progress Track", new Vector4(0f, 0f, 1f, 0f), new Vector2(60f, 16f), new Vector2(-84f, 24f));
            var progressTrackImage = progressTrack.GetComponent<Image>();
            progressTrackImage.color = new Color32(245, 255, 238, 70);
            progressTrackImage.raycastTarget = false;
            progressTrack.AddComponent<LayoutElement>().ignoreLayout = true;
            var progressFill = CreatePanel(progressTrack.transform, "Unlock Region Progress Fill", AnchorStretch(), Vector2.zero, Vector2.zero);
            unlockRegionProgressFill = progressFill.GetComponent<Image>();
            unlockRegionProgressFill.color = new Color32(96, 214, 118, 224);
            unlockRegionProgressFill.raycastTarget = false;
            progressFill.AddComponent<LayoutElement>().ignoreLayout = true;

            var action = CreatePanel(callout.transform, "Unlock Region Action", new Vector4(1f, 0f, 1f, 0f), new Vector2(-76f, 10f), new Vector2(-12f, 30f));
            unlockRegionAccentImage = action.GetComponent<Image>();
            unlockRegionAccentImage.color = new Color32(255, 207, 86, 238);
            unlockRegionAccentImage.raycastTarget = false;
            unlockRegionActionText = CreateText(action.transform, "Action Text", "\u53bb\u8fde\u63a5", 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            unlockRegionActionText.color = new Color32(83, 68, 30, 255);
            unlockRegionActionText.raycastTarget = false;
            Stretch(unlockRegionActionText.rectTransform);

            AddHudFacet(callout.transform, "Unlock Region Grass Facet", new Vector4(0.56f, 0.58f, 0.94f, 0.86f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 42), -8f);
            AddHudFacet(callout.transform, "Unlock Region Planning Facet", new Vector4(0.1f, 0.12f, 0.34f, 0.28f), Vector2.zero, Vector2.zero, new Color32(255, 207, 86, 50), 0f);
        }

        private void BuildUnlockRegionDashFrame(Transform parent)
        {
            for (var i = 0; i < 6; i += 1)
            {
                AddUnlockRegionDash(parent, "Unlock Dash Top " + i, new Vector4(0f, 1f, 0f, 1f), new Vector2(12f + i * 42f, -9f), new Vector2(34f + i * 42f, -6f));
                AddUnlockRegionDash(parent, "Unlock Dash Bottom " + i, new Vector4(0f, 0f, 0f, 0f), new Vector2(12f + i * 42f, 6f), new Vector2(34f + i * 42f, 9f));
            }

            for (var i = 0; i < 2; i += 1)
            {
                AddUnlockRegionDash(parent, "Unlock Dash Left " + i, new Vector4(0f, 0f, 0f, 0f), new Vector2(7f, 24f + i * 34f), new Vector2(10f, 46f + i * 34f));
                AddUnlockRegionDash(parent, "Unlock Dash Right " + i, new Vector4(1f, 0f, 1f, 0f), new Vector2(-10f, 24f + i * 34f), new Vector2(-7f, 46f + i * 34f));
            }
        }

        private void AddUnlockRegionDash(Transform parent, string name, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax)
        {
            var dash = CreatePanel(parent, name, anchors, offsetMin, offsetMax);
            var image = dash.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 168);
            image.raycastTarget = false;
            dash.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        private void RefreshUnlockRegionCallout(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            if (unlockRegionCalloutImage == null && unlockRegionTitleText == null && unlockRegionProgressFill == null)
            {
                return;
            }

            var required = snapshot != null ? Mathf.Max(1, snapshot.ObjectiveRequired) : 1;
            var progress = snapshot != null ? Mathf.Clamp(snapshot.ObjectiveProgress, 0, required) : 0;
            var amount = Mathf.Clamp01(progress / (float)required);
            var unlocked = snapshot != null && snapshot.ObjectiveDone;
            if (controller != null && controller.Grid != null)
            {
                unlocked = unlocked || controller.Grid.ExpansionUnlocked;
            }

            var pressure = metrics != null ? Mathf.Max(metrics.RoadBottleneckPressure, Mathf.Max(metrics.ServiceGapPressure, metrics.ForecastRisk)) : 0;
            var accent = unlocked
                ? new Color32(96, 214, 118, 255)
                : pressure >= 70
                    ? new Color32(255, 207, 86, 255)
                    : new Color32(65, 184, 220, 255);

            if (unlockRegionCalloutImage != null)
            {
                unlockRegionCalloutImage.color = unlocked
                    ? new Color32(196, 250, 164, 96)
                    : new Color32(231, 248, 136, pressure >= 70 ? (byte)112 : (byte)78);
            }

            if (unlockRegionTitleText != null)
            {
                unlockRegionTitleText.text = unlocked ? "\u65b0\u533a\u5df2\u5f00\u653e" : "\u672a\u89e3\u9501\u533a\u57df";
                unlockRegionTitleText.color = unlocked
                    ? new Color32(245, 255, 238, 255)
                    : new Color32(245, 255, 238, 252);
            }

            if (unlockRegionDetailText != null)
            {
                var objective = snapshot != null && !string.IsNullOrEmpty(snapshot.ObjectiveTitle)
                    ? CompactCardText(snapshot.ObjectiveTitle, 12)
                    : "\u8fde\u63a5\u9053\u8def";
                var status = snapshot != null && !string.IsNullOrEmpty(snapshot.ExpansionStatusText)
                    ? CompactCardText(snapshot.ExpansionStatusText, 12)
                    : objective;
                unlockRegionDetailText.text = unlocked
                    ? "\u53ef\u89c4\u5212\u65b0\u5730\u5757  " + progress + "/" + required
                    : status + "  " + progress + "/" + required;
            }

            if (unlockRegionProgressFill != null)
            {
                unlockRegionProgressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(0.08f, amount)), 1f);
                unlockRegionProgressFill.color = new Color32(accent.r, accent.g, accent.b, unlocked ? (byte)238 : (byte)212);
            }

            if (unlockRegionAccentImage != null)
            {
                unlockRegionAccentImage.color = new Color32(accent.r, accent.g, accent.b, 238);
            }

            if (unlockRegionActionText != null)
            {
                unlockRegionActionText.text = unlocked ? "\u53bb\u89c4\u5212" : "\u53bb\u8fde\u63a5";
                unlockRegionActionText.color = unlocked
                    ? new Color32(35, 95, 62, 255)
                    : new Color32(83, 68, 30, 255);
            }
        }

        private void OnUnlockRegionCalloutClicked()
        {
            if (controller == null)
            {
                return;
            }

            var unlocked = controller.Grid != null && controller.Grid.ExpansionUnlocked;
            controller.SetOverlay(unlocked ? OverlayMode.Zoning : OverlayMode.Traffic);
            if (interaction != null)
            {
                if (unlocked)
                {
                    interaction.SelectZoneTool(ZoneType.Residential);
                }
                else
                {
                    interaction.SelectRoadTool();
                }
            }

            controller.PublishHudFeedback(unlocked
                ? "\u65b0\u533a\uff1a\u5df2\u5207\u5230\u4f4f\u5b85\u5206\u533a\uff0c\u5f00\u59cb\u89c4\u5212\u65b0\u5730\u5757"
                : "\u672a\u89e3\u9501\u533a\u57df\uff1a\u5df2\u5207\u5230\u4ea4\u901a\u5c42\uff0c\u5148\u8fde\u63a5\u9053\u8def\u5e76\u63a8\u8fdb\u76ee\u6807", true);
        }

        private void BuildFeaturedBuildShelf(Transform root)
        {
            // REFERENCE_IMAGE_FEATURED_BUILD_SHELF adds the large icon build cards from the target bottom toolbar.
            var shelf = CreatePanel(root, "Featured Build Shelf", AnchorBottom(), new Vector2(164f, 194f), new Vector2(-392f, 278f));
            featuredBuildShelfImage = shelf.GetComponent<Image>();
            featuredBuildShelfImage.color = new Color32(18, 54, 42, 220);
            AddSoftCardShadow(shelf, 38);
            AddPanelTopAccent(shelf, new Color32(255, 207, 86, 170), 3f);
            var outline = shelf.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 94);
            outline.effectDistance = new Vector2(1.25f, -1.25f);

            featuredBuildShelfTitleText = CreateText(shelf.transform, "Featured Build Shelf Title", "\u63a8\u8350\u5efa\u9020", 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            featuredBuildShelfTitleText.color = new Color32(245, 255, 238, 242);
            featuredBuildShelfTitleText.raycastTarget = false;
            Stretch(featuredBuildShelfTitleText.rectTransform);
            featuredBuildShelfTitleText.rectTransform.offsetMin = new Vector2(10f, 59f);
            featuredBuildShelfTitleText.rectTransform.offsetMax = new Vector2(-520f, -5f);

            var host = CreatePanel(shelf.transform, "Featured Build Cards", new Vector4(0f, 0f, 1f, 1f), new Vector2(8f, 7f), new Vector2(-8f, -22f));
            var hostImage = host.GetComponent<Image>();
            hostImage.color = new Color32(0, 0, 0, 0);
            hostImage.raycastTarget = false;
            var layout = host.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddFeaturedBuildCard(host.transform, "\u9053\u8def", CityToolMode.BuildRoad, ZoneType.None, string.Empty, "\u8def");
            AddFeaturedBuildCard(host.transform, "\u4f4f\u5b85", CityToolMode.BuildBuilding, ZoneType.None, "residential_pod", "\u4f4f");
            AddFeaturedBuildCard(host.transform, "\u516c\u5bd3", CityToolMode.BuildBuilding, ZoneType.None, "apartment_block", "\u697c");
            AddFeaturedBuildCard(host.transform, "\u5546\u5e97", CityToolMode.BuildBuilding, ZoneType.None, "market_corner", "\u5546");
            AddFeaturedBuildCard(host.transform, "\u529e\u516c", CityToolMode.BuildBuilding, ZoneType.None, "office_studio", "\u529e");
            AddFeaturedBuildCard(host.transform, "\u516c\u56ed", CityToolMode.BuildBuilding, ZoneType.None, "pocket_park", "\u6811");
            AddFeaturedBuildCard(host.transform, "\u7535\u7ad9", CityToolMode.BuildBuilding, ZoneType.None, "micro_power", "\u7535");
            AddFeaturedBuildCard(host.transform, "\u6c34\u5854", CityToolMode.BuildBuilding, ZoneType.None, "water_tower", "\u6c34");
            AddHudFacet(shelf.transform, "Featured Build Shelf Shine", new Vector4(0.68f, 0.64f, 0.98f, 0.88f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 34), -8f);
        }

        private void AddFeaturedBuildCard(Transform parent, string title, CityToolMode mode, ZoneType zone, string buildingId, string glyphText)
        {
            var card = CreatePanel(parent, "Featured Build " + title, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = card.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 34);
            var outline = card.AddComponent<Outline>();
            outline.enabled = false;
            outline.effectColor = new Color32(255, 207, 86, 0);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            var layout = card.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            var button = card.AddComponent<Button>();
            button.onClick.AddListener(() => SelectFeaturedBuildCard(mode, zone, buildingId));

            var icon = CreatePanel(card.transform, "Featured Icon", new Vector4(0.5f, 1f, 0.5f, 1f), new Vector2(-22f, -36f), new Vector2(22f, -7f));
            var iconImage = icon.GetComponent<Image>();
            iconImage.color = ToolAccentColor(mode, zone, buildingId);
            iconImage.raycastTarget = false;
            var iconOutline = icon.AddComponent<Outline>();
            iconOutline.effectColor = new Color32(245, 255, 238, 106);
            iconOutline.effectDistance = new Vector2(1f, -1f);
            AddFeaturedBuildMiniature(icon.transform, mode, zone, buildingId);

            var glyph = CreateText(icon.transform, "Glyph", glyphText, 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(245, 255, 238, 255);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);

            var titleText = CreateText(card.transform, "Title", title, 10, FontStyle.Bold, TextAnchor.UpperCenter);
            titleText.color = new Color32(245, 255, 238, 252);
            titleText.raycastTarget = false;
            titleText.rectTransform.anchorMin = new Vector2(0f, 0.25f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 0.45f);
            titleText.rectTransform.offsetMin = new Vector2(3f, 0f);
            titleText.rectTransform.offsetMax = new Vector2(-3f, 0f);

            var costText = CreateText(card.transform, "Cost", "--", 9, FontStyle.Bold, TextAnchor.LowerCenter);
            costText.color = new Color32(255, 230, 132, 245);
            costText.raycastTarget = false;
            costText.rectTransform.anchorMin = new Vector2(0f, 0f);
            costText.rectTransform.anchorMax = new Vector2(1f, 0.22f);
            costText.rectTransform.offsetMin = new Vector2(3f, 1f);
            costText.rectTransform.offsetMax = new Vector2(-3f, 0f);

            var detailText = CreateText(card.transform, "Detail", "\u5efa\u9020", 8, FontStyle.Bold, TextAnchor.LowerCenter);
            detailText.color = new Color32(206, 238, 216, 228);
            detailText.raycastTarget = false;
            detailText.resizeTextForBestFit = true;
            detailText.resizeTextMinSize = 7;
            detailText.resizeTextMaxSize = 8;
            detailText.rectTransform.anchorMin = new Vector2(0f, 0.46f);
            detailText.rectTransform.anchorMax = new Vector2(1f, 0.62f);
            detailText.rectTransform.offsetMin = new Vector2(3f, 0f);
            detailText.rectTransform.offsetMax = new Vector2(-3f, 0f);

            var fill = CreateToolButtonAccent(card.transform, "Featured Recommendation Fill", AnchorBottom(), new Vector2(3f, 2f), new Vector2(-3f, 6f), new Color32(96, 214, 118, 170));
            fill.raycastTarget = false;

            var stateBadge = CreateToolButtonAccent(card.transform, "Featured State Badge", new Vector4(1f, 1f, 1f, 1f), new Vector2(-19f, -15f), new Vector2(-4f, -4f), new Color32(255, 207, 86, 0));
            stateBadge.raycastTarget = false;
            var stateText = CreateText(stateBadge.transform, "State", string.Empty, 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            stateText.color = new Color32(83, 68, 30, 0);
            stateText.raycastTarget = false;
            Stretch(stateText.rectTransform);

            AddHudFacet(card.transform, "Featured Build Card Facet", new Vector4(0.52f, 0.62f, 0.92f, 0.86f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 30), -8f);

            featuredBuildCards.Add(new FeaturedBuildCardBinding
            {
                Button = button,
                Card = image,
                Fill = fill,
                StateBadge = stateBadge,
                IconPanel = iconImage,
                Glyph = glyph,
                Title = titleText,
                Cost = costText,
                Detail = detailText,
                StateText = stateText,
                Outline = outline,
                ToolMode = mode,
                Zone = zone,
                BuildingId = buildingId
            });
        }

        private void AddFeaturedBuildMiniature(Transform parent, CityToolMode mode, ZoneType zone, string buildingId)
        {
            var accent = ToolAccentColor(mode, zone, buildingId);
            if (mode == CityToolMode.BuildRoad)
            {
                AddFeaturedMiniBlock(parent, "Road Body", new Color32(68, 82, 86, 238), new Vector4(0.13f, 0.36f, 0.87f, 0.62f));
                AddFeaturedMiniBlock(parent, "Road Stripe", new Color32(255, 242, 174, 218), new Vector4(0.2f, 0.48f, 0.8f, 0.54f));
                return;
            }

            if (buildingId == "pocket_park")
            {
                AddFeaturedMiniBlock(parent, "Park Grass", new Color32(116, 214, 99, 238), new Vector4(0.16f, 0.18f, 0.84f, 0.46f));
                AddFeaturedMiniBlock(parent, "Park Tree A", new Color32(44, 146, 66, 238), new Vector4(0.22f, 0.44f, 0.42f, 0.78f));
                AddFeaturedMiniBlock(parent, "Park Tree B", new Color32(70, 178, 82, 238), new Vector4(0.58f, 0.42f, 0.8f, 0.76f));
                return;
            }

            if (IsUtilityTool(buildingId))
            {
                AddFeaturedMiniBlock(parent, "Utility Base", new Color32(238, 232, 198, 238), new Vector4(0.24f, 0.16f, 0.76f, 0.36f));
                AddFeaturedMiniBlock(parent, "Utility Tower", accent, new Vector4(0.4f, 0.32f, 0.6f, 0.82f));
                AddFeaturedMiniBlock(parent, "Utility Cap", new Color32(86, 197, 224, 238), new Vector4(0.3f, 0.72f, 0.7f, 0.9f));
                return;
            }

            AddFeaturedMiniBlock(parent, "Building Base", new Color32(250, 237, 188, 238), new Vector4(0.22f, 0.14f, 0.78f, 0.34f));
            AddFeaturedMiniBlock(parent, "Building Body", accent, new Vector4(0.28f, 0.3f, 0.72f, 0.84f));
            AddFeaturedMiniBlock(parent, "Building Roof", new Color32(255, 203, 88, 238), new Vector4(0.22f, 0.74f, 0.78f, 0.92f));
            AddFeaturedMiniBlock(parent, "Building Window", new Color32(245, 255, 238, 186), new Vector4(0.35f, 0.47f, 0.46f, 0.58f));
            AddFeaturedMiniBlock(parent, "Building Window", new Color32(245, 255, 238, 186), new Vector4(0.54f, 0.47f, 0.65f, 0.58f));
        }

        private void AddFeaturedMiniBlock(Transform parent, string name, Color32 color, Vector4 anchors)
        {
            var block = CreatePanel(parent, name, anchors, Vector2.zero, Vector2.zero);
            var image = block.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            block.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        private void RefreshFeaturedBuildShelf(CityMetrics metrics)
        {
            var quoteVisible = controller != null && controller.CurrentPreview != null;
            SetFeaturedBuildShelfVisible(!quoteVisible);
            if (quoteVisible)
            {
                return;
            }

            if (featuredBuildCards.Count == 0)
            {
                return;
            }

            var strongest = StrongestToolRecommendationScore(metrics);
            var recommendedCount = 0;
            for (var i = 0; i < featuredBuildCards.Count; i += 1)
            {
                var card = featuredBuildCards[i];
                var binding = FindToolBinding(card.ToolMode, card.Zone, card.BuildingId);
                var active = binding != null && IsToolActive(binding);
                var score = binding != null ? ToolRecommendationScoreWithSelectedTile(binding, metrics) : 0;
                var recommended = !active && IsDemandRecommendedTool(score, strongest);
                if (recommended)
                {
                    recommendedCount += 1;
                }

                var accent = ToolAccentColor(card.ToolMode, card.Zone, card.BuildingId);
                if (card.Card != null)
                {
                    card.Card.color = active
                        ? new Color32(43, 166, 184, 246)
                        : recommended
                            ? new Color32(255, 232, 150, 118)
                            : new Color32(245, 255, 238, 34);
                }

                if (card.IconPanel != null)
                {
                    card.IconPanel.color = active
                        ? new Color32(245, 255, 238, 248)
                        : (recommended ? new Color32(255, 207, 86, 246) : accent);
                }

                if (card.Glyph != null)
                {
                    card.Glyph.color = active || recommended
                        ? new Color32(43, 64, 70, 255)
                        : new Color32(245, 255, 238, 255);
                }

                if (card.Title != null)
                {
                    card.Title.text = FeaturedBuildTitle(card, binding);
                    card.Title.color = active
                        ? new Color32(255, 255, 255, 255)
                        : (recommended ? new Color32(83, 68, 30, 255) : new Color32(245, 255, 238, 252));
                }

                if (card.Cost != null)
                {
                    card.Cost.text = FeaturedBuildCostText(card, binding);
                    card.Cost.color = active
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 230, 132, 245);
                }

                if (card.Detail != null)
                {
                    card.Detail.text = FeaturedBuildDetailStatusText(card, binding, metrics, active, recommended, score);
                    card.Detail.color = active
                        ? Color.white
                        : (recommended ? new Color32(83, 68, 30, 255) : new Color32(206, 238, 216, 228));
                }

                if (card.Fill != null)
                {
                    card.Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, score) / 100f), 1f);
                    card.Fill.color = active
                        ? new Color32(245, 255, 238, 182)
                        : recommended
                            ? new Color32(255, 207, 86, 222)
                            : new Color32(accent.r, accent.g, accent.b, 142);
                }

                if (card.StateBadge != null)
                {
                    card.StateBadge.color = active
                        ? new Color32(245, 255, 238, 238)
                        : recommended
                            ? new Color32(255, 207, 86, 238)
                            : score >= 55
                                ? new Color32(accent.r, accent.g, accent.b, 176)
                                : new Color32(255, 207, 86, 0);
                }

                if (card.StateText != null)
                {
                    card.StateText.text = FeaturedBuildStateBadgeText(active, recommended, score);
                    card.StateText.color = active
                        ? new Color32(28, 94, 82, 255)
                        : recommended
                            ? new Color32(83, 68, 30, 255)
                            : score >= 55
                                ? new Color32(245, 255, 238, 238)
                                : new Color32(83, 68, 30, 0);
                }

                if (card.Outline != null)
                {
                    card.Outline.enabled = active || recommended || score >= 72;
                    card.Outline.effectColor = active
                        ? new Color32(245, 255, 238, 238)
                        : recommended
                            ? new Color32(255, 207, 86, 226)
                            : new Color32(accent.r, accent.g, accent.b, 172);
                    card.Outline.effectDistance = active ? new Vector2(2f, -2f) : new Vector2(1.35f, -1.35f);
                }
            }

            if (featuredBuildShelfTitleText != null)
            {
                var orderTitleCue = FeaturedBuildShelfOrderCue(metrics);
                featuredBuildShelfTitleText.text = recommendedCount > 0
                    ? "\u5efa\u9020\u8ba2\u5355 +" + recommendedCount + orderTitleCue + "  \u4e3b" + CompactCardText(MiniMapPrimaryIssueLabel(metrics), 3)
                    : "\u5efa\u9020\u6e05\u5355  \u57ce\u5e02\u8bc4\u5206 " + (metrics != null ? metrics.CityScore.ToString() : "--");
                featuredBuildShelfTitleText.color = recommendedCount > 0 || strongest >= 72
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 242);
            }

            if (featuredBuildShelfImage != null)
            {
                featuredBuildShelfImage.color = recommendedCount > 0
                    ? new Color32(44, 70, 44, 224)
                    : new Color32(18, 54, 42, 220);
            }
        }

        private static string FeaturedBuildShelfOrderCue(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return "  \u53ef\u9886";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                var remaining = Mathf.Max(0, metrics.ActiveObjective.Required - metrics.ActiveObjective.Progress);
                return remaining <= 1 ? "  \u6536\u5c3e" : "  \u8fd8\u5dee" + remaining;
            }

            return metrics.BuildingUpgradeReadyCount > 0 ? "  \u5347+" + metrics.BuildingUpgradeReadyCount : string.Empty;
        }

        private ToolButtonBinding FindToolBinding(CityToolMode mode, ZoneType zone, string buildingId)
        {
            for (var i = 0; i < toolButtons.Count; i += 1)
            {
                var binding = toolButtons[i];
                if (binding == null || binding.ToolMode != mode)
                {
                    continue;
                }

                if (mode == CityToolMode.ZonePaint && binding.Zone != zone)
                {
                    continue;
                }

                if (mode == CityToolMode.BuildBuilding && binding.BuildingId != buildingId)
                {
                    continue;
                }

                return binding;
            }

            return null;
        }

        private string FeaturedBuildTitle(FeaturedBuildCardBinding card, ToolButtonBinding binding)
        {
            if (binding != null)
            {
                return CompactCardText(ToolBindingLabel(binding), 4);
            }

            if (card.ToolMode == CityToolMode.BuildRoad) return "\u9053\u8def";
            if (card.ToolMode == CityToolMode.ZonePaint) return CompactCardText(ZoneLabel(card.Zone), 4);
            if (card.ToolMode == CityToolMode.BuildBuilding) return CompactCardText(BuildingLabel(card.BuildingId), 4);
            return "\u5de5\u5177";
        }

        private string FeaturedBuildCostText(FeaturedBuildCardBinding card, ToolButtonBinding binding)
        {
            if (binding != null)
            {
                return ToolButtonMetaText(binding.ToolMode, binding.Zone, binding.BuildingId);
            }

            return ToolButtonMetaText(card.ToolMode, card.Zone, card.BuildingId);
        }

        private static string FeaturedBuildDetailText(FeaturedBuildCardBinding card, ToolButtonBinding binding, CityMetrics metrics)
        {
            if (binding != null)
            {
                return CompactCardText(ToolPlacementHint(binding, metrics), 5);
            }

            if (card.ToolMode == CityToolMode.BuildRoad) return "\u8fde\u8def";
            if (card.ToolMode == CityToolMode.ZonePaint) return "\u5237\u5730";
            if (IsUtilityTool(card.BuildingId)) return "\u4fdd\u4f9b";
            if (IsServiceTool(card.BuildingId)) return "\u8865\u670d";
            return "\u843d\u70b9";
        }

        private static string FeaturedBuildDetailStatusText(FeaturedBuildCardBinding card, ToolButtonBinding binding, CityMetrics metrics, bool active, bool recommended, int score)
        {
            // REFERENCE_IMAGE_BUILD_CARD_REASON gives the featured build shelf price/status/reason hierarchy.
            if (active)
            {
                return "\u5df2\u9009\u4e2d";
            }

            if (recommended && binding != null)
            {
                return FeaturedBuildOrderVerb(metrics) + " " + CompactCardText(ToolRecommendationDriverLabel(binding, metrics), 3) + " " + score;
            }

            if (score >= 55)
            {
                return "\u5019\u9009 " + score;
            }

            return FeaturedBuildDetailText(card, binding, metrics);
        }

        private static string FeaturedBuildOrderVerb(CityMetrics metrics)
        {
            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return "\u53ef\u9886";
            }

            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                return "\u63a5\u5355";
            }

            return "\u63a8\u8350";
        }

        private static string FeaturedBuildStateBadgeText(bool active, bool recommended, int score)
        {
            if (active)
            {
                return "\u2713";
            }

            if (recommended)
            {
                return "\u8350";
            }

            return score >= 55 ? score.ToString() : string.Empty;
        }

        private void SelectFeaturedBuildCard(CityToolMode mode, ZoneType zone, string buildingId)
        {
            if (interaction == null)
            {
                return;
            }

            if (controller != null)
            {
                controller.SetOverlay(FeaturedBuildOverlay(mode, zone, buildingId));
            }

            if (mode == CityToolMode.BuildRoad)
            {
                interaction.SelectRoadTool();
            }
            else if (mode == CityToolMode.UpgradeRoad)
            {
                interaction.SelectRoadUpgradeTool();
            }
            else if (mode == CityToolMode.ZonePaint)
            {
                interaction.SelectZoneTool(zone);
            }
            else if (mode == CityToolMode.BuildBuilding)
            {
                interaction.SelectBuildingTool(buildingId);
            }
            else if (mode == CityToolMode.Demolish)
            {
                interaction.SelectDemolishTool();
            }

            if (controller != null)
            {
                controller.PublishHudFeedback("\u5feb\u6377\u5efa\u9020\uff1a\u5df2\u9009 " + FeaturedBuildFeedbackLabel(mode, zone, buildingId), true);
            }
        }

        private static OverlayMode FeaturedBuildOverlay(CityToolMode mode, ZoneType zone, string buildingId)
        {
            if (mode == CityToolMode.BuildRoad || mode == CityToolMode.UpgradeRoad) return OverlayMode.Traffic;
            if (mode == CityToolMode.ZonePaint) return OverlayMode.Zoning;
            if (IsUtilityTool(buildingId)) return OverlayMode.Utilities;
            if (IsServiceTool(buildingId)) return OverlayMode.Services;
            if (buildingId == "pocket_park") return OverlayMode.LandValue;
            return OverlayMode.Normal;
        }

        private static string FeaturedBuildFeedbackLabel(CityToolMode mode, ZoneType zone, string buildingId)
        {
            if (mode == CityToolMode.BuildRoad) return "\u9053\u8def";
            if (mode == CityToolMode.UpgradeRoad) return "\u5347\u7ea7\u9053\u8def";
            if (mode == CityToolMode.ZonePaint) return ZoneLabel(zone);
            if (mode == CityToolMode.BuildBuilding) return BuildingLabel(buildingId);
            if (mode == CityToolMode.Demolish) return "\u62c6\u9664";
            return "\u5de5\u5177";
        }

        private void BuildSelectedTileDetailCard(Transform root)
        {
            // CITY_SKYLINES_SELECTED_TILE_CARD turns map clicks into a compact planning approval panel.
            var card = CreatePanel(root, "Selected Tile Detail Card", AnchorBottom(), new Vector2(164f, 224f), new Vector2(-392f, 292f));
            selectedTileDetailCardImage = card.GetComponent<Image>();
            selectedTileDetailCardImage.color = new Color32(18, 54, 42, 218);
            AddSoftCardShadow(card, 34);
            AddPanelTopAccent(card, new Color32(65, 183, 190, 152), 3f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 96);
            outline.effectDistance = new Vector2(1.3f, -1.3f);

            selectedTileDetailAccentImage = CreateToolButtonAccent(card.transform, "Selected Tile Accent Rail", AnchorLeft(), new Vector2(5f, 8f), new Vector2(9f, -8f), new Color32(65, 183, 190, 210));
            selectedTileDetailAccentImage.raycastTarget = false;

            selectedTileDetailTitleText = CreateText(card.transform, "Selected Tile Title", "\u5730\u5757\u8be6\u60c5", 12, FontStyle.Bold, TextAnchor.UpperLeft);
            selectedTileDetailTitleText.color = new Color32(245, 255, 238, 252);
            selectedTileDetailTitleText.raycastTarget = false;
            Stretch(selectedTileDetailTitleText.rectTransform);
            selectedTileDetailTitleText.rectTransform.offsetMin = new Vector2(16f, 43f);
            selectedTileDetailTitleText.rectTransform.offsetMax = new Vector2(-430f, -5f);

            selectedTileDetailSubtitleText = CreateText(card.transform, "Selected Tile Subtitle", "\u70b9\u51fb\u5730\u56fe\u67e5\u770b\u5730\u5757", 10, FontStyle.Bold, TextAnchor.UpperLeft);
            selectedTileDetailSubtitleText.color = new Color32(206, 238, 216, 234);
            selectedTileDetailSubtitleText.raycastTarget = false;
            selectedTileDetailSubtitleText.resizeTextForBestFit = true;
            selectedTileDetailSubtitleText.resizeTextMinSize = 8;
            selectedTileDetailSubtitleText.resizeTextMaxSize = 10;
            Stretch(selectedTileDetailSubtitleText.rectTransform);
            selectedTileDetailSubtitleText.rectTransform.offsetMin = new Vector2(16f, 22f);
            selectedTileDetailSubtitleText.rectTransform.offsetMax = new Vector2(-430f, -26f);

            selectedTileDetailStateBadgeImage = CreateToolButtonAccent(card.transform, "Selected Tile State Badge", AnchorRight(), new Vector2(-164f, -20f), new Vector2(-132f, -6f), new Color32(96, 214, 118, 228));
            selectedTileDetailStateBadgeImage.raycastTarget = false;
            selectedTileDetailStateText = CreateText(selectedTileDetailStateBadgeImage.transform, "State", "\u7a33", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            selectedTileDetailStateText.color = new Color32(43, 64, 70, 255);
            selectedTileDetailStateText.raycastTarget = false;
            Stretch(selectedTileDetailStateText.rectTransform);

            BuildSelectedTileMetricLane(card.transform, 0, "\u4ea4", new Color32(244, 173, 66, 226), out selectedTileTrafficFill, out selectedTileTrafficText);
            BuildSelectedTileMetricLane(card.transform, 1, "\u670d", new Color32(96, 214, 118, 226), out selectedTileServiceFill, out selectedTileServiceText);
            BuildSelectedTileMetricLane(card.transform, 2, "\u5730", new Color32(65, 184, 220, 226), out selectedTileLandFill, out selectedTileLandText);

            var action = CreatePanel(card.transform, "Selected Tile Action", AnchorRight(), new Vector2(-128f, 12f), new Vector2(-12f, -12f));
            selectedTileDetailActionImage = action.GetComponent<Image>();
            selectedTileDetailActionImage.color = new Color32(255, 207, 86, 238);
            var actionButton = action.AddComponent<Button>();
            actionButton.onClick.AddListener(OnSelectedTileDetailActionClicked);
            selectedTileDetailActionText = CreateText(action.transform, "Action Text", "\u67e5\u770b\u5efa\u8bae", 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            selectedTileDetailActionText.color = new Color32(83, 68, 30, 255);
            selectedTileDetailActionText.raycastTarget = false;
            Stretch(selectedTileDetailActionText.rectTransform);

            AddHudFacet(card.transform, "Selected Tile Card Facet", new Vector4(0.56f, 0.6f, 0.83f, 0.87f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 28), -8f);
            AddHudFacet(card.transform, "Selected Tile Approval Glow", new Vector4(0.72f, 0.14f, 0.96f, 0.34f), Vector2.zero, Vector2.zero, new Color32(255, 207, 86, 40), 0f);
        }

        private void BuildSelectedTileMetricLane(Transform parent, int index, string label, Color32 accent, out Image fill, out Text text)
        {
            var minY = 0.13f + index * 0.24f;
            var maxY = minY + 0.16f;
            var lane = CreatePanel(parent, "Selected Tile Metric " + label, new Vector4(0.42f, minY, 0.79f, maxY), Vector2.zero, Vector2.zero);
            var laneImage = lane.GetComponent<Image>();
            laneImage.color = new Color32(245, 255, 238, 30);
            laneImage.raycastTarget = false;
            lane.AddComponent<LayoutElement>().ignoreLayout = true;

            var fillObject = CreatePanel(lane.transform, "Metric Fill", AnchorStretch(), Vector2.zero, Vector2.zero);
            fill = fillObject.GetComponent<Image>();
            fill.color = new Color32(accent.r, accent.g, accent.b, 170);
            fill.raycastTarget = false;
            fillObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var labelChip = CreatePanel(lane.transform, "Metric Label", AnchorLeft(), new Vector2(2f, 2f), new Vector2(23f, -2f));
            var labelImage = labelChip.GetComponent<Image>();
            labelImage.color = accent;
            labelImage.raycastTarget = false;
            var glyph = CreateText(labelChip.transform, "Glyph", label, 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(43, 64, 70, 255);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);

            text = CreateText(lane.transform, "Metric Text", label + "--", 9, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.color = new Color32(245, 255, 238, 242);
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(28f, 0f);
            text.rectTransform.offsetMax = new Vector2(-4f, 0f);
        }

        private void RefreshSelectedTileDetailCard(CityMetrics metrics)
        {
            if (selectedTileDetailCardImage == null)
            {
                return;
            }

            if (interaction == null || controller == null || !interaction.HasSelectedTile)
            {
                RefreshSelectedTileEmptyState(metrics);
                return;
            }

            var pos = interaction.SelectedTile;
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null)
            {
                RefreshSelectedTileEmptyState(metrics);
                return;
            }

            var severity = MiniMapIssueSeverity(tile, metrics);
            var accent = SelectedTileDetailAccent(tile, metrics, severity);
            selectedTileDetailCardImage.color = severity >= 34
                ? new Color32(64, 50, 35, 224)
                : new Color32(18, 54, 42, 218);
            if (selectedTileDetailAccentImage != null)
            {
                selectedTileDetailAccentImage.color = new Color32(accent.r, accent.g, accent.b, 224);
            }

            if (selectedTileDetailStateBadgeImage != null)
            {
                selectedTileDetailStateBadgeImage.color = SelectedTileStateBadgeColor(tile, severity, accent);
            }

            if (selectedTileDetailStateText != null)
            {
                selectedTileDetailStateText.text = SelectedTileOrderBadgeLabel(tile, metrics, severity);
                selectedTileDetailStateText.color = severity >= 34
                    ? new Color32(83, 68, 30, 255)
                    : new Color32(43, 64, 70, 255);
            }

            if (selectedTileDetailTitleText != null)
            {
                selectedTileDetailTitleText.text = "\u5730\u5757\u8ba2\u5355 " + pos.X + "," + pos.Y + "  " + TerrainLabel(tile.Terrain) + "/" + ZoneLabel(tile.Zone);
                selectedTileDetailTitleText.color = severity >= 34
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 252);
            }

            if (selectedTileDetailSubtitleText != null)
            {
                selectedTileDetailSubtitleText.text = CompactCardText(
                    SelectedTileOrderSubtitle(pos, tile, metrics)
                    + " / "
                    + "\u4e3b:" + DominantTileIssueLabel(tile, metrics),
                    38);
            }

            SetSelectedTileMetric(selectedTileTrafficFill, selectedTileTrafficText, "\u4ea4\u901a", tile.Traffic, 100, new Color32(244, 173, 66, 226), tile.Traffic >= 58);
            SetSelectedTileMetric(selectedTileServiceFill, selectedTileServiceText, "\u670d\u52a1", ServiceAccessValue(tile), 100, new Color32(96, 214, 118, 226), TileHasUse(tile) && ServiceAccessValue(tile) < 28);
            SetSelectedTileMetric(selectedTileLandFill, selectedTileLandText, "\u5730\u4ef7", tile.LandValue, 100, new Color32(65, 184, 220, 226), tile.LandValue < 35);

            if (selectedTileDetailActionImage != null)
            {
                selectedTileDetailActionImage.color = new Color32(accent.r, accent.g, accent.b, 238);
            }

            if (selectedTileDetailActionText != null)
            {
                selectedTileDetailActionText.text = SelectedTileOrderActionLabel(tile, metrics);
                selectedTileDetailActionText.color = severity >= 34 || tile.Terrain == TerrainType.Water
                    ? new Color32(245, 255, 238, 255)
                    : new Color32(83, 68, 30, 255);
            }
        }

        private void RefreshSelectedTileEmptyState(CityMetrics metrics)
        {
            var recommended = RecommendedOverlayMode(metrics);
            var pressure = OverlayPressureScore(recommended, metrics);
            var accent = recommended == OverlayMode.Normal ? new Color32(96, 214, 118, 255) : OverlayModeAccentColor(recommended);
            selectedTileDetailCardImage.color = new Color32(18, 54, 42, 204);
            if (selectedTileDetailAccentImage != null)
            {
                selectedTileDetailAccentImage.color = new Color32(accent.r, accent.g, accent.b, 194);
            }

            if (selectedTileDetailStateBadgeImage != null)
            {
                selectedTileDetailStateBadgeImage.color = pressure >= 58
                    ? new Color32(255, 207, 86, 226)
                    : new Color32(accent.r, accent.g, accent.b, 204);
            }

            if (selectedTileDetailStateText != null)
            {
                selectedTileDetailStateText.text = pressure >= 58 ? "\u63a8" : "\u5f85";
                selectedTileDetailStateText.color = new Color32(43, 64, 70, 255);
            }

            if (selectedTileDetailTitleText != null)
            {
                selectedTileDetailTitleText.text = "\u5730\u5757\u8be6\u60c5  \u7b49\u5f85\u70b9\u9009";
                selectedTileDetailTitleText.color = new Color32(245, 255, 238, 238);
            }

            if (selectedTileDetailSubtitleText != null)
            {
                selectedTileDetailSubtitleText.text = "\u5efa\u8bae\u56fe\u5c42 " + OverlayLabel(recommended) + "  \u538b" + pressure + "  \u70b9\u5730\u5757\u67e5\u770b\u5ba1\u6279";
            }

            SetSelectedTileMetric(selectedTileTrafficFill, selectedTileTrafficText, "\u4ea4\u901a", metrics != null ? metrics.RoadBottleneckPressure : 0, 100, new Color32(244, 173, 66, 226), false);
            SetSelectedTileMetric(selectedTileServiceFill, selectedTileServiceText, "\u670d\u52a1", metrics != null ? metrics.ServiceCoverage : 0, 100, new Color32(96, 214, 118, 226), false);
            SetSelectedTileMetric(selectedTileLandFill, selectedTileLandText, "\u5730\u4ef7", metrics != null ? metrics.AverageLandValue : 0, 100, new Color32(65, 184, 220, 226), false);

            if (selectedTileDetailActionImage != null)
            {
                selectedTileDetailActionImage.color = new Color32(accent.r, accent.g, accent.b, 226);
            }

            if (selectedTileDetailActionText != null)
            {
                selectedTileDetailActionText.text = "\u770b" + OverlayLabel(recommended);
                selectedTileDetailActionText.color = new Color32(83, 68, 30, 255);
            }
        }

        private static void SetSelectedTileMetric(Image fill, Text text, string label, int value, int max, Color32 accent, bool warning)
        {
            var clamped = Mathf.Clamp(value, 0, max);
            if (fill != null)
            {
                fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, clamped) / (float)Mathf.Max(1, max)), 1f);
                fill.color = warning
                    ? new Color32(255, 207, 86, 224)
                    : new Color32(accent.r, accent.g, accent.b, 168);
            }

            if (text != null)
            {
                text.text = label + " " + clamped;
                text.color = warning
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 242);
            }
        }

        private static Color32 SelectedTileDetailAccent(TileData tile, CityMetrics metrics, int severity)
        {
            if (tile == null) return new Color32(65, 183, 190, 255);
            if (tile.Terrain == TerrainType.Water) return new Color32(86, 197, 224, 255);
            if (severity >= 34) return new Color32(244, 116, 71, 255);
            if (TileHasUse(tile) && ServiceAccessValue(tile) < 28) return new Color32(255, 207, 86, 255);
            if (tile.Traffic >= 58) return new Color32(244, 173, 66, 255);
            if (tile.LandValue < 35) return new Color32(65, 184, 220, 255);
            return new Color32(96, 214, 118, 255);
        }

        private static Color32 SelectedTileStateBadgeColor(TileData tile, int severity, Color32 accent)
        {
            // REFERENCE_IMAGE_SELECTED_TILE_STAGE_BADGE mirrors the compact stage chip on task cards.
            if (tile == null)
            {
                return new Color32(65, 183, 190, 210);
            }

            if (tile.Terrain == TerrainType.Water)
            {
                return new Color32(86, 197, 224, 228);
            }

            if (severity >= 34)
            {
                return new Color32(255, 188, 66, 238);
            }

            if (severity >= 18)
            {
                return new Color32(255, 207, 86, 228);
            }

            return new Color32(accent.r, accent.g, accent.b, 218);
        }

        private static string SelectedTileStateBadgeLabel(TileData tile, int severity)
        {
            if (tile == null) return "--";
            if (tile.Terrain == TerrainType.Water) return "\u6c34";
            if (severity >= 34) return "\u4e25";
            if (severity >= 18) return "\u6ce8";
            if (!string.IsNullOrEmpty(tile.RoadId)) return "\u8def";
            if (!string.IsNullOrEmpty(tile.BuildingId)) return "\u5efa";
            if (tile.Zone == ZoneType.None) return "\u7a7a";
            return "\u7a33";
        }

        private string SelectedTileOrderBadgeLabel(TileData tile, CityMetrics metrics, int severity)
        {
            if (tile == null) return "--";
            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done && TileHasUse(tile)) return "\u53ef\u9886";
            if (!string.IsNullOrEmpty(tile.BuildingId) && severity < 18 && tile.LandValue >= 58 && ServiceAccessValue(tile) >= 55) return "\u53ef\u5347";
            if (tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId) && string.IsNullOrEmpty(tile.RoadId) && tile.Terrain != TerrainType.Water) return "\u63a5\u5355";
            return SelectedTileStateBadgeLabel(tile, severity);
        }

        private string SelectedTileOrderSubtitle(GridPos pos, TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return "--";
            }

            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done && TileHasUse(tile))
            {
                return "\u53ef\u6536\u53d6 \u91d1+2500 \u4eba+30";
            }

            if (!string.IsNullOrEmpty(tile.BuildingId))
            {
                var ready = tile.LandValue >= 58 && ServiceAccessValue(tile) >= 55 && tile.Traffic < 58;
                return ready ? "\u6210\u719f\u5efa\u7b51 \u53ef\u5347\u7ea7/\u6536\u76ca" : "\u5efa\u7b51\u8ba2\u5355 " + TileOccupancyText(pos, tile);
            }

            if (tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.RoadId) && tile.Terrain != TerrainType.Water)
            {
                return "\u7a7a\u5730\u8ba2\u5355 " + OpenTileZoningHint(metrics);
            }

            return TileOccupancyText(pos, tile);
        }

        private static string SelectedTileActionLabel(TileData tile, CityMetrics metrics)
        {
            if (tile == null) return "\u67e5\u770b";
            if (tile.Terrain == TerrainType.Water) return "\u4fdd\u7559";
            var dominant = DominantTileIssueId(tile, metrics);
            if (dominant == 2) return "\u5347\u7ea7\u8def";
            if (dominant == 3) return "\u8865\u670d\u52a1";
            if (dominant == 4) return "\u8865\u516c\u4ea4";
            if (dominant == 5) return "\u8865\u505c\u8f66";
            if (dominant == 6) return "\u8865\u6c34\u7535";
            if (dominant == 7 || dominant == 8) return "\u63d0\u73af\u5883";
            if (dominant == 9) return "\u5237\u5206\u533a";
            if (dominant == 10) return "\u8865\u901a\u4fe1";
            if (dominant == 11) return "\u8865\u8d27\u8fd0";
            if (!string.IsNullOrEmpty(tile.RoadId) && tile.Traffic >= 58) return "\u5347\u7ea7\u8def";
            if (TileHasUse(tile) && ServiceAccessValue(tile) < 28) return "\u8865\u670d\u52a1";
            if (tile.LandValue < 35) return "\u63d0\u5730\u4ef7";
            if (tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId) && string.IsNullOrEmpty(tile.RoadId)) return "\u5237\u5206\u533a";
            if (!string.IsNullOrEmpty(tile.BuildingId)) return "\u770b\u6210\u957f";
            return "\u67e5\u770b";
        }

        private static string SelectedTileOrderActionLabel(TileData tile, CityMetrics metrics)
        {
            if (tile == null) return "\u67e5\u770b";
            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done && TileHasUse(tile)) return "\u9886\u5956\u52b1";
            if (!string.IsNullOrEmpty(tile.BuildingId) && tile.LandValue >= 58 && ServiceAccessValue(tile) >= 55 && tile.Traffic < 58) return "\u5347\u7ea7/\u6536\u53d6";
            if (tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId) && string.IsNullOrEmpty(tile.RoadId) && tile.Terrain != TerrainType.Water) return "\u63a5\u5206\u533a\u5355";
            return SelectedTileActionLabel(tile, metrics);
        }

        private void OnSelectedTileDetailActionClicked()
        {
            if (controller == null)
            {
                return;
            }

            if (interaction == null || !interaction.HasSelectedTile)
            {
                var recommended = RecommendedOverlayMode(controller.Metrics);
                controller.SetOverlay(recommended);
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u5df2\u5207\u5230\u63a8\u8350\u56fe\u5c42 " + OverlayLabel(recommended), true);
                return;
            }

            var pos = interaction.SelectedTile;
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null)
            {
                return;
            }

            if (tile.Terrain == TerrainType.Water)
            {
                controller.SetOverlay(OverlayMode.Normal);
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u6c34\u9762\u533a\u57df\u5efa\u8bae\u4fdd\u7559", true);
                return;
            }

            var dominant = DominantTileIssueId(tile, controller.Metrics);
            if (dominant == 2)
            {
                controller.SetOverlay(OverlayMode.Traffic);
                interaction.SelectRoadUpgradeTool();
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u4ea4\u901a\uff0c\u5df2\u9009\u5347\u7ea7\u8def", true);
                return;
            }

            if (dominant == 3)
            {
                controller.SetOverlay(OverlayMode.Services);
                interaction.SelectBuildingTool(SelectedTileServiceToolId(tile, controller.Metrics));
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u670d\u52a1\uff0c\u5df2\u9009\u6700\u7f3a\u7684\u8bbe\u65bd", true);
                return;
            }

            if (dominant == 4)
            {
                controller.SetOverlay(OverlayMode.Transit);
                interaction.SelectBuildingTool(controller.Metrics != null && controller.Metrics.Population >= 520 ? "metro_station" : "bus_hub");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u516c\u4ea4\uff0c\u5df2\u9009\u8fd0\u8f93\u8282\u70b9", true);
                return;
            }

            if (dominant == 5)
            {
                controller.SetOverlay(OverlayMode.Parking);
                interaction.SelectBuildingTool("parking_garage");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u505c\u8f66\uff0c\u5df2\u9009\u505c\u8f66\u697c", true);
                return;
            }

            if (dominant == 6)
            {
                controller.SetOverlay(OverlayMode.Utilities);
                interaction.SelectBuildingTool(controller.Metrics != null && controller.Metrics.FloodRisk >= 45 ? "rain_garden" : "water_tower");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u6c34\u7535/\u96e8\u6d2a\uff0c\u5df2\u9009\u8bbe\u65bd", true);
                return;
            }

            if (dominant == 7 || dominant == 8)
            {
                controller.SetOverlay(dominant == 8 ? OverlayMode.Pollution : OverlayMode.LandValue);
                interaction.SelectBuildingTool("pocket_park");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u73af\u5883/\u5730\u4ef7\uff0c\u5df2\u9009\u516c\u56ed", true);
                return;
            }

            if (dominant == 9)
            {
                controller.SetOverlay(OverlayMode.Zoning);
                interaction.SelectZoneTool(SelectedTileRecommendedZone(controller.Metrics));
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u7a7a\u5730\u672a\u89c4\u5212\uff0c\u5df2\u9009\u5206\u533a", true);
                return;
            }

            if (dominant == 10)
            {
                controller.SetOverlay(OverlayMode.Communications);
                interaction.SelectBuildingTool(tile.MailAccess < tile.CommunicationAccess ? "post_office" : "telecom_hub");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u901a\u4fe1/\u90ae\u653f\uff0c\u5df2\u9009\u8bbe\u65bd", true);
                return;
            }

            if (dominant == 11)
            {
                controller.SetOverlay(OverlayMode.Logistics);
                interaction.SelectBuildingTool("cargo_depot");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u4e3b\u56e0\u8d27\u8fd0/\u56de\u6536\uff0c\u5df2\u9009\u8282\u70b9", true);
                return;
            }

            if (!string.IsNullOrEmpty(tile.RoadId) && tile.Traffic >= 58)
            {
                controller.SetOverlay(OverlayMode.Traffic);
                interaction.SelectRoadUpgradeTool();
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u5df2\u9009\u5347\u7ea7\u8def\uff0c\u70b9\u51fb\u6ee1\u8f7d\u8def\u6bb5", true);
                return;
            }

            if (TileHasUse(tile) && ServiceAccessValue(tile) < 28)
            {
                controller.SetOverlay(OverlayMode.Services);
                interaction.SelectBuildingTool(SelectedTileServiceToolId(tile, controller.Metrics));
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u5df2\u9009\u670d\u52a1\u8bbe\u65bd\uff0c\u653e\u5728\u7f3a\u53e3\u8def\u53e3", true);
                return;
            }

            if (tile.LandValue < 35 && TileHasUse(tile))
            {
                controller.SetOverlay(OverlayMode.LandValue);
                interaction.SelectBuildingTool("pocket_park");
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u5df2\u9009\u516c\u56ed\uff0c\u7528\u4e8e\u62c9\u5347\u5730\u4ef7", true);
                return;
            }

            if (tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId) && string.IsNullOrEmpty(tile.RoadId))
            {
                controller.SetOverlay(OverlayMode.Zoning);
                interaction.SelectZoneTool(SelectedTileRecommendedZone(controller.Metrics));
                controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u5df2\u9009\u63a8\u8350\u5206\u533a\uff0c\u62d6\u62c9\u7a7a\u5730\u6210\u7247\u89c4\u5212", true);
                return;
            }

            controller.SetOverlay(RecommendedOverlayMode(controller.Metrics));
            controller.PublishHudFeedback("\u5730\u5757\u5ba1\u6279\uff1a\u5f53\u524d\u5730\u5757\u8fd0\u884c\u53ef\u63a7\uff0c\u5df2\u5207\u63a8\u8350\u56fe\u5c42", true);
        }

        private static string SelectedTileServiceToolId(TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return "pocket_park";
            }

            if (tile.FireProtectionAccess < 24 || (metrics != null && metrics.FireRisk >= 55)) return "fire_station";
            if (tile.SecurityAccess < 24 || tile.SafetyAccess < 24 || (metrics != null && metrics.CrimePressure >= 55)) return "police_kiosk";
            if (tile.HealthAccess < 24) return "health_post";
            if (tile.EducationAccess < 24) return "primary_school";
            if (tile.ParkAccess < 24 || tile.LandValue < 35) return "pocket_park";
            return "pocket_park";
        }

        private static ZoneType SelectedTileRecommendedZone(CityMetrics metrics)
        {
            if (metrics != null && metrics.Demand != null)
            {
                if (metrics.Demand.Residential >= Mathf.Max(metrics.Demand.Commercial, metrics.Demand.Industrial)) return ZoneType.Residential;
                if (metrics.Demand.Commercial >= metrics.Demand.Industrial) return ZoneType.Commercial;
                return ZoneType.Industrial;
            }

            return ZoneType.Residential;
        }

        private void BuildPlanningLensStrip(Transform root)
        {
            // CITY_SKYLINES_PLANNING_LENS_STRIP turns the lower HUD into quick information lenses.
            planningLensCards.Clear();
            var strip = CreatePanel(root, "Planning Lens Strip", AnchorBottom(), new Vector2(164f, 146f), new Vector2(-392f, 190f));
            var image = strip.GetComponent<Image>();
            image.color = new Color32(18, 54, 42, 214);
            AddSoftCardShadow(strip, 34);
            AddPanelTopAccent(strip, new Color32(65, 183, 190, 154), 3f);
            var outline = strip.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 92);
            outline.effectDistance = new Vector2(1.3f, -1.3f);
            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 7, 7);
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddPlanningLensSegment(strip.transform, "\u9700\u6c42", "\u9700", new Color32(96, 190, 122, 228), () => SelectPlanningLens(OverlayMode.Zoning, "\u9700\u6c42"));
            AddPlanningLensSegment(strip.transform, "\u670d\u52a1", "\u670d", new Color32(244, 139, 124, 228), () => SelectPlanningLens(OverlayMode.Services, "\u670d\u52a1"));
            AddPlanningLensSegment(strip.transform, "\u9053\u8def", "\u8def", new Color32(244, 173, 66, 228), () => SelectPlanningLens(OverlayMode.Traffic, "\u9053\u8def"));
            AddPlanningLensSegment(strip.transform, "\u8d22\u653f", "$", new Color32(255, 207, 86, 228), () => SelectPlanningLens(OverlayMode.LandValue, "\u8d22\u653f"));
            AddPlanningLensSegment(strip.transform, "\u6210\u957f", "\u5347", new Color32(112, 192, 214, 228), () => SelectPlanningLens(OverlayMode.LandValue, "\u6210\u957f"));
        }

        private void AddPlanningLensSegment(Transform parent, string labelText, string glyphText, Color32 accent, UnityAction action)
        {
            var segment = CreatePanel(parent, "Planning Lens " + labelText, AnchorFree(), Vector2.zero, Vector2.zero);
            var image = segment.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 26);
            planningLensCards.Add(image);
            var outline = segment.AddComponent<Outline>();
            outline.enabled = false;
            outline.effectColor = new Color32(accent.r, accent.g, accent.b, 0);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            planningLensOutlines.Add(outline);
            var button = segment.AddComponent<Button>();
            button.onClick.AddListener(action);
            segment.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var fill = CreateToolButtonAccent(segment.transform, "Lens Fill", AnchorBottom(), new Vector2(3f, 3f), new Vector2(-3f, 9f), accent);
            fill.raycastTarget = false;
            planningLensFills.Add(fill);

            var chip = CreateToolButtonAccent(segment.transform, "Lens Glyph Chip", AnchorLeft(), new Vector2(4f, 6f), new Vector2(24f, -6f), accent);
            chip.raycastTarget = false;
            var glyph = CreateText(chip.transform, "Glyph", glyphText, 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(43, 64, 70, 255);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);

            var badge = CreateToolButtonAccent(segment.transform, "Lens State Badge", AnchorTopRight(), new Vector2(-30f, -14f), new Vector2(-4f, -3f), new Color32(222, 246, 219, 120));
            badge.raycastTarget = false;
            planningLensBadgeImages.Add(badge);
            var badgeText = CreateText(badge.transform, "State", "\u7a33", 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            badgeText.color = new Color32(33, 62, 44, 245);
            badgeText.raycastTarget = false;
            Stretch(badgeText.rectTransform);
            planningLensBadgeTexts.Add(badgeText);

            var text = CreateText(segment.transform, "Label", labelText + " --", 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            text.color = new Color32(245, 255, 238, 245);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(29f, 0f);
            text.rectTransform.offsetMax = new Vector2(-34f, 0f);
            planningLensTexts.Add(text);
            AddHudFacet(segment.transform, "Lens Facet", new Vector4(0.45f, 0.58f, 0.96f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 24), -8f);
            AddPlanningLensRouteMarks(segment.transform, accent);
        }

        private void AddPlanningLensRouteMarks(Transform parent, Color32 accent)
        {
            for (var i = 0; i < 3; i += 1)
            {
                var x = 33f + i * 10f;
                var mark = CreateToolButtonAccent(parent, "Lens Route Mark " + i, new Vector4(0f, 0f, 0f, 0f), new Vector2(x, 4f), new Vector2(x + 6f, 6f), new Color32(accent.r, accent.g, accent.b, 92));
                mark.raycastTarget = false;
            }
        }

        private void SelectPlanningLens(OverlayMode mode, string label)
        {
            if (controller == null)
            {
                return;
            }

            var metrics = controller.Metrics;
            var previous = controller.OverlayMode;
            controller.SetOverlay(mode);
            var pressure = OverlayPressureScore(mode, metrics);
            var issue = CompactCardText(MiniMapPrimaryIssueLabel(metrics), 6);
            var action = CompactCardText(BuildLayerToolActionChain(metrics, previous, mode), 28);
            controller.PublishHudFeedback("\u4fe1\u606f\u89c6\u56fe " + label + " \u538b" + pressure + " \u4e3b" + issue + " > " + action, true);
        }

        private void RefreshPlanningLensStrip(CityMetrics metrics)
        {
            if (planningLensTexts.Count < 5 || planningLensFills.Count < 5)
            {
                return;
            }

            SetPlanningLensSegment(0, "\u9700\u6c42", PlanningDemandPressure(metrics), new Color32(96, 190, 122, 228), "\u5206\u533a", OverlayMode.Zoning);
            SetPlanningLensSegment(1, "\u670d\u52a1", metrics != null ? metrics.ServiceGapPressure : 0, new Color32(244, 139, 124, 228), "\u8986\u76d6", OverlayMode.Services);
            SetPlanningLensSegment(2, "\u9053\u8def", metrics != null ? metrics.RoadBottleneckPressure : 0, new Color32(244, 173, 66, 228), "\u901a\u52e4", OverlayMode.Traffic);
            SetPlanningLensSegment(3, "\u8d22\u653f", PlanningFiscalPressure(metrics), new Color32(255, 207, 86, 228), "\u9884\u7b97", OverlayMode.LandValue);
            SetPlanningLensSegment(4, "\u6210\u957f", PlanningGrowthPressure(metrics), new Color32(112, 192, 214, 228), "\u5347\u7ea7", OverlayMode.LandValue);
        }

        private void SetPlanningLensSegment(int index, string label, int pressure, Color32 accent, string hint, OverlayMode targetMode)
        {
            var clamped = Mathf.Clamp(pressure, 0, 100);
            var active = controller != null && controller.OverlayMode == targetMode;
            var metrics = controller != null ? controller.Metrics : null;
            var recommended = !active && RecommendedOverlayMode(metrics) == targetMode && OverlayPressureScore(targetMode, metrics) >= 42;
            var orderCue = PlanningLensOrderCue(metrics, targetMode, clamped);
            if (index < planningLensCards.Count && planningLensCards[index] != null)
            {
                // CITY_SKYLINES_LENS_ACTIVE_STATE makes the quick info lenses behave like selected CS info views.
                planningLensCards[index].color = active
                    ? new Color32(45, 154, 174, 238)
                    : (recommended ? new Color32(255, 207, 86, 72) : new Color32(245, 255, 238, 26));
            }

            if (index < planningLensFills.Count && planningLensFills[index] != null)
            {
                planningLensFills[index].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, clamped) / 100f), 1f);
                planningLensFills[index].color = active
                    ? new Color32(245, 255, 238, 230)
                    : clamped >= 70
                    ? new Color32(255, 207, 86, 238)
                    : new Color32(accent.r, accent.g, accent.b, 180);
            }

            if (index < planningLensTexts.Count && planningLensTexts[index] != null)
            {
                planningLensTexts[index].text = (active ? "\u25cf" : recommended ? "\u8350" : string.Empty) + label + " " + clamped + " " + hint + orderCue;
                planningLensTexts[index].color = active || clamped >= 70
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 245);
            }

            if (index < planningLensBadgeImages.Count && planningLensBadgeImages[index] != null)
            {
                var badgeColor = PlanningLensBadgeColor(active, recommended, clamped, accent);
                planningLensBadgeImages[index].color = badgeColor;
            }

            if (index < planningLensBadgeTexts.Count && planningLensBadgeTexts[index] != null)
            {
                planningLensBadgeTexts[index].text = PlanningLensBadgeLabel(active, recommended, clamped);
                planningLensBadgeTexts[index].color = active || clamped >= 70
                    ? new Color32(70, 44, 16, 248)
                    : new Color32(33, 62, 44, 245);
            }

            if (index < planningLensOutlines.Count && planningLensOutlines[index] != null)
            {
                planningLensOutlines[index].enabled = active || recommended || clamped >= 70;
                planningLensOutlines[index].effectColor = active
                    ? new Color32(245, 255, 238, 226)
                    : (recommended ? new Color32(255, 207, 86, 220) : new Color32(accent.r, accent.g, accent.b, 185));
                planningLensOutlines[index].effectDistance = active ? new Vector2(2f, -2f) : new Vector2(1.35f, -1.35f);
            }
        }

        private static string PlanningLensBadgeLabel(bool active, bool recommended, int pressure)
        {
            if (active) return "\u5f00";
            if (recommended || pressure >= 78) return "\u63a8";
            if (pressure >= 58) return "\u70ed";
            return "\u7a33";
        }

        private static string PlanningLensOrderCue(CityMetrics metrics, OverlayMode targetMode, int pressure)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return " \u9886";
            }

            if (targetMode == OverlayMode.Zoning && metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                var remaining = Mathf.Max(0, metrics.ActiveObjective.Required - metrics.ActiveObjective.Progress);
                return remaining <= 1 ? " \u6536\u5c3e" : " \u5355" + remaining;
            }

            if (targetMode == OverlayMode.Services && metrics.ServiceGapPressure >= 55)
            {
                return " \u8865";
            }

            if (targetMode == OverlayMode.Traffic && metrics.RoadBottleneckPressure >= 55)
            {
                return " \u4fee";
            }

            if (metrics.BuildingUpgradeReadyCount > 0 && pressure >= 50)
            {
                return " \u5347" + metrics.BuildingUpgradeReadyCount;
            }

            return string.Empty;
        }

        private static Color32 PlanningLensBadgeColor(bool active, bool recommended, int pressure, Color32 accent)
        {
            if (active) return new Color32(255, 207, 86, 238);
            if (recommended || pressure >= 78) return new Color32(255, 225, 124, 224);
            if (pressure >= 58) return new Color32(accent.r, accent.g, accent.b, 196);
            return new Color32(222, 246, 219, 118);
        }

        private static int PlanningDemandPressure(CityMetrics metrics)
        {
            if (metrics == null || metrics.Demand == null)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.Max(metrics.DemandUrgency, Mathf.Max(metrics.Demand.Residential, Mathf.Max(metrics.Demand.Commercial, metrics.Demand.Industrial))), 0, 100);
        }

        private static int PlanningFiscalPressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var incomePressure = metrics.NetIncome < 0 ? Mathf.Clamp(-metrics.NetIncome / 12, 0, 100) : 0;
            return Mathf.Clamp(Mathf.Max(metrics.BudgetStress, Mathf.Max(metrics.DebtPressure, incomePressure)), 0, 100);
        }

        private static int PlanningGrowthPressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var upgradePressure = Mathf.Clamp(metrics.BuildingUpgradeReadyCount * 10 + metrics.BuildingUpgradeBlockedCount * 8, 0, 100);
            return Mathf.Clamp(Mathf.Max(upgradePressure, Mathf.Max(100 - metrics.DevelopmentQuality, metrics.LandUseConflict)), 0, 100);
        }

        private void BuildOverlayLegendCard(Transform root)
        {
            // CITY_SKYLINES_INFO_VIEW_LEGEND keeps the active information layer readable beside the map.
            var card = CreatePanel(root, "Overlay Legend Card", AnchorBottomRight(), new Vector2(-386f, 166f), new Vector2(-184f, 238f));
            overlayLegendCardImage = card.GetComponent<Image>();
            overlayLegendCardImage.color = new Color32(18, 54, 42, 222);
            AddSoftCardShadow(card, 38);
            AddPanelTopAccent(card, new Color32(65, 183, 190, 154), 3f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 92);
            outline.effectDistance = new Vector2(1.35f, -1.35f);

            overlayLegendAccentImage = CreateToolButtonAccent(card.transform, "Legend Accent Rail", AnchorLeft(), new Vector2(4f, 8f), new Vector2(8f, -8f), new Color32(65, 183, 190, 190));
            overlayLegendAccentImage.raycastTarget = false;

            overlayLegendTitleText = CreateText(card.transform, "Legend Title", "\u4fe1\u606f\u56fe\u5c42 --", 11, FontStyle.Bold, TextAnchor.UpperLeft);
            overlayLegendTitleText.color = new Color32(245, 255, 238, 250);
            Stretch(overlayLegendTitleText.rectTransform);
            overlayLegendTitleText.rectTransform.offsetMin = new Vector2(14f, 33f);
            overlayLegendTitleText.rectTransform.offsetMax = new Vector2(-52f, -6f);

            overlayLegendStateBadgeImage = CreateToolButtonAccent(card.transform, "Legend State Badge", AnchorTopRight(), new Vector2(-48f, -22f), new Vector2(-8f, -6f), new Color32(255, 207, 86, 188));
            overlayLegendStateBadgeImage.raycastTarget = false;
            overlayLegendStateText = CreateText(overlayLegendStateBadgeImage.transform, "State", "\u63a8\u8350", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            overlayLegendStateText.color = new Color32(49, 63, 37, 255);
            overlayLegendStateText.raycastTarget = false;
            Stretch(overlayLegendStateText.rectTransform);

            overlayLegendDetailText = CreateText(card.transform, "Legend Detail", "\u70ed\u533a --", 9, FontStyle.Bold, TextAnchor.UpperLeft);
            overlayLegendDetailText.color = new Color32(206, 238, 216, 238);
            overlayLegendDetailText.lineSpacing = 0.86f;
            Stretch(overlayLegendDetailText.rectTransform);
            overlayLegendDetailText.rectTransform.offsetMin = new Vector2(14f, 6f);
            overlayLegendDetailText.rectTransform.offsetMax = new Vector2(-8f, -31f);

            var track = CreatePanel(card.transform, "Legend Pressure Track", new Vector4(0f, 0f, 1f, 0f), new Vector2(14f, 8f), new Vector2(-10f, 14f));
            var trackImage = track.GetComponent<Image>();
            trackImage.color = new Color32(245, 255, 238, 34);
            trackImage.raycastTarget = false;
            track.AddComponent<LayoutElement>().ignoreLayout = true;
            var fill = CreatePanel(track.transform, "Legend Pressure Fill", AnchorStretch(), Vector2.zero, Vector2.zero);
            overlayLegendPressureFill = fill.GetComponent<Image>();
            overlayLegendPressureFill.color = new Color32(65, 183, 190, 180);
            overlayLegendPressureFill.raycastTarget = false;
            fill.AddComponent<LayoutElement>().ignoreLayout = true;
            AddOverlayLegendTicks(track.transform);
            AddHudFacet(card.transform, "Legend Facet", new Vector4(0.55f, 0.52f, 0.98f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 28), -8f);
        }

        private void AddOverlayLegendTicks(Transform parent)
        {
            for (var i = 1; i < 4; i += 1)
            {
                var x = i / 4f;
                var tick = CreatePanel(parent, "Legend Tick " + i, new Vector4(x, 0f, x, 1f), new Vector2(-0.7f, 0f), new Vector2(0.7f, 0f));
                var image = tick.GetComponent<Image>();
                image.color = new Color32(245, 255, 238, 72);
                image.raycastTarget = false;
                tick.AddComponent<LayoutElement>().ignoreLayout = true;
            }
        }

        private void RefreshOverlayLegendCard(CityMetrics metrics)
        {
            if (overlayLegendTitleText == null && overlayLegendDetailText == null && overlayLegendPressureFill == null)
            {
                return;
            }

            var active = controller != null ? controller.OverlayMode : OverlayMode.Normal;
            var recommended = RecommendedOverlayMode(metrics);
            var layer = active == OverlayMode.Normal && recommended != OverlayMode.Normal ? recommended : active;
            var pressure = OverlayPressureScore(layer, metrics);
            var issue = metrics != null ? MiniMapPrimaryIssueLabel(metrics) : "--";
            var action = CompactCardText(BuildLayerToolActionChain(metrics, active, layer), 24);
            var accent = layer == OverlayMode.Normal ? new Color32(206, 238, 216, 255) : OverlayModeAccentColor(layer);

            if (overlayLegendTitleText != null)
            {
                overlayLegendTitleText.text = "\u56fe\u5c42 " + OverlayLabel(layer) + "  \u538b" + pressure;
                overlayLegendTitleText.color = pressure >= 70
                    ? new Color32(255, 232, 150, 255)
                    : new Color32(245, 255, 238, 250);
            }

            if (overlayLegendDetailText != null)
            {
                overlayLegendDetailText.text = "\u5c0f\u52a9\u624b " + CompactCardText(issue, 7) + "\n" + action;
            }

            if (overlayLegendPressureFill != null)
            {
                overlayLegendPressureFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, pressure) / 100f), 1f);
                overlayLegendPressureFill.color = pressure >= 70
                    ? new Color32(255, 207, 86, 218)
                    : new Color32(accent.r, accent.g, accent.b, 176);
            }

            if (overlayLegendAccentImage != null)
            {
                overlayLegendAccentImage.color = new Color32(accent.r, accent.g, accent.b, pressure >= 70 ? (byte)228 : (byte)184);
            }

            if (overlayLegendStateBadgeImage != null)
            {
                overlayLegendStateBadgeImage.color = OverlayLegendStateColor(layer, recommended, pressure, accent);
            }

            if (overlayLegendStateText != null)
            {
                overlayLegendStateText.text = OverlayLegendStateLabel(layer, active, recommended, pressure);
                overlayLegendStateText.color = pressure >= 70 || layer == recommended
                    ? new Color32(70, 44, 16, 248)
                    : new Color32(33, 62, 44, 245);
            }

            if (overlayLegendCardImage != null)
            {
                overlayLegendCardImage.color = pressure >= 70
                    ? new Color32(62, 48, 36, 226)
                    : new Color32(18, 54, 42, 222);
            }
        }

        private static string OverlayLegendStateLabel(OverlayMode layer, OverlayMode active, OverlayMode recommended, int pressure)
        {
            if (layer == active && active != OverlayMode.Normal) return "\u5f53\u524d";
            if (layer == recommended && recommended != OverlayMode.Normal) return "\u63a8\u8350";
            if (pressure >= 70) return "\u70ed\u533a";
            return "\u5e73\u7a33";
        }

        private static Color32 OverlayLegendStateColor(OverlayMode layer, OverlayMode recommended, int pressure, Color32 accent)
        {
            if (layer == recommended && recommended != OverlayMode.Normal) return new Color32(255, 221, 108, 232);
            if (pressure >= 70) return new Color32(255, 188, 66, 226);
            if (pressure >= 45) return new Color32(accent.r, accent.g, accent.b, 190);
            return new Color32(222, 246, 219, 132);
        }

        private void BuildPlacementQuoteCard(Transform root)
        {
            // CITY_SKYLINES_PLACEMENT_QUOTE turns hover previews into a build-cost and site-fit card.
            var card = CreatePanel(root, "Placement Quote Card", AnchorBottom(), new Vector2(164f, 194f), new Vector2(-392f, 246f));
            placementQuoteCardImage = card.GetComponent<Image>();
            placementQuoteCardImage.color = new Color32(19, 55, 42, 218);
            AddSoftCardShadow(card, 36);
            AddPanelTopAccent(card, new Color32(255, 207, 86, 168), 3f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 92);
            outline.effectDistance = new Vector2(1.25f, -1.25f);

            placementQuoteAccentImage = CreateToolButtonAccent(card.transform, "Quote Accent Rail", AnchorLeft(), new Vector2(4f, 7f), new Vector2(8f, -7f), new Color32(255, 207, 86, 190));
            placementQuoteAccentImage.raycastTarget = false;

            placementQuoteTitleText = CreateText(card.transform, "Quote Title", "\u9009\u5740\u8bc4\u4f30 --", 12, FontStyle.Bold, TextAnchor.UpperLeft);
            placementQuoteTitleText.color = new Color32(245, 255, 238, 252);
            placementQuoteTitleText.raycastTarget = false;
            Stretch(placementQuoteTitleText.rectTransform);
            placementQuoteTitleText.rectTransform.offsetMin = new Vector2(15f, 30f);
            placementQuoteTitleText.rectTransform.offsetMax = new Vector2(-258f, -5f);

            placementQuoteStateBadgeImage = CreateToolButtonAccent(card.transform, "Quote State Badge", AnchorTopRight(), new Vector2(-252f, -22f), new Vector2(-206f, -6f), new Color32(255, 236, 150, 220));
            placementQuoteStateBadgeImage.raycastTarget = false;
            placementQuoteStateText = CreateText(placementQuoteStateBadgeImage.transform, "State", "\u63a5\u5355", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            placementQuoteStateText.color = new Color32(83, 68, 30, 255);
            placementQuoteStateText.raycastTarget = false;
            Stretch(placementQuoteStateText.rectTransform);

            placementQuoteMetricText = CreateText(card.transform, "Quote Metrics", "\u9002\u914d --  \u98ce\u9669 --", 10, FontStyle.Bold, TextAnchor.UpperRight);
            placementQuoteMetricText.color = new Color32(206, 238, 216, 242);
            placementQuoteMetricText.raycastTarget = false;
            Stretch(placementQuoteMetricText.rectTransform);
            placementQuoteMetricText.rectTransform.offsetMin = new Vector2(360f, 30f);
            placementQuoteMetricText.rectTransform.offsetMax = new Vector2(-10f, -5f);

            placementQuoteDetailText = CreateText(card.transform, "Quote Detail", "\u9009\u5de5\u5177\u5e76\u60ac\u505c\u5730\u5757\uff0c\u67e5\u770b\u62a5\u4ef7\u548c\u963b\u6321\u539f\u56e0", 10, FontStyle.Bold, TextAnchor.UpperLeft);
            placementQuoteDetailText.color = new Color32(206, 238, 216, 238);
            placementQuoteDetailText.lineSpacing = 0.86f;
            placementQuoteDetailText.resizeTextForBestFit = true;
            placementQuoteDetailText.resizeTextMinSize = 8;
            placementQuoteDetailText.resizeTextMaxSize = 10;
            placementQuoteDetailText.raycastTarget = false;
            Stretch(placementQuoteDetailText.rectTransform);
            placementQuoteDetailText.rectTransform.offsetMin = new Vector2(15f, 7f);
            placementQuoteDetailText.rectTransform.offsetMax = new Vector2(-14f, -25f);

            var track = CreatePanel(card.transform, "Quote Score Track", new Vector4(0f, 0f, 1f, 0f), new Vector2(15f, 7f), new Vector2(-14f, 12f));
            var trackImage = track.GetComponent<Image>();
            trackImage.color = new Color32(245, 255, 238, 34);
            trackImage.raycastTarget = false;
            track.AddComponent<LayoutElement>().ignoreLayout = true;
            placementQuoteScoreFill = CreateToolButtonAccent(track.transform, "Quote Score Fill", AnchorStretch(), Vector2.zero, Vector2.zero, new Color32(96, 214, 118, 176));
            placementQuoteScoreFill.raycastTarget = false;
            AddHudFacet(card.transform, "Quote Low Poly Facet", new Vector4(0.42f, 0.54f, 0.96f, 0.88f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 24), -8f);
            card.SetActive(false);
        }

        private void RefreshPlacementQuoteCard(CityMetrics metrics)
        {
            if (placementQuoteTitleText == null && placementQuoteDetailText == null && placementQuoteScoreFill == null)
            {
                return;
            }

            var preview = controller != null ? controller.CurrentPreview : null;
            SetPlacementQuoteVisible(preview != null);
            if (preview == null)
            {
                return;
            }

            var active = ActiveToolBinding();
            var fit = PlacementQuoteFitScore(preview, active, metrics);
            var risk = PlacementQuoteRiskScore(preview, active, metrics);
            var accent = PlacementQuoteAccent(preview, fit, risk);

            if (placementQuoteTitleText != null)
            {
                placementQuoteTitleText.text = BuildPlacementQuoteTitle(preview, active);
                placementQuoteTitleText.color = preview != null && !preview.Ok
                    ? new Color32(255, 218, 134, 255)
                    : new Color32(245, 255, 238, 252);
            }

            if (placementQuoteMetricText != null)
            {
                placementQuoteMetricText.text = "\u9002\u914d " + fit + "  \u5956\u52b1 " + PlacementOrderRewardLabel(preview, metrics) + PlacementCashSuffix(metrics);
                placementQuoteMetricText.color = risk >= 70
                    ? new Color32(255, 218, 134, 255)
                    : new Color32(206, 238, 216, 242);
            }

            if (placementQuoteDetailText != null)
            {
                placementQuoteDetailText.text = BuildPlacementQuoteDetail(preview, active, metrics);
            }

            if (placementQuoteStateBadgeImage != null)
            {
                placementQuoteStateBadgeImage.color = PlacementQuoteStateBadgeColor(preview, metrics, fit, risk);
            }

            if (placementQuoteStateText != null)
            {
                placementQuoteStateText.text = PlacementQuoteStateLabel(preview, metrics, fit, risk);
                placementQuoteStateText.color = preview != null && preview.Ok && metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done
                    ? new Color32(35, 95, 62, 255)
                    : new Color32(83, 68, 30, 255);
            }

            if (placementQuoteScoreFill != null)
            {
                placementQuoteScoreFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, fit) / 100f), 1f);
                placementQuoteScoreFill.color = new Color32(accent.r, accent.g, accent.b, risk >= 70 ? (byte)222 : (byte)176);
            }

            if (placementQuoteAccentImage != null)
            {
                placementQuoteAccentImage.color = new Color32(accent.r, accent.g, accent.b, 204);
            }

            if (placementQuoteCardImage != null)
            {
                placementQuoteCardImage.color = risk >= 70
                    ? new Color32(64, 50, 35, 222)
                    : new Color32(19, 55, 42, 218);
            }
        }

        private void SetFeaturedBuildShelfVisible(bool visible)
        {
            if (featuredBuildShelfImage != null && featuredBuildShelfImage.gameObject.activeSelf != visible)
            {
                featuredBuildShelfImage.gameObject.SetActive(visible);
            }
        }

        private void SetPlacementQuoteVisible(bool visible)
        {
            if (placementQuoteCardImage != null && placementQuoteCardImage.gameObject.activeSelf != visible)
            {
                placementQuoteCardImage.gameObject.SetActive(visible);
            }
        }

        private string BuildPlacementQuoteTitle(ConstructionPreview preview, ToolButtonBinding active)
        {
            if (preview != null)
            {
                var state = preview.Ok ? "\u53ef\u4e0b\u5355 " : "\u8ba2\u5355\u53d7\u963b ";
                return state + CompactCardText(preview.Title, 12);
            }

            if (active != null)
            {
                return "\u5efa\u9020\u8ba2\u5355 " + CompactCardText(ToolBindingLabel(active), 10);
            }

            return "\u5efa\u9020\u8ba2\u5355";
        }

        private string BuildPlacementQuoteDetail(ConstructionPreview preview, ToolButtonBinding active, CityMetrics metrics)
        {
            if (preview != null)
            {
                var diagnosis = !string.IsNullOrEmpty(preview.SiteDiagnosis)
                    ? preview.SiteDiagnosis
                    : FirstPreviewDetailLine(preview);
                var second = PreviewSecondLine(preview);
                var action = preview.Ok
                    ? (string.IsNullOrEmpty(preview.ConfirmLabel) ? "\u70b9\u51fb\u63a5\u5355" : "\u70b9\u51fb" + preview.ConfirmLabel)
                    : "\u5148\u89e3\u51b3\u963b\u6321";
                return CompactCardText(action + " / " + diagnosis, 42)
                    + "\n" + CompactCardText(PlacementOrderNextStep(preview, active, metrics, second), 48);
            }

            if (active != null)
            {
                return CompactCardText("\u5efa\u8bae " + ToolPlacementHint(active, metrics), 42)
                    + "\n" + CompactCardText("\u63a5\u5355\u7406\u7531 " + ToolRecommendationDriverLabel(active, metrics), 48);
            }

            return "\u9009\u5de5\u5177\u5e76\u60ac\u505c\u5730\u5757\uff0c\u67e5\u770b\u8ba2\u5355\u5956\u52b1/\u98ce\u9669/\u9002\u914d";
        }

        private static string PlacementOrderRewardLabel(ConstructionPreview preview, CityMetrics metrics)
        {
            if (preview != null && !preview.Ok)
            {
                return "\u6682\u65e0";
            }

            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return "\u53ef\u9886";
            }

            if (metrics != null && metrics.DemandUrgency >= 60)
            {
                return "\u9700\u6c42";
            }

            if (metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7";
            }

            return "\u91d1\u5e01";
        }

        private string PlacementOrderNextStep(ConstructionPreview preview, ToolButtonBinding active, CityMetrics metrics, string fallback)
        {
            if (preview != null && preview.Ok)
            {
                var reward = PlacementOrderRewardLabel(preview, metrics);
                return "\u4e0b\u4e00\u6b65 \u786e\u8ba4\u5efa\u9020 > \u5956\u52b1" + reward;
            }

            if (active != null)
            {
                return "\u4e0b\u4e00\u6b65 " + ToolPlacementHint(active, metrics);
            }

            return "\u4e0b\u4e00\u6b65 " + fallback;
        }

        private static string PlacementQuoteStateLabel(ConstructionPreview preview, CityMetrics metrics, int fit, int risk)
        {
            if (preview != null && !preview.Ok) return "\u53d7\u963b";
            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done) return "\u53ef\u9886";
            if (fit >= 70 && risk < 60) return "\u63a5\u5355";
            if (risk >= 70) return "\u98ce\u9669";
            return "\u8bd5\u653e";
        }

        private static Color32 PlacementQuoteStateBadgeColor(ConstructionPreview preview, CityMetrics metrics, int fit, int risk)
        {
            if (preview != null && !preview.Ok) return new Color32(255, 188, 66, 232);
            if (metrics != null && metrics.ActiveObjective != null && metrics.ActiveObjective.Done) return new Color32(210, 246, 192, 238);
            if (fit >= 70 && risk < 60) return new Color32(255, 236, 150, 226);
            if (risk >= 70) return new Color32(255, 202, 86, 222);
            return new Color32(222, 246, 219, 156);
        }

        private static string PreviewSecondLine(ConstructionPreview preview)
        {
            if (preview == null || preview.Lines == null || preview.Lines.Count == 0)
            {
                return "\u6682\u65e0\u8be6\u60c5";
            }

            var index = !string.IsNullOrEmpty(preview.SiteDiagnosis) && preview.Lines.Count > 1 ? 1 : 0;
            return preview.Lines[Mathf.Clamp(index, 0, preview.Lines.Count - 1)];
        }

        private int PlacementQuoteFitScore(ConstructionPreview preview, ToolButtonBinding active, CityMetrics metrics)
        {
            if (preview != null)
            {
                if (!preview.Ok)
                {
                    return Mathf.Clamp(preview.SiteScore > 0 ? preview.SiteScore : 18, 0, 100);
                }

                return Mathf.Clamp(preview.SiteScore > 0 ? preview.SiteScore : 72, 0, 100);
            }

            return active != null ? Mathf.Clamp(ToolRecommendationScoreWithSelectedTile(active, metrics), 0, 100) : 0;
        }

        private static int PlacementQuoteRiskScore(ConstructionPreview preview, ToolButtonBinding active, CityMetrics metrics)
        {
            var risk = metrics != null ? Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.RoadBottleneckPressure, metrics.ServiceGapPressure)) : 0;
            if (preview != null && !preview.Ok)
            {
                risk = Mathf.Max(risk, 82);
            }

            if (active != null && active.ToolMode == CityToolMode.Demolish)
            {
                risk = Mathf.Max(risk, 48);
            }

            if (active != null && active.ToolMode == CityToolMode.BuildRoad && metrics != null)
            {
                risk = Mathf.Max(risk, metrics.IntersectionDelay);
            }

            return Mathf.Clamp(risk, 0, 100);
        }

        private static Color32 PlacementQuoteAccent(ConstructionPreview preview, int fit, int risk)
        {
            if (preview != null && !preview.Ok) return new Color32(255, 176, 70, 255);
            if (risk >= 70) return new Color32(244, 116, 71, 255);
            if (fit >= 70) return new Color32(96, 214, 118, 255);
            if (fit >= 42) return new Color32(65, 184, 220, 255);
            return new Color32(206, 238, 216, 255);
        }

        private static string PlacementCashSuffix(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            return "  \u73b0\u91d1 " + metrics.Cash;
        }

        private void BuildToolDockBadge(Transform root)
        {
            // REFERENCE_IMAGE_BUILD_DOCK_BADGE echoes the selected build-category tile above the bottom strip.
            var badge = CreatePanel(root, "Build Dock Badge", AnchorBottomLeft(), new Vector2(18f, 112f), new Vector2(116f, 144f));
            buildDockBadgeImage = badge.GetComponent<Image>();
            buildDockBadgeImage.color = new Color32(39, 125, 89, 238);
            buildDockBadgeText = CreateText(badge.transform, "Label", "\u5efa\u9020", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            buildDockBadgeText.color = new Color32(245, 255, 238, 255);
            buildDockBadgeText.lineSpacing = 0.82f;
            buildDockBadgeText.resizeTextForBestFit = true;
            buildDockBadgeText.resizeTextMinSize = 9;
            buildDockBadgeText.resizeTextMaxSize = 15;
            Stretch(buildDockBadgeText.rectTransform);
            buildDockBadgeText.rectTransform.offsetMin = new Vector2(30f, 0f);
            buildDockBadgeText.rectTransform.offsetMax = new Vector2(-6f, 0f);
            AddBuildDockBadgeIcon(badge.transform);
        }

        private void AddBuildDockBadgeIcon(Transform parent)
        {
            // REFERENCE_IMAGE_BUILD_DOCK_MAIN_TILE keeps the selected build category feeling like a large tile.
            var icon = CreatePanel(parent, "Build Dock Badge Icon", new Vector4(0f, 0.5f, 0f, 0.5f), new Vector2(7f, -12f), new Vector2(31f, 12f));
            var image = icon.GetComponent<Image>();
            image.color = new Color32(255, 202, 70, 235);
            image.raycastTarget = false;
            var outline = icon.AddComponent<Outline>();
            outline.effectColor = new Color32(245, 255, 238, 118);
            outline.effectDistance = new Vector2(1f, -1f);
            var layout = icon.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            buildDockBadgeGlyphText = CreateText(icon.transform, "Glyph", "\u5efa", 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            buildDockBadgeGlyphText.color = new Color32(43, 64, 70, 255);
            Stretch(buildDockBadgeGlyphText.rectTransform);
        }

        private void BuildLeftQuickActionCards(Transform root)
        {
            // REFERENCE_IMAGE_LEFT_QUICK_ACTION_CARDS mirrors the task/data buttons above the build dock.
            BuildLeftQuickActionCard(
                root,
                "Left Quick Task",
                new Vector2(14f, 150f),
                new Vector2(80f, 218f),
                "\u4efb\u52a1",
                "\u25a3",
                new Color32(255, 207, 86, 245),
                () =>
                {
                    if (controller != null)
                    {
                        var recommended = RecommendedOverlayMode(controller.Metrics);
                        controller.SetOverlay(recommended == OverlayMode.Normal ? OverlayMode.Zoning : recommended);
                    }
                });
            BuildLeftQuickActionCard(
                root,
                "Left Quick Data",
                new Vector2(88f, 150f),
                new Vector2(154f, 218f),
                "\u6570\u636e",
                "%",
                new Color32(206, 238, 216, 245),
                () =>
                {
                    if (controller != null)
                    {
                        controller.SetOverlay(OverlayMode.LandValue);
                    }
                });
        }

        private void BuildLeftQuickActionCard(Transform root, string name, Vector2 offsetMin, Vector2 offsetMax, string labelText, string glyphText, Color32 accent, UnityAction action)
        {
            var card = CreatePanel(root, name, AnchorBottomLeft(), offsetMin, offsetMax);
            var image = card.GetComponent<Image>();
            image.color = new Color32(23, 61, 43, 236);
            AddSoftCardShadow(card, 42);
            AddPanelTopAccent(card, accent, 3f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color32(accent.r, accent.g, accent.b, 118);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            var button = card.AddComponent<Button>();
            button.onClick.AddListener(action);

            var icon = CreatePanel(card.transform, "Quick Icon", new Vector4(0.5f, 1f, 0.5f, 1f), new Vector2(-16f, -38f), new Vector2(16f, -7f));
            var iconImage = icon.GetComponent<Image>();
            iconImage.color = accent;
            iconImage.raycastTarget = false;
            var iconOutline = icon.AddComponent<Outline>();
            iconOutline.effectColor = new Color32(245, 255, 238, 130);
            iconOutline.effectDistance = new Vector2(1.1f, -1.1f);
            var glyph = CreateText(icon.transform, "Glyph", glyphText, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(43, 64, 70, 255);
            Stretch(glyph.rectTransform);

            var label = CreateText(card.transform, "Label", labelText, 12, FontStyle.Bold, TextAnchor.LowerCenter);
            label.color = new Color32(245, 255, 238, 255);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(4f, 5f);
            label.rectTransform.offsetMax = new Vector2(-4f, -40f);

            var bead = CreatePanel(card.transform, "Quick Notice Bead", new Vector4(1f, 1f, 1f, 1f), new Vector2(-17f, -18f), new Vector2(-5f, -6f));
            var beadImage = bead.GetComponent<Image>();
            beadImage.color = new Color32(244, 116, 71, 242);
            beadImage.raycastTarget = false;
            bead.AddComponent<LayoutElement>().ignoreLayout = true;
            AddHudFacet(card.transform, "Quick Card Facet", new Vector4(0.12f, 0.66f, 0.88f, 0.9f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 30), -7f);
        }

        private void BuildActionChainStrip(Transform root)
        {
            // CITY_SKYLINES_ACTION_CHAIN_STRIP makes diagnosis, tool and placement read as one playable workflow.
            var strip = CreatePanel(root, "Action Chain Strip", AnchorBottom(), new Vector2(124f, 112f), new Vector2(-282f, 144f));
            actionChainStripImage = strip.GetComponent<Image>();
            actionChainStripImage.color = new Color32(18, 54, 42, 218);
            AddSoftCardShadow(strip, 34);
            var outline = strip.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 92);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var fillObject = new GameObject("Action Chain Pressure Fill");
            fillObject.transform.SetParent(strip.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.1f, 1f);
            fillRect.offsetMin = new Vector2(3f, 4f);
            fillRect.offsetMax = new Vector2(-3f, -4f);
            actionChainPressureFill = fillObject.AddComponent<Image>();
            actionChainPressureFill.color = new Color32(65, 169, 184, 118);
            actionChainPressureFill.raycastTarget = false;

            AddHudFacet(strip.transform, "Action Chain Glass Fold", new Vector4(0.68f, 0.56f, 0.98f, 0.88f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 42), -8f);
            AddActionChainStepChip(strip.transform, "\u770b", 0f, new Color32(65, 169, 184, 222));
            AddActionChainStepChip(strip.transform, "\u5efa", 0.09f, new Color32(255, 207, 86, 226));
            AddActionChainStepChip(strip.transform, "\u653e", 0.18f, new Color32(96, 190, 122, 226));
            AddActionChainStepChip(strip.transform, "\u9886", 0.27f, new Color32(255, 236, 150, 226));

            actionChainText = CreateText(strip.transform, "Action Chain Text", "\u770b\u56fe\u5c42 -> \u9009\u5de5\u5177 -> \u843d\u70b9 -> \u9886\u5956", 12, FontStyle.Bold, TextAnchor.MiddleLeft);
            actionChainText.color = new Color32(245, 255, 238, 248);
            Stretch(actionChainText.rectTransform);
            actionChainText.rectTransform.offsetMin = new Vector2(122f, 0f);
            actionChainText.rectTransform.offsetMax = new Vector2(-8f, 0f);
        }

        private void AddActionChainStepChip(Transform parent, string label, float anchorX, Color32 color)
        {
            var chip = CreatePanel(parent, "Action Chain Step " + label, new Vector4(anchorX, 0.5f, anchorX, 0.5f), new Vector2(8f, -10f), new Vector2(30f, 10f));
            var image = chip.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            chip.AddComponent<LayoutElement>().ignoreLayout = true;
            var text = CreateText(chip.transform, "Label", label, 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = new Color32(28, 64, 50, 255);
            Stretch(text.rectTransform);
        }

        private void RefreshActionChainStrip(CityMetrics metrics)
        {
            if (actionChainText == null && actionChainPressureFill == null && actionChainStripImage == null)
            {
                return;
            }

            var mode = controller != null ? controller.OverlayMode : OverlayMode.Normal;
            var layer = MiniMapFocusOverlayMode(mode, metrics);
            var pressure = OverlayPressureScore(layer, metrics);
            var chain = BuildLayerToolActionChain(metrics, mode, layer);
            var primary = metrics != null ? MiniMapPrimaryIssueLabel(metrics) : "--";
            var prefix = layer != mode && layer != OverlayMode.Normal ? "\u8350" : "\u770b";
            var status = MiniMapUrgencyTag(metrics, lastMiniMapSevereSamples, lastMiniMapWarningSamples);
            var orderSuffix = BuildActionChainOrderSuffix(metrics);
            var text = prefix + OverlayLabel(layer)
                + " \u538b" + pressure
                + " \u4e3b" + CompactCardText(primary, 5)
                + " \u70ed" + lastMiniMapSevereSamples + "/" + lastMiniMapWarningSamples
                + "  " + status + ">" + CompactCardText(chain, 24) + orderSuffix;

            if (actionChainText != null)
            {
                actionChainText.text = text;
                actionChainText.color = pressure >= 70 ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 248);
            }

            if (actionChainPressureFill != null)
            {
                actionChainPressureFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(8, pressure) / 100f), 1f);
                actionChainPressureFill.color = OverlayPressureFillColor(layer, pressure, false);
            }

            if (actionChainStripImage != null)
            {
                actionChainStripImage.color = pressure >= 70
                    ? new Color32(64, 48, 36, 228)
                    : new Color32(18, 54, 42, 218);
            }
        }

        private static string BuildActionChainOrderSuffix(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return ">\u63a5\u5355";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return ">\u9886\u5956";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                var progress = Mathf.Clamp(metrics.ActiveObjective.Progress, 0, metrics.ActiveObjective.Required);
                return ">\u8ba2\u5355" + progress + "/" + metrics.ActiveObjective.Required;
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return ">\u5347+" + metrics.BuildingUpgradeReadyCount;
            }

            if (metrics.ServiceGapPressure >= 55)
            {
                return ">\u8865\u670d\u52a1";
            }

            return ">\u63a5\u5355";
        }

        private void BuildCitySnapshotBoard(Transform root)
        {
            // CITY_SKYLINES_OPERATIONS_SNAPSHOT adds a compact live city health board without changing tool counts.
            var board = CreatePanel(root, "City Snapshot Board", AnchorBottomLeft(), new Vector2(12f, 232f), new Vector2(154f, 348f));
            citySnapshotPanelImage = board.GetComponent<Image>();
            citySnapshotPanelImage.color = new Color32(19, 58, 42, 222);
            citySnapshotPanelImage.raycastTarget = false;
            AddSoftCardShadow(board, 38);
            AddPanelTopAccent(board, new Color32(96, 214, 118, 152), 3f);
            var outline = board.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 96);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            citySnapshotTitleText = CreateText(board.transform, "Snapshot Title", "\u57ce\u5e02\u5feb\u7167", 10, FontStyle.Bold, TextAnchor.UpperLeft);
            citySnapshotTitleText.color = new Color32(245, 255, 238, 246);
            citySnapshotTitleText.raycastTarget = false;
            citySnapshotTitleText.lineSpacing = 0.86f;
            citySnapshotTitleText.resizeTextForBestFit = true;
            citySnapshotTitleText.resizeTextMinSize = 7;
            citySnapshotTitleText.resizeTextMaxSize = 10;
            Stretch(citySnapshotTitleText.rectTransform);
            citySnapshotTitleText.rectTransform.offsetMin = new Vector2(9f, 82f);
            citySnapshotTitleText.rectTransform.offsetMax = new Vector2(-8f, -4f);

            AddCitySnapshotMetric(board.transform, 0, "\u4eba", new Color32(206, 238, 216, 218));
            AddCitySnapshotMetric(board.transform, 1, "\u8d22", new Color32(255, 207, 86, 218));
            AddCitySnapshotMetric(board.transform, 2, "\u8def", new Color32(65, 184, 220, 218));
            AddCitySnapshotMetric(board.transform, 3, "\u670d", new Color32(96, 214, 118, 218));
            AddHudFacet(board.transform, "Snapshot Glass Fold", new Vector4(0.62f, 0.58f, 0.98f, 0.88f), Vector2.zero, Vector2.zero, new Color32(245, 255, 238, 34), -7f);
        }

        private void AddCitySnapshotMetric(Transform parent, int index, string glyph, Color32 accent)
        {
            var col = index % 2;
            var row = index / 2;
            var minX = 9f + col * 63f;
            var minY = 8f + (1 - row) * 36f;
            var card = CreatePanel(parent, "Snapshot Metric " + index, AnchorBottomLeft(), new Vector2(minX, minY), new Vector2(minX + 58f, minY + 30f));
            var image = card.GetComponent<Image>();
            image.color = new Color32(245, 255, 238, 30);
            image.raycastTarget = false;
            var fill = CreateToolButtonAccent(card.transform, "Snapshot Fill", AnchorBottom(), new Vector2(3f, 3f), new Vector2(-3f, 7f), accent);
            fill.raycastTarget = false;
            citySnapshotMetricFills.Add(fill);

            var glyphChip = CreateToolButtonAccent(card.transform, "Snapshot Glyph", AnchorLeft(), new Vector2(4f, 6f), new Vector2(20f, -6f), accent);
            glyphChip.raycastTarget = false;
            var glyphText = CreateText(glyphChip.transform, "Glyph", glyph, 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyphText.color = new Color32(43, 64, 70, 255);
            glyphText.raycastTarget = false;
            Stretch(glyphText.rectTransform);

            var label = CreateText(card.transform, "Value", "--", 8, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.color = new Color32(245, 255, 238, 246);
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 6;
            label.resizeTextMaxSize = 8;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(23f, 4f);
            label.rectTransform.offsetMax = new Vector2(-4f, -3f);
            citySnapshotMetricTexts.Add(label);
        }

        private void RefreshCitySnapshotBoard(CityMetrics metrics)
        {
            if (citySnapshotMetricTexts.Count < 4 || citySnapshotMetricFills.Count < 4)
            {
                return;
            }

            var populationScore = metrics == null || metrics.HousingCapacity <= 0
                ? 0
                : Mathf.Clamp(Mathf.RoundToInt(metrics.Population * 100f / Mathf.Max(1, metrics.HousingCapacity)), 0, 130);
            var cashScore = metrics == null ? 0 : Mathf.Clamp(50 + metrics.NetIncome / 18, 0, 100);
            var roadScore = metrics == null ? 0 : Mathf.Clamp(metrics.RoadConnectivity - metrics.RoadBottleneckPressure / 3, 0, 100);
            var serviceScore = metrics == null ? 0 : Mathf.Clamp(Mathf.Min(metrics.ServiceCoverage, 100 - metrics.ServiceGapPressure / 2), 0, 100);

            SetCitySnapshotMetric(0, "\u4eba " + (metrics != null ? metrics.Population + "/" + metrics.HousingCapacity : "--"), populationScore, populationScore > 100);
            SetCitySnapshotMetric(1, "\u8d22 " + (metrics != null ? FormatSigned(metrics.NetIncome) : "--"), cashScore, metrics != null && metrics.NetIncome < 0);
            SetCitySnapshotMetric(2, "\u8def " + (metrics != null ? metrics.RoadConnectivity.ToString() : "--"), roadScore, roadScore < 45);
            SetCitySnapshotMetric(3, "\u670d " + (metrics != null ? metrics.ServiceCoverage.ToString() : "--"), serviceScore, serviceScore < 45);

            var risk = metrics != null ? Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.RoadBottleneckPressure, metrics.ServiceGapPressure)) : 0;
            if (citySnapshotTitleText != null)
            {
                citySnapshotTitleText.text = "\u5feb\u7167 \u8bc4" + (metrics != null ? metrics.CityScore.ToString() : "--")
                    + "\n\u4e0b:" + BuildCitySnapshotActionText(metrics);
                citySnapshotTitleText.color = risk >= 70 ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 246);
            }

            if (citySnapshotPanelImage != null)
            {
                citySnapshotPanelImage.color = risk >= 70 ? new Color32(64, 48, 36, 228) : new Color32(19, 58, 42, 222);
            }
        }

        private void SetCitySnapshotMetric(int index, string text, int score, bool warning)
        {
            if (index < citySnapshotMetricTexts.Count && citySnapshotMetricTexts[index] != null)
            {
                citySnapshotMetricTexts[index].text = CompactCardText(text, 9);
                citySnapshotMetricTexts[index].color = warning ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 246);
            }

            if (index < citySnapshotMetricFills.Count && citySnapshotMetricFills[index] != null)
            {
                citySnapshotMetricFills[index].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(6, score) / 100f), 1f);
                citySnapshotMetricFills[index].color = warning ? new Color32(255, 188, 66, 226) : CitySnapshotScoreColor(score);
            }
        }

        private static Color32 CitySnapshotScoreColor(int score)
        {
            if (score < 45) return new Color32(244, 139, 124, 222);
            if (score < 70) return new Color32(255, 207, 86, 222);
            return new Color32(96, 214, 118, 222);
        }

        private static string BuildCitySnapshotActionText(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "--";
            }

            if (metrics.ForecastRisk >= 70)
            {
                return CompactCardText(metrics.ForecastAction, 8);
            }

            if (metrics.NetIncome < 0 || metrics.BudgetStress >= 65)
            {
                return "\u8c03\u9884\u7b97";
            }

            if (metrics.ServiceGapPressure >= 55 || metrics.ServiceCoverage < 45)
            {
                return "\u8865" + AdvisorServiceLabel(metrics);
            }

            if (metrics.RoadBottleneckPressure >= 55 || metrics.RoadConnectivity < 55)
            {
                return "\u5347\u7ea7\u9053\u8def";
            }

            if (metrics.DemandUrgency >= 55)
            {
                return "\u8865\u5206\u533a";
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7\u5efa\u7b51";
            }

            return "\u7a33\u6b65\u6269\u5efa";
        }

        private void BuildResourceObjectiveProgressBar(Transform parent)
        {
            // REFERENCE_IMAGE_RESOURCE_OBJECTIVE_PROGRESS mirrors the left-card progress strip in the reference UI.
            var bar = CreatePanel(parent, "Resource Objective Progress", AnchorFree(), Vector2.zero, Vector2.zero);
            bar.GetComponent<Image>().color = new Color32(94, 128, 86, 150);
            bar.AddComponent<LayoutElement>().preferredHeight = 24f;

            var fillObject = new GameObject("Resource Objective Fill");
            fillObject.transform.SetParent(bar.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            resourceObjectiveProgressFill = fillObject.AddComponent<Image>();
            resourceObjectiveProgressFill.color = new Color32(65, 169, 184, 230);

            resourceObjectiveProgressText = CreateText(bar.transform, "Resource Objective Text", "\u76ee\u6807 --", 12, FontStyle.Bold, TextAnchor.MiddleLeft);
            resourceObjectiveProgressText.color = new Color32(245, 255, 238, 255);
            Stretch(resourceObjectiveProgressText.rectTransform);
            resourceObjectiveProgressText.rectTransform.offsetMin = new Vector2(8f, 0f);
            resourceObjectiveProgressText.rectTransform.offsetMax = new Vector2(-8f, 0f);
        }

        private void RefreshResourceObjectiveProgress(CityHudSnapshot snapshot)
        {
            if (resourceObjectiveProgressFill == null || resourceObjectiveProgressText == null)
            {
                return;
            }

            var required = Mathf.Max(1, snapshot.ObjectiveRequired);
            var progress = Mathf.Clamp(snapshot.ObjectiveProgress, 0, required);
            var amount = progress / (float)required;
            resourceObjectiveProgressFill.rectTransform.anchorMax = new Vector2(amount, 1f);
            resourceObjectiveProgressFill.color = objectivePulseTimer > 0f
                ? (snapshot.ObjectiveDone ? new Color32(88, 204, 96, 255) : new Color32(255, 202, 70, 255))
                : snapshot.ObjectiveDone
                ? new Color32(88, 204, 96, 235)
                : amount >= 0.7f
                    ? new Color32(255, 202, 70, 235)
                    : new Color32(65, 169, 184, 230);
            var title = string.IsNullOrEmpty(snapshot.ObjectiveTitle) ? "\u5f53\u524d\u76ee\u6807" : CompactCardText(snapshot.ObjectiveTitle, 9);
            resourceObjectiveProgressText.text = title + "  " + progress + "/" + required + ObjectivePulseInlineText();
        }

        private void RefreshObjectivePulseState(CityHudSnapshot snapshot)
        {
            // CITY_SKYLINES_OBJECTIVE_PROGRESS_PULSE gives milestone progress immediate HUD feedback.
            var title = snapshot != null ? (snapshot.ObjectiveTitle ?? string.Empty) : string.Empty;
            var required = snapshot != null ? Mathf.Max(1, snapshot.ObjectiveRequired) : 1;
            var progress = snapshot != null ? Mathf.Clamp(snapshot.ObjectiveProgress, 0, required) : 0;
            var done = snapshot != null && snapshot.ObjectiveDone;
            if (!objectivePulsePrimed)
            {
                objectivePulsePrimed = true;
                lastObjectiveTitle = title;
                lastObjectiveRequired = required;
                lastObjectiveProgress = progress;
                lastObjectiveDone = done;
                return;
            }

            var sameObjective = title == lastObjectiveTitle && required == lastObjectiveRequired;
            if (done && !lastObjectiveDone)
            {
                objectivePulseTimer = 1.4f;
                objectivePulseText = "\u5df2\u5b8c\u6210";
            }
            else if (sameObjective && progress > lastObjectiveProgress)
            {
                objectivePulseTimer = 1.05f;
                objectivePulseText = "\u76ee\u6807+" + (progress - lastObjectiveProgress);
            }

            lastObjectiveTitle = title;
            lastObjectiveRequired = required;
            lastObjectiveProgress = progress;
            lastObjectiveDone = done;
        }

        private string ObjectivePulseInlineText()
        {
            if (objectivePulseTimer <= 0f || string.IsNullOrEmpty(objectivePulseText))
            {
                return string.Empty;
            }

            return "  " + objectivePulseText;
        }

        private string ObjectivePulseCardLine()
        {
            if (objectivePulseTimer <= 0f || string.IsNullOrEmpty(objectivePulseText))
            {
                return string.Empty;
            }

            return "\n\u8fdb\u5ea6 " + objectivePulseText;
        }

        private static Color32 ResourceLevelBadgeColor(CityHudSnapshot snapshot)
        {
            // REFERENCE_IMAGE_RESOURCE_LEVEL_PROGRESS_TINT makes the level medallion react to objective progress.
            if (snapshot == null)
            {
                return new Color32(79, 113, 108, 236);
            }

            var required = Mathf.Max(1, snapshot.ObjectiveRequired);
            var amount = Mathf.Clamp01(snapshot.ObjectiveProgress / (float)required);
            if (snapshot.ObjectiveDone || amount >= 1f)
            {
                return new Color32(255, 202, 70, 245);
            }

            if (amount >= 0.68f)
            {
                return new Color32(96, 190, 122, 242);
            }

            return new Color32(79, 113, 108, 236);
        }

        private void BuildMiniMapPanel(Transform root)
        {
            // LOW_POLY_ISOMETRIC_REFERENCE_UI adds the compact minimap/zoom cluster from the reference layout.
            var miniMap = CreatePanel(root, "Mini Map Zoom", AnchorBottomRight(), new Vector2(-282f, 8f), new Vector2(-12f, 126f));
            miniMap.GetComponent<Image>().color = new Color32(19, 62, 45, 232);
            AddSoftCardShadow(miniMap, 48);
            AddPanelTopAccent(miniMap, new Color32(65, 183, 190, 174), 3f);
            var miniMapOutline = miniMap.AddComponent<Outline>();
            miniMapOutline.effectColor = new Color32(54, 153, 142, 118);
            miniMapOutline.effectDistance = new Vector2(1.6f, -1.6f);
            var layout = miniMap.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 7, 6);
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var body = CreatePanel(miniMap.transform, "Mini Map Body", AnchorFree(), Vector2.zero, Vector2.zero);
            body.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            body.AddComponent<LayoutElement>().preferredHeight = 76f;
            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 6;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            var mapPreview = CreatePanel(body.transform, "Mini Map Preview", AnchorFree(), Vector2.zero, Vector2.zero);
            mapPreview.GetComponent<Image>().color = new Color32(139, 214, 154, 242);
            mapPreview.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddMiniMapBackdropFacets(mapPreview.transform);
            BuildMiniMapCells(mapPreview.transform);
            BuildMiniMapCameraStatusOverlay(mapPreview.transform);

            var controls = CreatePanel(body.transform, "Mini Map Controls", AnchorFree(), Vector2.zero, Vector2.zero);
            controls.GetComponent<Image>().color = new Color32(0, 0, 0, 0);
            var controlsLayout = controls.AddComponent<LayoutElement>();
            controlsLayout.preferredWidth = 34f;
            controlsLayout.flexibleWidth = 0f;
            var controlLayout = controls.AddComponent<VerticalLayoutGroup>();
            controlLayout.spacing = 5;
            controlLayout.childForceExpandWidth = true;
            controlLayout.childForceExpandHeight = true;
            AddMiniMapControlButton(controls.transform, "+", () => { if (cameraController != null) cameraController.ZoomIn(); });
            AddMiniMapControlButton(controls.transform, "0", () => { if (cameraController != null) cameraController.FrameMap(); });
            AddMiniMapControlButton(controls.transform, "-", () => { if (cameraController != null) cameraController.ZoomOut(); });
            miniMapRiskSummaryText = CreateText(miniMap.transform, "Mini Map Risk Summary", "\u70ed\u533a \u4e25 0 \u6ce8 0", 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            miniMapRiskSummaryText.color = new Color32(245, 255, 238, 245);
            miniMapRiskSummaryText.lineSpacing = 0.86f;
            miniMapRiskSummaryText.GetComponent<LayoutElement>().preferredHeight = 24f;
        }

        private void BuildMiniMapCameraStatusOverlay(Transform parent)
        {
            // CITY_SKYLINES_CAMERA_STATUS_MINIMAP makes smooth camera motion visible inside the overview.
            var overlay = CreatePanel(parent, "Mini Map Camera Status", new Vector4(0f, 0f, 1f, 0f), new Vector2(5f, 5f), new Vector2(-5f, 19f));
            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = new Color32(18, 54, 42, 202);
            overlayImage.raycastTarget = false;
            overlay.AddComponent<LayoutElement>().ignoreLayout = true;
            overlay.transform.SetAsLastSibling();

            var fill = CreatePanel(overlay.transform, "Camera Zoom Fill", AnchorStretch(), new Vector2(2f, 3f), new Vector2(-72f, -3f));
            miniMapCameraZoomFill = fill.GetComponent<Image>();
            miniMapCameraZoomFill.color = new Color32(65, 183, 190, 168);
            miniMapCameraZoomFill.raycastTarget = false;
            fill.AddComponent<LayoutElement>().ignoreLayout = true;

            miniMapCameraStatusText = CreateText(overlay.transform, "Camera Status Text", "\u955c\u5934 --", 8, FontStyle.Bold, TextAnchor.MiddleRight);
            miniMapCameraStatusText.color = new Color32(245, 255, 238, 242);
            miniMapCameraStatusText.raycastTarget = false;
            Stretch(miniMapCameraStatusText.rectTransform);
            miniMapCameraStatusText.rectTransform.offsetMin = new Vector2(4f, 0f);
            miniMapCameraStatusText.rectTransform.offsetMax = new Vector2(-4f, 0f);
            miniMapCameraStatusText.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        private void BuildMiniMapCells(Transform parent)
        {
            // DYNAMIC_MINIMAP_SAMPLER turns the reference minimap into a live city overview.
            miniMapCells.Clear();
            miniMapCellFacets.Clear();
            miniMapCellOutlines.Clear();
            var grid = parent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(13.2f, 8f);
            grid.spacing = new Vector2(1f, 1f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = MiniMapColumns;
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (var i = 0; i < MiniMapColumns * MiniMapRows; i += 1)
            {
                var cell = new GameObject("MiniMapCell" + i);
                cell.transform.SetParent(parent, false);
                var image = cell.AddComponent<Image>();
                image.color = new Color32(162, 226, 148, 255);
                var outline = cell.AddComponent<Outline>();
                outline.effectColor = new Color32(245, 255, 238, 54);
                outline.effectDistance = new Vector2(0.85f, -0.85f);
                outline.enabled = true;

                var facet = new GameObject("MiniMap Isometric Facet");
                facet.transform.SetParent(cell.transform, false);
                var facetRect = facet.AddComponent<RectTransform>();
                facetRect.anchorMin = new Vector2(0.14f, 0.18f);
                facetRect.anchorMax = new Vector2(0.9f, 0.62f);
                facetRect.offsetMin = Vector2.zero;
                facetRect.offsetMax = Vector2.zero;
                facetRect.localRotation = Quaternion.Euler(0f, 0f, -12f);
                var facetImage = facet.AddComponent<Image>();
                facetImage.color = new Color32(245, 255, 238, 44);
                facetImage.raycastTarget = false;
                facet.AddComponent<LayoutElement>().ignoreLayout = true;
                miniMapCells.Add(image);
                miniMapCellFacets.Add(facetImage);
                miniMapCellOutlines.Add(outline);
            }

            BuildMiniMapViewportFrame(parent);
        }

        private void AddMiniMapBackdropFacets(Transform parent)
        {
            // REFERENCE_IMAGE_MINIMAP_ISOMETRIC_BACKDROP gives the minimap a faint city model under live cells.
            AddMiniMapBackdropFacet(parent, "MiniMap Backdrop Water", new Vector4(0.02f, 0.1f, 0.5f, 0.42f), new Color32(86, 197, 224, 92));
            AddMiniMapBackdropRotatedFacet(parent, "MiniMap Backdrop River Shine", new Vector4(0.06f, 0.2f, 0.48f, 0.24f), new Color32(235, 255, 255, 84), -14f);
            AddMiniMapBackdropFacet(parent, "MiniMap Backdrop Road X", new Vector4(0.1f, 0.46f, 0.9f, 0.54f), new Color32(252, 244, 190, 70));
            AddMiniMapBackdropFacet(parent, "MiniMap Backdrop Road Y", new Vector4(0.46f, 0.14f, 0.54f, 0.88f), new Color32(252, 244, 190, 62));
            AddMiniMapBackdropRotatedFacet(parent, "MiniMap Backdrop Boulevard A", new Vector4(0.12f, 0.5f, 0.86f, 0.55f), new Color32(43, 64, 70, 54), -18f);
            AddMiniMapBackdropRotatedFacet(parent, "MiniMap Backdrop Boulevard B", new Vector4(0.33f, 0.16f, 0.82f, 0.2f), new Color32(43, 64, 70, 44), 18f);
            AddMiniMapBackdropFacet(parent, "MiniMap Backdrop District", new Vector4(0.56f, 0.42f, 0.92f, 0.86f), new Color32(255, 207, 86, 56));
            AddMiniMapBackdropFacet(parent, "MiniMap Backdrop Park Patch", new Vector4(0.14f, 0.62f, 0.36f, 0.88f), new Color32(96, 214, 118, 58));
            AddMiniMapBackdropFacet(parent, "MiniMap Backdrop Core Patch", new Vector4(0.58f, 0.18f, 0.82f, 0.36f), new Color32(245, 255, 238, 48));
        }

        private void AddMiniMapBackdropFacet(Transform parent, string name, Vector4 anchors, Color32 color)
        {
            var facet = new GameObject(name);
            facet.transform.SetParent(parent, false);
            var rect = facet.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchors.x, anchors.y);
            rect.anchorMax = new Vector2(anchors.z, anchors.w);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = facet.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var layout = facet.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            facet.transform.SetAsFirstSibling();
        }

        private void AddMiniMapBackdropRotatedFacet(Transform parent, string name, Vector4 anchors, Color32 color, float rotation)
        {
            var facet = new GameObject(name);
            facet.transform.SetParent(parent, false);
            var rect = facet.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchors.x, anchors.y);
            rect.anchorMax = new Vector2(anchors.z, anchors.w);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = facet.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var layout = facet.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            facet.transform.SetAsFirstSibling();
        }

        private void BuildMiniMapViewportFrame(Transform parent)
        {
            // REFERENCE_IMAGE_MINIMAP_VIEWPORT_FRAME mirrors the white camera window in the reference minimap.
            var frame = new GameObject("MiniMapViewportFrame");
            frame.transform.SetParent(parent, false);
            miniMapViewportFrame = frame.AddComponent<RectTransform>();
            miniMapViewportFrame.anchorMin = new Vector2(0f, 1f);
            miniMapViewportFrame.anchorMax = new Vector2(0f, 1f);
            miniMapViewportFrame.pivot = new Vector2(0.5f, 0.5f);
            miniMapViewportFrame.sizeDelta = new Vector2(42f, 18f);
            miniMapViewportFrame.anchoredPosition = new Vector2(72f, -24f);
            var image = frame.AddComponent<Image>();
            image.color = new Color32(214, 247, 255, 36);
            image.raycastTarget = false;
            miniMapViewportFrameImage = image;
            var outline = frame.AddComponent<Outline>();
            outline.effectColor = new Color32(232, 255, 255, 255);
            outline.effectDistance = new Vector2(2.3f, -2.3f);
            miniMapViewportFrameOutline = outline;
            var layout = frame.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private void AddMiniMapControlButton(Transform parent, string labelText, UnityAction action)
        {
            var obj = new GameObject("MiniMapButton " + labelText);
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = MiniMapControlColor(labelText);
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = labelText == "+" ? new Color32(245, 255, 238, 166) : new Color32(245, 255, 238, 108);
            outline.effectDistance = new Vector2(1.35f, -1.35f);
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            var displayText = labelText == "0" ? "\u4e2d" : labelText;
            var label = CreateText(obj.transform, "Label", displayText, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = new Color32(245, 255, 238, 255);
            Stretch(label.rectTransform);
        }

        private static Color32 MiniMapControlColor(string labelText)
        {
            // REFERENCE_IMAGE_MINIMAP_ZOOM_BUTTONS gives the zoom cluster the mockup's compact control feel.
            if (labelText == "+") return new Color32(96, 198, 92, 248);
            if (labelText == "0") return new Color32(54, 153, 142, 242);
            return new Color32(43, 132, 112, 242);
        }

        private void AddOverlayButton(Transform parent, OverlayMode mode, string labelText)
        {
            var obj = new GameObject(mode.ToString());
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = new Color32(245, 255, 238, 30);
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color32(54, 153, 142, 104);
            outline.effectDistance = new Vector2(1.15f, -1.15f);
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(() => { if (controller != null) controller.SetOverlay(mode); });
            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = 30f;
            layout.preferredWidth = 74f;

            var swatch = AddOverlayModeSwatch(obj.transform, mode);
            var pressureFill = AddOverlayPressureMeter(obj.transform, mode);
            var stateRail = AddOverlayStateRail(obj.transform);
            var recommendationBadge = AddOverlayRecommendationBadge(obj.transform);
            var divider = CreateToolButtonAccent(obj.transform, "Overlay List Divider", AnchorBottom(), new Vector2(39f, 1.5f), new Vector2(-7f, 2.8f), new Color32(245, 255, 238, 34));
            divider.raycastTarget = false;
            var spine = CreateToolButtonAccent(obj.transform, "Overlay List Spine", AnchorLeft(), new Vector2(2f, 5f), new Vector2(4f, -5f), new Color32(65, 183, 190, 52));
            spine.raycastTarget = false;
            var label = CreateText(obj.transform, mode.ToString(), labelText, 13, FontStyle.Bold, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(39f, 0f);
            label.rectTransform.offsetMax = new Vector2(-4f, -5f);
            overlaySwatches.Add(swatch);
            overlayPressureFills.Add(pressureFill);
            overlayStateRails.Add(stateRail);
            overlayRecommendationBadges.Add(recommendationBadge);
            overlaySwatchModes.Add(mode);
            overlayButtons.Add(new OverlayButtonBinding
            {
                Button = button,
                Label = label,
                Mode = mode
            });
        }

        private Image AddOverlayStateRail(Transform parent)
        {
            // REFERENCE_IMAGE_LAYER_SELECTED_RAIL gives the right-side layer stack a strong selected state.
            var rail = new GameObject("Overlay State Rail");
            rail.transform.SetParent(parent, false);
            var rect = rail.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(-5f, 4f);
            rect.offsetMax = new Vector2(-1f, -4f);
            var image = rail.AddComponent<Image>();
            image.color = new Color32(65, 169, 184, 0);
            image.raycastTarget = false;
            var layout = rail.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private Image AddOverlayRecommendationBadge(Transform parent)
        {
            // REFERENCE_IMAGE_LAYER_RECOMMENDATION_CORNER marks the recommended info layer without adding buttons.
            var badge = new GameObject("Overlay Recommendation Badge");
            badge.transform.SetParent(parent, false);
            var rect = badge.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(12f, 12f);
            rect.anchoredPosition = new Vector2(-3f, -3f);
            var image = badge.AddComponent<Image>();
            image.color = new Color32(255, 202, 70, 0);
            image.raycastTarget = false;
            var glyph = CreateText(badge.transform, "Glyph", "\u8350", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(83, 68, 30, 0);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);
            overlayRecommendationBadgeGlyphs.Add(glyph);
            var layout = badge.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private Image AddOverlayPressureMeter(Transform parent, OverlayMode mode)
        {
            // CITY_SKYLINES_LAYER_PRESSURE_METER turns the vertical layer stack into live information buttons.
            var track = new GameObject("Overlay Pressure Track");
            track.transform.SetParent(parent, false);
            var trackRect = track.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.offsetMin = new Vector2(39f, 4f);
            trackRect.offsetMax = new Vector2(-7f, 10f);
            var trackImage = track.AddComponent<Image>();
            trackImage.color = new Color32(54, 153, 142, 42);
            trackImage.raycastTarget = false;
            var trackLayout = track.AddComponent<LayoutElement>();
            trackLayout.ignoreLayout = true;

            var fill = new GameObject("Overlay Pressure Fill");
            fill.transform.SetParent(track.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.18f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = OverlayModeAccentColor(mode);
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private Image AddOverlayModeSwatch(Transform parent, OverlayMode mode)
        {
            // REFERENCE_IMAGE_VERTICAL_LAYER_ICONS gives the right toolbar readable icon color chips.
            var swatch = new GameObject("Overlay Swatch");
            swatch.transform.SetParent(parent, false);
            var rect = swatch.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(27f, 27f);
            rect.anchoredPosition = new Vector2(19f, 0f);
            var image = swatch.AddComponent<Image>();
            image.color = OverlayModeAccentColor(mode);
            image.raycastTarget = false;
            var outline = swatch.AddComponent<Outline>();
            outline.effectColor = new Color32(245, 255, 238, 138);
            outline.effectDistance = new Vector2(1.15f, -1.15f);
            var glyph = CreateText(swatch.transform, "Glyph", OverlayModeGlyph(mode), 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(245, 255, 238, 255);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);
            var layout = swatch.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private static string OverlayModeGlyph(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic) return "\u8def";
            if (mode == OverlayMode.Pollution) return "\u6c61";
            if (mode == OverlayMode.Zoning) return "\u533a";
            if (mode == OverlayMode.Services) return "\u670d";
            if (mode == OverlayMode.Transit) return "\u516c";
            if (mode == OverlayMode.LandValue) return "\u4ef7";
            if (mode == OverlayMode.Waste) return "\u56de";
            if (mode == OverlayMode.Logistics) return "\u8d27";
            if (mode == OverlayMode.Utilities) return "\u7535";
            if (mode == OverlayMode.Communications) return "\u4fe1";
            if (mode == OverlayMode.RoadSafety) return "\u5b89";
            if (mode == OverlayMode.Parking) return "P";
            if (mode == OverlayMode.Stormwater) return "\u96e8";
            return "\u89c6";
        }

        private static Color32 OverlayModeAccentColor(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic) return new Color32(244, 173, 66, 255);
            if (mode == OverlayMode.Pollution) return new Color32(169, 112, 190, 255);
            if (mode == OverlayMode.Zoning) return new Color32(96, 190, 122, 255);
            if (mode == OverlayMode.Services) return new Color32(244, 139, 124, 255);
            if (mode == OverlayMode.Transit) return new Color32(86, 139, 210, 255);
            if (mode == OverlayMode.LandValue) return new Color32(255, 202, 70, 255);
            if (mode == OverlayMode.Waste) return new Color32(82, 174, 144, 255);
            if (mode == OverlayMode.Logistics) return new Color32(222, 158, 86, 255);
            if (mode == OverlayMode.Utilities) return new Color32(82, 174, 186, 255);
            if (mode == OverlayMode.Communications) return new Color32(112, 192, 214, 255);
            if (mode == OverlayMode.RoadSafety) return new Color32(224, 106, 82, 255);
            if (mode == OverlayMode.Parking) return new Color32(168, 150, 118, 255);
            if (mode == OverlayMode.Stormwater) return new Color32(65, 169, 184, 255);
            return new Color32(206, 238, 216, 255);
        }

        private void AddToolDockCategoryBands(Transform parent)
        {
            // REFERENCE_IMAGE_DOCK_CATEGORY_BANDS separates roads, zoning, services and utilities without adding controls.
            AddToolDockCategoryBand(parent, "Dock Band Roads", new Vector4(0.01f, 0.56f, 0.2f, 0.96f), new Color32(54, 153, 142, 28), "\u9053\u8def");
            AddToolDockCategoryBand(parent, "Dock Band Zones", new Vector4(0.2f, 0.56f, 0.44f, 0.96f), new Color32(96, 202, 126, 34), "\u5206\u533a");
            AddToolDockCategoryBand(parent, "Dock Band Services", new Vector4(0.44f, 0.08f, 0.76f, 0.96f), new Color32(86, 150, 220, 30), "\u670d\u52a1");
            AddToolDockCategoryBand(parent, "Dock Band Utilities", new Vector4(0.76f, 0.08f, 0.99f, 0.52f), new Color32(255, 207, 86, 34), "\u8fd0\u8425");
        }

        private void AddToolDockCategoryBand(Transform parent, string name, Vector4 anchors, Color32 color, string labelText)
        {
            var band = CreatePanel(parent, name, anchors, Vector2.zero, Vector2.zero);
            var image = band.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var label = CreateText(band.transform, "Band Label", labelText, 8, FontStyle.Bold, TextAnchor.UpperLeft);
            label.color = new Color32(31, 86, 70, 166);
            label.raycastTarget = false;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(5f, 3f);
            label.rectTransform.offsetMax = new Vector2(-5f, -3f);
            var layout = band.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            band.transform.SetAsFirstSibling();
        }

        private void AddToolButton(Transform parent, string labelText, UnityAction action, CityToolMode mode, ZoneType zone, string buildingId)
        {
            var obj = new GameObject("Tool " + labelText);
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = new Color32(245, 255, 238, 34);
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color32(36, 116, 112, 0);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.enabled = false;
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            var toolAccent = ToolAccentColor(mode, zone, buildingId);
            var selectionGlow = CreateToolButtonAccent(obj.transform, "Selection Glow", AnchorStretch(), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Color32(0, 0, 0, 0));
            selectionGlow.raycastTarget = false;
            selectionGlow.transform.SetAsFirstSibling();
            var categoryRail = CreateToolButtonAccent(obj.transform, "Category Rail", AnchorLeft(), new Vector2(1f, 2f), new Vector2(4f, -2f), toolAccent);
            categoryRail.raycastTarget = false;
            var accent = CreateToolButtonAccent(obj.transform, "Accent", AnchorTop(), new Vector2(0f, -4f), Vector2.zero, toolAccent);
            var icon = CreateToolButtonAccent(obj.transform, "Icon Swatch", AnchorTopLeft(), new Vector2(5f, -18f), new Vector2(24f, -3f), toolAccent);
            AddToolDockMicroModel(icon.transform, mode, zone, buildingId, toolAccent);
            var glyph = AddToolIconGlyph(icon.transform, mode, zone, buildingId);
            var stateBadge = CreateToolButtonAccent(obj.transform, "State Badge", AnchorTopRight(), new Vector2(-18f, -17f), new Vector2(-3f, -3f), new Color32(0, 0, 0, 0));
            var stateBadgeText = CreateText(stateBadge.transform, "State", string.Empty, 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            stateBadgeText.color = new Color32(83, 68, 30, 0);
            stateBadgeText.raycastTarget = false;
            Stretch(stateBadgeText.rectTransform);
            AddDockButtonFacets(obj.transform, toolAccent);
            var label = CreateText(obj.transform, "Label", ToolButtonLabelText(labelText, mode, zone, buildingId), 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(20f, 2f);
            label.rectTransform.offsetMax = new Vector2(-3f, -4f);
            var meta = CreateText(obj.transform, "Meta", ToolButtonMetaText(mode, zone, buildingId), 8, FontStyle.Bold, TextAnchor.LowerRight);
            meta.color = new Color32(216, 244, 220, 225);
            Stretch(meta.rectTransform);
            meta.rectTransform.offsetMin = new Vector2(3f, 0f);
            meta.rectTransform.offsetMax = new Vector2(-4f, -1f);
            toolButtons.Add(new ToolButtonBinding
            {
                Button = button,
                Accent = accent,
                IconSwatch = icon,
                SelectionGlow = selectionGlow,
                StateBadge = stateBadge,
                Label = label,
                IconGlyph = glyph,
                MetaLabel = meta,
                StateBadgeText = stateBadgeText,
                Outline = outline,
                ToolMode = mode,
                Zone = zone,
                BuildingId = buildingId
            });
        }

        private Text AddToolIconGlyph(Transform parent, CityToolMode mode, ZoneType zone, string buildingId)
        {
            // REFERENCE_IMAGE_BUILD_DOCK_ICON_GLYPHS gives the dense bottom tool strip quick icon-like categories.
            var glyph = CreateText(parent, "Tool Glyph", ToolIconGlyph(mode, zone, buildingId), 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = new Color32(245, 255, 238, 255);
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);
            var layout = glyph.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.ignoreLayout = true;
            }

            return glyph;
        }

        private void AddToolDockMicroModel(Transform parent, CityToolMode mode, ZoneType zone, string buildingId, Color32 accent)
        {
            // CITY_SKYLINES_DOCK_MICRO_MODELS make dense tool buttons read like tiny buildable assets.
            if (mode == CityToolMode.BuildRoad || mode == CityToolMode.UpgradeRoad)
            {
                AddToolDockMicroBlock(parent, "Dock Micro Road", new Color32(56, 72, 76, 190), new Vector4(0.08f, 0.34f, 0.92f, 0.64f), Vector2.zero, Vector2.zero, -12f);
                AddToolDockMicroBlock(parent, "Dock Micro Lane", new Color32(255, 241, 170, 210), new Vector4(0.18f, 0.47f, 0.82f, 0.53f), Vector2.zero, Vector2.zero, -12f);
                return;
            }

            if (mode == CityToolMode.ZonePaint)
            {
                AddToolDockMicroBlock(parent, "Dock Micro Zone Lot", new Color32(accent.r, accent.g, accent.b, 96), new Vector4(0.12f, 0.16f, 0.88f, 0.46f), Vector2.zero, Vector2.zero, -12f);
                AddToolDockMicroBlock(parent, "Dock Micro Zone Edge", new Color32(245, 255, 238, 110), new Vector4(0.18f, 0.48f, 0.84f, 0.56f), Vector2.zero, Vector2.zero, -12f);
                return;
            }

            if (mode == CityToolMode.Demolish)
            {
                AddToolDockMicroBlock(parent, "Dock Micro Demo Base", new Color32(244, 116, 71, 120), new Vector4(0.18f, 0.18f, 0.82f, 0.4f), Vector2.zero, Vector2.zero, -12f);
                AddToolDockMicroBlock(parent, "Dock Micro Demo Slash", new Color32(245, 255, 238, 180), new Vector4(0.24f, 0.2f, 0.76f, 0.3f), Vector2.zero, Vector2.zero, 28f);
                return;
            }

            if (buildingId == "pocket_park" || buildingId == "rain_garden")
            {
                AddToolDockMicroBlock(parent, "Dock Micro Park Grass", new Color32(86, 190, 83, 134), new Vector4(0.14f, 0.14f, 0.86f, 0.42f), Vector2.zero, Vector2.zero, -12f);
                AddToolDockMicroBlock(parent, "Dock Micro Park Tree", new Color32(245, 255, 238, 96), new Vector4(0.34f, 0.42f, 0.66f, 0.76f), Vector2.zero, Vector2.zero, -12f);
                return;
            }

            if (IsUtilityTool(buildingId))
            {
                AddToolDockMicroBlock(parent, "Dock Micro Utility Base", new Color32(245, 255, 238, 112), new Vector4(0.18f, 0.14f, 0.82f, 0.34f), Vector2.zero, Vector2.zero, -12f);
                AddToolDockMicroBlock(parent, "Dock Micro Utility Tower", new Color32(accent.r, accent.g, accent.b, 166), new Vector4(0.38f, 0.34f, 0.62f, 0.78f), Vector2.zero, Vector2.zero, -12f);
                return;
            }

            AddToolDockMicroBlock(parent, "Dock Micro Building Base", new Color32(245, 255, 238, 104), new Vector4(0.18f, 0.12f, 0.82f, 0.32f), Vector2.zero, Vector2.zero, -12f);
            AddToolDockMicroBlock(parent, "Dock Micro Building Body", new Color32(accent.r, accent.g, accent.b, 156), new Vector4(0.28f, 0.3f, 0.72f, 0.78f), Vector2.zero, Vector2.zero, -12f);
            AddToolDockMicroBlock(parent, "Dock Micro Building Roof", new Color32(255, 207, 86, 148), new Vector4(0.22f, 0.7f, 0.78f, 0.88f), Vector2.zero, Vector2.zero, -12f);
        }

        private void AddToolDockMicroBlock(Transform parent, string name, Color32 color, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax, float rotation)
        {
            var block = CreateToolButtonAccent(parent, name, anchors, offsetMin, offsetMax, color);
            block.raycastTarget = false;
            block.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var layout = block.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private static string ToolIconGlyph(CityToolMode mode, ZoneType zone, string buildingId)
        {
            if (mode == CityToolMode.BuildRoad) return "\u8def";
            if (mode == CityToolMode.UpgradeRoad) return "\u5347";
            if (mode == CityToolMode.ZonePaint) return ZoneGlyph(zone);
            if (mode == CityToolMode.Demolish) return "\u62c6";
            if (buildingId == "bus_hub" || buildingId == "metro_station" || buildingId == "intercity_terminal") return "\u516c";
            if (buildingId == "cargo_depot" || buildingId == "distribution_center" || buildingId == "freight_rail_terminal") return "\u8d27";
            if (IsServiceTool(buildingId)) return ServiceToolGlyph(buildingId);
            if (IsUtilityTool(buildingId)) return UtilityToolGlyph(buildingId);
            if (buildingId == "market_corner" || buildingId == "mixed_use_block" || buildingId == "office_studio" || buildingId == "research_campus") return "\u5546";
            if (buildingId == "maker_yard" || buildingId == "resource_processor") return "\u5de5";
            return "\u4f4f";
        }

        private static string ZoneGlyph(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return "\u4f4f";
            if (zone == ZoneType.Commercial) return "\u5546";
            if (zone == ZoneType.MixedUse) return "\u6df7";
            if (zone == ZoneType.Office) return "\u529e";
            if (zone == ZoneType.Industrial) return "\u5de5";
            if (zone == ZoneType.Civic) return "\u670d";
            if (zone == ZoneType.Utility) return "\u7535";
            return "\u533a";
        }

        private static string ServiceToolGlyph(string buildingId)
        {
            if (buildingId == "health_post" || buildingId == "district_hospital") return "\u533b";
            if (buildingId == "primary_school" || buildingId == "community_college") return "\u5b66";
            if (buildingId == "fire_station") return "\u706b";
            if (buildingId == "police_kiosk" || buildingId == "police_precinct") return "\u8b66";
            if (buildingId == "parking_garage") return "P";
            if (buildingId == "telecom_hub" || buildingId == "post_office") return "\u4fe1";
            return "\u670d";
        }

        private static string UtilityToolGlyph(string buildingId)
        {
            if (buildingId == "rain_garden") return "\u96e8";
            if (buildingId == "water_tower" || buildingId == "water_reclaimer") return "\u6c34";
            if (buildingId == "waste_to_energy_plant" || buildingId == "recycling_yard") return "\u56de";
            return "\u7535";
        }

        private Image CreateToolButtonAccent(Transform parent, string name, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax, Color32 color)
        {
            // REFERENCE_IMAGE_TOOL_BUTTON_SWATCHES gives the bottom dock icon/color rhythm without adding buttons.
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchors.x, anchors.y);
            rect.anchorMax = new Vector2(anchors.z, anchors.w);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void AddDockButtonFacets(Transform parent, Color32 accent)
        {
            // REFERENCE_IMAGE_DOCK_BUTTON_FACETS gives dock cards a subtle low-poly surface.
            var highlight = CreateToolButtonAccent(parent, "Facet Highlight", AnchorTopRight(), new Vector2(-24f, -10f), new Vector2(-4f, -2f), new Color32(255, 255, 255, 70));
            highlight.raycastTarget = false;
            highlight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);

            var shade = CreateToolButtonAccent(parent, "Facet Shade", AnchorBottomLeft(), new Vector2(4f, 2f), new Vector2(20f, 8f), new Color32(accent.r, accent.g, accent.b, 58));
            shade.raycastTarget = false;
            shade.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        }

        private void AddControlButton(Transform parent, string labelText, UnityAction action)
        {
            var obj = new GameObject("Control " + labelText);
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = new Color32(255, 206, 72, 235);
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color32(138, 112, 44, 96);
            outline.effectDistance = new Vector2(1f, -1f);
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            var accent = new Color32(255, 202, 70, 245);
            CreateToolButtonAccent(obj.transform, "Control Accent", AnchorTop(), new Vector2(0f, -4f), Vector2.zero, accent);
            AddDockCommandGlyph(obj.transform, ControlGlyph(labelText), accent, new Color32(83, 68, 30, 255));
            AddDockButtonFacets(obj.transform, new Color32(255, 202, 70, 245));
            var label = CreateText(obj.transform, "Label", labelText, 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.color = new Color32(83, 68, 30, 255);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(18f, 0f);
            label.rectTransform.offsetMax = new Vector2(-2f, 0f);
        }

        private void AddPolicyButton(Transform parent, string labelText, CityPolicy policy)
        {
            var obj = new GameObject("Policy " + policy);
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = new Color32(245, 255, 238, 32);
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(() => { if (controller != null) controller.TogglePolicy(policy); });
            var policyAccent = PolicyAccentColor(policy);
            CreateToolButtonAccent(obj.transform, "Policy Accent", AnchorTop(), new Vector2(0f, -4f), Vector2.zero, policyAccent);
            AddDockCommandGlyph(obj.transform, PolicyGlyph(policy), policyAccent, new Color32(245, 255, 238, 255));
            AddDockButtonFacets(obj.transform, policyAccent);
            var label = CreateText(obj.transform, "Label", labelText, 9, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(18f, 0f);
            label.rectTransform.offsetMax = new Vector2(-2f, 0f);
            policyButtons.Add(new PolicyButtonBinding
            {
                Button = button,
                Label = label,
                Policy = policy
            });
        }

        private void AddDockCommandGlyph(Transform parent, string glyphText, Color32 accent, Color32 textColor)
        {
            // CITY_DOCK_COMMAND_GLYPHS keep control and policy cells visually aligned with build tools.
            var chip = CreateToolButtonAccent(parent, "Command Glyph Chip", AnchorLeft(), new Vector2(4f, 3f), new Vector2(17f, -3f), accent);
            chip.raycastTarget = false;
            var glyph = CreateText(chip.transform, "Glyph", glyphText, 7, FontStyle.Bold, TextAnchor.MiddleCenter);
            glyph.color = textColor;
            glyph.raycastTarget = false;
            Stretch(glyph.rectTransform);
            var layout = chip.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private static string ControlGlyph(string labelText)
        {
            // CITY_DOCK_CONTROL_GLYPHS separate simulation, finance and storage controls inside the dense dock.
            if (labelText == "\u6682\u505c") return "II";
            if (labelText == "\u500d\u901f") return "x";
            if (labelText == "\u7a0e\u7387") return "\u7a0e";
            if (labelText == "\u9884\u7b97") return "\u8d22";
            if (labelText == "\u503a\u5238") return "\u503a";
            if (labelText == "\u4fdd\u5b58") return "\u5b58";
            if (labelText == "\u8bfb\u53d6") return "\u8bfb";
            return "\u25b6";
        }

        private static string PolicyGlyph(CityPolicy policy)
        {
            // CITY_DOCK_POLICY_GLYPHS make policy buttons read like CS-style management toggles.
            if (policy == CityPolicy.GreenCode) return "\u7eff";
            if (policy == CityPolicy.TransitPriority) return "\u516c";
            if (policy == CityPolicy.GrowthGrants) return "\u589e";
            if (policy == CityPolicy.AffordableHousing) return "\u623f";
            if (policy == CityPolicy.TrafficSafetyCampaign) return "\u5b89";
            if (policy == CityPolicy.CompleteStreets) return "\u8857";
            if (policy == CityPolicy.SignalOptimization) return "\u706f";
            if (policy == CityPolicy.CongestionPricing) return "\u8d39";
            if (policy == CityPolicy.ParkingFees) return "P";
            return "\u653f";
        }

        private static Color32 PolicyAccentColor(CityPolicy policy)
        {
            // REFERENCE_IMAGE_POLICY_BUTTON_ACCENTS gives management buttons the same card rhythm as build tools.
            if (policy == CityPolicy.GreenCode) return new Color32(96, 190, 122, 255);
            if (policy == CityPolicy.TransitPriority) return new Color32(86, 139, 210, 255);
            if (policy == CityPolicy.GrowthGrants) return new Color32(255, 202, 70, 255);
            if (policy == CityPolicy.AffordableHousing) return new Color32(255, 204, 109, 255);
            if (policy == CityPolicy.TrafficSafetyCampaign) return new Color32(224, 106, 82, 255);
            if (policy == CityPolicy.CompleteStreets) return new Color32(82, 188, 158, 255);
            if (policy == CityPolicy.SignalOptimization) return new Color32(112, 192, 214, 255);
            if (policy == CityPolicy.CongestionPricing) return new Color32(222, 158, 86, 255);
            if (policy == CityPolicy.ParkingFees) return new Color32(168, 150, 118, 255);
            return new Color32(96, 190, 122, 255);
        }

        private void SetStatTexts(List<Text> labels, List<HudStat> stats)
        {
            for (var i = 0; i < labels.Count; i += 1)
            {
                if (i >= stats.Count)
                {
                    labels[i].text = string.Empty;
                    SetDemandStatBackplate(labels, i, null);
                    SetTopStatWarningStyle(labels, i, null);
                    continue;
                }

                var topStat = ReferenceEquals(labels, topTexts);
                labels[i].text = topStat
                    ? TopStatIcon(stats[i].Label) + " " + stats[i].Label + "  " + CompactCardText(stats[i].Value, 18)
                    : DemandChipText(stats[i]);
                labels[i].color = stats[i].Warning
                    ? (topStat ? new Color32(255, 225, 130, 255) : new Color32(255, 225, 130, 255))
                    : (topStat ? new Color32(245, 255, 238, 255) : new Color32(245, 255, 238, 255));
                SetTopStatWarningStyle(labels, i, stats[i]);
                SetDemandStatBackplate(labels, i, stats[i]);
            }
        }

        private void SetTopStatWarningStyle(List<Text> labels, int index, HudStat stat)
        {
            if (!ReferenceEquals(labels, topTexts) || index < 0 || index >= topTextOutlines.Count)
            {
                return;
            }

            // REFERENCE_IMAGE_RESOURCE_WARNING_ROWS gives urgent resource lines a compact amber scan cue.
            var outline = topTextOutlines[index];
            if (outline == null)
            {
                return;
            }

            outline.enabled = stat != null && stat.Warning;
            outline.effectColor = new Color32(255, 216, 113, 210);
            outline.effectDistance = new Vector2(1.25f, -1.25f);
            if (index < topStatScanMarkers.Count && topStatScanMarkers[index] != null)
            {
                topStatScanMarkers[index].color = TopStatScanMarkerColor(index, stat != null && stat.Warning);
            }

            if (index < topStatRowBackplates.Count && topStatRowBackplates[index] != null)
            {
                topStatRowBackplates[index].color = TopStatRowBackplateColor(index, stat != null && stat.Warning);
            }
        }

        private void SetDemandStatBackplate(List<Text> labels, int index, HudStat stat)
        {
            if (!ReferenceEquals(labels, demandTexts) || index < 0 || index >= labels.Count || labels[index] == null)
            {
                return;
            }

            // DEMAND_WARNING_BACKPLATES keeps all 33 bottom stats visible while surfacing urgent pressure.
            var parent = labels[index].transform.parent;
            var image = parent != null ? parent.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }

            image.color = stat == null
                ? new Color32(245, 255, 238, 24)
                : DemandStatBackplateColor(stat.Warning);

            var outline = parent != null ? parent.GetComponent<Outline>() : null;
            if (outline != null)
            {
                var amount = DemandFillAmount(stat);
                var hot = stat != null && amount >= 0.72f;
                outline.enabled = stat != null && (stat.Warning || hot);
                outline.effectColor = stat != null && stat.Warning
                    ? new Color32(244, 116, 71, 238)
                    : new Color32(255, 207, 86, 160);
                outline.effectDistance = stat != null && stat.Warning ? new Vector2(1.5f, -1.5f) : new Vector2(1.1f, -1.1f);
            }

            if (index < demandFillBars.Count && demandFillBars[index] != null)
            {
                var fill = demandFillBars[index];
                var amount = DemandFillAmount(stat);
                fill.rectTransform.anchorMax = new Vector2(amount, 0f);
                fill.color = DemandFillColor(stat, amount);
                SetDemandHotCorner(index, stat, amount);
            }

            if (index < demandGroupBars.Count && demandGroupBars[index] != null)
            {
                // CITY_SKYLINES_DEMAND_GROUP_BANDS make the 33-chip demand wall read as grouped information layers.
                demandGroupBars[index].color = stat == null
                    ? new Color32(245, 255, 238, 54)
                    : DemandGroupColor(stat.Id, stat.Warning);
            }

            SetDemandGroupTag(index, stat);
        }

        private void SetDemandGroupTag(int index, HudStat stat)
        {
            if (index < 0 || index >= demandGroupTags.Count || demandGroupTags[index] == null)
            {
                return;
            }

            // CITY_SKYLINES_DEMAND_CATEGORY_TAGS make the compact 33-chip demand panel scan like grouped CS-style data.
            var tag = demandGroupTags[index];
            tag.text = stat == null ? string.Empty : DemandGroupTag(stat.Id);
            tag.color = stat == null
                ? new Color32(245, 255, 238, 0)
                : DemandGroupTagColor(stat.Id, stat.Warning);
        }

        private void SetDemandHotCorner(int index, HudStat stat, float amount)
        {
            if (index < 0 || index >= demandHotCorners.Count || demandHotCorners[index] == null)
            {
                return;
            }

            var hot = stat != null && amount >= 0.72f;
            demandHotCorners[index].color = stat == null || !hot
                ? new Color32(255, 202, 70, 0)
                : (stat.Warning ? new Color32(244, 116, 71, 245) : new Color32(255, 207, 86, 224));
        }

        private static Color32 DemandStatBackplateColor(bool warning)
        {
            return warning
                ? new Color32(92, 69, 42, 218)
                : new Color32(245, 255, 238, 34);
        }

        private static string DemandChipText(HudStat stat)
        {
            // REFERENCE_IMAGE_DEMAND_STATUS_CHIPS keeps all 33 demand slots scannable in one-line chips.
            if (stat == null)
            {
                return string.Empty;
            }

            var label = ShortDemandLabel(stat.Id, stat.Label);
            var value = PrimaryDemandValue(stat.Value);
            var prefix = stat.Warning ? "!" : DemandFillAmount(stat) >= 0.72f ? "^" : string.Empty;
            return string.IsNullOrEmpty(value) ? prefix + label : prefix + label + " " + value;
        }

        private static string ShortDemandLabel(string id, string label)
        {
            if (id == "residential") return "\u4f4f";
            if (id == "commercial") return "\u5546";
            if (id == "mixed_use") return "\u6df7";
            if (id == "office") return "\u529e";
            if (id == "industrial") return "\u5de5";
            if (id == "rent") return "\u79df";
            if (id == "living") return "\u5c45";
            if (id == "crime") return "\u5b89";
            if (id == "skill") return "\u624d";
            if (id == "innovation") return "\u521b";
            if (id == "labor") return "\u5de5\u4f4d";
            if (id == "road_network") return "\u8def";
            if (id == "road_safety") return "\u8def\u5b89";
            if (id == "walkability") return "\u6b65";
            if (id == "commute") return "\u901a";
            if (id == "environment") return "\u73af";
            if (id == "public_health") return "\u5065";
            if (id == "disaster") return "\u707e";
            if (id == "attraction") return "\u5438";
            if (id == "visitors") return "\u6e38";
            if (id == "land_use") return "\u5730";
            if (id == "goods") return "\u8d27";
            if (id == "park") return "\u56ed";
            if (id == "health") return "\u533b";
            if (id == "education") return "\u5b66";
            if (id == "safety") return "\u6d88";
            if (id == "emergency") return "\u54cd";
            if (id == "waste") return "\u56de";
            if (id == "maintenance") return "\u7ef4";
            if (id == "utility_reliability") return "\u6c34\u7535";
            if (id == "transit") return "\u516c";
            if (id == "logistics") return "\u7269";
            if (id == "communication") return "\u4fe1";
            return CompactCardText(label, 2);
        }

        private static string PrimaryDemandValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var first = value;
            var slash = first.IndexOf('/');
            if (slash >= 0)
            {
                first = first.Substring(0, slash);
            }

            var space = first.IndexOf(' ');
            if (space >= 0)
            {
                first = first.Substring(0, space);
            }

            return CompactCardText(first, 4);
        }

        private static float DemandFillAmount(HudStat stat)
        {
            // DEMAND_CHIP_FILL_BARS adds reference-style pressure meters under each demand chip.
            if (stat == null || string.IsNullOrEmpty(stat.Value))
            {
                return 0f;
            }

            var value = stat.Value;
            var slash = value.IndexOf('/');
            if (slash > 0)
            {
                var used = ParseLeadingNumber(value.Substring(0, slash));
                var total = ParseLeadingNumber(value.Substring(slash + 1));
                if (total > 0f)
                {
                    return Mathf.Clamp01(used / total);
                }
            }

            var number = ParseLeadingNumber(value);
            if (number < 0f)
            {
                return 0.18f;
            }

            return Mathf.Clamp01(number / 100f);
        }

        private static Color32 DemandFillColor(HudStat stat, float amount)
        {
            // CITY_DEMAND_HEAT_FILL_COLORS turns the 33 demand chips into compact pressure meters.
            if (stat != null && stat.Warning)
            {
                return new Color32(236, 116, 56, 232);
            }

            if (amount >= 0.72f)
            {
                return new Color32(255, 202, 70, 224);
            }

            if (amount >= 0.46f)
            {
                return new Color32(153, 216, 94, 210);
            }

            return new Color32(92, 204, 112, 190);
        }

        private static Color32 DemandGroupColor(string id, bool warning)
        {
            var color = DemandGroupBaseColor(id);
            return warning
                ? BlendToolRecommendationColor(color, new Color32(255, 202, 70, 255), 0.36f)
                : color;
        }

        private static Color32 DemandGroupBaseColor(string id)
        {
            if (id == "residential" || id == "commercial" || id == "mixed_use" || id == "office" || id == "industrial" || id == "land_use")
            {
                return new Color32(96, 190, 122, 230);
            }

            if (id == "road_network" || id == "road_safety" || id == "walkability" || id == "commute" || id == "transit" || id == "logistics")
            {
                return new Color32(86, 139, 210, 230);
            }

            if (id == "park" || id == "health" || id == "education" || id == "safety" || id == "emergency" || id == "maintenance" || id == "crime" || id == "public_health")
            {
                return new Color32(244, 139, 124, 230);
            }

            if (id == "utility_reliability" || id == "waste" || id == "communication")
            {
                return new Color32(82, 174, 186, 230);
            }

            if (id == "environment" || id == "disaster" || id == "RISK_FORECAST_HUD")
            {
                return new Color32(169, 112, 190, 230);
            }

            if (id == "goods" || id == "skill" || id == "innovation" || id == "labor" || id == "attraction" || id == "visitors" || id == "rent" || id == "living")
            {
                return new Color32(255, 202, 70, 230);
            }

            return new Color32(245, 255, 238, 180);
        }

        private static string DemandGroupTag(string id)
        {
            if (id == "residential" || id == "commercial" || id == "mixed_use" || id == "office" || id == "industrial" || id == "land_use")
            {
                return "\u533a";
            }

            if (id == "road_network" || id == "road_safety" || id == "walkability" || id == "commute" || id == "transit" || id == "logistics")
            {
                return "\u8def";
            }

            if (id == "park" || id == "health" || id == "education" || id == "safety" || id == "emergency" || id == "maintenance" || id == "crime" || id == "public_health")
            {
                return "\u670d";
            }

            if (id == "utility_reliability" || id == "waste" || id == "communication")
            {
                return "\u7ba1";
            }

            if (id == "environment" || id == "disaster" || id == "RISK_FORECAST_HUD")
            {
                return "\u73af";
            }

            if (id == "goods" || id == "skill" || id == "innovation" || id == "labor" || id == "attraction" || id == "visitors" || id == "rent" || id == "living")
            {
                return "\u7ecf";
            }

            return "\u6570";
        }

        private static Color32 DemandGroupTagColor(string id, bool warning)
        {
            var baseColor = DemandGroupBaseColor(id);
            return warning
                ? new Color32(255, 225, 130, 245)
                : new Color32(baseColor.r, baseColor.g, baseColor.b, 218);
        }

        private static float ParseLeadingNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return -1f;
            }

            var start = -1;
            var length = 0;
            for (var i = 0; i < value.Length; i += 1)
            {
                var c = value[i];
                var numeric = char.IsDigit(c) || c == '-' || c == '+';
                if (numeric && start < 0)
                {
                    start = i;
                }

                if (start >= 0)
                {
                    if (char.IsDigit(c) || c == '-' || c == '+')
                    {
                        length += 1;
                        continue;
                    }

                    break;
                }
            }

            if (start < 0 || length == 0)
            {
                return -1f;
            }

            float parsed;
            return float.TryParse(value.Substring(start, length), out parsed) ? parsed : -1f;
        }

        private static string TopStatIcon(string label)
        {
            // REFERENCE_IMAGE_RESOURCE_ICON_PREFIXES gives the left resource panel the mockup's icon rhythm.
            if (string.IsNullOrEmpty(label))
            {
                return "\u25a0";
            }

            if (label.Contains("\u73b0\u91d1") || label.Contains("\u6536\u5165") || label.Contains("\u9884\u7b97"))
            {
                return "\u25cf";
            }

            if (label.Contains("\u4eba\u53e3") || label.Contains("\u5c45\u6c11"))
            {
                return "\u25c6";
            }

            if (label.Contains("\u7535") || label.Contains("\u80fd\u6e90"))
            {
                return "\u25b2";
            }

            if (label.Contains("\u6c34") || label.Contains("\u96e8"))
            {
                return "\u25bc";
            }

            if (label.Contains("\u5e78\u798f") || label.Contains("\u8bc4\u5206"))
            {
                return "\u25c9";
            }

            return "\u25a0";
        }

        private static string BuildObjectiveCardText(CityHudSnapshot snapshot)
        {
            var title = string.IsNullOrEmpty(snapshot.ObjectiveTitle) ? "\u57ce\u5e02\u4efb\u52a1" : snapshot.ObjectiveTitle;
            var required = Mathf.Max(1, snapshot.ObjectiveRequired);
            var text = "\u4efb\u52a1 " + title + "  " + Mathf.Min(snapshot.ObjectiveProgress, required) + "/" + required;
            var dailyOrder = BuildDailyOrderLine(snapshot);
            if (!string.IsNullOrEmpty(dailyOrder))
            {
                text += "\n" + dailyOrder;
            }

            var hint = BuildNextAdvisorLine(snapshot);
            if (!string.IsNullOrEmpty(hint))
            {
                text += "\n" + hint;
            }

            return text;
        }

        private static string BuildDailyOrderLine(CityHudSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ObjectiveRequired <= 0)
            {
                return string.Empty;
            }

            var required = Mathf.Max(1, snapshot.ObjectiveRequired);
            var progress = Mathf.Min(snapshot.ObjectiveProgress, required);
            var remaining = Mathf.Max(0, required - progress);
            var rewardCash = snapshot.ObjectiveDone ? 2500 : 2000;
            var rewardPopulation = snapshot.ObjectiveDone ? 30 : 20;
            if (snapshot.ObjectiveDone)
            {
                return "\u6bcf\u65e5\u8ba2\u5355 \u53ef\u9886 \u91d1+" + rewardCash + " \u4eba+" + rewardPopulation;
            }

            return "\u6bcf\u65e5\u8ba2\u5355 \u8fd8\u5dee" + remaining + "  \u5956\u52b1 \u91d1+" + rewardCash + " \u4eba+" + rewardPopulation;
        }

        private static string BuildNextAdvisorLine(CityHudSnapshot snapshot)
        {
            // RIGHT_CARD_NEXT_ADVISOR_STRIP makes the task card read like a city-builder advisor.
            var hint = snapshot.ObjectiveInsightParts != null && snapshot.ObjectiveInsightParts.Count > 0
                ? snapshot.ObjectiveInsightParts[0]
                : FirstObjectiveHintLine(BuildObjectiveHintText(snapshot));
            return string.IsNullOrEmpty(hint) ? string.Empty : "\u5efa\u8bae " + CompactCardText(hint, 18);
        }

        private static string BuildMilestoneTaskCardText(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            // RIGHT_SIDE_MILESTONE_TASK_CARDS keeps the reference task panel compact and data-driven.
            var required = Mathf.Max(1, snapshot.ObjectiveRequired);
            var currentTitle = CompactCardText(string.IsNullOrEmpty(snapshot.ObjectiveTitle) ? "\u89e3\u9501\u65b0\u533a" : snapshot.ObjectiveTitle, 8);
            var currentProgress = Mathf.Min(snapshot.ObjectiveProgress, required) + "/" + required;
            var state = TaskCardStateLabel(snapshot, metrics);
            if (metrics == null || metrics.Milestones == null || metrics.Milestones.Count == 0)
            {
                return "\u4efb\u52a1\u5355 #01  \u4e0b\u4e00\u6b65"
                    + "\n\u89e3\u9501\u65b0\u533a > " + currentTitle + " " + currentProgress
                    + "  " + state
                    + BuildTaskCardOrderLine(snapshot, metrics)
                    + ExpansionStatusCardLine(snapshot);
            }

            var completed = CountCompletedMilestones(metrics.Milestones);
            var text = "\u4efb\u52a1\u5355 #" + Mathf.Clamp(completed + 1, 1, 99).ToString("00") + "  \u4e0b\u4e00\u6b65"
                + "\n\u91cc\u7a0b\u7891 " + completed + "/" + metrics.Milestones.Count
                + "  \u89e3\u9501\u65b0\u533a " + currentTitle + " " + currentProgress
                + "  " + state
                + BuildTaskCardOrderLine(snapshot, metrics)
                + ExpansionStatusCardLine(snapshot);
            var shown = 0;
            for (var i = 0; i < metrics.Milestones.Count && shown < 1; i += 1)
            {
                var milestone = metrics.Milestones[i];
                if (milestone == null || milestone.Done)
                {
                    continue;
                }

                text = AppendMilestoneCardPart(text, milestone);
                shown += 1;
            }

            if (shown == 0 && snapshot.ObjectiveInsightParts != null && snapshot.ObjectiveInsightParts.Count > 0)
            {
                text += "\n\u5efa\u8bae > " + CompactCardText(snapshot.ObjectiveInsightParts[0], 12);
            }

            if (!string.IsNullOrEmpty(snapshot.RecentEventText))
            {
                // MILESTONE_CARD_RECENT_EVENT_BEACON keeps recent city events visible in the task card.
                text += "\n\u4e8b\u4ef6 > " + CompactCardText(snapshot.RecentEventText, 14);
            }

            return text;
        }

        private static string BuildTaskCardOrderLine(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            var rewardCash = snapshot != null && snapshot.ObjectiveDone ? 2500 : 2000;
            var rewardPopulation = metrics != null && metrics.Population >= 240 ? 30 : 20;
            var action = TaskCardOrderAction(snapshot, metrics);
            if (snapshot != null && snapshot.ObjectiveDone)
            {
                return "\n\u5956\u52b1 > \u91d1+" + rewardCash + " \u4eba+" + rewardPopulation + "  \u7acb\u5373\u9886\u53d6";
            }

            return "\n\u5956\u52b1 > \u91d1+" + rewardCash + " \u4eba+" + rewardPopulation + "  \u884c\u52a8 " + CompactCardText(action, 8);
        }

        private static string TaskCardOrderAction(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            if (snapshot != null && snapshot.ObjectiveRequired > 0)
            {
                var remaining = Mathf.Max(0, snapshot.ObjectiveRequired - snapshot.ObjectiveProgress);
                if (remaining <= 1)
                {
                    return "\u6536\u5c3e\u4e00\u5355";
                }
            }

            if (metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7\u5efa\u7b51";
            }

            if (metrics != null && metrics.ServiceGapPressure >= 55)
            {
                return "\u8865\u670d\u52a1";
            }

            if (metrics != null && metrics.RoadBottleneckPressure >= 55)
            {
                return "\u4fee\u8def\u7f51";
            }

            return "\u7ee7\u7eed\u5efa\u9020";
        }

        private static string ExpansionStatusCardLine(CityHudSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.ExpansionStatusText))
            {
                return string.Empty;
            }

            return "\n\u65b0\u533a\u72b6\u6001 > " + CompactCardText(snapshot.ExpansionStatusText, 16);
        }

        private static string AppendMilestoneCardPart(string text, CityMilestone milestone)
        {
            var required = Mathf.Max(1, milestone.Required);
            var progress = Mathf.Clamp(milestone.Progress, 0, required);
            return text + "\n\u4e0b\u4e00\u6b65 > " + CompactCardText(milestone.Title, 8) + " " + progress + "/" + required;
        }

        private static string TaskCardStateLabel(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            if (snapshot != null && snapshot.ObjectiveDone)
            {
                return "\u53ef\u9886\u53d6";
            }

            if (metrics != null && (metrics.ForecastRisk >= 65 || metrics.ServiceGapPressure >= 60 || metrics.RoadBottleneckPressure >= 60))
            {
                return "\u9ad8\u4f18\u5148";
            }

            return "\u8fdb\u884c\u4e2d";
        }

        private static string CompactCardText(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return string.IsNullOrEmpty(value) ? string.Empty : value;
            }

            return value.Substring(0, Mathf.Max(1, maxLength - 1)) + "...";
        }

        private static int CountCompletedMilestones(List<CityMilestone> milestones)
        {
            var completed = 0;
            for (var i = 0; i < milestones.Count; i += 1)
            {
                if (milestones[i] != null && milestones[i].Done)
                {
                    completed += 1;
                }
            }

            return completed;
        }

        private static string FirstObjectiveHintLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var newline = text.IndexOf((char)10);
            if (newline >= 0)
            {
                return text.Substring(0, newline);
            }

            return text.Length > 44 ? text.Substring(0, 44) : text;
        }

        private static string BuildObjectiveHintText(CityHudSnapshot snapshot)
        {
            var text = AppendObjectiveHintPart(string.Empty, snapshot.ObjectiveHint);
            var insights = snapshot.ObjectiveInsightParts;
            if (insights == null || insights.Count == 0)
            {
                insights = new List<string>();
                AddObjectiveFallbackInsight(insights, snapshot.ForecastText);
                AddObjectiveFallbackInsight(insights, snapshot.ServiceGapText);
                AddObjectiveFallbackInsight(insights, snapshot.DistrictPriorityText);
                AddObjectiveFallbackInsight(insights, snapshot.RoadHierarchyText);
                AddObjectiveFallbackInsight(insights, snapshot.CommuteCorridorText);
                AddObjectiveFallbackInsight(insights, snapshot.EconomicSpecializationText);
                AddObjectiveFallbackInsight(insights, snapshot.GrowthBottleneckText);
                AddObjectiveFallbackInsight(insights, snapshot.HousingAffordabilityText);
                AddObjectiveFallbackInsight(insights, snapshot.BuildingUpgradeReadinessText);
                AddObjectiveFallbackInsight(insights, snapshot.BudgetInsightText);
                AddObjectiveFallbackInsight(insights, snapshot.DemandInsightText);
                AddObjectiveFallbackInsight(insights, snapshot.RecentEventText);
            }

            for (var i = 0; i < insights.Count; i += 1)
            {
                text = AppendObjectiveHintPart(text, insights[i]);
            }

            return text;
        }

        private static string BuildCityPulseText(CityMetrics metrics)
        {
            // CITY_PULSE_KPI_STRIP keeps the right-side operations readout close to CS-style issue diagnosis.
            if (metrics == null)
            {
                return "\u8fd0\u8425 --";
            }

            var recommended = RecommendedOverlayMode(metrics);
            var pressure = OverlayPressureScore(recommended, metrics);
            var action = string.IsNullOrEmpty(metrics.ForecastAction)
                ? PrimaryPulseDriverLabel(metrics)
                : metrics.ForecastAction;
            return "\u8109\u640f \u4e3b" + PrimaryPulseDriverLabel(metrics)
                + "  \u56fe" + OverlayLabel(recommended) + pressure
                + "\n" + BuildTrendTripleText(metrics) + "  \u5355" + CompactCardText(BuildCityPulseOrderCue(metrics), 9)
                + "  \u505a" + CompactCardText(action, 8);
        }

        private static string BuildCityPulseOrderCue(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u63a5\u5355";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return "\u53ef\u9886\u5956";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                var remaining = Mathf.Max(0, metrics.ActiveObjective.Required - metrics.ActiveObjective.Progress);
                return remaining <= 1 ? "\u5feb\u5b8c\u6210" : "\u8fd8\u5dee" + remaining;
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7+" + metrics.BuildingUpgradeReadyCount;
            }

            if (metrics.ServiceGapPressure >= 55)
            {
                return "\u8865\u670d\u52a1";
            }

            if (metrics.ForecastRisk >= 70)
            {
                return "\u964d\u98ce\u9669";
            }

            return "\u63a5\u65b0\u5355";
        }

        private static string BuildCityEventTickerText(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            // CITY_EVENT_TICKER turns the alert row into a CS-style live city notification strip.
            var orderNews = BuildEventTickerOrderNews(metrics);
            if (!string.IsNullOrEmpty(orderNews))
            {
                return orderNews;
            }

            if (snapshot != null && snapshot.Alerts != null && snapshot.Alerts.Count > 0)
            {
                return "\u8b66\u62a5 " + CompactTickerPart(string.Join(" | ", snapshot.Alerts.ToArray()), 24);
            }

            if (metrics != null && metrics.ForecastRisk >= 70)
            {
                return "\u9884\u8b66 \u98ce\u9669" + metrics.ForecastRisk + " -> " + CompactTickerPart(metrics.ForecastAction, 14);
            }

            if (metrics != null && metrics.NetIncome < 0)
            {
                return "\u8d22\u52a1 \u6708\u6536" + FormatSigned(metrics.NetIncome) + "  \u73b0\u91d1\u53ef\u6491" + CashRunwayStatus(metrics);
            }

            if (metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7 \u5019\u9009" + metrics.BuildingUpgradeReadyCount + "  " + CompactTickerPart(metrics.BuildingUpgradeReadinessAction, 14);
            }

            if (snapshot != null && !string.IsNullOrEmpty(snapshot.RecentEventText))
            {
                return "\u4e8b\u4ef6 " + CompactTickerPart(snapshot.RecentEventText, 24);
            }

            if (metrics != null && metrics.ServiceGapPressure >= 55)
            {
                return "\u670d\u52a1 \u7f3a\u53e3" + metrics.ServiceGapPressure + " -> " + CompactTickerPart(metrics.ServiceGapAdvisorAction, 14);
            }

            return "\u8fd0\u884c\u7a33\u5b9a  \u57ce\u5e02\u5206" + (metrics != null ? metrics.CityScore.ToString() : "--");
        }

        private static string BuildEventTickerOrderNews(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return "\u8ba2\u5355\u5feb\u8baf \u53ef\u9886\u5956\u52b1 -> \u89c4\u5212\u65b0\u533a";
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Required > 0)
            {
                var required = Mathf.Max(1, metrics.ActiveObjective.Required);
                var progress = Mathf.Clamp(metrics.ActiveObjective.Progress, 0, required);
                var remaining = Mathf.Max(0, required - progress);
                if (remaining <= 2)
                {
                    return "\u8ba2\u5355\u5feb\u8baf " + progress + "/" + required + "  \u8fd8\u5dee" + remaining + " -> \u6536\u5c3e";
                }
            }

            return string.Empty;
        }

        private static Color32 CityEventTickerColor(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            if (snapshot != null && snapshot.Alerts != null && snapshot.Alerts.Count > 0)
            {
                return new Color32(224, 126, 49, 255);
            }

            if (metrics != null && (metrics.ForecastRisk >= 70 || metrics.NetIncome < 0))
            {
                return new Color32(171, 92, 48, 255);
            }

            if (metrics != null && (metrics.BuildingUpgradeReadyCount > 0 || metrics.ServiceGapPressure >= 55))
            {
                return new Color32(68, 124, 118, 255);
            }

            return new Color32(63, 146, 96, 255);
        }

        private static string BuildHudFooterStatusText(CityHudSnapshot snapshot, CityMetrics metrics)
        {
            var speed = metrics == null ? "--" : string.Empty;
            var eventText = snapshot != null && !string.IsNullOrEmpty(snapshot.RecentEventText)
                ? " | \u8fd1 " + CompactTickerPart(snapshot.RecentEventText, 12)
                : string.Empty;
            if (metrics == null)
            {
                return speed + eventText;
            }

            return "\u901f " + CashRunwayStatus(metrics)
                + " | \u8d22" + FormatSigned(metrics.NetIncome)
                + " | " + BuildTrendTripleText(metrics)
                + eventText;
        }

        private static string BuildTrendTripleText(CityMetrics metrics)
        {
            // CITY_TREND_TRIPLE keeps demand, growth and upgrade pressure visible without adding HUD controls.
            if (metrics == null)
            {
                return "\u8d8b--";
            }

            return "\u9700" + TrendBar(metrics.DemandUrgency)
                + " \u6210" + TrendBar(metrics.GrowthBottleneckScore)
                + " \u5347" + metrics.BuildingUpgradeReadyCount + "/" + metrics.BuildingUpgradeBlockedCount;
        }

        private static string TrendBar(int value)
        {
            var clamped = Mathf.Clamp(value, 0, 100);
            if (clamped >= 72) return "###";
            if (clamped >= 48) return "##-";
            if (clamped >= 24) return "#--";
            return "---";
        }

        private static string CompactTickerPart(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "--";
            }

            return value.Length <= maxLength ? value : value.Substring(0, Mathf.Max(1, maxLength - 1)) + "...";
        }

        private static Color32 AdvisorSeverityColor(CityMetrics metrics)
        {
            // RIGHT_ADVISOR_RISK_STRIP adds a passive color cue to the task card without changing its copy.
            if (metrics == null)
            {
                return new Color32(126, 170, 144, 230);
            }

            var pressure = Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.RoadBottleneckPressure, metrics.ServiceGapPressure));
            if (pressure >= 72 || metrics.NetIncome < -500)
            {
                return new Color32(236, 116, 56, 245);
            }

            if (pressure >= 55 || metrics.UtilityReliability < 85)
            {
                return new Color32(243, 190, 55, 245);
            }

            return new Color32(88, 204, 96, 235);
        }

        private static Vector2 AdvisorSeverityOutlineDistance(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return new Vector2(1.8f, -1.8f);
            }

            var pressure = Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.RoadBottleneckPressure, metrics.ServiceGapPressure));
            if (pressure >= 72 || metrics.NetIncome < -500)
            {
                return new Vector2(2.8f, -2.8f);
            }

            if (pressure >= 55 || metrics.UtilityReliability < 85)
            {
                return new Vector2(2.2f, -2.2f);
            }

            return new Vector2(1.8f, -1.8f);
        }

        private static string AdvisorSeverityBadgeLabel(CityMetrics metrics)
        {
            // CITY_SKYLINES_ADVISOR_BADGE_LABEL makes the right task card risk state readable without relying on color.
            if (metrics == null)
            {
                return "--";
            }

            var pressure = Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.RoadBottleneckPressure, metrics.ServiceGapPressure));
            if (pressure >= 72 || metrics.NetIncome < -500)
            {
                return "\u5371";
            }

            if (pressure >= 55 || metrics.UtilityReliability < 85)
            {
                return "\u6ce8";
            }

            return "\u7a33";
        }

        private static Color32 AdvisorSeverityBadgeTextColor(CityMetrics metrics)
        {
            var label = AdvisorSeverityBadgeLabel(metrics);
            return label == "\u6ce8"
                ? new Color32(80, 66, 30, 255)
                : new Color32(245, 255, 238, 255);
        }

        private static string PrimaryPulseDriverLabel(CityMetrics metrics)
        {
            // CITY_PULSE_PRIMARY_DRIVER_LABEL turns dense pulse numbers into a city-builder diagnosis.
            if (metrics.RoadBottleneckPressure >= 60 || metrics.CommuteEfficiency < 45 || metrics.CarDependency > 70)
            {
                return "\u4ea4\u901a";
            }

            if (metrics.ServiceGapPressure >= 55 || metrics.ServiceCoverage < 45)
            {
                return "\u670d\u52a1";
            }

            if (metrics.NetIncome < 0 || metrics.ForecastRisk >= 65)
            {
                return "\u8d22\u653f";
            }

            if (metrics.UtilityReliability < 90 || metrics.UtilityUtilization > 115 || metrics.FloodRisk > 55)
            {
                return "\u6c34\u7535";
            }

            if (metrics.HousingCapacity <= metrics.Population + 12 || metrics.RentPressure > 70)
            {
                return "\u4f4f\u623f";
            }

            return "\u5e73\u7a33";
        }

        private static string BuildCityTitleText(CityMetrics metrics)
        {
            // REFERENCE_IMAGE_CITY_TITLE mirrors the level/name/score header from the provided mockup.
            if (metrics == null)
            {
                return "\u53e3\u888b\u57ce\u5e02\u89c4\u5212\u5e08\n\u65b0\u751f\u8857\u533a  \u8bc4\u5206 --";
            }

            var levelName = string.IsNullOrEmpty(metrics.CityLevelName) ? "\u65b0\u751f\u8857\u533a" : metrics.CityLevelName;
            return "\u53e3\u888b\u57ce\u5e02\u89c4\u5212\u5e08\n" + levelName + "  \u8bc4\u5206 " + metrics.CityScore;
        }

        private static string BuildCityLevelBadgeText(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "1";
            }

            return Mathf.Clamp(1 + metrics.Population / 120, 1, 9).ToString();
        }

        private void RefreshSimulationStatusBadge(CityMetrics metrics)
        {
            if (simulationStatusBadgeText == null
                && simulationStatusBadgeImage == null
                && simulationStatusBadgeIconImage == null
                && simulationStatusBadgeIconText == null
                && simulationStatusBadgeSubText == null
                && simulationStatusRewardBadgeImage == null
                && simulationStatusRewardBadgeText == null)
            {
                return;
            }

            var snapshot = controller != null ? controller.HudSnapshot : null;
            if (simulationStatusBadgeText != null)
            {
                simulationStatusBadgeText.text = BuildSimulationStatusBadgeText(metrics, snapshot);
            }

            if (simulationStatusBadgeSubText != null)
            {
                simulationStatusBadgeSubText.text = BuildSimulationStatusBadgeSubText(metrics, snapshot);
                simulationStatusBadgeSubText.color = SimulationStatusBadgeSubTextColor(metrics);
            }

            if (simulationStatusBadgeImage != null)
            {
                simulationStatusBadgeImage.color = SimulationStatusBadgeColor(metrics);
            }

            if (simulationStatusBadgeIconImage != null)
            {
                simulationStatusBadgeIconImage.color = controller != null && controller.Paused
                    ? new Color32(245, 255, 238, 214)
                    : new Color32(253, 255, 248, 240);
            }

            if (simulationStatusBadgeIconText != null)
            {
                simulationStatusBadgeIconText.text = BuildSimulationStatusBadgeIcon(metrics);
                simulationStatusBadgeIconText.color = controller != null && controller.Paused
                    ? new Color32(88, 132, 96, 255)
                    : SimulationStatusBadgeIconColor(metrics);
            }

            if (simulationStatusRewardBadgeImage != null)
            {
                simulationStatusRewardBadgeImage.color = SimulationRewardBadgeColor(metrics, snapshot);
            }

            if (simulationStatusRewardBadgeText != null)
            {
                simulationStatusRewardBadgeText.text = SimulationRewardBadgeText(metrics, snapshot);
                simulationStatusRewardBadgeText.color = snapshot != null && snapshot.ObjectiveDone
                    ? new Color32(35, 95, 62, 255)
                    : new Color32(83, 68, 30, 255);
            }
        }

        private string BuildSimulationStatusBadgeText(CityMetrics metrics, CityHudSnapshot snapshot)
        {
            if (controller == null)
            {
                return "\u56de\u5408 --";
            }

            if (snapshot != null && snapshot.ObjectiveDone)
            {
                return "\u9886\u53d6\u5956\u52b1";
            }

            if (controller.Paused)
            {
                return "\u7ee7\u7eed\u89c4\u5212";
            }

            if (snapshot != null && snapshot.ObjectiveRequired > 0 && snapshot.ObjectiveProgress > 0)
            {
                return "\u4efb\u52a1\u63a8\u8fdb";
            }

            var pressure = SimulationActionPressure(metrics);
            if (pressure >= 70)
            {
                return "\u5148\u5904\u7406\u98ce\u9669";
            }

            if (metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u63a8\u8fdb\u5347\u7ea7";
            }

            return controller.SimulationSpeed >= 2f
                ? "\u5feb\u901f\u63a8\u8fdb"
                : "\u5b8c\u6210\u56de\u5408";
        }

        private string BuildSimulationStatusBadgeSubText(CityMetrics metrics, CityHudSnapshot snapshot)
        {
            if (controller == null)
            {
                return "\u7b49\u5f85\u57ce\u5e02\u6570\u636e";
            }

            if (snapshot != null && snapshot.ObjectiveDone)
            {
                return "\u5956\u52b1 \u91d1+2500 \u4eba+30 > \u89c4\u5212\u65b0\u533a";
            }

            if (controller.Paused)
            {
                var pausedMode = metrics != null ? RecommendedOverlayMode(metrics) : OverlayMode.Normal;
                return "\u6682\u505c\u4e2d  \u770b" + OverlayLabel(pausedMode);
            }

            if (snapshot != null && snapshot.ObjectiveRequired > 0 && snapshot.ObjectiveProgress > 0)
            {
                return "\u8fdb\u5ea6" + Mathf.Min(snapshot.ObjectiveProgress, snapshot.ObjectiveRequired) + "/" + Mathf.Max(1, snapshot.ObjectiveRequired)
                    + " > " + CompactCardText(snapshot.ObjectiveTitle, 9);
            }

            var pressure = SimulationActionPressure(metrics);
            if (pressure >= 70)
            {
                return "\u4e3b" + MiniMapPrimaryIssueLabel(metrics) + " " + pressure + " > " + CompactCardText(SimulationNextActionHint(metrics), 9);
            }

            if (metrics != null && metrics.DemandUrgency >= 50)
            {
                return "\u9700\u6c42" + metrics.DemandUrgency + " > \u8865\u5206\u533a";
            }

            if (metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5019\u9009" + metrics.BuildingUpgradeReadyCount + " > " + CompactCardText(GrowthOperationsText(metrics), 8);
            }

            return "x" + CompactSpeedLabel(controller.SimulationSpeed) + "  " + (metrics != null ? BuildTrendTripleText(metrics) : "\u89c4\u5212\u63a8\u8fdb");
        }

        private Color32 SimulationStatusBadgeSubTextColor(CityMetrics metrics)
        {
            var pressure = SimulationActionPressure(metrics);
            if (pressure >= 70)
            {
                return new Color32(104, 58, 26, 240);
            }

            return controller != null && controller.Paused
                ? new Color32(65, 88, 72, 238)
                : new Color32(103, 82, 32, 230);
        }

        private Color32 SimulationStatusBadgeColor(CityMetrics metrics)
        {
            // REFERENCE_IMAGE_SIMULATION_BADGE_STATE_COLOR makes pause/speed state visible at a glance.
            if (controller == null)
            {
                return new Color32(222, 241, 231, 230);
            }

            if (controller.Paused)
            {
                return new Color32(189, 205, 195, 238);
            }

            var pressure = SimulationActionPressure(metrics);
            if (pressure >= 70)
            {
                return new Color32(255, 172, 72, 245);
            }

            return controller.SimulationSpeed >= 2f
                ? new Color32(103, 196, 91, 245)
                : new Color32(255, 190, 57, 245);
        }

        private static int SimulationActionPressure(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.ServiceGapPressure, metrics.RoadBottleneckPressure)), 0, 100);
        }

        private static string SimulationNextActionHint(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u68c0\u67e5\u57ce\u5e02";
            }

            if (!string.IsNullOrEmpty(metrics.ForecastAction) && metrics.ForecastRisk >= 65) return metrics.ForecastAction;
            if (metrics.RoadBottleneckPressure >= 60) return "\u5347\u7ea7\u74f6\u9888\u8def";
            if (metrics.ServiceGapPressure >= 60) return "\u8865\u670d\u52a1";
            if (metrics.DemandUrgency >= 50) return "\u8865\u5206\u533a";
            return "\u5b8c\u6210\u56de\u5408";
        }

        private string BuildSimulationStatusBadgeIcon(CityMetrics metrics)
        {
            if (controller != null && controller.Paused)
            {
                return "\u23f8";
            }

            return SimulationActionPressure(metrics) >= 70 ? "!" : "\u25b6";
        }

        private Color32 SimulationStatusBadgeIconColor(CityMetrics metrics)
        {
            return SimulationActionPressure(metrics) >= 70
                ? new Color32(138, 74, 28, 255)
                : new Color32(255, 178, 45, 255);
        }

        private static string SimulationRewardBadgeText(CityMetrics metrics, CityHudSnapshot snapshot)
        {
            if (snapshot != null && snapshot.ObjectiveDone) return "\u53ef\u9886";
            if (SimulationActionPressure(metrics) >= 70) return "\u6025\u529e";
            if (snapshot != null && snapshot.ObjectiveProgress > 0) return "\u4efb\u52a1";
            return "\u56de\u5408";
        }

        private static Color32 SimulationRewardBadgeColor(CityMetrics metrics, CityHudSnapshot snapshot)
        {
            if (snapshot != null && snapshot.ObjectiveDone) return new Color32(210, 246, 192, 238);
            if (SimulationActionPressure(metrics) >= 70) return new Color32(255, 188, 66, 230);
            if (snapshot != null && snapshot.ObjectiveProgress > 0) return new Color32(255, 236, 150, 220);
            return new Color32(245, 255, 238, 150);
        }

        private static string BuildTopCapsuleText(CityMetrics metrics)
        {
            // REFERENCE_IMAGE_TOP_RESOURCE_CAPSULES keeps the main resources in compact top-right pills.
            if (metrics == null)
            {
                return "\u73b0\u91d1 --  \u4eba\u53e3 --  \u5e78\u798f --";
            }

            // REFERENCE_IMAGE_TOP_CAPSULE_COMPACT_TEXT avoids wasting width inside the top pill cluster.
            return "\u73b0\u91d1 " + metrics.Cash
                + "(" + FormatSigned(metrics.NetIncome) + ")"
                + "  \u4eba\u53e3 " + metrics.Population
                + "  \u5e78\u798f " + metrics.Happiness + "%";
        }

        private static string CashRunwayStatus(CityMetrics metrics)
        {
            if (metrics.NetIncome >= 0)
            {
                return "\u7a33\u5b9a";
            }

            if (metrics.CashRunwayDays < 0)
            {
                return "\u7d27\u5f20";
            }

            return metrics.CashRunwayDays + "\u5929";
        }

        private static void AddObjectiveFallbackInsight(List<string> insights, string text)
        {
            if (insights.Count >= 3 || string.IsNullOrEmpty(text))
            {
                return;
            }

            insights.Add(text);
        }

        private static string AppendObjectiveHintPart(string text, string part)
        {
            if (string.IsNullOrEmpty(part))
            {
                return text;
            }

            if (string.IsNullOrEmpty(text))
            {
                return part;
            }

            return text + "  " + part;
        }

        private string BuildToolStatusText()
        {
            if (interaction == null)
            {
                return "\u5de5\u5177\uff1a--";
            }

            if (interaction.ToolMode == CityToolMode.BuildRoad)
            {
                return ToolStatusWithLegend("\u5de5\u5177\uff1a\u94fa\u8def");
            }

            if (interaction.ToolMode == CityToolMode.UpgradeRoad)
            {
                return ToolStatusWithLegend("\u5de5\u5177\uff1a\u9053\u8def\u5347\u7ea7");
            }

            if (interaction.ToolMode == CityToolMode.ZonePaint)
            {
                return ToolStatusWithLegend("\u5de5\u5177\uff1a" + ZoneLabel(interaction.SelectedZone));
            }

            if (interaction.ToolMode == CityToolMode.BuildBuilding)
            {
                return ToolStatusWithLegend("\u5de5\u5177\uff1a" + BuildingLabel(interaction.SelectedBuildingId));
            }

            if (interaction.ToolMode == CityToolMode.Demolish)
            {
                return ToolStatusWithLegend("\u5de5\u5177\uff1a\u62c6\u9664");
            }

            return ToolStatusWithLegend("\u5de5\u5177\uff1a\u67e5\u770b");
        }

        private string BuildDockBadgeText()
        {
            // REFERENCE_IMAGE_DYNAMIC_BUILD_BADGE keeps the bottom category badge tied to the active tool.
            if (interaction == null)
            {
                return "\u5efa\u9020\n\u8ba2\u5355";
            }

            if (interaction.ToolMode == CityToolMode.BuildRoad) return "\u9053\u8def\n\u514d\u8d39";
            if (interaction.ToolMode == CityToolMode.UpgradeRoad) return "\u5347\u7ea7\n\u5956\u52b1";
            if (interaction.ToolMode == CityToolMode.ZonePaint) return CompactCardText(ZoneLabel(interaction.SelectedZone), 3) + "\n\u63a5\u5355";
            if (interaction.ToolMode == CityToolMode.BuildBuilding) return CompactCardText(BuildingLabel(interaction.SelectedBuildingId), 3) + "\n\u5efa\u9020";
            if (interaction.ToolMode == CityToolMode.Demolish) return "\u62c6\u9664\n\u8fd4\u6b3e";
            return "\u67e5\u770b\n\u70ed\u533a";
        }

        private string BuildDockBadgeGlyphText()
        {
            if (interaction == null)
            {
                return "\u5efa";
            }

            return ToolIconGlyph(interaction.ToolMode, interaction.SelectedZone, interaction.SelectedBuildingId);
        }

        private Color32 BuildDockBadgeColor()
        {
            if (interaction == null)
            {
                return new Color32(39, 125, 89, 238);
            }

            return ToolAccentColor(interaction.ToolMode, interaction.SelectedZone, interaction.SelectedBuildingId);
        }

        private string ToolStatusWithLegend(string status)
        {
            // CITY_TOOL_RECOMMENDATION_REASON_LINE explains why highlighted build tools are recommended.
            // REFERENCE_IMAGE_COMPACT_TOOL_STATUS keeps the right task card readable instead of log-like.
            var metrics = controller != null ? controller.Metrics : null;
            var recommendation = BuildToolRecommendationHint(metrics);
            var dashboard = BuildOperationsDashboardLine(metrics);
            var secondLine = string.IsNullOrEmpty(recommendation) ? dashboard : dashboard + " / " + recommendation;
            var shortcut = ToolShortcutHintText();
            var statusLine = string.IsNullOrEmpty(shortcut) ? status : status + "  " + shortcut;
            return statusLine + "  " + TimeStatusText() + "  " + CompactOperationsStatusText() + "\n" + secondLine;
        }

        private string BuildOperationsDashboardLine(CityMetrics metrics)
        {
            // CITY_SKYLINES_OPERATIONS_DASHBOARD_LINE condenses the right card into a CS-style operations readout.
            if (metrics == null)
            {
                return BuildOverlayLegendText();
            }

            var mode = controller != null ? controller.OverlayMode : OverlayMode.Normal;
            var recommended = RecommendedOverlayMode(metrics);
            var layer = recommended == OverlayMode.Normal ? mode : recommended;
            var issue = CompactCardText(MiniMapPrimaryIssueLabel(metrics), 7);
            var action = CompactCardText(BuildLayerToolActionChain(metrics, mode, layer), 30);
            var forecast = CompactCardText(string.IsNullOrEmpty(metrics.ForecastAction) ? issue : metrics.ForecastAction, 8);
            return "\u56fe\u5c42 " + OverlayLabel(layer)
                + " \u538b" + OverlayPressureScore(layer, metrics)
                + " / \u4e3b" + issue
                + " / " + action
                + " / \u9884" + metrics.ForecastRisk + ":" + forecast
                + " / \u70ed" + lastMiniMapSevereSamples + "+" + lastMiniMapWarningSamples;
        }

        private static string BuildEconomyTrendText(CityMetrics metrics)
        {
            // CITY_ECONOMY_TREND_READOUT makes cash runway visible inside the compact advisor line.
            if (metrics == null)
            {
                return "\u8d22--";
            }

            if (metrics.NetIncome >= 0)
            {
                return "\u8d22" + FormatSigned(metrics.NetIncome) + "/\u7a33";
            }

            return "\u8d22" + FormatSigned(metrics.NetIncome) + "/" + CashRunwayStatus(metrics);
        }

        private static string BuildForecastPressureLine(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u9884--";
            }

            return "\u9884" + metrics.ForecastRisk + ":" + CompactCardText(string.IsNullOrEmpty(metrics.ForecastAction) ? MiniMapPrimaryIssueLabel(metrics) : metrics.ForecastAction, 7);
        }

        private static string BuildDemandForecastLine(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u9700--";
            }

            var focus = string.IsNullOrEmpty(metrics.DemandFocus) ? "\u5e73\u7a33" : metrics.DemandFocus;
            return "\u9700" + metrics.DemandUrgency + ":" + CompactCardText(focus, 5);
        }

        private static string BuildUpgradeDiagnosisLine(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u5347--";
            }

            if (metrics.BuildingUpgradeReadyCount > 0 || metrics.BuildingUpgradeBlockedCount > 0)
            {
                var action = string.IsNullOrEmpty(metrics.BuildingUpgradeReadinessAction)
                    ? GrowthOperationsText(metrics)
                    : metrics.BuildingUpgradeReadinessAction;
                return "\u5347" + metrics.BuildingUpgradeReadyCount + "/" + metrics.BuildingUpgradeBlockedCount + ":" + CompactCardText(action, 6);
            }

            return "\u6210" + metrics.DevelopmentQuality + "/Lv" + metrics.MaxBuildingLevel;
        }

        private static string BuildBudgetPressureLine(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u8d22--";
            }

            var pressure = Mathf.Max(metrics.BudgetStress, metrics.DebtPressure);
            if (metrics.NetIncome < 0)
            {
                pressure = Mathf.Max(pressure, 58);
            }

            return "\u8d22" + pressure + ":" + BuildEconomyTrendText(metrics);
        }

        private string BuildLayerToolActionChain(CityMetrics metrics, OverlayMode activeMode, OverlayMode targetMode)
        {
            // CITY_SKYLINES_LAYER_TO_TOOL_CHAIN turns diagnosis into an immediate playable action.
            var best = BestRecommendedToolBinding(metrics);
            if (best != null)
            {
                return "\u770b" + OverlayLabel(targetMode)
                    + "->\u70b9" + CompactCardText(ToolBindingLabel(best), 5)
                    + "->" + CompactCardText(ToolPlacementHint(best, metrics), 8)
                    + " \u56e0" + CompactCardText(ToolRecommendationDriverLabel(best, metrics), 7);
            }

            var active = ActiveToolBinding();
            if (active != null)
            {
                return "\u770b" + OverlayLabel(activeMode)
                    + "->\u7528" + CompactCardText(ToolBindingLabel(active), 5)
                    + "->" + CompactCardText(ToolPlacementHint(active, metrics), 8);
            }

            return targetMode != activeMode
                ? "\u770b" + OverlayLabel(targetMode) + "->\u5b9a\u4f4d\u70ed\u533a"
                : "\u770b" + OverlayLabel(activeMode) + "->\u9009\u70ed\u533a\u8bca\u65ad";
        }

        private static string GrowthOperationsText(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return "\u6210\u957f--";
            }

            if (metrics.BuildingUpgradeReadyCount > 0 || metrics.BuildingUpgradeBlockedCount > 0)
            {
                return "\u5347\u5019" + metrics.BuildingUpgradeReadyCount + "/\u963b" + metrics.BuildingUpgradeBlockedCount;
            }

            return "\u6210\u957f" + metrics.DevelopmentQuality + "/Lv" + metrics.MaxBuildingLevel;
        }

        private string ToolShortcutHintText()
        {
            // CITY_TOOL_SHORTCUT_HINTS expose existing keyboard controls without adding buttons to the dock.
            if (interaction == null)
            {
                return string.Empty;
            }

            if (interaction.ToolMode == CityToolMode.BuildRoad) return "\u952e1";
            if (interaction.ToolMode == CityToolMode.UpgradeRoad) return "\u952eU";
            if (interaction.ToolMode == CityToolMode.Demolish) return "\u952eDel";
            if (interaction.ToolMode == CityToolMode.Inspect) return "\u952eI";

            if (interaction.ToolMode == CityToolMode.ZonePaint)
            {
                if (interaction.SelectedZone == ZoneType.Residential) return "\u952e2";
                if (interaction.SelectedZone == ZoneType.Commercial) return "\u952e3";
                if (interaction.SelectedZone == ZoneType.Industrial) return "\u952e4";
                return string.Empty;
            }

            if (interaction.ToolMode == CityToolMode.BuildBuilding)
            {
                if (interaction.SelectedBuildingId == "residential_pod") return "\u952e5";
                if (interaction.SelectedBuildingId == "market_corner") return "\u952e6";
                if (interaction.SelectedBuildingId == "pocket_park") return "\u952e7";
            }

            return string.Empty;
        }

        private string CompactOperationsStatusText()
        {
            if (controller == null || controller.Metrics == null)
            {
                return string.Empty;
            }

            var metrics = controller.Metrics;
            return "\u7a0e" + TaxLabel(metrics.TaxLevel)
                + " \u670d" + BudgetLabel(metrics.ServiceBudgetLevel)
                + " \u653f" + (metrics.ActivePolicies != null ? metrics.ActivePolicies.Count : 0);
        }

        private string BuildToolRecommendationHint(CityMetrics metrics)
        {
            var activeBinding = ActiveToolBinding();
            if (activeBinding != null)
            {
                return "\u63a8\u8350\u5de5\u5177:" + ToolBindingLabel(activeBinding)
                    + " -> " + ToolPlacementHint(activeBinding, metrics)
                    + " | \u7406\u7531:" + ToolRecommendationDriverLabel(activeBinding, metrics);
            }

            var strongest = StrongestToolRecommendationScore(metrics);
            if (strongest < 72)
            {
                return string.Empty;
            }

            var best = BestRecommendedToolBinding(metrics, strongest);
            return best == null
                ? string.Empty
                : "\u63a8\u8350\u5de5\u5177:" + ToolBindingLabel(best)
                    + " -> " + ToolPlacementHint(best, metrics)
                    + " | \u7406\u7531:" + ToolRecommendationDriverLabel(best, metrics);
        }

        private ToolButtonBinding BestRecommendedToolBinding(CityMetrics metrics)
        {
            return BestRecommendedToolBinding(metrics, StrongestToolRecommendationScore(metrics));
        }

        private ToolButtonBinding BestRecommendedToolBinding(CityMetrics metrics, int strongest)
        {
            if (strongest < 72)
            {
                return null;
            }

            ToolButtonBinding best = null;
            var bestScore = 0;
            for (var i = 0; i < toolButtons.Count; i += 1)
            {
                var binding = toolButtons[i];
                var score = ToolRecommendationScoreWithSelectedTile(binding, metrics);
                if (!IsToolActive(binding) && IsDemandRecommendedTool(score, strongest) && score > bestScore)
                {
                    best = binding;
                    bestScore = score;
                }
            }

            return best;
        }

        private ToolButtonBinding ActiveToolBinding()
        {
            for (var i = 0; i < toolButtons.Count; i += 1)
            {
                if (IsToolActive(toolButtons[i]))
                {
                    return toolButtons[i];
                }
            }

            return null;
        }

        private static string ToolPlacementHint(ToolButtonBinding binding, CityMetrics metrics)
        {
            // CITY_SKYLINES_RECOMMENDED_ACTION_HINT turns pressure highlights into an immediate player action.
            if (binding == null)
            {
                return string.Empty;
            }

            if (binding.ToolMode == CityToolMode.BuildRoad)
            {
                return "\u62d6\u8fde\u7a7a\u767d\u5730";
            }

            if (binding.ToolMode == CityToolMode.UpgradeRoad)
            {
                return "\u70b9\u7ea2\u6a59\u8def\u6bb5";
            }

            if (binding.ToolMode == CityToolMode.ZonePaint)
            {
                return "\u62d6 3-5 \u683c\u5e73\u5730";
            }

            if (binding.ToolMode == CityToolMode.Demolish)
            {
                return "\u70b9\u95f2\u7f6e\u683c";
            }

            var id = binding.BuildingId;
            if (id == "parking_garage") return "\u9760\u5546\u529e\u9ad8\u4ea4\u901a";
            if (id == "rain_garden") return "\u9760\u96e8\u6d2a\u4f4e\u503c\u533a";
            if (id == "road_maintenance_depot") return "\u9760\u5fd9\u78cc\u8def\u7f51";
            if (id == "emergency_shelter") return "\u63a5\u8def\u8986\u76d6\u5c45\u6c11";
            if (IsTransitOrLogisticsTool(id)) return "\u9760\u4e3b\u8def\u6216\u5546\u5de5\u533a";
            if (IsUtilityTool(id)) return "\u9760\u8def\u653e\u7a7a\u5730";
            if (IsServiceTool(id))
            {
                return metrics != null && !string.IsNullOrEmpty(metrics.ServiceGapFocus)
                    ? "\u9760\u7f3a" + CompactCardText(metrics.ServiceGapFocus, 4) + "\u4f4f\u533a"
                    : "\u9760\u4f4f\u533a\u8def\u53e3";
            }

            if (IsIndustrialTool(id)) return "\u9760\u8d27\u8fd0/\u5de5\u4e1a";
            if (IsCommercialTool(id)) return "\u9760\u4f4f\u5b85\u4e3b\u8def";
            if (IsResidentialTool(id)) return "\u9760\u516c\u56ed\u670d\u52a1";
            return "\u9760\u8def\u653e\u7f6e";
        }

        private string BuildPreviewText()
        {
            if (interaction != null && interaction.ToolMode == CityToolMode.Inspect)
            {
                return BuildTileInspectorText();
            }

            var preview = controller.CurrentPreview;
            if (preview == null)
            {
                return BuildTileInspectorText();
            }

            // ACTION_PREVIEW_COMPACT_DIAGNOSIS keeps the preview panel readable beside the dense tool grid.
            var action = preview.Ok
                ? (string.IsNullOrEmpty(preview.ConfirmLabel) ? "\u53ef\u6267\u884c" : "\u53ef\u5efa " + preview.ConfirmLabel)
                : "\u53d7\u963b";
            var text = CompactPreviewLine(preview.Title + "  " + action, 34);
            var detail = FirstPreviewDetailLine(preview);
            if (string.IsNullOrEmpty(detail))
            {
                detail = FirstObjectiveHintLine(BuildTileInspectorText());
            }

            return text + "\n" + CompactPreviewLine(detail, 42);
        }

        private void RefreshCommandFeedbackPulse()
        {
            if (controller != null && seenRuntimeSessionVersion != controller.RuntimeSessionVersion)
            {
                seenRuntimeSessionVersion = controller.RuntimeSessionVersion;
                seenCommandFeedbackVersion = controller.CommandFeedbackVersion;
                lastCommandFeedbackSucceeded = controller.LastCommandSucceeded;
                commandFeedbackText = controller.LastCommandFeedbackText;
                commandFeedbackPulseTimer = 0f;
                ClearPendingAdvisorAdoption();
            }

            if (controller == null || seenCommandFeedbackVersion == controller.CommandFeedbackVersion)
            {
                ExpirePendingAdvisorAdoption();
                return;
            }

            // COMMAND_FEEDBACK_PULSE gives build/zone/road commands immediate HUD confirmation.
            seenCommandFeedbackVersion = controller.CommandFeedbackVersion;
            lastCommandFeedbackSucceeded = controller.LastCommandSucceeded;
            // COMMAND_FEEDBACK_DETAIL_SUMMARY keeps the pulse text tied to the committed command.
            commandFeedbackText = controller.LastCommandFeedbackText;
            TryConfirmPendingAdvisorAdoption();
            commandFeedbackPulseTimer = 0.65f;
        }

        private void ArmPendingAdvisorAdoption(string advisorType)
        {
            if (string.IsNullOrEmpty(advisorType) || controller == null)
            {
                return;
            }

            pendingAdvisorType = advisorType;
            pendingAdvisorFeedbackVersion = controller.CommandFeedbackVersion;
            pendingAdvisorExpireTime = Time.time + PendingAdvisorAdoptionLifetime;
        }

        private void TryConfirmPendingAdvisorAdoption()
        {
            if (string.IsNullOrEmpty(pendingAdvisorType))
            {
                return;
            }

            if (Time.time > pendingAdvisorExpireTime)
            {
                ClearPendingAdvisorAdoption();
                return;
            }

            if (controller == null || controller.CommandFeedbackVersion <= pendingAdvisorFeedbackVersion)
            {
                return;
            }

            if (controller == null || !controller.LastCommandWasCommit)
            {
                return;
            }

            if (!lastCommandFeedbackSucceeded)
            {
                return;
            }

            if (!AdvisorAdoptionMatchesCommitKind(pendingAdvisorType, controller.LastCommandCommitKind))
            {
                ClearPendingAdvisorAdoption();
                return;
            }

            CityHudViewModelSmartAdvisor.RecordAdvisorAdoption(pendingAdvisorType);
            ClearPendingAdvisorAdoption();
        }

        private void ExpirePendingAdvisorAdoption()
        {
            if (!string.IsNullOrEmpty(pendingAdvisorType) && Time.time > pendingAdvisorExpireTime)
            {
                ClearPendingAdvisorAdoption();
            }
        }

        private void ClearPendingAdvisorAdoption()
        {
            pendingAdvisorType = string.Empty;
            pendingAdvisorFeedbackVersion = -1;
            pendingAdvisorExpireTime = 0f;
        }

        private static bool AdvisorAdoptionMatchesCommitKind(string advisorType, CityGameController.CommandCommitKind commitKind)
        {
            switch (advisorType)
            {
                case "ROAD_HIERARCHY_ADVISOR":
                case "COMMUTE_CORRIDOR_ADVISOR":
                    return commitKind == CityGameController.CommandCommitKind.Road;
                case "RISK_FORECAST_ADVISOR":
                case "SERVICE_GAP_ADVISOR":
                    return commitKind == CityGameController.CommandCommitKind.Building;
                case "BUDGET_BREAKDOWN_ADVISOR":
                    return commitKind == CityGameController.CommandCommitKind.Management;
                default:
                    return false;
            }
        }

        private string BuildCommandFeedbackPulseText(string text)
        {
            var detail = string.IsNullOrEmpty(commandFeedbackText) ? text : commandFeedbackText;
            if (IsNeutralCommandFeedback(detail))
            {
                return string.IsNullOrEmpty(detail) ? BuildTileInspectorText() : detail;
            }

            var prefix = lastCommandFeedbackSucceeded ? "\u5b8c\u6210  " : "\u53d7\u963b  ";
            return prefix + (string.IsNullOrEmpty(detail) ? BuildTileInspectorText() : detail);
        }

        private Color32 CommandFeedbackPreviewColor()
        {
            if (commandFeedbackPulseTimer > 0f)
            {
                if (IsNeutralCommandFeedback(commandFeedbackText))
                {
                    return new Color32(65, 169, 184, 255);
                }

                return lastCommandFeedbackSucceeded ? new Color32(63, 146, 96, 255) : new Color32(224, 126, 49, 255);
            }

            return controller.CurrentPreview == null || controller.CurrentPreview.Ok
                ? new Color32(43, 64, 70, 255)
                : new Color32(224, 126, 49, 255);
        }

        private static bool IsNeutralCommandFeedback(string text)
        {
            return !string.IsNullOrEmpty(text) && text.StartsWith("\u9884\u89c8 ", System.StringComparison.Ordinal);
        }

        private static string FirstPreviewDetailLine(ConstructionPreview preview)
        {
            if (preview == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(preview.SiteDiagnosis))
            {
                return preview.SiteDiagnosis;
            }

            return preview.Lines != null && preview.Lines.Count > 0 ? preview.Lines[0] : string.Empty;
        }

        private static string CompactPreviewLine(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return string.IsNullOrEmpty(value) ? string.Empty : value;
            }

            return value.Substring(0, Mathf.Max(1, maxLength - 1)) + "...";
        }

        private string BuildOverlayLegendText()
        {
            // TILE_INSPECTOR_OVERLAY_LEGEND gives every overlay a readable value scale.
            if (controller == null)
            {
                return "\u56fe\u5c42\uff1a--";
            }

            var mode = controller.OverlayMode;
            var pressure = OverlayPressureText(mode, controller.Metrics) + OverlayRecommendationText(mode, controller.Metrics);
            if (mode == OverlayMode.Traffic) return "\u56fe\u5c42\uff1a\u4ea4\u901a  \u7eff\u4f4e/\u9ec4\u4e2d/\u7ea2\u9ad8  " + pressure;
            if (mode == OverlayMode.Pollution) return "\u56fe\u5c42\uff1a\u6c61\u67d3  \u7eff\u4f4e/\u9ec4\u4e2d/\u7d2b\u9ad8  " + pressure;
            if (mode == OverlayMode.Zoning) return "\u56fe\u5c42\uff1a\u5206\u533a  \u7528\u5730\u7740\u8272  " + pressure;
            if (mode == OverlayMode.Services) return "\u56fe\u5c42\uff1a\u670d\u52a1  \u6697\u4f4e/\u7d2b\u4e2d/\u7eff\u9ad8  " + pressure;
            if (mode == OverlayMode.Transit) return "\u56fe\u5c42\uff1a\u516c\u4ea4  \u6697\u4f4e/\u84dd\u4e2d/\u9752\u9ad8  " + pressure;
            if (mode == OverlayMode.LandValue) return "\u56fe\u5c42\uff1a\u5730\u4ef7  \u84dd\u4f4e/\u7eff\u4e2d/\u9ec4\u9ad8  " + pressure;
            if (mode == OverlayMode.Waste) return "\u56fe\u5c42\uff1a\u56de\u6536  \u68d5\u4f4e/\u9752\u4e2d/\u7eff\u9ad8  " + pressure;
            if (mode == OverlayMode.Logistics) return "\u56fe\u5c42\uff1a\u8d27\u8fd0  \u6697\u4f4e/\u6a59\u4e2d/\u91d1\u9ad8  " + pressure;
            if (mode == OverlayMode.Utilities) return "\u56fe\u5c42\uff1a\u6c34\u7535  \u84dd\u7a33/\u7ea2\u77ed\u7f3a  " + pressure;
            if (mode == OverlayMode.Communications) return "\u56fe\u5c42\uff1a\u901a\u4fe1  \u6697\u4f4e/\u84dd\u4e2d/\u9752\u9ad8  " + pressure;
            if (mode == OverlayMode.RoadSafety) return "\u56fe\u5c42\uff1a\u8def\u5b89  \u7ea2\u4f4e/\u6a59\u4e2d/\u7eff\u9ad8  " + pressure;
            if (mode == OverlayMode.Parking) return "\u56fe\u5c42\uff1a\u505c\u8f66  \u6697\u4f4e/\u9ec4\u4e2d/\u7eff\u9ad8  " + pressure;
            if (mode == OverlayMode.Stormwater) return "\u56fe\u5c42\uff1a\u96e8\u6d2a  \u84dd\u4f4e/\u9752\u4e2d/\u7eff\u9ad8  " + pressure;
            return "\u56fe\u5c42\uff1a\u666e\u901a  \u9053\u8def/\u5efa\u7b51/\u5730\u5f62  " + pressure;
        }

        private static string OverlayPressureText(OverlayMode mode, CityMetrics metrics)
        {
            // CITY_SKYLINES_OVERLAY_PRESSURE_LABEL ties every information layer to live city pressure.
            if (metrics == null)
            {
                return "\u538b--";
            }

            if (mode == OverlayMode.Traffic) return "\u538b" + metrics.RoadBottleneckPressure;
            if (mode == OverlayMode.Pollution) return "\u538b" + Mathf.Max(metrics.Pollution, metrics.NoiseStress);
            if (mode == OverlayMode.Zoning) return "\u95f2" + metrics.IdleZoneTiles + "/\u51b2" + metrics.LandUseConflict;
            if (mode == OverlayMode.Services) return "\u7f3a" + metrics.ServiceGapPressure;
            if (mode == OverlayMode.Transit) return "\u8986" + metrics.TransitCoverage + "/\u5019" + metrics.TransitWaitPressure;
            if (mode == OverlayMode.LandValue) return "\u5747" + metrics.AverageLandValue + "/\u8d28" + metrics.DevelopmentQuality;
            if (mode == OverlayMode.Waste) return "\u8986" + metrics.WasteCoverage + "/\u6ee1" + metrics.WasteUtilization;
            if (mode == OverlayMode.Logistics) return "\u8986" + metrics.LogisticsCoverage + "/\u8d27" + metrics.GoodsBalance;
            if (mode == OverlayMode.Utilities) return "\u7a33" + metrics.UtilityReliability + "/\u6ee1" + metrics.UtilityUtilization;
            if (mode == OverlayMode.Communications) return "\u8986" + metrics.CommunicationCoverage + "/\u90ae" + metrics.MailCoverage;
            if (mode == OverlayMode.RoadSafety) return "\u5b89" + metrics.RoadSafety + "/\u9669" + metrics.AccidentRisk;
            if (mode == OverlayMode.Parking) return "\u538b" + metrics.ParkingPressure + "/\u8986" + metrics.ParkingCoverage;
            if (mode == OverlayMode.Stormwater) return "\u6d2a" + metrics.FloodRisk + "/\u97e7" + metrics.StormwaterResilience;
            return "\u8bc4" + metrics.CityScore + "/\u9669" + metrics.ForecastRisk;
        }

        private string BuildTileInspectorText()
        {
            if (interaction == null || controller == null || !interaction.HasSelectedTile)
            {
                return "\u5730\u5757\uff1a--  " + OverlayLabel(controller != null ? controller.OverlayMode : OverlayMode.Normal);
            }

            var pos = interaction.SelectedTile;
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null)
            {
                return "\u5730\u5757\uff1a" + pos.X + "," + pos.Y + "  --";
            }

            var firstLine = "\u5730\u5757\uff1a" + pos.X + "," + pos.Y
                + "  " + TerrainLabel(tile.Terrain)
                + "/" + ZoneLabel(tile.Zone)
                + "  " + TileOccupancyText(pos, tile)
                + "  " + TileOverlayValueText(controller.OverlayMode, tile)
                + "  " + TileRiskLabel(tile, controller.Metrics);
            var actionLine = CompactPreviewLine(BuildTileActionDiagnosis(controller.OverlayMode, tile), 28);
            var contextLine = CompactPreviewLine(TilePlacementContext(controller.OverlayMode, tile, controller.Metrics) + TileGrowthInspectorText(pos, tile), 24);
            var orderLine = CompactPreviewLine(SelectedTileOrderSubtitle(pos, tile, controller.Metrics), 28);
            return firstLine + "\n"
                + "\u4e3b:" + DominantTileIssueLabel(tile, controller.Metrics)
                + "  "
                + actionLine
                + contextLine
                + (string.IsNullOrEmpty(orderLine) ? string.Empty : "  \u5355:" + orderLine);
        }

        private string TileOccupancyText(GridPos pos, TileData tile)
        {
            if (!string.IsNullOrEmpty(tile.BuildingId))
            {
                var building = controller != null ? controller.GetPlacedBuildingAt(pos.X, pos.Y) : null;
                var definition = building != null ? controller.GetBuildingDefinition(building.ConfigId) : null;
                var label = definition != null && !string.IsNullOrEmpty(definition.Name)
                    ? definition.Name
                    : BuildingLabel(building != null ? building.ConfigId : tile.BuildingId);
                return "\u5efa\u7b51:" + label + (building != null ? " Lv" + building.Level : string.Empty);
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                var road = controller != null ? controller.GetRoadAt(pos.X, pos.Y) : null;
                if (road != null)
                {
                    return RoadTierLabel(road.Tier) + " " + road.Load + "/" + road.Capacity;
                }

                return "\u9053\u8def";
            }

            return "\u7a7a\u5730";
        }

        private string TileOverlayValueText(OverlayMode mode, TileData tile)
        {
            // TILE_OVERLAY_SHORT_GAP_LABELS keeps selected-tile readouts compact but actionable.
            if (mode == OverlayMode.Traffic) return "\u4ea4\u901a" + tile.Traffic + TrafficStressLabel(tile);
            if (mode == OverlayMode.Pollution) return "\u6c61" + tile.Pollution + "/\u566a" + tile.Noise + PollutionStressLabel(tile);
            if (mode == OverlayMode.Zoning) return "\u5206\u533a:" + ZoneLabel(tile.Zone);
            if (mode == OverlayMode.Services) return "\u670d\u52a1" + ServiceAccessValue(tile) + ServiceWeaknessLabel(tile);
            if (mode == OverlayMode.Transit) return "\u516c\u4ea4" + tile.TransitAccess + LowAccessLabel(tile.TransitAccess);
            if (mode == OverlayMode.LandValue) return "\u5730\u4ef7" + tile.LandValue + LandValueLabel(tile);
            if (mode == OverlayMode.Waste) return "\u56de\u6536" + tile.WasteAccess + LowAccessLabel(tile.WasteAccess);
            if (mode == OverlayMode.Logistics) return "\u8d27\u8fd0" + tile.LogisticsAccess + LowAccessLabel(tile.LogisticsAccess);
            if (mode == OverlayMode.Utilities) return UtilityOverlayValueText();
            if (mode == OverlayMode.Communications) return "\u901a\u4fe1" + Mathf.Max(tile.CommunicationAccess, tile.MailAccess) + CommunicationWeaknessLabel(tile);
            if (mode == OverlayMode.RoadSafety) return "\u517b\u62a4" + tile.RoadMaintenanceAccess + LowAccessLabel(tile.RoadMaintenanceAccess);
            if (mode == OverlayMode.Parking) return "\u505c\u8f66" + tile.ParkingAccess + LowAccessLabel(tile.ParkingAccess);
            if (mode == OverlayMode.Stormwater) return "\u96e8\u6d2a" + tile.StormwaterAccess + LowAccessLabel(tile.StormwaterAccess);
            return "\u5730\u4ef7" + tile.LandValue + "/\u4ea4" + tile.Traffic;
        }

        private static string TileRiskLabel(TileData tile, CityMetrics metrics)
        {
            // CITY_SKYLINES_SELECTED_TILE_RISK_BADGE gives the inspector a compact information-layer severity chip.
            var severity = MiniMapIssueSeverity(tile, metrics);
            if (severity >= 34) return "\u98ce\u9669:\u4e25" + severity;
            if (severity >= 18) return "\u98ce\u9669:\u6ce8" + severity;
            return "\u98ce\u9669:\u7a33";
        }

        private string UtilityOverlayValueText()
        {
            if (controller == null || controller.Metrics == null)
            {
                return "\u6c34\u7535--";
            }

            var metrics = controller.Metrics;
            var stress = metrics.UtilityReliability < 95 || metrics.UtilityUtilization > 115 || metrics.WastewaterUtilization > 115;
            return "\u6c34\u7535\u7a33" + metrics.UtilityReliability + (stress ? " \u9ad8\u538b" : string.Empty);
        }

        private static string TrafficStressLabel(TileData tile)
        {
            if (tile.Traffic >= 70) return " \u5835";
            if (tile.Traffic >= 45) return " \u5fd9";
            return string.Empty;
        }

        private static string PollutionStressLabel(TileData tile)
        {
            return Mathf.Max(tile.Pollution, tile.Noise) >= 45 ? " \u9ad8" : string.Empty;
        }

        private static string LandValueLabel(TileData tile)
        {
            return tile.LandValue < 35 ? " \u4f4e" : string.Empty;
        }

        private static string LowAccessLabel(int value)
        {
            return value < 24 ? " \u7f3a" : string.Empty;
        }

        private static string CommunicationWeaknessLabel(TileData tile)
        {
            if (tile.CommunicationAccess < 24) return " \u7f3a\u4fe1";
            if (tile.MailAccess < 24) return " \u7f3a\u90ae";
            return string.Empty;
        }

        private static string ServiceWeaknessLabel(TileData tile)
        {
            var label = "\u56ed";
            var value = tile.ParkAccess;
            SetWeakestService(ref label, ref value, "\u533b", tile.HealthAccess);
            SetWeakestService(ref label, ref value, "\u5b66", tile.EducationAccess);
            SetWeakestService(ref label, ref value, "\u706b", tile.FireProtectionAccess);
            SetWeakestService(ref label, ref value, "\u8b66", Mathf.Max(tile.SafetyAccess, tile.SecurityAccess));
            SetWeakestService(ref label, ref value, "\u6000", tile.DeathcareAccess);
            return value < 24 ? " \u7f3a" + label : string.Empty;
        }

        private static void SetWeakestService(ref string label, ref int value, string candidateLabel, int candidateValue)
        {
            if (candidateValue < value)
            {
                label = candidateLabel;
                value = candidateValue;
            }
        }

        private static int DominantTileIssueId(TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return 0;
            }

            if (tile.Terrain == TerrainType.Water)
            {
                return 1;
            }

            var hasUse = TileHasUse(tile);
            var bestId = 0;
            var bestScore = 0;
            ConsiderTileIssue(ref bestId, ref bestScore, 2, !string.IsNullOrEmpty(tile.RoadId) ? tile.Traffic : (hasUse ? tile.Traffic - 10 : 0));
            ConsiderTileIssue(ref bestId, ref bestScore, 3, hasUse ? 100 - ServiceAccessValue(tile) : 0);
            ConsiderTileIssue(ref bestId, ref bestScore, 4, hasUse && tile.Traffic >= 8 ? 70 - tile.TransitAccess : 0);
            ConsiderTileIssue(ref bestId, ref bestScore, 5, hasUse && tile.Traffic >= 8 ? 70 - tile.ParkingAccess : 0);
            var utilityPressure = metrics != null && (metrics.UtilityReliability < 90 || metrics.UtilityUtilization > 115 || metrics.FloodRisk >= 45) ? 64 : 0;
            ConsiderTileIssue(ref bestId, ref bestScore, 6, utilityPressure);
            ConsiderTileIssue(ref bestId, ref bestScore, 7, hasUse ? 52 - tile.LandValue : 0);
            ConsiderTileIssue(ref bestId, ref bestScore, 8, Mathf.Max(tile.Pollution, tile.Noise));
            var openPlain = tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId) && string.IsNullOrEmpty(tile.RoadId) && tile.Terrain == TerrainType.Plain;
            ConsiderTileIssue(ref bestId, ref bestScore, 9, openPlain && metrics != null && metrics.DemandUrgency >= 45 ? metrics.DemandUrgency : 0);
            ConsiderTileIssue(ref bestId, ref bestScore, 10, hasUse ? 72 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess) : 0);
            ConsiderTileIssue(ref bestId, ref bestScore, 11, hasUse ? Mathf.Max(68 - tile.LogisticsAccess, 68 - tile.WasteAccess) : 0);

            return bestScore >= 28 ? bestId : 0;
        }

        private static void ConsiderTileIssue(ref int bestId, ref int bestScore, int id, int score)
        {
            if (score > bestScore)
            {
                bestId = id;
                bestScore = score;
            }
        }

        private static string DominantTileIssueLabel(TileData tile, CityMetrics metrics)
        {
            var id = DominantTileIssueId(tile, metrics);
            if (id == 1) return "\u6c34\u9762";
            if (id == 2) return "\u4ea4\u901a" + tile.Traffic;
            if (id == 3) return "\u670d\u52a1" + ServiceAccessValue(tile);
            if (id == 4) return "\u516c\u4ea4" + tile.TransitAccess;
            if (id == 5) return "\u505c\u8f66" + tile.ParkingAccess;
            if (id == 6) return "\u6c34\u7535/\u96e8\u6d2a";
            if (id == 7) return "\u5730\u4ef7" + tile.LandValue;
            if (id == 8) return "\u6c61\u566a" + Mathf.Max(tile.Pollution, tile.Noise);
            if (id == 9) return "\u672a\u89c4\u5212";
            if (id == 10) return "\u901a\u4fe1/\u90ae\u653f";
            if (id == 11) return "\u8d27\u8fd0/\u56de\u6536";
            return "\u7a33";
        }

        private string BuildTileActionDiagnosis(OverlayMode mode, TileData tile)
        {
            // CITY_ACTIONABLE_TILE_DIAGNOSIS keeps the inspector tied to the active planning layer.
            if (tile.Terrain == TerrainType.Water)
            {
                return "\u8bca\u65ad:\u6c34\u9762\u4fdd\u7559";
            }

            var metrics = controller != null ? controller.Metrics : null;
            var hasUse = TileHasUse(tile);
            if (mode == OverlayMode.Traffic)
            {
                if (tile.Traffic >= 70) return "\u8bca\u65ad:\u9053\u8def\u6ee1\u8f7d -> \u9009\u5347\u8def\u70b9\u6b64\u8def\u6bb5";
                if (tile.Traffic >= 45) return "\u8bca\u65ad:\u4ea4\u901a\u504f\u9ad8 -> \u62d6\u5e73\u884c\u8def\u5206\u6d41";
            }

            if (mode == OverlayMode.Pollution && Mathf.Max(tile.Pollution, tile.Noise) >= 45)
            {
                return "\u8bca\u65ad:\u6c61\u67d3\u566a\u58f0 -> \u52a0\u7f13\u51b2/\u8fdc\u79bb\u4f4f\u5b85";
            }

            if (mode == OverlayMode.Services && hasUse && ServiceAccessValue(tile) < 26)
            {
                return "\u8bca\u65ad:\u670d\u52a1\u7a7a\u767d -> " + ServiceTileActionHint(tile, metrics);
            }

            if (mode == OverlayMode.Transit && hasUse && tile.TransitAccess < 24 && tile.Traffic >= 8)
            {
                return "\u8bca\u65ad:\u7f3a\u516c\u4ea4 -> \u653e\u516c\u4ea4/\u5730\u94c1";
            }

            if (mode == OverlayMode.Waste && hasUse && tile.WasteAccess < 24)
            {
                return "\u8bca\u65ad:\u7f3a\u56de\u6536 -> \u653e\u56de\u6536/\u5783\u573e\u7535";
            }

            if (mode == OverlayMode.Logistics && hasUse && tile.LogisticsAccess < 24 && tile.Traffic >= 8)
            {
                return "\u8bca\u65ad:\u7f3a\u8d27\u8fd0 -> \u653e\u4ed3\u50a8/\u8d27\u8fd0";
            }

            if (mode == OverlayMode.Communications && hasUse && Mathf.Max(tile.CommunicationAccess, tile.MailAccess) < 24)
            {
                return "\u8bca\u65ad:\u7f3a\u901a\u4fe1 -> \u653e\u901a\u4fe1/\u90ae\u653f";
            }

            if (mode == OverlayMode.RoadSafety && tile.RoadMaintenanceAccess < 24 && tile.Traffic > 0)
            {
                return "\u8bca\u65ad:\u7f3a\u517b\u62a4 -> \u653e\u9053\u8def\u517b\u62a4";
            }

            if (mode == OverlayMode.Parking && hasUse && tile.ParkingAccess < 24 && tile.Traffic >= 8)
            {
                return "\u8bca\u65ad:\u7f3a\u505c\u8f66 -> \u653e\u505c\u8f66\u697c";
            }

            if (mode == OverlayMode.Stormwater && tile.StormwaterAccess < 24)
            {
                return "\u8bca\u65ad:\u96e8\u6d2a\u8584\u5f31 -> \u653e\u96e8\u6c34\u56ed";
            }

            if (mode == OverlayMode.Utilities && controller != null && controller.Metrics != null && controller.Metrics.UtilityReliability < 90)
            {
                return "\u8bca\u65ad:\u6c34\u7535\u7d27\u5f20 -> " + UtilityTileActionHint(metrics);
            }

            if (mode == OverlayMode.LandValue && hasUse && tile.LandValue < 35)
            {
                return "\u8bca\u65ad:\u5730\u4ef7\u4f4e -> \u8865\u670d\u52a1/\u516c\u56ed";
            }

            if (mode == OverlayMode.Zoning
                && tile.Terrain == TerrainType.Plain
                && tile.Zone == ZoneType.None
                && string.IsNullOrEmpty(tile.BuildingId)
                && string.IsNullOrEmpty(tile.RoadId))
            {
                return "\u8bca\u65ad:\u53ef\u89c4\u5212 -> " + OpenTileZoningHint(metrics);
            }

            if (hasUse && tile.Traffic >= 70) return "\u8bca\u65ad:\u9053\u8def\u6ee1\u8f7d -> \u9009\u5347\u8def\u70b9\u6b64\u8def\u6bb5";
            if (hasUse && ServiceAccessValue(tile) < 22) return "\u8bca\u65ad:\u670d\u52a1\u7a7a\u767d -> " + ServiceTileActionHint(tile, metrics);
            if (tile.LandValue < 28) return "\u8bca\u65ad:\u5730\u4ef7\u4f4e -> \u8865\u670d\u52a1/\u516c\u56ed";
            return "\u8bca\u65ad:\u8fd0\u884c\u53ef\u63a7";
        }

        private static string TilePlacementContext(OverlayMode mode, TileData tile, CityMetrics metrics)
        {
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                return tile.Traffic >= 58 ? " / \u4f4d:\u74f6\u9888\u8def\u6bb5" : " / \u4f4d:\u8fde\u901a\u8f74";
            }

            if (mode == OverlayMode.Zoning && tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId))
            {
                return metrics != null && metrics.Demand != null && metrics.Demand.Industrial > Mathf.Max(metrics.Demand.Residential, metrics.Demand.Commercial)
                    ? " / \u4f4d:\u4e3b\u8def\u5916\u7f18"
                    : " / \u4f4d:\u8d34\u8def\u6210\u7247";
            }

            if (mode == OverlayMode.Services || (TileHasUse(tile) && ServiceAccessValue(tile) < 28))
            {
                return " / \u4f4d:\u670d\u52a1\u7f3a\u53e3\u4e2d\u5fc3";
            }

            if (mode == OverlayMode.Transit || mode == OverlayMode.Parking)
            {
                return " / \u4f4d:\u8def\u53e3/\u9ad8\u5ba2\u6d41";
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater)
            {
                return " / \u4f4d:\u9760\u8def\u4f4e\u503c\u5730";
            }

            if (mode == OverlayMode.Logistics)
            {
                return " / \u4f4d:\u4ea7\u4e1a\u8fb9\u754c";
            }

            if (mode == OverlayMode.Communications)
            {
                return " / \u4f4d:\u5546\u529e\u7ec4\u56e2";
            }

            if (tile.LandValue < 35)
            {
                return " / \u4f4d:\u516c\u56ed+\u670d\u52a1\u534a\u5f84";
            }

            return string.Empty;
        }

        private string TileGrowthInspectorText(GridPos pos, TileData tile)
        {
            if (controller == null || tile == null || string.IsNullOrEmpty(tile.BuildingId))
            {
                return string.Empty;
            }

            var building = controller.GetPlacedBuildingAt(pos.X, pos.Y);
            if (building == null)
            {
                return string.Empty;
            }

            if (building.Level >= 3)
            {
                return " / \u6210\u957f:Lv3";
            }

            var score = TileGrowthReadinessScore(tile, building);
            if (score >= 72)
            {
                return " / \u6210\u957f:Lv" + building.Level + " \u53ef\u5347";
            }

            return " / \u6210\u957f:Lv" + building.Level + " " + TileGrowthBlockerLabel(tile, building);
        }

        private static string TileCardDiagnosis(OverlayMode mode, TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return "--";
            }

            if (tile.Terrain == TerrainType.Water)
            {
                return "\u4fdd\u7559\u6c34\u9762";
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                return tile.Traffic >= 58 ? "\u74f6\u9888\u8def\u6bb5 \u5347\u7ea7\u9053\u8def" : "\u8fde\u901a\u8f74 \u89c2\u5bdf\u8f66\u6d41";
            }

            if (mode == OverlayMode.Services || (TileHasUse(tile) && ServiceAccessValue(tile) < 28))
            {
                return "\u670d\u52a1\u7f3a\u53e3 " + CompactCardText(ServiceTileActionHint(tile, metrics), 8);
            }

            if (mode == OverlayMode.Zoning && tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId))
            {
                return "\u53ef\u89c4\u5212 " + CompactCardText(OpenTileZoningHint(metrics), 8);
            }

            if (tile.LandValue < 35 && TileHasUse(tile))
            {
                return "\u5730\u4ef7\u4f4e \u8865\u516c\u56ed/\u670d\u52a1";
            }

            if (mode == OverlayMode.Transit || mode == OverlayMode.Parking)
            {
                return "\u4ea4\u901a\u843d\u70b9 \u8def\u53e3/\u5ba2\u6d41";
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater)
            {
                return "\u8bbe\u65bd\u843d\u70b9 \u9760\u8def\u4f4e\u503c\u5730";
            }

            return "\u8fd0\u884c\u53ef\u63a7";
        }

        private static int TileGrowthReadinessScore(TileData tile, PlacedBuilding building)
        {
            var connected = building != null && !string.IsNullOrEmpty(building.ConnectedRoadId) ? 18 : 0;
            var service = ServiceAccessValue(tile) / 3;
            var transit = tile.TransitAccess / 4;
            var land = tile.LandValue / 2;
            var pollutionPenalty = Mathf.Max(tile.Pollution, tile.Noise) / 3;
            return Mathf.Clamp(connected + service + transit + land - pollutionPenalty, 0, 100);
        }

        private static string TileGrowthBlockerLabel(TileData tile, PlacedBuilding building)
        {
            if (building == null || string.IsNullOrEmpty(building.ConnectedRoadId))
            {
                return "\u5148\u63a5\u8def";
            }

            if (ServiceAccessValue(tile) < 32)
            {
                return "\u670d" + ServiceAccessValue(tile);
            }

            if (tile.TransitAccess < 30)
            {
                return "\u516c" + tile.TransitAccess;
            }

            if (tile.Traffic >= 58)
            {
                return "\u5835" + tile.Traffic;
            }

            if (tile.LandValue < 42)
            {
                return "\u5730" + tile.LandValue;
            }

            if (Mathf.Max(tile.Pollution, tile.Noise) >= 42)
            {
                return "\u6c61\u566a" + Mathf.Max(tile.Pollution, tile.Noise);
            }

            return "\u7b49\u6210\u719f";
        }

        private static string ServiceTileActionHint(TileData tile, CityMetrics metrics)
        {
            // CITY_SKYLINES_TILE_NEXT_STEP_HINTS make inspector diagnoses point to one concrete action.
            var focus = metrics != null ? metrics.ServiceGapFocus : string.Empty;
            if (!string.IsNullOrEmpty(focus) && focus != "\u5747\u8861")
            {
                return "\u9009" + CompactCardText(focus, 4) + "\u8bbe\u65bd\u9760\u8def\u653e";
            }

            var label = "\u516c\u56ed";
            var value = tile.ParkAccess;
            SetWeakestService(ref label, ref value, "\u8bca\u6240", tile.HealthAccess);
            SetWeakestService(ref label, ref value, "\u5b66\u6821", tile.EducationAccess);
            SetWeakestService(ref label, ref value, "\u6d88\u9632", tile.FireProtectionAccess);
            SetWeakestService(ref label, ref value, "\u8b66\u52a1", Mathf.Max(tile.SafetyAccess, tile.SecurityAccess));
            SetWeakestService(ref label, ref value, "\u751f\u547d", tile.DeathcareAccess);
            return "\u9009" + label + "\u9760\u4f4f\u533a\u8def\u53e3";
        }

        private static string UtilityTileActionHint(CityMetrics metrics)
        {
            if (metrics != null && metrics.FloodRisk >= 45)
            {
                return "\u9009\u96e8\u6c34\u56ed\u653e\u4f4e\u503c\u533a";
            }

            if (metrics != null && metrics.WastewaterUtilization > 110)
            {
                return "\u9009\u6c61\u6c34\u8bbe\u65bd\u9760\u8def";
            }

            return "\u9009\u7535\u7ad9/\u6c34\u5854\u9760\u8def";
        }

        private static string OpenTileZoningHint(CityMetrics metrics)
        {
            if (metrics != null && metrics.Demand != null)
            {
                if (metrics.Demand.Residential >= Mathf.Max(metrics.Demand.Commercial, metrics.Demand.Industrial))
                {
                    return "\u62d6\u4f4f\u5b85\u533a 3-5 \u683c";
                }

                if (metrics.Demand.Commercial >= metrics.Demand.Industrial)
                {
                    return "\u62d6\u5546\u4e1a\u533a\u8d34\u4e3b\u8def";
                }

                return "\u62d6\u5de5\u4e1a\u533a\u8fdc\u4f4f\u5b85";
            }

            return "\u62d6\u4f4f\u5b85\u533a 3-5 \u683c";
        }

        private static bool TileHasUse(TileData tile)
        {
            return tile.Zone != ZoneType.None || !string.IsNullOrEmpty(tile.BuildingId);
        }

        private static int ServiceAccessValue(TileData tile)
        {
            return Mathf.Max(tile.ParkAccess, Mathf.Max(tile.HealthAccess, Mathf.Max(tile.DeathcareAccess, Mathf.Max(tile.EducationAccess, Mathf.Max(Mathf.Max(tile.SafetyAccess, tile.FireProtectionAccess), tile.SecurityAccess)))));
        }

        private void RefreshOverlayButtons()
        {
            var metrics = controller != null ? controller.Metrics : null;
            var recommendedMode = RecommendedOverlayMode(metrics);
            var recommendedScore = OverlayPressureScore(recommendedMode, metrics);
            for (var i = 0; i < overlayButtons.Count; i += 1)
            {
                var binding = overlayButtons[i];
                var swatchMode = binding != null ? binding.Mode : (i < overlaySwatchModes.Count ? overlaySwatchModes[i] : OverlayMode.Normal);
                var active = controller != null && controller.OverlayMode == swatchMode;
                var recommended = !active && swatchMode == recommendedMode && recommendedScore >= 56;
                var label = binding != null ? binding.Label : null;
                if (label != null)
                {
                    label.color = active ? Color.white : (recommended ? new Color32(255, 232, 150, 255) : new Color32(245, 255, 238, 235));
                    var image = binding != null && binding.Button != null ? binding.Button.GetComponent<Image>() : null;
                    if (image != null)
                    {
                        image.color = active
                            ? new Color32(45, 154, 174, 250)
                            : (recommended ? new Color32(255, 236, 156, 72) : new Color32(245, 255, 238, 34));
                    }

                    var buttonOutline = binding != null && binding.Button != null ? binding.Button.GetComponent<Outline>() : null;
                    if (buttonOutline != null)
                    {
                        // CITY_SKYLINES_RECOMMENDED_INFO_LAYER highlights the layer with the highest live pressure.
                        buttonOutline.effectColor = active
                            ? new Color32(245, 255, 238, 225)
                            : (recommended ? new Color32(255, 202, 70, 230) : new Color32(245, 255, 238, 70));
                        buttonOutline.effectDistance = active || recommended ? new Vector2(1.8f, -1.8f) : new Vector2(1f, -1f);
                    }

                    if (i < overlaySwatches.Count && overlaySwatches[i] != null)
                    {
                        // CITY_SKYLINES_LAYER_ICON_ACTIVE_STATE makes the right-side layer stack scan like icon buttons.
                        overlaySwatches[i].color = active ? new Color32(245, 255, 238, 255) : OverlayModeAccentColor(swatchMode);
                        overlaySwatches[i].rectTransform.sizeDelta = active
                            ? new Vector2(29f, 29f)
                            : (recommended ? new Vector2(28f, 28f) : new Vector2(27f, 27f));
                        var outline = overlaySwatches[i].GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.effectColor = active
                                ? new Color32(255, 202, 70, 185)
                                : (recommended ? new Color32(255, 202, 70, 190) : new Color32(245, 255, 238, 105));
                            outline.effectDistance = active || recommended ? new Vector2(1.6f, -1.6f) : new Vector2(1f, -1f);
                        }
                    }
                }

                if (i < overlayPressureFills.Count && overlayPressureFills[i] != null)
                {
                    var score = OverlayPressureScore(swatchMode, metrics);
                    var fillRect = overlayPressureFills[i].rectTransform;
                    fillRect.anchorMax = new Vector2(Mathf.Clamp01(score / 100f), 1f);
                    overlayPressureFills[i].color = OverlayPressureFillColor(swatchMode, score, active);
                }

                if (i < overlayStateRails.Count && overlayStateRails[i] != null)
                {
                    overlayStateRails[i].color = active
                        ? new Color32(45, 154, 174, 250)
                        : (recommended ? new Color32(255, 202, 70, 176) : new Color32(65, 169, 184, 0));
                }

                if (i < overlayRecommendationBadges.Count && overlayRecommendationBadges[i] != null)
                {
                    overlayRecommendationBadges[i].color = recommended
                        ? new Color32(255, 202, 70, 245)
                        : new Color32(255, 202, 70, 0);
                }

                if (i < overlayRecommendationBadgeGlyphs.Count && overlayRecommendationBadgeGlyphs[i] != null)
                {
                    overlayRecommendationBadgeGlyphs[i].color = recommended
                        ? new Color32(83, 68, 30, 255)
                        : new Color32(83, 68, 30, 0);
                }
            }
        }

        private static int OverlayPressureScore(OverlayMode mode, CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 18;
            }

            if (mode == OverlayMode.Traffic) return Mathf.Clamp(metrics.RoadBottleneckPressure, 8, 100);
            if (mode == OverlayMode.Pollution) return Mathf.Clamp(Mathf.Max(metrics.Pollution, metrics.NoiseStress), 8, 100);
            if (mode == OverlayMode.Zoning) return Mathf.Clamp(Mathf.Max(metrics.LandUseConflict, metrics.IdleZoneTiles / 2), 8, 100);
            if (mode == OverlayMode.Services) return Mathf.Clamp(metrics.ServiceGapPressure, 8, 100);
            if (mode == OverlayMode.Transit) return Mathf.Clamp(Mathf.Max(100 - metrics.TransitCoverage, metrics.TransitWaitPressure), 8, 100);
            if (mode == OverlayMode.LandValue) return Mathf.Clamp(Mathf.Max(100 - metrics.AverageLandValue, metrics.RentPressure), 8, 100);
            if (mode == OverlayMode.Waste) return Mathf.Clamp(Mathf.Max(100 - metrics.WasteCoverage, metrics.WasteUtilization - 20), 8, 100);
            if (mode == OverlayMode.Logistics) return Mathf.Clamp(Mathf.Max(100 - metrics.LogisticsCoverage, 100 - metrics.GoodsBalance), 8, 100);
            if (mode == OverlayMode.Utilities) return Mathf.Clamp(Mathf.Max(100 - metrics.UtilityReliability, metrics.UtilityUtilization - 20), 8, 100);
            if (mode == OverlayMode.Communications) return Mathf.Clamp(Mathf.Max(100 - metrics.CommunicationCoverage, 100 - metrics.MailCoverage), 8, 100);
            if (mode == OverlayMode.RoadSafety) return Mathf.Clamp(Mathf.Max(100 - metrics.RoadSafety, metrics.AccidentRisk), 8, 100);
            if (mode == OverlayMode.Parking) return Mathf.Clamp(Mathf.Max(metrics.ParkingPressure, 100 - metrics.ParkingCoverage), 8, 100);
            if (mode == OverlayMode.Stormwater) return Mathf.Clamp(Mathf.Max(metrics.FloodRisk, 100 - metrics.StormwaterResilience), 8, 100);
            return Mathf.Clamp(Mathf.Max(metrics.ForecastRisk, Mathf.Max(metrics.RoadBottleneckPressure, metrics.ServiceGapPressure)), 8, 100);
        }

        private static OverlayMode RecommendedOverlayMode(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return OverlayMode.Normal;
            }

            var bestMode = OverlayMode.Traffic;
            var bestScore = OverlayPressureScore(bestMode, metrics);
            var modes = new[]
            {
                OverlayMode.Pollution,
                OverlayMode.Zoning,
                OverlayMode.Services,
                OverlayMode.Transit,
                OverlayMode.LandValue,
                OverlayMode.Waste,
                OverlayMode.Logistics,
                OverlayMode.Utilities,
                OverlayMode.Communications,
                OverlayMode.RoadSafety,
                OverlayMode.Parking,
                OverlayMode.Stormwater
            };

            for (var i = 0; i < modes.Length; i += 1)
            {
                var score = OverlayPressureScore(modes[i], metrics);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMode = modes[i];
                }
            }

            return bestScore >= 42 ? bestMode : OverlayMode.Normal;
        }

        private static string OverlayRecommendationText(OverlayMode activeMode, CityMetrics metrics)
        {
            var recommended = RecommendedOverlayMode(metrics);
            if (recommended == OverlayMode.Normal || recommended == activeMode)
            {
                return string.Empty;
            }

            return "  \u5efa\u8bae\u770b:" + OverlayLabel(recommended);
        }

        private static Color32 OverlayPressureFillColor(OverlayMode mode, int score, bool active)
        {
            var accent = OverlayModeAccentColor(mode);
            if (active)
            {
                return score >= 70 ? new Color32(255, 186, 68, 255) : new Color32(245, 255, 238, 252);
            }

            if (score >= 70)
            {
                return new Color32(236, 92, 74, 248);
            }

            if (score >= 42)
            {
                return new Color32(255, 202, 70, 226);
            }

            return new Color32(accent.r, accent.g, accent.b, 142);
        }

        private void RefreshToolButtons()
        {
            var metrics = controller != null ? controller.Metrics : null;
            var strongestRecommendation = StrongestToolRecommendationScore(metrics);
            for (var i = 0; i < toolButtons.Count; i += 1)
            {
                var binding = toolButtons[i];
                var image = binding.Button != null ? binding.Button.GetComponent<Image>() : null;
                if (image == null)
                {
                    continue;
                }

                var active = IsToolActive(binding);
                var recommendationScore = ToolRecommendationScoreWithSelectedTile(binding, metrics);
                var recommended = !active && IsDemandRecommendedTool(recommendationScore, strongestRecommendation);
                image.color = active ? new Color32(43, 166, 184, 250) : DemandAwareToolColor(binding, metrics, strongestRecommendation);
                var accentColor = active
                    ? new Color32(255, 248, 198, 255)
                    : (recommended ? new Color32(255, 211, 93, 255) : ToolAccentColor(binding.ToolMode, binding.Zone, binding.BuildingId));
                if (binding.Accent != null)
                {
                    binding.Accent.color = accentColor;
                }

                if (binding.IconSwatch != null)
                {
                    binding.IconSwatch.color = active
                        ? new Color32(250, 255, 245, 255)
                        : ToolAccentColor(binding.ToolMode, binding.Zone, binding.BuildingId);
                }

                if (binding.IconGlyph != null)
                {
                    binding.IconGlyph.color = active
                        ? new Color32(28, 94, 82, 255)
                        : new Color32(245, 255, 238, 255);
                }

                if (binding.SelectionGlow != null)
                {
                    binding.SelectionGlow.color = active
                        ? new Color32(245, 255, 238, 132)
                        : recommended
                            ? new Color32(255, 207, 86, 82)
                            : new Color32(0, 0, 0, 0);
                }

                if (binding.StateBadge != null)
                {
                    // REFERENCE_IMAGE_DOCK_STATUS_CORNER adds the selected/recommended corner chip seen in city-builder toolbars.
                    binding.StateBadge.color = active
                        ? new Color32(245, 255, 238, 230)
                        : recommended
                            ? new Color32(255, 207, 86, 238)
                            : new Color32(0, 0, 0, 0);
                }

                if (binding.StateBadgeText != null)
                {
                    binding.StateBadgeText.text = active ? "\u2713" : (recommended ? "\u8350" : string.Empty);
                    binding.StateBadgeText.color = active
                        ? new Color32(28, 94, 82, 255)
                        : (recommended ? new Color32(83, 68, 30, 255) : new Color32(83, 68, 30, 0));
                }

                if (binding.MetaLabel != null)
                {
                    binding.MetaLabel.text = ToolButtonMetaStatusText(binding, metrics, active, recommended);
                    binding.MetaLabel.color = active ? Color.white : (recommended ? new Color32(108, 77, 34, 255) : new Color32(216, 244, 220, 225));
                }

                if (binding.Outline != null)
                {
                    // REFERENCE_IMAGE_TOOL_SELECTION_OUTLINE makes selected and recommended dock tiles scan quickly.
                    binding.Outline.enabled = active || recommended;
                    binding.Outline.effectColor = active
                        ? new Color32(245, 255, 238, 242)
                        : new Color32(255, 207, 86, 228);
                    binding.Outline.effectDistance = active ? new Vector2(2.3f, -2.3f) : new Vector2(1.7f, -1.7f);
                }

                var label = binding.Label;
                if (label != null)
                {
                    label.color = active ? Color.white : (recommended ? new Color32(77, 70, 38, 255) : new Color32(245, 255, 238, 242));
                }
            }
        }

        private void RefreshPolicyButtons()
        {
            for (var i = 0; i < policyButtons.Count; i += 1)
            {
                var binding = policyButtons[i];
                var image = binding.Button != null ? binding.Button.GetComponent<Image>() : null;
                if (image == null)
                {
                    continue;
                }

                var active = controller != null && controller.IsPolicyActive(binding.Policy);
                image.color = active
                    ? new Color32(95, 176, 107, 245)
                    : new Color32(245, 255, 238, 32);
                var label = binding.Label;
                if (label != null)
                {
                    label.color = active ? Color.white : new Color32(245, 255, 238, 236);
                }
            }
        }

        private void RefreshMiniMap()
        {
            if (miniMapCells.Count == 0 || controller == null || controller.Grid == null)
            {
                return;
            }

            var grid = controller.Grid;
            var selected = interaction != null && interaction.HasSelectedTile ? interaction.SelectedTile : new GridPos(-1, -1);
            var selectedSampleX = selected.X >= 0 ? SampleMiniMapAxisForTile(selected.X, MiniMapColumns, grid.Width) : -1;
            var selectedSampleY = selected.Y >= 0 ? SampleMiniMapAxisForTile(selected.Y, MiniMapRows, grid.Height) : -1;
            var severeSamples = 0;
            var warningSamples = 0;
            for (var row = 0; row < MiniMapRows; row += 1)
            {
                for (var column = 0; column < MiniMapColumns; column += 1)
                {
                    var index = row * MiniMapColumns + column;
                    if (index >= miniMapCells.Count || miniMapCells[index] == null)
                    {
                        continue;
                    }

                    var sampleY = MiniMapRows - row - 1;
                    var x = SampleMiniMapAxis(column, MiniMapColumns, grid.Width);
                    var y = SampleMiniMapAxis(sampleY, MiniMapRows, grid.Height);
                    var tile = controller.GetTile(x, y);
                    var severity = MiniMapIssueSeverity(tile, controller.Metrics);
                    if (severity >= 34) severeSamples += 1;
                    else if (severity >= 18) warningSamples += 1;
                    var selectedCell = column == selectedSampleX && sampleY == selectedSampleY;
                    var miniMapColor = MiniMapTileColor(tile, controller.Metrics);
                    var lockedCell = controller.Grid.IsLockedExpansionTile(new GridPos(x, y));
                    if (lockedCell)
                    {
                        miniMapColor = BlendToolRecommendationColor(miniMapColor, new Color32(255, 232, 126, 255), 0.42f);
                    }

                    // MINIMAP_SELECTED_CELL_BLEND_TINT keeps map heat readable under the active cursor.
                    miniMapCells[index].color = MiniMapFacetColor(miniMapColor, row, column, selectedCell);
                    if (index < miniMapCellFacets.Count && miniMapCellFacets[index] != null)
                    {
                        miniMapCellFacets[index].color = MiniMapCellFacetOverlayColor(tile, row, column, selectedCell, lockedCell);
                    }

                    if (index < miniMapCellOutlines.Count && miniMapCellOutlines[index] != null)
                    {
                        // MINIMAP_SELECTED_CELL_OUTLINE makes the active tile visible on the compact overview.
                        miniMapCellOutlines[index].enabled = true;
                        miniMapCellOutlines[index].effectColor = selectedCell
                            ? MiniMapSelectedIssueOutlineColor(tile, controller.Metrics)
                            : (lockedCell ? new Color32(255, 240, 156, 116) : MiniMapCellGridLineColor(tile, controller.Metrics));
                        miniMapCellOutlines[index].effectDistance = selectedCell ? new Vector2(1.6f, -1.6f) : new Vector2(0.75f, -0.75f);
                    }
                }
            }

            if (miniMapRiskSummaryText != null)
            {
                lastMiniMapSevereSamples = severeSamples;
                lastMiniMapWarningSamples = warningSamples;
                var mode = controller != null ? controller.OverlayMode : OverlayMode.Normal;
                var metrics = controller != null ? controller.Metrics : null;
                var focusMode = MiniMapFocusOverlayMode(mode, metrics);
                var pressure = OverlayPressureScore(focusMode, metrics);
                // CITY_SKYLINES_MINIMAP_LAYER_SUMMARY keeps the bottom-right overview tied to the active layer.
                var secondaryLine = BuildMiniMapCommandLine(mode, focusMode, metrics, severeSamples, warningSamples) + MiniMapSelectionHint();
                miniMapRiskSummaryText.text = BuildMiniMapSummaryLine(mode, focusMode, metrics, severeSamples, warningSamples, pressure)
                    + (string.IsNullOrEmpty(secondaryLine) ? string.Empty : "\n" + secondaryLine);
                miniMapRiskSummaryText.color = pressure >= 70 || severeSamples > 0
                    ? new Color32(255, 188, 132, 255)
                    : pressure >= 48 || warningSamples > 0
                        ? new Color32(255, 230, 132, 255)
                        : new Color32(245, 255, 238, 245);
            }

            RefreshMiniMapLayerAccent();
            RefreshMiniMapViewportFrame(selectedSampleX, selectedSampleY);
        }

        private static string BuildMiniMapSummaryLine(OverlayMode activeMode, OverlayMode focusMode, CityMetrics metrics, int severeSamples, int warningSamples, int pressure)
        {
            // REFERENCE_IMAGE_MINIMAP_COMMAND_SUMMARY makes the overview read like a compact city-operations card.
            var prefix = focusMode != activeMode && focusMode != OverlayMode.Normal ? "\u63a8" : "\u5c42";
            return prefix + OverlayLabel(focusMode)
                + " \u538b" + pressure
                + " \u70ed" + severeSamples + "/" + warningSamples
                + " \u4e3b" + MiniMapPrimaryIssueLabel(metrics);
        }

        private string BuildMiniMapCommandLine(OverlayMode activeMode, OverlayMode focusMode, CityMetrics metrics, int severeSamples, int warningSamples)
        {
            var chain = CompactCardText(BuildLayerToolActionChain(metrics, activeMode, focusMode), 20);
            var tag = MiniMapUrgencyTag(metrics, severeSamples, warningSamples);
            var order = MiniMapOrderCue(metrics);
            return tag + (string.IsNullOrEmpty(order) ? string.Empty : " " + order) + " " + chain + MiniMapRewardHint(metrics);
        }

        private static OverlayMode MiniMapFocusOverlayMode(OverlayMode activeMode, CityMetrics metrics)
        {
            var recommended = RecommendedOverlayMode(metrics);
            if (recommended == OverlayMode.Normal || recommended == activeMode)
            {
                return activeMode;
            }

            var recommendedPressure = OverlayPressureScore(recommended, metrics);
            var activePressure = OverlayPressureScore(activeMode, metrics);
            return recommendedPressure >= Mathf.Max(42, activePressure + 8) || activeMode == OverlayMode.Normal
                ? recommended
                : activeMode;
        }

        private static string MiniMapUrgencyTag(CityMetrics metrics, int severeSamples, int warningSamples)
        {
            var pressure = SimulationActionPressure(metrics);
            if (pressure >= 72 || severeSamples > 0)
            {
                return "\u4f18\u5148";
            }

            if (pressure >= 55 || warningSamples > 0)
            {
                return "\u5efa\u8bae";
            }

            if (metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u53ef\u5347";
            }

            return "\u7a33\u5b9a";
        }

        private static string MiniMapOrderCue(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            var objective = metrics.ActiveObjective;
            if (objective != null && objective.Required > 0)
            {
                var required = Mathf.Max(1, objective.Required);
                var progress = Mathf.Clamp(objective.Progress, 0, required);
                if (objective.Done)
                {
                    return "\u53ef\u9886";
                }

                if (!metrics.LockedExpansionUnlocked)
                {
                    return "\u65b0\u533a" + progress + "/" + required;
                }

                return "\u8ba2\u5355" + progress + "/" + required;
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return "\u5347\u7ea7+" + metrics.BuildingUpgradeReadyCount;
            }

            return string.Empty;
        }

        private static string MiniMapRewardHint(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return string.Empty;
            }

            if (metrics.ActiveObjective != null && metrics.ActiveObjective.Done)
            {
                return " >\u9886\u91d1";
            }

            if (metrics.BuildingUpgradeReadyCount > 0)
            {
                return " >\u5347\u7ea7";
            }

            return metrics.ForecastRisk >= 70 ? " >\u964d\u9669" : string.Empty;
        }

        private void RefreshMiniMapCameraStatus()
        {
            if (miniMapCameraStatusText == null && miniMapCameraZoomFill == null)
            {
                return;
            }

            if (cameraController == null)
            {
                cameraController = Camera.main != null ? Camera.main.GetComponent<CityCameraController>() : null;
            }

            var zoom = cameraController != null ? Mathf.Clamp01(cameraController.NormalizedZoom) : 0f;
            var percent = Mathf.RoundToInt(zoom * 100f);
            var settling = cameraController != null && cameraController.IsCameraSettling;
            var feedback = cameraController != null ? CameraFeedbackLabel(cameraController.LastCameraFeedback) : "--";
            if (miniMapCameraStatusText != null)
            {
                miniMapCameraStatusText.text = "\u955c\u5934 " + percent + "% " + (settling ? "\u6ed1\u52a8" : feedback);
                miniMapCameraStatusText.color = settling
                    ? new Color32(255, 230, 132, 255)
                    : new Color32(245, 255, 238, 242);
            }

            if (miniMapCameraZoomFill != null)
            {
                miniMapCameraZoomFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp(zoom, 0.08f, 1f), 1f);
                miniMapCameraZoomFill.color = settling
                    ? new Color32(255, 207, 86, 176)
                    : new Color32(65, 183, 190, 168);
            }
        }

        private static string CameraFeedbackLabel(string feedback)
        {
            if (string.IsNullOrEmpty(feedback)) return "\u7a33\u5b9a";
            if (feedback.IndexOf("Drag", System.StringComparison.OrdinalIgnoreCase) >= 0) return "\u62d6\u62fd";
            if (feedback.IndexOf("Zoom", System.StringComparison.OrdinalIgnoreCase) >= 0) return "\u7f29\u653e";
            if (feedback.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0) return "\u6784\u56fe";
            if (feedback.IndexOf("Pan", System.StringComparison.OrdinalIgnoreCase) >= 0) return "\u5e73\u79fb";
            return "\u8c03\u6574";
        }

        private void RefreshMiniMapLayerAccent()
        {
            if (miniMapViewportFrameImage == null && miniMapViewportFrameOutline == null)
            {
                return;
            }

            var metrics = controller != null ? controller.Metrics : null;
            var mode = controller != null ? controller.OverlayMode : OverlayMode.Normal;
            var accentMode = mode == OverlayMode.Normal ? RecommendedOverlayMode(metrics) : mode;
            var accent = accentMode == OverlayMode.Normal
                ? new Color32(214, 247, 255, 255)
                : OverlayModeAccentColor(accentMode);
            var pressure = OverlayPressureScore(accentMode, metrics);
            var strong = accentMode != OverlayMode.Normal && pressure >= 42;

            if (miniMapViewportFrameImage != null)
            {
                miniMapViewportFrameImage.color = strong
                    ? new Color32(accent.r, accent.g, accent.b, 58)
                    : new Color32(214, 247, 255, 36);
            }

            if (miniMapViewportFrameOutline != null)
            {
                miniMapViewportFrameOutline.effectColor = strong
                    ? new Color32(accent.r, accent.g, accent.b, 255)
                    : new Color32(232, 255, 255, 245);
                miniMapViewportFrameOutline.effectDistance = strong ? new Vector2(2.6f, -2.6f) : new Vector2(2.1f, -2.1f);
            }
        }

        private string MiniMapSelectionHint()
        {
            if (interaction == null || controller == null || !interaction.HasSelectedTile)
            {
                return string.Empty;
            }

            var pos = interaction.SelectedTile;
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null)
            {
                return string.Empty;
            }

            var diagnosis = ShortMiniMapDiagnosis(BuildTileActionDiagnosis(controller.OverlayMode, tile));
            return string.IsNullOrEmpty(diagnosis)
                ? string.Empty
                : " \u9009" + pos.X + "," + pos.Y + " " + diagnosis;
        }

        private string MiniMapOverlayRecommendationHint(OverlayMode mode)
        {
            var metrics = controller != null ? controller.Metrics : null;
            var recommended = RecommendedOverlayMode(metrics);
            if (recommended == OverlayMode.Normal || recommended == mode)
            {
                return string.Empty;
            }

            return "\u5efa\u8bae\u770b:" + OverlayLabel(recommended);
        }

        private static string MiniMapPrimaryIssueLabel(CityMetrics metrics)
        {
            // MINIMAP_PRIMARY_ISSUE_LABEL gives the compact overview a one-glyph reason for current hotspots.
            if (metrics == null)
            {
                return "--";
            }

            var road = RoadRadarPressure(metrics);
            var service = ServiceRadarPressure(metrics);
            var fiscal = FiscalRadarPressure(metrics);
            var utility = Mathf.Max(100 - metrics.UtilityReliability, Mathf.Max(metrics.UtilityUtilization - 10, metrics.FloodRisk));
            var housing = Mathf.Max(metrics.RentPressure, metrics.HousingCapacity <= metrics.Population + 12 ? 72 : 0);
            var max = Mathf.Max(metrics.ForecastRisk, Mathf.Max(road, Mathf.Max(service, Mathf.Max(fiscal, Mathf.Max(utility, housing)))));
            if (max < 35)
            {
                return "\u7a33\u5b9a";
            }

            if (max == road) return "\u4ea4\u5835";
            if (max == service) return "\u670d\u7f3a";
            if (max == fiscal) return "\u8d22\u538b";
            if (max == utility) return "\u6c34\u7535";
            if (max == housing) return "\u4f4f\u538b";
            return "\u98ce\u9669";
        }

        private static string ShortMiniMapDiagnosis(string diagnosis)
        {
            if (string.IsNullOrEmpty(diagnosis))
            {
                return string.Empty;
            }

            var arrow = diagnosis.IndexOf("->", System.StringComparison.Ordinal);
            if (arrow >= 0 && arrow + 2 < diagnosis.Length)
            {
                return CompactCardText(diagnosis.Substring(arrow + 2).Trim(), 9);
            }

            var prefix = "\u8bca\u65ad:";
            if (diagnosis.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                diagnosis = diagnosis.Substring(prefix.Length);
            }

            return CompactCardText(diagnosis, 9);
        }

        private void RefreshMiniMapViewportFrame(int selectedSampleX, int selectedSampleY)
        {
            if (miniMapViewportFrame == null || miniMapViewportFrame.parent == null)
            {
                return;
            }

            var previewRect = miniMapViewportFrame.parent as RectTransform;
            if (previewRect == null)
            {
                return;
            }

            var width = Mathf.Max(1f, previewRect.rect.width);
            var height = Mathf.Max(1f, previewRect.rect.height);
            var contentWidth = Mathf.Max(1f, width - 8f);
            var contentHeight = Mathf.Max(1f, height - 8f);
            var stepX = contentWidth / MiniMapColumns;
            var stepY = contentHeight / MiniMapRows;
            var frameWidth = Mathf.Clamp(stepX * 4.2f, 34f, contentWidth);
            var frameHeight = Mathf.Clamp(stepY * 2.4f, 16f, contentHeight);
            var column = selectedSampleX >= 0 ? selectedSampleX : MiniMapColumns / 2;
            var sampleY = selectedSampleY >= 0 ? selectedSampleY : MiniMapRows / 2;
            var row = MiniMapRows - sampleY - 1;
            var centerX = 4f + (column + 0.5f) * stepX;
            var centerY = -4f - (row + 0.5f) * stepY;
            centerX = Mathf.Clamp(centerX, 4f + frameWidth * 0.5f, width - 4f - frameWidth * 0.5f);
            centerY = Mathf.Clamp(centerY, -height + 4f + frameHeight * 0.5f, -4f - frameHeight * 0.5f);
            miniMapViewportFrame.sizeDelta = new Vector2(frameWidth, frameHeight);
            miniMapViewportFrame.anchoredPosition = new Vector2(centerX, centerY);
        }

        private static Color32 MiniMapSelectedIssueOutlineColor(TileData tile, CityMetrics metrics)
        {
            // MINIMAP_SELECTED_ISSUE_SEVERITY_OUTLINE makes the selected tile's risk readable at a glance.
            var severity = MiniMapIssueSeverity(tile, metrics);
            if (severity >= 34) return new Color32(224, 92, 70, 250);
            if (severity >= 18) return new Color32(244, 178, 76, 250);
            return new Color32(54, 153, 142, 245);
        }

        private static Color32 MiniMapCellGridLineColor(TileData tile, CityMetrics metrics)
        {
            // CITY_SKYLINES_MINIMAP_DIAGNOSTIC_GRID keeps every minimap sample legible without hiding heat colors.
            var severity = MiniMapIssueSeverity(tile, metrics);
            if (severity >= 34) return new Color32(255, 232, 218, 92);
            if (severity >= 18) return new Color32(255, 245, 206, 78);
            if (tile != null && !string.IsNullOrEmpty(tile.RoadId)) return new Color32(235, 248, 250, 66);
            return new Color32(245, 255, 238, 52);
        }

        private static int SampleMiniMapAxis(int sample, int sampleCount, int tileCount)
        {
            if (tileCount <= 1)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.RoundToInt((sample + 0.5f) * tileCount / sampleCount - 0.5f), 0, tileCount - 1);
        }

        private static int SampleMiniMapAxisForTile(int tile, int sampleCount, int tileCount)
        {
            if (tileCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.FloorToInt(tile * sampleCount / (float)tileCount), 0, sampleCount - 1);
        }

        private static Color32 MiniMapTileColor(TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return new Color32(80, 112, 104, 220);
            }

            var issue = MiniMapIssueSeverity(tile, metrics);
            if (issue >= 34)
            {
                return new Color32(234, 108, 82, 255);
            }

            if (issue >= 18)
            {
                return new Color32(250, 198, 90, 255);
            }

            if (tile.Terrain == TerrainType.Water)
            {
                return new Color32(86, 203, 226, 255);
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                return new Color32(62, 86, 82, 255);
            }

            if (!string.IsNullOrEmpty(tile.BuildingId))
            {
                return new Color32(252, 246, 218, 255);
            }

            return ZoneMiniMapColor(tile.Zone, tile.Terrain);
        }

        private static Color32 MiniMapFacetColor(Color32 baseColor, int row, int column, bool selectedCell)
        {
            if (selectedCell)
            {
                return BlendToolRecommendationColor(baseColor, new Color32(65, 169, 184, 245), 0.64f);
            }

            var facet = (row + column) % 3;
            if (facet == 0)
            {
                return BlendToolRecommendationColor(baseColor, new Color32(245, 255, 238, 255), 0.12f);
            }

            if (facet == 1)
            {
                return BlendToolRecommendationColor(baseColor, new Color32(44, 104, 78, 255), 0.08f);
            }

            return baseColor;
        }

        private static Color32 MiniMapCellFacetOverlayColor(TileData tile, int row, int column, bool selectedCell, bool lockedCell)
        {
            // REFERENCE_IMAGE_MINIMAP_ISOMETRIC_FACETS makes the overview read as a tiny isometric model.
            if (selectedCell)
            {
                return new Color32(245, 255, 238, 116);
            }

            if (lockedCell)
            {
                return new Color32(255, 248, 182, 92);
            }

            if (tile != null && tile.Terrain == TerrainType.Water)
            {
                return new Color32(235, 255, 255, 78);
            }

            if (tile != null && !string.IsNullOrEmpty(tile.RoadId))
            {
                return new Color32(245, 255, 238, 36);
            }

            if (tile != null && !string.IsNullOrEmpty(tile.BuildingId))
            {
                return new Color32(255, 255, 232, 94);
            }

            var alpha = (row + column) % 2 == 0 ? (byte)42 : (byte)26;
            return new Color32(245, 255, 238, alpha);
        }

        private static int MiniMapIssueSeverity(TileData tile, CityMetrics metrics)
        {
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return 0;
            }

            var severity = Mathf.Max(0, tile.Traffic - 58);
            severity = Mathf.Max(severity, Mathf.Max(tile.Pollution, tile.Noise) - 42);
            severity = Mathf.Max(severity, 28 - ServiceAccessValue(tile));
            severity = Mathf.Max(severity, 24 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess));
            severity = Mathf.Max(severity, 24 - tile.ParkingAccess);
            severity = Mathf.Max(severity, 24 - tile.StormwaterAccess);
            if (metrics != null)
            {
                severity = Mathf.Max(severity, 92 - metrics.UtilityReliability);
                severity = Mathf.Max(severity, metrics.FloodRisk - 52);
            }

            return severity;
        }

        private static Color32 ZoneMiniMapColor(ZoneType zone, TerrainType terrain)
        {
            if (terrain == TerrainType.Hill)
            {
                return new Color32(126, 177, 124, 255);
            }

            if (zone == ZoneType.Residential) return new Color32(151, 224, 136, 255);
            if (zone == ZoneType.Commercial) return new Color32(244, 214, 92, 255);
            if (zone == ZoneType.Industrial) return new Color32(197, 155, 218, 255);
            if (zone == ZoneType.Civic) return new Color32(116, 190, 226, 255);
            if (zone == ZoneType.Utility) return new Color32(91, 196, 193, 255);
            if (zone == ZoneType.Office) return new Color32(132, 190, 230, 255);
            if (zone == ZoneType.MixedUse) return new Color32(238, 174, 116, 255);
            return new Color32(146, 218, 130, 255);
        }

        private bool IsToolActive(ToolButtonBinding binding)
        {
            if (interaction == null || interaction.ToolMode != binding.ToolMode)
            {
                return false;
            }

            if (binding.ToolMode == CityToolMode.ZonePaint)
            {
                return interaction.SelectedZone == binding.Zone;
            }

            if (binding.ToolMode == CityToolMode.BuildBuilding)
            {
                return interaction.SelectedBuildingId == binding.BuildingId;
            }

            return true;
        }

        private static string ToolButtonLabelText(string labelText, CityToolMode mode, ZoneType zone, string buildingId)
        {
            // REFERENCE_IMAGE_TOOL_CARD_LABELS keeps the dense dock closer to card-like build entries.
            if (mode == CityToolMode.BuildRoad) return "\u9053\u8def";
            if (mode == CityToolMode.UpgradeRoad) return "\u5347\u7ea7";
            if (mode == CityToolMode.Demolish) return "\u62c6\u9664";
            if (mode == CityToolMode.ZonePaint) return ZoneShortLabel(zone);
            if (mode == CityToolMode.BuildBuilding) return CompactCardText(labelText, 4);
            return CompactCardText(labelText, 4);
        }

        private string ToolButtonMetaText(CityToolMode mode, ZoneType zone, string buildingId)
        {
            // REFERENCE_IMAGE_TOOL_CARD_PRICE_TAGS echoes the coin price row in the reference bottom toolbar.
            if (mode == CityToolMode.BuildRoad) return "$40";
            if (mode == CityToolMode.UpgradeRoad) return "\u5347";
            if (mode == CityToolMode.Demolish) return "\u8fd4";
            if (mode == CityToolMode.ZonePaint) return "$6";
            if (mode == CityToolMode.BuildBuilding && controller != null)
            {
                var definition = controller.GetBuildingDefinition(buildingId);
                if (definition != null)
                {
                    return "$" + CompactCost(definition.Cost);
                }
            }

            return string.Empty;
        }

        private string ToolButtonMetaStatusText(ToolButtonBinding binding, CityMetrics metrics, bool active, bool recommended)
        {
            // CITY_SKYLINES_RECOMMENDED_TOOL_META explains dock highlights without adding more controls.
            if (active)
            {
                return "\u5df2\u9009";
            }

            if (recommended)
            {
                // CITY_SKYLINES_TOOL_RECOMMENDATION_CALL_TO_ACTION keeps suggested dock cards actionable at tiny size.
                var reason = ToolRecommendationDriverLabel(binding, metrics);
                return string.IsNullOrEmpty(reason) ? "\u5efa\u8bae" : CompactCardText(reason, 4);
            }

            return ToolButtonMetaText(binding.ToolMode, binding.Zone, binding.BuildingId);
        }

        private static string CompactCost(int value)
        {
            if (value >= 1000)
            {
                return Mathf.RoundToInt(value / 100f) / 10f + "k";
            }

            return value.ToString();
        }

        private static Color32 ToolAccentColor(CityToolMode mode, ZoneType zone, string buildingId)
        {
            // REFERENCE_IMAGE_TOOL_CARD_SWATCH_COLORS gives each dock card the visual category of its building.
            if (mode == CityToolMode.BuildRoad || mode == CityToolMode.UpgradeRoad) return new Color32(92, 104, 106, 255);
            if (mode == CityToolMode.Demolish) return new Color32(224, 106, 82, 255);
            if (mode == CityToolMode.ZonePaint) return ZoneSwatchColor(zone);
            if (IsTransitOrLogisticsTool(buildingId)) return new Color32(86, 139, 210, 255);
            if (IsUtilityTool(buildingId)) return new Color32(84, 155, 158, 255);
            if (IsServiceTool(buildingId)) return new Color32(244, 139, 124, 255);
            if (IsIndustrialTool(buildingId)) return new Color32(222, 158, 86, 255);
            if (IsResidentialTool(buildingId)) return new Color32(255, 204, 109, 255);
            if (IsCommercialTool(buildingId)) return new Color32(88, 166, 226, 255);
            return new Color32(96, 190, 122, 255);
        }

        private static Color32 ZoneSwatchColor(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return new Color32(96, 190, 122, 255);
            if (zone == ZoneType.Commercial) return new Color32(88, 166, 226, 255);
            if (zone == ZoneType.MixedUse) return new Color32(82, 188, 158, 255);
            if (zone == ZoneType.Office) return new Color32(112, 192, 214, 255);
            if (zone == ZoneType.Industrial) return new Color32(222, 158, 86, 255);
            if (zone == ZoneType.Civic) return new Color32(244, 139, 124, 255);
            if (zone == ZoneType.Utility) return new Color32(82, 174, 186, 255);
            return new Color32(96, 190, 122, 255);
        }

        private static string ZoneShortLabel(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return "\u4f4f\u5b85";
            if (zone == ZoneType.Commercial) return "\u5546\u4e1a";
            if (zone == ZoneType.MixedUse) return "\u6df7\u5408";
            if (zone == ZoneType.Office) return "\u529e\u516c";
            if (zone == ZoneType.Industrial) return "\u5de5\u4e1a";
            if (zone == ZoneType.Civic) return "\u670d\u52a1";
            if (zone == ZoneType.Utility) return "\u8bbe\u65bd";
            return "\u5206\u533a";
        }

        private static Color32 ToolIdleColor(ToolButtonBinding binding)
        {
            // CITY_SKYLINES_STYLE_DIAGNOSTICS gives dense tool buttons category color without changing counts.
            if (binding == null)
            {
                return new Color32(245, 255, 238, 34);
            }

            if (binding.ToolMode == CityToolMode.BuildRoad || binding.ToolMode == CityToolMode.UpgradeRoad)
            {
                return new Color32(45, 65, 62, 222);
            }

            if (binding.ToolMode == CityToolMode.Demolish)
            {
                return new Color32(72, 48, 42, 222);
            }

            if (binding.ToolMode == CityToolMode.ZonePaint)
            {
                if (binding.Zone == ZoneType.Residential) return new Color32(32, 76, 45, 222);
                if (binding.Zone == ZoneType.Commercial) return new Color32(31, 64, 78, 222);
                if (binding.Zone == ZoneType.MixedUse) return new Color32(30, 76, 62, 222);
                if (binding.Zone == ZoneType.Office) return new Color32(32, 70, 80, 222);
                if (binding.Zone == ZoneType.Industrial) return new Color32(79, 59, 39, 222);
                if (binding.Zone == ZoneType.Civic) return new Color32(80, 52, 49, 222);
                if (binding.Zone == ZoneType.Utility) return new Color32(31, 72, 76, 222);
            }

            if (IsTransitOrLogisticsTool(binding.BuildingId))
            {
                return new Color32(32, 56, 83, 222);
            }

            if (IsUtilityTool(binding.BuildingId))
            {
                return new Color32(31, 72, 76, 222);
            }

            if (IsServiceTool(binding.BuildingId))
            {
                return new Color32(80, 52, 49, 222);
            }

            if (IsIndustrialTool(binding.BuildingId))
            {
                return new Color32(79, 59, 39, 222);
            }

            if (IsResidentialTool(binding.BuildingId))
            {
                return new Color32(32, 76, 45, 222);
            }

            if (IsCommercialTool(binding.BuildingId))
            {
                return new Color32(31, 64, 78, 222);
            }

            return new Color32(31, 70, 52, 222);
        }

        private int StrongestToolRecommendationScore(CityMetrics metrics)
        {
            var strongest = 0;
            for (var i = 0; i < toolButtons.Count; i += 1)
            {
                strongest = Mathf.Max(strongest, ToolRecommendationScoreWithSelectedTile(toolButtons[i], metrics));
            }

            return strongest;
        }

        private Color32 DemandAwareToolColor(ToolButtonBinding binding, CityMetrics metrics, int strongestRecommendation)
        {
            // CITY_DEMAND_TOOL_RECOMMENDATIONS turns pressure metrics into subtle build-tool guidance.
            var baseColor = ToolIdleColor(binding);
            var score = ToolRecommendationScoreWithSelectedTile(binding, metrics);
            if (!IsDemandRecommendedTool(score, strongestRecommendation))
            {
                return baseColor;
            }

            var amount = score >= 88 ? 0.62f : 0.42f;
            return BlendToolRecommendationColor(baseColor, new Color32(255, 225, 138, 248), amount);
        }

        private static bool IsDemandRecommendedTool(int score, int strongestRecommendation)
        {
            return strongestRecommendation >= 72 && score >= Mathf.Max(72, strongestRecommendation - 6);
        }

        private static string ToolBindingLabel(ToolButtonBinding binding)
        {
            if (binding == null)
            {
                return "--";
            }

            if (binding.ToolMode == CityToolMode.BuildRoad) return "\u94fa\u8def";
            if (binding.ToolMode == CityToolMode.UpgradeRoad) return "\u5347\u8def";
            if (binding.ToolMode == CityToolMode.Demolish) return "\u62c6\u9664";
            if (binding.ToolMode == CityToolMode.ZonePaint) return ZoneLabel(binding.Zone);
            if (binding.ToolMode == CityToolMode.BuildBuilding) return BuildingLabel(binding.BuildingId);
            return "\u67e5\u770b";
        }

        private static string ToolRecommendationDriverLabel(ToolButtonBinding binding, CityMetrics metrics)
        {
            if (binding == null || metrics == null)
            {
                return "\u9700\u6c42";
            }

            if (binding.ToolMode == CityToolMode.BuildRoad || binding.ToolMode == CityToolMode.UpgradeRoad)
            {
                if (InfrastructureRoadToolScore(metrics) >= 55)
                {
                    return InfrastructureToolDriverLabel("road_maintenance_depot", metrics, "\u517b\u62a4" + metrics.RoadMaintenanceCoverage);
                }

                return !string.IsNullOrEmpty(metrics.ForecastAction) && metrics.RoadBottleneckPressure >= 55
                    ? CompactCardText(metrics.ForecastAction, 8)
                    : (metrics.RoadBottleneckPressure >= 100 - metrics.RoadConnectivity ? "\u4ea4\u901a" + metrics.Congestion : "\u8fde\u901a" + metrics.RoadConnectivity);
            }

            if (binding.ToolMode == CityToolMode.ZonePaint)
            {
                return SpecificDemandDriverLabel(binding.Zone, metrics);
            }

            var id = binding.BuildingId;
            if (id == "parking_garage") return "\u505c\u8f66" + metrics.ParkingPressure;
            if (id == "rain_garden") return InfrastructureToolDriverLabel(id, metrics, "\u96e8\u6d2a" + metrics.FloodRisk);
            if (id == "road_maintenance_depot") return InfrastructureToolDriverLabel(id, metrics, "\u517b\u62a4" + metrics.RoadMaintenanceCoverage);
            if (id == "emergency_shelter") return InfrastructureToolDriverLabel(id, metrics, SpecificServiceDriverLabel(metrics));
            if (IsTransitOrLogisticsTool(id)) return metrics.RoadBottleneckPressure >= 100 - metrics.GoodsBalance ? "\u4ea4\u901a" + metrics.Congestion : "\u8d27\u8fd0" + metrics.LogisticsCoverage;
            if (IsUtilityTool(id)) return InfrastructureToolDriverLabel(id, metrics, SpecificUtilityDriverLabel(metrics));
            if (IsServiceTool(id)) return InfrastructureToolDriverLabel(id, metrics, SpecificServiceDriverLabel(metrics));
            if (IsIndustrialTool(id)) return metrics.GoodsBalance < 55 ? "\u4f9b\u7ed9" + metrics.GoodsBalance : SpecificDemandDriverLabel(ZoneType.Industrial, metrics);
            return SpecificDemandDriverLabel(ZoneType.MixedUse, metrics);
        }

        private static string SpecificDemandDriverLabel(ZoneType zone, CityMetrics metrics)
        {
            // CITY_SKYLINES_SPECIFIC_NEXT_STEP_LABELS tie highlighted tools to the actual city pressure.
            if (!string.IsNullOrEmpty(metrics.DemandFocus) && metrics.DemandUrgency >= 55)
            {
                return CompactCardText(metrics.DemandFocus, 6) + metrics.DemandUrgency;
            }

            if (metrics.Demand == null)
            {
                return "\u9700\u6c42";
            }

            if (zone == ZoneType.Residential) return "\u4f4f\u9700" + metrics.Demand.Residential;
            if (zone == ZoneType.Commercial) return "\u5546\u9700" + metrics.Demand.Commercial;
            if (zone == ZoneType.Industrial) return "\u5de5\u9700" + metrics.Demand.Industrial;
            if (zone == ZoneType.Office) return "\u529e\u9700" + metrics.Demand.Office;
            if (zone == ZoneType.MixedUse) return "\u6df7\u9700" + metrics.Demand.MixedUse;
            if (zone == ZoneType.Civic) return "\u670d\u7f3a" + metrics.ServiceGapPressure;
            if (zone == ZoneType.Utility) return "\u6c34\u7535" + (100 - metrics.UtilityReliability);
            return "\u9700\u6c42";
        }

        private static string SpecificServiceDriverLabel(CityMetrics metrics)
        {
            if (!string.IsNullOrEmpty(metrics.ServiceGapFocus) && metrics.ServiceGapPressure >= 34)
            {
                return CompactCardText(metrics.ServiceGapFocus, 6) + metrics.ServiceGapPressure;
            }

            if (!string.IsNullOrEmpty(metrics.BudgetAction) && metrics.BudgetStress >= 62)
            {
                return CompactCardText(metrics.BudgetAction, 7);
            }

            return "\u670d\u7f3a" + metrics.ServiceGapPressure;
        }

        private static string SpecificUtilityDriverLabel(CityMetrics metrics)
        {
            if (!string.IsNullOrEmpty(metrics.BudgetFocus) && metrics.BudgetStress >= 62)
            {
                return CompactCardText(metrics.BudgetFocus, 6) + metrics.BudgetStress;
            }

            if (metrics.FloodRisk >= 45)
            {
                return "\u6d2a\u98ce" + metrics.FloodRisk;
            }

            return "\u53ef\u9760" + metrics.UtilityReliability;
        }

        private static int ToolRecommendationScore(ToolButtonBinding binding, CityMetrics metrics)
        {
            if (binding == null || metrics == null)
            {
                return 0;
            }

            if (binding.ToolMode == CityToolMode.ZonePaint)
            {
                return DemandForZone(metrics, binding.Zone);
            }

            if (binding.ToolMode == CityToolMode.BuildRoad || binding.ToolMode == CityToolMode.UpgradeRoad)
            {
                return Mathf.Max(Mathf.Max(100 - metrics.RoadConnectivity, metrics.RoadBottleneckPressure), InfrastructureRoadToolScore(metrics));
            }

            var id = binding.BuildingId;
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }

            if (id == "parking_garage") return metrics.ParkingPressure;
            if (id == "rain_garden") return Mathf.Max(Mathf.Max(metrics.FloodRisk, 100 - metrics.StormwaterResilience), InfrastructureToolRecommendationScore(id, metrics));
            if (id == "road_maintenance_depot") return Mathf.Max(Mathf.Max(metrics.RoadBottleneckPressure, 100 - metrics.MaintenanceCondition), InfrastructureToolRecommendationScore(id, metrics));
            if (IsResidentialTool(id)) return metrics.Demand.Residential;
            if (id == "mixed_use_block") return Mathf.Max(metrics.Demand.MixedUse, Mathf.Max(metrics.Demand.Residential, metrics.Demand.Commercial));
            if (id == "office_studio" || id == "research_campus") return metrics.Demand.Office;
            if (IsCommercialTool(id)) return metrics.Demand.Commercial;
            if (IsIndustrialTool(id)) return Mathf.Max(metrics.Demand.Industrial, 100 - metrics.GoodsBalance);
            if (IsTransitOrLogisticsTool(id)) return Mathf.Max(metrics.RoadBottleneckPressure, 100 - metrics.GoodsBalance);
            if (IsUtilityTool(id)) return Mathf.Max(Mathf.Max(metrics.Demand.Utility, Mathf.Max(metrics.UtilityUtilization - 30, 100 - metrics.UtilityReliability)), InfrastructureToolRecommendationScore(id, metrics));
            if (IsServiceTool(id)) return Mathf.Max(Mathf.Max(metrics.Demand.Service, Mathf.Max(metrics.ServiceGapPressure, 100 - metrics.ServiceCoverage)), InfrastructureToolRecommendationScore(id, metrics));
            return 0;
        }

        private static int InfrastructureRoadToolScore(CityMetrics metrics)
        {
            if (metrics == null || metrics.InfrastructureResilienceScore < 45)
            {
                return 0;
            }

            return InfrastructureFocusHasAny(metrics, "\u9053\u8def", "\u517b\u62a4", "\u8fd0\u7ef4")
                ? Mathf.Clamp(metrics.InfrastructureResilienceScore - 4, 0, 96)
                : 0;
        }

        private static int InfrastructureToolRecommendationScore(string buildingId, CityMetrics metrics)
        {
            // INFRASTRUCTURE_RESILIENCE_TOOL_RECOMMENDATIONS converts the resilience advisor into build-order highlights.
            if (string.IsNullOrEmpty(buildingId) || metrics == null || metrics.InfrastructureResilienceScore < 45)
            {
                return 0;
            }

            var score = metrics.InfrastructureResilienceScore;
            if (buildingId == "rain_garden" && InfrastructureFocusHasAny(metrics, "\u96e8\u6d2a", "\u5185\u6d9d"))
            {
                return Mathf.Clamp(score + Math.Max(0, metrics.FloodRisk - 45) / 2, 0, 98);
            }

            if ((buildingId == "micro_power" || buildingId == "solar_farm" || buildingId == "water_tower" || buildingId == "water_reclaimer")
                && InfrastructureFocusHasAny(metrics, "\u6c34\u7535", "\u6c61\u6c34"))
            {
                return Mathf.Clamp(score + Math.Max(0, metrics.UtilityUtilization - 100) / 2, 0, 96);
            }

            if (buildingId == "road_maintenance_depot" && InfrastructureFocusHasAny(metrics, "\u9053\u8def", "\u517b\u62a4", "\u8fd0\u7ef4"))
            {
                return Mathf.Clamp(score + Math.Max(0, 55 - metrics.RoadMaintenanceCoverage) / 2, 0, 98);
            }

            if (buildingId == "emergency_shelter" && InfrastructureFocusHasAny(metrics, "\u5e94\u6025", "\u707e\u5907", "\u907f\u96be"))
            {
                return Mathf.Clamp(score + Math.Max(0, metrics.DisasterRisk - 45) / 2, 0, 96);
            }

            if ((buildingId == "health_post" || buildingId == "district_hospital" || buildingId == "fire_station" || buildingId == "police_kiosk" || buildingId == "police_precinct")
                && InfrastructureFocusHasAny(metrics, "\u5e94\u6025"))
            {
                return Mathf.Clamp(score - 8, 0, 88);
            }

            return 0;
        }

        private static string InfrastructureToolDriverLabel(string buildingId, CityMetrics metrics, string fallback)
        {
            if (metrics == null || InfrastructureToolRecommendationScore(buildingId, metrics) < 55)
            {
                return fallback;
            }

            if (!string.IsNullOrEmpty(metrics.InfrastructureResilienceDriver))
            {
                return CompactCardText(metrics.InfrastructureResilienceDriver, 7);
            }

            return string.IsNullOrEmpty(metrics.InfrastructureResilienceFocus)
                ? fallback
                : CompactCardText(metrics.InfrastructureResilienceFocus, 7);
        }

        private static bool InfrastructureFocusHasAny(CityMetrics metrics, params string[] markers)
        {
            if (metrics == null || markers == null)
            {
                return false;
            }

            var text = (metrics.InfrastructureResilienceFocus ?? string.Empty)
                + " " + (metrics.InfrastructureResilienceDriver ?? string.Empty)
                + " " + (metrics.InfrastructureResilienceAction ?? string.Empty);
            for (var i = 0; i < markers.Length; i += 1)
            {
                if (!string.IsNullOrEmpty(markers[i]) && text.Contains(markers[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private int ToolRecommendationScoreWithSelectedTile(ToolButtonBinding binding, CityMetrics metrics)
        {
            var cityScore = ToolRecommendationScore(binding, metrics);
            return Mathf.Clamp(Mathf.Max(cityScore, SelectedTileToolRecommendationBonus(binding, metrics)), 0, 100);
        }

        private int SelectedTileToolRecommendationBonus(ToolButtonBinding binding, CityMetrics metrics)
        {
            // CITY_SKYLINES_SELECTED_TILE_TOOL_LINK makes the bottom dock react to the inspected tile's diagnosis.
            if (binding == null || interaction == null || controller == null || !interaction.HasSelectedTile)
            {
                return 0;
            }

            var pos = interaction.SelectedTile;
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return 0;
            }

            var hasUse = TileHasUse(tile);
            if (binding.ToolMode == CityToolMode.UpgradeRoad && !string.IsNullOrEmpty(tile.RoadId))
            {
                return tile.Traffic >= 70 ? 96 : (tile.Traffic >= 45 || tile.RoadMaintenanceAccess < 24 ? 78 : 0);
            }

            if (binding.ToolMode == CityToolMode.BuildRoad)
            {
                if (tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.BuildingId) && string.IsNullOrEmpty(tile.RoadId) && tile.Terrain == TerrainType.Plain)
                {
                    return metrics != null && metrics.RoadConnectivity < 55 ? 78 : 62;
                }

                return tile.Traffic >= 55 ? 72 : 0;
            }

            if (binding.ToolMode == CityToolMode.ZonePaint)
            {
                var openPlain = tile.Terrain == TerrainType.Plain
                    && tile.Zone == ZoneType.None
                    && string.IsNullOrEmpty(tile.BuildingId)
                    && string.IsNullOrEmpty(tile.RoadId);
                if (!openPlain)
                {
                    return 0;
                }

                var demand = metrics != null ? DemandForZone(metrics, binding.Zone) : 0;
                return Mathf.Clamp(demand + 18, 0, 92);
            }

            if (binding.ToolMode != CityToolMode.BuildBuilding || string.IsNullOrEmpty(binding.BuildingId))
            {
                return 0;
            }

            var id = binding.BuildingId;
            if (id == "parking_garage" && hasUse && tile.ParkingAccess < 28 && tile.Traffic >= 6) return 96;
            if (id == "rain_garden" && (tile.StormwaterAccess < 28 || (metrics != null && metrics.FloodRisk >= 48))) return 94;
            if (id == "road_maintenance_depot" && (!string.IsNullOrEmpty(tile.RoadId) || tile.Traffic > 0) && tile.RoadMaintenanceAccess < 30) return 94;
            if ((id == "bus_hub" || id == "metro_station") && hasUse && tile.TransitAccess < 28 && tile.Traffic >= 8) return 90;
            if ((id == "distribution_center" || id == "cargo_depot" || id == "freight_rail_terminal") && hasUse && tile.LogisticsAccess < 28 && tile.Traffic >= 8) return 88;
            if ((id == "recycling_yard" || id == "waste_to_energy_plant") && hasUse && tile.WasteAccess < 28) return 90;
            if (id == "telecom_hub" && hasUse && tile.CommunicationAccess < 28) return 88;
            if (id == "post_office" && hasUse && tile.MailAccess < 28) return 86;
            if ((id == "micro_power" || id == "solar_farm" || id == "water_tower" || id == "water_reclaimer")
                && metrics != null
                && (metrics.UtilityReliability < 92 || metrics.UtilityUtilization > 108 || metrics.WastewaterUtilization > 108))
            {
                return 86;
            }

            if (!hasUse)
            {
                return 0;
            }

            var weakestServiceScore = SelectedTileServiceToolBonus(id, tile);
            if (weakestServiceScore > 0)
            {
                return weakestServiceScore;
            }

            if ((id == "pocket_park" || id == "city_plaza") && tile.LandValue < 36) return 84;
            if (IsServiceTool(id) && ServiceAccessValue(tile) < 24) return 78;
            return 0;
        }

        private static int SelectedTileServiceToolBonus(string buildingId, TileData tile)
        {
            if (tile == null || string.IsNullOrEmpty(buildingId))
            {
                return 0;
            }

            if ((buildingId == "pocket_park" || buildingId == "city_plaza") && tile.ParkAccess < 26) return 92;
            if ((buildingId == "health_post" || buildingId == "district_hospital") && tile.HealthAccess < 26) return 94;
            if ((buildingId == "primary_school" || buildingId == "community_college") && tile.EducationAccess < 26) return 92;
            if (buildingId == "fire_station" && tile.FireProtectionAccess < 26) return 92;
            if ((buildingId == "police_kiosk" || buildingId == "police_precinct") && Mathf.Max(tile.SafetyAccess, tile.SecurityAccess) < 26) return 92;
            if (buildingId == "memorial_garden" && tile.DeathcareAccess < 26) return 84;
            return 0;
        }

        private static int DemandForZone(CityMetrics metrics, ZoneType zone)
        {
            if (metrics == null || metrics.Demand == null)
            {
                return 0;
            }

            if (zone == ZoneType.Residential) return metrics.Demand.Residential;
            if (zone == ZoneType.Commercial) return metrics.Demand.Commercial;
            if (zone == ZoneType.Industrial) return metrics.Demand.Industrial;
            if (zone == ZoneType.Office) return metrics.Demand.Office;
            if (zone == ZoneType.MixedUse) return metrics.Demand.MixedUse;
            if (zone == ZoneType.Civic) return metrics.Demand.Service;
            if (zone == ZoneType.Utility) return metrics.Demand.Utility;
            return 0;
        }

        private static Color32 BlendToolRecommendationColor(Color32 baseColor, Color32 targetColor, float amount)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.r, targetColor.r, amount)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.g, targetColor.g, amount)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.b, targetColor.b, amount)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(baseColor.a, targetColor.a, amount)));
        }

        private static bool IsResidentialTool(string buildingId)
        {
            return buildingId == "residential_pod" || buildingId == "apartment_block";
        }

        private static bool IsCommercialTool(string buildingId)
        {
            return buildingId == "market_corner" || buildingId == "mixed_use_block" || buildingId == "office_studio" || buildingId == "research_campus" || buildingId == "convention_center";
        }

        private static bool IsIndustrialTool(string buildingId)
        {
            return buildingId == "maker_yard" || buildingId == "resource_processor";
        }

        private static bool IsTransitOrLogisticsTool(string buildingId)
        {
            return buildingId == "bus_hub" || buildingId == "metro_station" || buildingId == "intercity_terminal" || buildingId == "cargo_depot" || buildingId == "distribution_center" || buildingId == "freight_rail_terminal";
        }

        private static bool IsServiceTool(string buildingId)
        {
            return buildingId == "pocket_park" || buildingId == "city_plaza" || buildingId == "city_hall" || buildingId == "health_post" || buildingId == "district_hospital" || buildingId == "memorial_garden" || buildingId == "emergency_shelter" || buildingId == "primary_school" || buildingId == "community_college" || buildingId == "fire_station" || buildingId == "police_kiosk" || buildingId == "police_precinct" || buildingId == "telecom_hub" || buildingId == "post_office" || buildingId == "road_maintenance_depot" || buildingId == "parking_garage";
        }

        private static bool IsUtilityTool(string buildingId)
        {
            return buildingId == "rain_garden" || buildingId == "micro_power" || buildingId == "solar_farm" || buildingId == "water_tower" || buildingId == "water_reclaimer" || buildingId == "waste_to_energy_plant" || buildingId == "recycling_yard";
        }

        private Text CreateText(Transform parent, string name, string text, int size, FontStyle style, TextAnchor alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var label = obj.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.text = text;
            label.color = new Color32(43, 64, 70, 255);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            obj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return label;
        }

        private Text CreateDemandStatTile(Transform parent, string name)
        {
            var tile = new GameObject(name + " Tile");
            tile.transform.SetParent(parent, false);
            var image = tile.AddComponent<Image>();
            image.color = DemandStatBackplateColor(false);
            var outline = tile.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 220, 103, 0);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.enabled = false;
            tile.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var fillObject = new GameObject("Demand Fill");
            fillObject.transform.SetParent(tile.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 0f);
            fillRect.offsetMin = new Vector2(3f, 1f);
            fillRect.offsetMax = new Vector2(-3f, 5f);
            var fill = fillObject.AddComponent<Image>();
            fill.color = new Color32(92, 204, 112, 212);
            fill.raycastTarget = false;
            demandFillBars.Add(fill);

            var groupObject = new GameObject("Demand Group Band");
            groupObject.transform.SetParent(tile.transform, false);
            var groupRect = groupObject.AddComponent<RectTransform>();
            groupRect.anchorMin = new Vector2(0f, 0f);
            groupRect.anchorMax = new Vector2(0f, 1f);
            groupRect.offsetMin = new Vector2(0f, 2f);
            groupRect.offsetMax = new Vector2(4f, -2f);
            var group = groupObject.AddComponent<Image>();
            group.color = DemandGroupBaseColor(string.Empty);
            group.raycastTarget = false;
            demandGroupBars.Add(group);

            var hotObject = new GameObject("Demand Hot Corner");
            hotObject.transform.SetParent(tile.transform, false);
            var hotRect = hotObject.AddComponent<RectTransform>();
            hotRect.anchorMin = new Vector2(1f, 1f);
            hotRect.anchorMax = new Vector2(1f, 1f);
            hotRect.pivot = new Vector2(1f, 1f);
            hotRect.sizeDelta = new Vector2(10f, 10f);
            hotRect.anchoredPosition = new Vector2(-2f, -2f);
            var hotImage = hotObject.AddComponent<Image>();
            hotImage.color = new Color32(255, 202, 70, 0);
            hotImage.raycastTarget = false;
            var hotLayout = hotObject.AddComponent<LayoutElement>();
            hotLayout.ignoreLayout = true;
            demandHotCorners.Add(hotImage);

            var tagObject = new GameObject("Demand Group Tag");
            tagObject.transform.SetParent(tile.transform, false);
            var tag = tagObject.AddComponent<Text>();
            tag.font = font;
            tag.fontSize = 7;
            tag.fontStyle = FontStyle.Bold;
            tag.alignment = TextAnchor.MiddleCenter;
            tag.horizontalOverflow = HorizontalWrapMode.Overflow;
            tag.verticalOverflow = VerticalWrapMode.Truncate;
            tag.text = string.Empty;
            tag.color = new Color32(245, 255, 238, 0);
            tag.raycastTarget = false;
            var tagRect = tag.rectTransform;
            tagRect.anchorMin = new Vector2(0f, 1f);
            tagRect.anchorMax = new Vector2(0f, 1f);
            tagRect.pivot = new Vector2(0f, 1f);
            tagRect.sizeDelta = new Vector2(13f, 10f);
            tagRect.anchoredPosition = new Vector2(4f, -2f);
            var tagLayout = tagObject.AddComponent<LayoutElement>();
            tagLayout.ignoreLayout = true;
            demandGroupTags.Add(tag);

            var label = CreateText(tile.transform, name, "--", 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(13f, 0f);
            label.rectTransform.offsetMax = new Vector2(-2f, 0f);
            return label;
        }

        private static void AddSoftCardShadow(GameObject obj, byte alpha = 58)
        {
            // REFERENCE_IMAGE_SOFT_CARD_SHADOWS lift dense HUD panels off the city scene.
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color32(22, 48, 38, alpha);
            shadow.effectDistance = new Vector2(3f, -3f);
            shadow.useGraphicAlpha = true;
        }

        private void AddPanelTopAccent(GameObject obj, Color32 color, float height)
        {
            // REFERENCE_IMAGE_HUD_GOLD_TOP_EDGES unifies resource, demand, task, dock and minimap panels.
            var accent = new GameObject("Panel Top Accent");
            accent.transform.SetParent(obj.transform, false);
            var rect = accent.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(8f, -height - 2f);
            rect.offsetMax = new Vector2(-8f, -2f);
            var image = accent.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var layout = accent.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var shine = new GameObject("Panel Top Shine");
            shine.transform.SetParent(obj.transform, false);
            var shineRect = shine.AddComponent<RectTransform>();
            shineRect.anchorMin = new Vector2(0f, 1f);
            shineRect.anchorMax = new Vector2(1f, 1f);
            shineRect.offsetMin = new Vector2(12f, -height - 7f);
            shineRect.offsetMax = new Vector2(-26f, -height - 4f);
            var shineImage = shine.AddComponent<Image>();
            shineImage.color = new Color32(255, 255, 255, 54);
            shineImage.raycastTarget = false;
            var shineLayout = shine.AddComponent<LayoutElement>();
            shineLayout.ignoreLayout = true;
            AddPanelCornerFacets(obj, color, 58);
            AddPanelInnerGlassFacets(obj, color);
        }

        private void AddPanelCornerFacets(GameObject obj, Color32 color, byte alpha)
        {
            // REFERENCE_IMAGE_LOW_POLY_PANEL_CORNERS gives HUD cards clipped, faceted game-panel edges.
            var facetColor = new Color32(color.r, color.g, color.b, alpha);
            AddPanelCornerFacet(obj, "Panel Corner Top Left", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, 3f), new Vector2(10f, -9f), facetColor);
            AddPanelCornerFacet(obj, "Panel Corner Top Right", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(32f, 3f), new Vector2(-10f, -9f), facetColor);
            AddPanelCornerFacet(obj, "Panel Corner Left Facet", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, 26f), new Vector2(10f, -12f), facetColor);
            AddPanelCornerFacet(obj, "Panel Corner Right Facet", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(3f, 26f), new Vector2(-10f, -12f), facetColor);
        }

        private void AddPanelCornerFacet(GameObject obj, string name, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position, Color32 color)
        {
            var facet = new GameObject(name);
            facet.transform.SetParent(obj.transform, false);
            var rect = facet.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = facet.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var layout = facet.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private void AddPanelInnerGlassFacets(GameObject obj, Color32 color)
        {
            // REFERENCE_IMAGE_GLASS_PANEL_FACETS adds the soft top sheen and bottom fold seen in the target UI.
            AddPanelGlassFacet(obj, "Panel Inner Glass Sheen", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -20f), new Vector2(-34f, -9f), new Color32(255, 255, 255, 32));
            AddPanelGlassFacet(obj, "Panel Bottom Fold", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 7f), new Vector2(-16f, 11f), new Color32(color.r, color.g, color.b, 34));
            AddPanelGlassFacet(obj, "Panel Bottom Cool Edge", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 2f), new Vector2(-24f, 5f), new Color32(92, 184, 201, 26));
        }

        private void AddPanelGlassFacet(GameObject obj, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color32 color)
        {
            var facet = new GameObject(name);
            facet.transform.SetParent(obj.transform, false);
            var rect = facet.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = facet.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var layout = facet.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private Image AddHudFacet(Transform parent, string name, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax, Color32 color, float rotation)
        {
            var facet = CreatePanel(parent, name, anchors, offsetMin, offsetMax);
            var rect = facet.GetComponent<RectTransform>();
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = facet.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var layout = facet.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return image;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector4 anchors, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchors.x, anchors.y);
            rect.anchorMax = new Vector2(anchors.z, anchors.w);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            obj.AddComponent<Image>();
            return obj;
        }

        private void AddHorizontalLayout(GameObject obj, int spacing, int padding, TextAnchor alignment)
        {
            var layout = obj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = alignment;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private void AddVerticalLayout(GameObject obj, int spacing, int padding)
        {
            var layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Vector4 AnchorStretch()
        {
            return new Vector4(0f, 0f, 1f, 1f);
        }

        private static Vector4 AnchorTop()
        {
            return new Vector4(0f, 1f, 1f, 1f);
        }

        private static Vector4 AnchorTopLeft()
        {
            return new Vector4(0f, 1f, 0f, 1f);
        }

        private static Vector4 AnchorTopRight()
        {
            return new Vector4(1f, 1f, 1f, 1f);
        }

        private static Vector4 AnchorBottom()
        {
            return new Vector4(0f, 0f, 1f, 0f);
        }

        private static Vector4 AnchorBottomLeft()
        {
            return new Vector4(0f, 0f, 0f, 0f);
        }

        private static Vector4 AnchorLeft()
        {
            return new Vector4(0f, 0f, 0f, 1f);
        }

        private static Vector4 AnchorRight()
        {
            return new Vector4(1f, 0f, 1f, 1f);
        }

        private static Vector4 AnchorBottomRight()
        {
            return new Vector4(1f, 0f, 1f, 0f);
        }

        private static Vector4 AnchorFree()
        {
            return new Vector4(0f, 0f, 1f, 1f);
        }

        private static string ZoneLabel(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return "\u4f4f\u5b85\u533a";
            if (zone == ZoneType.Commercial) return "\u5546\u4e1a\u533a";
            if (zone == ZoneType.MixedUse) return "\u6df7\u5408\u533a";
            if (zone == ZoneType.Office) return "\u529e\u516c\u533a";
            if (zone == ZoneType.Industrial) return "\u5de5\u4e1a\u533a";
            if (zone == ZoneType.Civic) return "\u670d\u52a1\u533a";
            if (zone == ZoneType.Utility) return "\u8bbe\u65bd\u533a";
            return "\u672a\u5206\u533a";
        }

        private static string TerrainLabel(TerrainType terrain)
        {
            if (terrain == TerrainType.Water) return "\u6c34\u9762";
            if (terrain == TerrainType.Hill) return "\u4e18\u9675";
            return "\u5e73\u5730";
        }

        private static string OverlayLabel(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic) return "\u4ea4\u901a";
            if (mode == OverlayMode.Pollution) return "\u6c61\u67d3";
            if (mode == OverlayMode.Zoning) return "\u5206\u533a";
            if (mode == OverlayMode.Services) return "\u670d\u52a1";
            if (mode == OverlayMode.Transit) return "\u516c\u4ea4";
            if (mode == OverlayMode.LandValue) return "\u5730\u4ef7";
            if (mode == OverlayMode.Waste) return "\u56de\u6536";
            if (mode == OverlayMode.Logistics) return "\u8d27\u8fd0";
            if (mode == OverlayMode.Utilities) return "\u6c34\u7535";
            if (mode == OverlayMode.Communications) return "\u901a\u4fe1";
            if (mode == OverlayMode.RoadSafety) return "\u8def\u5b89";
            if (mode == OverlayMode.Parking) return "\u505c\u8f66";
            if (mode == OverlayMode.Stormwater) return "\u96e8\u6d2a";
            return "\u666e\u901a";
        }

        private static string RoadTierLabel(RoadTier tier)
        {
            if (tier == RoadTier.Arterial)
            {
                return "\u4e3b\u5e72\u9053";
            }

            return "\u652f\u8def";
        }

        private static string BuildingLabel(string buildingId)
        {
            if (buildingId == "residential_pod") return "\u4f4f\u5b85\u8231";
            if (buildingId == "apartment_block") return "\u516c\u5bd3";
            if (buildingId == "market_corner") return "\u5546\u94fa";
            if (buildingId == "mixed_use_block") return "\u6df7\u5408\u697c";
            if (buildingId == "office_studio") return "\u529e\u516c";
            if (buildingId == "research_campus") return "\u7814\u53d1";
            if (buildingId == "maker_yard") return "\u5de5\u574a";
            if (buildingId == "resource_processor") return "\u8d44\u6e90";
            if (buildingId == "pocket_park") return "\u516c\u56ed";
            if (buildingId == "city_plaza") return "\u5e7f\u573a";
            if (buildingId == "convention_center") return "\u4f1a\u5c55";
            if (buildingId == "city_hall") return "\u5e02\u653f\u5385";
            if (buildingId == "health_post") return "\u8bca\u6240";
            if (buildingId == "district_hospital") return "\u533b\u9662";
            if (buildingId == "memorial_garden") return "\u751f\u547d\u56ed";
            if (buildingId == "emergency_shelter") return "\u907f\u96be";
            if (buildingId == "bus_hub") return "\u516c\u4ea4";
            if (buildingId == "metro_station") return "\u5730\u94c1";
            if (buildingId == "intercity_terminal") return "\u57ce\u9645";
            if (buildingId == "cargo_depot") return "\u8d27\u8fd0";
            if (buildingId == "distribution_center") return "\u4ed3\u50a8";
            if (buildingId == "freight_rail_terminal") return "\u94c1\u8d27";
            if (buildingId == "primary_school") return "\u5b66\u6821";
            if (buildingId == "community_college") return "\u5b66\u9662";
            if (buildingId == "fire_station") return "\u6d88\u9632";
            if (buildingId == "police_kiosk") return "\u8b66\u52a1";
            if (buildingId == "police_precinct") return "\u5206\u5c40";
            if (buildingId == "telecom_hub") return "\u901a\u4fe1";
            if (buildingId == "post_office") return "\u90ae\u653f";
            if (buildingId == "road_maintenance_depot") return "\u517b\u62a4";
            if (buildingId == "parking_garage") return "\u505c\u8f66\u697c";
            if (buildingId == "rain_garden") return "\u96e8\u6c34\u56ed";
            if (buildingId == "micro_power") return "\u7535\u7ad9";
            if (buildingId == "solar_farm") return "\u592a\u9633\u80fd";
            if (buildingId == "water_tower") return "\u6c34\u5854";
            if (buildingId == "water_reclaimer") return "\u6c61\u6c34";
            if (buildingId == "waste_to_energy_plant") return "\u5783\u573e\u7535";
            if (buildingId == "recycling_yard") return "\u56de\u6536";
            return buildingId;
        }

        private string PolicyStatusText()
        {
            if (controller == null || controller.Metrics == null)
            {
                return string.Empty;
            }

            var policies = controller.Metrics.ActivePolicies;
            if (policies == null || policies.Count == 0)
            {
                return "  \u653f\u7b56\uff1a\u65e0";
            }

            return "  \u653f\u7b56\uff1a" + policies.Count + "  \u6536\u652f " + FormatSigned(-controller.Metrics.PolicyExpense);
        }

        private string TaxStatusText()
        {
            if (controller == null || controller.Metrics == null)
            {
                return string.Empty;
            }

            return "  \u7a0e\u7387\uff1a" + TaxLabel(controller.Metrics.TaxLevel) + " " + controller.Metrics.TaxRatePercent + "%";
        }

        private string BudgetStatusText()
        {
            if (controller == null || controller.Metrics == null)
            {
                return string.Empty;
            }

            return "  \u670d\u52a1\uff1a" + BudgetLabel(controller.Metrics.ServiceBudgetLevel) + " " + controller.Metrics.ServiceBudgetPercent + "% " + FormatSigned(controller.Metrics.ServiceBudgetExpense);
        }

        private static string TaxLabel(CityTaxLevel level)
        {
            if (level == CityTaxLevel.Low)
            {
                return "\u4f4e";
            }

            if (level == CityTaxLevel.High)
            {
                return "\u9ad8";
            }

            return "\u6807\u51c6";
        }

        private static string BudgetLabel(CityServiceBudgetLevel level)
        {
            if (level == CityServiceBudgetLevel.Lean)
            {
                return "\u7d27\u7f29";
            }

            if (level == CityServiceBudgetLevel.Boosted)
            {
                return "\u52a0\u7801";
            }

            return "\u6807\u51c6";
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private string TimeStatusText()
        {
            if (controller == null)
            {
                return "--";
            }

            return controller.Paused ? "\u5df2\u6682\u505c" : controller.SimulationSpeed + "x";
        }
    }
}
