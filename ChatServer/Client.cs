using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChatServer
{
    class Client
    {
        Socket client;
        public NetworkStream stream;
        public int hash { get; set; }
        public string username { get; set; }

        public Client(Socket _client, int _hash)
        {
            client = _client;
            hash = _hash;
            stream = new NetworkStream(client, true);
            Task.Factory.StartNew(ReceiveData);
        }

        public void Dispose()
        {
            client.Close();
            stream.Dispose();
            Program.chatList.Remove(this);
            foreach(Client user in Program.chatList)
                user.SendUserList(Program.chatList);
            Console.WriteLine("[Server] Client disconnected with hash {0}.", hash);
        }

        async void ReceiveData()
        {
            while (client.Connected)
            {
                try
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (data.Substring(0, 3) == "cht")
                    {
                        string message = data.Substring(4);
                        message = string.Format("[{0}] {1}: {2}", DateTime.Now.ToString("HH:mm"), username, message);
                        Console.WriteLine(message);
                        foreach(Client user in Program.chatList)
                            user.SendChat(message);
                    }
                    else if (data.Substring(0, 3) == "lin")
                    {
                        string[] info = data.Split('/');
                        HandleLogin(info[1], info[2]);
                    }
                    else if (data.Substring(0, 3) == "out")
                    {
                        string[] info = data.Split('/');
                        HandleLogout(info[1]);
                    }
                    else if (data.Substring(0, 3) == "reg")
                    {
                        string[] info = data.Split('/');
                        HandleRegistration(info[1], info[2]);
                    }
                    else if (data.Substring(0, 3) == "cpw")
                    {
                        string[] info = data.Split('/');
                        HandleChangePassword(info[1]);
                    }
                    
                }
                catch (Exception)
                {
                    this.Dispose();
                }
            }
        }

        private void HandleChangePassword(string _password)
        {
            var user = Program.db.users.SingleOrDefault(u => u.username == username);
            user.password = _password;
            Program.db.SaveChanges();
        }

        async void HandleLogin(string _username, string _password)
        {
            string loginCheck = "neg";
            user row = Program.db.users.SingleOrDefault(u => u.username == _username);
            if (row != null)
                if (row.password == _password)
                {
                    var user = Program.chatList.SingleOrDefault( u=> u.username == _username);
                    if (user == null)
                    {
                        loginCheck = "pos";
                        Console.WriteLine("[Chat] Client {0} joined chat with username: {1}", hash, _username);
                        this.username = _username;
                        Program.chatList.Add(this);
                    }
                }
            byte[] buffer = Encoding.ASCII.GetBytes(loginCheck);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            await stream.FlushAsync();

            if (loginCheck == "pos")
            {
                foreach (Client user in Program.chatList)
                    user.SendUserList(Program.chatList);
            }
        }

        void HandleLogout(string _username)
        {
            Program.chatList.Remove(this);
            foreach (Client user in Program.chatList)
                user.SendUserList(Program.chatList);
            Console.WriteLine("[Chat] Client {0} disconnected from chat with username: {1}.", hash, _username);
        }

        async void SendChat(string _message)
        {
            stream.Flush();
            byte[] buffer = Encoding.ASCII.GetBytes(string.Format("cht/{0}", _message));
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        async void HandleRegistration(string _username, string _password)
        {
            string registerCheck = "neg";
            user row = Program.db.users.SingleOrDefault(r => r.username == _username);
            if (row == null)
            {
                registerCheck = "pos";
                user newUser = new user();
                newUser.username = _username;
                newUser.password = _password;
                Program.db.users.Add(newUser);
                await Program.db.SaveChangesAsync();
                Console.WriteLine("[Regist] Client {0} registered username: {1}", hash, _username);
            }
            byte[] buffer = Encoding.ASCII.GetBytes(registerCheck);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            await stream.FlushAsync();
        }

        async void SendUserList(List<Client> ChatList)
        {
            string usersnames = "usr/";
            foreach (Client user in Program.chatList)
                usersnames += user.username + "/";
            byte[] buffer = Encoding.ASCII.GetBytes(usersnames);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            await stream.FlushAsync();
        }
    }
}
