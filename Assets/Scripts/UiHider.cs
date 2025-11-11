using UnityEngine;
using UnityEngine.InputSystem;

public class UiHider : MonoBehaviour
{
    public CanvasGroup UiCanvasGroup;
    
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (UiCanvasGroup.alpha == 0f)
                UiCanvasGroup.alpha = 1f;
            else
                UiCanvasGroup.alpha = 0f;
        }
    }
}
