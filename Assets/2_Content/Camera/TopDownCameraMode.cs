using UnityEngine;

public class TopDownCameraMode : ICameraMode
{
    public void Enter(CameraController controller)
    {
    }

    public void Exit(CameraController controller)
    {
    }

    public void Update(CameraController controller)
    {
        HandleMovement(controller);
        HandleDrag(controller);
        HandleZoom(controller);
        HandleRotation(controller);

        controller.UpdateTerrainHeight();
    }

    public void LateUpdate(CameraController controller)
    {
        controller.UpdatePivot();
        controller.UpdateCamera();
    }

    void HandleMovement(CameraController controller)
    {
        float speed = controller.MoveSpeed * controller.ZoomScale;

        if (controller.InputSprint)
            speed *= controller.FastMoveMultiplier;

        Vector3 movement =
            (controller.FlatVector(Vector3.forward) * controller.InputMove.y
            + controller.FlatVector(Vector3.right) * controller.InputMove.x)
            * speed * Time.deltaTime;

        controller.TargetPivotPosition += movement;
    }

    void HandleDrag(CameraController controller)
    {
        if (!controller.InputDrag)
        {
            controller.Dragging = false;
            return;
        }

        if (!controller.Dragging)
        {
            controller.Dragging = true;
            controller.LastMousePosition = controller.InputMousePosition;
        }

        Vector2 current = controller.InputMousePosition;
        Vector2 delta = current - controller.LastMousePosition;

        controller.LastMousePosition = current;

        controller.TargetPivotPosition -=
            (controller.FlatVector(Vector3.right) * delta.x
            + controller.FlatVector(Vector3.forward) * delta.y)
            * controller.DragPanSpeed * controller.ZoomScale;
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