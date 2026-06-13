using UnityEngine;
using UnityEngine.SceneManagement;
using PocketCity.Production;
using PocketCity.Materials;
using PocketCity.Core;
using PocketCity.Disaster;
using PocketCity.Quest;
using PocketCity.Achievement;
using PocketCity.Integration;
using System.Collections;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 游戏启动管理器 - 确保所有系统正确初始化，防止NullReferenceException
    /// 使用RuntimeInitializeOnLoadMethod自动启动，无需手动添加到场景
    /// </summary>
    [DefaultExecutionOrder(-100)] // 最先执行
    public class GameBootstrap : MonoBehaviour
    {
        [Header("自动修复缺失系统")]
        [SerializeField] private bool autoFixMissingSystems = true;

        [Header("必需的ScriptableObject资源")]
        [SerializeField] private MaterialDatabase materialDatabase;
        [SerializeField] private CityConfig cityConfig;

        [Header("调试")]
        [SerializeField] private bool enableDebugLogs = true;

        // 单例实例
        public static GameBootstrap Instance { get; private set; }

        // 系统引用（自动查找或创建）
        public StorageSystem Storage { get; private set; }
        public ProductionChainSystem Production { get; private set; }
        public TradeSystem Trade { get; private set; }
        public UnifiedStorageBridge StorageBridge { get; private set; }
        public ProductionCityBridge CityBridge { get; private set; }
        public DisasterSystem Disaster { get; private set; }
        public QuestSystem Quest { get; private set; }
        public AchievementSystem Achievement { get; private set; }
        public ObjectPoolManager ObjectPool { get; private set; }
        public Buildings.BuildingTraitSystem BuildingTrait { get; private set; }
        public SpecializedFactorySystem SpecializedFactory { get; private set; }
        public UrgentOrderSystem UrgentOrder { get; private set; }
        public Audio.AudioManager AudioMgr { get; private set; }
        public VFX.ParticleEffectSystem ParticleEffects { get; private set; }

        private bool isInitialized = false;
        private static bool autoCreated = false;

        /// <summary>
        /// 运行时自动初始化 - 无需手动添加到场景
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (autoCreated)
                return;

            autoCreated = true;

            Debug.Log("🚀 [GameBootstrap] 自动创建并初始化...");

            // 创建GameBootstrap GameObject
            GameObject bootstrapObj = new GameObject("__GameBootstrap__");
            DontDestroyOnLoad(bootstrapObj);

            var bootstrap = bootstrapObj.AddComponent<GameBootstrap>();
            bootstrap.autoFixMissingSystems = true;
            bootstrap.enableDebugLogs = true;

            Debug.Log("✅ [GameBootstrap] 自动创建完成");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Log("=== Game Bootstrap Starting ===");

            if (autoFixMissingSystems)
            {
                StartCoroutine(InitializeAllSystems());
            }
        }

        private IEnumerator InitializeAllSystems()
        {
            yield return null; // 等待一帧，确保场景完全加载

            // 1. 创建系统容器
            CreateSystemsContainer();

            // 2. 初始化核心生产系统
            InitializeProductionSystems();
            yield return null;

            // 3. 初始化材料和存储桥接
            InitializeStorageBridge();
            yield return null;

            // 4. 初始化城市生产桥接
            InitializeCityBridge();
            yield return null;

            // 5. 初始化灾难系统
            InitializeDisasterSystem();
            yield return null;

            // 6. 初始化任务和成就
            InitializeQuestAndAchievement();
            yield return null;

            // 7. 初始化对象池
            InitializeObjectPool();
            yield return null;

            // 8. 初始化BuildingTraitSystem
            InitializeBuildingTrait();
            yield return null;

            // 9. 初始化高级生产系统
            InitializeAdvancedProduction();
            yield return null;

            // 10. 初始化UI系统
            InitializeUISystems();
            yield return null;

            // 11. 验证所有系统
            ValidateAllSystems();

            isInitialized = true;
            Log("=== Game Bootstrap Complete ===");
        }

        private void CreateSystemsContainer()
        {
            var existing = GameObject.Find("--- Game Systems ---");
            if (existing == null)
            {
                var container = new GameObject("--- Game Systems ---");
                DontDestroyOnLoad(container);
                Log("Created systems container");
            }
        }

        private void InitializeProductionSystems()
        {
            Log("Initializing Production Systems...");

            // StorageSystem
            Storage = FindOrCreateSystem<StorageSystem>("StorageSystem");

            // ProductionChainSystem
            Production = FindOrCreateSystem<ProductionChainSystem>("ProductionChainSystem");
            if (Production != null && materialDatabase != null)
            {
                // 通过反射设置materialDB（如果没有公共setter）
                var field = typeof(ProductionChainSystem).GetField("materialDB",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(Production, materialDatabase);
                    Log("Set MaterialDatabase for ProductionChainSystem");
                }
            }

            // TradeSystem
            Trade = FindOrCreateSystem<TradeSystem>("TradeSystem");
            if (Trade != null && materialDatabase != null)
            {
                var field = typeof(TradeSystem).GetField("materialDB",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(Trade, materialDatabase);
                    Log("Set MaterialDatabase for TradeSystem");
                }
            }
        }

        private void InitializeStorageBridge()
        {
            Log("Initializing Storage Bridge...");
            StorageBridge = FindOrCreateSystem<UnifiedStorageBridge>("UnifiedStorageBridge");

            if (StorageBridge != null && Storage != null && materialDatabase != null)
            {
                // 设置引用
                var storageField = typeof(UnifiedStorageBridge).GetField("storageSystem",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (storageField != null)
                {
                    storageField.SetValue(StorageBridge, Storage);
                }

                var dbField = typeof(UnifiedStorageBridge).GetField("materialDatabase",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbField != null)
                {
                    dbField.SetValue(StorageBridge, materialDatabase);
                }

                Log("Configured UnifiedStorageBridge");
            }
        }

        private void InitializeCityBridge()
        {
            Log("Initializing City Bridge...");
            CityBridge = FindOrCreateSystem<ProductionCityBridge>("ProductionCityBridge");

            if (CityBridge != null)
            {
                // 查找CitySimulationCore
                var controller = FindObjectOfType<CityGameController>();
                var simulation = controller != null ? controller.Simulation : null;

                if (Production != null && Storage != null && simulation != null)
                {
                    var simField = typeof(ProductionCityBridge).GetField("simulation",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (simField != null)
                    {
                        simField.SetValue(CityBridge, simulation);
                    }

                    var prodField = typeof(ProductionCityBridge).GetField("production",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (prodField != null)
                    {
                        prodField.SetValue(CityBridge, Production);
                    }

                    var storageField = typeof(ProductionCityBridge).GetField("storage",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (storageField != null)
                    {
                        storageField.SetValue(CityBridge, Storage);
                    }

                    Log("Configured ProductionCityBridge");
                }
                else
                {
                    LogWarning("Missing dependencies for ProductionCityBridge");
                }
            }
        }

        private void InitializeDisasterSystem()
        {
            Log("Initializing Disaster System...");
            Disaster = FindOrCreateSystem<DisasterSystem>("DisasterSystem");
        }

        private void InitializeQuestAndAchievement()
        {
            Log("Initializing Quest & Achievement...");
            Quest = FindOrCreateSystem<QuestSystem>("QuestSystem");
            Achievement = FindOrCreateSystem<AchievementSystem>("AchievementSystem");
        }

        private void InitializeObjectPool()
        {
            Log("Initializing Object Pool...");
            ObjectPool = FindOrCreateSystem<ObjectPoolManager>("ObjectPoolManager");
        }

        private void InitializeBuildingTrait()
        {
            Log("Initializing BuildingTrait System...");
            BuildingTrait = FindOrCreateSystem<Buildings.BuildingTraitSystem>("BuildingTraitSystem");

            if (BuildingTrait != null)
            {
                // 查找CitySimulationCore
                var controller = FindObjectOfType<CityGameController>();
                var simulation = controller != null ? controller.Simulation : null;
                if (simulation != null)
                {
                    var simField = typeof(Buildings.BuildingTraitSystem).GetField("simulation",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (simField != null)
                    {
                        simField.SetValue(BuildingTrait, simulation);
                        Log("Configured BuildingTraitSystem");
                    }
                }
            }
        }

        private void InitializeAdvancedProduction()
        {
            Log("Initializing Advanced Production Systems...");

            // SpecializedFactorySystem
            SpecializedFactory = FindOrCreateSystem<SpecializedFactorySystem>("SpecializedFactorySystem");
            if (SpecializedFactory != null && materialDatabase != null && Storage != null)
            {
                var dbField = typeof(SpecializedFactorySystem).GetField("materialDB",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbField != null)
                {
                    dbField.SetValue(SpecializedFactory, materialDatabase);
                }

                var storageField = typeof(SpecializedFactorySystem).GetField("storage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (storageField != null)
                {
                    storageField.SetValue(SpecializedFactory, Storage);
                }

                Log("Configured SpecializedFactorySystem");
            }

            // UrgentOrderSystem
            UrgentOrder = FindOrCreateSystem<UrgentOrderSystem>("UrgentOrderSystem");
            if (UrgentOrder != null && materialDatabase != null && Storage != null)
            {
                var dbField = typeof(UrgentOrderSystem).GetField("materialDB",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbField != null)
                {
                    dbField.SetValue(UrgentOrder, materialDatabase);
                }

                var storageField = typeof(UrgentOrderSystem).GetField("storage",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (storageField != null)
                {
                    storageField.SetValue(UrgentOrder, Storage);
                }

                Log("Configured UrgentOrderSystem");
            }
        }

        private void InitializeUISystems()
        {
            Log("Initializing UI Systems...");

            // AudioManager
            AudioMgr = FindOrCreateSystem<Audio.AudioManager>("AudioManager");

            // ParticleEffectSystem
            ParticleEffects = FindOrCreateSystem<VFX.ParticleEffectSystem>("ParticleEffectSystem");

            // MinimapSystem等UI系统需要Canvas引用，暂时跳过自动创建
            // 由UI Prefab或场景手动设置
        }

        private T FindOrCreateSystem<T>(string objectName) where T : MonoBehaviour
        {
            // 先查找现有实例
            var existing = FindAnyObjectByType<T>();
            if (existing != null)
            {
                Log($"Found existing {typeof(T).Name}");
                return existing;
            }

            // 创建新实例
            var systemsContainer = GameObject.Find("--- Game Systems ---");
            if (systemsContainer == null)
            {
                systemsContainer = new GameObject("--- Game Systems ---");
                DontDestroyOnLoad(systemsContainer);
            }

            var obj = new GameObject(objectName);
            obj.transform.SetParent(systemsContainer.transform, false);
            var component = obj.AddComponent<T>();

            Log($"Created {typeof(T).Name}");
            return component;
        }

        private void ValidateAllSystems()
        {
            Log("=== Validating All Systems ===");

            ValidateSystem(Storage, "StorageSystem");
            ValidateSystem(Production, "ProductionChainSystem");
            ValidateSystem(Trade, "TradeSystem");
            ValidateSystem(StorageBridge, "UnifiedStorageBridge");
            ValidateSystem(CityBridge, "ProductionCityBridge");
            ValidateSystem(Disaster, "DisasterSystem");
            ValidateSystem(Quest, "QuestSystem");
            ValidateSystem(Achievement, "AchievementSystem");
            ValidateSystem(ObjectPool, "ObjectPoolManager");
            ValidateSystem(BuildingTrait, "BuildingTraitSystem");
            ValidateSystem(SpecializedFactory, "SpecializedFactorySystem");
            ValidateSystem(UrgentOrder, "UrgentOrderSystem");
            ValidateSystem(AudioMgr, "AudioManager");
            ValidateSystem(ParticleEffects, "ParticleEffectSystem");

            if (materialDatabase == null)
            {
                LogError("MaterialDatabase is NULL! Create via: Assets -> Create -> PocketCity -> Material Database");
            }
            else
            {
                Log($"[OK] MaterialDatabase with {materialDatabase.materials.Count} materials");
            }

            Log("=== Validation Complete ===");
        }

        private void ValidateSystem(MonoBehaviour system, string name)
        {
            if (system == null)
            {
                LogWarning($"[MISSING] {name}");
            }
            else
            {
                Log($"[OK] {name}");
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GameBootstrap] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[GameBootstrap] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[GameBootstrap] {message}");
        }

        // 公共API用于其他系统访问
        public static T GetSystem<T>() where T : MonoBehaviour
        {
            if (Instance == null)
            {
                Debug.LogError("[GameBootstrap] Instance is null!");
                return null;
            }

            if (typeof(T) == typeof(StorageSystem)) return Instance.Storage as T;
            if (typeof(T) == typeof(ProductionChainSystem)) return Instance.Production as T;
            if (typeof(T) == typeof(TradeSystem)) return Instance.Trade as T;
            if (typeof(T) == typeof(UnifiedStorageBridge)) return Instance.StorageBridge as T;
            if (typeof(T) == typeof(ProductionCityBridge)) return Instance.CityBridge as T;
            if (typeof(T) == typeof(DisasterSystem)) return Instance.Disaster as T;
            if (typeof(T) == typeof(QuestSystem)) return Instance.Quest as T;
            if (typeof(T) == typeof(AchievementSystem)) return Instance.Achievement as T;
            if (typeof(T) == typeof(ObjectPoolManager)) return Instance.ObjectPool as T;
            if (typeof(T) == typeof(Buildings.BuildingTraitSystem)) return Instance.BuildingTrait as T;
            if (typeof(T) == typeof(SpecializedFactorySystem)) return Instance.SpecializedFactory as T;
            if (typeof(T) == typeof(UrgentOrderSystem)) return Instance.UrgentOrder as T;
            if (typeof(T) == typeof(Audio.AudioManager)) return Instance.AudioMgr as T;
            if (typeof(T) == typeof(VFX.ParticleEffectSystem)) return Instance.ParticleEffects as T;

            return null;
        }

        public bool IsFullyInitialized()
        {
            return isInitialized &&
                   Storage != null &&
                   Production != null &&
                   CityBridge != null &&
                   BuildingTrait != null &&
                   AudioMgr != null;
        }
    }
}
