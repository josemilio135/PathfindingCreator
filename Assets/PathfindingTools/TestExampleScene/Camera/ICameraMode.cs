public interface ICameraMode
{
    void Enter(CameraController controller);
    void Exit(CameraController controller);
    void Update(CameraController controller);
    void LateUpdate(CameraController controller);
}