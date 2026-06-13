using System.Collections.Generic;

namespace PocketCity.Production
{
    /// <summary>
    /// 材料ID映射器 - 解决工厂产品ID与MaterialDatabase不匹配的问题
    /// </summary>
    public static class MaterialIdMapper
    {
        // 旧ID → 新ID映射表
        private static readonly Dictionary<string, string> idMapping = new Dictionary<string, string>
        {
            // Plural → Singular
            {"screws", "screw"},
            {"wires", "wire"},
            {"pipes", "pipe"},
            {"planks", "plank"},
            {"nails", "nails"}, // 保持不变
            {"bricks", "brick"},

            // 特殊名称映射
            {"wood", "plank"},           // wood → plank
            {"wood_log", "log"},         // wood_log → log
            {"wood_plank", "plank"},     // wood_plank → plank
            {"iron_ore", "ore"},         // iron_ore → ore
            {"iron_ingot", "metal_ingot"}, // 保持一致
            {"metal", "metal_ingot"},    // metal → metal_ingot

            // 食品类（暂无对应材料，映射到基础材料）
            {"donuts", "seeds"},         // donut店产品 → seeds
            {"pastries", "seeds"},
            {"bread", "seeds"},
            {"vegetables", "seeds"},
            {"wheat", "seeds"},
            {"flour", "seeds"},

            // 装饰类（暂无对应材料，映射到相关材料）
            {"flowers", "seeds"},
            {"trees", "seeds"},
            {"grass", "seeds"},
            {"chairs", "furniture"},
            {"tables", "furniture"},
            {"cabinets", "furniture"},

            // 已存在的材料（直接映射）
            {"cement", "cement"},
            {"paint", "paint"},
            {"glue", "glue"},
            {"tire", "tire"},
            {"furniture", "furniture"},
            {"lamp", "lamp"},
            {"appliance", "appliance"},
            {"engine", "engine"},
            {"pump", "pump"},
            {"circuit_board", "circuit_board"},
            {"glass", "glass"},
            {"handle", "handle"},
            {"fabric", "fabric"},
            {"cotton", "cotton"},
            {"plastic", "plastic"},
            {"rubber", "rubber"},
            {"seeds", "seeds"},

            // 新增4级材料系统
            {"ore", "ore"},
            {"log", "log"},
            {"crude_oil", "crude_oil"},
            {"metal_ingot", "metal_ingot"},
            {"plank", "plank"},
            {"brick", "brick"},
            {"screw", "screw"},
            {"wire", "wire"},
            {"pipe", "pipe"}
        };

        /// <summary>
        /// 规范化材料ID - 自动映射到MaterialDatabase中存在的ID
        /// </summary>
        public static string NormalizeId(string originalId)
        {
            if (string.IsNullOrEmpty(originalId))
                return originalId;

            // 转小写统一处理
            string lowerId = originalId.ToLower();

            // 查找映射
            if (idMapping.TryGetValue(lowerId, out string mappedId))
            {
                return mappedId;
            }

            // 未映射的保持原样
            return lowerId;
        }

        /// <summary>
        /// 批量规范化材料ID列表
        /// </summary>
        public static List<string> NormalizeIds(List<string> originalIds)
        {
            var result = new List<string>();
            foreach (var id in originalIds)
            {
                result.Add(NormalizeId(id));
            }
            return result;
        }

        /// <summary>
        /// 规范化材料ID数组
        /// </summary>
        public static string[] NormalizeIds(string[] originalIds)
        {
            var result = new string[originalIds.Length];
            for (int i = 0; i < originalIds.Length; i++)
            {
                result[i] = NormalizeId(originalIds[i]);
            }
            return result;
        }

        /// <summary>
        /// 检查ID是否需要映射
        /// </summary>
        public static bool NeedsMapping(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            string lowerId = id.ToLower();
            return idMapping.ContainsKey(lowerId) && idMapping[lowerId] != lowerId;
        }

        /// <summary>
        /// 添加自定义映射
        /// </summary>
        public static void AddMapping(string fromId, string toId)
        {
            idMapping[fromId.ToLower()] = toId.ToLower();
        }

        /// <summary>
        /// 获取所有映射（调试用）
        /// </summary>
        public static Dictionary<string, string> GetAllMappings()
        {
            return new Dictionary<string, string>(idMapping);
        }
    }
}
