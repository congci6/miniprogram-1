using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 建筑网格批处理器
    /// 合并相同材质的建筑以减少Draw Call
    /// </summary>
    public class BuildingBatcher : MonoBehaviour
    {
        private Dictionary<Material, List<BuildingInstance>> buildingsByMaterial = new Dictionary<Material, List<BuildingInstance>>();
        private List<GameObject> batchedObjects = new List<GameObject>();
        private bool isDirty = false;
        private float lastBatchTime = 0f;
        private const float MinBatchInterval = 0.5f; // 最小批处理间隔

        private struct BuildingInstance
        {
            public Mesh Mesh;
            public Matrix4x4 Transform;
        }

        public void AddBuilding(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (mesh == null || material == null) return;

            if (!buildingsByMaterial.ContainsKey(material))
            {
                buildingsByMaterial[material] = new List<BuildingInstance>();
            }

            buildingsByMaterial[material].Add(new BuildingInstance
            {
                Mesh = mesh,
                Transform = Matrix4x4.TRS(position, rotation, scale)
            });

            isDirty = true;
        }

        // 自动批处理（带节流）
        public void AutoBatch(Transform parent)
        {
            if (!isDirty) return;
            if (Time.time - lastBatchTime < MinBatchInterval) return;

            BatchAll(parent);
            lastBatchTime = Time.time;
        }

        public void BatchAll(Transform parent)
        {
            ClearBatches();

            foreach (var kvp in buildingsByMaterial)
            {
                var material = kvp.Key;
                var instances = kvp.Value;

                // 分组批处理（每组最多1000个，避免单个网格过大）
                const int maxPerBatch = 1000;
                for (int i = 0; i < instances.Count; i += maxPerBatch)
                {
                    var batch = instances.Skip(i).Take(maxPerBatch).ToList();
                    CreateBatch(batch, material, parent, i / maxPerBatch);
                }
            }

            isDirty = false;
        }

        private void CreateBatch(List<BuildingInstance> instances, Material material, Transform parent, int batchIndex)
        {
            if (instances.Count == 0) return;

            var combines = new CombineInstance[instances.Count];
            for (int i = 0; i < instances.Count; i++)
            {
                combines[i].mesh = instances[i].Mesh;
                combines[i].transform = instances[i].Transform;
            }

            var batchedMesh = new Mesh();
            batchedMesh.CombineMeshes(combines, true, true);
            batchedMesh.name = $"BatchedBuildings_{material.name}_{batchIndex}";

            var batchObj = new GameObject($"BuildingBatch_{material.name}_{batchIndex}");
            batchObj.transform.SetParent(parent, false);

            var filter = batchObj.AddComponent<MeshFilter>();
            filter.mesh = batchedMesh;

            var renderer = batchObj.AddComponent<MeshRenderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            batchedObjects.Add(batchObj);
        }

        public void ClearBatches()
        {
            foreach (var obj in batchedObjects)
            {
                if (obj != null)
                {
                    // 修复: 销毁Mesh避免内存泄漏
                    var filter = obj.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                    {
                        Object.Destroy(filter.sharedMesh);
                    }
                    Object.Destroy(obj);
                }
            }
            batchedObjects.Clear();
        }

        public void Clear()
        {
            ClearBatches();
            buildingsByMaterial.Clear();
            isDirty = false;
        }

        public int GetBatchCount()
        {
            return batchedObjects.Count;
        }

        public int GetBuildingCount()
        {
            return buildingsByMaterial.Values.Sum(list => list.Count);
        }

        public bool IsDirty => isDirty;

        // 获取批处理统计信息
        public BatchStatistics GetStatistics()
        {
            return new BatchStatistics
            {
                TotalBuildings = GetBuildingCount(),
                BatchCount = GetBatchCount(),
                MaterialCount = buildingsByMaterial.Count,
                AverageBuildingsPerBatch = GetBatchCount() > 0 ? GetBuildingCount() / (float)GetBatchCount() : 0
            };
        }

        public struct BatchStatistics
        {
            public int TotalBuildings;
            public int BatchCount;
            public int MaterialCount;
            public float AverageBuildingsPerBatch;
        }
    }
}
