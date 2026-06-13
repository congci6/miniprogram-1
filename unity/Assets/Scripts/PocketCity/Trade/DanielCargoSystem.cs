using UnityEngine;
using System.Collections.Generic;
using System;
using PocketCity.Core;
using PocketCity.Production;

namespace PocketCity.Trade
{
    [Serializable]
    public class CargoOrder
    {
        public string id;
        public List<CargoItem> items = new List<CargoItem>();
        public int rewardCash;
        public int rewardGoldenKeys;
        public float expiryTime; // 过期时间（秒）
        public bool isUrgent; // 是否紧急订单
        public float urgentMultiplier = 1f; // 紧急订单溢价
    }

    [Serializable]
    public class CargoItem
    {
        public string materialId;
        public int amount;
    }

    /// <summary>
    /// Daniel货运订单系统 - 定时刷新 + 紧急限时单
    /// </summary>
    public class DanielCargoSystem : MonoBehaviour
    {
        public static DanielCargoSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int normalOrdersPerRound = 3;
        [SerializeField] private float orderExpirySeconds = 86400f; // 24小时
        [SerializeField] private float urgentOrderExpirySeconds = 900f; // 15分钟

        [Header("References")]
        [SerializeField] private StorageSystem storage;

        private List<CargoOrder> activeOrders = new List<CargoOrder>();
        private float nextNormalRefreshTime;
        private float nextUrgentRefreshTime;

        // 定时刷新时间（真实时间）
        private readonly int[] refreshHours = { 8, 12, 18 }; // 早8点、中午12点、晚6点

        public event Action<CargoOrder> OnOrderCompleted;
        public event Action OnOrdersRefreshed;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ScheduleNextRefresh();
            ScheduleNextUrgentOrder();
        }

        private void Update()
        {
            float currentTime = Time.time;

            // 检查定时刷新
            if (currentTime >= nextNormalRefreshTime)
            {
                RefreshNormalOrders();
                ScheduleNextRefresh();
            }

            // 检查紧急订单
            if (currentTime >= nextUrgentRefreshTime)
            {
                GenerateUrgentOrder();
                ScheduleNextUrgentOrder();
            }

            // 检查订单过期
            CheckExpiredOrders();
        }

        private void ScheduleNextRefresh()
        {
            DateTime now = DateTime.Now;
            DateTime nextRefresh = now;

            // 找到下一个刷新时间点
            foreach (int hour in refreshHours)
            {
                DateTime candidate = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
                if (candidate > now)
                {
                    nextRefresh = candidate;
                    break;
                }
            }

            // 如果今天所有时间都过了，取明天的第一个时间点
            if (nextRefresh <= now)
            {
                nextRefresh = new DateTime(now.Year, now.Month, now.Day, refreshHours[0], 0, 0).AddDays(1);
            }

            nextNormalRefreshTime = Time.time + (float)(nextRefresh - now).TotalSeconds;
        }

        private void ScheduleNextUrgentOrder()
        {
            // 2-4小时随机
            float hours = UnityEngine.Random.Range(2f, 4f);
            nextUrgentRefreshTime = Time.time + hours * 3600f;
        }

        private void RefreshNormalOrders()
        {
            // 清除旧的非紧急订单
            activeOrders.RemoveAll(o => !o.isUrgent);

            // 生成3个新订单
            for (int i = 0; i < normalOrdersPerRound; i++)
            {
                activeOrders.Add(GenerateNormalOrder());
            }

            OnOrdersRefreshed?.Invoke();
            Debug.Log($"刷新了 {normalOrdersPerRound} 个普通订单");
        }

        private CargoOrder GenerateNormalOrder()
        {
            var order = new CargoOrder
            {
                id = Guid.NewGuid().ToString(),
                expiryTime = Time.time + orderExpirySeconds,
                isUrgent = false,
                rewardCash = 0,
                rewardGoldenKeys = 1
            };

            // 随机3-5种材料
            int itemCount = UnityEngine.Random.Range(3, 6);
            var materials = new[] { "wood_plank", "iron_ingot", "nails", "gears", "cement", "fabric" };

            for (int i = 0; i < itemCount; i++)
            {
                string material = materials[UnityEngine.Random.Range(0, materials.Length)];
                int amount = UnityEngine.Random.Range(2, 8);

                order.items.Add(new CargoItem { materialId = material, amount = amount });
                order.rewardCash += amount * 50; // 基础奖励
            }

            return order;
        }

