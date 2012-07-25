using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Security.Authentication;
using OTPHelpers.XML.OTPPacket;
using System.IO;
using OTPServer.Communication.Local.Observer;
using OTPServer.Communication.Local;

namespace OTPServer.Server
{
    class HandleClient : Observer, IDisposable
    {
        private TcpClient _ClientSocket;
        private volatile AutoResetEvent _Waiting = new AutoResetEvent(false);

        private Thread _ClientThread = null;
        public Thread ClientThread
        {
            get { return this._ClientThread; }
        }

        // TODO: Implement a (static) signaling mechanism to communicate the active state with the ClientThread
        private bool _Active;
        public bool Active
        {
            get { return this._Active; }
        }

        public HandleClient(TcpClient clientSocket)
        {
            this._ClientSocket = clientSocket;
            this._Active = false;
        }

        ~HandleClient()
        {
            this._ClientSocket = null;
            this._ClientThread = null;
            this._Active = false;
        }

        public bool Start()
        {
            bool started = false;
            if (!Active && _ClientThread == null)
            {
                this._Active = started = true;
                _ClientThread = new Thread(CommunicationThread);
                _ClientThread.Start();
            }
            return started;
        }

        public void Stop(bool stopThread)
        {
            this._Active = false;

            if (stopThread && _ClientThread != null)
            {
                _ClientThread.Abort();
                _ClientThread.Join();
            }

            if (_ClientSocket != null)
                this._ClientSocket.Close();
        }

        private void CommunicationThread()
        {
            try
            {
                X509Certificate certificate = Authority.Authority.GetServerCertificate();
                using (SslStream sslStream = new SslStream(this._ClientSocket.GetStream()))
                {
                    try
                    {
                        sslStream.AuthenticateAsServer(certificate);

                        OTPPacket otpPacket;

                    NextRequest:
                        otpPacket = new OTPPacket();
                        bool success = otpPacket.SetFromXML(sslStream, true);

                        if (!success)
                        {
                            // TODO: Client and server should manage to set-up a matching protocol version. (Now server rejects any request thats above himself)

                            OTPPacket errorPacket = CreateErrorPacket(
                                ProcessIdentifier.NONE,
                                "Malformed packet or wrong protocol version",
                                Message.STATUS.E_ERROR);
                            WritePacketToStream(sslStream, errorPacket);

                            return;
                        }

                        RequestObject<OTPPacket, AuthorityResponseObject> reqObj = Authority.Authority.Request(this, otpPacket);

                        // Wait for answer from RequestQueue (Observer, see Update())
                        this._Waiting.WaitOne();

                        if (reqObj.Response.SimpleResponse)
                        {
                            OTPPacket successPacket;
                            if (reqObj.Request.Message.Type == Message.TYPE.HELLO)
                                successPacket = CreateHelloPacket(reqObj.Response.ComplexResponse.ProcessIdentifier);
                            else
                            {
                                successPacket = CreateSuccessPacket(
                                reqObj.Response.ComplexResponse.ProcessIdentifier,
                                reqObj.Response.ComplexResponse.TextMessage,
                                reqObj.Response.ComplexResponse.StatusCode
                                );
                            }
                            WritePacketToStream(sslStream, successPacket);
                        }
                        else
                        {
                            OTPPacket errorPacket = CreateErrorPacket(
                                reqObj.Response.ComplexResponse.ProcessIdentifier,
                                reqObj.Response.ComplexResponse.TextMessage,
                                reqObj.Response.ComplexResponse.StatusCode
                                );
                            WritePacketToStream(sslStream, errorPacket);
                        }

                        goto NextRequest;
                    }
                    catch (AuthenticationException)
                    {
                        // TODO: Log it
                    }
                    catch (Exception)
                    {
                        // TODO: Log it
                        lock (sslStream)
                        {
                            if (sslStream.CanWrite)
                            {
                                OTPPacket errorPacket = CreateErrorPacket(
                                            ProcessIdentifier.NONE,
                                            "Unknown Error.",
                                            Message.STATUS.E_UNKNOWN);
                                WritePacketToStream(sslStream, errorPacket);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Log it
            }
            finally
            {
                Stop(false); // We don't need to interrupt the thread itself. We already reached the end.
            }
        }

        private void SetMessageAttributes(ref OTPPacket otpPacket, Message.TYPE type, string textMessage, Message.STATUS statusCode)
        {
            otpPacket.Message.Type = type;
            otpPacket.Message.TextMessage = textMessage;
            otpPacket.Message.StatusCode = statusCode;
        }

        private OTPPacket CreatePacket(int pid)
        {
            OTPPacket otpPacket = new OTPPacket();
            otpPacket.ProcessIdentifier.ID = pid;
            return otpPacket;
        }

        private OTPPacket CreateErrorPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.ERROR, message, statusCode);

            return otpPacket;
        }

        private OTPPacket CreateSuccessPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.SUCCESS, message, statusCode);

            return otpPacket;
        }

        private OTPPacket CreateHelloPacket(int pid)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.HELLO, String.Empty, Message.STATUS.NONE);

            return otpPacket;
        }

        private void WritePacketToStream(SslStream stream, OTPPacket otpPacket)
        {
            string otpPacketAsString = otpPacket.ToXMLString();
            byte[] otpPacketAsByteArray = Encoding.UTF8.GetBytes(otpPacketAsString);
            
            stream.Write(otpPacketAsByteArray);
            stream.Flush();
        }

        public override void Update()
        {
            // Returning from a RequestQueue
            this._Waiting.Set();
        }

        public void Dispose()
        {
            _Waiting.Dispose();
        }
    }
}
