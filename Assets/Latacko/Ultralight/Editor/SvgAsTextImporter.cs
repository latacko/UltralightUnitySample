using UnityEditor;
using UnityEngine;
using System.IO;

namespace Latacko.UltralightUnityEditor
{
    public class SvgTextCopyGenerator : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] imported,
            string[] deleted,
            string[] moved,
            string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (!path.EndsWith(".svg")) continue;

                string svgText = File.ReadAllText(path);

                string txtPath = Path.Combine(
                    Path.GetDirectoryName(path),
                    Path.GetFileName(path) + ".txt"
                );

                File.WriteAllText(txtPath, svgText);
                AssetDatabase.ImportAsset(txtPath);
            }
        }
    }
}