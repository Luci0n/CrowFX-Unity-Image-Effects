using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrowFX.EditorTools
{
    [InitializeOnLoad]
    internal static class CrowFXProfileSync
    {
        private static bool _isSynchronizing;

        static CrowFXProfileSync()
        {
            Undo.undoRedoPerformed += RefreshAllAutoLinkedEffects;
        }

        public static void PushFromEffect(CrowImageEffects source, CrowFXProfile profile)
        {
            if (source == null || profile == null || _isSynchronizing)
                return;

            try
            {
                _isSynchronizing = true;

                Undo.RecordObject(profile, "Sync CrowFX Profile");
                source.SaveToProfile(profile);
                EditorUtility.SetDirty(profile);

                ApplyProfileToEffects(profile, CollectLinkedEffects(profile, source, includeSource: false));
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        public static void ApplyToEffect(CrowFXProfile profile, CrowImageEffects effect, bool recordUndo = true)
        {
            if (profile == null || effect == null || _isSynchronizing)
                return;

            try
            {
                _isSynchronizing = true;

                if (recordUndo)
                    Undo.RecordObject(effect, "Apply CrowFX Profile");

                effect.ApplyProfile(profile);
                MarkEffectDirty(effect);
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        public static void ApplyToLinkedEffects(CrowFXProfile profile, CrowImageEffects source = null, bool includeSource = false)
        {
            if (profile == null || _isSynchronizing)
                return;

            try
            {
                _isSynchronizing = true;
                ApplyProfileToEffects(profile, CollectLinkedEffects(profile, source, includeSource));
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        public static void RefreshAllAutoLinkedEffects()
        {
            if (_isSynchronizing)
                return;

            try
            {
                _isSynchronizing = true;

                var effects = Resources.FindObjectsOfTypeAll<CrowImageEffects>();
                for (int i = 0; i < effects.Length; i++)
                {
                    var effect = effects[i];
                    if (!ShouldIncludeEffect(effect))
                        continue;

                    effect.ApplyProfile(effect.profile);
                    MarkEffectDirty(effect);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        public static void MarkEffectDirty(CrowImageEffects effect)
        {
            if (effect == null)
                return;

            EditorUtility.SetDirty(effect);

            var gameObject = effect.gameObject;
            if (gameObject == null)
                return;

            var scene = gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void ApplyProfileToEffects(CrowFXProfile profile, List<CrowImageEffects> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                effect.ApplyProfile(profile);
                MarkEffectDirty(effect);
            }
        }

        private static List<CrowImageEffects> CollectLinkedEffects(CrowFXProfile profile, CrowImageEffects source, bool includeSource)
        {
            var linked = new List<CrowImageEffects>();
            var effects = Resources.FindObjectsOfTypeAll<CrowImageEffects>();

            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                if (!ShouldIncludeEffect(effect))
                    continue;
                if (effect.profile != profile)
                    continue;
                if (!includeSource && effect == source)
                    continue;

                linked.Add(effect);
            }

            return linked;
        }

        private static bool ShouldIncludeEffect(CrowImageEffects effect)
        {
            if (effect == null || effect.profile == null || !effect.autoApplyProfile)
                return false;
            if (EditorUtility.IsPersistent(effect))
                return false;
            if ((effect.hideFlags & HideFlags.HideAndDontSave) != 0)
                return false;

            return true;
        }
    }
}
