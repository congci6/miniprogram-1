using UnityEngine;
using PocketCity.Production;
using PocketCity.Materials;
using PocketCity.Core;
using PocketCity.Disaster;
using PocketCity.Quest;
using PocketCity.Achievement;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 场景系统初始化器 - 自动创建所有必需的系统
    /// </summary>
    public class SceneSystemInitializer : MonoBehaviour
    {
        [Header("自动创建系统")]
        [SerializeField] private bool autoCreateSystems = true;

        [Header("生产系统")]
        [SerializeField] private bool createProductionSystems = true;
        [SerializeField] private MaterialDatabase materialDatabase;

        [Header("音频和特效")]
        [SerializeField] private bool createAudioAndVFX = true;

        [Header("灾难系统")]
        [SerializeField] private bool createDisasterSystem = true;

        [Header("任务和成就")]
        [SerializeField] private bool createQuestAndAchievement = true;

        [Header("UI系统")]
        [SerializeField] private bool createUISystems = true;

        private void Awake()
        {
            if (!autoCreateSystems) return;

            InitializeSystems();
        }

        private void InitializeSystems()
        {
            // 创建核心系统容器
            var systemsRoot = new GameObject("--- Game Systems ---");
            DontDestroyOnLoad(systemsRoot);

            // 1. 生产系统
            if (createProductionSystems)
            {
                CreateProductionSystems(systemsRoot.transform);
            }

            // 2. 音频和特效
            if (createAudioAndVFX)
            {
                CreateAudioAndVFX(systemsRoot.transform);
            }

            // 3. 灾难系统
            if (createDisasterSystem)
            {
                CreateDisasterSystems(systemsRoot.transform);
            }

            // 4. 任务和成就
            if (createQuestAndAchievement)
            {
                CreateQuestAndAchievement(systemsRoot.transform);
            }

            // 5. UI系统
            if (createUISystems)
            {
                CreateUISystems(systemsRoot.transform);
            }

            // 6. 对象池管理器
            CreateObjectPoolManager(systemsRoot.transform);

            Debug.Log("[SceneSystemInitializer] All systems initialized successfully");
        }

        private void CreateProductionSystems(Transform parent)
        {
            var productionRoot = new GameObject("Production Systems");
            productionRoot.transform.SetParent(parent, false);

            // StorageSystem
            var storageObj = new GameObject("StorageSystem");
            storageObj.transform.SetParent(productionRoot.transform, false);
            var storage = storageObj.AddComponent<StorageSystem>();

            // ProductionChainSystem
            var productionObj = new GameObject("ProductionChainSystem");
            productionObj.transform.SetParent(productionRoot.transform, false);
            var production = productionObj.AddComponent<ProductionChainSystem>();

            // TradeSystem
            var tradeObj = new GameObject("TradeSystem");
            tradeObj.transform.SetParent(productionRoot.transform, false);
            var trade = tradeObj.AddComponent<TradeSystem>();

            // UnifiedStorageBridge
            var bridgeObj = new GameObject("UnifiedStorageBridge");
            bridgeObj.transform.SetParent(productionRoot.transform, false);
            var bridge = bridgeObj.AddComponent<UnifiedStorageBridge>();

            // SpecializedFactorySystem
            var factoryObj = new GameObject("SpecializedFactorySystem");
            factoryObj.transform.SetParent(productionRoot.transform, false);
            var factory = factoryObj.AddComponent<SpecializedFactorySystem>();

            // UrgentOrderSystem
            var urgentObj = new GameObject("UrgentOrderSystem");
            urgentObj.transform.SetParent(productionRoot.transform, false);
            var urgent = urgentObj.AddComponent<UrgentOrderSystem>();

            Debug.Log("[SceneSystemInitializer] Production systems created");
        }

        private void CreateAudioAndVFX(Transform parent)
        {
            var audioVfxRoot = new GameObject("Audio & VFX");
            audioVfxRoot.transform.SetParent(parent, false);

            // AudioManager (如果类存在)
            var audioObj = new GameObject("AudioManager");
            audioObj.transform.SetParent(audioVfxRoot.transform, false);
            // audioObj.AddComponent<AudioManager>(); // 取消注释如果AudioManager存在

            // ParticleEffectSystem (如果类存在)
            var particleObj = new GameObject("ParticleEffectSystem");
            particleObj.transform.SetParent(audioVfxRoot.transform, false);
            // particleObj.AddComponent<ParticleEffectSystem>(); // 取消注释如果存在

            Debug.Log("[SceneSystemInitializer] Audio & VFX systems created");
        }

        private void CreateDisasterSystems(Transform parent)
        {
            var disasterRoot = new GameObject("Disaster Systems");
            disasterRoot.transform.SetParent(parent, false);

            // DisasterSystem
            var disasterObj = new GameObject("DisasterSystem");
            disasterObj.transform.SetParent(disasterRoot.transform, false);
            var disaster = disasterObj.AddComponent<DisasterSystem>();

            Debug.Log("[SceneSystemInitializer] Disaster systems created");
        }

        private void CreateQuestAndAchievement(Transform parent)
        {
            var questRoot = new GameObject("Quest & Achievement");
            questRoot.transform.SetParent(parent, false);

            // QuestSystem
            var questObj = new GameObject("QuestSystem");
            questObj.transform.SetParent(questRoot.transform, false);
            var quest = questObj.AddComponent<QuestSystem>();

            // AchievementSystem
            var achievementObj = new GameObject("AchievementSystem");
            achievementObj.transform.SetParent(questRoot.transform, false);
            var achievement = achievementObj.AddComponent<AchievementSystem>();

            Debug.Log("[SceneSystemInitializer] Quest & Achievement systems created");
        }

        private void CreateUISystems(Transform parent)
        {
            var uiRoot = new GameObject("UI Systems");
            uiRoot.transform.SetParent(parent, false);

            // NotificationSystem (如果类存在)
            var notificationObj = new GameObject("NotificationSystem");
            notificationObj.transform.SetParent(uiRoot.transform, false);
            // notificationObj.AddComponent<NotificationSystem>(); // 取消注释如果存在

            Debug.Log("[SceneSystemInitializer] UI systems created");
        }

        private void CreateObjectPoolManager(Transform parent)
        {
            // 检查是否已存在
            if (ObjectPoolManager.Instance != null) return;

            var poolObj = new GameObject("ObjectPoolManager");
            poolObj.transform.SetParent(parent, false);
            poolObj.AddComponent<ObjectPoolManager>();

            Debug.Log("[SceneSystemInitializer] ObjectPoolManager created");
        }

        // 编辑器辅助方法
        [ContextMenu("Force Initialize Systems")]
        public void ForceInitialize()
        {
            InitializeSystems();
        }

        [ContextMenu("Check Missing Systems")]
        public void CheckMissingSystems()
        {
            Debug.Log("=== Checking Missing Systems ===");

            CheckSystem<StorageSystem>("StorageSystem");
            CheckSystem<ProductionChainSystem>("ProductionChainSystem");
            CheckSystem<TradeSystem>("TradeSystem");
            CheckSystem<UnifiedStorageBridge>("UnifiedStorageBridge");
            CheckSystem<ObjectPoolManager>("ObjectPoolManager");
            CheckSystem<DisasterSystem>("DisasterSystem");
            CheckSystem<QuestSystem>("QuestSystem");
            CheckSystem<AchievementSystem>("AchievementSystem");

            Debug.Log("=== Check Complete ===");
        }

        private void CheckSystem<T>(string name) where T : MonoBehaviour
        {
            var instance = FindAnyObjectByType<T>();
            if (instance == null)
            {
                Debug.LogWarning($"[Missing] {name}");
            }
            else
            {
                Debug.Log($"[Found] {name}");
            }
        }
    }
}
