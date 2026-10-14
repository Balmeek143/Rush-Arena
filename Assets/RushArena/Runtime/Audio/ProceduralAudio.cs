using UnityEngine;

namespace RushArena.Runtime.Audio
{
    // Procedural sound effects generated dynamically at runtime.
    // Zero external audio asset dependencies; 100% cross-platform (Windows, WebGL, Android).
    public static class ProceduralAudio
    {
        private static AudioClip rifleClip;
        private static AudioClip plasmaClip;
        private static AudioClip hitMarkerClip;
        private static AudioClip jumpPadClip;
        private static AudioClip powerupClip;
        private static AudioClip damageClip;
        private static AudioSource globalSource;

        private static AudioSource GetGlobalSource()
        {
            if (globalSource != null) return globalSource;
            var obj = new GameObject("RushGlobalAudioSource");
            Object.DontDestroyOnLoad(obj);
            globalSource = obj.AddComponent<AudioSource>();
            globalSource.playOnAwake = false;
            globalSource.spatialBlend = 0f; // 2D HUD sound
            return globalSource;
        }

        public static void PlayRifle(AudioSource source = null)
        {
            if (rifleClip == null) rifleClip = CreateRifleClip();
            Play(source != null ? source : GetGlobalSource(), rifleClip, 0.75f, Random.Range(0.95f, 1.05f));
        }

        public static void PlayPlasma(AudioSource source = null)
        {
            if (plasmaClip == null) plasmaClip = CreatePlasmaClip();
            Play(source != null ? source : GetGlobalSource(), plasmaClip, 0.85f, Random.Range(0.92f, 1.08f));
        }

        public static void PlayHitMarker()
        {
            if (hitMarkerClip == null) hitMarkerClip = CreateHitMarkerClip();
            Play(GetGlobalSource(), hitMarkerClip, 0.9f, 1f);
        }

        public static void PlayJumpPad(AudioSource source = null)
        {
            if (jumpPadClip == null) jumpPadClip = CreateJumpPadClip();
            Play(source != null ? source : GetGlobalSource(), jumpPadClip, 0.95f, 1f);
        }

        public static void PlayPowerup()
        {
            if (powerupClip == null) powerupClip = CreatePowerupClip();
            Play(GetGlobalSource(), powerupClip, 0.9f, 1f);
        }

        public static void PlayDamage()
        {
            if (damageClip == null) damageClip = CreateDamageClip();
            Play(GetGlobalSource(), damageClip, 0.8f, Random.Range(0.95f, 1.05f));
        }

        private static void Play(AudioSource source, AudioClip clip, float volume, float pitch)
        {
            if (source == null || clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateRifleClip()
        {
            int sampleRate = 44100;
            float duration = 0.16f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 32f);
                float noise = (Random.value * 2f - 1f) * 0.7f;
                float body = Mathf.Sin(2f * Mathf.PI * (160f - t * 400f) * t) * 0.3f;
                data[i] = Mathf.Clamp((noise + body) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("RifleShot", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePlasmaClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 14f);
                float freq = Mathf.Lerp(800f, 90f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;
                float buzz = Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f;
                data[i] = Mathf.Clamp((tone + buzz) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("PlasmaBlast", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateHitMarkerClip()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 60f);
                float tone = Mathf.Sin(2f * Mathf.PI * 2100f * t) * 0.7f;
                float harm = Mathf.Sin(2f * Mathf.PI * 3300f * t) * 0.3f;
                data[i] = Mathf.Clamp((tone + harm) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("HitMarker", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateJumpPadClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin((t / duration) * Mathf.PI);
                float freq = Mathf.Lerp(110f, 480f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;
                float noise = (Random.value * 2f - 1f) * 0.3f;
                data[i] = Mathf.Clamp((wave + noise) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("JumpPadLaunch", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePowerupClip()
        {
            int sampleRate = 44100;
            float duration = 0.32f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 8f);
                float freq = t < 0.12f ? 523.25f : (t < 0.22f ? 659.25f : 783.99f); // C5 -> E5 -> G5
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f;
                data[i] = Mathf.Clamp(tone * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("PowerupChime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDamageClip()
        {
            int sampleRate = 44100;
            float duration = 0.14f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 28f);
                float thud = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.7f;
                float crunch = (Random.value * 2f - 1f) * 0.3f;
                data[i] = Mathf.Clamp((thud + crunch) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("DamageHurt", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
