using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAssetExporter
{

    [ExecuteInEditMode]
    [CustomEditor(typeof(AssetBundleUnity3D))]
    public class AssetBundleUnity3DEditor : Editor
    {

        private static string[] compressions = new string[]
        {
            "LZMA (default, best size, but slow load)", // 0
            "LZ4 (recommended, small and still fast)", // 1
            "None (largest size and fastest to load)", // 2
        };

        // Collect assets from folders (optional recursive)
        private void CollectAssets(UnityEngine.Object root,
            ref List<UnityEngine.Object> exports,
            bool recursive = false)
        {
            var path = AssetDatabase.GetAssetPath(root);
            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (var guid in AssetDatabase.FindAssets("", new[] { path }))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath
                        <UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
                    string fname = AssetDatabase.GetAssetPath(asset);
                    if (recursive) CollectAssets(asset, ref exports, recursive);
                    else if (!AssetDatabase.IsValidFolder(fname)) exports.Add(asset);
                }
            }
            // Just add as is to export
            else exports.Add(root);
        }

        // Helpers to determine if a graphics API is available according to build targets selected
        private static bool SupportsOpenGL(AssetBundleUnity3D script) => script.StandaloneWindows | script.StandaloneLinux | script.StandaloneMacOSX;
        private static bool SupportsVulkan(AssetBundleUnity3D script) => script.StandaloneWindows | script.StandaloneLinux;
        private static bool SupportsD3D11(AssetBundleUnity3D script) => script.StandaloneWindows;
        private static bool SupportsMetal(AssetBundleUnity3D script) => script.StandaloneMacOSX;

        // Helpers to determine if a build target should be actually built
        private static bool BuildOpenGL(BuildTarget target, AssetBundleUnity3D script) => script.WantOpenGL;
        private static bool BuildVulkan(BuildTarget target, AssetBundleUnity3D script) => script.WantVulkan && target != BuildTarget.StandaloneOSX;
        private static bool BuildD3D11(BuildTarget target, AssetBundleUnity3D script) => script.WantD3D11 && target == BuildTarget.StandaloneWindows64;
        private static bool BuildMetal(BuildTarget target, AssetBundleUnity3D script) => script.WantMetal && target == BuildTarget.StandaloneOSX;

        public bool ToggleUI(ref bool value, string label, string tooltip = null)
        {
            bool changed = GUILayout.Toggle(value,
                new GUIContent(label, tooltip),
                GUILayout.Height(20));
            // Do nothing if it didn't change
            if (changed == value) return value;
            // Record the undo action for this value
            Undo.RecordObject((AssetBundleUnity3D)target,
                changed ? "Enabled " : "Disabled " + label);
            // Update the reference
            value = changed;
            // Make sure changes are persisted (play safe)
            EditorUtility.SetDirty((AssetBundleUnity3D)target);
            // Allow chaining
            return value;
        }

        public bool FoldoutUI(ref bool value, string label)
        {
            bool changed = EditorGUILayout.Foldout(
                value, label, true);
            // Do nothing if it didn't change
            if (changed == value) return value;
            // Record the undo action for this value
            Undo.RecordObject((AssetBundleUnity3D)target,
                changed ? "Opened " : "Closed" + label);
            // Update the reference
            value = changed;
            // Make sure changes are persisted (play safe)
            EditorUtility.SetDirty((AssetBundleUnity3D)target);
            // Allow chaining
            return value;
        }

        private int PopupUI(ref int value, string label, string[] options)
        {
            int changed = EditorGUILayout.Popup(
                label, value, options);
            // Do nothing if it didn't change
            if (changed == value) return value;
            // Record the undo action for this value
            Undo.RecordObject((AssetBundleUnity3D)target,
                $"Set {label} to {options[changed]}");
            // Update the reference
            value = changed;
            // Make sure changes are persisted (play safe)
            EditorUtility.SetDirty((AssetBundleUnity3D)target);
            // Allow chaining
            return value;
        }

        private string TextFieldUI(ref string value, string label)
        {
            string changed = GUILayout.TextField(value);
            // Do nothing if it didn't change
            if (changed == value) return value;
            // Record the undo action for this value
            Undo.RecordObject((AssetBundleUnity3D)target,
                $"Changed {label} to {changed}");
            // Update the reference
            value = changed;
            // Make sure changes are persisted (play safe)
            EditorUtility.SetDirty((AssetBundleUnity3D)target);
            // Allow chaining
            return value;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = (AssetBundleUnity3D)target;
            if (string.IsNullOrEmpty(script.Path))
                script.Path = string.Empty;

            GUILayout.Space(6);

            ToggleUI(ref script.IncludeAssetsFromFolders,
                "Include assets from folders");

            EditorGUI.BeginDisabledGroup(
                !script.IncludeAssetsFromFolders);
            ToggleUI(ref script.SearchFoldersRecursively,
                "Search folders recursively");
            EditorGUI.EndDisabledGroup();

            ToggleUI(ref script.VerboseLogging,
                "Enable Verbose Logging");

            GUILayout.Space(12);

            if (FoldoutUI(ref script.ShowPlatformTargets, "Target Platforms"))
            {

                GUILayout.Space(6);

                ToggleUI(ref script.StandaloneWindows,
                    "Standalone Windows");
                ToggleUI(ref script.StandaloneMacOSX,
                    "Standalone Mac OSX");

                // Linux not required when stripping if windows is active
                // This is due to linux being a subset of APIs windows has
                EditorGUI.BeginDisabledGroup(script.StripRedundantAPIs
                    && script.StandaloneWindows);
                ToggleUI(ref script.StandaloneLinux,
                    "Standalone Linux");
                EditorGUI.EndDisabledGroup();

                GUILayout.Space(8);

                ToggleUI(ref script.StripRedundantAPIs,
                    "Strip redundant APIs");
                ToggleUI(ref script.SeparateShaders,
                    "Separate Shaders Only");

                GUILayout.Space(8);

                GUILayout.Label("Graphics API support", GUILayout.Height(20));

                GUILayout.Space(6);

                EditorGUI.BeginDisabledGroup(!SupportsOpenGL(script));
                ToggleUI(ref script.WantOpenGL,
                    "OpenGL (Win/Mac/Linux)");
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!SupportsVulkan(script));
                ToggleUI(ref script.WantVulkan,
                    "Vulkan (Win/Linux)");
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!SupportsD3D11(script));
                ToggleUI(ref script.WantD3D11,
                    "Direct 3D 11 (Windows)");
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!SupportsMetal(script));
                ToggleUI(ref script.WantMetal,
                    "Metal (Mac OSX)");
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(6);

            if (FoldoutUI(ref script.ShowShaderStripping, "Shader Stripping"))
            {
                GUILayout.Space(6);

                ToggleUI(ref script.StripFogLinear,
                    "Strip Linear Fog variants",
                    "When you know you don't use linear fog.");
                ToggleUI(ref script.StripFogExp,
                    "Strip Exponential Fog variants",
                    "When you know you don't use exponential fog.");
                ToggleUI(ref script.StripFogExp2,
                    "Strip ExpSquared Fog variants",
                    "When you know you don't use exp^2 fog.");
                ToggleUI(ref script.StripFogNone,
                    "Strip None Fog variants",
                    "Only enable if you are sure this is safe!");

                GUILayout.Space(8);

                ToggleUI(ref script.StripMetaPass,
                    "Strip Meta passes",
                    "Seem to be used for baked lightmaps.");
                ToggleUI(ref script.StripDeferredPass,
                    "Strip Deferred passes",
                    "Deferred Shading allows more dynamic lights in the scene, " +
                    "but only works on opaque (incl. cutout) materials. " +
                    "Most of the time you want this disabled.");
                ToggleUI(ref script.StripForwardBasePass,
                    "Strip Forward Base passes",
                    "Forward Rendering is the fallback mode supporting only a few dynamic lights. " +
                    "Should be safe to strip, unless you use it for semi-transparent materials.");
                ToggleUI(ref script.StripForwardAddPass,
                    "Strip Forward Add passes",
                    "Forward Rendering is the fallback mode supporting only a few dynamic lights. " +
                    "Should be safe to strip, unless you use it for semi-transparent materials."); ;
                ToggleUI(ref script.StripShadowCasterPass,
                    "Strip Shadow Caster passes",
                    "Enable this if you don't have or want to cast any shadows from materials.");

                GUILayout.Space(8);

                ToggleUI(ref script.StripInstancing,
                    "Strip Instancing variants",
                    "Should be safe to strip if you know that no material will use geometry instancing.");

                GUILayout.Space(8);

                ToggleUI(ref script.StripLightmapDirCombined,
                    "Strip Baked Directional Lightmap variants");
                ToggleUI(ref script.StripLightmapPlain,
                    "Strip Baked Non-Directional Lightmap variants");
                ToggleUI(ref script.StripLightmapDynamicDirCombined,
                    "Strip Realtime Directional Lightmap variants");
                ToggleUI(ref script.StripLightmapDynamicPlain,
                    "Strip Realtime Non-Directional Lightmap variants");
                ToggleUI(ref script.StripLightmapShadowMask,
                    "Strip Baked Shadow Mask Lightmap variants");
                ToggleUI(ref script.StripLightmapSubtractive,
                    "Strip Baked Subtractive Lightmap variants");

            }

            GUILayout.Space(12);

            ToggleUI(ref script.CreateDeterministicAssetBundle,
                "Create Deterministic Asset Bundle");

            ToggleUI(ref script.AppendHashToAssetBundleName,
                "Append Hash To Asset Bundle Name");

            GUILayout.Space(10);

            PopupUI(ref script.Compression,
                "Bundle Compression",
                compressions);

            GUILayout.Space(20);

            if (GUILayout.Button("Set unity3d resource path", GUILayout.Height(24)))
            {
                string fpath = string.IsNullOrEmpty(script.Path) ? "" : Path.GetDirectoryName(script.Path);
                string fname = string.IsNullOrEmpty(script.Path) ? "" : Path.GetFileName(script.Path);
                if (string.IsNullOrEmpty(fpath)) fpath = Path.GetFullPath(Application.dataPath);
                else fpath = Path.GetFullPath(Path.Join(Application.dataPath, fpath));
                var opath = EditorUtility.SaveFilePanel("Unity3D AssetBundle Path", fpath,
                    string.IsNullOrEmpty(script.Path) ? "AssetBundle" : fname, "unity3d");
                // Make folder relative to data path
                // Allows users to move the project
                if (Path.IsPathRooted(opath)) opath = PathUtil
                    .GetRelativePath(Application.dataPath, opath);
                if (!string.IsNullOrEmpty(opath) && script.Path != opath)
                {
                    Undo.RecordObject(script,
                        "Changed Bundle Path");
                    script.Path = opath;
                }
            }

            GUILayout.Space(2);

            TextFieldUI(ref script.Path, "Bundle Path");

            // Render the rest only if path is valid
            if (string.IsNullOrEmpty(script.Path)) return;

            // Re-create potential relative folder name to absolute
            string export = Path.IsPathRooted(script.Path) ? script.Path
                : Path.Combine(Application.dataPath, script.Path);

            GUILayout.Space(20);

            if (GUILayout.Button("Export unity3d resource", GUILayout.Height(48)))
            {

                // By default we export everything in the list
                UnityEngine.Object[] exports = script.Objects;
                // If option is set, we try to resolve folder(s)
                if (script.IncludeAssetsFromFolders)
                {
                    var assets = new List<UnityEngine.Object>();
                    foreach (var obj in script.Objects)
                        CollectAssets(obj, ref assets,
                            script.SearchFoldersRecursively);
                    if (script.VerboseLogging)
                        foreach (var asset in assets)
                            Debug.LogFormat(
                                "  found {0}\n   of type {1}\n",
                                AssetDatabase.GetAssetPath(asset),
                                asset.GetType().ToString());
                    exports = assets.ToArray();
                }

                /*
                // Create summary to report to Zilox ;)
                var aggregated = new SortedDictionary<string, int>();
                foreach (var asset in exports)
                {
                    var type = asset.GetType().ToString();
                    if (aggregated.ContainsKey(type))
                        aggregated[type] += 1;
                    else aggregated.Add(type, 1);
                }
                string summary = string.Format(
                    "Exporting {0} Asset(s)\n",
                    exports.Length);
                foreach (var kv in aggregated)
                {
                    summary += string.Format("  {0} [{1}]",
                        kv.Value, kv.Key);
                }
                Debug.Log(summary);
                */

                BuildAssetBundleOptions options = 0;

                if (script.CreateDeterministicAssetBundle) options
                        |= BuildAssetBundleOptions.DeterministicAssetBundle;
                if (script.AppendHashToAssetBundleName) options
                        |= BuildAssetBundleOptions.AppendHashToAssetBundleName;

                if (script.Compression == 1) options
                        |= BuildAssetBundleOptions.ChunkBasedCompression;
                if (script.Compression == 2) options
                        |= BuildAssetBundleOptions.UncompressedAssetBundle;

                // We need to use obsolete function, since new `BuildAssetBundles`
                // does not allow to store the bundle outside project directory.
                #pragma warning disable CS0618 //  Type or member is obsolete
                options |= BuildAssetBundleOptions.CollectDependencies;
                options |= BuildAssetBundleOptions.CompleteAssets;
                #pragma warning restore CS0618 //  Type or member is obsolete

                StripShaderVariants.CompiledShaders = 0;
                StripShaderVariants.StrippedShaders = 0;

                // Create a HashSet to mark APIs we have already exported
                // Required to avoid exporting the same APIs more than once
                HashSet<GraphicsDeviceType> seen = new HashSet<GraphicsDeviceType>();
                if (script.StandaloneWindows) ExportAssetBundle(exports, export,
                    options, BuildTarget.StandaloneWindows64, script, seen);
                // Strip everything beside shaders if only shaders get separated
                if (script.SeparateShaders) exports = exports.Where(
                    x => typeof(Shader).IsAssignableFrom(x.GetType())).ToArray();
                // Export platform specific bundles with the given assets
                if (script.StandaloneMacOSX) ExportAssetBundle(exports,
                    export, options, BuildTarget.StandaloneOSX, script, seen,
                    seen.Contains(GraphicsDeviceType.OpenGLCore) ? ".metal" : ".mac");
                if (script.StandaloneLinux && !(script.StripRedundantAPIs && script.StandaloneWindows))
                    ExportAssetBundle(exports, export, options, BuildTarget.StandaloneLinux64,
                        script, seen, seen.Contains(GraphicsDeviceType.Vulkan) ? ".metal" : ".nix");

                if (StripShaderVariants.StrippedShaders + StripShaderVariants.CompiledShaders > 0)
                    Debug.Log($"Compiled {StripShaderVariants.CompiledShaders} Shader"
                        + $" Variants, {StripShaderVariants.StrippedShaders} Skipped");

                /* Modern code partially works, but not really suited for our need
                 * Can't store outside of project directory (we could move it)
                 * Creates additional index? resource/manifest (could delete it)

                    string[] assets = new string[script.objects.Length];
                    for (int i = 0; i < script.objects.Length; i++)
                        assets[i] = AssetDatabase.GetAssetPath(script.objects[i]);
                    AssetBundleBuild[] builder = new AssetBundleBuild[1];
                    builder[0] = new AssetBundleBuild();
                    builder[0].assetNames = assets;
                    builder[0].assetBundleName = Path.GetFileNameWithoutExtension(script.path);
                    builder[0].assetBundleVariant = Path.GetExtension(script.path).Substring(1);
                    AssetBundleBuild[] builds = new AssetBundleBuild[0];
                    var target = AssetDatabase.GetAssetPath(script);
                    var dname = Path.GetDirectoryName(target);
                    var fname = Path.GetFileNameWithoutExtension(target);
                    var asset_path = Path.Combine(dname, fname);
                    if (!AssetDatabase.IsValidFolder(asset_path))
                        AssetDatabase.CreateFolder(dname, fname);
                    target = Path.Combine(asset_path, Path.GetFileName(script.path));
                    BuildPipeline.BuildAssetBundles(asset_path, builder,
                        BuildAssetBundleOptions.StrictMode,
                        BuildTarget.StandaloneWindows64);
                    AssetDatabase.Refresh();

                 */
            }

            // Render the rest only if path really exists
            if (!File.Exists(export)) return;

            GUILayout.Space(20);

            if (GUILayout.Button("Open path in explorer", GUILayout.Height(24)))
            {
                EditorUtility.RevealInFinder(export);
            }
        }

        // Export asset bundle for specific platform (with optional suffix)
        private void ExportAssetBundle(UnityEngine.Object[] exports,
            string path, BuildAssetBundleOptions options, BuildTarget target,
            AssetBundleUnity3D script, HashSet<GraphicsDeviceType> seen, string suffix = "")
        {
            GraphicsDeviceType[] apis = GetApis(target, script, seen);
            // Mangle suffix into final filename
            if (string.IsNullOrEmpty("suffix") == false)
                path = Path.Join(Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path)
                        + suffix + Path.GetExtension(path));
            // Update the player settings to correspond to our export settings
            var oldTarget = EditorUserBuildSettings.activeBuildTarget;
            var oldDefaultAPI = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
            var oldGfxAPIs = PlayerSettings.GetGraphicsAPIs(target);
            // EditorUserBuildSettings.SwitchActiveBuildTarget(
            //     BuildTargetGroup.Standalone, target);
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, apis);
            // Get the old settings to be able to reset it once done with export
            var gfxCfg = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var oldFogStripping = gfxCfg.FindProperty("m_FogStripping").boolValue;
            var oldFogKeepLinear = gfxCfg.FindProperty("m_FogKeepLinear").boolValue;
            var oldFogKeepExp = gfxCfg.FindProperty("m_FogKeepExp").boolValue;
            var oldFogKeepExp2 = gfxCfg.FindProperty("m_FogKeepExp2").boolValue;
            var oldFogStripInstancing = gfxCfg.FindProperty("m_InstancingStripping").intValue;
            var oldLightmapStripping = gfxCfg.FindProperty("m_LightmapStripping").boolValue;
            var oldLightmapKeepPlain = gfxCfg.FindProperty("m_LightmapKeepPlain").boolValue;
            var oldLightmapKeepDirCombined = gfxCfg.FindProperty("m_LightmapKeepDirCombined").boolValue;
            var oldLightmapKeepDynamicPlain = gfxCfg.FindProperty("m_LightmapKeepDynamicPlain").boolValue;
            var oldLightmapKeepDynamicDirCombined = gfxCfg.FindProperty("m_LightmapKeepDynamicDirCombined").boolValue;
            var oldLightmapKeepShadowMask = gfxCfg.FindProperty("m_LightmapKeepShadowMask").boolValue;
            var oldLightmapKeepSubtractive = gfxCfg.FindProperty("m_LightmapKeepSubtractive").boolValue;
            // Always enable shader variant stripping for best results
            // Otherwise we are depending on the actual project setting
            GraphicsSettings.logWhenShaderIsCompiled = script.VerboseLogging;
            gfxCfg.FindProperty("m_FogStripping").boolValue = true;
            gfxCfg.FindProperty("m_LightmapStripping").boolValue = true;
            // Either fully strip instancing or keep it (not sure if "unused" makes much sense)
            gfxCfg.FindProperty("m_InstancingStripping").intValue = script.StripInstancing ? 1 : 2;
            gfxCfg.FindProperty("m_FogKeepLinear").boolValue = !script.StripFogLinear;
            gfxCfg.FindProperty("m_FogKeepExp").boolValue = !script.StripFogExp;
            gfxCfg.FindProperty("m_FogKeepExp2").boolValue = !script.StripFogExp2;
            gfxCfg.FindProperty("m_LightmapKeepPlain").boolValue = !script.StripLightmapPlain;
            gfxCfg.FindProperty("m_LightmapKeepDirCombined").boolValue = !script.StripLightmapDirCombined;
            gfxCfg.FindProperty("m_LightmapKeepDynamicPlain").boolValue = !script.StripLightmapDynamicPlain;
            gfxCfg.FindProperty("m_LightmapKeepDynamicDirCombined").boolValue = !script.StripLightmapDynamicDirCombined;
            gfxCfg.FindProperty("m_LightmapKeepShadowMask").boolValue = !script.StripLightmapShadowMask;
            gfxCfg.FindProperty("m_LightmapKeepSubtractive").boolValue = !script.StripLightmapSubtractive;
            // Set config script for shader stripping
            StripShaderVariants.Cfg = script;
            // Update the global game settings
            gfxCfg.ApplyModifiedProperties();

            try
            {
                // Give verbose log message to report use APIs
                if (script.VerboseLogging) Debug.LogFormat(
                    "Create {0} with graphics API: {1}", Path.GetFileName(path),
                    string.Join(", ", Array.ConvertAll(apis, x => x.ToString())));
                // Call the actual exporting functionality
                #pragma warning disable CS0618 // Type or member is obsolete
                BuildPipeline.BuildAssetBundle(null, exports, path, options, target);
                #pragma warning restore CS0618 // Type or member is obsolete
            }
            finally
            {
                StripShaderVariants.Cfg = null;
                // Restore previous settings like any civilized code would do
                PlayerSettings.SetGraphicsAPIs(target, oldGfxAPIs);
                PlayerSettings.SetUseDefaultGraphicsAPIs(target, oldDefaultAPI);
                // Without this next unity startup may complain about mac target!?
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, oldTarget);
                // Reset low level values to roll back to old settings
                gfxCfg.FindProperty("m_FogStripping").boolValue = oldFogStripping;
                gfxCfg.FindProperty("m_FogKeepLinear").boolValue = oldFogKeepLinear;
                gfxCfg.FindProperty("m_FogKeepExp").boolValue = oldFogKeepExp;
                gfxCfg.FindProperty("m_FogKeepExp2").boolValue = oldFogKeepExp2;
                gfxCfg.FindProperty("m_InstancingStripping").intValue = oldFogStripInstancing;
                gfxCfg.FindProperty("m_LightmapStripping").boolValue = oldLightmapStripping;
                gfxCfg.FindProperty("m_LightmapKeepPlain").boolValue = oldLightmapKeepPlain;
                gfxCfg.FindProperty("m_LightmapKeepDirCombined").boolValue = oldLightmapKeepDirCombined;
                gfxCfg.FindProperty("m_LightmapKeepDynamicPlain").boolValue = oldLightmapKeepDynamicPlain;
                gfxCfg.FindProperty("m_LightmapKeepDynamicDirCombined").boolValue = oldLightmapKeepDynamicDirCombined;
                gfxCfg.FindProperty("m_LightmapKeepShadowMask").boolValue = oldLightmapKeepShadowMask;
                gfxCfg.FindProperty("m_LightmapKeepSubtractive").boolValue = oldLightmapKeepSubtractive;
                gfxCfg.ApplyModifiedProperties();
            }
        }

        // Get an array of graphics APIs to build for target platform
        private GraphicsDeviceType[] GetApis(BuildTarget target,
            AssetBundleUnity3D script, HashSet<GraphicsDeviceType> seen)
        {
            List<GraphicsDeviceType> types =
                new List<GraphicsDeviceType>();
            if (SupportsOpenGL(script) && BuildOpenGL(target, script))
                AddIfNotSeenYet(script, seen, types, GraphicsDeviceType.OpenGLCore);
            if (SupportsVulkan(script) && BuildVulkan(target, script))
                AddIfNotSeenYet(script, seen, types, GraphicsDeviceType.Vulkan);
            if (SupportsD3D11(script) && BuildD3D11(target, script))
                AddIfNotSeenYet(script, seen, types, GraphicsDeviceType.Direct3D11);
            if (SupportsMetal(script) && BuildMetal(target, script))
                AddIfNotSeenYet(script, seen, types, GraphicsDeviceType.Metal);
            return types.ToArray();
        }

        // Helper to optionally strip duplicate graphlics APIs
        // E.g. we may only want Metal vartiants for OSX, as
        // windows variant already provides Vulkan API
        private void AddIfNotSeenYet(
            AssetBundleUnity3D script, HashSet<GraphicsDeviceType> seen,
            List<GraphicsDeviceType> types, GraphicsDeviceType type)
        {
            if (script.StripRedundantAPIs)
            {
                if (seen.Contains(type)) return;
                seen.Add(type);
            }
            types.Add(type);
        }

    }


    // Polyfill Implementation for `Path.GetRelativePath`
    // From https://stackoverflow.com/a/74747405/1550314
    static class PathUtil
    {
        public static string GetRelativePath(string relativeTo, string path)
        {
            #if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return Path.GetRelativePath(relativeTo, path);
            #else
            return GetRelativePathPolyfill(relativeTo, path);
            #endif
        }

        #if !(NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        static string GetRelativePathPolyfill(string relativeTo, string path)
        {
            path = Path.GetFullPath(path);
            relativeTo = Path.GetFullPath(relativeTo);

            var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            IReadOnlyList<string> p1 = path.Split(separators);
            IReadOnlyList<string> p2 = relativeTo.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var sc = StringComparison;

            int i;
            int n = Math.Min(p1.Count, p2.Count);
            for (i = 0; i < n; i++)
                if (!string.Equals(p1[i], p2[i], sc))
                    break;

            if (i == 0)
            {
                // Cannot make a relative path, for example if the path resides on another drive.
                return path;
            }

            p1 = p1.Skip(i).Take(p1.Count - i).ToList();

            if (p1.Count == 1 && p1[0].Length == 0)
                p1 = Array.Empty<string>();

            string relativePath = string.Join(
                new string(Path.DirectorySeparatorChar, 1),
                Enumerable.Repeat("..", p2.Count - i).Concat(p1));

            if (relativePath.Length == 0)
                relativePath = ".";

            return relativePath;
        }

        static StringComparison StringComparison =>
            IsCaseSensitive ?
                StringComparison.Ordinal :
                StringComparison.OrdinalIgnoreCase;

        static bool IsCaseSensitive =>
            !(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        #endif
    }


    // Programatically strip certain shader variants
    // Also useful to just keep count of what is done
    class StripShaderVariants : IPreprocessShaders
    {

        // Assigned from outside to grab by us
        public static AssetBundleUnity3D Cfg = null;

        public static int CompiledShaders = 0;
        public static int StrippedShaders = 0;

        // Multiple callback may be implemented.
        public int callbackOrder => int.MinValue;

        static ShaderKeyword FOG_EXP = new ShaderKeyword("FOG_EXP");
        static ShaderKeyword FOG_EXP2 = new ShaderKeyword("FOG_EXP2");
        static ShaderKeyword FOG_LINEAR = new ShaderKeyword("FOG_LINEAR");

        // Do the processing of shaders to potentially alter the shader variants to be compiled
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Must have config
            if (Cfg == null) return;
            // In development, don't strip any variants
            if (EditorUserBuildSettings.development) return;
            // Register some statistics to look at later
            StripShaderVariantCounter.ShadersBefore = data.Count;
            // Strip certain pass variants in case render path is known to be unused
            if (Cfg.StripMetaPass && snippet.passType == PassType.Meta) data.Clear();
            else if (Cfg.StripDeferredPass && snippet.passType == PassType.Deferred) data.Clear(); 
            else if (Cfg.StripForwardBasePass && snippet.passType == PassType.ForwardBase) data.Clear(); 
            else if (Cfg.StripForwardAddPass && snippet.passType == PassType.ForwardAdd) data.Clear();
            else if (Cfg.StripShadowCasterPass && snippet.passType == PassType.ShadowCaster) data.Clear();
            // Process the variants array from behind
            for (int i = data.Count - 1; i != -1; --i)
            {
                bool remove = false;
                bool isFogExp = data[i].shaderKeywordSet.IsEnabled(FOG_EXP);
                bool isFogExp2 = data[i].shaderKeywordSet.IsEnabled(FOG_EXP2);
                bool isFogLinear = data[i].shaderKeywordSet.IsEnabled(FOG_LINEAR);
                // Debug.Log(shader.name + " Keywords " + string.Join(", ",
                //     data[i].shaderKeywordSet.GetShaderKeywords()));
                bool isFog = isFogExp | isFogExp2 | isFogLinear;
                if (Cfg.StripFogNone && !isFog) remove = true;
                else if (Cfg.StripFogExp && isFogExp) remove = true;
                else if (Cfg.StripFogExp2 && isFogExp2) remove = true;
                else if (Cfg.StripFogLinear && isFogLinear) remove = true;
                if (remove) data.RemoveAt(i);
            }
            // Print a log if verbose logging is enabled
            if (Cfg.VerboseLogging) Debug.Log(string.Format(
                "{0}: {1} variants stripped: {3}, compiling: {2}",
                shader.name, snippet.passName, data.Count,
                StripShaderVariantCounter.ShadersBefore - data.Count));
        }
    }

    // Just a hook to get statistics very late in the process
    class StripShaderVariantCounter : IPreprocessShaders
    {
        public static int ShadersBefore = -1;
        // Multiple callback may be implemented (run late).
        public int callbackOrder => int.MaxValue;
        // Do the processing of shaders to potentially alter the shader variants to be compiled
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Do some general statistics
            if (ShadersBefore == -1) return;
            StripShaderVariants.StrippedShaders += ShadersBefore - data.Count;
            StripShaderVariants.CompiledShaders += data.Count;
            ShadersBefore = -1;
        }
    }

}