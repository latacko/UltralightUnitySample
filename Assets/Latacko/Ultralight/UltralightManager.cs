using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Threading;
using UltralightSharedClasses.Classes;
using UltralightSharedClasses.FileSystemStructs;
using UltralightSharedClasses.Structs;
using UnityEngine;

namespace Latacko.UltralightUnity
{
    internal unsafe partial class UltralightManager : MonoBehaviour, IDisposable
    {
        private static WaitForSeconds _waitForSeconds1 = new(1);

        internal static UltralightManager Instance { get; private set; }

        [SerializeField] bool showUltralightLog;

        const uint MAGIC = 0x6C617461;
        const string STARTING_LINE = "Ultralight started!";

        uint lastFrame = 0;

        MemoryMappedFile mmf;
        MemoryMappedViewAccessor accessor;

        Header* Header;
        byte* BasePtr;

        int requestViewOffset;
        int destroyViewOffset;

        readonly Dictionary<uint, UltralightViewManager> views = new();

        readonly Dictionary<IntPtr, Action<uint, UltralightViewManager>> waitingViews = new();

        bool ultralightStarted;

        void RunUltralight()
        {
            string exePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Ultralight",
#if UNITY_WINDOWS
            "UltralightProducer.exe"
#else
                "UltralightProducer"
#endif
            );
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);

            process.OutputDataReceived += ReadUltralightLine;
            process.BeginOutputReadLine();
        }

        private void ReadUltralightLine(object sender, System.Diagnostics.DataReceivedEventArgs args)
        {
            if (showUltralightLog)
                UnityEngine.Debug.Log(args.Data);
            if (args.Data == STARTING_LINE)
                ultralightStarted = true;
        }


        IEnumerator DeleteStringLoop()
        {
            while (true)
            {
                yield return _waitForSeconds1;
                StringManager.TestIfDelete();
                FileManager.TestIfDelete();
            }
        }

        public void RequestNewView(uint width, uint height, bool isTransparent, Action<uint, UltralightViewManager> callback)
        {
            int index = (int)(Header->RequestViewEventWrite % ChunksData.REQUEST_VIEW_EVENT_CHUNKS);

            RequestViewEvent* ev = (RequestViewEvent*)(BasePtr + requestViewOffset + index * sizeof(RequestViewEvent));
            ev->width = width;
            ev->height = height;
            ev->isTransparent = (byte)(isTransparent ? 1 : 0);
            waitingViews.Add((IntPtr)ev, callback);

            Thread.MemoryBarrier();
            Header->RequestViewEventWrite++;
        }

        public void DestroyView(uint id)
        {
            int index = (int)(Header->DestroyViewEventWrite % ChunksData.DESTORY_VIEW_EVENT_CHUNKS);

            DestroyViewEvent* ev = (DestroyViewEvent*)(BasePtr + destroyViewOffset + index * sizeof(DestroyViewEvent));

            ev->id = id;

            Thread.MemoryBarrier();
            Header->DestroyViewEventWrite++;

            views.Remove(id);
        }

        void ScanForCreatedViews()
        {
            foreach (var item in new Dictionary<IntPtr, Action<uint, UltralightViewManager>>(waitingViews))
            {
                RequestViewEvent* ev = (RequestViewEvent*)item.Key;
                if (ev->id == 0)
                    continue;
                item.Value(ev->id, RegisterNewView(ev->id));
                waitingViews.Remove(item.Key);
            }
        }

        UltralightViewManager RegisterNewView(uint id)
        {
            var _viewManager = new UltralightViewManager(id);
            views.Add(id, _viewManager);
            return _viewManager;
        }

        public void Dispose()
        {
            foreach (var view in new Dictionary<uint, UltralightViewManager>(views))
            {
                DestroyView(view.Key);
            }
            FileManager.DeleteAll();
            StringManager.DeleteAll();
            Header->magic = Header->magic + 1;
            Debug.Log("Adding magic");
            accessor?.Dispose();
            mmf?.Dispose();

            GC.SuppressFinalize(this);
        }

        void OnDestroy()
        {
            Dispose();
        }
    }
}