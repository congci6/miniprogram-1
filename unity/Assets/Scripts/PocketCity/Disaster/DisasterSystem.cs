using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Disaster
{
    public enum DisasterType
    {
        Earthquake,
        Tornado,
        Meteor,
        Fire,
        Alien,
        Robot,
        Monster
    }

    [Serializable]
    public class DisasterConfig
    {
        public DisasterType type;
        public int level; // 1-6 stars
        public float radius;
        public int damage;
        public float duration;
    }

    public class DisasterSystem : MonoBehaviour
    {
        public static DisasterSystem Instance { get; private set; }

        [SerializeField] private List<DisasterConfig> disasterConfigs;
        [SerializeField] private float randomDisasterInterval = 300f;
        [SerializeField] private bool enableRandomDisasters = true;

        private Dictionary<(DisasterType, int), DisasterConfig> configLookup;
        private DisasterEffects effectsSystem;

        public event Action<DisasterType, int, Vector3> OnDisasterTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            effectsSystem = GetComponent<DisasterEffects>();
            InitializeConfigs();
        }

        private void Start()
        {
            if (enableRandomDisasters)
            {
                InvokeRepeating(nameof(TriggerRandomDisaster), randomDisasterInterval, randomDisasterInterval);
            }
        }

        private void InitializeConfigs()
        {
            if (disasterConfigs == null || disasterConfigs.Count == 0)
            {
                disasterConfigs = new List<DisasterConfig>();
                foreach (DisasterType type in Enum.GetValues(typeof(DisasterType)))
                {
                    for (int level = 1; level <= 6; level++)
                    {
                        disasterConfigs.Add(new DisasterConfig
                        {
                            type = type,
                            level = level,
                            radius = 10f + level * 5f,
                            damage = level * 20,
                            duration = 5f + level * 2f
                        });
                    }
                }
            }

            // 构建O(1)查找表
            configLookup = new Dictionary<(DisasterType, int), DisasterConfig>();
            foreach (var config in disasterConfigs)
            {
                configLookup[(config.type, config.level)] = config;
            }
        }

        public void TriggerDisaster(DisasterType type, int level, Vector3 position)
        {
            level = Mathf.Clamp(level, 1, 6);
            DisasterConfig config = GetConfig(type, level);

            if (config != null)
            {
                OnDisasterTriggered?.Invoke(type, level, position);
                effectsSystem?.ExecuteDisaster(config, position);
            }
        }

        public void TriggerRandomDisaster()
        {
            DisasterType randomType = (DisasterType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(DisasterType)).Length);
            int randomLevel = UnityEngine.Random.Range(1, 7);
            Vector3 randomPos = new Vector3(
                UnityEngine.Random.Range(-50f, 50f),
                0f,
                UnityEngine.Random.Range(-50f, 50f)
            );
            TriggerDisaster(randomType, randomLevel, randomPos);
        }

        private DisasterConfig GetConfig(DisasterType type, int level)
        {
            configLookup.TryGetValue((type, level), out var config);
            return config;
        }

        public void SetRandomDisastersEnabled(bool enabled)
        {
            enableRandomDisasters = enabled;
            if (enabled)
            {
                InvokeRepeating(nameof(TriggerRandomDisaster), randomDisasterInterval, randomDisasterInterval);
            }
            else
            {
                CancelInvoke(nameof(TriggerRandomDisaster));
            }
        }
    }
}
