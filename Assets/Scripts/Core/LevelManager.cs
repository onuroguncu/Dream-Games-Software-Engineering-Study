using UnityEngine;
using UnityEngine.SceneManagement;

//manages level progression and move count
//survives scene transitions via dontdestroyonload
public class LevelManager : MonoBehaviour
{
    private const string LEVEL_KEY = "CurrentLevel";
    private const int TOTAL_LEVELS = 10;

    public static LevelManager Instance { get; private set; }

    public int CurrentLevel { get; private set; }
    public int MovesLeft { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
    }

    //called at level start to set the available moves for this session
    public void StartLevel(int moveCount)
    {
        MovesLeft = moveCount;
    }

    //decrements moves and notifies the cream box
    public void SpendMove()
    {
        MovesLeft--;
        LevelSceneUI.Instance?.SpendMove();
    }

    //advances currentlevel and persists it to playerprefs
    public void CompleteLevel()
    {
        if (CurrentLevel <= TOTAL_LEVELS) CurrentLevel++;
        PlayerPrefs.SetInt(LEVEL_KEY, CurrentLevel);
        PlayerPrefs.Save();
    }

    //called after every move and checks win first then lose
    public void CheckWinLose()
    {
        if (GoalTracker.Instance != null && GoalTracker.Instance.AllGoalsComplete())
        {
            CompleteLevel();
            LevelSceneUI.Instance?.ShowWinScreen();
            return;
        }

        if (MovesLeft <= 0)
            LevelSceneUI.Instance?.ShowFailPopup();
    }

    public bool IsFinished() => CurrentLevel > TOTAL_LEVELS;
}
