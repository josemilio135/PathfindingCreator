using UnityEngine;

public class NavNode : BaseNode
{
    [SerializeField] Renderer _renderer;

    void Reset() => _renderer = GetComponentInChildren<Renderer>();

    public void SetVisible(bool visible)
    {
        if (_renderer != null) _renderer.enabled = visible;
    }
}