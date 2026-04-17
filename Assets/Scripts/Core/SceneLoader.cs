using UnityEngine;
using UnityEngine.SceneManagement;

//central helper for scene transitions.
//all scene loads go through here so scene names are defined in one place.
public class SceneLoader : MonoBehaviour
{
    public static void LoadMain() => SceneManager.LoadScene("MainScene");
    public static void LoadLevel() => SceneManager.LoadScene("LevelScene");
}
