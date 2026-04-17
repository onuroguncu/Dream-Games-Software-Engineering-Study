using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

//displays and updates the goal slots in the cream box
//each slot shows an icon, remaining count, or a checkmark when cleared
public class GoalBox : MonoBehaviour
{
    [Header("Vase Slot")]
    [SerializeField] private GameObject slotVase;
    [SerializeField] private Image vaseIcon;
    [SerializeField] private TextMeshProUGUI vaseCount;
    [SerializeField] private Image vaseCheck;

    [Header("Stone Slot")]
    [SerializeField] private GameObject slotStone;
    [SerializeField] private Image stoneIcon;
    [SerializeField] private TextMeshProUGUI stoneCount;
    [SerializeField] private Image stoneCheck;

    [Header("Chalice Slot")]
    [SerializeField] private GameObject slotChalice;
    [SerializeField] private Image chaliceIcon;
    [SerializeField] private TextMeshProUGUI chaliceCount;
    [SerializeField] private Image chaliceCheck;

    //called once at level start to show only the goal types present in the level
    public void Initialize(System.Collections.Generic.Dictionary<ObstacleType, int> totals)
    {
        SetupSlot(slotVase, vaseIcon, vaseCount, vaseCheck, ObstacleType.Vase, totals);
        SetupSlot(slotStone, stoneIcon, stoneCount, stoneCheck, ObstacleType.Stone, totals);
        SetupSlot(slotChalice, chaliceIcon, chaliceCount, chaliceCheck, ObstacleType.Chalice, totals);
    }

    private void SetupSlot(GameObject slot, Image icon, TextMeshProUGUI count, Image check,
        ObstacleType type, System.Collections.Generic.Dictionary<ObstacleType, int> totals)
    {
        if (!totals.ContainsKey(type) || totals[type] <= 0)
        {
            slot.SetActive(false);
            return;
        }

        slot.SetActive(true);
        icon.sprite = LoadObstacleSprite(type);
        count.text = totals[type].ToString();
        count.gameObject.SetActive(true);
        check.gameObject.SetActive(false);
    }

    //called by goaltracker whenever an obstacle is cleared
    //shows checkmark when all are cleared otherwise updates count with a punch
    public void UpdateCount(ObstacleType type, int cleared, int total)
    {
        Image icon; TextMeshProUGUI count; Image check; RectTransform rt;

        switch (type)
        {
            case ObstacleType.Vase:
                icon = vaseIcon; count = vaseCount; check = vaseCheck;
                rt = slotVase.GetComponent<RectTransform>(); break;
            case ObstacleType.Stone:
                icon = stoneIcon; count = stoneCount; check = stoneCheck;
                rt = slotStone.GetComponent<RectTransform>(); break;
            case ObstacleType.Chalice:
                icon = chaliceIcon; count = chaliceCount; check = chaliceCheck;
                rt = slotChalice.GetComponent<RectTransform>(); break;
            default: return;
        }

        int remaining = total - cleared;

        if (remaining <= 0)
        {
            count.gameObject.SetActive(false);
            check.gameObject.SetActive(true);
            check.transform.localScale = Vector3.zero;
            check.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
        else
        {
            count.text = remaining.ToString();
            rt.DOKill();
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * 0.25f, 0.25f, 4, 0.5f);
        }
    }

    private Sprite LoadObstacleSprite(ObstacleType type)
    {
        return type switch
        {
            ObstacleType.Vase => SpriteCache.Get("Art/Obstacles/Vase/vase_01"),
            ObstacleType.Stone => SpriteCache.Get("Art/Obstacles/Stone/stone"),
            ObstacleType.Chalice => SpriteCache.Get("Art/Obstacles/ChaliceBox/Chalice"),
            _ => null
        };
    }
}
