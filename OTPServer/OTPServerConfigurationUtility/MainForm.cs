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
            string certThumbPrint = Configuration.Instance.GetStringValue("serverCertificate");
            if (certThumbPrint != String.Empty)
                edit_certThumbprint.Text = certThumbPrint;

            string authorityThumbPrint = Configuration.Instance.GetStringValue("authorityCertificate");
            if (authorityThumbPrint != String.Empty)
                edit_authorityThumbprint.Text = authorityThumbPrint;

            SetCertificateInfoGUI(ref label_certNameValue, ref label_certAuthValue, certThumbPrint);
            SetCertificateInfoGUI(ref label_authorityCertNameValue, ref label_authorityCertAuthValue, authorityThumbPrint);

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

                btChooseSSLCertificate.Enabled = false;
                edit_certThumbprint.Enabled = false;
                edit_certThumbprint.Text = "Service is not installed";

                btChooseAuthorityCertificate.Enabled = false;
                edit_authorityThumbprint.Enabled = false;
                edit_authorityThumbprint.Text = "Service is not installed";

                btChooseMultiOTP.Enabled = false;
                edit_multiOtpPath.Enabled = false;
                edit_multiOtpPath.Text = "Service is not installed";

                btRestore.Enabled = false;

                btSave.Enabled = false;
            }
            ActiveControl = label1;
        }

        private void SetCertificateInfoGUI(ref Label labelName, ref Label labelAuth, string thumbPrint)
        {
            if (thumbPrint != null && thumbPrint != String.Empty)
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                collection = collection.Find(X509FindType.FindByThumbprint, thumbPrint, false);
                if (collection.Count == 1)
                {
                    labelName.Text = collection[0].Subject.Substring(collection[0].Subject.IndexOf("CN="));
                    labelAuth.Text = collection[0].Issuer.Substring(collection[0].Issuer.IndexOf("CN="));
                    return;
                }
            }
            labelName.Text = "N/A";
            labelAuth.Text = "N/A";
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            if (Configuration.Instance.SetValue("serverCertificate", edit_certThumbprint.Text)
                && Configuration.Instance.SetValue("authorityCertificate", edit_authorityThumbprint.Text)
                && Configuration.Instance.SetValue("multiOtpPath", edit_multiOtpPath.Text))
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

        private void btChooseSSLCertificate_Click(object sender, EventArgs e)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
            collection = X509Certificate2UI.SelectFromCollection(collection, "Select a SSL/TLS server authentication certificate to use with OTP server", "Select a certificate from the following list to use it for server authentication.", X509SelectionFlag.SingleSelection);
            if (collection.Count == 1)
            {
                edit_certThumbprint.Text = collection[0].Thumbprint;
                SetCertificateInfoGUI(ref label_certNameValue, ref label_certAuthValue, collection[0].Thumbprint);
            }
        }

        private void btChooseAuthorityCertificate_Click(object sender, EventArgs e)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
            collection = X509Certificate2UI.SelectFromCollection(collection, "Select an authority certificate to use with OTP server", "Select a certificate from the following list to use it to sign client authentication certificates.", X509SelectionFlag.SingleSelection);
            if (collection.Count == 1)
            {
                edit_authorityThumbprint.Text = collection[0].Thumbprint;
                SetCertificateInfoGUI(ref label_authorityCertNameValue, ref label_authorityCertAuthValue, collection[0].Thumbprint);
            }
        }

        private void openFileDialogMultiOTP_FileOk(object sender, CancelEventArgs e)
        {
            edit_multiOtpPath.Text = openFileDialogMultiOTP.FileName;
        }

        private void btChooseMultiOTP_Click(object sender, EventArgs e)
        {
            openFileDialogMultiOTP.ShowDialog();
        }
    }
}
