using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Focuser : MonoBehaviour
{
    [SerializeField] List<GameObject> targetsList = new();

    CameraController _camera;
    bool InputToggleFocus => Keyboard.current.fKey.wasPressedThisFrame;
    bool InputExitFocus => Mouse.current.middleButton.wasPressedThisFrame;
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

        if (InputToggleFocus)
        {
            ToggleFocusMode();
            return;
        }

        if (_isFocusMode && InputExitFocus)
        {
            ToggleFocusMode();
            return;
        }

        if (InputPrevTarget)
        {
            if (!_isFocusMode) EnterFocusMode();

            SetFocusTarget(false);
            return;
        }

        if (InputNextTarget)
        {
            if (!_isFocusMode) EnterFocusMode();

            SetFocusTarget(true);
            return;
        }
    }
    void EnterFocusMode()
    {
        if (_isFocusMode) return;
        if (targetsList.Count == 0) return;

        _isFocusMode = true;
        _camera.SetTarget(targetsList[_targetIndex].transform);
    }

    void ExitFocusMode()
    {
        if (!_isFocusMode) return;

        _isFocusMode = false;
        _camera.SetTopDown();
    }

    void ToggleFocusMode()
    {
        if (_isFocusMode) ExitFocusMode();
        else EnterFocusMode();
    }
    void SetFocusTarget(bool next)
    {
        if (targetsList.Count == 0) return;

        if (next) _targetIndex = (_targetIndex + 1) % targetsList.Count;

        else _targetIndex = (_targetIndex - 1 + targetsList.Count) % targetsList.Count;

        _camera.SetTarget(targetsList[_targetIndex].transform);
    }

}