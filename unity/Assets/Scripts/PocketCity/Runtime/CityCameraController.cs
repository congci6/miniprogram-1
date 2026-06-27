using UnityEngine;
using UnityEngine.EventSystems;

namespace PocketCity.Runtime
{
    public sealed class CityCameraController : MonoBehaviour
    {
        // MINIMAP_CAMERA_CONTROLS: runtime HUD minimap buttons route into ZoomIn, FrameMap, ZoomOut and AdjustZoom here.
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 mapSize = new Vector2(64f, 64f);
        [SerializeField] private float keyboardPanSpeed = 24f;
        [SerializeField] private float dragPanSpeed = 0.035f;
        [SerializeField] private float touchDragPanSpeed = 0.03f;
        [SerializeField] private float zoomSpeed = 8f;
        [SerializeField] private float panSmoothTime = 0.08f;
        [SerializeField] private float zoomSmoothTime = 0.08f;
        [SerializeField] private float pinchZoomSpeed = 0.03f;
        [SerializeField] private float minOrthographicSize = 12f;
        [SerializeField] private float maxOrthographicSize = 42f;

        private Vector3 lastPointerPosition;
        private Vector2 lastTouchPosition;
        private int activeTouchFingerId = -1;
        private float lastPinchDistance;
        private Vector3 targetPosition;
        private Vector3 panVelocity;
        private float targetOrthographicSize;
        private float zoomVelocity;
        private bool hasCameraState;
        private bool isCameraSettling;
        private Camera stateCamera;
        private string lastCameraFeedback = string.Empty;
        private float lastCameraFeedbackTime;

        public float CurrentZoom => targetCamera != null ? targetCamera.orthographicSize : 0f;
        public float TargetZoom => targetOrthographicSize > 0f ? targetOrthographicSize : CurrentZoom;
        public float NormalizedZoom => Mathf.InverseLerp(maxOrthographicSize, minOrthographicSize, TargetZoom);
        public bool CanZoomIn => targetCamera != null && TargetZoom > minOrthographicSize + 0.05f;
        public bool CanZoomOut => targetCamera != null && TargetZoom < maxOrthographicSize - 0.05f;
        public bool IsCameraSettling => isCameraSettling;
        public string LastCameraFeedback => lastCameraFeedback;
        public float LastCameraFeedbackTime => lastCameraFeedbackTime;

        private void Awake()
        {
            if (EnsureTargetCamera())
            {
                SyncCameraState();
            }
        }

        private void Update()
        {
            if (!EnsureTargetCamera())
            {
                return;
            }

            SyncCameraState();
            HandleKeyboardPan();
            if (UnityEngine.Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            else
            {
                ResetTouchState();
                HandleMouseDrag();
                HandleMouseZoom();
            }

            ClampTargetState();
            ApplyCameraSmoothing();
        }

        public void SetMapSize(float width, float height)
        {
            mapSize = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
            ClampCamera();
        }

        public void ZoomIn()
        {
            AdjustZoom(1f);
        }

        public void ZoomOut()
        {
            AdjustZoom(-1f);
        }

        public void FrameMap()
        {
            if (!EnsureTargetCamera())
            {
                return;
            }

            SyncCameraState();
            var center = new Vector3(mapSize.x * 0.5f, 0f, mapSize.y * 0.5f);
            var forward = targetCamera.transform.forward;
            var isLookingDown = Mathf.Abs(forward.y) > 0.99f;
            var distance = isLookingDown
                ? 64f
                : Mathf.Max(1f, (targetCamera.transform.position.y - center.y) / -forward.y);
            targetPosition = center - forward * distance;
            targetOrthographicSize = Mathf.Clamp(Mathf.Max(mapSize.x, mapSize.y) * 0.42f, minOrthographicSize, maxOrthographicSize);
            ClampTargetState();
            RecordCameraFeedback("Map Framed");
        }

        private bool EnsureTargetCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera != null;
        }

        private void SyncCameraState()
        {
            if (!hasCameraState || stateCamera != targetCamera)
            {
                targetPosition = targetCamera.transform.position;
                targetOrthographicSize = Mathf.Clamp(targetCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
                panVelocity = Vector3.zero;
                zoomVelocity = 0f;
                hasCameraState = true;
                stateCamera = targetCamera;
                ClampTargetState();
            }
        }

        private void HandleKeyboardPan()
        {
            var x = UnityEngine.Input.GetAxisRaw("Horizontal");
            var z = UnityEngine.Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(x) < 0.01f && Mathf.Abs(z) < 0.01f)
            {
                return;
            }

            var right = targetCamera.transform.right;
            var forward = Vector3.Cross(right, Vector3.up).normalized;
            var input = Vector2.ClampMagnitude(new Vector2(x, z), 1f);
            var delta = (right * input.x + forward * input.y) * keyboardPanSpeed * Time.deltaTime;
            delta.y = 0f;
            targetPosition += delta;
            RecordCameraFeedback("Keyboard Pan");
        }

