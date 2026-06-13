using System.Collections.Generic;
using PocketCity.Production;
using UnityEngine;

namespace PocketCity.Materials
{
    /// <summary>
    /// 统一存储桥接器 - 整合UpgradeMaterialSystem和StorageSystem
    /// </summary>
    public class UnifiedStorageBridge : MonoBehaviour
    {
        public static UnifiedStorageBridge Instance { get; private set; }

        [SerializeField] private StorageSystem storageSystem;
        [SerializeField] private MaterialDatabase materialDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 统一的材料访问接口
        public bool AddMaterial(string id, int amount)
        {
            if (storageSystem == null) return false;
            return storageSystem.AddItem(id, amount);
        }

        public bool ConsumeMaterial(string id, int amount)
        {
            if (storageSystem == null) return false;
            return storageSystem.RemoveItem(id, amount);
        }

        public bool HasMaterial(string id, int amount)
        {
            if (storageSystem == null) return false;
            return storageSystem.GetItemAmount(id) >= amount;
        }

        public int GetMaterialCount(string id)
        {
            if (storageSystem == null) return 0;
            return storageSystem.GetItemAmount(id);
        }

        // 建筑升级需求
        public Dictionary<string, int> GetBuildingRequirements(string buildingType, int level)
        {
            var reqs = new Dictionary<string, int>();
            if (level < 2 || level > 5) return reqs;

            switch (buildingType)
            {
                case "residential":
                    if (level == 2) { reqs["nails"] = 2; reqs["plank"] = 2; }
                    else if (level == 3) { reqs["cement"] = 3; reqs["pipe"] = 2; }
                    else if (level == 4) { reqs["paint"] = 4; reqs["furniture"] = 1; }
                    else if (level == 5) { reqs["appliance"] = 1; reqs["lamp"] = 2; reqs["furniture"] = 2; }
                    break;

                case "commercial":
                case "office":
                    if (level == 2) { reqs["nails"] = 3; reqs["plank"] = 2; reqs["paint"] = 1; }
                    else if (level == 3) { reqs["cement"] = 4; reqs["wire"] = 3; }
                    else if (level == 4) { reqs["furniture"] = 2; reqs["lamp"] = 3; }
                    else if (level == 5) { reqs["appliance"] = 2; reqs["circuit_board"] = 1; }
                    break;

                case "industrial":
                    if (level == 2) { reqs["cement"] = 2; reqs["pipe"] = 1; }
                    else if (level == 3) { reqs["cement"] = 5; reqs["pipe"] = 3; reqs["tire"] = 1; }
                    else if (level == 4) { reqs["engine"] = 1; reqs["pump"] = 1; }
                    else if (level == 5) { reqs["engine"] = 2; reqs["pump"] = 2; reqs["circuit_board"] = 1; }
                    break;

                default:
                    if (level == 2) { reqs["nails"] = 2; reqs["plank"] = 2; }
                    else if (level == 3) { reqs["cement"] = 3; reqs["wire"] = 2; }
                    else if (level == 4) { reqs["paint"] = 4; reqs["furniture"] = 1; }
                    else if (level == 5) { reqs["lamp"] = 2; reqs["appliance"] = 1; }
                    break;
            }

            return reqs;
        }

        public bool CanUpgradeBuilding(string buildingType, int targetLevel)
        {
            var requirements = GetBuildingRequirements(buildingType, targetLevel);
            foreach (var req in requirements)
            {
                if (!HasMaterial(req.Key, req.Value))
                    return false;
            }
            return true;
        }

        public bool TryUpgradeBuilding(string buildingType, int targetLevel)
        {
            var requirements = GetBuildingRequirements(buildingType, targetLevel);
            if (!CanUpgradeBuilding(buildingType, targetLevel))
                return false;

            foreach (var req in requirements)
            {
                ConsumeMaterial(req.Key, req.Value);
            }
            return true;
        }

        // 品质系统 - 产出稀有材料
        public MaterialQuality RollQuality(string materialId)
        {
            if (materialDatabase == null) return MaterialQuality.Common;

            var material = materialDatabase.GetMaterial(materialId);
            if (material == null) return MaterialQuality.Common;

            float roll = Random.value;
            if (roll < material.rareChance * 0.1f) return MaterialQuality.Rare;
            if (roll < material.rareChance) return MaterialQuality.Uncommon;
            return MaterialQuality.Common;
        }
    }
}
