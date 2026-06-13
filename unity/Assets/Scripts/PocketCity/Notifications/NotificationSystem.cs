using UnityEngine;
using System.Collections.Generic;
using PocketCity.Production;

namespace PocketCity.Notifications
{
    public enum NotificationType
    {
        ProductionComplete,
        BuildingReadyToUpgrade,
        BuildingUpgrade,
        TaxCollected,
        DisasterWarning,
        AchievementUnlocked,
        Achievement,
        Generic,
    }

    [System.Serializable]
    public class GameNotification
    {
        public string id;
        public NotificationType type;
        public string title;
        public string message;
        public Vector3 worldPosition;
        public float timestamp;
        public System.Action onClicked;
    }

    /// <summary>
    /// 游戏内通知系统（替代微信推送的本地通知）
    /// </summary>
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance { get; private set; }

        [SerializeField] private int maxNotifications = 10;
        [SerializeField] private float notificationDuration = 5f;

        private List<GameNotification> activeNotifications = new List<GameNotification>();

        public event System.Action<GameNotification> OnNotificationAdded;
        public event System.Action<GameNotification> OnNotificationClicked;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // 监听生产完成
            if (ProductionChainSystem.Instance != null)
            {
                ProductionChainSystem.Instance.OnProductionComplete += OnProductionComplete;
            }
        }

        private void OnProductionComplete(MaterialData material)
        {
            ShowNotification(
                NotificationType.ProductionComplete,
                "生产完成",
                $"{material.name} 已完成生产！点击收取",
                Vector3.zero
            );

            // 播放音效
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.ProductionComplete);
            }

            // 振动反馈
            if (WeChat.WeChatMiniGameBridge.Instance != null)
            {
                WeChat.WeChatMiniGameBridge.Instance.VibrateShort();
            }
        }

        public void ShowNotification(NotificationType type, string title, string message, Vector3 worldPos = default, System.Action onClicked = null)
        {
            // 移除过期通知
            activeNotifications.RemoveAll(n => Time.time - n.timestamp > notificationDuration);

            // 限制数量
            if (activeNotifications.Count >= maxNotifications)
            {
                activeNotifications.RemoveAt(0);
            }

            var notification = new GameNotification
            {
                id = System.Guid.NewGuid().ToString(),
                type = type,
                title = title,
                message = message,
                worldPosition = worldPos,
                timestamp = Time.time,
                onClicked = onClicked
            };

            activeNotifications.Add(notification);
            OnNotificationAdded?.Invoke(notification);

            Debug.Log($"[通知] {title}: {message}");
        }

        public void NotifyBuildingReadyToUpgrade(string buildingId, string buildingName)
        {
            ShowNotification(
                NotificationType.BuildingReadyToUpgrade,
                "建筑可升级",
                $"{buildingName} 已满足升级条件！",
                Vector3.zero,
                () => OnUpgradeNotificationClicked(buildingId)
            );
        }

        private void OnUpgradeNotificationClicked(string buildingId)
        {
            // TODO: 打开建筑升级UI
            Debug.Log($"点击升级建筑: {buildingId}");
        }

        public void ClickNotification(GameNotification notification)
        {
            notification.onClicked?.Invoke();
            activeNotifications.Remove(notification);
            OnNotificationClicked?.Invoke(notification);
        }

        public List<GameNotification> GetActiveNotifications()
        {
            return new List<GameNotification>(activeNotifications);
        }

        public int GetUnreadCount()
        {
            return activeNotifications.Count;
        }

        public void ClearAll()
        {
            activeNotifications.Clear();
        }
    }

    /// <summary>
    /// 微信小游戏推送通知（订阅消息）
    /// </summary>
    public class WeChatPushNotificationManager : MonoBehaviour
    {
        private const string TEMPLATE_PRODUCTION_COMPLETE = "production_complete_template";
        private const string TEMPLATE_BUILDING_UPGRADE = "building_upgrade_template";

        public static void SubscribeProductionNotification()
        {
            if (WeChat.WeChatMiniGameBridge.Instance != null)
            {
                WeChat.WeChatMiniGameBridge.Instance.SubscribeMessage(TEMPLATE_PRODUCTION_COMPLETE);
            }
        }

        public static void SubscribeBuildingNotification()
        {
            if (WeChat.WeChatMiniGameBridge.Instance != null)
            {
                WeChat.WeChatMiniGameBridge.Instance.SubscribeMessage(TEMPLATE_BUILDING_UPGRADE);
            }
        }

        /// <summary>
        /// 应用启动时检查离线期间的变化
        /// </summary>
        public static void CheckOfflineProgress()
        {
            // 检查生产是否完成
            if (ProductionChainSystem.Instance != null)
            {
                // 遍历所有工厂，显示完成的生产
                foreach (FactoryType type in System.Enum.GetValues(typeof(FactoryType)))
                {
                    var factory = ProductionChainSystem.Instance.GetFactory(type);
                    if (factory != null)
                    {
                        var completed = factory.slots.FindAll(s => s.isCompleted);
                        if (completed.Count > 0)
                        {
                            NotificationSystem.Instance?.ShowNotification(
                                NotificationType.ProductionComplete,
                                "离线生产完成",
                                $"有 {completed.Count} 个生产已完成！",
                                Vector3.zero
                            );
                        }
                    }
                }
            }
        }
    }
}
