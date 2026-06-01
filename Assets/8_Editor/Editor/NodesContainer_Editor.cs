using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodesContainer))]
public class NodesContainer_Editor : Editor
{
    SerializedProperty _agent;
    Editor _agentEditor;

    bool _showAgentConfig = true;

    void OnEnable()
    {
        _agent = serializedObject.FindProperty("_agent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawAgentSection();

        EditorGUILayout.Space();

        DrawPropertiesExcluding(serializedObject, "_agent", "m_Script");

        EditorGUILayout.Space();

        DrawButtons();

        EditorGUILayout.Space();

        DrawStatistics((NodesContainer)target);

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
            EditorGUILayout.Foldout(_showAgentConfig, "Edit Agent Config", true);

        if (!_showAgentConfig) return;

        CreateCachedEditor(config, null, ref _agentEditor);

        if (_agentEditor != null)
        {
            EditorGUILayout.BeginVertical("box");

            _agentEditor.OnInspectorGUI();

            EditorGUILayout.EndVertical();
        }
    }

    void CreateAgentConfig()
    {
        NodesContainer container = (NodesContainer)target;

        AgentConfig asset =
            ScriptableAssetUtility.
            CreateAsset<AgentConfig>("AgentConfig", ScriptableAssetUtility.CreateLocation.SelectedFolder);

        _agent.objectReferenceValue = asset;

        serializedObject.ApplyModifiedProperties();
    }

    void DrawButtons()
    {
        NodesContainer container = (NodesContainer)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Neighbors"))
        {
            Undo.RecordObject(container, "Build Neighbors");

            container.BuildNeighbors();

            EditorUtility.SetDirty(container);
        }
    }
    void DrawStatistics(NodesContainer container)
    {
        EditorGUILayout.LabelField(
            "Graph Statistics",
            EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            "Nodes",
            container.Nodes.Count.ToString("N0"));

        EditorGUILayout.LabelField(
            "Connections",
            container.ConnectionCount.ToString("N0"));

        EditorGUILayout.LabelField(
            "Average Connections",
            container.AverageConnections.ToString("F1"));

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Worst Case Solver Estimates",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "These estimates represent the theoretical worst-case amount of graph work. " +
            "Actual searches are usually much cheaper, especially when using A* or Theta*.",
            MessageType.Info);

        EditorGUILayout.BeginVertical("box");

        DrawSolverEstimate(
            "DFS / BFS",
            "O(N + E)",
            container.EstimatedDFS_BFS);

        DrawSolverEstimate(
            "Dijkstra",
            "O(E log N)",
            container.EstimatedDijkstra);

        DrawSolverEstimate(
            "A*",
            "O(E log N)",
            container.EstimatedAStar);

        DrawSolverEstimate(
            "Theta*",
            "O(E log N) + LOS",
            container.EstimatedThetaStar);

        DrawSolverEstimate(
            "Theta* Smooth",
            "O(E log N) + Extra LOS",
            container.EstimatedThetaStarSmooth);

        EditorGUILayout.EndVertical();
    }

    void DrawSolverEstimate(
        string solver,
        string complexity,
        int operations)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            solver,
            GUILayout.Width(120));

        EditorGUILayout.LabelField(
            complexity,
            GUILayout.Width(100));

        EditorGUILayout.LabelField(
            $"≈ {operations:N0} ops");

        EditorGUILayout.EndHorizontal();
    }
}