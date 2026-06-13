using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Disaster
{
    [Serializable]
    public class BuildingDamage : MonoBehaviour
    {
        public int buildingId;
        public float durability = 100f;
        public bool isDestroyed;
        public int maxDurability = 100;
        public int currentDurability = 100;
        public int repairCost = 100;
        public float serviceCoverageReduction = 0f;

        public event Action<int> OnDamaged;
        public event Action OnDestroyed;
        public event Action OnRepaired;

        public void TakeDamage(int damage)
        {
            currentDurability = Mathf.Max(0, currentDurability - damage);
            serviceCoverageReduction = 1f - ((float)currentDurability / maxDurability);

            OnDamaged?.Invoke(damage);

            if (currentDurability <= 0)
            {
                OnDestroyed?.Invoke();
            }
        }

        public bool Repair(int amount)
        {
            if (currentDurability >= maxDurability)
                return false;

            currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
            serviceCoverageReduction = 1f - ((float)currentDurability / maxDurability);

            OnRepaired?.Invoke();
            return true;
        }

        public int GetRepairCost()
        {
            int damagePercent = 100 - (currentDurability * 100 / maxDurability);
            return (repairCost * damagePercent) / 100;
        }

        public float GetServiceEfficiency()
        {
            return 1f - serviceCoverageReduction;
        }

        public bool IsDestroyed()
        {
            return currentDurability <= 0;
        }
    }

    public class DamageSystem : MonoBehaviour
    {
        private Dictionary<int, BuildingDamage> damageRegistry = new Dictionary<int, BuildingDamage>();

        public event Action<BuildingDamage, int> OnBuildingDamaged;
        public event Action<BuildingDamage> OnBuildingDestroyed;
        public event Action<BuildingDamage, int> OnBuildingRepaired;

        public void RegisterBuilding(int buildingId, BuildingDamage damage)
        {
            damageRegistry[buildingId] = damage;
        }

        public BuildingDamage GetBuildingDamage(int buildingId)
        {
            BuildingDamage result = null;
            damageRegistry.TryGetValue(buildingId, out result);
            return result;
        }

        public void DamageBuilding(BuildingDamage building, int damage)
        {
            if (building == null || building.IsDestroyed())
                return;

            int beforeDurability = building.currentDurability;
            building.TakeDamage(damage);

            OnBuildingDamaged?.Invoke(building, damage);

            if (building.IsDestroyed())
            {
                OnBuildingDestroyed?.Invoke(building);
            }
        }

        public bool RepairBuilding(BuildingDamage building, int funds)
        {
            if (building == null || building.currentDurability >= building.maxDurability)
                return false;

            int repairCost = building.GetRepairCost();
            if (funds < repairCost)
                return false;

            int repairAmount = building.maxDurability - building.currentDurability;
            building.Repair(repairAmount);

            OnBuildingRepaired?.Invoke(building, repairCost);
            return true;
        }

        public bool PartialRepair(BuildingDamage building, int funds)
        {
            if (building == null || building.currentDurability >= building.maxDurability)
                return false;

            int fullCost = building.GetRepairCost();
            if (funds <= 0)
                return false;

            float repairRatio = Mathf.Min(1f, (float)funds / fullCost);
            int repairAmount = Mathf.RoundToInt((building.maxDurability - building.currentDurability) * repairRatio);
            int actualCost = Mathf.RoundToInt(fullCost * repairRatio);

            building.Repair(repairAmount);
            OnBuildingRepaired?.Invoke(building, actualCost);
            return true;
        }

        public void DamageArea(Vector3 center, float radius, int damage)
        {
            Collider[] hits = Physics.OverlapSphere(center, radius);
            foreach (Collider hit in hits)
            {
                BuildingDamage building = hit.GetComponent<BuildingDamage>();
                if (building != null)
                {
                    float distance = Vector3.Distance(center, hit.transform.position);
                    float damageMultiplier = 1f - (distance / radius);
                    int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
                    DamageBuilding(building, finalDamage);
                }
            }
        }
    }
}
