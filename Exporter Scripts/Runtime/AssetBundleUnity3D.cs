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

        [Space(10)]

        [Tooltip("Drag objects to include in the export here")]
        public Object[] Objects;

    }

}

