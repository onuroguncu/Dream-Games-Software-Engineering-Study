using UnityEngine;
using UnityEngine.UI;
using TMPro;

//controls the main menu screen
//updates the level button label to show the current level or "Finished" if all levels are done
public class MainSceneUI : MonoBehaviour
{
    [SerializeField] private Button levelButton;
    [SerializeField] private TextMeshProUGUI levelButtonText;

    private void Start()
    {
        if (levelButton == null) return;
        levelButton.onClick.AddListener(OnLevelButtonClicked);
        UpdateButton();
    }

    private void OnEnable()
    {
        UpdateButton();
    }

    private void UpdateButton()
    {
        if (LevelManager.Instance == null) return;
        if (levelButtonText == null) return;
        if (LevelManager.Instance.IsFinished())
            levelButtonText.text = "Finished";
        else
            levelButtonText.text = "Level " + LevelManager.Instance.CurrentLevel;
    }

    private void OnLevelButtonClicked()
    {
        if (LevelManager.Instance == null) return;
        if (!LevelManager.Instance.IsFinished())
            SceneLoader.LoadLevel();
    }
}
