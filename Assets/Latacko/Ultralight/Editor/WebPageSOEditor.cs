using System.IO;
using Latacko.UltralightUnity;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Latacko.UltralightUnityEditor
{
    [CustomEditor(typeof(WebPageSO))]
    public class WebPageSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10);

            WebPageSO assetList = (WebPageSO)target;

            GUILayout.Label("Drag Assets Here", EditorStyles.boldLabel);

            Rect dropArea = GUILayoutUtility.GetRect(0, 70, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop assets to add", new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            });

            HandleDragAndDrop(dropArea, assetList);
            GUILayout.Space(20);

            if (GUILayout.Button("Add all files in currect dir"))
            {
                assetList.files.Clear();
                string _fullPath = Path.GetDirectoryName(Path.GetFullPath(AssetDatabase.GetAssetPath(assetList)));
                foreach (var item in Directory.EnumerateFiles(_fullPath, "*", SearchOption.AllDirectories))
                {
                    if (item.EndsWith(".meta") || item.EndsWith("asset"))
                        continue;
                    if (item.StartsWith(Application.dataPath))
                    {
                        string relative = "Assets" + item.Substring(Application.dataPath.Length);

                        var asset = AssetDatabase.LoadAssetAtPath<Object>(relative);
                        if (asset is VectorImage)
                            continue;

                        if (asset != null)
                        {
                            assetList.files.Add(new WebPageSO.WebPageFile
                            {
                                Asset = CreateAssetReference(asset)
                            });
                        }
                        else
                        {
                            Debug.LogWarning("Failed to load asset: " + relative);
                        }
                    }
                }
                EditorUtility.SetDirty(assetList);
                assetList.OnValidate();
                AssetDatabase.SaveAssets();
            }
        }

        static AssetReference CreateAssetReference(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            return new AssetReference(guid);
        }

        private void HandleDragAndDrop(Rect dropArea, WebPageSO assetList)
        {
            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case UnityEngine.EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    break;

                case UnityEngine.EventType.DragPerform:
                    DragAndDrop.AcceptDrag();

                    foreach (Object dragged in DragAndDrop.objectReferences)
                    {
                        assetList.files.Add(new WebPageSO.WebPageFile
                        {
                            Asset = CreateAssetReference(dragged)
                        });
                    }

                    EditorUtility.SetDirty(assetList);
                    assetList.OnValidate();
                    AssetDatabase.SaveAssets();

                    break;
            }
        }
    }
}