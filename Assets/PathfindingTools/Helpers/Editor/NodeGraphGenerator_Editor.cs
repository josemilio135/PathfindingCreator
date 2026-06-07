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
    SerializedProperty _targetContainer;

    SerializedProperty _samplerSettings;
    SerializedProperty _extraOffset;
    SerializedProperty _minCornerAngle;
    SerializedProperty _minArchCornerAngle;
    SerializedProperty _straightAngleTolerance;

    bool _showGizmos = true;
    bool _showAgentConfig = true;
    bool _showSampler = true;

    bool _drawAgentSize = true;
    bool _drawViewRange = true;
    bool _drawDetectionCorners = true;
    bool _drawMergeNodes = true;

    void OnEnable()
    {
        _viewer = (NodeGraphGenerator)target;

        _agent = serializedObject.FindProperty("_agent");
        _viewRange = serializedObject.FindProperty("_viewRange");
        _nodePrefab = serializedObject.FindProperty("_nodePrefab");
        _roundColliderPrecision = serializedObject.FindProperty("_roundColliderPrecision");
        _nodeMergeDistance = serializedObject.FindProperty("_nodeMergeDistance");
        _targetContainer = serializedObject.FindProperty("_targetContainer");

        _samplerSettings = serializedObject.FindProperty("_samplerSettings");
        _extraOffset = _samplerSettings.FindPropertyRelative("ExtraOffset");
        _minCornerAngle = _samplerSettings.FindPropertyRelative("MinCornerAngle");
        _minArchCornerAngle = _samplerSettings.FindPropertyRelative("MinArchCornerAngle");
        _straightAngleTolerance = _samplerSettings.FindPropertyRelative("StraightAngleTolerance");
    }

    void OnSceneGUI()
    {
        if (!_showGizmos) return;

        DrawViewRange();
        DrawVisibleCorners();

        List<Vector3> merged = _viewer.GetMergedCorners();
        DrawMergeNodes(merged);
        DrawAgentSize(merged);
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
        EditorGUILayout.PropertyField(_agent, new GUIContent("Agent Config"));

        if (_agent.objectReferenceValue == null)
            if (GUILayout.Button("Create", GUILayout.Width(70f)))
                CreateAgentConfig();

        EditorGUILayout.EndHorizontal();

        AgentConfig config = _agent.objectReferenceValue as AgentConfig;

        if (config == null)
        {
            EditorGUILayout.HelpBox("Assign or create an Agent Config.", MessageType.Warning);
            return;
        }

        _showAgentConfig = EditorGUILayout.Foldout(_showAgentConfig, "Edit Agent Config", true);
        if (!_showAgentConfig) return;

        EditorGUI.indentLevel++;
        CreateCachedEditor(config, null, ref _agentEditor);

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
        AgentConfig asset = ScriptableAssetUtility.CreateAsset<AgentConfig>(
            "AgentConfig", ScriptableAssetUtility.CreateLocation.SelectedFolder);

        _agent.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
    }

    void DrawNodePrefab()
    {
        EditorGUILayout.PropertyField(_nodePrefab, new GUIContent("Node Prefab"));
    }

    void DrawAdvancedSection()
    {
        EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_viewRange,
            new GUIContent("View Range",
                "Search radius for nearby colliders."));

        EditorGUILayout.PropertyField(_nodeMergeDistance,
            new GUIContent("Node Merge Distance",
                "Nodes closer than this are merged into one."));

        EditorGUILayout.Space(6f);

        _showSampler = EditorGUILayout.Foldout(_showSampler, "Corner Detection", true, EditorStyles.foldoutHeader);
        if (!_showSampler) return;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(_extraOffset,
            new GUIContent("Node Offset",
                "Extra distance from the corner surface. All nodes are pushed this far beyond the agent radius."));

        EditorGUILayout.Space(4f);

        // --- Convex obstacles ---
        EditorGUILayout.LabelField("Boxes � Spheres � Capsules � Convex Meshes", EditorStyles.miniBoldLabel);

        EditorGUILayout.IntSlider(_roundColliderPrecision, 4, 32,
            new GUIContent("Sphere & Capsule Precision",
                "Number of points sampled around spheres and capsules. Higher = more nodes, slower bake."));

        EditorGUILayout.Slider(_minCornerAngle, 0f, 180f,
            new GUIContent("Corner Angle Threshold",
                "Corners sharper than this generate a node. Below this angle, no node is placed."));

        EditorGUILayout.Slider(_straightAngleTolerance, 0f, 45f,
            new GUIContent("Straight Edge Threshold",
                "Edges within this angle of a straight line are skipped."));

        EditorGUILayout.Space(4f);

        // --- Architecture ---
        EditorGUILayout.LabelField("ProBuilder � Interiors � Non-Convex Meshes", EditorStyles.miniBoldLabel);

        EditorGUILayout.Slider(_minArchCornerAngle, 0f, 180f,
            new GUIContent("Wall Corner Threshold",
                "Wall joins sharper than this generate a node."));

        EditorGUI.indentLevel--;
    }

    void DrawBakeButtons()
    {
        EditorGUILayout.LabelField("Container", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_targetContainer, new GUIContent("Target Container",
            "Container where nodes will be baked. Leave empty to create a new one."));

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(_viewer.IsClean);
        if (GUILayout.Button("Clear"))
        {
            Undo.RecordObject(_viewer, "Clear Container");
            _viewer.ClearContainer();
            EditorUtility.SetDirty(_viewer);
        }
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("New Container"))
        {
            Undo.RecordObject(_viewer, "New Container");
            _viewer.NewContainer();
            EditorUtility.SetDirty(_viewer);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (_agent.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Missing Agent Config.", MessageType.Warning);

        if (_nodePrefab.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign a Node Prefab before baking.", MessageType.Warning);

        EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);

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
    }

    void DrawGizmosSection()
    {
        _showGizmos = EditorGUILayout.Foldout(_showGizmos, "Gizmos", true);
        if (!_showGizmos) return;

        EditorGUI.indentLevel++;

        _drawAgentSize = EditorGUILayout.Toggle("Agent Size", _drawAgentSize);
        _drawViewRange = EditorGUILayout.Toggle("View Range", _drawViewRange);
        _drawDetectionCorners = EditorGUILayout.Toggle("Detection Corners", _drawDetectionCorners);
        _drawMergeNodes = EditorGUILayout.Toggle("Merge Nodes", _drawMergeNodes);

        EditorGUI.indentLevel--;
    }

    void DrawViewRange()
    {
        if (!_drawViewRange) return;

        Handles.color = Color.white;
        Handles.DrawWireDisc(_viewer.transform.position, Vector3.up, _viewer.ViewRange);
    }

    void DrawVisibleCorners()
    {
        if (!_drawDetectionCorners) return;

        foreach (Vector3 corner in _viewer.GetVisibleCorners())
        {
            Handles.color = Color.yellow;
            Handles.DrawLine(_viewer.transform.position, corner);

            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0, corner, Quaternion.identity, 0.25f, EventType.Repaint);
        }
    }

    void DrawMergeNodes(List<Vector3> merged)
    {
        if (!_drawMergeNodes) return;

        foreach (Vector3 point in merged)
        {
            Handles.color = new Color(1f, 0f, 1f, 0.15f);
            Handles.DrawSolidDisc(point, Vector3.up, _viewer.NodeMergeDistance);

            Handles.color = Color.magenta;
            Handles.DrawWireDisc(point, Vector3.up, _viewer.NodeMergeDistance);
            Handles.SphereHandleCap(0, point, Quaternion.identity, 0.35f, EventType.Repaint);
        }
    }

    void DrawAgentSize(List<Vector3> merged)
    {
        if (!_drawAgentSize) return;

        float radius = _viewer.AgentRadius;
        float height = _viewer.AgentHeight;

        foreach (Vector3 point in merged)
        {
            Vector3 bottom = point;
            Vector3 top = point + Vector3.up * height;

            Handles.color = Color.green;

            Handles.DrawWireDisc(bottom, Vector3.up, radius);
            Handles.DrawWireDisc(top, Vector3.up, radius);

            Handles.DrawLine(bottom + Vector3.forward * radius, top + Vector3.forward * radius);
            Handles.DrawLine(bottom - Vector3.forward * radius, top - Vector3.forward * radius);
            Handles.DrawLine(bottom + Vector3.right * radius, top + Vector3.right * radius);
            Handles.DrawLine(bottom - Vector3.right * radius, top - Vector3.right * radius);

            Handles.color = Color.red;
            Handles.SphereHandleCap(0, point, Quaternion.identity, 0.08f, EventType.Repaint);
        }
    }
}