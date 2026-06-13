using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PocketCity.Core;
using PocketCity.Simulation;

namespace PocketCity.UI
{
    /// <summary>
    /// 建筑收集图鉴UI
    /// </summary>
    public class BuildingCollectionUI : MonoBehaviour
    {
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private Transform categoryListContainer;
        [SerializeField] private Transform buildingListContainer;
        [SerializeField] private TextMeshProUGUI totalProgressText;
        [SerializeField] private CitySimulationCore simulation;

        private Dictionary<BuildingCategory, List<string>> builtBuildings = new Dictionary<BuildingCategory, List<string>>();

        public void Show()
        {
            if (collectionPanel != null) collectionPanel.SetActive(true);
            RefreshCollection();
        }

        public void Hide()
        {
            if (collectionPanel != null) collectionPanel.SetActive(false);
        }

        private void RefreshCollection()
        {
            UpdateBuildingList();
            DisplayCategories();
            UpdateTotalProgress();
        }

        private void UpdateBuildingList()
        {
            builtBuildings.Clear();

            if (simulation == null) return;

            // 统计已建造的建筑类型
            foreach (var building in simulation.Buildings)
            {
                var def = simulation.Config.GetBuilding(building.ConfigId);
                if (def == null) continue;

                if (!builtBuildings.ContainsKey(def.Category))
                {
                    builtBuildings[def.Category] = new List<string>();
                }

                if (!builtBuildings[def.Category].Contains(building.ConfigId))
                {
                    builtBuildings[def.Category].Add(building.ConfigId);
                }
            }
        }

        private void DisplayCategories()
        {
            if (categoryListContainer == null) return;

            // 清空
            foreach (Transform child in categoryListContainer)
            {
                Destroy(child.gameObject);
            }

            // 显示各类别
            var categories = new[]
            {
                BuildingCategory.Residential,
                BuildingCategory.Commercial,
                BuildingCategory.Industrial,
                BuildingCategory.Service,
                BuildingCategory.Utility,
                BuildingCategory.Decoration
            };

            foreach (var category in categories)
            {
                CreateCategoryButton(category);
            }
        }

        private void CreateCategoryButton(BuildingCategory category)
        {
            var button = TMPUIHelper.CreateButton(
                categoryListContainer,
                category.ToString(),
                GetCategoryDisplayText(category),
                () => ShowCategoryDetails(category)
            );

            // 显示完成度
            int built = GetBuiltCount(category);
            int total = GetTotalCount(category);

            var progressText = TMPUIHelper.CreateText(button.transform, "Progress", $"({built}/{total})", 12);
            progressText.transform.localPosition = new Vector3(0, -20, 0);
        }

        private void ShowCategoryDetails(BuildingCategory category)
        {
            if (buildingListContainer == null) return;

            // 清空
            foreach (Transform child in buildingListContainer)
            {
                Destroy(child.gameObject);
            }

            // 获取该类别所有建筑定义
            var allBuildings = GetAllBuildingsInCategory(category);
            var builtIds = builtBuildings.ContainsKey(category) ? builtBuildings[category] : new List<string>();

            foreach (var buildingId in allBuildings)
            {
                bool isBuilt = builtIds.Contains(buildingId);
                CreateBuildingItem(buildingId, isBuilt);
            }
        }

        private void CreateBuildingItem(string buildingId, bool isBuilt)
        {
            GameObject item = new GameObject($"Building_{buildingId}");
            item.transform.SetParent(buildingListContainer);

            var def = simulation?.Config?.GetBuilding(buildingId);
            string displayName = def != null ? def.Name : buildingId;

            string status = isBuilt ? "✅" : "❌";
            var text = TMPUIHelper.CreateText(item.transform, "Text", $"{status} {displayName}", 14);

            if (!isBuilt)
            {
                text.color = Color.gray;

                // 显示如何获得
                var hintText = TMPUIHelper.CreateText(item.transform, "Hint", GetUnlockHint(buildingId), 10);
                hintText.transform.localPosition = new Vector3(0, -15, 0);
                hintText.color = Color.yellow;
            }
        }

        private string GetUnlockHint(string buildingId)
        {
            // 根据建筑ID返回解锁提示
            if (buildingId.Contains("unique"))
                return "完成成就解锁";
            else if (buildingId.Contains("premium"))
                return "使用高级货币购买";
            else
                return "达到等级要求后解锁";
        }

        private void UpdateTotalProgress()
        {
            if (totalProgressText == null) return;

            int totalBuilt = 0;
            int totalAvailable = 0;

            foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
            {
                totalBuilt += GetBuiltCount(category);
                totalAvailable += GetTotalCount(category);
            }

            totalProgressText.text = $"已收集：{totalBuilt} / {totalAvailable}";
        }

        private int GetBuiltCount(BuildingCategory category)
        {
            return builtBuildings.ContainsKey(category) ? builtBuildings[category].Count : 0;
        }

        private int GetTotalCount(BuildingCategory category)
        {
            return GetAllBuildingsInCategory(category).Count;
        }

        private List<string> GetAllBuildingsInCategory(BuildingCategory category)
        {
            List<string> buildings = new List<string>();

            if (simulation?.Config == null) return buildings;

            // 遍历配置中的所有建筑
            // TODO: 需要CityConfig提供GetAllBuildings()方法
            // 这里简化处理
            switch (category)
            {
                case BuildingCategory.Residential:
                    buildings.AddRange(new[] { "residential_1", "residential_2", "residential_3", "residential_4", "residential_5" });
                    break;
                case BuildingCategory.Commercial:
                    buildings.AddRange(new[] { "commercial_1", "commercial_2", "commercial_3", "shop", "market" });
                    break;
                case BuildingCategory.Industrial:
                    buildings.AddRange(new[] { "industrial_1", "industrial_2", "factory", "warehouse" });
                    break;
                case BuildingCategory.Service:
                    buildings.AddRange(new[] { "fire_station", "police_station", "hospital", "school", "university" });
                    break;
                case BuildingCategory.Utility:
                    buildings.AddRange(new[] { "power_plant", "water_tower", "sewage_plant" });
                    break;
                case BuildingCategory.Decoration:
                    buildings.AddRange(new[] { "park", "fountain", "statue", "tree", "bench" });
                    break;
            }

            return buildings;
        }

        private string GetCategoryDisplayText(BuildingCategory category)
        {
            return category switch
            {
                BuildingCategory.Residential => "🏠 住宅系列",
                BuildingCategory.Commercial => "🏪 商业系列",
                BuildingCategory.Industrial => "🏭 工业系列",
                BuildingCategory.Service => "🏥 服务系列",
                BuildingCategory.Utility => "⚡ 公用设施",
                BuildingCategory.Decoration => "🌳 装饰系列",
                _ => category.ToString()
            };
        }
    }
}
