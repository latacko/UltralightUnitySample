console.log("test123");

window.addEventListener("ultralightmessage", (event) => {
    const data = event.data
    document.getElementById("clicked").innerText = "Clicked: "+data.clicked+" times"
})

function ExitGame() {
    window.ultralight.postMessage("example_site", { action: "exit" });
}