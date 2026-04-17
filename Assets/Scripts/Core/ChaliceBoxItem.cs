using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

//2x2 obstacle with two phases
//P1(Door)-> destroyed on first hit and revealing chalices inside
//P2(Chalice)->10 chalices that each fall out individually on hits
//only the master cell(cbTL) handles damage and the other three cells reference it
public class ChaliceBoxItem : GridItem
{
    [SerializeField] private Image bgImage;
    [SerializeField] private Transform chaliceContainer;

    private enum Phase { Door, Chalice }
    private Phase currentPhase = Phase.Door;
    private bool doorDestroyed = false;
    private bool chalicePhaseReady = false;

    private int chalicesRemaining = 10;
    private const int CHALICE_GOAL = 10;
    private List<Image> chaliceImages = new List<Image>();

    public bool IsMasterCell { get; private set; }

    //called by gridmanager on the cbTL cell and registers goal count with goaltracker.
    public void SetAsMaster()
    {
        IsMasterCell = true;
        GoalTracker.Instance?.RegisterObstacle(ObstacleType.Chalice);
        GoalTracker.Instance?.RegisterObstacleWithCount(CHALICE_GOAL - 1);
    }

    public override void Init(int row, int col)
    {
        base.Init(row, col);
        UpdateSprite();
    }

    public override void TakeDamage(int amount)
    {
        if (!IsMasterCell) return;

        if (currentPhase == Phase.Door)
        {
            if (doorDestroyed) return;
            doorDestroyed = true;
            currentPhase = Phase.Chalice;
            SpawnDoorParticles();
            UpdateSprite();
            StartCoroutine(SpawnChalicesNextFrame());
        }
        else
        {
            if (!chalicePhaseReady) return;

            //remove a random chalice from the display
            for (int i = 0; i < amount; i++)
            {
                List<int> activeIndices = new List<int>();
                for (int j = 0; j < chaliceImages.Count; j++)
                    if (chaliceImages[j] != null)
                        activeIndices.Add(j);

                if (activeIndices.Count == 0) break;

                int randomIdx = activeIndices[Random.Range(0, activeIndices.Count)];
                SpawnChaliceParticles(GetComponent<RectTransform>());
                DropChalice(chaliceImages[randomIdx]);
                chaliceImages[randomIdx] = null;

                chalicesRemaining--;
                GoalTracker.Instance?.ObstacleCleared(ObstacleType.Chalice);
            }

            if (chalicesRemaining <= 0)
                DestroyItem();
        }
    }

    //wait one frame before spawning chalices so door sprite has been updated first
    private IEnumerator SpawnChalicesNextFrame()
    {
        yield return null;
        SpawnChalices();
        chalicePhaseReady = true;
    }

