using UnityEngine;

public class Lightflicker : MonoBehaviour
{
    public Light lightSource;

    [Header("Base Flicker")]
    public float intensityBase = 1f;
    public float intensityAmplitude = 0.6f;
    public float noiseSpeed = 2f;

    [Header("Spikes")]
    [Tooltip("How strong the spike is added on top of noise.")]
    public float spikeStrength = 2.5f;

    [Tooltip("Chance per second (0–1). Example: 0.1 = spike every ~10 sec")]
    public float spikeChance = 0.1f;

    private float currentSpike = 0f;
    private float spikeDecay = 0f;

    void Start()
    {
        if (lightSource == null)
            lightSource = GetComponent<Light>();
    }

    void Update()
    {
        // Base smooth flicker
        float noise = Mathf.PerlinNoise(Time.time * noiseSpeed, 0f);
        float flicker = intensityBase + noise * intensityAmplitude;

        // Trigger a spike randomly
        if (Random.value < spikeChance * Time.deltaTime)
        {
            currentSpike = spikeStrength;
            spikeDecay = 1f; // spike lasts briefly and fades out
        }

        // Decay the spike smoothly
        if (spikeDecay > 0f)
        {
            spikeDecay -= Time.deltaTime * 4f; // spike fades quickly
            flicker += currentSpike * spikeDecay;
        }

        lightSource.intensity = flicker;
    }
}