using System;
using UnityEngine;

namespace CrowFX
{
    public enum CrowFXPresetUsage
    {
        General, Gameplay, Cinematic, PixelArt, Print, Analog, Display, Horror, Experimental
    }

    public enum CrowFXGpuTier { Light, Moderate, Heavy, Reference }

    [CreateAssetMenu(fileName = "CrowFXPreset", menuName = "CrowFX/Preset Asset")]
    public sealed class CrowFXPresetAsset : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;
        [SerializeField, HideInInspector] private int schemaVersion = CurrentSchemaVersion;
        public string displayName = "New CrowFX Preset";
        public CrowFXPresetUsage usage = CrowFXPresetUsage.General;
        [TextArea(2, 5)] public string description;
        public string[] tags = Array.Empty<string>();
        public Texture2D thumbnail;
        [Range(0f, 1f)] public float authoredStrength = 1f;
        public CrowFXGpuTier gpuTier = CrowFXGpuTier.Moderate;
        public CrowFXProfile profile;
        public Texture2D[] requiredTextures = Array.Empty<Texture2D>();

        public int SchemaVersion => schemaVersion;

        public bool CanApply(out string reason)
        {
            if (schemaVersion > CurrentSchemaVersion)
            {
                reason = $"Preset schema {schemaVersion} is newer than this CrowFX version supports ({CurrentSchemaVersion}).";
                return false;
            }

            if (profile == null)
            {
                reason = "Preset has no CrowFX Profile assigned.";
                return false;
            }

            for (int i = 0; requiredTextures != null && i < requiredTextures.Length; i++)
            {
                if (requiredTextures[i] == null)
                {
                    reason = "One or more required textures are missing.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public void ApplyTo(CrowImageEffects effect)
        {
            if (effect == null || profile == null) return;
            effect.ApplyProfile(profile);
            effect.masterBlend *= authoredStrength;
        }

        private void OnValidate()
        {
            if (schemaVersion <= 0) schemaVersion = CurrentSchemaVersion;
            authoredStrength = Mathf.Clamp01(authoredStrength);
        }
    }
}
