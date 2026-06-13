using UnityEngine;
using System.Collections.Generic;
using PocketCity.Simulation;
using PocketCity.Core;

namespace PocketCity.Transportation
{
    /// <summary>
    /// 公交/捷运系统 - 减少拥堵 + 扩大通勤范围
    /// </summary>
    public class PublicTransportSystem : MonoBehaviour
    {
        public static PublicTransportSystem Instance { get; private set; }

        [SerializeField] private CitySimulationCore simulation;

        [Header("Settings")]
        [SerializeField] private int busStopCongestionReduction = 10; // 公交站减少10%拥堵
        [SerializeField] private int subwayStationCongestionReduction = 25; // 地铁站减少25%拥堵
        [SerializeField] private int busStopCommuteRange = 10; // 公交站扩大通勤范围10格
        [SerializeField] private int subwayStationCommuteRange = 30; // 地铁站扩大30格

        private List<PublicTransportStation> stations = new List<PublicTransportStation>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            RefreshStations();
        }

        /// <summary>
        /// 刷新所有公交站/地铁站
        /// </summary>
        public void RefreshStations()
        {
            stations.Clear();

            if (simulation == null) return;

            foreach (var building in simulation.Buildings)
            {
                var def = simulation.Config.GetBuilding(building.ConfigId);
                if (def == null) continue;

                TransportType type = GetTransportType(def);
                if (type != TransportType.None)
                {
                    stations.Add(new PublicTransportStation
                    {
                        buildingId = building.Id,
                        position = building.FootprintOrigin,
                        type = type,
                        isActive = true
                    });
                }
            }

            Debug.Log($"刷新公共交通：{stations.Count} 个站点");
        }

        /// <summary>
        /// 计算位置的拥堵减免
        /// </summary>
        public int GetCongestionReduction(GridPos pos)
        {
            int reduction = 0;

            foreach (var station in stations)
            {
                if (!station.isActive) continue;

                int distance = GridPos.ManhattanDistance(pos, station.position);
                int range = station.type == TransportType.BusStop ? 15 : 20;

                if (distance <= range)
                {
                    int stationReduction = station.type == TransportType.BusStop
                        ? busStopCongestionReduction
                        : subwayStationCongestionReduction;

                    reduction += stationReduction;
                }
            }

            return Mathf.Min(reduction, 50); // 最多减少50%
        }

        /// <summary>
        /// 检查两点是否被公共交通连接
        /// </summary>
        public bool IsConnectedByTransit(GridPos from, GridPos to)
        {
            // 检查起点附近是否有站点
            var fromStation = GetNearestStation(from);
            if (fromStation == null) return false;

            // 检查终点附近是否有站点
            var toStation = GetNearestStation(to);
            if (toStation == null) return false;

            // 地铁可以连接任意两个地铁站
            if (fromStation.type == TransportType.SubwayStation &&
                toStation.type == TransportType.SubwayStation)
            {
                return true;
            }

            // 公交需要在合理距离内
            int stationDistance = GridPos.ManhattanDistance(fromStation.position, toStation.position);
            return stationDistance <= 50;
        }

        /// <summary>
        /// 获取位置的有效通勤范围（受公共交通影响）
        /// </summary>
        public int GetEffectiveCommuteRange(GridPos pos, int baseRange)
        {
            var station = GetNearestStation(pos);
            if (station == null) return baseRange;

            int distance = GridPos.ManhattanDistance(pos, station.position);

            // 在站点范围内
            int stationRange = station.type == TransportType.BusStop ? 10 : 15;
            if (distance <= stationRange)
            {
                int bonus = station.type == TransportType.BusStop
                    ? busStopCommuteRange
                    : subwayStationCommuteRange;

                return baseRange + bonus;
            }

            return baseRange;
        }

        /// <summary>
        /// 获取最近的站点
        /// </summary>
        private PublicTransportStation GetNearestStation(GridPos pos)
        {
            PublicTransportStation nearest = null;
            int minDistance = int.MaxValue;

            foreach (var station in stations)
            {
                if (!station.isActive) continue;

                int distance = GridPos.ManhattanDistance(pos, station.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = station;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 获取站点覆盖的建筑数量
        /// </summary>
        public int GetCoveredBuildingsCount(string stationId)
        {
            var station = stations.Find(s => s.buildingId == stationId);
            if (station == null || simulation == null) return 0;

            int count = 0;
            int range = station.type == TransportType.BusStop ? 15 : 20;

            foreach (var building in simulation.Buildings)
            {
                int distance = GridPos.ManhattanDistance(station.position, building.FootprintOrigin);
                if (distance <= range)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 获取站点效果描述
        /// </summary>
        public string GetStationEffectDescription(string stationId)
        {
            var station = stations.Find(s => s.buildingId == stationId);
            if (station == null) return "";

            int coveredBuildings = GetCoveredBuildingsCount(stationId);
            int congestionReduction = station.type == TransportType.BusStop
                ? busStopCongestionReduction
                : subwayStationCongestionReduction;

            int commuteBonus = station.type == TransportType.BusStop
                ? busStopCommuteRange
                : subwayStationCommuteRange;

            string type = station.type == TransportType.BusStop ? "公交站" : "地铁站";

            return $"{type}\n" +
                   $"📍 覆盖建筑: {coveredBuildings}\n" +
                   $"🚦 减少拥堵: -{congestionReduction}%\n" +
                   $"🏠 扩大通勤: +{commuteBonus}格";
        }

        private TransportType GetTransportType(BuildingDefinition def)
        {
            string id = def.Id.ToLower();

            if (id.Contains("bus") || id.Contains("公交")) return TransportType.BusStop;
            if (id.Contains("subway") || id.Contains("metro") || id.Contains("地铁")) return TransportType.SubwayStation;

            return TransportType.None;
        }

        /// <summary>
        /// 计算道路拥堵（考虑公共交通）
        /// </summary>
        public float CalculateRoadCongestion(GridPos pos, float baseCongestion)
        {
            int reduction = GetCongestionReduction(pos);
            float multiplier = 1f - (reduction / 100f);
            return baseCongestion * multiplier;
        }
    }

    public enum TransportType
    {
        None,
        BusStop,
        SubwayStation
    }

    [System.Serializable]
    public class PublicTransportStation
    {
        public string buildingId;
        public GridPos position;
        public TransportType type;
        public bool isActive;
    }
}
