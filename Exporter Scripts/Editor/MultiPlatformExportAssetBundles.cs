using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Based off the original by Xyth - https://github.com/7D2D/Templates-and-Utilities/blob/29758ef38db5dc291004c3b5facce826a45b6df9/MultiPlatformExportAssetBundles.zip

namespace UnityAssetExporter
{

    [ExecuteInEditMode]
    public class MultiPlatformExportAssetBundles
    {
        static void SaveBundleFromSelection(BuildAssetBundleOptions options)
        {
            string path = EditorUtility.SaveFilePanel("Save Unity3D AssetBundle", "",
                "unity3d", "Please enter a file name to save the unity3d resource bundle to");
            if (path.Length != 0)
            {
                // include the following Graphic APIs
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new GraphicsDeviceType[] {
                    GraphicsDeviceType.Direct3D11, GraphicsDeviceType.OpenGLCore,
                    GraphicsDeviceType.Vulkan /*, GraphicsDeviceType.Metal */ });
                // Build the resource file from the active selection.

                // Build the resource file from the active selection.
                Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

                #pragma warning disable CS0618 //  Type or member is obsolete
                // We need to use obsolete function, since new `BuildAssetBundles`
                // does not allow to store the bundle outside project directory.
                options |= BuildAssetBundleOptions.CollectDependencies;
                options |= BuildAssetBundleOptions.CompleteAssets;
                BuildPipeline.BuildAssetBundle(null, selection, path,
                    options, BuildTarget.StandaloneWindows64);
                #pragma warning restore CS0618 //  Type or member is obsolete
                Selection.objects = selection;
            }
        }

        [MenuItem("Assets/Build LZ4 AssetBundle From Selection")]
        static void ExportResourceLZ4()
        {
            SaveBundleFromSelection(BuildAssetBundleOptions.ChunkBasedCompression);
        }

        [MenuItem("Assets/Build LZMA AssetBundle From Selection")]
        static void ExportResourceLZMA()
        {
            SaveBundleFromSelection(BuildAssetBundleOptions.None);
        }

        [MenuItem("Assets/Build Uncompressed AssetBundle From Selection")]
        static void ExportResourceUncompressed()
        {
            SaveBundleFromSelection(BuildAssetBundleOptions.UncompressedAssetBundle);
        }

    }

}