using UnityEngine;
using System.Collections.Generic;

namespace PocketCity.VFX
{
    public class ParticleEffectSystem : MonoBehaviour
    {
        public static ParticleEffectSystem Instance { get; private set; }

        // 简化版：不使用UnityEngine.ParticleSystem，避免模块依赖
        // 使用GameObject池来管理特效

        private Dictionary<string, GameObject> effectPrefabs = new Dictionary<string, GameObject>();
        private List<GameObject> activeEffects = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlayEffect(EffectType effectType, Vector3 position)
        {
            Debug.Log($"[ParticleEffectSystem] PlayEffect: {effectType} at {position}");

            // 简化版：仅日志输出
            // 实际项目中可以：
            // 1. 加载特效预制件
            // 2. 实例化GameObject
            // 3. 播放动画或特效
            // 4. 自动销毁
        }

        public void PlayEffectAtBuilding(EffectType effectType, string buildingId)
        {
            Debug.Log($"[ParticleEffectSystem] PlayEffect: {effectType} for building {buildingId}");
        }

        /// <summary>
        /// 注册特效预制件（可选）
        /// </summary>
        public void RegisterEffectPrefab(EffectType effectType, GameObject prefab)
        {
            if (prefab != null)
            {
                effectPrefabs[effectType.ToString()] = prefab;
            }
        }

        /// <summary>
        /// 清理所有激活的特效
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            activeEffects.Clear();
        }
    }

    public enum EffectType
    {
        BuildingPlaced,
        BuildingDemolished,
        BuildingUpgrade,
        Smoke,
        Fire,
        Explosion,
        Sparkle,
        Disaster,
        LevelUp,
        BuildingDestroyed,
    }
}
