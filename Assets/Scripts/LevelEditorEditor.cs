using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelEditor levelEditor = (LevelEditor)target;

        GUILayout.Space(20);

        var style1 = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("Instructions/Order Of Operations:", style1, GUILayout.ExpandWidth(true));

        if(GUILayout.Button("INSTRUCTIONS")) { Application.OpenURL(Application.dataPath + "/Instructions.txt"); } 
        GUILayout.Space(10);

        var style3 = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("Actions", style3, GUILayout.ExpandWidth(true));

        if(GUILayout.Button("Recreate Floor Plans")) { levelEditor.RecreateFloorPlans(); }
        if(GUILayout.Button("Refresh Floor Plans")) { levelEditor.RefreshFloorPlans(); }
        if(GUILayout.Button("Create Map")) { levelEditor.CreateMap(); }
        if(GUILayout.Button("Clear Map")) { levelEditor.ClearMap(); }
        if(GUILayout.Button("Create Editor Directory")) { levelEditor.CreateEditorDirectory(); }
        if(GUILayout.Button("Delete Editor Directory")) { levelEditor.DeleteLevelEditorDirectory(); }
    }
}
