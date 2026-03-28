using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using System;

namespace Latacko.UltralightUnity.Example
{
    public class ULInputSender : MonoBehaviour
    {
        ULView view;
        public float ScrollSensivity = 20;

        static readonly KeyCode[] allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        Vector3 lastmousePos;

        private void Awake()
        {
            view = GetComponent<ULView>();
        }

        void Start()
        {
        }

        void Update()
        {
            if (view.WebView == null)
                return;
            var pos = new Vector3(Input.mousePosition.x / Screen.width * view.Width, (Screen.height - Input.mousePosition.y) / Screen.height * view.Height);
            var scroll = Input.mouseScrollDelta;

            if (lastmousePos != pos)
            {
                lastmousePos = pos;

                view.WebView.PushMouseEvent(1, pos);
            }

            if (Input.GetMouseButtonDown(0))
                view.WebView.PushMouseEvent(2, pos);

            if (Input.GetMouseButtonUp(0))
                view.WebView.PushMouseEvent(3, pos);

            if (Input.GetMouseButtonDown(1))
                view.WebView.PushMouseEvent(4, pos);

            if (Input.GetMouseButtonUp(1))
                view.WebView.PushMouseEvent(5, pos);

            if (Input.GetMouseButtonDown(2))
                view.WebView.PushMouseEvent(6, pos);

            if (Input.GetMouseButtonUp(2))
                view.WebView.PushMouseEvent(7, pos);

            if (scroll.sqrMagnitude > 0f)
                view.WebView.PushMouseEvent(8, new((int)(scroll.x * ScrollSensivity), (int)(scroll.y * ScrollSensivity)));

            foreach (KeyCode key in allKeys)
            {
                // skip mouse buttons (they are handled separately)
                if ((int)key >= (int)KeyCode.Mouse0)
                    continue;

                if (Input.GetKeyDown(key))
                {
                    view.WebView.PushKeyEvent(1, key, IsKeypad(key));
                }

                if (Input.GetKeyUp(key))
                {
                    view.WebView.PushKeyEvent(2, key, IsKeypad(key));
                }
            }

            if (!string.IsNullOrEmpty(Input.inputString))
            {
                foreach (char c in Input.inputString)
                {
                    view.WebView.PushCharEvent(c);
                }
            }
        }

        bool IsKeypad(KeyCode key)
        {
            return key >= KeyCode.Keypad0 && key <= KeyCode.KeypadEquals;
        }
    }
}