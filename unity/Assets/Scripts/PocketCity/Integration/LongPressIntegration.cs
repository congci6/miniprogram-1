using UnityEngine;
using PocketCity.Input;
using PocketCity.Simulation;
using PocketCity.Core;
using PocketCity.Runtime;

namespace PocketCity.Integration
{
    /// <summary>
    /// 长按系统集成器 - 解决F-17长按未集成到主交互路由
    /// </summary>
    public class LongPressIntegration : MonoBehaviour
    {
        [SerializeField] private LongPressOperationSystem longPressSystem;
        [SerializeField] private CityInteractionController interactionController;
        [SerializeField] private CitySimulationCore simulation;

        private CityToolMode currentToolMode = CityToolMode.None;
        private string currentBuildingId;
        private ZoneType currentZoneType;

        private void Start()
        {
            // 自动查找系统
            if (longPressSystem == null)
                longPressSystem = FindAnyObjectByType<LongPressOperationSystem>();

            if (interactionController == null)
                interactionController = FindAnyObjectByType<CityInteractionController>();

            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }

            // 订阅长按事件
            if (longPressSystem != null)
            {
                longPressSystem.OnLongPressStart += HandleLongPressStart;
                longPressSystem.OnLongPressContinue += HandleLongPressContinue;
                longPressSystem.OnLongPressEnd += HandleLongPressEnd;
            }
        }

        /// <summary>
        /// 设置工具模式（从CityInteractionController调用）
        /// </summary>
        public void SetToolMode(CityToolMode mode, string buildingId = null, ZoneType zoneType = ZoneType.None)
        {
            currentToolMode = mode;
            currentBuildingId = buildingId;
            currentZoneType = zoneType;
        }

        private void HandleLongPressStart(Vector3 worldPos)
        {
            GridPos gridPos = WorldToGrid(worldPos);

            switch (currentToolMode)
            {
                case CityToolMode.PlaceBuilding:
                    TryPlaceBuildingAt(gridPos);
                    break;

                case CityToolMode.PlaceZone:
                    TryPlaceZoneAt(gridPos);
                    break;

                case CityToolMode.PlaceRoad:
                    TryPlaceRoadAt(gridPos);
                    break;

                case CityToolMode.Demolish:
                    TryDemolishAt(gridPos);
                    break;
            }
        }

        private void HandleLongPressContinue(Vector3 worldPos)
        {
            GridPos gridPos = WorldToGrid(worldPos);

            switch (currentToolMode)
            {
                case CityToolMode.PlaceBuilding:
                    TryPlaceBuildingAt(gridPos);
                    break;

                case CityToolMode.PlaceZone:
                    TryPlaceZoneAt(gridPos);
                    break;

                case CityToolMode.PlaceRoad:
                    TryPlaceRoadAt(gridPos);
                    break;

                case CityToolMode.Demolish:
                    TryDemolishAt(gridPos);
                    break;
            }
        }

        private void HandleLongPressEnd()
        {
            // 长按结束，可以显示总结或播放音效
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Click);
            }
        }

        private void TryPlaceBuildingAt(GridPos pos)
        {
            if (simulation == null || string.IsNullOrEmpty(currentBuildingId))
                return;

            var preview = simulation.PreviewPlaceBuilding(currentBuildingId, pos, (int)BuildingRotation.None);
            if (preview.Ok)
            {
                bool success = simulation.TryPlaceBuildingAt(currentBuildingId, pos, (int)BuildingRotation.None, out _);
                if (success)
                {
                    // 播放音效
                    if (Audio.AudioManager.Instance != null)
                    {
                        Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingPlaced);
                    }
                }
            }
        }

        private void TryPlaceZoneAt(GridPos pos)
        {
            if (simulation == null || currentZoneType == ZoneType.None)
                return;

            if (simulation.Grid.GetZoneType(pos) == ZoneType.None)
            {
                simulation.Grid.SetZoneType(pos, currentZoneType);

                // 播放音效
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Click);
                }
            }
        }

        private void TryPlaceRoadAt(GridPos pos)
        {
            if (simulation == null)
                return;

            if (simulation.Grid.GetRoadType(pos) == RoadType.None)
            {
                simulation.Grid.SetRoadType(pos, RoadType.Local);

                // 播放音效
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.Click);
                }
            }
        }

        private void TryDemolishAt(GridPos pos)
        {
            if (simulation == null)
                return;

            // 查找建筑
            string buildingId = simulation.Grid.FindBuildingIdAt(pos);
            if (!string.IsNullOrEmpty(buildingId))
            {
                simulation.TryDemolish(buildingId);

                // 播放音效
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingDemolished);
                }
            }
        }

        private GridPos WorldToGrid(Vector3 worldPos)
        {
            return new GridPos(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z));
        }

        private void OnDestroy()
        {
            if (longPressSystem != null)
            {
                longPressSystem.OnLongPressStart -= HandleLongPressStart;
                longPressSystem.OnLongPressContinue -= HandleLongPressContinue;
                longPressSystem.OnLongPressEnd -= HandleLongPressEnd;
            }
        }
    }

    public enum CityToolMode
    {
        None,
        PlaceBuilding,
        PlaceZone,
        PlaceRoad,
        Demolish,
        Inspect
    }
}
