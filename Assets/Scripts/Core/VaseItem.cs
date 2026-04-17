using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//twohit obstacle 1st hit cracks it and sprite changes, second hit destroys it
public class VaseItem : GridItem
{
    [SerializeField] private Image itemImage;
    private int hp = 2;

    public override void Init(int row, int col)
    {
        base.Init(row, col);
        GoalTracker.Instance?.RegisterObstacle(ObstacleType.Vase);
        UpdateSprite();
    }

    public override void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            SpawnParticles(phase: 2); //fullbreak particles
            DestroyItem();
        }
        else
        {
            SpawnParticles(phase: 1); //crackhit particles
            UpdateSprite();
        }
    }

    private void SpawnParticles(int phase)
    {
        Sprite p1 = SpriteCache.Get("Art/Obstacles/Vase/Particles/particle_vase_01");
        Sprite p2 = SpriteCache.Get("Art/Obstacles/Vase/Particles/particle_vase_02");
        Sprite p3 = SpriteCache.Get("Art/Obstacles/Vase/Particles/particle_vase_03");

        RectTransform selfRt = GetComponent<RectTransform>();
        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        if (phase == 1)
        {
            //7 small chips(mostly upward and sideways)
            SpawnParticle(p2, selfRt, container, new Vector2(-cs*0.30f,  cs*0.20f), new Vector2(-cs*0.55f,  cs*0.65f), 30f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.30f,  cs*0.20f), new Vector2( cs*0.55f,  cs*0.65f), 30f);
            SpawnParticle(p2, selfRt, container, new Vector2(-cs*0.15f,  cs*0.25f), new Vector2(-cs*0.35f,  cs*0.75f), 28f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.15f,  cs*0.25f), new Vector2( cs*0.35f,  cs*0.75f), 28f);
            SpawnParticle(p2, selfRt, container, new Vector2(-cs*0.35f,  cs*0.00f), new Vector2(-cs*0.70f,  cs*0.40f), 25f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.35f,  cs*0.00f), new Vector2( cs*0.70f,  cs*0.40f), 25f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.00f,  cs*0.30f), new Vector2( cs*0.10f,  cs*0.80f), 27f);
        }
        else
        {
            //10 mixedsize pieces(full burst on destruction)
            SpawnParticle(p1, selfRt, container, new Vector2(-cs*0.25f,  cs*0.25f), new Vector2(-cs*0.50f,  cs*0.80f), 45f);
            SpawnParticle(p3, selfRt, container, new Vector2( cs*0.25f,  cs*0.25f), new Vector2( cs*0.50f,  cs*0.80f), 45f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.00f,  cs*0.30f), new Vector2( cs*0.05f,  cs*0.90f), 40f);
            SpawnParticle(p1, selfRt, container, new Vector2(-cs*0.35f,  cs*0.10f), new Vector2(-cs*0.75f,  cs*0.55f), 38f);
            SpawnParticle(p3, selfRt, container, new Vector2( cs*0.35f,  cs*0.10f), new Vector2( cs*0.75f,  cs*0.55f), 38f);
            SpawnParticle(p2, selfRt, container, new Vector2(-cs*0.20f,  cs*0.00f), new Vector2(-cs*0.45f,  cs*0.45f), 30f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.20f,  cs*0.00f), new Vector2( cs*0.45f,  cs*0.45f), 30f);
            SpawnParticle(p2, selfRt, container, new Vector2(-cs*0.10f,  cs*0.20f), new Vector2(-cs*0.25f,  cs*0.70f), 27f);
            SpawnParticle(p2, selfRt, container, new Vector2( cs*0.10f,  cs*0.20f), new Vector2( cs*0.25f,  cs*0.70f), 27f);
            SpawnParticle(p1, selfRt, container, new Vector2( cs*0.00f, -cs*0.10f), new Vector2( cs*0.15f,  cs*0.50f), 35f);
        }
    }

    private void SpawnParticle(Sprite sprite, RectTransform selfRt, Transform container,
        Vector2 spawnOffset, Vector2 burstOffset, float size)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("VaseParticle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = selfRt.anchoredPosition + spawnOffset;
        rt.localScale = Vector3.one;

        //P1 burst outward
        Vector2 burstPos = rt.anchoredPosition + burstOffset;
        float burstDur = Random.Range(0.10f, 0.16f);

        //P2 fall off screen under gravity
        Vector2 fallPos = new Vector2(
            burstPos.x + Random.Range(-30f, 30f),
            -2000f);
        float fallDur = Random.Range(0.75f, 1.0f);

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOAnchorPos(burstPos, burstDur).SetEase(Ease.OutQuad));
        ps.Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-90f, 90f)), burstDur));
        ps.Append(rt.DOAnchorPos(fallPos, fallDur).SetEase(Ease.InQuad));
        ps.Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-400f, 400f)), fallDur, RotateMode.FastBeyond360));
        ps.Join(img.DOFade(0f, fallDur * 0.5f).SetDelay(fallDur * 0.5f).SetEase(Ease.InQuad));
        ps.OnComplete(() => Destroy(go));
    }

    //switches sprite between intact
    private void UpdateSprite()
    {
        if (itemImage == null) return;
        string spriteName = hp == 2 ? "vase_01" : "vase_02";
        itemImage.sprite = SpriteCache.Get($"Art/Obstacles/Vase/{spriteName}");
    }

    public override bool CanFall() => true;
    public override bool IsDestroyed() => hp <= 0;

    private void DestroyItem()
    {
        GoalTracker.Instance?.ObstacleCleared(ObstacleType.Vase);
        GridManager.Instance.ClearCell(Row, Col);
        Destroy(gameObject);
    }
}
