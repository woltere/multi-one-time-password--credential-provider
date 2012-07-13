using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;

namespace OTPServer.Server
{
    public class Server
    {
        public const int PORT = 16588;
        public const IPAddress IPADDR = IPAddress.Any;

        private int _ConnectionCount;
        public int ConnectionCount
        {
            get { return this._ConnectionCount; }
        }

        private bool _Active;
        public bool Active
        {
            get { return this._Active; }
        }

        public Server()
        {
            this._ConnectionCount = 0;
            this._Active = false;
        }

        ~Server()
        {
            this._ConnectionCount = 0;
            this._Active = false;
        }

        public bool Start()
        {
            this._Active = true;
            Listen();
            return true;
        }

        public bool Stop()
        {
            this._Active = false;
            return true;
        }

        public void Listen()
        {
            // TODO: Get IP and port from a config (or Authority?)
            TcpListener listener = new TcpListener(IPADDR, PORT);
            listener.Start();

            while (Active)
            {
                // TODO: Check Active state in frequent intervall (avoid blocking forever, when shutting down)
                TcpClient clientSocket = listener.AcceptTcpClient();
                HandleClient client = new HandleClient(clientSocket, this._ConnectionCount++);
                client.Start();
            }
        }
    }
}
