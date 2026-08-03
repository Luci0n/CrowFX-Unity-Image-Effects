using NUnit.Framework;
using UnityEngine;

namespace CrowFX.Tests
{
    public sealed class CrowFXPipelineTests
    {
        private GameObject _cameraObject;
        private CrowImageEffects _effect;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("CrowFX Test Camera", typeof(Camera));
            _effect = _cameraObject.AddComponent<CrowImageEffects>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_cameraObject != null) Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void NeutralDefaultsDoNotScheduleCreativePasses()
        {
            Assert.That(_effect.GetActivePassCount(), Is.EqualTo(2));
            Assert.That(_effect.GetEstimatedHistoryBytes(1920, 1080), Is.EqualTo(0));
        }

        [Test]
        public void HistoryMemoryHonorsConfiguredResolutionScales()
        {
            _effect.ghostEnabled = true;
            _effect.ghostBlend = 0.25f;
            _effect.ghostFrames = 4;
            _effect.ghostResolutionScale = 0.5f;

            long expectedPixels = 960L * 540L;
            long expectedBytes = expectedPixels * 8L * 5L;
            Assert.That(_effect.GetEstimatedHistoryBytes(1920, 1080), Is.EqualTo(expectedBytes));
        }

        [Test]
        public void ProfileRoundTripIncludesProfessionalAndMaskSettings()
        {
            var profile = ScriptableObject.CreateInstance<CrowFXProfile>();
            try
            {
                _effect.maskSoftness = 0.37f;
                _effect.maskPlacement = CrowImageEffects.MaskPlacement.BeforeSignalAndDisplays;
                _effect.qualityTier = CrowImageEffects.QualityTier.Reference;
                _effect.depthFar = 42f;
                _effect.filmEnabled = true;
                _effect.filmGrain = 0.19f;
                _effect.ditherSize = 6f;
                _effect.halftoneAreaModulation = true;
                _effect.halftoneColorMode = CrowImageEffects.HalftoneColorMode.CmykPrint;
                _effect.motionHistoryFps = 8f;
                _effect.compositeEnabled = true;
                _effect.compositeStandard = CrowImageEffects.VhsStandard.PAL;
                _effect.sharpenMode = CrowImageEffects.SharpenMode.UnsharpMask;
                _effect.SaveToProfile(profile);

                _effect.maskSoftness = 0f;
                _effect.maskPlacement = CrowImageEffects.MaskPlacement.EntireStack;
                _effect.qualityTier = CrowImageEffects.QualityTier.Low;
                _effect.depthFar = 1f;
                _effect.filmEnabled = false;
                _effect.ditherSize = 1f;
                _effect.halftoneAreaModulation = false;
                _effect.halftoneColorMode = CrowImageEffects.HalftoneColorMode.Luminance;
                _effect.motionHistoryFps = 60f;
                _effect.compositeEnabled = false;
                _effect.sharpenMode = CrowImageEffects.SharpenMode.ContrastAdaptive;
                _effect.ApplyProfile(profile);

                Assert.That(_effect.maskSoftness, Is.EqualTo(0.37f).Within(0.0001f));
                Assert.That(_effect.maskPlacement, Is.EqualTo(CrowImageEffects.MaskPlacement.BeforeSignalAndDisplays));
                Assert.That(_effect.qualityTier, Is.EqualTo(CrowImageEffects.QualityTier.Reference));
                Assert.That(_effect.depthFar, Is.EqualTo(42f).Within(0.0001f));
                Assert.That(_effect.filmEnabled, Is.True);
                Assert.That(_effect.filmGrain, Is.EqualTo(0.19f).Within(0.0001f));
                Assert.That(_effect.ditherSize, Is.EqualTo(6f).Within(0.0001f));
                Assert.That(_effect.halftoneAreaModulation, Is.True);
                Assert.That(_effect.halftoneColorMode, Is.EqualTo(CrowImageEffects.HalftoneColorMode.CmykPrint));
                Assert.That(_effect.motionHistoryFps, Is.EqualTo(8f).Within(0.0001f));
                Assert.That(_effect.compositeEnabled, Is.True);
                Assert.That(_effect.compositeStandard, Is.EqualTo(CrowImageEffects.VhsStandard.PAL));
                Assert.That(_effect.sharpenMode, Is.EqualTo(CrowImageEffects.SharpenMode.UnsharpMask));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
