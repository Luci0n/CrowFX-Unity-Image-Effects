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

        [Test]
        public void NewComponentsDefaultToSignalDomainDisplaySimulation()
        {
            // Migration must not fire on a freshly constructed component, or every new
            // setup would silently inherit the pre-2.1 linear-domain CRT and LCD response.
            Assert.That(_effect.displaySignalDomain, Is.True);
        }

        [Test]
        public void DisplaySignalDomainRoundTripsThroughProfile()
        {
            var profile = ScriptableObject.CreateInstance<CrowFXProfile>();
            try
            {
                _effect.displaySignalDomain = false;
                _effect.SaveToProfile(profile);

                _effect.displaySignalDomain = true;
                _effect.ApplyProfile(profile);

                Assert.That(_effect.displaySignalDomain, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void BoxSamplingCostScalesWithBlockSizeAndStaysBounded()
        {
            // Without a virtual grid a cell spans exactly pixelSize source texels, so the
            // estimate is independent of the current screen resolution.
            _effect.pixelSize = 8;
            _effect.samplingFilter = CrowImageEffects.SamplingFilter.Point;
            int pointCost = _effect.GetEstimatedSamplesPerPixel();

            _effect.samplingFilter = CrowImageEffects.SamplingFilter.Box;
            int boxCost = _effect.GetEstimatedSamplesPerPixel();

            // Shader takes one tap per two covered texels per axis: ceil(8/2)^2 = 16.
            Assert.That(boxCost - pointCost, Is.EqualTo(16 - 1),
                "Box integrates the cell, so it must report a higher per-pixel cost than point sampling.");

            // The shader caps its footprint at 8x8 taps; the estimate must agree or the
            // inspector's cost readout drifts from what actually executes.
            _effect.pixelSize = 1024;
            Assert.That(_effect.GetEstimatedSamplesPerPixel() - pointCost, Is.EqualTo(64 - 1));
        }

        [Test]
        public void BoxSamplingIsFreeWhenNoGridIsActive()
        {
            // Without pixelation or a virtual grid the sampling stage is skipped entirely,
            // so selecting Box must not schedule a pass or add cost.
            _effect.samplingFilter = CrowImageEffects.SamplingFilter.Box;
            Assert.That(_effect.GetActivePassCount(), Is.EqualTo(2));
        }

        /// <summary>Solo mutes every other section by writing neutral values into that section's
        /// gating properties. A section whose gates are missing from that mapping keeps rendering
        /// while everything else is silenced, which is the one thing Solo must never do. This walks
        /// the effect sections and asserts each one can actually be switched off.</summary>
        [Test]
        public void EveryEffectSectionCanBeNeutralized()
        {
            // Turn the whole stack on so nothing is trivially inactive.
            _effect.pixelSize = 4;
            _effect.pregradeEnabled = true;
            _effect.levels = 8;
            _effect.usePalette = true;
            _effect.useMask = true;
            _effect.useDepthMask = true;
            _effect.jitterEnabled = true; _effect.jitterStrength = 0.5f;
            _effect.bleedBlend = 0.5f; _effect.bleedIntensity = 1f;
            _effect.ghostEnabled = true; _effect.ghostBlend = 0.3f;
            _effect.edgeEnabled = true; _effect.edgeBlend = 1f; _effect.edgeStrength = 1f;
            _effect.unsharpEnabled = true; _effect.unsharpAmount = 0.5f;
            _effect.ditherMode = CrowImageEffects.DitherMode.Ordered4x4; _effect.ditherStrength = 0.5f;
            _effect.lensSensorEnabled = true; _effect.lensSensorIntensity = 1f;
            _effect.filmEnabled = true; _effect.filmIntensity = 1f;
            _effect.motionGlitchEnabled = true; _effect.motionGlitchIntensity = 0.6f;
            _effect.digitalVideoEnabled = true; _effect.digitalVideoIntensity = 0.7f;
            _effect.compositeEnabled = true; _effect.compositeIntensity = 0.7f;
            _effect.vhsEnabled = true; _effect.vhsIntensity = 0.8f;
            _effect.crtEnabled = true;
            _effect.lcdEnabled = true; _effect.lcdIntensity = 0.8f;

            int busyPasses = _effect.GetActivePassCount();
            Assert.That(busyPasses, Is.GreaterThan(2), "Test setup failed to activate the stack.");

            // Neutralizing every section must collapse the stack back to staging + present.
            _effect.pixelSize = 1; _effect.useVirtualGrid = false;
            _effect.pregradeEnabled = false;
            _effect.levels = 512; _effect.usePerChannel = false;
            _effect.levelsR = 512; _effect.levelsG = 512; _effect.levelsB = 512;
            _effect.animateLevels = false; _effect.luminanceOnly = false; _effect.invert = false;
            _effect.usePalette = false;
            _effect.thresholdCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            _effect.useMask = false;
            _effect.useDepthMask = false;
            _effect.jitterEnabled = false; _effect.jitterStrength = 0f;
            _effect.bleedBlend = 0f; _effect.bleedIntensity = 0f;
            _effect.ghostEnabled = false; _effect.ghostBlend = 0f;
            _effect.edgeEnabled = false; _effect.edgeBlend = 0f;
            _effect.unsharpEnabled = false; _effect.unsharpAmount = 0f;
            _effect.ditherMode = CrowImageEffects.DitherMode.None; _effect.ditherStrength = 0f;
            _effect.lensSensorEnabled = false; _effect.lensSensorIntensity = 0f;
            _effect.filmEnabled = false; _effect.filmIntensity = 0f;
            _effect.motionGlitchEnabled = false; _effect.motionGlitchIntensity = 0f;
            _effect.digitalVideoEnabled = false; _effect.digitalVideoIntensity = 0f;
            _effect.compositeEnabled = false; _effect.compositeIntensity = 0f;
            _effect.vhsEnabled = false; _effect.vhsIntensity = 0f;
            _effect.crtEnabled = false;
            _effect.lcdEnabled = false; _effect.lcdIntensity = 0f;

            Assert.That(_effect.GetActivePassCount(), Is.EqualTo(2),
                "A stage still schedules a pass after every section was neutralized, so Solo " +
                "cannot fully isolate. Check that its gating properties are covered by " +
                "CaptureNeutralPreviewOverrides.");
        }

        /// <summary>The Look Library recipe is derived from a profile rather than hand-written, so
        /// it must agree with what the stack actually schedules. A neutral profile has to report
        /// nothing, and a stage switched on has to appear.</summary>
        [Test]
        public void ProfileStageSummaryReflectsWhatIsActuallyEnabled()
        {
            var profile = ScriptableObject.CreateInstance<CrowFXProfile>();
            try
            {
                profile.CaptureFrom(_effect);
                Assert.That(profile.GetActiveStageSummary(), Is.EqualTo("SOURCE"),
                    "A default profile schedules no creative passes, so it must list no stages.");

                _effect.crtEnabled = true;
                _effect.vhsEnabled = true;
                _effect.vhsIntensity = 0.8f;
                _effect.levels = 16;
                _effect.filmEnabled = true;
                _effect.filmIntensity = 1f;
                profile.CaptureFrom(_effect);

                var stages = profile.GetActiveStageLabels();
                Assert.That(stages, Does.Contain("FILM"));
                Assert.That(stages, Does.Contain("POSTERIZE"));
                Assert.That(stages, Does.Contain("VHS"));
                Assert.That(stages, Does.Contain("CRT"));

                // Render order: quantization runs before transport, transport before display.
                Assert.That(stages.IndexOf("FILM"), Is.LessThan(stages.IndexOf("POSTERIZE")));
                Assert.That(stages.IndexOf("POSTERIZE"), Is.LessThan(stages.IndexOf("VHS")));
                Assert.That(stages.IndexOf("VHS"), Is.LessThan(stages.IndexOf("CRT")));

                // A stage whose enable flag is off must not be listed even when its other
                // values look active, which is how the old hand-written recipes drifted.
                _effect.edgeEnabled = false;
                _effect.edgeBlend = 1f;
                _effect.edgeStrength = 1f;
                profile.CaptureFrom(_effect);
                Assert.That(profile.GetActiveStageLabels(), Does.Not.Contain("EDGES"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void EmptyThresholdCurveIsNotTreatedAsAnAuthoredRemap()
        {
            var profile = ScriptableObject.CreateInstance<CrowFXProfile>();
            try
            {
                // An AnimationCurve with no keys evaluates to zero everywhere. Reading that as
                // an authored remap would schedule the palette pass with an all-black lookup.
                _effect.thresholdCurve = new AnimationCurve();
                profile.CaptureFrom(_effect);

                Assert.That(profile.GetActiveStageLabels(), Does.Not.Contain("TONE"));
                Assert.That(_effect.GetActivePassCount(), Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void BuiltInPipelineReportsFullSceneBufferSupport()
        {
            // The test project runs the Built-in Render Pipeline, where Edge Outline
            // normals and datamosh motion vectors are both available.
            Assert.That(CrowImageEffects.GetSceneBufferSupport(),
                Is.EqualTo(CrowImageEffects.SceneBufferSupport.BuiltIn));
        }

        [Test]
        public void InspectorSupportsEditingSeveralComponentsAtOnce()
        {
            // Located by reflection rather than a direct reference, so this assembly does not
            // have to depend on CrowFX.Editor just to assert one attribute.
            var editorType = System.Type.GetType(
                "CrowFX.EditorTools.CrowImageEffectsEditor, CrowFX.EditorTools", throwOnError: false);

            Assert.That(editorType, Is.Not.Null,
                "CrowImageEffectsEditor was not found. If it moved, update this test rather than deleting it.");

            // Without this attribute Unity refuses to draw the inspector for a multi-selection at
            // all, replacing it with "Multi-object editing not supported".
            // Unity names this attribute class without the conventional "Attribute" suffix.
            Assert.That(editorType.IsDefined(typeof(UnityEditor.CanEditMultipleObjects), inherit: false),
                Is.True,
                "CrowImageEffectsEditor must keep [CanEditMultipleObjects]: several actions now " +
                "loop over every selected component and would silently affect only the first without it.");
        }
    }
}
