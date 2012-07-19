using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OTPServer.Communication.Local;
using OTPServer.XML.OTPPacket;
using OTPServer.Communication.Local.Observer;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace OTPServer.Authority
{
    class Authority : Observer
    {
        private static RequestQueue<OTPPacket, AuthorityResponseObject> __Requests = null;
        private static X509Certificate __ServerCertificate = null;
        private static AutoResetEvent __Waiting = new AutoResetEvent(false);
        private static Storage.ProcessDataStorage __ProcessDataStorage = new Storage.ProcessDataStorage();

        private static Authority __Instance = null;
        public static Authority Instance
        {
            get
            {
                if (__Instance == null)
                    __Instance = new Authority();
                return __Instance;
            }
        }

        private Authority()
        {
            if (__Requests == null)
                __Requests = new RequestQueue<OTPPacket, AuthorityResponseObject>();

            __Requests.Attach(this);
        }

        ~Authority()
        {
            __Active = false;
            __Requests.Detach(this);
            __Requests = null;
        }

        private static bool __Active;
        public static bool Active
        {
            get { return __Active; }
        }

        public bool Start()
        {
            __Active = true;

            return true;
        }

        public bool Stop()
        {
            __Active = false;

            return true;
        }

        private void ProcessRequests()
        {
            while (Active)
            {
                if (__Requests.Empty())
                    __Waiting.WaitOne();

                RequestObject<OTPPacket, AuthorityResponseObject> reqObj = __Requests.Dequeue();

                if (reqObj.Request.Message.Type == Message.TYPE.HELLO)
                {
                    int pid = __ProcessDataStorage.CreateProcess(reqObj.Request);
                    reqObj.Response.ComplexResponse.ProcessIdentifier = pid;                    
                }
                reqObj.Notify();
                return;
            }
        }

        public static RequestObject<OTPPacket, AuthorityResponseObject> Request(Observer observer, OTPPacket otpPacket)
        {
            AuthorityResponseObject repObj = new AuthorityResponseObject();
            return __Requests.EnqueueAndObserve(observer, otpPacket, repObj);
        }

        public static X509Certificate GetServerCertificate()
        {
            if (__ServerCertificate == null) // TODO: Get certificate data from config
                __ServerCertificate = new X509Certificate("..\\path\\to\\Certificate.pfx", "ThisPasswordIsTheSameForInstallingTheCertificate");
            return __ServerCertificate;
        }
    }
}
