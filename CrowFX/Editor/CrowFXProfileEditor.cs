using UnityEditor;

namespace CrowFX.EditorTools
{
    [CustomEditor(typeof(CrowFXProfile))]
    public sealed class CrowFXProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool changed = false;

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DrawDefaultInspector();
                changed = changeCheck.changed;
            }

            serializedObject.ApplyModifiedProperties();

            var profile = (CrowFXProfile)target;
            if (changed && profile != null)
                CrowFXProfileSync.ApplyToLinkedEffects(profile, includeSource: true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "Auto Sync Profile updates every linked CrowFX component that has Auto Sync Profile enabled.",
                MessageType.Info);
        }
    }
}
