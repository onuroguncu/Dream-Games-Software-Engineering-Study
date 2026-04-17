using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//handles activation and explosion of rockets and TNTs
//also handles combos when two special items are adjacent
public class SpecialItemManager : MonoBehaviour
{
    public static SpecialItemManager Instance { get; private set; }

    //rocket part prefabs each rocket splits into two halves that fly in opposite directions
    [SerializeField] private GameObject rocketPartHPrefab;
    [SerializeField] private GameObject rocketPartVPrefab;

    private static Material trailMaterial;
    private int activeChains = 0;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        trailMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    //entry point when the player taps a rocket or TNT
    public void HandleSpecialTap(CubeItem tappedItem)
    {
        GameStateManager.Instance.SetProcessing(true);
        LevelManager.Instance?.SpendMove();
        StartCoroutine(TriggerSpecial(tappedItem));
    }

    //entry point when a rocket/tnt is hit by another explosion (chain reaction)
    public void TriggerByDamage(CubeItem item)
    {
        activeChains++;
        StartCoroutine(ExplodeOnly(item));
    }

    //explodes item without spending a move or triggering win/lose (used for chain reactions)
    private IEnumerator ExplodeOnly(CubeItem item)
    {
        int row = item.Row; int col = item.Col;
        bool isVertical = item.IsVertical; bool isTnt = item.IsTnt;
        GridManager.Instance.ClearCell(row, col);
        Destroy(item.gameObject);
        if (isTnt) yield return StartCoroutine(ExplodeTNT(row, col, 5));
        else       yield return StartCoroutine(ExplodeRocket(row, col, isVertical));
        activeChains--;
    }

    //checks for adjacent special items if found triggers a combo otherwise single explosion
    private IEnumerator TriggerSpecial(CubeItem item)
    {
        List<CubeItem> adjacentSpecials = GetAdjacentSpecials(item);

        if (adjacentSpecials.Count > 0)
            yield return StartCoroutine(HandleCombo(item, adjacentSpecials));
        else
            yield return StartCoroutine(ExplodeSingle(item));
    }

    //full singleitem explosion shrink animation->clear cell->explode->fall->win/lose check
    public IEnumerator ExplodeSingle(CubeItem item)
    {
        int row = item.Row;
        int col = item.Col;
        bool isVertical = item.IsVertical;
        bool isTnt = item.IsTnt;

        GridManager.Instance.ClearCell(row, col);
        yield return item.transform.DOScale(Vector3.zero, 0.15f)
            .SetEase(Ease.OutBack).WaitForCompletion();
        Destroy(item.gameObject);

        if (isTnt)
            yield return StartCoroutine(ExplodeTNT(row, col, 5));
        else
            yield return StartCoroutine(ExplodeRocket(row, col, isVertical));

        yield return new WaitUntil(() => activeChains == 0);
        yield return StartCoroutine(FallManager.Instance.ProcessFall());
        BlastManager.Instance.UpdateAllHints();
        GameStateManager.Instance.SetProcessing(false);
        LevelManager.Instance?.CheckWinLose();
    }

