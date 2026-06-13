using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCity.Production
{
    public enum MaterialTier
    {
        Basic,      // 基础原料
        Raw,        // 初加工
        Processed,  // 精加工
        Advanced    // 高级成品
    }

    public enum MaterialQuality
    {
        Common,
        Uncommon,
        Rare
    }

    [Serializable]
    public class Recipe
    {
        public string materialId;
        public int amount;
    }

    [Serializable]
    public class MaterialData
    {
        public string id;
        public string name;
        public MaterialTier tier;
        public float productionTime;
        public List<Recipe> recipe = new List<Recipe>();
        public int basePrice;
        public int baseValue { get { return basePrice; } set { basePrice = value; } }
        public Sprite icon;
        public float rareChance = 0.05f; // 5%概率产出稀有品质
    }

    [CreateAssetMenu(fileName = "MaterialDatabase", menuName = "PocketCity/Material Database")]
    public class MaterialDatabase : ScriptableObject
    {
        public List<MaterialData> materials = new List<MaterialData>();

        private Dictionary<string, MaterialData> materialDict;

        private void OnEnable()
        {
            materialDict = new Dictionary<string, MaterialData>();
            foreach (var material in materials)
            {
                if (material != null && !string.IsNullOrEmpty(material.id))
                {
                    materialDict[material.id] = material;
                }
            }
        }

        public MaterialData GetMaterial(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (materialDict == null) OnEnable();
            return materialDict.TryGetValue(id, out var mat) ? mat : null;
        }

        public List<MaterialData> GetMaterialsByTier(MaterialTier tier)
        {
            return materials.Where(m => m != null && m.tier == tier).ToList();
        }

        [ContextMenu("Initialize 4-Tier Materials")]
        public void InitializeEnhancedMaterials()
        {
            materials.Clear();

            // Tier 0: Basic (基础原料，无配方)
            AddBasic("ore", "矿石", 10f, 5);
            AddBasic("log", "原木", 15f, 6);
            AddBasic("crude_oil", "原油", 20f, 8);
            AddBasic("cotton", "棉花", 12f, 4);
            AddBasic("seeds", "种子", 8f, 3); // 新增

            // Tier 1: Raw (初加工，10-30s)
            AddRaw("metal_ingot", "金属锭", 30f, 15, "ore", 2);
            AddRaw("metal", "金属", 28f, 14, "ore", 2); // 新增（metal别名）
            AddRaw("plank", "木板", 25f, 12, "log", 2);
            AddRaw("plastic", "塑料颗粒", 40f, 18, "crude_oil", 1);
            AddRaw("fabric", "布料", 20f, 10, "cotton", 3);
            AddRaw("rubber", "橡胶", 35f, 16, "crude_oil", 1);
            AddRaw("glass", "玻璃", 30f, 14, "ore", 1);
            AddRaw("brick", "砖块", 32f, 16, "ore", 2); // 新增

            // Tier 2: Processed (精加工，60-180s)
            AddProcessed("nails", "钉子", 60f, 35, new[] { ("metal_ingot", 1) });
            AddProcessed("wire", "电线", 70f, 38, new[] { ("metal_ingot", 1) });
            AddProcessed("pipe", "管道", 80f, 42, new[] { ("metal_ingot", 2) });
            AddProcessed("tire", "轮胎", 120f, 55, new[] { ("rubber", 2), ("fabric", 1) });
            AddProcessed("paint", "油漆", 90f, 45, new[] { ("crude_oil", 1), ("plastic", 1) });
            AddProcessed("screw", "螺丝", 65f, 36, new[] { ("metal_ingot", 1) });
            AddProcessed("handle", "把手", 75f, 40, new[] { ("plastic", 1), ("metal_ingot", 1) });
            AddProcessed("cement", "水泥", 100f, 48, new[] { ("ore", 2) });
            AddProcessed("glue", "胶水", 55f, 32, new[] { ("plastic", 1) }); // 新增

            // Tier 3: Advanced (高级成品，300-1800s)
            AddAdvanced("engine", "引擎", 600f, 180, new[] { ("pipe", 3), ("wire", 2), ("screw", 4) });
            AddAdvanced("pump", "水泵", 480f, 150, new[] { ("pipe", 2), ("engine", 1) });
            AddAdvanced("circuit_board", "电路板", 720f, 200, new[] { ("wire", 4), ("plastic", 2), ("glass", 1) });
            AddAdvanced("furniture", "家具", 540f, 160, new[] { ("plank", 4), ("screw", 3), ("paint", 2) });
            AddAdvanced("lamp", "灯具", 420f, 140, new[] { ("wire", 2), ("glass", 2), ("metal_ingot", 1) });
            AddAdvanced("appliance", "家电", 900f, 250, new[] { ("circuit_board", 1), ("metal_ingot", 3), ("plastic", 2) });

            // 食品类（特殊）
            AddAdvanced("donut", "甜甜圈", 180f, 80, new[] { ("seeds", 2) }); // 新增
            AddAdvanced("bread", "面包", 150f, 70, new[] { ("seeds", 3) }); // 新增
            AddAdvanced("pastry", "糕点", 240f, 95, new[] { ("seeds", 2), ("fabric", 1) }); // 新增

            OnEnable();
        }

        private void AddBasic(string id, string name, float time, int price)
        {
            materials.Add(new MaterialData
            {
                id = id,
                name = name,
                tier = MaterialTier.Basic,
                productionTime = time,
                basePrice = price
            });
        }

        private void AddRaw(string id, string name, float time, int price, string inputId, int inputAmount)
        {
            materials.Add(new MaterialData
            {
                id = id,
                name = name,
                tier = MaterialTier.Raw,
                productionTime = time,
                basePrice = price,
                recipe = new List<Recipe> { new Recipe { materialId = inputId, amount = inputAmount } }
            });
        }

        private void AddProcessed(string id, string name, float time, int price, (string id, int amount)[] inputs)
        {
            var recipe = new List<Recipe>();
            foreach (var input in inputs)
            {
                recipe.Add(new Recipe { materialId = input.id, amount = input.amount });
            }

            materials.Add(new MaterialData
            {
                id = id,
                name = name,
                tier = MaterialTier.Processed,
                productionTime = time,
                basePrice = price,
                recipe = recipe
            });
        }

        private void AddAdvanced(string id, string name, float time, int price, (string id, int amount)[] inputs)
        {
            var recipe = new List<Recipe>();
            foreach (var input in inputs)
            {
                recipe.Add(new Recipe { materialId = input.id, amount = input.amount });
            }

            materials.Add(new MaterialData
            {
                id = id,
                name = name,
                tier = MaterialTier.Advanced,
                productionTime = time,
                basePrice = price,
                recipe = recipe,
                rareChance = 0.1f // 高级材料更高的稀有概率
            });
        }
    }
}
