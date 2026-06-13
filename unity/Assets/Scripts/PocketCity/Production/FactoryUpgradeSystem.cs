using UnityEngine;
using System.Collections.Generic;
using PocketCity.Production;

namespace PocketCity.Production
{
    /// <summary>
    /// 工厂升级系统 - Lv1=2槽 → Lv2=3槽 → Lv3=4槽
    /// </summary>
    public class FactoryUpgradeSystem : MonoBehaviour
    {
        public static FactoryUpgradeSystem Instance { get; private set; }

        [SerializeField] private ProductionChainSystem productionSystem;
        [SerializeField] private StorageSystem storage;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 获取工厂当前等级
        /// </summary>
        public int GetFactoryLevel(FactoryType type)
        {
            var factory = productionSystem?.GetFactory(type);
            if (factory == null) return 1;

            // 根据槽位数判断等级
            return factory.maxSlots switch
            {
                2 => 1,
                3 => 2,
                4 => 3,
                _ => 1
            };
        }

        /// <summary>
        /// 获取升级成本
        /// </summary>
        public Dictionary<string, int> GetUpgradeCost(FactoryType type)
        {
            int currentLevel = GetFactoryLevel(type);

            return currentLevel switch
            {
                1 => new Dictionary<string, int> // Lv1 → Lv2
                {
                    { "wood_plank", 5 },
                    { "iron_ingot", 3 },
                    { "nails", 10 }
                },
                2 => new Dictionary<string, int> // Lv2 → Lv3
                {
                    { "wood_plank", 10 },
                    { "iron_ingot", 5 },
                    { "gears", 5 },
                    { "cement", 3 }
                },
                _ => new Dictionary<string, int>() // 已满级
            };
        }

        /// <summary>
        /// 检查是否可以升级
        /// </summary>
        public bool CanUpgradeFactory(FactoryType type)
        {
            int currentLevel = GetFactoryLevel(type);
            if (currentLevel >= 3) return false; // 已满级

            var cost = GetUpgradeCost(type);
            if (storage == null) return false;

            foreach (var item in cost)
            {
                if (storage.GetItemAmount(item.Key) < item.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 尝试升级工厂
        /// </summary>
        public bool TryUpgradeFactory(FactoryType type)
        {
            if (!CanUpgradeFactory(type)) return false;

            var cost = GetUpgradeCost(type);
            var factory = productionSystem?.GetFactory(type);
            if (factory == null) return false;

            // 消耗材料
            foreach (var item in cost)
            {
                if (!storage.RemoveItem(item.Key, item.Value))
                    return false;
            }

            // 增加槽位
            factory.maxSlots++;

            // 播放音效
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingUpgrade);
            }

            // 播放特效
            if (VFX.ParticleEffectSystem.Instance != null)
            {
                VFX.ParticleEffectSystem.Instance.PlayEffect(VFX.EffectType.LevelUp, Vector3.zero);
            }

            Debug.Log($"{type} 工厂升级到 Lv.{GetFactoryLevel(type)}，槽位: {factory.maxSlots}");
            return true;
        }

        /// <summary>
        /// 获取升级需求文本（用于UI显示）
        /// </summary>
        public string GetUpgradeRequirementsText(FactoryType type)
        {
            int currentLevel = GetFactoryLevel(type);
            if (currentLevel >= 3) return "已达最高等级 (Lv.3)";

            var cost = GetUpgradeCost(type);
            string text = $"升级到 Lv.{currentLevel + 1} 需要：\n";

            foreach (var item in cost)
            {
                int has = storage?.GetItemAmount(item.Key) ?? 0;
                string checkMark = has >= item.Value ? "✅" : "❌";
                text += $"{checkMark} {item.Key}: {has}/{item.Value}\n";
            }

            return text;
        }
    }
}