    //creates 10 chalice image objects in two rows inside the chalicecontainer
    private void SpawnChalices()
    {
        if (chaliceContainer == null) return;

        Sprite chaliceSprite = SpriteCache.Get("Art/Obstacles/ChaliceBox/Chalice");

        (Vector2 pos, float scale)[] pins = new (Vector2, float)[]
        {
            (new Vector2(-50f, -20f), 1.5f),
            (new Vector2(50f, -20f), 1.5f),
            (new Vector2(-30f, -22.5f), 1.5f),
            (new Vector2(30f, -22.5f), 1.5f),
            (new Vector2(0, -25f), 1.5f),
        };

        float baseSize = 28f;
        chaliceImages.Clear();
        foreach (Transform child in chaliceContainer)
            Destroy(child.gameObject);

        for (int raf = 0; raf < 2; raf++)
        {
            float yOffset = raf == 0 ? -20f : 55f;
            for (int i = 0; i < pins.Length; i++)
            {
                GameObject go = new GameObject($"Chalice_{raf}_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(chaliceContainer, false);

                float size = baseSize * pins[i].scale;
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(size, size * 1.2f);
                float globalOffsetX = -30f;
                rt.anchoredPosition = new Vector2(pins[i].pos.x + globalOffsetX, pins[i].pos.y + yOffset);

                Image img = go.GetComponent<Image>();
                img.sprite = chaliceSprite;
                img.preserveAspect = true;
                chaliceImages.Add(img);
            }
        }

        for (int i = chaliceImages.Count - 1; i >= 0; i--)
            chaliceImages[i].transform.SetAsFirstSibling();
    }

    private void UpdateSprite()
    {
        if (bgImage == null) return;
        if (currentPhase == Phase.Door)
            bgImage.sprite = SpriteCache.Get("Art/Obstacles/ChaliceBox/ChaliceBoxDoors");
        else
            bgImage.sprite = SpriteCache.Get("Art/Obstacles/ChaliceBox/ChaliceBoxBg");
    }

    public override bool CanFall() => false;
    public override bool IsDestroyed() => chalicesRemaining <= 0;

    private void SpawnDoorParticles()
    {
        RectTransform selfRt = GetComponent<RectTransform>();
        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        for (int i = 1; i <= 10; i++)
        {
            Sprite s = SpriteCache.Get($"Art/Obstacles/ChaliceBox/Particles/DoorParticles/chalice_box_door_particle_{i:D2}");
            float angle = (360f / 10f * (i - 1) + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            Vector2 spawnOff = new Vector2(
                Mathf.Cos(angle) * cs * 0.7f,
                Mathf.Sin(angle) * cs * 0.7f);
            Vector2 burstOff = new Vector2(
                Mathf.Cos(angle) * cs * Random.Range(0.9f, 1.4f),
                Mathf.Sin(angle) * cs * Random.Range(0.9f, 1.4f));
            SpawnParticle(s, selfRt, container, spawnOff, burstOff, Random.Range(38f, 55f));
        }
    }

    private void SpawnChaliceParticles(RectTransform chaliceRt)
    {
        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        Sprite[] sprites = new Sprite[5];
        for (int i = 1; i <= 5; i++)
            sprites[i - 1] = SpriteCache.Get($"Art/Obstacles/ChaliceBox/Particles/BaseParticles/chalice_box_particle_{i:D2}");

        for (int i = 0; i < 5; i++)
        {
            float angle = (360f / 5f * i + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
            Vector2 spawnOff = new Vector2(
                Mathf.Cos(angle) * cs * 0.5f,
                Mathf.Sin(angle) * cs * 0.5f);
            Vector2 burstOff = new Vector2(
                Mathf.Cos(angle) * cs * Random.Range(0.7f, 1.0f),
                Mathf.Sin(angle) * cs * Random.Range(0.7f, 1.0f));
            SpawnParticle(sprites[i], chaliceRt, container, spawnOff, burstOff, Random.Range(22f, 35f));
        }
    }

    //reparents chalice image to grid container then animates it flying up to one side before falling offscreen
    private void DropChalice(Image chaliceImg)
    {
        if (chaliceImg == null) return;

        RectTransform rt = chaliceImg.GetComponent<RectTransform>();

        Vector3 worldPos = rt.position;
        rt.SetParent(GridManager.Instance.GridContainer, false);
        rt.position = worldPos;

        float tiltDir = Random.value > 0.5f ? 1f : -1f;
        float cs = GridManager.Instance.CellSize;
        Vector2 startPos = rt.anchoredPosition;

        //P1 toss upward and sideways
        float popDur = Random.Range(0.18f, 0.28f);
        Vector2 popTarget = new Vector2(
            startPos.x + tiltDir * Random.Range(cs * 0.4f, cs * 0.8f),
            startPos.y + Random.Range(cs * 0.6f, cs * 1.1f));

        //P2 fall offscreen
        float fallDur = Random.Range(0.6f, 0.9f);
        Vector2 fallTarget = new Vector2(
            popTarget.x + tiltDir * Random.Range(cs * 0.3f, cs * 0.6f),
            -2000f);

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOAnchorPos(popTarget, popDur).SetEase(Ease.OutQuad));
        seq.Join(rt.DORotate(new Vector3(0f, 0f, tiltDir * Random.Range(15f, 35f)), popDur));
        seq.Append(rt.DOAnchorPos(fallTarget, fallDur).SetEase(Ease.InQuad));
        seq.Join(rt.DORotate(new Vector3(0f, 0f, tiltDir * Random.Range(80f, 150f)), fallDur, RotateMode.FastBeyond360));
        seq.Join(chaliceImg.DOFade(0f, fallDur * 0.35f)
            .SetDelay(fallDur * 0.65f).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(chaliceImg.gameObject));
    }

    private void SpawnParticle(Sprite sprite, RectTransform sourceRt, Transform container,
        Vector2 spawnOffset, Vector2 burstOffset, float size)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("ChaliceParticle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.position = sourceRt.position;
        rt.anchoredPosition += spawnOffset;
        rt.localScale = Vector3.one;

        Vector2 burstPos = rt.anchoredPosition + burstOffset;
        float burstDur = Random.Range(0.10f, 0.18f);

        Vector2 fallPos = new Vector2(
            burstPos.x + Random.Range(-35f, 35f),
            -2000f);
        float fallDur = Random.Range(1.0f, 1.4f);

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOAnchorPos(burstPos, burstDur).SetEase(Ease.OutQuad));
        ps.Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-100f, 100f)), burstDur));
        ps.Append(rt.DOAnchorPos(fallPos, fallDur).SetEase(Ease.InQuad));
        ps.Join(rt.DORotate(new Vector3(0f, 0f, Random.Range(-400f, 400f)), fallDur, RotateMode.FastBeyond360));
        ps.Join(img.DOFade(0f, fallDur * 0.5f).SetDelay(fallDur * 0.5f).SetEase(Ease.InQuad));
        ps.OnComplete(() => Destroy(go));
    }

    private void DestroyItem()
    {
        GridManager.Instance.ClearChaliceBoxCells(this); //clears all cells, not just the master
        Destroy(gameObject);
    }
}
