using UnityEngine;
using PocketCity.Production;
using PocketCity.Disaster;
using PocketCity.Quest;
using PocketCity.Achievement;
using PocketCity.Materials;
using PocketCity.Tutorial;
using PocketCity.Integration;
using PocketCity.Audio;
using PocketCity.Runtime;
using PocketCity.Buildings;
using PocketCity.UI;
using PocketCity.VFX;
using PocketCity.Core;
using PocketCity.Bootstrap;
using PocketCity.Economy;
using PocketCity.Placement;
using PocketCity.Persistence;
using PocketCity.Settings;
using PocketCity.Input;
using PocketCity.Notifications;
using PocketCity.Visual;
using PocketCity.Achievements;
using PocketCity.Trade;
using PocketCity.Services;
using PocketCity.Transportation;
using PocketCity.CitySpecialization;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 场景自动初始化器 - 确保所有系统在场景启动时被创建
    /// 解决"代码存在但未接入场景"的问题
    /// 使用RuntimeInitializeOnLoadMethod自动执行，无需手动添加到场景
    /// </summary>
    [DefaultExecutionOrder(-1000)] // 最早执行
    public class MasterSceneInitializer : MonoBehaviour
    {
        [Header("Auto Initialize All Systems")]
        [SerializeField] private bool autoInitialize = true;

        private static bool initialized = false;

        /// <summary>
        /// 运行时自动初始化（无需手动添加到场景）
        /// 已禁用 - 使用GameBootstrap代替
        /// </summary>
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // 已禁用 - GameBootstrap会处理所有初始化
            // 保留此方法以备手动调用

            if (initialized)
                return;

            initialized = true;

            Debug.Log("🚀 [MasterSceneInitializer] 手动初始化...");

            // 创建主初始化器GameObject
            GameObject initializerObj = new GameObject("__MasterSceneInitializer__");
            DontDestroyOnLoad(initializerObj);

            var initializer = initializerObj.AddComponent<MasterSceneInitializer>();
            initializer.InitializeAllSystems();

            Debug.Log("✅ [MasterSceneInitializer] 初始化完成");
        }

        private void Awake()
        {
            // RuntimeInitializeOnLoadMethod已自动初始化，这里不再重复
            if (!autoInitialize)
                return;
        }

        private void InitializeAllSystems()
        {
            // 核心系统
            InitializeCoreBootstrap();

            // 生产系统
            InitializeProductionSystems();

            // 交易和材料
            InitializeTradeAndMaterials();

            // 灾难和恢复
            InitializeDisasterSystems();

            // 任务和成就
            InitializeQuestAndAchievement();

            // UI系统
            InitializeUISystems();

            // 音频和特效
            InitializeAudioAndVFX();

            // 辅助系统
            InitializeUtilitySystems();

            // 集成和管理器
            InitializeIntegrationSystems();

            // 输入和设置
            InitializeInputAndSettings();

            // 专精和服务
            InitializeSpecializationAndServices();
        }

        private void InitializeCoreBootstrap()
        {
            FindOrCreate<GameSystemBootstrap>("GameSystemBootstrap");
            FindOrCreate<ObjectPoolManager>("ObjectPoolManager");
        }

        private void InitializeProductionSystems()
        {
            FindOrCreate<ProductionChainSystem>("ProductionChainSystem");
            FindOrCreate<StorageSystem>("StorageSystem");
            FindOrCreate<SpecializedFactorySystem>("SpecializedFactorySystem");
            FindOrCreate<FactoryUpgradeSystem>("FactoryUpgradeSystem");
        }

        private void InitializeTradeAndMaterials()
        {
            FindOrCreate<TradeSystem>("TradeSystem");
            FindOrCreate<DanielCargoSystem>("DanielCargoSystem");
            FindOrCreate<UrgentOrderSystem>("UrgentOrderSystem");
            FindOrCreate<UpgradeMaterialSystem>("UpgradeMaterialSystem");
            FindOrCreate<UnifiedStorageBridge>("UnifiedStorageBridge");
            FindOrCreate<SmartCargoOrderGenerator>("SmartCargoOrderGenerator");
        }

        private void InitializeDisasterSystems()
        {
            FindOrCreate<DisasterSystem>("DisasterSystem");
            FindOrCreate<DifferentiatedDisasterSystem>("DifferentiatedDisasterSystem");
            FindOrCreate<DamageSystem>("DamageSystem");
            FindOrCreate<DisasterRewardSystem>("DisasterRewardSystem");
            FindOrCreate<DebrisCleanupSystem>("DebrisCleanupSystem");
            FindOrCreate<DisasterRecoverySystem>("DisasterRecoverySystem");
        }

        private void InitializeQuestAndAchievement()
        {
            FindOrCreate<QuestSystem>("QuestSystem");
            FindOrCreate<AchievementSystem>("AchievementSystem");
            FindOrCreate<ExtendedAchievementSystem>("ExtendedAchievementSystem");
        }

        private void InitializeUISystems()
        {
            // UI需要Canvas，尝试查找或创建
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("⚠️ 场景中无Canvas，部分UI系统可能无法工作");
            }

            FindOrCreate<BuildingUpgradePanel>("BuildingUpgradePanel");
            FindOrCreate<BuildingCollectionUI>("BuildingCollectionUI");
            FindOrCreate<ProductionTimerUI>("ProductionTimerUI");
            FindOrCreate<UIButtonSizeValidator>("UIButtonSizeValidator");
            FindOrCreate<MinimapSystem>("MinimapSystem");
        }

        private void InitializeAudioAndVFX()
        {
            FindOrCreate<AudioManager>("AudioManager");
            FindOrCreate<ParticleEffectSystem>("ParticleEffectSystem");
            FindOrCreate<ConstructionAnimation>("ConstructionAnimation");
        }

        private void InitializeUtilitySystems()
        {
            FindOrCreate<ForcedTutorialSystem>("ForcedTutorialSystem");
            FindOrCreate<BuildingTraitSystem>("BuildingTraitSystem");
            FindOrCreate<BuildingBatcher>("BuildingBatcher");
            FindOrCreate<NotificationSystem>("NotificationSystem");
        }

        private void InitializeIntegrationSystems()
        {
            FindOrCreate<ProductionCityBridge>("ProductionCityBridge");
            FindOrCreate<UnifiedCurrencyManager>("UnifiedCurrencyManager");
            FindOrCreate<UnifiedBuildingPlacement>("UnifiedBuildingPlacement");
            FindOrCreate<UnifiedUpgradeManager>("UnifiedUpgradeManager");
            FindOrCreate<FunctionalityActivator>("FunctionalityActivator");
            FindOrCreate<LongPressIntegration>("LongPressIntegration");
            FindOrCreate<BuildingDamagePersistence>("BuildingDamagePersistence");
        }

        private void InitializeInputAndSettings()
        {
            FindOrCreate<ImprovedTouchRecognition>("ImprovedTouchRecognition");
            FindOrCreate<LongPressOperationSystem>("LongPressOperationSystem");
            FindOrCreate<PinchSensitivitySettings>("PinchSensitivitySettings");
        }

        private void InitializeSpecializationAndServices()
        {
            FindOrCreate<CitySpecializationSystem>("CitySpecializationSystem");
            FindOrCreate<ServiceCoverageVisualization>("ServiceCoverageVisualization");
            FindOrCreate<RoadBasedServiceCoverage>("RoadBasedServiceCoverage");
            FindOrCreate<PublicTransportSystem>("PublicTransportSystem");
            FindOrCreate<DayNightCycleSystem>("DayNightCycleSystem");
        }

        private T FindOrCreate<T>(string name) where T : MonoBehaviour
        {
            // 先查找现有
            T existing = FindAnyObjectByType<T>();
            if (existing != null)
            {
                Debug.Log($"✓ 已存在: {name}");
                return existing;
            }

            // 创建新对象
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            T component = obj.AddComponent<T>();

            Debug.Log($"✅ 已创建: {name}");
            return component;
        }

        /// <summary>
        /// 验证所有系统是否正常工作
        /// </summary>
        [ContextMenu("验证所有系统")]
        public void ValidateAllSystems()
        {
            int totalSystems = 0;
            int activeSystems = 0;

            // 生产系统
            if (ProductionChainSystem.Instance != null) activeSystems++;
            totalSystems++;

            if (StorageSystem.Instance != null) activeSystems++;
            totalSystems++;

            if (TradeSystem.Instance != null) activeSystems++;
            totalSystems++;

            // 灾难系统
            if (DisasterSystem.Instance != null || FindAnyObjectByType<DisasterSystem>() != null) activeSystems++;
            totalSystems++;

            if (DisasterRecoverySystem.Instance != null) activeSystems++;
            totalSystems++;

            // 任务成就
            if (QuestSystem.Instance != null) activeSystems++;
            totalSystems++;

            if (ExtendedAchievementSystem.Instance != null) activeSystems++;
            totalSystems++;

            // 音频
            if (AudioManager.Instance != null) activeSystems++;
            totalSystems++;

            // 集成系统
            if (UnifiedCurrencyManager.Instance != null) activeSystems++;
            totalSystems++;

            if (UnifiedBuildingPlacement.Instance != null) activeSystems++;
            totalSystems++;

            if (UnifiedUpgradeManager.Instance != null) activeSystems++;
            totalSystems++;

            if (FunctionalityActivator.Instance != null) activeSystems++;
            totalSystems++;

            Debug.Log($"系统验证完成：{activeSystems}/{totalSystems} 系统已激活");

            if (activeSystems == totalSystems)
            {
                Debug.Log("🎉 所有系统正常工作！");
            }
            else
            {
                Debug.LogWarning($"⚠️ {totalSystems - activeSystems} 个系统未激活！");
            }
        }
    }
}
