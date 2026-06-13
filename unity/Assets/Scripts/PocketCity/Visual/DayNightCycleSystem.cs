using UnityEngine;

namespace PocketCity.Visual
{
    /// <summary>
    /// 昼夜切换系统
    /// </summary>
    public class DayNightCycleSystem : MonoBehaviour
    {
        public static DayNightCycleSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableAutoCycle = true;
        [SerializeField] private float dayDurationSeconds = 180f; // 3分钟一个昼夜

        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Color dayColor = new Color(1f, 0.96f, 0.84f);
        [SerializeField] private Color nightColor = new Color(0.3f, 0.4f, 0.6f);
        [SerializeField] private Color dawnColor = new Color(1f, 0.7f, 0.5f);
        [SerializeField] private Color duskColor = new Color(1f, 0.6f, 0.4f);

        [Header("Ambient")]
        [SerializeField] private Color ambientDayColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color ambientNightColor = new Color(0.2f, 0.2f, 0.3f);

        [Header("Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogDayColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color fogNightColor = new Color(0.1f, 0.1f, 0.2f);

        private float currentTime = 0f; // 0-1，0=午夜，0.5=正午
        private TimeOfDay currentTimeOfDay = TimeOfDay.Day;

        public enum TimeOfDay
        {
            Night,   // 0.0 - 0.25
            Dawn,    // 0.25 - 0.3
            Day,     // 0.3 - 0.7
            Dusk,    // 0.7 - 0.75
            Evening  // 0.75 - 1.0
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (directionalLight == null)
            {
                directionalLight = FindAnyObjectByType<Light>();
            }

            // 从正午开始
            currentTime = 0.5f;
            UpdateLighting();
        }

        private void Update()
        {
            if (enableAutoCycle)
            {
                AdvanceTime(Time.deltaTime / dayDurationSeconds);
            }

            UpdateLighting();
        }

        private void AdvanceTime(float delta)
        {
            currentTime += delta;
            if (currentTime >= 1f)
            {
                currentTime -= 1f;
            }
        }

        private void UpdateLighting()
        {
            if (directionalLight == null) return;

            // 更新时段
            UpdateTimeOfDay();

            // 计算颜色
            Color lightColor = GetLightColor(currentTime);
            Color ambientColor = GetAmbientColor(currentTime);

            // 应用
            directionalLight.color = lightColor;
            directionalLight.intensity = GetLightIntensity(currentTime);

            // 旋转太阳
            float sunAngle = currentTime * 360f - 90f; // -90度让正午在头顶
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);

            // 环境光
            RenderSettings.ambientLight = ambientColor;

            // 雾
            if (enableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(fogNightColor, fogDayColor, GetDayProgress(currentTime));
            }
        }

        private void UpdateTimeOfDay()
        {
            TimeOfDay newTime = GetTimeOfDay(currentTime);
            if (newTime != currentTimeOfDay)
            {
                currentTimeOfDay = newTime;
                OnTimeOfDayChanged(newTime);
            }
        }

        private TimeOfDay GetTimeOfDay(float time)
        {
            if (time < 0.25f) return TimeOfDay.Night;
            if (time < 0.3f) return TimeOfDay.Dawn;
            if (time < 0.7f) return TimeOfDay.Day;
            if (time < 0.75f) return TimeOfDay.Dusk;
            return TimeOfDay.Evening;
        }

        private Color GetLightColor(float time)
        {
            if (time < 0.25f)
                return nightColor;
            else if (time < 0.3f)
                return Color.Lerp(nightColor, dawnColor, (time - 0.25f) / 0.05f);
            else if (time < 0.35f)
                return Color.Lerp(dawnColor, dayColor, (time - 0.3f) / 0.05f);
            else if (time < 0.65f)
                return dayColor;
            else if (time < 0.7f)
                return Color.Lerp(dayColor, duskColor, (time - 0.65f) / 0.05f);
            else if (time < 0.75f)
                return Color.Lerp(duskColor, nightColor, (time - 0.7f) / 0.05f);
            else
                return nightColor;
        }

        private Color GetAmbientColor(float time)
        {
            float dayProgress = GetDayProgress(time);
            return Color.Lerp(ambientNightColor, ambientDayColor, dayProgress);
        }

        private float GetLightIntensity(float time)
        {
            float dayProgress = GetDayProgress(time);
            return Mathf.Lerp(0.3f, 1f, dayProgress);
        }

        private float GetDayProgress(float time)
        {
            if (time < 0.25f)
                return 0f; // 深夜
            else if (time < 0.5f)
                return (time - 0.25f) / 0.25f; // 日出
            else if (time < 0.7f)
                return 1f; // 白天
            else
                return 1f - (time - 0.7f) / 0.3f; // 日落
        }

        private void OnTimeOfDayChanged(TimeOfDay newTime)
        {
            Debug.Log($"时段变化：{newTime}");

            // 可以触发事件，如路灯开启、商店营业时间等
            switch (newTime)
            {
                case TimeOfDay.Dawn:
                    // 黎明
                    break;
                case TimeOfDay.Day:
                    // 白天
                    break;
                case TimeOfDay.Dusk:
                    // 黄昏
                    break;
                case TimeOfDay.Night:
                    // 夜晚 - 开启路灯
                    break;
            }
        }

        /// <summary>
        /// 手动切换到指定时段
        /// </summary>
        public void SetTimeOfDay(TimeOfDay time)
        {
            currentTime = time switch
            {
                TimeOfDay.Night => 0.1f,
                TimeOfDay.Dawn => 0.27f,
                TimeOfDay.Day => 0.5f,
                TimeOfDay.Dusk => 0.72f,
                TimeOfDay.Evening => 0.85f,
                _ => 0.5f
            };

            UpdateLighting();
        }

        /// <summary>
        /// 切换昼夜（一键切换）
        /// </summary>
        public void ToggleDayNight()
        {
            if (currentTimeOfDay == TimeOfDay.Day)
                SetTimeOfDay(TimeOfDay.Night);
            else
                SetTimeOfDay(TimeOfDay.Day);
        }

        /// <summary>
        /// 启用/禁用自动循环
        /// </summary>
        public void SetAutoCycle(bool enabled)
        {
            enableAutoCycle = enabled;
        }

        /// <summary>
        /// 获取当前时段
        /// </summary>
        public TimeOfDay GetCurrentTimeOfDay()
        {
            return currentTimeOfDay;
        }

        /// <summary>
        /// 获取当前时间（0-1）
        /// </summary>
        public float GetCurrentTime()
        {
            return currentTime;
        }

        /// <summary>
        /// 获取当前时间文本
        /// </summary>
        public string GetTimeString()
        {
            int hour = Mathf.FloorToInt(currentTime * 24f);
            int minute = Mathf.FloorToInt((currentTime * 24f - hour) * 60f);
            return $"{hour:D2}:{minute:D2}";
        }
    }
}
