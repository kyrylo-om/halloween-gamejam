using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Scene GUI and Inspector for SplinePath.
/// Allows developers to click and drag waypoints directly in the 3D Scene View,
/// add/remove points, and manage the patrol path easily.
/// </summary>
[CustomEditor(typeof(SplinePath))]
public class SplinePathEditor : Editor
{
    private SplinePath path;

    void OnEnable()
    {
        path = (SplinePath)target;
    }

    void OnSceneGUI()
    {
        if (path == null || path.points == null) return;

        Undo.RecordObject(path, "Edit Spline Path");

        for (int i = 0; i < path.points.Count; i++)
        {
            Vector3 worldPos = path.GetWorldPoint(i);

            // Display waypoint label
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = (i == 0) ? Color.green : Color.yellow }
            };
            Handles.Label(worldPos + Vector3.up * 0.35f, $"Point {i}", labelStyle);

            // Draw interactive 3D Position Handle
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                path.SetWorldPoint(i, newWorldPos);
                EditorUtility.SetDirty(path);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Path Editing Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point After Last"))
        {
            Undo.RecordObject(path, "Add Spline Point");
            Vector3 lastPoint = path.points.Count > 0 ? path.points[path.points.Count - 1] : Vector3.zero;
            path.points.Add(lastPoint + new Vector3(1f, 0f, 0f));
            EditorUtility.SetDirty(path);
        }

        if (GUILayout.Button("Remove Last Point") && path.points.Count > 2)
        {
            Undo.RecordObject(path, "Remove Spline Point");
            path.points.RemoveAt(path.points.Count - 1);
            EditorUtility.SetDirty(path);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Align Y to 0 (Ground)"))
        {
            Undo.RecordObject(path, "Flatten Spline to Ground");
            for (int i = 0; i < path.points.Count; i++)
            {
                Vector3 p = path.points[i];
                p.y = 0f;
                path.points[i] = p;
            }
            EditorUtility.SetDirty(path);
        }

        if (GUILayout.Button("Reverse Direction"))
        {
            Undo.RecordObject(path, "Reverse Spline Direction");
            path.points.Reverse();
            EditorUtility.SetDirty(path);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Select points and drag the 3D handles in the Scene View to edit the patrol path.", MessageType.Info);
    }
}

