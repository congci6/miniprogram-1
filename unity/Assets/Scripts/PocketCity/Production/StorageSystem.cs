using System;
using System.Collections.Generic;
using System.Linq;
using PocketCity.Core;
using UnityEngine;

namespace PocketCity.Production
{
    [Serializable]
    public class StorageItem
    {
        public string materialId;
        public int amount;
    }

    public class StorageSystem : MonoBehaviour
    {
        public static StorageSystem Instance { get; private set; }

        [SerializeField] private int maxCapacity = 60;
        [SerializeField] private MaterialDatabase materialDB;

        private Dictionary<string, int> inventory = new Dictionary<string, int>();

        public event Action<string, int> OnItemChanged;
        public event Action<int, int> OnCapacityChanged;

        private void Awake()
        {
            if (Instance != null) Destroy(gameObject);
            else Instance = this;
        }

        public int CurrentCapacity => inventory.Values.Sum();
        public int MaxCapacity => maxCapacity;
        public bool IsFull => CurrentCapacity >= maxCapacity;

        public bool AddItem(string materialId, int amount)
        {
            if (string.IsNullOrEmpty(materialId) || amount <= 0)
            {
                return false;
            }

            if (CurrentCapacity + amount > maxCapacity)
            {
                Debug.LogWarning("存储空间不足");
                return false;
            }

            if (!inventory.ContainsKey(materialId))
                inventory[materialId] = 0;

            inventory[materialId] += amount;
            OnItemChanged?.Invoke(materialId, inventory[materialId]);
            OnCapacityChanged?.Invoke(CurrentCapacity, maxCapacity);
            return true;
        }

        public bool RemoveItem(string materialId, int amount)
        {
            if (string.IsNullOrEmpty(materialId) || amount <= 0)
            {
                return false;
            }

            if (!inventory.ContainsKey(materialId) || inventory[materialId] < amount)
                return false;

            inventory[materialId] -= amount;
            if (inventory[materialId] == 0)
                inventory.Remove(materialId);

            OnItemChanged?.Invoke(materialId, inventory.ContainsKey(materialId) ? inventory[materialId] : 0);
            OnCapacityChanged?.Invoke(CurrentCapacity, maxCapacity);
            return true;
        }

        public int GetItemAmount(string materialId)
        {
            return inventory.TryGetValue(materialId, out var amount) ? amount : 0;
        }

        public bool HasMaterials(List<Recipe> recipe)
        {
            if (recipe == null)
            {
                return true;
            }

            return recipe.All(r => r != null && r.amount > 0 && GetItemAmount(r.materialId) >= r.amount);
        }

        public void ConsumeMaterials(List<Recipe> recipe)
        {
            foreach (var r in recipe)
            {
                RemoveItem(r.materialId, r.amount);
            }
        }

        public void ExpandCapacity(int amount)
        {
            maxCapacity += amount;
            OnCapacityChanged?.Invoke(CurrentCapacity, maxCapacity);
        }

        public int GetItemCount(string materialId)
        {
            return GetItemAmount(materialId);
        }

        /// <summary>
        /// 扩展仓库成本计算
        /// </summary>
        public int GetExpandCost()
        {
            // 成本递增：基础1000 + (当前容量/10)^2 * 100
            int baseCost = 1000;
            int scaledCost = (maxCapacity / 10) * (maxCapacity / 10) * 100;
            return baseCost + scaledCost;
        }

        /// <summary>
        /// 尝试扩展仓库（消耗金币或高级货币）
        /// </summary>
        public bool TryExpandStorage(int expandAmount = 10)
        {
            int cost = GetExpandCost();

            // 检查货币
            if (UnifiedCurrencySystem.Instance == null) return false;

            if (UnifiedCurrencySystem.Instance.Cash >= cost)
            {
                if (UnifiedCurrencySystem.Instance.SpendCash(cost))
                {
                    ExpandCapacity(expandAmount);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 使用高级货币快速扩展
        /// </summary>
        public bool TryExpandStorageWithPremium(int expandAmount = 20)
        {
            int premiumCost = 50; // 固定50高级货币

            if (UnifiedCurrencySystem.Instance == null) return false;

            if (UnifiedCurrencySystem.Instance.SpendPremium(premiumCost))
            {
                ExpandCapacity(expandAmount);
                return true;
            }

            return false;
        }

        public Dictionary<string, int> GetInventory()
        {
            return new Dictionary<string, int>(inventory);
        }

        public List<string> GetOptimizationSuggestions()
        {
            var suggestions = new List<string>();
            float usage = (float)CurrentCapacity / maxCapacity;

            if (usage > 0.9f)
                suggestions.Add("存储空间即将满载，建议扩展仓库或出售物品");

            // 检查过剩的基础材料
            foreach (var kvp in inventory)
            {
                var material = materialDB.GetMaterial(kvp.Key);
                if (material != null && material.tier == MaterialTier.Basic && kvp.Value > 20)
                {
                    suggestions.Add($"{material.name}库存过多({kvp.Value})，建议加工或出售");
                }
            }

            return suggestions;
        }
    }
}
