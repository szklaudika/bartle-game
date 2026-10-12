using UnityEngine;

// Procedurálne vygenerovaný zvuk motorovej píly (dvojtaktný motor + šum),
// použije sa, ak drevorubač nemá priradený vlastný AudioClip.
public static class ChainsawSound
{
    private static AudioClip _clip;

    public static AudioClip Clip
    {
        get
        {
            if (_clip == null) _clip = Generate();
            return _clip;
        }
    }

    private static AudioClip Generate()
    {
        const int sampleRate = 44100;
        const int length = sampleRate;      // 1 s, slučka
        const float engineHz = 95f;         // celé číslo cyklov za sekundu -> plynulá slučka
        var rng = new System.Random(1234);
        var samples = new float[length];

        float noise = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float phase = (t * engineHz) % 1f;

            // "puf" každého zapálenia valca
            float pop = Mathf.Exp(-phase * 7f) * 2f - 0.55f;
            float buzz = Mathf.Sin(2f * Mathf.PI * engineHz * 2f * t) * 0.35f
                       + Mathf.Sin(2f * Mathf.PI * engineHz * 5f * t) * 0.15f;

            noise = Mathf.Lerp(noise, (float)rng.NextDouble() * 2f - 1f, 0.35f);

            float s = pop * 0.45f + buzz + noise * 0.5f;
            samples[i] = (float)System.Math.Tanh(s * 1.4f) * 0.45f;
        }

        AudioClip clip = AudioClip.Create("ChainsawLoop", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
