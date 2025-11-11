using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[RequireComponent(typeof(SpriteRenderer))]
public class CycleSpritesOnCtrlE : MonoBehaviour
{
    [Tooltip("Assign 5 sprites in order. The script starts on index 0.")]
    public Sprite[] Sprites = new Sprite[5];

    private SpriteRenderer spriteRenderer;
    private int currentIndex = 0;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (Sprites == null || Sprites.Length < 5)
        {
            Debug.LogError("CycleSpritesOnCtrlE: Please assign exactly 5 sprites in the inspector.");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            if (Sprites[i] == null)
            {
                Debug.LogError("CycleSpritesOnCtrlE: One or more sprite slots are empty.");
                break;
            }
        }

        // Start on the first sprite
        spriteRenderer.sprite = Sprites[0];
        currentIndex = 0;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool ctrlHeld = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        bool ePressedThisFrame = keyboard.eKey.wasPressedThisFrame;

        if (ctrlHeld && ePressedThisFrame)
        {
            AdvanceSprite();
        }
    }

    private void AdvanceSprite()
    {
        if (Sprites == null || Sprites.Length < 5) return;

        currentIndex = (currentIndex + 1) % 5;
        var nextSprite = Sprites[currentIndex];
        if (nextSprite != null)
        {
            spriteRenderer.sprite = nextSprite;
        }
        else
        {
            Debug.LogWarning("CycleSpritesOnCtrlE: Next sprite slot is empty.");
        }
    }
}