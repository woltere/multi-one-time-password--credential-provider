/* * * * * * * * * * * * * * * * * * * * *
**
** Copyright 2012 Dominik Pretzsch
** 
**    Licensed under the Apache License, Version 2.0 (the "License");
**    you may not use this file except in compliance with the License.
**    You may obtain a copy of the License at
** 
**        http://www.apache.org/licenses/LICENSE-2.0
** 
**    Unless required by applicable law or agreed to in writing, software
**    distributed under the License is distributed on an "AS IS" BASIS,
**    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
**    See the License for the specific language governing permissions and
**    limitations under the License.
**
** * * * * * * * * * * * * * * * * * * */

using System;
using System.Diagnostics;

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
