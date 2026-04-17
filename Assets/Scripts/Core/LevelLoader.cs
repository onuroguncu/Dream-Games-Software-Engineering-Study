using UnityEngine;

//loads a level json file from resources and parses it into leveldata
//file naming convention->level_01, ..., level_10
public class LevelLoader : MonoBehaviour
{
    public static LevelData LoadLevel(int levelNumber)
    {
        string fileName = "level_" + levelNumber.ToString("D2");
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);

        if (jsonFile == null)
        {
            Debug.LogError("Level file not found: " + fileName);
            return null;
        }

        return JsonUtility.FromJson<LevelData>(jsonFile.text);
    }
}
