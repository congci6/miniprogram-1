using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace PocketCity.AI
{
    /// <summary>
    /// AI图像生成配置
    /// </summary>
    [CreateAssetMenu(fileName = "AIImageConfig", menuName = "PocketCity/AI Image Config")]
    public class AIImageConfig : ScriptableObject
    {
        [Header("API配置 - 请勿提交到Git")]
        [SerializeField] private string baseUrl = "https://wisart.klsf.cc/v1";
        [SerializeField] private string apiKey = ""; // 从环境变量或外部文件读取
        [SerializeField] private string model = "gpt-image-2";

        [Header("生成设置")]
        [SerializeField] private int defaultWidth = 512;
        [SerializeField] private int defaultHeight = 512;
        [SerializeField] private string defaultStyle = "game-icon";

        public string BaseUrl => baseUrl;
        public string ApiKey => apiKey;
        public string Model => model;
        public int DefaultWidth => defaultWidth;
        public int DefaultHeight => defaultHeight;
        public string DefaultStyle => defaultStyle;

        // 从环境变量加载API密钥（更安全）
        public string GetApiKey()
        {
            string envKey = Environment.GetEnvironmentVariable("WISART_API_KEY");
            return !string.IsNullOrEmpty(envKey) ? envKey : apiKey;
        }
    }

    /// <summary>
    /// AI图像生成请求
    /// </summary>
    [Serializable]
    public class ImageGenerationRequest
    {
        public string model;
        public string prompt;
        public int n = 1;
        public string size = "512x512";
        public string response_format = "url";
    }

    /// <summary>
    /// AI图像生成响应
    /// </summary>
    [Serializable]
    public class ImageGenerationResponse
    {
        public long created;
        public ImageData[] data;

        [Serializable]
        public class ImageData
        {
            public string url;
            public string b64_json;
        }
    }

    /// <summary>
    /// AI图像生成器 - 用于生成游戏UI和资源
    /// </summary>
    public class AIImageGenerator : MonoBehaviour
    {
        public static AIImageGenerator Instance { get; private set; }

        [SerializeField] private AIImageConfig config;

        public event Action<Texture2D> OnImageGenerated;
        public event Action<string> OnGenerationFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 生成图像
        /// </summary>
        public void GenerateImage(string prompt, Action<Texture2D> onComplete, Action<string> onError = null)
        {
            StartCoroutine(GenerateImageCoroutine(prompt, onComplete, onError));
        }

        private IEnumerator GenerateImageCoroutine(string prompt, Action<Texture2D> onComplete, Action<string> onError)
        {
            if (config == null)
            {
                string error = "AIImageConfig未设置！";
                Debug.LogError(error);
                onError?.Invoke(error);
                OnGenerationFailed?.Invoke(error);
                yield break;
            }

            string apiKey = config.GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                string error = "API密钥未设置！请在AIImageConfig中设置或设置环境变量WISART_API_KEY";
                Debug.LogError(error);
                onError?.Invoke(error);
                OnGenerationFailed?.Invoke(error);
                yield break;
            }

            // 构建请求
            var request = new ImageGenerationRequest
            {
                model = config.Model,
                prompt = prompt,
                n = 1,
                size = $"{config.DefaultWidth}x{config.DefaultHeight}",
                response_format = "url"
            };

            string jsonData = JsonUtility.ToJson(request);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // 发送请求
            string url = $"{config.BaseUrl}/images/generations";
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                Debug.Log($"🎨 正在生成图像: {prompt}");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    string error = $"图像生成失败: {www.error}\n响应: {www.downloadHandler.text}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                    OnGenerationFailed?.Invoke(error);
                }
                else
                {
                    // 解析响应
                    try
                    {
                        ImageGenerationResponse response = JsonUtility.FromJson<ImageGenerationResponse>(www.downloadHandler.text);

                        if (response.data != null && response.data.Length > 0)
                        {
                            string imageUrl = response.data[0].url;
                            Debug.Log($"✅ 图像URL获取成功: {imageUrl}");

                            // 下载图像
                            yield return StartCoroutine(DownloadImageCoroutine(imageUrl, onComplete, onError));
                        }
                        else
                        {
                            string error = "响应中没有图像数据";
                            Debug.LogError(error);
                            onError?.Invoke(error);
                            OnGenerationFailed?.Invoke(error);
                        }
                    }
                    catch (Exception e)
                    {
                        string error = $"解析响应失败: {e.Message}\n响应内容: {www.downloadHandler.text}";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                        OnGenerationFailed?.Invoke(error);
                    }
                }
            }
        }

        private IEnumerator DownloadImageCoroutine(string url, Action<Texture2D> onComplete, Action<string> onError)
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    string error = $"下载图像失败: {www.error}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                    OnGenerationFailed?.Invoke(error);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    Debug.Log($"✅ 图像下载成功: {texture.width}x{texture.height}");

                    onComplete?.Invoke(texture);
                    OnImageGenerated?.Invoke(texture);
                }
            }
        }

        /// <summary>
        /// 生成建筑图标
        /// </summary>
        public void GenerateBuildingIcon(string buildingName, string buildingType, Action<Texture2D> onComplete)
        {
            string prompt = $"game icon, {buildingType} building, {buildingName}, isometric view, simple, clean, game art style, white background";
            GenerateImage(prompt, onComplete);
        }

        /// <summary>
        /// 生成UI按钮
        /// </summary>
        public void GenerateUIButton(string buttonName, string description, Action<Texture2D> onComplete)
        {
            string prompt = $"game UI button, {buttonName}, {description}, simple icon, flat design, clean, white background";
            GenerateImage(prompt, onComplete);
        }

        /// <summary>
        /// 生成材料图标
        /// </summary>
        public void GenerateMaterialIcon(string materialName, string materialType, Action<Texture2D> onComplete)
        {
            string prompt = $"game item icon, {materialName}, {materialType}, clean, simple, game art style, white background";
            GenerateImage(prompt, onComplete);
        }
    }
}
