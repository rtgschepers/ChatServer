using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ChatServer
{
    class Program
    {
        static bool serverIsRunning;
        static TcpListener server;
        public static List<Client> chatList = new List<Client>();
        public static CyloChat_DBEntities db = new CyloChat_DBEntities();

        static void Main(string[] args)
        {
            foreach (var row in db.users)
            {
                Debug.WriteLine(row.username);
            }


            int port = 1337;
            serverIsRunning = false;
            IPAddress IP = IPAddress.Any;
            try
            {
                //Start the server with the IP and port defined above.
                server = new TcpListener(IP, port);
                server.Start();
                serverIsRunning = true;
                Console.WriteLine("[Server] Server has started successfully.\n[Server] Listening for clients...\n"); //Message on succes.
                //Start method for accepting clients.
                Task.Factory.StartNew(ListenForClients);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] {0}", ex.ToString());
            }
            do
            {
                string command = Console.ReadLine().ToLower();
                if (command == "/stop")
                    serverIsRunning = false;
                else if (command == "/clear")
                    Console.Clear();
            } while (serverIsRunning);
        }

        static async void ListenForClients()
        {
            while (serverIsRunning)
            {
                Socket socket = await server.AcceptSocketAsync();
                if (socket == null)
                    break;
                Console.WriteLine("[Server] Client connected with hash {0}!", socket.GetHashCode());
                Client newClient = new Client(socket, socket.GetHashCode());
            }
        }
    }
}
