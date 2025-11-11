using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // NEW: new input system

public class GroupLogoDisplay : MonoBehaviour
{
    [Header("Canvas Groups")]
    public CanvasGroup LogoCanvasGroup;

    [Header("UI Elements")]
    public TextMeshProUGUI GroupNumberText;
    public TextMeshProUGUI TeamMembersText;
    public TextMeshProUGUI TeamNameText;

    [Header("Logo Display")]
    public GameObject LogoImageObject;
    public SpriteRenderer LogoSpriteRenderer;
    public Sprite[] IconSprites;

    [Header("Pixelate Reveal")]
    public List<int> PixelateSizes = new List<int> { 2, 4, 8, 16, 32, 64, 128, 256, 512 };
    public float RevealStepDelay = 1f;

    [Header("Slide Settings")]
    public float SlideDistance = 1600f;
    public float SlideDuration = 0.6f;
    public Ease SlideEase = Ease.OutCubic;
    public Ease SlideHideEase = Ease.InCubic;
    public bool SlideFromRight = true;

    [Header("Logo Bounce Settings")]
    public float FinalLogoScale = 0.5f;        // final resting scale
    public float LogoBounceInScale1 = 1.25f;   // overshoot 1
    public float LogoBounceInScale2 = 0.95f;   // undershoot
    public float LogoBounceInScale3 = 1.10f;   // overshoot 2
    public float LogoBounceInTime1  = 0.18f;
    public float LogoBounceInTime2  = 0.14f;
    public float LogoBounceInTime3  = 0.12f;
    public float LogoBounceInSettleTime = 0.10f;

    public float LogoBounceOutPeakScale = 1.20f;
    public float LogoBounceOutUpTime = 0.12f;
    public float LogoBounceOutDownTime = 0.22f;
    public Ease  LogoBounceInEase1 = Ease.OutBack;
    public Ease  LogoBounceInEase2 = Ease.OutQuad;
    public Ease  LogoBounceInEase3 = Ease.OutQuad;
    public Ease  LogoBounceInSettleEase = Ease.OutQuad;
    public Ease  LogoBounceOutUpEase = Ease.OutQuad;
    public Ease  LogoBounceOutDownEase = Ease.InCubic;

    [Header("Logo Wiggle After Reveal")]
    public float LogoWiggleAngle = 15f;     // degrees (left/right)
    public float LogoWiggleTime  = 0.12f;   // time per leg
    public Ease  LogoWiggleEase  = Ease.OutSine;

    public AllIn1_ShineSweep AllIn1_ShineSweep;

    [Header("Interaction")]
    public bool ClickToReveal = true;             // NEW: gate un-pixelate on click
    public LayerMask LogoRaycastLayer = ~0;       // NEW: optional mask for raycast (default = everything)

    private int currentGroupNumber;
    private bool[] hasRevealedLogo;
    private GroupRevealManager revealManager;
    private Sprite currentLogoSprite;

    private Sequence revealSeq;
    private Sequence logoInSeq;
    private Sequence logoOutSeq;
    private Tween    slideTween;

    private RectTransform panelRect;
    private Transform logoTransform;

    // NEW: runtime state
    private bool waitingForClick = false;
    private Camera mainCam;

    private void Awake()
    {
        panelRect = LogoCanvasGroup != null ? LogoCanvasGroup.GetComponent<RectTransform>() : null;
        logoTransform = LogoImageObject != null ? LogoImageObject.transform : null;
        mainCam = Camera.main; // NEW

        if (LogoCanvasGroup != null)
        {
            LogoCanvasGroup.alpha = 1f;
            LogoCanvasGroup.interactable = false;
            LogoCanvasGroup.blocksRaycasts = false;
        }

        if (panelRect != null)
        {
            Vector2 pos = panelRect.anchoredPosition;
            float dir = SlideFromRight ? 1f : -1f;
            panelRect.anchoredPosition = pos + new Vector2(SlideDistance * dir, 0);
        }

        if (logoTransform != null)
            logoTransform.localScale = Vector3.zero;
    }

