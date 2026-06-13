using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketCity.Input
{
    /// <summary>
    /// 改进的触控识别系统 - 区分点击与拖动
    /// </summary>
    public class ImprovedTouchRecognition : MonoBehaviour
    {
        public static ImprovedTouchRecognition Instance { get; private set; }

        [Header("Thresholds")]
        [SerializeField] private float dragThreshold = 5f; // 像素阈值（从0.25改为5）
        [SerializeField] private float tapMaxDuration = 0.3f; // 点击最大持续时间

        private Vector2 touchStartPosition;
        private float touchStartTime;
        private bool isDragging = false;
        private bool isTouching = false;

        public enum TouchGesture
        {
            None,
            Tap,      // 点击
            Drag,     // 拖动
            LongPress // 长按
        }

        public TouchGesture CurrentGesture { get; private set; } = TouchGesture.None;

        public event System.Action<Vector2> OnTap;
        public event System.Action<Vector2, Vector2> OnDragStart;
        public event System.Action<Vector2, Vector2> OnDragUpdate;
        public event System.Action<Vector2> OnDragEnd;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            ProcessTouch();
        }

        private void ProcessTouch()
        {
            // 移动端触摸
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                ProcessTouchInput(touch.position, touch.phase);
            }
            // PC鼠标
            else
            {
                ProcessMouseInput();
            }
        }

        private void ProcessMouseInput()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnTouchBegin(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                OnTouchMove(UnityEngine.Input.mousePosition);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                OnTouchEnd(UnityEngine.Input.mousePosition);
            }
        }

        private void ProcessTouchInput(Vector2 position, TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                    OnTouchBegin(position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchMove(position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnd(position);
                    break;
            }
        }

        private void OnTouchBegin(Vector2 position)
        {
            // 忽略UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            isTouching = true;
            isDragging = false;
            touchStartPosition = position;
            touchStartTime = Time.time;
            CurrentGesture = TouchGesture.None;
        }

        private void OnTouchMove(Vector2 position)
        {
            if (!isTouching) return;

            float distance = Vector2.Distance(position, touchStartPosition);

            // 检查是否超过拖动阈值
            if (!isDragging && distance > dragThreshold)
            {
                isDragging = true;
                CurrentGesture = TouchGesture.Drag;

                OnDragStart?.Invoke(touchStartPosition, position);
            }

            // 拖动更新
            if (isDragging)
            {
                OnDragUpdate?.Invoke(touchStartPosition, position);
            }
        }

        private void OnTouchEnd(Vector2 position)
        {
            if (!isTouching) return;

            float distance = Vector2.Distance(position, touchStartPosition);
            float duration = Time.time - touchStartTime;

            // 判断手势类型
            if (isDragging)
            {
                // 拖动结束
                CurrentGesture = TouchGesture.Drag;
                OnDragEnd?.Invoke(position);
            }
            else if (distance < dragThreshold && duration < tapMaxDuration)
            {
                // 点击
                CurrentGesture = TouchGesture.Tap;
                OnTap?.Invoke(position);
            }
            else if (distance < dragThreshold && duration >= tapMaxDuration)
            {
                // 长按
                CurrentGesture = TouchGesture.LongPress;
            }

            // 重置
            isTouching = false;
            isDragging = false;
            CurrentGesture = TouchGesture.None;
        }

        /// <summary>
        /// 获取是否正在拖动
        /// </summary>
        public bool IsDragging()
        {
            return isDragging;
        }

        /// <summary>
        /// 获取拖动距离
        /// </summary>
        public float GetDragDistance()
        {
            if (!isTouching) return 0f;
            return Vector2.Distance(GetCurrentPosition(), touchStartPosition);
        }

        /// <summary>
        /// 获取拖动方向
        /// </summary>
        public Vector2 GetDragDirection()
        {
            if (!isTouching) return Vector2.zero;
            return (GetCurrentPosition() - touchStartPosition).normalized;
        }

        private Vector2 GetCurrentPosition()
        {
            if (UnityEngine.Input.touchCount > 0)
                return UnityEngine.Input.GetTouch(0).position;
            else
                return UnityEngine.Input.mousePosition;
        }

        /// <summary>
        /// 设置拖动阈值（运行时可调）
        /// </summary>
        public void SetDragThreshold(float threshold)
        {
            dragThreshold = threshold;
        }

        /// <summary>
        /// 获取当前拖动阈值
        /// </summary>
        public float GetDragThreshold()
        {
            return dragThreshold;
        }
    }
}
