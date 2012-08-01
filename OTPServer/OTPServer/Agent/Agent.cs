using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using OTPServer.Communication.Local;
using OTPServer.Communication.Local.Observer;
using OTPHelpers.XML.OTPPacket;
using System.Threading;
using System.IO;

namespace OTPServer.Agent
{
    static class Agent
    {
        private static string _MultiOtpPath = String.Empty;

        static Agent()
        {
            _MultiOtpPath = Configuration.Instance.GetStringValue("multiOtpPath");
        }

        /*
        private static bool __Active;
        public static bool Active
        {
            get { return __Active; }
        }
        */

        public static bool VerifyOneTimePassword(string username, string[] otp)
        {
            if (_MultiOtpPath != String.Empty)
            {
                string args = "-log " + username + " " + otp[0];
                using (Process exeProcess = Process.Start(_MultiOtpPath, args))
                {
                    exeProcess.WaitForExit();
                    if (exeProcess.ExitCode == 0)
                        return true;
                }                
            }
            return false;
        }

        public static bool ResyncOneTimePassword(string username, string[] otp)
        {
            if (_MultiOtpPath != String.Empty)
            {
                string args = "-log -resync " + username + " " + otp[0] + " " + otp[1];
                using (Process exeProcess = Process.Start(_MultiOtpPath, args))
                {
                    exeProcess.WaitForExit();
                    if (exeProcess.ExitCode == 14)
                        return true;
                }
            }
            return false;
        }
    }
}
