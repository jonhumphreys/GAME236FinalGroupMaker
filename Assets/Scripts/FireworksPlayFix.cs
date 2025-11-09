using UnityEngine;
using System.Collections;

public class FireworkPlayFix : MonoBehaviour
{
    private ParticleSystem parentPs;

    void OnEnable()
    {
        parentPs = GetComponent<ParticleSystem>();
        if (parentPs == null) return;

        // Force desired flags
        var mainParent = parentPs.main;
        mainParent.playOnAwake = true;

        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == parentPs) continue;
            var m = ps.main;
            m.playOnAwake = false; // child flash should not auto-play
        }

        StartCoroutine(PlayChildrenNextFrame());
    }

    IEnumerator PlayChildrenNextFrame()
    {
        yield return null; // next frame, avoids OnEnable race
        if (parentPs != null && !parentPs.isPlaying) parentPs.Play(true);

        // Manually pop the flash
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == parentPs) continue;
            ps.Clear(true);
            ps.Play(true);
        }
    }
}