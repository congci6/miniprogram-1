using UnityEngine;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 增量渲染管理器 - 避免全量重建
    /// </summary>
    public class IncrementalRenderingManager
    {
        // 建筑ID到GameObject的映射
        private Dictionary<string, GameObject> buildingObjects = new Dictionary<string, GameObject>();

        // 道路位置到GameObject的映射
        private Dictionary<GridPos, GameObject> roadObjects = new Dictionary<GridPos, GameObject>();

        // 装饰对象映射
        private Dictionary<GridPos, List<GameObject>> decorationObjects = new Dictionary<GridPos, List<GameObject>>();

        // 父节点
        private Transform buildingParent;
        private Transform roadParent;
        private Transform decorationParent;

        public IncrementalRenderingManager(Transform parent)
        {
            buildingParent = new GameObject("Buildings").transform;
            buildingParent.SetParent(parent, false);

            roadParent = new GameObject("Roads").transform;
            roadParent.SetParent(parent, false);

            decorationParent = new GameObject("Decorations").transform;
            decorationParent.SetParent(parent, false);
        }

        // 添加或更新建筑
        public void AddOrUpdateBuilding(string buildingId, GameObject buildingObj)
        {
            if (buildingObjects.TryGetValue(buildingId, out var oldObj))
            {
                // 移除旧对象
                if (oldObj != null)
                    Object.Destroy(oldObj);
            }

            buildingObjects[buildingId] = buildingObj;
            buildingObj.transform.SetParent(buildingParent, false);
        }

        // 移除建筑
        public void RemoveBuilding(string buildingId)
        {
            if (buildingObjects.TryGetValue(buildingId, out var obj))
            {
                if (obj != null)
                    Object.Destroy(obj);
                buildingObjects.Remove(buildingId);
            }
        }

        // 添加或更新道路
        public void AddOrUpdateRoad(GridPos pos, GameObject roadObj)
        {
            if (roadObjects.TryGetValue(pos, out var oldObj))
            {
                if (oldObj != null)
                    Object.Destroy(oldObj);
            }

            roadObjects[pos] = roadObj;
            roadObj.transform.SetParent(roadParent, false);
        }

        // 移除道路
        public void RemoveRoad(GridPos pos)
        {
            if (roadObjects.TryGetValue(pos, out var obj))
            {
                if (obj != null)
                    Object.Destroy(obj);
                roadObjects.Remove(pos);
            }
        }

        // 添加装饰
        public void AddDecoration(GridPos pos, GameObject decorObj)
        {
            if (!decorationObjects.ContainsKey(pos))
            {
                decorationObjects[pos] = new List<GameObject>();
            }

            decorationObjects[pos].Add(decorObj);
            decorObj.transform.SetParent(decorationParent, false);
        }

        // 清除位置的所有装饰
        public void ClearDecorations(GridPos pos)
        {
            if (decorationObjects.TryGetValue(pos, out var list))
            {
                foreach (var obj in list)
                {
                    if (obj != null)
                        Object.Destroy(obj);
                }
                list.Clear();
                decorationObjects.Remove(pos);
            }
        }

        // 清除所有
        public void ClearAll()
        {
            foreach (var obj in buildingObjects.Values)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            buildingObjects.Clear();

            foreach (var obj in roadObjects.Values)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }
            roadObjects.Clear();

            foreach (var list in decorationObjects.Values)
            {
                foreach (var obj in list)
                {
                    if (obj != null)
                        Object.Destroy(obj);
                }
            }
            decorationObjects.Clear();
        }

        // 获取统计信息
        public (int buildings, int roads, int decorations) GetStats()
        {
            int decorCount = 0;
            foreach (var list in decorationObjects.Values)
            {
                decorCount += list.Count;
            }
            return (buildingObjects.Count, roadObjects.Count, decorCount);
        }

        // 检查建筑是否存在
        public bool HasBuilding(string buildingId)
        {
            return buildingObjects.ContainsKey(buildingId);
        }

        // 检查道路是否存在
        public bool HasRoad(GridPos pos)
        {
            return roadObjects.ContainsKey(pos);
        }
    }
}
