using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Threading;

namespace OTPServerConfigurationUtility
{
    public partial class MainForm : Form
    {
        private int _ServiceStatus = 0;
        private ServiceController _Service = null;

        public MainForm()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            string serialNumber = Configuration.Instance.GetStringValue("serverCertificate");
            if (serialNumber != String.Empty)
                edit_serialNumber.Text = serialNumber;

            SetCertificateInfoGUI(serialNumber);
            SetServiceStatusGUI();

            ActiveControl = label1;
        }

        private void SetServiceStatusGUI()
        {
            _ServiceStatus = GetServiceStatus();
            if (_ServiceStatus == 1)
            {
                label_status.Text = "Running";
                label_status.ForeColor = Color.OliveDrab;
                btStart.Enabled = false;
                btStop.Enabled = true;
            }
            else if (_ServiceStatus == 0)
            {
                label_status.Text = "Stopped";
                label_status.ForeColor = Color.Crimson;
                btStart.Enabled = true;
                btStop.Enabled = false;
            }
            else
            {
                label_status.Text = "Not installed!";
                label_status.ForeColor = Color.Crimson;
                btStart.Enabled = false;
                btStop.Enabled = false;
                btChoose.Enabled = false;
                edit_serialNumber.Enabled = false;
                edit_serialNumber.Text = "Service is not installed";
                btRestore.Enabled = false;
                btSave.Enabled = false;
            }
            ActiveControl = label1;
        }

        private void btChoose_Click(object sender, EventArgs e)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
            collection = X509Certificate2UI.SelectFromCollection(collection, "Select a certificate to use with OTP server", "Select a certificate from the following list to use it with OTP server.", X509SelectionFlag.SingleSelection);
            if (collection.Count == 1)
            {
                edit_serialNumber.Text = collection[0].GetSerialNumberString();
                SetCertificateInfoGUI(collection[0].GetSerialNumberString());
            }
        }

        private void SetCertificateInfoGUI(string serialNumber)
        {
            if (serialNumber != null)
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                collection = collection.Find(X509FindType.FindBySerialNumber, serialNumber, false);
                if (collection.Count == 1)
                {
                    label_certNameValue.Text = collection[0].FriendlyName;
                    label_certAuthValue.Text = collection[0].Issuer.Substring(collection[0].Issuer.IndexOf("CN="));
                    return;
                }
            }
            label_certNameValue.Text = "N/A";
            label_certAuthValue.Text = "N/A";
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            if (Configuration.Instance.SetValue("serverCertificate", edit_serialNumber.Text))
                MessageBox.Show("Configuration saved successfully", "Saving configuration...", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Configuration could not be saved", "Saving configuration...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Initialize();
        }

        private void btRestore_Click(object sender, EventArgs e)
        {
            Initialize();
        }

        private int GetServiceStatus()
        {         
            if (_Service == null)
            {
                ServiceController[] scServices;
                scServices = ServiceController.GetServices();

                foreach (ServiceController scTemp in scServices)
                {
                    if (scTemp.ServiceName == "OneTimePasswordServer")
                    {
                        _Service = new ServiceController("OneTimePasswordServer");
                        if (_Service.Status == ServiceControllerStatus.Stopped)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
            }
            else
            {
                if (_Service.Status == ServiceControllerStatus.Stopped)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            return -1;
        }

        private void ServiceStart()
        {
            _Service.Start();
            while (_Service.Status == ServiceControllerStatus.Stopped)
            {
                Thread.Sleep(100);
                _Service.Refresh();
            }
            SetServiceStatusGUI();
        }

        private void ServiceStop()
        {
            _Service.Stop();
            while (_Service.Status != ServiceControllerStatus.Stopped)
            {
                Thread.Sleep(100);
                _Service.Refresh();
            }
            SetServiceStatusGUI();
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            ServiceStart();
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            ServiceStop();
        }
    }
}
