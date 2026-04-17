using UnityEngine;

//entry point for the level scene
//loads level data, initializes all managers and kicks off the first hint pass
public class LevelSceneManager : MonoBehaviour
{
    public static LevelSceneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        //increase DOTween capacity upfront(for warnings tnt can produce many simultaneous tweens)
        DG.Tweening.DOTween.SetTweensCapacity(8000, 800);

        int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : 1;
        LevelData data = LevelLoader.LoadLevel(level);

        if (data == null)
        {
            Debug.LogError("LevelData null! Level: " + level);
            return;
        }

        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager.Instance null!");
            return;
        }

        GoalTracker.Instance?.Reset();
        LevelManager.Instance?.StartLevel(data.move_count);
        GridManager.Instance.InitGrid(data);

        LevelSceneUI.Instance?.InitializeMoveBox(data.move_count);
        LevelSceneUI.Instance?.InitializeGoalBox();

        //show special item hint overlays on cubes
        BlastManager.Instance.UpdateAllHints();
    }
}