        private void GenerateUrgentOrder()
        {
            var order = new CargoOrder
            {
                id = Guid.NewGuid().ToString(),
                expiryTime = Time.time + urgentOrderExpirySeconds,
                isUrgent = true,
                urgentMultiplier = UnityEngine.Random.Range(2f, 3f), // 200%-300%溢价
                rewardCash = 0,
                rewardGoldenKeys = 2
            };

            // 2-3种高级材料
            int itemCount = UnityEngine.Random.Range(2, 4);
            var materials = new[] { "furniture", "engine", "appliances", "lighting", "windows" };

            for (int i = 0; i < itemCount; i++)
            {
                string material = materials[UnityEngine.Random.Range(0, materials.Length)];
                int amount = UnityEngine.Random.Range(1, 4);

                order.items.Add(new CargoItem { materialId = material, amount = amount });
                order.rewardCash += (int)(amount * 200 * order.urgentMultiplier);
            }

            activeOrders.Add(order);

            // 通知
            if (Notifications.NotificationSystem.Instance != null)
            {
                Notifications.NotificationSystem.Instance.ShowNotification(
                    Notifications.NotificationType.ProductionComplete,
                    "🚨 紧急订单",
                    $"限时15分钟！完成可获得 {order.rewardCash} 金币",
                    Vector3.zero
                );
            }

            Debug.Log($"生成紧急订单，溢价 {order.urgentMultiplier:P0}");
        }

        private void CheckExpiredOrders()
        {
            activeOrders.RemoveAll(order =>
            {
                if (Time.time >= order.expiryTime)
                {
                    Debug.Log($"订单 {order.id} 已过期");
                    return true;
                }
                return false;
            });
        }

        public List<CargoOrder> GetActiveOrders()
        {
            return new List<CargoOrder>(activeOrders);
        }

        public bool CanCompleteOrder(string orderId)
        {
            var order = activeOrders.Find(o => o.id == orderId);
            if (order == null || storage == null) return false;

            foreach (var item in order.items)
            {
                if (storage.GetItemAmount(item.materialId) < item.amount)
                    return false;
            }

            return true;
        }

        public bool TryCompleteOrder(string orderId)
        {
            var order = activeOrders.Find(o => o.id == orderId);
            if (order == null || !CanCompleteOrder(orderId)) return false;

            // 消耗材料
            foreach (var item in order.items)
            {
                if (!storage.RemoveItem(item.materialId, item.amount))
                    return false;
            }

            // 给予奖励
            if (UnifiedCurrencySystem.Instance != null)
            {
                UnifiedCurrencySystem.Instance.AddCash(order.rewardCash);
                UnifiedCurrencySystem.Instance.AddGoldenKeys(order.rewardGoldenKeys);
            }

            // 移除订单
            activeOrders.Remove(order);

            OnOrderCompleted?.Invoke(order);

            // 播放音效
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingPlaced);
            }

            Debug.Log($"完成订单！获得 {order.rewardCash} 金币 + {order.rewardGoldenKeys} 金钥匙");
            return true;
        }

        public string GetOrderRequirementsText(string orderId)
        {
            var order = activeOrders.Find(o => o.id == orderId);
            if (order == null) return "";

            string text = order.isUrgent ? "🚨 紧急订单\n" : "📦 货运订单\n";

            foreach (var item in order.items)
            {
                int has = storage?.GetItemAmount(item.materialId) ?? 0;
                string checkMark = has >= item.amount ? "✅" : "❌";
                text += $"{checkMark} {item.materialId}: {has}/{item.amount}\n";
            }

            float remainingTime = order.expiryTime - Time.time;
            int hours = Mathf.FloorToInt(remainingTime / 3600f);
            int minutes = Mathf.FloorToInt((remainingTime % 3600f) / 60f);

            text += $"\n⏱️ 剩余时间: {hours}小时{minutes}分钟\n";
            text += $"💰 奖励: {order.rewardCash} 金币 + {order.rewardGoldenKeys} 金钥匙";

            if (order.isUrgent)
            {
                text += $"\n🔥 溢价 {order.urgentMultiplier:P0}";
            }

            return text;
        }
    }
}
