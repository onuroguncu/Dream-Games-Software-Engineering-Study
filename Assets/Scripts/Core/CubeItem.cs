using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum CubeColor { Red, Green, Blue, Yellow }

//represents a colored cube on the grid. also used for rockets and TNTs via SetSpecialState
//handles sprite selection, hint overlays, destruction with particles, and blast particle effects
public class CubeItem : GridItem
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image hintOverlay;

    public CubeColor Color { get; private set; }
    public bool IsRocket { get; private set; }
    public bool IsTnt { get; private set; }
    public bool IsVertical { get; private set; }

    private void Awake()
    {
        if (itemImage == null)
            itemImage = GetComponent<Image>();
        if (itemImage == null)
            itemImage = gameObject.AddComponent<Image>();

        if (hintOverlay != null)
            hintOverlay.gameObject.SetActive(false);
    }

    public Image GetItemImage() => itemImage;

    public void SetColor(CubeColor color)
    {
        Color = color;
        if (itemImage == null)
            itemImage = GetComponent<Image>();
        UpdateSprite();
    }

    //promotes this cube to a rocket or TNT. called by GridManager on spawn and by BlastManager on creation
    public void SetSpecialState(bool isRocket, bool isTnt, bool isVertical = false)
    {
        IsRocket = isRocket;
        IsTnt = isTnt;
        IsVertical = isVertical;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (itemImage == null) return;
        string colorName = Color.ToString().ToLower();

        if (IsTnt)
            itemImage.sprite = SpriteCache.Get("Art/SpecialItems/TNT/TNT");
        else if (IsRocket)
        {
            string dir = IsVertical ? "vertical" : "horizontal";
            itemImage.sprite = SpriteCache.Get($"Art/SpecialItems/Rocket/{dir}_rocket");
        }
        else
            itemImage.sprite = SpriteCache.Get($"Art/Cubes/DefaultState/{colorName}");

        if (itemImage.sprite == null)
            Debug.LogWarning($"Sprite not found: {colorName}");

        var c = itemImage.color;
        itemImage.color = new Color(c.r, c.g, c.b, 1f);
    }

    //shows a rocket or TNT hint overlay on cubes that belong to a qualifying group
    //HintType.None hides the overlay
    public void SetHint(HintType hint)
    {
        if (hintOverlay == null) return;
        switch (hint)
        {
            case HintType.None:
                hintOverlay.gameObject.SetActive(false);
                break;
            case HintType.Rocket:
                hintOverlay.gameObject.SetActive(true);
                hintOverlay.sprite = SpriteCache.Get($"Art/Cubes/RocketState/{Color.ToString().ToLower()}_rocket");
                break;
            case HintType.TNT:
                hintOverlay.gameObject.SetActive(true);
                hintOverlay.sprite = SpriteCache.Get($"Art/Cubes/TntState/{Color.ToString().ToLower()}_tnt");
                break;
        }
    }

    public override bool CanFall() => true;
    public override void TakeDamage(int amount) => DestroyItem();

    public void DestroyItem()
    {
        SpawnBlastParticles();
        GridManager.Instance.ClearCell(Row, Col);
        Destroy(gameObject);
    }

    private void SpawnBlastParticles()
    {
        string colorName = Color.ToString().ToLower();
        Sprite particleSprite = SpriteCache.Get($"Art/Cubes/Particles/particle_{colorName}");
        if (particleSprite == null) return;

        RectTransform selfRt = GetComponent<RectTransform>();
        Transform container = GridManager.Instance.GridContainer;
        float cs = GridManager.Instance.CellSize;

        //15 tiny semi-transparent yellow stars that scatter outward and fade
        Sprite starSprite = SpriteCache.Get("Art/SpecialItems/Rocket/Particles/particle_star");
        if (starSprite != null)
        {
            float[] starSizes = { 10f, 12f, 9f, 13f, 10f, 14f, 9f, 12f, 10f, 13f, 9f, 12f, 10f, 14f, 9f };
            for (int i = 0; i < 15; i++)
            {
                GameObject go = new GameObject("BlastStar", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(container, false);

                Image img = go.GetComponent<Image>();
                img.sprite = starSprite;
                img.color = new Color(1f, 0.95f, 0.6f, Random.Range(0.45f, 0.7f));
                img.raycastTarget = false;

                RectTransform rt = go.GetComponent<RectTransform>();
                float size = starSizes[i];
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = selfRt.anchoredPosition + new Vector2(
                    Random.Range(-cs * 0.4f, cs * 0.4f),
                    Random.Range(-cs * 0.4f, cs * 0.4f));
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(cs * 0.15f, cs * 0.5f);
                Vector2 target = rt.anchoredPosition + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
                float delay = Random.Range(0f, 0.05f);
                float dur = Random.Range(0.2f, 0.38f);

                img.DOFade(0f, dur).SetDelay(delay).SetEase(Ease.InQuad).SetLink(go).OnComplete(() => Destroy(go));
                rt.DOScale(0f, dur).SetDelay(delay).SetEase(Ease.InQuad).SetLink(go);
                rt.DOAnchorPos(target, dur).SetDelay(delay).SetEase(Ease.OutQuad).SetLink(go);
            }
        }

        //5 colored particles with independent X/Y easing:
        //X uses OutCubic (fast spread then decelerate), Y uses InQuad (gravity acceleration)
        //this ensures all particles visibly spread sideways while falling off-screen
        for (int p = 0; p < 5; p++)
        {
            GameObject go = new GameObject("BlastParticle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(container, false);

            Image img = go.GetComponent<Image>();
            img.sprite = particleSprite;
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            float size = Random.Range(14f, 28f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = selfRt.anchoredPosition;
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float sideSign = Random.value > 0.5f ? -1f : 1f;
            float targetX = selfRt.anchoredPosition.x + sideSign * Random.Range(cs * 1.2f, cs * 2.5f);
            float xDur = Random.Range(0.45f, 0.8f);
            float yDur = Random.Range(0.7f, 1.05f);
            float delay = Random.Range(0f, 0.06f);
            float fadeDur = 0.22f;
            float fadeDelay = yDur * 0.62f;

            rt.DOAnchorPosX(targetX, xDur).SetDelay(delay).SetEase(Ease.OutCubic).SetLink(go);
            rt.DOAnchorPosY(-2000f, yDur).SetDelay(delay).SetEase(Ease.InQuad).SetLink(go);
            rt.DORotate(new Vector3(0f, 0f, Random.Range(-400f, 400f)), yDur, RotateMode.FastBeyond360).SetDelay(delay).SetLink(go);
            img.DOFade(0f, fadeDur).SetDelay(delay + fadeDelay).SetEase(Ease.InQuad).SetLink(go).OnComplete(() => Destroy(go));
        }
    }

    public override bool IsDestroyed() => false;
}
