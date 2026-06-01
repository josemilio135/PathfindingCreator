using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeGraphGenerator))]
public class NodeGraphGenerator_Editor : Editor
{
    NodeGraphGenerator _viewer;
    Editor _agentEditor;

    SerializedProperty _agent;
    SerializedProperty _nodePrefab;


    SerializedProperty _viewRange;

    SerializedProperty _roundColliderPrecision;
    SerializedProperty _nodeMergeDistance;
    SerializedProperty _extraOffset;
    SerializedProperty _minCornerAngle;

    SerializedProperty _automaticUndo;

    bool _showGizmos = true;
    bool _showAgentConfig = true;

    bool _drawAgentSize = true;
    bool _drawViewRange = true;
    bool _drawDetectionCorners = true;
    bool _drawMergeNodes = true;

    void OnEnable()
    {
        _viewer = (NodeGraphGenerator)target;

        _agent =
            serializedObject.FindProperty("_agent");
        _viewRange =
            serializedObject.FindProperty("_viewRange");
        _nodePrefab =
            serializedObject.FindProperty("_nodePrefab");
        _roundColliderPrecision =
            serializedObject.FindProperty("_roundColliderPrecision");
        _nodeMergeDistance =
            serializedObject.FindProperty("_nodeMergeDistance");
        _extraOffset =
            serializedObject.FindProperty("_extraOffset");
        _minCornerAngle =
            serializedObject.FindProperty("_minCornerAngle");
        _automaticUndo =
            serializedObject.FindProperty("_automaticUndo");
    }

    void OnSceneGUI()
    {
        if (!_showGizmos) return;

        DrawViewRange();
        DrawVisibleCorners();
        DrawMergeNodes();
        DrawAgentSize();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Convex MeshColliders are treated as regular obstacles.\n" +
            "Non-convex MeshColliders are treated as architectural surfaces for detailed corner detection.",
            MessageType.Info);

        EditorGUILayout.Space();

        DrawAgentSection();
        EditorGUILayout.Space();

        DrawNodePrefab();
        EditorGUILayout.Space();

        DrawAdvancedSection();
        EditorGUILayout.Space();

        DrawBakeButtons();
        EditorGUILayout.Space();

        DrawGizmosSection();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawAgentSection()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(
            _agent,
            new GUIContent("Agent Config"));

        if (_agent.objectReferenceValue == null)
        {
            if (GUILayout.Button("Create", GUILayout.Width(70f)))
            {
                CreateAgentConfig();
            }
        }

        EditorGUILayout.EndHorizontal();

        AgentConfig config =
            _agent.objectReferenceValue as AgentConfig;

        if (config == null)
        {
            EditorGUILayout.HelpBox(
                "Assign or create an Agent Config.",
                MessageType.Warning);

            return;
        }

        _showAgentConfig =
            EditorGUILayout.Foldout(
                _showAgentConfig,
                "Edit Agent Config",
                true);

        if (!_showAgentConfig)
            return;

        EditorGUI.indentLevel++;

        CreateCachedEditor(
            config,
            null,
            ref _agentEditor);

        if (_agentEditor != null)
        {
            EditorGUILayout.BeginVertical("box");
            _agentEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        EditorGUI.indentLevel--;
    }
    void CreateAgentConfig()
    {
        AgentConfig asset =
         ScriptableAssetUtility.
         CreateAsset<AgentConfig>("AgentConfig", ScriptableAssetUtility.CreateLocation.SelectedFolder);

        _agent.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
    }
    void DrawNodePrefab()
    {
        EditorGUILayout.PropertyField(
            _nodePrefab,
            new GUIContent("Node Prefab"));
    }

    void DrawAdvancedSection()
    {
        EditorGUILayout.LabelField(
            "Advanced Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            _viewRange,
            new GUIContent("View Range"));

        EditorGUILayout.IntSlider(
            _roundColliderPrecision,
            4, 32,
            new GUIContent("Round Collider Precision"));

        EditorGUILayout.PropertyField(
            _nodeMergeDistance,
            new GUIContent("Node Merge Distance"));

        EditorGUILayout.PropertyField(
            _extraOffset,
            new GUIContent("Corner Offset"));

        EditorGUILayout.Slider(
            _minCornerAngle,
            0f,
            180f,
            new GUIContent("Min Corner Angle"));
    }
    void DrawBakeButtons()
    {
        EditorGUILayout.LabelField("Bake Tools", EditorStyles.boldLabel);

        if (_agent.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "Missing Agent Config.",
                MessageType.Warning);
        }

        if (_nodePrefab.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Node Prefab before baking.",
                MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(!_viewer.CanBake);

        if (GUILayout.Button("Bake Only This Nodes"))
        {
            Undo.RecordObject(_viewer, "Bake Only This Nodes");

            _viewer.BakeOnlyThisNodes();
            EditorUtility.SetDirty(_viewer);
        }

        if (GUILayout.Button("Bake All Area Nodes"))
        {
            Undo.RecordObject(_viewer, "Bake All Area Nodes");

            _viewer.BakeAllNodes();
            EditorUtility.SetDirty(_viewer);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(_viewer.IsClean);

        if (GUILayout.Button("Undo Last Bake"))
        {
            Undo.RecordObject(_viewer, "Undo Last Bake");

            _viewer.UndoLastBake();
            EditorUtility.SetDirty(_viewer);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.PropertyField(
            _automaticUndo,
            GUIContent.none,
            GUILayout.Width(18f));

        GUILayout.Label(
            "Automatic Undo",
            GUILayout.Width(110f));

        EditorGUILayout.EndHorizontal();
    }

    void DrawGizmosSection()
    {
        _showGizmos =
            EditorGUILayout.Foldout(_showGizmos, "Gizmos", true);

        if (!_showGizmos) return;

        EditorGUI.indentLevel++;

        _drawAgentSize =
            EditorGUILayout.Toggle("Agent Size", _drawAgentSize);

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
            Handles.DrawLine(_viewer.transform.position, corner);

            Handles.color = Color.cyan;
            Handles.SphereHandleCap(
                0,
                corner,
                Quaternion.identity,
                0.25f,
                EventType.Repaint);
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
                0,
                point,
                Quaternion.identity,
                0.35f,
                EventType.Repaint);
        }
    }

    void DrawAgentSize()
    {
        if (!_drawAgentSize) return;

        float radius = _viewer.AgentRadius;
        float height = _viewer.AgentHeight;

        foreach (Vector3 point in _viewer.GetMergedCorners())
        {
            Vector3 bottomCenter = point;
            Vector3 topCenter = point + Vector3.up * height;

            Handles.color = Color.green;

            Handles.DrawWireDisc(
                bottomCenter, Vector3.up, radius);

            Handles.DrawWireDisc(
                topCenter, Vector3.up, radius);

            Handles.DrawLine(
                bottomCenter + Vector3.forward * radius,
                topCenter + Vector3.forward * radius);

            Handles.DrawLine(
                bottomCenter - Vector3.forward * radius,
                topCenter - Vector3.forward * radius);

            Handles.DrawLine(
                bottomCenter + Vector3.right * radius,
                topCenter + Vector3.right * radius);

            Handles.DrawLine(
                bottomCenter - Vector3.right * radius,
                topCenter - Vector3.right * radius);

            Handles.color = Color.red;

            Handles.SphereHandleCap(
                0,
                point,
                Quaternion.identity,
                0.08f,
                EventType.Repaint);
        }
    }
}
