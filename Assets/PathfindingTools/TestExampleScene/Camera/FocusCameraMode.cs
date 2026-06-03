using UnityEngine;

public class FocusCameraMode : ICameraMode
{
    readonly Transform _target;

    public FocusCameraMode(Transform target)
    {
        _target = target;
    }

    public void Enter(CameraController controller)
    {
        if (_target != null)
            controller.TargetPivotPosition = _target.position;
    }

    public void Exit(CameraController controller)
    {
    }

    public void Update(CameraController controller)
    {
        if (_target == null)
        {
            controller.SetTopDown();
            return;
        }

        controller.TargetPivotPosition = _target.position;

        HandleZoom(controller);
        HandleRotation(controller);
    }

    public void LateUpdate(CameraController controller)
    {
        controller.UpdatePivot();
        controller.UpdateCamera();
    }

    void HandleZoom(CameraController controller)
    {
        controller.TargetDistance -= controller.InputZoom * (controller.ZoomSpeed * 0.01f);

        controller.TargetDistance = Mathf.Clamp(
            controller.TargetDistance,
            controller.MinDistance,
            controller.MaxDistance);
    }

    void HandleRotation(CameraController controller)
    {
        if (!controller.InputRotate)
            return;

        controller.HorizontalAngle +=
            controller.InputMouseDelta.x *
            controller.HorizontalRotationSpeed *
            Time.deltaTime;

        controller.VerticalAngle -=
            controller.InputMouseDelta.y *
            controller.VerticalRotationSpeed *
            Time.deltaTime;

        controller.VerticalAngle = Mathf.Clamp(
            controller.VerticalAngle,
            controller.MinVerticalAngle,
            controller.MaxVerticalAngle);
    }
}