        private void HandleMouseDrag()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetMouseButtonDown(2))
            {
                lastPointerPosition = UnityEngine.Input.mousePosition;
            }

            if (!UnityEngine.Input.GetMouseButton(0) && !UnityEngine.Input.GetMouseButton(1) && !UnityEngine.Input.GetMouseButton(2))
            {
                return;
            }

            if (IsPointerOverUi())
            {
                lastPointerPosition = UnityEngine.Input.mousePosition;
                return;
            }

            var delta = UnityEngine.Input.mousePosition - lastPointerPosition;
            lastPointerPosition = UnityEngine.Input.mousePosition;
            if (delta.sqrMagnitude < 0.25f)
            {
                return;
            }

            targetPosition += ScreenDeltaToWorldPan(delta, dragPanSpeed);
            RecordCameraFeedback("Mouse Drag");
        }

        private void HandleMouseZoom()
        {
            var scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            if (IsPointerOverUi())
            {
                return;
            }

            ZoomTargetBy(scroll * zoomSpeed, UnityEngine.Input.mousePosition, scroll > 0f ? "Zoom In" : "Zoom Out");
        }

        private void AdjustZoom(float direction)
        {
            if (!EnsureTargetCamera())
            {
                return;
            }

            SyncCameraState();
            ZoomTargetBy(direction * zoomSpeed * 1.5f, GetScreenCenter(), direction > 0f ? "Zoom In" : "Zoom Out");
        }

        private void HandleTouchInput()
        {
            if (UnityEngine.Input.touchCount == 1)
            {
                lastPinchDistance = 0f;
                HandleSingleTouchPan(UnityEngine.Input.GetTouch(0));
                return;
            }

            activeTouchFingerId = -1;
            if (UnityEngine.Input.touchCount == 2)
            {
                HandleTouchZoom();
                return;
            }

            lastPinchDistance = 0f;
        }

        private void HandleSingleTouchPan(Touch touch)
        {
            if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
            {
                activeTouchFingerId = -1;
                return;
            }

            if (IsTouchOverUi(touch))
            {
                activeTouchFingerId = -1;
                lastTouchPosition = touch.position;
                return;
            }

            if (touch.phase == TouchPhase.Began || activeTouchFingerId != touch.fingerId)
            {
                activeTouchFingerId = touch.fingerId;
                lastTouchPosition = touch.position;
                return;
            }

            var delta = touch.position - lastTouchPosition;
            lastTouchPosition = touch.position;
            if (delta.sqrMagnitude < 0.25f)
            {
                return;
            }

            targetPosition += ScreenDeltaToWorldPan(delta, touchDragPanSpeed);
            RecordCameraFeedback("Touch Drag");
        }

        private void HandleTouchZoom()
        {
            if (UnityEngine.Input.touchCount != 2)
            {
                lastPinchDistance = 0f;
                return;
            }

            var a = UnityEngine.Input.GetTouch(0);
            var b = UnityEngine.Input.GetTouch(1);
            if (IsTouchOverUi(a) || IsTouchOverUi(b))
            {
                lastPinchDistance = 0f;
                return;
            }

            var distance = Vector2.Distance(a.position, b.position);
            if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
            {
                lastPinchDistance = distance;
                return;
            }

            if (lastPinchDistance > 0f)
            {
                var delta = distance - lastPinchDistance;
                if (Mathf.Abs(delta) > 0.75f)
                {
                    var center = (a.position + b.position) * 0.5f;
                    ZoomTargetBy(delta * pinchZoomSpeed, center, "Pinch Zoom");
                }
            }

            lastPinchDistance = distance;
        }

        private Vector3 ScreenDeltaToWorldPan(Vector2 delta, float speed)
        {
            var right = targetCamera.transform.right;
            var forward = Vector3.Cross(right, Vector3.up).normalized;
            var worldDelta = (-right * delta.x - forward * delta.y) * speed * targetOrthographicSize;
            worldDelta.y = 0f;
            return worldDelta;
        }

        private void ZoomTargetBy(float amount, Vector2 screenPoint, string feedback)
        {
            var previousZoom = targetOrthographicSize;
            var nextZoom = Mathf.Clamp(previousZoom - amount, minOrthographicSize, maxOrthographicSize);
            if (Mathf.Abs(nextZoom - previousZoom) < 0.001f)
            {
                targetOrthographicSize = nextZoom;
                return;
            }

            var anchorBefore = GroundPointAtScreen(screenPoint, targetPosition, previousZoom);
            targetOrthographicSize = nextZoom;
            var anchorAfter = GroundPointAtScreen(screenPoint, targetPosition, nextZoom);
            var reanchoredPosition = targetPosition + (anchorBefore - anchorAfter);
            reanchoredPosition.y = targetPosition.y;
            targetPosition = reanchoredPosition;
            ClampTargetState();
            RecordCameraFeedback(feedback);
        }

        private Vector3 GroundPointAtScreen(Vector2 screenPoint, Vector3 cameraPosition, float orthographicSize)
        {
            var viewport = targetCamera.ScreenToViewportPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
            var halfHeight = Mathf.Max(0.01f, orthographicSize);
            var halfWidth = halfHeight * Mathf.Max(0.01f, targetCamera.aspect);
            var origin = cameraPosition
                + targetCamera.transform.right * ((viewport.x - 0.5f) * 2f * halfWidth)
                + targetCamera.transform.up * ((viewport.y - 0.5f) * 2f * halfHeight);
            var forward = targetCamera.transform.forward;
            if (Mathf.Abs(forward.y) < 0.001f)
            {
                return origin;
            }

            return origin + forward * (-origin.y / forward.y);
        }

        private Vector2 GetScreenCenter()
        {
            var width = targetCamera != null && targetCamera.pixelWidth > 0 ? targetCamera.pixelWidth : Screen.width;
            var height = targetCamera != null && targetCamera.pixelHeight > 0 ? targetCamera.pixelHeight : Screen.height;
            return new Vector2(width * 0.5f, height * 0.5f);
        }

        private void ClampCamera()
        {
            if (!EnsureTargetCamera())
            {
                return;
            }

            ClampTargetState();
            targetCamera.transform.position = ClampPosition(targetCamera.transform.position, targetCamera.orthographicSize);
        }

        private void ClampTargetState()
        {
            if (!hasCameraState)
            {
                return;
            }

            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
            targetPosition = ClampPosition(targetPosition, targetOrthographicSize);
        }

        private Vector3 ClampPosition(Vector3 position, float orthographicSize)
        {
            var margin = orthographicSize * 0.65f;
            position.x = Mathf.Clamp(position.x, -margin, mapSize.x + margin);
            position.z = Mathf.Clamp(position.z, -margin, mapSize.y + margin);
            return position;
        }

        private void ApplyCameraSmoothing()
        {
            var smoothPan = Mathf.Max(0.001f, panSmoothTime);
            var smoothZoom = Mathf.Max(0.001f, zoomSmoothTime);
            var nextZoom = Mathf.Clamp(
                Mathf.SmoothDamp(targetCamera.orthographicSize, targetOrthographicSize, ref zoomVelocity, smoothZoom),
                minOrthographicSize,
                maxOrthographicSize);
            var nextPosition = Vector3.SmoothDamp(targetCamera.transform.position, targetPosition, ref panVelocity, smoothPan);
            targetCamera.orthographicSize = nextZoom;
            targetCamera.transform.position = ClampPosition(nextPosition, nextZoom);

            var closeToTarget = (targetCamera.transform.position - targetPosition).sqrMagnitude < 0.0001f
                && Mathf.Abs(targetCamera.orthographicSize - targetOrthographicSize) < 0.001f;
            if (closeToTarget)
            {
                targetCamera.transform.position = targetPosition;
                targetCamera.orthographicSize = targetOrthographicSize;
                panVelocity = Vector3.zero;
                zoomVelocity = 0f;
            }

            isCameraSettling = !closeToTarget
                || panVelocity.sqrMagnitude > 0.0001f
                || Mathf.Abs(zoomVelocity) > 0.001f;
        }

        private void ResetTouchState()
        {
            activeTouchFingerId = -1;
            lastPinchDistance = 0f;
        }

        private void RecordCameraFeedback(string feedback)
        {
            lastCameraFeedback = feedback;
            lastCameraFeedbackTime = Time.time;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsTouchOverUi(Touch touch)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }
    }
}
