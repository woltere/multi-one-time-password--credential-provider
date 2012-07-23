using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace OTPServer
{
    public partial class OneTimePasswordServer : ServiceBase
    {        
        private static Authority.Authority __Authority = null;
        private static Agent.Agent         __Agent     = null;
        private static Server.Server       __Server    = null;

        private static int __Status = 0;
        public static int Status
        {
            get { return __Status; }
        }

        public static bool AuthorityOnline
        {
            get { return ((__Status & AUTHORITY_ONLINE) == AUTHORITY_ONLINE); }
        }

        public static bool AgentOnline
        {
            get { return ((__Status & AGENT_ONLINE) == AGENT_ONLINE); }
        }

        public static bool ServerOnline
        {
            get { return ((__Status & SERVER_ONLINE) == SERVER_ONLINE); }
        }

        private const int AUTHORITY_ONLINE = 1;
        private const int AGENT_ONLINE     = 2;
        private const int SERVER_ONLINE    = 4;

        private const int AUTHORITY_OFFLINE = -AUTHORITY_ONLINE - 1;
        private const int AGENT_OFFLINE     = -AGENT_ONLINE     - 1;
        private const int SERVER_OFFLINE    = -SERVER_ONLINE    - 1;
           
        public OneTimePasswordServer()
        {
            InitializeComponent();
            
            /*
            if (!System.Diagnostics.EventLog.SourceExists("OTPServerService"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "OTPServerService", "OTPServerLog");
            }
            eventLog.Source = "OTPServerService";
            eventLog.Log = "OTPServerLog";

            Authority.Authority.Logger = eventLog;
            Agent.Agent.Logger = eventLog;
            Server.Server.Logger = eventLog;
            */
        }

        protected override void OnStart(string[] args)
        {
            if (!AuthorityOnline)
            {
                __Authority = (__Authority == null) ? Authority.Authority.Instance : __Authority;
                if (__Authority.Start())
                    __Status |= AUTHORITY_ONLINE;
            }
            if (!AgentOnline)
            {
                __Agent = (__Agent == null) ? Agent.Agent.Instance : __Agent;
                if (__Agent.Start())
                    __Status |= AGENT_ONLINE;
            }
            if (!ServerOnline && AuthorityOnline && AgentOnline) // TODO: Abort start and <<<shutdown service>>> when Authority or Agent are not online!
            {
                __Server = (__Server == null) ? Server.Server.Instance : __Server;
                if (__Server.Start())
                    __Status |= SERVER_ONLINE;
            }
        }

        protected override void OnStop()
        {
            if (ServerOnline && __Server != null)
            {                
                __Server.Stop();
                __Server = null;
                __Status &= SERVER_OFFLINE;
            }
            if (AgentOnline && __Agent != null)
            {
                __Agent.Stop();
                __Agent = null;
                __Status &= AGENT_OFFLINE;
            }
            if (AuthorityOnline && __Authority != null)
            {
                __Authority.Stop();
                __Authority = null;
                __Status &= AUTHORITY_OFFLINE;
            }
        }
    }
}
