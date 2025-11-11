using UnityEngine;
using UnityEngine.InputSystem;

public class Quitter : MonoBehaviour
{
    
    void Update()
    {
        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.qKey.isPressed)
            Application.Quit();
    }
}
