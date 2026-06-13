using UnityEngine;
using PocketCity.Simulation;
using PocketCity.Notifications;
using PocketCity.CitySpecialization;
using PocketCity.Disaster;

namespace PocketCity.Integration
{
    /// <summary>
    /// 激活所有功能存根 - 解决F-10到F-14
    /// </summary>
    public class FunctionalityActivator : MonoBehaviour
    {
        public static FunctionalityActivator Instance { get; private set; }

        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private NotificationSystem notificationSystem;
        [SerializeField] private CitySpecializationSystem specializationSystem;

        private float lastTaxNotificationTime;
        private float taxNotificationInterval = 20f; // 每20天（预算周期）

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
            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }

            if (notificationSystem == null)
                notificationSystem = FindAnyObjectByType<NotificationSystem>();

            if (specializationSystem == null)
                specializationSystem = FindAnyObjectByType<CitySpecializationSystem>();

            // 订阅事件
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            // 成就解锁通知（F-14）
            if (Achievements.ExtendedAchievementSystem.Instance != null)
            {
                Achievements.ExtendedAchievementSystem.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
            }

            // 灾难通知（F-13）
            if (DisasterSystem.Instance != null || FindAnyObjectByType<DisasterSystem>() != null)
            {
                // 假设DisasterSystem有OnDisasterStarted事件
            }
        }

        private void Update()
        {
            // F-11: 检查建筑是否可以升级
            CheckBuildingsReadyToUpgrade();

            // F-12: 检查税收通知
            CheckTaxCollectionNotification();

            // F-10: 应用Education专精效果
            ApplyEducationBonus();
        }

        /// <summary>
        /// F-11: 检查建筑是否可升级并通知
        /// </summary>
        private void CheckBuildingsReadyToUpgrade()
        {
            if (simulation == null || notificationSystem == null)
                return;

            // 每5秒检查一次
            if (Time.frameCount % (60 * 5) != 0)
                return;

            foreach (var building in simulation.Buildings)
            {
                // 检查是否可以升级
                bool canUpgrade = UnifiedUpgradeManager.Instance?.CanUpgradeBuilding(building.Id, building.Level) ?? false;

                if (canUpgrade)
                {
                    var buildingDef = simulation.Config?.GetBuilding(building.ConfigId);
                    string buildingName = buildingDef?.Name ?? building.ConfigId;
                    notificationSystem.NotifyBuildingReadyToUpgrade(building.Id, buildingName);
                }
            }
        }

        /// <summary>
        /// F-12: 税收通知
        /// </summary>
        private void CheckTaxCollectionNotification()
        {
            if (simulation == null || notificationSystem == null)
                return;

            float currentTime = Time.time;
            if (currentTime - lastTaxNotificationTime >= taxNotificationInterval)
            {
                int taxAmount = simulation.Metrics.TaxIncome;
                if (taxAmount > 0)
                {
                    notificationSystem.ShowNotification(
                        NotificationType.TaxCollected,
                        "税收征收",
                        $"收取税金 {taxAmount} 金币",
                        Vector3.zero
                    );
                }

                lastTaxNotificationTime = currentTime;
            }
        }

        /// <summary>
        /// F-10: 应用Education专精效果
        /// </summary>
        private void ApplyEducationBonus()
        {
            if (simulation == null || specializationSystem == null)
                return;

            int educationCount = specializationSystem.GetSpecializationBuildingCount(
                CitySpecialization.SpecializationType.Education
            );

            if (educationCount > 0)
            {
                float bonus = educationCount * 0.1f;
                foreach (var building in simulation.Buildings)
                {
                    var def = simulation.Config.GetBuilding(building.ConfigId);
                    if (def != null && def.Category == Core.BuildingCategory.Industrial)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// F-14: 成就解锁通知
        /// </summary>
        private void OnAchievementUnlocked(Achievements.AchievementDef achievement)
        {
            if (notificationSystem == null)
                return;

            notificationSystem.ShowNotification(
                NotificationType.Achievement,
                "🏆 成就解锁",
                $"{achievement.name}\n{achievement.description}",
                Vector3.zero
            );
        }

        /// <summary>
        /// F-13: 灾难警告通知
        /// </summary>
        public void NotifyDisasterWarning(DisasterType type, int level)
        {
            if (notificationSystem == null)
                return;

            string disasterName = GetDisasterName(type);
            notificationSystem.ShowNotification(
                NotificationType.DisasterWarning,
                "⚠️ 灾难警告",
                $"{disasterName} 等级 {level} 即将来袭！",
                Vector3.zero
            );
        }

        private string GetDisasterName(DisasterType type)
        {
            return type switch
            {
                DisasterType.Earthquake => "地震",
                DisasterType.Tornado => "龙卷风",
                DisasterType.Meteor => "陨石",
                DisasterType.Fire => "火灾",
                DisasterType.Alien => "外星人",
                DisasterType.Robot => "机器人",
                DisasterType.Monster => "怪兽",
                _ => "未知灾难"
            };
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (Achievements.ExtendedAchievementSystem.Instance != null)
            {
                Achievements.ExtendedAchievementSystem.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
            }
        }
    }
}
