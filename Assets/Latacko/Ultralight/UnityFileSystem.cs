using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using UltralightSharedClasses.Classes;
using UltralightSharedClasses.FileSystemStructs;
using UltralightSharedClasses.Structs;
using UnityEngine;

namespace Latacko.UltralightUnity
{
    public unsafe partial class UnityFileSystem : MonoBehaviour
    {
        readonly Queue<(nint ev, string path)> _pendingFileRequests = new();
        public List<WebPageSO> Pages = new();
        static readonly string[] homePages =
        {
        "index",
        "main",
        "home"
    };

        MemoryMappedFile mmf;
        MemoryMappedViewAccessor accessor;

        FileSystemHeader* header;

        uint existOffset;
        uint fileOffset;
        private void Start()
        {
            mmf = CreateMMF.OpenMemoryMappedFile(BASE_FILE_NAME.FILE_MANAGER);
            accessor = mmf.CreateViewAccessor();

            header = (FileSystemHeader*)accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();

            existOffset = header->existOffset;
            fileOffset = header->fileOffset;
        }

        void Update()
        {
            // return;
            FileExistRead();
            OpenFileRead();
            ProcessPendingFiles();
        }

        public void FileExistRead()
        {
            byte* basePtr = (byte*)header;
            while (header->fileExistRead < header->fileExistWrite)
            {
                int index = (int)(header->fileExistRead % ChunksData.FILE_EXIST_CHUNKS);
                FileExistEvent* ev = (FileExistEvent*)(basePtr + existOffset + index * sizeof(FileExistEvent));
                (var eventType, var headerObject, var stringList) = StringManager.ReadString(ev->id);
                var _path = stringList[0];
                ev->exist = (byte)(GetFile(_path) != null ? 1 : 0);
                ev->set = 1;
                Thread.MemoryBarrier();
                header->fileExistRead++;
            }
        }

        public void OpenFileRead()
        {
            byte* basePtr = (byte*)header;
            while (header->openFileRead < header->openFileWrite)
            {
                int index = (int)(header->openFileRead % ChunksData.FILE_OPEN_CHUNKS);
                FileOpenId* ev = (FileOpenId*)(basePtr + fileOffset + index * sizeof(FileOpenId));

                (var eventType, var headerObject, var stringList) = StringManager.ReadString(ev->path_id);

                var _path = stringList[0];
                _pendingFileRequests.Enqueue(((nint)ev, stringList[0]));

                Thread.MemoryBarrier();
                header->openFileRead++;
            }
        }


        public bool IsHomePage(string incomingRoute, WebPageSO.WebPageFile webPageFile)
        {
            if (incomingRoute != "" || webPageFile.Ext != ".html") return false;

            return homePages.Contains(webPageFile.Name);
        }

        public void Dispose()
        {
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