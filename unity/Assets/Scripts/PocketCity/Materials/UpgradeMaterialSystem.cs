using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Materials
{
    public enum MaterialRarity { Common, Rare, Epic }

    [Serializable]
    public class UpgradeMaterial
    {
        public string Id;
        public string Name;
        public MaterialRarity Rarity;
        public int Stack;
    }

    public class UpgradeMaterialSystem : MonoBehaviour
    {
        public static UpgradeMaterialSystem Instance { get; private set; }

        private Dictionary<string, int> materials = new Dictionary<string, int>();
        public int MaxStorage = 200;
        public int CurrentStorage => GetTotalCount();

        public event Action<string, int> OnMaterialChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeMaterials();
        }

        void InitializeMaterials()
        {
            // 常见材料
            materials["nails"] = 0;
            materials["plank"] = 0;
            materials["brick"] = 0;
            materials["cement"] = 0;
            materials["glue"] = 0;
            materials["paint"] = 0;

            // 稀有材料
            materials["bulldozer_blade"] = 0;
            materials["tire"] = 0;
            materials["kitchen"] = 0;
            materials["hammer"] = 0;
            materials["tape_measure"] = 0;
            materials["hard_hat"] = 0;
        }

        public bool AddMaterial(string id, int amount)
        {
            if (CurrentStorage + amount > MaxStorage) return false;
            materials[id] = materials.GetValueOrDefault(id, 0) + amount;
            OnMaterialChanged?.Invoke(id, materials[id]);
            return true;
        }

        public bool ConsumeMaterial(string id, int amount)
        {
            if (!HasMaterial(id, amount)) return false;
            materials[id] -= amount;
            OnMaterialChanged?.Invoke(id, materials[id]);
            return true;
        }

        public bool HasMaterial(string id, int amount) => materials.GetValueOrDefault(id, 0) >= amount;
        public int GetMaterialCount(string id) => materials.GetValueOrDefault(id, 0);
        int GetTotalCount() { int total = 0; foreach (var kvp in materials) total += kvp.Value; return total; }

        public bool CanUpgradeBuilding(Dictionary<string, int> requirements)
        {
            foreach (var req in requirements)
                if (!HasMaterial(req.Key, req.Value)) return false;
            return true;
        }

        public bool TryUpgradeBuilding(Dictionary<string, int> requirements)
        {
            if (!CanUpgradeBuilding(requirements)) return false;
            foreach (var req in requirements) ConsumeMaterial(req.Key, req.Value);
            return true;
        }

        public Dictionary<string, int> GetBuildingRequirements(string buildingType, int level)
        {
            var reqs = new Dictionary<string, int>();
            if (level < 2 || level > 5) return reqs;

            switch (buildingType)
            {
                case "residential":
                    if (level == 2) { reqs["nails"] = 2; reqs["plank"] = 2; }
                    else if (level == 3) { reqs["brick"] = 3; reqs["cement"] = 2; }
                    else if (level == 4) { reqs["glue"] = 4; reqs["paint"] = 3; }
                    else if (level == 5) { reqs["bulldozer_blade"] = 2; reqs["tire"] = 2; reqs["kitchen"] = 1; }
                    break;

                case "commercial":
                case "office":
                    if (level == 2) { reqs["nails"] = 3; reqs["plank"] = 1; reqs["paint"] = 1; }
                    else if (level == 3) { reqs["brick"] = 4; reqs["cement"] = 3; }
                    else if (level == 4) { reqs["glue"] = 5; reqs["paint"] = 4; reqs["tape_measure"] = 2; }
                    else if (level == 5) { reqs["bulldozer_blade"] = 3; reqs["tire"] = 2; reqs["kitchen"] = 2; }
                    break;

                case "industrial":
                    if (level == 2) { reqs["cement"] = 2; reqs["hard_hat"] = 1; }
                    else if (level == 3) { reqs["brick"] = 5; reqs["cement"] = 3; reqs["tire"] = 1; }
                    else if (level == 4) { reqs["glue"] = 3; reqs["paint"] = 2; reqs["bulldozer_blade"] = 1; }
                    else if (level == 5) { reqs["bulldozer_blade"] = 4; reqs["tire"] = 3; reqs["tape_measure"] = 1; }
                    break;

                default:
                    if (level == 2) { reqs["nails"] = 2; reqs["plank"] = 2; }
                    else if (level == 3) { reqs["brick"] = 3; reqs["cement"] = 2; reqs["hammer"] = 1; }
                    else if (level == 4) { reqs["glue"] = 4; reqs["paint"] = 3; reqs["tape_measure"] = 1; }
                    else if (level == 5) { reqs["bulldozer_blade"] = 2; reqs["tire"] = 2; reqs["kitchen"] = 1; }
                    break;
            }

            return reqs;
        }

        public void ExpandStorage(int additionalCapacity) { MaxStorage += additionalCapacity; }

        /// <summary>
        /// 获取材料数量（统一API）
        /// </summary>
        public int GetMaterialAmount(string materialId)
        {
            return GetMaterialCount(materialId);
        }

        public int GetAmount(string materialId)
        {
            return GetMaterialCount(materialId);
        }

        /// <summary>
        /// 移除材料（统一API）
        /// </summary>
        public bool RemoveMaterial(string materialId, int amount)
        {
            return ConsumeMaterial(materialId, amount);
        }

        public bool Remove(string materialId, int amount)
        {
            return ConsumeMaterial(materialId, amount);
        }
    }
}
