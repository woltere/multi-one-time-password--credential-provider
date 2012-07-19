using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;

namespace OTPServer.Server
{
    public class Server
    {
        // TODO: Move to config
        public const int       PORT            = 16588;
        public const IPAddress IPADDR          = IPAddress.Any;
        public const int       CLIENT_MAX_AGE  = 1; // Minutes
        public const int       MAX_CONNECTIONS = 250;

        private static Dictionary<int, HandleClient> __ClientHandles;
        private Thread _MaintainingThread = null;
        private Thread _ListeningThread = null;

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
                    HandleClient client = new HandleClient(clientSocket);

                    lock (__ClientHandles)
                        __ClientHandles.Add(Now(), client);
                    
                    client.Start();
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
                // TODO: Wait/Hold when there are no Handles (AutoResetWait)
                Thread.Sleep(500);
                foreach (KeyValuePair<int, HandleClient> client in __ClientHandles)
                {
                    if (client.Value.Active == false)
                        lock (__ClientHandles)
                            __ClientHandles.Remove(client.Key);

                    if (Now() - client.Key > CLIENT_MAX_AGE * 60)
                    {
                        client.Value.Stop(true);

                        lock (__ClientHandles)
                            __ClientHandles.Remove(client.Key);
                    }
                }
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
