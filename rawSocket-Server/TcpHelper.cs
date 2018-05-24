using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.IO;

namespace rawSocket_Server
{
    class TcpHelper
    {
        private static ConcurrentDictionary<string, TcpClient> tcpClient = new ConcurrentDictionary<string, TcpClient>();
        private static TcpListener listener { get; set; }
        private static bool accept { get; set; }

        ~TcpHelper()
        {
            listener.Stop();
        }

        public static void StartServer(int port)
        {
            IPAddress address = IPAddress.Any;
            listener = new TcpListener(address, port);

            listener.Start();
            accept = true;

            Console.WriteLine($"Server started. Listening to TCP clients at {listener.LocalEndpoint}");  
        }

        public static async void Listen()
        {
            if(listener != null && accept)
            {
                Console.WriteLine("Waiting for client...");
                while(true)
                {
                    var clientTask = await listener.AcceptTcpClientAsync();

                    if(clientTask != null)
                    {
                        TcpHelper tcpHelper = new TcpHelper();
                        tcpHelper.handleConnection(clientTask);
                    }
                }


            }
        }

        public async void handleConnection(TcpClient client)
        {
            string socketID;
            while(true)
            {
                socketID = Guid.NewGuid().ToString();
                if(tcpClient.TryAdd(socketID, client))
                {
                    break;
                }
            }
            try
            {
                Console.WriteLine("Client connected. Waiting for message");
                string message = "";
                byte[] buffer = new byte[1024];
                byte[] data;
                while(message != null && !message.StartsWith("quit"))
                {
                    data = Encoding.ASCII.GetBytes("Send next data: [enter 'quit' to terminate] ");
                    await client.GetStream().WriteAsync(data, 0, data.Length);

                    Array.Clear(buffer, 0, buffer.Length);
                    await client.GetStream().ReadAsync(buffer, 0, buffer.Length);

                    foreach(var toClient in tcpClient)
                    {
                        if(!toClient.Key.Equals(socketID))
                            await toClient.Value.GetStream().WriteAsync(buffer, 0, buffer.Length);
                    }

                    message = Encoding.ASCII.GetString(buffer);
                    Console.WriteLine(message);
                }
            }
            catch(Exception ex) { 
                Console.WriteLine(ex.Message);
            }

            if(client.Connected)
            {
                client.GetStream().Dispose();
            }

            tcpClient.TryRemove(socketID, out client);
            Console.WriteLine("Closed client connection. Current Connection: " + tcpClient.Count);
        }
    }
}