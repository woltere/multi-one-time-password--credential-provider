using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OTPServer.Agent
{
    class Agent
    {
        private static Agent __Instance = null;
        public static Agent Instance
        {
            get
            {
                if (__Instance == null)
                    __Instance = new Agent();
                return __Instance;
            }
        }

        private Agent()
        {
        }

        ~Agent()
        {
        }

        private static bool __Active;
        public static bool Active
        {
            get { return __Active; }
        }

        public bool Start()
        {
            return true;
        }

        public bool Stop()
        {
            return true;
        }
    }
}
