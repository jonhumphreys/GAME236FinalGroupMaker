// LogoRevealFX.cs
using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public static class LogoRevealFX
{
    // Default shader property names. Change if your shader uses different ones.
    public const string PixelProp = "_PixelateSize";
    public const string BrightProp = "_Brightness";

    // 1) Canvas helpers
    public static void HideImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.DOKill();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public static Sequence ShowCanvas(CanvasGroup cg, RectTransform slideTarget = null, float fade = 0.35f, float yOffset = -30f)
    {
        if (cg == null) return null;
        cg.DOKill();
        cg.interactable = true;
        cg.blocksRaycasts = true;

        var seq = DOTween.Sequence();
        seq.Join(cg.DOFade(1f, fade).SetEase(Ease.OutQuad));

        if (slideTarget != null)
        {
            var end = slideTarget.anchoredPosition;
            slideTarget.anchoredPosition = end + new Vector2(0, yOffset);
            seq.Join(slideTarget.DOAnchorPos(end, fade).SetEase(Ease.OutCubic));
        }

        return seq;
    }

    public static Sequence HideCanvas(CanvasGroup cg, RectTransform slideTarget = null, float fade = 0.25f, float yOffset = -20f, Action onComplete = null)
    {
        if (cg == null) return null;
        cg.DOKill();

        var seq = DOTween.Sequence();
        seq.Join(cg.DOFade(0f, fade).SetEase(Ease.InQuad));

        if (slideTarget != null)
        {
            seq.Join(slideTarget.DOAnchorPos(slideTarget.anchoredPosition + new Vector2(0, yOffset), fade).SetEase(Ease.InQuad));
        }

        seq.OnComplete(() =>
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            onComplete?.Invoke();
        });

        return seq;
    }

    // 2) Material tween helpers
    public static Tweener TweenMaterialFloat(Material mat, string prop, float from, float to, float dur, Ease ease)
    {
        float v = from;
        return DOTween.To(() => v, x => { v = x; if (mat != null) mat.SetFloat(prop, x); }, to, dur).SetEase(ease);
    }

    public static Tweener TweenMaterialInt(Material mat, string prop, int from, int to, float dur, Ease ease)
    {
        float f = from;
        return DOTween.To(() => f, x => { f = x; if (mat != null) mat.SetInt(prop, Mathf.RoundToInt(x)); }, to, dur).SetEase(ease);
    }

    // 3) Build the full logo reveal sequence
    // Smooth brighten, stepped pixelation from sizes list, logo scale pop and rotate, then callback
    public static Sequence BuildLogoRevealSequence(
        SpriteRenderer logoRenderer,
        Transform logoTransform,
        IList<int> pixelSizes,
        float stepDelay,
        float brightenTime = 0.25f,
        float popScale = 1.15f,
        float popUp = 0.20f,
        float popDown = 0.15f,
        float rotate = 10f,
        Action onComplete = null)
    {
        if (logoRenderer == null || logoRenderer.material == null) return null;
        var mat = logoRenderer.material;

        int startPix = pixelSizes != null && pixelSizes.Count > 0 ? pixelSizes[0] : 64;
        int endPix   = pixelSizes != null && pixelSizes.Count > 0 ? pixelSizes[pixelSizes.Count - 1] : 512;

        mat.SetInt(PixelProp, startPix);
        mat.SetFloat(BrightProp, -1f);

        var seq = DOTween.Sequence();

        // Brighten to 0
        seq.Append(TweenMaterialFloat(mat, BrightProp, -1f, 0f, brightenTime, Ease.OutQuad));

        // Pixelation through steps (fixed)
        if (pixelSizes != null && pixelSizes.Count > 1)
        {
            float stepTime = Mathf.Max(0.01f, stepDelay);
            int from = startPix; // carry forward

            for (int i = 1; i < pixelSizes.Count; i++)
            {
                int to = pixelSizes[i];

                int startLocal = from;   // capture per-iteration
                int endLocal   = to;

                seq.Append(DOTween.To(
                    () => (float)startLocal,
                    v => {
                        // do not read the material for "from" during build
                        int iv = Mathf.RoundToInt(v);
                        startLocal = iv;                     // keep local in sync
                        if (mat != null) mat.SetInt(PixelProp, iv);
                    },
                    endLocal,
                    stepTime
                ).SetEase(Ease.OutCubic));

                from = to; // advance for the next segment
            }
        }
        else
        {
            // Fallback smooth tween from start to end
            seq.Append(TweenMaterialInt(mat, PixelProp, startPix, endPix, Mathf.Max(0.2f, stepDelay), Ease.OutCubic));
        }


        // Pop and rotate flair
        if (logoTransform != null)
        {
            // Pop
            var startScale = logoTransform.localScale;
            seq.Join(logoTransform.DOScale(startScale * popScale, popUp).SetEase(Ease.OutBack));
            seq.Append(logoTransform.DOScale(startScale, popDown).SetEase(Ease.InQuad));

            // Tiny rotate and return
            seq.Join(logoTransform.DORotate(new Vector3(0, 0, rotate), popUp * 0.9f).SetEase(Ease.OutSine));
            seq.Append(logoTransform.DORotate(Vector3.zero, popDown * 0.9f).SetEase(Ease.InSine));
        }

        seq.OnComplete(() => onComplete?.Invoke());
        return seq;
    }

    // 4) Team name text reveal
    public static Sequence TeamNameReveal(TextMeshProUGUI tmp, string text, float typeTime = 0.6f, bool scrambleUpper = false)
    {
        if (tmp == null) return null;

        var rt = tmp.rectTransform;
        var startScale = rt.localScale;

        tmp.DOKill();
        rt.DOKill();

        var seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            tmp.text = "";
            tmp.DOText(text, typeTime, scrambleMode: scrambleUpper ? ScrambleMode.Uppercase : ScrambleMode.None);
        });
        seq.Join(rt.DOScale(startScale * 1.15f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(startScale, 0.15f).SetEase(Ease.InQuad));
        seq.Join(tmp.DOColor(Color.yellow, 0.15f).SetLoops(2, LoopType.Yoyo));

        return seq;
    }

    // 5) Pulse another text block for emphasis (e.g., members line)
    public static Sequence PulseText(TextMeshProUGUI tmp, Color flash, float upScale = 1.08f, float upTime = 0.12f, float downTime = 0.18f)
    {
        if (tmp == null) return null;

        var rt = tmp.rectTransform;
        var startScale = rt.localScale;

        tmp.DOKill();
        rt.DOKill();

        var seq = DOTween.Sequence();
        seq.Join(rt.DOScale(startScale * upScale, upTime).SetEase(Ease.OutQuad));
        seq.Append(rt.DOScale(startScale, downTime).SetEase(Ease.OutQuad));
        seq.Join(tmp.DOColor(flash, 0.15f).SetLoops(2, LoopType.Yoyo));
        return seq;
    }
}
