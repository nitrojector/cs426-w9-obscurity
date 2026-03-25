using UnityEngine;

public static class ProceduralAudio
{
    public static AudioClip CreateWallBumpClip()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 0.08f;
        int n = Mathf.Max(64, Mathf.RoundToInt(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = (1f - t) * (1f - t);
            data[i] = (UnityEngine.Random.value * 2f - 1f) * 0.45f * env;
        }

        var clip = AudioClip.Create("WallBump", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip CreateGoalHumClip()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 0.25f;
        int n = Mathf.Max(128, Mathf.RoundToInt(rate * dur));
        var data = new float[n];
        const float freq = 440f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Sin(Mathf.PI * i / (n - 1));
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.12f * env;
        }

        var clip = AudioClip.Create("GoalHum", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Short ascending sting when a level is cleared (not the final level).</summary>
    public static AudioClip CreateLevelVictoryClip()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 0.42f;
        int n = Mathf.Max(256, Mathf.RoundToInt(rate * dur));
        var data = new float[n];
        float[] freqs = { 523.25f, 659.25f, 783.99f, 987.77f };
        int step = n / freqs.Length;
        for (int p = 0; p < freqs.Length; p++)
        {
            int start = p * step;
            int len = (p == freqs.Length - 1) ? (n - start) : step;
            for (int i = 0; i < len; i++)
            {
                int idx = start + i;
                if (idx >= n) break;
                float t = idx / (float)rate;
                float env = Mathf.Sin(Mathf.PI * (i + 1) / (len + 1));
                data[idx] += Mathf.Sin(2f * Mathf.PI * freqs[p] * t) * 0.22f * env;
            }
        }

        var clip = AudioClip.Create("LevelVictory", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Loud multi-voice fanfare when every level is completed.</summary>
    public static AudioClip CreateFinalVictoryClip()
    {
        int rate = AudioSettings.outputSampleRate;
        float dur = 1.65f;
        int n = Mathf.Max(512, Mathf.RoundToInt(rate * dur));
        var data = new float[n];
        // Root + fifth + octave + high sparkle
        float f0 = 261.63f, f1 = 392f, f2 = 523.25f, f3 = 783.99f, f4 = 1046.5f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Sin(Mathf.PI * i / (n - 1));
            // Swell in middle
            env *= Mathf.SmoothStep(0f, 1f, i / (float)(n * 0.15f)) *
                   Mathf.SmoothStep(0f, 1f, (n - 1 - i) / (float)(n * 0.25f));

            float s = 0f;
            s += Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.38f;
            s += Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.32f;
            s += Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.35f;
            s += Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.28f;
            // Brassy buzz
            s += Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f2 * t * 1.5f)) * 0.12f * env;
            // Final rise last 0.35s
            float tail = Mathf.Clamp01((t - (dur - 0.35f)) / 0.35f);
            s += Mathf.Sin(2f * Mathf.PI * f4 * t) * 0.45f * tail * tail;

            data[i] = Mathf.Clamp(s * env, -1f, 1f);
        }

        var clip = AudioClip.Create("FinalVictory", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
