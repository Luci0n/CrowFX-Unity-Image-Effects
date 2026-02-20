#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrowFX.Helpers;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace CrowFX.EditorTools
{
    /// <summary>
    /// Builds the section model for CrowImageEffectsEditor:
    /// - Maps serialized field names -> section keys via [EffectSection]
    /// - Reads section meta via [EffectSectionMeta]
    /// - Produces sorted SectionDefs and a per-section property list
    /// - Provides helper to bind sectionKey -> custom drawer
    /// </summary>
    internal static class CrowFxSectionsModel
    {
        internal sealed class SectionDef
        {
            public string Key;
            public string Title;
            public string Icon;
            public string Hint;
            public int Order;

            /// <summary>Fold is owned by the editor (because it persists via EditorPrefs).</summary>
            public AnimBool Fold;

            /// <summary>If null => editor should auto-draw remaining properties for that section.</summary>
            public Action Draw;

            public SectionDef(string key)
            {
                Key = key;
                Title = key;
                Icon = "d_Settings";
                Hint = "";
                Order = 0;
            }
        }

        internal readonly struct BuildResult
        {
            public readonly Dictionary<string, List<string>> PropsBySection;
            public readonly List<SectionDef> Sections;

            public BuildResult(Dictionary<string, List<string>> propsBySection, List<SectionDef> sections)
            {
                PropsBySection = propsBySection;
                Sections = sections;
            }
        }

        // ---------------------------------------------------------------------------------------------
        // Public entry
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Rebuilds:
        /// - propsBySection (fieldName lists per section)
        /// - sections (sorted, with meta applied, fold created, and custom drawer bound)
        /// </summary>
        internal static BuildResult Build(
            SerializedObject serializedObject,
            HashSet<string> favoriteSections,
            Func<string, bool, AnimBool> getOrCreateSectionFold,
            Func<string, Action> resolveCustomDrawerOrNull,
            Type effectsType = null
        )
        {
            if (serializedObject == null) throw new ArgumentNullException(nameof(serializedObject));
            if (favoriteSections == null) favoriteSections = new HashSet<string>(StringComparer.Ordinal);
            if (getOrCreateSectionFold == null) throw new ArgumentNullException(nameof(getOrCreateSectionFold));
            if (resolveCustomDrawerOrNull == null) throw new ArgumentNullException(nameof(resolveCustomDrawerOrNull));

            effectsType ??= typeof(CrowImageEffects);

            var propsBySection = BuildPropertyMapFromAttributes(serializedObject, effectsType);
            var sections = BuildSectionDefs(
                propsBySection,
                ReadSectionMeta(effectsType),
                favoriteSections,
                getOrCreateSectionFold,
                resolveCustomDrawerOrNull
            );

            return new BuildResult(propsBySection, sections);
        }

        // ---------------------------------------------------------------------------------------------
        // Property discovery: fields -> [EffectSection] -> serialized property name lists
        // ---------------------------------------------------------------------------------------------

        private static Dictionary<string, List<string>> BuildPropertyMapFromAttributes(SerializedObject so, Type t)
        {
            var propsBySection = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = t.GetFields(flags);

            // Preserve "declaration order" stability for ties
            var declIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < fields.Length; i++)
                declIndex[fields[i].Name] = i;

            var discovered = new List<(string section, int order, int decl, string name)>(fields.Length);

            foreach (var f in fields)
            {
                if (f.IsStatic) continue;
                if (f.IsNotSerialized) continue;
                if (Attribute.IsDefined(f, typeof(HideInInspector))) continue;

                bool serializable = f.IsPublic || Attribute.IsDefined(f, typeof(SerializeField));
                if (!serializable) continue;

                var attr = f.GetCustomAttribute<EffectSectionAttribute>(inherit: true);
                if (attr == null) continue;

                var prop = so.FindProperty(f.Name);
                if (prop == null) continue;

                discovered.Add((attr.Section ?? "Misc", attr.Order, declIndex[f.Name], f.Name));
            }

            foreach (var g in discovered
                         .OrderBy(x => x.section, StringComparer.Ordinal)
                         .ThenBy(x => x.order)
                         .ThenBy(x => x.decl))
            {
                if (!propsBySection.TryGetValue(g.section, out var list))
                {
                    list = new List<string>(32);
                    propsBySection[g.section] = list;
                }
                list.Add(g.name);
            }

            return propsBySection;
        }

        // ---------------------------------------------------------------------------------------------
        // Meta discovery: [EffectSectionMeta] on CrowImageEffects
        // ---------------------------------------------------------------------------------------------

        private static Dictionary<string, EffectSectionMetaAttribute> ReadSectionMeta(Type t)
        {
            var meta = new Dictionary<string, EffectSectionMetaAttribute>(StringComparer.Ordinal);
            var metas = t.GetCustomAttributes<EffectSectionMetaAttribute>(inherit: true);

            foreach (var m in metas)
                meta[m.Key] = m;

            return meta;
        }

        // ---------------------------------------------------------------------------------------------
        // Section defs: apply meta + fold + custom drawers + sort (favorites first)
        // ---------------------------------------------------------------------------------------------

        private static List<SectionDef> BuildSectionDefs(
            Dictionary<string, List<string>> propsBySection,
            Dictionary<string, EffectSectionMetaAttribute> meta,
            HashSet<string> favorites,
            Func<string, bool, AnimBool> getOrCreateSectionFold,
            Func<string, Action> resolveCustomDrawerOrNull
        )
        {
            var sections = new List<SectionDef>(propsBySection.Count);

            // distinct keys
            var keys = propsBySection.Keys.Distinct(StringComparer.Ordinal).ToList();

            foreach (var key in keys)
            {
                var def = new SectionDef(key);

                if (meta.TryGetValue(key, out var m))
                {
                    def.Title = m.Title;
                    def.Icon = m.Icon;
                    def.Hint = m.Hint;
                    def.Order = m.Order;
                    def.Fold = getOrCreateSectionFold(key, m.DefaultExpanded);
                }
                else
                {
                    def.Title = key;
                    def.Icon = "d_Settings";
                    def.Hint = "";
                    def.Order = 500;
                    def.Fold = getOrCreateSectionFold(key, false);
                }

                def.Draw = resolveCustomDrawerOrNull(key);
                sections.Add(def);
            }

            sections.Sort((a, b) =>
            {
                bool aFav = favorites.Contains(a.Key);
                bool bFav = favorites.Contains(b.Key);

                if (aFav != bFav) return aFav ? -1 : 1;

                int c = a.Order.CompareTo(b.Order);
                if (c != 0) return c;

                return string.CompareOrdinal(a.Key, b.Key);
            });

            return sections;
        }
    }
}
#endif