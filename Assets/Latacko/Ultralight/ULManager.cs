using Unity.VisualScripting.YamlDotNet.Core;
using UnityEngine;

namespace Latacko.UltralightUnity
{
    public static class ULManagerAPI
    {
        public static async Awaitable<UltralightViewManager> CreateView(uint width, uint height)
        {
            return await CreateView(width, height, false);
        }

        public static async Awaitable<UltralightViewManager> CreateView(uint width, uint height, bool isTransparent)
        {
            UltralightViewManager _viewManager = null;
            UltralightManager.Instance.RequestNewView(width, height, isTransparent, (id, viewManger) =>
            {
                _viewManager = viewManger;
            });
            while (_viewManager == null)
            {
                await Awaitable.EndOfFrameAsync();
            }
            return _viewManager;
        }

        public static async Awaitable WaitForUltralight()
        {
            await UltralightManager.Instance.WaitForManager();
        }
    }
}