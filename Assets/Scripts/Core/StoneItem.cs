using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//singlehit obstacle destroyed on first damage and cannot fall.
public class StoneItem : GridItem
{
    [SerializeField] private Image itemImage;
    private bool destroyed = false;

    public override void Init(int row, int col)
    {
        base.Init(row, col);
        GoalTracker.Instance?.RegisterObstacle(ObstacleType.Stone);
        itemImage.sprite = SpriteCache.Get("Art/Obstacles/Stone/stone");
    }

    public override void TakeDamage(int amount)
    {
        destroyed = true;
        GoalTracker.Instance?.ObstacleCleared(ObstacleType.Stone);
        SpawnBreakParticles();
        GridManager.Instance.ClearCell(Row, Col);
        Destroy(gameObject);
    }

    private void SpawnBreakParticles()
    {
        Sprite p1 = SpriteCache.Get("Art/Obstacles/Stone/Particles/particle_stone_01");
        Sprite p2 = SpriteCache.Get("Art/Obstacles/Stone/Particles/particle_stone_02");
        Sprite p3 = SpriteCache.Get("Art/Obstacles/Stone/Particles/particle_stone_03");

        RectTransform selfRt = GetComponent<RectTransform>();
        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        SpawnParticle(p1, selfRt, container, new Vector2( cs * 0.25f,  cs * 0.25f), new Vector2( cs * 0.5f,  cs * 0.6f));
        SpawnParticle(p2, selfRt, container, new Vector2(-cs * 0.25f,  cs * 0.25f), new Vector2(-cs * 0.5f,  cs * 0.6f));
        SpawnParticle(p2, selfRt, container, new Vector2( cs * 0.30f, -cs * 0.10f), new Vector2( cs * 0.6f,  cs * 0.3f));
        SpawnParticle(p3, selfRt, container, new Vector2(-cs * 0.30f, -cs * 0.10f), new Vector2(-cs * 0.6f,  cs * 0.3f));
        SpawnParticle(p1, selfRt, container, new Vector2( 0f,          cs * 0.30f), new Vector2( cs * 0.1f,  cs * 0.7f));
    }

    private void SpawnParticle(Sprite sprite, RectTransform selfRt, Transform container,
        Vector2 spawnOffset, Vector2 burstOffset)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("StoneParticle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(75f, 75f);
        rt.anchoredPosition = selfRt.anchoredPosition + spawnOffset;
        rt.localScale = Vector3.one;

        //P1 burst outward
        Vector2 burstPos = rt.anchoredPosition + burstOffset;
        float burstDur = Random.Range(0.12f, 0.18f);

        //P2 fall off screen under gravity
        Vector2 fallPos = new Vector2(
            burstPos.x + Random.Range(-40f, 40f),
            -2000f);
        float fallDur = Random.Range(0.5f, 0.7f);

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOAnchorPos(burstPos, burstDur).SetEase(Ease.OutQuad));
        ps.Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-120f, 120f)), burstDur));
        ps.Append(rt.DOAnchorPos(fallPos, fallDur).SetEase(Ease.InQuad));
        ps.Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-400f, 400f)), fallDur, RotateMode.FastBeyond360));
        ps.Join(img.DOFade(0f, fallDur * 0.5f).SetDelay(fallDur * 0.5f).SetEase(Ease.InQuad));
        ps.OnComplete(() => Destroy(go));
    }

    public override bool CanFall() => false;
    public override bool IsDestroyed() => destroyed;
}
