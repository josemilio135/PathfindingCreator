using UnityEngine;
using UnityEngine.InputSystem;

public class CenitalCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used for raycasts and viewport calculations.")]
    [SerializeField] Camera _camera;

    [Tooltip("Base movement speed.")]
    [SerializeField, Min(0f)] float _moveSpeed = 8f;

    [Tooltip("Sprint multiplier while holding Left Shift.")]
    [SerializeField, Min(1f)] float _fastMoveMultiplier = 3f;

    [Tooltip("Smooth time used when moving the camera pivot.")]
    [SerializeField, Min(0.01f)] float _moveSmoothTime = 0.08f;

    [Header("Zoom")]
    [Tooltip("Mouse wheel zoom sensitivity.")]
    [SerializeField, Min(0f)] float _zoomSpeed = 200f;

    [Tooltip("Closest zoom distance.")]
    [SerializeField, Min(0.01f)] float _minDistance = 5f;

    [Tooltip("Farthest zoom distance.")]
    [SerializeField, Min(0.01f)] float _maxDistance = 40f;

    [Tooltip("Zoom interpolation speed.")]
    [SerializeField, Min(0.01f)] float _zoomSmoothTime = 0.1f;

    [Header("Rotation")]
    [Tooltip("Horizontal orbit sensitivity.")]
    [SerializeField, Min(0f)] float _horizontalRotationSpeed = 20f;

    [Tooltip("Vertical orbit sensitivity.")]
    [SerializeField, Min(0f)] float _verticalRotationSpeed = 20f;

    [Tooltip("Minimum vertical camera angle.")]
    [SerializeField, Range(-90f, 90f)] float _minVerticalAngle = -15f;

    [Tooltip("Maximum vertical camera angle.")]
    [SerializeField, Range(0f, 90f)] float _maxVerticalAngle = 90f;

    [Header("Drag Pan")]
    [Tooltip("Drag movement sensitivity.")]
    [SerializeField, Min(0f)] float _dragPanSpeed = 0.05f;

    [Header("Focus")]
    [Tooltip("Objects that can be selected.")]
    [SerializeField] LayerMask _selectionMask = ~0;

    [Header("Terrain Height")]
    [Tooltip("Keeps the camera at a fixed height relative to terrain.")]
    [SerializeField] bool _followTerrainHeight = true;

    [Tooltip("Terrain layers used for height detection.")]
    [SerializeField] LayerMask _terrainMask;

    [Tooltip("Height above terrain.")]
    [SerializeField, Min(0f)] float _terrainOffset = 5f;

    [Tooltip("Terrain height interpolation speed.")]
    [SerializeField, Min(0f)] float _terrainHeightSmooth = 8f;

    Transform _pivot;

    float _horizontalAngle;
    float _verticalAngle;

    float _distance;
    float _targetDistance;

    Vector3 _moveVelocity;
    Vector3 _targetPivotPosition;

    bool _dragging;
    Vector2 _lastMousePosition;

    Transform _selectedTarget;

    void OnValidate()
    {
        _maxDistance = Mathf.Max(_maxDistance, _minDistance);

        _maxVerticalAngle = Mathf.Max(_maxVerticalAngle, _minVerticalAngle);
    }

    void Awake()
    {
        if (_camera == null) _camera = Camera.main;

        GameObject pivot = new("[RTS Camera Pivot]");
        _pivot = pivot.transform;

        _pivot.position = transform.position;

        Vector3 euler = transform.rotation.eulerAngles;

        _horizontalAngle = euler.y;
        _verticalAngle = euler.x;

        _distance = Mathf.Clamp(Vector3.Distance(transform.position, _pivot.position), _minDistance, _maxDistance);

        if (_distance <= _minDistance)
            _distance = (_minDistance + _maxDistance) * 0.5f;

        _targetDistance = _distance;
        _targetPivotPosition = _pivot.position;
    }

    void Update()
    {
        HandleSelection();
        HandleFocus();

        HandleMovement();
        HandleDragPan();
        HandleZoom();
        HandleRotation();

        UpdateTerrainHeight();
    }

    void LateUpdate()
    {
        UpdatePivot();
        UpdateCamera();
    }

    void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;

        Vector3 forward = Quaternion.Euler(0f, _horizontalAngle, 0f) * Vector3.forward;

        Vector3 right = Quaternion.Euler(0f, _horizontalAngle, 0f) * Vector3.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        float zoomRatio = Mathf.InverseLerp(_minDistance, _maxDistance, _distance);

        float speed = _moveSpeed * Mathf.Lerp(0.5f, 2f, zoomRatio);

        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= _fastMoveMultiplier;

        Vector3 movement = (forward * moveInput.y + right * moveInput.x) * speed * Time.deltaTime;



        _targetPivotPosition += movement;
    }

    void HandleDragPan()
    {
        bool drag = Mouse.current.middleButton.isPressed || (Keyboard.current.spaceKey.isPressed && Mouse.current.leftButton.isPressed);

        if (drag && !_dragging)
        {
            _dragging = true;
            _lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (!drag)
        {
            _dragging = false;
            return;
        }

        Vector2 current = Mouse.current.position.ReadValue();
        Vector2 delta = current - _lastMousePosition;

        _lastMousePosition = current;

        float zoomRatio = Mathf.InverseLerp(_minDistance, _maxDistance, _distance);

        float zoomScale = Mathf.Lerp(0.5f, 2f, zoomRatio);

        Vector3 right = Quaternion.Euler(0f, _horizontalAngle, 0f) * Vector3.right;

        Vector3 forward = Quaternion.Euler(0f, _horizontalAngle, 0f) * Vector3.forward;

        right.y = 0f;
        forward.y = 0f;

        right.Normalize();
        forward.Normalize();

        _targetPivotPosition -= (right * delta.x + forward * delta.y) * _dragPanSpeed * zoomScale;
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        _targetDistance -= scroll * (_zoomSpeed * 0.01f);

        _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
    }

    void HandleRotation()
    {
        if (!Mouse.current.rightButton.isPressed) return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        _horizontalAngle += delta.x * _horizontalRotationSpeed * Time.deltaTime;

        _verticalAngle -= delta.y * _verticalRotationSpeed * Time.deltaTime;

        _verticalAngle = Mathf.Clamp(_verticalAngle, _minVerticalAngle, _maxVerticalAngle);
    }

    void HandleSelection()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _selectionMask)) return;

        _selectedTarget = hit.transform;
    }

    void HandleFocus()
    {
        if (_selectedTarget == null) return;

        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        _targetPivotPosition = _selectedTarget.position;
    }

    void UpdateTerrainHeight()
    {
        if (!_followTerrainHeight) return;

        Vector3 rayOrigin = _targetPivotPosition + Vector3.up * 500f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1000f, _terrainMask)) return;

        _targetPivotPosition.y = Mathf.Lerp(_targetPivotPosition.y, hit.point.y + _terrainOffset, Time.deltaTime * _terrainHeightSmooth);
    }

    void UpdatePivot()
    {
        _pivot.position = Vector3.SmoothDamp(_pivot.position, _targetPivotPosition, ref _moveVelocity, _moveSmoothTime);
    }

    void UpdateCamera()
    {
        _distance = Mathf.Lerp(_distance, _targetDistance, Time.deltaTime / _zoomSmoothTime);

        Quaternion rotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0f);

        Vector3 offset = rotation * new Vector3(0f, 0f, -_distance);

        transform.position = _pivot.position + offset;
        transform.rotation = rotation;
    }

    public void Focus(Transform target)
    {
        if (target == null)
            return;

        _selectedTarget = target;
        _targetPivotPosition = target.position;
    }

    public void Follow(Transform target)
    {
        _selectedTarget = target;
    }

    public void ClearSelection()
    {
        _selectedTarget = null;
    }
}