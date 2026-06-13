using UnityEngine;
using PocketCity.Tutorial;
using PocketCity.Integration;
using PocketCity.Audio;
using PocketCity.Runtime;

namespace PocketCity.Bootstrap
{
    /// <summary>
    /// 游戏系统自动挂载器 - 解决F-1到F-4未挂载问题
    /// 在游戏启动时自动创建和初始化所有必需系统
    /// </summary>
    public class GameSystemBootstrap : MonoBehaviour
    {
        [Header("Auto Initialize")]
        [SerializeField] private bool initializeOnAwake = true;

        [Header("System References")]
        public ForcedTutorialSystem tutorialSystem;
        public ProductionCityBridge productionBridge;
        public AudioManager audioManager;
        public BuildingBatcher buildingBatcher;

        private static GameSystemBootstrap instance;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (initializeOnAwake)
            {
                InitializeAllSystems();
            }
        }

        /// <summary>
        /// 初始化所有系统（F-1到F-4）
        /// </summary>
        public void InitializeAllSystems()
        {
            Debug.Log("🚀 开始初始化游戏系统...");

            // F-1: 强制教程系统
            InitializeTutorialSystem();

            // F-2: 生产城市桥接
            InitializeProductionBridge();

            // F-3: 音频管理器
            InitializeAudioManager();

            // F-4: 建筑批处理器
            InitializeBuildingBatcher();

            Debug.Log("✅ 所有系统初始化完成！");
        }

        private void InitializeTutorialSystem()
        {
            if (tutorialSystem == null)
            {
                tutorialSystem = gameObject.GetComponent<ForcedTutorialSystem>();
                if (tutorialSystem == null)
                {
                    tutorialSystem = gameObject.AddComponent<ForcedTutorialSystem>();
                }
            }

            // 检查是否需要启动教程
            bool tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
            if (!tutorialCompleted)
            {
                Debug.Log("📚 启动强制新手教程");
                tutorialSystem.enabled = true;
            }
            else
            {
                Debug.Log("✅ 教程已完成，跳过");
                tutorialSystem.enabled = false;
            }
        }

        private void InitializeProductionBridge()
        {
            if (productionBridge == null)
            {
                productionBridge = gameObject.GetComponent<ProductionCityBridge>();
                if (productionBridge == null)
                {
                    productionBridge = gameObject.AddComponent<ProductionCityBridge>();
                }
            }

            Debug.Log("✅ ProductionCityBridge 已初始化");
        }

        private void InitializeAudioManager()
        {
            if (audioManager == null)
            {
                audioManager = FindAnyObjectByType<AudioManager>();
                if (audioManager == null)
                {
                    GameObject audioObj = new GameObject("AudioManager");
                    audioObj.transform.SetParent(transform);
                    audioManager = audioObj.AddComponent<AudioManager>();
                }
            }

            Debug.Log("✅ AudioManager 已初始化");
        }

        private void InitializeBuildingBatcher()
        {
            if (buildingBatcher == null)
            {
                buildingBatcher = gameObject.AddComponent<BuildingBatcher>();
            }

            Debug.Log("✅ BuildingBatcher 已初始化");
        }

        /// <summary>
        /// 获取系统实例（外部访问）
        /// </summary>
        public static ForcedTutorialSystem GetTutorialSystem()
        {
            return instance?.tutorialSystem;
        }

        public static ProductionCityBridge GetProductionBridge()
        {
            return instance?.productionBridge;
        }

        public static AudioManager GetAudioManager()
        {
            return instance?.audioManager;
        }

        public static BuildingBatcher GetBuildingBatcher()
        {
            return instance?.buildingBatcher;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
