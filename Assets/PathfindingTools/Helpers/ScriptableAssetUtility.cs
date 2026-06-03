using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScriptableAssetUtility
{
    public enum CreateLocation
    {
        NextToScript,
        SelectedFolder,
        AssetsRoot
    }

    public static T CreateAsset<T>(
        string fileName,
        CreateLocation location = CreateLocation.SelectedFolder,
        MonoBehaviour scriptOwner = null)
        where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string folder = GetTargetFolder(location, scriptOwner);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(folder, $"{fileName}.asset"));

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;

        return asset;
    }

    static string GetTargetFolder(
        CreateLocation location,
        MonoBehaviour scriptOwner)
    {
        switch (location)
        {
            case CreateLocation.NextToScript:
                {
                    if (scriptOwner == null)
                        return "Assets";

                    MonoScript script =
                        MonoScript.FromMonoBehaviour(scriptOwner);

                    string scriptPath =
                        AssetDatabase.GetAssetPath(script);

                    return Path.GetDirectoryName(scriptPath);
                }

            case CreateLocation.SelectedFolder:
                {
                    Object selected = Selection.activeObject;

                    if (selected != null)
                    {
                        string path = AssetDatabase.GetAssetPath(selected);

                        if (Directory.Exists(path)) return path;

                        if (!string.IsNullOrEmpty(path))
                            return Path.GetDirectoryName(path);
                    }

                    return "Assets";
                }

            case CreateLocation.AssetsRoot:
            default: return "Assets";
        }
    }
}