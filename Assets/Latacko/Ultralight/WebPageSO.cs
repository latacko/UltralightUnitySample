using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Latacko.UltralightUnity
{
    [CreateAssetMenu(fileName = "WebPageSO", menuName = "Scriptable Objects/Ultralight/WebPageSO")]
    public class WebPageSO : ScriptableObject
    {
        public bool disabled;
#if UNITY_EDITOR
        public string fullPath;
#endif
        public string pagePath;
        public List<WebPageFile> files = new();

        [System.Serializable]
        public class WebPageFile
        {
            [field: SerializeField]
            public string Path { get; internal set; }
            [field: SerializeField]
            public string Ext { get; internal set; }
            [field: SerializeField]
            public string Name { get; internal set; }
            public AssetReference Asset;
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            fullPath = Path.GetDirectoryName(Path.GetFullPath(AssetDatabase.GetAssetPath(this)));
            pagePath = new DirectoryInfo(fullPath).Name;
            if (pagePath == "HTML")
                pagePath = "";
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].Asset == null) continue;
                string _path = AssetDatabase.GUIDToAssetPath(files[i].Asset.AssetGUID);
                files[i].Path = Path.GetDirectoryName(Path.GetFullPath(_path)).Replace(fullPath, "").Replace("\\", "/");
                files[i].Ext = "." + GetFirstExtension(Path.GetFullPath(_path));
                files[i].Name = Path.GetFileNameWithoutExtension(_path);
            }
        }
#endif

        public static string GetFirstExtension(string filename)
        {
            // Find the first '.' from left
            int dotIndex = filename.IndexOf('.');
            if (dotIndex == -1 || dotIndex == filename.Length - 1)
                return ""; // no extension

            // Find next dot after first one (for multiple extensions)
            int nextDot = filename.IndexOf('.', dotIndex + 1);

            // If there's another dot, take the first extension
            if (nextDot != -1)
                return filename.Substring(dotIndex + 1, nextDot - dotIndex - 1);

            // Otherwise, take everything after the first dot
            return filename.Substring(dotIndex + 1);
        }

    }
}