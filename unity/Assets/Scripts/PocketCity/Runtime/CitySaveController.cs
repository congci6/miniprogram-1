using PocketCity.Core;
using UnityEngine;

namespace PocketCity.Runtime
{
    public sealed class CitySaveController : MonoBehaviour
    {
        public static CitySaveController Instance { get; private set; }

        [SerializeField] private CityGameController controller;
        [SerializeField] private CityMapRenderer mapRenderer;
        [SerializeField] private WeChatMiniGameBridge platformBridge;
        [SerializeField] private string saveKey = "pocket_city_save_v1";
        [SerializeField] private bool autoSave = true;
        [SerializeField] private bool loadOnStartup = true;
        [SerializeField] private float autoSaveInterval = 20f;

        private const string WeChatSafeLifecycleFeedbackMarker = "WECHAT_SAFE_LIFECYCLE_FEEDBACK";
        private const float LifecycleSaveCooldownSeconds = 2f;
        private float autoSaveTimer;
        private float lastLifecycleSaveTime = -999f;
        private bool startupLoadAttempted;
        private string lastStatus = string.Empty;
        private string lastStorageStatus = string.Empty;

        public string LastStatus
        {
            get { return lastStatus; }
        }

        public string LastStorageStatus
        {
            get { return lastStorageStatus; }
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (controller == null)
            {
                controller = GetComponent<CityGameController>();
            }

            if (platformBridge == null)
            {
                platformBridge = GetComponent<WeChatMiniGameBridge>();
            }

            autoSaveTimer = Mathf.Max(5f, autoSaveInterval);
        }

        private void Start()
        {
            TryLoadOnStartup();
        }

        private void Update()
        {
            if (!autoSave || controller == null)
            {
                return;
            }

            if (!startupLoadAttempted)
            {
                return;
            }

            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer > 0f)
            {
                return;
            }

            autoSaveTimer = Mathf.Max(5f, autoSaveInterval);
            SaveGame(true);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                RequestLifecycleAutoSave("pause");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                RequestLifecycleAutoSave("focus");
            }
        }

        public bool SaveGame()
        {
            return SaveGame(false);
        }

        public bool SaveGame(bool quiet)
        {
            if (controller == null)
            {
                lastStatus = "Save failed: missing controller.";
                PlaySaveFeedback(false, quiet);
                return false;
            }

            var json = controller.ExportSaveJson();
            if (string.IsNullOrEmpty(json))
            {
                lastStatus = "Save failed: empty city snapshot.";
                PlaySaveFeedback(false, quiet);
                return false;
            }

            var saved = StorageSet(saveKey, json);
            lastStatus = saved ? "Saved day " + controller.Metrics.Day : "Save failed: storage unavailable.";
            RefreshStorageStatus();
            PlaySaveFeedback(saved, quiet);
            return saved;
        }

        public bool LoadGame()
        {
            return LoadGame(false);
        }

        private bool LoadGame(bool quiet)
        {
            if (controller == null)
            {
                lastStatus = "Load failed: missing controller.";
                PlaySaveFeedback(false, quiet);
                return false;
            }

            var json = StorageGet(saveKey);
            RefreshStorageStatus();
            if (string.IsNullOrEmpty(json))
            {
                lastStatus = "No save found.";
                PlaySaveFeedback(false, quiet);
                return false;
            }

            if (!controller.ImportSaveJson(json))
            {
                lastStatus = "Load failed: save data rejected.";
                PlaySaveFeedback(false, quiet);
                return false;
            }

            if (mapRenderer != null)
            {
                mapRenderer.RebuildAll();
            }

            lastStatus = "Loaded day " + controller.Metrics.Day;
            PlaySaveFeedback(true, quiet);
            return true;
        }

        public void DeleteSave()
        {
            var deleted = StorageDelete(saveKey);
            lastStatus = deleted ? "Save deleted." : "Delete failed: storage unavailable.";
            RefreshStorageStatus();
            PlaySaveFeedback(deleted, false);
        }

        public bool RequestLifecycleAutoSave(string reason)
        {
            if (!autoSave || controller == null)
            {
                return false;
            }

            if (Time.unscaledTime - lastLifecycleSaveTime < LifecycleSaveCooldownSeconds)
            {
                return false;
            }

            lastLifecycleSaveTime = Time.unscaledTime;
            if (SaveGame(true))
            {
                lastStatus = "Auto-saved on " + reason + " day " + controller.Metrics.Day;
                return true;
            }

            return false;
        }

        private void AutoSaveOnApplicationPause()
        {
            RequestLifecycleAutoSave("pause");
        }

        private void TryLoadOnStartup()
        {
            if (startupLoadAttempted)
            {
                return;
            }

            startupLoadAttempted = true;
            autoSaveTimer = Mathf.Max(5f, autoSaveInterval);
            if (loadOnStartup)
            {
                LoadGame(true);
            }
        }

        private bool StorageSet(string key, string value)
        {
            if (platformBridge != null)
            {
                return platformBridge.TrySetStorageString(key, value);
            }

            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
            return true;
        }

        private string StorageGet(string key)
        {
            if (platformBridge != null)
            {
                string value;
                return platformBridge.TryGetStorageString(key, out value) ? value : string.Empty;
            }

            return PlayerPrefs.GetString(key, string.Empty);
        }

        private bool StorageDelete(string key)
        {
            if (platformBridge != null)
            {
                return platformBridge.TryDeleteStorageKey(key);
            }

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return true;
        }

        private void PlaySaveFeedback(bool success, bool quiet)
        {
            if (quiet || platformBridge == null)
            {
                return;
            }

            if (success)
            {
                platformBridge.VibrateSuccess();
            }
            else
            {
                platformBridge.VibrateWarning();
            }
        }

        private void RefreshStorageStatus()
        {
            if (platformBridge != null)
            {
                lastStorageStatus = platformBridge.GetStorageStatusString();
            }
            else
            {
                lastStorageStatus = "PlayerPrefs fallback";
            }
        }
    }
}
