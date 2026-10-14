using UnityEngine;

namespace RushArena.Runtime.Audio
{
    // Procedural sound effects generated dynamically at runtime.
    // Zero external audio asset dependencies; 100% cross-platform (Windows, WebGL, Android).
    public static class ProceduralAudio
    {
        private static AudioClip pistolClip;
        private static AudioClip rifleClip;
        private static AudioClip plasmaClip;
        private static AudioClip scatterClip;
        private static AudioClip reloadClip;
        private static AudioClip weaponPickupClip;
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

        public static void PlayPistol(AudioSource source = null)
        {
            if (pistolClip == null) pistolClip = CreatePistolClip();
            Play(source != null ? source : GetGlobalSource(), pistolClip, 0.8f, Random.Range(0.96f, 1.04f));
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

        private static AudioClip sniperClip;
        private static AudioClip sniperZoomClip;
        private static AudioClip weaponDepletedClip;
        private static AudioClip machineGunClip;
        private static AudioClip powerupDismissClip;

        public static void PlayScattergun(AudioSource source = null)
        {
            if (scatterClip == null) scatterClip = CreateScattergunClip();
            Play(source != null ? source : GetGlobalSource(), scatterClip, 0.95f, Random.Range(0.90f, 1.05f));
        }

        public static void PlayMachineGun(AudioSource source = null)
        {
            if (machineGunClip == null) machineGunClip = CreateMachineGunClip();
            Play(source != null ? source : GetGlobalSource(), machineGunClip, 0.85f, Random.Range(0.94f, 1.06f));
        }

        public static void PlayPowerupDismiss(AudioSource source = null)
        {
            if (powerupDismissClip == null) powerupDismissClip = CreatePowerupDismissClip();
            Play(source != null ? source : GetGlobalSource(), powerupDismissClip, 0.6f, 1f);
        }

        public static void PlaySniper(AudioSource source = null)
        {
            if (sniperClip == null) sniperClip = CreateSniperClip();
            Play(source != null ? source : GetGlobalSource(), sniperClip, 1.0f, Random.Range(0.97f, 1.03f));
        }

        public static void PlaySniperZoom(AudioSource source = null)
        {
            if (sniperZoomClip == null) sniperZoomClip = CreateSniperZoomClip();
            Play(source != null ? source : GetGlobalSource(), sniperZoomClip, 0.7f, 1f);
        }

        public static void PlayWeaponDepleted(AudioSource source = null)
        {
            if (weaponDepletedClip == null) weaponDepletedClip = CreateWeaponDepletedClip();
            Play(source != null ? source : GetGlobalSource(), weaponDepletedClip, 0.85f, 1f);
        }

        public static void PlayReload(AudioSource source = null)
        {
            if (reloadClip == null) reloadClip = CreateReloadClip();
            Play(source != null ? source : GetGlobalSource(), reloadClip, 0.85f, 1f);
        }

        public static void PlayWeaponPickup()
        {
            if (weaponPickupClip == null) weaponPickupClip = CreateWeaponPickupClip();
            Play(GetGlobalSource(), weaponPickupClip, 0.95f, 1f);
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

        private static AudioClip CreatePistolClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 45f);
                float crack = (Random.value * 2f - 1f) * 0.8f;
                float pop = Mathf.Sin(2f * Mathf.PI * (280f - t * 800f) * t) * 0.4f;
                data[i] = Mathf.Clamp((crack + pop) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("PistolShot", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateScattergunClip()
        {
            int sampleRate = 44100;
            float duration = 0.32f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 18f);
                float boom = Mathf.Sin(2f * Mathf.PI * (120f - t * 250f) * t) * 0.6f;
                float blast = (Random.value * 2f - 1f) * 0.8f;
                data[i] = Mathf.Clamp((boom + blast) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("ScattergunShot", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateReloadClip()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // Double mechanical click
                float click1 = Mathf.Exp(-Mathf.Abs(t - 0.08f) * 120f) * Mathf.Sin(2f * Mathf.PI * 1800f * t);
                float click2 = Mathf.Exp(-Mathf.Abs(t - 0.32f) * 100f) * Mathf.Sin(2f * Mathf.PI * 1400f * t);
                float sliding = (t > 0.08f && t < 0.32f) ? (Random.value * 2f - 1f) * 0.15f : 0f;
                data[i] = Mathf.Clamp((click1 + click2 + sliding) * 0.7f, -1f, 1f);
            }
            var clip = AudioClip.Create("WeaponReload", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWeaponPickupClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 9f);
                float freq = t < 0.10f ? 440f : (t < 0.18f ? 659.25f : 880f); // A4 -> E5 -> A5
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f;
                data[i] = Mathf.Clamp(tone * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("WeaponPickup", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSniperClip()
        {
            int sampleRate = 44100;
            float duration = 0.40f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 16f);
                float crack = (Random.value * 2f - 1f) * 0.9f;
                float thud = Mathf.Sin(2f * Mathf.PI * (140f - t * 200f) * t) * 0.7f;
                data[i] = Mathf.Clamp((crack + thud) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("SniperShot", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSniperZoomClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin((t / duration) * Mathf.PI);
                float freq = Mathf.Lerp(600f, 1200f, t / duration);
                data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f, -1f, 1f);
            }
            var clip = AudioClip.Create("SniperZoom", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateWeaponDepletedClip()
        {
            int sampleRate = 44100;
            float duration = 0.22f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 22f);
                float freq = Mathf.Lerp(400f, 120f, t / duration);
                data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.7f, -1f, 1f);
            }
            var clip = AudioClip.Create("WeaponDepleted", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMachineGunClip()
        {
            int sampleRate = 44100;
            float duration = 0.11f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 36f);
                float crack = (Random.value * 2f - 1f) * 0.75f;
                float thud = Mathf.Sin(2f * Mathf.PI * (180f - t * 450f) * t) * 0.5f;
                float metal = Mathf.Sin(2f * Mathf.PI * 820f * t) * 0.2f;
                data[i] = Mathf.Clamp((crack + thud + metal) * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("MachineGunShot", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePowerupDismissClip()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 10f);
                float freq = Mathf.Lerp(784f, 392f, t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
                data[i] = Mathf.Clamp(tone * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("PowerupDismiss", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
