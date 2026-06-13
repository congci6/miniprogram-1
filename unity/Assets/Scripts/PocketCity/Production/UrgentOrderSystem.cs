using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Production
{
    [Serializable]
    public class UrgentOrder
    {
        public string orderId;
        public string title;
        public List<Recipe> requirements;
        public int baseReward;
        public int urgentBonus; // 200%+ 溢价
        public float deadline; // 剩余时间（秒）
        public float totalTime; // 总时间（用于显示进度）
        public string unlockReward; // 解锁的特殊建筑ID
        public bool isCompleted;
        public bool isExpired => deadline <= 0 && !isCompleted;
    }

    public class UrgentOrderSystem : MonoBehaviour
    {
        public static UrgentOrderSystem Instance { get; private set; }

        [SerializeField] private MaterialDatabase materialDB;
        [SerializeField] private StorageSystem storage;
        [SerializeField] private int maxActiveUrgentOrders = 2;
        [SerializeField] private float urgentOrderSpawnInterval = 1800f; // 30分钟生成一次

        private List<UrgentOrder> activeUrgentOrders = new List<UrgentOrder>();
        private float timeSinceLastSpawn;

        // 特殊奖励建筑池
        private readonly string[] specialBuildings = new string[]
        {
            "golden_statue",      // 金色雕像
            "fountain_plaza",     // 喷泉广场
            "victory_monument",   // 胜利纪念碑
            "luxury_hotel",       // 豪华酒店
            "tech_hub",           // 科技中心
            "art_museum"          // 艺术博物馆
        };

        public event Action<UrgentOrder> OnUrgentOrderSpawned;
        public event Action<UrgentOrder> OnUrgentOrderCompleted;
        public event Action<UrgentOrder> OnUrgentOrderExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // 更新倒计时
            for (int i = activeUrgentOrders.Count - 1; i >= 0; i--)
            {
                var order = activeUrgentOrders[i];
                if (!order.isCompleted)
                {
                    order.deadline -= deltaTime;
                    if (order.isExpired)
                    {
                        OnUrgentOrderExpired?.Invoke(order);
                        activeUrgentOrders.RemoveAt(i);
                    }
                }
            }

            // 生成新订单
            timeSinceLastSpawn += deltaTime;
            if (timeSinceLastSpawn >= urgentOrderSpawnInterval)
            {
                TrySpawnUrgentOrder();
                timeSinceLastSpawn = 0f;
            }
        }

        private void TrySpawnUrgentOrder()
        {
            if (activeUrgentOrders.Count >= maxActiveUrgentOrders)
                return;

            if (materialDB == null || materialDB.materials == null || materialDB.materials.Count == 0)
                return;

            var order = GenerateUrgentOrder();
            if (order != null)
            {
                activeUrgentOrders.Add(order);
                OnUrgentOrderSpawned?.Invoke(order);
            }
        }

        private UrgentOrder GenerateUrgentOrder()
        {
            // 随机选择2-4种高级材料
            var advancedMaterials = materialDB.GetMaterialsByTier(MaterialTier.Advanced);
            if (advancedMaterials.Count == 0) return null;

            var requirements = new List<Recipe>();
            int reqCount = UnityEngine.Random.Range(2, 5);
            int totalValue = 0;

            for (int i = 0; i < reqCount && advancedMaterials.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, advancedMaterials.Count);
                var mat = advancedMaterials[index];
                advancedMaterials.RemoveAt(index);

                int amount = UnityEngine.Random.Range(1, 4);
                requirements.Add(new Recipe
                {
                    materialId = mat.id,
                    amount = amount
                });

                totalValue += mat.basePrice * amount;
            }

            // 15分钟限时
            float timeLimit = 900f;

            // 200-300% 溢价
            int urgentBonus = (int)(totalValue * UnityEngine.Random.Range(2.0f, 3.0f));

            // 随机选择特殊建筑奖励
            string unlockReward = specialBuildings[UnityEngine.Random.Range(0, specialBuildings.Length)];

            return new UrgentOrder
            {
                orderId = "URGENT_" + Guid.NewGuid().ToString().Substring(0, 8),
                title = GetRandomOrderTitle(),
                requirements = requirements,
                baseReward = totalValue,
                urgentBonus = urgentBonus,
                deadline = timeLimit,
                totalTime = timeLimit,
                unlockReward = unlockReward,
                isCompleted = false
            };
        }

        private string GetRandomOrderTitle()
        {
            string[] titles = new string[]
            {
                "城市庆典紧急需求",
                "重要客户特别订单",
                "市长特批采购",
                "国际展会急单",
                "救灾物资征集",
                "皇家订单"
            };
            return titles[UnityEngine.Random.Range(0, titles.Length)];
        }

        public bool TryCompleteUrgentOrder(string orderId)
        {
            var order = activeUrgentOrders.Find(o => o.orderId == orderId);
            if (order == null || order.isCompleted || order.isExpired)
                return false;

            // 检查材料
            foreach (var req in order.requirements)
            {
                if (storage == null || storage.GetItemCount(req.materialId) < req.amount)
                    return false;
            }

            // 消耗材料
            foreach (var req in order.requirements)
            {
                storage?.RemoveItem(req.materialId, req.amount);
            }

            order.isCompleted = true;

            // 给予奖励
            var currency = Core.CurrencySystem.Instance;
            if (currency != null)
            {
                currency.AddCoins(order.baseReward + order.urgentBonus);
            }

            OnUrgentOrderCompleted?.Invoke(order);
            return true;
        }

        public List<UrgentOrder> GetActiveUrgentOrders()
        {
            return new List<UrgentOrder>(activeUrgentOrders);
        }

        public void RemoveCompletedOrders()
        {
            activeUrgentOrders.RemoveAll(o => o.isCompleted);
        }
    }
}
