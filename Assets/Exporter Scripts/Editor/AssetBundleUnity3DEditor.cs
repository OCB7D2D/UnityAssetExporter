using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
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
                    if (recursive) CollectAssets(asset, ref exports, recursive);
                    else if (!AssetDatabase.IsValidFolder(path)) exports.Add(asset);
                }
            }
            // Just add as is to export
            else exports.Add(root);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var script = (AssetBundleUnity3D)target;
            if (string.IsNullOrEmpty(script.Path))
                script.Path = string.Empty;

            string fpath = string.IsNullOrEmpty(script.Path) ? "" : Path.GetDirectoryName(script.Path);
            string fname = string.IsNullOrEmpty(script.Path) ? "" : Path.GetFileName(script.Path);

            script.IncludeAssetsFromFolders = GUILayout.Toggle(
                script.IncludeAssetsFromFolders,
                "Include assets from folders",
                GUILayout.Height(20));

            EditorGUI.BeginDisabledGroup(
                !script.IncludeAssetsFromFolders);
            script.SearchFoldersRecursively = GUILayout.Toggle(
                script.SearchFoldersRecursively,
                "Search folders recursively",
                GUILayout.Height(20));
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(20);

            script.CreateDeterministicAssetBundle = GUILayout.Toggle(
                script.CreateDeterministicAssetBundle,
                "Create Deterministic Asset Bundle",
                GUILayout.Height(20));

            script.AppendHashToAssetBundleName = GUILayout.Toggle(
                script.AppendHashToAssetBundleName,
                "Append Hash To Asset Bundle Name",
                GUILayout.Height(20));

            GUILayout.Space(10);

            script.Compression = EditorGUILayout.Popup(
                "Bundle Compression",
                script.Compression,
                compressions);

            GUILayout.Space(20);

            if (GUILayout.Button("Set unity3d resource path", GUILayout.Height(24)))
            {
                var opath = EditorUtility.SaveFilePanel("Unity3D AssetBundle Path", fpath,
                    string.IsNullOrEmpty(script.Path) ? "AssetBundle" : fname, "unity3d");
                // Make folder relative to data path
                // Allows users to move the project
                if (Path.IsPathRooted(opath)) opath = PathUtil
                    .GetRelativePath(Application.dataPath, opath);
                if (!string.IsNullOrEmpty(opath))
                    script.Path = opath;
            }

            GUILayout.Space(2);

            script.Path = GUILayout.TextField(script.Path);

            // Render the rest only if path is valid
            if (string.IsNullOrEmpty(script.Path)) return;

            // Re-create potential relative folder name to absolute
            string export = Path.IsPathRooted(script.Path) ? script.Path
                : Path.Combine(Application.dataPath, script.Path);

            GUILayout.Space(20);

            if (GUILayout.Button("Export unity3d resource", GUILayout.Height(48)))
            {

                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new GraphicsDeviceType[] {
                GraphicsDeviceType.Direct3D11, GraphicsDeviceType.OpenGLCore, GraphicsDeviceType.Vulkan });

                // By default we export everything in the list
                UnityEngine.Object[] exports = script.Objects;
                // If option is set, we try to resolve folder(s)
                if (script.IncludeAssetsFromFolders)
                {
                    var assets = new List<UnityEngine.Object>();
                    foreach (var obj in script.Objects)
                        CollectAssets(obj, ref assets,
                            script.SearchFoldersRecursively);
                    foreach (var asset in assets)
                        Debug.LogFormat(" exporting {0} ({1})",
                            AssetDatabase.GetAssetPath(asset),
                            asset.GetType().ToString());
                    exports = assets.ToArray();
                }

                BuildAssetBundleOptions build_options = 0;

                if (script.CreateDeterministicAssetBundle) build_options
                        |= BuildAssetBundleOptions.DeterministicAssetBundle;
                if (script.AppendHashToAssetBundleName) build_options
                        |= BuildAssetBundleOptions.AppendHashToAssetBundleName;

                if (script.Compression == 1) build_options
                        |= BuildAssetBundleOptions.ChunkBasedCompression;
                if (script.Compression == 2) build_options
                        |= BuildAssetBundleOptions.UncompressedAssetBundle;

                #pragma warning disable CS0618 //  Type or member is obsolete
                // We need to use obsolete function, since new `BuildAssetBundles`
                // does not allow to store the bundle outside project directory.
                build_options |= BuildAssetBundleOptions.CollectDependencies;
                build_options |= BuildAssetBundleOptions.CompleteAssets;
                // Call the actual exporting functionality
                BuildPipeline.BuildAssetBundle(null, exports,
                    export, build_options, BuildTarget.StandaloneWindows64);
                #pragma warning restore CS0618 //  Type or member is obsolete

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

}