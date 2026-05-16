using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeViewer))]
public class NodesGraph_Editor : Editor
{

    NodeViewer _viewer;
    bool _drawDebug = true;

    void OnEnable()
    {
        _viewer = (NodeViewer)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        DrawBakeButtons();

        EditorGUILayout.Space();

        DrawDebugToggle();
    }

    void DrawBakeButtons()
    {
        EditorGUILayout.LabelField("Bake Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Bake Corners"))
        {
            Undo.RecordObject(_viewer, "Bake Corners");

            _viewer.BakeCorners();

            EditorUtility.SetDirty(_viewer);
        }

        EditorGUI.BeginDisabledGroup(_viewer.IsClean);

        if (GUILayout.Button("Clear Corners"))
        {
            Undo.RecordObject(_viewer, "Clear Corners");

            _viewer.ClearCorners();

            EditorUtility.SetDirty(_viewer);
        }

        EditorGUI.EndDisabledGroup();
    }

    void DrawDebugToggle()
    {
        _drawDebug = EditorGUILayout.Toggle("Draw Debug", _drawDebug);
    }


    void OnSceneGUI()
    {
        if (!_drawDebug) return;

        DrawVisibleCorners();
    }

    void DrawVisibleCorners()
    {
        int collidersCount = _viewer.ScanWalls();

        Collider[] results = _viewer.Results;

        for (int i = 0; i < collidersCount; i++)
        {
            BoxCollider box = results[i] as BoxCollider;

            if (box == null) continue;

            DrawBoxCorners(box);
        }
    }

    void DrawBoxCorners(BoxCollider box)
    {
        Vector3[] corners = _viewer.GetCorners(box);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 realCorner = corners[i];

            Vector3 cornerDirection =
                realCorner - box.bounds.center;

            cornerDirection.Normalize();

            Vector3 cornerOffset =
                realCorner +
                cornerDirection * _viewer.CornerOffset;

            bool canSeeThisCorner =
                Perception.HasLineOfSight(
                    _viewer.transform.position, cornerOffset, _viewer.WallMask);

            if (!canSeeThisCorner) continue;

            Handles.color = Color.yellow;

            Handles.DrawLine(
                _viewer.transform.position, cornerOffset);

            Handles.color = Color.cyan;

            Handles.SphereHandleCap(
                0, cornerOffset, Quaternion.identity, 0.25f, EventType.Repaint);
        }
    }
}
