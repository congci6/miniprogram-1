using UnityEngine;
using UnityEngine.UI;

namespace PocketCity.UI
{
    /// <summary>
    /// 基于RenderTexture的高性能小地图
    /// </summary>
    public class RenderTextureMinimapSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform viewportIndicator;
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private int textureSize = 512;
        [SerializeField] private float cameraHeight = 100f;
        [SerializeField] private LayerMask minimapLayers = -1;

        private Camera minimapCamera;
        private RenderTexture minimapRT;

        private void Start()
        {
            InitializeMinimapCamera();
        }

        private void InitializeMinimapCamera()
        {
            // 创建RenderTexture
            minimapRT = new RenderTexture(textureSize, textureSize, 16);
            minimapRT.antiAliasing = 2;
            minimapRT.filterMode = FilterMode.Bilinear;

            if (minimapImage != null)
            {
                minimapImage.texture = minimapRT;
            }

            // 创建俯视相机
            GameObject camGO = new GameObject("MinimapCamera");
            camGO.transform.SetParent(transform);

            minimapCamera = camGO.AddComponent<Camera>();
            minimapCamera.targetTexture = minimapRT;
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 32f; // 根据地图大小调整
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.2f, 0.3f, 0.2f);
            minimapCamera.cullingMask = minimapLayers;
            minimapCamera.depth = -10; // 低于主相机
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
            UpdateViewportIndicator();
        }

        private void UpdateCameraPosition()
        {
            if (mainCamera == null || minimapCamera == null) return;

            // 跟随主相机位置
            Vector3 pos = mainCamera.transform.position;
            minimapCamera.transform.position = new Vector3(pos.x, cameraHeight, pos.z);
        }

        private void UpdateViewportIndicator()
        {
            if (mainCamera == null || viewportIndicator == null) return;

            // 简化：指示器固定在中心
            viewportIndicator.anchoredPosition = Vector2.zero;

            // 根据主相机FOV调整指示器大小
            float fov = mainCamera.orthographic ? mainCamera.orthographicSize : mainCamera.fieldOfView;
            float scale = fov / minimapCamera.orthographicSize;
            viewportIndicator.localScale = Vector3.one * Mathf.Clamp(scale, 0.1f, 1f);
        }

        public void OnMinimapClicked(Vector2 normalizedPos)
        {
            if (mainCamera == null || minimapCamera == null) return;

            // 转换为世界坐标
            float halfSize = minimapCamera.orthographicSize;
            Vector3 camPos = minimapCamera.transform.position;

            float worldX = camPos.x + (normalizedPos.x - 0.5f) * halfSize * 2f;
            float worldZ = camPos.z + (normalizedPos.y - 0.5f) * halfSize * 2f;

            mainCamera.transform.position = new Vector3(worldX, mainCamera.transform.position.y, worldZ);
        }

        private void OnDestroy()
        {
            if (minimapRT != null)
            {
                minimapRT.Release();
            }
        }
    }
}
