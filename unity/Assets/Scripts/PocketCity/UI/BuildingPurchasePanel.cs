using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketCity.Simulation;
using PocketCity.Core;

namespace PocketCity.UI
{
    /// <summary>
    /// 建筑购买面板 - 手动放置住宅
    /// </summary>
    public class BuildingPurchasePanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject buildingButtonPrefab;
        [SerializeField] private CitySimulationCore simulation;

        private string selectedBuildingId;

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            RefreshBuildingList();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            selectedBuildingId = null;
        }

        private void RefreshBuildingList()
        {
            if (buttonContainer == null || simulation == null) return;

            // 清空现有按钮
            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            // 添加可购买建筑
            var buildings = new[] {
                new { id = "residential_1", name = "小型住宅", cost = 500 },
                new { id = "residential_2", name = "中型住宅", cost = 1000 },
                new { id = "commercial_1", name = "小商店", cost = 800 },
                new { id = "industrial_1", name = "小工厂", cost = 1200 }
            };

            foreach (var building in buildings)
            {
                var btn = TMPUIHelper.CreateButton(buttonContainer, building.id,
                    $"{building.name}\n${building.cost}",
                    () => OnBuildingSelected(building.id));
            }
        }

        private void OnBuildingSelected(string buildingId)
        {
            selectedBuildingId = buildingId;
            Hide();

            // 进入放置模式
            if (BuildingPlacementController.Instance != null)
            {
                BuildingPlacementController.Instance.StartPlacement(buildingId);
            }
        }
    }

    /// <summary>
    /// 建筑放置控制器
    /// </summary>
    public class BuildingPlacementController : MonoBehaviour
    {
        public static BuildingPlacementController Instance { get; private set; }

        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private Camera mainCamera;

        private string currentBuildingId;
        private bool isPlacing = false;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartPlacement(string buildingId)
        {
            currentBuildingId = buildingId;
            isPlacing = true;
        }

        private void Update()
        {
            if (!isPlacing || simulation == null || mainCamera == null) return;

            // 显示预览
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 worldPos = hit.point;
                    GridPos gridPos = new GridPos(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z));

                    TryPlaceBuilding(gridPos);
                }
            }

            // ESC取消
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }

        private void TryPlaceBuilding(GridPos pos)
        {
            if (simulation == null) return;

            var definition = simulation.Config.GetBuilding(currentBuildingId);
            if (definition == null) return;

            // 检查金币
            if (UnifiedCurrencySystem.Instance != null)
            {
                if (!UnifiedCurrencySystem.Instance.SpendCash(definition.Cost))
                {
                    Debug.Log("金币不足");
                    return;
                }
            }

            // 放置建筑
            var preview = simulation.PreviewPlaceBuilding(currentBuildingId, pos, (int)BuildingRotation.None);
            if (preview.Ok)
            {
                simulation.TryPlaceBuildingAt(currentBuildingId, pos, (int)BuildingRotation.None, out _);

                // 播放音效
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingPlaced);
                }

                CancelPlacement();
            }
        }

        private void CancelPlacement()
        {
            isPlacing = false;
            currentBuildingId = null;
        }
    }
}
