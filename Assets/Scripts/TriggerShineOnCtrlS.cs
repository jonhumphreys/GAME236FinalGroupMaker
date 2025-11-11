using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[RequireComponent(typeof(AllIn1_ShineSweep))]
public class TriggerShineOnCtrlS : MonoBehaviour
{
    [Header("Shine Settings")]
    [Min(1)]
    public int sweepCount = 1;

    [Tooltip("Speed multiplier. Higher = faster.")]
    [Range(0.1f, 5f)]
    public float speedMultiplier = 1f;

    private AllIn1_ShineSweep shineSweep;
    private bool running;

    private void Awake()
    {
        shineSweep = GetComponent<AllIn1_ShineSweep>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if ((kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) &&
            kb.sKey.wasPressedThisFrame &&
            !running)
        {
            StartCoroutine(RunMultipleSweeps());
        }
    }

    private System.Collections.IEnumerator RunMultipleSweeps()
    {
        running = true;

        float originalDuration = shineSweep.duration;
        float adjustedDuration = Mathf.Max(0.05f, originalDuration / Mathf.Max(0.01f, speedMultiplier));

        for (int i = 0; i < sweepCount; i++)
        {
            bool reverse = (i % 2 == 1);

            // apply speed for this pass
            shineSweep.duration = adjustedDuration;

            shineSweep.TriggerShine(reverse);

            // simple wait until this pass should be done
            yield return new WaitForSeconds(adjustedDuration + 0.05f);
        }

        // restore
        shineSweep.duration = originalDuration;

        running = false;
    }
}