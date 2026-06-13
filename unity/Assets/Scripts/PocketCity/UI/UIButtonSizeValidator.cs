using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PocketCity.UI
{
    /// <summary>
    /// UI按钮大小验证和自动修复系统
    /// 确保所有按钮至少48x48像素（移动端触控标准）
    /// </summary>
    public class UIButtonSizeValidator : MonoBehaviour
    {
        [Header("Standards")]
        [SerializeField] private float minimumButtonSize = 48f; // 最小按钮尺寸（像素）
        [SerializeField] private float recommendedButtonSize = 56f; // 推荐尺寸
        [SerializeField] private float minimumSpacing = 8f; // 最小间距

        [Header("Auto Fix")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool showWarnings = true;

        private List<Button> undersizedButtons = new List<Button>();

        private void Start()
        {
            if (autoFixOnStart)
            {
                ValidateAndFixAllButtons();
            }
        }

        /// <summary>
        /// 验证并修复所有按钮尺寸
        /// </summary>
        public void ValidateAndFixAllButtons()
        {
            undersizedButtons.Clear();

            // 查找场景中所有按钮
            Button[] allButtons = FindObjectsOfType<Button>(true);

            foreach (var button in allButtons)
            {
                ValidateButton(button);
            }

            if (showWarnings && undersizedButtons.Count > 0)
            {
                Debug.LogWarning($"发现 {undersizedButtons.Count} 个尺寸不合规的按钮，已自动修复");
            }
            else if (undersizedButtons.Count == 0)
            {
                Debug.Log($"✅ 所有 {allButtons.Length} 个按钮尺寸合规");
            }
        }

        private void ValidateButton(Button button)
        {
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            Rect rect = rectTransform.rect;
            bool needsFix = false;

            // 检查宽度
            if (rect.width < minimumButtonSize)
            {
                needsFix = true;
                if (showWarnings)
                {
                    Debug.LogWarning($"按钮 {button.gameObject.name} 宽度 {rect.width:F1}px < {minimumButtonSize}px");
                }
            }

            // 检查高度
            if (rect.height < minimumButtonSize)
            {
                needsFix = true;
                if (showWarnings)
                {
                    Debug.LogWarning($"按钮 {button.gameObject.name} 高度 {rect.height:F1}px < {minimumButtonSize}px");
                }
            }

            if (needsFix)
            {
                undersizedButtons.Add(button);
                FixButtonSize(button, rectTransform);
            }
        }

        private void FixButtonSize(Button button, RectTransform rectTransform)
        {
            Rect rect = rectTransform.rect;

            // 计算新尺寸
            float newWidth = Mathf.Max(rect.width, recommendedButtonSize);
            float newHeight = Mathf.Max(rect.height, recommendedButtonSize);

            // 应用新尺寸
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

            Debug.Log($"✅ 修复按钮 {button.gameObject.name}: {rect.width:F1}x{rect.height:F1} → {newWidth:F1}x{newHeight:F1}");
        }

        /// <summary>
        /// 创建符合规范的按钮
        /// </summary>
        public static GameObject CreateStandardButton(Transform parent, string name, string text)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent);

            // 添加RectTransform
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120f, 56f); // 推荐尺寸

            // 添加Image
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 1f, 1f);

            // 添加Button
            Button button = buttonObj.AddComponent<Button>();

            // 添加文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var textComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.alignment = TMPro.TextAlignmentOptions.Center;
            textComponent.fontSize = 18;
            textComponent.color = Color.white;

            return buttonObj;
        }

        /// <summary>
        /// 检查按钮间距
        /// </summary>
        public void ValidateButtonSpacing(Transform container)
        {
            Button[] buttons = container.GetComponentsInChildren<Button>();
            if (buttons.Length < 2) return;

            for (int i = 0; i < buttons.Length - 1; i++)
            {
                RectTransform rect1 = buttons[i].GetComponent<RectTransform>();
                RectTransform rect2 = buttons[i + 1].GetComponent<RectTransform>();

                float distance = Vector2.Distance(rect1.position, rect2.position);
                float minDistance = (rect1.rect.width + rect2.rect.width) / 2f + minimumSpacing;

                if (distance < minDistance)
                {
                    Debug.LogWarning($"按钮间距过小：{buttons[i].name} ↔ {buttons[i + 1].name} ({distance:F1}px < {minDistance:F1}px)");
                }
            }
        }

        /// <summary>
        /// 生成按钮尺寸报告
        /// </summary>
        public string GenerateReport()
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);
            int compliantCount = 0;
            int undersizedCount = 0;
            int oversizedCount = 0;

            foreach (var button in allButtons)
            {
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform == null) continue;

                Rect rect = rectTransform.rect;
                float minDimension = Mathf.Min(rect.width, rect.height);

                if (minDimension < minimumButtonSize)
                    undersizedCount++;
                else if (minDimension > recommendedButtonSize * 2)
                    oversizedCount++;
                else
                    compliantCount++;
            }

            return $"UI按钮尺寸报告：\n" +
                   $"总数：{allButtons.Length}\n" +
                   $"✅ 合规：{compliantCount} ({compliantCount * 100f / allButtons.Length:F1}%)\n" +
                   $"⚠️ 过小：{undersizedCount}\n" +
                   $"⚠️ 过大：{oversizedCount}\n" +
                   $"标准：≥{minimumButtonSize}px, 推荐：{recommendedButtonSize}px";
        }

#if UNITY_EDITOR
        [ContextMenu("验证所有按钮")]
        private void ValidateInEditor()
        {
            ValidateAndFixAllButtons();
            Debug.Log(GenerateReport());
        }
#endif
    }

    /// <summary>
    /// 按钮尺寸辅助组件 - 附加到按钮上自动验证
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSizeHelper : MonoBehaviour
    {
        [SerializeField] private float minimumSize = 48f;
        [SerializeField] private bool autoFix = true;

        private void Start()
        {
            if (autoFix)
            {
                ValidateSize();
            }
        }

        private void ValidateSize()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            Rect rect = rectTransform.rect;
            bool needsFix = false;

            if (rect.width < minimumSize || rect.height < minimumSize)
            {
                needsFix = true;
            }

            if (needsFix)
            {
                float newWidth = Mathf.Max(rect.width, minimumSize);
                float newHeight = Mathf.Max(rect.height, minimumSize);

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

                Debug.Log($"✅ 自动修复按钮尺寸：{gameObject.name}");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // 绘制最小尺寸参考框
            Gizmos.color = Color.yellow;
            Vector3 center = rectTransform.position;
            Vector3 size = new Vector3(minimumSize, minimumSize, 0f);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
