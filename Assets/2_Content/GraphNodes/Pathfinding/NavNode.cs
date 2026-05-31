using UnityEngine;

public class NavNode : BaseNode
{
    Renderer _renderer;
    Color _defaultColor;

    static readonly Color _singleTargetColor = Color.green;
    static readonly Color _multiTargetColor = Color.yellow;

    int _targetCount = 0;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _defaultColor = _renderer.GetColor();
    }


    public void AddTarget()
    {
        _targetCount++;
        RefreshColor();
    }

    public void RemoveTarget()
    {
        _targetCount = Mathf.Max(0, _targetCount - 1);
        RefreshColor();
    }

    void RefreshColor()
    {
        if (_renderer == null) return;

        Color color = _targetCount switch
        {
            0 =>  _defaultColor,
            1 => _singleTargetColor,
            _ => _multiTargetColor
        };

        _renderer.SetColor(color);
    }
}
