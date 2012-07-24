using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OTPServer.Communication.Local;
using OTPServer.XML.OTPPacket;
using OTPServer.Communication.Local.Observer;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace OTPServer.Authority
{
    class Authority : Observer
    {
        public static EventLog Logger;

        private static Storage.ProcessDataStorage __ProcessDataStorage = null;
        private static volatile RequestQueue<OTPPacket, AuthorityResponseObject> __Requests = null;

        private static X509Certificate __ServerCertificate = null;
        private static volatile AutoResetEvent __Waiting = new AutoResetEvent(false);

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
            File.AppendAllText("C:\\authlog2.log", "AUTH: STARTING ProcessRequests;\n");
            while (Active)
            {
                File.AppendAllText("C:\\authlog2.log", "AUTH: IS ACTIVE;\n");
                if (__Requests.Empty())
                {
                    File.AppendAllText("C:\\authlog2.log", "AUTH: QUEUE EMPTY; WAITING;\n");
                    __Waiting.WaitOne();
                }

                File.AppendAllText("C:\\authlog2.log", "AUTH: PROCESSING;\n");

                RequestObject<OTPPacket, AuthorityResponseObject> reqObj = __Requests.Dequeue();

                // TODO: Check ProcessAge when existing (for life time check). Implement maintainer thread to check ProcessDataStorage.GetOldestProcess().
                if (reqObj.Request.Message.Type == Message.TYPE.HELLO)
                {
                    File.AppendAllText("C:\\authlog2.log", "AUTH: HELLO;\n");
                    reqObj.Request.ProcessIdentifier.ID = ProcessIdentifier.NONE; // Clients can not choose a PID
                    int pid = __ProcessDataStorage.CreateProcess(reqObj.Request);
                    AnswerSuccess(ref reqObj, pid, Message.STATUS.S_OK);
                }
                else if (reqObj.Request.Message.Type == Message.TYPE.ERROR || reqObj.Request.Message.Type == Message.TYPE.SUCCESS)
                {
                    File.AppendAllText("C:\\authlog2.log", "AUTH: SUCC||ERR;\n");
                    // TODO: Server should filter out ERROR and SUCCESS messages from client to avoid filling up the RequestQueue.
                    // DO NOTHING
                }
                else
                {
                    File.AppendAllText("C:\\authlog2.log", "AUTH: OTHER MESSAGE TYPES;\n");
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
                        File.AppendAllText("C:\\authlog2.log", "AUTH: REQ DENIED;\n");
                        AnswerNotAuthorized(ref reqObj);
                        goto NotifyAndContinue;
                    }

                    File.AppendAllText("C:\\authlog2.log", "AUTH: REQ AUTHED;\n");

                    if (reqObj.Request.Message.Type == Message.TYPE.ADD)
                    {
                        File.AppendAllText("C:\\authlog2.log", "AUTH: ADD;\n");
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
                        File.AppendAllText("C:\\authlog2.log", "AUTH: VERIFY;\n");
                        // TODO: add (optional) data to process and verify (through Agent). send verify result.
                    }
                    else if (reqObj.Request.Message.Type == Message.TYPE.RESYNC)
                    {
                        File.AppendAllText("C:\\authlog2.log", "AUTH: RESYNC;\n");
                        // TODO: add (optional) data to process and resync (through Agent). send resync result.
                    }
                    else
                    {
                        File.AppendAllText("C:\\authlog2.log", "AUTH: UNKNOWN/MALFORMED;\n");
                        AnswerFailure(ref reqObj, reqObj.Request.ProcessIdentifier.ID, Message.STATUS.E_ERROR, "Malformed message. Dont know what to do.");
                    }
                }

            NotifyAndContinue:
                File.AppendAllText("C:\\authlog2.log", "AUTH: NOTIFY AND CONTINUE;\n");
                reqObj.Notify();
            }
        }

        private void AnswerNotAuthorized(ref RequestObject<OTPPacket, AuthorityResponseObject> reqObj)
        {
            AnswerFailure(ref reqObj, reqObj.Request.ProcessIdentifier.ID, Message.STATUS.E_ERROR, "Request not authorized or incomplete. No public key provided or MAC is wrong.");
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
            File.AppendAllText("C:\\authlog.log", "AUTH: Getting request to enqueue;\n");

            AuthorityResponseObject repObj = new AuthorityResponseObject();
            RequestObject<OTPPacket, AuthorityResponseObject> reqObj = null;
            lock (__Requests)
                reqObj = __Requests.EnqueueAndObserve(observer, otpPacket, repObj);

            return reqObj;
        }

        public static X509Certificate GetServerCertificate()
        {
            if (__ServerCertificate == null) // TODO: Get certificate data from config
                __ServerCertificate = new X509Certificate2("C:\\A1833.pfx", "LpVA90");
            return __ServerCertificate;
        }

        public override void Update()
        {
            File.AppendAllText("C:\\authlog.log", "AUTH: Update();\n");
            __Waiting.Set();
            File.AppendAllText("C:\\authlog.log", "AUTH: Update: __Waiting.Set()'ed;\n");
        }
    }
}
