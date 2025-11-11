using UnityEngine;

public class FireworkRandomizer : MonoBehaviour
{
    [Header("Spread (how wide the burst travels)")]
    public float startSpeedMin = 5f;
    public float startSpeedMax = 12f;

    [Header("Optional: initial point radius of the burst")]
    public float radiusMin = 0.02f;
    public float radiusMax = 0.25f;

    [Header("Optional: uniform scale of entire effect")]
    public float scaleMin = 0.8f;
    public float scaleMax = 1.3f;

    [HideInInspector] public float chosenScale; // store chosen scale

    void OnEnable()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        main.startSpeed = Random.Range(startSpeedMin, startSpeedMax);

        var shape = ps.shape;
        shape.radius = Random.Range(radiusMin, radiusMax);

        chosenScale = Random.Range(scaleMin, scaleMax);
        transform.localScale = new Vector3(chosenScale, chosenScale, chosenScale);
    }
}