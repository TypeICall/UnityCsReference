// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace UnityEditor.Experimental.AssetImporters
{
    /// Universal structure that holds all the data relevant to importing an asset, including temporary data that needs to be shared across stages that make on any given importer's pipeline.
    ///
    /// Breaking up legacy importers into peaces and re-arranging them as pipelines that use those pieces so that the pieces can become building blocks that other importers can re-use implies that the pieces
    /// is not coupled with any given importer. For this decoupling and maximizing reuse, we need something that can hold the information describing what is being imported but also the data generated by the
    /// various parts that make up an importers pipeline. This container simply transports information from one "stage" to the other. Each stage is free to add/delete/alter the content of the container
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/AssetPipeline/AssetImportContext.h")]
    public class AssetImportContext
    {
        // The bindings generator is setting the instance pointer in this field
        internal IntPtr m_Self;

        // the context can only be instantiated in native code
        AssetImportContext() {}

        public extern string assetPath { get; internal set; }
        public extern string GetResultPath(string extension);

        public extern BuildTarget selectedBuildTarget { get; }

        extern void LogMessage(string msg, string file, int line, UnityEngine.Object obj, bool isAnError);

        [NativeThrows]
        public extern void SetMainObject(Object obj);
        public extern Object mainObject { get; }

        public void AddObjectToAsset(string identifier, Object obj)
        {
            AddObjectToAsset(identifier, obj, null);
        }

        [FreeFunction("AssetImportContextBindings::GetObjects", HasExplicitThis = true)]
        public extern void GetObjects([NotNull] List<Object> objects);

        [NativeThrows]
        public extern void AddObjectToAsset(string identifier, Object obj, Texture2D thumbnail);

        // Create a dependency against the contents of the source asset at the provided path
        // * if the asset at the path changes, it will trigger an import
        // * if the asset at the path moves, it will trigger an import
        public void DependsOnSourceAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "Cannot add dependency on invalid path.");
            }

            DependsOnSourceAssetInternal(path);
        }

        [NativeName("DependsOnSourceAsset")]
        private extern void DependsOnSourceAssetInternal(string path);

        public void DependsOnSourceAsset(GUID guid)
        {
            if (guid.Empty())
            {
                throw new ArgumentNullException("guid", "Cannot add dependency on empty GUID.");
            }

            DependsOnSourceAssetInternalGUID(guid);
        }

        [NativeName("DependsOnSourceAsset")]
        private extern void DependsOnSourceAssetInternalGUID(GUID guid);

        internal void DependsOnImportedAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "Cannot add dependency on invalid path.");
            }

            DependsOnImportedAssetInternal(path);
        }

        [NativeName("DependsOnImportedAsset")]
        private extern void DependsOnImportedAssetInternal(string path);

        public void DependsOnArtifact(GUID guid)
        {
            if (guid.Empty())
            {
                throw new ArgumentNullException("guid", "Cannot add dependency on empty GUID.");
            }

            DependsOnArtifactInternalGUID(guid);
        }

        [NativeName("DependsOnArtifact")]
        private extern void DependsOnArtifactInternalGUID(GUID guid);

        public void DependsOnArtifact(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "Cannot add dependency on invalid path.");
            }

            DependsOnArtifactInternalPath(path);
        }

        [NativeName("DependsOnArtifact")]
        private extern void DependsOnArtifactInternalPath(string path);

        public void DependsOnCustomDependency(string dependency)
        {
            if (string.IsNullOrEmpty(dependency))
            {
                throw new ArgumentNullException("dependency", "Cannot add custom dependency on an empty custom dependency.");
            }

            DependsOnCustomDependencyInternal(dependency);
        }

        [NativeName("DependsOnCustomDependency")]
        private extern void DependsOnCustomDependencyInternal(string path);

        public void LogImportError(string msg, UnityEngine.Object obj = null)
        {
            AddToLog(msg, true, obj);
        }

        internal void LogImportError(string msg, string file, int line, UnityEngine.Object obj = null)
        {
            AddToLog(msg, file, line, true, obj);
        }

        public void LogImportWarning(string msg, UnityEngine.Object obj = null)
        {
            AddToLog(msg, false, obj);
        }

        internal void LogImportWarning(string msg, string file, int line, UnityEngine.Object obj = null)
        {
            AddToLog(msg, file, line, false, obj);
        }

        void AddToLog(string msg, bool isAnError, UnityEngine.Object obj)
        {
            var st = new StackTrace(2, true);
            var sf = st.GetFrame(0);
            AddToLog(msg, sf.GetFileName(), sf.GetFileLineNumber(), isAnError, obj);
        }

        void AddToLog(string msg, string file, int line, bool isAnError, UnityEngine.Object obj)
        {
            LogMessage(msg, file, line, obj, isAnError);
        }
    }
}
