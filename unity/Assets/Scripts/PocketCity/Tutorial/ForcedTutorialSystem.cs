using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PocketCity.Core;
using PocketCity.Simulation;

namespace PocketCity.Tutorial
{
    public enum ForcedTutorialStepType
    {
        PlaceRoad,          // 铺路
        PlaceBuilding,      // 放置建筑
        ZoneArea,           // 划区
        BuildFactory,       // 建工厂
        StartProduction,    // 开始生产
        WaitProduction,     // 等待生产
        CollectMaterial,    // 收集材料
        UpgradeBuilding,    // 升级建筑
        CheckPopulation,    // 检查人口
        Complete            // 完成
    }

    [System.Serializable]
    public class ForcedTutorialStep
    {
        public ForcedTutorialStepType type;
        public string title;
        public string description;
        public Vector3 highlightPosition;
    }

    /// <summary>
    /// 强制10步新手教程系统
    /// </summary>
    public class ForcedTutorialSystem : MonoBehaviour
    {
        public static ForcedTutorialSystem Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private GameObject arrowIndicator;
        [SerializeField] private Image blockingOverlay;

        [Header("References")]
        [SerializeField] private CitySimulationCore simulation;

        private ForcedTutorialStep[] steps;
        private int currentStepIndex = 0;
        private bool tutorialActive = false;
        private bool tutorialCompleted = false;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            InitializeSteps();
            LoadProgress();
        }

        private void Start()
        {
            if (!tutorialCompleted)
            {
                StartTutorial();
            }
        }

