using UnityEngine;

namespace Latacko.UltralightUnity
{
    public class Message
    {
        public string action;
    }

    public class ExampleSiteController : MonoBehaviour
    {
        public ULView uLView;
        int count;
        async void Start()
        {
            await uLView.WaitUntilInitialized();
            uLView.WebView.LoadURL("file:///ExampleSite/index.html");
            uLView.WebView.MessageEmitted += MessageEmitted;
        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                uLView.WebView.PostMessage("{'clicked':"+(count++)+"}");
            }
        }

        private void MessageEmitted(string sender, string json)
        {
            var data = JsonUtility.FromJson<Message>(json);
            if (data.action == "exit")
            {
#if UNITY_STANDALONE
                Application.Quit();
#endif
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }


    }
}
