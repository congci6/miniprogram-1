using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketCity.Simulation;
using PocketCity.Integration;
using PocketCity.Achievement;

namespace PocketCity.UI
{
    /// <summary>
    /// 建筑升级UI面板
    /// </summary>
    public class BuildingUpgradePanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI requirementsText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        [Header("Material Icons")]
        [SerializeField] private Transform materialContainer;
        [SerializeField] private GameObject materialItemPrefab;

        private CitySimulationCore simulation;
        private ProductionCityBridge bridge;
        private string currentBuildingId;

        private void Awake()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        public void Initialize(CitySimulationCore sim, ProductionCityBridge bridgeRef)
        {
            simulation = sim;
            bridge = bridgeRef;
        }

        public void ShowForBuilding(string buildingId)
        {
            if (simulation == null || bridge == null) return;

            currentBuildingId = buildingId;
            var building = simulation.FindPlacedBuilding(buildingId);
            if (building == null) return;

            var definition = simulation.Config.GetBuilding(building.ConfigId);
            if (definition == null) return;

            // 显示面板
            if (panel != null)
                panel.SetActive(true);

            // 更新信息
            if (buildingNameText != null)
                buildingNameText.text = definition.Name;

            if (levelText != null)
                levelText.text = $"等级 {building.Level}/5";

            // 更新材料需求
            UpdateRequirements();

            // 检查是否可升级
            bool canUpgrade = bridge.CanUpgradeBuilding(buildingId);
            if (upgradeButton != null)
            {
                upgradeButton.interactable = canUpgrade;
                var btnText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = canUpgrade ? "升级" : "材料不足";
                }
            }
        }

        private void UpdateRequirements()
        {
            if (bridge == null || requirementsText == null) return;

            string reqText = bridge.GetUpgradeRequirementsText(currentBuildingId);
            requirementsText.text = reqText;
        }

        private void OnUpgradeClicked()
        {
            if (bridge == null || string.IsNullOrEmpty(currentBuildingId)) return;

            bool success = bridge.TryUpgradeWithMaterials(currentBuildingId);

            if (success)
            {
                var upgradedBuilding = simulation.FindPlacedBuilding(currentBuildingId);

                // 升级成功特效
                if (VFX.ParticleEffectSystem.Instance != null && upgradedBuilding != null)
                {
                    VFX.ParticleEffectSystem.Instance.PlayEffect(
                        VFX.EffectType.LevelUp,
                        upgradedBuilding.FootprintOrigin.ToVector3()
                    );
                }

                // 更新成就
                if (AchievementSystem.Instance != null)
                {
                    AchievementSystem.Instance.UpdateProgress(
                        AchievementType.BuildingCount,
                        simulation.Buildings.Count
                    );
                }

                // 刷新UI
                UpdateRequirements();

                if (upgradedBuilding != null)
                {
                    if (levelText != null)
                        levelText.text = $"等级 {upgradedBuilding.Level}/5";

                    // 检查是否已满级
                    if (upgradedBuilding.Level >= 5)
                    {
                        Hide();
                    }
                }
            }
            else
            {
                // 升级失败提示
                Debug.Log("升级失败：材料不足或不满足条件");
            }
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);

            currentBuildingId = null;
        }
    }

    /// <summary>
    /// 建筑信息显示器（点击建筑时显示）
    /// </summary>
    public class BuildingInfoDisplay : MonoBehaviour
    {
        public static BuildingInfoDisplay Instance { get; private set; }

        [SerializeField] private BuildingUpgradePanel upgradePanel;
        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private ProductionCityBridge bridge;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (upgradePanel != null)
            {
                upgradePanel.Initialize(simulation, bridge);
            }
        }

        public void OnBuildingClicked(string buildingId)
        {
            if (upgradePanel != null)
            {
                upgradePanel.ShowForBuilding(buildingId);
            }
        }
    }
}
