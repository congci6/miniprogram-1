using UnityEngine;
using System.Collections.Generic;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 简单的视锥剔除管理器，优化大型城市的渲染性能
    /// </summary>
    public class SimpleCullingManager
    {
        private Camera targetCamera;
        private Plane[] frustumPlanes;
        private float updateInterval = 0.1f;
        private float lastUpdateTime;
        private Dictionary<GameObject, Renderer> rendererCache = new Dictionary<GameObject, Renderer>();

        public SimpleCullingManager(Camera camera, float updateInterval = 0.1f)
        {
            this.targetCamera = camera;
            this.updateInterval = updateInterval;
            this.frustumPlanes = new Plane[6];
        }

        /// <summary>
        /// 更新视锥平面（不需要每帧调用）
        /// </summary>
        public void UpdateFrustum()
        {
            if (targetCamera == null)
                return;

            var currentTime = Time.time;
            if (currentTime - lastUpdateTime < updateInterval)
                return;

            lastUpdateTime = currentTime;
            GeometryUtility.CalculateFrustumPlanes(targetCamera, frustumPlanes);
        }

        /// <summary>
        /// 检查点是否在视锥内
        /// </summary>
        public bool IsVisible(Vector3 point)
        {
            if (frustumPlanes == null || frustumPlanes.Length == 0)
                return true;

            foreach (var plane in frustumPlanes)
            {
                if (plane.GetDistanceToPoint(point) < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查包围盒是否在视锥内
        /// </summary>
        public bool IsVisible(Bounds bounds)
        {
            if (frustumPlanes == null || frustumPlanes.Length == 0)
                return true;

            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }

        /// <summary>
        /// 批量剔除对象列表
        /// </summary>
        public void CullObjects(List<GameObject> objects, float cullDistance = 500f)
        {
            if (targetCamera == null || objects == null)
                return;

            var cameraPos = targetCamera.transform.position;

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj == null)
                {
                    // 清理无效缓存
                    continue;
                }

                var distance = Vector3.Distance(obj.transform.position, cameraPos);

                // 距离剔除
                if (distance > cullDistance)
                {
                    obj.SetActive(false);
                    continue;
                }

                // 视锥剔除 - 使用缓存的Renderer
                if (!rendererCache.TryGetValue(obj, out var renderer))
                {
                    renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        rendererCache[obj] = renderer;
                    }
                }

                if (renderer != null)
                {
                    var visible = IsVisible(renderer.bounds);
                    obj.SetActive(visible);
                }
            }

            // 定期清理失效缓存（每100次调用清理一次）
            cullCallCount++;
            if (cullCallCount >= 100)
            {
                CleanupCache();
                cullCallCount = 0;
            }
        }

        private int cullCallCount = 0;

        private void CleanupCache()
        {
            var keysToRemove = new List<GameObject>();
            foreach (var kvp in rendererCache)
            {
                if (kvp.Key == null || kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                rendererCache.Remove(key);
            }
        }

        public void ClearCache()
        {
            rendererCache.Clear();
        }
    }

    /// <summary>
    /// LOD管理器，根据距离切换建筑细节
    /// </summary>
    public class SimpleLODManager
    {
        public enum LODLevel
        {
            High,    // 0-50m: 完整细节
            Medium,  // 50-150m: 中等细节
            Low,     // 150-300m: 低细节
            Culled   // 300m+: 不渲染
        }

        private Camera targetCamera;
        private float highDetailDistance = 50f;
        private float mediumDetailDistance = 150f;
        private float lowDetailDistance = 300f;

        public SimpleLODManager(Camera camera)
        {
            this.targetCamera = camera;
        }

        public SimpleLODManager(Camera camera, float high, float medium, float low)
        {
            this.targetCamera = camera;
            this.highDetailDistance = high;
            this.mediumDetailDistance = medium;
            this.lowDetailDistance = low;
        }

        /// <summary>
        /// 计算对象的LOD级别
        /// </summary>
        public LODLevel CalculateLOD(Vector3 position)
        {
            if (targetCamera == null)
                return LODLevel.High;

            var distance = Vector3.Distance(position, targetCamera.transform.position);

            if (distance < highDetailDistance)
                return LODLevel.High;
            else if (distance < mediumDetailDistance)
                return LODLevel.Medium;
            else if (distance < lowDetailDistance)
                return LODLevel.Low;
            else
                return LODLevel.Culled;
        }

        /// <summary>
        /// 批量更新对象LOD
        /// </summary>
        public void UpdateLODs(List<GameObject> objects)
        {
            if (targetCamera == null || objects == null)
                return;

            for (int i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                var lod = CalculateLOD(obj.transform.position);
                ApplyLOD(obj, lod);
            }
        }

        /// <summary>
        /// 应用LOD到对象
        /// </summary>
        private void ApplyLOD(GameObject obj, LODLevel lod)
        {
            switch (lod)
            {
                case LODLevel.Culled:
                    obj.SetActive(false);
                    break;

                case LODLevel.Low:
                    obj.SetActive(true);
                    // 可以在这里切换到低细节网格
                    // var meshFilter = obj.GetComponent<MeshFilter>();
                    // if (meshFilter != null) meshFilter.mesh = lowDetailMesh;
                    break;

                case LODLevel.Medium:
                case LODLevel.High:
                    obj.SetActive(true);
                    // 切换到对应细节网格
                    break;
            }
        }

        /// <summary>
        /// 设置LOD距离阈值
        /// </summary>
        public void SetLODDistances(float high, float medium, float low)
        {
            this.highDetailDistance = high;
            this.mediumDetailDistance = medium;
            this.lowDetailDistance = low;
        }
    }
}
