using UnityEngine;
using TMPro;
using DG.Tweening;

//cram box component that displays and animates the remaining move count
public class MoveBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private RectTransform boxRect;
    [SerializeField] private Color normalColor = new Color(1f, 0.92f, 0.73f);

    private int remaining;

    public void Initialize(int moveCount)
    {
        remaining = moveCount;
        countText.text = remaining.ToString();
        countText.color = normalColor;
        boxRect.localScale = Vector3.one;
    }

    //decrements count and plays a punch scale animation to give feedback
    public void SpendMove()
    {
        if (remaining <= 0) return;

        remaining--;
        countText.text = remaining.ToString();

        PlayPunchAnimation();
    }

    private void PlayPunchAnimation()
    {
        boxRect.DOKill();
        boxRect.localScale = Vector3.one;
        boxRect.DOPunchScale(Vector3.one * 0.25f, 0.25f, 4, 0.5f);
    }

    public bool HasMoves() => remaining > 0;
}
