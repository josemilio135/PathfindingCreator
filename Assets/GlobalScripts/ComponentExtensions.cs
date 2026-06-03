using UnityEngine;

public static class ComponentExtensions
{
    /// <summary>
    /// Tries to get a component from this object or its parents.
    /// </summary>
    public static bool TryGetComponentInParent<T>(this Component comp, out T result) where T : class
    {
        result = comp.GetComponentInParent<T>();

        return result != null;
    }
}