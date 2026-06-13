using PocketCity.Core;
using UnityEngine;
using System.Runtime.InteropServices;

namespace PocketCity.WeChat
{
    /// <summary>
    /// 微信小游戏完整功能桥接
    /// </summary>
    public class WeChatMiniGameBridge : MonoBehaviour
    {
        public static WeChatMiniGameBridge Instance { get; private set; }

        public event System.Action<bool> OnAdWatched;
        public event System.Action<bool> OnShareCompleted;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // === 广告系统 ===

        /// <summary>
        /// 展示激励视频广告
        /// </summary>
        public void ShowRewardedAd(System.Action<bool> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShowRewardedAdJS();
#else
            Debug.Log("[WeChat] 激励视频广告播放（编辑器模拟）");
            callback?.Invoke(true);
#endif
            OnAdWatched?.Invoke(true);
        }

        /// <summary>
        /// 展示banner广告
        /// </summary>
        public void ShowBannerAd()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShowBannerAdJS();
#else
            Debug.Log("[WeChat] Banner广告显示（编辑器模拟）");
#endif
        }

        public void HideBannerAd()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            HideBannerAdJS();
#endif
        }

        // === 分享功能 ===

        public void ShareToChat(string title, string imageUrl, System.Action<bool> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShareToChatJS(title, imageUrl);
#else
            Debug.Log($"[WeChat] 分享到聊天: {title}");
            callback?.Invoke(true);
#endif
            OnShareCompleted?.Invoke(true);
        }

        // === 模板消息推送 ===

        public void SubscribeMessage(string templateId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SubscribeMessageJS(templateId);
#else
            Debug.Log($"[WeChat] 订阅消息: {templateId}");
#endif
        }

        // === 存档云同步 ===

        public void SaveToCloud(string key, string data)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveToCloudJS(key, data);
#else
            PlayerPrefs.SetString("Cloud_" + key, data);
            PlayerPrefs.Save();
            Debug.Log($"[WeChat] 云存档保存: {key}");
#endif
        }

        public string LoadFromCloud(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return LoadFromCloudJS(key);
#else
            return PlayerPrefs.GetString("Cloud_" + key, "");
#endif
        }

        // === 振动反馈 ===

        public void VibrateShort()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            VibrateShortJS();
#else
            Handheld.Vibrate();
#endif
        }

        public void VibrateLong()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            VibrateLongJS();
#endif
        }

        // === 排行榜 ===

        public void SubmitScore(int score)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SubmitScoreJS(score);
#else
            Debug.Log($"[WeChat] 提交分数: {score}");
#endif
        }

        public void ShowRankList()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShowRankListJS();
#else
            Debug.Log("[WeChat] 显示排行榜");
#endif
        }

        // === JavaScript接口声明 ===

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ShowRewardedAdJS();

        [DllImport("__Internal")]
        private static extern void ShowBannerAdJS();

        [DllImport("__Internal")]
        private static extern void HideBannerAdJS();

        [DllImport("__Internal")]
        private static extern void ShareToChatJS(string title, string imageUrl);

        [DllImport("__Internal")]
        private static extern void SubscribeMessageJS(string templateId);

        [DllImport("__Internal")]
        private static extern void SaveToCloudJS(string key, string data);

        [DllImport("__Internal")]
        private static extern string LoadFromCloudJS(string key);

        [DllImport("__Internal")]
        private static extern void VibrateShortJS();

        [DllImport("__Internal")]
        private static extern void VibrateLongJS();

        [DllImport("__Internal")]
        private static extern void SubmitScoreJS(int score);

        [DllImport("__Internal")]
        private static extern void ShowRankListJS();
#endif
    }

    /// <summary>
    /// 微信广告奖励管理器
    /// </summary>
    public class WeChatAdRewardManager : MonoBehaviour
    {
        public static void WatchAdForReward(string rewardType, int amount)
        {
            WeChatMiniGameBridge.Instance?.ShowRewardedAd((success) =>
            {
                if (!success) return;

                switch (rewardType)
                {
                    case "premium":
                        UnifiedCurrencySystem.Instance?.AddPremium(amount);
                        break;
                    case "cash":
                        UnifiedCurrencySystem.Instance?.AddCash(amount);
                        break;
                    case "goldenKey":
                        UnifiedCurrencySystem.Instance?.AddGoldenKeys(amount);
                        break;
                }

                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.PlaySound(Audio.SoundType.CoinCollect);
                }
            });
        }

        public static void WatchAdToSpeedupProduction()
        {
            WeChatMiniGameBridge.Instance?.ShowRewardedAd((success) =>
            {
                if (!success) return;

                // TODO: 加速当前所有生产
                Debug.Log("生产加速");
            });
        }
    }
}
