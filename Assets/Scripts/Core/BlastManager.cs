using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//group detection,normal blasts and special item creation
public class BlastManager : MonoBehaviour
{
    public static BlastManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void HandleCubeTap(CubeItem tappedCube)
    {
        List<CubeItem> group = FindGroup(tappedCube);
        if (group.Count < 2) return;

        GameStateManager.Instance.SetProcessing(true);
        LevelManager.Instance?.SpendMove();

        if (group.Count >= 6)
            StartCoroutine(BlastWithSpecial(group, tappedCube, isTnt: true));
        else if (group.Count >= 4)
            StartCoroutine(BlastWithSpecial(group, tappedCube, isTnt: false));
        else
            StartCoroutine(BlastNormal(group));
    }

    //shrink all cubes to zero destroy them then run fall and spawn
    private IEnumerator BlastNormal(List<CubeItem> group)
    {
        DamageAdjacentObstacles(group);

        Sequence seq = DOTween.Sequence();
        foreach (CubeItem cube in group)
        {
            seq.Join(cube.transform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetLink(cube.gameObject));
        }

        yield return seq.WaitForCompletion();

        foreach (CubeItem cube in group)
            cube.DestroyItem();

        yield return StartCoroutine(FallManager.Instance.ProcessFall());
        UpdateAllHints();
        GameStateManager.Instance.SetProcessing(false);
        LevelManager.Instance?.CheckWinLose();
    }

    //cubes fly toward the tapped cube in m.distance then rocket or TNT is created at the tapped position with creation wave effect
    private IEnumerator BlastWithSpecial(List<CubeItem> group, CubeItem tappedCube, bool isTnt)
    {
        DamageAdjacentObstacles(group);

        int spawnRow = tappedCube.Row;
        int spawnCol = tappedCube.Col;
        bool isVertical = Random.value > 0.5f; //random orientation for rockets
        CubeColor groupColor = tappedCube.Color;

        RectTransform tappedRt = tappedCube.GetComponent<RectTransform>();
        Vector2 targetAnchorPos = tappedRt != null ? tappedRt.anchoredPosition : Vector2.zero;

        //sort by m.distance so closer cubes merge first
        List<CubeItem> sorted = new List<CubeItem>(group);
        sorted.Remove(tappedCube);
        sorted.Sort((a, b) =>
        {
            float da = Mathf.Abs(a.Row - tappedCube.Row) + Mathf.Abs(a.Col - tappedCube.Col);
            float db = Mathf.Abs(b.Row - tappedCube.Row) + Mathf.Abs(b.Col - tappedCube.Col);
            return da.CompareTo(db);
        });

        //for large groups all cubes collapse simultaneously with no wave delay
        bool bigGroup   = group.Count >= 8;
        float moveDur   = bigGroup ? 0.03f : 0.02f;
        float delayStep = bigGroup ? 0f    : 0.01f;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject); //link to blastmanager so sequence dies if manager is destroyed

        //tapped cube pulses slightly to signal it's the merge target
        seq.Join(tappedCube.transform
            .DOScale(1.15f, 0.07f).SetEase(Ease.OutCubic));

        for (int i = 0; i < sorted.Count; i++)
        {
            CubeItem cube = sorted[i];
            int dist = Mathf.Abs(cube.Row - tappedCube.Row) + Mathf.Abs(cube.Col - tappedCube.Col);
            float delay = (dist - 1) * delayStep; //same distance cubes merge simultaneously

            RectTransform cubeRt = cube.GetComponent<RectTransform>();
            if (cubeRt != null)
            {
                seq.Join(cubeRt.DOAnchorPos(targetAnchorPos, moveDur)
                    .SetDelay(delay)
                    .SetEase(Ease.InBack));
            }
            seq.Join(cube.transform.DOScale(0f, moveDur * 0.6f)
                .SetDelay(delay + moveDur * 0.4f)
                .SetEase(Ease.InQuad));
        }

        yield return seq.WaitForCompletion();

        foreach (CubeItem cube in group)
        {
            GridManager.Instance.ClearCell(cube.Row, cube.Col);
            Destroy(cube.gameObject);
        }

        //flash and smoke rings at the creation point
        Vector3 spawnWorldPos = GridManager.Instance.GetWorldPosition(spawnRow, spawnCol);
        SpawnCreationWave(spawnWorldPos);

        CubeItem spawnedSpecial;
        if (isTnt)
            spawnedSpecial = GridManager.Instance.SpawnTNT(spawnRow, spawnCol, groupColor);
        else
            spawnedSpecial = GridManager.Instance.SpawnRocket(spawnRow, spawnCol, isVertical, groupColor);

        //pop the new special item in
        if (spawnedSpecial != null)
        {
            spawnedSpecial.transform.localScale = Vector3.zero;
            yield return spawnedSpecial.transform
                .DOScale(Vector3.one, 0.2f)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }

