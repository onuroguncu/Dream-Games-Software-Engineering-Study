using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

//handles gravity: drops floating items into empty cells, then fills remaining gaps with new cubes
//always called after any blast or special item explosion via ProcessFall()
public class FallManager : MonoBehaviour
{
    public static FallManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    //scans each column bottom-up, slides items down into empty slots, then spawns new cubes
    public IEnumerator ProcessFall()
    {
        List<(GridItem item, int fromRow, int toRow, int col)> fallList = new();

        for (int col = 0; col < GridManager.Instance.Cols; col++)
        {
            int emptyRow = 0;

            for (int row = 0; row < GridManager.Instance.Rows; row++)
            {
                GridItem item = GridManager.Instance.GetItem(row, col);

                if (item == null) continue;

                //obstacles (stone, chalice) block falling — items above them can't pass through
                if (!item.CanFall())
                {
                    emptyRow = row + 1;
                    continue;
                }

                if (emptyRow < row)
                {
                    GridManager.Instance.SetItem(emptyRow, col, item);
                    GridManager.Instance.ClearCell(row, col);
                    item.Row = emptyRow;
                    fallList.Add((item, row, emptyRow, col));
                }
                emptyRow++;
            }
        }

        //each item gets its own independent tween with SetLink so chain-reaction destroys
        //can't affect other items' tweens (shared Sequence breaks when any member is destroyed)
        //start existing-block fall tweens immediately (grid state already updated above)
        float maxFallDuration = 0f;
        foreach (var (item, fromRow, toRow, col) in fallList)
        {
            if (item == null || item.gameObject == null) continue;
            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt == null) continue;
            int distance = fromRow - toRow;
            float duration = Mathf.Clamp(0.1f + distance * 0.07f, 0.15f, 0.5f);
            Vector2 targetPos = GridManager.Instance.GetAnchoredPosition(toRow, col);
            rt.DOAnchorPos(targetPos, duration)
                .SetEase(Ease.InCubic)
                .SetLink(item.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    if (item != null && item.gameObject != null)
                        item.transform
                            .DOPunchScale(new Vector3(0.18f, -0.12f, 0f), 0.18f, 1, 0.4f)
                            .SetLink(item.gameObject);
                });
            if (duration > maxFallDuration) maxFallDuration = duration;
        }

        //spawn new cubes immediately so both waves fall in parallel
        float maxSpawnDuration = StartSpawnNewCubes();

        yield return new WaitForSeconds(Mathf.Max(maxFallDuration, maxSpawnDuration));
    }

    //starts fall tweens for all empty cells; returns the longest tween duration
    private float StartSpawnNewCubes()
    {
        float maxDuration = 0f;

        for (int col = 0; col < GridManager.Instance.Cols; col++)
        {
            for (int row = GridManager.Instance.Rows - 1; row >= 0; row--)
            {
                if (GridManager.Instance.GetItem(row, col) != null) continue;

                CubeItem newCube = GridManager.Instance.SpawnRandomCube(row, col);
                if (newCube == null) continue;

                RectTransform rt = newCube.GetComponent<RectTransform>();
                if (rt == null) continue;

                Image img = newCube.GetItemImage();
                if (img != null)
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);

                Vector2 startPos = GridManager.Instance.GetAnchoredPosition(
                    GridManager.Instance.Rows, col);
                rt.anchoredPosition = startPos;

                Vector2 targetPos = GridManager.Instance.GetAnchoredPosition(row, col);

                //delay increases with row index so lower rows land first (bottom-to-top cascade)
                float delay    = row * 0.05f;
                float duration = 0.1f + (GridManager.Instance.Rows - row) * 0.04f;

                rt.DOAnchorPos(targetPos, duration)
                    .SetDelay(delay)
                    .SetEase(Ease.InCubic)
                    .SetLink(newCube.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(() =>
                    {
                        if (newCube != null && newCube.gameObject != null)
                            newCube.transform
                                .DOPunchScale(new Vector3(0.18f, -0.12f, 0f), 0.18f, 1, 0.4f)
                                .SetLink(newCube.gameObject);
                    });

                if (img != null)
                    img.DOFade(1f, 0.05f)
                        .SetDelay(delay)
                        .SetLink(newCube.gameObject, LinkBehaviour.KillOnDestroy);

                float totalTime = delay + duration;
                if (totalTime > maxDuration) maxDuration = totalTime;
            }
        }

        return maxDuration;
    }
}
