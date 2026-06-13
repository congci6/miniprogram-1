using UnityEngine;
using System.Collections.Generic;
using PocketCity.Trade;
using PocketCity.Production;
using PocketCity.Materials;

namespace PocketCity.Integration
{
    /// <summary>
    /// 智能货运订单生成器 - 解决F-18货运订单与生产无关联
    /// 根据玩家库存和生产能力生成合理订单
    /// </summary>
    public class SmartCargoOrderGenerator : MonoBehaviour
    {
        public static SmartCargoOrderGenerator Instance { get; private set; }

        [SerializeField] private DanielCargoSystem cargoSystem;
        [SerializeField] private StorageSystem storage;
        [SerializeField] private UpgradeMaterialSystem materialSystem;

        [Header("Settings")]
        [SerializeField] private float feasibilityWeight = 0.7f; // 可行性权重

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
            if (cargoSystem == null)
                cargoSystem = FindAnyObjectByType<DanielCargoSystem>();

            if (storage == null)
                storage = FindAnyObjectByType<StorageSystem>();

            if (materialSystem == null)
                materialSystem = FindAnyObjectByType<UpgradeMaterialSystem>();
        }

        /// <summary>
        /// 生成智能订单（基于库存）
        /// </summary>
        public Trade.CargoOrder GenerateSmartOrder(bool isUrgent = false)
        {
            var order = new Trade.CargoOrder
            {
                id = System.Guid.NewGuid().ToString(),
                isUrgent = isUrgent,
                rewardCash = 0,
                rewardGoldenKeys = isUrgent ? 2 : 1
            };

            if (isUrgent)
            {
                order.expiryTime = Time.time + 900f; // 15分钟
                order.urgentMultiplier = Random.Range(2f, 3f);
            }
            else
            {
                order.expiryTime = Time.time + 86400f; // 24小时
                order.urgentMultiplier = 1f;
            }

            // 生成订单物品（智能选择）
            int itemCount = isUrgent ? Random.Range(2, 4) : Random.Range(3, 6);
            for (int i = 0; i < itemCount; i++)
            {
                var item = GenerateSmartItem(isUrgent);
                if (item != null)
                {
                    order.items.Add(item);
                    order.rewardCash += (int)(item.amount * GetMaterialBasePrice(item.materialId) * order.urgentMultiplier);
                }
            }

            return order;
        }

        /// <summary>
        /// 生成智能物品（优先选择玩家库存充足的材料）
        /// </summary>
        private Trade.CargoItem GenerateSmartItem(bool isUrgent)
        {
            List<string> candidateMaterials = GetCandidateMaterials(isUrgent);
            if (candidateMaterials.Count == 0)
                return null;

            // 根据库存量加权选择
            string selectedMaterial = SelectMaterialByInventory(candidateMaterials);

            int amount = CalculateReasonableAmount(selectedMaterial, isUrgent);

            return new Trade.CargoItem
            {
                materialId = selectedMaterial,
                amount = amount
            };
        }

        private List<string> GetCandidateMaterials(bool isUrgent)
        {
            List<string> candidates = new List<string>();

            if (isUrgent)
            {
                // 紧急订单：高级材料
                candidates.AddRange(new[] { "furniture", "engine", "appliances", "lighting", "windows", "bathroom" });
            }
            else
            {
                // 普通订单：基础+加工材料
                candidates.AddRange(new[] { "wood_plank", "iron_ingot", "nails", "gears", "cement", "fabric", "glass", "brick", "wires", "pipes" });
            }

            return candidates;
        }

        private string SelectMaterialByInventory(List<string> candidates)
        {
            // 计算每个材料的权重（库存越多，权重越高）
            Dictionary<string, float> weights = new Dictionary<string, float>();
            float totalWeight = 0f;

            foreach (var material in candidates)
            {
                int inventory = GetInventoryAmount(material);
                float weight = Mathf.Pow(inventory + 1, feasibilityWeight); // 库存量的指数权重

                weights[material] = weight;
                totalWeight += weight;
            }

            // 加权随机选择
            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (random <= cumulative)
                {
                    return kvp.Key;
                }
            }

            return candidates[0]; // 默认返回第一个
        }

        private int CalculateReasonableAmount(string materialId, bool isUrgent)
        {
            int inventory = GetInventoryAmount(materialId);

            if (isUrgent)
            {
                // 紧急订单：要求较少（1-3）
                return Random.Range(1, 4);
            }
            else
            {
                // 普通订单：基于库存量
                if (inventory >= 10)
                    return Random.Range(4, 8);
                else if (inventory >= 5)
                    return Random.Range(2, 5);
                else
                    return Random.Range(1, 3);
            }
        }

        private int GetInventoryAmount(string materialId)
        {
            int amount = 0;

            if (storage != null)
            {
                amount += storage.GetItemAmount(materialId);
            }

            if (materialSystem != null)
            {
                amount += materialSystem.GetMaterialAmount(materialId);
            }

            return amount;
        }

        private int GetMaterialBasePrice(string materialId)
        {
            // 根据材料等级返回基础价格
            if (IsTier4Material(materialId))
                return 200;
            else if (IsTier3Material(materialId))
                return 50;
            else if (IsTier2Material(materialId))
                return 30;
            else
                return 10;
        }

        private bool IsTier4Material(string id)
        {
            return id == "engine" || id == "furniture" || id == "appliances" || id == "lighting" || id == "windows" || id == "bathroom";
        }

        private bool IsTier3Material(string id)
        {
            return id == "nails" || id == "gears" || id == "wires" || id == "pipes" || id == "screws" || id == "tires" || id == "cement";
        }

        private bool IsTier2Material(string id)
        {
            return id == "iron_ingot" || id == "wood_plank" || id == "plastic" || id == "fabric" || id == "glass" || id == "brick";
        }

        /// <summary>
        /// 检查订单是否可完成
        /// </summary>
        public bool IsOrderFeasible(Trade.CargoOrder order)
        {
            foreach (var item in order.items)
            {
                int inventory = GetInventoryAmount(item.materialId);
                if (inventory < item.amount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取订单可行性评分（0-1）
        /// </summary>
        public float GetOrderFeasibilityScore(Trade.CargoOrder order)
        {
            if (order.items.Count == 0)
                return 0f;

            float totalScore = 0f;

            foreach (var item in order.items)
            {
                int inventory = GetInventoryAmount(item.materialId);
                float itemScore = Mathf.Min(1f, inventory / (float)item.amount);
                totalScore += itemScore;
            }

            return totalScore / order.items.Count;
        }
    }
}
