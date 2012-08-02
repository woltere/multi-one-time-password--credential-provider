using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using Microsoft.Win32;
using System.IO;

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
                string otpServerAddr = Context.Parameters["otpsrvaddr"];
                string installPath = Context.Parameters["targetdir"];

                RegistryKey otpClient = Registry.LocalMachine.OpenSubKey("SOFTWARE", true).CreateSubKey("blacksheep").CreateSubKey("OTPClient");
                otpClient.SetValue("OTPServerAddr", otpServerAddr);
                otpClient.SetValue("path", installPath);
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
                        using (RegistryKey otpClient = blacksheep.OpenSubKey("OTPClient"))
                        {
                            if (otpClient != null)
                                blacksheep.DeleteSubKey("OTPClient");
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
                        using (RegistryKey otpClient = blacksheep.OpenSubKey("OTPClient"))
                        {
                            if (otpClient != null)
                                blacksheep.DeleteSubKey("OTPClient");
                        }
                    }
                }
            }
            catch (Exception)
            {}
        }
    }
}
