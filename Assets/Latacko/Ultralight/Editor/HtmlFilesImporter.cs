using UnityEngine;
using System.IO;
using UnityEditor.AssetImporters;

namespace Latacko.UltralightUnityEditor
{
    [ScriptedImporter(1, new string[] { "js", "css", "pem", "dat" })]
    public class HtmlFilesImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            byte[] bytes = File.ReadAllBytes(ctx.assetPath);

            TextAsset asset = new TextAsset(bytes);

            ctx.AddObjectToAsset("text", asset);
            ctx.SetMainObject(asset);
        }
    }
}