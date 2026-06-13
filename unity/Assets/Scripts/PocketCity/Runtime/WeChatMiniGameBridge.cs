using PocketCity.Core;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PocketCity.Runtime
{
    public sealed class WeChatMiniGameBridge : MonoBehaviour
    {
        [SerializeField] private string shareTitle = "\u53e3\u888b\u57ce\u5e02\u89c4\u5212\u5e08";
        [SerializeField] private CitySaveController saveController;

        private const string WeChatSafeLifecycleFeedbackMarker = "WECHAT_SAFE_LIFECYCLE_FEEDBACK";
        private bool lifecycleCallbacksRegistered;
        private string lastPlatformStatus = string.Empty;

        public string LastPlatformStatus
        {
            get { return lastPlatformStatus; }
        }

        private void Awake()
        {
            if (saveController == null)
            {
                saveController = GetComponent<CitySaveController>();
            }
        }

        private void OnEnable()
        {
            RegisterLifecycleCallbacks();
        }

        public void OnWeChatHide()
        {
            RequestLifecycleSave("WeChat hide");
        }

        public void OnWeChatShow()
        {
            lastPlatformStatus = "Lifecycle resumed: WeChat show.";
        }

        public void ShareGame()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WxShare(shareTitle);
#else
            Debug.Log("ShareGame: " + shareTitle);
#endif
        }

        public void VibrateShort()
        {
            VibrateSafe("short");
        }

        public void VibrateSuccess()
        {
            VibrateSafe("success");
        }

        public void VibrateWarning()
        {
            VibrateSafe("warning");
        }

        public void SetStorageString(string key, string value)
        {
            TrySetStorageString(key, value);
        }

        public bool TrySetStorageString(string key, string value)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var saved = WxSetStorageString(key, value) != 0;
                if (!saved)
                {
                    lastPlatformStatus = "Storage save failed.";
                    return false;
                }
#else
                PlayerPrefs.SetString(key, value);
                PlayerPrefs.Save();
#endif
                lastPlatformStatus = "Storage saved.";
                return true;
            }
            catch (System.Exception error)
            {
                lastPlatformStatus = "Storage save failed: " + error.Message;
                Debug.LogWarning(lastPlatformStatus);
                return false;
            }
        }

        public string GetStorageString(string key)
        {
            string value;
            return TryGetStorageString(key, out value) ? value : string.Empty;
        }

        public bool TryGetStorageString(string key, out string value)
        {
            value = string.Empty;
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                value = WxGetStorageString(key);
#else
                value = PlayerPrefs.GetString(key, string.Empty);
#endif
                lastPlatformStatus = string.IsNullOrEmpty(value) ? "Storage empty." : "Storage loaded.";
                return true;
            }
            catch (System.Exception error)
            {
                lastPlatformStatus = "Storage load failed: " + error.Message;
                Debug.LogWarning(lastPlatformStatus);
                return false;
            }
        }

        public void DeleteStorageKey(string key)
        {
            TryDeleteStorageKey(key);
        }

        public bool TryDeleteStorageKey(string key)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var deleted = WxDeleteStorageKey(key) != 0;
                if (!deleted)
                {
                    lastPlatformStatus = "Storage delete failed.";
                    return false;
                }
#else
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
#endif
                lastPlatformStatus = "Storage deleted.";
                return true;
            }
            catch (System.Exception error)
            {
                lastPlatformStatus = "Storage delete failed: " + error.Message;
                Debug.LogWarning(lastPlatformStatus);
                return false;
            }
        }

        public string GetStorageStatusString()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                var status = WxGetStorageStatusString();
#else
                var status = "PlayerPrefs fallback";
#endif
                lastPlatformStatus = string.IsNullOrEmpty(status) ? "Storage status unavailable." : status;
                return lastPlatformStatus;
            }
            catch (System.Exception error)
            {
                lastPlatformStatus = "Storage status failed: " + error.Message;
                Debug.LogWarning(lastPlatformStatus);
                return lastPlatformStatus;
            }
        }

        private void VibrateSafe(string reason)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WxVibrateShort(reason);
#else
                Debug.Log("VibrateShort: " + reason);
#endif
                lastPlatformStatus = "Vibrate " + reason;
            }
            catch (System.Exception error)
            {
                lastPlatformStatus = "Vibrate failed: " + error.Message;
                Debug.LogWarning(lastPlatformStatus);
            }
        }

        private void RegisterLifecycleCallbacks()
        {
            if (lifecycleCallbacksRegistered)
            {
                return;
            }

            lifecycleCallbacksRegistered = true;
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WxRegisterLifecycleCallbacks(gameObject.name);
#else
                Debug.Log("RegisterLifecycleCallbacks: " + gameObject.name);
#endif
                lastPlatformStatus = "Lifecycle callbacks registered.";
            }
            catch (System.Exception error)
            {
                lifecycleCallbacksRegistered = false;
                lastPlatformStatus = "Lifecycle registration failed: " + error.Message;
                Debug.LogWarning(lastPlatformStatus);
            }
        }

        private bool RequestLifecycleSave(string reason)
        {
            if (saveController == null)
            {
                saveController = GetComponent<CitySaveController>();
            }

            if (saveController == null)
            {
                lastPlatformStatus = "Lifecycle save skipped: missing save controller.";
                return false;
            }

            var saved = saveController.RequestLifecycleAutoSave(reason);
            lastPlatformStatus = saved ? "Lifecycle save requested: " + reason : "Lifecycle save skipped: " + reason;
            return saved;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WxShare(string title);

        [DllImport("__Internal")]
        private static extern void WxRegisterLifecycleCallbacks(string targetName);

        [DllImport("__Internal")]
        private static extern void WxVibrateShort(string reason);

        [DllImport("__Internal")]
        private static extern int WxSetStorageString(string key, string value);

        [DllImport("__Internal")]
        private static extern string WxGetStorageString(string key);

        [DllImport("__Internal")]
        private static extern int WxDeleteStorageKey(string key);

        [DllImport("__Internal")]
        private static extern string WxGetStorageStatusString();
#endif
    }
}
