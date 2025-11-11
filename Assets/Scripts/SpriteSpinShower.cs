using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSpinShower : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationDuration = 1.0f;
    public int spinRevolutions = 3;

    private bool isVisible = false;
    private Transform spriteTransform;
    private Sequence currentSequence;

    private void Awake()
    {
        spriteTransform = transform;
        spriteTransform.localScale = Vector3.zero;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if ((keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
            keyboard.aKey.wasPressedThisFrame)
        {
            ToggleSprite();
        }
    }

    private void ToggleSprite()
    {
        if (currentSequence != null && currentSequence.IsActive())
            currentSequence.Kill();

        currentSequence = DOTween.Sequence();

        if (!isVisible)
        {
            currentSequence.Append(spriteTransform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack));
            currentSequence.Join(spriteTransform.DORotate(
                new Vector3(0, 0, 360f * spinRevolutions),
                animationDuration,
                RotateMode.FastBeyond360
            ).SetEase(Ease.OutCubic));
        }
        else
        {
            currentSequence.Append(spriteTransform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));
            currentSequence.Join(spriteTransform.DORotate(
                new Vector3(0, 0, -360f * spinRevolutions),
                animationDuration,
                RotateMode.FastBeyond360
            ).SetEase(Ease.InCubic));
        }

        isVisible = !isVisible;
    }
}
