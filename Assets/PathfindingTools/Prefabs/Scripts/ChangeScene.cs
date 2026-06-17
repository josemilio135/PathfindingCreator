using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [SerializeField] string targetScene = "";
    [SerializeField] NodesContainer _nodesContainer;

    bool shiftHeldInput => Keyboard.current != null &&
                     (Keyboard.current.leftShiftKey.isPressed ||
                      Keyboard.current.rightShiftKey.isPressed);

    bool changeSceneInput => Keyboard.current.tabKey.wasPressedThisFrame;
    bool reloadSceneInput => Keyboard.current.rKey.wasPressedThisFrame;
    bool toggleNodesInput => Keyboard.current.spaceKey.wasPressedThisFrame;

    private void Update()
    {

        if (!shiftHeldInput) return;

        if (changeSceneInput)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("Target scene is null");
                return;
            }
            SceneManager.LoadScene(targetScene);
        }

        if (reloadSceneInput)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (toggleNodesInput)
        {
            if (_nodesContainer == null)
            {
                Debug.LogWarning("Nodes Container is null");
                return;
            }
            _nodesContainer.ToggleNodesVisibility();
        }

    }
}
