using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(AudioSource))]
public class SoundToggler : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        
        if ((keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
            keyboard.mKey.wasPressedThisFrame)
        {
            ToggleSound();
        }
    }

    private void ToggleSound()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        else
        {
            audioSource.Play();
        }
    }
}
