using System;
using System.Collections.Generic;
using UnityEngine;
using PocketCity.Core;

namespace PocketCity.Production
{
    [Serializable]
    public class ProductionSlot
    {
        public MaterialData material;
        public float startTime;
        public float duration;
        public float elapsedTime;
        public bool isCompleted => elapsedTime >= duration;

        public void UpdateProgress(float deltaTime)
        {
            if (!isCompleted)
            {
                elapsedTime += deltaTime;
            }
        }
    }

    public enum FactoryType
    {
        BuildingSupplies,    // 建材厂
        Hardware,            // 五金店
        Farming,             // 农贸市场
        Furniture,           // 家具厂
        Gardening,           // 园艺店
        DonutShop            // 甜甜圈店
    }

    [Serializable]
    public class Factory
    {
        public FactoryType type;
        public int maxSlots = 2;
        public List<ProductionSlot> slots = new List<ProductionSlot>();
        public List<string> allowedProducts = new List<string>(); // 白名单

        public bool CanProduce() => slots.Count < maxSlots;

        public bool CanProduceMaterial(string materialId)
        {
            return allowedProducts.Count == 0 || allowedProducts.Contains(materialId);
        }

        public void StartProduction(MaterialData material)
        {
            if (!CanProduce()) return;
            slots.Add(new ProductionSlot
            {
                material = material,
                startTime = 0f,
                duration = material.productionTime,
                elapsedTime = 0f
            });
        }

        public List<MaterialData> CollectCompleted()
        {
            var completed = new List<MaterialData>();
            slots.RemoveAll(slot =>
            {
                if (slot.isCompleted)
                {
                    completed.Add(slot.material);
                    return true;
                }
                return false;
            });
            return completed;
        }
    }

    public class ProductionChainSystem : MonoBehaviour
    {
        public static ProductionChainSystem Instance { get; private set; }

        [SerializeField] private MaterialDatabase materialDB;
        [SerializeField] private StorageSystem storage;
        [SerializeField] private CityConfig config;

        private Dictionary<FactoryType, Factory> factories = new Dictionary<FactoryType, Factory>();

        public event System.Action<MaterialData> OnProductionComplete;

        private void Awake()
        {
            if (Instance != null) Destroy(gameObject);
            else Instance = this;

            InitializeFactories();
        }

        private void InitializeFactories()
        {
            int maxSlots = config != null ? config.FactoryMaxSlots : 2;
            foreach (FactoryType type in Enum.GetValues(typeof(FactoryType)))
            {
                var factory = new Factory { type = type, maxSlots = maxSlots };

                // 设置工厂白名单
                factory.allowedProducts = GetFactoryAllowedProducts(type);

                factories[type] = factory;
            }
        }

        private List<string> GetFactoryAllowedProducts(FactoryType type)
        {
            // 匹配MaterialDatabase中的实际材料ID
            switch (type)
            {
                case FactoryType.BuildingSupplies:
                    // 匹配: plank, brick, cement, pipe
                    return new List<string> { "plank", "brick", "cement", "pipe", "glass", "handle" };
                case FactoryType.Hardware:
                    // 匹配: metal_ingot, nails, wire, screw
                    return new List<string> { "metal_ingot", "metal", "nails", "wire", "screw", "pipe" };
                case FactoryType.Farming:
                    // 匹配: seeds, fabric
                    return new List<string> { "seeds", "cotton", "fabric" };
                case FactoryType.Furniture:
                    // 匹配: furniture, lamp
                    return new List<string> { "furniture", "lamp", "appliance" };
                case FactoryType.Gardening:
                    // 匹配: paint, glue
                    return new List<string> { "paint", "glue", "plastic" };
                case FactoryType.DonutShop:
                    // 特殊食品（暂无对应材料，保留兼容）
                    return new List<string> { "donut", "bread", "pastry" };
                default:
                    return new List<string>();
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var factory in factories.Values)
            {
                foreach (var slot in factory.slots)
                {
                    slot.UpdateProgress(deltaTime);
                }
            }
        }

        public bool TryStartProduction(string materialId, FactoryType factoryType)
        {
            if (materialDB == null || storage == null || !factories.ContainsKey(factoryType))
            {
                return false;
            }

            // 规范化材料ID
            string normalizedId = MaterialIdMapper.NormalizeId(materialId);

            var factory = factories[factoryType];
            var material = materialDB.GetMaterial(normalizedId);

            if (material == null)
            {
                Debug.LogWarning($"材料 {materialId} (normalized: {normalizedId}) 不存在于MaterialDatabase中");
                return false;
            }

            if (!factory.CanProduce())
                return false;

            // 验证工厂白名单（使用规范化ID）
            if (!factory.CanProduceMaterial(normalizedId))
            {
                Debug.LogWarning($"{factoryType} 工厂不能生产 {normalizedId}");
                return false;
            }

            // 检查材料
            if (!storage.HasMaterials(material.recipe))
                return false;

            // 消耗材料
            storage.ConsumeMaterials(material.recipe);

            // 开始生产
            factory.StartProduction(material);
            return true;
        }

        public void CollectProduction(FactoryType factoryType)
        {
            if (storage == null || !factories.ContainsKey(factoryType))
            {
                return;
            }

            var completed = factories[factoryType].CollectCompleted();
            foreach (var material in completed)
            {
                storage.AddItem(material.id, 1);
                OnProductionComplete?.Invoke(material);
            }
        }

        public Factory GetFactory(FactoryType type)
        {
            factories.TryGetValue(type, out var factory);
            return factory;
        }

        public Dictionary<FactoryType, Factory> GetAllFactories()
        {
            return factories;
        }
    }
}
