using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

if (args.Length != 2)
{
    Console.WriteLine("HotReload requires valid arguments.");
    Console.WriteLine("hr arg1 arg2");
    Console.WriteLine("- arg1 = Project file.");
    Console.WriteLine("- arg2 = Optional. Folder to monitor changes if provided, else will monitor the project folder only.");
    Console.WriteLine();
    return;
}

string project = args[0];
string projectPathName = Path.GetFullPath(project);
string projectPath = args[1] is null ? Path.GetDirectoryName(projectPathName)! :  Path.GetDirectoryName(args[1])!;

if (!Directory.Exists(projectPath))
{
    Console.WriteLine("Folder not found to monitor or is invalid.");
    return;
}

var httpListener = new HttpListener();
httpListener.Prefixes.Add("http://localhost:9000/");
httpListener.Start();
Console.WriteLine("WebSocket server started at ws://localhost:9000/");

WebSocket? webSocket = null;

FileSystemWatcher watcher = new()
{
    Path = projectPath,
    IncludeSubdirectories = true,
    EnableRaisingEvents = true,
    Filter = "*.*",
};


var startInfo = new ProcessStartInfo("dotnet")
{
    WorkingDirectory = projectPath,
    Arguments = $"run --project {projectPathName.Replace(projectPath, ".")} --verbosity quiet",    
};

var p = Process.Start(startInfo);

string[] ext = [".cs", ".razor", ".css"];

watcher.Changed += (s, e) => ProcessFile(e);
watcher.Renamed += (s, e) => ProcessFile(e);
watcher.Deleted += (s, e) => ProcessFile(e);

bool isCancelled = false;

Console.CancelKeyPress += (sender, e) => {    
    p?.Kill();
    isCancelled = true;
};

while (!isCancelled)
{
    HttpListenerContext context = await httpListener.GetContextAsync();

    if (context.Request.IsWebSocketRequest)
    {
        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
        webSocket = webSocketContext.WebSocket;
        await SendMessage();
    }
    else
    {
        context.Response.StatusCode = 400;
        context.Response.Close();
        Console.WriteLine("Received non-WebSocket request.");
    }
}

p?.Kill();

async void ProcessFile(FileSystemEventArgs e)
{    
    if (ext.Contains(Path.GetExtension(e.FullPath)))
    {
        if (Path.GetExtension(e.FullPath) != ".css")
        {
            p?.Kill();
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = projectPath,
                Arguments = $"run --project {projectPathName.Replace(projectPath, ".")} --verbosity quiet",
            };
            p = Process.Start(startInfo);        
        }
        await Refresh();
    }
}

async Task Refresh()
{
    Console.WriteLine("Refresing...");
    if (webSocket is null) return;
    byte[] responseBuffer = Encoding.UTF8.GetBytes("Refresh");
    await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
}

async Task SendMessage()
{
    byte[] buffer = new byte[1024 * 4];
    while (webSocket.State == WebSocketState.Open)
    {
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        else
            await Refresh();
    }
}