using UnityEngine;

namespace UnityAssetExporter
{

    [CreateAssetMenu(fileName = "AssetBundle", menuName = "Unity3D Bundle", order = 2)]
    [System.Serializable]
    public class AssetBundleUnity3D : ScriptableObject
    {

        [HideInInspector]
        public string Path = "";

        [HideInInspector]
        public bool IncludeAssetsFromFolders = false;

        [HideInInspector]
        public bool SearchFoldersRecursively = false;

        [HideInInspector]
        public bool CreateDeterministicAssetBundle = true;

        [HideInInspector]
        public bool AppendHashToAssetBundleName = true;

        [HideInInspector]
        public int Compression = 1;

        [HideInInspector]
        public bool VerboseLogging = false;

        [HideInInspector]
        public bool ShowPlatformTargets = false;
        [HideInInspector]
        public bool StandaloneWindows = true;
        [HideInInspector]
        public bool StandaloneMacOSX = false;
        [HideInInspector]
        public bool StandaloneLinux = false;
        [HideInInspector]
        public bool StripRedundantAPIs = true;
        [HideInInspector]
        public bool WantD3D11 = true;
        [HideInInspector]
        public bool WantOpenGL = true;
        [HideInInspector]
        public bool WantVulkan = true;
        [HideInInspector]
        public bool WantMetal = true;

        [Space(10)]

        [Tooltip("Drag objects to include in the export here")]
        public Object[] Objects;

    }

}
