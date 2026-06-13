using PocketCity.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketCity.Runtime
{
    public enum CityToolMode
    {
        Inspect,
        BuildRoad,
        UpgradeRoad,
        ZonePaint,
        BuildBuilding,
        Demolish
    }

    public sealed class CityInteractionController : MonoBehaviour
    {
        [SerializeField] private CityGameController controller;
        [SerializeField] private CityMapRenderer mapRenderer;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CityToolMode toolMode = CityToolMode.BuildRoad;
        [SerializeField] private string selectedBuildingId = "residential_pod";
        [SerializeField] private ZoneType selectedZone = ZoneType.Residential;

        private GridPos dragStart;
        private GridPos selectedTile;
        private bool hasDragStart;
        private bool hasSelectedTile;
        private int lastHoverPreviewSignature = int.MinValue;
        private int lastHoverHudFeedbackSignature = int.MinValue;
        private GridPos pendingBuildingPos;
        private string pendingBuildingId = string.Empty;
        private bool hasPendingBuildingConfirm;

        public CityToolMode ToolMode
        {
            get { return toolMode; }
        }

        public string SelectedBuildingId
        {
            get { return selectedBuildingId; }
        }

        public ZoneType SelectedZone
        {
            get { return selectedZone; }
        }

        public bool HasSelectedTile
        {
            get { return hasSelectedTile; }
        }

        public GridPos SelectedTile
        {
            get { return selectedTile; }
        }

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (controller == null || mapRenderer == null)
            {
                return;
            }

            HandleKeyboardShortcuts();
            HandlePointerInput();
        }

        public void SelectInspectTool()
        {
            toolMode = CityToolMode.Inspect;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                controller.SetOverlay(OverlayMode.Normal);
                PublishToolFeedback("\u67e5\u770b\u5de5\u5177", OverlayMode.Normal, "\u672a\u660e\u70ed\u70b9\uff1b\u70b9\u683c\u770b");
            }
        }

        public void SelectRoadTool()
        {
            toolMode = CityToolMode.BuildRoad;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                controller.SetOverlay(OverlayMode.Traffic);
                PublishToolFeedback("\u9053\u8def\u5de5\u5177", OverlayMode.Traffic, "\u65ad\u70b9/\u62e5\u5835\uff1b\u62d6\u7ebf\u8865\u8def");
            }

            RefreshSelectedTilePreview();
        }

        public void SelectRoadUpgradeTool()
        {
            toolMode = CityToolMode.UpgradeRoad;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                controller.SetOverlay(OverlayMode.Traffic);
                PublishToolFeedback("\u5347\u7ea7\u9053\u8def", OverlayMode.Traffic, "\u8f66\u6d41\u6ee1\u8f7d\uff1b\u70b9\u8def\u6bb5\u5347\u7ea7");
            }

            RefreshSelectedTilePreview();
        }

        public void SelectZoneTool(ZoneType zone)
        {
            toolMode = CityToolMode.ZonePaint;
            selectedZone = zone;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                controller.SetOverlay(OverlayMode.Zoning);
                PublishToolFeedback(ZoneToolLabel(zone), OverlayMode.Zoning, "\u9700\u6c42/\u51b2\u7a81\uff1b\u62d6\u5237\u7eff\u683c");
            }

            RefreshSelectedTilePreview();
        }

        public void SelectBuildingTool(string buildingId)
        {
            toolMode = CityToolMode.BuildBuilding;
            selectedBuildingId = buildingId;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                var overlay = OverlayForBuilding(buildingId);
                controller.SetOverlay(overlay);
                PublishToolFeedback(BuildingToolLabel(buildingId), overlay, BuildingToolHint(buildingId));
            }

            RefreshSelectedTilePreview();
        }

        public void SelectDemolishTool()
        {
            toolMode = CityToolMode.Demolish;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                controller.SetOverlay(OverlayMode.Normal);
                PublishToolFeedback("\u62c6\u9664\u5de5\u5177", OverlayMode.Normal, "\u95f2\u7f6e/\u9519\u653e\uff1b\u70b9\u5bf9\u8c61\u62c6");
            }

            RefreshSelectedTilePreview();
        }

        public void CancelActivePlanning()
        {
            var hadDrag = hasDragStart;
            var hadPendingBuilding = hasPendingBuildingConfirm;
            toolMode = CityToolMode.Inspect;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if (controller != null)
            {
                controller.SetOverlay(OverlayMode.Normal);
                controller.PublishHudFeedback(
                    hadDrag || hadPendingBuilding
                        ? "\u505a \u53d6\u6d88\u89c4\u5212  \u505a:\u770b\u5c42\u91cd\u843d\u70b9  \u5c42:\u666e\u901a"
                        : "\u505a \u56de\u5230\u67e5\u770b  \u505a:\u70b9\u70ed\u533a\u91cd\u89c4\u5212  \u5c42:\u666e\u901a",
                    true);
            }
        }

        private void PublishToolFeedback(string label, OverlayMode mode, string hint)
        {
            if (controller == null)
            {
                return;
            }

            controller.PublishHudFeedback("\u505a " + label + FormatToolHint(hint, OverlayToolLabel(mode)) + ToolFeedbackStatusSuffix(), true);
        }

        private string ToolFeedbackStatusSuffix()
        {
            var metrics = controller != null ? controller.Metrics : null;
            if (metrics == null)
            {
                return string.Empty;
            }

            var status = string.Empty;
            var objective = metrics.ActiveObjective;
            if (objective != null && !objective.Done && objective.Required > 0)
            {
                status = "  \u4efb " + objective.Progress + "/" + objective.Required + " " + ShortToolFeedbackText(objective.Title, 7);
            }

            if (!string.IsNullOrEmpty(metrics.ServiceGapFocus) && metrics.ServiceGapFocus != "\u5747\u8861")
            {
                return status + "  \u7f3a " + ShortToolFeedbackText(metrics.ServiceGapFocus, 5);
            }

            if (!string.IsNullOrEmpty(metrics.DemandFocus) && metrics.DemandUrgency >= 55)
            {
                return status + "  \u9700 " + ShortToolFeedbackText(metrics.DemandFocus, 5) + metrics.DemandUrgency;
            }

            return status;
        }

        private static string FormatToolHint(string hint, string layer)
        {
            var issue = hint;
            var recommendation = string.Empty;
            var separator = !string.IsNullOrEmpty(hint) ? hint.IndexOf('\uff1b') : -1;
            if (separator >= 0)
            {
                issue = hint.Substring(0, separator);
                recommendation = hint.Substring(separator + 1);
            }

            var text = "  \u72b6:" + DiagnosticPart(issue, "\u5f85\u786e\u8ba4")
                + "  \u505a:" + DiagnosticPart(ShortFixText(recommendation, layer), "\u79fb\u52a8\u5149\u6807");
            if (!string.IsNullOrEmpty(layer))
            {
                text += "  \u5c42:" + layer;
            }

            return text;
        }

        private static string ShortToolFeedbackText(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Mathf.Max(1, maxLength));
        }

        private string BuildingToolLabel(string buildingId)
        {
            var definition = controller != null ? controller.GetBuildingDefinition(buildingId) : null;
            if (definition != null && !string.IsNullOrEmpty(definition.Name))
            {
                return definition.Name;
            }

            return string.IsNullOrEmpty(buildingId) ? "\u5efa\u7b51\u5de5\u5177" : buildingId;
        }

        private static string BuildingToolHint(string buildingId)
        {
            if (buildingId == "bus_hub" || buildingId == "metro_station" || buildingId == "intercity_terminal") return "\u516c\u4ea4\u7f3a\u7ad9\uff1b\u8d34\u4e3b\u8def";
            if (buildingId == "cargo_depot" || buildingId == "distribution_center" || buildingId == "freight_rail_terminal") return "\u8d27\u8fd0\u94fe\u5f31\uff1b\u8d34\u8f74\u7ebf";
            if (buildingId == "parking_garage") return "\u505c\u8f66\u627f\u538b\uff1b\u9760\u9700\u6c42\u70b9";
            if (buildingId == "rain_garden") return "\u96e8\u6d2a\u8584\u5f31\uff1b\u653e\u4f4e\u9669\u683c";
            if (buildingId == "road_maintenance_depot") return "\u517b\u62a4\u4e0d\u8db3\uff1b\u9760\u5e72\u9053";
            if (buildingId == "pocket_park" || buildingId == "city_plaza") return "\u5730\u4ef7\u504f\u4f4e\uff1b\u9760\u4f4f\u5b85";
            if (buildingId == "micro_power" || buildingId == "water_tower" || buildingId == "water_reclaimer") return "\u6c34\u7535\u4e0d\u8db3\uff1b\u9760\u8def\u7a7a\u5730";
            return "\u7f3a\u63a5\u8def\u7a7a\u5730\uff1b\u70b9\u7eff\u683c";
        }

        private static string ZoneToolLabel(ZoneType zone)
        {
            if (zone == ZoneType.Residential) return "\u4f4f\u5b85\u5206\u533a";
            if (zone == ZoneType.Commercial) return "\u5546\u4e1a\u5206\u533a";
            if (zone == ZoneType.Industrial) return "\u5de5\u4e1a\u5206\u533a";
            if (zone == ZoneType.Office) return "\u529e\u516c\u5206\u533a";
            if (zone == ZoneType.MixedUse) return "\u6df7\u5408\u5206\u533a";
            if (zone == ZoneType.Civic) return "\u670d\u52a1\u5206\u533a";
            if (zone == ZoneType.Utility) return "\u8bbe\u65bd\u5206\u533a";
            return "\u5206\u533a\u5de5\u5177";
        }

        private static string OverlayToolLabel(OverlayMode mode)
        {
            if (mode == OverlayMode.Traffic) return "\u4ea4\u901a";
            if (mode == OverlayMode.Zoning) return "\u5206\u533a";
            if (mode == OverlayMode.Services) return "\u670d\u52a1";
            if (mode == OverlayMode.Transit) return "\u516c\u4ea4";
            if (mode == OverlayMode.Logistics) return "\u8d27\u8fd0";
            if (mode == OverlayMode.Utilities) return "\u6c34\u7535";
            if (mode == OverlayMode.Communications) return "\u901a\u4fe1";
            if (mode == OverlayMode.RoadSafety) return "\u8def\u5b89";
            if (mode == OverlayMode.Parking) return "\u505c\u8f66";
            if (mode == OverlayMode.Stormwater) return "\u96e8\u6d2a";
            if (mode == OverlayMode.Waste) return "\u56de\u6536";
            if (mode == OverlayMode.Pollution) return "\u6c61\u67d3";
            if (mode == OverlayMode.LandValue) return "\u5730\u4ef7";
            return "\u666e\u901a";
        }

        private void HandleKeyboardShortcuts()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) SelectRoadTool();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) SelectZoneTool(ZoneType.Residential);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) SelectZoneTool(ZoneType.Commercial);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha4)) SelectZoneTool(ZoneType.Industrial);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha5)) SelectBuildingTool("residential_pod");
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha6)) SelectBuildingTool("market_corner");
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha7)) SelectBuildingTool("pocket_park");
            if (UnityEngine.Input.GetKeyDown(KeyCode.I)) SelectInspectTool();
            if (UnityEngine.Input.GetKeyDown(KeyCode.U)) SelectRoadUpgradeTool();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace) || UnityEngine.Input.GetKeyDown(KeyCode.Delete)) SelectDemolishTool();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) CancelDragPreview();
        }

        private static OverlayMode OverlayForBuilding(string buildingId)
        {
            if (buildingId == "bus_hub" || buildingId == "metro_station" || buildingId == "intercity_terminal")
            {
                return OverlayMode.Transit;
            }

            if (buildingId == "cargo_depot" || buildingId == "resource_processor" || buildingId == "distribution_center" || buildingId == "freight_rail_terminal")
            {
                return OverlayMode.Logistics;
            }

            if (buildingId == "pocket_park" || buildingId == "city_plaza" || buildingId == "convention_center" || buildingId == "city_hall" || buildingId == "health_post" || buildingId == "district_hospital" || buildingId == "memorial_garden" || buildingId == "emergency_shelter" || buildingId == "primary_school" || buildingId == "community_college" || buildingId == "fire_station" || buildingId == "police_kiosk" || buildingId == "police_precinct")
            {
                return OverlayMode.Services;
            }

            if (buildingId == "telecom_hub" || buildingId == "post_office" || buildingId == "research_campus")
            {
                return OverlayMode.Communications;
            }

            if (buildingId == "road_maintenance_depot")
            {
                return OverlayMode.RoadSafety;
            }

            if (buildingId == "parking_garage")
            {
                return OverlayMode.Parking;
            }

            if (buildingId == "rain_garden")
            {
                return OverlayMode.Stormwater;
            }

            if (buildingId == "micro_power" || buildingId == "solar_farm" || buildingId == "water_tower" || buildingId == "water_reclaimer")
            {
                return OverlayMode.Utilities;
            }

            if (buildingId == "recycling_yard" || buildingId == "waste_to_energy_plant")
            {
                return OverlayMode.Waste;
            }

            return OverlayMode.Normal;
        }

        private void HandlePointerInput()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        CancelDragPreview();
                    }

                    return;
                }

                if (touch.phase == TouchPhase.Began)
                {
                    PointerDown(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    GridPos hoverPos;
                    if (TryScreenToGrid(touch.position, out hoverPos))
                    {
                        SelectTileForInspector(hoverPos);
                        UpdateHoverPreview(hoverPos);
                    }
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    PointerUp(touch.position);
                }
                else if (touch.phase == TouchPhase.Canceled)
                {
                    CancelDragPreview();
                }

                return;
            }

            UpdateMouseHoverTile();

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                PointerDown(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    CancelDragPreview();
                    return;
                }

                PointerUp(UnityEngine.Input.mousePosition);
            }
        }

        private void UpdateMouseHoverTile()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            GridPos hoverPos;
            if (TryScreenToGrid(UnityEngine.Input.mousePosition, out hoverPos))
            {
                SelectTileForInspector(hoverPos);
                UpdateHoverPreview(hoverPos);
            }
            else if (mapRenderer != null)
            {
                mapRenderer.ClearPlacementPreview();
                lastHoverPreviewSignature = int.MinValue;
            }
        }

        private void UpdateHoverPreview(GridPos hoverPos)
        {
            if (controller == null || mapRenderer == null)
            {
                return;
            }

            // UNITY_HOVER_DRAG_PREVIEW_GHOST keeps the HUD preview and map footprint in sync.
            var signature = HoverPreviewSignature(hoverPos);
            if (lastHoverPreviewSignature == signature)
            {
                return;
            }

            lastHoverPreviewSignature = signature;

            if (hasDragStart && toolMode == CityToolMode.BuildRoad)
            {
                var preview = controller.PreviewRoad(dragStart.X, dragStart.Y, hoverPos.X, hoverPos.Y);
                mapRenderer.ShowRoadPlacementPreview(dragStart, hoverPos, preview != null && preview.Ok);
                PublishHoverPreviewFeedback(preview, "\u9053\u8def\u65b9\u6848", signature);
                return;
            }

            if (hasDragStart && toolMode == CityToolMode.ZonePaint)
            {
                var preview = controller.PreviewZone(dragStart.X, dragStart.Y, hoverPos.X, hoverPos.Y, selectedZone);
                mapRenderer.ShowZonePlacementPreview(dragStart, hoverPos, selectedZone, preview != null && preview.Ok);
                PublishHoverPreviewFeedback(preview, "\u5206\u533a\u89c4\u5212", signature);
                return;
            }

            if (toolMode == CityToolMode.BuildRoad)
            {
                var preview = controller.PreviewRoad(hoverPos.X, hoverPos.Y, hoverPos.X, hoverPos.Y);
                mapRenderer.ShowSingleTilePlacementPreview(hoverPos, preview != null && preview.Ok);
                PublishHoverPreviewFeedback(preview, "\u9053\u8def\u65b9\u6848", signature);
                return;
            }

            if (toolMode == CityToolMode.ZonePaint)
            {
                var preview = controller.PreviewZone(hoverPos.X, hoverPos.Y, hoverPos.X, hoverPos.Y, selectedZone);
                mapRenderer.ShowZonePlacementPreview(hoverPos, hoverPos, selectedZone, preview != null && preview.Ok);
                PublishHoverPreviewFeedback(preview, "\u5206\u533a\u89c4\u5212", signature);
                return;
            }

            if (toolMode == CityToolMode.BuildBuilding)
            {
                var preview = controller.PreviewBuilding(selectedBuildingId, hoverPos.X, hoverPos.Y);
                var definition = controller.GetBuildingDefinition(selectedBuildingId);
                mapRenderer.ShowBuildingPlacementPreview(hoverPos, definition != null ? definition.Size : new GridSize(1, 1), preview != null && preview.Ok, preview != null ? preview.SiteScore : 0);
                if (preview != null && preview.Ok && IsPendingBuildingConfirm(hoverPos, selectedBuildingId))
                {
                    PublishPendingBuildingFeedback(hoverPos, selectedBuildingId, preview.SiteScore, signature);
                }
                else
                {
                    PublishHoverPreviewFeedback(preview, BuildingToolLabel(selectedBuildingId), signature);
                }

                return;
            }

            if (toolMode == CityToolMode.UpgradeRoad)
            {
                var preview = controller.PreviewRoadUpgrade(hoverPos.X, hoverPos.Y);
                mapRenderer.ShowSingleTilePlacementPreview(hoverPos, preview != null && preview.Ok);
                PublishHoverPreviewFeedback(preview, "\u9053\u8def\u5347\u7ea7", signature);
                return;
            }

            if (toolMode == CityToolMode.Demolish)
            {
                var preview = controller.PreviewDemolish(hoverPos.X, hoverPos.Y);
                mapRenderer.ShowSingleTilePlacementPreview(hoverPos, preview != null && preview.Ok);
                PublishHoverPreviewFeedback(preview, "\u62c6\u9664", signature);
                return;
            }

            if (toolMode == CityToolMode.Inspect)
            {
                mapRenderer.ShowInspectTileFocus(hoverPos);
                PublishInspectTileFeedback(hoverPos);
                return;
            }

            mapRenderer.ClearPlacementPreview();
        }

        private void PointerDown(Vector2 screenPosition)
        {
            GridPos gridPos;
            if (!TryScreenToGrid(screenPosition, out gridPos))
            {
                return;
            }

            SelectTileForInspector(gridPos);
            ShowSelectedTileFocus(gridPos);
            if (HandleLockedRegionTap(gridPos))
            {
                return;
            }

            if (toolMode == CityToolMode.Inspect)
            {
                PublishInspectTileFeedback(gridPos);
                return;
            }

            if (toolMode == CityToolMode.BuildBuilding)
            {
                var preview = controller.PreviewBuilding(selectedBuildingId, gridPos.X, gridPos.Y);
                var definition = controller.GetBuildingDefinition(selectedBuildingId);
                mapRenderer.ShowBuildingPlacementPreview(gridPos, definition != null ? definition.Size : new GridSize(1, 1), preview != null && preview.Ok, preview != null ? preview.SiteScore : 0);
                if (preview == null || !preview.Ok)
                {
                    ClearPendingBuildingConfirm();
                    mapRenderer.ShowCommandResultMarker(gridPos, false, toolMode);
                    controller.PublishHudFeedback(BuildBlockedPreviewFeedback(preview), false);
                    return;
                }

                if (!IsPendingBuildingConfirm(gridPos, selectedBuildingId))
                {
                    pendingBuildingPos = gridPos;
                    pendingBuildingId = selectedBuildingId;
                    hasPendingBuildingConfirm = true;
                    var pendingSignature = HoverPreviewSignature(gridPos);
                    lastHoverPreviewSignature = pendingSignature;
                    lastHoverHudFeedbackSignature = pendingSignature;
                    controller.PublishHudFeedback(BuildPendingBuildingFeedback(gridPos, selectedBuildingId, preview.SiteScore), false);
                    return;
                }

                var confirmed = controller.ConfirmBuilding(selectedBuildingId, gridPos.X, gridPos.Y);
                ClearPendingBuildingConfirm();
                mapRenderer.ShowCommandResultMarker(gridPos, confirmed, toolMode);
                PublishSingleTileSubmitFeedback(confirmed, BuildingToolLabel(selectedBuildingId), gridPos, preview);
                if (confirmed)
                {
                    mapRenderer.ClearPlacementPreview();
                    mapRenderer.RebuildAll();
                }

                return;
            }

            if (toolMode == CityToolMode.Demolish)
            {
                var preview = controller.PreviewDemolish(gridPos.X, gridPos.Y);
                mapRenderer.ShowSingleTilePlacementPreview(gridPos, preview != null && preview.Ok);
                var confirmed = controller.ConfirmDemolish(gridPos.X, gridPos.Y);
                mapRenderer.ShowCommandResultMarker(gridPos, confirmed, toolMode);
                PublishSingleTileSubmitFeedback(confirmed, "\u62c6\u9664", gridPos, preview);
                if (confirmed)
                {
                    mapRenderer.ClearPlacementPreview();
                    mapRenderer.RebuildAll();
                }

                return;
            }

            if (toolMode == CityToolMode.UpgradeRoad)
            {
                var preview = controller.PreviewRoadUpgrade(gridPos.X, gridPos.Y);
                mapRenderer.ShowSingleTilePlacementPreview(gridPos, preview != null && preview.Ok);
                var confirmed = controller.ConfirmRoadUpgrade(gridPos.X, gridPos.Y);
                mapRenderer.ShowCommandResultMarker(gridPos, confirmed, toolMode);
                PublishSingleTileSubmitFeedback(confirmed, "\u9053\u8def\u5347\u7ea7", gridPos, preview);
                if (confirmed)
                {
                    mapRenderer.ClearPlacementPreview();
                    mapRenderer.RebuildAll();
                }

                return;
            }

            dragStart = gridPos;
            hasDragStart = true;
            lastHoverPreviewSignature = int.MinValue;
            UpdateHoverPreview(gridPos);
            PublishDragStartFeedback(gridPos);
        }

        private void PointerUp(Vector2 screenPosition)
        {
            if (!hasDragStart)
            {
                return;
            }

            GridPos gridPos;
            if (!TryScreenToGrid(screenPosition, out gridPos))
            {
                hasDragStart = false;
                if (mapRenderer != null)
                {
                    mapRenderer.ClearPlacementPreview();
                }

                return;
            }

            SelectTileForInspector(gridPos);
            ShowSelectedTileFocus(gridPos);
            if (IsLockedRegionTile(dragStart) || IsLockedRegionTile(gridPos))
            {
                var focus = IsLockedRegionTile(gridPos) ? gridPos : dragStart;
                hasDragStart = false;
                lastHoverPreviewSignature = int.MinValue;
                if (mapRenderer != null)
                {
                    mapRenderer.ClearPlacementPreview();
                    mapRenderer.ShowLockedRegionTapMarker(focus);
                }

                PublishLockedRegionFeedback(focus);
                return;
            }

            if (toolMode == CityToolMode.BuildRoad)
            {
                controller.PreviewRoad(dragStart.X, dragStart.Y, gridPos.X, gridPos.Y);
                var confirmed = controller.ConfirmRoad(dragStart.X, dragStart.Y, gridPos.X, gridPos.Y);
                mapRenderer.ShowCommandResultMarker(CommandResultMarkerPos(dragStart, gridPos), confirmed, toolMode);
                PublishDragSubmitFeedback(confirmed, "\u94fa\u8def", DragTileCount(dragStart, gridPos) + "\u683c");
                if (confirmed)
                {
                    mapRenderer.ClearPlacementPreview();
                    mapRenderer.RebuildAll();
                }
            }
            else if (toolMode == CityToolMode.ZonePaint)
            {
                controller.PreviewZone(dragStart.X, dragStart.Y, gridPos.X, gridPos.Y, selectedZone);
                var confirmed = controller.ConfirmZone(dragStart.X, dragStart.Y, gridPos.X, gridPos.Y, selectedZone);
                mapRenderer.ShowCommandResultMarker(CommandResultMarkerPos(dragStart, gridPos), confirmed, toolMode);
                PublishDragSubmitFeedback(confirmed, "\u5212\u533a", DragRectText(dragStart, gridPos));
                if (confirmed)
                {
                    mapRenderer.ClearPlacementPreview();
                    mapRenderer.RebuildAll();
                }
            }

            hasDragStart = false;
            lastHoverPreviewSignature = int.MinValue;
        }

        private void RefreshSelectedTilePreview()
        {
            // REFERENCE_IMAGE_INSTANT_TOOL_PREVIEW keeps build tools visually anchored after toolbar taps.
            if (hasSelectedTile)
            {
                UpdateHoverPreview(selectedTile);
            }
        }

        private static GridPos CommandResultMarkerPos(GridPos from, GridPos to)
        {
            return new GridPos((from.X + to.X) / 2, (from.Y + to.Y) / 2);
        }

        private void PublishDragSubmitFeedback(bool confirmed, string label, string sizeText)
        {
            if (controller == null)
            {
                return;
            }

            var detail = confirmed ? CompactDragReceipt(controller.LastCommandFeedbackText) : PreviewFeedbackDetail(controller.CurrentPreview);
            if (string.IsNullOrEmpty(detail))
            {
                detail = CompactDragReceipt(controller.LastCommandFeedbackText);
            }

            var issue = confirmed ? "\u5b8c\u6210" : BlockedIssueFromText(detail, toolMode);
            var reason = confirmed
                ? (!string.IsNullOrEmpty(detail) ? detail : sizeText)
                : BlockedReasonFromText(detail, toolMode);
            var recommendation = confirmed ? SuccessNextAction(toolMode) : BlockedNextActionFromText(detail, toolMode);
            var text = label + " " + sizeText + ToolDiagnosticClause(issue, reason, recommendation);
            controller.PublishHudFeedback(text, confirmed);
        }

        private void PublishSingleTileSubmitFeedback(bool confirmed, string label, GridPos pos, ConstructionPreview preview)
        {
            if (controller == null)
            {
                return;
            }

            var detail = confirmed ? CompactDragReceipt(controller.LastCommandFeedbackText) : PreviewFeedbackDetail(preview);
            if (string.IsNullOrEmpty(detail))
            {
                detail = CompactDragReceipt(controller.LastCommandFeedbackText);
            }

            var issue = confirmed ? "\u5b8c\u6210" : BlockedIssueFromText(detail, toolMode);
            var reason = confirmed
                ? (!string.IsNullOrEmpty(detail) ? detail : pos.X + "," + pos.Y)
                : BlockedReasonFromText(detail, toolMode);
            var recommendation = confirmed ? SuccessNextAction(toolMode) : BlockedNextActionFromText(detail, toolMode);
            controller.PublishHudFeedback(label + " " + pos.X + "," + pos.Y + ToolDiagnosticClause(issue, reason, recommendation), confirmed);
        }

        private void PublishDragStartFeedback(GridPos start)
        {
            if (controller == null)
            {
                return;
            }

            if (toolMode == CityToolMode.BuildRoad)
            {
                controller.PublishHudFeedback("\u62d6 \u9053\u8def" + ToolDiagnosticClause("\u5b9a\u7ebf", "\u8d77 " + start.X + "," + start.Y, "\u62d6\u7ec8\u70b9\u677e\u624b"), true);
            }
            else if (toolMode == CityToolMode.ZonePaint)
            {
                controller.PublishHudFeedback("\u62d6 " + ZoneToolLabel(selectedZone) + ToolDiagnosticClause("\u5b9a\u8303\u56f4", "\u8d77 " + start.X + "," + start.Y, "\u62d6\u8303\u56f4\u677e\u624b"), true);
            }
        }

        private static int DragTileCount(GridPos from, GridPos to)
        {
            return Mathf.Max(Mathf.Abs(to.X - from.X), Mathf.Abs(to.Y - from.Y)) + 1;
        }

        private static string DragRectText(GridPos from, GridPos to)
        {
            var width = Mathf.Abs(to.X - from.X) + 1;
            var height = Mathf.Abs(to.Y - from.Y) + 1;
            return width + "x" + height;
        }

        private static string CompactDragReceipt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= 48 ? value : value.Substring(0, 47) + "...";
        }

        private void PublishHoverPreviewFeedback(ConstructionPreview preview, string fallbackTitle, int signature)
        {
            if (controller == null || preview == null || lastHoverHudFeedbackSignature == signature)
            {
                return;
            }

            lastHoverHudFeedbackSignature = signature;
            var title = string.IsNullOrEmpty(preview.Title) ? fallbackTitle : preview.Title;
            var detail = PreviewFeedbackDetail(preview);
            var score = preview.SiteScore > 0 ? "  \u8bc4" + preview.SiteScore : string.Empty;
            var issue = PreviewStateLabel(preview, detail);
            var reason = PreviewReasonLabel(preview, detail);
            var text = "\u9884 " + title + score + ToolDiagnosticClause(issue, reason, PreviewNextAction(preview, detail));
            controller.PublishHudFeedback(CompactHoverFeedback(text), preview.Ok);
        }

        private static string PreviewFeedbackDetail(ConstructionPreview preview)
        {
            if (preview == null)
            {
                return string.Empty;
            }

            if (!preview.Ok)
            {
                var blockedReason = PreviewBlockedReason(preview);
                if (!string.IsNullOrEmpty(blockedReason))
                {
                    return blockedReason;
                }
            }

            if (!string.IsNullOrEmpty(preview.SiteDiagnosis))
            {
                return preview.SiteDiagnosis;
            }

            return FirstPreviewLine(preview);
        }

        private string PreviewStateLabel(ConstructionPreview preview, string detail)
        {
            if (preview == null)
            {
                return "\u65e0\u9884";
            }

            if (!preview.Ok)
            {
                return BlockedIssueFromText(detail, toolMode);
            }

            if (toolMode == CityToolMode.BuildBuilding)
            {
                return "\u5f85\u786e\u8ba4";
            }

            if (toolMode == CityToolMode.ZonePaint)
            {
                return "\u53ef\u89c4\u5212";
            }

            if (toolMode == CityToolMode.UpgradeRoad || toolMode == CityToolMode.Demolish)
            {
                return toolMode == CityToolMode.UpgradeRoad ? "\u53ef\u5347\u8def" : "\u53ef\u62c6";
            }

            return "\u53ef\u5efa\u8def";
        }

        private string PreviewReasonLabel(ConstructionPreview preview, string detail)
        {
            if (preview == null)
            {
                return "\u65e0\u9884";
            }

            if (!preview.Ok)
            {
                return BlockedReasonFromText(detail, toolMode);
            }

            if (!string.IsNullOrEmpty(detail))
            {
                return detail;
            }

            if (toolMode == CityToolMode.BuildRoad)
            {
                return hasDragStart ? "\u7ebf\u6709\u6548" : "\u8d77\u70b9\u53ef\u7528";
            }

            if (toolMode == CityToolMode.ZonePaint)
            {
                return "\u683c\u53ef\u5237\u533a";
            }

            if (toolMode == CityToolMode.BuildBuilding)
            {
                return "\u5360\u5730/\u63a5\u8def\u53ef\u7528";
            }

            if (toolMode == CityToolMode.UpgradeRoad)
            {
                return "\u8def\u6bb5\u53ef\u6269";
            }

            if (toolMode == CityToolMode.Demolish)
            {
                return "\u5bf9\u8c61\u53ef\u62c6";
            }

            return "\u53ef\u7528";
        }

        private string PreviewNextAction(ConstructionPreview preview, string detail)
        {
            if (preview == null)
            {
                return "\u79fb\u5149\u6807\u590d\u6838";
            }

            if (!preview.Ok)
            {
                return BlockedNextActionFromText(detail, toolMode);
            }

            if (toolMode == CityToolMode.BuildRoad)
            {
                return hasDragStart ? "\u4ea4\u901a\u5c42\u677e\u624b\u786e\u8ba4" : "\u4ea4\u901a\u5c42\u62d6\u7ebf";
            }

            if (toolMode == CityToolMode.ZonePaint)
            {
                return hasDragStart ? "\u5206\u533a\u5c42\u677e\u624b\u786e\u8ba4" : "\u5206\u533a\u5c42\u62d6\u8303\u56f4";
            }

            if (toolMode == CityToolMode.BuildBuilding)
            {
                return "\u5f53\u524d\u5c42\u518d\u70b9\u540c\u683c";
            }

            if (toolMode == CityToolMode.UpgradeRoad)
            {
                return "\u4ea4\u901a\u5c42\u70b9\u5347";
            }

            if (toolMode == CityToolMode.Demolish)
            {
                return "\u666e\u901a\u5c42\u70b9\u62c6";
            }

            return string.Empty;
        }

        private static string PreviewBlockedReason(ConstructionPreview preview)
        {
            var reason = PreviewLineMatching(preview, "\u672a\u89e3\u9501");
            if (!string.IsNullOrEmpty(reason)) return reason;

            reason = PreviewLineMatching(preview, "\u73b0\u91d1\u4e0d\u8db3");
            if (!string.IsNullOrEmpty(reason)) return reason;

            reason = PreviewLineMatching(preview, "\u914d\u7f6e\u7f3a\u5931", "\u672a\u77e5");
            if (!string.IsNullOrEmpty(reason)) return reason;

            reason = PreviewLineMatching(preview, "\u8d85\u51fa", "\u5730\u56fe\u8fb9\u754c");
            if (!string.IsNullOrEmpty(reason)) return reason;

            reason = PreviewLineMatching(preview, "\u6b64\u5904\u6ca1\u6709\u9053\u8def", "\u6ca1\u6709\u9053\u8def", "\u5df2\u7ecf\u662f\u4e3b\u5e72\u9053", "\u6ca1\u6709\u5efa\u7b51");
            if (!string.IsNullOrEmpty(reason)) return reason;

            reason = PreviewLineMatching(preview, "\u63a8\u8350\u5206\u533a", "\u4e0d\u80fd", "\u4e0d\u53ef", "\u88ab\u5360\u7528", "\u6c34\u9762");
            if (!string.IsNullOrEmpty(reason)) return reason;

            return FirstPreviewLine(preview);
        }

        private static string FirstPreviewLine(ConstructionPreview preview)
        {
            if (preview == null || preview.Lines == null)
            {
                return string.Empty;
            }

            for (var i = 0; i < preview.Lines.Count; i += 1)
            {
                if (!string.IsNullOrEmpty(preview.Lines[i]))
                {
                    return preview.Lines[i];
                }
            }

            return string.Empty;
        }

        private static string PreviewLineMatching(ConstructionPreview preview, params string[] tokens)
        {
            if (preview == null || preview.Lines == null || tokens == null)
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

                for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex += 1)
                {
                    if (!string.IsNullOrEmpty(tokens[tokenIndex]) && line.IndexOf(tokens[tokenIndex], System.StringComparison.Ordinal) >= 0)
                    {
                        return line;
                    }
                }
            }

            return string.Empty;
        }

        private static string BlockedIssueFromText(string detail, CityToolMode mode)
        {
            if (ContainsText(detail, "\u672a\u89e3\u9501") || ContainsText(detail, "\u9501\u5b9a") || ContainsText(detail, "\u672a\u5f00\u653e"))
            {
                return "\u9501\u533a";
            }

            if (ContainsText(detail, "\u73b0\u91d1\u4e0d\u8db3"))
            {
                return "\u9884\u7b97\u7f3a";
            }

            if (ContainsText(detail, "\u8d85\u51fa") || ContainsText(detail, "\u5730\u56fe\u8fb9\u754c"))
            {
                return "\u8d8a\u754c";
            }

            if (ContainsText(detail, "\u6b64\u5904\u6ca1\u6709\u9053\u8def") || ContainsText(detail, "\u6ca1\u6709\u9053\u8def"))
            {
                return mode == CityToolMode.BuildBuilding ? "\u5efa\u7b51\u7f3a\u8def" : "\u8def\u70b9\u7a7a";
            }

            if (ContainsText(detail, "\u5df2\u7ecf\u662f\u4e3b\u5e72\u9053"))
            {
                return "\u8def\u5df2\u6ee1\u7ea7";
            }

            if (ContainsText(detail, "\u6ca1\u6709\u5efa\u7b51"))
            {
                return "\u5efa\u7b51\u7a7a";
            }

            if (ContainsText(detail, "\u63a8\u8350\u5206\u533a"))
            {
                return "\u5206\u533a\u9519";
            }

            if (ContainsText(detail, "\u4e0d\u80fd") || ContainsText(detail, "\u4e0d\u53ef") || ContainsText(detail, "\u88ab\u5360\u7528") || ContainsText(detail, "\u6c34\u9762"))
            {
                if (mode == CityToolMode.BuildRoad) return "\u8def\u843d\u70b9\u963b";
                if (mode == CityToolMode.ZonePaint) return "\u5206\u533a\u8303\u56f4\u963b";
                if (mode == CityToolMode.BuildBuilding) return "\u5efa\u7b51\u5360\u5730\u963b";
                if (mode == CityToolMode.Demolish) return "\u62c6\u9664\u963b";
            }

            return ToolModeBlockedIssue(mode);
        }

        private static string BlockedReasonFromText(string detail, CityToolMode mode)
        {
            if (ContainsText(detail, "\u672a\u89e3\u9501") || ContainsText(detail, "\u9501\u5b9a") || ContainsText(detail, "\u672a\u5f00\u653e"))
            {
                return "\u533a\u672a\u5f00";
            }

            if (ContainsText(detail, "\u73b0\u91d1\u4e0d\u8db3"))
            {
                return "\u73b0\u91d1\u7f3a";
            }

            if (ContainsText(detail, "\u8d85\u51fa") || ContainsText(detail, "\u5730\u56fe\u8fb9\u754c"))
            {
                return "\u8d8a\u5efa\u754c";
            }

            if (ContainsText(detail, "\u6b64\u5904\u6ca1\u6709\u9053\u8def") || ContainsText(detail, "\u6ca1\u6709\u9053\u8def"))
            {
                if (mode == CityToolMode.BuildBuilding) return "\u95e8\u524d\u672a\u63a5\u8def";
                if (mode == CityToolMode.UpgradeRoad) return "\u811a\u4e0b\u975e\u53ef\u5347\u8def";
                return "\u65e0\u53ef\u63a5\u8def";
            }

            if (ContainsText(detail, "\u5df2\u7ecf\u662f\u4e3b\u5e72\u9053"))
            {
                return "\u8def\u5df2\u4e3b\u5e72";
            }

            if (ContainsText(detail, "\u6ca1\u6709\u5efa\u7b51"))
            {
                return "\u683c\u5185\u65e0\u5efa\u7b51";
            }

            if (ContainsText(detail, "\u63a8\u8350\u5206\u533a"))
            {
                return "\u683c\u5206\u533a\u9519";
            }

            if (ContainsText(detail, "\u4e0d\u80fd") || ContainsText(detail, "\u4e0d\u53ef") || ContainsText(detail, "\u88ab\u5360\u7528") || ContainsText(detail, "\u6c34\u9762"))
            {
                if (mode == CityToolMode.BuildRoad) return "\u8def\u649e\u5efa\u7b51/\u6c34";
                if (mode == CityToolMode.ZonePaint) return "\u8303\u56f4\u542b\u8def/\u5efa/\u6c34";
                if (mode == CityToolMode.BuildBuilding) return "\u5360\u5730\u51b2\u7a81/\u6c34";
                if (mode == CityToolMode.Demolish) return "\u4e0d\u53ef\u62c6";
            }

            return "\u9884\u672a\u8fc7";
        }

        private static string BlockedNextActionFromText(string detail, CityToolMode mode)
        {
            if (ContainsText(detail, "\u672a\u89e3\u9501") || ContainsText(detail, "\u9501\u5b9a") || ContainsText(detail, "\u672a\u5f00\u653e"))
            {
                return LockedRegionNextAction(mode);
            }

            if (ContainsText(detail, "\u73b0\u91d1\u4e0d\u8db3"))
            {
                return "\u7b49\u6536\u5165";
            }

            if (ContainsText(detail, "\u8d85\u51fa") || ContainsText(detail, "\u5730\u56fe\u8fb9\u754c"))
            {
                return "\u8d77\u7ec8\u70b9\u6536\u56de\u56fe\u5185";
            }

            if (ContainsText(detail, "\u6b64\u5904\u6ca1\u6709\u9053\u8def") || ContainsText(detail, "\u6ca1\u6709\u9053\u8def"))
            {
                if (mode == CityToolMode.UpgradeRoad) return "\u4ea4\u901a\u5c42\u70b9\u652f\u8def";
                if (mode == CityToolMode.BuildBuilding) return "\u4ea4\u901a\u5c42\u5148\u63a5\u8def";
                return "\u4ea4\u901a\u5c42\u5148\u63a5\u8def";
            }

            if (ContainsText(detail, "\u5df2\u7ecf\u662f\u4e3b\u5e72\u9053"))
            {
                return "\u4ea4\u901a\u5c42\u5e73\u884c\u5206\u6d41";
            }

            if (ContainsText(detail, "\u6ca1\u6709\u5efa\u7b51"))
            {
                return "\u666e\u901a\u5c42\u70b9\u5efa\u7b51";
            }

            if (ContainsText(detail, "\u63a8\u8350\u5206\u533a"))
            {
                return mode == CityToolMode.BuildBuilding ? "\u5206\u533a\u5c42\u6539\u63a8\u8350\u533a" : "\u5206\u533a\u5c42\u6362\u7c7b/\u5730";
            }

            if (ContainsText(detail, "\u4e0d\u80fd") || ContainsText(detail, "\u4e0d\u53ef") || ContainsText(detail, "\u88ab\u5360\u7528") || ContainsText(detail, "\u6c34\u9762"))
            {
                if (mode == CityToolMode.BuildRoad) return "\u4ea4\u901a\u5c42\u907f\u7ea2\u683c/\u5148\u62c6";
                if (mode == CityToolMode.ZonePaint) return "\u5206\u533a\u5c42\u91cd\u62c9\u7eff\u683c";
                if (mode == CityToolMode.BuildBuilding) return "\u5f53\u524d\u5c42\u6362\u63a5\u8def\u7a7a\u5730";
                if (mode == CityToolMode.Demolish) return "\u666e\u901a\u5c42\u70b9\u53ef\u62c6\u5bf9\u8c61";
            }

            return ToolModeFallbackAction(mode);
        }

        private static string ToolModeBlockedIssue(CityToolMode mode)
        {
            if (mode == CityToolMode.BuildRoad) return "\u8def\u53d7\u963b";
            if (mode == CityToolMode.ZonePaint) return "\u5206\u533a\u53d7\u963b";
            if (mode == CityToolMode.BuildBuilding) return "\u9009\u5740\u53d7\u963b";
            if (mode == CityToolMode.UpgradeRoad) return "\u5347\u8def\u53d7\u963b";
            if (mode == CityToolMode.Demolish) return "\u62c6\u9664\u53d7\u963b";
            return "\u53d7\u963b";
        }

        private static string ToolModeFallbackAction(CityToolMode mode)
        {
            if (mode == CityToolMode.BuildRoad) return "\u4ea4\u901a\u5c42\u7eff\u683c\u8d77\u7ebf";
            if (mode == CityToolMode.ZonePaint) return "\u5206\u533a\u5c42\u91cd\u62c9\u7eff\u683c";
            if (mode == CityToolMode.BuildBuilding) return "\u5f53\u524d\u5c42\u6362\u63a5\u8def\u5730";
            if (mode == CityToolMode.UpgradeRoad) return "\u4ea4\u901a\u5c42\u70b9\u652f\u8def";
            if (mode == CityToolMode.Demolish) return "\u666e\u901a\u5c42\u70b9\u5efa\u7b51";
            return "\u666e\u901a\u5c42\u6362\u4f4d\u7f6e";
        }

        private static string SuccessNextAction(CityToolMode mode)
        {
            if (mode == CityToolMode.BuildRoad) return "\u4ea4\u901a\u5c42\u770b\u65ad/\u5835";
            if (mode == CityToolMode.ZonePaint) return "\u5206\u533a\u5c42\u770b\u9700/\u51b2";
            if (mode == CityToolMode.BuildBuilding) return "\u5f53\u524d\u5c42\u770b\u8986\u76d6";
            if (mode == CityToolMode.UpgradeRoad) return "\u4ea4\u901a\u5c42\u770b\u5bb9/\u5206";
            if (mode == CityToolMode.Demolish) return "\u666e\u901a\u5c42\u770b\u5730";
            return "\u5bf9\u5e94\u56fe\u5c42\u590d\u6838";
        }

        private static string LockedRegionNextAction(CityToolMode mode)
        {
            if (mode == CityToolMode.BuildRoad) return "\u5148\u6cbf\u5f00\u653e\u8fb9\u6536\u53e3";
            if (mode == CityToolMode.ZonePaint) return "\u5148\u5237\u5f00\u653e\u533a";
            if (mode == CityToolMode.BuildBuilding) return "\u56de\u5f00\u653e\u683c\u63a5\u8def";
            if (mode == CityToolMode.UpgradeRoad) return "\u5148\u5347\u5f00\u653e\u8def";
            if (mode == CityToolMode.Demolish) return "\u5148\u89e3\u9501";
            return "\u4efb\u9762\u677f\u5b8c\u6210\u89e3\u9501";
        }

        private static string DetailWithPrefix(string prefix, string detail)
        {
            return string.IsNullOrEmpty(detail) ? prefix + "\u6682\u65e0\u7ec6\u8282" : prefix + detail;
        }

        private string ToolDiagnosticClause(string issue, string reason, string recommendation)
        {
            return DiagnosticClause(issue, reason, recommendation, ToolRecommendedLayerLabel(toolMode));
        }

        private string ToolRecommendedLayerLabel(CityToolMode mode)
        {
            if (mode == CityToolMode.BuildRoad || mode == CityToolMode.UpgradeRoad) return OverlayToolLabel(OverlayMode.Traffic);
            if (mode == CityToolMode.ZonePaint) return OverlayToolLabel(OverlayMode.Zoning);
            if (mode == CityToolMode.BuildBuilding) return OverlayToolLabel(OverlayForBuilding(selectedBuildingId));
            if (mode == CityToolMode.Demolish) return OverlayToolLabel(OverlayMode.Normal);
            return OverlayToolLabel(OverlayMode.Normal);
        }

        private static string DiagnosticClause(string issue, string reason, string recommendation)
        {
            return DiagnosticClause(issue, reason, recommendation, string.Empty);
        }

        private static string DiagnosticClause(string issue, string reason, string recommendation, string layer)
        {
            var layerLabel = string.IsNullOrEmpty(layer) ? LayerFromText(recommendation) : layer;
            var text = "  \u72b6:" + DiagnosticPart(issue, DiagnosticPart(reason, "\u5f85\u786e\u8ba4"))
                + "  \u505a:" + DiagnosticPart(ShortFixText(recommendation, layerLabel), "\u79fb\u52a8\u5149\u6807");
            if (!string.IsNullOrEmpty(layerLabel))
            {
                text += "  \u5c42:" + layerLabel;
            }

            return text;
        }

        private static string ShortFixText(string value, string layer)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var text = value;
            if (text.StartsWith("\u9053\u8def\u5de5\u5177", System.StringComparison.Ordinal))
            {
                text = text.Substring(4);
            }

            if (text.StartsWith("\u5206\u533a\u5de5\u5177/", System.StringComparison.Ordinal))
            {
                text = text.Substring(5);
            }
            else if (text.StartsWith("\u5206\u533a\u5de5\u5177", System.StringComparison.Ordinal))
            {
                text = text.Substring(4);
            }

            if (!string.IsNullOrEmpty(layer) && text.StartsWith(layer + "\u5c42", System.StringComparison.Ordinal))
            {
                text = text.Substring(layer.Length + 1);
            }

            if (text.StartsWith("\u5f53\u524d\u5c42", System.StringComparison.Ordinal))
            {
                text = text.Substring(3);
            }

            if (text.StartsWith("\u5bf9\u5e94\u56fe\u5c42", System.StringComparison.Ordinal))
            {
                text = text.Substring(4);
            }

            return text.TrimStart();
        }

        private static string LayerFromText(string value)
        {
            if (ContainsText(value, "\u4ea4\u901a\u5c42") || ContainsText(value, "\u9053\u8def\u5de5\u5177")) return "\u4ea4\u901a";
            if (ContainsText(value, "\u5206\u533a\u5c42") || ContainsText(value, "\u5206\u533a\u5de5\u5177")) return "\u5206\u533a";
            if (ContainsText(value, "\u670d\u52a1\u5c42")) return "\u670d\u52a1";
            if (ContainsText(value, "\u516c\u4ea4\u5c42")) return "\u516c\u4ea4";
            if (ContainsText(value, "\u8d27\u8fd0\u5c42")) return "\u8d27\u8fd0";
            if (ContainsText(value, "\u6c34\u7535\u5c42")) return "\u6c34\u7535";
            if (ContainsText(value, "\u8def\u5b89\u5c42")) return "\u8def\u5b89";
            if (ContainsText(value, "\u505c\u8f66\u5c42")) return "\u505c\u8f66";
            if (ContainsText(value, "\u96e8\u6d2a\u5c42")) return "\u96e8\u6d2a";
            if (ContainsText(value, "\u5730\u4ef7\u5c42")) return "\u5730\u4ef7";
            if (ContainsText(value, "\u666e\u901a\u5c42")) return "\u666e\u901a";
            return string.Empty;
        }

        private static string DiagnosticPart(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static bool ContainsText(string value, string token)
        {
            return !string.IsNullOrEmpty(value)
                && !string.IsNullOrEmpty(token)
                && value.IndexOf(token, System.StringComparison.Ordinal) >= 0;
        }

        private static string CompactHoverFeedback(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 68)
            {
                return string.IsNullOrEmpty(value) ? string.Empty : value;
            }

            return value.Substring(0, 67) + "...";
        }

        private void PublishPendingBuildingFeedback(GridPos pos, string buildingId, int siteScore, int signature)
        {
            if (controller == null || lastHoverHudFeedbackSignature == signature)
            {
                return;
            }

            lastHoverHudFeedbackSignature = signature;
            controller.PublishHudFeedback(CompactHoverFeedback(BuildPendingBuildingFeedback(pos, buildingId, siteScore)), false);
        }

        private string BuildPendingBuildingFeedback(GridPos pos, string buildingId, int siteScore)
        {
            return "\u505a " + BuildingToolLabel(buildingId) + " " + pos.X + "," + pos.Y + "  \u8bc4" + siteScore + ToolDiagnosticClause("\u5f85\u843d\u5730", "\u9009\u5740\u53ef\u7528", "\u518d\u70b9\u540c\u683c\u843d\u5730");
        }

        private bool IsPendingBuildingConfirm(GridPos pos, string buildingId)
        {
            return hasPendingBuildingConfirm
                && pendingBuildingPos.X == pos.X
                && pendingBuildingPos.Y == pos.Y
                && pendingBuildingId == buildingId;
        }

        private string BuildBlockedPreviewFeedback(ConstructionPreview preview)
        {
            var detail = PreviewFeedbackDetail(preview);
            if (!string.IsNullOrEmpty(detail))
            {
                return "\u53d7\u963b" + ToolDiagnosticClause(BlockedIssueFromText(detail, toolMode), BlockedReasonFromText(detail, toolMode), BlockedNextActionFromText(detail, toolMode));
            }

            return "\u53d7\u963b" + ToolDiagnosticClause(ToolModeBlockedIssue(toolMode), "\u4f4d\u7f6e\u4e0d\u53ef\u5efa\u9020", ToolModeFallbackAction(toolMode));
        }

        private bool HandleLockedRegionTap(GridPos gridPos)
        {
            if (!IsLockedRegionTile(gridPos))
            {
                return false;
            }

            ClearPendingBuildingConfirm();
            hasDragStart = false;
            lastHoverPreviewSignature = int.MinValue;
            if (mapRenderer != null)
            {
                mapRenderer.ClearPlacementPreview();
                mapRenderer.ShowLockedRegionTapMarker(gridPos);
            }

            PublishLockedRegionFeedback(gridPos);
            return true;
        }

        private bool IsLockedRegionTile(GridPos pos)
        {
            var grid = controller != null ? controller.Grid : null;
            return grid != null && grid.IsLockedExpansionTile(pos);
        }

        private void PublishLockedRegionFeedback(GridPos gridPos)
        {
            if (controller == null)
            {
                return;
            }

            var metrics = controller.Metrics;
            var objective = metrics != null ? metrics.ActiveObjective : null;
            if (objective != null && objective.Required > 0)
            {
                var required = Mathf.Max(1, objective.Required);
                var progress = Mathf.Clamp(objective.Progress, 0, required);
                var title = string.IsNullOrEmpty(objective.Title) ? "\u89e3\u9501\u65b0\u533a" : ShortToolFeedbackText(objective.Title, 8);
                var hint = string.IsNullOrEmpty(objective.Hint) ? "\u5b8c\u6210\u5f53\u524d\u4efb" : ShortToolFeedbackText(objective.Hint, 13);
                controller.PublishHudFeedback("\u53d7\u963b \u9501\u533a " + gridPos.X + "," + gridPos.Y + ToolDiagnosticClause("\u9501\u533a", "\u4efb " + progress + "/" + required + " " + title, LockedRegionNextAction(toolMode) + " / " + hint), false);
                return;
            }

            controller.PublishHudFeedback("\u53d7\u963b \u9501\u533a " + gridPos.X + "," + gridPos.Y + ToolDiagnosticClause("\u9501\u533a", "\u6269\u5c55\u4efb\u672a\u5b8c\u6210", LockedRegionNextAction(toolMode)), false);
        }

        private void PublishInspectTileFeedback(GridPos gridPos)
        {
            if (controller == null)
            {
                return;
            }

            var tile = controller.GetTile(gridPos.X, gridPos.Y);
            if (tile == null)
            {
                return;
            }

            controller.PublishHudFeedback(BuildInspectTileFeedback(gridPos, tile), !IsLockedRegionTile(gridPos));
        }

        private string BuildInspectTileFeedback(GridPos gridPos, TileData tile)
        {
            if (IsLockedRegionTile(gridPos))
            {
                return BuildLockedTileInspectFeedback(gridPos);
            }

            if (!string.IsNullOrEmpty(tile.BuildingId))
            {
                return BuildBuildingInspectFeedback(gridPos, tile);
            }

            if (!string.IsNullOrEmpty(tile.RoadId))
            {
                return BuildRoadInspectFeedback(gridPos, tile);
            }

            return BuildEmptyTileInspectFeedback(gridPos, tile);
        }

        private string BuildLockedTileInspectFeedback(GridPos gridPos)
        {
            var metrics = controller != null ? controller.Metrics : null;
            var objective = metrics != null ? metrics.ActiveObjective : null;
            if (objective != null && objective.Required > 0)
            {
                var required = Mathf.Max(1, objective.Required);
                var progress = Mathf.Clamp(objective.Progress, 0, required);
                var title = string.IsNullOrEmpty(objective.Title) ? "\u89e3\u9501\u65b0\u533a" : ShortToolFeedbackText(objective.Title, 8);
                return "\u770b \u683c " + gridPos.X + "," + gridPos.Y + "  \u9501\u533a"
                    + DiagnosticClause("\u9501\u533a", "\u4efb " + progress + "/" + required + " " + title, LockedRegionNextAction(toolMode));
            }

            return "\u770b \u683c " + gridPos.X + "," + gridPos.Y + "  \u9501\u533a"
                + DiagnosticClause("\u9501\u533a", "\u6269\u5c55\u4efb\u672a\u5b8c\u6210", LockedRegionNextAction(toolMode));
        }

        private string BuildBuildingInspectFeedback(GridPos gridPos, TileData tile)
        {
            var building = controller != null ? controller.GetPlacedBuildingAt(gridPos.X, gridPos.Y) : null;
            var label = building != null ? BuildingToolLabel(building.ConfigId) : tile.BuildingId;
            var level = building != null ? " Lv" + building.Level : string.Empty;
            return "\u770b \u683c " + gridPos.X + "," + gridPos.Y + "  \u5efa " + label + level
                + "  \u670d" + ServiceAccessValue(tile)
                + "  \u8def" + tile.Traffic
                + BuildingInspectHint(tile, building);
        }

        private string BuildRoadInspectFeedback(GridPos gridPos, TileData tile)
        {
            var road = controller != null ? controller.GetRoadAt(gridPos.X, gridPos.Y) : null;
            var load = road != null ? road.Load + "/" + road.Capacity : tile.Traffic.ToString();
            var tier = road != null ? RoadTierInspectLabel(road.Tier) : "\u9053\u8def";
            return "\u770b \u683c " + gridPos.X + "," + gridPos.Y + "  \u8def " + tier
                + "  \u8f66" + load
                + "  \u517b" + tile.RoadMaintenanceAccess
                + RoadInspectHint(tile, road);
        }

        private string BuildEmptyTileInspectFeedback(GridPos gridPos, TileData tile)
        {
            return "\u770b \u683c " + gridPos.X + "," + gridPos.Y + "  \u7a7a " + TerrainInspectLabel(tile.Terrain) + "/" + ZoneInspectLabel(tile.Zone)
                + "  \u4ef7" + tile.LandValue
                + EmptyTilePressureSuffix(tile)
                + EmptyTileInspectHint(tile);
        }

        private static string BuildingInspectHint(TileData tile, PlacedBuilding building)
        {
            if (building != null && string.IsNullOrEmpty(building.ConnectedRoadId)) return DiagnosticClause("\u5efa\u5b64\u7acb", "\u672a\u63a5\u8def", "\u9053\u8def\u5de5\u5177\u63a5\u652f\u8def");
            if (tile.Traffic >= 70) return DiagnosticClause("\u8def\u5835", "\u95e8\u524d\u8f66\u9ad8", "\u4ea4\u901a\u5c42\u5347\u8def/\u5206\u6d41");
            if (ServiceAccessValue(tile) < 26)
            {
                var facility = WeakestServiceFacilityLabel(tile);
                return DiagnosticClause("\u670d\u7f3a", facility + "\u4f4e", "\u670d\u52a1\u5c42\u8865" + facility);
            }

            if (tile.LandValue < 35) return DiagnosticClause("\u5347\u7ea7\u963b", "\u5730\u4ef7\u4f4e", "\u5730\u4ef7\u5c42\u8865\u516c\u56ed/\u5e7f\u573a");
            return DiagnosticClause("\u53ef\u63a7", "\u670d/\u8def\u7a33", "\u7ee7\u7eed\u770b\u5bf9\u5e94\u56fe\u5c42");
        }

        private static string RoadInspectHint(TileData tile, RoadNode road)
        {
            if (road != null && road.Capacity > 0 && road.Load >= road.Capacity) return DiagnosticClause("\u8def\u6ee1", "\u8f66" + road.Load + "/" + road.Capacity, "\u5347\u8def/\u4ea4\u901a\u5c42\u5206\u6d41");
            if (tile.Traffic >= 70) return DiagnosticClause("\u8def\u5835", "\u70ed\u70b9\u9ad8", "\u4ea4\u901a\u5c42\u5347\u7ea7/\u5206\u6d41");
            if (tile.RoadMaintenanceAccess < 24 && tile.Traffic > 0) return DiagnosticClause("\u517b\u7f3a", "\u9ad8\u8f66\u7f3a\u517b", "\u8def\u5b89\u5c42\u5efa\u517b\u62a4\u7ad9");
            if (road != null && road.NeighborCount <= 1) return DiagnosticClause("\u65ad\u5934\u8def", "\u53ea\u8fde\u4e00\u4fa7", "\u9053\u8def\u5de5\u5177\u7ee7\u7eed\u63a5");
            return DiagnosticClause("\u901a\u884c\u7a33", "\u5bb9/\u517b\u53ef\u63a7", "\u4ea4\u901a\u5c42\u6301\u7eed\u770b");
        }

        private string EmptyTileInspectHint(TileData tile)
        {
            if (tile.Terrain == TerrainType.Water) return DiagnosticClause("\u5730\u53d7\u9650", "\u6c34\u9762", "\u666e\u901a\u5c42\u6362\u5e73\u5730");
            if (tile.Zone == ZoneType.None) return DiagnosticClause("\u672a\u5206\u533a", "\u65e0\u7528\u5730", "\u5206\u533a\u5de5\u5177/\u5206\u533a\u5c42 " + OpenTileInspectZoningHint(controller != null ? controller.Metrics : null));
            if (tile.LandValue < 35) return DiagnosticClause("\u5165\u9a7b\u6162", "\u5730\u4ef7\u4f4e", "\u5730\u4ef7\u5c42\u5148\u8865\u516c\u56ed/\u5e7f\u573a");

            var demand = DemandForZone(controller != null ? controller.Metrics : null, tile.Zone);
            return demand > 0
                ? DiagnosticClause("\u7b49\u5165\u9a7b", "\u5206\u533a\u9700" + demand, "\u5206\u533a\u5c42\u4fdd\u6301\u8fde\u8def\u7a7a\u5730")
                : DiagnosticClause("\u9700\u4e0d\u5f3a", "\u672c\u533a\u9700\u4f4e", "\u5206\u533a\u5c42\u6539\u9ad8\u9700\u7c7b\u578b");
        }

        private static string EmptyTilePressureSuffix(TileData tile)
        {
            if (tile.Pollution >= 55)
            {
                return "  \u6c61\u67d3" + tile.Pollution;
            }

            if (tile.Noise >= 55)
            {
                return "  \u566a\u58f0" + tile.Noise;
            }

            if (tile.ParkingAccess > 0 && tile.ParkingAccess < 25)
            {
                return "  \u505c\u8f66" + tile.ParkingAccess;
            }

            if (tile.StormwaterAccess > 0 && tile.StormwaterAccess < 25)
            {
                return "  \u96e8\u6d2a" + tile.StormwaterAccess;
            }

            return string.Empty;
        }

        private static string OpenTileInspectZoningHint(CityMetrics metrics)
        {
            if (metrics == null || metrics.Demand == null)
            {
                return "\u53ef\u5212\u5206\u533a";
            }

            var zone = ZoneType.Residential;
            var demand = metrics.Demand.Residential;
            if (metrics.Demand.Commercial > demand)
            {
                zone = ZoneType.Commercial;
                demand = metrics.Demand.Commercial;
            }

            if (metrics.Demand.Industrial > demand)
            {
                zone = ZoneType.Industrial;
                demand = metrics.Demand.Industrial;
            }

            if (metrics.Demand.Office > demand)
            {
                zone = ZoneType.Office;
                demand = metrics.Demand.Office;
            }

            if (metrics.Demand.MixedUse > demand)
            {
                zone = ZoneType.MixedUse;
                demand = metrics.Demand.MixedUse;
            }

            return "\u53ef\u5212" + ZoneToolLabel(zone) + " \u9700" + demand;
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

        private static int ServiceAccessValue(TileData tile)
        {
            return Mathf.Max(tile.ParkAccess, Mathf.Max(tile.HealthAccess, Mathf.Max(tile.DeathcareAccess, Mathf.Max(tile.EducationAccess, Mathf.Max(Mathf.Max(tile.SafetyAccess, tile.FireProtectionAccess), tile.SecurityAccess)))));
        }

        private static string WeakestServiceFacilityLabel(TileData tile)
        {
            var label = "\u516c\u56ed";
            var value = tile.ParkAccess;
            SetWeakestService(ref label, ref value, "\u8bca\u6240", tile.HealthAccess);
            SetWeakestService(ref label, ref value, "\u5b66\u6821", tile.EducationAccess);
            SetWeakestService(ref label, ref value, "\u6d88\u9632", tile.FireProtectionAccess);
            SetWeakestService(ref label, ref value, "\u8b66\u52a1", Mathf.Max(tile.SafetyAccess, tile.SecurityAccess));
            SetWeakestService(ref label, ref value, "\u751f\u547d", tile.DeathcareAccess);
            return label;
        }

        private static void SetWeakestService(ref string label, ref int value, string candidateLabel, int candidateValue)
        {
            if (candidateValue < value)
            {
                label = candidateLabel;
                value = candidateValue;
            }
        }

        private static string RoadTierInspectLabel(RoadTier tier)
        {
            return tier == RoadTier.Arterial ? "\u4e3b\u5e72\u9053" : "\u652f\u8def";
        }

        private static string TerrainInspectLabel(TerrainType terrain)
        {
            if (terrain == TerrainType.Water) return "\u6c34\u9762";
            if (terrain == TerrainType.Hill) return "\u4e18\u9675";
            return "\u5e73\u5730";
        }

        private static string ZoneInspectLabel(ZoneType zone)
        {
            return zone == ZoneType.None ? "\u672a\u5206\u533a" : ZoneToolLabel(zone);
        }

        private void ClearPendingBuildingConfirm()
        {
            hasPendingBuildingConfirm = false;
            pendingBuildingId = string.Empty;
        }

        private void SelectTileForInspector(GridPos gridPos)
        {
            // TILE_INSPECTOR_OVERLAY_LEGEND uses the last valid map target as a read-only HUD focus.
            selectedTile = gridPos;
            hasSelectedTile = true;
        }

        private void ShowSelectedTileFocus(GridPos gridPos)
        {
            if (mapRenderer != null)
            {
                mapRenderer.ShowSelectedTileFocus(gridPos);
            }
        }

        private void ResetHoverPreview()
        {
            lastHoverPreviewSignature = int.MinValue;
            lastHoverHudFeedbackSignature = int.MinValue;
            if (mapRenderer != null)
            {
                mapRenderer.ClearPlacementPreview();
            }
        }

        private void CancelDragPreview()
        {
            // CITY_BUILDER_CANCEL_DRAG_ON_HUD prevents releasing over HUD from confirming map actions underneath.
            var hadDrag = hasDragStart;
            var hadPendingBuilding = hasPendingBuildingConfirm;
            hasDragStart = false;
            ClearPendingBuildingConfirm();
            ResetHoverPreview();
            if ((hadDrag || hadPendingBuilding) && controller != null)
            {
                controller.PublishHudFeedback(hadPendingBuilding ? "\u53d6\u6d88 \u5efa\u9020" : "\u53d6\u6d88 \u62d6\u62fd", true);
            }
        }

        private int HoverPreviewSignature(GridPos hoverPos)
        {
            unchecked
            {
                var hash = 37;
                hash = hash * 31 + (int)toolMode;
                hash = hash * 31 + hoverPos.X;
                hash = hash * 31 + hoverPos.Y;
                hash = hash * 31 + (hasDragStart ? 1 : 0);
                hash = hash * 31 + dragStart.X;
                hash = hash * 31 + dragStart.Y;
                hash = hash * 31 + (int)selectedZone;
                hash = hash * 31 + StringHash(selectedBuildingId);
                hash = hash * 31 + (hasPendingBuildingConfirm ? 1 : 0);
                if (hasPendingBuildingConfirm)
                {
                    hash = hash * 31 + pendingBuildingPos.X;
                    hash = hash * 31 + pendingBuildingPos.Y;
                    hash = hash * 31 + StringHash(pendingBuildingId);
                }

                hash = hash * 31 + (controller != null ? (int)controller.OverlayMode : 0);
                return hash;
            }
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

        private bool TryScreenToGrid(Vector2 screenPosition, out GridPos gridPos)
        {
            gridPos = new GridPos();
            var cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (cameraToUse == null)
            {
                return false;
            }

            var ray = cameraToUse.ScreenPointToRay(screenPosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (!ground.Raycast(ray, out distance))
            {
                return false;
            }

            var world = ray.GetPoint(distance);
            gridPos = mapRenderer.WorldToGrid(world);
            return controller.Grid != null && controller.Grid.InBounds(gridPos);
        }
    }
}
