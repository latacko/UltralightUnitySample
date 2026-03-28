using System;
using UltralightSharedClasses.Classes;
using UltralightSharedClasses.Structs;
using UnityEngine;

namespace Latacko.UltralightUnity
{
    internal partial class UltralightManager : MonoBehaviour, IDisposable
    {
        bool ultralightManagerReady;
        private async void Awake()
        {
            Instance = this;

            RunUltralight();

            await WaitForUltralight();

            mmf = CreateMMF.OpenMemoryMappedFile(BASE_FILE_NAME.MANAGER);

            accessor = mmf.CreateViewAccessor();
            unsafe
            {
                BasePtr = (byte*)accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
                Header = (Header*)BasePtr;

                requestViewOffset = (int)Header->requestViewOffset;
                destroyViewOffset = (int)Header->destroyViewOffset;

                if (Header->magic != MAGIC)
                    throw new Exception("MAGIC MISMATCH!");
            }

            StartCoroutine(DeleteStringLoop());
            ultralightManagerReady = true;
        }

        void Update()
        {
            if (!ultralightManagerReady)
                return;
            unsafe
            {
                Header->requestCounter++;

                if (Header->frameCounter == lastFrame)
                    return;

                lastFrame = Header->frameCounter;

                ScanForCreatedViews();
            }
        }


        private async Awaitable WaitForUltralight()
        {
            while (!ultralightStarted)
            {
                await Awaitable.EndOfFrameAsync();
            }
            await Awaitable.WaitForSecondsAsync(1);
        }

        public async Awaitable WaitForManager()
        {
            while (!ultralightManagerReady)
            {
                await Awaitable.EndOfFrameAsync();
            }
        }
    }
}