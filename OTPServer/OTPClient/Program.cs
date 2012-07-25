using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using OTPHelpers.XML.OTPPacket;
using System.Security.Cryptography;
using System.Threading;

namespace OTPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome...");

            RSACryptoServiceProvider key = new RSACryptoServiceProvider();

            string server = "192.168.1.3";
            TcpClient client = new TcpClient(server, 16588);

            Console.WriteLine("Establishing Connection...");
            using (SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
            {
                Console.WriteLine("Authenticating as Client...");
                sslStream.AuthenticateAsClient(server);

                OTPPacket response = new OTPPacket();
                bool success;
                Data data;

                /////////
                Console.WriteLine("Sending HELLO packet");

                OTPPacket request = CreateHelloPacket(0);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                success = response.SetFromXML(sslStream, true);
                //Console.WriteLine("\nSUCC: " + success.ToString());
                Console.WriteLine("RESP: " + response.ToXMLString());
                //Console.WriteLine("PID:  " + response.ProcessIdentifier.ID.ToString());
                //Console.WriteLine("TYPE: " + response.Message.Type.ToString());

                //Thread.Sleep(1000);

                //////////
                Console.WriteLine("\nSending ADD packet containing public RSA KeyData");

                request = CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = Message.TYPE.ADD;
                request.SetFromXML("<KeyData>" + key.ToXmlString(false) + "</KeyData>", false);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                //Console.WriteLine("\nSUCC: " + success.ToString());
                Console.WriteLine("RESP: " + response.ToXMLString());
                //Console.WriteLine("PID:  " + response.ProcessIdentifier.ID.ToString());
                //Console.WriteLine("TYPE: " + response.Message.Type.ToString());

                //Thread.Sleep(1000);

                //////////
                Console.WriteLine("\nSending ADD packet containing DATA attribute USERNAME");

                request = CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = Message.TYPE.ADD;
                request.Message.TimeStamp = NowMilli();
                request.Message.MAC = key.SignData(
                    Encoding.UTF8.GetBytes(response.ProcessIdentifier.ID.ToString() + request.Message.TimeStamp.ToString()),
                    new MD5CryptoServiceProvider());

                data = new Data();
                data.Username = "testUserName";
                request.DataItems.Add(data);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                //Console.WriteLine("\nSUCC: " + success.ToString());
                Console.WriteLine("RESP: " + response.ToXMLString());
                //Console.WriteLine("PID:  " + response.ProcessIdentifier.ID.ToString());
                //Console.WriteLine("TYPE: " + response.Message.Type.ToString());

                //Thread.Sleep(1000);

                //////////
                Console.WriteLine("\nSending ADD packet containing DATA attribute USERNAME, but wrong MAC");

                request = CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = Message.TYPE.ADD;
                request.Message.TimeStamp = NowMilli();
                request.Message.MAC = key.SignData(
                    Encoding.UTF8.GetBytes(response.ProcessIdentifier.ID.ToString() + (request.Message.TimeStamp + 1).ToString()),
                    new MD5CryptoServiceProvider());

                data = new Data();
                data.Username = "testUserName";
                request.DataItems.Add(data);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                //Console.WriteLine("\nSUCC: " + success.ToString());
                Console.WriteLine("RESP: " + response.ToXMLString());
                //Console.WriteLine("PID:  " + response.ProcessIdentifier.ID.ToString());
                //Console.WriteLine("TYPE: " + response.Message.Type.ToString());

                //Thread.Sleep(1000);
            }
            client.Close();
            key.Dispose();
        }
        // The following method is invoked by the RemoteCertificateValidationDelegate.
        // This allows you to check the certificate and accept or reject it
        // return true will accept the certificate
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Console.WriteLine("Validating Server Certificate...");

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                Console.WriteLine("Validation OK...");
                return true;
            }

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private static void SetMessageAttributes(ref OTPPacket otpPacket, Message.TYPE type, string textMessage, Message.STATUS statusCode)
        {
            otpPacket.Message.Type = type;
            otpPacket.Message.TextMessage = textMessage;
            otpPacket.Message.StatusCode = statusCode;
        }

        private static OTPPacket CreatePacket(int pid)
        {
            OTPPacket otpPacket = new OTPPacket();
            otpPacket.ProcessIdentifier.ID = pid;
            return otpPacket;
        }

        private static OTPPacket CreateErrorPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.ERROR, message, statusCode);

            return otpPacket;
        }

        private static OTPPacket CreateSuccessPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.SUCCESS, message, statusCode);

            return otpPacket;
        }

        private static OTPPacket CreateHelloPacket(int pid)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.HELLO, String.Empty, Message.STATUS.NONE);

            return otpPacket;
        }

        private static void WritePacketToStream(SslStream stream, OTPPacket otpPacket)
        {
            string otpPacketAsString = otpPacket.ToXMLString();
            byte[] otpPacketAsByteArray = Encoding.UTF8.GetBytes(otpPacketAsString);

            stream.Write(otpPacketAsByteArray);
            stream.Flush();
        }

        // MILLISECONDS
        private static long NowMilli()
        {
            DateTime epochStart = new DateTime(1970, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan ts = new TimeSpan(now.Ticks - epochStart.Ticks);
            return (Convert.ToInt64(ts.TotalMilliseconds));
        }
    }
}
