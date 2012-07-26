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
using OTPHelpers;

namespace OTPServer.Server
{
    class HandleClient : Observer, IDisposable
    {
        private TcpClient _ClientSocket;
        private volatile AutoResetEvent _Waiting = new AutoResetEvent(false);

        private int _ProtocolVersion = 0;

        private Thread _CommunicationThread = null;
        public Thread ClientThread
        {
            get { return this._CommunicationThread; }
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
            this._CommunicationThread = null;
            this._Active = false;
        }

        public bool Start()
        {
            bool started = false;
            if (!Active && _CommunicationThread == null)
            {
                this._Active = started = true;
                _CommunicationThread = new Thread(Communication);
                _CommunicationThread.Start();
            }
            return started;
        }

        public void Stop(bool stopThread)
        {
            this._Active = false;

            if (stopThread && _CommunicationThread != null)
            {
                _CommunicationThread.Abort();
                _CommunicationThread.Join();
            }

            if (_ClientSocket != null)
                this._ClientSocket.Close();
        }

        private void Communication()
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

                        if (!success && otpPacket.ProtocolVersionMismatch)
                        {
                            // TODO: Client and server should manage to set-up a matching protocol version.

                            OTPPacket errorPacket = PacketHelper.CreateErrorPacket(
                                ProcessIdentifier.NONE,
                                "Protocol version mismatch. Try this one.", // Suggestion is made through OTPPacket's version field
                                Message.STATUS.PR_SWITCH_REQ);
                            WritePacketToStream(sslStream, errorPacket);

                            goto NextRequest;
                        }
                        else if (!success)
                        {
                            OTPPacket errorPacket = PacketHelper.CreateErrorPacket(
                                ProcessIdentifier.NONE,
                                "Malformed packet.",
                                Message.STATUS.E_MALFORMED);
                            WritePacketToStream(sslStream, errorPacket);

                            return;
                        } 

                        if (otpPacket.Message.Type == Message.TYPE.SUCCESS || otpPacket.Message.Type == Message.TYPE.ERROR)
                            goto NextRequest; // Clients should not send ERROR or SUCCESS messages. We dont want the RequestQueue to get spammed.

                        // Setting protocol version used by client
                        // TODO: Implement protocol changing. But now there is only version 1 ;)
                        this._ProtocolVersion = otpPacket.ProtocolVersion;

                        // Handing this request to the Authority's RequestQueue
                        RequestObject<OTPPacket, AuthorityResponseObject> reqObj = Authority.Authority.Request(this, otpPacket);

                        // Wait for answer from RequestQueue (Observer, see Update())
                        this._Waiting.WaitOne();

                        if (reqObj.Response.SimpleResponse)
                        {
                            OTPPacket successPacket;
                            if (reqObj.Request.Message.Type == Message.TYPE.HELLO)
                                successPacket = PacketHelper.CreateHelloPacket(reqObj.Response.ComplexResponse.ProcessIdentifier);
                            else
                            {
                                successPacket = PacketHelper.CreateSuccessPacket(
                                reqObj.Response.ComplexResponse.ProcessIdentifier,
                                reqObj.Response.ComplexResponse.TextMessage,
                                reqObj.Response.ComplexResponse.StatusCode
                                );
                            }
                            WritePacketToStream(sslStream, successPacket);
                        }
                        else
                        {
                            OTPPacket errorPacket = PacketHelper.CreateErrorPacket(
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
                                OTPPacket errorPacket = PacketHelper.CreateErrorPacket(
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