    //splits the rocket into two part objects that sweep across the grid step by step
    //each step damages cells and spawns trail glow and star particles
    public IEnumerator ExplodeRocket(int row, int col, bool isVertical)
    {
        Vector3 startPos = GridManager.Instance.GetWorldPosition(row, col);

        GameObject partA = Instantiate(rocketPartHPrefab, startPos, Quaternion.identity,
            GridManager.Instance.GridContainer);
        GameObject partB = Instantiate(rocketPartHPrefab, startPos, Quaternion.identity,
            GridManager.Instance.GridContainer);

        Image imgA = partA.GetComponent<Image>();
        Image imgB = partB.GetComponent<Image>();

        if (isVertical)
        {
            if (imgA != null) imgA.sprite = SpriteCache.Get("Art/SpecialItems/Rocket/vertical_rocket_part_top");
            if (imgB != null) imgB.sprite = SpriteCache.Get("Art/SpecialItems/Rocket/vertical_rocket_part_bottom");
        }
        else
        {
            if (imgA != null) imgA.sprite = SpriteCache.Get("Art/SpecialItems/Rocket/horizontal_rocket_part_right");
            if (imgB != null) imgB.sprite = SpriteCache.Get("Art/SpecialItems/Rocket/horizontal_rocket_part_left");
        }

        int maxSteps = isVertical ? GridManager.Instance.Rows : GridManager.Instance.Cols;
        float stepDur   = 0.05f;
        float totalDur  = maxSteps * stepDur;

        Sprite smokeSprite = SpriteCache.Get("Art/SpecialItems/Rocket/Particles/particle_smoke");
        Sprite starSprite  = SpriteCache.Get("Art/SpecialItems/Rocket/Particles/particle_star");

        Sequence damageSeq = DOTween.Sequence();
        for (int step = 1; step <= maxSteps; step++)
        {
            int s = step;
            damageSeq.AppendCallback(() =>
            {
                if (isVertical) { DamageCellInternal(row + s, col); DamageCellInternal(row - s, col); }
                else            { DamageCellInternal(row, col + s); DamageCellInternal(row, col - s); }
            });
            if (step < maxSteps) damageSeq.AppendInterval(stepDur);
        }

        //rocket parts move smoothly; OnUpdate spawns trail every frame for a continuous ribbon
        Vector3 endA = isVertical ? GridManager.Instance.GetWorldPosition(row + maxSteps, col)
                                  : GridManager.Instance.GetWorldPosition(row, col + maxSteps);
        Vector3 endB = isVertical ? GridManager.Instance.GetWorldPosition(row - maxSteps, col)
                                  : GridManager.Instance.GetWorldPosition(row, col - maxSteps);

        int tickA = 0, tickB = 0;
        partA.transform.DOMove(endA, totalDur).SetEase(Ease.Linear)
            .SetLink(partA, LinkBehaviour.KillOnDestroy)
            .OnUpdate(() =>
            {
                SpawnContinuousTrailGlow(partA.transform.position, smokeSprite, isVertical);
                if (tickA++ % 3 == 0)
                    for (int i = 0; i < 3; i++)
                        SpawnTrailStar(partA.transform.position, starSprite, GridManager.Instance.GridContainer, GridManager.Instance.CellSize);
            });

        partB.transform.DOMove(endB, totalDur).SetEase(Ease.Linear)
            .SetLink(partB, LinkBehaviour.KillOnDestroy)
            .OnUpdate(() =>
            {
                SpawnContinuousTrailGlow(partB.transform.position, smokeSprite, isVertical);
                if (tickB++ % 3 == 0)
                    for (int i = 0; i < 3; i++)
                        SpawnTrailStar(partB.transform.position, starSprite, GridManager.Instance.GridContainer, GridManager.Instance.CellSize);
            });

        yield return damageSeq.WaitForCompletion();
        yield return new WaitForSeconds(stepDur);
        Destroy(partA);
        Destroy(partB);
    }

    //spawns overlapping glow rects every frame to create a continuous connected trail
    private void SpawnContinuousTrailGlow(Vector3 worldPos, Sprite sprite, bool isVertical)
    {
        if (sprite == null) return;
        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        //outer glow — elongated in sweep direction so consecutive spawns overlap
        SpawnTrailGlow(worldPos, sprite, container, isVertical,
            isVertical ? new Vector2(cs * 0.9f, cs * 1.6f) : new Vector2(cs * 1.6f, cs * 0.9f),
            new Color(1f, 0.72f, 0f, 0.45f), 0.18f);

        //inner bright core
        SpawnTrailGlow(worldPos, sprite, container, isVertical,
            isVertical ? new Vector2(cs * 0.38f, cs * 1.2f) : new Vector2(cs * 1.2f, cs * 0.38f),
            new Color(1f, 1f, 0.75f, 0.75f), 0.12f);
    }

    private void SpawnTrailGlow(Vector3 worldPos, Sprite sprite, Transform container, bool isVertical,
        Vector2 size, Color color, float duration)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("RocketGlow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.material = trailMaterial;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.position = worldPos;

        img.DOFade(0f, duration).SetEase(Ease.InQuad).SetLink(go).OnComplete(() => Destroy(go));
    }

