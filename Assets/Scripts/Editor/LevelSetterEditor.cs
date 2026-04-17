using UnityEditor;
using UnityEngine;

//editor window to manually override the saved currentlevel in playerprefs.
//menu->dreamgames->setlevel
public class LevelSetterEditor : EditorWindow
{
    private int levelToSet = 1;

    [MenuItem("DreamGames/Set Level")]
    public static void ShowWindow()
    {
        GetWindow<LevelSetterEditor>("Set Level");
    }

    private void OnGUI()
    {
        GUILayout.Label("Set Current Level", EditorStyles.boldLabel);
        levelToSet = EditorGUILayout.IntField("Level:", levelToSet);
        if (GUILayout.Button("Apply"))
        {
            PlayerPrefs.SetInt("CurrentLevel", levelToSet);
            PlayerPrefs.Save();
            Debug.Log($"Level set to {levelToSet}");
        }
    }
}
