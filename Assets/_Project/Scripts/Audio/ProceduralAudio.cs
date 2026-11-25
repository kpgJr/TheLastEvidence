using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Génère les sons du prototype par synthèse (aucune ressource externe ni musique protégée).
    /// Les clips peuvent être remplacés par de vrais enregistrements dans l'AudioManager.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 22050;

        private delegate float SampleFunc(float t, int i, System.Random rng);

        private static AudioClip Make(string name, float duration, SampleFunc func, int seed = 1)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var data = new float[count];
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(func((float)i / SampleRate, i, rng), -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);
        private static float Sine(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        public static AudioClip Create(SfxType type)
        {
            switch (type)
            {
                case SfxType.Click:
                    return Make("click", 0.03f, (t, i, r) => Sine(1800, t) * Mathf.Exp(-t * 200) * 0.5f);
                case SfxType.KeypadBeep:
                    return Make("beep", 0.09f, (t, i, r) => Sine(1250, t) * 0.35f * (t < 0.08f ? 1 : 0));
                case SfxType.Success:
                    return Make("success", 0.6f, (t, i, r) =>
                    {
                        float f = t < 0.15f ? 660 : t < 0.3f ? 880 : 1320;
                        return Sine(f, t) * 0.35f * Mathf.Exp(-(t % 0.15f) * 6f);
                    });
                case SfxType.Error:
                    return Make("error", 0.35f, (t, i, r) => Mathf.Sign(Sine(110, t)) * 0.18f * (1 - t / 0.35f));
                case SfxType.Locked:
                    return Make("locked", 0.25f, (t, i, r) =>
                        (Noise(r) * 0.3f + Sine(180, t) * 0.4f) * Mathf.Exp(-t * 25f) +
                        (t > 0.1f ? (Noise(r) * 0.3f + Sine(160, t) * 0.4f) * Mathf.Exp(-(t - 0.1f) * 25f) : 0));
                case SfxType.Pickup:
                    return Make("pickup", 0.35f, (t, i, r) =>
                    {
                        float a = Mathf.Exp(-(t % 0.08f) * 60f);
                        return (Sine(2400 + 300 * Mathf.Floor(t / 0.08f), t) * 0.25f + Noise(r) * 0.08f) * a;
                    }, 3);
                case SfxType.Evidence:
                    return Make("evidence", 1.2f, (t, i, r) =>
                        (Sine(523, t) + Sine(784, t) * 0.6f + Sine(1046, t) * 0.3f) * 0.2f * Mathf.Exp(-t * 3f));
                case SfxType.DoorOpen:
                case SfxType.DoorClose:
                {
                    bool open = type == SfxType.DoorOpen;
                    float prevLp = 0;
                    return Make(open ? "door_open" : "door_close", open ? 0.9f : 0.45f, (t, i, r) =>
                    {
                        float creak = open ? Mathf.Sign(Sine(90 + 40 * Mathf.Sin(t * 9f) + t * 60, t)) * 0.08f * Mathf.Sin(Mathf.PI * t / 0.9f) : 0;
                        prevLp += (Noise(r) - prevLp) * 0.08f;
                        float thud = !open ? prevLp * 2.5f * Mathf.Exp(-t * 14f) + Sine(70, t) * 0.5f * Mathf.Exp(-t * 18f) : prevLp * 0.6f;
                        return creak + thud;
                    }, 5);
                }
                case SfxType.Drawer:
                {
                    float lp = 0;
                    return Make("drawer", 0.45f, (t, i, r) =>
                    {
                        lp += (Noise(r) - lp) * 0.15f;
                        return lp * 1.4f * Mathf.Sin(Mathf.PI * t / 0.45f) + (t > 0.38f ? Sine(120, t) * 0.4f * Mathf.Exp(-(t - 0.38f) * 40) : 0);
                    }, 7);
                }
                case SfxType.Switch:
                    return Make("switch", 0.06f, (t, i, r) => (Noise(r) * 0.5f + Sine(900, t) * 0.3f) * Mathf.Exp(-t * 120f));
                case SfxType.Footstep:
                {
                    float lp = 0;
                    return Make("footstep", 0.16f, (t, i, r) =>
                    {
                        lp += (Noise(r) - lp) * 0.12f;
                        return (lp * 2.2f + Sine(85, t) * 0.4f) * Mathf.Exp(-t * 30f);
                    }, 11);
                }
                case SfxType.Paper:
                {
                    float prev = 0;
                    return Make("paper", 0.3f, (t, i, r) =>
                    {
                        float n = Noise(r);
                        float hp = n - prev; prev = n;
                        return hp * 0.25f * Mathf.Sin(Mathf.PI * t / 0.3f) * (0.6f + 0.4f * Mathf.Sin(t * 70));
                    }, 13);
                }
                case SfxType.Computer:
                    return Make("computer", 0.4f, (t, i, r) => Sine(t < 0.2f ? 980 : 1470, t) * 0.25f * (t % 0.2f < 0.15f ? 1 : 0));
                case SfxType.Electric:
                    return Make("electric", 0.8f, (t, i, r) =>
                        (Mathf.Sign(Sine(50, t)) * 0.15f + Noise(r) * 0.25f * (r.NextDouble() < 0.02 ? 1 : 0.15f)) * Mathf.Exp(-t * 2.5f), 17);
                default:
                    return Make("silence", 0.05f, (t, i, r) => 0);
            }
        }

        public static AudioClip CreateAmbience(AmbienceType type)
        {
            switch (type)
            {
                case AmbienceType.Rain:
                {
                    float lp = 0;
                    return Make("rain", 6f, (t, i, r) =>
                    {
                        lp += (Noise(r) - lp) * 0.3f;
                        float drop = r.NextDouble() < 0.0015 ? Noise(r) * 0.6f : 0f;
                        return lp * 0.35f + drop;
                    }, 21);
                }
                case AmbienceType.Wind:
                {
                    float brown = 0;
                    return Make("wind", 8f, (t, i, r) =>
                    {
                        brown = Mathf.Clamp(brown + Noise(r) * 0.02f, -1, 1) * 0.998f;
                        // Modulation périodique pour boucler sans rupture audible.
                        return brown * 0.6f * (0.55f + 0.45f * Mathf.Sin(2 * Mathf.PI * t / 8f));
                    }, 23);
                }
                case AmbienceType.Clock:
                    return Make("clock", 2f, (t, i, r) =>
                    {
                        float local = t % 1f;
                        float f = t < 1f ? 2600 : 2100;
                        return (Sine(f, t) * 0.5f + Noise(r) * 0.3f) * Mathf.Exp(-local * 180f);
                    }, 29);
                case AmbienceType.Hum:
                    return Make("hum", 1f, (t, i, r) => (Sine(50, t) * 0.5f + Sine(100, t) * 0.3f + Sine(150, t) * 0.12f) * 0.3f);
                case AmbienceType.Music:
                    return Make("music", 16f, (t, i, r) =>
                    {
                        // Nappe sombre : accord de ré mineur lentement modulé.
                        float lfo = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * t / 16f);
                        float chord = Sine(73.4f, t) * 0.5f + Sine(110f, t) * 0.3f * lfo + Sine(146.8f, t) * 0.2f
                                      + Sine(174.6f, t) * 0.15f * (1 - lfo);
                        return chord * 0.22f;
                    });
                default:
                    return Make("silence", 1f, (t, i, r) => 0);
            }
        }
    }
}
