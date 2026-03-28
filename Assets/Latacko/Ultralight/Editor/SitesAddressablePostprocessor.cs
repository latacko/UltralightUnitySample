using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace Latacko.UltralightUnityEditor
{
    public class SitesAddressablePostprocessor : AssetPostprocessor
    {
        static readonly HashSet<string> supportedExtensions = new()
        {
            ".html", ".js", ".css", ".png", ".jpg", ".jpeg", ".dat", ".pem", ".txt", ".bytes", ".json", ".xml"
        };
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            const string groupName = "Sites";
            var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, false, null);

            if (group.GetSchema<ContentUpdateGroupSchema>() == null)
                group.AddSchema<ContentUpdateGroupSchema>();

            var schema = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;

            schema.IncludeInBuild = true;

            bool changed = false;

            var entriesToRemove = new List<AddressableAssetEntry>();
            foreach (var entry in group.entries)
            {
                string ext = System.IO.Path.GetExtension(entry.address).ToLower();
                if (!supportedExtensions.Contains(ext))
                    entriesToRemove.Add(entry);
            }
            foreach (var entry in entriesToRemove)
                settings.RemoveAssetEntry(entry.guid);

            foreach (var path in importedAssets)
            {
                if (!path.StartsWith("Assets/Sites/")) continue;
                if (AssetDatabase.IsValidFolder(path)) continue;

                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (!supportedExtensions.Contains(ext)) continue; // skip LICENSE, etc.

                string guid = AssetDatabase.AssetPathToGUID(path);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = path;
                changed = true;
            }

            if (changed)
                AssetDatabase.SaveAssets();
        }
    }
}
