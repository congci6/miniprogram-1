using UnityEngine;
using System.Collections.Generic;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 程序化建筑网格生成器
    /// 使用seed保证稳定性，支持LOD和变体系统
    /// </summary>
    public class ProceduralBuildingMeshGenerator
    {
        // 基础网格缓存
        private static Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
        private static Dictionary<string, LinkedListNode<string>> cacheNodes = new Dictionary<string, LinkedListNode<string>>();
        private static LinkedList<string> cacheAccessOrder = new LinkedList<string>();
        private static int maxCacheSize = 200; // 最多缓存200个网格

        public enum DetailLevel
        {
            High,    // 完整细节：窗户、阳台、屋顶装饰
            Medium,  // 中等细节：简化窗户、基本屋顶
            Low      // 低细节：简单方块
        }

        /// <summary>
        /// 生成建筑网格（带缓存和变体支持）
        /// </summary>
        public static Mesh GenerateBuildingMesh(string buildingType, int seed, DetailLevel detail)
        {
            var cacheKey = $"{buildingType}_{seed}_{detail}";

            // 缓存命中
            if (meshCache.ContainsKey(cacheKey))
            {
                // 更新访问顺序（LRU）
                UpdateCacheAccess(cacheKey);
                return meshCache[cacheKey];
            }

            // 生成新网格
            Random.InitState(seed);
            var variant = BuildingVariantGenerator.GenerateVariant(buildingType, seed);
            var mesh = CreateBuildingMesh(buildingType, variant, detail);

            // 添加到缓存（LRU管理）
            AddToCache(cacheKey, mesh);

            return mesh;
        }

        private static void AddToCache(string key, Mesh mesh)
        {
            // 如果缓存已满，移除最旧的
            if (meshCache.Count >= maxCacheSize)
            {
                var oldestNode = cacheAccessOrder.First;
                if (oldestNode != null)
                {
                    var oldestKey = oldestNode.Value;
                    cacheAccessOrder.RemoveFirst();
                    cacheNodes.Remove(oldestKey);

                    if (meshCache.TryGetValue(oldestKey, out var oldMesh))
                    {
                        if (oldMesh != null)
                            Object.Destroy(oldMesh);
                        meshCache.Remove(oldestKey);
                    }
                }
            }

            meshCache[key] = mesh;
            var node = cacheAccessOrder.AddLast(key);
            cacheNodes[key] = node;
        }

        private static void UpdateCacheAccess(string key)
        {
            // O(1) LRU：移到链表末尾
            if (cacheNodes.TryGetValue(key, out var node))
            {
                cacheAccessOrder.Remove(node);
                cacheAccessOrder.AddLast(node);
            }
        }

        private static Mesh CreateBuildingMesh(string buildingType, BuildingVariant variant, DetailLevel detail)
        {
            // 根据建筑类型确定基础参数
            var height = GetBuildingHeight(buildingType) * variant.HeightScale;
            var width = GetBuildingWidth(buildingType) * variant.WidthScale;
            var depth = GetBuildingDepth(buildingType) * variant.DepthScale;

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            // 主体建筑
            AddBuildingBody(vertices, triangles, uvs, width, height, depth);

            // 根据细节级别添加装饰
            if (detail == DetailLevel.High)
            {
                AddWindows(vertices, triangles, uvs, width, height, depth, 4, variant.WindowPattern);
                if (variant.HasBalcony)
                {
                    AddBalconies(vertices, triangles, uvs, width, height, depth);
                }
                AddRoofWithVariant(vertices, triangles, uvs, width, depth, height, variant.RoofType, variant.HasRoofDetail);
                AddEntrance(vertices, triangles, uvs, width, depth);
            }
            else if (detail == DetailLevel.Medium)
            {
                AddWindows(vertices, triangles, uvs, width, height, depth, 2, variant.WindowPattern);
                AddSimpleRoof(vertices, triangles, uvs, width, depth, height);
            }
            // Low detail - 只有主体

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void AddBuildingBody(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float h, float d)
        {
            int start = v.Count;

            // 前面
            v.Add(new Vector3(0, 0, 0)); v.Add(new Vector3(w, 0, 0));
            v.Add(new Vector3(w, h, 0)); v.Add(new Vector3(0, h, 0));
            // 后面
            v.Add(new Vector3(w, 0, d)); v.Add(new Vector3(0, 0, d));
            v.Add(new Vector3(0, h, d)); v.Add(new Vector3(w, h, d));
            // 左面
            v.Add(new Vector3(0, 0, d)); v.Add(new Vector3(0, 0, 0));
            v.Add(new Vector3(0, h, 0)); v.Add(new Vector3(0, h, d));
            // 右面
            v.Add(new Vector3(w, 0, 0)); v.Add(new Vector3(w, 0, d));
            v.Add(new Vector3(w, h, d)); v.Add(new Vector3(w, h, 0));

            // 前
            t.Add(start+0); t.Add(start+1); t.Add(start+2);
            t.Add(start+0); t.Add(start+2); t.Add(start+3);
            // 后
            t.Add(start+4); t.Add(start+5); t.Add(start+6);
            t.Add(start+4); t.Add(start+6); t.Add(start+7);
            // 左
            t.Add(start+8); t.Add(start+9); t.Add(start+10);
            t.Add(start+8); t.Add(start+10); t.Add(start+11);
            // 右
            t.Add(start+12); t.Add(start+13); t.Add(start+14);
            t.Add(start+12); t.Add(start+14); t.Add(start+15);

            // UV
            for (int i = 0; i < 16; i++)
                uvs.Add(new Vector2(i % 2, i / 2 % 2));
        }

        private static void AddWindows(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float h, float d, int floors, int pattern)
        {
            float windowSize = 0.15f;
            float spacing = h / (floors + 1);

            for (int floor = 1; floor <= floors; floor++)
            {
                float y = spacing * floor;

                // 根据pattern选择窗户排列
                if (pattern == 0) // 标准两窗
                {
                    AddWindow(v, t, uvs, w * 0.3f, y, 0.01f, windowSize);
                    AddWindow(v, t, uvs, w * 0.7f, y, 0.01f, windowSize);
                }
                else if (pattern == 1) // 三窗
                {
                    AddWindow(v, t, uvs, w * 0.25f, y, 0.01f, windowSize * 0.9f);
                    AddWindow(v, t, uvs, w * 0.5f, y, 0.01f, windowSize * 0.9f);
                    AddWindow(v, t, uvs, w * 0.75f, y, 0.01f, windowSize * 0.9f);
                }
                else if (pattern == 2) // 大窗
                {
                    AddWindow(v, t, uvs, w * 0.5f, y, 0.01f, windowSize * 1.5f);
                }
                else if (pattern == 3) // 密集小窗
                {
                    AddWindow(v, t, uvs, w * 0.2f, y, 0.01f, windowSize * 0.7f);
                    AddWindow(v, t, uvs, w * 0.4f, y, 0.01f, windowSize * 0.7f);
                    AddWindow(v, t, uvs, w * 0.6f, y, 0.01f, windowSize * 0.7f);
                    AddWindow(v, t, uvs, w * 0.8f, y, 0.01f, windowSize * 0.7f);
                }
                else // pattern 4 - 单大窗
                {
                    AddWindow(v, t, uvs, w * 0.5f, y, 0.01f, windowSize * 1.8f);
                }
            }
        }

        private static void AddWindow(List<Vector3> v, List<int> t, List<Vector2> uvs, float x, float y, float z, float size)
        {
            int start = v.Count;
            v.Add(new Vector3(x - size/2, y - size/2, z));
            v.Add(new Vector3(x + size/2, y - size/2, z));
            v.Add(new Vector3(x + size/2, y + size/2, z));
            v.Add(new Vector3(x - size/2, y + size/2, z));

            t.Add(start); t.Add(start+1); t.Add(start+2);
            t.Add(start); t.Add(start+2); t.Add(start+3);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
        }

        private static void AddRoof(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d, float h)
        {
            AddRoofWithVariant(v, t, uvs, w, d, h, Random.Range(0, 3), false);
        }

        private static void AddRoofWithVariant(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d, float h, int roofType, bool addDetail)
        {
            if (roofType == 0) // 平顶
            {
                AddFlatRoof(v, t, uvs, w, d, h);
                if (addDetail)
                {
                    AddRoofDetail(v, t, uvs, w, d, h);
                }
            }
            else if (roofType == 1) // 尖顶
            {
                AddPeakedRoof(v, t, uvs, w, d, h);
            }
            else // 圆顶样式（简化为平顶+装饰）
            {
                AddFlatRoof(v, t, uvs, w, d, h);
                if (addDetail)
                {
                    AddRoofDetail(v, t, uvs, w, d, h);
                }
            }
        }

        private static void AddBalconies(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float h, float d)
        {
            // 在中层添加阳台
            float balconyY = h * 0.6f;
            float balconyDepth = 0.12f;
            float balconyWidth = w * 0.4f;

            int start = v.Count;
            v.Add(new Vector3(w * 0.3f, balconyY, -balconyDepth));
            v.Add(new Vector3(w * 0.7f, balconyY, -balconyDepth));
            v.Add(new Vector3(w * 0.7f, balconyY, 0));
            v.Add(new Vector3(w * 0.3f, balconyY, 0));

            t.Add(start); t.Add(start+1); t.Add(start+2);
            t.Add(start); t.Add(start+2); t.Add(start+3);

            for (int i = 0; i < 4; i++)
                uvs.Add(new Vector2(i % 2, i / 2));
        }

        private static void AddRoofDetail(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d, float h)
        {
            // 添加屋顶装饰（小塔楼）
            float detailSize = w * 0.15f;
            int start = v.Count;

            v.Add(new Vector3(w * 0.5f - detailSize/2, h, d * 0.5f - detailSize/2));
            v.Add(new Vector3(w * 0.5f + detailSize/2, h, d * 0.5f - detailSize/2));
            v.Add(new Vector3(w * 0.5f + detailSize/2, h + detailSize * 0.8f, d * 0.5f - detailSize/2));
            v.Add(new Vector3(w * 0.5f - detailSize/2, h + detailSize * 0.8f, d * 0.5f - detailSize/2));

            t.Add(start); t.Add(start+1); t.Add(start+2);
            t.Add(start); t.Add(start+2); t.Add(start+3);

            for (int i = 0; i < 4; i++)
                uvs.Add(new Vector2(0.5f, 0.5f));
        }

        private static void AddSimpleRoof(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d, float h)
        {
            AddFlatRoof(v, t, uvs, w, d, h);
        }

        private static void AddFlatRoof(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d, float h)
        {
            int start = v.Count;
            v.Add(new Vector3(0, h, 0));
            v.Add(new Vector3(w, h, 0));
            v.Add(new Vector3(w, h, d));
            v.Add(new Vector3(0, h, d));

            t.Add(start); t.Add(start+1); t.Add(start+2);
            t.Add(start); t.Add(start+2); t.Add(start+3);

            for (int i = 0; i < 4; i++)
                uvs.Add(new Vector2(i % 2, i / 2));
        }

        private static void AddPeakedRoof(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d, float h)
        {
            float roofHeight = h * 0.2f;
            int start = v.Count;

            v.Add(new Vector3(w/2, h + roofHeight, d/2)); // 顶点
            v.Add(new Vector3(0, h, 0));
            v.Add(new Vector3(w, h, 0));
            v.Add(new Vector3(w, h, d));
            v.Add(new Vector3(0, h, d));

            // 四个面
            t.Add(start); t.Add(start+1); t.Add(start+2);
            t.Add(start); t.Add(start+2); t.Add(start+3);
            t.Add(start); t.Add(start+3); t.Add(start+4);
            t.Add(start); t.Add(start+4); t.Add(start+1);

            for (int i = 0; i < 5; i++)
                uvs.Add(new Vector2(0.5f, 0.5f));
        }

        private static void AddEntrance(List<Vector3> v, List<int> t, List<Vector2> uvs, float w, float d)
        {
            float doorWidth = 0.3f;
            float doorHeight = 0.5f;
            AddWindow(v, t, uvs, w/2, doorHeight/2, -0.01f, doorWidth);
        }

        private static float GetBuildingHeight(string type)
        {
            if (type.Contains("apartment") || type.Contains("office"))
                return Random.Range(2.5f, 4.0f);
            if (type.Contains("residential") || type.Contains("commercial"))
                return Random.Range(1.5f, 2.5f);
            if (type.Contains("industrial") || type.Contains("warehouse"))
                return Random.Range(1.2f, 2.0f);
            return Random.Range(1.0f, 2.0f);
        }

        private static float GetBuildingWidth(string type)
        {
            return Random.Range(0.7f, 0.9f);
        }

        private static float GetBuildingDepth(string type)
        {
            return Random.Range(0.7f, 0.9f);
        }

        /// <summary>
        /// 清除缓存（用于内存管理）
        /// </summary>
        public static void ClearCache()
        {
            foreach (var mesh in meshCache.Values)
            {
                if (mesh != null)
                    Object.Destroy(mesh);
            }
            meshCache.Clear();
            cacheAccessOrder.Clear();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static CacheStatistics GetCacheStatistics()
        {
            return new CacheStatistics
            {
                CachedMeshCount = meshCache.Count,
                MaxCacheSize = maxCacheSize,
                CacheUsagePercent = (meshCache.Count / (float)maxCacheSize) * 100f
            };
        }

        /// <summary>
        /// 设置最大缓存大小
        /// </summary>
        public static void SetMaxCacheSize(int size)
        {
            maxCacheSize = Mathf.Max(10, size); // 最少10个
        }

        public struct CacheStatistics
        {
            public int CachedMeshCount;
            public int MaxCacheSize;
            public float CacheUsagePercent;
        }
    }
}
