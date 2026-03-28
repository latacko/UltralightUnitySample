using UnityEngine;

namespace Latacko.UltralightUnity.Example
{
    [RequireComponent(typeof(ULView))]
    public class ULViewLoadHtml : MonoBehaviour
    {
        [Multiline(50)]
        public string html;
        async void Start()
        {
            await GetComponent<ULView>().WaitUntilInitialized();
            GetComponent<ULView>().WebView.LoadHTML(html);
        }

    }
}
