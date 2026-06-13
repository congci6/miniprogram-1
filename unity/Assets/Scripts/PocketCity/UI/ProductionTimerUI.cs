using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PocketCity.Production;

namespace PocketCity.UI
{
    /// <summary>
    /// 生产倒计时UI - 显示工厂生产进度
    /// </summary>
    public class ProductionTimerUI : MonoBehaviour
    {
        [SerializeField] private Transform factoryListContainer;
        [SerializeField] private GameObject factoryPanelPrefab;
        [SerializeField] private ProductionChainSystem productionSystem;

        private void Start()
        {
            RefreshFactoryPanels();
        }

        private void Update()
        {
            UpdateAllTimers();
        }

        private void RefreshFactoryPanels()
        {
            if (factoryListContainer == null || productionSystem == null) return;

            // 清空现有面板
            foreach (Transform child in factoryListContainer)
            {
                Destroy(child.gameObject);
            }

            // 为每个工厂类型创建面板
            var factories = productionSystem.GetAllFactories();
            foreach (var kvp in factories)
            {
                CreateFactoryPanel(kvp.Key, kvp.Value);
            }
        }

        private void CreateFactoryPanel(FactoryType type, Factory factory)
        {
            GameObject panel = factoryPanelPrefab != null
                ? Instantiate(factoryPanelPrefab, factoryListContainer)
                : new GameObject($"{type}_Panel");

            panel.transform.SetParent(factoryListContainer);

            // 添加组件
            var uiData = panel.AddComponent<FactoryPanelUI>();
            uiData.factoryType = type;
            uiData.factory = factory;

            // 创建UI元素
            var title = TMPUIHelper.CreateText(panel.transform, "Title", GetFactoryName(type), 18);
            title.transform.localPosition = new Vector3(0, 50, 0);

            // 槽位状态
            for (int i = 0; i < factory.maxSlots; i++)
            {
                var slotPanel = new GameObject($"Slot_{i}");
                slotPanel.transform.SetParent(panel.transform);
                slotPanel.transform.localPosition = new Vector3(0, 20 - i * 25, 0);

                var slotData = slotPanel.AddComponent<SlotUI>();
                slotData.slotIndex = i;
                slotData.parentFactory = uiData;

                var slotText = TMPUIHelper.CreateText(slotPanel.transform, "Text", "", 14);
                slotData.slotText = slotText;
            }
        }

        private void UpdateAllTimers()
        {
            var panels = factoryListContainer.GetComponentsInChildren<FactoryPanelUI>();
            foreach (var panel in panels)
            {
                UpdateFactoryPanel(panel);
            }
        }

        private void UpdateFactoryPanel(FactoryPanelUI panelUI)
        {
            var factory = panelUI.factory;
            if (factory == null) return;

            var slots = panelUI.GetComponentsInChildren<SlotUI>();
            for (int i = 0; i < slots.Length; i++)
            {
                var slotUI = slots[i];
                if (i < factory.slots.Count)
                {
                    var slot = factory.slots[i];
                    UpdateSlotUI(slotUI, slot);
                }
                else
                {
                    slotUI.slotText.text = "空闲";
                    slotUI.slotText.color = Color.gray;
                }
            }
        }

        private void UpdateSlotUI(SlotUI slotUI, ProductionSlot slot)
        {
            if (slot.isCompleted)
            {
                slotUI.slotText.text = $"✅ {slot.material.name} - 点击收取";
                slotUI.slotText.color = Color.green;
            }
            else
            {
                float remaining = slot.duration - slot.elapsedTime;
                string timeStr = FormatTime(remaining);
                float progress = slot.elapsedTime / slot.duration * 100f;

                slotUI.slotText.text = $"⏳ {slot.material.name} - {timeStr} ({progress:F0}%)";
                slotUI.slotText.color = Color.yellow;
            }
        }

        private string FormatTime(float seconds)
        {
            if (seconds < 60)
                return $"{Mathf.CeilToInt(seconds)}秒";
            else if (seconds < 3600)
                return $"{Mathf.CeilToInt(seconds / 60)}分钟";
            else
                return $"{Mathf.CeilToInt(seconds / 3600)}小时";
        }

        private string GetFactoryName(FactoryType type)
        {
            return type switch
            {
                FactoryType.BuildingSupplies => "🏗️ 建材厂",
                FactoryType.Hardware => "🔧 五金店",
                FactoryType.Farming => "🌾 农贸市场",
                FactoryType.Furniture => "🪑 家具厂",
                FactoryType.Gardening => "🌻 园艺店",
                FactoryType.DonutShop => "🍩 甜甜圈店",
                _ => type.ToString()
            };
        }
    }

    public class FactoryPanelUI : MonoBehaviour
    {
        public FactoryType factoryType;
        public Factory factory;
    }

    public class SlotUI : MonoBehaviour
    {
        public int slotIndex;
        public FactoryPanelUI parentFactory;
        public TextMeshProUGUI slotText;
    }
}
