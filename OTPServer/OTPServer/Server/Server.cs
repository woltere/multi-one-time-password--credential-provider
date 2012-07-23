using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace OTPServer.Server
{
    public class Server
    {
        // TODO: Move to config
        public const int PORT = 16588;
        public const int       CLIENT_MAX_AGE  = 1; // Minutes
        public const int       MAX_CONNECTIONS = 250;

        public static EventLog Logger;

        private static Dictionary<int, HandleClient> __ClientHandles;
        private Thread _MaintainingThread = null;
        private Thread _ListeningThread = null;

        private static IPAddress __IPADDR = null;
        public static IPAddress IPADDR
        {
            get { return __IPADDR; }
        }

        public static int ConnectionCount
        {
            get { return __ClientHandles.Count; }
        }

        private static bool __Active;
        public static bool Active
        {
            get { return __Active; }
        }

        private static Server __Instance = null;
        public static Server Instance
        {
            get
            {
                if (__Instance == null)
                    __Instance = new Server();
                return __Instance;
            }
        }

        private Server()
        {
            __IPADDR = IPAddress.Any;

            __Active = false;
            if (__ClientHandles == null)
                __ClientHandles = new Dictionary<int, HandleClient>();            
        }

        ~Server()
        {
            __Active = false;
            __ClientHandles = null;

            _ListeningThread = null;
            _MaintainingThread = null;
        }

        public bool Start()
        {
            __Active = true;

            Listen(false);
            MaintainClientHandles(false);

            return true;
        }

        public bool Stop()
        {
            __Active = false;

            if (_MaintainingThread != null)
                _MaintainingThread.Interrupt();

            if (_ListeningThread != null)
                _ListeningThread.Interrupt();

            return true;
        }

        private void Listen()
        {
            Listen(true);
        }

        private void Listen(bool isThread)
        {
            TcpListener listener;

            if (!isThread && _ListeningThread == null)
            {                
                _ListeningThread = new Thread(Listen);
                _ListeningThread.Start();
                return;
            }
            else
            {
                listener = new TcpListener(IPADDR, PORT);
                listener.Start();
            }

            while (Active && isThread)
            {
                if (ConnectionCount <= MAX_CONNECTIONS)
                {
                    // TODO: Check Active state in frequent intervall (avoid blocking forever, when shutting down). BeginAcceptTcpClient() (asynchronous)?
                    TcpClient clientSocket = listener.AcceptTcpClient();

                    using (HandleClient client = new HandleClient(clientSocket))
                    {
                        lock (__ClientHandles)
                            __ClientHandles.Add(Now(), client);

                        client.Start();
                    }
                }
            }
        }

        private void MaintainClientHandles()
        {
            MaintainClientHandles(true);
        }

        private void MaintainClientHandles(bool isThread)
        {
            if (!isThread && _MaintainingThread == null)
            {
                _MaintainingThread = new Thread(MaintainClientHandles);
                _MaintainingThread.Start();
                return;
            }

            while (Active && isThread)
            {
                try
                {
                    Thread.Sleep(500);

                    // TODO: Wait/Hold when there are no Handles (AutoResetWait)
                    while (__ClientHandles.Count <= 0)
                        Thread.Sleep(500);

                        List<KeyValuePair<int, HandleClient>> tempList;
                        lock (__ClientHandles)
                            tempList = new List<KeyValuePair<int, HandleClient>>(__ClientHandles);

                        foreach (KeyValuePair<int, HandleClient> client in tempList)
                        {
                            if (client.Value.Active == false)
                            {
                                File.AppendAllText("C:\\maintainer.log", "client.Value.Active == false;\n");
                                lock (__ClientHandles)
                                    __ClientHandles.Remove(client.Key);
                            }

                            if (Now() - client.Key > CLIENT_MAX_AGE * 60)
                            {
                                File.AppendAllText("C:\\maintainer.log", "Now() - client.Key > CLIENT_MAX_AGE * 60;\n");
                                client.Value.Stop(true);
                                lock (__ClientHandles)
                                    __ClientHandles.Remove(client.Key);
                            }
                        }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception)
                { }
            }
        }

        private static int Now()
        {
            DateTime epochStart = new DateTime(1970, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan ts = new TimeSpan(now.Ticks - epochStart.Ticks);
            return (Convert.ToInt32(ts.TotalSeconds));
        }
    }
}
