using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PocketCity.Simulation;
using PocketCity.Core;

namespace PocketCity.Services
{
    /// <summary>
    /// 沿道路BFS寻路的服务覆盖系统
    /// </summary>
    public class RoadBasedServiceCoverage : MonoBehaviour
    {
        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private int maxSearchDistance = 50; // 最大寻路距离

        /// <summary>
        /// 检查位置是否被服务覆盖（必须通过道路连接）
        /// </summary>
        public bool IsPositionCoveredByService(GridPos targetPos, ServiceType serviceType, int serviceRange)
        {
            if (simulation == null) return false;

            // 获取所有该类型的服务建筑
            var serviceBuildings = GetServiceBuildingsByType(serviceType);

            foreach (var building in serviceBuildings)
            {
                // 使用BFS检查是否可以通过道路到达
                if (IsConnectedByRoad(building.FootprintOrigin, targetPos, serviceRange))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// BFS检查两点是否通过道路连接（在范围内）
        /// </summary>
        private bool IsConnectedByRoad(GridPos start, GridPos target, int maxDistance)
        {
            if (simulation == null) return false;

            Queue<GridPos> queue = new Queue<GridPos>();
            HashSet<GridPos> visited = new HashSet<GridPos>();
            Dictionary<GridPos, int> distances = new Dictionary<GridPos, int>();

            queue.Enqueue(start);
            visited.Add(start);
            distances[start] = 0;

            while (queue.Count > 0)
            {
                GridPos current = queue.Dequeue();
                int currentDist = distances[current];

                // 达到目标
                if (current.Equals(target))
                {
                    return true;
                }

                // 超出范围
                if (currentDist >= maxDistance)
                {
                    continue;
                }

                // 检查四个方向
                GridPos[] neighbors = new[]
                {
                    new GridPos(current.X + 1, current.Y),
                    new GridPos(current.X - 1, current.Y),
                    new GridPos(current.X, current.Y + 1),
                    new GridPos(current.X, current.Y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    // 边界检查
                    if (!simulation.Grid.IsInBounds(neighbor))
                        continue;

                    // 已访问
                    if (visited.Contains(neighbor))
                        continue;

                    // 必须是道路或目标位置
                    bool isRoad = simulation.Grid.GetRoadType(neighbor) != RoadType.None;
                    bool isTarget = neighbor.Equals(target);

                    if (!isRoad && !isTarget)
                        continue;

                    visited.Add(neighbor);
                    distances[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        /// <summary>
        /// 获取从服务建筑可到达的所有位置（用于可视化）
        /// </summary>
        public HashSet<GridPos> GetReachablePositions(GridPos servicePos, int maxDistance)
        {
            if (simulation == null) return new HashSet<GridPos>();

            HashSet<GridPos> reachable = new HashSet<GridPos>();
            Queue<GridPos> queue = new Queue<GridPos>();
            Dictionary<GridPos, int> distances = new Dictionary<GridPos, int>();

            queue.Enqueue(servicePos);
            reachable.Add(servicePos);
            distances[servicePos] = 0;

            while (queue.Count > 0)
            {
                GridPos current = queue.Dequeue();
                int currentDist = distances[current];

                if (currentDist >= maxDistance)
                    continue;

                GridPos[] neighbors = new[]
                {
                    new GridPos(current.X + 1, current.Y),
                    new GridPos(current.X - 1, current.Y),
                    new GridPos(current.X, current.Y + 1),
                    new GridPos(current.X, current.Y - 1)
                };

                foreach (var neighbor in neighbors)
                {
                    if (!simulation.Grid.IsInBounds(neighbor))
                        continue;

                    if (reachable.Contains(neighbor))
                        continue;

                    // 道路或建筑位置
                    bool isRoad = simulation.Grid.GetRoadType(neighbor) != RoadType.None;
                    bool hasBuilding = !string.IsNullOrEmpty(simulation.Grid.FindBuildingIdAt(neighbor));

                    if (!isRoad && !hasBuilding)
                        continue;

                    reachable.Add(neighbor);
                    distances[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return reachable;
        }

        /// <summary>
        /// 检查建筑是否缺少服务（用于显示警告图标）
        /// </summary>
        public List<ServiceType> GetMissingServices(PlacedBuilding building)
        {
            List<ServiceType> missing = new List<ServiceType>();

            var pos = building.FootprintOrigin;

            // 检查各项服务（必须通过道路连接）
            if (!IsPositionCoveredByService(pos, ServiceType.Fire, 20))
                missing.Add(ServiceType.Fire);

            if (!IsPositionCoveredByService(pos, ServiceType.Police, 15))
                missing.Add(ServiceType.Police);

            if (!IsPositionCoveredByService(pos, ServiceType.Health, 25))
                missing.Add(ServiceType.Health);

            return missing;
        }

        private List<PlacedBuilding> GetServiceBuildingsByType(ServiceType type)
        {
            if (simulation == null) return new List<PlacedBuilding>();

            return simulation.Buildings.Where(b =>
            {
                var def = simulation.Config.GetBuilding(b.ConfigId);
                if (def == null) return false;

                return GetServiceTypeFromBuilding(def) == type;
            }).ToList();
        }

        private ServiceType GetServiceTypeFromBuilding(BuildingDefinition def)
        {
            string id = def.Id.ToLower();

            if (id.Contains("fire")) return ServiceType.Fire;
            if (id.Contains("police")) return ServiceType.Police;
            if (id.Contains("hospital") || id.Contains("clinic")) return ServiceType.Health;
            if (id.Contains("school") || id.Contains("university")) return ServiceType.Education;

            return ServiceType.None;
        }

        /// <summary>
        /// 获取不被任何道路连接的建筑（孤岛建筑）
        /// </summary>
        public List<PlacedBuilding> GetIsolatedBuildings()
        {
            if (simulation == null) return new List<PlacedBuilding>();

            List<PlacedBuilding> isolated = new List<PlacedBuilding>();

            foreach (var building in simulation.Buildings)
            {
                // 检查是否连接到道路网络
                if (!IsConnectedToRoadNetwork(building.FootprintOrigin))
                {
                    isolated.Add(building);
                }
            }

            return isolated;
        }

        private bool IsConnectedToRoadNetwork(GridPos pos)
        {
            // 检查相邻格是否有道路
            GridPos[] neighbors = new[]
            {
                new GridPos(pos.X + 1, pos.Y),
                new GridPos(pos.X - 1, pos.Y),
                new GridPos(pos.X, pos.Y + 1),
                new GridPos(pos.X, pos.Y - 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (simulation.Grid.IsInBounds(neighbor))
                {
                    if (simulation.Grid.GetRoadType(neighbor) != RoadType.None)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
