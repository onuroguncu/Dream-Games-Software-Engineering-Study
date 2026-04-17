using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

//win screen shown after all goals are cleared
//overlay fade → "Perfect!" text → star pop → particles → tap-to-continue
//accepts touch to return to the main menu
public class WinScreen : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [SerializeField] private TextMeshProUGUI perfectText;
    [SerializeField] private Image starImage;
    [SerializeField] private Transform particleContainer;
    [SerializeField] private TextMeshProUGUI tapToContinueText;

    private bool canTap = false;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        canTap = false;

        overlay.color = new Color(0, 0, 0, 0);
        perfectText.alpha = 0;
        starImage.transform.localScale = Vector3.zero;
        tapToContinueText.alpha = 0;

        perfectText.transform.localPosition = new Vector3(0, 800, 0);

        Sequence seq = DOTween.Sequence();

        seq.Append(overlay.DOFade(0.9f, 0.25f).SetEase(Ease.OutCubic));
        seq.Append(perfectText.DOFade(1f, 0.22f).SetEase(Ease.OutCubic));
        seq.Join(perfectText.transform.DOLocalMoveY(400, 0.35f).SetEase(Ease.OutBack));
        seq.Append(starImage.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));

        //star idle breathe loop after entry animation completes
        seq.AppendCallback(() =>
        {
            starImage.transform.DOScale(1.08f, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        });

        seq.AppendCallback(() => SpawnParticles());
        seq.AppendInterval(0.3f);
        seq.Append(tapToContinueText.DOFade(1f, 0.3f));

        //taptocontinue blink loop
        seq.AppendCallback(() =>
        {
            tapToContinueText.DOFade(0.2f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        });

        //delay before enabling tap so the player doesnt accidentally skip
        seq.AppendInterval(0.5f);
        seq.AppendCallback(() => canTap = true);
    }

    //spawns 80 looping celebration particles that radiate outward from center
    private void SpawnParticles()
    {
        Sprite particleSprite = SpriteCache.Get("Art/UI/Gameplay/Celebration/Particles/additive_particle_star");
        if (particleSprite == null) return;

        for (int i = 0; i < 80; i++)
        {
            GameObject go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(particleContainer, false);

            Image img = go.GetComponent<Image>();
            img.sprite = particleSprite;
            img.color = new Color(1f, 0.9f, 0.3f, 1f);

            RectTransform rt = go.GetComponent<RectTransform>();
            float size = Random.Range(15f, 50f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(80f, 350f);
            Vector2 targetPos = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

            float duration = Random.Range(0.8f, 1.6f);
            float delay = Random.Range(0f, 0.35f);

            rt.localScale = Vector3.zero;

            Sequence ps = DOTween.Sequence();
            ps.SetDelay(delay);
            ps.Append(rt.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
            ps.Join(rt.DOAnchorPos(targetPos, duration).SetEase(Ease.OutQuad));
            ps.Join(img.DOFade(0f, duration).SetEase(Ease.InQuad));
            ps.SetLoops(-1, LoopType.Restart);
        }
    }

    private void Update()
    {
        if (!canTap) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            LoadMain();
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            LoadMain();
    }

    private void LoadMain()
    {
        canTap = false;
        DOTween.KillAll();
        SceneLoader.LoadMain();
    }
}