    private void Update()
    {
        // NEW: if we’re waiting for a logo click, look for left mouse press and hit-test the logo
        if (waitingForClick)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (ClickedLogoSprite())
                {
                    waitingForClick = false;
                    StartNewRevealAnimation();
                }
            }
        }
    }

    public void Initialize(int totalGroups, GroupRevealManager manager)
    {
        hasRevealedLogo = new bool[totalGroups];
        revealManager = manager;
    }

    public void ShowLogoScreen(int groupNumber, List<string> studentNames, Sprite logoSprite)
    {
        StoreCurrentGroupData(groupNumber, logoSprite);
        UpdateUIElements(studentNames);

        KillAllTweens();

        AssignLogoSprite();

        bool alreadyRevealed = HasBeenRevealed(groupNumber);
        if (alreadyRevealed)
        {
            SetPixelateSizeSafe(512);
            SetBrightnessSafe(0f);
        }
        else
        {
            int startPix = PixelateSizes.Count > 0 ? PixelateSizes[0] : 64;
            SetPixelateSizeSafe(startPix);
            SetBrightnessSafe(0f);
        }

        ShowLogoImage();
        if (logoTransform != null) logoTransform.localScale = Vector3.zero;

        // Ensure a collider exists for sprite click detection (NEW)
        EnsureLogoCollider2D();

        SlideInPanel(() =>
        {
            PlayLogoBounceIn(() =>
            {
                if (alreadyRevealed)
                {
                    DisplayTeamNameFancy();
                }
                else
                {
                    if (ClickToReveal)
                    {
                        // Wait for user click on the logo before starting reveal (NEW)
                        waitingForClick = true;
                    }
                    else
                    {
                        // Old behavior: auto-start reveal
                        StartNewRevealAnimation();
                    }
                }
            });
        });
    }

    public void HideLogoScreen()
    {
        if (revealSeq != null) { revealSeq.Kill(); revealSeq = null; }
        waitingForClick = false; // NEW: cancel waiting state

        PlayLogoBounceOut(() =>
        {
            SlideOutPanel(() =>
            {
                HideLogoImage();
                if (LogoCanvasGroup != null)
                {
                    LogoCanvasGroup.interactable = false;
                    LogoCanvasGroup.blocksRaycasts = false;
                }
            });
        });
    }

    private void StoreCurrentGroupData(int groupNumber, Sprite logoSprite)
    {
        currentGroupNumber = groupNumber;
        currentLogoSprite = logoSprite;
    }

    private void UpdateUIElements(List<string> studentNames)
    {
        if (GroupNumberText != null)
            GroupNumberText.text = $"Group {currentGroupNumber}";

        if (TeamNameText != null)
            TeamNameText.text = "";

        if (TeamMembersText != null)
        {
            TeamMembersText.text = string.Join(", ", studentNames);
            LogoRevealFX.PulseText(TeamMembersText, flash: Color.cyan, upScale: 1.08f);
        }
    }

    private void SetupLogoSprite()
    {
        ShowLogoImage();
        AssignLogoSprite();
    }

    private void ShowLogoImage()
    {
        if (LogoImageObject != null)
        {
            LogoImageObject.SetActive(true);
        }
    }

    private void HideLogoImage()
    {
        if (LogoImageObject != null)
            LogoImageObject.SetActive(false);
    }

    private void AssignLogoSprite()
    {
        if (LogoSpriteRenderer != null && currentLogoSprite != null)
            LogoSpriteRenderer.sprite = currentLogoSprite;
    }

    private bool HasBeenRevealed(int groupNumber)
    {
        return hasRevealedLogo != null && hasRevealedLogo[groupNumber - 1];
    }

    private void StartNewRevealAnimation()
    {
        if (LogoSpriteRenderer == null || LogoSpriteRenderer.material == null)
            return;

        revealSeq?.Kill();
        int startPix = PixelateSizes.Count > 0 ? PixelateSizes[0] : 64;
        SetPixelateSizeSafe(startPix);
        SetBrightnessSafe(-1f);

        revealSeq = LogoRevealFX.BuildLogoRevealSequence(
            logoRenderer: LogoSpriteRenderer,
            logoTransform: null,
            pixelSizes: PixelateSizes,
            stepDelay: RevealStepDelay,
            brightenTime: 0f,
            onComplete: () => PlayLogoWiggle(DisplayTeamNameFancy)
        );
    }

    private void DisplayTeamNameFancy()
    {
        string teamName = "Unknown Team";
        if (LogoSpriteRenderer != null && LogoSpriteRenderer.sprite != null)
            teamName = LogoSpriteRenderer.sprite.name;

        if (TeamNameText != null)
            LogoRevealFX.TeamNameReveal(TeamNameText, teamName, typeTime: 0.6f, scrambleUpper: false);

        MarkLogoAsRevealed();
        NotifyRevealManager();

        AllIn1_ShineSweep.TriggerShine();
    }

    private void MarkLogoAsRevealed()
    {
        if (hasRevealedLogo != null)
            hasRevealedLogo[currentGroupNumber - 1] = true;
    }

    private void PlayLogoWiggle(System.Action onComplete)
    {
        if (logoTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        logoTransform.DOKill();
        logoTransform.localRotation = Quaternion.identity;

        DOTween.Sequence()
            .Append(logoTransform.DORotate(new Vector3(0, 0, -LogoWiggleAngle), LogoWiggleTime).SetEase(LogoWiggleEase))
            .Append(logoTransform.DORotate(new Vector3(0, 0,  LogoWiggleAngle), LogoWiggleTime * 1.1f).SetEase(LogoWiggleEase))
            .Append(logoTransform.DORotate(Vector3.zero,                           LogoWiggleTime).SetEase(LogoWiggleEase))
            .OnComplete(() => onComplete?.Invoke());
    }

    private void NotifyRevealManager()
    {
        if (revealManager != null)
            revealManager.OnLogoRevealed(currentGroupNumber, currentLogoSprite);
    }

    private void SetPixelateSizeSafe(int size)
    {
        if (LogoSpriteRenderer != null && LogoSpriteRenderer.material != null)
            LogoSpriteRenderer.material.SetInt(LogoRevealFX.PixelProp, size);
    }

    private void SetBrightnessSafe(float value)
    {
        if (LogoSpriteRenderer != null && LogoSpriteRenderer.material != null)
            LogoSpriteRenderer.material.SetFloat(LogoRevealFX.BrightProp, value);
    }

    private void SlideInPanel(System.Action onComplete)
    {
        if (LogoCanvasGroup == null || panelRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        LogoCanvasGroup.interactable = true;
        LogoCanvasGroup.blocksRaycasts = true;

        slideTween?.Kill();

        Vector2 endPos = Vector2.zero;
        float dir = SlideFromRight ? 1f : -1f;
        Vector2 startPos = endPos + new Vector2(SlideDistance * dir, 0);

        panelRect.anchoredPosition = startPos;
        slideTween = panelRect.DOAnchorPos(endPos, SlideDuration)
            .SetEase(SlideEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void SlideOutPanel(System.Action onComplete)
    {
        if (LogoCanvasGroup == null || panelRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        slideTween?.Kill();

        Vector2 current = panelRect.anchoredPosition;
        float dir = SlideFromRight ? 1f : -1f;
        Vector2 target = current + new Vector2(SlideDistance * dir, 0);

        slideTween = panelRect.DOAnchorPos(target, SlideDuration)
            .SetEase(SlideHideEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void PlayLogoBounceIn(System.Action onComplete)
    {
        if (logoTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        logoInSeq?.Kill();
        logoOutSeq?.Kill();

        logoTransform.localScale = Vector3.zero;
        float baseScale = FinalLogoScale;

        logoInSeq = DOTween.Sequence();
        logoInSeq.Append(logoTransform.DOScale(baseScale * LogoBounceInScale1, LogoBounceInTime1).SetEase(LogoBounceInEase1));
        logoInSeq.Append(logoTransform.DOScale(baseScale * LogoBounceInScale2, LogoBounceInTime2).SetEase(LogoBounceInEase2));
        logoInSeq.Append(logoTransform.DOScale(baseScale * LogoBounceInScale3, LogoBounceInTime3).SetEase(LogoBounceInEase3));
        logoInSeq.Append(logoTransform.DOScale(baseScale,                LogoBounceInSettleTime).SetEase(LogoBounceInSettleEase));
        logoInSeq.OnComplete(() => onComplete?.Invoke());
    }

    private void PlayLogoBounceOut(System.Action onComplete)
    {
        if (logoTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        logoInSeq?.Kill();
        logoOutSeq?.Kill();

        float baseScale = FinalLogoScale;

        logoOutSeq = DOTween.Sequence();
        logoOutSeq.Append(logoTransform.DOScale(baseScale * LogoBounceOutPeakScale, LogoBounceOutUpTime).SetEase(LogoBounceOutUpEase));
        logoOutSeq.Append(logoTransform.DOScale(0f, LogoBounceOutDownTime).SetEase(LogoBounceOutDownEase));
        logoOutSeq.OnComplete(() => onComplete?.Invoke());
    }

    private void KillAllTweens()
    {
        revealSeq?.Kill();
        logoInSeq?.Kill();
        logoOutSeq?.Kill();
        slideTween?.Kill();

        revealSeq = null;
        logoInSeq = null;
        logoOutSeq = null;
        slideTween = null;
    }

    // ---------- NEW Helpers for click-to-reveal ----------

    private void EnsureLogoCollider2D()
    {
        if (LogoSpriteRenderer == null) return;

        // Try any Collider2D on the same object
        var col = LogoSpriteRenderer.GetComponent<Collider2D>();
        if (col == null)
        {
            // Add a BoxCollider2D sized to the sprite’s bounds (good default)
            var box = LogoSpriteRenderer.gameObject.AddComponent<BoxCollider2D>();
            // Auto-size based on sprite bounds in local space
            if (LogoSpriteRenderer.sprite != null)
            {
                var b = LogoSpriteRenderer.sprite.bounds;
                box.offset = b.center;
                box.size = b.size;
            }
        }
    }

    private bool ClickedLogoSprite()
    {
        if (LogoSpriteRenderer == null || mainCam == null) return false;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        Vector2 world2D = new Vector2(worldPos.x, worldPos.y);

        // Raycast at mouse position; requires Collider2D on the logo
        RaycastHit2D hit = Physics2D.Raycast(world2D, Vector2.zero, 0f, LogoRaycastLayer);
        if (hit.collider == null) return false;

        return hit.collider.gameObject == LogoSpriteRenderer.gameObject;
    }
}
