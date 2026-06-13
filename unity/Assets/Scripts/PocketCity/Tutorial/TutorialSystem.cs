using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Tutorial
{
    public enum TutorialStepType
    {
        Welcome,
        BuildRoad,
        PlaceZone,
        WaitForGrowth,
        CollectTax,
        BuildService,
        Complete
    }

    [System.Serializable]
    public class TutorialStepTypeData
    {
        public TutorialStepType step;
        public string title;
        public string description;
        public string highlightUIElement; // UI元素ID用于高亮
        public bool blockInput = true;
    }

    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        [SerializeField] private List<TutorialStepTypeData> steps = new List<TutorialStepTypeData>();
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private UnityEngine.UI.Text titleText;
        [SerializeField] private UnityEngine.UI.Text descriptionText;

        private TutorialStepType currentStep = TutorialStepType.Welcome;
        private bool tutorialCompleted = false;

        public event System.Action<TutorialStepType> OnStepChanged;
        public event System.Action OnTutorialCompleted;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            LoadProgress();
        }

        private void Start()
        {
            if (!tutorialCompleted)
            {
                ShowCurrentStep();
            }
        }

        public bool IsTutorialActive => !tutorialCompleted && currentStep != TutorialStepType.Complete;

        public void StartTutorial()
        {
            currentStep = TutorialStepType.Welcome;
            tutorialCompleted = false;
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            var stepData = steps.Find(s => s.step == currentStep);
            if (stepData == null || tutorialPanel == null) return;

            tutorialPanel.SetActive(true);
            if (titleText != null) titleText.text = stepData.title;
            if (descriptionText != null) descriptionText.text = stepData.description;

            OnStepChanged?.Invoke(currentStep);

            // TODO: 高亮UI元素
        }

        public void CompleteCurrentStep()
        {
            currentStep = (TutorialStepType)((int)currentStep + 1);

            if (currentStep == TutorialStepType.Complete)
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
            tutorialCompleted = true;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);

            OnTutorialCompleted?.Invoke();
            SaveProgress();

            // 奖励：100高级货币
            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddPremium(100);
            }
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("TutorialStepType", (int)currentStep);
            PlayerPrefs.SetInt("TutorialCompleted", tutorialCompleted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            currentStep = (TutorialStepType)PlayerPrefs.GetInt("TutorialStepType", 0);
            tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        }

        // 供外部调用检测步骤完成
        public void NotifyRoadBuilt() { if (currentStep == TutorialStepType.BuildRoad) CompleteCurrentStep(); }
        public void NotifyZonePlaced() { if (currentStep == TutorialStepType.PlaceZone) CompleteCurrentStep(); }
        public void NotifyBuildingGrown() { if (currentStep == TutorialStepType.WaitForGrowth) CompleteCurrentStep(); }
        public void NotifyTaxCollected() { if (currentStep == TutorialStepType.CollectTax) CompleteCurrentStep(); }
        public void NotifyServiceBuilt() { if (currentStep == TutorialStepType.BuildService) CompleteCurrentStep(); }
    }
}
