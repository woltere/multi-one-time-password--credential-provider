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
using System.Collections;
using System.ComponentModel;
using Microsoft.Win32;

namespace InstallerActions
{
    [RunInstaller(true)]
    public partial class CustomActions : System.Configuration.Install.Installer
    {
        public CustomActions()
        {
            InitializeComponent();
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            try
            {                
                string installPath = Context.Parameters["targetdir"];

                RegistryKey otpServer = Registry.LocalMachine.OpenSubKey("SOFTWARE", true).CreateSubKey("blacksheep").CreateSubKey("OTPServer");
                otpServer.SetValue("path", installPath);
            }
            catch (Exception)
            {}
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);

            try
            {
                using (RegistryKey blacksheep = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("blacksheep", true))
                {
                    if (blacksheep != null)
                    {
                        using (RegistryKey otpServer = blacksheep.OpenSubKey("OTPServer"))
                        {
                            if (otpServer != null)
                                blacksheep.DeleteSubKey("OTPServer");
                        }
                    }
                }
            }
            catch (Exception)
            {}
        }

        [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand)]
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            try
            {
                using (RegistryKey blacksheep = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("blacksheep", true))
                {
                    if (blacksheep != null)
                    {
                        using (RegistryKey otpServer = blacksheep.OpenSubKey("OTPServer"))
                        {
                            if (otpServer != null)
                                blacksheep.DeleteSubKey("OTPServer");
                        }
                    }
                }
            }
            catch (Exception)
            {}
        }
    }
}
