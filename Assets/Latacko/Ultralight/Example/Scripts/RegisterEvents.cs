using System;
using UnityEngine;

namespace Latacko.UltralightUnity.Example
{
    public class RegisterEvents : MonoBehaviour
    {
        public ULView receiver;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        async void Start()
        {
            await receiver.WaitUntilInitialized();
            receiver.WebView.OnMessageConsole += OnMessageConsole;
            receiver.WebView.MessageEmitted += MessageEmitted;
            receiver.WebView.OnLoadFailed += OnLoadFailed;
            receiver.WebView.OnURLChanged += OnUrlChanged;
            receiver.WebView.OnDOMReady += OnDOMReady;
        }

        private void OnDOMReady()
        {
            Debug.Log("DOM ready");
        }

        private void OnMessageConsole(ULMessageSource source, ULMessageLevel level, string message, uint line_number, uint column_number, string source_id)
        {
            Debug.Log(source + " |" + level + "| Message: " + message + " Line number " + line_number + " Column number " + column_number + " Source id " + source_id);
        }

        private void MessageEmitted(string sender, string json)
        {
            Debug.Log("Post message from " + sender + " json: " + json);
        }

        void OnLoadFailed(ulong frameid, bool isMainFrame, string url, string description, string errorDomain, int errorCode)
        {
            Debug.LogError(url + " " + description + " " + errorCode);
        }

        private void OnUrlChanged(string newUrl)
        {
            Debug.Log("Site changed " + newUrl, gameObject);
        }
    }
}
