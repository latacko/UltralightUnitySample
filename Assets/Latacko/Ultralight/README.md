## WebView events
```c#
public delegate void OnTitleChangedHandler(string newTitle);
public event OnTitleChangedHandler OnTitleChanged;

public delegate void OnURLChangedHandler(string newUrl);
public event OnURLChangedHandler OnURLChanged;

public event Action OnBeginLoading;
public event Action OnFinishLoading;
public event Action OnDOMReady;

public delegate void OnLoadFailedHandler(ulong frameId, bool isMainFramem, string url, string description, string errorDomain, int errorCode);
public event OnLoadFailedHandler OnLoadFailed;

public delegate void OnMessageConsoleHandler(ULMessageSource source, ULMessageLevel level, string message, uint line_number, uint column_number, strinsource_id);
public event OnMessageConsoleHandler OnMessageConsole;

public delegate void MessageEmittedEvent(string sender, string json);
public event MessageEmittedEvent MessageEmitted;
```

## WebView functions
```c#
public void PostMessage(string json)
```

## ULView functions
```c#
public async Awaitable WaitUntilInitialized()
```

## WebView usage
```c#
async void Start()
{
    await GetComponent<ULView>().WaitUntilInitialized();
    GetComponent<ULView>().WebView.LoadHTML(html);
}
```

## ULManagerAPI
```c#
async ULManagerAPI.CreateView(width, height)
async ULManagerAPI.CreateView(width, height, isTransparent)
async ULManagerAPI.WaitForUltralight()
```


# Communication between c# and js
## c# to js
```c#
GetComponent<ULView>().WebView.PostMessage("{'name':'John', 'age':30, 'car':null}")
```

```js
window.addEventListener('ultralightmessage', (event)=>{
    const data = event.data
    console.log('Json received ' + JSON.stringify(data));
    if (data.name=='John'){
        console.log('Hi John!')
    }
})
```

## js to c#
```c#
GetComponent<ULView>().WebView.MessageEmitted += MessageEmitted;

private void MessageEmitted(string sender, string json)
{
    Debug.Log("Post message from " + sender + " json: "+ json);
}
```

```js
window.ultralight.postMessage("from_where", {
    test: "aaa",
    test2: "test123",
    test3: 123,
    test4: true,
    test5: false,
});
```
If window.ultralight is not set, wait for ultralightready message like:
```js
if (window.ultralight) {
    console.log("Ultralight is set. Sending message immediately");
    SendMessageToCSharp();
} else {
    console.log("Waiting for ultralight.");
    window.addEventListener("ultralightready", SendMessageToCSharp);
}

function SendMessageToCSharp() {
    window.ultralight.postMessage("test", {
        test: "aaa",
        test2: "test123",
        test3: 123,
        test4: true,
        test5: false,
    });
}
```

# Creating site
## Supported formats
- html, 
- js
- css
- png
- jpg
- jpeg
- svg

## Creating site
1. Create folder of site inside Sites folder
2. Right click -> Create -> Scriptable Objects -> Ultralight -> WebPageSO
3. Add there all site files
4. Add WebPageSO to UltralightManager instance in the scene in Pages
5. Open site with url: file:///SiteName/file.ext