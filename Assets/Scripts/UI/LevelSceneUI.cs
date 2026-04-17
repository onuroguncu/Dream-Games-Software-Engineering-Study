using UnityEngine;
using TMPro;
using DG.Tweening;

//banner controller for the level scene.
//delegates to movebox and goalbox components shows win fail overlays
public class LevelSceneUI : MonoBehaviour
{
    public static LevelSceneUI Instance { get; private set; }

    [SerializeField] private MoveBox moveBox;
    [SerializeField] private GoalBox goalBox;
    [SerializeField] private GameObject failPopup;
    [SerializeField] private TextMeshProUGUI failTitleText;
    [SerializeField] private WinScreen winScreen;

    public void ShowWinScreen() => winScreen.Show();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void InitializeMoveBox(int moveCount) =>
        moveBox.Initialize(moveCount);

    public void InitializeGoalBox() =>
        goalBox.Initialize(GoalTracker.Instance.GetAllTotals());

    public void SpendMove() =>
        moveBox.SpendMove();

    public bool HasMoves() =>
        moveBox.HasMoves();

    public void UpdateGoal(ObstacleType type, int cleared, int total) =>
        goalBox?.UpdateCount(type, cleared, total);

    //shows the fail popup with a bouncein animation
    public void ShowFailPopup()
    {
        if (failPopup == null) return;
        if (failTitleText != null)
        {
            int level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : 1;
            failTitleText.text = $"Level {level}";
        }

        failPopup.SetActive(true);

        RectTransform rt = failPopup.GetComponent<RectTransform>();
        rt.localScale = Vector3.zero;
        Vector2 originalPos = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(originalPos.x, originalPos.y + 100f);

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(1.1f, 0.32f).SetEase(Ease.OutBack));
        seq.Join(rt.DOAnchorPos(originalPos, 0.32f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));
    }

    public void OnCloseButton() =>
        SceneLoader.LoadMain();

    public void OnTryAgainButton()
    {
        if (failPopup != null) failPopup.SetActive(false);
        SceneLoader.LoadLevel();
    }
}
