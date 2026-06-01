using UnityEditor;
using UnityEngine;

public static class ScriptableAssetUtility
{
    public static T CreateAssetNextToScript<T>(MonoBehaviour target, string fileName)
        where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        MonoScript script =
            MonoScript.FromMonoBehaviour(target);

        string scriptPath =
            AssetDatabase.GetAssetPath(script);

        string folder =
            System.IO.Path.GetDirectoryName(scriptPath);

        string assetPath =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{fileName}.asset");

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(asset);

        return asset;
    }
}