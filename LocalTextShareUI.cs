using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.Windows.Forms;


namespace LocalTextShare
{
    public partial class LocalTextShareUI : Form
    {
        private static TraceSource _source = new TraceSource("TestLog");
        private List<WebSocket> openSockets = new List<WebSocket>();
        private object syncLock = new object();
        private bool isAcceptingConnections;
        private int count = 0;
        HttpListener localServer;

        public LocalTextShareUI()
        {
            _source.TraceInformation("Starting up form components");
            InitializeComponent();
            StartServer();
        }

        private async void StartServer()
        {
            _source.TraceInformation("Preparing server environment...");
            await AddFirewallRule();
            _source.TraceInformation("Starting up server...");
            localServer = new HttpListener();
            localServer.Prefixes.Add("http://localhost:80/");
            localServer.Prefixes.Add("http://192.168.123.61:80/");
            localServer.Start();
            _source.TraceInformation("Server has started");

            try
            {
                await ServerLoop();
            }
            catch (ObjectDisposedException d)
            {
                localServer = new HttpListener();
                _source.TraceEvent(TraceEventType.Error, 40, d.Message);
            }
            catch (Exception e)
            {
                _source.TraceEvent(TraceEventType.Error, 50, e.Message);
            }
        }

        //TODO modify firewall rules programmatically
        private Task AddFirewallRule()
        {
            return Task.Run(() =>
            {
                string cmd = RunCMD("netsh advfirewall firewall show rule \"Text Broadcast\"");
                if (cmd.StartsWith("\r\nNo rules match the specified criteria."))
                {
                    cmd = RunCMD("netsh advfirewall firewall add rule name=\"Text Broadcast\" dir=in action=allow remoteip=localsubnet protocol=tcp localport=7070");
                    if (cmd.Contains("Ok."))
                    {
                        _source.TraceInformation("Firewall rule \"Text Broadcast\" was added.");
                    }
                    else
                    {
                        _source.TraceEvent(TraceEventType.Error, 551, cmd);
                    }
                }
                else
                {
                    _source.TraceInformation("Firewall rule \"Text Broadcast\" already exists.");
                }
            });   
        }

        private string RunCMD(string cmd)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/C " + cmd;
            proc.StartInfo.UseShellExecute = false; // This is required to redirect outputs
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();
            string res = proc.StandardOutput.ReadToEnd();
            proc.StandardOutput.Close();

            proc.Close();
            return res;
        }

        private async Task ServerLoop()
        {
            isAcceptingConnections = true;
            while (isAcceptingConnections)
            {
                // Wait for next request
                _source.TraceInformation("Awaiting request...");
                var ctx = await localServer.GetContextAsync();
                _source.TraceInformation("Request receieved.");

                if (ctx.Request.IsWebSocketRequest)
                {
                    _source.TraceInformation("Got WS request");
                    // Create WebSocket
                    WebSocketContext webSocketContext = null;
                    try
                    {
                        webSocketContext = await ctx.AcceptWebSocketAsync(subProtocol: null);
                        Interlocked.Increment(ref count);
                        _source.TraceInformation("Processed: " + count.ToString());
                    }
                    catch (Exception e)
                    {
                        ctx.Response.StatusCode = 500;
                        ctx.Response.Close();
                        _source.TraceEvent(TraceEventType.Error, 500, e.Message);
                    }
                    if (ctx.Response.StatusCode != 500)
                    {
                        WebSocket webSocket = webSocketContext.WebSocket;

                        try
                        {
                            // Used to close WebSocket only in this case
                            byte[] receiveBuffer = new byte[1024];
                            while (webSocket.State == WebSocketState.Open)
                            {
                                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                                if (receiveResult.MessageType == WebSocketMessageType.Close)
                                {
                                    _source.TraceInformation("Closing WebSocket");
                                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                                }
                                else if (receiveResult.MessageType == WebSocketMessageType.Text)
                                {
                                    openSockets.Add(webSocket);
                                    ArraySegment<byte> message = new ArraySegment<byte>(Encoding.UTF8.GetBytes(
                                        "This was a triumph"
                                    ));
                                    await webSocket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // Research notes: any error that occurs during Close / Send / Receive will mess up the WebSocket
                            _source.TraceEvent(TraceEventType.Error, 544, e.Message);
                        }
                        finally
                        {
                            if (webSocket != null)
                                openSockets.Remove(webSocket);
                                webSocket.Dispose();
                        }
                    }
                }
                else
                {
                    // Deliver file data

                    var resPath = ctx.Request.Url.LocalPath;
                    if (resPath.Length <= 1)
                        resPath = "/index.html";

                    var file = Application.StartupPath + "/WebServer" + resPath;
                    if (File.Exists(file))
                    {
                        byte[] responseData;

                        // Acquire one-time access of HTML file
                        // TODO: Refactor this into form initialization, index.html shouldn't change
                        responseData = File.ReadAllBytes(file);

                        var fileInfo = new FileInfo(file);
                        // Define file types for finicky browsers
                        switch (fileInfo.Extension)
                        {
                            case ".css":
                                ctx.Response.ContentType = "text/css";
                                break;
                            case ".html":
                            case ".htm":
                                ctx.Response.ContentType = "text/html";
                                break;
                        }

                        ctx.Response.StatusCode = 200;

                        try
                        {
                            _source.TraceInformation("Sending file data to requester...");
                            await ctx.Response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
                        }
                        catch (Exception e)
                        {
                            _source.TraceEvent(TraceEventType.Error, 555, e.Message);
                        }

                        ctx.Response.Close();
                        _source.TraceInformation("Response complete.");

                    }
                    else
                    {
                        // Explain (politely) that the WebServer folder is improperly configured.
                        _source.TraceEvent(TraceEventType.Error, 404, string.Format("Requested file ({0}) not found.", file));
                        // TODO: Implement
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="myNewText"></param>
        private void BroadcastText(string myNewText)
        {
            ArraySegment<byte> message = new ArraySegment<byte>(
                Encoding.UTF8.GetBytes(myNewText)
            );
            foreach (WebSocket ws in openSockets)
            {
                try
                {
                    ws.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception e)
                {
                    _source.TraceEvent(TraceEventType.Error, 536, e.Message);
                }                
            }
        }

        private void testTextBox_TextChanged(object sender, EventArgs e)
        {
            BroadcastText(testTextBox.Text);
        }
    }
}
