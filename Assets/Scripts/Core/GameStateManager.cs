using UnityEngine;

//tracks whether the game is currently running an animation or processing a move
//while isprocessing is true inputhandler blocks all player input
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public bool IsProcessing { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetProcessing(bool value) => IsProcessing = value;
}
