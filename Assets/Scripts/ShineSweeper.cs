using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AllIn1_ShineSweep : MonoBehaviour
{
    [Header("Shine Motion")]
    public float duration = 1.2f;
    public float overshoot = 0.25f;
    public float shineAngleDeg = 45f;
    public float shineWidth = 0.25f;
    public float shineOpacity = 1.0f;

    [Header("Auto Trigger")]
    public bool triggerOnStart = false;

    private SpriteRenderer spriteRenderer;
    private Material materialRef;
    private MaterialPropertyBlock mpb;

    // Resolved property names
    private string propProgress;
    private string propAngle;
    private string propWidth;
    private string propOpacity;
    private string propEnable;

    private Coroutine running;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        materialRef = spriteRenderer.sharedMaterial;

        if (materialRef != null)
        {
            propProgress = FirstExisting(new[]
            {
                "_ShineLocation","_ShinePos","_ShineProgress","_ShineX","_ShineSlider"
            });
            propAngle = FirstExisting(new[]
            {
                "_ShineRotation","_ShineAngle"
            });
            propWidth = FirstExisting(new[]
            {
                "_ShineWidth","_ShineSize","_ShineBandWidth"
            });
            propOpacity = FirstExisting(new[]
            {
                "_ShineOpacity","_ShineIntensity","_ShineAlpha"
            });
            propEnable = FirstExisting(new[]
            {
                "_EnableShine","_ShineEnable","_ShineOn","_ShineToggle"
            });
        }

        EnsureDisabled();

        if (!string.IsNullOrEmpty(propProgress) && materialRef.HasProperty(propProgress))
        {
            mpb.SetFloat(propProgress, -Mathf.Abs(overshoot));
            spriteRenderer.SetPropertyBlock(mpb);
        }
    }

    private void Start()
    {
        if (triggerOnStart)
            TriggerShine();
    }

    // existing no-arg trigger stays forward
    public void TriggerShine()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShineRoutine(false));
    }

    // new: allow caller to choose direction
    public void TriggerShine(bool reverse)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShineRoutine(reverse));
    }

    private IEnumerator ShineRoutine(bool reverse)
    {
        if (materialRef == null)
            yield break;

        EnableShine();

        spriteRenderer.GetPropertyBlock(mpb);
        if (!string.IsNullOrEmpty(propAngle) && materialRef.HasProperty(propAngle))
            mpb.SetFloat(propAngle, shineAngleDeg);
        if (!string.IsNullOrEmpty(propWidth) && materialRef.HasProperty(propWidth))
            mpb.SetFloat(propWidth, shineWidth);
        if (!string.IsNullOrEmpty(propOpacity) && materialRef.HasProperty(propOpacity))
            mpb.SetFloat(propOpacity, shineOpacity);
        spriteRenderer.SetPropertyBlock(mpb);

        if (string.IsNullOrEmpty(propProgress) || !materialRef.HasProperty(propProgress))
        {
            yield return new WaitForSeconds(duration);
            EnsureDisabled();
            running = null;
            yield break;
        }

        float forwardStart = -Mathf.Abs(overshoot);
        float forwardEnd   =  1f + Mathf.Abs(overshoot);

        float start = reverse ? forwardEnd : forwardStart;
        float end   = reverse ? forwardStart : forwardEnd;

        // snap to starting side so it begins off-sprite
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(propProgress, start);
        spriteRenderer.SetPropertyBlock(mpb);

        float t = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (t < d)
        {
            float u = t / d;
            float eased = u * u * (3f - 2f * u); // smoothstep
            float value = Mathf.Lerp(start, end, eased);

            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(propProgress, value);
            spriteRenderer.SetPropertyBlock(mpb);

            t += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(propProgress, end);
        spriteRenderer.SetPropertyBlock(mpb);

        EnsureDisabled();
        running = null;
    }

    private void EnableShine()
    {
        spriteRenderer.GetPropertyBlock(mpb);

        if (!string.IsNullOrEmpty(propEnable) && materialRef.HasProperty(propEnable))
        {
            mpb.SetFloat(propEnable, 1f);
        }
        else if (!string.IsNullOrEmpty(propOpacity) && materialRef.HasProperty(propOpacity))
        {
            mpb.SetFloat(propOpacity, shineOpacity);
        }

        spriteRenderer.SetPropertyBlock(mpb);
    }

    private void EnsureDisabled()
    {
        spriteRenderer.GetPropertyBlock(mpb);

        if (!string.IsNullOrEmpty(propEnable) && materialRef != null && materialRef.HasProperty(propEnable))
        {
            mpb.SetFloat(propEnable, 0f);
        }
        else if (!string.IsNullOrEmpty(propOpacity) && materialRef != null && materialRef.HasProperty(propOpacity))
        {
            mpb.SetFloat(propOpacity, 0f);
        }

        spriteRenderer.SetPropertyBlock(mpb);
    }

    private string FirstExisting(string[] candidates)
    {
        if (materialRef == null || materialRef.shader == null) return null;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (materialRef.HasProperty(candidates[i]))
                return candidates[i];
        }
        return null;
    }
}
