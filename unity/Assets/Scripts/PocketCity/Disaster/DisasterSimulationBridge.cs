using UnityEngine;
using PocketCity.Core;
using PocketCity.Simulation;

namespace PocketCity.Disaster
{
    /// <summary>
    /// 灾难系统与模拟层的桥接
    /// </summary>
    public class DisasterSimulationBridge : MonoBehaviour
    {
        private CitySimulationCore simulation;
        private DisasterSystem disasterSystem;

        private void Awake()
        {
            disasterSystem = GetComponent<DisasterSystem>();
        }

        public void Initialize(CitySimulationCore sim)
        {
            simulation = sim;
            if (disasterSystem != null)
            {
                disasterSystem.OnDisasterTriggered += OnDisaster;
            }
        }

        private void OnDisaster(DisasterType type, int level, Vector3 position)
        {
            if (simulation == null) return;

            var gridPos = WorldToGrid(position);
            var radius = GetDisasterRadius(type, level);
            var damage = GetDisasterDamage(type, level);

            ApplyDisasterToSimulation(gridPos, radius, damage);
        }

        private void ApplyDisasterToSimulation(GridPos center, int radius, int damage)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var pos = new GridPos(center.X + dx, center.Y + dy);
                    if (!simulation.Grid.IsInBounds(pos)) continue;

                    var building = simulation.Grid.GetBuildingAt(pos);
                    if (building != null)
                    {
                        int distance = System.Math.Abs(dx) + System.Math.Abs(dy);
                        float mult = 1f - ((float)distance / radius);
                        int finalDamage = (int)(damage * mult);

                        simulation.DamageBuilding(building.Id, finalDamage);
                    }
                }
            }
        }

        private GridPos WorldToGrid(Vector3 worldPos)
        {
            return new GridPos(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z));
        }

        private int GetDisasterRadius(DisasterType type, int level)
        {
            return 10 + level * 5;
        }

        private int GetDisasterDamage(DisasterType type, int level)
        {
            return level * 20;
        }
    }
}