        private void InitializeSteps()
        {
            steps = new ForcedTutorialStep[]
            {
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.PlaceRoad,
                    title = "步骤 1: 铺设道路",
                    description = "点击道路工具，在闪烁位置铺设第一条道路"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.PlaceBuilding,
                    title = "步骤 2: 放置住宅",
                    description = "免费获得一个住宅单元！点击放置到地图上"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.ZoneArea,
                    title = "步骤 3: 划分区域",
                    description = "在住宅周围划出住宅区，让城市自然生长"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.BuildFactory,
                    title = "步骤 4: 建造工厂",
                    description = "免费获得一座建材厂！这是生产材料的关键"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.StartProduction,
                    title = "步骤 5: 开始生产",
                    description = "点击工厂 → 选择钉子 → 开始生产"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.WaitProduction,
                    title = "步骤 6: 等待完成",
                    description = "生产需要时间...（加速中）"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.CollectMaterial,
                    title = "步骤 7: 收集材料",
                    description = "点击工厂，领取完成的钉子"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.UpgradeBuilding,
                    title = "步骤 8: 升级住宅",
                    description = "点击住宅 → 使用钉子升级 → 人口增长！"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.CheckPopulation,
                    title = "步骤 9: 人口增长",
                    description = "恭喜！你的城市人口达到了10人"
                },
                new ForcedTutorialStep
                {
                    type = ForcedTutorialStepType.Complete,
                    title = "教程完成！",
                    description = "每天重复这个流程：产材料→升级→扩大城市\n\n奖励：100高级货币"
                }
            };
        }

        public void StartTutorial()
        {
            tutorialActive = true;
            currentStepIndex = 0;
            ShowCurrentStep();
            BlockInput(true);
        }

        private void ShowCurrentStep()
        {
            if (currentStepIndex >= steps.Length) return;

            var step = steps[currentStepIndex];

            if (tutorialPanel != null) tutorialPanel.SetActive(true);
            if (titleText != null) titleText.text = step.title;
            if (descriptionText != null) descriptionText.text = step.description;

            // 显示箭头指示
            if (arrowIndicator != null && step.highlightPosition != Vector3.zero)
            {
                arrowIndicator.SetActive(true);
                arrowIndicator.transform.position = step.highlightPosition + Vector3.up * 2f;
                StartCoroutine(BounceArrow());
            }

            // 根据步骤类型执行特殊逻辑
            HandleStepLogic(step);
        }

        private void HandleStepLogic(ForcedTutorialStep step)
        {
            switch (step.type)
            {
                case ForcedTutorialStepType.PlaceBuilding:
                    // 免费赠送住宅
                    if (UnifiedCurrencySystem.Instance != null)
                    {
                        UnifiedCurrencySystem.Instance.AddCash(500);
                    }
                    break;

                case ForcedTutorialStepType.BuildFactory:
                    // 免费赠送工厂
                    if (UnifiedCurrencySystem.Instance != null)
                    {
                        UnifiedCurrencySystem.Instance.AddCash(1200);
                    }
                    break;

                case ForcedTutorialStepType.WaitProduction:
                    // 加速生产到完成
                    StartCoroutine(AutoCompleteProduction());
                    break;

                case ForcedTutorialStepType.Complete:
                    // 奖励高级货币
                    if (UnifiedCurrencySystem.Instance != null)
                    {
                        UnifiedCurrencySystem.Instance.AddPremium(100);
                    }
                    break;
            }
        }

        private IEnumerator AutoCompleteProduction()
        {
            yield return new WaitForSeconds(2f);
            // TODO: 直接完成生产
            CompleteCurrentStep();
        }

        private IEnumerator BounceArrow()
        {
            if (arrowIndicator == null) yield break;

            Vector3 originalPos = arrowIndicator.transform.position;

            while (arrowIndicator.activeInHierarchy)
            {
                float offset = Mathf.Sin(Time.time * 3f) * 0.3f;
                arrowIndicator.transform.position = originalPos + Vector3.up * offset;
                yield return null;
            }
        }

        public void CompleteCurrentStep()
        {
            if (!tutorialActive) return;

            currentStepIndex++;

            if (currentStepIndex >= steps.Length)
            {
                CompleteTutorial();
            }
            else
            {
                ShowCurrentStep();
            }

            SaveProgress();
        }

        private void CompleteTutorial()
        {
            tutorialActive = false;
            tutorialCompleted = true;

            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (arrowIndicator != null) arrowIndicator.SetActive(false);

            BlockInput(false);
            SaveProgress();

            Debug.Log("教程完成！");
        }

        private void BlockInput(bool block)
        {
            if (blockingOverlay != null)
            {
                blockingOverlay.gameObject.SetActive(block);
                blockingOverlay.raycastTarget = block;
            }
        }

        public bool IsTutorialActive()
        {
            return tutorialActive;
        }

        public ForcedTutorialStepType GetCurrentStepType()
        {
            if (currentStepIndex >= steps.Length) return ForcedTutorialStepType.Complete;
            return steps[currentStepIndex].type;
        }

        // 外部调用通知步骤完成
        public void NotifyRoadPlaced() { if (GetCurrentStepType() == ForcedTutorialStepType.PlaceRoad) CompleteCurrentStep(); }
        public void NotifyBuildingPlaced() { if (GetCurrentStepType() == ForcedTutorialStepType.PlaceBuilding) CompleteCurrentStep(); }
        public void NotifyZonePlaced() { if (GetCurrentStepType() == ForcedTutorialStepType.ZoneArea) CompleteCurrentStep(); }
        public void NotifyFactoryBuilt() { if (GetCurrentStepType() == ForcedTutorialStepType.BuildFactory) CompleteCurrentStep(); }
        public void NotifyProductionStarted() { if (GetCurrentStepType() == ForcedTutorialStepType.StartProduction) CompleteCurrentStep(); }
        public void NotifyMaterialCollected() { if (GetCurrentStepType() == ForcedTutorialStepType.CollectMaterial) CompleteCurrentStep(); }
        public void NotifyBuildingUpgraded() { if (GetCurrentStepType() == ForcedTutorialStepType.UpgradeBuilding) CompleteCurrentStep(); }
        public void NotifyPopulationReached(int population)
        {
            if (GetCurrentStepType() == ForcedTutorialStepType.CheckPopulation && population >= 10)
            {
                CompleteCurrentStep();
            }
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("ForcedTutorialStep", currentStepIndex);
            PlayerPrefs.SetInt("TutorialCompleted", tutorialCompleted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            currentStepIndex = PlayerPrefs.GetInt("ForcedTutorialStep", 0);
            tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        }
    }
}
