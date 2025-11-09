using DG.Tweening;
using UnityEngine;

public class DoTweenScreenShake : MonoBehaviour
{
    // Assign these in the inspector
    public Transform CameraToShake;        // the Main Camera transform (or a tiny parent just above it)
    public RectTransform UIRig;            // full-screen RectTransform inside your Canvas

    public bool UseUnscaledTime = true;
    public int Vibrato = 32;
    public float Randomness = 90f;
    public bool FadeOut = true;

    public Vector3 WorldPosStrength = new Vector3(0.45f, 0.45f, 0f);
    public float WorldRotZ = 8f;           // degrees
    public Vector2 UIPosStrength = new Vector2(45f, 45f); // pixels
    public float UIRotZ = 8f;              // degrees

    Tween wPos, wRot, uPos, uRot;

    void KillAll()
    {
        if (wPos != null && wPos.IsActive()) wPos.Kill(true);
        if (wRot != null && wRot.IsActive()) wRot.Kill(true);
        if (uPos != null && uPos.IsActive()) uPos.Kill(true);
        if (uRot != null && uRot.IsActive()) uRot.Kill(true);
    }

    public void HitLight()  => Shake(0.18f, 0.65f);
    public void HitHeavy()  => Shake(0.28f, 1.00f);
    public void Explosion() => Shake(0.45f, 1.60f);

    public void Shake(float duration, float magnitude = 1f)
    {
        KillAll();

        // World: shake the camera
        if (CameraToShake != null)
        {
            Vector3 wp = WorldPosStrength * magnitude;
            float wr = WorldRotZ * magnitude;

            wPos = CameraToShake.DOShakePosition(duration, wp, Vibrato, Randomness, false, FadeOut)
                                .SetUpdate(UseUnscaledTime)
                                .SetTarget(CameraToShake);

            wRot = CameraToShake.DOShakeRotation(duration, new Vector3(0f, 0f, wr), Vibrato, Randomness, FadeOut)
                                .SetUpdate(UseUnscaledTime)
                                .SetTarget(CameraToShake);
        }

        // UI: shake the UIRig inside the Canvas
        if (UIRig != null)
        {
            Vector2 up = UIPosStrength * magnitude;
            float ur = UIRotZ * magnitude;

            uPos = UIRig.DOShakeAnchorPos(duration, up, Vibrato, Randomness, FadeOut)
                        .SetUpdate(UseUnscaledTime)
                        .SetTarget(UIRig);

            uRot = UIRig.transform.DOShakeRotation(duration, new Vector3(0f, 0f, ur), Vibrato, Randomness, FadeOut)
                                  .SetUpdate(UseUnscaledTime)
                                  .SetTarget(UIRig);
        }
    }
}