        yield return StartCoroutine(FallManager.Instance.ProcessFall());
        UpdateAllHints();
        GameStateManager.Instance.SetProcessing(false);
        LevelManager.Instance?.CheckWinLose();
    }

    //plays expanding ring and smoke particles at point where item is created
    private void SpawnCreationWave(Vector3 worldPos)
    {
        Sprite wave = SpriteCache.Get("Art/SpecialItems/TNT/Particles/particle_tnt_01");
        if (wave == null) return;

        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        SpawnWaveRing(worldPos, wave, container, cs * 2.6f, 0.35f, new Color(1f, 1f,   0.8f,  0.9f));
        SpawnWaveRing(worldPos, wave, container, cs * 1.5f, 0.22f, new Color(1f, 0.95f, 0.3f,  1f));
        SpawnWaveRing(worldPos, wave, container, cs * 0.8f, 0.15f, new Color(1f, 1f,   1f,    1f));
        SpawnWaveRing(worldPos, wave, container, cs * 0.4f, 0.10f, new Color(1f, 1f,   1f,    1f));

        Sprite smoke = SpriteCache.Get("Art/SpecialItems/Rocket/Particles/particle_smoke");
        if (smoke != null)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = (360f / 6f * i + Random.Range(-25f, 25f)) * Mathf.Deg2Rad;
                float dist  = Random.Range(cs * 0.3f, cs * 0.7f);
                Vector2 offset = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
                SpawnCreationSmoke(worldPos, smoke, container, offset, cs);
            }
        }
    }

    private void SpawnCreationSmoke(Vector3 worldPos, Sprite sprite, Transform container, Vector2 offset, float cs)
    {
        GameObject go = new GameObject("CreationSmoke", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = new Color(1f, 1f, 1f, 1f);
        img.raycastTarget = false;

        float size = Random.Range(cs * 0.15f, cs * 0.28f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.position = worldPos;
        rt.anchoredPosition += offset;
        rt.localScale = Vector3.one * 0.2f;

        float dur = Random.Range(0.25f, 0.4f);

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOScale(1f, dur * 0.4f).SetEase(Ease.OutBack));
        ps.Join(img.DOFade(0f, dur).SetEase(Ease.InQuad));
        ps.Join(rt.DOAnchorPos(rt.anchoredPosition + new Vector2(offset.x * 0.5f, offset.y * 0.5f + cs * 0.2f), dur).SetEase(Ease.OutQuad));
        ps.SetLink(go);
        ps.OnComplete(() => Destroy(go));
    }

    //expands ring sprite from zero to targetsize while fading out
    private void SpawnWaveRing(Vector3 worldPos, Sprite sprite, Transform container, float targetSize, float duration, Color color)
    {
        GameObject go = new GameObject("CreationWave", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;
        rt.position = worldPos;

        Sequence ps = DOTween.Sequence();
        ps.Append(rt.DOSizeDelta(new Vector2(targetSize, targetSize), duration).SetEase(Ease.OutQuad));
        ps.Join(img.DOFade(0f, duration).SetEase(Ease.InQuad));
        ps.SetLink(go);
        ps.OnComplete(() => Destroy(go));
    }

    //bfs flood fill to find all connected cubes of the same color as startcube
    //*rockets and tnts are excluded(they are handled by SpecialItemManager)
    public List<CubeItem> FindGroup(CubeItem startCube)
    {
        List<CubeItem> group = new List<CubeItem>();
        Queue<CubeItem> queue = new Queue<CubeItem>();
        HashSet<CubeItem> visited = new HashSet<CubeItem>();

        queue.Enqueue(startCube);
        visited.Add(startCube);

        while (queue.Count > 0)
        {
            CubeItem current = queue.Dequeue();
            group.Add(current);

            foreach (CubeItem neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor) && neighbor.Color == startCube.Color)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return group;
    }

    private List<CubeItem> GetNeighbors(CubeItem cube)
    {
        List<CubeItem> neighbors = new List<CubeItem>();
        TryAddCube(cube.Row + 1, cube.Col, neighbors);
        TryAddCube(cube.Row - 1, cube.Col, neighbors);
        TryAddCube(cube.Row, cube.Col + 1, neighbors);
        TryAddCube(cube.Row, cube.Col - 1, neighbors);
        return neighbors;
    }

    private void TryAddCube(int row, int col, List<CubeItem> list)
    {
        GridItem item = GridManager.Instance.GetItem(row, col);
        if (item is CubeItem cube && !cube.IsRocket && !cube.IsTnt)
            list.Add(cube);
    }

    //deals 1 damage to every unique noncube/nonstone neighbor of blast group
    private void DamageAdjacentObstacles(List<CubeItem> group)
    {
        HashSet<GridItem> damaged = new HashSet<GridItem>();
        foreach (CubeItem cube in group)
        {
            CheckObstacle(cube.Row + 1, cube.Col, damaged);
            CheckObstacle(cube.Row - 1, cube.Col, damaged);
            CheckObstacle(cube.Row, cube.Col + 1, damaged);
            CheckObstacle(cube.Row, cube.Col - 1, damaged);
        }
    }

    private void CheckObstacle(int row, int col, HashSet<GridItem> damaged)
    {
        GridItem item = GridManager.Instance.GetItem(row, col);
        if (item == null || item is CubeItem || item is StoneItem) return;
        if (damaged.Contains(item)) return;
        item.TakeDamage(1);
        damaged.Add(item);
    }

    //scans the entire grid and assigns rocket/tnt hint overlays to cubes
    public void UpdateAllHints()
    {
        for (int r = 0; r < GridManager.Instance.Rows; r++)
            for (int c = 0; c < GridManager.Instance.Cols; c++)
            {
                GridItem item = GridManager.Instance.GetItem(r, c);
                if (item is CubeItem cube) cube.SetHint(HintType.None);
            }

        HashSet<CubeItem> processed = new HashSet<CubeItem>();
        for (int r = 0; r < GridManager.Instance.Rows; r++)
            for (int c = 0; c < GridManager.Instance.Cols; c++)
            {
                GridItem item = GridManager.Instance.GetItem(r, c);
                if (item is CubeItem cube && !cube.IsRocket && !cube.IsTnt
                    && !processed.Contains(cube))
                {
                    List<CubeItem> group = FindGroup(cube);
                    HintType hint = HintType.None;
                    if (group.Count >= 6) hint = HintType.TNT;
                    else if (group.Count >= 4) hint = HintType.Rocket;
                    foreach (CubeItem c2 in group)
                    {
                        c2.SetHint(hint);
                        processed.Add(c2);
                    }
                }
            }
    }
}

public enum HintType { None, Rocket, TNT }
