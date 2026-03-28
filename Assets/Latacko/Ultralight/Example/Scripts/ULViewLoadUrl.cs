using UnityEngine;

namespace Latacko.UltralightUnity.Example
{
    [RequireComponent(typeof(ULView))]
    public class ULViewLoadUrl : MonoBehaviour
    {
        public string url;
        async void Start()
        {
            await GetComponent<ULView>().WaitUntilInitialized();
            GetComponent<ULView>().WebView.LoadURL(url);
        }

    }
}
