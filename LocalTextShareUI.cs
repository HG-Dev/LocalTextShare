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
using System.Windows.Forms;


namespace LocalTextShare
{
    public partial class LocalTextShareUI : Form
    {
        private static TraceSource _source = new TraceSource("TestLog");
        private string _subtitleTemplate = File.ReadAllText(Application.StartupPath + "/WebServer/subtitle_content_template.txt");

        private bool isAcceptingConnections = false;
        private object syncLock = new object();
        private ReaderWriterLock rwl = new ReaderWriterLock();
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
            var url = "http://127.0.0.1:7070/";
            localServer.Prefixes.Add("http://localhost:7070/");
            localServer.Prefixes.Add(url);
            localServer.Start();
            _source.TraceInformation("Server has started");
            try
            {
                await ServerLoop();
            }
            catch (ObjectDisposedException d)
            {
                localServer = new HttpListener();
            }
            catch (Exception e)
            {
                _source.TraceEvent(TraceEventType.Error, 50, e.Message);
            }
        }

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
                var resPath = ctx.Request.Url.LocalPath;
                if (resPath.Length <= 1)
                    resPath = "/index.html";
                // Get local data as requested
                var page = Application.StartupPath + "/WebServer" + resPath;
                if (File.Exists(page))
                {
                    byte[] responseData;

                    // Acquire one-time access of HTML file
                    // TODO: Refactor this into form initialization, index.html shouldn't change
                    rwl.AcquireReaderLock(Timeout.Infinite);
                    responseData = File.ReadAllBytes(page);
                    rwl.ReleaseReaderLock();

                    var fileInfo = new FileInfo(page);
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
                        _source.TraceInformation("Sending page data to requester...");
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
                    _source.TraceEvent(TraceEventType.Error, 404, string.Format("Requested page ({0}) not found.", page));
                    // TODO: Implement
                }
            }
        }

        private void TestTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            _source.TraceInformation(e.KeyChar.ToString());
            UpdateBroadcastedText(testTextBox.Text);
        }

        /// <summary>
        /// Given a string, rewrites a small HTML file that is displayed as an iframe in index.html.
        /// </summary>
        /// <param name="myNewText">Next text to display in an iframe in index.html.</param>
        private void UpdateBroadcastedText(string myNewText)
        {
            rwl.AcquireWriterLock(Timeout.Infinite);
            _source.TraceInformation(myNewText);
            _source.TraceInformation(_subtitleTemplate);
            File.WriteAllText(Application.StartupPath + "/WebServer/subtitle_content.html", string.Format(_subtitleTemplate, myNewText, myNewText));
            rwl.ReleaseWriterLock();
            
        }
    }
}
