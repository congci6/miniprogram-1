using UnityEngine;
using UnityEngine.UI;
using PocketCity.Core;
using PocketCity.Simulation;
using Unity.Collections;

namespace PocketCity.UI
{
    public class MinimapSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform viewportIndicator;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CitySimulationCore simulation;

        [Header("Settings")]
        [SerializeField] private int textureSize = 256;
        [SerializeField] private float updateInterval = 0.5f;

        private Texture2D minimapTexture;
        private float updateTimer;
        private NativeArray<Color32> pixelBuffer;

        // 颜色映射
        private readonly Color32 emptyColor = new Color32(51, 76, 51, 255);
        private readonly Color32 roadColor = new Color32(102, 102, 102, 255);
        private readonly Color32 residentialColor = new Color32(76, 204, 76, 255);
        private readonly Color32 commercialColor = new Color32(76, 128, 255, 255);
        private readonly Color32 industrialColor = new Color32(230, 179, 51, 255);
        private readonly Color32 serviceColor = new Color32(255, 76, 76, 255);

        private void Start()
        {
            InitializeTexture();
            UpdateMinimap();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateMinimap();
            }

            UpdateViewportIndicator();
        }

        private void InitializeTexture()
        {
            minimapTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            minimapTexture.filterMode = FilterMode.Point;

            // 初始化NativeArray缓冲区
            int totalPixels = textureSize * textureSize;
            pixelBuffer = new NativeArray<Color32>(totalPixels, Allocator.Persistent);

            if (minimapImage != null)
            {
                minimapImage.texture = minimapTexture;
            }
        }

        private void UpdateMinimap()
        {
            if (simulation == null || minimapTexture == null || !pixelBuffer.IsCreated) return;

            int gridWidth = simulation.Grid.Width;
            int gridHeight = simulation.Grid.Height;

            // 使用NativeArray直接写入，避免SetPixel
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int gridX = x * gridWidth / textureSize;
                    int gridY = y * gridHeight / textureSize;

                    Color32 pixelColor = GetGridColor(gridX, gridY);
                    int index = y * textureSize + x;
                    pixelBuffer[index] = pixelColor;
                }
            }

            // 一次性上传所有像素
            minimapTexture.SetPixelData(pixelBuffer, 0);
            minimapTexture.Apply(false);
        }

        private Color32 GetGridColor(int x, int y)
        {
            var pos = new GridPos(x, y);

            // 检查道路
            if (simulation.Grid.GetRoadType(pos) != RoadType.None)
                return roadColor;

            // 检查建筑
            var buildingId = simulation.Grid.FindBuildingIdAt(pos);
            if (!string.IsNullOrEmpty(buildingId))
            {
                var building = simulation.FindPlacedBuilding(buildingId);
                if (building != null)
                {
                    var definition = simulation.Config.GetBuilding(building.ConfigId);
                    if (definition != null)
                    {
                        return definition.Category switch
                        {
                            BuildingCategory.Residential => residentialColor,
                            BuildingCategory.Commercial => commercialColor,
                            BuildingCategory.Industrial => industrialColor,
                            BuildingCategory.Service => serviceColor,
                            _ => emptyColor
                        };
                    }
                }
            }

            // 检查分区
            var zone = simulation.Grid.GetZoneType(pos);
            if (zone != ZoneType.None)
            {
                return zone switch
                {
                    ZoneType.Residential => (Color)residentialColor * 0.5f,
                    ZoneType.Commercial => (Color)commercialColor * 0.5f,
                    ZoneType.Industrial => (Color)industrialColor * 0.5f,
                    _ => emptyColor
                };
            }

            return emptyColor;
        }

        private void OnDestroy()
        {
            // 清理NativeArray避免内存泄漏
            if (pixelBuffer.IsCreated)
            {
                pixelBuffer.Dispose();
            }
        }

        private void UpdateViewportIndicator()
        {
            if (mainCamera == null || viewportIndicator == null || simulation == null) return;

            // 简化版：显示相机中心位置
            float normalizedX = mainCamera.transform.position.x / simulation.Grid.Width;
            float normalizedY = mainCamera.transform.position.z / simulation.Grid.Height;

            viewportIndicator.anchoredPosition = new Vector2(
                normalizedX * textureSize,
                normalizedY * textureSize
            );
        }

        public void OnMinimapClicked(Vector2 clickPos)
        {
            if (mainCamera == null || simulation == null) return;

            // 将点击位置转换为世界坐标
            float worldX = (clickPos.x / textureSize) * simulation.Grid.Width;
            float worldZ = (clickPos.y / textureSize) * simulation.Grid.Height;

            mainCamera.transform.position = new Vector3(worldX, mainCamera.transform.position.y, worldZ);
        }
    }
}
