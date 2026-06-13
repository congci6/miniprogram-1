using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCity.Production
{
    [Serializable]
    public class SpecializedProductionSlot
    {
        public string materialId;
        public float startTime;
        public float duration;
        public float elapsedTime;
        public MaterialQuality quality = MaterialQuality.Common;
        public bool isCompleted => elapsedTime >= duration;

        public void UpdateProgress(float deltaTime)
        {
            if (!isCompleted)
            {
                elapsedTime += deltaTime;
            }
        }
    }

    public enum SpecializedFactoryType
    {
        Smelter,        // 冶炼厂: ore -> metal_ingot
        Sawmill,        // 锯木厂: log -> plank
        Refinery,       // 精炼厂: crude_oil -> plastic/rubber
        TextileMill,    // 纺织厂: cotton -> fabric
        MetalWorks,     // 金属加工: metal_ingot -> nails/wire/pipe/screw
        ChemicalPlant,  // 化工厂: plastic + oil -> paint
        Workshop,       // 作坊: plank + screw -> furniture
        ElectronicsLab  // 电子厂: wire + plastic -> circuit_board
    }

    [Serializable]
    public class SpecializedFactory
    {
        public SpecializedFactoryType type;
        public int maxSlots = 2;
        public List<SpecializedProductionSlot> slots = new List<SpecializedProductionSlot>();
        public HashSet<string> allowedMaterials = new HashSet<string>();

        public bool CanProduce() => slots.Count < maxSlots;
        public bool CanProduceMaterial(string materialId) => allowedMaterials.Contains(materialId);

        public void StartProduction(string materialId, float duration, MaterialQuality quality)
        {
            if (!CanProduce() || !CanProduceMaterial(materialId)) return;

            slots.Add(new SpecializedProductionSlot
            {
                materialId = materialId,
                startTime = Time.time,
                duration = duration,
                elapsedTime = 0f,
                quality = quality
            });
        }

        public List<(string materialId, MaterialQuality quality)> CollectCompleted()
        {
            var completed = new List<(string, MaterialQuality)>();
            slots.RemoveAll(slot =>
            {
                if (slot.isCompleted)
                {
                    completed.Add((slot.materialId, slot.quality));
                    return true;
                }
                return false;
            });
            return completed;
        }

        public void UpdateSlots(float deltaTime)
        {
            foreach (var slot in slots)
            {
                slot.UpdateProgress(deltaTime);
            }
        }
    }

    public class SpecializedFactorySystem : MonoBehaviour
    {
        public static SpecializedFactorySystem Instance { get; private set; }

        [SerializeField] private MaterialDatabase materialDB;
        [SerializeField] private StorageSystem storage;

        private Dictionary<SpecializedFactoryType, SpecializedFactory> factories = new Dictionary<SpecializedFactoryType, SpecializedFactory>();

        public event Action<string, MaterialQuality> OnProductionCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeSpecializedFactories();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var factory in factories.Values)
            {
                factory.UpdateSlots(deltaTime);
                var completed = factory.CollectCompleted();
                foreach (var (materialId, quality) in completed)
                {
                    storage?.AddItem(materialId, 1);
                    OnProductionCompleted?.Invoke(materialId, quality);
                }
            }
        }

        private void InitializeSpecializedFactories()
        {
            // 冶炼厂: Basic矿石 -> Raw金属锭
            AddFactory(SpecializedFactoryType.Smelter, "metal_ingot", "glass");

            // 锯木厂: Basic原木 -> Raw木板
            AddFactory(SpecializedFactoryType.Sawmill, "plank");

            // 精炼厂: Basic原油 -> Raw塑料/橡胶
            AddFactory(SpecializedFactoryType.Refinery, "plastic", "rubber");

            // 纺织厂: Basic棉花 -> Raw布料
            AddFactory(SpecializedFactoryType.TextileMill, "fabric");

            // 金属加工: Raw金属锭 -> Processed钉子/电线/管道/螺丝
            AddFactory(SpecializedFactoryType.MetalWorks, "nails", "wire", "pipe", "screw", "handle");

            // 化工厂: Raw塑料 -> Processed油漆/水泥/轮胎
            AddFactory(SpecializedFactoryType.ChemicalPlant, "paint", "cement", "tire");

            // 作坊: Processed -> Advanced家具/灯具
            AddFactory(SpecializedFactoryType.Workshop, "furniture", "lamp");

            // 电子厂: Processed -> Advanced电路板/家电/引擎/水泵
            AddFactory(SpecializedFactoryType.ElectronicsLab, "circuit_board", "appliance", "engine", "pump");
        }

        private void AddFactory(SpecializedFactoryType type, params string[] allowedMaterials)
        {
            var factory = new SpecializedFactory
            {
                type = type,
                maxSlots = 2,
                allowedMaterials = new HashSet<string>(allowedMaterials)
            };
            factories[type] = factory;
        }

        public bool TryStartProduction(SpecializedFactoryType factoryType, string materialId)
        {
            if (!factories.TryGetValue(factoryType, out var factory))
                return false;

            if (!factory.CanProduce() || !factory.CanProduceMaterial(materialId))
                return false;

            var material = materialDB?.GetMaterial(materialId);
            if (material == null) return false;

            // 检查原料
            foreach (var ingredient in material.recipe)
            {
                if (storage == null || storage.GetItemCount(ingredient.materialId) < ingredient.amount)
                    return false;
            }

            // 消耗原料
            foreach (var ingredient in material.recipe)
            {
                storage?.RemoveItem(ingredient.materialId, ingredient.amount);
            }

            // 随机品质
            var quality = RollQuality(material);

            factory.StartProduction(materialId, material.productionTime, quality);
            return true;
        }

        private MaterialQuality RollQuality(MaterialData material)
        {
            float roll = UnityEngine.Random.value;
            if (roll < material.rareChance * 0.1f) return MaterialQuality.Rare;
            if (roll < material.rareChance) return MaterialQuality.Uncommon;
            return MaterialQuality.Common;
        }

        public SpecializedFactory GetFactory(SpecializedFactoryType type)
        {
            factories.TryGetValue(type, out var factory);
            return factory;
        }

        public List<SpecializedFactory> GetAllFactories()
        {
            return factories.Values.ToList();
        }
    }
}
