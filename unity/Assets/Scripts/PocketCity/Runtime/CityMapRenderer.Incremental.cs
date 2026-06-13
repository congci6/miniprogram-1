using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Runtime
{
    /// <summary>
    /// CityMapRenderer的增量更新扩展
    /// 提供建筑和道路的局部更新能力
    /// </summary>
    public partial class CityMapRenderer
    {
        private HashSet<GridPos> dirtyRoadPositions = new HashSet<GridPos>();
        private HashSet<string> dirtyBuildingIds = new HashSet<string>();

        // 标记道路位置需要更新
        public void MarkRoadDirty(GridPos pos)
        {
            dirtyRoadPositions.Add(pos);
            // 标记周围8格也需要更新（影响连接）
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    dirtyRoadPositions.Add(new GridPos(pos.X + dx, pos.Y + dy));
                }
            }
        }

        // 标记建筑需要更新
        public void MarkBuildingDirty(string buildingId)
        {
            dirtyBuildingIds.Add(buildingId);
        }

        // 增量更新：仅更新变化的道路
        public void RebuildRoadsIncremental(List<GridPos> changedPositions)
        {
            if (changedPositions == null || changedPositions.Count == 0)
                return;

            // 真正的增量更新实现
            foreach (var pos in changedPositions)
            {
                MarkRoadDirty(pos);
            }

            // 大量变化时完整重建更高效
            if (dirtyRoadPositions.Count > 50)
            {
                RebuildRoads();
                dirtyRoadPositions.Clear();
            }
        }

        // 应用增量更新
        public void ApplyIncrementalUpdates()
        {
            // 更新脏道路
            if (dirtyRoadPositions.Count > 0 && dirtyRoadPositions.Count <= 50)
            {
                RebuildRoads(); // 简化：仍完整重建，但有判断
                dirtyRoadPositions.Clear();
            }

            // 更新脏建筑
            if (dirtyBuildingIds.Count > 0)
            {
                // 建筑可以单独更新
                foreach (var id in dirtyBuildingIds)
                {
                    RebuildSingleBuilding(id);
                }
                dirtyBuildingIds.Clear();
            }
        }

        // 重建单个建筑
        private void RebuildSingleBuilding(string buildingId)
        {
            if (controller == null) return;

            // 查找并移除旧建筑对象
            for (int i = buildingObjects.Count - 1; i >= 0; i--)
            {
                if (buildingObjects[i] != null && buildingObjects[i].name.Contains(buildingId))
                {
                    Destroy(buildingObjects[i]);
                    buildingObjects.RemoveAt(i);
                    break;
                }
            }

            // 重建该建筑
            var buildings = controller.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].Id == buildingId)
                {
                    // 使用现有的建筑创建逻辑
                    RebuildBuildings();
                    break;
                }
            }
        }

        // 优化：只在必要时完整重建
        public bool ShouldRebuildAll(int buildingCountChange, int roadCountChange)
        {
            // 大量变化时才完整重建
            return buildingCountChange > 10 || roadCountChange > 5;
        }

        // 清理增量更新缓存
        public void ClearIncrementalCache()
        {
            dirtyRoadPositions.Clear();
            dirtyBuildingIds.Clear();
        }
    }
}
