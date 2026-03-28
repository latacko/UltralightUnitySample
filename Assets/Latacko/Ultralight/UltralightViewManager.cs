using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UltralightSharedClasses.Classes;
using UltralightSharedClasses.StringHeaders;
using UltralightSharedClasses.Structs;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Latacko.UltralightUnity
{
    public unsafe class UltralightViewManager : IDisposable
    {
        readonly uint ID;

        const uint MAGIC = 0x6C617461;

        public uint Width;
        public uint Height;

        const int BUFS = 3;

        #region Events
        #pragma warning disable CS0067
        public delegate void OnTitleChangedHandler(string newTitle);
        public event OnTitleChangedHandler OnTitleChanged;
        public delegate void OnURLChangedHandler(string newUrl);
        public event OnURLChangedHandler OnURLChanged;
        public event Action OnBeginLoading;
        public event Action OnFinishLoading;
        public event Action OnDOMReady;
        public delegate void OnLoadFailedHandler(ulong frameId, bool isMainFramem, string url, string description, string errorDomain, int errorCode);
        public event OnLoadFailedHandler OnLoadFailed;
        public delegate void OnMessageConsoleHandler(ULMessageSource source, ULMessageLevel level, string message, uint line_number, uint column_number, string source_id);
        public event OnMessageConsoleHandler OnMessageConsole;
        public delegate void MessageEmittedEvent(string sender, string json);
        public event MessageEmittedEvent MessageEmitted;
        #pragma warning restore CS0067
        #endregion

        private readonly ViewHeader* header;
        private readonly byte* basePtr;
        private readonly int mouseOffset;
        private readonly int keyOffset;
        private readonly int textOffset;

        private readonly int resizeOffset;
        private readonly int loadOffset;
        private readonly int setUp_HTML_OR_URL_Offset;
        private readonly int messageConsoleOffset;
        private readonly int messageEmittedOffset;
        private readonly int postMessageOffset;
        private readonly int baseEventsOffset;
        private readonly int frameOffset;

        readonly MemoryMappedFile mmf;
        readonly MemoryMappedViewAccessor accessor;

        internal UltralightViewManager(uint id)
        {
            mmf = CreateMMF.OpenMemoryMappedFile(BASE_FILE_NAME.VIEW + id.ToString());

            accessor = mmf.CreateViewAccessor();

            basePtr = (byte*)accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
            int HeaderSize = Marshal.SizeOf<ViewHeader>();

            header = (ViewHeader*)basePtr;
            mouseOffset = HeaderSize;
            keyOffset = (int)header->keyOffset;
            textOffset = (int)header->textOffset;

            resizeOffset = (int)header->resizeOffset;
            loadOffset = (int)header->loadEventsOffset;
            setUp_HTML_OR_URL_Offset = (int)header->setupHTML_OR_URL_Offset;
            baseEventsOffset = (int)header->baseEventsOffset;
            messageConsoleOffset = (int)header->messageConsoleOffset;
            messageEmittedOffset = (int)header->messageEmittedOffset;
            postMessageOffset = (int)header->postMessageOffset;

            Width = header->width;
            Height = header->height;

            frameOffset = (int)header->frameOffset;

            if (header->magic != MAGIC)
                throw new Exception("MAGIC MISMATCH!");
        }

        public void PushMouseEvent(uint type, Vector3 pos)
        {
            try
            {
                int index = (int)(header->buttonEventWrite % ChunksData.MOUSE_EVENT_CHUNKS);

                if (index < 0 || index >= 128)
                {
                    return;
                }

                IntPtr addr = (IntPtr)(basePtr + mouseOffset + index * sizeof(MouseEvent));

                MouseEvent* ev = (MouseEvent*)(basePtr + mouseOffset + index * sizeof(MouseEvent));

                ev->type = type;
                ev->x = (int)pos.x;
                ev->y = (int)pos.y;

                Thread.MemoryBarrier();

                header->buttonEventWrite++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"PushMouseEvent crash: {ex}");
            }
        }

        public void PushKeyEvent(uint type, UnityEngine.KeyCode key, bool is_keypad)
        {
            int index = (int)(header->keyEventWrite % ChunksData.KEY_EVENT_CHUNKS);

            KeyEvent* ev = (KeyEvent*)(basePtr + keyOffset + index * sizeof(KeyEvent));

            ev->type = type;
            ev->key = key.ToUltralight();
            ev->is_keypad = is_keypad ? 1 : (uint)0;

            Thread.MemoryBarrier();

            header->keyEventWrite++;
        }

        public void PushCharEvent(char character)
        {
            if (character == '\0' || char.IsControl(character))
                return; // ignore null


            int index = (int)(header->inputTextEventWrite % ChunksData.TEXT_EVENT_CHUNKS);

            InputTextEvent* ev = (InputTextEvent*)(basePtr + textOffset + index * sizeof(InputTextEvent));

            ev->character = character;

            Thread.MemoryBarrier();

            header->inputTextEventWrite++;
        }

        public void Resize(uint width, uint height)
        {
            int index = (int)(header->resizeEventWrite % ChunksData.RESIZE_EVENT_CHUNKS);

            ResizeEvent* ev = (ResizeEvent*)(basePtr + resizeOffset + index * sizeof(ResizeEvent));

            ev->width = width;
            ev->height = height;

            Thread.MemoryBarrier();

            header->resizeEventWrite++;
        }

        internal void LoadAdvancedEvent()
        {
            while (header->loadEventsRead < header->loadEventsWrite)
            {
                int index = (int)(header->loadEventsRead % ChunksData.LOAD_EVENT_CHUNKS);
                LoadEventId* ev = (LoadEventId*)(basePtr + loadOffset + index * sizeof(LoadEventId));

                (var eventType, var headerObject, var stringList) = StringManager.ReadString(ev->id);

                if (eventType == UltralightSharedClasses.StringHeaders.EventType.LoadFailed && headerObject != null)
                {
                    LoadFieldHeader _header = (LoadFieldHeader)headerObject;
                    OnLoadFailed?.Invoke(_header.frameId, _header.isMainFrame == 1, stringList[0], stringList[1], stringList[2], (int)_header.errorCode);
                }
                else if (eventType == UltralightSharedClasses.StringHeaders.EventType.UrlChanged)
                {
                    OnURLChanged?.Invoke(stringList[0]);
                }

                Thread.MemoryBarrier();
                header->loadEventsRead++;
            }
        }

        void WriteSetUpEvent(uint id)
        {
            int index = (int)(header->setUpEventWrite % ChunksData.SETUP_HTML_OR_URL);

            LoadEventId* ev = (LoadEventId*)(basePtr + setUp_HTML_OR_URL_Offset + index * sizeof(LoadEventId));

            ev->id = id;

            Thread.MemoryBarrier();

            header->setUpEventWrite++;
        }

        public void LoadHTML(string html)
        {
            var _setUpHeader = new SetUpHTMLORURLHeader()
            {
                type = SetUpType.html
            };
            uint _id = StringManager.GenerateMMF<SetUpHTMLORURLHeader>(UltralightSharedClasses.StringHeaders.EventType.Set_HTML_OR_URL, _setUpHeader, html);
            WriteSetUpEvent(_id);
        }

        public void LoadURL(string url)
        {
            var _setUpHeader = new SetUpHTMLORURLHeader()
            {
                type = SetUpType.url
            };
            uint _id = StringManager.GenerateMMF<SetUpHTMLORURLHeader>(UltralightSharedClasses.StringHeaders.EventType.Set_HTML_OR_URL, _setUpHeader, url);
            WriteSetUpEvent(_id);
        }

        public int GetBufferSize()
        {
            return (int)header->bufferSize;
        }

        public Vector2Int GetFrameSize()
        {
            return new((int)header->width, (int)header->height);
        }

        public byte* GetFramePointer()
        {
            int readIndex = (int)header->writeIndex;
            return basePtr + frameOffset + (GetBufferSize() * readIndex);
        }

        public uint GetResizeCounter()
        {
            return header->resizeCounter;
        }

        /// <summary>
        /// Not working due to ultralight not working c api.
        /// Maybe will be fixed in future.
        /// </summary>
        [Obsolete("Not working due to ultralight not working c api. Maybe will be fixed in future")]
        public void OpenInspector()
        {
            header->openInspector = 1;
            Console.WriteLine("Requested inspector");
        }

        public void PostMessage(string json)
        {
            int index = (int)(header->postMessageEventWrite % ChunksData.POST_MESSAGE_CHUNKS);

            LoadEventId* ev = (LoadEventId*)(basePtr + postMessageOffset + index * sizeof(LoadEventId));

            uint _id = StringManager.GenerateMMF<EmptyHeader>(UltralightSharedClasses.StringHeaders.EventType.PostMessage, null, json);
            ev->id = _id;

            Thread.MemoryBarrier();

            header->postMessageEventWrite++;
        }

        public void ReadEmittedMessages()
        {
            while (header->messageEmittedEventRead < header->messageEmittedEventWrite)
            {
                int index = (int)(header->messageEmittedEventRead % ChunksData.MESSAGE_EMITTED_CHUNKS);
                LoadEventId* ev = (LoadEventId*)(basePtr + messageEmittedOffset + index * sizeof(LoadEventId));

                (var eventType, var headerObject, var stringList) = StringManager.ReadString(ev->id);
                MessageEmitted?.Invoke(stringList[0], stringList[1]);

                header->messageEmittedEventRead++;
            }
        }

        public void ReadMessageConsole()
        {
            while (header->messageConsoleEventRead < header->messageConsoleEventWrite)
            {
                int index = (int)(header->messageConsoleEventRead % ChunksData.MESSAGE_CONSOLE_CHUNKS);
                LoadEventId* ev = (LoadEventId*)(basePtr + messageConsoleOffset + index * sizeof(LoadEventId));

                (var eventType, var headerObject, var stringList) = StringManager.ReadString(ev->id);
                var _consoleHeader = (MessageConsoleHeader)headerObject;
                OnMessageConsole?.Invoke((ULMessageSource)_consoleHeader.source, (ULMessageLevel)_consoleHeader.level, stringList[0], _consoleHeader.line_number, _consoleHeader.column_number, stringList[1]);

                header->messageConsoleEventRead++;
            }
        }


        public void Dispose()
        {
            accessor?.Dispose();
            mmf?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}