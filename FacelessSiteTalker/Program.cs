using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;

namespace FacelessSiteTalker
{
    // Credit to https://eecs.blog/using-websockets-in-c/ for the code for the server.
    class Program
    {
        static SystemManager sysManager = null;
        
        static async Task Main(string[] args)
        {
            sysManager = GetSystemManager();
            sysManager.ShowWindow(false);
            sysManager.Dock();
            await RunServer();
        }

        static SystemManager GetSystemManager()
        {
            #if WINDOWS
                return new WindowsSystemManager();
            #else
                return new SystemManager();
            #endif
        }

        static async Task RunServer()
        {
            //Define the URL for the WebSocket server.
            string serverUrl = "http://localhost:8080/ws/";
            //Create and start a new HttpListener on the specified URL.
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(serverUrl);
            listener.Start();
            Console.WriteLine($"WebSocket Server started on {serverUrl}");
            //Keep listening for incoming connections forever/until the server is stopped.
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                //Handle each connection asynchronously. The _ is just a temporary variable that will get discarded.
                _ = Task.Run(() => HandleWebSocketConnection(context));
            }
        }

        private static async Task HandleWebSocketConnection(HttpListenerContext context)
        {
            UInt32 notificationId = 0;
            //As this is a WebSocket server we'll check if the incoming request is a WebSocket request.
            //If not we'll, return a 400 Bad Request response and close the connection.
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            //Accept the WebSocket connection and get the WebSocket from it.
            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
            WebSocket webSocket = wsContext.WebSocket;
            Console.WriteLine("WebSocket connection established.");
            //Create a buffer to store the received message from the WebSocket into it.
            var receiveBuffer = new byte[1024];
            WebSocketReceiveResult result =
                await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
            //Keep receiving messages from the WebSocket until the connection is closed.
            while (!result.CloseStatus.HasValue)
            {
                //Convert the received message to a string and display it to the server console.
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                Console.WriteLine($"Received: {receivedMessage}");
                if(sysManager!=null)
                    sysManager.SendNotification("Notification!", receivedMessage, "showNotification", notificationId.ToString());

                if (receivedMessage == "show")
                {
                    sysManager.ShowWindow(true);
                    sysManager.ChangeIcon("TrayIcon");
                }

                if (receivedMessage == "hide")
                {
                    sysManager.ShowWindow(false);
                    sysManager.ChangeIcon("GreenIcon");
                }

                notificationId++;
                //Send back the received message.
                byte[] sendMessage = Encoding.UTF8.GetBytes($"Server echo: {receivedMessage}");
                await webSocket.SendAsync(new ArraySegment<byte>(sendMessage), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                //Setup the receive task to receive the next message from the WebSocket.
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
            }

            //Close the WebSocket connection after we are done.
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            Console.WriteLine("WebSocket connection closed");
        }
    }
}