using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketCity.Input
{
    /// <summary>
    /// 长按连续操作系统 - 长按划区持续放置
    /// </summary>
    public class LongPressOperationSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float longPressThreshold = 0.3f; // 长按阈值（秒）
        [SerializeField] private float continuousInterval = 0.1f; // 连续操作间隔

        [Header("References")]
        [SerializeField] private Camera mainCamera;

        private bool isPressed = false;
        private bool isLongPress = false;
        private float pressStartTime;
        private float lastOperationTime;
        private Vector3 pressStartPosition;
        private int longPressContinueCount = 0; // 连续操作计数

        public bool IsLongPressActive => isLongPress;

        public event System.Action<Vector3> OnLongPressStart;
        public event System.Action<Vector3> OnLongPressContinue;
        public event System.Action OnLongPressEnd;

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // 检测按下
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnPressDown();
            }

            // 检测持续按住
            if (UnityEngine.Input.GetMouseButton(0))
            {
                OnPressHold();
            }

            // 检测松开
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                OnPressUp();
            }
        }

        private void OnPressDown()
        {
            // 忽略UI点击
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            isPressed = true;
            pressStartTime = Time.time;
            pressStartPosition = UnityEngine.Input.mousePosition;
            isLongPress = false;
        }

        private void OnPressHold()
        {
            if (!isPressed) return;

            float pressDuration = Time.time - pressStartTime;

            // 检测是否达到长按阈值
            if (!isLongPress && pressDuration >= longPressThreshold)
            {
                // 确认是长按（没有大幅移动）
                float dragDistance = Vector3.Distance(UnityEngine.Input.mousePosition, pressStartPosition);
                if (dragDistance < 50f) // 50像素内认为是静止
                {
                    StartLongPress();
                }
            }

            // 长按模式下的连续操作
            if (isLongPress)
            {
                float timeSinceLastOp = Time.time - lastOperationTime;
                if (timeSinceLastOp >= continuousInterval)
                {
                    ContinueLongPress();
                    lastOperationTime = Time.time;
                }
            }
        }

        private void OnPressUp()
        {
            if (isLongPress)
            {
                EndLongPress();
            }

            isPressed = false;
            isLongPress = false;
        }

        private void StartLongPress()
        {
            isLongPress = true;
            lastOperationTime = Time.time;

            Vector3 worldPos = GetWorldPosition(UnityEngine.Input.mousePosition);
            OnLongPressStart?.Invoke(worldPos);

            // 震动反馈（移动端）
            if (Application.isMobilePlatform)
            {
            }

            Debug.Log("长按模式启动");
        }

        private void ContinueLongPress()
        {
            Vector3 worldPos = GetWorldPosition(UnityEngine.Input.mousePosition);
            OnLongPressContinue?.Invoke(worldPos);
        }

        private void EndLongPress()
        {
            OnLongPressEnd?.Invoke();
            Debug.Log("长按模式结束");
        }

        private Vector3 GetWorldPosition(Vector3 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.point;
            }

            // 默认平面投射
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// 获取当前拖动路径
        /// </summary>
        public Vector3[] GetDragPath()
        {
            // TODO: 记录拖动轨迹
            return new Vector3[0];
        }
    }

    /// <summary>
    /// 长按建造辅助器 - 连接到建造系统
    /// </summary>
    public class LongPressBuildHelper : MonoBehaviour
    {
        [SerializeField] private LongPressOperationSystem longPressSystem;
        [SerializeField] private Simulation.CitySimulationCore simulation;

        private string currentBuildingId;
        private bool isBuildMode = false;
        private int longPressContinueCount = 0;

        private void Start()
        {
            if (longPressSystem != null)
            {
                longPressSystem.OnLongPressStart += OnLongPressStart;
                longPressSystem.OnLongPressContinue += OnLongPressContinue;
                longPressSystem.OnLongPressEnd += OnLongPressEnd;
            }
        }

        public void StartBuildMode(string buildingId)
        {
            currentBuildingId = buildingId;
            isBuildMode = true;
        }

        public void StopBuildMode()
        {
            isBuildMode = false;
            currentBuildingId = null;
        }

        private void OnLongPressStart(Vector3 worldPos)
        {
            if (!isBuildMode || string.IsNullOrEmpty(currentBuildingId)) return;

            TryPlaceBuilding(worldPos);
        }

        private void OnLongPressContinue(Vector3 worldPos)
        {
            if (!isBuildMode || string.IsNullOrEmpty(currentBuildingId)) return;

            TryPlaceBuilding(worldPos);
        }

        private void OnLongPressEnd()
        {
            // 长按结束
        }

        private void TryPlaceBuilding(Vector3 worldPos)
        {
            if (simulation == null) return;

            Core.GridPos gridPos = new Core.GridPos(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.z)
            );

            // 尝试放置（简化版，无预览）
            bool success = simulation.TryPlaceBuilding(
                currentBuildingId,
                gridPos,
                out var preview
            );

            if (success)
            {
                longPressContinueCount++;
            }
        }

        private void OnDestroy()
        {
            if (longPressSystem != null)
            {
                longPressSystem.OnLongPressStart -= OnLongPressStart;
                longPressSystem.OnLongPressContinue -= OnLongPressContinue;
                longPressSystem.OnLongPressEnd -= OnLongPressEnd;
            }
        }
    }
}
