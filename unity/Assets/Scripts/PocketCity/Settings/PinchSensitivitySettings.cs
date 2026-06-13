using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PocketCity.Settings
{
    /// <summary>
    /// 捏合灵敏度设置系统
    /// </summary>
    public class PinchSensitivitySettings : MonoBehaviour
    {
        public static PinchSensitivitySettings Instance { get; private set; }

        [Header("Default Values")]
        [SerializeField] private float defaultZoomSpeed = 0.5f;
        [SerializeField] private float defaultRotationSpeed = 1f;
        [SerializeField] private float defaultPanSpeed = 1f;

        [Header("Ranges")]
        [SerializeField] private float minZoomSpeed = 0.1f;
        [SerializeField] private float maxZoomSpeed = 2f;
        [SerializeField] private float minRotationSpeed = 0.2f;
        [SerializeField] private float maxRotationSpeed = 3f;
        [SerializeField] private float minPanSpeed = 0.3f;
        [SerializeField] private float maxPanSpeed = 3f;

        // 当前设置
        private float currentZoomSpeed;
        private float currentRotationSpeed;
        private float currentPanSpeed;

        public event System.Action OnSettingsChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            LoadSettings();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            currentZoomSpeed = PlayerPrefs.GetFloat("ZoomSpeed", defaultZoomSpeed);
            currentRotationSpeed = PlayerPrefs.GetFloat("RotationSpeed", defaultRotationSpeed);
            currentPanSpeed = PlayerPrefs.GetFloat("PanSpeed", defaultPanSpeed);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("ZoomSpeed", currentZoomSpeed);
            PlayerPrefs.SetFloat("RotationSpeed", currentRotationSpeed);
            PlayerPrefs.SetFloat("PanSpeed", currentPanSpeed);
            PlayerPrefs.Save();
        }

        // === Zoom Speed ===
        public void SetZoomSpeed(float speed)
        {
            currentZoomSpeed = Mathf.Clamp(speed, minZoomSpeed, maxZoomSpeed);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public float GetZoomSpeed() => currentZoomSpeed;

        // === Rotation Speed ===
        public void SetRotationSpeed(float speed)
        {
            currentRotationSpeed = Mathf.Clamp(speed, minRotationSpeed, maxRotationSpeed);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public float GetRotationSpeed() => currentRotationSpeed;

        // === Pan Speed ===
        public void SetPanSpeed(float speed)
        {
            currentPanSpeed = Mathf.Clamp(speed, minPanSpeed, maxPanSpeed);
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        public float GetPanSpeed() => currentPanSpeed;

        // === Presets ===
        public void ApplyPreset(SensitivityPreset preset)
        {
            switch (preset)
            {
                case SensitivityPreset.Slow:
                    SetZoomSpeed(0.2f);
                    SetRotationSpeed(0.5f);
                    SetPanSpeed(0.5f);
                    break;

                case SensitivityPreset.Normal:
                    SetZoomSpeed(0.5f);
                    SetRotationSpeed(1f);
                    SetPanSpeed(1f);
                    break;

                case SensitivityPreset.Fast:
                    SetZoomSpeed(1f);
                    SetRotationSpeed(2f);
                    SetPanSpeed(2f);
                    break;

                case SensitivityPreset.VeryFast:
                    SetZoomSpeed(1.5f);
                    SetRotationSpeed(3f);
                    SetPanSpeed(2.5f);
                    break;
            }

            Debug.Log($"应用灵敏度预设：{preset}");
        }

        public void ResetToDefault()
        {
            currentZoomSpeed = defaultZoomSpeed;
            currentRotationSpeed = defaultRotationSpeed;
            currentPanSpeed = defaultPanSpeed;
            SaveSettings();
            OnSettingsChanged?.Invoke();
            Debug.Log("重置为默认灵敏度");
        }

        public enum SensitivityPreset
        {
            Slow,
            Normal,
            Fast,
            VeryFast
        }
    }

    /// <summary>
    /// 灵敏度设置UI面板
    /// </summary>
    public class SensitivitySettingsUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider zoomSlider;
        [SerializeField] private Slider rotationSlider;
        [SerializeField] private Slider panSlider;
        [SerializeField] private TextMeshProUGUI zoomValueText;
        [SerializeField] private TextMeshProUGUI rotationValueText;
        [SerializeField] private TextMeshProUGUI panValueText;

        private void Start()
        {
            InitializeUI();
            LoadCurrentSettings();
        }

        private void InitializeUI()
        {
            if (zoomSlider != null)
            {
                zoomSlider.minValue = 0.1f;
                zoomSlider.maxValue = 2f;
                zoomSlider.onValueChanged.AddListener(OnZoomChanged);
            }

            if (rotationSlider != null)
            {
                rotationSlider.minValue = 0.2f;
                rotationSlider.maxValue = 3f;
                rotationSlider.onValueChanged.AddListener(OnRotationChanged);
            }

            if (panSlider != null)
            {
                panSlider.minValue = 0.3f;
                panSlider.maxValue = 3f;
                panSlider.onValueChanged.AddListener(OnPanChanged);
            }
        }

        private void LoadCurrentSettings()
        {
            if (PinchSensitivitySettings.Instance == null) return;

            if (zoomSlider != null)
            {
                zoomSlider.value = PinchSensitivitySettings.Instance.GetZoomSpeed();
                UpdateZoomText(zoomSlider.value);
            }

            if (rotationSlider != null)
            {
                rotationSlider.value = PinchSensitivitySettings.Instance.GetRotationSpeed();
                UpdateRotationText(rotationSlider.value);
            }

            if (panSlider != null)
            {
                panSlider.value = PinchSensitivitySettings.Instance.GetPanSpeed();
                UpdatePanText(panSlider.value);
            }
        }

        private void OnZoomChanged(float value)
        {
            PinchSensitivitySettings.Instance?.SetZoomSpeed(value);
            UpdateZoomText(value);
        }

        private void OnRotationChanged(float value)
        {
            PinchSensitivitySettings.Instance?.SetRotationSpeed(value);
            UpdateRotationText(value);
        }

        private void OnPanChanged(float value)
        {
            PinchSensitivitySettings.Instance?.SetPanSpeed(value);
            UpdatePanText(value);
        }

        private void UpdateZoomText(float value)
        {
            if (zoomValueText != null)
                zoomValueText.text = $"{value:F1}x";
        }

        private void UpdateRotationText(float value)
        {
            if (rotationValueText != null)
                rotationValueText.text = $"{value:F1}x";
        }

        private void UpdatePanText(float value)
        {
            if (panValueText != null)
                panValueText.text = $"{value:F1}x";
        }

        public void Show()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void Hide()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        public void ApplyPreset(int presetIndex)
        {
            var preset = (PinchSensitivitySettings.SensitivityPreset)presetIndex;
            PinchSensitivitySettings.Instance?.ApplyPreset(preset);
            LoadCurrentSettings();
        }

        public void ResetToDefault()
        {
            PinchSensitivitySettings.Instance?.ResetToDefault();
            LoadCurrentSettings();
        }
    }

    /// <summary>
    /// 应用灵敏度设置到相机控制器
    /// </summary>
    public class CameraControllerWithSensitivity : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private Vector2 lastTouchPosition;
        private float lastPinchDistance;

        private void Update()
        {
            HandleTouchInput();
        }

        private void HandleTouchInput()
        {
            // 双指缩放
            if (UnityEngine.Input.touchCount == 2)
            {
                HandlePinchZoom();
            }
            // 单指拖动
            else if (UnityEngine.Input.touchCount == 1)
            {
                HandlePan();
            }
        }

        private void HandlePinchZoom()
        {
            Touch touch0 = UnityEngine.Input.GetTouch(0);
            Touch touch1 = UnityEngine.Input.GetTouch(1);

            float currentDistance = Vector2.Distance(touch0.position, touch1.position);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                lastPinchDistance = currentDistance;
                return;
            }

            float deltaDist = currentDistance - lastPinchDistance;
            float zoomSpeed = PinchSensitivitySettings.Instance?.GetZoomSpeed() ?? 0.5f;

            // 应用缩放
            if (targetCamera != null)
            {
                float newSize = targetCamera.orthographicSize - deltaDist * zoomSpeed * 0.01f;
                targetCamera.orthographicSize = Mathf.Clamp(newSize, 5f, 50f);
            }

            lastPinchDistance = currentDistance;
        }

        private void HandlePan()
        {
            Touch touch = UnityEngine.Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                return;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastTouchPosition;
                float panSpeed = PinchSensitivitySettings.Instance?.GetPanSpeed() ?? 1f;

                // 应用平移
                if (targetCamera != null)
                {
                    Vector3 movement = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * 0.01f;
                    targetCamera.transform.position += movement;
                }

                lastTouchPosition = touch.position;
            }
        }
    }
}
