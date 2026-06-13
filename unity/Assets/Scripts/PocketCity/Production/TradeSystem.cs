using System;
using System.Collections.Generic;
using UnityEngine;
using PocketCity.Core;

namespace PocketCity.Production
{
    [Serializable]
    public class MarketListing
    {
        public string sellerId;
        public string materialId;
        public int amount;
        public int pricePerUnit;
        public float listTime;
    }

    [Serializable]
    public class CargoOrder
    {
        public string orderId;
        public List<Recipe> requirements;
        public int reward;
        public float deadline;
        public bool isCompleted;
    }

    public class TradeSystem : MonoBehaviour
    {
        public static TradeSystem Instance { get; private set; }

        [SerializeField] private MaterialDatabase materialDB;
        [SerializeField] private StorageSystem storage;

        [Header("Global Market")]
        [SerializeField] private float marketRefreshInterval = 300f; // 5分钟
        private List<MarketListing> globalMarket = new List<MarketListing>();
        private float lastMarketRefresh;

        [Header("Cargo Orders")]
        [SerializeField] private int maxActiveOrders = 3;
        private List<CargoOrder> activeOrders = new List<CargoOrder>();

        public event Action OnMarketRefreshed;
        public event Action<CargoOrder> OnOrderCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            RefreshMarket();
            GenerateCargoOrders();
        }

        private void Update()
        {
            if (Time.time - lastMarketRefresh > marketRefreshInterval)
            {
                RefreshMarket();
            }
        }

        // 全球市场
        public void RefreshMarket()
        {
            globalMarket.Clear();
            if (materialDB == null || materialDB.materials == null)
            {
                lastMarketRefresh = Time.time;
                OnMarketRefreshed?.Invoke();
                return;
            }

            var random = new System.Random();

            foreach (var material in materialDB.materials)
            {
                if (material == null || string.IsNullOrEmpty(material.id) || material.basePrice <= 0)
                {
                    continue;
                }

                if (random.Next(0, 100) < 60) // 60% 出现概率
                {
                    var maxPrice = Mathf.Max(material.basePrice + 1, (int)(material.basePrice * 1.5f));
                    globalMarket.Add(new MarketListing
                    {
                        sellerId = "NPC_" + random.Next(1000, 9999),
                        materialId = material.id,
                        amount = random.Next(1, 10),
                        pricePerUnit = random.Next(material.basePrice, maxPrice),
                        listTime = Time.time
                    });
                }
            }

            lastMarketRefresh = Time.time;
            OnMarketRefreshed?.Invoke();
        }

        public bool BuyFromMarket(MarketListing listing, int amount)
        {
            if (listing == null || storage == null || amount <= 0)
            {
                return false;
            }

            if (amount > listing.amount) return false;
            if (storage.CurrentCapacity + amount > storage.MaxCapacity) return false;

            int totalCost = listing.pricePerUnit * amount;

            var currency = CurrencySystem.Instance;
            if (currency == null || !currency.CanAfford(totalCost))
                return false;

            if (!currency.SpendCoins(totalCost))
                return false;

            storage.AddItem(listing.materialId, amount);
            listing.amount -= amount;

            if (listing.amount == 0)
                globalMarket.Remove(listing);

            return true;
        }

        public bool SellToMarket(string materialId, int amount, int pricePerUnit)
        {
            if (materialDB == null || storage == null || amount <= 0)
            {
                return false;
            }

            var material = materialDB.GetMaterial(materialId);
            if (material == null) return false;

            // 价格范围限制
            int minPrice = (int)(material.basePrice * 0.8f);
            int maxPrice = (int)(material.basePrice * 2f);
            pricePerUnit = Mathf.Clamp(pricePerUnit, minPrice, maxPrice);

            if (!storage.RemoveItem(materialId, amount))
                return false;

            globalMarket.Add(new MarketListing
            {
                sellerId = "Player",
                materialId = materialId,
                amount = amount,
                pricePerUnit = pricePerUnit,
                listTime = Time.time
            });

            int earnings = pricePerUnit * amount;
            var currency = CurrencySystem.Instance;
            if (currency != null)
            {
                currency.AddCoins(earnings);
            }

            return true;
        }

        public List<MarketListing> GetMarketListings() => new List<MarketListing>(globalMarket);

        // 货运订单系统 (Daniel Cargo)
        public void GenerateCargoOrders()
        {
            if (materialDB == null || materialDB.materials == null || materialDB.materials.Count == 0)
            {
                return;
            }

            var materials = new List<MaterialData>();
            for (var i = 0; i < materialDB.materials.Count; i += 1)
            {
                var material = materialDB.materials[i];
                if (material != null && !string.IsNullOrEmpty(material.id))
                {
                    materials.Add(material);
                }
            }

            if (materials.Count == 0)
            {
                return;
            }

            var random = new System.Random();

            while (activeOrders.Count < maxActiveOrders)
            {
                var order = new CargoOrder
                {
                    orderId = "CARGO_" + Guid.NewGuid().ToString().Substring(0, 8),
                    requirements = new List<Recipe>(),
                    deadline = Time.time + random.Next(600, 1800), // 10-30分钟
                    isCompleted = false
                };

                // 随机1-3种材料需求
                int reqCount = random.Next(1, 4);
                for (int i = 0; i < reqCount; i++)
                {
                    var mat = materials[random.Next(materials.Count)];
                    order.requirements.Add(new Recipe
                    {
                        materialId = mat.id,
                        amount = random.Next(1, 5)
                    });
                }

                // 计算奖励
                order.reward = 0;
                foreach (var req in order.requirements)
                {
                    var mat = materialDB.GetMaterial(req.materialId);
                    if (mat != null)
                    {
                        order.reward += mat.basePrice * req.amount;
                    }
                }
                order.reward = (int)(order.reward * 1.2f); // 20%溢价

                activeOrders.Add(order);
            }
        }

        public bool CompleteCargoOrder(string orderId)
        {
            if (storage == null)
            {
                return false;
            }

            var order = activeOrders.Find(o => o.orderId == orderId);
            if (order == null || order.isCompleted) return false;

            // 检查是否超时
            if (Time.time > order.deadline)
            {
                activeOrders.Remove(order);
                return false;
            }

            // 检查材料
            if (!storage.HasMaterials(order.requirements))
                return false;

            // 消耗材料
            storage.ConsumeMaterials(order.requirements);

            order.isCompleted = true;

            var currency = CurrencySystem.Instance;
            if (currency != null)
            {
                currency.AddCoins(order.reward);
            }

            OnOrderCompleted?.Invoke(order);

            activeOrders.Remove(order);
            GenerateCargoOrders(); // 生成新订单

            return true;
        }

        public List<CargoOrder> GetActiveOrders() => new List<CargoOrder>(activeOrders);

        public float GetOrderTimeRemaining(string orderId)
        {
            var order = activeOrders.Find(o => o.orderId == orderId);
            return order != null ? Mathf.Max(0, order.deadline - Time.time) : 0;
        }
    }
}