    private void SpawnTrailStar(Vector3 worldPos, Sprite sprite, Transform container, float cellSize)
    {
        if (sprite == null) return;

        float size     = Random.Range(10f, 22f);
        float duration = Random.Range(0.4f, 0.7f);

        GameObject go = new GameObject("RocketStar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 0.6f, 1f);
        img.raycastTarget = false;
        img.material = trailMaterial;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.position = worldPos;
        rt.anchoredPosition += new Vector2(
            Random.Range(-cellSize * 0.4f, cellSize * 0.4f),
            Random.Range(-cellSize * 0.4f, cellSize * 0.4f));
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float delay = Random.Range(0f, 0.1f);
        img.DOFade(0f, duration).SetDelay(delay).SetEase(Ease.InQuad).SetLink(go).OnComplete(() => Destroy(go));
        rt.DOScale(0.15f, duration).SetDelay(delay).SetEase(Ease.InQuad).SetLink(go);
    }

    //tnt damages a square area of (areaSize x areaSize) centered on the explosion cell
    public IEnumerator ExplodeTNT(int row, int col, int areaSize)
    {
        Vector3 center = GridManager.Instance.GetWorldPosition(row, col);
        SpawnTNTExplosion(center, areaSize);
        ShakeGrid();

        int half = areaSize / 2;
        yield return new WaitForSeconds(0.1f);

        for (int r = row - half; r <= row + half; r++)
            for (int c = col - half; c <= col + half; c++)
                DamageCellInternal(r, c);
    }

    private void ShakeGrid()
    {
        RectTransform grid = GridManager.Instance.GridContainer;
        grid.DOKill();
        grid.DOShakeAnchorPos(0.4f, strength: 12f, vibrato: 18, randomness: 60f, snapping: false)
            .SetEase(Ease.OutQuad);
    }

    private void SpawnTNTExplosion(Vector3 center, int areaSize)
    {
        Sprite burst = SpriteCache.Get("Art/SpecialItems/TNT/Particles/particle_tnt_01");
        Sprite spark = SpriteCache.Get("Art/SpecialItems/TNT/Particles/particle_tnt_02");
        Transform container = GridManager.Instance.GridContainer;
        float cellSize = GridManager.Instance.CellSize;
        float radius = (areaSize / 2f) * cellSize;

        SpawnTNTCenterFlash(center, spark, container, cellSize * 4f);

        //expanding burst rings from largest to smallest
        SpawnTNTBurst(center, burst, container, cellSize * 5f,   0.5f,  new Color(1f, 1f,   0.85f, 1f));
        SpawnTNTBurst(center, burst, container, cellSize * 3.5f, 0.4f,  new Color(1f, 0.85f, 0.1f, 1f));
        SpawnTNTBurst(center, burst, container, cellSize * 2f,   0.25f, new Color(1f, 0.5f,  0.0f, 1f));

        //28 sparks flying outward in all directions
        int sparkCount = 28;
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = (360f / sparkCount * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            float dist  = Random.Range(radius * 0.6f, radius * 1.5f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            SpawnTNTSpark(center, spark, container, dir, dist);
        }
    }

    private void SpawnTNTCenterFlash(Vector3 worldPos, Sprite sprite, Transform container, float size)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("TNTFlash", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 0.9f, 1f);
        img.raycastTarget = false;
        img.material = trailMaterial;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.position = worldPos;
        rt.localScale = Vector3.zero;

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOScale(1f, 0.08f).SetEase(Ease.OutExpo));
        ps.Append(rt.DOScale(1.3f, 0.1f).SetEase(Ease.InQuad));
        ps.Join(img.DOFade(0f, 0.1f).SetEase(Ease.InQuad));
        ps.SetLink(go);
        ps.OnComplete(() => Destroy(go));
    }

