using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UI;

namespace Latacko.UltralightUnity
{
    public class ULView : MonoBehaviour
    {
        public UltralightViewManager WebView { get; private set; }

        public uint Width => WebView.Width;
        public uint Height => WebView.Height;

        public RawImage rawImage;

        Texture2D texture;
        RenderTexture gpuTexture;
        RenderTexture flippedTexture;

        uint lastResize = 0;

        int bufferSize;

        public ComputeShader flipShader;

        int kernel;

        async void Start()
        {
            await ULManagerAPI.WaitForUltralight();

            WebView = await ULManagerAPI.CreateView(1920, 1080);
            
            InitializeTexture();


            kernel = flipShader.FindKernel("Flip");
        }

        void Update()
        {
            if (WebView == null)
                return;
            LoadTexture();
            WebView.LoadAdvancedEvent();
            WebView.ReadEmittedMessages();
            WebView.ReadMessageConsole();
            WebView.ReadBaseEvents();
        }

        /// <summary>
        /// Wait until WebView is set
        /// </summary>
        /// <returns></returns>
        public async Awaitable WaitUntilInitialized()
        {
            while (WebView == null)
            {
                await Awaitable.EndOfFrameAsync();
            }
        }

        void InitializeTexture()
        {
            var _frameSize = WebView.GetFrameSize();

            WebView.Width = (uint)_frameSize.x;
            WebView.Height = (uint)_frameSize.y;
            texture = new Texture2D(_frameSize.x, _frameSize.y, TextureFormat.BGRA32, false);

            bufferSize = WebView.GetBufferSize();

            lastResize = WebView.GetResizeCounter();

            gpuTexture = new RenderTexture(_frameSize.x, _frameSize.y, 0)
            {
                enableRandomWrite = true
            };
            gpuTexture.Create();

            flippedTexture = new RenderTexture(_frameSize.x, _frameSize.y, 0)
            {
                enableRandomWrite = true
            };
            flippedTexture.Create();

            rawImage.texture = flippedTexture;
        }

        void LoadTexture()
        {
            //? Resize
            if (WebView.GetResizeCounter() != lastResize)
            {
                InitializeTexture();
                return;
            }

            unsafe
            {
                byte* src = WebView.GetFramePointer();

                var texBuffer = texture.GetRawTextureData<byte>();
                void* dst = texBuffer.GetUnsafePtr();

                // CPU shared memory -> Texture2D
                UnsafeUtility.MemCpy(dst, src, bufferSize);
            }

            texture.Apply(false);

            // Texture2D -> GPU source texture
            Graphics.Blit(texture, gpuTexture);

            // flip compute: gpuTexture -> flippedTexture
            flipShader.SetTexture(kernel, "Source", gpuTexture);
            flipShader.SetTexture(kernel, "Result", flippedTexture);

            int groupsX = Mathf.CeilToInt(gpuTexture.width / 8f);
            int groupsY = Mathf.CeilToInt(gpuTexture.height / 8f);
            flipShader.Dispatch(kernel, groupsX, groupsY, 1);
        }
    }
}