using UnityEngine;
using System.Collections.Generic;
using PocketCity.Disaster;
using PocketCity.Simulation;
using PocketCity.Runtime;

namespace PocketCity.Persistence
{
    /// <summary>
    /// 建筑损坏持久化系统 - 解决F-16建筑破坏持久化
    /// </summary>
    [System.Serializable]
    public class BuildingDamageData
    {
        public string buildingId;
        public int durability;
        public bool isDestroyed;
    }

    [System.Serializable]
    public class DamagePersistenceData
    {
        public List<BuildingDamageData> damages = new List<BuildingDamageData>();
    }

    public class BuildingDamagePersistence : MonoBehaviour
    {
        public static BuildingDamagePersistence Instance { get; private set; }

        [SerializeField] private DamageSystem damageSystem;
        [SerializeField] private CitySimulationCore simulation;

        private const string SAVE_KEY = "BuildingDamageData";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (damageSystem == null)
                damageSystem = FindAnyObjectByType<DamageSystem>();

            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }

            // 订阅保存事件
            if (CitySaveController.Instance != null)
            {
                // 假设CitySaveController有保存事件
            }
        }

        /// <summary>
        /// 保存建筑损坏数据
        /// </summary>
        public void SaveDamageData()
        {
            if (damageSystem == null || simulation == null)
                return;

            var data = new DamagePersistenceData();

            foreach (var building in simulation.Buildings)
            {
                var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                if (damage != null && damage.durability < 100)
                {
                    data.damages.Add(new BuildingDamageData
                    {
                        buildingId = building.Id,
                        durability = (int)damage.durability,
                        isDestroyed = damage.isDestroyed
                    });
                }
            }

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            Debug.Log($"保存了 {data.damages.Count} 个建筑的损坏数据");
        }

        /// <summary>
        /// 加载建筑损坏数据
        /// </summary>
        public void LoadDamageData()
        {
            if (damageSystem == null || simulation == null)
                return;

            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                Debug.Log("无建筑损坏数据");
                return;
            }

            string json = PlayerPrefs.GetString(SAVE_KEY);
            var data = JsonUtility.FromJson<DamagePersistenceData>(json);

            if (data == null || data.damages == null)
                return;

            int loadedCount = 0;

            foreach (var damageData in data.damages)
            {
                var building = simulation.FindPlacedBuilding(damageData.buildingId);
                if (building != null)
                {
                    // 恢复损坏状态
                    var damage = damageSystem.GetBuildingDamage(int.Parse(building.Id));
                    if (damage == null)
                    {
                        damage = new BuildingDamage
                        {
                            buildingId = int.Parse(building.Id),
                            durability = damageData.durability,
                            isDestroyed = damageData.isDestroyed
                        };

                        // 添加到DamageSystem（假设有AddDamage方法）
                        // damageSystem.AddDamage(damage);
                    }
                    else
                    {
                        damage.durability = damageData.durability;
                        damage.isDestroyed = damageData.isDestroyed;
                    }

                    loadedCount++;
                }
            }

            Debug.Log($"加载了 {loadedCount} 个建筑的损坏数据");
        }

        /// <summary>
        /// 清除所有损坏数据
        /// </summary>
        public void ClearDamageData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("清除了所有建筑损坏数据");
        }

        /// <summary>
        /// 自动保存（每分钟）
        /// </summary>
        private void Update()
        {
            if (Time.frameCount % (60 * 60) == 0) // 每60秒
            {
                SaveDamageData();
            }
        }

        private void OnApplicationQuit()
        {
            SaveDamageData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveDamageData();
            }
        }
    }
}
