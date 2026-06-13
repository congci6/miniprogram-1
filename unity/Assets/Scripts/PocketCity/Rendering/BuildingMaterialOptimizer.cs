using UnityEngine;
using System.Collections.Generic;

namespace PocketCity.Rendering
{
    /// <summary>
    /// 建筑材质批处理优化器
    /// 使用MaterialPropertyBlock减少DrawCall
    /// </summary>
    public class BuildingMaterialOptimizer : MonoBehaviour
    {
        public static BuildingMaterialOptimizer Instance { get; private set; }

        [SerializeField] private Material sharedBuildingMaterial;
        [SerializeField] private Texture2D facadeAtlas;

        private MaterialPropertyBlock propertyBlock;
        private Dictionary<string, Vector4> uvOffsets = new Dictionary<string, Vector4>();

        // Shader属性ID（缓存避免字符串查找）
        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        private static readonly int UVOffsetID = Shader.PropertyToID("_UVOffset");

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            propertyBlock = new MaterialPropertyBlock();
            InitializeUVOffsets();
        }

        private void InitializeUVOffsets()
        {
            // 假设4x4图集，16个建筑纹理
            float tileSize = 0.25f;

            uvOffsets["residential"] = new Vector4(0f, 0.75f, tileSize, tileSize);
            uvOffsets["commercial"] = new Vector4(0.25f, 0.75f, tileSize, tileSize);
            uvOffsets["industrial"] = new Vector4(0.5f, 0.75f, tileSize, tileSize);
            uvOffsets["office"] = new Vector4(0.75f, 0.75f, tileSize, tileSize);

            uvOffsets["hospital"] = new Vector4(0f, 0.5f, tileSize, tileSize);
            uvOffsets["school"] = new Vector4(0.25f, 0.5f, tileSize, tileSize);
            uvOffsets["police"] = new Vector4(0.5f, 0.5f, tileSize, tileSize);
            uvOffsets["fire"] = new Vector4(0.75f, 0.5f, tileSize, tileSize);

            uvOffsets["park"] = new Vector4(0f, 0.25f, tileSize, tileSize);
            uvOffsets["power"] = new Vector4(0.25f, 0.25f, tileSize, tileSize);
            uvOffsets["water"] = new Vector4(0.5f, 0.25f, tileSize, tileSize);
            uvOffsets["road"] = new Vector4(0.75f, 0.25f, tileSize, tileSize);
        }

        /// <summary>
        /// 应用建筑材质（使用PropertyBlock实例化）
        /// </summary>
        public void ApplyBuildingMaterial(Renderer renderer, string buildingType, Color tintColor)
        {
            if (renderer == null || sharedBuildingMaterial == null)
                return;

            renderer.sharedMaterial = sharedBuildingMaterial;

            propertyBlock.Clear();
            propertyBlock.SetColor(ColorID, tintColor);

            if (facadeAtlas != null)
            {
                propertyBlock.SetTexture(MainTexID, facadeAtlas);

                if (uvOffsets.TryGetValue(buildingType, out Vector4 offset))
                {
                    propertyBlock.SetVector(UVOffsetID, offset);
                }
            }

            renderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// 批量应用材质
        /// </summary>
        public void ApplyBatchMaterials(List<Renderer> renderers, string buildingType, Color tintColor)
        {
            foreach (var renderer in renderers)
            {
                ApplyBuildingMaterial(renderer, buildingType, tintColor);
            }
        }
    }

    /// <summary>
    /// LOD管理器
    /// </summary>
    public class BuildingLODManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float lodDistance0 = 30f; // High quality
        [SerializeField] private float lodDistance1 = 60f; // Medium quality
        // > lodDistance1 = Low quality

        private List<LODBuilding> buildings = new List<LODBuilding>();

        private void Update()
        {
            if (mainCamera == null) return;

            Vector3 camPos = mainCamera.transform.position;

            foreach (var building in buildings)
            {
                if (building.renderer == null) continue;

                float distance = Vector3.Distance(camPos, building.position);

                int targetLOD = distance < lodDistance0 ? 0 :
                                distance < lodDistance1 ? 1 : 2;

                if (targetLOD != building.currentLOD)
                {
                    SwitchLOD(building, targetLOD);
                }
            }
        }

        public void RegisterBuilding(GameObject buildingGO, Renderer renderer, Vector3 position)
        {
            buildings.Add(new LODBuilding
            {
                gameObject = buildingGO,
                renderer = renderer,
                position = position,
                currentLOD = 0
            });
        }

        public void UnregisterBuilding(GameObject buildingGO)
        {
            buildings.RemoveAll(b => b.gameObject == buildingGO);
        }

        private void SwitchLOD(LODBuilding building, int lod)
        {
            building.currentLOD = lod;

            // 简化版：通过缩放模拟LOD（真实项目应切换Mesh）
            float scale = lod switch
            {
                0 => 1f,   // High: 全细节
                1 => 0.9f, // Med: 略简化
                2 => 0.8f, // Low: 简化
                _ => 1f
            };

            if (building.renderer != null)
            {
                // 通过PropertyBlock调整细节（真实项目切换Mesh）
                building.gameObject.transform.localScale = Vector3.one * scale;
            }
        }

        private class LODBuilding
        {
            public GameObject gameObject;
            public Renderer renderer;
            public Vector3 position;
            public int currentLOD;
        }
    }
}