    private void SpawnTNTBurst(Vector3 worldPos, Sprite sprite, Transform container,
        float targetSize, float duration, Color color)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("TNTBurst", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.material = trailMaterial;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;
        rt.position = worldPos;

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOSizeDelta(new Vector2(targetSize, targetSize), duration * 0.45f).SetEase(Ease.OutExpo));
        ps.Join(img.DOFade(0f, duration).SetEase(Ease.InCubic));
        ps.SetLink(go);
        ps.OnComplete(() => Destroy(go));
    }

    private void SpawnTNTSpark(Vector3 worldPos, Sprite sprite, Transform container,
        Vector2 direction, float distance)
    {
        if (sprite == null) return;

        float size     = Random.Range(30f, 60f);
        float duration = Random.Range(0.4f, 0.65f);
        float delay    = Random.Range(0f, 0.06f);

        GameObject go = new GameObject("TNTSpark", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, Random.Range(0.75f, 1f), 0.1f, 1f);
        img.raycastTarget = false;
        img.material = trailMaterial;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.position = worldPos;
        rt.localScale = Vector3.one;

        Vector2 target = rt.anchoredPosition + direction * distance;

        Sequence ps = DOTween.Sequence();
        ps.PrependInterval(delay);
        ps.Append(rt.DOAnchorPos(target, duration).SetEase(Ease.OutQuad));
        ps.Join(rt.DOScale(0.1f, duration).SetEase(Ease.InQuad));
        ps.Join(img.DOFade(0f, duration * 0.5f).SetEase(Ease.InQuad).SetDelay(duration * 0.5f));
        ps.SetLink(go);
        ps.OnComplete(() => Destroy(go));
    }

    //handles combo when two or more special items are adjacent and one is tapped
    //tnt+tnt->bigger area (7x7). tnt+Rocket->3 rows or columns. rocket+rocket->cross sweep
    private IEnumerator HandleCombo(CubeItem tapped, List<CubeItem> adjacents)
    {
        int row = tapped.Row;
        int col = tapped.Col;

        //animate all adjacent specials flying into the tapped item's position
        Sequence seq = DOTween.Sequence();
        Vector3 targetPos = GridManager.Instance.GetWorldPosition(row, col);

        foreach (CubeItem adj in adjacents)
        {
            GridManager.Instance.ClearCell(adj.Row, adj.Col);
            seq.Join(adj.transform.DOMove(targetPos, 0.3f).SetEase(Ease.InQuad));
        }
        GridManager.Instance.ClearCell(row, col);

        yield return seq.WaitForCompletion();

        Destroy(tapped.gameObject);
        foreach (CubeItem adj in adjacents) Destroy(adj.gameObject);

        int tntCount = (tapped.IsTnt ? 1 : 0) + adjacents.FindAll(a => a.IsTnt).Count;
        int rocketCount = (tapped.IsRocket ? 1 : 0) + adjacents.FindAll(a => a.IsRocket).Count;

        if (tntCount >= 2)
            yield return StartCoroutine(ExplodeTNT(row, col, 7));
        else if (tntCount == 1 && rocketCount >= 1)
            yield return StartCoroutine(ExplodeTNTRocket(row, col));
        else
            yield return StartCoroutine(ExplodeRocketRocket(row, col));

        yield return new WaitUntil(() => activeChains == 0);
        yield return StartCoroutine(FallManager.Instance.ProcessFall());
        BlastManager.Instance.UpdateAllHints();
        GameStateManager.Instance.SetProcessing(false);
        LevelManager.Instance?.CheckWinLose();
    }

    //rocket+rocket combo sweep both axes simultaneously
    private IEnumerator ExplodeRocketRocket(int row, int col)
    {
        Coroutine h = StartCoroutine(ExplodeRocket(row, col, isVertical: false));
        Coroutine v = StartCoroutine(ExplodeRocket(row, col, isVertical: true));
        yield return h;
        yield return v;
    }

    //tnt+rocket combo fires rockets along 3 rows and 3 columns through center
    private IEnumerator ExplodeTNTRocket(int row, int col)
    {
        for (int r = row - 1; r <= row + 1; r++)
            StartCoroutine(ExplodeRocket(r, col, isVertical: false));

        for (int c = col - 1; c <= col + 1; c++)
            StartCoroutine(ExplodeRocket(row, c, isVertical: true));

        yield return new WaitForSeconds(0.5f);
    }

    //damages a single cell rockets tnts in the target cell are chaintriggered
    //chalicebox hits always go to the master cell
    private void DamageCellInternal(int row, int col)
    {
        GridItem item = GridManager.Instance.GetItem(row, col);
        if (item == null) return;

        if (item is CubeItem cube)
        {
            if (cube.IsRocket || cube.IsTnt)
                TriggerByDamage(cube);
            else
            {
                GridManager.Instance.ClearCell(row, col);
                cube.DestroyItem();
            }
        }
        else if (item is ChaliceBoxItem chaliceBox)
        {
            ChaliceBoxItem master = FindChaliceMaster(chaliceBox);
            if (master != null)
                master.TakeDamage(1);
        }
        else
            item.TakeDamage(1);
    }

    //searches the grid for the master chaliceboxitems
    private ChaliceBoxItem FindChaliceMaster(ChaliceBoxItem cb)
    {
        if (cb.IsMasterCell) return cb;
        for (int r = 0; r < GridManager.Instance.Rows; r++)
            for (int c = 0; c < GridManager.Instance.Cols; c++)
                if (GridManager.Instance.GetItem(r, c) is ChaliceBoxItem master && master.IsMasterCell)
                    return master;
        return null;
    }

    //public entry point used by external systems to damage a cell
    public void DamageCell(int row, int col) => DamageCellInternal(row, col);

    private List<CubeItem> GetAdjacentSpecials(CubeItem cube)
    {
        List<CubeItem> specials = new List<CubeItem>();
        CheckSpecial(cube.Row + 1, cube.Col, specials);
        CheckSpecial(cube.Row - 1, cube.Col, specials);
        CheckSpecial(cube.Row, cube.Col + 1, specials);
        CheckSpecial(cube.Row, cube.Col - 1, specials);
        return specials;
    }

    private void CheckSpecial(int row, int col, List<CubeItem> list)
    {
        GridItem item = GridManager.Instance.GetItem(row, col);
        if (item is CubeItem cube && (cube.IsRocket || cube.IsTnt))
            list.Add(cube);
    }
}
