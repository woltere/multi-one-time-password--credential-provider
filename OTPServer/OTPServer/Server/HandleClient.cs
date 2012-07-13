using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Security.Authentication;
using OTPServer.XML.OTPPacket;
using System.IO;

namespace OTPServer.Server
{
    class HandleClient
    {
        private int _ClientNumber;
        private TcpClient _ClientSocket;

        private bool _Active;
        public bool Active
        {
            get { return this._Active; }
        }

        public HandleClient(TcpClient clientSocket, int clientNumber)
        {
            this._ClientSocket = clientSocket;
            this._ClientNumber = clientNumber;
            this._Active = false;
        }

        ~HandleClient()
        {
        }

        public bool Start()
        {
            bool started = false;
            if (!Active)
            {
                this._Active = started = true;
                Thread ctThread = new Thread(CommunicationThread);
                ctThread.Start();
            }
            return started;
        }

        public void Stop()
        {
            this._Active = false;
        }

        private void CommunicationThread()
        {
            // TODO: Ask Authority for the certificate to use
            X509Certificate certificate = new X509Certificate("..\\path\\to\\Certificate.pfx", "ThisPasswordIsTheSameForInstallingTheCertificate");
            using (SslStream sslStream = new SslStream(this._ClientSocket.GetStream()))
            {
                try
                {
                    sslStream.AuthenticateAsServer(certificate);
                }
                catch (AuthenticationException)
                {
                    this._Active = false;
                }

                if (Active)
                {
                    OTPPacket otpPacket = new OTPPacket();
                    bool success = otpPacket.SetFromXML(sslStream, false);
                    if (!success)
                    {
                        this._Active = false;
                        WritePacketToStream(sslStream, GetErrorPacket(0, "Malformed packet or wrong protocol version", Message.STATUS.E_ERROR));
                    }
                    // TODO: Send packet to the Authority for further inspection
                }
            }
        }

        private OTPPacket GetErrorPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = new OTPPacket();

            otpPacket.ProcessIdentifier.ID = pid;
            otpPacket.Message.Type = Message.TYPE.ERROR;
            otpPacket.Message.TextMessage = message;
            otpPacket.Message.StatusCode = statusCode;

            return otpPacket;
        }

        private void WritePacketToStream(Stream stream, OTPPacket otpPacket)
        {
            string otpPacketAsString = otpPacket.ToXMLString();
            byte[] otpPacketAsByteArray = Encoding.ASCII.GetBytes(otpPacketAsString);

            using (StreamWriter streamWriter = new StreamWriter(stream))
            {
                streamWriter.Write(otpPacketAsByteArray);
            }
        }
    }
}
