using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PocketCity.UI
{
    /// <summary>
    /// TMP UI创建辅助工具
    /// </summary>
    public static class TMPUIHelper
    {
        /// <summary>
        /// 创建TMP文本（替代Unity.UI.Text）
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize = 24)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.fontStyle = FontStyles.Normal;

            // 自动添加RectTransform
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);

            return tmp;
        }

        /// <summary>
        /// 创建带轮廓的文本
        /// </summary>
        public static TextMeshProUGUI CreateTextWithOutline(Transform parent, string name, string text, int fontSize = 24, Color outlineColor = default)
        {
            TextMeshProUGUI tmp = CreateText(parent, name, text, fontSize);

            if (outlineColor == default)
                outlineColor = Color.black;

            tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = outlineColor;

            return tmp;
        }

        /// <summary>
        /// 创建按钮
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string text, System.Action onClick = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 60);

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f);

            Button btn = go.AddComponent<Button>();
            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            // 添加文本
            TextMeshProUGUI tmp = CreateText(go.transform, "Text", text, 20);
            tmp.rectTransform.anchorMin = Vector2.zero;
            tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.sizeDelta = Vector2.zero;

            return btn;
        }

        /// <summary>
        /// 创建图标按钮
        /// </summary>
        public static Button CreateIconButton(Transform parent, string name, Sprite icon, System.Action onClick = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);

            Image img = go.AddComponent<Image>();
            img.sprite = icon;
            img.color = Color.white;

            Button btn = go.AddComponent<Button>();
            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        /// <summary>
        /// 创建数值标签（货币显示）
        /// </summary>
        public static GameObject CreateCurrencyLabel(Transform parent, string name, string labelText, int value, Sprite icon = null)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);

            RectTransform rt = container.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // 图标
            if (icon != null)
            {
                GameObject iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(container.transform, false);

                Image iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = icon;

                RectTransform iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.sizeDelta = new Vector2(30, 30);
            }

            // 标签
            TextMeshProUGUI label = CreateText(container.transform, "Label", labelText, 18);
            label.rectTransform.sizeDelta = new Vector2(80, 30);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // 数值
            TextMeshProUGUI valueText = CreateText(container.transform, "Value", value.ToString(), 22);
            valueText.rectTransform.sizeDelta = new Vector2(90, 30);
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.fontStyle = FontStyles.Bold;
            valueText.color = Color.yellow;

            return container;
        }

        /// <summary>
        /// 更新数值标签
        /// </summary>
        public static void UpdateCurrencyLabel(GameObject label, int newValue)
        {
            TextMeshProUGUI valueText = label.transform.Find("Value")?.GetComponent<TextMeshProUGUI>();
            if (valueText != null)
                valueText.text = newValue.ToString();
        }
    }

    /// <summary>
    /// 图标资源管理器
    /// </summary>
    public class IconAtlas : MonoBehaviour
    {
        public static IconAtlas Instance { get; private set; }

        [Header("Currency Icons")]
        public Sprite cashIcon;
        public Sprite premiumIcon;
        public Sprite goldenKeyIcon;

        [Header("Building Category Icons")]
        public Sprite residentialIcon;
        public Sprite commercialIcon;
        public Sprite industrialIcon;
        public Sprite serviceIcon;

        [Header("Overlay Icons")]
        public Sprite trafficIcon;
        public Sprite pollutionIcon;
        public Sprite zoneIcon;
        public Sprite serviceOverlayIcon;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public Sprite GetIcon(string iconName)
        {
            return iconName switch
            {
                "cash" => cashIcon,
                "premium" => premiumIcon,
                "goldenKey" => goldenKeyIcon,
                "residential" => residentialIcon,
                "commercial" => commercialIcon,
                "industrial" => industrialIcon,
                "service" => serviceIcon,
                "traffic" => trafficIcon,
                "pollution" => pollutionIcon,
                "zone" => zoneIcon,
                _ => null
            };
        }
    }
}
