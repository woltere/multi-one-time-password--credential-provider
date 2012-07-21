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
        private static Storage.ProcessDataStorage __ProcessDataStorage = null;
        private static RequestQueue<OTPPacket, AuthorityResponseObject> __Requests = null;

        private static X509Certificate __ServerCertificate = null;
        private static AutoResetEvent __Waiting = new AutoResetEvent(false);

        private static Thread __ProcessRequestsThread = null;

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

            if (__ProcessDataStorage == null)
                __ProcessDataStorage = new Storage.ProcessDataStorage();

            __Requests.Attach(this);
        }

        ~Authority()
        {
            __Active = false;

            __Requests.Detach(this);
            __Requests = null;

            __ProcessRequestsThread = null;
            __ProcessDataStorage = null;
            __ServerCertificate = null;
        }

        private static bool __Active;
        public static bool Active
        {
            get { return __Active; }
        }

        public bool Start()
        {
            __Active = true;

            if (__ProcessRequestsThread == null)
            {
                __ProcessRequestsThread = new Thread(ProcessRequests);
                __ProcessRequestsThread.Start();
            }

            return true;
        }

        public bool Stop()
        {
            __Active = false;

            if (__ProcessRequestsThread != null)
                __ProcessRequestsThread.Interrupt();

            return true;
        }

        private void ProcessRequests()
        {
            while (Active)
            {
                if (__Requests.Empty())
                    __Waiting.WaitOne();

                RequestObject<OTPPacket, AuthorityResponseObject> reqObj = __Requests.Dequeue();

                // TODO: Check ProcessAge when existing (for life time check). Implement maintainer thread to check ProcessDataStorage.GetOldestProcess().
                if (reqObj.Request.Message.Type == Message.TYPE.HELLO)
                {
                    int pid = __ProcessDataStorage.CreateProcess(reqObj.Request);
                    AnswerSuccess(ref reqObj, pid, Message.STATUS.S_OK);
                }
                else if (reqObj.Request.Message.Type == Message.TYPE.ERROR || reqObj.Request.Message.Type == Message.TYPE.SUCCESS)
                {
                    // TODO: Server should filter out ERROR and SUCCESS messages from client to avoid filling up the RequestQueue.
                    // DO NOTHING
                }
                else
                {
                    // These requests need to be authorized, except an ADD containing KeyData only.
                    bool reqAuthorized = false;
                    if (__ProcessDataStorage.ProcessExists(reqObj.Request) > 0
                     && __ProcessDataStorage.GetProcess(reqObj.Request).KeyData.Type != KeyData.TYPE.NONE)
                        reqAuthorized = CheckMessageAuthenticationCode(reqObj.Request);
                    else if (__ProcessDataStorage.ProcessExists(reqObj.Request) > 0
                          && reqObj.Request.Message.Type == Message.TYPE.ADD 
                          && reqObj.Request.KeyData.Type != KeyData.TYPE.NONE
                          && reqObj.Request.DataItems.Count == 0)
                        reqAuthorized = true;

                    if (!reqAuthorized)
                    {
                        AnswerNotAuthorized(ref reqObj);
                        goto NotifyAndReturn;
                    }

                    if (reqObj.Request.Message.Type == Message.TYPE.ADD)
                    { 
                        if (__ProcessDataStorage.AddDataFromPacketToProcess(reqObj.Request))
                        {
                            AnswerSuccess(ref reqObj, reqObj.Request.ProcessIdentifier.ID, Message.STATUS.S_OK);
                        }
                        else
                        {
                            AnswerFailure(ref reqObj, reqObj.Request.ProcessIdentifier.ID, Message.STATUS.E_ERROR, "Could not add data. Maybe duplicate data was sent.");
                        }
                    }
                    else if (reqObj.Request.Message.Type == Message.TYPE.VERIFY)
                    {
                        // TODO: add (optional) data to process and verify (through Agent). send verify result.
                    }
                    else if (reqObj.Request.Message.Type == Message.TYPE.RESYNC)
                    {
                        // TODO: add (optional) data to process and resync (through Agent). send resync result.
                    }
                    else
                    {
                        AnswerFailure(ref reqObj, reqObj.Request.ProcessIdentifier.ID, Message.STATUS.E_ERROR, "Malformed message. Dont know what to do.");
                    }
                }

            NotifyAndReturn:
                reqObj.Notify();
                return;
            }
        }

        private void AnswerNotAuthorized(ref RequestObject<OTPPacket, AuthorityResponseObject> reqObj)
        {
            AnswerFailure(ref reqObj, reqObj.Request.ProcessIdentifier.ID, Message.STATUS.E_ERROR, "Request not authorized. No public key provided or MAC is wrong.");
        }

        private bool CheckMessageAuthenticationCode(OTPPacket otpPacket)
        {
            return false;
        }

        private void AnswerSuccess(ref RequestObject<OTPPacket, AuthorityResponseObject> reqObj, int pid, Message.STATUS statusCode)
        {
            Answer(ref reqObj, true, pid, statusCode, String.Empty);
        }

        private void AnswerFailure(ref RequestObject<OTPPacket, AuthorityResponseObject> reqObj, int pid, Message.STATUS statusCode, string textMessage)
        {
            Answer(ref reqObj, false, pid, statusCode, textMessage);
        }

        private void Answer(ref RequestObject<OTPPacket, AuthorityResponseObject> reqObj, bool simpleResponse, int pid, Message.STATUS statusCode, string textMessage)
        {
            reqObj.Response.SimpleResponse = simpleResponse;
            reqObj.Response.ComplexResponse.ProcessIdentifier = pid;
            reqObj.Response.ComplexResponse.StatusCode = statusCode;
            reqObj.Response.ComplexResponse.TextMessage = textMessage;
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
