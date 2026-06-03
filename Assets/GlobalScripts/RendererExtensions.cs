using UnityEngine;

public static class RendererExtensions
{
    /// <summary>
    /// Sets a color property using MaterialPropertyBlock.
    /// </summary>
    public static void SetColor(this Renderer renderer, Color color, string propertyName = "_BaseColor")
    {
        var block = GetPropertyBlock(renderer);

        block.SetColor(propertyName, color);

        renderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// Sets a float property using MaterialPropertyBlock.
    /// </summary>
    public static void SetRenderFloat(this Renderer renderer, string propertyName, float value)
    {
        var block = GetPropertyBlock(renderer);

        block.SetFloat(propertyName, value);

        renderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// Sets an int property using MaterialPropertyBlock.
    /// </summary>
    public static void SetRenderInt(this Renderer renderer, string propertyName, int value)
    {
        var block = GetPropertyBlock(renderer);

        block.SetInt(propertyName, value);

        renderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// Sets a vector property using MaterialPropertyBlock.
    /// </summary>
    public static void SetRenderVector(this Renderer renderer, string propertyName, Vector4 value)
    {
        var block = GetPropertyBlock(renderer);

        block.SetVector(propertyName, value);

        renderer.SetPropertyBlock(block);
    }

    /// <summary>
    /// Gets a color directly from the material.
    /// </summary>
    public static Color GetColor(this Renderer renderer, string propertyName = "_BaseColor")
    {
        var mat = renderer.material;

        if (mat.HasProperty(propertyName))
            return mat.GetColor(propertyName);

        return Color.white;
    }

    static MaterialPropertyBlock GetPropertyBlock(Renderer renderer)
    {
        var block = new MaterialPropertyBlock();

        renderer.GetPropertyBlock(block);

        return block;
    }
}
