using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Focuser : MonoBehaviour
{
    [SerializeField] List<GameObject> targetsList = new();

    CameraController _camera;
    bool InputChangeCamMode => Keyboard.current.spaceKey.wasPressedThisFrame;
    bool InputPrevTarget => Keyboard.current.qKey.wasPressedThisFrame
                         || Keyboard.current.leftArrowKey.wasPressedThisFrame;
    bool InputNextTarget => Keyboard.current.eKey.wasPressedThisFrame
                         || Keyboard.current.rightArrowKey.wasPressedThisFrame;

    bool _isFocusMode = false;
    int _targetIndex = 0;

    private void Start()
    {
        _camera = CameraController.Instance;
    }

    private void Update()
    {
        if (!_camera) return;

        if (InputChangeCamMode) ToggleFocusMode();

        if (!_isFocusMode) return;

        if (InputPrevTarget) SetFocusTarget(false);

        if (InputNextTarget) SetFocusTarget(true);
    }
    void SetFocusTarget(bool next)
    {
        if (targetsList.Count == 0) return;

        if (next)
        {
            _targetIndex = (_targetIndex + 1) % targetsList.Count;
        }
        else
        {
            _targetIndex = (_targetIndex - 1 + targetsList.Count) % targetsList.Count;
        }

        _camera.SetTarget(targetsList[_targetIndex].transform);
    }

    void ToggleFocusMode()
    {
        _isFocusMode = !_isFocusMode;

        if (_isFocusMode)
        {
            if (targetsList.Count == 0) return;
            _camera.SetTarget(targetsList[_targetIndex].transform);
        }
        else _camera.SetTopDown();
    }
}