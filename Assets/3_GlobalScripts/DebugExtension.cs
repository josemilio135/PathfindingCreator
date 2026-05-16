using UnityEngine;

public static class DebugExtension
{
    public static void DrawPoint( Vector3 position, Color color,float size = 0.2f)
    {
        Debug.DrawLine(
            position - Vector3.up * size,
            position + Vector3.up * size,
            color);

        Debug.DrawLine(
            position - Vector3.right * size,
            position + Vector3.right * size,
            color);

        Debug.DrawLine(
            position - Vector3.forward * size,
            position + Vector3.forward * size,
            color);
    }
}