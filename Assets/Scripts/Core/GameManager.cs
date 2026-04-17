using UnityEngine;
using UnityEngine.SceneManagement;

//survives scene transitions via dontdestroyonload
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMainScene() => SceneManager.LoadScene("MainScene");
    public void LoadLevelScene() => SceneManager.LoadScene("LevelScene");
}
