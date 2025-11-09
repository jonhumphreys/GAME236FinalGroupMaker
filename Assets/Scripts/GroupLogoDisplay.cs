using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

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

    private void Awake()
    {
        panelRect = LogoCanvasGroup != null ? LogoCanvasGroup.GetComponent<RectTransform>() : null;
        logoTransform = LogoImageObject != null ? LogoImageObject.transform : null;

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

        // Ensure we have the sprite assigned, but keep the object inactive until after we set pixels
        AssignLogoSprite();

        // Prepare material state BEFORE the logo becomes visible
        bool alreadyRevealed = HasBeenRevealed(groupNumber);
        if (alreadyRevealed)
        {
            // Fully clear/bright for previously revealed teams
            SetPixelateSizeSafe(512);
            SetBrightnessSafe(0f);
        }
        else
        {
            // Start heavily pixelated and dark for new reveals
            int startPix = PixelateSizes.Count > 0 ? PixelateSizes[0] : 64;
            SetPixelateSizeSafe(startPix);
            SetBrightnessSafe(0f);
        }

        // Now show the image (no clear frame flash) and reset scale
        ShowLogoImage();
        if (logoTransform != null) logoTransform.localScale = Vector3.zero;

        SlideInPanel(() =>
        {
            PlayLogoBounceIn(() =>
            {
                if (alreadyRevealed)
                {
                    // Skip reveal; maybe add a tiny wiggle + team name
                    DisplayTeamNameFancy();
                }
                else
                {
                    StartNewRevealAnimation();
                }
            });
        });
    }


    public void HideLogoScreen()
    {
        if (revealSeq != null) { revealSeq.Kill(); revealSeq = null; }

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
            LogoImageObject.SetActive(true);
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

        // After pixelation finishes, wiggle the logo, then show team name
        revealSeq = LogoRevealFX.BuildLogoRevealSequence(
            logoRenderer: LogoSpriteRenderer,
            logoTransform: null,                // keep pop disabled; we do wiggle ourselves
            pixelSizes: PixelateSizes,
            stepDelay: RevealStepDelay,
            brightenTime: 0f,
            onComplete: () => PlayLogoWiggle(DisplayTeamNameFancy)
        );
    }


    private void DisplayTeamNameFancy()
    {
        // Default fallback name
        string teamName = "Unknown Team";

        if (LogoSpriteRenderer != null && LogoSpriteRenderer.sprite != null)
        {
            // Use the sprite’s asset name as the team name
            teamName = LogoSpriteRenderer.sprite.name;
        }

        if (TeamNameText != null)
            LogoRevealFX.TeamNameReveal(TeamNameText, teamName, typeTime: 0.6f, scrambleUpper: false);

        MarkLogoAsRevealed();
        NotifyRevealManager();
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

        // start from zero to be deterministic
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
}
