using System.Threading;
using UltralightSharedClasses.FileSystemStructs;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Latacko.UltralightUnity
{
    public partial class UnityFileSystem : MonoBehaviour
    {

        async void ProcessPendingFiles()
        {
            while (_pendingFileRequests.TryDequeue(out var request))
            {
                var bytes = await GetBytes(GetFile(request.path));

                unsafe
                {
                    FileOpenId* ev = (FileOpenId*)request.ev;
                    if (bytes == null || bytes.Length == 0)
                        ev->file_id = 1;
                    else
                        ev->file_id = FileManager.GenerateMMF(bytes);

                    Thread.MemoryBarrier();
                }
            }
        }

        public WebPageSO.WebPageFile GetFile(string path)
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                if (Pages[i] == null)
                    continue;
                // Debug.Log(Pages[i].pagePath);
                if (!path.StartsWith(Pages[i].pagePath)) continue;

                path = path.Substring(Pages[i].pagePath.Length);
                if (path.StartsWith("/"))
                    path = path.Substring(1);
                for (int j = 0; j < Pages[i].files.Count; j++)
                {
                    var _route = (Pages[i].files[j].Path == "" ? "" : Pages[i].files[j].Path + "/") + (Pages[i].files[j].Name + Pages[i].files[j].Ext).Replace(".ignoreThisExt", "");
                    if (path != _route.Substring(0, Mathf.Min(_route.Length, path.Length)) && !IsHomePage(path, Pages[i].files[j])) continue;
                    return Pages[i].files[j];
                }
            }
            return null;
        }

        public async Awaitable<byte[]> GetBytes(WebPageSO.WebPageFile webPageFile)
        {
            try
            {
                switch (webPageFile.Ext)
                {
                    case ".png":
                        {
                            var handle = webPageFile.Asset.LoadAssetAsync<Texture2D>();
                            await handle.Task;
                            var texture = handle.Result;
                            var readable = GetEncodeableTexture(texture);
                            var bytes = readable.EncodeToPNG();
                            Destroy(readable);
                            Addressables.Release(handle);
                            return bytes;
                        }
                    case ".jpg":
                    case ".jpeg":
                        {
                            var handle = webPageFile.Asset.LoadAssetAsync<Texture2D>();
                            await handle.Task;
                            var texture = handle.Result;
                            var readable = GetEncodeableTexture(texture);
                            var bytes = readable.EncodeToJPG();
                            Destroy(readable);
                            Addressables.Release(handle);
                            return bytes;
                        }
                    case ".svg":
                    case ".html":
                    case ".js":
                    case ".css":
                    case ".dat":
                    case ".pem":
                        {
                            var handle = webPageFile.Asset.LoadAssetAsync<TextAsset>();
                            await handle.Task;
                            var bytes = handle.Result.bytes;
                            Addressables.Release(handle);
                            return bytes;
                        }
                    default:
                        Debug.LogError("Unconfigured extension: " + webPageFile.Ext);
                        return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        public Texture2D GetEncodeableTexture(Texture2D texture2D)
        {
            RenderTexture rt = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0);
            Graphics.Blit(texture2D, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(texture2D.width, texture2D.height);
            readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return readable;
        }
    }
}