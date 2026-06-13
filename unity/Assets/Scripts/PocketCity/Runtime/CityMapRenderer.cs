using System.Collections.Generic;
using PocketCity.Core;
using UnityEngine;

namespace PocketCity.Runtime
{
    public sealed partial class CityMapRenderer : MonoBehaviour
    {
        [SerializeField] private CityGameController controller;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float overlayLift = 0.035f;
        [SerializeField] private float roadHeight = 0.08f;
        [SerializeField] private float buildingBaseHeight = 0.45f;
        [SerializeField] private Material vertexColorMaterial;
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material roadLineMaterial;
        [SerializeField] private Material residentialMaterial;
        [SerializeField] private Material commercialMaterial;
        [SerializeField] private Material mixedUseMaterial;
        [SerializeField] private Material officeMaterial;
        [SerializeField] private Material industrialMaterial;
        [SerializeField] private Material serviceMaterial;
        [SerializeField] private Material utilityMaterial;
        [SerializeField] private Material roofMaterial;
        [SerializeField] private Material windowMaterial;
        [SerializeField] private Material buildingFootprintMaterial;
        [SerializeField] private Material treeTrunkMaterial;
        [SerializeField] private Material treeCanopyMaterial;
        [SerializeField] private Material rockMaterial;
        [SerializeField] private Material shoreMaterial;
        [SerializeField] private Material grassGridMaterial;
        [SerializeField] private Material lockedAreaMaterial;
        [SerializeField] private Material trafficPulseMaterial;
        [SerializeField] private Material serviceNeedMaterial;
        [SerializeField] private Material previewOkMaterial;
        [SerializeField] private Material previewBlockedMaterial;

        private readonly List<GameObject> roadObjects = new List<GameObject>();
        private readonly List<GameObject> buildingObjects = new List<GameObject>();
        private readonly List<GameObject> decorationObjects = new List<GameObject>();
        private readonly List<GameObject> guideObjects = new List<GameObject>();
        private readonly List<GameObject> mapIssueObjects = new List<GameObject>();
        private readonly List<GameObject> planningSignalObjects = new List<GameObject>();
        private readonly List<GameObject> placementPreviewObjects = new List<GameObject>();
        private readonly List<GameObject> selectedTileFocusObjects = new List<GameObject>();
        private readonly List<GameObject> commandResultObjects = new List<GameObject>();

        // Performance: Culling and LOD system
        private SimpleCullingManager cullingManager;
        private SimpleLODManager lodManager;
        [SerializeField] private bool enableCulling = true;
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float cullingUpdateInterval = 0.15f;
        [SerializeField] private float cullDistance = 400f;
        [SerializeField] private float lodHighDistance = 40f;
        [SerializeField] private float lodMediumDistance = 120f;
        [SerializeField] private float lodLowDistance = 250f;

        private struct CityIssueSignal
        {
            public GridPos Pos;
            public TileData Tile;
            public int Severity;
        }

        private struct CoverageNeedSignal
        {
            public GridPos Pos;
            public float Height;
        }

        private struct GroundMarkerSignal
        {
            public GridPos Pos;
            public int Score;
        }

        private struct ZoneOpportunitySignal
        {
            public GridPos Pos;
            public ZoneType Zone;
            public int Score;
        }

        private struct BuildingUpgradeSignal
        {
            public PlacedBuilding Building;
            public TileData Tile;
            public int Score;
            public int GrowthScore;
            public bool Ready;
        }

        private enum ObjectiveFocusKind
        {
            Road,
            Zone,
            Service,
            Transit,
            Utility,
            Upgrade,
            Economy
        }

        private enum CityIssueAdvisorMarkerKind
        {
            General,
            Traffic,
            Service,
            Fiscal,
            Utility
        }

        private Mesh terrainMesh;
        private Mesh overlayMesh;
        private Mesh cubeMesh;
        private MeshFilter terrainFilter;
        private MeshFilter overlayFilter;
        private OverlayMode lastOverlay;
        private int lastRoadCount = -1;
        private int lastRoadSignature = -1;
        private int lastBuildingCount = -1;
        private int lastBuildingSignature = -1;
        private int lastMetricSignature = -1;
        private int lastDay = -1;
        private int placementPreviewSignature = int.MinValue;
        private int selectedTileFocusSignature = int.MinValue;
        private bool lastExpansionUnlocked;
        private float commandResultExpiresAt;

        public float CellSize
        {
            get { return cellSize; }
        }

        private void Awake()
        {
            EnsureMaterials();
            EnsureMeshLayer("Terrain", 0f, ref terrainFilter, ref terrainMesh);
            EnsureMeshLayer("Overlay", overlayLift, ref overlayFilter, ref overlayMesh);

            // Initialize performance optimization systems
            if (enableCulling || enableLOD)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    if (enableCulling)
                    {
                        cullingManager = new SimpleCullingManager(mainCamera, cullingUpdateInterval);
                    }
                    if (enableLOD)
                    {
                        lodManager = new SimpleLODManager(mainCamera, lodHighDistance, lodMediumDistance, lodLowDistance);
                    }
                }
            }
        }

        private void Start()
        {
            RebuildAll();
        }

        private void Update()
        {
            if (commandResultObjects.Count > 0 && Time.time >= commandResultExpiresAt)
            {
                ClearObjects(commandResultObjects);
            }

            if (controller == null || controller.Grid == null || controller.Metrics == null)
            {
                return;
            }

            var roads = controller.Roads;
            var buildings = controller.Buildings;
            var roadCount = roads != null ? roads.Count : 0;
            var roadSignature = RoadVisualSignature(roads);
            var buildingCount = buildings != null ? buildings.Count : 0;
            var buildingSignature = BuildingVisualSignature(buildings);
            var day = controller.Metrics.Day;
            var metricSignature = PlanningMetricSignature(controller.Metrics);
            var expansionUnlocked = controller.Grid.ExpansionUnlocked;
            var expansionChanged = expansionUnlocked != lastExpansionUnlocked;
            var dayChanged = lastDay >= 0 && lastDay != day;
            var buildingsAdded = lastBuildingCount >= 0 && buildingCount > lastBuildingCount;
            var addedBuildingCount = buildingsAdded ? buildingCount - lastBuildingCount : 0;

            if (terrainMesh == null || lastRoadCount != roadCount || lastRoadSignature != roadSignature || lastBuildingCount != buildingCount || lastBuildingSignature != buildingSignature)
            {
                RebuildAll();
                if (buildingsAdded)
                {
                    ShowCityGrowthPulse(addedBuildingCount);
                }
                else if (dayChanged)
                {
                    ShowDailySettlementPulse();
                }

                if (expansionChanged && expansionUnlocked)
                {
                    ShowExpansionUnlockedPulse();
                }

                return;
            }

            if (lastOverlay != controller.OverlayMode || lastDay != day || lastMetricSignature != metricSignature)
            {
                RebuildOverlay();
                RebuildPlanningSignals();
                RebuildMapIssueHotspots();
                if (dayChanged)
                {
                    ShowDailySettlementPulse();
                }

                lastOverlay = controller.OverlayMode;
                lastDay = day;
                lastMetricSignature = metricSignature;
            }

            if (expansionChanged)
            {
                RebuildLockedRegionGuide();
                if (expansionUnlocked)
                {
                    ShowExpansionUnlockedPulse();
                }

                lastExpansionUnlocked = expansionUnlocked;
            }

            // Performance: Apply culling and LOD
            ApplyPerformanceOptimizations();
        }

        private void ApplyPerformanceOptimizations()
        {
            if (enableCulling && cullingManager != null)
            {
                cullingManager.UpdateFrustum();
                cullingManager.CullObjects(buildingObjects, cullDistance);
                cullingManager.CullObjects(decorationObjects, cullDistance * 1.2f);
                cullingManager.CullObjects(planningSignalObjects, cullDistance * 0.8f);
            }

            if (enableLOD && lodManager != null)
            {
                lodManager.UpdateLODs(buildingObjects);
            }
        }

        public void RebuildAll()
        {
            if (controller == null || controller.Grid == null)
            {
                return;
            }

            RebuildTerrain();
            RebuildOverlay();
            RebuildRoads();
            RebuildBuildings();
            RebuildDecorations();
            RebuildLockedRegionGuide();
            RebuildPlanningSignals();
            RebuildMapIssueHotspots();
            lastOverlay = controller.OverlayMode;
            lastRoadCount = controller.Roads != null ? controller.Roads.Count : 0;
            lastRoadSignature = RoadVisualSignature(controller.Roads);
            lastBuildingCount = controller.Buildings != null ? controller.Buildings.Count : 0;
            lastBuildingSignature = BuildingVisualSignature(controller.Buildings);
            lastDay = controller.Metrics != null ? controller.Metrics.Day : -1;
            lastMetricSignature = PlanningMetricSignature(controller.Metrics);
            lastExpansionUnlocked = controller.Grid != null && controller.Grid.ExpansionUnlocked;
        }

        public GridPos WorldToGrid(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            return new GridPos(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
        }

        public void ClearPlacementPreview()
        {
            placementPreviewSignature = int.MinValue;
            ClearObjects(placementPreviewObjects);
        }

        public void ClearSelectedTileFocus()
        {
            selectedTileFocusSignature = int.MinValue;
            ClearObjects(selectedTileFocusObjects);
        }

        public void ShowSelectedTileFocus(GridPos pos)
        {
            // REFERENCE_IMAGE_SELECTED_TILE_CORNERS keeps the last clicked tile readable while previews change.
            var tile = controller != null ? controller.GetTile(pos.X, pos.Y) : null;
            var zone = tile != null ? tile.Zone : ZoneType.None;
            var signature = PlacementPreviewSignature(6, pos, pos, new GridSize(1, 1), true, zone) * 31 + TileDiagnosticSignature(tile);
            if (selectedTileFocusSignature == signature)
            {
                return;
            }

            ClearObjects(selectedTileFocusObjects);
            selectedTileFocusSignature = signature;
            var center = CellCenter(pos, roadHeight + 0.155f);
            var accent = SelectedTileFocusMaterial(tile);
            AddSelectedTileFocusBase(center, accent);
            AddSelectedTileFocusCorners(center, accent);
            AddSelectedTileFocusBeacon(center, tile);
            AddTileContextMicroHints(selectedTileFocusObjects, "SelectedTile", center, tile);
            AddSelectedTileInformationLens(pos, center, tile);
            AddSelectedOpenLotPotentialCue(pos, center, tile);
        }

        public void ShowBuildingPlacementPreview(GridPos pos, GridSize size, bool ok, int siteScore = 0)
        {
            // UNITY_HOVER_DRAG_PREVIEW_GHOST gives city-builder tools immediate map feedback before commit.
            var signature = PlacementPreviewSignature(1, pos, new GridPos(pos.X + size.W - 1, pos.Y + size.H - 1), size, ok, ZoneType.None) * 31 + Mathf.Clamp(siteScore, 0, 100);
            if (placementPreviewSignature == signature)
            {
                return;
            }

            ClearObjects(placementPreviewObjects);
            placementPreviewSignature = signature;
            var material = ok ? previewOkMaterial : previewBlockedMaterial;
            var width = Mathf.Max(1, size.W) * cellSize * 0.86f;
            var depth = Mathf.Max(1, size.H) * cellSize * 0.86f;
            var center = new Vector3((pos.X + size.W * 0.5f) * cellSize, roadHeight + 0.13f, (pos.Y + size.H * 0.5f) * cellSize);
            AddLooseCube(placementPreviewObjects, "BuildingPlacementGhost", material, center, new Vector3(width, 0.08f, depth));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementMast", material, center + new Vector3(0f, 0.22f, 0f), new Vector3(0.18f, 0.34f, 0.18f));
            AddPlacementCornerGuides(center, width, depth, material, "BuildingPlacementCornerGuide");
            AddBuildingConstructionPreviewDetails(center, width, depth, ok);
            AddBuildingPlacementScorePips(center, width, depth, ok, siteScore);
        }

        private void AddBuildingConstructionPreviewDetails(Vector3 center, float width, float depth, bool ok)
        {
            // REFERENCE_IMAGE_CONSTRUCTION_SITE_PREVIEW gives build placement the crane-and-foundation read.
            var fenceMaterial = ok ? roadLineMaterial : previewBlockedMaterial;
            var padMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            AddLooseCube(placementPreviewObjects, "BuildingPlacementFoundationPad", padMaterial, center + new Vector3(0f, -0.035f, 0f), new Vector3(width * 0.82f, 0.026f, depth * 0.82f));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementFenceFront", fenceMaterial, center + new Vector3(0f, 0.09f, -depth * 0.43f), new Vector3(width * 0.74f, 0.05f, 0.04f));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementFenceBack", fenceMaterial, center + new Vector3(0f, 0.09f, depth * 0.43f), new Vector3(width * 0.74f, 0.05f, 0.04f));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementFenceLeft", fenceMaterial, center + new Vector3(-width * 0.43f, 0.09f, 0f), new Vector3(0.04f, 0.05f, depth * 0.74f));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementFenceRight", fenceMaterial, center + new Vector3(width * 0.43f, 0.09f, 0f), new Vector3(0.04f, 0.05f, depth * 0.74f));

            var mastBase = center + new Vector3(width * 0.32f, 0.32f, depth * 0.32f);
            AddLooseCube(placementPreviewObjects, "BuildingPlacementMiniCraneMast", fenceMaterial, mastBase, new Vector3(0.06f, 0.46f, 0.06f));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementMiniCraneArm", fenceMaterial, mastBase + new Vector3(-width * 0.18f, 0.2f, 0f), new Vector3(width * 0.42f, 0.045f, 0.045f));
            AddLooseCube(placementPreviewObjects, "BuildingPlacementMiniCraneHook", fenceMaterial, mastBase + new Vector3(-width * 0.36f, 0.08f, 0f), new Vector3(0.045f, 0.2f, 0.045f));
        }

        private void AddBuildingPlacementScorePips(Vector3 center, float width, float depth, bool ok, int siteScore)
        {
            // CITY_SKYLINES_SITE_SCORE_PREVIEW shows whether a valid building site is strong or merely acceptable.
            if (!ok)
            {
                AddLooseCube(placementPreviewObjects, "BuildingPlacementBlockedScorePip", previewBlockedMaterial, center + new Vector3(width * 0.34f, 0.12f, depth * 0.34f), new Vector3(0.13f, 0.045f, 0.13f));
                return;
            }

            var clampedScore = Mathf.Clamp(siteScore, 0, 100);
            var pipCount = clampedScore >= 76 ? 3 : (clampedScore >= 52 ? 2 : 1);
            for (var i = 0; i < pipCount; i += 1)
            {
                var offset = new Vector3(width * 0.34f - i * 0.15f, 0.12f + i * 0.012f, depth * 0.34f);
                var pipMaterial = clampedScore >= 76 ? windowMaterial : (clampedScore >= 52 ? serviceNeedMaterial : previewOkMaterial);
                AddLooseCube(placementPreviewObjects, "BuildingPlacementSiteScorePip", pipMaterial, center + offset, new Vector3(0.11f, 0.045f, 0.11f));
            }
        }

        public void ShowRoadPlacementPreview(GridPos from, GridPos to, bool ok)
        {
            var signature = PlacementPreviewSignature(2, from, to, new GridSize(1, 1), ok, ZoneType.None);
            if (placementPreviewSignature == signature)
            {
                return;
            }

            ClearObjects(placementPreviewObjects);
            placementPreviewSignature = signature;
            AddRoadPreviewCells(from, to, "RoadPlacementGhost");
            if (!ok)
            {
                AddRoadPreviewRouteStatusBadge(from, to);
            }
        }

        public void ShowZonePlacementPreview(GridPos from, GridPos to, ZoneType zone, bool ok)
        {
            var signature = PlacementPreviewSignature(3, from, to, new GridSize(1, 1), ok, zone);
            if (placementPreviewSignature == signature)
            {
                return;
            }

            ClearObjects(placementPreviewObjects);
            placementPreviewSignature = signature;
            var material = ok ? previewOkMaterial : previewBlockedMaterial;
            var minX = Mathf.Min(from.X, to.X);
            var maxX = Mathf.Max(from.X, to.X);
            var minY = Mathf.Min(from.Y, to.Y);
            var maxY = Mathf.Max(from.Y, to.Y);
            for (var y = minY; y <= maxY; y += 1)
            {
                for (var x = minX; x <= maxX; x += 1)
                {
                    var previewPos = new GridPos(x, y);
                    AddLooseCube(placementPreviewObjects, "ZonePlacementGhost", material, CellCenter(previewPos, 0.12f), new Vector3(cellSize * 0.82f, 0.045f, cellSize * 0.82f));
                    if (x == minX || x == maxX || y == minY || y == maxY)
                    {
                        AddZonePlacementParcelBorder(previewPos, material);
                    }
                }
            }

            var width = (maxX - minX + 1) * cellSize * 0.9f;
            var depth = (maxY - minY + 1) * cellSize * 0.9f;
            var center = new Vector3((minX + maxX + 1) * cellSize * 0.5f, roadHeight + 0.16f, (minY + maxY + 1) * cellSize * 0.5f);
            AddPlacementCornerGuides(center, width, depth, material, "ZonePlacementCornerGuide");
        }

        private void AddZonePlacementParcelBorder(GridPos pos, Material material)
        {
            // CITY_SKYLINES_ZONE_PREVIEW_PARCEL_BORDERS gives drag-zoning a visible lot grid before commit.
            var center = CellCenter(pos, roadHeight + 0.13f);
            var span = cellSize * 0.78f;
            AddLooseCube(placementPreviewObjects, "ZonePlacementParcelBorder", material, center + new Vector3(0f, 0.025f, -span * 0.5f), new Vector3(span, 0.022f, 0.032f));
            AddLooseCube(placementPreviewObjects, "ZonePlacementParcelBorder", material, center + new Vector3(0f, 0.025f, span * 0.5f), new Vector3(span, 0.022f, 0.032f));
            AddLooseCube(placementPreviewObjects, "ZonePlacementParcelBorder", material, center + new Vector3(-span * 0.5f, 0.025f, 0f), new Vector3(0.032f, 0.022f, span));
            AddLooseCube(placementPreviewObjects, "ZonePlacementParcelBorder", material, center + new Vector3(span * 0.5f, 0.025f, 0f), new Vector3(0.032f, 0.022f, span));
        }

        public void ShowSingleTilePlacementPreview(GridPos pos, bool ok)
        {
            var signature = PlacementPreviewSignature(4, pos, pos, new GridSize(1, 1), ok, ZoneType.None);
            if (placementPreviewSignature == signature)
            {
                return;
            }

            ClearObjects(placementPreviewObjects);
            placementPreviewSignature = signature;
            var material = ok ? previewOkMaterial : previewBlockedMaterial;
            var center = CellCenter(pos, 0.14f);
            AddLooseCube(placementPreviewObjects, "SingleTilePlacementGhost", material, center, new Vector3(cellSize * 0.72f, 0.08f, cellSize * 0.72f));
            AddPlacementCornerGuides(center, cellSize * 0.78f, cellSize * 0.78f, material, "SingleTilePlacementCornerGuide");
        }

        public void ShowInspectTileFocus(GridPos pos)
        {
            // CITY_SKYLINES_INSPECT_TILE_FOCUS anchors HUD readouts to the hovered map tile.
            var overlaySignature = controller != null ? (int)controller.OverlayMode : 0;
            var tile = controller != null ? controller.GetTile(pos.X, pos.Y) : null;
            var signature = (PlacementPreviewSignature(5, pos, pos, new GridSize(1, 1), true, ZoneType.None) * 31 + overlaySignature) * 31 + TileDiagnosticSignature(tile);
            if (placementPreviewSignature == signature)
            {
                return;
            }

            ClearObjects(placementPreviewObjects);
            placementPreviewSignature = signature;
            var material = overlaySignature == (int)OverlayMode.Normal ? roadLineMaterial : windowMaterial;
            var center = CellCenter(pos, roadHeight + 0.13f);
            AddLooseCube(placementPreviewObjects, "InspectTileFocusPad", material, center, new Vector3(cellSize * 0.42f, 0.028f, cellSize * 0.42f));
            AddLooseCube(placementPreviewObjects, "InspectTileFocusCross", windowMaterial, center + new Vector3(0f, 0.035f, 0f), new Vector3(cellSize * 0.34f, 0.024f, 0.045f));
            AddLooseCube(placementPreviewObjects, "InspectTileFocusCross", windowMaterial, center + new Vector3(0f, 0.035f, 0f), new Vector3(0.045f, 0.024f, cellSize * 0.34f));
            AddPlacementCornerGuides(center, cellSize * 0.82f, cellSize * 0.82f, material, "InspectTileFocusCorner");
            AddInspectTileDiagnosticCues(pos, tile, controller != null ? controller.OverlayMode : OverlayMode.Normal, center);
        }

        private void AddInspectTileDiagnosticCues(GridPos pos, TileData tile, OverlayMode mode, Vector3 center)
        {
            // CITY_SKYLINES_TILE_DIAGNOSTIC_BADGES turn inspect hover into an in-map information readout.
            if (tile == null)
            {
                AddInspectStatusBeacon(center, previewBlockedMaterial, 36);
                return;
            }

            if (tile.Terrain == TerrainType.Water)
            {
                AddInspectWaterCue(center);
                return;
            }

            var metrics = controller != null ? controller.Metrics : null;
            var cueMode = mode == OverlayMode.Normal ? PrimaryInspectIssueMode(tile, metrics) : mode;
            var pressure = Mathf.Max(InspectPressureScore(tile, cueMode, metrics), CityIssueSeverity(tile, metrics));
            var material = InspectPressureMaterial(pressure);
            AddInspectStatusBeacon(center, material, pressure);
            AddInspectModeGlyph(center + new Vector3(-cellSize * 0.32f, 0.16f, -cellSize * 0.32f), cueMode, material);
            AddTileContextMicroHints(placementPreviewObjects, "InspectTile", center, tile);

            if (cueMode == OverlayMode.Zoning && tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.RoadId) && string.IsNullOrEmpty(tile.BuildingId))
            {
                AddInspectParcelOpportunityCue(center, metrics);
            }

            if (!string.IsNullOrEmpty(tile.RoadId) || tile.Traffic >= 45)
            {
                AddInspectTrafficCue(center, tile);
            }

            if (NeedsCoverageSignal(tile, cueMode, metrics))
            {
                AddInspectNeedBracket(center, material);
            }
        }

        private void AddInspectWaterCue(Vector3 center)
        {
            AddLooseCube(placementPreviewObjects, "InspectWaterKeepPad", windowMaterial, center + new Vector3(0f, 0.02f, 0f), new Vector3(cellSize * 0.46f, 0.022f, cellSize * 0.28f));
            AddLooseCube(placementPreviewObjects, "InspectWaterWave", roadLineMaterial, center + new Vector3(0f, 0.07f, -cellSize * 0.12f), new Vector3(cellSize * 0.32f, 0.02f, 0.035f));
            AddLooseCube(placementPreviewObjects, "InspectWaterWave", roadLineMaterial, center + new Vector3(0f, 0.085f, cellSize * 0.12f), new Vector3(cellSize * 0.32f, 0.02f, 0.035f));
        }

        private void AddInspectStatusBeacon(Vector3 center, Material material, int pressure)
        {
            var clamped = Mathf.Clamp(pressure, 0, 90);
            var height = 0.12f + clamped * 0.004f;
            var beaconCenter = center + new Vector3(cellSize * 0.32f, height * 0.5f + 0.08f, cellSize * 0.32f);
            AddLooseCube(placementPreviewObjects, "InspectDiagnosticBeaconBase", material, center + new Vector3(cellSize * 0.32f, 0.065f, cellSize * 0.32f), new Vector3(0.2f, 0.035f, 0.2f));
            AddLooseCube(placementPreviewObjects, "InspectDiagnosticBeacon", material, beaconCenter, new Vector3(0.095f, height, 0.095f));
            AddLooseCube(placementPreviewObjects, "InspectDiagnosticBeaconCap", roadLineMaterial, beaconCenter + new Vector3(0f, height * 0.5f + 0.04f, 0f), new Vector3(0.18f, 0.04f, 0.18f));
        }

        private void AddInspectModeGlyph(Vector3 center, OverlayMode mode, Material material)
        {
            AddLooseCube(placementPreviewObjects, "InspectModeBadgePad", material, center, new Vector3(0.25f, 0.035f, 0.25f));

            if (mode == OverlayMode.Services)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeServicePlus", roadLineMaterial, center + new Vector3(0f, 0.042f, 0f), new Vector3(0.2f, 0.03f, 0.055f));
                AddLooseCube(placementPreviewObjects, "InspectModeServicePlus", roadLineMaterial, center + new Vector3(0f, 0.044f, 0f), new Vector3(0.055f, 0.03f, 0.2f));
                return;
            }

            if (mode == OverlayMode.Transit)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeTransitTrack", roadLineMaterial, center + new Vector3(0f, 0.044f, -0.06f), new Vector3(0.23f, 0.028f, 0.035f));
                AddLooseCube(placementPreviewObjects, "InspectModeTransitTrack", roadLineMaterial, center + new Vector3(0f, 0.044f, 0.06f), new Vector3(0.23f, 0.028f, 0.035f));
                return;
            }

            if (mode == OverlayMode.Logistics)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeCargoBox", serviceNeedMaterial, center + new Vector3(-0.045f, 0.062f, 0f), new Vector3(0.11f, 0.08f, 0.11f));
                AddLooseCube(placementPreviewObjects, "InspectModeCargoBox", material, center + new Vector3(0.055f, 0.085f, 0.02f), new Vector3(0.12f, 0.1f, 0.1f));
                return;
            }

            if (mode == OverlayMode.Waste)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeWasteBin", material, center + new Vector3(0f, 0.075f, 0f), new Vector3(0.14f, 0.11f, 0.13f));
                AddLooseCube(placementPreviewObjects, "InspectModeWasteLid", roadLineMaterial, center + new Vector3(0f, 0.14f, 0f), new Vector3(0.18f, 0.03f, 0.14f));
                return;
            }

            if (mode == OverlayMode.Communications)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeCommsMast", material, center + new Vector3(0f, 0.11f, 0f), new Vector3(0.045f, 0.19f, 0.045f));
                AddLooseCube(placementPreviewObjects, "InspectModeCommsHead", roadLineMaterial, center + new Vector3(0f, 0.21f, 0f), new Vector3(0.2f, 0.035f, 0.045f));
                return;
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeUtilityDrop", windowMaterial, center + new Vector3(0f, 0.07f, 0f), new Vector3(0.16f, 0.07f, 0.16f));
                AddLooseCube(placementPreviewObjects, "InspectModeUtilityPipe", roadLineMaterial, center + new Vector3(0f, 0.12f, 0f), new Vector3(0.22f, 0.03f, 0.055f));
                return;
            }

            if (mode == OverlayMode.Pollution)
            {
                AddLooseCube(placementPreviewObjects, "InspectModePollutionStack", material, center + new Vector3(-0.045f, 0.1f, 0f), new Vector3(0.055f, 0.16f, 0.055f));
                AddLooseCube(placementPreviewObjects, "InspectModePollutionPuff", trafficPulseMaterial, center + new Vector3(0.06f, 0.18f, 0f), new Vector3(0.13f, 0.07f, 0.13f));
                return;
            }

            if (mode == OverlayMode.LandValue || mode == OverlayMode.Zoning)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeParcelPlaque", grassGridMaterial, center + new Vector3(0f, 0.05f, 0f), new Vector3(0.18f, 0.045f, 0.18f));
                AddLooseCube(placementPreviewObjects, "InspectModeParcelTick", roadLineMaterial, center + new Vector3(-0.055f, 0.085f, -0.055f), new Vector3(0.1f, 0.026f, 0.03f));
                AddLooseCube(placementPreviewObjects, "InspectModeParcelTick", roadLineMaterial, center + new Vector3(-0.055f, 0.088f, -0.055f), new Vector3(0.03f, 0.026f, 0.1f));
                return;
            }

            AddLooseCube(placementPreviewObjects, "InspectModeRoadCue", roadLineMaterial, center + new Vector3(0f, 0.045f, 0f), new Vector3(0.24f, 0.03f, 0.055f));
            if (mode == OverlayMode.Parking)
            {
                AddLooseCube(placementPreviewObjects, "InspectModeParkingBlock", roadLineMaterial, center + new Vector3(0.06f, 0.1f, 0f), new Vector3(0.07f, 0.08f, 0.07f));
            }
        }

        private void AddInspectTrafficCue(Vector3 center, TileData tile)
        {
            var material = tile.Traffic >= 70 ? trafficPulseMaterial : serviceNeedMaterial;
            AddLooseCube(placementPreviewObjects, "InspectTrafficLoadBand", material, center + new Vector3(0f, 0.09f, cellSize * 0.3f), new Vector3(cellSize * 0.38f, 0.026f, 0.055f));
            if (tile.Traffic >= 70)
            {
                AddLooseCube(placementPreviewObjects, "InspectTrafficQueueTick", windowMaterial, center + new Vector3(-cellSize * 0.14f, 0.125f, cellSize * 0.3f), new Vector3(0.045f, 0.055f, 0.045f));
                AddLooseCube(placementPreviewObjects, "InspectTrafficQueueTick", windowMaterial, center + new Vector3(cellSize * 0.14f, 0.125f, cellSize * 0.3f), new Vector3(0.045f, 0.055f, 0.045f));
            }
        }

        private void AddInspectNeedBracket(Vector3 center, Material material)
        {
            AddLooseCube(placementPreviewObjects, "InspectNeedBracket", material, center + new Vector3(-cellSize * 0.36f, 0.07f, cellSize * 0.36f), new Vector3(cellSize * 0.2f, 0.034f, 0.045f));
            AddLooseCube(placementPreviewObjects, "InspectNeedBracket", material, center + new Vector3(-cellSize * 0.36f, 0.073f, cellSize * 0.36f), new Vector3(0.045f, 0.034f, cellSize * 0.2f));
            AddLooseCube(placementPreviewObjects, "InspectNeedBracket", material, center + new Vector3(cellSize * 0.36f, 0.07f, -cellSize * 0.36f), new Vector3(cellSize * 0.2f, 0.034f, 0.045f));
            AddLooseCube(placementPreviewObjects, "InspectNeedBracket", material, center + new Vector3(cellSize * 0.36f, 0.073f, -cellSize * 0.36f), new Vector3(0.045f, 0.034f, cellSize * 0.2f));
        }

        private void AddInspectParcelOpportunityCue(Vector3 center, CityMetrics metrics)
        {
            var material = InspectDemandMaterial(metrics);
            AddLooseCube(placementPreviewObjects, "InspectParcelOpportunityPad", material, center + new Vector3(0f, 0.055f, -cellSize * 0.31f), new Vector3(cellSize * 0.32f, 0.026f, 0.06f));
            AddLooseCube(placementPreviewObjects, "InspectParcelOpportunityStake", material, center + new Vector3(-cellSize * 0.18f, 0.13f, -cellSize * 0.31f), new Vector3(0.045f, 0.16f, 0.045f));
            AddLooseCube(placementPreviewObjects, "InspectParcelOpportunityFlag", roadLineMaterial, center + new Vector3(-cellSize * 0.1f, 0.21f, -cellSize * 0.31f), new Vector3(0.16f, 0.055f, 0.035f));
        }

        private Material InspectDemandMaterial(CityMetrics metrics)
        {
            if (metrics == null || metrics.Demand == null)
            {
                return residentialMaterial;
            }

            var demand = metrics.Demand;
            var best = Mathf.Max(demand.Residential, Mathf.Max(demand.Commercial, Mathf.Max(demand.Industrial, Mathf.Max(demand.Office, demand.MixedUse))));
            if (best == demand.Commercial) return commercialMaterial;
            if (best == demand.Industrial) return industrialMaterial;
            if (best == demand.Office) return officeMaterial;
            if (best == demand.MixedUse) return mixedUseMaterial;
            return residentialMaterial;
        }

        private OverlayMode PrimaryInspectIssueMode(TileData tile, CityMetrics metrics)
        {
            if (tile == null)
            {
                return OverlayMode.Normal;
            }

            var highestScore = CityIssueSeverity(tile, metrics);
            var mode = highestScore >= 18 ? OverlayMode.Services : OverlayMode.Normal;
            SelectInspectIssueMode(tile.Traffic - 42, OverlayMode.Traffic, ref highestScore, ref mode);
            SelectInspectIssueMode(34 - ServiceAccessValue(tile), OverlayMode.Services, ref highestScore, ref mode);
            SelectInspectIssueMode(32 - tile.TransitAccess + tile.Traffic / 4, OverlayMode.Transit, ref highestScore, ref mode);
            SelectInspectIssueMode(32 - tile.LogisticsAccess + tile.Traffic / 4, OverlayMode.Logistics, ref highestScore, ref mode);
            SelectInspectIssueMode(30 - tile.WasteAccess, OverlayMode.Waste, ref highestScore, ref mode);
            SelectInspectIssueMode(30 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess), OverlayMode.Communications, ref highestScore, ref mode);
            SelectInspectIssueMode(30 - tile.ParkingAccess + tile.Traffic / 4, OverlayMode.Parking, ref highestScore, ref mode);
            SelectInspectIssueMode(PollutionStress(tile) - 24, OverlayMode.Pollution, ref highestScore, ref mode);
            SelectInspectIssueMode(36 - tile.LandValue, OverlayMode.LandValue, ref highestScore, ref mode);
            SelectInspectIssueMode(30 - tile.StormwaterAccess, OverlayMode.Stormwater, ref highestScore, ref mode);
            if (metrics != null)
            {
                SelectInspectIssueMode(Mathf.Max(95 - metrics.UtilityReliability, metrics.UtilityUtilization - 105), OverlayMode.Utilities, ref highestScore, ref mode);
                SelectInspectIssueMode(Mathf.Max(metrics.FloodRisk - 45, 62 - metrics.StormwaterResilience), OverlayMode.Stormwater, ref highestScore, ref mode);
            }

            if (mode == OverlayMode.Normal
                && tile.Zone == ZoneType.None
                && string.IsNullOrEmpty(tile.RoadId)
                && string.IsNullOrEmpty(tile.BuildingId))
            {
                return OverlayMode.Zoning;
            }

            return mode;
        }

        private static void SelectInspectIssueMode(int score, OverlayMode candidate, ref int highestScore, ref OverlayMode mode)
        {
            if (score > highestScore)
            {
                highestScore = score;
                mode = candidate;
            }
        }

        private static int InspectPressureScore(TileData tile, OverlayMode mode, CityMetrics metrics)
        {
            if (tile == null)
            {
                return 0;
            }

            if (mode == OverlayMode.Traffic) return tile.Traffic;
            if (mode == OverlayMode.Pollution) return PollutionStress(tile);
            if (mode == OverlayMode.Services) return Mathf.Max(0, 44 - ServiceAccessValue(tile));
            if (mode == OverlayMode.Transit) return Mathf.Max(0, 42 - tile.TransitAccess + tile.Traffic / 4);
            if (mode == OverlayMode.LandValue) return Mathf.Max(0, LandValueSignalThreshold(metrics) - tile.LandValue);
            if (mode == OverlayMode.Waste) return Mathf.Max(0, 42 - tile.WasteAccess);
            if (mode == OverlayMode.Logistics) return Mathf.Max(0, 42 - tile.LogisticsAccess + tile.Traffic / 4);
            if (mode == OverlayMode.Utilities && metrics != null) return Mathf.Max(Mathf.Max(100 - metrics.UtilityReliability, metrics.UtilityUtilization - 100), metrics.WastewaterUtilization - 100);
            if (mode == OverlayMode.Communications) return Mathf.Max(0, 42 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess));
            if (mode == OverlayMode.RoadSafety) return Mathf.Max(0, 42 - tile.RoadMaintenanceAccess + tile.Traffic / 4);
            if (mode == OverlayMode.Parking) return Mathf.Max(0, 42 - tile.ParkingAccess + tile.Traffic / 4);
            if (mode == OverlayMode.Stormwater) return Mathf.Max(0, Mathf.Max(42 - tile.StormwaterAccess, metrics != null ? Mathf.Max(metrics.FloodRisk - 36, 70 - metrics.StormwaterResilience) : 0));
            if (mode == OverlayMode.Zoning && tile.Zone == ZoneType.None && string.IsNullOrEmpty(tile.RoadId) && string.IsNullOrEmpty(tile.BuildingId)) return 28;
            return CityIssueSeverity(tile, metrics);
        }

        private Material InspectPressureMaterial(int pressure)
        {
            if (pressure >= 46)
            {
                return trafficPulseMaterial;
            }

            if (pressure >= 22)
            {
                return serviceNeedMaterial;
            }

            return previewOkMaterial;
        }

        public void ShowCommandResultMarker(GridPos pos, bool ok, CityToolMode mode = CityToolMode.Inspect)
        {
            // REFERENCE_IMAGE_COMMAND_RESULT_MARKER gives taps a compact in-map success or blocked cue.
            ClearObjects(commandResultObjects);
            commandResultExpiresAt = Time.time + 0.72f;
            var material = ok ? previewOkMaterial : previewBlockedMaterial;
            var center = CellCenter(pos, roadHeight + buildingBaseHeight + 0.16f);
            AddLooseCube(commandResultObjects, ok ? "CommandResultOkPad" : "CommandResultBlockedPad", material, center, new Vector3(cellSize * 0.54f, 0.075f, cellSize * 0.54f));
            AddLooseCube(commandResultObjects, ok ? "CommandResultOkPost" : "CommandResultBlockedPost", material, center + new Vector3(0f, 0.18f, 0f), new Vector3(0.08f, 0.3f, 0.08f));
            AddLooseCube(commandResultObjects, ok ? "CommandResultOkCap" : "CommandResultBlockedCap", material, center + new Vector3(0f, 0.36f, 0f), new Vector3(cellSize * 0.3f, 0.08f, cellSize * 0.3f));
            AddCommandResultToolGlyph(center, material, mode, ok);
            AddCommandResultStatusGlyph(center, material, ok);
        }

        public void ShowLockedRegionTapMarker(GridPos pos)
        {
            // REFERENCE_IMAGE_LOCKED_REGION_TAP_MARKER makes the expansion boundary feel interactive.
            ClearObjects(commandResultObjects);
            commandResultExpiresAt = Time.time + 1.18f;
            var progress = LockedRegionObjectiveProgress01();
            var center = CellCenter(pos, roadHeight + buildingBaseHeight + 0.14f);
            var progressWidth = Mathf.Lerp(cellSize * 0.18f, cellSize * 0.5f, Mathf.Clamp01(progress));
            AddLooseCube(commandResultObjects, "LockedRegionTapPad", lockedAreaMaterial, center, new Vector3(cellSize * 0.62f, 0.07f, cellSize * 0.62f));
            AddLooseCube(commandResultObjects, "LockedRegionTapProgress", roadLineMaterial, center + new Vector3(0f, 0.075f, -cellSize * 0.26f), new Vector3(progressWidth, 0.035f, 0.055f));
            AddLooseCube(commandResultObjects, "LockedRegionTapLockBody", roadLineMaterial, center + new Vector3(0f, 0.26f, 0f), new Vector3(cellSize * 0.28f, 0.22f, cellSize * 0.22f));
            AddLooseCube(commandResultObjects, "LockedRegionTapLockCore", lockedAreaMaterial, center + new Vector3(0f, 0.275f, 0f), new Vector3(cellSize * 0.14f, 0.09f, cellSize * 0.12f));
            AddLooseCube(commandResultObjects, "LockedRegionTapLockShackle", windowMaterial, center + new Vector3(0f, 0.43f, 0f), new Vector3(cellSize * 0.36f, 0.055f, cellSize * 0.09f));
            AddLooseCube(commandResultObjects, "LockedRegionTapUnlockSpark", serviceNeedMaterial, center + new Vector3(cellSize * 0.28f, 0.52f, -cellSize * 0.2f), new Vector3(0.11f, 0.07f, 0.11f));
            AddLockedRegionTapBoundaryBunting(center, progress);
        }

        private void AddLockedRegionTapBoundaryBunting(Vector3 center, float progress)
        {
            var activeMaterial = progress >= 0.5f ? previewOkMaterial : lockedAreaMaterial;
            var front = center + new Vector3(0f, 0.02f, -cellSize * 0.44f);
            var back = center + new Vector3(0f, 0.024f, cellSize * 0.44f);
            AddLooseCube(commandResultObjects, "LockedRegionTapBoundaryString", roadLineMaterial, front, new Vector3(cellSize * 0.58f, 0.024f, 0.032f));
            AddLooseCube(commandResultObjects, "LockedRegionTapBoundaryString", roadLineMaterial, back, new Vector3(cellSize * 0.46f, 0.024f, 0.032f));
            AddLooseCube(commandResultObjects, "LockedRegionTapBuntingFlag", activeMaterial, front + new Vector3(-cellSize * 0.22f, 0.05f, 0f), new Vector3(cellSize * 0.12f, 0.08f, 0.035f));
            AddLooseCube(commandResultObjects, "LockedRegionTapBuntingFlag", serviceNeedMaterial, front + new Vector3(0f, 0.055f, 0f), new Vector3(cellSize * 0.13f, 0.09f, 0.035f));
            AddLooseCube(commandResultObjects, "LockedRegionTapBuntingFlag", activeMaterial, front + new Vector3(cellSize * 0.22f, 0.05f, 0f), new Vector3(cellSize * 0.12f, 0.08f, 0.035f));
            AddLooseCube(commandResultObjects, "LockedRegionTapBoundaryStake", roadLineMaterial, front + new Vector3(-cellSize * 0.34f, 0.08f, 0f), new Vector3(0.035f, 0.18f, 0.035f));
            AddLooseCube(commandResultObjects, "LockedRegionTapBoundaryStake", roadLineMaterial, front + new Vector3(cellSize * 0.34f, 0.08f, 0f), new Vector3(0.035f, 0.18f, 0.035f));
            AddLooseCube(commandResultObjects, "LockedRegionTapObjectivePip", progress >= 0.85f ? previewOkMaterial : windowMaterial, back + new Vector3(-cellSize * 0.16f, 0.05f, 0f), new Vector3(cellSize * 0.1f, 0.05f, 0.04f));
            AddLooseCube(commandResultObjects, "LockedRegionTapObjectivePip", progress >= 0.85f ? previewOkMaterial : lockedAreaMaterial, back + new Vector3(0f, 0.055f, 0f), new Vector3(cellSize * 0.1f, 0.06f, 0.04f));
            AddLooseCube(commandResultObjects, "LockedRegionTapObjectivePip", progress >= 0.85f ? previewOkMaterial : lockedAreaMaterial, back + new Vector3(cellSize * 0.16f, 0.05f, 0f), new Vector3(cellSize * 0.1f, 0.05f, 0.04f));
        }

        public void ShowExpansionUnlockedPulse()
        {
            if (controller == null || controller.Grid == null)
            {
                return;
            }

            ClearObjects(commandResultObjects);
            commandResultExpiresAt = Time.time + 1.55f;

            int startX;
            int startY;
            int endX;
            int endY;
            controller.Grid.LockedExpansionBounds(out startX, out startY, out endX, out endY);
            var center = new Vector3((startX + endX + 1) * 0.5f * cellSize, roadHeight + 0.15f, (startY + endY + 1) * 0.5f * cellSize);
            var width = Mathf.Max(1, endX - startX + 1) * cellSize;
            var depth = Mathf.Max(1, endY - startY + 1) * cellSize;

            AddLooseCube(commandResultObjects, "ExpansionUnlockedCenterPad", previewOkMaterial, center, new Vector3(cellSize * 0.78f, 0.055f, cellSize * 0.78f));
            AddDailySettlementPulseRing(center + new Vector3(0f, 0.02f, 0f), cellSize * 1.12f, previewOkMaterial, "ExpansionUnlockedInnerPulse");
            AddDailySettlementPulseRing(center + new Vector3(0f, 0.05f, 0f), cellSize * 1.78f, roadLineMaterial, "ExpansionUnlockedOuterPulse");
            AddLooseCube(commandResultObjects, "ExpansionUnlockedGateNorth", roadLineMaterial, new Vector3(center.x, roadHeight + 0.23f, (endY + 1f) * cellSize), new Vector3(Mathf.Min(width * 0.42f, cellSize * 2.2f), 0.052f, 0.08f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedGateSouth", roadLineMaterial, new Vector3(center.x, roadHeight + 0.23f, startY * cellSize), new Vector3(Mathf.Min(width * 0.42f, cellSize * 2.2f), 0.052f, 0.08f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedGateEast", roadLineMaterial, new Vector3((endX + 1f) * cellSize, roadHeight + 0.23f, center.z), new Vector3(0.08f, 0.052f, Mathf.Min(depth * 0.42f, cellSize * 2.2f)));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedGateWest", roadLineMaterial, new Vector3(startX * cellSize, roadHeight + 0.23f, center.z), new Vector3(0.08f, 0.052f, Mathf.Min(depth * 0.42f, cellSize * 2.2f)));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedGlowColumn", previewOkMaterial, center + new Vector3(0f, 0.38f, 0f), new Vector3(cellSize * 0.18f, 0.68f, cellSize * 0.18f));
            AddDailySettlementTick(center + new Vector3(0f, 0.9f, 0f));
            AddExpansionUnlockedSparkles(center, startX, startY, endX, endY);
        }

        private void AddExpansionUnlockedSparkles(Vector3 center, int startX, int startY, int endX, int endY)
        {
            AddLooseCube(commandResultObjects, "ExpansionUnlockedSpark", serviceNeedMaterial, center + new Vector3(cellSize * 0.48f, 0.62f, cellSize * 0.1f), new Vector3(0.12f, 0.075f, 0.12f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedSpark", previewOkMaterial, center + new Vector3(-cellSize * 0.42f, 0.7f, cellSize * 0.22f), new Vector3(0.1f, 0.08f, 0.1f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedSpark", roadLineMaterial, center + new Vector3(cellSize * 0.08f, 0.78f, -cellSize * 0.46f), new Vector3(0.11f, 0.08f, 0.11f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedCorner", previewOkMaterial, new Vector3(startX * cellSize, roadHeight + 0.24f, startY * cellSize), new Vector3(0.18f, 0.08f, 0.18f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedCorner", previewOkMaterial, new Vector3((endX + 1f) * cellSize, roadHeight + 0.24f, startY * cellSize), new Vector3(0.18f, 0.08f, 0.18f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedCorner", previewOkMaterial, new Vector3(startX * cellSize, roadHeight + 0.24f, (endY + 1f) * cellSize), new Vector3(0.18f, 0.08f, 0.18f));
            AddLooseCube(commandResultObjects, "ExpansionUnlockedCorner", previewOkMaterial, new Vector3((endX + 1f) * cellSize, roadHeight + 0.24f, (endY + 1f) * cellSize), new Vector3(0.18f, 0.08f, 0.18f));
        }

        private void AddCommandResultStatusGlyph(Vector3 center, Material material, bool ok)
        {
            // CITY_SKYLINES_COMMAND_RESULT_STATUS_MARK lets success and blocked taps read from the city map.
            var top = center + new Vector3(0f, 0.56f, 0f);
            if (ok)
            {
                AddLooseCube(commandResultObjects, "CommandResultOkCheckShort", roadLineMaterial, top + new Vector3(-0.08f, 0f, -0.02f), new Vector3(0.12f, 0.035f, 0.055f));
                AddLooseCube(commandResultObjects, "CommandResultOkCheckLong", roadLineMaterial, top + new Vector3(0.06f, 0.02f, 0.04f), new Vector3(0.22f, 0.035f, 0.055f));
                return;
            }

            AddLooseCube(commandResultObjects, "CommandResultBlockedX", material, top, new Vector3(0.24f, 0.04f, 0.055f));
            AddLooseCube(commandResultObjects, "CommandResultBlockedX", material, top, new Vector3(0.055f, 0.04f, 0.24f));
        }

        private void AddCommandResultToolGlyph(Vector3 center, Material material, CityToolMode mode, bool ok)
        {
            // CITY_SKYLINES_COMMAND_RESULT_GLYPHS distinguish build, zone, road and demolish feedback at a glance.
            var glyphCenter = center + new Vector3(0f, 0.43f, 0f);
            if (mode == CityToolMode.BuildRoad || mode == CityToolMode.UpgradeRoad)
            {
                AddLooseCube(commandResultObjects, "CommandResultRoadGlyph", roadLineMaterial, glyphCenter, new Vector3(cellSize * 0.36f, 0.035f, 0.07f));
                AddLooseCube(commandResultObjects, "CommandResultRoadGlyph", roadLineMaterial, glyphCenter + new Vector3(0f, 0.028f, 0f), new Vector3(0.07f, 0.035f, cellSize * 0.24f));
                return;
            }

            if (mode == CityToolMode.ZonePaint)
            {
                AddLooseCube(commandResultObjects, "CommandResultZoneGlyph", material, glyphCenter + new Vector3(0f, 0f, -0.1f), new Vector3(cellSize * 0.26f, 0.035f, 0.045f));
                AddLooseCube(commandResultObjects, "CommandResultZoneGlyph", material, glyphCenter + new Vector3(-0.1f, 0f, 0f), new Vector3(0.045f, 0.035f, cellSize * 0.26f));
                return;
            }

            if (mode == CityToolMode.Demolish)
            {
                AddLooseCube(commandResultObjects, "CommandResultDemolishGlyph", material, glyphCenter, new Vector3(cellSize * 0.32f, 0.04f, 0.055f));
                AddLooseCube(commandResultObjects, "CommandResultDemolishGlyph", material, glyphCenter, new Vector3(0.055f, 0.04f, cellSize * 0.32f));
                return;
            }

            if (mode == CityToolMode.BuildBuilding)
            {
                var scale = ok ? new Vector3(0.16f, 0.18f, 0.16f) : new Vector3(0.2f, 0.08f, 0.2f);
                AddLooseCube(commandResultObjects, "CommandResultBuildingGlyph", material, glyphCenter + new Vector3(0f, ok ? 0.04f : 0f, 0f), scale);
            }
        }

        private void ShowDailySettlementPulse()
        {
            if (controller == null || controller.Grid == null)
            {
                return;
            }

            ClearObjects(commandResultObjects);
            commandResultExpiresAt = Time.time + 1.05f;

            var focus = DailySettlementFocus();
            var center = CellCenter(focus, roadHeight + 0.12f);
            AddDailySettlementPulseRing(center, cellSize * 1.08f, serviceNeedMaterial, "DailySettlementOuterPulse");
            AddDailySettlementPulseRing(center + new Vector3(0f, 0.035f, 0f), cellSize * 0.72f, previewOkMaterial, "DailySettlementInnerPulse");
            AddLooseCube(commandResultObjects, "DailySettlementGlowColumn", windowMaterial, center + new Vector3(0f, 0.32f, 0f), new Vector3(cellSize * 0.16f, 0.56f, cellSize * 0.16f));
            AddLooseCube(commandResultObjects, "DailySettlementGlowCap", roadLineMaterial, center + new Vector3(0f, 0.64f, 0f), new Vector3(cellSize * 0.34f, 0.055f, cellSize * 0.34f));
            AddDailySettlementTick(center + new Vector3(0f, 0.82f, 0f));
            AddDailySettlementSparkles(center);
        }

        private void ShowCityGrowthPulse(int addedCount)
        {
            if (controller == null || controller.Grid == null)
            {
                return;
            }

            ClearObjects(commandResultObjects);
            commandResultExpiresAt = Time.time + 1.15f;

            var focus = CityGrowthPulseFocus();
            var center = CellCenter(focus, roadHeight + 0.13f);
            var scaleBoost = Mathf.Clamp(addedCount, 1, 4) * 0.08f;
            AddDailySettlementPulseRing(center, cellSize * (0.72f + scaleBoost), previewOkMaterial, "CityGrowthInnerPulse");
            AddDailySettlementPulseRing(center + new Vector3(0f, 0.035f, 0f), cellSize * (1.05f + scaleBoost), serviceNeedMaterial, "CityGrowthOuterPulse");
            AddLooseCube(commandResultObjects, "CityGrowthPermitPad", previewOkMaterial, center, new Vector3(cellSize * 0.5f, 0.05f, cellSize * 0.5f));
            AddLooseCube(commandResultObjects, "CityGrowthPermitPost", roadLineMaterial, center + new Vector3(-cellSize * 0.2f, 0.25f, -cellSize * 0.18f), new Vector3(0.055f, 0.36f, 0.055f));
            AddLooseCube(commandResultObjects, "CityGrowthPermitFlag", windowMaterial, center + new Vector3(-cellSize * 0.08f, 0.42f, -cellSize * 0.18f), new Vector3(cellSize * 0.26f, 0.09f, 0.04f));
            AddLooseCube(commandResultObjects, "CityGrowthGoldSpark", serviceNeedMaterial, center + new Vector3(cellSize * 0.34f, 0.46f, cellSize * 0.08f), new Vector3(0.11f, 0.08f, 0.11f));
            AddLooseCube(commandResultObjects, "CityGrowthBlueSpark", windowMaterial, center + new Vector3(-cellSize * 0.22f, 0.56f, cellSize * 0.26f), new Vector3(0.09f, 0.075f, 0.09f));
            AddLooseCube(commandResultObjects, "CityGrowthGreenSpark", previewOkMaterial, center + new Vector3(cellSize * 0.1f, 0.64f, -cellSize * 0.36f), new Vector3(0.1f, 0.08f, 0.1f));
        }

        private GridPos CityGrowthPulseFocus()
        {
            var buildings = controller.Buildings;
            if (buildings == null || buildings.Count == 0)
            {
                return DailySettlementFocus();
            }

            var grid = controller.Grid;
            var centerX = (grid.Width - 1) * 0.5f;
            var centerY = (grid.Height - 1) * 0.5f;
            var best = buildings[0].Pos;
            var bestScore = int.MinValue;
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var building = buildings[i];
                var distance = Mathf.RoundToInt(Mathf.Abs(building.Pos.X - centerX) + Mathf.Abs(building.Pos.Y - centerY));
                var score = 900 - building.AgeDays * 140 - distance * 6;
                if (building.AutoDeveloped)
                {
                    score += 280;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = building.Pos;
                }
            }

            return best;
        }

        private GridPos DailySettlementFocus()
        {
            var grid = controller.Grid;
            var centerX = (grid.Width - 1) * 0.5f;
            var centerY = (grid.Height - 1) * 0.5f;
            var fallback = new GridPos(Mathf.Clamp(Mathf.RoundToInt(centerX), 0, grid.Width - 1), Mathf.Clamp(Mathf.RoundToInt(centerY), 0, grid.Height - 1));
            var best = fallback;
            var bestScore = int.MinValue;

            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (tile == null || tile.Terrain == TerrainType.Water)
                    {
                        continue;
                    }

                    var distance = Mathf.RoundToInt(Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY));
                    var score = 1000 - distance * 12;
                    if (tile.Zone == ZoneType.Civic)
                    {
                        score += 620;
                    }

                    if (!string.IsNullOrEmpty(tile.BuildingId))
                    {
                        score += 120;
                    }

                    if (!string.IsNullOrEmpty(tile.RoadId))
                    {
                        score += 60;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new GridPos(x, y);
                    }
                }
            }

            return best;
        }

        private void AddDailySettlementPulseRing(Vector3 center, float radius, Material material, string name)
        {
            var segmentLength = Mathf.Max(cellSize * 0.26f, radius * 0.42f);
            var segmentThickness = Mathf.Max(0.045f, cellSize * 0.055f);
            var segmentHeight = 0.026f;
            AddLooseCube(commandResultObjects, name + "North", material, center + new Vector3(0f, 0f, radius), new Vector3(segmentLength, segmentHeight, segmentThickness));
            AddLooseCube(commandResultObjects, name + "South", material, center + new Vector3(0f, 0f, -radius), new Vector3(segmentLength, segmentHeight, segmentThickness));
            AddLooseCube(commandResultObjects, name + "East", material, center + new Vector3(radius, 0f, 0f), new Vector3(segmentThickness, segmentHeight, segmentLength));
            AddLooseCube(commandResultObjects, name + "West", material, center + new Vector3(-radius, 0f, 0f), new Vector3(segmentThickness, segmentHeight, segmentLength));

            var diagonalOffset = radius * 0.68f;
            var diagonalScale = new Vector3(segmentLength * 0.74f, segmentHeight, segmentThickness);
            AddLooseCubeRotated(commandResultObjects, name + "NorthEast", material, center + new Vector3(diagonalOffset, 0.008f, diagonalOffset), diagonalScale, -45f);
            AddLooseCubeRotated(commandResultObjects, name + "NorthWest", material, center + new Vector3(-diagonalOffset, 0.008f, diagonalOffset), diagonalScale, 45f);
            AddLooseCubeRotated(commandResultObjects, name + "SouthEast", material, center + new Vector3(diagonalOffset, 0.008f, -diagonalOffset), diagonalScale, 45f);
            AddLooseCubeRotated(commandResultObjects, name + "SouthWest", material, center + new Vector3(-diagonalOffset, 0.008f, -diagonalOffset), diagonalScale, -45f);
        }

        private void AddDailySettlementTick(Vector3 center)
        {
            AddLooseCubeRotated(commandResultObjects, "DailySettlementTickShort", previewOkMaterial, center + new Vector3(-cellSize * 0.11f, 0f, -cellSize * 0.03f), new Vector3(cellSize * 0.3f, 0.055f, 0.075f), 45f);
            AddLooseCubeRotated(commandResultObjects, "DailySettlementTickLong", roadLineMaterial, center + new Vector3(cellSize * 0.1f, 0.045f, cellSize * 0.03f), new Vector3(cellSize * 0.52f, 0.06f, 0.08f), -32f);
        }

        private void AddDailySettlementSparkles(Vector3 center)
        {
            var high = center + new Vector3(0f, 0.55f, 0f);
            AddLooseCube(commandResultObjects, "DailySettlementGoldSpark", roadLineMaterial, high + new Vector3(cellSize * 0.46f, 0f, cellSize * 0.08f), new Vector3(0.11f, 0.07f, 0.11f));
            AddLooseCube(commandResultObjects, "DailySettlementGreenSpark", previewOkMaterial, high + new Vector3(-cellSize * 0.38f, 0.08f, cellSize * 0.18f), new Vector3(0.09f, 0.08f, 0.09f));
            AddLooseCube(commandResultObjects, "DailySettlementGoldSpark", serviceNeedMaterial, high + new Vector3(cellSize * 0.08f, 0.16f, -cellSize * 0.44f), new Vector3(0.1f, 0.075f, 0.1f));
        }

        private void RebuildTerrain()
        {
            BuildTileMesh(terrainMesh, TerrainColorForTile, true, true);
        }

        private void RebuildOverlay()
        {
            BuildTileMesh(overlayMesh, ReadableOverlayColorForTile, true, false);
        }

        private void BuildTileMesh(Mesh mesh, System.Func<int, int, Color32> colorForTile, bool facetedTerrain, bool sculptTerrain)
        {
            var grid = controller.Grid;
            var vertices = new List<Vector3>(grid.Width * grid.Height * 4);
            var triangles = new List<int>(grid.Width * grid.Height * 6);
            var colors = new List<Color32>(grid.Width * grid.Height * 4);

            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var index = vertices.Count;
                    var x0 = x * cellSize;
                    var z0 = y * cellSize;
                    var y0 = sculptTerrain ? TerrainVisualHeightForTile(x, y, 0) : 0f;
                    var y1 = sculptTerrain ? TerrainVisualHeightForTile(x, y, 1) : 0f;
                    var y2 = sculptTerrain ? TerrainVisualHeightForTile(x, y, 2) : 0f;
                    var y3 = sculptTerrain ? TerrainVisualHeightForTile(x, y, 3) : 0f;
                    vertices.Add(new Vector3(x0, y0, z0));
                    vertices.Add(new Vector3(x0 + cellSize, y1, z0));
                    vertices.Add(new Vector3(x0, y2, z0 + cellSize));
                    vertices.Add(new Vector3(x0 + cellSize, y3, z0 + cellSize));
                    triangles.Add(index);
                    triangles.Add(index + 2);
                    triangles.Add(index + 1);
                    triangles.Add(index + 1);
                    triangles.Add(index + 2);
                    triangles.Add(index + 3);

                    var color = colorForTile(x, y);
                    if (facetedTerrain)
                    {
                        // LOW_POLY_TERRAIN_SHADE_PATCHES gives flat tiles a gentle faceted read.
                        colors.Add(FacetedTileColor(color, x, y, 0));
                        colors.Add(FacetedTileColor(color, x, y, 1));
                        colors.Add(FacetedTileColor(color, x, y, 2));
                        colors.Add(FacetedTileColor(color, x, y, 3));
                    }
                    else
                    {
                        colors.Add(color);
                        colors.Add(color);
                        colors.Add(color);
                        colors.Add(color);
                    }
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            if (sculptTerrain)
            {
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
        }

        private float TerrainVisualHeightForTile(int x, int y, int corner)
        {
            // LOW_POLY_TERRAIN_HEIGHT_LAYERS make the river sit low and hills pop without affecting simulation.
            var tile = controller.GetTile(x, y);
            if (tile == null)
            {
                return 0f;
            }

            if (tile.Terrain == TerrainType.Water)
            {
                return -0.018f + TerrainCornerFacetJitter(x, y, corner, 0.002f);
            }

            if (tile.Terrain == TerrainType.Hill)
            {
                return 0.03f + TerrainCornerFacetJitter(x, y, corner, 0.006f);
            }

            if (!string.IsNullOrEmpty(tile.RoadId) || !string.IsNullOrEmpty(tile.BuildingId))
            {
                return 0.002f;
            }

            if (tile.Terrain == TerrainType.Plain && IsShorelineSceneryTile(x, y))
            {
                return 0.014f + GrassCheckerHeightOffset(x, y) * 0.45f + TerrainCornerFacetJitter(x, y, corner, 0.005f);
            }

            if (tile.Terrain == TerrainType.Plain)
            {
                return GrassCheckerHeightOffset(x, y) + TerrainCornerFacetJitter(x, y, corner, 0.004f);
            }

            return TerrainCornerFacetJitter(x, y, corner, 0.004f);
        }

        private static float TerrainCornerFacetJitter(int x, int y, int corner, float amplitude)
        {
            var hash = x * 73 + y * 41 + corner * 17;
            var value = ((hash % 7) - 3) / 3f;
            return value * amplitude;
        }

        private static float GrassCheckerHeightOffset(int x, int y)
        {
            var checker = ((x + y) & 1) == 0 ? 0.007f : 0.001f;
            var microStep = ((x * 11 + y * 7) % 5) * 0.0008f;
            return checker + microStep;
        }

        private void RebuildRoads()
        {
            ClearObjects(roadObjects);
            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            for (var i = 0; i < roads.Count; i += 1)
            {
                var road = roads[i];
                var obj = CreateCube("Road", roadMaterial);
                obj.transform.SetParent(transform, false);
                var tierHeight = road.Tier == RoadTier.Arterial ? roadHeight * 1.35f : roadHeight;
                var width = road.Tier == RoadTier.Arterial ? cellSize * 1.08f : cellSize * 0.95f;
                obj.transform.localPosition = CellCenter(road.Pos, tierHeight * 0.5f);
                obj.transform.localScale = new Vector3(width, tierHeight, width);
                roadObjects.Add(obj);

                var hasLeft = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y);
                var hasRight = HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
                var hasDown = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1);
                var hasUp = HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
                var hasHorizontal = hasLeft || hasRight;
                var hasVertical = hasDown || hasUp;
                var lineWidth = road.Tier == RoadTier.Arterial ? 0.07f : 0.05f;
                if (hasHorizontal || !hasVertical)
                {
                    AddRoadCenterMark(road.Pos, width * 0.68f, lineWidth, tierHeight);
                }

                if (hasVertical)
                {
                    AddRoadCenterMark(road.Pos, lineWidth, width * 0.68f, tierHeight);
                }

                AddRoadLaneDashes(road.Pos, hasHorizontal, hasVertical, tierHeight, road.Tier, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp));
                AddRoadNodeReadabilityCue(road, hasLeft, hasRight, hasDown, hasUp, hasHorizontal, hasVertical, tierHeight);
                AddRoadNodeTurnArrowCues(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                AddRoadNodeMicroStuds(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                AddRoadCongestionMicroPulse(road, hasHorizontal, hasVertical, tierHeight);
                AddRoadTrafficReadoutBadge(road, hasHorizontal, hasVertical, tierHeight);

                if (road.Tier == RoadTier.Arterial)
                {
                    AddArterialLaneEdges(road.Pos, hasHorizontal, hasVertical, width, tierHeight);
                }

                AddRoadCurbEdges(road.Pos, hasLeft, hasRight, hasDown, hasUp, width, tierHeight);
                AddRoadParcelAccessCues(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp));
                AddCentralBoulevardCues(road, hasLeft, hasRight, hasDown, hasUp, hasHorizontal, hasVertical, tierHeight);
                AddRoadFlowChevrons(road, hasHorizontal, hasVertical, tierHeight);
                AddRoadDirectionArrowCue(road, hasLeft, hasRight, hasDown, hasUp, hasHorizontal, hasVertical, tierHeight);
                AddRoadRoutePointCues(road, hasHorizontal, hasVertical, tierHeight);
                AddRoadTrafficCars(road, hasHorizontal, hasVertical, tierHeight);
                AddFreshRoadPaintDetails(road, hasHorizontal, hasVertical, tierHeight, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp));
                AddRoadsideMicroDecor(road, hasHorizontal, hasVertical, tierHeight, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp));
                AddRoadsideLifeOrderCues(road, hasHorizontal, hasVertical, tierHeight, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp));
                AddRoadJunctionWayfindingSigns(road, hasLeft, hasRight, hasDown, hasUp, hasHorizontal, hasVertical, tierHeight, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp));
                AddRoadNetworkEdgePlanningMarker(road, hasLeft, hasRight, hasDown, hasUp, tierHeight);

                if (RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp) >= 3)
                {
                    AddRoadIntersectionPavers(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                    AddRoadIntersectionCrosswalks(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                    AddRoadIntersectionSignals(road.Pos, tierHeight);
                    AddRoadIntersectionGardenIslands(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                    AddIntersectionCivicLife(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                }
                else if (RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp) == 2 && hasHorizontal && hasVertical)
                {
                    AddRoadCornerPocketPaver(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                    AddRoadCornerPlantingCue(road.Pos, hasLeft, hasRight, hasDown, hasUp, tierHeight);
                }
            }
        }

        private void AddRoadsideLifeOrderCues(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop, int connections)
        {
            var hash = DecorationHash(road.Pos.X + 13, road.Pos.Y + 17);
            if (connections >= 4 || hash % 3 == 1)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            var along = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var side = ((hash >> 2) & 1) == 0 ? -1f : 1f;
            var baseCenter = CellCenter(road.Pos, roadTop)
                + normal * side * cellSize * 0.49f
                + along * ((((hash >> 4) & 3) - 1.5f) * cellSize * 0.08f)
                + new Vector3(0f, 0.045f, 0f);

            if (hash % 4 == 0)
            {
                AddRoadsideMarketCue(baseCenter, horizontal, along, normal * side);
                return;
            }

            if (hash % 4 == 1)
            {
                AddRoadsideDeliveryCue(baseCenter, horizontal, along, normal * side);
                return;
            }

            AddRoadsideResidentCue(baseCenter, horizontal, along, normal * side, hash);
        }

        private void AddRoadsideMarketCue(Vector3 baseCenter, bool horizontal, Vector3 along, Vector3 side)
        {
            var stallScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.11f, cellSize * 0.13f)
                : new Vector3(cellSize * 0.13f, 0.11f, cellSize * 0.22f);
            var awningScale = horizontal
                ? new Vector3(cellSize * 0.26f, 0.045f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.045f, cellSize * 0.26f);
            AddLooseCube(roadObjects, "RoadsideMarketPad", grassGridMaterial, baseCenter + new Vector3(0f, -0.035f, 0f), awningScale);
            AddLooseCube(roadObjects, "RoadsideMarketStall", serviceMaterial, baseCenter + new Vector3(0f, 0.06f, 0f), stallScale);
            AddLooseCube(roadObjects, "RoadsideMarketAwning", serviceNeedMaterial, baseCenter + new Vector3(0f, 0.155f, 0f), awningScale);
            AddLooseCube(roadObjects, "RoadsideMarketOrderTag", roadLineMaterial, baseCenter + side * cellSize * 0.11f + new Vector3(0f, 0.2f, 0f), horizontal ? new Vector3(cellSize * 0.12f, 0.04f, 0.03f) : new Vector3(0.03f, 0.04f, cellSize * 0.12f));
            AddLooseCube(roadObjects, "RoadsideMarketCrate", commercialMaterial, baseCenter - along * cellSize * 0.16f + new Vector3(0f, 0.025f, 0f), new Vector3(cellSize * 0.08f, 0.06f, cellSize * 0.08f));
        }

        private void AddRoadsideDeliveryCue(Vector3 baseCenter, bool horizontal, Vector3 along, Vector3 side)
        {
            var bayScale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.026f, cellSize * 0.08f)
                : new Vector3(cellSize * 0.08f, 0.026f, cellSize * 0.3f);
            AddLooseCube(roadObjects, "RoadsideDeliveryBay", roadLineMaterial, baseCenter + new Vector3(0f, -0.012f, 0f), bayScale);
            AddLooseCube(roadObjects, "RoadsideDeliveryParcel", serviceNeedMaterial, baseCenter + along * cellSize * 0.09f + new Vector3(0f, 0.055f, 0f), new Vector3(cellSize * 0.09f, 0.09f, cellSize * 0.09f));
            AddLooseCube(roadObjects, "RoadsideDeliveryParcelLid", windowMaterial, baseCenter + along * cellSize * 0.09f + new Vector3(0f, 0.115f, 0f), new Vector3(cellSize * 0.1f, 0.02f, cellSize * 0.1f));
            AddLooseCube(roadObjects, "RoadsideDeliveryBikeBody", commercialMaterial, baseCenter - along * cellSize * 0.13f + side * cellSize * 0.045f + new Vector3(0f, 0.055f, 0f), horizontal ? new Vector3(cellSize * 0.16f, 0.055f, cellSize * 0.055f) : new Vector3(cellSize * 0.055f, 0.055f, cellSize * 0.16f));
            AddLooseCube(roadObjects, "RoadsideDeliveryBikeWheel", roadMaterial, baseCenter - along * cellSize * 0.2f + side * cellSize * 0.045f + new Vector3(0f, 0.026f, 0f), new Vector3(0.05f, 0.05f, 0.05f));
        }

        private void AddRoadsideResidentCue(Vector3 baseCenter, bool horizontal, Vector3 along, Vector3 side, int seed)
        {
            var dotMaterial = (seed & 2) == 0 ? mixedUseMaterial : roofMaterial;
            AddLooseCube(roadObjects, "RoadsideResidentShadow", buildingFootprintMaterial, baseCenter + new Vector3(0.025f, -0.032f, 0.025f), new Vector3(cellSize * 0.18f, 0.014f, cellSize * 0.14f));
            AddLooseCube(roadObjects, "RoadsideResidentBody", dotMaterial, baseCenter + new Vector3(0f, 0.075f, 0f), new Vector3(0.055f, 0.13f, 0.055f));
            AddLooseCube(roadObjects, "RoadsideResidentHead", windowMaterial, baseCenter + new Vector3(0f, 0.17f, 0f), new Vector3(0.07f, 0.06f, 0.07f));
            AddLooseCube(roadObjects, "RoadsideResidentThoughtPip", serviceNeedMaterial, baseCenter + side * cellSize * 0.12f + along * cellSize * 0.08f + new Vector3(0f, 0.24f, 0f), new Vector3(0.06f, 0.04f, 0.06f));
            AddLooseCube(roadObjects, "RoadsideResidentThoughtPip", roadLineMaterial, baseCenter + side * cellSize * 0.17f + along * cellSize * 0.13f + new Vector3(0f, 0.29f, 0f), new Vector3(0.04f, 0.032f, 0.04f));
        }

        private void AddRoadNodeReadabilityCue(RoadNode road, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINES_ROAD_NODE_READABILITY gives intersections and termini crisp map-node cues.
            var connections = RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp);
            if (connections >= 3)
            {
                var center = CellCenter(road.Pos, roadTop + 0.05f);
                AddLooseCube(roadObjects, "RoadNodeControlPlate", roadLineMaterial, center, new Vector3(cellSize * 0.2f, 0.026f, cellSize * 0.2f));
                AddLooseCube(roadObjects, "RoadNodeControlCore", windowMaterial, center + new Vector3(0f, 0.028f, 0f), new Vector3(cellSize * 0.11f, 0.03f, cellSize * 0.11f));
                AddRoadNodeApproachTick(road.Pos, hasLeft, -1f, 0f, roadTop);
                AddRoadNodeApproachTick(road.Pos, hasRight, 1f, 0f, roadTop);
                AddRoadNodeApproachTick(road.Pos, hasDown, 0f, -1f, roadTop);
                AddRoadNodeApproachTick(road.Pos, hasUp, 0f, 1f, roadTop);
                return;
            }

            if (connections <= 1)
            {
                AddRoadTerminalNodeCue(road.Pos, hasLeft, hasRight, hasDown, hasUp, roadTop);
                return;
            }

            if (road.Tier == RoadTier.Arterial)
            {
                var vertical = hasVertical && !hasHorizontal;
                var center = CellCenter(road.Pos, roadTop + 0.044f);
                AddLooseCube(roadObjects, "ArterialNodeGuidePlate", roadLineMaterial, center, vertical ? new Vector3(0.08f, 0.022f, cellSize * 0.36f) : new Vector3(cellSize * 0.36f, 0.022f, 0.08f));
                AddLooseCube(roadObjects, "ArterialNodeGuideDot", windowMaterial, center + new Vector3(0f, 0.026f, 0f), new Vector3(0.095f, 0.03f, 0.095f));
            }
        }

        private void AddRoadNodeTurnArrowCues(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // CITY_SKYLINES_NODE_TURN_ARROWS make intersection intent readable in the base map.
            var connections = RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp);
            if (connections < 3)
            {
                return;
            }

            var hash = DecorationHash(pos.X, pos.Y);
            var side = (hash & 1) == 0 ? -1f : 1f;
            if (hasLeft) AddRoadNodeTurnArrow(pos, -1f, 0f, side, roadTop);
            if (hasRight) AddRoadNodeTurnArrow(pos, 1f, 0f, -side, roadTop);
            if (hasDown) AddRoadNodeTurnArrow(pos, 0f, -1f, -side, roadTop);
            if (hasUp) AddRoadNodeTurnArrow(pos, 0f, 1f, side, roadTop);

            if (connections >= 4)
            {
                AddLooseCubeRotated(roadObjects, "RoadNodeTransferDiamond", windowMaterial, CellCenter(pos, roadTop + 0.132f), new Vector3(cellSize * 0.16f, 0.016f, cellSize * 0.16f), 45f);
            }
        }

        private void AddRoadNodeTurnArrow(GridPos pos, float xDir, float zDir, float side, float roadTop)
        {
            var horizontal = Mathf.Abs(xDir) > 0.01f;
            var direction = new Vector3(xDir, 0f, zDir);
            var sideOffset = horizontal
                ? new Vector3(0f, 0f, side * cellSize * 0.095f)
                : new Vector3(side * cellSize * 0.095f, 0f, 0f);
            var stemCenter = CellCenter(pos, roadTop + 0.118f) + direction * cellSize * 0.23f + sideOffset;
            var headCenter = stemCenter + direction * cellSize * 0.07f;
            var stemScale = horizontal
                ? new Vector3(cellSize * 0.13f, 0.014f, 0.026f)
                : new Vector3(0.026f, 0.014f, cellSize * 0.13f);
            AddLooseCube(roadObjects, "RoadNodeTurnArrowStem", windowMaterial, stemCenter, stemScale);

            var headScale = new Vector3(cellSize * 0.085f, 0.014f, 0.024f);
            if (horizontal)
            {
                var yawA = xDir > 0f ? 35f : 145f;
                var yawB = xDir > 0f ? -35f : -145f;
                AddLooseCubeRotated(roadObjects, "RoadNodeTurnArrowHead", roadLineMaterial, headCenter + new Vector3(-xDir * cellSize * 0.024f, 0f, cellSize * 0.026f), headScale, yawA);
                AddLooseCubeRotated(roadObjects, "RoadNodeTurnArrowHead", roadLineMaterial, headCenter + new Vector3(-xDir * cellSize * 0.024f, 0f, -cellSize * 0.026f), headScale, yawB);
                return;
            }

            var verticalYawA = zDir > 0f ? 55f : -55f;
            var verticalYawB = zDir > 0f ? 125f : -125f;
            AddLooseCubeRotated(roadObjects, "RoadNodeTurnArrowHead", roadLineMaterial, headCenter + new Vector3(cellSize * 0.026f, 0f, -zDir * cellSize * 0.024f), headScale, verticalYawA);
            AddLooseCubeRotated(roadObjects, "RoadNodeTurnArrowHead", roadLineMaterial, headCenter + new Vector3(-cellSize * 0.026f, 0f, -zDir * cellSize * 0.024f), headScale, verticalYawB);
        }

        private void AddRoadNodeMicroStuds(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // LOW_POLY_ROAD_NODE_STUDS add crisp raised dots to busy junctions and corner turns.
            var connections = RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp);
            if (connections < 2)
            {
                return;
            }

            var material = connections >= 3 ? windowMaterial : roadLineMaterial;
            var stud = cellSize * 0.055f;
            var offset = cellSize * 0.29f;
            if (connections >= 3)
            {
                AddRoadDetailMark("RoadNodeMicroStud", material, pos, stud, stud, -offset, -offset, roadTop + 0.052f);
                AddRoadDetailMark("RoadNodeMicroStud", material, pos, stud, stud, offset, -offset, roadTop + 0.052f);
                AddRoadDetailMark("RoadNodeMicroStud", material, pos, stud, stud, -offset, offset, roadTop + 0.052f);
                AddRoadDetailMark("RoadNodeMicroStud", material, pos, stud, stud, offset, offset, roadTop + 0.052f);
                return;
            }

            if ((hasLeft || hasRight) && (hasDown || hasUp))
            {
                var xSign = hasLeft ? -1f : 1f;
                var zSign = hasDown ? -1f : 1f;
                AddRoadDetailMark("RoadCornerMicroStud", serviceNeedMaterial, pos, stud, stud, xSign * offset, zSign * offset, roadTop + 0.046f);
                AddRoadDetailMark("RoadCornerTurnPlate", roadLineMaterial, pos, cellSize * 0.15f, cellSize * 0.04f, xSign * cellSize * 0.16f, zSign * cellSize * 0.24f, roadTop + 0.042f);
            }
        }

        private void AddRoadNodeApproachTick(GridPos pos, bool active, float xSign, float zSign, float roadTop)
        {
            if (!active)
            {
                return;
            }

            var horizontal = Mathf.Abs(xSign) > 0.01f;
            var center = CellCenter(pos, roadTop + 0.09f) + new Vector3(xSign * cellSize * 0.25f, 0f, zSign * cellSize * 0.25f);
            var scale = horizontal ? new Vector3(0.05f, 0.055f, 0.11f) : new Vector3(0.11f, 0.055f, 0.05f);
            AddLooseCube(roadObjects, "RoadNodeApproachTick", serviceNeedMaterial, center, scale);
        }

        private void AddRoadTerminalNodeCue(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            var openX = hasLeft && !hasRight ? 1f : (hasRight && !hasLeft ? -1f : 0f);
            var openZ = hasDown && !hasUp ? 1f : (hasUp && !hasDown ? -1f : 0f);
            if (openX == 0f && openZ == 0f)
            {
                openZ = -1f;
            }

            var center = CellCenter(pos, roadTop + 0.086f) + new Vector3(openX * cellSize * 0.33f, 0f, openZ * cellSize * 0.33f);
            var horizontal = Mathf.Abs(openX) > 0.01f;
            AddLooseCube(roadObjects, "RoadTerminalNodeBollard", serviceNeedMaterial, center, new Vector3(0.08f, 0.13f, 0.08f));
            AddLooseCube(roadObjects, "RoadTerminalNodeReflector", roadLineMaterial, center + new Vector3(0f, 0.09f, 0f), horizontal ? new Vector3(0.035f, 0.035f, 0.13f) : new Vector3(0.13f, 0.035f, 0.035f));
        }

        private void AddRoadNetworkEdgePlanningMarker(RoadNode road, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // CITY_SKYLINES_ROAD_EDGE_SERVICE_MARKERS make road termini and edge parcels feel actively managed.
            var connectionCount = RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp);
            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            if (connectionCount >= 3 || (connectionCount == 2 && hash % 3 != 0 && !IsCentralRoadTile(road.Pos)))
            {
                return;
            }

            Vector3 direction;
            GridPos openPos;
            if (!TryRoadEdgePlanningDirection(road.Pos, hasLeft, hasRight, hasDown, hasUp, hash, out direction, out openPos))
            {
                return;
            }

            var tile = controller.GetTile(openPos.X, openPos.Y);
            var load = TrafficLoadPercent(road);
            var serviceGap = tile != null ? Mathf.Max(0, 42 - ServiceAccessValue(tile)) : 0;
            var material = load >= 72 ? trafficPulseMaterial : (serviceGap >= 18 ? serviceNeedMaterial : previewOkMaterial);
            var curbRunsHorizontal = Mathf.Abs(direction.z) > 0.01f;
            var along = curbRunsHorizontal ? Vector3.right : Vector3.forward;
            var center = CellCenter(road.Pos, roadTop + 0.05f) + direction * cellSize * 0.48f;
            var padScale = curbRunsHorizontal
                ? new Vector3(cellSize * 0.42f, 0.026f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.026f, cellSize * 0.42f);
            var stripeScale = curbRunsHorizontal
                ? new Vector3(cellSize * 0.3f, 0.018f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.018f, cellSize * 0.3f);
            var postOffset = along * cellSize * (((hash & 1) == 0) ? 0.18f : -0.18f);

            AddLooseCube(roadObjects, "RoadEdgePlanningMarkerPad", shoreMaterial != null ? shoreMaterial : roadLineMaterial, center, padScale);
            AddLooseCube(roadObjects, "RoadEdgePlanningStatusStripe", material, center + new Vector3(0f, 0.032f, 0f), stripeScale);
            AddLooseCube(roadObjects, "RoadEdgePlanningSurveyPost", serviceMaterial, center + postOffset + new Vector3(0f, 0.11f, 0f), new Vector3(0.035f, 0.22f, 0.035f));
            AddLooseCube(roadObjects, "RoadEdgePlanningSurveyHead", material, center + postOffset + new Vector3(0f, 0.235f, 0f), new Vector3(cellSize * 0.12f, 0.045f, cellSize * 0.08f));
            AddRoadEdgePlanningPips(center - postOffset * 0.55f + direction * cellSize * 0.04f, along, load, serviceGap, material);
        }

        private bool TryRoadEdgePlanningDirection(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, int hash, out Vector3 direction, out GridPos openPos)
        {
            direction = Vector3.zero;
            openPos = pos;

            if (hasLeft && !hasRight && IsRoadEdgePlanningLot(pos.X + 1, pos.Y))
            {
                direction = Vector3.right;
                openPos = new GridPos(pos.X + 1, pos.Y);
                return true;
            }

            if (hasRight && !hasLeft && IsRoadEdgePlanningLot(pos.X - 1, pos.Y))
            {
                direction = Vector3.left;
                openPos = new GridPos(pos.X - 1, pos.Y);
                return true;
            }

            if (hasDown && !hasUp && IsRoadEdgePlanningLot(pos.X, pos.Y + 1))
            {
                direction = Vector3.forward;
                openPos = new GridPos(pos.X, pos.Y + 1);
                return true;
            }

            if (hasUp && !hasDown && IsRoadEdgePlanningLot(pos.X, pos.Y - 1))
            {
                direction = Vector3.back;
                openPos = new GridPos(pos.X, pos.Y - 1);
                return true;
            }

            var start = Mathf.Abs(hash) % 4;
            for (var i = 0; i < 4; i += 1)
            {
                if (TryRoadEdgePlanningDirectionByIndex(pos, hasLeft, hasRight, hasDown, hasUp, (start + i) % 4, out direction, out openPos))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryRoadEdgePlanningDirectionByIndex(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, int index, out Vector3 direction, out GridPos openPos)
        {
            direction = Vector3.zero;
            openPos = pos;
            if (index == 0 && !hasDown && IsRoadEdgePlanningLot(pos.X, pos.Y - 1))
            {
                direction = Vector3.back;
                openPos = new GridPos(pos.X, pos.Y - 1);
                return true;
            }

            if (index == 1 && !hasUp && IsRoadEdgePlanningLot(pos.X, pos.Y + 1))
            {
                direction = Vector3.forward;
                openPos = new GridPos(pos.X, pos.Y + 1);
                return true;
            }

            if (index == 2 && !hasLeft && IsRoadEdgePlanningLot(pos.X - 1, pos.Y))
            {
                direction = Vector3.left;
                openPos = new GridPos(pos.X - 1, pos.Y);
                return true;
            }

            if (index == 3 && !hasRight && IsRoadEdgePlanningLot(pos.X + 1, pos.Y))
            {
                direction = Vector3.right;
                openPos = new GridPos(pos.X + 1, pos.Y);
                return true;
            }

            return false;
        }

        private bool IsRoadEdgePlanningLot(int x, int y)
        {
            var tile = controller.GetTile(x, y);
            return tile != null
                && tile.Terrain != TerrainType.Water
                && string.IsNullOrEmpty(tile.RoadId)
                && string.IsNullOrEmpty(tile.BuildingId);
        }

        private void AddRoadEdgePlanningPips(Vector3 center, Vector3 along, int load, int serviceGap, Material material)
        {
            var count = load >= 78 ? 3 : (serviceGap >= 22 ? 2 : 1);
            for (var i = 0; i < count; i += 1)
            {
                var pipMaterial = i == count - 1 ? material : roadLineMaterial;
                AddLooseCube(roadObjects, "RoadEdgePlanningServicePip", pipMaterial, center + along * ((i - (count - 1) * 0.5f) * cellSize * 0.065f) + new Vector3(0f, 0.06f + i * 0.01f, 0f), new Vector3(0.045f, 0.045f + i * 0.01f, 0.045f));
            }
        }

        private void AddRoadCongestionMicroPulse(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            var loadPercent = TrafficLoadPercent(road);
            if (loadPercent < 72)
            {
                return;
            }

            var hot = loadPercent >= 88;
            var vertical = hasVertical && !hasHorizontal;
            var center = CellCenter(road.Pos, roadTop + 0.112f);
            var material = hot ? trafficPulseMaterial : serviceNeedMaterial;
            var length = Mathf.Lerp(cellSize * 0.28f, cellSize * 0.44f, Mathf.Clamp01((loadPercent - 72) / 40f));
            var width = hot ? 0.05f : 0.038f;
            var lineScale = vertical ? new Vector3(width, 0.018f, length) : new Vector3(length, 0.018f, width);
            var sideScale = vertical ? new Vector3(length * 0.52f, 0.018f, width) : new Vector3(width, 0.018f, length * 0.52f);
            var sideOffset = vertical ? Vector3.right * cellSize * 0.2f : Vector3.forward * cellSize * 0.2f;
            AddLooseCube(roadObjects, "RoadCongestionMicroPulse", material, center, lineScale);
            AddLooseCube(roadObjects, "RoadCongestionMicroPulseEdge", windowMaterial, center + sideOffset, sideScale);
            AddLooseCube(roadObjects, "RoadCongestionMicroPulseEdge", windowMaterial, center - sideOffset, sideScale);
        }

        private void AddRoadTrafficReadoutBadge(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINES_TRAFFIC_READOUT_BADGE adds a tiny green/yellow/red info-view load read at busy road nodes.
            var loadPercent = TrafficLoadPercent(road);
            var junctionReadout = road.NeighborCount >= 3 && loadPercent >= 48;
            if (loadPercent < 62 && road.Tier != RoadTier.Arterial && !junctionReadout)
            {
                return;
            }

            if (loadPercent < 44)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            var along = horizontal ? Vector3.right : Vector3.forward;
            var side = horizontal ? Vector3.forward : Vector3.right;
            var sideSign = (hash & 1) == 0 ? 1f : -1f;
            var material = RoadTrafficReadoutMaterial(loadPercent);
            var center = CellCenter(road.Pos, roadTop + 0.164f)
                - along * cellSize * 0.22f
                + side * sideSign * cellSize * 0.31f;
            var plateScale = horizontal
                ? new Vector3(cellSize * 0.24f, 0.038f, cellSize * 0.14f)
                : new Vector3(cellSize * 0.14f, 0.038f, cellSize * 0.24f);
            var trackScale = horizontal
                ? new Vector3(cellSize * 0.17f, 0.018f, 0.032f)
                : new Vector3(0.032f, 0.018f, cellSize * 0.17f);
            var tetherScale = horizontal
                ? new Vector3(0.034f, 0.018f, cellSize * 0.2f)
                : new Vector3(cellSize * 0.2f, 0.018f, 0.034f);

            AddLooseCube(roadObjects, "RoadTrafficReadoutPlate", material, center, plateScale);
            AddLooseCube(roadObjects, "RoadTrafficReadoutTrack", roadLineMaterial, center + new Vector3(0f, 0.041f, 0f), trackScale);
            AddLooseCube(roadObjects, "RoadTrafficReadoutTether", windowMaterial, center - side * sideSign * cellSize * 0.15f + new Vector3(0f, -0.018f, 0f), tetherScale);
            AddRoadTrafficReadoutBars(center, horizontal, loadPercent, material);
        }

        private void AddRoadTrafficReadoutBars(Vector3 center, bool horizontal, int loadPercent, Material material)
        {
            var barCount = loadPercent >= 90 ? 3 : (loadPercent >= 68 ? 2 : 1);
            var along = horizontal ? Vector3.right : Vector3.forward;
            var side = horizontal ? Vector3.forward : Vector3.right;
            for (var i = 0; i < barCount; i += 1)
            {
                var offset = (i - (barCount - 1) * 0.5f) * cellSize * 0.052f;
                var height = 0.034f + i * 0.014f + Mathf.Clamp01((loadPercent - 44) / 56f) * 0.018f;
                var barMaterial = i == barCount - 1 ? material : windowMaterial;
                AddLooseCube(roadObjects, "RoadTrafficReadoutBar", barMaterial, center + along * offset - side * cellSize * 0.025f + new Vector3(0f, 0.072f + i * 0.004f, 0f), new Vector3(0.034f, height, 0.034f));
            }
        }

        private Material RoadTrafficReadoutMaterial(int loadPercent)
        {
            if (loadPercent >= 88)
            {
                return trafficPulseMaterial;
            }

            if (loadPercent >= 68)
            {
                return serviceNeedMaterial;
            }

            return previewOkMaterial;
        }

        private void AddCentralBoulevardCues(RoadNode road, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINES_REFERENCE_BOULEVARD_CUES gives the road grid a brighter main-street skeleton.
            var connections = RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp);
            var central = IsCentralRoadTile(road.Pos);
            if (road.Tier == RoadTier.Arterial)
            {
                AddRoadBoulevardMedian(road.Pos, hasHorizontal, hasVertical, roadTop);
            }

            if (central && connections >= 2)
            {
                AddCentralSidewalkCues(road.Pos, hasHorizontal, hasVertical, roadTop);
                AddCentralStreetFurniture(road.Pos, hasHorizontal, hasVertical, roadTop);
                AddRoadsideParkingBayCues(road.Pos, hasHorizontal, hasVertical, roadTop);
            }

            if ((central && connections >= 2) || road.Tier == RoadTier.Arterial)
            {
                AddTransitStopCue(road, hasHorizontal, hasVertical, roadTop);
            }

            if (central && connections >= 3)
            {
                AddCentralIntersectionPlaza(road.Pos, hasLeft, hasRight, hasDown, hasUp, roadTop);
            }
        }

        private bool IsCentralRoadTile(GridPos pos)
        {
            if (controller == null || controller.Grid == null)
            {
                return false;
            }

            var centerX = (controller.Grid.Width - 1) * 0.5f;
            var centerY = (controller.Grid.Height - 1) * 0.5f;
            var radiusX = Mathf.Max(4f, controller.Grid.Width * 0.32f);
            var radiusY = Mathf.Max(3f, controller.Grid.Height * 0.32f);
            return Mathf.Abs(pos.X - centerX) <= radiusX && Mathf.Abs(pos.Y - centerY) <= radiusY;
        }

        private void AddRoadBoulevardMedian(GridPos pos, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            if (hasHorizontal || !hasVertical)
            {
                AddRoadDetailMark("LowPolyBoulevardGreenMedian", grassGridMaterial, pos, cellSize * 0.42f, 0.045f, -cellSize * 0.16f, 0f, roadTop + 0.012f);
                AddRoadDetailMark("LowPolyBoulevardGreenMedian", grassGridMaterial, pos, cellSize * 0.42f, 0.045f, cellSize * 0.16f, 0f, roadTop + 0.012f);
            }

            if (hasVertical)
            {
                AddRoadDetailMark("LowPolyBoulevardGreenMedian", grassGridMaterial, pos, 0.045f, cellSize * 0.42f, 0f, -cellSize * 0.16f, roadTop + 0.012f);
                AddRoadDetailMark("LowPolyBoulevardGreenMedian", grassGridMaterial, pos, 0.045f, cellSize * 0.42f, 0f, cellSize * 0.16f, roadTop + 0.012f);
            }

            AddBoulevardMedianMicroDetail(pos, hasHorizontal, hasVertical, roadTop);
        }

        private void AddBoulevardMedianMicroDetail(GridPos pos, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // REFERENCE_IMAGE_BOULEVARD_TREES makes arterial medians feel like planned green avenues.
            var hash = DecorationHash(pos.X, pos.Y);
            if (hash % 2 != 0)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            var center = CellCenter(pos, roadTop + 0.08f);
            var offset = horizontal
                ? new Vector3((((hash >> 2) & 1) == 0 ? -0.16f : 0.16f) * cellSize, 0f, 0f)
                : new Vector3(0f, 0f, (((hash >> 2) & 1) == 0 ? -0.16f : 0.16f) * cellSize);
            var detailCenter = center + offset;
            AddLooseCube(roadObjects, "LowPolyBoulevardTreeTrunk", treeTrunkMaterial, detailCenter + new Vector3(0f, 0.07f, 0f), new Vector3(0.035f, 0.14f, 0.035f));
            AddLooseCube(roadObjects, "LowPolyBoulevardTreeCanopy", treeCanopyMaterial, detailCenter + new Vector3(0f, 0.18f, 0f), new Vector3(0.16f, 0.14f, 0.16f));
            if (hash % 4 == 0)
            {
                AddLooseCube(roadObjects, "LowPolyBoulevardLampPost", serviceMaterial, center - offset + new Vector3(0f, 0.1f, 0f), new Vector3(0.032f, 0.2f, 0.032f));
                AddLooseCube(roadObjects, "LowPolyBoulevardLampGlow", windowMaterial, center - offset + new Vector3(0f, 0.22f, 0f), new Vector3(0.1f, 0.04f, 0.1f));
            }
        }

        private void AddTransitStopCue(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINES_TRANSIT_STOP_CUES makes planned corridors read like operated public transport lines.
            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            var central = IsCentralRoadTile(road.Pos);
            if (road.Tier != RoadTier.Arterial && (!central || hash % 4 != 0))
            {
                return;
            }

            if (road.Tier == RoadTier.Arterial && hash % 3 == 1)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            var side = ((hash >> 2) & 1) == 0 ? -1f : 1f;
            var along = (((hash >> 5) & 3) - 1.5f) * cellSize * 0.09f;
            var normalOffset = side * cellSize * 0.48f;
            var baseCenter = horizontal
                ? CellCenter(road.Pos, roadTop) + new Vector3(along, 0f, normalOffset)
                : CellCenter(road.Pos, roadTop) + new Vector3(normalOffset, 0f, along);
            var platformScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.035f, cellSize * 0.095f)
                : new Vector3(cellSize * 0.095f, 0.035f, cellSize * 0.34f);
            var backScale = horizontal
                ? new Vector3(cellSize * 0.25f, 0.16f, 0.035f)
                : new Vector3(0.035f, 0.16f, cellSize * 0.25f);
            var roofScale = horizontal
                ? new Vector3(cellSize * 0.29f, 0.045f, cellSize * 0.14f)
                : new Vector3(cellSize * 0.14f, 0.045f, cellSize * 0.29f);

            AddLooseCube(roadObjects, "LowPolyTransitStopPlatform", shoreMaterial != null ? shoreMaterial : roadLineMaterial, baseCenter + new Vector3(0f, 0.055f, 0f), platformScale);
            AddLooseCube(roadObjects, "LowPolyTransitStopGlassBack", windowMaterial, baseCenter + new Vector3(0f, 0.16f, 0f), backScale);
            AddLooseCube(roadObjects, "LowPolyTransitStopRoof", serviceNeedMaterial, baseCenter + new Vector3(0f, 0.265f, 0f), roofScale);
            AddTransitStopCurbPaint(baseCenter, horizontal, side);
            AddTransitStopShelterDetails(baseCenter, horizontal, side);
            AddTransitStopBusMarker(baseCenter, horizontal, side);
            AddTransitStopSchedulePips(baseCenter, horizontal, side, hash);

            var routeOffset = horizontal
                ? new Vector3(cellSize * 0.18f, 0f, -side * cellSize * 0.055f)
                : new Vector3(-side * cellSize * 0.055f, 0f, cellSize * 0.18f);
            var signCenter = baseCenter + routeOffset;
            AddLooseCube(roadObjects, "LowPolyTransitStopSignPost", serviceMaterial, signCenter + new Vector3(0f, 0.16f, 0f), new Vector3(0.032f, 0.25f, 0.032f));
            AddLooseCube(roadObjects, "LowPolyTransitStopRoutePlate", commercialMaterial, signCenter + new Vector3(0f, 0.31f, 0f), horizontal ? new Vector3(0.16f, 0.07f, 0.035f) : new Vector3(0.035f, 0.07f, 0.16f));
            AddLooseCube(roadObjects, "LowPolyTransitStopTimetable", roadLineMaterial, signCenter + new Vector3(0f, 0.245f, 0f), horizontal ? new Vector3(0.1f, 0.045f, 0.028f) : new Vector3(0.028f, 0.045f, 0.1f));

            if (hash % 2 == 0)
            {
                var passengerCenter = baseCenter - routeOffset * 0.42f;
                AddLooseCube(roadObjects, "LowPolyTransitPassengerBody", mixedUseMaterial, passengerCenter + new Vector3(0f, 0.13f, 0f), new Vector3(0.055f, 0.15f, 0.055f));
                AddLooseCube(roadObjects, "LowPolyTransitPassengerHead", roofMaterial, passengerCenter + new Vector3(0f, 0.24f, 0f), new Vector3(0.07f, 0.06f, 0.07f));
            }
        }

        private void AddTransitStopCurbPaint(Vector3 baseCenter, bool horizontal, float side)
        {
            var offset = horizontal
                ? new Vector3(0f, 0.036f, -side * cellSize * 0.15f)
                : new Vector3(-side * cellSize * 0.15f, 0.036f, 0f);
            var stripeScale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.018f, 0.028f)
                : new Vector3(0.028f, 0.018f, cellSize * 0.3f);
            AddLooseCube(roadObjects, "LowPolyTransitStopCurbStripe", roadLineMaterial, baseCenter + offset, stripeScale);
            AddLooseCube(roadObjects, "LowPolyTransitStopQueueTile", windowMaterial, baseCenter - offset * 0.55f + new Vector3(0f, 0.032f, 0f), stripeScale * 0.62f);
        }

        private void AddTransitStopShelterDetails(Vector3 baseCenter, bool horizontal, float side)
        {
            // LOW_POLY_TRANSIT_SHELTER_DETAILS makes bus stops read as usable waiting shelters.
            var along = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var shelterSide = normal * side;
            var postScale = new Vector3(0.032f, 0.2f, 0.032f);
            var benchScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.045f, 0.045f)
                : new Vector3(0.045f, 0.045f, cellSize * 0.18f);
            AddLooseCube(roadObjects, "LowPolyTransitShelterPost", serviceMaterial, baseCenter + along * cellSize * 0.13f + shelterSide * cellSize * 0.055f + new Vector3(0f, 0.17f, 0f), postScale);
            AddLooseCube(roadObjects, "LowPolyTransitShelterPost", serviceMaterial, baseCenter - along * cellSize * 0.13f + shelterSide * cellSize * 0.055f + new Vector3(0f, 0.17f, 0f), postScale);
            AddLooseCube(roadObjects, "LowPolyTransitShelterBench", roadLineMaterial, baseCenter - shelterSide * cellSize * 0.035f + new Vector3(0f, 0.105f, 0f), benchScale);
            AddLooseCube(roadObjects, "LowPolyTransitShelterMapPanel", windowMaterial, baseCenter - along * cellSize * 0.13f + new Vector3(0f, 0.2f, 0f), horizontal ? new Vector3(0.035f, 0.12f, 0.08f) : new Vector3(0.08f, 0.12f, 0.035f));
        }

        private void AddTransitStopBusMarker(Vector3 baseCenter, bool horizontal, float side)
        {
            // LOW_POLY_BUS_STOP_MARKER makes the shelter read clearly even when zoomed out.
            var laneOffset = horizontal
                ? new Vector3(0f, 0f, -side * cellSize * 0.28f)
                : new Vector3(-side * cellSize * 0.28f, 0f, 0f);
            var busCenter = baseCenter + laneOffset + new Vector3(0f, 0.075f, 0f);
            AddRoadCarPart("LowPolyTransitMiniBusBody", commercialMaterial, busCenter, horizontal, 0.34f, 0.13f, 0.095f);
            AddRoadCarPart("LowPolyTransitMiniBusWindowBand", windowMaterial, busCenter + new Vector3(0f, 0.065f, 0f), horizontal, 0.24f, 0.11f, 0.035f);
            AddRoadCarPart("LowPolyTransitMiniBusStripe", roadLineMaterial, busCenter + new Vector3(0f, 0.03f, 0f), horizontal, 0.28f, 0.135f, 0.02f);
        }

        private void AddTransitStopSchedulePips(Vector3 baseCenter, bool horizontal, float side, int seed)
        {
            // LOW_POLY_TRANSIT_SCHEDULE_PIPS make station signs read as active city infrastructure.
            var along = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var boardCenter = baseCenter
                - normal * side * cellSize * 0.02f
                + along * ((((seed >> 6) & 1) == 0 ? -1f : 1f) * cellSize * 0.11f)
                + new Vector3(0f, 0.335f, 0f);
            var plateScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.052f, 0.03f)
                : new Vector3(0.03f, 0.052f, cellSize * 0.18f);
            var pipScale = horizontal
                ? new Vector3(cellSize * 0.036f, 0.018f, 0.028f)
                : new Vector3(0.028f, 0.018f, cellSize * 0.036f);
            AddLooseCube(roadObjects, "LowPolyTransitSchedulePlate", roadLineMaterial, boardCenter, plateScale);
            for (var i = 0; i < 3; i += 1)
            {
                var material = i == 0 ? windowMaterial : (i == 2 ? serviceNeedMaterial : lockedAreaMaterial);
                AddLooseCube(roadObjects, "LowPolyTransitSchedulePip", material, boardCenter + along * ((i - 1) * cellSize * 0.055f) + new Vector3(0f, 0.035f + i * 0.004f, 0f), pipScale);
            }

            var flagCenter = baseCenter + normal * side * cellSize * 0.11f - along * cellSize * 0.2f;
            AddLooseCube(roadObjects, "LowPolyTransitStopBeaconPost", serviceMaterial, flagCenter + new Vector3(0f, 0.22f, 0f), new Vector3(0.028f, 0.22f, 0.028f));
            AddLooseCube(roadObjects, "LowPolyTransitStopBeaconCap", windowMaterial, flagCenter + new Vector3(0f, 0.35f, 0f), new Vector3(0.09f, 0.035f, 0.09f));
        }

        private void AddCentralSidewalkCues(GridPos pos, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            var material = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            if (hasHorizontal || !hasVertical)
            {
                AddRoadDetailMark("LowPolyMainStreetWalk", material, pos, cellSize * 0.46f, 0.034f, 0f, -cellSize * 0.39f, roadTop + 0.01f);
                AddRoadDetailMark("LowPolyMainStreetWalk", material, pos, cellSize * 0.46f, 0.034f, 0f, cellSize * 0.39f, roadTop + 0.01f);
            }

            if (hasVertical)
            {
                AddRoadDetailMark("LowPolyMainStreetWalk", material, pos, 0.034f, cellSize * 0.46f, -cellSize * 0.39f, 0f, roadTop + 0.01f);
                AddRoadDetailMark("LowPolyMainStreetWalk", material, pos, 0.034f, cellSize * 0.46f, cellSize * 0.39f, 0f, roadTop + 0.01f);
            }
        }

        private void AddCentralStreetFurniture(GridPos pos, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // REFERENCE_IMAGE_MAIN_STREET_PROPS adds tiny planters, kiosks and signs along the bright central roads.
            var hash = DecorationHash(pos.X, pos.Y);
            if ((hasHorizontal || !hasVertical) && hash % 2 == 0)
            {
                AddCentralStreetFurnitureSet(pos, true, ((hash >> 3) & 1) == 0 ? -1f : 1f, hash, roadTop);
            }

            if (hasVertical && hash % 3 != 1)
            {
                AddCentralStreetFurnitureSet(pos, false, ((hash >> 4) & 1) == 0 ? -1f : 1f, hash >> 1, roadTop);
            }
        }

        private void AddCentralStreetFurnitureSet(GridPos pos, bool horizontal, float side, int hash, float roadTop)
        {
            var center = CellCenter(pos, roadTop);
            var along = (((hash >> 1) & 1) == 0 ? -0.19f : 0.19f) * cellSize;
            var edge = side * cellSize * 0.43f;
            var furnitureBase = horizontal
                ? center + new Vector3(along, 0f, edge)
                : center + new Vector3(edge, 0f, along);
            var longScale = horizontal
                ? new Vector3(0.18f, 0.065f, 0.09f)
                : new Vector3(0.09f, 0.065f, 0.18f);

            AddLooseCube(roadObjects, "LowPolyMainStreetPlanterBox", shoreMaterial != null ? shoreMaterial : roadLineMaterial, furnitureBase + new Vector3(0f, 0.055f, 0f), longScale);
            AddLooseCube(roadObjects, "LowPolyMainStreetPlanterGreen", treeCanopyMaterial, furnitureBase + new Vector3(0f, 0.115f, 0f), longScale * 0.72f);

            var signAlong = (((hash >> 2) & 1) == 0 ? 0.12f : -0.12f) * cellSize;
            var signBase = horizontal
                ? center + new Vector3(signAlong, 0f, edge - side * cellSize * 0.08f)
                : center + new Vector3(edge - side * cellSize * 0.08f, 0f, signAlong);
            AddLooseCube(roadObjects, "LowPolyMainStreetSignPost", serviceMaterial, signBase + new Vector3(0f, 0.15f, 0f), new Vector3(0.032f, 0.26f, 0.032f));
            AddLooseCube(roadObjects, "LowPolyMainStreetSignPlate", windowMaterial, signBase + new Vector3(0f, 0.29f, 0f), horizontal ? new Vector3(0.18f, 0.07f, 0.032f) : new Vector3(0.032f, 0.07f, 0.18f));

            if (hash % 4 == 0)
            {
                var kioskBase = horizontal
                    ? center + new Vector3(-along, 0f, edge)
                    : center + new Vector3(edge, 0f, -along);
                AddLooseCube(roadObjects, "LowPolyMainStreetKiosk", commercialMaterial, kioskBase + new Vector3(0f, 0.12f, 0f), new Vector3(0.15f, 0.18f, 0.15f));
                AddLooseCube(roadObjects, "LowPolyMainStreetKioskAwning", roofMaterial, kioskBase + new Vector3(0f, 0.24f, -side * 0.02f), horizontal ? new Vector3(0.22f, 0.045f, 0.12f) : new Vector3(0.12f, 0.045f, 0.22f));
            }
        }

        private void AddRoadsideParkingBayCues(GridPos pos, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // REFERENCE_IMAGE_ROADSIDE_PARKING_BAYS clarifies the central road edge without adding simulation lots.
            var hash = DecorationHash(pos.X, pos.Y);
            if (hash % 3 != 0)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            var side = ((hash >> 4) & 1) == 0 ? -1f : 1f;
            var center = CellCenter(pos, roadTop + 0.034f);
            var bayCenter = horizontal
                ? center + new Vector3((((hash >> 1) & 1) == 0 ? -0.18f : 0.18f) * cellSize, 0f, side * cellSize * 0.5f)
                : center + new Vector3(side * cellSize * 0.5f, 0f, (((hash >> 1) & 1) == 0 ? -0.18f : 0.18f) * cellSize);
            var bayScale = horizontal
                ? new Vector3(cellSize * 0.28f, 0.018f, cellSize * 0.12f)
                : new Vector3(cellSize * 0.12f, 0.018f, cellSize * 0.28f);
            var lineScale = horizontal
                ? new Vector3(0.026f, 0.018f, cellSize * 0.12f)
                : new Vector3(cellSize * 0.12f, 0.018f, 0.026f);
            AddLooseCube(roadObjects, "LowPolyRoadsideParkingBay", roadLineMaterial, bayCenter, bayScale);
            AddLooseCube(roadObjects, "LowPolyRoadsideParkingDivider", roadLineMaterial, bayCenter + (horizontal ? new Vector3(cellSize * 0.13f, 0.006f, 0f) : new Vector3(0f, 0.006f, cellSize * 0.13f)), lineScale);
            AddRoadsideParkingBaySignage(bayCenter, horizontal, side, hash);

            if (hash % 6 == 0)
            {
                var carCenter = bayCenter + new Vector3(0f, 0.065f, 0f);
                AddRoadCarPart("LowPolyParkedCarBody", serviceNeedMaterial, carCenter, horizontal, 0.2f, 0.1f, 0.065f);
                AddRoadCarPart("LowPolyParkedCarCabin", windowMaterial, carCenter + new Vector3(0f, 0.045f, 0f), horizontal, 0.09f, 0.08f, 0.035f);
            }
        }

        private void AddRoadsideParkingBaySignage(Vector3 bayCenter, bool horizontal, float side, int hash)
        {
            // REFERENCE_IMAGE_ROADSIDE_PARKING_SIGNS adds tiny readable P marks to the curb without new assets.
            var along = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var paintCenter = bayCenter - along * cellSize * 0.07f + new Vector3(0f, 0.028f, 0f);
            var stemScale = horizontal
                ? new Vector3(0.026f, 0.018f, cellSize * 0.105f)
                : new Vector3(cellSize * 0.105f, 0.018f, 0.026f);
            var loopTopScale = horizontal
                ? new Vector3(cellSize * 0.095f, 0.018f, 0.024f)
                : new Vector3(0.024f, 0.018f, cellSize * 0.095f);
            var loopSideScale = horizontal
                ? new Vector3(0.024f, 0.018f, cellSize * 0.066f)
                : new Vector3(cellSize * 0.066f, 0.018f, 0.024f);

            AddLooseCube(roadObjects, "LowPolyParkingPStem", windowMaterial, paintCenter - along * cellSize * 0.035f, stemScale);
            AddLooseCube(roadObjects, "LowPolyParkingPTop", windowMaterial, paintCenter + along * cellSize * 0.018f + normal * side * cellSize * 0.035f, loopTopScale);
            AddLooseCube(roadObjects, "LowPolyParkingPBowl", windowMaterial, paintCenter + along * cellSize * 0.045f, loopSideScale);

            if (hash % 2 != 0)
            {
                return;
            }

            var signBase = bayCenter + normal * side * cellSize * 0.16f + along * cellSize * 0.14f;
            AddLooseCube(roadObjects, "LowPolyParkingSignPost", serviceMaterial, signBase + new Vector3(0f, 0.14f, 0f), new Vector3(0.03f, 0.24f, 0.03f));
            AddLooseCube(roadObjects, "LowPolyParkingSignPlate", commercialMaterial, signBase + new Vector3(0f, 0.28f, 0f), horizontal ? new Vector3(0.15f, 0.07f, 0.032f) : new Vector3(0.032f, 0.07f, 0.15f));
            AddLooseCube(roadObjects, "LowPolyParkingSignPip", windowMaterial, signBase + new Vector3(0f, 0.325f, 0f), new Vector3(0.055f, 0.026f, 0.026f));
        }

        private void AddRoadJunctionWayfindingSigns(RoadNode road, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, bool hasHorizontal, bool hasVertical, float roadTop, int connectionCount)
        {
            // REFERENCE_IMAGE_JUNCTION_WAYFINDING_SIGNS adds tiny readable street furniture to corners and crossings.
            if (connectionCount < 2)
            {
                return;
            }

            var cornerTurn = connectionCount == 2 && hasHorizontal && hasVertical;
            if (!cornerTurn && connectionCount < 3)
            {
                return;
            }

            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            if (road.Tier != RoadTier.Arterial && !cornerTurn && hash % 3 == 1)
            {
                return;
            }

            var signX = RoadGuideOpenSide(hasLeft, hasRight, hash, 0);
            var signZ = RoadGuideOpenSide(hasDown, hasUp, hash, 1);
            var baseCenter = CellCenter(road.Pos, roadTop + 0.13f) + new Vector3(signX * cellSize * 0.43f, 0f, signZ * cellSize * 0.43f);
            var plateMaterial = road.Tier == RoadTier.Arterial ? commercialMaterial : windowMaterial;

            AddLooseCube(roadObjects, "LowPolyJunctionGuidePost", serviceMaterial, baseCenter, new Vector3(0.036f, 0.26f, 0.036f));
            AddLooseCube(roadObjects, "LowPolyJunctionGuidePlateX", plateMaterial, baseCenter + new Vector3(0f, 0.16f, 0f), new Vector3(cellSize * 0.25f, 0.065f, 0.034f));
            AddLooseCube(roadObjects, "LowPolyJunctionGuidePlateZ", serviceNeedMaterial, baseCenter + new Vector3(0f, 0.245f, 0f), new Vector3(0.034f, 0.065f, cellSize * 0.25f));
            AddLooseCube(roadObjects, "LowPolyJunctionGuideCap", roadLineMaterial, baseCenter + new Vector3(0f, 0.33f, 0f), new Vector3(0.085f, 0.035f, 0.085f));
            AddRoadJunctionGroundArrow(road.Pos, hasHorizontal || !hasVertical, -signX, -signZ, roadTop);

            if (road.Tier != RoadTier.Arterial && (hash & 1) == 0)
            {
                AddRoadJunctionParkingGuide(baseCenter, hasHorizontal || !hasVertical, signX, signZ);
            }
        }

        private static float RoadGuideOpenSide(bool hasNegative, bool hasPositive, int hash, int bitOffset)
        {
            if (hasNegative && !hasPositive) return 1f;
            if (hasPositive && !hasNegative) return -1f;
            return ((hash >> bitOffset) & 1) == 0 ? -1f : 1f;
        }

        private void AddRoadJunctionGroundArrow(GridPos pos, bool horizontal, float signX, float signZ, float roadTop)
        {
            var center = CellCenter(pos, roadTop + 0.092f) + new Vector3(signX * cellSize * 0.24f, 0f, signZ * cellSize * 0.24f);
            var stemScale = horizontal
                ? new Vector3(cellSize * 0.19f, 0.014f, 0.03f)
                : new Vector3(0.03f, 0.014f, cellSize * 0.19f);
            var headScale = new Vector3(cellSize * 0.09f, 0.014f, 0.026f);

            AddLooseCube(roadObjects, "LowPolyJunctionGroundArrowStem", roadLineMaterial, center, stemScale);
            if (horizontal)
            {
                var xDir = Mathf.Approximately(signX, 0f) ? 1f : Mathf.Sign(signX);
                AddLooseCubeRotated(roadObjects, "LowPolyJunctionGroundArrowHead", roadLineMaterial, center + new Vector3(xDir * cellSize * 0.1f, 0f, cellSize * 0.03f), headScale, xDir > 0f ? 35f : 145f);
                AddLooseCubeRotated(roadObjects, "LowPolyJunctionGroundArrowHead", roadLineMaterial, center + new Vector3(xDir * cellSize * 0.1f, 0f, -cellSize * 0.03f), headScale, xDir > 0f ? -35f : -145f);
                return;
            }

            var zDir = Mathf.Approximately(signZ, 0f) ? 1f : Mathf.Sign(signZ);
            AddLooseCubeRotated(roadObjects, "LowPolyJunctionGroundArrowHead", roadLineMaterial, center + new Vector3(cellSize * 0.03f, 0f, zDir * cellSize * 0.1f), headScale, zDir > 0f ? 55f : -55f);
            AddLooseCubeRotated(roadObjects, "LowPolyJunctionGroundArrowHead", roadLineMaterial, center + new Vector3(-cellSize * 0.03f, 0f, zDir * cellSize * 0.1f), headScale, zDir > 0f ? 125f : -125f);
        }

        private void AddRoadJunctionParkingGuide(Vector3 baseCenter, bool horizontal, float signX, float signZ)
        {
            var offset = horizontal
                ? new Vector3(-signX * cellSize * 0.12f, 0f, -signZ * cellSize * 0.055f)
                : new Vector3(-signX * cellSize * 0.055f, 0f, -signZ * cellSize * 0.12f);
            var signCenter = baseCenter + offset + new Vector3(0f, 0.13f, 0f);
            var plateScale = horizontal
                ? new Vector3(cellSize * 0.14f, 0.065f, 0.03f)
                : new Vector3(0.03f, 0.065f, cellSize * 0.14f);

            AddLooseCube(roadObjects, "LowPolyJunctionParkingPlate", commercialMaterial, signCenter, plateScale);
            AddLooseCube(roadObjects, "LowPolyJunctionParkingPStem", roadLineMaterial, signCenter + new Vector3(0f, 0.042f, 0f), new Vector3(0.026f, 0.09f, 0.026f));
            AddLooseCube(roadObjects, "LowPolyJunctionParkingPTop", roadLineMaterial, signCenter + new Vector3(0.045f, 0.07f, 0f), horizontal ? new Vector3(0.08f, 0.026f, 0.024f) : new Vector3(0.024f, 0.026f, 0.08f));
        }

        private void AddCentralIntersectionPlaza(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            var plazaMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            AddRoadDetailMark("LowPolyCentralIntersectionPlaza", plazaMaterial, pos, cellSize * 0.26f, cellSize * 0.26f, 0f, 0f, roadTop + 0.014f);
            if (hasLeft) AddRoadDetailMark("LowPolyCentralIntersectionCorner", grassGridMaterial, pos, 0.08f, 0.08f, -cellSize * 0.29f, -cellSize * 0.29f, roadTop + 0.016f);
            if (hasRight) AddRoadDetailMark("LowPolyCentralIntersectionCorner", grassGridMaterial, pos, 0.08f, 0.08f, cellSize * 0.29f, cellSize * 0.29f, roadTop + 0.016f);
            if (hasDown) AddRoadDetailMark("LowPolyCentralIntersectionCorner", grassGridMaterial, pos, 0.08f, 0.08f, cellSize * 0.29f, -cellSize * 0.29f, roadTop + 0.016f);
            if (hasUp) AddRoadDetailMark("LowPolyCentralIntersectionCorner", grassGridMaterial, pos, 0.08f, 0.08f, -cellSize * 0.29f, cellSize * 0.29f, roadTop + 0.016f);
            AddCentralPlazaFountain(pos, roadTop);
        }

        private void AddCentralPlazaFountain(GridPos pos, float roadTop)
        {
            // REFERENCE_IMAGE_CENTER_PLAZA_FOUNTAIN gives the city core a bright low-poly landmark.
            var center = CellCenter(pos, roadTop + 0.055f);
            AddLooseCube(roadObjects, "LowPolyCentralFountainBasin", windowMaterial, center + new Vector3(0f, 0.02f, 0f), new Vector3(cellSize * 0.14f, 0.035f, cellSize * 0.14f));
            AddLooseCube(roadObjects, "LowPolyCentralFountainJet", windowMaterial, center + new Vector3(0f, 0.12f, 0f), new Vector3(cellSize * 0.04f, 0.18f, cellSize * 0.04f));
            AddLooseCube(roadObjects, "LowPolyCentralFountainSparkle", roadLineMaterial, center + new Vector3(0f, 0.22f, 0f), new Vector3(cellSize * 0.12f, 0.028f, cellSize * 0.04f));
            AddLooseCube(roadObjects, "LowPolyCentralFlowerBed", serviceNeedMaterial, center + new Vector3(cellSize * 0.18f, 0.02f, 0f), new Vector3(cellSize * 0.09f, 0.035f, cellSize * 0.09f));
            AddLooseCube(roadObjects, "LowPolyCentralFlowerBed", treeCanopyMaterial, center + new Vector3(-cellSize * 0.18f, 0.02f, 0f), new Vector3(cellSize * 0.09f, 0.035f, cellSize * 0.09f));
        }

        private void AddRoadCenterMark(GridPos pos, float width, float depth, float roadTop)
        {
            var marker = CreateCube("RoadCenterMark", roadLineMaterial);
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = CellCenter(pos, roadTop + 0.012f);
            marker.transform.localScale = new Vector3(width, 0.014f, depth);
            roadObjects.Add(marker);
        }

        private void AddArterialLaneEdges(GridPos pos, bool hasHorizontal, bool hasVertical, float width, float roadTop)
        {
            // CITY_SKYLINE_ROAD_DETAILS makes arterial corridors read clearly in the isometric city view.
            var shoulderMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            if (hasHorizontal || !hasVertical)
            {
                AddRoadDetailMark("ArterialShoulderBand", shoulderMaterial, pos, width * 0.86f, 0.035f, 0f, -cellSize * 0.42f, roadTop + 0.008f);
                AddRoadDetailMark("ArterialShoulderBand", shoulderMaterial, pos, width * 0.86f, 0.035f, 0f, cellSize * 0.42f, roadTop + 0.008f);
                AddRoadDetailMark("ArterialLaneEdge", pos, width * 0.74f, 0.026f, 0f, -cellSize * 0.28f, roadTop);
                AddRoadDetailMark("ArterialLaneEdge", pos, width * 0.74f, 0.026f, 0f, cellSize * 0.28f, roadTop);
            }

            if (hasVertical)
            {
                AddRoadDetailMark("ArterialShoulderBand", shoulderMaterial, pos, 0.035f, width * 0.86f, -cellSize * 0.42f, 0f, roadTop + 0.008f);
                AddRoadDetailMark("ArterialShoulderBand", shoulderMaterial, pos, 0.035f, width * 0.86f, cellSize * 0.42f, 0f, roadTop + 0.008f);
                AddRoadDetailMark("ArterialLaneEdge", pos, 0.026f, width * 0.74f, -cellSize * 0.28f, 0f, roadTop);
                AddRoadDetailMark("ArterialLaneEdge", pos, 0.026f, width * 0.74f, cellSize * 0.28f, 0f, roadTop);
            }
        }

        private void AddRoadLaneDashes(GridPos pos, bool hasHorizontal, bool hasVertical, float roadTop, RoadTier tier, int connectionCount)
        {
            // REFERENCE_IMAGE_DASHED_LANE_MARKERS gives straight roads crisp toy-city lane rhythm.
            if (connectionCount == 0 || connectionCount >= 3)
            {
                return;
            }

            var laneOffset = tier == RoadTier.Arterial ? cellSize * 0.18f : cellSize * 0.14f;
            var dashLength = tier == RoadTier.Arterial ? cellSize * 0.16f : cellSize * 0.12f;
            var dashWidth = tier == RoadTier.Arterial ? 0.032f : 0.026f;

            if (hasHorizontal || !hasVertical)
            {
                AddRoadLaneDashStrip(pos, true, laneOffset, roadTop, dashLength, dashWidth);
            }

            if (hasVertical)
            {
                AddRoadLaneDashStrip(pos, false, laneOffset, roadTop, dashLength, dashWidth);
            }
        }

        private void AddRoadLaneDashStrip(GridPos pos, bool horizontal, float laneOffset, float roadTop, float dashLength, float dashWidth)
        {
            var center = CellCenter(pos, roadTop + 0.041f);
            var scale = horizontal
                ? new Vector3(dashLength, 0.012f, dashWidth)
                : new Vector3(dashWidth, 0.012f, dashLength);
            for (var i = -1; i <= 1; i += 1)
            {
                var along = i * cellSize * 0.22f;
                var offset = horizontal
                    ? new Vector3(along, 0f, laneOffset)
                    : new Vector3(laneOffset, 0f, along);
                AddLooseCube(roadObjects, "LowPolyRoadLaneDash", windowMaterial, center + offset, scale);
            }
        }

        private void AddRoadIntersectionCrosswalks(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            if (hasLeft) AddCrosswalkSet(pos, -1f, 0f, roadTop);
            if (hasRight) AddCrosswalkSet(pos, 1f, 0f, roadTop);
            if (hasDown) AddCrosswalkSet(pos, 0f, -1f, roadTop);
            if (hasUp) AddCrosswalkSet(pos, 0f, 1f, roadTop);
        }

        private void AddRoadIntersectionPavers(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // REFERENCE_IMAGE_INTERSECTION_PAVERS gives busy junctions a polished city-builder plaza read.
            var pavingMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            AddRoadDetailMark("RoadIntersectionPaver", pavingMaterial, pos, cellSize * 0.5f, cellSize * 0.5f, 0f, 0f, roadTop);
            if (hasLeft) AddRoadDetailMark("RoadTurnGuide", roadLineMaterial, pos, cellSize * 0.18f, 0.03f, -cellSize * 0.18f, -cellSize * 0.18f, roadTop);
            if (hasRight) AddRoadDetailMark("RoadTurnGuide", roadLineMaterial, pos, cellSize * 0.18f, 0.03f, cellSize * 0.18f, cellSize * 0.18f, roadTop);
            if (hasDown) AddRoadDetailMark("RoadTurnGuide", roadLineMaterial, pos, 0.03f, cellSize * 0.18f, cellSize * 0.18f, -cellSize * 0.18f, roadTop);
            if (hasUp) AddRoadDetailMark("RoadTurnGuide", roadLineMaterial, pos, 0.03f, cellSize * 0.18f, -cellSize * 0.18f, cellSize * 0.18f, roadTop);
        }

        private void AddRoadIntersectionGardenIslands(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // REFERENCE_IMAGE_CLEAR_ISOMETRIC_JUNCTIONS adds readable corner islands without changing traffic logic.
            var seed = DecorationHash(pos.X, pos.Y);
            if (hasLeft && hasDown) AddRoadIntersectionGardenIsland(pos, -1f, -1f, roadTop, seed);
            if (hasLeft && hasUp) AddRoadIntersectionGardenIsland(pos, -1f, 1f, roadTop, seed >> 1);
            if (hasRight && hasDown) AddRoadIntersectionGardenIsland(pos, 1f, -1f, roadTop, seed >> 2);
            if (hasRight && hasUp) AddRoadIntersectionGardenIsland(pos, 1f, 1f, roadTop, seed >> 3);
        }

        private void AddRoadIntersectionGardenIsland(GridPos pos, float signX, float signZ, float roadTop, int seed)
        {
            var islandCenter = CellCenter(pos, roadTop + 0.04f) + new Vector3(signX * cellSize * 0.28f, 0f, signZ * cellSize * 0.28f);
            AddLooseCube(roadObjects, "LowPolyIntersectionCornerIsland", shoreMaterial != null ? shoreMaterial : roadLineMaterial, islandCenter, new Vector3(cellSize * 0.14f, 0.035f, cellSize * 0.14f));
            AddLooseCube(roadObjects, "LowPolyIntersectionGrassInset", grassGridMaterial, islandCenter + new Vector3(0f, 0.026f, 0f), new Vector3(cellSize * 0.095f, 0.022f, cellSize * 0.095f));
            AddRoadIntersectionPocketGreenery(islandCenter, signX, signZ, seed);
            if ((seed & 1) == 0)
            {
                AddLooseCube(roadObjects, "LowPolyIntersectionFlowerDot", serviceNeedMaterial, islandCenter + new Vector3(signX * cellSize * 0.018f, 0.06f, signZ * cellSize * 0.018f), new Vector3(cellSize * 0.045f, 0.035f, cellSize * 0.045f));
            }
        }

        private void AddRoadIntersectionPocketGreenery(Vector3 islandCenter, float signX, float signZ, int seed)
        {
            var shrubCenter = islandCenter + new Vector3(-signX * cellSize * 0.045f, 0.072f, -signZ * cellSize * 0.045f);
            AddLooseCube(roadObjects, "LowPolyIntersectionPocketShrub", treeCanopyMaterial, shrubCenter, new Vector3(cellSize * 0.07f, 0.07f, cellSize * 0.07f));
            if (seed % 3 != 0)
            {
                return;
            }

            AddLooseCube(roadObjects, "LowPolyIntersectionPocketMarkerPost", serviceMaterial, islandCenter + new Vector3(signX * cellSize * 0.06f, 0.12f, signZ * cellSize * 0.06f), new Vector3(0.028f, 0.18f, 0.028f));
            AddLooseCube(roadObjects, "LowPolyIntersectionPocketMarkerCap", windowMaterial, islandCenter + new Vector3(signX * cellSize * 0.06f, 0.225f, signZ * cellSize * 0.06f), new Vector3(0.08f, 0.034f, 0.08f));
        }

        private void AddRoadCornerPocketPaver(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // REFERENCE_IMAGE_STREET_CORNER_PAVERS makes L-turns read like small city blocks, not flat road squares.
            var pavingMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var signX = hasRight ? 1f : -1f;
            var signZ = hasUp ? 1f : -1f;
            AddRoadDetailMark("RoadCornerPocketPaver", pavingMaterial, pos, cellSize * 0.26f, cellSize * 0.26f, signX * cellSize * 0.22f, signZ * cellSize * 0.22f, roadTop + 0.006f);
            AddRoadDetailMark("RoadCornerCurbCap", roadLineMaterial, pos, cellSize * 0.28f, 0.026f, signX * cellSize * 0.17f, signZ * cellSize * 0.34f, roadTop + 0.01f);
            AddRoadDetailMark("RoadCornerCurbCap", roadLineMaterial, pos, 0.026f, cellSize * 0.28f, signX * cellSize * 0.34f, signZ * cellSize * 0.17f, roadTop + 0.01f);
        }

        private void AddRoadCornerPlantingCue(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // LOW_POLY_CORNER_PLANTERS make road elbows feel intentional and sunny.
            var signX = hasRight ? 1f : -1f;
            var signZ = hasUp ? 1f : -1f;
            var center = CellCenter(pos, roadTop + 0.055f) + new Vector3(signX * cellSize * 0.31f, 0f, signZ * cellSize * 0.31f);
            AddLooseCube(roadObjects, "RoadCornerPlanterPad", grassGridMaterial, center, new Vector3(cellSize * 0.12f, 0.034f, cellSize * 0.12f));
            if (DecorationHash(pos.X, pos.Y) % 3 == 0)
            {
                AddLooseCube(roadObjects, "RoadCornerPlanterSaplingTrunk", treeTrunkMaterial, center + new Vector3(0f, 0.08f, 0f), new Vector3(0.03f, 0.15f, 0.03f));
                AddLooseCube(roadObjects, "RoadCornerPlanterSaplingCanopy", treeCanopyMaterial, center + new Vector3(0f, 0.18f, 0f), new Vector3(0.11f, 0.1f, 0.11f));
                return;
            }

            AddLooseCube(roadObjects, "RoadCornerPlanterFlower", serviceNeedMaterial, center + new Vector3(0f, 0.055f, 0f), new Vector3(cellSize * 0.075f, 0.035f, cellSize * 0.075f));
        }

        private void AddCrosswalkSet(GridPos pos, float dirX, float dirY, float roadTop)
        {
            var alongHorizontal = Mathf.Abs(dirX) > 0f;
            var stopLineWidth = alongHorizontal ? 0.035f : 0.42f;
            var stopLineDepth = alongHorizontal ? 0.42f : 0.035f;
            AddRoadDetailMark("RoadStopLine", pos, stopLineWidth, stopLineDepth, dirX * cellSize * 0.18f, dirY * cellSize * 0.18f, roadTop + 0.004f);
            AddCrosswalkLandingPad(pos, dirX, dirY, alongHorizontal, roadTop);
            for (var i = -2; i <= 2; i += 1)
            {
                var lateral = i * cellSize * 0.075f;
                var centerX = dirX * cellSize * 0.31f + (alongHorizontal ? 0f : lateral);
                var centerZ = dirY * cellSize * 0.31f + (alongHorizontal ? lateral : 0f);
                var stripeWidth = alongHorizontal ? 0.105f : 0.28f;
                var stripeDepth = alongHorizontal ? 0.28f : 0.105f;
                AddRoadDetailMark("RoadCrosswalkStripe", pos, stripeWidth, stripeDepth, centerX, centerZ, roadTop);
            }

            var cornerX = dirX * cellSize * 0.34f + (alongHorizontal ? 0f : cellSize * 0.28f);
            var cornerZ = dirY * cellSize * 0.34f + (alongHorizontal ? cellSize * 0.28f : 0f);
            AddRoadDetailMark("RoadCrosswalkSafetyDot", serviceNeedMaterial, pos, 0.07f, 0.07f, cornerX, cornerZ, roadTop + 0.01f);
            AddCrosswalkCurbPins(pos, dirX, dirY, alongHorizontal, roadTop);
        }

        private void AddCrosswalkLandingPad(GridPos pos, float dirX, float dirY, bool alongHorizontal, float roadTop)
        {
            var material = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var padWidth = alongHorizontal ? 0.1f : 0.34f;
            var padDepth = alongHorizontal ? 0.34f : 0.1f;
            AddRoadDetailMark("RoadCrosswalkLandingPad", material, pos, padWidth, padDepth, dirX * cellSize * 0.43f, dirY * cellSize * 0.43f, roadTop + 0.006f);
        }

        private void AddCrosswalkCurbPins(GridPos pos, float dirX, float dirY, bool alongHorizontal, float roadTop)
        {
            var material = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            for (var side = -1; side <= 1; side += 2)
            {
                var centerX = dirX * cellSize * 0.42f + (alongHorizontal ? 0f : side * cellSize * 0.23f);
                var centerZ = dirY * cellSize * 0.42f + (alongHorizontal ? side * cellSize * 0.23f : 0f);
                var pinWidth = alongHorizontal ? 0.052f : 0.12f;
                var pinDepth = alongHorizontal ? 0.12f : 0.052f;
                AddRoadDetailMark("RoadCrosswalkCurbPin", material, pos, pinWidth, pinDepth, centerX, centerZ, roadTop + 0.014f);
            }
        }

        private void AddRoadIntersectionSignals(GridPos pos, float roadTop)
        {
            // REFERENCE_IMAGE_INTERSECTION_SIGNAL_POSTS adds tiny readable city details at busy junctions.
            AddRoadSignalPost(pos, -0.34f, -0.34f, roadTop);
            AddRoadSignalPost(pos, 0.34f, 0.34f, roadTop);
        }

        private void AddRoadSignalPost(GridPos pos, float offsetX, float offsetZ, float roadTop)
        {
            var baseCenter = CellCenter(pos, roadTop + 0.13f) + new Vector3(offsetX * cellSize, 0f, offsetZ * cellSize);
            AddLooseCube(roadObjects, "LowPolyTrafficSignalPost", serviceMaterial, baseCenter, new Vector3(0.045f, 0.26f, 0.045f));
            AddLooseCube(roadObjects, "LowPolyTrafficSignalLamp", trafficPulseMaterial, baseCenter + new Vector3(0f, 0.17f, 0f), new Vector3(0.11f, 0.08f, 0.11f));
            AddLooseCube(roadObjects, "LowPolyTrafficSignalGlow", windowMaterial, baseCenter + new Vector3(0f, 0.215f, 0f), new Vector3(0.07f, 0.035f, 0.07f));
            AddRoadSignalArms(baseCenter, offsetX, offsetZ);
        }

        private void AddRoadSignalArms(Vector3 baseCenter, float offsetX, float offsetZ)
        {
            var signX = offsetX < 0f ? 1f : -1f;
            var signZ = offsetZ < 0f ? 1f : -1f;
            AddLooseCube(roadObjects, "LowPolyTrafficSignalArm", serviceMaterial, baseCenter + new Vector3(signX * 0.075f, 0.205f, 0f), new Vector3(0.17f, 0.032f, 0.032f));
            AddLooseCube(roadObjects, "LowPolyTrafficSignalArm", serviceMaterial, baseCenter + new Vector3(0f, 0.205f, signZ * 0.075f), new Vector3(0.032f, 0.032f, 0.17f));
            AddLooseCube(roadObjects, "LowPolyTrafficSignalAmberLamp", serviceNeedMaterial, baseCenter + new Vector3(signX * 0.15f, 0.205f, 0f), new Vector3(0.055f, 0.045f, 0.04f));
            AddLooseCube(roadObjects, "LowPolyTrafficSignalWalkPlate", roadLineMaterial, baseCenter + new Vector3(0f, -0.115f, signZ * 0.035f), new Vector3(0.09f, 0.045f, 0.035f));
        }

        private void AddRoadDetailMark(string name, GridPos pos, float width, float depth, float offsetX, float offsetZ, float roadTop)
        {
            AddRoadDetailMark(name, roadLineMaterial, pos, width, depth, offsetX, offsetZ, roadTop);
        }

        private void AddRoadDetailMark(string name, Material material, GridPos pos, float width, float depth, float offsetX, float offsetZ, float roadTop)
        {
            var marker = CreateCube(name, material);
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = CellCenter(pos, roadTop + 0.018f) + new Vector3(offsetX, 0f, offsetZ);
            marker.transform.localScale = new Vector3(width, 0.012f, depth);
            roadObjects.Add(marker);
        }

        private void AddRoadCurbEdges(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float width, float roadTop)
        {
            // CITY_SKYLINE_ROAD_CURB_READABILITY separates roads from grass and planned parcels.
            // REFERENCE_IMAGE_CONCRETE_ROAD_CURBS keeps curbs distinct from painted lane lines.
            var curbMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var length = width * 0.74f;
            if (!hasDown) AddRoadDetailMark("RoadCurbEdge", curbMaterial, pos, length, 0.022f, 0f, -cellSize * 0.49f, roadTop);
            if (!hasUp) AddRoadDetailMark("RoadCurbEdge", curbMaterial, pos, length, 0.022f, 0f, cellSize * 0.49f, roadTop);
            if (!hasLeft) AddRoadDetailMark("RoadCurbEdge", curbMaterial, pos, 0.022f, length, -cellSize * 0.49f, 0f, roadTop);
            if (!hasRight) AddRoadDetailMark("RoadCurbEdge", curbMaterial, pos, 0.022f, length, cellSize * 0.49f, 0f, roadTop);
            AddRoadWaterfrontCues(pos, hasLeft, hasRight, hasDown, hasUp, width, roadTop);

            var connectionCount = RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp);
            AddRoadEdgeDepthShadows(pos, hasLeft, hasRight, hasDown, hasUp, width, roadTop, connectionCount);

            if (connectionCount <= 1)
            {
                if (hasLeft && !hasRight) AddRoadDetailMark("RoadTerminalCap", curbMaterial, pos, 0.032f, width * 0.42f, cellSize * 0.38f, 0f, roadTop);
                else if (hasRight && !hasLeft) AddRoadDetailMark("RoadTerminalCap", curbMaterial, pos, 0.032f, width * 0.42f, -cellSize * 0.38f, 0f, roadTop);
                else if (hasDown && !hasUp) AddRoadDetailMark("RoadTerminalCap", curbMaterial, pos, width * 0.42f, 0.032f, 0f, cellSize * 0.38f, roadTop);
                else AddRoadDetailMark("RoadTerminalCap", curbMaterial, pos, width * 0.42f, 0.032f, 0f, -cellSize * 0.38f, roadTop);
            }
        }

        private void AddRoadParcelAccessCues(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop, int connectionCount)
        {
            // CITY_SKYLINES_ROAD_PARCEL_ACCESS_CUES visually connect roads to zoned lots and building entrances.
            if (connectionCount >= 3)
            {
                AddRoadIntersectionParcelAccessCue(pos, hasLeft, hasRight, hasDown, hasUp, roadTop);
                return;
            }

            bool occupied;
            if (!hasDown && TryRoadsideParcelAccess(pos.X, pos.Y - 1, out occupied))
            {
                AddRoadParcelAccessMark(pos, true, -1f, roadTop, DecorationHash(pos.X, pos.Y - 1), occupied);
            }

            if (!hasUp && TryRoadsideParcelAccess(pos.X, pos.Y + 1, out occupied))
            {
                AddRoadParcelAccessMark(pos, true, 1f, roadTop, DecorationHash(pos.X, pos.Y + 1), occupied);
            }

            if (!hasLeft && TryRoadsideParcelAccess(pos.X - 1, pos.Y, out occupied))
            {
                AddRoadParcelAccessMark(pos, false, -1f, roadTop, DecorationHash(pos.X - 1, pos.Y), occupied);
            }

            if (!hasRight && TryRoadsideParcelAccess(pos.X + 1, pos.Y, out occupied))
            {
                AddRoadParcelAccessMark(pos, false, 1f, roadTop, DecorationHash(pos.X + 1, pos.Y), occupied);
            }
        }

        private void AddRoadIntersectionParcelAccessCue(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            bool found = false;
            bool bestHorizontal = true;
            bool bestOccupied = false;
            float bestSign = -1f;
            int bestSeed = 0;

            SelectRoadIntersectionParcelAccess(pos, !hasDown, pos.X, pos.Y - 1, true, -1f, ref found, ref bestHorizontal, ref bestSign, ref bestSeed, ref bestOccupied);
            SelectRoadIntersectionParcelAccess(pos, !hasUp, pos.X, pos.Y + 1, true, 1f, ref found, ref bestHorizontal, ref bestSign, ref bestSeed, ref bestOccupied);
            SelectRoadIntersectionParcelAccess(pos, !hasLeft, pos.X - 1, pos.Y, false, -1f, ref found, ref bestHorizontal, ref bestSign, ref bestSeed, ref bestOccupied);
            SelectRoadIntersectionParcelAccess(pos, !hasRight, pos.X + 1, pos.Y, false, 1f, ref found, ref bestHorizontal, ref bestSign, ref bestSeed, ref bestOccupied);

            if (found)
            {
                AddRoadParcelAccessMark(pos, bestHorizontal, bestSign, roadTop, bestSeed, bestOccupied, true);
            }
        }

        private void SelectRoadIntersectionParcelAccess(GridPos roadPos, bool openSide, int parcelX, int parcelY, bool horizontalEdge, float sign, ref bool found, ref bool bestHorizontal, ref float bestSign, ref int bestSeed, ref bool bestOccupied)
        {
            if (!openSide)
            {
                return;
            }

            bool occupied;
            if (!TryRoadsideParcelAccess(parcelX, parcelY, out occupied))
            {
                return;
            }

            if (found && (!occupied || bestOccupied))
            {
                return;
            }

            found = true;
            bestHorizontal = horizontalEdge;
            bestSign = sign;
            bestSeed = DecorationHash(parcelX, parcelY) ^ DecorationHash(roadPos.X, roadPos.Y);
            bestOccupied = occupied;
        }

        private bool TryRoadsideParcelAccess(int x, int y, out bool occupied)
        {
            occupied = false;
            var tile = controller != null ? controller.GetTile(x, y) : null;
            if (tile == null || tile.Terrain == TerrainType.Water || !string.IsNullOrEmpty(tile.RoadId))
            {
                return false;
            }

            occupied = !string.IsNullOrEmpty(tile.BuildingId);
            return occupied || tile.Zone != ZoneType.None;
        }

        private void AddRoadParcelAccessMark(GridPos pos, bool horizontalEdge, float sign, float roadTop, int seed, bool occupied, bool compact = false)
        {
            if (!compact && !occupied && seed % 2 != 0)
            {
                return;
            }

            var material = occupied ? roadLineMaterial : (shoreMaterial != null ? shoreMaterial : roadLineMaterial);
            var accent = occupied ? windowMaterial : serviceNeedMaterial;
            var along = (((seed >> 2) & 3) - 1.5f) * cellSize * 0.055f;
            var edge = sign * cellSize * 0.425f;
            var apronLong = compact ? cellSize * 0.2f : cellSize * 0.28f;
            var apronShort = compact ? 0.052f : 0.065f;
            var stripeLong = compact ? 0.11f : 0.15f;
            if (horizontalEdge)
            {
                AddRoadDetailMark(compact ? "RoadIntersectionAccessApron" : "RoadParcelAccessApron", material, pos, apronLong, apronShort, along, edge, roadTop + 0.018f);
                AddRoadDetailMark(compact ? "RoadIntersectionAccessStripe" : "RoadParcelAccessCrosswalk", roadLineMaterial, pos, 0.035f, stripeLong, along - cellSize * 0.07f, sign * cellSize * 0.315f, roadTop + 0.026f);
                if (!compact)
                {
                    AddRoadDetailMark("RoadParcelAccessCrosswalk", roadLineMaterial, pos, 0.035f, stripeLong, along + cellSize * 0.07f, sign * cellSize * 0.315f, roadTop + 0.026f);
                }

                if (occupied || compact)
                {
                    AddRoadDetailMark("RoadParcelAccessDoorLight", accent, pos, 0.075f, 0.045f, along, sign * cellSize * 0.49f, roadTop + 0.04f);
                }

                return;
            }

            AddRoadDetailMark(compact ? "RoadIntersectionAccessApron" : "RoadParcelAccessApron", material, pos, apronShort, apronLong, edge, along, roadTop + 0.018f);
            AddRoadDetailMark(compact ? "RoadIntersectionAccessStripe" : "RoadParcelAccessCrosswalk", roadLineMaterial, pos, stripeLong, 0.035f, sign * cellSize * 0.315f, along - cellSize * 0.07f, roadTop + 0.026f);
            if (!compact)
            {
                AddRoadDetailMark("RoadParcelAccessCrosswalk", roadLineMaterial, pos, stripeLong, 0.035f, sign * cellSize * 0.315f, along + cellSize * 0.07f, roadTop + 0.026f);
            }

            if (occupied || compact)
            {
                AddRoadDetailMark("RoadParcelAccessDoorLight", accent, pos, 0.045f, 0.075f, sign * cellSize * 0.49f, along, roadTop + 0.04f);
            }
        }

        private void AddRoadEdgeDepthShadows(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float width, float roadTop, int connectionCount)
        {
            // REFERENCE_IMAGE_ROAD_EDGE_DEPTH gives straight roads a toy-like curb lip without crowding junctions.
            if (connectionCount > 2)
            {
                return;
            }

            var shadowMaterial = buildingFootprintMaterial != null ? buildingFootprintMaterial : roadMaterial;
            var stepMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var length = width * 0.66f;
            if (!hasDown)
            {
                AddRoadDetailMark("RoadEdgeDepthShadow", shadowMaterial, pos, length, 0.026f, 0f, -cellSize * 0.535f, roadTop - 0.01f);
                AddRoadDetailMark("RoadSidewalkStep", stepMaterial, pos, length * 0.72f, 0.018f, 0f, -cellSize * 0.455f, roadTop + 0.006f);
            }

            if (!hasRight)
            {
                AddRoadDetailMark("RoadEdgeDepthShadow", shadowMaterial, pos, 0.026f, length, cellSize * 0.535f, 0f, roadTop - 0.01f);
                AddRoadDetailMark("RoadSidewalkStep", stepMaterial, pos, 0.018f, length * 0.72f, cellSize * 0.455f, 0f, roadTop + 0.006f);
            }

            if (!hasUp)
            {
                AddRoadDetailMark("RoadSunlitCurbLip", roadLineMaterial, pos, length * 0.58f, 0.014f, 0f, cellSize * 0.455f, roadTop + 0.014f);
            }

            if (!hasLeft)
            {
                AddRoadDetailMark("RoadSunlitCurbLip", roadLineMaterial, pos, 0.014f, length * 0.58f, -cellSize * 0.455f, 0f, roadTop + 0.014f);
            }
        }

        private void AddRoadWaterfrontCues(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float width, float roadTop)
        {
            // REFERENCE_IMAGE_WATERFRONT_ROAD_EDGE makes roads beside the river read as bridge/embankment edges.
            var railLength = width * 0.58f;
            if (!hasDown && IsWaterTile(pos.X, pos.Y - 1)) AddWaterfrontRail(pos, railLength, 0.034f, 0f, -cellSize * 0.42f, true, roadTop);
            if (!hasUp && IsWaterTile(pos.X, pos.Y + 1)) AddWaterfrontRail(pos, railLength, 0.034f, 0f, cellSize * 0.42f, true, roadTop);
            if (!hasLeft && IsWaterTile(pos.X - 1, pos.Y)) AddWaterfrontRail(pos, 0.034f, railLength, -cellSize * 0.42f, 0f, false, roadTop);
            if (!hasRight && IsWaterTile(pos.X + 1, pos.Y)) AddWaterfrontRail(pos, 0.034f, railLength, cellSize * 0.42f, 0f, false, roadTop);
        }

        private void AddWaterfrontRail(GridPos pos, float width, float depth, float offsetX, float offsetZ, bool horizontal, float roadTop)
        {
            AddRoadDetailMark("WaterfrontRoadCurb", roadLineMaterial, pos, width, depth, offsetX, offsetZ, roadTop);
            var postWidth = horizontal ? 0.05f : 0.034f;
            var postDepth = horizontal ? 0.034f : 0.05f;
            AddRoadDetailMark("WaterfrontRailPost", roadLineMaterial, pos, postWidth, postDepth, offsetX - (horizontal ? cellSize * 0.2f : 0f), offsetZ - (horizontal ? 0f : cellSize * 0.2f), roadTop + 0.018f);
            AddRoadDetailMark("WaterfrontRailPost", roadLineMaterial, pos, postWidth, postDepth, offsetX + (horizontal ? cellSize * 0.2f : 0f), offsetZ + (horizontal ? 0f : cellSize * 0.2f), roadTop + 0.018f);
        }

        private void AddRoadFlowChevrons(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINE_ROAD_FLOW_CHEVRONS gives busy corridors a Cities-style traffic read in normal view.
            var loadPercent = TrafficLoadPercent(road);
            if (road.Tier != RoadTier.Arterial && loadPercent < 55)
            {
                return;
            }

            var markerLength = road.Tier == RoadTier.Arterial ? 0.18f : 0.14f;
            var markerWidth = road.Tier == RoadTier.Arterial ? 0.028f : 0.022f;
            if (hasHorizontal || !hasVertical)
            {
                AddRoadChevronMark("RoadFlowChevron", road.Pos, markerLength, markerWidth, 0.12f, -0.055f, 32f, roadTop);
                AddRoadChevronMark("RoadFlowChevron", road.Pos, markerLength, markerWidth, 0.12f, 0.055f, -32f, roadTop);
            }

            if (hasVertical)
            {
                AddRoadChevronMark("RoadFlowChevron", road.Pos, markerLength, markerWidth, -0.055f, 0.12f, -58f, roadTop);
                AddRoadChevronMark("RoadFlowChevron", road.Pos, markerLength, markerWidth, 0.055f, 0.12f, 58f, roadTop);
            }
        }

        private void AddRoadChevronMark(string name, GridPos pos, float length, float width, float offsetX, float offsetZ, float rotationY, float roadTop)
        {
            var marker = CreateCube(name, roadLineMaterial);
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = CellCenter(pos, roadTop + 0.025f) + new Vector3(offsetX, 0f, offsetZ);
            marker.transform.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            marker.transform.localScale = new Vector3(length, 0.014f, width);
            roadObjects.Add(marker);
        }

        private void AddRoadDirectionArrowCue(RoadNode road, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINES_DIRECTION_ARROWS add legible lane intent without changing road simulation.
            var loadPercent = TrafficLoadPercent(road);
            var central = IsCentralRoadTile(road.Pos);
            if (road.Tier != RoadTier.Arterial && !central && loadPercent < 50)
            {
                return;
            }

            if (hasHorizontal && hasVertical)
            {
                return;
            }

            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            if (road.Tier != RoadTier.Arterial && loadPercent < 70 && hash % 3 == 1)
            {
                return;
            }

            if (hasHorizontal || !hasVertical)
            {
                var direction = hasRight || !hasLeft ? 1f : -1f;
                AddRoadDirectionArrow(road.Pos, true, direction, ((hash & 1) == 0 ? -0.18f : 0.18f) * cellSize, roadTop);
                return;
            }

            var verticalDirection = hasUp || !hasDown ? 1f : -1f;
            AddRoadDirectionArrow(road.Pos, false, verticalDirection, ((hash & 1) == 0 ? -0.18f : 0.18f) * cellSize, roadTop);
        }

        private void AddRoadDirectionArrow(GridPos pos, bool horizontal, float direction, float laneOffset, float roadTop)
        {
            var stemCenter = CellCenter(pos, roadTop + 0.036f) + (horizontal ? new Vector3(0f, 0f, laneOffset) : new Vector3(laneOffset, 0f, 0f));
            var headCenter = stemCenter + (horizontal ? new Vector3(direction * cellSize * 0.15f, 0f, 0f) : new Vector3(0f, 0f, direction * cellSize * 0.15f));
            var stemScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.014f, 0.032f)
                : new Vector3(0.032f, 0.014f, cellSize * 0.22f);
            AddLooseCube(roadObjects, "LowPolyRoadDirectionArrowStem", roadLineMaterial, stemCenter, stemScale);

            var headScale = new Vector3(cellSize * 0.13f, 0.014f, 0.028f);
            if (horizontal)
            {
                var yawA = direction > 0f ? 32f : 148f;
                var yawB = direction > 0f ? -32f : -148f;
                AddLooseCubeRotated(roadObjects, "LowPolyRoadDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(-direction * cellSize * 0.035f, 0f, cellSize * 0.035f), headScale, yawA);
                AddLooseCubeRotated(roadObjects, "LowPolyRoadDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(-direction * cellSize * 0.035f, 0f, -cellSize * 0.035f), headScale, yawB);
                return;
            }

            var verticalYawA = direction > 0f ? 58f : -58f;
            var verticalYawB = direction > 0f ? 122f : -122f;
            AddLooseCubeRotated(roadObjects, "LowPolyRoadDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(cellSize * 0.035f, 0f, -direction * cellSize * 0.035f), headScale, verticalYawA);
            AddLooseCubeRotated(roadObjects, "LowPolyRoadDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(-cellSize * 0.035f, 0f, -direction * cellSize * 0.035f), headScale, verticalYawB);
        }

        private void AddRoadRoutePointCues(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // CITY_SKYLINES_ROUTE_DOTS make transit and freight corridors legible in normal map view.
            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            var central = IsCentralRoadTile(road.Pos);
            var loadPercent = TrafficLoadPercent(road);
            var transitRoute = road.Tier == RoadTier.Arterial || (central && (loadPercent >= 38 || hash % 4 == 0));
            var freightRoute = IsFreightRouteRoad(road.Pos) && (road.Tier == RoadTier.Arterial || loadPercent >= 28 || hash % 3 == 0);
            if (!transitRoute && !freightRoute)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            if (transitRoute && (road.Tier == RoadTier.Arterial || hash % 3 != 1))
            {
                AddTransitRoutePointDots(road.Pos, horizontal, roadTop, hash);
            }

            if (freightRoute && hash % 3 != 2)
            {
                AddFreightRoutePointDots(road.Pos, horizontal, roadTop, hash);
            }
        }

        private void AddTransitRoutePointDots(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            var side = ((seed >> 3) & 1) == 0 ? -1f : 1f;
            var center = CellCenter(pos, roadTop + 0.142f) + (horizontal ? new Vector3(0f, 0f, side * cellSize * 0.27f) : new Vector3(side * cellSize * 0.27f, 0f, 0f));
            var bandScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.018f, 0.035f)
                : new Vector3(0.035f, 0.018f, cellSize * 0.34f);
            AddLooseCube(roadObjects, "LowPolyTransitRoutePointBand", windowMaterial, center, bandScale);

            var along = horizontal ? Vector3.right : Vector3.forward;
            for (var i = 0; i < 2; i += 1)
            {
                var offset = (i == 0 ? -1f : 1f) * cellSize * 0.13f;
                AddLooseCube(roadObjects, "LowPolyTransitRoutePointDot", commercialMaterial, center + along * offset + new Vector3(0f, 0.034f, 0f), new Vector3(0.07f, 0.04f, 0.07f));
            }
        }

        private void AddFreightRoutePointDots(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            var side = ((seed >> 4) & 1) == 0 ? 1f : -1f;
            var center = CellCenter(pos, roadTop + 0.146f) + (horizontal ? new Vector3(0f, 0f, side * cellSize * 0.31f) : new Vector3(side * cellSize * 0.31f, 0f, 0f));
            var along = horizontal ? Vector3.right : Vector3.forward;
            var linkScale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.018f, 0.03f)
                : new Vector3(0.03f, 0.018f, cellSize * 0.3f);
            AddLooseCube(roadObjects, "LowPolyFreightRouteLink", industrialMaterial, center, linkScale);
            AddLooseCube(roadObjects, "LowPolyFreightRouteCrate", serviceNeedMaterial, center - along * cellSize * 0.12f + new Vector3(0f, 0.045f, 0f), new Vector3(0.09f, 0.07f, 0.09f));
            AddLooseCube(roadObjects, "LowPolyFreightRouteCrate", industrialMaterial, center + along * cellSize * 0.1f + new Vector3(0f, 0.055f, 0f), new Vector3(0.11f, 0.085f, 0.1f));
        }

        private bool IsFreightRouteRoad(GridPos pos)
        {
            var roadTile = controller != null ? controller.GetTile(pos.X, pos.Y) : null;
            if (roadTile != null && roadTile.LogisticsAccess >= 42 && roadTile.Traffic >= 8)
            {
                return true;
            }

            return IsFreightRouteTile(pos.X - 1, pos.Y)
                || IsFreightRouteTile(pos.X + 1, pos.Y)
                || IsFreightRouteTile(pos.X, pos.Y - 1)
                || IsFreightRouteTile(pos.X, pos.Y + 1);
        }

        private bool IsFreightRouteTile(int x, int y)
        {
            var tile = controller != null ? controller.GetTile(x, y) : null;
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return false;
            }

            if (tile.Zone == ZoneType.Industrial || tile.Zone == ZoneType.Utility)
            {
                return true;
            }

            return !string.IsNullOrEmpty(tile.BuildingId) && tile.LogisticsAccess >= 34;
        }

        private void AddRoadTrafficCars(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop)
        {
            // LOW_POLY_TRAFFIC_CAR_MARKERS adds tiny city-life cars to the reference-style road grid.
            var loadPercent = TrafficLoadPercent(road);
            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            var central = IsCentralRoadTile(road.Pos);
            if (loadPercent < 32 && hash % (central ? 4 : 15) != 0)
            {
                return;
            }

            if (loadPercent < 58 && hash % (central ? 2 : 3) != 0)
            {
                return;
            }

            var horizontal = hasHorizontal || !hasVertical;
            var laneOffset = ((hash & 1) == 0 ? -0.18f : 0.18f) * cellSize;
            var alongOffset = (((hash >> 3) & 3) - 1.5f) * cellSize * 0.11f;
            var offset = horizontal
                ? new Vector3(alongOffset, 0f, laneOffset)
                : new Vector3(laneOffset, 0f, alongOffset);
            var center = CellCenter(road.Pos, roadTop + 0.075f) + offset;
            var bodyMaterial = loadPercent >= 80
                ? trafficPulseMaterial
                : (((hash >> 2) & 1) == 0 ? serviceNeedMaterial : commercialMaterial);
            AddRoadCarShadow(center, horizontal);
            AddRoadCarPart("LowPolyTrafficCarBody", bodyMaterial, center, horizontal, 0.26f, 0.13f, 0.07f);
            AddRoadCarPart("LowPolyTrafficCarCabin", windowMaterial, center + new Vector3(0f, 0.055f, 0f), horizontal, 0.12f, 0.1f, 0.045f);
            AddRoadCarDetails(center, horizontal, loadPercent >= 80);
            AddRoadTrafficTrail(center, horizontal, loadPercent >= 80, hash);
            AddRoadTrafficLaneLife(center, horizontal, loadPercent, hash);

            if (road.Tier == RoadTier.Arterial && loadPercent >= 72 && hash % 2 == 0)
            {
                var secondOffset = horizontal
                    ? new Vector3(-alongOffset * 0.65f, 0f, -laneOffset)
                    : new Vector3(-laneOffset, 0f, -alongOffset * 0.65f);
                var secondCenter = CellCenter(road.Pos, roadTop + 0.075f) + secondOffset;
                AddRoadCarShadow(secondCenter, horizontal);
                AddRoadCarPart("LowPolyTrafficCarBody", commercialMaterial, secondCenter, horizontal, 0.24f, 0.12f, 0.065f);
                AddRoadCarPart("LowPolyTrafficCarCabin", windowMaterial, secondCenter + new Vector3(0f, 0.052f, 0f), horizontal, 0.11f, 0.09f, 0.04f);
                AddRoadCarDetails(secondCenter, horizontal, true);
                AddRoadTrafficTrail(secondCenter, horizontal, true, hash + 17);
                AddRoadTrafficLaneLife(secondCenter, horizontal, loadPercent, hash + 17);
            }
        }

        private void AddRoadCarShadow(Vector3 center, bool horizontal)
        {
            var scale = horizontal
                ? new Vector3(0.32f, 0.014f, 0.16f)
                : new Vector3(0.16f, 0.014f, 0.32f);
            AddLooseCube(roadObjects, "LowPolyTrafficCarShadow", roadMaterial, center + new Vector3(0f, -0.045f, 0f), scale);
        }

        private void AddRoadCarPart(string name, Material material, Vector3 center, bool horizontal, float length, float width, float height)
        {
            var scale = horizontal
                ? new Vector3(length, height, width)
                : new Vector3(width, height, length);
            AddLooseCube(roadObjects, name, material, center, scale);
        }

        private void AddRoadCarDetails(Vector3 center, bool horizontal, bool hotTraffic)
        {
            // REFERENCE_IMAGE_TOYLIKE_TRAFFIC_CARS gives cars the chunky low-poly read from the target mockup.
            var wheelScale = horizontal
                ? new Vector3(0.055f, 0.035f, 0.035f)
                : new Vector3(0.035f, 0.035f, 0.055f);
            var headlightScale = horizontal
                ? new Vector3(0.035f, 0.026f, 0.09f)
                : new Vector3(0.09f, 0.026f, 0.035f);
            var front = horizontal ? new Vector3(0.15f, 0.006f, 0f) : new Vector3(0f, 0.006f, 0.15f);
            var side = horizontal ? new Vector3(0f, 0.002f, 0.075f) : new Vector3(0.075f, 0.002f, 0f);
            AddLooseCube(roadObjects, "LowPolyTrafficWheel", roadMaterial, center - front + side, wheelScale);
            AddLooseCube(roadObjects, "LowPolyTrafficWheel", roadMaterial, center - front - side, wheelScale);
            AddLooseCube(roadObjects, "LowPolyTrafficWheel", roadMaterial, center + front + side, wheelScale);
            AddLooseCube(roadObjects, "LowPolyTrafficWheel", roadMaterial, center + front - side, wheelScale);
            AddLooseCube(roadObjects, hotTraffic ? "LowPolyTrafficBrakeLight" : "LowPolyTrafficHeadlight", hotTraffic ? trafficPulseMaterial : roadLineMaterial, center + front + new Vector3(0f, 0.032f, 0f), headlightScale);
        }

        private void AddRoadTrafficTrail(Vector3 center, bool horizontal, bool hotTraffic, int hash)
        {
            // REFERENCE_IMAGE_TRAFFIC_FLOW_BREADCRUMBS makes roads feel active without changing simulation.
            var direction = ((hash >> 4) & 1) == 0 ? 1f : -1f;
            var material = hotTraffic ? trafficPulseMaterial : roadLineMaterial;
            var dashScale = horizontal
                ? new Vector3(0.12f, 0.012f, 0.026f)
                : new Vector3(0.026f, 0.012f, 0.12f);
            for (var i = 0; i < 3; i += 1)
            {
                var fade = 1f - i * 0.18f;
                var offset = cellSize * (0.19f + i * 0.12f) * direction;
                var dashCenter = horizontal
                    ? center + new Vector3(-offset, -0.052f, 0f)
                    : center + new Vector3(0f, -0.052f, -offset);
                AddLooseCube(roadObjects, "LowPolyTrafficFlowTrail", material, dashCenter, dashScale * fade);
            }
        }

        private void AddRoadTrafficLaneLife(Vector3 center, bool horizontal, int loadPercent, int seed)
        {
            // LOW_POLY_TRAFFIC_LANE_LIFE adds tail lights and queue beads without touching traffic data.
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var direction = ((seed >> 4) & 1) == 0 ? 1f : -1f;
            var tailScale = horizontal
                ? new Vector3(0.035f, 0.022f, 0.055f)
                : new Vector3(0.055f, 0.022f, 0.035f);
            var beadScale = new Vector3(0.055f, 0.026f, 0.055f);
            AddLooseCube(roadObjects, "LowPolyTrafficTailLightLeft", trafficPulseMaterial, center - tangent * direction * cellSize * 0.15f + normal * cellSize * 0.055f + new Vector3(0f, 0.035f, 0f), tailScale);
            AddLooseCube(roadObjects, "LowPolyTrafficTailLightRight", trafficPulseMaterial, center - tangent * direction * cellSize * 0.15f - normal * cellSize * 0.055f + new Vector3(0f, 0.035f, 0f), tailScale);

            if (loadPercent < 64)
            {
                return;
            }

            for (var i = 0; i < 2; i += 1)
            {
                var offset = cellSize * (0.24f + i * 0.13f) * -direction;
                var material = i == 0 && loadPercent >= 82 ? trafficPulseMaterial : roadLineMaterial;
                AddLooseCube(roadObjects, "LowPolyTrafficQueueBead", material, center + tangent * offset + new Vector3(0f, -0.035f, 0f), beadScale * (1f - i * 0.18f));
            }
        }

        private void AddFreshRoadPaintDetails(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop, int connectionCount)
        {
            // LOW_POLY_FRESH_ROAD_PAINT adds bright toy-city road details without touching traffic logic.
            if (connectionCount == 0)
            {
                return;
            }

            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            var horizontal = hasHorizontal || !hasVertical;
            if (connectionCount < 3)
            {
                AddRoadShoulderReflectors(road.Pos, horizontal, roadTop, hash);
                AddRoadSidewalkPaintPips(road.Pos, horizontal, roadTop, hash);
                AddRoadCleanEdgeGlints(road.Pos, horizontal, roadTop, hash, road.Tier == RoadTier.Arterial);
            }

            var central = IsCentralRoadTile(road.Pos);
            if ((central && hash % 5 == 0) || (road.Tier == RoadTier.Arterial && hash % 7 == 0))
            {
                AddRoadMicroVehicle(road.Pos, horizontal, roadTop, hash, road.Tier == RoadTier.Arterial);
            }
        }

        private void AddRoadsideMicroDecor(RoadNode road, bool hasHorizontal, bool hasVertical, float roadTop, int connectionCount)
        {
            // CITY_SKYLINES_ROADSIDE_MICRO_DECOR adds small visible life without changing road simulation.
            var hash = DecorationHash(road.Pos.X, road.Pos.Y);
            var central = IsCentralRoadTile(road.Pos);
            var horizontal = hasHorizontal || !hasVertical;
            if ((central && hash % 4 == 0) || (road.Tier == RoadTier.Arterial && hash % 5 == 0))
            {
                AddRoadsideLampRun(road.Pos, horizontal, roadTop, hash);
            }

            if ((central && hash % 6 == 0) || (road.Tier == RoadTier.Arterial && hash % 9 == 0))
            {
                AddRoadsideServiceVan(road.Pos, horizontal, roadTop, hash);
            }

            if (connectionCount <= 1 || (central && hash % 11 == 0))
            {
                AddRoadsideConstructionHint(road.Pos, horizontal, roadTop, hash);
            }

            if ((central && hash % 5 == 1) || (road.Tier == RoadTier.Arterial && hash % 7 == 2))
            {
                AddRoadsideWayfindingSign(road.Pos, horizontal, roadTop, hash);
            }

            if (connectionCount == 2 && hasHorizontal != hasVertical && ((central && hash % 6 == 2) || (road.Tier == RoadTier.Arterial && hash % 8 == 3)))
            {
                AddRoadsideBusStopCue(road.Pos, horizontal, roadTop, hash);
            }

            if ((central && hash % 7 == 3) || (road.Tier == RoadTier.Arterial && hash % 6 == 4))
            {
                AddRoadsideMobilityMarker(road.Pos, horizontal, roadTop, hash);
            }

            if ((central && hash % 4 == 2) || (road.Tier == RoadTier.Arterial && hash % 5 == 1) || (connectionCount >= 3 && hash % 3 == 0))
            {
                AddRoadsidePocketSignCluster(road, horizontal, roadTop, hash, connectionCount);
            }
        }

        private void AddRoadsideLampRun(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 2) & 1) == 0 ? -1f : 1f;
            var baseCenter = CellCenter(pos, roadTop + 0.11f) + normal * side * cellSize * 0.5f;
            for (var i = -1; i <= 1; i += 2)
            {
                var lampCenter = baseCenter + tangent * (i * cellSize * 0.22f);
                AddLooseCube(roadObjects, "LowPolyRoadsideLampPost", serviceMaterial, lampCenter + new Vector3(0f, 0.12f, 0f), new Vector3(0.034f, 0.24f, 0.034f));
                AddLooseCube(roadObjects, "LowPolyRoadsideLampGlow", windowMaterial, lampCenter + new Vector3(0f, 0.25f, 0f), new Vector3(0.105f, 0.04f, 0.105f));
            }
        }

        private void AddRoadsideServiceVan(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 4) & 1) == 0 ? 1f : -1f;
            var center = CellCenter(pos, roadTop + 0.08f)
                + normal * side * cellSize * 0.39f
                + tangent * ((((seed >> 6) & 3) - 1.5f) * cellSize * 0.08f);
            var bodyScale = horizontal
                ? new Vector3(cellSize * 0.27f, 0.11f, cellSize * 0.12f)
                : new Vector3(cellSize * 0.12f, 0.11f, cellSize * 0.27f);
            var cabinScale = horizontal
                ? new Vector3(cellSize * 0.1f, 0.075f, cellSize * 0.1f)
                : new Vector3(cellSize * 0.1f, 0.075f, cellSize * 0.1f);
            var stripeScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.026f, cellSize * 0.024f)
                : new Vector3(cellSize * 0.024f, 0.026f, cellSize * 0.2f);
            AddLooseCube(roadObjects, "LowPolyRoadsideServiceVanShadow", roadMaterial, center + new Vector3(0f, -0.055f, 0f), bodyScale * 1.12f);
            AddLooseCube(roadObjects, "LowPolyRoadsideServiceVanBody", utilityMaterial, center, bodyScale);
            AddLooseCube(roadObjects, "LowPolyRoadsideServiceVanCabin", windowMaterial, center + new Vector3(0f, 0.06f, 0f) - tangent * cellSize * 0.07f, cabinScale);
            AddLooseCube(roadObjects, "LowPolyRoadsideServiceVanStripe", roadLineMaterial, center + new Vector3(0f, 0.035f, 0f) + normal * side * cellSize * 0.018f, stripeScale);
        }

        private void AddRoadsideConstructionHint(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 3) & 1) == 0 ? -1f : 1f;
            var center = CellCenter(pos, roadTop + 0.09f) + normal * side * cellSize * 0.47f + tangent * cellSize * 0.16f;
            var boardScale = horizontal
                ? new Vector3(cellSize * 0.24f, 0.09f, cellSize * 0.04f)
                : new Vector3(cellSize * 0.04f, 0.09f, cellSize * 0.24f);
            var barScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.024f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.024f, cellSize * 0.16f);
            AddLooseCube(roadObjects, "LowPolyRoadsideConstructionConeBase", roadLineMaterial, center - tangent * cellSize * 0.18f, new Vector3(0.11f, 0.03f, 0.11f));
            AddLooseCube(roadObjects, "LowPolyRoadsideConstructionConeBody", serviceNeedMaterial, center - tangent * cellSize * 0.18f + new Vector3(0f, 0.07f, 0f), new Vector3(0.07f, 0.12f, 0.07f));
            AddLooseCube(roadObjects, "LowPolyRoadsideConstructionBoard", serviceNeedMaterial, center + new Vector3(0f, 0.11f, 0f), boardScale);
            AddLooseCube(roadObjects, "LowPolyRoadsideConstructionBoardStripe", roadLineMaterial, center + new Vector3(0f, 0.13f, 0f), barScale);
        }

        private void AddRoadsideWayfindingSign(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            // LOW_POLY_ROADSIDE_WAYFINDING adds tiny signs and flower beds around key roads.
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 3) & 1) == 0 ? -1f : 1f;
            var center = CellCenter(pos, roadTop + 0.1f)
                + normal * side * cellSize * 0.48f
                + tangent * ((((seed >> 5) & 3) - 1.5f) * cellSize * 0.07f);
            var signScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.07f, 0.035f)
                : new Vector3(0.035f, 0.07f, cellSize * 0.2f);
            var arrowScale = horizontal
                ? new Vector3(cellSize * 0.11f, 0.028f, 0.032f)
                : new Vector3(0.032f, 0.028f, cellSize * 0.11f);
            var flowerScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.035f, cellSize * 0.07f)
                : new Vector3(cellSize * 0.07f, 0.035f, cellSize * 0.16f);
            AddLooseCube(roadObjects, "LowPolyRoadsideWayfindingPost", serviceMaterial, center + new Vector3(0f, 0.12f, 0f), new Vector3(0.034f, 0.24f, 0.034f));
            AddLooseCube(roadObjects, "LowPolyRoadsideWayfindingPlate", roadLineMaterial, center + new Vector3(0f, 0.255f, 0f), signScale);
            AddLooseCube(roadObjects, "LowPolyRoadsideWayfindingArrow", windowMaterial, center + new Vector3(0f, 0.302f, 0f) + tangent * side * cellSize * 0.035f, arrowScale);
            AddLooseCube(roadObjects, "LowPolyRoadsideSignFlowerBed", serviceNeedMaterial, center - normal * side * cellSize * 0.1f + new Vector3(0f, -0.035f, 0f), flowerScale);
        }

        private void AddRoadsideBusStopCue(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            // CITY_SKYLINES_TRANSIT_STOP_CUE gives straight road corridors a readable bus stop without changing transit data.
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 4) & 1) == 0 ? 1f : -1f;
            var along = (((seed >> 6) & 3) - 1.5f) * cellSize * 0.055f;
            var center = CellCenter(pos, roadTop + 0.075f) + normal * side * cellSize * 0.49f + tangent * along;
            var platformScale = horizontal
                ? new Vector3(cellSize * 0.42f, 0.026f, cellSize * 0.12f)
                : new Vector3(cellSize * 0.12f, 0.026f, cellSize * 0.42f);
            var curbStripeScale = horizontal
                ? new Vector3(cellSize * 0.32f, 0.018f, cellSize * 0.03f)
                : new Vector3(cellSize * 0.03f, 0.018f, cellSize * 0.32f);
            var benchScale = horizontal
                ? new Vector3(cellSize * 0.23f, 0.055f, cellSize * 0.04f)
                : new Vector3(cellSize * 0.04f, 0.055f, cellSize * 0.23f);
            var canopyScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.045f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.045f, cellSize * 0.34f);
            var signScale = horizontal
                ? new Vector3(cellSize * 0.13f, 0.08f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.08f, cellSize * 0.13f);
            AddLooseCube(roadObjects, "LowPolyBusStopPlatform", shoreMaterial != null ? shoreMaterial : roadLineMaterial, center, platformScale);
            AddLooseCube(roadObjects, "LowPolyBusStopCurbStripe", roadLineMaterial, center - normal * side * cellSize * 0.09f + new Vector3(0f, 0.022f, 0f), curbStripeScale);
            AddLooseCube(roadObjects, "LowPolyBusStopBench", serviceMaterial, center + normal * side * cellSize * 0.025f + new Vector3(0f, 0.055f, 0f), benchScale);
            AddLooseCube(roadObjects, "LowPolyBusStopShelterPost", windowMaterial, center + tangent * cellSize * 0.16f + new Vector3(0f, 0.12f, 0f), new Vector3(0.03f, 0.22f, 0.03f));
            AddLooseCube(roadObjects, "LowPolyBusStopShelterPost", windowMaterial, center - tangent * cellSize * 0.16f + new Vector3(0f, 0.12f, 0f), new Vector3(0.03f, 0.22f, 0.03f));
            AddLooseCube(roadObjects, "LowPolyBusStopCanopy", commercialMaterial, center + normal * side * cellSize * 0.025f + new Vector3(0f, 0.245f, 0f), canopyScale);
            AddLooseCube(roadObjects, "LowPolyBusStopSignPost", roadLineMaterial, center - tangent * cellSize * 0.25f + new Vector3(0f, 0.14f, 0f), new Vector3(0.03f, 0.28f, 0.03f));
            AddLooseCube(roadObjects, "LowPolyBusStopSignPlate", commercialMaterial, center - tangent * cellSize * 0.25f + new Vector3(0f, 0.31f, 0f), signScale);
            AddLooseCube(roadObjects, "LowPolyBusStopSignDot", roadLineMaterial, center - tangent * cellSize * 0.25f + new Vector3(0f, 0.315f, 0f), new Vector3(0.045f, 0.028f, 0.045f));
        }

        private void AddRoadsideMobilityMarker(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            // CITY_SKYLINES_ROADSIDE_MOBILITY_MARKERS add parking/transit wayfinding without changing city data.
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 4) & 1) == 0 ? -1f : 1f;
            var center = CellCenter(pos, roadTop + 0.1f)
                + normal * side * cellSize * 0.5f
                + tangent * ((((seed >> 6) & 3) - 1.5f) * cellSize * 0.075f);
            var plateScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.12f, 0.035f)
                : new Vector3(0.035f, 0.12f, cellSize * 0.16f);
            var bayScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.018f, 0.035f)
                : new Vector3(0.035f, 0.018f, cellSize * 0.34f);
            var curbScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.016f, 0.026f)
                : new Vector3(0.026f, 0.016f, cellSize * 0.22f);
            var plateCenter = center + new Vector3(0f, 0.26f, 0f);
            var isParking = (seed & 2) == 0;
            AddLooseCube(roadObjects, "LowPolyMobilitySignPost", serviceMaterial, center + new Vector3(0f, 0.13f, 0f), new Vector3(0.032f, 0.26f, 0.032f));
            AddLooseCube(roadObjects, isParking ? "LowPolyParkingPPlate" : "LowPolyTransitRoutePlate", isParking ? windowMaterial : commercialMaterial, plateCenter, plateScale);
            AddRoadsideMobilityGlyph(plateCenter + new Vector3(0f, 0.006f, 0f), horizontal, isParking);
            AddLooseCube(roadObjects, "LowPolyMobilityCurbBay", roadLineMaterial, center - normal * side * cellSize * 0.14f + new Vector3(0f, -0.045f, 0f), bayScale);
            AddLooseCube(roadObjects, "LowPolyMobilityBayEndCap", windowMaterial, center - normal * side * cellSize * 0.14f + tangent * cellSize * 0.2f + new Vector3(0f, -0.035f, 0f), curbScale);
            AddLooseCube(roadObjects, "LowPolyMobilityBayEndCap", windowMaterial, center - normal * side * cellSize * 0.14f - tangent * cellSize * 0.2f + new Vector3(0f, -0.035f, 0f), curbScale);
            if (!isParking)
            {
                AddLooseCube(roadObjects, "LowPolyTransitRouteDot", roadLineMaterial, center + tangent * cellSize * 0.16f + new Vector3(0f, 0.02f, 0f), new Vector3(0.052f, 0.035f, 0.052f));
            }
        }

        private void AddRoadsideMobilityGlyph(Vector3 plateCenter, bool horizontal, bool parking)
        {
            var markMaterial = parking ? roadMaterial : roadLineMaterial;
            if (parking)
            {
                var stemScale = horizontal
                    ? new Vector3(0.032f, 0.082f, 0.038f)
                    : new Vector3(0.038f, 0.082f, 0.032f);
                var loopScale = horizontal
                    ? new Vector3(0.09f, 0.026f, 0.038f)
                    : new Vector3(0.038f, 0.026f, 0.09f);
                var side = horizontal ? Vector3.right : Vector3.forward;
                AddLooseCube(roadObjects, "LowPolyParkingPStem", markMaterial, plateCenter + new Vector3(0f, 0.018f, 0f) - side * cellSize * 0.022f, stemScale);
                AddLooseCube(roadObjects, "LowPolyParkingPTop", markMaterial, plateCenter + new Vector3(0f, 0.046f, 0f) + side * cellSize * 0.018f, loopScale);
                AddLooseCube(roadObjects, "LowPolyParkingPMid", markMaterial, plateCenter + new Vector3(0f, 0.014f, 0f) + side * cellSize * 0.014f, loopScale * 0.86f);
                return;
            }

            var routeScale = horizontal
                ? new Vector3(cellSize * 0.09f, 0.026f, 0.035f)
                : new Vector3(0.035f, 0.026f, cellSize * 0.09f);
            var crossScale = horizontal
                ? new Vector3(0.035f, 0.026f, cellSize * 0.08f)
                : new Vector3(cellSize * 0.08f, 0.026f, 0.035f);
            AddLooseCube(roadObjects, "LowPolyTransitRouteGlyph", markMaterial, plateCenter + new Vector3(0f, 0.038f, 0f), routeScale);
            AddLooseCube(roadObjects, "LowPolyTransitRouteGlyph", markMaterial, plateCenter + new Vector3(0f, -0.012f, 0f), routeScale);
            AddLooseCube(roadObjects, "LowPolyTransitRouteCross", markMaterial, plateCenter + new Vector3(0f, 0.014f, 0f), crossScale);
        }

        private void AddRoadsidePocketSignCluster(RoadNode road, bool horizontal, float roadTop, int seed, int connectionCount)
        {
            // REFERENCE_IMAGE_ROADSIDE_POCKET_SIGNS adds readable street-side guide boards, planters, and route tabs.
            if (connectionCount == 0)
            {
                return;
            }

            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var side = ((seed >> 5) & 1) == 0 ? -1f : 1f;
            var along = (((seed >> 7) & 3) - 1.5f) * cellSize * 0.075f;
            var center = CellCenter(road.Pos, roadTop + 0.105f)
                + normal * side * cellSize * (connectionCount >= 3 ? 0.44f : 0.5f)
                + tangent * along;
            var padScale = horizontal
                ? new Vector3(cellSize * 0.46f, 0.024f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.024f, cellSize * 0.46f);
            var boardScale = horizontal
                ? new Vector3(cellSize * 0.24f, 0.12f, 0.036f)
                : new Vector3(0.036f, 0.12f, cellSize * 0.24f);
            var tabScale = horizontal
                ? new Vector3(cellSize * 0.105f, 0.032f, 0.028f)
                : new Vector3(0.028f, 0.032f, cellSize * 0.105f);
            var flowerScale = horizontal
                ? new Vector3(cellSize * 0.14f, 0.038f, cellSize * 0.062f)
                : new Vector3(cellSize * 0.062f, 0.038f, cellSize * 0.14f);

            AddLooseCube(roadObjects, "LowPolyRoadsidePocketPad", shoreMaterial != null ? shoreMaterial : roadLineMaterial, center + new Vector3(0f, -0.055f, 0f), padScale);
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketSignPost", serviceMaterial, center + new Vector3(0f, 0.09f, 0f), new Vector3(0.034f, 0.24f, 0.034f));
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketGuideBoard", windowMaterial, center + new Vector3(0f, 0.245f, 0f), boardScale);
            AddRoadsidePocketSignTabs(center + new Vector3(0f, 0.285f, 0f), tangent, tabScale, road.Tier == RoadTier.Arterial);

            AddLooseCube(roadObjects, "LowPolyRoadsidePocketFlowerBox", serviceNeedMaterial, center - tangent * cellSize * 0.18f - normal * side * cellSize * 0.045f + new Vector3(0f, -0.02f, 0f), flowerScale);
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketShrub", treeCanopyMaterial, center + tangent * cellSize * 0.19f + new Vector3(0f, 0.02f, 0f), new Vector3(cellSize * 0.095f, 0.085f, cellSize * 0.095f));
            AddRoadsidePocketGroundArrow(center, horizontal, tangent, normal, side);
        }

        private void AddRoadsidePocketSignTabs(Vector3 boardCenter, Vector3 tangent, Vector3 tabScale, bool arterial)
        {
            var firstMaterial = arterial ? commercialMaterial : roadLineMaterial;
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketRouteTab", firstMaterial, boardCenter - tangent * cellSize * 0.055f, tabScale);
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketRouteTab", serviceNeedMaterial, boardCenter + tangent * cellSize * 0.055f + new Vector3(0f, -0.038f, 0f), tabScale * 0.78f);
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketRoutePip", roadMaterial, boardCenter + new Vector3(0f, 0.046f, 0f), new Vector3(cellSize * 0.045f, 0.024f, cellSize * 0.045f));
        }

        private void AddRoadsidePocketGroundArrow(Vector3 center, bool horizontal, Vector3 tangent, Vector3 normal, float side)
        {
            var arrowCenter = center - normal * side * cellSize * 0.12f + new Vector3(0f, -0.04f, 0f);
            var stemScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.016f, 0.026f)
                : new Vector3(0.026f, 0.016f, cellSize * 0.18f);
            var headScale = new Vector3(cellSize * 0.09f, 0.016f, 0.026f);
            var direction = side > 0f ? -1f : 1f;
            AddLooseCube(roadObjects, "LowPolyRoadsidePocketArrowStem", roadLineMaterial, arrowCenter, stemScale);
            AddLooseCubeRotated(roadObjects, "LowPolyRoadsidePocketArrowHead", roadLineMaterial, arrowCenter + tangent * direction * cellSize * 0.1f + normal * cellSize * 0.032f, headScale, horizontal ? (direction > 0f ? 35f : 145f) : (direction > 0f ? 58f : -58f));
            AddLooseCubeRotated(roadObjects, "LowPolyRoadsidePocketArrowHead", roadLineMaterial, arrowCenter + tangent * direction * cellSize * 0.1f - normal * cellSize * 0.032f, headScale, horizontal ? (direction > 0f ? -35f : -145f) : (direction > 0f ? 122f : -122f));
        }

        private void AddRoadShoulderReflectors(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 2) & 1) == 0 ? -1f : 1f;
            var baseCenter = CellCenter(pos, roadTop + 0.07f) + normal * side * cellSize * 0.36f;
            var reflectorScale = new Vector3(cellSize * 0.07f, 0.024f, cellSize * 0.045f);
            if (horizontal)
            {
                reflectorScale = new Vector3(cellSize * 0.09f, 0.024f, cellSize * 0.035f);
            }

            for (var i = -1; i <= 1; i += 1)
            {
                var material = i == 0 ? windowMaterial : roadLineMaterial;
                AddLooseCube(roadObjects, "LowPolyFreshRoadReflector", material, baseCenter + tangent * (i * cellSize * 0.19f), reflectorScale);
            }
        }

        private void AddRoadCleanEdgeGlints(GridPos pos, bool horizontal, float roadTop, int seed, bool arterial)
        {
            // REFERENCE_IMAGE_CLEAN_ROAD_EDGE_GLINTS keeps dark asphalt feeling fresh and readable.
            if (!arterial && seed % 2 != 0)
            {
                return;
            }

            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var length = arterial ? cellSize * 0.5f : cellSize * 0.36f;
            var sideDistance = arterial ? cellSize * 0.36f : cellSize * 0.31f;
            var along = (((seed >> 5) & 3) - 1.5f) * cellSize * 0.055f;
            var glintScale = horizontal
                ? new Vector3(length, 0.012f, 0.018f)
                : new Vector3(0.018f, 0.012f, length);
            var capScale = horizontal
                ? new Vector3(cellSize * 0.11f, 0.014f, 0.022f)
                : new Vector3(0.022f, 0.014f, cellSize * 0.11f);
            var center = CellCenter(pos, roadTop + 0.07f) + tangent * along;
            AddLooseCube(roadObjects, "LowPolyCleanRoadEdgeGlint", windowMaterial, center + normal * sideDistance, glintScale);
            AddLooseCube(roadObjects, "LowPolyCleanRoadEdgeGlint", roadLineMaterial, center - normal * sideDistance + tangent * cellSize * 0.12f, glintScale * 0.68f);
            AddLooseCube(roadObjects, "LowPolyCleanRoadEdgeCap", roadLineMaterial, center + normal * sideDistance - tangent * cellSize * 0.28f + new Vector3(0f, 0.01f, 0f), capScale);
        }

        private void AddRoadSidewalkPaintPips(GridPos pos, bool horizontal, float roadTop, int seed)
        {
            if (seed % 3 == 1)
            {
                return;
            }

            var normal = horizontal ? Vector3.forward : Vector3.right;
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var side = ((seed >> 4) & 1) == 0 ? 1f : -1f;
            var center = CellCenter(pos, roadTop + 0.064f) + normal * side * cellSize * 0.48f + tangent * (((seed >> 5) & 3) - 1.5f) * cellSize * 0.08f;
            var pipScale = horizontal
                ? new Vector3(cellSize * 0.13f, 0.026f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.026f, cellSize * 0.13f);
            AddLooseCube(roadObjects, "LowPolyFreshCurbPaintPip", serviceNeedMaterial, center, pipScale);
            AddLooseCube(roadObjects, "LowPolyFreshCurbWhiteCap", roadLineMaterial, center - tangent * cellSize * 0.1f + new Vector3(0f, 0.018f, 0f), pipScale * 0.72f);
        }

        private void AddRoadMicroVehicle(GridPos pos, bool horizontal, float roadTop, int seed, bool arterial)
        {
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var laneSide = ((seed >> 1) & 1) == 0 ? -1f : 1f;
            var center = CellCenter(pos, roadTop + 0.084f)
                + normal * laneSide * cellSize * (arterial ? 0.27f : 0.22f)
                + tangent * ((((seed >> 5) & 3) - 1.5f) * cellSize * 0.09f);
            var bodyMaterial = (seed & 8) == 0 ? mixedUseMaterial : serviceNeedMaterial;
            var bodyScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.075f, cellSize * 0.085f)
                : new Vector3(cellSize * 0.085f, 0.075f, cellSize * 0.2f);
            var cabinScale = horizontal
                ? new Vector3(cellSize * 0.08f, 0.055f, cellSize * 0.06f)
                : new Vector3(cellSize * 0.06f, 0.055f, cellSize * 0.08f);
            var lightScale = horizontal
                ? new Vector3(cellSize * 0.035f, 0.022f, cellSize * 0.075f)
                : new Vector3(cellSize * 0.075f, 0.022f, cellSize * 0.035f);
            AddLooseCube(roadObjects, "LowPolyMicroCarShadow", roadMaterial, center + new Vector3(0f, -0.052f, 0f), bodyScale * 1.14f);
            AddLooseCube(roadObjects, "LowPolyMicroCarBody", bodyMaterial, center, bodyScale);
            AddLooseCube(roadObjects, "LowPolyMicroCarCabin", windowMaterial, center + new Vector3(0f, 0.052f, 0f), cabinScale);
            AddLooseCube(roadObjects, "LowPolyMicroCarHeadlight", roadLineMaterial, center + tangent * laneSide * cellSize * 0.105f + new Vector3(0f, 0.035f, 0f), lightScale);
        }

        private void AddIntersectionCivicLife(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop)
        {
            // LOW_POLY_INTERSECTION_CIVIC_LIFE sharpens crossings with waiting pedestrians and survey-bright corners.
            var seed = DecorationHash(pos.X, pos.Y);
            if (hasLeft) AddIntersectionCrossingAccent(pos, -1f, 0f, roadTop, seed);
            if (hasRight) AddIntersectionCrossingAccent(pos, 1f, 0f, roadTop, seed >> 1);
            if (hasDown) AddIntersectionCrossingAccent(pos, 0f, -1f, roadTop, seed >> 2);
            if (hasUp) AddIntersectionCrossingAccent(pos, 0f, 1f, roadTop, seed >> 3);
            AddIntersectionWaitingPedestrians(pos, hasLeft, hasRight, hasDown, hasUp, roadTop, seed);
        }

        private void AddIntersectionCrossingAccent(GridPos pos, float dirX, float dirZ, float roadTop, int seed)
        {
            if (seed % 2 != 0)
            {
                return;
            }

            var horizontalApproach = Mathf.Abs(dirX) > 0.01f;
            var stopCenter = CellCenter(pos, roadTop + 0.052f) + new Vector3(dirX * cellSize * 0.39f, 0f, dirZ * cellSize * 0.39f);
            var tangent = horizontalApproach ? Vector3.forward : Vector3.right;
            var tickScale = horizontalApproach
                ? new Vector3(cellSize * 0.065f, 0.022f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.022f, cellSize * 0.065f);
            AddLooseCube(roadObjects, "LowPolyFreshCrosswalkCornerTile", shoreMaterial != null ? shoreMaterial : roadLineMaterial, stopCenter + tangent * cellSize * 0.18f, tickScale);
            AddLooseCube(roadObjects, "LowPolyFreshCrosswalkCornerTile", windowMaterial, stopCenter - tangent * cellSize * 0.18f + new Vector3(0f, 0.012f, 0f), tickScale * 0.72f);
        }

        private void AddIntersectionWaitingPedestrians(GridPos pos, bool hasLeft, bool hasRight, bool hasDown, bool hasUp, float roadTop, int seed)
        {
            var added = 0;
            if (hasLeft && hasDown) added += AddIntersectionPedestrian(pos, -1f, -1f, roadTop, seed + added * 11);
            if (hasRight && hasUp) added += AddIntersectionPedestrian(pos, 1f, 1f, roadTop, seed + added * 11);
            if (added < 2 && hasLeft && hasUp) added += AddIntersectionPedestrian(pos, -1f, 1f, roadTop, seed + added * 11);
            if (added < 2 && hasRight && hasDown) AddIntersectionPedestrian(pos, 1f, -1f, roadTop, seed + added * 11);
        }

        private int AddIntersectionPedestrian(GridPos pos, float signX, float signZ, float roadTop, int seed)
        {
            if (seed % 3 == 1)
            {
                return 0;
            }

            var center = CellCenter(pos, roadTop + 0.12f) + new Vector3(signX * cellSize * 0.37f, 0f, signZ * cellSize * 0.37f);
            var bodyMaterial = (seed & 2) == 0 ? commercialMaterial : serviceNeedMaterial;
            AddLooseCube(roadObjects, "LowPolyCrossingPersonShadow", roadMaterial, center + new Vector3(0f, -0.08f, 0f), new Vector3(0.1f, 0.012f, 0.1f));
            AddLooseCube(roadObjects, "LowPolyCrossingPersonBody", bodyMaterial, center + new Vector3(0f, 0.035f, 0f), new Vector3(0.055f, 0.13f, 0.055f));
            AddLooseCube(roadObjects, "LowPolyCrossingPersonHead", roofMaterial, center + new Vector3(0f, 0.13f, 0f), new Vector3(0.068f, 0.055f, 0.068f));
            return 1;
        }


        private static int RoadConnectionCount(bool hasLeft, bool hasRight, bool hasDown, bool hasUp)
        {
            var count = 0;
            if (hasLeft) count += 1;
            if (hasRight) count += 1;
            if (hasDown) count += 1;
            if (hasUp) count += 1;
            return count;
        }

        private static bool HasRoadAt(IReadOnlyList<RoadNode> roads, int x, int y)
        {
            if (roads == null)
            {
                return false;
            }

            for (var i = 0; i < roads.Count; i += 1)
            {
                if (roads[i].Pos.X == x && roads[i].Pos.Y == y)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddRoadPreviewCells(GridPos from, GridPos to, string name)
        {
            var stepX = from.X <= to.X ? 1 : -1;
            var stepY = from.Y <= to.Y ? 1 : -1;
            for (var x = from.X; x != to.X + stepX; x += stepX)
            {
                AddRoadPreviewCell(new GridPos(x, from.Y), true, name);
            }

            for (var y = from.Y + stepY; y != to.Y + stepY; y += stepY)
            {
                AddRoadPreviewCell(new GridPos(to.X, y), false, name);
            }
        }

        private void AddRoadPreviewCell(GridPos pos, bool horizontal, string name)
        {
            // CITY_SKYLINES_ROAD_PREVIEW_EXISTING_SEGMENTS separates new spend from existing connections.
            var center = CellCenter(pos, roadHeight + 0.08f);
            var tile = controller != null ? controller.GetTile(pos.X, pos.Y) : null;
            var existingRoad = HasRoadTile(pos.X, pos.Y);
            if (existingRoad)
            {
                var connectorScale = horizontal
                    ? new Vector3(cellSize * 0.58f, 0.045f, 0.055f)
                    : new Vector3(0.055f, 0.045f, cellSize * 0.58f);
                var tickScale = horizontal
                    ? new Vector3(0.05f, 0.04f, cellSize * 0.22f)
                    : new Vector3(cellSize * 0.22f, 0.04f, 0.05f);
                AddLooseCube(placementPreviewObjects, name + "ExistingConnector", roadLineMaterial, center + new Vector3(0f, 0.012f, 0f), connectorScale);
                AddLooseCube(placementPreviewObjects, name + "ExistingEndpointTick", windowMaterial, center + new Vector3(0f, 0.044f, 0f), tickScale);
                return;
            }

            if (RoadPreviewCellBlocked(tile))
            {
                AddRoadPreviewBlockedCell(pos, center, horizontal, tile, name);
                return;
            }

            var ghostScale = horizontal
                ? new Vector3(cellSize * 0.72f, 0.055f, cellSize * 0.5f)
                : new Vector3(cellSize * 0.5f, 0.055f, cellSize * 0.72f);
            AddLooseCube(placementPreviewObjects, name, previewOkMaterial, center, ghostScale);
            if (RoadPreviewTouchesWater(pos))
            {
                AddRoadPreviewWaterfrontCue(center, horizontal);
            }

            AddPlacementCornerGuides(center, horizontal ? cellSize * 0.76f : cellSize * 0.56f, horizontal ? cellSize * 0.56f : cellSize * 0.76f, previewOkMaterial, name + "CornerGuide");
        }

        private static bool RoadPreviewCellBlocked(TileData tile)
        {
            return tile == null || tile.Terrain == TerrainType.Water || !string.IsNullOrEmpty(tile.BuildingId);
        }

        private void AddRoadPreviewBlockedCell(GridPos pos, Vector3 center, bool horizontal, TileData tile, string name)
        {
            // CITY_SKYLINES_ROAD_PREVIEW_CELL_BLOCKERS marks the exact tile that rejects a road drag.
            AddLooseCube(placementPreviewObjects, name + "BlockedPad", previewBlockedMaterial, center, new Vector3(cellSize * 0.64f, 0.055f, cellSize * 0.64f));
            AddLooseCubeRotated(placementPreviewObjects, name + "BlockedX", previewBlockedMaterial, center + new Vector3(0f, 0.06f, 0f), new Vector3(cellSize * 0.54f, 0.035f, 0.06f), 45f);
            AddLooseCubeRotated(placementPreviewObjects, name + "BlockedX", previewBlockedMaterial, center + new Vector3(0f, 0.064f, 0f), new Vector3(cellSize * 0.54f, 0.035f, 0.06f), -45f);

            if (tile != null && tile.Terrain == TerrainType.Water)
            {
                AddRoadPreviewWaterBlocker(center, horizontal);
                return;
            }

            AddRoadPreviewOccupiedBlocker(center, horizontal);
        }

        private void AddRoadPreviewWaterBlocker(Vector3 center, bool horizontal)
        {
            var rippleScale = horizontal
                ? new Vector3(cellSize * 0.44f, 0.022f, 0.045f)
                : new Vector3(0.045f, 0.022f, cellSize * 0.44f);
            AddLooseCube(placementPreviewObjects, "RoadPreviewWaterRipple", windowMaterial, center + new Vector3(0f, 0.09f, -cellSize * 0.14f), rippleScale);
            AddLooseCube(placementPreviewObjects, "RoadPreviewWaterRipple", windowMaterial, center + new Vector3(0f, 0.095f, cellSize * 0.14f), rippleScale);
        }

        private void AddRoadPreviewOccupiedBlocker(Vector3 center, bool horizontal)
        {
            var postScale = new Vector3(0.055f, 0.22f, 0.055f);
            AddLooseCube(placementPreviewObjects, "RoadPreviewOccupiedPost", previewBlockedMaterial, center + new Vector3(-cellSize * 0.22f, 0.12f, -cellSize * 0.22f), postScale);
            AddLooseCube(placementPreviewObjects, "RoadPreviewOccupiedPost", previewBlockedMaterial, center + new Vector3(cellSize * 0.22f, 0.12f, cellSize * 0.22f), postScale);
        }

        private void AddRoadPreviewRouteStatusBadge(GridPos from, GridPos to)
        {
            // CITY_SKYLINES_ROAD_PREVIEW_ROUTE_BADGE shows route-level failures without hiding valid cells.
            var center = new Vector3((from.X + to.X + 1f) * cellSize * 0.5f, roadHeight + 0.24f, (from.Y + to.Y + 1f) * cellSize * 0.5f);
            AddLooseCube(placementPreviewObjects, "RoadPreviewRouteBlockedBadge", previewBlockedMaterial, center, new Vector3(cellSize * 0.38f, 0.08f, cellSize * 0.38f));
            AddLooseCube(placementPreviewObjects, "RoadPreviewRouteBlockedPost", previewBlockedMaterial, center + new Vector3(0f, 0.16f, 0f), new Vector3(0.07f, 0.28f, 0.07f));
            AddLooseCube(placementPreviewObjects, "RoadPreviewRouteBlockedCap", roadLineMaterial, center + new Vector3(0f, 0.34f, 0f), new Vector3(cellSize * 0.28f, 0.055f, cellSize * 0.08f));
        }

        private bool RoadPreviewTouchesWater(GridPos pos)
        {
            return IsWaterTile(pos.X - 1, pos.Y) || IsWaterTile(pos.X + 1, pos.Y) || IsWaterTile(pos.X, pos.Y - 1) || IsWaterTile(pos.X, pos.Y + 1);
        }

        private void AddRoadPreviewWaterfrontCue(Vector3 center, bool horizontal)
        {
            // CITY_SKYLINES_ROAD_PREVIEW_WATERFRONT_CUE previews embankments on legal waterfront roads.
            var railScale = horizontal
                ? new Vector3(cellSize * 0.56f, 0.032f, 0.035f)
                : new Vector3(0.035f, 0.032f, cellSize * 0.56f);
            var offset = horizontal ? new Vector3(0f, 0.075f, -cellSize * 0.25f) : new Vector3(-cellSize * 0.25f, 0.075f, 0f);
            AddLooseCube(placementPreviewObjects, "RoadPreviewWaterfrontRail", roadLineMaterial, center + offset, railScale);
            AddLooseCube(placementPreviewObjects, "RoadPreviewWaterfrontRail", roadLineMaterial, center - offset + new Vector3(0f, 0.15f, 0f), railScale);
        }

        private Material SelectedTileFocusMaterial(TileData tile)
        {
            if (tile == null)
            {
                return roadLineMaterial;
            }

            if (tile.Terrain == TerrainType.Water)
            {
                return windowMaterial;
            }

            if (!string.IsNullOrEmpty(tile.BuildingId))
            {
                return roadLineMaterial;
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                return serviceNeedMaterial;
            }

            if (tile.Zone == ZoneType.None)
            {
                return previewOkMaterial;
            }

            return MaterialForZone(tile.Zone);
        }

        private void AddSelectedTileFocusBase(Vector3 center, Material accent)
        {
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusSoftBase", windowMaterial, center + new Vector3(0f, -0.025f, 0f), new Vector3(cellSize * 0.62f, 0.018f, cellSize * 0.62f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusAccentCore", accent, center + new Vector3(0f, 0.005f, 0f), new Vector3(cellSize * 0.34f, 0.018f, cellSize * 0.34f));
        }

        private void AddSelectedTileFocusCorners(Vector3 center, Material accent)
        {
            var half = cellSize * 0.43f;
            var arm = cellSize * 0.22f;
            var thickness = Mathf.Max(0.035f, cellSize * 0.04f);
            var y = center.y + 0.09f;
            AddSelectedTileFocusCorner(center, half, arm, thickness, y, accent, -1f, -1f);
            AddSelectedTileFocusCorner(center, half, arm, thickness, y, accent, 1f, -1f);
            AddSelectedTileFocusCorner(center, half, arm, thickness, y, accent, -1f, 1f);
            AddSelectedTileFocusCorner(center, half, arm, thickness, y, accent, 1f, 1f);
        }

        private void AddSelectedTileFocusCorner(Vector3 center, float half, float arm, float thickness, float y, Material accent, float signX, float signZ)
        {
            var corner = new Vector3(center.x + signX * half, y, center.z + signZ * half);
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusPlanningCorner", accent, corner + new Vector3(-signX * arm * 0.62f, -0.028f, 0f), new Vector3(arm * 1.18f, thickness * 0.72f, thickness * 0.72f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusPlanningCorner", accent, corner + new Vector3(0f, -0.026f, -signZ * arm * 0.62f), new Vector3(thickness * 0.72f, thickness * 0.72f, arm * 1.18f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusWhiteCorner", windowMaterial, corner + new Vector3(-signX * arm * 0.5f, 0f, 0f), new Vector3(arm, thickness, thickness));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusWhiteCorner", windowMaterial, corner + new Vector3(0f, 0f, -signZ * arm * 0.5f), new Vector3(thickness, thickness, arm));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusAccentPip", accent, corner + new Vector3(-signX * arm * 0.18f, 0.03f, -signZ * arm * 0.18f), new Vector3(thickness * 1.4f, thickness * 0.75f, thickness * 1.4f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusSurveyPost", roadLineMaterial, corner + new Vector3(-signX * arm * 0.06f, 0.06f, -signZ * arm * 0.06f), new Vector3(thickness * 0.82f, 0.13f, thickness * 0.82f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusSurveyCap", accent, corner + new Vector3(-signX * arm * 0.06f, 0.14f, -signZ * arm * 0.06f), new Vector3(thickness * 1.55f, thickness * 0.72f, thickness * 1.55f));
        }

        private void AddSelectedTileFocusBeacon(Vector3 center, TileData tile)
        {
            var material = SelectedTileFocusMaterial(tile);
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusPinStem", material, center + new Vector3(cellSize * 0.32f, 0.16f, -cellSize * 0.32f), new Vector3(0.045f, 0.24f, 0.045f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileFocusPinCap", roadLineMaterial, center + new Vector3(cellSize * 0.32f, 0.3f, -cellSize * 0.32f), new Vector3(0.14f, 0.045f, 0.14f));
        }

        private void AddSelectedTileInformationLens(GridPos pos, Vector3 center, TileData tile)
        {
            // CITY_SKYLINES_SELECTED_INFO_LENS echoes the map issue language on the active tile.
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return;
            }

            var metrics = controller != null ? controller.Metrics : null;
            var severity = CityIssueSeverity(tile, metrics);
            var kind = CityIssueAdvisorKind(tile, metrics);
            var pressureMaterial = InspectPressureMaterial(Mathf.Max(0, Mathf.Max(severity, tile.Traffic - 42)));
            var material = severity >= 18 ? CityIssueAdvisorMaterial(kind, pressureMaterial) : SelectedTileFocusMaterial(tile);
            var lensCenter = center + new Vector3(-cellSize * 0.32f, 0.19f, cellSize * 0.32f);
            AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoLensBadge", material, lensCenter, new Vector3(0.2f, 0.044f, 0.16f));
            AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoLensHeader", roadLineMaterial, lensCenter + new Vector3(0f, 0.043f, 0f), new Vector3(0.14f, 0.018f, 0.034f));
            AddSelectedTileInformationGlyph(pos, tile, kind, lensCenter + new Vector3(0f, 0.08f, 0f), material);

            if (severity >= 18)
            {
                AddSelectedTileIssuePips(lensCenter, severity, material);
            }

            AddSelectedTileTrafficRibbonCue(pos, center, tile);
        }

        private void AddSelectedTileInformationGlyph(GridPos pos, TileData tile, CityIssueAdvisorMarkerKind kind, Vector3 center, Material material)
        {
            if (kind == CityIssueAdvisorMarkerKind.Traffic)
            {
                var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
                var roadScale = vertical ? new Vector3(0.034f, 0.024f, 0.14f) : new Vector3(0.14f, 0.024f, 0.034f);
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoTrafficGlyphRoad", roadLineMaterial, center, roadScale);
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoTrafficGlyphLoad", trafficPulseMaterial, center + new Vector3(0f, 0.03f, 0f), roadScale * 0.62f);
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Service)
            {
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoServiceGlyph", serviceNeedMaterial, center, new Vector3(0.13f, 0.024f, 0.034f));
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoServiceGlyph", serviceNeedMaterial, center + new Vector3(0f, 0.002f, 0f), new Vector3(0.034f, 0.024f, 0.13f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Utility)
            {
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoUtilityGlyphNode", windowMaterial, center, new Vector3(0.1f, 0.04f, 0.1f));
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoUtilityGlyphPipe", roadLineMaterial, center + new Vector3(0f, 0.038f, 0f), new Vector3(0.14f, 0.022f, 0.032f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Fiscal)
            {
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoFiscalGlyph", serviceNeedMaterial, center, new Vector3(0.14f, 0.026f, 0.1f));
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoFiscalGlyphLine", roadLineMaterial, center + new Vector3(0f, 0.034f, 0f), new Vector3(0.1f, 0.018f, 0.028f));
                return;
            }

            var accent = !string.IsNullOrEmpty(tile.BuildingId) ? roadLineMaterial : material;
            AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoGeneralGlyph", accent, center + new Vector3(0f, 0.02f, 0f), new Vector3(0.085f, 0.055f, 0.085f));
        }

        private void AddSelectedTileIssuePips(Vector3 lensCenter, int severity, Material material)
        {
            var pipCount = severity >= 58 ? 3 : (severity >= 36 ? 2 : 1);
            for (var i = 0; i < pipCount; i += 1)
            {
                var pipMaterial = i == pipCount - 1 && severity >= 58 ? trafficPulseMaterial : material;
                AddLooseCube(selectedTileFocusObjects, "SelectedTileInfoIssuePip", pipMaterial, lensCenter + new Vector3(0.13f, 0.042f + i * 0.018f, -0.065f + i * 0.045f), new Vector3(0.04f, 0.032f, 0.04f));
            }
        }

        private void AddSelectedTileTrafficRibbonCue(GridPos pos, Vector3 center, TileData tile)
        {
            if (string.IsNullOrEmpty(tile.RoadId) && tile.Traffic < 45)
            {
                return;
            }

            var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
            var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y) || !vertical;
            var material = tile.Traffic >= 70 ? trafficPulseMaterial : serviceNeedMaterial;
            var ribbonCenter = center + new Vector3(0f, 0.074f, 0f);
            var ribbonScale = horizontal
                ? new Vector3(cellSize * 0.58f, 0.02f, 0.062f)
                : new Vector3(0.062f, 0.02f, cellSize * 0.58f);
            var edgeScale = horizontal
                ? new Vector3(cellSize * 0.36f, 0.018f, 0.026f)
                : new Vector3(0.026f, 0.018f, cellSize * 0.36f);
            AddLooseCube(selectedTileFocusObjects, "SelectedTileTrafficRibbonCue", material, ribbonCenter, ribbonScale);
            AddLooseCube(selectedTileFocusObjects, "SelectedTileTrafficRibbonCueCore", roadLineMaterial, ribbonCenter + new Vector3(0f, 0.03f, 0f), edgeScale);

            var count = tile.Traffic >= 70 ? 3 : 2;
            var along = horizontal ? Vector3.right : Vector3.forward;
            for (var i = 0; i < count; i += 1)
            {
                var offset = (i - (count - 1) * 0.5f) * cellSize * 0.15f;
                AddLooseCube(selectedTileFocusObjects, "SelectedTileTrafficRibbonCueTick", i == count - 1 ? trafficPulseMaterial : windowMaterial, ribbonCenter + along * offset + new Vector3(0f, 0.06f + i * 0.004f, 0f), new Vector3(0.036f, 0.034f, 0.036f));
            }
        }

        private void AddTileContextMicroHints(List<GameObject> objects, string prefix, Vector3 center, TileData tile)
        {
            // CITY_SKYLINES_TILE_CONTEXT_HINTS put service, movement, and land-value reads directly around the parcel.
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return;
            }

            AddServiceAccessMiniHint(objects, prefix, center + new Vector3(-cellSize * 0.47f, 0.125f, cellSize * 0.12f), ServiceAccessValue(tile));
            AddTrafficLoadMiniHint(objects, prefix, center + new Vector3(0f, 0.122f, -cellSize * 0.48f), tile.Traffic);
            AddLandValueMiniHint(objects, prefix, center + new Vector3(cellSize * 0.47f, 0.125f, cellSize * 0.12f), tile.LandValue);
        }

        private void AddServiceAccessMiniHint(List<GameObject> objects, string prefix, Vector3 center, int serviceScore)
        {
            var material = serviceScore >= 58 ? previewOkMaterial : (serviceScore >= 34 ? serviceNeedMaterial : trafficPulseMaterial);
            AddLooseCube(objects, prefix + "ServiceHintPlate", serviceMaterial, center, new Vector3(cellSize * 0.22f, 0.024f, cellSize * 0.16f));
            AddLooseCube(objects, prefix + "ServiceHintCross", material, center + new Vector3(0f, 0.032f, 0f), new Vector3(cellSize * 0.16f, 0.024f, 0.036f));
            AddLooseCube(objects, prefix + "ServiceHintCross", material, center + new Vector3(0f, 0.034f, 0f), new Vector3(0.036f, 0.024f, cellSize * 0.16f));

            var count = Mathf.Clamp(serviceScore / 30 + 1, 1, 3);
            for (var i = 0; i < count; i += 1)
            {
                AddLooseCube(objects, prefix + "ServiceHintPip", material, center + new Vector3((i - 1) * 0.055f, 0.068f + i * 0.004f, cellSize * 0.12f), new Vector3(0.035f, 0.035f, 0.035f));
            }
        }

        private void AddTrafficLoadMiniHint(List<GameObject> objects, string prefix, Vector3 center, int traffic)
        {
            var material = traffic >= 70 ? trafficPulseMaterial : (traffic >= 45 ? serviceNeedMaterial : previewOkMaterial);
            var fill = Mathf.Lerp(cellSize * 0.14f, cellSize * 0.34f, Mathf.Clamp01(traffic / 100f));
            AddLooseCube(objects, prefix + "TrafficHintAsphalt", roadMaterial, center, new Vector3(cellSize * 0.38f, 0.024f, 0.07f));
            AddLooseCube(objects, prefix + "TrafficHintLoad", material, center + new Vector3(0f, 0.028f, 0f), new Vector3(fill, 0.022f, 0.034f));

            var count = traffic >= 70 ? 3 : (traffic >= 45 ? 2 : 1);
            for (var i = 0; i < count; i += 1)
            {
                var x = (i - (count - 1) * 0.5f) * 0.095f;
                AddLooseCube(objects, prefix + "TrafficHintQueueTick", traffic >= 70 && i == count - 1 ? trafficPulseMaterial : roadLineMaterial, center + new Vector3(x, 0.062f + i * 0.004f, 0f), new Vector3(0.035f, 0.044f, 0.034f));
            }
        }

        private void AddLandValueMiniHint(List<GameObject> objects, string prefix, Vector3 center, int landValue)
        {
            var material = landValue >= 62 ? windowMaterial : (landValue >= 38 ? roadLineMaterial : serviceNeedMaterial);
            AddLooseCube(objects, prefix + "LandValueHintPlaque", serviceNeedMaterial, center, new Vector3(cellSize * 0.19f, 0.026f, cellSize * 0.19f));
            AddLooseCube(objects, prefix + "LandValueHintGem", material, center + new Vector3(0f, 0.038f, 0f), new Vector3(0.095f, 0.07f, 0.095f));
            AddLooseCube(objects, prefix + "LandValueHintUnderline", roadLineMaterial, center + new Vector3(0f, 0.086f, -cellSize * 0.1f), new Vector3(cellSize * 0.17f, 0.02f, 0.028f));

            var count = landValue >= 78 ? 3 : (landValue >= 48 ? 2 : 1);
            for (var i = 0; i < count; i += 1)
            {
                AddLooseCube(objects, prefix + "LandValueHintSpark", material, center + new Vector3(cellSize * 0.11f, 0.075f + i * 0.03f, (i - 1) * 0.045f), new Vector3(0.035f, 0.044f, 0.035f));
            }
        }

        private void AddSelectedOpenLotPotentialCue(GridPos pos, Vector3 center, TileData tile)
        {
            if (!IsVacantDevelopmentTile(tile))
            {
                return;
            }

            var score = OpenLotDevelopmentPotentialScore(pos, tile);
            var material = OpenLotPotentialMaterial(tile, score);
            var scoreMaterial = score >= 70 ? windowMaterial : (score >= 44 ? serviceNeedMaterial : roadLineMaterial);
            var cueCenter = center + new Vector3(0f, 0.105f, cellSize * 0.45f);
            AddLooseCube(selectedTileFocusObjects, "SelectedOpenLotPotentialBlueprint", material, cueCenter, new Vector3(cellSize * 0.36f, 0.024f, cellSize * 0.16f));
            AddLooseCube(selectedTileFocusObjects, "SelectedOpenLotPotentialSurveyLine", roadLineMaterial, cueCenter + new Vector3(0f, 0.028f, 0f), new Vector3(cellSize * 0.3f, 0.018f, 0.028f));
            AddPotentialScorePips(selectedTileFocusObjects, "SelectedOpenLotPotential", cueCenter + new Vector3(0f, 0.054f, 0.052f), score, scoreMaterial);
        }

        private void AddPlacementCornerGuides(Vector3 center, float width, float depth, Material material, string name)
        {
            // REFERENCE_IMAGE_PLANNING_CORNER_GUIDES echoes the crisp dashed build zones in the mockup.
            var halfX = width * 0.5f;
            var halfZ = depth * 0.5f;
            var arm = Mathf.Min(cellSize * 0.28f, Mathf.Min(width, depth) * 0.32f);
            var thickness = Mathf.Max(0.035f, cellSize * 0.04f);
            var y = center.y + 0.075f;
            AddPlacementCornerGuide(center, halfX, halfZ, arm, thickness, y, material, name, -1f, -1f);
            AddPlacementCornerGuide(center, halfX, halfZ, arm, thickness, y, material, name, 1f, -1f);
            AddPlacementCornerGuide(center, halfX, halfZ, arm, thickness, y, material, name, -1f, 1f);
            AddPlacementCornerGuide(center, halfX, halfZ, arm, thickness, y, material, name, 1f, 1f);
        }

        private void AddPlacementCornerGuide(Vector3 center, float halfX, float halfZ, float arm, float thickness, float y, Material material, string name, float signX, float signZ)
        {
            var corner = new Vector3(center.x + signX * halfX, y, center.z + signZ * halfZ);
            AddLooseCube(placementPreviewObjects, name, material, corner + new Vector3(-signX * arm * 0.5f, 0f, 0f), new Vector3(arm, thickness, thickness));
            AddLooseCube(placementPreviewObjects, name, material, corner + new Vector3(0f, 0f, -signZ * arm * 0.5f), new Vector3(thickness, thickness, arm));
        }

        private void RebuildBuildings()
        {
            ClearObjects(buildingObjects);
            var buildings = controller.Buildings;
            if (buildings == null)
            {
                return;
            }

            for (var i = 0; i < buildings.Count; i += 1)
            {
                var building = buildings[i];
                var tile = controller.GetTile(building.Pos.X, building.Pos.Y);
                var definition = controller.GetBuildingDefinition(building.ConfigId);
                var zone = tile != null ? tile.Zone : ZoneType.None;
                var material = MaterialForDefinition(definition, zone);
                var obj = CreateBuildingVisual(building, definition, material, zone);
                buildingObjects.Add(obj);
            }
        }

        // Performance: Incremental building update
        public void RebuildBuildingsIncremental(System.Collections.Generic.List<string> changedBuildingIds)
        {
            if (changedBuildingIds == null || changedBuildingIds.Count == 0)
                return;

            var buildings = controller.Buildings;
            if (buildings == null)
                return;

            // Remove old visuals for changed buildings
            for (int i = buildingObjects.Count - 1; i >= 0; i--)
            {
                var obj = buildingObjects[i];
                if (obj != null && changedBuildingIds.Contains(obj.name))
                {
                    Destroy(obj);
                    buildingObjects.RemoveAt(i);
                }
            }

            // Add new visuals
            foreach (var building in buildings)
            {
                if (changedBuildingIds.Contains(building.Id))
                {
                    var tile = controller.GetTile(building.Pos.X, building.Pos.Y);
                    var definition = controller.GetBuildingDefinition(building.ConfigId);
                    var zone = tile != null ? tile.Zone : ZoneType.None;
                    var material = MaterialForDefinition(definition, zone);
                    var obj = CreateBuildingVisual(building, definition, material, zone);
                    buildingObjects.Add(obj);
                }
            }
        }

        private void RebuildDecorations()
        {
            // LOW_POLY_ISOMETRIC_REFERENCE_UI keeps scenery procedural and export-light.
            ClearObjects(decorationObjects);
            var grid = controller.Grid;
            if (grid == null)
            {
                return;
            }

            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var hash = DecorationHash(x, y);
                    var tile = controller.GetTile(x, y);
                    if (tile != null && tile.Terrain == TerrainType.Water)
                    {
                        if (HasWaterCrossingRoad(x, y)) AddWaterCrossingHint(new GridPos(x, y));
                        AddWaterSurfaceDetail(new GridPos(x, y), hash);
                        continue;
                    }

                    if (!IsOpenSceneryTile(x, y))
                    {
                        continue;
                    }

                    if (tile != null && tile.Terrain == TerrainType.Plain && tile.Zone == ZoneType.None && (hash % 4 == 0 || IsShorelineSceneryTile(x, y)))
                    {
                        AddGrassCheckerRelief(new GridPos(x, y), hash);
                    }

                    if (tile != null && tile.Terrain == TerrainType.Plain && hash % 3 == 0)
                    {
                        AddGrassGridCue(new GridPos(x, y), hash);
                    }

                    if (tile != null && tile.Terrain == TerrainType.Plain && IsRoadsideSceneryTile(x, y) && (hash % 13 == 0 || hash % 23 == 0))
                    {
                        AddRoadsideMiniScene(new GridPos(x, y), hash);
                    }

                    if (IsUnbuiltZonedSceneryTile(tile) && hash % 2 == 0)
                    {
                        AddZoneParcelCue(new GridPos(x, y), tile.Zone, hash);
                        continue;
                    }

                    if (tile != null
                        && tile.Terrain == TerrainType.Plain
                        && tile.Zone == ZoneType.None
                        && !IsShorelineSceneryTile(x, y)
                        && IsRoadsideSceneryTile(x, y)
                        && hash % 9 == 0)
                    {
                        var openLotPos = new GridPos(x, y);
                        AddOpenLotDevelopmentPotentialDecor(openLotPos, tile, hash, CellCenter(openLotPos, 0.052f));
                        continue;
                    }

                    var centralScenery = IsCentralRoadTile(new GridPos(x, y));
                    if (tile != null
                        && tile.Terrain == TerrainType.Plain
                        && tile.Zone == ZoneType.None
                        && centralScenery
                        && !IsShorelineSceneryTile(x, y)
                        && IsRoadsideSceneryTile(x, y)
                        && hash % 6 == 0)
                    {
                        AddCentralGreenParcelAccent(new GridPos(x, y), hash);
                    }

                    if (tile != null
                        && tile.Terrain == TerrainType.Plain
                        && tile.Zone == ZoneType.None
                        && IsRoadsideSceneryTile(x, y)
                        && (hash % 7 == 0 || (centralScenery && hash % 4 == 0)))
                    {
                        AddRoadEdgePocketPlaza(new GridPos(x, y), hash);
                        continue;
                    }

                    if (tile != null
                        && tile.Terrain == TerrainType.Plain
                        && tile.Zone == ZoneType.None
                        && !IsShorelineSceneryTile(x, y)
                        && IsRoadsideSceneryTile(x, y)
                        && (hash % 11 == 0 || (centralScenery && hash % 8 == 0)))
                    {
                        AddPocketParkingMarkings(new GridPos(x, y), hash);
                        continue;
                    }

                    if (tile != null && tile.Terrain == TerrainType.Plain && IsRoadsideSceneryTile(x, y) && (hash % 5 == 0 || (centralScenery && hash % 3 == 0)))
                    {
                        AddRoadsideGreenCue(new GridPos(x, y), hash);
                        continue;
                    }

                    if (tile != null
                        && tile.Terrain == TerrainType.Plain
                        && tile.Zone == ZoneType.None
                        && !IsShorelineSceneryTile(x, y)
                        && !IsRoadsideSceneryTile(x, y)
                        && hash % 13 == 0)
                    {
                        AddMeadowDetailCluster(new GridPos(x, y), hash);
                        continue;
                    }

                    if (tile != null
                        && tile.Terrain == TerrainType.Plain
                        && tile.Zone == ZoneType.None
                        && !IsShorelineSceneryTile(x, y)
                        && !IsRoadsideSceneryTile(x, y)
                        && hash % 17 == 0)
                    {
                        AddFreshLawnPocket(new GridPos(x, y), hash);
                        continue;
                    }

                    if (IsShorelineSceneryTile(x, y))
                    {
                        var shorePos = new GridPos(x, y);
                        AddContinuousShorelineBand(shorePos);
                        AddRiverbankMicroSteps(shorePos, hash);
                        if (hash % 4 == 0)
                        {
                            AddShorelineDetail(shorePos, hash);
                        }
                    }
                    else if (tile != null && tile.Terrain == TerrainType.Hill && tile.Zone == ZoneType.None && hash % 3 == 0)
                    {
                        AddHillFacetCue(new GridPos(x, y), hash);
                    }
                    else if (hash % 19 == 0)
                    {
                        AddTree(new GridPos(x, y), hash);
                    }
                    else if (hash % 31 == 0)
                    {
                        AddRock(new GridPos(x, y), hash);
                    }
                }
            }
        }

        private void AddMeadowDetailCluster(GridPos pos, int seed)
        {
            // REFERENCE_IMAGE_MEADOW_CLUSTERS fills quiet grass lots with bright low-poly city scenery.
            var center = CellCenter(pos, 0.054f);
            var horizontal = (seed & 2) == 0;
            var side = ((seed >> 4) & 1) == 0 ? -1f : 1f;
            var along = horizontal ? Vector3.right : Vector3.forward;
            var cross = horizontal ? Vector3.forward : Vector3.right;
            var lawnScale = horizontal
                ? new Vector3(cellSize * 0.62f, 0.018f, cellSize * 0.34f)
                : new Vector3(cellSize * 0.34f, 0.018f, cellSize * 0.62f);
            var flowerScale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.036f, cellSize * 0.07f)
                : new Vector3(cellSize * 0.07f, 0.036f, cellSize * 0.3f);
            var pathScale = horizontal
                ? new Vector3(cellSize * 0.36f, 0.014f, cellSize * 0.032f)
                : new Vector3(cellSize * 0.032f, 0.014f, cellSize * 0.36f);

            AddLooseCube(decorationObjects, "LowPolyMeadowClusterLawn", grassGridMaterial, center, lawnScale);
            AddLooseCube(decorationObjects, "LowPolyMeadowClusterFlowerBand", serviceNeedMaterial, center + cross * (side * cellSize * 0.12f) + new Vector3(0f, 0.034f, 0f), flowerScale);
            AddLooseCube(decorationObjects, "LowPolyMeadowClusterPathChip", roadLineMaterial, center - cross * (side * cellSize * 0.16f) + new Vector3(0f, 0.026f, 0f), pathScale);
            AddMeadowDetailSaplings(center, along, cross, side, seed);
            AddMeadowDetailStones(center, along, cross, side, seed);
        }

        private void AddMeadowDetailSaplings(Vector3 center, Vector3 along, Vector3 cross, float side, int seed)
        {
            var first = center - along * cellSize * 0.22f - cross * side * cellSize * 0.06f;
            var second = center + along * cellSize * 0.18f - cross * side * cellSize * 0.18f;
            AddLooseCube(decorationObjects, "LowPolyMeadowSaplingTrunk", treeTrunkMaterial, first + new Vector3(0f, 0.1f, 0f), new Vector3(0.045f, 0.19f, 0.045f));
            AddLooseCube(decorationObjects, "LowPolyMeadowSaplingCanopy", treeCanopyMaterial, first + new Vector3(0f, 0.23f, 0f), new Vector3(cellSize * 0.18f, 0.15f, cellSize * 0.18f));

            if (seed % 3 != 1)
            {
                AddLooseCube(decorationObjects, "LowPolyMeadowShrubMound", treeCanopyMaterial, second + new Vector3(0f, 0.08f, 0f), new Vector3(cellSize * 0.16f, 0.11f, cellSize * 0.14f));
                AddLooseCube(decorationObjects, "LowPolyMeadowShrubHighlight", grassGridMaterial, second + new Vector3(0f, 0.145f, 0f), new Vector3(cellSize * 0.1f, 0.035f, cellSize * 0.09f));
            }
        }

        private void AddMeadowDetailStones(Vector3 center, Vector3 along, Vector3 cross, float side, int seed)
        {
            var pebbleBase = center + along * cellSize * 0.26f + cross * side * cellSize * 0.2f;
            var stoneSize = cellSize * (0.07f + ((seed >> 5) & 3) * 0.008f);
            AddLooseCube(decorationObjects, "LowPolyMeadowStone", rockMaterial, pebbleBase + new Vector3(0f, stoneSize * 0.48f, 0f), new Vector3(stoneSize * 1.2f, stoneSize * 0.7f, stoneSize));
            AddLooseCube(decorationObjects, "LowPolyMeadowStoneGlint", shoreMaterial != null ? shoreMaterial : roadLineMaterial, pebbleBase + new Vector3(0f, stoneSize * 0.9f, 0f), new Vector3(stoneSize * 0.58f, 0.018f, stoneSize * 0.34f));

            if ((seed & 1) == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyMeadowFlowerDot", serviceNeedMaterial, center - along * cellSize * 0.08f + cross * side * cellSize * 0.23f + new Vector3(0f, 0.06f, 0f), new Vector3(cellSize * 0.06f, 0.035f, cellSize * 0.06f));
                AddLooseCube(decorationObjects, "LowPolyMeadowParcelTick", roadLineMaterial, center + along * cellSize * 0.32f - cross * side * cellSize * 0.32f + new Vector3(0f, 0.032f, 0f), new Vector3(cellSize * 0.16f, 0.014f, cellSize * 0.026f));
            }
        }

        private bool HasWaterCrossingRoad(int x, int y)
        {
            return (HasRoadTile(x - 1, y) && HasRoadTile(x + 1, y))
                || (HasRoadTile(x, y - 1) && HasRoadTile(x, y + 1));
        }

        private void AddWaterCrossingHint(GridPos pos)
        {
            // REFERENCE_IMAGE_RIVER_BRIDGE_HINT adds a low-poly bridge cue where roads meet across water.
            var horizontal = HasRoadTile(pos.X - 1, pos.Y) && HasRoadTile(pos.X + 1, pos.Y);
            var center = CellCenter(pos, 0.12f);
            var deckScale = horizontal
                ? new Vector3(cellSize * 0.9f, 0.055f, cellSize * 0.24f)
                : new Vector3(cellSize * 0.24f, 0.055f, cellSize * 0.9f);
            var shadowScale = horizontal
                ? new Vector3(cellSize * 0.82f, 0.018f, cellSize * 0.32f)
                : new Vector3(cellSize * 0.32f, 0.018f, cellSize * 0.82f);
            var abutmentScale = horizontal
                ? new Vector3(cellSize * 0.12f, 0.07f, cellSize * 0.32f)
                : new Vector3(cellSize * 0.32f, 0.07f, cellSize * 0.12f);
            // REFERENCE_IMAGE_RIVER_BRIDGE_ABUTMENTS adds tiny end caps and shadow under bridge decks.
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeShadow", shoreMaterial, center + new Vector3(0f, -0.04f, 0f), shadowScale);
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeDeck", roadMaterial, center, deckScale);
            if (horizontal)
            {
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeCenterLine", roadLineMaterial, center + new Vector3(0f, 0.06f, 0f), new Vector3(cellSize * 0.5f, 0.028f, 0.025f));
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeApproachPaver", shoreMaterial, center + new Vector3(-cellSize * 0.55f, 0.01f, 0f), new Vector3(cellSize * 0.18f, 0.025f, cellSize * 0.36f));
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeApproachPaver", shoreMaterial, center + new Vector3(cellSize * 0.55f, 0.01f, 0f), new Vector3(cellSize * 0.18f, 0.025f, cellSize * 0.36f));
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeAbutment", shoreMaterial, center + new Vector3(-cellSize * 0.46f, -0.005f, 0f), abutmentScale);
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeAbutment", shoreMaterial, center + new Vector3(cellSize * 0.46f, -0.005f, 0f), abutmentScale);
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeRail", roadLineMaterial, center + new Vector3(0f, 0.055f, -cellSize * 0.16f), new Vector3(cellSize * 0.76f, 0.035f, 0.035f));
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeRail", roadLineMaterial, center + new Vector3(0f, 0.055f, cellSize * 0.16f), new Vector3(cellSize * 0.76f, 0.035f, 0.035f));
                AddRiverBridgeLayeredDetails(center, true);
                return;
            }

            AddLooseCube(decorationObjects, "LowPolyRiverBridgeCenterLine", roadLineMaterial, center + new Vector3(0f, 0.06f, 0f), new Vector3(0.025f, 0.028f, cellSize * 0.5f));
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeApproachPaver", shoreMaterial, center + new Vector3(0f, 0.01f, -cellSize * 0.55f), new Vector3(cellSize * 0.36f, 0.025f, cellSize * 0.18f));
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeApproachPaver", shoreMaterial, center + new Vector3(0f, 0.01f, cellSize * 0.55f), new Vector3(cellSize * 0.36f, 0.025f, cellSize * 0.18f));
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeAbutment", shoreMaterial, center + new Vector3(0f, -0.005f, -cellSize * 0.46f), abutmentScale);
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeAbutment", shoreMaterial, center + new Vector3(0f, -0.005f, cellSize * 0.46f), abutmentScale);
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeRail", roadLineMaterial, center + new Vector3(-cellSize * 0.16f, 0.055f, 0f), new Vector3(0.035f, 0.035f, cellSize * 0.76f));
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeRail", roadLineMaterial, center + new Vector3(cellSize * 0.16f, 0.055f, 0f), new Vector3(0.035f, 0.035f, cellSize * 0.76f));
            AddRiverBridgeLayeredDetails(center, false);
        }

        private void AddRiverBridgeLayeredDetails(Vector3 center, bool horizontal)
        {
            // CITY_SKYLINES_LIGHT_BRIDGE_DETAILS makes water crossings read as small built structures.
            var span = horizontal ? Vector3.right : Vector3.forward;
            var side = horizontal ? Vector3.forward : Vector3.right;
            var pylonScale = new Vector3(0.055f, 0.28f, 0.055f);
            var capScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.035f, cellSize * 0.06f)
                : new Vector3(cellSize * 0.06f, 0.035f, cellSize * 0.18f);
            var braceScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.03f, 0.035f)
                : new Vector3(0.035f, 0.03f, cellSize * 0.2f);
            for (var i = -1; i <= 1; i += 2)
            {
                var pylonCenter = center + span * (i * cellSize * 0.34f) + new Vector3(0f, 0.18f, 0f);
                AddLooseCube(decorationObjects, "LowPolyRiverBridgePylon", serviceMaterial, pylonCenter, pylonScale);
                AddLooseCube(decorationObjects, "LowPolyRiverBridgePylonCap", roadLineMaterial, pylonCenter + new Vector3(0f, 0.16f, 0f), capScale);
                AddLooseCube(decorationObjects, "LowPolyRiverBridgeLampGlow", windowMaterial, pylonCenter + side * cellSize * 0.18f + new Vector3(0f, 0.23f, 0f), new Vector3(0.09f, 0.035f, 0.09f));
            }

            AddLooseCube(decorationObjects, "LowPolyRiverBridgeBrace", roadLineMaterial, center + side * cellSize * 0.18f + new Vector3(0f, 0.19f, 0f), braceScale);
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeBrace", roadLineMaterial, center - side * cellSize * 0.18f + new Vector3(0f, 0.19f, 0f), braceScale);
            AddLooseCube(decorationObjects, "LowPolyRiverBridgeWaterShadow", buildingFootprintMaterial, center + new Vector3(0f, -0.075f, 0f), horizontal ? new Vector3(cellSize * 0.62f, 0.014f, cellSize * 0.18f) : new Vector3(cellSize * 0.18f, 0.014f, cellSize * 0.62f));
            AddRiverBridgeApproachDetails(center, horizontal);
        }

        private void AddRiverBridgeApproachDetails(Vector3 center, bool horizontal)
        {
            // CITY_SKYLINES_BRIDGEHEAD_DETAILS adds readable approach paint and pocket landscaping at river crossings.
            var span = horizontal ? Vector3.right : Vector3.forward;
            var side = horizontal ? Vector3.forward : Vector3.right;
            var paintScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.014f, cellSize * 0.032f)
                : new Vector3(cellSize * 0.032f, 0.014f, cellSize * 0.2f);
            var headScale = horizontal
                ? new Vector3(cellSize * 0.09f, 0.014f, cellSize * 0.028f)
                : new Vector3(cellSize * 0.028f, 0.014f, cellSize * 0.09f);
            var planterScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.045f, cellSize * 0.1f)
                : new Vector3(cellSize * 0.1f, 0.045f, cellSize * 0.16f);
            var signScale = horizontal
                ? new Vector3(cellSize * 0.13f, 0.075f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.075f, cellSize * 0.13f);

            for (var i = -1; i <= 1; i += 2)
            {
                var approach = center + span * (i * cellSize * 0.58f);
                AddLooseCube(decorationObjects, "LowPolyBridgeheadYieldPaint", windowMaterial, approach - span * (i * cellSize * 0.09f) + new Vector3(0f, 0.086f, 0f), paintScale);
                AddLooseCubeRotated(decorationObjects, "LowPolyBridgeheadYieldPaintHead", roadLineMaterial, approach - span * (i * cellSize * 0.2f) + side * cellSize * 0.035f + new Vector3(0f, 0.088f, 0f), headScale, horizontal ? (i > 0 ? 32f : 148f) : (i > 0 ? 58f : -58f));
                AddLooseCubeRotated(decorationObjects, "LowPolyBridgeheadYieldPaintHead", roadLineMaterial, approach - span * (i * cellSize * 0.2f) - side * cellSize * 0.035f + new Vector3(0f, 0.088f, 0f), headScale, horizontal ? (i > 0 ? -32f : -148f) : (i > 0 ? 122f : -122f));

                for (var s = -1; s <= 1; s += 2)
                {
                    var bollardCenter = approach + side * (s * cellSize * 0.22f);
                    AddLooseCube(decorationObjects, "LowPolyBridgeheadBollard", serviceNeedMaterial, bollardCenter + new Vector3(0f, 0.095f, 0f), new Vector3(0.055f, 0.16f, 0.055f));
                    AddLooseCube(decorationObjects, "LowPolyBridgeheadBollardCap", roadLineMaterial, bollardCenter + new Vector3(0f, 0.19f, 0f), new Vector3(0.075f, 0.028f, 0.075f));
                }

                var planterCenter = approach + side * cellSize * 0.34f + span * (i * cellSize * 0.04f);
                AddLooseCube(decorationObjects, "LowPolyBridgeheadPlanterBox", shoreMaterial != null ? shoreMaterial : roadLineMaterial, planterCenter + new Vector3(0f, 0.05f, 0f), planterScale);
                AddLooseCube(decorationObjects, "LowPolyBridgeheadPlanterCanopy", treeCanopyMaterial, planterCenter + new Vector3(0f, 0.13f, 0f), new Vector3(cellSize * 0.12f, 0.09f, cellSize * 0.12f));

                var signCenter = approach - side * cellSize * 0.34f;
                AddLooseCube(decorationObjects, "LowPolyBridgeheadSignPost", serviceMaterial, signCenter + new Vector3(0f, 0.16f, 0f), new Vector3(0.032f, 0.24f, 0.032f));
                AddLooseCube(decorationObjects, "LowPolyBridgeheadWaySign", commercialMaterial, signCenter + new Vector3(0f, 0.3f, 0f), signScale);
                AddLooseCube(decorationObjects, "LowPolyBridgeheadWaySignStripe", roadLineMaterial, signCenter + new Vector3(0f, 0.33f, 0f), signScale * 0.48f);
            }
        }

        private void AddWaterSurfaceDetail(GridPos pos, int seed)
        {
            // LOW_POLY_WATER_SURFACE_RIPPLES gives the river interior the bright reference-image water detail.
            var center = CellCenter(pos, 0.055f);
            var horizontalWater = IsWaterTile(pos.X - 1, pos.Y) || IsWaterTile(pos.X + 1, pos.Y);
            var verticalWater = IsWaterTile(pos.X, pos.Y - 1) || IsWaterTile(pos.X, pos.Y + 1);
            var horizontal = horizontalWater == verticalWater ? (seed & 2) == 0 : horizontalWater;
            var rippleScale = horizontal
                ? new Vector3(cellSize * 0.56f, 0.018f, cellSize * 0.045f)
                : new Vector3(cellSize * 0.045f, 0.018f, cellSize * 0.56f);
            var jitterX = (((seed >> 3) & 7) - 3) * cellSize * 0.025f;
            var jitterZ = (((seed >> 6) & 7) - 3) * cellSize * 0.025f;
            AddLooseCube(decorationObjects, "LowPolyWaterRippleDash", windowMaterial, center + new Vector3(jitterX, 0f, jitterZ), rippleScale);
            AddWaterSurfaceFacetShimmer(pos, center, horizontal, seed);
            AddWaterSpecularLadder(pos, center, horizontal, seed);
            AddWaterCornerSparkleChain(pos, center, horizontal, seed);

            if (horizontalWater || verticalWater)
            {
                // REFERENCE_IMAGE_WATER_FLOW_STREAKS aligns water highlights with the river channel.
                var flowScale = horizontal
                    ? new Vector3(cellSize * 0.34f, 0.014f, cellSize * 0.026f)
                    : new Vector3(cellSize * 0.026f, 0.014f, cellSize * 0.34f);
                var flowOffset = horizontal
                    ? new Vector3(0f, 0.012f, cellSize * 0.12f * (((seed & 4) == 0) ? 1f : -1f))
                    : new Vector3(cellSize * 0.12f * (((seed & 4) == 0) ? 1f : -1f), 0.012f, 0f);
                AddLooseCube(decorationObjects, "LowPolyWaterFlowStreak", windowMaterial, center + flowOffset, flowScale);
            }

            if (IsNearShoreWaterTile(pos.X, pos.Y))
            {
                AddNearShoreWaterGlint(pos, center, horizontal, seed);
                AddWaterlineShallowPatch(pos, seed);
            }

            if (seed % 13 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyWaterSpark", windowMaterial, center + new Vector3(-jitterZ, 0.012f, jitterX), new Vector3(cellSize * 0.1f, 0.016f, cellSize * 0.1f));
            }

            if (seed % 29 == 0 && (horizontalWater || verticalWater) && !HasWaterCrossingRoad(pos.X, pos.Y))
            {
                AddWaterTaxiCue(pos, center, horizontal);
            }
        }

        private void AddWaterCornerSparkleChain(GridPos pos, Vector3 center, bool horizontal, int seed)
        {
            // LOW_POLY_WATER_CORNER_SPARKLES add small clear highlights to the tile corners.
            var nearShore = IsNearShoreWaterTile(pos.X, pos.Y);
            if (!nearShore && seed % 2 != 0)
            {
                return;
            }

            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var sparkleScale = horizontal
                ? new Vector3(cellSize * 0.13f, 0.011f, cellSize * 0.022f)
                : new Vector3(cellSize * 0.022f, 0.011f, cellSize * 0.13f);
            var softScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.01f, cellSize * 0.018f)
                : new Vector3(cellSize * 0.018f, 0.01f, cellSize * 0.2f);
            AddLooseCube(decorationObjects, "LowPolyWaterCornerSparkle", windowMaterial, center + tangent * cellSize * 0.25f - normal * cellSize * 0.24f + new Vector3(0f, 0.034f, 0f), sparkleScale);
            AddLooseCube(decorationObjects, "LowPolyWaterCornerSparkle", roadLineMaterial, center - tangent * cellSize * 0.22f + normal * cellSize * 0.22f + new Vector3(0f, 0.03f, 0f), sparkleScale * 0.76f);

            if (nearShore)
            {
                AddLooseCube(decorationObjects, "LowPolyWaterClearEdgeSoftGlint", windowMaterial, center - tangent * cellSize * 0.05f - normal * cellSize * 0.32f + new Vector3(0f, 0.026f, 0f), softScale);
            }
        }

        private void AddWaterSpecularLadder(GridPos pos, Vector3 center, bool horizontal, int seed)
        {
            // LOW_POLY_WATER_SPECULAR_LADDER adds stepped sunny flecks without changing the terrain mesh.
            if (seed % 3 == 1 && !IsNearShoreWaterTile(pos.X, pos.Y))
            {
                return;
            }

            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var count = IsNearShoreWaterTile(pos.X, pos.Y) ? 3 : 2;
            for (var i = 0; i < count; i += 1)
            {
                var along = ((i - (count - 1) * 0.5f) * 0.18f + (((seed >> (i + 2)) & 1) == 0 ? -0.025f : 0.025f)) * cellSize;
                var side = (((seed >> (i + 5)) & 1) == 0 ? -1f : 1f) * cellSize * (0.09f + i * 0.055f);
                var glintScale = horizontal
                    ? new Vector3(cellSize * (0.18f - i * 0.025f), 0.011f, cellSize * 0.022f)
                    : new Vector3(cellSize * 0.022f, 0.011f, cellSize * (0.18f - i * 0.025f));
                var material = i == 0 && seed % 5 == 0 ? roadLineMaterial : windowMaterial;
                AddLooseCube(decorationObjects, "LowPolyWaterSunFleck", material, center + tangent * along + normal * side + new Vector3(0f, 0.028f + i * 0.004f, 0f), glintScale);
            }
        }

        private void AddWaterSurfaceFacetShimmer(GridPos pos, Vector3 center, bool horizontal, int seed)
        {
            // LOW_POLY_WATER_FACET_SHIMMER gives the river the bright stepped highlights from the reference.
            var tangent = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var glintScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.012f, cellSize * 0.026f)
                : new Vector3(cellSize * 0.026f, 0.012f, cellSize * 0.22f);
            var offsetA = tangent * ((((seed >> 2) & 3) - 1.5f) * cellSize * 0.08f) + normal * cellSize * 0.18f;
            var offsetB = tangent * ((((seed >> 5) & 3) - 1.5f) * cellSize * 0.08f) - normal * cellSize * 0.2f;
            AddLooseCube(decorationObjects, "LowPolyWaterFacetShimmer", windowMaterial, center + offsetA + new Vector3(0f, 0.018f, 0f), glintScale);

            if (seed % 3 == 0 || IsNearShoreWaterTile(pos.X, pos.Y))
            {
                AddLooseCube(decorationObjects, "LowPolyWaterFacetShimmerSoft", shoreMaterial != null ? shoreMaterial : windowMaterial, center + offsetB + new Vector3(0f, 0.014f, 0f), glintScale * 0.72f);
            }
        }

        private bool IsNearShoreWaterTile(int x, int y)
        {
            return IsWaterTile(x, y)
                && (!IsWaterTile(x - 1, y) || !IsWaterTile(x + 1, y) || !IsWaterTile(x, y - 1) || !IsWaterTile(x, y + 1));
        }

        private void AddNearShoreWaterGlint(GridPos pos, Vector3 center, bool horizontal, int seed)
        {
            // REFERENCE_IMAGE_SHALLOW_WATER_GLINTS brightens the river edge near grass and bridges.
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var side = ((seed & 8) == 0 ? -1f : 1f) * cellSize * 0.22f;
            var scale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.014f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.014f, cellSize * 0.3f);
            AddLooseCube(decorationObjects, "LowPolyNearShoreGlint", shoreMaterial != null ? shoreMaterial : windowMaterial, center + normal * side + new Vector3(0f, 0.024f, 0f), scale);
        }

        private void AddWaterlineShallowPatch(GridPos pos, int seed)
        {
            // REFERENCE_IMAGE_SHALLOW_RIVER_SHELF keeps water edges bright and readable from the isometric camera.
            var direction = ShallowWaterBankDirection(pos.X, pos.Y);
            if (direction == Vector2.zero)
            {
                return;
            }

            var center = CellCenter(pos, 0.046f);
            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var horizontal = Mathf.Abs(direction.y) > 0.01f;
            var shelfScale = horizontal
                ? new Vector3(cellSize * 0.46f, 0.014f, cellSize * 0.075f)
                : new Vector3(cellSize * 0.075f, 0.014f, cellSize * 0.46f);
            var sparkleScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.012f, cellSize * 0.025f)
                : new Vector3(cellSize * 0.025f, 0.012f, cellSize * 0.16f);
            AddLooseCube(decorationObjects, "LowPolyShallowRiverShelf", shoreMaterial != null ? shoreMaterial : windowMaterial, center + normal * cellSize * 0.34f, shelfScale);
            AddLooseCube(decorationObjects, "LowPolyShallowRiverFoamDash", windowMaterial, center + normal * cellSize * 0.24f + tangent * cellSize * 0.18f + new Vector3(0f, 0.014f, 0f), sparkleScale);
            if (seed % 2 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyShallowRiverFoamDash", windowMaterial, center + normal * cellSize * 0.2f - tangent * cellSize * 0.14f + new Vector3(0f, 0.018f, 0f), sparkleScale * 0.78f);
            }
        }

        private Vector2 ShallowWaterBankDirection(int x, int y)
        {
            if (!IsWaterTile(x - 1, y)) return new Vector2(-1f, 0f);
            if (!IsWaterTile(x + 1, y)) return new Vector2(1f, 0f);
            if (!IsWaterTile(x, y - 1)) return new Vector2(0f, -1f);
            if (!IsWaterTile(x, y + 1)) return new Vector2(0f, 1f);
            return Vector2.zero;
        }

        private void AddWaterTaxiCue(GridPos pos, Vector3 center, bool horizontal)
        {
            // REFERENCE_IMAGE_RIVER_LIFE adds sparse boats and wakes so the blue river feels active.
            var bodyScale = horizontal
                ? new Vector3(cellSize * 0.28f, 0.055f, cellSize * 0.11f)
                : new Vector3(cellSize * 0.11f, 0.055f, cellSize * 0.28f);
            var cabinScale = horizontal
                ? new Vector3(cellSize * 0.12f, 0.05f, cellSize * 0.08f)
                : new Vector3(cellSize * 0.08f, 0.05f, cellSize * 0.12f);
            var wakeScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.014f, cellSize * 0.026f)
                : new Vector3(cellSize * 0.026f, 0.014f, cellSize * 0.18f);
            var direction = horizontal ? Vector3.right : Vector3.forward;
            var normal = horizontal ? Vector3.forward : Vector3.right;
            var boatCenter = center + normal * ((((DecorationHash(pos.X, pos.Y) >> 4) & 1) == 0 ? -1f : 1f) * cellSize * 0.12f) + new Vector3(0f, 0.05f, 0f);
            AddLooseCube(decorationObjects, "LowPolyWaterTaxiBody", serviceNeedMaterial, boatCenter, bodyScale);
            AddLooseCube(decorationObjects, "LowPolyWaterTaxiCabin", windowMaterial, boatCenter + new Vector3(0f, 0.05f, 0f), cabinScale);
            AddLooseCube(decorationObjects, "LowPolyWaterTaxiWake", windowMaterial, boatCenter - direction * cellSize * 0.2f + normal * cellSize * 0.08f + new Vector3(0f, -0.02f, 0f), wakeScale);
            AddLooseCube(decorationObjects, "LowPolyWaterTaxiWake", windowMaterial, boatCenter - direction * cellSize * 0.2f - normal * cellSize * 0.08f + new Vector3(0f, -0.02f, 0f), wakeScale);
        }

        private void AddGrassGridCue(GridPos pos, int seed)
        {
            // LOW_POLY_GRASS_GRID_CUES echo the reference map's readable diagonal planning grid.
            var center = CellCenter(pos, 0.043f);
            var trim = (seed & 1) == 0 ? 0.5f : 0.38f;
            AddLooseCube(decorationObjects, "LowPolyGrassGridLine", grassGridMaterial, center + new Vector3(0f, 0f, -cellSize * 0.43f), new Vector3(cellSize * trim, 0.018f, 0.018f));
            AddLooseCube(decorationObjects, "LowPolyGrassGridLine", grassGridMaterial, center + new Vector3(-cellSize * 0.43f, 0f, 0f), new Vector3(0.018f, 0.018f, cellSize * trim));
            AddFreshGrassMosaicCue(pos, center, seed);
            AddGrassBioswaleDetail(center, seed);
            AddGrassParcelCornerMarks(pos, center, seed);
        }

        private void AddGrassParcelCornerMarks(GridPos pos, Vector3 center, int seed)
        {
            // REFERENCE_IMAGE_PARCEL_CORNER_MARKS gives empty lawns crisp buildable-lot edges.
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null || tile.Zone != ZoneType.None || tile.Terrain != TerrainType.Plain)
            {
                return;
            }

            var importantEdge = IsRoadsideSceneryTile(pos.X, pos.Y) || IsShorelineSceneryTile(pos.X, pos.Y);
            if (!importantEdge && seed % 2 != 0)
            {
                return;
            }

            var y = 0.072f + (((seed >> 4) & 1) == 0 ? 0f : 0.006f);
            var half = cellSize * 0.39f;
            var arm = cellSize * 0.18f;
            var thickness = Mathf.Max(0.018f, cellSize * 0.022f);
            var material = IsShorelineSceneryTile(pos.X, pos.Y) && shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            AddGrassParcelCornerMark(center, -1f, -1f, y, half, arm, thickness, material);
            AddGrassParcelCornerMark(center, 1f, 1f, y + 0.004f, half, arm, thickness, material);
            if (importantEdge && seed % 5 == 0)
            {
                AddGrassParcelCornerMark(center, -1f, 1f, y + 0.002f, half, arm * 0.82f, thickness, windowMaterial);
            }
        }

        private void AddGrassParcelCornerMark(Vector3 center, float signX, float signZ, float y, float half, float arm, float thickness, Material material)
        {
            var corner = new Vector3(center.x + signX * half, y, center.z + signZ * half);
            AddLooseCube(decorationObjects, "LowPolyGrassParcelCornerArm", material, corner + new Vector3(-signX * arm * 0.5f, 0f, 0f), new Vector3(arm, thickness, thickness));
            AddLooseCube(decorationObjects, "LowPolyGrassParcelCornerArm", material, corner + new Vector3(0f, 0.002f, -signZ * arm * 0.5f), new Vector3(thickness, thickness, arm));
            AddLooseCube(decorationObjects, "LowPolyGrassParcelCornerPin", windowMaterial, corner + new Vector3(-signX * arm * 0.18f, 0.026f, -signZ * arm * 0.18f), new Vector3(thickness * 1.6f, 0.034f, thickness * 1.6f));
        }

        private void AddFreshGrassMosaicCue(GridPos pos, Vector3 center, int seed)
        {
            // LOW_POLY_FRESH_GRASS_MOSAIC makes empty lawns feel brighter without changing the terrain mesh.
            var importantEdge = IsRoadsideSceneryTile(pos.X, pos.Y) || IsShorelineSceneryTile(pos.X, pos.Y);
            if (!importantEdge && seed % 2 != 0)
            {
                return;
            }

            var horizontal = (seed & 2) == 0;
            var side = ((seed >> 4) & 1) == 0 ? -1f : 1f;
            var patchOffset = horizontal
                ? new Vector3(side * cellSize * 0.18f, 0.024f, cellSize * 0.13f)
                : new Vector3(cellSize * 0.13f, 0.024f, side * cellSize * 0.18f);
            var patchScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.014f, cellSize * 0.13f)
                : new Vector3(cellSize * 0.13f, 0.014f, cellSize * 0.22f);
            var stitchScale = horizontal
                ? new Vector3(cellSize * 0.24f, 0.012f, cellSize * 0.018f)
                : new Vector3(cellSize * 0.018f, 0.012f, cellSize * 0.24f);
            AddLooseCube(decorationObjects, "LowPolyFreshGrassMosaicPatch", treeCanopyMaterial, center + patchOffset, patchScale);
            AddLooseCube(decorationObjects, "LowPolyFreshGrassMosaicSunStitch", roadLineMaterial, center - patchOffset * 0.58f + new Vector3(0f, 0.03f, 0f), stitchScale);
        }

        private void AddGrassCheckerRelief(GridPos pos, int seed)
        {
            // LOW_POLY_GRASS_CHECKER_RELIEF makes empty grass read as tiny stepped isometric tiles.
            var raised = ((pos.X + pos.Y) & 1) == 0;
            var center = CellCenter(pos, raised ? 0.052f : 0.044f);
            var longAxis = (seed & 2) == 0;
            var padScale = longAxis
                ? new Vector3(cellSize * 0.46f, 0.018f, cellSize * 0.3f)
                : new Vector3(cellSize * 0.3f, 0.018f, cellSize * 0.46f);
            var lipScale = longAxis
                ? new Vector3(cellSize * 0.42f, 0.014f, 0.026f)
                : new Vector3(0.026f, 0.014f, cellSize * 0.42f);
            AddLooseCube(decorationObjects, raised ? "LowPolyRaisedGrassTile" : "LowPolyInsetGrassTile", grassGridMaterial, center, padScale);
            AddLooseCube(decorationObjects, "LowPolyGrassCheckerLip", shoreMaterial != null ? shoreMaterial : roadLineMaterial, center + new Vector3(0f, 0.02f, raised ? -cellSize * 0.16f : cellSize * 0.16f), lipScale);

            if (seed % 8 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyGrassCheckerFlowerDot", serviceNeedMaterial, center + new Vector3(cellSize * 0.18f, 0.05f, cellSize * 0.12f), new Vector3(0.07f, 0.038f, 0.07f));
            }
        }

        private void AddGrassBioswaleDetail(Vector3 center, int seed)
        {
            // CITY_SKYLINES_GREEN_RELIEF_DETAIL makes idle green tiles read as managed stormwater space.
            if (seed % 4 != 0)
            {
                return;
            }

            var horizontal = (seed & 2) == 0;
            var basinScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.018f, cellSize * 0.13f)
                : new Vector3(cellSize * 0.13f, 0.018f, cellSize * 0.34f);
            var reedScale = horizontal
                ? new Vector3(cellSize * 0.045f, 0.13f, cellSize * 0.11f)
                : new Vector3(cellSize * 0.11f, 0.13f, cellSize * 0.045f);
            AddLooseCube(decorationObjects, "LowPolyGreenReliefBasin", windowMaterial, center + new Vector3(cellSize * 0.18f, 0.028f, cellSize * 0.14f), basinScale);
            AddLooseCube(decorationObjects, "LowPolyGreenReliefReed", treeCanopyMaterial, center + new Vector3(cellSize * 0.03f, 0.12f, cellSize * 0.2f), reedScale);
            AddLooseCube(decorationObjects, "LowPolyGreenReliefSurveyPip", roadLineMaterial, center + new Vector3(-cellSize * 0.24f, 0.056f, cellSize * 0.22f), new Vector3(0.075f, 0.035f, 0.075f));
        }

        private void AddRoadEdgePocketPlaza(GridPos pos, int seed)
        {
            // REFERENCE_IMAGE_ROAD_EDGE_POCKET_PLAZAS adds small sunny plazas beside road edges.
            var center = CellCenter(pos, 0.06f);
            var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y);
            var side = ((seed >> 3) & 1) == 0 ? -1f : 1f;
            var paverMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var paverScale = horizontal
                ? new Vector3(cellSize * 0.58f, 0.026f, cellSize * 0.34f)
                : new Vector3(cellSize * 0.34f, 0.026f, cellSize * 0.58f);
            var lawnScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.024f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.024f, cellSize * 0.22f);
            var pathScale = horizontal
                ? new Vector3(cellSize * 0.44f, 0.018f, 0.038f)
                : new Vector3(0.038f, 0.018f, cellSize * 0.44f);
            var benchScale = horizontal
                ? new Vector3(cellSize * 0.22f, 0.045f, 0.06f)
                : new Vector3(0.06f, 0.045f, cellSize * 0.22f);
            var lawnOffset = horizontal
                ? new Vector3(0f, 0.024f, side * cellSize * 0.11f)
                : new Vector3(side * cellSize * 0.11f, 0.024f, 0f);
            var benchOffset = horizontal
                ? new Vector3(-cellSize * 0.16f, 0.075f, -side * cellSize * 0.16f)
                : new Vector3(-side * cellSize * 0.16f, 0.075f, -cellSize * 0.16f);
            var treeOffset = horizontal
                ? new Vector3(cellSize * 0.22f, 0f, -side * cellSize * 0.18f)
                : new Vector3(-side * cellSize * 0.18f, 0f, cellSize * 0.22f);

            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketPaver", paverMaterial, center, paverScale);
            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketLawn", grassGridMaterial, center + lawnOffset, lawnScale);
            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketPath", roadLineMaterial, center + new Vector3(0f, 0.028f, 0f), pathScale);
            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketFlower", serviceNeedMaterial, center - lawnOffset * 0.65f + new Vector3(0f, 0.045f, 0f), lawnScale * 0.58f);
            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketBench", serviceMaterial, center + benchOffset, benchScale);
            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketTreeTrunk", treeTrunkMaterial, center + treeOffset + new Vector3(0f, 0.1f, 0f), new Vector3(0.045f, 0.2f, 0.045f));
            AddLooseCube(decorationObjects, "LowPolyRoadEdgePocketTreeCanopy", treeCanopyMaterial, center + treeOffset + new Vector3(0f, 0.25f, 0f), new Vector3(0.18f, 0.16f, 0.18f));
        }

        private void AddPocketParkingMarkings(GridPos pos, int seed)
        {
            // LOW_POLY_POCKET_PARKING_MARKINGS add tiny roadside stalls for parking readability.
            var center = CellCenter(pos, 0.058f);
            var roadNormal = AdjacentRoadNormal(pos);
            var alongX = Mathf.Abs(roadNormal.z) > 0.01f || roadNormal == Vector3.zero;
            var padScale = alongX
                ? new Vector3(cellSize * 0.62f, 0.024f, cellSize * 0.42f)
                : new Vector3(cellSize * 0.42f, 0.024f, cellSize * 0.62f);
            var stallLineScale = alongX
                ? new Vector3(0.026f, 0.018f, cellSize * 0.3f)
                : new Vector3(cellSize * 0.3f, 0.018f, 0.026f);
            var aisleScale = alongX
                ? new Vector3(cellSize * 0.52f, 0.018f, 0.036f)
                : new Vector3(0.036f, 0.018f, cellSize * 0.52f);
            AddLooseCube(decorationObjects, "LowPolyPocketParkingPad", roadMaterial, center, padScale);
            for (var i = -1; i <= 1; i += 1)
            {
                var offset = i * cellSize * 0.14f;
                AddLooseCube(decorationObjects, "LowPolyPocketParkingBayLine", roadLineMaterial, center + (alongX ? new Vector3(offset, 0.03f, 0f) : new Vector3(0f, 0.03f, offset)), stallLineScale);
            }

            AddLooseCube(decorationObjects, "LowPolyPocketParkingAisleLine", windowMaterial, center + roadNormal * cellSize * 0.12f + new Vector3(0f, 0.034f, 0f), aisleScale);
            if (seed % 3 == 0)
            {
                var pylonOffset = alongX ? new Vector3(-cellSize * 0.24f, 0f, -cellSize * 0.14f) : new Vector3(-cellSize * 0.14f, 0f, -cellSize * 0.24f);
                AddLooseCube(decorationObjects, "LowPolyPocketParkingSignPost", serviceMaterial, center + pylonOffset + new Vector3(0f, 0.14f, 0f), new Vector3(0.035f, 0.24f, 0.035f));
                AddLooseCube(decorationObjects, "LowPolyPocketParkingSignPlate", roadLineMaterial, center + pylonOffset + new Vector3(0f, 0.27f, 0f), new Vector3(0.13f, 0.09f, 0.035f));
            }
        }

        private Vector3 AdjacentRoadNormal(GridPos pos)
        {
            if (HasRoadTile(pos.X, pos.Y - 1)) return Vector3.back;
            if (HasRoadTile(pos.X, pos.Y + 1)) return Vector3.forward;
            if (HasRoadTile(pos.X - 1, pos.Y)) return Vector3.left;
            if (HasRoadTile(pos.X + 1, pos.Y)) return Vector3.right;
            return Vector3.zero;
        }

        private void AddFreshLawnPocket(GridPos pos, int seed)
        {
            // REFERENCE_IMAGE_FRESH_LAWN_POCKETS breaks up empty grass with bright tiny park details.
            var center = CellCenter(pos, 0.052f);
            var horizontal = (seed & 1) == 0;
            var patchScale = horizontal
                ? new Vector3(cellSize * 0.56f, 0.022f, cellSize * 0.34f)
                : new Vector3(cellSize * 0.34f, 0.022f, cellSize * 0.56f);
            var pathScale = horizontal
                ? new Vector3(cellSize * 0.38f, 0.018f, 0.035f)
                : new Vector3(0.035f, 0.018f, cellSize * 0.38f);
            var flowerOffset = horizontal
                ? new Vector3(cellSize * 0.18f, 0.034f, cellSize * 0.1f)
                : new Vector3(cellSize * 0.1f, 0.034f, cellSize * 0.18f);
            var shrubOffset = horizontal
                ? new Vector3(-cellSize * 0.18f, 0.08f, -cellSize * 0.1f)
                : new Vector3(-cellSize * 0.1f, 0.08f, -cellSize * 0.18f);

            AddLooseCube(decorationObjects, "LowPolyFreshLawnPatch", grassGridMaterial, center, patchScale);
            AddLooseCube(decorationObjects, "LowPolyFreshLawnPath", roadLineMaterial, center + new Vector3(0f, 0.026f, 0f), pathScale);
            AddLooseCube(decorationObjects, "LowPolyFreshLawnFlowerBed", serviceNeedMaterial, center + flowerOffset, new Vector3(cellSize * 0.16f, 0.045f, cellSize * 0.1f));
            AddLooseCube(decorationObjects, "LowPolyFreshLawnShrub", treeCanopyMaterial, center + shrubOffset, new Vector3(0.14f, 0.13f, 0.14f));
            if (seed % 3 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyFreshLawnGlint", windowMaterial, center - flowerOffset * 0.7f + new Vector3(0f, 0.04f, 0f), new Vector3(cellSize * 0.12f, 0.018f, cellSize * 0.045f));
            }
        }

        private bool IsOpenSceneryTile(int x, int y)
        {
            var tile = controller.GetTile(x, y);
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(tile.RoadId) || !string.IsNullOrEmpty(tile.BuildingId))
            {
                return false;
            }

            return x > 1 && y > 1 && x < controller.Grid.Width - 2 && y < controller.Grid.Height - 2;
        }

        private static bool IsUnbuiltZonedSceneryTile(TileData tile)
        {
            return tile != null
                && tile.Terrain != TerrainType.Water
                && tile.Zone != ZoneType.None
                && string.IsNullOrEmpty(tile.BuildingId)
                && string.IsNullOrEmpty(tile.RoadId);
        }

        private static bool IsVacantDevelopmentTile(TileData tile)
        {
            return tile != null
                && tile.Terrain != TerrainType.Water
                && string.IsNullOrEmpty(tile.BuildingId)
                && string.IsNullOrEmpty(tile.RoadId);
        }

        private void AddZoneParcelCue(GridPos pos, ZoneType zone, int seed)
        {
            // CITY_PLANNING_ZONE_PARCEL_CUES makes undeveloped zoning read like planned city parcels.
            var material = MaterialForZone(zone);
            var center = CellCenter(pos, 0.052f);
            AddLooseCube(decorationObjects, "LowPolyZoneFootprintPad", buildingFootprintMaterial, center + new Vector3(0f, -0.022f, 0f), new Vector3(cellSize * 0.62f, 0.018f, cellSize * 0.62f));
            AddLooseCube(decorationObjects, "LowPolyZoneBuildIntentPad", grassGridMaterial, center + new Vector3(cellSize * 0.12f, 0.006f, cellSize * 0.1f), new Vector3(cellSize * 0.32f, 0.018f, cellSize * 0.22f));
            AddLooseCube(decorationObjects, "LowPolyZoneParcelEdge", material, center + new Vector3(0f, 0f, -cellSize * 0.36f), new Vector3(cellSize * 0.54f, 0.026f, 0.032f));
            AddLooseCube(decorationObjects, "LowPolyZoneParcelEdge", material, center + new Vector3(-cellSize * 0.36f, 0f, 0f), new Vector3(0.032f, 0.026f, cellSize * 0.54f));
            AddZoneDistrictBoundaryCues(pos, zone, seed, center, material);
            AddZoneConstructionFence(pos, zone, seed, center, material);
            AddZoneParcelLotNumberPlaque(pos, zone, seed, center, material);
            AddZoneServiceHotspotFlag(zone, seed, center, material);
            if (seed % 5 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyZoneParcelStake", material, center + new Vector3(-cellSize * 0.34f, 0.06f, -cellSize * 0.34f), new Vector3(0.06f, 0.13f, 0.06f));
                AddLooseCube(decorationObjects, "LowPolyZoneParcelFlag", roadLineMaterial, center + new Vector3(-cellSize * 0.29f, 0.15f, -cellSize * 0.31f), new Vector3(0.16f, 0.055f, 0.035f));
            }

            AddZoneParcelIntentDetail(zone, seed, center, material);
            AddZoneParcelPermitCue(zone, seed, center, material);
            AddUnbuiltLotStatusMarker(zone, seed, center, material);
            AddOpenLotDevelopmentPotentialDecor(pos, controller.GetTile(pos.X, pos.Y), seed, center);
        }

        private void AddZoneConstructionFence(GridPos pos, ZoneType zone, int seed, Vector3 center, Material material)
        {
            // CITY_SKYLINES_ACTIVE_LOT_FENCING turns approved empty parcels into visible build sites.
            var fenceMaterial = zone == ZoneType.Civic || zone == ZoneType.Utility ? windowMaterial : serviceNeedMaterial;
            var accentMaterial = zone == ZoneType.Industrial ? serviceMaterial : material;
            AddLooseCube(decorationObjects, "ZoneConstructionFenceRail", fenceMaterial, center + new Vector3(0f, 0.112f, -cellSize * 0.43f), new Vector3(cellSize * 0.62f, 0.046f, 0.034f));
            AddLooseCube(decorationObjects, "ZoneConstructionFenceRail", fenceMaterial, center + new Vector3(0f, 0.116f, cellSize * 0.43f), new Vector3(cellSize * 0.62f, 0.046f, 0.034f));
            AddLooseCube(decorationObjects, "ZoneConstructionFenceColorBand", accentMaterial, center + new Vector3(0f, 0.148f, -cellSize * 0.43f), new Vector3(cellSize * 0.42f, 0.018f, 0.038f));

            if (seed % 3 != 1)
            {
                AddLooseCube(decorationObjects, "ZoneConstructionSideRail", fenceMaterial, center + new Vector3(-cellSize * 0.43f, 0.114f, 0f), new Vector3(0.034f, 0.046f, cellSize * 0.46f));
                AddLooseCube(decorationObjects, "ZoneConstructionSideBand", accentMaterial, center + new Vector3(-cellSize * 0.43f, 0.148f, 0f), new Vector3(0.038f, 0.018f, cellSize * 0.3f));
            }
            else
            {
                AddLooseCube(decorationObjects, "ZoneConstructionSideRail", fenceMaterial, center + new Vector3(cellSize * 0.43f, 0.114f, 0f), new Vector3(0.034f, 0.046f, cellSize * 0.46f));
                AddLooseCube(decorationObjects, "ZoneConstructionSideBand", accentMaterial, center + new Vector3(cellSize * 0.43f, 0.148f, 0f), new Vector3(0.038f, 0.018f, cellSize * 0.3f));
            }

            AddZoneConstructionFencePost(center, fenceMaterial, -1f, -1f);
            AddZoneConstructionFencePost(center, fenceMaterial, 1f, -1f);
            AddZoneConstructionFencePost(center, fenceMaterial, -1f, 1f);
            AddZoneConstructionFencePost(center, fenceMaterial, 1f, 1f);
            AddLooseCubeRotated(decorationObjects, "ZoneConstructionSurveyTape", roadLineMaterial, center + new Vector3(0f, 0.086f, 0f), new Vector3(cellSize * 0.52f, 0.018f, 0.026f), (seed & 2) == 0 ? 34f : -34f);

            if (IsRoadsideSceneryTile(pos.X, pos.Y))
            {
                AddZoneConstructionGate(pos, center, fenceMaterial);
            }

            if (seed % 3 == 0 || zone == ZoneType.Industrial || zone == ZoneType.Utility)
            {
                AddZoneConstructionMaterialStack(zone, seed, center, material);
            }
        }

        private void AddZoneConstructionFencePost(Vector3 center, Material material, float signX, float signZ)
        {
            var postCenter = center + new Vector3(signX * cellSize * 0.43f, 0.13f, signZ * cellSize * 0.43f);
            AddLooseCube(decorationObjects, "ZoneConstructionFencePost", material, postCenter, new Vector3(0.055f, 0.16f, 0.055f));
            AddLooseCube(decorationObjects, "ZoneConstructionFencePostCap", roadLineMaterial, postCenter + new Vector3(0f, 0.09f, 0f), new Vector3(0.09f, 0.035f, 0.09f));
        }

        private void AddZoneConstructionGate(GridPos pos, Vector3 center, Material material)
        {
            if (HasRoadTile(pos.X, pos.Y - 1))
            {
                AddZoneConstructionGateEdge(center, true, -1f, material);
                return;
            }

            if (HasRoadTile(pos.X, pos.Y + 1))
            {
                AddZoneConstructionGateEdge(center, true, 1f, material);
                return;
            }

            if (HasRoadTile(pos.X - 1, pos.Y))
            {
                AddZoneConstructionGateEdge(center, false, -1f, material);
                return;
            }

            if (HasRoadTile(pos.X + 1, pos.Y))
            {
                AddZoneConstructionGateEdge(center, false, 1f, material);
            }
        }

        private void AddZoneConstructionGateEdge(Vector3 center, bool horizontalEdge, float sign, Material material)
        {
            var gateCenter = center + (horizontalEdge
                ? new Vector3(0f, 0.164f, sign * cellSize * 0.43f)
                : new Vector3(sign * cellSize * 0.43f, 0.164f, 0f));
            var gateScale = horizontalEdge
                ? new Vector3(cellSize * 0.28f, 0.052f, 0.04f)
                : new Vector3(0.04f, 0.052f, cellSize * 0.28f);
            var stripeScale = horizontalEdge
                ? new Vector3(cellSize * 0.16f, 0.02f, 0.045f)
                : new Vector3(0.045f, 0.02f, cellSize * 0.16f);
            AddLooseCube(decorationObjects, "ZoneConstructionGatePanel", material, gateCenter, gateScale);
            AddLooseCube(decorationObjects, "ZoneConstructionGateStripe", roadLineMaterial, gateCenter + new Vector3(0f, 0.036f, 0f), stripeScale);
        }

        private void AddZoneConstructionMaterialStack(ZoneType zone, int seed, Vector3 center, Material material)
        {
            var sideX = ((seed >> 4) & 1) == 0 ? 1f : -1f;
            var sideZ = ((seed >> 5) & 1) == 0 ? 1f : -1f;
            var stackCenter = center + new Vector3(sideX * cellSize * 0.2f, 0.074f, sideZ * cellSize * 0.18f);
            var stackMaterial = zone == ZoneType.Residential ? roofMaterial : material;
            AddLooseCube(decorationObjects, "ZoneConstructionMaterialStack", stackMaterial, stackCenter, new Vector3(cellSize * 0.22f, 0.09f, cellSize * 0.16f));
            AddLooseCube(decorationObjects, "ZoneConstructionMaterialTop", roadLineMaterial, stackCenter + new Vector3(0f, 0.068f, 0f), new Vector3(cellSize * 0.24f, 0.025f, cellSize * 0.18f));
            AddLooseCube(decorationObjects, "ZoneConstructionPipeBundle", utilityMaterial, stackCenter + new Vector3(-sideX * cellSize * 0.18f, 0.012f, 0f), new Vector3(cellSize * 0.18f, 0.04f, 0.055f));
        }

        private void AddOpenLotDevelopmentPotentialDecor(GridPos pos, TileData tile, int seed, Vector3 center)
        {
            // CITY_SKYLINES_VACANT_LOT_POTENTIAL adds small build-readiness cues to empty parcels.
            if (!IsVacantDevelopmentTile(tile))
            {
                return;
            }

            var score = OpenLotDevelopmentPotentialScore(pos, tile);
            if (score < 36 && seed % 3 != 0)
            {
                return;
            }

            var material = OpenLotPotentialMaterial(tile, score);
            var scoreMaterial = score >= 70 ? windowMaterial : (score >= 44 ? serviceNeedMaterial : roadLineMaterial);
            var highValue = score >= 64 || tile.Zone != ZoneType.None;
            var offset = highValue
                ? new Vector3(-cellSize * 0.22f, 0.058f, -cellSize * 0.2f)
                : new Vector3(cellSize * 0.18f, 0.056f, cellSize * 0.18f);
            var baseCenter = center + offset;
            AddLooseCube(decorationObjects, "OpenLotPotentialBlueprint", material, baseCenter, new Vector3(cellSize * 0.28f, 0.022f, cellSize * 0.18f));
            AddLooseCube(decorationObjects, "OpenLotPotentialGridLine", roadLineMaterial, baseCenter + new Vector3(0f, 0.024f, -cellSize * 0.055f), new Vector3(cellSize * 0.22f, 0.018f, 0.026f));
            AddLooseCube(decorationObjects, "OpenLotPotentialGridLine", roadLineMaterial, baseCenter + new Vector3(-cellSize * 0.085f, 0.026f, 0f), new Vector3(0.026f, 0.018f, cellSize * 0.13f));
            AddPotentialScorePips(decorationObjects, "OpenLotPotential", baseCenter + new Vector3(cellSize * 0.03f, 0.048f, cellSize * 0.08f), score, scoreMaterial);

            if (score >= 58)
            {
                AddLooseCube(decorationObjects, "OpenLotPotentialSurveyStake", material, baseCenter + new Vector3(cellSize * 0.17f, 0.105f, -cellSize * 0.1f), new Vector3(0.038f, 0.18f, 0.038f));
                AddLooseCube(decorationObjects, "OpenLotPotentialFlag", scoreMaterial, baseCenter + new Vector3(cellSize * 0.235f, 0.2f, -cellSize * 0.1f), new Vector3(cellSize * 0.13f, 0.055f, 0.032f));
            }

            if (score >= 76)
            {
                AddLooseCube(decorationObjects, "OpenLotPotentialMiniCraneMast", material, baseCenter + new Vector3(-cellSize * 0.16f, 0.135f, cellSize * 0.09f), new Vector3(0.036f, 0.24f, 0.036f));
                AddLooseCube(decorationObjects, "OpenLotPotentialMiniCraneArm", scoreMaterial, baseCenter + new Vector3(-cellSize * 0.06f, 0.25f, cellSize * 0.09f), new Vector3(cellSize * 0.22f, 0.03f, 0.03f));
            }
        }

        private int OpenLotDevelopmentPotentialScore(GridPos pos, TileData tile)
        {
            if (!IsVacantDevelopmentTile(tile))
            {
                return 0;
            }

            var metrics = controller != null ? controller.Metrics : null;
            var score = tile.LandValue / 2
                + ServiceAccessValue(tile) / 4
                + tile.TransitAccess / 5
                + Mathf.Max(tile.LogisticsAccess, tile.ParkingAccess) / 8
                - tile.Traffic / 5
                - PollutionStress(tile) / 8;
            if (controller != null && controller.Grid != null && IsRoadsideSceneryTile(pos.X, pos.Y))
            {
                score += 18;
            }

            if (tile.Zone != ZoneType.None)
            {
                score = Mathf.Max(score, ZoneOpportunityScore(tile.Zone, metrics));
            }
            else if (metrics != null && metrics.Demand != null)
            {
                var demand = metrics.Demand;
                var bestDemand = Mathf.Max(demand.Residential, Mathf.Max(demand.Commercial, Mathf.Max(demand.Industrial, Mathf.Max(demand.Office, demand.MixedUse))));
                score += bestDemand / 4;
            }

            return Mathf.Clamp(score, 0, 100);
        }

        private Material OpenLotPotentialMaterial(TileData tile, int score)
        {
            if (tile != null && tile.Zone != ZoneType.None)
            {
                return MaterialForZone(tile.Zone);
            }

            if (score >= 70)
            {
                return windowMaterial;
            }

            if (score >= 44)
            {
                return InspectDemandMaterial(controller != null ? controller.Metrics : null);
            }

            return grassGridMaterial;
        }

        private void AddPotentialScorePips(List<GameObject> objects, string prefix, Vector3 center, int score, Material material)
        {
            var count = score >= 84 ? 4 : (score >= 64 ? 3 : (score >= 42 ? 2 : 1));
            for (var i = 0; i < count; i += 1)
            {
                var offset = (i - (count - 1) * 0.5f) * 0.056f;
                AddLooseCube(objects, prefix + "ScorePip", i == count - 1 && score >= 76 ? windowMaterial : material, center + new Vector3(offset, i * 0.012f, 0f), new Vector3(0.035f, 0.04f + i * 0.006f, 0.035f));
            }
        }

        private void AddZoneDistrictBoundaryCues(GridPos pos, ZoneType zone, int seed, Vector3 center, Material material)
        {
            // CITY_SKYLINES_DISTRICT_BOUNDARY_CUES adds crisp survey edges where planned zones change.
            var added = 0;
            if (IsZoneBoundaryEdge(pos.X, pos.Y - 1, zone))
            {
                AddZoneDistrictBoundaryEdge(center, new Vector2(0f, -1f), material, seed + added * 13);
                added += 1;
            }

            if (IsZoneBoundaryEdge(pos.X, pos.Y + 1, zone))
            {
                AddZoneDistrictBoundaryEdge(center, new Vector2(0f, 1f), material, seed + added * 13);
                added += 1;
            }

            if (IsZoneBoundaryEdge(pos.X - 1, pos.Y, zone))
            {
                AddZoneDistrictBoundaryEdge(center, new Vector2(-1f, 0f), material, seed + added * 13);
                added += 1;
            }

            if (IsZoneBoundaryEdge(pos.X + 1, pos.Y, zone))
            {
                AddZoneDistrictBoundaryEdge(center, new Vector2(1f, 0f), material, seed + added * 13);
                added += 1;
            }

            if (added > 1)
            {
                var pinOffset = new Vector3(cellSize * 0.34f, 0.075f, cellSize * 0.34f);
                AddLooseCube(decorationObjects, "ZoneBoundaryCornerSurveyPin", roadLineMaterial, center + pinOffset, new Vector3(0.075f, 0.12f, 0.075f));
                AddLooseCube(decorationObjects, "ZoneBoundaryCornerSurveyCap", material, center + pinOffset + new Vector3(0f, 0.08f, 0f), new Vector3(0.13f, 0.035f, 0.13f));
            }
        }

        private bool IsZoneBoundaryEdge(int x, int y, ZoneType zone)
        {
            var neighbor = controller.GetTile(x, y);
            if (neighbor == null)
            {
                return false;
            }

            if (neighbor.Terrain == TerrainType.Water || !string.IsNullOrEmpty(neighbor.RoadId))
            {
                return true;
            }

            return neighbor.Zone != zone;
        }

        private void AddZoneDistrictBoundaryEdge(Vector3 center, Vector2 direction, Material material, int seed)
        {
            var edgeOffset = new Vector3(direction.x * cellSize * 0.43f, 0.028f, direction.y * cellSize * 0.43f);
            var alongHorizontal = Mathf.Abs(direction.y) > 0.01f;
            var railScale = alongHorizontal
                ? new Vector3(cellSize * 0.72f, 0.022f, 0.026f)
                : new Vector3(0.026f, 0.022f, cellSize * 0.72f);
            var tickScale = alongHorizontal
                ? new Vector3(0.035f, 0.035f, cellSize * 0.09f)
                : new Vector3(cellSize * 0.09f, 0.035f, 0.035f);
            AddLooseCube(decorationObjects, "ZoneDistrictBoundaryRail", roadLineMaterial, center + edgeOffset, railScale);
            AddLooseCube(decorationObjects, "ZoneDistrictBoundaryColorBand", material, center + edgeOffset + new Vector3(0f, 0.018f, 0f), railScale * 0.62f);
            AddZoneBoundaryPriorityPips(center, direction, material, seed);

            var tangent = alongHorizontal ? Vector3.right : Vector3.forward;
            var count = (seed & 1) == 0 ? 2 : 3;
            for (var i = 0; i < count; i += 1)
            {
                var step = (i - (count - 1) * 0.5f) * cellSize * 0.22f;
                AddLooseCube(decorationObjects, "ZoneDistrictBoundaryTick", roadLineMaterial, center + edgeOffset + tangent * step + new Vector3(0f, 0.042f, 0f), tickScale);
            }
        }

        private void AddZoneBoundaryPriorityPips(Vector3 center, Vector2 direction, Material material, int seed)
        {
            // CITY_SKYLINES_CLEAN_DISTRICT_EDGE_PIPS make zone transitions readable without new data layers.
            if (seed % 2 != 0)
            {
                return;
            }

            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var baseCenter = center + normal * cellSize * 0.43f + new Vector3(0f, 0.07f, 0f);
            var pipScale = new Vector3(0.07f, 0.055f, 0.07f);
            AddLooseCube(decorationObjects, "ZoneBoundaryPriorityPip", material, baseCenter + tangent * cellSize * 0.22f, pipScale);
            AddLooseCube(decorationObjects, "ZoneBoundaryPriorityPip", roadLineMaterial, baseCenter - tangent * cellSize * 0.22f, pipScale * 0.82f);
        }

        private void AddZoneParcelLotNumberPlaque(GridPos pos, ZoneType zone, int seed, Vector3 center, Material material)
        {
            // LOW_POLY_LOT_NUMBER_PLAQUES give planned parcels the tiny readable labels from city-builder maps.
            if (seed % 3 != 0)
            {
                return;
            }

            var side = (seed & 8) == 0 ? -1f : 1f;
            var plaqueCenter = center + new Vector3(side * cellSize * 0.25f, 0.084f, -cellSize * 0.23f);
            var plaqueMaterial = zone == ZoneType.Civic || zone == ZoneType.Utility ? serviceNeedMaterial : roadLineMaterial;
            AddLooseCube(decorationObjects, "ZoneLotNumberPlaqueBase", plaqueMaterial, plaqueCenter, new Vector3(cellSize * 0.22f, 0.032f, cellSize * 0.14f));
            AddLooseCube(decorationObjects, "ZoneLotNumberPlaqueInk", material, plaqueCenter + new Vector3(0f, 0.03f, 0f), new Vector3(cellSize * 0.14f, 0.02f, 0.026f));

            var digitCount = 1 + Mathf.Abs(pos.X * 17 + pos.Y * 11) % 3;
            for (var i = 0; i < digitCount; i += 1)
            {
                var offset = (i - (digitCount - 1) * 0.5f) * cellSize * 0.055f;
                AddLooseCube(decorationObjects, "ZoneLotNumberPlaqueDigit", windowMaterial, plaqueCenter + new Vector3(offset, 0.055f, cellSize * 0.035f), new Vector3(0.026f, 0.026f, 0.026f));
            }
        }

        private void AddZoneServiceHotspotFlag(ZoneType zone, int seed, Vector3 center, Material material)
        {
            // LOW_POLY_SERVICE_HOTSPOT_FLAGS mark civic and utility parcels as planned service anchors.
            if (zone != ZoneType.Civic && zone != ZoneType.Utility && !(zone == ZoneType.MixedUse && seed % 7 == 0))
            {
                return;
            }

            var flagMaterial = zone == ZoneType.Utility ? windowMaterial : serviceNeedMaterial;
            var flagCenter = center + new Vector3(cellSize * 0.28f, 0f, cellSize * 0.26f);
            AddLooseCube(decorationObjects, "ZoneServiceHotspotFlagPost", material, flagCenter + new Vector3(0f, 0.16f, 0f), new Vector3(0.04f, 0.3f, 0.04f));
            AddLooseCube(decorationObjects, "ZoneServiceHotspotFlag", flagMaterial, flagCenter + new Vector3(cellSize * 0.08f, 0.29f, 0f), new Vector3(cellSize * 0.17f, 0.07f, 0.035f));
            AddLooseCube(decorationObjects, "ZoneServiceHotspotFlagDot", roadLineMaterial, flagCenter + new Vector3(cellSize * 0.17f, 0.33f, 0f), new Vector3(0.055f, 0.035f, 0.035f));
        }

        private void AddZoneParcelPermitCue(ZoneType zone, int seed, Vector3 center, Material material)
        {
            // CITY_PLANNING_PERMIT_CUES make empty zones feel like approved lots waiting for construction.
            if (seed % 4 != 0)
            {
                return;
            }

            var offset = new Vector3(-cellSize * 0.2f, 0f, cellSize * 0.24f);
            AddLooseCube(decorationObjects, "ZonePermitPost", roadLineMaterial, center + offset + new Vector3(0f, 0.13f, 0f), new Vector3(0.035f, 0.24f, 0.035f));
            AddLooseCube(decorationObjects, "ZonePermitBoard", serviceNeedMaterial, center + offset + new Vector3(cellSize * 0.07f, 0.25f, 0f), new Vector3(cellSize * 0.18f, 0.09f, 0.035f));

            if (zone == ZoneType.Industrial || zone == ZoneType.Office || zone == ZoneType.MixedUse || zone == ZoneType.Commercial)
            {
                var craneBase = center + new Vector3(cellSize * 0.24f, 0f, -cellSize * 0.2f);
                AddLooseCube(decorationObjects, "ZonePermitMiniCraneMast", material, craneBase + new Vector3(0f, 0.15f, 0f), new Vector3(0.04f, 0.3f, 0.04f));
                AddLooseCube(decorationObjects, "ZonePermitMiniCraneArm", serviceNeedMaterial, craneBase + new Vector3(-cellSize * 0.1f, 0.29f, 0f), new Vector3(cellSize * 0.28f, 0.035f, 0.035f));
                AddLooseCube(decorationObjects, "ZonePermitMiniCraneHook", roadLineMaterial, craneBase + new Vector3(-cellSize * 0.22f, 0.2f, 0f), new Vector3(0.03f, 0.16f, 0.03f));
                return;
            }

            AddLooseCube(decorationObjects, "ZonePermitGardenStake", treeCanopyMaterial, center + new Vector3(cellSize * 0.2f, 0.1f, -cellSize * 0.2f), new Vector3(0.11f, 0.18f, 0.11f));
        }

        private void AddZoneParcelIntentDetail(ZoneType zone, int seed, Vector3 center, Material material)
        {
            // REFERENCE_IMAGE_ZONE_INTENT_MINIS make empty zoning read as planned low-poly development.
            var offset = new Vector3(cellSize * 0.15f, 0f, cellSize * 0.12f);
            if ((seed & 4) != 0)
            {
                offset = new Vector3(-cellSize * 0.12f, 0f, cellSize * 0.15f);
            }

            if (zone == ZoneType.Residential)
            {
                AddLooseCube(decorationObjects, "ZoneIntentHomeBody", material, center + offset + new Vector3(0f, 0.1f, 0f), new Vector3(cellSize * 0.24f, 0.16f, cellSize * 0.2f));
                AddLooseCube(decorationObjects, "ZoneIntentHomeRoof", roofMaterial, center + offset + new Vector3(0f, 0.2f, 0f), new Vector3(cellSize * 0.3f, 0.06f, cellSize * 0.24f));
                return;
            }

            if (zone == ZoneType.Commercial || zone == ZoneType.MixedUse)
            {
                AddLooseCube(decorationObjects, "ZoneIntentStorefront", material, center + offset + new Vector3(0f, 0.09f, 0f), new Vector3(cellSize * 0.26f, 0.14f, cellSize * 0.2f));
                AddLooseCube(decorationObjects, "ZoneIntentAwning", windowMaterial, center + offset + new Vector3(0f, 0.18f, -cellSize * 0.08f), new Vector3(cellSize * 0.28f, 0.04f, cellSize * 0.08f));
                return;
            }

            if (zone == ZoneType.Industrial)
            {
                AddLooseCube(decorationObjects, "ZoneIntentWorkshop", material, center + offset + new Vector3(0f, 0.09f, 0f), new Vector3(cellSize * 0.28f, 0.14f, cellSize * 0.22f));
                AddLooseCube(decorationObjects, "ZoneIntentChimney", serviceMaterial, center + offset + new Vector3(cellSize * 0.09f, 0.22f, -cellSize * 0.04f), new Vector3(0.06f, 0.24f, 0.06f));
                return;
            }

            if (zone == ZoneType.Civic)
            {
                AddLooseCube(decorationObjects, "ZoneIntentCivicFlagPost", roadLineMaterial, center + offset + new Vector3(-cellSize * 0.08f, 0.18f, 0f), new Vector3(0.05f, 0.28f, 0.05f));
                AddLooseCube(decorationObjects, "ZoneIntentCivicFlag", windowMaterial, center + offset + new Vector3(cellSize * 0.02f, 0.28f, 0f), new Vector3(cellSize * 0.18f, 0.07f, 0.035f));
                return;
            }

            if (zone == ZoneType.Utility)
            {
                AddLooseCube(decorationObjects, "ZoneIntentUtilityPad", utilityMaterial, center + offset + new Vector3(0f, 0.08f, 0f), new Vector3(cellSize * 0.24f, 0.12f, cellSize * 0.24f));
                AddLooseCube(decorationObjects, "ZoneIntentUtilityLamp", windowMaterial, center + offset + new Vector3(cellSize * 0.12f, 0.19f, 0f), new Vector3(0.07f, 0.13f, 0.07f));
                return;
            }

            if (zone == ZoneType.Office)
            {
                AddLooseCube(decorationObjects, "ZoneIntentOfficeCore", material, center + offset + new Vector3(0f, 0.12f, 0f), new Vector3(cellSize * 0.22f, 0.22f, cellSize * 0.2f));
                AddLooseCube(decorationObjects, "ZoneIntentOfficeGlint", windowMaterial, center + offset + new Vector3(-cellSize * 0.04f, 0.2f, -cellSize * 0.08f), new Vector3(cellSize * 0.12f, 0.045f, 0.035f));
            }
        }

        private void AddUnbuiltLotStatusMarker(ZoneType zone, int seed, Vector3 center, Material material)
        {
            // CITY_SKYLINES_UNBUILT_LOT_STATUS_MARKER keeps approved but empty parcels visibly build-ready.
            var corner = center + new Vector3(cellSize * 0.31f, 0f, -cellSize * 0.31f);
            AddLooseCube(decorationObjects, "UnbuiltLotStatusPlate", roadLineMaterial, corner + new Vector3(0f, 0.048f, 0f), new Vector3(cellSize * 0.2f, 0.024f, cellSize * 0.12f));
            AddLooseCube(decorationObjects, "UnbuiltLotStatusDot", material, corner + new Vector3(0f, 0.082f, 0f), new Vector3(0.075f, 0.05f, 0.075f));

            if (seed % 3 == 0 || zone == ZoneType.Civic || zone == ZoneType.Utility)
            {
                AddLooseCube(decorationObjects, "UnbuiltLotSurveyTripod", serviceMaterial, corner + new Vector3(-cellSize * 0.12f, 0.11f, cellSize * 0.1f), new Vector3(0.045f, 0.2f, 0.045f));
                AddLooseCube(decorationObjects, "UnbuiltLotSurveyHead", windowMaterial, corner + new Vector3(-cellSize * 0.12f, 0.22f, cellSize * 0.1f), new Vector3(0.11f, 0.045f, 0.08f));
            }

            if (seed % 5 == 0)
            {
                AddLooseCube(decorationObjects, "UnbuiltLotSafetyConeBase", roadLineMaterial, corner + new Vector3(cellSize * 0.11f, 0.045f, cellSize * 0.11f), new Vector3(0.1f, 0.03f, 0.1f));
                AddLooseCube(decorationObjects, "UnbuiltLotSafetyConeBody", serviceNeedMaterial, corner + new Vector3(cellSize * 0.11f, 0.1f, cellSize * 0.11f), new Vector3(0.07f, 0.09f, 0.07f));
            }
        }

        private bool IsShorelineSceneryTile(int x, int y)
        {
            // LOW_POLY_SHORELINE_DETAILS adds a visible river edge without changing simulation tiles.
            return IsWaterTile(x - 1, y) || IsWaterTile(x + 1, y) || IsWaterTile(x, y - 1) || IsWaterTile(x, y + 1);
        }

        private bool IsRoadsideSceneryTile(int x, int y)
        {
            return HasRoadTile(x - 1, y) || HasRoadTile(x + 1, y) || HasRoadTile(x, y - 1) || HasRoadTile(x, y + 1);
        }

        private bool HasRoadTile(int x, int y)
        {
            var tile = controller.GetTile(x, y);
            return tile != null && !string.IsNullOrEmpty(tile.RoadId);
        }

        private bool IsWaterTile(int x, int y)
        {
            var tile = controller.GetTile(x, y);
            return tile != null && tile.Terrain == TerrainType.Water;
        }

        private void AddRoadsideGreenCue(GridPos pos, int seed)
        {
            // REFERENCE_IMAGE_ROADSIDE_GREENERY adds fresh boulevard trees beside the city road grid.
            var center = CellCenter(pos, 0.045f);
            var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y);
            var stripScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.026f, cellSize * 0.58f)
                : new Vector3(cellSize * 0.58f, 0.026f, cellSize * 0.18f);
            AddLooseCube(decorationObjects, "LowPolyRoadsideGreenStrip", grassGridMaterial, center, stripScale);
            var lampSide = (seed & 8) == 0 ? 1f : -1f;
            var lampOffset = horizontal
                ? new Vector3(0f, 0f, lampSide * cellSize * 0.32f)
                : new Vector3(lampSide * cellSize * 0.32f, 0f, 0f);
            var benchOffset = horizontal
                ? new Vector3(cellSize * 0.22f, 0f, -lampSide * cellSize * 0.28f)
                : new Vector3(-lampSide * cellSize * 0.28f, 0f, cellSize * 0.22f);
            var benchScale = horizontal
                ? new Vector3(cellSize * 0.24f, 0.055f, 0.06f)
                : new Vector3(0.06f, 0.055f, cellSize * 0.24f);
            AddLooseCube(decorationObjects, "LowPolyRoadsideBench", roadLineMaterial, center + benchOffset + new Vector3(0f, 0.09f, 0f), benchScale);
            AddLooseCube(decorationObjects, "LowPolyRoadsideBenchBase", serviceMaterial, center + benchOffset + new Vector3(0f, 0.05f, 0f), new Vector3(0.05f, 0.08f, 0.05f));
            AddLooseCube(decorationObjects, "LowPolyRoadsideLampPost", serviceMaterial, center + lampOffset + new Vector3(0f, 0.16f, 0f), new Vector3(0.045f, 0.32f, 0.045f));
            AddLooseCube(decorationObjects, "LowPolyRoadsideLampGlow", windowMaterial, center + lampOffset + new Vector3(0f, 0.34f, 0f), new Vector3(0.13f, 0.05f, 0.13f));
            AddTree(pos, seed);
        }

        private void AddCentralGreenParcelAccent(GridPos pos, int seed)
        {
            // LOW_POLY_CENTRAL_GREEN_PARCELS gives the core map brighter parklet blocks between roads.
            var center = CellCenter(pos, 0.064f);
            var normal = AdjacentRoadNormal(pos);
            if (normal == Vector3.zero)
            {
                normal = (seed & 1) == 0 ? Vector3.forward : Vector3.right;
            }

            var tangent = Mathf.Abs(normal.x) > 0.01f ? Vector3.forward : Vector3.right;
            var side = ((seed >> 3) & 1) == 0 ? -1f : 1f;
            var acrossRoad = Mathf.Abs(normal.x) > 0.01f;
            var padScale = acrossRoad
                ? new Vector3(cellSize * 0.28f, 0.026f, cellSize * 0.52f)
                : new Vector3(cellSize * 0.52f, 0.026f, cellSize * 0.28f);
            var pathScale = acrossRoad
                ? new Vector3(cellSize * 0.05f, 0.018f, cellSize * 0.42f)
                : new Vector3(cellSize * 0.42f, 0.018f, cellSize * 0.05f);

            var padCenter = center - normal * cellSize * 0.14f;
            var treeCenter = padCenter + tangent * side * cellSize * 0.16f;
            AddLooseCube(decorationObjects, "LowPolyCentralGreenParcelPad", grassGridMaterial, padCenter, padScale);
            AddLooseCube(decorationObjects, "LowPolyCentralGreenParcelPath", roadLineMaterial, center + normal * cellSize * 0.16f + new Vector3(0f, 0.026f, 0f), pathScale);
            AddLooseCube(decorationObjects, "LowPolyCentralGreenParcelTreeTrunk", treeTrunkMaterial, treeCenter + new Vector3(0f, 0.105f, 0f), new Vector3(0.045f, 0.21f, 0.045f));
            AddLooseCube(decorationObjects, "LowPolyCentralGreenParcelTreeCanopy", treeCanopyMaterial, treeCenter + new Vector3(0f, 0.25f, 0f), new Vector3(cellSize * 0.17f, 0.15f, cellSize * 0.17f));
            AddLooseCube(decorationObjects, "LowPolyCentralGreenParcelSunPip", windowMaterial, padCenter - tangent * side * cellSize * 0.16f + new Vector3(0f, 0.052f, 0f), new Vector3(cellSize * 0.08f, 0.035f, cellSize * 0.08f));
        }

        private void AddRoadsideMiniScene(GridPos pos, int seed)
        {
            // LOW_POLY_ROADSIDE_MINI_SCENES add tiny trees, stones, and view markers around road grids.
            var center = CellCenter(pos, 0.062f);
            var normal = AdjacentRoadNormal(pos);
            if (normal == Vector3.zero)
            {
                normal = ((seed & 1) == 0) ? Vector3.forward : Vector3.right;
            }

            var tangent = Mathf.Abs(normal.x) > 0.01f ? Vector3.forward : Vector3.right;
            var side = ((seed >> 3) & 1) == 0 ? 1f : -1f;
            var groveCenter = center - normal * cellSize * 0.18f + tangent * side * cellSize * 0.18f;
            AddLooseCube(decorationObjects, "LowPolyRoadsideSceneGrassPad", grassGridMaterial, center - normal * cellSize * 0.08f, new Vector3(cellSize * 0.38f, 0.02f, cellSize * 0.28f));
            AddLooseCube(decorationObjects, "LowPolyRoadsideSceneTreeTrunk", treeTrunkMaterial, groveCenter + new Vector3(0f, 0.12f, 0f), new Vector3(0.045f, 0.22f, 0.045f));
            AddLooseCube(decorationObjects, "LowPolyRoadsideSceneTreeCanopy", treeCanopyMaterial, groveCenter + new Vector3(0f, 0.28f, 0f), new Vector3(cellSize * 0.18f, 0.16f, cellSize * 0.18f));
            AddLooseCube(decorationObjects, "LowPolyRoadsideSceneStone", rockMaterial, center + normal * cellSize * 0.16f - tangent * side * cellSize * 0.12f + new Vector3(0f, 0.07f, 0f), new Vector3(cellSize * 0.13f, 0.08f, cellSize * 0.1f));

            if (seed % 3 == 0)
            {
                var markerCenter = center + tangent * side * cellSize * 0.28f + normal * cellSize * 0.08f;
                AddLooseCube(decorationObjects, "LowPolyRoadsideViewMarkerPost", serviceMaterial, markerCenter + new Vector3(0f, 0.15f, 0f), new Vector3(0.035f, 0.28f, 0.035f));
                AddLooseCube(decorationObjects, "LowPolyRoadsideViewMarkerPlate", roadLineMaterial, markerCenter + new Vector3(0f, 0.29f, 0f), new Vector3(cellSize * 0.16f, 0.055f, 0.04f));
                AddLooseCube(decorationObjects, "LowPolyRoadsideViewMarkerDot", windowMaterial, markerCenter + new Vector3(0f, 0.335f, 0f), new Vector3(0.055f, 0.035f, 0.045f));
            }
        }

        private void AddHillFacetCue(GridPos pos, int seed)
        {
            // REFERENCE_IMAGE_HILL_FACET_CUES turns resource hills into readable low-poly terraces.
            var center = CellCenter(pos, 0.055f);
            var horizontal = (seed & 1) == 0;
            var terraceScale = horizontal
                ? new Vector3(cellSize * 0.54f, 0.055f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.055f, cellSize * 0.54f);
            var highlightScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.026f, cellSize * 0.06f)
                : new Vector3(cellSize * 0.06f, 0.026f, cellSize * 0.34f);
            AddLooseCube(decorationObjects, "LowPolyHillTerrace", rockMaterial, center + new Vector3(0f, 0.02f, 0f), terraceScale);
            AddLooseCube(decorationObjects, "LowPolyHillGrassHighlight", grassGridMaterial, center + new Vector3(0f, 0.07f, 0f), highlightScale);
        }

        private void AddTree(GridPos pos, int seed)
        {
            // FRESH_SHORELINE_TREE_VARIATION keeps open green space lively in the low-poly city view.
            var jitterX = (((seed >> 2) & 7) - 3) * cellSize * 0.025f;
            var jitterZ = (((seed >> 5) & 7) - 3) * cellSize * 0.025f;
            var center = CellCenter(pos, 0f) + new Vector3(jitterX, 0f, jitterZ);
            var trunkHeight = 0.24f + (seed % 3) * 0.025f;
            var canopyWidth = 0.3f + ((seed >> 3) % 4) * 0.025f;
            var canopyHeight = 0.24f + ((seed >> 6) % 4) * 0.02f;
            AddLooseCube(decorationObjects, "LowPolyTreeGroundShadow", buildingFootprintMaterial, center + new Vector3(0.04f, 0.012f, 0.04f), new Vector3(canopyWidth * 0.82f, 0.012f, canopyWidth * 0.64f));
            AddLooseCube(decorationObjects, "LowPolyTreeTrunk", treeTrunkMaterial, center + new Vector3(0f, trunkHeight * 0.5f, 0f), new Vector3(0.08f, trunkHeight, 0.08f));
            AddLooseCube(decorationObjects, "LowPolyTreeCanopy", treeCanopyMaterial, center + new Vector3(0f, trunkHeight + 0.16f, 0f), new Vector3(canopyWidth, canopyHeight, canopyWidth));
            AddTreeCanopyFreshLayers(center, seed, trunkHeight, canopyWidth, canopyHeight);
            if ((seed & 1) == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyTreeCanopyHighlight", treeCanopyMaterial, center + new Vector3(0.08f, trunkHeight + 0.28f, -0.06f), new Vector3(canopyWidth * 0.56f, 0.14f, canopyWidth * 0.5f));
            }

            if (seed % 5 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyTreeCompanionShrub", treeCanopyMaterial, center + new Vector3(-0.18f, 0.09f, 0.16f), new Vector3(0.16f, 0.12f, 0.14f));
                AddLooseCube(decorationObjects, "LowPolyTreeFlowerDot", serviceNeedMaterial, center + new Vector3(-0.24f, 0.065f, 0.07f), new Vector3(0.075f, 0.045f, 0.075f));
            }

            if (seed % 4 == 0)
            {
                AddTreeClusterAccent(center, seed, canopyWidth);
            }
        }

        private void AddTreeCanopyFreshLayers(Vector3 center, int seed, float trunkHeight, float canopyWidth, float canopyHeight)
        {
            // LOW_POLY_TREE_LAYERED_CANOPY gives trees the stacked, sunny silhouette from the reference map.
            var side = ((seed >> 3) & 1) == 0 ? -1f : 1f;
            var lowerScale = new Vector3(canopyWidth * 1.08f, Mathf.Max(0.07f, canopyHeight * 0.32f), canopyWidth * 0.92f);
            var topScale = new Vector3(canopyWidth * 0.42f, Mathf.Max(0.055f, canopyHeight * 0.24f), canopyWidth * 0.36f);
            AddLooseCube(decorationObjects, "LowPolyTreeCanopyLowerLayer", treeCanopyMaterial, center + new Vector3(-side * cellSize * 0.04f, trunkHeight + 0.07f, side * cellSize * 0.035f), lowerScale);
            AddLooseCube(decorationObjects, "LowPolyTreeCanopyFreshTop", grassGridMaterial, center + new Vector3(side * cellSize * 0.075f, trunkHeight + canopyHeight + 0.17f, -side * cellSize * 0.055f), topScale);

            if (seed % 3 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyTreeSunlitLeafPip", roadLineMaterial, center + new Vector3(-side * cellSize * 0.12f, trunkHeight + canopyHeight + 0.12f, side * cellSize * 0.1f), new Vector3(0.055f, 0.032f, 0.055f));
            }
        }

        private void AddTreeClusterAccent(Vector3 center, int seed, float canopyWidth)
        {
            // LOW_POLY_TREE_CLUSTER_ACCENTS make empty ground read as intentional groves instead of isolated cubes.
            var side = ((seed >> 4) & 1) == 0 ? -1f : 1f;
            var offset = new Vector3(side * cellSize * 0.22f, 0f, -side * cellSize * 0.16f);
            AddLooseCube(decorationObjects, "LowPolyTreeClusterShadow", buildingFootprintMaterial, center + offset + new Vector3(0.03f, 0.012f, 0.03f), new Vector3(canopyWidth * 0.55f, 0.012f, canopyWidth * 0.44f));
            AddLooseCube(decorationObjects, "LowPolyTreeClusterSaplingTrunk", treeTrunkMaterial, center + offset + new Vector3(0f, 0.105f, 0f), new Vector3(0.05f, 0.21f, 0.05f));
            AddLooseCube(decorationObjects, "LowPolyTreeClusterSaplingCanopy", treeCanopyMaterial, center + offset + new Vector3(0f, 0.245f, 0f), new Vector3(canopyWidth * 0.56f, 0.16f, canopyWidth * 0.52f));
            AddLooseCube(decorationObjects, "LowPolyTreeClusterGroundFlower", serviceNeedMaterial, center - offset * 0.52f + new Vector3(0f, 0.06f, 0f), new Vector3(0.07f, 0.04f, 0.07f));
        }

        private void AddRock(GridPos pos, int seed)
        {
            var center = CellCenter(pos, 0f);
            var size = 0.18f + (seed % 5) * 0.015f;
            AddLooseCube(decorationObjects, "LowPolyRock", rockMaterial, center + new Vector3(0.18f, size * 0.45f, -0.12f), new Vector3(size * 1.25f, size * 0.75f, size));
            AddLooseCube(decorationObjects, "LowPolyRockFacetHighlight", shoreMaterial != null ? shoreMaterial : roadLineMaterial, center + new Vector3(0.21f, size * 0.86f, -0.15f), new Vector3(size * 0.56f, 0.026f, size * 0.34f));
            if ((seed & 1) == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyPebbleCluster", rockMaterial, center + new Vector3(-0.12f, 0.05f, 0.18f), new Vector3(size * 0.48f, size * 0.32f, size * 0.42f));
                AddLooseCube(decorationObjects, "LowPolyPebbleGrassSprig", grassGridMaterial, center + new Vector3(-0.18f, 0.09f, 0.1f), new Vector3(0.055f, 0.12f, 0.055f));
            }

            if (seed % 3 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyRockWarmFacet", roadLineMaterial, center + new Vector3(0.07f, 0.08f, 0.17f), new Vector3(size * 0.38f, 0.024f, size * 0.24f));
                AddLooseCube(decorationObjects, "LowPolyRockMeadowTuft", treeCanopyMaterial, center + new Vector3(-0.26f, 0.075f, -0.04f), new Vector3(0.07f, 0.1f, 0.07f));
            }
        }

        private void AddShorelineDetail(GridPos pos, int seed)
        {
            var center = CellCenter(pos, 0f);
            var edgeIndex = 0;
            if (IsWaterTile(pos.X - 1, pos.Y)) AddShorelineDetailEdge(center, new Vector2(-1f, 0f), seed + edgeIndex++ * 17);
            if (IsWaterTile(pos.X + 1, pos.Y)) AddShorelineDetailEdge(center, new Vector2(1f, 0f), seed + edgeIndex++ * 17);
            if (IsWaterTile(pos.X, pos.Y - 1)) AddShorelineDetailEdge(center, new Vector2(0f, -1f), seed + edgeIndex++ * 17);
            if (IsWaterTile(pos.X, pos.Y + 1)) AddShorelineDetailEdge(center, new Vector2(0f, 1f), seed + edgeIndex * 17);
        }

        private void AddRiverbankMicroSteps(GridPos pos, int seed)
        {
            // LOW_POLY_RIVERBANK_MICRO_STEPS add visible bank height against the lower blue water.
            var direction = ShorelineWaterDirection(pos.X, pos.Y);
            if (direction == Vector2.zero)
            {
                return;
            }

            var center = CellCenter(pos, 0.066f);
            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var horizontal = Mathf.Abs(direction.y) > 0.01f;
            var shelfScale = horizontal
                ? new Vector3(cellSize * 0.58f, 0.022f, cellSize * 0.08f)
                : new Vector3(cellSize * 0.08f, 0.022f, cellSize * 0.58f);
            var grassStepScale = horizontal
                ? new Vector3(cellSize * 0.42f, 0.018f, cellSize * 0.055f)
                : new Vector3(cellSize * 0.055f, 0.018f, cellSize * 0.42f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankRaisedShelf", shoreMaterial, center + normal * cellSize * 0.16f, shelfScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankCheckerStep", grassGridMaterial, center - normal * cellSize * 0.12f + tangent * ((((seed >> 2) & 1) == 0 ? -1f : 1f) * cellSize * 0.16f), grassStepScale);
            AddRiverbankFlowerRibbon(center, direction, seed);

            if (seed % 5 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyRiverbankStepStone", rockMaterial, center - normal * cellSize * 0.28f - tangent * cellSize * 0.18f + new Vector3(0f, 0.035f, 0f), new Vector3(cellSize * 0.12f, 0.06f, cellSize * 0.1f));
            }
        }

        private void AddRiverbankFlowerRibbon(Vector3 center, Vector2 direction, int seed)
        {
            // LOW_POLY_RIVERBANK_FLOWER_RIBBON adds small fresh green and flower accents along the waterline.
            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var horizontal = Mathf.Abs(direction.x) <= 0.01f;
            var grassScale = horizontal
                ? new Vector3(cellSize * 0.38f, 0.016f, cellSize * 0.045f)
                : new Vector3(cellSize * 0.045f, 0.016f, cellSize * 0.38f);
            var flowerScale = new Vector3(cellSize * 0.052f, 0.036f, cellSize * 0.052f);
            var glintScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.01f, cellSize * 0.018f)
                : new Vector3(cellSize * 0.018f, 0.01f, cellSize * 0.2f);
            var side = ((seed >> 4) & 1) == 0 ? -1f : 1f;
            var ribbonCenter = center - normal * cellSize * 0.2f + tangent * side * cellSize * 0.11f + new Vector3(0f, 0.092f, 0f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankFlowerRibbonGrass", grassGridMaterial, ribbonCenter, grassScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankFlowerRibbonDot", serviceNeedMaterial, ribbonCenter + tangent * side * cellSize * 0.13f + new Vector3(0f, 0.035f, 0f), flowerScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankFlowerRibbonDot", serviceNeedMaterial, ribbonCenter - tangent * side * cellSize * 0.12f + new Vector3(0f, 0.032f, 0f), flowerScale * 0.78f);

            if (seed % 3 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyRiverbankFlowerRibbonWaterGlint", windowMaterial, center + normal * cellSize * 0.48f - tangent * side * cellSize * 0.16f + new Vector3(0f, 0.064f, 0f), glintScale);
            }
        }

        private void AddShorelineDetailEdge(Vector3 center, Vector2 direction, int seed)
        {
            // REFERENCE_IMAGE_MULTI_EDGE_SHORELINE_DETAILS keeps river bends and long banks equally polished.
            var offset = new Vector3(direction.x * cellSize * 0.28f, 0f, direction.y * cellSize * 0.28f);
            var bandScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.18f, 0.035f, cellSize * 0.5f)
                : new Vector3(cellSize * 0.5f, 0.035f, cellSize * 0.18f);
            AddLooseCube(decorationObjects, "LowPolyShorelineBand", shoreMaterial, center + offset + new Vector3(0f, 0.035f, 0f), bandScale);
            var innerBankScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.08f, 0.026f, cellSize * 0.42f)
                : new Vector3(cellSize * 0.42f, 0.026f, cellSize * 0.08f);
            AddLooseCube(decorationObjects, "LowPolyShorelineInnerBank", grassGridMaterial, center + offset * 0.56f + new Vector3(0f, 0.052f, 0f), innerBankScale);
            var pathScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.055f, 0.022f, cellSize * 0.34f)
                : new Vector3(cellSize * 0.34f, 0.022f, cellSize * 0.055f);
            AddLooseCube(decorationObjects, "LowPolyShorelineWalkSegment", roadLineMaterial, center + offset * 0.74f + new Vector3(0f, 0.075f, 0f), pathScale);
            AddRiverbankWalkwayDetail(center, direction, seed);
            AddRiverbankFreshEdgeDetail(center, direction, seed);
            AddRiverbankPocketGrove(center, direction, seed);
            AddRiverbankGaugeDetail(center, direction, seed);
            AddRiverbankPebbleRun(center, direction, seed);

            if (seed % 4 == 1)
            {
                AddRiverbankAccessMarker(center, direction);
            }

            if (seed % 3 == 0)
            {
                var tangent = new Vector3(direction.y * cellSize * 0.12f, 0f, -direction.x * cellSize * 0.12f);
                AddLooseCube(decorationObjects, "LowPolyShorelineReed", treeCanopyMaterial, center - offset * 0.45f + tangent + new Vector3(0f, 0.12f, 0f), new Vector3(0.08f, 0.24f, 0.08f));
            }

            if (seed % 5 == 0)
            {
                var glintScale = Mathf.Abs(direction.x) > 0f
                    ? new Vector3(cellSize * 0.08f, 0.022f, cellSize * 0.32f)
                    : new Vector3(cellSize * 0.32f, 0.022f, cellSize * 0.08f);
                AddLooseCube(decorationObjects, "LowPolyWaterGlint", windowMaterial, center + offset * 1.55f + new Vector3(0f, 0.05f, 0f), glintScale);
            }

            if (seed % 7 == 0)
            {
                AddShorelinePierDetail(center, direction, seed);
            }
        }

        private void AddRiverbankPebbleRun(Vector3 center, Vector2 direction, int seed)
        {
            // LOW_POLY_RIVERBANK_PEBBLE_RUN adds tiny light stones where the bank meets the shallow shelf.
            if ((seed & 1) != 0)
            {
                return;
            }

            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var horizontal = Mathf.Abs(direction.x) <= 0.01f;
            var pebbleScale = new Vector3(cellSize * 0.07f, 0.035f, cellSize * 0.055f);
            var stitchScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.014f, cellSize * 0.022f)
                : new Vector3(cellSize * 0.022f, 0.014f, cellSize * 0.18f);
            var baseCenter = center + normal * cellSize * 0.18f + new Vector3(0f, 0.104f, 0f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankPebble", rockMaterial, baseCenter + tangent * cellSize * 0.24f, pebbleScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankPebble", shoreMaterial != null ? shoreMaterial : roadLineMaterial, baseCenter - tangent * cellSize * 0.08f + new Vector3(0f, 0.006f, 0f), pebbleScale * 0.72f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankFoamStitch", windowMaterial, center + normal * cellSize * 0.48f - tangent * cellSize * 0.2f + new Vector3(0f, 0.058f, 0f), stitchScale);
        }

        private void AddRiverbankGaugeDetail(Vector3 center, Vector2 direction, int seed)
        {
            // CITY_SKYLINES_RIVERBANK_GAUGE_DETAIL adds tiny flood and survey cues along managed edges.
            if (seed % 3 != 0)
            {
                return;
            }

            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var gaugeCenter = center + normal * cellSize * 0.28f + tangent * ((((seed >> 2) & 1) == 0 ? -1f : 1f) * cellSize * 0.18f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankFloodGaugePost", utilityMaterial, gaugeCenter + new Vector3(0f, 0.17f, 0f), new Vector3(0.035f, 0.3f, 0.035f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankFloodGaugeTop", roadLineMaterial, gaugeCenter + new Vector3(0f, 0.33f, 0f), new Vector3(0.14f, 0.035f, 0.055f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankFloodGaugeMark", windowMaterial, gaugeCenter + new Vector3(0f, 0.24f, 0f), new Vector3(0.09f, 0.026f, 0.045f));

            if (seed % 6 == 0)
            {
                var swaleCenter = center - normal * cellSize * 0.12f - tangent * cellSize * 0.16f + new Vector3(0f, 0.07f, 0f);
                var swaleScale = Mathf.Abs(direction.x) > 0f
                    ? new Vector3(cellSize * 0.1f, 0.035f, cellSize * 0.26f)
                    : new Vector3(cellSize * 0.26f, 0.035f, cellSize * 0.1f);
                AddLooseCube(decorationObjects, "LowPolyRiverbankBioswale", treeCanopyMaterial, swaleCenter, swaleScale);
                AddLooseCube(decorationObjects, "LowPolyRiverbankBioswaleWater", windowMaterial, swaleCenter + new Vector3(0f, 0.026f, 0f), swaleScale * 0.52f);
            }
        }

        private void AddRiverbankFreshEdgeDetail(Vector3 center, Vector2 direction, int seed)
        {
            // LOW_POLY_FRESH_RIVERBANK accents grass, rocks, and reeds beside the clean shore band.
            if (seed % 2 != 0)
            {
                return;
            }

            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var side = ((seed >> 3) & 1) == 0 ? 1f : -1f;
            var bankCenter = center - normal * cellSize * 0.08f + tangent * side * cellSize * 0.18f;
            AddLooseCube(decorationObjects, "LowPolyRiverbankGrassClump", treeCanopyMaterial, bankCenter + new Vector3(0f, 0.105f, 0f), new Vector3(cellSize * 0.12f, 0.12f, cellSize * 0.1f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankTinyRock", rockMaterial, bankCenter - tangent * side * cellSize * 0.12f + new Vector3(0f, 0.06f, 0f), new Vector3(cellSize * 0.11f, 0.075f, cellSize * 0.085f));
            AddRiverbankShrubCluster(center, direction, seed);

            if (seed % 6 == 0)
            {
                var reedScale = Mathf.Abs(direction.x) > 0f
                    ? new Vector3(cellSize * 0.045f, 0.18f, cellSize * 0.12f)
                    : new Vector3(cellSize * 0.12f, 0.18f, cellSize * 0.045f);
                AddLooseCube(decorationObjects, "LowPolyRiverbankReedPair", grassGridMaterial, center + normal * cellSize * 0.42f + tangent * side * cellSize * 0.1f + new Vector3(0f, 0.12f, 0f), reedScale);
            }
        }

        private void AddRiverbankShrubCluster(Vector3 center, Vector2 direction, int seed)
        {
            // LOW_POLY_RIVERBANK_SHRUB_CLUSTER thickens the bright shore edge with small green groups.
            if (seed % 4 == 3)
            {
                return;
            }

            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var side = ((seed >> 5) & 1) == 0 ? -1f : 1f;
            var baseCenter = center - normal * cellSize * 0.24f + tangent * side * cellSize * 0.12f + new Vector3(0f, 0.09f, 0f);
            var hedgeScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.09f, 0.09f, cellSize * 0.2f)
                : new Vector3(cellSize * 0.2f, 0.09f, cellSize * 0.09f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankShrubCluster", treeCanopyMaterial, baseCenter, hedgeScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankShrubCluster", treeCanopyMaterial, baseCenter + tangent * side * cellSize * 0.14f + new Vector3(0f, 0.025f, 0f), hedgeScale * 0.74f);

            if (seed % 8 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyRiverbankShrubFlower", serviceNeedMaterial, baseCenter - tangent * side * cellSize * 0.12f + new Vector3(0f, 0.05f, 0f), new Vector3(cellSize * 0.065f, 0.038f, cellSize * 0.065f));
            }
        }

        private void AddRiverbankPocketGrove(Vector3 center, Vector2 direction, int seed)
        {
            // LOW_POLY_RIVERBANK_POCKET_GROVE adds fresh grass, trees, and rocks along the river edge.
            if (seed % 4 == 1)
            {
                return;
            }

            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var side = ((seed >> 4) & 1) == 0 ? -1f : 1f;
            var groveCenter = center - normal * cellSize * 0.2f + tangent * side * cellSize * 0.25f;
            var grassScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.18f, 0.026f, cellSize * 0.34f)
                : new Vector3(cellSize * 0.34f, 0.026f, cellSize * 0.18f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankFreshGrassPad", grassGridMaterial, groveCenter + new Vector3(0f, 0.07f, 0f), grassScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankYoungTreeTrunk", treeTrunkMaterial, groveCenter + tangent * side * cellSize * 0.06f + new Vector3(0f, 0.17f, 0f), new Vector3(0.045f, 0.24f, 0.045f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankYoungTreeCanopy", treeCanopyMaterial, groveCenter + tangent * side * cellSize * 0.06f + new Vector3(0f, 0.34f, 0f), new Vector3(cellSize * 0.18f, 0.16f, cellSize * 0.18f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankSmoothStone", rockMaterial, groveCenter - tangent * side * cellSize * 0.16f + normal * cellSize * 0.08f + new Vector3(0f, 0.105f, 0f), new Vector3(cellSize * 0.12f, 0.075f, cellSize * 0.09f));

            if (seed % 5 == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyRiverbankFlowerDot", serviceNeedMaterial, groveCenter - normal * cellSize * 0.08f + new Vector3(0f, 0.105f, 0f), new Vector3(cellSize * 0.075f, 0.045f, cellSize * 0.075f));
            }
        }

        private void AddRiverbankWalkwayDetail(Vector3 center, Vector2 direction, int seed)
        {
            // LOW_POLY_RIVERBANK_WALKWAY makes the river edge read as a planned pedestrian path.
            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var pathCenter = center + normal * cellSize * 0.18f + new Vector3(0f, 0.1f, 0f);
            var paverScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.06f, 0.022f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.022f, cellSize * 0.06f);
            AddLooseCube(decorationObjects, "LowPolyRiverbankWalkwayPaver", shoreMaterial != null ? shoreMaterial : roadLineMaterial, pathCenter + tangent * cellSize * 0.12f, paverScale);
            AddLooseCube(decorationObjects, "LowPolyRiverbankWalkwayPaver", shoreMaterial != null ? shoreMaterial : roadLineMaterial, pathCenter - tangent * cellSize * 0.12f, paverScale);

            if ((seed & 1) == 0)
            {
                var railScale = Mathf.Abs(direction.x) > 0f
                    ? new Vector3(0.035f, 0.03f, cellSize * 0.28f)
                    : new Vector3(cellSize * 0.28f, 0.03f, 0.035f);
                AddLooseCube(decorationObjects, "LowPolyRiverbankWalkwayRail", roadLineMaterial, center + normal * cellSize * 0.33f + new Vector3(0f, 0.16f, 0f), railScale);
            }

            if (seed % 3 == 0)
            {
                var lampBase = pathCenter - tangent * cellSize * 0.2f;
                AddLooseCube(decorationObjects, "LowPolyRiverbankWalkwayLampPost", serviceMaterial, lampBase + new Vector3(0f, 0.14f, 0f), new Vector3(0.035f, 0.26f, 0.035f));
                AddLooseCube(decorationObjects, "LowPolyRiverbankWalkwayLampGlow", windowMaterial, lampBase + new Vector3(0f, 0.29f, 0f), new Vector3(0.11f, 0.045f, 0.11f));
            }
        }

        private void AddRiverbankAccessMarker(Vector3 center, Vector2 direction)
        {
            // REFERENCE_IMAGE_RIVERBANK_ACCESS_MARKERS adds small civilized edges to the low-poly river.
            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var baseCenter = center + normal * cellSize * 0.2f + tangent * cellSize * 0.16f;
            AddLooseCube(decorationObjects, "LowPolyRiverbankStep", shoreMaterial, baseCenter + new Vector3(0f, 0.09f, 0f), new Vector3(cellSize * 0.18f, 0.04f, cellSize * 0.16f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankAccessPost", serviceMaterial, baseCenter + tangent * cellSize * 0.12f + new Vector3(0f, 0.18f, 0f), new Vector3(0.035f, 0.22f, 0.035f));
            AddLooseCube(decorationObjects, "LowPolyRiverbankAccessRail", roadLineMaterial, baseCenter + tangent * cellSize * 0.04f + new Vector3(0f, 0.25f, 0f), new Vector3(cellSize * 0.2f, 0.035f, 0.035f));
        }

        private void AddShorelinePierDetail(Vector3 center, Vector2 direction, int seed)
        {
            // REFERENCE_IMAGE_RIVER_PIER_STEPS makes the river edge feel like a planned waterfront.
            var normal = new Vector3(direction.x, 0f, direction.y);
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var shoreWood = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var pierScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.32f, 0.05f, cellSize * 0.13f)
                : new Vector3(cellSize * 0.13f, 0.05f, cellSize * 0.32f);
            var stepScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.18f, 0.035f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.035f, cellSize * 0.18f);
            var pierCenter = center + normal * cellSize * 0.58f + new Vector3(0f, 0.09f, 0f);
            AddLooseCube(decorationObjects, "LowPolyWaterfrontPierDeck", shoreWood, pierCenter, pierScale);
            AddLooseCube(decorationObjects, "LowPolyWaterfrontPierStep", roadLineMaterial, center + normal * cellSize * 0.36f + new Vector3(0f, 0.095f, 0f), stepScale);

            var postOffset = tangent * cellSize * 0.09f;
            AddLooseCube(decorationObjects, "LowPolyWaterfrontPierPost", serviceMaterial, pierCenter + postOffset + normal * cellSize * 0.08f + new Vector3(0f, 0.08f, 0f), new Vector3(0.035f, 0.17f, 0.035f));
            AddLooseCube(decorationObjects, "LowPolyWaterfrontPierPost", serviceMaterial, pierCenter - postOffset + normal * cellSize * 0.08f + new Vector3(0f, 0.08f, 0f), new Vector3(0.035f, 0.17f, 0.035f));
            if ((seed & 1) == 0)
            {
                AddLooseCube(decorationObjects, "LowPolyWaterfrontTinyBench", roadLineMaterial, center - normal * cellSize * 0.08f + tangent * cellSize * 0.11f + new Vector3(0f, 0.09f, 0f), new Vector3(0.18f, 0.045f, 0.07f));
            }
        }

        private void AddContinuousShorelineBand(GridPos pos)
        {
            // REFERENCE_IMAGE_CONTINUOUS_RIVER_EDGE keeps the bright river border clean between random details.
            var center = CellCenter(pos, 0f);
            var hasEdge = false;
            if (IsWaterTile(pos.X - 1, pos.Y)) { AddContinuousShorelineEdge(center, new Vector2(-1f, 0f)); hasEdge = true; }
            if (IsWaterTile(pos.X + 1, pos.Y)) { AddContinuousShorelineEdge(center, new Vector2(1f, 0f)); hasEdge = true; }
            if (IsWaterTile(pos.X, pos.Y - 1)) { AddContinuousShorelineEdge(center, new Vector2(0f, -1f)); hasEdge = true; }
            if (IsWaterTile(pos.X, pos.Y + 1)) { AddContinuousShorelineEdge(center, new Vector2(0f, 1f)); hasEdge = true; }
            if (hasEdge)
            {
                AddShorelineCornerCap(pos, center);
            }
        }

        private void AddContinuousShorelineEdge(Vector3 center, Vector2 direction)
        {
            var offset = new Vector3(direction.x * cellSize * 0.39f, 0.056f, direction.y * cellSize * 0.39f);
            var bandScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.07f, 0.022f, cellSize * 0.82f)
                : new Vector3(cellSize * 0.82f, 0.022f, cellSize * 0.07f);
            var lipScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.035f, 0.018f, cellSize * 0.66f)
                : new Vector3(cellSize * 0.66f, 0.018f, cellSize * 0.035f);
            AddLooseCube(decorationObjects, "LowPolyContinuousShorelineBand", shoreMaterial, center + offset, bandScale);
            AddLooseCube(decorationObjects, "LowPolyContinuousShorelineLip", grassGridMaterial, center + offset * 0.82f + new Vector3(0f, 0.014f, 0f), lipScale);
            var foamScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.026f, 0.012f, cellSize * 0.58f)
                : new Vector3(cellSize * 0.58f, 0.012f, cellSize * 0.026f);
            AddLooseCube(decorationObjects, "LowPolyShorelineFoam", windowMaterial, center + offset * 1.08f + new Vector3(0f, 0.018f, 0f), foamScale);
            AddBrightShorelineWaterEdge(center, direction, offset);
        }

        private void AddBrightShorelineWaterEdge(Vector3 center, Vector2 direction, Vector3 offset)
        {
            // LOW_POLY_BRIGHT_RIVER_EDGE keeps the waterline fresh and readable against grass.
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var sparkleScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.018f, 0.012f, cellSize * 0.22f)
                : new Vector3(cellSize * 0.22f, 0.012f, cellSize * 0.018f);
            var shelfScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.032f, 0.01f, cellSize * 0.44f)
                : new Vector3(cellSize * 0.44f, 0.01f, cellSize * 0.032f);
            AddLooseCube(decorationObjects, "LowPolyShallowWaterShelf", shoreMaterial != null ? shoreMaterial : windowMaterial, center + offset * 1.32f + new Vector3(0f, 0.018f, 0f), shelfScale);
            AddLooseCube(decorationObjects, "LowPolyBrightShorelineEdge", windowMaterial, center + offset * 1.2f + tangent * cellSize * 0.16f + new Vector3(0f, 0.026f, 0f), sparkleScale);
            AddLooseCube(decorationObjects, "LowPolyBrightShorelineEdge", windowMaterial, center + offset * 1.2f - tangent * cellSize * 0.16f + new Vector3(0f, 0.026f, 0f), sparkleScale);
            AddShorelineSandbarFacets(center, direction, offset);
            AddCleanShorelineSurveyTicks(center, direction, offset);
        }

        private void AddShorelineSandbarFacets(Vector3 center, Vector2 direction, Vector3 offset)
        {
            // LOW_POLY_SHORELINE_SANDBARS give the river edge a shallow, sunlit shelf.
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var horizontal = Mathf.Abs(direction.x) <= 0.01f;
            var barScale = horizontal
                ? new Vector3(cellSize * 0.28f, 0.012f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.012f, cellSize * 0.28f);
            var pebbleScale = new Vector3(cellSize * 0.06f, 0.035f, cellSize * 0.05f);
            var shelfCenter = center + offset * 1.42f + new Vector3(0f, 0.028f, 0f);
            AddLooseCube(decorationObjects, "LowPolyShorelineSandbarFacet", shoreMaterial != null ? shoreMaterial : roadLineMaterial, shelfCenter + tangent * cellSize * 0.08f, barScale);
            AddLooseCube(decorationObjects, "LowPolyShorelineSandbarFacet", roadLineMaterial, shelfCenter - tangent * cellSize * 0.22f + new Vector3(0f, 0.006f, 0f), barScale * 0.62f);
            AddLooseCube(decorationObjects, "LowPolyShorelinePebbleSpark", rockMaterial, center + offset * 0.98f + tangent * cellSize * 0.28f + new Vector3(0f, 0.055f, 0f), pebbleScale);
        }

        private void AddCleanShorelineSurveyTicks(Vector3 center, Vector2 direction, Vector3 offset)
        {
            // CITY_SKYLINES_CLEAN_SHORELINE_TICKS sharpen the readable line between river and developable land.
            var tangent = new Vector3(direction.y, 0f, -direction.x);
            var tickScale = Mathf.Abs(direction.x) > 0f
                ? new Vector3(cellSize * 0.032f, 0.02f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.02f, cellSize * 0.032f);
            var postScale = new Vector3(0.045f, 0.11f, 0.045f);
            var baseCenter = center + offset * 0.62f + new Vector3(0f, 0.1f, 0f);
            AddLooseCube(decorationObjects, "CleanShorelineSurveyTick", roadLineMaterial, baseCenter + tangent * cellSize * 0.24f, tickScale);
            AddLooseCube(decorationObjects, "CleanShorelineSurveyTick", roadLineMaterial, baseCenter - tangent * cellSize * 0.24f, tickScale);
            AddLooseCube(decorationObjects, "CleanShorelineMarkerPost", serviceMaterial, baseCenter + tangent * cellSize * 0.34f + new Vector3(0f, 0.045f, 0f), postScale);
        }

        private void AddShorelineCornerCap(GridPos pos, Vector3 center)
        {
            // REFERENCE_IMAGE_SHORELINE_CORNER_CAPS keeps L-shaped river banks visually continuous.
            var left = IsWaterTile(pos.X - 1, pos.Y);
            var right = IsWaterTile(pos.X + 1, pos.Y);
            var down = IsWaterTile(pos.X, pos.Y - 1);
            var up = IsWaterTile(pos.X, pos.Y + 1);
            if (left && down) AddShorelineCornerCapPart(center, -1f, -1f);
            if (left && up) AddShorelineCornerCapPart(center, -1f, 1f);
            if (right && down) AddShorelineCornerCapPart(center, 1f, -1f);
            if (right && up) AddShorelineCornerCapPart(center, 1f, 1f);
        }

        private void AddShorelineCornerCapPart(Vector3 center, float signX, float signZ)
        {
            var offset = new Vector3(signX * cellSize * 0.24f, 0.065f, signZ * cellSize * 0.24f);
            AddLooseCube(decorationObjects, "LowPolyShorelineCornerCap", shoreMaterial, center + offset, new Vector3(cellSize * 0.22f, 0.04f, cellSize * 0.22f));
            AddLooseCube(decorationObjects, "LowPolyShorelineCornerInnerCap", grassGridMaterial, center + offset * 0.78f + new Vector3(0f, 0.025f, 0f), new Vector3(cellSize * 0.13f, 0.026f, cellSize * 0.13f));
        }

        private Vector2 ShorelineWaterDirection(int x, int y)
        {
            if (IsWaterTile(x - 1, y)) return new Vector2(-1f, 0f);
            if (IsWaterTile(x + 1, y)) return new Vector2(1f, 0f);
            if (IsWaterTile(x, y - 1)) return new Vector2(0f, -1f);
            if (IsWaterTile(x, y + 1)) return new Vector2(0f, 1f);
            return Vector2.zero;
        }

        private void RebuildLockedRegionGuide()
        {
            ClearObjects(guideObjects);
            var grid = controller.Grid;
            if (grid == null)
            {
                return;
            }

            if (grid.ExpansionUnlocked)
            {
                return;
            }

            int startX;
            int startY;
            int endX;
            int endY;
            grid.LockedExpansionBounds(out startX, out startY, out endX, out endY);

            for (var x = startX; x <= endX; x += 1)
            {
                AddLockedDash((x + 0.5f) * cellSize, (startY + 0.05f) * cellSize, cellSize * 0.62f, 0.045f);
                AddLockedDash((x + 0.5f) * cellSize, (endY + 0.95f) * cellSize, cellSize * 0.62f, 0.045f);
            }

            for (var y = startY; y <= endY; y += 1)
            {
                AddLockedDash((startX + 0.05f) * cellSize, (y + 0.5f) * cellSize, 0.045f, cellSize * 0.62f);
                AddLockedDash((endX + 0.95f) * cellSize, (y + 0.5f) * cellSize, 0.045f, cellSize * 0.62f);
            }

            AddLockedCornerMarker(startX, startY, 1f, 1f);
            AddLockedCornerMarker(endX + 1, startY, -1f, 1f);
            AddLockedCornerMarker(startX, endY + 1, 1f, -1f);
            AddLockedCornerMarker(endX + 1, endY + 1, -1f, -1f);
            AddLockedRegionPlanningField(startX, startY, endX, endY);
            AddLockedRegionGroundPolish(startX, startY, endX, endY);
            AddLockedRegionPlanningStripes(startX, startY, endX, endY);
            AddLockedRegionInnerDashedGuide(startX, startY, endX, endY);
            AddLockedRegionFutureRoadGrid(startX, startY, endX, endY);
            AddLockedRegionGatewayStubs(startX, startY, endX, endY);
            AddLockedRegionSurveyStakes(startX, startY, endX, endY);
            AddLockedRegionPlanningStakes(startX, startY, endX, endY);
            AddLockedRegionBoundaryCones(startX, startY, endX, endY);
            AddLockedRegionEdgeGreenery(startX, startY, endX, endY);
            AddLockedRegionHint(startX, startY, endX, endY);
            AddLockedRegionUnlockWorksite(startX, startY, endX, endY);
            AddLockedRegionProgressCues(startX, startY, endX, endY);
            AddLockedRegionUnlockBeacons(startX, startY, endX, endY);
            AddLockedRegionBoundaryInfoLayer(startX, startY, endX, endY);
            AddLockedRegionSurveyRulers(startX, startY, endX, endY);
            AddLockedRegionFreshSurveyMarks(startX, startY, endX, endY);
            AddLockedRegionEdgeHintMarkers(startX, startY, endX, endY);
            AddLockedRegionPerimeterBeaconDashes(startX, startY, endX, endY);
            AddLockedRegionBlueprintMicroDetails(startX, startY, endX, endY);
            AddLockedRegionOuterDashHalo(startX, startY, endX, endY);
            AddLockedRegionApprovalBadges(startX, startY, endX, endY);
            AddLockedRegionPerimeterMicroDecor(startX, startY, endX, endY);
            AddLockedRegionMobileTaskPins(startX, startY, endX, endY);
        }

        private void AddLockedDash(float x, float z, float width, float depth)
        {
            AddLooseCube(guideObjects, "LockedRegionDashedOutline", lockedAreaMaterial, new Vector3(x, 0.07f, z), new Vector3(width, 0.035f, depth));
        }

        private void AddLockedRegionOuterDashHalo(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_OUTER_DASH_HALO keeps the unopened region light, dashed, and readable.
            var progress = LockedRegionObjectiveProgress01();
            var material = progress >= 0.5f ? windowMaterial : roadLineMaterial;
            var y = 0.168f;
            for (var x = startX; x <= endX; x += 2)
            {
                AddLockedRegionOuterDash(new Vector3((x + 0.5f) * cellSize, y, (startY - 0.08f) * cellSize), true, material);
                AddLockedRegionOuterDash(new Vector3((x + 0.5f) * cellSize, y, (endY + 1.08f) * cellSize), true, material);
            }

            for (var z = startY; z <= endY; z += 2)
            {
                AddLockedRegionOuterDash(new Vector3((startX - 0.08f) * cellSize, y, (z + 0.5f) * cellSize), false, material);
                AddLockedRegionOuterDash(new Vector3((endX + 1.08f) * cellSize, y, (z + 0.5f) * cellSize), false, material);
            }

            AddLooseCube(guideObjects, "LockedRegionOuterDashHaloCorner", lockedAreaMaterial, new Vector3(startX * cellSize, y + 0.025f, startY * cellSize), new Vector3(cellSize * 0.18f, 0.05f, cellSize * 0.18f));
            AddLooseCube(guideObjects, "LockedRegionOuterDashHaloCorner", material, new Vector3((endX + 1f) * cellSize, y + 0.025f, (endY + 1f) * cellSize), new Vector3(cellSize * 0.18f, 0.05f, cellSize * 0.18f));
        }

        private void AddLockedRegionOuterDash(Vector3 center, bool horizontal, Material material)
        {
            var scale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.02f, cellSize * 0.04f)
                : new Vector3(cellSize * 0.04f, 0.02f, cellSize * 0.34f);
            AddLooseCube(guideObjects, "LockedRegionOuterDashHalo", material, center, scale);
        }

        private void AddLockedRegionApprovalBadges(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_APPROVAL_BADGES gives the dashed future district readable approval corners.
            var progress = LockedRegionObjectiveProgress01();
            var material = progress >= 0.75f ? previewOkMaterial : windowMaterial;
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            AddLockedRegionApprovalCorner(new Vector3(startX * cellSize + cellSize * 0.36f, 0.2f, startY * cellSize + cellSize * 0.36f), 1f, 1f, material);
            AddLockedRegionApprovalCorner(new Vector3((endX + 1f) * cellSize - cellSize * 0.36f, 0.2f, startY * cellSize + cellSize * 0.36f), -1f, 1f, material);
            AddLockedRegionApprovalCorner(new Vector3(startX * cellSize + cellSize * 0.36f, 0.2f, (endY + 1f) * cellSize - cellSize * 0.36f), 1f, -1f, material);
            AddLockedRegionApprovalCorner(new Vector3((endX + 1f) * cellSize - cellSize * 0.36f, 0.2f, (endY + 1f) * cellSize - cellSize * 0.36f), -1f, -1f, material);
            AddLockedRegionApprovalArrow(new Vector3(centerX - cellSize * 1.05f, 0.19f, (startY + 0.32f) * cellSize), true, material);
            AddLockedRegionApprovalArrow(new Vector3((endX + 0.68f) * cellSize, 0.19f, centerZ + cellSize * 0.88f), false, material);
        }

        private void AddLockedRegionApprovalCorner(Vector3 center, float signX, float signZ, Material material)
        {
            AddLooseCube(guideObjects, "LockedRegionApprovalCornerPad", grassGridMaterial, center + new Vector3(0f, -0.08f, 0f), new Vector3(cellSize * 0.34f, 0.028f, cellSize * 0.34f));
            AddLooseCube(guideObjects, "LockedRegionApprovalCornerLamp", material, center + new Vector3(0f, 0.04f, 0f), new Vector3(cellSize * 0.13f, 0.11f, cellSize * 0.13f));
            AddLooseCube(guideObjects, "LockedRegionApprovalCornerArm", roadLineMaterial, center + new Vector3(signX * cellSize * 0.16f, 0.01f, 0f), new Vector3(cellSize * 0.24f, 0.024f, cellSize * 0.035f));
            AddLooseCube(guideObjects, "LockedRegionApprovalCornerArm", roadLineMaterial, center + new Vector3(0f, 0.012f, signZ * cellSize * 0.16f), new Vector3(cellSize * 0.035f, 0.024f, cellSize * 0.24f));
        }

        private void AddLockedRegionApprovalArrow(Vector3 center, bool horizontal, Material material)
        {
            var shaftScale = horizontal
                ? new Vector3(cellSize * 0.42f, 0.026f, cellSize * 0.045f)
                : new Vector3(cellSize * 0.045f, 0.026f, cellSize * 0.42f);
            var headOffset = horizontal ? new Vector3(cellSize * 0.24f, 0f, 0f) : new Vector3(0f, 0f, cellSize * 0.24f);
            AddLooseCube(guideObjects, "LockedRegionApprovalArrowShaft", material, center, shaftScale);
            AddLooseCubeRotated(guideObjects, "LockedRegionApprovalArrowHead", roadLineMaterial, center + headOffset, new Vector3(cellSize * 0.15f, 0.026f, cellSize * 0.055f), horizontal ? 45f : -45f);
            AddLooseCubeRotated(guideObjects, "LockedRegionApprovalArrowHead", roadLineMaterial, center + headOffset, new Vector3(cellSize * 0.15f, 0.026f, cellSize * 0.055f), horizontal ? -45f : 45f);
        }

        private void AddLockedRegionPerimeterMicroDecor(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_EDGE_MICRO_DECOR adds low-poly survey props and planting beside the next district.
            var progress = LockedRegionObjectiveProgress01();
            for (var x = startX + 1; x < endX; x += 3)
            {
                AddLockedRegionEdgeMicroDecorCell(new Vector3((x + 0.5f) * cellSize, 0.17f, (startY - 0.28f) * cellSize), true, DecorationHash(x, startY), progress);
                AddLockedRegionEdgeMicroDecorCell(new Vector3((x + 0.5f) * cellSize, 0.17f, (endY + 1.28f) * cellSize), true, DecorationHash(x, endY + 7), progress);
            }

            for (var y = startY + 1; y < endY; y += 3)
            {
                AddLockedRegionEdgeMicroDecorCell(new Vector3((startX - 0.28f) * cellSize, 0.17f, (y + 0.5f) * cellSize), false, DecorationHash(startX, y), progress);
                AddLockedRegionEdgeMicroDecorCell(new Vector3((endX + 1.28f) * cellSize, 0.17f, (y + 0.5f) * cellSize), false, DecorationHash(endX + 7, y), progress);
            }
        }

        private void AddLockedRegionEdgeMicroDecorCell(Vector3 center, bool horizontal, int seed, float progress)
        {
            var along = horizontal ? Vector3.right : Vector3.forward;
            var cross = horizontal ? Vector3.forward : Vector3.right;
            var activeMaterial = progress >= 0.55f ? previewOkMaterial : lockedAreaMaterial;
            var padScale = horizontal
                ? new Vector3(cellSize * 0.46f, 0.026f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.026f, cellSize * 0.46f);
            var tapeScale = horizontal
                ? new Vector3(cellSize * 0.36f, 0.018f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.018f, cellSize * 0.36f);
            var flowerScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.038f, cellSize * 0.07f)
                : new Vector3(cellSize * 0.07f, 0.038f, cellSize * 0.18f);

            AddLooseCube(guideObjects, "LockedRegionEdgeMicroPad", grassGridMaterial, center + new Vector3(0f, -0.05f, 0f), padScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeSurveyTape", roadLineMaterial, center + cross * cellSize * 0.07f, tapeScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeFlowerStrip", serviceNeedMaterial, center - cross * cellSize * 0.08f + along * cellSize * 0.14f + new Vector3(0f, -0.01f, 0f), flowerScale);
            AddLooseCube(guideObjects, "LockedRegionEdgePlanterGreen", treeCanopyMaterial, center - along * cellSize * 0.14f + new Vector3(0f, 0.02f, 0f), new Vector3(cellSize * 0.11f, 0.09f, cellSize * 0.11f));

            if ((seed & 1) == 0)
            {
                AddLooseCube(guideObjects, "LockedRegionEdgeSurveyStake", serviceMaterial, center + along * cellSize * 0.24f + new Vector3(0f, 0.09f, 0f), new Vector3(0.04f, 0.22f, 0.04f));
                AddLooseCube(guideObjects, "LockedRegionEdgeSurveyCap", activeMaterial, center + along * cellSize * 0.24f + new Vector3(0f, 0.22f, 0f), new Vector3(0.1f, 0.04f, 0.1f));
                return;
            }

            AddLooseCube(guideObjects, "LockedRegionEdgeSupplyCrate", activeMaterial, center - along * cellSize * 0.22f + new Vector3(0f, 0.035f, 0f), new Vector3(cellSize * 0.12f, 0.09f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionEdgeCrateGlint", windowMaterial, center - along * cellSize * 0.22f + new Vector3(0f, 0.095f, 0f), horizontal ? new Vector3(cellSize * 0.08f, 0.018f, 0.024f) : new Vector3(0.024f, 0.018f, cellSize * 0.08f));
        }

        private void AddLockedRegionMobileTaskPins(int startX, int startY, int endX, int endY)
        {
            var progress = LockedRegionObjectiveProgress01();
            var centerX = (startX + endX + 1f) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1f) * 0.5f * cellSize;
            var material = progress >= 0.85f ? previewOkMaterial : serviceNeedMaterial;
            AddLockedRegionMobileTaskPin(new Vector3(centerX - cellSize * 0.72f, 0.2f, centerZ - cellSize * 0.42f), material, progress, true);
            AddLockedRegionMobileTaskPin(new Vector3(centerX + cellSize * 0.64f, 0.2f, centerZ + cellSize * 0.5f), material, progress, false);
            AddLooseCube(guideObjects, "LockedRegionMobileTaskRoute", roadLineMaterial, new Vector3(centerX - cellSize * 0.04f, 0.106f, centerZ + cellSize * 0.02f), new Vector3(cellSize * 1.08f, 0.024f, cellSize * 0.06f));
            AddLooseCubeRotated(guideObjects, "LockedRegionMobileTaskRouteArrow", material, new Vector3(centerX + cellSize * 0.47f, 0.112f, centerZ + cellSize * 0.23f), new Vector3(cellSize * 0.2f, 0.026f, cellSize * 0.055f), 35f);
            AddLooseCubeRotated(guideObjects, "LockedRegionMobileTaskRouteArrow", material, new Vector3(centerX + cellSize * 0.54f, 0.112f, centerZ + cellSize * 0.12f), new Vector3(cellSize * 0.2f, 0.026f, cellSize * 0.055f), -35f);
        }

        private void AddLockedRegionMobileTaskPin(Vector3 center, Material material, float progress, bool leading)
        {
            var glow = progress >= 0.85f ? previewOkMaterial : lockedAreaMaterial;
            AddLooseCube(guideObjects, "LockedRegionMobileTaskPinShadow", buildingFootprintMaterial, center + new Vector3(0.04f, -0.17f, 0.04f), new Vector3(cellSize * 0.46f, 0.018f, cellSize * 0.34f));
            AddLooseCube(guideObjects, "LockedRegionMobileTaskPinBody", material, center + new Vector3(0f, 0.03f, 0f), new Vector3(cellSize * 0.23f, 0.27f, cellSize * 0.2f));
            AddLooseCube(guideObjects, "LockedRegionMobileTaskPinCap", roadLineMaterial, center + new Vector3(0f, 0.21f, 0f), new Vector3(cellSize * 0.32f, 0.06f, cellSize * 0.26f));
            AddLooseCube(guideObjects, "LockedRegionMobileTaskPinDot", glow, center + new Vector3(0f, 0.285f, 0f), new Vector3(cellSize * 0.13f, 0.05f, cellSize * 0.13f));
            AddLooseCube(guideObjects, "LockedRegionMobileTaskPinStem", roadLineMaterial, center + new Vector3(0f, -0.09f, 0f), new Vector3(0.04f, 0.22f, 0.04f));
            AddLooseCube(guideObjects, "LockedRegionMobileTaskPinTail", material, center + new Vector3((leading ? 1f : -1f) * cellSize * 0.16f, -0.05f, cellSize * 0.12f), new Vector3(cellSize * 0.14f, 0.052f, cellSize * 0.08f));
        }

        private void AddLockedRegionPerimeterBeaconDashes(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_PERIMETER_BEACONS add a bright dotted read to the next expansion edge.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var progress = LockedRegionObjectiveProgress01();
            AddLockedRegionPerimeterBeacon(new Vector3(centerX - cellSize * 0.68f, 0.13f, (startY + 0.08f) * cellSize), true, progress, 0);
            AddLockedRegionPerimeterBeacon(new Vector3(centerX + cellSize * 0.68f, 0.13f, (endY + 0.92f) * cellSize), true, progress, 1);
            AddLockedRegionPerimeterBeacon(new Vector3((startX + 0.08f) * cellSize, 0.13f, centerZ + cellSize * 0.68f), false, progress, 2);
            AddLockedRegionPerimeterBeacon(new Vector3((endX + 0.92f) * cellSize, 0.13f, centerZ - cellSize * 0.68f), false, progress, 3);
        }

        private void AddLockedRegionPerimeterBeacon(Vector3 center, bool horizontal, float progress, int index)
        {
            var active = progress > index * 0.22f;
            var material = active ? windowMaterial : lockedAreaMaterial;
            var along = horizontal ? Vector3.right : Vector3.forward;
            var dashScale = horizontal
                ? new Vector3(cellSize * 0.18f, 0.024f, cellSize * 0.04f)
                : new Vector3(cellSize * 0.04f, 0.024f, cellSize * 0.18f);
            AddLooseCube(guideObjects, "LockedRegionPerimeterBeaconPad", grassGridMaterial, center + new Vector3(0f, -0.045f, 0f), new Vector3(cellSize * 0.28f, 0.026f, cellSize * 0.28f));
            AddLooseCube(guideObjects, "LockedRegionPerimeterBeaconMast", lockedAreaMaterial, center + new Vector3(0f, 0.1f, 0f), new Vector3(0.04f, 0.22f, 0.04f));
            AddLooseCube(guideObjects, "LockedRegionPerimeterBeaconGlow", material, center + new Vector3(0f, 0.24f, 0f), new Vector3(cellSize * 0.12f, 0.045f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionPerimeterBeaconDash", roadLineMaterial, center + along * cellSize * 0.2f + new Vector3(0f, -0.02f, 0f), dashScale);
            AddLooseCube(guideObjects, "LockedRegionPerimeterBeaconDash", material, center - along * cellSize * 0.2f + new Vector3(0f, -0.02f, 0f), dashScale);
        }

        private void AddLockedCornerMarker(int gridX, int gridY, float dirX, float dirY)
        {
            var x = gridX * cellSize;
            var z = gridY * cellSize;
            AddLooseCube(guideObjects, "LockedRegionCornerBracket", lockedAreaMaterial, new Vector3(x + dirX * cellSize * 0.26f, 0.095f, z), new Vector3(cellSize * 0.5f, 0.045f, 0.06f));
            AddLooseCube(guideObjects, "LockedRegionCornerBracket", lockedAreaMaterial, new Vector3(x, 0.095f, z + dirY * cellSize * 0.26f), new Vector3(0.06f, 0.045f, cellSize * 0.5f));
        }

        private void AddLockedRegionPlanningField(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_REGION_PLANNING_FIELD fills the unlock area with faint parcel guides.
            for (var y = startY + 1; y < endY; y += 2)
            {
                for (var x = startX + 1; x < endX; x += 2)
                {
                    var center = CellCenter(new GridPos(x, y), 0.04f);
                    AddLooseCube(guideObjects, "LockedRegionParcelGhost", grassGridMaterial, center, new Vector3(cellSize * 0.58f, 0.012f, cellSize * 0.58f));
                    AddLooseCube(guideObjects, "LockedRegionParcelTick", lockedAreaMaterial, center + new Vector3(-cellSize * 0.29f, 0.02f, -cellSize * 0.29f), new Vector3(cellSize * 0.16f, 0.02f, 0.024f));
                    AddLooseCube(guideObjects, "LockedRegionParcelTick", lockedAreaMaterial, center + new Vector3(-cellSize * 0.29f, 0.02f, -cellSize * 0.29f), new Vector3(0.024f, 0.02f, cellSize * 0.16f));
                    AddLockedRegionParcelFrame(center, DecorationHash(x, y));
                }
            }

            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            AddLooseCube(guideObjects, "LockedRegionCenterTag", lockedAreaMaterial, new Vector3(centerX, 0.075f, centerZ + cellSize * 1.05f), new Vector3(cellSize * 1.75f, 0.03f, cellSize * 0.16f));
            AddLooseCube(guideObjects, "LockedRegionCenterTag", lockedAreaMaterial, new Vector3(centerX - cellSize * 0.8f, 0.08f, centerZ + cellSize * 0.85f), new Vector3(cellSize * 0.14f, 0.035f, cellSize * 0.42f));
            AddLockedRegionFutureSkyline(centerX, centerZ);
        }

        private void AddLockedRegionGroundPolish(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_GROUND_POLISH keeps the locked district bright, faceted, and intentionally unfinished.
            var progress = LockedRegionObjectiveProgress01();
            for (var y = startY + 1; y < endY; y += 2)
            {
                for (var x = startX + 1; x < endX; x += 2)
                {
                    var seed = DecorationHash(x, y);
                    if (seed % 3 == 1)
                    {
                        continue;
                    }

                    AddLockedRegionGroundFacet(new GridPos(x, y), seed, progress);
                }
            }
        }

        private void AddLockedRegionGroundFacet(GridPos pos, int seed, float progress)
        {
            var center = CellCenter(pos, 0.033f);
            var horizontal = (seed & 1) == 0;
            var patchMaterial = progress >= 0.62f && seed % 5 == 0 ? previewOkMaterial : (seed % 4 == 0 ? grassGridMaterial : lockedAreaMaterial);
            var patchScale = horizontal
                ? new Vector3(cellSize * 0.56f, 0.014f, cellSize * 0.3f)
                : new Vector3(cellSize * 0.3f, 0.014f, cellSize * 0.56f);
            var sunFacetScale = horizontal
                ? new Vector3(cellSize * 0.28f, 0.012f, cellSize * 0.055f)
                : new Vector3(cellSize * 0.055f, 0.012f, cellSize * 0.28f);
            var cornerPip = new Vector3((((seed >> 3) & 1) == 0 ? -1f : 1f) * cellSize * 0.18f, 0.028f, (((seed >> 4) & 1) == 0 ? -1f : 1f) * cellSize * 0.16f);

            AddLooseCube(guideObjects, "LockedRegionGroundFacet", patchMaterial, center, patchScale);
            AddLooseCube(guideObjects, "LockedRegionGroundSunFacet", roadLineMaterial, center + cornerPip, sunFacetScale);

            var detailKind = seed % 4;
            if (detailKind == 0)
            {
                AddLockedRegionGroundSapling(center, seed);
                return;
            }

            if (detailKind == 2)
            {
                AddLockedRegionGroundRockPile(center, seed);
                return;
            }

            AddLockedRegionGroundBuildStack(center, seed, horizontal);
        }

        private void AddLockedRegionGroundSapling(Vector3 center, int seed)
        {
            var side = ((seed >> 5) & 1) == 0 ? -1f : 1f;
            var trunkCenter = center + new Vector3(side * cellSize * 0.18f, 0.105f, -side * cellSize * 0.08f);
            AddLooseCube(guideObjects, "LockedRegionGroundSaplingShadow", buildingFootprintMaterial, trunkCenter + new Vector3(0.025f, -0.06f, 0.025f), new Vector3(cellSize * 0.18f, 0.01f, cellSize * 0.14f));
            AddLooseCube(guideObjects, "LockedRegionGroundSaplingTrunk", treeTrunkMaterial, trunkCenter, new Vector3(0.04f, 0.18f, 0.04f));
            AddLooseCube(guideObjects, "LockedRegionGroundSaplingCanopy", treeCanopyMaterial, trunkCenter + new Vector3(0f, 0.15f, 0f), new Vector3(cellSize * 0.17f, 0.14f, cellSize * 0.17f));
            AddLooseCube(guideObjects, "LockedRegionGroundFlowerPip", serviceNeedMaterial, center + new Vector3(-side * cellSize * 0.18f, 0.036f, side * cellSize * 0.16f), new Vector3(cellSize * 0.06f, 0.036f, cellSize * 0.06f));
        }

        private void AddLockedRegionGroundRockPile(Vector3 center, int seed)
        {
            var side = ((seed >> 6) & 1) == 0 ? -1f : 1f;
            var rockCenter = center + new Vector3(side * cellSize * 0.16f, 0.046f, side * cellSize * 0.12f);
            AddLooseCube(guideObjects, "LockedRegionGroundRock", rockMaterial, rockCenter, new Vector3(cellSize * 0.16f, 0.08f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionGroundPebble", rockMaterial, center + new Vector3(-side * cellSize * 0.08f, 0.028f, -side * cellSize * 0.22f), new Vector3(cellSize * 0.08f, 0.052f, cellSize * 0.07f));
            AddLooseCube(guideObjects, "LockedRegionGroundRockGlint", shoreMaterial != null ? shoreMaterial : roadLineMaterial, rockCenter + new Vector3(0f, 0.052f, 0f), new Vector3(cellSize * 0.09f, 0.016f, cellSize * 0.045f));
        }

        private void AddLockedRegionGroundBuildStack(Vector3 center, int seed, bool horizontal)
        {
            var along = horizontal ? Vector3.right : Vector3.forward;
            var cross = horizontal ? Vector3.forward : Vector3.right;
            var side = ((seed >> 7) & 1) == 0 ? -1f : 1f;
            var stackCenter = center + cross * side * cellSize * 0.16f + new Vector3(0f, 0.045f, 0f);
            var plankScale = horizontal
                ? new Vector3(cellSize * 0.28f, 0.036f, cellSize * 0.055f)
                : new Vector3(cellSize * 0.055f, 0.036f, cellSize * 0.28f);
            AddLooseCube(guideObjects, "LockedRegionGroundMaterialStack", serviceMaterial, stackCenter, plankScale);
            AddLooseCube(guideObjects, "LockedRegionGroundMaterialTop", roadLineMaterial, stackCenter + new Vector3(0f, 0.04f, 0f) - along * cellSize * 0.03f, plankScale * 0.82f);
            AddLooseCube(guideObjects, "LockedRegionGroundSurveyPeg", lockedAreaMaterial, center - cross * side * cellSize * 0.2f + along * cellSize * 0.18f + new Vector3(0f, 0.07f, 0f), new Vector3(0.04f, 0.14f, 0.04f));
        }

        private void AddLockedRegionParcelFrame(Vector3 center, int seed)
        {
            // LOW_POLY_LOCKED_PARCEL_FRAMES makes future lots read as dashed planned tiles.
            var inset = cellSize * 0.33f;
            var dash = cellSize * 0.18f;
            var y = 0.072f;
            AddLooseCube(guideObjects, "LockedRegionParcelFrameDash", roadLineMaterial, center + new Vector3(-inset, y, -inset * 0.35f), new Vector3(0.024f, 0.018f, dash));
            AddLooseCube(guideObjects, "LockedRegionParcelFrameDash", roadLineMaterial, center + new Vector3(inset, y, inset * 0.35f), new Vector3(0.024f, 0.018f, dash));
            AddLooseCube(guideObjects, "LockedRegionParcelFrameDash", roadLineMaterial, center + new Vector3(-inset * 0.35f, y, inset), new Vector3(dash, 0.018f, 0.024f));
            AddLooseCube(guideObjects, "LockedRegionParcelFrameDash", roadLineMaterial, center + new Vector3(inset * 0.35f, y, -inset), new Vector3(dash, 0.018f, 0.024f));

            if (seed % 3 == 0)
            {
                AddLockedRegionParcelMiniLock(center);
            }
        }

        private void AddLockedRegionParcelMiniLock(Vector3 center)
        {
            AddLooseCube(guideObjects, "LockedRegionParcelMiniLockBase", lockedAreaMaterial, center + new Vector3(cellSize * 0.18f, 0.105f, -cellSize * 0.18f), new Vector3(cellSize * 0.16f, 0.07f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionParcelMiniLockShackle", roadLineMaterial, center + new Vector3(cellSize * 0.18f, 0.165f, -cellSize * 0.18f), new Vector3(cellSize * 0.2f, 0.035f, cellSize * 0.055f));
            AddLooseCube(guideObjects, "LockedRegionParcelMiniLockDot", roadLineMaterial, center + new Vector3(cellSize * 0.18f, 0.112f, -cellSize * 0.18f), new Vector3(cellSize * 0.045f, 0.026f, cellSize * 0.035f));
        }

        private void AddLockedRegionFutureSkyline(float centerX, float centerZ)
        {
            // REFERENCE_IMAGE_LOCKED_FUTURE_SKYLINE hints at the next district without creating real buildings.
            var baseCenter = new Vector3(centerX + cellSize * 0.32f, 0.08f, centerZ - cellSize * 0.48f);
            AddLooseCube(guideObjects, "LockedRegionFutureBlockFootprint", grassGridMaterial, baseCenter, new Vector3(cellSize * 0.56f, 0.024f, cellSize * 0.48f));
            AddLooseCube(guideObjects, "LockedRegionFutureTowerGhost", lockedAreaMaterial, baseCenter + new Vector3(-cellSize * 0.16f, 0.24f, -cellSize * 0.06f), new Vector3(cellSize * 0.18f, 0.42f, cellSize * 0.18f));
            AddLooseCube(guideObjects, "LockedRegionFutureTowerGhost", lockedAreaMaterial, baseCenter + new Vector3(cellSize * 0.08f, 0.18f, cellSize * 0.12f), new Vector3(cellSize * 0.2f, 0.3f, cellSize * 0.18f));
            AddLooseCube(guideObjects, "LockedRegionFutureRoofGlow", roadLineMaterial, baseCenter + new Vector3(-cellSize * 0.16f, 0.47f, -cellSize * 0.06f), new Vector3(cellSize * 0.16f, 0.035f, cellSize * 0.08f));
            AddLooseCube(guideObjects, "LockedRegionFutureAccessPath", roadLineMaterial, baseCenter + new Vector3(-cellSize * 0.08f, 0.035f, cellSize * 0.34f), new Vector3(cellSize * 0.42f, 0.022f, cellSize * 0.055f));
        }

        private void AddLockedRegionSurveyStakes(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_SURVEY_STAKES reads the expansion area as a planned construction site.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            AddLockedRegionSurveyPair(new Vector3(startX * cellSize + cellSize * 0.32f, 0.12f, centerZ - cellSize * 0.58f), true);
            AddLockedRegionSurveyPair(new Vector3(centerX - cellSize * 0.58f, 0.12f, startY * cellSize + cellSize * 0.32f), false);
        }

        private void AddLockedRegionSurveyPair(Vector3 center, bool horizontal)
        {
            var offset = horizontal ? Vector3.right * cellSize * 0.24f : Vector3.forward * cellSize * 0.24f;
            var tapeScale = horizontal
                ? new Vector3(cellSize * 0.48f, 0.035f, 0.035f)
                : new Vector3(0.035f, 0.035f, cellSize * 0.48f);
            AddLooseCube(guideObjects, "LockedRegionSurveyStake", lockedAreaMaterial, center - offset + new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.22f, 0.05f));
            AddLooseCube(guideObjects, "LockedRegionSurveyStake", lockedAreaMaterial, center + offset + new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.22f, 0.05f));
            AddLooseCube(guideObjects, "LockedRegionSurveyTape", roadLineMaterial, center + new Vector3(0f, 0.19f, 0f), tapeScale);
        }

        private void AddLockedRegionPlanningStakes(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_PLANNING_STAKES makes future parcels feel surveyed and build-ready.
            var midY = (startY + endY + 1) * 0.5f;
            for (var x = startX + 1; x < endX; x += 3)
            {
                AddLockedRegionPlanningStake(new Vector3((x + 0.5f) * cellSize, 0.12f, (midY + 0.18f) * cellSize), true, DecorationHash(x, startY));
            }

            var midX = (startX + endX + 1) * 0.5f;
            for (var y = startY + 2; y < endY; y += 3)
            {
                AddLockedRegionPlanningStake(new Vector3((midX - 0.18f) * cellSize, 0.12f, (y + 0.5f) * cellSize), false, DecorationHash(startX, y));
            }
        }

        private void AddLockedRegionPlanningStake(Vector3 center, bool horizontal, int seed)
        {
            var lineScale = horizontal
                ? new Vector3(cellSize * 0.42f, 0.026f, 0.032f)
                : new Vector3(0.032f, 0.026f, cellSize * 0.42f);
            var flagScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.06f, 0.032f)
                : new Vector3(0.032f, 0.06f, cellSize * 0.16f);
            var lineOffset = horizontal ? Vector3.right * cellSize * 0.2f : Vector3.forward * cellSize * 0.2f;
            AddLooseCube(guideObjects, "LockedRegionPlanningStakePost", lockedAreaMaterial, center + new Vector3(0f, 0.08f, 0f), new Vector3(0.045f, 0.22f, 0.045f));
            AddLooseCube(guideObjects, "LockedRegionPlanningStakeString", roadLineMaterial, center + lineOffset * 0.5f + new Vector3(0f, 0.2f, 0f), lineScale);
            AddLooseCube(guideObjects, "LockedRegionPlanningStakeFlag", seed % 2 == 0 ? serviceNeedMaterial : roadLineMaterial, center + lineOffset * 0.28f + new Vector3(0f, 0.3f, 0f), flagScale);
        }

        private void AddLockedRegionBoundaryCones(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_BOUNDARY_WORKSITE gives the future district edge a readable construction state.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            AddLockedRegionCone(new Vector3(centerX - cellSize * 0.72f, 0.1f, (startY - 0.18f) * cellSize));
            AddLockedRegionCone(new Vector3(centerX + cellSize * 0.72f, 0.1f, (startY - 0.18f) * cellSize));
            AddLockedRegionCone(new Vector3((startX - 0.18f) * cellSize, 0.1f, centerZ - cellSize * 0.72f));
            AddLockedRegionCone(new Vector3((startX - 0.18f) * cellSize, 0.1f, centerZ + cellSize * 0.72f));
        }

        private void AddLockedRegionCone(Vector3 center)
        {
            AddLooseCube(guideObjects, "LockedRegionConeBase", roadLineMaterial, center, new Vector3(cellSize * 0.16f, 0.035f, cellSize * 0.16f));
            AddLooseCube(guideObjects, "LockedRegionConeBody", serviceNeedMaterial, center + new Vector3(0f, 0.075f, 0f), new Vector3(cellSize * 0.1f, 0.13f, cellSize * 0.1f));
            AddLooseCube(guideObjects, "LockedRegionConeStripe", roadLineMaterial, center + new Vector3(0f, 0.135f, 0f), new Vector3(cellSize * 0.12f, 0.025f, cellSize * 0.12f));
        }

        private void AddLockedRegionPlanningStripes(int startX, int startY, int endX, int endY)
        {
            // CITY_SKYLINES_LOCKED_REGION_PLANNING_STRIPES makes the expansion area feel like a future district.
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var usableHeight = Mathf.Max(cellSize * 2f, (endY - startY - 1) * cellSize * 0.55f);
            for (var x = startX + 1; x < endX; x += 2)
            {
                var center = new Vector3((x + 0.5f) * cellSize, 0.066f, centerZ);
                AddLooseCubeRotated(guideObjects, "LockedRegionPlanningStripe", roadLineMaterial, center, new Vector3(cellSize * 0.045f, 0.022f, usableHeight), 42f);
            }
        }

        private void AddLockedRegionInnerDashedGuide(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_INNER_DASHES reads as an unavailable but planned expansion district.
            var ySouth = (startY + 1.15f) * cellSize;
            var yNorth = (endY - 0.15f) * cellSize;
            for (var x = startX + 2; x < endX; x += 2)
            {
                var centerX = (x + 0.5f) * cellSize;
                AddLooseCube(guideObjects, "LockedRegionInnerDash", roadLineMaterial, new Vector3(centerX, 0.118f, ySouth), new Vector3(cellSize * 0.46f, 0.025f, cellSize * 0.045f));
                AddLooseCube(guideObjects, "LockedRegionInnerDash", roadLineMaterial, new Vector3(centerX, 0.118f, yNorth), new Vector3(cellSize * 0.46f, 0.025f, cellSize * 0.045f));
            }

            var xWest = (startX + 1.15f) * cellSize;
            var xEast = (endX - 0.15f) * cellSize;
            for (var y = startY + 2; y < endY; y += 2)
            {
                var centerZ = (y + 0.5f) * cellSize;
                AddLooseCube(guideObjects, "LockedRegionInnerDash", roadLineMaterial, new Vector3(xWest, 0.12f, centerZ), new Vector3(cellSize * 0.045f, 0.025f, cellSize * 0.46f));
                AddLooseCube(guideObjects, "LockedRegionInnerDash", roadLineMaterial, new Vector3(xEast, 0.12f, centerZ), new Vector3(cellSize * 0.045f, 0.025f, cellSize * 0.46f));
            }

            AddLockedRegionMiniLockSign(new Vector3(xWest, 0.16f, ySouth), true);
            AddLockedRegionMiniLockSign(new Vector3(xEast, 0.16f, yNorth), false);
        }

        private void AddLockedRegionMiniLockSign(Vector3 center, bool horizontal)
        {
            var signScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.06f, cellSize * 0.12f)
                : new Vector3(cellSize * 0.12f, 0.06f, cellSize * 0.34f);
            AddLooseCube(guideObjects, "LockedRegionMiniLockSignBase", lockedAreaMaterial, center, signScale);
            AddLooseCube(guideObjects, "LockedRegionMiniLockBody", roadLineMaterial, center + new Vector3(0f, 0.08f, 0f), new Vector3(cellSize * 0.14f, 0.09f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionMiniLockShackle", lockedAreaMaterial, center + new Vector3(0f, 0.15f, 0f), new Vector3(cellSize * 0.19f, 0.04f, cellSize * 0.06f));
        }

        private void AddLooseCubeRotated(List<GameObject> list, string name, Material material, Vector3 position, Vector3 scale, float yaw)
        {
            var obj = CreateCube(name, material);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            obj.transform.localScale = scale;
            list.Add(obj);
        }

        private void AddLockedRegionFutureRoadGrid(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_FUTURE_ROAD_GRID makes the expansion area read as a planned district.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var spanX = Mathf.Max(cellSize * 2.2f, (endX - startX - 1) * cellSize * 0.82f);
            var spanZ = Mathf.Max(cellSize * 2.2f, (endY - startY - 1) * cellSize * 0.82f);
            AddLooseCube(guideObjects, "LockedRegionFutureRoadGhost", roadLineMaterial, new Vector3(centerX, 0.09f, centerZ), new Vector3(spanX, 0.026f, cellSize * 0.08f));
            AddLooseCube(guideObjects, "LockedRegionFutureRoadGhost", roadLineMaterial, new Vector3(centerX, 0.092f, centerZ), new Vector3(cellSize * 0.08f, 0.026f, spanZ));

            var nodeSize = cellSize * 0.16f;
            AddLooseCube(guideObjects, "LockedRegionFutureNode", lockedAreaMaterial, new Vector3(centerX - spanX * 0.32f, 0.13f, centerZ), new Vector3(nodeSize, 0.08f, nodeSize));
            AddLooseCube(guideObjects, "LockedRegionFutureNode", lockedAreaMaterial, new Vector3(centerX + spanX * 0.32f, 0.13f, centerZ), new Vector3(nodeSize, 0.08f, nodeSize));
            AddLooseCube(guideObjects, "LockedRegionFutureNode", lockedAreaMaterial, new Vector3(centerX, 0.13f, centerZ - spanZ * 0.32f), new Vector3(nodeSize, 0.08f, nodeSize));
            AddLooseCube(guideObjects, "LockedRegionFutureNode", lockedAreaMaterial, new Vector3(centerX, 0.13f, centerZ + spanZ * 0.32f), new Vector3(nodeSize, 0.08f, nodeSize));
            AddLockedRegionSecondaryPlanGrid(centerX, centerZ, spanX, spanZ);
            AddLockedRegionOverviewNodes(centerX, centerZ, spanX, spanZ);
        }

        private void AddLockedRegionSecondaryPlanGrid(float centerX, float centerZ, float spanX, float spanZ)
        {
            // LOW_POLY_FUTURE_DISTRICT_SPURS suggests planned streets while keeping the locked area sparse.
            var roadScaleX = new Vector3(spanX * 0.34f, 0.018f, cellSize * 0.045f);
            var roadScaleZ = new Vector3(cellSize * 0.045f, 0.018f, spanZ * 0.34f);
            var offsetX = spanX * 0.22f;
            var offsetZ = spanZ * 0.22f;
            AddLooseCube(guideObjects, "LockedRegionFutureRoadSpur", roadLineMaterial, new Vector3(centerX - offsetX, 0.104f, centerZ - offsetZ), roadScaleX);
            AddLooseCube(guideObjects, "LockedRegionFutureRoadSpur", roadLineMaterial, new Vector3(centerX + offsetX, 0.104f, centerZ + offsetZ), roadScaleX);
            AddLooseCube(guideObjects, "LockedRegionFutureRoadSpur", roadLineMaterial, new Vector3(centerX - offsetX, 0.106f, centerZ + offsetZ), roadScaleZ);
            AddLooseCube(guideObjects, "LockedRegionFutureRoadSpur", roadLineMaterial, new Vector3(centerX + offsetX, 0.106f, centerZ - offsetZ), roadScaleZ);

            var nodeSize = cellSize * 0.12f;
            AddLooseCube(guideObjects, "LockedRegionFuturePlanNode", grassGridMaterial, new Vector3(centerX - offsetX, 0.13f, centerZ - offsetZ), new Vector3(nodeSize, 0.055f, nodeSize));
            AddLooseCube(guideObjects, "LockedRegionFuturePlanNode", grassGridMaterial, new Vector3(centerX + offsetX, 0.13f, centerZ + offsetZ), new Vector3(nodeSize, 0.055f, nodeSize));
            AddLooseCube(guideObjects, "LockedRegionFuturePlanNode", grassGridMaterial, new Vector3(centerX - offsetX, 0.13f, centerZ + offsetZ), new Vector3(nodeSize, 0.055f, nodeSize));
            AddLooseCube(guideObjects, "LockedRegionFuturePlanNode", grassGridMaterial, new Vector3(centerX + offsetX, 0.13f, centerZ - offsetZ), new Vector3(nodeSize, 0.055f, nodeSize));
        }

        private void AddLockedRegionOverviewNodes(float centerX, float centerZ, float spanX, float spanZ)
        {
            // REFERENCE_IMAGE_LOCKED_OVERVIEW_NODES adds small minimap-like planning dots inside the locked district.
            var nodeSize = cellSize * 0.1f;
            var tickScaleX = new Vector3(cellSize * 0.22f, 0.02f, cellSize * 0.035f);
            var tickScaleZ = new Vector3(cellSize * 0.035f, 0.02f, cellSize * 0.22f);
            var nodes = new[]
            {
                new Vector3(centerX - spanX * 0.18f, 0.152f, centerZ),
                new Vector3(centerX + spanX * 0.18f, 0.152f, centerZ),
                new Vector3(centerX, 0.152f, centerZ - spanZ * 0.18f),
                new Vector3(centerX, 0.152f, centerZ + spanZ * 0.18f)
            };

            for (var i = 0; i < nodes.Length; i += 1)
            {
                var material = i % 2 == 0 ? roadLineMaterial : lockedAreaMaterial;
                AddLooseCube(guideObjects, "LockedRegionOverviewNode", material, nodes[i], new Vector3(nodeSize, 0.055f, nodeSize));
                AddLooseCube(guideObjects, "LockedRegionOverviewRouteTick", roadLineMaterial, nodes[i] + new Vector3(0f, 0.028f, 0f), i < 2 ? tickScaleX : tickScaleZ);
            }
        }

        private void AddLockedRegionBlueprintMicroDetails(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_BLUEPRINT_MICRODETAILS gives the unopened district small measured-plan marks.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var spanX = Mathf.Max(cellSize * 1.4f, (endX - startX + 1) * cellSize * 0.36f);
            var spanZ = Mathf.Max(cellSize * 1.4f, (endY - startY + 1) * cellSize * 0.36f);
            var progress = LockedRegionObjectiveProgress01();
            var material = progress >= 0.5f ? windowMaterial : lockedAreaMaterial;

            AddLooseCube(guideObjects, "LockedRegionBlueprintCornerPin", material, new Vector3(centerX - spanX * 0.42f, 0.164f, centerZ - spanZ * 0.42f), new Vector3(cellSize * 0.11f, 0.044f, cellSize * 0.11f));
            AddLooseCube(guideObjects, "LockedRegionBlueprintCornerPin", roadLineMaterial, new Vector3(centerX + spanX * 0.42f, 0.166f, centerZ + spanZ * 0.42f), new Vector3(cellSize * 0.11f, 0.044f, cellSize * 0.11f));
            AddLooseCube(guideObjects, "LockedRegionBlueprintParcelDash", roadLineMaterial, new Vector3(centerX - spanX * 0.18f, 0.158f, centerZ + spanZ * 0.28f), new Vector3(cellSize * 0.3f, 0.022f, cellSize * 0.035f));
            AddLooseCube(guideObjects, "LockedRegionBlueprintParcelDash", material, new Vector3(centerX + spanX * 0.24f, 0.16f, centerZ - spanZ * 0.18f), new Vector3(cellSize * 0.035f, 0.022f, cellSize * 0.3f));
        }

        private void AddLockedRegionGatewayStubs(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_GATEWAY_STUBS show how the future district will connect to the city grid.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var westX = (startX - 0.42f) * cellSize;
            var southZ = (startY - 0.42f) * cellSize;
            AddLooseCube(guideObjects, "LockedRegionGatewayRoadStub", roadMaterial, new Vector3(westX, 0.082f, centerZ), new Vector3(cellSize * 0.82f, 0.032f, cellSize * 0.2f));
            AddLooseCube(guideObjects, "LockedRegionGatewayRoadLine", roadLineMaterial, new Vector3(westX, 0.108f, centerZ), new Vector3(cellSize * 0.52f, 0.026f, 0.026f));
            AddLooseCube(guideObjects, "LockedRegionGatewayRoadStub", roadMaterial, new Vector3(centerX, 0.084f, southZ), new Vector3(cellSize * 0.2f, 0.032f, cellSize * 0.82f));
            AddLooseCube(guideObjects, "LockedRegionGatewayRoadLine", roadLineMaterial, new Vector3(centerX, 0.11f, southZ), new Vector3(0.026f, 0.026f, cellSize * 0.52f));

            AddLooseCube(guideObjects, "LockedRegionConstructionGate", lockedAreaMaterial, new Vector3(startX * cellSize + cellSize * 0.04f, 0.14f, centerZ), new Vector3(0.08f, 0.18f, cellSize * 0.5f));
            AddLooseCube(guideObjects, "LockedRegionConstructionGate", lockedAreaMaterial, new Vector3(centerX, 0.14f, startY * cellSize + cellSize * 0.04f), new Vector3(cellSize * 0.5f, 0.18f, 0.08f));
        }

        private void AddLockedRegionEdgeGreenery(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_LOCKED_EDGE_GREENERY keeps the future district border fresh instead of empty.
            for (var x = startX + 1; x < endX; x += 3)
            {
                AddLockedRegionEdgePlanter(new Vector3((x + 0.5f) * cellSize, 0.09f, (startY - 0.28f) * cellSize), true, DecorationHash(x, startY));
            }

            for (var y = startY + 1; y < endY; y += 3)
            {
                AddLockedRegionEdgePlanter(new Vector3((startX - 0.28f) * cellSize, 0.09f, (y + 0.5f) * cellSize), false, DecorationHash(startX, y));
            }
        }

        private void AddLockedRegionEdgePlanter(Vector3 center, bool horizontal, int seed)
        {
            var padScale = horizontal
                ? new Vector3(cellSize * 0.46f, 0.026f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.026f, cellSize * 0.46f);
            var flowerScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.04f, cellSize * 0.08f)
                : new Vector3(cellSize * 0.08f, 0.04f, cellSize * 0.16f);
            var flowerOffset = horizontal
                ? new Vector3(cellSize * 0.12f, 0.03f, 0f)
                : new Vector3(0f, 0.03f, cellSize * 0.12f);
            var saplingOffset = horizontal
                ? new Vector3(-cellSize * 0.13f, 0f, 0f)
                : new Vector3(0f, 0f, -cellSize * 0.13f);

            AddLooseCube(guideObjects, "LockedRegionEdgeGreenPatch", grassGridMaterial, center, padScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeFlowerPatch", serviceNeedMaterial, center + flowerOffset, flowerScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeSaplingTrunk", treeTrunkMaterial, center + saplingOffset + new Vector3(0f, 0.1f, 0f), new Vector3(0.04f, 0.18f, 0.04f));
            AddLooseCube(guideObjects, "LockedRegionEdgeSaplingCanopy", treeCanopyMaterial, center + saplingOffset + new Vector3(0f, 0.23f, 0f), new Vector3(0.16f, 0.14f, 0.16f));
            if (seed % 2 == 0)
            {
                var pathScale = horizontal
                    ? new Vector3(cellSize * 0.24f, 0.018f, 0.035f)
                    : new Vector3(0.035f, 0.018f, cellSize * 0.24f);
                AddLooseCube(guideObjects, "LockedRegionEdgePathTile", roadLineMaterial, center - flowerOffset * 0.72f + new Vector3(0f, 0.032f, 0f), pathScale);
            }
        }

        private void AddLockedRegionHint(int startX, int startY, int endX, int endY)
        {
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var center = new Vector3(centerX, 0.11f, centerZ);
            AddLooseCube(guideObjects, "LockedRegionHintPad", grassGridMaterial, center, new Vector3(cellSize * 1.35f, 0.035f, cellSize * 0.92f));
            AddLooseCube(guideObjects, "LockedRegionHintBody", lockedAreaMaterial, center + new Vector3(0f, 0.12f, 0f), new Vector3(cellSize * 0.46f, 0.18f, cellSize * 0.4f));
            AddLooseCube(guideObjects, "LockedRegionHintShackleLeft", roadLineMaterial, center + new Vector3(-cellSize * 0.15f, 0.28f, 0f), new Vector3(0.08f, 0.24f, 0.08f));
            AddLooseCube(guideObjects, "LockedRegionHintShackleRight", roadLineMaterial, center + new Vector3(cellSize * 0.15f, 0.28f, 0f), new Vector3(0.08f, 0.24f, 0.08f));
            AddLooseCube(guideObjects, "LockedRegionHintShackleTop", roadLineMaterial, center + new Vector3(0f, 0.38f, 0f), new Vector3(cellSize * 0.38f, 0.08f, 0.08f));
        }

        private void AddLockedRegionUnlockWorksite(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_UNLOCK_WORKSITE adds a bright, tangible next-district staging point.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var progress = LockedRegionObjectiveProgress01();
            var activeMaterial = progress >= 0.5f ? windowMaterial : lockedAreaMaterial;
            var warmMaterial = progress >= 0.78f ? previewOkMaterial : serviceNeedMaterial;
            var westAnchor = new Vector3(centerX - cellSize * 0.78f, 0.1f, centerZ - cellSize * 0.54f);
            var eastAnchor = new Vector3(centerX + cellSize * 0.76f, 0.1f, centerZ + cellSize * 0.46f);

            AddLockedRegionToolCrate(westAnchor, true, activeMaterial);
            AddLockedRegionToolCrate(eastAnchor, false, warmMaterial);
            AddLockedRegionMiniDozer(new Vector3(centerX + cellSize * 0.42f, 0.12f, centerZ - cellSize * 0.66f), true, warmMaterial);
            AddLockedRegionUnlockArrow(new Vector3(centerX - cellSize * 0.08f, 0.142f, centerZ - cellSize * 0.72f), true, activeMaterial);
            AddLockedRegionUnlockArrow(new Vector3(centerX + cellSize * 0.62f, 0.146f, centerZ + cellSize * 0.08f), false, activeMaterial);

            if (progress >= 0.66f)
            {
                AddLooseCube(guideObjects, "LockedRegionWorksiteReadySpark", previewOkMaterial, new Vector3(centerX - cellSize * 0.36f, 0.46f, centerZ + cellSize * 0.42f), new Vector3(cellSize * 0.12f, 0.07f, cellSize * 0.12f));
                AddLooseCube(guideObjects, "LockedRegionWorksiteReadySpark", roadLineMaterial, new Vector3(centerX + cellSize * 0.28f, 0.52f, centerZ - cellSize * 0.28f), new Vector3(cellSize * 0.1f, 0.065f, cellSize * 0.1f));
            }
        }

        private void AddLockedRegionToolCrate(Vector3 center, bool horizontal, Material accentMaterial)
        {
            var padScale = horizontal
                ? new Vector3(cellSize * 0.52f, 0.03f, cellSize * 0.26f)
                : new Vector3(cellSize * 0.26f, 0.03f, cellSize * 0.52f);
            var stripeScale = horizontal
                ? new Vector3(cellSize * 0.32f, 0.022f, cellSize * 0.04f)
                : new Vector3(cellSize * 0.04f, 0.022f, cellSize * 0.32f);
            var postOffset = horizontal ? Vector3.right * cellSize * 0.22f : Vector3.forward * cellSize * 0.22f;

            AddLooseCube(guideObjects, "LockedRegionWorksitePad", grassGridMaterial, center, padScale);
            AddLooseCube(guideObjects, "LockedRegionWorksiteToolCrate", serviceMaterial, center + new Vector3(0f, 0.095f, 0f), new Vector3(cellSize * 0.22f, 0.14f, cellSize * 0.18f));
            AddLooseCube(guideObjects, "LockedRegionWorksiteToolLid", accentMaterial, center + new Vector3(0f, 0.18f, 0f), new Vector3(cellSize * 0.25f, 0.035f, cellSize * 0.2f));
            AddLooseCube(guideObjects, "LockedRegionWorksiteMeasureStripe", roadLineMaterial, center - postOffset * 0.35f + new Vector3(0f, 0.045f, 0f), stripeScale);
            AddLooseCube(guideObjects, "LockedRegionWorksiteSurveyPost", roadLineMaterial, center + postOffset + new Vector3(0f, 0.16f, 0f), new Vector3(0.035f, 0.3f, 0.035f));
            AddLooseCube(guideObjects, "LockedRegionWorksiteSurveyFlag", accentMaterial, center + postOffset + new Vector3(0f, 0.31f, 0f), horizontal ? new Vector3(cellSize * 0.16f, 0.055f, 0.032f) : new Vector3(0.032f, 0.055f, cellSize * 0.16f));
        }

        private void AddLockedRegionMiniDozer(Vector3 center, bool horizontal, Material accentMaterial)
        {
            var bodyScale = horizontal
                ? new Vector3(cellSize * 0.34f, 0.14f, cellSize * 0.18f)
                : new Vector3(cellSize * 0.18f, 0.14f, cellSize * 0.34f);
            var cabinScale = horizontal
                ? new Vector3(cellSize * 0.15f, 0.11f, cellSize * 0.14f)
                : new Vector3(cellSize * 0.14f, 0.11f, cellSize * 0.15f);
            var bladeScale = horizontal
                ? new Vector3(cellSize * 0.08f, 0.1f, cellSize * 0.28f)
                : new Vector3(cellSize * 0.28f, 0.1f, cellSize * 0.08f);
            var trackScale = horizontal
                ? new Vector3(cellSize * 0.32f, 0.035f, cellSize * 0.055f)
                : new Vector3(cellSize * 0.055f, 0.035f, cellSize * 0.32f);
            var forward = horizontal ? Vector3.right : Vector3.forward;
            var side = horizontal ? Vector3.forward : Vector3.right;

            AddLooseCube(guideObjects, "LockedRegionMiniDozerTrack", roadMaterial, center - side * cellSize * 0.08f, trackScale);
            AddLooseCube(guideObjects, "LockedRegionMiniDozerTrack", roadMaterial, center + side * cellSize * 0.08f, trackScale);
            AddLooseCube(guideObjects, "LockedRegionMiniDozerBody", accentMaterial, center + new Vector3(0f, 0.09f, 0f), bodyScale);
            AddLooseCube(guideObjects, "LockedRegionMiniDozerCab", windowMaterial, center - forward * cellSize * 0.08f + new Vector3(0f, 0.205f, 0f), cabinScale);
            AddLooseCube(guideObjects, "LockedRegionMiniDozerBlade", roadLineMaterial, center + forward * cellSize * 0.24f + new Vector3(0f, 0.075f, 0f), bladeScale);
        }

        private void AddLockedRegionUnlockArrow(Vector3 center, bool horizontal, Material material)
        {
            var shaftScale = horizontal
                ? new Vector3(cellSize * 0.38f, 0.024f, cellSize * 0.055f)
                : new Vector3(cellSize * 0.055f, 0.024f, cellSize * 0.38f);
            var headOffset = horizontal ? Vector3.right * cellSize * 0.24f : Vector3.forward * cellSize * 0.24f;

            AddLooseCube(guideObjects, "LockedRegionWorksiteArrowShaft", material, center, shaftScale);
            AddLooseCubeRotated(guideObjects, "LockedRegionWorksiteArrowHead", roadLineMaterial, center + headOffset, new Vector3(cellSize * 0.14f, 0.026f, cellSize * 0.052f), horizontal ? 45f : -45f);
            AddLooseCubeRotated(guideObjects, "LockedRegionWorksiteArrowHead", roadLineMaterial, center + headOffset, new Vector3(cellSize * 0.14f, 0.026f, cellSize * 0.052f), horizontal ? -45f : 45f);
        }

        private void AddLockedRegionProgressCues(int startX, int startY, int endX, int endY)
        {
            // CITY_SKYLINES_LOCKED_REGION_PROGRESS projects the active milestone onto the future district.
            var amount = LockedRegionObjectiveProgress01();
            var filled = Mathf.Clamp(Mathf.CeilToInt(amount * 4f), 0, 4);
            var positions = new[]
            {
                new Vector3((startX + 1.2f) * cellSize, 0.135f, (startY + 0.28f) * cellSize),
                new Vector3((endX - 1.2f) * cellSize, 0.135f, (startY + 0.28f) * cellSize),
                new Vector3((endX + 0.72f) * cellSize, 0.135f, (endY - 1.2f) * cellSize),
                new Vector3((startX + 0.28f) * cellSize, 0.135f, (endY - 1.2f) * cellSize)
            };

            for (var i = 0; i < positions.Length; i += 1)
            {
                var done = i < filled;
                var material = done ? roadLineMaterial : lockedAreaMaterial;
                var width = done ? cellSize * 0.48f : cellSize * 0.34f;
                AddLooseCube(guideObjects, done ? "LockedRegionProgressTickFilled" : "LockedRegionProgressTickPending", material, positions[i], new Vector3(width, 0.052f, cellSize * 0.09f));
                if (done)
                {
                    AddLooseCube(guideObjects, "LockedRegionProgressTickGlow", lockedAreaMaterial, positions[i] + new Vector3(0f, 0.045f, 0f), new Vector3(width * 0.82f, 0.03f, cellSize * 0.18f));
                }
            }

            if (amount >= 0.82f)
            {
                AddLockedRegionReadyCorners(startX, startY, endX, endY);
            }
        }

        private void AddLockedRegionReadyCorners(int startX, int startY, int endX, int endY)
        {
            AddLooseCube(guideObjects, "LockedRegionReadyCornerGlow", roadLineMaterial, new Vector3(startX * cellSize, 0.18f, startY * cellSize), new Vector3(cellSize * 0.48f, 0.055f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionReadyCornerGlow", roadLineMaterial, new Vector3((endX + 1f) * cellSize, 0.18f, startY * cellSize), new Vector3(cellSize * 0.48f, 0.055f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionReadyCornerGlow", roadLineMaterial, new Vector3(startX * cellSize, 0.18f, (endY + 1f) * cellSize), new Vector3(cellSize * 0.48f, 0.055f, cellSize * 0.12f));
            AddLooseCube(guideObjects, "LockedRegionReadyCornerGlow", roadLineMaterial, new Vector3((endX + 1f) * cellSize, 0.18f, (endY + 1f) * cellSize), new Vector3(cellSize * 0.48f, 0.055f, cellSize * 0.12f));
        }

        private float LockedRegionObjectiveProgress01()
        {
            var objective = controller != null && controller.Metrics != null ? controller.Metrics.ActiveObjective : null;
            if (objective == null)
            {
                return 0f;
            }

            var required = Mathf.Max(1, objective.Required);
            return objective.Done ? 1f : Mathf.Clamp01(objective.Progress / (float)required);
        }

        private void AddLockedRegionUnlockBeacons(int startX, int startY, int endX, int endY)
        {
            // REFERENCE_IMAGE_UNLOCK_BEACONS makes the locked expansion read like a bright pending milestone.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var offsetX = Mathf.Max(cellSize * 0.95f, (endX - startX) * cellSize * 0.18f);
            var offsetZ = Mathf.Max(cellSize * 0.95f, (endY - startY) * cellSize * 0.18f);
            var amount = LockedRegionObjectiveProgress01();
            AddLockedRegionUnlockBeacon(new Vector3(centerX - offsetX, 0.12f, centerZ - offsetZ), amount);
            AddLockedRegionUnlockBeacon(new Vector3(centerX + offsetX, 0.12f, centerZ + offsetZ), amount);
        }

        private void AddLockedRegionUnlockBeacon(Vector3 center, float progressAmount)
        {
            var coreHeight = Mathf.Lerp(0.22f, 0.42f, Mathf.Clamp01(progressAmount));
            var glowSize = Mathf.Lerp(cellSize * 0.26f, cellSize * 0.42f, Mathf.Clamp01(progressAmount));
            AddLooseCube(guideObjects, "LockedRegionUnlockBeaconBase", lockedAreaMaterial, center, new Vector3(cellSize * 0.34f, 0.07f, cellSize * 0.34f));
            AddLooseCube(guideObjects, "LockedRegionUnlockBeaconCore", roadLineMaterial, center + new Vector3(0f, coreHeight * 0.5f + 0.03f, 0f), new Vector3(cellSize * 0.16f, coreHeight, cellSize * 0.16f));
            AddLooseCube(guideObjects, "LockedRegionUnlockBeaconGlow", lockedAreaMaterial, center + new Vector3(0f, coreHeight + 0.08f, 0f), new Vector3(glowSize, 0.05f, glowSize));
            AddLockedRegionBeaconGroundSignal(center, progressAmount, glowSize);
        }

        private void AddLockedRegionBeaconGroundSignal(Vector3 center, float progressAmount, float glowSize)
        {
            // LOW_POLY_UNLOCK_BEACON_SIGNAL makes the locked district callout visible from the base map.
            var material = progressAmount >= 0.5f ? windowMaterial : roadLineMaterial;
            var radius = Mathf.Lerp(cellSize * 0.42f, cellSize * 0.62f, Mathf.Clamp01(progressAmount));
            var span = Mathf.Max(cellSize * 0.18f, glowSize * 0.62f);
            AddLooseCube(guideObjects, "LockedRegionBeaconSignalNorth", material, center + new Vector3(0f, 0.02f, radius), new Vector3(span, 0.018f, cellSize * 0.04f));
            AddLooseCube(guideObjects, "LockedRegionBeaconSignalSouth", material, center + new Vector3(0f, 0.02f, -radius), new Vector3(span, 0.018f, cellSize * 0.04f));
            AddLooseCube(guideObjects, "LockedRegionBeaconSignalEast", material, center + new Vector3(radius, 0.024f, 0f), new Vector3(cellSize * 0.04f, 0.018f, span));
            AddLooseCube(guideObjects, "LockedRegionBeaconSignalWest", material, center + new Vector3(-radius, 0.024f, 0f), new Vector3(cellSize * 0.04f, 0.018f, span));
            AddLooseCube(guideObjects, "LockedRegionBeaconSignalSpark", serviceNeedMaterial, center + new Vector3(cellSize * 0.24f, 0.09f, -cellSize * 0.24f), new Vector3(cellSize * 0.09f, 0.052f, cellSize * 0.09f));
        }

        private void AddLockedRegionBoundaryInfoLayer(int startX, int startY, int endX, int endY)
        {
            // CITY_SKYLINES_LOCKED_BOUNDARY_INFO_LAYER makes the unopened district read like a planned overlay.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var spanX = Mathf.Max(cellSize * 2f, (endX - startX + 1) * cellSize * 0.72f);
            var spanZ = Mathf.Max(cellSize * 2f, (endY - startY + 1) * cellSize * 0.72f);
            var progress = LockedRegionObjectiveProgress01();

            AddLockedRegionBoundaryTape(new Vector3(centerX, 0.12f, (startY - 0.1f) * cellSize), true, spanX, progress);
            AddLockedRegionBoundaryTape(new Vector3(centerX, 0.12f, (endY + 1.1f) * cellSize), true, spanX, progress);
            AddLockedRegionBoundaryTape(new Vector3((startX - 0.1f) * cellSize, 0.12f, centerZ), false, spanZ, progress);
            AddLockedRegionBoundaryTape(new Vector3((endX + 1.1f) * cellSize, 0.12f, centerZ), false, spanZ, progress);
            AddLockedRegionPermitBoard(new Vector3(centerX - spanX * 0.22f, 0.18f, (startY - 0.42f) * cellSize), true, progress);
            AddLockedRegionPermitBoard(new Vector3((startX - 0.42f) * cellSize, 0.18f, centerZ + spanZ * 0.22f), false, progress);
        }

        private void AddLockedRegionBoundaryTape(Vector3 center, bool horizontal, float length, float progress)
        {
            var railScale = horizontal
                ? new Vector3(length, 0.028f, cellSize * 0.055f)
                : new Vector3(cellSize * 0.055f, 0.028f, length);
            AddLooseCube(guideObjects, "LockedRegionBoundaryTapeRail", roadLineMaterial, center, railScale);

            var tickCount = Mathf.Clamp(Mathf.RoundToInt(length / Mathf.Max(0.1f, cellSize * 0.9f)), 2, 7);
            var filled = Mathf.Clamp(Mathf.CeilToInt(tickCount * Mathf.Clamp01(progress)), 0, tickCount);
            var along = horizontal ? Vector3.right : Vector3.forward;
            var tickScale = horizontal
                ? new Vector3(cellSize * 0.16f, 0.035f, cellSize * 0.07f)
                : new Vector3(cellSize * 0.07f, 0.035f, cellSize * 0.16f);
            for (var i = 0; i < tickCount; i += 1)
            {
                var t = tickCount <= 1 ? 0f : (i / (float)(tickCount - 1) - 0.5f);
                var material = i < filled ? windowMaterial : lockedAreaMaterial;
                AddLooseCube(guideObjects, "LockedRegionBoundaryTapeTick", material, center + along * (t * length * 0.84f) + new Vector3(0f, 0.035f, 0f), tickScale);
            }
        }

        private void AddLockedRegionPermitBoard(Vector3 center, bool horizontal, float progress)
        {
            var boardScale = horizontal
                ? new Vector3(cellSize * 0.62f, 0.11f, cellSize * 0.06f)
                : new Vector3(cellSize * 0.06f, 0.11f, cellSize * 0.62f);
            var lineScale = horizontal
                ? new Vector3(cellSize * 0.4f, 0.032f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.032f, cellSize * 0.4f);
            var postOffset = horizontal ? Vector3.right * cellSize * 0.34f : Vector3.forward * cellSize * 0.34f;
            AddLooseCube(guideObjects, "LockedRegionPermitBoardPost", lockedAreaMaterial, center - postOffset + new Vector3(0f, 0.03f, 0f), new Vector3(0.045f, 0.26f, 0.045f));
            AddLooseCube(guideObjects, "LockedRegionPermitBoardPost", lockedAreaMaterial, center + postOffset + new Vector3(0f, 0.03f, 0f), new Vector3(0.045f, 0.26f, 0.045f));
            AddLooseCube(guideObjects, "LockedRegionPermitBoard", serviceNeedMaterial, center + new Vector3(0f, 0.2f, 0f), boardScale);
            AddLooseCube(guideObjects, "LockedRegionPermitBoardLine", roadLineMaterial, center + new Vector3(0f, 0.235f, 0f), lineScale);
            AddLooseCube(guideObjects, "LockedRegionPermitBoardProgress", progress >= 0.66f ? windowMaterial : lockedAreaMaterial, center + new Vector3(0f, 0.285f, 0f), lineScale * Mathf.Lerp(0.45f, 0.9f, Mathf.Clamp01(progress)));
        }

        private void AddLockedRegionSurveyRulers(int startX, int startY, int endX, int endY)
        {
            // CITY_SKYLINES_LOCKED_SURVEY_RULERS sharpen the unopened area as a measured construction boundary.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var spanX = Mathf.Max(cellSize * 2f, (endX - startX + 1) * cellSize * 0.66f);
            var spanZ = Mathf.Max(cellSize * 2f, (endY - startY + 1) * cellSize * 0.66f);
            var progress = LockedRegionObjectiveProgress01();

            AddLockedRegionSurveyRuler(new Vector3(centerX, 0.185f, (startY + 0.28f) * cellSize), true, spanX, progress);
            AddLockedRegionSurveyRuler(new Vector3((startX + 0.28f) * cellSize, 0.187f, centerZ), false, spanZ, progress);
            AddLockedRegionSurveyLaser(new Vector3(centerX + spanX * 0.22f, 0.18f, centerZ - spanZ * 0.22f), progress);
        }

        private void AddLockedRegionSurveyRuler(Vector3 center, bool horizontal, float length, float progress)
        {
            var railScale = horizontal
                ? new Vector3(length, 0.026f, cellSize * 0.042f)
                : new Vector3(cellSize * 0.042f, 0.026f, length);
            AddLooseCube(guideObjects, "LockedRegionSurveyRulerRail", roadLineMaterial, center, railScale);

            var tickCount = Mathf.Clamp(Mathf.RoundToInt(length / Mathf.Max(0.1f, cellSize * 0.72f)), 3, 8);
            var along = horizontal ? Vector3.right : Vector3.forward;
            var tickScale = horizontal
                ? new Vector3(cellSize * 0.038f, 0.055f, cellSize * 0.09f)
                : new Vector3(cellSize * 0.09f, 0.055f, cellSize * 0.038f);
            var filled = Mathf.Clamp(Mathf.RoundToInt(tickCount * Mathf.Clamp01(progress)), 0, tickCount);
            for (var i = 0; i < tickCount; i += 1)
            {
                var t = tickCount <= 1 ? 0f : i / (float)(tickCount - 1) - 0.5f;
                var material = i < filled ? windowMaterial : lockedAreaMaterial;
                AddLooseCube(guideObjects, "LockedRegionSurveyRulerTick", material, center + along * (t * length * 0.88f) + new Vector3(0f, 0.04f, 0f), tickScale);
            }
        }

        private void AddLockedRegionSurveyLaser(Vector3 center, float progress)
        {
            var activeMaterial = progress >= 0.5f ? windowMaterial : lockedAreaMaterial;
            AddLooseCube(guideObjects, "LockedRegionSurveyLaserTripod", serviceMaterial, center + new Vector3(0f, 0.13f, 0f), new Vector3(0.07f, 0.28f, 0.07f));
            AddLooseCube(guideObjects, "LockedRegionSurveyLaserHead", activeMaterial, center + new Vector3(0f, 0.31f, 0f), new Vector3(0.18f, 0.07f, 0.12f));
            AddLooseCubeRotated(guideObjects, "LockedRegionSurveyLaserSweep", roadLineMaterial, center + new Vector3(cellSize * 0.18f, 0.31f, -cellSize * 0.08f), new Vector3(cellSize * 0.5f, 0.018f, 0.03f), -32f);
        }

        private void AddLockedRegionFreshSurveyMarks(int startX, int startY, int endX, int endY)
        {
            // LOW_POLY_LOCKED_FRESH_SURVEY_MARKS reinforces the dashed future district boundary.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var spanX = Mathf.Max(cellSize * 1.8f, (endX - startX + 1) * cellSize * 0.44f);
            var spanZ = Mathf.Max(cellSize * 1.8f, (endY - startY + 1) * cellSize * 0.44f);
            AddLockedRegionSurveyTarget(new Vector3(centerX - spanX * 0.5f, 0.145f, centerZ - spanZ * 0.5f), true);
            AddLockedRegionSurveyTarget(new Vector3(centerX + spanX * 0.5f, 0.145f, centerZ + spanZ * 0.5f), true);
            AddLockedRegionSurveyTarget(new Vector3(centerX - spanX * 0.18f, 0.145f, centerZ + spanZ * 0.36f), false);
            AddLockedRegionSurveyTarget(new Vector3(centerX + spanX * 0.36f, 0.145f, centerZ - spanZ * 0.18f), false);

            AddLockedRegionSurveyDashRun(new Vector3(centerX, 0.132f, centerZ - spanZ * 0.42f), true, spanX * 0.76f);
            AddLockedRegionSurveyDashRun(new Vector3(centerX - spanX * 0.42f, 0.134f, centerZ), false, spanZ * 0.76f);
        }

        private void AddLockedRegionEdgeHintMarkers(int startX, int startY, int endX, int endY)
        {
            // CITY_SKYLINES_UNLOCK_EDGE_HINTS make the unopened boundary read as a reachable next district.
            var centerX = (startX + endX + 1) * 0.5f * cellSize;
            var centerZ = (startY + endY + 1) * 0.5f * cellSize;
            var progress = LockedRegionObjectiveProgress01();
            AddLockedRegionEdgeHintMarker(new Vector3(centerX, 0.19f, (startY - 0.58f) * cellSize), true, 1f, progress);
            AddLockedRegionEdgeHintMarker(new Vector3(centerX, 0.19f, (endY + 1.58f) * cellSize), true, -1f, progress);
            AddLockedRegionEdgeHintMarker(new Vector3((startX - 0.58f) * cellSize, 0.19f, centerZ), false, 1f, progress);
            AddLockedRegionEdgeHintMarker(new Vector3((endX + 1.58f) * cellSize, 0.19f, centerZ), false, -1f, progress);
        }

        private void AddLockedRegionEdgeHintMarker(Vector3 center, bool horizontalEdge, float inwardSign, float progress)
        {
            var normal = horizontalEdge ? Vector3.forward * inwardSign : Vector3.right * inwardSign;
            var along = horizontalEdge ? Vector3.right : Vector3.forward;
            var boardScale = horizontalEdge
                ? new Vector3(cellSize * 0.64f, 0.09f, cellSize * 0.06f)
                : new Vector3(cellSize * 0.06f, 0.09f, cellSize * 0.64f);
            var stemScale = horizontalEdge
                ? new Vector3(cellSize * 0.045f, 0.026f, cellSize * 0.24f)
                : new Vector3(cellSize * 0.24f, 0.026f, cellSize * 0.045f);
            var capScale = horizontalEdge
                ? new Vector3(cellSize * 0.16f, 0.03f, cellSize * 0.06f)
                : new Vector3(cellSize * 0.06f, 0.03f, cellSize * 0.16f);
            AddLooseCube(guideObjects, "LockedRegionEdgeHintPad", grassGridMaterial, center - normal * cellSize * 0.04f + new Vector3(0f, -0.11f, 0f), boardScale * 1.12f);
            AddLooseCube(guideObjects, "LockedRegionEdgeHintBoard", lockedAreaMaterial, center, boardScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeHintArrowStem", roadLineMaterial, center + normal * cellSize * 0.18f + new Vector3(0f, 0.075f, 0f), stemScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeHintArrowCap", roadLineMaterial, center + normal * cellSize * 0.31f + along * cellSize * 0.055f + new Vector3(0f, 0.08f, 0f), capScale);
            AddLooseCube(guideObjects, "LockedRegionEdgeHintArrowCap", roadLineMaterial, center + normal * cellSize * 0.31f - along * cellSize * 0.055f + new Vector3(0f, 0.08f, 0f), capScale);

            var filled = Mathf.Clamp(Mathf.CeilToInt(progress * 3f), 0, 3);
            var pipScale = new Vector3(cellSize * 0.07f, 0.035f, cellSize * 0.07f);
            for (var i = 0; i < 3; i += 1)
            {
                var material = i < filled ? windowMaterial : roadLineMaterial;
                AddLooseCube(guideObjects, "LockedRegionEdgeHintProgressPip", material, center - normal * cellSize * 0.14f + along * ((i - 1) * cellSize * 0.13f) + new Vector3(0f, 0.085f, 0f), pipScale);
            }
        }

        private void AddLockedRegionSurveyTarget(Vector3 center, bool bright)
        {
            var targetMaterial = bright ? roadLineMaterial : lockedAreaMaterial;
            AddLooseCube(guideObjects, "LockedRegionSurveyTargetPad", grassGridMaterial, center, new Vector3(cellSize * 0.28f, 0.028f, cellSize * 0.28f));
            AddLooseCube(guideObjects, "LockedRegionSurveyTargetCross", targetMaterial, center + new Vector3(0f, 0.035f, 0f), new Vector3(cellSize * 0.24f, 0.022f, cellSize * 0.045f));
            AddLooseCube(guideObjects, "LockedRegionSurveyTargetCross", targetMaterial, center + new Vector3(0f, 0.037f, 0f), new Vector3(cellSize * 0.045f, 0.022f, cellSize * 0.24f));
            AddLooseCube(guideObjects, "LockedRegionSurveyTargetFlagPost", serviceMaterial, center + new Vector3(cellSize * 0.16f, 0.13f, -cellSize * 0.16f), new Vector3(0.035f, 0.24f, 0.035f));
            AddLooseCube(guideObjects, "LockedRegionSurveyTargetFlag", bright ? windowMaterial : serviceNeedMaterial, center + new Vector3(cellSize * 0.23f, 0.24f, -cellSize * 0.16f), new Vector3(cellSize * 0.14f, 0.055f, 0.032f));
        }

        private void AddLockedRegionSurveyDashRun(Vector3 center, bool horizontal, float length)
        {
            var count = Mathf.Clamp(Mathf.RoundToInt(length / Mathf.Max(0.1f, cellSize * 0.46f)), 3, 9);
            var along = horizontal ? Vector3.right : Vector3.forward;
            var dashScale = horizontal
                ? new Vector3(cellSize * 0.2f, 0.022f, cellSize * 0.035f)
                : new Vector3(cellSize * 0.035f, 0.022f, cellSize * 0.2f);
            for (var i = 0; i < count; i += 1)
            {
                var t = count <= 1 ? 0f : i / (float)(count - 1) - 0.5f;
                var material = i % 2 == 0 ? roadLineMaterial : lockedAreaMaterial;
                AddLooseCube(guideObjects, "LockedRegionFreshSurveyDash", material, center + along * (t * length), dashScale);
            }
        }

        private void RebuildPlanningSignals()
        {
            // CITY_SKYLINES_STYLE_DIAGNOSTICS keeps layer feedback visible without changing tool counts.
            ClearObjects(planningSignalObjects);
            if (controller == null || controller.Grid == null)
            {
                return;
            }

            var mode = controller.OverlayMode;
            if (mode == OverlayMode.Normal)
            {
                RebuildCityIssueBadges();
                RebuildBuildingUpgradeSignals();
                RebuildNormalTrafficRibbons();
                RebuildZoneOpportunitySignals(false);
                RebuildObjectiveFocusSignal();
            }

            if (mode == OverlayMode.Zoning)
            {
                RebuildZoneOpportunitySignals(true);
            }

            RebuildHighLandValueSignals(mode);
            RebuildTransitNodeSignals(mode);
            RebuildParkingPressureSignals(mode);
            RebuildStormwaterRiskSignals(mode);
            RebuildLayerGroundMarkers(mode);
            RebuildOverlayLegibilityCues(mode);
            RebuildInformationViewRoadFurniture(mode);

            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking)
            {
                RebuildRoadPressureSignals();
            }

            if (mode == OverlayMode.Transit)
            {
                RebuildTransitRouteBands();
            }

            if (mode == OverlayMode.Logistics)
            {
                RebuildLogisticsRouteBands();
            }

            if (mode == OverlayMode.Services
                || mode == OverlayMode.Transit
                || mode == OverlayMode.Logistics
                || mode == OverlayMode.Waste
                || mode == OverlayMode.Communications
                || mode == OverlayMode.Parking
                || mode == OverlayMode.RoadSafety
                || mode == OverlayMode.Pollution
                || mode == OverlayMode.LandValue
                || mode == OverlayMode.Utilities
                || mode == OverlayMode.Stormwater)
            {
                RebuildCoverageProviderAnchors(mode);
                RebuildCoverageNeedSignals(mode);
            }
        }

        private void RebuildInformationViewRoadFurniture(OverlayMode mode)
        {
            // CITY_SKYLINES_INFORMATION_ROAD_FURNITURE adds small lane signs, stop hints, and coverage rings to active info layers.
            if (!InformationViewRoadFurnitureMode(mode))
            {
                return;
            }

            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            var added = 0;
            for (var i = 0; i < roads.Count && added < 42; i += 1)
            {
                var road = roads[i];
                var tile = controller.GetTile(road.Pos.X, road.Pos.Y);
                if (tile == null)
                {
                    continue;
                }

                var score = InformationViewRoadFurnitureScore(road, tile, mode);
                if (score < InformationViewRoadFurnitureThreshold(mode))
                {
                    continue;
                }

                AddInformationViewRoadFurniture(road, roads, mode, score);
                added += 1;
            }
        }

        private static bool InformationViewRoadFurnitureMode(OverlayMode mode)
        {
            return mode == OverlayMode.Traffic
                || mode == OverlayMode.RoadSafety
                || mode == OverlayMode.Parking
                || mode == OverlayMode.Transit
                || mode == OverlayMode.Logistics
                || mode == OverlayMode.Services;
        }

        private int InformationViewRoadFurnitureScore(RoadNode road, TileData tile, OverlayMode mode)
        {
            var load = TrafficLoadPercent(road);
            var arterial = road.Tier == RoadTier.Arterial ? 18 : 0;
            var junction = road.NeighborCount >= 3 ? 16 : road.NeighborCount * 3;
            if (mode == OverlayMode.Transit)
            {
                var wait = controller.Metrics != null ? controller.Metrics.TransitWaitPressure / 6 : 0;
                return tile.TransitAccess + load / 4 + arterial + junction / 2 + wait;
            }

            if (mode == OverlayMode.Logistics)
            {
                return tile.LogisticsAccess + load / 3 + arterial + (IsFreightRouteRoad(road.Pos) ? 22 : 0);
            }

            if (mode == OverlayMode.Services)
            {
                return ServiceAccessValue(tile) + load / 5 + arterial / 2 + junction / 2;
            }

            return load + arterial + junction;
        }

        private static int InformationViewRoadFurnitureThreshold(OverlayMode mode)
        {
            if (mode == OverlayMode.Transit) return 42;
            if (mode == OverlayMode.Logistics) return 46;
            if (mode == OverlayMode.Services) return 44;
            return 58;
        }

        private void AddInformationViewRoadFurniture(RoadNode road, IReadOnlyList<RoadNode> roads, OverlayMode mode, int score)
        {
            var hasLeft = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y);
            var hasRight = HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
            var hasDown = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1);
            var hasUp = HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
            var hasHorizontal = hasLeft || hasRight;
            var hasVertical = hasDown || hasUp;
            var horizontal = hasHorizontal || !hasVertical;
            var roadTop = road.Tier == RoadTier.Arterial ? roadHeight * 1.35f : roadHeight;
            var material = InformationViewModeMaterial(mode, score);
            var direction = horizontal ? (hasRight || !hasLeft ? 1f : -1f) : (hasUp || !hasDown ? 1f : -1f);
            var laneOffset = (((DecorationHash(road.Pos.X, road.Pos.Y) & 1) == 0 ? -1f : 1f) * cellSize * 0.21f);

            AddInformationViewLaneArrow(road.Pos, horizontal, direction, laneOffset, roadTop, material);

            if (mode == OverlayMode.Transit)
            {
                AddInformationTransitStopHint(road.Pos, horizontal, roadTop, score);
                return;
            }

            if (mode == OverlayMode.Logistics)
            {
                AddInformationFreightNodeHint(road.Pos, horizontal, roadTop, score);
                return;
            }

            if (mode == OverlayMode.Services)
            {
                AddInformationServiceCoverageRing(road.Pos, roadTop, score);
                return;
            }

            AddInformationTrafficSignalDots(road.Pos, horizontal, roadTop, score, RoadConnectionCount(hasLeft, hasRight, hasDown, hasUp) >= 3);
        }

        private Material InformationViewModeMaterial(OverlayMode mode, int score)
        {
            if (mode == OverlayMode.Transit) return windowMaterial;
            if (mode == OverlayMode.Logistics) return industrialMaterial;
            if (mode == OverlayMode.Services) return serviceMaterial;
            return score >= 86 ? trafficPulseMaterial : serviceNeedMaterial;
        }

        private void AddInformationViewLaneArrow(GridPos pos, bool horizontal, float direction, float laneOffset, float roadTop, Material material)
        {
            var stemCenter = CellCenter(pos, roadTop + 0.19f) + (horizontal ? new Vector3(0f, 0f, laneOffset) : new Vector3(laneOffset, 0f, 0f));
            var headCenter = stemCenter + (horizontal ? new Vector3(direction * cellSize * 0.2f, 0f, 0f) : new Vector3(0f, 0f, direction * cellSize * 0.2f));
            var stemScale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.026f, 0.046f)
                : new Vector3(0.046f, 0.026f, cellSize * 0.3f);
            AddLooseCube(planningSignalObjects, "InfoViewDirectionArrowStem", material, stemCenter, stemScale);

            var headScale = new Vector3(cellSize * 0.16f, 0.024f, 0.04f);
            if (horizontal)
            {
                AddLooseCubeRotated(planningSignalObjects, "InfoViewDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(-direction * cellSize * 0.04f, 0f, cellSize * 0.045f), headScale, direction > 0f ? 32f : 148f);
                AddLooseCubeRotated(planningSignalObjects, "InfoViewDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(-direction * cellSize * 0.04f, 0f, -cellSize * 0.045f), headScale, direction > 0f ? -32f : -148f);
                return;
            }

            AddLooseCubeRotated(planningSignalObjects, "InfoViewDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(cellSize * 0.045f, 0f, -direction * cellSize * 0.04f), headScale, direction > 0f ? 58f : -58f);
            AddLooseCubeRotated(planningSignalObjects, "InfoViewDirectionArrowHead", roadLineMaterial, headCenter + new Vector3(-cellSize * 0.045f, 0f, -direction * cellSize * 0.04f), headScale, direction > 0f ? 122f : -122f);
        }

        private void AddInformationTrafficSignalDots(GridPos pos, bool horizontal, float roadTop, int score, bool intersection)
        {
            var side = horizontal ? Vector3.forward : Vector3.right;
            var center = CellCenter(pos, roadTop + 0.205f) + side * cellSize * 0.34f;
            var stemScale = new Vector3(0.05f, 0.19f, 0.05f);
            AddLooseCube(planningSignalObjects, "InfoViewSignalPost", roadMaterial, center + new Vector3(0f, 0.095f, 0f), stemScale);
            AddLooseCube(planningSignalObjects, "InfoViewSignalHead", intersection ? trafficPulseMaterial : serviceNeedMaterial, center + new Vector3(0f, 0.23f, 0f), new Vector3(0.13f, 0.2f, 0.075f));
            AddLooseCube(planningSignalObjects, "InfoViewSignalRedDot", trafficPulseMaterial, center + new Vector3(0f, 0.285f, 0f), new Vector3(0.055f, 0.035f, 0.032f));
            AddLooseCube(planningSignalObjects, "InfoViewSignalAmberDot", serviceNeedMaterial, center + new Vector3(0f, 0.235f, 0f), new Vector3(0.055f, 0.035f, 0.032f));
            AddLooseCube(planningSignalObjects, "InfoViewSignalFlowDot", score >= 86 ? roadLineMaterial : windowMaterial, center + new Vector3(0f, 0.185f, 0f), new Vector3(0.055f, 0.035f, 0.032f));
        }

        private void AddInformationTransitStopHint(GridPos pos, bool horizontal, float roadTop, int score)
        {
            var side = horizontal ? Vector3.forward : Vector3.right;
            var along = horizontal ? Vector3.right : Vector3.forward;
            var center = CellCenter(pos, roadTop + 0.2f) - side * cellSize * 0.33f;
            AddLooseCube(planningSignalObjects, "InfoViewTransitStopPad", windowMaterial, center, horizontal ? new Vector3(cellSize * 0.38f, 0.024f, 0.055f) : new Vector3(0.055f, 0.024f, cellSize * 0.38f));
            AddLooseCube(planningSignalObjects, "InfoViewTransitStopPost", commercialMaterial, center + new Vector3(0f, 0.15f, 0f), new Vector3(0.05f, 0.27f, 0.05f));
            AddLooseCube(planningSignalObjects, "InfoViewTransitStopFlag", roadLineMaterial, center + along * cellSize * 0.08f + new Vector3(0f, 0.3f, 0f), horizontal ? new Vector3(0.2f, 0.058f, 0.04f) : new Vector3(0.04f, 0.058f, 0.2f));
            if (score >= 68)
            {
                AddLooseCube(planningSignalObjects, "InfoViewTransitStopQueuePip", serviceNeedMaterial, center - along * cellSize * 0.16f + new Vector3(0f, 0.078f, 0f), new Vector3(0.055f, 0.11f, 0.055f));
            }
        }

        private void AddInformationFreightNodeHint(GridPos pos, bool horizontal, float roadTop, int score)
        {
            var side = horizontal ? Vector3.forward : Vector3.right;
            var along = horizontal ? Vector3.right : Vector3.forward;
            var center = CellCenter(pos, roadTop + 0.19f) + side * cellSize * 0.34f;
            AddLooseCube(planningSignalObjects, "InfoViewFreightNodeDock", industrialMaterial, center, horizontal ? new Vector3(cellSize * 0.34f, 0.026f, 0.08f) : new Vector3(0.08f, 0.026f, cellSize * 0.34f));
            AddLooseCube(planningSignalObjects, "InfoViewFreightNodeCrate", serviceNeedMaterial, center - along * cellSize * 0.1f + new Vector3(0f, 0.075f, 0f), new Vector3(0.105f, 0.095f, 0.105f));
            AddLooseCube(planningSignalObjects, "InfoViewFreightNodeCrate", score >= 72 ? trafficPulseMaterial : industrialMaterial, center + along * cellSize * 0.1f + new Vector3(0f, 0.092f, 0f), new Vector3(0.12f, 0.11f, 0.11f));
            AddLooseCube(planningSignalObjects, "InfoViewFreightNodeLabel", roadLineMaterial, center + new Vector3(0f, 0.16f, 0f), horizontal ? new Vector3(0.24f, 0.032f, 0.042f) : new Vector3(0.042f, 0.032f, 0.24f));
        }

        private void AddInformationServiceCoverageRing(GridPos pos, float roadTop, int score)
        {
            var center = CellCenter(pos, roadTop + 0.18f);
            var radius = Mathf.Lerp(cellSize * 0.34f, cellSize * 0.48f, Mathf.Clamp01(score / 100f));
            var material = score >= 70 ? serviceMaterial : serviceNeedMaterial;
            AddLooseCube(planningSignalObjects, "InfoViewServiceCoverageRingNorth", material, center + new Vector3(0f, 0f, radius), new Vector3(cellSize * 0.24f, 0.02f, 0.04f));
            AddLooseCube(planningSignalObjects, "InfoViewServiceCoverageRingSouth", material, center + new Vector3(0f, 0f, -radius), new Vector3(cellSize * 0.24f, 0.02f, 0.04f));
            AddLooseCube(planningSignalObjects, "InfoViewServiceCoverageRingEast", material, center + new Vector3(radius, 0f, 0f), new Vector3(0.04f, 0.02f, cellSize * 0.24f));
            AddLooseCube(planningSignalObjects, "InfoViewServiceCoverageRingWest", material, center + new Vector3(-radius, 0f, 0f), new Vector3(0.04f, 0.02f, cellSize * 0.24f));
            AddLooseCube(planningSignalObjects, "InfoViewServiceCoveragePlus", roadLineMaterial, center + new Vector3(0f, 0.045f, 0f), new Vector3(0.2f, 0.03f, 0.055f));
            AddLooseCube(planningSignalObjects, "InfoViewServiceCoveragePlus", roadLineMaterial, center + new Vector3(0f, 0.047f, 0f), new Vector3(0.055f, 0.03f, 0.2f));
        }

        private void RebuildHighLandValueSignals(OverlayMode mode)
        {
            // CITY_SKYLINES_HIGH_VALUE_GLINTS surface premium blocks without changing land-value math.
            if (mode != OverlayMode.Normal && mode != OverlayMode.LandValue)
            {
                return;
            }

            var grid = controller.Grid;
            var metrics = controller.Metrics;
            var threshold = HighLandValueSignalThreshold(metrics);
            var signals = new List<GroundMarkerSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (!IsDevelopedMapTile(tile) || !string.IsNullOrEmpty(tile.RoadId) || tile.LandValue < threshold)
                    {
                        continue;
                    }

                    signals.Add(new GroundMarkerSignal
                    {
                        Pos = new GridPos(x, y),
                        Score = tile.LandValue + tile.TransitAccess / 5 + ServiceAccessValue(tile) / 6
                    });
                }
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(mode == OverlayMode.LandValue ? 24 : 8, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddHighLandValueSignal(signals[i].Pos, signals[i].Score, mode == OverlayMode.LandValue);
            }
        }

        private static int HighLandValueSignalThreshold(CityMetrics metrics)
        {
            return metrics != null ? Mathf.Clamp(Mathf.Max(58, metrics.AverageLandValue + 10), 52, 82) : 62;
        }

        private void AddHighLandValueSignal(GridPos pos, int score, bool expanded)
        {
            var tile = controller.GetTile(pos.X, pos.Y);
            if (tile == null)
            {
                return;
            }

            var center = CellCenter(pos, roadHeight + 0.128f);
            var size = Mathf.Lerp(cellSize * 0.24f, cellSize * 0.42f, Mathf.Clamp01(score / 120f));
            var material = tile.Zone == ZoneType.None ? serviceNeedMaterial : MaterialForZone(tile.Zone);
            AddLooseCube(planningSignalObjects, "HighLandValuePlaque", roadLineMaterial, center, new Vector3(size, 0.026f, size * 0.62f));
            AddLooseCube(planningSignalObjects, "HighLandValueCore", material, center + new Vector3(0f, 0.028f, 0f), new Vector3(size * 0.62f, 0.022f, size * 0.22f));
            AddLooseCube(planningSignalObjects, "HighLandValueSpark", windowMaterial, center + new Vector3(size * 0.23f, 0.074f, -size * 0.18f), new Vector3(0.08f, 0.07f, 0.08f));
            AddHighLandValueGroundStencil(center, size, score, expanded);

            if (!expanded)
            {
                return;
            }

            AddLooseCube(planningSignalObjects, "HighLandValueCornerTick", serviceNeedMaterial, center + new Vector3(-size * 0.36f, 0.052f, size * 0.28f), new Vector3(size * 0.38f, 0.026f, 0.038f));
            AddLooseCube(planningSignalObjects, "HighLandValueCornerTick", serviceNeedMaterial, center + new Vector3(-size * 0.36f, 0.054f, size * 0.28f), new Vector3(0.038f, 0.026f, size * 0.28f));
            if (!string.IsNullOrEmpty(tile.BuildingId))
            {
                AddLooseCube(planningSignalObjects, "HighLandValueSkylinePip", windowMaterial, center + new Vector3(0f, 0.13f, size * 0.22f), new Vector3(0.075f, 0.12f, 0.075f));
            }
        }

        private void AddHighLandValueGroundStencil(Vector3 center, float size, int score, bool expanded)
        {
            // CITY_SKYLINES_LAND_VALUE_GROUND_STENCIL makes premium blocks read from top-down camera angles.
            AddLooseCube(planningSignalObjects, "HighLandValueGroundGem", serviceNeedMaterial, center + new Vector3(-size * 0.28f, 0.032f, -size * 0.16f), new Vector3(size * 0.18f, 0.02f, size * 0.18f));
            AddLooseCube(planningSignalObjects, "HighLandValueGroundUnderline", roadLineMaterial, center + new Vector3(-size * 0.02f, 0.034f, size * 0.22f), new Vector3(size * 0.44f, 0.018f, 0.032f));
            if (expanded || score >= 92)
            {
                AddLooseCube(planningSignalObjects, "HighLandValueGroundRiseBar", windowMaterial, center + new Vector3(size * 0.22f, 0.058f, size * 0.08f), new Vector3(0.042f, 0.09f, 0.042f));
                AddLooseCube(planningSignalObjects, "HighLandValueGroundRiseBar", roadLineMaterial, center + new Vector3(size * 0.32f, 0.074f, size * 0.08f), new Vector3(0.042f, 0.12f, 0.042f));
            }
        }

        private void RebuildTransitNodeSignals(OverlayMode mode)
        {
            // CITY_SKYLINES_TRANSIT_NODE_SIGNS make route access visible as map nodes and not only heat color.
            if (mode != OverlayMode.Normal && mode != OverlayMode.Transit)
            {
                return;
            }

            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            var signals = new List<GroundMarkerSignal>();
            for (var i = 0; i < roads.Count; i += 1)
            {
                var road = roads[i];
                var tile = controller.GetTile(road.Pos.X, road.Pos.Y);
                if (tile == null)
                {
                    continue;
                }

                var score = tile.TransitAccess + (road.Tier == RoadTier.Arterial ? 18 : 0) + road.NeighborCount * 5;
                if (controller.Metrics != null)
                {
                    score += controller.Metrics.TransitWaitPressure / 5;
                }

                if (score < (mode == OverlayMode.Transit ? 36 : 54))
                {
                    continue;
                }

                signals.Add(new GroundMarkerSignal
                {
                    Pos = road.Pos,
                    Score = score
                });
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(mode == OverlayMode.Transit ? 28 : 8, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddTransitNodeSignal(signals[i].Pos, signals[i].Score, mode == OverlayMode.Transit);
            }
        }

        private void AddTransitNodeSignal(GridPos pos, int score, bool expanded)
        {
            var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
            var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y) || !vertical;
            var center = CellCenter(pos, roadHeight + 0.166f);
            var routeScale = horizontal
                ? new Vector3(cellSize * 0.5f, 0.022f, 0.045f)
                : new Vector3(0.045f, 0.022f, cellSize * 0.5f);
            AddLooseCube(planningSignalObjects, "TransitNodeRoutePlate", windowMaterial, center, routeScale);
            AddLooseCube(planningSignalObjects, "TransitNodeStopPost", commercialMaterial, center + new Vector3(0f, 0.15f, 0f), new Vector3(0.05f, 0.28f, 0.05f));
            AddLooseCube(planningSignalObjects, "TransitNodeStopCap", roadLineMaterial, center + new Vector3(0f, 0.31f, 0f), new Vector3(0.2f, 0.055f, 0.1f));

            if (expanded || score >= 72)
            {
                var side = horizontal ? Vector3.forward : Vector3.right;
                AddLooseCube(planningSignalObjects, "TransitNodePlatformEdge", roadLineMaterial, center + side * cellSize * 0.18f + new Vector3(0f, 0.032f, 0f), routeScale * 0.72f);
                AddLooseCube(planningSignalObjects, "TransitNodePassengerPip", serviceNeedMaterial, center - side * cellSize * 0.18f + new Vector3(0f, 0.075f, 0f), new Vector3(0.055f, 0.105f, 0.055f));
            }
        }

        private void RebuildParkingPressureSignals(OverlayMode mode)
        {
            // CITY_SKYLINES_PARKING_PRESSURE_BAYS put compact parking stress glyphs on affected blocks.
            if (mode != OverlayMode.Normal && mode != OverlayMode.Parking)
            {
                return;
            }

            var grid = controller.Grid;
            var metrics = controller.Metrics;
            var signals = new List<GroundMarkerSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (!IsDevelopedMapTile(tile) || !string.IsNullOrEmpty(tile.RoadId))
                    {
                        continue;
                    }

                    var score = 100 - tile.ParkingAccess + tile.Traffic / 3;
                    if (metrics != null)
                    {
                        score += metrics.ParkingPressure / 4;
                    }

                    if (score < (mode == OverlayMode.Parking ? 46 : 68))
                    {
                        continue;
                    }

                    signals.Add(new GroundMarkerSignal
                    {
                        Pos = new GridPos(x, y),
                        Score = score
                    });
                }
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(mode == OverlayMode.Parking ? 26 : 8, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddParkingPressureSignal(signals[i].Pos, signals[i].Score, mode == OverlayMode.Parking);
            }
        }

        private void AddParkingPressureSignal(GridPos pos, int score, bool expanded)
        {
            var center = CellCenter(pos, roadHeight + 0.13f);
            var heat = Mathf.Clamp(score, 0, 120);
            var material = heat >= 82 ? trafficPulseMaterial : serviceNeedMaterial;
            AddLooseCube(planningSignalObjects, "ParkingPressureBayPlate", material, center, new Vector3(cellSize * 0.32f, 0.026f, cellSize * 0.24f));
            AddLooseCube(planningSignalObjects, "ParkingPressureCarBody", roadMaterial, center + new Vector3(0f, 0.06f, 0f), new Vector3(cellSize * 0.22f, 0.08f, cellSize * 0.13f));
            AddLooseCube(planningSignalObjects, "ParkingPressureCarWindow", windowMaterial, center + new Vector3(0f, 0.115f, -cellSize * 0.02f), new Vector3(cellSize * 0.12f, 0.035f, cellSize * 0.08f));
            AddParkingPressureGroundStencil(center, heat, expanded);

            if (!expanded && heat < 82)
            {
                return;
            }

            var pipCount = heat >= 94 ? 3 : 2;
            for (var i = 0; i < pipCount; i += 1)
            {
                var offset = (i - (pipCount - 1) * 0.5f) * cellSize * 0.1f;
                AddLooseCube(planningSignalObjects, "ParkingPressureQueuePip", material, center + new Vector3(offset, 0.17f + i * 0.014f, cellSize * 0.21f), new Vector3(0.05f, 0.075f, 0.05f));
            }
        }

        private void AddParkingPressureGroundStencil(Vector3 center, int heat, bool expanded)
        {
            // CITY_SKYLINES_PARKING_GROUND_STENCIL gives parking pressure a clear P-shaped surface mark.
            var material = heat >= 82 ? trafficPulseMaterial : roadLineMaterial;
            AddLooseCube(planningSignalObjects, "ParkingPressurePMarkStem", material, center + new Vector3(-cellSize * 0.145f, 0.034f, -cellSize * 0.18f), new Vector3(0.045f, 0.018f, cellSize * 0.22f));
            AddLooseCube(planningSignalObjects, "ParkingPressurePMarkTop", material, center + new Vector3(-cellSize * 0.06f, 0.036f, -cellSize * 0.27f), new Vector3(cellSize * 0.17f, 0.018f, 0.045f));
            AddLooseCube(planningSignalObjects, "ParkingPressurePMarkMid", material, center + new Vector3(-cellSize * 0.065f, 0.038f, -cellSize * 0.18f), new Vector3(cellSize * 0.14f, 0.018f, 0.04f));
            if (expanded || heat >= 92)
            {
                AddLooseCube(planningSignalObjects, "ParkingPressureOverflowLane", serviceNeedMaterial, center + new Vector3(cellSize * 0.18f, 0.04f, 0f), new Vector3(0.044f, 0.018f, cellSize * 0.42f));
                AddLooseCube(planningSignalObjects, "ParkingPressureOverflowLane", serviceNeedMaterial, center + new Vector3(cellSize * 0.28f, 0.042f, 0f), new Vector3(0.044f, 0.018f, cellSize * 0.34f));
            }
        }

        private void RebuildStormwaterRiskSignals(OverlayMode mode)
        {
            // CITY_SKYLINES_STORMWATER_RISK_GAUGES add rain and flood-risk callouts to vulnerable tiles.
            if (mode != OverlayMode.Normal && mode != OverlayMode.Stormwater)
            {
                return;
            }

            var grid = controller.Grid;
            var metrics = controller.Metrics;
            var signals = new List<GroundMarkerSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (tile == null || tile.Terrain == TerrainType.Water)
                    {
                        continue;
                    }

                    var exposed = IsDevelopedMapTile(tile) || IsShorelineSceneryTile(x, y);
                    if (!exposed)
                    {
                        continue;
                    }

                    var score = 70 - tile.StormwaterAccess + (IsShorelineSceneryTile(x, y) ? 12 : 0);
                    if (metrics != null)
                    {
                        score += metrics.FloodRisk / 3 + Mathf.Max(0, 70 - metrics.StormwaterResilience) / 3;
                    }

                    if (score < (mode == OverlayMode.Stormwater ? 48 : 72))
                    {
                        continue;
                    }

                    signals.Add(new GroundMarkerSignal
                    {
                        Pos = new GridPos(x, y),
                        Score = score
                    });
                }
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(mode == OverlayMode.Stormwater ? 28 : 8, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddStormwaterRiskSignal(signals[i].Pos, signals[i].Score, mode == OverlayMode.Stormwater);
            }
        }

        private void AddStormwaterRiskSignal(GridPos pos, int score, bool expanded)
        {
            var center = CellCenter(pos, roadHeight + 0.12f);
            var material = score >= 86 ? trafficPulseMaterial : windowMaterial;
            var span = Mathf.Lerp(cellSize * 0.28f, cellSize * 0.5f, Mathf.Clamp01(score / 120f));
            AddLooseCube(planningSignalObjects, "StormwaterRiskWetPatch", windowMaterial, center, new Vector3(span, 0.018f, span * 0.52f));
            AddLooseCube(planningSignalObjects, "StormwaterRiskGaugePost", material, center + new Vector3(-span * 0.28f, 0.14f, -span * 0.18f), new Vector3(0.045f, 0.28f, 0.045f));
            AddLooseCube(planningSignalObjects, "StormwaterRiskGaugeTop", roadLineMaterial, center + new Vector3(-span * 0.28f, 0.29f, -span * 0.18f), new Vector3(0.14f, 0.035f, 0.055f));
            AddLooseCube(planningSignalObjects, "StormwaterRiskWaterline", material, center + new Vector3(0f, 0.052f, span * 0.18f), new Vector3(span * 0.62f, 0.018f, 0.035f));
            AddStormwaterRiskGroundStencil(center, span, score, expanded);

            if (expanded || score >= 86)
            {
                AddLooseCube(planningSignalObjects, "StormwaterRiskSandbag", serviceNeedMaterial, center + new Vector3(span * 0.24f, 0.07f, -span * 0.18f), new Vector3(0.13f, 0.07f, 0.09f));
                AddLooseCube(planningSignalObjects, "StormwaterRiskFlowTick", roadLineMaterial, center + new Vector3(span * 0.08f, 0.086f, span * 0.28f), new Vector3(span * 0.28f, 0.022f, 0.035f));
            }
        }

        private void AddStormwaterRiskGroundStencil(Vector3 center, float span, int score, bool expanded)
        {
            // CITY_SKYLINES_STORMWATER_SURFACE_MARKS make runoff direction obvious on the terrain.
            AddLooseCube(planningSignalObjects, "StormwaterRiskCatchmentBasin", utilityMaterial, center + new Vector3(span * 0.22f, 0.028f, span * 0.02f), new Vector3(span * 0.32f, 0.018f, span * 0.18f));
            AddLooseCube(planningSignalObjects, "StormwaterRiskRunoffArrow", roadLineMaterial, center + new Vector3(span * 0.02f, 0.058f, -span * 0.28f), new Vector3(span * 0.34f, 0.018f, 0.032f));
            AddLooseCubeRotated(planningSignalObjects, "StormwaterRiskRunoffArrowHead", roadLineMaterial, center + new Vector3(span * 0.21f, 0.06f, -span * 0.28f), new Vector3(span * 0.16f, 0.018f, 0.03f), 35f);
            AddLooseCubeRotated(planningSignalObjects, "StormwaterRiskRunoffArrowHead", roadLineMaterial, center + new Vector3(span * 0.21f, 0.06f, -span * 0.28f), new Vector3(span * 0.16f, 0.018f, 0.03f), -35f);
            if (expanded || score >= 92)
            {
                AddLooseCube(planningSignalObjects, "StormwaterRiskDepthTick", trafficPulseMaterial, center + new Vector3(-span * 0.34f, 0.06f, span * 0.22f), new Vector3(0.045f, 0.11f, 0.045f));
                AddLooseCube(planningSignalObjects, "StormwaterRiskDepthTick", windowMaterial, center + new Vector3(-span * 0.24f, 0.045f, span * 0.22f), new Vector3(0.045f, 0.08f, 0.045f));
            }
        }

        private void RebuildLayerGroundMarkers(OverlayMode mode)
        {
            // CITY_LAYER_GROUND_MARKERS paint a few layer-specific hotspots directly onto the map.
            if (controller == null || controller.Grid == null)
            {
                return;
            }

            if (mode == OverlayMode.Normal || mode == OverlayMode.Zoning)
            {
                return;
            }

            var metrics = controller.Metrics;
            var grid = controller.Grid;
            var signals = new List<GroundMarkerSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (tile == null || tile.Terrain == TerrainType.Water)
                    {
                        continue;
                    }

                    var score = LayerGroundMarkerScore(tile, mode, metrics);
                    if (score < 38)
                    {
                        continue;
                    }

                    signals.Add(new GroundMarkerSignal
                    {
                        Pos = new GridPos(x, y),
                        Score = score
                    });
                }
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(28, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddLayerGroundMarker(signals[i].Pos, mode, signals[i].Score);
            }
        }

        private void RebuildOverlayLegibilityCues(OverlayMode mode)
        {
            // REFERENCE_IMAGE_OVERLAY_MAP_TRACES keep roads and parcels readable under heatmap layers.
            if (mode == OverlayMode.Normal || controller == null || controller.Grid == null)
            {
                return;
            }

            var grid = controller.Grid;
            var count = 0;
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (tile == null || tile.Terrain == TerrainType.Water)
                    {
                        continue;
                    }

                    var pos = new GridPos(x, y);
                    if (!string.IsNullOrEmpty(tile.RoadId))
                    {
                        AddOverlayRoadTrace(pos, mode);
                        count += 1;
                    }
                    else if (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None)
                    {
                        AddOverlayParcelTrace(pos, mode, tile);
                        count += 1;
                    }

                    if (count >= 96)
                    {
                        return;
                    }
                }
            }
        }

        private void AddOverlayRoadTrace(GridPos pos, OverlayMode mode)
        {
            var center = CellCenter(pos, roadHeight + 0.066f);
            var hasHorizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y);
            var hasVertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
            var material = OverlayLegibilityMaterial(mode);

            if (hasHorizontal || !hasVertical)
            {
                AddLooseCube(planningSignalObjects, "OverlayRoadTrace", material, center + new Vector3(0f, 0f, -cellSize * 0.25f), new Vector3(cellSize * 0.56f, 0.016f, 0.026f));
                AddLooseCube(planningSignalObjects, "OverlayRoadTrace", roadLineMaterial, center + new Vector3(0f, 0.014f, cellSize * 0.25f), new Vector3(cellSize * 0.44f, 0.012f, 0.02f));
            }

            if (hasVertical)
            {
                AddLooseCube(planningSignalObjects, "OverlayRoadTrace", material, center + new Vector3(-cellSize * 0.25f, 0f, 0f), new Vector3(0.026f, 0.016f, cellSize * 0.56f));
                AddLooseCube(planningSignalObjects, "OverlayRoadTrace", roadLineMaterial, center + new Vector3(cellSize * 0.25f, 0.014f, 0f), new Vector3(0.02f, 0.012f, cellSize * 0.44f));
            }
        }

        private void AddOverlayParcelTrace(GridPos pos, OverlayMode mode, TileData tile)
        {
            var center = CellCenter(pos, roadHeight + 0.055f);
            var material = !string.IsNullOrEmpty(tile.BuildingId) ? roadLineMaterial : MaterialForZone(tile.Zone);
            var accent = OverlayLegibilityMaterial(mode);
            var span = cellSize * 0.34f;
            var inset = cellSize * 0.33f;
            AddLooseCube(planningSignalObjects, "OverlayParcelCorner", material, center + new Vector3(-inset, 0f, -inset), new Vector3(span, 0.014f, 0.026f));
            AddLooseCube(planningSignalObjects, "OverlayParcelCorner", material, center + new Vector3(-inset, 0.002f, -inset), new Vector3(0.026f, 0.014f, span));
            AddLooseCube(planningSignalObjects, "OverlayParcelAccent", accent, center + new Vector3(inset * 0.8f, 0.006f, inset * 0.8f), new Vector3(span * 0.55f, 0.014f, 0.026f));
        }

        private Material OverlayLegibilityMaterial(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking || mode == OverlayMode.Pollution)
            {
                return serviceNeedMaterial;
            }

            if (mode == OverlayMode.Transit || mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Communications)
            {
                return windowMaterial;
            }

            return shoreMaterial != null ? shoreMaterial : roadLineMaterial;
        }

        private int LayerGroundMarkerScore(TileData tile, OverlayMode mode, CityMetrics metrics)
        {
            if (mode == OverlayMode.Traffic)
            {
                return Mathf.Max(tile.Traffic, metrics != null ? metrics.RoadBottleneckPressure : 0);
            }

            if (mode == OverlayMode.RoadSafety)
            {
                return Mathf.Max(100 - tile.RoadMaintenanceAccess, metrics != null ? metrics.AccidentRisk : 0);
            }

            if (mode == OverlayMode.Parking)
            {
                return Mathf.Max(100 - tile.ParkingAccess, metrics != null ? metrics.ParkingPressure : 0);
            }

            if (mode == OverlayMode.Pollution)
            {
                return Mathf.Max(tile.Pollution, tile.Noise);
            }

            if (mode == OverlayMode.LandValue)
            {
                return Mathf.Max(0, LandValueSignalThreshold(metrics) - tile.LandValue);
            }

            if (mode == OverlayMode.Services)
            {
                return Mathf.Max(0, 70 - ServiceAccessValue(tile));
            }

            if (mode == OverlayMode.Transit)
            {
                return Mathf.Max(0, 68 - tile.TransitAccess + tile.Traffic / 4);
            }

            if (mode == OverlayMode.Logistics)
            {
                return Mathf.Max(0, 68 - tile.LogisticsAccess + tile.Traffic / 4);
            }

            if (mode == OverlayMode.Waste)
            {
                return Mathf.Max(0, 70 - tile.WasteAccess);
            }

            if (mode == OverlayMode.Communications)
            {
                return Mathf.Max(0, 70 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess));
            }

            if (mode == OverlayMode.Utilities)
            {
                return metrics != null
                    ? Mathf.Max(Mathf.Max(100 - metrics.UtilityReliability, metrics.UtilityUtilization - 10), metrics.WastewaterUtilization - 10)
                    : 0;
            }

            if (mode == OverlayMode.Stormwater)
            {
                return Mathf.Max(0, 70 - tile.StormwaterAccess + (metrics != null ? metrics.FloodRisk / 4 : 0));
            }

            return 0;
        }

        private void AddLayerGroundMarker(GridPos pos, OverlayMode mode, int score)
        {
            var material = LayerGroundMarkerMaterial(mode, score);
            var center = CellCenter(pos, roadHeight + 0.03f);
            var markerSize = Mathf.Lerp(cellSize * 0.28f, cellSize * 0.48f, Mathf.Clamp01(score / 100f));
            var hash = DecorationHash(pos.X, pos.Y);
            var horizontal = (hash & 1) == 0;
            var stripeScale = horizontal
                ? new Vector3(markerSize * 0.9f, 0.018f, 0.045f)
                : new Vector3(0.045f, 0.018f, markerSize * 0.9f);
            AddLooseCube(planningSignalObjects, "LayerGroundMarkerPad", material, center, new Vector3(markerSize, 0.022f, markerSize));
            AddLooseCube(planningSignalObjects, "LayerGroundMarkerStripe", windowMaterial, center + new Vector3(0f, 0.024f, 0f), stripeScale);
            AddLayerGroundMarkerScale(center, mode, score, markerSize, horizontal);

            if (score >= 62)
            {
                AddLooseCube(planningSignalObjects, "LayerGroundMarkerPost", material, center + new Vector3(-markerSize * 0.24f, 0.11f, -markerSize * 0.24f), new Vector3(0.038f, 0.2f, 0.038f));
                AddLooseCube(planningSignalObjects, "LayerGroundMarkerFlag", LayerGroundMarkerAccent(mode), center + new Vector3(-markerSize * 0.16f, 0.22f, -markerSize * 0.24f), new Vector3(0.16f, 0.06f, 0.032f));
            }
        }

        private void AddLayerGroundMarkerScale(Vector3 center, OverlayMode mode, int score, float markerSize, bool horizontal)
        {
            // CITY_SKYLINES_LAYER_HEAT_RULER turns each hotspot into a tiny readable diagnostic gauge.
            var accent = LayerGroundMarkerAccent(mode);
            var railCenter = center + (horizontal ? new Vector3(0f, 0.044f, markerSize * 0.34f) : new Vector3(markerSize * 0.34f, 0.044f, 0f));
            var railScale = horizontal
                ? new Vector3(markerSize * 0.72f, 0.016f, 0.024f)
                : new Vector3(0.024f, 0.016f, markerSize * 0.72f);
            AddLooseCube(planningSignalObjects, "LayerHeatRulerRail", roadLineMaterial, railCenter, railScale);

            var tickCount = score >= 82 ? 4 : (score >= 62 ? 3 : 2);
            var tickStep = markerSize * 0.18f;
            for (var i = 0; i < tickCount; i += 1)
            {
                var offset = (i - (tickCount - 1) * 0.5f) * tickStep;
                var tickCenter = railCenter + (horizontal ? new Vector3(offset, 0.026f, 0f) : new Vector3(0f, 0.026f, offset));
                var tickHeight = 0.04f + i * 0.012f;
                var tickMaterial = i == tickCount - 1 && score >= 72 ? trafficPulseMaterial : accent;
                var tickScale = horizontal
                    ? new Vector3(0.032f, tickHeight, 0.034f)
                    : new Vector3(0.034f, tickHeight, 0.032f);
                AddLooseCube(planningSignalObjects, "LayerHeatRulerTick", tickMaterial, tickCenter, tickScale);
            }

            if (score >= 72)
            {
                var hotScale = horizontal
                    ? new Vector3(markerSize * 0.26f, 0.018f, 0.034f)
                    : new Vector3(0.034f, 0.018f, markerSize * 0.26f);
                AddLooseCube(planningSignalObjects, "LayerHeatRulerHotBand", trafficPulseMaterial, railCenter + new Vector3(0f, 0.052f, 0f), hotScale);
            }
        }

        private Material LayerGroundMarkerMaterial(OverlayMode mode, int score)
        {
            if (score >= 72)
            {
                return trafficPulseMaterial;
            }

            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking || mode == OverlayMode.Pollution)
            {
                return serviceNeedMaterial;
            }

            if (mode == OverlayMode.Services || mode == OverlayMode.LandValue || mode == OverlayMode.Waste)
            {
                return serviceMaterial;
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Transit || mode == OverlayMode.Communications)
            {
                return windowMaterial;
            }

            return roadLineMaterial;
        }

        private Material LayerGroundMarkerAccent(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking || mode == OverlayMode.Pollution)
            {
                return roadLineMaterial;
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Transit || mode == OverlayMode.Communications)
            {
                return utilityMaterial;
            }

            return windowMaterial;
        }

        private void RebuildTransitRouteBands()
        {
            // CITY_SKYLINES_TRANSIT_ROUTE_BANDS turns individual stops into visible route corridors.
            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            var routeCount = 0;
            for (var i = 0; i < roads.Count && routeCount < 64; i += 1)
            {
                var road = roads[i];
                var tile = controller.GetTile(road.Pos.X, road.Pos.Y);
                if (tile == null)
                {
                    continue;
                }

                var transitRoad = road.Tier == RoadTier.Arterial || tile.TransitAccess >= 24;
                if (!transitRoad && tile.Traffic < 45)
                {
                    continue;
                }

                AddTransitRouteBand(road, roads, tile);
                routeCount += 1;
            }
        }

        private void AddTransitRouteBand(RoadNode road, IReadOnlyList<RoadNode> roads, TileData tile)
        {
            var hasHorizontal = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y) || HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
            var hasVertical = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1) || HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
            var vertical = hasVertical && !hasHorizontal;
            var access = Mathf.Clamp(tile.TransitAccess, 0, 100);
            var length = road.Tier == RoadTier.Arterial ? cellSize * 0.74f : cellSize * 0.58f;
            var thickness = access >= 48 ? 0.055f : 0.04f;
            var center = CellCenter(road.Pos, roadHeight + 0.132f) + (vertical ? new Vector3(-cellSize * 0.18f, 0f, 0f) : new Vector3(0f, 0f, -cellSize * 0.18f));
            var routeScale = vertical
                ? new Vector3(thickness, 0.022f, length)
                : new Vector3(length, 0.022f, thickness);
            AddLooseCube(planningSignalObjects, "TransitRouteBand", windowMaterial, center, routeScale);

            var accentScale = vertical
                ? new Vector3(thickness * 0.52f, 0.024f, length * 0.58f)
                : new Vector3(length * 0.58f, 0.024f, thickness * 0.52f);
            AddLooseCube(planningSignalObjects, "TransitRouteCore", roadLineMaterial, center + new Vector3(0f, 0.024f, 0f), accentScale);
            AddTransitRouteFlowTicks(center, vertical, access);

            if ((controller.Metrics != null && controller.Metrics.TransitWaitPressure >= 48) || tile.TransitAccess < 20)
            {
                AddTransitWaitQueue(center, vertical, road.Pos);
            }
        }

        private void AddTransitRouteFlowTicks(Vector3 center, bool vertical, int access)
        {
            var tickCount = access >= 54 ? 3 : 2;
            var along = vertical ? Vector3.forward : Vector3.right;
            for (var i = 0; i < tickCount; i += 1)
            {
                var offset = (i - (tickCount - 1) * 0.5f) * cellSize * 0.18f;
                var tickCenter = center + along * offset + new Vector3(0f, 0.052f, 0f);
                var tickScale = vertical
                    ? new Vector3(0.04f, 0.018f, cellSize * 0.11f)
                    : new Vector3(cellSize * 0.11f, 0.018f, 0.04f);
                AddLooseCube(planningSignalObjects, "TransitRouteFlowTick", commercialMaterial, tickCenter, tickScale);
            }
        }

        private void AddTransitWaitQueue(Vector3 routeCenter, bool vertical, GridPos pos)
        {
            var hash = DecorationHash(pos.X, pos.Y);
            var count = (hash % 2) + 2;
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            for (var i = 0; i < count; i += 1)
            {
                var passenger = routeCenter + along * ((i - 0.5f) * cellSize * 0.09f) + side * cellSize * 0.14f;
                AddLooseCube(planningSignalObjects, "TransitWaitPassengerBody", serviceNeedMaterial, passenger + new Vector3(0f, 0.075f, 0f), new Vector3(0.045f, 0.12f, 0.045f));
                AddLooseCube(planningSignalObjects, "TransitWaitPassengerHead", roofMaterial, passenger + new Vector3(0f, 0.16f, 0f), new Vector3(0.055f, 0.045f, 0.055f));
            }
        }

        private void RebuildLogisticsRouteBands()
        {
            // CITY_SKYLINES_FREIGHT_FLOW_BANDS make logistics overlays read as moving goods corridors.
            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            var routeCount = 0;
            for (var i = 0; i < roads.Count && routeCount < 64; i += 1)
            {
                var road = roads[i];
                var tile = controller.GetTile(road.Pos.X, road.Pos.Y);
                if (tile == null)
                {
                    continue;
                }

                var freightScore = tile.LogisticsAccess + tile.Traffic / 3 + (road.Tier == RoadTier.Arterial ? 18 : 0);
                if (!IsFreightRouteRoad(road.Pos) && freightScore < 34)
                {
                    continue;
                }

                AddLogisticsRouteBand(road, roads, tile, freightScore);
                routeCount += 1;
            }
        }

        private void AddLogisticsRouteBand(RoadNode road, IReadOnlyList<RoadNode> roads, TileData tile, int freightScore)
        {
            var hasHorizontal = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y) || HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
            var hasVertical = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1) || HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
            var vertical = hasVertical && !hasHorizontal;
            var length = road.Tier == RoadTier.Arterial ? cellSize * 0.76f : cellSize * 0.6f;
            var thickness = freightScore >= 64 ? 0.07f : 0.052f;
            var center = CellCenter(road.Pos, roadHeight + 0.146f) + (vertical ? new Vector3(cellSize * 0.2f, 0f, 0f) : new Vector3(0f, 0f, cellSize * 0.2f));
            var routeScale = vertical
                ? new Vector3(thickness, 0.022f, length)
                : new Vector3(length, 0.022f, thickness);
            var material = freightScore >= 74 ? trafficPulseMaterial : industrialMaterial;
            AddLooseCube(planningSignalObjects, "LogisticsRouteBand", material, center, routeScale);

            var coreScale = vertical
                ? new Vector3(thickness * 0.48f, 0.024f, length * 0.64f)
                : new Vector3(length * 0.64f, 0.024f, thickness * 0.48f);
            AddLooseCube(planningSignalObjects, "LogisticsRouteCore", serviceNeedMaterial, center + new Vector3(0f, 0.026f, 0f), coreScale);
            AddLogisticsFlowTicks(center, vertical, freightScore);

            if (tile.LogisticsAccess < 28 || freightScore >= 78)
            {
                AddLogisticsCargoQueue(center, vertical, freightScore);
            }
        }

        private void AddLogisticsFlowTicks(Vector3 center, bool vertical, int freightScore)
        {
            var count = freightScore >= 74 ? 3 : 2;
            var along = vertical ? Vector3.forward : Vector3.right;
            for (var i = 0; i < count; i += 1)
            {
                var offset = (i - (count - 1) * 0.5f) * cellSize * 0.17f;
                var tickCenter = center + along * offset + new Vector3(0f, 0.056f, 0f);
                var tickScale = vertical
                    ? new Vector3(0.045f, 0.02f, cellSize * 0.12f)
                    : new Vector3(cellSize * 0.12f, 0.02f, 0.045f);
                AddLooseCube(planningSignalObjects, "LogisticsFlowTick", roadLineMaterial, tickCenter, tickScale);
            }
        }

        private void AddLogisticsCargoQueue(Vector3 routeCenter, bool vertical, int freightScore)
        {
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var boxCount = freightScore >= 82 ? 3 : 2;
            for (var i = 0; i < boxCount; i += 1)
            {
                var cargo = routeCenter - along * (cellSize * 0.14f) + side * ((i - 0.5f) * cellSize * 0.11f) + new Vector3(0f, 0.08f + i * 0.012f, 0f);
                AddLooseCube(planningSignalObjects, "LogisticsCargoQueueBox", i == boxCount - 1 ? trafficPulseMaterial : serviceNeedMaterial, cargo, new Vector3(0.1f, 0.08f, 0.1f));
                AddLooseCube(planningSignalObjects, "LogisticsCargoQueueLabel", roadLineMaterial, cargo + new Vector3(0f, 0.055f, 0f), new Vector3(0.07f, 0.02f, 0.026f));
            }
        }

        private void RebuildRoadPressureSignals()
        {
            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            for (var i = 0; i < roads.Count; i += 1)
            {
                var road = roads[i];
                var loadPercent = TrafficLoadPercent(road);
                if (loadPercent < 42 && road.NeighborCount < 3)
                {
                    continue;
                }

                var height = 0.18f + Mathf.Clamp(loadPercent, 0, 130) * 0.0042f;
                var width = road.Tier == RoadTier.Arterial ? 0.22f : 0.16f;
                var material = TrafficLoadMaterial(loadPercent);
                AddLooseCube(planningSignalObjects, "TrafficPulseMarker", material, CellCenter(road.Pos, roadHeight + height * 0.5f + 0.08f), new Vector3(width, height, width));
                if (loadPercent >= 96)
                {
                    AddLooseCube(planningSignalObjects, "TrafficOverloadMarkerCap", windowMaterial, CellCenter(road.Pos, roadHeight + height + 0.12f), new Vector3(width * 1.15f, 0.035f, width * 1.15f));
                }

                AddRoadPressureDirectionCue(road, roads, loadPercent, material);
            }
        }

        private void AddRoadPressureDirectionCue(RoadNode road, IReadOnlyList<RoadNode> roads, int loadPercent, Material material)
        {
            // CITY_SKYLINES_ROAD_PRESSURE_DIRECTION_BANDS make traffic overlays read as segment pressure, not only pins.
            var hasHorizontal = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y) || HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
            var hasVertical = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1) || HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
            var vertical = hasVertical && !hasHorizontal;
            var length = road.Tier == RoadTier.Arterial ? 0.74f : 0.58f;
            var thickness = loadPercent >= 92 ? 0.085f : 0.062f;
            var sideOffset = vertical
                ? new Vector3(cellSize * 0.13f, 0f, 0f)
                : new Vector3(0f, 0f, cellSize * 0.13f);
            var center = CellCenter(road.Pos, roadHeight + 0.142f) + sideOffset;
            var scale = vertical
                ? new Vector3(thickness, 0.022f, length)
                : new Vector3(length, 0.022f, thickness);
            AddLooseCube(planningSignalObjects, "RoadPressureDirectionBand", material, center, scale);

            var brightScale = vertical
                ? new Vector3(thickness * 0.42f, 0.024f, length * 0.74f)
                : new Vector3(length * 0.74f, 0.024f, thickness * 0.42f);
            AddLooseCube(planningSignalObjects, "RoadPressureFlowCore", windowMaterial, center + new Vector3(0f, 0.029f, 0f), brightScale);
            AddRoadPressureInfoRibbonBadge(center, vertical, loadPercent, material);
            AddTrafficQueueTicks(center, vertical, loadPercent);
        }

        private void AddRoadPressureInfoRibbonBadge(Vector3 center, bool vertical, int loadPercent, Material material)
        {
            // CITY_SKYLINES_INFO_ROAD_LOAD_BADGE keeps active traffic layers readable at a glance.
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var badgeCenter = center - along * cellSize * 0.28f - side * cellSize * 0.18f + new Vector3(0f, 0.092f, 0f);
            var badgeMaterial = loadPercent >= 90 ? trafficPulseMaterial : material;
            AddLooseCube(planningSignalObjects, "RoadPressureInfoRibbonBadge", badgeMaterial, badgeCenter, new Vector3(0.14f, 0.045f, 0.12f));
            AddLooseCube(planningSignalObjects, "RoadPressureInfoRibbonBadgeLine", roadLineMaterial, badgeCenter + new Vector3(0f, 0.042f, 0f), new Vector3(0.105f, 0.018f, 0.03f));
            AddLooseCube(planningSignalObjects, "RoadPressureInfoRibbonLocator", windowMaterial, center - side * cellSize * 0.18f + new Vector3(0f, 0.054f, 0f), vertical ? new Vector3(0.034f, 0.018f, cellSize * 0.34f) : new Vector3(cellSize * 0.34f, 0.018f, 0.034f));
        }

        private void RebuildNormalTrafficRibbons()
        {
            // NORMAL_VIEW_TRAFFIC_RIBBONS surfaces urgent road bottlenecks without switching overlays.
            var roads = controller.Roads;
            if (roads == null)
            {
                return;
            }

            var added = 0;
            for (var i = 0; i < roads.Count && added < 28; i += 1)
            {
                var road = roads[i];
                var loadPercent = TrafficLoadPercent(road);
                if (loadPercent < 76 && (road.NeighborCount < 3 || loadPercent < 66))
                {
                    continue;
                }

                AddTrafficLoadRibbon(road, roads, loadPercent);
                added += 1;
            }
        }

        private void AddTrafficLoadRibbon(RoadNode road, IReadOnlyList<RoadNode> roads, int loadPercent)
        {
            var hasHorizontal = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y) || HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
            var hasVertical = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1) || HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
            var vertical = hasVertical && !hasHorizontal;
            var thickness = loadPercent >= 92 ? 0.105f : 0.075f;
            var length = road.Tier == RoadTier.Arterial ? 0.72f : 0.56f;
            var scale = vertical
                ? new Vector3(thickness, 0.018f, length)
                : new Vector3(length, 0.018f, thickness);
            var center = CellCenter(road.Pos, roadHeight + 0.115f);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbon", TrafficLoadMaterial(loadPercent), center, scale);
            AddTrafficLoadRibbonEdges(center, vertical, length, thickness, loadPercent);
            AddTrafficLoadRibbonGroundShadow(center, vertical, length, thickness, loadPercent);
            AddTrafficLoadRibbonSeverityBadge(center, vertical, loadPercent);
            AddTrafficLoadRibbonReadoutTag(center, vertical, loadPercent);
            AddTrafficLoadRibbonFlowNotches(center, vertical, loadPercent);
            AddTrafficLoadMeter(center, vertical, loadPercent);
            AddTrafficQueueTicks(center, vertical, loadPercent);
            if ((hasHorizontal && hasVertical) || road.NeighborCount >= 3)
            {
                AddTrafficJunctionLoadNode(center, loadPercent);
            }

            if (loadPercent >= 92)
            {
                var highlightScale = vertical
                    ? new Vector3(thickness * 0.42f, 0.018f, length * 0.92f)
                    : new Vector3(length * 0.92f, 0.018f, thickness * 0.42f);
                AddLooseCube(planningSignalObjects, "NormalTrafficRibbonHotline", windowMaterial, center + new Vector3(0f, 0.028f, 0f), highlightScale);
            }
        }

        private void AddTrafficLoadRibbonGroundShadow(Vector3 center, bool vertical, float length, float thickness, int loadPercent)
        {
            // CITY_SKYLINES_ROAD_LOAD_RIBBON_FOOTPRINT anchors hot segments to the road instead of floating as loose pins.
            var haloLength = Mathf.Min(cellSize * 0.9f, length + cellSize * 0.12f);
            var haloWidth = Mathf.Max(thickness * 1.9f, cellSize * 0.16f);
            var haloScale = vertical
                ? new Vector3(haloWidth, 0.012f, haloLength)
                : new Vector3(haloLength, 0.012f, haloWidth);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonFootprint", roadLineMaterial, center + new Vector3(0f, -0.044f, 0f), haloScale);

            if (loadPercent < 88)
            {
                return;
            }

            var hotScale = vertical
                ? new Vector3(haloWidth * 0.42f, 0.014f, haloLength * 0.82f)
                : new Vector3(haloLength * 0.82f, 0.014f, haloWidth * 0.42f);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonOverloadFootprint", trafficPulseMaterial, center + new Vector3(0f, -0.026f, 0f), hotScale);
        }

        private void AddTrafficLoadRibbonSeverityBadge(Vector3 center, bool vertical, int loadPercent)
        {
            // CITY_SKYLINES_ROAD_LOAD_BADGE gives the ribbon a small readable overload tag.
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var badgeMaterial = loadPercent >= 92 ? trafficPulseMaterial : serviceNeedMaterial;
            var badgeCenter = center + along * cellSize * 0.31f + side * cellSize * 0.24f + new Vector3(0f, 0.088f, 0f);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonLoadBadge", badgeMaterial, badgeCenter, new Vector3(0.16f, 0.05f, 0.13f));
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonLoadBadgeHeader", roadLineMaterial, badgeCenter + new Vector3(0f, 0.046f, 0f), new Vector3(0.12f, 0.022f, 0.034f));

            var pipCount = loadPercent >= 96 ? 3 : 2;
            for (var i = 0; i < pipCount; i += 1)
            {
                var offset = (i - (pipCount - 1) * 0.5f) * 0.05f;
                AddLooseCube(planningSignalObjects, "NormalTrafficRibbonLoadBadgePip", i == pipCount - 1 ? badgeMaterial : windowMaterial, badgeCenter - along * 0.03f + side * offset + new Vector3(0f, 0.084f + i * 0.006f, 0f), new Vector3(0.036f, 0.035f + i * 0.01f, 0.036f));
            }
        }

        private void AddTrafficLoadRibbonReadoutTag(Vector3 center, bool vertical, int loadPercent)
        {
            // CITY_SKYLINES_TRAFFIC_RIBBON_READOUT adds a compact load tag beside normal-view bottlenecks.
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var material = loadPercent >= 92 ? trafficPulseMaterial : serviceNeedMaterial;
            var tagCenter = center - along * cellSize * 0.28f - side * cellSize * 0.27f + new Vector3(0f, 0.108f, 0f);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonReadoutTag", material, tagCenter, new Vector3(0.18f, 0.044f, 0.15f));
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonReadoutTrack", roadLineMaterial, tagCenter + new Vector3(0f, 0.046f, 0f), new Vector3(0.13f, 0.018f, 0.034f));

            var barCount = loadPercent >= 96 ? 3 : 2;
            for (var i = 0; i < barCount; i += 1)
            {
                var barMaterial = i == barCount - 1 && loadPercent >= 92 ? trafficPulseMaterial : windowMaterial;
                AddLooseCube(planningSignalObjects, "NormalTrafficRibbonReadoutBar", barMaterial, tagCenter + side * ((i - 1) * 0.044f) + new Vector3(0f, 0.078f + i * 0.006f, 0f), new Vector3(0.032f, 0.028f + i * 0.012f, 0.032f));
            }

            var flowScale = vertical
                ? new Vector3(0.028f, 0.016f, cellSize * 0.14f)
                : new Vector3(cellSize * 0.14f, 0.016f, 0.028f);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonReadoutFlow", roadLineMaterial, tagCenter + along * cellSize * 0.08f + new Vector3(0f, 0.02f, 0f), flowScale);
        }

        private void AddTrafficLoadRibbonFlowNotches(Vector3 center, bool vertical, int loadPercent)
        {
            // CITY_SKYLINES_ROAD_LOAD_NOTCHES make the ribbon read as moving queued traffic.
            var notchCount = loadPercent >= 96 ? 4 : (loadPercent >= 86 ? 3 : 2);
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var notchScale = vertical
                ? new Vector3(0.04f, 0.018f, 0.095f)
                : new Vector3(0.095f, 0.018f, 0.04f);
            for (var i = 0; i < notchCount; i += 1)
            {
                var offset = (i - (notchCount - 1) * 0.5f) * cellSize * 0.15f;
                var sideShift = ((i & 1) == 0 ? 1f : -1f) * cellSize * 0.07f;
                var material = i == notchCount - 1 && loadPercent >= 92 ? trafficPulseMaterial : roadLineMaterial;
                AddLooseCube(planningSignalObjects, "NormalTrafficRibbonFlowNotch", material, center + along * offset + side * sideShift + new Vector3(0f, 0.052f + i * 0.002f, 0f), notchScale);
            }
        }

        private void AddTrafficLoadRibbonEdges(Vector3 center, bool vertical, float length, float thickness, int loadPercent)
        {
            // CITY_SKYLINES_TRAFFIC_LOAD_EDGES make road load read as a lane-wide information ribbon.
            var material = loadPercent >= 92 ? trafficPulseMaterial : roadLineMaterial;
            var side = vertical ? Vector3.right : Vector3.forward;
            var edgeOffset = side * Mathf.Max(cellSize * 0.105f, thickness * 1.3f);
            var edgeScale = vertical
                ? new Vector3(0.028f, 0.015f, length * 0.94f)
                : new Vector3(length * 0.94f, 0.015f, 0.028f);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonEdge", material, center + edgeOffset + new Vector3(0f, 0.024f, 0f), edgeScale);
            AddLooseCube(planningSignalObjects, "NormalTrafficRibbonEdge", material, center - edgeOffset + new Vector3(0f, 0.024f, 0f), edgeScale);
        }

        private void AddTrafficLoadMeter(Vector3 center, bool vertical, int loadPercent)
        {
            var meterCount = loadPercent >= 96 ? 4 : (loadPercent >= 86 ? 3 : 2);
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var meterMaterial = loadPercent >= 92 ? trafficPulseMaterial : serviceNeedMaterial;
            for (var i = 0; i < meterCount; i += 1)
            {
                var t = i - (meterCount - 1) * 0.5f;
                var meterCenter = center + along * (t * cellSize * 0.13f) - side * cellSize * 0.21f + new Vector3(0f, 0.068f + i * 0.005f, 0f);
                var meterScale = vertical
                    ? new Vector3(0.045f, 0.032f + i * 0.009f, 0.075f)
                    : new Vector3(0.075f, 0.032f + i * 0.009f, 0.045f);
                AddLooseCube(planningSignalObjects, "NormalTrafficLoadMeter", i == meterCount - 1 ? trafficPulseMaterial : meterMaterial, meterCenter, meterScale);
            }
        }

        private void AddTrafficJunctionLoadNode(Vector3 center, int loadPercent)
        {
            var material = loadPercent >= 92 ? trafficPulseMaterial : serviceNeedMaterial;
            var radius = loadPercent >= 92 ? cellSize * 0.32f : cellSize * 0.25f;
            AddLooseCube(planningSignalObjects, "NormalTrafficJunctionLoadNode", material, center + new Vector3(0f, 0.052f, 0f), new Vector3(cellSize * 0.26f, 0.032f, cellSize * 0.26f));
            AddLooseCube(planningSignalObjects, "NormalTrafficJunctionLoadArm", roadLineMaterial, center + new Vector3(radius, 0.08f, 0f), new Vector3(cellSize * 0.16f, 0.024f, 0.04f));
            AddLooseCube(planningSignalObjects, "NormalTrafficJunctionLoadArm", roadLineMaterial, center + new Vector3(-radius, 0.08f, 0f), new Vector3(cellSize * 0.16f, 0.024f, 0.04f));
            AddLooseCube(planningSignalObjects, "NormalTrafficJunctionLoadArm", roadLineMaterial, center + new Vector3(0f, 0.084f, radius), new Vector3(0.04f, 0.024f, cellSize * 0.16f));
            AddLooseCube(planningSignalObjects, "NormalTrafficJunctionLoadArm", roadLineMaterial, center + new Vector3(0f, 0.084f, -radius), new Vector3(0.04f, 0.024f, cellSize * 0.16f));
        }

        private void AddTrafficQueueTicks(Vector3 center, bool vertical, int loadPercent)
        {
            // CITY_SKYLINES_TRAFFIC_QUEUE_TICKS add directional queue hints to overloaded road ribbons.
            var tickCount = loadPercent >= 92 ? 3 : 2;
            for (var i = 0; i < tickCount; i += 1)
            {
                var offset = (i - (tickCount - 1) * 0.5f) * cellSize * 0.18f;
                var tickCenter = vertical
                    ? center + new Vector3(0f, 0.034f, offset)
                    : center + new Vector3(offset, 0.034f, 0f);
                var tickScale = vertical
                    ? new Vector3(0.085f, 0.024f, 0.04f)
                    : new Vector3(0.04f, 0.024f, 0.085f);
                AddLooseCube(planningSignalObjects, "NormalTrafficQueueTick", windowMaterial, tickCenter, tickScale);
            }

            AddTrafficQueuePulseHalo(center, vertical, loadPercent);
        }

        private void AddTrafficQueuePulseHalo(Vector3 center, bool vertical, int loadPercent)
        {
            // CITY_SKYLINES_TRAFFIC_MICRO_PULSE gives bottlenecks a tiny heartbeat in normal and traffic layers.
            if (loadPercent < 76)
            {
                return;
            }

            var material = loadPercent >= 92 ? trafficPulseMaterial : serviceNeedMaterial;
            var major = Mathf.Lerp(cellSize * 0.34f, cellSize * 0.56f, Mathf.Clamp01((loadPercent - 76) / 34f));
            var minor = loadPercent >= 92 ? 0.052f : 0.04f;
            var y = center.y + 0.06f;
            AddLooseCube(planningSignalObjects, "TrafficQueuePulseHalo", material, new Vector3(center.x, y, center.z), vertical ? new Vector3(minor, 0.018f, major) : new Vector3(major, 0.018f, minor));
            AddLooseCube(planningSignalObjects, "TrafficQueuePulseHaloWing", windowMaterial, new Vector3(center.x, y + 0.026f, center.z), vertical ? new Vector3(major * 0.42f, 0.016f, minor) : new Vector3(minor, 0.016f, major * 0.42f));
            AddTrafficHeatFlowBeads(center, vertical, loadPercent, material);
        }

        private void AddTrafficHeatFlowBeads(Vector3 center, bool vertical, int loadPercent, Material material)
        {
            // CITY_SKYLINES_TRAFFIC_HEAT_FLOW_BEADS make bottleneck ribbons read as directional congestion streams.
            var beadCount = loadPercent >= 92 ? 4 : 3;
            var along = vertical ? Vector3.forward : Vector3.right;
            var side = vertical ? Vector3.right : Vector3.forward;
            var spacing = cellSize * 0.145f;
            var sideDrift = loadPercent >= 92 ? cellSize * 0.07f : cellSize * 0.045f;
            var beadScale = vertical
                ? new Vector3(0.052f, 0.026f, cellSize * 0.086f)
                : new Vector3(cellSize * 0.086f, 0.026f, 0.052f);
            var wakeScale = vertical
                ? new Vector3(0.032f, 0.014f, cellSize * 0.16f)
                : new Vector3(cellSize * 0.16f, 0.014f, 0.032f);

            for (var i = 0; i < beadCount; i += 1)
            {
                var t = i - (beadCount - 1) * 0.5f;
                var beadCenter = center + along * (t * spacing) + side * (((i & 1) == 0 ? 1f : -1f) * sideDrift) + new Vector3(0f, 0.094f + i * 0.006f, 0f);
                var beadMaterial = i == beadCount - 1 && loadPercent >= 92 ? trafficPulseMaterial : material;
                AddLooseCube(planningSignalObjects, "TrafficHeatFlowBead", beadMaterial, beadCenter, beadScale);
                AddLooseCube(planningSignalObjects, "TrafficHeatFlowWake", windowMaterial, beadCenter - along * cellSize * 0.085f + new Vector3(0f, -0.026f, 0f), wakeScale);
            }
        }

        private Material TrafficLoadMaterial(int loadPercent)
        {
            // CITY_SKYLINES_TRAFFIC_LOAD_GRADES separates medium pressure from overloaded roads.
            if (loadPercent >= 82)
            {
                return trafficPulseMaterial;
            }

            return serviceNeedMaterial;
        }

        private void RebuildZoneOpportunitySignals(bool expanded)
        {
            // CITY_SKYLINES_ZONE_OPPORTUNITY_MARKERS make idle parcels feel like actionable demand opportunities.
            var grid = controller.Grid;
            var metrics = controller.Metrics;
            if (grid == null || metrics == null || metrics.Demand == null)
            {
                return;
            }

            var signals = new List<ZoneOpportunitySignal>();
            var step = expanded ? 1 : 2;
            for (var y = 0; y < grid.Height; y += step)
            {
                for (var x = 0; x < grid.Width; x += step)
                {
                    var tile = controller.GetTile(x, y);
                    if (!IsZoneOpportunityTile(tile))
                    {
                        continue;
                    }

                    var score = ZoneOpportunityScore(tile.Zone, metrics);
                    if (score < (expanded ? 38 : 56))
                    {
                        continue;
                    }

                    signals.Add(new ZoneOpportunitySignal
                    {
                        Pos = new GridPos(x, y),
                        Zone = tile.Zone,
                        Score = score
                    });
                }
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(expanded ? 32 : 14, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddZoneOpportunityMarker(signals[i].Pos, signals[i].Zone, signals[i].Score, expanded);
            }
        }

        private static bool IsZoneOpportunityTile(TileData tile)
        {
            return tile != null
                && tile.Terrain != TerrainType.Water
                && tile.Zone != ZoneType.None
                && string.IsNullOrEmpty(tile.BuildingId)
                && string.IsNullOrEmpty(tile.RoadId);
        }

        private static int ZoneOpportunityScore(ZoneType zone, CityMetrics metrics)
        {
            if (metrics == null || metrics.Demand == null)
            {
                return 0;
            }

            var demand = metrics.Demand;
            if (zone == ZoneType.Residential) return Mathf.Max(demand.Residential, metrics.HousingCapacity <= metrics.Population + 12 ? 72 : demand.Residential);
            if (zone == ZoneType.Commercial) return Mathf.Max(demand.Commercial, metrics.GoodsBalance < 0 ? 58 : demand.Commercial);
            if (zone == ZoneType.Industrial) return Mathf.Max(demand.Industrial, metrics.GoodsBalance < 0 ? 64 : demand.Industrial);
            if (zone == ZoneType.Office) return Mathf.Max(demand.Office, metrics.WorkforceSkill >= 50 ? demand.Office + 10 : demand.Office);
            if (zone == ZoneType.MixedUse) return Mathf.Max(demand.MixedUse, Mathf.Max(demand.Residential, demand.Commercial));
            if (zone == ZoneType.Civic) return Mathf.Max(demand.Service, metrics.ServiceGapPressure);
            if (zone == ZoneType.Utility) return Mathf.Max(demand.Utility, Mathf.Max(metrics.UtilityUtilization - 22, 100 - metrics.UtilityReliability));
            return 0;
        }

        private void AddZoneOpportunityMarker(GridPos pos, ZoneType zone, int score, bool expanded)
        {
            var material = MaterialForZone(zone);
            var center = CellCenter(pos, roadHeight + 0.13f);
            var height = Mathf.Clamp(0.12f + score * 0.0025f, 0.18f, 0.42f);
            var padSize = expanded ? cellSize * 0.5f : cellSize * 0.38f;
            AddLooseCube(planningSignalObjects, "ZoneOpportunityPad", material, center, new Vector3(padSize, 0.026f, padSize));
            AddLooseCube(planningSignalObjects, "ZoneOpportunityGlow", windowMaterial, center + new Vector3(0f, 0.03f, 0f), new Vector3(padSize * 0.58f, 0.022f, padSize * 0.18f));
            AddLooseCube(planningSignalObjects, "ZoneOpportunityPost", material, center + new Vector3(-cellSize * 0.18f, height * 0.5f + 0.03f, -cellSize * 0.18f), new Vector3(0.045f, height, 0.045f));
            AddLooseCube(planningSignalObjects, "ZoneOpportunityFlag", ZoneOpportunityAccentMaterial(zone), center + new Vector3(-cellSize * 0.1f, height + 0.08f, -cellSize * 0.18f), new Vector3(cellSize * 0.2f, 0.07f, 0.035f));
            AddZoneOpportunityGlyph(pos, zone, center, score);
            AddZoneDemandHotspotPlaque(pos, zone, center, score, expanded);
            AddZoneConstructionSiteCue(pos, zone, center, score, expanded);
        }

        private void AddZoneConstructionSiteCue(GridPos pos, ZoneType zone, Vector3 center, int score, bool expanded)
        {
            // CITY_SKYLINES_CONSTRUCTION_LOT_CUES make empty high-demand parcels read as build sites.
            if (score < (expanded ? 44 : 64))
            {
                return;
            }

            var material = MaterialForZone(zone);
            var hash = DecorationHash(pos.X, pos.Y);
            var width = expanded ? cellSize * 0.52f : cellSize * 0.4f;
            var depth = expanded ? cellSize * 0.42f : cellSize * 0.32f;
            var baseCenter = center + new Vector3(cellSize * 0.12f, -0.066f, -cellSize * 0.12f);
            AddLooseCube(planningSignalObjects, "ConstructionLotFootprintPad", shoreMaterial != null ? shoreMaterial : roadLineMaterial, baseCenter, new Vector3(width, 0.022f, depth));
            AddLooseCube(planningSignalObjects, "ConstructionLotSurveyLine", roadLineMaterial, baseCenter + new Vector3(0f, 0.026f, -depth * 0.36f), new Vector3(width * 0.72f, 0.018f, 0.026f));
            AddLooseCube(planningSignalObjects, "ConstructionLotSurveyLine", roadLineMaterial, baseCenter + new Vector3(-width * 0.36f, 0.028f, 0f), new Vector3(0.026f, 0.018f, depth * 0.72f));

            if (expanded || score >= 78)
            {
                var side = (hash & 1) == 0 ? -1f : 1f;
                AddLooseCube(planningSignalObjects, "ConstructionLotSafetyConeBase", roadLineMaterial, baseCenter + new Vector3(side * width * 0.32f, 0.054f, depth * 0.28f), new Vector3(0.09f, 0.028f, 0.09f));
                AddLooseCube(planningSignalObjects, "ConstructionLotSafetyConeBody", serviceNeedMaterial, baseCenter + new Vector3(side * width * 0.32f, 0.108f, depth * 0.28f), new Vector3(0.06f, 0.082f, 0.06f));
                AddLooseCube(planningSignalObjects, "ConstructionLotPermitTag", material, baseCenter + new Vector3(-side * width * 0.24f, 0.11f, -depth * 0.3f), new Vector3(0.12f, 0.075f, 0.034f));
            }
        }

        private void AddZoneDemandHotspotPlaque(GridPos pos, ZoneType zone, Vector3 center, int score, bool expanded)
        {
            if (!IsCoreDemandZone(zone) || score < (expanded ? 48 : 62))
            {
                return;
            }

            var material = MaterialForZone(zone);
            var accent = ZoneOpportunityAccentMaterial(zone);
            var hash = DecorationHash(pos.X, pos.Y);
            var side = (hash & 1) == 0 ? -1f : 1f;
            var size = Mathf.Lerp(cellSize * 0.16f, cellSize * 0.28f, Mathf.Clamp01(score / 100f));
            var plaqueCenter = center + new Vector3(side * cellSize * 0.26f, -0.075f, cellSize * 0.27f);
            AddLooseCube(planningSignalObjects, "DemandHotspotParcelPlaque", material, plaqueCenter, new Vector3(size, 0.024f, size * 0.7f));
            AddLooseCube(planningSignalObjects, "DemandHotspotParcelHeatLine", windowMaterial, plaqueCenter + new Vector3(0f, 0.026f, 0f), new Vector3(size * 0.72f, 0.018f, 0.035f));
            AddDemandHotspotGlyph(zone, plaqueCenter + new Vector3(0f, 0.055f, 0f), material, accent);
            if (score >= 74)
            {
                AddDemandHotspotTicks(plaqueCenter, accent, score);
            }

            AddDemandHotspotOrderTicket(plaqueCenter, zone, score, side, material, accent);
        }

        private void AddDemandHotspotOrderTicket(Vector3 plaqueCenter, ZoneType zone, int score, float side, Material material, Material accent)
        {
            var ticketCenter = plaqueCenter + new Vector3(-side * cellSize * 0.14f, 0.092f, -cellSize * 0.105f);
            var ticketMaterial = score >= 82 ? accent : material;
            AddLooseCube(planningSignalObjects, "DemandHotspotOrderTicket", ticketMaterial, ticketCenter, new Vector3(cellSize * 0.18f, 0.038f, cellSize * 0.12f));
            AddLooseCube(planningSignalObjects, "DemandHotspotOrderTicketLine", roadLineMaterial, ticketCenter + new Vector3(0f, 0.033f, -cellSize * 0.025f), new Vector3(cellSize * 0.12f, 0.018f, 0.022f));
            AddLooseCube(planningSignalObjects, "DemandHotspotOrderTicketLine", windowMaterial, ticketCenter + new Vector3(0f, 0.055f, cellSize * 0.025f), new Vector3(cellSize * 0.08f, 0.016f, 0.02f));
            if (zone == ZoneType.Residential || zone == ZoneType.Commercial || zone == ZoneType.MixedUse)
            {
                AddLooseCube(planningSignalObjects, "DemandHotspotOrderReadyDot", serviceNeedMaterial, ticketCenter + new Vector3(side * cellSize * 0.09f, 0.078f, 0f), new Vector3(0.052f, 0.04f, 0.052f));
            }
        }

        private static bool IsCoreDemandZone(ZoneType zone)
        {
            return zone == ZoneType.Residential
                || zone == ZoneType.Commercial
                || zone == ZoneType.MixedUse
                || zone == ZoneType.Industrial;
        }

        private void AddDemandHotspotGlyph(ZoneType zone, Vector3 center, Material material, Material accent)
        {
            if (zone == ZoneType.Residential)
            {
                AddLooseCube(planningSignalObjects, "DemandHotspotHomeWall", material, center, new Vector3(0.095f, 0.05f, 0.07f));
                AddLooseCube(planningSignalObjects, "DemandHotspotHomeRoof", roofMaterial, center + new Vector3(0f, 0.044f, 0f), new Vector3(0.12f, 0.034f, 0.085f));
                return;
            }

            if (zone == ZoneType.Commercial || zone == ZoneType.MixedUse)
            {
                AddLooseCube(planningSignalObjects, "DemandHotspotShopFront", windowMaterial, center, new Vector3(0.11f, 0.045f, 0.052f));
                AddLooseCube(planningSignalObjects, "DemandHotspotShopAwning", accent, center + new Vector3(0f, 0.042f, 0f), new Vector3(0.13f, 0.026f, 0.045f));
                return;
            }

            AddLooseCube(planningSignalObjects, "DemandHotspotIndustryShed", material, center, new Vector3(0.12f, 0.05f, 0.08f));
            AddLooseCube(planningSignalObjects, "DemandHotspotIndustryStack", accent, center + new Vector3(-0.042f, 0.052f, 0f), new Vector3(0.038f, 0.095f, 0.038f));
        }

        private void AddDemandHotspotTicks(Vector3 plaqueCenter, Material material, int score)
        {
            var count = score >= 88 ? 3 : 2;
            for (var i = 0; i < count; i += 1)
            {
                var offset = (i - (count - 1) * 0.5f) * 0.058f;
                AddLooseCube(planningSignalObjects, "DemandHotspotHeatTick", material, plaqueCenter + new Vector3(offset, 0.092f + i * 0.006f, -0.055f), new Vector3(0.034f, 0.048f + i * 0.012f, 0.034f));
            }
        }

        private Material ZoneOpportunityAccentMaterial(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return roofMaterial;
            if (zone == ZoneType.Utility || zone == ZoneType.Office) return windowMaterial;
            if (zone == ZoneType.Industrial || zone == ZoneType.Civic) return serviceNeedMaterial;
            return roadLineMaterial;
        }

        private void AddZoneOpportunityGlyph(GridPos pos, ZoneType zone, Vector3 center, int score)
        {
            var y = score >= 72 ? 0.09f : 0.075f;
            var glyphCenter = center + new Vector3(cellSize * 0.16f, y, cellSize * 0.16f);
            if (zone == ZoneType.Residential)
            {
                AddLooseCube(planningSignalObjects, "ZoneOpportunityHomeGlyph", roofMaterial, glyphCenter + new Vector3(0f, 0.04f, 0f), new Vector3(0.16f, 0.07f, 0.13f));
                return;
            }

            if (zone == ZoneType.Commercial || zone == ZoneType.MixedUse)
            {
                AddLooseCube(planningSignalObjects, "ZoneOpportunityStoreGlyph", windowMaterial, glyphCenter, new Vector3(0.18f, 0.045f, 0.055f));
                AddLooseCube(planningSignalObjects, "ZoneOpportunityStoreGlyph", roadLineMaterial, glyphCenter + new Vector3(0f, 0.045f, 0f), new Vector3(0.13f, 0.035f, 0.045f));
                return;
            }

            if (zone == ZoneType.Industrial)
            {
                AddLooseCube(planningSignalObjects, "ZoneOpportunityIndustryGlyph", serviceNeedMaterial, glyphCenter + new Vector3(-0.04f, 0.05f, 0f), new Vector3(0.055f, 0.14f, 0.055f));
                AddLooseCube(planningSignalObjects, "ZoneOpportunityIndustryGlyph", roadLineMaterial, glyphCenter + new Vector3(0.06f, 0.02f, 0f), new Vector3(0.13f, 0.045f, 0.08f));
                return;
            }

            if (zone == ZoneType.Utility)
            {
                AddLooseCube(planningSignalObjects, "ZoneOpportunityUtilityGlyph", windowMaterial, glyphCenter, new Vector3(0.14f, 0.055f, 0.14f));
                AddLooseCube(planningSignalObjects, "ZoneOpportunityUtilityPipeGlyph", roadLineMaterial, glyphCenter + new Vector3(0f, 0.045f, 0f), new Vector3(0.22f, 0.03f, 0.045f));
                return;
            }

            if (zone == ZoneType.Civic)
            {
                AddLooseCube(planningSignalObjects, "ZoneOpportunityServiceGlyph", roadLineMaterial, glyphCenter, new Vector3(0.18f, 0.034f, 0.055f));
                AddLooseCube(planningSignalObjects, "ZoneOpportunityServiceGlyph", roadLineMaterial, glyphCenter, new Vector3(0.055f, 0.034f, 0.18f));
                return;
            }

            AddLooseCube(planningSignalObjects, "ZoneOpportunityOfficeGlyph", windowMaterial, glyphCenter, new Vector3(0.14f, 0.12f, 0.1f));
        }

        private static int TrafficLoadPercent(RoadNode road)
        {
            if (road == null)
            {
                return 0;
            }

            return road.Capacity > 0 ? Mathf.RoundToInt(road.Load * 100f / road.Capacity) : road.Load;
        }

        private void RebuildObjectiveFocusSignal()
        {
            var metrics = controller != null ? controller.Metrics : null;
            if (metrics == null || metrics.ActiveObjective == null || metrics.ActiveObjective.Done)
            {
                return;
            }

            var milestone = FirstOpenMilestone(metrics);
            var milestoneId = milestone != null ? milestone.Id : string.Empty;
            var kind = ObjectiveFocusKindFor(milestoneId);
            GridPos focus;
            int score;
            if (!TryFindObjectiveFocus(kind, milestoneId, out focus, out score)
                && !TryFindObjectiveIssueFocus(out focus, out score))
            {
                return;
            }

            var required = milestone != null ? milestone.Required : metrics.ActiveObjective.Required;
            var progress = milestone != null ? milestone.Progress : metrics.ActiveObjective.Progress;
            AddObjectiveFocusMarker(focus, kind, progress, required, score);
        }

        private static CityMilestone FirstOpenMilestone(CityMetrics metrics)
        {
            if (metrics == null || metrics.Milestones == null)
            {
                return null;
            }

            for (var i = 0; i < metrics.Milestones.Count; i += 1)
            {
                var milestone = metrics.Milestones[i];
                if (milestone != null && !milestone.Done)
                {
                    return milestone;
                }
            }

            return null;
        }

        private bool TryFindObjectiveFocus(ObjectiveFocusKind kind, string milestoneId, out GridPos focus, out int score)
        {
            if (kind == ObjectiveFocusKind.Road)
            {
                return TryFindObjectiveRoadFocus(out focus, out score);
            }

            if (kind == ObjectiveFocusKind.Transit || kind == ObjectiveFocusKind.Service || kind == ObjectiveFocusKind.Utility)
            {
                var mode = ObjectiveCoverageModeFor(kind, milestoneId);
                if (TryFindObjectiveCoverageFocus(mode, out focus, out score))
                {
                    return true;
                }
            }

            if (kind == ObjectiveFocusKind.Upgrade)
            {
                return TryFindObjectiveUpgradeFocus(out focus, out score) || TryFindObjectiveZoneFocus(out focus, out score);
            }

            if (kind == ObjectiveFocusKind.Economy)
            {
                return TryFindObjectiveIssueFocus(out focus, out score);
            }

            return TryFindObjectiveZoneFocus(out focus, out score);
        }

        private bool TryFindObjectiveRoadFocus(out GridPos focus, out int score)
        {
            focus = new GridPos();
            score = -1;
            var roads = controller != null ? controller.Roads : null;
            if (roads == null || roads.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < roads.Count; i += 1)
            {
                var road = roads[i];
                if (road == null)
                {
                    continue;
                }

                var load = TrafficLoadPercent(road);
                var roadScore = load + road.NeighborCount * 8 + (road.Tier == RoadTier.Local ? 10 : 0);
                if (roadScore > score)
                {
                    score = roadScore;
                    focus = road.Pos;
                }
            }

            return score >= 0;
        }

        private bool TryFindObjectiveCoverageFocus(OverlayMode mode, out GridPos focus, out int score)
        {
            focus = new GridPos();
            score = -1;
            var grid = controller != null ? controller.Grid : null;
            var metrics = controller != null ? controller.Metrics : null;
            if (grid == null)
            {
                return false;
            }

            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (!NeedsCoverageSignal(tile, mode, metrics))
                    {
                        continue;
                    }

                    var tileScore = LayerGroundMarkerScore(tile, mode, metrics);
                    if (tileScore > score)
                    {
                        score = tileScore;
                        focus = new GridPos(x, y);
                    }
                }
            }

            return score >= 0;
        }

        private bool TryFindObjectiveZoneFocus(out GridPos focus, out int score)
        {
            focus = new GridPos();
            score = -1;
            var grid = controller != null ? controller.Grid : null;
            var metrics = controller != null ? controller.Metrics : null;
            if (grid == null || metrics == null)
            {
                return false;
            }

            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (!IsZoneOpportunityTile(tile))
                    {
                        continue;
                    }

                    var tileScore = ZoneOpportunityScore(tile.Zone, metrics);
                    if (tileScore > score)
                    {
                        score = tileScore;
                        focus = new GridPos(x, y);
                    }
                }
            }

            return score >= 0;
        }

        private bool TryFindObjectiveUpgradeFocus(out GridPos focus, out int score)
        {
            focus = new GridPos();
            score = -1;
            var buildings = controller != null ? controller.Buildings : null;
            var metrics = controller != null ? controller.Metrics : null;
            if (buildings == null || metrics == null)
            {
                return false;
            }

            for (var i = 0; i < buildings.Count; i += 1)
            {
                var building = buildings[i];
                if (building == null || BuildingLevel(building) >= 3)
                {
                    continue;
                }

                var definition = controller.GetBuildingDefinition(building.ConfigId);
                var modelKey = ModelKeyVisualCatalog(definition);
                if (!VisualSupportsGrowth(definition, modelKey))
                {
                    continue;
                }

                var tile = controller.GetTile(building.Pos.X, building.Pos.Y);
                if (tile == null)
                {
                    continue;
                }

                var growthScore = BuildingGrowthVisualScore(building);
                var blockerScore = BuildingUpgradeMapBlockerScore(building, tile, metrics, growthScore);
                var buildingScore = Mathf.Max(growthScore, blockerScore);
                if (buildingScore > score)
                {
                    score = buildingScore;
                    focus = new GridPos(building.Pos.X + Mathf.Max(0, building.Size.W - 1) / 2, building.Pos.Y + Mathf.Max(0, building.Size.H - 1) / 2);
                }
            }

            return score >= 0;
        }

        private bool TryFindObjectiveIssueFocus(out GridPos focus, out int score)
        {
            focus = new GridPos();
            score = -1;
            var grid = controller != null ? controller.Grid : null;
            var metrics = controller != null ? controller.Metrics : null;
            if (grid == null)
            {
                return false;
            }

            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    var severity = CityIssueSeverity(tile, metrics);
                    if (severity > score)
                    {
                        score = severity;
                        focus = new GridPos(x, y);
                    }
                }
            }

            return score >= 12;
        }

        private void AddObjectiveFocusMarker(GridPos pos, ObjectiveFocusKind kind, int progress, int required, int score)
        {
            var material = ObjectiveFocusMaterial(kind, score);
            var center = CellCenter(pos, roadHeight + 0.18f);
            AddObjectiveFocusRing(center, material);
            AddLooseCube(planningSignalObjects, "ObjectiveFocusPinBase", material, center + new Vector3(0f, 0.025f, 0f), new Vector3(cellSize * 0.32f, 0.055f, cellSize * 0.32f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusPinPost", material, center + new Vector3(0f, 0.27f, 0f), new Vector3(0.07f, 0.48f, 0.07f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusPinCap", roadLineMaterial, center + new Vector3(0f, 0.54f, 0f), new Vector3(cellSize * 0.34f, 0.07f, cellSize * 0.34f));
            AddObjectiveFocusGlyph(center + new Vector3(0f, 0.64f, 0f), kind, material);
            AddObjectiveFocusProgress(center, progress, required, material);
            AddObjectiveFocusMobileOrderTag(center, kind, progress, required, material);
        }

        private void AddObjectiveFocusRing(Vector3 center, Material material)
        {
            var radius = cellSize * 0.46f;
            AddLooseCube(planningSignalObjects, "ObjectiveFocusRingNorth", material, center + new Vector3(0f, 0f, radius), new Vector3(cellSize * 0.42f, 0.028f, 0.055f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusRingSouth", material, center + new Vector3(0f, 0f, -radius), new Vector3(cellSize * 0.42f, 0.028f, 0.055f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusRingEast", material, center + new Vector3(radius, 0f, 0f), new Vector3(0.055f, 0.028f, cellSize * 0.42f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusRingWest", material, center + new Vector3(-radius, 0f, 0f), new Vector3(0.055f, 0.028f, cellSize * 0.42f));
        }

        private void AddObjectiveFocusGlyph(Vector3 center, ObjectiveFocusKind kind, Material material)
        {
            if (kind == ObjectiveFocusKind.Road || kind == ObjectiveFocusKind.Transit)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusRoadGlyph", windowMaterial, center, new Vector3(0.26f, 0.04f, 0.055f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusRoadGlyph", material, center + new Vector3(0f, 0.045f, 0f), new Vector3(0.055f, 0.04f, 0.2f));
                return;
            }

            if (kind == ObjectiveFocusKind.Service)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusServiceGlyph", material, center, new Vector3(0.24f, 0.04f, 0.06f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusServiceGlyph", material, center, new Vector3(0.06f, 0.04f, 0.24f));
                return;
            }

            if (kind == ObjectiveFocusKind.Utility)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusUtilityGlyph", windowMaterial, center, new Vector3(0.17f, 0.06f, 0.17f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusUtilityPipe", material, center + new Vector3(0f, 0.055f, 0f), new Vector3(0.26f, 0.035f, 0.055f));
                return;
            }

            if (kind == ObjectiveFocusKind.Upgrade)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusUpgradeStem", previewOkMaterial, center + new Vector3(0f, 0.035f, 0f), new Vector3(0.065f, 0.16f, 0.065f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusUpgradeArrow", roadLineMaterial, center + new Vector3(0f, 0.14f, 0f), new Vector3(0.19f, 0.055f, 0.1f));
                return;
            }

            if (kind == ObjectiveFocusKind.Economy)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusEconomyGlyph", serviceNeedMaterial, center, new Vector3(0.2f, 0.05f, 0.14f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusEconomyLine", roadLineMaterial, center + new Vector3(0f, 0.055f, 0f), new Vector3(0.14f, 0.028f, 0.032f));
                return;
            }

            AddLooseCube(planningSignalObjects, "ObjectiveFocusZoneGlyph", material, center, new Vector3(0.18f, 0.05f, 0.18f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusZoneSpark", roadLineMaterial, center + new Vector3(0f, 0.075f, 0f), new Vector3(0.09f, 0.06f, 0.09f));
        }

        private void AddObjectiveFocusProgress(Vector3 center, int progress, int required, Material material)
        {
            var steps = 4;
            var amount = required <= 0 ? 0f : Mathf.Clamp01(progress / (float)Mathf.Max(1, required));
            var filled = Mathf.Clamp(Mathf.CeilToInt(amount * steps), 0, steps);
            for (var i = 0; i < steps; i += 1)
            {
                var x = (i - 1.5f) * 0.11f;
                var pipMaterial = i < filled ? material : buildingFootprintMaterial;
                AddLooseCube(planningSignalObjects, "ObjectiveFocusProgressPip", pipMaterial, center + new Vector3(x, 0.11f, -cellSize * 0.32f), new Vector3(0.075f, 0.035f, 0.045f));
            }
        }

        private void AddObjectiveFocusMobileOrderTag(Vector3 center, ObjectiveFocusKind kind, int progress, int required, Material material)
        {
            var done = required > 0 && progress >= required;
            var tagMaterial = done ? previewOkMaterial : material;
            var tagCenter = center + new Vector3(cellSize * 0.34f, 0.24f, -cellSize * 0.28f);
            AddLooseCube(planningSignalObjects, "ObjectiveFocusOrderTagBack", buildingFootprintMaterial, tagCenter + new Vector3(0.035f, -0.035f, 0.035f), new Vector3(cellSize * 0.36f, 0.026f, cellSize * 0.22f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusOrderTagCard", tagMaterial, tagCenter, new Vector3(cellSize * 0.32f, 0.046f, cellSize * 0.2f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusOrderTagLine", roadLineMaterial, tagCenter + new Vector3(0f, 0.042f, -cellSize * 0.035f), new Vector3(cellSize * 0.22f, 0.018f, 0.026f));
            AddLooseCube(planningSignalObjects, "ObjectiveFocusOrderTagLine", windowMaterial, tagCenter + new Vector3(0f, 0.066f, cellSize * 0.035f), new Vector3(cellSize * 0.16f, 0.016f, 0.024f));
            if (done)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusRewardDot", serviceNeedMaterial, tagCenter + new Vector3(cellSize * 0.18f, 0.082f, 0f), new Vector3(0.07f, 0.05f, 0.07f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusRewardSpark", roadLineMaterial, tagCenter + new Vector3(cellSize * 0.24f, 0.12f, -cellSize * 0.055f), new Vector3(0.042f, 0.065f, 0.042f));
                return;
            }

            if (kind == ObjectiveFocusKind.Upgrade)
            {
                AddLooseCube(planningSignalObjects, "ObjectiveFocusUpgradeMiniArrow", previewOkMaterial, tagCenter + new Vector3(cellSize * 0.18f, 0.086f, 0f), new Vector3(0.05f, 0.11f, 0.05f));
                AddLooseCube(planningSignalObjects, "ObjectiveFocusUpgradeMiniArrowHead", roadLineMaterial, tagCenter + new Vector3(cellSize * 0.18f, 0.15f, 0f), new Vector3(0.12f, 0.038f, 0.08f));
            }
        }

        private Material ObjectiveFocusMaterial(ObjectiveFocusKind kind, int score)
        {
            if (score >= 72)
            {
                return trafficPulseMaterial;
            }

            if (kind == ObjectiveFocusKind.Road || kind == ObjectiveFocusKind.Transit)
            {
                return commercialMaterial;
            }

            if (kind == ObjectiveFocusKind.Service || kind == ObjectiveFocusKind.Economy)
            {
                return serviceNeedMaterial;
            }

            if (kind == ObjectiveFocusKind.Utility)
            {
                return utilityMaterial;
            }

            if (kind == ObjectiveFocusKind.Upgrade)
            {
                return previewOkMaterial;
            }

            return mixedUseMaterial;
        }

        private static ObjectiveFocusKind ObjectiveFocusKindFor(string milestoneId)
        {
            if (IsObjectiveId(milestoneId, "road_grid", "connected_grid", "arterial_spine", "road_care", "safe_roads", "traffic_flow", "complete_streets", "signal_optimization", "congestion_pricing"))
            {
                return ObjectiveFocusKind.Road;
            }

            if (IsObjectiveId(milestoneId, "walkable_city", "smooth_commute", "low_car_core", "parking_relief", "parking_fees", "transit_spine", "transit_capacity", "transit_reliability", "metro_network", "regional_gateway"))
            {
                return ObjectiveFocusKind.Transit;
            }

            if (IsObjectiveId(milestoneId, "balanced_utilities", "utility_resilience", "renewable_power", "water_sanitation", "stormwater_ready", "green_city", "healthy_city", "connected_business", "communication_capacity", "mail_service"))
            {
                return ObjectiveFocusKind.Utility;
            }

            if (IsObjectiveId(milestoneId, "service_core", "service_capacity", "balanced_services", "response_ready", "disaster_preparedness", "health_net", "regional_healthcare", "healthcare_capacity", "deathcare_ready", "education_net", "education_capacity", "safety_net", "fire_resilience", "secure_blocks", "police_readiness", "clean_blocks", "waste_capacity", "freight_loop", "freight_capacity", "supply_chain_buffer", "rail_freight_gateway"))
            {
                return ObjectiveFocusKind.Service;
            }

            if (IsObjectiveId(milestoneId, "vertical_growth", "quality_blocks", "density_core", "mixed_core"))
            {
                return ObjectiveFocusKind.Upgrade;
            }

            if (IsObjectiveId(milestoneId, "service_budget_balance", "healthy_budget", "fiscal_credit", "debt_service_control", "civic_administration", "administration_capacity", "policy_trial"))
            {
                return ObjectiveFocusKind.Economy;
            }

            return ObjectiveFocusKind.Zone;
        }

        private static OverlayMode ObjectiveCoverageModeFor(ObjectiveFocusKind kind, string milestoneId)
        {
            if (kind == ObjectiveFocusKind.Transit)
            {
                if (IsObjectiveId(milestoneId, "parking_relief", "parking_fees", "low_car_core"))
                {
                    return OverlayMode.Parking;
                }

                return OverlayMode.Transit;
            }

            if (kind == ObjectiveFocusKind.Utility)
            {
                if (IsObjectiveId(milestoneId, "stormwater_ready", "green_city", "healthy_city"))
                {
                    return OverlayMode.Stormwater;
                }

                if (IsObjectiveId(milestoneId, "connected_business", "communication_capacity", "mail_service"))
                {
                    return OverlayMode.Communications;
                }

                return OverlayMode.Utilities;
            }

            if (IsObjectiveId(milestoneId, "clean_blocks", "waste_capacity"))
            {
                return OverlayMode.Waste;
            }

            if (IsObjectiveId(milestoneId, "freight_loop", "freight_capacity", "supply_chain_buffer", "rail_freight_gateway"))
            {
                return OverlayMode.Logistics;
            }

            return OverlayMode.Services;
        }

        private static bool IsObjectiveId(string value, params string[] ids)
        {
            if (string.IsNullOrEmpty(value) || ids == null)
            {
                return false;
            }

            for (var i = 0; i < ids.Length; i += 1)
            {
                if (value == ids[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildMapIssueHotspots()
        {
            // LOW_POLY_BASE_MAP_ISSUE_HOTSPOTS keeps top city problems visible in the bright map layer.
            ClearObjects(mapIssueObjects);
            if (controller == null || controller.Grid == null || controller.Metrics == null || controller.OverlayMode != OverlayMode.Normal)
            {
                return;
            }

            var grid = controller.Grid;
            var metrics = controller.Metrics;
            var signals = new List<CityIssueSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    if (tile == null || tile.Terrain == TerrainType.Water)
                    {
                        continue;
                    }

                    var severity = CityIssueSeverity(tile, metrics);
                    if (severity < 36)
                    {
                        continue;
                    }

                    signals.Add(new CityIssueSignal
                    {
                        Pos = new GridPos(x, y),
                        Tile = tile,
                        Severity = severity
                    });
                }
            }

            signals.Sort((left, right) => right.Severity.CompareTo(left.Severity));
            var count = Mathf.Min(10, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddMapIssueHotspot(signals[i].Pos, signals[i].Tile, signals[i].Severity, metrics);
            }
        }

        private void AddMapIssueHotspot(GridPos pos, TileData tile, int severity, CityMetrics metrics)
        {
            var fallback = CityIssueUsesTrafficMaterial(tile, metrics) ? trafficPulseMaterial : serviceNeedMaterial;
            var kind = CityIssueAdvisorKind(tile, metrics);
            var material = CityIssueAdvisorMaterial(kind, fallback);
            var center = CellCenter(pos, roadHeight + 0.11f);
            var radius = severity >= 58 ? cellSize * 0.48f : cellSize * 0.38f;
            var span = severity >= 58 ? cellSize * 0.3f : cellSize * 0.22f;
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotPad", material, center, new Vector3(cellSize * 0.28f, 0.026f, cellSize * 0.28f));
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotNorth", material, center + new Vector3(0f, 0.018f, radius), new Vector3(span, 0.02f, 0.038f));
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotSouth", material, center + new Vector3(0f, 0.018f, -radius), new Vector3(span, 0.02f, 0.038f));
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotEast", material, center + new Vector3(radius, 0.022f, 0f), new Vector3(0.038f, 0.02f, span));
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotWest", material, center + new Vector3(-radius, 0.022f, 0f), new Vector3(0.038f, 0.02f, span));
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotPost", roadLineMaterial, center + new Vector3(-cellSize * 0.24f, 0.13f, cellSize * 0.24f), new Vector3(0.04f, 0.24f, 0.04f));
            AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotFlag", material, center + new Vector3(-cellSize * 0.14f, 0.25f, cellSize * 0.24f), new Vector3(cellSize * 0.18f, 0.07f, 0.035f));
            AddMapIssueHotspotGlyph(kind, center + new Vector3(0f, 0.074f, 0f), material);
            AddMapIssueHotspotOrderTicket(kind, center, material, severity);

            if (severity >= 58)
            {
                AddLooseCube(mapIssueObjects, "BaseMapIssueHotspotPriorityDot", windowMaterial, center + new Vector3(cellSize * 0.22f, 0.086f, -cellSize * 0.22f), new Vector3(0.08f, 0.055f, 0.08f));
            }
        }

        private void AddMapIssueHotspotOrderTicket(CityIssueAdvisorMarkerKind kind, Vector3 center, Material material, int severity)
        {
            var ticketMaterial = severity >= 58 ? trafficPulseMaterial : material;
            var ticketCenter = center + new Vector3(cellSize * 0.24f, 0.126f, -cellSize * 0.2f);
            AddLooseCube(mapIssueObjects, "BaseMapIssueOrderTicket", ticketMaterial, ticketCenter, new Vector3(cellSize * 0.22f, 0.036f, cellSize * 0.14f));
            AddLooseCube(mapIssueObjects, "BaseMapIssueOrderTicketLine", roadLineMaterial, ticketCenter + new Vector3(0f, 0.034f, -cellSize * 0.028f), new Vector3(cellSize * 0.15f, 0.016f, 0.022f));
            AddLooseCube(mapIssueObjects, "BaseMapIssueOrderTicketLine", windowMaterial, ticketCenter + new Vector3(0f, 0.056f, cellSize * 0.028f), new Vector3(cellSize * 0.1f, 0.015f, 0.02f));
            if (kind == CityIssueAdvisorMarkerKind.Traffic || severity >= 58)
            {
                AddLooseCube(mapIssueObjects, "BaseMapIssueOrderHotDot", serviceNeedMaterial, ticketCenter + new Vector3(cellSize * 0.12f, 0.076f, 0f), new Vector3(0.052f, 0.04f, 0.052f));
            }
        }

        private void AddMapIssueHotspotGlyph(CityIssueAdvisorMarkerKind kind, Vector3 center, Material material)
        {
            if (kind == CityIssueAdvisorMarkerKind.Traffic)
            {
                AddLooseCube(mapIssueObjects, "BaseMapIssueTrafficGlyph", roadLineMaterial, center, new Vector3(cellSize * 0.22f, 0.022f, 0.04f));
                AddLooseCube(mapIssueObjects, "BaseMapIssueTrafficQueue", trafficPulseMaterial, center + new Vector3(0f, 0.028f, 0f), new Vector3(cellSize * 0.12f, 0.026f, 0.035f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Service)
            {
                AddLooseCube(mapIssueObjects, "BaseMapIssueServiceCross", serviceNeedMaterial, center, new Vector3(cellSize * 0.2f, 0.024f, 0.045f));
                AddLooseCube(mapIssueObjects, "BaseMapIssueServiceCross", serviceNeedMaterial, center + new Vector3(0f, 0.002f, 0f), new Vector3(0.045f, 0.024f, cellSize * 0.2f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Utility)
            {
                AddLooseCube(mapIssueObjects, "BaseMapIssueUtilityDrop", windowMaterial, center, new Vector3(0.13f, 0.045f, 0.13f));
                AddLooseCube(mapIssueObjects, "BaseMapIssueUtilityPipe", roadLineMaterial, center + new Vector3(0f, 0.036f, 0f), new Vector3(cellSize * 0.2f, 0.022f, 0.036f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Fiscal)
            {
                AddLooseCube(mapIssueObjects, "BaseMapIssueFiscalLedger", serviceNeedMaterial, center, new Vector3(cellSize * 0.2f, 0.026f, cellSize * 0.13f));
                AddLooseCube(mapIssueObjects, "BaseMapIssueFiscalLine", roadLineMaterial, center + new Vector3(0f, 0.034f, 0f), new Vector3(cellSize * 0.13f, 0.02f, 0.028f));
                return;
            }

            AddLooseCube(mapIssueObjects, "BaseMapIssueGeneralDot", material, center + new Vector3(0f, 0.026f, 0f), new Vector3(0.11f, 0.055f, 0.11f));
        }

        private void RebuildCityIssueBadges()
        {
            // NORMAL_VIEW_CITY_ISSUE_BADGES gives the main city view a compact problem layer.
            var grid = controller.Grid;
            var metrics = controller.Metrics;
            var signals = new List<CityIssueSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    var tile = controller.GetTile(x, y);
                    var severity = CityIssueSignalSeverity(tile, metrics);
                    if (severity < 14 || (severity < 28 && ((x + y) & 1) != 0))
                    {
                        continue;
                    }

                    signals.Add(new CityIssueSignal
                    {
                        Pos = new GridPos(x, y),
                        Tile = tile,
                        Severity = severity
                    });
                }
            }

            signals.Sort((left, right) => right.Severity.CompareTo(left.Severity));
            var count = Mathf.Min(36, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                var signal = signals[i];
                var material = CityIssueUsesTrafficMaterial(signal.Tile, metrics) ? trafficPulseMaterial : serviceNeedMaterial;
                var height = 0.18f + Mathf.Clamp(signal.Severity, 0, 90) * 0.004f;
                AddCityIssueBadge(signal.Pos, signal.Tile, material, height, signal.Severity, metrics);
            }
        }

        private static int CityIssueSignalSeverity(TileData tile, CityMetrics metrics)
        {
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return 0;
            }

            var severity = CityIssueSeverity(tile, metrics);
            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                severity = Mathf.Max(severity, tile.Traffic - 42);
                severity = Mathf.Max(severity, 42 - tile.RoadMaintenanceAccess);
            }

            var occupiedOrZoned = !string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None;
            if (occupiedOrZoned)
            {
                var serviceGap = CityServiceGapPressure(tile);
                if (serviceGap > 0)
                {
                    severity = Mathf.Max(severity, 16 + serviceGap);
                }

                severity = Mathf.Max(severity, tile.Traffic - 48);
                severity = Mathf.Max(severity, 34 - tile.ParkingAccess);
                severity = Mathf.Max(severity, 32 - tile.StormwaterAccess);
            }

            if (metrics != null && occupiedOrZoned)
            {
                severity = Mathf.Max(severity, FiscalIssueSeverity(metrics));
                severity = Mathf.Max(severity, 100 - metrics.UtilityReliability);
            }

            return Mathf.Clamp(severity, 0, 96);
        }

        private static int CityServiceGapPressure(TileData tile)
        {
            if (tile == null)
            {
                return 0;
            }

            var coreServiceFloor = Mathf.Min(Mathf.Min(tile.HealthAccess, tile.FireProtectionAccess), Mathf.Min(tile.SafetyAccess, tile.SecurityAccess));
            var serviceGap = Mathf.Max(36 - ServiceAccessValue(tile), 30 - coreServiceFloor);
            serviceGap = Mathf.Max(serviceGap, 30 - tile.WasteAccess);
            serviceGap = Mathf.Max(serviceGap, 30 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess));
            serviceGap = Mathf.Max(serviceGap, 28 - tile.TransitAccess + tile.Traffic / 6);
            serviceGap = Mathf.Max(serviceGap, 28 - tile.LogisticsAccess + tile.Traffic / 6);
            return Mathf.Max(0, serviceGap);
        }

        private void AddCityIssueBadge(GridPos pos, TileData tile, Material material, float height, int severity, CityMetrics metrics)
        {
            var center = CellCenter(pos, roadHeight + height * 0.5f + 0.18f);
            AddCityIssueBadgeFooting(pos, tile, material, severity, metrics);
            AddCityIssueAdvisorLocator(pos, tile, material, severity, metrics);
            AddCityIssueInformationStencil(pos, tile, material, severity, metrics);
            AddLooseCube(planningSignalObjects, "CityIssueBadgePost", material, center, new Vector3(0.08f, height, 0.08f));
            var capSize = severity >= 48 ? 0.32f : 0.24f;
            var capCenter = center + new Vector3(0f, height * 0.5f + 0.05f, 0f);
            AddLooseCube(planningSignalObjects, "CityIssueBadgeCap", material, capCenter, new Vector3(capSize, 0.08f, capSize));
            if (severity >= 44)
            {
                AddLooseCube(planningSignalObjects, "CityIssueBadgePriorityCrown", roadLineMaterial, center + new Vector3(0f, height * 0.5f + 0.13f, 0f), new Vector3(0.18f, 0.045f, 0.18f));
            }

            AddCityIssueSeverityTicks(capCenter, severity);
            AddCityIssueBadgeGlyph(pos, tile, metrics, capCenter + new Vector3(0f, severity >= 44 ? 0.19f : 0.15f, 0f), material);
            AddCityIssueBadgeCategoryTabs(pos, tile, metrics, capCenter, severity, material);
            AddCityIssueBadgeReadoutLadder(capCenter, severity, material);
        }

        private void AddCityIssueBadgeCategoryTabs(GridPos pos, TileData tile, CityMetrics metrics, Vector3 capCenter, int severity, Material material)
        {
            // CITY_SKYLINES_ISSUE_BADGE_TABS add compact category tabs so stacked problem badges are scannable.
            var kind = CityIssueAdvisorKind(tile, metrics);
            var tabMaterial = CityIssueAdvisorMaterial(kind, material);
            var name = CityIssueAdvisorName(kind);
            var tabCenter = capCenter + new Vector3(cellSize * 0.19f, 0.076f, cellSize * 0.13f);
            AddLooseCube(planningSignalObjects, "CityIssue" + name + "BadgeCategoryTab", tabMaterial, tabCenter, new Vector3(0.15f, 0.036f, 0.08f));
            AddLooseCube(planningSignalObjects, "CityIssue" + name + "BadgeCategoryTabEdge", roadLineMaterial, tabCenter + new Vector3(0f, 0.034f, 0f), new Vector3(0.105f, 0.018f, 0.03f));

            if (severity >= 52)
            {
                AddLooseCube(planningSignalObjects, "CityIssue" + name + "BadgePriorityPin", trafficPulseMaterial, tabCenter + new Vector3(0.07f, 0.072f, -0.025f), new Vector3(0.052f, 0.052f, 0.052f));
            }

            if (kind == CityIssueAdvisorMarkerKind.Traffic)
            {
                var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
                var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y) || !vertical;
                var lineScale = horizontal ? new Vector3(0.13f, 0.016f, 0.024f) : new Vector3(0.024f, 0.016f, 0.13f);
                AddLooseCube(planningSignalObjects, "CityIssueTrafficBadgeMiniRibbon", trafficPulseMaterial, tabCenter + new Vector3(0f, 0.064f, 0f), lineScale);
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Service)
            {
                AddLooseCube(planningSignalObjects, "CityIssueServiceBadgeMiniCross", serviceNeedMaterial, tabCenter + new Vector3(0f, 0.064f, 0f), new Vector3(0.11f, 0.018f, 0.026f));
                AddLooseCube(planningSignalObjects, "CityIssueServiceBadgeMiniCross", serviceNeedMaterial, tabCenter + new Vector3(0f, 0.066f, 0f), new Vector3(0.026f, 0.018f, 0.11f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Utility)
            {
                AddLooseCube(planningSignalObjects, "CityIssueUtilityBadgeMiniPipe", windowMaterial, tabCenter + new Vector3(0f, 0.062f, 0f), new Vector3(0.12f, 0.02f, 0.034f));
                AddLooseCube(planningSignalObjects, "CityIssueUtilityBadgeMiniNode", roadLineMaterial, tabCenter + new Vector3(0.052f, 0.086f, 0f), new Vector3(0.044f, 0.04f, 0.044f));
            }
        }

        private void AddCityIssueBadgeReadoutLadder(Vector3 capCenter, int severity, Material material)
        {
            // CITY_SKYLINES_ISSUE_BADGE_LADDER makes the badge severity readable from a glancing angle.
            if (severity < 28)
            {
                return;
            }

            var rungCount = severity >= 64 ? 4 : (severity >= 46 ? 3 : 2);
            var railCenter = capCenter + new Vector3(-0.18f, 0.07f, 0.16f);
            AddLooseCube(planningSignalObjects, "CityIssueBadgeReadoutLadderRail", roadLineMaterial, railCenter + new Vector3(0f, 0.034f, 0f), new Vector3(0.028f, 0.11f, 0.032f));
            for (var i = 0; i < rungCount; i += 1)
            {
                var rungMaterial = i == rungCount - 1 && severity >= 58 ? trafficPulseMaterial : material;
                AddLooseCube(planningSignalObjects, "CityIssueBadgeReadoutLadderRung", rungMaterial, railCenter + new Vector3(0.056f, 0.014f + i * 0.034f, 0f), new Vector3(0.1f, 0.018f, 0.034f));
            }
        }

        private void AddCityIssueInformationStencil(GridPos pos, TileData tile, Material material, int severity, CityMetrics metrics)
        {
            // CITY_SKYLINES_INFORMATION_STENCIL gives normal-view problem pins a tile-level diagnosis footprint.
            var kind = CityIssueAdvisorKind(tile, metrics);
            var stencilMaterial = CityIssueAdvisorMaterial(kind, material);
            var center = CellCenter(pos, roadHeight + 0.074f);
            var span = Mathf.Lerp(cellSize * 0.46f, cellSize * 0.72f, Mathf.Clamp01(severity / 72f));
            var corner = severity >= 48 ? cellSize * 0.2f : cellSize * 0.15f;
            var inset = span * 0.5f;
            AddLooseCube(planningSignalObjects, "CityIssueInfoFrameNorthWestA", stencilMaterial, center + new Vector3(-inset, 0f, inset), new Vector3(corner, 0.018f, 0.034f));
            AddLooseCube(planningSignalObjects, "CityIssueInfoFrameNorthWestB", stencilMaterial, center + new Vector3(-inset, 0.002f, inset), new Vector3(0.034f, 0.018f, corner));
            AddLooseCube(planningSignalObjects, "CityIssueInfoFrameSouthEastA", stencilMaterial, center + new Vector3(inset, 0f, -inset), new Vector3(corner, 0.018f, 0.034f));
            AddLooseCube(planningSignalObjects, "CityIssueInfoFrameSouthEastB", stencilMaterial, center + new Vector3(inset, 0.002f, -inset), new Vector3(0.034f, 0.018f, corner));
            AddLooseCube(planningSignalObjects, "CityIssueInfoFramePulseDot", severity >= 48 ? trafficPulseMaterial : windowMaterial, center + new Vector3(inset * 0.46f, 0.035f, inset * 0.46f), new Vector3(0.07f, 0.045f, 0.07f));

            if (kind == CityIssueAdvisorMarkerKind.Traffic)
            {
                AddCityIssueTrafficLoadStencil(pos, tile, center, severity);
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Service)
            {
                AddCityIssueServiceGapStencil(tile, center, severity);
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Utility)
            {
                AddCityIssueUtilityGapStencil(center, severity);
            }
        }

        private void AddCityIssueTrafficLoadStencil(GridPos pos, TileData tile, Vector3 center, int severity)
        {
            var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
            var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y) || !vertical;
            var load = tile != null ? Mathf.Max(tile.Traffic, severity + 48) : severity + 48;
            var material = load >= 78 || severity >= 44 ? trafficPulseMaterial : serviceNeedMaterial;
            var along = vertical && !horizontal ? Vector3.forward : Vector3.right;
            var side = vertical && !horizontal ? Vector3.right : Vector3.forward;
            var length = Mathf.Lerp(cellSize * 0.34f, cellSize * 0.58f, Mathf.Clamp01((load - 52) / 48f));
            var laneScale = vertical && !horizontal
                ? new Vector3(0.052f, 0.02f, length)
                : new Vector3(length, 0.02f, 0.052f);
            AddLooseCube(planningSignalObjects, "CityIssueTrafficLoadLane", material, center + side * cellSize * 0.16f + new Vector3(0f, 0.046f, 0f), laneScale);
            AddLooseCube(planningSignalObjects, "CityIssueTrafficLoadLane", roadLineMaterial, center - side * cellSize * 0.16f + new Vector3(0f, 0.05f, 0f), laneScale);
            AddLooseCube(planningSignalObjects, "CityIssueTrafficLoadQueueHead", windowMaterial, center + along * (length * 0.38f) + new Vector3(0f, 0.078f, 0f), vertical && !horizontal ? new Vector3(0.1f, 0.034f, 0.045f) : new Vector3(0.045f, 0.034f, 0.1f));
        }

        private void AddCityIssueServiceGapStencil(TileData tile, Vector3 center, int severity)
        {
            var gap = tile != null ? CityServiceGapPressure(tile) : 0;
            var span = Mathf.Lerp(cellSize * 0.34f, cellSize * 0.62f, Mathf.Clamp01(Mathf.Max(gap, severity) / 64f));
            AddLooseCube(planningSignalObjects, "CityIssueServiceGapScanHorizontal", serviceNeedMaterial, center + new Vector3(0f, 0.048f, -span * 0.44f), new Vector3(span, 0.02f, 0.038f));
            AddLooseCube(planningSignalObjects, "CityIssueServiceGapScanVertical", serviceNeedMaterial, center + new Vector3(-span * 0.44f, 0.052f, 0f), new Vector3(0.038f, 0.02f, span));
            AddLooseCube(planningSignalObjects, "CityIssueServiceGapMissingNode", trafficPulseMaterial, center + new Vector3(span * 0.4f, 0.088f, span * 0.4f), new Vector3(0.095f, 0.07f, 0.095f));
            AddLooseCube(planningSignalObjects, "CityIssueServiceGapNeedLine", roadLineMaterial, center + new Vector3(span * 0.16f, 0.09f, span * 0.16f), new Vector3(span * 0.44f, 0.026f, 0.034f));
        }

        private void AddCityIssueUtilityGapStencil(Vector3 center, int severity)
        {
            var span = severity >= 48 ? cellSize * 0.56f : cellSize * 0.42f;
            AddLooseCube(planningSignalObjects, "CityIssueUtilityGapBasin", windowMaterial, center + new Vector3(0f, 0.044f, 0f), new Vector3(span, 0.018f, span * 0.42f));
            AddLooseCube(planningSignalObjects, "CityIssueUtilityGapPipe", roadLineMaterial, center + new Vector3(-span * 0.22f, 0.076f, 0f), new Vector3(span * 0.48f, 0.024f, 0.038f));
            AddLooseCube(planningSignalObjects, "CityIssueUtilityGapWarning", severity >= 48 ? trafficPulseMaterial : serviceNeedMaterial, center + new Vector3(span * 0.28f, 0.1f, 0f), new Vector3(0.08f, 0.082f, 0.08f));
        }

        private void AddCityIssueAdvisorLocator(GridPos pos, TileData tile, Material material, int severity, CityMetrics metrics)
        {
            // CITY_SKYLINES_ADVISOR_LOCATOR makes problem tiles easy to find from the city-level view.
            var kind = CityIssueAdvisorKind(tile, metrics);
            var center = CellCenter(pos, roadHeight + 0.128f);
            var pulseMaterial = CityIssueAdvisorMaterial(kind, material);
            var radius = severity >= 48 ? cellSize * 0.54f : cellSize * 0.44f;
            var thickness = severity >= 48 ? 0.046f : 0.034f;
            var shortSpan = severity >= 36 ? cellSize * 0.36f : cellSize * 0.28f;
            AddLooseCube(planningSignalObjects, "CityIssueAdvisorPulseNorth", pulseMaterial, center + new Vector3(0f, 0f, radius), new Vector3(shortSpan, 0.018f, thickness));
            AddLooseCube(planningSignalObjects, "CityIssueAdvisorPulseSouth", pulseMaterial, center + new Vector3(0f, 0f, -radius), new Vector3(shortSpan, 0.018f, thickness));
            AddLooseCube(planningSignalObjects, "CityIssueAdvisorPulseEast", pulseMaterial, center + new Vector3(radius, 0.006f, 0f), new Vector3(thickness, 0.018f, shortSpan));
            AddLooseCube(planningSignalObjects, "CityIssueAdvisorPulseWest", pulseMaterial, center + new Vector3(-radius, 0.006f, 0f), new Vector3(thickness, 0.018f, shortSpan));

            if (severity >= 36)
            {
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorPulsePing", roadLineMaterial, center + new Vector3(-radius * 0.54f, 0.034f, -radius * 0.54f), new Vector3(cellSize * 0.14f, 0.024f, 0.04f));
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorPulsePing", windowMaterial, center + new Vector3(radius * 0.54f, 0.038f, radius * 0.54f), new Vector3(0.04f, 0.024f, cellSize * 0.14f));
            }

            AddCityIssueHotspotFlag(center, kind, severity, pulseMaterial);
            AddCityIssueAdvisorPlate(center, kind, material);
        }

        private void AddCityIssueHotspotFlag(Vector3 center, CityIssueAdvisorMarkerKind kind, int severity, Material material)
        {
            var name = CityIssueAdvisorName(kind);
            var horizontal = kind != CityIssueAdvisorMarkerKind.Traffic;
            var flagBase = center + new Vector3(-cellSize * 0.34f, 0.018f, cellSize * 0.32f);
            AddLooseCube(planningSignalObjects, "CityIssue" + name + "HotspotFlagPost", roadLineMaterial, flagBase + new Vector3(0f, 0.15f, 0f), new Vector3(0.038f, 0.3f, 0.038f));
            AddLooseCube(planningSignalObjects, "CityIssue" + name + "HotspotFlag", material, flagBase + new Vector3(horizontal ? cellSize * 0.08f : 0f, 0.28f, horizontal ? 0f : cellSize * 0.08f), horizontal ? new Vector3(cellSize * 0.2f, 0.075f, 0.034f) : new Vector3(0.034f, 0.075f, cellSize * 0.2f));

            if (severity >= 44)
            {
                AddLooseCube(planningSignalObjects, "CityIssue" + name + "HotspotFlagTip", windowMaterial, flagBase + new Vector3(horizontal ? cellSize * 0.19f : 0f, 0.335f, horizontal ? 0f : cellSize * 0.19f), new Vector3(0.064f, 0.035f, 0.064f));
            }
        }

        private void AddCityIssueAdvisorPlate(Vector3 center, CityIssueAdvisorMarkerKind kind, Material material)
        {
            var name = CityIssueAdvisorName(kind);
            var plateMaterial = CityIssueAdvisorMaterial(kind, material);
            var plateCenter = center + new Vector3(cellSize * 0.32f, 0.096f, -cellSize * 0.32f);
            AddLooseCube(planningSignalObjects, "CityIssue" + name + "AdvisorPlate", plateMaterial, plateCenter, new Vector3(cellSize * 0.26f, 0.058f, 0.05f));
            AddLooseCube(planningSignalObjects, "CityIssue" + name + "AdvisorPlateHeader", roadLineMaterial, plateCenter + new Vector3(0f, 0.052f, 0f), new Vector3(cellSize * 0.18f, 0.024f, 0.056f));
            AddCityIssueAdvisorPlateGlyph(kind, plateCenter + new Vector3(0f, 0.092f, 0f), plateMaterial);
        }

        private void AddCityIssueAdvisorPlateGlyph(CityIssueAdvisorMarkerKind kind, Vector3 center, Material material)
        {
            if (kind == CityIssueAdvisorMarkerKind.Traffic)
            {
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorTrafficGlyph", roadLineMaterial, center, new Vector3(0.16f, 0.026f, 0.032f));
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorTrafficGlyph", trafficPulseMaterial, center + new Vector3(0f, 0.034f, 0f), new Vector3(0.1f, 0.03f, 0.028f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Service)
            {
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorServiceGlyph", serviceNeedMaterial, center, new Vector3(0.15f, 0.028f, 0.034f));
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorServiceGlyph", serviceNeedMaterial, center, new Vector3(0.034f, 0.028f, 0.15f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Fiscal)
            {
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorFiscalGlyph", serviceNeedMaterial, center, new Vector3(0.15f, 0.032f, 0.04f));
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorFiscalGlyphLine", roadLineMaterial, center + new Vector3(0f, 0.036f, 0f), new Vector3(0.1f, 0.024f, 0.03f));
                return;
            }

            if (kind == CityIssueAdvisorMarkerKind.Utility)
            {
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorUtilityGlyph", windowMaterial, center, new Vector3(0.11f, 0.05f, 0.11f));
                AddLooseCube(planningSignalObjects, "CityIssueAdvisorUtilityGlyphPipe", roadLineMaterial, center + new Vector3(0f, 0.042f, 0f), new Vector3(0.16f, 0.026f, 0.032f));
                return;
            }

            AddLooseCube(planningSignalObjects, "CityIssueAdvisorGeneralGlyph", material, center + new Vector3(0f, 0.018f, 0f), new Vector3(0.1f, 0.07f, 0.1f));
        }

        private Material CityIssueAdvisorMaterial(CityIssueAdvisorMarkerKind kind, Material fallback)
        {
            if (kind == CityIssueAdvisorMarkerKind.Traffic) return trafficPulseMaterial;
            if (kind == CityIssueAdvisorMarkerKind.Service) return serviceNeedMaterial;
            if (kind == CityIssueAdvisorMarkerKind.Fiscal) return roadLineMaterial;
            if (kind == CityIssueAdvisorMarkerKind.Utility) return windowMaterial;
            return fallback;
        }

        private static CityIssueAdvisorMarkerKind CityIssueAdvisorKind(TileData tile, CityMetrics metrics)
        {
            var occupiedOrZoned = tile != null && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None);
            if (tile != null && (!string.IsNullOrEmpty(tile.RoadId) || tile.Traffic >= 58 || tile.ParkingAccess < 24))
            {
                return CityIssueAdvisorMarkerKind.Traffic;
            }

            if (tile != null && occupiedOrZoned && CityServiceGapPressure(tile) >= 8)
            {
                return CityIssueAdvisorMarkerKind.Service;
            }

            if (metrics != null && IsFiscalStress(metrics))
            {
                return CityIssueAdvisorMarkerKind.Fiscal;
            }

            if (tile != null && (tile.StormwaterAccess < 24 || (metrics != null && (metrics.UtilityReliability < 95 || metrics.FloodRisk > 55))))
            {
                return CityIssueAdvisorMarkerKind.Utility;
            }

            return CityIssueAdvisorMarkerKind.General;
        }

        private static string CityIssueAdvisorName(CityIssueAdvisorMarkerKind kind)
        {
            if (kind == CityIssueAdvisorMarkerKind.Traffic) return "Traffic";
            if (kind == CityIssueAdvisorMarkerKind.Service) return "Service";
            if (kind == CityIssueAdvisorMarkerKind.Fiscal) return "Fiscal";
            if (kind == CityIssueAdvisorMarkerKind.Utility) return "Utility";
            return "General";
        }

        private void AddCityIssueSeverityTicks(Vector3 capCenter, int severity)
        {
            // CITY_SKYLINES_FLOATING_ISSUE_STACK makes normal-view pins readable before opening a layer.
            var ticks = severity >= 62 ? 3 : (severity >= 36 ? 2 : 1);
            for (var i = 0; i < ticks; i += 1)
            {
                var tickMaterial = severity >= 62 && i == ticks - 1 ? trafficPulseMaterial : roadLineMaterial;
                var tickHeight = 0.045f + i * 0.018f;
                var x = (i - (ticks - 1) * 0.5f) * 0.085f;
                AddLooseCube(planningSignalObjects, "CityIssueSeverityTick", tickMaterial, capCenter + new Vector3(x, 0.095f + i * 0.01f, -0.12f), new Vector3(0.052f, tickHeight, 0.035f));
            }
        }

        private void AddCityIssueBadgeGlyph(GridPos pos, TileData tile, CityMetrics metrics, Vector3 center, Material material)
        {
            if (tile == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(tile.RoadId) || tile.Traffic >= 58)
            {
                var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
                var longScale = vertical ? new Vector3(0.045f, 0.04f, 0.24f) : new Vector3(0.24f, 0.04f, 0.045f);
                var shortScale = vertical ? new Vector3(0.03f, 0.035f, 0.13f) : new Vector3(0.13f, 0.035f, 0.03f);
                AddLooseCube(planningSignalObjects, "CityIssueTrafficGlyphRoad", roadLineMaterial, center, longScale);
                AddLooseCube(planningSignalObjects, "CityIssueTrafficGlyphQueue", trafficPulseMaterial, center + new Vector3(0f, 0.055f, 0f), shortScale);
                return;
            }

            if (PollutionStress(tile) >= 42)
            {
                AddLooseCube(planningSignalObjects, "CityIssuePollutionGlyphStack", trafficPulseMaterial, center + new Vector3(-0.045f, 0.035f, 0f), new Vector3(0.055f, 0.14f, 0.055f));
                AddLooseCube(planningSignalObjects, "CityIssuePollutionGlyphPuff", serviceNeedMaterial, center + new Vector3(0.055f, 0.13f, 0f), new Vector3(0.13f, 0.06f, 0.13f));
                return;
            }

            if ((metrics != null && (metrics.FloodRisk > 55 || metrics.StormwaterResilience < 62)) || tile.StormwaterAccess < 24)
            {
                AddLooseCube(planningSignalObjects, "CityIssueWaterGlyphBase", windowMaterial, center, new Vector3(0.18f, 0.035f, 0.13f));
                AddLooseCube(planningSignalObjects, "CityIssueWaterGlyphDrop", roadLineMaterial, center + new Vector3(0f, 0.08f, 0f), new Vector3(0.09f, 0.09f, 0.09f));
                return;
            }

            if (tile.ParkingAccess < 24 && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None))
            {
                AddLooseCube(planningSignalObjects, "CityIssueParkingGlyphStem", roadLineMaterial, center + new Vector3(-0.045f, 0.04f, 0f), new Vector3(0.052f, 0.14f, 0.05f));
                AddLooseCube(planningSignalObjects, "CityIssueParkingGlyphLoop", roadLineMaterial, center + new Vector3(0.045f, 0.09f, 0f), new Vector3(0.14f, 0.052f, 0.05f));
                return;
            }

            if (ServiceAccessValue(tile) < 28 && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None))
            {
                AddLooseCube(planningSignalObjects, "CityIssueServiceGlyphPlus", serviceNeedMaterial, center, new Vector3(0.24f, 0.04f, 0.055f));
                AddLooseCube(planningSignalObjects, "CityIssueServiceGlyphPlus", serviceNeedMaterial, center, new Vector3(0.055f, 0.04f, 0.24f));
                return;
            }

            if (tile.LandValue < 35 && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None))
            {
                AddLooseCube(planningSignalObjects, "CityIssueLandValueGlyphPlaque", serviceNeedMaterial, center, new Vector3(0.18f, 0.045f, 0.18f));
                AddLooseCube(planningSignalObjects, "CityIssueLandValueGlyphSpark", roadLineMaterial, center + new Vector3(0f, 0.075f, 0f), new Vector3(0.09f, 0.07f, 0.09f));
                return;
            }

            if (metrics != null && IsFiscalStress(metrics))
            {
                AddLooseCube(planningSignalObjects, "CityIssueFiscalGlyphLedger", serviceNeedMaterial, center, new Vector3(0.2f, 0.045f, 0.14f));
                AddLooseCube(planningSignalObjects, "CityIssueFiscalGlyphLine", roadLineMaterial, center + new Vector3(0f, 0.055f, -0.035f), new Vector3(0.14f, 0.025f, 0.025f));
                AddLooseCube(planningSignalObjects, "CityIssueFiscalGlyphLine", roadLineMaterial, center + new Vector3(0f, 0.082f, 0.035f), new Vector3(0.12f, 0.025f, 0.025f));
                return;
            }

            AddLooseCube(planningSignalObjects, "CityIssueGenericGlyphDot", material, center + new Vector3(0f, 0.045f, 0f), new Vector3(0.12f, 0.1f, 0.12f));
        }

        private void AddCityIssueBadgeFooting(GridPos pos, TileData tile, Material material, int severity, CityMetrics metrics)
        {
            // CITY_SKYLINES_ISSUE_PRIORITY_FOOTING makes normal-view issues read as prioritized diagnostics.
            var center = CellCenter(pos, roadHeight + 0.105f);
            var baseSize = severity >= 44 ? 0.38f : 0.3f;
            AddLooseCube(planningSignalObjects, "CityIssueBadgeBase", material, center, new Vector3(baseSize, 0.052f, baseSize));
            var occupiedOrZoned = tile != null && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None);
            if (occupiedOrZoned && IsFiscalStress(metrics))
            {
                AddLooseCube(planningSignalObjects, "CityIssueFiscalLedgerFootnote", serviceNeedMaterial, center + new Vector3(-0.09f, 0.058f, 0.09f), new Vector3(0.15f, 0.035f, 0.055f));
                AddLooseCube(planningSignalObjects, "CityIssueFiscalLedgerFootnote", roadLineMaterial, center + new Vector3(-0.09f, 0.098f, 0.09f), new Vector3(0.11f, 0.028f, 0.045f));
            }

            if (tile != null && (!string.IsNullOrEmpty(tile.RoadId) || tile.Traffic >= 45))
            {
                var vertical = HasRoadTile(pos.X, pos.Y - 1) || HasRoadTile(pos.X, pos.Y + 1);
                var horizontal = HasRoadTile(pos.X - 1, pos.Y) || HasRoadTile(pos.X + 1, pos.Y) || !vertical;
                var cueScale = horizontal
                    ? new Vector3(cellSize * 0.34f, 0.026f, 0.045f)
                    : new Vector3(0.045f, 0.026f, cellSize * 0.34f);
                AddLooseCube(planningSignalObjects, "CityIssueTrafficDirectionCue", roadLineMaterial, center + new Vector3(0f, 0.036f, 0f), cueScale);
                return;
            }

            if (tile != null && PollutionStress(tile) >= 42)
            {
                AddLooseCube(planningSignalObjects, "CityIssuePollutionStackFootnote", trafficPulseMaterial, center + new Vector3(-0.07f, 0.065f, 0f), new Vector3(0.055f, 0.13f, 0.055f));
                AddLooseCube(planningSignalObjects, "CityIssuePollutionPuffFootnote", serviceNeedMaterial, center + new Vector3(0.07f, 0.16f, 0f), new Vector3(0.13f, 0.06f, 0.13f));
                return;
            }

            if (tile != null && tile.StormwaterAccess < 24)
            {
                AddLooseCube(planningSignalObjects, "CityIssueStormwaterFootnote", windowMaterial, center + new Vector3(0f, 0.032f, 0f), new Vector3(cellSize * 0.24f, 0.024f, cellSize * 0.16f));
                AddLooseCube(planningSignalObjects, "CityIssueStormwaterDropFootnote", roadLineMaterial, center + new Vector3(0f, 0.08f, 0f), new Vector3(0.09f, 0.07f, 0.09f));
                return;
            }

            if (tile != null && tile.ParkingAccess < 24 && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None))
            {
                AddLooseCube(planningSignalObjects, "CityIssueParkingFootnote", roadLineMaterial, center + new Vector3(-0.06f, 0.04f, 0f), new Vector3(0.07f, 0.08f, 0.16f));
                AddLooseCube(planningSignalObjects, "CityIssueParkingFootnote", roadLineMaterial, center + new Vector3(0.08f, 0.04f, 0f), new Vector3(0.07f, 0.08f, 0.16f));
                return;
            }

            if (tile != null && ServiceAccessValue(tile) < 28 && occupiedOrZoned)
            {
                AddLooseCube(planningSignalObjects, "CityIssueServiceCrossFootnote", serviceNeedMaterial, center + new Vector3(0f, 0.045f, 0f), new Vector3(cellSize * 0.2f, 0.03f, 0.055f));
                AddLooseCube(planningSignalObjects, "CityIssueServiceCrossFootnote", serviceNeedMaterial, center + new Vector3(0f, 0.047f, 0f), new Vector3(0.055f, 0.03f, cellSize * 0.2f));
                return;
            }

            if (tile != null && tile.LandValue < 35 && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None))
            {
                AddLooseCube(planningSignalObjects, "CityIssueLandValuePlaqueFootnote", serviceNeedMaterial, center + new Vector3(0f, 0.036f, 0f), new Vector3(cellSize * 0.22f, 0.035f, cellSize * 0.22f));
                AddLooseCube(planningSignalObjects, "CityIssueLandValueSparkFootnote", roadLineMaterial, center + new Vector3(0f, 0.086f, 0f), new Vector3(0.1f, 0.06f, 0.1f));
                return;
            }

            AddLooseCube(planningSignalObjects, "CityIssueServiceFooting", roadLineMaterial, center + new Vector3(0f, 0.036f, 0f), new Vector3(cellSize * 0.18f, 0.026f, cellSize * 0.18f));
        }

        private void RebuildBuildingUpgradeSignals()
        {
            var metrics = controller.Metrics;
            var buildings = controller.Buildings;
            if (metrics == null || buildings == null)
            {
                return;
            }

            if (metrics.BuildingUpgradeReadyCount <= 0
                && metrics.BuildingUpgradeBlockedCount <= 0
                && metrics.ServiceGapPressure < 48
                && !IsFiscalStress(metrics))
            {
                return;
            }

            var signals = new List<BuildingUpgradeSignal>();
            for (var i = 0; i < buildings.Count; i += 1)
            {
                var building = buildings[i];
                if (BuildingLevel(building) >= 3)
                {
                    continue;
                }

                var definition = controller.GetBuildingDefinition(building.ConfigId);
                var modelKey = ModelKeyVisualCatalog(definition);
                if (!VisualSupportsGrowth(definition, modelKey))
                {
                    continue;
                }

                var tile = controller.GetTile(building.Pos.X, building.Pos.Y);
                if (tile == null)
                {
                    continue;
                }

                var growthScore = BuildingGrowthVisualScore(building);
                var ready = metrics.BuildingUpgradeReadyCount > 0 && growthScore >= 72;
                var blockerScore = BuildingUpgradeMapBlockerScore(building, tile, metrics, growthScore);
                var blocked = !ready && blockerScore >= 26;
                if (!ready && !blocked)
                {
                    continue;
                }

                signals.Add(new BuildingUpgradeSignal
                {
                    Building = building,
                    Tile = tile,
                    Score = ready ? growthScore : blockerScore,
                    GrowthScore = growthScore,
                    Ready = ready
                });
            }

            signals.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(18, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddBuildingUpgradeMapHint(signals[i], metrics);
            }
        }

        private int BuildingUpgradeMapBlockerScore(PlacedBuilding building, TileData tile, CityMetrics metrics, int growthScore)
        {
            var score = Mathf.Max(0, 62 - growthScore);
            if (string.IsNullOrEmpty(building.ConnectedRoadId))
            {
                score = Mathf.Max(score, 72);
            }

            score = Mathf.Max(score, tile.Traffic - 38);
            score = Mathf.Max(score, 34 - ServiceAccessValue(tile));
            if (metrics != null)
            {
                score = Mathf.Max(score, metrics.BuildingUpgradeBlockedCount > 0 ? 34 : 0);
                score = Mathf.Max(score, metrics.ServiceGapPressure - 18);
                score = Mathf.Max(score, metrics.DevelopmentQuality < 52 ? 52 - metrics.DevelopmentQuality : 0);
                score = Mathf.Max(score, FiscalIssueSeverity(metrics) - 4);
            }

            return Mathf.Clamp(score, 0, 100);
        }

        private void AddBuildingUpgradeMapHint(BuildingUpgradeSignal signal, CityMetrics metrics)
        {
            var building = signal.Building;
            var tile = signal.Tile;
            var width = Mathf.Max(1, building.Size.W) * cellSize * 0.72f;
            var depth = Mathf.Max(1, building.Size.H) * cellSize * 0.72f;
            var center = new Vector3(
                (building.Pos.X + Mathf.Max(1, building.Size.W) * 0.5f) * cellSize,
                roadHeight + 0.16f,
                (building.Pos.Y + Mathf.Max(1, building.Size.H) * 0.5f) * cellSize);
            var markerCenter = center + new Vector3(width * 0.28f, 0f, -depth * 0.34f);
            AddBuildingUpgradeMaturityGlow(signal, center, width, depth);

            if (signal.Ready)
            {
                AddLooseCube(planningSignalObjects, "BuildingUpgradeReadyHalo", previewOkMaterial, markerCenter, new Vector3(width * 0.42f, 0.03f, depth * 0.16f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeArrowStem", previewOkMaterial, markerCenter + new Vector3(0f, 0.11f, 0f), new Vector3(0.055f, 0.2f, 0.055f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeArrowCap", roadLineMaterial, markerCenter + new Vector3(0f, 0.23f, 0f), new Vector3(0.18f, 0.05f, 0.08f));
            }
            else
            {
                var blockerMaterial = BuildingGrowthBlockerMaterial(building);
                AddLooseCube(planningSignalObjects, "BuildingUpgradeBlockedPad", blockerMaterial, markerCenter, new Vector3(width * 0.3f, 0.04f, depth * 0.14f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeBlockedPost", blockerMaterial, markerCenter + new Vector3(0f, 0.11f, 0f), new Vector3(0.05f, 0.2f, 0.05f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeBlockedDot", roadLineMaterial, markerCenter + new Vector3(0f, 0.24f, 0f), new Vector3(0.09f, 0.045f, 0.06f));
                AddBuildingGrowthBottleneckTag(building, tile, metrics, center, width, depth);
            }

            AddBuildingUpgradePressureCues(building, tile, metrics, center, width, depth, signal.Ready);
            AddBuildingUpgradeCandidateMeter(signal, center, width, depth);
            AddBuildingUpgradeOrderCard(signal, center, width, depth);
        }

        private void AddBuildingUpgradeCandidateMeter(BuildingUpgradeSignal signal, Vector3 center, float width, float depth)
        {
            // CITY_SKYLINES_UPGRADE_CANDIDATE_METER makes readiness visible even before the arrow appears.
            var score = Mathf.Clamp(signal.GrowthScore, 0, 100);
            if (score < 48 && !signal.Ready)
            {
                return;
            }

            var steps = score >= 86 ? 3 : (score >= 64 ? 2 : 1);
            var material = signal.Ready ? previewOkMaterial : serviceNeedMaterial;
            var baseCenter = center + new Vector3(width * 0.38f, 0.065f, depth * 0.38f);
            AddLooseCube(planningSignalObjects, "BuildingUpgradeCandidateMeterBase", roadLineMaterial, baseCenter, new Vector3(0.18f, 0.026f, 0.12f));
            for (var i = 0; i < steps; i += 1)
            {
                AddLooseCube(planningSignalObjects, "BuildingUpgradeCandidateMeterPip", i == steps - 1 ? material : windowMaterial, baseCenter + new Vector3((i - 1) * 0.065f, 0.052f + i * 0.014f, 0f), new Vector3(0.045f, 0.052f + i * 0.018f, 0.045f));
            }
        }

        private void AddBuildingUpgradeOrderCard(BuildingUpgradeSignal signal, Vector3 center, float width, float depth)
        {
            var cardMaterial = signal.Ready ? previewOkMaterial : serviceNeedMaterial;
            var cardCenter = center + new Vector3(-width * 0.34f, 0.19f, depth * 0.43f);
            AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderCardShadow", buildingFootprintMaterial, cardCenter + new Vector3(0.035f, -0.045f, 0.035f), new Vector3(0.34f, 0.022f, 0.2f));
            AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderCard", cardMaterial, cardCenter, new Vector3(0.3f, 0.05f, 0.18f));
            AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderLine", roadLineMaterial, cardCenter + new Vector3(0f, 0.045f, -0.035f), new Vector3(0.2f, 0.018f, 0.024f));
            AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderLine", windowMaterial, cardCenter + new Vector3(0f, 0.068f, 0.035f), new Vector3(0.14f, 0.016f, 0.022f));

            if (signal.Ready)
            {
                AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderRewardDot", roadLineMaterial, cardCenter + new Vector3(0.17f, 0.092f, 0f), new Vector3(0.064f, 0.046f, 0.064f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderRewardGlint", windowMaterial, cardCenter + new Vector3(0.22f, 0.13f, -0.045f), new Vector3(0.04f, 0.058f, 0.04f));
                return;
            }

            AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderBlockerDot", trafficPulseMaterial, cardCenter + new Vector3(0.17f, 0.088f, 0f), new Vector3(0.058f, 0.05f, 0.058f));
            AddLooseCube(planningSignalObjects, "BuildingUpgradeOrderBlockerLine", roadLineMaterial, cardCenter + new Vector3(0.17f, 0.132f, 0f), new Vector3(0.032f, 0.07f, 0.032f));
        }

        private void AddBuildingUpgradeMaturityGlow(BuildingUpgradeSignal signal, Vector3 center, float width, float depth)
        {
            var maturity = Mathf.Clamp(signal.GrowthScore, 0, 100);
            if (maturity < 44)
            {
                return;
            }

            var material = signal.Ready ? previewOkMaterial : windowMaterial;
            var glowWidth = Mathf.Lerp(width * 0.2f, width * 0.48f, maturity / 100f);
            var glowCenter = center + new Vector3(-width * 0.02f, 0.035f, -depth * 0.48f);
            AddLooseCube(planningSignalObjects, "BuildingUpgradeMaturityGlow", material, glowCenter, new Vector3(glowWidth, 0.022f, 0.05f));
            if (maturity >= 64)
            {
                AddLooseCube(planningSignalObjects, "BuildingUpgradeMaturityGlint", roadLineMaterial, glowCenter + new Vector3(width * 0.18f, 0.036f, 0f), new Vector3(0.075f, 0.032f, 0.075f));
            }
        }

        private void AddBuildingGrowthBottleneckTag(PlacedBuilding building, TileData tile, CityMetrics metrics, Vector3 center, float width, float depth)
        {
            var material = BuildingGrowthBlockerMaterial(building);
            if (metrics != null && metrics.GrowthBottleneckScore >= 62)
            {
                material = trafficPulseMaterial;
            }
            else if (tile != null && ServiceAccessValue(tile) < 30)
            {
                material = serviceNeedMaterial;
            }

            var tagCenter = center + new Vector3(-width * 0.36f, 0.05f, -depth * 0.42f);
            AddLooseCube(planningSignalObjects, "BuildingGrowthBottleneckCornerTag", material, tagCenter, new Vector3(0.17f, 0.03f, 0.045f));
            AddLooseCube(planningSignalObjects, "BuildingGrowthBottleneckCornerTag", material, tagCenter + new Vector3(-0.062f, 0.002f, 0.062f), new Vector3(0.045f, 0.03f, 0.17f));
            AddLooseCube(planningSignalObjects, "BuildingGrowthBottleneckPulseDot", roadLineMaterial, tagCenter + new Vector3(0.018f, 0.052f, 0.018f), new Vector3(0.078f, 0.045f, 0.078f));
            if (metrics != null && metrics.GrowthBottleneckScore >= 74)
            {
                AddLooseCube(planningSignalObjects, "BuildingGrowthBottleneckHotTick", trafficPulseMaterial, tagCenter + new Vector3(0.1f, 0.082f, -0.032f), new Vector3(0.042f, 0.08f, 0.042f));
            }
        }

        private void AddBuildingUpgradePressureCues(PlacedBuilding building, TileData tile, CityMetrics metrics, Vector3 center, float width, float depth, bool ready)
        {
            if (!ready && (string.IsNullOrEmpty(building.ConnectedRoadId) || tile.Traffic >= 58))
            {
                var trafficCenter = center + new Vector3(-width * 0.28f, 0.02f, -depth * 0.34f);
                AddLooseCube(planningSignalObjects, "BuildingUpgradeBlockedTrafficCue", trafficPulseMaterial, trafficCenter, new Vector3(width * 0.22f, 0.028f, 0.052f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeBlockedTrafficCue", trafficPulseMaterial, trafficCenter + new Vector3(0f, 0.002f, 0f), new Vector3(0.052f, 0.028f, depth * 0.2f));
            }

            if (ServiceAccessValue(tile) < 30 || (metrics != null && metrics.ServiceGapPressure >= 55))
            {
                var serviceCenter = center + new Vector3(-width * 0.28f, 0.04f, depth * 0.32f);
                AddLooseCube(planningSignalObjects, "BuildingUpgradeServiceCue", serviceNeedMaterial, serviceCenter, new Vector3(0.2f, 0.032f, 0.055f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeServiceCue", serviceNeedMaterial, serviceCenter, new Vector3(0.055f, 0.032f, 0.2f));
            }

            if (IsFiscalStress(metrics))
            {
                var fiscalCenter = center + new Vector3(width * 0.28f, 0.04f, depth * 0.32f);
                AddLooseCube(planningSignalObjects, "BuildingUpgradeFiscalCue", roadLineMaterial, fiscalCenter, new Vector3(0.18f, 0.03f, 0.06f));
                AddLooseCube(planningSignalObjects, "BuildingUpgradeFiscalCue", serviceNeedMaterial, fiscalCenter + new Vector3(0f, 0.042f, 0f), new Vector3(0.13f, 0.028f, 0.052f));
            }
        }

        private void RebuildCoverageNeedSignals(OverlayMode mode)
        {
            var grid = controller.Grid;
            var signals = new List<CoverageNeedSignal>();
            for (var y = 0; y < grid.Height; y += 1)
            {
                for (var x = 0; x < grid.Width; x += 1)
                {
                    if (((x + y) & 1) != 0)
                    {
                        continue;
                    }

                    var tile = controller.GetTile(x, y);
                    if (!NeedsCoverageSignal(tile, mode, controller.Metrics))
                    {
                        continue;
                    }

                    var height = CoverageSignalHeight(tile, mode, controller.Metrics);
                    signals.Add(new CoverageNeedSignal
                    {
                        Pos = new GridPos(x, y),
                        Height = height
                    });
                }
            }

            signals.Sort((left, right) => right.Height.CompareTo(left.Height));
            var count = Mathf.Min(64, signals.Count);
            for (var i = 0; i < count; i += 1)
            {
                AddServiceGapPin(signals[i].Pos, mode, signals[i].Height);
            }
        }

        private void RebuildCoverageProviderAnchors(OverlayMode mode)
        {
            // CITY_SKYLINES_COVERAGE_PROVIDER_ANCHORS show service sources as well as uncovered demand.
            var buildings = controller.Buildings;
            if (buildings == null)
            {
                return;
            }

            var added = 0;
            var vehicleAdded = 0;
            for (var i = 0; i < buildings.Count && added < 36; i += 1)
            {
                var building = buildings[i];
                var definition = controller.GetBuildingDefinition(building.ConfigId);
                var modelKey = ModelKeyVisualCatalog(definition);
                if (!CoverageProviderMatchesMode(modelKey, definition, mode))
                {
                    continue;
                }

                AddCoverageProviderAnchor(building, definition, modelKey, mode);
                if (vehicleAdded < 12 && TryAddCoverageProviderVehicle(building, modelKey, mode, vehicleAdded))
                {
                    vehicleAdded += 1;
                }

                added += 1;
            }
        }

        private void AddCoverageProviderAnchor(PlacedBuilding building, BuildingDefinition definition, string modelKey, OverlayMode mode)
        {
            var center = new Vector3(
                (building.Pos.X + Mathf.Max(1, building.Size.W) * 0.5f) * cellSize,
                roadHeight + 0.105f,
                (building.Pos.Y + Mathf.Max(1, building.Size.H) * 0.5f) * cellSize);
            var radius = definition != null && definition.ServiceRadius > 0 ? definition.ServiceRadius : 7;
            var span = Mathf.Clamp(0.36f + radius * 0.035f, 0.46f, 1.08f);
            var material = CoverageProviderMaterial(mode, modelKey);
            AddLooseCube(planningSignalObjects, "CoverageProviderHalo", material, center, new Vector3(cellSize * span, 0.018f, cellSize * span));
            AddLooseCube(planningSignalObjects, "CoverageProviderHaloCore", windowMaterial, center + new Vector3(0f, 0.022f, 0f), new Vector3(cellSize * span * 0.62f, 0.018f, cellSize * span * 0.22f));
            AddCoverageProviderSourcePulse(center, span, mode, material);
            AddCoverageProviderBudgetPulseEdge(center, span, mode, material);
            AddCoverageProviderRangeTicks(center, span, material);
            AddCoverageProviderRangeFlags(center, span, mode, material);
            AddCoverageProviderRangeBadge(center, span, mode, material);
            AddCoverageProviderRadiusPetals(center, span, mode, material);
            AddLooseCube(planningSignalObjects, "CoverageProviderBeacon", material, center + new Vector3(0f, 0.18f, 0f), new Vector3(0.105f, 0.25f, 0.105f));
            AddLooseCube(planningSignalObjects, "CoverageProviderCap", material, center + new Vector3(0f, 0.33f, 0f), new Vector3(0.24f, 0.06f, 0.24f));
            AddCoverageProviderModeGlyph(center, mode, material);
        }

        private bool TryAddCoverageProviderVehicle(PlacedBuilding building, string modelKey, OverlayMode mode, int vehicleIndex)
        {
            // REFERENCE_IMAGE_PROVIDER_RESPONSE_VEHICLES add small operated-service cues to coverage layers.
            if (!CoverageProviderVehicleEligible(modelKey, mode))
            {
                return false;
            }

            if (!CoverageProviderVehiclePriority(modelKey, mode) && DecorationHash(building.Pos.X, building.Pos.Y) % 2 != 0)
            {
                return false;
            }

            RoadNode road;
            bool horizontal;
            if (!TryFindProviderRoadAnchor(building, out road, out horizontal))
            {
                return false;
            }

            var hash = DecorationHash(road.Pos.X + vehicleIndex * 3, road.Pos.Y + vehicleIndex * 7);
            var roadTop = road.Tier == RoadTier.Arterial ? roadHeight * 1.35f : roadHeight;
            var laneOffset = ((hash & 1) == 0 ? -0.2f : 0.2f) * cellSize;
            var alongOffset = (((hash >> 2) & 3) - 1.5f) * cellSize * 0.09f;
            var offset = horizontal
                ? new Vector3(alongOffset, 0f, laneOffset)
                : new Vector3(laneOffset, 0f, alongOffset);
            var center = CellCenter(road.Pos, roadTop + 0.088f) + offset;
            var bodyMaterial = CoverageProviderVehicleMaterial(modelKey, mode);
            var accentMaterial = CoverageProviderMaterial(mode, modelKey);

            AddProviderVehicleShadow(center, horizontal);
            AddProviderVehiclePart("CoverageProviderVehicleBody", bodyMaterial, center, horizontal, 0.3f, 0.15f, 0.075f);
            AddProviderVehiclePart("CoverageProviderVehicleCab", windowMaterial, center + new Vector3(0f, 0.057f, 0f), horizontal, 0.13f, 0.1f, 0.048f);
            AddProviderVehicleWheels(center, horizontal);
            AddProviderVehicleMarker(center, horizontal, modelKey, mode, accentMaterial);
            return true;
        }

        private bool TryFindProviderRoadAnchor(PlacedBuilding building, out RoadNode road, out bool horizontal)
        {
            road = null;
            horizontal = true;
            var roads = controller != null ? controller.Roads : null;
            if (building == null || roads == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(building.ConnectedRoadId))
            {
                for (var i = 0; i < roads.Count; i += 1)
                {
                    if (roads[i].Id == building.ConnectedRoadId)
                    {
                        road = roads[i];
                        horizontal = ProviderRoadIsHorizontal(roads, road);
                        return true;
                    }
                }
            }

            var width = Mathf.Max(1, building.Size.W);
            var depth = Mathf.Max(1, building.Size.H);
            var minX = building.Pos.X - 1;
            var maxX = building.Pos.X + width;
            var minY = building.Pos.Y - 1;
            var maxY = building.Pos.Y + depth;
            var targetX = building.Pos.X * 2 + width;
            var targetY = building.Pos.Y * 2 + depth;
            var bestScore = int.MaxValue;
            for (var i = 0; i < roads.Count; i += 1)
            {
                var candidate = roads[i];
                if (candidate.Pos.X < minX || candidate.Pos.X > maxX || candidate.Pos.Y < minY || candidate.Pos.Y > maxY)
                {
                    continue;
                }

                var score = Mathf.Abs(candidate.Pos.X * 2 + 1 - targetX) + Mathf.Abs(candidate.Pos.Y * 2 + 1 - targetY);
                if (score < bestScore)
                {
                    bestScore = score;
                    road = candidate;
                }
            }

            if (road == null)
            {
                return false;
            }

            horizontal = ProviderRoadIsHorizontal(roads, road);
            return true;
        }

        private static bool ProviderRoadIsHorizontal(IReadOnlyList<RoadNode> roads, RoadNode road)
        {
            if (road == null)
            {
                return true;
            }

            var hasLeft = HasRoadAt(roads, road.Pos.X - 1, road.Pos.Y);
            var hasRight = HasRoadAt(roads, road.Pos.X + 1, road.Pos.Y);
            var hasDown = HasRoadAt(roads, road.Pos.X, road.Pos.Y - 1);
            var hasUp = HasRoadAt(roads, road.Pos.X, road.Pos.Y + 1);
            var hasHorizontal = hasLeft || hasRight;
            var hasVertical = hasDown || hasUp;
            return hasHorizontal || !hasVertical;
        }

        private static bool CoverageProviderVehicleEligible(string modelKey, OverlayMode mode)
        {
            if (mode == OverlayMode.Services)
            {
                return modelKey == "park" || modelKey == "plaza" || modelKey == "clinic" || modelKey == "school"
                    || modelKey == "advanced_education" || modelKey == "safety" || modelKey == "security"
                    || modelKey == "shelter" || modelKey == "deathcare";
            }

            return mode == OverlayMode.Transit
                || mode == OverlayMode.Logistics
                || mode == OverlayMode.Waste
                || mode == OverlayMode.Communications
                || mode == OverlayMode.RoadSafety
                || mode == OverlayMode.Utilities
                || mode == OverlayMode.Stormwater
                || mode == OverlayMode.Parking
                || mode == OverlayMode.LandValue;
        }

        private static bool CoverageProviderVehiclePriority(string modelKey, OverlayMode mode)
        {
            return modelKey == "clinic"
                || modelKey == "safety"
                || modelKey == "security"
                || mode == OverlayMode.Transit
                || mode == OverlayMode.Waste
                || mode == OverlayMode.RoadSafety
                || mode == OverlayMode.Utilities
                || mode == OverlayMode.Stormwater;
        }

        private Material CoverageProviderVehicleMaterial(string modelKey, OverlayMode mode)
        {
            if (modelKey == "clinic" || modelKey == "safety" || modelKey == "security" || mode == OverlayMode.RoadSafety) return trafficPulseMaterial;
            if (mode == OverlayMode.Transit) return commercialMaterial;
            if (mode == OverlayMode.Logistics || modelKey == "warehouse" || modelKey == "freight_rail") return industrialMaterial;
            if (mode == OverlayMode.Waste) return serviceNeedMaterial;
            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Communications) return utilityMaterial;
            return serviceMaterial;
        }

        private void AddProviderVehicleShadow(Vector3 center, bool horizontal)
        {
            var scale = horizontal
                ? new Vector3(0.36f, 0.012f, 0.18f)
                : new Vector3(0.18f, 0.012f, 0.36f);
            AddLooseCube(planningSignalObjects, "CoverageProviderVehicleShadow", roadMaterial, center + new Vector3(0f, -0.045f, 0f), scale);
        }

        private void AddProviderVehiclePart(string name, Material material, Vector3 center, bool horizontal, float length, float width, float height)
        {
            var scale = horizontal
                ? new Vector3(length, height, width)
                : new Vector3(width, height, length);
            AddLooseCube(planningSignalObjects, name, material, center, scale);
        }

        private void AddProviderVehicleWheels(Vector3 center, bool horizontal)
        {
            var wheelScale = horizontal
                ? new Vector3(0.052f, 0.032f, 0.033f)
                : new Vector3(0.033f, 0.032f, 0.052f);
            var front = horizontal ? new Vector3(0.15f, 0.002f, 0f) : new Vector3(0f, 0.002f, 0.15f);
            var side = horizontal ? new Vector3(0f, 0f, 0.08f) : new Vector3(0.08f, 0f, 0f);
            AddLooseCube(planningSignalObjects, "CoverageProviderVehicleWheel", roadMaterial, center - front + side, wheelScale);
            AddLooseCube(planningSignalObjects, "CoverageProviderVehicleWheel", roadMaterial, center - front - side, wheelScale);
            AddLooseCube(planningSignalObjects, "CoverageProviderVehicleWheel", roadMaterial, center + front + side, wheelScale);
            AddLooseCube(planningSignalObjects, "CoverageProviderVehicleWheel", roadMaterial, center + front - side, wheelScale);
        }

        private void AddProviderVehicleMarker(Vector3 center, bool horizontal, string modelKey, OverlayMode mode, Material accentMaterial)
        {
            var top = center + new Vector3(0f, 0.07f, 0f);
            var longScale = horizontal ? new Vector3(0.18f, 0.028f, 0.04f) : new Vector3(0.04f, 0.028f, 0.18f);
            var shortScale = horizontal ? new Vector3(0.04f, 0.03f, 0.11f) : new Vector3(0.11f, 0.03f, 0.04f);

            if (modelKey == "clinic")
            {
                AddProviderVehiclePart("CoverageProviderAmbulanceCross", trafficPulseMaterial, top, horizontal, 0.16f, 0.04f, 0.03f);
                AddProviderVehiclePart("CoverageProviderAmbulanceCross", trafficPulseMaterial, top + new Vector3(0f, 0.002f, 0f), horizontal, 0.045f, 0.14f, 0.032f);
                return;
            }

            if (modelKey == "safety" || modelKey == "security")
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderEmergencyLight", trafficPulseMaterial, top, longScale);
                AddLooseCube(planningSignalObjects, "CoverageProviderEmergencyLight", windowMaterial, top + new Vector3(0f, 0.025f, 0f), shortScale);
                return;
            }

            if (mode == OverlayMode.Transit)
            {
                AddProviderVehiclePart("CoverageProviderTransitBusStripe", roadLineMaterial, top, horizontal, 0.24f, 0.035f, 0.032f);
                return;
            }

            if (mode == OverlayMode.Logistics || mode == OverlayMode.Waste)
            {
                var rear = horizontal ? new Vector3(-0.1f, 0.02f, 0f) : new Vector3(0f, 0.02f, -0.1f);
                AddProviderVehiclePart("CoverageProviderCargoBox", accentMaterial, center + rear + new Vector3(0f, 0.06f, 0f), horizontal, 0.11f, 0.11f, 0.07f);
                return;
            }

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Communications)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderUtilityBeacon", windowMaterial, top, new Vector3(0.12f, 0.04f, 0.12f));
                AddProviderVehiclePart("CoverageProviderUtilityStripe", accentMaterial, center + new Vector3(0f, 0.05f, 0f), horizontal, 0.22f, 0.035f, 0.024f);
                return;
            }

            AddProviderVehiclePart("CoverageProviderServiceStripe", accentMaterial, top, horizontal, 0.2f, 0.035f, 0.03f);
        }

        private void AddCoverageProviderSourcePulse(Vector3 center, float span, OverlayMode mode, Material material)
        {
            // CITY_SKYLINES_SERVICE_SOURCE_PULSE links provider anchors to their surrounding coverage field.
            var y = center.y + 0.046f;
            var range = Mathf.Clamp(cellSize * span * 0.34f, cellSize * 0.18f, cellSize * 0.42f);
            var rayLength = Mathf.Clamp(cellSize * span * 0.26f, cellSize * 0.16f, cellSize * 0.32f);
            var rayMaterial = mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking
                ? trafficPulseMaterial
                : material;
            AddLooseCube(planningSignalObjects, "CoverageProviderSourcePulseCore", windowMaterial, center + new Vector3(0f, 0.032f, 0f), new Vector3(0.16f, 0.025f, 0.16f));
            AddLooseCube(planningSignalObjects, "CoverageProviderSourcePulseRay", rayMaterial, new Vector3(center.x - range, y, center.z), new Vector3(rayLength, 0.018f, 0.036f));
            AddLooseCube(planningSignalObjects, "CoverageProviderSourcePulseRay", rayMaterial, new Vector3(center.x + range, y, center.z), new Vector3(rayLength, 0.018f, 0.036f));
            AddLooseCube(planningSignalObjects, "CoverageProviderSourcePulseRay", rayMaterial, new Vector3(center.x, y, center.z - range), new Vector3(0.036f, 0.018f, rayLength));
            AddLooseCube(planningSignalObjects, "CoverageProviderSourcePulseRay", rayMaterial, new Vector3(center.x, y, center.z + range), new Vector3(0.036f, 0.018f, rayLength));

            if (mode == OverlayMode.Transit || mode == OverlayMode.Logistics || mode == OverlayMode.Services)
            {
                var diagonalScale = new Vector3(rayLength * 0.82f, 0.016f, 0.03f);
                AddLooseCubeRotated(planningSignalObjects, "CoverageProviderSourcePulseDiagonal", roadLineMaterial, center + new Vector3(range * 0.58f, 0.012f, -range * 0.58f), diagonalScale, 45f);
                AddLooseCubeRotated(planningSignalObjects, "CoverageProviderSourcePulseDiagonal", roadLineMaterial, center + new Vector3(-range * 0.58f, 0.012f, range * 0.58f), diagonalScale, 45f);
            }
        }

        private void AddCoverageProviderBudgetPulseEdge(Vector3 center, float span, OverlayMode mode, Material baseMaterial)
        {
            // CITY_SKYLINES_SERVICE_BUDGET_EDGE shows whether a provider is lean, boosted, or under fiscal pressure.
            var metrics = controller != null ? controller.Metrics : null;
            if (metrics == null || !CoverageProviderBudgetMode(mode))
            {
                return;
            }

            var leanBudget = metrics.ServiceBudgetPercent < 100;
            var boostedBudget = metrics.ServiceBudgetPercent > 100;
            var fiscalPressure = IsFiscalStress(metrics) || metrics.BudgetStress >= 58;
            if (!leanBudget && !boostedBudget && !fiscalPressure)
            {
                return;
            }

            var material = boostedBudget && !fiscalPressure ? baseMaterial : serviceNeedMaterial;
            if (fiscalPressure && (mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking || mode == OverlayMode.Traffic))
            {
                material = trafficPulseMaterial;
            }

            var radius = cellSize * Mathf.Clamp(span * 0.66f, 0.34f, 0.82f);
            var y = center.y + 0.072f;
            var thick = fiscalPressure ? 0.036f : 0.026f;
            AddLooseCube(planningSignalObjects, "CoverageProviderBudgetPulseEdge", material, new Vector3(center.x, y, center.z + radius), new Vector3(radius * 0.82f, thick, 0.044f));
            AddLooseCube(planningSignalObjects, "CoverageProviderBudgetPulseEdge", material, new Vector3(center.x, y, center.z - radius), new Vector3(radius * 0.82f, thick, 0.044f));
            AddLooseCube(planningSignalObjects, "CoverageProviderBudgetPulseEdge", material, new Vector3(center.x + radius, y + 0.006f, center.z), new Vector3(0.044f, thick, radius * 0.82f));
            AddLooseCube(planningSignalObjects, "CoverageProviderBudgetPulseEdge", material, new Vector3(center.x - radius, y + 0.006f, center.z), new Vector3(0.044f, thick, radius * 0.82f));

            if (leanBudget || fiscalPressure)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderBudgetShortfallTick", serviceNeedMaterial, center + new Vector3(-radius * 0.45f, 0.118f, radius * 0.45f), new Vector3(0.13f, 0.042f, 0.04f));
                AddLooseCube(planningSignalObjects, "CoverageProviderBudgetShortfallTick", roadLineMaterial, center + new Vector3(-radius * 0.45f, 0.16f, radius * 0.45f), new Vector3(0.09f, 0.032f, 0.035f));
                return;
            }

            AddLooseCube(planningSignalObjects, "CoverageProviderBudgetBoostGlint", roadLineMaterial, center + new Vector3(radius * 0.45f, 0.116f, -radius * 0.45f), new Vector3(0.12f, 0.035f, 0.04f));
            AddLooseCube(planningSignalObjects, "CoverageProviderBudgetBoostGlint", windowMaterial, center + new Vector3(radius * 0.45f, 0.154f, -radius * 0.45f), new Vector3(0.07f, 0.035f, 0.035f));
        }

        private static bool CoverageProviderBudgetMode(OverlayMode mode)
        {
            return mode == OverlayMode.Services
                || mode == OverlayMode.Transit
                || mode == OverlayMode.Logistics
                || mode == OverlayMode.Waste
                || mode == OverlayMode.Communications
                || mode == OverlayMode.Parking
                || mode == OverlayMode.RoadSafety
                || mode == OverlayMode.Utilities
                || mode == OverlayMode.Stormwater;
        }

        private void AddCoverageProviderRangeTicks(Vector3 center, float span, Material material)
        {
            // CITY_SKYLINES_PROVIDER_SOURCE_TICKS separate coverage sources from unmet-demand pins.
            var distance = cellSize * span * 0.52f;
            var y = center.y + 0.038f;
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeTick", material, new Vector3(center.x - distance, y, center.z), new Vector3(0.055f, 0.032f, cellSize * 0.22f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeTick", material, new Vector3(center.x + distance, y, center.z), new Vector3(0.055f, 0.032f, cellSize * 0.22f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeTick", material, new Vector3(center.x, y, center.z - distance), new Vector3(cellSize * 0.22f, 0.032f, 0.055f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeTick", material, new Vector3(center.x, y, center.z + distance), new Vector3(cellSize * 0.22f, 0.032f, 0.055f));
            var sweepScale = new Vector3(cellSize * span * 0.72f, 0.02f, 0.04f);
            AddLooseCubeRotated(planningSignalObjects, "CoverageProviderRangeSweep", material, new Vector3(center.x, y + 0.008f, center.z), sweepScale, 45f);
            AddLooseCubeRotated(planningSignalObjects, "CoverageProviderRangeSweep", material, new Vector3(center.x, y + 0.008f, center.z), sweepScale, -45f);
        }

        private void AddCoverageProviderRangeFlags(Vector3 center, float span, OverlayMode mode, Material material)
        {
            // CITY_SKYLINES_SERVICE_RANGE_FLAGS make coverage extents visible as small survey flags.
            var distance = cellSize * span * 0.6f;
            AddCoverageProviderRangeFlag(center + new Vector3(-distance, 0.065f, distance), true, mode, material);
            AddCoverageProviderRangeFlag(center + new Vector3(distance, 0.065f, -distance), false, mode, material);
        }

        private void AddCoverageProviderRangeFlag(Vector3 baseCenter, bool horizontal, OverlayMode mode, Material material)
        {
            var flagMaterial = mode == OverlayMode.Parking || mode == OverlayMode.RoadSafety || mode == OverlayMode.Traffic
                ? trafficPulseMaterial
                : material;
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeFlagPost", roadLineMaterial, baseCenter + new Vector3(0f, 0.1f, 0f), new Vector3(0.035f, 0.22f, 0.035f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeFlag", flagMaterial, baseCenter + new Vector3(0f, 0.21f, 0f), horizontal ? new Vector3(cellSize * 0.18f, 0.065f, 0.032f) : new Vector3(0.032f, 0.065f, cellSize * 0.18f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeFlagTip", windowMaterial, baseCenter + new Vector3(0f, 0.255f, 0f), new Vector3(0.07f, 0.035f, 0.07f));
        }

        private void AddCoverageProviderRangeBadge(Vector3 center, float span, OverlayMode mode, Material material)
        {
            // CITY_SKYLINES_SERVICE_RANGE_BADGE makes provider influence sources visible at a glance.
            var distance = cellSize * span * 0.44f;
            var badgeCenter = center + new Vector3(distance, 0.07f, -distance);
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadge", roadLineMaterial, badgeCenter, new Vector3(0.18f, 0.032f, 0.18f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadgeCore", material, badgeCenter + new Vector3(0f, 0.034f, 0f), new Vector3(0.11f, 0.03f, 0.11f));

            if (mode == OverlayMode.Transit || mode == OverlayMode.Logistics)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadgeRoute", windowMaterial, badgeCenter + new Vector3(0f, 0.07f, -0.035f), new Vector3(0.15f, 0.024f, 0.026f));
                AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadgeRoute", windowMaterial, badgeCenter + new Vector3(0f, 0.07f, 0.035f), new Vector3(0.15f, 0.024f, 0.026f));
                return;
            }

            if (mode == OverlayMode.Stormwater || mode == OverlayMode.Utilities)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadgeDrop", windowMaterial, badgeCenter + new Vector3(0f, 0.075f, 0f), new Vector3(0.09f, 0.045f, 0.09f));
                return;
            }

            AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadgeDot", windowMaterial, badgeCenter + new Vector3(-0.04f, 0.075f, -0.04f), new Vector3(0.045f, 0.035f, 0.045f));
            AddLooseCube(planningSignalObjects, "CoverageProviderRangeBadgeDot", windowMaterial, badgeCenter + new Vector3(0.04f, 0.075f, 0.04f), new Vector3(0.045f, 0.035f, 0.045f));
        }

        private void AddCoverageProviderRadiusPetals(Vector3 center, float span, OverlayMode mode, Material material)
        {
            // CITY_SKYLINES_COVERAGE_RADIUS_PETALS make service influence read as a clean field, not only a pin.
            var distance = cellSize * span * 0.34f;
            var y = center.y + 0.064f;
            var petalMaterial = mode == OverlayMode.Services || mode == OverlayMode.LandValue
                ? serviceNeedMaterial
                : material;
            var horizontalScale = new Vector3(cellSize * span * 0.34f, 0.018f, 0.035f);
            var verticalScale = new Vector3(0.035f, 0.018f, cellSize * span * 0.34f);
            AddLooseCube(planningSignalObjects, "CoverageRadiusPetal", petalMaterial, new Vector3(center.x, y, center.z - distance), horizontalScale);
            AddLooseCube(planningSignalObjects, "CoverageRadiusPetal", petalMaterial, new Vector3(center.x, y, center.z + distance), horizontalScale);
            AddLooseCube(planningSignalObjects, "CoverageRadiusPetal", petalMaterial, new Vector3(center.x - distance, y, center.z), verticalScale);
            AddLooseCube(planningSignalObjects, "CoverageRadiusPetal", petalMaterial, new Vector3(center.x + distance, y, center.z), verticalScale);

            if (mode == OverlayMode.Stormwater || mode == OverlayMode.Utilities || mode == OverlayMode.Transit)
            {
                AddLooseCube(planningSignalObjects, "CoverageRadiusPetalCore", windowMaterial, center + new Vector3(0f, 0.072f, 0f), new Vector3(cellSize * span * 0.26f, 0.016f, 0.03f));
            }
        }

        private void AddCoverageProviderModeGlyph(Vector3 center, OverlayMode mode, Material material)
        {
            var glyphCenter = center + new Vector3(0f, 0.39f, 0f);
            if (mode == OverlayMode.Services || mode == OverlayMode.LandValue)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderServicePlus", windowMaterial, glyphCenter, new Vector3(0.26f, 0.035f, 0.07f));
                AddLooseCube(planningSignalObjects, "CoverageProviderServicePlus", windowMaterial, glyphCenter, new Vector3(0.07f, 0.035f, 0.26f));
                return;
            }

            if (mode == OverlayMode.Transit || mode == OverlayMode.Logistics)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderTransitTrack", roadLineMaterial, glyphCenter + new Vector3(0f, 0f, -0.085f), new Vector3(0.32f, 0.032f, 0.04f));
                AddLooseCube(planningSignalObjects, "CoverageProviderTransitTrack", roadLineMaterial, glyphCenter + new Vector3(0f, 0f, 0.085f), new Vector3(0.32f, 0.032f, 0.04f));
                return;
            }

            if (mode == OverlayMode.Waste)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderWasteBin", material, glyphCenter + new Vector3(0f, 0.025f, 0f), new Vector3(0.18f, 0.12f, 0.16f));
                AddLooseCube(planningSignalObjects, "CoverageProviderWasteLid", roadLineMaterial, glyphCenter + new Vector3(0f, 0.105f, 0f), new Vector3(0.22f, 0.032f, 0.18f));
                return;
            }

            if (mode == OverlayMode.Communications)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderCommsMast", material, glyphCenter + new Vector3(0f, 0.07f, 0f), new Vector3(0.05f, 0.19f, 0.05f));
                AddLooseCube(planningSignalObjects, "CoverageProviderCommsHead", windowMaterial, glyphCenter + new Vector3(0f, 0.17f, 0f), new Vector3(0.24f, 0.04f, 0.05f));
                return;
            }

            if (mode == OverlayMode.Parking)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderParkingP", roadLineMaterial, glyphCenter + new Vector3(-0.045f, 0.04f, 0f), new Vector3(0.055f, 0.14f, 0.05f));
                AddLooseCube(planningSignalObjects, "CoverageProviderParkingP", roadLineMaterial, glyphCenter + new Vector3(0.055f, 0.09f, 0f), new Vector3(0.16f, 0.045f, 0.05f));
                return;
            }

            if (mode == OverlayMode.RoadSafety || mode == OverlayMode.Traffic)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderRoadWrench", roadLineMaterial, glyphCenter, new Vector3(0.28f, 0.035f, 0.055f));
                AddLooseCube(planningSignalObjects, "CoverageProviderRoadWrench", roadLineMaterial, glyphCenter + new Vector3(0.09f, 0.028f, 0f), new Vector3(0.08f, 0.035f, 0.16f));
                return;
            }

            if (mode == OverlayMode.Stormwater || mode == OverlayMode.Utilities)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderUtilityDrop", windowMaterial, glyphCenter, new Vector3(0.18f, 0.055f, 0.18f));
                AddLooseCube(planningSignalObjects, "CoverageProviderUtilityPipe", roadLineMaterial, glyphCenter + new Vector3(0f, 0.05f, 0f), new Vector3(0.28f, 0.035f, 0.055f));
                return;
            }

            if (mode == OverlayMode.Pollution)
            {
                AddLooseCube(planningSignalObjects, "CoverageProviderPollutionNode", trafficPulseMaterial, glyphCenter + new Vector3(-0.04f, 0.05f, 0f), new Vector3(0.07f, 0.14f, 0.07f));
                AddLooseCube(planningSignalObjects, "CoverageProviderPollutionFilter", windowMaterial, glyphCenter + new Vector3(0.08f, 0.11f, 0f), new Vector3(0.14f, 0.06f, 0.14f));
            }
        }

        private Material CoverageProviderMaterial(OverlayMode mode, string modelKey)
        {
            if (mode == OverlayMode.Services || mode == OverlayMode.LandValue) return serviceMaterial;
            if (mode == OverlayMode.Transit || mode == OverlayMode.Communications || mode == OverlayMode.Stormwater || mode == OverlayMode.Utilities) return windowMaterial;
            if (mode == OverlayMode.Logistics || mode == OverlayMode.Waste || mode == OverlayMode.Parking) return serviceNeedMaterial;
            if (mode == OverlayMode.Pollution && (modelKey == "industrial" || modelKey == "resource" || modelKey == "waste_to_energy")) return trafficPulseMaterial;
            if (mode == OverlayMode.RoadSafety) return trafficPulseMaterial;
            return serviceNeedMaterial;
        }

        private static bool CoverageProviderMatchesMode(string modelKey, BuildingDefinition definition, OverlayMode mode)
        {
            if (string.IsNullOrEmpty(modelKey))
            {
                return false;
            }

            if (mode == OverlayMode.Services)
            {
                return modelKey == "park" || modelKey == "plaza" || modelKey == "clinic" || modelKey == "school"
                    || modelKey == "advanced_education" || modelKey == "safety"
                    || modelKey == "security" || modelKey == "shelter" || modelKey == "deathcare";
            }

            if (mode == OverlayMode.Transit) return modelKey == "transit" || modelKey == "intercity";
            if (mode == OverlayMode.Logistics) return modelKey == "logistics" || modelKey == "warehouse" || modelKey == "freight_rail" || modelKey == "resource";
            if (mode == OverlayMode.Waste) return modelKey == "recycling" || modelKey == "waste_to_energy";
            if (mode == OverlayMode.Communications) return modelKey == "communications" || modelKey == "mail";
            if (mode == OverlayMode.Parking) return modelKey == "parking";
            if (mode == OverlayMode.RoadSafety) return modelKey == "road_maintenance";
            if (mode == OverlayMode.Utilities)
            {
                return definition != null
                    && definition.Category == BuildingCategory.Utility
                    && (definition.PowerOutput > 0 || definition.WaterOutput > 0);
            }
            if (mode == OverlayMode.Stormwater) return modelKey == "stormwater" || modelKey == "water" || modelKey == "sewage";
            if (mode == OverlayMode.Pollution) return modelKey == "industrial" || modelKey == "resource" || modelKey == "waste_to_energy" || modelKey == "park" || modelKey == "stormwater";
            if (mode == OverlayMode.LandValue)
            {
                return modelKey == "park" || modelKey == "plaza" || modelKey == "landmark" || modelKey == "administration"
                    || (definition != null && definition.ServiceValue >= 20 && definition.ServiceRadius >= 10);
            }

            return false;
        }

        private void AddServiceGapPin(GridPos pos, OverlayMode mode, float height)
        {
            // CITY_BUILDER_SERVICE_GAP_PIN_STYLE makes diagnostic pins read as compact map markers.
            var material = ServiceGapPinMaterial(mode);
            var baseCenter = CellCenter(pos, roadHeight + 0.09f);
            AddServiceGapImpactHalo(pos, mode, material, height);
            AddServiceGapCoverageCallout(pos, mode, material, height);
            AddLooseCube(planningSignalObjects, "ServiceGapPinBase", material, baseCenter, new Vector3(0.3f, 0.055f, 0.3f));
            AddServiceGapGroundLocator(pos, mode, material, height);
            AddServiceGapNeedBracket(pos, mode, material, height);
            AddServiceGapUnservedMarker(pos, mode, material, height);
            AddServiceGapBudgetShortfallMarker(pos, mode, material, height);
            AddLooseCube(planningSignalObjects, "ServiceGapPin", material, CellCenter(pos, roadHeight + height * 0.5f + 0.12f), new Vector3(0.12f, height, 0.12f));
            AddLooseCube(planningSignalObjects, "ServiceGapPinCap", material, CellCenter(pos, roadHeight + height + 0.19f), new Vector3(0.22f, 0.07f, 0.22f));
            AddServiceGapModeCue(pos, mode, material);
            AddServiceGapDispatchCard(pos, mode, material, height);
        }

        private void AddServiceGapDispatchCard(GridPos pos, OverlayMode mode, Material material, float height)
        {
            var center = CellCenter(pos, roadHeight + Mathf.Clamp(height, 0.3f, 0.72f) + 0.2f) + new Vector3(cellSize * 0.26f, 0f, -cellSize * 0.24f);
            var cardMaterial = mode == OverlayMode.Services || mode == OverlayMode.LandValue ? serviceNeedMaterial : material;
            AddLooseCube(planningSignalObjects, "ServiceGapDispatchCardShadow", buildingFootprintMaterial, center + new Vector3(0.035f, -0.04f, 0.035f), new Vector3(cellSize * 0.28f, 0.02f, cellSize * 0.18f));
            AddLooseCube(planningSignalObjects, "ServiceGapDispatchCard", cardMaterial, center, new Vector3(cellSize * 0.25f, 0.046f, cellSize * 0.16f));
            AddLooseCube(planningSignalObjects, "ServiceGapDispatchCardLine", roadLineMaterial, center + new Vector3(0f, 0.04f, -cellSize * 0.03f), new Vector3(cellSize * 0.16f, 0.017f, 0.024f));
            AddLooseCube(planningSignalObjects, "ServiceGapDispatchCardLine", windowMaterial, center + new Vector3(0f, 0.062f, cellSize * 0.03f), new Vector3(cellSize * 0.11f, 0.016f, 0.022f));
            AddLooseCube(planningSignalObjects, "ServiceGapDispatchNeedDot", trafficPulseMaterial, center + new Vector3(cellSize * 0.14f, 0.082f, 0f), new Vector3(0.056f, 0.046f, 0.056f));
        }

        private void AddServiceGapGroundLocator(GridPos pos, OverlayMode mode, Material material, float height)
        {
            // CITY_SKYLINES_SERVICE_GAP_GROUND_LOCATOR shows the exact affected ground tile under the pin.
            var center = CellCenter(pos, roadHeight + 0.056f);
            var span = Mathf.Clamp(0.28f + height * 0.42f, 0.34f, 0.62f);
            var locatorMaterial = ServiceGapLocatorMaterial(mode, material);
            AddLooseCube(planningSignalObjects, "ServiceGapGroundLocatorPad", locatorMaterial, center, new Vector3(cellSize * span, 0.016f, cellSize * span));
            AddLooseCube(planningSignalObjects, "ServiceGapGroundLocatorNorth", roadLineMaterial, center + new Vector3(0f, 0.024f, cellSize * span * 0.5f), new Vector3(cellSize * 0.2f, 0.018f, 0.03f));
            AddLooseCube(planningSignalObjects, "ServiceGapGroundLocatorSouth", roadLineMaterial, center + new Vector3(0f, 0.024f, -cellSize * span * 0.5f), new Vector3(cellSize * 0.2f, 0.018f, 0.03f));
            AddLooseCube(planningSignalObjects, "ServiceGapGroundLocatorEast", roadLineMaterial, center + new Vector3(cellSize * span * 0.5f, 0.027f, 0f), new Vector3(0.03f, 0.018f, cellSize * 0.2f));
            AddLooseCube(planningSignalObjects, "ServiceGapGroundLocatorWest", roadLineMaterial, center + new Vector3(-cellSize * span * 0.5f, 0.027f, 0f), new Vector3(0.03f, 0.018f, cellSize * 0.2f));
            AddServiceGapNearestRoadTether(pos, center, mode, locatorMaterial);

            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater)
            {
                AddUtilityGapGroundLocatorDetails(center, mode, height);
                return;
            }

            AddServiceGapDemandFootprint(center, mode, height);
        }

        private Material ServiceGapLocatorMaterial(OverlayMode mode, Material fallback)
        {
            if (mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater || mode == OverlayMode.Transit)
            {
                return windowMaterial;
            }

            if (mode == OverlayMode.Services || mode == OverlayMode.LandValue)
            {
                return serviceNeedMaterial;
            }

            return fallback;
        }

        private void AddServiceGapNearestRoadTether(GridPos pos, Vector3 center, OverlayMode mode, Material material)
        {
            var direction = Vector3.zero;
            if (HasRoadTile(pos.X, pos.Y - 1)) direction = Vector3.back;
            else if (HasRoadTile(pos.X, pos.Y + 1)) direction = Vector3.forward;
            else if (HasRoadTile(pos.X - 1, pos.Y)) direction = Vector3.left;
            else if (HasRoadTile(pos.X + 1, pos.Y)) direction = Vector3.right;

            if (direction == Vector3.zero)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapUnlinkedGroundNode", mode == OverlayMode.Utilities ? windowMaterial : serviceNeedMaterial, center + new Vector3(0f, 0.048f, 0f), new Vector3(0.09f, 0.045f, 0.09f));
                return;
            }

            var horizontal = Mathf.Abs(direction.x) > 0.01f;
            var tetherCenter = center + direction * cellSize * 0.27f + new Vector3(0f, 0.042f, 0f);
            var tetherScale = horizontal
                ? new Vector3(cellSize * 0.3f, 0.018f, 0.034f)
                : new Vector3(0.034f, 0.018f, cellSize * 0.3f);
            AddLooseCube(planningSignalObjects, "ServiceGapRoadTether", material, tetherCenter, tetherScale);
            AddLooseCube(planningSignalObjects, "ServiceGapRoadTetherNode", roadLineMaterial, center + direction * cellSize * 0.42f + new Vector3(0f, 0.068f, 0f), new Vector3(0.075f, 0.042f, 0.075f));
        }

        private void AddServiceGapDemandFootprint(Vector3 center, OverlayMode mode, float height)
        {
            var demandMaterial = mode == OverlayMode.Services || mode == OverlayMode.LandValue ? serviceNeedMaterial : roadLineMaterial;
            var span = Mathf.Clamp(cellSize * (0.18f + height * 0.18f), cellSize * 0.2f, cellSize * 0.34f);
            AddLooseCube(planningSignalObjects, "ServiceGapDemandFootprintLine", demandMaterial, center + new Vector3(-span * 0.5f, 0.052f, -span * 0.32f), new Vector3(span, 0.018f, 0.028f));
            AddLooseCube(planningSignalObjects, "ServiceGapDemandFootprintLine", demandMaterial, center + new Vector3(-span * 0.32f, 0.054f, -span * 0.5f), new Vector3(0.028f, 0.018f, span));
            AddLooseCube(planningSignalObjects, "ServiceGapDemandFootprintNode", roadLineMaterial, center + new Vector3(span * 0.28f, 0.07f, span * 0.28f), new Vector3(0.07f, 0.052f, 0.07f));
        }

        private void AddUtilityGapGroundLocatorDetails(Vector3 center, OverlayMode mode, float height)
        {
            // CITY_SKYLINES_UTILITY_GAP_GROUND_DETAILS distinguish pipe, water, and resilience gaps on the ground.
            var span = Mathf.Clamp(cellSize * (0.28f + height * 0.2f), cellSize * 0.28f, cellSize * 0.46f);
            AddLooseCube(planningSignalObjects, "UtilityGapGroundPipeRun", roadLineMaterial, center + new Vector3(0f, 0.058f, 0f), new Vector3(span, 0.02f, 0.042f));
            AddLooseCube(planningSignalObjects, "UtilityGapGroundPipeRun", roadLineMaterial, center + new Vector3(-span * 0.24f, 0.062f, 0f), new Vector3(0.042f, 0.02f, span * 0.62f));
            AddLooseCube(planningSignalObjects, "UtilityGapGroundNode", windowMaterial, center + new Vector3(span * 0.32f, 0.086f, 0f), new Vector3(0.085f, 0.07f, 0.085f));

            if (mode == OverlayMode.Stormwater)
            {
                AddLooseCube(planningSignalObjects, "UtilityGapStormwaterBasin", windowMaterial, center + new Vector3(-span * 0.22f, 0.084f, span * 0.22f), new Vector3(0.14f, 0.035f, 0.1f));
                AddLooseCube(planningSignalObjects, "UtilityGapRunoffArrow", trafficPulseMaterial, center + new Vector3(span * 0.08f, 0.098f, -span * 0.22f), new Vector3(0.18f, 0.02f, 0.034f));
                return;
            }

            AddLooseCube(planningSignalObjects, "UtilityGapReliabilityChip", utilityMaterial, center + new Vector3(-span * 0.32f, 0.098f, span * 0.16f), new Vector3(0.11f, 0.052f, 0.075f));
            AddLooseCube(planningSignalObjects, "UtilityGapReliabilitySpark", trafficPulseMaterial, center + new Vector3(-span * 0.17f, 0.13f, span * 0.16f), new Vector3(0.055f, 0.065f, 0.055f));
        }

        private void AddServiceGapNeedBracket(GridPos pos, OverlayMode mode, Material material, float height)
        {
            // CITY_SKYLINES_UNMET_NEED_BRACKET makes gap markers read as demand hotspots, not providers.
            var center = CellCenter(pos, roadHeight + 0.11f);
            var span = Mathf.Clamp(0.24f + height * 0.34f, 0.3f, 0.58f);
            var bracketMaterial = mode == OverlayMode.Services || mode == OverlayMode.LandValue ? serviceNeedMaterial : material;
            AddLooseCube(planningSignalObjects, "ServiceGapNeedBracket", bracketMaterial, center + new Vector3(-cellSize * span, 0f, -cellSize * span), new Vector3(cellSize * 0.2f, 0.034f, 0.045f));
            AddLooseCube(planningSignalObjects, "ServiceGapNeedBracket", bracketMaterial, center + new Vector3(-cellSize * span, 0f, -cellSize * span), new Vector3(0.045f, 0.034f, cellSize * 0.2f));
            AddLooseCube(planningSignalObjects, "ServiceGapNeedBracket", bracketMaterial, center + new Vector3(cellSize * span, 0f, cellSize * span), new Vector3(cellSize * 0.2f, 0.034f, 0.045f));
            AddLooseCube(planningSignalObjects, "ServiceGapNeedBracket", bracketMaterial, center + new Vector3(cellSize * span, 0f, cellSize * span), new Vector3(0.045f, 0.034f, cellSize * 0.2f));
        }

        private void AddServiceGapUnservedMarker(GridPos pos, OverlayMode mode, Material material, float height)
        {
            // CITY_SKYLINES_UNSERVED_DEMAND_MARK makes gap pins read as demand problems instead of service sources.
            var center = CellCenter(pos, roadHeight + Mathf.Clamp(height, 0.34f, 0.78f) + 0.32f);
            var markMaterial = mode == OverlayMode.Services || mode == OverlayMode.LandValue
                ? serviceNeedMaterial
                : (mode == OverlayMode.Transit || mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater ? windowMaterial : material);
            AddLooseCube(planningSignalObjects, "ServiceGapUnservedStem", markMaterial, center, new Vector3(0.055f, 0.18f, 0.055f));
            AddLooseCube(planningSignalObjects, "ServiceGapUnservedDot", roadLineMaterial, center + new Vector3(0f, 0.16f, 0f), new Vector3(0.12f, 0.055f, 0.12f));
            AddLooseCubeRotated(planningSignalObjects, "ServiceGapUnservedTick", markMaterial, center + new Vector3(-cellSize * 0.18f, -0.08f, cellSize * 0.16f), new Vector3(cellSize * 0.26f, 0.022f, 0.035f), -35f);
            AddLooseCubeRotated(planningSignalObjects, "ServiceGapUnservedTick", markMaterial, center + new Vector3(cellSize * 0.18f, -0.08f, -cellSize * 0.16f), new Vector3(cellSize * 0.26f, 0.022f, 0.035f), -35f);
        }

        private void AddServiceGapBudgetShortfallMarker(GridPos pos, OverlayMode mode, Material material, float height)
        {
            var metrics = controller != null ? controller.Metrics : null;
            if (metrics == null || metrics.ServiceBudgetPercent >= 100 || !CoverageProviderBudgetMode(mode))
            {
                return;
            }

            var center = CellCenter(pos, roadHeight + Mathf.Clamp(height, 0.34f, 0.78f) + 0.53f);
            var accent = metrics.BudgetStress >= 58 ? trafficPulseMaterial : serviceNeedMaterial;
            var baseMaterial = mode == OverlayMode.Transit || mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater ? windowMaterial : material;
            AddLooseCube(planningSignalObjects, "ServiceGapBudgetShortfallPlate", baseMaterial, center, new Vector3(0.22f, 0.042f, 0.14f));
            AddLooseCube(planningSignalObjects, "ServiceGapBudgetShortfallLine", roadLineMaterial, center + new Vector3(0f, 0.052f, -0.035f), new Vector3(0.15f, 0.023f, 0.026f));

            var pipCount = metrics.ServiceBudgetPercent <= 75 ? 3 : 2;
            for (var i = 0; i < pipCount; i += 1)
            {
                var x = (i - (pipCount - 1) * 0.5f) * 0.064f;
                AddLooseCube(planningSignalObjects, "ServiceGapBudgetShortfallPip", i == pipCount - 1 ? accent : serviceNeedMaterial, center + new Vector3(x, 0.087f + i * 0.008f, 0.04f), new Vector3(0.042f, 0.044f + i * 0.012f, 0.038f));
            }
        }

        private void AddServiceGapImpactHalo(GridPos pos, OverlayMode mode, Material material, float height)
        {
            // CITY_SKYLINES_COVERAGE_PIN_HALO gives diagnostic pins a readable footprint without changing coverage logic.
            var center = CellCenter(pos, roadHeight + 0.074f);
            var span = Mathf.Clamp(0.34f + height * 0.55f, 0.38f, 0.72f);
            if (mode == OverlayMode.Traffic || mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapDirectionalHalo", material, center, new Vector3(cellSize * span, 0.018f, cellSize * 0.06f));
                AddLooseCube(planningSignalObjects, "ServiceGapDirectionalHalo", material, center, new Vector3(cellSize * 0.06f, 0.018f, cellSize * span));
                return;
            }

            if (mode == OverlayMode.Stormwater || mode == OverlayMode.Utilities)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapUtilityHalo", windowMaterial, center, new Vector3(cellSize * span, 0.018f, cellSize * span * 0.68f));
                return;
            }

            AddLooseCube(planningSignalObjects, "ServiceGapImpactHalo", material, center, new Vector3(cellSize * span, 0.016f, cellSize * span));
        }

        private void AddServiceGapCoverageCallout(GridPos pos, OverlayMode mode, Material material, float height)
        {
            // CITY_SKYLINES_SERVICE_GAP_CALLOUT separates uncovered demand from covered source halos.
            var center = CellCenter(pos, roadHeight + 0.16f);
            var span = Mathf.Clamp(0.24f + height * 0.22f, 0.3f, 0.54f);
            var accent = mode == OverlayMode.Transit || mode == OverlayMode.Utilities || mode == OverlayMode.Stormwater
                ? windowMaterial
                : roadLineMaterial;
            AddLooseCube(planningSignalObjects, "ServiceGapCalloutNorth", material, center + new Vector3(0f, 0.004f, cellSize * span), new Vector3(cellSize * 0.22f, 0.024f, 0.04f));
            AddLooseCube(planningSignalObjects, "ServiceGapCalloutEast", material, center + new Vector3(cellSize * span, 0.006f, 0f), new Vector3(0.04f, 0.024f, cellSize * 0.22f));
            AddLooseCube(planningSignalObjects, "ServiceGapCalloutBlink", accent, center + new Vector3(-cellSize * span * 0.52f, 0.035f, -cellSize * span * 0.52f), new Vector3(0.08f, 0.052f, 0.08f));
        }

        private Material ServiceGapPinMaterial(OverlayMode mode)
        {
            if (mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking || mode == OverlayMode.Traffic)
            {
                return trafficPulseMaterial;
            }

            if (mode == OverlayMode.Stormwater || mode == OverlayMode.Utilities || mode == OverlayMode.Transit)
            {
                return windowMaterial;
            }

            return serviceNeedMaterial;
        }

        private void AddServiceGapModeCue(GridPos pos, OverlayMode mode, Material material)
        {
            // CITY_SKYLINES_LAYER_PIN_CUES gives coverage pins a readable mode-specific footing.
            var center = CellCenter(pos, roadHeight + 0.135f);
            if (mode == OverlayMode.Transit)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapTransitTrackCue", roadLineMaterial, center + new Vector3(0f, 0f, -0.11f), new Vector3(0.32f, 0.026f, 0.035f));
                AddLooseCube(planningSignalObjects, "ServiceGapTransitTrackCue", roadLineMaterial, center + new Vector3(0f, 0f, 0.11f), new Vector3(0.32f, 0.026f, 0.035f));
                return;
            }

            if (mode == OverlayMode.Stormwater)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapWaterBaseCue", windowMaterial, center, new Vector3(0.38f, 0.022f, 0.22f));
                return;
            }

            if (mode == OverlayMode.Logistics)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapCargoBoxCue", serviceNeedMaterial, center + new Vector3(-0.07f, 0.025f, 0f), new Vector3(0.14f, 0.08f, 0.14f));
                AddLooseCube(planningSignalObjects, "ServiceGapCargoBoxCue", material, center + new Vector3(0.08f, 0.045f, 0.02f), new Vector3(0.16f, 0.11f, 0.12f));
                return;
            }

            if (mode == OverlayMode.Waste)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapWasteBinCue", material, center, new Vector3(0.16f, 0.16f, 0.14f));
                AddLooseCube(planningSignalObjects, "ServiceGapWasteLidCue", roadLineMaterial, center + new Vector3(0f, 0.105f, 0f), new Vector3(0.2f, 0.035f, 0.17f));
                return;
            }

            if (mode == OverlayMode.Communications)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapAntennaMastCue", material, center + new Vector3(0f, 0.09f, 0f), new Vector3(0.045f, 0.18f, 0.045f));
                AddLooseCube(planningSignalObjects, "ServiceGapAntennaHeadCue", roadLineMaterial, center + new Vector3(0f, 0.19f, 0f), new Vector3(0.22f, 0.035f, 0.045f));
                return;
            }

            if (mode == OverlayMode.Pollution)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapPollutionStackCue", material, center + new Vector3(-0.06f, 0.08f, 0f), new Vector3(0.06f, 0.16f, 0.06f));
                AddLooseCube(planningSignalObjects, "ServiceGapPollutionPuffCue", trafficPulseMaterial, center + new Vector3(0.08f, 0.18f, 0f), new Vector3(0.14f, 0.08f, 0.14f));
                return;
            }

            if (mode == OverlayMode.LandValue)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapLandValuePlaqueCue", serviceNeedMaterial, center, new Vector3(0.24f, 0.05f, 0.24f));
                AddLooseCube(planningSignalObjects, "ServiceGapLandValueSparkCue", roadLineMaterial, center + new Vector3(0f, 0.075f, 0f), new Vector3(0.12f, 0.08f, 0.12f));
                return;
            }

            if (mode == OverlayMode.Utilities)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapUtilityNodeCue", windowMaterial, center, new Vector3(0.24f, 0.055f, 0.16f));
                AddLooseCube(planningSignalObjects, "ServiceGapUtilityPoleCue", material, center + new Vector3(0f, 0.095f, 0f), new Vector3(0.055f, 0.16f, 0.055f));
                return;
            }

            if (mode == OverlayMode.RoadSafety || mode == OverlayMode.Parking || mode == OverlayMode.Traffic)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapTrafficCue", material, center, new Vector3(0.34f, 0.026f, 0.055f));
                if (mode == OverlayMode.Parking)
                {
                    AddLooseCube(planningSignalObjects, "ServiceGapParkingBlockCue", roadLineMaterial, center + new Vector3(0.08f, 0.055f, 0f), new Vector3(0.08f, 0.07f, 0.08f));
                }

                return;
            }

            if (mode == OverlayMode.Services)
            {
                AddLooseCube(planningSignalObjects, "ServiceGapCrossCue", material, center, new Vector3(0.28f, 0.034f, 0.07f));
                AddLooseCube(planningSignalObjects, "ServiceGapCrossCue", material, center, new Vector3(0.07f, 0.034f, 0.28f));
            }
        }

        private static bool NeedsCoverageSignal(TileData tile, OverlayMode mode, CityMetrics metrics)
        {
            // LAYER_GAP_PIN_SIGNALS expands the diagnostic pins across existing information layers.
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return false;
            }

            var roadTile = !string.IsNullOrEmpty(tile.RoadId);
            var occupiedOrZoned = !string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None;
            if (mode == OverlayMode.RoadSafety)
            {
                return (roadTile || occupiedOrZoned) && tile.RoadMaintenanceAccess < 24 && (tile.Traffic > 0 || (metrics != null && metrics.AccidentRisk > 48));
            }

            if (roadTile)
            {
                return false;
            }

            if (!occupiedOrZoned)
            {
                return false;
            }

            if (mode == OverlayMode.Services) return ServiceAccessValue(tile) < 26;
            if (mode == OverlayMode.Transit) return tile.TransitAccess < 24 && tile.Traffic >= 8;
            if (mode == OverlayMode.Logistics) return tile.LogisticsAccess < 24 && tile.Traffic >= 8;
            if (mode == OverlayMode.Waste) return tile.WasteAccess < 24;
            if (mode == OverlayMode.Communications) return Mathf.Max(tile.CommunicationAccess, tile.MailAccess) < 24;
            if (mode == OverlayMode.Parking) return tile.ParkingAccess < 24 && (tile.Traffic >= 8 || IsParkingSensitiveUse(tile));
            if (mode == OverlayMode.Pollution) return PollutionStress(tile) >= (IsPollutionSensitiveUse(tile) ? 24 : 42);
            if (mode == OverlayMode.LandValue) return tile.LandValue < LandValueSignalThreshold(metrics);
            if (mode == OverlayMode.Utilities) return IsUtilityStress(metrics);
            if (mode == OverlayMode.Stormwater) return tile.StormwaterAccess < 24 || IsStormwaterStress(metrics);
            return false;
        }

        private static bool IsDevelopedMapTile(TileData tile)
        {
            return tile != null
                && tile.Terrain != TerrainType.Water
                && (!string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None);
        }

        private static float CoverageSignalHeight(TileData tile, OverlayMode mode, CityMetrics metrics)
        {
            var score = 28;
            if (mode == OverlayMode.Services) score = 42 - ServiceAccessValue(tile);
            else if (mode == OverlayMode.Transit) score = 42 - tile.TransitAccess + tile.Traffic / 3;
            else if (mode == OverlayMode.Logistics) score = 42 - tile.LogisticsAccess + tile.Traffic / 3;
            else if (mode == OverlayMode.Waste) score = 42 - tile.WasteAccess;
            else if (mode == OverlayMode.Communications) score = 42 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess);
            else if (mode == OverlayMode.Parking) score = 42 - tile.ParkingAccess + tile.Traffic / 3;
            else if (mode == OverlayMode.RoadSafety) score = 42 - tile.RoadMaintenanceAccess + tile.Traffic / 4;
            else if (mode == OverlayMode.Pollution) score = PollutionStress(tile);
            else if (mode == OverlayMode.LandValue) score = LandValueSignalThreshold(metrics) - tile.LandValue;
            else if (mode == OverlayMode.Utilities && metrics != null) score = Mathf.Max(Mathf.Max(95 - metrics.UtilityReliability, metrics.UtilityUtilization - 95), metrics.WastewaterUtilization - 95);
            else if (mode == OverlayMode.Stormwater) score = Mathf.Max(42 - tile.StormwaterAccess, metrics != null ? Mathf.Max(metrics.FloodRisk, 70 - metrics.StormwaterResilience) : 0);
            return 0.26f + Mathf.Clamp(score, 0, 90) * 0.004f;
        }

        private static bool IsParkingSensitiveUse(TileData tile)
        {
            return tile.Zone == ZoneType.Commercial || tile.Zone == ZoneType.Office || tile.Zone == ZoneType.MixedUse || tile.Zone == ZoneType.Civic;
        }

        private static bool IsPollutionSensitiveUse(TileData tile)
        {
            return tile.Zone == ZoneType.Residential || tile.Zone == ZoneType.MixedUse || tile.Zone == ZoneType.Office || tile.Zone == ZoneType.Civic;
        }

        private static int PollutionStress(TileData tile)
        {
            return tile.Pollution + Mathf.Max(0, tile.Noise - 10);
        }

        private static int LandValueSignalThreshold(CityMetrics metrics)
        {
            return metrics != null && (metrics.DevelopmentQuality < 52 || metrics.BuildingUpgradeBlockedCount > 0) ? 45 : 36;
        }

        private static bool IsUtilityStress(CityMetrics metrics)
        {
            return metrics != null && (metrics.UtilityReliability < 95 || metrics.UtilityUtilization > 115 || metrics.WastewaterUtilization > 115 || metrics.FloodRisk > 55);
        }

        private static bool IsStormwaterStress(CityMetrics metrics)
        {
            return metrics != null && (metrics.StormwaterResilience < 62 || metrics.StormwaterUtilization > 110 || metrics.FloodRisk > 55);
        }

        private static bool IsFiscalStress(CityMetrics metrics)
        {
            return FiscalIssueSeverity(metrics) >= 18;
        }

        private static int FiscalIssueSeverity(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            var severity = Mathf.Max(0, metrics.BudgetStress - 30);
            if (metrics.NetIncome < 0)
            {
                severity = Mathf.Max(severity, 18 + Mathf.Min(38, -metrics.NetIncome / 80));
            }

            if (metrics.CashRunwayDays > 0 && metrics.CashRunwayDays <= 45)
            {
                severity = Mathf.Max(severity, 58 - metrics.CashRunwayDays);
            }

            return Mathf.Clamp(severity, 0, 70);
        }

        private static int ServiceAccessValue(TileData tile)
        {
            return Mathf.Max(tile.ParkAccess, Mathf.Max(tile.HealthAccess, Mathf.Max(tile.DeathcareAccess, Mathf.Max(tile.EducationAccess, Mathf.Max(Mathf.Max(tile.SafetyAccess, tile.FireProtectionAccess), tile.SecurityAccess)))));
        }

        private static int CityIssueSeverity(TileData tile, CityMetrics metrics)
        {
            if (tile == null || tile.Terrain == TerrainType.Water)
            {
                return 0;
            }

            var severity = 0;
            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                severity = Mathf.Max(severity, tile.Traffic - 48);
                severity = Mathf.Max(severity, 34 - tile.RoadMaintenanceAccess);
                return Mathf.Max(0, severity);
            }

            var occupiedOrZoned = !string.IsNullOrEmpty(tile.BuildingId) || tile.Zone != ZoneType.None;
            if (!occupiedOrZoned)
            {
                return 0;
            }

            severity = Mathf.Max(severity, tile.Traffic - 54);
            severity = Mathf.Max(severity, 34 - ServiceAccessValue(tile));
            severity = Mathf.Max(severity, 30 - tile.TransitAccess + tile.Traffic / 4);
            severity = Mathf.Max(severity, 30 - tile.LogisticsAccess + tile.Traffic / 4);
            severity = Mathf.Max(severity, 28 - tile.WasteAccess);
            severity = Mathf.Max(severity, 28 - Mathf.Max(tile.CommunicationAccess, tile.MailAccess));
            severity = Mathf.Max(severity, 28 - tile.ParkingAccess);
            severity = Mathf.Max(severity, PollutionStress(tile) - (IsPollutionSensitiveUse(tile) ? 18 : 36));
            severity = Mathf.Max(severity, 34 - tile.LandValue);
            severity = Mathf.Max(severity, 28 - tile.StormwaterAccess);
            if (metrics != null)
            {
                severity = Mathf.Max(severity, 95 - metrics.UtilityReliability);
                severity = Mathf.Max(severity, metrics.UtilityUtilization - 105);
                severity = Mathf.Max(severity, metrics.WastewaterUtilization - 105);
                severity = Mathf.Max(severity, metrics.FloodRisk - 45);
                severity = Mathf.Max(severity, 62 - metrics.StormwaterResilience);
                severity = Mathf.Max(severity, FiscalIssueSeverity(metrics));
            }

            return Mathf.Max(0, severity);
        }

        private static bool CityIssueUsesTrafficMaterial(TileData tile, CityMetrics metrics)
        {
            return tile != null
                && (tile.Traffic >= 58
                    || PollutionStress(tile) >= 42
                    || (metrics != null && (metrics.FloodRisk > 55 || metrics.UtilityReliability < 90)));
        }

        private void AddLooseCube(List<GameObject> list, string name, Material material, Vector3 position, Vector3 scale)
        {
            var obj = CreateCube(name, material);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            list.Add(obj);
        }

        private GameObject CreateBuildingVisual(PlacedBuilding building, BuildingDefinition definition, Material material, ZoneType zone)
        {
            // BUILDING_VISUAL_PREFAB_LIBRARY keeps visuals procedural for the mini-game export.
            var root = new GameObject("Building-" + building.ConfigId);
            root.transform.SetParent(transform, false);

            var modelKey = ModelKeyVisualCatalog(definition);
            var tile = controller != null ? controller.GetTile(building.Pos.X, building.Pos.Y) : null;
            var width = Mathf.Max(1, building.Size.W) * cellSize * 0.82f;
            var depth = Mathf.Max(1, building.Size.H) * cellSize * 0.82f;
            var level = BuildingLevel(building);
            var height = (buildingBaseHeight + Mathf.Max(1, building.Size.W + building.Size.H) * 0.18f) * (1f + (level - 1) * 0.28f);
            height *= BuildingVisualHeightScale(building, definition, modelKey);

            // 生成建筑变体
            var seed = building.Id.GetHashCode();
            var variant = BuildingVariantGenerator.GenerateVariant(building.ConfigId, seed);

            // 应用变体尺寸
            width *= variant.WidthScale;
            depth *= variant.DepthScale;
            height *= variant.HeightScale;

            // 生成程序化材质（应用颜色变体）
            material = ProceduralBuildingMaterial.GenerateMaterial(modelKey, variant.ColorVariation, material);

            AddPart(root, "LowPolyBuildingFootprintShadow", buildingFootprintMaterial, building, width * 1.08f, 0.035f, depth * 1.08f, 0f, 0.018f, 0f);
            AddPart(root, "LowPolyBuildingCastShadow", buildingFootprintMaterial, building, width * 0.92f, 0.018f, depth * 0.92f, width * 0.08f, 0.012f, depth * 0.08f);
            AddBuildingParcelPad(root, building, modelKey, width, depth);
            AddBuildingZoneSkirt(root, building, zone, width, depth);
            AddBuildingEntryPaver(root, building, modelKey, width, depth);
            AddBuildingGrowthCues(root, building, definition, modelKey, width, height, depth, level);
            AddRecentConstructionCues(root, building, modelKey, width, height, depth);
            AddBuildingServiceStatusPlaque(root, building, tile, definition, modelKey, width, height, depth);
            AddBuildingRewardBubble(root, building, tile, definition, modelKey, width, height, depth, level);

            if (string.IsNullOrEmpty(modelKey))
            {
                FallbackCubeVisual(root, building, material, width, depth, height);
                AddSkylineFacadeDetails(root, building, modelKey, width, height, depth, level);
                return root;
            }

            if (modelKey == "residential")
            {
                AddPart(root, "HousingPod", material, building, width * 0.9f, height, depth * 0.9f, 0f, height * 0.5f, 0f);
                AddPart(root, "Roof", roofMaterial, building, width * 0.68f, Mathf.Max(0.08f, height * 0.12f), depth * 0.72f, 0f, height + 0.04f, 0f);
            }
            else if (modelKey == "commercial" || modelKey == "mixed_use")
            {
                AddPart(root, "Storefront", material, building, width, height * 0.42f, depth, 0f, height * 0.21f, 0f);
                AddPart(root, "UpperBlock", material, building, width * 0.72f, height * 0.74f, depth * 0.76f, 0f, height * 0.79f, 0f);
                AddPart(root, "SignBand", serviceMaterial, building, width * 0.82f, 0.08f, depth * 0.12f, 0f, height * 0.5f, depth * 0.47f);
            }
            else if (modelKey == "office" || modelKey == "innovation")
            {
                AddPart(root, "OfficeCore", material, building, width * 0.66f, height * 1.22f, depth * 0.66f, 0f, height * 0.61f, 0f);
                AddPart(root, "SkyDeck", serviceMaterial, building, width * 0.48f, 0.1f, depth * 0.5f, 0f, height * 1.25f, 0f);
                AddPart(root, "SideWing", material, building, width * 0.22f, height * 0.74f, depth * 0.58f, width * 0.34f, height * 0.37f, 0f);
            }
            else if (modelKey == "industrial" || modelKey == "resource" || modelKey == "warehouse")
            {
                AddPart(root, "IndustrialShed", material, building, width, height * 0.55f, depth, 0f, height * 0.28f, 0f);
                AddPart(root, "PlantStack", utilityMaterial, building, width * 0.18f, height * 1.05f, depth * 0.18f, width * 0.32f, height * 0.72f, -depth * 0.22f);
                AddPart(root, "ServiceBay", serviceMaterial, building, width * 0.35f, height * 0.28f, depth * 0.18f, -width * 0.28f, height * 0.22f, depth * 0.42f);
            }
            else if (modelKey == "park" || modelKey == "plaza" || modelKey == "deathcare")
            {
                AddPart(root, "CivicGround", material, building, width, height * 0.18f, depth, 0f, height * 0.09f, 0f);
                AddPart(root, "GardenMarker", serviceMaterial, building, width * 0.24f, height * 0.55f, depth * 0.24f, -width * 0.25f, height * 0.38f, -depth * 0.2f);
                AddPart(root, "Canopy", material, building, width * 0.45f, height * 0.18f, depth * 0.45f, width * 0.16f, height * 0.42f, depth * 0.15f);
                AddLandscapeAmenityDetails(root, building, width, depth);
            }
            else if (modelKey == "clinic" || modelKey == "school" || modelKey == "advanced_education" || modelKey == "administration")
            {
                AddPart(root, "PublicBlock", material, building, width * 0.9f, height * 0.72f, depth * 0.9f, 0f, height * 0.36f, 0f);
                AddPart(root, "EntryWing", serviceMaterial, building, width * 0.36f, height * 0.28f, depth * 0.34f, 0f, height * 0.24f, depth * 0.43f);
                AddPart(root, "RoofCap", roofMaterial, building, width * 0.64f, height * 0.2f, depth * 0.64f, 0f, height * 0.82f, 0f);
            }
            else if (modelKey == "transit" || modelKey == "intercity" || modelKey == "freight_rail" || modelKey == "logistics")
            {
                AddPart(root, "StationHall", material, building, width, height * 0.42f, depth * 0.68f, 0f, height * 0.21f, 0f);
                AddPart(root, "Platform", roadMaterial, building, width * 1.02f, 0.1f, depth * 0.24f, 0f, 0.1f, depth * 0.36f);
                AddPart(root, "Tower", serviceMaterial, building, width * 0.22f, height * 0.88f, depth * 0.22f, width * 0.34f, height * 0.5f, -depth * 0.2f);
            }
            else if (modelKey == "communications" || modelKey == "mail")
            {
                AddPart(root, "CommsBase", material, building, width * 0.74f, height * 0.54f, depth * 0.74f, 0f, height * 0.27f, 0f);
                AddPart(root, "AntennaMast", serviceMaterial, building, width * 0.12f, height * 1.15f, depth * 0.12f, 0f, height * 0.86f, 0f);
                AddPart(root, "SignalHead", material, building, width * 0.34f, height * 0.1f, depth * 0.34f, 0f, height * 1.45f, 0f);
            }
            else if (modelKey == "safety" || modelKey == "security" || modelKey == "shelter" || modelKey == "road_maintenance")
            {
                AddPart(root, "ResponseBase", material, building, width * 0.9f, height * 0.5f, depth * 0.9f, 0f, height * 0.25f, 0f);
                AddPart(root, "GarageDoor", roadMaterial, building, width * 0.4f, height * 0.2f, depth * 0.08f, 0f, height * 0.24f, depth * 0.46f);
                AddPart(root, "Beacon", serviceMaterial, building, width * 0.18f, height * 0.36f, depth * 0.18f, width * 0.25f, height * 0.68f, -depth * 0.2f);
            }
            else if (modelKey == "parking")
            {
                AddPart(root, "ParkingDeck", material, building, width * 0.92f, height * 0.86f, depth * 0.92f, 0f, height * 0.43f, 0f);
                AddPart(root, "Ramp", roadMaterial, building, width * 0.7f, height * 0.12f, depth * 0.2f, 0f, height * 0.26f, depth * 0.4f);
            }
            else if (modelKey == "power" || modelKey == "solar" || modelKey == "water" || modelKey == "sewage" || modelKey == "recycling" || modelKey == "waste_to_energy" || modelKey == "stormwater")
            {
                AddPart(root, "UtilityPad", material, building, width, height * 0.28f, depth, 0f, height * 0.14f, 0f);
                AddPart(root, "UtilityTank", utilityMaterial, building, width * 0.38f, height * 0.7f, depth * 0.38f, -width * 0.22f, height * 0.48f, 0f);
                AddPart(root, "UtilityNode", serviceMaterial, building, width * 0.28f, height * 0.42f, depth * 0.28f, width * 0.26f, height * 0.36f, depth * 0.18f);
            }
            else if (modelKey == "landmark")
            {
                AddPart(root, "LandmarkPodium", material, building, width, height * 0.35f, depth, 0f, height * 0.18f, 0f);
                AddPart(root, "LandmarkTower", serviceMaterial, building, width * 0.42f, height * 1.22f, depth * 0.42f, 0f, height * 0.78f, 0f);
                AddPart(root, "LandmarkCrown", material, building, width * 0.62f, height * 0.14f, depth * 0.62f, 0f, height * 1.42f, 0f);
            }
            else
            {
                FallbackCubeVisual(root, building, material, width, depth, height);
            }

            AddFormalPrefabReplacementDetails(root, building, definition, modelKey, width, height, depth, level);
            AddSkylineFacadeDetails(root, building, modelKey, width, height, depth, level);
            return root;
        }

        private void AddBuildingRewardBubble(GameObject root, PlacedBuilding building, TileData tile, BuildingDefinition definition, string modelKey, float width, float height, float depth, int level)
        {
            // SIMCITY_BUILDING_REWARD_BUBBLES adds tiny positive map feedback without changing gameplay stats.
            if (building == null || tile == null || string.IsNullOrEmpty(building.ConnectedRoadId))
            {
                return;
            }

            var seed = DecorationHash(building.Pos.X, building.Pos.Y);
            var serviceScore = ServiceAccessValue(tile);
            var happy = serviceScore >= 62 && tile.LandValue >= 50 && tile.Traffic < 62;
            var provider = IsServiceStatusProvider(definition, modelKey);
            var mature = level >= 2 && tile.LandValue >= 58;
            if (!happy && !provider && !mature)
            {
                return;
            }

            if (!provider && seed % 3 == 1)
            {
                return;
            }

            int faceX;
            int faceZ;
            GetBuildingRoadFace(building, out faceX, out faceZ);
            var bubbleY = Mathf.Clamp(height + 0.34f + (seed % 3) * 0.035f, 0.68f, height + 0.58f);
            var along = ((seed & 4) == 0 ? -0.26f : 0.28f);
            var bubbleMaterial = provider ? BuildingRewardProviderMaterial(definition, modelKey) : (mature ? previewOkMaterial : serviceNeedMaterial);
            AddBuildingFacePart(root, "BuildingRewardBubbleStem", roadLineMaterial, building, faceX, faceZ, 0.04f, 0.24f, 0.04f, along, bubbleY - 0.18f, 0.3f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleBack", bubbleMaterial, building, faceX, faceZ, 0.2f, 0.15f, 0.065f, along, bubbleY, 0.32f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleShine", windowMaterial, building, faceX, faceZ, 0.09f, 0.045f, 0.07f, along - 0.035f, bubbleY + 0.045f, 0.34f, width, depth);
            AddBuildingRewardBubblePickupPolish(root, building, faceX, faceZ, width, depth, bubbleY, along, bubbleMaterial, provider, mature);

            if (provider)
            {
                AddBuildingFacePart(root, "BuildingRewardBubbleServicePlus", roadLineMaterial, building, faceX, faceZ, 0.13f, 0.028f, 0.075f, along, bubbleY + 0.002f, 0.35f, width, depth);
                AddBuildingFacePart(root, "BuildingRewardBubbleServicePlus", roadLineMaterial, building, faceX, faceZ, 0.036f, 0.092f, 0.075f, along, bubbleY + 0.002f, 0.35f, width, depth);
                return;
            }

            AddBuildingFacePart(root, "BuildingRewardBubbleCoin", roadLineMaterial, building, faceX, faceZ, 0.095f, 0.082f, 0.075f, along, bubbleY + 0.002f, 0.35f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleCoinGlint", windowMaterial, building, faceX, faceZ, 0.032f, 0.038f, 0.08f, along + 0.04f, bubbleY + 0.034f, 0.36f, width, depth);
        }

        private void AddBuildingRewardBubblePickupPolish(GameObject root, PlacedBuilding building, int faceX, int faceZ, float width, float depth, float bubbleY, float along, Material bubbleMaterial, bool provider, bool mature)
        {
            // MOBILE_REWARD_BUBBLE_PICKUP_POLISH makes collectible map feedback read at thumb-scale.
            var liftMaterial = provider ? serviceMaterial : (mature ? previewOkMaterial : bubbleMaterial);
            AddBuildingFacePart(root, "BuildingRewardBubblePickupTray", roadLineMaterial, building, faceX, faceZ, 0.22f, 0.024f, 0.072f, along + 0.02f, bubbleY - 0.145f, 0.336f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubblePickupTail", liftMaterial, building, faceX, faceZ, 0.052f, 0.13f, 0.07f, along + 0.075f, bubbleY - 0.105f, 0.345f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubblePickupDot", roadLineMaterial, building, faceX, faceZ, 0.058f, 0.046f, 0.075f, along + 0.112f, bubbleY - 0.005f, 0.365f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleCollectRing", windowMaterial, building, faceX, faceZ, 0.16f, 0.018f, 0.076f, along + 0.112f, bubbleY - 0.052f, 0.374f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleCollectRing", roadLineMaterial, building, faceX, faceZ, 0.018f, 0.13f, 0.077f, along + 0.112f, bubbleY - 0.052f, 0.378f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleTapSpark", liftMaterial, building, faceX, faceZ, 0.035f, 0.058f, 0.078f, along + 0.214f, bubbleY + 0.035f, 0.372f, width, depth);
            AddBuildingFacePart(root, "BuildingRewardBubbleTapSpark", windowMaterial, building, faceX, faceZ, 0.07f, 0.02f, 0.079f, along + 0.214f, bubbleY + 0.035f, 0.382f, width, depth);

            if (provider || mature)
            {
                AddBuildingFacePart(root, "BuildingRewardBubblePickupSpark", windowMaterial, building, faceX, faceZ, 0.044f, 0.074f, 0.078f, along - 0.125f, bubbleY + 0.095f, 0.372f, width, depth);
                AddBuildingFacePart(root, "BuildingRewardBubblePickupSpark", roadLineMaterial, building, faceX, faceZ, 0.088f, 0.026f, 0.078f, along - 0.125f, bubbleY + 0.095f, 0.382f, width, depth);
            }
        }

        private Material BuildingRewardProviderMaterial(BuildingDefinition definition, string modelKey)
        {
            if (definition != null && definition.Category == BuildingCategory.Utility)
            {
                return utilityMaterial;
            }

            if (IsUtilityModel(modelKey))
            {
                return utilityMaterial;
            }

            if (modelKey == "clinic" || modelKey == "safety" || modelKey == "security" || modelKey == "shelter")
            {
                return trafficPulseMaterial;
            }

            if (modelKey == "transit" || modelKey == "intercity" || modelKey == "parking")
            {
                return windowMaterial;
            }

            return serviceMaterial;
        }

        private void AddBuildingServiceStatusPlaque(GameObject root, PlacedBuilding building, TileData tile, BuildingDefinition definition, string modelKey, float width, float height, float depth)
        {
            // CITY_SKYLINES_BUILDING_SERVICE_PLAQUES add tiny facade status cards without changing simulation data.
            var kind = BuildingServiceStatusKind(building, tile, definition, modelKey);
            if (kind == 0)
            {
                return;
            }

            int faceX;
            int faceZ;
            GetBuildingRoadFace(building, out faceX, out faceZ);
            var centerY = Mathf.Clamp(height * 0.56f, 0.32f, height + 0.08f);
            var material = BuildingServiceStatusMaterial(kind, definition, modelKey);
            AddBuildingFacePart(root, "BuildingServiceStatusPlaqueBack", material, building, faceX, faceZ, 0.26f, 0.12f, 0.052f, 0.36f, centerY, 0.62f, width, depth);
            AddBuildingFacePart(root, "BuildingServiceStatusPlaqueHeader", roadLineMaterial, building, faceX, faceZ, 0.18f, 0.035f, 0.058f, 0.36f, centerY + 0.052f, 0.635f, width, depth);
            AddBuildingServiceStatusGlyph(root, building, kind, faceX, faceZ, width, depth, centerY, 0.36f);
            AddBuildingServiceStatusMicroMeter(root, building, kind, material, faceX, faceZ, width, depth, centerY);
        }

        private int BuildingServiceStatusKind(PlacedBuilding building, TileData tile, BuildingDefinition definition, string modelKey)
        {
            if (building == null || tile == null)
            {
                return 0;
            }

            var metrics = controller != null ? controller.Metrics : null;
            if (string.IsNullOrEmpty(building.ConnectedRoadId))
            {
                return 1;
            }

            if (tile.ParkingAccess < 26 && IsParkingSensitiveUse(tile))
            {
                return 3;
            }

            if (tile.StormwaterAccess < 26 || (metrics != null && metrics.FloodRisk >= 66 && tile.StormwaterAccess < 42))
            {
                return 4;
            }

            if (ServiceAccessValue(tile) < 30 && !IsUtilityModel(modelKey))
            {
                return 2;
            }

            if (tile.LandValue >= HighLandValueSignalThreshold(metrics) && !IsUtilityModel(modelKey))
            {
                return 5;
            }

            return IsServiceStatusProvider(definition, modelKey) ? 6 : 0;
        }

        private Material BuildingServiceStatusMaterial(int kind, BuildingDefinition definition, string modelKey)
        {
            if (kind == 1) return trafficPulseMaterial;
            if (kind == 2 || kind == 3) return serviceNeedMaterial;
            if (kind == 4) return windowMaterial;
            if (kind == 5) return previewOkMaterial;
            if (definition != null && definition.Category == BuildingCategory.Utility) return utilityMaterial;
            if (IsUtilityModel(modelKey)) return utilityMaterial;
            return serviceMaterial;
        }

        private void AddBuildingServiceStatusGlyph(GameObject root, PlacedBuilding building, int kind, int faceX, int faceZ, float width, float depth, float centerY, float alongOffset)
        {
            var faceOffset = 0.648f;
            if (kind == 1)
            {
                AddBuildingFacePart(root, "BuildingStatusRoadMissingBar", roadLineMaterial, building, faceX, faceZ, 0.15f, 0.04f, 0.055f, alongOffset, centerY - 0.012f, faceOffset, width, depth);
                AddBuildingFacePart(root, "BuildingStatusRoadMissingPost", roadLineMaterial, building, faceX, faceZ, 0.045f, 0.11f, 0.055f, alongOffset, centerY - 0.006f, faceOffset, width, depth);
                return;
            }

            if (kind == 2)
            {
                AddBuildingFacePart(root, "BuildingStatusServicePlus", roadLineMaterial, building, faceX, faceZ, 0.16f, 0.04f, 0.055f, alongOffset, centerY - 0.005f, faceOffset, width, depth);
                AddBuildingFacePart(root, "BuildingStatusServicePlus", roadLineMaterial, building, faceX, faceZ, 0.045f, 0.12f, 0.055f, alongOffset, centerY - 0.005f, faceOffset, width, depth);
                return;
            }

            if (kind == 3)
            {
                AddBuildingFacePart(root, "BuildingStatusParkingStem", roadMaterial, building, faceX, faceZ, 0.045f, 0.13f, 0.055f, alongOffset - 0.035f, centerY, faceOffset, width, depth);
                AddBuildingFacePart(root, "BuildingStatusParkingLoop", roadMaterial, building, faceX, faceZ, 0.14f, 0.043f, 0.055f, alongOffset + 0.015f, centerY + 0.045f, faceOffset, width, depth);
                AddBuildingFacePart(root, "BuildingStatusParkingLoop", roadMaterial, building, faceX, faceZ, 0.11f, 0.038f, 0.055f, alongOffset + 0.005f, centerY - 0.005f, faceOffset, width, depth);
                return;
            }

            if (kind == 4)
            {
                AddBuildingFacePart(root, "BuildingStatusWaterLine", utilityMaterial, building, faceX, faceZ, 0.17f, 0.038f, 0.055f, alongOffset, centerY - 0.02f, faceOffset, width, depth);
                AddBuildingFacePart(root, "BuildingStatusWaterDrop", roadLineMaterial, building, faceX, faceZ, 0.07f, 0.09f, 0.055f, alongOffset, centerY + 0.05f, faceOffset, width, depth);
                return;
            }

            if (kind == 5)
            {
                AddBuildingFacePart(root, "BuildingStatusLandValueGem", roadLineMaterial, building, faceX, faceZ, 0.11f, 0.09f, 0.055f, alongOffset, centerY + 0.018f, faceOffset, width, depth);
                AddBuildingFacePart(root, "BuildingStatusLandValueGlint", windowMaterial, building, faceX, faceZ, 0.05f, 0.06f, 0.055f, alongOffset + 0.08f, centerY + 0.07f, faceOffset, width, depth);
                return;
            }

            AddBuildingFacePart(root, "BuildingStatusProviderDot", roadLineMaterial, building, faceX, faceZ, 0.1f, 0.095f, 0.055f, alongOffset, centerY + 0.012f, faceOffset, width, depth);
            AddBuildingFacePart(root, "BuildingStatusProviderLine", windowMaterial, building, faceX, faceZ, 0.16f, 0.035f, 0.055f, alongOffset, centerY - 0.052f, faceOffset, width, depth);
        }

        private void AddBuildingServiceStatusMicroMeter(GameObject root, PlacedBuilding building, int kind, Material material, int faceX, int faceZ, float width, float depth, float centerY)
        {
            // CITY_SKYLINES_BUILDING_STATUS_MICROMETER adds small service-state chips to individual buildings.
            var faceOffset = 0.662f;
            var chipCount = kind == 6 ? 2 : 3;
            var alert = kind == 1 || kind == 2 || kind == 3 || kind == 4;
            for (var i = 0; i < chipCount; i += 1)
            {
                var offset = 0.25f + i * 0.085f;
                var chipMaterial = i == 0
                    ? material
                    : (alert && i == chipCount - 1 ? trafficPulseMaterial : roadLineMaterial);
                AddBuildingFacePart(root, "BuildingServiceStatusMicroChip", chipMaterial, building, faceX, faceZ, 0.045f, 0.035f, 0.058f, offset, centerY - 0.088f, faceOffset, width, depth);
            }

            if (alert)
            {
                AddBuildingFacePart(root, "BuildingServiceStatusAlertUnderline", serviceNeedMaterial, building, faceX, faceZ, 0.2f, 0.026f, 0.058f, 0.36f, centerY - 0.13f, faceOffset, width, depth);
            }
        }

        private static bool IsServiceStatusProvider(BuildingDefinition definition, string modelKey)
        {
            if (definition != null && (definition.Category == BuildingCategory.Service || definition.Category == BuildingCategory.Utility))
            {
                return true;
            }

            return modelKey == "clinic"
                || modelKey == "school"
                || modelKey == "advanced_education"
                || modelKey == "safety"
                || modelKey == "security"
                || modelKey == "shelter"
                || modelKey == "road_maintenance"
                || modelKey == "transit"
                || modelKey == "intercity"
                || modelKey == "logistics"
                || modelKey == "freight_rail"
                || modelKey == "parking"
                || IsUtilityModel(modelKey);
        }

        private void AddBuildingGrowthCues(GameObject root, PlacedBuilding building, BuildingDefinition definition, string modelKey, float width, float height, float depth, int level)
        {
            // CITY_SKYLINES_BUILDING_GROWTH_CUES makes vertical growth and upgrade blockers visible on the map.
            if (!VisualSupportsGrowth(definition, modelKey))
            {
                return;
            }

            var pipCount = Mathf.Clamp(level, 1, 3);
            for (var i = 0; i < pipCount; i += 1)
            {
                AddPart(root, "BuildingLevelPip", roadLineMaterial, building, width * 0.08f, 0.045f, depth * 0.045f, -width * 0.28f + i * width * 0.11f, Mathf.Max(0.18f, height + 0.12f), -depth * 0.48f);
            }

            AddBuildingLevelRibbons(root, building, width, height, depth, level);

            if (level >= 3)
            {
                AddPart(root, "BuildingMaxLevelCrown", windowMaterial, building, width * 0.22f, 0.045f, depth * 0.06f, width * 0.24f, height + 0.16f, -depth * 0.46f);
                return;
            }

            var score = BuildingGrowthVisualScore(building);
            var metrics = controller != null ? controller.Metrics : null;
            if (score >= 72 && metrics != null && metrics.BuildingUpgradeReadyCount > 0)
            {
                AddPart(root, "BuildingUpgradeReadyHalo", previewOkMaterial, building, width * 0.38f, 0.035f, depth * 0.14f, width * 0.22f, height + 0.11f, -depth * 0.44f);
                AddPart(root, "BuildingUpgradeArrowStem", previewOkMaterial, building, width * 0.055f, 0.18f, depth * 0.055f, width * 0.22f, height + 0.22f, -depth * 0.44f);
                AddPart(root, "BuildingUpgradeArrowCap", roadLineMaterial, building, width * 0.18f, 0.05f, depth * 0.08f, width * 0.22f, height + 0.34f, -depth * 0.44f);
                return;
            }

            if (score < 52 && metrics != null && metrics.BuildingUpgradeBlockedCount > 0)
            {
                var blockerMaterial = BuildingGrowthBlockerMaterial(building);
                AddPart(root, "BuildingUpgradeBlockedPad", blockerMaterial, building, width * 0.24f, 0.04f, depth * 0.11f, width * 0.28f, height + 0.08f, -depth * 0.46f);
                AddPart(root, "BuildingUpgradeBlockedPost", blockerMaterial, building, width * 0.045f, 0.18f, depth * 0.045f, width * 0.28f, height + 0.19f, -depth * 0.46f);
                AddPart(root, "BuildingUpgradeBlockedDot", roadLineMaterial, building, width * 0.08f, 0.04f, depth * 0.055f, width * 0.28f, height + 0.32f, -depth * 0.46f);
            }
        }

        private void AddBuildingLevelRibbons(GameObject root, PlacedBuilding building, float width, float height, float depth, int level)
        {
            if (level < 2)
            {
                return;
            }

            var ribbonY = Mathf.Max(0.26f, height * 0.54f);
            AddPart(root, "BuildingLevelRibbonFront", serviceMaterial, building, width * 0.56f, 0.045f, 0.035f, 0f, ribbonY, -depth * 0.49f);
            AddPart(root, "BuildingLevelRibbonSide", serviceMaterial, building, 0.035f, 0.045f, depth * 0.46f, -width * 0.49f, ribbonY, -depth * 0.06f);
            if (level >= 3)
            {
                AddPart(root, "BuildingLevelHighRibbonFront", windowMaterial, building, width * 0.46f, 0.04f, 0.032f, 0f, Mathf.Max(ribbonY + 0.22f, height * 0.72f), -depth * 0.5f);
            }
        }

        private static bool VisualSupportsGrowth(BuildingDefinition definition, string modelKey)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.Category == BuildingCategory.Utility || definition.Category == BuildingCategory.Service)
            {
                return false;
            }

            return !IsLandscapeModel(modelKey);
        }

        private float BuildingVisualHeightScale(PlacedBuilding building, BuildingDefinition definition, string modelKey)
        {
            // REFERENCE_IMAGE_CITY_CORE_HEIGHT_LAYERING makes the downtown grid read with low-poly skyline depth.
            if (definition == null || IsLandscapeModel(modelKey) || IsUtilityModel(modelKey))
            {
                return 1f;
            }

            var scale = 1f;
            if (IsCentralRoadTile(building.Pos))
            {
                scale += 0.09f;
            }

            if (!string.IsNullOrEmpty(building.ConnectedRoadId))
            {
                scale += 0.04f;
            }

            if (definition.Category == BuildingCategory.Commercial || modelKey == "office" || modelKey == "innovation")
            {
                scale += 0.05f;
            }

            return Mathf.Clamp(scale, 1f, 1.2f);
        }

        private int BuildingGrowthVisualScore(PlacedBuilding building)
        {
            var tile = controller != null ? controller.GetTile(building.Pos.X, building.Pos.Y) : null;
            if (tile == null)
            {
                return 0;
            }

            var connected = string.IsNullOrEmpty(building.ConnectedRoadId) ? 0 : 18;
            var service = ServiceAccessValue(tile) / 3;
            var transit = tile.TransitAccess / 4;
            var land = tile.LandValue / 2;
            var pollutionPenalty = Mathf.Max(tile.Pollution, tile.Noise) / 3;
            return Mathf.Clamp(connected + service + transit + land - pollutionPenalty, 0, 100);
        }

        private Material BuildingGrowthBlockerMaterial(PlacedBuilding building)
        {
            var tile = controller != null ? controller.GetTile(building.Pos.X, building.Pos.Y) : null;
            if (tile == null)
            {
                return previewBlockedMaterial;
            }

            if (string.IsNullOrEmpty(building.ConnectedRoadId) || tile.Traffic >= 60)
            {
                return trafficPulseMaterial;
            }

            return serviceNeedMaterial;
        }

        private void AddLandscapeAmenityDetails(GameObject root, PlacedBuilding building, float width, float depth)
        {
            // REFERENCE_IMAGE_PARK_AMENITY_DETAILS makes plazas and parks read as playful city spaces.
            AddPart(root, "LandscapeFountainBasin", windowMaterial, building, width * 0.22f, 0.045f, depth * 0.22f, width * 0.16f, 0.15f, -depth * 0.18f);
            AddPart(root, "LandscapeFountainJet", windowMaterial, building, width * 0.05f, 0.24f, depth * 0.05f, width * 0.16f, 0.29f, -depth * 0.18f);
            AddPart(root, "LandscapeFountainSpark", roadLineMaterial, building, width * 0.14f, 0.035f, depth * 0.14f, width * 0.16f, 0.43f, -depth * 0.18f);
            AddPart(root, "LandscapeGardenPath", roadLineMaterial, building, width * 0.56f, 0.028f, depth * 0.09f, -width * 0.04f, 0.105f, depth * 0.02f);
            AddPart(root, "LandscapeGardenPath", roadLineMaterial, building, width * 0.09f, 0.028f, depth * 0.5f, -width * 0.08f, 0.11f, depth * 0.02f);
            AddPart(root, "LandscapeBench", roadLineMaterial, building, width * 0.26f, 0.055f, depth * 0.055f, -width * 0.18f, 0.14f, depth * 0.23f);
            AddPart(root, "LandscapeHedgeRow", treeCanopyMaterial, building, width * 0.36f, 0.09f, depth * 0.07f, -width * 0.12f, 0.17f, -depth * 0.34f);
            AddPart(root, "LandscapeFlowerBed", serviceNeedMaterial, building, width * 0.18f, 0.045f, depth * 0.13f, width * 0.28f, 0.13f, depth * 0.22f);
            AddPart(root, "LandscapeTreeAccent", treeCanopyMaterial, building, width * 0.2f, 0.22f, depth * 0.2f, -width * 0.34f, 0.28f, -depth * 0.04f);
            AddLandscapeGreenwayDetails(root, building, width, depth);
        }

        private void AddLandscapeGreenwayDetails(GameObject root, PlacedBuilding building, float width, float depth)
        {
            // CITY_SKYLINES_PARK_GREENWAY_DETAILS gives parks a clean usable open-space silhouette.
            AddPart(root, "LandscapeGreenwayLoopNorth", grassGridMaterial, building, width * 0.54f, 0.026f, depth * 0.07f, -width * 0.02f, 0.118f, -depth * 0.31f);
            AddPart(root, "LandscapeGreenwayLoopSouth", grassGridMaterial, building, width * 0.54f, 0.026f, depth * 0.07f, width * 0.02f, 0.118f, depth * 0.32f);
            AddPart(root, "LandscapeGreenwayLoopWest", grassGridMaterial, building, width * 0.07f, 0.026f, depth * 0.48f, -width * 0.36f, 0.12f, 0f);
            AddPart(root, "LandscapeGreenwayPocketTree", treeCanopyMaterial, building, width * 0.16f, 0.18f, depth * 0.16f, width * 0.36f, 0.25f, -depth * 0.02f);
            AddPart(root, "LandscapeGreenwayPocketShrub", treeCanopyMaterial, building, width * 0.12f, 0.095f, depth * 0.12f, width * 0.3f, 0.14f, -depth * 0.28f);
            AddPart(root, "LandscapeGreenwayWayfinder", windowMaterial, building, width * 0.08f, 0.18f, depth * 0.08f, -width * 0.34f, 0.24f, depth * 0.34f);
            AddPart(root, "LandscapeGreenwayWayfinderCap", roadLineMaterial, building, width * 0.18f, 0.04f, depth * 0.08f, -width * 0.28f, 0.35f, depth * 0.34f);
        }

        private void GetBuildingRoadFace(PlacedBuilding building, out int faceX, out int faceZ)
        {
            faceX = 0;
            faceZ = -1;
            TryGetBuildingRoadFace(building, out faceX, out faceZ);
        }

        private bool TryGetBuildingRoadFace(PlacedBuilding building, out int faceX, out int faceZ)
        {
            faceX = 0;
            faceZ = -1;
            if (building == null || controller == null || controller.Grid == null)
            {
                return false;
            }

            var bestScore = 0;
            SelectBuildingRoadFace(building, 0, -1, ref bestScore, ref faceX, ref faceZ);
            SelectBuildingRoadFace(building, -1, 0, ref bestScore, ref faceX, ref faceZ);
            SelectBuildingRoadFace(building, 1, 0, ref bestScore, ref faceX, ref faceZ);
            SelectBuildingRoadFace(building, 0, 1, ref bestScore, ref faceX, ref faceZ);
            return bestScore > 0;
        }

        private void SelectBuildingRoadFace(PlacedBuilding building, int candidateX, int candidateZ, ref int bestScore, ref int faceX, ref int faceZ)
        {
            var score = BuildingRoadFaceScore(building, candidateX, candidateZ);
            if (score <= bestScore)
            {
                return;
            }

            bestScore = score;
            faceX = candidateX;
            faceZ = candidateZ;
        }

        private int BuildingRoadFaceScore(PlacedBuilding building, int faceX, int faceZ)
        {
            var widthTiles = Mathf.Max(1, building.Size.W);
            var depthTiles = Mathf.Max(1, building.Size.H);
            var score = 0;
            if (faceZ != 0)
            {
                var y = faceZ < 0 ? building.Pos.Y - 1 : building.Pos.Y + depthTiles;
                for (var x = building.Pos.X; x < building.Pos.X + widthTiles; x += 1)
                {
                    score += RoadFaceTileScore(x, y, building.ConnectedRoadId);
                }

                return score;
            }

            var sideX = faceX < 0 ? building.Pos.X - 1 : building.Pos.X + widthTiles;
            for (var y = building.Pos.Y; y < building.Pos.Y + depthTiles; y += 1)
            {
                score += RoadFaceTileScore(sideX, y, building.ConnectedRoadId);
            }

            return score;
        }

        private int RoadFaceTileScore(int x, int y, string connectedRoadId)
        {
            var tile = controller != null ? controller.GetTile(x, y) : null;
            if (tile == null || string.IsNullOrEmpty(tile.RoadId))
            {
                return 0;
            }

            return !string.IsNullOrEmpty(connectedRoadId) && tile.RoadId == connectedRoadId ? 10 : 4;
        }

        private void AddBuildingFacePart(GameObject root, string name, Material material, PlacedBuilding building, int faceX, int faceZ, float spanRatio, float partHeight, float thicknessRatio, float alongOffsetRatio, float centerY, float faceOffsetRatio, float width, float depth)
        {
            var normalizedFaceX = faceX == 0 ? 0 : (faceX > 0 ? 1 : -1);
            var normalizedFaceZ = faceZ == 0 ? 0 : (faceZ > 0 ? 1 : -1);
            if (normalizedFaceX == 0 && normalizedFaceZ == 0)
            {
                normalizedFaceZ = -1;
            }

            var faceSpan = normalizedFaceX != 0 ? depth : width;
            var faceDepth = normalizedFaceX != 0 ? width : depth;
            var span = faceSpan * spanRatio;
            var thickness = faceDepth * thicknessRatio;
            var alongOffset = faceSpan * alongOffsetRatio;
            var faceOffset = faceDepth * faceOffsetRatio;
            if (normalizedFaceX != 0)
            {
                AddPart(root, name, material, building, thickness, partHeight, span, normalizedFaceX * faceOffset, centerY, alongOffset);
                return;
            }

            AddPart(root, name, material, building, span, partHeight, thickness, alongOffset, centerY, normalizedFaceZ * faceOffset);
        }

        private void AddBuildingEntryPaver(GameObject root, PlacedBuilding building, string modelKey, float width, float depth)
        {
            // REFERENCE_IMAGE_BUILDING_ENTRY_PAVERS anchors ordinary buildings to the city grid.
            if (!UsesEntryPaver(modelKey))
            {
                return;
            }

            int faceX;
            int faceZ;
            GetBuildingRoadFace(building, out faceX, out faceZ);
            AddBuildingFacePart(root, "LowPolyEntryPaver", roadLineMaterial, building, faceX, faceZ, 0.34f, 0.026f, 0.13f, 0f, 0.105f, 0.6f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntryShadow", roadMaterial, building, faceX, faceZ, 0.42f, 0.018f, 0.12f, 0f, 0.085f, 0.63f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntryCurbTickLeft", roadLineMaterial, building, faceX, faceZ, 0.11f, 0.028f, 0.05f, -0.24f, 0.13f, 0.58f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntryCurbTickRight", roadLineMaterial, building, faceX, faceZ, 0.11f, 0.028f, 0.05f, 0.24f, 0.13f, 0.58f, width, depth);

            var connectedToRoad = BuildingRoadFaceScore(building, faceX, faceZ) > 0;
            if (connectedToRoad)
            {
                AddBuildingFacePart(root, "LowPolyStreetConnectorWalk", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, faceX, faceZ, 0.24f, 0.02f, 0.26f, 0f, 0.074f, 0.78f, width, depth);
                AddBuildingFacePart(root, "LowPolyStreetConnectorGlint", windowMaterial, building, faceX, faceZ, 0.12f, 0.016f, 0.035f, 0f, 0.096f, 0.92f, width, depth);
            }

            AddBuildingEntryDecor(root, building, faceX, faceZ, width, depth, connectedToRoad);
        }

        private void AddBuildingEntryDecor(GameObject root, PlacedBuilding building, int faceX, int faceZ, float width, float depth, bool connectedToRoad)
        {
            // REFERENCE_IMAGE_BUILDING_ENTRY_DECOR adds tiny awnings, door lights, and planters at street level.
            AddBuildingFacePart(root, "LowPolyEntryAwning", roofMaterial, building, faceX, faceZ, 0.28f, 0.055f, 0.085f, 0f, 0.245f, 0.56f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntryDoorGlow", windowMaterial, building, faceX, faceZ, 0.12f, 0.12f, 0.035f, 0f, 0.205f, 0.615f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntryPlanterLeft", treeCanopyMaterial, building, faceX, faceZ, 0.075f, 0.075f, 0.06f, -0.24f, 0.155f, 0.61f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntryPlanterRight", treeCanopyMaterial, building, faceX, faceZ, 0.075f, 0.075f, 0.06f, 0.24f, 0.155f, 0.61f, width, depth);
            AddBuildingEntryMicroSignage(root, building, faceX, faceZ, width, depth, connectedToRoad);

            if (connectedToRoad)
            {
                AddBuildingFacePart(root, "LowPolyEntryWelcomeMat", serviceNeedMaterial, building, faceX, faceZ, 0.18f, 0.018f, 0.1f, 0f, 0.125f, 0.75f, width, depth);
            }
        }

        private void AddBuildingEntryMicroSignage(GameObject root, PlacedBuilding building, int faceX, int faceZ, float width, float depth, bool connectedToRoad)
        {
            // LOW_POLY_ENTRY_MICRO_SIGNAGE gives ordinary buildings storefront-like street detail.
            var seed = DecorationHash(building.Pos.X, building.Pos.Y);
            var signMaterial = connectedToRoad ? serviceNeedMaterial : roadLineMaterial;
            AddBuildingFacePart(root, "LowPolyEntryBladeSign", signMaterial, building, faceX, faceZ, 0.095f, 0.12f, 0.04f, 0.34f, 0.3f, 0.65f, width, depth);
            AddBuildingFacePart(root, "LowPolyEntrySignGlint", windowMaterial, building, faceX, faceZ, 0.052f, 0.035f, 0.045f, 0.34f, 0.335f, 0.67f, width, depth);

            if (seed % 2 == 0)
            {
                AddBuildingFacePart(root, "LowPolyEntryCanopyTrim", roadLineMaterial, building, faceX, faceZ, 0.24f, 0.026f, 0.08f, 0f, 0.285f, 0.62f, width, depth);
            }

            if (seed % 3 == 0)
            {
                AddBuildingFacePart(root, "LowPolyEntryMenuTile", windowMaterial, building, faceX, faceZ, 0.075f, 0.085f, 0.04f, -0.34f, 0.24f, 0.64f, width, depth);
                AddBuildingFacePart(root, "LowPolyEntryMenuLine", roadLineMaterial, building, faceX, faceZ, 0.052f, 0.024f, 0.045f, -0.34f, 0.27f, 0.66f, width, depth);
            }
        }

        private void AddBuildingParcelPad(GameObject root, PlacedBuilding building, string modelKey, float width, float depth)
        {
            // REFERENCE_IMAGE_BLOCK_SIDEWALK_PADS makes buildings sit on readable city-builder parcels.
            if (IsLandscapeModel(modelKey))
            {
                return;
            }

            var padMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            AddPart(root, "LowPolyParcelSidewalkPad", padMaterial, building, width * 1.04f, 0.018f, depth * 1.04f, 0f, 0.042f, 0f);
            int faceX;
            int faceZ;
            GetBuildingRoadFace(building, out faceX, out faceZ);
            AddBuildingParcelFoundationFrame(root, building, width, depth, faceX, faceZ);
            AddBuildingFacePart(root, "LowPolyParcelStreetCurb", roadLineMaterial, building, faceX, faceZ, 0.82f, 0.022f, 0.035f, 0f, 0.072f, 0.56f, width, depth);
            AddBuildingFacePart(root, "LowPolyParcelStreetTick", roadLineMaterial, building, faceX, faceZ, 0.16f, 0.022f, 0.035f, -0.36f, 0.078f, 0.54f, width, depth);
            AddBuildingFacePart(root, "LowPolyParcelStreetTick", roadLineMaterial, building, faceX, faceZ, 0.16f, 0.022f, 0.035f, 0.36f, 0.078f, 0.54f, width, depth);
        }

        private void AddBuildingParcelFoundationFrame(GameObject root, PlacedBuilding building, float width, float depth, int faceX, int faceZ)
        {
            // LOW_POLY_PARCEL_FOUNDATION_FRAME gives each grown building a crisp city-builder lot outline.
            var frameWidth = width * 1.08f;
            var frameDepth = depth * 1.08f;
            var edgeThickness = 0.032f;
            var edgeY = 0.078f;
            AddPart(root, "LowPolyParcelFoundationEdge", roadLineMaterial, building, frameWidth, 0.024f, edgeThickness, 0f, edgeY, -frameDepth * 0.5f);
            AddPart(root, "LowPolyParcelFoundationEdge", roadLineMaterial, building, frameWidth, 0.024f, edgeThickness, 0f, edgeY, frameDepth * 0.5f);
            AddPart(root, "LowPolyParcelFoundationEdge", roadLineMaterial, building, edgeThickness, 0.024f, frameDepth, -frameWidth * 0.5f, edgeY, 0f);
            AddPart(root, "LowPolyParcelFoundationEdge", roadLineMaterial, building, edgeThickness, 0.024f, frameDepth, frameWidth * 0.5f, edgeY, 0f);

            var corner = 0.085f;
            AddPart(root, "LowPolyParcelFoundationCorner", windowMaterial, building, corner, 0.035f, corner, -frameWidth * 0.5f, edgeY + 0.012f, -frameDepth * 0.5f);
            AddPart(root, "LowPolyParcelFoundationCorner", windowMaterial, building, corner, 0.035f, corner, frameWidth * 0.5f, edgeY + 0.012f, -frameDepth * 0.5f);
            AddPart(root, "LowPolyParcelFoundationCorner", windowMaterial, building, corner, 0.035f, corner, -frameWidth * 0.5f, edgeY + 0.012f, frameDepth * 0.5f);
            AddPart(root, "LowPolyParcelFoundationCorner", windowMaterial, building, corner, 0.035f, corner, frameWidth * 0.5f, edgeY + 0.012f, frameDepth * 0.5f);

            AddBuildingFacePart(root, "LowPolyParcelAddressPlate", serviceNeedMaterial, building, faceX, faceZ, 0.18f, 0.032f, 0.04f, -0.24f, edgeY + 0.03f, 0.58f, width, depth);
            AddBuildingFacePart(root, "LowPolyParcelAddressGlint", windowMaterial, building, faceX, faceZ, 0.09f, 0.02f, 0.045f, -0.24f, edgeY + 0.062f, 0.6f, width, depth);
        }

        private void AddRecentConstructionCues(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // REFERENCE_IMAGE_CITY_CONSTRUCTION_CUES makes new zoning growth visible in the live demo.
            if (building == null || IsLandscapeModel(modelKey))
            {
                return;
            }

            var visibleDays = building.AutoDeveloped ? 8 : 3;
            if (building.AgeDays > visibleDays)
            {
                return;
            }

            if (building.AgeDays <= 1)
            {
                AddFreshConstructionFoundationCues(root, building, width, depth);
            }
            else if (building.AgeDays <= 4)
            {
                AddActiveConstructionScaffoldCues(root, building, width, height, depth);
            }
            else
            {
                AddConstructionCleanupCues(root, building, width, height, depth);
            }

            if (building.AutoDeveloped)
            {
                AddPart(root, "AutoGrowthPermitFlag", windowMaterial, building, width * 0.18f, 0.07f, depth * 0.035f, -width * 0.36f, height + 0.2f, -depth * 0.48f);
            }
        }

        private void AddFreshConstructionFoundationCues(GameObject root, PlacedBuilding building, float width, float depth)
        {
            // CITY_CONSTRUCTION_STAGE_FOUNDATION makes brand-new growth read as a fresh build site.
            AddPart(root, "ConstructionFreshFoundationPad", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.78f, 0.035f, depth * 0.66f, 0f, 0.09f, -depth * 0.02f);
            AddPart(root, "ConstructionFreshFootingFront", roadLineMaterial, building, width * 0.58f, 0.04f, depth * 0.045f, 0f, 0.13f, -depth * 0.38f);
            AddPart(root, "ConstructionFreshFootingSide", roadLineMaterial, building, width * 0.045f, 0.04f, depth * 0.42f, -width * 0.32f, 0.13f, -depth * 0.06f);
            AddPart(root, "ConstructionMaterialStack", serviceNeedMaterial, building, width * 0.18f, 0.12f, depth * 0.12f, width * 0.28f, 0.16f, depth * 0.22f);
            AddPart(root, "ConstructionSafetyFenceFront", serviceMaterial, building, width * 0.74f, 0.06f, depth * 0.035f, 0f, 0.18f, -depth * 0.56f);
            AddPart(root, "ConstructionSafetyFenceSide", serviceMaterial, building, width * 0.035f, 0.06f, depth * 0.5f, -width * 0.5f, 0.18f, -depth * 0.02f);
        }

        private void AddActiveConstructionScaffoldCues(GameObject root, PlacedBuilding building, float width, float height, float depth)
        {
            // CITY_CONSTRUCTION_STAGE_ACTIVE shows mid-build scaffolding and small crane activity.
            AddPart(root, "ConstructionScaffoldFront", roadLineMaterial, building, width * 0.58f, 0.04f, 0.035f, 0f, Mathf.Max(0.24f, height * 0.42f), -depth * 0.535f);
            AddPart(root, "ConstructionScaffoldPost", serviceMaterial, building, width * 0.04f, height * 0.58f, depth * 0.035f, -width * 0.34f, Mathf.Max(0.28f, height * 0.36f), -depth * 0.54f);
            AddPart(root, "ConstructionScaffoldPost", serviceMaterial, building, width * 0.04f, height * 0.58f, depth * 0.035f, width * 0.34f, Mathf.Max(0.28f, height * 0.36f), -depth * 0.54f);
            AddPart(root, "ConstructionMidDeck", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.42f, 0.035f, depth * 0.12f, -width * 0.06f, Mathf.Max(0.26f, height * 0.54f), -depth * 0.52f);
            AddPart(root, "ConstructionUpperDeck", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.36f, 0.032f, depth * 0.1f, width * 0.04f, Mathf.Max(0.32f, height * 0.7f), -depth * 0.52f);
            AddPart(root, "ConstructionSideBrace", roadLineMaterial, building, 0.035f, 0.04f, depth * 0.42f, -width * 0.42f, Mathf.Max(0.3f, height * 0.55f), -depth * 0.18f);
            AddPart(root, "ConstructionBrickStack", serviceMaterial, building, width * 0.16f, 0.09f, depth * 0.12f, width * 0.28f, 0.15f, -depth * 0.42f);
            AddPart(root, "ConstructionCraneMast", serviceNeedMaterial, building, width * 0.045f, 0.52f, depth * 0.045f, width * 0.46f, height + 0.24f, -depth * 0.28f);
            AddPart(root, "ConstructionCraneArm", serviceNeedMaterial, building, width * 0.46f, 0.045f, depth * 0.035f, width * 0.28f, height + 0.52f, -depth * 0.28f);
            AddPart(root, "ConstructionCraneHook", roadLineMaterial, building, width * 0.04f, 0.2f, depth * 0.035f, width * 0.06f, height + 0.39f, -depth * 0.28f);
        }

        private void AddConstructionCleanupCues(GameObject root, PlacedBuilding building, float width, float height, float depth)
        {
            // CITY_CONSTRUCTION_STAGE_CLEANUP leaves a short-lived polish pass after the building opens.
            AddPart(root, "ConstructionCleanupPermitBoard", serviceNeedMaterial, building, width * 0.16f, 0.1f, depth * 0.035f, -width * 0.38f, 0.32f, -depth * 0.56f);
            AddPart(root, "ConstructionCleanupPermitPost", roadLineMaterial, building, width * 0.035f, 0.24f, depth * 0.035f, -width * 0.45f, 0.22f, -depth * 0.56f);
            AddPart(root, "ConstructionCleanupCurbPatch", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.36f, 0.035f, depth * 0.055f, 0f, 0.1f, -depth * 0.58f);
            AddPart(root, "ConstructionCleanupToolCrate", serviceMaterial, building, width * 0.13f, 0.09f, depth * 0.1f, width * 0.34f, 0.15f, -depth * 0.5f);
            if (height > 0.8f)
            {
                AddPart(root, "ConstructionFinalGlint", windowMaterial, building, width * 0.12f, 0.05f, depth * 0.035f, width * 0.22f, height + 0.13f, -depth * 0.48f);
            }
        }

        private static bool UsesEntryPaver(string modelKey)
        {
            return modelKey == "residential"
                || modelKey == "commercial"
                || modelKey == "mixed_use"
                || modelKey == "office"
                || modelKey == "innovation"
                || modelKey == "clinic"
                || modelKey == "school"
                || modelKey == "advanced_education"
                || modelKey == "administration";
        }

        private void AddBuildingZoneSkirt(GameObject root, PlacedBuilding building, ZoneType zone, float width, float depth)
        {
            // CITY_DISTRICT_ZONE_SKIRTS keeps the parcel's land-use color visible after buildings grow.
            if (zone == ZoneType.None)
            {
                return;
            }

            var material = MaterialForZone(zone);
            int faceX;
            int faceZ;
            GetBuildingRoadFace(building, out faceX, out faceZ);
            AddBuildingFacePart(root, "ZoneSkirtFront", material, building, faceX, faceZ, 0.82f, 0.035f, 0.055f, 0f, 0.065f, 0.53f, width, depth);
            AddBuildingFacePart(root, "ZoneSkirtSide", material, building, faceX, faceZ, 0.2f, 0.035f, 0.065f, -0.42f, 0.07f, 0.28f, width, depth);
            AddBuildingFacePart(root, "ZoneSkirtSide", material, building, faceX, faceZ, 0.2f, 0.035f, 0.065f, 0.42f, 0.07f, 0.28f, width, depth);
            AddBuildingFacePart(root, "ZoneParcelCornerTick", material, building, faceX, faceZ, 0.12f, 0.05f, 0.12f, -0.46f, 0.09f, 0.46f, width, depth);
        }

        private void AddSkylineFacadeDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth, int level)
        {
            // CITY_SKYLINE_FACADE_DETAILS gives the isometric city model readable windows and rooftops.
            if (IsLandscapeModel(modelKey))
            {
                AddPart(root, "LandscapePath", roadLineMaterial, building, width * 0.64f, 0.025f, depth * 0.12f, 0f, 0.055f, 0f);
                return;
            }

            if (IsUtilityModel(modelKey))
            {
                AddPart(root, "UtilityWarningBand", windowMaterial, building, width * 0.52f, 0.05f, depth * 0.08f, 0f, Mathf.Max(0.14f, height * 0.36f), depth * 0.48f);
                return;
            }

            var bandCount = Mathf.Clamp(level + (height > 1.25f ? 1 : 0), 1, 4);
            for (var i = 0; i < bandCount; i += 1)
            {
                var t = (i + 1f) / (bandCount + 1f);
                var y = Mathf.Max(0.18f, height * t);
                // ISOMETRIC_VISIBLE_FACADE_BANDS keeps skyline details on the camera-facing sides.
                AddPart(root, "SkylineWindowBandFront", windowMaterial, building, width * 0.58f, 0.045f, 0.025f, 0f, y, -depth * 0.465f);
                if (i % 2 == 0)
                {
                    AddPart(root, "SkylineWindowBandSide", windowMaterial, building, 0.025f, 0.045f, depth * 0.46f, -width * 0.465f, y, 0f);
                }
            }

            if (level >= 2 && !IsUtilityModel(modelKey))
            {
                AddPart(root, "SkylineVerticalWindowPillar", windowMaterial, building, width * 0.035f, height * 0.52f, 0.03f, width * 0.24f, height * 0.56f, -depth * 0.47f);
                AddPart(root, "SkylineCornerGlint", windowMaterial, building, 0.03f, height * 0.42f, depth * 0.035f, -width * 0.47f, height * 0.56f, -depth * 0.24f);
            }

            AddBuildingSunlitFacet(root, building, modelKey, width, height, depth, level);
            AddBuildingFacadeMicroPanels(root, building, modelKey, width, height, depth, level);

            if (height > 0.85f || modelKey == "office" || modelKey == "innovation" || modelKey == "landmark")
            {
                AddPart(root, "SkylineRooftopUnit", roofMaterial, building, width * 0.24f, 0.09f, depth * 0.24f, -width * 0.2f, height + 0.12f, -depth * 0.12f);
                AddPart(root, "SkylineRoofAccent", windowMaterial, building, width * 0.16f, 0.06f, depth * 0.16f, width * 0.18f, height + 0.16f, depth * 0.12f);
            }

            AddSkylineRoofDetails(root, building, modelKey, width, height, depth, level);
            AddCentralSkylineAccents(root, building, modelKey, width, height, depth);
            AddDistrictIdentityDetails(root, building, modelKey, width, height, depth);
        }

        private void AddBuildingSunlitFacet(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth, int level)
        {
            // REFERENCE_IMAGE_SUNLIT_FACETS gives taller blocks the bright isometric edge polish from the mockup.
            if (IsLandscapeModel(modelKey) || IsUtilityModel(modelKey))
            {
                return;
            }

            if (level < 2 && !IsCentralRoadTile(building.Pos) && height < 1.05f)
            {
                return;
            }

            var facetY = Mathf.Max(0.22f, height * 0.62f);
            AddPart(root, "SunlitFacadeFacetFront", roofMaterial, building, width * 0.28f, 0.05f, 0.028f, width * 0.2f, facetY, -depth * 0.505f);
            AddPart(root, "SunlitFacadeFacetSide", windowMaterial, building, 0.028f, 0.05f, depth * 0.26f, -width * 0.505f, Mathf.Max(0.2f, height * 0.48f), -depth * 0.18f);
            if (height > 1.2f || level >= 3)
            {
                AddPart(root, "SunlitRoofFacet", roofMaterial, building, width * 0.22f, 0.04f, depth * 0.08f, width * 0.22f, height + 0.105f, -depth * 0.22f);
            }
        }

        private void AddBuildingFacadeMicroPanels(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth, int level)
        {
            // LOW_POLY_BUILDING_FACADE_MICROPANELS add small lit facade tiles while keeping the block silhouette intact.
            if (IsLandscapeModel(modelKey) || IsUtilityModel(modelKey))
            {
                return;
            }

            var seed = DecorationHash(building.Pos.X, building.Pos.Y);
            var panelY = Mathf.Clamp(height * 0.42f, 0.24f, Mathf.Max(0.25f, height - 0.06f));
            var trimMaterial = seed % 3 == 0 ? roofMaterial : roadLineMaterial;
            AddPart(root, "LowPolyFacadeInsetPanelFront", trimMaterial, building, width * 0.18f, 0.042f, 0.026f, -width * 0.26f, panelY, -depth * 0.507f);
            AddPart(root, "LowPolyFacadeInsetPanelFront", windowMaterial, building, width * 0.14f, 0.035f, 0.028f, width * 0.28f, Mathf.Min(height + 0.02f, panelY + height * 0.18f), -depth * 0.51f);

            if (level >= 2 || seed % 4 == 0)
            {
                AddPart(root, "LowPolyFacadeSideInset", windowMaterial, building, 0.026f, 0.04f, depth * 0.16f, -width * 0.51f, Mathf.Max(0.22f, height * 0.68f), depth * 0.2f);
                AddPart(root, "LowPolyFacadeShadowNotch", buildingFootprintMaterial, building, width * 0.16f, 0.028f, 0.024f, -width * 0.06f, Mathf.Max(0.2f, height * 0.28f), -depth * 0.512f);
            }
        }

        private void AddSkylineRoofDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth, int level)
        {
            // CITY_SKYLINE_ROOF_RIMS_AND_GREENROOFS adds readable low-poly roof layers without extra assets.
            var roofY = height + 0.055f;
            AddPart(root, "SkylineRoofFrontRim", roofMaterial, building, width * 0.78f, 0.045f, depth * 0.05f, 0f, roofY, -depth * 0.49f);
            AddPart(root, "SkylineRoofSideRim", roofMaterial, building, width * 0.05f, 0.045f, depth * 0.62f, -width * 0.49f, roofY, -depth * 0.02f);
            AddBuildingRoofFacetTiles(root, building, modelKey, width, height, depth, level, roofY);

            if (modelKey == "residential" || modelKey == "commercial" || modelKey == "mixed_use" || modelKey == "office" || modelKey == "innovation")
            {
                var patchWidth = modelKey == "office" || modelKey == "innovation" ? width * 0.28f : width * 0.36f;
                AddPart(root, "RooftopGreenPatch", treeCanopyMaterial, building, patchWidth, 0.04f, depth * 0.22f, width * 0.12f, roofY + 0.035f, -depth * 0.16f);
            }

            if (modelKey == "office" || modelKey == "innovation" || modelKey == "administration" || modelKey == "school" || modelKey == "clinic" || level >= 2)
            {
                AddPart(root, "RooftopSolarPatch", windowMaterial, building, width * 0.3f, 0.035f, depth * 0.16f, -width * 0.16f, roofY + 0.045f, depth * 0.14f);
            }

            if (level >= 2 && !IsLandscapeModel(modelKey))
            {
                // CITY_SKYLINE_VERTICAL_GROWTH_CROWNS makes upgraded buildings visibly mature on the map.
                AddPart(root, "GrowthRoofStep", serviceMaterial, building, width * 0.42f, 0.055f, depth * 0.18f, 0f, roofY + 0.075f, depth * 0.02f);
                AddPart(root, "GrowthRoofGlint", windowMaterial, building, width * 0.14f, 0.055f, depth * 0.055f, width * 0.25f, roofY + 0.115f, -depth * 0.2f);
            }

            if (level >= 3 && !IsLandscapeModel(modelKey))
            {
                AddPart(root, "GrowthSkylineCrown", roofMaterial, building, width * 0.26f, 0.08f, depth * 0.24f, 0f, roofY + 0.17f, 0f);
                AddPart(root, "GrowthCrownBeacon", windowMaterial, building, width * 0.08f, 0.16f, depth * 0.08f, 0f, roofY + 0.28f, 0f);
            }

            AddSkylineRoofMicroDecor(root, building, modelKey, width, height, depth, level, roofY);
        }

        private void AddBuildingRoofFacetTiles(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth, int level, float roofY)
        {
            // LOW_POLY_ROOF_FACET_TILES add bright roof seams and tiny terrace pads to flat tops.
            if (IsLandscapeModel(modelKey))
            {
                return;
            }

            var seed = DecorationHash(building.Pos.X, building.Pos.Y);
            var seamMaterial = seed % 2 == 0 ? roadLineMaterial : roofMaterial;
            AddPart(root, "LowPolyRoofFacetTile", seamMaterial, building, width * 0.22f, 0.026f, depth * 0.055f, -width * 0.23f, roofY + 0.04f, -depth * 0.26f);
            AddPart(root, "LowPolyRoofFacetTile", windowMaterial, building, width * 0.12f, 0.024f, depth * 0.048f, width * 0.28f, roofY + 0.048f, depth * 0.22f);

            if (level >= 2 || height > 1f)
            {
                AddPart(root, "LowPolyRoofTerracePad", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.28f, 0.028f, depth * 0.12f, -width * 0.08f, roofY + 0.055f, depth * 0.28f);
            }
        }

        private void AddSkylineRoofMicroDecor(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth, int level, float roofY)
        {
            // LOW_POLY_ROOF_MICRO_DECOR adds small vents, flags, and glints to break up flat roofs.
            if (IsLandscapeModel(modelKey))
            {
                return;
            }

            var seed = DecorationHash(building.Pos.X, building.Pos.Y);
            if (seed % 2 == 0 || level >= 2)
            {
                AddPart(root, "RooftopBrightVent", roadLineMaterial, building, width * 0.12f, 0.07f, depth * 0.1f, width * 0.31f, roofY + 0.075f, depth * 0.24f);
                AddPart(root, "RooftopVentGlint", windowMaterial, building, width * 0.08f, 0.032f, depth * 0.035f, width * 0.31f, roofY + 0.13f, depth * 0.18f);
            }

            if (seed % 3 == 0 && height > 0.62f)
            {
                AddPart(root, "RooftopTinyFlagPost", serviceMaterial, building, width * 0.035f, 0.18f, depth * 0.035f, -width * 0.31f, roofY + 0.14f, -depth * 0.24f);
                AddPart(root, "RooftopTinyFlag", serviceNeedMaterial, building, width * 0.12f, 0.055f, depth * 0.032f, -width * 0.24f, roofY + 0.22f, -depth * 0.24f);
            }

            if ((seed & 5) == 5)
            {
                AddPart(root, "RooftopServiceHatch", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.18f, 0.032f, depth * 0.14f, -width * 0.14f, roofY + 0.04f, depth * 0.28f);
            }
        }

        private void AddCentralSkylineAccents(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // REFERENCE_IMAGE_CENTRAL_SKYLINE_POP gives downtown buildings the crisp roof crowns and lit edges in the mockup.
            if (!IsCentralRoadTile(building.Pos) || !IsCentralSkylineModel(modelKey))
            {
                return;
            }

            var roofY = height + 0.11f;
            AddPart(root, "CentralSkylinePodiumFrontLip", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.86f, 0.045f, depth * 0.055f, 0f, 0.16f, -depth * 0.55f);
            AddPart(root, "CentralSkylinePodiumSideLip", shoreMaterial != null ? shoreMaterial : roadLineMaterial, building, width * 0.055f, 0.045f, depth * 0.72f, -width * 0.55f, 0.16f, 0f);
            AddPart(root, "CentralSkylineFrontLightSpine", windowMaterial, building, width * 0.035f, Mathf.Max(0.24f, height * 0.58f), depth * 0.035f, width * 0.34f, height * 0.6f, -depth * 0.5f);
            AddPart(root, "CentralSkylineSideLightSpine", windowMaterial, building, width * 0.035f, Mathf.Max(0.2f, height * 0.48f), depth * 0.035f, -width * 0.5f, height * 0.56f, depth * 0.32f);
            AddPart(root, "CentralSkylineRoofCrown", roofMaterial, building, width * 0.34f, 0.075f, depth * 0.28f, 0f, roofY + 0.09f, 0f);
            AddPart(root, "CentralSkylineRoofGlow", windowMaterial, building, width * 0.16f, 0.05f, depth * 0.1f, width * 0.18f, roofY + 0.17f, -depth * 0.14f);
            AddPart(root, "CentralSkylineSetbackBlock", serviceMaterial, building, width * 0.24f, 0.16f, depth * 0.22f, -width * 0.08f, roofY + 0.2f, depth * 0.04f);
            AddPart(root, "CentralSkylineSetbackGlint", windowMaterial, building, width * 0.1f, 0.055f, depth * 0.06f, width * 0.06f, roofY + 0.25f, -depth * 0.1f);

            if (modelKey == "commercial" || modelKey == "mixed_use")
            {
                AddPart(root, "CentralStorefrontGoldAwning", serviceNeedMaterial, building, width * 0.52f, 0.055f, depth * 0.09f, 0f, Mathf.Max(0.26f, height * 0.34f), -depth * 0.58f);
            }

            if (height > 1.05f || modelKey == "landmark")
            {
                AddPart(root, "CentralSkylineNeedleBeacon", windowMaterial, building, width * 0.06f, 0.28f, depth * 0.06f, -width * 0.16f, roofY + 0.3f, depth * 0.08f);
            }

            AddCentralSkylineLayering(root, building, modelKey, width, height, depth);
        }

        private void AddCentralSkylineLayering(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // CITY_SKYLINES_CENTER_LAYERING adds mid-rise terraces and stacked roof detail to downtown blocks.
            var ledgeMaterial = shoreMaterial != null ? shoreMaterial : roadLineMaterial;
            var midY = Mathf.Max(0.36f, height * 0.52f);
            AddPart(root, "CentralSkylineMidTerraceFront", ledgeMaterial, building, width * 0.56f, 0.045f, depth * 0.05f, -width * 0.06f, midY, -depth * 0.54f);
            AddPart(root, "CentralSkylineMidTerraceSide", ledgeMaterial, building, width * 0.05f, 0.045f, depth * 0.44f, -width * 0.54f, midY * 0.92f, -depth * 0.08f);
            AddPart(root, "CentralSkylineTerraceGreen", treeCanopyMaterial, building, width * 0.22f, 0.045f, depth * 0.12f, width * 0.18f, midY + 0.045f, -depth * 0.34f);
            AddPart(root, "CentralSkylineTerraceGlow", windowMaterial, building, width * 0.12f, 0.04f, depth * 0.045f, -width * 0.26f, midY + 0.065f, -depth * 0.5f);

            if (height > 1f || modelKey == "landmark")
            {
                AddPart(root, "CentralSkylineUpperSetback", serviceMaterial, building, width * 0.26f, 0.18f, depth * 0.24f, width * 0.12f, height + 0.2f, depth * 0.02f);
                AddPart(root, "CentralSkylineUpperWindowBand", windowMaterial, building, width * 0.18f, 0.04f, depth * 0.035f, width * 0.12f, height + 0.23f, -depth * 0.12f);
            }

            if (modelKey == "office" || modelKey == "innovation" || modelKey == "landmark")
            {
                AddPart(root, "CentralSkylineRoofPlantBox", grassGridMaterial, building, width * 0.24f, 0.05f, depth * 0.12f, -width * 0.28f, height + 0.18f, depth * 0.22f);
                AddPart(root, "CentralSkylineAntennaMast", serviceMaterial, building, width * 0.035f, 0.22f, depth * 0.035f, width * 0.3f, height + 0.28f, depth * 0.18f);
                AddPart(root, "CentralSkylineAntennaTip", windowMaterial, building, width * 0.075f, 0.045f, depth * 0.075f, width * 0.3f, height + 0.42f, depth * 0.18f);
            }
        }

        private static bool IsCentralSkylineModel(string modelKey)
        {
            return modelKey == "office"
                || modelKey == "commercial"
                || modelKey == "mixed_use"
                || modelKey == "innovation"
                || modelKey == "landmark"
                || modelKey == "administration";
        }

        private void AddDistrictIdentityDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // CITY_DISTRICT_IDENTITY_DETAILS adds small readable cues for each building family.
            if (modelKey == "residential")
            {
                // REFERENCE_IMAGE_BUILDING_IDENTITY_TRIMS gives homes front porches and chimney silhouettes.
                AddPart(root, "ResidentialPorchStep", roadLineMaterial, building, width * 0.3f, 0.045f, depth * 0.14f, 0f, 0.12f, -depth * 0.55f);
                AddPart(root, "ResidentialFlowerBox", windowMaterial, building, width * 0.2f, 0.055f, depth * 0.035f, -width * 0.18f, Mathf.Max(0.24f, height * 0.48f), -depth * 0.53f);
                AddPart(root, "ResidentialChimney", serviceMaterial, building, width * 0.08f, 0.24f, depth * 0.08f, width * 0.24f, height + 0.16f, depth * 0.12f);
                return;
            }

            if (modelKey == "commercial" || modelKey == "mixed_use")
            {
                AddPart(root, "StorefrontAwning", roofMaterial, building, width * 0.62f, 0.055f, depth * 0.1f, 0f, Mathf.Max(0.2f, height * 0.44f), -depth * 0.53f);
                AddPart(root, "StorefrontSignBlade", windowMaterial, building, width * 0.14f, 0.12f, depth * 0.035f, width * 0.32f, Mathf.Max(0.27f, height * 0.52f), -depth * 0.56f);
                AddPart(root, "StorefrontPlanterBox", treeCanopyMaterial, building, width * 0.34f, 0.045f, depth * 0.045f, -width * 0.18f, 0.145f, -depth * 0.56f);
                return;
            }

            if (modelKey == "transit" || modelKey == "intercity" || modelKey == "freight_rail" || modelKey == "logistics")
            {
                // CITY_NODE_TRANSIT_IDENTITY marks stations and freight hubs as readable city nodes.
                AddPart(root, "PlatformGuideStripe", roadLineMaterial, building, width * 0.72f, 0.035f, depth * 0.05f, 0f, 0.18f, -depth * 0.5f);
                AddPart(root, "TransitTransferPavers", roadLineMaterial, building, width * 0.48f, 0.035f, depth * 0.18f, -width * 0.05f, 0.13f, -depth * 0.58f);
                AddPart(root, "TransitNodePylon", serviceMaterial, building, width * 0.08f, 0.38f, depth * 0.08f, -width * 0.36f, 0.3f, -depth * 0.42f);
                AddPart(root, "TransitNodePylon", serviceMaterial, building, width * 0.08f, 0.38f, depth * 0.08f, width * 0.36f, 0.3f, -depth * 0.42f);
                AddPart(root, "TransitStopCanopy", roofMaterial, building, width * 0.42f, 0.06f, depth * 0.14f, 0f, 0.5f, -depth * 0.42f);
                return;
            }

            if (modelKey == "landmark")
            {
                // CITY_NODE_LANDMARK_IDENTITY gives civic anchors readable plaza and beacon cues.
                // REFERENCE_IMAGE_LANDMARK_VERTICAL_HIGHLIGHTS makes landmark towers pop in the low-poly city.
                AddLandmarkVerticalHighlights(root, building, width, height, depth);
                AddPart(root, "LandmarkPlazaAxis", roadLineMaterial, building, width * 0.62f, 0.035f, depth * 0.08f, 0f, 0.13f, -depth * 0.58f);
                AddPart(root, "LandmarkCrownGlint", windowMaterial, building, width * 0.24f, 0.05f, depth * 0.08f, 0f, height * 1.42f + 0.12f, -depth * 0.18f);
                AddPart(root, "LandmarkBeaconSpire", windowMaterial, building, width * 0.08f, 0.38f, depth * 0.08f, 0f, height * 1.42f + 0.34f, 0f);
                return;
            }

            if (modelKey == "clinic" || modelKey == "school" || modelKey == "advanced_education" || modelKey == "administration")
            {
                AddPart(root, "PublicEntrySteps", roadLineMaterial, building, width * 0.32f, 0.045f, depth * 0.16f, 0f, 0.12f, -depth * 0.55f);
                AddPart(root, "PublicEntryCanopy", roofMaterial, building, width * 0.36f, 0.055f, depth * 0.1f, 0f, Mathf.Max(0.26f, height * 0.36f), -depth * 0.52f);
                AddPart(root, "PublicServiceBadge", windowMaterial, building, width * 0.12f, 0.09f, depth * 0.04f, width * 0.22f, Mathf.Max(0.32f, height * 0.58f), -depth * 0.53f);
                AddPublicServiceIdentityDetails(root, building, modelKey, width, height, depth);
                return;
            }

            if (modelKey == "communications" || modelKey == "mail")
            {
                AddCommunicationsIdentityDetails(root, building, modelKey, width, height, depth);
                return;
            }

            if (modelKey == "safety" || modelKey == "security" || modelKey == "shelter" || modelKey == "road_maintenance")
            {
                AddResponseIdentityDetails(root, building, modelKey, width, height, depth);
                return;
            }

            if (modelKey == "industrial" || modelKey == "resource" || modelKey == "warehouse")
            {
                AddPart(root, "LoadingApron", roadMaterial, building, width * 0.42f, 0.035f, depth * 0.16f, -width * 0.24f, 0.11f, -depth * 0.54f);
                AddPart(root, "LoadingDoorStripe", roadLineMaterial, building, width * 0.24f, 0.045f, depth * 0.045f, -width * 0.24f, 0.22f, -depth * 0.56f);
                AddPart(root, "IndustrialRoofVent", serviceMaterial, building, width * 0.1f, 0.16f, depth * 0.1f, width * 0.2f, height + 0.12f, -depth * 0.12f);
                return;
            }

            if (modelKey == "parking")
            {
                AddParkingIdentityDetails(root, building, width, height, depth);
                return;
            }

            if (IsUtilityModel(modelKey))
            {
                AddUtilityIdentityDetails(root, building, modelKey, width, height, depth);
            }
        }

        private void AddFormalPrefabReplacementDetails(GameObject root, PlacedBuilding building, BuildingDefinition definition, string modelKey, float width, float height, float depth, int level)
        {
            // FORMAL_BUILDING_PREFAB_REPLACEMENT adds production identity details while keeping WebGL export procedural.
            var id = definition != null ? definition.Id : string.Empty;

            if (id == "apartment_block")
            {
                AddPart(root, "ApartmentBalconyRail", roadLineMaterial, building, width * 0.54f, 0.035f, depth * 0.035f, 0f, Mathf.Max(0.38f, height * 0.62f), -depth * 0.57f);
                AddPart(root, "ApartmentBalconyPlanter", treeCanopyMaterial, building, width * 0.2f, 0.055f, depth * 0.04f, width * 0.22f, Mathf.Max(0.42f, height * 0.62f), -depth * 0.59f);
                if (level >= 2)
                {
                    AddPart(root, "ApartmentRoofGarden", grassGridMaterial, building, width * 0.38f, 0.04f, depth * 0.18f, -width * 0.06f, height + 0.19f, depth * 0.12f);
                }

                return;
            }

            if (id == "district_hospital")
            {
                AddPart(root, "HospitalHelipadRing", roadLineMaterial, building, width * 0.32f, 0.035f, depth * 0.32f, width * 0.18f, height + 0.22f, depth * 0.06f);
                AddPart(root, "HospitalHelipadCrossA", trafficPulseMaterial, building, width * 0.22f, 0.04f, depth * 0.045f, width * 0.18f, height + 0.26f, depth * 0.06f);
                AddPart(root, "HospitalHelipadCrossB", trafficPulseMaterial, building, width * 0.045f, 0.04f, depth * 0.22f, width * 0.18f, height + 0.26f, depth * 0.06f);
                AddPart(root, "HospitalAmbulanceBay", roadMaterial, building, width * 0.28f, 0.08f, depth * 0.12f, -width * 0.26f, 0.18f, -depth * 0.56f);
                return;
            }

            if (id == "research_campus")
            {
                AddPart(root, "ResearchDomeBase", windowMaterial, building, width * 0.26f, 0.1f, depth * 0.26f, -width * 0.22f, height + 0.18f, depth * 0.08f);
                AddPart(root, "ResearchDomeCap", roofMaterial, building, width * 0.18f, 0.09f, depth * 0.18f, -width * 0.22f, height + 0.27f, depth * 0.08f);
                AddPart(root, "ResearchBeacon", serviceNeedMaterial, building, width * 0.07f, 0.16f, depth * 0.07f, width * 0.24f, height + 0.25f, -depth * 0.12f);
                return;
            }

            if (id == "convention_center")
            {
                AddPart(root, "ConventionGrandCanopy", roofMaterial, building, width * 0.72f, 0.075f, depth * 0.22f, 0f, Mathf.Max(0.34f, height * 0.5f), -depth * 0.55f);
                AddPart(root, "ConventionQueuePlaza", roadLineMaterial, building, width * 0.62f, 0.03f, depth * 0.22f, 0f, 0.13f, -depth * 0.64f);
                AddPart(root, "ConventionBannerBlade", serviceNeedMaterial, building, width * 0.12f, 0.28f, depth * 0.04f, width * 0.38f, Mathf.Max(0.4f, height * 0.68f), -depth * 0.58f);
                return;
            }

            if (id == "intercity_terminal")
            {
                AddPart(root, "IntercityRoofSweep", roofMaterial, building, width * 0.78f, 0.08f, depth * 0.22f, 0f, height * 0.72f, -depth * 0.14f);
                AddPart(root, "IntercityGateLine", roadLineMaterial, building, width * 0.62f, 0.035f, depth * 0.05f, 0f, 0.16f, -depth * 0.6f);
                AddPart(root, "IntercityPylonLight", windowMaterial, building, width * 0.08f, 0.32f, depth * 0.08f, -width * 0.38f, 0.36f, -depth * 0.42f);
                return;
            }

            if (id == "freight_rail_terminal")
            {
                AddPart(root, "FreightRailTrackA", roadMaterial, building, width * 0.84f, 0.035f, depth * 0.045f, 0f, 0.12f, depth * 0.5f);
                AddPart(root, "FreightRailTrackB", roadMaterial, building, width * 0.84f, 0.035f, depth * 0.045f, 0f, 0.12f, depth * 0.62f);
                AddPart(root, "FreightRailCraneHook", serviceNeedMaterial, building, width * 0.045f, 0.28f, depth * 0.045f, width * 0.28f, height * 0.92f, depth * 0.1f);
                return;
            }

            if (modelKey == "deathcare")
            {
                AddPart(root, "MemorialReflectionPool", windowMaterial, building, width * 0.24f, 0.03f, depth * 0.18f, width * 0.12f, 0.13f, -depth * 0.16f);
                AddPart(root, "MemorialBloomRow", serviceNeedMaterial, building, width * 0.38f, 0.045f, depth * 0.05f, -width * 0.08f, 0.15f, depth * 0.34f);
                return;
            }

            if (modelKey == "stormwater")
            {
                AddPart(root, "StormwaterReedBed", treeCanopyMaterial, building, width * 0.44f, 0.12f, depth * 0.12f, -width * 0.08f, 0.2f, -depth * 0.2f);
                AddPart(root, "StormwaterInletBlue", windowMaterial, building, width * 0.18f, 0.04f, depth * 0.24f, width * 0.26f, 0.13f, depth * 0.2f);
                return;
            }

            if (modelKey == "power")
            {
                AddPart(root, "PowerTransformerCoilA", serviceNeedMaterial, building, width * 0.11f, 0.22f, depth * 0.11f, -width * 0.18f, 0.34f, -depth * 0.16f);
                AddPart(root, "PowerTransformerCoilB", serviceNeedMaterial, building, width * 0.11f, 0.22f, depth * 0.11f, width * 0.02f, 0.34f, -depth * 0.16f);
                AddPart(root, "PowerSafeFence", roadLineMaterial, building, width * 0.52f, 0.05f, depth * 0.045f, 0f, 0.17f, -depth * 0.58f);
            }
        }

        private void AddPublicServiceIdentityDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // CITY_SKYLINES_PUBLIC_SERVICE_ICONS make civic buildings identifiable without textures.
            if (modelKey == "clinic")
            {
                AddPart(root, "ClinicCrossHorizontal", trafficPulseMaterial, building, width * 0.24f, 0.045f, depth * 0.035f, 0f, Mathf.Max(0.36f, height * 0.64f), -depth * 0.56f);
                AddPart(root, "ClinicCrossVertical", trafficPulseMaterial, building, width * 0.07f, 0.17f, depth * 0.035f, 0f, Mathf.Max(0.36f, height * 0.64f), -depth * 0.565f);
                return;
            }

            if (modelKey == "school")
            {
                AddPart(root, "SchoolFlagPost", roadLineMaterial, building, width * 0.045f, 0.34f, depth * 0.045f, -width * 0.34f, 0.35f, -depth * 0.42f);
                AddPart(root, "SchoolFlag", serviceMaterial, building, width * 0.2f, 0.08f, depth * 0.035f, -width * 0.25f, 0.48f, -depth * 0.44f);
                AddPart(root, "SchoolYardLine", roadLineMaterial, building, width * 0.46f, 0.028f, depth * 0.045f, width * 0.05f, 0.105f, -depth * 0.64f);
                return;
            }

            if (modelKey == "advanced_education")
            {
                AddPart(root, "CampusBookLeft", windowMaterial, building, width * 0.18f, 0.055f, depth * 0.2f, -width * 0.09f, height + 0.19f, -depth * 0.1f);
                AddPart(root, "CampusBookRight", windowMaterial, building, width * 0.18f, 0.055f, depth * 0.2f, width * 0.1f, height + 0.19f, -depth * 0.1f);
                AddPart(root, "CampusQuadPath", roadLineMaterial, building, width * 0.42f, 0.028f, depth * 0.055f, 0f, 0.105f, -depth * 0.64f);
                return;
            }

            AddPart(root, "CivicColumnLeft", roadLineMaterial, building, width * 0.055f, 0.28f, depth * 0.045f, -width * 0.18f, 0.28f, -depth * 0.56f);
            AddPart(root, "CivicColumnCenter", roadLineMaterial, building, width * 0.055f, 0.28f, depth * 0.045f, 0f, 0.28f, -depth * 0.56f);
            AddPart(root, "CivicColumnRight", roadLineMaterial, building, width * 0.055f, 0.28f, depth * 0.045f, width * 0.18f, 0.28f, -depth * 0.56f);
            AddPart(root, "CivicSeal", serviceMaterial, building, width * 0.16f, 0.05f, depth * 0.16f, 0f, height + 0.18f, -depth * 0.08f);
        }

        private void AddCommunicationsIdentityDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // CITY_SKYLINES_COMMS_MAIL_ICONS split mail and telecom nodes in the city silhouette.
            if (modelKey == "mail")
            {
                AddPart(root, "MailEnvelopeFlap", roadLineMaterial, building, width * 0.28f, 0.04f, depth * 0.035f, 0f, Mathf.Max(0.28f, height * 0.48f), -depth * 0.55f);
                AddPart(root, "MailSignalFlagPost", serviceMaterial, building, width * 0.045f, 0.32f, depth * 0.045f, width * 0.31f, 0.35f, -depth * 0.36f);
                AddPart(root, "MailSignalFlag", windowMaterial, building, width * 0.18f, 0.07f, depth * 0.035f, width * 0.39f, 0.47f, -depth * 0.38f);
                return;
            }

            AddPart(root, "CommsSignalWaveNear", windowMaterial, building, width * 0.28f, 0.045f, depth * 0.04f, 0f, height * 1.44f + 0.16f, -depth * 0.1f);
            AddPart(root, "CommsSignalWaveFar", windowMaterial, building, width * 0.42f, 0.045f, depth * 0.04f, 0f, height * 1.44f + 0.28f, -depth * 0.16f);
        }

        private void AddResponseIdentityDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // CITY_SKYLINES_RESPONSE_BUILDING_ICONS distinguish fire, police, shelter, and road maintenance.
            if (modelKey == "safety")
            {
                AddPart(root, "SafetyRedLightBar", trafficPulseMaterial, building, width * 0.28f, 0.055f, depth * 0.055f, 0f, Mathf.Max(0.32f, height * 0.68f), -depth * 0.52f);
                AddPart(root, "SafetyHoseStripe", roadLineMaterial, building, width * 0.42f, 0.04f, depth * 0.045f, 0f, 0.19f, -depth * 0.56f);
                return;
            }

            if (modelKey == "security")
            {
                AddPart(root, "SecurityBlueLightBar", windowMaterial, building, width * 0.28f, 0.055f, depth * 0.055f, 0f, Mathf.Max(0.32f, height * 0.68f), -depth * 0.52f);
                AddPart(root, "SecurityShieldPlate", roadLineMaterial, building, width * 0.18f, 0.13f, depth * 0.045f, -width * 0.22f, Mathf.Max(0.28f, height * 0.44f), -depth * 0.56f);
                return;
            }

            if (modelKey == "shelter")
            {
                AddPart(root, "ShelterRoofMarker", roofMaterial, building, width * 0.36f, 0.07f, depth * 0.2f, 0f, height + 0.17f, -depth * 0.08f);
                AddPart(root, "ShelterSafeDoor", windowMaterial, building, width * 0.2f, 0.19f, depth * 0.045f, 0f, 0.24f, -depth * 0.56f);
                return;
            }

            AddPart(root, "RoadMaintenanceWrenchHandle", roadLineMaterial, building, width * 0.36f, 0.05f, depth * 0.055f, -width * 0.02f, Mathf.Max(0.32f, height * 0.58f), -depth * 0.56f);
            AddPart(root, "RoadMaintenanceWrenchHead", serviceMaterial, building, width * 0.12f, 0.12f, depth * 0.055f, width * 0.18f, Mathf.Max(0.34f, height * 0.62f), -depth * 0.56f);
        }

        private void AddParkingIdentityDetails(GameObject root, PlacedBuilding building, float width, float height, float depth)
        {
            // REFERENCE_IMAGE_PARKING_IDENTITY gives parking buildings a readable map icon without texture assets.
            var deckY = Mathf.Max(0.28f, height * 0.82f);
            AddPart(root, "ParkingRoofBayLine", roadLineMaterial, building, width * 0.72f, 0.035f, depth * 0.045f, 0f, deckY, -depth * 0.18f);
            AddPart(root, "ParkingRoofBayLine", roadLineMaterial, building, width * 0.72f, 0.035f, depth * 0.045f, 0f, deckY, depth * 0.02f);
            AddPart(root, "ParkingRampArrow", windowMaterial, building, width * 0.18f, 0.045f, depth * 0.08f, width * 0.22f, Mathf.Max(0.2f, height * 0.34f), depth * 0.5f);
            AddPart(root, "ParkingPylonPost", serviceMaterial, building, width * 0.07f, 0.42f, depth * 0.07f, -width * 0.36f, 0.36f, -depth * 0.48f);
            AddPart(root, "ParkingPylonPlate", windowMaterial, building, width * 0.26f, 0.22f, depth * 0.045f, -width * 0.36f, 0.62f, -depth * 0.5f);
            AddPart(root, "ParkingPMarkStem", roadMaterial, building, width * 0.045f, 0.16f, depth * 0.035f, -width * 0.41f, 0.64f, -depth * 0.535f);
            AddPart(root, "ParkingPMarkTop", roadMaterial, building, width * 0.13f, 0.045f, depth * 0.035f, -width * 0.35f, 0.69f, -depth * 0.535f);
            AddPart(root, "ParkingPMarkMid", roadMaterial, building, width * 0.11f, 0.04f, depth * 0.035f, -width * 0.35f, 0.625f, -depth * 0.535f);
        }

        private void AddUtilityIdentityDetails(GameObject root, PlacedBuilding building, string modelKey, float width, float height, float depth)
        {
            // CITY_UTILITY_NODE_IDENTITY makes power, water, waste, and stormwater nodes legible in the base view.
            AddPart(root, "UtilityPipeRun", roadLineMaterial, building, width * 0.62f, 0.045f, depth * 0.055f, 0f, 0.16f, -depth * 0.55f);
            AddPart(root, "UtilityStatusLamp", windowMaterial, building, width * 0.09f, 0.13f, depth * 0.09f, width * 0.34f, Mathf.Max(0.28f, height * 0.56f), -depth * 0.18f);
            AddPart(root, "UtilityServiceDot", serviceMaterial, building, width * 0.07f, 0.1f, depth * 0.07f, width * 0.22f, Mathf.Max(0.24f, height * 0.46f), -depth * 0.32f);

            if (modelKey == "solar")
            {
                AddPart(root, "SolarArrayGlint", windowMaterial, building, width * 0.42f, 0.035f, depth * 0.16f, 0f, height * 0.74f, depth * 0.18f);
                AddPart(root, "SolarArrayGlint", windowMaterial, building, width * 0.34f, 0.035f, depth * 0.14f, -width * 0.18f, height * 0.84f, -depth * 0.12f);
                return;
            }

            if (modelKey == "water" || modelKey == "sewage" || modelKey == "stormwater")
            {
                AddPart(root, "UtilityBlueGauge", windowMaterial, building, width * 0.18f, 0.05f, depth * 0.18f, -width * 0.22f, height * 0.86f, 0f);
                AddPart(root, "UtilityFlowStripe", windowMaterial, building, width * 0.12f, 0.045f, depth * 0.42f, width * 0.26f, 0.19f, depth * 0.12f);
                return;
            }

            if (modelKey == "recycling" || modelKey == "waste_to_energy")
            {
                AddPart(root, "UtilityRecycleBin", treeCanopyMaterial, building, width * 0.18f, 0.16f, depth * 0.18f, -width * 0.28f, 0.22f, -depth * 0.42f);
                AddPart(root, "UtilityServiceHatch", roadMaterial, building, width * 0.2f, 0.045f, depth * 0.16f, width * 0.14f, 0.19f, -depth * 0.44f);
                return;
            }

            AddPart(root, "UtilityPowerBus", serviceMaterial, building, width * 0.12f, 0.32f, depth * 0.12f, -width * 0.34f, 0.32f, depth * 0.22f);
            AddPart(root, "UtilityPowerBar", windowMaterial, building, width * 0.3f, 0.045f, depth * 0.055f, -width * 0.24f, 0.5f, depth * 0.22f);
        }

        private void AddLandmarkVerticalHighlights(GameObject root, PlacedBuilding building, float width, float height, float depth)
        {
            AddPart(root, "LandmarkVerticalHighlightFront", windowMaterial, building, width * 0.055f, height * 0.84f, depth * 0.045f, -width * 0.16f, height * 0.78f, -depth * 0.225f);
            AddPart(root, "LandmarkVerticalHighlightSide", windowMaterial, building, width * 0.045f, height * 0.72f, depth * 0.055f, -width * 0.225f, height * 0.72f, depth * 0.12f);
        }

        private static bool IsLandscapeModel(string modelKey)
        {
            return modelKey == "park" || modelKey == "plaza" || modelKey == "deathcare";
        }

        private static bool IsUtilityModel(string modelKey)
        {
            return modelKey == "power" || modelKey == "solar" || modelKey == "water" || modelKey == "sewage" || modelKey == "recycling" || modelKey == "waste_to_energy" || modelKey == "stormwater";
        }

        private GameObject FallbackCubeVisual(GameObject root, PlacedBuilding building, Material material, float width, float depth, float height)
        {
            AddPart(root, "FallbackCubeVisual", material, building, width, height, depth, 0f, height * 0.5f, 0f);
            return root;
        }

        private void AddPart(GameObject root, string name, Material material, PlacedBuilding building, float width, float height, float depth, float offsetX, float centerY, float offsetZ)
        {
            var part = CreateCube(name, material);
            part.transform.SetParent(root.transform, false);
            var originX = (building.Pos.X + building.Size.W * 0.5f) * cellSize;
            var originZ = (building.Pos.Y + building.Size.H * 0.5f) * cellSize;
            part.transform.localPosition = new Vector3(originX + offsetX, roadHeight + centerY, originZ + offsetZ);
            part.transform.localScale = new Vector3(Mathf.Max(0.05f, width), Mathf.Max(0.05f, height), Mathf.Max(0.05f, depth));
        }

        private Color32 TerrainColorForTile(int x, int y)
        {
            var tile = controller.GetTile(x, y);
            if (tile == null)
            {
                return new Color32(0, 0, 0, 0);
            }

            var shade = ((x * 37 + y * 19) % 7) - 3;
            if (tile.Terrain == TerrainType.Water) return ShiftColor(new Color32(104, 222, 246, 255), shade);
            if (tile.Terrain == TerrainType.Hill) return ShiftColor(new Color32(198, 224, 145, 255), shade);
            var baseColor = new Color32(158, 232, 146, 255);
            if (string.IsNullOrEmpty(tile.RoadId))
            {
                baseColor = BlendColor(baseColor, ZoneTerrainTint(tile.Zone), ZoneTerrainTintStrength(tile.Zone));
            }

            return ShiftColor(baseColor, shade);
        }

        private Color32 OverlayColorForTile(int x, int y)
        {
            return controller.GetOverlayColor(x, y);
        }

        private Color32 ReadableOverlayColorForTile(int x, int y)
        {
            // CITY_SKYLINES_FACETED_OVERLAY keeps heatmaps readable while matching the low-poly terrain lighting.
            var color = OverlayColorForTile(x, y);
            if (color.a == 0)
            {
                return color;
            }

            var alpha = color.a;
            var lift = alpha >= 150 ? 8 : 5;
            return new Color32(
                (byte)Mathf.Clamp(color.r + lift, 0, 255),
                (byte)Mathf.Clamp(color.g + lift, 0, 255),
                (byte)Mathf.Clamp(color.b + lift, 0, 255),
                alpha);
        }

        private Vector3 CellCenter(GridPos pos, float y)
        {
            return new Vector3((pos.X + 0.5f) * cellSize, y, (pos.Y + 0.5f) * cellSize);
        }

        private GameObject CreateCube(string name, Material material)
        {
            var obj = new GameObject(name);
            var filter = obj.AddComponent<MeshFilter>();
            filter.sharedMesh = GetCubeMesh();

            var renderer = obj.AddComponent<MeshRenderer>();
            obj.name = name;
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return obj;
        }

        private Mesh GetCubeMesh()
        {
            if (cubeMesh != null)
            {
                return cubeMesh;
            }

            cubeMesh = new Mesh { name = "PocketCityPhysicsFreeCube" };
            cubeMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            };
            cubeMesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23,
            };
            cubeMesh.RecalculateNormals();
            cubeMesh.RecalculateBounds();
            return cubeMesh;
        }

        private void EnsureMeshLayer(string layerName, float y, ref MeshFilter filter, ref Mesh mesh)
        {
            var obj = new GameObject(layerName);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0f, y, 0f);
            filter = obj.AddComponent<MeshFilter>();
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = vertexColorMaterial;
            mesh = new Mesh { name = layerName + "Mesh" };
            filter.sharedMesh = mesh;
        }

        private void EnsureMaterials()
        {
            if (vertexColorMaterial == null)
            {
                vertexColorMaterial = new Material(Shader.Find("Pocket City/Vertex Color Transparent"));
            }

            // REFERENCE_IMAGE_BRIGHT_CITY_PALETTE keeps runtime fallback materials aligned with generated assets.
            roadMaterial = roadMaterial != null ? roadMaterial : SolidMaterial("PocketCityRoad", new Color32(84, 98, 101, 255));
            roadLineMaterial = roadLineMaterial != null ? roadLineMaterial : SolidMaterial("PocketCityRoadLine", new Color32(255, 246, 190, 255));
            residentialMaterial = residentialMaterial != null ? residentialMaterial : SolidMaterial("PocketCityResidential", new Color32(255, 216, 126, 255));
            commercialMaterial = commercialMaterial != null ? commercialMaterial : SolidMaterial("PocketCityCommercial", new Color32(92, 192, 235, 255));
            mixedUseMaterial = mixedUseMaterial != null ? mixedUseMaterial : SolidMaterial("PocketCityMixedUse", new Color32(98, 216, 168, 255));
            officeMaterial = officeMaterial != null ? officeMaterial : SolidMaterial("PocketCityOffice", new Color32(135, 211, 240, 255));
            industrialMaterial = industrialMaterial != null ? industrialMaterial : SolidMaterial("PocketCityIndustrial", new Color32(235, 142, 92, 255));
            serviceMaterial = serviceMaterial != null ? serviceMaterial : SolidMaterial("PocketCityService", new Color32(252, 184, 116, 255));
            utilityMaterial = utilityMaterial != null ? utilityMaterial : SolidMaterial("PocketCityUtility", new Color32(101, 199, 213, 255));
            roofMaterial = roofMaterial != null ? roofMaterial : SolidMaterial("PocketCityRoof", new Color32(255, 244, 211, 255));
            windowMaterial = windowMaterial != null ? windowMaterial : SolidMaterial("PocketCityWindowGlow", new Color32(224, 252, 242, 255));
            buildingFootprintMaterial = buildingFootprintMaterial != null ? buildingFootprintMaterial : SolidMaterial("PocketCityBuildingFootprint", new Color32(91, 133, 108, 255));
            treeTrunkMaterial = treeTrunkMaterial != null ? treeTrunkMaterial : SolidMaterial("PocketCityTreeTrunk", new Color32(143, 104, 68, 255));
            treeCanopyMaterial = treeCanopyMaterial != null ? treeCanopyMaterial : SolidMaterial("PocketCityTreeCanopy", new Color32(91, 205, 86, 255));
            rockMaterial = rockMaterial != null ? rockMaterial : SolidMaterial("PocketCityRock", new Color32(178, 191, 177, 255));
            shoreMaterial = shoreMaterial != null ? shoreMaterial : SolidMaterial("PocketCityShore", new Color32(247, 232, 157, 255));
            grassGridMaterial = grassGridMaterial != null ? grassGridMaterial : SolidMaterial("PocketCityGrassGrid", new Color32(219, 249, 142, 255));
            lockedAreaMaterial = lockedAreaMaterial != null ? lockedAreaMaterial : SolidMaterial("PocketCityLockedArea", new Color32(239, 251, 145, 255));
            trafficPulseMaterial = trafficPulseMaterial != null ? trafficPulseMaterial : SolidMaterial("PocketCityTrafficPulse", new Color32(244, 116, 71, 255));
            serviceNeedMaterial = serviceNeedMaterial != null ? serviceNeedMaterial : SolidMaterial("PocketCityServiceNeed", new Color32(255, 196, 95, 255));
            previewOkMaterial = previewOkMaterial != null ? previewOkMaterial : SolidMaterial("PocketCityPreviewOk", new Color32(95, 202, 139, 210));
            previewBlockedMaterial = previewBlockedMaterial != null ? previewBlockedMaterial : SolidMaterial("PocketCityPreviewBlocked", new Color32(238, 99, 82, 220));
        }

        private Material SolidMaterial(string materialName, Color32 color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = materialName };
            material.color = color;
            return material;
        }

        private Material MaterialForZone(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return residentialMaterial;
            if (zone == ZoneType.Commercial) return commercialMaterial;
            if (zone == ZoneType.MixedUse) return mixedUseMaterial;
            if (zone == ZoneType.Office) return officeMaterial;
            if (zone == ZoneType.Industrial) return industrialMaterial;
            if (zone == ZoneType.Civic) return serviceMaterial;
            if (zone == ZoneType.Utility) return utilityMaterial;
            return serviceMaterial;
        }

        private Material MaterialForDefinition(BuildingDefinition definition, ZoneType zone)
        {
            if (definition == null)
            {
                return MaterialForZone(zone);
            }

            if (definition.Category == BuildingCategory.Residential) return residentialMaterial;
            if (definition.Category == BuildingCategory.Commercial)
            {
                if (definition.PreferredZone == ZoneType.Office) return officeMaterial;
                if (definition.PreferredZone == ZoneType.MixedUse) return mixedUseMaterial;
                return commercialMaterial;
            }

            if (definition.Category == BuildingCategory.Industrial) return industrialMaterial;
            if (definition.Category == BuildingCategory.Utility) return utilityMaterial;
            return serviceMaterial;
        }

        private static string ModelKeyVisualCatalog(BuildingDefinition definition)
        {
            return definition != null && !string.IsNullOrEmpty(definition.ModelKey)
                ? definition.ModelKey
                : "fallback";
        }

        private static int BuildingVisualSignature(IReadOnlyList<PlacedBuilding> buildings)
        {
            if (buildings == null)
            {
                return 0;
            }

            unchecked
            {
                var hash = 17;
                for (var i = 0; i < buildings.Count; i += 1)
                {
                    // BUILDING_VISUAL_PREFAB_LIBRARY includes identity and footprint so model-key styles rebuild.
                    hash = hash * 31 + StringHash(buildings[i].ConfigId);
                    hash = hash * 31 + buildings[i].Pos.X;
                    hash = hash * 31 + buildings[i].Pos.Y;
                    hash = hash * 31 + buildings[i].Size.W;
                    hash = hash * 31 + buildings[i].Size.H;
                    hash = hash * 31 + BuildingLevel(buildings[i]);
                    hash = hash * 31 + Mathf.Min(buildings[i].AgeDays, 9);
                }

                return hash;
            }
        }

        private static int RoadVisualSignature(IReadOnlyList<RoadNode> roads)
        {
            if (roads == null)
            {
                return 0;
            }

            unchecked
            {
                var hash = 23;
                for (var i = 0; i < roads.Count; i += 1)
                {
                    hash = hash * 31 + roads[i].Pos.X;
                    hash = hash * 31 + roads[i].Pos.Y;
                    hash = hash * 31 + (int)roads[i].Tier;
                    hash = hash * 31 + roads[i].Load;
                    hash = hash * 31 + roads[i].Capacity;
                }

                return hash;
            }
        }

        private static int PlanningMetricSignature(CityMetrics metrics)
        {
            if (metrics == null)
            {
                return 0;
            }

            unchecked
            {
                // CITY_SKYLINES_IMMEDIATE_LAYER_REFRESH keeps policy and budget changes visible without rebuilding geometry.
                var hash = 31;
                hash = hash * 37 + metrics.Congestion;
                hash = hash * 37 + metrics.RoadBottleneckPressure;
                hash = hash * 37 + metrics.ServiceGapPressure;
                hash = hash * 37 + metrics.ServiceBudgetPercent;
                hash = hash * 37 + metrics.ServiceBudgetExpense;
                hash = hash * 37 + metrics.BudgetStress;
                hash = hash * 37 + metrics.TransitCoverage;
                hash = hash * 37 + metrics.TransitWaitPressure;
                hash = hash * 37 + metrics.DemandUrgency;
                hash = hash * 37 + metrics.CashRunwayDays;
                hash = hash * 37 + metrics.ForecastRisk;
                hash = hash * 37 + metrics.UtilityReliability;
                hash = hash * 37 + metrics.FloodRisk;
                hash = hash * 37 + metrics.StormwaterResilience;
                hash = hash * 37 + metrics.StormwaterUtilization;
                hash = hash * 37 + metrics.Happiness;
                hash = hash * 37 + metrics.NetIncome;
                hash = hash * 37 + metrics.DevelopmentQuality;
                hash = hash * 37 + metrics.AverageLandValue;
                hash = hash * 37 + metrics.ParkingPressure;
                hash = hash * 37 + metrics.ParkingCoverage;
                hash = hash * 37 + metrics.TransitReliability;
                hash = hash * 37 + metrics.GrowthBottleneckScore;
                hash = hash * 37 + metrics.BuildingUpgradeReadinessScore;
                hash = hash * 37 + metrics.BuildingUpgradeReadyCount;
                hash = hash * 37 + metrics.BuildingUpgradeBlockedCount;
                if (metrics.Demand != null)
                {
                    hash = hash * 37 + metrics.Demand.Residential;
                    hash = hash * 37 + metrics.Demand.Commercial;
                    hash = hash * 37 + metrics.Demand.Industrial;
                    hash = hash * 37 + metrics.Demand.Office;
                    hash = hash * 37 + metrics.Demand.MixedUse;
                    hash = hash * 37 + metrics.Demand.Service;
                    hash = hash * 37 + metrics.Demand.Utility;
                }

                hash = hash * 37 + metrics.HousingCapacity;
                hash = hash * 37 + metrics.GoodsBalance;
                hash = hash * 37 + metrics.WorkforceSkill;
                if (metrics.ActiveObjective != null)
                {
                    hash = hash * 37 + metrics.ActiveObjective.Progress;
                    hash = hash * 37 + metrics.ActiveObjective.Required;
                    hash = hash * 37 + (metrics.ActiveObjective.Done ? 1 : 0);
                }

                if (metrics.Milestones != null)
                {
                    for (var i = 0; i < metrics.Milestones.Count; i += 1)
                    {
                        var milestone = metrics.Milestones[i];
                        if (milestone == null || milestone.Done)
                        {
                            continue;
                        }

                        hash = hash * 37 + StringHash(milestone.Id);
                        hash = hash * 37 + milestone.Progress;
                        hash = hash * 37 + milestone.Required;
                        break;
                    }
                }

                return hash;
            }
        }

        private static int DecorationHash(int x, int y)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + x * 73856093;
                hash = hash * 31 + y * 19349663;
                return hash == int.MinValue ? int.MaxValue : Mathf.Abs(hash);
            }
        }

        private static int TileDiagnosticSignature(TileData tile)
        {
            if (tile == null)
            {
                return 0;
            }

            unchecked
            {
                var hash = 41;
                hash = hash * 31 + (int)tile.Terrain;
                hash = hash * 31 + (int)tile.Zone;
                hash = hash * 31 + (string.IsNullOrEmpty(tile.RoadId) ? 0 : 1);
                hash = hash * 31 + (string.IsNullOrEmpty(tile.BuildingId) ? 0 : 1);
                hash = hash * 31 + tile.Traffic / 5;
                hash = hash * 31 + ServiceAccessValue(tile) / 5;
                hash = hash * 31 + tile.LandValue / 5;
                hash = hash * 31 + tile.TransitAccess / 8;
                hash = hash * 31 + tile.LogisticsAccess / 8;
                hash = hash * 31 + tile.ParkingAccess / 8;
                hash = hash * 31 + PollutionStress(tile) / 8;
                return hash;
            }
        }

        private static int PlacementPreviewSignature(int kind, GridPos from, GridPos to, GridSize size, bool ok, ZoneType zone)
        {
            unchecked
            {
                var hash = 29;
                hash = hash * 31 + kind;
                hash = hash * 31 + from.X;
                hash = hash * 31 + from.Y;
                hash = hash * 31 + to.X;
                hash = hash * 31 + to.Y;
                hash = hash * 31 + size.W;
                hash = hash * 31 + size.H;
                hash = hash * 31 + (ok ? 1 : 0);
                hash = hash * 31 + (int)zone;
                return hash;
            }
        }

        private static Color32 ShiftColor(Color32 color, int amount)
        {
            return new Color32(
                (byte)Mathf.Clamp(color.r + amount, 0, 255),
                (byte)Mathf.Clamp(color.g + amount, 0, 255),
                (byte)Mathf.Clamp(color.b + amount, 0, 255),
                color.a);
        }

        private static Color32 BlendColor(Color32 a, Color32 b, float t)
        {
            if (t <= 0f)
            {
                return a;
            }

            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)), 0, 255),
                a.a);
        }

        private static Color32 ZoneTerrainTint(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return new Color32(255, 210, 102, 255);
            if (zone == ZoneType.Commercial) return new Color32(88, 196, 230, 255);
            if (zone == ZoneType.MixedUse) return new Color32(93, 214, 166, 255);
            if (zone == ZoneType.Office) return new Color32(136, 210, 238, 255);
            if (zone == ZoneType.Industrial) return new Color32(232, 137, 89, 255);
            if (zone == ZoneType.Civic) return new Color32(247, 185, 103, 255);
            if (zone == ZoneType.Utility) return new Color32(103, 194, 204, 255);
            return new Color32(134, 207, 142, 255);
        }

        private static float ZoneTerrainTintStrength(ZoneType zone)
        {
            return zone == ZoneType.None ? 0f : 0.22f;
        }

        private static Color32 FacetedTileColor(Color32 color, int x, int y, int corner)
        {
            return ShiftColor(color, LowPolyCornerShade(x, y, corner));
        }

        private static int LowPolyCornerShade(int x, int y, int corner)
        {
            // LOW_POLY_ISOMETRIC_LIGHT_DIRECTION keeps the city board bright with a stable north-east light.
            var light = 0;
            if (corner == 1) light = 10;
            else if (corner == 3) light = 4;
            else if (corner == 0) light = -3;
            else light = -8;
            var jitter = ((x * 17 + y * 29 + corner * 7) % 5) - 2;
            return light + jitter;
        }

        private static int BuildingLevel(PlacedBuilding building)
        {
            return building == null ? 1 : Mathf.Max(1, Mathf.Min(3, building.Level));
        }

        private static int StringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i += 1)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static void ClearObjects(List<GameObject> objects)
        {
            for (var i = 0; i < objects.Count; i += 1)
            {
                if (objects[i] != null)
                {
                    Destroy(objects[i]);
                }
            }

            objects.Clear();
        }
    }
}
