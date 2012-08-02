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
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Security.Authentication;
using OTPHelpers.XML.OTPPacket;
using OTPServer.Communication.Local.Observer;
using OTPServer.Communication.Local;
using OTPHelpers;

namespace OTPServer.Server
{
    class HandleClient : Observer, IDisposable
    {
        private volatile AutoResetEvent _Waiting = new AutoResetEvent(false);

        // Preparation for protocol changing
        private int _ProtocolVersion = 0;

        private Thread _CommunicationThread = null;
        public Thread CommunicationThread
        {
            get { return this._CommunicationThread; }
        }

        // TODO: Implement a (static) signaling mechanism to communicate the active state with the ClientThread
        private bool _Active;
        public bool Active
        {
            get { return this._Active; }
        }

        public HandleClient()
        {
            string schemaPath = Configuration.Instance.GetStringValue("path");
            if (schemaPath != null)
            {
                schemaPath += "XML\\OTPPacketSchema.xsd";
                PacketHelper.SchemaPath = schemaPath;
            }

            this._Active = false;
        }

        ~HandleClient()
        {
            this._CommunicationThread = null;
            this._Active = false;
        }

        public bool Start(TcpClient tcpClient)
        {
            bool started = false;
            if (!Active && _CommunicationThread == null)
            {
                this._Active = started = true;
                _CommunicationThread = new Thread(Communication);
                _CommunicationThread.Start(tcpClient);
            }
            return started;
        }

        public void Stop()
        {
            this._Active = false;

            if (_CommunicationThread != null)
            {
                _CommunicationThread.Abort();
                _CommunicationThread.Join();
            }
        }

        private void Communication(object tcpClientObject)
        {
            SslStream sslStream = null;
            TcpClient tcpClient = null;
            try
            {
                tcpClient = (TcpClient)tcpClientObject;
                sslStream = new SslStream(tcpClient.GetStream());
                X509Certificate certificate = Authority.Authority.GetServerCertificate();
                sslStream.AuthenticateAsServer(certificate);

                if (!sslStream.IsAuthenticated || !sslStream.IsEncrypted || !sslStream.IsSigned)
                    return;

                OTPPacket otpPacket;

            NextRequest:
                otpPacket = PacketHelper.CreatePacket(ProcessIdentifier.NONE);
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
            catch (ThreadAbortException)
            {
                // Nothing
            }
            catch (ThreadInterruptedException)
            {
                // Nothing
            }
            catch (Exception)
            {
                // TODO: Log it
                if (sslStream != null && sslStream.CanWrite)
                {
                    OTPPacket errorPacket = PacketHelper.CreateErrorPacket(
                                ProcessIdentifier.NONE,
                                "Unknown Error.",
                                Message.STATUS.E_UNKNOWN);
                    WritePacketToStream(sslStream, errorPacket);
                }
            }
            finally
            {
                if (sslStream != null)
                {
                    sslStream.Close();
                    sslStream.Dispose();
                }
                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
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
