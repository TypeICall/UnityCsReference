// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PackageManagerPrefs
    {
        static IPackageManagerPrefs s_Instance = null;
        public static IPackageManagerPrefs instance { get { return s_Instance ?? PackageManagerPrefsInternal.instance; } }

        [Serializable]
        private class PackageManagerPrefsInternal : ScriptableSingleton<PackageManagerPrefsInternal>, IPackageManagerPrefs
        {
            private const string k_SkipRemoveConfirmationPrefs = "PackageManager.SkipRemoveConfirmation";
            private const string k_SkipDisableConfirmationPrefs = "PackageManager.SkipDisableConfirmation";
            private const string k_LastUsedFilterPrefsPrefix = "PackageManager.Filter_";

            private static string projectIdentifier
            {
                get
                {
                    // PlayerSettings.productGUID is already used as LocalProjectID by Analytics, so we use it too
                    return PlayerSettings.productGUID.ToString();
                }
            }
            private static string lastUsedFilterForProjectPerfs { get { return k_LastUsedFilterPrefsPrefix + projectIdentifier; } }

            [SerializeField]
            private bool m_DismissPreviewPackagesInUse;
            public bool dismissPreviewPackagesInUse
            {
                get => m_DismissPreviewPackagesInUse;
                set => m_DismissPreviewPackagesInUse = value;
            }

            public bool skipRemoveConfirmation
            {
                get { return EditorPrefs.GetBool(k_SkipRemoveConfirmationPrefs, false); }
                set { EditorPrefs.SetBool(k_SkipRemoveConfirmationPrefs, value); }
            }

            public bool skipDisableConfirmation
            {
                get { return EditorPrefs.GetBool(k_SkipDisableConfirmationPrefs, false); }
                set { EditorPrefs.SetBool(k_SkipDisableConfirmationPrefs, value); }
            }

            public PackageFilterTab? lastUsedPackageFilter
            {
                get
                {
                    try
                    {
                        return (PackageFilterTab)Enum.Parse(typeof(PackageFilterTab), EditorPrefs.GetString(lastUsedFilterForProjectPerfs, PackageFiltering.instance.defaultFilterTab.ToString()));
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                set
                {
                    EditorPrefs.SetString(lastUsedFilterForProjectPerfs, value?.ToString());
                }
            }

            [SerializeField]
            private bool m_DependenciesExpanded = false;
            public bool dependenciesExpanded
            {
                get => m_DependenciesExpanded;
                set => m_DependenciesExpanded = value;
            }

            [SerializeField]
            private bool m_SamplesExpanded = false;
            public bool samplesExpanded
            {
                get => m_SamplesExpanded;
                set => m_SamplesExpanded = value;
            }
        }
    }
}
