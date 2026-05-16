using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeGraphGenerator))]
public class NodeGraphGenerator_Editor : Editor
{
    NodeGraphGenerator _viewer;

    bool _showGizmos = true;

    bool _drawViewRange = true;
    bool _drawDetectionCorners = true;
    bool _drawMergeNodes = true;

    void OnEnable()
    {
        _viewer = (NodeGraphGenerator)target;
    }
    void OnSceneGUI()
    {
        if (!_showGizmos) return;

        DrawViewRange();
        DrawVisibleCorners();
        DrawMergeNodes();
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
            "Bake Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Bake Only This Nodes"))
        {
            Undo.RecordObject(_viewer, "Bake Only This Nodes");

            _viewer.BakeOnlyThisNodes();

            EditorUtility.SetDirty(_viewer);
        }
        
        if (GUILayout.Button("Bake All Nodes"))
        {
            Undo.RecordObject(_viewer, "Bake All Nodes");

            _viewer.BakeAllNodes();

            EditorUtility.SetDirty(_viewer);
        }

        EditorGUI.BeginDisabledGroup(_viewer.IsClean);

        if (GUILayout.Button("Clear All Nodes"))
        {
            Undo.RecordObject(_viewer, "Clear All Nodes");

            _viewer.ClearAllNodes();

            EditorUtility.SetDirty(_viewer);
        }

        EditorGUI.EndDisabledGroup();
    }

    void DrawGizmosSection()
    {
        _showGizmos = EditorGUILayout.Foldout(
            _showGizmos, "Gizmos", true);

        if (!_showGizmos) return;

        EditorGUI.indentLevel++;

        _drawViewRange =
            EditorGUILayout.Toggle("View Range", _drawViewRange);

        _drawDetectionCorners =
            EditorGUILayout.Toggle("Detection Corners", _drawDetectionCorners);

        _drawMergeNodes =
            EditorGUILayout.Toggle("Merge Nodes", _drawMergeNodes);

        EditorGUI.indentLevel--;
    }



    void DrawViewRange()
    {
        if (!_drawViewRange) return;

        Handles.color = Color.white;

        Handles.DrawWireDisc(
            _viewer.transform.position, Vector3.up, _viewer.ViewRange);
    }

    void DrawVisibleCorners()
    {
        if (!_drawDetectionCorners) return;

        foreach (Vector3 corner in _viewer.GetVisibleCorners())
        {
            Handles.color = Color.yellow;

            Handles.DrawLine(
                _viewer.transform.position, corner);

            Handles.color = Color.cyan;

            Handles.SphereHandleCap(
                0, corner, Quaternion.identity, 0.25f, EventType.Repaint);
        }
    }
    void DrawMergeNodes()
    {
        if (!_drawMergeNodes) return;

        List<Vector3> mergedPoints = _viewer.GetMergedCorners();

        for (int i = 0; i < mergedPoints.Count; i++)
        {
            Vector3 point = mergedPoints[i];

            Handles.color = new Color(1f, 0f, 1f, 0.15f);

            Handles.DrawSolidDisc(
                point, Vector3.up, _viewer.NodeMergeDistance);

            Handles.color = Color.magenta;

            Handles.DrawWireDisc(
                point, Vector3.up, _viewer.NodeMergeDistance);

            Handles.SphereHandleCap(
                0, point, Quaternion.identity, 0.35f, EventType.Repaint);
        }
    }
}