using System.Collections.Generic;
using UnityEngine;
using PocketCity.Production;

namespace PocketCity.Production
{
    /// <summary>
    /// 扩展24+种材料的4级加工链
    /// </summary>
    [CreateAssetMenu(fileName = "ExtendedMaterialDatabase", menuName = "PocketCity/Extended Material Database")]
    public class ExtendedMaterialDatabase : ScriptableObject
    {
        public List<MaterialData> materials = new List<MaterialData>();

        public void InitializeExtendedMaterials()
        {
            materials.Clear();

            // === Tier 1: Basic (基础材料) ===
            AddMaterial("iron_ore", "铁矿石", MaterialTier.Basic, 30, 10);
            AddMaterial("wood_log", "原木", MaterialTier.Basic, 30, 10);
            AddMaterial("crude_oil", "原油", MaterialTier.Basic, 45, 15);
            AddMaterial("cotton", "棉花", MaterialTier.Basic, 30, 10);
            AddMaterial("sand", "沙石", MaterialTier.Basic, 30, 10);
            AddMaterial("clay", "黏土", MaterialTier.Basic, 30, 10);

            // === Tier 2: Refined (精炼材料) ===
            AddMaterialWithRecipe("iron_ingot", "铁锭", MaterialTier.Processed, 120, 30,
                new Recipe { materialId = "iron_ore", amount = 2 });

            AddMaterialWithRecipe("wood_plank", "木板", MaterialTier.Processed, 120, 30,
                new Recipe { materialId = "wood_log", amount = 2 });

            AddMaterialWithRecipe("plastic", "塑料粒", MaterialTier.Processed, 180, 40,
                new Recipe { materialId = "crude_oil", amount = 2 });

            AddMaterialWithRecipe("fabric", "布匹", MaterialTier.Processed, 120, 30,
                new Recipe { materialId = "cotton", amount = 3 });

            AddMaterialWithRecipe("glass", "玻璃", MaterialTier.Processed, 120, 30,
                new Recipe { materialId = "sand", amount = 2 });

            AddMaterialWithRecipe("brick", "砖块", MaterialTier.Processed, 120, 30,
                new Recipe { materialId = "clay", amount = 2 });

            // === Tier 3: Component (组件) ===
            AddMaterialWithRecipe("nails", "钉子", MaterialTier.Processed, 600, 50,
                new Recipe { materialId = "iron_ingot", amount = 1 },
                new Recipe { materialId = "wood_plank", amount = 1 });

            AddMaterialWithRecipe("gears", "齿轮", MaterialTier.Processed, 600, 60,
                new Recipe { materialId = "iron_ingot", amount = 2 });

            AddMaterialWithRecipe("wires", "电线", MaterialTier.Processed, 600, 50,
                new Recipe { materialId = "iron_ingot", amount = 1 },
                new Recipe { materialId = "plastic", amount = 1 });

            AddMaterialWithRecipe("pipes", "管道", MaterialTier.Processed, 600, 55,
                new Recipe { materialId = "iron_ingot", amount = 2 });

            AddMaterialWithRecipe("paint", "油漆", MaterialTier.Processed, 600, 45,
                new Recipe { materialId = "plastic", amount = 1 });

            AddMaterialWithRecipe("screws", "螺丝", MaterialTier.Processed, 600, 50,
                new Recipe { materialId = "iron_ingot", amount = 1 });

            AddMaterialWithRecipe("tires", "轮胎", MaterialTier.Processed, 600, 60,
                new Recipe { materialId = "plastic", amount = 2 });

            AddMaterialWithRecipe("cement", "水泥", MaterialTier.Processed, 600, 55,
                new Recipe { materialId = "sand", amount = 2 },
                new Recipe { materialId = "clay", amount = 1 });

            // === Tier 4: Finished (成品) ===
            AddMaterialWithRecipe("engine", "引擎", MaterialTier.Advanced, 3600, 200,
                new Recipe { materialId = "gears", amount = 2 },
                new Recipe { materialId = "screws", amount = 3 },
                new Recipe { materialId = "wires", amount = 1 });

            AddMaterialWithRecipe("furniture", "家具", MaterialTier.Advanced, 3600, 180,
                new Recipe { materialId = "wood_plank", amount = 3 },
                new Recipe { materialId = "nails", amount = 2 },
                new Recipe { materialId = "paint", amount = 1 });

            AddMaterialWithRecipe("appliances", "电器", MaterialTier.Advanced, 3600, 220,
                new Recipe { materialId = "wires", amount = 2 },
                new Recipe { materialId = "plastic", amount = 2 },
                new Recipe { materialId = "screws", amount = 2 });

            AddMaterialWithRecipe("lighting", "照明", MaterialTier.Advanced, 3600, 160,
                new Recipe { materialId = "wires", amount = 1 },
                new Recipe { materialId = "glass", amount = 2 });

            AddMaterialWithRecipe("bathroom", "卫浴", MaterialTier.Advanced, 3600, 190,
                new Recipe { materialId = "pipes", amount = 2 },
                new Recipe { materialId = "glass", amount = 1 },
                new Recipe { materialId = "cement", amount = 1 });

            AddMaterialWithRecipe("windows", "门窗", MaterialTier.Advanced, 3600, 170,
                new Recipe { materialId = "glass", amount = 2 },
                new Recipe { materialId = "wood_plank", amount = 2 });
        }

        private void AddMaterial(string id, string name, MaterialTier tier, float productionTime, int basePrice)
        {
            materials.Add(new MaterialData
            {
                id = id,
                name = name,
                tier = tier,
                productionTime = productionTime,
                basePrice = basePrice,
                recipe = new List<Recipe>()
            });
        }

        private void AddMaterialWithRecipe(string id, string name, MaterialTier tier, float productionTime, int basePrice, params Recipe[] recipes)
        {
            materials.Add(new MaterialData
            {
                id = id,
                name = name,
                tier = tier,
                productionTime = productionTime,
                basePrice = basePrice,
                recipe = new List<Recipe>(recipes)
            });
        }

        public MaterialData GetMaterial(string id)
        {
            return materials.Find(m => m.id == id);
        }
    }
}
