using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeViewer))]
public class NodesGraph_Editor : Editor
{
    NodeViewer _viewer;

    bool _showGizmos = true;

    bool _drawViewRange = true;
    bool _drawDetectionCorners = true;

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

        DrawGizmosSection();
    }

    void DrawBakeButtons()
    {
        EditorGUILayout.LabelField(
            "Bake Tools",
            EditorStyles.boldLabel);

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

    void DrawGizmosSection()
    {
        _showGizmos = EditorGUILayout.Foldout(
            _showGizmos,
            "Gizmos",
            true);

        if (!_showGizmos) return;

        EditorGUI.indentLevel++;

        _drawDetectionCorners =
            EditorGUILayout.Toggle(
                "Detection Corners",
                _drawDetectionCorners);

        _drawViewRange =
            EditorGUILayout.Toggle(
                "View Range",
                _drawViewRange);

        EditorGUI.indentLevel--;
    }

    void OnSceneGUI()
    {
        DrawViewRange();

        DrawVisibleCorners();
    }

    void DrawViewRange()
    {
        if (!_drawViewRange) return;

        Handles.color = Color.white;

        Handles.DrawWireDisc(
            _viewer.transform.position,
            Vector3.up,
            _viewer.ViewRange);
    }

    void DrawVisibleCorners()
    {
        if (!_drawDetectionCorners) return;

        foreach (Vector3 corner in _viewer.GetVisibleCorners())
        {
            Handles.color = Color.yellow;

            Handles.DrawLine(
                _viewer.transform.position,
                corner);

            Handles.color = Color.cyan;

            Handles.SphereHandleCap(
                0,
                corner,
                Quaternion.identity,
                0.25f,
                EventType.Repaint);
        }
    }
}