using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("References")]
    [SerializeField] Camera _camera;

    [Header("Movement")]
    [SerializeField] float _moveSpeed = 8f;
    [SerializeField] float _fastMoveMultiplier = 3f;
    [SerializeField] float _moveSmoothTime = 0.08f;

    [Header("Zoom")]
    [SerializeField] float _zoomSpeed = 200f;
    [SerializeField] float _minDistance = 5f;
    [SerializeField] float _maxDistance = 40f;
    [SerializeField] float _zoomSmoothTime = 0.1f;

    [Header("Rotation")]
    [SerializeField] float _horizontalRotationSpeed = 20f;
    [SerializeField] float _verticalRotationSpeed = 20f;
    [SerializeField] float _minVerticalAngle = -15f;
    [SerializeField] float _maxVerticalAngle = 90f;

    [Header("Drag Pan")]
    [SerializeField] float _dragPanSpeed = 0.05f;

    [Header("Terrain Height")]
    [SerializeField] bool _followTerrainHeight = true;
    [SerializeField] LayerMask _terrainMask;
    [SerializeField] float _terrainOffset = 5f;
    [SerializeField] float _terrainHeightSmooth = 8f;

    Transform _pivot;

    float _horizontalAngle;
    float _verticalAngle;

    float _distance;
    float _targetDistance;

    Vector3 _targetPivotPosition;
    Vector3 _moveVelocity;

    bool _dragging;
    Vector2 _lastMousePosition;

    ICameraMode _mode;

    #region Public parameters
    public Camera Camera => _camera;
    public Transform Pivot => _pivot;

    public float MoveSpeed => _moveSpeed;
    public float FastMoveMultiplier => _fastMoveMultiplier;
    public float MoveSmoothTime => _moveSmoothTime;

    public float ZoomSpeed => _zoomSpeed;
    public float MinDistance => _minDistance;
    public float MaxDistance => _maxDistance;
    public float ZoomSmoothTime => _zoomSmoothTime;

    public float HorizontalRotationSpeed => _horizontalRotationSpeed;
    public float VerticalRotationSpeed => _verticalRotationSpeed;
    public float MinVerticalAngle => _minVerticalAngle;
    public float MaxVerticalAngle => _maxVerticalAngle;

    public float DragPanSpeed => _dragPanSpeed;

    public bool FollowTerrainHeight => _followTerrainHeight;
    public LayerMask TerrainMask => _terrainMask;
    public float TerrainOffset => _terrainOffset;
    public float TerrainHeightSmooth => _terrainHeightSmooth;

    public float HorizontalAngle
    {
        get => _horizontalAngle;
        set => _horizontalAngle = value;
    }

    public float VerticalAngle
    {
        get => _verticalAngle;
        set => _verticalAngle = value;
    }

    public float Distance
    {
        get => _distance;
        set => _distance = value;
    }

    public float TargetDistance
    {
        get => _targetDistance;
        set => _targetDistance = value;
    }

    public Vector3 TargetPivotPosition
    {
        get => _targetPivotPosition;
        set => _targetPivotPosition = value;
    }

    public Vector3 MoveVelocity
    {
        get => _moveVelocity;
        set => _moveVelocity = value;
    }

    public bool Dragging
    {
        get => _dragging;
        set => _dragging = value;
    }

    public Vector2 LastMousePosition
    {
        get => _lastMousePosition;
        set => _lastMousePosition = value;
    }

    public float ZoomRatio => Mathf.InverseLerp(_minDistance, _maxDistance, _distance);
    public float ZoomScale => Mathf.Lerp(0.5f, 2f, ZoomRatio);

    public Vector2 InputMove
    {
        get
        {
            Vector2 input = Vector2.zero;

            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;

            return input;
        }
    }

    public bool InputSprint => Keyboard.current.leftShiftKey.isPressed;
    public bool InputRotate => Mouse.current.rightButton.isPressed;
    public bool InputDrag => Mouse.current.middleButton.isPressed || (Keyboard.current.spaceKey.isPressed && Mouse.current.leftButton.isPressed);

    public Vector2 InputMousePosition => Mouse.current.position.ReadValue();
    public Vector2 InputMouseDelta => Mouse.current.delta.ReadValue();
    public float InputZoom => Mouse.current.scroll.ReadValue().y;

    #endregion

    void OnValidate()
    {
        _maxDistance = Mathf.Max(_maxDistance, _minDistance);
        _maxVerticalAngle = Mathf.Max(_maxVerticalAngle, _minVerticalAngle);
    }

    void Awake()
    {
        if (!Instance) Instance = this;
        else Destroy(this);

        if (_camera == null) _camera = Camera.main;

        GameObject pivot = new("[Camera Pivot]");
        _pivot = pivot.transform;

        _pivot.position = transform.position;

        _horizontalAngle = transform.rotation.eulerAngles.y;
        _verticalAngle = transform.rotation.eulerAngles.x;

        _distance = (_minDistance + _maxDistance) * 0.5f;
        _targetDistance = _distance;

        _targetPivotPosition = _pivot.position;

        SetMode(new TopDownCameraMode());
    }

    void Update()
    {
        _mode?.Update(this);
    }

    void LateUpdate()
    {
        _mode?.LateUpdate(this);
    }

    public void SetMode(ICameraMode mode)
    {
        _mode?.Exit(this);

        _mode = mode;

        _mode?.Enter(this);
    }

    public void SetTarget(Transform target)
    {
        SetMode(new FocusCameraMode(target));
    }

    public void SetTopDown()
    {
        SetMode(new TopDownCameraMode());
    }

    public void UpdateTerrainHeight()
    {
        if (!_followTerrainHeight)
            return;

        Vector3 rayOrigin = _targetPivotPosition + Vector3.up * 500f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1000f, _terrainMask))
            return;

        _targetPivotPosition.y = Mathf.Lerp(
            _targetPivotPosition.y,
            hit.point.y + _terrainOffset,
            Time.deltaTime * _terrainHeightSmooth);
    }

    public void UpdatePivot()
    {
        _pivot.position = Vector3.SmoothDamp(
            _pivot.position,
            _targetPivotPosition,
            ref _moveVelocity,
            _moveSmoothTime);
    }

    public void UpdateCamera()
    {
        _distance = Mathf.Lerp(
            _distance,
            _targetDistance,
            Time.deltaTime / _zoomSmoothTime);

        Quaternion rotation = Quaternion.Euler(
            _verticalAngle,
            _horizontalAngle,
            0f);

        Vector3 offset = rotation * new Vector3(
            0f,
            0f,
            -_distance);

        transform.position = _pivot.position + offset;
        transform.rotation = rotation;
    }

    public Vector3 FlatVector(Vector3 vector)
    {
        Vector3 flat = Quaternion.Euler(
            0f,
            _horizontalAngle,
            0f) * vector;

        flat.y = 0f;

        return flat.normalized;
    }
}
